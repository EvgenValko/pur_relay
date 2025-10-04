using System.Net;
using System.Text;

namespace PurrNetRelayServer;

/// <summary>
/// Simple HTTP server for health checks and status monitoring
/// </summary>
public class HealthCheckServer
{
    private readonly HttpListener _listener;
    private readonly RelayServer _relayServer;
    private readonly int _httpPort;
    private bool _isRunning;
    private Thread? _listenerThread;

    public HealthCheckServer(RelayServer relayServer, int httpPort = 8080)
    {
        _relayServer = relayServer;
        _httpPort = httpPort;
        _listener = new HttpListener();
    }

    public void Start()
    {
        try
        {
            _listener.Prefixes.Add($"http://*:{_httpPort}/");
            _listener.Start();
            _isRunning = true;

            _listenerThread = new Thread(HandleRequests)
            {
                IsBackground = true
            };
            _listenerThread.Start();

            Console.WriteLine($"Health check server listening on HTTP port {_httpPort}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to start health check server: {ex.Message}");
        }
    }

    public void Stop()
    {
        _isRunning = false;
        _listener.Stop();
        _listener.Close();
    }

    private void HandleRequests()
    {
        while (_isRunning)
        {
            try
            {
                var context = _listener.GetContext();
                var request = context.Request;
                var response = context.Response;

                string responseString = "";
                int statusCode = 200;

                switch (request.Url?.AbsolutePath)
                {
                    case "/":
                    case "/health":
                        responseString = GetHealthResponse();
                        response.ContentType = "application/json";
                        break;

                    case "/status":
                        responseString = GetStatusResponse();
                        response.ContentType = "application/json";
                        break;

                    case "/ping":
                        responseString = "pong";
                        response.ContentType = "text/plain";
                        break;

                    default:
                        responseString = "Not Found";
                        statusCode = 404;
                        response.ContentType = "text/plain";
                        break;
                }

                response.StatusCode = statusCode;
                var buffer = Encoding.UTF8.GetBytes(responseString);
                response.ContentLength64 = buffer.Length;
                response.OutputStream.Write(buffer, 0, buffer.Length);
                response.OutputStream.Close();
            }
            catch (HttpListenerException)
            {
                // Listener stopped
                break;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Health check error: {ex.Message}");
            }
        }
    }

    private string GetHealthResponse()
    {
        bool isHealthy = _relayServer.IsRunning;
        return $"{{\"status\":\"{(isHealthy ? "healthy" : "unhealthy")}\",\"relay\":\"running\"}}";
    }

    private string GetStatusResponse()
    {
        var stats = _relayServer.GetStatistics();
        return $@"{{
  ""status"": ""healthy"",
  ""relay"": {{
    ""running"": {(_relayServer.IsRunning ? "true" : "false")},
    ""activeRooms"": {stats.ActiveRooms},
    ""totalConnections"": {stats.TotalConnections},
    ""totalRooms"": {stats.TotalRoomsCreated},
    ""uptime"": ""{stats.Uptime}""
  }}
}}";
    }
}

