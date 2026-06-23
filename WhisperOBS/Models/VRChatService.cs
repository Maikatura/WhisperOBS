using System.Net;
using System.Net.Sockets;
using System.Text;

namespace WhisperOBS.Services;

public sealed class VRChatService : IDisposable
{
    private readonly UdpClient _udpClient;
    private readonly IPEndPoint _vrcEndPoint;

    public VRChatService()
    {
        _udpClient = new UdpClient();
        _vrcEndPoint = new IPEndPoint(IPAddress.Loopback, 9001);
    }

    /// <summary>
    /// Broadcasts a string to the VRChat chatbox.
    /// </summary>
    public async Task SendCaptionAsync(string text)
    {
        var packet = new List<byte>();
        packet.AddRange(EncodeOscString("/chatbox/input"));
        packet.AddRange(EncodeOscString(",sTT"));
        packet.AddRange(EncodeOscString(text)); 
        
        byte[] data = packet.ToArray();
        await _udpClient.SendAsync(data, data.Length, new IPEndPoint(IPAddress.Loopback, 9000));
    }

    private byte[] EncodeOscString(string str)
    {
        var bytes = Encoding.UTF8.GetBytes(str + "\0");
        var padded = new byte[(bytes.Length + 3) & ~3];
        Buffer.BlockCopy(bytes, 0, padded, 0, bytes.Length);
        return padded;
    }

    public void Dispose() => _udpClient.Dispose();
}