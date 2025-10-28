using System;
using MiniRedis.Core;
using MiniRedis.Infra;
using FluentAssertions;
using Xunit;

public class CommandProcessorTests
{
    private static CommandProcessor NewProcessor(string? savePath = null)
    {
        var store = new KeyValueStore();
        var persistence = new Persistence(savePath ?? System.IO.Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid() + ".json"));
        return new CommandProcessor(store, persistence);
    }

    [Fact]
    public void Ping_returns_pong()
    {
        var p = NewProcessor();
        p.Process("PING").Should().Be("PONG");
    }

    [Fact]
    public void Set_and_get_work()
    {
        var p = NewProcessor();
        p.Process("SET a 123").Should().Be("OK");
        p.Process("GET a").Should().Be("123");
    }

    [Fact]
    public void Get_missing_returns_nil()
    {
        var p = NewProcessor();
        p.Process("GET nope").Should().Be("(nil)");
    }

    [Fact]
    public void Del_returns_integer_count()
    {
        var p = NewProcessor();
        p.Process("SET a 1");
        p.Process("DEL a").Should().Be("(integer) 1");
        p.Process("DEL a").Should().Be("(integer) 0");
    }

    [Fact]
    public void Expire_and_ttl_roundtrip()
    {
        var p = NewProcessor();
        p.Process("SET a 1");
        p.Process("EXPIRE a 5").Should().Be("(integer) 1");
        var ttl = p.Process("TTL a");
        ttl.Should().StartWith("(integer) ");
    }

    [Fact]
    public void Incr_success_and_error_paths()
    {
        var p = NewProcessor();

        p.Process("INCR a").Should().Be("(integer) 1");
        p.Process("INCR a").Should().Be("(integer) 2");

        p.Process("SET b hello");
        var err = p.Process("INCR b");
        err.Should().StartWith("(error)");
    }

    [Fact]
    public void Keys_and_flushall()
    {
        var p = NewProcessor();
        p.Process("SET a 1");
        p.Process("SET b 2");

        var keys = p.Process("KEYS");
        keys.Split(' ').Should().BeSupersetOf(new[] { "a", "b" });

        p.Process("FLUSHALL").Should().Be("OK");
        p.Process("KEYS").Should().Be("(empty)");
    }

    [Fact]
    public void Save_writes_snapshot_and_reports_count()
    {
        var tempPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid() + ".json");
        var store = new KeyValueStore();
        var persistence = new Persistence(tempPath);
        var p = new CommandProcessor(store, persistence);

        p.Process("SET a 1");
        var res = p.Process("SAVE");
        res.Should().StartWith("Saved ");
        System.IO.File.Exists(tempPath).Should().BeTrue();

        // cleanup
        System.IO.File.Delete(tempPath);
    }
}
