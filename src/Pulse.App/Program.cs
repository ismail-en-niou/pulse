using Avalonia;
using Pulse.App.Api;
using Pulse.Core;
using Pulse.Core.Activities;
using Pulse.Core.Diagnostics;
using Pulse.Core.Settings;

namespace Pulse.App;

internal static class Program
{
    [STAThread]
    public static int Main(string[] args)
    {
        PulseLog.Sink = message => Console.WriteLine($"{DateTime.Now:HH:mm:ss.fff} {message}");

        using var instance = SingleInstance.TryAcquire(PulsePaths.Lock);
        if (instance is null)
        {
            PulseLog.Event("another Pulse is already running; exiting");
            return 1;
        }

        var settings = PulseSettings.LoadOrCreate(PulsePaths.Settings, PulseLog.Event);
        using var activities = new ActivityService(settings);
        using var api = LocalApi.TryStart(activities, PlatformFactory.CreateFileWriter(), PulsePaths.Connection);

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        return 0;
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
