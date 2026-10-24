using Pulse.Core.Platform;

namespace Pulse.Core.Api;

/// <summary>Reads and writes connection.json, which tells the CLI where the app listens and which token to send.</summary>
public static class ConnectionFile
{
    /// <returns>The connection info, or null when the file is missing, unreadable or broken.</returns>
    public static ConnectionInfo? Read(string path)
    {
        try
        {
            // Share delete and write, so the app can replace the file while a CLI is reading it.
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            using var reader = new StreamReader(stream);
            return ConnectionInfo.TryParse(reader.ReadToEnd());
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    /// <summary>The per-install token: the one already in the file when it looks like ours, otherwise a new one.</summary>
    public static string LoadOrCreateToken(string path) =>
        Read(path)?.Token is { } token && IsWellFormed(token) ? token : ConnectionInfo.NewToken();

    public static void Write(IUserOnlyFileWriter writer, string path, ConnectionInfo info) => writer.Write(path, info.ToJson());

    /// <summary>Same shape as <see cref="ConnectionInfo.NewToken"/>: 43 base64url characters.</summary>
    private static bool IsWellFormed(string token) =>
        token.Length == 43 && token.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_');
}
