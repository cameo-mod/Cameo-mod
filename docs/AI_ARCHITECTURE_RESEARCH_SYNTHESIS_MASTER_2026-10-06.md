# Cameo AI architecture and external-research synthesis — 2026-10-06

## Purpose and authority

This is the single AI-focused reading of two supplied external reports together with the
project's existing architecture and research.  It is a decision document, not a replacement for
binding design, implementation, or maintainer-ruling documents.

Sources merged here:

1. `deep-research-report.md` — a statistical-learning architecture and training proposal.
2. `deep-research-report-2.md` — a broader design framework, including an AI roadmap and product
   priorities.
3. `docs/design/AI_ARCHITECTURE.md` — the authoritative current architecture, contracts, module
   seams, and implemented/in-progress phases.
4. `docs/design/AI_DEEP_RESEARCH.md`, `AI_LEARNING_RESEARCH_2026-10-03.md`, and fleet research
   (`RESEARCH_2026-10-05_rts_ai_learning.md`, `LEARN_CATALOG_2026-10-06.md`,
   `MAP_FEATURES_2026-10-06.md`, `OPPONENT_FEATURES_2026-10-06.md`).

When sources disagree, project maintainer rulings and verified current code win.  A claim in an
external report is a proposal or a lead until it is checked against the checked-out tree.  In
particular, counts of modules, branches, data sets, and implementation status in the external
reports are snapshots and must not be used as a release claim without rerunning the relevant
audit.

## Decision

Cameo should build a **modular, fog-honest, offline-fitted learning plane around the existing bot
owners**.  It should not build a monolithic self-learning bot, train an end-to-end whole-game
policy, persist a profile of a human player, store map identities, or let a shipped client alter a
model during a match.

The resulting architecture is:

```text
public map rules + legal own start        visible enemies + decaying contact memory
              |                                           |
              v                                           v
       MapFeatureVector                         OpponentFeatureVector
              \                                           /
               \                                         /
                v                                       v
   reviewed, versioned integer learned artifacts and hand-authored defaults
                                |
                                v
       advisory interfaces: fight / economy / build / plan selection
                                |
                                v
existing decision owners: CombatVeto, UnitBuilder, BaseBuilder, SquadManager, Master
                                |
                                v
                       orders -> OpenRA world

write-only anonymous logs -> offline fitters -> validation -> review -> committed artifact
```

The modules remain responsible for issuing orders.  Learned providers publish bounded inputs;
they do not create a competing execution path.

## The non-negotiable contract

The sources strongly converge on these rules.  They also preserve the current architecture's
`Observe -> Synthesize -> Plan -> Arbitrate -> Execute -> Verify` separation.

| Contract | Practical rule |
|---|---|
| One owner per decision | `UnitBuilder`, `BaseBuilder`, and `SquadManager` retain production, construction, and squad authority. A learner is a provider or veto input, never a second order issuer. |
| Fog honesty | Runtime features use only visible state, remembered sightings, public map/rules facts, and inference derived from them. Truth data may score an offline record, never become a live feature. |
| No identity learning | No account, player name, slot history, map name, UID, hash, coordinate lookup, or persistent opponent record. Opponent attributes exist only for the current match. |
| Frozen artifacts | Runtime reads reviewed data loaded before play. Fitters run only in the development pipeline. The match never rewrites the model. |
| Deterministic, bounded runtime | Integer/fixed-point arithmetic, sorted iteration and explicit tie-breaks; 64-bit intermediates and clamps; a missing, invalid, sparse, or disabled model returns the present hand-authored behaviour. |
| Switch-off equivalence | Every consumer is behind its own default-off increment. Fixed-seed order-stream parity is required before promotion. |
| Traceable evidence | Artifacts carry rules, feature, reward and role schema versions plus source counts and validation results. No cross-schema reuse by accident. |

`AI_ARCHITECTURE.md` also establishes a separate simulation boundary: bot reasoning is unsynced
and may affect the world only through orders.  Anything that changes synced personality state must
use the established deterministic order round-trip.  An external suggestion to grant/revoke a
condition directly from an unsynced bot module is therefore rejected.

## What the research confirms

### 1. Learn by signal density, not by fashion

The first report's most useful principle matches `AI_LEARNING_RESEARCH_2026-10-03.md`: fit many
parameters only when each match supplies many relevant observations; tune few parameters when the
reward is one noisy match result.  The adopted division is:

| Learning family | Suitable targets | Method | Current direction |
|---|---|---|---|
| Measured | combat calibration, effective role/unit value, prediction residuals | shrinkage integer regression / Bayesian estimate | LEARN-FIGHT and its precursors |
| Threshold | engage/retreat, expansion risk, credit floor | Bayesian threshold with hard clamp | fight then economy catalog rows |
| Discrete choice | opening, plan, posture arm | hierarchical conservative bandit | only after adequate logged exploration |
| Small continuous tune | bounded build/economy knobs | paired A/B, SPSA or coordinate search | offline only, low dimension |
| In-match adaptation | transient caution or pressure | bounded rolling estimate | never persisted |
| Fixed | fog legality, decision ownership, bounds, determinism | no learning | permanent invariant |

This accepts the reports' recommendations for statistical residuals, hierarchical priors,
confidence gates, and narrowly owned rewards.  It rejects their implication that a large parameter
count is a goal.  A fitted combat model may have many coefficients because engagements are dense;
game-level tuning should remain deliberately small.

### 2. Strategy needs abstraction and commitment

The research support for case-based opening choice, plan bandits, and strategy switching is useful
only at the strategic layer.  The bot should select or update a posture from a fog-honest snapshot,
then hold it long enough to be legible.  Minimum holds, hysteresis, emergency overrides, and a
logged switch reason are required.  Tactical facts such as air presence generally alter
composition/priority, not the coarse personality.

The external reports correctly identify dynamic switching as more capable than a match-long random
personality.  The project architecture already specifies the safe mechanism and ownership for it:
the Master owns main target, personality, and the published snapshot; specialist modules consume
it.  This remains a staged design/implementation path, not evidence that every suggested switch
is already live.

### 3. Offline evaluation must precede promotion

Both research tracks recommend more than raw win rate: engagement trade, prediction error,
objective outcome, safety/order-rate, duration and variety.  Adopt that principle.  Match-level
splits must keep all rows from a match together; evaluation should additionally hold out contexts,
including faction matchups and ranges of continuous feature values.  A candidate progresses only
through schema validation, offline fit checks, conservative confidence evidence, paired A/B and
reviewed artifact commit.

The first report's suggested action-propensity logging and off-policy evaluation are valuable
future enhancements.  They are not a reason to skip paired validation: propensity scores help
triage candidates but do not establish safe deployment by themselves.

## Current architecture versus recommendations

| Area | Current architecture/research | Synthesis decision | Next concrete work |
|---|---|---|---|
| Decision ownership | Explicit single-owner pattern and advisory seams | Preserve | Audit new modules for duplicate order authority. |
| Fog and scouting | Fog/contact memory is foundational; old paths require continuing audit | Strengthen | Make every new feature seen-side; add hidden-state equivalence tests and scouting dependencies. |
| Match telemetry | P0 anonymous logging and engagement scoring exist/in progress; current schemas are the compatibility boundary | Extend carefully | Add only versioned, anonymous decision/context fields needed by a named fitter. |
| Combat learning | Predictor/priors and LEARN-FIGHT target calibration/value/thresholds | Adopt first | Finish switch-gated fight consumers and validate against existing predictor bounds. |
| Economy learning | Existing builders/limits plus ECON-A/ECON-B seams | Adopt second | Fit expansion, refinery/harvester, production-income and credit-float residuals; retain safety floors. |
| Map context | Maintainer superseded map buckets with `MapFeatureVector/v1` | Adopt continuous vector | Implement public, per-start vector once; fit smooth bounded functions, never a map lookup. |
| Opponent context | Maintainer requires a per-match, identity-free vector | Adopt with confidence | Implement observed-only vector and confidence; dispose it at game end. |
| Plan/posture selection | Existing design supports master-led switching; research supports conservative bandits | Defer until logs mature | Log episodes and support first; then trial a bounded initial-plan prior. |
| Deep RL/LLM runtime | Existing deep research already rejects whole-game deep RL for now | Reject/defer | LLMs may assist offline analysis, documentation, or fixture generation only. |

## Coherence and contradiction register

| Subject | Conflict | Resolution |
|---|---|---|
| Per-opponent memory | Older architecture/research and outside RTS examples describe persistence per human opponent. Current maintainer ruling is identity-free attribute learning. | Do not store a player key. Use public enemy faction/doctrine plus a transient, confidence-weighted opponent vector; discard it after the match. |
| Map conditioning | Research examples often key data by map; an older fleet concept used discrete map classes. | No map ID or bucket table. Use `MapFeatureVector/v1` and a smooth, bounded model; all map-specific geometry remains live. |
| Runtime model updates | General adaptive-AI literature sometimes persists live outcomes. | Shipped builds never mutate learned artifacts. Only short-lived in-match estimates may vary, inside fixed bounds. |
| Omniscient training data | External report permits truth state for lower-variance offline scoring; runtime must be fog-honest. | Accept truth only as a target/score where the schema labels it as truth. Runtime feature vectors and model inputs remain seen-side. Confirm this boundary in each fitter test. |
| Dynamic personalities | CN-inspired examples directly mutate conditions from a bot module. | Reuse the safeguards (hysteresis, cooldown, emergency logic), not that mechanism. Use the deterministic order bridge defined by the architecture. |
| Claim of implemented features | Both external reports describe pieces as present based on snapshots and proposals. | Treat as unverified until source/code/audit evidence shows it in the current branch. Status labels in this document deliberately say implemented, in-flight, or design separately. |
| Whole-game neural policy | Some cited systems achieved strong play with major distributed training. | Do not pursue for the main bot now. The sample, compute, interpretability and fairness costs do not fit Cameo's current needs. |
| Broad product roadmap | Report two proposes ladders, campaign, doctrines, and community timing. | Keep its stability-first message and player-facing clarity goals; route the feature-specific proposals to binding roadmap/design documents, not the AI architecture. |

