using NAudio.Wave;
using WhisperOBS.Audio;
using WhisperOBS.Models;
using WhisperOBS.Server;

namespace WhisperOBS.UI;

/// <summary>
/// Main application window.  Entirely code-constructed — no .Designer.cs or .resx.
///
/// Layout:
///   ┌─────────────────────────────────────────────────────┐
///   │  Header bar  (logo + status pill)                   │
///   ├──────────────┬──────────────────────────────────────┤
///   │  Left panel  │  Right panel                         │
///   │  ─ Model     │  ─ Waveform visualizer               │
///   │  ─ Mic       │  ─ Caption log                       │
///   │  ─ Language  │  ─ OBS URL bar                       │
///   │  ─ Port      │                                      │
///   │  ─ Chunk s   │                                      │
///   │  ─ Start/Stop│                                      │
///   └──────────────┴──────────────────────────────────────┘
/// </summary>
public sealed class MainForm : Form
{
    // ── Backend services ──────────────────────────────────────────────────────
    private WhisperService?    _whisper;
    private MicrophoneCapture? _mic;
    private CaptionServer?     _server;
    private bool               _running;
    private readonly System.Windows.Forms.Timer _peakTimer = new() { Interval = 50 };

    // ── UI controls we need to reference after construction ───────────────────
    private ComboBox        _modelCombo   = null!;
    private ComboBox        _micCombo     = null!;
    private ComboBox        _langCombo    = null!;
    private NumericUpDown   _portNum      = null!;
    private NumericUpDown   _chunkNum     = null!;
    private ThemedButton    _startBtn     = null!;
    private ThemedButton    _clearBtn     = null!;
    private StatusPill      _statusPill   = null!;
    private WaveformVisualizer _waveform  = null!;
    private CaptionLog      _captionLog   = null!;
    private Label           _urlLabel     = null!;
    private ProgressBar     _downloadBar  = null!;
    private Label           _downloadLabel = null!;

    // ── Known models ──────────────────────────────────────────────────────────
    private static readonly (string Label, string FileName, string Url)[] Models =
    [
        ("Tiny   (~75 MB)",   "ggml-tiny.bin",    "https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-tiny.bin"),
        ("Base   (~142 MB)",  "ggml-base.bin",    "https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-base.bin"),
        ("Small  (~466 MB)",  "ggml-small.bin",   "https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-small.bin"),
        ("Medium (~1.5 GB)",  "ggml-medium.bin",  "https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-medium.bin"),
        ("Large  (~2.9 GB)",  "ggml-large-v3.bin","https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-large-v3.bin"),
    ];

    public MainForm()
    {
        BuildForm();
        PopulateMics();
        MarkDownloadedModels();
        _peakTimer.Tick += (_, _) => _waveform.DecayPeak();
        _peakTimer.Start();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Form construction
    // ─────────────────────────────────────────────────────────────────────────

    private void BuildForm()
    {
        // ── Window chrome ─────────────────────────────────────────────────────
        Text            = "WhisperOBS";
        Size            = new Size(1000, 680);
        MinimumSize     = new Size(820, 560);
        BackColor       = Theme.Background;
        ForeColor       = Theme.TextPrimary;
        Font            = Theme.FontBody;
        StartPosition   = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.Sizable;
        Icon            = BuildIcon();

        // ── Root layout: header + body ────────────────────────────────────────
        var root = new TableLayoutPanel
        {
            Dock        = DockStyle.Fill,
            RowCount    = 2,
            ColumnCount = 1,
            BackColor   = Theme.Background,
            Padding     = Padding.Empty,
            Margin      = Padding.Empty,
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        Controls.Add(root);

        root.Controls.Add(BuildHeader(), 0, 0);
        root.Controls.Add(BuildBody(),   0, 1);
    }

    // ── Header ────────────────────────────────────────────────────────────────

    private Panel BuildHeader()
    {
        var header = new Panel
        {
            Dock      = DockStyle.Fill,
            BackColor = Theme.Surface,
            Padding   = new Padding(Theme.PadLg, 0, Theme.PadLg, 0),
        };

        // Left: logo
        var logo = new Label
        {
            Text      = "WHISPER",
            Font      = Theme.FontDisplay,
            ForeColor = Theme.TextPrimary,
            AutoSize  = true,
            Dock      = DockStyle.Left,
            TextAlign = ContentAlignment.MiddleLeft,
        };

        var logoAccent = new Label
        {
            Text      = "OBS",
            Font      = Theme.FontDisplay,
            ForeColor = Theme.Accent,
            AutoSize  = true,
            Dock      = DockStyle.Left,
            TextAlign = ContentAlignment.MiddleLeft,
        };

        // Right: status pill
        _statusPill = new StatusPill
        {
            StatusText = "Idle",
            State      = StatusPill.PillState.Idle,
            Dock       = DockStyle.Right,
            Width      = 140,
        };

        header.Controls.Add(_statusPill);
        header.Controls.Add(logoAccent);
        header.Controls.Add(logo);

        // Bottom border
        var border = new Panel
        {
            Height    = 1,
            Dock      = DockStyle.Bottom,
            BackColor = Theme.Border,
        };
        header.Controls.Add(border);

        return header;
    }

    // ── Body: left sidebar + right content ───────────────────────────────────

    private Control BuildBody()
    {
        var body = new TableLayoutPanel
        {
            Dock        = DockStyle.Fill,
            ColumnCount = 2,
            RowCount    = 1,
            BackColor   = Theme.Background,
            Padding     = Padding.Empty,
            Margin      = Padding.Empty,
        };
        body.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 280));
        body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        body.Controls.Add(BuildSidebar(), 0, 0);
        body.Controls.Add(BuildContent(), 1, 0);
        return body;
    }

