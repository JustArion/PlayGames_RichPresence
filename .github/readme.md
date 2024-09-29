> [!NOTE]
> Additional options available in the Tray Icon

### Requirements
[.NET 8.0.X Desktop Runtime](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)

### Permissions
- `IO`
  - Connects to named pipe (`discord-ipc-{0}`)
    - Handled by Nuget package [DiscordRichPresence](https://www.nuget.org/packages/DiscordRichPresence)
  - `Write Access`
    - Writes a single Log File to the `PlayGames RichPresence.exe` directory
      - `PlayGames RichPresence.log`
    - _Can_ create a single registry key (`\HKCU\Software\Microsoft\Windows\CurrentVersion\Run\PlayGames RichPresence`) 
      - Default is off, configurable by the user
  - `Read Access`
    - Reads the file (`AppData/Local/Google/Play Games/Service.log`) if it exists | ( See [Technical-Document](technical-1.md) )
    - Reads a single registry key (`\HKCU\Software\Microsoft\Windows\CurrentVersion\Run\PlayGames RichPresence`) 
      - Run on Startup
- `Network`
  - `Upload Access`
    - Sends logging data (`http://localhost:9999`)
      - Configurable by the user / command line, Handled by Nuget package [Serilog.Sinks.Seq](https://www.nuget.org/packages/Serilog.Sinks.Seq) & external application ([Seq](https://datalust.co/seq))

### Custom Launch Args

| Argument                 |     Default Value     | Description                                                           |
|:-------------------------|:---------------------:|:----------------------------------------------------------------------|
| --custom-application-id= |  1204167311922167860  | [Discord Application Id](https://discord.com/developers/applications) |
| --seq-url=               | http://localhost:9999 | Seq Logging Platform                                                  |
| --extended-logging       |         `N/A`         | File Log Level: Verbose (From Warning)                                |
| --rp-disabled-on-start   |         `N/A`         | Rich Presence is Disabled for *Play Games*                            |

**Launch Args Example**

`& '.\PlayGames RichPresence.exe' --extended-logging --seq-url=http://localhost:9999`

### Tray Options

- Enabled (Checkbox)
- Run on Startup (Checkbox)
- Hide Tray (Button, Hides the Tray Icon until next start)
- Exit (Closes the program)

### Startup

Enabling `Run on Startup` clones the current launch arguments and runs it as that on startup.