## External framework findings outside the bot

The second report is intentionally much broader than AI.  Its important project-level proposals
are retained below so that this synthesis does not quietly discard them, but they are **not adopted
as binding AI architecture**.  Their owning design documents must decide them.

| External finding | Coherent use in Cameo | Status here |
|---|---|---|
| Stability before feature breadth | Treat boot, crash, performance and multiplayer/desync evidence as release gates; do not use learning experiments to mask a broken baseline. | Adopted as delivery policy. |
| Shared mechanics, legible faction doctrine, then spectacle | Keep AI rules-derived and faction-aware, while gameplay identity remains owned by the faction, balance and formula documents. | Aligns with existing separation of policy from content. |
| Counter, telegraph, cost, and opportunity cost | Use these as evaluation dimensions for strategy and balance changes, alongside win rate; bots should learn to recognize visible counterplay, never hidden state. | Adopted evaluation principle. |
| Soft-counter/role readability | Feed consumers role-derived facts from rules and public observations; do not train on ad hoc unit-name heuristics. | Aligns with the current role and arsenal direction. |
| Doctrines and broad faction/campaign proposals | Assess separately for scope, rules compatibility, assets, and player testing. They should become bot inputs only after their gameplay contract is stable. | Deferred to owning design/roadmap documents. |
| Ladder, tournament, community and service roadmap | Useful product hypotheses, but operational commitments require maintainer capacity, legal review, and current repository planning. | Not an AI implementation decision. |

## Approved target data model

### Context at runtime

Each learned policy consumes a narrow tuple:

```text
(own faction, public enemy faction/doctrine, current fog-honest decision context,
 MapFeatureVector?, OpponentFeatureVector? with per-field confidence)
```

`MapFeatureVector` is created at load from public geometry and the bot's legal own start.
`OpponentFeatureVector` is recomputed during the match from sightings and visible effects only.
Both have explicit schema versions and missing-value bits.  The optional providers return no
adjustment before their increments are enabled; their absence is ordinary and must leave current
behaviour exact.

The recommended artifact form is an integer additive ridge model with a reviewed, capped hinge
registry.  Discrete choice surfaces may instead use a capped anonymous prototype set with fixed
weighted-L1 smoothing.  Both shrink sparse matchup evidence through matchup -> faction -> global,
have file-size caps, and never retain a row per map or human opponent.

### Logs and fitting

Logs should record the decision context, declared eligible actions, selected action, model/schema
versions, confidence, and later outcome for the owner of that decision.  They must label which
fields are observed versus truth.  Fitters reject mixed rule, feature, reward, or role schemas.
Every emitted artifact carries at least:

```yaml
LearningManifest:
  RulesHash: ...
  AiSchemaVersion: ...
  FeatureSchemaVersion: ...
  RoleSchemaHash: ...
  RewardSchemaVersion: ...
  SourceMatches: ...
  SourceObservations: ...
  Validation: ...
  ParentArtifact: ...
```

This is the valuable provenance recommendation from the first report.  It prevents stale learning
data from silently surviving a balance or schema change.

## Ordered action plan

1. **Protect the baseline.** Finish the P0 log/parity investigation before interpreting learned
   results. Keep boot, deterministic order-stream, fog-honesty, no-identity and size audits as
   release gates. A learning feature that changes orders while disabled is a defect, not an
   experimental result.
2. **Complete the fight trio.** Ship only reviewed, default-off predictor calibration, effective
   value residuals and engage/retreat thresholds. Validate each against the existing combat
   predictor and slices with low observation confidence.
3. **Complete the economy quartet.** Use the same artifact and manifest discipline for expansion,
   harvester/refinery, production-versus-income, and credit-float decisions. Do not duplicate the
   ECON-A/ECON-B authorities or override their hard safety floors.
4. **Make provenance explicit.** Add the manifest/learnability registry before the catalog grows.
   A registry entry identifies owner, bounded range, feature schema, reward, scope, fitter, sample
   floor, and switch. It prevents an accidental learnable gameplay invariant.
5. **Implement context providers as telemetry first.** Land map and opponent vectors with no
   gameplay consumer, prove their integer/fog/no-identity properties, then permit an optional
   consumer behind its own increment.
6. **Add conservative plan learning last.** When episode logs and support are adequate, trial an
   initial-plan bandit or contextual prior with a hand-authored fallback, lower-confidence floor,
   hold time, and a switch/veto diagnostic. Do not let it select a strategy merely because a
   sparse historical cell looks good.
7. **Use research outside AI correctly.** Apply the second report's stability gates, readability,
   counter/tell/cost discipline, and staged release thinking to product planning. Assess its
   concrete faction, campaign, and service proposals in their owning design documents, with
   repository evidence, rather than silently making them part of the bot architecture.

## Promotion gates

A model or new consumer is promotable only when all apply:

- The switch-off fixed-seed order stream is bit-identical.
- The runtime inputs pass the fog-honesty and no-identity grammar audits.
- Integer overflow, sort order, missing/sparse evidence, schema mismatch and fallback tests pass.
- The shipped artifact stays below its bounded storage budget and is independent of map count and
  player count.
- Offline evaluation uses match-level splits and reports support/confidence; paired A/B clears the
  agreed lower-confidence floor without a regression in safety, invalid orders, variety or game
  duration.
- A reviewer can reconstruct the fitter input, manifest, bounds, switch, and consumer from the
  committed files.

## Explicit non-goals

- A runtime LLM, remote service, or neural controller issuing RTS orders.
- A replay of hidden truth as a live tactical input.
- Persistent storage about a person, account, seat, named map, map hash, or historical coordinate.
- Automatic promotion or mutation of a model after a public match.
- Replacing the current ownership graph with a new central order issuer.

## Result

The external research adds useful statistical discipline, conservative evaluation ideas, and a
strong stability-first product reminder.  The current Cameo architecture supplies the missing
engineering constraints: explicit order ownership, fog-honest observation, deterministic
integration, audited artifacts and switch-gated rollout.  The coherent plan is to combine those
strengths in small, observable increments, beginning with fight and economy residuals and only
later using continuous map/opponent context for bounded advice.

---

# Integrated reconciliation with the statistical-parametric master report

This section records the exact merge decisions after reading the actual
`AI_ARCHITECTURE_RESEARCH_SYNTHESIS_2026-10-06.md`, rather than relying on its earlier summary.

The architecture-and-research synthesis above is the **decision spine**. The statistical material
below is an implementation/research annex. If a detailed statistical recommendation conflicts with
the decision spine, the decision spine wins unless a later binding Cameo ruling explicitly changes it.

## R1. Conflicts found and resolved

| Subject | Statistical-parametric report | Actual architecture synthesis | Final merged ruling |
|---|---|---|---|
| Map conditioning | Suggested hierarchical pooling by “MapFeatureVector/v1 continuous context” in several candidate parameter rows. | Explicitly forbids map name, UID, hash, coordinate lookup, or bucket table; adopts `MapFeatureVector/v1`. | **Remove MapFeatureVector/v1 continuous context/map-ID learning.** Map context is a continuous public-geometry feature vector with no stable map key. |
| Map provenance in learning logs | Earlier schema proposal included a map hash for reproducibility. | Explicit non-goal includes persistent storage about named map or map hash. | **Do not persist map identity/hash in the learning record or artifact.** Pair experiments with a batch-local opaque pair/cell token that carries no reusable map identity. |
| Opponent history | Earlier report already rejected per-human profiles but discussed faction/matchup pooling. | Explicitly forbids player/account/slot history and persistent opponent record; permits public faction/doctrine plus transient observed vector. | Keep faction/matchup priors only where current design permits; all behavior features are current-match and identity-free. Never create a human-opponent key. |
| Runtime adaptation | Earlier report allowed bounded match-local adaptation. | Frozen artifacts are mandatory, but the adopted learning table explicitly permits transient bounded in-match caution/pressure that is never persisted. | No conflict after terminology fix: **runtime controller state may adapt; the learned model/artifact may not train or rewrite itself.** |
| Runtime numeric model | Earlier report described statistical formulas in real-valued mathematical notation. | Runtime target is integer/fixed-point, sorted, clamped and deterministic. | Offline fitters may use reproducible statistical math internally; emitted artifacts and runtime evaluation use approved integer/fixed-point representation. |
| Strategy learning | Earlier roadmap placed plan/opening learning relatively early. | Ordered plan puts plan learning last, after P0, fight, economy, registry, and telemetry-only map/opponent providers. | **Architecture synthesis ordering wins.** Contextual/plan learning is Step 6. |
| Contextual bandits/OPE | Earlier report recommended propensity logging and contextual bandits. | Propensity/OPE is a valuable future enhancement but may not replace paired validation. | Keep propensity logging as future-facing telemetry; paired A/B and promotion gates remain mandatory. |
| Parameter count | Earlier report gave V1/V2/mature direct-DOF numbers. | Explicitly rejects “large parameter count” as a goal and says game-level tuning stays deliberately small. | Treat counts only as **engineering ceilings/catalog scale**, never targets. Tune one coherent low-dimensional group at a time. |
| Target context model | Earlier report was algorithm-agnostic among hierarchical regression/bandits. | Approved runtime form is integer additive ridge with reviewed capped hinges; discrete surfaces may use capped anonymous prototypes with fixed weighted-L1 smoothing. | Use that approved form for `MapFeatureVector`/`OpponentFeatureVector` consumers first. Contextual bandits are later experiments, not the default context model. |
| Combat learning ownership | Earlier report emphasized rich engagement priors. | Ordered action plan defines fight calibration/value/threshold trio and single-owner seams. | Keep separate statistical writers by quantity; no competing combat authority or order issuer. |
| Registry | Earlier report had a large parameter catalog. | Requires manifest/learnability registry before catalog growth. | **Catalog is research inventory; registry is permission.** No learned value becomes live without an approved registry entry and audit. |
| Non-AI findings | Earlier report mostly ignored broader game-design research. | Explicitly routes faction/campaign/service ideas to owning design documents. | Preserve that separation. |

