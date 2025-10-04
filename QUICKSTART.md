# Quick Start Guide

## Running the Relay Server

### 1. Build the Project
```bash
cd C:\Users\admin\Projects\relay
dotnet build -c Release
```

### 2. Run the Server

**Windows:**
```bash
start-server.bat
```

Or with custom settings:
```bash
start-server.bat 9050 1000 30
```

**Linux/Mac:**
```bash
chmod +x start-server.sh
./start-server.sh
```

Or with custom settings:
```bash
./start-server.sh 9050 1000 30
```

**Direct .NET command:**
```bash
dotnet run
```

## Using with Unity

### 1. Add RelayTransport Component

In your Unity scene:
1. Find or create your NetworkManager GameObject
2. Add the `RelayTransport` component
3. Configure the settings:
   - **Relay Address**: Your server address (e.g., `127.0.0.1` for local testing)
   - **Relay Port**: `9050` (default)
   - **Room Name**: A unique room code (leave empty for auto-generated)
   - **Max Clients**: Maximum players allowed (default: 10)

### 2. Example Usage (Host)

```csharp
using PurrNet.Transports;

public class GameManager : MonoBehaviour
{
    private RelayTransport relayTransport;
    
    void Start()
    {
        relayTransport = GetComponent<RelayTransport>();
        relayTransport.relayAddress = "127.0.0.1"; // or your server IP
        relayTransport.relayPort = 9050;
        relayTransport.roomName = "MYGAME123"; // Share this with clients
        relayTransport.maxClients = 10;
    }
    
    public void HostGame()
    {
        relayTransport.StartServer(); // Creates room on relay server
    }
}
```

### 3. Example Usage (Client)

```csharp
using PurrNet.Transports;

public class GameManager : MonoBehaviour
{
    private RelayTransport relayTransport;
    
    void Start()
    {
        relayTransport = GetComponent<RelayTransport>();
        relayTransport.relayAddress = "127.0.0.1"; // or your server IP
        relayTransport.relayPort = 9050;
        relayTransport.roomName = "MYGAME123"; // Room code from host
    }
    
    public void JoinGame()
    {
        relayTransport.StartClient(); // Joins room on relay server
    }
}
```

## Testing Locally

### 1. Start the Relay Server
```bash
cd C:\Users\admin\Projects\relay
dotnet run
```

You should see:
```
=== PurrNet Relay Server ===

Port: 9050
Max Rooms: 1000
Tick Rate: 30 Hz

Server started. Press Ctrl+C to stop.
```

### 2. Test with Unity

1. Open Unity project at `C:\Users\admin\Projects\Purnet`
2. Create a test scene with NetworkManager + RelayTransport
3. Set relay address to `127.0.0.1` and port to `9050`
4. Build the project
5. Run one instance as host (creates room)
6. Run another instance as client (joins room)

## Deploying to Production

### Option 1: Windows Server
```bash
# Build release
dotnet publish -c Release -r win-x64 --self-contained

# Run as Windows Service (requires NSSM or similar)
nssm install PurrNetRelay "C:\path\to\PurrNetRelayServer.exe"
nssm set PurrNetRelay AppParameters "--port 9050 --max-rooms 1000"
nssm start PurrNetRelay
```

### Option 2: Linux Server (systemd)
```bash
# Build release
dotnet publish -c Release -r linux-x64 --self-contained

# Copy to server
scp -r bin/Release/net8.0/linux-x64/publish/* user@server:/opt/purrnet-relay/

# Create systemd service (see README.md for full config)
sudo systemctl enable purrnet-relay
sudo systemctl start purrnet-relay
```

### Option 3: Docker
```bash
# Build Docker image
docker build -t purrnet-relay .

# Run container
docker run -d -p 9050:9050/udp --name purrnet-relay purrnet-relay

# View logs
docker logs -f purrnet-relay
```

## Common Issues

**"Port already in use"**
- Another application is using port 9050
- Use a different port: `dotnet run -- --port 9051`

**"Can't connect from Unity"**
- Check firewall settings
- Verify relay server is running
- Ensure relay address and port match in Unity

**"Room not found"**
- Host must create room before clients join
- Room names are case-sensitive
- Check server logs for errors

## Server Logs

The server outputs detailed logs:
```
Peer connected: 192.168.1.100:54321 (ID: 1)
Room created: 'MYGAME123' by 192.168.1.100:54321 (Max clients: 10)
Active rooms: 1/1000
Client 192.168.1.101:54322 joined room 'MYGAME123'
```

## Performance Tips

- **Local testing**: Use tick rate 30 Hz
- **Production**: Use tick rate 30-60 Hz depending on game needs
- **Large scale**: Consider multiple relay servers with load balancing
- **Monitoring**: Watch CPU and network usage, adjust max rooms accordingly

