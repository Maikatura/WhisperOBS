namespace WhisperOBS.UI;

/// <summary>
/// Premium ultra-dark slate & deep blue theme configuration.
/// Focused on minimalist, high-contrast, modern developer tool aesthetics.
/// </summary>
internal static class Theme
{
    // ── Palette (Deep Slate & Cobalt) ─────────────────────────────────────────
    public static readonly Color Background = Color.FromArgb(0xFF, 0x0A, 0x0B, 0x0D); // True deep charcoal
    public static readonly Color Surface = Color.FromArgb(0xFF, 0x12, 0x14, 0x18); // Solid card background
    public static readonly Color SurfaceAlt = Color.FromArgb(0xFF, 0x1A, 0x1D, 0x24); // Hover/Active states
    public static readonly Color Border = Color.FromArgb(0xFF, 0x26, 0x29, 0x30); // Clean, non-distracting borders

    // Accents (Clean Tech Blue instead of Cyber Magenta)
    public static readonly Color Accent = Color.FromArgb(0xFF, 0x25, 0x63, 0xEB); // Vibrant Royal/Cobalt Blue
    public static readonly Color AccentDim = Color.FromArgb(0xFF, 0x1D, 0x4E, 0xD8); // Deep Blue for selected items

    // Typography Colors
    public static readonly Color TextPrimary = Color.FromArgb(0xFF, 0xF1, 0xF5, 0xF9); // Soft crisp white (Slate 100)

    public static readonly Color
        TextMuted = Color.FromArgb(0xFF, 0x94, 0xA3, 0xB8); // Highly legible muted text (Slate 400)

    public static readonly Color TextCaption = Color.FromArgb(0xFF, 0x64, 0x74, 0x8B); // Low-hierarchy text (Slate 500)

    // Status
    public static readonly Color Success = Color.FromArgb(0xFF, 0x10, 0xB9, 0x81); // Emerald Green
    public static readonly Color Warning = Color.FromArgb(0xFF, 0xF5, 0x9E, 0x0B); // Amber Yellow
    public static readonly Color WaveformBar = Color.FromArgb(0xFF, 0x3B, 0x82, 0xF6); // Electric Blue

    public static readonly Color GridSelectionBg = AccentDim;
    public static readonly Color DangerRed = Color.FromArgb(0xFF, 0xEF, 0x44, 0x44); // Slate Rose / Crimson
    public static readonly Color DangerRedHover = Color.FromArgb(0xFF, 0xF8, 0x71, 0x71); // Light crisp red

    // ── Typography (Clean Inter/Segoe UI scale) ───────────────────────────────
    public static readonly Font FontDisplay = new("Segoe UI", 18f, FontStyle.Bold); // Slightly tighter, cleaner size
    public static readonly Font FontHeading = new("Segoe UI", 11f, FontStyle.Bold);
    public static readonly Font FontBody = new("Segoe UI", 9.5f, FontStyle.Regular);
    public static readonly Font FontMuted = new("Segoe UI", 8.5f, FontStyle.Regular);
    public static readonly Font FontMono = new("Consolas", 9.5f, FontStyle.Regular);
    public static readonly Font FontMonoLarge = new("Consolas", 11f, FontStyle.Regular);
    public static readonly Font FontLabel = new("Segoe UI", 8f, FontStyle.Bold); // Eyebrows

    // ── Spacing & Layout ──────────────────────────────────────────────────────
    public const int PadSm = 6; // Tighter internal element padding
    public const int PadMd = 12; // Standard grid spacing
    public const int PadLg = 20; // Generous container margins
    public const int Radius = 6; // Clean, modern slight rounding

    // ── UI Generation Helpers ─────────────────────────────────────────────────

    /// <summary>
    /// Apply dark theme to a ComboBox with clean custom rendering.
    /// </summary>
    public static void ApplyToCombo(ComboBox cb)
    {
        cb.FlatStyle = FlatStyle.Flat;
        cb.BackColor = Theme.Surface;
        cb.ForeColor = Theme.TextPrimary;
        cb.Font = Theme.FontBody;
        cb.ItemHeight = 26;

        cb.DrawItem -= ComboDrawItem;

        cb.DrawMode = DrawMode.OwnerDrawFixed;
        cb.DrawItem += ComboDrawItem;
    }

    /// <summary>
    /// Handles custom flat drawing logic for individual dropdown list elements.
    /// </summary>
    private static void ComboDrawItem(object? sender, DrawItemEventArgs e)
    {
        if (e.Index < 0 || sender is not ComboBox cb) return;

        bool selected = (e.State & DrawItemState.Selected) != 0;
        bool hasFocus = (e.State & DrawItemState.Focus) != 0;

     
        var g = e.Graphics;
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

        Color backgroundColor = selected ? Theme.AccentDim : Theme.Surface;
        using var bgBrush = new SolidBrush(backgroundColor);
        g.FillRectangle(bgBrush, e.Bounds);

        string itemText = cb.Items[e.Index]?.ToString() ?? "";
        Color textColor = selected ? Color.White : Theme.TextPrimary;
        using var fgBrush = new SolidBrush(textColor);

        float textY = e.Bounds.Y + (e.Bounds.Height - Theme.FontBody.GetHeight(g)) / 2f;

        g.DrawString(
            itemText,
            Theme.FontBody,
            fgBrush,
            new RectangleF(e.Bounds.X + 8f, textY, e.Bounds.Width - 8f, e.Bounds.Height));

        if (hasFocus && !selected)
        {
            using var focusPen = new Pen(Theme.Border, 1f);
            g.DrawRectangle(focusPen, e.Bounds.X, e.Bounds.Y, e.Bounds.Width - 1, e.Bounds.Height - 1);
        }
    }

