using System.Drawing.Drawing2D;
using IndustrialMonitor.Core.Models;

namespace IndustrialMonitor.WinForms.UI.Controls;

/// <summary>
/// 轻量实时趋势图，使用 GDI+ 自绘，避免额外图表 NuGet 依赖。
/// 只保存最近 MaxPoints 个点；不负责数据库查询。
/// </summary>
public sealed class TrendChart : Control
{
    private readonly List<TelemetryRecord> _points = new();

    public TrendChart()
    {
        DoubleBuffered = true;
        SetStyle(
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw |
            ControlStyles.UserPaint,
            true);
        BackColor = UiTheme.Surface;
        Font = UiTheme.Body;
        MinimumSize = new Size(300, 180);
    }

    public int MaxPoints { get; set; } = 60;
    public Color SeriesColor { get; set; } = UiTheme.Accent;
    public string Unit { get; set; } = string.Empty;
    public string EmptyText { get; set; } = "暂无采样数据，等待采集器写入…";

    public void SetSeries(IEnumerable<TelemetryRecord> records)
    {
        _points.Clear();
        _points.AddRange(records
            .OrderBy(r => r.CollectedAtUtc)
            .TakeLast(MaxPoints));
        Invalidate();
    }

    public void Append(TelemetryRecord record)
    {
        _points.Add(record);
        if (_points.Count > MaxPoints)
            _points.RemoveRange(0, _points.Count - MaxPoints);
        Invalidate();
    }

    public void Clear()
    {
        _points.Clear();
        Invalidate();
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        e.Graphics.Clear(BackColor);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

        if (_points.Count == 0)
        {
            using var emptyFont = new Font("Microsoft YaHei UI", 9F);
            using var emptyBrush = new SolidBrush(UiTheme.Muted);
            var size = e.Graphics.MeasureString(EmptyText, emptyFont);
            e.Graphics.DrawString(
                EmptyText,
                emptyFont,
                emptyBrush,
                (Width - size.Width) / 2,
                (Height - size.Height) / 2);
            return;
        }

        const int left = 48;
        const int right = 24;
        const int top = 22;
        const int bottom = 30;
        var plot = new Rectangle(left, top, Math.Max(10, Width - left - right), Math.Max(10, Height - top - bottom));

        var values = _points.Select(p => p.Value).ToArray();
        var min = values.Min();
        var max = values.Max();
        if (Math.Abs(max - min) < 0.0001)
        {
            min -= 1;
            max += 1;
        }
        else
        {
            var padding = (max - min) * 0.15;
            min -= padding;
            max += padding;
        }

        using var gridPen = new Pen(Color.FromArgb(236, 241, 245), 1);
        using var axisPen = new Pen(UiTheme.Border, 1);
        using var labelFont = new Font("Microsoft YaHei UI", 7.5F);
        using var labelBrush = new SolidBrush(UiTheme.Muted);

        for (var i = 0; i <= 4; i++)
        {
            var y = plot.Top + plot.Height * i / 4f;
            e.Graphics.DrawLine(gridPen, plot.Left, y, plot.Right, y);

            var value = max - (max - min) * i / 4f;
            var text = value.ToString("0.##");
            var size = e.Graphics.MeasureString(text, labelFont);
            e.Graphics.DrawString(text, labelFont, labelBrush, plot.Left - size.Width - 8, y - size.Height / 2);
        }

        e.Graphics.DrawLine(axisPen, plot.Left, plot.Top, plot.Left, plot.Bottom);
        e.Graphics.DrawLine(axisPen, plot.Left, plot.Bottom, plot.Right, plot.Bottom);

        var points = new PointF[_points.Count];
        for (var i = 0; i < _points.Count; i++)
        {
            var x = _points.Count == 1
                ? plot.Left + plot.Width / 2f
                : plot.Left + plot.Width * i / (_points.Count - 1f);
            var y = plot.Bottom - (float)((_points[i].Value - min) / (max - min)) * plot.Height;
            points[i] = new PointF(x, y);
        }

        using var areaPath = new GraphicsPath();
        areaPath.AddLine(plot.Left, plot.Bottom, points[0].X, points[0].Y);
        if (points.Length > 1)
            areaPath.AddLines(points);
        areaPath.AddLine(points[^1].X, points[^1].Y, plot.Right, plot.Bottom);
        areaPath.CloseFigure();

        using var areaBrush = new LinearGradientBrush(
            plot,
            Color.FromArgb(90, SeriesColor),
            Color.FromArgb(0, SeriesColor),
            LinearGradientMode.Vertical);
        e.Graphics.FillPath(areaBrush, areaPath);

        using var linePen = new Pen(SeriesColor, 2.2f);
        if (points.Length > 1)
            e.Graphics.DrawLines(linePen, points);

        using var pointBrush = new SolidBrush(SeriesColor);
        e.Graphics.FillEllipse(pointBrush, points[^1].X - 4, points[^1].Y - 4, 8, 8);

        var lastText = $"{_points[^1].Value:0.##} {Unit}".Trim();
        using var lastBrush = new SolidBrush(UiTheme.Text);
        using var lastFont = new Font("Microsoft YaHei UI", 8.5F, FontStyle.Bold);
        var lastSize = e.Graphics.MeasureString(lastText, lastFont);
        var lastX = Math.Min(points[^1].X + 8, Width - lastSize.Width - 4);
        var lastY = Math.Max(2, points[^1].Y - lastSize.Height - 2);
        e.Graphics.DrawString(lastText, lastFont, lastBrush, lastX, lastY);

        var firstTime = _points[0].CollectedAtUtc.ToLocalTime().ToString("HH:mm:ss");
        var lastTime = _points[^1].CollectedAtUtc.ToLocalTime().ToString("HH:mm:ss");
        e.Graphics.DrawString(firstTime, labelFont, labelBrush, plot.Left, plot.Bottom + 8);
        var lastTimeSize = e.Graphics.MeasureString(lastTime, labelFont);
        e.Graphics.DrawString(lastTime, labelFont, labelBrush, plot.Right - lastTimeSize.Width, plot.Bottom + 8);
    }
}