    // ── Left sidebar ──────────────────────────────────────────────────────────

    private Control BuildSidebar()
    {
        var sidebar = new Panel
        {
            Dock      = DockStyle.Fill,
            BackColor = Theme.Surface,
            Padding   = new Padding(Theme.PadMd),
        };

        // Right border
        sidebar.Paint += (_, e) =>
        {
            using var pen = new Pen(Theme.Border, 1);
            e.Graphics.DrawLine(pen, sidebar.Width - 1, 0, sidebar.Width - 1, sidebar.Height);
        };

        var flow = new FlowLayoutPanel
        {
            Dock          = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            AutoScroll    = true,
            WrapContents  = false,
            BackColor     = Color.Transparent,
            Padding       = Padding.Empty,
        };

        int inputW = 226;

        // ── Model ─────────────────────────────────────────────────────────────
        flow.Controls.Add(Theme.MakeEyebrow("Whisper Model"));
        _modelCombo = new ComboBox { Width = inputW, DropDownStyle = ComboBoxStyle.DropDownList };
        foreach (var (label, _, _) in Models) _modelCombo.Items.Add(label);
        _modelCombo.SelectedIndex = 1; // default: Base
        Theme.ApplyToCombo(_modelCombo);
        flow.Controls.Add(_modelCombo);

        _downloadBar = new ProgressBar
        {
            Width   = inputW,
            Height  = 6,
            Visible = false,
            Style   = ProgressBarStyle.Continuous,
            Margin  = new Padding(0, 4, 0, 0),
        };
        flow.Controls.Add(_downloadBar);

        _downloadLabel = new Label
        {
            Text      = "",
            Font      = Theme.FontMuted,
            ForeColor = Theme.TextMuted,
            Width     = inputW,
            AutoSize  = false,
            Height    = 16,
            Visible   = false,
        };
        flow.Controls.Add(_downloadLabel);

        // ── Microphone ────────────────────────────────────────────────────────
        flow.Controls.Add(Spacer(8));
        flow.Controls.Add(Theme.MakeEyebrow("Microphone"));
        _micCombo = new ComboBox { Width = inputW, DropDownStyle = ComboBoxStyle.DropDownList };
        Theme.ApplyToCombo(_micCombo);
        flow.Controls.Add(_micCombo);

        // ── Language ──────────────────────────────────────────────────────────
        flow.Controls.Add(Spacer(8));
        flow.Controls.Add(Theme.MakeEyebrow("Language"));
        _langCombo = new ComboBox { Width = inputW, DropDownStyle = ComboBoxStyle.DropDownList };
        Theme.ApplyToCombo(_langCombo);
        foreach (var (code, name) in LanguageList())
            _langCombo.Items.Add($"{code}  –  {name}");
        _langCombo.SelectedIndex = 0; // auto-detect
        flow.Controls.Add(_langCombo);

        // ── Port ──────────────────────────────────────────────────────────────
        flow.Controls.Add(Spacer(8));
        flow.Controls.Add(Theme.MakeEyebrow("OBS Overlay Port"));
        _portNum = ThemedInput.MakeNumeric(1024, 65535, 5000, inputW);
        _portNum.ValueChanged += (_, _) => UpdateUrlLabel();
        flow.Controls.Add(_portNum);

        // ── Chunk duration ────────────────────────────────────────────────────
        flow.Controls.Add(Spacer(4));
        flow.Controls.Add(Theme.MakeLabel("Chunk duration (seconds)", muted: true));
        _chunkNum = ThemedInput.MakeNumeric(1, 10, 3, inputW);
        flow.Controls.Add(_chunkNum);

        // ── Spacer push ───────────────────────────────────────────────────────
        flow.Controls.Add(Spacer(20));

        // ── Buttons ───────────────────────────────────────────────────────────
        _startBtn = new ThemedButton("▶  Start Listening", ThemedButton.ButtonVariant.Primary)
        {
            Width  = inputW,
            Height = 44,
        };
        _startBtn.Click += OnStartStop;
        flow.Controls.Add(_startBtn);

        flow.Controls.Add(Spacer(6));

        _clearBtn = new ThemedButton("Clear Log", ThemedButton.ButtonVariant.Secondary)
        {
            Width  = inputW,
            Height = 36,
        };
        _clearBtn.Click += (_, _) => _captionLog.Clear();
        flow.Controls.Add(_clearBtn);

        sidebar.Controls.Add(flow);
        return sidebar;
    }

