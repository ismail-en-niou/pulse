using Avalonia;

namespace Pulse.Spike;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args) =>
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);

    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>()
            .UsePlatformDetect()
            // Accessory app on macOS: no Dock icon, not in Cmd+Tab, and allowed over other apps' full-screen Spaces.
            .With(new MacOSPlatformOptions { ShowInDock = false })
            .LogToTrace();
}
