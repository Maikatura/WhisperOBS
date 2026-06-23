using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;

namespace WhisperOBS.Server;

public sealed class WebSocketManager
{
    private readonly ConcurrentDictionary<Guid, WebSocket> _clients = new();

    public void AddClient(Guid id, WebSocket ws) => _clients[id] = ws;
    public void RemoveClient(Guid id) => _clients.TryRemove(id, out _);

    public async Task BroadcastAsync(string payload, CancellationToken ct)
    {
        var bytes = Encoding.UTF8.GetBytes(payload);
        var segment = new ArraySegment<byte>(bytes);

        foreach (var (id, ws) in _clients)
        {
            // Skip clients that aren't ready
            if (ws.State != WebSocketState.Open) continue;

            try 
            {
                await ws.SendAsync(segment, WebSocketMessageType.Text, true, ct);
            }
            catch (WebSocketException) 
            {
                _clients.TryRemove(id, out _);
            }
        }
    }
}