# Kubernetes Manifests

Apply in order:

```bash
kubectl apply -f k8s/namespace.yaml
kubectl apply -f k8s/redis.yaml
kubectl apply -f k8s/app.yaml
kubectl apply -f k8s/ingress.yaml
```

Notes:
- Update `image` in `k8s/app.yaml` to your registry image before deployment.
- Ingress assumes an NGINX ingress controller and local host mapping.
