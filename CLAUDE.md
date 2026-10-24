# Pulse — project guide for Claude Code

Pulse is an open-source Dynamic Island for developers on Windows and macOS. A pill at the top of the screen shows Claude Code status, CI results, pull requests, a focus timer, and activities pushed by any script through a local API and the `pulse` CLI.

## Current state

Milestone 2 done (v0.2, unreleased, branch `m2-foundation`):
- Pulse.App runs the island from activities: top activity collapsed with a state glow, the recent list on hover, fade-out of finished ones. It also runs the local API on 127.0.0.1 with the per-install token in `connection.json` (`docs/api.md`).
- Pulse.Cli is the `pulse` command.
- The platform projects hold placement, user-only files, secret stores and launch at login behind Core interfaces. Secret stores and launch at login are not used until M4/M5.
- Windows is verified by tests and an automated smoke test (focus, click-through, hover, idle CPU 0.02%).
- Nothing on macOS (M1 or M2) has run on real hardware yet. The non-activating click decision (spike finding 8) is still open.
- `spike/` is reference code from M1; `spike/README.md` has its findings.

The next step is the macOS check, then milestone 3: `/m3-claude-code`.
Update this section at the end of every milestone.

## Stack

- .NET 10, C# (LangVersion latest), Avalonia UI
- xUnit for tests
- Solution format: `.slnx`
- Shared settings in `Directory.Build.props`: Nullable enable, ImplicitUsings enable, TreatWarningsAsErrors true

## Structure

```text
src/
  Pulse.App/               Avalonia app: island UI, tray/menu bar icon, wiring
  Pulse.Core/              Activity model, priority queue, settings, event bus. No OS code.
  Pulse.Platform.Windows/  Window placement, Credential Manager, launch at login
  Pulse.Platform.Mac/      Notch placement (NSWindow interop), Keychain, launch at login
  Pulse.Cli/               The `pulse` command-line tool
tests/
  Pulse.Tests/             xUnit tests for Pulse.Core and Pulse.Cli mapping logic
spike/                     Throwaway spike code from milestone 1
docs/                      ROADMAP.md and design notes
```

## Commands

```bash
dotnet build Pulse.slnx
dotnet test Pulse.slnx
dotnet run --project src/Pulse.App
dotnet run --project src/Pulse.Cli -- notify "hello"
```

## Rules

1. **Local API listens on 127.0.0.1 only**, and every request needs the per-install token.
2. **Never log tokens, prompts, file contents or command output.** Logs contain event types and timings only.
3. **Secrets live in the OS keychain** (Windows Credential Manager / macOS Keychain), never in config files. The one exception is the local API token: it lives in `connection.json`, which only the current user can read, so the CLI and hooks never wait on the keychain.
4. **Pulse.Core has no OS dependencies.** Anything that touches Win32, AppKit or the keychain goes in a Platform project behind an interface defined in Core.
5. **Every change to Core or to CLI mapping logic needs a unit test.**
6. **Never steal focus.** The island must not take keyboard focus from the user's active window.
7. **Idle CPU stays under 1%.** Nothing polls when its source is disabled.
8. **The `pulse hook` commands must exit fast** and fail silently when Pulse isn't running, so Claude Code is never slowed down.
9. Writing to the user's Claude Code settings: back up first, merge without removing existing hooks, show a diff and ask before writing.

## How to work in this repo

- One milestone per session. Commands live in `.claude/commands/` (`/m1-spike` … `/m6-release`).
- Start every milestone in plan mode and wait for approval before writing code.
- Build and run tests after each step. Commit after each working step with a clear message.
- At the end of a milestone: update "Current state" above, `CHANGELOG.md`, and the README if behavior changed. Then give the user a manual test checklist for Windows and macOS.
- If something can't be done reliably on one OS, say so and propose a simpler version instead of hacking around the platform.
