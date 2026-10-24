using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Pulse.App.Island;
using Pulse.Core.Activities;

namespace Pulse.App;

public partial class App : Application
{
    /// <summary>What the island shows. Null only in the XAML previewer.</summary>
    internal ActivityService? Activities { get; init; }

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new IslandWindow(AppOptions.Parse(desktop.Args ?? []), Activities);

            Console.CancelKeyPress += (_, e) =>
            {
                e.Cancel = true;
                Dispatcher.UIThread.Post(() => desktop.Shutdown());
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
