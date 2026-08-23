# audit_recent_changes — last 14 day(s) of history

Commits reviewed: **104**, files touched: **31444**

| code | meaning | count | blocking |
|---|---|---|---|
| R1 | balance yaml edited without the ledger | 6 | yes |
| R2 | audit script never run by run_all.sh | 0 | yes |
| R3 | provenance (wrong-identity trailer blocks; missing one on the shared identity is review-only) | 5 | partly |
| R4 | engine/mod.config change (needs boot gate) | 9 | no |


## R1 — hand-edited balance numbers (6)

| commit | date | subject | fields |
|---|---|---|---|
| bd215785 | 2026-08-18 | feat(weapons): convert legacy flame/chemical App | Damage |
| b010cc6e | 2026-08-18 | Hover Transport Added | Cost, HP, Range, Speed |
| 786bb2f2 | 2026-08-18 | Added all Combat ships for GDI/Nod | Burst, BurstDelays, Cost, Damage, HP, Range, ReloadDelay, Speed |
| 0d2cd6e8 | 2026-08-10 | feat(warhead): auto-scaling Integrity/EMP + unif | Damage, Spread |
| 39995bba | 2026-08-10 | balance(weapons): wire D2K_StormGunInf/Cymek to  | Damage |
| 4e9c3198 | 2026-08-10 | balance(weapons): collapse Exorcist family + Shr | Damage |


## R2 — audits missing from run_all.sh (0)

_none found_


## R3 — commits without provenance (5)

| commit | date | author | problem | severity |
|---|---|---|---|---|
| 7800eaab | 2026-08-17 | Zan Yewang | agent trailer `Devin AI <devin@cognition.ai>` on a non-shared identity | review |
| 519105d4 | 2026-08-16 | Zan Yewang | agent trailer `Devin AI <devin@cognition.ai>` on a non-shared identity | review |
| e62ac4ea | 2026-08-16 | Zan Yewang | agent trailer `Devin AI <devin@cognition.ai>` on a non-shared identity | review |
| 988a7580 | 2026-08-11 | Devin AI | agent trailer `Devin AI <devin@cognition.ai>` on a non-shared identity | review |
| 1d5d5e55 | 2026-08-11 | Zan Yewang | agent trailer `Devin AI <devin@cognition.ai>` on a non-shared identity | review |


## R4 — engine/config changes to re-verify (9)

| commit | date | note |
|---|---|---|
| c69604be | 2026-08-17 | mod.config changed (rebuild + boot gate required) |
| a74638de | 2026-08-16 | mod.config changed (rebuild + boot gate required) |
| 41f2870b | 2026-08-16 | mod.config changed (rebuild + boot gate required) |
| 13379957 | 2026-08-16 | mod.config changed (rebuild + boot gate required) |
| 988a7580 | 2026-08-11 | mod.config changed (rebuild + boot gate required) |
| 1d5d5e55 | 2026-08-11 | mod.config changed (rebuild + boot gate required) |
| f2284b1c | 2026-08-11 | mod.config changed (rebuild + boot gate required) |
| 37686675 | 2026-08-11 | mod.config changed (rebuild + boot gate required) |
| f926d461 | 2026-08-10 | mod.config changed (rebuild + boot gate required) |


## R5 — most-churned files (re-read these first)

| file | commits touching it |
|---|---|
| mods/cameo/weapons/weapons.yaml | 32 |
| tools/balance/gen_weapon_template.py | 26 |
| docs/design/BALANCE_PROGRAM_PLAN.md | 23 |
| docs/design/PHYSICAL_STATE_SYSTEM.md | 14 |
| docs/balance/derived/tiberiansun_forgotten.json | 13 |
| docs/balance/derived/warcraft2_humans.json | 13 |
| mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml | 12 |
| mods/cameo/ContentPacks/TiberianSun/GDI/yaml/weapons.yaml | 12 |
| docs/balance/tiberiansun_gdi.json | 12 |
| docs/balance/derived/redalert2mod_futuretech.json | 12 |
| docs/balance/derived/starcraft_zerg.json | 12 |
| docs/balance/derived/tiberiansun_nod.json | 12 |
| DEVELOPMENT_LOG.md | 12 |
| docs/balance/derived/redalert2mod_consortium.json | 12 |
| docs/balance/derived/starcraft_protoss.json | 12 |


## Reviewer checklist (not machine-checkable)

- [ ] Every yaml change in the window boot-gated (`launch-game.cmd` reached the menu)?
- [ ] C# changes rebuilt (`dotnet build -c Release -p:TargetPlatform=win-x64`)?
- [ ] New actors/weapons named with underscores only, and Fluent keys added?
- [ ] Generated reports under `docs/audit/latest/` regenerated via run_all.sh, not hand-edited?
- [ ] ROADMAP.md updated for finished/queued work?


## Enforcement

R1/R3 block only for commits on or after **2026-08-12**: 3 R1 and 0 R3 of 6/5 findings are in scope; the rest predate the gate.


## FAIL

- 3 R1, 0 R2, 0 R3 blocking finding(s)

