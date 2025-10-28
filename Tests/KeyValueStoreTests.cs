using System;
using System.Linq;
using System.Threading.Tasks;
using MiniRedis.Core;
using FluentAssertions;
using Xunit;

public class KeyValueStoreTests
{
    [Fact]
    public void SetGet_roundtrip_without_expiry()
    {
        var store = new KeyValueStore();

        store.Set("a", "hello", expiryUtc: null);

        store.TryGet("a", out var v).Should().BeTrue();
        v.Should().Be("hello");
    }

    [Fact]
    public void Get_returns_nil_semantics_when_missing()
    {
        var store = new KeyValueStore();
        store.TryGet("nope", out var _).Should().BeFalse();
    }

    [Fact]
    public void Expire_and_ttl_behaviour()
    {
        var store = new KeyValueStore();
        store.Set("a", "1", null);

        store.Expire("a", TimeSpan.FromSeconds(5)).Should().BeTrue();

        var ttl = store.Ttl("a");
        ttl.Should().NotBeNull();
        ttl!.Value.TotalSeconds.Should().BePositive();
    }

    [Fact]
    public async Task Key_expires_after_time_passes()
    {
        var store = new KeyValueStore();
        store.Set("a", "1", DateTimeOffset.UtcNow.AddMilliseconds(100));

        await Task.Delay(150);
        store.TryGet("a", out _).Should().BeFalse();
    }

    [Fact]
    public void Incr_creates_and_increments_integers()
    {
        var store = new KeyValueStore();

        var r1 = store.Incr("cnt");
        r1.IsError.Should().BeFalse();
        r1.Value.Should().Be(1);

        var r2 = store.Incr("cnt");
        r2.IsError.Should().BeFalse();
        r2.Value.Should().Be(2);
    }

    [Fact]
    public void Incr_returns_error_when_value_is_not_integer()
    {
        var store = new KeyValueStore();
        store.Set("x", "hello", null);

        var r = store.Incr("x");
        r.IsError.Should().BeTrue();
        r.ErrorMessage.Should().Contain("not an integer");
    }

    [Fact]
    public void Snapshot_excludes_expired_keys()
    {
        var store = new KeyValueStore();
        store.Set("live", "1", null);
        store.Set("old", "1", DateTimeOffset.UtcNow.AddSeconds(-1));

        var snap = store.Snapshot();
        snap.Keys.Should().Contain("live");
        snap.Keys.Should().NotContain("old");
    }

    [Fact]
    public async Task Sweeper_removes_expired_keys()
    {
        var store = new KeyValueStore();
        store.Set("a", "1", DateTimeOffset.UtcNow.AddMilliseconds(50));

        using var cts = store.StartSweeper(TimeSpan.FromMilliseconds(25));
        await Task.Delay(150);

        store.Keys().Should().NotContain("a");
        cts.Cancel();
    }
}
