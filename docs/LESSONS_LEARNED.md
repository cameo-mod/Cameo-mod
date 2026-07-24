# Lessons Learned — Start Here Before Every Task

**Read this document, `AGENT_WORKSPACE.md`, `PROJECT_CONTEXT.md`, and especially `DESIGN.md` before touching any code, YAML, asset, or balance value.** All canonical documents must be loaded into context at the start of every session.

This is the central, repository-owned record of hard-won lessons, safe defaults, and recurring pitfalls discovered while working on Cameo. `docs/balance/LESSONS_LEARNED.md` is now a redirect to this file; keep all new lessons here.

---

## Required reading order for every new task

1. `docs/LESSONS_LEARNED.md` (this file) — safe defaults and pitfalls.
2. `docs/AGENT_WORKSPACE.md` — source-of-truth map, operating sequence, incident protocol, commit gate.
3. `docs/PROJECT_CONTEXT.md` — short project orientation and current safety focus.
4. `docs/DESIGN.md` — binding rules and conventions (read the relevant sections, especially before modifying YAML, assets, naming, weapons, balance, or descriptions).
5. `docs/design/ROADMAP.md` — current work queue and P0 items.
6. `docs/Cameo_Knowledge_Base_Manual.md` — engine and custom-trait reference, as needed.
7. `docs/audit/SUMMARY.md` — known issue classes and current audit status.

Do not modify rules, assets, or balance numbers until these documents are in context. When this document and `DESIGN.md` conflict with code or old notes, the repository documents win unless an audit baseline explicitly defers the fix.

## Contents

