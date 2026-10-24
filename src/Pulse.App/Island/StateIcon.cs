using Avalonia.Controls;
using Path = Avalonia.Controls.Shapes.Path;
using Avalonia.Media;
using Pulse.Core.Activities;

namespace Pulse.App.Island;

/// <summary>A 16×16 stroked icon for an activity state, in the state's color.</summary>
internal sealed class StateIcon : Viewbox
{
    private static readonly Dictionary<ActivityState, Geometry> Geometries =
        Enum.GetValues<ActivityState>().ToDictionary(s => s, s => Geometry.Parse(ActivityVisuals.IconPath(s)));

    private readonly Path _path = new()
    {
        StrokeThickness = 2.4,
        StrokeLineCap = PenLineCap.Round,
        StrokeJoin = PenLineJoin.Round,
    };

    public StateIcon()
    {
        Width = 16;
        Height = 16;
        Stretch = Stretch.Uniform;
        Child = new Canvas { Width = 24, Height = 24, Children = { _path } };
    }

    public void Show(ActivityState state)
    {
        _path.Data = Geometries[state];
        _path.Stroke = new SolidColorBrush(ActivityVisuals.Glow(state));
    }
}