    /// <summary>Create a styled muted section header tracking above layout elements.</summary>
    public static Label MakeEyebrow(string text)
    {
        string spacedText = string.Join(" ", text.ToUpperInvariant().ToCharArray());

        return new Label
        {
            Text      = spacedText,
            Font      = new Font(FontLabel.FontFamily, FontLabel.Size, FontStyle.Bold),
            ForeColor = Accent,
            BackColor = Color.Transparent,
            AutoSize  = true,
            Margin    = new Padding(0, PadLg, 0, PadSm)
        };
    }

    /// <summary>Create a standard body label.</summary>
    public static Label MakeLabel(string text, bool muted = false) => new()
    {
        Text = text,
        Font = FontBody,
        ForeColor = muted ? TextCaption : TextPrimary,
        BackColor = Color.Transparent,
        AutoSize = true
    };

    /// <summary>
    /// Applies a premium dark slate layout style directly onto an existing NumericUpDown control,
    /// stripping its legacy gray spinner blocks and injecting uniform spacing.
    /// </summary>
    public static void ApplyToNumeric(NumericUpDown nud)
    {
        nud.BackColor = Theme.SurfaceAlt;
        nud.ForeColor = Theme.TextPrimary;
        nud.BorderStyle = BorderStyle.None;
        nud.Font = Theme.FontBody;
        nud.Height = 32; 
        
        nud.HandleCreated -= HandleNumericStructure;
        nud.HandleCreated += HandleNumericStructure;
        
        nud.MouseWheel -= HandleNumericScroll;
        nud.MouseWheel += HandleNumericScroll;
        
        nud.Paint -= PaintNumericBorder;
        nud.Paint += PaintNumericBorder;
        
        if (nud.IsHandleCreated)
        {
            HandleNumericStructure(nud, EventArgs.Empty);
            nud.Invalidate();
        }
    }


    private static void HandleNumericStructure(object? sender, EventArgs e)
    {
        if (sender is not NumericUpDown n || n.Controls.Count < 2) return;

        var spinner = n.Controls[0];
        var textBox = n.Controls[1];

        spinner.Visible = false;
        spinner.Width = 0;

        textBox.Location = new Point(8, (n.Height - textBox.Height) / 2);
        textBox.Width = n.Width - 16;
    }

    private static void HandleNumericScroll(object? sender, MouseEventArgs e)
    {
        if (sender is not NumericUpDown n) return;
        int step = e.Delta > 0 ? 1 : -1;
        decimal newValue = n.Value + step;
        if (newValue >= n.Minimum && newValue <= n.Maximum)
        {
            n.Value = newValue;
        }

        if (e is HandledMouseEventArgs hme) hme.Handled = true;
    }

    private static void PaintNumericBorder(object? sender, PaintEventArgs e)
    {
        if (sender is not NumericUpDown box) return;
        using var pen = new Pen(Theme.Border, 1f);
        e.Graphics.DrawRectangle(pen, 0, 0, box.Width - 1, box.Height - 1);
    }

    /// <summary>
    /// Implements your clean, modern borderless dark mode theme over a target DataGridView engine.
    /// </summary>
    public static void ApplyGridTheme(DataGridView grid)
    {
        grid.BackgroundColor = Surface;
        grid.BorderStyle = BorderStyle.FixedSingle;
        grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
        grid.GridColor = Border;
        grid.RowHeadersVisible = false;
        grid.AllowUserToAddRows = false;
        grid.AllowUserToDeleteRows = false;
        grid.AllowUserToResizeRows = false;
        grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        grid.MultiSelect = false;
        grid.EnableHeadersVisualStyles = false;
        grid.RowTemplate.Height = 38;
        
        grid.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = SurfaceAlt,
            ForeColor = TextMuted,
            Font = FontHeading,
            SelectionBackColor = SurfaceAlt,
            Alignment = DataGridViewContentAlignment.MiddleLeft,
            Padding = new Padding(PadSm, 0, 0, 0)
        };
        grid.ColumnHeadersHeight = 34;
        grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
        
        grid.DefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Surface,
            ForeColor = TextPrimary,
            Font = FontMonoLarge,
            SelectionBackColor = GridSelectionBg,
            SelectionForeColor = TextPrimary,
            Alignment = DataGridViewContentAlignment.MiddleLeft,
            Padding = new Padding(PadSm, 0, 0, 0)
        };
    }
}