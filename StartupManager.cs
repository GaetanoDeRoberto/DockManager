using Microsoft.Win32;
using System.Diagnostics;

namespace DockManager;

internal static class StartupManager
{
    private const string RunKey =
        @"Software\Microsoft\Windows\CurrentVersion\Run";

    private const string ValueName = "DockManager";

    public static bool IsEnabled()
    {
        try
        {
            using RegistryKey? key =
                Registry.CurrentUser.OpenSubKey(RunKey, false);

            return key?.GetValue(ValueName) is not null;
        }
        catch
        {
            return false;
        }
    }

    public static bool SetEnabled(bool enabled)
    {
        try
        {
            using RegistryKey key =
                Registry.CurrentUser.CreateSubKey(RunKey);

            if (enabled)
            {
                string executable =
                    Environment.ProcessPath ??
                    Process.GetCurrentProcess().MainModule?.FileName ??
                    string.Empty;

                if (string.IsNullOrWhiteSpace(executable))
                    return false;

                key.SetValue(
                    ValueName,
                    $"\"{executable}\"");
            }
            else
            {
                key.DeleteValue(ValueName, false);
            }

            return true;
        }
        catch
        {
            return false;
        }
    }
}
