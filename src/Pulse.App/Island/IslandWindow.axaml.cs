using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Pulse.Core.Activities;
using Pulse.Core.Diagnostics;
using Pulse.Core.Platform;

namespace Pulse.App.Island;

public partial class IslandWindow : Window
{
    private static readonly TimeSpan CompactFade = TimeSpan.FromMilliseconds(120);

    private readonly ActivityService? _activities;
    private readonly IIslandPlacement _placement;
    private readonly IslandController _controller;
    private readonly Dictionary<string, ActivityRow> _rows = new(StringComparer.Ordinal);
    private IslandLayout _layout;
    private long _shownVersion = -1;
    private Activity? _top;

    // The collapsed content crossfades when the top activity changes.
    private string? _compactId;
    private Activity? _pendingTop;
    private bool _compactFading;

    /// <summary>Used by the XAML loader and previewer.</summary>
    public IslandWindow() : this(AppOptions.Default, null)
    {
    }

    internal IslandWindow(AppOptions options, ActivityService? activities)
    {
        InitializeComponent();
        TransparencyLevelHint = [WindowTransparencyLevel.Transparent];
        DebugBounds.IsVisible = options.DebugBounds;
        ExpandedContent.Width = IslandLayout.ContentWidth;

        _activities = activities;
        _placement = PlatformFactory.CreatePlacement(this, options);
        _layout = IslandLayout.Create(_placement.DetectNotch());
        // Size and position before Show(), so the window never appears anywhere else first.
        ApplyLayout();
        _controller = new IslandController(this, _placement, _layout, ApplyFrame);
        _placement.PointerMoved += _controller.OnPointer;

        Opened += (_, _) =>
        {
            _placement.OnOpened();
            Relayout("opened");
        };
        Closed += (_, _) =>
        {
            if (_activities is not null)
                _activities.Changed -= OnActivitiesChanged;
            _placement.Dispose();
        };
        ScalingChanged += (_, _) => Relayout("scaling-changed");
        Screens.Changed += (_, _) => Relayout("screens-changed");

        if (_activities is not null)
        {
            _activities.Changed += OnActivitiesChanged;
            ShowActivities(_activities.Snapshot());
        }
    }

    /// <summary>Raised on whichever thread changed the activities; snapshots that arrive late are dropped.</summary>
    private void OnActivitiesChanged(object? sender, ActivitiesChangedEventArgs e) =>
        Dispatcher.UIThread.Post(() =>
        {
            if (e.Version <= _shownVersion)
                return;

            _shownVersion = e.Version;
            ShowActivities(e.Activities);
        });

    private void ShowActivities(IReadOnlyList<Activity> activities)
    {
        _top = activities.Count > 0 ? activities[0] : null;
        ShowCompact(_top);
        ShowGlow(_top);
        ShowRows(activities);
    }

    private void ShowCompact(Activity? top)
    {
        if (top?.Id == _compactId && !_compactFading)
        {
            FillCompact(top);
            return;
        }

        // Fade the old activity out, then the new one in. Changes during the fade only replace what comes next.
        _pendingTop = top;
        if (_compactFading)
            return;

        _compactFading = true;
        Compact.Opacity = 0;
        DispatcherTimer.RunOnce(() =>
        {
            _compactFading = false;
            _compactId = _pendingTop?.Id;
            FillCompact(_pendingTop);
        }, CompactFade);
    }

    private void FillCompact(Activity? activity)
    {
        Compact.Opacity = activity is null ? 0 : 1;
        if (activity is null)
            return;

        CompactIcon.Show(activity.State);
        CompactTitle.Text = activity.Title;
        CompactTrailing.Text = _layout.NotchStyle ? ActivityVisuals.NotchTrailing(activity) : ActivityVisuals.ProgressText(activity);
    }

    private void ShowGlow(Activity? top)
    {
        var color = top is null ? Colors.Transparent : ActivityVisuals.Glow(top.State);
        Pill.BorderBrush = new SolidColorBrush(Color.FromArgb(top is null ? (byte)0 : (byte)0x99, color.R, color.G, color.B));
        Pill.BoxShadow = new BoxShadows(new BoxShadow
        {
            IsInset = true,
            Blur = 16,
            Color = Color.FromArgb(top is null ? (byte)0 : (byte)0x66, color.R, color.G, color.B),
        });
    }

