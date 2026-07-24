# Original Unit Stats — ground-truth matrix for relative balancing

**Purpose.** Real stats from the *source games*, so faction identity and relative unit
strength are derived from DATA, not assumption. Companion to `FACTION_IDENTITY.md`
(the lean directions) and the balance pipeline (the mechanical enforcement).

## Two hard-won rules (2026-07-25, from the maintainer + the data)

1. **Faction identity is PER-UNIT-TYPE, not global.** A faction can be tanky in one
   type and frail in another. Confirmed by source stats:
   - **RA2 Soviets:** *infantry weak*, *tanks strongest & slowest*. **Allies:** *infantry
     tankiest/most-powerful & slow*, *tanks weakest & fast*. (So a Soviet-infantry frail
     reading is CORRECT, not a bug.)
   - **StarCraft Terran:** *infantry frail* (Marine 40 HP), *vehicles tanky* (Siege 150).
   - **Protoss:** tanky in every type (shields). **Zerg:** small units frailest/cheapest,
     Ultralisk a monster-tank.
   → The leans in `FACTION_IDENTITY.md` are the **infantry** leans; a faction's vehicle
   / aircraft lean is set separately (from this matrix) in the vehicle/aircraft passes.
2. **The in-game unit template encodes the role — trust it, don't override by HP.**
   `LineBreaker` = melee-range VEHICLE (heavy assault: Ultralisk, WC2 Knight/Ogre),
   `MainBattleTank` = tank, `MeleeInfantry`/`ScoutInfantry`/… = infantry. Balance class
   follows the template subtype; do not force a unit into another class because its HP
   looks big for the class.

---

## StarCraft: Brood War (complete — [unitstatistics.com](https://unitstatistics.com/starcraft/), [Liquipedia](https://liquipedia.net/starcraft/Unit_Statistics))

Effective HP = HP + Shield. Cost = minerals/gas. Supply in parens.

### Terran — frail infantry, tanky vehicles
| Unit | HP | eff-HP | GrndDmg | AirDmg | Rng | Cost m/g | Sup |
|---|--:|--:|--:|--:|--:|--:|--:|
| Marine | 40 | 40 | 6 | 6 | 4 | 50/0 | 1 |
| Firebat | 50 | 50 | 16 | 0 | 2 | 50/25 | 1 |
| Ghost | 45 | 45 | 10 | 10 | 7 | 25/75 | 1 |
| Medic | 60 | 60 | 0 | 0 | — | 50/25 | 1 |
| Vulture | 80 | 80 | 20 | 0 | 5 | 75/0 | 2 |
| Goliath | 125 | 125 | 12 | 20 | 5 | 100/50 | 2 |
| Siege Tank | 150 | 150 | 30/70 | 0 | 7/12 | 150/100 | 2 |
| Wraith | 120 | 120 | 8 | 20 | 5 | 150/100 | 2 |
| Battlecruiser | 500 | 500 | 25 | 25 | 6 | 400/300 | 6 |

### Protoss — tanky everywhere (shields), expensive
| Unit | HP | eff-HP | GrndDmg | AirDmg | Rng | Cost m/g | Sup |
|---|--:|--:|--:|--:|--:|--:|--:|
| Zealot | 100 | 160 | 16 | 0 | 1 | 100/0 | 2 |
| Dragoon | 100 | 180 | 20 | 20 | 4 | 125/50 | 2 |
| Dark Templar | 80 | 120 | 40 | 0 | 1 | 125/100 | 2 |
| High Templar | 40 | 80 | 0 | 0 | — | 50/150 | 2 |
| Archon | 10 | 360 | 30 | 30 | 2 | 100/300 | 4 |
| Reaver | 100 | 180 | 100 | 0 | 8 | 200/100 | 4 |
| Scout | 150 | 250 | 8 | 28 | 4 | 275/125 | 3 |
| Carrier | 300 | 450 | 6×8 | 6×8 | 8 | 350/250 | 6 |

### Zerg — cheap/frail small units, monster Ultralisk
| Unit | HP | GrndDmg | AirDmg | Rng | Cost m/g | Sup |
|---|--:|--:|--:|--:|--:|--:|
| Zergling | 35 | 5 | 0 | 1 | 25/0 | 0.5 |
| Hydralisk | 80 | 10 | 10 | 4 | 75/25 | 1 |
| Lurker | 125 | 20 | 0 | 6 | 200/200 | 2 |
| Infested Terran | 60 | 500 | 0 | 1 | 100/50 | 1 |
| Defiler | 80 | 0 | 0 | — | 50/150 | 2 |
| Ultralisk | 400 | 20 | 0 | 1 | 200/200 | 4 |
| Mutalisk | 120 | 9 | 9 | 3 | 100/100 | 2 |
| Guardian | 150 | 20 | 0 | 8 | 150/200 | 2 |
| Devourer | 250 | 0 | 25 | 6 | 250/150 | 2 |
| Scourge | 25 | 0 | 110 | 1 | 12/38 | 0.5 |

