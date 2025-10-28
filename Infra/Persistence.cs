using System.Text.Json;
using MiniRedis.Core;

namespace MiniRedis.Infra;

public sealed class Persistence(string path)
{
    private readonly string _path = path;

    public int? TrySave(IReadOnlyDictionary<string, CacheItem> snapshot)
    {
        try
        {
            var json = JsonSerializer.Serialize(snapshot, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_path, json);
            return snapshot.Count;
        }
        catch
        {
            return null; 
        }
    }


    public async Task<Dictionary<string, CacheItem>?> TryLoadAsync()
    {
        try
        {
            if (!File.Exists(_path)) return new();
            var json = await File.ReadAllTextAsync(_path);
            return JsonSerializer.Deserialize<Dictionary<string, CacheItem>>(json) ?? new();
        }
        catch
        {
            return null; 
        }
    }
}