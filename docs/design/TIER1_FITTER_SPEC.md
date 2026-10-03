# TIER1_FITTER_SPEC — the tier-1 "measured from logs" fitter

Owner: Devin-Tier1 (was EMBER's queue slot in `docs/HANDOFF.md` line 23; no branch existed).
Rulings: DESIGN §19.13 (five learning tiers, tier 1 = measured), §19.2 (frozen at match start,
per-faction priors, bounded residuals), §19.5 (fog honesty); AI_ARCHITECTURE §12.30 (EL-0 record),
§12.31 (NOVA's combat veto, `IBotEngagementPriors`); research basis
`AI_LEARNING_RESEARCH_2026-10-03.md` (tier 1 row: ~10³ coefficients, ~500 logged battles, seconds of CPU).

Status: SPEC ONLY — no code. Awaiting coordinator review before implementation.

---

## 1. What tier 1 is

An offline fitter that turns closed `engagement/1` records into a committed, stat-normalised
corrections file, plus the small record-only log additions that make the fit identifiable.
It learns nothing in-match. In-match consumers read only the frozen output file.

Output feeds the tier-2 veto's damage assembly through `IBotEngagementPriors.CorrectionMilli`
(NOVA branch `devin/nova/inc-n-combat-veto`): bounded thousandths multipliers on
`BotUnitProfile.DamagePerTickAgainst` per (attacker, target). NOVA's interface comment reserves
exactly this slot: "the file that feeds an implementation owns the unit→delivery-key mapping —
this interface stays stat-normalised (no per-unit ids)".

## 2. Input

### 2.1 Primary: `engagement/1` records (`Logs/cameo-ai-engagements.jsonl`)

Read via `ai_log_common.load()` — the same loader `tools/ai/engagement_report.py` uses; no new
log parser. One closed record per fight per observing bot (both sides of a duel each write one:
the elsmoke logs show Multi0 `hard` + Multi1 `classic` rows for the same fight).

Per-record fields the fit consumes (AI_MATCH_LOG.md "Engagement log"):

| block | fields used |
|---|---|
| header | `game_uid`, `record_id`, `faction`, `bot_type`, `personality`, `start_tick`, `end_tick`, `duration_ticks`, `kind`, `skirmish`, `centroid` |
| `seen.start` | force values, `enemy_units`, `enemy_defence_count`, `predicted_ratio_milli`, `predicted_*_surviving_permille` |
| `response` (defend) | `response_ticks`, `army_dist_at_start_cells`, `first_own_hurt_tick` |
| `tactics` | `approach_angle_deg`, `artillery_first`, `into_defences_value`, `suicide_index_milli`, `defence_points` |
| `outcome` | `own_lost_value`, `enemy_killed_value`, `*_unit/defence/building` splits, `own_lost_by_role` |
| `truth.start/end` | `enemy_unit_value`, `enemy_defence_value`, `enemy_loss_value` |
| `score` | `trade_milli`, `predicted_trade_milli`, `vs_prediction_milli` |

Rules:

- `skirmish: true` records are parsed but excluded from every fitted table (same rule as the report).
- `seen` vs `truth`: the offline fitter may use `truth` for **calibration only** (measuring how
  fog shortfall biases a cell). Fitted coefficients are computed against `seen` inputs — the same
  information the in-match predictor will have. No in-match code path ever touches `truth`
  (DESIGN §19.13; the writer's `OmniscientTruthScan` stays the single manifested site).
- `record: posture` lines are ignored by this fit (they belong to EL-1).

### 2.2 The gap: `engagement/1` has no per-type composition — small schema extension required

`seen.start`/`seen.end`/`truth.*` carry aggregate values only. A delivery×armour fit needs to know
*which* unit types fought. The `own`/`enemy` `(BotUnitProfile → count)` maps already exist inside
`BuildSeen`/`OmniscientTruthScan`; they are dropped before serialisation. Spec adds, record-only:

- `composition` object inside `seen.start`, `seen.end`, `truth.start`, `truth.end`:
  `{"units": {"<actor_name>": <count>}, "defences": {"<actor_name>": <count>}}`.
  `seen` keeps the existing visibility filter (visible or frozen enemies only); `truth` stays
  unfogged. Unit ids in the *input* log are permitted — the "never per-unit ids" rule binds the
  OUTPUT coefficient keys, and the existing `arsenal_priors.yaml` precedent already keys on names.
- `enemy_faction` on the record header. The opponent's faction is lobby-public information (chosen
  openly), so this is fog-honest and removes the need to pair records across players for faction
  identity. (Record pairing on `game_uid` + overlapping `(centroid, start_tick)` remains the
  fallback for logs written before this field exists, cross-checked against
  `cameo-ai-matches.jsonl` which lists both players' factions per `game_uid`.)
- JSONL readers ignore unknown keys; old records still parse (they fit nothing new — only new
  records with `composition` feed the cell grid). Version stays `engagement/1` (additive fields,
  no semantic change), matching how schema-2 situation records were added under the same writer rule.

### 2.3 Stat baseline: the resolved ruleset + balance ledger

Delivery/armour mapping never hand-parses yaml (CLAUDE.md 8e): the fitter resolves each logged
type name to a `BotUnitProfile`-equivalent (cost, HP, armour, per-weapon damage-per-tick, main
warhead `Versus`, range, target types) through `miniyaml.Ruleset.resolve*` —
`weapon_efficiency.versus_of` for the Versus row. The pipeline prior for cell (d, a) is exactly
the resolved `Versus` percentage of delivery family d's main warhead against armour a — so a
rebalance regenerates the prior for free (research doc "Factions share a prior").

## 3. The coefficient table (~10³ coefficients)

All values are bounded residual corrections (thousandths, 1000 = neutral) on stat-derived priors.
Never a per-unit id: keys are delivery families and armour classes only.

### 3.1 Delivery × armour strength grid — ~260 cells

- **Delivery families**: the `^Warhead_{Family}_{Level}` taxonomy in
  `docs/balance/weapon_classes.yaml` (Bullet, CannonAP, CannonHE, MissileAP, MissileHE, MissileAA,
  Flak, Flame, Chemical, Grenade, Shrapnel, Tesla, Laser, Railgun, Sniper, Arrow, Sword, Magic,
  Toxic, …). ~26 families after the 3-way split; the fitter enumerates them from the resolved
  warhead templates, never a hardcoded list. A unit's delivery class = its main damage warhead's
  family (same rule as `BotUnitProfiles.Build`: largest positive `DamageWarhead`).
- **Armour classes**: the resolved `Armor.Type` set (`armor_census`: effectively ~10 —
  None/infantry/vehicle/air classes + Wood/Concrete + the plating-class names as used in `Versus`).
- Cell `C[d][a]`: measured damage-efficiency correction of delivery d against armour a.
  Prior = resolved Versus[d][a]/100. Fitted residual = observed/prior, shrunk toward 1.0.

### 3.2 Static-defence state strengths — ~100 cells

- Defences carry weapons → they occupy the same (d, a) grid, but *attacking into* them changes the
  fight beyond Lanchester (range advantage, forced entry). Two fitted quantities:
  - `DefenceState[defence-delivery]`: effectiveness correction for static defence fire
    (fitted on `defend`/`attack` records where `defence_points > 0`).
  - `IntoDefences` global correction on the attacker's side, regressed from
    `tactics.into_defences_value` vs `outcome` — the "suicide into defences" term the veto needs.

### 3.3 Attrition exponent — 1 coefficient

Lanchester assumes square law. Fit exponent α on surviving-fraction outcomes
(`predicted_*_surviving_permille` vs realised value fractions): a global correction the predictor
applies uniformly. Bounded [0.5, 2.0], shrinks to 1.0 (square law) without evidence.

### 3.4 Timing and response priors — remainder (~600)

- `AttackTiming[enemy_faction][phase]`: distribution over director phases / 6000-tick buckets of
  first `attack`-kind engagements that faction initiates (their record's `kind` mirrored: an
  engagement that is `defend` for us is `attack` for them — the cross-record pair supplies it; or
  directly from `enemy_faction`'s own records in the same `game_uid`).
- `Response[own_faction][phase]`: `response_ticks` quantiles (p50/p90) and
  `army_dist_at_start_cells` medians for `defend` records — the expected-defence calibration.
- `SuicideIndex[faction-pair]`: shrunk median of `tactics.suicide_index_milli` for attack records.
- Pooled global → game-family → faction like the tier-3 bandit hierarchy (research doc §"Factions
  share a prior"): under-observed cells shrink to the parent.

### 3.5 Calibration side-product (not shipped to the game)

`truth` - `seen` enemy value gap per phase = the fog-bias curve, extending
`VisibilityPercentByPhase` in `arsenal_priors.yaml`. Reported in the fitter's stdout report;
optionally regenerated into the existing field.

## 4. Fitting method

Closed-form / EM, seconds of CPU — no gradient descent, no external deps (stdlib only, like
`fit_arsenal_priors.py`).

1. **Build the sample**: every scored record with `composition` → (own deliveries share vector,
   enemy armours share vector, observed enemy_killed_value / duration, own_lost_value / duration).
2. **Credit attribution (EM)**: each enemy kill's value is credited across the attacker's delivery
   families in proportion to that side's prior-weighted damage share against the victim's armour
   class; each own loss is credited across the enemy's delivery families likewise. Iterate
   (~20 rounds, deterministic order): measured damage dealt by cell (d,a) accumulates; converges
   to the MLE of per-cell rates (the Stanescu regulariser's extension).
3. **Shrinkage**: `C[d][a] = (measured + K · prior[d][a]) / (expected + K)` with pseudo-evidence K
   (default 5000 value units — same constant style as `SHRINK_VALUE` in `fit_arsenal_priors.py`).
   Thin cells stay at the pipeline prior; a cell needs its own evidence to move.
4. **Hierarchical pooling** for timing/response tables: global → game family → faction → matchup;
   each level shrinks toward its parent proportional to sample count.
5. **Bounds**: every correction clamps to [500, 2000] thousandths before write (mirrors
   `EngagementPriorsBotModuleInfo.MinCorrectionMilli/MaxCorrectionMilli` on the consumer side —
   the file never inverts a fight even if the fit is wrong).
6. Determinism: fixed iteration order, integer/decimal math or documented float rounding, sorted
   output — same logs in, same file out (`check_determinism` precedent exists in tools/balance).

## 5. Output file

`mods/cameo/ai/learned/engagement_priors.yaml` — new file, committed, reviewed per fit
(DESIGN §19.2: release reads the committed file; the fit runs only on dev).

```yaml
# GENERATED by tools/ai/fit_engagement_priors.py — do not edit by hand.
BotEngagementPriors:
        Schema: 1
        LedgerHash: <sha256 over sorted docs/balance/*.json raw ledgers>
        FittedFrom: <N engagements / M matches>
        AttritionExponentMilli: 1000
        # per-cell residual thousandths on the resolved Versus prior; absent cell = neutral
        DeliveryArmour@<delivery>__x__<armour>: <milli>
        ...
        DefenceState@<delivery>: <milli>
        IntoDefencesMilli: <milli>
        AttackTiming@<enemy_faction>: p10_tick,p50_tick,p90_tick
        Response@<own_faction>: p50_ticks,p90_ticks,army_dist_cells
        SuicideIndex@<mine>__vs__<theirs>: <milli>
```

Versioning against the balance ledger: `LedgerHash` plus, per cell, the prior the cell was fitted
on is recoverable from the ledger itself. At load the consumer recomputes current priors:
`applied[d][a] = clamp(current_prior[d][a] × residual[d][a])`. A cell whose pipeline prior moved
since `LedgerHash` is marked stale and reverts to neutral until the next fit — a rebalance
discounts old fits automatically instead of poisoning them (research doc §"rebalances reset only
the residuals"; DESIGN §19.2 fingerprint ruling).

Determinism: the file is committed, so every client loads identical bytes; parsing is
`MiniYaml.FromStream` (same as `ArsenalPriors.Parse`) — no randomness, no I/O variance. Read once
at match start (frozen, §19.2); bots run host-only so there is no sync surface regardless.

## 6. Who reads it

- **Tier-2 veto (consumer, already built)**: `CombatVetoEval.Predict` multiplies
  `DamagePerTickAgainst` by `priors.CorrectionMilli(attacker, target)` on the own side.
  `EngagementPriorsBotModule` (NOVA branch) currently maps that call to per-unit-type
  `TradePercent` from `arsenal_priors.yaml`; its own comment says a finer attacker×target table
  lands later "without an API change". Spec'd consumer change (small): extend the module to
  prefer the delivery×armour table when the new file exists — map `BotUnitProfile` → its main
  warhead family × `target.Armor` → `C[d][a]`; fall back to `TradePercent`, else 1000.
- **Later tiers**: the tier-5 engagement network's inputs include "fog-honest ratios of tier-1
  strength split by range band" (research doc) — same provider seam.
- `BotLearnedPriors` (production weighting) keeps `arsenal_priors.yaml` — unchanged.

## 7. The switch

Increment group `AO_tier1_priors` (letter provisional — next free group at merge time), arming
condition `tier1_priors` on the priors provider (`genericbot && tier1_priors`, classic never sees
it — same seam as `combatveto`). Default OFF: with the group unarmed the provider is disabled and
`CorrectionMilli` returns 1000 — pure `BotCombatPredictor`, bit-identical.
(`tools/ai/increment_switches.yaml` gets one group entry; the EL `composition`/`enemy_faction`
log additions are record-only and need no switch.)

## 8. Files

**Create**
- `docs/design/TIER1_FITTER_SPEC.md` — this spec.
- `tools/ai/fit_engagement_priors.py` — the fitter (stdlib only).
- `tools/tests/test_fit_engagement_priors.py` — unit tests.
- `mods/cameo/ai/learned/engagement_priors.yaml` — first fitted file (after smoke fit).

**Modify**
- `OpenRA.Mods.Cameo/Traits/BotModules/EngagementLogBotModule.cs` — emit `composition` in
  seen/truth blocks and `enemy_faction` in the header (record-only; no decisions read it).
- `docs/design/AI_MATCH_LOG.md` — document the new fields.
- `docs/design/AI_ARCHITECTURE.md` — new §12.32 describing tier 1 (short, §12.30/§12.31 style).
- `OpenRA.Mods.Cameo/Traits/BotModules/EngagementPriorsBotModule.cs` — serve the new table
  (consumer-side; coordinates with NOVA's branch — lands after/incorporates it).
- `tools/ai/increment_switches.yaml` — group `AO_tier1_priors`.

## 9. Acceptance tests

1. **Unit tests** (`tools/tests/test_fit_engagement_priors.py`, pytest — `test_fit_arsenal_priors.py`
   precedent):
   - synthetic records where one delivery over/under-performs → cell moves the right direction;
   - thin cells stay at prior under shrinkage; bounds clamp at [500, 2000];
   - missing `composition` → record skipped, no crash; skirmishes excluded;
   - stale `LedgerHash` → affected cells revert to neutral;
   - determinism: two runs on the same fixture produce byte-identical yaml.
2. **C# side** (consumer change): extend `ArsenalPriorsTest`-style coverage —
   parse the new file, delivery×armour lookup, fallback chain (cell → TradePercent → 1000),
   clamp bounds; `CombatVetoEvalTest` prior-shift vector still passes.
3. **Smoke fit**: run the fitter on existing logs — `C:/tmp/elsmoke-out/Logs` +
   `elsmoke-out-2v2` hold `engagement/1` records (~420 lines incl. posture). Records predate
   `composition`, so acceptance = clean parse, correct skips, sane report; a short batch with the
   new fields produces a valid `engagement_priors.yaml` (coordinator runs batches — hand in the
   fitter + a `--write` preview).
4. Report mode (`--json`) prints per-cell evidence counts so the reviewer sees which cells moved
   and why (the "review the new file" gate of §19.2).

## 10. Open questions for the coordinator

1. Composition logging adds ~1–3 KB per engagement record. Acceptable, or cap at top-N types?
2. `enemy_faction` in the header: faction is lobby-public — confirm no fog objection. (Pairing
   fallback exists either way.)
3. Own output file vs folding residuals into `arsenal_priors.yaml` — spec keeps them separate
   (different granularity and consumers); OK?
4. Switch letter `AO` is provisional against whatever lands first (NOVA used `AN_combat_veto`,
   DAWN `AN_tier3_bandits` on branches).
5. EMBER's HANDOFF slot vs this assignment — flagging so the queue doesn't double-assign.
