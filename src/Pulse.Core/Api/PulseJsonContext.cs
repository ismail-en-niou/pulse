using System.Text.Json;
using System.Text.Json.Serialization;
using Pulse.Core.Activities;
using Pulse.Core.Settings;

namespace Pulse.Core.Api;

/// <summary>Source-generated JSON for the API and Pulse's files: camelCase, no reflection, trim/AOT safe.</summary>
[JsonSourceGenerationOptions(JsonSerializerDefaults.Web, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(Activity))]
[JsonSerializable(typeof(CreateActivityRequest))]
[JsonSerializable(typeof(UpdateActivityRequest))]
[JsonSerializable(typeof(FinishActivityRequest))]
[JsonSerializable(typeof(ActivityListResponse))]
[JsonSerializable(typeof(ErrorResponse))]
[JsonSerializable(typeof(ConnectionInfo))]
[JsonSerializable(typeof(PulseSettings))]
public sealed partial class PulseJsonContext : JsonSerializerContext
{
    /// <summary>Same types, indented, for files people may open.</summary>
    public static PulseJsonContext Indented { get; } = new(new JsonSerializerOptions(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true,
    });
}
