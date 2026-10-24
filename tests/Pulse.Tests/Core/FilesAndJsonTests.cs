using System.Text.Json;
using Pulse.Core.Activities;
using Pulse.Core.Api;
using Pulse.Core.Settings;

namespace Pulse.Tests.Core;

public sealed class FilesAndJsonTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "pulse-tests-" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_directory))
            Directory.Delete(_directory, recursive: true);
    }

    [Fact]
    public void Settings_are_created_with_defaults_on_first_run()
    {
        var path = Path.Combine(_directory, "settings.json");

        var settings = PulseSettings.LoadOrCreate(path);

        Assert.Equal(PulseSettings.DefaultFinishedActivitySeconds, settings.FinishedActivitySeconds);
        Assert.Contains("\"finishedActivitySeconds\": 10", File.ReadAllText(path));
        Assert.DoesNotContain("Lifetime", File.ReadAllText(path));
    }

    [Fact]
    public void Settings_are_read_back()
    {
        Directory.CreateDirectory(_directory);
        var path = Path.Combine(_directory, "settings.json");
        File.WriteAllText(path, """{ "finishedActivitySeconds": 30 }""");

        Assert.Equal(TimeSpan.FromSeconds(30), PulseSettings.LoadOrCreate(path).FinishedActivityLifetime);
    }

    [Fact]
    public void A_broken_settings_file_is_kept_and_defaults_are_used()
    {
        Directory.CreateDirectory(_directory);
        var path = Path.Combine(_directory, "settings.json");
        File.WriteAllText(path, "{ not json");
        string? error = null;

        var settings = PulseSettings.LoadOrCreate(path, e => error = e);

        Assert.Equal(PulseSettings.DefaultFinishedActivitySeconds, settings.FinishedActivitySeconds);
        Assert.Equal("{ not json", File.ReadAllText(path));
        Assert.NotNull(error);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(-5, 1)]
    [InlineData(100_000, 3600)]
    public void Finished_lifetime_is_clamped(int seconds, int expectedSeconds) =>
        Assert.Equal(TimeSpan.FromSeconds(expectedSeconds), new PulseSettings { FinishedActivitySeconds = seconds }.FinishedActivityLifetime);

    [Fact]
    public void Connection_info_round_trips_with_camel_case_names()
    {
        var info = new ConnectionInfo(52731, ConnectionInfo.NewToken(), 1234);

        var json = info.ToJson();

        Assert.Contains("\"port\": 52731", json);
        Assert.DoesNotContain("isListening", json);
        Assert.Equal(info, ConnectionInfo.TryParse(json));
    }

    [Theory]
    [InlineData("")]
    [InlineData("not json")]
    [InlineData("""{ "port": 1 }""")]
    [InlineData("""{ "port": 1, "token": "" }""")]
    public void Unusable_connection_files_parse_to_null(string json) => Assert.Null(ConnectionInfo.TryParse(json));

    [Fact]
    public void A_stopped_app_leaves_port_zero()
    {
        Assert.False(new ConnectionInfo(0, "t", 0).IsListening);
        Assert.True(new ConnectionInfo(52731, "t", 1).IsListening);
    }

    [Fact]
    public void Tokens_are_long_random_and_url_safe()
    {
        var a = ConnectionInfo.NewToken();
        var b = ConnectionInfo.NewToken();

        Assert.NotEqual(a, b);
        Assert.Equal(43, a.Length); // 32 bytes, base64url without padding
        Assert.All(a, c => Assert.True(char.IsAsciiLetterOrDigit(c) || c is '-' or '_'));
    }

    [Fact]
    public void Activity_ids_use_the_readable_alphabet()
    {
        var id = ActivityIds.New();

        Assert.Equal(ActivityIds.Length, id.Length);
        Assert.All(id, c => Assert.Contains(c, "0123456789abcdefghjkmnpqrstvwxyz"));
    }

    [Fact]
    public void Activities_serialize_states_in_camel_case_and_skip_nulls()
    {
        var activity = new Activity { Id = "a1", Source = "cli", Title = "Waiting", State = ActivityState.NeedsInput };

        var json = JsonSerializer.Serialize(activity, PulseJsonContext.Default.Activity);

        Assert.Contains("\"state\":\"needsInput\"", json);
        Assert.Contains("\"title\":\"Waiting\"", json);
        Assert.DoesNotContain("subtitle", json);
    }

    [Fact]
    public void Requests_deserialize_from_api_json()
    {
        var request = JsonSerializer.Deserialize(
            """{ "title": "Train", "state": "needsInput", "progress": 5, "source": "cli" }""",
            PulseJsonContext.Default.CreateActivityRequest)!;

        Assert.Equal(new CreateActivityRequest("Train", State: ActivityState.NeedsInput, Progress: 5, Source: "cli"), request);
    }
}
