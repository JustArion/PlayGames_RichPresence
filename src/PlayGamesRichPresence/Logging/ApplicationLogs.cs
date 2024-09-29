namespace Dawn.PlayGames.RichPresence.Logging;

using global::Serilog;
using global::Serilog.Core;
using global::Serilog.Events;
using Serilog.CustomEnrichers;
using Serilog.Themes;

internal static class ApplicationLogs
{
    private const string LOGGING_FORMAT =
        "{Level:u1} {Timestamp:yyyy-MM-dd HH:mm:ss.ffffff}   [{Source}] {Message:lj}{NewLine}{Exception}";

    #if RELEASE
    private const string DEFAULT_SEQ_URL = "http://localhost:9999";
    #endif
    private static bool _initialized;

    public static void Initialize()
    {
        if (_initialized)
            return;

        _initialized = true;

        if (AttachConsole(ATTACH_PARENT_PROCESS))
            Console.WriteLine("[*] Attached Console to Parent");

        if (!Console.IsOutputRedirected)
        {
            var stdout = new StreamWriter(Console.OpenStandardOutput())
                { AutoFlush = true };
            Console.SetOut(stdout);
        }

        var args = Environment.GetCommandLineArgs();

        try
        {
            var config = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .Enrich.WithClassName()
                .Enrich.WithProcessName()
                .Enrich.FromLogContext()
                .WriteTo.Console(outputTemplate: LOGGING_FORMAT, theme: BlizzardTheme.GetTheme,
                    restrictedToMinimumLevel: LogEventLevel.Verbose,
                    applyThemeToRedirectedOutput: true, standardErrorFromLevel: LogEventLevel.Error)
                .WriteTo.File(Path.Combine(AppContext.BaseDirectory, $"{Application.ProductName}.log"),
                outputTemplate: LOGGING_FORMAT,
                restrictedToMinimumLevel: args.Contains("--extended-logging")
                    ? LogEventLevel.Verbose
                    : LogEventLevel.Warning,
                flushToDiskInterval: TimeSpan.FromSeconds(1));



            #if RELEASE
            if (args.FirstOrDefault(a => a.StartsWith("--seq-url=")) is { } seqArg)
            {
                var customServerUrl = seqArg.Split('=');
                if (customServerUrl.Length > 1 && Uri.TryCreate(customServerUrl[1], UriKind.Absolute, out _))
                    config.WriteTo.Seq(customServerUrl[1],
                        restrictedToMinimumLevel: LogEventLevel.Warning);
                else
                    config.WriteTo.Seq(DEFAULT_SEQ_URL,
                        restrictedToMinimumLevel: LogEventLevel.Warning);
            }
            else
            {
                // This is personal preference, but you can set your Seq server to catch :9999 too.
                // (Logs to nowhere if there's no Seq server listening on port 9999
                config.WriteTo.Seq(DEFAULT_SEQ_URL, restrictedToMinimumLevel: LogEventLevel.Warning);
            }
            #endif

            Log.Logger = config.CreateLogger();

            AppDomain.CurrentDomain.UnhandledException +=
                (_, eo) => Log.Fatal(eo.ExceptionObject as Exception, "Unhandled Exception");

            #if !DEBUG
            AppDomain.CurrentDomain.ProcessExit +=
                (_, _) => Log.Information("Shutting Down...");
            #endif

            Log.Information("Initialized");
        }
        catch (Exception e)
        {
            Console.Error.WriteLine(e);
        }
    }
}