    /// <summary>
    /// Updates rows in place, adds new ones fading in, and fades out the ones that left. A fading row keeps its
    /// place until its fade ends, then the pill shrinks.
    /// </summary>
    private void ShowRows(IReadOnlyList<Activity> activities)
    {
        var (visible, hidden) = ActivityVisuals.SplitRows(activities);
        var visibleIds = visible.Select(a => a.Id).ToHashSet(StringComparer.Ordinal);

        foreach (var row in _rows.Values.Where(r => !r.IsFading && !visibleIds.Contains(r.Id)).ToList())
            FadeOut(row);

        var ordered = new List<Control>(visible.Count);
        foreach (var activity in visible)
        {
            if (!_rows.TryGetValue(activity.Id, out var row))
            {
                row = new ActivityRow(activity);
                _rows.Add(activity.Id, row);
                Dispatcher.UIThread.Post(() => row.Opacity = row.IsFading ? 0 : 1, DispatcherPriority.Background);
            }
            else
            {
                if (row.IsFading)
                {
                    // Back in view before its fade ended, e.g. when a "+N more" row moved up.
                    row.IsFading = false;
                    row.Opacity = 1;
                }

                row.Update(activity);
            }

            ordered.Add(row);
        }

        var children = Rows.Children;
        foreach (var fading in children.OfType<ActivityRow>().Where(r => r.IsFading).ToList())
            ordered.Insert(Math.Min(children.IndexOf(fading), ordered.Count), fading);

        for (var i = 0; i < ordered.Count; i++)
        {
            var current = children.IndexOf(ordered[i]);
            if (current < 0)
                children.Insert(i, ordered[i]);
            else if (current != i)
                children.Move(current, i);
        }

        MoreLabel.Text = $"+{hidden} more";
        MoreLabel.IsVisible = hidden > 0;
        UpdateContentSize();
    }

    private void FadeOut(ActivityRow row)
    {
        row.IsFading = true;
        row.Opacity = 0;
        DispatcherTimer.RunOnce(() =>
        {
            if (!row.IsFading)
                return;

            Rows.Children.Remove(row);
            _rows.Remove(row.Id);
            UpdateContentSize();
        }, ActivityRow.FadeDuration);
    }

    private void UpdateContentSize()
    {
        EmptyState.IsVisible = Rows.Children.Count == 0;
        _controller.SetContent(new IslandContent(_top is not null, Rows.Children.Count, MoreLabel.IsVisible));
    }

    private void Relayout(string reason)
    {
        _layout = IslandLayout.Create(_placement.DetectNotch());
        ApplyLayout();
        _controller.UpdateLayout(_layout);
        if (!_compactFading)
            FillCompact(_top); // the notch decides which short text shows
        PulseLog.Event($"placement ({reason}): window {_layout.WindowSize.Width}x{_layout.WindowSize.Height} at {Position}, " +
                       $"scale {RenderScaling:0.##}, {(_layout.Notch is { } n ? $"notch {n.Width:0}x{n.Height:0}" : "no notch")}, " +
                       $"transparency {ActualTransparencyLevel}, {_placement.ClickThroughMode}");
    }

    /// <summary>Sizes the window, lays out the collapsed content and centers the window at the top of the primary screen.</summary>
    private void ApplyLayout()
    {
        Width = _layout.WindowSize.Width;
        Height = _layout.WindowSize.Height;
        ExpandedContent.Margin = new Thickness(0, _layout.ContentTop, 0, 0);

        CompactLayer.Width = _layout.Active.Width;
        CompactLayer.Height = _layout.Active.Height;
        if (_layout.NotchStyle)
        {
            // Icon in the left ear, short text in the right one, nothing under the notch.
            Compact.ColumnDefinitions = new ColumnDefinitions($"{IslandLayout.EarWidth},{_layout.NotchWidth},{IslandLayout.EarWidth}");
            CompactIcon.HorizontalAlignment = HorizontalAlignment.Center;
            CompactIcon.Margin = default;
            CompactTitle.IsVisible = false;
            CompactTrailing.HorizontalAlignment = HorizontalAlignment.Center;
            CompactTrailing.MaxWidth = IslandLayout.EarWidth - 12;
            CompactTrailing.Margin = default;
        }
        else
        {
            Compact.ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto");
            CompactIcon.HorizontalAlignment = HorizontalAlignment.Left;
            CompactIcon.Margin = new Thickness(12, 0, 0, 0);
            CompactTitle.IsVisible = true;
            CompactTrailing.HorizontalAlignment = HorizontalAlignment.Right;
            CompactTrailing.MaxWidth = double.PositiveInfinity;
            CompactTrailing.Margin = new Thickness(0, 0, 14, 0);
        }

        // Bounds are physical pixels on Windows and points (Scaling = 1) on macOS, so one formula fits both.
        if (Screens.Primary is { } screen)
        {
            var bounds = screen.Bounds;
            var widthOnScreen = (int)Math.Round(_layout.WindowSize.Width * screen.Scaling);
            Position = new PixelPoint(bounds.X + (bounds.Width - widthOnScreen) / 2, bounds.Y);
        }
    }

    private void ApplyFrame(PillFrame frame)
    {
        Pill.Width = frame.Width;
        Pill.Height = frame.Height;
        Pill.CornerRadius = frame.CornerRadius;
        CompactLayer.Opacity = frame.CompactOpacity;
        ExpandedContent.Opacity = frame.ExpandedOpacity;
    }
}
