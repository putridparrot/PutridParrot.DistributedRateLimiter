namespace PutridParrot.DistributedRateLimiter.RateLimiting;

public sealed class RedisDistributedRateLimiter(IRedisRateCounterStore rateCounterStore) : IDistributedRateLimiter
{
    public async Task<DistributedRateLimitLease> AcquireAsync(
        string key,
        int permitLimit,
        TimeSpan window,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Rate limit key cannot be null or whitespace.", nameof(key));
        }

        if (permitLimit <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(permitLimit), "Permit limit must be greater than zero.");
        }

        if (window <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(window), "Window must be greater than zero.");
        }

        var (currentCount, ttl) = await rateCounterStore
            .IncrementAsync(key, window, cancellationToken)
            .ConfigureAwait(false);

        var current = (int)Math.Min(int.MaxValue, currentCount);
        var acquired = current <= permitLimit;
        var remaining = Math.Max(0, permitLimit - current);
        var retryAfter = acquired ? TimeSpan.Zero : ttl;

        return new DistributedRateLimitLease(
            acquired,
            permitLimit,
            current,
            remaining,
            retryAfter,
            DateTimeOffset.UtcNow.Add(ttl));
    }
}