    // ── Right content panel ───────────────────────────────────────────────────

    private Control BuildContent()
    {
        var panel = new TableLayoutPanel
        {
            Dock        = DockStyle.Fill,
            RowCount    = 3,
            ColumnCount = 1,
            BackColor   = Theme.Background,
            Padding     = new Padding(Theme.PadLg, Theme.PadMd, Theme.PadLg, Theme.PadMd),
        };
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 80));   // waveform
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));   // caption log
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));   // URL bar

        // ── Waveform ──────────────────────────────────────────────────────────
        _waveform = new WaveformVisualizer
        {
            Dock   = DockStyle.Fill,
            Margin = new Padding(0, 0, 0, Theme.PadSm),
        };
        panel.Controls.Add(_waveform, 0, 0);

        // ── Caption log ───────────────────────────────────────────────────────
        var logCard = new Panel
        {
            Dock      = DockStyle.Fill,
            BackColor = Theme.SurfaceAlt,
            Padding   = Padding.Empty,
            Margin    = new Padding(0, 0, 0, Theme.PadSm),
        };

        var logHeader = new Panel
        {
            Dock      = DockStyle.Top,
            Height    = 30,
            BackColor = Theme.Surface,
            Padding   = new Padding(Theme.PadSm, 0, Theme.PadSm, 0),
        };
        var logTitle = Theme.MakeLabel("CAPTION HISTORY");
        logTitle.ForeColor = Theme.Accent;
        logTitle.Font      = Theme.FontLabel;
        logTitle.Dock      = DockStyle.Left;
        logTitle.TextAlign = ContentAlignment.MiddleLeft;
        logHeader.Controls.Add(logTitle);

        _captionLog = new CaptionLog { Dock = DockStyle.Fill };
        logCard.Controls.Add(_captionLog);
        logCard.Controls.Add(logHeader);
        panel.Controls.Add(logCard, 0, 1);

        // ── OBS URL bar ───────────────────────────────────────────────────────
        var urlBar = new Panel
        {
            Dock      = DockStyle.Fill,
            BackColor = Theme.Surface,
            Padding   = new Padding(Theme.PadMd, 0, Theme.PadMd, 0),
        };

        var urlEyebrow = new Label
        {
            Text      = "OBS BROWSER SOURCE URL",
            Font      = Theme.FontLabel,
            ForeColor = Theme.Accent,
            Dock      = DockStyle.Left,
            Width     = 200,
            TextAlign = ContentAlignment.MiddleLeft,
        };

        _urlLabel = new Label
        {
            Text      = $"http://localhost:{_portNum?.Value ?? 5000}",
            Font      = Theme.FontMonoLarge,
            ForeColor = Theme.TextPrimary,
            Dock      = DockStyle.Left,
            AutoSize  = true,
            TextAlign = ContentAlignment.MiddleLeft,
            Cursor    = Cursors.Hand,
        };
        _urlLabel.Click += (_, _) =>
        {
            Clipboard.SetText(_urlLabel.Text);
            _captionLog.AppendSystem("URL copied to clipboard.");
        };

        var copyBtn = new ThemedButton("Copy URL", ThemedButton.ButtonVariant.Secondary)
        {
            Width  = 90,
            Height = 30,
            Dock   = DockStyle.Right,
            Margin = new Padding(0, 7, 0, 7),
        };
        copyBtn.Click += (_, _) =>
        {
            Clipboard.SetText(_urlLabel.Text);
            _captionLog.AppendSystem("URL copied to clipboard.");
        };

        urlBar.Controls.Add(copyBtn);
        urlBar.Controls.Add(_urlLabel);
        urlBar.Controls.Add(urlEyebrow);
        panel.Controls.Add(urlBar, 0, 2);

        return panel;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Logic
    // ─────────────────────────────────────────────────────────────────────────

    private async void OnStartStop(object? sender, EventArgs e)
    {
        if (_running) await StopAsync();
        else          await StartAsync();
    }

    private async Task StartAsync()
    {
        // Disable settings while loading; start button stays enabled so user can cancel
        SetSettingsEnabled(false);
        _startBtn.Enabled      = false;
        _statusPill.State      = StatusPill.PillState.Loading;
        _statusPill.StatusText = "Loading…";

        try
        {
            // ── Resolve model path (download if needed) ───────────────────────
            int modelIdx = _modelCombo.SelectedIndex;
            var (_, fileName, url) = Models[modelIdx];
            string modelsDir  = Path.Combine(AppContext.BaseDirectory, "models");
            Directory.CreateDirectory(modelsDir);
            string modelPath  = Path.Combine(modelsDir, fileName);

            if (!File.Exists(modelPath))
            {
                _captionLog.AppendSystem($"Downloading {fileName}…");
                _downloadBar.Visible   = true;
                _downloadLabel.Visible = true;
                _downloadBar.Value     = 0;

                await DownloadModelAsync(url, modelPath);

                _downloadBar.Visible   = false;
                _downloadLabel.Visible = false;
                _downloadLabel.Text    = "";
                _captionLog.AppendSystem("Download complete.");
            }

            // ── Load Whisper ──────────────────────────────────────────────────
            _captionLog.AppendSystem("Loading Whisper model…");
            string langCode = _langCombo.SelectedIndex == 0 ? "auto"
                : _langCombo.Text.Split("  –  ")[0].Trim();

            _whisper = new WhisperService(modelPath, langCode);
            await _whisper.InitAsync();
            _captionLog.AppendSystem("Model ready.");

            // ── Start HTTP/WS server ──────────────────────────────────────────
            int port = (int)_portNum.Value;
            _server  = new CaptionServer(port);
            _ = _server.StartAsync();
            _captionLog.AppendSystem($"OBS overlay server started on port {port}.");

            // ── Start mic capture ─────────────────────────────────────────────
            int micIdx = _micCombo.SelectedIndex;
            _mic       = new MicrophoneCapture(micIdx)
            {
                ChunkSeconds = (double)_chunkNum.Value
            };

            _mic.AudioReady += async (samples) =>
            {
                float rms = ComputeRms(samples);
                Invoke(() => _waveform.Push(rms * 3.5f));

                var text = await _whisper!.TranscribeAsync(samples);
                if (!string.IsNullOrWhiteSpace(text))
                {
                    Invoke(() => _captionLog.AppendCaption(text));
                    await _server!.BroadcastAsync(text);
                }
            };

            _mic.Start();

            _running               = true;
            _startBtn.Text         = "■  Stop";
            _startBtn.Variant      = ThemedButton.ButtonVariant.Danger;
            _startBtn.Enabled      = true;
            _statusPill.State      = StatusPill.PillState.Running;
            _statusPill.StatusText = "Listening";
            _captionLog.AppendSystem("Listening…");
        }
        catch (Exception ex)
        {
            _statusPill.State      = StatusPill.PillState.Error;
            _statusPill.StatusText = "Error";
            _captionLog.AppendSystem($"Error: {ex.Message}");
            // StopAsync re-enables settings, so call it on error too
            await StopAsync();
        }
    }

    private async Task StopAsync()
    {
        _mic?.Stop();
        _mic?.Dispose();
        _mic = null;

        _whisper?.Dispose();
        _whisper = null;

        _server = null;

        _running               = false;
        _startBtn.Text         = "▶  Start Listening";
        _startBtn.Variant      = ThemedButton.ButtonVariant.Primary;
        _startBtn.Enabled      = true;
        _statusPill.State      = StatusPill.PillState.Idle;
        _statusPill.StatusText = "Idle";
        _captionLog.AppendSystem("Stopped.");

        // Re-enable all settings controls now that we're stopped
        SetSettingsEnabled(true);

        await Task.CompletedTask;
    }

    /// <summary>Enable or disable only the settings controls (not the start/stop button).</summary>
    private void SetSettingsEnabled(bool enabled)
    {
        _modelCombo.Enabled = enabled;
        _micCombo.Enabled   = enabled;
        _langCombo.Enabled  = enabled;
        _portNum.Enabled    = enabled;
        _chunkNum.Enabled   = enabled;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────────────────────────

    private void PopulateMics()
    {
        _micCombo.Items.Clear();
        int count = WaveInEvent.DeviceCount;
        if (count == 0) { _micCombo.Items.Add("No microphones found"); return; }
        for (int i = 0; i < count; i++)
            _micCombo.Items.Add(WaveInEvent.GetCapabilities(i).ProductName);
        _micCombo.SelectedIndex = 0;
    }

    private void MarkDownloadedModels()
    {
        string modelsDir = Path.Combine(AppContext.BaseDirectory, "models");
        for (int i = 0; i < Models.Length; i++)
        {
            bool exists = File.Exists(Path.Combine(modelsDir, Models[i].FileName));
            _modelCombo.Items[i] = (exists ? "✓ " : "  ") + Models[i].Label;
        }
    }

    private void UpdateUrlLabel()
    {
        if (_urlLabel is not null)
            _urlLabel.Text = $"http://localhost:{_portNum.Value}";
    }

    private async Task DownloadModelAsync(string url, string destPath)
    {
        using var http = new HttpClient();
        http.DefaultRequestHeaders.Add("User-Agent", "WhisperOBS/1.0");

        using var response = await http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();

        long? total = response.Content.Headers.ContentLength;
        await using var stream = await response.Content.ReadAsStreamAsync();
        await using var file   = File.Create(destPath);

        var    buffer     = new byte[81920];
        long   downloaded = 0;
        int    read;

        while ((read = await stream.ReadAsync(buffer)) > 0)
        {
            await file.WriteAsync(buffer.AsMemory(0, read));
            downloaded += read;

            if (total.HasValue)
            {
                int pct = (int)(downloaded * 100 / total.Value);
                Invoke(() =>
                {
                    _downloadBar.Value   = pct;
                    _downloadLabel.Text  = $"{downloaded / 1_048_576} MB / {total.Value / 1_048_576} MB";
                });
            }
        }

        MarkDownloadedModels();
    }

    private static float ComputeRms(float[] samples)
    {
        if (samples.Length == 0) return 0f;
        double sum = 0;
        foreach (var s in samples) sum += s * s;
        return (float)Math.Sqrt(sum / samples.Length);
    }

    private static Control Spacer(int height) => new Panel
    {
        Height    = height,
        BackColor = Color.Transparent,
    };

    private static Icon BuildIcon()
    {
        // Programmatically build a small icon: red circle on dark bg
        var bmp = new Bitmap(32, 32);
        using (var g = Graphics.FromImage(bmp))
        {
            g.Clear(Color.FromArgb(0x1A, 0x1A, 0x2E));
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            using var brush = new SolidBrush(Color.FromArgb(0xE9, 0x45, 0x60));
            g.FillEllipse(brush, 4, 4, 24, 24);
        }
        return Icon.FromHandle(bmp.GetHicon());
    }

    // ── Language list ─────────────────────────────────────────────────────────
    private static (string code, string name)[] LanguageList() =>
    [
        ("auto", "Auto-detect"),
        ("en",   "English"),
        ("sv",   "Swedish"),
        ("de",   "German"),
        ("fr",   "French"),
        ("es",   "Spanish"),
        ("it",   "Italian"),
        ("pt",   "Portuguese"),
        ("nl",   "Dutch"),
        ("pl",   "Polish"),
        ("ru",   "Russian"),
        ("ja",   "Japanese"),
        ("zh",   "Chinese"),
        ("ko",   "Korean"),
        ("ar",   "Arabic"),
        ("hi",   "Hindi"),
        ("tr",   "Turkish"),
        ("fi",   "Finnish"),
        ("nb",   "Norwegian"),
        ("da",   "Danish"),
    ];

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _mic?.Stop();
        _mic?.Dispose();
        _whisper?.Dispose();
        _peakTimer.Dispose();
        base.OnFormClosed(e);
    }
}