## R2. Canonical execution order

The merged document adopts the architecture synthesis's order without modification:

1. Protect and prove the P0 baseline/parity/logging boundary.
2. Complete the fight-learning trio.
3. Complete the economy-learning quartet.
4. Land the learning manifest and learnability registry/audit.
5. Land `MapFeatureVector` and `OpponentFeatureVector` as telemetry-only providers first.
6. Add conservative plan/posture learning only after the evidence base is mature.
7. Route non-AI research to its proper design owners.

All detailed parameter and algorithm recommendations in the appendices are subordinate to this order.

## R3. Registry contract

The earlier parameter catalog becomes a **registry seed**, not a list of automatically approved knobs.
A live learnable entry should contain at least:

```yaml
Learnable:
  Id: stable_parameter_or_family_id
  Owner: module_or_fitter
  RuntimeConsumer: advisory_interface_or_existing_decision_owner
  Artifact: file_and_key
  Switch: default_off_increment
  Representation: integer_or_fixed_point
  Default: authored_default
  Minimum: hard_min
  Maximum: hard_max
  Constraints:
    - relational_or_safety_rule
  FeatureSchema: version
  Measurements:
    - anonymous_fog_honest_field
  RewardOrFit: named_objective_or_estimator
  MinimumEvidence:
    Kind: engagements|matches|pairs|posterior_support
    Value: fitter_specific_rule
  Scope: global|faction|matchup|continuous_context
  IdentityPolicy: forbidden
  Staleness: fingerprint_or_schema_dependency
  Fallback: authored_default_or_parent
```

The audit rejects:

- an artifact key with no registry entry;
- a value outside its hard range;
- a violated relational constraint;
- player/map identity;
- an unknown feature/reward/role schema;
- a consumer without its declared switch/fallback;
- evidence below the registered floor;
- duplicate ownership of the same decision.

## R4. Current implementation evidence versus acceptance state

The following distinction must be maintained throughout future edits:

- **implemented in code** is not the same as
- **wired behind an increment** is not the same as
- **A/B validated** is not the same as
- **promoted/default-on**.

The earlier repository audit verified many learning and spatial modules in the canonical tree, including
engagement logging, combat veto, plan-bandit machinery, build-order tuning, bounded match-local adaptation,
zone topology, influence layers, scouting, utility axes, Director/scale systems and the AI experiment tools.
The architecture synthesis is correct to keep status labels conservative: every release claim must be
rechecked against the current checked-out branch and current audits.

Recent non-master work also demonstrates the intended sequence:

- P0 privacy/anonymization and post-game signature work;
- offline fight-threshold learning behind its own increment.

Those are implementation evidence, not proof of promotion to `master` or default-on behavior.

## R5. Parameter-count policy

Do not optimize toward a parameter count.

Use these **maximum working budgets**, not goals:

| Layer | Working budget |
|---|---:|
| One SPSA/coordinate group | normally 4–12 scalar DOF |
| Deliberately coupled expert group | up to ~16; >16 requires explicit justification |
| Absolute experimental ceiling for one simultaneous black-box tune | 24 |
| Discrete arms exposed in one decision | keep small enough for meaningful support; pool hierarchically |
| Measured residual cells | may be hundreds/thousands when each engagement directly labels them and sparse cells shrink/fallback |
| Runtime neural weights | none in the current target architecture |

The old V1/V2/mature totals are therefore retained only as catalog-size thought experiments, not milestones.

---

# Statistical implementation annex

The following sections preserve the important unique material from the parametric research while applying
the exact architecture contract above.

## Appendix A — External statistical research details

## 5.1 Learned combat models: highest immediate value

Stanescu, Barriga & Buro's StarCraft work is especially relevant because it learns **corrections to an analytical combat model**, not an entire policy. Their central lesson transfers cleanly:

- derive a physically/statistically meaningful prior from unit properties;
- fit residual strengths from actual engagements;
- regularize toward the prior;
- use the calibrated model inside attack/retreat decisions.

Uriarte & Ontañón similarly show that efficient combat abstractions can provide useful outcome estimates without full simulation.

**Cameo fit:** exceptionally strong. Cameo already has structured weapon/Versus data and a balance pipeline, so its stat-derived prior is richer than a generic unit-id strength table.

## 5.2 Bayesian and hierarchical opponent/strategy models

RTS strategy inference research under partial observation demonstrates that enemy intent can be estimated from sparse, fogged observations. Bayesian opening models and HMM strategic-state models are useful mainly as **features and priors**, not as replacements for the bot.

**Cameo fit:**

- use public faction when known;
- pool Random/unknown faction to higher-level priors;
- classify a small number of strategic states from fog-honest evidence;
- update belief inside a match;
- do not persist individual-human identity.

## 5.3 Thompson sampling and conservative bandits

Bandits are appropriate for choices that are naturally discrete:

- personality;
- opening;
- plan overlay;
- composition package;
- optional doctrine.

Cameo's current continuous-reward PlanBandit is better matched to its EL score than a binary Beta posterior.

Safety literature on conservative bandits and safe policy improvement adds a useful principle:

> exploration should be constrained by a baseline or safety floor when uncertainty is large.

Cameo already has an LCB floor. That should remain.

## 5.4 SPSA and stochastic black-box optimization

SPSA is well suited to Cameo because one gradient estimate costs two experiment arms regardless of dimension. But its efficiency does not mean “tune everything together.”

Best practice for Cameo:

- use common random numbers / paired maps, spawns, opponents and seeds;
- normalize parameters;
- perturb coherent groups;
- keep hard bounds;
- preserve an untouched validation set;
- require a final league gate after the adaptive optimization loop.

Current Cameo's log-multiplier implementation and deterministic SHA-256 perturbations are strong choices.

## 5.5 Bayesian optimization / SMAC

Bayesian optimization is attractive when:

- evaluation is very expensive;
- dimensionality is low;
- parameters interact nonlinearly;
- an SPSA group proves too noisy.

Gaussian-process BO is best kept to small dimensions. Tree/forest model-based optimizers such as SMAC are more natural if future groups contain mixed categorical and numeric values.

**Recommendation:** fallback/specialist tool, not the default training loop.

## 5.6 CMA-ES

CMA-ES is useful where:

- a small vector has strong interactions;
- local gradient estimates are unreliable;
- evaluation can be done in cheap tactical scenarios.

The clearest Cameo candidate is a formation/micro vector, not global economy.

**Recommendation:** optional specialist optimizer after a deterministic skirmish harness exists; full-game validation remains mandatory.

## 5.7 Contextual bandits and off-policy evaluation

This is the most important research addition not fully reflected in the current architecture.

Once discrete decisions depend on context, logs should record:

- action/arm;
- policy version;
- eligible action set;
- selection probability (propensity);
- context features;
- outcome.

That enables inverse-propensity and doubly robust estimators to evaluate candidate policies using historical exploration data. Without logged propensities, retrospective off-policy evaluation is biased or impossible.

**Recommendation:** add propensity logging before expanding exploration.

## 5.8 League/self-play lessons

AlphaStar's directly useful lesson is not its neural architecture. It is training diversity:

- current bot;
- past versions;
- exploiters;
- alternative personalities;
- multiple maps/factions.

A candidate that only beats one baseline can overfit a single strategic niche.

Cameo already has `run_league.py`; use it as the release gate for learned files.

## 5.9 Whole-game deep RL

Whole-game learned policies require far more data and hardware than Cameo's current development loop can justify. They are also harder to explain, audit, preserve across faction additions and reconcile with the project's modular ownership rules.

**Recommendation:** remain deferred.

---

## Appendix B — Candidate learnable-parameter catalog (registry seed, not approval)

The following catalog treats a “parameter” as a semantically meaningful scalar/vector or sparse table. It deliberately distinguishes raw stored coefficients from independent optimization degrees of freedom.


## 8.1 A. Combat measurement
| Parameter / family | Owner | Context | Type / safe bounds | Learning route | Update | Reward/evidence | Pooling | Safety / dependency |
|---|---|---|---|---|---|---|---|---|
| delivery_armour_residual[d,a] | EngagementPriorsBotModule / BotCombatPredictor | Damage effectiveness by delivery family × armour | positive multiplier; 0.5–2.0 | Tier 1 empirical-Bayes fit | offline batch | prediction residual / value lost | global cell; sparse family residual later | staleness tied to resolved Versus PriorPct |
| global_combat_scale | EngagementPriorsBotModule | global residual calibration | positive multiplier; 0.5–2.0 | Tier 1 | offline batch | observed/expected loss | global | must not absorb balance errors blindly |
| attrition_exponent | BotCombatPredictor | Lanchester/order-of-attrition shape | continuous ~0.5–2.0 | Tier 1 fit | offline batch | outcome likelihood/error | global first | tight regularization; one scalar |
| into_defences_milli | EngagementPriors / predictor | penalty/correction attacking static defence | 0.5–2.0 | Tier 1 | offline batch | observed vs predicted defence fights | global→family | seen inputs only live |
| defence_state_residual[delivery/state] | EngagementPriors | garrisoned/deployed/static defence states | 0.5–2.0 | Tier 1 | offline batch | defence engagement residual | global sparse | minimum evidence |
| range_band_residual[delivery,range_band] | future richer priors | range advantage omitted by base predictor | 0.7–1.4 suggested | Tier 1 later | offline | prediction error stratified by range | global | add only if residual analysis shows structure |
| hp_fraction_residual[class,band] | future richer priors | damaged-force effectiveness | 0.7–1.3 | Tier 1 later | offline | survival/trade residual | global | avoid double counting HP already in predictor |
| visibility_factor[phase] | Master/CP calibration | remembered army vs offline truth by phase | 0.1–1.0 | Tier 1 diagnostic | offline | seen/truth gap | global→MapFeatureVector/v1 continuous features (no map identity or bucket key) | never feed truth directly live; use only learned conservative prior |
| attack_timing_quantiles[enemy_faction,phase] | opponent-model feature | typical enemy attack timing | quantiles | Tier 1 measured | offline | defend/attack timestamps | global→family→enemy faction | public faction only |
| response_time_quantiles[own_faction,phase] | Master / defence planning | own response latency | quantiles | Tier 1 measured | offline | first hurt→first mobile damage | global→own faction | measurement first; don't optimize latency by hidden info |
| suicide_index[matchup] | Veto diagnostics | value entering defences vs defences killed | nonnegative ratio | Tier 1 measured | offline | EL tactics/outcome | family→matchup | diagnostic can become veto feature |
| unit_role_trade_residual[role,enemy_role] | arsenal/production | role effectiveness | 0.5–2.0 | Tier 1 empirical Bayes | offline | value traded/lost | global→family→matchup | prefer roles over actor IDs |
| cause_of_loss_share[role] | production/defence advisors | what actually killed army/base | simplex | Tier 1 | offline | death attribution | global→matchup | minimum evidence |
| prediction_calibration_intercept | BotCombatPredictor | calibrate predicted ratio to realized outcome | bounded scalar | regression | offline | Brier/log loss / residual | global | holdout calibration |
| prediction_calibration_slope | BotCombatPredictor | over/under-confidence | 0.5–2.0 | regression | offline | Brier/log loss | global | holdout calibration |

