namespace WhisperOBS.UI;

/// <summary>
/// Owner-drawn premium button with the modern slate/blue theme.
/// Supports clean alpha hover overlays and subtle micro-interactions.
/// </summary>
internal sealed class ThemedButton : Control
{
    public enum ButtonVariant { Primary, Secondary, Danger }

    private bool          _hovered;
    private bool          _pressed;
    private ButtonVariant _variant;

    public ButtonVariant Variant
    {
        get => _variant;
        set { _variant = value; Invalidate(); }
    }

    public bool IsToggled { get; set; }

    public ThemedButton(string text, ButtonVariant variant = ButtonVariant.Primary)
    {
        Text           = text;
        _variant       = variant;
        Font           = Theme.FontHeading;
        Cursor         = Cursors.Hand;
        Size           = new Size(160, 42);
        DoubleBuffered = true;

        SetStyle(ControlStyles.SupportsTransparentBackColor | ControlStyles.UserPaint, true);
        BackColor = Color.Transparent;

        MouseEnter += (_, _) => { _hovered = true;  Invalidate(); };
        MouseLeave += (_, _) => { _hovered = false; _pressed = false; Invalidate(); };
        MouseDown  += (_, e) => { if (e.Button == MouseButtons.Left) { _pressed = true;  Invalidate(); } };
        MouseUp    += (_, e) => { if (e.Button == MouseButtons.Left) { _pressed = false; Invalidate(); } };
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

        var rect = new Rectangle(0, 0, Width - 1, Height - 1);

        Color bg = _variant switch
        {
            ButtonVariant.Primary => IsToggled || _pressed ? Theme.AccentDim : Theme.Accent,
            ButtonVariant.Danger  => _pressed ? Color.FromArgb(0xFF, 0x99, 0x1B, 0x1B) : 
                                                Color.FromArgb(0xFF, 0xEF, 0x44, 0x44),
            _                     => _pressed ? Theme.Surface     : Theme.SurfaceAlt
        };

        Color fg = _variant switch
        {
            ButtonVariant.Secondary => _hovered ? Theme.TextPrimary : Theme.TextMuted,
            _                       => Color.White
        };

        using var bgBrush = new SolidBrush(bg);
        using var path    = RoundedRect(rect, Theme.Radius);
        g.FillPath(bgBrush, path);

        if (_hovered && !_pressed)
        {
            int tintAlpha = _variant == ButtonVariant.Secondary ? 10 : 25;
            using var hoverOverlay = new SolidBrush(Color.FromArgb(tintAlpha, Color.White));
            g.FillPath(hoverOverlay, path);
        }
        Color borderColor = _variant switch
        {
            ButtonVariant.Secondary => _hovered ? Theme.TextCaption : Theme.Border,
            _                       => Color.Transparent
        };

        if (borderColor != Color.Transparent)
        {
            using var borderPen = new Pen(borderColor, 1f);
            g.DrawPath(borderPen, path);
        }
        
        using var fgBrush = new SolidBrush(fg);
        var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
        
        var textRect = new RectangleF(0, _pressed ? 1f : 0f, Width, Height);
        g.DrawString(Text, Font, fgBrush, textRect, sf);
    }

    private static System.Drawing.Drawing2D.GraphicsPath RoundedRect(Rectangle r, int radius)
    {
        int d = radius * 2;
        var path = new System.Drawing.Drawing2D.GraphicsPath();
        path.AddArc(r.X, r.Y, d, d, 180, 90);
        path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
        path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }
}