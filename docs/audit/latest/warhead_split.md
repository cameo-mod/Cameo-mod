# Warhead-split guard (multi-warhead over-damage)


## FAIL 1 — broadcast fingerprint (4)

Every SpreadDamage warhead (mains + sides) shares one identical value — the 2026-07-22 broadcast bug. Fix by editing the per-shot TOTAL through the workbook so `distribute_damage` splits it, or by restoring the intended per-warhead values.

| weapon | mains | sides | damage |
|---|---|---|---|
| HydraSpit | 4 | 1 | 18000 |
| NanoArtilleryAG | 3 | 2 | 7777 |
| NaxiAlienPistol | 3 | 1 | 8000 |
| NaxiAlienPistol_elite | 3 | 1 | 8000 |


## FAIL 2 — FriendlyFire louder than the shot (0)

None. ✅


## Review — high uniform stacks (informational, 246)

Allowed, but 8000+ per-warhead x N is a big total — confirm it is intended (not flattening residue).

| weapon | mains | per_warhead | total |
|---|---|---|---|
| 110mm_Gun | 3 | 10000 | 30000 |
| 120mm_cobra | 4 | 30000 | 120000 |
| 120mm_cobra_deploy | 4 | 30000 | 120000 |
| 120mm_python | 4 | 30000 | 120000 |
| 120mm_python_deploy | 4 | 30000 | 120000 |
| 120mm_td | 4 | 14000 | 56000 |
| 12MissilesSpawnerScud | 4 | 24000 | 96000 |
| 155mm | 3 | 10000 | 30000 |
| 155mmBastion | 3 | 10000 | 30000 |
| 155mmBastionCryo | 3 | 10000 | 30000 |
| 155mmCryo | 3 | 10000 | 30000 |
| ArbiterCannon | 4 | 10000 | 40000 |
| ArcherArtilleryShell | 5 | 14000 | 70000 |
| ArtilleryExplode | 3 | 10000 | 30000 |
| AsianPhoenixRocket | 3 | 20000 | 60000 |
| AsianPhoenixRocket_elite | 3 | 20000 | 60000 |
| AthenaLaser | 6 | 32000 | 192000 |
| BCYamatoCannon | 9 | 16000 | 144000 |
| BallistaMultiShot | 4 | 10000 | 40000 |
| BallistaMultiShotEnergized | 4 | 10000 | 40000 |
| BallistaSingleShotAirEnergized | 8 | 20000 | 160000 |
| BallistaTowerMultiShot | 4 | 10000 | 40000 |
| BallistaTowerMultiShotEnergized | 4 | 10000 | 40000 |
| BlackEagleMissiles | 3 | 16000 | 48000 |
| BlackEagleMissiles_elite | 3 | 16000 | 48000 |
| BlackHandLaser | 3 | 48000 | 144000 |
| BuggyPlasmaGrenade | 3 | 20000 | 60000 |
| CabalArtilleryWalkerShell | 4 | 42000 | 168000 |
| CabalArtilleryWalkerShellUpgraded | 8 | 42000 | 336000 |
| CabalBeholderLaser | 4 | 10000 | 40000 |
| CabalCommandoPlasma | 3 | 50000 | 150000 |
| CabalCommandoPlasmaMk2 | 3 | 50000 | 150000 |
| CabalCommandoPlasmaMk2Neutron | 6 | 50000 | 300000 |
| CabalCommandoPlasmaNeutron | 6 | 50000 | 300000 |
| CabalHeavyReaperMissiles | 4 | 12000 | 48000 |
| CabalHeavyReaperMissiles_AA | 4 | 12000 | 48000 |
| CabalHunterKillerLasers_elite | 3 | 10000 | 30000 |
| CabalManticoreMissilesAA | 4 | 12000 | 48000 |
| CabalMothershipRockets | 6 | 10000 | 60000 |
| CabalReaperMissiles | 4 | 8000 | 32000 |
| CabalReaperMissiles_AA | 4 | 8000 | 32000 |
| CabalSubmarinePlasma | 3 | 25000 | 75000 |
| ChemRockets | 3 | 12000 | 36000 |
| ChemicalBikeRockets | 4 | 8000 | 32000 |
| ChemicalBikeRocketsExplosion | 4 | 8000 | 32000 |
| ChemicalHonestJohn | 4 | 30000 | 120000 |
| ChemicalStealthTankExplosion | 3 | 10000 | 30000 |
| ChemicalStealthTankMissiles | 3 | 10000 | 30000 |
| D2K_155mm | 4 | 12000 | 48000 |
| D2K_155mm2 | 4 | 12000 | 48000 |
| D2K_155mm3 | 3 | 8000 | 24000 |
| D2K_155mm_turret | 4 | 12000 | 48000 |
| D2K_APC_Rocket | 3 | 8000 | 24000 |
| D2K_APC_Rocket_AA | 3 | 8000 | 24000 |
| D2K_Rocket_Trooper | 3 | 8000 | 24000 |
| D2K_Rocket_Trooper1 | 3 | 8000 | 24000 |
| D2K_Rocket_Trooper2 | 3 | 8000 | 24000 |
| D2K_Rocket_Trooper_AA | 3 | 10000 | 30000 |
| D2K_Rocket_Trooper_AGOnly | 3 | 10000 | 30000 |
| D2K_SiegeQuad | 4 | 12000 | 48000 |
| DalekCannon | 3 | 100000 | 300000 |
| DalekCannon_elite | 3 | 200000 | 600000 |
| DeviatorMissile | 4 | 10000 | 40000 |
| DeviatorMissile_Artillery | 6 | 10000 | 60000 |
| DragoonCannon | 4 | 10000 | 40000 |
| DragunovSniper | 5 | 80000 | 400000 |
| DredMissile | 3 | 30000 | 90000 |
| DuelistTankCannon | 6 | 14000 | 84000 |
| Dune_SiegeMortar | 4 | 10000 | 40000 |
| EMPGrenade | 8 | 8000 | 64000 |
| FirehawkBomb | 4 | 10000 | 40000 |
| FutureMechPlasma | 3 | 10000 | 30000 |
| FutureMechPlasma_elite | 3 | 10000 | 30000 |
| FutureTankCannons | 6 | 100000 | 600000 |
| FutureTankCannons_elite | 6 | 100000 | 600000 |
| Future_Cryocopter_Rocket | 3 | 16000 | 48000 |
| Future_MultiMissile_Sigma | 5 | 10000 | 50000 |
| GDIRigMissilePod | 4 | 8000 | 32000 |
| GDIRigMissilePodAMT | 4 | 8000 | 32000 |
| GDISniperRifle | 4 | 8000 | 32000 |
| GladiusCannon | 11 | 10000 | 110000 |
| GoliathMk2Rockets | 4 | 8000 | 32000 |
| GuardianShoot | 3 | 8000 | 24000 |
| HammerheadArtillery | 3 | 11111 | 33333 |
| HydraSpit | 4 | 18000 | 72000 |
| IdolCannon | 4 | 10000 | 40000 |
| InfestedExplosion | 3 | 50000 | 150000 |
| IvanBomb | 3 | 30000 | 90000 |
| IvanBombAir | 3 | 30000 | 90000 |
| IxianBomb_EMP | 3 | 30000 | 90000 |
| JapanSuperBomb | 5 | 10000 | 50000 |
| JapanesePlasmaBomb | 3 | 10000 | 30000 |
| KodiakCannon | 5 | 8000 | 40000 |
| Laboratory_Bioball | 5 | 10000 | 50000 |
| LatinBuggyRocket | 4 | 10000 | 40000 |
| LatinBuggyRocket_elite | 4 | 10000 | 40000 |
| LatinMonkeyGrenade1 | 3 | 10000 | 30000 |
| LatinMonkeyGrenade2 | 3 | 10000 | 30000 |
| LatinMonkeyGrenade3 | 3 | 10000 | 30000 |
| LunarNaxiJadgDestroyer | 3 | 30000 | 90000 |
| LunarNaxiJadgDestroyer_elite | 3 | 30000 | 90000 |
| Lunar_GreenGrilleArty | 4 | 16000 | 64000 |
| Lunar_GreenGrilleArty_elite | 4 | 16000 | 64000 |
| Lunar_GreenJadgDestroyer | 4 | 30000 | 120000 |
| Lunar_GreenJadgDestroyer_elite | 4 | 30000 | 120000 |
| MadcapGun | 3 | 12000 | 36000 |
| MammothTusk2 | 3 | 16000 | 48000 |
| MammothTusk2TargetingComputer | 3 | 16000 | 48000 |
| MammothTusk2Thermobaric | 3 | 16000 | 48000 |
| MammothTusk2ThermobaricTargetingComputer | 3 | 16000 | 48000 |
| MammothTuskTesla | 4 | 8000 | 32000 |
| MammothTuskTeslaTargetingComputer | 4 | 8000 | 32000 |
| MarauderMissiles | 3 | 10000 | 30000 |
| MarineMG | 3 | 12000 | 36000 |
| MigMissiles | 4 | 8000 | 32000 |
| MigMissiles_AA | 4 | 8000 | 32000 |
| MigMissiles_AA_elite | 4 | 8000 | 32000 |
| MigMissiles_elite | 4 | 8000 | 32000 |
| MigMissiles_fire | 4 | 8000 | 32000 |
| MigMissiles_fire_elite | 4 | 8000 | 32000 |

