#!/usr/bin/env python3
"""convert_apply_to_scaled_v2.py — convert legacy flame/chemical
ApplyPhysicalState warheads to damage-scaled PhysicalStateName/Scale.

Scope:
  * ^LightFlameWeapon, ^MediumFlameWeapon, ^HeavyFlameWeapon
  * ^LightChemicalWeapon, ^MediumChemicalWeapon, ^HeavyChemicalWeapon
  * concrete weapons overriding Warhead@<Family>, Warhead@<Family>Percentage,
    Warhead@<Family>FriendlyFire or carrying Warhead@PhysicalState<Family>[FriendlyFire]

For every converted warhead:
  - SpreadDamage  -> AreaDamage
  - HealthPercentageDamage -> AreaDamagePercentage
  - Range: removed from inside the warhead
  - main: ValidRelationships: Ally, Neutral, Enemy
  - main: FriendlyFireDamage: 50 / FriendlyFireSpread: 50
  - main + percentage: PhysicalStateName / PhysicalStateScale
  - FriendlyFire and ApplyPhysicalState warheads removed

Standalone ApplyPhysicalState warheads (non-flame/chemical, e.g. cryo) are
reported and left untouched.

Usage:
    python tools/balance/convert_apply_to_scaled_v2.py           # dry run
    python tools/balance/convert_apply_to_scaled_v2.py --apply   # write files
"""

from __future__ import annotations

import argparse
import pathlib
import re
import sys

# miniyaml lives in tools/audit
sys.path.insert(0, str(pathlib.Path(__file__).resolve().parents[1] / "audit"))
from miniyaml import load, load_manifest, find_repo_root

FAMILIES = [
    "LightFlameWeapon",
    "MediumFlameWeapon",
    "HeavyFlameWeapon",
    "LightChemicalWeapon",
    "MediumChemicalWeapon",
    "HeavyChemicalWeapon",
]

FAMILY_RE = "|".join(re.escape(f) for f in FAMILIES)
MAIN_RE = re.compile(rf"^Warhead@({FAMILY_RE})$")
PERC_RE = re.compile(rf"^Warhead@({FAMILY_RE})Percentage$")
FF_RE = re.compile(rf"^Warhead@({FAMILY_RE})FriendlyFire$")
PS_RE = re.compile(rf"^Warhead@PhysicalState({FAMILY_RE})(FriendlyFire)?$")

PHYSICAL_STATE = {
    "LightFlameWeapon": "Temperature",
    "MediumFlameWeapon": "Temperature",
    "HeavyFlameWeapon": "Temperature",
    "LightChemicalWeapon": "Corrosion",
    "MediumChemicalWeapon": "Corrosion",
    "HeavyChemicalWeapon": "Corrosion",
}

# keys we actively manage inside a converted warhead
MAIN_DROP = {"Range", "ValidRelationships", "FriendlyFireDamage", "FriendlyFireSpread",
             "PhysicalStateName", "PhysicalStateScale"}
PERC_DROP = {"Range", "PhysicalStateName", "PhysicalStateScale"}


def strip_inline_comment(text: str) -> str:
    """Mirror miniyaml's _strip_comment for key extraction."""
    out = re.sub(r"(?<!\\)#.*$", "", text)
    return out.replace("\\#", "#")


def get_key(line: str) -> str | None:
    """Return the bare key of a '\\t*Key: value' line, or None."""
    body = strip_inline_comment(line).strip()
    if ":" not in body:
        return None
    return body.split(":", 1)[0].strip()


def get_prefix(line: str) -> str:
    """Return the leading whitespace (tabs) of a line."""
    stripped = line.lstrip("\t")
    return line[: len(line) - len(stripped)]


def detect_eol_and_bom(raw: bytes) -> tuple[str, bool]:
    crlf = raw.count(b"\r\n")
    lf = raw.count(b"\n") - crlf
    eol = "\r\n" if crlf >= lf else "\n"
    bom = raw.startswith(b"\xef\xbb\xbf")
    return eol, bom


def read_file_lines(path: pathlib.Path) -> tuple[list[str], str, bool]:
    raw = path.read_bytes()
    eol, bom = detect_eol_and_bom(raw)
    enc = "utf-8-sig" if bom else "utf-8"
    # text mode with default newline normalises universal newlines to '\n'
    text = path.read_text(encoding=enc, errors="replace")
    lines = text.split("\n")
    return lines, eol, bom


def write_file_lines(path: pathlib.Path, lines: list[str], eol: str, bom: bool) -> None:
    # .gitattributes enforces LF for yaml; always write LF for yaml files
    # regardless of the line ending that happened to be in the working tree.
    eol = "\n" if path.name.endswith(".yaml") else eol
    text = eol.join(lines)
    if not text.endswith(eol) and lines:
        text += eol
    enc = "utf-8-sig" if bom else "utf-8"
    path.write_text(text, encoding=enc, newline="")


