using System.Reflection;
using System.Text.Json;
using Pulse.Core.Activities;
using Pulse.Core.Api;

namespace Pulse.Cli;

internal static class ExitCodes
{
    public const int Ok = 0;
    public const int Rejected = 1;
    public const int Usage = 2;
    public const int NotRunning = 3;
}

/// <summary>Parses, calls the running app and prints the result. Errors go to stderr, results to stdout.</summary>
internal static class CliRunner
{
    public const string Usage = """
        pulse: push activities to the Pulse island

        Usage:
          pulse start <title> [--subtitle <text>] [--progress <0-100>] [--priority <n>] [--needs-input]
          pulse progress <0-100> [<message>] [--id <id>]
          pulse update [--title <text>] [--subtitle <text>] [--state running|needs-input]
                       [--progress <0-100>] [--priority <n>] [--id <id>]
          pulse done [<message>] [--id <id>]
          pulse fail [<message>] [--id <id>]
          pulse notify <message> [--subtitle <text>] [--priority <n>]
          pulse list [--json]
          pulse help | --version

        start and notify print the new activity's id:  id=$(pulse start "Training model")
        Without --id, progress, update, done and fail change the newest unfinished
        activity started with the same --source (default: cli).
        --priority is -100 to 100 (default 0); higher shows first.

        Exit codes: 0 ok, 1 rejected by Pulse, 2 usage error, 3 Pulse isn't running.
        """;

    /// <param name="readConnection">Reads connection.json; only called for commands that talk to the app.</param>
    public static async Task<int> RunAsync(string[] args, TextWriter stdout, TextWriter stderr, Func<ConnectionInfo?> readConnection)
    {
        var parsed = CliParser.Parse(args);
        switch (parsed.Command)
        {
            case null:
                await stderr.WriteLineAsync($"pulse: {parsed.Error}");
                await stderr.WriteLineAsync("Run 'pulse help' for usage.");
                return ExitCodes.Usage;
            case HelpCommand:
                await stdout.WriteLineAsync(Usage);
                return ExitCodes.Ok;
            case VersionCommand:
                await stdout.WriteLineAsync($"pulse {Version}");
                return ExitCodes.Ok;
        }

        var call = CliMapper.Map(parsed.Command);
        if (readConnection() is not { IsListening: true } connection)
            return await NotRunning(stderr);

        ApiResponse response;
        try
        {
            using var client = new PulseClient(connection);
            response = await client.SendAsync(call);
        }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException)
        {
            // Pulse quit without cleaning up, or hangs: either way nothing useful is listening.
            return await NotRunning(stderr);
        }

        if (!response.IsSuccess)
        {
            await stderr.WriteLineAsync($"pulse: {ErrorMessage(response)}");
            return ExitCodes.Rejected;
        }

        await WriteOutput(call.Output, response.Body, stdout);
        return ExitCodes.Ok;
    }

    private static async Task WriteOutput(CliOutput output, string body, TextWriter stdout)
    {
        switch (output)
        {
            case CliOutput.Id:
                await stdout.WriteLineAsync(JsonSerializer.Deserialize(body, PulseJsonContext.Default.Activity)!.Id);
                break;
            case CliOutput.Json:
                await stdout.WriteLineAsync(body);
                break;
            case CliOutput.Table:
                var activities = JsonSerializer.Deserialize(body, PulseJsonContext.Default.ActivityListResponse)!.Activities;
                if (activities.Count == 0)
                    await stdout.WriteLineAsync("No activities.");
                foreach (var activity in activities)
                    await stdout.WriteLineAsync(FormatRow(activity));
                break;
        }
    }

    /// <summary>One line per activity: id, state, progress, title and subtitle.</summary>
    public static string FormatRow(Activity activity)
    {
        var progress = activity.Progress is { } p ? $"{p}%" : "";
        var subtitle = activity.Subtitle is { } s ? $" · {s}" : "";
        return $"{activity.Id,-10}  {StateName(activity.State),-11}  {progress,4}  {activity.Title}{subtitle}";
    }

    private static string StateName(ActivityState state) => state switch
    {
        ActivityState.NeedsInput => "needs-input",
        _ => state.ToString().ToLowerInvariant(),
    };

    private static string ErrorMessage(ApiResponse response)
    {
        if (response.Status == 401)
            return "Pulse rejected the token in connection.json; restart Pulse";

        try
        {
            return JsonSerializer.Deserialize(response.Body, PulseJsonContext.Default.ErrorResponse)?.Message ?? $"HTTP {response.Status}";
        }
        catch (JsonException)
        {
            return $"HTTP {response.Status}";
        }
    }

    private static async Task<int> NotRunning(TextWriter stderr)
    {
        await stderr.WriteLineAsync("pulse: Pulse isn't running. Start the Pulse app first.");
        return ExitCodes.NotRunning;
    }

    private static string Version
    {
        get
        {
            var version = typeof(CliRunner).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "unknown";
            var plus = version.IndexOf('+'); // drop the commit hash the SDK appends
            return plus < 0 ? version : version[..plus];
        }
    }
}
