namespace PurrNetRelayServer;

/// <summary>
/// Configuration for the relay server
/// </summary>
public class RelayServerConfig
{
    /// <summary>
    /// Port to listen on
    /// </summary>
    public int Port { get; set; } = 9050;

    /// <summary>
    /// Maximum number of concurrent rooms
    /// </summary>
    public int MaxRooms { get; set; } = 1000;

    /// <summary>
    /// Server tick rate in Hz
    /// </summary>
    public int TickRate { get; set; } = 30;

    /// <summary>
    /// Connection timeout in seconds
    /// </summary>
    public int TimeoutSeconds { get; set; } = 10;

    /// <summary>
    /// Maximum clients per room
    /// </summary>
    public int MaxClientsPerRoom { get; set; } = 100;
}

