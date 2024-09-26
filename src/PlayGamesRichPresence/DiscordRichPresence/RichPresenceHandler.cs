// #define LISTEN_TO_RPCS
namespace Dawn.PlayGames.RichPresence.DiscordRichPresence;

using DiscordRPC;
using global::Serilog;
using global::Serilog.Core;

public class RichPresenceHandler : IDisposable
{
    private const string DEFAULT_APPLICATION_ID = "1204167311922167860";

    private readonly Logger _logger = (Logger)Log.ForContext<RichPresenceHandler>();
    private DiscordRpcClient _client = null!;

    public RichPresenceHandler()
    {
        InitializeUnderlyingClient();
        AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
    }

    private void OnProcessExit(object? sender, EventArgs e) => Dispose();

    public void SetPresence(RichPresence presence)
    {
        _client.SetPresence(presence);
    }

    public void ClearPresence()
    {
        _client.SetPresence(null);
    }

    private void InitializeUnderlyingClient()
    {
        _logger.Debug("Initializing IPC Client");

        var applicationId = DEFAULT_APPLICATION_ID;

        if (Environment.GetCommandLineArgs().FirstOrDefault(x => x.StartsWith("--custom-application-id=")) is { } arg)
        {
            var customApplicationId = arg.Split('=');

            if (customApplicationId.Length > 1 && long.TryParse(customApplicationId[1], out _))
                applicationId = customApplicationId[1];
        }

        _client = new DiscordRpcClient(applicationId, -1, (SerilogToDiscordLogger)_logger);

        _client.SkipIdenticalPresence = true;
        _client.Initialize();
        #if LISTEN_TO_RPCS
        _client.OnRpcMessage += (_, msg) => Log.Debug("Received RPC Message: {@Message}", msg);
        #endif
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
