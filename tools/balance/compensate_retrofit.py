#!/usr/bin/env python3
"""Restore mean output on weapons whose warheads MERGED during the retrofit batch.

`retrofit_legacy_template.py` converts one template per run. A weapon that inherits
SEVERAL legacy templates mapping into the SAME family therefore gets converted several
times, and once two of its warheads carry the same key MiniYaml merges them into one
node — the smaller one's damage simply disappears. `GladiusCannon` inherits
`^MediumCannon` + `^HeavyCannon` + `^TankDestroyerCannon` while already carrying
`CannonHE_Medium`/`CannonHE_Heavy`/`CannonAP_Light`, and lost 30 000 damage that way.

Compensating inside each per-template run cannot fix it: run N computes its correction
from a tree that runs 1..N-1 have already changed, so the corrections compound and never
converge. This runs ONCE over the finished batch and compares against the ORIGINAL tree,
which makes it exact.

    python tools/balance/compensate_retrofit.py --rev HEAD
    python tools/balance/compensate_retrofit.py --rev HEAD --apply

⚠ KNOWN GAP (2026-08-16): this closes 29 of the 33 drifting weapons. The three cannon
TEMPLATES (`^HeavyCannon`, `^MediumCannon`, `^TankDestroyerCannon`) get WORSE, because
their resolved `Damage` reads 2000 (the family template's own value) while the file says
838 — the written override is not winning the merge, and the cause is not yet understood.
Do NOT run this on templates until that is explained; restrict it to concrete weapons, or
find why the override loses. The 4th, `japan_imperialscoutsman_rifle_waveforce`, also has
Spread/Falloff drift (100 -> 250) and needs the same investigation.

Effective damage is linear in `Damage`, so restoring the weapon's mean is a single
closed-form solve: scale the surviving family warhead by whatever factor makes the sum
match again. This preserves resolved behaviour (CLAUDE.md rule 5). Whether a weapon
*should* carry three warheads of one family is a separate design question — the answer
changes its balance, not its conversion.
"""
from __future__ import annotations

import argparse
import pathlib
import shutil
import subprocess
import sys
import tempfile

ROOT = pathlib.Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "tools" / "audit"))
sys.path.insert(0, str(ROOT / "tools" / "balance"))
import miniyaml  # noqa: E402
from retrofit_legacy_template import YamlFile, weapon_files  # noqa: E402
from verify_retrofit import NOT_ARMOR, effective, main_warheads  # noqa: E402

if hasattr(sys.stdout, "reconfigure"):
    sys.stdout.reconfigure(encoding="utf-8")


def mean_output(warheads, armors) -> float:
    if not armors or not warheads:
        return 0.0
    eff = effective(warheads, armors)
    return sum(eff.values()) / len(armors)


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__,
                                 formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--rev", default="HEAD", help="the pre-retrofit revision")
    ap.add_argument("--apply", action="store_true", help="write the files")
    ap.add_argument("--tolerance", type=float, default=0.02)
    args = ap.parse_args()

    tmp = pathlib.Path(tempfile.mkdtemp(prefix="cameo_comp_"))
    base = tmp / "base"
    try:
        subprocess.run(["git", "worktree", "add", "--detach", str(base), args.rev],
                       cwd=ROOT, check=True, capture_output=True)
        before = miniyaml.Ruleset(base)
        after = miniyaml.Ruleset(ROOT)

        # Which weapon lives in which file, so a fix lands in the weapon's OWN block.
        index: dict[str, pathlib.Path] = {}
        for path in weapon_files():
            f = YamlFile(path)
            for i, line in enumerate(f.lines):
                if line.endswith(":") and YamlFile.indent(line) == 0:
                    index.setdefault(line[:-1], path)

        fixes: dict[pathlib.Path, list[tuple[str, str, int]]] = {}
        skipped = []
        for name in sorted(set(before.weapons) & set(after.weapons)):
            wb, wa = main_warheads(before, name), main_warheads(after, name)
            if not wb or not wa:
                continue
            armors = sorted({k for _, t in wb + wa for k in t} - NOT_ARMOR)
            if not armors:
                continue
            mb, ma = mean_output(wb, armors), mean_output(wa, armors)
            if mb <= 0 or ma <= 0 or abs(ma / mb - 1.0) <= args.tolerance:
                continue

            # Pick the warhead to adjust: the family node carrying the most damage, which
            # is the one the merge collapsed into.
            res = after.resolve_weapon(name)
            best_key, best_dmg, best_mv = None, 0.0, 0.0
            for c in res.children:
                if not c.key.startswith("Warhead@"):
                    continue
                low = c.key.lower()
                if "percentage" in low or "extradamage" in low or "friendlyfire" in low:
                    continue
                d = c.child("Damage")
                vs = c.child("Versus")
                if d is None or vs is None:
                    continue
                try:
                    dmg = float((d.value or "").strip())
                except ValueError:
                    continue
                vals = []
                for a in vs.children:
                    if a.key in NOT_ARMOR:
                        continue
                    try:
                        vals.append(float((a.value or "").strip()))
                    except ValueError:
                        pass
                if dmg > best_dmg and vals:
                    best_key, best_dmg = c.key, dmg
                    best_mv = sum(vals) / len(vals) / 100.0
            if best_key is None or best_mv <= 0:
                skipped.append((name, mb, ma))
                continue

            # Linear solve: moving this one warhead by `delta` moves the mean by
            # delta * mean_versus. Restore exactly.
            delta = (mb - ma) / best_mv
            target = max(1, round(best_dmg + delta))
            path = index.get(name)
            if path is None:
                skipped.append((name, mb, ma))
                continue
            fixes.setdefault(path, []).append((name, best_key, target))

        touched = 0
        for path, items in sorted(fixes.items()):
            f = YamlFile(path)
            for name, key, target in items:
                span = f.block(name)
                if span is None:
                    continue
                node = f.node(span, key)
                if node is None:
                    f.insert(span[0], f"\t\tDamage: {target}")
                    f.insert(span[0], f"\t{key}:")
                else:
                    di = f.child(node, "Damage")
                    if di is None:
                        f.insert(node[0] + 1, f"\t\tDamage: {target}")
                    else:
                        f.replace(di, f"\t\tDamage: {target}")
                touched += 1
                print(f"  {name:38s} {key:28s} -> Damage {target}")
            if args.apply and f.dirty:
                f.save()

        print(f"\n{touched} weapons compensated in {len(fixes)} files")
        if skipped:
            print(f"{len(skipped)} could not be solved automatically:")
            for name, mb, ma in skipped[:10]:
                print(f"      {name:38s} {mb:10.1f} -> {ma:10.1f}")
        print("(dry run — pass --apply)" if not args.apply else "WRITTEN")
        return 0
    finally:
        subprocess.run(["git", "worktree", "remove", "--force", str(base)],
                       cwd=ROOT, capture_output=True)
        shutil.rmtree(tmp, ignore_errors=True)


if __name__ == "__main__":
    raise SystemExit(main())