def is_blank_or_comment(line: str) -> bool:
    s = line.strip()
    return not s or s.startswith("#")


def line_indent(line: str) -> int:
    return len(line) - len(line.lstrip("\t "))


def block_end(
    lines: list[str],
    start_idx: int,
    next_1based_line: int | None,
    start_indent: int,
) -> int:
    """Return 0-based exclusive end for a block that starts at start_idx,
    stopping before the next sibling (next_1based_line) and trimming any
    trailing blank/comment lines that belong to the following block."""
    if next_1based_line is None:
        end = len(lines)
    else:
        end = next_1based_line - 1  # 0-based exclusive before next sibling
    while end > start_idx + 1:
        prev = lines[end - 1]
        if is_blank_or_comment(prev) and line_indent(prev) <= start_indent:
            end -= 1
        else:
            break
    return end


def top_level_end(nodes: list, idx: int, lines: list[str]) -> int:
    """End index (exclusive) for top-level node at nodes[idx]."""
    next_line = nodes[idx + 1].line if idx + 1 < len(nodes) else None
    return block_end(lines, nodes[idx].line - 1, next_line, 0)


def child_end(
    parent_node,
    child_idx: int,
    parent_end: int,
    lines: list[str],
) -> int:
    """End index (exclusive) for a child of parent_node."""
    children = parent_node.children
    child = children[child_idx]
    if child_idx + 1 < len(children):
        next_line = children[child_idx + 1].line
    else:
        # parent_end is 0-based exclusive; block_end expects 1-based next sibling line
        next_line = parent_end + 1
    start_idx = child.line - 1
    start_indent = line_indent(lines[start_idx])
    return block_end(lines, start_idx, next_line, start_indent)


def transform_main_block(block: list[str], family: str) -> list[str]:
    state = PHYSICAL_STATE[family]
    first = block[0]
    prefix = get_prefix(first)
    key = get_key(first) or f"Warhead@{family}"
    child_prefix = prefix + "\t"

    new = [f"{prefix}{key}: AreaDamage"]
    new.append(f"{child_prefix}ValidRelationships: Ally, Neutral, Enemy")
    new.append(f"{child_prefix}FriendlyFireDamage: 50")
    new.append(f"{child_prefix}FriendlyFireSpread: 50")

    for line in block[1:]:
        k = get_key(line)
        if k in MAIN_DROP:
            continue
        new.append(line)

    new.append(f"{child_prefix}PhysicalStateName: {state}")
    new.append(f"{child_prefix}PhysicalStateScale: 300")
    return new


def transform_perc_block(block: list[str], family: str) -> list[str]:
    state = PHYSICAL_STATE[family]
    first = block[0]
    prefix = get_prefix(first)
    key = get_key(first) or f"Warhead@{family}Percentage"
    child_prefix = prefix + "\t"

    new = [f"{prefix}{key}: AreaDamagePercentage"]
    for line in block[1:]:
        k = get_key(line)
        if k in PERC_DROP:
            continue
        new.append(line)

    new.append(f"{child_prefix}PhysicalStateName: {state}")
    new.append(f"{child_prefix}PhysicalStateScale: 300")
    return new


def group_family_children(parent_node) -> dict[str, dict[str, tuple[int, int, int]]]:
    """Map family -> {kind: (child_index, start_line, end_line)}.
    kinds: main, perc, ff, ps, psff."""
    groups: dict[str, dict[str, tuple[int, int, int]]] = {}
    for i, child in enumerate(parent_node.children):
        m = MAIN_RE.match(child.key)
        if m:
            groups.setdefault(m.group(1), {})["main"] = (i, child.line, 0)
            continue
        m = PERC_RE.match(child.key)
        if m:
            groups.setdefault(m.group(1), {})["perc"] = (i, child.line, 0)
            continue
        m = FF_RE.match(child.key)
        if m:
            groups.setdefault(m.group(1), {})["ff"] = (i, child.line, 0)
            continue
        m = PS_RE.match(child.key)
        if m:
            family = m.group(1)
            kind = "psff" if m.group(2) else "ps"
            groups.setdefault(family, {})[kind] = (i, child.line, 0)
    return groups


class FileOperation:
    __slots__ = ("start", "end", "new_lines", "description")

    def __init__(self, start: int, end: int, new_lines: list[str], description: str):
        self.start = start
        self.end = end
        self.new_lines = new_lines
        self.description = description


