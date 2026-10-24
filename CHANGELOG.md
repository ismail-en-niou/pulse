# Changelog

All notable changes to Pulse are documented here. The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and versions follow [Semantic Versioning](https://semver.org/).

## [Unreleased]

### Added
- Milestone 2 foundation: `Pulse.slnx` with `Pulse.App`, `Pulse.Core`, `Pulse.Platform.Windows`, `Pulse.Platform.Mac`, `Pulse.Cli` and `Pulse.Tests`
- Activities: id, source, title, subtitle, state (running, needs input, success, failure, notice), optional progress, priority and timestamps; a priority queue decides what the island shows
- The island shows the most important activity collapsed, with its icon, title, progress and a colored glow (blue running, orange needs input, green success, red failure); on notched Macs the icon and a short text sit beside the notch
- Hovering the island lists the recent activities with subtitles and progress bars; finished activities fade out after `finishedActivitySeconds` in `settings.json` (default 10)
- Local HTTP API on 127.0.0.1 only, on a port the OS picks, with a per-install token; port and token are in `connection.json`, readable by the current user only ([docs/api.md](docs/api.md))
- `pulse` CLI: `start`, `progress`, `update`, `done`, `fail`, `notify`, `list`; `start` and `notify` print the activity id for scripts
- Only one Pulse runs at a time
- Platform services for later milestones: Windows Credential Manager and macOS Keychain secret stores, launch at login (Run key, LaunchAgent)
- Milestone 1 spike in `spike/Pulse.Spike` (Avalonia 12.1.4): a borderless, transparent, always-on-top pill at the top-center of the primary screen that expands on hover with a spring animation
- Windows: the pill never takes focus, stays out of the taskbar and Alt+Tab, and lets clicks around it through to the windows behind
- macOS: the pill sits above the menu bar over the notch (a capsule when there is no notch), on all Spaces and over full-screen apps, and lets clicks around it through; `--mac-nonactivating` probes clicking without activating Pulse

## [0.0.1] - 2026-10-23

### Added
- Project scaffold: README, MIT license, CLAUDE.md project guide
- Claude Code milestone commands `/m1-spike` to `/m6-release` in `.claude/commands/`
- Claude Code project settings with safe default permissions
- Roadmap in `docs/ROADMAP.md`
- CI workflow (build and test on Windows and macOS once the solution exists)
- Release workflow that publishes a GitHub release for every `v*` tag
- Contributing guide and issue templates
