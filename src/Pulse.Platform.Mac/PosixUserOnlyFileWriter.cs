using System.Runtime.Versioning;
using System.Text;
using Pulse.Core.Platform;

namespace Pulse.Platform.Mac;

/// <summary>
/// Writes a file with mode 0600 in a folder created as 0700. The file is written next to the target and
/// renamed over it, so readers never see half a file. Works on macOS and other Unix systems.
/// </summary>
[UnsupportedOSPlatform("windows")]
public sealed class PosixUserOnlyFileWriter : IUserOnlyFileWriter
{
    private const UnixFileMode UserReadWrite = UnixFileMode.UserRead | UnixFileMode.UserWrite;

    public void Write(string path, string contents)
    {
        var fullPath = Path.GetFullPath(path);
        var directory = Path.GetDirectoryName(fullPath)!;
        // Leaves an existing folder's mode alone; the file's own mode is what protects it.
        Directory.CreateDirectory(directory, UserReadWrite | UnixFileMode.UserExecute);

        var temp = Path.Combine(directory, $".{Path.GetFileName(fullPath)}.{Guid.NewGuid():N}.tmp");
        try
        {
            var options = new FileStreamOptions { Mode = FileMode.CreateNew, Access = FileAccess.Write, UnixCreateMode = UserReadWrite };
            using (var stream = new FileStream(temp, options))
            {
                stream.Write(new UTF8Encoding(encoderShouldEmitUTF8Identifier: false).GetBytes(contents));
            }

            File.Move(temp, fullPath, overwrite: true);
        }
        catch
        {
            File.Delete(temp);
            throw;
        }
    }
}
