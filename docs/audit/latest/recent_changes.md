# audit_recent_changes — last 14 day(s) of history

Commits reviewed: **232**, files touched: **2218**

| code | meaning | count | blocking |
|---|---|---|---|
| R1 | balance yaml edited without the ledger | 17 | yes |
| R2 | audit script never run by run_all.sh | 9 | yes |
| R3 | provenance (wrong-identity trailer blocks; missing one on the shared identity is review-only) | 15 | partly |
| R4 | engine/mod.config change (needs boot gate) | 0 | no |


## R1 — hand-edited balance numbers (17)

| commit | date | subject | fields |
|---|---|---|---|
| ad558d1e | 2026-09-24 | Restore 3 drain-minified D2k weapon blocks to li | Burst, BurstDelays, Damage, MinRange, Range, ReloadDelay, Speed, Spread |
| 0a3dead3 | 2026-09-24 | W7 batch-5: inline 3 no-covering edges (807->804 | Burst, BurstDelays, Damage, Range, ReloadDelay, Speed, Spread |
| 566f50b5 | 2026-09-24 | W7 batch-4: mechanized conversion, 59 edges conv | Burst, BurstDelays, Damage, Range, ReloadDelay, Speed, Spread |
| 0814a94f | 2026-09-24 | R17 fold batch-4: 18 pack weapons, 24 chips fold | Damage |
| 1e0b199e | 2026-09-24 | W7 batch-3 + R17 folds: 12 ExtraDamage folds, 3  | Damage, Range |
| 65506d91 | 2026-09-24 | W27 batch-7: outpost2.yaml -> effects_op2.yaml ( | Range |
| a31b1d33 | 2026-09-24 | W7 batch-2: sc_zerg_devourer_acidcloud_aa -> ^Wa | Damage, ReloadDelay, Spread |
| 0a1b4801 | 2026-09-24 | W7 batch-1: convert 17 ratchet-neutral weapon->w | Burst, BurstDelays, Damage, Range, ReloadDelay, Speed, Spread |
| f6279b71 | 2026-09-24 | w27 batch-6: legacy d2k/tiberiandawn/tiberiansun | Range |
| 8f7c7fff | 2026-09-24 | w27 batch-5: tiberiansun packs -> effects_ts.yam | Range |
| a7aabc72 | 2026-09-24 | w27 batch-4: tiberiandawn packs -> effects_td.ya | Range |
| 749d8172 | 2026-09-24 | w27 batch-3: all six d2k pack files -> effects_d | Range |
| 86577a7a | 2026-09-24 | W7: convert unclaimed weapon-parent edges in out | Damage, Range, ReloadDelay, Speed, Spread |
| 004a9cb8 | 2026-09-24 | W7: convert all weapon-parent edges in weapons.y | Burst, BurstDelays, Damage, MinRange, Range, ReloadDelay, Speed, Spread |
| d36f3b0a | 2026-09-24 | W23-RA batch 1: TKM file retrofit (20/21 weapons | Damage, MinRange, Range, ReloadDelay, Speed, Spread |
| 20e99dbc | 2026-09-13 | test(R8): add carrier ammo runtime gate | Burst, BurstDelays, HP, MinRange, Range, ReloadDelay |
| b8c44c2c | 2026-09-10 | Added Neutral Map stuff | Damage, HP |


## R2 — audits missing from run_all.sh (9)

| script | problem |
|---|---|
| tools/audit/audit_bot_insurance.py | not invoked by run_all.sh |
| tools/audit/audit_chrome_master_freshness.py | not invoked by run_all.sh |
| tools/audit/audit_chrome_scale_variants.py | not invoked by run_all.sh |
| tools/audit/audit_inline_effects.py | not invoked by run_all.sh |
| tools/audit/audit_orphan_removals.py | not invoked by run_all.sh |
| tools/audit/audit_promotion_superiority.py | not invoked by run_all.sh |
| tools/audit/audit_scaled_bullet_overrides.py | not invoked by run_all.sh |
| tools/audit/audit_upgrade_regression.py | not invoked by run_all.sh |
| tools/audit/audit_weapon_identity.py | not invoked by run_all.sh |


## R3 — commits without provenance (15)

| commit | date | author | problem | severity |
|---|---|---|---|---|
| 043e6c40 | 2026-09-24 | AedisToru | no Co-Authored-By trailer (shared identity) | review |
| 8f53b8dd | 2026-09-23 | devin-ai-integration[bot] | agent trailer `Zan Yewang <inyucedora@gmail.com>` on a non-shared identity | review |
| c6894f19 | 2026-09-23 | devin-ai-integration[bot] | agent trailer `Zan Yewang <inyucedora@gmail.com>` on a non-shared identity | review |
| d81a1bfd | 2026-09-23 | Blackrobe | agent trailer `Codex GPT-5.6 Luna <noreply@openai.com>` on a non-shared identity | review |
| 1519a758 | 2026-09-22 | devin-ai-integration[bot] | agent trailer `Devin AI <devin@cognition.ai>` on a non-shared identity | review |
| fdbb58ef | 2026-09-22 | devin-ai-integration[bot] | agent trailer `Devin AI <devin@cognition.ai>` on a non-shared identity | review |
| 4a1139b3 | 2026-09-16 | Blackrobe | agent trailer `DeepSeek Flash <noreply@deepseek.com>` on a non-shared identity | review |
| 03049aad | 2026-09-16 | Blackrobe | agent trailer `DeepSeek Flash <noreply@deepseek.com>` on a non-shared identity | review |
| ce93267b | 2026-09-13 | Zan Yewang | agent trailer `Devin AI <devin@cognition.ai>` on a non-shared identity | review |
| ab498104 | 2026-09-13 | Zan Yewang | agent trailer `Devin AI <devin@cognition.ai>` on a non-shared identity | review |
| 1b12b7fd | 2026-09-13 | Zan Yewang | agent trailer `Devin AI <devin@cognition.ai>` on a non-shared identity | review |
| 96fc961b | 2026-09-13 | Zan Yewang | agent trailer `Devin AI <devin@cognition.ai>` on a non-shared identity | review |
| 0c164ff8 | 2026-09-13 | Zan Yewang | agent trailer `Devin AI <devin@cognition.ai>` on a non-shared identity | review |
| f2dd0fe1 | 2026-09-13 | Zan Yewang | agent trailer `Devin AI <158243242+devin-ai-integration[bot]@users.noreply.github.com>` on a non-shared identity | review |
| 0f908e7a | 2026-09-12 | AedisToru | no Co-Authored-By trailer (shared identity) | review |


## R4 — engine/config changes to re-verify (0)

_none found_


## R5 — most-churned files (re-read these first)

| file | commits touching it |
|---|---|
| docs/HANDOFF.md | 54 |
| docs/DESIGN.md | 45 |
| DEVELOPMENT_LOG.md | 36 |
| docs/LESSONS_LEARNED.md | 29 |
| docs/balance/derived/redalert2_allies.json | 21 |
| docs/balance/derived/redalert_allies.json | 21 |
| docs/balance/derived/shared_redalert.json | 21 |
| docs/balance/derived/tiberiandawn_gdi.json | 21 |
| docs/balance/derived/redalert_japan.json | 21 |
| docs/balance/derived/tiberiandawn_nod.json | 21 |
| tools/audit/audit_weapon_shape.py | 20 |
| docs/balance/derived/redalert2mod_futuretech.json | 20 |
| docs/balance/derived/redalert_soviets.json | 20 |
| docs/balance/derived/starcraft_terran.json | 20 |
| docs/balance/derived/tiberiansun_cabal.json | 20 |


## Reviewer checklist (not machine-checkable)

- [ ] Every yaml change in the window boot-gated (`launch-game.cmd` reached the menu)?
- [ ] C# changes rebuilt (`dotnet build -c Release -p:TargetPlatform=win-x64`)?
- [ ] New actors/weapons named with underscores only, and Fluent keys added?
- [ ] Generated reports under `docs/audit/latest/` regenerated via run_all.sh, not hand-edited?
- [ ] ROADMAP.md updated for finished/queued work?


## Enforcement

R1/R3 block only for commits on or after **2026-08-12**: 17 R1 and 0 R3 of 17/15 findings are in scope; the rest predate the gate.


## FAIL

- 17 R1, 9 R2, 0 R3 blocking finding(s)

