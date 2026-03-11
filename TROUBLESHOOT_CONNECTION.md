# Troubleshooting DigitalOcean Connection Issues

## Quick Diagnosis

### Step 1: Verify Server is Running

SSH into your Droplet:
```bash
ssh root@YOUR_DROPLET_IP
```

Check if container is running:
```bash
docker ps
```

Should show:
```
CONTAINER ID   IMAGE           STATUS
abc123         purrnet-relay   Up 10 minutes (healthy)
```

If not running:
```bash
docker logs relay
```

### Step 2: Test Health Check (HTTP)

From your Droplet:
```bash
curl http://localhost:8080/health
```

Should return: `{"status":"healthy","relay":"running"}`

From your computer:
```bash
curl http://YOUR_DROPLET_IP:8080/health
```

**If this doesn't work** → HTTP port blocked
**If this works** → HTTP OK, problem is UDP

### Step 3: Check Firewall (Most Common Issue!)

On Droplet:
```bash
ufw status
```

Should show:
```
Status: active

To                         Action      From
--                         ------      ----
9050/udp                   ALLOW       Anywhere
8080/tcp                   ALLOW       Anywhere
22/tcp                     ALLOW       Anywhere
```

**If 9050/udp is missing**:
```bash
ufw allow 9050/udp
ufw reload
```

### Step 4: Check DigitalOcean Cloud Firewall

1. Go to https://cloud.digitalocean.com/networking/firewalls
2. Check if there's a firewall attached to your Droplet
3. If yes, make sure it allows:
   - **Inbound UDP 9050** from All IPv4 and All IPv6
   - **Inbound TCP 8080** from All IPv4 and All IPv6

**To add UDP rule**:
- Type: Custom
- Protocol: UDP
- Port: 9050
- Sources: All IPv4, All IPv6

### Step 5: Test UDP Port

From your computer (Windows PowerShell):
```powershell
# Test if UDP port is reachable
Test-NetConnection -ComputerName YOUR_DROPLET_IP -Port 9050 -InformationLevel Detailed
```

Or use online tool:
- https://www.yougetsignal.com/tools/open-ports/
- Enter your Droplet IP
- Port: 9050
- Protocol: UDP

**Note**: Many online checkers can't test UDP properly. Better to test from Unity.

### Step 6: Check Relay Server Logs

```bash
docker logs -f relay
```

You should see:
```
Relay server listening on port 9050
Health check server listening on HTTP port 8080
```

When Unity tries to connect, you should see:
```
Peer connected: YOUR_IP:XXXXX (ID: 0)
```

**If you don't see this** → Packets not reaching server

### Step 7: Verify Unity Configuration

In Unity:
```csharp
relayTransport.relayAddress = "YOUR_DROPLET_IP"; // NOT "localhost"!
relayTransport.relayPort = 9050; // Must be 9050
```

Print to verify:
```csharp
Debug.Log($"Connecting to {relayTransport.relayAddress}:{relayTransport.relayPort}");
```

## Common Issues & Solutions

### Issue 1: "Connection Timeout"

**Cause**: Firewall blocking UDP

**Fix**:
```bash
# On Droplet
ufw allow 9050/udp
ufw status

# Verify rule is there
netstat -ulnp | grep 9050
```

Should show:
```
udp    0    0 0.0.0.0:9050    0.0.0.0:*    12345/dotnet
```

### Issue 2: Health Check Works But UDP Doesn't

**Cause**: Only TCP allowed, UDP blocked

**Fix**: Check **both** firewalls:

1. **UFW (on Droplet)**:
   ```bash
   ufw allow 9050/udp
   ```

2. **DigitalOcean Cloud Firewall** (in web console):
   - Networking → Firewalls
   - Edit your firewall
   - Add Inbound Rule: UDP 9050

### Issue 3: "LiteNetLib.NetManager: Connection failed"

**Cause**: Server not responding to connection key

**Check**: Server logs should show connection attempts:
```bash
docker logs relay | grep "Peer connected"
```

**If empty**: Firewall issue
**If showing**: Connection working, check Unity code

### Issue 4: Wrong IP Address

**Verify Droplet IP**:
```bash
# On Droplet
curl ifconfig.me
```

This is your public IP. Use this in Unity!

### Issue 5: Docker Container Not Exposing UDP

**Verify Docker port mapping**:
```bash
docker ps --format "table {{.Ports}}"
```

