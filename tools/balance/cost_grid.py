#!/usr/bin/env python3
"""cost_grid.py — what price RESOLUTION should Cameo use, and what does a grid cost us?

    python tools/balance/cost_grid.py
    python tools/balance/cost_grid.py --atom 20 --md docs/audit/latest/cost_grid.md

⛔ THE TRAP THIS TOOL EXISTS TO CATCH: A FLAT GRID CANNOT SERVE A 1000x COST RANGE.
"Round every price to a multiple of 20" sounds like one rule and is really two claims:
that 20 is a legible ATOM (true, and 89% of the roster already obeys it) and that 20 is
the right STEP (true only near 140 credits). Cameo's median unit costs **1,200**, where a
20-credit step is 1.7% -- eight times finer than the 14.3% a player can actually perceive
(`tools/reference/peer_cost_grid.py`, 14 shipped mods, 266 adjacent-cost gaps). Snapping
to a flat 20 would therefore change almost nothing and fix nothing: the over-precision is
not in the last digit, it is in the SPACING.

So the grid this tool proposes keeps the atom and derives the step:

    step(price) = max(atom, atom * round(RESOLUTION * price / atom))
    rung        = round(price / step) * step

Every price stays a multiple of the atom -- legible, and mental arithmetic still works --
while adjacent rungs stay at least one perceptible notch apart. Near the cheap end the two
rules coincide and the grid IS a flat 20; by the median it has grown to ~160.

⚠ THIS IS A PROPOSAL TOOL, NOT AN APPLIER. It never writes yaml. Prices move through the
ledger and `apply_balance --confirm` like every other balance number (CLAUDE.md rule 3),
and a grid snap is a repricing that has to survive the band check and the boot gate.
"""
from __future__ import annotations
import argparse, collections, json, math, pathlib, statistics, sys

ROOT = pathlib.Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "tools/balance"))
import check_band as cb  # noqa: E402

# The measured cost resolution of shipped OpenRA mods. See tools/reference/peer_cost_grid.py.
RESOLUTION = 0.143


def step_for(price: float, atom: int) -> int:
    """The grid step at this price: the atom, or the nearest whole number of atoms that
    spans one perceptible notch -- whichever is larger."""
    return max(atom, atom * round(RESOLUTION * price / atom))


def snap(price: float, atom: int) -> int:
    s = step_for(price, atom)
    return max(s, int(round(price / s) * s))


def main() -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument("--atom", type=int, default=20,
                    help="the smallest legible price unit (maintainer proposal: 20)")
    ap.add_argument("--md")
    args = ap.parse_args()
    atom = args.atom

    costs = []
    for _fn, actor, u, du in cb.collect({}):
        c = cb.fnum((u.get("cost") or {}).get("v") if isinstance(u.get("cost"), dict)
                    else u.get("cost"))
        if c and c > 0 and not u.get("build_limit"):
            costs.append((c, actor))
    vals = sorted(c for c, _a in costs)

    out = []
    w = out.append
    w(f"# Cost grid — atom {atom}, resolution {RESOLUTION:.1%}\n")
    w(f"roster: **{len(vals)} buildable non-epic priced units**, "
      f"**{min(vals):,.0f} – {max(vals):,.0f}** credits "
      f"(**{max(vals)/min(vals):,.0f}x** range), median **{statistics.median(vals):,.0f}**\n")

    w("## ⛔ Why a FLAT atom is not a grid\n")
    w(f"| price | a flat {atom} as a % | verdict | the derived step here |")
    w("|--:|--:|---|--:|")
    for p in (100, 200, 400, 800, 1200, 2400, 5000):
        rel = atom / p
        verd = ("about right" if 0.10 <= rel <= 0.20 else
                "TOO COARSE" if rel > 0.20 else
                "finer than perception" if rel > 0.05 else "FALSE PRECISION")
        w(f"| {p:,} | {rel:.1%} | {verd} | **{step_for(p, atom):,}** |")
    already = sum(1 for v in vals if abs(v / atom - round(v / atom)) < 1e-9)
    w(f"\n⭐ **{already} of {len(vals)} prices ({already/len(vals):.0%}) are ALREADY multiples "
      f"of {atom}.** The atom is not the problem and snapping to it changes almost nothing — "
      f"the over-precision is in the SPACING, not the last digit.\n")

    w("## The derived grid\n")
    w("| price band | grid step | one notch |")
    w("|---|--:|--:|")
    seen, rows = set(), []
    p = float(min(vals))
    while p <= max(vals):
        s = step_for(p, atom)
        if s not in seen:
            seen.add(s)
            rows.append((p, s))
        p *= 1.05
    for i, (p0, s) in enumerate(rows):
        hi = rows[i + 1][0] if i + 1 < len(rows) else max(vals)
        w(f"| {p0:,.0f} – {hi:,.0f} | **{s:,}** | {s/max(p0,1):.1%} |")

    snapped = [snap(v, atom) for v in vals]
    moved = sum(1 for a, b in zip(vals, snapped) if a != b)
    err = [abs(b - a) / a for a, b in zip(vals, snapped)]
    w(f"\n## What the snap costs\n")
    w(f"| metric | before | after |")
    w("|---|--:|--:|")
    w(f"| distinct prices | **{len(set(vals))}** | **{len(set(snapped))}** |")
    w(f"| prices per unit | {len(set(vals))/len(vals):.3f} | {len(set(snapped))/len(vals):.3f} |")
    dv = sorted(set(vals)); ds = sorted(set(snapped))
    for label, seq in (("before", dv), ("after", ds)):
        st = [seq[i+1]/seq[i] for i in range(len(seq)-1) if seq[i] > 0]
        w(f"| median adjacent step ({label}) | {statistics.median(st):.3f}x | |")
    w(f"\nunits whose price MOVES: **{moved} ({moved/len(vals):.0%})**, "
      f"median move **{statistics.median(err):.2%}**, worst **{max(err):.1%}**\n")
    w(f"⛔ Every move is a repricing: it goes through the ledger and `apply_balance "
      f"--confirm`, and must re-pass `check_band` and the boot gate. This tool proposes; "
      f"it never writes.\n")

    w("## The band, for scale\n")
    w(f"hard band {cb.FLOOR:.2f}–{cb.CEIL:.2f} = **{cb.CEIL/cb.FLOOR:.1f}x** wide -> "
      f"**{math.log(cb.CEIL/cb.FLOOR)/math.log(1+RESOLUTION):.1f} rungs**; "
      f"target {cb.SWEET_LO:.2f}–{cb.SWEET_HI:.2f} = **{cb.SWEET_HI/cb.SWEET_LO:.2f}x** -> "
      f"**{math.log(cb.SWEET_HI/cb.SWEET_LO)/math.log(1+RESOLUTION):.1f} rungs**\n")

    text = "\n".join(out)
    print(text)
    if args.md:
        pathlib.Path(args.md).write_text(text + "\n", encoding="utf-8")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
