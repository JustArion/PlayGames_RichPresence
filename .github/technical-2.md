There is a case where the `Google Play Games beta` app is entirely killed by something unexpected (Task Manager or other things)
- Service.exe
- client.exe
- crosvm.exe (Multiple)

This would cause the app state to be frozen in the log file. Meaning the Rich Presence will show that you're playing a game when you're not.

We can fix this by waiting for `crosvm.exe` to exit. If it does. We can clear the Rich Presence.

Issues:
- crosvm has multiple processes

We can figure out what's the __main__ process by sorting all `crosvm.exe` processes by their start time. 
The earliest process is the main process.

If that doesn't work out in the future, we can also query the process' command line. The main process has some unique command line entries.

It doesn't necessarily matter what those args are, but rather that the main process will have significantly more than the sub-processes.

We could query the dreaded WMI if there's any problems in the future. Though that's probably a last resort.

**Additional Concerns:**
- Access Violations

If by some chance `crosvm.exe` is ran as administrator, subscribing to their exit events will fail due to an access violation.

This should never realistically happen and I don't think the scope of our application should elevate to administrator to match it either.

We can probably poll query all `crosvm.exe` processes to wait for an exit like that. It's messy...

eg.
```csharp
// [ Async & Long Running ]

while (true)
{
    var process = Process.GetProcessesByName("crosvm").OrderBy(x => x.StartTime).FirstOrDefault();

    if (process == null)
    {
        _currentAppState = AppSessionState.Stopped;
        // Clear Rich Presence
        break;
    }
    await Task.Delay(TimeSpan.FromSeconds(5));
}

```