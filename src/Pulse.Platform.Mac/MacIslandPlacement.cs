using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Avalonia.Controls;
using Avalonia.Platform;
using Pulse.Core.Diagnostics;
using Pulse.Core.Platform;

namespace Pulse.Platform.Mac;

/// <summary>
/// macOS: sits above the menu bar (over the notch when there is one), on every Space and over full-screen apps.
/// Clicks pass through because the window ignores mouse events whenever the cursor is outside the pill;
/// the cursor is followed with NSEvent mouse-moved monitors, which need no permission and cost nothing while idle.
/// </summary>
[SupportedOSPlatform("macos")]
public sealed unsafe class MacIslandPlacement : IIslandPlacement
{
    // NSMainMenuWindowLevel + 3: above the menu bar, below pop-up menus (101) so menus still cover the pill.
    private const nint WindowLevel = 24 + 3;

    // NSWindowCollectionBehavior: CanJoinAllSpaces | Stationary | IgnoresCycle | FullScreenAuxiliary.
    private const ulong CollectionBehavior = (1 << 0) | (1 << 4) | (1 << 6) | (1 << 8);

    private const ulong NSEventMaskMouseMoved = 1 << 5;

    private static MacIslandPlacement? s_current;

    private readonly Window _window;
    private readonly bool _nonActivating;
    private nint _globalMonitor;
    private nint _localMonitor;
    private bool? _ignoresMouseEvents;

    /// <param name="nonActivating">Opt-in probe from the spike: try to make clicks on the pill not activate Pulse (private API).</param>
    public MacIslandPlacement(Window window, bool nonActivating)
    {
        _window = window;
        _nonActivating = nonActivating;
        s_current = this;

        // The NSWindow already exists, so configure it before Show(). Topmost is never set on macOS:
        // Avalonia would reset the window level whenever it changes.
        ConfigureWindow();
    }

    public string ClickThroughMode =>
        "ignoresMouseEvents" + (_nonActivating ? " · non-activating probe" : "");

    public event Action<IslandPoint?>? PointerMoved;

    private nint NSWindow => (_window.TryGetPlatformHandle() as IMacOSTopLevelPlatformHandle)?.NSWindow ?? 0;

    public NotchInfo? DetectNotch()
    {
        // Only Apple silicon Macs have a notch. This also keeps the struct returns below off x86_64,
        // where they would need objc_msgSend_stret.
        if (RuntimeInformation.ProcessArchitecture != Architecture.Arm64)
            return null;

        var screens = ObjC.Msg(ObjC.Class("NSScreen"), ObjC.Sel("screens"));
        if (screens == 0 || ObjC.Msg(screens, ObjC.Sel("count")) == 0)
            return null;

        // Index 0 is the primary screen: the one with the menu bar.
        var screen = ObjC.Msg(screens, ObjC.Sel("firstObject"));
        if (!ObjC.RespondsTo(screen, "safeAreaInsets") || !ObjC.RespondsTo(screen, "auxiliaryTopLeftArea"))
            return null; // before macOS 12

        var height = ObjC.MsgInsets(screen, ObjC.Sel("safeAreaInsets")).Top;
        if (height <= 0)
            return null;

        var frame = ObjC.MsgRect(screen, ObjC.Sel("frame"));
        var left = ObjC.MsgRect(screen, ObjC.Sel("auxiliaryTopLeftArea"));
        var right = ObjC.MsgRect(screen, ObjC.Sel("auxiliaryTopRightArea"));
        var width = frame.Size.Width - left.Size.Width - right.Size.Width;
        return width > 0 ? new NotchInfo(width, height) : null;
    }

    public void OnOpened()
    {
        // Again after Show(), in case anything was reset while the window was ordered in.
        ConfigureWindow();

        var window = NSWindow;
        if (window == 0)
        {
            PulseLog.Event("no NSWindow handle; macOS window setup skipped");
            return;
        }

        if (_nonActivating)
            MakeNonActivating(window);

        ObjC.MsgVoid(window, ObjC.Sel("orderFrontRegardless"));
        SetIgnoresMouseEvents(true);
        InstallMouseMonitors();
    }

    public void SetPointerInside(bool inside) => SetIgnoresMouseEvents(!inside);

    public void OnAnimationStarted()
    {
    }

    public void OnSettled(IslandRect pill, double cornerRadius)
    {
    }

    public void Dispose()
    {
        RemoveMonitor(ref _globalMonitor);
        RemoveMonitor(ref _localMonitor);
        if (s_current == this)
            s_current = null;
    }

    private void ConfigureWindow()
    {
        var window = NSWindow;
        if (window == 0)
            return;

        ObjC.MsgVoid(window, ObjC.Sel("setLevel:"), WindowLevel);
        ObjC.MsgVoid(window, ObjC.Sel("setCollectionBehavior:"), (nint)CollectionBehavior);
        ObjC.MsgVoid(window, ObjC.Sel("setHasShadow:"), (byte)0);
    }

