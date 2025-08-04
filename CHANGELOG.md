# Changelog

## v1.5.0

- ⚡️ Updated .NET Runtime (.NET 8 -> .NET 9)

## v1.4.1

- 🦺 Bugfix: Rich Presences sometimes had no art
- 🦺 Bugfix: Cleared up some impossible app states (Stopping -> Running)
- ⚡️ (Auto-Update only) If checking for updates fail, it will retry a few seconds later up to 3 times
- ⚡️ Added option for Velopack (Auto-Update) users to disable auto-updates
    - Run with `--no-auto-update`

## v1.4.0

- ⚡️ Added the ability to auto-update by downloading the Setup or Portable versions (Standalone won't auto-update)

## v1.3.3

- 🦺 Hotfix: System Packages could slip through as actual games
- ⚡️ Developer Emulator games should now display better game names (`defensederby` -> `Defense Derby`)

## v1.3.2

- 🦺 Bugfix: App would not work if both Play Games and Play Games Developer Edition was not detected (#2)

## v1.3.1

- 🦺 Bugfix: Developer Emulator showed the timestamp from the start of the emulator instead of the start of the game being played

## v1.3.0

- ⚡️ Now supports Google Play Games Developer Emulator

## v1.2.2

- 🦺 Bugfix: The current rich presence wouldn't be restored if previously disabled in the tray and re-enabled.
- 🦺 Bugfix: Disabling the presence from the tray would prevent the app from launching automatically on Windows start.
- 🦺 Bugfix: Exceptions during logging initialization could have lead to crashes.

## v1.2.0

- 🦺 Bugfix: Non-games no longer appear in rich presences (Like that you're playing Google's Settings app)

## v1.1.0

- 🦺 Bugfix: Run on Startup wouldn't work correctly
- ⚡️ Rich Presence resilience optimizations
- ⚡️ Added +1 additional item to the permissions notice

