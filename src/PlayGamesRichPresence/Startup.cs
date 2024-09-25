namespace Dawn.PlayGames.RichPresence;

using Microsoft.Win32;

public static class Startup
{
    private const string STARTUP_SUBKEY = @"Software\Microsoft\Windows\CurrentVersion\Run";
    // Write Only
    public static void StartWithWindows(string key, string pathAndArgs)
    {
        using var startupKey = Registry.CurrentUser.OpenSubKey(STARTUP_SUBKEY, true)!;
        startupKey.SetValue(key, pathAndArgs);
    }
    public static void RemoveStartup(string key)
    {
        using var startupKey = Registry.CurrentUser.OpenSubKey(STARTUP_SUBKEY, true)!;
        startupKey.DeleteValue(key, false);
    }
    // ---

    // Read-Only
    public static bool Contains(string key, string value)
    {
        using var startupKey = Registry.CurrentUser.OpenSubKey(STARTUP_SUBKEY, false)!;
        var regVal = startupKey.GetValue(key);

        return regVal?.ToString()?.Contains(value) ?? false;
    }
    public static bool StartsWithWindows(string key)
    {
        using var startupKey = Registry.CurrentUser.OpenSubKey(STARTUP_SUBKEY, false)!;
        var regVal = startupKey.GetValue(key);

        return regVal != null;
    }

    public static bool ValidateStartsWithWindows(string key, string pathAndArgs)
    {
        using var startupKey = Registry.CurrentUser.OpenSubKey(STARTUP_SUBKEY, false)!;
        var regVal = startupKey.GetValue(key);

        return regVal?.ToString() == pathAndArgs;
    }
    // ---
}