using System.Runtime.Versioning;
using System.Xml.Linq;
using Pulse.Platform.Mac;

namespace Pulse.Tests.Platform;

public sealed class MacPlatformTests : IDisposable
{
    private readonly string _directory = TestEnvironment.TempDirectory();

    public void Dispose()
    {
        if (Directory.Exists(_directory))
            Directory.Delete(_directory, recursive: true);
    }

    [Fact]
    [UnsupportedOSPlatform("windows")]
    public void User_only_file_has_mode_0600()
    {
        Assert.SkipWhen(OperatingSystem.IsWindows(), "Unix only");
        var path = Path.Combine(_directory, "connection.json");

        new PosixUserOnlyFileWriter().Write(path, "first");
        new PosixUserOnlyFileWriter().Write(path, "second");

        Assert.Equal("second", File.ReadAllText(path));
        Assert.Equal([path], Directory.GetFiles(_directory));
        Assert.Equal(UnixFileMode.UserRead | UnixFileMode.UserWrite, File.GetUnixFileMode(path));
        Assert.Equal(UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute, File.GetUnixFileMode(_directory));
    }

    [Fact]
    [SupportedOSPlatform("macos")]
    public void Secret_store_round_trips_through_the_keychain()
    {
        Assert.SkipUnless(OperatingSystem.IsMacOS(), "macOS only");
        Assert.SkipWhen(TestEnvironment.IsCI, "uses the real keychain");
        var store = new MacSecretStore(service: "PulseTest-" + Guid.NewGuid().ToString("N"));

        try
        {
            Assert.Null(store.Get("github"));

            store.Set("github", "first-secret");
            store.Set("github", "sécret-ünïcode");
            Assert.Equal("sécret-ünïcode", store.Get("github"));

            store.Delete("github");
            Assert.Null(store.Get("github"));
            store.Delete("github");
        }
        finally
        {
            store.Delete("github");
        }
    }

    // Plain file work, so this runs on every OS.
    [Fact]
    public void Launch_at_login_writes_and_removes_a_launch_agent()
    {
        var launch = new MacLaunchAtLogin(_directory, "/Applications/Pulse & Co/Pulse");
        var plist = Path.Combine(_directory, MacLaunchAtLogin.Label + ".plist");

        Assert.False(launch.IsEnabled);
        launch.SetEnabled(true);

        Assert.True(launch.IsEnabled);
        var text = File.ReadAllText(plist);
        Assert.StartsWith("<?xml version=\"1.0\" encoding=\"UTF-8\"?>", text);
        var dict = XDocument.Parse(text).Root!.Element("dict")!;
        var program = dict.Element("array")!.Element("string")!.Value;
        Assert.Equal("/Applications/Pulse & Co/Pulse", program);
        Assert.Contains(dict.Elements("key"), k => k.Value == "RunAtLoad");

        launch.SetEnabled(false);
        Assert.False(launch.IsEnabled);
        launch.SetEnabled(false);
    }
}
