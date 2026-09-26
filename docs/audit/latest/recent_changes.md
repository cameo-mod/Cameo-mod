# audit_recent_changes — last 14 day(s) of history

Commits reviewed: **219**, files touched: **1698**

| code | meaning | count | blocking |
|---|---|---|---|
| R1 | balance yaml edited without the ledger | 9 | yes |
| R2 | audit script never run by run_all.sh | 9 | yes |
| R3 | provenance (wrong-identity trailer blocks; missing one on the shared identity is review-only) | 15 | partly |
| R4 | engine/mod.config change (needs boot gate) | 0 | no |


## R1 — hand-edited balance numbers (9)

| commit | date | subject | fields |
|---|---|---|---|
| c63c0415 | 2026-09-25 | W7: materialize 33 remaining DAWN-file-set weapo | Burst, BurstDelays, Damage, MinRange, Range, ReloadDelay, Speed, Spread |
| 95e18490 | 2026-09-25 | pack self-containment: eliminate hard value-ref  | Burst, BurstDelays, Damage, HP, MinRange, Range, ReloadDelay, Speed, Spread |
| 72468eb3 | 2026-09-25 | TD/TS/SC self-containment: eliminate cross-pack  | Damage, Range, ReloadDelay, Speed |
| f51320c8 | 2026-09-25 | D2k self-containment: eliminate all cross-pack i | Burst, BurstDelays, Range, ReloadDelay, Spread |
| d46ecd9d | 2026-09-24 | W7/W27/R17: DAWN lane — weapon 3-way conversions | Burst, BurstDelays, Damage, MinRange, Range, ReloadDelay, Speed, Spread |
| 86577a7a | 2026-09-24 | W7: convert unclaimed weapon-parent edges in out | Damage, Range, ReloadDelay, Speed, Spread |
| 004a9cb8 | 2026-09-24 | W7: convert all weapon-parent edges in weapons.y | Burst, BurstDelays, Damage, MinRange, Range, ReloadDelay, Speed, Spread |
| d36f3b0a | 2026-09-24 | W23-RA batch 1: TKM file retrofit (20/21 weapons | Damage, MinRange, Range, ReloadDelay, Speed, Spread |
| 20e99dbc | 2026-09-13 | test(R8): add carrier ammo runtime gate | Burst, BurstDelays, HP, MinRange, Range, ReloadDelay |


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
| docs/HANDOFF.md | 58 |
| docs/DESIGN.md | 44 |
| docs/LESSONS_LEARNED.md | 28 |
| DEVELOPMENT_LOG.md | 25 |
| docs/balance/derived/redalert2_allies.json | 21 |
| docs/balance/derived/redalert_allies.json | 21 |
| docs/balance/derived/redalert_japan.json | 21 |
| docs/balance/derived/shared_redalert.json | 21 |
| docs/balance/derived/tiberiandawn_gdi.json | 21 |
| docs/balance/derived/tiberiandawn_nod.json | 21 |
| docs/balance/derived/redalert2mod_futuretech.json | 20 |
| docs/balance/derived/redalert_soviets.json | 20 |
| docs/balance/derived/starcraft_terran.json | 20 |
| docs/balance/derived/tiberiansun_cabal.json | 20 |
| docs/balance/derived/tiberiansun_forgotten.json | 20 |


## Reviewer checklist (not machine-checkable)

- [ ] Every yaml change in the window boot-gated (`launch-game.cmd` reached the menu)?
- [ ] C# changes rebuilt (`dotnet build -c Release -p:TargetPlatform=win-x64`)?
- [ ] New actors/weapons named with underscores only, and Fluent keys added?
- [ ] Generated reports under `docs/audit/latest/` regenerated via run_all.sh, not hand-edited?
- [ ] ROADMAP.md updated for finished/queued work?


## Enforcement

R1/R3 block only for commits on or after **2026-08-12**: 9 R1 and 0 R3 of 9/15 findings are in scope; the rest predate the gate.


## FAIL

- 9 R1, 9 R2, 0 R3 blocking finding(s)

