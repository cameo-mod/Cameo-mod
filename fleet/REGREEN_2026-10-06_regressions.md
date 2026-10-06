# TOOLS-REGREEN — real-regression findings (2026-10-06)

Branch: `devin/tier4/tools-regreen` (worktree `C:\cameo-wt\t4-regreen`), base `origin/inc/2026_10_05` @ `b6f522e78`.
Owner: Devin-Reviewer (`01a1027f-147c-7b13-afeb-bdd16fd7b03c`).

Convention per lead order: every confirmed real regression is filed here with file/line +
expected vs actual. Nothing in this file is repinned. Stale-baseline repairs land in commits
on the branch with provenance citations instead.

---

## R1 — Materialized `Warhead@Missile*` local ladders frozen at pre-R16 values (27 weapons)

**Class:** data regression — needs maintainer ruling or a follow-up retune/migration pass.
**Suite:** `tools/tests/test_missile_role_policy.py::test_role_profiles_preserve_payload_and_all_other_behavior`
(27 subtests still failing after the 4 stale-fixture repairs; contract at
`test_missile_role_policy.py:184-187`).

**Expected:** the live main warhead's `Versus`/`PercentageVersus`/`Spread`/`Falloff` equals
either the recorded reviewed override (same-family case) or the current `^Warhead_<family>`
template profile (role-change case). R16 (`8e20af41e`, maintainer ruling: every Versus table
geometric-mean 100) normalized all `^Warhead_*` templates.

**Actual:** each weapon below carries a *local* `Warhead@` block (authored by the 3-inherit
missile retrofit `ab945ea31`, 2026-08-03, which copied then-current template bodies into the
weapon). `8e20af41e` retuned only `^Warhead_*` template definitions in
`mods/cameo/weapons/weapons.yaml`; `audit_versus_profile` scans only template families
(210 mains), so the materialized copies were never re-normalized. `0382fc032` (armor 12.0l)
later added the new armor columns to these tables, leaving pre-R16 *values* beside
post-12.0l *columns*. Result: resolved Versus geomean 82–95 vs the family's normalized
94–100 — the exact arithmetic-mean inflation R16 was ruled to eliminate.

**Evidence table** (local block location; `gm` = resolved Versus geomean; `tmpl` = family
template geomean):

