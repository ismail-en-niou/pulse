using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Pulse.Core.Activities;

namespace Pulse.App.Island;

/// <summary>One activity in the expanded list: icon, title, subtitle and a progress bar while it runs.</summary>
internal sealed class ActivityRow : Grid
{
    public static readonly TimeSpan FadeDuration = TimeSpan.FromMilliseconds(250);

    private static readonly IBrush SubtitleBrush = new SolidColorBrush(Color.FromArgb(0x8C, 0xFF, 0xFF, 0xFF));
    private static readonly IBrush TrailingBrush = new SolidColorBrush(Color.FromArgb(0xB3, 0xFF, 0xFF, 0xFF));
    private static readonly IBrush TrackBrush = new SolidColorBrush(Color.FromArgb(0x26, 0xFF, 0xFF, 0xFF));

    private readonly StateIcon _icon = new() { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 10, 0) };
    private readonly TextBlock _title = new()
    {
        FontSize = 13,
        FontWeight = FontWeight.SemiBold,
        Foreground = Brushes.White,
        TextTrimming = TextTrimming.CharacterEllipsis,
    };
    private readonly TextBlock _subtitle = new() { FontSize = 11, Foreground = SubtitleBrush, TextTrimming = TextTrimming.CharacterEllipsis };
    private readonly TextBlock _trailing = new()
    {
        FontSize = 12,
        Foreground = TrailingBrush,
        VerticalAlignment = VerticalAlignment.Center,
        Margin = new Thickness(10, 0, 0, 0),
    };
    private readonly Grid _bar = new() { Height = 3, Margin = new Thickness(0, 4, 0, 0) };
    private readonly Border _barFill = new() { CornerRadius = new CornerRadius(1.5) };

    public ActivityRow(Activity activity)
    {
        Id = activity.Id;
        Height = IslandLayout.RowHeight;
        ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto");
        Opacity = 0; // fades in once attached
        Transitions = [new DoubleTransition { Property = OpacityProperty, Duration = FadeDuration }];

        _bar.Children.Add(new Border { Background = TrackBrush, CornerRadius = new CornerRadius(1.5), [ColumnSpanProperty] = 2 });
        _bar.Children.Add(_barFill);

        var text = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Children = { _title, _subtitle, _bar } };
        SetColumn(_icon, 0);
        SetColumn(text, 1);
        SetColumn(_trailing, 2);
        Children.Add(_icon);
        Children.Add(text);
        Children.Add(_trailing);

        Update(activity);
    }

    public string Id { get; }

    /// <summary>The activity left the list; the row is fading out and goes away when the fade ends.</summary>
    public bool IsFading { get; set; }

    public void Update(Activity activity)
    {
        _icon.Show(activity.State);
        _title.Text = activity.Title;
        _subtitle.Text = activity.Subtitle;
        _subtitle.IsVisible = activity.Subtitle is not null;

        var progress = ActivityVisuals.ProgressText(activity);
        _trailing.Text = progress;
        _bar.IsVisible = progress is not null;
        if (activity.Progress is { } p)
        {
            // Star columns split the track, so the fill needs no measuring.
            _bar.ColumnDefinitions = new ColumnDefinitions($"{p}*,{100 - p}*");
            _barFill.Background = new SolidColorBrush(ActivityVisuals.Glow(activity.State));
        }
    }
}
