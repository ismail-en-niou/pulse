#!/usr/bin/env bash
# Creates github.com/ismail-en-niou/pulse, pushes main, and pushes the v0.0.1 tag.
# The tag push triggers .github/workflows/release.yml, which creates the GitHub release.
# Requires the GitHub CLI (gh) logged in: gh auth login
set -euo pipefail

OWNER="ismail-en-niou"
REPO="pulse"

cd "$(dirname "$0")/.."

gh auth status >/dev/null || { echo "Run 'gh auth login' first."; exit 1; }

gh repo create "$OWNER/$REPO" \
  --public \
  --description "A Dynamic Island for developers on Windows and macOS: Claude Code status, CI, PRs and focus in a pill at the top of your screen." \
  --source . \
  --remote origin \
  --push

git push origin --tags

gh repo edit "$OWNER/$REPO" --add-topic dynamic-island,developer-tools,claude-code,avalonia,dotnet,windows,macos

echo "Done: https://github.com/$OWNER/$REPO"
echo "The release workflow is running: https://github.com/$OWNER/$REPO/actions"
