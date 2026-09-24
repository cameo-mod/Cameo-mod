# audit_recent_changes — last 14 day(s) of history

Commits reviewed: **199**, files touched: **2010**

| code | meaning | count | blocking |
|---|---|---|---|
| R1 | balance yaml edited without the ledger | 2 | yes |
| R2 | audit script never run by run_all.sh | 9 | yes |
| R3 | provenance (wrong-identity trailer blocks; missing one on the shared identity is review-only) | 14 | partly |
| R4 | engine/mod.config change (needs boot gate) | 0 | no |


## R1 — hand-edited balance numbers (2)

| commit | date | subject | fields |
|---|---|---|---|
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


## R3 — commits without provenance (14)

| commit | date | author | problem | severity |
|---|---|---|---|---|
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
| docs/HANDOFF.md | 44 |
| docs/DESIGN.md | 43 |
| DEVELOPMENT_LOG.md | 22 |
| docs/balance/derived/redalert_japan.json | 21 |
| docs/balance/derived/tiberiandawn_nod.json | 21 |
| docs/LESSONS_LEARNED.md | 20 |
| docs/balance/derived/redalert2_allies.json | 20 |
| docs/balance/derived/redalert_allies.json | 20 |
| docs/balance/derived/redalert_soviets.json | 20 |
| docs/balance/derived/shared_redalert.json | 20 |
| docs/balance/derived/starcraft_terran.json | 20 |
| docs/balance/derived/tiberiandawn_gdi.json | 20 |
| docs/balance/derived/tiberiansun_cabal.json | 20 |
| docs/balance/derived/tiberiansun_forgotten.json | 20 |
| docs/balance/derived/tiberiansun_gdi.json | 20 |


## Reviewer checklist (not machine-checkable)

- [ ] Every yaml change in the window boot-gated (`launch-game.cmd` reached the menu)?
- [ ] C# changes rebuilt (`dotnet build -c Release -p:TargetPlatform=win-x64`)?
- [ ] New actors/weapons named with underscores only, and Fluent keys added?
- [ ] Generated reports under `docs/audit/latest/` regenerated via run_all.sh, not hand-edited?
- [ ] ROADMAP.md updated for finished/queued work?


## Enforcement

R1/R3 block only for commits on or after **2026-08-12**: 2 R1 and 0 R3 of 2/14 findings are in scope; the rest predate the gate.


## FAIL

- 2 R1, 9 R2, 0 R3 blocking finding(s)

