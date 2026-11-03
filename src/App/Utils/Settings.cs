using Microsoft.Win32;
using System;

namespace CoffeeBean.Utils;

internal enum ScopeMode
{
    Always,
    WhenRunning,
    WhenForeground
}

internal static class Settings
{
    private const string KeyPath = @"HKEY_CURRENT_USER\Software\CoffeeBean";

    public static bool RunEnabled
    {
        get => (string)Registry.GetValue(KeyPath, "RunEnabled", null) != "0";
        set => Registry.SetValue(KeyPath, "RunEnabled", value ? "1" : "0");
    }

    public static ScopeMode ScopeMode
    {
        get => Enum.TryParse((string)Registry.GetValue(KeyPath, "ScopeMode", null), out ScopeMode mode)
            ? mode
            : ScopeMode.Always;
        set => Registry.SetValue(KeyPath, "ScopeMode", value.ToString());
    }

    public static string ScopeProcessName
    {
        get
        {
            var value = (string)Registry.GetValue(KeyPath, "ScopeProcessName", null);
            return string.IsNullOrWhiteSpace(value) ? null : value;
        }
        set
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                using var key = Registry.CurrentUser.OpenSubKey(@"Software\CoffeeBean", true);
                key?.DeleteValue("ScopeProcessName", false);
            }
            else
            {
                Registry.SetValue(KeyPath, "ScopeProcessName", TargetProcess.NormalizeName(value));
            }
        }
    }
}
