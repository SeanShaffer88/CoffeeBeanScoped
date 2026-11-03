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
    private const string KeyPath = @"HKEY_CURRENT_USER\Software\CoffeeBeanScoped";

    static Settings()
    {
        // One-time copy of settings saved by versions before the rename to CoffeeBeanScoped.
        try
        {
            using var legacyKey = Registry.CurrentUser.OpenSubKey(@"Software\CoffeeBean");
            if (legacyKey == null)
            {
                return;
            }
            using var key = Registry.CurrentUser.CreateSubKey(@"Software\CoffeeBeanScoped");
            foreach (var name in new[] { "RunEnabled", "ScopeMode", "ScopeProcessName" })
            {
                if (key.GetValue(name) == null && legacyKey.GetValue(name) is object value)
                {
                    key.SetValue(name, value);
                }
            }
        }
        catch (Exception)
        {
            // Migration is best-effort; defaults apply if it cannot run.
        }
    }

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
                using var key = Registry.CurrentUser.OpenSubKey(@"Software\CoffeeBeanScoped", true);
                key?.DeleteValue("ScopeProcessName", false);
            }
            else
            {
                Registry.SetValue(KeyPath, "ScopeProcessName", TargetProcess.NormalizeName(value));
            }
        }
    }
}
