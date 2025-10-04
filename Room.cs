using LiteNetLib;

namespace PurrNetRelayServer;

/// <summary>
/// Represents a game room with a host and clients
/// </summary>
public class Room
{
    public string RoomName { get; }
    public NetPeer Host { get; }
    public int MaxClients { get; }
    public DateTime CreatedAt { get; }
    public DateTime LastActivity { get; set; }
    
    private readonly Dictionary<int, NetPeer> _clients = new();
    private readonly object _lock = new();

    public Room(string roomName, NetPeer host, int maxClients)
    {
        RoomName = roomName;
        Host = host;
        MaxClients = maxClients;
        CreatedAt = DateTime.UtcNow;
        LastActivity = DateTime.UtcNow;
    }

    public int ClientCount
    {
        get
        {
            lock (_lock)
            {
                return _clients.Count;
            }
        }
    }

    public bool IsFull
    {
        get
        {
            lock (_lock)
            {
                return _clients.Count >= MaxClients;
            }
        }
    }

    public bool TryAddClient(NetPeer client)
    {
        lock (_lock)
        {
            if (_clients.Count >= MaxClients)
                return false;

            if (_clients.ContainsKey(client.Id))
                return false;

            _clients[client.Id] = client;
            LastActivity = DateTime.UtcNow;
            return true;
        }
    }

    public bool RemoveClient(int clientId)
    {
        lock (_lock)
        {
            bool removed = _clients.Remove(clientId);
            if (removed)
                LastActivity = DateTime.UtcNow;
            return removed;
        }
    }

    public bool ContainsClient(int clientId)
    {
        lock (_lock)
        {
            return _clients.ContainsKey(clientId);
        }
    }

    public NetPeer? GetClient(int clientId)
    {
        lock (_lock)
        {
            return _clients.TryGetValue(clientId, out var peer) ? peer : null;
        }
    }

    public List<NetPeer> GetAllClients()
    {
        lock (_lock)
        {
            return new List<NetPeer>(_clients.Values);
        }
    }

    public List<int> GetAllClientIds()
    {
        lock (_lock)
        {
            return new List<int>(_clients.Keys);
        }
    }
}

