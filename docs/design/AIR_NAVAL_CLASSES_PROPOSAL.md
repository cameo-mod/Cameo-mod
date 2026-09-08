# AIR / NAVAL / ECONOMY CLASSES — PROPOSAL (LANE-5)

> **Status:** PROPOSAL — not a decision. The maintainer rules, then someone
> else writes `class_anchors.json`. This document touches no data file.
>
> **Author:** AURORA (A3, GLM-5.2 High), branch `devin/aurora/lane5-proposal`.
> **Data source:** `docs/balance/*.json` (post-LANE-4 extraction), 268 units
> across 9 subtypes that have a template but no class anchor.

## 1. The problem

`class_anchors.json` holds 27 classes and **not one of them is an air, naval,
or economy class**. That leaves **268 real units** (units with a valid
template) with nowhere to belong — the pipeline cannot price them until the
classes exist.

| subtype | units | domain |
|---|--:|---|
| `Helicopter` | 58 | air |
| `Bomber` | 44 | air |
| `Fighter` | 33 | air |
| `Harvester` | 30 | economy — `FORMULA_V2` §6c does not cover it |
| `ScoutShip` | 30 | naval |
| `UnarmedTransportHelicopter` | 25 | air (sub-template of `^HelicopterTemplate`) |
| `Spaceship` | 21 | air |
| `ArtilleryShip` | 16 | naval |
| `BattleShip` | 11 | naval |
| **Total** | **268** | |

## 2. Proposed class set

For each proposed class: the member count, the observed stat spread
(min / median / max), and the existing ground class it is most analogous to.

### 2.1 Air classes

#### `attack_helicopter` (proposed) — analogous to `mbt` (ground)

The armed workhorse of the sky. Members inherit `^HelicopterTemplate` and
carry at least one armament. Excludes unarmed transports (see
`transport_helicopter`).

| stat | min / median / max |
|---|---|
| HP | 10,000 / 70,000 / 1,350,000 |
| Cost | 25 / 2,000 / 6,000 |
| Speed | 30 / 125 / 200 |
| Sight (cell) | 2 / 36 / 64 |
| Max range | 1,448 / 5,514 / 12,345 |
| Max dmg/shot | 4,000 / 16,000 / 120,000 |

**Members:** 58 total (57 buildable). Spread is wide because the subtype
lumps scout helis (`protoss_observer` 12.5k HP, 500 cost) with capital
airships (`schwarzermond_spacezeppelin` 1.35M HP, 6,000 cost).

**Recommendation:** Split into `attack_helicopter` (armed, ≤200k HP) and
`capital_airship` (armed, >200k HP) — see §2.1b. The current
`Helicopter` subtype conflates two very different unit tiers.

#### `capital_airship` (proposed) — analogous to `dreadnought` (ground)

The slow, very-high-HP, high-cost air units. Members inherit
`^HelicopterTemplate` but have HP > 200,000 and cost > 3,000.

| stat | min / median / max |
|---|---|
| HP | 265,000 / 1,000,000 / 1,350,000 |
| Cost | 3,600 / 5,000 / 6,000 |
| Speed | 35 / 35 / 100 |
| Max dmg/shot | 4,000 / 8,000 / 120,000 |

**Members (approx 6):** `ra2_soviets_transportkirov` (265k HP),
`naxis_transportzeppelin` (1.25M HP), `schwarzermond_spacezeppelin` (1.35M HP),
`steelconsortium_cargoship` (125k HP — borderline),
`steelconsortium_skyhammer` (120k HP — borderline),
`protoss_arbiter` (225k HP).

**Rationale:** These units are qualitatively different from attack helis —
they are flying fortresses, not skirmishers. Pricing them in the same class
as a 25-cost drone would distort the anchor.

#### `transport_helicopter` (proposed) — analogous to `support` (ground)

Unarmed or lightly-armed air transports. Members inherit
`^UnarmedTransportHelicopterTemplate` (a sub-template of
`^HelicopterTemplate`).

| stat | min / median / max |
|---|---|
| HP | 17,500 / 100,000 / 500,000 |
| Cost | 600 / 2,000 / 6,000 |
| Speed | 45 / 156 / 189 |
| Max range | 4,000 / 4,250 / 6,000 |
| Max dmg/shot | 6,000 / 6,000 / 6,000 |