def process_weapon(
    parent_node,
    parent_end: int,
    lines: list[str],
    standalone: list[dict],
) -> list[FileOperation]:
    """Collect edit operations for one top-level weapon/template block."""
    ops: list[FileOperation] = []

    # collect standalone ApplyPhysicalState warheads first
    for child in parent_node.children:
        if child.value == "ApplyPhysicalState" and not PS_RE.match(child.key):
            amount = ""
            for c in child.children:
                if c.key == "Amount":
                    amount = c.value
                    break
            state = ""
            for c in child.children:
                if c.key == "PhysicalStateName":
                    state = c.value
                    break
            standalone.append({
                "weapon": parent_node.key,
                "file": parent_node.file,
                "line": child.line,
                "warhead": child.key,
                "physical_state_name": state,
                "amount": amount,
            })

    groups = group_family_children(parent_node)
    if not groups:
        return ops

    # compute child end lines and build operations
    # we need the child index to compute end, so re-derive from node.children
    for family, kinds in groups.items():
        for kind, (idx, start_1based, _) in kinds.items():
            start_idx = start_1based - 1
            end_idx = child_end(parent_node, idx, parent_end, lines)
            kinds[kind] = (idx, start_1based, end_idx)

    # process removals first (descending by start index) so later operations keep valid ranges
    for family, kinds in groups.items():
        for kind in ("psff", "ps", "ff"):
            if kind in kinds:
                _, _, end_idx = kinds[kind]
                start_idx = kinds[kind][1] - 1
                ops.append(FileOperation(
                    start_idx, end_idx, [],
                    f"remove {family} {kind} from {parent_node.key}"
                ))

        if "perc" in kinds:
            start_idx = kinds["perc"][1] - 1
            end_idx = kinds["perc"][2]
            block = lines[start_idx:end_idx]
            new_block = transform_perc_block(block, family)
            ops.append(FileOperation(
                start_idx, end_idx, new_block,
                f"convert {family} percentage in {parent_node.key}"
            ))

        if "main" in kinds:
            start_idx = kinds["main"][1] - 1
            end_idx = kinds["main"][2]
            block = lines[start_idx:end_idx]
            new_block = transform_main_block(block, family)
            ops.append(FileOperation(
                start_idx, end_idx, new_block,
                f"convert {family} main in {parent_node.key}"
            ))

    return ops


def convert_file(
    path: pathlib.Path,
    apply: bool,
    standalone: list[dict],
) -> tuple[bool, int]:
    """Return (had_changes, operation_count)."""
    lines, eol, bom = read_file_lines(path)

    try:
        nodes = load(path)
    except Exception as exc:
        print(f"  [SKIP] {path}: parse error {exc}")
        return False, 0

    ops: list[FileOperation] = []
    for i, node in enumerate(nodes):
        parent_end = top_level_end(nodes, i, lines)
        weapon_ops = process_weapon(node, parent_end, lines, standalone)
        ops.extend(weapon_ops)

    if not ops:
        return False, 0

    # sort descending by start to apply safely
    ops.sort(key=lambda o: o.start, reverse=True)

    for op in ops:
        lines[op.start:op.end] = op.new_lines

    if apply:
        write_file_lines(path, lines, eol, bom)
        print(f"  [WRITE] {path}: {len(ops)} blocks")
    else:
        converted = path.parent / (path.name + ".converted")
        write_file_lines(converted, lines, eol, bom)
        print(f"  [DRY-RUN] {path}: {len(ops)} blocks -> {converted}")

    return True, len(ops)


def main() -> int:
    ap = argparse.ArgumentParser(
        description="Convert ApplyPhysicalState flame/chemical warheads to damage-scaled."
    )
    ap.add_argument("--apply", action="store_true", help="write changes to yaml files")
    args = ap.parse_args()

    repo_root = find_repo_root()
    manifest = load_manifest(repo_root)
    standalone: list[dict] = []
    changed_files = 0
    total_ops = 0

    print(f"{'DRY-RUN' if not args.apply else 'APPLY'}: scanning {len(manifest.weapons)} weapon files")
    for path in manifest.weapons:
        if not path.name.endswith(".yaml") or path.name.endswith(".converted"):
            continue
        if not path.exists():
            continue
        had, n = convert_file(path, args.apply, standalone)
        if had:
            changed_files += 1
            total_ops += n

    print(f"\nSummary: {changed_files} file(s), {total_ops} block operation(s)")

    if standalone:
        print(f"\nStandalone ApplyPhysicalState warheads (not converted; {len(standalone)}):")
        for s in standalone:
            print(f"  {s['weapon']} in {s['file']}:{s['line']} {s['warhead']} "
                  f"(state={s['physical_state_name'] or '?'}, amount={s['amount'] or '?'})")
    else:
        print("\nNo standalone ApplyPhysicalState warheads found.")

    if args.apply:
        print("\nWrote changes. Review with git diff and run audits.")
    else:
        print("\nDry-run complete. Inspect *.yaml.converted files and run with --apply.")

    return 0


if __name__ == "__main__":
    sys.exit(main())
