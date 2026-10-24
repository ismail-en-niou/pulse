using Pulse.Core.Activities;

namespace Pulse.Core.Api;

/// <summary>Body of <c>POST /v1/activities</c>. Only the title is required.</summary>
public sealed record CreateActivityRequest(
    string? Title,
    string? Subtitle = null,
    ActivityState? State = null,
    int? Progress = null,
    int? Priority = null,
    string? Source = null);

/// <summary>Body of <c>PATCH /v1/activities/{id}</c>. Null leaves a field unchanged; an empty subtitle clears it.</summary>
public sealed record UpdateActivityRequest(
    string? Title = null,
    string? Subtitle = null,
    ActivityState? State = null,
    int? Progress = null,
    int? Priority = null);

/// <summary>Body of <c>POST /v1/activities/{id}/finish</c>: success or failure, with an optional message.</summary>
public sealed record FinishActivityRequest(ActivityState State, string? Subtitle = null);

public sealed record ActivityListResponse(IReadOnlyList<Activity> Activities);

public sealed record ErrorResponse(string Error, string Message);

public static class ApiRoutes
{
    public const string Activities = "/v1/activities";

    /// <summary>Stands in for an id: the newest unfinished activity from the <see cref="SourceQuery"/> source.</summary>
    public const string Latest = "latest";

    public const string SourceQuery = "source";

    public static string Activity(string id) => $"{Activities}/{Uri.EscapeDataString(id)}";

    public static string Finish(string id) => $"{Activity(id)}/finish";
}
