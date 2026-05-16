namespace PutridParrot.DistributedRateLimiter.RateLimiting;

public interface IDistributedRateLimiter
{
    Task<DistributedRateLimitLease> AcquireAsync(
        string key,
        int permitLimit,
        TimeSpan window,
        CancellationToken cancellationToken = default);
}