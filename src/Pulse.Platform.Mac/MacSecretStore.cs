using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text;
using Pulse.Core.Platform;

namespace Pulse.Platform.Mac;

/// <summary>
/// Secrets as generic passwords in the user's login keychain (service "Pulse", account = key), through
/// Security.framework's SecItem API. The secret is stored as UTF-8.
/// </summary>
/// <remarks>
/// The keychain ties each item to the code signature of the app that created it. Unsigned development builds
/// change with every rebuild, so macOS may ask once per build whether Pulse may use the item.
/// </remarks>
[SupportedOSPlatform("macos")]
public sealed unsafe partial class MacSecretStore : ISecretStore
{
    private const string SecurityLib = "/System/Library/Frameworks/Security.framework/Security";
    private const string CoreFoundationLib = "/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation";
    private const int ErrSecSuccess = 0;
    private const int ErrSecItemNotFound = -25300;
    private const uint CFStringEncodingUTF8 = 0x08000100;

    private readonly string _service;

    /// <param name="service">Keychain service name; tests use their own so they never touch real secrets.</param>
    public MacSecretStore(string service = "Pulse")
    {
        _service = service;
    }

    public string? Get(string key)
    {
        var query = ItemQuery(key, (Constants.ReturnData, Constants.True), (Constants.MatchLimit, Constants.MatchLimitOne));
        try
        {
            var status = SecItemCopyMatching(query, out var data);
            if (status == ErrSecItemNotFound)
                return null;
            Check(status, "read");

            try
            {
                return Encoding.UTF8.GetString(CFDataGetBytePtr(data), (int)CFDataGetLength(data));
            }
            finally
            {
                CFRelease(data);
            }
        }
        finally
        {
            CFRelease(query);
        }
    }

    public void Set(string key, string secret)
    {
        var bytes = Encoding.UTF8.GetBytes(secret);
        nint data;
        fixed (byte* pointer = bytes)
            data = CFDataCreate(0, pointer, bytes.Length);
        CryptographicOperations.ZeroMemory(bytes);

        var query = ItemQuery(key);
        var update = Dictionary((Constants.ValueData, data));
        try
        {
            var status = SecItemUpdate(query, update);
            if (status == ErrSecItemNotFound)
            {
                var item = ItemQuery(key, (Constants.ValueData, data));
                try
                {
                    status = SecItemAdd(item, 0);
                }
                finally
                {
                    CFRelease(item);
                }
            }

            Check(status, "write");
        }
        finally
        {
            CFRelease(update);
            CFRelease(query);
            CFRelease(data);
        }
    }

    public void Delete(string key)
    {
        var query = ItemQuery(key);
        try
        {
            var status = SecItemDelete(query);
            if (status != ErrSecItemNotFound)
                Check(status, "delete");
        }
        finally
        {
            CFRelease(query);
        }
    }

    /// <summary>A generic-password query for this service and <paramref name="key"/>, plus extra pairs. Release it.</summary>
    private nint ItemQuery(string key, params (nint Key, nint Value)[] extra)
    {
        var service = CFString(_service);
        var account = CFString(key);
        try
        {
            return Dictionary([(Constants.Class, Constants.ClassGenericPassword), (Constants.AttrService, service), (Constants.AttrAccount, account), .. extra]);
        }
        finally
        {
            // The dictionary retains its keys and values.
            CFRelease(service);
            CFRelease(account);
        }
    }

    private static nint Dictionary(params (nint Key, nint Value)[] pairs)
    {
        var keys = stackalloc nint[pairs.Length];
        var values = stackalloc nint[pairs.Length];
        for (var i = 0; i < pairs.Length; i++)
        {
            keys[i] = pairs[i].Key;
            values[i] = pairs[i].Value;
        }

        return CFDictionaryCreate(0, keys, values, pairs.Length, Constants.KeyCallBacks, Constants.ValueCallBacks);
    }

    private static nint CFString(string value) => CFStringCreateWithCString(0, value, CFStringEncodingUTF8);

    private static void Check(int status, string operation)
    {
        if (status != ErrSecSuccess)
            throw new InvalidOperationException($"keychain {operation} failed (OSStatus {status})");
    }

    /// <summary>Framework constants, loaded on first use (only ever on macOS).</summary>
    private static class Constants
    {
        private static readonly nint Security = NativeLibrary.Load(SecurityLib);
        private static readonly nint CoreFoundation = NativeLibrary.Load(CoreFoundationLib);

        public static readonly nint Class = Value(Security, "kSecClass");
        public static readonly nint ClassGenericPassword = Value(Security, "kSecClassGenericPassword");
        public static readonly nint AttrService = Value(Security, "kSecAttrService");
        public static readonly nint AttrAccount = Value(Security, "kSecAttrAccount");
        public static readonly nint ValueData = Value(Security, "kSecValueData");
        public static readonly nint ReturnData = Value(Security, "kSecReturnData");
        public static readonly nint MatchLimit = Value(Security, "kSecMatchLimit");
        public static readonly nint MatchLimitOne = Value(Security, "kSecMatchLimitOne");
        public static readonly nint True = Value(CoreFoundation, "kCFBooleanTrue");

        // These two are structs, passed by address.
        public static readonly nint KeyCallBacks = NativeLibrary.GetExport(CoreFoundation, "kCFTypeDictionaryKeyCallBacks");
        public static readonly nint ValueCallBacks = NativeLibrary.GetExport(CoreFoundation, "kCFTypeDictionaryValueCallBacks");

        /// <summary>The exported symbol is a variable holding the CF object.</summary>
        private static nint Value(nint library, string symbol) => *(nint*)NativeLibrary.GetExport(library, symbol);
    }

    [LibraryImport(CoreFoundationLib)]
    private static partial nint CFDictionaryCreate(nint allocator, nint* keys, nint* values, nint count, nint keyCallBacks, nint valueCallBacks);

    [LibraryImport(CoreFoundationLib, StringMarshalling = StringMarshalling.Utf8)]
    private static partial nint CFStringCreateWithCString(nint allocator, string value, uint encoding);

    [LibraryImport(CoreFoundationLib)]
    private static partial nint CFDataCreate(nint allocator, byte* bytes, nint length);

    [LibraryImport(CoreFoundationLib)]
    private static partial nint CFDataGetLength(nint data);

    [LibraryImport(CoreFoundationLib)]
    private static partial byte* CFDataGetBytePtr(nint data);

    [LibraryImport(CoreFoundationLib)]
    private static partial void CFRelease(nint value);

    [LibraryImport(SecurityLib)]
    private static partial int SecItemCopyMatching(nint query, out nint result);

    [LibraryImport(SecurityLib)]
    private static partial int SecItemAdd(nint attributes, nint result);

    [LibraryImport(SecurityLib)]
    private static partial int SecItemUpdate(nint query, nint attributesToUpdate);

    [LibraryImport(SecurityLib)]
    private static partial int SecItemDelete(nint query);
}