Should show:
```
0.0.0.0:9050->9050/udp, 0.0.0.0:8080->8080/tcp
```

**If missing UDP**:
```bash
docker stop relay
docker rm relay
docker run -d \
  --name relay \
  --restart unless-stopped \
  -p 9050:9050/udp \
  -p 8080:8080/tcp \
  purrnet-relay
```

### Issue 6: IPv6 Only Server

Some DigitalOcean droplets default to IPv6.

**Check**:
```bash
ip addr show
```

**Fix**: Use IPv4 address (starts with numbers like 143.198.x.x, not letters)

## Complete Diagnostic Script

Run this on your Droplet:

```bash
#!/bin/bash
echo "=== PurrNet Relay Diagnostics ==="
echo ""

echo "1. Container Status:"
docker ps | grep relay

echo ""
echo "2. Health Check:"
curl -s http://localhost:8080/health

echo ""
echo "3. UFW Status:"
ufw status | grep -E "9050|8080|Status"

echo ""
echo "4. Port Listening:"
netstat -ulnp | grep 9050

echo ""
echo "5. Recent Logs:"
docker logs --tail 10 relay

echo ""
echo "6. Public IP:"
curl -s ifconfig.me

echo ""
echo "=== Diagnostics Complete ==="
```

Save as `diagnose.sh`, then:
```bash
chmod +x diagnose.sh
./diagnose.sh
```

## Step-by-Step Fix

### 1. Restart Everything

```bash
# Stop and remove container
docker stop relay
docker rm relay

# Make sure firewall allows UDP
ufw allow 9050/udp
ufw allow 8080/tcp
ufw reload

# Restart container
docker run -d \
  --name relay \
  --restart unless-stopped \
  -p 9050:9050/udp \
  -p 8080:8080/tcp \
  purrnet-relay

# Verify it's running
docker ps
docker logs relay
```

### 2. Test from Unity

```csharp
using UnityEngine;
using PurrNet.Transports;

public class TestRelay : MonoBehaviour
{
    void Start()
    {
        var relay = GetComponent<RelayTransport>();
        
        // Replace with your actual Droplet IP
        relay.relayAddress = "143.198.123.45"; // YOUR IP HERE!
        relay.relayPort = 9050;
        relay.roomName = "TEST123";
        
        Debug.Log($"[TEST] Connecting to {relay.relayAddress}:{relay.relayPort}");
        Debug.Log($"[TEST] Room: {relay.roomName}");
        
        relay.StartServer();
    }
}
```

Watch Unity console for connection messages.

### 3. Monitor Server

While Unity is connecting:
```bash
docker logs -f relay
```

You should see:
```
Peer connected: YOUR_PC_IP:XXXXX (ID: 0)
Room created: 'TEST123' by YOUR_PC_IP:XXXXX (Max clients: 10)
```

## Quick Checklist

- [ ] Docker container is running: `docker ps`
- [ ] Health check works: `curl http://localhost:8080/health`
- [ ] UFW allows UDP 9050: `ufw status`
- [ ] Cloud Firewall allows UDP 9050 (check web console)
- [ ] Using correct Droplet IP (not localhost!)
- [ ] Unity relayAddress is set to Droplet IP
- [ ] Unity relayPort is 9050
- [ ] Server logs show "listening on port 9050"
- [ ] No errors in `docker logs relay`

## Still Not Working?

### Test with Simple UDP Echo

Create `test-udp.sh` on Droplet:
```bash
#!/bin/bash
nc -ul 9050
```

Run it:
```bash
chmod +x test-udp.sh
./test-udp.sh
```

From your PC (PowerShell):
```powershell
# Send UDP packet
$udpClient = New-Object System.Net.Sockets.UdpClient
$bytes = [System.Text.Encoding]::ASCII.GetBytes("test")
$udpClient.Send($bytes, $bytes.Length, "YOUR_DROPLET_IP", 9050)
```

If this works → Relay server issue
If this doesn't work → Network/firewall issue

## Get Help

If still not working, collect this info:

```bash
# On Droplet
echo "=== Info for Support ==="
echo "Droplet IP: $(curl -s ifconfig.me)"
echo ""
docker ps
echo ""
docker logs --tail 20 relay
echo ""
ufw status
echo ""
netstat -ulnp | grep 9050
```

Send output to support or forum!


