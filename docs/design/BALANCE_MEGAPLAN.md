# BALANCE MEGAPLAN — the ordered path to a finished balance pipeline

The single source of truth for **what order** to build the balance pipeline and **everything it
needs** to be complete. Written 2026-08-04. This does not replace the detailed docs — it **threads
them** into one sequence so we never lose the order. Each phase links the doc that owns the detail.

> **Authority note (rev. 2026-08-23) — three files, three jobs, no overlap:**
>
> | file | owns |
> |---|---|
> | **this file** | the strategic **phase sequence** (what order, A→G) |
> | [`BALANCE_PROGRAM_PLAN.md`](BALANCE_PROGRAM_PLAN.md) | **status, ownership, acceptance criteria** — the W1–W26 board, the file-set map (§2), the binding order of operations (§0a) |
> | [`ROADMAP.md`](ROADMAP.md) | the **live granular queue** — individual tasks + commit hashes; crashes jump it |
>
> ⚠ **Where §0a of `BALANCE_PROGRAM_PLAN.md` and the phase order below disagree, §0a wins** — it is
> the newer ruling (2026-08-17) and it is measured. When any of the three disagree on *status*, the
> **artifact** wins: verify, then fix all of them.
>
> This doc supersedes the older `MEGAPLAN.md`, archived at
> [`../history/MEGAPLAN_2026-08-08.md`](../history/MEGAPLAN_2026-08-08.md).
> [`../history/handoffs/AREADAMAGE_HANDOFF_2026-08-04.md`](../history/handoffs/AREADAMAGE_HANDOFF_2026-08-04.md)
> is historical — its warhead conversion is complete (see §1).
>
> ⚠ **This file carries 20 `memory <name>` citations.** They point at a private, per-agent memory
> store that no other reader — maintainer, co-maintainer or another agent — can open. Treat every
> one as **provenance, never authority**. If a memory carries a binding rule, promote it into
> `DESIGN.md`; if it carries a number, pin it in `docs/audit/doc_claims.yaml`.
>
> Companion docs (do NOT duplicate — this indexes them): `BALANCE_PIPELINE.md` (the sanctioned
> loop), `FORMULA_V2.md` (the laws), `BALANCE_SYNTHESIS.md` (synthesis laws), `ARMOR_SYSTEM.md` +
> `WEAPON_TYPE_SYSTEM.md` + `WEAPON_3WAY_SPLIT.md` (weapon/armor grammar), `SPREAD_FALLOFF_PLAN.md`
> (per-type spread/falloff profiles), `AREADAMAGE_WARHEAD_REBALANCE.md` (warhead design + energy chips),
> `ORIGINAL_UNIT_STATS.md` + `ORIGINAL_UNITS_RAW.md` + `GAME_SPECIFIC_WEAPON_BASES.md` +
> `FACTION_IDENTITY.md` (reference material), `class_anchors.json` + `anchor_decisions_log.md` +
> `vehicle_class_decisions.md` (anchors), `discrepancies.md` (Phase-3 triage).

---

## 0. The mental model — 3 layers feeding one formula, applied through anchors

```
 LAYER 1  ORIGINAL_UNIT_STATS.md   cross-game reference library (whole C&C series + SC2 all
          (+ ORIGINAL_UNITS_RAW)   branches + WC3 + Cosmonarchy + Dune/Outpost2). [STAT] vs
                                    [IDENTITY] tagging, per-game normalization.
    +
 LAYER 2  extracted MODS           MO / CnCR / RV (RA2), SP / CnCR (TS), DTA / CA (TD+RA1).
                                    "how a good mod already tuned this unit."
    +
 LAYER 3  old Cameo + synthesis    the mod's own history + faction identity choices.
    |
    v  synthesize (well-reasoned, per-unit unique) + the FORMULA
 CLASS ANCHORS  (class_anchors.json)  per-class baseline: HP/Cost, DPS/Cost, A/B, tier, K.
    |
    v  members spread by formula(baseline weights) + synthesis, NEVER equal to the anchor
 PER-UNIT STATS  ->  ledger (docs/balance/*.json)  ->  yaml  (via apply_balance)
```

