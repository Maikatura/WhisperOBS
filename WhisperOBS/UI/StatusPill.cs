namespace WhisperOBS.UI;

/// <summary>
/// Small animated status pill (dot + label) that pulses when active.
/// </summary>
internal sealed class StatusPill : Control
{
    public enum PillState { Idle, Loading, Running, Error }

    private PillState _state = PillState.Idle;
    private float     _pulse = 0f;
    private bool      _pulseDir = true;
    private readonly System.Windows.Forms.Timer _timer;

    public PillState State
    {
        get => _state;
        set { _state = value; Invalidate(); }
    }

    public string StatusText { get; set; } = "Idle";

    public StatusPill()
    {
        DoubleBuffered = true;
        BackColor      = Color.Black;
        Size           = new Size(130, 26);
        Font           = Theme.FontBody;

        _timer = new System.Windows.Forms.Timer { Interval = 40 };
        _timer.Tick += (_, _) =>
        {
            if (_state == PillState.Running || _state == PillState.Loading)
            {
                _pulse += _pulseDir ? 0.06f : -0.06f;
                if (_pulse >= 1f) { _pulse = 1f; _pulseDir = false; }
                if (_pulse <= 0f) { _pulse = 0f; _pulseDir = true;  }
                Invalidate();
            }
        };
        _timer.Start();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

        Color dotColor = _state switch
        {
            PillState.Running => Theme.Success,
            PillState.Loading => Theme.Warning,
            PillState.Error   => Theme.Accent,
            _                 => Theme.TextMuted,
        };

        // Pulsing glow halo
        if (_state == PillState.Running || _state == PillState.Loading)
        {
            int haloSize = 14;
            int haloAlpha = (int)(30 + 60 * _pulse);
            using var haloBrush = new SolidBrush(Color.FromArgb(haloAlpha, dotColor));
            g.FillEllipse(haloBrush, 1, (Height - haloSize) / 2, haloSize, haloSize);
        }

        // Dot
        int dotSize = 10;
        int dotY    = (Height - dotSize) / 2;
        using var dotBrush = new SolidBrush(dotColor);
        g.FillEllipse(dotBrush, 4, dotY, dotSize, dotSize);

        // Label
        using var textBrush = new SolidBrush(Theme.TextPrimary);
        g.DrawString(StatusText, Font, textBrush, 20, (Height - Font.GetHeight()) / 2f);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _timer.Dispose();
        base.Dispose(disposing);
    }
}