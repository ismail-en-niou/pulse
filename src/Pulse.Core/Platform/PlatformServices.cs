namespace Pulse.Core.Platform;

/// <summary>Secrets for services the user connects (e.g. a GitHub token), kept in the OS keychain (CLAUDE.md rule 3).</summary>
public interface ISecretStore
{
    /// <returns>The secret, or null when none is stored under <paramref name="key"/>.</returns>
    string? Get(string key);

    /// <summary>Stores or replaces the secret.</summary>
    void Set(string key, string secret);

    /// <summary>Removes the secret; does nothing when there is none.</summary>
    void Delete(string key);
}

/// <summary>Starts Pulse when the user logs in.</summary>
public interface ILaunchAtLogin
{
    bool IsSupported { get; }

    bool IsEnabled { get; }

    /// <summary>Registers or unregisters the running executable.</summary>
    void SetEnabled(bool enabled);
}

/// <summary>Writes files that only the current user may read, such as connection.json with the API token.</summary>
public interface IUserOnlyFileWriter
{
    /// <summary>Creates or atomically replaces <paramref name="path"/>, readable and writable by the current user only.</summary>
    void Write(string path, string contents);
}
