using Pulse.Core.Activities;

namespace Pulse.Tests.Core;

public class ActivityQueueTests
{
    private static readonly DateTimeOffset T0 = new(2026, 10, 24, 12, 0, 0, TimeSpan.Zero);

    private static Activity Make(string id, ActivityState state = ActivityState.Running, int priority = 0, int createdSecond = 0) => new()
    {
        Id = id,
        Source = "cli",
        Title = id,
        State = state,
        Priority = priority,
        CreatedAt = T0.AddSeconds(createdSecond),
        UpdatedAt = T0.AddSeconds(createdSecond),
        FinishedAt = state.IsFinished() ? T0.AddSeconds(createdSecond) : null,
    };

    private static ActivityQueue QueueOf(params Activity[] activities)
    {
        var queue = new ActivityQueue();
        foreach (var activity in activities)
            queue.Upsert(activity);
        return queue;
    }

    private static string[] Ids(ActivityQueue queue) => queue.ToList().Select(a => a.Id).ToArray();

    [Fact]
    public void An_empty_queue_shows_nothing()
    {
        var queue = new ActivityQueue();

        Assert.Null(queue.Peek());
        Assert.Empty(queue.ToList());
    }

    [Fact]
    public void Higher_priority_wins_over_urgency()
    {
        var queue = QueueOf(
            Make("asking", ActivityState.NeedsInput),
            Make("important", ActivityState.Running, priority: 10));

        Assert.Equal("important", queue.Peek()!.Id);
    }

    [Fact]
    public void Same_priority_orders_by_urgency_of_the_state()
    {
        var queue = QueueOf(
            Make("info", ActivityState.Info),
            Make("success", ActivityState.Success),
            Make("running", ActivityState.Running),
            Make("failure", ActivityState.Failure),
            Make("asking", ActivityState.NeedsInput));

        Assert.Equal(["asking", "failure", "running", "success", "info"], Ids(queue));
    }

    [Fact]
    public void Same_urgency_shows_the_newest_first()
    {
        var queue = QueueOf(
            Make("old", createdSecond: 1),
            Make("new", createdSecond: 3),
            Make("middle", createdSecond: 2));

        Assert.Equal(["new", "middle", "old"], Ids(queue));
    }

    [Fact]
    public void Upsert_moves_an_updated_activity_to_its_new_place()
    {
        var queue = QueueOf(Make("a", createdSecond: 1), Make("b", createdSecond: 2));
        Assert.Equal("b", queue.Peek()!.Id);

        queue.Upsert(Make("a", ActivityState.NeedsInput, createdSecond: 1));

        Assert.Equal(["a", "b"], Ids(queue));
        Assert.Equal(2, queue.Count);
    }

    [Fact]
    public void Remove_takes_the_activity_out()
    {
        var queue = QueueOf(Make("a"), Make("b", createdSecond: 1));

        Assert.True(queue.Remove("b"));
        Assert.False(queue.Remove("b"));

        Assert.Equal(["a"], Ids(queue));
        Assert.False(queue.TryGet("b", out _));
    }
}
