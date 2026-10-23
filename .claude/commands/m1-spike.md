---
description: Milestone 1 (v0.1) — spike proving the pill window on Windows and macOS
---

Milestone 1 of Pulse (see CLAUDE.md). Before building the real app, make a throwaway spike in `spike/` that proves the hard parts, in C# .NET 10 with Avalonia UI:

1. A borderless, transparent, always-on-top Avalonia window shaped as a black pill (about 200x36), centered at the top of the primary screen.
2. macOS: place it over the notch area, above the menu bar, visible on all Spaces and over full-screen apps. Use native interop on the NSWindow handle (window level, collectionBehavior). If the Mac has no notch, center it at the top of the screen.
3. Windows: top-center, never steals focus, hidden from Alt+Tab and the taskbar.
4. Hover expands it smoothly to about 420x140 with a spring-like animation; moving the mouse away collapses it.
5. When collapsed, clicks outside the pill pass through to the windows behind it.

Start in plan mode. In the plan, list which parts are risky on each OS and how we'll test them manually. Wait for my approval before writing code.

When it works, update the "Current state" section of CLAUDE.md, add an entry to CHANGELOG.md, commit, and give me a manual test checklist for Windows and macOS.

$ARGUMENTS
