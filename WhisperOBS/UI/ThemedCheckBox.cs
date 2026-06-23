using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace WhisperOBS.UI;

public sealed class ThemedCheckBox : CheckBox
{
    public ThemedCheckBox()
    {
        FlatStyle = FlatStyle.Flat;
        ForeColor = Theme.TextPrimary;
        Font = Theme.FontBody;
        AutoSize = true;

        this.DoubleBuffered = true;
        this.SetStyle(ControlStyles.AllPaintingInWmPaint | 
                      ControlStyles.UserPaint | 
                      ControlStyles.OptimizedDoubleBuffer |
                      ControlStyles.SupportsTransparentBackColor, true);
    }

    protected override void OnPaintBackground(PaintEventArgs pevent) 
    {
        
    }
    
    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
        e.Graphics.Clear(this.Parent?.BackColor ?? Theme.Background);
        
        var boxRect = new Rectangle(0, 2, 16, 16);
        using (var brush = new SolidBrush(Checked ? Theme.Accent : Theme.SurfaceAlt))
        {
            e.Graphics.FillRectangle(brush, boxRect);
        }

        using (var pen = new Pen(Checked ? Theme.Accent : Theme.TextMuted, 1))
        {
            e.Graphics.DrawRectangle(pen, boxRect);
        }

        if (Checked)
        {
            using (var pen = new Pen(Theme.Background, 2))
            {
                e.Graphics.DrawLine(pen, 3, 9, 6, 12);
                e.Graphics.DrawLine(pen, 6, 12, 13, 5);
            }
        }

        var textRect = new Rectangle(22, 0, Width - 22, Height);
        TextRenderer.DrawText(e.Graphics, Text, Font, textRect, ForeColor, 
                              TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
    }
    
    public override Size GetPreferredSize(Size proposedSize)
    {
        var textSize = TextRenderer.MeasureText(Text, Font);
        return new Size(textSize.Width + 28, Math.Max(textSize.Height, 20));
    }

    protected override void OnCheckedChanged(EventArgs e)
    {
        base.OnCheckedChanged(e);
        Invalidate();
    }
}