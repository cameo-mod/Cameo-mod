# Folded percentage runtime audit

- Reachable direct-hit weapons activated: **186**
- Folded direct-hit applications activated: **189**
- Direct weapons also carrying standalone percentage hits: **7**
- Direct weapons whose folded hit feeds physical state: **7**
- Direct weapons whose folded hit feeds integrity: **5**
- Legacy Int32 overflow applications repaired: **8**
- Authored shared-mode (SharedVersus) applications: **5** (of which zero-unit: **3** — visibly listed, not counted as percentage hits)
- Non-default direct relationship sets: **0**
- Dispatch structural findings: **0**

## Repaired overflow cases

A repair is ONLY a legacy Int32 wrap independent of SharedVersus. The `runtime units` column shows the value actually applied today; for shared-mode rows it already carries the approved h/2 design scale (Scale itself moved 10000 -> 2000), so that column alone is NOT the before-migration value.

| weapon | warhead | legacy Int32 units | wide legacy units | runtime units |
|---|---|---:|---:|---:|
| `DalekCannon` | `Railgun_HeavyFlatCompatibility` | -6474 | 15000 | 15000 |
| `DalekCannon_elite` | `Railgun_HeavyFlatCompatibility` | 8525 | 30000 | 30000 |
| `ExecutionerSword` | `Melee_Medium` | 8525 | 30000 | 30000 |
| `Hakureiring2` | `Magic_Heavy` | -9474 | 12000 | 12000 |
| `OIHakureiring2` | `Magic_Heavy` | -9474 | 12000 | 12000 |
| `TSLocustBombChem` | `Chemical_Heavy` | -7974 | 13500 | 13500 |
| `d2kStormLasher` | `Storm_Heavy` | -6474 | 15000 | 15000 |
| `sandmarinemortar` | `Demolition_HeavyFlatCompatibility` | -9474 | 12000 | 12000 |

## Shared-mode inventory

Every authored reachable SharedVersus AreaDamage application, including zero-unit profiles. Zero-unit rows are authored configurations with no effective percentage hit, NOT activated percentage hits.

| weapon | warhead | h | Damage | Scale | shared runtime units |
|---|---|---:|---:|---:|---:|
| `RA2sabot` | `CannonAP` | 0 | 80000 | 2000 | 0 |
| `RA2sabot_elite` | `CannonAP` | 0 | 160000 | 2000 | 0 |
| `TS90mm` | `CannonAP` | 1000 | 6000 | 2000 | 30 |
| `TS90mmDep` | `CannonAP` | 1000 | 6000 | 2000 | 30 |
| `corrino_buggy_gun` | `CannonAP` | 0 | 3000 | 2000 | 0 |

## Zero-unit shared applications

- `RA2sabot`:`CannonAP` (h=0, Damage=80000, Scale=2000) — shared units 0
- `RA2sabot_elite`:`CannonAP` (h=0, Damage=160000, Scale=2000) — shared units 0
- `corrino_buggy_gun`:`CannonAP` (h=0, Damage=3000, Scale=2000) — shared units 0

## Direct-hit mixed effects

- Standalone plus folded: `RA2HeavyMirageGun`, `RA2HeavyMirageGun_elite`, `RA2MirageGun`, `RA2MirageGun_elite`, `SteelMegaSword_elite`, `Tentacle`, `WaveTurretImpact`
- Physical state: `ChainGunMH60Cryo`, `M60mgCryo`, `NaxDieGlocke`, `RAVulcanCryo`, `SheridanVulcanCryo`, `wc2deathknightDeathAndDecay_Hit`, `zsu_23Cryo`
- Integrity: `PsiStorm`, `RA2DiskDrain`, `TSSonicZapWeapon`, `WaveArtilleryImpact`, `WaveTurretImpact`

_PASS — the active rules contain no invalid or double-percentage shapes._
