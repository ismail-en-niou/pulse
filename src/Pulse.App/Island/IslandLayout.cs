using Avalonia;
using Pulse.Core.Platform;

namespace Pulse.App.Island;

/// <summary>What the island has to show, as far as its size is concerned.</summary>
/// <param name="HasActivity">The collapsed pill widens to show the top activity.</param>
/// <param name="Rows">Rows in the expanded list, including ones still fading out.</param>
/// <param name="HasMore">A "+N more" line sits under the rows.</param>
internal readonly record struct IslandContent(bool HasActivity, int Rows, bool HasMore)
{
    public static IslandContent Idle => new(false, 0, false);
}

/// <summary>What the pill looks like at one point of the animation.</summary>
internal readonly record struct PillFrame(double Width, double Height, CornerRadius CornerRadius, double CompactOpacity, double ExpandedOpacity);

/// <summary>
/// Geometry of the pill in DIPs (points on macOS): idle, showing an activity, and expanded with its list.
/// The window keeps one fixed size, big enough for the tallest list plus spring overshoot; only the pill animates.
/// </summary>
internal sealed class IslandLayout
{
    public const int MaxRows = 4;
    public const double RowHeight = 44;
    public const double RowGap = 2;
    public const double MoreHeight = 20;
    public const double ExpandedWidth = 420;
    public const double ContentWidth = ExpandedWidth - 2 * 18;

    /// <summary>On notched Macs, the space left and right of the notch that holds the icon and the short text.</summary>
    public const double EarWidth = 76;

    private const double ExpandedRadius = 28;
    private const double NotchRadius = 12;
    private const double BottomPadding = 12;
    private const double OvershootRoom = 20;
    private const double WindowPadding = 8;

    private IslandLayout(NotchInfo? notch)
    {
        Notch = notch;
        if (notch is { } n)
        {
            // Flush with the top of the screen. Idle, slightly wider than the notch so placement is visible.
            NotchWidth = Math.Round(n.Width);
            var height = Math.Round(n.Height);
            Idle = new Size(Math.Max(200, NotchWidth + 24), height);
            Active = new Size(NotchWidth + 2 * EarWidth, height);
            TopOffset = 0;
            ContentTop = height + 6;
        }
        else
        {
            Idle = new Size(160, 30);
            Active = new Size(300, 36);
            TopOffset = 8;
            ContentTop = 12;
        }

        var tallest = Math.Max(ExpandedHeight(new IslandContent(true, MaxRows, false)), ExpandedHeight(new IslandContent(true, MaxRows - 1, true)));
        WindowSize = new Size(
            2 * Math.Ceiling((ExpandedWidth + 2 * (OvershootRoom + WindowPadding)) / 2),
            Math.Ceiling(TopOffset + tallest + OvershootRoom + WindowPadding));
    }

    public NotchInfo? Notch { get; }

    /// <summary>Notched Macs get a flat top that blends into the notch; everything else gets a capsule.</summary>
    public bool NotchStyle => Notch is not null;

    /// <summary>Width of the notch, or 0 without one.</summary>
    public double NotchWidth { get; }

    /// <summary>Collapsed with nothing to show.</summary>
    public Size Idle { get; }

    /// <summary>Collapsed, showing the top activity.</summary>
    public Size Active { get; }

    /// <summary>Distance from the top of the window (and screen) to the top of the pill.</summary>
    public double TopOffset { get; }

    /// <summary>Where the expanded list starts inside the pill (below the notch on notched Macs).</summary>
    public double ContentTop { get; }

    public Size WindowSize { get; }

    public static IslandLayout Create(NotchInfo? notch) => new(notch);

    /// <summary>The size the pill moves towards.</summary>
    public Size Target(IslandContent content, bool expanded) =>
        expanded ? new Size(ExpandedWidth, ExpandedHeight(content)) : content.HasActivity ? Active : Idle;

    /// <summary>An empty list still gets one row's height, for the "No activities" line.</summary>
    public double ExpandedHeight(IslandContent content)
    {
        var rows = Math.Clamp(content.Rows, 1, MaxRows);
        return ContentTop + rows * RowHeight + (rows - 1) * RowGap + (content.HasMore ? MoreHeight : 0) + BottomPadding;
    }

    /// <param name="width">Current width from its spring; clamped to fit the window.</param>
    /// <param name="height">Current height from its spring; clamped to fit the window.</param>
    /// <param name="expansion">0 collapsed, 1 expanded; drives corners and which content shows.</param>
    public PillFrame FrameAt(double width, double height, double expansion)
    {
        var w = Math.Clamp(width, 1, WindowSize.Width - 2 * WindowPadding);
        var h = Math.Clamp(height, 1, WindowSize.Height - TopOffset - WindowPadding);
        var e = Math.Clamp(expansion, 0, 1);

        var collapsedRadius = NotchStyle ? NotchRadius : h / 2;
        var radius = Math.Min(Lerp(collapsedRadius, ExpandedRadius, e), h / 2);
        var corners = NotchStyle ? new CornerRadius(0, 0, radius, radius) : new CornerRadius(radius);

        // The compact content leaves early and the list arrives late, so they never overlap.
        var compactOpacity = Math.Clamp(1 - e / 0.35, 0, 1);
        var expandedOpacity = Math.Clamp((e - 0.55) / 0.4, 0, 1);
        return new PillFrame(w, h, corners, compactOpacity, expandedOpacity);
    }

    /// <summary>The pill's rectangle in window coordinates.</summary>
    public IslandRect PillRect(PillFrame frame) =>
        new((WindowSize.Width - frame.Width) / 2, TopOffset, frame.Width, frame.Height);

    private static double Lerp(double from, double to, double t) => from + (to - from) * t;
}
