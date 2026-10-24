using Pulse.Cli;
using Pulse.Core.Activities;
using Pulse.Core.Api;

namespace Pulse.Tests.Cli;

public class CliMapperTests
{
    [Fact]
    public void Start_creates_a_running_activity_and_prints_its_id()
    {
        var call = CliMapper.Map(new StartCommand("Training model", "epoch 1", 5, 2, false, "cli"));

        Assert.Equal(HttpMethod.Post, call.Method);
        Assert.Equal("/v1/activities", call.Path);
        Assert.Equal(new CreateActivityRequest("Training model", "epoch 1", null, 5, 2, "cli"), call.Body);
        Assert.Equal(CliOutput.Id, call.Output);
    }

    [Fact]
    public void Start_with_needs_input_starts_in_that_state() =>
        Assert.Equal(ActivityState.NeedsInput, ((CreateActivityRequest)CliMapper.Map(new StartCommand("Approve?", null, null, null, true, "cli")).Body!).State);

    [Fact]
    public void Progress_without_an_id_targets_the_latest_activity_of_the_source()
    {
        var call = CliMapper.Map(new ProgressCommand(40, "epoch 4", null, "cli"));

        Assert.Equal(HttpMethod.Patch, call.Method);
        Assert.Equal("/v1/activities/latest?source=cli", call.Path);
        Assert.Equal(new UpdateActivityRequest(Subtitle: "epoch 4", Progress: 40), call.Body);
        Assert.Equal(CliOutput.Nothing, call.Output);
    }

    [Fact]
    public void Progress_with_an_id_targets_that_activity() =>
        Assert.Equal("/v1/activities/abc123", CliMapper.Map(new ProgressCommand(40, null, "abc123", "cli")).Path);

    [Fact]
    public void Update_sends_only_what_was_given()
    {
        var call = CliMapper.Map(new UpdateCommand(null, "waiting for review", ActivityState.NeedsInput, null, null, "abc", "cli"));

        Assert.Equal("/v1/activities/abc", call.Path);
        Assert.Equal(new UpdateActivityRequest(Subtitle: "waiting for review", State: ActivityState.NeedsInput), call.Body);
    }

    [Fact]
    public void Done_finishes_with_success_and_fail_with_failure()
    {
        var done = CliMapper.Map(new FinishCommand(true, null, null, "cli"));
        var fail = CliMapper.Map(new FinishCommand(false, "Tests failed", "abc", "cli"));

        Assert.Equal(HttpMethod.Post, done.Method);
        Assert.Equal("/v1/activities/latest/finish?source=cli", done.Path);
        Assert.Equal(new FinishActivityRequest(ActivityState.Success), done.Body);
        Assert.Equal("/v1/activities/abc/finish", fail.Path);
        Assert.Equal(new FinishActivityRequest(ActivityState.Failure, "Tests failed"), fail.Body);
    }

    [Fact]
    public void Notify_creates_an_info_activity_and_prints_its_id()
    {
        var call = CliMapper.Map(new NotifyCommand("Backup finished", null, null, "backup"));

        Assert.Equal("/v1/activities", call.Path);
        Assert.Equal(new CreateActivityRequest("Backup finished", State: ActivityState.Info, Source: "backup"), call.Body);
        Assert.Equal(CliOutput.Id, call.Output);
    }

    [Fact]
    public void List_is_a_get_printed_as_a_table_or_json()
    {
        Assert.Equal(new ApiCall(HttpMethod.Get, "/v1/activities", null, CliOutput.Table), CliMapper.Map(new ListCommand(false)));
        Assert.Equal(CliOutput.Json, CliMapper.Map(new ListCommand(true)).Output);
    }

    [Fact]
    public void Sources_and_ids_are_escaped_in_the_url()
    {
        Assert.Equal("/v1/activities/latest/finish?source=my%20tool", CliMapper.Map(new FinishCommand(true, null, null, "my tool")).Path);
        Assert.Equal("/v1/activities/a%2Fb", CliMapper.Map(new ProgressCommand(1, null, "a/b", "cli")).Path);
    }

    [Fact]
    public void Help_and_version_have_no_api_call() =>
        Assert.Throws<ArgumentException>(() => CliMapper.Map(new HelpCommand()));
}
