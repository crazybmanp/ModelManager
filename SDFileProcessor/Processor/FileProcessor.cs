using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Timers;
using System.Windows;
using MetadataExtractor;
using Directory = MetadataExtractor.Directory;

namespace SDFileProcessor.Processor;

class FileProcessor
{
    private readonly string path;

    private DateTime? lastCheck = null;
    private readonly TimeSpan checkFrequency = new TimeSpan(0, 0, 0, 0, 250);
    private readonly System.Timers.Timer timer;

    private List<FileProgress> files;
    private static readonly ModelHashCache ModelHashCache = new ModelHashCache();

    private bool active;

    public FileProcessor(string path, TimeSpan checkFrequency) : this(path)
    {
        this.checkFrequency = checkFrequency;
    }

    public FileProcessor(string path)
    {
        this.path = path;
        active = false;

        timer = new System.Timers.Timer();
        timer.Interval = checkFrequency.TotalMilliseconds;
        timer.Elapsed += timer_Elapsed;
        timer.AutoReset = false;

        LoadFileList();
        files ??= new List<FileProgress>();
    }

    public void Start()
    {
        active = true;
        CheckFolder();
        timer.Start();
    }

    public void Stop()
    {
        active = false;
        timer.Stop();
    }

    public bool IsRunning()
    {
        return active;
    }

    public DateTime? GetLastCheck()
    {
	    return lastCheck;
    }

    public FileStats? GetFileStats()
    {
        FileStats stats = new FileStats();
        stats.TotalFiles = files.Count;
        stats.UnprocessedFiles = files.Count(f => f.GetStatus() == ProcessingStatus.Unprocessed);
        stats.ProcessingFiles = files.Count(f => f.GetStatus() == ProcessingStatus.Processing);
        stats.ProcessedFiles = files.Count(f => f.GetStatus() == ProcessingStatus.Processed);
        stats.IgnoredFiles = files.Count(f => f.GetStatus() == ProcessingStatus.NotAnImage);
        stats.FailedFiles = files.Count(f => f.GetStatus() == ProcessingStatus.Unrecoverable || f.GetStatus() == ProcessingStatus.NoMetadata);
        return stats;
    }

    public void Retry(ProcessingStatus status)
    {
        foreach (FileProgress item in files.Where(e => e.GetStatus() == status))
        {
            item.SetStatus(ProcessingStatus.Unknown);
        }
    }

