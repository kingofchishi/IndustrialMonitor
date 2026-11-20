using System.Drawing.Drawing2D;

namespace IndustrialMonitor.WinForms.UI.Controls;

/// <summary>
/// 顶部指标卡：标题、主数值、辅助说明。自绘可以避免嵌套太多 Label。
/// </summary>
public sealed class KpiCard : Control
{
    private string _value = "-";
    private string _hint = string.Empty;

    public KpiCard()
    {
        DoubleBuffered = true;
        SetStyle(ControlStyles.SupportsTransparentBackColor, true);
        BackColor = Color.Transparent;
        Height = 104;
        Dock = DockStyle.Fill;
        Margin = new Padding(0, 0, 12, 0);
        Font = UiTheme.Body;
    }

    public string Caption { get; set; } = "指标";
    public Color AccentColor { get; set; } = UiTheme.Accent;

    public void SetValue(string value, string? hint = null)
    {
        _value = value;
        if (hint is not null)
            _hint = hint;
        Invalidate();
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        e.Graphics.Clear(Parent?.BackColor ?? UiTheme.Background);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

        var rect = new Rectangle(0, 0, Width - 1, Height - 1);
        using var path = UiTheme.RoundedRect(rect, 14);
        using var fill = new SolidBrush(UiTheme.Surface);
        using var border = new Pen(UiTheme.Border);
        e.Graphics.FillPath(fill, path);
        e.Graphics.DrawPath(border, path);

        using var accent = new SolidBrush(AccentColor);
        e.Graphics.FillRectangle(accent, 0, 18, 5, Height - 36);

        using var captionFont = new Font("Microsoft YaHei UI", 9F, FontStyle.Regular);
        using var valueFont = new Font("Microsoft YaHei UI", 22F, FontStyle.Bold);
        using var hintFont = new Font("Microsoft YaHei UI", 8F, FontStyle.Regular);
        using var captionBrush = new SolidBrush(UiTheme.Muted);
        using var valueBrush = new SolidBrush(UiTheme.Text);
        using var hintBrush = new SolidBrush(UiTheme.Muted);

        e.Graphics.DrawString(Caption, captionFont, captionBrush, 20, 16);
        e.Graphics.DrawString(_value, valueFont, valueBrush, 18, 36);
        if (!string.IsNullOrWhiteSpace(_hint))
            e.Graphics.DrawString(_hint, hintFont, hintBrush, 20, Height - 28);
    }
}
