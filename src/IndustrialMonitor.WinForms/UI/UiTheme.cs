using System.Drawing.Drawing2D;

namespace IndustrialMonitor.WinForms.UI;

internal static class UiTheme
{
    public static readonly Color Background = Color.FromArgb(244, 247, 249);
    public static readonly Color Surface = Color.White;
    public static readonly Color Sidebar = Color.FromArgb(20, 33, 49);
    public static readonly Color SidebarHover = Color.FromArgb(31, 48, 68);
    public static readonly Color SidebarText = Color.FromArgb(203, 213, 225);
    public static readonly Color Accent = Color.FromArgb(0, 150, 136);
    public static readonly Color AccentDark = Color.FromArgb(0, 121, 107);
    public static readonly Color AccentSoft = Color.FromArgb(224, 247, 244);
    public static readonly Color Blue = Color.FromArgb(37, 99, 235);
    public static readonly Color BlueSoft = Color.FromArgb(232, 240, 254);
    public static readonly Color Warning = Color.FromArgb(217, 119, 6);
    public static readonly Color WarningSoft = Color.FromArgb(254, 243, 199);
    public static readonly Color Danger = Color.FromArgb(220, 53, 69);
    public static readonly Color DangerSoft = Color.FromArgb(254, 226, 226);
    public static readonly Color Text = Color.FromArgb(30, 41, 59);
    public static readonly Color Muted = Color.FromArgb(100, 116, 139);
    public static readonly Color Border = Color.FromArgb(226, 232, 240);
    public static readonly Color GridAlternate = Color.FromArgb(248, 250, 252);

    public static readonly Font Body = new("Microsoft YaHei UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
    public static readonly Font BodyBold = new("Microsoft YaHei UI", 9F, FontStyle.Bold, GraphicsUnit.Point);
    public static readonly Font Small = new("Microsoft YaHei UI", 8.25F, FontStyle.Regular, GraphicsUnit.Point);
    public static readonly Font Title = new("Microsoft YaHei UI", 16F, FontStyle.Bold, GraphicsUnit.Point);
    public static readonly Font PageTitle = new("Microsoft YaHei UI", 18F, FontStyle.Bold, GraphicsUnit.Point);
    public static readonly Font KpiValue = new("Microsoft YaHei UI", 22F, FontStyle.Bold, GraphicsUnit.Point);

    public static Button PrimaryButton(string text, int width = 108)
    {
        return new Button
        {
            Text = text,
            Width = width,
            Height = 34,
            FlatStyle = FlatStyle.Flat,
            BackColor = Accent,
            ForeColor = Color.White,
            Font = BodyBold,
            Cursor = Cursors.Hand,
            Margin = new Padding(0, 0, 8, 0),
            FlatAppearance = { BorderSize = 0 }
        };
    }

    public static Button GhostButton(string text, int width = 96)
    {
        return new Button
        {
            Text = text,
            Width = width,
            Height = 34,
            FlatStyle = FlatStyle.Flat,
            BackColor = Surface,
            ForeColor = Text,
            Font = Body,
            Cursor = Cursors.Hand,
            Margin = new Padding(0, 0, 8, 0),
            FlatAppearance = { BorderSize = 1, BorderColor = Border }
        };
    }

    public static Label Label(string text, Font? font = null, Color? color = null)
    {
        return new Label
        {
            Text = text,
            AutoSize = true,
            Font = font ?? Body,
            ForeColor = color ?? Text,
            BackColor = Color.Transparent,
            Margin = new Padding(0)
        };
    }

    public static BufferedDataGridView CreateGrid()
    {
        var grid = new BufferedDataGridView
        {
            Dock = DockStyle.Fill,
            BackgroundColor = Surface,
            BorderStyle = BorderStyle.None,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            MultiSelect = false,
            RowHeadersVisible = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            AutoGenerateColumns = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            EnableHeadersVisualStyles = false,
            GridColor = Border,
            CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
            ColumnHeadersHeight = 38,
            RowTemplate = { Height = 34 },
            Font = Body
        };

        grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(248, 250, 252);
        grid.ColumnHeadersDefaultCellStyle.ForeColor = Muted;
        grid.ColumnHeadersDefaultCellStyle.Font = BodyBold;
        grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = Color.FromArgb(248, 250, 252);
        grid.ColumnHeadersDefaultCellStyle.Padding = new Padding(8, 0, 8, 0);
        grid.DefaultCellStyle.BackColor = Surface;
        grid.DefaultCellStyle.ForeColor = Text;
        grid.DefaultCellStyle.SelectionBackColor = AccentSoft;
        grid.DefaultCellStyle.SelectionForeColor = Text;
        grid.DefaultCellStyle.Padding = new Padding(8, 0, 8, 0);
        grid.AlternatingRowsDefaultCellStyle.BackColor = GridAlternate;

        return grid;
    }

    public static void AddTextColumn(
        DataGridView grid,
        string header,
        string property,
        float fillWeight = 100,
        DataGridViewContentAlignment alignment = DataGridViewContentAlignment.MiddleLeft)
    {
        grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = header,
            DataPropertyName = property,
            FillWeight = fillWeight,
            SortMode = DataGridViewColumnSortMode.NotSortable,
            DefaultCellStyle = { Alignment = alignment }
        });
    }

    public static void ShowError(IWin32Window owner, string message, string title = "操作失败")
    {
        MessageBox.Show(owner, message, title, MessageBoxButtons.OK, MessageBoxIcon.Error);
    }

    public static void ShowInfo(IWin32Window owner, string message, string title = "提示")
    {
        MessageBox.Show(owner, message, title, MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    public static GraphicsPath RoundedRect(Rectangle bounds, int radius)
    {
        var path = new GraphicsPath();
        var diameter = radius * 2;
        if (diameter <= 0 || bounds.Width <= diameter || bounds.Height <= diameter)
        {
            path.AddRectangle(bounds);
            return path;
        }

        path.AddArc(bounds.Left, bounds.Top, diameter, diameter, 180, 90);
        path.AddArc(bounds.Right - diameter, bounds.Top, diameter, diameter, 270, 90);
        path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(bounds.Left, bounds.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        return path;
    }
}

internal sealed class BufferedDataGridView : DataGridView
{
    public BufferedDataGridView()
    {
        DoubleBuffered = true;
    }
}
