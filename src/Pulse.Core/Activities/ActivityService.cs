using Pulse.Core.Api;
using Pulse.Core.Settings;

namespace Pulse.Core.Activities;

/// <param name="Version">Increases with every change, so a listener can drop snapshots that arrive out of order.</param>
/// <param name="Activities">All current activities in display order.</param>
public sealed record ActivitiesChangedEventArgs(long Version, IReadOnlyList<Activity> Activities);

/// <summary>
/// The hub every source and the island go through. Thread-safe. Finished activities are removed after
/// <see cref="PulseSettings.FinishedActivityLifetime"/> by a single timer set for the next expiry, so nothing polls.
/// </summary>
public sealed class ActivityService : IDisposable
{
    private readonly Lock _gate = new();
    private readonly ActivityQueue _queue = new();
    private readonly TimeProvider _time;
    private readonly TimeSpan _finishedLifetime;
    private readonly ITimer _expiryTimer;
    private long _version;
    private DateTimeOffset _lastCreatedAt = DateTimeOffset.MinValue;

    public ActivityService(PulseSettings settings, TimeProvider? time = null)
    {
        _time = time ?? TimeProvider.System;
        _finishedLifetime = settings.FinishedActivityLifetime;
        _expiryTimer = _time.CreateTimer(_ => RemoveExpired(), null, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
    }

    /// <summary>Raised after every change, on the thread that made it (a request thread or the expiry timer).</summary>
    public event EventHandler<ActivitiesChangedEventArgs>? Changed;

    public IReadOnlyList<Activity> Snapshot()
    {
        lock (_gate)
            return _queue.ToList();
    }

    public ActivityResult Create(CreateActivityRequest request) =>
        Change(now =>
        {
            // Two creates within one clock tick would tie, leaving display order and "latest" to chance.
            var createdAt = now > _lastCreatedAt ? now : _lastCreatedAt.AddTicks(1);
            var result = ActivityRules.Create(request, NewId(), createdAt);
            if (result.IsSuccess)
                _lastCreatedAt = createdAt;
            return result;
        });

    /// <param name="id">An activity id, or <see cref="ApiRoutes.Latest"/>.</param>
    /// <param name="latestSource">Source used to resolve <see cref="ApiRoutes.Latest"/>.</param>
    public ActivityResult Update(string id, UpdateActivityRequest request, string? latestSource = null) =>
        Change(now => Find(id, latestSource) is { } current
            ? ActivityRules.Update(current, request, now)
            : NotFound(id));

    /// <param name="id">An activity id, or <see cref="ApiRoutes.Latest"/>.</param>
    /// <param name="latestSource">Source used to resolve <see cref="ApiRoutes.Latest"/>.</param>
    public ActivityResult Finish(string id, FinishActivityRequest request, string? latestSource = null) =>
        Change(now => Find(id, latestSource) is { } current
            ? ActivityRules.Finish(current, request, now)
            : NotFound(id));

    public void Dispose() => _expiryTimer.Dispose();

    private ActivityResult Change(Func<DateTimeOffset, ActivityResult> apply)
    {
        ActivitiesChangedEventArgs? changed = null;
        ActivityResult result;

        lock (_gate)
        {
            var now = _time.GetUtcNow();
            result = apply(now);
            if (result.Activity is { } activity)
            {
                _queue.Upsert(activity);
                ScheduleExpiry(now);
                changed = new ActivitiesChangedEventArgs(++_version, _queue.ToList());
            }
        }

        if (changed is not null)
            Changed?.Invoke(this, changed);

        return result;
    }

    private Activity? Find(string id, string? latestSource)
    {
        if (id != ApiRoutes.Latest)
            return _queue.TryGet(id, out var activity) ? activity : null;

        var source = string.IsNullOrWhiteSpace(latestSource) ? ActivityRules.DefaultSource : latestSource.Trim().ToLowerInvariant();
        return _queue.All
            .Where(a => !a.IsFinished && a.Source == source)
            .MaxBy(a => a.CreatedAt);
    }

    private string NewId()
    {
        string id;
        do
            id = ActivityIds.New();
        while (_queue.TryGet(id, out _));

        return id;
    }

    private void RemoveExpired()
    {
        ActivitiesChangedEventArgs? changed = null;

        lock (_gate)
        {
            var now = _time.GetUtcNow();
            var expired = _queue.All.Where(a => ExpiresAt(a) <= now).Select(a => a.Id).ToList();
            foreach (var id in expired)
                _queue.Remove(id);

            ScheduleExpiry(now);
            if (expired.Count > 0)
                changed = new ActivitiesChangedEventArgs(++_version, _queue.ToList());
        }

        if (changed is not null)
            Changed?.Invoke(this, changed);
    }

    /// <summary>Points the one timer at the next finished activity to expire, or stops it.</summary>
    private void ScheduleExpiry(DateTimeOffset now)
    {
        var next = _queue.All.Select(ExpiresAt).Where(at => at is not null).Min();
        var dueTime = next is { } at ? Max(at - now, TimeSpan.Zero) : Timeout.InfiniteTimeSpan;
        _expiryTimer.Change(dueTime, Timeout.InfiniteTimeSpan);
    }

    private DateTimeOffset? ExpiresAt(Activity activity) => activity.FinishedAt + _finishedLifetime;

    private static TimeSpan Max(TimeSpan a, TimeSpan b) => a > b ? a : b;

    private static ActivityResult NotFound(string id) =>
        ActivityResult.Fail(ActivityError.NotFound,
            id == ApiRoutes.Latest ? "no unfinished activity from this source" : "no activity with this id");
}
