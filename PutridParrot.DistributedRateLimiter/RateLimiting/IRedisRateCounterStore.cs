namespace PutridParrot.DistributedRateLimiter.RateLimiting;

public interface IRedisRateCounterStore
{
    Task<(long CurrentCount, TimeSpan TimeToLive)> IncrementAsync(
        string key,
        TimeSpan window,
        CancellationToken cancellationToken = default);
}