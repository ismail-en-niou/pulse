---
description: Milestone 6 (v1.0) — installers, signing, Homebrew, winget and release docs
---

Milestone 6 of Pulse: prepare v1.0 for public release. Extend `.github/workflows/release.yml` rather than replacing it.

1. Windows: Velopack installer (x64 and arm64) built on every `v*` tag and attached to the GitHub release. Generate a winget manifest.
2. macOS: universal app (arm64 + x64) packaged as a DMG. Add signing and notarization steps that run only when the required secrets are set; document the secrets in docs/RELEASING.md. Add a Homebrew cask.
3. The `pulse` CLI ships inside both installers and is added to PATH (or documented clearly if it can't be).
4. README: demo GIF placeholders, features, install instructions per OS, Claude Code setup in one command, privacy section.
5. Make sure CHANGELOG.md, CONTRIBUTING.md and the issue templates are up to date. Add docs/sources.md explaining how to add a new activity source.

Start in plan mode and wait for approval. At the end, tell me exactly which commands to run to tag and publish v1.0.0.

$ARGUMENTS
