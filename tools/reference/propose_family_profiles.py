#!/usr/bin/env python3
"""W13 step 3 — a proposed Versus profile for every `^Warhead_<Family>_<Level>`.

This is where the three laws meet. For each Cameo family and level:

  1. **Platform** (DESIGN.md §12.0a) picks the reference rows — a family's LEVEL
     tracks the platform tier, so `_Light` learns from infantry and small turrets
     and `_Heavy` from heavy vehicles and big defences. Pooling all "lasers"
     together mixes a rifle with an Obelisk, which is the flattening W13 exists
     to undo.
  2. **The ordering law** (`gen_weapon_template.py`) places the values: macro
     priority x light/heavy direction. The corpus supplies magnitudes, never order.
  3. **The shape law** (DESIGN.md §12.0) finishes it: peak 100, no two values
     identical, uneven steps allowed, floor 10-25 (5 only for the deliberate
     extreme), then the derived armors.

    python tools/reference/propose_family_profiles.py
    python tools/reference/propose_family_profiles.py --family Laser
    python tools/reference/propose_family_profiles.py --write

⚠ **Families with no reference coverage are reported, not invented.** Melee,
Arrow, Magic, Sonic, Nuclear and the rest have no cross-mod equivalent to learn
from — they are Cameo's own inventions — so this prints "no source" for them
rather than borrowing a neighbouring family's numbers and calling it evidence.

⚠ **KNOWN LIMITATION — compressed blocks need a DESIGN decision, not more data.**
Where the corpus does not differentiate the rungs inside a macro block, the
no-ties rule can only separate them by its minimum step, and the output looks
like `Scout 96 · Light 97 · Medium 98 · Heavy 99 · Superheavy 100`. That is
honest — the canonical `AP` warhead really is ~100 against every armoured
target, because the source games never needed to tell a medium tank from a heavy
one — but it is not a usable Cameo ladder: five rungs spanning four points is
flat in everything but the letter of the law.

Read those blocks as *"the field says: all of these are top targets"* and let the
DESIGN spread them. The corpus cannot supply a distinction it never drew, and
padding one in here would disguise a design choice as a measurement.
"""
from __future__ import annotations

import argparse
import collections
import pathlib
import statistics
import sys

ROOT = pathlib.Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "tools" / "reference"))
sys.path.insert(0, str(ROOT / "tools" / "balance"))
import aggregate_archetype as ag  # noqa: E402
import gen_weapon_template as gwt  # noqa: E402
import survey_platforms as sp  # noqa: E402

OUT = ROOT / "docs" / "reference" / "family_profiles.md"

if hasattr(sys.stdout, "reconfigure"):
    sys.stdout.reconfigure(encoding="utf-8")

# Cameo family -> (corpus family, law direction). Direction mirrors
# gen_weapon_template.py so the two cannot drift.
FAMILY_SOURCE = {
    "Bullet": ("bullet", "light"),
    "CannonHE": ("cannon", "light"),
    "CannonAP": ("cannon", "heavy"),
    "MissileHE": ("missile", "light"),
    "MissileAP": ("missile", "heavy"),
    "MissileAA": ("missile", "heavy"),
    "Laser": ("laser", "heavy"),
    "Prism": ("laser", "light"),
    "Flame": ("flame", "light"),
    "Tesla": ("tesla", "heavy"),
}
# Cameo's own inventions — no cross-mod equivalent, so no proposal is offered.
NO_SOURCE = ["Flak", "Chemical", "Melee", "Arrow", "Magic", "Demolition",
             "Concussion", "Sonic", "Railgun", "Nuclear"]

# LEVEL -> the platform tiers it learns from. A level is a statement about how
# big the weapon is, and platform is the only thing in the corpus that says so.
LEVEL_PLATFORMS = {
    "Light": ("infantry", "vehicle_light", "defense_small"),
    "Medium": ("vehicle_medium", "defense_small"),
    "Heavy": ("vehicle_heavy", "defense_big"),
    "Super": ("defense_big",),
}
# Floor by level: heavier weapons are allowed to be more specialised.
LEVEL_FLOOR = {"Light": 25, "Medium": 20, "Heavy": 15, "Super": 10}

# The law's direction word, as `survey_platforms.direction` reports it.
DIRECTION_WORD = {"light": "anti-LIGHT", "heavy": "anti-HEAVY"}
# Below this, a tier is reported as thin rather than treated as settled.
MIN_ROWS = 8