## 8.19 B. Discrete strategy
| Parameter / family | Owner | Context | Type / safe bounds | Learning route | Update | Reward/evidence | Pooling | Safety / dependency |
|---|---|---|---|---|---|---|---|---|
| personality_arm | PlanBanditBotModule | rush/turtle/tech/expansion/steamroller/guerrilla | categorical | Tier 3 Student-t Thompson | match-start | continuous EL/match composite | any→family→faction→matchup | LCB safety floor; freeze per match |
| plan_overlay_arm | PlanBanditBotModule | named bounded knob overlay | categorical | Tier 3 Student-t Thompson | match-start | EL total_milli / match score | same hierarchy | record armed-set survivorship |
| opening_arm | BuildOrderKnobs + PlanBandit integration | authored build category sequence | categorical | Tier 3; continuous/episode reward preferred | match-start | opening episode value + later match regularizer | faction→matchup→plan class | do not duplicate two incompatible opening learners |
| composition_arm | UnitCompositions / UnitBuilder | safe authored composition package | categorical | bandit later | episode/match | role-adjusted trade and objective | faction→matchup | tech eligibility/veto first |
| attack_doctrine_arm | Squad/mission planner | direct/siege/flank/harass | categorical | contextual bandit later | engagement | engagement score | platoon type→context cluster | requires propensity logging and veto |
| support_power_doctrine_arm | SupportPowerBotASModule | hold/coordinate/immediate use | categorical | bandit later | episode | objective swing / value denied | power family | never per individual power until evidence |
| team_role_arm | Team blackboard/TC | ground/air/tech/raider role split | categorical | bandit later | match | team result + marginal contribution | team size/faction family | avoid unstable role thrashing |

## 8.29 C. Build/economy SPSA
| Parameter / family | Owner | Context | Type / safe bounds | Learning route | Update | Reward/evidence | Pooling | Safety / dependency |
|---|---|---|---|---|---|---|---|---|
| knob_tempo | BuildOrderKnobsBotModule | overall build timing multiplier | learned multiplier 0.8–1.25 | Tier 4 SPSA | offline | match+0.5 EL composite | global→family→faction/personality | current SPSA group |
| knob_greed | BuildOrderKnobsBotModule | economy investment | 0.8–1.25 learned overlay | Tier 4 | offline | composite + economy efficiency diagnostics | family→faction/personality | bounded by authored preset |
| knob_production | BuildOrderKnobsBotModule | production infrastructure | 0.8–1.25 | Tier 4 | offline | composite | family→faction/personality | separate from unit actor weights |
| knob_tech | BuildOrderKnobsBotModule | tech investment/timing | 0.8–1.25 | Tier 4 | offline | composite / tech timing | family→faction/personality | preserve faction identity priors |
| knob_defence | BuildOrderKnobsBotModule | defence investment | 0.8–1.25 | Tier 4 | offline | composite / base-loss diagnostics | family→faction/personality | not same as hidden HP bonus |
| knob_power_margin | BuildOrderKnobsBotModule | power headroom | 0.8–1.25 | Tier 4 | offline | brownout/idle/composite | family→faction | hard safety floor against power collapse |
| knob_expansion | BuildOrderKnobsBotModule | expansion appetite | 0.8–1.25 | Tier 4 after consumer verification | offline | composite + expansion ROI | family→faction/personality/MapFeatureVector/v1 continuous features (no map identity or bucket key) | only enable in tuner after its consumer effect is verified |
| knob_support | BuildOrderKnobsBotModule | support/superweapon investment | 0.8–1.25 | Tier 4 | offline | composite | family→faction/personality | support-family specific later |
| react_air_gain | BuildOrderKnobs | strength of response to observed air | bounded ~0.75–1.35× current | Tier 4 group 2 | offline, applied live | post-reaction episodes | global→family | seen air only |
| react_rush_gain | BuildOrderKnobs | response to early pressure | bounded | Tier 4 | offline | defence/trade score | global→family | fog-honest |
| react_turtle_gain | BuildOrderKnobs | response to observed static defence | bounded | Tier 4 | offline | attack quality | global→family | fog-honest |
| react_out_earned_gain | BuildOrderKnobs | response to observed economy lead | bounded | Tier 4 | offline | composite/econ recovery | global→family | seen proxy only |
| react_air_threshold | BuildOrderKnobs | air evidence threshold | current-centered bounded integer | measured first, tune later | offline | classification precision/recall | family | prefer measured timing/value prior |
| react_rush_window | BuildOrderKnobs | early-game window | bounded ticks | measured timing prior | offline | attack timing distribution | family/faction | measure rather than black-box optimize first |
| react_rush_pressure_threshold | BuildOrderKnobs | base pressure needed to react | bounded value | measured→tune | offline | false/true reaction outcome | family | seen only |
| react_turtle_defence_threshold | BuildOrderKnobs | defence value to classify turtle | bounded value | measured→tune | offline | plan classification | family | avoid duplicate enemy-plan classifier |
| react_turtle_share_threshold | BuildOrderKnobs | defence share classification | 20–70% suggested | measured→tune | offline | classification/outcome | global | same classifier should feed OM |
| react_out_earned_pct | BuildOrderKnobs | economy disadvantage trigger | 110–180% suggested | measured→tune | offline | recovery outcomes | family | observed proxy uncertainty |

## 8.50 D. Combat/veto tuning
| Parameter / family | Owner | Context | Type / safe bounds | Learning route | Update | Reward/evidence | Pooling | Safety / dependency |
|---|---|---|---|---|---|---|---|---|
| veto_engage_ratio_pct | CombatVetoBotModule | uncommitted engage safety bar | bounded around 50 | Tier 4 separate group | offline | trade + veto counterfactual score | global→personality/family | keep hysteresis |
| veto_abort_ratio_pct | CombatVetoBotModule | already-committed abort bar | bounded around 35; < engage | Tier 4 | offline | trade/preservation | global→personality | constraint abort < engage |
| veto_launch_ratio_pct | CombatVetoBotModule | wave launch safety bar | bounded around 60 | Tier 4 | offline | launch outcome/match | global→personality/family | fog parity floor retained |
| veto_flee_speed_margin_pct | CombatVetoBotModule | whether retreat can outrun pursuit | 80–130% suggested | measure/tune later | offline | retreat survival | global/platoon type | physical interpretation |
| defence_include_cells | CombatVetoBotModule | remembered static defence inclusion radius | bounded cells | measure/tune later | offline | predictor calibration | global/MapFeatureVector/v1 continuous features (no map identity or bucket key) | tie to weapon/region scale if possible |
| retreat_ratio_base | SquadManager/CP | shared commit/retreat bar | bounded percent | Tier 4 | offline | engagement value preserved | personality→family | one engagement authority only |
| inmatch_gain_permille | InMatchAdaptBotModule | how strongly current match form moves bar | 0–30 suggested around current 15 | Tier 4 | offline | same-match subsequent engagements | global→personality | live state resets every match |
| inmatch_max_delta_pct | InMatchAdaptBotModule | hard adaptation bound | design safety constant; current 20 | authored / maybe very conservative tune | release design review | tail-risk analysis | global | prefer DO NOT LEARN in V1 |
| inmatch_interval_ticks | InMatchAdaptBotModule | adapt recompute cadence | current 250 | authored | n/a | latency/stability | global | DO NOT LEARN initially |
| combat_prior_min_correction | EngagementPriors | minimum fitted damage correction | current 500 | safety constant | design review | tail calibration | global | DO NOT LEARN automatically |
| combat_prior_max_correction | EngagementPriors | maximum fitted damage correction | current 2000 | safety constant | design review | tail calibration | global | DO NOT LEARN automatically |

