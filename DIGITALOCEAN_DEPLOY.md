# DigitalOcean Deployment Guide

## Quick Deploy to DigitalOcean App Platform

### Option 1: Via Web Console (Easiest)

1. **Push to GitHub**:
   ```bash
   cd C:\Users\admin\Projects\relay
   git init
   git add .
   git commit -m "Initial relay server"
   git remote add origin https://github.com/YOUR_USERNAME/purrnet-relay.git
   git push -u origin main
   ```

2. **Create App on DigitalOcean**:
   - Go to https://cloud.digitalocean.com/apps
   - Click **"Create App"**
   - Select **"GitHub"** and authorize
   - Choose your `purrnet-relay` repository
   - Click **"Next"**

3. **Configure App**:
   - **Type**: Web Service (auto-detected)
   - **Dockerfile Path**: `/Dockerfile` (auto-detected)
   - **HTTP Port**: `8080`
   - **HTTP Routes**: `/`
   - Click **"Edit Plan"**

4. **Select Plan**:
   - **Basic**: $5/month (512MB RAM, 1 vCPU) - for testing
   - **Professional**: $12/month (1GB RAM, 2 vCPU) - recommended
   - Click **"Back"**

5. **Health Check (Auto-configured)**:
   DigitalOcean automatically uses the `HEALTHCHECK` from your Dockerfile!
   - Path: `/health`
   - Port: `8080`
   - Protocol: HTTP

6. **Deploy**:
   - Click **"Next"** → **"Next"** → **"Create Resources"**
   - Wait 3-5 minutes for build and deploy
   - ✅ Done!

### Option 2: Via CLI

```bash
# Install doctl
# Windows (with Chocolatey):
choco install doctl

# Or download from: https://github.com/digitalocean/doctl/releases

# Authenticate
doctl auth init

# Create app spec
cat > app-spec.yaml << EOF
name: purrnet-relay
services:
  - name: relay
    github:
      repo: YOUR_USERNAME/purrnet-relay
      branch: main
    dockerfile_path: Dockerfile
    http_port: 8080
    health_check:
      http_path: /health
      port: 8080
    instance_size_slug: basic-xs
    instance_count: 1
EOF

# Deploy
doctl apps create --spec app-spec.yaml
```

## After Deployment

### Get Your Server URL

In DigitalOcean console, you'll see:
```
https://purrnet-relay-xxxxx.ondigitalocean.app
```

### Important: Get UDP Endpoint

**DigitalOcean App Platform doesn't support UDP!** 😱

You need to use a **Droplet** (VPS) instead for UDP relay server.

## Correct Deployment: Using Droplet (VPS)

### 1. Create Droplet

1. Go to https://cloud.digitalocean.com/droplets
2. Click **"Create Droplet"**
3. Choose:
   - **Image**: Docker (Marketplace)
   - **Plan**: Basic
   - **CPU**: Regular ($6/mo = 1GB/1vCPU, or $12/mo = 2GB/2vCPU)
   - **Datacenter**: Closest to your players
   - **Authentication**: SSH key or Password
4. Click **"Create Droplet"**

### 2. SSH into Droplet

```bash
ssh root@YOUR_DROPLET_IP
```

### 3. Clone and Run

```bash
# Install git (if not already)
apt update
apt install -y git

# Clone your repo
git clone https://github.com/YOUR_USERNAME/purrnet-relay.git
cd purrnet-relay

# Build and run with Docker
docker build -t purrnet-relay .
docker run -d \
  --name relay \
  --restart unless-stopped \
  -p 9050:9050/udp \
  -p 8080:8080/tcp \
  purrnet-relay
```

### 4. Verify It's Running

```bash
# Check health
curl http://localhost:8080/health

# Check container
docker ps

# Check logs
docker logs relay
```

### 5. Configure Firewall

```bash
# Allow UDP and HTTP
ufw allow 9050/udp
ufw allow 8080/tcp
ufw allow 22/tcp  # SSH
ufw enable
```

### 6. Test from Unity