**Relative read:** Protoss eff-HP (160–450) ≫ Terran (40–150) ≈ Zerg small (25–125).
Cheapest→dearest supply-cost: Zergling/Scourge (0.5) < Marine (1) < most (2) < heavies (4–6).

---

## Warcraft 2 (complete — [unitstatistics.com](https://unitstatistics.com/warcraft2/))

**Original WC2 is a MIRROR** (Human ↔ Orc identical). Cameo de-mirrors (WC3 approach,
DESIGN.md §10) — these are the shared baseline to diverge FROM (Human defensive/+range,
Orc aggressive/+HP). Damage shown as min–max.

| Human / Orc | HP | Armor | Dmg | Rng | Speed | Gold/Wood |
|---|--:|--:|:-:|--:|--:|--:|
| Peasant / Peon | 30 | 0 | 2–9 | 1 | 10 | 400/0 |
| Footman / Grunt | 60 | 2 | 6 | 1 | 10 | 600/0 |
| Elven Archer / Troll Axethrower | 40 | 2 | 3–9 | 4 | 10 | 500/50 |
| Knight / Ogre | 90 | 4 | 2–12 | 1 | **13** | 800/100 |
| Ballista / Catapult (siege) | 110 | 0 | 25–80 | 8 | **5** | 900/300 |
| Demo Squad / Sappers | 40 | 0 | 1–6 | 1 | 11 | 750/250 |
| Mage / Death Knight | 60 | 0 | 2–9 | 2/3 | 10 | 1200/0 |
| Gryphon / Dragon (air) | 100 | 0 | 8–16 | 4 | 14 | 2500/0 |
| Flying Machine / Zeppelin (air scout) | 150 | 0 | 0 | — | 17 | 500/100 |

**Relative read:** HP: Ballista/Catapult 110 > Knight/Ogre 90 > Footman/Grunt 60 =
Mage/DK 60 > Archer 40 = Demo 40 > Peasant 30. Speed: Zeppelin 17 > Dragon 14 >
Knight 13 > Sappers 11 > most 10 > siege 5. (Knights are the *fast* heavy melee; siege is slow.)

---

## Red Alert 2 / Yuri (PARTIAL — raw HP still being gathered)

Sources so far: [CNCNZ](https://cncnz.com/games/yuris-revenge/), [unitstatistics](https://unitstatistics.com/red-alert2/) (coarse), search excerpts.

| Unit | Faction | Type | HP | Cost | Note |
|---|---|---|--:|--:|---|
| Conscript | Soviet | Inf | 125 | 100 | cheap fodder, Flak armor |
| G.I. | Allied | Inf | 100 | 200 | deploys to sandbags |
| Initiate | Yuri | Inf | 100 | 200 | psychic; strong vs inf |
| Grizzly Tank | Allied | Armor | — | 700 | fast, weaker |
| Rhino Tank | Soviet | Armor | — | 900 | slow, strong |
| Lasher | Yuri | Armor | — | ~800 | standard |
| Mirage Tank | Allied | Armor | — | 1000 | cloak |
| Tesla Tank | Soviet | Armor | — | 1200 | tesla |

**Per-type identity (maintainer-confirmed):** Allies = tanky/powerful/**slow infantry** +
weak/fast tanks; Soviets = weak infantry + strongest/**slow tanks**. So Allied infantry
should lean tanky-slow, Soviet infantry frail — the *opposite* of their tank lean.

**TODO:** pull raw RA2 rules Strength values (Rhino, Apocalypse, Prism, Mirage, Tesla
Trooper, Flak Trooper, Desolator, Navy SEAL, GGI, Chrono Legionnaire, Brute, Virus…).

---

## To extend (living doc)
- RA2/YR full raw HP + damage (rules Strength values).
- C&C Tiberian Dawn & Tiberian Sun unit stats.
- Dune 2000 / Emperor unit stats.
- Then: build the cross-game *relative* normalization (per role) that seeds the vehicle
  and aircraft passes, and correct the per-type leans in `FACTION_IDENTITY.md`.