**Two laws that govern everything (never violate):**
- **Never hand-edit a balance number in yaml.** Everything flows ledger/workbook -> `apply_balance`.
  `audit_balance_drift` fails red when yaml and the committed ledger disagree.
- **Anchors are BASELINE comparisons, NOT per-unit targets.** HP/Cost, DPS/Cost, A/B aggregates
  describe the class; members are UNIQUE, spread by the formula + synthesis. (memory `cameo-anchor-definition`)

---

## 1. Where we are (VERIFIED 2026-08-08 — supersedes the 2026-08-04 snapshot)

**Phase A (weapon/warhead foundation) — the bulk of the STRUCTURE is done; tuning + collapse remain:**
- **Warheads: DONE.** Universal `AreaDamage` conversion complete — every live weapon main is
  `AreaDamage` + baked 50/50 FF (`Ally, Neutral, Enemy`); `_FriendlyFire` twins retired; Nuclear
  superweapon hand-tuned; `AreaDamagePercentage` for %HP. C# built + boot-proven.
- **A1 generator reconcile: ✅ DONE.** `gen_weapon_template.py` emits AreaDamage + baked FF +
  `^Warhead_<Family>_<Level>` + `_Percentage`; guard `verify_generator_sync.py` (run_all.sh `gen_sync`)
  reports **drift = 0** → regenerate is a verified no-op.
- **A2 cannon templates: ✅ BUILT.** `^Warhead_CannonAP_{L/M/H}` + `^Warhead_CannonHE_{L/M/H}` exist
  (weapons.yaml ~3429–3720), on the two-level ordering law. Repointing weapons onto them proceeds via
  the Phase-B collapse.
- **A3 projectile/effect libraries: ✅ BUILT.** `^Projectile_<Family>_<Level>` + `^Effect_<Family>_<Level>`
  (`gen_effects.py`); weapons inherit them via the 3-way split.
- **A4 weapon tuning — PARTLY done:** energy `_ExtraDamage` chips reworked (paid-for law) + thin energy
  spread ✅ (`b068a94f6`); MissileAA spread reduction ✅. **OPEN:** per-type spread/falloff profiles
  (DESIGNED in `SPREAD_FALLOFF_PLAN.md`, not yet applied — awaiting authoring-model pick), projectile-
  speed / tank-shell rules (documented, not applied), Railgun charge-delay downside, the spread-pricing
  formula term.
- **Weapon 3-way split: Phase A DONE** (0 single-inherit weapons remain). **Phase B (mixed-family
  collapse) IN PROGRESS** — Sniper family (21 weapons ✅ `fa1016d21`), Chemical 2-family group
  (5 weapons ✅ `ac17eb827`); **~350+ mixed weapons in ~250 groups remain** (maintainer-directed,
  dominant-damage heuristic in `docs/audit/latest/phase_b_survey.md`). This IS Phase A5 (retiring the
  deprecated inline old-family damage keys) — same effort, one queue.
- Guards green: `find_empty_warhead.py = 0`, `find_orphan_old_keys{,_multi}.py = 0 real`.

**Phase B (reference material):**
- **B2 extract CnCR + RV: ✅ DONE** (325 + 208 units in `ORIGINAL_UNITS_RAW.md`; sources in
  `~/Downloads`). All Layer-2 mods extracted (DTA/CA/SP/MO/CnCR/RV); only Dune/Outpost2 stubs pending.
  Remaining reference work = R4 synthesis into per-class targets (B1/B3).

