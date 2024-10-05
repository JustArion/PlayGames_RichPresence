#define LISTEN_TO_RPCS
using DiscordRPC.Message;

namespace Dawn.PlayGames.RichPresence.DiscordRichPresence;

using DiscordRPC;
using global::Serilog;
using global::Serilog.Core;

public class RichPresenceHandler : IDisposable
{
    private const string DEFAULT_APPLICATION_ID = "1204167311922167860";

    private readonly Logger _logger = (Logger)Log.ForContext<RichPresenceHandler>();
    private DiscordRpcClient _client = null!;
    private RichPresence? _currentPresence;

    public RichPresenceHandler()
    {
        InitializeUnderlyingClient();
        AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
    }

    private void OnProcessExit(object? sender, EventArgs e) => Dispose();

    public void SetPresence(RichPresence? presence)
    {
        if (!ApplicationFeatures.GetFeature(x => x.RichPresenceEnabled))
            return;

        if (presence != null)
            Log.Information("Setting Rich Presence for {GameTitle}", presence.Details);

        _client.SetPresence(presence);
        Interlocked.Exchange(ref _currentPresence, presence);
    }

    public void RemovePresence()
    {
        _client.ClearPresence();

        Interlocked.Exchange(ref _currentPresence, null);
    }

    private void InitializeUnderlyingClient()
    {
        _logger.Debug("Initializing IPC Client");

        var applicationId = Arguments.HasCustomApplicationId
            ? Arguments.CustomApplicationId
            : DEFAULT_APPLICATION_ID;

        _client = new DiscordRpcClient(applicationId, logger: (SerilogToDiscordLogger)_logger);

        _client.SkipIdenticalPresence = true;
        _client.Initialize();
        #if LISTEN_TO_RPCS
        _client.OnRpcMessage += (_, msg) => Log.Debug("Received RPC Message: {@Message}", msg);
        #endif

        _client.OnPresenceUpdate += OnPresenceUpdate;
    }

    private void OnPresenceUpdate(object _, PresenceMessage args)
    {
        if (args.Presence == null)
            return;

        // We clear up some ghosting
        if (_currentPresence != null)
            return;

        _logger.Verbose("Attempting to correct some rich presence ghosting");
        _client.ClearPresence();
    }


    public void Dispose()
    {
        Log.Debug("Disposing IPC Client");
        _client.ClearPresence();
        _client.Dispose();
        _client = null!;
        GC.SuppressFinalize(this);
    }
}
