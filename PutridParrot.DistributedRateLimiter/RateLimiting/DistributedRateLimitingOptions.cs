namespace PutridParrot.DistributedRateLimiter.RateLimiting;

public sealed class DistributedRateLimitingOptions
{
    public const string SectionName = "DistributedRateLimiting";

    public string RedisConnectionString { get; set; } = "localhost:6379";

    public int PermitLimit { get; set; } = 10;

    public int WindowSeconds { get; set; } = 60;

    public string KeyPrefix { get; set; } = "ratelimit";
}