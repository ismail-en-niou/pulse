# Pulse

**A Dynamic Island for developers, on Windows and macOS.**

Pulse is a small pill at the top of your screen (in the notch on Mac, floating top-center on Windows) that shows the heartbeat of your dev work:

- **Claude Code needs you** — the pill glows orange when Claude Code is waiting for your input or permission
- **Claude is working** — see the current action ("Editing Program.cs", "Running tests") without switching windows
- **CI passed or failed** — GitHub Actions runs on your branches, live
- **Reviews waiting** — pull requests where your review is requested
- **Focus timer** — a countdown in the pill; only important events can interrupt
- **Anything else** — push your own activities from any script with the `pulse` CLI

> Status: **early development**. The milestone 1 spike (the pill window on Windows and macOS) is in [`spike/`](spike/README.md). See the [roadmap](docs/ROADMAP.md).

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

## Use it from any script (planned)

```bash
pulse start "Training model"
pulse progress 40
pulse done
pulse fail "Tests failed"
pulse notify "Backup finished"
```

## Tech stack

- **.NET 10 + Avalonia UI** — one C# codebase for Windows and macOS
- Platform-specific code (window placement, keychain, launch at login) behind interfaces
- Local API on `127.0.0.1` only, protected by a per-install token

## Privacy

- Pulse never sends your code, prompts or file contents anywhere.
- The local API listens on `127.0.0.1` only.
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
