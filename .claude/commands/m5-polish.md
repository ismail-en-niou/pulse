---
description: Milestone 5 (v0.5) — focus timer, settings, themes, hotkeys, multi-monitor, robustness
---

Milestone 5 of Pulse: make it feel polished.

1. Focus timer: start it from the island or with `pulse focus 25`. The pill shows the countdown. During focus, only NeedsInput and Failure activities may expand the island; everything else waits until focus ends.
2. Settings window: sources on/off, sounds on/off, glow colors, island size, auto-hide in full-screen apps, which monitor (primary, active or all), launch at login.
3. Global hotkeys (configurable, with conflict detection): toggle island visibility, open expanded view, clear finished activities.
4. Light and dark styles following the OS. Smooth 60 fps animations. Never steal focus (rule 6).
5. Robustness: single instance only, global exception handler, local rolling log files (max 7 days) that follow rule 2 in CLAUDE.md.
6. Performance: idle CPU under 1%. Measure it and report the numbers.

Start in plan mode and wait for approval. Work in small steps, building and committing after each. At the end, update CLAUDE.md, CHANGELOG.md and README, and give me a manual test checklist for Windows and macOS.

$ARGUMENTS
