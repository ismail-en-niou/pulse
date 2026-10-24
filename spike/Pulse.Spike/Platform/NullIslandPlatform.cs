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
    }

    public string ClickThroughMode => "no click-through";

    public bool TracksPointer => false;

    public event Action<Point?>? PointerMoved
    {
        add { }
        remove { }
    }

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
