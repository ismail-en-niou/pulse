using Pulse.Core.Activities;
using Pulse.Core.Api;
using Pulse.Core.Diagnostics;
using Pulse.Core.Platform;

namespace Pulse.App.Api;

/// <summary>
/// Runs the API for the app's lifetime and keeps connection.json in step: the port while running, port 0 after.
/// The token stays the same across restarts, so the file is all a script ever needs.
/// </summary>
internal sealed class LocalApi : IDisposable
{
    private readonly ApiServer _server;
    private readonly IUserOnlyFileWriter _writer;
    private readonly string _connectionPath;
    private readonly ConnectionInfo _info;

    private LocalApi(ApiServer server, IUserOnlyFileWriter writer, string connectionPath, ConnectionInfo info)
    {
        _server = server;
        _writer = writer;
        _connectionPath = connectionPath;
        _info = info;
    }

    public int Port => _server.Port;

    /// <returns>The running API, or null when it couldn't start; the island then runs without it.</returns>
    public static LocalApi? TryStart(ActivityService activities, IUserOnlyFileWriter writer, string connectionPath)
    {
        try
        {
            var token = ConnectionFile.LoadOrCreateToken(connectionPath);
            var handler = new ApiHandler(activities, token);
            // Off the UI thread, so nothing in Kestrel can capture the UI synchronization context.
            var server = Task.Run(() => ApiServer.StartAsync(handler.HandleAsync)).GetAwaiter().GetResult();

            var info = new ConnectionInfo(server.Port, token, Environment.ProcessId);
            ConnectionFile.Write(writer, connectionPath, info);
            PulseLog.Event($"api listening on 127.0.0.1:{server.Port}");
            return new LocalApi(server, writer, connectionPath, info);
        }
        catch (Exception e)
        {
            PulseLog.Event($"api failed to start ({e.GetType().Name})");
            return null;
        }
    }

    public void Dispose()
    {
        try
        {
            Task.Run(() => _server.DisposeAsync().AsTask()).GetAwaiter().GetResult();
        }
        finally
        {
            ConnectionFile.Write(_writer, _connectionPath, _info with { Port = 0, Pid = 0 });
            PulseLog.Event("api stopped");
        }
    }
}
