namespace Dawn.WinForms.ContextMenu;

using System.ComponentModel;

public sealed class RiotContextMenuStrip : ContextMenuStrip
{
    public RiotContextMenuStrip()
    {
        BackColor = RiotContextMenuDefaults.ContextMenuStripBackgroundColor;
        AllowTransparency = true;
        AutoSize = false;
        ShowItemToolTips = false;
    }


    protected override void OnOpening(CancelEventArgs e)
    {
        SetSize();
        HandleSubmenus(Items);
    }

    private ToolStripItem? _lastItem;
    protected override void OnMouseMove(MouseEventArgs mea)
    {
        base.OnMouseMove(mea);

        // AllowHoverOnDisabled(mea);
    }

    private void AllowHoverOnDisabled(MouseEventArgs mea)
    {
        var item = GetItemAt(mea.Location);

        if (item is null)
        {
            Console.WriteLine("Null");
            return;

        }


        if (item != _lastItem)
        {
            _lastItem = item;
            Console.WriteLine(item.Text);

            if (!item.Enabled)
            {
                item.BackColor = RiotContextMenuDefaults.ContextMenuStripBackgroundColor;
            }
        }
    }
    private void AllowHoverOnDisabled(ToolStripDropDown ts, MouseEventArgs mea)
    {
        var item = ts.GetItemAt(mea.Location);

        if (item is null)
            return;


        if (item != _lastItem)
        {
            _lastItem = item;
            Console.WriteLine(item.Text);

            if (!item.Enabled)
            {
                item.BackColor = RiotContextMenuDefaults.ContextMenuStripBackgroundColor;
            }
        }
    }

    private readonly List<ToolStripDropDown> _hoverFunctionalityAdded = [];

    private void HandleSubmenus(ToolStripItemCollection ic)
    {
        // Console.WriteLine("Settng background");
        foreach (ToolStripItem it in ic)
        {
            if (it is not ToolStripDropDownItem item)
                continue;
            
            var dd = item.DropDown;
            dd.DefaultDropDownDirection = ToolStripDropDownDirection.Left;
            
            var items = item.DropDownItems;
            
            if (!_hoverFunctionalityAdded.Contains(dd))
            {
                item.MouseEnter += (_, _) => item.ShowDropDown();
                _hoverFunctionalityAdded.Add(dd);
            }
            
            dd.AutoSize = false;
            dd.Size = SyncSize(items);
            
            item.BackColor = RiotContextMenuDefaults.ContextMenuStripBackgroundColor;
            HandleSubmenus(item.DropDownItems);
        }
    }

    private Size SyncSize(ToolStripItemCollection ic)
    {
        var height = RiotMenuRenderer.DEFAULT_HEIGHT;
        foreach (ToolStripItem ctxItem in ic)
        {
            if (ctxItem is ToolStripSeparator)
                height += RiotMenuRenderer.SEPARATOR_HEIGHT;
            else height += RiotMenuRenderer.ITEM_HEIGHT;
        }

        return new Size(RiotMenuRenderer.DEFAULT_WIDTH, height);
    }

    private void SetSize()
    {
        var height = RiotMenuRenderer.DEFAULT_HEIGHT;
        foreach (ToolStripItem ctxItem in Items)
        {
            if (ctxItem is ToolStripSeparator)
                height += RiotMenuRenderer.SEPARATOR_HEIGHT;
            else height += RiotMenuRenderer.ITEM_HEIGHT;
        }
        
        // SimpleLogger.Info($"Height is {height}");

        Size  = new Size(RiotMenuRenderer.DEFAULT_WIDTH, height);
    }

    private Bitmap? menuItemHeaderSize;

    [Browsable(false)]
    public bool IsMainMenu { get; set; }

    [Browsable(false)]
    public int MenuItemHeight { get; set; } = 25;

    [Browsable(false)]
    public Color MenuItemTextColor { get; set; } = Color.Empty;

    [Browsable(false)]
    public Color PrimaryColor { get; set; } = Color.Empty;

    private void LoadMenuItemHeight()
    {
        menuItemHeaderSize = IsMainMenu ? new Bitmap(25, 45) : new Bitmap(20, MenuItemHeight);

        foreach (ToolStripItem menuItem in Items)
        {
            RecursivelySetMenuImage(menuItem);
        }
    }

    private void RecursivelySetMenuImage(ToolStripItem item, int depth = 0, int maxDepth = 10)
    {
        if (item is ToolStripSeparator)
            return;
        item.ImageScaling = ToolStripItemImageScaling.None;
        item.Image ??= menuItemHeaderSize;

        if (item is not ToolStripDropDownItem tsmi) 
            return;
        
        if (++depth > maxDepth)
            return;
        
        foreach (ToolStripItem subItem in tsmi.DropDownItems)
            RecursivelySetMenuImage(subItem, depth);
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        if (DesignMode) 
            return;
        
        Renderer = new RiotMenuRenderer();
        LoadMenuItemHeight();
    }
}