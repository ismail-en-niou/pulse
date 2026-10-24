using Pulse.Cli;
using Pulse.Core.Activities;

namespace Pulse.Tests.Cli;

public class CliParserTests
{
    private static CliCommand Parse(params string[] args)
    {
        var result = CliParser.Parse(args);
        Assert.True(result.Command is not null, result.Error);
        return result.Command;
    }

    private static string ParseError(params string[] args)
    {
        var result = CliParser.Parse(args);
        Assert.Null(result.Command);
        Assert.False(string.IsNullOrEmpty(result.Error));
        return result.Error;
    }

    [Fact]
    public void Start_takes_a_title_and_defaults_to_the_cli_source() =>
        Assert.Equal(new StartCommand("Training model", null, null, null, false, "cli"), Parse("start", "Training model"));

    [Fact]
    public void Start_joins_unquoted_words() =>
        Assert.Equal("Training the model", ((StartCommand)Parse("start", "Training", "the", "model")).Title);

    [Fact]
    public void Start_takes_every_option_in_both_forms()
    {
        var command = Parse("start", "Deploy", "--subtitle", "to staging", "--progress=10", "--priority", "-5", "--needs-input", "--source=deploy");

        Assert.Equal(new StartCommand("Deploy", "to staging", 10, -5, true, "deploy"), command);
    }

    [Fact]
    public void Options_may_come_before_the_words() =>
        Assert.Equal("Build", ((StartCommand)Parse("start", "--priority", "3", "Build")).Title);

    [Fact]
    public void Double_dash_lets_text_start_with_a_dash() =>
        Assert.Equal("--not an option", ((NotifyCommand)Parse("notify", "--", "--not an option")).Message);

    [Theory]
    [InlineData("40", 40)]
    [InlineData("40%", 40)]
    [InlineData("0", 0)]
    [InlineData("100", 100)]
    public void Progress_takes_a_percentage(string value, int expected) =>
        Assert.Equal(new ProgressCommand(expected, null, null, "cli"), Parse("progress", value));

    [Fact]
    public void Progress_takes_a_message_and_an_id() =>
        Assert.Equal(new ProgressCommand(40, "epoch 4 of 10", "abc123", "cli"), Parse("progress", "40", "epoch", "4", "of", "10", "--id", "abc123"));

    [Theory]
    [InlineData("101")]
    [InlineData("-1")]
    [InlineData("4.5")]
    [InlineData("forty")]
    [InlineData("")]
    public void Progress_outside_0_to_100_is_a_usage_error(string value) =>
        Assert.Contains("progress", ParseError("progress", value));

    [Fact]
    public void Progress_needs_a_number() => Assert.Contains("progress", ParseError("progress"));

    [Fact]
    public void Done_and_fail_take_an_optional_message()
    {
        Assert.Equal(new FinishCommand(true, null, null, "cli"), Parse("done"));
        Assert.Equal(new FinishCommand(false, "Tests failed", null, "cli"), Parse("fail", "Tests failed"));
        Assert.Equal(new FinishCommand(true, "All green", "abc", "ci"), Parse("done", "All", "green", "--id=abc", "--source", "ci"));
    }

    [Fact]
    public void Notify_takes_a_message() =>
        Assert.Equal(new NotifyCommand("Backup finished", "12 GB", 2, "cli"), Parse("notify", "Backup", "finished", "--subtitle", "12 GB", "--priority", "2"));

    [Fact]
    public void Update_takes_options()
    {
        var command = Parse("update", "--title", "Train v2", "--state", "needs-input", "--priority", "10");

        Assert.Equal(new UpdateCommand("Train v2", null, ActivityState.NeedsInput, null, 10, null, "cli"), command);
    }

    [Theory]
    [InlineData("running", ActivityState.Running)]
    [InlineData("needs-input", ActivityState.NeedsInput)]
    [InlineData("needsInput", ActivityState.NeedsInput)]
    public void Update_accepts_the_two_changeable_states(string value, ActivityState expected) =>
        Assert.Equal(expected, ((UpdateCommand)Parse("update", "--state", value)).State);

    [Fact]
    public void Update_rejects_finished_states_and_empty_updates()
    {
        Assert.Contains("state", ParseError("update", "--state", "success"));
        Assert.Contains("nothing to update", ParseError("update"));
        Assert.Contains("options only", ParseError("update", "some words"));
    }

    [Theory]
    [InlineData("101")]
    [InlineData("-101")]
    [InlineData("high")]
    public void Priority_outside_the_range_is_a_usage_error(string value) =>
        Assert.Contains("priority", ParseError("start", "Build", "--priority", value));

    [Fact]
    public void List_takes_a_json_flag()
    {
        Assert.Equal(new ListCommand(false), Parse("list"));
        Assert.Equal(new ListCommand(true), Parse("list", "--json"));
        Assert.Contains("no arguments", ParseError("list", "everything"));
    }

    [Theory]
    [InlineData("help")]
    [InlineData("--help")]
    [InlineData("-h")]
    public void Help_has_three_spellings(string arg) => Assert.IsType<HelpCommand>(Parse(arg));

    [Fact]
    public void Help_anywhere_wins() => Assert.IsType<HelpCommand>(Parse("start", "--help"));

    [Theory]
    [InlineData("version")]
    [InlineData("--version")]
    public void Version_has_two_spellings(string arg) => Assert.IsType<VersionCommand>(Parse(arg));

    [Fact]
    public void Missing_and_unknown_commands_are_usage_errors()
    {
        Assert.Contains("missing command", ParseError());
        Assert.Contains("unknown command 'stop'", ParseError("stop"));
    }

    [Theory]
    [InlineData("start")]
    [InlineData("notify")]
    public void Commands_that_need_text_reject_none(string command) =>
        Assert.Contains("missing", ParseError(command));

    [Fact]
    public void Option_mistakes_are_usage_errors()
    {
        Assert.Contains("unknown option '--colour'", ParseError("start", "Build", "--colour", "red"));
        Assert.Contains("unknown option '-s'", ParseError("start", "Build", "-s", "x"));
        Assert.Contains("unknown option '--id'", ParseError("start", "Build", "--id", "x"));
        Assert.Contains("--subtitle needs a value", ParseError("start", "Build", "--subtitle"));
        Assert.Contains("--needs-input takes no value", ParseError("start", "Build", "--needs-input=yes"));
        Assert.Contains("given twice", ParseError("start", "Build", "--priority", "1", "--priority", "2"));
    }
}
