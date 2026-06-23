using NAudio.Wave;

namespace WhisperOBS.Audio;

/// <summary>
/// Captures microphone audio via NAudio, resamples to 16 kHz mono float32,
/// and raises <see cref="AudioReady"/> every <see cref="ChunkSeconds"/> seconds.
/// </summary>
public sealed class MicrophoneCapture : IDisposable
{
    // Whisper expects 16 kHz mono float32
    private const int SampleRate  = 16_000;
    private const int Channels    = 1;

    /// <summary>How many seconds of audio to collect before transcribing.</summary>
    public double ChunkSeconds { get; set; } = 3.0;

    /// <summary>
    /// Raised on a thread-pool thread with a complete audio chunk ready for transcription.
    /// </summary>
    public event Func<float[], Task>? AudioReady;

    private readonly int _deviceIndex;
    private WaveInEvent?       _waveIn;
    private WaveFormat?        _captureFormat;

    // Ring buffer accumulating raw bytes from the mic
    private readonly List<byte> _rawBuffer = new();
    private readonly object     _bufLock   = new();
    private int _samplesPerChunk;

    public MicrophoneCapture(int deviceIndex = 0)
    {
        _deviceIndex = deviceIndex;
    }

    public void Start()
    {
        var caps = WaveInEvent.GetCapabilities(_deviceIndex);

        // Prefer 16 kHz mono if the device supports it, otherwise use 44.1 kHz stereo
        // and resample during conversion.
        _captureFormat = new WaveFormat(SampleRate, 16, Channels);

        // Some devices don't support 16 kHz; fall back to 44100 stereo
        try
        {
            _waveIn = CreateWaveIn(_captureFormat);
            _waveIn.StartRecording(); // test – will throw if format unsupported
        }
        catch
        {
            _waveIn?.Dispose();
            _captureFormat = new WaveFormat(44_100, 16, 2);
            _waveIn = CreateWaveIn(_captureFormat);
            _waveIn.StartRecording();
        }

        // How many raw bytes equal one chunk?
        int bytesPerSample = _captureFormat.BitsPerSample / 8;
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
            BufferMilliseconds = 100
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
        lock (_bufLock)
        {
            for (int i = 0; i < e.BytesRecorded; i++)
                _rawBuffer.Add(e.Buffer[i]);

            int bytesPerSample  = _captureFormat!.BitsPerSample / 8;
            int bytesPerFrame   = bytesPerSample * _captureFormat.Channels;
            int bytesNeeded     = _samplesPerChunk * bytesPerSample;

            if (_rawBuffer.Count >= bytesNeeded)
            {
                byte[] chunk = _rawBuffer.GetRange(0, bytesNeeded).ToArray();
                _rawBuffer.RemoveRange(0, bytesNeeded);

                // Process off the hot path
                _ = Task.Run(async () =>
                {
                    float[] pcm = ConvertToWhisperPcm(chunk, _captureFormat);
                    if (AudioReady is not null)
                        await AudioReady(pcm);
                });
            }
        }
    }

    /// <summary>
    /// Converts raw PCM bytes from the capture format to 16 kHz mono float32.
    /// </summary>
    private static float[] ConvertToWhisperPcm(byte[] raw, WaveFormat srcFormat)
    {
        // 1. Decode to float samples (handles 16-bit and 32-bit PCM)
        float[] srcSamples = Pcm16ToFloat(raw);

        // 2. Down-mix to mono if needed
        float[] mono = srcFormat.Channels == 1
            ? srcSamples
            : StereoToMono(srcSamples);

        // 3. Resample to 16 kHz if needed
        float[] resampled = srcFormat.SampleRate == SampleRate
            ? mono
            : Resample(mono, srcFormat.SampleRate, SampleRate);

        return resampled;
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

    /// <summary>Linear interpolation resampler – adequate quality for speech.</summary>
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

    public void Stop()
    {
        _waveIn?.StopRecording();
    }

    public void Dispose()
    {
        _waveIn?.Dispose();
    }
}