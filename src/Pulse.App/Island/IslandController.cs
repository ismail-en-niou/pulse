using System.Diagnostics;
using Avalonia.Controls;
using Pulse.Core.Diagnostics;
using Pulse.Core.Platform;

namespace Pulse.App.Island;

/// <summary>
/// Hover and content decide the pill's target size; springs on width, height and expansion move it there
/// frame by frame. Frames are requested only while a spring moves, so a settled island does no work.
/// </summary>
internal sealed class IslandController
{
    /// <summary>Extra margin around the pill that still counts as hovering.</summary>
    private const double HoverSlop = 2;

    /// <summary>Settled when within a twentieth of a DIP.</summary>
    private const double SizePrecision = 0.05;

    private static readonly Motion Expand = new(0.42, 0.68);
    private static readonly Motion Collapse = new(0.32, 0.85);

    /// <summary>The content changed size: move without bouncing.</summary>
    private static readonly Motion Reflow = new(0.36, 0.9);

    private readonly TopLevel _topLevel;
    private readonly IIslandPlacement _placement;
    private readonly Action<PillFrame> _apply;
    private readonly Spring _width;
    private readonly Spring _height;
    private readonly Spring _expansion = new();

    private IslandLayout _layout;
    private IslandContent _content = IslandContent.Idle;
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

        var size = layout.Target(_content, expanded: false);
        _width = new Spring(size.Width, SizePrecision);
        _height = new Spring(size.Height, SizePrecision);
        _apply(CurrentFrame);
    }

    private PillFrame CurrentFrame => _layout.FrameAt(_width.Value, _height.Value, _expansion.Value);

    private IslandRect CurrentPillRect => _layout.PillRect(CurrentFrame);

    private bool IsSettled => _width.IsSettled && _height.IsSettled && _expansion.IsSettled;

    /// <summary>New screen or scale: jump to the new geometry without animating.</summary>
    public void UpdateLayout(IslandLayout layout)
    {
        _layout = layout;
        var size = layout.Target(_content, _expanded);
        _width.Snap(size.Width);
        _height.Snap(size.Height);
        _expansion.Snap(_expanded ? 1 : 0);
        _apply(CurrentFrame);

        // A running animation finds the springs settled on its next frame and reports there.
        if (!_animating)
            Settled();
    }

    public void SetContent(IslandContent content)
    {
        if (content == _content)
            return;

        _content = content;
        MoveTo(Reflow, retargetExpansion: false);
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
        MoveTo(inside ? Expand : Collapse, retargetExpansion: true);
    }

    private void MoveTo(Motion motion, bool retargetExpansion)
    {
        var size = _layout.Target(_content, _expanded);
        _width.Retarget(size.Width, motion.Response, motion.DampingRatio);
        _height.Retarget(size.Height, motion.Response, motion.DampingRatio);
        if (retargetExpansion)
            _expansion.Retarget(_expanded ? 1 : 0, motion.Response, motion.DampingRatio);

        if (!IsSettled)
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

        _width.Advance(dt);
        _height.Advance(dt);
        _expansion.Advance(dt);
        _apply(CurrentFrame);

        if (!IsSettled)
        {
            _topLevel.RequestAnimationFrame(OnFrame);
            return;
        }

        _animating = false;
        var elapsed = Stopwatch.GetElapsedTime(_animationStart);
        PulseLog.Event($"settled {(_expanded ? "expanded" : "collapsed")} in {elapsed.TotalMilliseconds:0} ms, " +
                       $"{_frames} frames ({_frames / elapsed.TotalSeconds:0} fps)");
        Settled();
    }

    private void Settled() => _placement.OnSettled(CurrentPillRect, CurrentFrame.CornerRadius.BottomLeft);

    private readonly record struct Motion(double Response, double DampingRatio);
}
