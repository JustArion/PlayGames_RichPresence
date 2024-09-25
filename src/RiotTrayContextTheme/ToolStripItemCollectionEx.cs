namespace Dawn.WinForms.ContextMenu;

internal static class ToolStripItemCollectionEx
{
    public static void Add(this ToolStripItemCollection items, string text, EventHandler onClick) => items.Add(text, null, onClick);
}