using Pulse.Core.Activities;
using Pulse.Core.Api;

namespace Pulse.Tests.Core;

public class ActivityRulesTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 24, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Later = Now.AddSeconds(5);

    private static Activity Create(CreateActivityRequest request)
    {
        var result = ActivityRules.Create(request, "a1", Now);
        Assert.True(result.IsSuccess, result.Message);
        return result.Activity!;
    }

    private static Activity Running(int? progress = null) =>
        Create(new CreateActivityRequest("Training model", Subtitle: "epoch 1", Progress: progress));

    [Fact]
    public void Create_defaults_to_a_running_activity_from_the_api()
    {
        var activity = Create(new CreateActivityRequest("  Training model  "));

        Assert.Equal("a1", activity.Id);
        Assert.Equal("Training model", activity.Title);
        Assert.Equal(ActivityState.Running, activity.State);
        Assert.Equal(ActivityRules.DefaultSource, activity.Source);
        Assert.Equal(0, activity.Priority);
        Assert.Null(activity.Progress);
        Assert.Equal(Now, activity.CreatedAt);
        Assert.Equal(Now, activity.UpdatedAt);
        Assert.Null(activity.FinishedAt);
    }

    [Theory]
    [InlineData(ActivityState.Success)]
    [InlineData(ActivityState.Failure)]
    [InlineData(ActivityState.Info)]
    public void Create_in_a_finished_state_finishes_immediately(ActivityState state)
    {
        var activity = Create(new CreateActivityRequest("Backup finished", State: state));

        Assert.True(activity.IsFinished);
        Assert.Equal(Now, activity.FinishedAt);
    }

    [Fact]
    public void Create_can_start_by_asking_for_input()
    {
        var activity = Create(new CreateActivityRequest("Approve deploy?", State: ActivityState.NeedsInput));

        Assert.Equal(ActivityState.NeedsInput, activity.State);
        Assert.False(activity.IsFinished);
    }

    [Fact]
    public void Create_lowercases_the_source_and_drops_an_empty_subtitle()
    {
        var activity = Create(new CreateActivityRequest("Build", Subtitle: "  ", Source: " CLI "));

        Assert.Equal("cli", activity.Source);
        Assert.Null(activity.Subtitle);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_requires_a_title(string? title) =>
        AssertInvalid(ActivityRules.Create(new CreateActivityRequest(title), "a1", Now));

    [Fact]
    public void Create_limits_title_and_subtitle_length()
    {
        Assert.True(ActivityRules.Create(new CreateActivityRequest(new string('t', 120)), "a1", Now).IsSuccess);
        AssertInvalid(ActivityRules.Create(new CreateActivityRequest(new string('t', 121)), "a1", Now));
        AssertInvalid(ActivityRules.Create(new CreateActivityRequest("ok", Subtitle: new string('s', 241)), "a1", Now));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public void Create_rejects_progress_outside_0_to_100(int progress) =>
        AssertInvalid(ActivityRules.Create(new CreateActivityRequest("ok", Progress: progress), "a1", Now));

    [Theory]
    [InlineData(-101)]
    [InlineData(101)]
    public void Create_rejects_priority_outside_the_range(int priority) =>
        AssertInvalid(ActivityRules.Create(new CreateActivityRequest("ok", Priority: priority), "a1", Now));

    [Theory]
    [InlineData("my source")]
    [InlineData("a/b")]
    [InlineData("ünïcode")]
    public void Create_rejects_unusual_source_names(string source) =>
        AssertInvalid(ActivityRules.Create(new CreateActivityRequest("ok", Source: source), "a1", Now));

    [Fact]
    public void Update_changes_only_the_fields_it_carries()
    {
        var result = ActivityRules.Update(Running(), new UpdateActivityRequest(Progress: 40), Later);

        Assert.True(result.IsSuccess);
        var updated = result.Activity!;
        Assert.Equal(40, updated.Progress);
        Assert.Equal("Training model", updated.Title);
        Assert.Equal("epoch 1", updated.Subtitle);
        Assert.Equal(ActivityState.Running, updated.State);
        Assert.Equal(Now, updated.CreatedAt);
        Assert.Equal(Later, updated.UpdatedAt);
    }

    [Fact]
    public void Update_switches_between_running_and_needs_input()
    {
        var waiting = ActivityRules.Update(Running(), new UpdateActivityRequest(State: ActivityState.NeedsInput), Later).Activity!;
        Assert.Equal(ActivityState.NeedsInput, waiting.State);

        var resumed = ActivityRules.Update(waiting, new UpdateActivityRequest(State: ActivityState.Running), Later).Activity!;
        Assert.Equal(ActivityState.Running, resumed.State);
    }

    [Theory]
    [InlineData(ActivityState.Success)]
    [InlineData(ActivityState.Failure)]
    [InlineData(ActivityState.Info)]
    public void Update_cannot_finish_an_activity(ActivityState state) =>
        AssertInvalid(ActivityRules.Update(Running(), new UpdateActivityRequest(State: state), Later));

    [Fact]
    public void Update_with_an_empty_subtitle_clears_it()
    {
        var updated = ActivityRules.Update(Running(), new UpdateActivityRequest(Subtitle: ""), Later).Activity!;

        Assert.Null(updated.Subtitle);
    }

    [Fact]
    public void Update_rejects_an_empty_title_and_bad_progress()
    {
        AssertInvalid(ActivityRules.Update(Running(), new UpdateActivityRequest(Title: " "), Later));
        AssertInvalid(ActivityRules.Update(Running(), new UpdateActivityRequest(Progress: 150), Later));
    }

    [Fact]
    public void Update_of_a_finished_activity_is_rejected()
    {
        var done = ActivityRules.Finish(Running(), new FinishActivityRequest(ActivityState.Success), Later).Activity!;

        var result = ActivityRules.Update(done, new UpdateActivityRequest(Progress: 10), Later);

        Assert.Equal(ActivityError.Finished, result.Error);
    }

    [Fact]
    public void Finish_with_success_completes_measured_progress()
    {
        var done = ActivityRules.Finish(Running(progress: 40), new FinishActivityRequest(ActivityState.Success), Later).Activity!;

        Assert.Equal(ActivityState.Success, done.State);
        Assert.Equal(100, done.Progress);
        Assert.Equal(Later, done.FinishedAt);
    }

    [Fact]
    public void Finish_with_success_leaves_unmeasured_progress_alone()
    {
        var done = ActivityRules.Finish(Running(), new FinishActivityRequest(ActivityState.Success), Later).Activity!;

        Assert.Null(done.Progress);
    }

    [Fact]
    public void Finish_with_failure_keeps_progress_and_sets_the_message()
    {
        var failed = ActivityRules.Finish(Running(progress: 40), new FinishActivityRequest(ActivityState.Failure, "Tests failed"), Later).Activity!;

        Assert.Equal(ActivityState.Failure, failed.State);
        Assert.Equal(40, failed.Progress);
        Assert.Equal("Tests failed", failed.Subtitle);
    }

    [Theory]
    [InlineData(ActivityState.Running)]
    [InlineData(ActivityState.NeedsInput)]
    [InlineData(ActivityState.Info)]
    public void Finish_needs_success_or_failure(ActivityState state) =>
        AssertInvalid(ActivityRules.Finish(Running(), new FinishActivityRequest(state), Later));

    [Fact]
    public void Finish_twice_is_rejected()
    {
        var done = ActivityRules.Finish(Running(), new FinishActivityRequest(ActivityState.Success), Later).Activity!;

        var again = ActivityRules.Finish(done, new FinishActivityRequest(ActivityState.Failure), Later);

        Assert.Equal(ActivityError.Finished, again.Error);
    }

    private static void AssertInvalid(ActivityResult result)
    {
        Assert.Equal(ActivityError.Invalid, result.Error);
        Assert.Null(result.Activity);
        Assert.False(string.IsNullOrEmpty(result.Message));
    }
}
