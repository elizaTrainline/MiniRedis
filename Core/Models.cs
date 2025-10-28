namespace MiniRedis.Core;

public record CacheItem(string Value, DateTimeOffset? ExpiryUtc)
{ 
    public bool IsExpired => ExpiryUtc is not null && DateTimeOffset.UtcNow >= ExpiryUtc; 
}

public readonly record struct IncrResult(long? Value, bool IsError, string? ErrorMessage)
{
    public static IncrResult Success(long v) => new IncrResult(v, false, null);
    public static IncrResult Error(string e) => new IncrResult(null, true, e);
}