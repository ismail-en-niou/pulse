using Avalonia.Media;
using Pulse.App.Island;
using Pulse.Core.Activities;

namespace Pulse.Tests.App;

public class ActivityVisualsTests
{
    private static Activity Make(ActivityState state, int? progress = null, string title = "Training model") => new()
    {
        Id = "a1",
        Source = "cli",
        Title = title,
        State = state,
        Progress = progress,
    };

    [Theory]
    [InlineData(ActivityState.Running, "#0A84FF")]
    [InlineData(ActivityState.NeedsInput, "#FF9F0A")]
    [InlineData(ActivityState.Success, "#30D158")]
    [InlineData(ActivityState.Failure, "#FF453A")]
    public void Glow_is_blue_orange_green_and_red(ActivityState state, string expected) =>
        Assert.Equal(Color.Parse(expected), ActivityVisuals.Glow(state));

    [Fact]
    public void Every_state_has_its_own_color_and_icon()
    {
        var states = Enum.GetValues<ActivityState>();

        Assert.Equal(states.Length, states.Select(ActivityVisuals.Glow).Distinct().Count());
        Assert.Equal(states.Length, states.Select(ActivityVisuals.IconPath).Distinct().Count());
    }

    [Fact]
    public void Progress_shows_only_while_the_activity_runs()
    {
        Assert.Equal("40%", ActivityVisuals.ProgressText(Make(ActivityState.Running, 40)));
        Assert.Equal("0%", ActivityVisuals.ProgressText(Make(ActivityState.NeedsInput, 0)));
        Assert.Null(ActivityVisuals.ProgressText(Make(ActivityState.Running)));
        Assert.Null(ActivityVisuals.ProgressText(Make(ActivityState.Success, 100)));
        Assert.Null(ActivityVisuals.ProgressText(Make(ActivityState.Failure, 40)));
    }

    [Fact]
    public void Beside_the_notch_progress_wins_over_the_title()
    {
        Assert.Equal("40%", ActivityVisuals.NotchTrailing(Make(ActivityState.Running, 40)));
        Assert.Equal("Training model", ActivityVisuals.NotchTrailing(Make(ActivityState.Failure, 40)));
    }

    [Theory]
    [InlineData(0, 0, 0)]
    [InlineData(3, 3, 0)]
    [InlineData(4, 4, 0)]
    [InlineData(5, 3, 2)]
    [InlineData(9, 3, 6)]
    public void Past_the_maximum_the_last_row_becomes_a_more_line(int count, int rows, int hidden)
    {
        var activities = Enumerable.Range(0, count).Select(i => Make(ActivityState.Running, title: $"job {i}") with { Id = $"a{i}" }).ToList();

        var split = ActivityVisuals.SplitRows(activities);

        Assert.Equal(rows, split.Rows.Count);
        Assert.Equal(hidden, split.Hidden);
        Assert.Equal(activities.Take(rows), split.Rows);
    }
}
