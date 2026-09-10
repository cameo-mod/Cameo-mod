# Warhead-split guard (multi-warhead over-damage)


## FAIL 1 — broadcast fingerprint / every MAIN identical (55 vs baseline 69)

_at or below baseline_ — pre-existing **W24** debt (55 weapons), not a regression. The ratchet catches new broadcasts without blocking every commit on the existing pile. **Lower `BROADCAST_BASELINE` as W24 collapses weapons; never raise it.**

| weapon | mains | per_warhead | total |
|---|---|---|---|
| 12MissilesSpawnerScud | 4 | 24000 | 96000 |
| AlliedTankDestroyerCannon | 2 | 12000 | 24000 |
| AsianChaosMine | 2 | 125000 | 250000 |
| AsianPhoenixRocket | 3 | 20000 | 60000 |
| AsianPhoenixRocket_elite | 3 | 20000 | 60000 |
| BikeRockets | 2 | 8000 | 16000 |
| D2K_Rocket_Trooper2 | 3 | 8000 | 24000 |
| DredMissile | 3 | 30000 | 90000 |
| HellfireCryo | 2 | 8000 | 16000 |
| IdolCannon | 4 | 10000 | 40000 |
| JimRaynorMachineGun | 2 | 2000 | 4000 |
| MagicOrb | 2 | 12000 | 24000 |
| MagicOrb2 | 2 | 4000 | 8000 |
| NaxiMP40 | 3 | 2000 | 6000 |
| NaxiMP40_elite | 3 | 2000 | 6000 |
| NodTorpTube | 2 | 8000 | 16000 |
| NodTorpTubeBlackMarket | 2 | 8000 | 16000 |
| OIBigPlasmaCannon | 3 | 8000 | 24000 |
| RA2Comet | 3 | 20000 | 60000 |
| RA2Comet_elite | 3 | 20000 | 60000 |
| RA2DiskDrain | 2 | 2000 | 4000 |
| RA2KirovBomb_nuclear | 2 | 80000 | 160000 |
| RA2KirovBomb_nuclear_elite | 2 | 100000 | 200000 |
| RA2KirovBomb_rad | 2 | 48000 | 96000 |
| RA2KirovBomb_tesla | 2 | 80000 | 160000 |
| RA2Robotmm | 3 | 8000 | 24000 |
| RA2Robotmm_elite | 3 | 8000 | 24000 |
| RA2SCUD | 3 | 30000 | 90000 |
| RA2SCUDELITE | 4 | 30000 | 120000 |
| RA2SCUD_fire | 3 | 30000 | 90000 |
| RA2SCUD_tesla | 3 | 30000 | 90000 |
| SCUD | 2 | 60000 | 120000 |
| SCUDIrak | 2 | 60000 | 120000 |
| SamuraiBladeCharged | 2 | 10000 | 20000 |
| SkyHawkChainGun | 2 | 2000 | 4000 |
| SkyHawkChainGunWaveforce | 3 | 2000 | 6000 |
| TS155mm_bluenuke | 2 | 60000 | 120000 |
| TSTacticalChemMissileDamage | 2 | 10000 | 20000 |
| TSTacticalMissileDamage | 2 | 10000 | 20000 |
| TSVulcan | 2 | 2000 | 4000 |


_... and 15 more._


## Review — exact gameplay restorations (0)

_none found_


## Review — routing-revealed composites (0)

Exact-fingerprint exceptions for pre-existing composites whose dead legacy slots previously masked them from the ratchet. Any main-key or damage change removes the exception and is checked normally.

_none found_


## FAIL 2 — FriendlyFire louder than the shot (0)

None. ✅


## Review — high uniform stacks (informational, 17)

Allowed, but 8000+ per-warhead x N is a big total — confirm it is intended (not flattening residue).

| weapon | mains | per_warhead | total |
|---|---|---|---|
| 12MissilesSpawnerScud | 4 | 24000 | 96000 |
| AsianPhoenixRocket | 3 | 20000 | 60000 |
| AsianPhoenixRocket_elite | 3 | 20000 | 60000 |
| D2K_Rocket_Trooper2 | 3 | 8000 | 24000 |
| DredMissile | 3 | 30000 | 90000 |
| IdolCannon | 4 | 10000 | 40000 |
| OIBigPlasmaCannon | 3 | 8000 | 24000 |
| RA2Comet | 3 | 20000 | 60000 |
| RA2Comet_elite | 3 | 20000 | 60000 |
| RA2Robotmm | 3 | 8000 | 24000 |
| RA2Robotmm_elite | 3 | 8000 | 24000 |
| RA2SCUD | 3 | 30000 | 90000 |
| RA2SCUDELITE | 4 | 30000 | 120000 |
| RA2SCUD_fire | 3 | 30000 | 90000 |
| RA2SCUD_tesla | 3 | 30000 | 90000 |
| V3Explode | 3 | 10000 | 30000 |
| YakTeslaBomb | 4 | 40000 | 160000 |

