using Pulse.Core.Api;

namespace Pulse.Core.Activities;

public enum ActivityError
{
    None,
    NotFound,

    /// <summary>The activity already finished and can't change.</summary>
    Finished,
    Invalid,
}

public readonly record struct ActivityResult(Activity? Activity, ActivityError Error, string? Message)
{
    public bool IsSuccess => Error == ActivityError.None;

    public static ActivityResult Ok(Activity activity) => new(activity, ActivityError.None, null);

    public static ActivityResult Fail(ActivityError error, string message) => new(null, error, message);
}

/// <summary>Validation and state transitions. Pure: the caller supplies the id and the time.</summary>
public static class ActivityRules
{
    public const int MaxTitleLength = 120;
    public const int MaxSubtitleLength = 240;
    public const int MaxSourceLength = 40;
    public const int MinPriority = -100;
    public const int MaxPriority = 100;

    /// <summary>Source used when an API caller doesn't name one. The CLI sends "cli".</summary>
    public const string DefaultSource = "api";

    public static ActivityResult Create(CreateActivityRequest request, string id, DateTimeOffset now)
    {
        var title = request.Title?.Trim();
        if (string.IsNullOrEmpty(title))
            return Invalid("title is required");

        if ((CheckTitle(title) ?? CheckSubtitle(request.Subtitle) ?? CheckProgress(request.Progress) ??
             CheckPriority(request.Priority)) is { } error)
            return Invalid(error);

        var source = string.IsNullOrWhiteSpace(request.Source) ? DefaultSource : request.Source.Trim().ToLowerInvariant();
        if (CheckSource(source) is { } sourceError)
            return Invalid(sourceError);

        var state = request.State ?? ActivityState.Running;
        return ActivityResult.Ok(new Activity
        {
            Id = id,
            Source = source,
            Title = title,
            Subtitle = Normalize(request.Subtitle),
            State = state,
            Progress = request.Progress,
            Priority = request.Priority ?? 0,
            CreatedAt = now,
            UpdatedAt = now,
            FinishedAt = state.IsFinished() ? now : null,
        });
    }

    public static ActivityResult Update(Activity current, UpdateActivityRequest request, DateTimeOffset now)
    {
        if (current.IsFinished)
            return ActivityResult.Fail(ActivityError.Finished, "the activity has already finished");

        if (request.State is { } state && state is not (ActivityState.Running or ActivityState.NeedsInput))
            return Invalid("state can only change between running and needsInput; finish the activity instead");

        var title = request.Title?.Trim();
        if (title is not null && title.Length == 0)
            return Invalid("title can't be empty");

        if (((title is null ? null : CheckTitle(title)) ?? CheckSubtitle(request.Subtitle) ??
             CheckProgress(request.Progress) ?? CheckPriority(request.Priority)) is { } error)
            return Invalid(error);

        return ActivityResult.Ok(current with
        {
            Title = title ?? current.Title,
            Subtitle = request.Subtitle is null ? current.Subtitle : Normalize(request.Subtitle),
            State = request.State ?? current.State,
            Progress = request.Progress ?? current.Progress,
            Priority = request.Priority ?? current.Priority,
            UpdatedAt = now,
        });
    }

    public static ActivityResult Finish(Activity current, FinishActivityRequest request, DateTimeOffset now)
    {
        if (current.IsFinished)
            return ActivityResult.Fail(ActivityError.Finished, "the activity has already finished");

        if (request.State is not (ActivityState.Success or ActivityState.Failure))
            return Invalid("an activity finishes with success or failure");

        if (CheckSubtitle(request.Subtitle) is { } error)
            return Invalid(error);

        return ActivityResult.Ok(current with
        {
            State = request.State,
            Subtitle = request.Subtitle is null ? current.Subtitle : Normalize(request.Subtitle),
            // A successful job is complete; a failed one keeps the progress it reached.
            Progress = request.State == ActivityState.Success && current.Progress is not null ? 100 : current.Progress,
            UpdatedAt = now,
            FinishedAt = now,
        });
    }

    private static string? CheckTitle(string title) =>
        title.Length > MaxTitleLength ? $"title is longer than {MaxTitleLength} characters" : null;

    private static string? CheckSubtitle(string? subtitle) =>
        subtitle is not null && subtitle.Trim().Length > MaxSubtitleLength
            ? $"subtitle is longer than {MaxSubtitleLength} characters"
            : null;

    private static string? CheckProgress(int? progress) =>
        progress is < 0 or > 100 ? "progress must be between 0 and 100" : null;

    private static string? CheckPriority(int? priority) =>
        priority is < MinPriority or > MaxPriority ? $"priority must be between {MinPriority} and {MaxPriority}" : null;

    private static string? CheckSource(string source)
    {
        if (source.Length > MaxSourceLength)
            return $"source is longer than {MaxSourceLength} characters";

        foreach (var c in source)
        {
            if (!(char.IsAsciiLetterOrDigit(c) || c is '-' or '_' or '.'))
                return "source may only contain letters, digits, '-', '_' and '.'";
        }

        return null;
    }

    /// <summary>Trims, and turns an empty string into "no subtitle".</summary>
    private static string? Normalize(string? subtitle)
    {
        var trimmed = subtitle?.Trim();
        return string.IsNullOrEmpty(trimmed) ? null : trimmed;
    }

    private static ActivityResult Invalid(string message) => ActivityResult.Fail(ActivityError.Invalid, message);
}
