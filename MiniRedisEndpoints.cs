using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using MiniRedis.Core;
using MiniRedis.Infra;

public static class MiniRedisEndpoints
{
    public static void MapMiniRedisEndpoints(this WebApplication app)
    {
        app.MapGet("/ping", () => "PONG");

        app.MapGet("/get/{key}", (string key, KeyValueStore store)
            => store.TryGet(key, out var v) ? Results.Ok(v) : Results.NotFound());

        app.MapPost("/set/{key}", (string key, string value, int? ex, KeyValueStore store) =>
        {
            DateTimeOffset? expiry = ex.HasValue ? DateTimeOffset.UtcNow.AddSeconds(ex.Value) : null;
            store.Set(key, value, expiry);
            return Results.Ok("OK");
        });

        app.MapPost("/incr/{key}", (string key, KeyValueStore store) =>
        {
            var r = store.Incr(key);
            return r.IsError ? Results.BadRequest(r.ErrorMessage) : Results.Ok(r.Value);
        });

        app.MapPost("/expire/{key}", (string key, int seconds, KeyValueStore store)
            => store.Expire(key, TimeSpan.FromSeconds(seconds)) ? Results.Ok(1) : Results.Ok(0));

        app.MapGet("/ttl/{key}", (string key, KeyValueStore store) =>
        {
            var ttl = store.Ttl(key);
            if (ttl is null) return Results.Ok(-2);
            if (ttl == TimeSpan.MaxValue) return Results.Ok(-1);
            return Results.Ok((int)Math.Ceiling(ttl.Value.TotalSeconds));
        });

        app.MapGet("/keys", (KeyValueStore store) => Results.Ok(store.Keys()));

        app.MapPost("/save", (KeyValueStore store, Persistence p)
            => Results.Ok(p.TrySave(store.Snapshot()) ?? 0));
    }
}