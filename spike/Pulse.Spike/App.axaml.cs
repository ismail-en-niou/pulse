using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;

namespace Pulse.Spike;

public partial class App : Application
{
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new IslandWindow(SpikeOptions.Parse(desktop.Args ?? []));

            Console.CancelKeyPress += (_, e) =>
            {
                e.Cancel = true;
                Dispatcher.UIThread.Post(() => desktop.Shutdown());
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}

/// <summary>Command-line switches for manual testing.</summary>
public sealed record SpikeOptions(bool DebugBounds, bool MacNonActivating)
{
    public static SpikeOptions Default { get; } = new(false, false);

    public static SpikeOptions Parse(string[] args) =>
        new(args.Contains("--debug-bounds"), args.Contains("--mac-nonactivating"));
}