In Unity:
```csharp
relayTransport.relayAddress = "YOUR_DROPLET_IP"; // e.g., "143.198.123.45"
relayTransport.relayPort = 9050;
```

## Auto-Deploy Updates

### Set up GitHub Actions

Create `.github/workflows/deploy.yml`:

```yaml
name: Deploy to DigitalOcean

on:
  push:
    branches: [ main ]

jobs:
  deploy:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v2
      
      - name: Deploy to Droplet
        uses: appleboy/ssh-action@master
        with:
          host: ${{ secrets.DROPLET_IP }}
          username: root
          key: ${{ secrets.SSH_PRIVATE_KEY }}
          script: |
            cd purrnet-relay
            git pull
            docker build -t purrnet-relay .
            docker stop relay || true
            docker rm relay || true
            docker run -d \
              --name relay \
              --restart unless-stopped \
              -p 9050:9050/udp \
              -p 8080:8080/tcp \
              purrnet-relay
```

Add secrets in GitHub:
- `DROPLET_IP`: Your droplet's IP
- `SSH_PRIVATE_KEY`: Your SSH private key

## Monitoring

### Check Status

```bash
# Via HTTP
curl http://YOUR_DROPLET_IP:8080/status

# Container stats
docker stats relay

# Logs
docker logs -f relay
```

### Set Up Monitoring Dashboard

DigitalOcean provides built-in monitoring:
1. Go to your Droplet page
2. Click **"Monitoring"** tab
3. View CPU, RAM, Network graphs

## Pricing

| Configuration | Monthly Cost | Capacity (Medium Game) |
|---------------|--------------|------------------------|
| 1GB / 1vCPU   | $6           | 15-25 sessions         |
| 2GB / 2vCPU   | $12          | 80-120 sessions        |
| 4GB / 2vCPU   | $24          | 250-350 sessions       |
| 8GB / 4vCPU   | $48          | 600-800 sessions       |

## Scaling

### Multiple Regions

Deploy droplets in multiple regions:
- **New York** (for US East players)
- **Frankfurt** (for EU players)
- **Singapore** (for Asia players)

Each droplet runs independently, players connect to closest one.

### Load Balancer

For high traffic, use DigitalOcean Load Balancer:
```bash
# Not recommended for UDP relay
# Better: Use DNS round-robin or manual server selection in Unity
```

## Backup & Updates

### Backup

No backup needed - relay server is stateless!
Just keep your code in Git.

### Updates

```bash
ssh root@YOUR_DROPLET_IP
cd purrnet-relay
git pull
docker build -t purrnet-relay .
docker stop relay
docker rm relay
docker run -d --name relay --restart unless-stopped -p 9050:9050/udp -p 8080:8080/tcp purrnet-relay
```

Or use GitHub Actions (see above).

## Troubleshooting

### Health Check Works But UDP Doesn't

**Issue**: HTTP health check passes but Unity can't connect.

**Fix**: Check firewall allows UDP:
```bash
ufw status
# Should show: 9050/udp ALLOW Anywhere
```

### High CPU Usage

**Monitor**:
```bash
docker stats relay
```

**If >80% CPU**:
- Upgrade droplet size
- Reduce `--tick-rate`
- Limit `--max-rooms`

### Out of Memory

```bash
# Check memory
free -h

# Restart container
docker restart relay
```

## Quick Commands

```bash
# Start
docker start relay

# Stop
docker stop relay

# Restart
docker restart relay

# Logs (last 100 lines)
docker logs --tail 100 relay

# Logs (live)
docker logs -f relay

# Check health
curl http://localhost:8080/status

# Remove container
docker stop relay && docker rm relay
```

## Summary

✅ **For UDP Relay Server**: Use **Droplet** (VPS), not App Platform
✅ **Cost**: $12/month for 2GB/2vCPU (80-120 sessions)
✅ **Setup Time**: 10 minutes
✅ **Auto-deploy**: Optional GitHub Actions
✅ **Health Check**: Built into Docker image
✅ **Monitoring**: DigitalOcean dashboard + `/status` endpoint

🎉 **Your relay server is now production-ready!**

