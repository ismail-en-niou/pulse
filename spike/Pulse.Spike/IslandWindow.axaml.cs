using Avalonia;
using Avalonia.Controls;
using Pulse.Spike.Platform;

namespace Pulse.Spike;

public partial class IslandWindow : Window
{
    private readonly IIslandPlatform _platform;
    private readonly IslandController _controller;
    private IslandLayout _layout;

    /// <summary>Used by the XAML loader and previewer.</summary>
    public IslandWindow() : this(SpikeOptions.Default)
    {
    }

    internal IslandWindow(SpikeOptions options)
    {
        InitializeComponent();
        TransparencyLevelHint = [WindowTransparencyLevel.Transparent];
        DebugBounds.IsVisible = options.DebugBounds;

        _platform = IslandPlatform.Create(this, options);
        _layout = IslandLayout.Create(_platform.DetectNotch());
        // Size and position before Show(), so the window never appears anywhere else first.
        ApplyLayout();
        _controller = new IslandController(this, _platform, _layout, ApplyFrame);

        if (_platform.TracksPointer)
        {
            _platform.PointerMoved += _controller.OnPointer;
        }
        else
        {
            PointerMoved += (_, e) => _controller.OnPointer(e.GetPosition(this));
            PointerExited += (_, _) => _controller.OnPointer(null);
        }

        Opened += (_, _) =>
        {
            _platform.OnOpened();
            Relayout("opened");
        };
        Closed += (_, _) => _platform.Dispose();
        ScalingChanged += (_, _) => Relayout("scaling-changed");
        Screens.Changed += (_, _) => Relayout("screens-changed");
    }

    private void Relayout(string reason)
    {
        _layout = IslandLayout.Create(_platform.DetectNotch());
        ApplyLayout();
        _controller.UpdateLayout(_layout);
        Diagnostics.Text = DescribeEnvironment();
        Log.Event($"placement ({reason}): window {_layout.WindowSize.Width}x{_layout.WindowSize.Height} at {Position}, " +
                  $"pill {_layout.Collapsed.Width}x{_layout.Collapsed.Height}, scale {RenderScaling:0.##}, " +
                  $"transparency {ActualTransparencyLevel}");
    }

    /// <summary>Sizes the window and centers it at the top of the primary screen.</summary>
    private void ApplyLayout()
    {
        Width = _layout.WindowSize.Width;
        Height = _layout.WindowSize.Height;
        ExpandedContent.Margin = new Thickness(0, _layout.ContentTop, 0, 0);

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
        ExpandedContent.Opacity = frame.ContentOpacity;
    }

    private string DescribeEnvironment()
    {
        var os = OperatingSystem.IsWindows() ? "Windows" : OperatingSystem.IsMacOS() ? "macOS" : "other OS";
        var notch = _layout.Notch is { } n ? $"notch {n.Width:0}×{n.Height:0}" : "no notch";
        return $"{os} · scale {RenderScaling:0.##} · {notch} · transparency {ActualTransparencyLevel} · " +
               _platform.ClickThroughMode;
    }
}
