# Pulse roadmap

## Path to v1.0

| Version | Milestone command | What ships |
|---|---|---|
| v0.1 | `/m1-spike` | Pill window on Windows and macOS (notch on Mac), expand/collapse animation, click-through |
| v0.2 | `/m2-foundation` | Project structure, activity model, priority queue, local API, `pulse` CLI |
| v0.3 | `/m3-claude-code` | Claude Code hooks: working, needs input, finished; one-command install |
| v0.4 | `/m4-github` | GitHub Actions runs, review requests, your PRs |
| v0.5 | `/m5-polish` | Focus timer, settings, themes, hotkeys, multi-monitor, robustness |
| v1.0 | `/m6-release` | Velopack (Windows), signed DMG (macOS), winget, Homebrew, docs |

Riskiest part: placing the window over the macOS notch and above the menu bar. Milestone 1 exists to prove it before anything else is built.

## After v1.0

### More developer sources
- Other AI coding agents (Codex CLI, Cursor, Aider) through the same CLI/hook pattern
- Local dev servers: detect when `localhost:3000` comes up or crashes
- Test watchers: `dotnet test`, Jest, pytest results live in the pill
- Docker: containers starting, unhealthy or exited
- Deploys: Vercel, Netlify, Azure finishing
- Jira or Linear tickets assigned to you

### Daily life
- Meeting countdown with one-click join for Teams and Meet, plus mic and camera status
- Privacy mode: during screen sharing, the island hides its content automatically
- Mini drop shelf: drag files onto the pill to keep them for later
- Now playing music (Windows media controls first; macOS later)

### Power features
- Plugin system: a source is a small JSON manifest + script, so the community adds integrations without touching the core
- Daily summary: "6 Claude sessions, 2 CI failures fixed, 3h focus"
- Phone companion: forward needs-input alerts to your phone when you're away from the desk
- Team mode: a shared island for a team's release pipeline

### Suggested order after v1.0
1. Plugin system
2. Local dev servers + test watchers
3. Meeting countdown + privacy mode
4. Daily summary
