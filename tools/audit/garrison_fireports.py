#!/usr/bin/env python3
"""Migrate retired fire-port traits and audit canonical geometry/capacity.

Includes dormant YAML and map.yaml entries in packed .oramap files. Reuses the
repository MiniYaml inheritance model for mounted actor capacity checks.
"""
from __future__ import annotations

import argparse
import hashlib
import json
import os
from pathlib import Path
import re
import tempfile
import zipfile

from miniyaml import Ruleset, load_text

ROOT = Path(__file__).resolve().parents[2]
ALIASES = {"AttackOpenTopped", "AttackGarrisonedSP"}
KEY = re.compile(r"^(?P<indent>[\t ]*)(?P<remove>-?)(?P<base>AttackOpenTopped|AttackGarrisonedSP|AttackGarrisoned)(?P<suffix>@[^:]+)?:")


def migrate_text(text: str, source: str) -> tuple[str, list[dict]]:
    lines = text.splitlines(keepends=True)
    result, changes = [], []
    trait_indent = None
    for number, line in enumerate(lines, 1):
        stripped = line.lstrip("\t ")
        indent = len(line) - len(stripped)
        if stripped.strip() and not stripped.startswith("#") and trait_indent is not None and indent <= trait_indent:
            trait_indent = None
        match = KEY.match(line)
        if match:
            trait_indent = len(match["indent"]) if not match["remove"] else None
            if match["base"] in ALIASES:
                old = match["remove"] + match["base"] + (match["suffix"] or "")
                new = match["remove"] + "AttackGarrisoned" + (match["suffix"] or "")
                line = line[:len(match["indent"])] + new + line[match.end() - 1:]
                changes.append({"source": source, "line": number, "old": old, "new": new})
        if trait_indent is not None and indent > trait_indent and stripped.startswith("PerPassengerTargeting:"):
            changes.append({"source": source, "line": number, "old": stripped.strip(), "new": None})
            continue
        result.append(line)
    migrated = "".join(result)
    def validate(nodes):
        found = set()
        for node in nodes:
            if node.key.split("@", 1)[0] == "AttackGarrisoned":
                if node.key in found:
                    raise ValueError(f"{source}:{node.line}: duplicate canonical trait {node.key}")
                found.add(node.key)
            elif node.key.startswith("-AttackGarrisoned"):
                found.discard(node.key[1:])
            validate(node.children)
    validate(load_text(migrated, source))
    return migrated, changes


def documents(root: Path):
    for path in sorted((root / "mods").rglob("*")):
        if path.suffix.lower() in {".yaml", ".yml"}:
            yield path, None, path.read_text(encoding="utf-8-sig")
        elif path.suffix.lower() == ".oramap" and zipfile.is_zipfile(path):
            with zipfile.ZipFile(path) as archive:
                for name in sorted(archive.namelist()):
                    if name.lower().endswith((".yaml", ".yml")):
                        yield path, name, archive.read(name).decode("utf-8-sig")


def migrate(root: Path, write: bool) -> list[dict]:
    manifest, packed = [], {}
    for path, member, text in documents(root):
        source = path.relative_to(root).as_posix() + ("!" + member if member else "")
        new, changes = migrate_text(text, source)
        if not changes:
            continue
        for change in changes:
            change["input_sha256"] = hashlib.sha256(text.encode("utf-8")).hexdigest()
            change["output_sha256"] = hashlib.sha256(new.encode("utf-8")).hexdigest()
        manifest.extend(changes)
        if write:
            if member is None:
                path.write_text(new, encoding="utf-8", newline="")
            else:
                packed.setdefault(path, {})[member] = new.encode("utf-8")
    for path, replacements in packed.items():
        handle, temporary = tempfile.mkstemp(prefix=path.name + ".", suffix=".tmp", dir=path.parent)
        os.close(handle)
        try:
            with zipfile.ZipFile(path) as before, zipfile.ZipFile(temporary, "w") as after:
                after.comment = before.comment
                for entry in before.infolist():
                    after.writestr(entry, replacements.get(entry.filename, before.read(entry.filename)))
            os.replace(temporary, path)
        finally:
            if os.path.exists(temporary):
                os.unlink(temporary)
    return manifest


def legacy_uses(root: Path) -> list[str]:
    failures = []
    for path, member, text in documents(root):
        source = path.relative_to(root).as_posix() + ("!" + member if member else "")
        for number, line in enumerate(text.splitlines(), 1):
            match = KEY.match(line)
            if match and match["base"] in ALIASES:
                failures.append(f"{source}:{number}: retired trait {match['base']}")
    return failures


def mounted_capacity(root: Path, rules=None) -> tuple[list[dict], list[str]]:
    rules = rules if rules is not None else Ruleset(root)
    candidates = set()
    memo = {}

    def may_have_ports(name, stack=frozenset()):
        if name in memo:
            return memo[name]
        if name in stack:
            return False
        actor = rules.actor(name)
        if actor is None:
            return False
        found = any(c.key.split("@", 1)[0] == "AttackGarrisoned" for c in actor.children)
        found |= any(may_have_ports(c.value, stack | {name}) for c in actor.children if c.key.split("@", 1)[0] == "Inherits")
        memo[name] = found
        return found

    for name in rules.actors:
        if may_have_ports(name):
            candidates.add(name)
    rows, failures = [], []
    for name in sorted(candidates):
        actor = rules.resolve(name)
        capacities = [int(c.get("MaxWeight") or "0") for c in actor.children if c.key.split("@", 1)[0] in {"Cargo", "Garrisonable"}]
        capacity = sum(capacities)
        for trait in actor.children_named("AttackGarrisoned"):
            offsets = [x.strip() for x in (trait.get("PortOffsets") or "").split(",") if x.strip()]
            count = len(offsets) // 3
            if not offsets or len(offsets) % 3:
                failures.append(f"{name}: malformed PortOffsets")
            for field in ("PortYaws", "PortCones"):
                value = trait.get(field)
                if value is not None and len(value.split(",")) != count:
                    failures.append(f"{name}: {field} length differs from ports")
            overflow = (trait.get("NoFireOverflow") or "false").lower() == "true"
            rows.append({"actor": name, "trait": trait.key, "ports": count, "capacity_upper_bound": capacity, "intentional_no_fire": overflow})
            if capacity > count and not overflow:
                failures.append(f"{name}: capacity {capacity} exceeds {count} ports without NoFireOverflow acknowledgement")
    return rows, failures


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--root", type=Path, default=ROOT)
    parser.add_argument("--write", action="store_true")
    parser.add_argument("--manifest", type=Path)
    parser.add_argument("--capacity-report", type=Path)
    args = parser.parse_args()
    changes = migrate(args.root, args.write)
    if args.write and args.manifest:
        args.manifest.parent.mkdir(parents=True, exist_ok=True)
        previous = json.loads(args.manifest.read_text(encoding="utf-8"))["changes"] if args.manifest.exists() else []
        if changes or not args.manifest.exists():
            args.manifest.write_text(json.dumps({"version": 1, "changes": previous + changes}, indent=2) + "\n", encoding="utf-8")
    errors = legacy_uses(args.root)
    rows, capacity_errors = mounted_capacity(args.root)
    errors.extend(capacity_errors)
    if args.capacity_report:
        args.capacity_report.write_text(json.dumps(rows, indent=2) + "\n", encoding="utf-8")
    for error in errors:
        print(error)
    print(f"{'FAIL' if errors else 'PASS'}: {len(changes)} migration edits, {len(rows)} resolved port actors, {len(errors)} violations")
    return int(bool(errors))


if __name__ == "__main__":
    raise SystemExit(main())
