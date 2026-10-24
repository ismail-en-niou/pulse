using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Pulse.App;
using Pulse.App.Api;
using Pulse.Core.Activities;
using Pulse.Core.Api;
using Pulse.Core.Settings;

namespace Pulse.Tests.App;

/// <summary>The real Kestrel server and connection file, on a free port and a temporary folder.</summary>
public sealed class LocalApiTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "pulse-tests-" + Guid.NewGuid().ToString("N"));
    private readonly ActivityService _activities = new(new PulseSettings());

    private string ConnectionPath => Path.Combine(_directory, "connection.json");

    public void Dispose()
    {
        _activities.Dispose();
        if (Directory.Exists(_directory))
            Directory.Delete(_directory, recursive: true);
    }

    [Fact]
    public async Task Listens_on_IPv4_loopback_only_and_requires_the_token()
    {
        await using var server = await ApiServer.StartAsync(new ApiHandler(_activities, "secret").HandleAsync, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal("127.0.0.1", server.Address.Host);
        Assert.InRange(server.Port, 1, 65535);

        using var client = new HttpClient { BaseAddress = server.Address };
        var anonymous = await client.GetAsync(ApiRoutes.Activities, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "secret");
        var list = await client.GetAsync(ApiRoutes.Activities, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, list.StatusCode);
    }

    [Fact]
    public async Task Bodies_over_the_limit_are_rejected()
    {
        await using var server = await ApiServer.StartAsync(new ApiHandler(_activities, "secret").HandleAsync, cancellationToken: TestContext.Current.CancellationToken);
        using var client = new HttpClient { BaseAddress = server.Address };
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "secret");

        var huge = new CreateActivityRequest(new string('x', ApiServer.MaxRequestBodyBytes));
        var response = await client.PostAsJsonAsync(ApiRoutes.Activities, huge, PulseJsonContext.Default.CreateActivityRequest, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.RequestEntityTooLarge, response.StatusCode);
        Assert.Empty(_activities.Snapshot());
    }

    [Fact]
    public void Writes_the_port_while_running_and_port_0_after_with_the_same_token()
    {
        var writer = PlatformFactory.CreateFileWriter();

        int port;
        string token;
        using (var api = LocalApi.TryStart(_activities, writer, ConnectionPath))
        {
            Assert.NotNull(api);
            var running = ConnectionFile.Read(ConnectionPath)!;
            Assert.Equal(api.Port, running.Port);
            Assert.Equal(Environment.ProcessId, running.Pid);
            Assert.True(running.IsListening);
            port = running.Port;
            token = running.Token;
        }

        var stopped = ConnectionFile.Read(ConnectionPath)!;
        Assert.False(stopped.IsListening);
        Assert.Equal(token, stopped.Token);

        using var restarted = LocalApi.TryStart(_activities, writer, ConnectionPath);
        Assert.NotNull(restarted);
        Assert.Equal(token, ConnectionFile.Read(ConnectionPath)!.Token);
        Assert.True(port > 0);
    }
}
