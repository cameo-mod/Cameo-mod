#!/usr/bin/env python3
"""Apply an increment's behaviour switches to a FROZEN A/B worktree (AI_MASTER_PLAN §1.2 step 6, amended 2026-10-01).

    python tools/ai/apply_increment_switches.py <worktree> --groups A_squad_tactics,B_ownership_engineers_missions
    python tools/ai/apply_increment_switches.py <worktree> --groups D_zone_topology --dry-run

Reads tools/ai/increment_switches.yaml from THIS repo and edits <worktree>/mods/cameo/ai/*.yaml in place: under every
instance of each named trait (`Trait:` or `Trait@name:`, except the `skip` list) it sets each field — replacing an
existing line of that field or inserting one right under the trait header. Prints every change; refuses to run on
the main checkout. Never commit the result: master keeps the defaults until the increment's A/B decides.
"""
from __future__ import annotations

import argparse
import hashlib
import json
import pathlib
import re
import sys

REPO = pathlib.Path(__file__).resolve().parents[2]
SPEC = REPO / "tools" / "ai" / "increment_switches.yaml"


def load_effect_classes(path: pathlib.Path, groups: dict) -> dict[str, str]:
    """Metadata is separate from trait patches; missing/duplicate/unknown classes fail closed."""
    classes = {}
    active = False
    seen_section = False
    for raw in path.read_text(encoding="utf-8").splitlines():
        line = raw.split("#", 1)[0].rstrip()
        if not line.strip():
            continue
        indent = len(line) - len(line.lstrip(" "))
        if indent == 0:
            active = line == "effect_classes:"
            if active:
                if seen_section:
                    raise ValueError("duplicate effect_classes section")
                seen_section = True
        elif active:
            key, separator, value = line.strip().partition(":")
            value = value.strip()
            if indent != 2 or not separator or key in classes or value not in {"restraint", "capability", "neutral"}:
                raise ValueError("invalid/duplicate effect class")
            classes[key] = value
    if set(classes) != set(groups):
        raise ValueError(f"effect_classes keys must equal groups: missing={sorted(set(groups)-set(classes))}, extra={sorted(set(classes)-set(groups))}")
    return classes


def restraint_budget(names: list[str], classes: dict[str, str], baseline: dict[str, str] | None = None,
                     reclassification_note: str = "") -> dict:
    """At most one newly armed restraint; a capability->restraint change also counts."""
    baseline = {} if baseline is None else baseline
    if len(names) != len(set(names)) or set(names) - set(classes) or set(baseline) - set(classes):
        raise ValueError("duplicate/unknown selected or baseline group")
    if any(c not in {"restraint", "capability", "neutral"} for c in baseline.values()):
        raise ValueError("invalid baseline class")
    changed = sorted(n for n in names if n in baseline and baseline[n] != classes[n])
    if changed and not reclassification_note.strip():
        raise ValueError("class changes require explicit reviewed reclassification_note")
    restraints = sorted(n for n in names if classes[n] == "restraint" and baseline.get(n) != "restraint")
    return {"baseline_groups": sorted(baseline), "selected_groups": names,
            "added_groups": sorted(set(names)-set(baseline)), "reclassified_groups": changed,
            "new_restraint_groups": restraints, "new_restraint_count": len(restraints),
            "ordinary_increment_allowed": len(restraints) <= 1}


def verified_manifest(path: pathlib.Path | None, expected_sha: str | None) -> dict | None:
    """The pin identifies externally reviewed input; this tool cannot certify lead approval."""
    if path is None and expected_sha is None:
        return None
    if path is None or expected_sha is None or not re.fullmatch(r"[0-9a-fA-F]{64}", expected_sha):
        raise ValueError("manifest requires its reviewed exact SHA256 pin")
    with path.open("rb") as stream:
        data = stream.read(65537)
    if len(data) > 65536 or hashlib.sha256(data).hexdigest() != expected_sha.lower():
        raise ValueError("manifest size/digest mismatch")
    def unique(pairs):
        result = {}
        for key, value in pairs:
            if key in result:
                raise ValueError("duplicate manifest key")
            result[key] = value
        return result
    result = json.loads(data, object_pairs_hook=unique)
    if not isinstance(result, dict) or type(result.get("version")) is not int or result["version"] != 1:
        raise ValueError("unsupported increment manifest")
    if not isinstance(result.get("approval_receipt"), str) or not result["approval_receipt"].strip():
        raise ValueError("missing external lead review receipt")
    return result