## 8.64 E. Scale/utility/director
| Parameter / family | Owner | Context | Type / safe bounds | Learning route | Update | Reward/evidence | Pooling | Safety / dependency |
|---|---|---|---|---|---|---|---|---|
| scale_army_ratio | ScaleTargetsBotModule | army size response to seen enemy | bounded multiplier | Tier 4 group | offline | match/combat score | family→personality | difficulty line remains separate |
| scale_defence_ratio | ScaleTargetsBotModule | defence target response | bounded | Tier 4 | offline | base survival/resource efficiency | family→personality | avoid runaway turtling |
| scale_aircraft_ratio | ScaleTargetsBotModule | air target response | bounded | Tier 4 later | offline | air engagement/match | family | depends on air doctrine |
| scale_unscouted_margin | ScaleTargetsBotModule | uncertainty reserve for unseen enemy | nonnegative bounded | Tier 4 only after fog audits | offline | hidden-truth calibration + match | MapFeatureVector/v1 continuous features (no map identity or bucket key)/phase | truth only fits coefficient offline |
| scale_growth_rate | ScaleTargetsBotModule | long-game target growth | small positive | Tier 4 later | offline | long-match performance | family/personality | needs long-match sample |
| utility_turtle_rush_rest | BotUtilityAxes | resting macro identity | bounded bipolar axis | authored prior; family-level tune | offline | match/episode | personality/family | identity must dominate learning |
| utility_tech_expansion_rest | BotUtilityAxes | resting macro identity | bounded bipolar | authored prior + tune | offline | match/episode | personality/family | preserve identity |
| utility_steamroller_guerrilla_rest | BotUtilityAxes | resting macro identity | bounded bipolar | authored prior + tune | offline | match/episode | personality/family | preserve identity |
| utility_axis_response_weights | BotUtilityAxes | response to winning/losing/home pressure/etc. | small bounded vector | Tier 4 V2 | offline | episode score | global→family | do not tune with build knobs same SPSA group |
| utility_axis_decay | BotUtilityAxes | return toward rest | bounded time constant | Tier 4 V2 | offline | oscillation/stability + match | global | stability constraint |
| director_pressure_threshold | BotDirector | build-up→pressure | bounded around current design | Tier 4 | offline | pressure episode + match | global→personality | pacing only; no resource cheats |
| director_climax_threshold | BotDirector | pressure→climax | bounded | Tier 4 | offline | attack quality | global | must exceed pressure |
| director_relief_exit_threshold | BotDirector | relief→build-up | bounded | Tier 4 | offline | loss recovery | global | hysteresis ordering constraint |
| director_hysteresis | BotDirector | phase anti-chatter margin | bounded positive | Tier 4 | offline | phase-switch rate + score | global | penalize oscillation |
| director_pressure_force_scale | BotDirector | attack launch bar in pressure | bounded multiplier | Tier 4 | offline | attack episode | personality | no stats/income change |
| director_climax_force_scale | BotDirector | launch bar in climax | bounded multiplier | Tier 4 | offline | attack quality | personality | safety veto remains downstream |
| director_relief_force_scale | BotDirector | conservatism during relief | bounded multiplier | Tier 4 | offline | recovery | personality | safety veto remains |

## 8.84 F. Scouting/information
| Parameter / family | Owner | Context | Type / safe bounds | Learning route | Update | Reward/evidence | Pooling | Safety / dependency |
|---|---|---|---|---|---|---|---|---|
| scout_rebuild_cooldown_ticks | ScoutBotModule | replace lost scouts | bounded ticks | Tier 4 | offline | intel gained per scout cost | global→faction family | current replacement-rationing evidence |
| scout_count_target | ScoutBotModule / ScaleTargets | number/value of scouts | bounded integer/ratio | Tier 4 | offline | information gain minus losses | personality/family | use roles rather than actor IDs |
| enemy_spawn_bonus | ScoutBotModule | priority of probable spawn | bounded score weight | Tier 4 V2 | offline | time-to-first-contact / useful intel | MapFeatureVector/v1 continuous features (no map identity or bucket key) | public spawn data only |
| staleness_weight | Master target/scout | value of refreshing old intel | bounded weight | Tier 4 | offline | information gain / decision improvement | global→MapFeatureVector/v1 continuous features (no map identity or bucket key) | same staleness semantics across modules |
| interest_weight | Scout/Influence | prefer economically/militarily important zones | bounded | Tier 4 | offline | intel utility | global | avoid chasing hidden truth |
| route_risk_weight | RegionRouter/Scout | risk cost for scout routes | bounded | Tier 4 | offline | scout survival + info | platoon type/map | separate air/ground if needed |
| explore_confirm_mix | ScoutBotModule | new terrain vs re-check suspected enemy | simplex/0..1 | Tier 4 | offline | information value | personality/MapFeatureVector/v1 continuous features (no map identity or bucket key) | requires logged reason/outcome |
| memory_decay_ticks | BotFogMemory/Influence | how stale remembered mobile threat becomes | positive ticks | measured first | offline | seen→truth predictive calibration | MapFeatureVector/v1 continuous features (no map identity or bucket key)/unit role | do not erase known static buildings identically |
| influence_history_alpha | BotInfluenceLayers | EMA update rate | 0..1 | Tier 4 V2 | offline | future threat prediction | MapFeatureVector/v1 continuous features (no map identity or bucket key) | fit prediction, not win rate first |
| influence_decay_ticks | BotInfluenceLayers | blend current memory toward history | positive ticks | Tier 4 V2 | offline | threat prediction error | MapFeatureVector/v1 continuous features (no map identity or bucket key) | paired with alpha |
| influence_neighbor_spread | BotInfluenceLayers | threat reach across adjacent zones | bounded fraction | measure/tune V2 | offline | combat location prediction | MapFeatureVector/v1 continuous features (no map identity or bucket key) | avoid double count weapon range |

## 8.98 G. Production/counter mix
| Parameter / family | Owner | Context | Type / safe bounds | Learning route | Update | Reward/evidence | Pooling | Safety / dependency |
|---|---|---|---|---|---|---|---|---|
| role_mix[anti_inf,anti_vehicle,anti_air,siege,scout,...] | UnitBuilderBotModuleCA | army role composition target | simplex; K roles => K-1 DOF | Tier 4 | offline | match + role trade residual | personality→family→matchup | Dirichlet/log-ratio parameterization |
| role_floor_pct | UnitBuilderBotModuleCA | minimum representation of needed role | 0..small % | Tier 4 | offline | composition robustness | global→personality | avoid starving counters |
| counter_demand_gain[role] | BotCounterDemandController / production | response to enemy role mix | bounded multiplier | Tier 4 | offline | post-demand fight score | global→family | sustained/hysteretic condition retained |
| counter_demand_enter_threshold | counter demand | when demand condition turns on | bounded | Tier 4 V2 | offline | precision/recall of useful counter build | role family | hysteresis |
| counter_demand_exit_threshold | counter demand | when demand clears | bounded < enter | Tier 4 V2 | offline | same | role family | ordering constraint |
| production_prior_residual[role/family] | BotLearnedPriors | trade-derived unit preference | 0.5–2.0 | Tier 1 measured | offline | value trade | family→matchup | not per actor unless sparse residual with strong shrinkage |
| parallel_production_trigger | ParallelProductionBotModule | when extra factories pay | bounded | Tier 4 later | offline | queue idle / throughput / match | faction family | respect economy/power |
| army_first_bias | ArmyFirstBotModule | pause building to complete army timing | bounded/arm bit | bandit or tune later | offline | timing episode | personality | prefer plan arm if categorical |

## 8.109 H. Expansion/base
| Parameter / family | Owner | Context | Type / safe bounds | Learning route | Update | Reward/evidence | Pooling | Safety / dependency |
|---|---|---|---|---|---|---|---|---|
| exp_resource_weight | ExpansionPlannerBotModule | field economic value | positive normalized weight | Tier 4 specialist group | offline | ROI/match | MapFeatureVector/v1 continuous features (no map identity or bucket key)/family | normalize score terms |
| exp_threat_weight | ExpansionPlannerBotModule | safety penalty | positive | Tier 4 | offline | expansion survival/ROI | MapFeatureVector/v1 continuous features (no map identity or bucket key) | fog-honest threat |
| exp_distance_weight | ExpansionPlannerBotModule | travel/payback cost | positive | Tier 4 | offline | ROI/time-to-pay | MapFeatureVector/v1 continuous features (no map identity or bucket key) | path distance, not hidden shortcut |
| exp_ally_claim_weight | ExpansionPlanner/TC | avoid allied conflict | positive | Tier 4/team later | offline | team expansion efficiency | team size | deterministic claims remain |
| mcv_expansion_trigger | McvManager/ExpansionPlanner | when to send/found expansion | bounded resource/time ratio | Tier 4 | offline | expansion ROI + base loss | faction family | failure feedback required |
| defence_perimeter_share | DefenseCoveragePlanner | perimeter vs interior defence share | 0..1 | Tier 4 | offline | coverage/base survival | MapFeatureVector/v1 continuous features (no map identity or bucket key)/faction | current geometry stays authored |
| defence_specialty_mix | DefenseCoveragePlanner | AA/AG/specialty allocation | simplex | Tier 1 cause-of-loss + Tier 4 residual | offline | cause of loss | matchup | derive demand first |
| production_front_setback | BaseFrontBackPlanner | how far behind defence line producers sit | bounded cells | Tier 4 later | offline | producer survival + reinforcement time | MapFeatureVector/v1 continuous features (no map identity or bucket key) | geometry safety bounds |
| valuable_rear_bias | BaseFrontBackPlanner | how aggressively valuables go back | bounded weight | Tier 4 later | offline | valuable survival / path cost | global | class assignment itself authored |
| radar_coverage_value_weight | BaseFrontBackPlanner | value of new approach coverage | bounded | Tier 4 later | offline | intel/coverage | MapFeatureVector/v1 continuous features (no map identity or bucket key) | provider count/power constraints authored |

