namespace WhisperOBS.UI;

/// <summary>
/// Minimalist animated status indicator with precise geometry alignment.
/// Automatically blends into the parent control's surface background.
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
        SetStyle(ControlStyles.SupportsTransparentBackColor | 
                 ControlStyles.UserPaint | 
                 ControlStyles.AllPaintingInWmPaint, true);
        
        DoubleBuffered = true;
        BackColor      = Color.Transparent;
        Size           = new Size(130, 26);
        Font           = Theme.FontBody;

        _timer = new System.Windows.Forms.Timer { Interval = 30 };
        _timer.Tick += (_, _) =>
        {
            if (_state == PillState.Running || _state == PillState.Loading)
            {
                _pulse += _pulseDir ? 0.05f : -0.05f;
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
            _                 => Theme.TextCaption
        };
        
        int dotSize = 8;
        float centerY = Height / 2f;
        float dotX = 6f;
        float dotY = centerY - (dotSize / 2f);
        
        if (_state == PillState.Running || _state == PillState.Loading)
        {
            float haloScale = 1.2f + (0.6f * _pulse); 
            float haloSize = dotSize * haloScale;
            float haloX = dotX + (dotSize / 2f) - (haloSize / 2f);
            float haloY = centerY - (haloSize / 2f);
            
            int haloAlpha = (int)(15 + 30 * _pulse); 
            using var haloBrush = new SolidBrush(Color.FromArgb(haloAlpha, dotColor));
            g.FillEllipse(haloBrush, haloX, haloY, haloSize, haloSize);
        }
        
        using var dotBrush = new SolidBrush(dotColor);
        g.FillEllipse(dotBrush, dotX, dotY, dotSize, dotSize);
        
        Color textColor = _state == PillState.Idle ? Theme.TextMuted : Theme.TextPrimary;
        using var textBrush = new SolidBrush(textColor);
        
        float textX = dotX + dotSize + 10f;
        float textY = centerY - (Font.GetHeight(g) / 2f);
        
        g.DrawString(StatusText, Font, textBrush, textX, textY);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _timer.Dispose();
        base.Dispose(disposing);
    }
}