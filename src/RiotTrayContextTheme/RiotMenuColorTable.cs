namespace Dawn.WinForms.ContextMenu;

using System.Diagnostics.CodeAnalysis;

[SuppressMessage("ReSharper", "ConvertToAutoProperty")]
public class RiotMenuColorTable(Color primaryColor) : ProfessionalColorTable
{
    private readonly Color backColor = Color.White;
    private readonly Color leftColumnColor = Color.Transparent;
    private readonly Color borderColor = RiotContextMenuDefaults.ContextMenuStripBorderColor;
    private readonly Color menuItemBorderColor = primaryColor;
    private readonly Color menuItemSelectedColor = primaryColor;

    public override Color ToolStripDropDownBackground => backColor;
    public override Color MenuBorder => borderColor;
    public override Color MenuItemBorder => menuItemBorderColor;
    public override Color MenuItemSelected => menuItemSelectedColor;
    public override Color ImageMarginGradientBegin => leftColumnColor;
    public override Color ImageMarginGradientMiddle => leftColumnColor;
    public override Color ImageMarginGradientEnd => leftColumnColor;
}