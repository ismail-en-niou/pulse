using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text;
using Pulse.Core.Platform;

namespace Pulse.Platform.Windows;

/// <summary>
/// Secrets as generic credentials in Windows Credential Manager, named "Pulse:&lt;key&gt;" and kept on this machine
/// (not roamed). The secret is stored as UTF-8.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed unsafe class WindowsSecretStore : ISecretStore
{
    private readonly string _prefix;

    /// <param name="prefix">Target name prefix; tests use their own so they never touch real secrets.</param>
    public WindowsSecretStore(string prefix = "Pulse")
    {
        _prefix = prefix;
    }

    public string? Get(string key)
    {
        if (!Win32.CredRead(Target(key), Win32.CRED_TYPE_GENERIC, 0, out var buffer))
        {
            var error = Marshal.GetLastPInvokeError();
            return error == Win32.ERROR_NOT_FOUND ? null : throw Failure("read", error);
        }

        try
        {
            var credential = (Win32.CREDENTIAL*)buffer;
            return Encoding.UTF8.GetString((byte*)credential->CredentialBlob, (int)credential->CredentialBlobSize);
        }
        finally
        {
            Win32.CredFree(buffer);
        }
    }

    public void Set(string key, string secret)
    {
        var blob = Encoding.UTF8.GetBytes(secret);
        var target = Marshal.StringToHGlobalUni(Target(key));
        var userName = Marshal.StringToHGlobalUni(key);
        try
        {
            if (blob.Length > Win32.CRED_MAX_CREDENTIAL_BLOB_SIZE)
                throw new ArgumentException($"secrets are limited to {Win32.CRED_MAX_CREDENTIAL_BLOB_SIZE} bytes", nameof(secret));

            fixed (byte* blobPointer = blob)
            {
                var credential = new Win32.CREDENTIAL
                {
                    Type = Win32.CRED_TYPE_GENERIC,
                    TargetName = target,
                    UserName = userName,
                    CredentialBlobSize = (uint)blob.Length,
                    CredentialBlob = (nint)blobPointer,
                    Persist = Win32.CRED_PERSIST_LOCAL_MACHINE,
                };

                if (!Win32.CredWrite(credential, 0))
                    throw Failure("write", Marshal.GetLastPInvokeError());
            }
        }
        finally
        {
            CryptographicOperations.ZeroMemory(blob);
            Marshal.FreeHGlobal(target);
            Marshal.FreeHGlobal(userName);
        }
    }

    public void Delete(string key)
    {
        if (!Win32.CredDelete(Target(key), Win32.CRED_TYPE_GENERIC, 0))
        {
            var error = Marshal.GetLastPInvokeError();
            if (error != Win32.ERROR_NOT_FOUND)
                throw Failure("delete", error);
        }
    }

    private string Target(string key) => $"{_prefix}:{key}";

    private static InvalidOperationException Failure(string operation, int error) =>
        new($"Credential Manager {operation} failed (error {error})");
}
