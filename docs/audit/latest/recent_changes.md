# audit_recent_changes — last 14 day(s) of history

Commits reviewed: **919**, files touched: **4162**

| code | meaning | count | blocking |
|---|---|---|---|
| R1 | balance yaml edited without the ledger | 18 | yes |
| R2 | audit script never run by run_all.sh | 11 | yes |
| R3 | provenance (wrong-identity trailer blocks; missing one on the shared identity is review-only) | 69 | partly |
| R4 | engine/mod.config change (needs boot gate) | 13 | no |


## R1 — hand-edited balance numbers (18)

| commit | date | subject | fields |
|---|---|---|---|
| 230cdf71 | 2026-10-08 | test(balance): re-record history fixtures agains | Spread |
| 4e47ead9 | 2026-10-06 | Rebase air-only missiles on AA templates | Damage, Speed, Spread |
| d300295e | 2026-10-01 | packs: materialize 12 hard cross-pack refs (reso | Damage, Range, ReloadDelay, Speed, Spread |
| 2f440999 | 2026-09-26 | W7 tail-2: 5 more de-parented after collision pr | Burst, BurstDelays, Damage, Range, ReloadDelay, Speed, Spread |
| 5eff1183 | 2026-09-26 | rule-4 Versus remediation: restore weapon-parent | BurstDelays, MinRange |
| 01c89a5a | 2026-09-26 | fix(W7): restore Inherits edges where splices ma | Damage |
| 8e553991 | 2026-09-26 | feat(W7): chain-collapse batch-3 — 57 children o | Burst, BurstDelays, Damage, MinRange, Range, ReloadDelay, Speed, Spread |
| 9ec11477 | 2026-09-26 | feat(W7): collapse 52 more weapon->weapon edges  | Damage, MinRange, Range, ReloadDelay, Speed, Spread |
| e4000127 | 2026-09-26 | feat(W7): collapse 55 weapon->weapon edges onto  | BurstDelays, Damage, MinRange, Range, ReloadDelay, Speed, Spread |
| 1ae9c7d0 | 2026-09-26 | R17 chip folds (W5 batch-1): 20 weapons / 21 chi | Damage |
| 2c0cf719 | 2026-09-26 | W8 batch-2: 13 more legacy edges -> covering edg | Burst, BurstDelays, Damage, MinRange, Range, ReloadDelay, Speed |
| 2ba249f0 | 2026-09-26 | feat(W7): convert YuriGatlingTankMG{1,2,3}[_AA]  | ReloadDelay, Speed |
| bc676e8d | 2026-09-26 | W8 batch-1: 52 legacy-template edges -> covering | Damage, MinRange, Range, ReloadDelay, Speed, Spread |
| 90aef52d | 2026-09-26 | W7: replace 19 concrete-weapon inherits with cov | Damage, Range, ReloadDelay, Speed, Spread |
| 6b0f3fdb | 2026-09-25 | W2 sweep: drop 152 dead ^Warhead_ edges (-77), 4 | Range, ReloadDelay |
| 450dcea5 | 2026-09-26 | feat(W7): materialize held-67 ExtraDamage batch  | BurstDelays, Damage, Range, ReloadDelay, Speed, Spread |
| 434413f0 | 2026-09-26 | W7 packs: restore resolved child-order parity (N | Damage, Range, Spread |
| 5407fd8a | 2026-09-25 | W7 ContentPack batch: 97 pack-level weapon-paren | Burst, BurstDelays, Damage, MinRange, Range, ReloadDelay, Speed, Spread |


## R2 — audits missing from run_all.sh (11)

| script | problem |
|---|---|
| tools/audit/audit_bot_insurance.py | not invoked by run_all.sh |
| tools/audit/audit_bot_wiring.py | not invoked by run_all.sh |
| tools/audit/audit_chrome_master_freshness.py | not invoked by run_all.sh |
| tools/audit/audit_chrome_scale_variants.py | not invoked by run_all.sh |
| tools/audit/audit_inline_effects.py | not invoked by run_all.sh |
| tools/audit/audit_multi_traitinfo_scan.py | not invoked by run_all.sh |
| tools/audit/audit_orphan_removals.py | not invoked by run_all.sh |
| tools/audit/audit_promotion_superiority.py | not invoked by run_all.sh |
| tools/audit/audit_scaled_bullet_overrides.py | not invoked by run_all.sh |
| tools/audit/audit_upgrade_regression.py | not invoked by run_all.sh |
| tools/audit/audit_weapon_identity.py | not invoked by run_all.sh |


## R3 — commits without provenance (69)

| commit | date | author | problem | severity |
|---|---|---|---|---|
| 680dd25d | 2026-10-09 | Claude | agent trailer `Claude Opus 5.5 <noreply@anthropic.com>` on a non-shared identity | review |
| ba8e99ac | 2026-10-09 | Claude | agent trailer `Claude Opus 5.5 <noreply@anthropic.com>` on a non-shared identity | review |
| f70c37ba | 2026-10-09 | Claude | agent trailer `Claude Opus 5.5 <noreply@anthropic.com>` on a non-shared identity | review |
| 11490f9e | 2026-10-09 | Claude | agent trailer `Claude Opus 5.5 <noreply@anthropic.com>` on a non-shared identity | review |
| f185a733 | 2026-10-09 | Claude | agent trailer `Claude Opus 5.5 <noreply@anthropic.com>` on a non-shared identity | review |
| 5041e038 | 2026-10-09 | Claude | agent trailer `Claude Opus 5.5 <noreply@anthropic.com>` on a non-shared identity | review |
| f5cf0b34 | 2026-10-09 | Claude | agent trailer `Claude Opus 5.5 <noreply@anthropic.com>` on a non-shared identity | review |
| 5621960d | 2026-10-09 | Claude | agent trailer `Claude Opus 5.5 <noreply@anthropic.com>` on a non-shared identity | review |
| 5c8cfe04 | 2026-10-09 | AedisToru | no Co-Authored-By trailer (shared identity) | review |
| 3d875fd2 | 2026-10-09 | Claude | agent trailer `Claude Opus 5.5 <noreply@anthropic.com>` on a non-shared identity | review |
| 2a417dc3 | 2026-10-09 | AedisToru | no Co-Authored-By trailer (shared identity) | review |
| cdb75da4 | 2026-10-09 | AedisToru | no Co-Authored-By trailer (shared identity) | review |
| e9814217 | 2026-10-09 | AedisToru | no Co-Authored-By trailer (shared identity) | review |
| b5425946 | 2026-10-09 | AedisToru | no Co-Authored-By trailer (shared identity) | review |
| a12d9661 | 2026-10-09 | AedisToru | no Co-Authored-By trailer (shared identity) | review |
| 83f14ab1 | 2026-10-09 | AedisToru | no Co-Authored-By trailer (shared identity) | review |
| d7494099 | 2026-10-09 | AedisToru | no Co-Authored-By trailer (shared identity) | review |
| ab7285e8 | 2026-10-09 | AedisToru | no Co-Authored-By trailer (shared identity) | review |
| 20a8107a | 2026-10-08 | AedisToru | no Co-Authored-By trailer (shared identity) | review |
| 3d99405b | 2026-10-08 | AedisToru | no Co-Authored-By trailer (shared identity) | review |
| 4bf69671 | 2026-10-08 | AedisToru | no Co-Authored-By trailer (shared identity) | review |
| 37d9fc6a | 2026-10-08 | AedisToru | no Co-Authored-By trailer (shared identity) | review |
| 65367213 | 2026-10-07 | AedisToru | no Co-Authored-By trailer (shared identity) | review |
| 29383802 | 2026-10-08 | AedisToru | no Co-Authored-By trailer (shared identity) | review |
| d8ccd7da | 2026-10-07 | AedisToru | no Co-Authored-By trailer (shared identity) | review |
| fa59898f | 2026-10-07 | AedisToru | no Co-Authored-By trailer (shared identity) | review |
| a716cb6d | 2026-10-06 | AedisToru | no Co-Authored-By trailer (shared identity) | review |
| 1832a4c6 | 2026-10-06 | AedisToru | no Co-Authored-By trailer (shared identity) | review |
| 210ec947 | 2026-10-06 | AedisToru | no Co-Authored-By trailer (shared identity) | review |
| 128b02a1 | 2026-10-06 | AedisToru | no Co-Authored-By trailer (shared identity) | review |
| 4e47ead9 | 2026-10-06 | AedisToru | no Co-Authored-By trailer (shared identity) | review |
| c061cfbb | 2026-10-05 | AedisToru | no Co-Authored-By trailer (shared identity) | review |
| 5fecf671 | 2026-10-06 | AedisToru | no Co-Authored-By trailer (shared identity) | review |
| 444e855e | 2026-10-06 | AedisToru | no Co-Authored-By trailer (shared identity) | review |
| b7f6d436 | 2026-10-06 | AedisToru | no Co-Authored-By trailer (shared identity) | review |
| 0fd6ec67 | 2026-10-05 | AedisToru | no Co-Authored-By trailer (shared identity) | review |
| b6f522e7 | 2026-10-05 | AedisToru | no Co-Authored-By trailer (shared identity) | review |
| 2e944a9e | 2026-10-05 | AedisToru | no Co-Authored-By trailer (shared identity) | review |
| 772b8de5 | 2026-10-04 | AedisToru | no Co-Authored-By trailer (shared identity) | review |
| 0ddcb6b0 | 2026-10-04 | AedisToru | no Co-Authored-By trailer (shared identity) | review |
| 08d28859 | 2026-10-04 | AedisToru | no Co-Authored-By trailer (shared identity) | review |
| 6fd362b6 | 2026-10-04 | AedisToru | no Co-Authored-By trailer (shared identity) | review |
| 8fd71f6b | 2026-10-03 | AedisToru | no Co-Authored-By trailer (shared identity) | review |
| a382470b | 2026-10-03 | AedisToru | no Co-Authored-By trailer (shared identity) | review |
| 612014a7 | 2026-10-03 | AedisToru | no Co-Authored-By trailer (shared identity) | review |
| 509db870 | 2026-10-03 | AedisToru | no Co-Authored-By trailer (shared identity) | review |
| 8a86238b | 2026-10-01 | AedisToru | no Co-Authored-By trailer (shared identity) | review |
| 27de8684 | 2026-10-01 | AedisToru | no Co-Authored-By trailer (shared identity) | review |
| be34aa6d | 2026-10-01 | AedisToru | no Co-Authored-By trailer (shared identity) | review |
| 3e889dc5 | 2026-09-30 | AedisToru | no Co-Authored-By trailer (shared identity) | review |
| a47642cf | 2026-09-30 | AedisToru | no Co-Authored-By trailer (shared identity) | review |
| 2e52abaf | 2026-09-30 | AedisToru | no Co-Authored-By trailer (shared identity) | review |
| d891992d | 2026-09-28 | AedisToru | no Co-Authored-By trailer (shared identity) | review |
| 2a7e2e29 | 2026-09-28 | AedisToru | no Co-Authored-By trailer (shared identity) | review |
| cefceacd | 2026-09-28 | AedisToru | no Co-Authored-By trailer (shared identity) | review |
| 624348f6 | 2026-09-29 | AedisToru | no Co-Authored-By trailer (shared identity) | review |
| 7b0cd5d7 | 2026-09-28 | AedisToru | no Co-Authored-By trailer (shared identity) | review |
| 5b526e79 | 2026-09-28 | Zan Yewang | agent trailer `Devin AI <158243242+devin-ai-integration[bot]@users.noreply.github.com>` on a non-shared identity | review |
| 86b463d6 | 2026-09-28 | Zan Yewang | agent trailer `Devin AI <158243242+devin-ai-integration[bot]@users.noreply.github.com>` on a non-shared identity | review |
| 58005b41 | 2026-09-28 | Zan Yewang | agent trailer `Devin AI <158243242+devin-ai-integration[bot]@users.noreply.github.com>` on a non-shared identity | review |
| b862072c | 2026-09-28 | Zan Yewang | agent trailer `Devin AI <158243242+devin-ai-integration[bot]@users.noreply.github.com>` on a non-shared identity | review |
| 8f1e5579 | 2026-09-28 | Zan Yewang | agent trailer `Devin AI <158243242+devin-ai-integration[bot]@users.noreply.github.com>` on a non-shared identity | review |
| c6c990b7 | 2026-09-28 | Zan Yewang | agent trailer `Devin AI <158243242+devin-ai-integration[bot]@users.noreply.github.com>` on a non-shared identity | review |
| b92183f3 | 2026-09-28 | Blackrobe | agent trailer `OpenAI GPT-6 Astra <noreply@openai.com>` on a non-shared identity | review |
| cfafbbf0 | 2026-09-28 | Blackrobe | agent trailer `OpenAI GPT-6 Astra <noreply@openai.com>` on a non-shared identity | review |
| 045d0c37 | 2026-09-27 | Zan Yewang | agent trailer `Devin AI <devin@cognition.ai>` on a non-shared identity | review |
| 67b18009 | 2026-09-27 | Zan Yewang | agent trailer `Devin AI <devin@cognition.ai>` on a non-shared identity | review |
| e9d50021 | 2026-09-27 | Zan Yewang | agent trailer `Devin AI <158243242+devin-ai-integration[bot]@users.noreply.github.com>` on a non-shared identity | review |
| 56de7db0 | 2026-09-27 | Zan Yewang | agent trailer `Devin AI <158243242+devin-ai-integration[bot]@users.noreply.github.com>` on a non-shared identity | review |


## R4 — engine/config changes to re-verify (13)

| commit | date | note |
|---|---|---|
| 7743fc17 | 2026-10-10 | mod.config changed (rebuild + boot gate required) |
| b06615a8 | 2026-10-09 | mod.config changed (rebuild + boot gate required) |
| a9349d01 | 2026-10-08 | mod.config changed (rebuild + boot gate required) |
| d8ccd7da | 2026-10-07 | mod.config changed (rebuild + boot gate required) |
| fa59898f | 2026-10-07 | mod.config changed (rebuild + boot gate required) |
| 210ec947 | 2026-10-06 | mod.config changed (rebuild + boot gate required) |
| 444e855e | 2026-10-06 | mod.config changed (rebuild + boot gate required) |
| b7f6d436 | 2026-10-06 | mod.config changed (rebuild + boot gate required) |
| 11f1825c | 2026-09-29 | mod.config changed (rebuild + boot gate required) |
| 5441ace1 | 2026-09-29 | mod.config changed (rebuild + boot gate required) |
| 5b526e79 | 2026-09-28 | mod.config changed (rebuild + boot gate required) |
| 86b463d6 | 2026-09-28 | mod.config changed (rebuild + boot gate required) |
| 7489f8e9 | 2026-09-28 | mod.config changed (rebuild + boot gate required) |


## R5 — most-churned files (re-read these first)

| file | commits touching it |
|---|---|
| DEVELOPMENT_LOG.md | 361 |
| docs/design/AI_ARCHITECTURE.md | 160 |
| mods/cameo/ai/ai.yaml | 135 |
| docs/HANDOFF.md | 120 |
| OpenRA.Mods.CA/Traits/BotModules/SquadManagerBotModuleCA.cs | 96 |
| tools/ai/increment_switches.yaml | 73 |
| OpenRA.Mods.Cameo/Traits/BotModules/BotSituation.cs | 67 |
| docs/DESIGN.md | 61 |
| docs/LESSONS_LEARNED.md | 56 |
| docs/design/AI_MODULE_MAP.md | 48 |
| OpenRA.Mods.CA/Traits/BotModules/BotModuleLogic/BaseBuilderQueueManagerCA.cs | 43 |
| docs/design/ROADMAP.md | 43 |
| tools/audit/fog_honesty_manifest.json | 39 |
| OpenRA.Mods.CA/Traits/BotModules/Squads/States/GroundStatesCA.cs | 35 |
| docs/design/AI_MATCH_LOG.md | 35 |


## Reviewer checklist (not machine-checkable)

- [ ] Every yaml change in the window boot-gated (`launch-game.cmd` reached the menu)?
- [ ] C# changes rebuilt (`dotnet build -c Release -p:TargetPlatform=win-x64`)?
- [ ] New actors/weapons named with underscores only, and Fluent keys added?
- [ ] Generated reports under `docs/audit/latest/` regenerated via run_all.sh, not hand-edited?
- [ ] ROADMAP.md updated for finished/queued work?


## Enforcement

R1/R3 block only for commits on or after **2026-08-12**: 18 R1 and 0 R3 of 18/69 findings are in scope; the rest predate the gate.


## FAIL

- 18 R1, 11 R2, 0 R3 blocking finding(s)

