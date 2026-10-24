using System.Runtime.Versioning;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using Pulse.Core.Platform;

namespace Pulse.Platform.Windows;

/// <summary>
/// Writes a file whose access list grants the current user and nobody else, with inherited entries removed.
/// The file is written next to the target and moved over it, so readers never see half a file.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class WindowsUserOnlyFileWriter : IUserOnlyFileWriter
{
    public void Write(string path, string contents)
    {
        var fullPath = Path.GetFullPath(path);
        var directory = Path.GetDirectoryName(fullPath)!;
        Directory.CreateDirectory(directory);

        var temp = Path.Combine(directory, $".{Path.GetFileName(fullPath)}.{Guid.NewGuid():N}.tmp");
        try
        {
            using (var stream = new FileInfo(temp).Create(
                       FileMode.CreateNew, FileSystemRights.FullControl, FileShare.None, 4096, FileOptions.None, UserOnlySecurity()))
            {
                stream.Write(new UTF8Encoding(encoderShouldEmitUTF8Identifier: false).GetBytes(contents));
            }

            // A move within the folder keeps the file's own access list.
            File.Move(temp, fullPath, overwrite: true);
        }
        catch
        {
            File.Delete(temp);
            throw;
        }
    }

    private static FileSecurity UserOnlySecurity()
    {
        var user = WindowsIdentity.GetCurrent().User ?? throw new InvalidOperationException("no user SID for the current process");
        var security = new FileSecurity();
        security.SetAccessRuleProtection(isProtected: true, preserveInheritance: false);
        security.AddAccessRule(new FileSystemAccessRule(user, FileSystemRights.FullControl, AccessControlType.Allow));
        return security;
    }
}
