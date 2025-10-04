# Testing Health Checks

Quick guide to verify health checks are working.

## Local Testing

### 1. Start the Server

```bash
cd C:\Users\admin\Projects\relay
dotnet run
```

You should see:
```
=== PurrNet Relay Server ===

UDP Port: 9050
HTTP Port: 8080
Max Rooms: 1000
Tick Rate: 30 Hz

Relay server listening on port 9050
Health check server listening on HTTP port 8080
Server started. Press Ctrl+C to stop.
```

### 2. Test Health Endpoint

**PowerShell (Windows):**
```powershell
Invoke-WebRequest -Uri http://localhost:8080/health
```

**Bash/CMD:**
```bash
curl http://localhost:8080/health
```

**Expected Response:**
```json
{"status":"healthy","relay":"running"}
```

### 3. Test Status Endpoint

```bash
curl http://localhost:8080/status
```

**Expected Response:**
```json
{
  "status": "healthy",
  "relay": {
    "running": true,
    "activeRooms": 0,
    "totalConnections": 0,
    "totalRooms": 0,
    "uptime": "00:01:23"
  }
}
```

### 4. Test Ping Endpoint

```bash
curl http://localhost:8080/ping
```

**Expected Response:**
```
pong
```

## Docker Testing

### Build Image

```bash
docker build -t purrnet-relay .
```

### Run Container

```bash
docker run -p 9050:9050/udp -p 8080:8080/tcp purrnet-relay
```

### Test Health Check

```bash
curl http://localhost:8080/health
```

### Check Docker Health Status

```bash
docker ps
```

Look for "healthy" in the STATUS column:
```
CONTAINER ID   IMAGE           STATUS
abc123def456   purrnet-relay   Up 30 seconds (healthy)
```

## Deployment Platform Testing

### Railway

After deploying, Railway will automatically check:
```
https://your-app.railway.app/health
```

You can also test manually:
```bash
curl https://your-app.railway.app/health
```

### Render

Render checks:
```
http://your-app.onrender.com/health
```

### Your Own VPS

If deployed to a VPS:
```bash
curl http://your-server-ip:8080/health
```

## Troubleshooting

### Health Check Returns Nothing

**Cause**: HTTP server not started

**Check**:
1. Look for "Health check server listening on HTTP port 8080" in logs
2. Verify port 8080 is not in use by another app
3. Check firewall allows TCP traffic on port 8080

**Fix**:
```bash
# Windows - check if port is in use
netstat -an | findstr :8080

# Linux - check if port is in use
netstat -an | grep 8080

# Try different port
dotnet run -- --http-port 8081
```

### Connection Refused

**Cause**: Firewall blocking port 8080

**Fix (Linux)**:
```bash
sudo ufw allow 8080/tcp
```

**Fix (Windows)**:
```powershell
New-NetFirewallRule -DisplayName "PurrNet Health Check" -Direction Inbound -LocalPort 8080 -Protocol TCP -Action Allow
```

### 404 Not Found

**Cause**: Wrong URL path

**Correct URLs**:
- ✅ `http://localhost:8080/health`
- ✅ `http://localhost:8080/status`
- ✅ `http://localhost:8080/ping`
- ❌ `http://localhost:8080/api/health` (wrong)
- ❌ `http://localhost:8080` (returns 404, use `/health` instead)

Actually, `/` also works and redirects to `/health`!

### Docker Health Check Failing

**Check Docker logs**:
```bash
docker logs <container-id>
```

**Inspect health check**:
```bash
docker inspect <container-id> | grep Health -A 10
```

**Manual health check inside container**:
```bash
docker exec -it <container-id> /bin/bash
curl http://localhost:8080/health
```

## Success Indicators

✅ **All Working:**
- Server starts without errors
- Both UDP (9050) and HTTP (8080) ports show as listening
- `/health` returns 200 OK with JSON response
- `/status` shows current statistics
- `/ping` returns "pong"

✅ **Ready for Deployment:**
- Health checks work locally
- Docker health check shows "healthy"
- UDP relay accepts connections from Unity

## Next Steps

Once health checks are working:
1. Deploy to your platform (Railway, Render, etc.)
2. Configure platform health check settings
3. Test Unity connection to deployed server
4. Monitor server status via `/status` endpoint

## Monitoring Script

Save this as `monitor.sh`:

```bash
#!/bin/bash
while true; do
    RESPONSE=$(curl -s http://localhost:8080/status)
    TIMESTAMP=$(date '+%Y-%m-%d %H:%M:%S')
    echo "[$TIMESTAMP] $RESPONSE"
    sleep 10
done
```

Run it:
```bash
chmod +x monitor.sh
./monitor.sh
```

This will show server stats every 10 seconds!

