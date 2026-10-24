using System.Diagnostics;
using Avalonia.Controls;
using Pulse.Core.Diagnostics;
using Pulse.Core.Platform;

namespace Pulse.App.Island;

/// <summary>
/// Hover state machine: the cursor decides the spring target, and the spring drives the pill frame by frame.
/// Frames are requested only while the spring moves, so a settled island does no work.
/// </summary>
internal sealed class IslandController
{
    /// <summary>Extra margin around the pill that still counts as hovering.</summary>
    private const double HoverSlop = 2;

    private readonly TopLevel _topLevel;
    private readonly IIslandPlacement _placement;
    private readonly Action<PillFrame> _apply;
    private readonly Spring _spring = new();

    private IslandLayout _layout;
    private bool _expanded;
    private bool _animating;
    private TimeSpan? _lastFrameTime;
    private int _frames;
    private long _animationStart;

    public IslandController(TopLevel topLevel, IIslandPlacement placement, IslandLayout layout, Action<PillFrame> apply)
    {
        _topLevel = topLevel;
        _placement = placement;
        _layout = layout;
        _apply = apply;
        _apply(CurrentFrame);
    }

    private PillFrame CurrentFrame => _layout.FrameAt(_spring.Value);

    private IslandRect CurrentPillRect => _layout.PillRect(CurrentFrame);

    public void UpdateLayout(IslandLayout layout)
    {
        _layout = layout;
        _apply(CurrentFrame);
        if (!_animating)
            _placement.OnSettled(CurrentPillRect, CurrentFrame.CornerRadius.BottomLeft);
    }

    /// <summary>Cursor position in window coordinates, or null when the cursor has left the window.</summary>
    public void OnPointer(IslandPoint? position)
    {
        var inside = position is { } p && CurrentPillRect.Inflate(HoverSlop).Contains(p);
        _placement.SetPointerInside(inside);
        if (inside == _expanded)
            return;

        _expanded = inside;
        PulseLog.Event(inside ? "hover-enter" : "hover-exit");
        if (inside)
            _spring.Retarget(1, response: 0.42, dampingRatio: 0.68);
        else
            _spring.Retarget(0, response: 0.32, dampingRatio: 0.85);

        StartAnimation();
    }

    private void StartAnimation()
    {
        if (_animating)
            return;

        _animating = true;
        _lastFrameTime = null;
        _frames = 0;
        _animationStart = Stopwatch.GetTimestamp();
        _placement.OnAnimationStarted();
        _topLevel.RequestAnimationFrame(OnFrame);
    }

    private void OnFrame(TimeSpan time)
    {
        var dt = _lastFrameTime is { } last ? (time - last).TotalSeconds : 1.0 / 60;
        _lastFrameTime = time;
        _frames++;

        _spring.Advance(dt);
        _apply(CurrentFrame);

        if (!_spring.IsSettled)
        {
            _topLevel.RequestAnimationFrame(OnFrame);
            return;
        }

        _animating = false;
        var elapsed = Stopwatch.GetElapsedTime(_animationStart);
        PulseLog.Event($"settled {(_expanded ? "expanded" : "collapsed")} in {elapsed.TotalMilliseconds:0} ms, " +
                       $"{_frames} frames ({_frames / elapsed.TotalSeconds:0} fps)");
        _placement.OnSettled(CurrentPillRect, CurrentFrame.CornerRadius.BottomLeft);
    }
}
