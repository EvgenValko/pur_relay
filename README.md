# PurrNet Relay Server

A standalone relay server for PurrNet using LiteNetLib. This server facilitates connections between hosts and clients when direct peer-to-peer connections are not possible (NAT traversal, firewall issues, etc.).

## Features

- **Room-based architecture**: Hosts create rooms, clients join them
- **Efficient packet forwarding**: Low-latency relay between host and clients
- **Configurable limits**: Maximum rooms, clients per room, etc.
- **Automatic cleanup**: Removes inactive rooms and disconnected peers
- **Cross-platform**: Runs on Windows, Linux, and macOS

## Requirements

- .NET 8.0 or higher
- LiteNetLib 1.2.0+

## Building

```bash
dotnet build -c Release
```

## Running

### Basic Usage

```bash
dotnet run
```

### With Custom Port

```bash
dotnet run -- --port 9050
```

### All Options

```bash
dotnet run -- --port 9050 --max-rooms 1000 --tick-rate 30
```

Or after building:

```bash
./PurrNetRelayServer --port 9050 --max-rooms 1000 --tick-rate 30
```

## Command Line Options

| Option | Short | Description | Default |
|--------|-------|-------------|---------|
| `--port` | `-p` | Port to listen on | 9050 |
| `--max-rooms` | `-r` | Maximum number of concurrent rooms | 1000 |
| `--tick-rate` | `-t` | Server tick rate in Hz | 30 |
| `--help` | `-h` | Show help message | - |

## Protocol

The relay server uses a simple binary protocol over UDP with the following packet types:

### Client → Relay

- **CreateRoom** (0): Create a new room
  - `string roomName`
  - `int maxClients`
  
- **JoinRoom** (1): Join an existing room
  - `string roomName`
  
- **LeaveRoom** (2): Leave current room
  
- **Data** (3): Send data to host or client
  - For host: `int targetClientId` + payload
  - For client: payload only

### Relay → Client

- **RoomCreated** (10): Room successfully created
  
- **RoomJoined** (11): Successfully joined room
  
- **ClientConnected** (12): New client joined (sent to host)
  - `int clientId`
  
- **ClientDisconnected** (13): Client left (sent to host)
  - `int clientId`
  
- **HostData** (14): Data from host (sent to clients)
  - payload
  
- **ClientData** (15): Data from client (sent to host)
  - `int clientId` + payload
  
- **Error** (16): Error message
  - `string errorMessage`

## Configuration

You can modify the following settings in `RelayServerConfig.cs`:

```csharp
public class RelayServerConfig
{
    public int Port { get; set; } = 9050;
    public int MaxRooms { get; set; } = 1000;
    public int TickRate { get; set; } = 30;
    public int TimeoutSeconds { get; set; } = 10;
    public int MaxClientsPerRoom { get; set; } = 100;
}
```

## Unity Integration

To use this relay server with Unity, use the `RelayTransport` component:

1. Add `RelayTransport` component to your NetworkManager GameObject
2. Set the relay address and port (default: 127.0.0.1:9050)
3. Set a room name (or leave empty for auto-generated)
4. Start server (creates room) or client (joins room)

### Example Unity Setup

```csharp
var relayTransport = GetComponent<RelayTransport>();
relayTransport.relayAddress = "your-relay-server.com";
relayTransport.relayPort = 9050;
relayTransport.roomName = "MyGameRoom";
relayTransport.maxClients = 10;

// For host
relayTransport.StartServer();

// For client
relayTransport.StartClient();
```

## Performance Considerations

- **Tick Rate**: Higher tick rates (e.g., 60 Hz) provide lower latency but use more CPU
- **Max Rooms**: Each room consumes minimal memory, but having many active rooms will increase CPU usage
- **Network Bandwidth**: The relay forwards all packets, so bandwidth scales with the number of active connections

## Deployment

### Linux (systemd service)

Create `/etc/systemd/system/purrnet-relay.service`:

```ini
[Unit]
Description=PurrNet Relay Server
After=network.target

[Service]
Type=simple
User=relay
WorkingDirectory=/opt/purrnet-relay
ExecStart=/usr/bin/dotnet /opt/purrnet-relay/PurrNetRelayServer.dll --port 9050
Restart=always
RestartSec=10

[Install]
WantedBy=multi-user.target
```

Then:
```bash
sudo systemctl daemon-reload
sudo systemctl enable purrnet-relay
sudo systemctl start purrnet-relay
```

### Docker

Create `Dockerfile`:

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY *.csproj .
RUN dotnet restore
COPY . .
RUN dotnet publish -c Release -o /app

FROM mcr.microsoft.com/dotnet/runtime:8.0
WORKDIR /app
COPY --from=build /app .
EXPOSE 9050/udp
ENTRYPOINT ["dotnet", "PurrNetRelayServer.dll"]
```

Build and run:
```bash
docker build -t purrnet-relay .
docker run -d -p 9050:9050/udp purrnet-relay --port 9050
```

## Monitoring

The server outputs the following information to console:

- Connection/disconnection events
- Room creation/deletion
- Active room count
- Errors and warnings

On shutdown (Ctrl+C), it displays statistics:
- Total uptime
- Total connections
- Total rooms created
- Active rooms

## Security Considerations

- The server does not implement authentication by default
- Consider adding authentication in production environments
- Use firewalls to restrict access to trusted IPs if needed
- Monitor for potential DDoS attacks

## Troubleshooting

### "Failed to start server on port XXXX"

- Port is already in use by another application
- Insufficient permissions (ports < 1024 require root on Linux)
- Firewall blocking the port

### High CPU usage

- Reduce tick rate
- Check for excessive room creation/deletion
- Monitor network traffic

### Clients can't connect

- Check firewall rules
- Ensure port forwarding is configured correctly
- Verify relay address and port are correct in Unity

## License

This project is part of PurrNet and follows the same license.

## Support

For issues and questions, please refer to the main PurrNet repository.

