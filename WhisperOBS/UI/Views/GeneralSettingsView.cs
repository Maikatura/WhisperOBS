using WhisperOBS.Models;

namespace WhisperOBS.UI.Views;

public sealed class GeneralSettingsView : UserControl
{
    private readonly List<SettingBinding> _bindings = new();
    
    public GeneralSettingsView()
    {
        Dock      = DockStyle.Fill;
        BackColor = Theme.Background;
        ForeColor = Theme.TextPrimary;
        Font      = Theme.FontBody;

        var mainLayout = new TableLayoutPanel
        {
            Dock        = DockStyle.Fill,
            RowCount    = 4,
            ColumnCount = 1,
            Padding     = new Padding(Theme.PadLg)
        };
        
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 15f));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50f));
        
        mainLayout.Controls.Add(Theme.MakeEyebrow("GENERAL APP PREFERENCES"), 0, 0);
        
        var separator = new Panel { 
            Height = 1,
            BackColor = Theme.SurfaceAlt, 
            Dock = DockStyle.Fill, 
            Margin = new Padding(0, 0, 0, 10) 
        };
        mainLayout.Controls.Add(separator, 0, 1);

        
        var scrollDeck = new Panel { Dock = DockStyle.Fill, AutoScroll = true, Margin = new Padding(0, Theme.PadMd, 0, 0) };
        var paramStack = new TableLayoutPanel { Dock = DockStyle.Top, ColumnCount = 1, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };
        paramStack.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        scrollDeck.Controls.Add(paramStack);
        mainLayout.Controls.Add(scrollDeck, 0, 2);

        void AppendBlock(Control header, Control input, int space = 24)
        {
            header.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            paramStack.RowCount++;
            paramStack.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            paramStack.Controls.Add(header);
            input.Anchor  = AnchorStyles.Left;
            input.Margin  = new Padding(0, 0, 0, space);
            paramStack.RowCount++;
            paramStack.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            paramStack.Controls.Add(input);
        }
        
        void AddCategoryHeader(string title)
        {
            var header = new Label
            {
                Text = title.ToUpper(),
                Font = new Font(Theme.FontBody.FontFamily, 9f, FontStyle.Bold),
                ForeColor = Theme.Accent,
                AutoSize = true,
                Margin = new Padding(0, 15, 0, 5) 
            };
    
            paramStack.RowCount++;
            paramStack.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            paramStack.Controls.Add(header);
        }
        
        void AddCheckToStack(string key, string text, bool defaultVal = false)
        {
            var cb = new ThemedCheckBox { 
                Text = text, 
                AutoSize = true, 
                ForeColor = Theme.TextPrimary, 
                Font = Theme.FontBody, 
                Margin = new Padding(0, Theme.PadSm, 0, Theme.PadSm),
                Checked = defaultVal
            };
    
            paramStack.RowCount++;
            paramStack.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            paramStack.Controls.Add(cb);
    
            _bindings.Add(new SettingBinding(key, cb));
        }

        var checksContainer = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, AutoSize = true };
        
        AddCategoryHeader("Engine Settings");
        AddCheckToStack("Engine.EnableDiagnostics", "Enable extended system diagnostic logging", false);
        AddCheckToStack("Engine.Profanity", "Filter out profanity (Uses your censor words)", true);
        
        AddCategoryHeader("VRChat");
        AddCheckToStack("VRChat.Enabled", "Enable VRChat Textbox integration", false);
        
        AddCategoryHeader("Interface");
        AddCheckToStack("UI.MinimizeToTray", "Minimize to system tray on close", true);

        AppendBlock(Theme.MakeLabel("Application Operational Behaviors", muted: true), checksContainer);

        // 4. Footer
        var footerDeck = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft };
        var commitBtn = new ThemedButton("Apply Profile Settings", ThemedButton.ButtonVariant.Primary) { Width = 180, Height = 36 };
        commitBtn.Click += (s, e) => SavePreferencesProfile();
        footerDeck.Controls.Add(commitBtn);
        mainLayout.Controls.Add(footerDeck, 0, 3);

        this.Controls.Add(mainLayout);

        LoadSavedPreferencesProfile();
    }

    private void LoadSavedPreferencesProfile()
    {
        var config = AppSettings.Instance;
        foreach (var b in _bindings) 
            b.Control.Checked = config.GetBool(b.Key, false);
    }

    private void SavePreferencesProfile()
    {
        var config = AppSettings.Instance;
        foreach (var b in _bindings) 
            config.Set(b.Key, b.Control.Checked.ToString());
        config.Save();
        MessageBox.Show("Settings applied successfully.", "WhisperOBS", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }
}