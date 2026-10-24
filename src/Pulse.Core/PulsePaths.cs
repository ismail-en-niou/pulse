namespace Pulse.Core;

/// <summary>Where Pulse keeps its files: settings, the API connection file and the single-instance lock.</summary>
public static class PulsePaths
{
    /// <summary>Overrides the data folder, e.g. for tests or a second development instance.</summary>
    public const string HomeVariable = "PULSE_HOME";

    /// <summary>%LOCALAPPDATA%\Pulse on Windows, ~/Library/Application Support/Pulse on macOS.</summary>
    public static string Home => ResolveHome(Environment.GetEnvironmentVariable(HomeVariable));

    public static string Settings => Path.Combine(Home, "settings.json");

    public static string Connection => Path.Combine(Home, "connection.json");

    public static string Lock => Path.Combine(Home, "pulse.lock");

    public static string ResolveHome(string? overrideDirectory)
    {
        if (!string.IsNullOrWhiteSpace(overrideDirectory))
            return Path.GetFullPath(overrideDirectory);

        if (OperatingSystem.IsMacOS())
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            return Path.Combine(home, "Library", "Application Support", "Pulse");
        }

        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Pulse");
    }
}
