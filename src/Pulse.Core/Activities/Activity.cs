using System.Security.Cryptography;

namespace Pulse.Core.Activities;

/// <summary>One thing the island can show: a running job, a question for the user, a result or a notice.</summary>
public sealed record Activity
{
    public required string Id { get; init; }

    /// <summary>Who reported it, e.g. "cli" or "claude-code".</summary>
    public required string Source { get; init; }

    public required string Title { get; init; }

    public string? Subtitle { get; init; }

    public ActivityState State { get; init; }

    /// <summary>0–100, or null when the activity has no measurable progress.</summary>
    public int? Progress { get; init; }

    /// <summary>−100 to 100; higher is shown first. Defaults to 0.</summary>
    public int Priority { get; init; }

    public DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset UpdatedAt { get; init; }

    public DateTimeOffset? FinishedAt { get; init; }

    public bool IsFinished => State.IsFinished();
}

public static class ActivityIds
{
    /// <summary>Lowercase Crockford base32: no i, l, o or u, so ids are easy to read and type.</summary>
    private const string Alphabet = "0123456789abcdefghjkmnpqrstvwxyz";

    public const int Length = 10;

    public static string New() => new(RandomNumberGenerator.GetItems<char>(Alphabet, Length));
}
