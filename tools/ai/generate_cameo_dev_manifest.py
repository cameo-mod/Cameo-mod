#!/usr/bin/env python3
"""Generate a DEV-only manifest with package-local includes expanded.

OpenRA reads manifest Includes from the selected mod package before its
filesystem mounts exist, so a separate cameo-dev package cannot borrow the
release package's bare Include paths directly.
"""

import argparse
from pathlib import Path, PurePosixPath


def generate(cameo_dir: Path) -> str:
    root = cameo_dir.resolve(strict=True)
    manifest = (root / "mod.yaml").read_text(encoding="utf-8").replace("\r\n", "\n")
    changes = (
        ("\t\t$cameo: cameo", "\t\t^EngineDir|mods/cameo: cameo"),
        ("\nSequences:\n", "\n\tcameo|rules/dev/logging.yaml\nSequences:\n"),
        ("Assemblies: OpenRA.Mods.Cameo.Unified.dll,", "Assemblies: OpenRA.Mods.Cameo.Dev.dll, OpenRA.Mods.Cameo.Unified.dll,"),
        # ABLATE-01: the DEV-only ServerTrait that writes the synced cameo-ablate lobby option. It lives in the DEV
        # assembly, so only this generated DEV manifest may name it (the release manifest never does).
        ("\n\tCameoDevSeed\n", "\n\tCameoDevSeed\n\tAblationServerDev\n"),
    )
    for old, new in changes:
        if manifest.count(old) != 1:
            raise ValueError("release manifest shape changed")
        manifest = manifest.replace(old, new, 1)
    output = []
    include_count = 0
    for line in manifest.splitlines(keepends=True):
        if not line.startswith("Include: "):
            output.append(line)
            continue
        relative = PurePosixPath(line[len("Include: "):].strip())
        if relative.is_absolute() or ".." in relative.parts:
            raise ValueError("unsafe manifest include")
        path = (root / Path(*relative.parts)).resolve(strict=True)
        if not path.is_relative_to(root) or not path.is_file():
            raise ValueError("manifest include escapes the release package")
        included = path.read_text(encoding="utf-8")
        if any(item.startswith("Include: ") for item in included.splitlines()):
            raise ValueError("nested manifest include needs explicit review")
        output.append(included.rstrip("\r\n") + "\n")
        include_count += 1

    if include_count == 0:
        raise ValueError("release manifest unexpectedly has no includes")
    return "".join(output).replace("\r\n", "\n")


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("cameo_dir", type=Path)
    parser.add_argument("output", type=Path)
    args = parser.parse_args()
    result = generate(args.cameo_dir)
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(result, encoding="utf-8", newline="\n")


if __name__ == "__main__":
    main()
