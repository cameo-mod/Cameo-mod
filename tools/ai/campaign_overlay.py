"""Materialize a complete manifest for an AI-only campaign overlay.

The engine parses Includes before mounting FileSystem packages. Non-AI resources
come from the pinned baseline package; only declared AI rules use the overlay.
"""
import os
import pathlib
import re
import shutil


def materialize_manifest(root, overlay, engine_root=None):
    root = pathlib.Path(root).resolve()
    mod = root / "mods" / "cameo"
    engine_root = pathlib.Path(engine_root or root / "engine").resolve()
    target = pathlib.Path(overlay).resolve() / "mods" / "cameo"
    text = (mod / "mod.yaml").read_text(encoding="utf-8")
    if (target / "mod.yaml").exists():
        raise ValueError("overlay manifest destination must be new")
    if not (target / "ai" / "ai.yaml").is_file():
        raise ValueError("dual-arm AI must be materialized first")
    include_paths = []
    for name in re.findall(r"(?m)^Include: ([^\r\n]+)$", text):
        relative = pathlib.PurePosixPath(name)
        if relative.is_absolute() or ".." in relative.parts or "\\" in name or ":" in name:
            raise ValueError("unsafe manifest include: " + name)
        source = (mod / name).resolve()
        if not source.is_relative_to(mod) or not source.is_file():
            raise ValueError("missing baseline manifest include: " + name)
        destination = target / name
        destination.parent.mkdir(parents=True, exist_ok=True)
        shutil.copyfile(source, destination)
        include_paths.append(name)
    mount = "\t\t$cameo: cameo"
    if text.count(mount + "\n") != 1:
        raise ValueError("expected one baseline cameo package mount")
    # MiniYaml splits at the first colon, including a Windows drive colon.
    # Caret paths resolve against the explicitly pinned engine directory.
    def package_path(path):
        relative = pathlib.Path(os.path.relpath(path, engine_root)).as_posix()
        return "^EngineDir|" + relative
    text = text.replace(mount + "\n",
        f"\t\t{package_path(mod)}: cameo\n\t\t{package_path(target)}: campaign_ai\n", 1)
    rules = re.findall(r"(?m)^\tcameo\|ai/([^\r\n]+)$", text)
    if not rules:
        raise ValueError("manifest has no declared AI rules")
    for name in rules:
        if not (target / "ai" / name).is_file():
            raise ValueError("overlay AI rule missing: " + name)
    text = re.sub(r"(?m)^\tcameo\|ai/", "\tcampaign_ai|ai/", text)
    (target / "mod.yaml").write_text(text, encoding="utf-8")
    return {"manifest": str(target / "mod.yaml"), "includes": include_paths,
            "ai_rules": rules, "baseline_package": str(mod)}


def launch_mod_arguments(root, engine_root, overlay):
    # InstalledMods assigns each ID repeatedly: the LAST valid manifest wins.
    # An explicit directory is appended after all search-path candidates.
    mod = pathlib.Path(overlay).resolve() / "mods" / "cameo"
    if not (mod / "mod.yaml").is_file():
        raise ValueError("complete campaign manifest missing")
    return [f"Game.Mod={mod}",
            f"Engine.ModSearchPaths={pathlib.Path(root).resolve() / 'mods'},{pathlib.Path(engine_root).resolve() / 'mods'}",
            f"Engine.EngineDir={pathlib.Path(engine_root).resolve()}"]
