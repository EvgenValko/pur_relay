using LiteNetLib;
using PurrNetRelayServer;

Console.WriteLine("=== PurrNet Relay Server ===");
Console.WriteLine();

// Parse command line arguments
int port = 9050;
int maxRooms = 1000;
int tickRate = 30;
int httpPort = 8080;

for (int i = 0; i < args.Length; i++)
{
    switch (args[i])
    {
        case "-p" or "--port":
            if (i + 1 < args.Length && int.TryParse(args[i + 1], out int p))
                port = p;
            break;
        case "-r" or "--max-rooms":
            if (i + 1 < args.Length && int.TryParse(args[i + 1], out int r))
                maxRooms = r;
            break;
        case "-t" or "--tick-rate":
            if (i + 1 < args.Length && int.TryParse(args[i + 1], out int t))
                tickRate = t;
            break;
        case "--http-port":
            if (i + 1 < args.Length && int.TryParse(args[i + 1], out int h))
                httpPort = h;
            break;
        case "-h" or "--help":
            Console.WriteLine("Usage: PurrNetRelayServer [options]");
            Console.WriteLine();
            Console.WriteLine("Options:");
            Console.WriteLine("  -p, --port <port>         UDP port to listen on (default: 9050)");
            Console.WriteLine("  -r, --max-rooms <rooms>   Maximum number of rooms (default: 1000)");
            Console.WriteLine("  -t, --tick-rate <rate>    Server tick rate in Hz (default: 30)");
            Console.WriteLine("  --http-port <port>        HTTP port for health checks (default: 8080)");
            Console.WriteLine("  -h, --help                Show this help message");
            return;
    }
}

var config = new RelayServerConfig
{
    Port = port,
    MaxRooms = maxRooms,
    TickRate = tickRate
};

Console.WriteLine($"UDP Port: {config.Port}");
Console.WriteLine($"HTTP Port: {httpPort}");
Console.WriteLine($"Max Rooms: {config.MaxRooms}");
Console.WriteLine($"Tick Rate: {config.TickRate} Hz");
Console.WriteLine();

var server = new RelayServer(config);
var healthServer = new HealthCheckServer(server, httpPort);

// Handle Ctrl+C gracefully
Console.CancelKeyPress += (sender, e) =>
{
    e.Cancel = true;
    Console.WriteLine("\nShutting down server...");
    healthServer.Stop();
    server.Stop();
};

server.Start();
healthServer.Start();

Console.WriteLine("Server started. Press Ctrl+C to stop.");
Console.WriteLine();

// Main loop
while (server.IsRunning)
{
    server.Update();
    Thread.Sleep(1000 / config.TickRate);
}

healthServer.Stop();
Console.WriteLine("Server stopped.");

