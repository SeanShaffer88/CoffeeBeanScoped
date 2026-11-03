using System;
using System.Diagnostics;
using System.IO;

namespace CoffeeBean.Utils;

internal static class TargetProcess
{
    public static string NormalizeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        var trimmed = name.Trim().Trim('"');
        try
        {
            trimmed = Path.GetFileName(trimmed);
        }
        catch (ArgumentException)
        {
            // Keep the typed text as-is if it is not a valid path.
        }

        if (trimmed.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
        {
            trimmed = trimmed.Substring(0, trimmed.Length - 4);
        }

        return trimmed;
    }

    public static bool IsRunning(string processName)
    {
        if (string.IsNullOrWhiteSpace(processName))
        {
            return false;
        }

        var processes = Process.GetProcessesByName(processName);
        try
        {
            return processes.Length > 0;
        }
        finally
        {
            foreach (var process in processes)
            {
                process.Dispose();
            }
        }
    }

    public static bool IsInForeground(string processName)
    {
        if (string.IsNullOrWhiteSpace(processName))
        {
            return false;
        }

        var window = WinApi.GetForegroundWindow();
        if (window == IntPtr.Zero)
        {
            return false;
        }

        WinApi.GetWindowThreadProcessId(window, out var processId);
        if (processId == 0)
        {
            return false;
        }

        try
        {
            using var process = Process.GetProcessById((int)processId);
            return string.Equals(process.ProcessName, processName, StringComparison.OrdinalIgnoreCase);
        }
        catch (ArgumentException)
        {
            return false;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    public static bool ConditionIsMet(ScopeMode mode, string processName)
    {
        switch (mode)
        {
            case ScopeMode.Always:
                return true;
            case ScopeMode.WhenRunning:
                return IsRunning(processName);
            case ScopeMode.WhenForeground:
                return IsInForeground(processName);
            default:
                return true;
        }
    }
}
