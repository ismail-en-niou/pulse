---
description: Milestone 3 (v0.3) — Claude Code integration through hooks
---

Milestone 3 of Pulse: Claude Code integration using Claude Code hooks.

1. Read the official Claude Code hooks documentation first. Summarize which hook events and stdin JSON fields we can use, and confirm the settings file locations on Windows and macOS.
2. Add `pulse hook claude` to Pulse.Cli. It reads the hook JSON from stdin and maps it to activities:
   - session start / user prompt submitted → "Claude is working" (Running), titled with the project folder name
   - tool use → subtitle shows the current action, for example "Editing Program.cs" or "Running tests"
   - waiting for permission or input → NeedsInput, orange glow, gentle pulse animation, optional sound
   - stop → Success "Claude finished" with elapsed time
   Sessions in different projects show as separate activities (keyed by session id).
3. Add `pulse install claude-hooks` and `pulse uninstall claude-hooks`. Follow rule 9 in CLAUDE.md: back up the settings file, merge without removing existing hooks, show a diff and ask before writing. Uninstall removes only Pulse's hooks.
4. Clicking a Claude activity brings its terminal window to the front when the OS allows it; otherwise it does nothing.
5. Rule 8 in CLAUDE.md is critical: the hook command must exit fast and fail silently if Pulse isn't running.
6. Unit tests for the JSON → activity mapping using sample hook payloads stored as fixtures in tests/.

Start in plan mode and wait for approval. Build, test and commit after each step. At the end, update CLAUDE.md, CHANGELOG.md and README (add a "Claude Code setup" section), and give me a manual test checklist.

$ARGUMENTS
