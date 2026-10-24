using Microsoft.Extensions.Time.Testing;
using Pulse.Core.Activities;
using Pulse.Core.Api;
using Pulse.Core.Settings;

namespace Pulse.Tests.Core;

public sealed class ActivityServiceTests : IDisposable
{
    private readonly FakeTimeProvider _time = new(new DateTimeOffset(2026, 10, 24, 12, 0, 0, TimeSpan.Zero));
    private readonly ActivityService _service;
    private readonly List<ActivitiesChangedEventArgs> _changes = [];

    public ActivityServiceTests()
    {
        _service = new ActivityService(new PulseSettings { FinishedActivitySeconds = 10 }, _time);
        _service.Changed += (_, e) => _changes.Add(e);
    }

    public void Dispose() => _service.Dispose();

    private Activity Start(string title, string source = "cli")
    {
        var result = _service.Create(new CreateActivityRequest(title, Source: source));
        Assert.True(result.IsSuccess, result.Message);
        return result.Activity!;
    }

    private string[] Titles() => _service.Snapshot().Select(a => a.Title).ToArray();

    [Fact]
    public void Create_assigns_unique_ids_and_reports_each_change_in_order()
    {
        var a = Start("A");
        var b = Start("B");

        Assert.NotEqual(a.Id, b.Id);
        Assert.Equal(ActivityIds.Length, a.Id.Length);
        Assert.Equal([1L, 2L], _changes.Select(c => c.Version));
        Assert.Equal(["B", "A"], _changes[^1].Activities.Select(x => x.Title));
    }

    [Fact]
    public void Latest_is_the_newest_unfinished_activity_from_that_source()
    {
        var first = Start("first");
        _time.Advance(TimeSpan.FromSeconds(1));
        var second = Start("second");
        _time.Advance(TimeSpan.FromSeconds(1));
        Start("other tool", source: "backup");

        var progress = _service.Update(ApiRoutes.Latest, new UpdateActivityRequest(Progress: 40), "cli");
        Assert.Equal(second.Id, progress.Activity!.Id);

        _service.Finish(second.Id, new FinishActivityRequest(ActivityState.Success));
        var next = _service.Update(ApiRoutes.Latest, new UpdateActivityRequest(Progress: 70), "cli");

        Assert.Equal(first.Id, next.Activity!.Id);
    }

    [Fact]
    public void Activities_created_in_the_same_instant_keep_their_order()
    {
        var ids = Enumerable.Range(0, 20).Select(i => Start($"job {i}").Id).ToList();

        Assert.Equal(Enumerable.Range(0, 20).Reverse().Select(i => $"job {i}"), Titles());
        Assert.All(_service.Snapshot().Zip(_service.Snapshot().Skip(1)), pair => Assert.True(pair.First.CreatedAt > pair.Second.CreatedAt));

        var progress = _service.Update(ApiRoutes.Latest, new UpdateActivityRequest(Progress: 10), "cli");
        Assert.Equal(ids[^1], progress.Activity!.Id);
    }

    [Fact]
    public void Latest_without_an_unfinished_activity_is_not_found()
    {
        var only = Start("only");
        _service.Finish(only.Id, new FinishActivityRequest(ActivityState.Success));

        var result = _service.Finish(ApiRoutes.Latest, new FinishActivityRequest(ActivityState.Success), "cli");

        Assert.Equal(ActivityError.NotFound, result.Error);
    }

    [Fact]
    public void Unknown_ids_are_not_found_and_change_nothing()
    {
        Start("A");
        var changesBefore = _changes.Count;

        var result = _service.Update("doesnotexist", new UpdateActivityRequest(Progress: 1));

        Assert.Equal(ActivityError.NotFound, result.Error);
        Assert.Equal(changesBefore, _changes.Count);
    }

    [Fact]
    public void A_rejected_change_raises_no_event()
    {
        var a = Start("A");
        var changesBefore = _changes.Count;

        var result = _service.Update(a.Id, new UpdateActivityRequest(Progress: 500));

        Assert.Equal(ActivityError.Invalid, result.Error);
        Assert.Equal(changesBefore, _changes.Count);
    }

    [Fact]
    public void Finished_activities_fade_out_after_the_configured_time()
    {
        var job = Start("job");
        _service.Finish(job.Id, new FinishActivityRequest(ActivityState.Success));

        _time.Advance(TimeSpan.FromSeconds(9));
        Assert.Equal(["job"], Titles());

        _time.Advance(TimeSpan.FromSeconds(1));
        Assert.Empty(Titles());
        Assert.Empty(_changes[^1].Activities);
    }

    [Fact]
    public void Notices_fade_out_and_running_activities_stay()
    {
        Start("still running");
        _service.Create(new CreateActivityRequest("Backup finished", State: ActivityState.Info));

        _time.Advance(TimeSpan.FromHours(1));

        Assert.Equal(["still running"], Titles());
    }

    [Fact]
    public void Each_finished_activity_fades_out_on_its_own_schedule()
    {
        var a = Start("A");
        var b = Start("B");
        _service.Finish(a.Id, new FinishActivityRequest(ActivityState.Failure));
        _time.Advance(TimeSpan.FromSeconds(5));
        _service.Finish(b.Id, new FinishActivityRequest(ActivityState.Success));

        _time.Advance(TimeSpan.FromSeconds(5));
        Assert.Equal(["B"], Titles());

        _time.Advance(TimeSpan.FromSeconds(5));
        Assert.Empty(Titles());
    }

    [Fact]
    public void Finished_lifetime_comes_from_settings()
    {
        using var service = new ActivityService(new PulseSettings { FinishedActivitySeconds = 3 }, _time);
        service.Create(new CreateActivityRequest("note", State: ActivityState.Info));

        _time.Advance(TimeSpan.FromSeconds(3));

        Assert.Empty(service.Snapshot());
    }
}