    private void timer_Elapsed(object? sender, ElapsedEventArgs e)
    {
        if (active)
        {
            try
            {
                CheckFolder();
                timer.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"There has been an error checking the folder:\n{ex}", "Error in Check Folder", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        else
        {
            timer.Stop();
        }
    }

    private readonly string[] validExtensions = [".jpg", ".jpeg", ".png", ".bmp"];
	private void CheckFolder()
    {
        if (!System.IO.Directory.Exists(path))
        {
            throw new FileNotFoundException($"Cannot Find the watch path at {path}");
        }


		DirectoryInfo watchdir = new DirectoryInfo(path);
		FileInfo[] newFiles = watchdir.GetFiles()
			.Where(e => validExtensions.Contains(e.Extension))
			.ToArray();

        MergeCurrentFiles(newFiles);

        CheckFiles();

        List<FileProgress> filesToProcess = files.Where(f => f.GetStatus() == ProcessingStatus.Unprocessed).OrderBy(f => f.FileInfo.CreationTime).Take(1).ToList();
        ProcessFiles(filesToProcess);

        SaveFileList();
        lastCheck = DateTime.Now;
    }

    private static readonly string FileListFile = "fileList.txt";
    private string FileListLoc => Path.Combine(path, FileListFile);
    JsonSerializerOptions options = new()
    {
        WriteIndented = true,
    };

    private void LoadFileList()
    {
        if (File.Exists(FileListLoc))
        {
            string serial = File.ReadAllText(FileListLoc);
            JsonSerializerOptions optionsCopy = new(options);
            List<FileProgressSerial> filesSerials = JsonSerializer.Deserialize<List<FileProgressSerial>>(serial, optionsCopy) ?? throw new Exception("File List storage file is corrupt!");
            files = filesSerials.Select(e => new FileProgress(e)).ToList();
        }
        else
        {
            files = new List<FileProgress>();
        }
    }

    private void SaveFileList()
    {
        JsonSerializerOptions optionsCopy = new JsonSerializerOptions(options);
        string serializedList = JsonSerializer.Serialize(files.Select(e => e.GetSerializable()).ToList(), optionsCopy);
        File.WriteAllText(FileListLoc, serializedList);
    }

    private void MergeCurrentFiles(FileInfo[] currentFiles)
    {
        //find all new files
        Dictionary<string, FileProgress> allFilesMap = files.ToDictionary(f => f.Path);
        FileInfo[] newFiles = currentFiles.Where(f => !allFilesMap.ContainsKey(f.FullName)).ToArray();
        foreach (FileInfo file in newFiles)
        {
            files.Add(new FileProgress(file));
        }

        //find all missing files
        Dictionary<string, FileInfo> currentFilesMap = currentFiles.ToDictionary(f => f.FullName);
        FileProgress[] missingFiles = files.Where(f => !currentFilesMap.ContainsKey(f.Path)).ToArray();
        foreach (FileProgress item in missingFiles)
        {
            files.Remove(item);
        }
    }

    private void CheckFiles()
    {
        foreach (FileProgress file in files)
        {
            if (file.IsProcessable())
            {
                file.CheckProcessingStatus();
            }
        }
    }

    private void ProcessFiles(List<FileProgress> filesToProcess)
    {
        foreach (FileProgress file in filesToProcess)
        {
            ProcessFile(file);
        }
    }

    private static void ProcessFile(FileProgress file)
    {
        file.SetStatus(ProcessingStatus.Processing);
        try
        {
            try
            {
                if (!FileProgress.ValidExtensions.Contains(file.FileInfo.Extension))
                {
                    throw new RejectException(RejectException.RejectReason.InvalidFileType, $"Invalid file type: {file.FileInfo.Extension}");
                }
                else if (!file.FileInfo.Exists)
                {
                    throw new RejectException(RejectException.RejectReason.FileDoesNotExist, $"File does not exist: {file.Path}");
                }

                string tagText;
                try
                {
                    IEnumerable<Directory> directories = ImageMetadataReader.ReadMetadata(file.Path);

                    Directory pngText = directories.First(d => d.Name == "PNG-tEXt");
                    Tag tag = pngText.Tags.First(t => t.Name == "Textual Data");

                    tagText = tag.Description ?? throw new Exception("No metadata in tag");
                }
                catch (Exception ex)
                {
                    throw new RejectException(RejectException.RejectReason.CouldNotReadMetadata, $"Could not read metadata from file: {ex}");
                }

                // Tag File
                ParsedMetadata pmr = ParseMetadata(tagText);
                if (pmr.modelName == null)
                {
                    bool check = ModelHashCache.GetModelNameFromHash(ref pmr);
                    if (!check)
                    {
                        LogNewUnknownModelFile(pmr.modelHash);
                        throw new RejectException(RejectException.RejectReason.CouldNotFindModelNameFromHash, $"Could not find model name from hash '{pmr.modelHash}'");
                    }
                }
                else
                {
                    ModelHashCache.AddModelNameForHash(pmr);
                }

                string tags = MetadataToTags(pmr);

                const string tagAppendTxt = @".Tags.txt";
                string tagFileLoc = file.Path + tagAppendTxt;
                FileStream tagStream = new FileStream(tagFileLoc, FileMode.Create);

                tagStream.Write(Encoding.UTF8.GetBytes(tags));
                tagStream.Close();

                // Notes File
                string notes =
                    $"Prompt: {pmr.prompt}|Negative Prompt: {pmr.negativePrompt}|LastLine: {pmr.lastLine}";

                const string notesAppendTxt = @".Notes.txt";
                string notesFileLoc = file.Path + notesAppendTxt;
                FileStream notesStream = new FileStream(notesFileLoc, FileMode.Create);

                notesStream.Write(Encoding.UTF8.GetBytes(notes));
                notesStream.Close();
            }
            catch (RejectException ex)
            {
                switch (ex.Reason)
                {
                    case RejectException.RejectReason.FileDoesNotExist:
                        file.SetStatus(ProcessingStatus.Unrecoverable);
                        break;
                    case RejectException.RejectReason.InvalidFileType:
                        file.SetStatus(ProcessingStatus.Unknown);
                        break;
                    case RejectException.RejectReason.CouldNotReadMetadata:
                        file.SetStatus(ProcessingStatus.NoMetadata);
                        break;
                    case RejectException.RejectReason.CouldNotFindModelNameFromHash:
                    case RejectException.RejectReason.NewLastRowValue:
                        file.SetStatus(ProcessingStatus.Unrecoverable);
                        break;
                    case RejectException.RejectReason.Other:
                        throw;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
                return;
            }
        }
        catch (Exception ex)
        {
            string tagFileLoc = file.Path + @".Tags.txt";
            string notesFileLoc = file.Path + @".Notes.txt";

            FileInfo tagFile = new FileInfo(tagFileLoc);
            FileInfo notesFile = new FileInfo(notesFileLoc);

            if (tagFile.Exists)
            {
                tagFile.Delete();
            }

            if (notesFile.Exists)
            {
                notesFile.Delete();
            }

            file.SetStatus(ProcessingStatus.Unrecoverable);
            MessageBox.Show($"Error Processing file {file.FileName}, This file is unrecoverable: \n{ex.Message}", "Processingt File", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        file.SetStatus(ProcessingStatus.Processed);
    }

    private static string MetadataToTags(ParsedMetadata pmr)
    {
        string output = "";
        foreach (string p in pmr.parameters)
        {
            AddTag(p, ref output);
        }

        foreach (string p in pmr.negativeParemeters)
        {
            AddTag(p, ref output);
        }

        AddTag(pmr.cfg, ref output);
        AddTag(pmr.sampler, ref output);
        AddTag(pmr.ScheduleType, ref output, false);
        AddTag(pmr.seed, ref output);
        AddTag(pmr.steps, ref output);
        AddTag(pmr.modelName, ref output);
        AddTag(pmr.modelHash, ref output);
        AddTag(pmr.loras, ref output, false);
        AddTag(pmr.hiresUpscale, ref output, false);
        AddTag(pmr.hiresSteps, ref output, false);
        AddTag(pmr.hiresUpscaler, ref output, false);
        AddTag(pmr.highresScheduleType, ref output, false);
        AddTag(pmr.vaeHash, ref output, false);
        AddTag(pmr.vae, ref output, false);
        AddTag(pmr.variationSeed, ref output, false);
        AddTag(pmr.variationSeedStrength, ref output, false);
        AddTag(pmr.batchsize, ref output, false);
        AddTag(pmr.batchpos, ref output, false);

        return output;
    }

    private static void AddTag(string? tag, ref string output, bool required = true)
    {
        if (!string.IsNullOrEmpty(tag))
        {
            if (output != "")
            {
                output += $"\n{tag}";
            }
            else
            {
                output += tag;
            }
        }
        else if (required)
        {
            throw new Exception("Tag is required");
        }
    }

    private static ParsedMetadata ParseMetadata(string metadata)
    {
        const string paramstring = @"parameters: ";
        const string negparamstring = @"Negative prompt: ";
        const string lastRow = @"Steps";

        byte[] byteArray = Encoding.UTF8.GetBytes(metadata);
        MemoryStream stream = new MemoryStream(byteArray);
        StreamReader sr = new StreamReader(stream);

        string parameters = "";
        string negparameters = "";
        string lastrow = "";

        string? line = sr.ReadLine();
        while (line != null)
        {
            if (line.StartsWith(paramstring))
            {
                line = line.Substring(paramstring.Length);
                line.TrimStart();
                if (!(line.StartsWith(negparamstring) || line.StartsWith(lastRow)))
                {
                    parameters = line;
                    line = sr.ReadLine();
                    while (line != null && !line.StartsWith(negparamstring) && !line.StartsWith(lastRow))
                    {
                        parameters += line;
                        line = sr.ReadLine();
                    }
                }
            }
            else if (line.StartsWith(negparamstring))
            {
                line = line.Substring(negparamstring.Length);
                line.TrimStart();

                negparameters = line;
                line = sr.ReadLine();
                while (line != null && !line.StartsWith(lastRow))
                {
                    negparameters += line;
                    line = sr.ReadLine();
                }
            }
            else if (line.StartsWith(lastRow))
            {
                lastrow = line;
                line = sr.ReadLine();
            }
            else
            {
                throw new Exception($"Unknown line: {line}");
            }
        }

        Dictionary<string, string> lastrowDict = ParseLastRow(lastrow);


        ParsedMetadata parsedMetadata = new ParsedMetadata
        {
            parameters = ParseTags(parameters),
            negativeParemeters = ParseTags(negparameters, "negative"),
            cfg = ParsedMetadataElement("cfg", lastrowDict, "CFG scale"),
            sampler = ParsedMetadataElement("sampler", lastrowDict, "Sampler"),
            ScheduleType = ParsedMetadataElement("scheduletype", lastrowDict, "Schedule type", false),
            seed = ParsedMetadataElement("seed", lastrowDict, "Seed"),
            steps = ParsedMetadataElement("steps", lastrowDict, "Steps"),
            modelName = ParsedMetadataElement("model", lastrowDict, "Model", false),
            modelHash = ParsedMetadataElement("modelhash", lastrowDict, "Model hash"),
            hiresUpscale = ParsedMetadataElement("hiresupscale", lastrowDict, "Hires upscale", false),
            hiresSteps = ParsedMetadataElement("hiressteps", lastrowDict, "Hires steps", false),
            hiresUpscaler = ParsedMetadataElement("hiresupscaler", lastrowDict, "Hires upscaler", false),
            highresScheduleType = ParsedMetadataElement("highresscheduletype", lastrowDict, "Hires schedule type", false),
            vaeHash = ParsedMetadataElement("vaehash", lastrowDict, "VAE hash", false),
            vae = ParsedMetadataElement("vae", lastrowDict, "VAE", false),
            variationSeed = ParsedMetadataElement("variationseed", lastrowDict, "Variation seed", false),
            variationSeedStrength = ParsedMetadataElement("variationseedstrength", lastrowDict, "Variation seed strength", false),
            batchsize = ParsedMetadataElement("batchsize", lastrowDict, "Batch size", false),
            batchpos = ParsedMetadataElement("batchpos", lastrowDict, "Batch pos", false),
            loras = lastrowDict.GetValueOrDefault("Lora hashes", ""),
            prompt = parameters,
            negativePrompt = negparameters,
            lastLine = lastrow
        };

        return parsedMetadata;
    }

    private static string ParsedMetadataElement(string tagNamespace, Dictionary<string, string> lastrowDict, string s)
    {
        return ParsedMetadataElement(tagNamespace, lastrowDict, s, true) ?? throw new Exception($"Could not find {s} in dictionary");
    }
    private static string? ParsedMetadataElement(string tagNamespace, Dictionary<string, string> lastrowDict, string s, bool orFail)
    {
        if (lastrowDict.TryGetValue(s, out var value))
        {
            return $"{tagNamespace}:{value}";
        }
        else
        {
            if (orFail)
            {
                throw new Exception($"Could not find {s} in dictionary");
            }
            return null;
        }
    }

    private static Dictionary<string, string> ParseLastRow(string lastrow)
    {
        bool rejecttracker = false;

        byte[] byteArray = Encoding.UTF8.GetBytes(lastrow);
        MemoryStream stream = new MemoryStream(byteArray);
        StreamReader sr = new StreamReader(stream);

        Dictionary<string, string> metadata = new Dictionary<string, string>();
        while (!sr.EndOfStream)
        {
            string tag = ReadNextTag(sr).Trim();

            string contents = ProcessTag(tag, sr, out bool reject).Trim();
            if (contents != "")
            {
                metadata.Add(tag.Trim(), contents);
            }

            if (reject)
            {
                rejecttracker = true;
            }
        }

        if (rejecttracker)
        {
            throw new RejectException(RejectException.RejectReason.NewLastRowValue, "Rejecting file due to one or more new Last Row Values;");
        }
        return metadata;
    }

    private static readonly List<string> LastlineTagIgnoreList = new List<string> {
            "ComfyUI Workflows",
            "Clip skip",
            "Token merging ratio",
            "NGMS",
            "Version",
            "Size",
            "RP Active",
            "RP Divide mode",
            "RP Matrix submode",
            "RP Mask submode",
            "RP Prompt submode",
            "RP Calc Mode",
            "RP Ratios",
            "RP Base Ratios",
            "RP Use Base",
            "RP Use Common",
            "RP Use Ncommon",
            "RP Options",
            "RP LoRA Neg Te Ratios",
            "RP LoRA Neg U Ratios",
            "RP threshold",
            "RP LoRA Stop Step",
            "RP LoRA Hires Stop Step",
            "RP Flip",
            "RP Change AND",
            "ControlNet 0",
            "Denoising strength",
            "Token merging ratio hr",
            "Face restoration",
            "RNG",
            "LLuL Enabled",
            "LLuL Multiply",
            "LLuL Weight",
            "LLuL Layers",
            "LLuL Apply to",
            "LLuL Start steps",
            "LLuL Max steps",
            "LLuL Upscaler",
            "LLuL Downscaler",
            "LLuL Interpolation",
            "LLuL x",
            "LLuL y",
            "ADetailer model",
            "ADetailer confidence",
            "ADetailer dilate/erode",
            "ADetailer mask blur",
            "ADetailer denoising strength",
            "ADetailer inpaint only masked",
            "ADetailer inpaint padding",
            "ADetailer version",
            "ADetailer model 2nd",
            "ADetailer confidence 2nd",
            "ADetailer dilate/erode 2nd",
            "ADetailer mask blur 2nd",
            "ADetailer denoising strength 2nd",
            "ADetailer inpaint only masked 2nd",
            "ADetailer inpaint padding 2nd",
            "ControlNet"
            };

    private static string ProcessTag(string tag, StreamReader sr, out bool reject)
    {
        reject = false;

        char c = (char)sr.Read();
        if (c != ' ')
        {
            throw new Exception("error in assumption in ProcessingTag");

        }

        List<string> elements;
        c = (char)sr.Peek();
        switch (c)
        {
            case '"':
                elements = ExtractLastLineList(sr);
                break;
            default:
                elements = [ReadToNextTag(sr)];
                break;
        }

        if (LastlineTagIgnoreList.Contains(tag))
        {
            return "";
        }

        switch (tag)
        {
            case "Lora hashes":
                return string.Join('\n', elements.Select(e => $"lora:{e}"));
            case "TI hashes":
                return string.Join('\n', elements.Select(e => $"ti:{e}"));
            case "Model":
            case "Model hash":
            case "CFG scale":
            case "Sampler":
            case "Schedule type":
            case "Seed":
            case "Hires upscale":
            case "Hires steps":
            case "Hires upscaler":
            case "Hires schedule type":
            case "VAE hash":
            case "VAE":
            case "Variation seed":
            case "Variation seed strength":
            case "Batch size":
            case "Batch pos":
            case "Steps":
                if (elements.Count != 1)
                {
                    throw new Exception($"Error in assumption in ProcessingTag: {tag}");
                }
                return elements[0];
            default:
                LogNewLastLineParam(tag);
                reject = true;
                return "";
        }

    }

    private static readonly string NewLastLineParamsFile = "NewLastLineParams.txt";
    private static List<string>? newLastLineParams = null;
    private static void LogNewLastLineParam(string tag)
    {
        if (newLastLineParams == null)
        {
            if (File.Exists(NewLastLineParamsFile))
            {
                newLastLineParams = File.ReadAllLines(NewLastLineParamsFile).ToList();
            }
            else
            {
                newLastLineParams = new List<string>();
            }
        }

        if (!newLastLineParams.Contains(tag))
        {
            newLastLineParams.Add(tag);
            File.WriteAllLines(NewLastLineParamsFile, newLastLineParams);
        }
    }

    private static readonly string UnknownModelFile = "UnknownModelFile.txt";
    private static List<string>? unknownModels = null;
    private static void LogNewUnknownModelFile(string tag)
    {
        if (unknownModels == null)
        {
            if (File.Exists(UnknownModelFile))
            {
                unknownModels = File.ReadAllLines(UnknownModelFile).ToList();
            }
            else
            {
                unknownModels = new List<string>();
            }
        }

        if (!unknownModels.Contains(tag))
        {
            unknownModels.Add(tag);
            File.WriteAllLines(UnknownModelFile, unknownModels);
        }
    }

    private static List<string> ExtractLastLineList(StreamReader sr)
    {
        char c = (char)sr.Read();
        if (c != '"')
        {
            throw new Exception("HashList not formatted correctly");
        }

        string lora = ReadToCharacter('"', sr);
        string[] loras = lora.Split(',');
        List<string> lorasList = new List<string>();
        foreach (string l in loras)
        {
            if (l.Contains(':'))
            {
                string[] lsplit = l.Split(':');
                lorasList.Add($"{lsplit[0].Trim()}-{lsplit[1].Trim()}");
            }
            else
            {
                lorasList.Add(l.Trim());
            }
        }
        if (sr.Peek() == (int)',')
        {
            sr.Read();
        }

        return lorasList;
    }

    private static string ReadToNextTag(StreamReader sr)
    {
        const char tag = ',';
        return ReadToCharacter(tag, sr, true);
    }

    private static string ReadNextTag(StreamReader sr)
    {
        const char tag = ':';
        return ReadToCharacter(tag, sr);
    }

    private static string ReadToCharacter(char c, StreamReader sr, bool orEol = false)
    {
        string r = "";
        while (!sr.EndOfStream)
        {
            char next = (char)sr.Read();
            if (next == c)
            {
                return r;
            }
            r += next;
        }
        if (orEol)
        {
            return r;
        }
        throw new Exception("End of Stream reached");
    }

    private static List<string> ParseTags(string tags, string? preface = null)
    {
        tags = Regex.Replace(tags, @"<[^>]*>", ",");

        List<string> splitTags = Regex.Split(tags, @"[\n,]").ToList();
        splitTags = splitTags.Where(s => !String.IsNullOrWhiteSpace(s))
            .Select(s => s.Trim()).ToList();

        splitTags = splitTags.SelectMany(potentialTag => SplitByChar('(', potentialTag)).ToList();
        splitTags = splitTags.SelectMany(potentialTag => SplitByChar(')', potentialTag)).ToList();
        splitTags = splitTags.SelectMany(potentialTag => SplitByChar('[', potentialTag)).ToList();
        splitTags = splitTags.SelectMany(potentialTag => SplitByChar(']', potentialTag)).ToList();
        splitTags = splitTags.SelectMany(potentialTag => SplitByChar('{', potentialTag)).ToList();
        splitTags = splitTags.SelectMany(potentialTag => SplitByChar('}', potentialTag)).ToList();

        List<string> parsedTags = new List<string>();
        foreach (string potentialTag in splitTags)
        {
            string tag = potentialTag;
            if (tag.Contains(':'))
            {
                tag = tag.Substring(0, tag.IndexOf(':'));
            }

            tag = tag.Trim();
            if (tag.Length > 0)
            {
                parsedTags.Add(tag);
            }
        }

        parsedTags = parsedTags.Distinct().Select(s => $"{PrefaceString(preface)}{s.ToLower()}").ToList();
        return parsedTags;
    }

    private static string PrefaceString(string? preface)
    {
        if (preface == null)
        {
            return "";
        }
        return $"{preface}:";
    }

    private static List<string> SplitByChar(char split, string potentialTag)
    {
        List<string> processedTags = new List<string>();

        int splitloc = potentialTag.IndexOf(split);
        if (splitloc != -1 && (splitloc == 0 || potentialTag[splitloc - 1] != '\\'))
        {
            int currentIndex = 0;
            while (currentIndex < potentialTag.Length)
            {
                int nextOccurrence = potentialTag.IndexOf(split, currentIndex);
                if (nextOccurrence == -1)
                {
                    string remainingTag = potentialTag.Substring(currentIndex);
                    remainingTag = remainingTag.Trim();
                    if (remainingTag.Length > 0)
                    {
                        processedTags.Add(remainingTag);
                    }
                    break;
                }
                else
                {
                    string tag = potentialTag.Substring(currentIndex, nextOccurrence - currentIndex);
                    tag = tag.Trim();
                    if (tag.Length > 0)
                    {
                        processedTags.Add(tag);
                    }
                    currentIndex = nextOccurrence + 1;
                }
            }
        }
        else
        {
            processedTags.Add(potentialTag);
        }
        return processedTags;
    }
}

public class RejectException : Exception
{
    public enum RejectReason
    {
        InvalidFileType,
        FileDoesNotExist,
        CouldNotReadMetadata,
        CouldNotFindModelNameFromHash,
        NewLastRowValue,
        Other
    }

    private readonly RejectReason reason;

    public RejectReason Reason => reason;

    public RejectException(RejectReason reason)
    {
        this.reason = reason;
    }

    public RejectException(RejectReason reason, string message)
        : base(message)
    {
        this.reason = reason;
    }

    public RejectException(RejectReason reason, string message, Exception inner)
        : base(message, inner)
    {
        this.reason = reason;
    }

    public string GetRejectFolder()
    {
        switch (reason)
        {
            case RejectReason.InvalidFileType:
                return "invalidType";
            case RejectReason.FileDoesNotExist:
                return "doesNotExist";
            case RejectReason.CouldNotReadMetadata:
                return "metadata";
            case RejectReason.CouldNotFindModelNameFromHash:
                return "modelName";
            case RejectReason.NewLastRowValue:
                return "lastRow";
            default:
                return "";
        }
    }
}
