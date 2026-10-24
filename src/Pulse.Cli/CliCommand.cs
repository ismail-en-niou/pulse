using Pulse.Core.Activities;

namespace Pulse.Cli;

/// <summary>A parsed command line. <see cref="CliParser"/> makes them; <see cref="CliMapper"/> turns them into API calls.</summary>
internal abstract record CliCommand;

internal sealed record StartCommand(string Title, string? Subtitle, int? Progress, int? Priority, bool NeedsInput, string Source) : CliCommand;

/// <param name="Id">The activity to change, or null for the newest unfinished one from <paramref name="Source"/>.</param>
internal sealed record ProgressCommand(int Progress, string? Message, string? Id, string Source) : CliCommand;

internal sealed record UpdateCommand(
    string? Title, string? Subtitle, ActivityState? State, int? Progress, int? Priority, string? Id, string Source) : CliCommand;

/// <summary><c>pulse done</c> (<paramref name="Success"/>) or <c>pulse fail</c>.</summary>
internal sealed record FinishCommand(bool Success, string? Message, string? Id, string Source) : CliCommand;

internal sealed record NotifyCommand(string Message, string? Subtitle, int? Priority, string Source) : CliCommand;

internal sealed record ListCommand(bool Json) : CliCommand;

internal sealed record HelpCommand : CliCommand;

internal sealed record VersionCommand : CliCommand;

/// <summary>Either a command or a usage error, never both.</summary>
internal readonly record struct CliParseResult(CliCommand? Command, string? Error)
{
    public static CliParseResult Ok(CliCommand command) => new(command, null);

    public static CliParseResult Fail(string error) => new(null, error);
}