## 8.122 I. Formation/micro
| Parameter / family | Owner | Context | Type / safe bounds | Learning route | Update | Reward/evidence | Pooling | Safety / dependency |
|---|---|---|---|---|---|---|---|---|
| formation_rim_radius | ArmyStaging/AssaultFormation | formation/staging geometry | bounded cells | specialist CMA-ES/SPSA | offline skirmish then full game | engagement score | platoon type | terrain legality hard constraint |
| formation_arc_width_gain | AssaultFormationBotModule | concave width vs army size | bounded | specialist | offline | time-to-fire / losses | platoon type | preserve range matching |
| formation_rank_spacing | AssaultFormationBotModule | front/back rank spacing | bounded cells | specialist | offline | engagement score | platoon type | pathability |
| formation_arrival_stagger | AssaultFormationBotModule | simultaneous range arrival | bounded ticks/distance | specialist | offline | first-volley concentration | platoon type | no per-tick steering |
| focus_fire_stickiness | micro/squad | stay on selected target | 0..1 | Tier 4 specialist | offline | overkill vs kill speed | platoon/weapon family | target legality |
| focus_fire_threat_weight | micro/squad | target's damage threat | bounded | Tier 4 | offline | engagement score | platoon | combine with killability |
| focus_fire_killability_weight | micro/squad | DPS-vs-armour / remaining HP | bounded | Tier 4 | offline | engagement score | platoon | predictor-derived |
| kite_range_margin | micro/squad | minimum range advantage to kite | bounded cells/fraction | Tier 4 specialist | offline | survival/trade | weapon/platoon | speed feasibility veto |
| kite_step_fraction | micro/squad | retreat step distance | bounded | Tier 4 | offline | uptime/survival | platoon | pathable cells only |
| pullback_health_frac | micro/repair | damaged unit withdrawal threshold | 0..1 | Tier 4 | offline | value preserved | unit role/platoon | repair availability context |
| support_wait_gain | formation | wait for artillery/support | bounded | Tier 4 | offline | engagement score | platoon | timeout authored |
| splash_separation_weight | formation | spread under splash threat | bounded | Tier 4 | offline | AoE losses | enemy delivery family | fog-honest seen weapon threat |
| friendly_fire_weight | formation | avoid own AoE | bounded | Tier 4 | offline | own FF losses | weapon family | hard safety floor possible |
| choke_avoid_weight | formation/zone map | avoid bad chokepoint congestion | bounded | Tier 4 | offline | engagement/path delay | MapFeatureVector/v1 continuous features (no map identity or bucket key) | don't override mission destination |

## 8.139 J. Missions/operations
| Parameter / family | Owner | Context | Type / safe bounds | Learning route | Update | Reward/evidence | Pooling | Safety / dependency |
|---|---|---|---|---|---|---|---|---|
| mission_bid_value_weight | mission bidding future | value of objective | bounded | Tier 4 V2 | offline | mission success + match | mission type | needs mission cards/outcome sink |
| mission_bid_distance_weight | mission bidding | travel cost | bounded | Tier 4 | offline | mission efficiency | mission type/map | path distance |
| mission_bid_risk_weight | mission bidding | threat cost | bounded | Tier 4 | offline | survival/success | mission type | fog-honest influence |
| raid_value_threshold | Master/Squad | when raid is worthwhile | bounded value/utility | Tier 4 | offline | raid mission score | personality/family | mission outcome attribution |
| defend_request_threshold | Master/TC | when to allocate defence mission | bounded threat ratio | Tier 4 | offline | base saved/opportunity cost | personality/team | avoid duplicate owner |
| secure_threshold | future Secure missions | when territory objective earns squad | bounded utility | Tier 4 V2 | offline | map control/match | MapFeatureVector/v1 continuous features (no map identity or bucket key) | after mission bidding exists |
| staging_reserve_share | ArmyStagingBotModule | home/internal-threat reserve | 0..1 bounded | Tier 4 | offline | base defence vs attack opportunity | personality | scale targets interaction |
| staging_rim_inset | ArmyStagingBotModule | distance inside defence rim | small bounded cells | Tier 4 later | offline | response time / artillery exposure | MapFeatureVector/v1 continuous features (no map identity or bucket key) | geometry constraint |

## 8.150 K. Air/support
| Parameter / family | Owner | Context | Type / safe bounds | Learning route | Update | Reward/evidence | Pooling | Safety / dependency |
|---|---|---|---|---|---|---|---|---|
| air_engage_ratio | air doctrine | air commit threshold | bounded ratio | Tier 4 | offline | air trade score | air role/family | reuse CP semantics |
| air_aa_priority_weight | air doctrine | prefer killing AA | bounded | Tier 4 | offline | sortie survival/objective | matchup | seen AA only |
| air_ground_safe_threshold | air doctrine | when ground targets are safe enough | bounded | Tier 4 | offline | sortie score | air role | influence threat-air |
| bomber_strike_min_value | air doctrine | minimum target value for bomber package | bounded cost | Tier 4 later | offline | strike ROI | faction family | ammo/reload context |
| support_attractiveness[family] | SupportPowerBotASModule | minimum utility for nuke/strike/disable/heal/etc. | bounded normalized utility | Tier 4 | offline | objective/value swing | power family | not one free scalar per power initially |
| support_climax_hold_bias | SupportPowerBotAS + Director | hold strategic power for synchronized climax | bounded | Tier 4 | offline | climax outcome | power family/personality | expiry/overhold penalty |
| support_consideration_residual[family,feature] | Support power scorer | calibrate authored utility terms | sparse bounded residual | Tier 1 measured | offline | post-use swing | power family | minimum evidence |

## 8.160 L. Team coordination
| Parameter / family | Owner | Context | Type / safe bounds | Learning route | Update | Reward/evidence | Pooling | Safety / dependency |
|---|---|---|---|---|---|---|---|---|
| team_sync_window_ticks | Team blackboard/commander | attack arrival synchronization | bounded ticks | Tier 4 later | offline 2v2 | team fight score | team size | human ally not coerced |
| team_sync_force_scale | TC/Director | how much ally timing lowers/raises launch bar | bounded multiplier | Tier 4 | offline | team engagement | team size/personality | combat veto downstream |
| team_defend_answer_threshold | TC | when ally request is worth answering | bounded utility | Tier 4 | offline | ally saved/opportunity cost | team size | distance/CP feasibility |
| team_target_focus_weight | TC/Master | shared-target convergence | bounded | Tier 4 | offline | team objective | team size | avoid overconcentration |
| team_personality_coverage_weight | TC/PlanBandit | prefer complementary allied doctrines | bounded | bandit/tune later | match-start | team result | team comp | public/allied data only |
| team_expansion_claim_penalty | TC/Expansion | avoid same field | large authored penalty or bounded weight | authored/tune | offline | resource conflict | team size | claim correctness more important than fine tuning |

## 8.169 M. Opponent model
| Parameter / family | Owner | Context | Type / safe bounds | Learning route | Update | Reward/evidence | Pooling | Safety / dependency |
|---|---|---|---|---|---|---|---|---|
| plan_prior[class] | Opponent model feature | rush/turtle/air/out-earn/etc. prior | probability simplex | empirical Bayes | offline + within-match Bayesian update | classification calibration | enemy faction→family | no human identity |
| plan_feature_likelihood[class,feature] | Opponent model | P(observation\|plan) | probabilities | MLE/Bayesian | offline | held-out likelihood | enemy faction→family | fogged features only |
| plan_transition[class_i,class_j] | optional HMM | strategy transition probabilities | row-simplex | MLE later | offline | sequence likelihood | enemy faction→global | feature only, not direct policy |
| belief_decay/change_rate | Opponent model | adapt to plan transition/nonstationarity | bounded | fit | offline | held-out prediction | global→faction | avoid overreacting one sighting |
| counter_plan_arm | PlanBandit | opening/plan conditioned on plan belief | categorical | contextual bandit V2 | match/episode | reward with propensities | faction/matchup | safety floor |

## 8.177 N. Learning-system hyperparameters
| Parameter / family | Owner | Context | Type / safe bounds | Learning route | Update | Reward/evidence | Pooling | Safety / dependency |
|---|---|---|---|---|---|---|---|---|
| tier1_shrink_value | fit_engagement_priors.py | pseudo-evidence strength | positive; current 5000 | hyperparameter validation | offline | held-out prediction | global | do not optimize on same training rows |
| tier1_min_pair_samples | fit_engagement_priors.py | specific matchup evidence gate | integer; current 8 | hyperparameter | offline | held-out error | global | conservative |
| bandit_prior_count | PlanBanditBotModule | parent pseudo-observation cap | integer; current 8 | hyperparameter | rarely tune | offline simulation/league | global | stability over responsiveness |
| bandit_lcb_z | PlanBanditBotModule | safety confidence level | positive; current 1.64 | safety hyperparameter | design/statistical review | tail loss | global | do not chase mean win rate |
| bandit_min_evidence | PlanBanditBotModule | evidence before safety exclusion | integer; current 4 | hyperparameter | review | false exclusion rate | global | small samples pool upward |
| bandit_min_safety_lcb | PlanBanditBotModule | minimum safe arm quality | current -250 milli | safety hyperparameter | league validation | catastrophic-loss rate | global/personality maybe | baseline-relative alternative worth evaluating |
| bandit_decay | tune_plan_bandits.py | nonstationarity/recency | 0..1 | offline meta-tune | per release | future predictive accuracy | global | balance change explicit reset stronger |
| spsa_a | tune_build_order.py | gain numerator | current 0.10 | calibrated hyperparameter | offline synthetic + empirical | convergence/validation | per semantic group maybe | freeze during one run |
| spsa_c | tune_build_order.py | perturbation scale | current 0.08 | calibrated | offline | signal/noise | per group | large enough to exceed match noise |
| spsa_A | tune_build_order.py | early stability constant | current 10 | calibrated | offline | convergence | per group | freeze per run |
| spsa_alpha | tune_build_order.py | gain decay exponent | current 0.602 | authored literature constant | offline | n/a | global | do not tune casually |
| spsa_gamma | tune_build_order.py | perturbation decay | current 0.101 | authored literature constant | offline | n/a | global | do not tune casually |
| spsa_weak_step_scale | tune_build_order.py | step when \|z\| below gate | current 0.25 | proposal: validate | offline | drift vs learning speed | per group | final untouched validation mandatory |
| spsa_min_matches | tune_build_order.py | per-arm evidence floor | current 20 | statistical design | experiment | SE/effect size | global | not an acceptance sample size |
| learn_multiplier_min/max | tune_build_order.py | offline learned overlay bounds | current 0.8–1.25 | safety constants | design review | tail behavior | global/group | do not widen without evidence |
| balance_change_discount | all learners | confidence retained after semantic change | 0..1 | governance hyperparameter | on version change | post-change predictive value | affected scope only | prefer dependency-aware invalidation |
| exploration_propensity | future contextual bandit dev build | safe non-default action probability | small, e.g. 1–5% | experiment design | dev only | coverage vs cost | action/context | veto-approved actions only |
| ope_clip_weight | offline evaluation tool | importance-weight clipping | positive | validation hyperparameter | offline | bias/variance | global | report sensitivity |
| regularization_lambda | regression/future net | shrinkage/weight decay | positive | cross-validated | offline | holdout score | model family | never chosen on final test |

