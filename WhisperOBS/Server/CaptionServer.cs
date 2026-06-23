using System.Collections.Concurrent;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace WhisperOBS.Server;

public sealed class CaptionServer
{
    private readonly int _port;
    private readonly ConcurrentDictionary<Guid, WebSocket> _clients = new();
    private readonly string _overlayHtml;

    private static readonly Dictionary<string, string> DefaultStyles = new()
    {
        { "font-size", "2.6rem" },
        { "font-weight", "700" },
        { "color", "#FFFFFF" },
        { "background", "rgba(0, 0, 0, 0.45)" },
        { "bottom-position", "60px" },
        { "text-shadow", "-2px -2px 0 #000, 2px -2px 0 #000, -2px 2px 0 #000, 2px 2px 0 #000, 0 4px 8px rgba(0,0,0,0.6)" }
    };

    public CaptionServer(int port, Dictionary<string, string>? customStyles = null, int fadeDelayMs = 4000, int appendWindowMs = 1400)
    {
        _port = port;
        
        var styles = new Dictionary<string, string>(DefaultStyles);
        if (customStyles != null)
        {
            foreach (var (key, value) in customStyles)
                styles[key] = value;
        }

        _overlayHtml = LoadOverlay(styles, fadeDelayMs, appendWindowMs);
    }

    public async Task StartAsync()
    {
        var listener = new HttpListener();
        listener.Prefixes.Add($"http://localhost:{_port}/");
        listener.Start();
        Console.WriteLine($"[Server] Listening on http://localhost:{_port}/");

        while (true)
        {
            HttpListenerContext ctx;
            try { ctx = await listener.GetContextAsync(); }
            catch { break; }

            _ = HandleContextAsync(ctx);
        }
    }

    private async Task HandleContextAsync(HttpListenerContext ctx)
    {
        if (ctx.Request.IsWebSocketRequest && ctx.Request.RawUrl == "/ws")
        {
            try
            {
                var wsCtx = await ctx.AcceptWebSocketAsync(null);
                _ = Task.Run(() => HandleWebSocketAsync(wsCtx.WebSocket));
            }
            catch
            {
                ctx.Response.StatusCode = 500;
                ctx.Response.Close();
            }
        }
        else
        {
            byte[] bytes = Encoding.UTF8.GetBytes(_overlayHtml);
            ctx.Response.ContentType     = "text/html; charset=utf-8";
            ctx.Response.ContentLength64 = bytes.Length;
            await ctx.Response.OutputStream.WriteAsync(bytes);
            ctx.Response.Close();
        }
    }

    private async Task HandleWebSocketAsync(WebSocket ws)
    {
        var id = Guid.NewGuid();
        _clients[id] = ws;
        Console.WriteLine($"[WS] Client connected ({_clients.Count} total)");

        var buf = new byte[1024];
        try
        {
            while (ws.State == WebSocketState.Open)
            {
                var result = await ws.ReceiveAsync(buf, CancellationToken.None);
                if (result.MessageType == WebSocketMessageType.Close)
                    break;
            }
        }
        catch { }
        finally
        {
            _clients.TryRemove(id, out _);
            Console.WriteLine($"[WS] Client disconnected ({_clients.Count} total)");
            try { await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "bye", CancellationToken.None); }
            catch { }
        }
    }

    public async Task BroadcastAsync(string text)
    {
        if (_clients.IsEmpty) return;

        var payload = JsonSerializer.Serialize(new { type = "caption", text });
        var bytes   = Encoding.UTF8.GetBytes(payload);
        var segment = new ArraySegment<byte>(bytes);

        var dead = new List<Guid>();
        foreach (var (id, ws) in _clients)
        {
            try
            {
                if (ws.State == WebSocketState.Open)
                    await ws.SendAsync(segment, WebSocketMessageType.Text, true, CancellationToken.None);
                else
                    dead.Add(id);
            }
            catch { dead.Add(id); }
        }
        foreach (var id in dead) _clients.TryRemove(id, out _);
    }

    private string LoadOverlay(Dictionary<string, string> styles, int fadeDelay, int appendWindow)
    {
        string external = Path.Combine(AppContext.BaseDirectory, "wwwroot", "overlay.html");
        if (File.Exists(external))
            return File.ReadAllText(external);

        var sb = new StringBuilder();
        sb.AppendLine(":root {");
        foreach (var (key, value) in styles)
        {
            sb.AppendLine($"  --caption-{key}: {value};");
        }
        sb.AppendLine("}");

        string processing = EmbeddedOverlayHtml.Replace("/* [[DYNAMIC_STYLES]] */", sb.ToString());
        processing = processing.Replace("[[FADE_DELAY]]", fadeDelay.ToString());
        processing = processing.Replace("[[APPEND_WINDOW]]", appendWindow.ToString());

        return processing;
    }

    // ── Overlay HTML ─────────────────────────────────────────────────────────

    private const string EmbeddedOverlayHtml = """
        <!DOCTYPE html>
        <html lang="en">
        <head>
          <meta charset="UTF-8"/>
          <meta name="viewport" content="width=device-width, initial-scale=1.0"/>
          <title>WhisperOBS Captions</title>
          <style>
            /* [[DYNAMIC_STYLES]] */

            *, *::before, *::after { box-sizing: border-box; margin: 0; padding: 0; }

            html, body {
              width: 100%; height: 100%;
              background: transparent;
              overflow: hidden;
            }

            #caption-wrap {
              position: fixed;
              bottom: var(--caption-bottom-position);
              left: 50%;
              transform: translateX(-50%);
              width: 90%;
              max-width: 1400px;
              text-align: center;
              pointer-events: none;
            }

            #caption {
              display: inline-block;
              font-family: 'Segoe UI', 'Arial', sans-serif;
              font-size: var(--caption-font-size);
              font-weight: var(--caption-font-weight);
              line-height: 1.35;
              color: var(--caption-color);
              text-shadow: var(--caption-text-shadow);
              padding: 0.25em 0.6em;
              border-radius: 6px;
              background: var(--caption-background);
              backdrop-filter: blur(4px);
              opacity: 0;
              transition: opacity 0.25s ease;
            }

            #caption.visible { opacity: 1; }
          </style>
        </head>
        <body>
          <div id="caption-wrap"><span id="caption"></span></div>

          <script>
            const captionEl = document.getElementById('caption');
            
            let hideTimer   = null;
            let lastMessageTime = 0;

            const SHOW_DURATION  = [[FADE_DELAY]];
            const APPEND_WINDOW  = [[APPEND_WINDOW]];

            function showCaption(text) {
              const now = Date.now();
              
              if (captionEl.classList.contains('visible') && (now - lastMessageTime < APPEND_WINDOW)) {
                captionEl.textContent += " " + text;
              } else {
                captionEl.textContent = text;
              }

              lastMessageTime = now;
              captionEl.classList.add('visible');

              clearTimeout(hideTimer);
              hideTimer = setTimeout(() => { 
                captionEl.classList.remove('visible'); 
              }, SHOW_DURATION);
            }

            let ws;
            function connect() {
              ws = new WebSocket(`ws://${location.host}/ws`);
              ws.onmessage = (evt) => {
                try {
                  const msg = JSON.parse(evt.data);
                  if (msg.type === 'caption') showCaption(msg.text);
                } catch {}
              };
              ws.onclose = ws.onerror = () => {
                setTimeout(connect, 2000);
              };
            }
            connect();
          </script>
        </body>
        </html>
        """;
}