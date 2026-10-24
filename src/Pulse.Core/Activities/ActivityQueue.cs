using System.Diagnostics.CodeAnalysis;

namespace Pulse.Core.Activities;

/// <summary>
/// Priority queue of activities with lookup by id. The first item is what the collapsed island shows;
/// the whole order is the expanded list. Not thread-safe: <see cref="ActivityService"/> guards it.
/// </summary>
public sealed class ActivityQueue
{
    private readonly Dictionary<string, Activity> _byId = new(StringComparer.Ordinal);
    private readonly SortedSet<Activity> _ordered = new(DisplayOrder.Instance);

    public int Count => _byId.Count;

    public IEnumerable<Activity> All => _byId.Values;

    public Activity? Peek() => _ordered.Count == 0 ? null : _ordered.Min;

    public bool TryGet(string id, [MaybeNullWhen(false)] out Activity activity) => _byId.TryGetValue(id, out activity);

    /// <summary>Adds the activity, or replaces the one with the same id and moves it to its new place.</summary>
    public void Upsert(Activity activity)
    {
        if (_byId.Remove(activity.Id, out var previous))
            _ordered.Remove(previous);

        _byId.Add(activity.Id, activity);
        _ordered.Add(activity);
    }

    public bool Remove(string id)
    {
        if (!_byId.Remove(id, out var previous))
            return false;

        _ordered.Remove(previous);
        return true;
    }

    public IReadOnlyList<Activity> ToList() => [.. _ordered];

    /// <summary>
    /// Priority first (set by the caller), then urgency of the state, then the newest activity.
    /// Ordering by creation time keeps two running jobs from swapping places on every progress update.
    /// </summary>
    public sealed class DisplayOrder : IComparer<Activity>
    {
        public static readonly DisplayOrder Instance = new();

        public int Compare(Activity? x, Activity? y)
        {
            if (ReferenceEquals(x, y))
                return 0;
            if (x is null)
                return 1;
            if (y is null)
                return -1;

            var byPriority = y.Priority.CompareTo(x.Priority);
            if (byPriority != 0)
                return byPriority;

            var byUrgency = y.State.Urgency().CompareTo(x.State.Urgency());
            if (byUrgency != 0)
                return byUrgency;

            var byAge = y.CreatedAt.CompareTo(x.CreatedAt);
            return byAge != 0 ? byAge : string.CompareOrdinal(x.Id, y.Id);
        }
    }
}
