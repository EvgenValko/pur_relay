using LiteNetLib;
using LiteNetLib.Utils;

namespace PurrNetRelayServer;

/// <summary>
/// Main relay server that handles room management and packet forwarding
/// </summary>
public class RelayServer : INetEventListener
{
    private readonly RelayServerConfig _config;
    private readonly NetManager _netManager;
    private readonly Dictionary<string, Room> _rooms = new();
    private readonly Dictionary<int, Room> _peerToRoom = new();
    private readonly object _lock = new();
    
    private int _totalConnections = 0;
    private int _totalRoomsCreated = 0;
    private DateTime _startTime;
    
    public bool IsRunning { get; private set; }

    public RelayServer(RelayServerConfig config)
    {
        _config = config;
        _netManager = new NetManager(this)
        {
            UnconnectedMessagesEnabled = false,
            PingInterval = 900,
            DisconnectTimeout = config.TimeoutSeconds * 1000,
            AutoRecycle = true
        };
    }

    public void Start()
    {
        if (IsRunning)
            return;

        if (!_netManager.Start(_config.Port))
        {
            Console.WriteLine($"Failed to start server on port {_config.Port}");
            return;
        }

        IsRunning = true;
        _startTime = DateTime.UtcNow;
        Console.WriteLine($"Relay server listening on port {_config.Port}");
    }

    public void Stop()
    {
        if (!IsRunning)
            return;

        IsRunning = false;
        _netManager.Stop();
        
        lock (_lock)
        {
            _rooms.Clear();
            _peerToRoom.Clear();
        }

        PrintStatistics();
    }

    public void Update()
    {
        if (!IsRunning)
            return;

        _netManager.PollEvents();
        
        // Clean up empty rooms periodically
        CleanupEmptyRooms();
    }

    private void CleanupEmptyRooms()
    {
        lock (_lock)
        {
            var roomsToRemove = new List<string>();
            
            foreach (var kvp in _rooms)
            {
                var room = kvp.Value;
                
                // Remove room if host disconnected and no clients, or if inactive for too long
                if (room.Host.ConnectionState != ConnectionState.Connected && room.ClientCount == 0)
                {
                    roomsToRemove.Add(kvp.Key);
                }
                else if (room.ClientCount == 0 && 
                         (DateTime.UtcNow - room.LastActivity).TotalMinutes > 5)
                {
                    roomsToRemove.Add(kvp.Key);
                }
            }

            foreach (var roomName in roomsToRemove)
            {
                _rooms.Remove(roomName);
                Console.WriteLine($"Removed inactive room: {roomName}");
            }
        }
    }

    public void OnPeerConnected(NetPeer peer)
    {
        _totalConnections++;
        Console.WriteLine($"Peer connected: {peer.Address}:{peer.Port} (ID: {peer.Id})");
    }

    public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
    {
        Console.WriteLine($"Peer disconnected: {peer.Address}:{peer.Port} (ID: {peer.Id}) - {disconnectInfo.Reason}");
        
        lock (_lock)
        {
            if (_peerToRoom.TryGetValue(peer.Id, out var room))
            {
                if (room.Host.Id == peer.Id)
                {
                    // Host disconnected - notify all clients and remove room
                    NotifyRoomClosed(room);
                    _rooms.Remove(room.RoomName);
                    
                    foreach (var clientId in room.GetAllClientIds())
                    {
                        _peerToRoom.Remove(clientId);
                    }
                    
                    Console.WriteLine($"Room '{room.RoomName}' closed (host disconnected)");
                }
                else
                {
                    // Client disconnected - notify host
                    room.RemoveClient(peer.Id);
                    NotifyClientDisconnected(room, peer.Id);
                }
                
                _peerToRoom.Remove(peer.Id);
            }
        }
    }

    public void OnNetworkError(System.Net.IPEndPoint endPoint, System.Net.Sockets.SocketError socketError)
    {
        Console.WriteLine($"Network error at {endPoint}: {socketError}");
    }

