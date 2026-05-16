# putridparrot-distributed-rate-limiter Helm Chart

## Install

```bash
helm install drl ./helm/putridparrot-distributed-rate-limiter -n drl --create-namespace
```

## Upgrade

```bash
helm upgrade drl ./helm/putridparrot-distributed-rate-limiter -n drl
```

## Key values

- `app.image.repository`
- `app.image.tag`
- `app.replicaCount`
- `app.ingress.enabled`
- `redis.enabled`
- `redis.service.port`

## Example override

```bash
helm install drl ./helm/putridparrot-distributed-rate-limiter \
  -n drl --create-namespace \
  --set app.image.repository=myregistry.azurecr.io/putridparrot-distributed-rate-limiter \
  --set app.image.tag=1.0.0 \
  --set app.ingress.enabled=true
```
