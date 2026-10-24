# Changelog

All notable changes to Pulse are documented here. The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and versions follow [Semantic Versioning](https://semver.org/).

## [Unreleased]

### Added
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