def baseline_is_armed(tree: pathlib.Path, baseline: dict, groups: dict, skip: list[str]) -> bool:
    """Baseline claims cannot hide a newly armed restraint: require its actual current patches."""
    texts = [p.read_text(encoding="utf-8") for p in sorted((tree / "mods/cameo/ai").glob("*.yaml"))]
    for group in baseline:
        for trait, fields in groups[group].items():
            hits = False
            pattern = re.compile(rf"^\t({re.escape(trait)}(@[A-Za-z0-9_]+)?):\s*$", re.M)
            for text in texts:
                selected = [m.group(1) for m in pattern.finditer(text) if m.group(1) not in skip]
                if not selected:
                    continue
                hits = True
                if apply(text, trait, fields, skip)[1]:
                    return False
            if not hits:
                return False
    return True


def group_patch_sha256(patches: dict) -> str:
    """Canonical policy patch identity, independent of comments/order/line endings."""
    return hashlib.sha256(json.dumps(patches, sort_keys=True, separators=(",", ":")).encode("utf-8")).hexdigest()


def verify_baseline_patches(baseline: dict, pins: dict, groups: dict) -> None:
    if not isinstance(pins, dict) or set(pins) != set(baseline):
        raise ValueError("baseline_patch_sha256 keys must equal baseline_classes")
    for group in baseline:
        pin = pins[group]
        if not isinstance(pin, str) or pin != group_patch_sha256(groups[group]):
            raise ValueError(f"baseline policy changed for {group}; remove it from baseline and count as newly armed")


def load_spec(path: pathlib.Path) -> tuple[list[str], dict[str, dict[str, dict[str, str]]], dict[str, list[str]]]:
    """A tiny reader for this file's fixed shape (skip list + needs map + groups → trait → field: value); comments ignored."""
    skip: list[str] = []
    groups: dict[str, dict[str, dict[str, str]]] = {}
    needs: dict[str, list[str]] = {}
    group = trait = section = None
    for raw in path.read_text(encoding="utf-8").splitlines():
        line = raw.split("#", 1)[0].rstrip()
        if not line.strip():
            continue
        indent = len(line) - len(line.lstrip(" "))
        key, _, value = line.strip().partition(":")
        value = value.strip()
        if indent == 0:
            section = key
            if key == "skip":
                skip = [s.strip() for s in value.strip("[]").split(",") if s.strip()]
        elif indent == 2 and section == "needs":
            needs[key] = [s.strip() for s in value.strip("[]").split(",") if s.strip()]
        elif indent == 2 and section == "groups":
            group = key
            groups[group] = {}
        elif indent == 4 and section == "groups" and group:
            trait = key
            groups[group][trait] = {}
        elif indent == 6 and section == "groups" and group and trait:
            groups[group][trait][key] = value
    return skip, groups, needs


