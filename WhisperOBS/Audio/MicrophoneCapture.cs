using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace WhisperOBS.Audio;

/// <summary>
/// Captures microphone audio via NAudio, resamples to 16 kHz mono float32,
/// and handles high-frequency live visual updates alongside low-frequency transcription chunking.
/// </summary>
public sealed class MicrophoneCapture : IDisposable
{
    private const int SampleRate = 16_000;
    private const int Channels   = 1;

    public double ChunkSeconds { get; set; } = 3.0;

    /// <summary>
    /// Raised on a thread-pool thread with a complete long audio chunk ready for transcription (e.g., every 3s).
    /// </summary>
    public event Func<float[], Task>? AudioReady;

    /// <summary>
    /// FAST PATH CRITICAL: Raised instantly every 100ms with processed float samples for fluid UI visualization.
    /// </summary>
    public event Action<float[]>? LiveBufferAvailable;

    private readonly int _deviceIndex;
    private WaveInEvent? _waveIn;
    private WaveFormat?  _captureFormat;

    private readonly List<byte> _rawBuffer = new();
    private readonly object     _bufLock   = new();
    private int _samplesPerChunk;

    public MicrophoneCapture(int deviceIndex = 0)
    {
        _deviceIndex = deviceIndex;
    }

    public void Start()
    {
        _captureFormat = new WaveFormat(SampleRate, 16, Channels);

        try
        {
            _waveIn = CreateWaveIn(_captureFormat);
            _waveIn.StartRecording();
        }
        catch
        {
            _waveIn?.Dispose();
            _captureFormat = new WaveFormat(44_100, 16, 2);
            _waveIn = CreateWaveIn(_captureFormat);
            _waveIn.StartRecording();
        }

        _samplesPerChunk = (int)(ChunkSeconds * _captureFormat.SampleRate * _captureFormat.Channels);
        Console.WriteLine($"[Mic] Capturing at {_captureFormat.SampleRate} Hz, " +
                          $"{_captureFormat.Channels}ch – chunk = {ChunkSeconds}s");
    }

    private WaveInEvent CreateWaveIn(WaveFormat format)
    {
        var wi = new WaveInEvent
        {
            DeviceNumber = _deviceIndex,
            WaveFormat   = format,
            BufferMilliseconds = 100 // Visuals get fed every 100ms
        };
        wi.DataAvailable    += OnDataAvailable;
        wi.RecordingStopped += (_, e) =>
        {
            if (e.Exception is not null)
                Console.Error.WriteLine($"[Mic] Recording error: {e.Exception.Message}");
        };
        return wi;
    }

    private void OnDataAvailable(object? sender, WaveInEventArgs e)
    {
        if (e.BytesRecorded == 0) return;

        // ── 1. FAST PATH: Convert the immediate 100ms incoming buffer for visuals ──
        byte[] immediateBuffer = new byte[e.BytesRecorded];
        Buffer.BlockCopy(e.Buffer, 0, immediateBuffer, 0, e.BytesRecorded);

        // Convert and broadcast immediately (Non-blocking)
        _ = Task.Run(() =>
        {
            float[] livePcm = ConvertToWhisperPcm(immediateBuffer, _captureFormat!);
            LiveBufferAvailable?.Invoke(livePcm);
        });

        // ── 2. SLOW PATH: Accumulate raw bytes into long transcription chunk ──
        lock (_bufLock)
        {
            _rawBuffer.AddRange(immediateBuffer);

            int bytesPerSample = _captureFormat!.BitsPerSample / 8;
            int bytesNeeded    = _samplesPerChunk * bytesPerSample;

            if (_rawBuffer.Count >= bytesNeeded)
            {
                byte[] chunk = _rawBuffer.GetRange(0, bytesNeeded).ToArray();
                _rawBuffer.RemoveRange(0, bytesNeeded);

                _ = Task.Run(async () =>
                {
                    float[] pcm = ConvertToWhisperPcm(chunk, _captureFormat);
                    if (AudioReady is not null)
                        await AudioReady(pcm);
                });
            }
        }
    }

    private static float[] ConvertToWhisperPcm(byte[] raw, WaveFormat srcFormat)
    {
        float[] srcSamples = Pcm16ToFloat(raw);

        float[] mono = srcFormat.Channels == 1 ? srcSamples : StereoToMono(srcSamples);

        return srcFormat.SampleRate == SampleRate
            ? mono
            : Resample(mono, srcFormat.SampleRate, SampleRate);
    }

    private static float[] Pcm16ToFloat(byte[] raw)
    {
        int count = raw.Length / 2;
        float[] samples = new float[count];
        for (int i = 0; i < count; i++)
            samples[i] = BitConverter.ToInt16(raw, i * 2) / 32768f;
        return samples;
    }

    private static float[] StereoToMono(float[] stereo)
    {
        float[] mono = new float[stereo.Length / 2];
        for (int i = 0; i < mono.Length; i++)
            mono[i] = (stereo[i * 2] + stereo[i * 2 + 1]) * 0.5f;
        return mono;
    }

    private static float[] Resample(float[] src, int srcRate, int dstRate)
    {
        double ratio    = (double)srcRate / dstRate;
        int    dstCount = (int)(src.Length / ratio);
        float[] dst     = new float[dstCount];

        for (int i = 0; i < dstCount; i++)
        {
            double srcPos = i * ratio;
            int    lo     = (int)srcPos;
            int    hi     = Math.Min(lo + 1, src.Length - 1);
            double t      = srcPos - lo;
            dst[i] = (float)(src[lo] * (1 - t) + src[hi] * t);
        }
        return dst;
    }

    public void Stop() => _waveIn?.StopRecording();
    public void Dispose() => _waveIn?.Dispose();
}