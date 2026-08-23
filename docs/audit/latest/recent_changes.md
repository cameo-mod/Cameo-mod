# audit_recent_changes — last 14 day(s) of history

Commits reviewed: **230**, files touched: **31503**

| code | meaning | count | blocking |
|---|---|---|---|
| R1 | balance yaml edited without the ledger | 10 | yes |
| R2 | audit script never run by run_all.sh | 3 | yes |
| R3 | provenance (wrong-identity trailer blocks; missing one on the shared identity is review-only) | 7 | partly |
| R4 | engine/mod.config change (needs boot gate) | 10 | no |


## R1 — hand-edited balance numbers (10)

| commit | date | subject | fields |
|---|---|---|---|
| 47a66b6c | 2026-08-21 | fix(w24): the nuclear batch collapsed 15 warhead | Damage |
| 33959758 | 2026-08-21 | HeatRayBeam1-4: complete Inferno 3-way split + s | Range, ReloadDelay |
| 89c94c89 | 2026-08-20 | D2K: 3-way split OrniBomb and OrniBombC | Range |
| 86634636 | 2026-08-20 | W24: D2K ^ORocket/^OMissile 3-way split | MinRange |
| bd215785 | 2026-08-18 | feat(weapons): convert legacy flame/chemical App | Damage |
| b010cc6e | 2026-08-18 | Hover Transport Added | Cost, HP, Range, Speed |
| 786bb2f2 | 2026-08-18 | Added all Combat ships for GDI/Nod | Burst, BurstDelays, Cost, Damage, HP, Range, ReloadDelay, Speed |
| 0d2cd6e8 | 2026-08-10 | feat(warhead): auto-scaling Integrity/EMP + unif | Damage, Spread |
| 39995bba | 2026-08-10 | balance(weapons): wire D2K_StormGunInf/Cymek to  | Damage |
| 4e9c3198 | 2026-08-10 | balance(weapons): collapse Exorcist family + Shr | Damage |


## R2 — audits missing from run_all.sh (3)

| script | problem |
|---|---|
| tools/audit/audit_inline_effects.py | not invoked by run_all.sh |
| tools/audit/audit_upgrade_regression.py | not invoked by run_all.sh |
| tools/audit/audit_weapon_identity.py | not invoked by run_all.sh |


## R3 — commits without provenance (7)

| commit | date | author | problem | severity |
|---|---|---|---|---|
| 026963fd | 2026-08-23 | Claude | agent trailer `Claude Opus 5 <noreply@anthropic.com>` on a non-shared identity | review |
| 2bb046ae | 2026-08-20 | Zan Yewang | agent trailer `Devin AI <devin@cognition.ai>` on a non-shared identity | review |
| 7800eaab | 2026-08-17 | Zan Yewang | agent trailer `Devin AI <devin@cognition.ai>` on a non-shared identity | review |
| 519105d4 | 2026-08-16 | Zan Yewang | agent trailer `Devin AI <devin@cognition.ai>` on a non-shared identity | review |
| e62ac4ea | 2026-08-16 | Zan Yewang | agent trailer `Devin AI <devin@cognition.ai>` on a non-shared identity | review |
| 988a7580 | 2026-08-11 | Devin AI | agent trailer `Devin AI <devin@cognition.ai>` on a non-shared identity | review |
| 1d5d5e55 | 2026-08-11 | Zan Yewang | agent trailer `Devin AI <devin@cognition.ai>` on a non-shared identity | review |


## R4 — engine/config changes to re-verify (10)

| commit | date | note |
|---|---|---|
| f1c64e93 | 2026-08-22 | mod.config changed (rebuild + boot gate required) |
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
| docs/design/BALANCE_PROGRAM_PLAN.md | 64 |
| mods/cameo/weapons/weapons.yaml | 51 |
| tools/balance/gen_weapon_template.py | 40 |
| DEVELOPMENT_LOG.md | 40 |
| docs/balance/derived/redalert_soviets.json | 30 |
| docs/design/ROADMAP.md | 28 |
| docs/balance/derived/tiberiansun_forgotten.json | 28 |
| docs/balance/derived/redalert2mod_consortium.json | 27 |
| docs/balance/derived/tiberiansun_nod.json | 26 |
| docs/balance/derived/shared_redalert.json | 26 |
| docs/balance/derived/starcraft_protoss.json | 26 |
| docs/balance/derived/d2k_ordos.json | 25 |
| docs/balance/derived/redalert2mod_futuretech.json | 25 |
| docs/balance/derived/warcraft2_humans.json | 25 |
| docs/balance/derived/redalert2mod_asianalliance.json | 24 |


## Reviewer checklist (not machine-checkable)

- [ ] Every yaml change in the window boot-gated (`launch-game.cmd` reached the menu)?
- [ ] C# changes rebuilt (`dotnet build -c Release -p:TargetPlatform=win-x64`)?
- [ ] New actors/weapons named with underscores only, and Fluent keys added?
- [ ] Generated reports under `docs/audit/latest/` regenerated via run_all.sh, not hand-edited?
- [ ] ROADMAP.md updated for finished/queued work?


## Enforcement

R1/R3 block only for commits on or after **2026-08-12**: 7 R1 and 0 R3 of 10/7 findings are in scope; the rest predate the gate.


## FAIL

- 7 R1, 3 R2, 0 R3 blocking finding(s)

