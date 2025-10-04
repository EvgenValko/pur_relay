# Troubleshooting Guide

## Common Issues and Solutions

### 1. "Unable to translate bytes at index X from specified code page to Unicode"

**Problem**: This error occurs when Unity and relay server use incompatible serialization formats for strings.

**Cause**: 
- Unity's `PurrNet.Packing.Packer` uses a different string serialization format than LiteNetLib's `NetDataWriter`
- The relay server expects LiteNetLib format, but Unity was sending PurrNet format

**Solution**: ✅ **FIXED in v1.0.1**
- `RelayTransport.cs` now uses `NetDataWriter` from LiteNetLib for all communication with relay server
- This ensures both sides use the same serialization format

**Changes Made**:
```csharp
// Before (caused encoding errors):
_packer.ResetPositionAndMode(false);
Packer<string>.Write(_packer, roomName);

// After (works correctly):
var writer = new NetDataWriter();
writer.Put(roomName);
```

### 1.1. Players Not Spawning / Game Data Not Working

**Problem**: Connection works, but players don't spawn or game data doesn't sync correctly.

**Cause**: 
- Incorrect data offset when extracting game packets from relay messages
- Was using `reader.Position` which doesn't account for internal LiteNetLib headers
- Game data was being read from wrong position in buffer

**Solution**: ✅ **FIXED in v1.0.2**
- Now uses `reader.GetRemainingBytesSegment()` which correctly extracts remaining data
- This ensures game packets are properly forwarded with correct offsets

**Changes Made**:
```csharp
// Before (wrong offset):
var data = new ByteData(reader.RawData, reader.Position, reader.AvailableBytes);

// After (correct):
var segment = reader.GetRemainingBytesSegment();
var data = new ByteData(segment);
```

### 2. Port Already in Use

**Error**: `Failed to start server on port 9050`

**Solutions**:
- Use a different port: `dotnet run -- --port 9051`
- Stop the conflicting application
- Check if old relay instance is still running

**Find process using port** (Windows):
```powershell
netstat -ano | findstr :9050
taskkill /PID <process_id> /F
```

**Find process using port** (Linux):
```bash
sudo lsof -i :9050
sudo kill -9 <process_id>
```

### 3. Clients Can't Connect to Relay

**Symptoms**:
- Unity shows "Connecting" state indefinitely
- No logs appear on relay server

**Checklist**:
- ✅ Relay server is running and showing "Relay server listening on port 9050"
- ✅ Firewall allows UDP traffic on the relay port
- ✅ Relay address and port in Unity match server settings
- ✅ For remote servers, public IP is used, not localhost

**Test Connection**:
```bash
# On relay server machine
netstat -an | findstr 9050   # Windows
netstat -an | grep 9050      # Linux
```

### 4. Room Not Found

**Error**: `Room not found`

**Causes**:
- Host hasn't created room yet
- Room name mismatch (case-sensitive)
- Room was cleaned up due to inactivity
- Host disconnected before client joined

**Solutions**:
- Ensure host calls `StartServer()` before client calls `StartClient()`
- Verify room names match exactly (case-sensitive)
- Check relay server logs for room creation confirmation

### 5. Connection Drops After Few Seconds

**Symptoms**:
- Initial connection works
- Disconnects after ~10 seconds

**Cause**: Timeout due to no keepalive packets

**Solution**: Check if your game is sending data regularly, or the connection will timeout. Default timeout is 10 seconds.

**Adjust timeout** in `RelayTransport.cs`:
```csharp
[SerializeField]
private float _timeoutInSeconds = 30f; // Increase timeout
```

And in `RelayServerConfig.cs`:
```csharp
public int TimeoutSeconds { get; set; } = 30; // Match Unity timeout
```

### 6. High Latency

**Symptoms**:
- Game feels laggy
- High ping values

**Analysis**:
- Relay adds one extra hop: Client → Relay → Host
- Expected overhead: 5-50ms depending on relay location

**Solutions**:
- Deploy relay server closer to your players geographically
- Use a VPS with good network connectivity
- Consider multiple regional relay servers
- For LAN games, use `UDPTransport` instead (direct P2P)

