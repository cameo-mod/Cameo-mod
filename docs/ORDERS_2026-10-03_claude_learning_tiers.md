# ORDERS — 2026-10-03 · the learning tiers (DESIGN §19.13)

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
| — | P0 staging NRE guard | `devin/nova/am-nre-guard` | `AM_army_staging` | accepted |
| — | ledger re-extract after FirepowerMultiplier 50 | #791 `nova/ledger_resync` | none | byte-identical to a fresh `extract_stats` |

## 2. Open items (in order; one owner each — rule 6)

**F1 · Tier 1 phase B — the consumer (owner: Devin-Tier1).** `EngagementPriorsBotModule` serves the
delivery×armour table from `mods/cameo/ai/learned/engagement_priors.yaml` when present (cell → `TradePercent` → 1000),
behind `AP_tier1_priors` (`genericbot && tier1_priors`, classic never sees it). Re-price a cell only when its prior
is unchanged since `LedgerHash` (stale cells → neutral). Faction-keyed lookups honour `enemy_faction_public` (Random
slot → family/global pool). Tests: parse, lookup, fallback chain, clamp, stale-cell revert; `CombatVetoEvalTest` vectors.

**F2 · Fog: the public faction of an enemy (owner: Devin-T3Verify).** Enemy-faction reads use `Player.Faction` (a
Random slot's RESOLVED faction) instead of what the lobby shows (`Player.DisplayFaction`, `Player.cs:65/177`):
`BotLearnedPriors.cs:141,145`, `BotSituation.cs:1660,1732`, `BuildOrderKnobsBotModule.cs:398,402`,
`PlanBanditBotModule.EnemyFactionOf`. One helper (e.g. `BotFactionView.PublicFactionOf(Player)` → the lobby faction, or
"" for a Random pick, the same test as `EngagementLogBotModule.LobbyShowsFaction`) replaces all of them. **Classic never
changes (WORKFLOW §3.2):** first list which sites classic reaches; those keep today's read behind the helper's
`genericbot`-only path, and the report says so. Tests: a Random slot yields "" and pooling falls back; a fixed pick is
unchanged (A/B matches use fixed factions → bit-identical there).

**F3 · Tier 3 in the architecture doc (owner: Devin-T3Verify, after F2).** `AI_ARCHITECTURE.md` has §12.31 (tier 2) and
§12.32 (tier 1) but no tier-3 section. Add §12.33 in the same style (module, seams, file, switch, rulings, tests) from
DAWN's spec commit `8fd71f6b5`; regenerate nothing by hand.

**F4 · Tier 4 — SPSA over the BO-1 knobs (owner: Devin-LedgerVerify; SPEC FIRST, coordinator approves).**
`tools/ai/tune_build_order.py` already does route 2 as coordinate descent (one knob ± delta per paired experiment,
Holm-corrected paired z, bounded writes). DESIGN §19.13 tier 4 asks for SPSA / Bayesian optimisation on ~8 knobs per
faction with a continuous score. Spec an `--spsa` proposer: simultaneous ±c·Δ Rademacher perturbation of all knobs →
2 arms per step instead of 2 per knob; gain sequences a_k, c_k; the objective = mean EL per-fight `score.total_milli`
(+ the existing win/margin term) over paired cells; the same bounds, evidence floors and Holm gate before `--write`; a
deterministic perturbation seed per (personality, faction, step) so a step is reproducible. Spec →
`docs/design/TIER4_SPSA_SPEC.md` on `devin/tier4-spsa`; no code until approved.

**F5 · Coordinator:** push the increment, close #790 as superseded, then ONE increment A/B of every default-off group
(AK, AL, AM, AN, AO, …) with EL logging on both arms (WORKFLOW §4: mirrors only, ≤ 3 drivers, `--render fast`).
