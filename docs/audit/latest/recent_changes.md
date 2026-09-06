# audit_recent_changes — last 30 day(s) of history

Commits reviewed: **849**, files touched: **10883**

| code | meaning | count | blocking |
|---|---|---|---|
| R1 | balance yaml edited without the ledger | 47 | yes |
| R2 | audit script never run by run_all.sh | 4 | yes |
| R3 | provenance (wrong-identity trailer blocks; missing one on the shared identity is review-only) | 32 | partly |
| R4 | engine/mod.config change (needs boot gate) | 10 | no |


## R1 — hand-edited balance numbers (47)

| commit | date | subject | fields |
|---|---|---|---|
| 0169409d | 2026-09-06 | feat(ordos/weapons): migrate 41 Ordos-only weapo | Burst, BurstDelays, Damage, MinRange, Range, ReloadDelay, Speed, Spread |
| e5b5f85f | 2026-09-06 | feat(shared/weapons): migrate mtank_pri from leg | Burst, BurstDelays, Damage, MinRange, Range, ReloadDelay |
| de619f38 | 2026-09-06 | feat(atreides/weapons): migrate D2KRepair + HMG  | Damage, Range, ReloadDelay, Spread |
| 5d3c8a13 | 2026-09-05 | fix(D2k/Shared): move 110mm_Gun + D2K_TowerMissi | Burst, BurstDelays, Damage, MinRange, Range, ReloadDelay, Speed |
| 87622694 | 2026-09-05 | feat: port Atreides-unique weapons from legacy d | Burst, BurstDelays, Damage, MinRange, Range, ReloadDelay, Speed, Spread |
| 1858d013 | 2026-09-05 | feat: add Corrino siege tank + husk, update heav | Cost, HP, Range, Speed |
| c2b77716 | 2026-09-05 | feat: add Corrino gunship and advanced carryall | Cost, HP, Range, Speed |
| 95261bec | 2026-09-05 | W24: collapse MissileAttackRobotGun (MissileAP_L | Damage |
| cda4c54e | 2026-09-05 | fix: remove duplicate inherits and restore merge | Burst, BurstDelays, Damage, Range, ReloadDelay, Speed, Spread |
| 9f7d2c09 | 2026-09-02 | Polish projectile streaks and defensive fire | Speed |
| d83ed80e | 2026-08-29 | Remove remaining sniper splash and strengthen we | Spread |
| 7de94587 | 2026-08-29 | Repair paid weapon upgrade contracts (#310) | Damage, Range, ReloadDelay |
| 58a3e2d7 | 2026-08-29 | Restore real bullet projectile speeds (#305) | Speed |
| 5a8669b7 | 2026-08-26 | feat(weapons): W24 collapse 4 same-family weapon | Damage |
| 05d70935 | 2026-08-26 | feat(weapons): W24 collapse StarCraft/Zerg Infes | Damage |
| 40f74a47 | 2026-08-25 | feat(weapons): W24 collapse 12 same-family weapo | Damage |
| 9ebd13c2 | 2026-08-25 | feat(weapons): W24 collapse 3 StarCraft/Terran s | Damage |
| 0bd58c2a | 2026-08-25 | feat(weapons): W24 collapse 3 Consortium Bullet_ | Damage |
| eb5da8d3 | 2026-08-25 | feat(weapons): add generic Mortar/MortarFire/Mor | Damage, MinRange, Range, ReloadDelay |
| 94cd582b | 2026-08-25 | D2k Phase 4: Atreides/Harkonnen/Corrino expansio | BuildDuration, Cost, Damage, HP, Range, ReloadDelay, Speed |
| 3498a54e | 2026-08-25 | D2k Corrino APC + trooper + Ordos sequence fixes | Cost, HP, Speed |
| b5e439cb | 2026-08-25 | W24 TiberianDawn/GDI bullet collapses + Ordos bu | Damage |
| d519ceaf | 2026-08-25 | D2k Corrino: corrino_cannon weapon, heavyfactory | ReloadDelay |
| af3ff5f9 | 2026-08-25 | D2k Corrino pack expansion: infantry, aircraft,  | Cost, HP, Range, Speed |
| 07135e6f | 2026-08-25 | D2k Corrino pack expansion: buggy vehicle + miss | Cost, Damage, HP, Range, ReloadDelay, Speed |
| afdaae46 | 2026-08-25 | D2k Harkonnen pack completion: infantry, aircraf | Cost, HP, Range, Speed |
| f07d8d35 | 2026-08-25 | D2k faction rollout: Atreides completion + Corri | BuildDuration, Cost, HP, Range, ReloadDelay, Speed |
| d11b9072 | 2026-08-25 | feat(warcraft2): port 4 hero weapon pairs from w | Cost, Damage, HP, Range, ReloadDelay, Speed |
| 5a74091b | 2026-08-25 | W24 A12: collapse ATMine (RedAlert Shared) onto  | Damage |
| 71ea9200 | 2026-08-23 | separate AI chrono from chrono reinforcements an | Range, ReloadDelay |
| c49a8f20 | 2026-08-23 | Add drop pods to the dropship bay and rework scr | Range, Speed |
| 924e4f68 | 2026-08-23 | Add the dropship bay | BuildDuration, Cost, HP, Speed |
| 47a66b6c | 2026-08-21 | fix(w24): the nuclear batch collapsed 15 warhead | Damage |
| 33959758 | 2026-08-21 | HeatRayBeam1-4: complete Inferno 3-way split + s | Range, ReloadDelay |
| 89c94c89 | 2026-08-20 | D2K: 3-way split OrniBomb and OrniBombC | Range |
| 86634636 | 2026-08-20 | W24: D2K ^ORocket/^OMissile 3-way split | MinRange |
| bd215785 | 2026-08-18 | feat(weapons): convert legacy flame/chemical App | Damage |
| b010cc6e | 2026-08-18 | Hover Transport Added | Cost, HP, Range, Speed |
| 786bb2f2 | 2026-08-18 | Added all Combat ships for GDI/Nod | Burst, BurstDelays, Cost, Damage, HP, Range, ReloadDelay, Speed |
| a20cda71 | 2026-08-12 | W2: convert wc2deathknightDeathAndDecay_Hit to I | Damage |
| 086efefc | 2026-08-11 | feat(balance): convert HonestJohn to 3-way split | Damage |
| 14713d57 | 2026-08-11 | fix(tesla): rename extra-damage chips and restor | Damage |
| 0d2cd6e8 | 2026-08-10 | feat(warhead): auto-scaling Integrity/EMP + unif | Damage, Spread |
| 39995bba | 2026-08-10 | balance(weapons): wire D2K_StormGunInf/Cymek to  | Damage |
| 4e9c3198 | 2026-08-10 | balance(weapons): collapse Exorcist family + Shr | Damage |
| fefb19f6 | 2026-08-10 | Improve bullet casing ejection (#249) | Speed |
| ea160f40 | 2026-08-10 | Restore autogun projectile visuals (#248) | Speed |


## R2 — audits missing from run_all.sh (4)

| script | problem |
|---|---|
| tools/audit/audit_inline_effects.py | not invoked by run_all.sh |
| tools/audit/audit_scaled_bullet_overrides.py | not invoked by run_all.sh |
| tools/audit/audit_upgrade_regression.py | not invoked by run_all.sh |
| tools/audit/audit_weapon_identity.py | not invoked by run_all.sh |


## R3 — commits without provenance (32)

| commit | date | author | problem | severity |
|---|---|---|---|---|
| 979d172c | 2026-09-05 | AedisToru | no Co-Authored-By trailer (shared identity) | review |
| c6313f50 | 2026-09-05 | AedisToru | no Co-Authored-By trailer (shared identity) | review |
| 3256bb36 | 2026-08-31 | Devin AI | agent trailer `Devin AI <devin@cognition.ai>` on a non-shared identity | review |
| d3f188d0 | 2026-08-31 | Devin AI | agent trailer `Devin AI <devin@cognition.ai>` on a non-shared identity | review |
| c91de468 | 2026-08-31 | Devin AI | agent trailer `Devin AI <devin@cognition.ai>` on a non-shared identity | review |
| e2ed9716 | 2026-08-28 | Claude | agent trailer `Claude Opus 5 <noreply@anthropic.com>` on a non-shared identity | review |
| 485dfc9a | 2026-08-28 | Claude | agent trailer `Claude Opus 5 <noreply@anthropic.com>` on a non-shared identity | review |
| 1173d0bf | 2026-08-28 | Claude | agent trailer `Claude Opus 5 <noreply@anthropic.com>` on a non-shared identity | review |
| 7033824c | 2026-08-28 | Claude | agent trailer `Claude Opus 5 <noreply@anthropic.com>` on a non-shared identity | review |
| 018e7fe6 | 2026-08-28 | Claude | agent trailer `Claude Opus 5 <noreply@anthropic.com>` on a non-shared identity | review |
| c4c6744c | 2026-08-28 | Claude | agent trailer `Claude Opus 5 <noreply@anthropic.com>` on a non-shared identity | review |
| 1a00da5f | 2026-08-28 | Claude | agent trailer `Claude Opus 5 <noreply@anthropic.com>` on a non-shared identity | review |
| a3aaa7ec | 2026-08-28 | Claude | agent trailer `Claude Opus 5 <noreply@anthropic.com>` on a non-shared identity | review |
| 5d807dc6 | 2026-08-26 | AedisToru | no Co-Authored-By trailer (shared identity) | review |
| c71bde9d | 2026-08-25 | AedisToru | no Co-Authored-By trailer (shared identity) | review |
| ec2457c0 | 2026-08-25 | AedisToru | no Co-Authored-By trailer (shared identity) | review |
| ccace5a5 | 2026-08-25 | AedisToru | no Co-Authored-By trailer (shared identity) | review |
| 36ee102c | 2026-08-24 | Devin AI | agent trailer `Devin AI <devin@cognition.ai>` on a non-shared identity | review |
| 75238eb3 | 2026-08-24 | Devin AI | agent trailer `Devin AI <devin@cognition.ai>` on a non-shared identity | review |
| 5f0f2828 | 2026-08-24 | Devin AI | agent trailer `Devin AI <devin@cognition.ai>` on a non-shared identity | review |
| 1d18d5d4 | 2026-08-23 | Claude | agent trailer `Claude Opus 5 <noreply@anthropic.com>` on a non-shared identity | review |
| 4ec4fd1c | 2026-08-23 | Claude | agent trailer `Claude Opus 5 <noreply@anthropic.com>` on a non-shared identity | review |
| 20f15194 | 2026-08-23 | Claude | agent trailer `Claude Opus 5 <noreply@anthropic.com>` on a non-shared identity | review |
| 026963fd | 2026-08-23 | Claude | agent trailer `Claude Opus 5 <noreply@anthropic.com>` on a non-shared identity | review |
| 2bb046ae | 2026-08-20 | Zan Yewang | agent trailer `Devin AI <devin@cognition.ai>` on a non-shared identity | review |
| 7800eaab | 2026-08-17 | Zan Yewang | agent trailer `Devin AI <devin@cognition.ai>` on a non-shared identity | review |
| 519105d4 | 2026-08-16 | Zan Yewang | agent trailer `Devin AI <devin@cognition.ai>` on a non-shared identity | review |
| e62ac4ea | 2026-08-16 | Zan Yewang | agent trailer `Devin AI <devin@cognition.ai>` on a non-shared identity | review |
| 988a7580 | 2026-08-11 | Devin AI | agent trailer `Devin AI <devin@cognition.ai>` on a non-shared identity | review |
| 1d5d5e55 | 2026-08-11 | Zan Yewang | agent trailer `Devin AI <devin@cognition.ai>` on a non-shared identity | review |
| 7155a0f1 | 2026-08-11 | AedisToru | no Co-Authored-By trailer (shared identity) | review |
| 59ade89e | 2026-08-11 | AedisToru | no Co-Authored-By trailer (shared identity) | review |


## R4 — engine/config changes to re-verify (10)

| commit | date | note |
|---|---|---|
| f1c64e93 | 2026-08-22 | mod.config changed (rebuild + boot gate required) |
| fd58e3f9 | 2026-08-20 | mod.config changed (rebuild + boot gate required) |
| c69604be | 2026-08-17 | mod.config changed (rebuild + boot gate required) |
| a74638de | 2026-08-16 | mod.config changed (rebuild + boot gate required) |
| 41f2870b | 2026-08-16 | mod.config changed (rebuild + boot gate required) |
| d6e8712c | 2026-08-15 | mod.config changed (rebuild + boot gate required) |
| 988a7580 | 2026-08-11 | mod.config changed (rebuild + boot gate required) |
| 1d5d5e55 | 2026-08-11 | mod.config changed (rebuild + boot gate required) |
| f2284b1c | 2026-08-11 | mod.config changed (rebuild + boot gate required) |
| ea160f40 | 2026-08-10 | mod.config changed (rebuild + boot gate required) |


## R5 — most-churned files (re-read these first)

| file | commits touching it |
|---|---|
| DEVELOPMENT_LOG.md | 206 |
| docs/design/BALANCE_PROGRAM_PLAN.md | 143 |
| mods/cameo/weapons/weapons.yaml | 103 |
| docs/balance/derived/redalert_soviets.json | 80 |
| mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml | 71 |
| docs/balance/redalert_soviets.json | 71 |
| docs/design/ROADMAP.md | 70 |
| docs/HANDOFF.md | 68 |
| docs/balance/derived/d2k_ixian.json | 66 |
| docs/balance/derived/tiberiansun_forgotten.json | 66 |
| tools/balance/gen_weapon_template.py | 66 |
| docs/balance/derived/redalert2mod_futuretech.json | 63 |
| docs/balance/derived/tiberiansun_nod.json | 63 |
| docs/AI_HANDOFF_2026-08-05.md | 63 |
| docs/balance/derived/redalert2mod_consortium.json | 61 |


## Reviewer checklist (not machine-checkable)

- [ ] Every yaml change in the window boot-gated (`launch-game.cmd` reached the menu)?
- [ ] C# changes rebuilt (`dotnet build -c Release -p:TargetPlatform=win-x64`)?
- [ ] New actors/weapons named with underscores only, and Fluent keys added?
- [ ] Generated reports under `docs/audit/latest/` regenerated via run_all.sh, not hand-edited?
- [ ] ROADMAP.md updated for finished/queued work?


## Enforcement

R1/R3 block only for commits on or after **2026-08-12**: 40 R1 and 0 R3 of 47/32 findings are in scope; the rest predate the gate.


## FAIL

- 40 R1, 4 R2, 0 R3 blocking finding(s)

