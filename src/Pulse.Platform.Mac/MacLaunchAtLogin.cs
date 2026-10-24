using System.Xml.Linq;
using Pulse.Core.Platform;

namespace Pulse.Platform.Mac;

/// <summary>
/// Starts Pulse at login with a LaunchAgent in ~/Library/LaunchAgents. launchd reads it at the next login,
/// so nothing starts or stops now. SMAppService would need an app bundle, which arrives with packaging (M6).
/// </summary>
public sealed class MacLaunchAtLogin : ILaunchAtLogin
{
    /// <summary>launchd label and plist name, until packaging picks the bundle identifier.</summary>
    public const string Label = "io.github.ismail-en-niou.pulse";

    private readonly string _plistPath;
    private readonly string? _executablePath;

    /// <param name="launchAgentsDirectory">Defaults to ~/Library/LaunchAgents; tests use a temporary folder.</param>
    /// <param name="executablePath">Program to start; defaults to the running executable.</param>
    public MacLaunchAtLogin(string? launchAgentsDirectory = null, string? executablePath = null)
    {
        var directory = launchAgentsDirectory ??
                        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Library", "LaunchAgents");
        _plistPath = Path.Combine(directory, Label + ".plist");
        _executablePath = executablePath ?? Environment.ProcessPath;
    }

    public bool IsSupported => _executablePath is not null;

    public bool IsEnabled => File.Exists(_plistPath);

    public void SetEnabled(bool enabled)
    {
        if (!enabled)
        {
            File.Delete(_plistPath);
            return;
        }

        var path = _executablePath ?? throw new InvalidOperationException("the running executable is unknown");
        Directory.CreateDirectory(Path.GetDirectoryName(_plistPath)!);
        File.WriteAllText(_plistPath, CreatePlist(path));
    }

    public static string CreatePlist(string executablePath)
    {
        var plist = new XDocument(
            new XDocumentType("plist", "-//Apple//DTD PLIST 1.0//EN", "http://www.apple.com/DTDs/PropertyList-1.0.dtd", null),
            new XElement("plist", new XAttribute("version", "1.0"),
                new XElement("dict",
                    new XElement("key", "Label"), new XElement("string", Label),
                    new XElement("key", "ProgramArguments"), new XElement("array", new XElement("string", executablePath)),
                    new XElement("key", "RunAtLoad"), new XElement("true"),
                    new XElement("key", "ProcessType"), new XElement("string", "Interactive"))));

        // XDocument.ToString() leaves out the XML declaration.
        return "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n" + plist + "\n";
    }
}
