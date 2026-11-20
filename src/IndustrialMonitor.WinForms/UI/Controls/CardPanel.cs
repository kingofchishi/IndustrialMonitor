using System.Drawing.Drawing2D;

namespace IndustrialMonitor.WinForms.UI.Controls;

/// <summary>
/// 带圆角和细边框的卡片容器。WinForms 原生 Panel 没有圆角，这里用 OnPaint 绘制。
/// </summary>
public sealed class CardPanel : Panel
{
    public CardPanel()
    {
        DoubleBuffered = true;
        BackColor = UiTheme.Surface;
        Padding = new Padding(16);
        Margin = new Padding(0);
    }

    public int CornerRadius { get; set; } = 14;
    public Color BorderColor { get; set; } = UiTheme.Border;
    public Color FillColor { get; set; } = UiTheme.Surface;

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        e.Graphics.Clear(Parent?.BackColor ?? UiTheme.Background);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

        var rect = new Rectangle(0, 0, Width - 1, Height - 1);
        using var path = UiTheme.RoundedRect(rect, CornerRadius);
        using var brush = new SolidBrush(FillColor);
        using var pen = new Pen(BorderColor, 1);

        e.Graphics.FillPath(brush, path);
        e.Graphics.DrawPath(pen, path);
    }
}
