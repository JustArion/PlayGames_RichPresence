namespace Dawn.WinForms.ContextMenu;

using System.Drawing.Drawing2D;

public class RiotMenuRenderer() : ToolStripProfessionalRenderer(new RiotMenuColorTable(primaryColor))
{
    private static readonly Color primaryColor = Color.FromArgb(0x24, 0x24, 0x24);
    private static readonly Color textColor = Color.FromArgb(0xf9, 0xf9, 0xf9);
    private readonly int arrowThickness = 3;

    private List<string> _names = [];
    

    protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
    {
        e.Item.ForeColor = e.Item.Selected ? Color.White : textColor;

        var color = e.Item.Enabled ? e.Item.Selected ? Color.White : textColor : ColorTranslator.FromHtml("#757575");

        if (e.Item is not ToolStripDropDownItem item)
        {
            base.OnRenderItemText(e);
            return;
        }
        
        var paddingNeeded = item.Image == null ? 0 : item.Image.Width + 20;
        
        var textWidth = (item.Text?.Length * 8) ?? 0;

        var textY = (item.Height - item.Font.Height) / 2;
        
        TextRenderer.DrawText(e.Graphics, item.Text, item.Font, new Point(paddingNeeded, textY), color);
    }

    protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
    {
        if (e.Item is not ToolStripSeparator separator)
            return;
        // base.OnRenderSeparator(e);

        separator.AutoSize = false;
        separator.Margin = new(-1);
        separator.Padding = new(0);
        separator.Height = 1;
        separator.Size = new(separator.Width, 1);
        
        // Draw a line
        var rect = new Rectangle(0, 0, separator.Width, separator.Height);
        using var pen = new Pen(ColorTranslator.FromHtml("#242424"), 1);
        
        // Draw the separator
        var yAxis = rect.Y - -(rect.Bottom / 2);
        // Console.WriteLine($"Axis: {yAxis}");
        // yAxis = 3;
        e.Graphics.DrawLine(pen, rect.X, yAxis, rect.X + rect.Width, yAxis);
    }

    protected override void OnRenderItemCheck(ToolStripItemImageRenderEventArgs e)
    {

        var img = e.Image;
        
        e = new ToolStripItemImageRenderEventArgs(e.Graphics, e.Item, img, e.Item.ContentRectangle);
        
        // if (e.Image != null)
        //     e.Graphics.DrawImage(e.Image, e.ImageRectangle, -12, -8, e.ImageRectangle.Width, e.ImageRectangle.Height, GraphicsUnit.Pixel);


        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        using var path = new GraphicsPath();
        using var pen = new Pen(Color.White, 3);

        // var points = new Point[]
        // {
        //     new(0, 5),
        //     new(5, 0),
        //     new(15, 10),
        // };
        //
        // path.AddLines(points);
        //
        // // Flip the paths vertically
        // path.Transform(new Matrix(1, 0, 0, -1, 15, 20));
        
        var points = new Point[]
        {
            new(15, 15),
            new(20, 20),
            new(30, 10),
        };

        path.AddLines(points);
        
        g.DrawPath(pen, path);
    }

    protected override void OnRenderDropDownButtonBackground(ToolStripItemRenderEventArgs e)
    {
        // base.OnRenderDropDownButtonBackground(e);

        if (e.Item is not ToolStripDropDownItem dropDown)
            return;

        var dd = dropDown.DropDown;

        dd.BackColor = RiotContextMenuDefaults.ContextMenuStripBackgroundColor;

        dropDown.AutoSize = false;
        
        foreach (ToolStripItem downItem in dropDown.DropDownItems)
        {
            var args = new ToolStripItemRenderEventArgs(e.Graphics, downItem);

            if (downItem is ToolStripDropDownItem)
                OnRenderDropDownButtonBackground(args);
            else
                OnRenderItemBackground(args);
        }
        
        // e.Item.Margin = new(-39, 0,0,0);
        e.Item.Margin = new(-37, 0,0,0);
        // e.Item.Margin = new(-38, 0,0,0);

        ApplyThemeStyle(e.Item);
        
        if (_padding.Right > 0)
        {
            var text = e.Item.Text;
            e.Item.Text = text == null ? string.Empty : new string(' ', _padding.Right / 2) + text.Trim(); 
        }

        if (e.Item is { Selected: false, Pressed: false })
            return;
        
        var hoverColor = "#1f1e21";
        var hoverColorBrush = new SolidBrush(ColorTranslator.FromHtml(hoverColor));
        var contentRectangle = e.Item.ContentRectangle;
        var fillArea = contentRectangle with { Width = contentRectangle.Width + 11 };
        e.Graphics.FillRectangle(hoverColorBrush, fillArea);
    }


