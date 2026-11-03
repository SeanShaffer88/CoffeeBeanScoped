using CoffeeBean.Utils;
using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Threading;

namespace CoffeeBean;

public partial class NotifyIcon
{
    private static readonly string githubUrl = "https://github.com/PolarGoose/CoffeeBean";
    private readonly Icon enabledIcon = LoadIcon("iconEnabled");
    private readonly Icon disabledIcon = LoadIcon("iconDisabled");
    private readonly DispatcherTimer scopeTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
    private bool screenLockEnabled;
    private bool wakeHeld;
    private bool screenLockStatusBeforeSessionWasLocked;
    private ScopeMode scopeMode = Settings.ScopeMode;
    private string scopeProcessName = Settings.ScopeProcessName;

    public static string VersionMenuText =>
        $"Version: {AssemblyInfoRetriever.InformationalVersion}\n{githubUrl}";

    public bool IsScreenLockEnabled
    {
        get => screenLockEnabled;
        set
        {
            if (screenLockEnabled == value)
            {
                return;
            }
            screenLockEnabled = value;
            EnabledMenuItem.Checked = value;
            UpdateWakeState();
        }
    }

    public NotifyIcon()
    {
        InitializeComponent();
        TrayIcon.Icon = disabledIcon;
        LaunchAtStartupMenuItem.Checked = AutoStartup.IsEnabled;
        LaunchEnabledMenuItem.Checked = Settings.RunEnabled;
        RefreshScopeMenu();
        scopeTimer.Tick += (sender, e) => UpdateWakeState();
        scopeTimer.Start();
        SystemEvents.SessionSwitch += OnSessionSwitchEvent;
    }

    public void SetScope(ScopeMode mode, string processName)
    {
        scopeMode = mode;
        scopeProcessName = TargetProcess.NormalizeName(processName);
        Settings.ScopeMode = scopeMode;
        Settings.ScopeProcessName = scopeProcessName;
        RefreshScopeMenu();
        UpdateWakeState();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            SystemEvents.SessionSwitch -= OnSessionSwitchEvent;
            scopeTimer.Stop();
            if (wakeHeld)
            {
                ApplyScreenLockStatus(false);
                wakeHeld = false;
            }
            TrayIcon.Dispose();
            TrayMenu.Dispose();
            enabledIcon.Dispose();
            disabledIcon.Dispose();
        }

        base.Dispose(disposing);
    }

    private static Icon LoadIcon(string name)
    {
        using var stream = System.Windows.Application.GetResourceStream(new Uri($"/Icons/{name}.ico", UriKind.Relative)).Stream;
        return new Icon(stream);
    }

    private static void ApplyScreenLockStatus(bool isEnabled)
    {
        if(isEnabled)
        {
            WinApi.SetThreadExecutionState(WinApi.EXECUTION_STATE.ES_CONTINUOUS | WinApi.EXECUTION_STATE.ES_DISPLAY_REQUIRED);
        }
        else
        {
            WinApi.SetThreadExecutionState(WinApi.EXECUTION_STATE.ES_CONTINUOUS);
        }
    }

    private void UpdateWakeState()
    {
        var shouldHold = screenLockEnabled && TargetProcess.ConditionIsMet(scopeMode, scopeProcessName);
        if (shouldHold != wakeHeld)
        {
            ApplyScreenLockStatus(shouldHold);
            wakeHeld = shouldHold;
        }
        TrayIcon.Icon = wakeHeld ? enabledIcon : disabledIcon;
        TrayIcon.Text = BuildTrayText();
    }

    private string BuildTrayText()
    {
        string text;
        if (!screenLockEnabled)
        {
            text = "CoffeeBean — off";
        }
        else if (scopeMode == ScopeMode.Always)
        {
            text = "CoffeeBean — keeping awake";
        }
        else if (string.IsNullOrWhiteSpace(scopeProcessName))
        {
            text = "CoffeeBean — no app chosen";
        }
        else if (wakeHeld)
        {
            text = $"CoffeeBean — awake while {scopeProcessName}";
        }
        else
        {
            text = scopeMode == ScopeMode.WhenForeground
                ? $"CoffeeBean — waiting for {scopeProcessName} in front"
                : $"CoffeeBean — waiting for {scopeProcessName}";
        }

        return text.Length <= 63 ? text : text.Substring(0, 63);
    }

    private void RefreshScopeMenu()
    {
        ScopeAlwaysMenuItem.Checked = scopeMode == ScopeMode.Always;
        ScopeRunningMenuItem.Checked = scopeMode == ScopeMode.WhenRunning;
        ScopeForegroundMenuItem.Checked = scopeMode == ScopeMode.WhenForeground;
        ScopeTargetMenuItem.Text = string.IsNullOrWhiteSpace(scopeProcessName)
            ? "App: none chosen"
            : $"App: {scopeProcessName}";
    }

    private bool TryChooseApp(out string processName)
    {
        using var dialog = new ProcessPickerDialog(scopeProcessName);
        if (dialog.ShowDialog() == DialogResult.OK &&
            !string.IsNullOrWhiteSpace(dialog.SelectedProcessName))
        {
            processName = dialog.SelectedProcessName;
            return true;
        }
        processName = null;
        return false;
    }

    private void SetScopedMode(ScopeMode mode)
    {
        var target = scopeProcessName;
        if (string.IsNullOrWhiteSpace(target) && !TryChooseApp(out target))
        {
            return;
        }
        SetScope(mode, target);
    }

    private void ScopeAlways_OnClick(object sender, EventArgs e)
    {
        SetScope(ScopeMode.Always, scopeProcessName);
    }

    private void ScopeRunning_OnClick(object sender, EventArgs e)
    {
        SetScopedMode(ScopeMode.WhenRunning);
    }

    private void ScopeForeground_OnClick(object sender, EventArgs e)
    {
        SetScopedMode(ScopeMode.WhenForeground);
    }

    private void ScopeChooseApp_OnClick(object sender, EventArgs e)
    {
        if (!TryChooseApp(out var target))
        {
            return;
        }
        SetScope(scopeMode == ScopeMode.Always ? ScopeMode.WhenRunning : scopeMode, target);
    }

    private void OnSessionSwitchEvent(object sender, SessionSwitchEventArgs e)
    {
        switch (e.Reason)
        {
            case SessionSwitchReason.SessionLock:
                screenLockStatusBeforeSessionWasLocked = IsScreenLockEnabled;
                IsScreenLockEnabled = false;
                break;
            case SessionSwitchReason.SessionUnlock:
                IsScreenLockEnabled = screenLockStatusBeforeSessionWasLocked;
                break;
        }
    }

    private void TrayIcon_MouseClick(object sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            IsScreenLockEnabled = !IsScreenLockEnabled;
        }
    }

    private void About_OnClick(object sender, EventArgs e)
    {
        _ = Process.Start("explorer", githubUrl);
    }

    private void Close_OnClick(object sender, EventArgs e)
    {
        System.Windows.Application.Current.Shutdown();
    }

    private void LaunchAtStartupMenuItem_CheckedChanged(object sender, EventArgs e)
    {
        if (LaunchAtStartupMenuItem.Checked)
        {
            AutoStartup.Enable();
        }
        else
        {
            AutoStartup.Disable();
        }
    }

    private void LaunchEnabledMenuItem_CheckedChanged(object sender, EventArgs e)
    {
        Settings.RunEnabled = LaunchEnabledMenuItem.Checked;
    }

    private void EnabledMenuItem_CheckedChanged(object sender, EventArgs e)
    {
        IsScreenLockEnabled = EnabledMenuItem.Checked;
    }
}
