using Avalonia;
using Avalonia.Controls;

namespace Pulse.Spike;

public partial class IslandWindow : Window
{
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
        Topmost = true;
        ShowInTaskbar = false;

        _layout = IslandLayout.Create(notch: null);
        ApplyLayout();
        ApplyFrame(_layout.FrameAt(0));

        Opened += (_, _) => Relayout("opened");
        ScalingChanged += (_, _) => Relayout("scaling-changed");
        Screens.Changed += (_, _) => Relayout("screens-changed");
    }

    private void Relayout(string reason)
    {
        _layout = IslandLayout.Create(notch: null);
        ApplyLayout();
        ApplyFrame(_layout.FrameAt(0));
        Diagnostics.Text = DescribeEnvironment();
        Log.Event($"placement ({reason}): window {_layout.WindowSize.Width}x{_layout.WindowSize.Height} at {Position}, " +
                  $"scale {RenderScaling:0.##}, transparency {ActualTransparencyLevel}");
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
        return $"{os} · scale {RenderScaling:0.##} · {notch} · transparency {ActualTransparencyLevel}";
    }
}
