using Avalonia;
using Pulse.Core.Diagnostics;

namespace Pulse.App;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        PulseLog.Sink = message => Console.WriteLine($"{DateTime.Now:HH:mm:ss.fff} {message}");
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .With(new MacOSPlatformOptions
            {
                // Accessory app: no Dock icon, not in Cmd+Tab, and allowed over other apps' full-screen Spaces.
                ShowInDock = false,
                // Avalonia's app delegate activates the app (and briefly the Dock) at launch, which would take
                // focus from the user's window. The island needs none of the delegate's events.
                DisableAvaloniaAppDelegate = true,
            })
            .LogToTrace();
}
