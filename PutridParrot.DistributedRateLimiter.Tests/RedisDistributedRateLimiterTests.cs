using PutridParrot.DistributedRateLimiter.RateLimiting;

namespace PutridParrot.DistributedRateLimiter.Tests;

public class RedisDistributedRateLimiterTests
{
    [Fact]
    public async Task AcquireAsync_Throws_WhenKeyIsNullOrWhitespace()
    {
        var store = new FakeStore((1, TimeSpan.FromSeconds(30)));
        var sut = new RedisDistributedRateLimiter(store);

        await Assert.ThrowsAsync<ArgumentException>(() => sut.AcquireAsync(" ", 5, TimeSpan.FromSeconds(30)));
    }

    [Fact]
    public async Task AcquireAsync_Throws_WhenPermitLimitIsInvalid()
    {
        var store = new FakeStore((1, TimeSpan.FromSeconds(30)));
        var sut = new RedisDistributedRateLimiter(store);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => sut.AcquireAsync("user:1", 0, TimeSpan.FromSeconds(30)));
    }

    [Fact]
    public async Task AcquireAsync_Throws_WhenWindowIsInvalid()
    {
        var store = new FakeStore((1, TimeSpan.FromSeconds(30)));
        var sut = new RedisDistributedRateLimiter(store);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => sut.AcquireAsync("user:1", 5, TimeSpan.Zero));
    }

    [Fact]
    public async Task AcquireAsync_CallsStoreWithExpectedArguments()
    {
        var ttl = TimeSpan.FromSeconds(12);
        var store = new FakeStore((3, ttl));
        var sut = new RedisDistributedRateLimiter(store);
        var token = new CancellationTokenSource().Token;

        await sut.AcquireAsync("ip:127.0.0.1", 5, TimeSpan.FromSeconds(30), token);

        Assert.Equal(1, store.CallCount);
        Assert.Equal("ip:127.0.0.1", store.LastKey);
        Assert.Equal(TimeSpan.FromSeconds(30), store.LastWindow);
        Assert.Equal(token, store.LastToken);
    }

    [Fact]
    public async Task AcquireAsync_ReturnsAcquiredLease_WhenWithinPermitLimit()
    {
        var store = new FakeStore((2, TimeSpan.FromSeconds(20)));
        var sut = new RedisDistributedRateLimiter(store);

        var lease = await sut.AcquireAsync("user:2", 5, TimeSpan.FromSeconds(30));

        Assert.True(lease.IsAcquired);
        Assert.Equal(5, lease.Limit);
        Assert.Equal(2, lease.CurrentCount);
        Assert.Equal(3, lease.Remaining);
        Assert.Equal(TimeSpan.Zero, lease.RetryAfter);
    }

    [Fact]
    public async Task AcquireAsync_ReturnsRejectedLease_WhenOverPermitLimit()
    {
        var ttl = TimeSpan.FromSeconds(17);
        var store = new FakeStore((6, ttl));
        var sut = new RedisDistributedRateLimiter(store);

        var lease = await sut.AcquireAsync("user:3", 5, TimeSpan.FromSeconds(30));

        Assert.False(lease.IsAcquired);
        Assert.Equal(5, lease.Limit);
        Assert.Equal(6, lease.CurrentCount);
        Assert.Equal(0, lease.Remaining);
        Assert.Equal(ttl, lease.RetryAfter);
    }

    [Fact]
    public async Task AcquireAsync_ClampsCurrentCountToIntMaxValue()
    {
        var store = new FakeStore(((long)int.MaxValue + 100, TimeSpan.FromSeconds(10)));
        var sut = new RedisDistributedRateLimiter(store);

        var lease = await sut.AcquireAsync("user:4", int.MaxValue, TimeSpan.FromSeconds(30));

        Assert.Equal(int.MaxValue, lease.CurrentCount);
    }

    private sealed class FakeStore((long currentCount, TimeSpan ttl) response) : IRedisRateCounterStore
    {
        private readonly (long currentCount, TimeSpan ttl) _response = response;

        public int CallCount { get; private set; }

        public string? LastKey { get; private set; }

        public TimeSpan LastWindow { get; private set; }

        public CancellationToken LastToken { get; private set; }

        public Task<(long CurrentCount, TimeSpan TimeToLive)> IncrementAsync(string key, TimeSpan window, CancellationToken cancellationToken = default)
        {
            CallCount++;
            LastKey = key;
            LastWindow = window;
            LastToken = cancellationToken;
            return Task.FromResult((_response.currentCount, _response.ttl));
        }
    }
}
