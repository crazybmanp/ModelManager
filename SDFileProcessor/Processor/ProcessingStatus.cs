using System.ComponentModel.Design.Serialization;
using System.Text.Json.Serialization;

namespace SDFileProcessor.Processor;
public class ProcessingStatus
{
    private ProcessingStatus(string value) { Value = value; }

    public static ProcessingStatus Deserialize(string value)
    {
        //check if the value matches one of the valid statuses
        if(value == "UNKNOWN") return Unknown;
        if(value == "UNPROCESSED") return Unprocessed;
        if(value == "PROCESSING") return Processing;
        if(value == "PROCESSED") return Processed;
        if(value == "DATAFILE") return DataFile;
        if(value == "NOTIMAGE") return NotAnImage;
        if(value == "NOMETADATA") return NoMetadata;
        if(value == "UNRECOVERABLE") return Unrecoverable;
        throw new InvalidOperationException("Invalid ProcessingStatus value");
    }

    public string Value { get; private set; }

    public static ProcessingStatus Unknown { get { return new ProcessingStatus("UNKNOWN"); } }
    public static ProcessingStatus Unprocessed { get { return new ProcessingStatus("UNPROCESSED"); } }
    public static ProcessingStatus Processing { get { return new ProcessingStatus("PROCESSING"); } }
    public static ProcessingStatus Processed { get { return new ProcessingStatus("PROCESSED"); } }
    public static ProcessingStatus DataFile { get { return new ProcessingStatus("DATAFILE"); } }
    public static ProcessingStatus NotAnImage { get { return new ProcessingStatus("NOTIMAGE"); } }
    public static ProcessingStatus NoMetadata { get { return new ProcessingStatus("NOMETADATA"); } }
    public static ProcessingStatus Unrecoverable { get { return new ProcessingStatus("UNRECOVERABLE"); } }

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

