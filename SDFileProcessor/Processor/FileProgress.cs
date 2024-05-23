using System.IO;

namespace SDFileProcessor.Processor;

internal class FileProgress
{
    public FileInfo FileInfo { get; set; }
    private ProcessingStatus status = ProcessingStatus.Unknown;
    private DateTime? LastStatusChange = null;

    public static readonly string[] ValidExtensions = [".png", ".jpg", ".jpeg"];
    public static readonly string[] DataExtensions = [".txt", ".json"];

    public FileProgress(FileInfo fileInfo)
    {
        FileInfo = fileInfo;
    }

    public FileProgress(FileProgressSerial serial)
    {
        FileInfo = new FileInfo(serial.path);
        status = ProcessingStatus.Deserialize(serial.status);
        LastStatusChange = serial.LastStatusChange;
    }

    public void SetStatus(ProcessingStatus status)
    {
        this.status = status;
        LastStatusChange = DateTime.Now;
    }
    public ProcessingStatus GetStatus()
    {
        return status;
    }

    public DateTime? GetLastStatusChange()
    {
        return LastStatusChange;
    }

    public FileProgressSerial GetSerializable()
    {
        var test = new FileProgressSerial
        {
            path = Path,
            status = status.Value,
            LastStatusChange = LastStatusChange
        };
        return test;
    }

    public string Path => FileInfo.FullName;
    public string fileName => FileInfo.Name;

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

    public bool isProcessable()
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
