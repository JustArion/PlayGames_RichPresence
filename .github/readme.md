[![Test Runner](https://github.com/JustArion/PlayGames_RichPresence/actions/workflows/tests.yml/badge.svg)](https://github.com/JustArion/PlayGames_RichPresence/actions/workflows/tests.yml)

> [!NOTE]
> - The project has a [sister-repo](https://github.com/JustArion/MuMu_RichPresence) for `MuMu Player`
> - Additional options available in the Tray Icon
> - Play Games Developer Emulator is also supported

## Table of Contents
- [Requirements](#requirements)
- [Installation](#installation)
- [Tray Options](#tray-options)
- [Auto-Startup](#auto-startup)
- [Custom Launch Args](#custom-launch-args)
- [Previews](#previews)
- [Building from Source](#building-from-source)
- [Permissions](#permissions)

---
### Requirements
[.NET 9.0.X Desktop Runtime](https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/runtime-desktop-9.0.8-windows-x64-installer) (x64)

---
### Installation
- Standalone
    - No Auto Update
- Portable
    - Auto Update
- Setup
    - Auto Update
    - Shortcut in Start Menu
    - Can be uninstalled by right clicking uninstall in Start Menu
    - Installed in `%appdata%/Local`

---
### Tray Options

- Enabled (Checkbox)
- Open App Directory (Button)
- Run on Startup (Checkbox)
- Hide Tray (Button, Hides the Tray Icon until next start)
- Exit (Closes the program)

---
### Auto-Startup

Enabling `Run on Startup` clones the current launch arguments and runs it as that on startup.

---
### Custom Launch Args

| Argument                 |     Default Value     | Description                                                                                      |
|:-------------------------|:---------------------:|:-------------------------------------------------------------------------------------------------|
| --custom-application-id= |  1204167311922167860  | [Discord Application Id](https://discord.com/developers/applications)                            |
| --seq-url=               | http://localhost:9999 | Seq Logging Platform                                                                             |
| --bind-to=               |         `N/A`         | Binds this process to another process' ID. When the other process exits, this one does too       |
| --extended-logging       |         `N/A`         | File Log Level: Verbose (From Warning)                                                           |
| --rp-disabled-on-start   |         `N/A`         | Rich Presence is Disabled for *Play Games*                                                       |
| --no-file-logging        |         `N/A`         | Disables logging to the file (Located in the current directory)                                  |
| --no-auto-update         |         `N/A`         | Disables Auto-Updates & Checking for Updates (Only affects Velopack (Portable / Setup) versions) |

**Launch Args Example**

`& '.\PlayGames RichPresence.exe' --extended-logging --seq-url=http://localhost:9999`

---
### Previews
![context-menu-preview](images/TrayContextMenuPreview.png)
![rich-presence-preview](images/RichPresencePreview.png)
---

## For advanced users

### Building from Source

#### Pre-Build Requirements

- [.NET SDK 9.0.X](https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/sdk-9.0.304-windows-x64-installer) (x64)<br>
- [Git](https://git-scm.com/downloads)

---
#### Build Steps

**Manual**
```ps1
git clone https://github.com/JustArion/PlayGames_RichPresence && cd "PlayGames_RichPresence"
git submodule update --init --remote --recursive
dotnet publish .\src\PlayGamesRichPresence\ --runtime win-x64 --output ./bin/
```

**with Auto-Update**
```ps1
$VERSION = '1.0.0'
git clone https://github.com/JustArion/PlayGames_RichPresence && cd "PlayGames_RichPresence"
git submodule update --init --remote --recursive
dotnet publish .\src\PlayGames_RichPresence\ --runtime win-x64 --output ./bin/
dotnet tool update -g vpk
vpk pack --packId 'PlayGames-RichPresence' -v "$VERSION" --outputDir 'velopack' --mainExe 'PlayGames RichPresence Standalone.exe' --packDir 'bin' --framework net9-x64-desktop
echo "Successfully built to 'velopack'"
```

**Makefile**
```ps1
git clone https://github.com/JustArion/PlayGames_RichPresence && cd "PlayGames_RichPresence"
make build
echo "Successfully built to 'bin'"
```

**Makefile with Auto-Update**
```ps1
git clone https://github.com/JustArion/PlayGames_RichPresence && cd "PlayGames_RichPresence"
make velopack VERSION="1.0.0"
echo "Successfully built to 'velopack'"
```

After running these commands the output should be in the `bin` folder in the root directory of the repo.

### Permissions

A comprehensive list of permissions the application needs / could need can be found [here](permissions.md)

---

