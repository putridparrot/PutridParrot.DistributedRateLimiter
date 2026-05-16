# PutridParrot.DistributedRateLimiter

A .NET 10 distributed rate limiting solution with:

- **`PutridParrot.DistributedRateLimiter`**: reusable library (Redis-backed)
- **`PutridParrot.DistributedRateLimiter.Sample`**: sample ASP.NET Core host demonstrating usage

## Prerequisites

- .NET SDK 10
- Redis (local or remote)

## Project Structure

- `PutridParrot.DistributedRateLimiter`
  - `RateLimiting/IDistributedRateLimiter.cs`
  - `RateLimiting/RedisDistributedRateLimiter.cs`
  - `RateLimiting/IRedisRateCounterStore.cs`
  - `RateLimiting/RedisRateCounterStore.cs`
  - `RateLimiting/DistributedRateLimitingOptions.cs`
- `PutridParrot.DistributedRateLimiter.Sample`
  - API host configuration in `Program.cs`
  - sample endpoint: `GET /limited`

## Configuration

Configure in `appsettings.json` (or environment-specific settings):

```json
{
  "DistributedRateLimiting": {
	"RedisConnectionString": "localhost:6379",
	"PermitLimit": 5,
	"WindowSeconds": 30,
	"KeyPrefix": "drl"
  }
}
```

### Settings

- `RedisConnectionString`: Redis endpoint/connection string
- `PermitLimit`: maximum requests allowed per window
- `WindowSeconds`: fixed window size in seconds
- `KeyPrefix`: Redis key prefix

## How it works

The limiter uses Redis atomic increment + key expiration:

1. Increment counter for a key (for example, per client IP).
2. Apply TTL when the key is first created.
3. If counter is above `PermitLimit`, request is rejected with `429`.

This makes it safe to run across multiple app instances.

## Using the library in your own app

1. Add a project/package reference to `PutridParrot.DistributedRateLimiter`.
2. Register options and services in DI:

```csharp
using PutridParrot.DistributedRateLimiter.RateLimiting;
using StackExchange.Redis;
using Microsoft.Extensions.Options;

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
```

3. Acquire a lease per request:

```csharp
var lease = await rateLimiter.AcquireAsync(
	key,
	options.PermitLimit,
	TimeSpan.FromSeconds(options.WindowSeconds),
	cancellationToken);

if (!lease.IsAcquired)
{
	return Results.StatusCode(StatusCodes.Status429TooManyRequests);
}
```

## Running the sample

From workspace root:

```bash
dotnet build PutridParrot.DistributedRateLimiter.slnx
dotnet run --project PutridParrot.DistributedRateLimiter/PutridParrot.DistributedRateLimiter.csproj
```

Then call:

- `GET /weatherforecast` (existing sample)
- `GET /limited` (rate-limited endpoint)

## Run with Docker Compose

From workspace root:

```bash
docker compose up --build
```

Endpoints:

- App: `http://localhost:8080/limited`
- Redis: `localhost:6379`

Stop:

```bash
docker compose down
```

## Deploy using Kubernetes YAML

Apply manifests in order:

```bash
kubectl apply -f k8s/namespace.yaml
kubectl apply -f k8s/redis.yaml
kubectl apply -f k8s/app.yaml
kubectl apply -f k8s/ingress.yaml
```

Notes:

- Update `image` in `k8s/app.yaml` to your pushed image.
- Ingress host defaults to `putridparrot-distributed-rate-limiter.local`.

## Deploy using Helm

Install:

```bash
helm install drl ./helm/putridparrot-distributed-rate-limiter -n drl --create-namespace
```

Upgrade:

```bash
helm upgrade drl ./helm/putridparrot-distributed-rate-limiter -n drl
```

Example with image override and ingress enabled:

```bash
helm install drl ./helm/putridparrot-distributed-rate-limiter \
  -n drl --create-namespace \
  --set app.image.repository=myregistry.azurecr.io/putridparrot-distributed-rate-limiter \
  --set app.image.tag=1.0.0 \
  --set app.ingress.enabled=true
```

## `/limited` response behavior

When allowed (`200 OK`):

- `X-RateLimit-Limit`
- `X-RateLimit-Remaining`
- `X-RateLimit-Reset`

When blocked (`429 Too Many Requests`):

- Same headers as above
- `Retry-After`

## Notes

- Current sample keying strategy is by remote IP (`HttpContext.Connection.RemoteIpAddress`).
- For production, consider keying by authenticated user, API key, tenant, or route as needed.
- For very high throughput scenarios, evaluate script caching and connection tuning.
