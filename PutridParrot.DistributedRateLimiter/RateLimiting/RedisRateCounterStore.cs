using StackExchange.Redis;

namespace PutridParrot.DistributedRateLimiter.RateLimiting;

public sealed class RedisRateCounterStore(IConnectionMultiplexer connectionMultiplexer) : IRedisRateCounterStore
{
    public async Task<(long CurrentCount, TimeSpan TimeToLive)> IncrementAsync(
        string key,
        TimeSpan window,
        CancellationToken cancellationToken = default)
    {
        var db = connectionMultiplexer.GetDatabase();

        var ttlMs = (long)Math.Ceiling(window.TotalMilliseconds);
        if (ttlMs <= 0)
        {
            ttlMs = 1;
        }

        var script = @"
local current = redis.call('INCR', KEYS[1])
if current == 1 then
    redis.call('PEXPIRE', KEYS[1], ARGV[1])
end
local ttl = redis.call('PTTL', KEYS[1])
if ttl < 0 then
    ttl = ARGV[1]
end
return {current, ttl}
";

        var result = (RedisResult[]?)await db.ScriptEvaluateAsync(
            script,
            [key],
            [ttlMs]).ConfigureAwait(false);

        if (result is null || result.Length < 2)
        {
            throw new InvalidOperationException("Redis script did not return the expected result payload.");
        }

        var currentCount = (long)result[0];
        var pttl = (long)result[1];

        if (pttl < 0)
        {
            pttl = ttlMs;
        }

        return (currentCount, TimeSpan.FromMilliseconds(pttl));
    }
}