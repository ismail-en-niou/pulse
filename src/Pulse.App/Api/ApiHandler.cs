using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Microsoft.AspNetCore.Http;
using Pulse.Core.Activities;
using Pulse.Core.Api;
using Pulse.Core.Diagnostics;

namespace Pulse.App.Api;

/// <summary>
/// The whole local API in one request delegate: token check first, then four routes. Routing by hand keeps the
/// server free of reflection and lets tests drive it with a plain <see cref="DefaultHttpContext"/>.
/// </summary>
internal sealed class ApiHandler
{
    private const string BearerPrefix = "Bearer ";

    private readonly ActivityService _activities;
    private readonly byte[] _token;

    public ApiHandler(ActivityService activities, string token)
    {
        _activities = activities;
        _token = Encoding.UTF8.GetBytes(token);
    }

    public async Task HandleAsync(HttpContext context)
    {
        var started = Stopwatch.GetTimestamp();
        var route = "unknown";
        try
        {
            if (!HasToken(context.Request))
            {
                context.Response.Headers.WWWAuthenticate = "Bearer";
                await WriteError(context, StatusCodes.Status401Unauthorized, "unauthorized", "missing or wrong token");
                return;
            }

            route = await RouteAsync(context);
        }
        catch (BadHttpRequestException e)
        {
            // Kestrel's limits, e.g. a body over the size limit.
            await WriteError(context, e.StatusCode, "badRequest", "the request was rejected by the server's limits");
        }
        finally
        {
            // Event type, status and timing only (CLAUDE.md rule 2).
            PulseLog.Event($"api {route} {context.Response.StatusCode} in {Stopwatch.GetElapsedTime(started).TotalMilliseconds:0.#} ms");
        }
    }

    private async Task<string> RouteAsync(HttpContext context)
    {
        var request = context.Request;
        var path = request.Path.Value ?? "";
        if (path.EndsWith('/'))
            path = path[..^1];

        if (path == ApiRoutes.Activities)
        {
            if (HttpMethods.IsGet(request.Method))
            {
                await WriteJson(context, StatusCodes.Status200OK, new ActivityListResponse(_activities.Snapshot()), PulseJsonContext.Default.ActivityListResponse);
                return "list";
            }

            if (HttpMethods.IsPost(request.Method))
            {
                await CreateAsync(context);
                return "create";
            }

            await MethodNotAllowed(context, "GET, POST");
            return "activities";
        }

        if (!path.StartsWith(ApiRoutes.Activities + "/", StringComparison.Ordinal))
        {
            await WriteError(context, StatusCodes.Status404NotFound, "notFound", "no such endpoint");
            return "unknown";
        }

        // "{id}" or "{id}/finish"; ids never contain a slash.
        var rest = path[(ApiRoutes.Activities.Length + 1)..].Split('/');
        var source = request.Query[ApiRoutes.SourceQuery].ToString();
        switch (rest)
        {
            case [var id] when id.Length > 0:
                if (!HttpMethods.IsPatch(request.Method))
                {
                    await MethodNotAllowed(context, "PATCH");
                    return "activity";
                }

                if (await ReadBody(context, PulseJsonContext.Default.UpdateActivityRequest) is { } update)
                    await WriteResult(context, _activities.Update(id, update, source), StatusCodes.Status200OK);
                return "update";

            case [var id, "finish"] when id.Length > 0:
                if (!HttpMethods.IsPost(request.Method))
                {
                    await MethodNotAllowed(context, "POST");
                    return "finish";
                }

                if (await ReadBody(context, PulseJsonContext.Default.FinishActivityRequest) is { } finish)
                    await WriteResult(context, _activities.Finish(id, finish, source), StatusCodes.Status200OK);
                return "finish";

            default:
                await WriteError(context, StatusCodes.Status404NotFound, "notFound", "no such endpoint");
                return "unknown";
        }
    }

    private async Task CreateAsync(HttpContext context)
    {
        if (await ReadBody(context, PulseJsonContext.Default.CreateActivityRequest) is not { } create)
            return;

        var result = _activities.Create(create);
        if (result.Activity is { } activity)
            context.Response.Headers.Location = ApiRoutes.Activity(activity.Id);
        await WriteResult(context, result, StatusCodes.Status201Created);
    }

    /// <summary>Constant-time comparison, so response timing says nothing about how much of a guess was right.</summary>
    private bool HasToken(HttpRequest request)
    {
        var header = request.Headers.Authorization.ToString();
        if (!header.StartsWith(BearerPrefix, StringComparison.OrdinalIgnoreCase))
            return false;

        var presented = Encoding.UTF8.GetBytes(header[BearerPrefix.Length..].Trim());
        return CryptographicOperations.FixedTimeEquals(presented, _token);
    }

    /// <returns>The body, or null after writing a 400 response.</returns>
    private static async Task<T?> ReadBody<T>(HttpContext context, JsonTypeInfo<T> type) where T : class
    {
        try
        {
            if (await JsonSerializer.DeserializeAsync(context.Request.Body, type, context.RequestAborted) is { } body)
                return body;
        }
        catch (JsonException)
        {
        }

        await WriteError(context, StatusCodes.Status400BadRequest, "invalid", "the body is not a valid JSON request");
        return null;
    }

    private static Task WriteResult(HttpContext context, ActivityResult result, int successStatus)
    {
        if (result.Activity is { } activity)
            return WriteJson(context, successStatus, activity, PulseJsonContext.Default.Activity);

        return result.Error switch
        {
            ActivityError.NotFound => WriteError(context, StatusCodes.Status404NotFound, "notFound", result.Message!),
            ActivityError.Finished => WriteError(context, StatusCodes.Status409Conflict, "finished", result.Message!),
            _ => WriteError(context, StatusCodes.Status400BadRequest, "invalid", result.Message!),
        };
    }

    private static Task MethodNotAllowed(HttpContext context, string allow)
    {
        context.Response.Headers.Allow = allow;
        return WriteError(context, StatusCodes.Status405MethodNotAllowed, "methodNotAllowed", $"use {allow}");
    }

    private static Task WriteError(HttpContext context, int status, string error, string message) =>
        WriteJson(context, status, new ErrorResponse(error, message), PulseJsonContext.Default.ErrorResponse);

    private static Task WriteJson<T>(HttpContext context, int status, T value, JsonTypeInfo<T> type)
    {
        context.Response.StatusCode = status;
        return context.Response.WriteAsJsonAsync(value, type, contentType: null, context.RequestAborted);
    }
}
