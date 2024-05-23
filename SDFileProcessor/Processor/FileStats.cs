namespace SDFileProcessor.Processor;

public class FileStats
{
    public int TotalFiles { get; set; }
    public int UnprocessedFiles { get; set; }
    public int ProcessingFiles { get; set; }
    public int ProcessedFiles { get; set; }
    public int IgnoredFiles { get; set; }
    public int FailedFiles { get; set; }

    public override string ToString()
    {
        //each property on one line apiece
        return $"Total Files: {TotalFiles}\n" +
            $"Unprocessed Files: {UnprocessedFiles}\n" +
            $"Processing Files: {ProcessingFiles}\n" +
            $"Processed Files: {ProcessedFiles}\n" +
            $"Ignored Files: {IgnoredFiles}\n" +
            $"Failed Files: {FailedFiles}";
    }
}
