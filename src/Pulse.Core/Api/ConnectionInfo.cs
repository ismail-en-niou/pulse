using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Pulse.Core.Api;

/// <summary>
/// Contents of connection.json: where the running app listens and the per-install token every request must carry.
/// The file is readable by the current user only. <see cref="Port"/> is 0 while Pulse isn't running.
/// </summary>
public sealed record ConnectionInfo(int Port, string Token, int Pid)
{
    [JsonIgnore]
    public bool IsListening => Port is > 0 and <= 65535;

    /// <summary>256 random bits, base64url.</summary>
    public static string NewToken() => Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(32));

    public static ConnectionInfo? TryParse(string json)
    {
        try
        {
            var info = JsonSerializer.Deserialize(json, PulseJsonContext.Indented.ConnectionInfo);
            return info is { Token.Length: > 0 } ? info : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public string ToJson() => JsonSerializer.Serialize(this, PulseJsonContext.Indented.ConnectionInfo);
}
