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

# Worth taking. Each needs a profile no existing rung provides.
CANDIDATES = {
    "naval_*": (
        "**NAVAL — the real gap.** Cameo has NO naval armor and no naval "
        "template. Verified by resolving every real ship — an actor whose "
        "`Mobile.Locomotor` is `naval`, which is the only reliable test; matching "
        "on names like 'carrier' or 'battlecruiser' finds FLYING units and was "
        "how an earlier draft of this note wrongly claimed ships use `Spaceship`. "
        "The three buildable ships (`ra2_allies_aegiscruiser`, "
        "`ra2_soviets_seascorpion`, `yuri_boomersubmarine`) use the VEHICLE "
        "ladder: Medium, Light, Medium. So a submarine and a light tank currently "
        "share a damage profile, and no weapon can be made good at sea without "
        "also being good against tanks. DTA splits naval three ways "
        "(naval_light/medium/heavy) for exactly the reason ground armor is split. "
        "Naval anchors are already queued (ROADMAP phase C-naval) and cannot be "
        "stated without this."),
    "wall": (
        "Walls resist small arms almost completely but fall to demolition — a "
        "shape no building rung has, since Wood/Steel/Concrete all sit on the "
        "normal ladder. Without it, wall balance is a per-weapon hack."),
    "harvester": (
        "Economy units as their own rung. This is a genuine BALANCE LEVER: it "
        "lets harvester harassment be tuned without touching every light "
        "vehicle, which is otherwise the only way to reach it."),
    "cy": (
        "Construction Yard. Cameo already special-cases it (the AtomicCore "
        "75%-CY superweapon), and an armor type turns that hand-tuning into a "
        "profile column — superweapon-vs-CY becomes a number, not an exception."),
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
            status = "CANDIDATE"
        print(f"{armor:16} {count:6} {n:5}  {status[:76]}")

    print("\n" + "=" * 74)
    print("RECOMMENDED ADDITIONS")
    print("=" * 74)
    for name, why in CANDIDATES.items():
        print(f"\n* {name}\n  " + why.replace("**", ""))
    print("\nNot recommended: everything in COVERED above — each duplicates a rung "
          "Cameo\n  already has, and a 17th armor that behaves like the 16th is "
          "just more numbers\n  to keep consistent.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