def apply(text: str, trait: str, fields: dict[str, str], skip: list[str]) -> tuple[str, list[str]]:
    """Set `fields` under every instance header of `trait` (tab-indented MiniYaml)."""
    lines = text.split("\n")
    changes: list[str] = []
    header = re.compile(rf"^\t({re.escape(trait)}(@[A-Za-z0-9_]+)?):\s*$")
    i = 0
    while i < len(lines):
        m = header.match(lines[i].rstrip("\r"))
        if not m or m.group(1) in skip:
            i += 1
            continue
        start = i + 1
        end = start
        while end < len(lines) and (lines[end].startswith("\t\t") or not lines[end].strip()):
            end += 1
        eol = "\r" if lines[i].endswith("\r") else ""
        for field, value in fields.items():
            hit = next((j for j in range(start, end) if re.match(rf"^\t\t{re.escape(field)}:", lines[j])), None)
            new = f"\t\t{field}: {value}{eol}"
            if hit is not None:
                if lines[hit] != new:
                    changes.append(f"{m.group(1)}.{field}: {lines[hit].strip()} -> {value}")
                    lines[hit] = new
            else:
                lines.insert(start, new)
                end += 1
                changes.append(f"{m.group(1)}.{field}: (default) -> {value}")
        i = end
    return "\n".join(lines), changes


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    ap.add_argument("worktree", type=pathlib.Path)
    ap.add_argument("--groups", required=True, help="comma-separated group names, or 'all'")
    ap.add_argument("--dry-run", action="store_true")
    ap.add_argument("--spec", type=pathlib.Path, default=SPEC,
                    help="switch spec to read (default: tools/ai/increment_switches.yaml); tune_build_order.py --propose writes experiment specs")
    ap.add_argument("--increment-manifest", type=pathlib.Path,
                    help="externally reviewed version1 manifest; baseline_classes, selected_groups, approval_receipt")
    ap.add_argument("--manifest-sha256", help="exact externally approved increment-manifest digest")
    args = ap.parse_args()
    sys.stdout.reconfigure(encoding="utf-8")

    tree = args.worktree.resolve()
    if (tree / ".git").is_dir():  # the main checkout has a .git DIRECTORY; worktrees have a .git file
        print("refusing: apply switches only to a frozen A/B worktree, never the main checkout", file=sys.stderr)
        return 2

    try:
        skip, groups, needs = load_spec(args.spec)
        names = list(groups) if args.groups == "all" else args.groups.split(",")
        classes = load_effect_classes(args.spec, groups)
        manifest = verified_manifest(args.increment_manifest, args.manifest_sha256)
        baseline = {} if manifest is None else manifest.get("baseline_classes", {})
        if not isinstance(baseline, dict):
            raise ValueError("baseline_classes must be a mapping")
        if manifest is not None and manifest.get("selected_groups") != names:
            raise ValueError("manifest selected_groups must exactly match requested order")
        note = "" if manifest is None else manifest.get("reclassification_note", "")
        if not isinstance(note, str):
            raise ValueError("reclassification_note must be a string")
        budget = restraint_budget(names, classes, baseline, note)
        verify_baseline_patches(baseline, {} if manifest is None else manifest.get("baseline_patch_sha256", {}), groups)
        if not baseline_is_armed(tree, baseline, groups, skip):
            raise ValueError("baseline restraint/capability patches are not already armed in this tree")
        combination = manifest is not None and manifest.get("purpose") == "combination_test" and manifest.get("campaign_allowed") is False
        if not budget["ordinary_increment_allowed"] and not combination:
            raise ValueError(f"restraint budget exceeded: {budget['new_restraint_groups']}; use one new restraint, or an exact lead-reviewed combination-test manifest (never campaign)")
        budget["purpose"] = "combination_test" if combination else "ordinary_increment"
        # Ordinary budget acceptance is not campaign authorization either.
        budget["campaign_allowed"] = False if combination else None
        budget["approval_manifest_sha256"] = args.manifest_sha256
        budget["selected_patch_sha256"] = {n: group_patch_sha256(groups[n]) for n in names}
    except (OSError, ValueError, TypeError, KeyError) as e:
        print(f"refusing: {e}", file=sys.stderr)
        return 2
    unknown = [n for n in names if n not in groups]
    if unknown:
        print(f"unknown group(s): {unknown}; known: {list(groups)}", file=sys.stderr)
        return 2
    missing = {n: [d for d in needs.get(n, []) if d not in names] for n in names}
    missing = {n: deps for n, deps in missing.items() if deps}
    if missing:
        print(f"unmet switch dependencies: {missing} (declare satisfied-by-default deps in the spec's comments only)", file=sys.stderr)
        return 2
    stray = sorted(set(needs) - set(groups))
    if stray:
        print(f"WARNING: needs entries name unknown groups: {stray}", file=sys.stderr)

    print("restraint budget: " + json.dumps(budget, sort_keys=True))

    wanted: dict[str, dict[str, str]] = {}
    for n in names:
        for trait, fields in groups[n].items():
            wanted.setdefault(trait, {}).update(fields)

    total = 0
    seen: set[str] = set()
    for f in sorted((tree / "mods" / "cameo" / "ai").glob("*.yaml")):
        text = f.read_text(encoding="utf-8")
        new = text
        for trait, fields in wanted.items():
            new, changes = apply(new, trait, fields, skip)
            for c in changes:
                print(f"{f.name}: {c}")
            total += len(changes)
            if changes:
                seen.add(trait)
        if new != text and not args.dry_run:
            f.write_text(new, encoding="utf-8", newline="")

    missing = sorted(set(wanted) - seen)
    if missing:
        print(f"WARNING: no instance changed for {missing} (already set, or trait not in ai yaml)")
    print(f"{total} change(s){' (dry run)' if args.dry_run else ''} for groups {names}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
