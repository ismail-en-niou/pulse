using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;

namespace Pulse.App.Api;

/// <summary>
/// Kestrel on 127.0.0.1 only (CLAUDE.md rule 1), on a port the OS picks. Built from an empty host: no
/// configuration sources, so no environment variable or settings file can add an endpoint or turn on logging.
/// </summary>
internal sealed class ApiServer : IAsyncDisposable
{
    public const int MaxRequestBodyBytes = 16 * 1024;

    private static readonly TimeSpan StopTimeout = TimeSpan.FromSeconds(2);

    private readonly WebApplication _app;

    private ApiServer(WebApplication app, Uri address)
    {
        _app = app;
        Address = address;
    }

    /// <summary>For example http://127.0.0.1:52731/.</summary>
    public Uri Address { get; }

    public int Port => Address.Port;

    /// <param name="port">0 lets the OS pick a free port.</param>
    public static async Task<ApiServer> StartAsync(RequestDelegate handler, int port = 0, CancellationToken cancellationToken = default)
    {
        var builder = WebApplication.CreateEmptyBuilder(new WebApplicationOptions());
        builder.WebHost.UseKestrelCore().ConfigureKestrel(kestrel =>
        {
            kestrel.Listen(IPAddress.Loopback, port);
            kestrel.AddServerHeader = false;
            kestrel.Limits.MaxRequestBodySize = MaxRequestBodyBytes;
            kestrel.Limits.MaxRequestHeadersTotalSize = 16 * 1024;
        });

        var app = builder.Build();
        app.Run(handler);
        await app.StartAsync(cancellationToken);

        var address = app.Services.GetRequiredService<IServer>().Features.GetRequiredFeature<IServerAddressesFeature>().Addresses.Single();
        return new ApiServer(app, new Uri(address));
    }

    public async ValueTask DisposeAsync()
    {
        using (var timeout = new CancellationTokenSource(StopTimeout))
            await _app.StopAsync(timeout.Token);
        await _app.DisposeAsync();
    }
}
