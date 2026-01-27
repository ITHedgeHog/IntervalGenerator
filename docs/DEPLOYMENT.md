# Deployment Guide

This guide covers deploying IntervalGenerator to Docker, Kubernetes, and using Garden.io for cloud-native deployments.

## Table of Contents

- [Docker Deployment](#docker-deployment)
- [Kubernetes Deployment](#kubernetes-deployment)
- [Garden.io Integration](#gardenio-integration)

---

## Docker Deployment

### Building the Docker Image

Build a Docker image locally:

```bash
docker build -t intervalgenerator:latest .
```

To specify a version tag:

```bash
docker build -t intervalgenerator:v1.0.0 --build-arg VERSION=v1.0.0 .
```

### Running Locally with Docker

Run the container with default settings:

```bash
docker run -p 8080:8080 intervalgenerator:latest
```

The API will be available at `http://localhost:8080`

### With Environment Variables

Override configuration via environment variables:

```bash
docker run -p 8080:8080 \
  -e ASPNETCORE_ENVIRONMENT=Development \
  -e ApiSettings__MeterGeneration__DefaultMeterCount=50 \
  -e ApiSettings__Authentication__ApiKey=my-custom-key \
  -e ApiSettings__Authentication__ApiPassword=my-custom-password \
  intervalgenerator:latest
```

### Health Checks

Docker health check is configured in the image. You can also manually verify:

```bash
curl http://localhost:8080/health
```

Response:
```json
{
  "status": "healthy",
  "timestamp": "2024-01-15T10:30:00Z"
}
```

---

## Kubernetes Deployment

### Prerequisites

- `kubectl` configured and authenticated to your cluster (EKS, AKS, or GKE)
- Cluster with at least 1 node available
- IntervalGenerator Docker image available (from DockerHub or private registry)

### Quick Start

Deploy all resources with a single command:

```bash
kubectl apply -f k8s/configmap.yaml
kubectl create secret generic interval-generator-secret \
  --from-literal=ApiSettings__Authentication__ApiKey='demo-api-key' \
  --from-literal=ApiSettings__Authentication__ApiPassword='demo-api-password'
kubectl apply -f k8s/deployment.yaml
kubectl apply -f k8s/service.yaml
```

Verify deployment:

```bash
kubectl get deployments
kubectl get pods
kubectl get svc
```

### Deployment Manifest Walkthrough

**File**: `k8s/deployment.yaml`

Key configuration:

```yaml
spec:
  replicas: 2              # High availability with 2 pods
  strategy:
    type: RollingUpdate    # Zero-downtime updates
    rollingUpdate:
      maxSurge: 1
      maxUnavailable: 0

  template:
    spec:
      containers:
      - name: api
        image: ithedgehog/intervalgenerator:latest
        imagePullPolicy: IfNotPresent

        # Port configuration
        ports:
        - containerPort: 8080
          name: http
          protocol: TCP

        # Resource limits - adjust for your cluster
        resources:
          requests:
            cpu: 100m
            memory: 256Mi
          limits:
            cpu: 500m
            memory: 512Mi

        # Health checks
        livenessProbe:
          httpGet:
            path: /health
            port: 8080
          initialDelaySeconds: 30
          periodSeconds: 10

        readinessProbe:
          httpGet:
            path: /health
            port: 8080
          initialDelaySeconds: 5
          periodSeconds: 5

        # Security context
        securityContext:
          runAsNonRoot: true
          runAsUser: 1000
          readOnlyRootFilesystem: false
```

### Service Manifest Walkthrough

**File**: `k8s/service.yaml`

The service exposes the deployment:

```yaml
apiVersion: v1
kind: Service
metadata:
  name: interval-generator-service
  namespace: default
spec:
  type: ClusterIP
  selector:
    app: interval-generator
  ports:
  - protocol: TCP
    port: 80           # External port
    targetPort: 8080   # Container port
    name: http
```

### ConfigMap Configuration

**File**: `k8s/configmap.yaml`

Configuration is managed via ConfigMap:

```bash
kubectl describe configmap interval-generator-config
```

Key settings:

```yaml
ApiSettings__MeterGeneration__DefaultMeterCount: "100"
ApiSettings__MeterGeneration__DefaultStartDate: "2024-01-01"
ApiSettings__MeterGeneration__DefaultEndDate: "2024-12-31"
ApiSettings__MeterGeneration__DefaultIntervalPeriod: "30"
ApiSettings__MeterGeneration__DefaultBusinessType: "Office"
ApiSettings__MeterGeneration__DeterministicMode: "true"
ApiSettings__MeterGeneration__Seed: "42"
ApiSettings__MeterGeneration__EnableDynamicGeneration: "true"
ApiSettings__Authentication__Enabled: "true"
ASPNETCORE_ENVIRONMENT: "Production"
```

### Secret Creation and Management

Create the secret containing sensitive credentials:

```bash
kubectl create secret generic interval-generator-secret \
  --from-literal=ApiSettings__Authentication__ApiKey='your-api-key' \
  --from-literal=ApiSettings__Authentication__ApiPassword='your-password'
```

Or from an example file (for documentation):

```bash
kubectl apply -f k8s/secret.example.yaml
```

View secret metadata (not values):

```bash
kubectl describe secret interval-generator-secret
```

Update a secret:

```bash
kubectl delete secret interval-generator-secret
kubectl create secret generic interval-generator-secret \
  --from-literal=ApiSettings__Authentication__ApiKey='new-api-key' \
  --from-literal=ApiSettings__Authentication__ApiPassword='new-password'
```

### Resource Limits and Requests Explained

**Requests** - What the container guarantees to use:
- `cpu: 100m` - 0.1 CPU cores
- `memory: 256Mi` - 256 Megabytes

**Limits** - Maximum resources the container can use:
- `cpu: 500m` - 0.5 CPU cores
- `memory: 512Mi` - 512 Megabytes

Adjust these based on your expected load:

| Load | CPU Request | CPU Limit | Memory Request | Memory Limit |
|------|------------|-----------|----------------|--------------|
| Low (dev) | 50m | 200m | 128Mi | 256Mi |
| Medium | 100m | 500m | 256Mi | 512Mi |
| High | 250m | 1000m | 512Mi | 1Gi |

### Health Checks and Readiness Probes

**Liveness Probe** - Restarts the pod if it becomes unresponsive:

```yaml
livenessProbe:
  httpGet:
    path: /health
    port: 8080
  initialDelaySeconds: 30    # Wait 30s before first check
  periodSeconds: 10          # Check every 10s
  failureThreshold: 3        # Restart after 3 failures
```

**Readiness Probe** - Removes the pod from load balancing if not ready:

```yaml
readinessProbe:
  httpGet:
    path: /health
    port: 8080
  initialDelaySeconds: 5
  periodSeconds: 5
  failureThreshold: 2
```

### Accessing the API via Port-Forward

Forward the service port to your local machine:

```bash
kubectl port-forward svc/interval-generator-service 8080:80
```

The API is now available at `http://localhost:8080`

### Test the Deployment

```bash
# List MPANs
curl http://localhost:8080/mpans

# Get half-hourly data
curl -H "Api-Key: demo-api-key" \
     -H "Api-Password: demo-api-password" \
     "http://localhost:8080/v2/mpanhhperperiod?mpan=1234567890123"

# Get API info
curl http://localhost:8080/
```

### Verifying Deployment

```bash
# Check pod status
kubectl get pods -l app=interval-generator
kubectl logs -l app=interval-generator --tail=50

# Describe deployment for detailed info
kubectl describe deployment interval-generator

# Check resource usage
kubectl top pods -l app=interval-generator
```

### Scaling Replicas

Manually scale the deployment:

```bash
# Scale to 3 replicas
kubectl scale deployment interval-generator --replicas=3

# View current replicas
kubectl get deployment interval-generator
```

Set up autoscaling:

```bash
kubectl autoscale deployment interval-generator \
  --min=2 --max=5 --cpu-percent=80
```

### Updating Configuration

Update the ConfigMap:

```bash
# Edit in your editor
kubectl edit configmap interval-generator-config

# Or apply from file after editing
kubectl apply -f k8s/configmap.yaml
```

**Note**: ConfigMap changes require pod restart to take effect:

```bash
# Restart all pods
kubectl rollout restart deployment interval-generator

# Monitor rollout
kubectl rollout status deployment interval-generator
```

Update the image:

```bash
# Set new image version
kubectl set image deployment/interval-generator \
  api=ithedgehog/intervalgenerator:v1.0.1

# Monitor rollout
kubectl rollout status deployment interval-generator

# Rollback if needed
kubectl rollout undo deployment interval-generator
```

### Troubleshooting

**Pod stuck in pending:**

```bash
kubectl describe pod <pod-name>
# Check Events section for resource constraints or node affinity issues
```

**Pod crashes:**

```bash
kubectl logs <pod-name>
kubectl logs <pod-name> --previous  # Previous run's logs
```

**Health check failures:**

```bash
# Test health endpoint directly
kubectl exec <pod-name> -- curl -s http://localhost:8080/health

# Check probe configuration
kubectl describe pod <pod-name>
```

**Authentication errors:**

```bash
# Verify secret exists
kubectl get secret interval-generator-secret

# Check ConfigMap for auth settings
kubectl describe configmap interval-generator-config
```

---

## Garden.io Integration

### Garden.io Overview

[Garden.io](https://garden.io/) is a development platform that simplifies building, testing, and deploying containerized applications to Kubernetes. Key benefits:

- **Local + remote parity** - Test against a real Kubernetes cluster during development
- **Fast iteration** - Hot reload and intelligent caching
- **Integrated workflows** - Build, deploy, and test in one command
- **Environment management** - Easy dev/staging/prod configuration
- **Port forwarding** - Automatic service access without manual `kubectl port-forward`

### Garden.io Configuration File

**File**: `garden.yml`

The configuration defines the project, environments, and services.

### Build Configuration

```yaml
kind: Build
name: api
type: container
spec:
  dockerfile: Dockerfile
  targetImage: ithedgehog/intervalgenerator:${project.version}
```

### Deploy Configuration

```yaml
kind: Deploy
name: api
type: kubernetes
build: api
spec:
  files:
    - k8s/deployment.yaml
    - k8s/service.yaml
    - k8s/configmap.yaml

  # Automatically forward port 8080
  portForwards:
    - name: http
      resource: Deployment/interval-generator
      targetPort: 8080
      localPort: 8080
```

### Development Workflow with Garden

**Initialize project:**

```bash
garden init
```

**Deploy to cluster:**

```bash
garden deploy
```

This will:
1. Build the Docker image
2. Apply all Kubernetes manifests
3. Forward port 8080 to localhost
4. Wait for pods to be ready

**View deployment status:**

```bash
garden status
```

**Watch logs:**

```bash
garden logs --follow
```

**Run tests:**

```bash
garden test
```

**Clean up:**

```bash
garden delete
```

### Testing with Garden

Garden includes a test runner. Add a test configuration:

```yaml
kind: Test
name: api-health
type: container
dependencies:
  - deploy.api
spec:
  image: curlimages/curl:latest
  command:
    - /bin/sh
    - -c
    - |
      curl -f http://interval-generator-service/health || exit 1
```

Run tests:

```bash
garden test
```

### Environment-Specific Configuration

Define multiple environments in `garden.yml`:

```yaml
environments:
  - name: dev
  - name: staging
  - name: prod
```

Deploy to a specific environment:

```bash
garden deploy --env staging
```

### Custom Configuration with Variables

Set environment-specific variables:

```bash
# Override via command line
garden deploy --set apiSettings.meterCount=200

# Or set via environment
export GARDEN_VARIABLES='{"meterCount": 200}'
garden deploy
```

---

## Summary

| Method | Use Case | Complexity |
|--------|----------|-----------|
| Docker | Local development, single container | Low |
| Kubernetes (kubectl) | Production, multi-environment, fine control | Medium |
| Garden.io | Team development, local + remote testing | Medium |

Choose based on your deployment needs and team experience with Kubernetes.
