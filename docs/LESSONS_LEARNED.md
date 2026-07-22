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

---

## Latest lessons from the July 2026 infantry rebalance pass

### Ledger patching safety

- When patching ledger JSONs from generated markdown balance reports, only overwrite primary damage warheads.
  - Skip `HealthPercentageDamage` warheads entirely.
  - Skip warheads whose tag contains `Friendly` (e.g., `GrenadeFriendlyFire`) to avoid corrupting friendly-fire or self-damage values.
  - Update only `SpreadDamage` / `TargetDamage` primary warheads with the report's `dmg` value.

### Zero-delta formula-price pipeline

- To keep the formula price delta `Δ` at `0` or `±1`:
  - Round solved `Range` to the nearest integer (step 1) inside the class band.
  - Constrain the `FirepowerMultiplier` uniqueness search so `solve_class_baseline_range` stays inside the class band; otherwise the unit cannot price correctly and `Δ` will explode.
  - For auto-cost units, set `Cost` to `round(formula_price)` after the final `Range` is chosen.
  - If the solved `Range` is outside the band, the `cost`/`stat`/`tech` combination is inconsistent — adjust one of them, not the `Range` alone.

### Multiplier formatting

- All OpenRA `*Multiplier` traits (`FirepowerMultiplier`, `DamageMultiplier`, `RangeMultiplier`, `ReloadDelayMultiplier`, `SpeedMultiplier`, `InaccuracyMultiplier`, etc.) use `Modifier` as an **integer percentage in 1 % steps**.
- `89` means 89 %, `100` means 100 %, `125` means 125 %.
- Decimal `Modifier` values such as `0.89` are wrong and must be converted to `89`.
- `tools/balance/apply_balance.py` and `tools/balance/extract_stats.py` now convert between the ledger fraction (`0.89`) and the YAML integer (`89`) automatically.
- `tools/audit/audit_multiplier_modifiers.py` flags any non-integer `*Multiplier Modifier` value.

### Balance tooling discipline

- **Always syntax-check a script before running it** — `python -m py_compile <script>` catches typos that would otherwise leave the pipeline half-finished.
- Then run Python balance scripts through `tools/balance/run_with_guard.py` (syntax pre-check + 60 s timeout guard) or, when the guard is not yet available, `python -m py_compile` + the script directly.
- Keep curated `*_rebalance_proposal_final.py` scripts as the source of truth until the ledger JSONs are fully refreshed.
- Do not rely on the generic `propose_class_rebalance.py` for curated classes while ledger `class_anchor`, `subtype`, and weapon stats are stale.
- **After every `apply_balance.py --confirm` run, `extract_stats.py` and `audit_multiplier_modifiers.py` execute automatically**. A full audit (`tools/balance/_run_full_audit.py` or `tools/audit/run_all.sh`) is still mandatory before commit.

### Data hygiene

- Ledger `design.tech_tier` and `design.class_anchor` are stale.
  - Derive `TechTier` M from YAML `Buildable.Prerequisites` chains, ignoring production buildings.
  - M = `1.0` for T1/T2, `0.75` for T3 (tech center / lab / facility), `0.5` for T4/T5 (superweapon / epic).
- Ledger weapon `Damage`, `ReloadDelay`, and `Burst` values cannot be trusted for curated classes; verify against YAML and faction intent.

### Stat granularity

- Infantry Speed can use **steps of 1** (not 5; the 5-step rule is for vehicles only).
- `FirepowerMultiplier` is the primary lever for making effective DPS unique; it can range from 5 % to 200 % (1 % integer steps).
- Raw `Damage` should be kept in 2000-step increments for the balance pipeline.

### DPS and formula rules

- Effective DPS = `base_dps * FirepowerMultiplier`.
- `base_dps` must **not** include `FirepowerMultiplier`; compute raw base DPS first, then apply the multiplier once.
- If `solve_class_baseline_range` returns a value outside the class band, the cost/stat/tech combination is inconsistent — adjust one of them rather than blindly clamping.

## Class-specific notes

### Scout

- Anchor: `naxis_naxiriflesoldier` — HP 20000, Speed 60, Range 5000, DPS 60, Cost 100.
- Verifier: `forgotten_mutantsoldier` 2×/2× at Cost 250.
- Band: range 4500–5500.

### Closecombat

- Anchor: `td_gdi_shotgunner` — HP 50000, Speed 75, Range 3500, eff-DPS 233.33, Cost 200.
- Verifier: `asianalliance_fanatic` — HP 100000, Speed 75, Range 3500, eff-DPS 466.67, Cost 500.
- `naxis_sssoldier` needs `FirepowerMultiplier ~136 %` and `BurstDelays = 5` to justify Cost 240 at T3/0.75 and land in the [2500,4500) band.
- Band: range [2500,4500).

### Special Forces

- Anchor: `japan_imperialscoutsman` — HP 15000, Speed 50, Range 6000, DPS 240, Cost 200.
- Verifier: `schwarzermond_lunarsoldier` 2×/2× at Cost 500.
- `td_nod_lasertrooper` is a T4/0.5× heavy trooper: HP 60000, Speed 50, Damage 48000@50, DPS 960, Cost 750, Range 6000.
- `cabal_eliminator800` rebalance: Damage 4000, ReloadDelay 5, Burst 1, no gatling, Cost ~1450.
- Band: range 5500–6500.

## Uniqueness enforcement

- Keep HP, Speed, Range, and effective DPS unique within each class.
- Nudge `FirepowerMultiplier` across its full 5 %–200 % range to maximize separation.
- Nudge Speed in integer steps of 1.
- Round solved Range to the nearest integer and nudge ±1 to break ties.

## Dual-weapon units

- Units with multiple armaments (e.g. `ra2_soviets_flaktrooper` short anti-ground / long anti-air) must keep their weapon ranges and armament slots intact.
- Adjust effective DPS only through `FirepowerMultiplier` and `ReloadDelay` for these units, and only if the multiplier does not produce even/duplicate DPS results.
- Avoid changing `Damage` or `Range` for dual-weapon units.

## Audit and pipeline findings from 2026-07-22

### Audit report encoding

- `docs/audit/latest/*.md` files can be written in UTF-16 with embedded null bytes.
- Decode them to clean UTF-8 before reading or processing (e.g. `tools/balance/_decode_audit.py` or an equivalent one-shot script).
- Never commit `.safe.md` decoded copies; regenerate them on demand.

### `MinRange` rule and intentional exceptions

- The default rule is `MinRange = round(Range / 5)` rounded to the nearest 5.
- **Never apply blindly.** Keep the following categories as exceptions:
  - Super-weapon / global-spawner weapons: `*Spawner*`, `*SCUD*`, `*TacticalMissile*`, and any weapon with `Range > 100 000`.
  - Linear-pulse projectiles that mechanically need a minimum range of 1: `WaveArtilleryImpact`, `WaveTurretImpact`, `LurkerSpinesImpact`.
  - Meme/intentional numeric pairs: e.g. `RA160mm` family (`Range 11111`, `MinRange 2222`), `YakovlevCannon` (`Range 4444`, `MinRange 888`).
  - Elite weapons should inherit `MinRange` from their base weapon unless a specific exception is documented.
  - `RA2DiskDrain` / `RA2DiskSteal` are intentionally short-ranged; consider removing `MinRange` entirely rather than forcing 25.

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
