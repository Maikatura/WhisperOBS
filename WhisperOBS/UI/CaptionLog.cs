namespace WhisperOBS.UI;

/// <summary>
/// Scrolling log panel showing timestamped caption history.
/// Owner-drawn for consistent dark-theme look with accent highlights.
/// </summary>
internal sealed class CaptionLog : Control
{
    private record LogEntry(DateTime Time, string Text, bool IsSystem);

    private readonly List<LogEntry>  _entries  = [];
    private readonly VScrollBar      _scroll;
    private const    int             LineH     = 24;
    private const    int             MaxLines  = 500;

    public CaptionLog()
    {
        DoubleBuffered = true;
        BackColor      = Theme.SurfaceAlt;

        _scroll = new VScrollBar
        {
            Dock    = DockStyle.Right,
            Minimum = 0,
            Maximum = 0,
            Value   = 0,
        };
        _scroll.Scroll += (_, _) => Invalidate();
        Controls.Add(_scroll);

        // Mouse wheel
        MouseWheel += (_, e) =>
        {
            int delta = -(e.Delta / 120) * 3;
            _scroll.Value = Math.Clamp(_scroll.Value + delta, _scroll.Minimum,
                Math.Max(_scroll.Minimum, _scroll.Maximum - _scroll.LargeChange + 1));
            Invalidate();
        };
    }

    public void AppendCaption(string text) => Append(text, isSystem: false);
    public void AppendSystem(string text)  => Append(text, isSystem: true);

    private void Append(string text, bool isSystem)
    {
        if (InvokeRequired) { Invoke(() => Append(text, isSystem)); return; }

        _entries.Add(new LogEntry(DateTime.Now, text, isSystem));
        if (_entries.Count > MaxLines) _entries.RemoveAt(0);

        int visibleLines = ClientHeight / LineH;
        int totalLines   = _entries.Count;
        _scroll.Maximum  = Math.Max(0, totalLines - 1);
        _scroll.LargeChange = visibleLines;

        // Auto-scroll to bottom
        _scroll.Value = Math.Max(0, _scroll.Maximum - visibleLines + 1);
        Invalidate();
    }

    public void Clear()
    {
        if (InvokeRequired) { Invoke(Clear); return; }
        _entries.Clear();
        _scroll.Value   = 0;
        _scroll.Maximum = 0;
        Invalidate();
    }

    private int ClientHeight => Height;

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.Clear(Theme.SurfaceAlt);

        int scrollW    = _scroll.Visible ? _scroll.Width : 0;
        int drawWidth  = Width - scrollW - Theme.PadSm * 2;
        int startIndex = _scroll.Value;
        int visLines   = (Height / LineH) + 1;

        for (int i = 0; i < visLines; i++)
        {
            int idx = startIndex + i;
            if (idx >= _entries.Count) break;

            var entry = _entries[idx];
            int y     = i * LineH;

            // Zebra row
            if (idx % 2 == 0)
            {
                using var rowBrush = new SolidBrush(Color.FromArgb(15, 255, 255, 255));
                g.FillRectangle(rowBrush, 0, y, Width - scrollW, LineH);
            }

            // Timestamp
            string ts = entry.Time.ToString("HH:mm:ss");
            using var tsBrush = new SolidBrush(Theme.TextMuted);
            g.DrawString(ts, Theme.FontMuted, tsBrush, Theme.PadSm, y + (LineH - Theme.FontMuted.Height) / 2f);

            // Caption text
            int textX = Theme.PadSm + 58;
            Color textColor = entry.IsSystem ? Theme.TextMuted : Theme.TextCaption;
            using var textBrush = new SolidBrush(textColor);
            Font textFont = entry.IsSystem ? Theme.FontMuted : Theme.FontMono;
            g.DrawString(entry.Text, textFont, textBrush,
                new RectangleF(textX, y + 2, drawWidth - textX + Theme.PadSm, LineH),
                new StringFormat { Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap });
        }

        // Empty state
        if (_entries.Count == 0)
        {
            using var emptyBrush = new SolidBrush(Theme.TextMuted);
            var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            g.DrawString("Captions will appear here once you start listening.",
                Theme.FontBody, emptyBrush, new RectangleF(0, 0, Width, Height), sf);
        }
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        Invalidate();
    }
}