    private void SetIgnoresMouseEvents(bool ignore)
    {
        if (_ignoresMouseEvents == ignore)
            return;

        _ignoresMouseEvents = ignore;
        ObjC.MsgVoid(NSWindow, ObjC.Sel("setIgnoresMouseEvents:"), (byte)(ignore ? 1 : 0));
    }

    private void InstallMouseMonitors()
    {
        if (_globalMonitor != 0)
            return;

        // Global monitors see events sent to other apps (while the window ignores the mouse),
        // local monitors see events sent to this app (while it doesn't).
        var global = ObjC.CreateGlobalBlock((nint)(delegate* unmanaged<nint, nint, void>)&OnGlobalMouseMoved, "v16@?0@8");
        var local = ObjC.CreateGlobalBlock((nint)(delegate* unmanaged<nint, nint, nint>)&OnLocalMouseMoved, "@16@?0@8");

        var nsEvent = ObjC.Class("NSEvent");
        _globalMonitor = Retain(ObjC.Msg(nsEvent, ObjC.Sel("addGlobalMonitorForEventsMatchingMask:handler:"), NSEventMaskMouseMoved, global));
        _localMonitor = Retain(ObjC.Msg(nsEvent, ObjC.Sel("addLocalMonitorForEventsMatchingMask:handler:"), NSEventMaskMouseMoved, local));
        PulseLog.Event($"mouse monitors global={_globalMonitor != 0} local={_localMonitor != 0}");
    }

    private static void RemoveMonitor(ref nint monitor)
    {
        if (monitor == 0)
            return;

        ObjC.MsgVoid(ObjC.Class("NSEvent"), ObjC.Sel("removeMonitor:"), monitor);
        ObjC.MsgVoid(monitor, ObjC.Sel("release"));
        monitor = 0;
    }

    private static nint Retain(nint obj) => obj == 0 ? 0 : ObjC.Msg(obj, ObjC.Sel("retain"));

    /// <summary>Monitors run on the main thread, which is Avalonia's UI thread on macOS.</summary>
    private void ReportCursor()
    {
        if (_window.Screens.Primary is not { } primary)
            return;

        // Cocoa screen coordinates start at the bottom-left of the primary screen; Avalonia's start at its top-left.
        var mouse = ObjC.MsgPoint(ObjC.Class("NSEvent"), ObjC.Sel("mouseLocation"));
        var primaryTop = primary.Bounds.Y + primary.Bounds.Height;
        var origin = _window.Position;
        PointerMoved?.Invoke(new IslandPoint(mouse.X - origin.X, primaryTop - mouse.Y - origin.Y));
    }

    private static void HandleMouseMoved()
    {
        try
        {
            s_current?.ReportCursor();
        }
        catch (Exception e)
        {
            // Never let an exception cross back into AppKit.
            PulseLog.Event($"mouse monitor failed: {e.GetType().Name}");
        }
    }

    [UnmanagedCallersOnly]
    private static void OnGlobalMouseMoved(nint block, nint nsEvent) => HandleMouseMoved();

    [UnmanagedCallersOnly]
    private static nint OnLocalMouseMoved(nint block, nint nsEvent)
    {
        HandleMouseMoved();
        return nsEvent; // pass the event on to its window
    }

    /// <summary>
    /// Opt-in experiment (private API): swap the NSWindow to a runtime subclass that can't become key or main,
    /// then set AppKit's preventsActivation flag (what a non-activating NSPanel uses), so clicking the pill
    /// shouldn't activate Pulse. Avalonia has no non-activating panel of its own.
    /// </summary>
    private static void MakeNonActivating(nint window)
    {
        const string name = "PulseNonActivatingWindow";
        var subclass = ObjC.objc_getClass(name);
        if (subclass == 0)
        {
            subclass = ObjC.objc_allocateClassPair(ObjC.object_getClass(window), name, 0);
            var returnNo = (nint)(delegate* unmanaged<nint, nint, byte>)&ReturnNo;
            var types = RuntimeInformation.ProcessArchitecture == Architecture.Arm64 ? "B@:" : "c@:";
            ObjC.class_addMethod(subclass, ObjC.Sel("canBecomeKeyWindow"), returnNo, types);
            ObjC.class_addMethod(subclass, ObjC.Sel("canBecomeMainWindow"), returnNo, types);
            ObjC.objc_registerClassPair(subclass);
        }

        ObjC.object_setClass(window, subclass);

        var preventsActivation = ObjC.RespondsTo(window, "_setPreventsActivation:");
        if (preventsActivation)
            ObjC.MsgVoid(window, ObjC.Sel("_setPreventsActivation:"), (byte)1);

        PulseLog.Event($"non-activating probe applied, preventsActivation {(preventsActivation ? "set" : "unavailable")}");
    }

    [UnmanagedCallersOnly]
    private static byte ReturnNo(nint self, nint selector) => 0;
}
