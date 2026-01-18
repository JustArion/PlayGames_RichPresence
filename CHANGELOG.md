# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Development]
- ⚡️Added support for `.env` files
  - Place a file called `.env` in either the MuMu_RichPresence Directory or it's parent

## [2.0.0] / 2025-12-23
- ⚡️Rich Presences now have clickable links directing them to the respective game's listing on the Play Store
- ⚡️Now shows the game you're playing in the members / direct message list instead of "Google Play Games"
    - This is different than the change below. The one below uses Rich Presences made by Discord, this change uses Rich Presences made by Play Games Rich Presence (If no official rich presence is detected)
- ⚡️Most games will now show "Playing \<game name>" instead of normally "Playing Google Play Games"
    - Discord has added a ton of mobile games to their "Official Presences" list, which we now also use if we detect any of them!
    - We save a list of these "Official Presences" instead of asking Discord for them every time (`detectable.json`)
    - What this does **not** do is show the game in your "Recently Played" list. As this to my knowledge requires each game to have a specific path / process name which can *somewhat* be set by the user but not programatically
- ⚡️ Updated .NET Runtime (.NET 9 -> .NET 10)
    - **Huge apologies for updating the runtime again!**
    - Won't change the runtime version for a really long time now
    - A missing dependencies popup will appear with an option to install the update. Pressing "Install Update" will update the app
- 🦺 Improved startup times for `Auto Update` users. Checking for updates caused the app to wait until checking was done.

## [1.5.1] / 2025-11-16
- 🦺 Bugfix: Enabling Rich Presence on Discord after a game has already started would not show the game as being played. It now correctly updates within 5 seconds.
- 🦺 Bugfix: Fixed a rare case where "Run on Startup" would be checked but would not actually start. This was due to the .exe being moved after "Run on Startup" was checked.
- 🦺 Play Games Rich Presence will now only keep the current version's logs
- ⚡️Added launch arg for hiding the tray icon on start

## [1.5.0] / 2025-08-26
- ⚡️ Updated .NET Runtime (.NET 8 -> .NET 9)
- 🦺 Bugfix: Rich Presences would not show for non-"en-US"/"en-GB" localizations

## [1.4.1] / 2025-02-19
- 🦺 Bugfix: Rich Presences sometimes had no art
- 🦺 Bugfix: Cleared up some impossible app states (Stopping -> Running)
- ⚡️ (Auto-Update only) If checking for updates fail, it will retry a few seconds later up to 3 times
- ⚡️ Added option for Velopack (Auto-Update) users to disable auto-updates
    - Run with `--no-auto-update`

## [1.4.0] / 2025-02-16
- ⚡️ Added the ability to auto-update by downloading the Setup or Portable versions (Standalone won't auto-update)

## [1.3.3] / 2025-02-15
- 🦺 Hotfix: System Packages could slip through as actual games
- ⚡️ Developer Emulator games should now display better game names (`defensederby` -> `Defense Derby`)

## [1.3.2] / 2025-02-11
- 🦺 Bugfix: App would not work if both Play Games and Play Games Developer Edition was not detected (#2)

## [1.3.1] / 2025-02-08
- 🦺 Bugfix: Developer Emulator showed the timestamp from the start of the emulator instead of the start of the game being played

## [1.3.0] / 2025-02-08
- ⚡️ Now supports Google Play Games Developer Emulator

## [1.2.2] / 2024-12-28
- 🦺 Bugfix: The current rich presence wouldn't be restored if previously disabled in the tray and re-enabled.
- 🦺 Bugfix: Disabling the presence from the tray would prevent the app from launching automatically on Windows start.
- 🦺 Bugfix: Exceptions during logging initialization could have lead to crashes.

## [1.2.0] / 2024-11-03
- 🦺 Bugfix: Non-games no longer appear in rich presences (Like that you're playing Google's Settings app)

## [1.1.0] / 2024-10-10
- 🦺 Bugfix: Run on Startup wouldn't work correctly
- ⚡️ Rich Presence resilience optimizations
- ⚡️ Added +1 additional item to the permissions notice

## [1.0.0] / 2024-10-08
- ⚡️Initial release!

[Development]: https://github.com/JustArion/PlayGames_RichPresence/compare/v2.0.0...HEAD
[2.0.0]: https://github.com/JustArion/PlayGames_RichPresence/compare/v1.5.1...v2.0.0
[1.5.1]: https://github.com/JustArion/PlayGames_RichPresence/compare/v1.5.0...v1.5.1
[1.5.0]: https://github.com/JustArion/PlayGames_RichPresence/compare/v1.4.0...v1.5.0
[1.4.0]: https://github.com/JustArion/PlayGames_RichPresence/compare/v1.3.3...v1.4.0
[1.3.3]: https://github.com/JustArion/PlayGames_RichPresence/compare/v1.3.2...v1.3.3
[1.3.2]: https://github.com/JustArion/PlayGames_RichPresence/compare/v1.3.1...v1.3.2
[1.3.1]: https://github.com/JustArion/PlayGames_RichPresence/compare/v1.3.0...v1.3.1
[1.3.0]: https://github.com/JustArion/PlayGames_RichPresence/compare/v1.2.2...v1.3.0
[1.2.2]: https://github.com/JustArion/PlayGames_RichPresence/compare/1.2.0...v1.2.2
[1.2.0]: https://github.com/JustArion/PlayGames_RichPresence/compare/1.1.0...1.2.0
[1.1.0]: https://github.com/JustArion/PlayGames_RichPresence/compare/1.0.0...1.1.0
[1.0.0]: https://github.com/JustArion/PlayGames_RichPresence/tree/1.0.0