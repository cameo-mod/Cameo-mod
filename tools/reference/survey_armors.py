#!/usr/bin/env python3
"""Every armor type the reference mods use — which are worth adopting?

Maintainer, 2026-08-15: *"check all the reference material for additional armor
types we can use. What armor types they have there in all the mods could still be
a good addition and useful to better balance our things?"*

An armor type earns its place only if it needs a DIFFERENT profile from every
type Cameo already has. Adding one that behaves like an existing rung just makes
17 numbers to write instead of 16. So the report shows, per candidate, how many
independent mods use it and how many warheads mention it — breadth first, because
one mod's private armor is that mod's taste, while five mods agreeing is a
structural gap.

    python tools/reference/survey_armors.py
"""
from __future__ import annotations

import collections
import json
import pathlib
import sys

ROOT = pathlib.Path(__file__).resolve().parents[2]
CORPUS = ROOT / "docs" / "reference" / "versus_raw.json"

if hasattr(sys.stdout, "reconfigure"):
    sys.stdout.reconfigure(encoding="utf-8")

CAMEO = {"none", "flak", "plate", "heroic", "scout", "light", "medium", "heavy",
         "superheavy", "wood", "steel", "concrete", "fighter", "bomber",
         "helicopter", "spaceship", "shield", "airborne"}

# Already covered by an existing Cameo rung — adopting them would duplicate.
COVERED = {
    "drone": "Scout — Cameo's light/robotic rung already is this",
    "brick": "a fourth building tier; Wood/Steel/Concrete already spans it",
    "infantry": "a macro bucket, not a rung (None/Flak/Plate are finer)",
    "aircraft": "a macro bucket; Cameo's four air classes are finer",
    "building": "a macro bucket; Wood/Steel/Concrete are finer",
    "defense": "role, not armor — Cameo prices defences by class instead",
    "special": "an engine catch-all with no consistent meaning across mods",
    "invulnerable": "Cameo expresses this with target types, not an armor",
    "tree": "map prop — DESIGN.md §13 handles props via the Obstacle target type",
    "truk": "one mod's civilian-truck special case",
    "disruptor": "one mod's unit-specific armor",
    "boss": "Cameo's epic role is a build-limit promotion, priced not armored",
    "rocket": "projectile-vs-projectile interception, not unit armor",
    # Shattered Paradise ships both `building` and `buildingarmor` etc. — the
    # long forms are that mod's aliases for the same buckets, 3 rows each.
    "buildingarmor": "SP alias of `building`",
    "concretearmor": "SP alias of `concrete`",
    "defensearmor": "SP alias of `defense`",
    "infantryarmor": "SP alias of `infantry`",
    "vehiclearmor": "SP alias of a vehicle bucket",
}

# ⚠ VERDICT (maintainer, 2026-08-15): **NONE of them.** All four candidates I
# proposed were rejected, each for a reason that shows the existing 16 already
# cover the ground:
#
#   naval_*   "Naval is sadly not a big part of our gameplay so having naval
#             armor types would rarely see any use. They can use vehicle armors
#             for now." — an armor class earns its place by being USED, and a
#             rung nothing stands on is 16 more numbers per warhead for nothing.
#   wall      Walls already map cleanly onto the building ladder:
#             wire fence + sandbags -> Wood, chainlink -> Steel, concrete -> Concrete.
#             My claim that walls "need a shape no building rung has" was wrong;
#             they need the shapes that already exist, correctly assigned.
#   cy        The Construction Yard already uses Concrete, the heavy building
#             rung, and the AtomicCore superweapon is already tuned to 75% of CY
#             health against it. The special case is SOLVED, not outstanding.
#   harvester Not adopted — see below.
#
# So the answer to "what should we take from the reference mods' armor sets" is
# **nothing**. That is a real finding, not a failure to find one: 16 rungs plus
# two derived hybrids span what 16 mods express, and the differences are naming
# and macro-bucket granularity rather than missing design space.
REJECTED_CANDIDATES = {
    "naval_light/medium/heavy": (
        "Ships DO borrow the vehicle ladder — the three buildable ones "
        "(`ra2_allies_aegiscruiser` Medium, `ra2_soviets_seascorpion` Light, "
        "`yuri_boomersubmarine` Medium) share profiles with light tanks. But "
        "three ships is the whole navy: naval is not a significant part of "
        "Cameo's gameplay, so three new rungs would cost 3 numbers on every one "
        "of ~55 warhead templates and almost never be read. Vehicle armors for "
        "now; revisit only if naval ever grows a real roster."),
    "wall": (
        "My reasoning was wrong. Walls do NOT need a shape the building ladder "
        "lacks — they need the shapes it already has, correctly assigned: "
        "wire fence + sandbags -> Wood, chainlink -> Steel, concrete wall -> "
        "Concrete. That is an assignment job, not a new armor type."),
    "harvester": (
        "Rejected with the rest: harvesters are light vehicles and the light "
        "vehicle rung already reaches them. Tuning harassment separately is a "
        "real want, but it belongs to unit pricing, not to a 17th armor column."),
    "cy": (
        "Already solved. The Construction Yard uses Concrete, the heavy building "
        "rung, and the AtomicCore superweapon is already tuned to 75% of CY "
        "health against it. There is no outstanding special case to absorb."),
}


def main() -> int:
    if not CORPUS.exists():
        print(f"missing {CORPUS.relative_to(ROOT)} — run extract_versus.py first")
        return 1
    data = json.loads(CORPUS.read_text(encoding="utf-8"))
    sources: dict[str, set[str]] = collections.defaultdict(set)
    rows = collections.Counter()
    for sid, entry in data["sources"].items():
        for row in entry["rows"]:
            for armor in row.get("versus", {}):
                sources[armor.lower()].add(sid)
                rows[armor.lower()] += 1

    print(f"{len(rows)} distinct armor types across {len(data['sources'])} mods\n")
    print(f"{'armor':16} {'rows':>6} {'mods':>5}  status")
    for armor, count in rows.most_common():
        n = len(sources[armor])
        if armor in CAMEO:
            status = "Cameo has it"
        elif armor in COVERED:
            status = f"covered — {COVERED[armor]}"
        else:
            status = "considered -> REJECTED (see verdict)"
        print(f"{armor:16} {count:6} {n:5}  {status[:76]}")

    print("\n" + "=" * 74)
    print("VERDICT: adopt NOTHING — the 16 rungs already span what 16 mods express")
    print("=" * 74)
    for name, why in REJECTED_CANDIDATES.items():
        print(f"\n* {name}  [REJECTED]\n  " + why.replace("**", ""))
    print("\nEverything else duplicates a rung Cameo already has. A 17th armor that "
          "behaves\n  like the 16th is just more numbers to keep consistent across "
          "every warhead.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
