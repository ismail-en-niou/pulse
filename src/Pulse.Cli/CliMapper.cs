using Pulse.Core.Activities;
using Pulse.Core.Api;

namespace Pulse.Cli;

/// <summary>What the CLI prints after a successful call.</summary>
internal enum CliOutput
{
    Nothing,

    /// <summary>The activity's id, so scripts can do <c>id=$(pulse start "Build")</c>.</summary>
    Id,

    Table,
    Json,
}

/// <param name="Body">A request record from <see cref="Pulse.Core.Api"/>, or null.</param>
internal sealed record ApiCall(HttpMethod Method, string Path, object? Body, CliOutput Output);

/// <summary>Turns a parsed command into the API call that carries it out. Pure, so it is unit-tested.</summary>
internal static class CliMapper
{
    public static ApiCall Map(CliCommand command) => command switch
    {
        StartCommand c => new ApiCall(HttpMethod.Post, ApiRoutes.Activities,
            new CreateActivityRequest(c.Title, c.Subtitle, c.NeedsInput ? ActivityState.NeedsInput : null, c.Progress, c.Priority, c.Source),
            CliOutput.Id),

        ProgressCommand c => new ApiCall(HttpMethod.Patch, ActivityPath(c.Id, c.Source),
            new UpdateActivityRequest(Subtitle: c.Message, Progress: c.Progress),
            CliOutput.Nothing),

        UpdateCommand c => new ApiCall(HttpMethod.Patch, ActivityPath(c.Id, c.Source),
            new UpdateActivityRequest(c.Title, c.Subtitle, c.State, c.Progress, c.Priority),
            CliOutput.Nothing),

        FinishCommand c => new ApiCall(HttpMethod.Post, FinishPath(c.Id, c.Source),
            new FinishActivityRequest(c.Success ? ActivityState.Success : ActivityState.Failure, c.Message),
            CliOutput.Nothing),

        NotifyCommand c => new ApiCall(HttpMethod.Post, ApiRoutes.Activities,
            new CreateActivityRequest(c.Message, c.Subtitle, ActivityState.Info, Priority: c.Priority, Source: c.Source),
            CliOutput.Id),

        ListCommand c => new ApiCall(HttpMethod.Get, ApiRoutes.Activities, null, c.Json ? CliOutput.Json : CliOutput.Table),

        _ => throw new ArgumentException($"{command.GetType().Name} has no API call", nameof(command)),
    };

    /// <summary>An explicit id, or "latest" resolved within the source.</summary>
    private static string ActivityPath(string? id, string source) =>
        id is not null ? ApiRoutes.Activity(id) : ApiRoutes.Activity(ApiRoutes.Latest) + SourceQuery(source);

    private static string FinishPath(string? id, string source) =>
        id is not null ? ApiRoutes.Finish(id) : ApiRoutes.Finish(ApiRoutes.Latest) + SourceQuery(source);

    private static string SourceQuery(string source) => $"?{ApiRoutes.SourceQuery}={Uri.EscapeDataString(source)}";
}
