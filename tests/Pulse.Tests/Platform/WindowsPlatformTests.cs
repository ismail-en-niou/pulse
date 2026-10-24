using System.Runtime.Versioning;
using System.Security.AccessControl;
using System.Security.Principal;
using Microsoft.Win32;
using Pulse.Platform.Windows;

namespace Pulse.Tests.Platform;

[SupportedOSPlatform("windows")]
public sealed class WindowsPlatformTests : IDisposable
{
    private readonly string _directory = TestEnvironment.TempDirectory();

    public void Dispose()
    {
        if (Directory.Exists(_directory))
            Directory.Delete(_directory, recursive: true);
    }

    [Fact]
    public void User_only_file_grants_the_current_user_and_nobody_else()
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), "Windows only");
        var path = Path.Combine(_directory, "connection.json");

        new WindowsUserOnlyFileWriter().Write(path, "first");
        new WindowsUserOnlyFileWriter().Write(path, "second");

        Assert.Equal("second", File.ReadAllText(path));
        Assert.Equal([path], Directory.GetFiles(_directory)); // no temp file left behind

        var security = new FileInfo(path).GetAccessControl();
        Assert.True(security.AreAccessRulesProtected);
        var rules = security.GetAccessRules(includeExplicit: true, includeInherited: true, typeof(SecurityIdentifier))
            .Cast<FileSystemAccessRule>()
            .ToList();
        var rule = Assert.Single(rules);
        Assert.Equal(WindowsIdentity.GetCurrent().User, rule.IdentityReference);
        Assert.Equal(AccessControlType.Allow, rule.AccessControlType);
    }

    [Fact]
    public void Secret_store_round_trips_through_Credential_Manager()
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), "Windows only");
        Assert.SkipWhen(TestEnvironment.IsCI, "uses the real Credential Manager");
        var store = new WindowsSecretStore(prefix: "PulseTest-" + Guid.NewGuid().ToString("N"));

        try
        {
            Assert.Null(store.Get("github"));

            store.Set("github", "first-secret");
            store.Set("github", "sécret-ünïcode");
            Assert.Equal("sécret-ünïcode", store.Get("github"));

            store.Delete("github");
            Assert.Null(store.Get("github"));
            store.Delete("github"); // deleting a missing secret is fine
        }
        finally
        {
            store.Delete("github");
        }
    }

    [Fact]
    public void Launch_at_login_writes_and_removes_a_quoted_Run_value()
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), "Windows only");
        var name = "PulseTest-" + Guid.NewGuid().ToString("N");
        var launch = new WindowsLaunchAtLogin(name, @"C:\Program Files\Pulse\Pulse.App.exe");

        try
        {
            Assert.True(launch.IsSupported);
            Assert.False(launch.IsEnabled);

            launch.SetEnabled(true);
            Assert.True(launch.IsEnabled);
            using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run"))
                Assert.Equal("\"C:\\Program Files\\Pulse\\Pulse.App.exe\"", key!.GetValue(name));

            launch.SetEnabled(false);
            Assert.False(launch.IsEnabled);
            launch.SetEnabled(false); // disabling twice is fine
        }
        finally
        {
            launch.SetEnabled(false);
        }
    }
}
