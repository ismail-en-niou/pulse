# Pulse

**A Dynamic Island for developers, on Windows and macOS.**

Pulse is a small pill at the top of your screen (in the notch on Mac, floating top-center on Windows) that shows the heartbeat of your dev work:

- **Claude Code needs you** — the pill glows orange when Claude Code is waiting for your input or permission
- **Claude is working** — see the current action ("Editing Program.cs", "Running tests") without switching windows
- **CI passed or failed** — GitHub Actions runs on your branches, live
- **Reviews waiting** — pull requests where your review is requested
- **Focus timer** — a countdown in the pill; only important events can interrupt
- **Anything else** — push your own activities from any script with the `pulse` CLI

> Status: **early development**. Milestone 2 (v0.2, unreleased): the island shows activities that any script pushes through the `pulse` CLI or the local API. Verified on Windows; the macOS check on real hardware is still pending. See the [roadmap](docs/ROADMAP.md).

## Why Pulse?

Dynamic Island clones already exist for music and calendars. Pulse is built for one person: the developer who runs AI agents, builds and tests all day and wants to know *the moment* something needs attention.

## Planned features

| Version | What ships |
|---|---|
| v0.1 | Pill window on Windows and macOS, expand/collapse animation |
| v0.2 | Event bus, local API, `pulse` CLI |
| v0.3 | Claude Code integration via hooks |
| v0.4 | GitHub: CI runs and pull requests |
| v0.5 | Focus timer, settings, themes, hotkeys, multi-monitor |
| v1.0 | Installers for Windows and macOS, Homebrew, winget |

## Use it from any script

```bash
pulse start "Training model"         # prints the new activity's id
pulse progress 40 "epoch 4 of 10"    # progress, with an optional message
pulse done                           # or: pulse fail "Tests failed"
pulse notify "Backup finished"       # a one-off notice
pulse list                           # what the island shows; --json for scripts
```

Without `--id`, `progress`, `update`, `done` and `fail` change the newest unfinished activity your script started. To run several at once, keep the ids:

```bash
train=$(pulse start "Training model" --progress 0)
tests=$(pulse start "Running tests" --source ci)
pulse progress 60 --id "$train"
pulse fail "3 tests failed" --id "$tests"
```

More options:
- `--subtitle <text>` sets the second line, and `--priority <-100..100>` makes an activity show first.
- `pulse start … --needs-input` turns the glow orange; `pulse update --state running` turns it back.
- `pulse help` lists every command.

Exit codes are 0 for ok, 1 when Pulse rejected the request, 2 for a usage error and 3 when Pulse isn't running.

The island shows the most important activity collapsed, with a glow: blue for running, orange for needs input, green for success, red for failure. Hover it to see the recent list. Finished activities fade out after 10 seconds; change `finishedActivitySeconds` in `settings.json` to keep them longer.

Tools that would rather speak HTTP can use the [local API](docs/api.md) directly.

## Tech stack

- **.NET 10 + Avalonia UI** — one C# codebase for Windows and macOS
- Platform-specific code (window placement, keychain, launch at login) behind interfaces
- Local API on `127.0.0.1` only, protected by a per-install token

## Run from source

Requirements: the .NET 10 SDK on Windows 10/11 or macOS 14+.

```bash
dotnet build Pulse.slnx
dotnet run --project src/Pulse.App                      # the island; Ctrl+C in the terminal quits
dotnet run --project src/Pulse.Cli -- notify "hello"    # or put src/Pulse.Cli/bin/Debug/net10.0 on your PATH
```

Pulse keeps its files in `%LOCALAPPDATA%\Pulse` on Windows and `~/Library/Application Support/Pulse` on macOS. Set `PULSE_HOME` to use another folder, for example for a second development copy.

| File | What it holds |
|---|---|
| `settings.json` | Your settings, such as `finishedActivitySeconds`. Never secrets. |
| `connection.json` | The API port and token for the CLI, readable only by you. |
| `pulse.lock` | Keeps a second Pulse from starting. |

Development switches for `Pulse.App`:
- `--debug-bounds` outlines the transparent window, to see where clicks pass through.
- `--mac-nonactivating` (macOS) probes clicking the pill without activating Pulse.

## Privacy

- Pulse never sends your code, prompts or file contents anywhere.
- The local API listens on `127.0.0.1` only, and every request needs the token from `connection.json`, a file only you can read.
- Logs contain event types and timings only: never tokens, activity titles or file contents.
- The only network calls go to the services you connect yourself (for example GitHub), and tokens are stored in your OS keychain.

## Building with Claude Code

This repo is set up to be built milestone by milestone with [Claude Code](https://claude.com/claude-code). Open the folder in Claude Code and run the milestone commands in order:

```text
/m1-spike
/m2-foundation
/m3-claude-code
/m4-github
/m5-polish
/m6-release
```

Each command contains the full prompt for that milestone. Run `/clear` between milestones; `CLAUDE.md` carries the project knowledge forward.

## Contributing

Contributions are welcome. Read [CONTRIBUTING.md](CONTRIBUTING.md) first.

## License

[MIT](LICENSE) © 2026 Ismail En-niou
