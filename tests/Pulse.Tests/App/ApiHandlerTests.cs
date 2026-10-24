using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Time.Testing;
using Pulse.App.Api;
using Pulse.Core.Activities;
using Pulse.Core.Api;
using Pulse.Core.Settings;

namespace Pulse.Tests.App;

public sealed class ApiHandlerTests : IDisposable
{
    private const string Token = "test-token-0123456789abcdefghijklmnopqrstuv";

    private readonly FakeTimeProvider _time = new(new DateTimeOffset(2026, 10, 24, 12, 0, 0, TimeSpan.Zero));
    private readonly ActivityService _activities;
    private readonly ApiHandler _handler;

    public ApiHandlerTests()
    {
        _activities = new ActivityService(new PulseSettings(), _time);
        _handler = new ApiHandler(_activities, Token);
    }

    public void Dispose() => _activities.Dispose();

    private sealed record Response(int Status, string Body, IHeaderDictionary Headers)
    {
        public T Read<T>(System.Text.Json.Serialization.Metadata.JsonTypeInfo<T> type) => JsonSerializer.Deserialize(Body, type)!;

        public string Error => Read(PulseJsonContext.Default.ErrorResponse).Error;
    }

    private async Task<Response> Send(string method, string pathAndQuery, string? body = null, string? authorization = "Bearer " + Token)
    {
        var context = new DefaultHttpContext();
        var queryStart = pathAndQuery.IndexOf('?');
        context.Request.Method = method;
        context.Request.Path = queryStart < 0 ? pathAndQuery : pathAndQuery[..queryStart];
        context.Request.QueryString = queryStart < 0 ? QueryString.Empty : new QueryString(pathAndQuery[queryStart..]);
        if (authorization is not null)
            context.Request.Headers.Authorization = authorization;
        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(body ?? ""));
        var responseBody = new MemoryStream();
        context.Response.Body = responseBody;

        await _handler.HandleAsync(context);

        return new Response(context.Response.StatusCode, Encoding.UTF8.GetString(responseBody.ToArray()), context.Response.Headers);
    }

    private async Task<Activity> Create(string title)
    {
        var response = await Send("POST", "/v1/activities", $$"""{ "title": "{{title}}", "source": "cli" }""");
        Assert.Equal(201, response.Status);
        return response.Read(PulseJsonContext.Default.Activity);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Bearer")]
    [InlineData("Bearer wrong-token")]
    [InlineData("Basic " + Token)]
    [InlineData("Bearer " + Token + "x")]
    public async Task Requests_without_the_right_token_are_rejected_before_anything_else(string? authorization)
    {
        var response = await Send("POST", "/v1/activities", """{ "title": "sneaky" }""", authorization);

        Assert.Equal(401, response.Status);
        Assert.Equal("unauthorized", response.Error);
        Assert.Equal("Bearer", response.Headers.WWWAuthenticate.ToString());
        Assert.Empty(_activities.Snapshot());
    }

    [Fact]
    public async Task Unknown_paths_also_need_the_token()
    {
        Assert.Equal(401, (await Send("GET", "/nope", authorization: null)).Status);
        Assert.Equal(404, (await Send("GET", "/nope")).Status);
    }

    [Fact]
    public async Task Create_returns_201_with_the_new_activity_and_its_location()
    {
        var response = await Send("POST", "/v1/activities", """{ "title": "Training model", "progress": 5 }""");

        Assert.Equal(201, response.Status);
        Assert.StartsWith("application/json", response.Headers.ContentType.ToString());
        var activity = response.Read(PulseJsonContext.Default.Activity);
        Assert.Equal("Training model", activity.Title);
        Assert.Equal(5, activity.Progress);
        Assert.Equal(ActivityState.Running, activity.State);
        Assert.Equal($"/v1/activities/{activity.Id}", response.Headers.Location.ToString());
        Assert.Single(_activities.Snapshot());
    }

    [Fact]
    public async Task List_returns_activities_in_display_order()
    {
        await Create("first");
        await Create("second");

        var response = await Send("GET", "/v1/activities");

        Assert.Equal(200, response.Status);
        Assert.Equal(["second", "first"], response.Read(PulseJsonContext.Default.ActivityListResponse).Activities.Select(a => a.Title));
    }

    [Fact]
    public async Task Update_by_id_changes_the_activity()
    {
        var activity = await Create("job");

        var response = await Send("PATCH", $"/v1/activities/{activity.Id}", """{ "progress": 40, "subtitle": "epoch 4" }""");

        Assert.Equal(200, response.Status);
        var updated = response.Read(PulseJsonContext.Default.Activity);
        Assert.Equal(40, updated.Progress);
        Assert.Equal("epoch 4", updated.Subtitle);
    }

    [Fact]
    public async Task Latest_resolves_within_the_source_from_the_query()
    {
        var job = await Create("job");
        _activities.Create(new CreateActivityRequest("other", Source: "backup"));

        var progress = await Send("PATCH", "/v1/activities/latest?source=cli", """{ "progress": 70 }""");
        var finish = await Send("POST", "/v1/activities/latest/finish?source=cli", """{ "state": "success" }""");

        Assert.Equal(job.Id, progress.Read(PulseJsonContext.Default.Activity).Id);
        Assert.Equal(ActivityState.Success, finish.Read(PulseJsonContext.Default.Activity).State);
    }

    [Fact]
    public async Task Finishing_twice_is_a_conflict()
    {
        var activity = await Create("job");
        Assert.Equal(200, (await Send("POST", $"/v1/activities/{activity.Id}/finish", """{ "state": "failure", "subtitle": "Tests failed" }""")).Status);

        var again = await Send("POST", $"/v1/activities/{activity.Id}/finish", """{ "state": "success" }""");

        Assert.Equal(409, again.Status);
        Assert.Equal("finished", again.Error);
    }

    [Fact]
    public async Task Unknown_ids_are_404()
    {
        var response = await Send("PATCH", "/v1/activities/doesnotexist", """{ "progress": 1 }""");

        Assert.Equal(404, response.Status);
        Assert.Equal("notFound", response.Error);
    }

    [Theory]
    [InlineData("")]
    [InlineData("null")]
    [InlineData("{ not json")]
    [InlineData("""{ "title": 5 }""")]
    [InlineData("""{ "title": "ok", "state": "exploded" }""")]
    [InlineData("""{ "subtitle": "no title" }""")]
    [InlineData("""{ "title": "ok", "progress": 101 }""")]
    public async Task Bad_create_bodies_are_400(string body)
    {
        var response = await Send("POST", "/v1/activities", body);

        Assert.Equal(400, response.Status);
        Assert.Equal("invalid", response.Error);
        Assert.Empty(_activities.Snapshot());
    }

    [Theory]
    [InlineData("DELETE", "/v1/activities", "GET, POST")]
    [InlineData("GET", "/v1/activities/abc", "PATCH")]
    [InlineData("PATCH", "/v1/activities/abc/finish", "POST")]
    public async Task Wrong_methods_are_405_with_the_allowed_ones(string method, string path, string allow)
    {
        var response = await Send(method, path, "{}");

        Assert.Equal(405, response.Status);
        Assert.Equal(allow, response.Headers.Allow.ToString());
    }

    [Theory]
    [InlineData("/v1")]
    [InlineData("/v1/activities/abc/finish/now")]
    [InlineData("/v1/activities//finish")]
    [InlineData("/v2/activities")]
    public async Task Other_paths_are_404(string path) =>
        Assert.Equal(404, (await Send("POST", path, "{}")).Status);
}
