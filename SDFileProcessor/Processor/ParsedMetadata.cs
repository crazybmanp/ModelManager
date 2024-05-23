namespace SDFileProcessor.Processor;

public class ParsedMetadata
{
    public required List<string> parameters;
    public required List<string> negativeParemeters;
    public string? modelName;
    public required string modelHash;
    public required string cfg;
    public required string sampler;
    public required string? ScheduleType;
    public required string seed;
    public required string steps;
    public string? loras;

    public string? hiresUpscale;
    public string? hiresSteps;
    public string? hiresUpscaler;
    public string? highresScheduleType;
    public string? vaeHash;
    public string? vae;
    public string? variationSeed;
    public string? variationSeedStrength;
    public string? batchsize;
    public string? batchpos;

    public required string prompt;
    public required string negativePrompt;
    public required string lastLine;
}