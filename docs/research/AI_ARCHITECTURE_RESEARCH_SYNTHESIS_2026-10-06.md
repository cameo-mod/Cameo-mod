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
