namespace WhisperOBS.UI;

/// <summary>
/// Signature element: a live scrolling bar-graph waveform that shows microphone
/// amplitude in real time. Bars scroll from right to left like a spectrum analyzer.
/// </summary>
internal sealed class WaveformVisualizer : Control
{
    private const int BarCount  = 60;
    private const int BarGap    = 2;

    private readonly float[] _levels = new float[BarCount];
    private int   _writeHead = 0;
    private float _peak      = 0f;
    private float _peakDecay = 0f;

    public WaveformVisualizer()
    {
        DoubleBuffered = true;
        BackColor      = Theme.SurfaceAlt;
        Size           = new Size(400, 60);
        MinimumSize    = new Size(100, 40);
    }

    /// <summary>Push a new amplitude sample (0.0 – 1.0).</summary>
    public void Push(float level)
    {
        level = Math.Clamp(level, 0f, 1f);
        _levels[_writeHead % BarCount] = level;
        _writeHead++;

        if (level > _peakDecay) _peakDecay = level;

        Invalidate();
    }

    /// <summary>Tick the peak-hold decay. Call every ~50 ms.</summary>
    public void DecayPeak()
    {
        _peakDecay = Math.Max(0f, _peakDecay - 0.015f);
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

        int w = Width, h = Height;
        g.Clear(Theme.SurfaceAlt);

        float barW = (float)(w - BarGap * (BarCount - 1)) / BarCount;
        if (barW < 1) barW = 1;

        for (int i = 0; i < BarCount; i++)
        {
            // Oldest bar on the left, newest on the right
            int idx = (_writeHead - BarCount + i + BarCount) % BarCount;
            float level = _levels[idx];

            float barH = Math.Max(2, level * (h - 4));
            float x    = i * (barW + BarGap);
            float y    = (h - barH) / 2f;

            // Gradient: dim at low levels, vivid at high
            int alpha = (int)(80 + 175 * level);
            using var brush = new SolidBrush(Color.FromArgb(alpha, Theme.Accent));
            g.FillRectangle(brush, x, y, barW, barH);
        }

        // Peak-hold line
        if (_peakDecay > 0.02f)
        {
            float py = (h - _peakDecay * (h - 4)) / 2f;
            using var peakPen = new Pen(Color.FromArgb(200, Theme.Accent), 1.5f);
            g.DrawLine(peakPen, 0, py, w, py);
        }

        // Label
        using var labelBrush = new SolidBrush(Theme.TextMuted);
        g.DrawString("MIC", Theme.FontLabel, labelBrush, 4, 4);
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        Invalidate();
    }
}