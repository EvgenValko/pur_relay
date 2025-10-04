namespace PurrNetRelayServer;

/// <summary>
/// Packet types for relay protocol communication
/// </summary>
public enum RelayPacketType : byte
{
    // Client -> Relay
    CreateRoom = 0,
    JoinRoom = 1,
    LeaveRoom = 2,
    Data = 3,
    
    // Relay -> Client
    RoomCreated = 10,
    RoomJoined = 11,
    ClientConnected = 12,
    ClientDisconnected = 13,
    HostData = 14,
    ClientData = 15,
    Error = 16
}

