using System;
using System.Drawing;
using System.Windows.Forms;
using WhisperOBS.UI.Views;

namespace WhisperOBS.UI;

/// <summary>
/// Scalable application hub displaying a navigation sidebar mapped to specific configurations views.
/// </summary>
public sealed class SettingsDialog : Form
{
    private readonly Panel _stageContainer;
    private readonly FlowLayoutPanel _sidebar;
    private Button _activeNavButton = null!;

    private readonly Control _generalInfoView;
    private readonly Control _censorSettingsView;
    private readonly Control _audioDeviceView;
    private readonly Control _AboutView;

    public SettingsDialog()
    {
        Text            = "Preferences & Configuration Profiles";
        Size            = new Size(840, 640);
        MinimumSize     = new Size(700, 500);
        FormBorderStyle = FormBorderStyle.Sizable;
        MaximizeBox     = true;
        MinimizeBox     = false;
        StartPosition   = FormStartPosition.CenterParent;
        BackColor       = Theme.Background;
        ForeColor       = Theme.TextPrimary;
        Font            = Theme.FontBody;
        
        var coreLayout = new TableLayoutPanel
        {
            Dock        = DockStyle.Fill,
            ColumnCount = 2,
            RowCount    = 1,
        };
        coreLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180f)); 
        coreLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        Controls.Add(coreLayout);

      
        _sidebar = new FlowLayoutPanel
        {
            Dock            = DockStyle.Fill,
            BackColor       = Theme.Surface,
            FlowDirection   = FlowDirection.TopDown,
            Padding         = new Padding(0, Theme.PadLg, 0, 0),
            Margin          = Padding.Empty
        };
        coreLayout.Controls.Add(_sidebar, 0, 0);

        
        _stageContainer = new Panel
        {
            Dock            = DockStyle.Fill,
            BackColor       = Theme.Background,
            Margin          = Padding.Empty
        };
        coreLayout.Controls.Add(_stageContainer, 1, 0);

        _generalInfoView    = new GeneralSettingsView();
        _censorSettingsView = new CensorSettingsView();
        _audioDeviceView    = new Panel { BackColor = Theme.Background };
        _AboutView          = new AboutSettingsView() { BackColor = Theme.Background };

        BuildNavigationMenu();
        SwitchToView(_generalInfoView, (Button)_sidebar.Controls[1]);
    }

    private void BuildNavigationMenu()
    {
        var titleLabel = Theme.MakeEyebrow(" PREFERENCES");
        titleLabel.Margin = new Padding(Theme.PadMd, 0, 0, Theme.PadMd);
        _sidebar.Controls.Add(titleLabel);
        
        CreateNavButton("General Info",  _generalInfoView);
        CreateNavButton("Censor Filters", _censorSettingsView);
        CreateNavButton("Audio & Device", _audioDeviceView);
        CreateNavButton("About", _AboutView);
    }

    private void CreateNavButton(string title, Control viewInstance)
    {
        var navBtn = new Button
        {
            Text            = "  " + title,
            Size            = new Size(180, 40),
            FlatStyle       = FlatStyle.Flat,
            TextAlign       = ContentAlignment.MiddleLeft,
            Font            = Theme.FontHeading,
            ForeColor       = Theme.TextMuted,
            BackColor       = Theme.Surface,
            Margin          = Padding.Empty
        };
    
        navBtn.FlatAppearance.BorderSize = 0;
        navBtn.FlatAppearance.MouseOverBackColor = Theme.SurfaceAlt;
        navBtn.FlatAppearance.MouseDownBackColor = Theme.AccentDim;

        navBtn.Click += (s, e) => SwitchToView(viewInstance, navBtn);
        _sidebar.Controls.Add(navBtn);
    }

    private void SwitchToView(Control newView, Button sourceButton)
    {
        if (_activeNavButton != null)
        {
            _activeNavButton.BackColor = Theme.Surface;
            _activeNavButton.ForeColor = Theme.TextMuted;
        }

        _activeNavButton = sourceButton;
        _activeNavButton.BackColor = Theme.SurfaceAlt;
        _activeNavButton.ForeColor = Theme.Accent;

        _stageContainer.Controls.Clear();
        
        newView.Dock = DockStyle.Fill;
        _stageContainer.Controls.Add(newView);
    }
}