using Microsoft.Extensions.Options;
using PutridParrot.DistributedRateLimiter.RateLimiting;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services
    .AddOptions<DistributedRateLimitingOptions>()
    .Bind(builder.Configuration.GetSection(DistributedRateLimitingOptions.SectionName));

builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    var options = sp.GetRequiredService<IOptions<DistributedRateLimitingOptions>>().Value;
    return ConnectionMultiplexer.Connect(options.RedisConnectionString);
});
builder.Services.AddSingleton<IRedisRateCounterStore, RedisRateCounterStore>();
builder.Services.AddSingleton<IDistributedRateLimiter, RedisDistributedRateLimiter>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();


app.MapGet("/limited", async (HttpContext context, IDistributedRateLimiter rateLimiter, IOptions<DistributedRateLimitingOptions> optionsAccessor, CancellationToken cancellationToken) =>
{
    var options = optionsAccessor.Value;
    var keyId = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    var key = $"{options.KeyPrefix}:{keyId}";

    var lease = await rateLimiter.AcquireAsync(
        key,
        options.PermitLimit,
        TimeSpan.FromSeconds(options.WindowSeconds),
        cancellationToken);

    context.Response.Headers.Append("X-RateLimit-Limit", lease.Limit.ToString());
    context.Response.Headers.Append("X-RateLimit-Remaining", lease.Remaining.ToString());
    context.Response.Headers.Append("X-RateLimit-Reset", lease.WindowEndsAtUtc.ToUnixTimeSeconds().ToString());

    if (!lease.IsAcquired)
    {
        context.Response.Headers.Append("Retry-After", Math.Max(1, (int)Math.Ceiling(lease.RetryAfter.TotalSeconds)).ToString());
        return Results.StatusCode(StatusCodes.Status429TooManyRequests);
    }

    return Results.Ok(new
    {
        Message = "Request allowed",
        lease.Limit,
        lease.CurrentCount,
        lease.Remaining,
        lease.WindowEndsAtUtc
    });
})
.WithName("GetLimited");

app.Run();
