#!/usr/bin/env python3
"""review_batch_diff.py — did a whole W24 batch preserve behaviour?

`review_resolve_diff.py` verifies weapons you NAME. This verifies the ones you did not think
to name, which is where the defects were: it resolves **every** weapon in two repo roots and
diffs the invariants a structural retrofit must never change.

    python tools/audit/review_batch_diff.py <base_root> <head_root> [--json out.json]

⛔ WHY THIS EXISTS (2026-08-19). A 39-commit W24 batch shipped seven weapons with 30–93% of
their damage missing — `SCUDNUKE` 300000 -> 20000 — plus a weapon that lost its firing sound
and two that silently gained air-targeting. Every existing guard passed: the tree booted, 227
tests were green, `find_empty_warhead` was 0, every doc claim matched. The maintainer found it
by asking *"did it just delete all the other warheads without adjusting the sum?"* — which is a
question no guard in the suite was asking. This asks it.

⚠ THE MEASUREMENT TRAP THAT MADE IT LOOK 7x WORSE. A raw total-damage diff flagged **52**
weapons. Only 7 were defects; the other 45 were the intended `DamagesConcrete` dedup, which
collapses two concrete warheads into one and legitimately moves the total by 1–7%. So
`Warhead@*Concrete*` is excluded from the damage comparison by default (`--with-concrete` to
include it). Judge main damage on main warheads, or the real signal drowns.

⚠ BLAST SHAPE IS REPORTED, NEVER FAILED, and it is the subtlest check here.
`AreaDamageWarhead.cs` splits Damage ACROSS ticks (`perTickModifier = Ticks > 1 ? 100 / Ticks
: 100`), so `Ticks` is TOTAL-PRESERVING: collapsing a 10-ring nuclear shockwave onto a family
with no `Ticks` keeps every point of damage and still turns an expanding blast into one
instantaneous thump. A damage check alone cannot see that, so `Spread`/`Falloff`/`Ticks`/
`MaxRadius` are diffed too — as a REPORT, because changing the shape is often the whole point
of moving a weapon onto a family.

EXIT CODE: 1 if any weapon's non-concrete main damage or authored percentage-damage total changed
— those are the "Damage verbatim" law from `WEAPON_3WAY_SPLIT.md` and are not judgement calls.
Everything else is reported but does not fail, because a retrofit may legitimately change `Burst`
cadence or add a projectile.
"""
from __future__ import annotations

import argparse
import json
import pathlib
import sys

if hasattr(sys.stdout, "reconfigure"):          # Windows consoles default to cp1252
    sys.stdout.reconfigure(encoding="utf-8")

sys.path.insert(0, str(pathlib.Path(__file__).resolve().parent))
sys.path.insert(0, str(pathlib.Path(__file__).resolve().parents[1] / "balance"))
from cameo_model import Model  # noqa: E402
import percentage_damage as pd  # noqa: E402


# Match the repository's upgrade audit. This stays below the legacy Int32
# multiplication overflow boundary for the current authored percentage units.
PERCENTAGE_REFERENCE_HP = 200_000


def _dmg(node) -> float | None:
    try:
        return float(str(node.get("Damage")).strip())
    except (TypeError, ValueError):
        return None


def snapshot(root: str, with_concrete: bool) -> dict[str, dict]:
    """Resolved behavioural fingerprint for every concrete weapon in one repo root."""
    rs = Model(pathlib.Path(root)).rs
    out: dict[str, dict] = {}
    for name in rs.weapons:
        if name.startswith("^"):
            continue
        node = rs.resolve_weapon(name)
        if node is None:
            continue
        total, mains, shape = 0.0, [], []
        for wh in node.children:
            if not wh.key.startswith("Warhead"):
                continue
            if not with_concrete and "Concrete" in wh.key:
                continue
            rel = (wh.get("ValidRelationships") or "").strip()
            if "Ally" in rel and "Enemy" not in rel:      # friendly-fire twin, not a main
                continue
            d = _dmg(wh)
            if d is not None and d > 0 and "Percentage" not in wh.key:
                total += d
                mains.append(int(d))
                # The blast GEOMETRY. Damage says how much; this says where and over how long.
                # `Ticks` is the expanding-shockwave count — `AreaDamageWarhead.cs` splits Damage
                # ACROSS ticks (`perTickModifier = 100 / Ticks`), so dropping it preserves the sum
                # and silently changes a 10-ring nuclear shockwave into one instantaneous blast.
                shape.append("|".join(str(wh.get(k) or "-")
                                      for k in ("Spread", "Falloff", "Ticks", "MaxRadius")))
        out[name] = {
            "damage": total,
            "mains": sorted(mains, reverse=True),
            "percentage_damage": sum(
                app["runtime_hp"]
                for app in pd.percentage_applications(node, PERCENTAGE_REFERENCE_HP)
                if "friendlyfire" not in app["tag"].lower()),
            "shape": sorted(shape),
            "Range": node.get("Range"),
            "ReloadDelay": node.get("ReloadDelay"),
            "Burst": node.get("Burst"),
            "ValidTargets": node.get("ValidTargets"),
            "report": bool(node.get("Report") or node.get("StartBurstReport")),
        }
    return out