**Phase C (anchors):**
- **Vehicle 13-class anchors LOCKED** (`class_anchors.json` + `anchor_decisions_log.md` "★ LOCKED
  2026-08-01"); templates built + armor normalized per-class. RESTAT of baselines+members pending
  (needs weapon DPS/range stable from A4).
- Infantry-class proposals drafted (`docs/balance/proposal_*_infantry.md`); 4 new templates +
  `^AntiTankAntiAir` split + scout-verifier tier fix pending, then lock.
- Defense + aircraft anchors: not started.

**Phases D–G:** `FORMULA_V2` has open terms (spread-pricing, AA/AoE pricing, per-class defense/infantry
baselines). Ledgers exist (`docs/balance/*.json`, 28 factions) but many predate current laws. Workbooks
exist (`cameo_armor_system.xlsx` legacy reference, `cameo_balance_v2.xlsx` workbench). Per-faction
synthesize→apply (F) not started. Phase-3 discrepancy triage open (`docs/balance/discrepancies.md`).

---

## 2. PHASE A — finish the WEAPON / WARHEAD foundation (unblocks DPS + range for everything)

*Balance cannot be finalized until every weapon's effective DPS + range is stable, because pricing
is driven by EFFECTIVE DPS = raw × ∏ firepower knobs (memory `cameo-firepower-mult-in-dps`).*

- **A1. Generator reconcile (AreaDamage drift) — TOP PRIORITY.** `gen_weapon_template.py` still
  emits `SpreadDamage` + old naming; the 54-template flip was a one-shot script. Update the
  generator to emit `AreaDamage` + `ValidRelationships: Ally, Neutral, Enemy` + `FriendlyFireDamage/
  Spread 50`, drop the FF twin, `^Warhead_{tag}`/`Warhead@{tag}_Percentage` naming. Then
  `regenerate + diff` the 54 non-Nuclear templates == file (no-op). **Until then DO NOT regenerate**
  (would revert). (`../history/handoffs/AREADAMAGE_HANDOFF_2026-08-04.md` §3c)
- **A2. Cannon/weapon rebuild** — `CannonAP_{L/M/H}` (anti-heavy) + `CannonHE_{L/M/H}` (anti-veh);
  current cannons -> CannonHE, TankDestroyerCannon -> CannonAP_Light. Built by `gen_weapon_template.py`
  via the two-level ordering law (macro priority × light/heavy). (memory `cameo-cannon-weapon-templates`,
  `cameo-weapon-ordering-law`; docs `ARMOR_SYSTEM.md` §PROFILE, `WEAPON_TYPE_SYSTEM.md`)
- **A3. Projectile + effect template libraries** — `^Projectile<Family>_<Level>` + `^Effect<...>`
  (3-way split, `WEAPON_3WAY_SPLIT.md`, `PROJECTILE_EFFECT_SOURCING.md`). Retrofit weapons inherit
  them. Custom effects = RGBA PngSheet, pair every effect with a sound (memory `cameo-custom-effects-pngsheet`).
- **A4. Weapon tuning laws** (all in `AREADAMAGE_WARHEAD_REBALANCE.md` §3–§5):
  - Energy `_ExtraDamage` chips repurposed with LOCKED ladders (Laser=anti-inf, Railgun=anti-building
    +superheavy Concrete 200>Steel 175>Wood 150 / Shield 10, Tesla=anti-inf+shield keep, Prism/Magic
    =none); thin energy main Spread ~800->150.
  - MissileAA spread reduction (never applied).
  - Projectile-speed / tank-shell rules (regular tank speed=maxRange/10 CannonHE 2×spread; TD +
    cannon-turret speed=maxRange/5 CannonAP small spread; hybrid 50/50 speed=maxRange/10×1.5).
  - Overall spread reduction + a **spread-pricing term** in the formula (diminishing returns,
    expected-targets-hit, capped by the single-target case).
- **A5. Retire deprecated inline damage keys** — 297 live weapons still on inline `Warhead@1Dam`
  etc.; convert to template inherits (memory `cameo-versus-only-in-templates`; DESIGN §870).

**Guard for A:** `audit_warhead_split`, `audit_template_conformance`, `find_empty_warhead.py`
(now blocking in `run_all.sh`), + BOOT GATE. Versus lives ONLY in `^Warhead_*` templates.

---

## 3. PHASE B — REFERENCE MATERIAL (the deep research; feeds every anchor)

*The 3-layer framework (memory `cameo-source-library-scope`, `cameo-balance-synthesis`).*

- **B1. Layer 1 completeness — `ORIGINAL_UNIT_STATS.md`.** The cross-game library. Ensure every
  Cameo unit has its original-source row(s), `[STAT]` (raw numbers) vs `[IDENTITY]` (role/flavor)
  tagged, per-game normalized to Cameo's scale. Faction identity sources: Japan = RA3 Empire + WW2 +
  Touhou; AsianAlliance = Generals China. (⚠ RA2 unitstatistics "health" is a 1–5 rating, NOT raw HP.)
- **B2. Layer 2 — extract the remaining reference MODS + normalize.** DONE: DTA, CA, SP, MO.
  **PENDING: CnCR, RV.** Extract their unit stats, normalize, fold into the per-unit reference rows.
  (memory `cameo-source-library-scope`). Also `shattered_paradise_research.md` (SP done).
- **B3. Layer 3 — synthesis inputs.** Old Cameo values + `FACTION_IDENTITY.md` + rock-paper-scissors
  mandate. This is the "well-reasoned" judgment layer that combines B1+B2 into an intended role.

---

## 4. PHASE C — ANCHORS (per-class baselines, the synthesis output)

*Class anchor = the baseline a class's members are spread around by the formula. Aggregate targets
only (HP/Cost, DPS/Cost, A/B), NOT per-unit. (memory `cameo-anchor-definition`, `cameo-baseline-law`)*

- **C1. Vehicle anchors — LOCKED (13 classes).** `class_anchors.json` + `anchor_decisions_log.md`
  ("★ LOCKED 2026-08-01"): epic-top, ≤2.0× A+B spread, HP 10k-steps, DPS/Cost 0.5–1.5. RESUME =
  restat the 13 baselines + members once A2/A4 land (DPS/range stable). `vehicle_class_decisions.md`,
  `vehicle_class_review.md`, `membership_review.md`, `proposal_vehicle_defense_anchors.md`.
- **C2. Infantry class anchors — draft -> lock.** 12 proposals exist (`proposal_*_infantry.md`:
  scout, closecombat, grenadier, mortar, melee, archer, heavy, flying, rocket_trooper, heavy_sniper,
  pure_sniper, special_forces). NEED: 4 new templates (heavy sniper / rocket trooper / archer /
  support), `^AntiTankAntiAir` split, fix scout verifier tier (forgotten_mutantsoldier is T3 not T1).
  (memory `cameo-infantry-class-program-state`). Lock into `class_anchors.json`.
- **C3. Defense + aircraft anchors.** Per-class baselines for defenses + aircraft (memory
  `cameo-formula-future-tasks`). AA class-gating (only some classes get AA).

**Anchor law (memory `cameo-verifier-tier-k-match`):** baseline + its verifier must share the same
TechTier M-bucket AND K, or the 2.5× identity breaks (T1=T2=M1.0, T3=0.75, T4/5=0.5; tier from
tech-building prereqs only; gatling K1.25; charge-up K adjust).

---

## 5. PHASE D — the FORMULA (FORMULA_V2 completeness)

*Read `FORMULA_V2.md` FIRST (memory `cameo-baseline-law`): O=P=Q=cost baselines, 2×/2×/250%
verifiers, stat bands, conversion checklist.*

- **D1. Complete the missing terms** (memory `cameo-formula-future-tasks`): per-class baselines
  (defenses + infantry), AA pricing, AoE pricing, per-ability specials, the **spread-pricing term**
  (from A4). Bake OUT per-actor multipliers, keep only global 50%+150% (BALANCE_SYNTHESIS law).
- **D2. Verifier laws** — tier+K match (C3), FirepowerMultiplier in effective DPS (unconditional one
  per actor; deploy/undeploy units priced as separate actors — memory `cameo-firepower-mult-in-dps`).
- Code home: `tools/balance/formula.py` (+ `extract_stats.py` provenance).

---

## 6. PHASE E — the EXCEL / WORKBOOK pipeline

*Dual-write law (memory `cameo-sheet-yaml-dual-write`): price set in `cameo_armor_system.xlsx` first
(M in its cell; O/P/Q recompute), yaml FOLLOWS; never scale costs directly in yaml. If
`~$cameo_armor_system.xlsx` exists the workbook is OPEN in Excel — do NOT write it; queue + say so.*

- **E1. Legacy reference** `cameo_armor_system.xlsx` remains the design-judgment reference until the
  Phase-3 discrepancy triage completes (`discrepancies.md`).
- **E2. The v2 workbench** — `tools/balance/build_workbook.py` -> `cameo_balance_v2.xlsx` (gitignored),
  edit the UNLOCKED input cells, read back with `import_workbook.py`. Also
  `cameo_balance_by_faction.xlsx` / `cameo_balance_by_type.xlsx` views. Excel is OPTIONAL — you can
  edit the ledger JSON directly instead.

---

## 7. PHASE F — SYNTHESIZE + APPLY (per faction/class, the actual rebalance)

*The sanctioned loop (`BALANCE_PIPELINE.md`), repeated per faction/class:*

1. `python tools/balance/extract_stats.py` — refresh the ledger from yaml (raw stats + provenance).
2. **Synthesize members** from the anchor (Phase C) + reference (Phase B) via the formula (Phase D):
   each member UNIQUE, spread by formula(baseline weights) + `BALANCE_SYNTHESIS.md` (tighten spread
   0.4–3.5× rifle, strict class↔weapon binding, rock-paper-scissors). Write into the LEDGER (or the
   workbook, Phase E). `propose_class_rebalance.py` / `propose_rebalance.py` assist.
3. `python tools/balance/fit_class.py` — fit members to the anchor (applies FP-mult, skips
   conditional arms). Then `apply_balance.py --faction X --confirm` (dry-run WITHOUT `--confirm`).
   **`--confirm` requires an explicit maintainer order.**
4. Re-run `extract_stats.py`, run `tools/audit/run_all.sh` + **BOOT GATE**, commit yaml + ledger
   TOGETHER.

Do this class-by-class / faction-by-faction. Recommended order: get ONE class end-to-end (e.g. the
13 vehicle classes, since they're locked) as the reference implementation, then infantry, defenses,
aircraft, then per-faction sweeps.

---

## 8. PHASE G — DISCREPANCY TRIAGE + CLEANUP (runs alongside)

- **Phase-3 discrepancy triage** — `docs/balance/discrepancies.md`: reconcile the legacy
  `cameo_armor_system.xlsx` vs the new laws; retire the legacy sheet when clean.
- **YAML cleanup** — `MEGAPLAN_YAML_CLEANUP.md`, `weapons_cleanup_plan.md`: dead weapon files
  (redalert2.yaml etc.) deletion, actor-inheritance -> `^Templates` review (deferred, grandfathered
  — memory `cameo-no-actor-inheritance`), closed-file-set discipline.
- **ContentPack migration** (the mission end-goal) — split remaining monoliths, per-faction ai.yaml,
  move assets in (memory `cameo-mission-contentpacks`, `docs/MIGRATION.md`). Balance-independent;
  can run in parallel.

---

## 9. THE CANONICAL ORDER (one sequence — do not reorder A before B where noted)

```
A1 generator reconcile (unblocks safe regen)          <- DO FIRST (warhead work in flight)
A2 cannon/weapon rebuild (CannonAP/HE ×L/M/H)         <- unblocks DPS/range
A3 projectile + effect templates
A4 weapon tuning laws (energy chips, spreads, speeds, spread-pricing)
A5 retire inline damage keys
        |  (weapons stable -> DPS/range stable)
B1 ORIGINAL_UNIT_STATS completeness                    <- reference, can parallel A
B2 extract CnCR + RV, normalize
B3 synthesis inputs (faction identity)
        |
D1/D2 finish FORMULA_V2 terms (spread-pricing needs A4)
        |
C1 vehicle anchors restat (locked; needs A2/A4)
C2 infantry anchors (4 new templates, lock proposals)
C3 defense + aircraft anchors
        |
E build/refresh workbooks (or edit ledgers directly)
        |
F  per class/faction: extract -> synthesize members -> fit_class -> apply_balance --confirm
   -> audit + BOOT + commit   (repeat for all 28 factions / all classes)
        |
G  discrepancy triage + yaml/ContentPack cleanup  (parallel throughout)
```

---

## 10. GUARDRAILS (invariants that must ALWAYS hold — the pipeline is not "done" until all green)

- **BOOT GATE before every commit** (memory `cameo-launch-before-commit`) — the only thing that
  catches junk trait nodes.
- **`tools/audit/run_all.sh` green** — incl. `audit_balance_drift` (yaml==ledger), `audit_warhead_split`,
  `audit_template_conformance`, `find_empty_warhead.py` (blocking), `audit_stat_formulas`.
- **Never hand-edit balance numbers** (pipeline only). **Never change a warhead/Burst/BurstDelays
  without explicit permission** (memory `cameo-warhead-change-permission`).
- **Versus ONLY in `^Warhead_*` templates** (memory `cameo-versus-only-in-templates`).
- **Scoped `git add`, never `-A`** (maintainer WIP). **Reports via bash `run_all.sh` only** (PowerShell
  `>` = UTF-16 hazard). **Underscore-only naming** (no hyphens).
- **The DLL loads from `engine/bin`** (rebuild after C# changes; copy to the tracked `mods/cameo`
  copy for release — memory `cameo-dll-deploy-engine-bin`).

---

## 11. "DONE" definition (the finish line)

The balance pipeline is complete when: every weapon uses templated AreaDamage + templated Versus with
stable effective DPS (Phase A); every unit has reference rows from all 3 layers (Phase B); every class
has a locked anchor (Phase C); FORMULA_V2 has no missing terms (Phase D); every faction's ledger is
formula-derived, workbook-consistent, and applied to yaml (Phase F); `run_all.sh` is fully green and
the game boots (Phase G + guardrails). At that point a single `extract_stats -> fit_class ->
apply_balance` round-trip is a no-op diff — the definition of a converged pipeline.

---

## 12. Open-items checklist (verified 2026-08-08; tick as completed)

- [x] A1 generator reconcile (AreaDamage, drift=0)  · [x] A2 cannon templates built  · [x] A3 projectile/effect libs
- [x] A4 energy chips (paid-for) · [x] A4 MissileAA spread reduction
- [ ] A4 per-type spread/falloff profiles (DESIGNED in SPREAD_FALLOFF_PLAN.md; pick authoring model → generate → boot)
- [ ] A4 projectile-speed / tank-shell rules applied (AP `range/5`, HE `range/10` 2× spread, artillery slow lob)
- [ ] A4 Railgun charge-delay downside (= 50% ReloadDelay, armament-level)  · [ ] A4 spread-pricing formula term
- [ ] A5 / Phase B: collapse the ~350+ remaining mixed-family weapons (dominant-damage; retires inline old keys)
- [ ] B1 ORIGINAL_UNIT_STATS complete  · [x] B2 extract CnCR + RV (done)  · [ ] B3 faction-identity synthesis
- [ ] D1 FORMULA_V2 missing terms (spread-pricing, AA/AoE, per-class defense/infantry baselines)  · [ ] D2 verifier laws wired into extract/fit
- [ ] C1 vehicle anchors restat  · [ ] C2 infantry anchors (4 templates + lock)  · [ ] C3 defense/aircraft anchors
- [ ] E workbooks refreshed (or ledger-direct)
- [ ] F per-class/faction synthesize -> fit -> apply -> audit -> boot -> commit (×all)
- [ ] G discrepancy triage clean · legacy xlsx retired · yaml/ContentPack cleanup
- [ ] Guardrails: run_all.sh fully green + round-trip no-op
