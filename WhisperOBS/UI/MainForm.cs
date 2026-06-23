using NAudio.Wave;
using WhisperOBS.Audio;
using WhisperOBS.Models;
using WhisperOBS.Server;
using WhisperOBS.Services;
using WhisperOBS.UI.Views;

namespace WhisperOBS.UI;

/// <summary>
/// Main application window. Entirely code-constructed.
/// Features a modern, monolithic flat layout with premium spacing.
/// </summary>
public sealed class MainForm : Form
{
    private bool _isShuttingDown = false;
    
    private WhisperService?    _whisper;
    private VRChatService?    _vrchat;
    private MicrophoneCapture? _mic;
    private CaptionServer?     _server;
    private bool               _running;
    private readonly System.Windows.Forms.Timer _peakTimer = new() { Interval = 50 };
    
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
    private NotifyIcon _trayIcon = null!;


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
        SetupTrayIcon();
        _peakTimer.Tick += (_, _) => _waveform.DecayPeak();
        _peakTimer.Start();

        var serverPort = AppSettings.Instance.GetInt("Overlay.Port");
        if (serverPort == 0)
        {
            serverPort = 5000;
            AppSettings.Instance.Set("Overlay.Port", serverPort);
        }
        _server = new CaptionServer(serverPort);
        _ = _server.StartAsync();
    }

    private void SetupTrayIcon()
    {
        var menu = new ContextMenuStrip();
        
        var openItem = new ToolStripMenuItem("Open WhisperOBS");
        openItem.Click += (_, _) => {
            this.Show();
            this.WindowState = FormWindowState.Normal;
            this.Activate();
        };
        menu.Items.Add(openItem);
        
        menu.Items.Add(new ToolStripSeparator());
        
        var exitItem = new ToolStripMenuItem("Exit");
        exitItem.Click += (_, _) => {
            this.Close();
        };
        menu.Items.Add(exitItem);
        
        _trayIcon = new NotifyIcon
        {
            Icon = this.Icon,
            Text = "WhisperOBS",
            Visible = true,
            ContextMenuStrip = menu
        };
    }
    

    private void BuildForm()
    {
        Text            = "WhisperOBS";
        Size            = new Size(1040, 720);
        MinimumSize     = new Size(860, 600);
        BackColor       = Theme.Background;
        ForeColor       = Theme.TextPrimary;
        Font            = Theme.FontBody;
        StartPosition   = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.Sizable;
        Icon            = BuildIcon();
        
        var root = new TableLayoutPanel
        {
            Dock        = DockStyle.Fill,
            RowCount    = 2,
            ColumnCount = 1,
            BackColor   = Theme.Background,
            Padding     = Padding.Empty,
            Margin      = Padding.Empty,
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 64));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        Controls.Add(root);

        root.Controls.Add(BuildHeader(), 0, 0);
        root.Controls.Add(BuildBody(),   0, 1);
    }
    
    private Panel BuildHeader()
    {
        var header = new Panel
        {
            Dock      = DockStyle.Fill,
            BackColor = Theme.Surface,
            Padding   = new Padding(Theme.PadLg, 0, Theme.PadLg, 0),
        };

        var grid = new TableLayoutPanel
        {
            Dock            = DockStyle.Fill,
            ColumnCount     = 2,
            RowCount        = 1,
            BackColor       = Color.Transparent,
            Padding         = Padding.Empty,
            Margin          = Padding.Empty
        };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        grid.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
        header.Controls.Add(grid);

         var logoGroup = new Panel
        {
            Anchor    = AnchorStyles.Left | AnchorStyles.Right,
            Height    = 32, 
            BackColor = Color.Transparent,
            Margin    = Padding.Empty
        };

        var brandLogo = new Label
        {
            Dock     = DockStyle.Fill,
            Font     = Theme.FontDisplay,
            AutoSize = false,
            Margin   = Padding.Empty
        };

        brandLogo.Paint += (sender, e) =>
        {
            var g = e.Graphics;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            string part1 = "WHISPER";
            string part2 = "OBS";

            Size size1 = TextRenderer.MeasureText(g, part1, brandLogo.Font, Size.Empty, TextFormatFlags.NoPadding);

            TextRenderer.DrawText(g, part1, brandLogo.Font, 
                new Point(0, (brandLogo.Height - size1.Height) / 2), 
                Theme.TextPrimary, 
                TextFormatFlags.NoPadding);
            
            int part2X = size1.Width + 4; 
            Size size2 = TextRenderer.MeasureText(g, part2, brandLogo.Font, Size.Empty, TextFormatFlags.NoPadding);

            TextRenderer.DrawText(g, part2, brandLogo.Font, 
                new Point(part2X, (brandLogo.Height - size2.Height) / 2), 
                Theme.Accent, 
                TextFormatFlags.NoPadding);
        };

        logoGroup.Controls.Add(brandLogo);
        grid.Controls.Add(logoGroup, 0, 0);
        
        _statusPill = new StatusPill
        {
            StatusText = "Idle",
            State      = StatusPill.PillState.Idle,
            Anchor     = AnchorStyles.Right, 
            Width      = 120,
            Height     = 28, 
            Margin     = Padding.Empty
        };
        grid.Controls.Add(_statusPill, 1, 0);
        
        header.Paint += (_, e) =>
        {
            using var pen = new Pen(Theme.Border, 1f);
            e.Graphics.DrawLine(pen, 0, header.Height - 1, header.Width, header.Height - 1);
        };

        return header;
    }
    

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
        body.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 300));
        body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        body.Controls.Add(BuildSidebar(), 0, 0);
        body.Controls.Add(BuildContent(), 1, 0);
        return body;
    }

    private Control BuildSidebar()
{
    var sidebar = new Panel
    {
        Dock = DockStyle.Fill,
        BackColor = Theme.Surface,
        Padding = new Padding(Theme.PadLg, Theme.PadMd, Theme.PadLg, Theme.PadMd),
    };

    sidebar.Paint += (_, e) => {
        using var pen = new Pen(Theme.Border, 1f);
        e.Graphics.DrawLine(pen, sidebar.Width - 1, 0, sidebar.Width - 1, sidebar.Height);
    };

    var scrollContainer = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = Color.Transparent };
    var contentStack = new TableLayoutPanel { Dock = DockStyle.Top, ColumnCount = 1, AutoSize = true };
    contentStack.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
    scrollContainer.Controls.Add(contentStack);
    sidebar.Controls.Add(scrollContainer);

    void AddSettingBlock(string title, Control input, int bottomSpace = 24)
    {
        var header = Theme.MakeEyebrow(title);
        header.Margin = new Padding(0, 16, 0, 4); 
        
        var separator = new Panel { Height = 1, BackColor = Theme.SurfaceAlt, Dock = DockStyle.Fill, Margin = new Padding(0, 0, 0, 8) };

        contentStack.RowCount += 3;
        contentStack.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        contentStack.RowStyles.Add(new RowStyle(SizeType.Absolute, 10f));
        contentStack.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        
        contentStack.Controls.Add(header, 0, contentStack.RowCount - 3);
        contentStack.Controls.Add(separator, 0, contentStack.RowCount - 2);
        
        input.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        input.Margin = new Padding(0, 0, 0, bottomSpace);
        contentStack.Controls.Add(input, 0, contentStack.RowCount - 1);
    }
    
    void AddDirectly(Control control, int bottomSpace = 8)
    {
        control.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        control.Margin = new Padding(0, 0, 0, bottomSpace);
    
        contentStack.RowCount++;
        contentStack.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        contentStack.Controls.Add(control);
    }
    
    _modelCombo = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
    foreach (var (label, _, _) in Models) _modelCombo.Items.Add(label);
    _modelCombo.SelectedIndex = 1;
    Theme.ApplyToCombo(_modelCombo);
    AddSettingBlock("Whisper Model", _modelCombo);

    _downloadBar = new ProgressBar
    {
        Width   = 90,
        Height  = 6,
        Visible = false,
        Style   = ProgressBarStyle.Continuous,
        Margin  = new Padding(0, 4, 0, 0),
    };
    AddDirectly(_downloadBar, bottomSpace: 2); // Tight spacing between bar and label
    
    _downloadLabel = new Label
    {
        Text      = "",
        Font      = Theme.FontMuted,
        ForeColor = Theme.TextMuted,
        Width     = 90,
        AutoSize  = false,
        Height    = 16,
        Visible   = false,
    };
    AddDirectly(_downloadLabel, bottomSpace: 28); // Standard gap after status
    
    _micCombo = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
    Theme.ApplyToCombo(_micCombo);
    AddSettingBlock("Microphone Input", _micCombo);
    
    _langCombo = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList };
    Theme.ApplyToCombo(_langCombo);
    foreach (var (code, name) in LanguageList()) _langCombo.Items.Add($"{code}  –  {name}");
    _langCombo.SelectedIndex = 0;
    AddSettingBlock("Transcription Language", _langCombo);

    _portNum = ThemedInput.MakeNumeric(1024, 65535, 5000);
    _portNum.ValueChanged += (_, _) => UpdateUrlLabel();
    AddSettingBlock("Port", _portNum);

    
    _startBtn = new ThemedButton("▶  Start Listening", ThemedButton.ButtonVariant.Primary)
    {
        Height = 46,
        Anchor = AnchorStyles.Left | AnchorStyles.Right,
        Margin = Padding.Empty
    };
    _startBtn.Click += OnStartStop;

    var settingsBtn = new ThemedButton("⚙  Settings", ThemedButton.ButtonVariant.Secondary)
    {
        Height = 36,
        Anchor = AnchorStyles.Left | AnchorStyles.Right,
        Margin = new Padding(0, 12, 0, 0)
    };
    settingsBtn.Click += (_, _) =>
    {
        using var diag = new SettingsDialog();
        diag.ShowDialog(this);
    };

    _clearBtn = new ThemedButton("Clear Session Log", ThemedButton.ButtonVariant.Secondary)
    {
        Height = 36,
        Anchor = AnchorStyles.Left | AnchorStyles.Right,
        Margin = new Padding(0, 12, 0, 0) 
    };
    _clearBtn.Click += (_, _) => _captionLog.Clear();

    var actionDeck = new TableLayoutPanel
    {
        Dock            = DockStyle.Bottom,
        ColumnCount     = 1,
        RowCount        = 3, 
        Height          = 46 + 12 + 36 + 12 + 36, 
        BackColor       = Color.Transparent,
        Padding         = Padding.Empty,
        Margin          = Padding.Empty
    };
    actionDeck.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
    actionDeck.RowStyles.Add(new RowStyle(SizeType.Absolute, 46f));
    actionDeck.RowStyles.Add(new RowStyle(SizeType.Absolute, 48f)); 
    actionDeck.RowStyles.Add(new RowStyle(SizeType.Absolute, 48f));
    
    actionDeck.Controls.Add(_startBtn, 0, 0);
    actionDeck.Controls.Add(settingsBtn, 0, 1); 
    actionDeck.Controls.Add(_clearBtn, 0, 2); 
    sidebar.Controls.Add(actionDeck);
    
    return sidebar;
}

    

    private Control BuildContent()
    {
        var panel = new TableLayoutPanel
        {
            Dock        = DockStyle.Fill,
            RowCount    = 3,
            ColumnCount = 1,
            BackColor   = Theme.Background,
            Padding     = new Padding(Theme.PadLg, Theme.PadLg, Theme.PadLg, Theme.PadLg),
        };
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 90));   
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));   
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));   

        _waveform = new WaveformVisualizer
        {
            Dock   = DockStyle.Fill,
            Margin = new Padding(0, 0, 0, Theme.PadMd),
        };
        panel.Controls.Add(_waveform, 0, 0);

        var visualizerClock = new System.Windows.Forms.Timer
        {
            Interval = 30 
        };
        visualizerClock.Tick += (s, e) =>
        {
            if (_waveform is { IsDisposed: false, Visible: true })
            {
                _waveform.DecayPeak();
            }
        };
        
        panel.Disposed += (s, e) => {
            visualizerClock.Stop();
            visualizerClock.Dispose();
        };

        visualizerClock.Start();

        var logCard = new Panel
        {
            Dock      = DockStyle.Fill,
            BackColor = Theme.Surface,
            Margin    = new Padding(0, 0, 0, Theme.PadMd),
        };

        var logHeader = new Panel
        {
            Dock      = DockStyle.Top,
            Height    = 36,
            BackColor = Theme.Surface,
            Padding   = new Padding(Theme.PadMd, 0, Theme.PadMd, 0),
        };

        var logTitle = Theme.MakeEyebrow("Live Transcription Logs");
        logTitle.Dock      = DockStyle.Left;
        logTitle.TextAlign = ContentAlignment.MiddleLeft;
        logTitle.AutoSize  = false; 
        logTitle.Width     = 250; 
        logHeader.Controls.Add(logTitle);

        logHeader.Paint += (_, e) => {
            using var pen = new Pen(Theme.Border, 1f);
            e.Graphics.DrawLine(pen, 0, logHeader.Height - 1, logHeader.Width, logHeader.Height - 1);
        };

        _captionLog = new CaptionLog 
        { 
            Dock = DockStyle.Fill,
            Padding = new Padding(Theme.PadSm)
        };

        logCard.Controls.Add(_captionLog);
        logCard.Controls.Add(logHeader);

        panel.Controls.Add(logCard, 0, 1);
        

        var urlBar = new Panel
        {
            Dock      = DockStyle.Fill,
            BackColor = Theme.Surface,
            Padding   = new Padding(Theme.PadMd, 0, Theme.PadMd, 0),
        };

        var urlEyebrow = new Label
        {
            Text      = "OBS BROWSER SOURCE CONNECT URL:",
            Font      = Theme.FontLabel,
            ForeColor = Theme.TextMuted,
            Dock      = DockStyle.Left,
            Width     = 240,
            TextAlign = ContentAlignment.MiddleLeft,
        };

        _urlLabel = new Label
        {
            Text      = $"http://localhost:{_portNum?.Value ?? 5000}",
            Font      = Theme.FontMonoLarge,
            ForeColor = Theme.TextPrimary,
            Dock      = DockStyle.Fill, 
            TextAlign = ContentAlignment.MiddleLeft,
            Cursor    = Cursors.Hand,
        };
        

        var copyAction = new Action(() => {
            Clipboard.SetText(_urlLabel.Text);
            _captionLog.AppendSystem("Connection link copied to clipboard infrastructure safely.");
        });

        _urlLabel.Click += (_, _) => copyAction();

        var copyBtn = new ThemedButton("Copy URL", ThemedButton.ButtonVariant.Secondary)
        {
            Width  = 100,
            Height = 32,
            Anchor = AnchorStyles.Right,
            Location = new Point(urlBar.Width - 100 - Theme.PadMd, (urlBar.Height - 32) / 2)
        };
        copyBtn.Click += (_, _) => copyAction();

        urlBar.Resize += (_, _) => {
            copyBtn.Location = new Point(urlBar.Width - copyBtn.Width - urlBar.Padding.Right, (urlBar.Height - copyBtn.Height) / 2);
        };

        urlBar.Paint += (_, e) => {
            using var pen = new Pen(Theme.Border, 1f);
            e.Graphics.DrawRectangle(pen, 0, 0, urlBar.Width - 1, urlBar.Height - 1);
        };

        urlBar.Controls.Add(_urlLabel);   
        urlBar.Controls.Add(urlEyebrow);  
        urlBar.Controls.Add(copyBtn);     
        panel.Controls.Add(urlBar, 0, 2);

        return panel;
    }
    

    private async void OnStartStop(object? sender, EventArgs e)
    {
        if (_running) await StopAsync();
        else          await StartAsync();
    }

    private async Task StartAsync()
    {
        SetSettingsEnabled(false);
        _startBtn.Enabled      = false;
        _statusPill.State      = StatusPill.PillState.Loading;
        _statusPill.StatusText = "Loading…";

        try
        {
            int modelIdx = _modelCombo.SelectedIndex;
            var (_, fileName, url) = Models[modelIdx];
            string modelsDir  = Path.Combine(AppContext.BaseDirectory, "models");
            Directory.CreateDirectory(modelsDir);
            string modelPath  = Path.Combine(modelsDir, fileName);

            if (!File.Exists(modelPath))
            {
                _captionLog.AppendSystem($"Downloading system resource model: {fileName}…");
                _downloadBar.Visible   = true;
                _downloadLabel.Visible = true;
                _downloadBar.Value     = 0;

                await DownloadModelAsync(url, modelPath);

                _downloadBar.Visible   = false;
                _downloadLabel.Visible = false;
                _downloadLabel.Text    = "";
                _captionLog.AppendSystem("Download operation finished.");
            }

            _captionLog.AppendSystem("Initializing model dependencies…");
            string langCode = _langCombo.SelectedIndex == 0 ? "auto"
                : _langCombo.Text.Split("  –  ")[0].Trim();

            _whisper = new WhisperService(modelPath, langCode);
            await _whisper.InitAsync();
            _captionLog.AppendSystem("Engine starting Whisper service.");

            var isVRChatEnabled = AppSettings.Instance.GetBool("VRChat.Enabled");
            if (isVRChatEnabled)
            {
                _vrchat = new VRChatService();
                _captionLog.AppendSystem("Engine starting VRChat service.");
            }
            
            _captionLog.AppendSystem("Engine status: online.");
            
            int port = (int)_portNum.Value;

            if (_server is null)
            {
                _server  = new CaptionServer(port);    
            }
            else
            {
                if (port != _server.GetPort())
                {
                    _server  = new CaptionServer(port);   
                    _ = _server.StartAsync();
                }
            }
            
            
            _captionLog.AppendSystem($"Local networking active on port: {port}.");

            if (_micCombo == null) return;
            
            int micIdx = _micCombo.SelectedIndex;
            if (micIdx < 0) 
            {
                MessageBox.Show("Please select a microphone first.");
                return;
            }
            
            try 
            {
                _mic = new MicrophoneCapture(micIdx)
                {
                    ChunkSeconds = (double)(_chunkNum?.Value ?? (decimal)3.0)
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Microphone init failed: {ex.Message}");
            }

            if (_mic == null)
            {
                _statusPill.State      = StatusPill.PillState.Error;
                _statusPill.StatusText = "Error";
                _captionLog.AppendSystem($"Start failed: Couldn't start microphone capture.");
                await StopAsync();
                return;
            }
            
            _mic.LiveBufferAvailable += (samples) =>
            {
                if (_isShuttingDown) return;
                if (this.IsDisposed || this.Disposing) return;

                float rms = ComputeRms(samples);
    
                this.BeginInvoke(new Action(() => 
                {
                    if (!this.IsDisposed) 
                    {
                        _waveform?.Push(rms * 20.0f);
                    }
                }));
            };
            
            _mic.AudioReady += (samples) =>
            {
                if (_isShuttingDown) return Task.CompletedTask;
                
                _ = Task.Run(async () =>
                {
                    try
                    {
                        var text = await _whisper!.TranscribeAsync(samples);
                        if (!string.IsNullOrWhiteSpace(text))
                        {
                            var isProfanityFilterOn = AppSettings.Instance.GetBool("Engine.Profanity");
                            var filteredText = isProfanityFilterOn ? CensorSettingsView.FilterText(text) : text;

                            if (!string.IsNullOrWhiteSpace(filteredText))
                            {
                                Invoke(() => _captionLog.AppendCaption(filteredText));
                                await _server!.SendCaptionAsync(filteredText);
                                await _vrchat!.SendCaptionAsync(filteredText);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Invoke(() => _captionLog.AppendSystem($"Transcription error: {ex.Message}"));
                    }
                });
                
                return Task.CompletedTask;
            };

            _mic.Start();

            _running               = true;
            _startBtn.Text         = "■  Stop Capture Engine";
            _startBtn.Variant      = ThemedButton.ButtonVariant.Danger;
            _startBtn.Enabled      = true;
            _statusPill.State      = StatusPill.PillState.Running;
            _statusPill.StatusText = "Listening";
            _captionLog.AppendSystem("Stream pipeline captured cleanly.");
        }
        catch (Exception ex)
        {
            _statusPill.State      = StatusPill.PillState.Error;
            _statusPill.StatusText = "Error";
            _captionLog.AppendSystem($"Pipeline crash detailing: {ex.Message}");
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
        

        _running               = false;
        _startBtn.Text         = "▶  Start Listening";
        _startBtn.Variant      = ThemedButton.ButtonVariant.Primary;
        _startBtn.Enabled      = true;
        _statusPill.State      = StatusPill.PillState.Idle;
        _statusPill.StatusText = "Idle";
        _captionLog.AppendSystem("Pipeline session closed cleanly.");

        SetSettingsEnabled(true);
        await Task.CompletedTask;
    }

    private void SetSettingsEnabled(bool enabled)
    {
        _modelCombo?.Invoke(new Action(() => _modelCombo.Enabled = enabled));
        _micCombo?.Invoke(new Action(() => _micCombo.Enabled = enabled));
        _langCombo?.Invoke(new Action(() => _langCombo.Enabled = enabled));
        _portNum?.Invoke(new Action(() => _portNum.Enabled = enabled));
        _chunkNum?.Invoke(new Action(() => _chunkNum.Enabled = enabled));
    }
    
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
                    if (!_downloadBar.IsDisposed && _downloadBar.IsHandleCreated && !_isShuttingDown)
                    {
                        _downloadBar.Value = Math.Clamp(pct, _downloadBar.Minimum, _downloadBar.Maximum);
                        _downloadLabel.Text = $"{downloaded / 1_048_576} MB / {total.Value / 1_048_576} MB";
                    }
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
        Tag       = "Spacer"
    };

    private static Icon BuildIcon()
    {
        var bmp = new Bitmap(32, 32);
        using (var g = Graphics.FromImage(bmp))
        {
            g.Clear(Theme.Background);
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            using var brush = new SolidBrush(Theme.Accent);
            g.FillEllipse(brush, 6, 6, 20, 20);
        }
        return Icon.FromHandle(bmp.GetHicon());
    }

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
    
    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);

        bool minimizeToTray = AppSettings.Instance.GetBool("UI.MinimizeToTray");
        if (minimizeToTray && this.WindowState == FormWindowState.Minimized)
        {
            this.Hide();
            _trayIcon.Visible = true;
        }
    }

    protected override async void OnFormClosing(FormClosingEventArgs e)
    {
        if (_isShuttingDown)
        {
            base.OnFormClosing(e);
            return;
        }

        e.Cancel = true;
        _isShuttingDown = true;
    
        if (_downloadBar != null && !_downloadBar.IsDisposed)
        {
            _downloadBar.Visible = false;
        }
        
        this.Enabled = false; 

        try 
        {
            await StopAsync();
        
            
            _server?.Dispose();
            _server = null;
            
            _mic?.Stop();
            _mic?.Dispose();
            _whisper?.Dispose();
            _vrchat?.Dispose();
            _peakTimer?.Dispose();
            _downloadBar?.Dispose();
            _downloadLabel?.Dispose();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Cleanup error: {ex.Message}");
        }

        this.Close();
    }
    
    
}