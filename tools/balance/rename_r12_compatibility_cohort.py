#!/usr/bin/env python3
"""Rename the R12 36-template compatibility cohort without changing resolved behavior.

The writer is transactional over affected active rules/weapon files. It compares canonical
resolved dumps for every active weapon definition (templates included), with the approved name map
applied to the baseline, and refuses any remaining reference to an old cohort name.
"""
from __future__ import annotations

import argparse
import hashlib
import json
import pathlib
import subprocess
import sys


ROOT = pathlib.Path(__file__).resolve().parents[2]
sys.path[:0] = [str(ROOT / "tools" / "audit"), str(ROOT / "tools" / "balance")]

from miniyaml import Ruleset  # noqa: E402
from promote_compatibility_warheads import new_template_name  # noqa: E402
from reconcile_r12_consumer_closure import proposed_inner_name  # noqa: E402
from resolved_gate import apply_map  # noqa: E402


PREFIX = "^Compatibility_"


def norm(path: pathlib.Path) -> str:
    return path.resolve().relative_to(ROOT.resolve()).as_posix()


def rename_maps(rs: Ruleset) -> tuple[dict[str, str], dict[str, str], dict[str, str]]:
    templates = sorted(name for name in rs.weapons if name.startswith(PREFIX))
    template_map = {name: new_template_name(name) for name in templates}
    inner_map = {}
    for template in templates:
        for child in rs.weapons[template].children:
            base = child.key.lstrip("-")
            if base.startswith("Warhead@") and base.endswith("Compatibility"):
                old = base[len("Warhead@"):]
                inner_map[old] = proposed_inner_name(old)
    if len(template_map) != 36 or len(inner_map) != 36:
        raise RuntimeError(
            f"R12 cohort drift: expected 36 templates/payloads, got "
            f"{len(template_map)}/{len(inner_map)}")
    if len(set(template_map.values())) != len(template_map):
        raise RuntimeError("proposed template names are not unique")
    if len(set(inner_map.values())) != len(inner_map):
        raise RuntimeError("proposed payload names are not unique")
    template_collisions = sorted(set(template_map.values()) & set(rs.weapons))
    if template_collisions:
        raise RuntimeError(f"proposed template name collision: {template_collisions}")
    full_map = dict(template_map)
    for old, new in inner_map.items():
        full_map["Warhead@" + old] = "Warhead@" + new
        full_map["-Warhead@" + old] = "-Warhead@" + new
    return template_map, inner_map, full_map


def source_replacements(template_map: dict[str, str], inner_map: dict[str, str]) -> dict[str, str]:
    out = dict(template_map)
    for old, new in inner_map.items():
        out["Warhead@" + old] = "Warhead@" + new
    return out


def active_sources(rs: Ruleset) -> list[pathlib.Path]:
    return sorted(set(rs.manifest.rules + rs.manifest.weapons))


def affected_sources(paths: list[pathlib.Path], replacements: dict[str, str]) -> list[pathlib.Path]:
    needles = tuple(old.encode("utf-8") for old in replacements)
    return [path for path in paths if any(needle in path.read_bytes() for needle in needles)]


def dirty_paths(paths: list[pathlib.Path]) -> list[str]:
    if not paths:
        return []
    rel = [norm(path) for path in paths]
    proc = subprocess.run(
        ["git", "status", "--porcelain=v1", "--", *rel], cwd=ROOT,
        check=True, capture_output=True, text=True, encoding="utf-8")
    return [line for line in proc.stdout.splitlines() if line.strip()]


def rewrite(paths: list[pathlib.Path], replacements: dict[str, str]) -> dict[str, int]:
    counts = {old: 0 for old in replacements}
    ordered = sorted(replacements.items(), key=lambda item: len(item[0]), reverse=True)
    for path in paths:
        raw = path.read_text(encoding="utf-8-sig")
        for old, new in ordered:
            found = raw.count(old)
            if found:
                raw = raw.replace(old, new)
                counts[old] += found
        path.write_text(raw, encoding="utf-8", newline="")
    return counts


def canonical_node(node, name_map: dict[str, str]):
    return [
        apply_map(str(node.key), name_map),
        apply_map(str(node.value or "").strip(), name_map),
        [canonical_node(child, name_map) for child in node.children],
    ]


def resolved_dump(rs: Ruleset, name_map: dict[str, str]) -> bytes:
    rows = {}
    for name in sorted(rs.weapons):
        resolved = rs.resolve_weapon(name)
        if resolved is None:
            raise RuntimeError(f"could not resolve active weapon definition {name}")
        mapped_name = apply_map(name, name_map)
        if mapped_name in rows:
            raise RuntimeError(f"resolved dump name collision: {mapped_name}")
        rows[mapped_name] = canonical_node(resolved, name_map)
    return (json.dumps(rows, ensure_ascii=False, sort_keys=True, separators=(",", ":")) + "\n").encode(
        "utf-8")


