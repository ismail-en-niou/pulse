using Avalonia;
using Avalonia.Controls;

namespace Pulse.Spike.Platform;

/// <summary>Fallback for operating systems Pulse doesn't target: a plain topmost window without click-through.</summary>
internal sealed class NullIslandPlatform : IIslandPlatform
{
    public NullIslandPlatform(Window window)
    {
        window.Topmost = true;
        window.ShowInTaskbar = false;
        window.PointerMoved += (_, e) => PointerMoved?.Invoke(e.GetPosition(window));
        window.PointerExited += (_, _) => PointerMoved?.Invoke(null);
    }

    public string ClickThroughMode => "no click-through";

    public event Action<Point?>? PointerMoved;

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

    public void OnSettled(Rect pillRect, CornerRadius corners)
    {
    }

    public void Dispose()
    {
    }
}
