using System.Collections.Concurrent;

namespace MiniRedis.Core;

public sealed class KeyValueStore
{
    private readonly ConcurrentDictionary<string, CacheItem> _map = new();

    public int Count => _map.Count;

    public void Set(string key, string value, DateTimeOffset? expiryUtc) => _map[key] = new CacheItem(value, expiryUtc);

    public bool TryGet(string key, out string? value)
    {
        value = null;
        if (!_map.TryGetValue(key, out var item)) return false;
        if (item.IsExpired) 
        { 
            _map.TryRemove(key, out _); 
            return false; 
        }

        value = item.Value; 
        return true;
    }

    public bool Del(string key) => _map.TryRemove(key, out _);

    public bool Expire(string key, TimeSpan ttl)
    {
        if (!_map.TryGetValue(key, out var item)) return false;
        _map[key] = item with { ExpiryUtc = DateTimeOffset.UtcNow.Add(ttl) };
        return true;
    }

    public TimeSpan? Ttl(string key)
    {
        if (!_map.TryGetValue(key, out var item)) return null;
        if (item.ExpiryUtc is null) return TimeSpan.MaxValue;
        var remaining = item.ExpiryUtc.Value - DateTimeOffset.UtcNow;
        return remaining < TimeSpan.Zero ? null : remaining;
    }

    public IncrResult Incr(string key)
    {
        var nowItem = _map.AddOrUpdate(key,
        addValueFactory: _ => new CacheItem("1", null),
        updateValueFactory: (_, item) =>
        {
            if (item.IsExpired) return new CacheItem("1", null);
            if (!long.TryParse(item.Value, out var i)) return item with { Value = "(error) value is not an integer" };
            return new CacheItem((i + 1).ToString(), item.ExpiryUtc);
        });

        if (nowItem.Value.StartsWith("(error)"))
            return IncrResult.Error(nowItem.Value);
        return IncrResult.Success(long.Parse(nowItem.Value));
    }

    public IEnumerable<string> Keys() => _map.Keys;

    public void Clear() => _map.Clear();

    public IReadOnlyDictionary<string, CacheItem> Snapshot() => 
        _map.Where(kv => !kv.Value.IsExpired).ToDictionary(kv => kv.Key, kv => kv.Value);

    public CancellationTokenSource StartSweeper(TimeSpan period)
    {
        var cts = new CancellationTokenSource();
        _ = Task.Run(async () =>
        {
            while (!cts.IsCancellationRequested)
            {
                foreach (var kv in _map)
                    if (kv.Value.IsExpired) _map.TryRemove(kv.Key, out _);
                try { await Task.Delay(period, cts.Token); } catch { break; }
            }
        });
        return cts;
    }
}