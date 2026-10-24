using Avalonia;
using Avalonia.Controls;

namespace Pulse.Spike.Platform;

/// <summary>The OS-specific parts of the island: window flags, notch detection and click-through.</summary>
internal interface IIslandPlatform : IDisposable
{
    /// <summary>How clicks outside the pill reach the windows behind it, for the diagnostics line.</summary>
    string ClickThroughMode { get; }

    /// <summary>
    /// True when the platform reports the cursor through <see cref="PointerMoved"/> itself,
    /// instead of the window relying on Avalonia pointer events.
    /// </summary>
    bool TracksPointer { get; }

    /// <summary>Cursor position in window coordinates.</summary>
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
    public static IIslandPlatform Create(Window window, SpikeOptions options) => new NullIslandPlatform(window);
}
