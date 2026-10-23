# Contributing to Pulse

Thanks for your interest in Pulse!

## Before you start

- Read [CLAUDE.md](CLAUDE.md). Its rules apply to every contribution, whether you use Claude Code or not.
- Check the [roadmap](docs/ROADMAP.md) and open issues so work isn't duplicated.
- For anything bigger than a small fix, open an issue first to discuss the approach.

## Development

Requirements: the .NET 10 SDK, on Windows 10/11 or macOS 14+.

```bash
dotnet build Pulse.slnx
dotnet test Pulse.slnx
dotnet run --project src/Pulse.App
```

## Pull requests

- One feature or fix per pull request.
- Logic in `Pulse.Core` comes with unit tests.
- Test on the operating system you changed. Say in the PR which OS you tested on.
- Never include real tokens, usernames or private repository names in fixtures.
- Update `CHANGELOG.md` under "Unreleased".

## Adding a new activity source

A guide will live in `docs/sources.md` once the plugin system lands. Until then, open an issue describing the source and the events it would show.