# --------------------------------------------------------------------------- #
# Flat-block spreading — OPT-IN, because it is DESIGN, not measurement.
# --------------------------------------------------------------------------- #
# The corpus routinely fails to separate rungs inside a macro block: the
# canonical `AP` warhead is ~100 against every armoured target, because the
# source games never needed to tell a medium tank from a heavy one. The no-ties
# rule then separates them by its minimum step and CannonAP's vehicle block
# reads `96 · 97 · 98 · 99 · 100` — distinct, and useless.
#
# `--spread-flat-blocks` fixes that by widening any block whose internal span is
# under `MIN_BLOCK_SPAN`, redistributing its values around their own mean so the
# block keeps the POSITION the macro priority gave it while its rungs become
# readable. Nothing else moves.
#
# It is off by default on purpose. The default output is what the field says; the
# flag is Cameo choosing something the field never expressed, and the two should
# never be confused in a document someone later mines for evidence.
MIN_BLOCK_SPAN = 20.0


def spread_flat_blocks(profile: dict[str, float], order: list[str],
                       min_span: float = MIN_BLOCK_SPAN) -> dict[str, float]:
    """Widen any macro block the corpus left effectively flat.

    Operates per macro ladder, in the sequence `order` already fixed, so the
    ranking is never changed — only the spacing. The block is re-spread around
    its own mean, which keeps its centre of mass and therefore its standing
    relative to the other blocks; a block that the field placed low stays low.

    Blocks the corpus DID differentiate (span >= min_span) are left untouched,
    so real measured shape always wins over the synthetic spread.
    """
    out = dict(profile)
    for ladder in ag.CAMEO_LADDERS.values():
        present = [a for a in order if a in ladder and a in profile]
        if len(present) < 2:
            continue
        values = [profile[a] for a in present]
        span = max(values) - min(values)
        if span >= min_span:
            continue                        # the field said something; keep it
        # Anchor at the block's OWN top and widen downward. Centering on the mean
        # and spreading both ways looks fairer but pushes the upper rungs past
        # 100, where the clamp flattens them back into ties that `enforce_distinct`
        # then separates by 1 — reproducing the exact problem this exists to fix
        # (`Scout 88 · Light 93 · Medium 97 · Heavy 99 · Superheavy 100`).
        # The top of a block is also what ranks it against the other blocks, so
        # holding it fixed keeps the macro priority the field measured.
        top = max(values)
        step = min_span / (len(present) - 1)
        for i, armor in enumerate(present):
            out[armor] = round(max(1.0, top - i * step), 1)
    return out


def rows_for(corpus_family: str, level: str) -> list[dict]:
    wanted = set(LEVEL_PLATFORMS[level])
    return [r for r in sp.survey(corpus_family) if r["platform"] in wanted]


def profile_for(family: str, level: str, cache: dict,
                spread: bool = False) -> tuple[dict, int, int] | None:
    corpus_family, direction = FAMILY_SOURCE[family]
    if corpus_family not in cache:
        cache[corpus_family] = sp.survey(corpus_family)
    wanted = set(LEVEL_PLATFORMS[level])
    rows = [r for r in cache[corpus_family] if r["platform"] in wanted]

    # ⚠ ROLE FILTER — maintainer: *"you need to be very careful when you compare
    # versus values from different units because we need to first make sure they
    # have the same role as in cameo."*
    #
    # The corpus family is only half the match. `cannon` covers both the infantry
    # bazooka that shreds troops and the tank gun that kills armour, and Cameo
    # splits those into CannonHE and CannonAP. Feeding every cannon row into
    # CannonAP and then REORDERING it heavy-first does not produce an anti-armour
    # weapon — it produces an anti-infantry weapon wearing an anti-armour ladder,
    # which is how `Plate` came out at 100 on a family whose whole point is that
    # infantry are a bad target for it.
    #
    # So a row only teaches a family if the row's own measured direction agrees
    # with the family's. `flat` rows are kept as neutral evidence; they carry
    # magnitude information without arguing for either direction.
    matching = [r for r in rows
                if sp.direction(r["versus"]) in (DIRECTION_WORD[direction], "flat")]
    if len(matching) >= MIN_ROWS:
        rows = matching
    elif matching:
        rows = matching                       # thin, but still the right role
    if not rows:
        return None

    buckets: dict[str, list[float]] = collections.defaultdict(list)
    for row in rows:
        mapped, _ = ag.to_cameo(row["versus"])
        for armor, value in mapped.items():
            buckets[armor].append(value)
    # Median WITHIN the platform tier — never a mean, never across tiers.
    median = {a: statistics.median(v) for a, v in buckets.items() if v}
    if len(median) < 4:
        return None

    # ⚠ ORDER COMES FROM THE GENERATOR, not from a per-ladder sort.
    #
    # `ag.lawful_profile` sorts within each macro ladder independently, so each
    # ladder keeps its own peak — which meant `Plate` came out at 100 on
    # CannonAP, a family whose entire premise is that infantry are a poor target.
    # The macro PRIORITY (`VEH > BLD > INF > AIR`) was never applied.
    #
    # `gen_weapon_template.build_order()` already produces the full 16-armor
    # sequence from exactly that priority plus the light/heavy direction — it is
    # the ordering law in code. Reading it from there rather than re-deriving it
    # also means this tool cannot drift from the generator it feeds.
    blocks, gen_direction, _air, _levels = gwt.WEAPONS[family]
    order = gwt.build_order(blocks, gen_direction)
    values = sorted((median[a] for a in order if a in median), reverse=True)
    lawful = {a: v for a, v in zip([a for a in order if a in median], values)}
    lawful = ag.shape_profile(lawful, LEVEL_FLOOR[level])
    if spread:
        lawful = spread_flat_blocks(lawful, order)
    lawful.update(ag.derive_armors(lawful))
    lawful = ag.enforce_distinct(lawful)
    return lawful, len(rows), len({r["source"] for r in rows})


