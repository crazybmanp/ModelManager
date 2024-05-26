namespace SDFileProcessor.Processor;
public class ProcessingStatus
{
    private ProcessingStatus(string value) { Value = value; }

    public static ProcessingStatus Deserialize(string value)
    {
        switch (value)
        {
            //check if the value matches one of the valid statuses
            case "UNKNOWN":
                return Unknown;
            case "UNPROCESSED":
                return Unprocessed;
            case "PROCESSING":
                return Processing;
            case "PROCESSED":
                return Processed;
            case "DATAFILE":
                return DataFile;
            case "NOTIMAGE":
                return NotAnImage;
            case "NOMETADATA":
                return NoMetadata;
            case "UNRECOVERABLE":
                return Unrecoverable;
            default:
                throw new InvalidOperationException("Invalid ProcessingStatus value");
        }
    }

    public string Value { get; private set; }

    public static ProcessingStatus Unknown => new ProcessingStatus("UNKNOWN");
    public static ProcessingStatus Unprocessed => new ProcessingStatus("UNPROCESSED");
    public static ProcessingStatus Processing => new ProcessingStatus("PROCESSING");
    public static ProcessingStatus Processed => new ProcessingStatus("PROCESSED");
    public static ProcessingStatus DataFile => new ProcessingStatus("DATAFILE");
    public static ProcessingStatus NotAnImage => new ProcessingStatus("NOTIMAGE");
    public static ProcessingStatus NoMetadata => new ProcessingStatus("NOMETADATA");
    public static ProcessingStatus Unrecoverable => new ProcessingStatus("UNRECOVERABLE");

    public override string ToString()
    {
        return Value;
    }

    public override bool Equals(object? obj)
    {
        if(obj == null) return false;
        if(obj.GetType() != typeof(ProcessingStatus)) return false;
        return ((ProcessingStatus)obj).Value == Value;
    }

    public static bool operator ==(ProcessingStatus x, ProcessingStatus y)
    {
        return x.Equals(y);
    }

    public static bool operator !=(ProcessingStatus x, ProcessingStatus y)
    {
        return !x.Equals(y);
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }
}