**Members:** 25 total (17 buildable). Most are unarmed (range/dmg = n/a);
a few non-buildable variants (`carryall.reinforce`, `frigate.paradrop`)
carry a defensive weapon.

**Rationale:** Transports are priced by capacity and survivability, not by
DPS. A dedicated class lets the formula use HP/speed/cost without being
distorted by the zero-DPS armed units.

#### `fighter` (proposed) — analogous to `scout_vehicle` (ground)

Fast, low-HP, high-DPS air superiority units. Members inherit
`^FighterTemplate`.

| stat | min / median / max |
|---|---|
| HP | 7,500 / 50,000 / 175,000 |
| Cost | 120 / 1,200 / 5,000 |
| Speed | 75 / 165 / 300 |
| Max range | 2,000 / 6,666 / 12,288 |
| Max dmg/shot | 2,000 / 12,345 / 200,000 |

**Members:** 33 total (20 buildable). The `200,000 dmg/shot` outlier is
`ra1_soviets_nuclearyak` — a nuclear-armed fighter that may warrant its own
subclass or a maintainer ruling.

**Rationale:** Fighters are the air-equivalent of scout vehicles: fast,
fragile, high burst. The ground `scout_vehicle` class is the closest
analogue for pricing shape.

#### `bomber` (proposed) — analogous to `line_breaker` (ground)

Slow-to-medium speed, high-volley-damage air units. Members inherit
`^BomberTemplate`.

| stat | min / median / max |
|---|---|
| HP | 5,000 / 76,250 / 180,000 |
| Cost | 50 / 1,850 / 7,000 |
| Speed | 35 / 180 / 250 |
| Max range | 750 / 5,207 / 12,500 |
| Max dmg/shot | 4,000 / 27,000 / 400,000 |
| Volley dmg | 6,000 / 60,315 / 450,000 |

**Members:** 44 total (19 buildable). Many non-buildable members are
support-power spawned units (badgers, kami drones, A10s). The
`400,000 dmg/shot` outlier is
`ra1_soviets_supersonicnuclearbomber` — another nuclear unit.

**Rationale:** Bombers are the air-equivalent of line breakers: high
alpha, long range, vulnerable to interception. The ground `line_breaker`
class is the closest analogue.

#### `spaceship` (proposed) — analogous to `dreadnought` (ground)

The heaviest air units — flying capitals. Members inherit
`^SpaceshipTemplate`.

| stat | min / median / max |
|---|---|
| HP | 125,000 / 400,000 / 3,750,000 |
| Cost | 1,500 / 5,000 / 15,000 |
| Speed | 25 / 45 / 145 |
| Sight (cell) | 73 / 73 / 73 |
| Max range | 2,500 / 7,030 / 10,000 |
| Max dmg/shot | 2,000 / 36,000 / 200,000 |
| Volley dmg | 4,000 / 60,010 / 816,090 |

**Members:** 21 total (21 buildable). Includes
`schwarzermond_dieglocke` (3.75M HP, 15,000 cost — the most expensive unit
in the mod), `terran_phobos` (1M HP), `protoss_starshipsovereign` (750k HP).

**Rationale:** Spaceships are a distinct tier above even capital airships.
Their cost (up to 15,000) and HP (up to 3.75M) make them the air-equivalent
of dreadnoughts. Pricing them with fighters would break the anchor.

### 2.2 Naval classes

#### `scout_ship` (proposed) — analogous to `scout_vehicle` (ground)

Fast, low-to-medium HP naval units. Members inherit `^ScoutShipTemplate`.

| stat | min / median / max |
|---|---|
| HP | 20,000 / 100,000 / 240,000 |
| Cost | 500 / 1,200 / 3,600 |
| Speed | 55 / 85 / 140 |
| Max range | 1,250 / 8,000 / 24,000 |
| Max dmg/shot | 2,000 / 32,000 / 60,000 |

**Members:** 30 total (30 buildable). Includes gunboats, submarines, hover
craft, and transports. The 24,000-range outlier is `sub.latin` (a syndicate
submarine).