## 8.199 O. Design constants / never self-optimize
| Parameter / family | Owner | Context | Type / safe bounds | Learning route | Update | Reward/evidence | Pooling | Safety / dependency |
|---|---|---|---|---|---|---|---|---|
| unit_damage_hp_armour_cost | game balance | actual unit balance | n/a | DO NOT LEARN | balance pipeline only | game design | n/a | bot cannot rewrite game balance |
| fog_visibility_or_reveals_map | engine/game rules | information access | n/a | DO NOT LEARN | n/a | fairness | n/a | no learned vision cheat |
| cash/resource_cheat | difficulty/insurance | free resources | n/a | DO NOT LEARN as policy | n/a | fairness | n/a | explicit difficulty design only |
| human_reaction_delay_line | difficulty identity | difficulty contract | n/a | authored difficulty | n/a | difficulty feel | n/a | learning applies underneath/on policy, not erasing tiers |
| raw_actor_units_to_build_rows | UnitBuilder YAML | thousands of correlated IDs | n/a | DO NOT independently optimize | n/a | derive roles/residuals | role/family | sample complexity explosion |
| map_geometry_constants_that_define_legality | placement/topology | passability/geometry | n/a | DO NOT LEARN | n/a | correctness | n/a | learn scoring weights, not legality |
| mission_ownership_and_lease_rules | ownership framework | one owner per actor/decision | n/a | DO NOT LEARN | n/a | correctness | n/a | contract |
| sync_boundary | OpenRA architecture | orders and deterministic simulation | n/a | DO NOT LEARN | n/a | correctness | n/a | architectural law |

---

## Appendix C — Algorithms and update rules

## 11.1 Tier-1 empirical-Bayes residual

For an observed/expected quantity in a cell:

```text
g = pooled_global_obs / pooled_global_exp

residual =
    (obs + K * g) /
    (g * (exp + K))
```

then clamp to the approved correction range.

Interpretation:

- no evidence ⇒ approximately 1.0;
- strong evidence ⇒ local observed/expected ratio;
- sparse cells inherit the global prior.

Prefer a hierarchical extension in log space if later scopes become faction-specific.

## 11.2 Continuous-reward Thompson sampling

For an arm with sufficient statistics `(n, mean, m2)`:

```text
s² = m2 / (n - 1)
SE = sqrt(s² / n)

sample ~ StudentT(df=n-1, loc=mean, scale=SE)
```

For large `n`, normal approximation is adequate.

Parent scopes contribute capped pseudo-observations, as current PlanBandit already does.

Use **Beta-Bernoulli only for genuinely binary rewards**. Do not convert a rich engagement score into a fake win/loss purely to use a Beta posterior.

## 11.3 Conservative safety floor

For each arm:

```text
LCB = posterior_mean - z_safe * posterior_SE
```

If:

```text
evidence >= min_evidence
AND LCB < minimum_safe_score
```

the arm cannot win the draw.

A stronger future variant is baseline-relative:

```text
LCB(candidate - baseline) >= -allowed_regret
```

This aligns with conservative-bandit / safe-policy-improvement literature.

## 11.4 SPSA

Current Cameo form:

```text
Delta_i ∈ {-1,+1}

c_k = c / (k+1)^0.101
a_k = a / (k+1+A)^0.602

theta_plus  = theta + c_k Delta
theta_minus = theta - c_k Delta

g_i = (J_plus - J_minus) / (2 * effective_delta_i)

theta_next = clamp(theta + step_scale * a_k * g)
```

Use log space for multiplicative knobs.

### Recommended experiment discipline

- same map;
- same factions;
- same spawn pairing;
- same seed family;
- same opponent;
- plus and minus differ only by tested vector;
- minimum current floor = 20 scored matches per arm;
- final validation uses **fresh** seeds/maps and does not reuse adaptive-training games.

The current “weak evidence still moves 0.25×” rule is acceptable as an optimizer heuristic only if the final untouched A/B/league gate is strict. It is **not** itself proof that the new vector is better.

## 11.5 Safe bounded in-match control (not online training)

Current pattern:

```text
delta =
 clamp(
   -running_engagement_score * gain,
   -max_delta,
   +max_delta
 )
```

Recommended rules:

- only `seen` inputs;
- bounded;
- no persistent write;
- reset every match;
- one authority consumes the delta;
- parameter gain is learned offline;
- hard max delta remains authored in V1;
- log every update and the subsequent engagements for causal analysis.

## 11.6 Contextual bandit — future extension

If plan selection starts depending on a feature vector `x`, use a contextual model such as linear Thompson/LinUCB **only after** logging propensities.

For linear reward:

```text
E[r | x,a] = xᵀ beta_a
```

Use hierarchical regularization so sparse factions shrink to family/global parameters.

This can replace a combinatorial explosion of:

```text
matchup × map × phase × plan-class × arm
```

with a smaller feature-conditioned model.

## 11.7 Bayesian optimization

Use when:

- ≤ ~10–15 meaningful continuous dimensions;
- match evaluations are especially expensive;
- SPSA gradient estimates remain unstable;
- objective is smooth enough for a surrogate.

Do not make GP-BO the default for all bot parameters.

## 11.8 CMA-ES

Use primarily for a tightly coupled micro/formation vector tested in short repeatable combat scenarios.

Never accept a skirmish-optimized CMA-ES vector without full-game validation.

---

## Appendix D — Reward and credit assignment

Pure win rate is too sparse. Pure local efficiency is gameable.

Use a hierarchy.

## 12.1 Match reward

Recommended release-level objective:

```text
R_match =
    W_win * outcome
  + W_margin * normalized_value_margin
  + W_speed * bounded_speed_term
  + W_objective * objective_control
```

No raw harvesting bonus.

## 12.2 Engagement reward

Current EL decomposition is a strong basis:

```text
R_engagement =
    trade_quality
  + performance_vs_prediction
  + objective_effect
```

Normalize by value at risk where possible.

## 12.3 Episode rewards

Use local horizons for decisions whose effect ends early:

- opening: economy/army/objective state at opening end + small match-result regularizer;
- expansion: payback/survival before next expansion;
- support power: target effect + objective swing in a bounded window;
- raid: value damage and disruption minus committed losses;
- defend request: prevented loss minus opportunity cost;
- scouting: information gain and later decision utility minus scout cost.

## 12.4 Reward-hacking protections

Never reward:

- raw resource collection alone;
- raw kills alone;
- merely surviving;
- number of actions;
- building one favored role irrespective of enemy;
- scouting hidden truth;
- repeatedly triggering a telemetry event.

Use zero-sum or opportunity-cost-adjusted terms whenever practical.

---

## Appendix E — Logging and schema details

# Implementation detail: context features, manifests, and experiment pairing

## I1. `MapFeatureVector/v1`

This is calculated from public geometry/rules and the bot's legal own start. It is not keyed by map identity.

Candidate feature families, subject to the registry and existing-provider reuse:

- own-start to public/legally-known strategic-distance statistics;
- reachable-land and water fractions;
- region and chokepoint counts/density;
- corridor-width quantiles;
- resource-field counts and distance/payback quantiles;
- expansion travel-cost statistics;
- base openness/perimeter measures;
- height/ramp statistics.

Persist/runtime values are integer/fixed-point and have missing bits where needed. A consumer fits a smooth,
bounded function of the vector; it never stores “this named map did X”.

## I2. `OpponentFeatureVector`

Recomputed during the match from visibility, remembered contacts and visible effects only. Candidate fields:

- seen army value;
- seen defence value and defence share;
- seen air/artillery/stealth role shares;
- observed expansion/economy proxy;
- observed tech-stage proxy;
- pressure/attack timing;
- confidence/staleness per field;
- transient strategic-belief probabilities represented in fixed point if approved.

It is disposed at match end. There is no player/account/history key.

## I3. Paired experiments without persistent map identity

A paired A/B runner may know which scenario it launches, but learning records should not retain a reusable
map name/hash. Use a **batch-local opaque pair/cell token** that means only “these two runs were paired”.
The token must not be stable across batches and must not be resolvable by the runtime learner into a map identity.

This preserves common-random-number/paired-statistics benefits without teaching the bot a named-map table.

## I4. Manifest

Every emitted learned artifact should carry the architecture synthesis's manifest fields:

```yaml
LearningManifest:
  RulesHash: ...
  AiSchemaVersion: ...
  FeatureSchemaVersion: ...
  RoleSchemaHash: ...
  RewardSchemaVersion: ...
  SourceMatches: ...
  SourceObservations: ...
  Validation: ...
  ParentArtifact: ...
```

Where an exact rules hash cannot be reproduced safely in the runtime environment, the artifact must fail closed
according to its registry fallback instead of silently applying stale values.

The existing engagement log is strong. The next critical addition is **decision attribution suitable for causal/off-policy analysis**.

Every learnable discrete decision should log:

```json
{
  "policy_id": "plan_bandit/2",
  "policy_git_sha": "...",
  "parameter_file_hash": "...",
  "switch_set": "...",
  "decision_id": "...",
  "tick": 12345,
  "context_version": 3,
  "eligible_actions": ["a","b","c"],
  "chosen_action": "b",
  "propensity": 0.173,
  "features_seen": { "...": "..." },
  "reason": "...",
  "outcome_record_id": "..."
}
```

For continuous controllers also log:

- base parameter;
- learned multiplier;
- personality/family/faction scope chosen;
- live bounded delta;
- final effective value;
- provider source.

## 13.1 Visibility provenance

Every enemy-derived feature should be classifiable as:

- visible now;
- remembered;
- public lobby/map information;
- inferred from fog-honest history;
- offline truth.

`offline truth` must never appear in the live feature vector.

## 13.2 Fingerprints

Every training batch should include:

- mod commit;
- engine commit;
- feature-schema hash;
- resolved AI hash;
- learned-file hashes;
- balance ledger hash;
- schema versions;
- switch set;
- seed;
- faction pair;
- spawn.

Abort or split a training batch if these change.

---

## Appendix F — Statistical validation protocol