| weapon | local block (file:line) | warhead | gm | tmpl gm |
|---|---|---|---|---|
| AsianMLRS | ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml:9335 | MissileAP_Medium | 87.7 | 98.3 |
| AsianSpitfireRockets | ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml:9515 | MissileAP_Medium | 87.7 | 98.3 |
| NaxiShrek | ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml:2029 | MissileHE_Medium | 94.5 | 100.1 |
| NaxiShrekCons | ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml:2514 | MissileHE_Medium | 94.5 | 100.1 |
| NaxiShrekCons_elite | ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml:2872 | MissileHE_Medium | 94.5 | 100.1 |
| NaxiShrek_elite | ContentPacks/RedAlert2Mod/Naxis/yaml/weapons.yaml:2389 | MissileHE_Medium | 94.5 | 100.1 |
| RA2APCRocket | ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml:7116 | MissileHE_Medium | 94.5 | 100.1 |
| RA2APCRocket_elite | ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml:7381 | MissileHE_Medium | 94.5 | 100.1 |
| RA2HoverMissile | ContentPacks/RedAlert2/Shared/yaml/weapons.yaml:1194 | MissileHE_Light | 90.6 | 98.1 |
| RA2HoverMissile_AA | ContentPacks/RedAlert2/Shared/yaml/weapons.yaml:1398 | MissileAA_Light | 82.9 | 93.9 |
| RA2HoverMissile_AA_elite | ContentPacks/RedAlert2/Shared/yaml/weapons.yaml:1488 | MissileAA_Light | 82.9 | 93.9 |
| RA2HoverMissile_elite | ContentPacks/RedAlert2/Shared/yaml/weapons.yaml:1293 | MissileHE_Light | 90.6 | 98.1 |
| RA2MammothTusk_AA | ContentPacks/RedAlert2/Soviets/yaml/weapons.yaml:2169 | MissileAA_Medium | 82.7 | 94.6 |
| RA2MammothTusk_AA_elite | ContentPacks/RedAlert2/Soviets/yaml/weapons.yaml:2257 | MissileAA_Medium | 82.7 | 94.6 |
| RA2Medusa_AA | ContentPacks/RedAlert2/Allies/yaml/weapons.yaml:3013 | MissileAA_Medium | 82.7 | 94.6 |
| RA2MultiHoverMissile | ContentPacks/RedAlert2/Shared/yaml/weapons.yaml:1835 | MissileAP_Light | 86.4 | 97.2 |
| RA2MultiHoverMissile_AA | ContentPacks/RedAlert2/Shared/yaml/weapons.yaml:2140 | MissileAA_Light | 82.9 | 93.9 |
| RA2MultiHoverMissile_AA_elite | ContentPacks/RedAlert2/Shared/yaml/weapons.yaml:2230 | MissileAA_Light | 82.9 | 93.9 |
| RA2MultiHoverMissile_elite | ContentPacks/RedAlert2/Shared/yaml/weapons.yaml:2033 | MissileAP_Light | 86.4 | 97.2 |
| RA2MultiThunderboltMissile | ContentPacks/RedAlert2/Shared/yaml/weapons.yaml:2336 | MissileAP_Light | 86.4 | 97.2 |
| RA2MultiThunderboltMissile_elite | ContentPacks/RedAlert2/Shared/yaml/weapons.yaml:2434 | MissileAP_Light | 86.4 | 97.2 |
| RA2ThunderboltMissile | ContentPacks/RedAlert2/Shared/yaml/weapons.yaml:1593 | MissileAP_Light | 86.4 | 97.2 |
| RA2ThunderboltMissile_elite | ContentPacks/RedAlert2/Shared/yaml/weapons.yaml:1691 | MissileAP_Light | 86.4 | 97.2 |
| RocketAngelRockets | ContentPacks/RedAlert/Japan/yaml/weapons.yaml:2043 | MissileAP_Light | 86.4 | 97.2 |
| ra1_soviets_monstertank_missile | ContentPacks/RedAlert/Soviets/yaml/weapons.yaml:10510 | MissileAP_Heavy | 94.2 | 98.7 |
| ra1_allies_longbow_missile | ContentPacks/RedAlert/Allies/yaml/weapons.yaml:184 | MissileAP_Heavy | 94.2 | 98.7 |
| RA2Patriot | ContentPacks/RedAlert2/Allies/yaml/weapons.yaml:21 | MissileAA_Medium | 82.7 | 94.6 |

**Direction for a fix (not applied):** either migrate these to `Inherits@missilerole` +
warhead-only-Damage overrides (the pattern the rest of the cohort already uses), or run the
same `_to_mean` normalization on the materialized blocks. Both need a maintainer ruling —
this agent did not touch balance data.

**Note on the four `_AA`/FlatCompatibility same-family members** (`RA2HoverMissile_AA`,
`RA2HoverMissile_AA_elite`, `RA2MultiHoverMissile_AA`, `RA2MultiHoverMissile_AA_elite`):
their contract expected the *recorded reviewed override* (`old[0]`, `MissileAA_LightFlatCompatibility`
profile) — live also carries a materialized `MissileAA_Light` ladder (None:36-era, gm 82.9,
plus REFLECTOR 69→70). Same root class; included here rather than filed separately.

---

## Pending triage (not yet filed)

- `cannonap_endpoint_cohort` (10) — runtime-units contract, under investigation.
- AI HQ/refinery cleanup (5) — HQ sell-protection set shrank to zerg only; checking provenance.
- Duplicate sibling keys — checking against `3e14d9b4a` collapse scope.
- Dragunov / hammertank / EMP damage drift — TTK flips, under investigation.
- Remaining misc stale candidates (d2k fluent, ai_logging, yak, soviet_rename, sonic,
  authorized_* cohorts) — classification in progress.
