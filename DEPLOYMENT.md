# Deployment Guide

## Health Checks

The relay server now includes an HTTP server for health checks on port 8080 (configurable).

### Endpoints

- **GET /health** - Simple health check (returns `{"status":"healthy"}`)
- **GET /ping** - Simple ping endpoint (returns `pong`)
- **GET /status** - Detailed server statistics (JSON)

### Example Response

```bash
curl http://localhost:8080/health
# {"status":"healthy","relay":"running"}

curl http://localhost:8080/status
# {
#   "status": "healthy",
#   "relay": {
#     "running": true,
#     "activeRooms": 15,
#     "totalConnections": 87,
#     "totalRooms": 23,
#     "uptime": "02:15:30"
#   }
# }
```

## Deployment Platforms

### Railway

1. **Create `railway.toml`**:
```toml
[build]
builder = "dockerfile"
dockerfilePath = "Dockerfile"

[deploy]
startCommand = "dotnet PurrNetRelayServer.dll --port 9050 --http-port 8080"
healthcheckPath = "/health"
healthcheckTimeout = 100
restartPolicyType = "on-failure"
restartPolicyMaxRetries = 10

[[services]]
name = "purrnet-relay"

  [[services.ports]]
  name = "relay-udp"
  protocol = "udp"
  port = 9050

  [[services.ports]]
  name = "health-http"
  protocol = "tcp"
  port = 8080
```

2. **Deploy**:
```bash
railway up
```

3. **Configure Health Check**:
   - Go to Railway dashboard
   - Settings → Health Check
   - Path: `/health`
   - Port: `8080`
   - Protocol: `HTTP`

### Render

1. **Create `render.yaml`**:
```yaml
services:
  - type: web
    name: purrnet-relay
    runtime: docker
    dockerfilePath: ./Dockerfile
    plan: starter
    envVars:
      - key: PORT
        value: 8080
    healthCheckPath: /health
    ports:
      - port: 8080
        protocol: tcp
      - port: 9050
        protocol: udp
```

2. **Deploy via Render Dashboard**:
   - Connect your GitHub repo
   - Render auto-detects `render.yaml`
   - Deploy!

### Docker Compose

```yaml
version: '3.8'
services:
  relay:
    build: .
    ports:
      - "9050:9050/udp"
      - "8080:8080/tcp"
    environment:
      - RELAY_PORT=9050
      - HTTP_PORT=8080
      - MAX_ROOMS=1000
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:8080/health"]
      interval: 30s
      timeout: 3s
      retries: 3
    restart: unless-stopped
```

### Kubernetes

```yaml
apiVersion: v1
kind: Service
metadata:
  name: purrnet-relay
spec:
  type: LoadBalancer
  ports:
    - name: relay-udp
      protocol: UDP
      port: 9050
      targetPort: 9050
    - name: health-http
      protocol: TCP
      port: 8080
      targetPort: 8080
  selector:
    app: purrnet-relay
---
apiVersion: apps/v1
kind: Deployment
metadata:
  name: purrnet-relay
spec:
  replicas: 2
  selector:
    matchLabels:
      app: purrnet-relay
  template:
    metadata:
      labels:
        app: purrnet-relay
    spec:
      containers:
      - name: relay
        image: your-registry/purrnet-relay:latest
        ports:
        - containerPort: 9050
          protocol: UDP
        - containerPort: 8080
          protocol: TCP
        env:
        - name: RELAY_PORT
          value: "9050"
        - name: HTTP_PORT
          value: "8080"
        livenessProbe:
          httpGet:
            path: /health
            port: 8080
          initialDelaySeconds: 10
          periodSeconds: 30
        readinessProbe:
          httpGet:
            path: /health
            port: 8080
          initialDelaySeconds: 5
          periodSeconds: 10
```

### AWS (Elastic Beanstalk)