### 7. Room Full Error

**Error**: `Room is full`

**Cause**: Room has reached maximum client limit

**Solutions**:
- Increase `maxClients` in `RelayTransport` settings
- Increase `MaxClientsPerRoom` in relay server config
- Create multiple rooms for your game

### 8. Memory/CPU Issues on Relay Server

**Symptoms**:
- Server becomes slow over time
- High memory usage

**Analysis**:
```bash
# Monitor server resources
dotnet run -- --port 9050 --tick-rate 30
# Watch CPU and memory in task manager / top
```

**Solutions**:
- Reduce tick rate: `--tick-rate 15` (lower CPU usage)
- Limit max rooms: `--max-rooms 500`
- Check for memory leaks in logs
- Restart server periodically

### 9. Compilation Errors in Unity

**Error**: `The type or namespace name 'NetDataWriter' could not be found`

**Cause**: Missing `using LiteNetLib.Utils;` directive

**Solution**: Ensure `RelayTransport.cs` has:
```csharp
using LiteNetLib;
using LiteNetLib.Utils;
```

### 10. Relay Server Crashes on Startup

**Error**: Various startup errors

**Checklist**:
- ✅ .NET 9.0 (or 8.0) is installed: `dotnet --version`
- ✅ Dependencies restored: `dotnet restore`
- ✅ Project builds: `dotnet build`
- ✅ Port is not already in use

**Reinstall dependencies**:
```bash
dotnet clean
dotnet restore
dotnet build
```

## Debugging Tips

### Enable Verbose Logging (Relay Server)

Add logging to `RelayServer.cs`:
```csharp
Console.WriteLine($"[DEBUG] Packet type: {packetType}, Size: {reader.AvailableBytes}");
```

### Monitor Network Traffic

**Windows**:
```powershell
# Watch UDP traffic on port 9050
netstat -an 1 | findstr :9050
```

**Linux**:
```bash
# Watch UDP traffic
sudo tcpdump -i any udp port 9050 -v
```

### Unity Debug Logs

Add logging in `RelayTransport.cs`:
```csharp
Debug.Log($"[RelayTransport] Sending CreateRoom: {_roomName}");
Debug.Log($"[RelayTransport] Received packet: {packetType}");
```

### Test with Multiple Clients

1. Build Unity project
2. Run one instance as host (StartServer)
3. Run another as client (StartClient)
4. Watch relay server console for connections

## Performance Benchmarks

### Expected Values:
- **Connection Time**: 50-200ms
- **Additional Latency**: 5-50ms per hop
- **Max Rooms**: 1000+ (limited by server resources)
- **Max Clients per Room**: 100 (configurable)
- **CPU Usage**: ~5-15% for 100 active connections @ 30Hz
- **Memory Usage**: ~50-100MB baseline + ~1KB per room

### Stress Testing:

Create test script to spawn multiple connections:
```csharp
for (int i = 0; i < 100; i++)
{
    var transport = CreateNewTransport();
    transport.roomName = $"TEST_{i}";
    transport.StartServer();
}
```

Monitor relay server performance during test.

## Getting Help

If you're still having issues:

1. Check relay server console output
2. Check Unity console for errors
3. Verify versions match (LiteNetLib 1.2.0)
4. Test with minimal scene (just NetworkManager + RelayTransport)
5. Try with localhost first before remote server

## Version Compatibility

| Component | Version | Required |
|-----------|---------|----------|
| .NET | 8.0 or 9.0 | Yes |
| LiteNetLib | 1.2.0+ | Yes |
| Unity | 2021.3+ | Recommended |
| PurrNet | Latest | Yes |

## Quick Health Check

Run these commands to verify setup:

```bash
# 1. Check .NET version
dotnet --version

# 2. Build relay server
cd C:\Users\admin\Projects\relay
dotnet build -c Release

# 3. Start server
dotnet run

# 4. In another terminal, check if port is listening
netstat -an | findstr :9050   # Windows
netstat -an | grep 9050       # Linux

# 5. In Unity, add RelayTransport component
# 6. Set relay address to 127.0.0.1
# 7. Try StartServer() / StartClient()
```

If all steps work, your setup is correct!