**Rationale:** Scout ships are the naval-equivalent of scout vehicles:
fast, versatile, the workhorse of the sea. Some are combat (gunboats,
subs) and some are transport (LST, hover) — the maintainer may want to
split transports into `transport_ship`.

#### `artillery_ship` (proposed) — analogous to `fire_support` (ground)

Slow, long-range, high-damage naval units. Members inherit
`^ArtilleryShipTemplate`.

| stat | min / median / max |
|---|---|
| HP | 70,000 / 237,500 / 350,000 |
| Cost | 1,750 / 3,175 / 4,500 |
| Speed | 40 / 50 / 65 |
| Max range | 8,940 / 15,000 / 25,000 |
| Max dmg/shot | 4,000 / 62,500 / 80,000 |
| Volley dmg | 8,000 / 62,718 / 400,000 |

**Members:** 16 total (16 buildable). Cruisers, carriers, missile subs,
bombardment ships. The 400,000 volley outlier is `nax_bitsmark`.

**Rationale:** Artillery ships are the naval-equivalent of fire support:
slow, long-range, devastating alpha. The ground `fire_support` class is
the closest analogue.

#### `battleship` (proposed) — analogous to `mbt` (ground)

Medium-to-high HP, medium-range, sustained-DPS naval units. Members inherit
`^BattleShipTemplate`.

| stat | min / median / max |
|---|---|
| HP | 25,000 / 125,000 / 250,000 |
| Cost | 500 / 1,600 / 2,600 |
| Speed | 35 / 85 / 150 |
| Max range | 7,168 / 8,854 / 13,500 |
| Max dmg/shot | 6,000 / 45,000 / 120,000 |
| Volley dmg | 12,000 / 92,000 / 260,000 |

**Members:** 11 total (11 buildable). Destroyers, frigates, battleships,
corvettes.

**Rationale:** Battleships are the naval-equivalent of MBTs: the mainline
combat ship. They trade range for durability compared to artillery ships.

### 2.3 Economy class

#### `harvester` (proposed) — NO ground analogue (new class)

Economy units that gather resources. Members inherit `^HarvesterTemplate`.
`FORMULA_V2` §6c does not cover economy units, so this class needs a
maintainer-defined pricing model.

| stat | min / median / max |
|---|---|
| HP | 25,000 / 110,000 / 240,000 |
| Cost | 250 / 1,000 / 1,200 |
| Speed | 38 / 80 / 125 |
| Max range | 1,333 / 1,500 / 5,384 |
| Max dmg/shot | 8,000 / 10,000 / 12,000 |

**Members:** 30 total (30 buildable). All factions have exactly 1-2
harvesters. The WC2 peasants (`wc2_humans_peasant`, `wc2_orcs_peon`) are
borderline — they are worker units that also build, not pure harvesters.

**Rationale:** Harvesters are priced by gather rate and survivability, not
by combat stats. A dedicated class is the only option — no ground class
fits. The maintainer must define the pricing model (see §4).

## 3. Hand-tag disagreements — 39 rows

The explicit hand tag WINS by design (a maintainer override must survive
re-derivation). The question is whether these 39 overrides were deliberate
or drift from the 18% copy.

