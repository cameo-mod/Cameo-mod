# audit_recent_changes — last 14 day(s) of history

Commits reviewed: **350**, files touched: **472**

| code | meaning | count | blocking |
|---|---|---|---|
| R1 | balance yaml edited without the ledger | 12 | yes |
| R2 | audit script never run by run_all.sh | 2 | yes |
| R3 | provenance (wrong-identity trailer blocks; missing one on the shared identity is review-only) | 9 | partly |
| R4 | engine/mod.config change (needs boot gate) | 5 | no |


## R1 — hand-edited balance numbers (12)

| commit | date | subject | fields |
|---|---|---|---|
| a20cda71 | 2026-08-12 | W2: convert wc2deathknightDeathAndDecay_Hit to I | Damage |
| 086efefc | 2026-08-11 | feat(balance): convert HonestJohn to 3-way split | Damage |
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


## R2 — audits missing from run_all.sh (2)

| script | problem |
|---|---|
| tools/audit/audit_damage_grid.py | not invoked by run_all.sh |
| tools/audit/audit_unconverted_templates.py | not invoked by run_all.sh |


## R3 — commits without provenance (9)

| commit | date | author | problem | severity |
|---|---|---|---|---|
| 519105d4 | 2026-08-16 | Zan Yewang | agent trailer `Devin AI <devin@cognition.ai>` on a non-shared identity | review |
| e62ac4ea | 2026-08-16 | Zan Yewang | agent trailer `Devin AI <devin@cognition.ai>` on a non-shared identity | review |
| 1d5d5e55 | 2026-08-11 | Zan Yewang | agent trailer `Devin AI <devin@cognition.ai>` on a non-shared identity | review |
| 7155a0f1 | 2026-08-11 | AedisToru | no Co-Authored-By trailer (shared identity) | review |
| 59ade89e | 2026-08-11 | AedisToru | no Co-Authored-By trailer (shared identity) | review |
| 2f80c05d | 2026-08-04 | AedisToru | no Co-Authored-By trailer (shared identity) | review |
| b26ba5ac | 2026-08-04 | AedisToru | no Co-Authored-By trailer (shared identity) | review |
| 7bc8861d | 2026-08-04 | AedisToru | no Co-Authored-By trailer (shared identity) | review |
| b6a58b76 | 2026-08-04 | AedisToru | no Co-Authored-By trailer (shared identity) | review |


## R4 — engine/config changes to re-verify (5)

| commit | date | note |
|---|---|---|
| d6e8712c | 2026-08-15 | mod.config changed (rebuild + boot gate required) |
| 1d5d5e55 | 2026-08-11 | mod.config changed (rebuild + boot gate required) |
| f2284b1c | 2026-08-11 | mod.config changed (rebuild + boot gate required) |
| ea160f40 | 2026-08-10 | mod.config changed (rebuild + boot gate required) |
| 5b9173cf | 2026-08-04 | mod.config changed (rebuild + boot gate required) |


## R5 — most-churned files (re-read these first)

| file | commits touching it |
|---|---|
| docs/AI_HANDOFF_2026-08-05.md | 63 |
| mods/cameo/weapons/weapons.yaml | 56 |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 39 |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 39 |
| docs/balance/redalert_soviets.json | 39 |
| docs/design/BALANCE_PROGRAM_PLAN.md | 37 |
| tools/balance/gen_weapon_template.py | 35 |
| mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml | 33 |
| docs/balance/tiberiansun_gdi.json | 33 |
| docs/balance/d2k_ixian.json | 31 |
| mods/cameo/ContentPacks/D2k/Ixian/yaml/weapons.yaml | 31 |
| docs/design/ROADMAP.md | 30 |
| mods/cameo/weapons/redalert2mod.yaml | 29 |
| mods/cameo/weapons/redalert2.yaml | 29 |
| mods/cameo/ContentPacks/RedAlert2Mod/TKM/yaml/weapons.yaml | 29 |


## Reviewer checklist (not machine-checkable)

- [ ] Every yaml change in the window boot-gated (`launch-game.cmd` reached the menu)?
- [ ] C# changes rebuilt (`dotnet build -c Release -p:TargetPlatform=win-x64`)?
- [ ] New actors/weapons named with underscores only, and Fluent keys added?
- [ ] Generated reports under `docs/audit/latest/` regenerated via run_all.sh, not hand-edited?
- [ ] ROADMAP.md updated for finished/queued work?


## Enforcement

R1/R3 block only for commits on or after **2026-08-12**: 1 R1 and 0 R3 of 12/9 findings are in scope; the rest predate the gate.


## FAIL

- 1 R1, 2 R2, 0 R3 blocking finding(s)

