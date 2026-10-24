using System.Runtime.Versioning;
using Microsoft.Win32;
using Pulse.Core.Platform;

namespace Pulse.Platform.Windows;

/// <summary>Starts Pulse at login through the current user's Run key. No admin rights needed.</summary>
[SupportedOSPlatform("windows")]
public sealed class WindowsLaunchAtLogin : ILaunchAtLogin
{
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";

    private readonly string _valueName;
    private readonly string? _executablePath;

    /// <param name="valueName">Name of the Run value; tests use their own.</param>
    /// <param name="executablePath">Program to start; defaults to the running executable.</param>
    public WindowsLaunchAtLogin(string valueName = "Pulse", string? executablePath = null)
    {
        _valueName = valueName;
        _executablePath = executablePath ?? Environment.ProcessPath;
    }

    public bool IsSupported => _executablePath is not null;

    public bool IsEnabled
    {
        get
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKey);
            return key?.GetValue(_valueName) is string;
        }
    }

    public void SetEnabled(bool enabled)
    {
        using var key = Registry.CurrentUser.CreateSubKey(RunKey, writable: true);
        if (!enabled)
        {
            key.DeleteValue(_valueName, throwOnMissingValue: false);
            return;
        }

        var path = _executablePath ?? throw new InvalidOperationException("the running executable is unknown");
        key.SetValue(_valueName, $"\"{path}\"", RegistryValueKind.String);
    }
}
