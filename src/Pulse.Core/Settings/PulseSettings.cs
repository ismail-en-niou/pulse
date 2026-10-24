using System.Text.Json;
using System.Text.Json.Serialization;
using Pulse.Core.Api;

namespace Pulse.Core.Settings;

/// <summary>User settings from settings.json. Never holds secrets.</summary>
public sealed record PulseSettings
{
    public const int DefaultFinishedActivitySeconds = 10;

    /// <summary>How long a finished activity (success, failure, notice) stays before it fades out.</summary>
    public int FinishedActivitySeconds { get; init; } = DefaultFinishedActivitySeconds;

    [JsonIgnore]
    public TimeSpan FinishedActivityLifetime => TimeSpan.FromSeconds(Math.Clamp(FinishedActivitySeconds, 1, 3600));

    /// <summary>
    /// Reads the settings file, or writes the defaults there on first run so users can find and edit it.
    /// A broken file is left alone and the defaults are used.
    /// </summary>
    /// <param name="onError">Receives a short description of the problem; never the file's content.</param>
    public static PulseSettings LoadOrCreate(string path, Action<string>? onError = null)
    {
        try
        {
            if (!File.Exists(path))
            {
                var defaults = new PulseSettings();
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                File.WriteAllText(path, JsonSerializer.Serialize(defaults, PulseJsonContext.Indented.PulseSettings));
                return defaults;
            }

            return JsonSerializer.Deserialize(File.ReadAllText(path), PulseJsonContext.Indented.PulseSettings) ?? new PulseSettings();
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or JsonException)
        {
            onError?.Invoke($"settings unreadable ({e.GetType().Name}), using defaults");
            return new PulseSettings();
        }
    }
}
