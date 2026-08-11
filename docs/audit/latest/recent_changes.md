# audit_recent_changes — last 14 day(s) of history

Commits reviewed: **327**, files touched: **643**

| code | meaning | count | blocking |
|---|---|---|---|
| R1 | balance yaml edited without the ledger | 14 | yes |
| R2 | audit script never run by run_all.sh | 0 | yes |
| R3 | provenance (wrong-identity trailer blocks; missing one on the shared identity is review-only) | 56 | partly |
| R4 | engine/mod.config change (needs boot gate) | 8 | no |


## R1 — hand-edited balance numbers (14)

| commit | date | subject | fields |
|---|---|---|---|
| 14713d57 | 2026-08-11 | fix(tesla): rename extra-damage chips and restor | Damage |
| 0d2cd6e8 | 2026-08-10 | feat(warhead): auto-scaling Integrity/EMP + unif | Damage, Spread |
| 39995bba | 2026-08-10 | balance(weapons): wire D2K_StormGunInf/Cymek to  | Damage |
| 4e9c3198 | 2026-08-10 | balance(weapons): collapse Exorcist family + Shr | Damage |
| fefb19f6 | 2026-08-10 | Improve bullet casing ejection (#249) | Speed |
| ea160f40 | 2026-08-10 | Restore autogun projectile visuals (#248) | Speed |
| e7399983 | 2026-08-04 | fix(audit): correct MinimumExposure, MinRange, r | MinRange |
| 9c801f51 | 2026-08-04 | fix(weapons): correct HighV to Bullet_Medium and | Damage |
| a5185ca1 | 2026-08-04 | refactor(weapons): convert HighV to 3-way templa | Range |
| 2bc3034c | 2026-08-04 | fix(weapons): add missing generic_bullet_casing  | Range, ReloadDelay, Speed |
| c240b615 | 2026-08-03 | Balance RA2 Soviet Sentry Gun burst | Burst, ReloadDelay |
| 78bcba8f | 2026-08-03 | Add spent casing effects to gun defenses (#238) | Range, Speed |
| 4877a61b | 2026-07-29 | Rework AttractsWorms, Passenger.Weight, Delivers | Burst, BurstDelays, Damage, HP, MinRange, Range, ReloadDelay, Speed, Spread |
| c2cb7394 | 2026-07-29 | Reduce StarCraft worker death explosion damage ( | Damage |


## R2 — audits missing from run_all.sh (0)

_none found_


## R3 — commits without provenance (56)

| commit | date | author | problem | severity |
|---|---|---|---|---|
| 1d5d5e55 | 2026-08-11 | Zan Yewang | agent trailer `Devin AI <devin@cognition.ai>` on a non-shared identity | review |
| 7155a0f1 | 2026-08-11 | AedisToru | no Co-Authored-By trailer (shared identity) | review |
| 59ade89e | 2026-08-11 | AedisToru | no Co-Authored-By trailer (shared identity) | review |
| 2f80c05d | 2026-08-04 | AedisToru | no Co-Authored-By trailer (shared identity) | review |
| b26ba5ac | 2026-08-04 | AedisToru | no Co-Authored-By trailer (shared identity) | review |
| 7bc8861d | 2026-08-04 | AedisToru | no Co-Authored-By trailer (shared identity) | review |
| b6a58b76 | 2026-08-04 | AedisToru | no Co-Authored-By trailer (shared identity) | review |
| 714806d3 | 2026-08-02 | AedisToru | no Co-Authored-By trailer (shared identity) | review |
| f496c18b | 2026-08-02 | AedisToru | no Co-Authored-By trailer (shared identity) | review |
| 9a994768 | 2026-08-02 | AedisToru | no Co-Authored-By trailer (shared identity) | review |
| 79fe0ea7 | 2026-08-02 | AedisToru | no Co-Authored-By trailer (shared identity) | review |
| f9454587 | 2026-07-31 | AedisToru | no Co-Authored-By trailer (shared identity) | review |
| 6ca064f0 | 2026-07-31 | AedisToru | no Co-Authored-By trailer (shared identity) | review |
| 476e79ce | 2026-07-31 | AedisToru | no Co-Authored-By trailer (shared identity) | review |
| 42547fe9 | 2026-07-31 | AedisToru | no Co-Authored-By trailer (shared identity) | review |
| bd884a17 | 2026-07-31 | AedisToru | no Co-Authored-By trailer (shared identity) | review |
| 2a2c8f07 | 2026-07-31 | AedisToru | no Co-Authored-By trailer (shared identity) | review |
| a7f01417 | 2026-07-31 | AedisToru | no Co-Authored-By trailer (shared identity) | review |
| 2202abf1 | 2026-07-31 | AedisToru | no Co-Authored-By trailer (shared identity) | review |
| 55fc0635 | 2026-07-31 | AedisToru | no Co-Authored-By trailer (shared identity) | review |
| f0d7dd12 | 2026-07-31 | AedisToru | no Co-Authored-By trailer (shared identity) | review |
| 3b1c547d | 2026-07-31 | AedisToru | no Co-Authored-By trailer (shared identity) | review |
| f68a0183 | 2026-07-31 | AedisToru | no Co-Authored-By trailer (shared identity) | review |
| 8f83b6fa | 2026-07-31 | AedisToru | no Co-Authored-By trailer (shared identity) | review |
| 8a89bdd2 | 2026-07-31 | AedisToru | no Co-Authored-By trailer (shared identity) | review |
| faf4a5f8 | 2026-07-31 | AedisToru | no Co-Authored-By trailer (shared identity) | review |
| 7a296c96 | 2026-07-31 | AedisToru | no Co-Authored-By trailer (shared identity) | review |
| 86508c13 | 2026-07-31 | AedisToru | no Co-Authored-By trailer (shared identity) | review |
| 0016079c | 2026-07-31 | AedisToru | no Co-Authored-By trailer (shared identity) | review |
| a939c929 | 2026-07-31 | AedisToru | no Co-Authored-By trailer (shared identity) | review |
| 842c3e46 | 2026-07-31 | AedisToru | no Co-Authored-By trailer (shared identity) | review |
| 13029589 | 2026-07-31 | AedisToru | no Co-Authored-By trailer (shared identity) | review |
| decede7f | 2026-07-31 | AedisToru | no Co-Authored-By trailer (shared identity) | review |
| cd347b0e | 2026-07-31 | AedisToru | no Co-Authored-By trailer (shared identity) | review |
| 1072298f | 2026-07-30 | AedisToru | no Co-Authored-By trailer (shared identity) | review |
| bcb1a8f0 | 2026-07-30 | AedisToru | no Co-Authored-By trailer (shared identity) | review |
| 626aa9e0 | 2026-07-30 | AedisToru | no Co-Authored-By trailer (shared identity) | review |
| 930b72c9 | 2026-07-30 | AedisToru | no Co-Authored-By trailer (shared identity) | review |
| b0805ed5 | 2026-07-30 | AedisToru | no Co-Authored-By trailer (shared identity) | review |
| 546c52c4 | 2026-07-30 | AedisToru | no Co-Authored-By trailer (shared identity) | review |
| 768379bf | 2026-07-30 | AedisToru | no Co-Authored-By trailer (shared identity) | review |
| 61443ae8 | 2026-07-30 | AedisToru | no Co-Authored-By trailer (shared identity) | review |
| 952d747c | 2026-07-30 | AedisToru | no Co-Authored-By trailer (shared identity) | review |
| d2cab5bb | 2026-07-30 | AedisToru | no Co-Authored-By trailer (shared identity) | review |
| 1716149b | 2026-07-30 | AedisToru | no Co-Authored-By trailer (shared identity) | review |
| a6e9a386 | 2026-07-29 | AedisToru | no Co-Authored-By trailer (shared identity) | review |
| 4877a61b | 2026-07-29 | AedisToru | no Co-Authored-By trailer (shared identity) | review |
| a4739554 | 2026-07-29 | AedisToru | no Co-Authored-By trailer (shared identity) | review |
| 11fa20e2 | 2026-07-29 | AedisToru | no Co-Authored-By trailer (shared identity) | review |
| d227b8fb | 2026-07-29 | AedisToru | no Co-Authored-By trailer (shared identity) | review |
| c47242e8 | 2026-07-29 | AedisToru | no Co-Authored-By trailer (shared identity) | review |
| 43df3923 | 2026-07-28 | AedisToru | no Co-Authored-By trailer (shared identity) | review |
| 11e8219e | 2026-07-28 | AedisToru | no Co-Authored-By trailer (shared identity) | review |
| 8ec7714a | 2026-07-28 | AedisToru | no Co-Authored-By trailer (shared identity) | review |
| 7a8f8fdd | 2026-07-28 | AedisToru | no Co-Authored-By trailer (shared identity) | review |
| 983b17dd | 2026-07-28 | AedisToru | no Co-Authored-By trailer (shared identity) | review |


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
| docs/AI_HANDOFF_2026-08-05.md | 63 |
| mods/cameo/weapons/weapons.yaml | 50 |
| docs/design/ROADMAP.md | 45 |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 42 |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 41 |
| mods/cameo/weapons/tiberiansun.yaml | 37 |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 37 |
| docs/balance/tiberiansun_gdi.json | 36 |
| mods/cameo/weapons/redalert2.yaml | 36 |
| mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml | 36 |
| docs/balance/redalert_soviets.json | 35 |
| mods/cameo/ContentPacks/TiberianSun/GDI/yaml/weapons.yaml | 35 |
| mods/cameo/ContentPacks/RedAlert/Shared/yaml/weapons.yaml | 35 |
| mods/cameo/ContentPacks/D2k/Ixian/yaml/weapons.yaml | 33 |
| mods/cameo/ContentPacks/RedAlert2Mod/TKM/yaml/weapons.yaml | 33 |


## Reviewer checklist (not machine-checkable)

- [ ] Every yaml change in the window boot-gated (`launch-game.cmd` reached the menu)?
- [ ] C# changes rebuilt (`dotnet build -c Release -p:TargetPlatform=win-x64`)?
- [ ] New actors/weapons named with underscores only, and Fluent keys added?
- [ ] Generated reports under `docs/audit/latest/` regenerated via run_all.sh, not hand-edited?
- [ ] ROADMAP.md updated for finished/queued work?


## Enforcement

R1/R3 block only for commits on or after **2026-08-12**: 0 R1 and 0 R3 of 14/56 findings are in scope; the rest predate the gate.

