using NAudio.Wave;

namespace WhisperOBS.Models;

/// <summary>
/// Interactive console wizard that collects all user configuration before launch.
/// </summary>
public static class SetupWizard
{
    // Pre-defined Whisper model download URLs (gguf format for Whisper.net)
    private static readonly (string Label, string FileName, string Url)[] KnownModels =
    [
        ("Tiny   (~75 MB)  – fastest, least accurate",
            "ggml-tiny.bin",
            "https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-tiny.bin"),

        ("Base   (~142 MB) – good balance for most users",
            "ggml-base.bin",
            "https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-base.bin"),

        ("Small  (~466 MB) – better accuracy",
            "ggml-small.bin",
            "https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-small.bin"),

        ("Medium (~1.5 GB) – high accuracy",
            "ggml-medium.bin",
            "https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-medium.bin"),

        ("Large  (~2.9 GB) – best accuracy",
            "ggml-large-v3.bin",
            "https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-large-v3.bin"),
    ];

    public static async Task<AppConfig> RunAsync()
    {
        var config = new AppConfig();

        // ── Model selection ──────────────────────────────────────────────────
        string modelsDir = Path.Combine(AppContext.BaseDirectory, "models");
        Directory.CreateDirectory(modelsDir);

        Console.WriteLine("── Whisper Model ──────────────────────────────────");

        // Check which models are already downloaded
        var downloaded = KnownModels
            .Select((m, i) => (m, i, path: Path.Combine(modelsDir, m.FileName)))
            .ToList();

        Console.WriteLine("Available models (* = already downloaded):\n");
        for (int i = 0; i < KnownModels.Length; i++)
        {
            var (label, fileName, _) = KnownModels[i];
            bool exists = File.Exists(Path.Combine(modelsDir, fileName));
            Console.WriteLine($"  [{i + 1}] {(exists ? "*" : " ")} {label}");
        }
        Console.WriteLine($"  [C]   Use a custom model path");
        Console.Write("\nChoice: ");

        string? modelChoice = Console.ReadLine()?.Trim().ToUpper();

        if (modelChoice == "C")
        {
            Console.Write("Enter full path to model file: ");
            config.ModelPath = Console.ReadLine()?.Trim() ?? string.Empty;
            if (!File.Exists(config.ModelPath))
                throw new FileNotFoundException("Model file not found.", config.ModelPath);
        }
        else if (int.TryParse(modelChoice, out int modelIdx) && modelIdx >= 1 && modelIdx <= KnownModels.Length)
        {
            var (_, fileName, url) = KnownModels[modelIdx - 1];
            config.ModelPath = Path.Combine(modelsDir, fileName);

            if (!File.Exists(config.ModelPath))
            {
                Console.WriteLine($"\nDownloading {fileName}…");
                await DownloadFileAsync(url, config.ModelPath);
                Console.WriteLine("Download complete.\n");
            }
            else
            {
                Console.WriteLine("Model already downloaded.\n");
            }
        }
        else
        {
            Console.WriteLine("Invalid choice, defaulting to tiny model.");
            var (_, fileName, url) = KnownModels[0];
            config.ModelPath = Path.Combine(modelsDir, fileName);
            if (!File.Exists(config.ModelPath))
            {
                Console.WriteLine($"Downloading {fileName}…");
                await DownloadFileAsync(url, config.ModelPath);
            }
        }

        // ── Language selection ───────────────────────────────────────────────
        Console.WriteLine("── Language ───────────────────────────────────────");
        Console.WriteLine("Enter language code (e.g. en, sv, de, fr) or leave blank for auto-detect:");
        Console.Write("Language: ");
        config.Language = Console.ReadLine()?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(config.Language))
            config.Language = "auto";
        Console.WriteLine();

        // ── Microphone selection ─────────────────────────────────────────────
        Console.WriteLine("── Microphone ─────────────────────────────────────");
        int deviceCount = WaveInEvent.DeviceCount;
        if (deviceCount == 0)
            throw new Exception("No microphone devices found!");

        for (int i = 0; i < deviceCount; i++)
        {
            var cap = WaveInEvent.GetCapabilities(i);
            Console.WriteLine($"  [{i}] {cap.ProductName}");
        }

        Console.Write($"\nSelect mic index [0-{deviceCount - 1}] (default 0): ");
        string? micInput = Console.ReadLine()?.Trim();
        config.DeviceIndex = int.TryParse(micInput, out int micIdx) ? micIdx : 0;
        Console.WriteLine();

        // ── Port ─────────────────────────────────────────────────────────────
        Console.Write("HTTP port for OBS overlay (default 5000): ");
        string? portInput = Console.ReadLine()?.Trim();
        config.Port = int.TryParse(portInput, out int port) ? port : 5000;
        Console.WriteLine();

        return config;
    }

    private static async Task DownloadFileAsync(string url, string destPath)
    {
        using var http = new HttpClient();
        http.DefaultRequestHeaders.Add("User-Agent", "WhisperOBS/1.0");

        using var response = await http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();

        long? totalBytes = response.Content.Headers.ContentLength;
        await using var stream = await response.Content.ReadAsStreamAsync();
        await using var file = File.Create(destPath);

        var buffer = new byte[81920];
        long downloaded = 0;
        int read;

        while ((read = await stream.ReadAsync(buffer)) > 0)
        {
            await file.WriteAsync(buffer.AsMemory(0, read));
            downloaded += read;

            if (totalBytes.HasValue)
            {
                double pct = downloaded * 100.0 / totalBytes.Value;
                Console.Write($"\r  {pct:F1}% ({downloaded / 1_048_576} MB / {totalBytes.Value / 1_048_576} MB)   ");
            }
        }
        Console.WriteLine();
    }
}