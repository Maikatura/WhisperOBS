using Whisper.net;
using Whisper.net.Ggml;

namespace WhisperOBS.Models;

/// <summary>
/// Wraps Whisper.net to provide async transcription of raw 16 kHz mono PCM float32 audio.
/// </summary>
public sealed class WhisperService : IDisposable
{
    private readonly string _modelPath;
    private readonly string _language;

    private WhisperFactory?    _factory;
    private WhisperProcessor?  _processor;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public WhisperService(string modelPath, string language)
    {
        _modelPath = modelPath;
        _language  = language;
    }

    public async Task InitAsync()
    {
        await Task.Run(() =>
        {
            _factory = WhisperFactory.FromPath(_modelPath);

            var builder = _factory.CreateBuilder()
                .WithThreads(Math.Max(1, Environment.ProcessorCount / 2))
                .WithSingleSegment()          // emit one segment at a time
                .WithNoContext()              // don't carry context between chunks
                .WithTemperature(0f);

            if (_language != "auto")
                builder = builder.WithLanguage(_language);

            _processor = builder.Build();
        });
    }

    /// <summary>
    /// Transcribes a chunk of 16 kHz mono float32 PCM audio.
    /// Returns the trimmed transcript, or empty string if nothing was detected.
    /// </summary>
    public async Task<string> TranscribeAsync(float[] samples)
    {
        if (_processor is null) return string.Empty;

        // Whisper.net is not thread-safe; serialise calls.
        await _lock.WaitAsync();
        try
        {
            var segments = new List<string>();

            await foreach (var segment in _processor.ProcessAsync(samples))
            {
                var text = segment.Text.Trim();

                // Filter out common hallucination artifacts
                if (string.IsNullOrWhiteSpace(text)) continue;
                if (text.StartsWith('[') && text.EndsWith(']')) continue; // e.g. [BLANK_AUDIO]
                if (text.StartsWith('(') && text.EndsWith(')')) continue; // e.g. (music)

                segments.Add(text);
            }

            return string.Join(" ", segments).Trim();
        }
        finally
        {
            _lock.Release();
        }
    }

    public void Dispose()
    {
        _processor?.Dispose();
        _factory?.Dispose();
        _lock.Dispose();
    }
}