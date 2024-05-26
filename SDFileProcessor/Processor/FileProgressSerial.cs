namespace SDFileProcessor.Processor;

internal class FileProgressSerial
{
    public required string Path { get; init; }
    public required string Status { get; init; }
    public DateTime? LastStatusChange { get; init; }
}
