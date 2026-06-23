namespace WhisperOBS.UI;

/// <summary>
/// All colors, fonts, and spacing constants for the WhisperOBS dark theme.
/// Change values here to retheme the entire application.
/// </summary>
internal static class Theme
{
    // ── Palette ───────────────────────────────────────────────────────────────
    public static readonly Color Background   = Color.FromArgb(0xFF, 0x0F, 0x0F, 0x0F);
    public static readonly Color Surface      = Color.FromArgb(0xFF, 0x1A, 0x1A, 0x2E);
    public static readonly Color SurfaceAlt   = Color.FromArgb(0xFF, 0x16, 0x16, 0x24);
    public static readonly Color Border       = Color.FromArgb(0xFF, 0x2A, 0x2A, 0x45);
    public static readonly Color Accent       = Color.FromArgb(0xFF, 0xE9, 0x45, 0x60);
    public static readonly Color AccentDim    = Color.FromArgb(0xFF, 0x8B, 0x1A, 0x2A);
    public static readonly Color TextPrimary  = Color.FromArgb(0xFF, 0xF0, 0xF0, 0xF5);
    public static readonly Color TextMuted    = Color.FromArgb(0xFF, 0x7A, 0x7A, 0x9A);
    public static readonly Color TextCaption  = Color.FromArgb(0xFF, 0xC8, 0xC8, 0xFF);
    public static readonly Color Success      = Color.FromArgb(0xFF, 0x2E, 0xCC, 0x71);
    public static readonly Color Warning      = Color.FromArgb(0xFF, 0xF3, 0x9C, 0x12);
    public static readonly Color WaveformBar  = Color.FromArgb(0xFF, 0xE9, 0x45, 0x60);

    // ── Typography ────────────────────────────────────────────────────────────
    public static readonly Font FontDisplay   = new("Segoe UI", 20f, FontStyle.Bold);
    public static readonly Font FontHeading   = new("Segoe UI", 11f, FontStyle.Bold);
    public static readonly Font FontBody      = new("Segoe UI", 9.5f, FontStyle.Regular);
    public static readonly Font FontMuted     = new("Segoe UI", 8.5f, FontStyle.Regular);
    public static readonly Font FontMono      = new("Consolas", 9.5f, FontStyle.Regular);
    public static readonly Font FontMonoLarge = new("Consolas", 11f,  FontStyle.Regular);
    public static readonly Font FontLabel     = new("Segoe UI", 8f,   FontStyle.Bold);

    // ── Spacing ───────────────────────────────────────────────────────────────
    public const int PadSm  = 8;
    public const int PadMd  = 14;
    public const int PadLg  = 22;
    public const int Radius = 6;

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Apply dark theme to a ComboBox.
    /// Uses OwnerDrawFixed so we control row colours, but sets ItemHeight
    /// explicitly so the dropdown rows actually render at the right size.
    /// </summary>
    public static void ApplyToCombo(ComboBox cb)
    {
        cb.FlatStyle  = FlatStyle.Flat;
        cb.BackColor  = Surface;
        cb.ForeColor  = TextPrimary;
        cb.Font       = FontBody;
        cb.DrawMode   = DrawMode.OwnerDrawFixed;
        cb.ItemHeight = 22;
        cb.DrawItem  += ComboDrawItem;
    }

    private static void ComboDrawItem(object? sender, DrawItemEventArgs e)
    {
        if (e.Index < 0 || sender is not ComboBox cb) return;

        bool selected = (e.State & DrawItemState.Selected) != 0;
        bool hasFocus = (e.State & DrawItemState.Focus)    != 0;

        // Background
        using var bgBrush = new SolidBrush(selected ? AccentDim : Surface);
        e.Graphics.FillRectangle(bgBrush, e.Bounds);

        // Text, vertically centred in the row
        string itemText = cb.Items[e.Index]?.ToString() ?? "";
        using var fgBrush = new SolidBrush(TextPrimary);
        e.Graphics.DrawString(
            itemText,
            FontBody,
            fgBrush,
            new RectangleF(
                e.Bounds.X + 6,
                e.Bounds.Y + (e.Bounds.Height - FontBody.Height) / 2f,
                e.Bounds.Width - 6,
                e.Bounds.Height));

        // Dotted focus rectangle only when keyboard-navigating
        if (hasFocus && !selected)
            ControlPaint.DrawFocusRectangle(e.Graphics, e.Bounds, TextPrimary, Surface);
    }

    /// <summary>Create a styled label that acts as a section eyebrow.</summary>
    public static Label MakeEyebrow(string text) => new()
    {
        Text      = text.ToUpperInvariant(),
        Font      = FontLabel,
        ForeColor = Accent,
        BackColor = Color.Transparent,
        AutoSize  = true,
        Margin    = new Padding(0, PadMd, 0, 4)
    };

    /// <summary>Create a standard body label.</summary>
    public static Label MakeLabel(string text, bool muted = false) => new()
    {
        Text      = text,
        Font      = FontBody,
        ForeColor = muted ? TextMuted : TextPrimary,
        BackColor = Color.Transparent,
        AutoSize  = true
    };
}