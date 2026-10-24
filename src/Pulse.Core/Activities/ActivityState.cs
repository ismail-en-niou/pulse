using System.Text.Json.Serialization;

namespace Pulse.Core.Activities;

[JsonConverter(typeof(JsonStringEnumConverter<ActivityState>))]
public enum ActivityState
{
    [JsonStringEnumMemberName("running")]
    Running,

    [JsonStringEnumMemberName("needsInput")]
    NeedsInput,

    [JsonStringEnumMemberName("success")]
    Success,

    [JsonStringEnumMemberName("failure")]
    Failure,

    /// <summary>A one-off notice (<c>pulse notify</c>). Finished as soon as it is created.</summary>
    [JsonStringEnumMemberName("info")]
    Info,
}

public static class ActivityStateExtensions
{
    /// <summary>Finished activities can't change any more and fade out after the configured time.</summary>
    public static bool IsFinished(this ActivityState state) =>
        state is ActivityState.Success or ActivityState.Failure or ActivityState.Info;

    /// <summary>How urgently a state needs the user's eyes; higher wins in the island.</summary>
    public static int Urgency(this ActivityState state) => state switch
    {
        ActivityState.NeedsInput => 4,
        ActivityState.Failure => 3,
        ActivityState.Running => 2,
        ActivityState.Success => 1,
        _ => 0,
    };
}
