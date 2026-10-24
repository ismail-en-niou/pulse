using Avalonia.Controls;
using Pulse.Core.Platform;

namespace Pulse.App.Island;

/// <summary>Fallback for operating systems Pulse doesn't target: a plain topmost window without click-through.</summary>
internal sealed class NullIslandPlacement : IIslandPlacement
{
    public NullIslandPlacement(Window window)
    {
        window.Topmost = true;
        window.ShowInTaskbar = false;
        window.PointerMoved += (_, e) =>
        {
            var position = e.GetPosition(window);
            PointerMoved?.Invoke(new IslandPoint(position.X, position.Y));
        };
        window.PointerExited += (_, _) => PointerMoved?.Invoke(null);
    }

    public string ClickThroughMode => "no click-through";

    public event Action<IslandPoint?>? PointerMoved;

    public NotchInfo? DetectNotch() => null;

    public void OnOpened()
    {
    }

    public void SetPointerInside(bool inside)
    {
    }

    public void OnAnimationStarted()
    {
    }

    public void OnSettled(IslandRect pill, double cornerRadius)
    {
    }

    public void Dispose()
    {
    }
}
