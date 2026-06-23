namespace WhisperOBS.UI;

/// <summary>
/// Owner-drawn button with the WhisperOBS dark theme, rounded corners,
/// and smooth hover / pressed state transitions.
/// </summary>
internal sealed class ThemedButton : Control
{
    public enum ButtonVariant { Primary, Secondary, Danger }

    private bool         _hovered;
    private bool         _pressed;
    private ButtonVariant _variant;

    public ButtonVariant Variant
    {
        get => _variant;
        set { _variant = value; Invalidate(); }
    }

    public bool IsToggled { get; set; }  // for Start/Stop toggle look

    public ThemedButton(string text, ButtonVariant variant = ButtonVariant.Primary)
    {
        Text         = text;
        _variant     = variant;
        Font         = Theme.FontHeading;
        Cursor       = Cursors.Hand;
        Size         = new Size(160, 42);
        DoubleBuffered = true;

        MouseEnter += (_, _) => { _hovered = true;  Invalidate(); };
        MouseLeave += (_, _) => { _hovered = false; _pressed = false; Invalidate(); };
        MouseDown  += (_, e) => { if (e.Button == MouseButtons.Left) { _pressed = true;  Invalidate(); } };
        MouseUp    += (_, e) => { if (e.Button == MouseButtons.Left) { _pressed = false; Invalidate(); } };
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g   = e.Graphics;
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

        var rect = new Rectangle(0, 0, Width - 1, Height - 1);

        Color bg = _variant switch
        {
            ButtonVariant.Primary   => IsToggled ? Theme.AccentDim :
                                       _pressed  ? Theme.AccentDim :
                                       _hovered  ? Color.FromArgb(0xFF, 0xC5, 0x2A, 0x45) : Theme.Accent,
            ButtonVariant.Danger    => _pressed  ? Color.FromArgb(0xFF, 0x6B, 0x0A, 0x15) :
                                       _hovered  ? Color.FromArgb(0xFF, 0xA0, 0x10, 0x20) :
                                                   Color.FromArgb(0xFF, 0x7B, 0x0A, 0x1A),
            _                      => _pressed  ? Theme.SurfaceAlt :
                                       _hovered  ? Theme.Border     : Theme.Surface,
        };

        Color fg = _variant == ButtonVariant.Secondary ? Theme.TextPrimary : Color.White;

        using var bgBrush = new SolidBrush(bg);
        using var path    = RoundedRect(rect, Theme.Radius);
        g.FillPath(bgBrush, path);

        // border
        using var borderPen = new Pen(_variant == ButtonVariant.Secondary ? Theme.Border : bg, 1f);
        g.DrawPath(borderPen, path);

        // text
        using var fgBrush = new SolidBrush(fg);
        var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
        g.DrawString(Text, Font, fgBrush, new RectangleF(0, 0, Width, Height), sf);
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

    protected override void OnClick(EventArgs e)
    {
        base.OnClick(e);
    }
}