using Avalonia.Controls;
using Pulse.App.Island;
using Pulse.Core.Platform;
using Pulse.Platform.Mac;
using Pulse.Platform.Windows;

namespace Pulse.App;

/// <summary>Picks the platform implementation of each Core interface for the OS Pulse runs on.</summary>
internal static class PlatformFactory
{
    public static IIslandPlacement CreatePlacement(Window window, AppOptions options)
    {
        if (OperatingSystem.IsWindows())
            return new WindowsIslandPlacement(window);

        if (OperatingSystem.IsMacOS())
            return new MacIslandPlacement(window, options.MacNonActivating);

        return new NullIslandPlacement(window);
    }
}