def render(results: dict) -> str:
    lines = [
        "# W13 — proposed Versus profiles per `^Warhead_<Family>_<Level>`",
        "",
        "Generated by `tools/reference/propose_family_profiles.py`.",
        "",
        "Each row is: reference rows for that family at that PLATFORM TIER ->",
        "median within the tier -> ordering law -> shape law -> derived armors.",
        "Values are normalised to peak 100; absolute lethality is `Damage`, not this.",
        "",
        "⚠ A level learns from its own platform tier, never from a pool of the whole",
        "family: `_Light` from infantry and small turrets, `_Heavy` from heavy",
        "vehicles and big defences. Pooling mixes a laser rifle with an Obelisk.",
        "",
    ]
    for family, levels in results.items():
        lines += [f"## {family}", "",
                  "| level | n | mods | " + " | ".join(ag.CAMEO16) + " |",
                  "|---|--:|--:|" + "--:|" * len(ag.CAMEO16)]
        for level, payload in levels.items():
            if payload is None:
                lines.append(f"| {level} | 0 | 0 | "
                             + " | ".join("—" for _ in ag.CAMEO16) + " |")
                continue
            profile, n, mods = payload
            cells = ["—" if profile.get(a) is None else f"{profile[a]:g}"
                     for a in ag.CAMEO16]
            lines.append(f"| **{level}** | {n} | {mods} | " + " | ".join(cells) + " |")
        lines.append("")
    lines += ["## No reference coverage", "",
              "Cameo inventions with no cross-mod equivalent. These stay hand-designed —",
              "borrowing a neighbouring family's numbers would look like evidence and",
              "would not be:", "",
              "- " + ", ".join(f"`{f}`" for f in NO_SOURCE), ""]
    return "\n".join(lines)


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__,
                                 formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--family", help="one family only")
    ap.add_argument("--write", action="store_true", help=f"write {OUT.relative_to(ROOT)}")
    ap.add_argument("--spread-flat-blocks", action="store_true",
                    help="DESIGN step: widen macro blocks the corpus left flat")
    args = ap.parse_args()

    families = [args.family] if args.family else list(FAMILY_SOURCE)
    for family in families:
        if family not in FAMILY_SOURCE:
            print(f"unknown family {family}; known: {', '.join(FAMILY_SOURCE)}")
            return 1

    cache: dict = {}
    results: dict = {}
    for family in families:
        levels = {}
        for level in ("Light", "Medium", "Heavy"):
            levels[level] = profile_for(family, level, cache,
                                        spread=args.spread_flat_blocks)
        results[family] = levels

    for family, levels in results.items():
        print(f"\n{family}  (from `{FAMILY_SOURCE[family][0]}`, "
              f"dir={FAMILY_SOURCE[family][1]})")
        for level, payload in levels.items():
            if payload is None:
                print(f"   {level:7} no reference rows at this platform tier")
                continue
            profile, n, mods = payload
            key = ("None", "Plate", "Scout", "Light", "Medium", "Heavy",
                   "Superheavy", "Concrete", "Helicopter")
            shown = " ".join(f"{a[:4]}={profile[a]:.0f}" for a in key if a in profile)
            print(f"   {level:7} n={n:3} mods={mods}  {shown}")
    if args.write:
        OUT.write_text(render(results), encoding="utf-8")
        print(f"\nwrote {OUT.relative_to(ROOT)}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
