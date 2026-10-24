using Avalonia;
using Avalonia.Controls;

namespace Pulse.Spike.Platform;

/// <summary>The OS-specific parts of the island: window flags, notch detection and click-through.</summary>
internal interface IIslandPlatform : IDisposable
{
    /// <summary>How clicks outside the pill reach the windows behind it, for the diagnostics line.</summary>
    string ClickThroughMode { get; }

    /// <summary>Cursor position in window coordinates, or null when the cursor has left the window.</summary>
    event Action<Point?>? PointerMoved;

    /// <summary>The primary screen's notch, or null when it has none.</summary>
    NotchInfo? DetectNotch();

    /// <summary>Called once the window is on screen.</summary>
    void OnOpened();

    void SetPointerInside(bool inside);

    void OnAnimationStarted();

    void OnSettled(Rect pillRect, CornerRadius corners);
}

internal static class IslandPlatform
{
    public static IIslandPlatform Create(Window window, SpikeOptions options)
    {
        if (OperatingSystem.IsWindows())
            return new WindowsIslandPlatform(window);

        if (OperatingSystem.IsMacOS())
            return new MacIslandPlatform(window, options.MacNonActivating);

        return new NullIslandPlatform(window);
    }
}
