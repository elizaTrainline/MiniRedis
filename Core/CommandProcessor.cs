using MiniRedis.Infra;

namespace MiniRedis.Core;

public sealed class CommandProcessor
{
    private readonly KeyValueStore _store;
    private readonly Persistence _persistence;

    public CommandProcessor(KeyValueStore store, Persistence persistence)
    { 
        _store = store; 
        _persistence = persistence; 
    }

    public string Process(string line)
    {
        var parts = ArgSplitter.Split(line);
        if (parts.Count == 0) 
            return "(error) empty";
        
        var cmd = parts[0].ToUpperInvariant();

        return cmd switch
        {
            "PING" => "PONG",
            "SET" => DoSet(parts),
            "GET" => DoGet(parts),
            "DEL" => DoDel(parts),
            "EXPIRE" => DoExpire(parts),
            "TTL" => DoTtl(parts),
            "INCR" => DoIncr(parts),
            "KEYS" => DoKeys(),
            "FLUSHALL" => DoFlushAll(),
            "SAVE" => DoSave(),
            _ => $"(error) unknown command '{cmd}'"
        };
    }

    private string DoSet(IReadOnlyList<string> parts)
    {
        if (parts.Count < 3) 
            return "(error) SET key value [EX seconds]";

        var key = parts[1];
        var value = parts[2];
        DateTimeOffset? expiry = null;

        if (parts.Count >= 5 && parts[3].ToUpperInvariant() == "EX" && int.TryParse(parts[4], out var s))
        expiry = DateTimeOffset.UtcNow.AddSeconds(s);
        _store.Set(key, value, expiry);
        return "OK";
    }

    private string DoGet(IReadOnlyList<string> parts)
    {
        if (parts.Count < 2) 
            return "(error) GET key";
        return _store.TryGet(parts[1], out var val) ? val! : "(nil)";
    }

    private string DoDel(IReadOnlyList<string> parts)
    {
        if (parts.Count < 2) 
            return "(error) DEL key";
        return _store.Del(parts[1]) ? "(integer) 1" : "(integer) 0";
    }

    private string DoExpire(IReadOnlyList<string> parts)
    {
        if (parts.Count < 3) 
            return "(error) EXPIRE key seconds";
        if (!int.TryParse(parts[2], out var s)) 
            return "(error) seconds must be int";
        return _store.Expire(parts[1], TimeSpan.FromSeconds(s)) ? "(integer) 1" : "(integer) 0";
    }
     
    private string DoTtl(IReadOnlyList<string> parts)
    {
        if (parts.Count < 2) 
            return "(error) TTL key";
        var ttl = _store.Ttl(parts[1]);
        return ttl switch
        {
            null => "(integer) -2",
            TimeSpan t when t == TimeSpan.MaxValue => "(integer) -1",
            TimeSpan t => $"(integer) {(int)Math.Ceiling(t.TotalSeconds)}"
        };
    }

    private string DoIncr(IReadOnlyList<string> parts)
    {
        if (parts.Count < 2) 
            return "(error) INCR key";
        var result = _store.Incr(parts[1]);
        return result.IsError ? result.ErrorMessage! : $"(integer) {result.Value}";
    }

    private string DoKeys() => _store.Keys().Any() ? string.Join(' ', _store.Keys()) : "(empty)";

    private string DoFlushAll()
    { 
        _store.Clear(); 
        return "OK"; 
    }

    private string DoSave() => _persistence.TrySave(_store.Snapshot()) is { } n ? $"Saved {n} keys" : "(error) save failed";
}