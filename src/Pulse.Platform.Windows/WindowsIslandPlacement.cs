using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Avalonia.Controls;
using Pulse.Core.Diagnostics;
using Pulse.Core.Platform;

namespace Pulse.Platform.Windows;

/// <summary>
/// Windows: never activates, stays out of the taskbar and Alt+Tab, and limits its hit-test area to the pill
/// with a window region so clicks around it reach the windows behind.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class WindowsIslandPlacement : IIslandPlacement
{
    /// <summary>The region reaches this far past the pill so it never clips the anti-aliased edge.</summary>
    private const int RegionMargin = 2;

    private readonly Window _window;

    public WindowsIslandPlacement(Window window)
    {
        _window = window;

        // Avalonia rebuilds the extended style on every window property change, so our flags must come from its callback.
        Win32Properties.AddWindowStylesCallback(window, (style, exStyle) =>
            (style, exStyle | Win32.WS_EX_NOACTIVATE | Win32.WS_EX_TOOLWINDOW));
        Win32Properties.AddWndProcHookCallback(window, WndProcHook);

        // Avalonia hides the taskbar button by giving the window a hidden owner. Changing the property
        // also re-applies the styles, so the callback above takes effect before the window is shown.
        window.ShowInTaskbar = false;
        window.Topmost = true;

        // The region keeps these events to the pill's area.
        window.PointerMoved += (_, e) =>
        {
            var position = e.GetPosition(window);
            PointerMoved?.Invoke(new IslandPoint(position.X, position.Y));
        };
        window.PointerExited += (_, _) => OnPointerExited();
    }

    public string ClickThroughMode => "window region";

    public event Action<IslandPoint?>? PointerMoved;

    private nint Hwnd => _window.TryGetPlatformHandle()?.Handle ?? 0;

    public NotchInfo? DetectNotch() => null;

    public void OnOpened()
    {
        var exStyle = (uint)Win32.GetWindowLongW(Hwnd, Win32.GWL_EXSTYLE);
        PulseLog.Event($"ex-style toolwindow={Has(exStyle, Win32.WS_EX_TOOLWINDOW)} noactivate={Has(exStyle, Win32.WS_EX_NOACTIVATE)} " +
                       $"topmost={Has(exStyle, Win32.WS_EX_TOPMOST)}");
    }

    public void SetPointerInside(bool inside)
    {
    }

    /// <summary>The whole window takes input while the pill moves, so the region never lags behind the drawing.</summary>
    public void OnAnimationStarted() => SetRegion(null, 0);

    public void OnSettled(IslandRect pill, double cornerRadius) => SetRegion(pill, cornerRadius);

    private void SetRegion(IslandRect? pill, double cornerRadius)
    {
        var hwnd = Hwnd;
        if (hwnd == 0)
            return;

        if (pill is not { } rect)
        {
            Win32.SetWindowRgn(hwnd, 0, true);
            return;
        }

        var scale = _window.RenderScaling;
        var diameter = (int)Math.Ceiling(2 * (cornerRadius * scale + RegionMargin));
        var region = Win32.CreateRoundRectRgn(
            (int)Math.Floor(rect.X * scale) - RegionMargin,
            (int)Math.Floor(rect.Y * scale) - RegionMargin,
            (int)Math.Ceiling(rect.Right * scale) + RegionMargin + 1,
            (int)Math.Ceiling(rect.Bottom * scale) + RegionMargin + 1,
            diameter,
            diameter);

        if (Win32.SetWindowRgn(hwnd, region, true) == 0)
            Win32.DeleteObject(region);
    }

    private void OnPointerExited()
    {
        var hwnd = Hwnd;
        if (hwnd != 0 && Win32.GetCursorPos(out var cursor) &&
            Win32.GetAncestor(Win32.WindowFromPoint(cursor), Win32.GA_ROOT) == hwnd)
        {
            // Releasing mouse capture after a click makes Avalonia report an exit although the cursor never left.
            // Keep the hover, and ask Windows to post WM_MOUSELEAVE when the cursor really leaves.
            var track = new Win32.TRACKMOUSEEVENT
            {
                cbSize = (uint)Marshal.SizeOf<Win32.TRACKMOUSEEVENT>(),
                dwFlags = Win32.TME_LEAVE,
                hwndTrack = hwnd,
            };
            Win32.TrackMouseEvent(ref track);

            var scale = _window.RenderScaling;
            var origin = _window.Position;
            PointerMoved?.Invoke(new IslandPoint((cursor.X - origin.X) / scale, (cursor.Y - origin.Y) / scale));
            return;
        }

        PointerMoved?.Invoke(null);
    }

    private static nint WndProcHook(nint hWnd, uint msg, nint wParam, nint lParam, ref bool handled)
    {
        if (msg != Win32.WM_MOUSEACTIVATE)
            return 0;

        // Clicking the pill must not take focus from the user's window.
        handled = true;
        return Win32.MA_NOACTIVATE;
    }

    private static bool Has(uint exStyle, uint flag) => (exStyle & flag) != 0;

    public void Dispose()
    {
    }
}
