﻿using Dawn.PlayGames.RichPresence.Logging;
using Velopack;
using Velopack.Sources;

namespace Dawn.PlayGames.RichPresence.Tools;

internal static class AutoUpdate
{
    internal static async Task Velopack()
    {
        VelopackApp.Build().Run(new SerilogToMicrosoftLogger(Log.Logger));

        var manager = new UpdateManager(new GithubSource("https://github.com/JustArion/PlayGames_RichPresence", null, false));

        if (manager.IsInstalled)
            Log.Information("The Velopack Update Manager is present");
        else
        {
            Log.Information("The Auto Update feature is not present and won't be used");
            return;
        }

        var version = await manager.CheckForUpdatesAsync();
        if (version == null)
            return;

        await manager.DownloadUpdatesAsync(version);

        Log.Information("Updates are ready to be installed and will be applied on next restart ({Version})", version.TargetFullRelease.Version);
        // manager.ApplyUpdatesAndRestart(version);
    }
}