SOFT = ("Range", "ReloadDelay", "Burst", "ValidTargets")


def compare(base: dict, head: dict) -> tuple[dict, list, list]:
    gone = sorted(set(base) - set(head))
    added = sorted(set(head) - set(base))
    changed: dict[str, list] = {}
    for w in sorted(set(base) & set(head)):
        b, h = base[w], head[w]
        diffs = []
        if abs(b["damage"] - h["damage"]) > 0.5:
            diffs.append(["main_damage", b["damage"], h["damage"]])
        if abs(b["percentage_damage"] - h["percentage_damage"]) > 0.5:
            diffs.append([
                "percentage_damage", b["percentage_damage"], h["percentage_damage"]])
        for k in SOFT:
            if (b[k] or "") != (h[k] or ""):
                diffs.append([k, b[k], h[k]])
        if b["shape"] != h["shape"]:
            diffs.append(["blast_shape", " ; ".join(b["shape"]), " ; ".join(h["shape"])])
        if b["report"] and not h["report"]:
            diffs.append(["Report", "yes", "LOST"])
        if diffs:
            changed[w] = diffs
    return changed, gone, added


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__,
                                 formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("base_root")
    ap.add_argument("head_root")
    ap.add_argument("--json", help="write the full per-weapon findings here")
    ap.add_argument("--with-concrete", action="store_true",
                    help="include Warhead@*Concrete* in the damage total (default: excluded — "
                         "the DamagesConcrete dedup is an INTENDED change and drowns the signal)")
    a = ap.parse_args()

    base = snapshot(a.base_root, a.with_concrete)
    head = snapshot(a.head_root, a.with_concrete)
    changed, gone, added = compare(base, head)

    print(f"# review_batch_diff — {len(base)} weapons in base, {len(head)} in head")
    if not a.with_concrete:
        print("_`Warhead@*Concrete*` excluded from damage (see --with-concrete)._")
    print()
    if gone:
        print(f"**{len(gone)} weapon(s) REMOVED:** {', '.join(gone[:12])}"
              f"{' …' if len(gone) > 12 else ''}")
    if added:
        print(f"**{len(added)} weapon(s) ADDED:** {', '.join(added[:12])}"
              f"{' …' if len(added) > 12 else ''}")

    by_kind: dict[str, list[str]] = {}
    for w, diffs in changed.items():
        for d in diffs:
            by_kind.setdefault(d[0], []).append(w)

    dmg = by_kind.get("main_damage", [])
    if dmg:
        print(f"\n## ⛔ FAIL — main damage changed on {len(dmg)} weapon(s)\n")
        print("`WEAPON_3WAY_SPLIT.md`: the retrofit *\"PRESERVES the weapon's existing on-grid "
              "value verbatim; it invents NO numbers\"*. A collapse must carry the TOTAL.\n")
        print("| factor | before | after | weapon |")
        print("|--:|--:|--:|---|")
        rows = []
        for w in dmg:
            b, h = base[w]["damage"], head[w]["damage"]
            rows.append((h / b if b else 999.0, b, h, w))
        for f, b, h, w in sorted(rows):
            print(f"| {f:.2f} | {b:.0f} | {h:.0f} | `{w}` |")
    else:
        print("\n## ✅ main damage preserved on every weapon")

    pct = by_kind.get("percentage_damage", [])
    if pct:
        print(f"\n## ⛔ FAIL — percentage damage changed on {len(pct)} weapon(s)\n")
        print("Values are runtime HP at a 200,000 HP reference target, so both standalone "
              "percentage twins and folded `PercentageScale` hits are compared with the "
              "engine's integer arithmetic.\n")
        print("| before | after | weapon |")
        print("|--:|--:|---|")
        for w in pct:
            b, h = base[w]["percentage_damage"], head[w]["percentage_damage"]
            print(f"| {b:.0f} | {h:.0f} | `{w}` |")
    else:
        print("\n## ✅ percentage damage preserved on every weapon")

    for kind in SOFT + ("Report", "blast_shape"):
        ws = by_kind.get(kind)
        if not ws:
            continue
        print(f"\n### ⚠ {kind} changed on {len(ws)} weapon(s)")
        for w in ws[:10]:
            d = next(x for x in changed[w] if x[0] == kind)
            print(f"- `{w}`: {d[1]!r} → {d[2]!r}")
        if len(ws) > 10:
            print(f"- … {len(ws) - 10} more")

    if a.json:
        pathlib.Path(a.json).write_text(
            json.dumps({"changed": changed, "removed": gone, "added": added}, indent=1),
            encoding="utf-8")
        print(f"\n_wrote {a.json}_")

    return 1 if dmg or pct else 0


if __name__ == "__main__":
    sys.exit(main())
