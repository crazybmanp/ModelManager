using System.IO;

namespace SDFileProcessor.Processor;

internal class FileProgress(FileInfo fileInfo)
{
    public FileInfo FileInfo { get; set; } = fileInfo;
    private ProcessingStatus status = ProcessingStatus.Unknown;
    private DateTime? lastStatusChange = null;

    public static readonly string[] ValidExtensions = [".png", ".jpg", ".jpeg"];
    public static readonly string[] DataExtensions = [".txt", ".json"];

    public FileProgress(FileProgressSerial serial) : this(new FileInfo(serial.Path))
    {
        status = ProcessingStatus.Deserialize(serial.Status);
        lastStatusChange = serial.LastStatusChange;
    }

    public void SetStatus(ProcessingStatus newStatus)
    {
        status = newStatus;
        lastStatusChange = DateTime.Now;
    }
    public ProcessingStatus GetStatus()
    {
        return status;
    }

    public DateTime? GetLastStatusChange()
    {
        return lastStatusChange;
    }

    public FileProgressSerial GetSerializable()
    {
        var test = new FileProgressSerial
        {
            Path = Path,
            Status = status.Value,
            LastStatusChange = lastStatusChange
        };
        return test;
    }

    public string Path => FileInfo.FullName;
    public string FileName => FileInfo.Name;

    private void CheckUnknownFile()
    {
        if (ValidExtensions.Contains(FileInfo.Extension))
        {
            CheckProcessingStatus();
        }
        else if (DataExtensions.Contains(FileInfo.Extension))
        {
            SetStatus(ProcessingStatus.DataFile);
        }
        else
        {
            SetStatus(ProcessingStatus.NotAnImage);
        }
    }

    public void CheckProcessingStatus()
    {
        string tagFileLoc = Path + @".Tags.txt";
        string notesFileLoc = Path + @".Notes.txt";

        FileInfo tagFile = new FileInfo(tagFileLoc);
        FileInfo notesFile = new FileInfo(notesFileLoc);

        if (tagFile.Exists && notesFile.Exists)
        {
            SetStatus(ProcessingStatus.Processed);
        }
        else if (!tagFile.Exists && !notesFile.Exists)
        {
            SetStatus(ProcessingStatus.Unprocessed);
        }
        else
        {
            SetStatus(ProcessingStatus.Unrecoverable);
        }
    }

    public bool IsProcessable()
    {
        if (status == ProcessingStatus.Unknown)
        {
            CheckUnknownFile();
        }

        if (status == ProcessingStatus.Unprocessed)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    public override string ToString()
    {
        return $"{FileInfo.Name} - {status}";
    }
}
