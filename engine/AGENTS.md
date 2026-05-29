# Repository Guidelines
## Project Structure & Module Organization
OpenRA is a C# solution under `OpenRA.sln`. Core engine and shared traits live in `OpenRA.Game/`, while launchers sit in `OpenRA.Launcher/` and `OpenRA.WindowsLauncher/`. Server tooling is under `OpenRA.Server/`, utilities in `OpenRA.Utility/`, and automated checks in `OpenRA.Test/`. Game content is organized inside `mods/`, with each mod (`ra`, `cnc`, `d2k`, etc.) split into rules under `<mod>/` and art/sound payloads under `<mod>-content/`. Build artifacts land in `bin/`, so keep working assets out of that directory.

## Build, Test, and Development Commands
Use `make CONFIGURATION=Debug` (or `.\make.ps1 build Debug`) to compile against .NET 6 and refresh required data files. `make test` (or `.\make.ps1 test`) validates mod YAML and scripting errors early. Run the managed unit suite with `dotnet test OpenRA.Test/OpenRA.Test.csproj -c Debug`. Launch the game binaries via `.\launch-game.cmd` on Windows or `./launch-game.sh` on Unix. Re-run `make clean` when switching between Mono and .NET builds to avoid stale `obj/` output.

## Coding Style & Naming Conventions
`.editorconfig` enforces tab indentation (4-wide) for C#, YAML, Lua, and scripts, LF newlines, and trimmed trailing whitespace. Follow the OpenRA coding standard: PascalCase for classes, camelCase for locals, and UPPER_SNAKE_CASE only for constants. Public APIs need XML doc summaries. `make check` enables StyleCop and build-as-warning rules; fix violations before requesting review.

## Testing Guidelines
Tests live beside their feature namespaces inside `OpenRA.Test/*`, using NUnit with `[Test]` methods and filenames ending in `*Test.cs`. Add regression coverage next to the module you touch, and name new fixtures after the class under test. Run `dotnet test` before pushing. For mod content, pair gameplay changes with a scenario or rules check using `make test` to catch YAML regressions.

## Commit & Pull Request Guidelines
Commits in this fork stay short, lowercase, and imperative (for example, `fix debug error`). Squash noisy work-in-progress commits before sharing. Rebase onto the `bleed` branch, ensure your change compiles and tests cleanly, and call out any required assets. Pull requests should link relevant issues, note balance/UI impacts, and propose a changelog blurb in the discussion, per `CONTRIBUTING.md`.

## Legacy Trait Policy
Prefer additive traits over modifying long-lived multipliers. Leave existing reload/speed traits unchanged unless a bug fix is unavoidable; fit new behaviour into fresh traits or internal conversions so downstream mods retain expected semantics. Document any exceptions inline in YAML or info classes.