using Pulse.App.Api;
using Pulse.Cli;
using Pulse.Core.Activities;
using Pulse.Core.Api;
using Pulse.Core.Settings;

namespace Pulse.Tests.Cli;

/// <summary>The CLI against the real API server on a free port. Reading connection.json is covered by ConnectionFileTests.</summary>
public sealed class CliEndToEndTests : IAsyncLifetime
{
    private readonly ActivityService _activities = new(new PulseSettings());
    private readonly string _token = ConnectionInfo.NewToken();
    private ApiServer? _server;
    private ConnectionInfo? _connection;

    public async ValueTask InitializeAsync()
    {
        _server = await ApiServer.StartAsync(new ApiHandler(_activities, _token).HandleAsync);
        _connection = new ConnectionInfo(_server.Port, _token, Environment.ProcessId);
    }

    public async ValueTask DisposeAsync()
    {
        if (_server is not null)
            await _server.DisposeAsync();
        _activities.Dispose();
    }

    private async Task<(int Code, string Out, string Err)> Run(params string[] args)
    {
        var stdout = new StringWriter();
        var stderr = new StringWriter();
        var code = await CliRunner.RunAsync(args, stdout, stderr, () => _connection);
        return (code, stdout.ToString().Trim(), stderr.ToString().Trim());
    }

    [Fact]
    public async Task Start_progress_done_drives_one_activity_from_start_to_success()
    {
        var start = await Run("start", "Training model");
        Assert.Equal(ExitCodes.Ok, start.Code);
        var id = start.Out;
        Assert.Equal(ActivityIds.Length, id.Length);

        Assert.Equal((ExitCodes.Ok, "", ""), await Run("progress", "40", "epoch 4"));
        var running = Assert.Single(_activities.Snapshot());
        Assert.Equal((id, 40, "epoch 4"), (running.Id, running.Progress, running.Subtitle));

        Assert.Equal(ExitCodes.Ok, (await Run("done", "--id", id)).Code);
        var done = Assert.Single(_activities.Snapshot());
        Assert.Equal(ActivityState.Success, done.State);
        Assert.Equal(100, done.Progress);
    }

    [Fact]
    public async Task Fail_sets_the_message_and_notify_prints_an_id()
    {
        await Run("start", "Tests");
        Assert.Equal(ExitCodes.Ok, (await Run("fail", "Tests failed")).Code);
        var notify = await Run("notify", "Backup finished");

        Assert.Equal(ExitCodes.Ok, notify.Code);
        Assert.Equal(ActivityState.Info, _activities.Snapshot().Single(a => a.Id == notify.Out).State);
        Assert.Equal("Tests failed", _activities.Snapshot().Single(a => a.State == ActivityState.Failure).Subtitle);
    }

    [Fact]
    public async Task List_prints_a_row_per_activity_or_json()
    {
        Assert.Equal("No activities.", (await Run("list")).Out);
        var id = (await Run("start", "Training model", "--progress", "40", "--subtitle", "epoch 4")).Out;

        var table = await Run("list");
        var json = await Run("list", "--json");

        Assert.Equal($"{id}  running       40%  Training model · epoch 4", table.Out);
        Assert.Contains($"\"id\":\"{id}\"", json.Out);
    }

    [Fact]
    public async Task Rejections_print_the_reason_and_exit_1()
    {
        var result = await Run("done");

        Assert.Equal(ExitCodes.Rejected, result.Code);
        Assert.Equal("pulse: no unfinished activity from this source", result.Err);
    }

    [Fact]
    public async Task A_stale_token_is_reported_as_such()
    {
        _connection = _connection! with { Token = ConnectionInfo.NewToken() };

        var result = await Run("list");

        Assert.Equal(ExitCodes.Rejected, result.Code);
        Assert.Contains("rejected the token", result.Err);
    }

    [Fact]
    public async Task Usage_errors_exit_2_without_calling_pulse()
    {
        _connection = null;

        var result = await Run("progress", "lots");

        Assert.Equal(ExitCodes.Usage, result.Code);
        Assert.Contains("progress must be", result.Err);
    }

    [Fact]
    public async Task Help_and_version_need_no_running_app()
    {
        _connection = null;

        Assert.Contains("pulse start <title>", (await Run("help")).Out);
        Assert.StartsWith("pulse 0.", (await Run("--version")).Out);
    }

    [Fact]
    public async Task No_connection_port_0_and_a_closed_port_all_mean_not_running()
    {
        var port = _server!.Port;
        _connection = null;
        Assert.Equal(ExitCodes.NotRunning, (await Run("list")).Code);

        _connection = new ConnectionInfo(0, _token, 0);
        Assert.Equal(ExitCodes.NotRunning, (await Run("list")).Code);

        await _server.DisposeAsync();
        _server = null;
        _connection = new ConnectionInfo(port, _token, 1);
        var closed = await Run("start", "Build");
        Assert.Equal(ExitCodes.NotRunning, closed.Code);
        Assert.Contains("isn't running", closed.Err);
    }
}
