---
description: Milestone 2 (v0.2) — real project structure, activity model, local API and pulse CLI
---

Milestone 2 of Pulse. The spike works. Build the real project following the structure, commands and rules in CLAUDE.md.

1. Create `Pulse.slnx`, `Directory.Build.props` and the projects listed in CLAUDE.md. Move the working spike code into `Pulse.App` and the platform projects, behind interfaces defined in `Pulse.Core` (for example `IIslandPlacement`, `ISecretStore`, `ILaunchAtLogin`).
2. Activity model in Core: id, source, title, subtitle, state (Running, NeedsInput, Success, Failure, Info), optional progress 0–100, priority, timestamps. A priority queue decides what the island shows.
3. Island UI: when collapsed, show the highest-priority activity (icon, short text, colored glow: blue running, orange needs-input, green success, red failure). When expanded, show a list of recent activities. Finished activities fade out after a configurable time.
4. Local HTTP API on 127.0.0.1 only. Pick a random free port and write port + per-install token to a config file in the user's app data folder (file readable by the current user only). Endpoints to create, update and finish activities. Every request must carry the token.
5. `Pulse.Cli` wraps the API: `pulse start "Training model"`, `pulse progress 40`, `pulse done`, `pulse fail "Tests failed"`, `pulse notify "message"`. Activity ids are returned so scripts can update the right activity.
6. Unit tests for the priority queue, activity state transitions and CLI argument parsing.
7. Add a CI workflow step if needed so `dotnet build` and `dotnet test` run on Windows and macOS runners.

Start in plan mode and wait for approval. Build and test after each step, commit after each working step. At the end, update CLAUDE.md "Current state", CHANGELOG.md and README, and give me a manual test checklist.

$ARGUMENTS
