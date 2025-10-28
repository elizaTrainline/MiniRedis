using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MiniRedis.Core;
using MiniRedis.Infra;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<KeyValueStore>();
builder.Services.AddSingleton(new Persistence(Path.Combine(AppContext.BaseDirectory, "miniredis.json")));
builder.Services.AddSingleton<CommandProcessor>();

var app = builder.Build();

{
    var store = app.Services.GetRequiredService<KeyValueStore>();
    var persistence = app.Services.GetRequiredService<Persistence>();

    var loaded = await persistence.TryLoadAsync();
    if (loaded is not null)
    {
        foreach (var kv in loaded)
        {
            var k = kv.Key;
            var v = kv.Value;
            if (!v.IsExpired) store.Set(k, v.Value, v.ExpiryUtc);
        }
    }
    store.StartSweeper(TimeSpan.FromSeconds(1));
}

app.MapMiniRedisEndpoints();

app.Run();