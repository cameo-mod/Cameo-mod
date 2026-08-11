# audit_recent_changes — last 14 day(s) of history

Commits reviewed: **313**, files touched: **596**

| code | meaning | count | blocking |
|---|---|---|---|
| R1 | balance yaml edited without the ledger | 32 | yes |
| R2 | audit script never run by run_all.sh | 0 | yes |
| R3 | commit without a Co-Authored-By trailer | 80 | yes |
| R4 | engine/mod.config change (needs boot gate) | 8 | no |


## R1 — hand-edited balance numbers (32)

| commit | date | subject | fields |
|---|---|---|---|
| 5a14355e | 2026-08-11 | feat(physical-state): BUILD 3 — SonicDebuff rena | Range |
| 14713d57 | 2026-08-11 | fix(tesla): rename extra-damage chips and restor | Damage |
| 0d2cd6e8 | 2026-08-10 | feat(warhead): auto-scaling Integrity/EMP + unif | Damage, Spread |
| 39995bba | 2026-08-10 | balance(weapons): wire D2K_StormGunInf/Cymek to  | Damage |
| 4e9c3198 | 2026-08-10 | balance(weapons): collapse Exorcist family + Shr | Damage |
| f926d461 | 2026-08-10 | balance(weapons): Magic 5x-Sonic giant-killer, S | Damage, Range, ReloadDelay, Spread |
| 86b88ed3 | 2026-08-10 | feat(weapons): add Storm blend family (Tesla + M | Damage, Range, ReloadDelay, Spread |
| 606adc36 | 2026-08-10 | fix(weapons): Magic %-equalizer encodes magnitud | Damage, Range, ReloadDelay, Spread |
| fefb19f6 | 2026-08-10 | Improve bullet casing ejection (#249) | Speed |
| ea160f40 | 2026-08-10 | Restore autogun projectile visuals (#248) | Speed |
| bb46a5c3 | 2026-08-10 | feat(weapons): Tesla L/M extrapolation + Thermob | Damage, Range, ReloadDelay, Spread |
| 7ac87f46 | 2026-08-10 | feat(weapons): add Quantum blend family (Railgun | Damage, Range, ReloadDelay, Spread |
| a461f21b | 2026-08-09 | feat(weapons): add Thermobaric blend family (Dem | Damage, Range, ReloadDelay, Spread |
| 2e6d6968 | 2026-08-09 | feat(physical-state): multi-state C# + Plasma fa | Damage, Range, ReloadDelay, Spread |
| 08c07e5d | 2026-08-08 | fix(weapons): add ApplyPhysicalState twins to ^E | Range |
| b068a94f | 2026-08-08 | feat(warheads): energy ExtraDamage chips (paid-f | Damage, Spread |
| e7399983 | 2026-08-04 | fix(audit): correct MinimumExposure, MinRange, r | MinRange |
| 9c801f51 | 2026-08-04 | fix(weapons): correct HighV to Bullet_Medium and | Damage |
| 7f8a10e5 | 2026-08-04 | fix(generator): enforce 2000-damage grid in warh | Damage |
| a5185ca1 | 2026-08-04 | refactor(weapons): convert HighV to 3-way templa | Range |
| 3d110272 | 2026-08-04 | fix(balance): reduce MissileAA spread via genera | Spread |
| 2bc3034c | 2026-08-04 | fix(weapons): add missing generic_bullet_casing  | Range, ReloadDelay, Speed |
| 5291151c | 2026-08-04 | chore: clean trailing whitespace in advancewars. | HP |
| 1b638bf2 | 2026-08-04 | feat(warheads): nuclear AreaDamage + AreaDamageP | Damage, Spread |
| 956cf1ec | 2026-08-02 | feat(weapons): warhead layer — add FriendlyFire  | Damage, Spread |
| c240b615 | 2026-08-03 | Balance RA2 Soviet Sentry Gun burst | Burst, ReloadDelay |
| 0a664903 | 2026-08-02 | feat(weapons): 3-way split layers 2+3 — projecti | Damage, Range, Speed |
| 78bcba8f | 2026-08-03 | Add spent casing effects to gun defenses (#238) | Range, Speed |
| 930b72c9 | 2026-07-30 | Raise TD rocket soldier cost 200->300 (weak at 2 | Cost |
| 4877a61b | 2026-07-29 | Rework AttractsWorms, Passenger.Weight, Delivers | Burst, BurstDelays, Damage, HP, MinRange, Range, ReloadDelay, Speed, Spread |
| c2cb7394 | 2026-07-29 | Reduce StarCraft worker death explosion damage ( | Damage |
| 43df3923 | 2026-07-28 | fix: rename latinsyndicate_latintankkiller to la | Range |


## R2 — audits missing from run_all.sh (0)

_none found_


## R3 — commits without provenance (80)

| commit | date | author | problem |
|---|---|---|---|
| f2284b1c | 2026-08-11 | Blackrobe | no Co-Authored-By trailer |
| 3ffb482c | 2026-08-11 | Blackrobe | no Co-Authored-By trailer |
| 7155a0f1 | 2026-08-11 | AedisToru | no Co-Authored-By trailer |
| 59ade89e | 2026-08-11 | AedisToru | no Co-Authored-By trailer |
| 12bd3f5f | 2026-08-10 | Blackrobe | no Co-Authored-By trailer |
| fefb19f6 | 2026-08-10 | Blackrobe | no Co-Authored-By trailer |
| ea160f40 | 2026-08-10 | Blackrobe | no Co-Authored-By trailer |
| d7716457 | 2026-08-10 | Blackrobe | no Co-Authored-By trailer |
| 10c08582 | 2026-08-10 | Blackrobe | no Co-Authored-By trailer |
| 916b0d7d | 2026-08-09 | Blackrobe | no Co-Authored-By trailer |
| a7f53cef | 2026-08-09 | Blackrobe | no Co-Authored-By trailer |
| 0ab5c5f4 | 2026-08-09 | Blackrobe | no Co-Authored-By trailer |
| c73eab50 | 2026-08-09 | Blackrobe | no Co-Authored-By trailer |
| 2f80c05d | 2026-08-04 | AedisToru | no Co-Authored-By trailer |
| b26ba5ac | 2026-08-04 | AedisToru | no Co-Authored-By trailer |
| 7bc8861d | 2026-08-04 | AedisToru | no Co-Authored-By trailer |
| b6a58b76 | 2026-08-04 | AedisToru | no Co-Authored-By trailer |
| 714806d3 | 2026-08-02 | AedisToru | no Co-Authored-By trailer |
| f496c18b | 2026-08-02 | AedisToru | no Co-Authored-By trailer |
| 9a994768 | 2026-08-02 | AedisToru | no Co-Authored-By trailer |
| 375b2e28 | 2026-08-03 | Blackrobe | no Co-Authored-By trailer |
| 09784a24 | 2026-08-03 | Blackrobe | no Co-Authored-By trailer |
| c240b615 | 2026-08-03 | Blackrobe | no Co-Authored-By trailer |
| 9b2fd7b7 | 2026-08-03 | Blackrobe | no Co-Authored-By trailer |
| 54e91e0d | 2026-08-03 | Blackrobe | no Co-Authored-By trailer |
| 78bcba8f | 2026-08-03 | Blackrobe | no Co-Authored-By trailer |
| 87de13aa | 2026-08-02 | Blackrobe | no Co-Authored-By trailer |
| 87ed1b7c | 2026-08-02 | Blackrobe | no Co-Authored-By trailer |
| b3fd8932 | 2026-08-02 | Blackrobe | no Co-Authored-By trailer |
| 66cc03bb | 2026-08-02 | Blackrobe | no Co-Authored-By trailer |
| 79fe0ea7 | 2026-08-02 | AedisToru | no Co-Authored-By trailer |
| 0fc453db | 2026-08-02 | Blackrobe | no Co-Authored-By trailer |
| a131c9e1 | 2026-08-01 | Blackrobe | no Co-Authored-By trailer |
| 6e210c35 | 2026-08-01 | Blackrobe | no Co-Authored-By trailer |
| f9454587 | 2026-07-31 | AedisToru | no Co-Authored-By trailer |
| 6ca064f0 | 2026-07-31 | AedisToru | no Co-Authored-By trailer |
| 476e79ce | 2026-07-31 | AedisToru | no Co-Authored-By trailer |
| 42547fe9 | 2026-07-31 | AedisToru | no Co-Authored-By trailer |
| bd884a17 | 2026-07-31 | AedisToru | no Co-Authored-By trailer |
| 2a2c8f07 | 2026-07-31 | AedisToru | no Co-Authored-By trailer |
| a7f01417 | 2026-07-31 | AedisToru | no Co-Authored-By trailer |
| 2202abf1 | 2026-07-31 | AedisToru | no Co-Authored-By trailer |
| 55fc0635 | 2026-07-31 | AedisToru | no Co-Authored-By trailer |
| f0d7dd12 | 2026-07-31 | AedisToru | no Co-Authored-By trailer |
| 3b1c547d | 2026-07-31 | AedisToru | no Co-Authored-By trailer |
| f68a0183 | 2026-07-31 | AedisToru | no Co-Authored-By trailer |
| 8f83b6fa | 2026-07-31 | AedisToru | no Co-Authored-By trailer |
| 8a89bdd2 | 2026-07-31 | AedisToru | no Co-Authored-By trailer |
| faf4a5f8 | 2026-07-31 | AedisToru | no Co-Authored-By trailer |
| 7a296c96 | 2026-07-31 | AedisToru | no Co-Authored-By trailer |
| 86508c13 | 2026-07-31 | AedisToru | no Co-Authored-By trailer |
| 0016079c | 2026-07-31 | AedisToru | no Co-Authored-By trailer |
| a939c929 | 2026-07-31 | AedisToru | no Co-Authored-By trailer |
| 842c3e46 | 2026-07-31 | AedisToru | no Co-Authored-By trailer |
| 13029589 | 2026-07-31 | AedisToru | no Co-Authored-By trailer |
| decede7f | 2026-07-31 | AedisToru | no Co-Authored-By trailer |
| cd347b0e | 2026-07-31 | AedisToru | no Co-Authored-By trailer |
| 1072298f | 2026-07-30 | AedisToru | no Co-Authored-By trailer |
| bcb1a8f0 | 2026-07-30 | AedisToru | no Co-Authored-By trailer |
| 626aa9e0 | 2026-07-30 | AedisToru | no Co-Authored-By trailer |
| 930b72c9 | 2026-07-30 | AedisToru | no Co-Authored-By trailer |
| b0805ed5 | 2026-07-30 | AedisToru | no Co-Authored-By trailer |
| 546c52c4 | 2026-07-30 | AedisToru | no Co-Authored-By trailer |
| 768379bf | 2026-07-30 | AedisToru | no Co-Authored-By trailer |
| 61443ae8 | 2026-07-30 | AedisToru | no Co-Authored-By trailer |
| 952d747c | 2026-07-30 | AedisToru | no Co-Authored-By trailer |
| d2cab5bb | 2026-07-30 | AedisToru | no Co-Authored-By trailer |
| 1716149b | 2026-07-30 | AedisToru | no Co-Authored-By trailer |
| a6e9a386 | 2026-07-29 | AedisToru | no Co-Authored-By trailer |
| 4877a61b | 2026-07-29 | AedisToru | no Co-Authored-By trailer |
| a4739554 | 2026-07-29 | AedisToru | no Co-Authored-By trailer |
| 11fa20e2 | 2026-07-29 | AedisToru | no Co-Authored-By trailer |
| d227b8fb | 2026-07-29 | AedisToru | no Co-Authored-By trailer |
| c47242e8 | 2026-07-29 | AedisToru | no Co-Authored-By trailer |
| c2cb7394 | 2026-07-29 | Blackrobe | no Co-Authored-By trailer |
| 43df3923 | 2026-07-28 | AedisToru | no Co-Authored-By trailer |
| 11e8219e | 2026-07-28 | AedisToru | no Co-Authored-By trailer |
| 8ec7714a | 2026-07-28 | AedisToru | no Co-Authored-By trailer |
| 7a8f8fdd | 2026-07-28 | AedisToru | no Co-Authored-By trailer |
| 983b17dd | 2026-07-28 | AedisToru | no Co-Authored-By trailer |


## R4 — engine/config changes to re-verify (8)

| commit | date | note |
|---|---|---|
| 1d5d5e55 | 2026-08-11 | mod.config changed (rebuild + boot gate required) |
| f2284b1c | 2026-08-11 | mod.config changed (rebuild + boot gate required) |
| ea160f40 | 2026-08-10 | mod.config changed (rebuild + boot gate required) |
| 5b9173cf | 2026-08-04 | mod.config changed (rebuild + boot gate required) |
| 6e210c35 | 2026-08-01 | mod.config changed (rebuild + boot gate required) |
| 6ca064f0 | 2026-07-31 | mod.config changed (rebuild + boot gate required) |
| 1716149b | 2026-07-30 | mod.config changed (rebuild + boot gate required) |
| 4877a61b | 2026-07-29 | mod.config changed (rebuild + boot gate required) |


## R5 — most-churned files (re-read these first)

| file | commits touching it |
|---|---|
| docs/AI_HANDOFF_2026-08-05.md | 62 |
| mods/cameo/weapons/weapons.yaml | 49 |
| docs/design/ROADMAP.md | 42 |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 42 |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 41 |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 37 |
| mods/cameo/weapons/redalert2.yaml | 36 |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 36 |
| mods/cameo/ContentPacks/RedAlert/Shared/yaml/weapons.yaml | 35 |
| mods/cameo/weapons/tiberiansun.yaml | 34 |
| docs/balance/redalert_soviets.json | 33 |
| mods/cameo/ContentPacks/D2k/Ixian/yaml/weapons.yaml | 33 |
| mods/cameo/ContentPacks/RedAlert2Mod/TKM/yaml/weapons.yaml | 33 |
| mods/cameo/weapons/redalert2mod.yaml | 32 |
| mods/cameo/ContentPacks/D2k/Ordos/yaml/weapons.yaml | 31 |


## Reviewer checklist (not machine-checkable)

- [ ] Every yaml change in the window boot-gated (`launch-game.cmd` reached the menu)?
- [ ] C# changes rebuilt (`dotnet build -c Release -p:TargetPlatform=win-x64`)?
- [ ] New actors/weapons named with underscores only, and Fluent keys added?
- [ ] Generated reports under `docs/audit/latest/` regenerated via run_all.sh, not hand-edited?
- [ ] ROADMAP.md updated for finished/queued work?


## Enforcement

R1/R3 block only for commits on or after **2026-08-11**: 2 R1 and 4 R3 of 32/80 findings are in scope; the rest predate the gate.


## FAIL

- 2 R1, 0 R2, 4 R3 blocking finding(s)

