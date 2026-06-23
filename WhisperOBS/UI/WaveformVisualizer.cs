using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace WhisperOBS.UI;

/// <summary>
/// Premium modern audio waveform visualizer. Dynamically scales its resolution to fit 
/// its width, renders symmetric center-mirrored audio bars, and implements sleek double-capped peak tracking.
/// </summary>
internal sealed class WaveformVisualizer : Control
{
    private const int TargetBarWidth = 4;
    private const int BarGap         = 2;
    private const float DecayStep    = 0.025f;

    private float[] _levels = Array.Empty<float>();
    private int _writeHead  = 0;
    private float _peakDecay = 0f;

    public WaveformVisualizer()
    {
        SetStyle(ControlStyles.UserPaint | 
                 ControlStyles.AllPaintingInWmPaint | 
                 ControlStyles.OptimizedDoubleBuffer | 
                 ControlStyles.ResizeRedraw, true);

        BackColor   = Theme.SurfaceAlt;
        Size        = new Size(400, 60);
        MinimumSize = new Size(100, 40);
        
        RecalculateResolution();
    }

    /// <summary>Push a new amplitude sample (0.0 – 1.0).</summary>
    public void Push(float level)
    {
        if (_levels.Length == 0) return;

        level = Math.Clamp(level, 0f, 1f);
        _levels[_writeHead % _levels.Length] = level;
        _writeHead++;

        if (level > _peakDecay) _peakDecay = level;

        Invalidate();
    }

    /// <summary>Tick the peak-hold decay. Call every ~30-50 ms.</summary>
    public void DecayPeak()
    {
        if (_peakDecay > 0f)
        {
            _peakDecay = Math.Max(0f, _peakDecay - DecayStep);
            Invalidate();
        }
    }

    private void RecalculateResolution()
    {
        int totalAllocatedWidth = Width;
        int barSpaceNeeded = TargetBarWidth + BarGap;
        int maxBars = Math.Max(10, totalAllocatedWidth / barSpaceNeeded);

        if (_levels.Length != maxBars)
        {
            var newArray = new float[maxBars];
            int itemsToCopy = Math.Min(_levels.Length, maxBars);
            if (itemsToCopy > 0)
            {
                Array.Copy(_levels, _levels.Length - itemsToCopy, newArray, 0, itemsToCopy);
            }
            _levels = newArray;
            _writeHead = itemsToCopy;
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        int w = Width;
        int h = Height;
        g.Clear(Theme.SurfaceAlt);

        int barCount = _levels.Length;
        if (barCount == 0) return;

        float computedBarW = (float)(w - (BarGap * (barCount - 1))) / barCount;
        if (computedBarW < 1f) computedBarW = 1f;

        using var barBrush = new SolidBrush(Theme.Accent);

        for (int i = 0; i < barCount; i++)
        {
            int idx = (_writeHead - barCount + i + barCount) % barCount;
            float level = _levels[idx];

            float barH = Math.Max(2f, level * (h - 8)); 
        
            float x = i * (computedBarW + BarGap);
            float y = (h - barH) / 2f;

            using var path = GetRoundedRectPath(new RectangleF(x, y, computedBarW, barH), computedBarW / 2f);
        
            int alpha = (int)(100 + 155 * level);
            barBrush.Color = Color.FromArgb(alpha, Theme.Accent);
            g.FillPath(barBrush, path);
        }

        using var labelBrush = new SolidBrush(Theme.TextMuted);
        g.DrawString("MIC", Theme.FontLabel, labelBrush, 6, 6);
    }

    private static GraphicsPath GetRoundedRectPath(RectangleF rect, float radius)
    {
        var path = new GraphicsPath();
        if (radius <= 0.1f)
        {
            path.AddRectangle(rect);
            return path;
        }

        float diameter = radius * 2f;
        if (diameter > rect.Width) diameter = rect.Width;
        if (diameter > rect.Height) diameter = rect.Height;

        var size = new SizeF(diameter, diameter);
        var arc = new RectangleF(rect.Location, size);

        path.AddArc(arc, 180, 90);
        arc.X = rect.Right - diameter;
        path.AddArc(arc, 270, 90);
        arc.Y = rect.Bottom - diameter;
        path.AddArc(arc, 0, 90);
        arc.X = rect.Left;
        path.AddArc(arc, 90, 90);
        path.CloseFigure();

        return path;
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        RecalculateResolution();
        Invalidate();
    }
}