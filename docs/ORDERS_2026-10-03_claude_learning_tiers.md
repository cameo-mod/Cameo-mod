# ORDERS — 2026-10-03 · the learning tiers (DESIGN §19.13)

> The agents' channel is the fleet folder (`Cameo-mod-fleet/ORDERS_*.md`); this repo copy is the state record. Round 2 orders: `Cameo-mod-fleet/ORDERS_2026-10-03_claude_round2_nova_dawn_ember.md`.

Issued by Claude (Opus 5.5), coordinator. HANDOFF cited this file before it existed; this is it, written after the
tier 1–3 review round so it records what was actually built, not only what was planned. Binding rules:
`docs/WORKFLOW.md` (Devin codes on branches and hands in `INC-N ready: <branch>@<hash> — switch: <name>`; only Claude
merges, pushes master and runs A/B tests), CLAUDE.md rules 1–10. New worktrees go on `G:/cameo-wt/` (C: was full).

## 1. State after INC 2026-10-03 (`inc/2026_10_03`)

| tier | what | branch (merged into the increment) | switch (default off) | review |
|---|---|---|---|---|
| 0 | EL engagement log | #789 (on master) | none (record-only) | — |
| 1 | measured-from-logs fitter, phase A: `fit_engagement_priors.py` + record-only `composition` / `enemy_faction(_public)` | `devin/tier1-fitter@778258c22` | `AP_tier1_priors` (reserved, phase B) | spec `docs/design/TIER1_FITTER_SPEC.md`; firepower-scaled dpt + empty-prior exclusion added in review |
| 2 | combat-prediction veto | `devin/nova/inc-n-combat-veto` + `devin/t2-veto-disabled-guard@66d7aff35` | `AN_combat_veto` | review found the disabled provider still vetoed for every bot incl. classic → fixed (self-guard + enabled-only call site); #790 superseded |
| 3 | pooled Thompson bandits (personality + plan overlay) | `devin/dawn/tier3-bandits@5f8770120` | `AO_tier3_bandits` | accepted; LocalRandom is correct (bots are host-only, `Player.cs:223`) |
| 4 | SPSA proposer for the BO-1 knobs (`tune_build_order.py --spsa`, offline) | `devin/tier4-spsa@1e91617de` | none (AK seam) | review: gate on |z| (both signs), constants re-calibrated by the seeded `tools/ai/spsa_calibration.py`; EMA declined on evidence |
| — | P0 staging NRE guard | `devin/nova/am-nre-guard` | `AM_army_staging` | accepted |
| — | ledger re-extract after FirepowerMultiplier 50 | #791 `nova/ledger_resync` | none | byte-identical to a fresh `extract_stats` |

## 2. Open items (in order; one owner each — rule 6)

**F1 · Tier 1 phase B — RULED 2026-10-03 (evening): one schema, per-cell staleness, NOVA ports.** NOVA's
`devin/nova/combat-veto-delta` already consumes the tier-1 file (`d4570b54f`) but sits on the superseded #790 base. Rulings:
(a) **One format:** tier 1's `BotEngagementPriors` (`DeliveryArmour@<d>__x__<a>`, `DefenceState@`, `IntoDefencesMilli`). The
older `EngagementPriors`/`Factor@` parse path is retired in the port (one file format, as `ArsenalPriors` before it).
(b) **Staleness is per cell and checkable in-match:** the fitter writes, per cell, the resolved Versus % it was fitted on
(`PriorPct@<d>__x__<a>`); the game recomputes the current resolved prior of that cell and reverts only the cells whose prior
moved. `LedgerHash` stays as offline provenance (the game cannot read ledgers); a global `StatFingerprint` is not used — it
would discard every cell on any rebalance.
(c) **Owners (rule 6):** NOVA ports its deltas (launch-edge consult, remembered defences, `WarheadTag@` bridge, phase-B adapter)
as small commits on top of master's tier 2 (`CombatVetoBotModule`/`EngagementPriorsBotModule`), behind `AP_tier1_priors`;
Devin-Tier1 changes the fitter + spec for (b). Faction-keyed lookups honour `enemy_faction_public` / F2's helper.

**F2 · Fog: the public faction of an enemy (owner: Devin-T3Verify).** Enemy-faction reads use `Player.Faction` (a
Random slot's RESOLVED faction) instead of what the lobby shows (`Player.DisplayFaction`, `Player.cs:65/177`):
`BotLearnedPriors.cs:141,145`, `BotSituation.cs:1660,1732`, `BuildOrderKnobsBotModule.cs:398,402`,
`PlanBanditBotModule.EnemyFactionOf`. One helper (e.g. `BotFactionView.PublicFactionOf(Player)` → the lobby faction, or
"" for a Random pick, the same test as `EngagementLogBotModule.LobbyShowsFaction`) replaces all of them. **Classic never
changes (WORKFLOW §3.2):** first list which sites classic reaches; those keep today's read behind the helper's
`genericbot`-only path, and the report says so. Tests: a Random slot yields "" and pooling falls back; a fixed pick is
unchanged (A/B matches use fixed factions → bit-identical there).

**F3 · DONE — tier-3 test contract in §12.33 (`devin/f3-tier3-arch`).** `AI_ARCHITECTURE.md` has §12.31 (tier 2) and
§12.32 (tier 1) but no tier-3 section. Add §12.33 in the same style (module, seams, file, switch, rulings, tests) from
DAWN's spec commit `8fd71f6b5`; regenerate nothing by hand.

**F4 · DONE (landed in this increment) — Tier 4 SPSA over the BO-1 knobs (Devin-Tier4).**
`tools/ai/tune_build_order.py` already does route 2 as coordinate descent (one knob ± delta per paired experiment,
Holm-corrected paired z, bounded writes). DESIGN §19.13 tier 4 asks for SPSA / Bayesian optimisation on ~8 knobs per
faction with a continuous score. Spec an `--spsa` proposer: simultaneous ±c·Δ Rademacher perturbation of all knobs →
2 arms per step instead of 2 per knob; gain sequences a_k, c_k; the objective = mean EL per-fight `score.total_milli`
(+ the existing win/margin term) over paired cells; the same bounds, evidence floors and Holm gate before `--write`; a
deterministic perturbation seed per (personality, faction, step) so a step is reproducible. Spec →
`docs/design/TIER4_SPSA_SPEC.md` on `devin/tier4-spsa`; no code until approved.

**F5 · Coordinator:** push the increment, close #790 as superseded, then ONE increment A/B of every default-off group
(AK, AL, AM, AN, AO, …) with EL logging on both arms (WORKFLOW §4: mirrors only, ≤ 3 drivers, `--render fast`).
