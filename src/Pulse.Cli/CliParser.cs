using System.Globalization;
using Pulse.Core.Activities;

namespace Pulse.Cli;

/// <summary>
/// Parses <c>pulse &lt;command&gt; [arguments] [--option value | --option=value | --flag]</c>. Pure, so every rule is
/// unit-tested. Words after the command are joined, so <c>pulse notify Backup finished</c> needs no quotes;
/// <c>--</c> ends the options for text that starts with a dash.
/// </summary>
internal static class CliParser
{
    public const string DefaultSource = "cli";

    private static readonly Dictionary<string, Spec> Specs = new(StringComparer.Ordinal)
    {
        ["start"] = new(["subtitle", "progress", "priority", "source"], ["needs-input"]),
        ["progress"] = new(["id", "source"], []),
        ["update"] = new(["title", "subtitle", "state", "progress", "priority", "id", "source"], []),
        ["done"] = new(["id", "source"], []),
        ["fail"] = new(["id", "source"], []),
        ["notify"] = new(["subtitle", "priority", "source"], []),
        ["list"] = new([], ["json"]),
    };

    public static CliParseResult Parse(IReadOnlyList<string> args)
    {
        if (args.Count == 0)
            return CliParseResult.Fail("missing command");

        var name = args[0];
        if (name is "help" or "--help" or "-h")
            return CliParseResult.Ok(new HelpCommand());
        if (name is "version" or "--version")
            return CliParseResult.Ok(new VersionCommand());
        if (!Specs.TryGetValue(name, out var spec))
            return CliParseResult.Fail($"unknown command '{name}'");

        var options = new Dictionary<string, string>(StringComparer.Ordinal);
        var flags = new HashSet<string>(StringComparer.Ordinal);
        var words = new List<string>();
        var onlyWords = false;

        for (var i = 1; i < args.Count; i++)
        {
            var arg = args[i];
            if (onlyWords || !IsOption(arg))
            {
                words.Add(arg);
                continue;
            }

            if (arg == "--")
            {
                onlyWords = true;
                continue;
            }

            if (arg is "-h" or "--help")
                return CliParseResult.Ok(new HelpCommand());
            if (!arg.StartsWith("--", StringComparison.Ordinal))
                return CliParseResult.Fail($"unknown option '{arg}' for 'pulse {name}'");

            var equals = arg.IndexOf('=');
            var key = equals < 0 ? arg[2..] : arg[2..equals];
            var inlineValue = equals < 0 ? null : arg[(equals + 1)..];

            if (spec.Flags.Contains(key))
            {
                if (inlineValue is not null)
                    return CliParseResult.Fail($"--{key} takes no value");
                flags.Add(key);
            }
            else if (spec.Options.Contains(key))
            {
                if (options.ContainsKey(key))
                    return CliParseResult.Fail($"--{key} is given twice");

                var value = inlineValue ?? (i + 1 < args.Count ? args[++i] : null);
                if (value is null)
                    return CliParseResult.Fail($"--{key} needs a value");
                options[key] = value;
            }
            else
            {
                return CliParseResult.Fail($"unknown option '--{key}' for 'pulse {name}'");
            }
        }

        try
        {
            return CliParseResult.Ok(Build(name, words, options, flags));
        }
        catch (UsageException e)
        {
            return CliParseResult.Fail(e.Message);
        }
    }

    private static CliCommand Build(string name, List<string> words, Dictionary<string, string> options, HashSet<string> flags)
    {
        var source = options.GetValueOrDefault("source") ?? DefaultSource;
        var id = options.GetValueOrDefault("id");
        var subtitle = options.GetValueOrDefault("subtitle");
        var progress = Progress(options.GetValueOrDefault("progress"));
        var priority = Priority(options.GetValueOrDefault("priority"));

        switch (name)
        {
            case "start":
                return new StartCommand(Text(words, "a title"), subtitle, progress, priority, flags.Contains("needs-input"), source);

            case "progress":
                if (words.Count == 0)
                    throw new UsageException("missing progress (0-100)");
                return new ProgressCommand(Progress(words[0])!.Value, OptionalText(words.Skip(1)), id, source);

            case "update":
                if (words.Count > 0)
                    throw new UsageException("update takes options only, e.g. --subtitle \"epoch 4\"");
                var title = options.GetValueOrDefault("title");
                var state = State(options.GetValueOrDefault("state"));
                if (title is null && subtitle is null && state is null && progress is null && priority is null)
                    throw new UsageException("nothing to update: give --title, --subtitle, --state, --progress or --priority");
                return new UpdateCommand(title, subtitle, state, progress, priority, id, source);

            case "done":
            case "fail":
                return new FinishCommand(name == "done", OptionalText(words), id, source);

            case "notify":
                return new NotifyCommand(Text(words, "a message"), subtitle, priority, source);

            default: // list
                if (words.Count > 0)
                    throw new UsageException("list takes no arguments");
                return new ListCommand(flags.Contains("json"));
        }
    }

    /// <summary>"--x" and "-x" are options; "-" alone and negative numbers are words.</summary>
    private static bool IsOption(string arg) =>
        arg.Length > 1 && arg[0] == '-' && !char.IsAsciiDigit(arg[1]);

    private static string Text(List<string> words, string what) =>
        OptionalText(words) ?? throw new UsageException($"missing {what}");

    private static string? OptionalText(IEnumerable<string> words)
    {
        var text = string.Join(' ', words).Trim();
        return text.Length == 0 ? null : text;
    }

    private static int? Progress(string? value)
    {
        if (value is null)
            return null;

        var number = value.EndsWith('%') ? value[..^1] : value;
        if (!int.TryParse(number, NumberStyles.None, CultureInfo.InvariantCulture, out var progress) || progress > 100)
            throw new UsageException($"progress must be a whole number from 0 to 100, not '{value}'");
        return progress;
    }

    private static int? Priority(string? value)
    {
        if (value is null)
            return null;

        if (!int.TryParse(value, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var priority) ||
            priority is < ActivityRules.MinPriority or > ActivityRules.MaxPriority)
            throw new UsageException($"priority must be a whole number from {ActivityRules.MinPriority} to {ActivityRules.MaxPriority}, not '{value}'");
        return priority;
    }

    private static ActivityState? State(string? value) => value switch
    {
        null => null,
        "running" => ActivityState.Running,
        "needs-input" or "needsInput" => ActivityState.NeedsInput,
        _ => throw new UsageException($"state must be running or needs-input, not '{value}'"),
    };

    /// <param name="Options">Options that take a value.</param>
    private sealed record Spec(string[] Options, string[] Flags);

    private sealed class UsageException(string message) : Exception(message);
}
