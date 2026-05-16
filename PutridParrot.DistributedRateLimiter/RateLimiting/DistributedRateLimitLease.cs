namespace PutridParrot.DistributedRateLimiter.RateLimiting;

public sealed record DistributedRateLimitLease(
    bool IsAcquired,
    int Limit,
    int CurrentCount,
    int Remaining,
    TimeSpan RetryAfter,
    DateTimeOffset WindowEndsAtUtc);