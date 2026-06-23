using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace WhisperOBS.UI.Views;

/// <summary>
/// Dedicated information view showcasing structural architecture specifications, 
/// runtime framework engines, developer credentials, and active system properties.
/// </summary>
public sealed class AboutSettingsView : UserControl
{
    public AboutSettingsView()
    {
        Dock = DockStyle.Fill;
        BackColor = Theme.Background;
        ForeColor = Theme.TextPrimary;
        Font = Theme.FontBody;

        var mainLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 3,
            ColumnCount = 1,
            Padding = new Padding(Theme.PadLg)
        };
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28f));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 46f));
        Controls.Add(mainLayout);

        mainLayout.Controls.Add(Theme.MakeEyebrow("ABOUT WHISPEROBS"), 0, 0);

        var scrollDeck = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            Margin = new Padding(0, Theme.PadMd, 0, 0)
        };
        mainLayout.Controls.Add(scrollDeck, 0, 1);

        var paramStack = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            ColumnCount = 1,
            RowCount = 0,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Margin = Padding.Empty
        };
        paramStack.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        scrollDeck.Controls.Add(paramStack);

        void AppendBlock(Control header, Control bodyElement, int space = 28)
        {
            header.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            header.Margin = new Padding(0, 0, 0, 4);
            paramStack.RowCount++;
            paramStack.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            paramStack.Controls.Add(header);

            bodyElement.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            bodyElement.Margin = new Padding(0, 0, 0, space);
            paramStack.RowCount++;
            paramStack.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            paramStack.Controls.Add(bodyElement);
        }

        var techDescLabel = new Label
        {
            Text =
                "I built WhisperOBS because the existing alternatives for Whisper integration kept bugging out and essentially became abandonware. Since they were getting almost no updates, I decided to build my own—something lightweight, stable, and focused on actually working reliably for local transcription.",
            Font = Theme.FontBody,
            ForeColor = Theme.TextPrimary,
            AutoSize = true
        };
        AppendBlock(Theme.MakeLabel("Why this project?", muted: true), techDescLabel);


        var appVersion = System.Reflection.Assembly.GetEntryAssembly()?.GetName().Version?.ToString(3) ?? "1.0";
        var bitness = Environment.Is64BitProcess ? "64-bit" : "32-bit";

        var versionLabel = new Label
        {
            Text = $"Version {appVersion} ({bitness})\n" +
                   $"Running on .NET {Environment.Version.Major}.{Environment.Version.Minor} with a pure code-built WinForms UI.",
            Font = Theme.FontMonoLarge,
            ForeColor = Theme.TextPrimary,
            AutoSize = true
        };
        AppendBlock(Theme.MakeLabel("Build Info", muted: true), versionLabel);

        var modulesGrid = new TableLayoutPanel
        {
            ColumnCount = 2,
            RowCount = 3,
            AutoSize = true,
            Margin = Padding.Empty,
            Padding = new Padding(0, 4, 0, 4)
        };
        modulesGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180f));
        modulesGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

        void AddRow(string name, string role, int rowIndex)
        {
            var lblName = new Label
            {
                Text = name, Font = Theme.FontHeading, ForeColor = Theme.Accent, AutoSize = true,
                Margin = new Padding(0, 4, 0, 4)
            };
            var lblRole = new Label
            {
                Text = role, Font = Theme.FontBody, ForeColor = Theme.TextMuted, AutoSize = true,
                Margin = new Padding(0, 4, 0, 4)
            };
            modulesGrid.Controls.Add(lblName, 0, rowIndex);
            modulesGrid.Controls.Add(lblRole, 1, rowIndex);
        }

        AddRow("Whisper.cpp (ggml)", "High-speed local inference processing framework engine.", 0);
        AddRow("NAudio Capture Core", "Low-latency Windows multimedia audio buffer stream loops.", 1);
        AddRow("WebSockets Server", "Asynchronous broadcast pipeline out to localized browser clients.", 2);

        AppendBlock(Theme.MakeLabel("Underlying Core Systems Infrastructure", muted: true), modulesGrid);

        var footerDeck = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            Margin = new Padding(0, Theme.PadSm, 0, 0)
        };

        var exploreBtn = new ThemedButton("Open App Storage Folder", ThemedButton.ButtonVariant.Secondary)
        {
            Width = 220,
            Height = 36
        };
        exploreBtn.Click += (s, e) => OpenDataDirectory();
        footerDeck.Controls.Add(exploreBtn);
        mainLayout.Controls.Add(footerDeck, 0, 2);
    }

    private static void OpenDataDirectory()
    {
        try
        {
            string baseDir = AppContext.BaseDirectory;
            if (Directory.Exists(baseDir))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = baseDir,
                    UseShellExecute = true,
                    Verb = "open"
                });
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Unable to explore app directory configuration path: {ex.Message}",
                "System IO Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }
}