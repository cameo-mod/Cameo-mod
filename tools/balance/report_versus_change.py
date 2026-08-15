#!/usr/bin/env python3
"""report_versus_change.py — what a `^Warhead_*` regeneration did to the profiles.

The usual retrofit check, `tools/audit/review_resolve_diff.py`, deliberately
IGNORES Versus tables ("intended changes ... are NOT flagged"), so it is blind to
the one thing a W13 regeneration changes. This is the instrument for that change:
it reads the `^Warhead_<Family>_<Level>` blocks out of two revisions of
`weapons.yaml` and reports, per family and level, how the profile moved.

    python tools/balance/report_versus_change.py                 # HEAD vs worktree
    python tools/balance/report_versus_change.py <rev>           # <rev> vs worktree
    python tools/balance/report_versus_change.py <rev> --write   # + docs/balance/versus_change.md

`mean` is the number that matters for pricing: `K` is a share-weighted average of
the profile, so a family whose mean moves 1.4x is a family whose priced DPS moves
1.4x and whose `Damage` the balance pipeline must then re-solve. `span` is the
number that matters for gameplay — it is the counter-play the rebuild exists to
restore.
"""
from __future__ import annotations

import pathlib
import statistics
import subprocess
import sys

ROOT = pathlib.Path(__file__).resolve().parents[2]
YAML = "mods/cameo/weapons/weapons.yaml"
OUT = ROOT / "docs" / "balance" / "versus_change.md"

if hasattr(sys.stdout, "reconfigure"):
    sys.stdout.reconfigure(encoding="utf-8")

# Not armor types: `Shield` is the W21 layer (its own rule) and `HAZMAT` is a
# damage-type gate. Neither belongs in a profile's shape statistics.
NOT_ARMOR = {"Shield", "HAZMAT"}


def profiles(text: str) -> dict[str, dict[str, int]]:
    """`^Warhead_X` -> {armor: value} for the MAIN warhead of each template.

    The main warhead is the first `Warhead@...: AreaDamage` in the block; the
    `_Percentage` twin and the `_ExtraDamage` chip carry their own tables and are
    reported separately by their own key so a chip change cannot masquerade as a
    profile change.
    """
    out: dict[str, dict[str, int]] = {}
    template = warhead = None
    in_versus = False
    for line in text.split("\n"):
        if not line.strip() or line.lstrip().startswith("#"):
            continue
        if line[0] not in " \t":
            template = line.rstrip()[:-1] if line.rstrip().endswith(":") else None
            warhead = None
            in_versus = False
            continue
        if template is None or not template.startswith("^Warhead_"):
            continue
        stripped = line.strip()
        depth = len(line) - len(line.lstrip("\t"))
        if depth == 1 and stripped.startswith("Warhead@"):
            key = stripped.split(":", 1)[0][len("Warhead@"):]
            warhead = key
            in_versus = False
            continue
        if depth == 2:
            in_versus = stripped == "Versus:"
            continue
        if depth == 3 and in_versus and warhead is not None and ":" in stripped:
            armor, _, value = stripped.partition(":")
            try:
                out.setdefault(f"{template}|{warhead}", {})[armor.strip()] = int(value)
            except ValueError:
                pass
    return out


def stats(profile: dict[str, int]) -> tuple[float, int, int, int]:
    values = [v for a, v in profile.items() if a not in NOT_ARMOR]
    if not values:
        return (0.0, 0, 0, 0)
    return (statistics.fmean(values), max(values), min(values), max(values) - min(values))


def show(rev: str) -> str:
    if rev == "WORKTREE":
        return (ROOT / YAML).read_text(encoding="utf-8")
    return subprocess.run(["git", "show", f"{rev}:{YAML}"], cwd=ROOT,
                          capture_output=True, text=True, check=True).stdout


def render(before: dict, after: dict) -> tuple[str, list[float]]:
    lines = ["| template | warhead | mean before | mean after | ratio | span before | span after |",
             "|---|---|--:|--:|--:|--:|--:|"]
    ratios = []
    for key in sorted(set(before) & set(after)):
        b, a = stats(before[key]), stats(after[key])
        if before[key] == after[key]:
            continue
        ratio = a[0] / b[0] if b[0] else 0.0
        ratios.append(ratio)
        template, _, warhead = key.partition("|")
        lines.append(f"| `{template}` | `{warhead}` | {b[0]:.1f} | {a[0]:.1f} | "
                     f"**{ratio:.2f}x** | {b[3]} | {a[3]} |")
    only_before = sorted(set(before) - set(after))
    only_after = sorted(set(after) - set(before))
    if only_before:
        lines += ["", "**Gone:** " + ", ".join(f"`{k}`" for k in only_before)]
    if only_after:
        lines += ["", "**New:** " + ", ".join(f"`{k}`" for k in only_after)]
    return "\n".join(lines), ratios


def main() -> int:
    args = [a for a in sys.argv[1:] if not a.startswith("--")]
    rev = args[0] if args else "HEAD"
    before, after = profiles(show(rev)), profiles(show("WORKTREE"))
    table, ratios = render(before, after)
    print(f"# Versus profile change: {rev} -> worktree\n")
    print(table)
    if ratios:
        print(f"\n{len(ratios)} warhead table(s) changed; mean lethality ratio "
              f"{statistics.fmean(ratios):.2f}x (min {min(ratios):.2f} max {max(ratios):.2f})")
        print("\n⚠ `ratio` is the factor the pricing formula must absorb: K is a "
              "share-weighted average of the profile, so a 1.4x mean is 1.4x priced "
              "DPS until `apply_balance` re-solves Damage (needs a maintainer order).")
    else:
        print("\nno profile changed")
    if "--write" in sys.argv[1:]:
        OUT.write_text(f"# Versus profile change: {rev} -> worktree\n\n{table}\n",
                       encoding="utf-8")
        print(f"\nwrote {OUT.relative_to(ROOT)}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