    public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channelNumber, DeliveryMethod deliveryMethod)
    {
        if (reader.AvailableBytes < 1)
        {
            reader.Recycle();
            return;
        }

        var packetType = (RelayPacketType)reader.GetByte();

        try
        {
            switch (packetType)
            {
                case RelayPacketType.CreateRoom:
                    HandleCreateRoom(peer, reader);
                    break;

                case RelayPacketType.JoinRoom:
                    HandleJoinRoom(peer, reader);
                    break;

                case RelayPacketType.LeaveRoom:
                    HandleLeaveRoom(peer);
                    break;

                case RelayPacketType.Data:
                    HandleData(peer, reader, deliveryMethod);
                    break;

                default:
                    Console.WriteLine($"Unknown packet type: {packetType}");
                    break;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error handling packet from {peer.Address}:{peer.Port}: {ex.Message}");
            SendError(peer, $"Server error: {ex.Message}");
        }

        reader.Recycle();
    }

    public void OnNetworkReceiveUnconnected(System.Net.IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType)
    {
        // Not used
    }

    public void OnNetworkLatencyUpdate(NetPeer peer, int latency)
    {
        // Not used
    }

    public void OnConnectionRequest(ConnectionRequest request)
    {
        if (request.Data.GetString() == "PurrRelay")
        {
            request.Accept();
        }
        else
        {
            request.Reject();
        }
    }

    private void HandleCreateRoom(NetPeer peer, NetPacketReader reader)
    {
        if (reader.AvailableBytes < 2)
        {
            SendError(peer, "Invalid create room packet");
            return;
        }

        string roomName = reader.GetString();
        int maxClients = reader.GetInt();

        if (string.IsNullOrWhiteSpace(roomName))
        {
            SendError(peer, "Room name cannot be empty");
            return;
        }

        if (maxClients <= 0 || maxClients > _config.MaxClientsPerRoom)
        {
            SendError(peer, $"Invalid max clients count. Must be between 1 and {_config.MaxClientsPerRoom}");
            return;
        }

        lock (_lock)
        {
            if (_rooms.Count >= _config.MaxRooms)
            {
                SendError(peer, "Server is full. Maximum rooms reached.");
                return;
            }

            if (_rooms.ContainsKey(roomName))
            {
                SendError(peer, "Room already exists");
                return;
            }

            if (_peerToRoom.ContainsKey(peer.Id))
            {
                SendError(peer, "Already in a room");
                return;
            }

            var room = new Room(roomName, peer, maxClients);
            _rooms[roomName] = room;
            _peerToRoom[peer.Id] = room;
            _totalRoomsCreated++;

            Console.WriteLine($"Room created: '{roomName}' by {peer.Address}:{peer.Port} (Max clients: {maxClients})");
        }

        // Send success response
        var writer = new NetDataWriter();
        writer.Put((byte)RelayPacketType.RoomCreated);
        peer.Send(writer, DeliveryMethod.ReliableOrdered);

        PrintRoomStats();
    }

    private void HandleJoinRoom(NetPeer peer, NetPacketReader reader)
    {
        if (reader.AvailableBytes < 1)
        {
            SendError(peer, "Invalid join room packet");
            return;
        }

        string roomName = reader.GetString();

        lock (_lock)
        {
            if (!_rooms.TryGetValue(roomName, out var room))
            {
                SendError(peer, "Room not found");
                return;
            }

            if (_peerToRoom.ContainsKey(peer.Id))
            {
                SendError(peer, "Already in a room");
                return;
            }

            if (room.IsFull)
            {
                SendError(peer, "Room is full");
                return;
            }

            if (!room.TryAddClient(peer))
            {
                SendError(peer, "Failed to join room");
                return;
            }

            _peerToRoom[peer.Id] = room;
            Console.WriteLine($"Client {peer.Address}:{peer.Port} joined room '{roomName}'");

            // Notify client of successful join
            var writer = new NetDataWriter();
            writer.Put((byte)RelayPacketType.RoomJoined);
            peer.Send(writer, DeliveryMethod.ReliableOrdered);

            // Notify host of new client
            NotifyClientConnected(room, peer.Id);
        }
    }

    private void HandleLeaveRoom(NetPeer peer)
    {
        lock (_lock)
        {
            if (!_peerToRoom.TryGetValue(peer.Id, out var room))
                return;

            if (room.Host.Id == peer.Id)
            {
                // Host leaving - close room
                NotifyRoomClosed(room);
                _rooms.Remove(room.RoomName);
                
                foreach (var clientId in room.GetAllClientIds())
                {
                    _peerToRoom.Remove(clientId);
                }
                
                Console.WriteLine($"Room '{room.RoomName}' closed (host left)");
            }
            else
            {
                // Client leaving
                room.RemoveClient(peer.Id);
                NotifyClientDisconnected(room, peer.Id);
                Console.WriteLine($"Client {peer.Address}:{peer.Port} left room '{room.RoomName}'");
            }

            _peerToRoom.Remove(peer.Id);
        }
    }

    private void HandleData(NetPeer peer, NetPacketReader reader, DeliveryMethod deliveryMethod)
    {
        lock (_lock)
        {
            if (!_peerToRoom.TryGetValue(peer.Id, out var room))
                return;

            room.LastActivity = DateTime.UtcNow;

            if (room.Host.Id == peer.Id)
            {
                // Host sending data to a client
                if (reader.AvailableBytes < 4)
                    return;

                int targetClientId = reader.GetInt();
                var targetClient = room.GetClient(targetClientId);
                
                if (targetClient != null && targetClient.ConnectionState == ConnectionState.Connected)
                {
                    var writer = new NetDataWriter();
                    writer.Put((byte)RelayPacketType.HostData);
                    writer.Put(reader.RawData, reader.Position, reader.AvailableBytes);
                    targetClient.Send(writer, deliveryMethod);
                }
            }
            else
            {
                // Client sending data to host
                if (room.Host.ConnectionState == ConnectionState.Connected)
                {
                    var writer = new NetDataWriter();
                    writer.Put((byte)RelayPacketType.ClientData);
                    writer.Put(peer.Id);
                    writer.Put(reader.RawData, reader.Position, reader.AvailableBytes);
                    room.Host.Send(writer, deliveryMethod);
                }
            }
        }
    }

    private void NotifyClientConnected(Room room, int clientId)
    {
        if (room.Host.ConnectionState != ConnectionState.Connected)
            return;

        var writer = new NetDataWriter();
        writer.Put((byte)RelayPacketType.ClientConnected);
        writer.Put(clientId);
        room.Host.Send(writer, DeliveryMethod.ReliableOrdered);
    }

    private void NotifyClientDisconnected(Room room, int clientId)
    {
        if (room.Host.ConnectionState != ConnectionState.Connected)
            return;

        var writer = new NetDataWriter();
        writer.Put((byte)RelayPacketType.ClientDisconnected);
        writer.Put(clientId);
        room.Host.Send(writer, DeliveryMethod.ReliableOrdered);
    }

    private void NotifyRoomClosed(Room room)
    {
        var clients = room.GetAllClients();
        foreach (var client in clients)
        {
            if (client.ConnectionState == ConnectionState.Connected)
            {
                SendError(client, "Room closed by host");
                client.Disconnect();
            }
        }
    }

    private void SendError(NetPeer peer, string errorMessage)
    {
        Console.WriteLine($"Error for {peer.Address}:{peer.Port}: {errorMessage}");
        
        var writer = new NetDataWriter();
        writer.Put((byte)RelayPacketType.Error);
        writer.Put(errorMessage);
        peer.Send(writer, DeliveryMethod.ReliableOrdered);
    }

    private void PrintRoomStats()
    {
        lock (_lock)
        {
            Console.WriteLine($"Active rooms: {_rooms.Count}/{_config.MaxRooms}");
        }
    }

    private void PrintStatistics()
    {
        var uptime = DateTime.UtcNow - _startTime;
        Console.WriteLine("\n=== Server Statistics ===");
        Console.WriteLine($"Uptime: {uptime:hh\\:mm\\:ss}");
        Console.WriteLine($"Total connections: {_totalConnections}");
        Console.WriteLine($"Total rooms created: {_totalRoomsCreated}");
        Console.WriteLine($"Active rooms: {_rooms.Count}");
        Console.WriteLine("========================\n");
    }
}

