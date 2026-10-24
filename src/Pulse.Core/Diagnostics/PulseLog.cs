namespace Pulse.Core.Diagnostics;

/// <summary>
/// Event log shared by every project. Messages are event types and timings only: never tokens, prompts,
/// titles, file contents or command output (CLAUDE.md rule 2). The app decides where they go.
/// </summary>
public static class PulseLog
{
    public static Action<string>? Sink { get; set; }

    public static void Event(string message) => Sink?.Invoke(message);
}