| subtype | hand tag | template implies | rows | recommendation |
|---|---|---|--:|---|
| `ScoutInfantry` | support | **scout** | 6 | **Drift.** Scout infantry are not support. Remove the override. |
| `HeavyInfantry` | special_forces | **heavy_infantry** | 5 | **Deliberate?** Heavy infantry tagged as special forces may be a faction-specific elite. Maintainer ruling. |
| `AntiTankAntiAirInfantry` | special_forces | **rocket_trooper** | 4 | **Drift.** Rocket troopers are not special forces. Remove. |
| `SniperInfantry` | support | **pure_sniper** | 4 | **Drift.** Snipers are not support. Remove. |
| `AntiTankAntiAirInfantry` | archer | **rocket_trooper** | 3 | **Drift.** Archers are medieval; rocket troopers are modern. Remove. |
| `HeavyInfantry` | support | **heavy_infantry** | 2 | **Drift.** Heavy infantry are not support. Remove. |
| `AntiTankAntiAirInfantry` | support | **rocket_trooper** | 2 | **Drift.** Remove. |
| `SniperInfantry` | special_forces | **pure_sniper** | 2 | **Deliberate?** Special-forces snipers may be elite. Maintainer ruling. |
| `MeleeInfantry` | special_forces | **melee** | 1 | **Deliberate?** A special melee unit (e.g. zealot). Maintainer ruling. |
| `MeleeInfantry` | support | **melee** | 1 | **Drift.** Melee infantry are not support. Remove. |
| `SniperInfantry` | heavy_sniper | **pure_sniper** | 1 | **Deliberate?** A heavy sniper variant. Maintainer ruling. |
| `MainBattleTank` | heavy_infantry | **mbt** | 1 | **Drift.** A tank is not infantry. Remove. |
| `FireSupport` | rocket_trooper | **fire_support** | 1 | **Drift.** Fire support is not a rocket trooper. Remove. |
| `ScoutVehicle` | scout | **scout_vehicle** | 1 | **Drift.** `scout` and `scout_vehicle` are different classes. Remove. |
| `LineBreaker` | closecombat | **line_breaker** | 1 | **Drift.** Line breaker is not close combat. Remove. |
| `SniperInfantry` | archer | **pure_sniper** | 1 | **Drift.** An archer is not a sniper. Remove. |
| `AntiTankAntiAirInfantry` | heavy_sniper | **rocket_trooper** | 1 | **Drift.** Remove. |
| `Dreadnought` | high_tech_tank | **dreadnought** | 1 | **Drift.** Dreadnought is not high-tech tank. Remove. |
| `HeroInfantry` | support | **commando** | 1 | **Drift.** A hero is not support. Remove. |

### Summary

- **28 rows** appear to be drift (the hand tag is clearly wrong for the
  unit type). Recommendation: remove the override and let the template
  win.
- **11 rows** are ambiguous (special-forces/heavy-sniper tags that could
  be deliberate elite designations). Recommendation: maintainer ruling
  per row.

### Proposed blanket rule

> If the hand tag and the template-implied class disagree, and the hand
> tag is `support` or `archer`, the hand tag is drift — remove it. If the
> hand tag is `special_forces`, `heavy_sniper`, or `high_tech_tank`, it
> may be a deliberate elite designation — require a maintainer ruling.

This would auto-fix 22 of the 39 rows and flag 17 for review.

## 4. Open questions for the maintainer

1. **Helicopter split:** Should `Helicopter` be split into
   `attack_helicopter` (≤200k HP) and `capital_airship` (>200k HP), or
   kept as one class?
2. **Transport split:** Should `UnarmedTransportHelicopter` and
   `ScoutShip` transports be split into separate `transport_helicopter`
   and `transport_ship` classes, or kept with their parent classes?
3. **Nuclear outliers:** `ra1_soviets_nuclearyak` (200k dmg/shot) and
   `ra1_soviets_supersonicnuclearbomber` (400k dmg/shot) are extreme
   outliers. Should they be excluded from the class anchor calculation,
   or given their own class?
4. **Harvester pricing:** `FORMULA_V2` §6c does not cover economy units.
   What pricing model should `harvester` use? Options:
   - (a) Cost = f(HP, speed) — ignore combat stats.
   - (b) Cost = f(HP, speed, gather_rate) — needs a gather_rate field.
   - (c) Fixed cost per faction — no formula.
5. **WC2 peasants:** `wc2_humans_peasant` and `wc2_orcs_peon` are
   workers that also build. Should they be in `harvester` or a separate
   `worker` class?
6. **Spaceship vs capital airship:** Are these distinct enough to warrant
   two classes, or should they be merged into `capital_air`?
7. **Hand-tag blanket rule:** Is the proposed blanket rule (§3) correct,
   or should all 39 rows get individual review?

## 5. What this proposal does NOT do

- Does NOT edit `class_anchors.json` — that is a separate task after the
  maintainer rules.
- Does NOT edit any YAML, ledger, or data file.
- Does NOT define the pricing formula for any class — that is
  `FORMULA_V2`'s job.
- Does NOT touch the 45 "no template" units (LANE-4, awaiting ruling).

---

*Generated by AURORA (A3, GLM-5.2 High) on 2026-09-07.
Data from `docs/balance/*.json` post-LANE-4 extraction.*
