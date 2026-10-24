using System.Net.Http.Headers;
using System.Net.Http.Json;
using Pulse.Core.Api;

namespace Pulse.Cli;

internal sealed record ApiResponse(int Status, string Body)
{
    public bool IsSuccess => Status is >= 200 and < 300;
}

/// <summary>HTTP to the running app on 127.0.0.1. Short timeouts, so a hung or missing app never stalls a script.</summary>
internal sealed class PulseClient : IDisposable
{
    private readonly HttpClient _http;

    public PulseClient(ConnectionInfo connection)
    {
        // No proxy: a system or HTTP_PROXY proxy must never see loopback traffic or the token.
        var handler = new SocketsHttpHandler { UseProxy = false, ConnectTimeout = TimeSpan.FromSeconds(1) };
        _http = new HttpClient(handler)
        {
            BaseAddress = new Uri($"http://127.0.0.1:{connection.Port}"),
            Timeout = TimeSpan.FromSeconds(3),
        };
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", connection.Token);
    }

    /// <exception cref="HttpRequestException">Nothing is listening, or the connection failed.</exception>
    /// <exception cref="TaskCanceledException">Pulse didn't answer in time.</exception>
    public async Task<ApiResponse> SendAsync(ApiCall call, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(call.Method, call.Path) { Content = Content(call.Body) };
        using var response = await _http.SendAsync(request, cancellationToken);
        return new ApiResponse((int)response.StatusCode, await response.Content.ReadAsStringAsync(cancellationToken));
    }

    public void Dispose() => _http.Dispose();

    private static HttpContent? Content(object? body) => body switch
    {
        null => null,
        CreateActivityRequest create => JsonContent.Create(create, PulseJsonContext.Default.CreateActivityRequest),
        UpdateActivityRequest update => JsonContent.Create(update, PulseJsonContext.Default.UpdateActivityRequest),
        FinishActivityRequest finish => JsonContent.Create(finish, PulseJsonContext.Default.FinishActivityRequest),
        _ => throw new ArgumentException($"no JSON contract for {body.GetType().Name}", nameof(body)),
    };
}
