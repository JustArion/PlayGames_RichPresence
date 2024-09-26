namespace Dawn.PlayGames.RichPresence.Tray;

using System.Linq.Expressions;
using global::Serilog;
using WinForms.ContextMenu;

public class RichPresence_Tray
{
    internal NotifyIcon Tray { get; private set; }
    private readonly ILogger _logger = Log.ForContext<RichPresence_Tray>();
    public RichPresence_Tray()
    {
        Tray = new();

        Tray.Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
        Tray.Text = Application.ProductName;
        Tray.Visible = true;

        AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
        Tray.Disposed += OnTrayDisposed;

        Tray.ContextMenuStrip = new RiotContextMenuStrip();

        AddStripItems(Tray.ContextMenuStrip.Items);
    }

    private void AddStripItems(ToolStripItemCollection items)
    {
        items.AddRange(Header());
        try
        {
            items.Add(Enabled());
            items.Add(RunOnStartup());
            items.Add(HideTray());
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to add tray items");
        }
        finally
        {
            items.Add(Exit());

            LogInteractionsRecursively(items);
        }
    }

    private void LogInteractionsRecursively(ToolStripItemCollection items)
    {
        foreach (ToolStripItem item in items)
        {
            item.Click += (_, _) => Log.Verbose("OnMenuItemClick: {MenuItemText}", item.Text);

            if (item is ToolStripMenuItem menuItem)
                LogInteractionsRecursively(menuItem.DropDownItems);
        }
    }

    private const string RICH_PRESENCE_DISABLED_COMMAND_LINE_ARGUMENT = "--rp-disabled-on-start";

    public event EventHandler<bool> RichPresenceEnabledChanged = delegate { };

    private ToolStripMenuItem Enabled()
    {
        ApplicationFeatures.SyncFeature(f => f.RichPresenceEnabled, !Environment.GetCommandLineArgs().Contains(RICH_PRESENCE_DISABLED_COMMAND_LINE_ARGUMENT));

        var enabledItem = new ToolStripMenuItem("Enabled");
        enabledItem.Checked = ApplicationFeatures.GetFeature(f => f.RichPresenceEnabled);

        enabledItem.Click += (_, _) =>
        {
            var enabled = !enabledItem.Checked;

            ChangeEnabledStateOnStartupIfNecessary(enabled);

            ApplicationFeatures.SetFeature(f => f.RichPresenceEnabled, enabled);
            enabledItem.Checked = enabled;

            RichPresenceEnabledChanged.Invoke(this, enabled);
        };

        return enabledItem;
    }

    private void ChangeEnabledStateOnStartupIfNecessary(bool enabled)
    {
        if (!Startup.StartsWithWindows(Application.ProductName!))
            return;

        Startup.StartWithWindows(Application.ProductName!,
            enabled
                ? Environment.CommandLine.Replace(RICH_PRESENCE_DISABLED_COMMAND_LINE_ARGUMENT, string.Empty)
                : $"{Environment.CommandLine} {RICH_PRESENCE_DISABLED_COMMAND_LINE_ARGUMENT}");
    }

    private ToolStripMenuItem HideTray() => new("Hide Tray", null, (_, _) => Tray.Visible = false);

    private ToolStripMenuItem RunOnStartup()
    {
        var startup = new ToolStripMenuItem("Run on Startup");
        startup.Checked = Startup.StartsWithWindows(Application.ProductName!);

        startup.Click += delegate
        {
            if (startup.Checked)
                Startup.RemoveStartup(Application.ProductName!);
            else
                Startup.StartWithWindows(Application.ProductName!, Environment.CommandLine);

            startup.Checked = !startup.Checked;
        };

        return startup;
    }

    private ToolStripMenuItem Exit() => new("Exit", null, (_, _) => Tray.Dispose());

    private ToolStripItem[] Header()
    {
        var header = new ToolStripMenuItem("Play Games Rich Presence");
        header.Enabled = false;

        var separator = new ToolStripSeparator();

        return [header, separator];
    }

    private void OnTrayDisposed(object? sender, EventArgs e)
    {
        AppDomain.CurrentDomain.ProcessExit -= OnProcessExit;
        Application.Exit();
    }

    private void OnProcessExit(object? sender, EventArgs e) => Tray.Visible = false;
}
