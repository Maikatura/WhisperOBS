using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace WhisperOBS.Server;

public sealed class CaptionServer : IDisposable
{
    private readonly int _port;
    private readonly HttpListener _listener = new();
    private readonly WebSocketManager _wsManager = new();
    private readonly string _overlayTemplate;

    public CaptionServer(int port)
    {
        _port = port;
        string path = Path.Combine(AppContext.BaseDirectory, "wwwroot", "overlay.html");
        _overlayTemplate = File.Exists(path) ? File.ReadAllText(path) : "<html>Overlay missing</html>";
    }

    public async Task StartAsync()
    {
        _listener.Prefixes.Add($"http://localhost:{_port}/");
        _listener.Start();
        while (_listener.IsListening)
        {
            var ctx = await _listener.GetContextAsync();
            _ = HandleContextAsync(ctx);
        }
    }

    private async Task HandleContextAsync(HttpListenerContext ctx)
    {
        string path = ctx.Request.Url!.LocalPath;

        if (path == "/ws" && ctx.Request.IsWebSocketRequest)
        {
            var wsCtx = await ctx.AcceptWebSocketAsync(null);
            var id = Guid.NewGuid();
            _wsManager.AddClient(id, wsCtx.WebSocket);
            
            var buffer = new byte[1024];
            while (wsCtx.WebSocket.State == WebSocketState.Open)
                await wsCtx.WebSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

            _wsManager.RemoveClient(id);
        }
        else
        {
            if (path == "/") path = "/overlay.html";

            string filePath = Path.Combine(AppContext.BaseDirectory, "wwwroot", path.TrimStart('/'));

            if (File.Exists(filePath))
            {
                byte[] buffer = File.ReadAllBytes(filePath);
                ctx.Response.ContentType = path.EndsWith(".css") ? "text/css" :
                    path.EndsWith(".js") ? "text/javascript" : "text/html";
                ctx.Response.OutputStream.Write(buffer, 0, buffer.Length);
            }

            ctx.Response.Close();
        }
    }

    public async Task SendCaptionAsync(string text)
    {
        var payload = JsonSerializer.Serialize(new { type = "caption", text });
        await _wsManager.BroadcastAsync(payload, CancellationToken.None);
    }

    public void Dispose()
    {
        _listener.Stop();
        _listener.Close();
    }

    public int GetPort()
    {
        return _port;
    }
}