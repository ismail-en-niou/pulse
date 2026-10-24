namespace Pulse.App;

/// <summary>
/// Holds pulse.lock open without sharing for the life of the process, so a second Pulse can't overwrite the
/// first one's connection.json. The OS releases the lock if the process dies.
/// </summary>
internal sealed class SingleInstance : IDisposable
{
    private readonly FileStream _lock;

    private SingleInstance(FileStream lockStream)
    {
        _lock = lockStream;
    }

    /// <returns>The lock, or null when another Pulse holds it.</returns>
    public static SingleInstance? TryAcquire(string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        try
        {
            return new SingleInstance(new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None));
        }
        catch (IOException)
        {
            return null;
        }
    }

    public void Dispose() => _lock.Dispose();
}
