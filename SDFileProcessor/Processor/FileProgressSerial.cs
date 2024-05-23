using System.Text.Json.Serialization;

namespace SDFileProcessor.Processor;

internal class FileProgressSerial
{
    public required string path { get; set; }
    public required string status { get; set; }
    public DateTime? LastStatusChange { get; set; }
}