    protected override void OnRenderArrow(ToolStripArrowRenderEventArgs e)
    {
        if (e.Item == null)
            return;
        
        var graph = e.Graphics;
        var arrowSize = new Size(5, 12);
        var arrowColor = e.Item.Selected ? Color.White : textColor;
        var rect = new Rectangle(e.ArrowRectangle.Location.X, (e.ArrowRectangle.Height - arrowSize.Height) / 2,
            arrowSize.Width, arrowSize.Height);
        
        // We Move the arrow on the X-Axis -50px
        rect.X = (int)(rect.X / 1.85);

        if (!string.IsNullOrWhiteSpace(e.Item.Text))
            rect.X = Math.Clamp(rect.X - e.Item.Text.Length * 8, e.Item.Text.Length * 10, int.MaxValue);

        using var path = new GraphicsPath();
        using var pen = new Pen(arrowColor, arrowThickness);
        //Drawing
        graph.SmoothingMode = SmoothingMode.AntiAlias;
        path.AddLine(rect.Left, rect.Top, rect.Right, rect.Top + rect.Height / 2);
        path.AddLine(rect.Right, rect.Top + rect.Height / 2, rect.Left, rect.Top + rect.Height);
        graph.DrawPath(pen, path);
    }

    private static readonly Padding _padding = new(10,0,5,0);

    private static ToolStripMenuItem ? _lastItem;
    protected override void OnRenderItemImage(ToolStripItemImageRenderEventArgs e)
    {
        if (e.Item is ToolStripMenuItem mi)
            _lastItem = mi;
        
        if (e.Image == null)
            return;

        var paddedRect = e.ImageRectangle with
        {
            X = e.ImageRectangle.X + _padding.Left, 
            Y = e.ImageRectangle.Y + _padding.Top
        };
            
        e.Graphics.DrawImage(e.Image, paddedRect);

    }

    protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
    {
        var item = e.Item;
        ApplyThemeStyle(item);

        if (_padding.Right > 0)
        {
            var text = e.Item.Text;
            item.Text = text == null ? string.Empty : new string(' ', _padding.Right / 2) + text.Trim();
        }
        
        if (item.Selected || item.Pressed)
        {
            var hoverColor = "#1f1e21";
            var hoverColorBrush = new SolidBrush(ColorTranslator.FromHtml(hoverColor));
            e.Graphics.FillRectangle(hoverColorBrush, item.ContentRectangle);
            
            return;
        }
        
        base.OnRenderMenuItemBackground(e);
    }

    internal const int SEPARATOR_HEIGHT = 0;
    internal const int ITEM_HEIGHT = 34;
    internal const int DEFAULT_HEIGHT = 3;
    internal const int DEFAULT_WIDTH = 266;
    // internal const int DEFAULT_WIDTH = 266;
    private void ApplyThemeStyle(ToolStripItem e)
    {
        e.Overflow = ToolStripItemOverflow.Never;
        e.AutoSize = false;
        e.Height = ITEM_HEIGHT;
        e.AutoToolTip = false;
        e.Width = DEFAULT_WIDTH;
        e.Size = new(DEFAULT_WIDTH, ITEM_HEIGHT);
        e.ForeColor = RiotContextMenuDefaults.ContextMenuStripTextColor;
        e.ImageTransparentColor = Color.White;
        e.ImageAlign = ContentAlignment.MiddleRight;
    }
}