using Avalonia;
using Pulse.Core.Platform;

namespace Pulse.App.Island;

/// <summary>What the pill looks like at one point of the animation.</summary>
internal readonly record struct PillFrame(double Width, double Height, CornerRadius CornerRadius, double ContentOpacity);

/// <summary>
/// Collapsed and expanded geometry of the pill, in DIPs (points on macOS). The window keeps one fixed size
/// big enough for the expanded pill plus spring overshoot; only the pill inside it animates.
/// </summary>
internal sealed class IslandLayout
{
    /// <summary>Largest spring progress we leave room for (1 = fully expanded).</summary>
    private const double MaxProgress = 1.1;
    private const double WindowPadding = 8;

    private readonly double _collapsedRadius;
    private readonly double _expandedRadius;

    private IslandLayout(NotchInfo? notch)
    {
        Notch = notch;
        Expanded = new Size(420, 140);
        _expandedRadius = 32;

        if (notch is { } n)
        {
            // Flush with the top of the screen, slightly wider than the notch so placement is visible.
            Collapsed = new Size(Math.Max(200, Math.Round(n.Width) + 24), Math.Round(n.Height));
            _collapsedRadius = 10;
            TopOffset = 0;
            ContentTop = Collapsed.Height + 8;
        }
        else
        {
            Collapsed = new Size(200, 36);
            _collapsedRadius = Collapsed.Height / 2;
            TopOffset = 8;
            ContentTop = 18;
        }

        var maxWidth = Lerp(Collapsed.Width, Expanded.Width, MaxProgress);
        var maxHeight = Lerp(Collapsed.Height, Expanded.Height, MaxProgress);
        WindowSize = new Size(
            2 * Math.Ceiling((maxWidth + 2 * WindowPadding) / 2),
            Math.Ceiling(TopOffset + maxHeight + WindowPadding));
    }

    public NotchInfo? Notch { get; }

    public Size Collapsed { get; }

    public Size Expanded { get; }

    /// <summary>Distance from the top of the window (and screen) to the top of the pill.</summary>
    public double TopOffset { get; }

    /// <summary>Where the expanded content starts inside the pill (below the notch on notched Macs).</summary>
    public double ContentTop { get; }

    public Size WindowSize { get; }

    /// <summary>Notched Macs get a flat top that blends into the notch; everything else gets a capsule.</summary>
    public bool NotchStyle => Notch is not null;

    public static IslandLayout Create(NotchInfo? notch) => new(notch);

    public PillFrame FrameAt(double progress)
    {
        var p = Math.Clamp(progress, 0, MaxProgress);
        var width = Lerp(Collapsed.Width, Expanded.Width, p);
        var height = Lerp(Collapsed.Height, Expanded.Height, p);
        var radius = Math.Min(Lerp(_collapsedRadius, _expandedRadius, Math.Min(p, 1)), height / 2);
        var corners = NotchStyle ? new CornerRadius(0, 0, radius, radius) : new CornerRadius(radius);
        var contentOpacity = Math.Clamp((p - 0.55) / 0.4, 0, 1);
        return new PillFrame(width, height, corners, contentOpacity);
    }

    /// <summary>The pill's rectangle in window coordinates.</summary>
    public IslandRect PillRect(PillFrame frame) =>
        new((WindowSize.Width - frame.Width) / 2, TopOffset, frame.Width, frame.Height);

    private static double Lerp(double from, double to, double t) => from + (to - from) * t;
}