## 14.1 Training and final validation are different datasets

Do not tune and declare victory on the same matches.

Recommended split:

- **training/adaptive:** used by fitter/SPSA/bandit;
- **validation:** used during model/group selection;
- **final holdout:** untouched until candidate freeze.

Split by seed and preferably map/faction cells, not random log lines from the same match.

## 14.2 Paired design

For plus/minus or candidate/control:

- mirror factions first;
- swap spawn;
- use paired seeds;
- pair map/opponent cells;
- analyze paired score differences.

This can reduce variance substantially relative to unrelated matches.

## 14.3 Confidence intervals

Report at minimum:

- win/loss and Wilson or beta-binomial interval;
- paired continuous-score mean difference with bootstrap/normal interval;
- engagement score distribution;
- catastrophic-loss/suicide rate;
- per-map/per-faction breakdown.

Do not hide a subgroup collapse behind pooled mean.

## 14.4 Sample sizes

There is no single universal “8 games is enough” number.

Recommended operational levels:

- smoke/inertness: small batches;
- SPSA gradient: current minimum 20 scored matches per arm;
- ordinary behavior A/B: 24–48 per arm depending variance;
- high-impact release decision: 48+ per arm and league coverage;
- small expected win-rate deltas: power analysis may require hundreds or thousands.

Treat 16-match historical gates as engineering screens, not precise balance estimates.

## 14.5 Multiple comparisons

Current Holm handling is appropriate for families of simultaneous hypotheses.

If experiments are repeatedly peeked/sequentially stopped, use:

- predeclared stopping rules;
- alpha spending/sequential Holm, or
- confidence sequences.

Do not run until a lucky p-value appears.

## 14.6 League gate

Final learned candidate should face:

- current control;
- past accepted versions;
- rush exploiter;
- turtle exploiter;
- air-focused exploiter;
- artillery/siege exploiter;
- harass/guerrilla exploiter;
- varied maps;
- representative faction families.

The score should include worst-cell/regression constraints, not only global win rate.

---

## Appendix G — Determinism, sync, fairness and release governance

## 15.1 Synced simulation

Must remain deterministic. Learned bot logic affects it only through legal orders/conditions using the existing OpenRA bot architecture.

## 15.2 Host-local bot reasoning

May use unsynced local state according to the current architecture, but:

- no hidden enemy information;
- no live external network;
- no silent rules mutation;
- no per-machine persistent release learning.

## 15.3 Learned files

Release files are:

- committed;
- versioned;
- reviewed;
- reproducible;
- read at/before match setup as designed;
- immutable during the match.

## 15.4 Match-local adaptation

Allowed only if:

- it uses fog-honest current-match evidence;
- it is bounded;
- it resets;
- its law is versioned;
- its gains are trained offline;
- save/load behavior is tested if relevant.

## 15.5 Opponent privacy/fairness

Keep the current faction-level opponent-model ruling.

Do not store or tune against identity of individual human players.

---

## Appendix H — Failure modes and mitigations

| Failure | Why it happens | Mitigation |
|---|---|---|
| overfitting one faction/map | too many independent scopes | hierarchical shrinkage + holdouts |
| SPSA drift | noisy plus/minus contrast | pairing, small coherent groups, bounds, final holdout |
| bandit survivorship bias | veto changes which engagements exist | record armed set; stratify fitter |
| selection bias | only successful actions yield records | explicit denied/failure mission cards |
| reward hacking | local metric divorced from winning | multi-level reward + league gate |
| hidden-info leakage | offline truth reused live | schema provenance + fog audit |
| stale learned priors after rebalance | underlying stats changed | per-cell fingerprint/PriorPct + scope discount |
| catastrophic forgetting | aggressive global decay | dependency-aware discount, keep parent priors |
| faction homogenization | optimizer moves all toward same optimum | faction doctrine as prior + bounded residuals |
| strategy oscillation | live utility/adaptation too reactive | hysteresis, decay, hard bounds |
| double decision authority | two modules both “learn” same action | one owner per decision; providers advise |
| correlated train/test games | same seeds/logs reused | separate final holdout |
| p-hacking | repeated peeking | sequential correction/predeclared stops |
| map overfit | one tournament map dominates | archetype/map holdouts and league |
| team credit failure | win/loss assigns same reward to all | team objective + marginal contribution proxies |
| sample fragmentation | matchup×map×phase×arm explosion | feature/context model + hierarchical pooling |
| balance vs AI confusion | tuner moves unit stats | strict separation: policy files vs balance pipeline |

---

## Appendix I — Bibliography and evidence strength

## Strong / primary academic evidence

1. J. C. Spall, **“Multivariate Stochastic Approximation Using a Simultaneous Perturbation Gradient Approximation,”** IEEE Transactions on Automatic Control 37(3), 1992.  
   Relevance: SPSA foundation; two objective evaluations independent of parameter dimension.

2. J. C. Spall, **“Implementation of the Simultaneous Perturbation Algorithm for Stochastic Optimization,”** IEEE Transactions on Aerospace and Electronic Systems 34(3), 1998.  
   Relevance: practical gain-sequence guidance.

3. M. Stanescu, N. Barriga, M. Buro, **“Using Lanchester Attrition Laws for Combat Prediction in StarCraft,”** AIIDE 2015.  
   https://ojs.aaai.org/index.php/AIIDE/article/view/12780  
   Relevance: learned combat-strength parameters over analytical RTS combat models.

4. S. Ontañón, **“The Combinatorial Multi-Armed Bandit Problem and Its Application to Real-Time Strategy Games,”** AIIDE 2013.  
   https://ojs.aaai.org/index.php/AIIDE/article/view/12681  
   Relevance: bandit-style allocation/search in RTS action spaces.

5. A. Uriarte, S. Ontañón, **“Combat Models for RTS Games,”** 2016.  
   https://arxiv.org/abs/1605.05305  
   Relevance: fast learned/analytical combat outcome models.

6. O. Vinyals et al., **“Grandmaster Level in StarCraft II Using Multi-Agent Reinforcement Learning,”** Nature 2019.  
   https://www.nature.com/articles/s41586-019-1724-z  
   Relevance: league/self-play and exploiters; not a recommendation to copy AlphaStar's policy architecture.

7. O. Vinyals et al., **“StarCraft II: A New Challenge for Reinforcement Learning,”** 2017.  
   https://arxiv.org/abs/1708.04782  
   Relevance: RTS partial observability/action complexity.

8. A. Synnaeve et al., **StarCraft II state estimation / defogging**, NeurIPS 2018.  
   Relevance: partial-observation state estimation can improve downstream policy decisions.

9. J. Dereszynski et al., **“Learning Probabilistic Behavior Models in Real-Time Strategy Games,”** AIIDE 2011.  
   Relevance: latent strategic-state modeling from gameplay traces.

10. Y. Li et al., **“A Contextual-Bandit Approach to Personalized News Article Recommendation,”** WWW 2010.  
    Relevance: LinUCB/contextual-bandit methodology; analogy for context-conditioned arm choice.

11. A. Garivier, E. Moulines, **non-stationary bandit work**.  
    Relevance: discount/window approaches when reward distributions move.

12. Y. Wang, A. Agarwal, M. Dudík, **“Optimal and Adaptive Off-policy Evaluation in Contextual Bandits,”** ICML 2017.  
    Relevance: importance/doubly-robust off-policy evaluation.

13. M. Dudík, J. Langford, L. Li, **“Doubly Robust Policy Evaluation and Learning,”** ICML 2011.  
    Relevance: logged-policy evaluation; motivation for propensity logging.

14. Y. Wu et al., **“Conservative Bandits,”** ICML 2016.  
    https://proceedings.mlr.press/v48/wu16.html  
    Relevance: exploration subject to baseline-performance constraints.

15. R. Laroche et al., **“Safe Policy Improvement with Baseline Bootstrapping,”** ICML 2019.  
    Relevance: baseline fallback under uncertainty.

16. J. Snoek, H. Larochelle, R. P. Adams, **“Practical Bayesian Optimization of Machine Learning Algorithms,”** NeurIPS 2012.  
    Relevance: expensive black-box hyperparameter search.

17. F. Hutter, H. Hoos, K. Leyton-Brown, **SMAC / model-based algorithm configuration**.  
    Relevance: mixed/conditional black-box configuration.

18. N. Hansen, **CMA-ES tutorial/reference material**.  
    Relevance: coupled continuous black-box optimization.

## Strong current Cameo sources

- `CLAUDE.md`
- `docs/README.md`
- `docs/TASK_INDEX.md`
- `docs/DESIGN.md` §19.2 and §19.13
- `docs/HANDOFF.md`
- `docs/design/AI_ARCHITECTURE.md`
- `docs/design/AI_MASTER_PLAN.md`
- `docs/design/AI_DEEP_RESEARCH.md`
- `docs/design/AI_LEARNING_RESEARCH_2026-10-03.md`
- `docs/design/TIER4_SPSA_SPEC.md`
- current C# bot modules under `OpenRA.Mods.Cameo/Traits/BotModules/`
- `tools/ai/`
- `mods/cameo/ai/`
- `mods/cameo/ai/learned/`

## Useful community/engineering evidence

- Steamhammer/McRave opening/opponent-model descriptions;
- BWEM terrain analysis;
- M28AI development notes;
- Stockfish/Fishtest SPSA engineering practice;
- OpenRA and related open-source bot implementations.

These are useful engineering analogies, but they should not override primary literature or current Cameo binding design.

## Low-confidence material deliberately not used as a core design basis

The parallel report contained several claims that were either difficult to trace to a strong primary source or unnecessary for the design:

- generic “Company of Heroes logistic-regression AI mods” claims;
- generic “Age of Empires villager-allocation parametric research” without a concrete primary citation;
- GPU/iGPU-offload recommendations for the current small-model problem;
- broad NEAT/meta-learning proposals as if they were immediate Cameo requirements;
- exact claims that a clean retrain beats continual training by a universal percentage.

They may remain research leads, but they do not belong in the critical-path implementation plan without stronger evidence.

---