def stale_source_references(paths: list[pathlib.Path], template_map: dict[str, str],
                            inner_map: dict[str, str]) -> list[dict]:
    tokens = list(template_map) + ["Warhead@" + old for old in inner_map]
    out = []
    for path in paths:
        for line_no, line in enumerate(path.read_text(encoding="utf-8-sig").splitlines(), 1):
            found = [token for token in tokens if token in line]
            if found:
                out.append({"file": norm(path), "line": line_no, "tokens": found})
    return out


def proof_report(before: Ruleset, after: Ruleset, paths: list[pathlib.Path], counts: dict[str, int],
                 template_map: dict[str, str], inner_map: dict[str, str],
                 full_map: dict[str, str]) -> dict:
    baseline = resolved_dump(before, full_map)
    candidate = resolved_dump(after, {})
    stale = stale_source_references(active_sources(after), template_map, inner_map)
    concrete = sum(not name.startswith("^") for name in after.weapons)
    templates = len(after.weapons) - concrete
    return {
        "schema": 1,
        "scope": "R12 36-template pure rename across active rules and weapons",
        "counts": {
            "active_weapon_definitions": len(after.weapons),
            "active_concrete_weapons": concrete,
            "active_weapon_templates": templates,
            "template_renames": len(template_map),
            "payload_renames": len(inner_map),
            "affected_active_files": len(paths),
            "source_replacements": sum(counts.values()),
            "stale_old_name_references": len(stale),
        },
        "resolved_dump": {
            "byte_identical_after_baseline_name_map": baseline == candidate,
            "bytes": len(candidate),
            "mapped_baseline_sha256": hashlib.sha256(baseline).hexdigest(),
            "candidate_sha256": hashlib.sha256(candidate).hexdigest(),
        },
        "affected_files": [norm(path) for path in paths],
        "replacement_counts": counts,
        "template_renames": template_map,
        "payload_renames": inner_map,
        "stale_references": stale,
        "conclusion": (
            "The candidate is acceptable only when the canonical resolved dumps are byte-identical "
            "after applying the 36-template/36-payload name map to the baseline and no active "
            "source retains an old cohort name."
        ),
    }


def run(*, apply: bool, proof_path: pathlib.Path | None = None) -> dict:
    before = Ruleset(str(ROOT))
    template_map, inner_map, full_map = rename_maps(before)
    replacements = source_replacements(template_map, inner_map)
    sources = active_sources(before)
    affected = affected_sources(sources, replacements)
    proof_target = (ROOT / proof_path).resolve() if proof_path else None
    if proof_target is not None and proof_target in {path.resolve() for path in sources}:
        raise RuntimeError(f"proof output overlaps active source: {proof_target}")
    dirty = dirty_paths(affected)
    if dirty:
        raise RuntimeError("affected active files are dirty:\n" + "\n".join(dirty))
    snapshots = {path: path.read_bytes() for path in affected}
    completed = False
    try:
        counts = rewrite(affected, replacements)
        after = Ruleset(str(ROOT))
        report = proof_report(
            before, after, affected, counts, template_map, inner_map, full_map)
        if not report["resolved_dump"]["byte_identical_after_baseline_name_map"]:
            raise RuntimeError("resolved dump changed outside the approved name map")
        if report["counts"]["stale_old_name_references"]:
            raise RuntimeError(
                f"{report['counts']['stale_old_name_references']} stale old-name references remain")
        if proof_target:
            proof_target.parent.mkdir(parents=True, exist_ok=True)
            proof_target.write_text(
                json.dumps(report, indent=2, sort_keys=True) + "\n", encoding="utf-8")
        if not apply:
            for path, raw in snapshots.items():
                path.write_bytes(raw)
        completed = True
        return report
    finally:
        if not completed:
            for path, raw in snapshots.items():
                path.write_bytes(raw)


def main(argv=None) -> int:
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("--apply", action="store_true")
    parser.add_argument("--proof", type=pathlib.Path)
    args = parser.parse_args(argv)
    try:
        report = run(apply=args.apply, proof_path=args.proof)
    except (RuntimeError, OSError, subprocess.CalledProcessError) as exc:
        print(f"REFUSED: {exc}", file=sys.stderr)
        return 1
    print(json.dumps({"mode": "apply" if args.apply else "dry-run", **report["counts"],
                      **report["resolved_dump"]}, indent=2, sort_keys=True))
    if not args.apply:
        print("dry run: affected files restored byte-for-byte")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