1. **Create `Dockerrun.aws.json`**:
```json
{
  "AWSEBDockerrunVersion": "1",
  "Image": {
    "Name": "your-registry/purrnet-relay:latest"
  },
  "Ports": [
    {
      "ContainerPort": 9050,
      "Protocol": "udp"
    },
    {
      "ContainerPort": 8080,
      "Protocol": "tcp"
    }
  ],
  "HealthCheck": {
    "Test": ["CMD-SHELL", "curl -f http://localhost:8080/health || exit 1"],
    "Interval": 30,
    "Timeout": 3,
    "Retries": 3
  }
}
```

2. **Deploy**:
```bash
eb init
eb create purrnet-relay-env
eb deploy
```

### DigitalOcean App Platform

1. **Create `.do/app.yaml`**:
```yaml
name: purrnet-relay
services:
  - name: relay
    dockerfile_path: Dockerfile
    github:
      repo: your-username/your-repo
      branch: main
    health_check:
      http_path: /health
      port: 8080
    ports:
      - port: 9050
        protocol: UDP
      - port: 8080
        protocol: TCP
    instance_size_slug: basic-xs
    instance_count: 1
```

2. **Deploy via CLI**:
```bash
doctl apps create --spec .do/app.yaml
```

## Environment Variables

You can configure via environment variables:

```bash
export RELAY_PORT=9050
export HTTP_PORT=8080
export MAX_ROOMS=1000
export TICK_RATE=30

dotnet run -- --port $RELAY_PORT --http-port $HTTP_PORT --max-rooms $MAX_ROOMS --tick-rate $TICK_RATE
```

## Testing Health Checks Locally

```bash
# Start server
dotnet run

# In another terminal
curl http://localhost:8080/health
# Should return: {"status":"healthy","relay":"running"}

curl http://localhost:8080/status
# Returns detailed statistics

curl http://localhost:8080/ping
# Returns: pong
```

## Monitoring

### Prometheus Metrics (Optional Enhancement)

You can extend `HealthCheckServer.cs` to add Prometheus metrics:

```csharp
case "/metrics":
    responseString = GetPrometheusMetrics();
    response.ContentType = "text/plain";
    break;

private string GetPrometheusMetrics()
{
    var stats = _relayServer.GetStatistics();
    return $@"# HELP relay_active_rooms Number of active rooms
# TYPE relay_active_rooms gauge
relay_active_rooms {stats.ActiveRooms}

# HELP relay_total_connections Total connections made
# TYPE relay_total_connections counter
relay_total_connections {stats.TotalConnections}

# HELP relay_uptime_seconds Server uptime in seconds
# TYPE relay_uptime_seconds gauge
relay_uptime_seconds {stats.Uptime.TotalSeconds}
";
}
```

## Troubleshooting

### Health Check Fails

1. **Verify HTTP server is running**:
   ```bash
   curl http://localhost:8080/health
   ```

2. **Check port binding**:
   ```bash
   netstat -an | grep 8080
   ```

3. **Check logs**:
   ```bash
   # Look for "Health check server listening on HTTP port 8080"
   ```

### UDP Port Not Working

Some platforms (like Heroku) don't support UDP. Use platforms that support UDP:
- ✅ Railway
- ✅ Render
- ✅ DigitalOcean
- ✅ AWS EC2
- ✅ Google Cloud Compute
- ✅ Azure VMs
- ❌ Heroku (TCP only)
- ❌ Vercel (serverless)
- ❌ Netlify (static)

## Production Checklist

- [ ] Health check endpoint responds on `/health`
- [ ] Both UDP (9050) and TCP (8080) ports are exposed
- [ ] Environment variables are set
- [ ] Resource limits are appropriate (CPU/RAM)
- [ ] Monitoring is set up
- [ ] Logs are being collected
- [ ] Auto-restart is configured
- [ ] Backup server is ready (optional)
- [ ] DNS is configured
- [ ] Firewall allows UDP + TCP traffic

## Quick Deploy Commands

**Build Docker Image**:
```bash
docker build -t purrnet-relay .
```

**Run Locally**:
```bash
docker run -p 9050:9050/udp -p 8080:8080/tcp purrnet-relay
```

**Test Health Check**:
```bash
curl http://localhost:8080/health
```

**Test Unity Connection**:
In Unity, set `relayAddress` to your server's public IP and `relayPort` to `9050`.

