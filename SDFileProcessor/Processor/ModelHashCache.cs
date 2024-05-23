using System.IO;
using System.Text.Json;

namespace SDFileProcessor.Processor;

public class ModelHashCache
{
    private const string CacheLocation = "modelHashCache.json";
    private Dictionary<string, string> cache = new Dictionary<string, string>();

    public ModelHashCache()
    {
        LoadCacheFromDisk();
    }

    public bool getModelNameFromHash(ref ParsedMetadata pmr)
    {
        if (cache.TryGetValue(pmr.modelHash, out string? modelName))
        {
            pmr.modelName = modelName;
            return true;
        }
        return false;
    }

    public void AddModelNameForHash(ParsedMetadata pmr)
    {
        if (cache.ContainsKey(pmr.modelHash) || pmr.modelName == null)
        {
            return;
        }
        cache.Add(pmr.modelHash, pmr.modelName);
        SaveCacheToDisk();
    }

    private void SaveCacheToDisk()
    {
        string json = JsonSerializer.Serialize(cache);
        File.WriteAllText(CacheLocation, json);
    }

    private void LoadCacheFromDisk()
    {
        if (File.Exists(CacheLocation))
        {
            string json = File.ReadAllText(CacheLocation);
            try
            {
                cache = JsonSerializer.Deserialize<Dictionary<string, string>>(json) ??
                        new Dictionary<string, string>();
                return;
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error loading cache from disk: {e.Message}");
                cache = new Dictionary<string, string>();
            }
        }
        cache = new Dictionary<string, string>();
    }
}