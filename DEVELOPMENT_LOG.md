# Development Log

## 2026-08-24 — old-repo reconciliation, no-file-change merge, full verification

- Investigated `cameo-mod/Cameo-mod/compare/master...Zeruel87:Cameo-mod:master` showing 2 stray commits on the old fork.
- Re-added `https://github.com/Zeruel87/Cameo-mod.git` as `upstream`, fetched and inspected the two commits:
  - `15159ad7a` Merge pull request #128 from cameo-mod/op2_zhall
  - `fd58e3f93` W24: D2K heavy missile HE 3-way split with D2K Shared projectile/effect templates (#133)
- A direct merge would have produced ~594k lines of conflicts because the repos diverged by 2232 commits; instead did `git merge -s ours upstream/master` on a temp branch, fast-forwarded `weapon_structure_and_warhead_fold` and pushed both it and `master` to `cameo-mod/Cameo-mod`.
- The GitHub compare page now reports "There isn’t anything to compare" and "cameo-mod:master is up to date with all commits from Zeruel87:master".
- Verified the merge did not change the working tree or the content: only pre-existing uncommitted change is `tools/balance/gen_weapon_template.py` (heaviness-bell WIP, 124 new lines) and untracked `scratchpad/` files.
- Ran gating audits: `find_empty_warhead` 0; `find_orphan_old_keys` 0 real; `find_orphan_old_keys_multi` 0; `audit_doc_claims` 19/19 green; `audit_doc_health` PASS; `environment.py` complete; `verify_generator_sync` drift 0; `audit_heaviness_bell` 0 inversions/0 mean drift; `tools/tests` 300/300 OK; `audit_warhead_split` 937 vs baseline 939 (pre-existing W24 debt, not a regression).
- Re-read `HANDOFF.md`, `design/ROADMAP.md` and related docs; current queue: implement bell in `gen_weapon_template.py` (Step 5 per HANDOFF §3.0), W24 burn-down, independent W7/W9/W10 meters.
- Did **not** touch the live `gen_weapon_template.py` WIP or any weapon YAML to avoid breaking in-progress work.

### Open todos at end of session

1. Decide whether to force-push `Zeruel87/Cameo-mod:master` to match `cameo-mod/Cameo-mod:master` (destructive).
2. ~~Remove or re-point local `upstream` remote to prevent accidental pushes to the old repo.~~ DONE — removed `upstream` (Zeruel87).
3. ~~Fix stale `multi_main_fired_weapons` 927 → 925 in `HANDOFF.md`, `BALANCE_PROGRAM_PLAN.md`, and `audit/SUMMARY.md`.~~ DONE — `audit_doc_claims` still 19/19 clean.
4. Regenerate `docs/audit/latest/` with `python tools/audit/run_all.py` (bash unavailable; Python port is the fallback) from a complete tree, then review every changed tracked file before staging.
5. Continue W24/Phase B work only after verifying set B availability; `_stageB_made.txt` remains in scratchpad.

## 2026-08-24 (continued #2) — picked up open todos

- Removed local `upstream` remote (Zeruel87) to prevent accidental pushes; remotes now `origin` and `github-desktop-SteamsDev`.
- Fixed stale `multi_main_fired_weapons` count from `927` to `925` in:
  - `docs/HANDOFF.md` (overview and board table),
  - `docs/design/BALANCE_PROGRAM_PLAN.md` (Phase A A6),
  - `docs/audit/SUMMARY.md` (programme-scale debt table).
- Re-ran `audit_doc_claims`: 19/19 clean; `multi_main_fired_weapons` measured 925 matches documented 925.
- Verified the live heaviness-bell WIP in `tools/balance/gen_weapon_template.py` is still off (`USE_BELL` defaults to `0`) and the current generator reproduces shipped templates (`verify_generator_sync` drift 0 with bell off).
- Re-ran `tools/balance/preview_bell.py` (valid tilt-to-tilt comparison): 130 of 136 profiles move, mean 8.3% row change, **0 ladder inversions**, worst row 32.0% on `Chemical_Medium`; the shipped `class_tilt` scores worse against the same control. Did NOT enable `USE_BELL` or splice because rule 4 requires explicit authorisation to change `Versus`.
- Re-read `HANDOFF.md` thoroughly and updated it: the three tooling defects are **already fixed**, `docs/audit/latest/` has been regenerated from a complete tree, and Step 5's generator half is done. Set B remains **NOT free** (31 `^LightFlameWeapon` matches live); did not touch weapon YAML.
- Ran the full audit suite (`python tools/audit/run_all.py`; bash unavailable on this Windows shell) from a complete tree to regenerate `docs/audit/latest/*.md`. Suite exit code 1 from pre-existing gating failures; `audit_doc_health` **PASS**.
- `tools/tests` still 300/300 green; `find_empty_warhead` 0.
- Committed the inert bell work to `weapon_structure_and_warhead_fold`:
  - `tools/balance/gen_weapon_template.py` + `tools/balance/preview_bell.py` (OFF by default, `CAMEO_HEAVINESS_BELL=1` to preview).
  - `OpenRA.Mods.Cameo/Warheads/AreaDamageWarhead.cs` gains `Heaviness` int field (0 = disabled / today's behaviour).
  - Rebuilt (`dotnet build` 0 errors) and boot-gated: `MenuPostProcessEffect.PostWorldLoaded`, no new `exception-*.log`.
- **Continued Step 5:** ported `heaviness_bell` to C# (`OpenRA.Mods.Cameo/Warheads/HeavinessBell.cs`) and wired it to `AreaDamageWarhead` at `RulesetLoaded`. `Heaviness=0` keeps authored Versus; non-zero tilts `Versus`/`PercentageVersus`. Spread scale intentionally not wired (pending ruling). Rebuilt, re-tested, re-boot-gated; all green. Refreshed `docs/audit/latest/`.

### Open at end of session

- Wire `Heaviness` into `AreaDamageWarhead`'s `Versus` lookup / `Spread` computation (the C# transform).
  **DONE 2026-08-24** — `HeavinessBell.cs` ported from `gen_weapon_template.py`, wired at
  `RulesetLoaded`. `Heaviness = 0` keeps today's behaviour; non-zero tilts `Versus` and
  `PercentageVersus` and scales `Spread` linearly 2/3 → 1 → 4/3 for h ∈ [0,2] (Light/Medium/Heavy).
  Trace/Super are outside the ruled h range and not yet reproduced. No yaml sets `Heaviness`, so
  the change is inert.
- Only after the C# transform is proven: enable `USE_BELL`, splice the generator, collapse Light/Medium/Heavy templates, set per-weapon `Heaviness`.
- Set B remains NOT free (31 `^LightFlameWeapon` matches); do not touch weapon YAML.

## 2026-08-24 (continued) — full composition-rollout cost analysis

- Merged `master` into `weapon_structure_and_warhead_fold` via fast-forward (`ad213ce0a`) and returned to the feature branch; no working-tree changes.
- Measured the live Cameo roster from `cameo_model`:
  - 29 real (non-meta) factions, 812 unique buildable combat units, 903 faction-specific combat rows, 1,782 unit x queue rows.
- Measured `mods/cameo/ai/ai.yaml`:
  - one `UnitBuilderBotModuleCA@generic` with `UseCompositions: true`, 1,386 `UnitsToBuild` entries (1,375 unique units), 2 active `Composition@` entries (11 UTB rows).
- Measured reference AI systems:
  - `CAmod` `UnitCompositionsBotModule`: 7 compositions, 223 total `UnitsToBuild` entries (195 baseline + 6 pushes).
  - `crystallized-nexus` `CNSquadManagerBotModule`: 198 `Teams` across 5 personalities, 232 `Slots` total.
- Ran projections in `scratchpad/ai_compositions/_tmp_full_cost.py` for full rollout scenarios (global baseline vs per-faction vs per-faction x personality); worst-case full data is 145-435 compositions and 1,400-8,900 `UnitsToBuild` rows.
- No YAML or code changes committed; generated scripts live only in untracked `scratchpad/`.

## 2026-08-22 — A2 committed + audit guards documented

- Committed W24 A2 (five nuclear/thermobaric weapons collapsed to one damage family).
- Cleaned the malformed `docs/design/BALANCE_PROGRAM_PLAN.md` W24 status line.
- Added `W27` to the BPP for inline `Warhead@Effect*` debt.
- Documented the `audit_upgrade_regression.py` + blast-shape diff findings in
  `docs/audit/SUMMARY.md` and `docs/LESSONS_LEARNED.md`.
- Recorded the maintainer ruling: effect warheads should be inherited, not inline;
  superweapons are the only accepted exception.
- Built and ran `tools/audit/audit_inline_effects.py`: 665 concrete weapons carry
  815 inline effect nodes; 628 non-exempt (superweapons auto-detected) remain.

## 2026-08-22 — docs/audit: reconcile `doc_claims` and regenerate `latest/` evidence

- Ran `tools/audit/run_all.sh` and fixed the `audit_doc_claims` mismatches:
  - `shield_versus_mean` 186.791, `shield_hp_factor` 0.535357,
  - `multi_main_fired_weapons` 927, `w24_multi_main_fed` 380,
  - `plating_families` 37.
- Updated `docs/audit/doc_claims.yaml` and the listed design docs
  (`BALANCE_PROGRAM_PLAN`, `PHYSICAL_STATE_SYSTEM`, `PSEUDO_ARMOR_AND_INTEGRITY`,
  `SUPERWEAPON_LAYER_DAMAGE`, `PLATING_COMPOSITION_REFINEMENT`, `DESIGN.md`).
- Appended the 5 missing blend families to the plating matrix
  (`CannonNuke`, `MissileNuke`, `MissileQuantum`, `MissileTesla`, `MissileThermobaric`).
- Regenerated `docs/audit/latest/*.md` and `docs/factions/MATRIX.md`,
  converted all evidence to UTF-8 LF.
- `python tools/audit/audit_doc_claims.py` is clean (16/16 green).
- Boot-gated: menu loaded, no new exceptions.
- Commit: `564089ef9`.

## 2026-08-22 — W24 A2: five nuclear/thermobaric weapons collapsed (boot-gated)

- Converted five multi-main weapons to one damage warhead each, preserving per-shot totals:
  - `NuclearMaverick` -> `^Warhead_MissileHE_Heavy` (40 000 main, 11 percentage)
  - `ThermobaricNuclearMaverick` -> `^Warhead_MissileThermobaric_Heavy` (42 000 main, 15 percentage)
  - `MonsterTank120mm` -> `^Warhead_CannonNuke_Heavy` (80 000 main, 22 percentage)
  - `TorpTubeThermobaric` -> `^Warhead_MissileNuke_Heavy` (32 000 main, 9 percentage)
  - `MonsterTank120mmThermobaric` -> `^Warhead_CannonFire_Heavy` (120 000 main, 42 percentage)
- Dropped the `^Warhead_Nuclear_Super` component from the Su-57 base/upgrade pair.
- Fixed `^Warhead_CannonFire_*` and `^Warhead_MissileFire_*` `DamageTypes` to
  `Prone75Percent, TriggerProne, FireDeath, Incendiary` in `tools/balance/gen_weapon_template.py`
  and re-spliced `mods/cameo/weapons/weapons.yaml`.
- Left `SCUDNUKE` and `SCUDNUKEThermobaric` on `^Warhead_Nuclear_Super` pending maintainer call.
- Verification: `review_batch_diff` clean, `find_empty_warhead` 0, `find_orphan_old_keys` 0 real,
  `audit_warhead_split` 939 vs baseline 939, `verify_generator_sync` 0,
  `extract_stats --check` 0, boot-gated (menu loaded, no new exceptions).

## 2026-08-22 — W24 A1a: delivery-first blend family rename

- Renamed the four element-first blend families to delivery-first names
  (CannonFire, MissileFire, CannonChem, MissileChem) across
  gen_weapon_template.py, mods/cameo/weapons/weapons.yaml,
  mods/cameo/weapons/missiles.yaml, and four ContentPack weapon files.

- Fixed tools/rename/safe_rename.py to preserve the exact case of the
  replacement string (it was lower-casing all renamed ids).

- Fixed tools/balance/splice_templates.py to always run the full generator
  before splicing, so shield_uniqueness sees the complete set and
  produces correct final Shield values; also preserves the original
  newline style.

- Spliced Flame and MissileChem blocks so verify_generator_sync
  reports drift = 0.

- Regenerated balance ledgers (extract_stats.py); audit_balance_drift clean.

- find_empty_warhead 0, find_orphan_old_keys 0,
  audit_warhead_split broadcast count 944 (baseline 939; expected red).

## 2026-08-21 — JapanesePlasmaBomb 3-way split (boot-gated)





- Converted `JapanesePlasmaBomb` in `mods/cameo/ContentPacks/RedAlert/Japan/yaml/weapons.yaml`:


  - Replaced the legacy `Inherits@3: ^HeavyBomb` full-stack inheritance with the split


    `Inherits@wh3: ^Warhead_Demolition_Heavy` and `Inherits@fx2: ^Effect_Demolition_Heavy`.

  - Kept the existing chemical and flame 3-way split (`^Warhead_Chemical_Heavy`,


    `^Warhead_Flame_Heavy`, `^Projectile_Chem_Heavy`, `^Effect_Flame_Heavy`).

  - Preserved demolition totals: main `10000` flat (`AreaDamage`, `MaxRadius: 3200`,


    `Spread: 800`) and percentage `5%` (`AreaDamagePercentage`, `MaxRadius: 1600`,


    `Spread: 400`).

  - Preserved old `HeavyBomb` falloff shape: the new `^Warhead_Demolition_Heavy` family


    `Falloff` is `100, 50, 25, 10, 5, 0`; setting `MaxRadius: 3200` and `1600` makes the


    resolved falloff identical to the old 5-step `100, 50, 25, 10, 5` shape.

  - Preserved local damage types `Prone100Percent, TriggerProne, ElectricityDeath, Tesla`


    and `ValidRelationships: Enemy` on the demolition warheads (the family defaults to


    `Ally, Neutral, Enemy`).

  - Restored the weapon-specific primary explosion visual by overriding


    `Warhead@Effect1.Explosions: poof` (the `^Effect_Demolition_Heavy` family supplies


    `building`). Kept `Warhead@Effect` (`blueartexp`/`psahit00.aud`) and `Warhead@Effect2`


    (`blue_building_napalm`).

  - Preserved the bullet projectile (`Image: hakureiring`, `Speed: 250`, `Inaccuracy: 500`,


    `TrailImage: blue_smokey`) and burst/report behavior.

- `find_empty_warhead` 0, `find_orphan_old_keys` 0, `audit_warhead_split` broadcast


  count 941 (baseline already 941), `audit_balance_drift` clean, `extract_stats` regenerated.

- `review_resolve_diff` reports `OK (behavioural invariants preserved)`.

- `launch-game.cmd` reached `MenuPostProcessEffect.PostWorldLoaded`; no new `exception-*.log`.




## 2026-08-21 — TorpTubeThermobaric full 3-way split (boot-gated)





- Converted `TorpTubeThermobaric` in `mods/cameo/ContentPacks/RedAlert/Shared/yaml/weapons.yaml`:


  - Replaced legacy `Inherits: ^NuclearWarhead` with `Inherits@wh: ^Warhead_Nuclear_Super`


    and `Inherits@fx: ^Effect_Nuclear_Super`.

  - Replaced the remaining `Inherits@2: ^HeavyMissile` full-stack with


    `Inherits@wh2: ^Warhead_MissileAP_Heavy`, `Inherits@proj: ^Projectile_Missile_Heavy`,


    and `Inherits@fx2: ^Effect_MissileAP_Heavy`.

  - Preserved nuclear totals: main `1600` × 10 ticks (`MaxRadius: 9000`) for the old


    `16000` flat, and percentage `1` × 8 ticks (`Spread: 500`, `MaxRadius: 4500`) for


    the old `8%`.

  - Preserved missile totals: main `16000` flat (`AreaDamage`, `MaxRadius: 4000`,


    `Spread: 800`) and percentage `8%` (`AreaDamagePercentage`, `MaxRadius: 2000`,


    `Spread: 400`).

  - Preserved old nuclear shape: `AffectsParent: true`, `ValidRelationships: Enemy`,


    `FireDeath, Incendiary`, and `TargetActorCenter: false`.

  - Preserved the torpedo projectile (`Image: v2`, `Speed: 150`, `TrailImage: bubbles`,


    water-bound, cloak palette) and report `torpedo1.aud`. The bespoke projectile is


    still built from scratch with `-Projectile:`, so `^Projectile_Missile_Heavy` is


    declared as the family but the resolved torpedo fields are unchanged.

  - Removed the new `Warhead@Glow` that `^Effect_Nuclear_Super`/`^Effect_MissileAP_Heavy`


    would have introduced by keeping `-Warhead@Glow:`.

  - Effect order kept `^Effect_Nuclear_Super` first so `^Effect_MissileAP_Heavy` wins for


    `ShieldHit`, `Concrete` (`200`), `DuneRock`, `DuneSand`, `RA2Crater`, and the


    non-nuclear `Effect` (`big_frag`), then the weapon overrides to `nuke_small`/


    `kaboom22.aud`/`ImpactActors: true`. A local `Warhead@ShieldHit` override keeps


    `Duration: 10` (the `^Effect_MissileAP_Heavy` family supplies `12`).

- `find_empty_warhead` 0, `find_orphan_old_keys` 0, `audit_warhead_split` broadcast


  count 941 (no change), `audit_balance_drift` clean, `extract_stats` regenerated.

- `review_resolve_diff` reports `OK (behavioural invariants preserved)`.

- `launch-game.cmd` reached `MenuPostProcessEffect.PostWorldLoaded`; no new `exception-*.log`.




## 2026-08-21 — MonsterTank120mm 3-way split (boot-gated)





- Converted `MonsterTank120mm` in `mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml`


  from `^NuclearWarhead` to the 3-way split:


  - `Inherits@wh: ^Warhead_Nuclear_Super`


  - `Inherits@wh2: ^Warhead_CannonHE_Heavy`


  - `Inherits@proj: ^Projectile_Shell_Heavy`


  - `Inherits@fx: ^Effect_CannonHE_Heavy`


  - `Inherits@fx2: ^Effect_Nuclear_Super`


- Preserved per-shot totals: `CannonHE_Heavy` `40000` flat / `20%`; `Nuclear_Super` main


  `4000` × 10 ticks (`MaxRadius: 9000`) and percentage `2` × 10 ticks (`Spread: 500`,


  `MaxRadius: 4500`) for the old `20%`.

- Preserved old `SpreadDamage`/`HealthPercentageDamage` shape for the nuclear half:


  `AffectsParent: true`, `ValidRelationships: Enemy`, `FireDeath, Incendiary`.

- Kept `Report: nukemisl.aud`, bullet projectile (`Image: 120MM`, `Speed: 300`, `Inaccuracy: 500`),


  and the local `Effect` (`nuke_small`, `kaboom22.aud`, `ImpactActors: true`).

- `MonsterTank120mmThermobaric` (child) now inherits the same nuclear/cannon split plus


  `^Warhead_Flame_Heavy` / `^Projectile_Flame_Heavy` / `^Effect_Flame_Heavy`; resolved


  totals remain `120000` flat + `60%`.

- `find_empty_warhead` 0, `find_orphan_old_keys` 0, `audit_warhead_split` broadcast


  baseline lowered 944 → 942, `audit_balance_drift` clean, `audit_doc_claims` 16/16,


  `verify_generator_sync` drift 0.

- `launch-game.cmd` reached `MenuPostProcessEffect.PostWorldLoaded`; no new `exception-*.log`.




## 2026-08-21 — ThermobaricNuclearMaverick 3-way split (boot-gated)





- Converted `ThermobaricNuclearMaverick` in `mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml`


  from the broken duplicate `Inherits@2: ^NuclearWarhead` / `Inherits@2: ^Warhead_Flame_Heavy` stack


  to a clean 3-way split with distinct inherit keys:


  - `Inherits@wh: ^Warhead_MissileHE_Heavy`


  - `Inherits@wh2: ^Warhead_Nuclear_Super`


  - `Inherits@wh3: ^Warhead_Flame_Heavy`


  - `Inherits@proj: ^Projectile_Missile_Heavy`


  - `Inherits@fx: ^Effect_Flame_Heavy`


  - `Inherits@fx2: ^Effect_Nuclear_Super`


- Preserved total per-shot damage: `MissileHE_Heavy`/`Flame_Heavy` stay `14000` flat/`7%`;


  `^Warhead_Nuclear_Super` delivers `1400` × 10-tick `AreaDamage` (`MaxRadius: 9000`) and


  `1` × 7-tick `AreaDamagePercentage` (`Spread: 500`, `MaxRadius: 4500`) to keep the old `7%`


  percentage total while using the canonical nuclear family.

- Preserved old `SpreadDamage`/`HealthPercentageDamage` shape (`FireDeath, Incendiary` damage


  types, `AffectsParent: false`, `ValidRelationships: Enemy`) for the nuclear half.

- Resolved `Effect`/`Effect2`, `Glow`, `Smudge`, `RA2Scorch`, `GroundFire`, `Concrete: 1000`,


  `ShieldHit` duration 25, `ShieldHitEffect`, `ShieldHitEffectNuclear` all unchanged.

- `extract_stats.py` regenerated ledgers and derived sidecars; `audit_balance_drift` clean.

- Audits: `find_empty_warhead` 0, `find_orphan_old_keys` 0 real,


  `audit_warhead_split` 944 (baseline lowered 945→944),


  `audit_doc_claims` 16/16 clean, `verify_generator_sync` drift 0.

- `launch-game.cmd` reached `MenuPostProcessEffect.PostWorldLoaded`; no new `exception-*.log`.




## 2026-08-21 — NuclearMaverick 3-way split (boot-gated)





- Converted `NuclearMaverick` in `mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml`


  from the old full-stack `^NuclearWarhead` to a 3-way split finish conversion:


  - `Inherits@wh: ^Warhead_MissileHE_Heavy`


  - `Inherits@wh2: ^Warhead_Nuclear_Super`


  - `Inherits@proj: ^Projectile_Missile_Heavy`


  - `Inherits@fx: ^Effect_Nuclear_Super`


  - `Inherits@fx2: ^Effect_MissileHE_Heavy`


- Preserved per-shot totals (40000 flat + 20% percentage) by using the


  `^Warhead_Nuclear_Super` 10-tick `AreaDamage` design with local `MaxRadius: 9000`


  (main, `Damage: 2000`) and `Spread: 500`/`MaxRadius: 4500` (percentage, `Damage: 1`).

- Preserved old `SpreadDamage`/`HealthPercentageDamage` shape (falloff 100->10,


  `AffectsParent: false`, `ValidRelationships: Enemy`, `DamageTypes: Prone75Percent,


  TriggerProne, FireDeath, Incendiary`) while moving to the canonical nuclear family.

- Preserved `^Effect_MissileHE_Heavy` as the dominant effect layer: `Concrete: 200`,


  `ShieldHit` duration 10, `EffectAir: big_explosion_air`, main `Effect: nuke_small`


  (local), `Glow`/`Smudge`/dune smudges, plus `^Effect_Nuclear_Super`'s


  `Smudge1/2/3` and `ShieldHitEffectNuclear`.

- `extract_stats.py` regenerated ledgers and derived sidecars; `audit_balance_drift` clean.

- Audits: `find_empty_warhead` 0, `find_orphan_old_keys` 0 real,


  `audit_warhead_split` 945 (baseline lowered 946->945),


  `audit_doc_claims` 16/16 clean, `verify_generator_sync` drift 0.

- `launch-game.cmd` reached `MenuPostProcessEffect.PostWorldLoaded`; no new `exception-*.log`.




## 2026-08-24 — HammerheadArtillery 3-way split (boot-gated)





- Converted `HammerheadArtillery` in `mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml`


  from the old `^RA2Grenade` + `^HeavyBomb` + `^SteelMediumCannon` pileup to a 2-warhead 3-way split:


  - `Inherits@wh: ^Warhead_Demolition_Heavy` (`Damage: 22222`, `Demolition_Heavy_Percentage` `Damage: 22`)


  - `Inherits@wh2: ^Warhead_CannonHE_Medium` (`Damage: 11111`, `CannonHE_Medium_Percentage` `Damage: 11`)


  - `Inherits@proj: ^Projectile_Shell_Medium` with local `Bullet` overrides


  - `Inherits@fx: ^Effect_Demolition_Heavy`


- Merged `Demolition_Light` (11111/11) and `HeavyBomb` (11111/11) into one heavy demolition warhead


  so the per-shot total stays 33333/33. The `CannonHE_Medium` warhead stays as the cannon-shell


  contribution.

- Preserved `Projectile: Bullet` (`Image: 120MM`, `Speed: 333`, `LaunchAngle: 111`, `Inaccuracy: 1111`,


  `Blockable: false`, blue contrail colors/widths/length), `Range: 11111`, `MinRange: 2220`,


  `ReloadDelay: 111`, `Report: vdesatta.wav, vdesattb.wav`.

- Inlined all actor-specific effect/smudge/glow/shield/concrete overrides:


  `steel_blueexp`/`makoexplose` main, `siege_impact` second, `blue_building_napalm`/`kaboom12`


  delayed, `RA2Crater`/`RA2Scorch` + cannon dune smudges, `med_explosion_air` air effect,


  `ra2_small_watersplash` water, shell-style shield-hit sound, `Concrete: 150`, `ShieldHit` duration 10.

- `review_resolve_diff.py wt_baseline . HammerheadArtillery` reports only the expected damage-multiset


  collapse; all projectile/effect invariants preserved.

- `extract_stats.py` regenerated ledgers and derived sidecars; `audit_balance_drift` clean.

- Audits: `find_empty_warhead` 0, `find_orphan_old_keys` 0 real, `audit_warhead_split` 946


  (baseline lowered 950→946), `audit_doc_claims` 16/16 clean, `verify_generator_sync` drift 0.

- `launch-game.cmd` reached `MenuPostProcessEffect.PostWorldLoaded`; no new `exception-*.log`.




## 2026-08-21 — AsianChemicalBombs 3-way split (boot-gated)





- Converted `AsianChemicalBombs` in `mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml`


  from the old full-stack `^HeavyChemicalWeapon` to a clean 3-way split:


  - `Inherits@wh: ^Warhead_Chemical_Heavy`


  - `Inherits@2: ^RA2MediumCannon`


- Kept the custom projectile (Bullet, `Image: aa_plasgree`, `Speed: 400`, contrail,


  trail), `Report: vflaat1a.wav, vflaat1b.wav`, `Range: 3000`, `ReloadDelay: 8`,


  `InvalidTargets: wall`, and `ValidTargets: Ground, Water`.

- Preserved both 2000 damage warheads (Chemical_Heavy and CannonHE_Medium) and the


  `HealthPercentageDamage` CannonHE percentage warhead.

- Inlined `RA2VirusDeath` kill type, `Corrosion` physical state, `aa_plasgreeexp`


  explosion with `GlowScale: 2.0`, and the `RA2MediumCannon`-supplied `Concrete: 150`


  / shell-style shield-hit effects.

- `review_resolve_diff.py wt_baseline . AsianChemicalBombs` OK.

- `extract_stats.py` regenerated ledgers; `audit_balance_drift` clean.

- Audits: `find_empty_warhead` 0, `find_orphan_old_keys` 0 real, `audit_warhead_split`


  947 (baseline 950), `audit_doc_claims` 16/16 clean, `verify_generator_sync` drift 0.

- `launch-game.cmd` reached `MenuPostProcessEffect.PostWorldLoaded`; no new `exception-*.log`.




## 2026-08-21 — TSScoopDualChem 3-way split (boot-gated)





- Converted `TSScoopDualChem` in `mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/weapons.yaml`


  from the old full-stack `^MediumChemicalWeapon` to a 3-way split:


  - `Inherits@wh: ^Warhead_CannonHE_Medium`


  - `Inherits@wh2: ^Warhead_Chemical_Medium`


  - `Inherits@proj: ^Projectile_Shell_Medium`


  - `Inherits@fx: ^Effect_CannonHE_Medium`


  - `Inherits@fx2: ^TSCannonEffect`


- Preserved CannonHE 20000 / percentage 10 plus Chemical 10000 / percentage 5,


  `Bullet` `Speed: 3500`, `Report: flamer2.aud`, `med_tibnapalm` ground explosion


  with `xplobig6.aud` and glow, `ShieldHit` duration 8, and bullet-style shield-


  hit sounds by inlining the actor-specific overrides.

- `review_resolve_diff.py wt_baseline . TSScoopDualChem` OK.

- `extract_stats.py` regenerated ledgers; `audit_balance_drift` clean.

- Audits: `find_empty_warhead` 0, `find_orphan_old_keys` 0 real, `audit_warhead_split`


  947 (baseline 950), `audit_doc_claims` 16/16 clean, `verify_generator_sync` drift 0.

- `launch-game.cmd` reached `MenuPostProcessEffect.PostWorldLoaded`; no new `exception-*.log`.




## 2026-08-21 — TS70mmChem 3-way split (boot-gated)





- Converted `TS70mmChem` in `mods/cameo/ContentPacks/TiberianSun/Forgotten/yaml/weapons.yaml`


  from the old full-stack `^LightChemicalWeapon` to a proper 3-way split:


  - `Inherits@wh: ^Warhead_CannonHE_Medium`


  - `Inherits@wh2: ^Warhead_Chemical_Light`


  - `Inherits@proj: ^Projectile_Shell_Medium`


  - `Inherits@fx: ^Effect_CannonHE_Medium`


  - `Inherits@fx2: ^TSCannonEffect`


- Preserved the per-actor projectile speed (`Bullet` `Speed: 3500`), report (`flamer2.aud`),


  chemical warhead damage (4000 CannonHE + 2000 Chemical), percentage damage, `TiberiumDeath`


  kill type, `chemball` explosion, `ShieldHit` duration 6, `Concrete: 100`, and bullet-style


  `ShieldHitEffect` sounds by inlining the local overrides that the old full-stack used to supply.

- `review_resolve_diff.py wt_baseline . TS70mmChem` OK.

- `extract_stats.py` regenerated ledgers; `audit_balance_drift` clean.

- Audits: `find_empty_warhead` 0, `find_orphan_old_keys` 0 real, `audit_warhead_split`


  947 (baseline 950), `audit_doc_claims` 16/16 clean, `verify_generator_sync` drift 0.

- `launch-game.cmd` reached `MenuPostProcessEffect.PostWorldLoaded`; no new `exception-*.log`.




## 2026-08-21 — SteelHoverMissile 3-way split (boot-gated)





- Converted `SteelHoverMissile` in `mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/weapons.yaml`


  from `^ArrowWeapon + ^SteelLightMissile` to `^SteelLightMissile` only, collapsing the


  two 4000 main warheads (`ArrowWeapon` + `MissileAP_Light`) into one `MissileAP_Light`:


  - `Damage: 8000`


  - `MissileAP_Light_Percentage` `Damage: 4` (HealthPercentageDamage preserved)


- Kept the per-faction `^SteelLightMissile` addon (it supplies the RA2-style missile


  contrail and `steel_blueexp` look) and `Inherits@fx: ^Effect_Grey_Explosion_Small_RA2`


  (resolved `ra2_small_grey_explosion` ground/water effect).

- Added `ImpactActors: false` to the local `Warhead@Effect` node to preserve the exact


  resolved CreateEffect behaviour after `^ArrowWeapon` was removed.

- `review_resolve_diff.py wt_baseline . SteelHoverMissile` OK.

- `extract_stats.py` regenerated ledgers; `audit_balance_drift` clean.

- Updated `doc_claims.yaml` and `docs/design/BALANCE_PROGRAM_PLAN.md` W24 counts:


  `multi_main_fired_weapons` 935 → 934; 1–2 legacy 117 → 116; broadcast 577 → 576 (61.7%).

- Audits: `find_empty_warhead` 0, `find_orphan_old_keys` 0 real, `audit_warhead_split`


  947 (baseline 950, one fewer broadcast), `audit_doc_claims` 16/16 clean,


  `verify_generator_sync` drift 0.

- `launch-game.cmd` reached `MenuPostProcessEffect.PostWorldLoaded`; no new `exception-*.log`.




## 2026-08-21 — HueyGun 3-way split (boot-gated)





- Converted `HueyGun` in `mods/cameo/ContentPacks/RedAlert2Mod/TKM/yaml/weapons.yaml`


  from `^FlakWeapon` + `^RA2Chaingun` to the single-family 3-way split:


  - `Inherits@wh: ^Warhead_Bullet_Medium` (Damage: 4000, 2 × 2000 preserved)


  - `Inherits@proj: ^Projectile_Bullet_Medium`


  - `Inherits@fx: ^Effect_Bullet_Medium_RA2`


- Preserved `ValidTargets: Ground, Water, Air`, `ReloadDelay: 7`, `Range: 4783`,


  `Report: mgun11.aud`.

- Inlined resolved `ImpactSounds: xplos.aud` on `Effect` and `EffectAir` (the


  `^Effect_Bullet_Medium_RA2` template does not carry impact sounds; the FlakWeapon


  pileup had supplied them). Added `ValidTargets: Air` to the local `EffectAir`.

- `review_resolve_diff.py wt_baseline . HueyGun` OK.

- `extract_stats.py` regenerated ledgers; `audit_balance_drift` clean.

- Updated `doc_claims.yaml` and `docs/design/BALANCE_PROGRAM_PLAN.md` W24 counts:


  `multi_main_fired_weapons` 936 → 935; 1–2 legacy 118 → 117; broadcast 578 → 577 (61.7%).

- Audits: `find_empty_warhead` 0, `find_orphan_old_keys` 0 real, `audit_warhead_split`


  948 (baseline 950, two fewer broadcasts), `audit_doc_claims` 16/16 clean,


  `verify_generator_sync` drift 0.

- `launch-game.cmd` reached `MenuPostProcessEffect.PostWorldLoaded`; no new `exception-*.log`.




## 2026-08-21 — ChainGunMH60 3-way split (boot-gated)





- Converted `ChainGunMH60` in `mods/cameo/ContentPacks/RedAlert/Allies/yaml/weapons.yaml`


  from the old full-stack `^SmallArms`/`^Grenade`/`^FlakWeapon`/`^Chaingun` pileup to the


  single-family 3-way split:


  - `Inherits@wh: ^Warhead_Bullet_Medium` with local `Damage: 8000` (4 × 2000 preserved)


  - `Inherits@proj: ^Projectile_Bullet_Medium` (bullet/50CAL/contrail visuals preserved)


  - `Inherits@fx: ^Effect_Bullet_Medium` (piffs/water/shield hit core preserved)


- Preserved `ReloadDelay: 6`, `Range: 3375`, `Report: gun13.aud`, `ValidTargets: Ground, Water, Air`.

- Inlined the resolved impact-sound/actor overrides and `EffectAir` locally so


  `review_resolve_diff.py` reports the CreateEffect behaviour as unchanged.

- `review_resolve_diff.py wt_baseline . ChainGunMH60` OK (behavioural invariants preserved).

- `extract_stats.py` regenerated all ledgers; `audit_balance_drift` clean.

- Updated `doc_claims.yaml` and `docs/design/BALANCE_PROGRAM_PLAN.md` W24 counts:


  `multi_main_fired_weapons` 937 → 936; W24 pileup shape 202 → 201; broadcast


  count 579 → 578; the four prose occurrences in BPP now read 936.

- Audits: `find_empty_warhead` 0, `find_orphan_old_keys` 0 real, `audit_doc_claims` 16/16 clean,


  `audit_warhead_split` 949 (baseline 950, one fewer broadcast), `verify_generator_sync` drift 0.

- `launch-game.cmd` reached `MenuPostProcessEffect.PostWorldLoaded`; no new `exception-*.log`.

- Skipped `GDISniperRifle` in the same `phase_b_survey` group because the file is currently


  open in the maintainer IDE; will revisit when it is not live WIP.




## 2026-08-21 — Ixian D2K missile damage-total correction (boot-gated)





- Re-verified `D2K_TowerMissile` and `mtank_pri2` against their pre-refactor


  (`7d346685^`) resolved baseline and found the local `Damage` had been set to


  the per-warhead value instead of the per-shot total. Restored the totals:


  - `D2K_TowerMissile`: one `Warhead@MissileAP_Heavy` main `Damage: 16000`


    (was 4 × 4000) and `Damage: 8` for the percentage twin (was 4 × 2).

  - `mtank_pri2`: one `Warhead@MissileAP_Heavy` main `Damage: 24000`


    (was 3 × 8000) and `Damage: 12` for the percentage twin (was 3 × 4).

- Removed explicit `HealthPercentageDamage` from the percentage twins so the


  `^D2KMissile` `AreaDamagePercentage` family is inherited consistently.

- Regenerated all balance ledgers with `extract_stats.py`; `audit_balance_drift`


  reports 32/32 ledgers clean.

- `review_resolve_diff.py wt_pre_7d34668 . D2K_TowerMissile mtank_pri2` reports


  behavioural invariants preserved.

- Audits: `find_empty_warhead` 0, `find_orphan_old_keys` 0 real, `audit_warhead_split`


  950 pre-existing broadcasts, `audit_physical_state_warheads` PASS,


  `audit_doc_claims` 16/16 clean, `verify_generator_sync` drift 0.

- `launch-game.cmd` reached `MenuPostProcessEffect.PostWorldLoaded`; no new


  `exception-*.log`.




## 2026-08-24 — Ixian D2K missile correction (boot-gated)





- Corrected `D2K_TowerMissile` and `mtank_pri2` in


  `mods/cameo/ContentPacks/D2k/Ixian/yaml/weapons.yaml` from the previous


  `Inherits@wh/@wh2/@wh3` (and `@wh4` for the tower) multi-warhead composition to a


  single `Inherits: ^D2KMissile` with custom D2K projectile/effect overrides.

- Removed the 7 per-weapon `^Warhead_*_D2K_TowerMissile` /


  `^Warhead_*_D2K_mtank_pri2` templates from


  `mods/cameo/ContentPacks/D2k/Shared/yaml/weapons.yaml`; the weapons now use the


  existing `^Warhead_MissileAP_Heavy` family via `^D2KMissile` with local `Damage`


  overrides (Tower 4000/percentage 2; tank 8000/percentage 4).

- Preserved D2K heavy missile projectile visuals, smudge/glow/shield/concrete


  effects, `Range`, `ReloadDelay`, `MinRange`, `Report`, `ValidTargets`, `TargetActorCenter`,


  and `Burst`/`BurstDelays`.

- Updated `docs/design/WEAPON_3WAY_SPLIT.md` to remove the Ixian multi-warhead


  exception from the allow-list.

- Updated `docs/design/BALANCE_PROGRAM_PLAN.md` W24 counts (937 multi-main fired,


  579 broadcast / 61.8%), `docs/design/PHYSICAL_STATE_SYSTEM.md`


  (`w24_multi_main_fed` 386→383), `docs/audit/doc_claims.yaml`


  (`multi_main_fired_weapons` 939→937, `w24_multi_main_fed` 385→383,


  `physical_state_fired_weapons` 450→448), `tools/audit/audit_warhead_split.py`


  baseline (952→950), and `docs/design/ROADMAP.md`.

- Re-extracted balance ledgers (`python tools/balance/extract_stats.py`) and


  verified `audit_balance_drift` clean.

- Verification:


  - `scratchpad/ixian_*_before.json` vs `scratchpad/ixian_*_after.json`: extra


    Demolition/Flame/Flak warheads removed; MissileAP main/percentage `Damage`


    and `Projectile`/`Effect` layers preserved.

  - `tools/audit/find_empty_warhead.py` → 0


  - `tools/audit/effect_audit.py` → 0 duplicate `DamagesConcrete`


  - `tools/audit/audit_warhead_split.py` at/below baseline (950)


  - `tools/audit/audit_physical_state_warheads.py` PASS


  - `tools/audit/audit_doc_claims.py` PASS


  - `tools/balance/verify_generator_sync.py` drift 0


  - `launch-game.cmd` reached `MenuPostProcessEffect.PostWorldLoaded`; no new `exception-*.log`





## 2026-08-21 — HeatRayBeam1-4 Inferno 3-way split + doc claim sync (boot-gated)





- Converted `HeatRayBeam1/2/3/4` in


  `mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml` from a partial


  3-way split (`Inherits@wh` + `Inherits@fx` + inline `Projectile`) to a clean


  `Inherits@wh` / `Inherits@proj` / `Inherits@fx` split.

- Added `^Projectile_Inferno_Heavy_HeatRayBeam` in the same file, holding the


  per-weapon `RadBeam` projectile fields (`Color`, `Amplitude`, `WaveLength`,


  `BeamDuration`, `Thickness`, `QuantizationCount`).

- Added `^Effect_Inferno_Heavy` in `mods/cameo/weapons/weapons.yaml` as an alias


  of `^Effect_Flame_Heavy` so the family has its own effect layer; `HeatRayBeam1`


  keeps its local `small_napalm` / `Volume: 0.25` effect override.

- Preserved resolved `Damage`, `Spread`, `Falloff`, `DamageTypes`, `ValidTargets`,


  `Range`, `ReloadDelay`, `Report`, `SoundVolume`, `Projectile` visuals, and all


  `HeatRayBeam2/3/4` beam colour/thickness overrides.

- Fixed stale shield survivability numbers in `docs/DESIGN.md` and


  `docs/design/ARMOR_LAYERS.md` and updated `docs/audit/doc_claims.yaml`


  so `audit_doc_claims.py` passes again (`shield_versus_mean` 183.26, `shield_hp_factor` 0.5457).

- Reconciled W2 status across `docs/design/BALANCE_PROGRAM_PLAN.md` and


  `docs/design/ROADMAP.md` (back in progress, owner Devin, 31 `^LightFlameWeapon`


  matches remain, `HeatRayBeam1-4` 3-way split done).

- Updated `docs/design/WEAPON_3WAY_SPLIT.md` progress log.

- Verification:


  - `scratchpad/heatray_*.json` before/after: all four weapons **identical**


  - `tools/audit/find_empty_warhead.py` → 0


  - `tools/audit/effect_audit.py` → 0 duplicate `DamagesConcrete`


  - `tools/audit/audit_warhead_split.py` at/below baseline (952)


  - `tools/audit/audit_physical_state_warheads.py` PASS


  - `tools/audit/audit_doc_claims.py` PASS


  - `tools/balance/verify_generator_sync.py` drift 0


  - `launch-game.cmd` reached `MenuPostProcessEffect.PostWorldLoaded`; no new `exception-*.log`





## 2026-08-23 — Ixian giant multi-warhead 3-way split (boot-gated)





- Converted `D2K_TowerMissile` and `mtank_pri2` in


  `mods/cameo/ContentPacks/D2k/Ixian/yaml/weapons.yaml` from the old mixed


  `^Grenade`/`^MediumFlameWeapon`/`^FlakWeapon`/`^D2KMissile` full-stack pattern to


  explicit `Inherits@wh` / `Inherits@wh2` / `Inherits@wh3` (and `@wh4` for the tower)


  / `Inherits@proj` / `Inherits@fx`.

- Removed legacy full-stack inherits (`^Grenade`, `^MediumFlameWeapon`, `^FlakWeapon`,


  `^D2KMissile`). Both weapons were added to the `docs/design/WEAPON_3WAY_SPLIT.md`


  exception allow-list because their resolved giant multi-warhead identity requires


  more than two warhead layers (Demolition + Flame + Flak + MissileAP for the tower;


  Demolition + Flame + MissileAP for the tank).

- Added four D2K Shared templates in `mods/cameo/ContentPacks/D2k/Shared/yaml/weapons.yaml`:


  `^Projectile_Missile_Heavy_D2K_TowerMissile`,


  `^Projectile_Missile_Heavy_D2K_mtank_pri2`,


  `^Effect_MissileAP_Heavy_D2K_TowerMissile`, and


  `^Effect_MissileAP_Heavy_D2K_mtank_pri2`.

- Preserved resolved `Damage`, `Versus`, `Spread`, `Falloff`, `DamageTypes`,


  `PhysicalState`, `ReloadDelay`, `Range`, `MinRange`, `Report`, `ValidTargets`,


  `TargetActorCenter`, `Burst`/`BurstDelays`, `Projectile` visuals/turn behaviour,


  `Concrete`, glow, smudges, shield-hit, air/water effects, and the mixed


  Demolition/Flame/Flak/MissileAP warhead contributions on the tower.

- Verification:


  - `scratchpad/verify_ixian.py` (equivalent to `tools/audit/review_resolve_diff.py`)


    OK for both weapons


  - `tools/audit/effect_audit.py` → 0 duplicate `DamagesConcrete`


  - `tools/audit/find_empty_warhead.py` → 0 empty warheads


  - `tools/audit/find_orphan_old_keys.py` → 0 real bugs


  - `tools/audit/audit_warhead_split.py` at/below baseline (952)


  - `tools/audit/audit_balance_drift.py` → 32 ledgers clean (re-extracted via `extract_stats.py`)


  - `launch-game.cmd` reached `MenuPostProcessEffect.PostWorldLoaded`; no new `exception-*.log`





- **Post-audit correction:** `tools/audit/review_resolve_diff.py` compared


  the core behavioural invariants, but a full resolved-vs-baseline diff


  (`scratchpad/compare_full.py`) showed the per-weapon `Versus` and warhead


  overrides still lived inside the weapon nodes. Restructured the two Ixian


  weapons so every `Versus` row lives in dedicated D2K Shared


  `^Warhead_*_D2K_TowerMissile` / `^Warhead_*_D2K_mtank_pri2` templates (with all


  plating rows present, missing ones at the 100% default), and the weapon nodes


  only carry `Inherits@wh`/`Inherits@wh2`/`Inherits@wh3` (and `@wh4` for the


  tower) plus `Inherits@proj`/`Inherits@fx`. This eliminates the `-Key:` removal


  hacks while preserving the resolved baseline exactly. Re-extracted all balance


  ledgers (`extract_stats.py`) and re-ran `audit_balance_drift.py` (clean).




## 2026-08-23 — D2K Rocket Trooper family 3-way split (boot-gated)





- Converted `D2K_Rocket_Trooper` (`mods/cameo/weapons/d2k.yaml`),


  `D2K_Rocket_Trooper1`/`D2K_Rocket_Trooper2` (`mods/cameo/ContentPacks/D2k/Ixian/yaml/weapons.yaml`),


  and `D2K_Rocket_Trooper_AA`/`D2K_Rocket_Trooper_AGOnly` (`mods/cameo/ContentPacks/D2k/Ordos/yaml/weapons.yaml`)


  from the old `Inherits: ^D2KRocket` / `Inherits: ^D2K_Cannon` full-stack pattern to explicit


  `Inherits@wh` / `Inherits@proj` / `Inherits@fx`.

- Removed legacy full-stack inherits (`^D2KRocket`, `^D2K_Cannon`). The triple-warhead


  Rocket Troopers were added to the `docs/design/WEAPON_3WAY_SPLIT.md` exception


  allow-list because their resolved damage identity requires three warhead layers.

- Added six D2K Shared templates in `mods/cameo/ContentPacks/D2k/Shared/yaml/weapons.yaml`:


  `^Projectile_Missile_Medium_D2K_Rocket_Trooper`,


  `^Projectile_Missile_Light_D2K_Rocket_Trooper1`,


  `^Projectile_Missile_Light_D2K_Rocket_Trooper_AA`,


  `^Projectile_Grenade_Light_D2K_Rocket_Trooper2`,


  `^Projectile_Grenade_Light_D2K_Rocket_Trooper_AGOnly`,


  and `^Effect_MissileAP_Heavy_D2K_Rocket_Trooper`.

- Preserved `Damage`, `Versus`, `Spread`, `ReloadDelay`, `Range`, `Report`, `ValidTargets`,


  `Projectile` visuals/turn behaviour, `Concrete`, glow, smudges, shield-hit, air/water


  effects, and the mixed Demolition/Railgun/Cannon warhead contribution on Trooper2/AGOnly.

- Verification:


  - `tools/audit/review_resolve_diff.py` OK for all five weapons


  - `tools/audit/effect_audit.py` → 0 duplicate `DamagesConcrete`


  - `tools/audit/find_empty_warhead.py` → 0 empty warheads


  - `tools/audit/find_orphan_old_keys.py` → 0 real bugs


  - `tools/audit/audit_balance_drift.py` → 32 ledgers clean (re-extracted via `extract_stats.py`)


  - `launch-game.cmd` reached `MenuPostProcessEffect.PostWorldLoaded`; no new `exception-*.log`





## 2026-08-23 — Documentation review + doc_claims reconciliation





- Completed a full discrepancy review of design/instruction/audit documents


  (`docs/research/doc_review.md` generated for inspection).

- Reconciled `docs/audit/doc_claims.yaml` with live measurements:


  `multi_main_fired_weapons` 975→939, `meters_filling_before_death` 118→122,


  `corrosion_meter_actors` 783→785, `w24_multi_main_fed` 386→385,


  `physical_state_fired_weapons` 449→450.

- `python tools/audit/audit_doc_claims.py` now passes (16/16 claims clean).

- Updated `docs/design/ROADMAP.md` to reflect live W2 status (`^LightFlameWeapon`


  still has 28 inheritors, not ready/done) and current generator drift


  (`verify_generator_sync.py` reports drift = 10 + `^Warhead_Sniper_Light` not emitted).

- Identified next D2K 3-way split targets after `DevBullet`/`PlasBullet`:


  `D2K_Rocket_Trooper` family (in progress by subagent) and Ixian giant multi


  (`D2K_TowerMissile`, `mtank_pri2` in


  `mods/cameo/ContentPacks/D2k/Ixian/yaml/weapons.yaml`).

- Outstanding cross-cutting drift (not D2K): `tools/balance/verify_generator_sync.py`


  reports 9 chemical warhead blocks out of sync with `gen_weapon_template.py`


  (`PhysicalStates` vs `PhysicalStateName`, `Corrosion` scale, `TiberiumDeath`


  vs `ExplosionDeath`). Pending maintainer/generator alignment before splicing.







## 2026-08-20 — D2K Devastator/Plasma cannon 3-way split (boot-gated)





- Converted `DevBullet` and `PlasBullet` in `mods/cameo/weapons/d2k.yaml` from the old


  `Inherits: ^D2K_Cannon` / `Inherits: DevBullet` pattern to explicit


  `Inherits@wh` / `Inherits@proj` / `Inherits@fx`.

- Added `^Warhead_CannonHE_Heavy_D2K_DevBullet`, `^Projectile_Shell_Heavy_D2K_DevBullet`,


  and `^Effect_CannonHE_Heavy_D2K_DevBullet` in


  `mods/cameo/ContentPacks/D2k/Shared/yaml/weapons.yaml`.

- Preserved `Spread: 666`, `Damage: 80000`, `Versus`, `DamageTypes`, `HealthPercentageDamage`,


  `Concrete: 3333`, `Glow`, `d2k_shockwave` impact sound/animation, `Projectile` speed/image,


  `Range`, `ReloadDelay`, `Report`, and all `EffectAir`/`EffectWater`/shield/smudges.

- Fixed the duplicate ground effect: the old `Warhead@3Eff: d2k_shockwave` and inherited


  `Warhead@Effect: d2k_small_napalm` were merged into a single `Warhead@Effect: d2k_shockwave`


  with `ValidTargets: Ground, Ship`.

- `PlasBullet` now shares the same three D2K Shared layers, overriding `ReloadDelay`,


  `Projectile` speed/image, and main warhead `Damage`/`Spread` only.

- Regenerated `d2k_harkonnen` balance ledger and derived sidecar.

- Verification:


  - `tools/audit/effect_audit.py` → 0 duplicate `DamagesConcrete`


  - `tools/audit/find_empty_warhead.py` → 0 empty warheads


  - `tools/audit/find_orphan_old_keys.py` → 0 real bugs


  - `tools/audit/audit_balance_drift.py` → 32 ledgers clean


  - `launch-game.cmd` reached `MenuPostProcessEffect.PostWorldLoaded`; no new


    `exception-*.log`





## 2026-08-22 — W24 cluster 9: D2K-rocket six-weapon split (boot-gated)





- Converted `GoliathRockets_AA`, `WraithRockets_AA`, `SunDogRockets`, `MissileTurret` (`mods/cameo/ContentPacks/StarCraft/Terran/yaml/weapons.yaml`), `ScoutRockets_AA` (`mods/cameo/ContentPacks/StarCraft/Protoss/yaml/weapons.yaml`), and `HeavyOrdosCombatTankRockets` (`mods/cameo/ContentPacks/D2k/Ordos/yaml/weapons.yaml`) to the single `^D2KRocket` archetype.

- Removed `^Chaingun`, `^FlakWeapon`, `^LightMissile`, `^MediumMissile` inherits and their old main/percentage warheads.

- Collapsed five identical damage warheads per weapon into one `Warhead@MissileAP_Heavy` with totals 30000/10000/10000/20000/10000/10000 and percentage twins 15/5/5/10/5/5.

- Preserved `Range`, `ReloadDelay`, `Report`, `ValidTargets`, `Burst`/`BurstDelays`, local `Projectile` overrides (including Wraith/HeavyOrdos `ContrailStartColor`/`ContrailEndColor` and launch angles), and restored the flak-bullet contrail visual fields (`ContrailZOffset`, `ContrailStartColor`, `ContrailEndColor`, `ContrailStartWidth`, `ContrailEndWidth`) as local overrides because `^Projectile_Missile_Heavy` drops them.

- Added local `Warhead@EffectWater: CreateEffect` (`Explosions: small_splash`) on all six because `^D2KRocket` (via `^Effect_MissileAP_Heavy`) does not define a water effect.

- Lowered `BROADCAST_BASELINE` in `tools/audit/audit_warhead_split.py` from 958 to 952.

- Regenerated balance ledgers and derived sidecars for affected factions (`d2k_ordos`, `starcraft_protoss`, `starcraft_terran`).

- Regenerated `docs/audit/latest/phase_b_survey.md`.

- Verification:


  - `tools/audit/review_resolve_diff.py` OK for all six


  - `find_empty_warhead.py` = 0


  - `find_orphan_old_keys.py` = 0 real bugs


  - `audit_warhead_split.py` at/below baseline (952)


  - `audit_physical_state_warheads.py` PASS


  - `audit_balance_drift.py` clean


  - `sweep_areadamage.py` dry-run no cluster changes


  - `extract_stats.py` clean


  - `launch-game.cmd` reached `MenuPostProcessEffect.PostWorldLoaded`; no new `exception-*.log`





## 2026-08-22 — W24 cluster 5: Tiberian Sun tiberium bazookas (boot-gated)





- Converted `TSTibBazooka` (Nod) and `TSChemBazooka` (Forgotten) to the 3-way split


  using `^Warhead_MissileAP_Light`, `^Projectile_Missile_Light`, `^Effect_MissileAP_Light`.

- Removed old `^LightChemicalWeapon` and `^LightMissile` inherits.

- Collapsed `6000` chemical + `24000` missile damage into one `Damage: 30000` main and


  `3` + `12` percentage into a single `Damage: 15` percentage warhead.

- Preserved the `Corrosion` physical state by keeping `PhysicalStateName: Corrosion` and


  scaling the amount to the merged warhead (`PhysicalStateScale: 20`) so the post-armor


  corrosion matches the old 6000-damage chemical contribution.

- Preserved ally-damage proportion with `FriendlyFireDamage: 90` on both main and


  percentage warheads.

- Preserved `spittrail` missile trail, `small_poof` ground effect, `med_explosion_air`


  air effect, `Concrete: 100`, shield-hit duration 6, and all smudges.

- Kept `TSChemBazooka`'s `SpawnSmokeParticle` cloud warhead.

- Fixed an attempted `-Warhead@EffectWater:` removal that failed because


  `^Effect_MissileAP_Light` does not define that key.

- Regenerated balance ledgers and derived sidecars for affected factions.

- Updated `docs/design/BALANCE_PROGRAM_PLAN.md` W24 status line.

- Verification:


  - `find_empty_warhead.py` = 0


  - `find_orphan_old_keys.py` = 0 real bugs


  - `audit_warhead_split.py` at/below baseline


  - `audit_physical_state_warheads.py` PASS


  - `audit_balance_drift.py` clean


  - `review_resolve_diff.py` OK for both weapons


  - `launch-game.cmd` reached `MenuPostProcessEffect.PostWorldLoaded`; no new


    `exception-*.log` after the fix.




## 2026-08-22 — W24 cluster 4: Dragon SAM (boot-gated)





- Converted `Dragon` in `mods/cameo/ContentPacks/TiberianDawn/Nod/yaml/weapons.yaml` to the


  3-way split using `^Warhead_MissileAA_Heavy`, `^Projectile_Flak_Heavy`, `^Effect_Flak_Heavy`.

- Removed old `^HeavyAAWeapon`, `^HeavyMissile`, and `^ImpactGlow` inherits; moved the


  `GlowImpact` warhead into the local effect layer.

- Preserved the homing `Missile` projectile with `Image: MISSILE`, `TrailImage: smokey`,


  inaccuracy 150, speed 500, launch/turn behavior, and the AA-only `ValidTargets: Air`.

- Collapsed two 6000-damage warheads into one `Damage: 12000` main and `Damage: 6`


  percentage, preserving `ValidRelationships: Neutral, Enemy`.

- Preserved `big_frag` / `small_building` / `small_splash` impact effects, shield-hit


  duration 10, concrete damage 200, and all smudge behavior.

- Lowered `BROADCAST_BASELINE` in `tools/audit/audit_warhead_split.py` from 972 to 970.

- Regenerated balance ledgers and derived sidecars for affected factions.

- Updated `docs/design/BALANCE_PROGRAM_PLAN.md` W24 status line.

- Verification:


  - `find_empty_warhead.py` = 0


  - `find_orphan_old_keys.py` = 0 real bugs


  - `audit_warhead_split.py` at/below baseline


  - `audit_balance_drift.py` clean


  - `review_resolve_diff.py` OK for `dragon`


  - `launch-game.cmd` reached `MenuPostProcessEffect.PostWorldLoaded`; no new


    `exception-*.log`.




## 2026-08-22 — W24 cluster 3: FutureTech missile javelins (boot-gated)





- Converted `FutureJavelinRockets`, its children (`_elite`, `Deployed`, `Deployed_elite`),


  and `Future_MultiMissile_Javelin` to `^Warhead_MissileAP_Light` with the 3-way split.

  Removed old `^LightMissile`, `^FlakWeapon`, `^MediumMissile`, `^ShrapnelWeapon`, and


  `^D2KRocket` inherits. Preserved resolved `d2k_RPG` projectile image/trail, `ROCKET1.WAV`


  report, ranges, reload delays, burst offsets, and all impact effects.

- Collapsed five duplicate damage warheads per weapon into one `Damage: 10000` main and a


  single `Damage: 5` percentage warhead.

- Lowered `BROADCAST_BASELINE` in `tools/audit/audit_warhead_split.py` from 977 to 972.

- Regenerated balance ledgers and derived sidecars for affected factions.

- Updated `docs/design/BALANCE_PROGRAM_PLAN.md` W24 status line.

- Verification:


  - `find_empty_warhead.py` = 0


  - `find_orphan_old_keys.py` = 0 real bugs


  - `audit_warhead_split.py` at/below baseline


  - `audit_balance_drift.py` clean


  - `review_resolve_diff.py` OK for all five weapons


  - `launch-game.cmd` reached `MenuPostProcessEffect.PostWorldLoaded`; no new


    `exception-*.log`.




## 2026-08-22 — W24 cluster 2 + weapon-family corrections (boot-gated)





- Corrected `wc2cannontowerFire` to `CannonHE_Heavy` and `wc2dragonFireVisible` to


  `Flame_Heavy` after maintainer review; preserved resolved projectile/effect behaviour.

- Converted W24 cluster: `SporemawShoot`, `wc2demolitionsquadExplode`,


  `wc2mageFireballVisible`/`wc2mageFireballExplosion`, and child `wc2ogremageRunes_Hit`


  to `^Warhead_CannonAP_Light` with one warhead, one projectile, and one effect inherit.

- Moved Protoss `Inherits@corr: ^Corrodible` into `^LargeProtoss` and removed six


  redundant per-unit corrosion inherits (dragoon/archon now covered).

- Lowered `BROADCAST_BASELINE` in `tools/audit/audit_warhead_split.py` from 981 to 977.

- Regenerated balance ledgers and derived sidecars for affected factions.

- Updated `docs/design/BALANCE_PROGRAM_PLAN.md` W24 status line.

- Verification:


  - `find_empty_warhead.py` = 0


  - `find_orphan_old_keys.py` = 0 real bugs


  - `audit_warhead_split.py` at/below baseline (977)


  - `audit_balance_drift.py` clean


  - `review_resolve_diff.py` OK for all cluster weapons; `wc2ogremageRunes_Hit` intentionally


    collapsed from 10 inherited damage warheads + 1 child warhead to a single `Damage: 11250`


    main (expected Damage multiset flag).

  - `launch-game.cmd` reached `MenuPostProcessEffect.PostWorldLoaded` with no new


    `exception-*.log`.




## 2026-08-21 — Cryo/Inferno promoted to blend families (package 3)





- `tools/balance/gen_weapon_template.py`:


  - Removed `Cryo` / `Inferno` from `INHERIT_FAMILIES`.

  - Added `Cryo` = Laser×Prism and `Inferno` = Flame×Prism to `BLEND_FAMILIES`.

  - Updated `COMPOSITION` (`Cryo` energy 0.55 / thermo 0.25 / kinetic 0.20) and


    `COMPOSITION_OVERRIDE` (`Inferno` thermo 0.65 / energy 0.35).

  - Updated `PHYSICS_RANK` (`Cryo` 0.75, `Inferno` 0.57) and the blend-header comment.

  - Fixed blend header to print `no PhysicalStates` for empty state maps.

- Regenerated all 97 `^Warhead_*` templates in `mods/cameo/weapons/weapons.yaml`


  via `splice_templates.py --all`; `verify_generator_sync.py` reports drift = 1


  (the pre-existing hand-authored `^Warhead_Sniper_Light` only).

- Regenerated 32 balance ledgers and derived sidecars with `extract_stats.py`.

- Updated `docs/design/PHYSICAL_STATE_SYSTEM.md`, `docs/design/ARMOR_LAYERS.md`,


  and `docs/design/BALANCE_PROGRAM_PLAN.md` to reflect the new family model.

- Verification: `extract_stats.py --check` 0 drift; `audit_balance_drift.py` clean;


  `audit_physical_state_warheads.py` PASS; `audit_armor_upgrade_harm.py` clean;


  `test_plating_composition.py` 10/10; `test_physical_state_price.py` 17/17;


  `find_empty_warhead.py` 0; `find_orphan_old_keys.py` 0 real bugs.

- Boot-gate: `launch-game.cmd` reached `MenuPostProcessEffect.PostWorldLoaded`,


  `exception-*.log` count 183 → 183 (no new exceptions).




## 2026-08-20 — Computed prerequisite-chain tech tier





- Added `tools/balance/tier_chain.py` with `TierChain(model)` resolving buildable


  prerequisites to a total building-chain cost `C`, restricted to the actor's


  own ContentPack leaf plus the same game's `Shared` pack. Cheapest valid provider


  selected per token; buildings deduplicated across branches; cycles are broken.

- `TierChain` indexes `Building` actors with `Valued.Cost` and both their actor


  name and `ProvidesPrerequisite` tokens as providers.

- `tools/balance/formula.py` now exports `TIER_B` (9500.0), `TIER_S` (8250.0),


  and `tier_multiplier(C)`. Docstrings updated to distinguish absolute


  (`class_anchor_price`) and relative (`class_baseline_price`) usage.

- `tools/balance/extract_stats.py` attaches `tier_chain_cost` and `tier_multiplier`


  to each buildable actor's `_derived` blob; manual `design.tech_tier` values are


  never overwritten.

- `tools/balance/fit_class.py` uses the absolute tier in `unit_inputs()`, preferring


  a manual `design.tech_tier` and falling back to the derived `tier_multiplier`.

- `tools/balance/propose_class_rebalance.py` computes per-class relative tier


  `f(C)/f(C_anchor)` for `class_baseline_price`; the anchor's manual `tech_tier`


  is used as the denominator when present.

- `tools/balance/build_workbook.py` writes the absolute `TechTier` to the


  spreadsheet and divides by the anchor's absolute tier inside the class-baseline


  `Price` and `RangeSolve` formulas.

- `tools/balance/check_band.py` loads derived sidecars, computes absolute unit


  tier, and uses the relative tier for `class_baseline_price` while keeping the


  absolute tier for `class_anchor_price`.

- Regenerated all 32 raw ledgers and 32 derived sidecars with `extract_stats.py`.

- Verified: `td_nod_lasertrooper` → `tier_chain_cost = 27000.0`, `tier_multiplier =


  0.3204`; its closure contains only Nod and Shared buildings (no GDI).

- `extract_stats.py --check` reports 0 drifted; `audit_balance_drift.py` is clean.

- `build_workbook.py` and `propose_class_rebalance.py --class mbt` run without


  errors; `fit_class.py --class scout --anchor naxis_naxiriflesoldier` produces


  a candidate and was reverted so `class_anchors.json` is unchanged.

- Updated `docs/design/RESEARCH_NOTES.md`, `docs/design/ROADMAP.md`, and this log.

- Building-plug addons (`Plug:` trait) are not counted as separate actor-name


  providers, so `wc2_orcs_deathknight` resolves to $15,000 (Great Hall +


  Temple of the Damned) rather than double-counting the Fortress upgrade plug.




## 2026-08-19 — Delivery-weighted physical-state price multiplier wired into fit_class





- `tools/balance/extract_stats.py` now imports `physical_state_price` and calls


  `physical_state_price.actor_multipliers(rs)` once per extraction pass. The resulting


  per-actor record (`physical_state_weight`, `physical_state_multiplier`,


  `physical_state_weapon`) is attached to the actor's `_derived` blob and lifted into


  `docs/balance/derived/*.json` by `split_derived()`.

- `tools/balance/fit_class.py` now applies `formula.physical_state_price_multiplier()`


  in `price_unit()`, using the derived sidecar weight. The helper `physical_state_weight()`


  checks `u["_derived"]`, then the sidecar `du`, then the raw unit, defaulting to 0.

- Regenerated all 32 ledgers and derived sidecars (`extract_stats.py`).

- Verified with `fit_class.py --class line_breaker --anchor td_nod_flametank --use-k`:


  the anchor prices at **1000** against an actual cost of **800** (+25%), matching the


  full E2 ceiling. Non-state anchors (e.g. `mbt` / `tiger.nax`) price at cost0 with no


  surcharge.

- `find_empty_warhead.py` = 0; `audit_physical_state_warheads.py` PASS.

- Updated `docs/design/PHYSICAL_STATE_SYSTEM.md` and `docs/design/ROADMAP.md`.




## 2026-08-18 — ApplyPhysicalState → damage-scaled conversion (flame/chemical, boot-gated)





- Implemented `tools/balance/convert_apply_to_scaled_v2.py` (dry-run by default,


  `--apply` required, block-aware/line-based, no regex, preserves BOM/line endings,


  reports standalone cases).

- Converted legacy templates `^LightFlameWeapon`, `^MediumFlameWeapon`,


  `^HeavyFlameWeapon`, `^LightChemicalWeapon`, `^MediumChemicalWeapon`,


  `^HeavyChemicalWeapon` and all concrete overrides in 34 YAML weapon files:


  - `SpreadDamage` → `AreaDamage`


  - `HealthPercentageDamage` → `AreaDamagePercentage`


  - removed `Range:` from inside converted warheads


  - main warhead: `ValidRelationships: Ally, Neutral, Enemy`,


    `FriendlyFireDamage: 50`, `FriendlyFireSpread: 50`


  - main + percentage warheads: `PhysicalStateName` / `PhysicalStateScale`


    (`Temperature`/`300` for flame, `Corrosion`/`300` for chemical)


  - removed associated FriendlyFire twins and fixed `ApplyPhysicalState` warheads.

- Removed two stale `-Warhead@PhysicalStateMediumFlameWeapon*` removal lines in


  `mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml` that became invalid


  after the template physical-state warheads were removed.

- Verification:


  - `python tools/audit/audit_physical_state_warheads.py` PASS


  - `python tools/audit/find_empty_warhead.py` = 0


  - `utility.cmd cameo --check-yaml` completed without fatal YAML exceptions


    (pre-existing actor/condition warnings unrelated to this change)


  - `launch-game.cmd` reached the main menu (`MenuPostProcessEffect.PostWorldLoaded`


    in `%APPDATA%/OpenRA/Logs/perf.log`; no new `exception-*.log` after the run).

- Standalone `ApplyPhysicalState` cases left untouched: 43 non-target (cryo/non-family)


  blocks reported by the conversion script; flame/chemical `ApplyPhysicalState`


  warheads were removed.

- Note: `tools/audit/audit_physical_state_warheads.py` already expects


  `PhysicalStateScale: 300` in the working tree; do not commit without reviewing


  that diff.







## 2026-08-17 — RA2 effect-template final sweep (Shared/Allies/Yuri/redalert2mod/AsianAlliance/Syndicate, boot-gated)





- Completed the final `ra2_*` inline-effect sweep in the loaded RA2 tree


  (`mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml`,


  `mods/cameo/ContentPacks/RedAlert2/Allies/yaml/weapons.yaml`,


  `mods/cameo/ContentPacks/RedAlert2/Yuri/yaml/weapons.yaml`,


  `mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/weapons.yaml`,


  `mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/weapons.yaml`,


  `mods/cameo/weapons/redalert2mod.yaml`).

- Removed the unused `^Effect_Disk_Ray_RA2` template.

- Updated `^Effect_Psi_Wave_RA2` with `ImpactActors: false` and `AffectsParent: true`


  and wired `PsiWaveX` to it.

- Wired `IonPulseDischarge` to `^Effect_Emp_Fx_RA2` and `ChronoshiftImpact` to


  `^Effect_Chrono_Fd_RA2`, preserving their secondary/glow/distortion warheads.

- Converted `NaxisBlackBomb`, `AsianOilBomb`, and `RA2FreedomAK47` to the


  appropriate `^Effect_*_RA2` inherits.

- Cleaned redundant local `Warhead@Effect` / `-ImpactSounds` blocks from


  `RA2MirageGun` and `RA2HeavyMirageGun`.

- Ensured `RA2PsychicJab` `Inherits@fx` is the last inherit.

- Simplified `DredMissile` and `YRBoomerSCUD` water-effect overrides (removed


  the `gexpwala` typo sound, kept `ImpactActors: false`).

- Fixed `LatinBuggyRocket` and `AsianSmallOilBomb` to a single winning


  `Inherits@fx`.

- Boot crash on `^Effect_Tesla_Impact_RA2` / `^Effect_Tesla_Heavy` circular


  inheritance was fixed by inlining the `^Effect_Tesla_Heavy` `EMPUnit` and


  `ShieldHit` warheads into `^Effect_Tesla_Impact_RA2`, `^Effect_Ion_Ring_RA2`,


  and `^Effect_Psi_Wave_RA2` instead of inheriting them.

- Verification: `find_empty_warhead.py = 0`, `audit_empty_warheads.py = 0`,


  `extract_stats.py` clean, `audit_balance_drift.py` clean,


  `audit_effect_warhead_names.py` 0 violations, `check_effect_audio.py` OK,


  `launch-game.cmd` reached the main menu


  (`MenuPostProcessEffect.PostWorldLoaded` in `perf.log`; no new


  `exception-*.log` after the successful run). One stale exception log from


  the pre-fix boot remains (`exception-2026-08-17T161444Z.log`).

- `python tools/audit/run_all.py` still exits 1 on pre-existing failures


  (`audit_inherits`, `audit_upgrades`, `audit_fluent`, `audit_basebuilder_crates`,


  `audit_buildable_order`, `audit_weapon_suffixes`, `audit_warhead_split`);


  these are unrelated to this effect wiring and pre-date the current sweep.

- Remaining: `SCTyr` in `StarCraft/Terran/yaml/weapons.yaml` still has a


  three-explosion `ra2_*` list with no matching single RA2 template; the


  legacy `mods/cameo/weapons/redalert2.yaml` is excluded from the loaded tree.




## 2026-08-17 — RA2 sprite-named effect template library (foundation + shared/Soviets wiring, boot-gated)





- Generated a complete `^Effect_<family>_<size>_RA2` template library for the


  54 `ra2_*` effect sequences in `mods/cameo/sequences/misc.yaml` and inserted


  it into `mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml`.

- Replaced the old `^Effect_MissileHE_Medium_RA2` with the new


  `^Effect_Explosion_Large_RA2`.

- Wired the shared RA2 weapon stacks to the new templates:


  `^RA2FlakWeapon`, `^RA2LightMissile`, `^RA2MediumMissile`,


  `^RA2HeavyMissile`, `^RA2TankDestroyerCannon`, `^RA2MediumCannon`,


  `^RA2HeavyCannon`, `^RA2Grenade`, `^RA2TeslaWeapon`, `^RA2RailgunWeapon`,


  `^RA2EliteEffects`, `RA2UnitExplode`, `RA2UnitExplodeBig`,


  `RA2BuildingExplode`, `KirovExplode`, `RA2LargeDebris`, `RA2Terrorist`.

- Wired `RA2RTruckRocket` in `mods/cameo/weapons/redalert2mod.yaml`.

- Began Soviets concrete cleanup: `RA2TURRETFLAKAA`, `SeaScorpion_AA`,


  `RA2FLAKAA`, `RA2FlakTrackAAGun`, `RA2KirovBomb`, `RA2KirovBomb_tesla`,


  `RA2120xmm`, `RA160mmE_fire_elite`, `RA160mmE_tesla_elite`,


  `RA2UnitExplodeSmall`.

- Verification: `find_empty_warhead.py = 0`, `extract_stats.py` clean,


  `audit_balance_drift.py` clean, `launch-game.cmd` reached the main menu


  (`MenuPostProcessEffect.PostWorldLoaded` in `perf.log`, no new


  `exception-*.log`).

- Remaining: wire Allies/Yuri/redalert2mod/Shared concrete weapons that still


  have inline `Explosions: ra2_*`; sweep RA2Atomic nuke-ball and Lightning


  Storm ion-ring effects; run `review_resolve_diff.py`; full audit suite has


  pre-existing failures unrelated to this change.




## 2026-08-17 — RA2 effect template sweep continuation (Shared/redalert2mod/Yuri, Floating Disk, boot-gated)





- `mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml`:


  - `RA2Atomic` now uses `Inherits@fx: ^Effect_Nuke_Ball_RA2`; removed local


    `Warhead@Effect`, kept radiation warhead.

  - `^Effect_Ion_Ring_RA2` updated to inherit `^Effect_Tesla_Heavy` and added


    `ImpactActors: false`; `LightningStormDamage` now `Inherits@fx:` from it,


    preserving both `SpawnSmokeParticle` warheads.

  - Added `Warhead@EffectAir` to `^Effect_Tesla_Impact_RA2` and wired


    `TeslaArmorDischargeDummy` to it, removing its local effect blocks.

  - Wired remaining concrete weapons to RA2 effect templates:


    `RA2HoverMissile_elite`, `RA2ThunderboltMissile_elite`,


    `RA2MultiHoverMissile_elite`, `RA2MultiThunderboltMissile_elite`,


    `RA2DroneSparks`, `MigMissiles_fire`, `MigMissiles_tesla`, `RA2SCUDELITE`,


    `RA2DepthCharge` (added `^Effect_Depth_Charge_RA2`).

  - Added `-ImpactSounds:` to `^Effect_Init_Fire_RA2`.

- `mods/cameo/weapons/redalert2mod.yaml`:


  - Wired `AsianHowitzerSplash`, `AsianFlameFragment`, `AsianFlamerTurret`,


    `SteelHoverMissile_elite`, `MeteorFlameFragment` to RA2 effect templates.

- `mods/cameo/ContentPacks/RedAlert2/Yuri/yaml/weapons.yaml`:


  - Wired `RA2PsychicJab` to `^Effect_Init_Fire_RA2`.

- Floating Disk muzzle:


  - Added `^RA2DiskMuzzle` in `ContentPacks/RedAlert2/Shared/yaml/sequences.yaml`


    with a `ra2_diskray` sequence.

  - `yuri_floatingdisk` now `Inherits: ^RA2DiskMuzzle` and overrides


    `ra2_diskray` with `Scale: 0.9`, `Offset: 0,35`, `Tick: 100`.

  - `Armament@SECOND` and `Armament@Steal` in


    `ContentPacks/RedAlert2/Yuri/yaml/aircraft.yaml` now use


    `MuzzleSequence: ra2_diskray`.

- Skipped weapons already inheriting wired RA2 stacks (e.g., `^RA2MediumMissile`,


  `^RA2Grenade`, `^RA2TankDestroyerCannon`) and edge cases left for maintainer


  review: `DredMissile`, `NaxTorpTube` (custom water sound + wired parent),


  `NaxiMeteor` (glow fields), `MigMissiles_rad` (sprite `ra2radbang` not


  matching the `ra2_*` underscore convention).

- Verification: `find_empty_warhead.py = 0`, `extract_stats.py` clean,


  `audit_balance_drift.py` clean, `launch-game.cmd` reached main menu


  (`MenuPostProcessEffect.PostWorldLoaded` in `perf.log`, no new


  `exception-*.log`). `python tools/audit/run_all.py` still reports the same


  pre-existing failures as the prior session.




## 2026-07-18 — BALANCE PIPELINE LIVE (all agents read this)





**NEW LAW: never hand-edit balance numbers in yaml.** The pipeline is


implemented and enforced (`docs/design/BALANCE_PIPELINE.md`, CLAUDE.md


"Balance changes" section, DESIGN §12):


extract_stats.py → docs/balance/*.json (raw-stat ledger, committed) →


build_workbook.py → cameo_balance_v2.xlsx (gitignored workbench) →


import_workbook.py → apply_balance.py --confirm (maintainer order) →


re-extract, audits, boot, commit yaml+ledger together.

`audit_balance_drift` in run_all fails RED whenever yaml and ledger


disagree — hand edits cannot land silently anymore.

Loop PROVEN: exact fixed point + live 1000→1050→1000 round trip.

Phase 5 (per-class anchors via fit_class.py + class_anchors.json)


awaits maintainer anchor picks; the fixed-point test also exposed and


fixed an order-dependent resolver-cache-poisoning bug in


tools/audit/miniyaml.py that affected ALL resolved-value audits.




## 2026-07-18 — Claude session (TKM port + Blackrobe batch)





- TKM CONTRIBUTOR PORT (`3bb6a34b3`): full-repo zip from a community


  contributor analyzed (base = cea431010 with pre-rename-id payload),


  translated through the applied rename_map_tkm, per-actor 3-way


  merged into the pack. Arsenal-tree redesign, GP-25 replaces M203,


  Berezka speed/cloak, engineer field kits, new weapons + warhead .cs


  (DLLs rebuilt). Deviations flagged in the commit (kept warfactory


  ProvidesPrerequisite — his removal would orphan every


  ~tkm_warfactory prereq).

- TKM MOVED into ContentPacks/RedAlert2Mod (`d981d65fe` renames +


  `915714fe8` manifest/mod.yaml — the renames rode the earlier commit


  via the staged index; completion committed immediately). Theme


  folder rename POSTPONED (Blackrobe) — candidates logged in ROADMAP.

- Monster tank Tesla/Thermonuclear rockets (`d981d65fe`): real weapon


  swaps (mammoth logic) replace the imperceptible +10% multipliers;


  duplicate ActorStatValues fixed earlier in `71765570b`.

- Survival (`e8af695eb`): superlinear ramp, wave-size floor (dip fix),


  veteran waves; win-objective fix earlier in `71765570b`. `survival 2`


  copy was deleted by the team (`32669f345`) — main copy carries all.

- SM passive income (Blackrobe): moondairyfarm verified correctly


  wired; the missing piece (ra2oilderrick/ra2ywall conyard provisions)


  is the MAINTAINER'S OTHER SESSION's uncommitted WIP — do not


  double-fix. Laser Beetle/M200B report: wiring verified WAD


  (replacement promotions retire them); if the REPLACEMENTS don't


  appear despite bought promotions, check rank1 granting in-game.

- NEXT: FULL SM REBALANCE (ROADMAP P1, sheet-first, workbook free).




## 2026-07-17 — Claude session SID-20260717-cl4b7e (RA1 legacy rename + two-session repair pass)





**Landed (commits `fdd466494`, `4cf7e6909` + this session's repair commit):**


- RA1 LEGACY-ID RENAME complete: all 52 old-style ids (RAE1, PT/DD/CA,


  SS/MSUB, POWR/APWR/RASILO, BADR family, naval yards, civilians, husks,


  8 upgrade proxies) → grammar-compliant ids; only `japan` unprefixed.

  Applied by tools/rename/apply_ra1_legacy.py (context-scoped successor


  to apply.py). zerofighter collision → japan_zerofighter_slave.

- Umlaut transliteration (schwarzermond_ubermensch), CABAL plasmaturret


  buildable + mobilestealthgenerator removed, stale RA1 monoliths deleted.

- REPAIR PASS after two-session collision (this entry's second half):


  1. 13 explicit `actor_<oldid>.description/.name` yaml refs broke when


     ftl keys renamed (whole-identifier pass can't see through the


     `actor_` prefix) — added a fluent-stem pass to the applicator


     (combined-alternation regexes; 52 sequential re.subs was too slow)


     and fixed all 13. audit_fluent: 17 → 0 unresolved.

  2. warcraft2_en.ftl + tkm_en.ftl were NEVER registered in mod.yaml


     FluentMessages — WC2/TKM faction descriptions showed raw keys.

     Registered both.

  3. 19 audit reports in docs/audit/latest/ were UTF-16-corrupted by a


     concurrent session's PowerShell `>` redirect (10 committed


     corrupted). Regenerated the whole suite via bash run_all.sh (UTF-8).

     Lesson saved to agent memory.

- Verification: full audit suite green (fluent 0 unresolved, consistency


  73/0, packs P2 = known D2k suffix-style backlog only), resolver spot


  checks green (3913 actors / 2365 weapons, zero old ids), FACTIONS.md


  clean of old ids, boot gate to main menu.

- SM promotion grid: implemented by the concurrent session in


  SchwarzerMond/yaml/promotions.yaml with CABAL-pattern gating BUT the


  chains deviate from the maintainer's image; row order under redesign —


  see ROADMAP P2 (sharpened 2026-07-17 with maintainer's MARS/tier


  clarifications + reshuffle proposal). DO NOT touch the grid before the


  maintainer picks an option.

- NOTE for all agents: SCUD/SCUDNUKE (RedAlert/Soviets weapons.yaml) are


  legacy-uppercase WEAPON ids shared with generals/darkreign — WPN-MIGRATE


  scope, intentionally untouched by the actor rename.

- SM PROMOTION GRID FINALIZED (maintainer decision): columns


  infantry | vehicles | air/artillery/support, tier-laddered rows —


  see ROADMAP P2 (RESOLVED) for the binding table. promotions.yaml


  re-chained, `..._promotion_bermensch` → `..._promotion_ubermensch`,


  ^PromotionUnitBuff stripped from 10 non-promotion SM units


  (FutureTech convention: grid units only). Boot green.

- NEW ORDER: FULL SM REBALANCE (sheet-first; post-buff-strip stats;


  38 stat_formulas findings as the seed) — queued as ROADMAP P1.




## 2026-07-16





**Task:** Diagnose ACP connection issue with Claude.

**Done:**


- Confirmed ACP refers to Agent Client Protocol; Claude integration is typically via `claude-agent-acp` / `claude-code-acp` or inside Devin Desktop/Windsurf/Zed/JetBrains.

- Checked Cameo-mod repo: no ACP/Claude config present.

- Checked local environment: `node`, `npm`, `devin`, `claude`, and `claude-agent-acp` are not on PATH for this shell; no Windsurf ACP registry (`~/.windsurf/acp/registry.json`) or Windsurf logs found.

**Diagnosis (after user logs):** Devin Desktop/Windsurf is trying to spawn `npx -y @agentclientprotocol/claude-agent-acp@0.59.0`, but `npx` is not found in the IDE's PATH (`spawn npx ENOENT`). The ACP client needs Node.js installed (>=20.19 for this package) and available to the IDE process.

**Fix applied:**


- Downloaded and extracted Node.js v24.18.0 LTS to `%LOCALAPPDATA%\Programs\nodejs\node-v24.18.0-win-x64`.

- Added the Node `bin` directory to the user `PATH`.

- Set PowerShell execution policy to `RemoteSigned` for the current user so `*.ps1` scripts (including `npx.ps1`) can run.

- Installed `@agentclientprotocol/claude-agent-acp@0.59.0` globally via `npm`.

- Verified `node -v`, `npx -v`, `claude-agent-acp --version`, and `npx -y @agentclientprotocol/claude-agent-acp@0.59.0 --version` all work.

**Next:** Restart Devin Desktop/Windsurf so the IDE process picks up the updated `PATH`, then enable the Claude agent again.




## 2026-08-04 — Balance ledger re-extract





- Refreshed 32 per-faction JSON ledgers from the current resolved ruleset (`python tools/balance/extract_stats.py`).

- Drift check: 0 drifted.

- Multiplier audit: 0 non-integer `Modifier` values (run with `PYTHONIOENCODING=utf-8`).

- Boot-gate: reached main menu (`PostWorldLoaded`), no new `exception-*.log` files.

- Committed updated ledgers + current uncommitted YAML rule sync (Yuri Slave Miner cost/build duration, `^SwarmlingGrinderTemplate` Valued default).




## 2026-08-04 — extract_stats design_weapon_class fix + HighV NRE





- `tools/balance/extract_stats.py`:


  - Removed all remaining `Versus: Shield` heuristics for `design_weapon_class`.

  - `design_weapon_class` is now derived only from `weapon_classes.yaml` sidecar + keyword fallback.

  - Any weapon mixing more than two warhead-class templates returns `design_weapon_class: null` and `weapon_class_source: illegal_mix` (or `allowlist_mix` for deliberate Dune combat-tank / siege exceptions).

  - Dummy weapons with no damage warheads are marked `extraction_note: no_damage_warheads` and `pricing: false` so they do not feed the balance formula.

  - Re-extracted all 32 `docs/balance/*.json` ledgers; `extract_stats.py --check` reports 0 drifted.

- `mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/weapons.yaml`:


  - `HighV` `Warhead@Bullet_Medium_Percentage` was missing its warhead type, causing the weapon to be dropped from the ruleset and `td_gdi_guardtower` to fail at boot (`Weapons Ruleset does not contain an entry 'highv'`). Set it to `HealthPercentageDamage` to match `M16AP`.

- Boot-gate: reached main menu (`PostWorldLoaded`); no new `exception-*.log` files.




## 2026-08-04 — extract_stats refine class-template detection





- `tools/balance/extract_stats.py`:


  - Treat `^Projectile_*` and `^Effect_*` split-family templates as non-class


    components, leaving only `^Warhead_*` and legacy class templates as class


    inputs. This removes false `illegal_mix` hits from the new 3-way warhead


    split and lets `design_weapon_class` correctly reflect the weapon's real


    class family.

  - Re-extracted all 32 `docs/balance/*.json` ledgers; `extract_stats.py --check`


    reports 0 drifted.




## 2026-08-04 — extract_stats warhead renames and RA2 Thunderbolt family 3-way split





- `tools/balance/extract_stats.py`:


  - Renamed the weapon-template output from `weapon_types` to `warheads`; it now


    contains only resolved `^Warhead_*` templates (recursed through `^`-parents).

  - Renamed the damage-node output from `warheads` to `damage_warheads`.

  - Updated all balance-tool consumers (`build_workbook.py`, `_requantize_ledgers.py`,


    `_patch_ledgers_from_reports.py`, `fit_class.py`, `import_workbook.py`,


    `apply_balance.py`, `update_ranges.py`, `propose_class_rebalance.py`, `check_band.py`)


    to use the new ledger keys.

- `mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml`:


  - Converted `RA2ThunderboltMissile`, `RA2MultiHoverMissile`, and


    `RA2MultiThunderboltMissile` to the new 3-way split: first and last `Inherits`


    become the two `^Warhead_*` templates, the last also provides `^Projectile_*`


    and `^Effect_*`; middle `Inherits` and re-added `Warhead@` overrides removed.

- `mods/cameo/ContentPacks/RedAlert2/Allies/yaml/weapons.yaml`:


  - Converted `RA2PatriotThunderboltMissile` to the new 3-way split.

- Re-extracted all 32 `docs/balance/*.json` ledgers; `extract_stats.py --check`


  reports 0 drifted.

- Boot-gate: reached main menu (`MenuPostProcessEffect.PostWorldLoaded`); no new


  `exception-*.log` files.







## 2026-08-22 — W24 Phase B: SCUDNUKE/SCUDNUKEThermobaric collapse to Nuclear_Super





- Converted SCUDNUKE in mods/cameo/ContentPacks/RedAlert/Soviets/yaml/weapons.yaml:


  - Removed 15 stacked old full-stack inherits (^HeavyMissile, ^MediumMissile, ^LightMissile, ^HeavyBomb, ^ShrapnelWeapon, ^Grenade, ^HeavyChemicalWeapon, ^MediumChemicalWeapon, ^LightChemicalWeapon, ^HeavyFlameWeapon, ^MediumFlameWeapon, ^LightFlameWeapon, ^TankDestroyerCannon, ^FlakWeapon, ^NuclearWarhead).

  - Replaced with Inherits@wh: ^Warhead_Nuclear_Super and Inherits@fx: ^Effect_Nuclear_Super.

  - Per-shot totals preserved: 20000 flat + 10% percentage via Nuclear_Super main Damage: 20000 (10-tick AreaDamage, MaxRadius: 9000, Spread: 1000) and percentage Damage: 10 (10-tick AreaDamagePercentage, Spread: 500, MaxRadius: 4500); ValidRelationships: Enemy, AffectsParent: true, DamageTypes: Prone75Percent, TriggerProne, FireDeath, Incendiary.

  - V2 Bullet projectile retained (Image: V2, Speed: 240, Inaccuracy: 240, LaunchAngle: 80, TrailImage: smokey, contrail colors from the old ^HeavyMissile inherit restored as local overrides).

  - Warhead@Effect kept with ImpactSounds: kaboom22.aud; ^Effect_Nuclear_Super supplies Explosions: nuke_explosion, ImpactActors: false, plus ShieldHit, Concrete: 1000, delayed Scorch smudges, and nuke glow.

  - SCUDNUKEThermobaric still inherits SCUDNUKE and overrides the projectile contrail (width/length/colors); it now resolves to the same single nuke warhead.

- review_resolve_diff.py expected flags: 15 duplicate 20000 warheads collapse to one, ValidTargets becomes Ground, Water, Air, effect stack simplifies to nuke-specific.

- Audits: find_empty_warhead.py 0, find_orphan_old_keys.py 0 real, audit_warhead_split broadcast count lowered 941 -> 939 (baseline updated), audit_balance_drift clean.

- tools/balance/extract_stats.py re-ran; 32 ledgers + derived sidecars refreshed.

- docs/audit/latest/phase_b_survey.md regenerated: 294 concrete, 12 pure single, 0 finish, 282 mixed in 210 groups.

- Updated docs/design/BALANCE_PROGRAM_PLAN.md W24 row.

- Boot-gate: reached MenuPostProcessEffect.PostWorldLoaded; no new exception-*.log.

## 2026-08-22 — W24 A1b: generate five new blend families

- Added CannonNuke, MissileNuke, MissileQuantum, MissileTesla, MissileThermobaric (L/M/H) to gen_weapon_template.py BLEND_FAMILIES, PHYSICS_RANK, FAMILY_DAMAGE_TYPES, FAMILY_INTEGRITY_SCALE.
- Expanded Nuclear in WEAPONS to L/M/H/Super so it can be a blend parent while remaining HAND_TUNED (Nuclear_Super still hand-authored, not emitted).
- Parent choices: CannonNuke = Nuclear + CannonHE; MissileNuke = Nuclear + MissileAP; MissileTesla = Tesla + MissileAP; MissileQuantum = Railgun + Laser + Tesla + 3xMissileAP; MissileThermobaric = Demolition + Concussion + Flame + 3xMissileHE.
- Extended splice_templates.py to append missing ^Warhead_* blocks at end of weapons.yaml.
- Ran splice_templates --all: 112 blocks (15 new) spliced/ appended; verify_generator_sync drift 0; extract_stats regenerated, 0 drift; find_empty_warhead 0; find_orphan_old_keys 0 real; audit_warhead_split 944 vs baseline 939 (expected red, unchanged).
- Boot-gate reached MenuPostProcessEffect.PostWorldLoaded; no new exception-*.log.

## 2026-08-24 — W24 Phase B: RA2 Apocalypse 120mm and rad-chemical 3-way split

- Converted RA2120xmm and RA2120xmm_rad in
  mods/cameo/ContentPacks/RedAlert2/Soviets/yaml/weapons.yaml to the canonical
  three-layer composition:
  - RA2120xmm: ^Warhead_CannonAP_Light, ^Projectile_Shell_Light,
    ^Effect_CannonAP_Light, with ^Effect_Apoc_Explosion_RA2 as an RA2 visual
    addon and a local EffectAir override to preserve ig_explosion_air.
  - RA2120xmm_rad: ^Warhead_Chemical_Light, ^Projectile_Shell_Light,
    ^Effect_Chem_Light, with ^Effect_Apoc_Explosion_RA2 and ^RA2RadShell as
    addons; local EffectAir, smudges, and radiation behaviour preserved.
- Per-shot totals preserved: RA2120xmm 12000 flat, RA2120xmm_rad 16000 flat.
-
eview_resolve_diff.py before/after passes: behavioural invariants preserved
  for both weapons and child variants (RA2120xmm_fire, RA2120xmm_tesla,
  RA2120xmm_elite, RA2120xmm_rad_elite, RA2120xmm_fire_elite,
  RA2120xmm_tesla_elite).
- Audits: ind_empty_warhead.py 0; ind_orphan_old_keys.py 0 real;
  udit_warhead_split broadcast baseline lowered 939 -> 931;
  udit_doc_claims all 19 green after updating doc_claims.yaml and affected
  docs; extract_stats.py --check 0 drift; erify_generator_sync 0 drift.
- Re-extracted balance ledgers with 	ools/balance/extract_stats.py; only
  docs/balance/redalert2_soviets.json + docs/balance/derived/redalert2_soviets.json
  changed.
- Updated documentation counts: docs/audit/doc_claims.yaml,
  docs/design/BALANCE_PROGRAM_PLAN.md, docs/HANDOFF.md,
  docs/audit/SUMMARY.md, docs/audit/latest/doc_claims.md,
  docs/audit/latest/unconverted_templates.md.
- Boot-gate: reached MenuPostProcessEffect.PostWorldLoaded; no new
  exception-*.log files.

## 2026-08-24 — W24 Phase B: Apocalypse 120mm variant family correction

- Created ^Warhead_CannonTesla_Light/Medium/Heavy in the generator (blend of Tesla + CannonAP,
  rank 0.66, IntegrityScale 50, ElectricityDeath/Tesla DamageTypes) and spliced it into
  mods/cameo/weapons/weapons.yaml; verify_generator_sync drift 0.
- Re-pointed the Apocalypse 120mm variants to cannon-delivery blend families:
  - RA2120xmm_rad: ^Warhead_CannonChem_Light, ^Effect_Chem_Light, Corrosion scale 100.
  - RA2120xmm_fire: ^Warhead_CannonFire_Light, ^Effect_Flame_Light.
  - RA2120xmm_tesla: ^Warhead_CannonTesla_Light, ^Effect_Tesla_Impact_RA2.
- Preserved per-shot damage totals (rad 16000, fire/tesla 12000) and kept RA2 addons / FireShrapnel.
- review_resolve_diff: damage, Range, ReloadDelay, Burst, projectile fields preserved for all
  variants; CreateEffect changes flagged only for fire and tesla (intended visual shifts).
- Audits: find_empty_warhead 0; find_orphan_old_keys 0 real; verify_generator_sync 0;
  extract_stats.py --check 0 drift; audit_doc_claims all 19 green after updating
  doc_claims.yaml and affected docs (plating_families 47, w24_multi_main_fed 381,
  physical_state_fired_weapons 462); audit_warhead_split 931 at baseline.
- Re-extracted balance ledgers with tools/balance/extract_stats.py.
- Boot-gate: reached MenuPostProcessEffect.PostWorldLoaded; no new exception-*.log.