- [Latest lessons from the July 2026 infantry rebalance pass](#latest-lessons-from-the-july-2026-infantry-rebalance-pass)
- [Class-specific notes](#class-specific-notes)
- [Uniqueness enforcement](#uniqueness-enforcement)
- [Dual-weapon units](#dual-weapon-units)
- [Audit and pipeline findings from 2026-07-22](#audit-and-pipeline-findings-from-2026-07-22)
- [Interactable trait and upgrade actors (2026-07-24)](#interactable-trait-and-upgrade-actors-2026-07-24)
- [Git workflow and commit rules (2026-07-24)](#git-workflow-and-commit-rules-2026-07-24)

---

## Latest lessons from the July 2026 infantry rebalance pass

### Ledger patching safety

- When patching ledger JSONs from generated markdown balance reports, only overwrite primary damage warheads.
  - Skip `HealthPercentageDamage` warheads entirely.
  - Skip warheads whose tag contains `Friendly` (e.g., `GrenadeFriendlyFire`) to avoid corrupting friendly-fire or self-damage values.
  - Update only `SpreadDamage` / `TargetDamage` primary warheads with the report's damage value (the report table column is `dmg`; it maps to the YAML `Damage` field / ledger warhead `damage`).

### Zero-delta formula-price pipeline

- To keep the formula price delta `Δ` at `0` or `±1`:
  - Round solved `Range` to the nearest **10** (range is ALWAYS a multiple of 10) inside the class band.
  - Solve `Range` with `solve_class_baseline_range` to hit the cost, then clamp to the band. (Uniqueness is a separate concern and is NOT about `FirepowerMultiplier` — see [Uniqueness enforcement](#uniqueness-enforcement).)
  - For auto-cost units, set `Cost` to `round(formula_price)` after the final `Range` is chosen.
  - If the solved `Range` falls outside the band, do NOT just clamp `Range`. Re-balance the unit's stats **together** while preserving its feel; if several actors of the class fall outside, preserve their **relative** range order within the class. Burst count, `BurstDelays`, `ReloadDelay`, `Speed`, and `Range` are the most *memorable* stats (change sparingly); HP and damage-per-shot can be tuned more freely (especially with the fine-grained `FirepowerMultiplier`).

### Multiplier formatting

- All OpenRA `*Multiplier` traits (`FirepowerMultiplier`, `DamageMultiplier`, `RangeMultiplier`, `ReloadDelayMultiplier`, `SpeedMultiplier`, `InaccuracyMultiplier`, etc.) use `Modifier` as an **integer percentage in 1 % steps**.
- `89` means 89 %, `100` means 100 %, `125` means 125 %.
- Decimal `Modifier` values such as `0.89` are wrong and must be converted to `89`.
- `tools/balance/apply_balance.py` and `tools/balance/extract_stats.py` now convert between the ledger fraction (`0.89`) and the YAML integer (`89`) automatically.
- `tools/audit/audit_multiplier_modifiers.py` flags any non-integer `*Multiplier Modifier` value.

### Balance tooling discipline

- **Always syntax-check a script before running it** — `python -m py_compile <script>` catches typos that would otherwise leave the pipeline half-finished.
- Then run Python balance scripts through `tools/balance/run_with_guard.py` (syntax pre-check + 60 s timeout guard) or, when the guard is not yet available, `python -m py_compile` + the script directly.
- `propose_class_rebalance.py` is now the generalized dispatcher for ALL 14 classes (reads `class_anchors.json`, uses the SUM engine `formula.spread_damage_sum`). It only prices units already tagged `design.class_anchor`; membership tagging is still pending, so classify a class's units before trusting its full roster output. The old per-class `*_rebalance_proposal_final.py` one-offs are superseded and slated for archival.
- **After every `apply_balance.py --confirm` run, `extract_stats.py` and `audit_multiplier_modifiers.py` execute automatically**. A full audit (`tools/audit/run_all.py` or `tools/audit/run_all.sh`) is still mandatory before commit.

### Data hygiene

- Ledger `design.tech_tier` and `design.class_anchor` are stale.
  - Derive `TechTier` M from YAML `Buildable.Prerequisites` chains, ignoring production buildings.
  - M = `1.0` for T1/T2, `0.75` for T3 (tech center / lab / facility), `0.5` for T4/T5 (superweapon / epic).
- Ledger weapon `Damage`, `ReloadDelay`, and `Burst` values cannot be trusted for curated classes; verify against YAML and faction intent.

### Stat granularity

- **Speed step depends on the domain:** infantry use **steps of 1**; vehicles, aircraft, AND ships use **steps of 5** (their speed is divided by 5 to derive the turn-rate, so it must be a multiple of 5).
- `Range` is always a **multiple of 10**.
- `FirepowerMultiplier` is the **fine-tuning** lever (1 % integer steps, 5 %–200 %): after coarse-tuning warhead `Damage` on the 2000-step grid, use the FP multiplier to land the exact intended DPS. It is a multiplier and is **meaningless on its own** — it is never a uniqueness key (see [Uniqueness enforcement](#uniqueness-enforcement)).
- Raw `Damage` should be kept in 2000-step increments for the balance pipeline (percentage warheads in 1-steps).

### DPS and formula rules

- Effective DPS = `base_dps * FirepowerMultiplier`, where `base_dps` uses the SUM of all offensive warheads (SUM law).
- `base_dps` must **not** include `FirepowerMultiplier`; compute raw base DPS first, then apply the multiplier once.
- If `solve_class_baseline_range` returns a value outside the class band, re-balance the unit's stats together (preserving feel + relative range) rather than blindly clamping — see [Zero-delta formula-price pipeline](#zero-delta-formula-price-pipeline).

## Class-specific notes

### Scout

- Anchor: `naxis_naxiriflesoldier` — HP 20000, Speed 60, Range 5000, DPS 60, Cost 100.
- Verifier: `forgotten_mutantsoldier` 2×/2× at Cost 250.
- Band: range 4500–5500.

### Closecombat

- Anchor: `td_gdi_shotgunner` — HP 50000, Speed 75, Range 3500, **eff-DPS 250**, Cost 200. Weapon SA 2000 + CG 2000 (WC 0.875), Burst 5, **ReloadDelay 70** → 4000×5/70×0.875 = 250.0 (round, damage on the 2000-grid, no FP multiplier needed).
- Verifier: `asianalliance_fanatic` — HP 100000, Speed 75, Range 3500, **eff-DPS 500**, Cost 500. Same SA 2000 + CG 2000, **Burst 10**, ReloadDelay 70 → 4000×10/70×0.875 = 500.0 (exactly 2×).
- Band: range [2500,4500).

### Special Forces

- Anchor: `japan_imperialscoutsman` — HP 15000, Speed 50, Range 6000, DPS 240, Cost 200.
- Verifier: `schwarzermond_lunarsoldier` 2×/2× at Cost 500.
- `td_nod_lasertrooper` is a T4/0.5× heavy trooper: HP 60000, Speed 50, Cost 750, Range 6000. Weapon = CannonAP + Flak + Laser triad, each warhead **16000** → SUM **48000** @ ReloadDelay 50 → DPS **960** (4× the SF baseline's 240, and 4× HP). Under the SUM law the 48000 is the *sum* of three 16000 warheads, not 48000-per-warhead.
- `cabal_eliminator800` rebalance: Damage 4000, ReloadDelay 5, Burst 1, no gatling, Cost ~1450.
- Band: range 5500–6500.

## Uniqueness enforcement

- **Exactly 5 stats must be unique within a class** — checked against each other; the uniqueness audit must enforce THESE AND ONLY THESE:
  1. `HP`
  2. `Speed`
  3. **effective damage per shot** = Σ(all offensive warhead `Damage`) × `FirepowerMultiplier`
  4. `ReloadDelay` — the RAW value, **NOT** the effective/burst-adjusted reload
  5. `Range`
- `FirepowerMultiplier` alone — or any single one of these values in isolation — need NOT be unique; on its own it is meaningless. This **supersedes** any earlier "make effective DPS unique via FirepowerMultiplier" rule: DPS is derived, and uniqueness lives on the 5 raw stats above, with #3 (damage×FP) and #4 (raw ReloadDelay) checked **separately** (two units may share one if they differ on the other).
- Break ties by nudging a stat on its own grid: `Speed` steps of **1** (infantry) / **5** (vehicles, aircraft, ships), `Range` steps of **10**, `Damage` steps of **2000** (then FP-multiplier fine-tune), `HP` steps of **1000**.
- **CODE NOTE:** `propose_class_rebalance.resolve_dps_uniqueness` and the uniqueness audit currently key on *effective DPS* — they must be updated to key on the 5 stats above (raw damage×FP and raw ReloadDelay separately).

## Dual-weapon units

- Units with two weapons (e.g. `ra2_soviets_flaktrooper`: short anti-ground + long anti-air) are balanced **independently — as if each weapon were its own actor**: one anti-ground-only actor and one anti-air-only actor, sharing the same `HP` and `Speed` but each with its own `Damage`, `Range`, `ReloadDelay`, and `Burst` fitted to its weapon.
- **Range is relative between the two weapons** (e.g. anti-air range = anti-ground range × 1.5). The RATIO is the rule, so if one weapon's range must change, change **both** to preserve the ratio.
- `FirepowerMultiplier` is **shared** — it scales BOTH weapons at once. So tune each weapon's other stats (`Damage` on the 2000-grid, `ReloadDelay`, `Burst`, `Range`) FIRST, and use the FP multiplier only for final fine-tuning, remembering every FP change hits both weapons together.

## Audit and pipeline findings from 2026-07-22

### Audit report encoding

- `docs/audit/latest/*.md` files can be written in UTF-16 with embedded null bytes.
- Decode them to clean UTF-8 before reading or processing (e.g. `tools/balance/_decode_audit.py` or an equivalent one-shot script).
- Never commit `.safe.md` decoded copies; regenerate them on demand.

### `MinRange` rule and intentional exceptions

- The default rule is `MinRange = round(Range / 5)` rounded to the nearest 5.
- **Never apply blindly.** Keep the following categories as exceptions:
  - Super-weapon / global-spawner weapons: `*Spawner*`, `*SCUD*`, `*TacticalMissile*`, and any weapon with `Range > 100 000`.
  - Linear-pulse projectiles `WaveArtilleryImpact`, `WaveTurretImpact`, `LurkerSpinesImpact`: `MinRange` is **removed entirely** (maintainer 2026-07-22 — they no longer carry any minimum range; do NOT force `MinRange 1`).
  - Meme/intentional numeric pairs: e.g. `RA160mm` family (`Range 11111`, `MinRange 2222`), `YakovlevCannon` (`Range 4444`, `MinRange 888`).
  - Elite weapons should inherit `MinRange` from their base weapon unless a specific exception is documented.
  - `RA2DiskDrain` / `RA2DiskSteal`: `MinRange` is **removed entirely** (maintainer 2026-07-22 — no minimum range; do NOT force 25).

### Weapon uniqueness

- Same-faction duplicate weapons (`W1` in `audit_weapon_uniqueness`) should usually be split so each actor can be rebalanced independently.
- **Keep shared** when the weapon is intentionally identical: `pdlaserbike`, `spore`, `tentacle`, `asianrailtank2` triad, plus all healing/repair beams.
- **Carrier-borrowed weapons (`W3`)** must never be split; the whole point is IFV/Salamander-style weapon borrowing.
- Naming convention for new unique weapons: `<actor>_<base_weapon>` (e.g. `ixian_lightinfantry_light_inf_lmg`).

### `buildable_order` audit

- The audit flags two separate things:
  1. **Prerequisite-token order** inside a single `Prerequisites` list: tech-building tokens should appear before `~..._promotion_unlock...` tokens.
  2. **`BuildPaletteOrder` sort order** per faction and per build queue, ordered by tier then cost.
- The two checks are independent; fixes are applied faction-by-faction, ignoring actors from other factions.

### `stat_formulas` audit decisions ( maintainer-confirmed )

- **F1** `Repairable.HpPerStep` and **F2** `SelfHealing Step` are formula candidates but were not explicitly approved yet.
- **F3** `Repairable` on infantry-slot mechs/vehicles: keep the trait for units that use the infantry body for animation but are mechanically vehicles/mechs.
- **F4** shield `RegenAmount`: the TD Nod cybernetics upgrade intentionally uses a flat shield/armor-plating bonus; do not overwrite it with the generic `2 × SelfHealing Step` rule. Fix outliers such as `ixian_stormlasher` individually.
- **F5/F6** defense `RevealsShroud` and `DetectCloaked`: apply the formula but cap extreme super-weapon ranges (e.g. `steelconsortium_bfg10000`).
- **F7** `Power.Amount`: apply `-Cost/20` except for walls, fences, and bunkers, which never consume power.
- **F8–F10** vehicle and turret `TurnSpeed`: safe to apply.
- **F11** turreted artillery firing-slow pattern: deferred; put on the roadmap for a future audit rework.
- **F15/F16** Light/Heavy Support composition: apply.
- **F17/F18** fighter/bomber `TurnSpeed` and AA-without-air warhead: apply; F18 is a genuine bug find.

### `propose_class_rebalance.py` / `_patch_ledgers_from_reports.py` fixes

- **SUM LAW (maintainer 2026-07-22, supersedes the earlier MAX rule):** effective
  per-shot damage = the **SUM** of every offensive `SpreadDamage` warhead on the
  weapon, **never** the max. A multi-warhead weapon deals the ADDED damage of all
  its warheads to a target; pricing on the max would let a 10-warhead weapon deal
  10× the damage for the price of one. The one canonical reducer is
  `formula.spread_damage_sum()`; `propose_class_rebalance.spread_damages`,
  `fit_class`, and `update_ranges` all call it so MAX can never creep back.
- `spread_damage_sum()` skips `*ExtraDamage` (shield-only chip), `*Percentage`
  (`HealthPercentageDamage`), and `*FriendlyFire` (own-side splash) warheads.
- The ledger stores `firepower_multiplier` as a fraction (e.g. `1.03`); do **not** divide by 100 again inside the proposal script.
- `_patch_ledgers_from_reports.py` must select exactly the same primary armament (`Armament` or `Armament@PRIMARY`) as `propose_class_rebalance.py`.
- Multi-warhead weapons carry each warhead at its OWN intended damage; the weapon's
  effective damage is their sum. (The by-type/by-faction workbooks already model
  this — one sub-row per warhead, `DPS = Σ sub-rows`.) Do NOT set every warhead
  equal to the intended total — that was the MAX-era mistake that left 20
  closecombat/SF units 2–3× hot.
- Include a `dmg_filter` column (`smallarms` / `all`) in the report for scout small-arms-only pricing.

### Script hygiene (pending)

- Multiple `scout_rebalance_*.py`, `closecombat_rebalance_*.py`, and `special_forces_rebalance_*.py` scripts are redundant with the generic `propose_class_rebalance.py`.
- Plan: consolidate the helpers into one `tools/balance/rebalance_classes.py` dispatcher that calls `extract` → `propose` → `patch` → `apply` (dry-run/confirm) → `build_workbook`.
- Do this after the current audit batch is finished and the pipeline is trusted.

## Interactable trait and upgrade actors (2026-07-24)

### The crash

- **Removing the `Interactable` trait from upgrade actors crashes the game.** `Interactable` provides the hit-testing/mouse-interaction bounds that the engine needs for any actor that exists in the game world. Without it, the engine cannot process clicks or selection on the actor and crashes.
- All upgrade actors inherit `Interactable` from `^upgrade.template` (`mods/cameo/rules/defaults.yaml` line 8759). This is the canonical source — do NOT remove it or add `-Interactable:` to upgrade actors.

### The audit lint rule conflict

- `tools/audit/audit_yaml_lint_rules.py` check 4 (`find_interactable_selectable_conflicts`) flags any actor that has BOTH `Interactable` and `Selectable` traits in the same YAML block as a "conflict".
- However, `Interactable` and `Selectable` serve **complementary** purposes in OpenRA:
  - `Interactable` provides the click/hit-test bounds (required for the actor to be interactive at all).
  - `Selectable` provides selection visual feedback (selection box, health bar, decoration bounds) and **depends on** `Interactable` to function.
- `^promotion_upgrade.template` (line 8771) previously inherited `Interactable` from `^upgrade.template` AND added `Selectable` with `DecorationBounds`. This caused duplicate `InteractableInfo` errors in `--check-yaml` for all promotion upgrades across all factions. **Resolved 2026-07-24:** `Selectable` was removed from `^promotion_upgrade.template`, eliminating ~9k errors and ~9k warnings. The remaining `Interactable + Selectable` warnings are only from 6 engine-level bridge actors (`bridge1`–`bridge4`, `sbridge1`, `sbridge2`).
- The audit script only checks literal trait text within the same YAML block, not resolved inheritance. So `^promotion_upgrade.template` is NOT flagged (because `Interactable:` doesn't appear in its own block, only in the parent). But any actor that explicitly writes both traits in the same block would be flagged.

### What needs future research

- **Is the audit lint rule correct?** The rule assumes `Interactable` and `Selectable` are mutually exclusive, but the engine appears to treat them as complementary. Need to verify:
  1. Whether OpenRA engine actually forbids both traits on the same actor (it doesn't seem to — `Selectable` requires `Interactable`).
  2. Whether the rule should be relaxed to only flag cases where both traits are explicitly defined with conflicting `Bounds`/`DecorationBounds` values.
  3. Whether the rule should be removed entirely or changed to a warning instead of a failure.
- **Goal:** Be completely warnings-and-errors free without crashing the game. The current situation is: the audit flags a false-positive conflict, but removing `Interactable` to satisfy the audit crashes the game. The audit rule needs to be fixed, not the actors.

## Git workflow and commit rules (2026-07-24)

### Binding rules from user and co-maintainer Blackrobe

- **Always fetch, pull, and merge before any commit.** The remote may have changes from other developers. If the engine pin (`mod.config` `ENGINE_VERSION`) changed, always run `make all` to fetch and build the new engine before boot-gating. Never skip the boot-gate.
- **Always boot-gate before committing.** Launch the game with `launch-game.cmd`, wait for the main menu (perf.log ends with `MenuPostProcessEffect.PostWorldLoaded`), kill the process, then check for NEW `exception-*.log` files in `%APPDATA%/OpenRA/Logs`. A commit that breaks the boot is not acceptable.
- **`utility.cmd cameo --check-yaml` is a linting/YAML validation tool, NOT a boot-gate substitute.** Use it for: verifying cosmetic refactors (actor/template renames), checking broken prerequisites, and detecting gameplay-relevant YAML issues. **Goal: 0 errors AND 0 warnings.** The utility takes a VERY LONG TIME (10+ minutes) — only run it when you have completed ALL connected tasks from the last report and expect 0 errors/warnings to confirm. Do NOT run it repeatedly. Keep findings from the last report in ROADMAP and docs so they can be fixed without re-running. It is ABSOLUTELY NECESSARY — just choose wisely WHEN to run it.
- **Always update ALL relevant documentation files BEFORE committing.** This includes `docs/design/ROADMAP.md`, `docs/DESIGN.md`, `docs/audit/SUMMARY.md`, `docs/LESSONS_LEARNED.md`, and any other docs affected by the change. Check old docs for outdated information, inconsistencies, and contradictions — fix them. A commit without updated docs is an incomplete commit.
- **Do not spam commits on upstream master.** Use a pull request (PR) for cleaner commit history. Create a feature branch, push it, open a PR, and merge only after verification.
- **Only merge a PR if either:** (a) you no longer detect regression caused by the changes, or (b) launching the game no longer results in a crash. Commits that do not break the master branch are a naturally acceptable outcome.
- **Commit titles must be self-explanatory to all developers.** Terms like "Phase 5", "A2 audit", "Fix B5", or "X/Y law" are only understood internally by Aedis and their agent. If such internal pointers are necessary, elaborate where to find the definition (e.g. "see docs/audit/SUMMARY.md bug class B5") and what kind of project it links to.
- **When a task is completely done, merge the feature branch to master.** Do not leave completed work stranded on a feature branch. Ensure boot-gate passes and docs are updated before merging.
- See also: `docs/AGENT_WORKSPACE.md` § Git workflow and commit rules.
