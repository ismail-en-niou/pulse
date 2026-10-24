namespace Pulse.Spike;

/// <summary>Console event log. Event types and timings only (CLAUDE.md rule 2).</summary>
internal static class Log
{
    public static void Event(string message) => Console.WriteLine($"{DateTime.Now:HH:mm:ss.fff} {message}");
}
