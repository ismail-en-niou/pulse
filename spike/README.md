# Milestone 1 spike: the pill window

Throwaway code that proves the hard parts of the island before the real project is built in milestone 2. Not part of `Pulse.slnx`, not built by CI.

## Run

```bash
dotnet run --project spike/Pulse.Spike                          # the island
dotnet run --project spike/Pulse.Spike -- --debug-bounds        # also outline the whole (transparent) window
dotnet run --project spike/Pulse.Spike -- --mac-nonactivating   # macOS: probe clicking without activating (private API)
```

Quit with Ctrl+C in the terminal. The console shows an event log (event types and timings only): placement, hover enter/exit, and how long each animation took at what frame rate. Hover the pill to see a diagnostics line with OS, scale, notch size, transparency level and click-through mode.

## How it works

- **One fixed-size transparent window** (about 458×167 DIP) at the top-center of the primary screen. The pill is drawn inside it and animated with a damped spring (`Spring.cs`, driven by `RequestAnimationFrame` only while moving). The native window never resizes, because resizing every frame flickers on both OSes.
- **Placement** uses Avalonia's `Screens.Primary`: bounds are physical pixels on Windows and points (with `Scaling = 1`) on macOS, so `x = Bounds.X + (Bounds.Width − width × Scaling) / 2` works for both. On a notched Mac the collapsed pill is the notch plus 12 pt on each side (at least 200 pt), flat on top. Elsewhere it is a 200×36 capsule.
- **Click-through.** The window is bigger than the collapsed pill, so the area around the pill must let clicks through:
  - Windows: `SetWindowRgn` limits the window to the pill at rest. While animating it covers the whole window.
  - macOS: `ignoresMouseEvents` is on whenever the cursor is outside the pill. The cursor is followed with NSEvent global and local mouse-moved monitors.
- **Platform code** sits behind `Platform/IIslandPlatform.cs`: `WindowsIslandPlatform`, `MacIslandPlatform` and `NullIslandPlatform` for other OSes.

## Results

| Requirement | Windows 11 (125%, 1920×1080) | macOS |
|---|---|---|
| Borderless transparent topmost pill, top-center | ✅ verified | ⏳ pending |
| Over the notch, above the menu bar; capsule without notch | n/a | ⏳ pending |
| All Spaces, over full-screen apps | n/a | ⏳ pending |
| Never steals focus (launch, hover, click) | ✅ verified | ⏳ pending (click: see probe) |
| Hidden from Alt+Tab and taskbar | ✅ styles verified, Alt+Tab UI by hand | n/a (no Dock icon, not in Cmd+Tab) |
| Spring expand to 420×140 on hover, collapse on leave | ✅ verified (~715 ms expand, ~470 ms collapse, 55–60 fps) | ⏳ pending |
| Clicks around the collapsed pill reach windows behind | ✅ verified (beside, below, capsule corner) | ⏳ pending |
| Idle CPU under 1% | ✅ 0.000% once settled | ⏳ pending |

The Windows column comes from an automated smoke test. It drives the cursor and checks foreground window, extended styles, `WindowFromPoint` around the pill, the window region before, during and after hover, and process CPU time. 18/18 checks passed.

## Findings for milestone 2

1. **Avalonia 12** renamed `SystemDecorations` to `WindowDecorations`.
2. **Windows: Avalonia rebuilds the extended window style on every property change.** Extra flags (`WS_EX_NOACTIVATE`, `WS_EX_TOOLWINDOW`) must come from `Win32Properties.AddWindowStylesCallback`, followed by a property change (here `ShowInTaskbar = false`) so the callback runs.
3. **Windows: releasing mouse capture after a click makes Avalonia raise `PointerExited`** although the cursor is still on the pill. Without a fix, a click collapses and re-expands the island. Exits are now checked against `GetCursorPos`/`WindowFromPoint`, and `TrackMouseEvent(TME_LEAVE)` is re-armed.
4. **Windows: `SetWindowRgn` works for hit-testing** with Avalonia's composition-based transparent window. It is applied only at rest so the region never clips a moving pill.
5. **macOS: Avalonia's app delegate activates the app at launch.** It also briefly activates the Dock, because its desired activation policy stays `Regular`. Both would take focus from the user, so `DisableAvaloniaAppDelegate = true`. The island needs none of the delegate's events.
6. **macOS: Avalonia sets the window level only when `Topmost` or the owner changes.** Never set `Topmost` on macOS, or it overrides the native level.
7. **macOS: clicks don't reliably pass through transparent pixels** on recent macOS, which is why `ignoresMouseEvents` follows the cursor.
8. **macOS: Avalonia has no non-activating panel.** Clicking the pill will activate Pulse unless the `--mac-nonactivating` probe works, and Apple warns this can pull a full-screen app out of its Space. M2 must decide based on the probe's result.
9. **Idle CPU is zero once settled.** The first few animations cost a few percent of one core for about 20–30 s while tiered JIT recompiles them. Consider ReadyToRun when packaging (M6).

## Not covered

Tray/menu-bar icon, activities, settings, choosing a monitor, Windows virtual desktops (the island stays on the desktop it started on), Linux beyond a plain topmost window.
