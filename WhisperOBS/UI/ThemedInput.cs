namespace WhisperOBS.UI;

/// <summary>Factory for consistently-styled input controls.</summary>
internal static class ThemedInput
{
    public static TextBox MakeTextBox(string placeholder = "", int width = 200) => new()
    {
        BackColor     = Theme.SurfaceAlt,
        ForeColor     = Theme.TextPrimary,
        BorderStyle   = BorderStyle.FixedSingle,
        Font          = Theme.FontBody,
        Width         = width,
        PlaceholderText = placeholder
    };

    public static NumericUpDown MakeNumeric(int min, int max, int value, int width = 90) => new()
    {
        BackColor    = Theme.SurfaceAlt,
        ForeColor    = Theme.TextPrimary,
        BorderStyle  = BorderStyle.FixedSingle,
        Font         = Theme.FontBody,
        Minimum      = min,
        Maximum      = max,
        Value        = value,
        Width        = width,
    };

    /// <summary>Render a horizontal separator line.</summary>
    public static Panel MakeSeparator(int topMargin = 8, int bottomMargin = 8) => new()
    {
        Height    = 1,
        Dock      = DockStyle.Top,
        BackColor = Theme.Border,
        Margin    = new Padding(0, topMargin, 0, bottomMargin)
    };

    /// <summary>Panel with Surface color, used as card containers.</summary>
    public static Panel MakeCard(int padding = Theme.PadMd) => new()
    {
        BackColor = Theme.Surface,
        Padding   = new Padding(padding),
        Margin    = new Padding(0, 0, 0, Theme.PadSm),
    };
}