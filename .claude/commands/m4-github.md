---
description: Milestone 4 (v0.4) — GitHub Actions runs and pull requests in the island
---

Milestone 4 of Pulse: GitHub integration.

1. Sign in with a fine-grained personal access token entered in Settings, read-only scopes only (document the exact permissions in the README). Store it with `ISecretStore` (OS keychain), never in a file.
2. Repository picker in Settings. For the selected repositories, watch:
   - GitHub Actions runs on my branches: Running → Success/Failure, with workflow name and duration
   - Pull requests where my review is requested
   - My own PRs: new reviews, changes requested, merged
3. Poll efficiently: conditional requests with ETags, respect rate-limit headers, back off on errors, pause when the computer is idle or the source is disabled.
4. Clicking an activity opens it in the default browser.
5. Unit tests for the mapping from API responses to activities, using recorded JSON fixtures (with fake usernames and repo names).

Start in plan mode and wait for approval. Build, test and commit after each step. At the end, update CLAUDE.md, CHANGELOG.md and README, and give me a manual test checklist.

$ARGUMENTS
