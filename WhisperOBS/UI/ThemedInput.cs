namespace WhisperOBS.UI;

/// <summary>Factory for consistently-styled input controls.</summary>
internal static class ThemedInput
{
    public static TextBox MakeTextBox(string placeholder = "", int width = 200) => new()
    {
        BackColor = Theme.SurfaceAlt,
        ForeColor = Theme.TextPrimary,
        BorderStyle = BorderStyle.FixedSingle,
        Font = Theme.FontBody,
        Width = width,
        PlaceholderText = placeholder
    };

    /// <summary>
    /// Creates a premium, fully flat NumericUpDown control with custom-redrawn minimalist spinner arrows.
    /// Supports a customizable default height for seamless layout spacing.
    /// </summary>
    public static NumericUpDown MakeNumeric(int min, int max, int value, int width = 90, int defaultHeight = 32)
    {
        var nud = new NumericUpDown
        {
            AutoSize = false,
            Width = width,
            Height = defaultHeight,

            MinimumSize = new Size(0, defaultHeight),
            MaximumSize = new Size(0, defaultHeight),

            BackColor = Theme.SurfaceAlt,
            ForeColor = Theme.TextPrimary,
            BorderStyle = BorderStyle.None,
            Font = Theme.FontBody,
            Minimum = min,
            Maximum = max,
            Value = value
        };

        nud.HandleCreated += (s, _) =>
        {
            if (s is not NumericUpDown n || n.Controls.Count < 2) return;

            var spinner = n.Controls[0];
            var textBox = n.Controls[1];

            spinner.Width = 22;
            spinner.Height = n.Height;
            spinner.Location = new Point(n.Width - spinner.Width, 0);

            spinner.Paint += (sender, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                var spinCtrl = (Control)sender!;

                using var bgBrush = new SolidBrush(Theme.SurfaceAlt);
                g.Clear(Theme.SurfaceAlt);

                using var linePen = new Pen(Theme.Border, 1f);
                g.DrawLine(linePen, 0, 0, 0, spinCtrl.Height);

                int halfHeight = spinCtrl.Height / 2;

                using var arrowPen = new Pen(Theme.TextMuted, 2f);
                arrowPen.StartCap = System.Drawing.Drawing2D.LineCap.Round;
                arrowPen.EndCap = System.Drawing.Drawing2D.LineCap.Round;

                int cx = spinCtrl.Width / 2;
                int uy = halfHeight / 2 + 1;
                g.DrawLine(arrowPen, cx - 4, uy + 2, cx, uy - 2);
                g.DrawLine(arrowPen, cx, uy - 2, cx + 4, uy + 2);

                int dy = halfHeight + (halfHeight / 2) - 1;
                g.DrawLine(arrowPen, cx - 4, dy - 2, cx, dy + 2);
                g.DrawLine(arrowPen, cx, dy + 2, cx + 4, dy - 2);
            };

            textBox.Location = new Point(10, (n.Height - textBox.Height) / 2);
            textBox.Width = n.Width - spinner.Width - 14;
        };

        nud.MouseWheel += (sender, e) =>
        {
            if (sender is not NumericUpDown n) return;
            int step = e.Delta > 0 ? 1 : -1;
            decimal newValue = n.Value + step;
            if (newValue >= n.Minimum && newValue <= n.Maximum) n.Value = newValue;
            if (e is HandledMouseEventArgs hme) hme.Handled = true;
        };

        nud.Paint += (sender, e) =>
        {
            if (sender is not NumericUpDown box) return;
            using var pen = new Pen(Theme.Border, 1f);
            e.Graphics.DrawRectangle(pen, 0, 0, box.Width - 1, box.Height - 1);
        };

        return nud;
    }

    /// <summary>Render a horizontal separator line.</summary>
    public static Panel MakeSeparator(int topMargin = 8, int bottomMargin = 8) => new()
    {
        Height = 1,
        Dock = DockStyle.Top,
        BackColor = Theme.Border,
        Margin = new Padding(0, topMargin, 0, bottomMargin)
    };

    /// <summary>Panel with Surface color, used as card containers.</summary>
    public static Panel MakeCard(int padding = Theme.PadMd) => new()
    {
        BackColor = Theme.Surface,
        Padding = new Padding(padding),
        Margin = new Padding(0, 0, 0, Theme.PadSm),
    };
}