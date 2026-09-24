# gen_damage_matrix — armor classes & Versus aggregates (§8.1)

Armor types in live actors: **21**, warheads with Versus tables: **5616**


## Armor types referenced by actors

ARMOR, Bomber, COMPOSITE, Concrete, Fighter, Flak, HAZMAT, Heavy, Helicopter, Heroic, Light, Medium, None, Plate, REFLECTOR, Scout, Shield, Spaceship, Steel, Superheavy, Wood


## Versus aggregate per armor type (across all warheads)

| armor type | #warheads naming it | mean Versus | min | max |
|---|---|---|---|---|
| ARMOR | 2627 | 70% | 70 | 70 |
| BLAST | 2627 | 70% | 36 | 106 |
| Bomber | 5363 | 44% | 1 | 196 |
| COMPOSITE | 2627 | 67% | 35 | 106 |
| Concrete | 5593 | 53% | 0 | 200 |
| Fighter | 5363 | 44% | 1 | 200 |
| Flak | 5363 | 67% | 1 | 250 |
| HAZMAT | 2725 | 72% | 25 | 102 |
| Heavy | 5605 | 61% | 0 | 200 |
| Helicopter | 5363 | 43% | 1 | 177 |
| Heroic | 5363 | 48% | 1 | 300 |
| Light | 5607 | 60% | 1 | 200 |
| Medium | 5569 | 60% | 0 | 200 |
| None | 5596 | 74% | 1 | 1000 |
| Plate | 5363 | 67% | 1 | 275 |
| REFLECTOR | 3025 | 66% | 42 | 105 |
| Scout | 5363 | 58% | 1 | 200 |
| Shield | 5251 | 118% | 9 | 400 |
| Spaceship | 5363 | 43% | 1 | 191 |
| Steel | 5363 | 55% | 1 | 175 |
| Superheavy | 5367 | 62% | 1 | 200 |
| Wood | 5610 | 59% | 0 | 200 |
| harvester | 28 | 41% | 25 | 100 |
| invulnerable | 28 | 0% | 0 | 0 |
| wall | 26 | 49% | 5 | 100 |


_Armor types with 0 warhead references are either default-100% targets everywhere or orphaned armor classes — cross-check with audit_orphans. Full per-warhead dump: run with --full._

