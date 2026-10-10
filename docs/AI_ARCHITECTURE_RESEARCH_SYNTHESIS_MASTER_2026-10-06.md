# Cameo AI architecture and research synthesis: master document

_First written 2026-10-06 (external reports merged with the project's AI research). **Revised 2026-10-09** by Claude
(Opus) against master `5c8cfe04`: every status claim was re-checked against the tree, contradictions with binding
rulings were fixed, the runtime-architecture review was folded in (Part III), and a phased improvement plan was added
(Part IV). The file name keeps its first date so that existing links still work._

**How to read it.** Part I is the decision spine: what the bot is and the rules it keeps. Part II is the verified state
on 2026-10-09. Part III lists the findings, with evidence. Part IV is the improvement plan, covering architecture,
performance, smartness and behaviour. Part V is the statistical annex, which is research detail subordinate to Parts
I and IV. Read sections, not the whole file (CLAUDE.md).

**Precedence.** Binding rulings win over this document, in this order: `docs/DESIGN.md` §19 (AI rulings) → maintainer
rulings recorded in `CLAUDE.md` / `docs/WORKFLOW.md` → `docs/design/AI_ARCHITECTURE.md` (contracts and specs) →
this document → research files. The work queue with owners and estimates stays in
[`design/AI_MASTER_PLAN.md`](design/AI_MASTER_PLAN.md) §3; Part IV feeds it and does not replace it. Where this document
and the code disagree, the code wins. Fix the document when that happens (CLAUDE.md, "Don't trust, verify").

---

# Part I — Decision spine

## 1. Executive summary

1. **The architecture is right; the runtime under it is under-specified.** Cameo's bot is a layered set of modules
   with one owner per decision. Advisor modules publish bounded inputs and existing owners issue the orders, and one
   order gate (`ModularBot.QueueOrder`) sees every order. That is the correct shape for a large modular RTS AI, and
   it stays. What is missing sits underneath it: modules share one random stream per player; the sense→decide→act
   layering is declared and audited (AR-10, AI_ARCHITECTURE §10.5c) but the runtime still ticks in merged-yaml order;
   there is no central scheduler; and the strategist hub is coupled through its concrete type (Part III, F1–F5).
2. **The learning plane is real but young.** The learnability registry exists (`tools/ai/learnables.yaml`, 40 rows,
   validator `tools/tests/test_learnables_registry.py`). Only 3 rows have a runtime consumer, and two of those break the
   contract this document sets: one runs without a switch, and another declares an artifact that is not in the tree
   (F9, F10).
3. **Most of the 2026-10-02 review has been fixed.** Seven of its nine code and lab findings are verified fixed in the
   tree (§6). The TC-3 consistency model and one-responder rescue capacity are still unverified.
4. **The 2026-10-08 playtest (Part IIIb).** The playtest build armed all 59 switch groups at once (never A/B'd) and
   switched off the engineers' binding omniscience. It carries a game crash (M10), an MCV ownership conflict (M12) and a
   learned-file bug (M9) that are still open; two base-building bugs were fixed today. The first measured 1v1 was
   **not** passive. In teams both configurations failed in different ways: the armed set starves its economy
   (M6, M12) and drains armies into escorts (M13); the pre-arm default turns whole teams into turtles (M14, a binding
   ruling left behind a switch). Fix the defects before choosing a switch set.
5. **The plan (Part IV), in one line:** make the lab trustworthy first (per-module random streams, a standing
   order-stream parity gate, regenerated evidence, a performance baseline), then give the runtime a skeleton (a
   switchable layer scheduler, interface-only seams, a crossed-order ratchet), then spend the gained headroom on
   smartness (the canonical learning order D1–D7) and behaviour (UT, the blended squad manager, over the zone graph
   and influence maps). Every step goes through the increment A/B.

## 2. Decision

Cameo builds a **modular, fog-honest, offline-fitted learning plane around the existing bot owners, on top of a
deterministic module runtime whose declared layers (AR-10) become its tick order through switchable increments.** It
does not build a monolithic self-learning bot, train a whole-game policy, persist a profile of a human player, store
map identities, or let a shipped client change a model during a match.

```text
OBSERVE      BotSituation (MasterAi snapshot) · fog memory · TacticalMap (zones) · threat · radar contacts
                 │   public map rules + legal own start ──► MapFeatureVector/v1 (planned)
                 │   visible enemies + decaying memory  ──► OpponentFeatureVector (planned)
                 ▼
SYNTHESIZE   MasterAi: target, urgency, utility axes, Director, team broadcast, coalition fold
                 ▼
PLAN/ADVISE  ExpansionPlanner · DefenseCoverage · Siege · Formation · ScaleTargets · BuildOrderKnobs ·
             Spacing · StealthDoctrine · ProductionWidth · CombatVeto        ◄── reviewed, versioned,
                 ▼                                                              integer learned artifacts
ARBITRATE    BaseBuilder · UnitBuilder · SquadManager · lease registry · action budget
                 ▼
EXECUTE      squad states · engineer · repair · harvest · deploy · transport …
                 ▼
             IBot.QueueOrder ──► order gate (lease verdict at issue time) ──► world.IssueOrder
                 ▼
VERIFY       engagement log · mission cards · placement/expansion logs · replay health ──► offline fitters
             ──► validation ──► review ──► committed artifact (mods/cameo/ai/learned/)
```

Learned providers publish bounded inputs; they never create a competing execution path. The layer names above are the
ones used in the 2026-10-02 review §1. They are declared per module and audited (AR-10: `ai_arch_audit.py` R8,
`LAYER_OF`), but the runtime still ticks in merged-yaml order, where ContentPack includes put the act modules first.
The diagram's stages map onto the audit's layers as OBSERVE ≈ PERCEPTION, SYNTHESIZE ≈ SITUATION, PLAN/ADVISE ≈
STRATEGY, ARBITRATE/EXECUTE ≈ PRODUCTION + EXECUTION, VERIFY ≈ TELEMETRY (SUPPORT is infrastructure). Part IV, phase
B, lets an increment make the declared order the runtime order (F2, F3).

## 3. The non-negotiable contract

| Contract | Practical rule | Binding source |
|---|---|---|
| One owner per decision | `UnitBuilder`, `BaseBuilder` and `SquadManager` keep production, construction and squad authority. A learner is a provider or a veto input, never a second order issuer. Duplicate modules are merged, never run side by side. | DESIGN §19.3 |
| One owner per unit | A unit order from a module that doesn't hold the unit's lease is refused at the gate. An emergency may preempt it. | DESIGN §19.6 |
| Orders only | A bot acts only through `IBot.QueueOrder`. Direct actor mutation desyncs a multiplayer game. Synced state (for example the personality) changes only through an order round trip (`SetBotPersonality`). | DESIGN §19.8 |
| Fog honesty | Runtime features use only visible state, remembered sightings, public map/rules facts, and inference from them. Truth data may score an offline record (the engagement log's `truth` block) but never becomes a live feature. | DESIGN §19.5, §19.13 |
| No identity learning | No account, player name, slot history, map name, UID, hash, or coordinate lookup. Opponent memory is **per enemy faction** only, and opponent behaviour features exist only for the current match. | DESIGN §19.2; `learnables.yaml` `identity_policy` |
| Frozen artifacts | Release builds only read committed, reviewed files in `mods/cameo/ai/learned/`. Fitters run on dev builds and harness runs. A match never rewrites a model. Learned values apply inside the host's bot, never in rules at load. | DESIGN §19.2 |
| Bounded, deterministic runtime | Integer/fixed-point arithmetic, sorted iteration and explicit tie-breaks, 64-bit intermediates and clamps. A missing, invalid, sparse or disabled model falls back to today's hand-authored behaviour. | AI_ARCHITECTURE §12.22 (`ScaleTargetsEval`, fixed-point ×1000) |
| **Independent randomness** (new) | Each module draws from its own lobby-seeded stream, so a change in one module's draws never shifts another module's decisions. | proposed here, F1 |
| Declared tick layers | Every ticking module declares its layer (`LAYER_OF`); a seam that needs same-tick data is declared in `FRESH_EDGES` and the audit fails if it reads one tick late; a reorder never lands silently. A one-tick-old read is otherwise accepted, because publish cadences bound it. | AI_ARCHITECTURE §10.5c (AR-10); proposed runtime form: F3 |
| Switch-off equivalence | Every consumer sits behind its own default-off increment switch. With the switch off, the fixed-seed order stream is bit-identical. | AI_MASTER_PLAN §1.2; WORKFLOW |
| Traceable evidence | Artifacts carry rules, feature, reward and role schema versions, source counts and validation results. No reuse across schemas. | §17 |
| Director: no cheats | The Director changes attack timing and aggression only, never income, stats, production speed or vision. | DESIGN §19.2 |

## 4. What the research confirms

### 4.1 Learn by signal density, not by fashion

Fit many parameters only where each match supplies many relevant observations. Tune only a few where the reward is one
noisy match result (`design/AI_LEARNING_RESEARCH_2026-10-03.md`; DESIGN §19.13, five tiers, binding).

| Tier (DESIGN §19.13) | Suitable targets | Method | Scale |
|---|---|---|---|
| 1 Measured | combat calibration, effective role/unit value, timing and response priors | shrinkage integer regression / empirical Bayes | ~1,000 coefficients; ~500 logged battles |
| 2 Veto | engage/retreat safety | combat-prediction veto that learns nothing itself; its thresholds are tier-1/4 numbers | — |
| 3 Bandits | personality, opening, attack plan | Thompson sampling with an LCB safety floor, pooled global → family → faction → matchup | a few arms per decision |
| 4 Tuned knobs | bounded build/economy/strategy multipliers | paired A/B, SPSA, Bayesian optimisation | ~8 knobs per faction; 4–12 per group (§15.5) |
| 5 Engagement network | assault / flank / siege-first / harass / retreat when a squad meets resistance | one small fixed-weight network per platoon type, trained offline on ≥ ~50k logged engagements | ~5k weights per network |
| — In-match adaptation | transient caution or pressure | bounded rolling estimate, reset every match | gains are tier-4 knobs |
| — Fixed | fog legality, decision ownership, bounds, determinism | no learning | permanent invariant |

Tier 5 is **binding and last**. Its network advises the squad owner, which still issues the orders. Its weights are
frozen in release and quantised for deterministic evaluation. It is not a "neural controller issuing RTS orders", and
no whole-game neural policy is pursued. A large parameter count is never a goal (§15.5).

### 4.2 Strategy needs abstraction and commitment

Case-based opening choice, plan bandits and strategy switching belong at the strategic layer only. The bot picks or
updates a posture from a fog-honest snapshot, then holds it long enough to be legible: minimum holds, hysteresis,
emergency overrides and a logged switch reason. Tactical facts such as air presence change composition and priority,
not the coarse personality. Emergency means losing, and it never rewrites the posture (DESIGN §19.11). The Master owns
the main target, the personality and the published snapshot; specialist modules consume them.

### 4.3 Offline evaluation must precede promotion

Measure more than raw win rate: engagement trade, prediction error, objective outcome, order rate and safety,
duration, variety. Splits at match level keep all rows from one match together, and evaluation also holds out
contexts (faction matchups, ranges of continuous feature values). A candidate goes through schema validation, offline
fit checks, conservative confidence evidence, the paired increment A/B and a reviewed artifact commit. Propensity
logging and off-policy evaluation help triage candidates, but never replace the paired A/B.

---

# Part II — Verified state, 2026-10-09

Every number below was measured on `5c8cfe04` unless it names another source. The tree was then built (engine pin
`331657f07a`, 0 errors) and boot-gated to the main menu (route: LESSONS_LEARNED "Building and boot-gating in a Linux
cloud container"). On that built tree `ai_module_map.py --check` and `ai_arch_audit.py --check` both report their
committed docs **STALE**. A fresh generation, inspected and not committed, gives the counts below. Committing the
regenerated docs is phase A, A3.

## 5. The bot as built

### 5.1 Scale

| Measure | Value | How measured |
|---|---|---|
| Bot C# | ~125k lines: CA 26.5k, Cameo 43.6k, Fransbot 56.4k | `wc -l` over the bot module files |
| Loaded module types / instances | 91 / 172 | fresh `ai_module_map.py` run (unchanged from the committed map) |
| Module types in C# that are not loaded (check C3) | 19 (the committed map says 18; new: `AiEconomyHealthRecorder`) | fresh `ai_module_map.py` run |
| `IBot*` interfaces | 51 | `grep "interface IBot"` over CA, Cameo, Fransbot, Contracts, Unified |
| `QueueOrder(` call sites | 296; Fransbot holds the largest share (`FransMcvExpansionManager` 35) | grep |
| Modules that claim units through the lease registry | 16 (CA 5, Cameo 10, Fransbot 1) | grep `TryClaim/Transfer/Preempt` |
| Module blocks behind an experiment switch (`RequiresCondition: genericbot && …`) | 31 | `mods/cameo/ai/ai.yaml` |
| Hand-written interval/tick fields in module Infos | 313 | grep `public readonly int …(Interval\|Ticks)… = N` |
| `BotRng.For(...)` call sites | 94, in 23 files | grep |
| Full `World.Actors` scan sites / `FindActorsInCircle` / `ActorsWithTrait<>` in bot code | 45 (18 files) / 41 / 25 | grep (static sites, not per-tick cost) |
| Bot-module test coverage | 19.4 % line / 17.2 % branch | HANDOFF; `docs/audit/coverage_botmodules.md` |

**Fransbot.** Its code runs in full only as the separate `fransbot` bot type (`enable-fransbot`). Eight of its modules
also arm inside the Frankenstein through `inc3_frans_services` (`mods/cameo/ai/fransbot.yaml`). Of those eight, only
`FransCommanderCoreBotModule` has `QueueOrder` sites (2). Check that it holds leases and stays orders-only (§19.8)
before that switch is armed.

### 5.2 What is right (keep it)

* **One order gate** (`OpenRA.Mods.Cameo/Traits/ModularBot.cs:131`). Every order carries its issuer (`Type@N`, or the
  ambient `BotIssuer.IssueAs` scope) and the emergency flag. The order is judged against the lease table when it is
  *issued*, not when it is queued (AR-8). Grouped orders are filtered member by member and rebuilt from the members
  that remain (AR-1). Enforcement is on (`ai.yaml:4064` `EnforceAtOrderGate: true`).
* **Leases.** Squad membership is a lease with a heartbeat (`SquadManagerBotModuleCA`, LC1:
  `Math.Max(200, AttackForceInterval*4)` ticks). The idle pool is deliberately left unleased so that scouts,
  engineers and collectors can borrow from it, and a member that another module claims is handed off rather than
  fought over.
* **Advisors don't issue orders.** Spacing → `IBotPlacementAdvisor`, formation → `IBotAssaultFormation`, scale
  targets → `IBotScaleTargets`, production width → `IBotProductionWidth`, BO knobs → `IBotBuildOrderKnobs`,
  coalition → `IBotCoalition`. The existing owners consume all of them.
* **Seeded decisions.** `OpenRA.Mods.CA/BotRng.cs` seeds bot draws from the lobby seed per player.
  `BotPersonalityController` takes exactly one `SharedRandom` draw on every client (AR-2), and the bandit pin reaches
  the world only through a synced order.
* **Measurement exists.** Per-module timing every `ModulePerfReportIntervalTicks` (1500). The engagement log, mission
  cards (with `Released(match_end)` closing open attempts, `BotMissionLog.cs:239`), placement and expansion logs,
  `replay_health.py`, and `ai_log_common.match_score` as the one shared objective of the BO report and tuner.

## 6. The 2026-10-02 review: what is fixed

Source: [`history/design/CAMEO_AI_ARCHITECTURE_REVIEW_2026-10-02_POST_MERGE.md`](history/design/CAMEO_AI_ARCHITECTURE_REVIEW_2026-10-02_POST_MERGE.md).

| # | Finding | State on `5c8cfe04` | Evidence |
|---|---|---|---|
| 3.1 | Generated architecture evidence stale | **Open, confirmed**: both `--check`s report STALE on the built tree | Part IV A3 |
| 3.2 | Obsolete switch targets | Not re-audited | run `apply_increment_switches.py --dry-run` in A3 |
| 4.1 | TC-3 temporal coherence | **Open (unverified)**: no epoch or documented eventual-consistency rule found in `IBotCoalition.cs` | F2 covers the intra-bot half |
| 4.2 | TeamBroadcast liveness | **Fixed**: one rule, `BroadcastMaxAgeTicks = 500` | `IBotTeamMember.cs:220-231` |
| 4.3 | One responder for many rescues | **Unverified**: the responder is "the nearest free ally"; capacity is not re-checked | `IBotCoalition.cs:43` |
| 4.4 | `ClientIndex` as identity | **Fixed in the type**: `RequesterId` / `ResponderId` participant ids exist beside the legacy index | `IBotCoalition.cs:48-54` |
| 6.1 | `out_earned` depends on player count | **Fixed** | `BuildOrderKnobsBotModule.cs:449` |
| 6.2 | Report and tuner score differently | **Fixed**: both use `ai_log_common.match_score` | `build_order_report.py:15`, `tune_build_order.py:60` |
| 6.3 | Gate not truly paired; best-of-many selection | **Fixed**: paired z by experimental cell (Welch only when cells don't pair), Holm-Bonferroni step-down over the family | `tune_build_order.py:21, 318-348, 351, 478-520` |
| 6.4 | Opening learning confounded by knob experiments | **Fixed**: opening posteriors update from base/control-arm matches only | `tune_build_order.py:31-33` |
| 7 | Open mission attempts at match end | **Fixed** | `BotMissionLog.cs:53,239` |

## 7. The learning plane, verified

`tools/ai/learnables.yaml` (schema 1, sourced from the fleet's 40-row `LEARN_CATALOG_2026-10-06.md`, which is not
committed) is the registry this document's earlier version asked for. It landed 2026-10-07 (DEVELOPMENT_LOG
"LEARN-REGISTRY"). Its validator passes (3/3 tests).

| Registry state | Rows | Rows with a runtime consumer |
|---|--:|---|
| `wired` | 2 | `predictor_calibration` (switch `AP_tier1_priors`), `opening_build` (`AK_build_order_knobs`) |
| `wired_currently_enabled_without_increment_switch` | 1 | `effective_unit_value` (`BotLearnedPriors`, artifact `arsenal_priors.yaml`), see F10 |
| `planned` and `planned_*` variants | 37 | none |

Artifacts in `mods/cameo/ai/learned/`: `arsenal_priors.yaml`, `build_order_knobs.yaml`, `plan_bandits.yaml`.
`predictor_calibration` declares `mods/cameo/ai/learned/engagement_priors.yaml`, which **does not exist** (F9).
`MapFeatureVector` and `OpponentFeatureVector` have **no code** yet; `map_feature_vector` exists only as an allowed
scope key. The ruling "no map identity, a continuous feature vector instead" is recorded in the registry's
`identity_policy` but **not in DESIGN.md §19**. Promote it there (Part IV, A7), because memory and registries are
provenance, not authority (CLAUDE.md).

Verified learner constants (the annex quotes them): SPSA `a, c, A = 0.10, 0.08, 10`, exponents 0.602 / 0.101, weak
step 0.25, `MIN_MATCHES = 20`, multipliers 800–1250 ‰, Holm-Bonferroni step-down, SHA-256 perturbation signs
(`tune_build_order.py:64-75, 351, 655`). PlanBandit: `PriorCount 8`, `LcbZ 164` (1.64), `MinEvidence 4`,
`MinSafetyLcb -250`, Student-t Thompson (`PlanBanditMath.cs:24`). Tier-1 fitter: `SHRINK_VALUE 5000`,
`MIN_PAIR_SAMPLES 8`, correction 500–2000 ‰ (`fit_engagement_priors.py:52-55`; consumer
`EngagementPriorsBotModule.cs:185-188`). In-match adaptation: `GainPermille 15`, `MaxDeltaPct 20`, `IntervalTicks 250`.

---

# Part III — Findings

Each finding states its value and cost. F1–F8 are about the runtime, F9–F10 the learning plane, F11 this document
itself, and F12 a stale binding line.

### F1 — All modules share one random stream per player, which confounds every A/B *(high value, small cost)*
`BotRng.For(player)` returns **one** `MersenneTwister` per player (`OpenRA.Mods.CA/BotRng.cs`), and all 94 call sites
draw from it. Any change in the number of draws in one module shifts every later draw in every other module. Concrete
case: `PlanBanditBotModule` takes `df + 1` Box-Muller gaussians per arm (`PlanBanditMath.cs:130`, uniforms from
`BotRng.For(player)` at `PlanBanditBotModule.cs:256`), so the number of draws depends on how much evidence the learned
file holds. Arming `AO_tier3_bandits`, or simply committing a new `plan_bandits.yaml`, reshuffles scouting, squads and
build randomness in every other module. The increment A/B then measures that noise along with the change.
**Fix:** `BotRng.For(player, moduleKey)`, salted with a *stable* hash of the module type name (FNV-1a; never
`string.GetHashCode`, which .NET randomises per process). NUnit: module A's draws leave module B's sequence unchanged;
same seed ⇒ same sequence. It changes every draw once, so take a fresh baseline afterwards.

### F2 — Tick order is declared and audited, but only as a document *(medium value; builds on AR-10)*
`ModularBot` ticks modules in resolved `Player` child order: the merged yaml across every rules file, with ContentPack
`ai.yaml` includes loading **before** `mods/cameo/ai/ai.yaml`. Traits without dependencies keep that order
(`ActorInfo.TraitsInConstructOrder`). AR-10 already made this explicit (AI_ARCHITECTURE **§10.5c**, `ai_arch_audit.py`
**R8**): every ticking module declares a layer in `LAYER_OF` (R8a). `FRESH_EDGES` names seams that need same-tick data
(R8b; none today). The full stale-read table is generated into `AI_ARCH_COVERAGE.md` (58 last-tick read edges at its
last generation), and `--check` fails until a reorder is regenerated deliberately. §10.5c also ruled on the two lags
an earlier review cited: `ScaleTargets` and `BuildOrderKnobs` read MasterAi one tick old, which is the better
direction, because the builders that consume them read them fresh.
**What remains:** (a) the layer declaration exists only in the audit, while runtime order still follows the yaml.
ContentPack includes hoist the act modules (unit builder, squad managers, base builder) to tick positions 0–10, ahead
of every sense/decide module, so the declared sense→decide→act layering is not the order the code actually runs in.
(b) §10.5c judged an explicit reorder sync-safe, but because it changes every provider edge at once it left it to "the
lead's increment call". **Fix:** F3's scheduler is that reorder in a *switchable* form: it reads the same `LAYER_OF`
layers (moved into the module Infos so the runtime and the audit share one source) and orders by layer only for the
modules an increment opts in.

### F3 — Every module ticks every tick and runs its own clock *(medium value, medium cost; measure first)*
`ModularBot.cs:234`: "Every module ticks every tick … Attention gating needs a clock/decision split first." There are
313 hand-written interval fields and no shared schedule, so expensive scans can land on the same tick. Whether they do
is **unmeasured**; A6 measures it first. ⛔ This is **not** about capping the bot's actions: DESIGN §19.1 rules *no
APM cap* (`HumanPaceBotModule` stays at 0; a 120-orders/min cap went 0–4 in the A/B). The scheduler spreads CPU; it
never limits orders. **Fix:** a layer scheduler inside `ModularBot`. Each module declares `(layer, interval)`, where the
layer comes from F2's `LAYER_OF`. A module that declares nothing keeps its current position and interval 1, so nothing
changes until an increment opts a family in. Intervals are offset by a stable issuer hash so they don't line up.
**Freshness stamps are not part of this fix.** §10.5c considered and **rejected** a `snapshot.ComputedTick` stamp
("it would annotate the same staleness without removing it, and the cadence windows already bound it"). That premise
holds as long as cadences stay fixed. Revisit it only if C3 lengthens intervals enough that the cadence window no
longer bounds staleness, and then as a lead decision that amends §10.5c (B2).

### F4 — The strategist hub is coupled through its concrete type *(medium value, medium cost)*
`MasterAiBotModule` provides 16 interfaces, consumes about 20 modules, and lives in `BotSituation.cs` (2,596 lines),
where the file name doesn't match the class. Eight modules (AiPlacementLogWriter, BuildOrderKnobs, EngagementLog,
ScaleTargets, Scout, StealthDoctrine, TacticalMap, AiSituationLogWriter) bind to the **concrete class**, so the
"bounded seams" rule doesn't hold at its own hub. `MasterAiEval` (pure decisions, bit-identical) is the right start.
**Fix:** consumers bind only to interfaces. Split the file into `partial` files by concern (situation, target choice,
mission lifecycle, director/utility, team publish), keeping **one** owner and adding no second strategic brain
(2026-10-02 review §8). Name the file after the class.

### F5 — The order queue has no priority lane *(low–medium value, small cost)*
All orders share one FIFO: `MaxQueuedOrders 512`, the oldest is dropped when full, and `MinOrderQuotientPerTick 5`
issues at least 1/5 of the queue each tick (`ModularBot.cs:40, 51, 131-145, 268-288`). DESIGN §19.6 records the drop
and the action-budget return-to-front. With no action cap (DESIGN §19.1) the queue drains quickly, so the risk is
bursts: one module's burst can delay an attack response by a few ticks, or evict it if the queue fills. The 2026-09-28
cap experiment found that "orders piled up behind repeats" and that an order-lane fix helped (DESIGN §19.1). **Fix:**
orders queued with `Emergency == true` go in a lane that drains first, and orders for the same unit from the same
issuer are coalesced so only the newest is kept. Measure `ORDERGATE DROPPED` and queue depth first; build it only if
either is ever non-trivial.

### F6 — The gate can only judge leased units *(medium value)*
An order to an unleased unit always passes. Two modules ordering the same unit within the window are logged
(`ORDERGATE CROSSED`, `ModularBot.cs:215`) and counted per match in the `order_gate` block, which
`tools/ai/round_trip_check.py:96-101` reports, but only as a WARN. Nothing fails when the count rises. **Fix:**
compare the crossed count against the previous increment's baseline and fail on a rise (a metric that may only go
down). Any Fransbot module harvested into the Frankenstein must lease the units it orders. Today only
`FransTransportCommanderBotModule` does.

### F7 — Large files and thin tests *(prerequisite for F3/F4)*
`FransMcvExpansionManagerBotModule` 13,956 lines, `SquadManagerBotModuleCA` 3,723 (+ `GroundStatesCA` 1,642),
`TacticalMapBotModule` 3,067, `BotSituation.cs` 2,596, `BotEffectiveDamage` 2,514, `BaseBuilderQueueManagerCA` 2,111,
against 19 % line coverage. Before each refactor: an identical field dump (`tools/ai/dump_bot_modules.py` +
`diff_bot_modules.py`) and `RecordingBot` order-capture tests (`OpenRA.Mods.Cameo.Test/TestFixtures/RecordingBot.cs`),
so that "bit-identical" is proved, not claimed.

### F8 — Switches have no end of life
31 module blocks hang behind `genericbot && <switch>`, plus `increment_switches.yaml`, the 19 unloaded C3 types and
the dormant `fransbot` bot type. **Fix:** every switch gets an expiry. After its A/B it is either promoted (the
condition is removed) or deleted (the yaml and code go). The same rule applies to types: retire C3 types, and delete
`fransbot` once nothing in it is left unharvested (AI_SYNTHESIS §7.4 step 7).

### F9 — A wired learnable points at an artifact that isn't there
`predictor_calibration` is `state: wired` with `artifact.path: mods/cameo/ai/learned/engagement_priors.yaml`, and that
file is absent. Either the consumer falls back to defaults silently (then the state should say so), or the artifact
was never committed. **Fix:** commit the fitted file through the normal review, or change the state to
`wired_awaiting_artifact`. Extend `test_learnables_registry.py` so that `wired` requires the artifact to exist.

### F10 — A learnable is live without its own switch
`effective_unit_value` (`BotLearnedPriors`, `arsenal_priors.yaml`) is
`wired_currently_enabled_without_increment_switch`. That breaks switch-off equivalence (§3). **Fix:** give it its
own default-off increment switch, prove the order stream is identical with it off, then A/B it like any other.

### F11 — Errors in this document's first version (fixed in this revision)
1. **Contradiction with binding DESIGN §19.13:** the first version said "runtime neural weights: none in the target
   architecture". Tier 5 (one small fixed-weight engagement network per platoon type) is binding. §4.1 and §15.5 now
   say so.
2. **Map-row slip:** a blanket replace turned "pool by map" into "pool by MapFeatureVector/v1" in R1's first row, so the
   resolution read "remove MapFeatureVector/v1 learning", the opposite of the decision. Fixed in §15.1. The catalog's
   repeated parenthesis is now one definition (Appendix B header).
3. **Conditions from bot code:** Appendix G said bot logic affects the world through "orders/conditions". DESIGN §19.8
   allows orders only. Fixed.
4. **Missing sources:** the external reports (`deep-research-report*.md`) and the fleet research files
   (`RESEARCH_2026-10-05_rts_ai_learning.md`, `LEARN_CATALOG_2026-10-06.md`, `MAP_FEATURES_2026-10-06.md`,
   `OPPONENT_FEATURES_2026-10-06.md`, `AI_ARCHITECTURE_RESEARCH_SYNTHESIS_2026-10-06.md`) are **not in the repository**.
   §12 marks them so; commit them under `docs/research/` if they are to stay citable.
5. **Registry described as missing:** the action plan asked for a registry that already existed on 2026-10-07. It is
   now listed as shipped, with its gaps (F9, F10).
6. **Heading numbering:** the annex carried the source report's numbers (`## 5.1`, `## 8.19 B.`, a stray H1 inside
   Appendix E). Renumbered to the appendix letters.

### F12 — DESIGN §19.6's rollout line is stale
DESIGN §19.6 "Rollout" says `BotUnitLeaseRegistry.EnforceAtOrderGate` "is false until its A/B". The artifact says
otherwise: `mods/cameo/ai/ai.yaml:4064` has `EnforceAtOrderGate: true` under `RequiresCondition: genericbot`, so the
gate refuses today. The artifact wins (CLAUDE.md). **Fix:** the maintainer updates §19.6's rollout line (when and on
which A/B enforcement went on). This revision doesn't edit binding text.

---

# Part IIIb — The 2026-10-08 playtest: what arming every switch did to the bots

_Added 2026-10-09 at the maintainer's request. The question: "the idea of so many switches is that each of them
increases the bots' smartness … together the bot should be unstoppable … but the opposite happened." Everything below
is measured on master `5c8cfe04` plus the evidence named in each row. "Fixed today" means the fix is on master
`5c8cfe04`, not that it was proven in a match._

## PT1. What changed between the strong bots and the weak ones

| | Before (strong bots) | Playtest build (weak bots) |
|---|---|---|
| Experiment grants | 1 extra: `inc3f1` (the eight INC-3 Fransbot services, `hard` + exploit bots) | **20 grants** flipped from `fransbot`/empty to every difficulty tier (`37d9fc6a`, 10-08 19:43) |
| Behaviour fields | `AJ_field_coverage` on by default (maintainer ruling 2026-10-04) | **181 field changes**: the same 24 fields in **each** of the six `SquadManagerBotModuleCA@<personality>` blocks (144), plus 37 in the builders, planners, MasterAi and harvester module |
| Engineer/crate omniscience (DESIGN §19.5, binding) | on | **off**: `4bf69671` (10-08 19:46) set `CheckCaptureTargetsForVisibility`, `CheckRepairTargetsForVisibility` and crate `CheckTargetsForVisibility` to `true` |
| A/B evidence for what was armed | — | **none**: no group armed on 10-08 has a result in `AI_MATCH_LOG.md`; the last recorded increment A/B is INC-1+2+3 (2026-10-01, 13-3) |

The commit is exactly `apply_increment_switches.py --groups all`: on the pre-arm `ai.yaml` its dry run lists the
same 201 changes, and on today's `ai.yaml` it lists 0. **All 59 switch groups** are armed, and the result was committed
to master, although the tool's header says: "Never commit the result: master keeps the defaults until the increment's
A/B decides." So the playtest tested 59 groups of never-measured behaviour at once, in an 8-bot team game, a format several of
them had never been run in.

## PT2. Why "every switch adds intelligence" fails

These are the ways a stack of switches can lose strength; PT5 measures how much each one bites. (The first measured
1v1 did **not** turn passive, so in the playtest the team-game and bug factors weigh at least as much as points 1–2.)


1. **Most of the armed switches are restraints, not capabilities.** A restraint can only say "not yet" or "no": hold
   buildings, hold the army, veto a launch, keep a reserve, retreat earlier, yield a field to an ally. Each was built
   to stop one failure seen in one match (suicide attacks, stutter-stepping, over-commitment). A capability adds an
   action (repair, deploy, garrison, stealth squads, plugs, expansion). Of the 59 armed groups, the ones that sit
   directly on the attack or the base-building path are restraints (table PT3).
2. **Restraints compose by AND.** An attack launches only when *every* gate says go: army value ≥ the scaled target,
   the Director phase bar, the combat veto (≥ 60 % predicted against remembered defences), the defend reserve, no live
   Defend hold, and then the in-match adaptation does not pull it back. If each gate alone says "go" 80 % of the time,
   six of them together say go 26 % of the time. Nobody owns the question "has this bot attacked at all in the last
   N minutes?" There is no liveness rule. The one valve CA had, `MaxIdleUnits` (attack anyway once the idle army is
   big enough), is **scaled up by the same factor as the army target** when scale targets are on
   (`SquadManagerBotModuleCA.cs:3189-3193`), so it moves away exactly when the bar does.
3. **Each switch was built and checked against master with the others off.** A module that is correct alone can be
   wrong in combination: the army-first cash vote assumes something else will raise income; the expansion switches
   assume the refinery placer works on every faction; the combat veto assumes the army it judges was allowed to grow.
   Nothing measured the combination, and the AI_MASTER_PLAN §1.2 rule (one increment, one A/B, behaviour behind
   switches) exists to catch exactly this.
4. **Team features were armed into an 8-bot team for the first time.** Ten groups (R, S, V, W, BB, BC, BD, BE, BF, BH:
   sync attacks, defend answers, expansion claims, role split, the TC-3 coalition with its rescue and assist elections,
   sectors and main target, team capture claims) are "inert in 1v1", and their A/Bs are 1v1
   mirror matches (CLAUDE.md "A/B = mirror matches only"). They had never run at 8 allied bots.
5. **One armed module crashes the game** (F13), so part of the armed set could not have been measured even if someone
   had tried.

## PT3. The mechanisms, one by one

| # | Mechanism | Armed by | Symptom it produces | Evidence | State after today |
|---|---|---|---|---|---|
| M1 | Army-first cash vote: every non-essential building (second factory, tech, defences) is held while cash < 2,500 and stays held until cash ≥ 4,000. A bot spending income on units seldom reaches 4,000. On top: no optional building until 14 combat units exist while cash > 1,500. | `AE_army_first` + `MinArmyUnitsBeforeBuildings: 14`, `ArmyFirstMinCash: 1500` | small base, one factory, no tech or defences: "didn't build good bases" | `ArmyFirstBotModule.cs` `ArmyFirstEval.NextPaused` / `ArmyCountHolds`; yaml `ai.yaml:3962-3965` | **open** |
| M2 | Launch bar replaced by the scale-target army value (`max(own SquadValue × difficulty × time growth, seen enemy army × 0.6–1.4 × up to 2 for unscouted map)`), and `MaxIdleUnits` scaled by the same factor | `ST_scale_targets` | the idle army waits for a bar it may never reach; the forced-launch valve moves with it | `SquadManagerBotModuleCA.cs:3185-3193`; `ScaleTargetsEval.Target` | **open** |
| M3 | Combat veto: a wave launch is vetoed below a 60 % predicted ratio against remembered armed defences within 12 cells of the target | `AN_combat_veto` (+ `BM_live_combat_model`, `AP_tier1_priors`) | against a defended human base, waves never leave: "didn't attack us" | `CombatVetoBotModule.cs:34-50` | **open** (and its fitted priors never load, M9) |
| M4 | Director pacing: the launch bar × 150 % in Relief (after losses), × 100 % in Build-up | `P_di2_director_pacing` | after a lost fight, the bot needs a 50 % bigger army before it tries again | `SquadManagerBotModuleCA.cs:271-282, 3166-3175` | **open** |
| M5 | Caution layers: defend reserve (25 %, ×2 for turtles), defend preservation, in-match adaptation (losing tightens the retreat bar by up to 20 points), army staging (idle army waits at the defences) | `O_ca2_defend_reserve`, `Q_ut3_defend_share`, `AQ_inmatch_adapt`, `AM_army_staging` | more units held home, earlier retreats after early losses | the modules' Infos | **open** |
| M6 | Expansion money sinks: expand to every reachable field, greedy MCV appetite, and an expansion pre-build that requests refineries ahead of the MCV | `AC_cover_map_expansion`, `U_ut4_expansion_appetite`, `BT_expansion_prebuild` (+ AJ, already on) | cash goes into MCVs and refineries instead of an army | increment_switches rows AC, U, BT | **partly fixed**: MCVs that never deployed (`b06615a8`, engine `331657f07a`) now find a reachable cell. The appetite itself is unchanged. |
| M7 | Red Alert refinery dock check rejected every legal site; three failures could latch the base builder **for the rest of the game** (or trigger a conyard "relocation" that cancelled the whole queue) | pre-existing since 10-04 (REF-1); hit far more often with M6's extra refinery requests | the base stops growing mid-game: "sometimes just idle" | `368f4554` commit message; pre-fix `BaseBuilderQueueManagerCA.cs:142-195` | **fixed today** (`368f4554`) |
| M8 | Engineers and MCV-recovery crates limited to visible targets, against binding DESIGN §19.5 | `4bf69671` | engineers stop capturing (per §19.5 they "would just suicide"); a bot that loses its MCV cannot recover | the yaml diff | **fixed today** (`02241219` restored all three flags) |
| M9 | Learned files never load: every learned path is bare (`ai/learned/*.yaml`), but the mod is mounted only as `cameo|…` and `Folder.Contents` indexes top-level names only, so `FileSystem.Exists()` is false | all learners (`AO_tier3_bandits`, `AK_build_order_knobs`, `BotLearnedPriors`, `AP_tier1_priors`) | bandits pick arms from priors only (this run: personality `rush`, plan `fortify`); learned counters and priors never apply | runtime log: `plan-bandit learned: ai/learned/plan_bandits.yaml missing, priors only`, `LEARNED priors: … arsenal_priors.yaml missing`; `engine/OpenRA.Game/FileSystem/FileSystem.cs:257-266`, `Folder.cs:30-39` | **open** (present before the arm too) |
| M10 | Front/back planner crashes the game: `FindTilesInAnnulus(…, outer)` with `outer = ceil((FrontProj + 14) / cos 45°) + 1`, which exceeds the engine's `MaximumTileSearchRange` 50 once a defence front is ~21+ cells from the base centre | `BI_front_back_placement` (`Enabled: true`) | game crash (on "A Nuclear Winter" at world tick ~2,000) | this review's run: `ArgumentOutOfRangeException … requested range (71) cannot exceed … (50)` at `BaseFrontBackPlannerBotModule.cs:773` | **open** |
| M12 | MCV ownership conflict: the expansion pre-build claims the travelling MCV under `BaseBuilderBotModuleCA`'s name (`BotLeasePurpose.McvExpansion`, `BaseBuilderBotModuleCA.cs:1247-1272, 1881`), but the engine's `McvExpansionManagerBotModule` is the module that moves and deploys MCVs. With enforcement on, the gate refuses its orders | `BT_expansion_prebuild` + `EnforceAtOrderGate: true` | expansion MCVs stand still; money banks up | measured: **1,044 refused** `Move` orders (`ORDERGATE REFUSE McvExpansionManagerBotModule@0 … held by BaseBuilderBotModuleCA`, from tick 12,107) in the armed 1v1 | **open** (today's MCV fix repairs the deploy-cell search, not this) |
| M13 | Team escort drain: the armed expansion appetite (AC, U, greedy MCV) keeps every bot claiming far fields; each contested claim asks the TC-3 assist election to escort it, and defend requests elect rescuers. The elected bot's army spends the game escorting allies instead of attacking | `BH_tc3_assist_election`, `BC_tc3_rescue_election`, `S_tc2_defend_answers` with AC/U | in a team, some bots never attack and their army stays small: "passive, almost no units, no threat" | measured 3v3 (PT5): 90 escort answers + 46 defend answers; Multi0 made **0** attack waves in 60,000 ticks with a peak army of 22,270 | **open** |
| M14 | Personality convergence across a team. **Pre-arm:** `EmergencyKeepsPersonality: false` (master default) forces `turtle` on any emergency, so an early hit turns every bot of a team into a turtle; this contradicts binding DESIGN §19.11, whose fix ships only as switch group `AL_emergency_net_loss`. **Armed:** the plan bandit, running on priors only (M9), pinned the **same** personality arm on every teammate at tick 7 | master default (pre-arm); `AO_tier3_bandits` (armed) | a whole team plays one posture, often `turtle`: passive, few units | measured personality timelines (PT5): pre-arm 3v3 `expansion→turtle` (t 3,757), `turtle`, `steamroller→turtle` (t 4,657); pre-arm 2v2 both `→turtle` (t ~4,800–4,950); armed 3v3 all `→tech` at t 7; armed 2v2 both `→steamroller`. Code: `BotSituation.cs:2109` | **open**. The binding-ruling half is a one-line default change (`EmergencyKeepsPersonality: true`, `EmergencyLossArmyPct: 25`); the bandit half needs its cause found (why one arm for all teammates) |
| M11 | Plugs bought through real queues | `F_plug_spawn` + `02241219` | bots now pay for plugs (fair; previously an instant installer). Slightly more spending | `02241219` | changed today (fairness fix, not a strength fix) |

## PT4. Team play: how bots coordinate, and where humans fit in

| Channel | Bot → bot | Bot → human ally | Human ally → bot |
|---|---|---|---|
| Team blackboard (target vote, defend requests, sync waves, expansion and capture claims, TC-3 coalition) | yes, host-only and fog-honest (`TeamBlackboard.CollectBroadcasts`) | **no** | **no**: the collector only reads allies with `p.IsBot` (`IBotTeamMember.cs:256`), so a human ally publishes nothing and is never answered through it |
| Beacons | not used | **no**: no bot module ever issues `PlaceBeacon` (the only emitter is the human UI's `BeaconOrderGenerator`) | **yes**: `BeaconResponderBotModule` (always on for `genericbot`) sends up to 6 idle combat units to a beacon with seen/remembered enemies within 8 cells, or a repair unit to a beacon on an allied building |
| Chat / pings / attack-move markers | — | no | no |

What this means in play:
* **8 bots on one team** (the playtest) run every team feature at full scale. Every defend request elects a rescuer,
  every ally's Climax opens the others' launch window, and on a contested expansion field or capture target the bot
  with the lower ClientIndex wins while the rest stand down, so the later-seated bots of an 8-bot team expand less. None of this had been measured beyond 2v2 (see PT2 point 4).
* **A human with bot allies** gets help only by beaconing, and only from idle units (6 at most per bot); the bots
  never tell the human where they will attack or that they need help.
* **Gap list for the plan:** (1) a `BeaconSignalBotModule` that places a beacon when the bot publishes a defend
  request or commits a wave, rate-limited and only for allies that include a human, so a human sees what the
  blackboard already says; (2) a human-ally adapter for the blackboard (a human's base under attack, seen through
  shared allied vision, becomes a defend request the bots can answer); (3) a team A/B at 2v2 and larger before any
  team feature is armed by default, scored with `tools/ai/team_coordination_report.py` (shared pushes, defend
  answers, contested claims, coverage).

## PT5. Measured: the armed set vs the pre-arm set

Setup: this review's container (Linux, 4 cores, software rendering, so ~12–45 ticks/s), `tools/ai/run_ai_match_batch.py`
at `GameSpeed: maximum` (1 ms timestep), `--time-limit 1` (60,000 ticks), `hard` vs `classic`, `td_gdi` mirror,
"A Nuclear Winter" (2-player Tournament map). The **armed** arm is today's master `ai.yaml` with one change:
`BaseFrontBackPlannerBotModule Enabled: false`, because with it on the game crashes (M10). The **pre-arm** arm is today's
master with `37d9fc6a` reversed. Run logs: `/tmp/claude-0/run_*` in the review container (not committed).

| Arm | Result | Duration | Kills / deaths (value) | Buildings killed / lost | Earned / spent | Banked at end | Order gate |
|---|---|---|---|---|---|---|---|
| armed, full `ai.yaml` | **crash** at world tick ~2,000 (M10) | — | — | — | — | — | — |
| armed − front/back planner | `hard` **won** | 19,766 ticks | 74,610 / 13,100 | 26 / 0 | 236,540 / 158,360 | **86,723** | 1,044 refused (M12), 17 crossed |
| pre-arm | `hard` **won** | 22,759 ticks | 154,580 / 103,680 | 47 / 5 | 325,948 / 322,050 | 11,395 | 84 refused (`ExternalBotOrdersManager` vs squad leases), 3 crossed |

**3v3 on "Winter's End (Rich)"** (Tournament, 6 spawns; team A Multi0–2 `hard` top-left, team B Multi3–5 `classic`
bottom-right, verified): armed (minus the front/back planner), 60,000-tick limit, no exceptions. Result: **draw by
timeout** (every record says `lost`, the harness's way of recording a time-out). The `hard` team was far ahead on
attrition (kills 612,280 vs 268,220; buildings 61 killed vs 23 lost) but could not end the game in ~40 game-minutes.

| Bot | Kills / deaths (value) | Buildings killed / lost | Earned / spent per tick | Peak banked | Peak army | Attack waves (`secure`) | Escort / defend answers | Refused orders |
|---|---|---|---|---|---|---|---|---|
| Multi0 `hard` | 50,670 / 50,230 | 1 / 0 | 6.66 / 5.86 | 62,562 | 22,270 | **0** | 32 / 20 | 0 |
| Multi1 `hard` | 499,110 / 222,080 | 60 / 13 | 11.80 / 10.84 | 62,631 | 148,790 | 10 | 36 / 18 | **11,962** MCV (M12) |
| Multi2 `hard` | 62,500 / 29,610 | 0 / 10 | 7.52 / 6.70 | 62,361 | 48,800 | 2 | 22 / 8 | 0 |
| Multi3–5 `classic` | 268,220 total / 608,120 total | 23 / 61 total | ~4.9 / ~5.0 each | ~10,100 each | 48,480–63,850 | — | — | — |

`team_coordination_report.py`: team A made 244 mission attempts with 68 defend missions, but only **one** shared push
window (Multi1 + Multi2 against Multi4) in the whole match. The combat veto fired 4 times, so M3 is not what held them
back here. **This reproduces the playtest symptom:** two of the three armed bots spent the match as escorts and
defenders (M13), banked up to 62,000 each, and never threatened anyone. The one strong bot carried the team.

**2v2 on "Terra Cotta"** (Tournament, 4 spawns; `hard` ×2 vs `classic` ×2), armed (minus the front/back planner):
the `hard` team **lost** after 14,680 ticks (~10 game-minutes), no exceptions.

| Bot | Kills / deaths (value) | Buildings killed / lost | Earned / spent per tick | Peak army | Attack waves | Refused MCV orders |
|---|---|---|---|---|---|---|
| Multi0 `hard` | 12,600 / 88,030 | 1 / 25 | 6.65 / 7.11 | 18,210 | **0** | 162 |
| Multi1 `hard` | 29,300 / 96,260 | 2 / 39 | 5.87 / 6.28 | 22,140 | **0** | 492 |
| Multi2 `classic` | 128,880 / 17,850 | 40 / 1 | 9.51 / 10.27 | 64,510 | — | — |
| Multi3 `classic` | 54,750 / 25,290 | 24 / 2 | 8.47 / 9.31 | 52,510 | — | — |

No escort drain here (0 escort answers), so the cause is economic. The expansion loop logged 217 "MCV sent to field"
lines, 65 "parked field … no yard founded" and 46 greedy MCV requests: the same MCV at 26,88 is re-sent to field 25
every 20 ticks ("hand-out 1, 2, 3"), never moves because the gate refuses `McvExpansionManagerBotModule`'s orders (M12),
and the field is parked for 3,000 ticks. Both `hard` bots earned about 30 % less per tick than `classic`, built armies about a
third the size, and never launched a wave: **the playtest symptom, in a 2v2, from M6 + M12.**

**Same 2v2, pre-arm** (`37d9fc6a` reversed): the `hard` team **won** after 57,535 ticks.

| Bot | Kills / deaths (value) | Buildings killed / lost | Earned / spent per tick | Peak banked | Peak army | Refused orders |
|---|---|---|---|---|---|---|
| Multi0 `hard` | 492,060 / 541,270 | 76 / 41 | 14.29 / 14.48 | 11,000 | 90,710 | 102 (`ExternalBotOrdersManager` vs squad/scout leases) |
| Multi1 `hard` | 544,250 / 486,960 | 62 / 26 | 12.17 / 12.32 | 11,793 | 91,270 | 70 (same) |
| Multi2 `classic` | 516,440 / 517,150 | 35 / 73 | 8.43 / 8.57 | 10,093 | 67,910 | — |
| Multi3 `classic` | 505,120 / 513,490 | 33 / 66 | 8.55 / 8.70 | 10,081 | 56,290 | — |

**The A/B on one map, one match per arm:** pre-arm income per `hard` bot is about **twice** the armed income
(14.29 and 12.17 vs 6.65 and 5.87), peak armies about **four to five times** larger (~91,000 vs 18,210 and 22,140),
with **no refused MCV orders** (the M12 conflict needs `BT_expansion_prebuild`), and the match is won instead of lost.
(The "attack waves" column of the armed tables counts `secure:` mission cards, which only exist when
`UseRaidMissionSteering` is armed, so it is comparable between armed matches only; across arms compare kills and
buildings.)

**Same 3v3, pre-arm**: the `hard` team **lost** after 13,770 ticks (~9 game-minutes), no exceptions. All three `hard`
bots ended as `turtle` (two switched to it at ticks 3,757 and 4,657, when the omniscient `classic` bots first hit them),
peaked at armies of 7,260–12,690, earned 5.50–5.99 per tick and banked 20,691–22,864 each; kills 24,370 against
deaths 123,700 for the team; 0 buildings killed, 50 lost. The armed 3v3 drew; the pre-arm 3v3 lost. **M14** is the
visible cause here (emergency → everyone turtles).

**Reading the five matches together (n = 1 per arm, so directions, not verdicts):**

| Match | Armed (minus planner) | Pre-arm | Main cause seen in the losing/weaker arm |
|---|---|---|---|
| 1v1, A Nuclear Winter | won; spent 8.01/tick, banked 86,723 | won; spent 14.15/tick | M1 + M12 (spending speed) |
| 2v2, Terra Cotta | **lost**; ~30 % less income than `classic`, armies 18–22k | **won**; armies ~91k | M6 + M12 (MCV loop) |
| 3v3, Winter's End (Rich) | draw; two of three bots as escorts | **lost**; whole team turtled | armed: M13; pre-arm: M14 |

Neither configuration is safe. The armed set costs economy (M6, M12) and, in teams, attack power (M13). The pre-arm
default carries M14 (a binding ruling left switched off). That is why the next step is fixing the defects, not choosing
a switch set (PT7), and why every claim here needs repeats under the mirror A/B protocol before it becomes a verdict.

**Per world tick** (the two matches ran to different lengths): the pre-arm bot earned **14.32** vs **11.97**
(+20 %) and spent **14.15** vs **8.01** (+77 %); it ended with 11,395 banked against 86,723, and a larger army
(133,440 vs 106,210) despite fighting much harder (deaths 103,680 vs 13,100). Personalities differed (`turtle` vs
`steamroller`, from the random draw), and n = 1 per arm, so this is a direction, not a verdict.

**What the pair shows.** With the crash removed, the armed bot is **not** passive in a 1v1 against `classic`: it
fought early (kills from tick 2,250) and won in about 13 game-minutes. Both arms won, but the armed bot turns income into
army far more slowly. It holds back buildings and expansions (M1, M12) and banks the money: a third of its income was
unspent. So the restraint stack (M1–M5) alone does not reproduce "passive and idle" in a duel; what it measurably costs
is spending speed. The 3v3 above does reproduce it, through the team escort drain (M13) on top of the banking. The playtest differed from this duel in five ways that
all point toward weaker bots: an 8-bot **team** (the ten team groups, PT4), **human** opponents with defended bases
(the veto, M3), Red Alert factions (M7, fixed today), engineers limited to visible targets (M8, fixed today), and
possibly maps where the front/back planner's search stays under its crash limit.


## PT6. What today's updates fixed, and what they did not

| Today's change | Playtest problem it addresses | Status |
|---|---|---|
| `368f4554` passable-bib dock access + bounded law-refinery retry | M7: base builder latched / relocations on Red Alert factions | fixed (unit tests 79/79, suite 1270/1270, boot gate in its commit) |
| `b06615a8` + engine `331657f07a` reachable MCV deployment search | M6 part: expansion MCVs that never deploy | fixed in the search; runtime review still owed (`design/MCV_DEPLOY_CELL_REPAIR.md`) |
| `02241219` restores engineer/crate omniscience | M8 | fixed |
| `02241219` plugs via normal queues; limited superweapons by default | B2/B6 playtest bugs (rules), not bot strength | done; bots now pay for plugs |
| economy health logger, replay health gate, queue-transition seam | diagnostics only (no behaviour) | they will *detect* M1/M6/M7-type stalls in future matches |
| — | M1–M5 (the restraint stack), M9 (learned files), M10 (crash), M12 (MCV ownership conflict), M13 (team escort drain), M14 (team personality convergence) | **not addressed** |

## PT7. What to do

**For the next playtest (no code needed):**
1. Revert `37d9fc6a`'s grants and fields, keeping today's fixes. `git revert 37d9fc6a` stops on one hunk (the
   `BaseFrontBackPlannerBotModule` block, whose context now includes today's `SkipUnreachableDeployCellsCondition`
   line); keep that line and take `Enabled: false`. The rest reverses as-is (checked with `patch -R`). That restores the configuration that beat players.
2. If the playtest is meant to *show* features, arm **capabilities only**: K unit repair, L garrison defence, M deploy,
   X bridge repair, Y stealth squads, AB garrison contest, AH parallel production, AG assault fan-out, BJ/BK/BL/BO
   stutter fixes. Leave every restraint (AE, ST, AN, P, O, Q, AQ, AM) and BI off.

**Code fixes (small, each its own increment):**
3. M10: clamp `outer` to `world.Map.Grid.MaximumTileSearchRange` in `BaseFrontBackPlannerBotModule.Refresh()`, or
   walk the cone in rings without the annulus helper. Add a regression test with a front 40 cells out.
4. M9: write the learned paths as `cameo|ai/learned/…` (or resolve them through the mod package), and add a runtime
   assertion to `round_trip_check.py`: a configured learned file that logs "missing" while it exists in the tree
   fails the check.
5. M1: make the army-first cash vote relative to income (hold only while unit queues are actually starved), not to
   absolute 2,500/4,000 thresholds; drop `MinArmyUnitsBeforeBuildings: 14` until an A/B supports it.
6. M2: keep `MaxIdleUnits` as an **absolute** forced-launch valve, not scaled by the army target.
6a. M12: the module that issues the MCV's orders must hold its lease. Either the expansion demand hands the lease to
   `McvExpansionManagerBotModule` (`IBotUnitLeases.Transfer`, the negotiated handoff the engineer-transport path
   already uses), or the base builder emits the MCV orders itself under `BotIssuer.IssueAs`. Gate: 0 refused MCV
   orders in a match with `BT_expansion_prebuild` armed.
6b. M13: an escort or rescue answer must not cost a bot its offence. Answer an assist only from an army above the
   bot's own launch bar (spare units), cap concurrent escorts per bot at one, and expire an escort when the claim is
   uncontested. Gate: in a 3v3, every `hard` bot launches at least one wave in the first 30,000 ticks.
6c. M14: make the binding ruling the default: `EmergencyKeepsPersonality: true` and `EmergencyLossArmyPct: 25` on
   `MasterAiBotModule` in `ai.yaml` (today they are only in switch group `AL_emergency_net_loss`), and find why the
   priors-only bandit gives every teammate the same arm. A binding ruling never lives behind an experiment switch.

**Architecture (feeds Part IV):**
7. **A liveness rule for attacks.** Every restraint on the launch path gets a bounded hold, and one owner (the squad
   manager) guarantees "no more than N minutes without a launch while the idle army is above X". `replay_health.py`
   gets a matching check: no attack launch in the first N minutes = symptom hold. This is the same discipline the order
   gate applies to ownership.
8. **A restraint budget.** Classify every switch as restraint / capability / neutral in `increment_switches.yaml`, and
   never arm more than one new restraint per increment, so its cost is measurable.
9. **Combination tests before a playtest.** "Arm everything" is itself a configuration, and it gets one A/B like any
   increment: the full playtest set vs the shipped set, mirror matches, plus one team-size run (`--team-size`) for the
   team features. Phase A's parity and performance gates (Part IV) make that cheap.

## PT8. Should every number like these be learned?

**Yes, by binding ruling, but through the registry and in small groups.** DESIGN §19.2 (maintainer 2026-09-29):
"Every bot number is learnable. Today's values are starting points." The route is fixed: *measured* from logs, *tuned*
by paired experiments, or *chosen* by a bandit, as bounded multipliers on the defaults, frozen at match start.
The army-first thresholds (2,500 / 4,000 / 14 units / 1,500) are already registered: `credit_float` (rank 9,
`tools/ai/learnables.yaml`, owner `ArmyFirstBotModule` + `UnitBuilderBotModuleCA`, bounds 0–20,000 cash, method
`bayesian_hysteresis_threshold`), but its state is `planned`, its fitter `tune_credit_float.py` does not exist yet, and
its switch is unscheduled.

Four limits keep "learn every number" from being a shortcut:
1. **Scale.** The bot module Infos declare 1,272 `int` and 133 `bool` fields (before per-personality yaml overrides).
   One match is one noisy result, so tuning can move about 8–24 numbers at a time (§15.5; AI_LEARNING_RESEARCH
   2026-10-03: a 5-point win-rate difference takes 783–3,130 matches to detect). The registry's 40 families group the
   numbers so each learns where its signal is dense.
2. **Learned files do not load today** (M9). Until the paths are fixed, every learned value, including the shipped
   `arsenal_priors.yaml`, `build_order_knobs.yaml` and `plan_bandits.yaml`, is silently ignored.
3. **Learning cannot fix a missing rule.** Tuning a restraint's threshold finds the least-bad threshold. It does not
   add the liveness rule (PT7 item 7) that guarantees the bot attacks at all, or remove a crash (M10) or an ownership
   conflict (M12). Those are code fixes that come first.
4. **Some numbers must never be learned** (Appendix B.O): fog/legality, ownership and lease rules, the sync boundary,
   unit stats (balance pipeline only), and the difficulty line (DESIGN §19.1).

## PT9. Status of this investigation and where to continue

Done (2026-10-09): the static analysis M1–M14, the switch-set proof (201 changes = `--groups all`), the crash
reproduction (M10), the learned-file proof (M9), and one match per arm on three maps (PT5): 1v1 "A Nuclear Winter",
2v2 "Terra Cotta", 3v3 "Winter's End (Rich)". For the 3v3, team A Multi0–2 starts at 6,34 / 26,26 / 34,6 (top-left) and
team B Multi3–5 at 95,123 / 103,103 / 123,95 (bottom-right); `split_spawn_sides` gives that split for all 720 orderings
of the spawn list.

**Next, in order:** (1) the code and default fixes PT7 items 3, 4, 6a, 6c (A8–A10 and the M14 default), boot-gated;
(2) the M13 design call (PT7 item 6b); (3) repeats, at least 4 per arm and map with both spawn sides, of the fixed bot
against `classic` on the same three maps, scored with `tools/ai/ab_summary.py` and
`python tools/ai/team_coordination_report.py <support dir>`. In a fresh cloud session, follow LESSONS_LEARNED
"Building and boot-gating in a Linux cloud container"; the match harness needs `engine/bin/OpenRA.exe` → `OpenRA`
(a symlink inside the gitignored `engine/`) and `xvfb-run`. Keep any swapped `ai.yaml` out of every commit.

---

# Part IV — The improvement plan

## 8. Goals and the measures that prove them

| Goal | Measure (tool) | Target direction |
|---|---|---|
| Trustworthy experiments | A/B variance between two identical arms; order-stream parity with switches off (`order_stream_diff.py`, `round_trip_check.py`) | identical arms within noise; parity exact |
| Performance | per-module ms per tick and worst tick (the ModularBot timing report in the debug log; `replay_health.py` has no performance check yet, see C4 and `design/REPLAY_HEALTH_ANALYZER.md`) | lower mean, flatter peaks; a per-bot budget set from the first measured baseline (A6), not guessed |
| Ownership | `order_gate` refused/crossed counts per match (`round_trip_check.py`), preempts | only down (F6) |
| Smartness | increment A/B score (`ab_summary.py`, `ai_log_common.match_score`); engagement score vs prediction (`engagement_report.py`); predictor calibration error | up, with no regression in any faction cell |
| Behaviour | army mix per class and spam flags (`army_mix_report.py`); idle production and brownout ticks (MasterAi §13.1 counters); suicide index; mission outcomes (`mission_story.py`) | fewer spam flags and suicide attacks; more completed missions |
| Code health | coverage of bot modules (`coverage_report.py`), loaded vs C3 types, live switches | coverage up; C3 and switch counts down |

## 9. Phases

Sizes are relative (S ≤ 1 session, M 2–4, L 5+), so that AI_MASTER_PLAN §3 can turn them into PERT hours. Each
item names its gate. Items that change behaviour land behind a switch in an increment, and the increment is measured
by one mirror-match A/B (`--factions td_gdi` and `--factions td_nod` as separate shards).

### Phase A — Make the lab trustworthy (do first; everything later is measured with it)

| id | Work | Size | Gate |
|---|---|---|---|
| A1 | Per-module random streams (F1): `BotRng.For(player, moduleKey)` with an FNV-1a salt, then migrate the 94 call sites mechanically | S | NUnit independence test; build; boot gate; **new baseline** recorded |
| A2 | Declare in `FRESH_EDGES` (AR-10 R8b) every seam that B3 or C3 must keep same-tick, so the audit guards it **before** any reorder or interval change | S | `ai_arch_audit.py --check` clean on a complete tree |
| A3 | Regenerate evidence on a complete tree: `ai_module_map.py --write`, `ai_arch_audit.py --check`, `apply_increment_switches.py --dry-run`, `bash tools/audit/run_all.sh` | S | `--check` clean; switch dry-run lists no dead targets |
| A4 | Registry honesty (F9, F10): `wired` ⇒ artifact exists; give `effective_unit_value` its own switch | S | `test_learnables_registry.py`; order-stream parity with the switch off |
| A5 | Order-stream parity harness as a standing gate: fixed seed, all increment switches off, two runs ⇒ identical order stream | S | `order_stream_diff.py` exits 0 |
| A6 | Performance baseline: a league run with `ModulePerfReportIntervalTicks` on; commit the per-module ms table and the worst tick per bot | S | table committed under `docs/audit/` |
| A7 | Promote the map/opponent identity ruling from `learnables.yaml` into DESIGN.md §19 (maintainer sign-off) | S | maintainer ruling recorded |
| A8 | Playtest crash M10: clamp the front/back planner's annulus to `MaximumTileSearchRange`; regression test with a front 40 cells out | S | test; boot; the armed 1v1 on "A Nuclear Winter" runs past tick 2,000 |
| A9 | Learned files M9: resolve `ai/learned/*` through the mod package (`cameo|…`); `round_trip_check.py` fails when a configured learned file logs "missing" while it exists | S | log shows the three files loaded |
| A10 | MCV ownership M12: the issuer of MCV orders holds the MCV's lease (Transfer to `McvExpansionManagerBotModule`, or `IssueAs`) | S | 0 refused MCV orders with `BT_expansion_prebuild` armed |
| A11 | Playtest configuration rule (PT7): revert `37d9fc6a` (one trivial conflict) or arm capabilities only; never commit `--groups all` | S | maintainer call |

### Phase B — Give the runtime a skeleton

| id | Work | Size | Gate |
|---|---|---|---|
| B1 | Layer scheduler in `ModularBot` (F2, F3): the layer moves from the audit's `LAYER_OF` into each module Info (one source; the audit reads it back) plus an optional interval; modules not opted in keep today's position and interval 1. No action cap (DESIGN §19.1) | M | A5 parity with no module opted in; R8 still clean; build; boot |
| B2 | **Only if C3 makes cadences variable:** re-open the §10.5c decision on snapshot stamps (`ComputedTick`) with the A6/C3 numbers; a lead decision that amends §10.5c, not a coder task | S | written ruling |
| B3 | Opt modules into layer order, one family per increment (sense → decide → act), with stable interval offsets; each increment is the switchable slice of the reorder §10.5c left to the lead | M | per-increment A/B; R8 table regenerated; A6 shows flatter peaks |
| B4 | Order lanes (F5), **only if** A6's run shows `ORDERGATE DROPPED` or deep queues: an emergency lane; same-issuer, same-unit coalescing | S | NUnit with `RecordingBot`; A/B |
| B5 | Interface-only seams to MasterAi (F4): 8 consumers move to interfaces; partial-file split; file renamed | M | `dump_bot_modules`/`diff_bot_modules` empty; MasterAi tests green |
| B6 | Crossed-order ratchet (F6): `round_trip_check.py` fails on a rise against the previous increment's baseline; the lease requirement for harvested Fransbot code written into AI_MASTER_PLAN §1.2 | S | the check fails on a rise |

### Phase C — Performance (spend the measured headroom)

Optimise only what A6 shows is hot. The candidate list is static evidence, not a profile:

| id | Work | Size | Gate |
|---|---|---|---|
| C1 | One shared per-tick actor index. 45 full `World.Actors` scan sites in 18 files; serve them from one shared index built once per tick by a sense-layer module (own / visible enemy / remembered, by role) | M | identical order stream; A6 delta |
| C2 | Cache trait lookups. 381 `TraitsImplementing<>` sites in bot code: cache per Activate where the trait set is fixed; **never** cache conditional traits across calls (the AR-6 lesson, `ModularBot.cs` GateOrder comment) | M | parity; A6 delta |
| C3 | Interval tuning after B1: an expensive scan moves to a longer interval only if no `FRESH_EDGES` seam depends on it and its consumers' cadence window still bounds the lag (§10.5c) | S | R8 clean; A/B, no behaviour regression |
| C4 | A performance check in `replay_health.py` (the category already specified in `design/REPLAY_HEALTH_ANALYZER.md`), reading the ModularBot timing lines, with a per-bot ms budget taken from A6 | S | the analyzer fails over budget |

### Phase D — Smartness: the canonical learning order (unchanged order, now with gates)

The order is the one this document already set (§15, R2). It is restated with the gates the runtime now gives it:

| id | Work | Needs | Gate |
|---|---|---|---|
| D1 | Protect the P0 baseline: anonymous logging, parity, fog/no-identity audits as release gates | A1–A5 | parity exact; `audit_fog_honesty.py` |
| D2 | Fight trio: predictor calibration, effective value residuals, engage/retreat thresholds (default-off consumers) | D1, A4 | offline holdout calibration; A/B |
| D3 | Economy quartet: expansion, harvester/refinery, production-versus-income, credit float, without duplicating ECON-A/ECON-B owners | D2 | same |
| D4 | Registry completeness: every artifact key has a row; the audit rejects identity keys, out-of-range values and missing switches | shipped 2026-10-07; A4 closes its gaps | `test_learnables_registry.py` |
| D5 | `MapFeatureVector/v1` and `OpponentFeatureVector` as telemetry-only sense-layer providers (`LAYER_OF` PERCEPTION), with integer/fog/no-identity tests | D1, A7 | no consumer; parity exact |
| D6 | Conservative plan learning: initial-plan prior / contextual bandit with propensity logging, hold time and a fallback | D5, mature logs | LCB floor; league gate |
| D7 | Tier-5 engagement network (DESIGN §19.13): advises the squad owner; quantised fixed weights; trained once ≥ ~50k engagements are logged | D2, logs | holdout; A/B; league |

### Phase E — Behaviour

These items already sit in AI_MASTER_PLAN §3. Phase B changes how they are built:

* **UT, one blended squad manager over the bipolar utility axes**, is built as a decide-layer module on the B1
  scheduler, consuming interfaces only (B5). That replaces today's per-personality
  `SquadManagerBotModuleCA@rush/@turtle/@tech…` instances.
* **ZG → IM** (zone graph, influence layers) publish in the sense layer; any seam that scouting, siege or routing needs
  same-tick is declared in `FRESH_EDGES` (R8b).
* **CA-4 formation / CA-5 air doctrine / CA-2 siege** stay advisors. Their orders go through the owning squad state,
  under its lease.
* **Variety without blunders:** bandits (D6) propose and the combat veto disposes (DESIGN §19.13 tier 2). Pacing comes
  from the Director (no cheats). Human-likeness comes from delays and controlled mistakes scaled by difficulty
  (AI_MASTER_PLAN `HL`), never from an action cap (DESIGN §19.1).

### Phase F — Code health (continuous)

* Switch expiry (F8): every switch row in `increment_switches.yaml` gets `expires:` (the increment after its A/B); an
  audit lists overdue switches.
* Retire the C3 types and, at the end of the harvest, the `fransbot` bot type.
* Split each of the F7 files the first time an item touches it, behind an identical dump and RecordingBot tests.
* Coverage: every new or touched module ships with tests; `coverage_report.py` is tracked per increment.

## 10. How the work is delivered (binding workflow)

Per [`WORKFLOW.md`](WORKFLOW.md) and CLAUDE.md: Opus plans, specs, reviews and merges. Sonnet sub-agents write the code
in prepared worktrees (with `engine/` copied) and never commit. Devin agents code and hand in "INC-N ready". Only
Claude merges and runs A/B tests. Every C# change is rebuilt (`DOTNET_ROLL_FORWARD=LatestMajor dotnet build -c Release
--nologo -p:TargetPlatform=win-x64`) and boot-gated before commit (menu reached, no new `exception-*.log`). Engine-side
changes try a mod-side shadow type first (rule 7). All of phase B is mod-side (`ModularBot` is already the Cameo
shadow). Scoped `git add` only.

## 11. Promotion gates

A model, a consumer, or a runtime change (phases A–C) is promotable only when all of these hold:

- With its switch off, the fixed-seed order stream is bit-identical (A5).
- Runtime inputs pass the fog-honesty and no-identity audits.
- Integer overflow, sort order, missing/sparse evidence, schema mismatch and fallback tests pass.
- Its random draws come from its own stream (A1), and any seam it needs same-tick is declared in `FRESH_EDGES`
  (AR-10 R8b) and passes.
- It stays inside the per-bot time budget (C4) once that budget exists.
- The shipped artifact stays below its storage budget and doesn't grow with the number of maps or players.
- Offline evaluation uses match-level splits and reports support and confidence. The paired A/B clears the agreed
  lower-confidence floor with no regression in safety, invalid orders, CROSSED pairs, variety or game duration.
- A reviewer can rebuild the fitter input, manifest, bounds, switch and consumer from the committed files.

## 12. Sources

In the repository: `docs/DESIGN.md` §19 (§19.1, §19.2, §19.3, §19.5, §19.6, §19.8, §19.11, §19.13),
`docs/design/AI_ARCHITECTURE.md` (§10.5c, §12.22), `tools/ai/ai_arch_audit.py` (R8),
`docs/design/AI_MASTER_PLAN.md`, `docs/design/AI_SYNTHESIS.md`, `docs/design/AI_DEEP_RESEARCH.md`,
`docs/design/AI_LEARNING_RESEARCH_2026-10-03.md`, `docs/design/TIER4_SPSA_SPEC.md`,
`docs/history/design/CAMEO_AI_ARCHITECTURE_REVIEW_2026-10-02_POST_MERGE.md`, `docs/design/AI_MODULE_MAP.md`,
`tools/ai/learnables.yaml`, `mods/cameo/ai/`, the C# under `OpenRA.Mods.CA/Traits/BotModules/`,
`OpenRA.Mods.Cameo/Traits/BotModules/` and `OpenRA.Mods.Fransbot/Traits/`.

**Not in the repository** (cited by the first version, provenance only until committed): `deep-research-report.md`,
`deep-research-report-2.md`, `RESEARCH_2026-10-05_rts_ai_learning.md`, `LEARN_CATALOG_2026-10-06.md`,
`MAP_FEATURES_2026-10-06.md`, `OPPONENT_FEATURES_2026-10-06.md`, `AI_ARCHITECTURE_RESEARCH_SYNTHESIS_2026-10-06.md`.

---

# Part V — Research decisions and statistical annex

Sections 13–19 and Appendices A–I are the first version's research decisions, kept with the corrections listed in F11.
Where they and Parts I–IV differ, Parts I–IV win.

## 13. Current architecture versus the research recommendations

| Area | Current architecture/research | Synthesis decision | Next concrete work |
|---|---|---|---|
| Decision ownership | Explicit single-owner pattern and advisory seams; one order gate | Preserve | F4–F6, B4–B6 |
| Runtime scheduling | Merged-yaml tick order with declared, audited layers (AR-10); per-module clocks (F2, F3) | Make the declared order switchable at runtime; spread CPU, never cap actions | A2, B1, B3 |
| Fog and scouting | Fog/contact memory is foundational; `audit_fog_honesty.py` ratchet | Strengthen | Every new feature reads the seen side; hidden-state equivalence tests |
| Match telemetry | Anonymous engagement/mission/placement logs exist | Extend carefully | Only versioned, anonymous fields a named fitter needs |
| Combat learning | Predictor, priors and veto shipped behind switches | Adopt first | D2 |
| Economy learning | Builders/limits plus ECON-A/ECON-B seams | Adopt second | D3 |
| Map context | Registry allows `map_feature_vector`; no code yet | Adopt a continuous vector | D5, A7 |
| Opponent context | Faction profiles (DESIGN §19.2); per-match observed vector planned | Adopt with confidence | D5 |
| Plan/posture selection | PlanBandit (Student-t Thompson, LCB floor) behind `AO_tier3_bandits` | Extend only when logs mature | D6 |
| Engagement network | DESIGN §19.13 tier 5, binding, last | Adopt last | D7 |
| Whole-game deep RL / runtime LLM | Rejected (§19.13; §19.2: LLM offline analyst only) | Reject | — |

## 14. Coherence and contradiction register

| Subject | Conflict | Resolution |
|---|---|---|
| Per-opponent memory | Older research and outside RTS examples persist data per human opponent. | Per **enemy faction** only (DESIGN §19.2); per-match observed vector, discarded at match end. |
| Map conditioning | Research examples key data by map; an older fleet concept used discrete map classes. | No map ID, hash or bucket table. Use `MapFeatureVector/v1` (continuous, public geometry) and a smooth, bounded model. To be promoted into DESIGN §19 (A7). |
| Runtime model updates | Adaptive-AI literature sometimes persists live outcomes. | Shipped builds never change learned artifacts; only bounded, per-match controller state varies. |
| Omniscient training data | Truth state lowers variance offline; runtime must be fog-honest. | Truth only as a labelled target or score (the engagement log's `truth` block); live inputs stay on the seen side. |
| Dynamic personalities | CN-inspired examples set conditions directly from a bot module. | Keep the safeguards (hysteresis, cooldown, emergency logic), drop the mechanism: use the `SetBotPersonality` order round trip (DESIGN §19.8). |
| Neural weights | The first version said "none". | DESIGN §19.13 tier 5 is binding: one small advisory network per platoon type, last (D7). |
| Shared randomness | Not addressed in the first version. | Per-module streams (F1, A1) are a precondition for any A/B that needs to stay clean. |
| Snapshot freshness stamps | The 2026-10-09 first draft (AI_SYNTHESIS §8) proposed a `BuiltTick` stamp. | AI_ARCHITECTURE §10.5c already **rejected** stamps (cadences bound the one-tick lag). Withdrawn; re-opened only by B2's condition. |
| Action/attention budget | The 2026-10-09 first draft called the zero `HumanPaceBotModule` budget a gap. | DESIGN §19.1 ⛔ *no APM cap* (a cap lost 0–4). Withdrawn; the scheduler spreads CPU only. Human-likeness is `HL` (delays, mistakes). |
| Order-gate enforcement | DESIGN §19.6 says enforcement is off until its A/B. | `ai.yaml:4064` has it on. The artifact wins; the maintainer updates §19.6 (F12). |
| Implemented-feature claims | External reports describe pieces as present from snapshots. | Unverified until code or audit evidence shows them on the current branch (Part II). |
| Whole-game neural policy | Strong cited systems used massive distributed training. | Not pursued. |
| Broad product roadmap | Report two proposes ladders, campaign, doctrines, community timing. | Keep its stability-first message; route feature proposals to their owning design documents (§16). |

## 15. Integrated reconciliation with the statistical-parametric report

The decision spine (Parts I–IV) wins over this annex unless a later binding ruling says otherwise.

### 15.1 R1. Conflicts found and resolved

| Subject | Statistical-parametric report | Architecture synthesis | Final merged ruling |
|---|---|---|---|
| Map conditioning | Suggested hierarchical pooling **by map** in several candidate parameter rows. | Forbids map name, UID, hash, coordinate lookup or bucket table; adopts `MapFeatureVector/v1`. | **No map-ID or bucket pooling.** Map context is the continuous public-geometry `MapFeatureVector/v1`, with no stable map key. |
| Map provenance in learning logs | An earlier schema proposal included a map hash for reproducibility. | No persistent storage about a named map or a map hash. | No map identity or hash in learning records or artifacts. Pair experiments with a batch-local opaque pair token (Appendix E.3). |
| Opponent history | Rejected per-human profiles; discussed faction/matchup pooling. | Faction/doctrine plus a transient observed vector. | Faction/matchup priors where DESIGN §19.2 permits; behaviour features are per-match and carry no identity. |
| Runtime adaptation | Allowed bounded match-local adaptation. | Frozen artifacts; transient bounded in-match caution/pressure allowed. | **Controller state may adapt; the model may not train or rewrite itself.** |
| Runtime numeric model | Real-valued notation. | Integer/fixed-point, sorted, clamped, deterministic. | Fitters may use real-valued statistics; emitted artifacts and runtime evaluation are integer/fixed-point. |
| Strategy learning | Plan learning early. | Plan learning after P0, fight, economy, registry and telemetry providers. | **The spine's order wins** (D6). |
| Contextual bandits / OPE | Propensity logging and contextual bandits recommended. | Valuable, but never a replacement for paired validation. | Log propensities ahead of time; paired A/B stays mandatory. |
| Parameter count | V1/V2/mature degree-of-freedom totals. | Not a goal; game-level tuning stays small. | Counts are engineering ceilings only (§15.5). |
| Target context model | Algorithm-agnostic. | Integer additive ridge with reviewed, capped hinges; capped anonymous prototypes for discrete surfaces. | That form first; contextual bandits later. |
| Combat learning ownership | Rich engagement priors. | Fight trio and single-owner seams. | Separate writers per quantity; no competing combat authority. |
| Registry | A large parameter catalog. | A registry before the catalog grows. | **The catalog is research inventory; the registry is permission** (`learnables.yaml`, shipped). |
| Non-AI findings | Mostly ignored. | Routed to owning design documents. | Keep that separation. |

### 15.2 R2. Canonical learning order

1. Protect and prove the P0 baseline/parity/logging boundary (D1).
2. Complete the fight-learning trio (D2).
3. Complete the economy-learning quartet (D3).
4. Learning manifest and registry/audit (D4, shipped 2026-10-07; gaps in A4).
5. `MapFeatureVector` and `OpponentFeatureVector` as telemetry-only providers (D5).
6. Conservative plan/posture learning only once the evidence base is mature (D6).
7. Tier-5 engagement network (D7, DESIGN §19.13).
8. Route non-AI research to its design owners (§16).

### 15.3 R3. Registry contract

`tools/ai/learnables.yaml` implements this contract (schema 1). A live entry carries at least:

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

The audit rejects: an artifact key with no registry entry; a value outside its hard range; a violated relational
constraint; player or map identity; an unknown feature, reward or role schema; a consumer without its declared switch
and fallback (today: F10); evidence below the registered floor; duplicate ownership of one decision; **a `wired` row
whose artifact is missing (F9, to add).**

### 15.4 R4. Implementation evidence versus acceptance

Keep these four states distinct: **implemented in code** ≠ **wired behind an increment** ≠ **A/B validated** ≠
**promoted/default-on**. Part II records the 2026-10-09 state. Every release claim is re-checked against the
current branch and audits.

### 15.5 R5. Parameter-count policy

Don't optimise toward a parameter count. These are **maximum working budgets**, not goals:

| Layer | Working budget |
|---|---:|
| One SPSA/coordinate group | normally 4–12 scalar DOF |
| Deliberately coupled expert group | up to ~16; >16 needs explicit justification |
| Absolute ceiling for one simultaneous black-box tune | 24 |
| Discrete arms in one decision | small enough for meaningful support; pool hierarchically |
| Measured residual cells | hundreds to thousands, when each engagement labels them directly and sparse cells shrink to a fallback |
| Runtime neural weights | none today; tier 5 adds ~5k per platoon-type network (DESIGN §19.13, D7) |

## 16. External framework findings outside the bot

The second external report is much broader than AI. Its project-level proposals are kept so they are not quietly
lost, but they are **not binding AI architecture**; their owning design documents decide them.

| External finding | Coherent use in Cameo | Status here |
|---|---|---|
| Stability before feature breadth | Boot, crash, performance and multiplayer/desync evidence are release gates; learning experiments never mask a broken baseline. | Adopted as delivery policy. |
| Shared mechanics, legible faction doctrine, then spectacle | Keep the AI rules-derived and faction-aware; gameplay identity belongs to the faction, balance and formula documents. | Matches the existing separation. |
| Counter, telegraph, cost, opportunity cost | Evaluation dimensions next to win rate; bots learn visible counterplay, never hidden state. | Adopted as an evaluation principle. |
| Soft-counter/role readability | Consumers read role facts derived from rules and public observations, not unit-name heuristics. | Matches the role/arsenal direction. |
| Doctrines; faction/campaign proposals | Assessed separately; bot inputs only once their gameplay contract is stable. | Deferred to the owning documents. |
| Ladder, tournament, community, service roadmap | Product hypotheses that need maintainer capacity and legal review. | Not an AI decision. |

## 17. Approved target data model

### 17.1 Context at runtime

Each learned policy consumes a narrow tuple:

```text
(own faction, public enemy faction/doctrine, current fog-honest decision context,
 MapFeatureVector?, OpponentFeatureVector? with per-field confidence)
```

`MapFeatureVector` is built at load from public geometry and the bot's legal own start. `OpponentFeatureVector` is
recomputed during the match from sightings and visible effects only. Both carry schema versions and missing-value
bits, and both are published by sense-layer modules (`LAYER_OF` PERCEPTION). Until their increments are on, the
providers return no adjustment, and their absence leaves current behaviour exactly as it is.

The artifact form is an integer additive ridge model with a reviewed, capped hinge registry. Discrete choice surfaces
may instead use a capped anonymous prototype set with fixed weighted-L1 smoothing. Both shrink sparse matchup
evidence through matchup → faction → global, have file-size caps, and never keep a row per map or per human opponent.

### 17.2 Logs and fitting

Logs record the decision context, the declared eligible actions, the selected action, model and schema versions,
confidence, and the later outcome for the decision's owner. They label which fields are observed and which are truth.
Fitters reject mixed rule, feature, reward or role schemas. Every emitted artifact carries:

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

## 18. Explicit non-goals

- A runtime LLM, remote service, or neural controller **issuing** RTS orders. The tier-5 network only advises the
  squad owner.
- Hidden truth replayed as a live tactical input.
- Persistent storage about a person, account, seat, named map, map hash, or historical coordinate.
- Automatic promotion or mutation of a model after a public match.
- Replacing the ownership graph with a new central order issuer. The layer scheduler (B1) orders *ticks*; it issues
  no orders.
- An action (APM) cap on the bot (DESIGN §19.1). Difficulty scales through delays, self-preservation and production,
  never through the bot's hands.

## 19. Result

The external research adds statistical discipline, conservative evaluation ideas and a stability-first reminder.
Cameo's architecture supplies the engineering constraints: explicit order ownership, fog-honest observation,
deterministic integration, audited artifacts and switch-gated rollout. The 2026-10-09 review adds the missing runtime
contract: independent randomness, the declared layer order made runtime-switchable, interface-only seams and a
crossed-order ratchet. The
plan is to lay that skeleton (phases A–B), measure and spend performance headroom (C), and then grow smartness and
behaviour in small, observable increments (D–E).

---

## Appendix A — External statistical research details

### A.1 Learned combat models: highest immediate value

Stanescu, Barriga & Buro's StarCraft work is especially relevant because it learns **corrections to an analytical combat model**, not an entire policy. Their central lesson transfers cleanly:

- derive a physically/statistically meaningful prior from unit properties;
- fit residual strengths from actual engagements;
- regularize toward the prior;
- use the calibrated model inside attack/retreat decisions.

Uriarte & Ontañón similarly show that efficient combat abstractions can provide useful outcome estimates without full simulation.

**Cameo fit:** exceptionally strong. Cameo already has structured weapon/Versus data and a balance pipeline, so its stat-derived prior is richer than a generic unit-id strength table.

### A.2 Bayesian and hierarchical opponent/strategy models

RTS strategy inference research under partial observation demonstrates that enemy intent can be estimated from sparse, fogged observations. Bayesian opening models and HMM strategic-state models are useful mainly as **features and priors**, not as replacements for the bot.

**Cameo fit:**

- use public faction when known;
- pool Random/unknown faction to higher-level priors;
- classify a small number of strategic states from fog-honest evidence;
- update belief inside a match;
- do not persist individual-human identity.

### A.3 Thompson sampling and conservative bandits

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

### A.4 SPSA and stochastic black-box optimization

SPSA is well suited to Cameo because one gradient estimate costs two experiment arms regardless of dimension. But its efficiency does not mean “tune everything together.”

Best practice for Cameo:

- use common random numbers / paired maps, spawns, opponents and seeds;
- normalize parameters;
- perturb coherent groups;
- keep hard bounds;
- preserve an untouched validation set;
- require a final league gate after the adaptive optimization loop.

Current Cameo's log-multiplier implementation and deterministic SHA-256 perturbations are strong choices.

### A.5 Bayesian optimization / SMAC

Bayesian optimization is attractive when:

- evaluation is very expensive;
- dimensionality is low;
- parameters interact nonlinearly;
- an SPSA group proves too noisy.

Gaussian-process BO is best kept to small dimensions. Tree/forest model-based optimizers such as SMAC are more natural if future groups contain mixed categorical and numeric values.

**Recommendation:** fallback/specialist tool, not the default training loop.

### A.6 CMA-ES

CMA-ES is useful where:

- a small vector has strong interactions;
- local gradient estimates are unreliable;
- evaluation can be done in cheap tactical scenarios.

The clearest Cameo candidate is a formation/micro vector, not global economy.

**Recommendation:** optional specialist optimizer after a deterministic skirmish harness exists; full-game validation remains mandatory.

### A.7 Contextual bandits and off-policy evaluation

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

### A.8 League/self-play lessons

AlphaStar's directly useful lesson is not its neural architecture. It is training diversity:

- current bot;
- past versions;
- exploiters;
- alternative personalities;
- multiple maps/factions.

A candidate that only beats one baseline can overfit a single strategic niche.

Cameo already has `tools/ai/run_league.py`; use it as the release gate for learned files.

### A.9 Whole-game deep RL

Whole-game learned policies require far more data and hardware than Cameo's current development loop can justify. They are also harder to explain, audit, preserve across faction additions and reconcile with the project's modular ownership rules.

**Recommendation:** remain deferred.

---

## Appendix B — Candidate learnable-parameter catalog (registry seed, not approval)

The following catalog treats a “parameter” as a semantically meaningful scalar/vector or sparse table. It deliberately distinguishes raw stored coefficients from independent optimization degrees of freedom.

**Conventions.** In the Pooling column, *map features* always means `MapFeatureVector/v1`: continuous public-geometry features with no map identity, hash or bucket key (§14, §15.1). This catalog is research inventory; a row becomes live only through a `tools/ai/learnables.yaml` entry (§15.3). Owner names are the intended consumer and may not be loaded today (check `design/AI_MODULE_MAP.md`).


### B.A Combat measurement
| Parameter / family | Owner | Context | Type / safe bounds | Learning route | Update | Reward/evidence | Pooling | Safety / dependency |
|---|---|---|---|---|---|---|---|---|
| delivery_armour_residual[d,a] | EngagementPriorsBotModule / BotCombatPredictor | Damage effectiveness by delivery family × armour | positive multiplier; 0.5–2.0 | Tier 1 empirical-Bayes fit | offline batch | prediction residual / value lost | global cell; sparse family residual later | staleness tied to resolved Versus PriorPct |
| global_combat_scale | EngagementPriorsBotModule | global residual calibration | positive multiplier; 0.5–2.0 | Tier 1 | offline batch | observed/expected loss | global | must not absorb balance errors blindly |
| attrition_exponent | BotCombatPredictor | Lanchester/order-of-attrition shape | continuous ~0.5–2.0 | Tier 1 fit | offline batch | outcome likelihood/error | global first | tight regularization; one scalar |
| into_defences_milli | EngagementPriors / predictor | penalty/correction attacking static defence | 0.5–2.0 | Tier 1 | offline batch | observed vs predicted defence fights | global→family | seen inputs only live |
| defence_state_residual[delivery/state] | EngagementPriors | garrisoned/deployed/static defence states | 0.5–2.0 | Tier 1 | offline batch | defence engagement residual | global sparse | minimum evidence |
| range_band_residual[delivery,range_band] | future richer priors | range advantage omitted by base predictor | 0.7–1.4 suggested | Tier 1 later | offline | prediction error stratified by range | global | add only if residual analysis shows structure |
| hp_fraction_residual[class,band] | future richer priors | damaged-force effectiveness | 0.7–1.3 | Tier 1 later | offline | survival/trade residual | global | avoid double counting HP already in predictor |
| visibility_factor[phase] | Master/CP calibration | remembered army vs offline truth by phase | 0.1–1.0 | Tier 1 diagnostic | offline | seen/truth gap | global→map features | never feed truth directly live; use only learned conservative prior |
| attack_timing_quantiles[enemy_faction,phase] | opponent-model feature | typical enemy attack timing | quantiles | Tier 1 measured | offline | defend/attack timestamps | global→family→enemy faction | public faction only |
| response_time_quantiles[own_faction,phase] | Master / defence planning | own response latency | quantiles | Tier 1 measured | offline | first hurt→first mobile damage | global→own faction | measurement first; don't optimize latency by hidden info |
| suicide_index[matchup] | Veto diagnostics | value entering defences vs defences killed | nonnegative ratio | Tier 1 measured | offline | EL tactics/outcome | family→matchup | diagnostic can become veto feature |
| unit_role_trade_residual[role,enemy_role] | arsenal/production | role effectiveness | 0.5–2.0 | Tier 1 empirical Bayes | offline | value traded/lost | global→family→matchup | prefer roles over actor IDs |
| cause_of_loss_share[role] | production/defence advisors | what actually killed army/base | simplex | Tier 1 | offline | death attribution | global→matchup | minimum evidence |
| prediction_calibration_intercept | BotCombatPredictor | calibrate predicted ratio to realized outcome | bounded scalar | regression | offline | Brier/log loss / residual | global | holdout calibration |
| prediction_calibration_slope | BotCombatPredictor | over/under-confidence | 0.5–2.0 | regression | offline | Brier/log loss | global | holdout calibration |

### B.B Discrete strategy
| Parameter / family | Owner | Context | Type / safe bounds | Learning route | Update | Reward/evidence | Pooling | Safety / dependency |
|---|---|---|---|---|---|---|---|---|
| personality_arm | PlanBanditBotModule | rush/turtle/tech/expansion/steamroller/guerrilla | categorical | Tier 3 Student-t Thompson | match-start | continuous EL/match composite | any→family→faction→matchup | LCB safety floor; freeze per match |
| plan_overlay_arm | PlanBanditBotModule | named bounded knob overlay | categorical | Tier 3 Student-t Thompson | match-start | EL total_milli / match score | same hierarchy | record armed-set survivorship |
| opening_arm | BuildOrderKnobs + PlanBandit integration | authored build category sequence | categorical | Tier 3; continuous/episode reward preferred | match-start | opening episode value + later match regularizer | faction→matchup→plan class | do not duplicate two incompatible opening learners |
| composition_arm | UnitCompositions / UnitBuilder | safe authored composition package | categorical | bandit later | episode/match | role-adjusted trade and objective | faction→matchup | tech eligibility/veto first |
| attack_doctrine_arm | Squad/mission planner | direct/siege/flank/harass | categorical | contextual bandit later | engagement | engagement score | platoon type→context cluster | requires propensity logging and veto |
| support_power_doctrine_arm | SupportPowerBotASModule | hold/coordinate/immediate use | categorical | bandit later | episode | objective swing / value denied | power family | never per individual power until evidence |
| team_role_arm | Team blackboard/TC | ground/air/tech/raider role split | categorical | bandit later | match | team result + marginal contribution | team size/faction family | avoid unstable role thrashing |

### B.C Build/economy SPSA
| Parameter / family | Owner | Context | Type / safe bounds | Learning route | Update | Reward/evidence | Pooling | Safety / dependency |
|---|---|---|---|---|---|---|---|---|
| knob_tempo | BuildOrderKnobsBotModule | overall build timing multiplier | learned multiplier 0.8–1.25 | Tier 4 SPSA | offline | match+0.5 EL composite | global→family→faction/personality | current SPSA group |
| knob_greed | BuildOrderKnobsBotModule | economy investment | 0.8–1.25 learned overlay | Tier 4 | offline | composite + economy efficiency diagnostics | family→faction/personality | bounded by authored preset |
| knob_production | BuildOrderKnobsBotModule | production infrastructure | 0.8–1.25 | Tier 4 | offline | composite | family→faction/personality | separate from unit actor weights |
| knob_tech | BuildOrderKnobsBotModule | tech investment/timing | 0.8–1.25 | Tier 4 | offline | composite / tech timing | family→faction/personality | preserve faction identity priors |
| knob_defence | BuildOrderKnobsBotModule | defence investment | 0.8–1.25 | Tier 4 | offline | composite / base-loss diagnostics | family→faction/personality | not same as hidden HP bonus |
| knob_power_margin | BuildOrderKnobsBotModule | power headroom | 0.8–1.25 | Tier 4 | offline | brownout/idle/composite | family→faction | hard safety floor against power collapse |
| knob_expansion | BuildOrderKnobsBotModule | expansion appetite | 0.8–1.25 | Tier 4 after consumer verification | offline | composite + expansion ROI | family→faction/personality/map features | only enable in tuner after its consumer effect is verified |
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

### B.D Combat/veto tuning
| Parameter / family | Owner | Context | Type / safe bounds | Learning route | Update | Reward/evidence | Pooling | Safety / dependency |
|---|---|---|---|---|---|---|---|---|
| veto_engage_ratio_pct | CombatVetoBotModule | uncommitted engage safety bar | bounded around 50 | Tier 4 separate group | offline | trade + veto counterfactual score | global→personality/family | keep hysteresis |
| veto_abort_ratio_pct | CombatVetoBotModule | already-committed abort bar | bounded around 35; < engage | Tier 4 | offline | trade/preservation | global→personality | constraint abort < engage |
| veto_launch_ratio_pct | CombatVetoBotModule | wave launch safety bar | bounded around 60 | Tier 4 | offline | launch outcome/match | global→personality/family | fog parity floor retained |
| veto_flee_speed_margin_pct | CombatVetoBotModule | whether retreat can outrun pursuit | 80–130% suggested | measure/tune later | offline | retreat survival | global/platoon type | physical interpretation |
| defence_include_cells | CombatVetoBotModule | remembered static defence inclusion radius | bounded cells | measure/tune later | offline | predictor calibration | global/map features | tie to weapon/region scale if possible |
| retreat_ratio_base | SquadManager/CP | shared commit/retreat bar | bounded percent | Tier 4 | offline | engagement value preserved | personality→family | one engagement authority only |
| inmatch_gain_permille | InMatchAdaptBotModule | how strongly current match form moves bar | 0–30 suggested around current 15 | Tier 4 | offline | same-match subsequent engagements | global→personality | live state resets every match |
| inmatch_max_delta_pct | InMatchAdaptBotModule | hard adaptation bound | design safety constant; current 20 | authored / maybe very conservative tune | release design review | tail-risk analysis | global | prefer DO NOT LEARN in V1 |
| inmatch_interval_ticks | InMatchAdaptBotModule | adapt recompute cadence | current 250 | authored | n/a | latency/stability | global | DO NOT LEARN initially |
| combat_prior_min_correction | EngagementPriors | minimum fitted damage correction | current 500 | safety constant | design review | tail calibration | global | DO NOT LEARN automatically |
| combat_prior_max_correction | EngagementPriors | maximum fitted damage correction | current 2000 | safety constant | design review | tail calibration | global | DO NOT LEARN automatically |

### B.E Scale/utility/director
| Parameter / family | Owner | Context | Type / safe bounds | Learning route | Update | Reward/evidence | Pooling | Safety / dependency |
|---|---|---|---|---|---|---|---|---|
| scale_army_ratio | ScaleTargetsBotModule | army size response to seen enemy | bounded multiplier | Tier 4 group | offline | match/combat score | family→personality | difficulty line remains separate |
| scale_defence_ratio | ScaleTargetsBotModule | defence target response | bounded | Tier 4 | offline | base survival/resource efficiency | family→personality | avoid runaway turtling |
| scale_aircraft_ratio | ScaleTargetsBotModule | air target response | bounded | Tier 4 later | offline | air engagement/match | family | depends on air doctrine |
| scale_unscouted_margin | ScaleTargetsBotModule | uncertainty reserve for unseen enemy | nonnegative bounded | Tier 4 only after fog audits | offline | hidden-truth calibration + match | map features/phase | truth only fits coefficient offline |
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

### B.F Scouting/information
| Parameter / family | Owner | Context | Type / safe bounds | Learning route | Update | Reward/evidence | Pooling | Safety / dependency |
|---|---|---|---|---|---|---|---|---|
| scout_rebuild_cooldown_ticks | ScoutBotModule | replace lost scouts | bounded ticks | Tier 4 | offline | intel gained per scout cost | global→faction family | current replacement-rationing evidence |
| scout_count_target | ScoutBotModule / ScaleTargets | number/value of scouts | bounded integer/ratio | Tier 4 | offline | information gain minus losses | personality/family | use roles rather than actor IDs |
| enemy_spawn_bonus | ScoutBotModule | priority of probable spawn | bounded score weight | Tier 4 V2 | offline | time-to-first-contact / useful intel | map features | public spawn data only |
| staleness_weight | Master target/scout | value of refreshing old intel | bounded weight | Tier 4 | offline | information gain / decision improvement | global→map features | same staleness semantics across modules |
| interest_weight | Scout/Influence | prefer economically/militarily important zones | bounded | Tier 4 | offline | intel utility | global | avoid chasing hidden truth |
| route_risk_weight | RegionRouter/Scout | risk cost for scout routes | bounded | Tier 4 | offline | scout survival + info | platoon type / map features | separate air/ground if needed |
| explore_confirm_mix | ScoutBotModule | new terrain vs re-check suspected enemy | simplex/0..1 | Tier 4 | offline | information value | personality/map features | requires logged reason/outcome |
| memory_decay_ticks | BotFogMemory/Influence | how stale remembered mobile threat becomes | positive ticks | measured first | offline | seen→truth predictive calibration | map features/unit role | do not erase known static buildings identically |
| influence_history_alpha | BotInfluenceLayers | EMA update rate | 0..1 | Tier 4 V2 | offline | future threat prediction | map features | fit prediction, not win rate first |
| influence_decay_ticks | BotInfluenceLayers | blend current memory toward history | positive ticks | Tier 4 V2 | offline | threat prediction error | map features | paired with alpha |
| influence_neighbor_spread | BotInfluenceLayers | threat reach across adjacent zones | bounded fraction | measure/tune V2 | offline | combat location prediction | map features | avoid double count weapon range |

### B.G Production/counter mix
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

### B.H Expansion/base
| Parameter / family | Owner | Context | Type / safe bounds | Learning route | Update | Reward/evidence | Pooling | Safety / dependency |
|---|---|---|---|---|---|---|---|---|
| exp_resource_weight | ExpansionPlannerBotModule | field economic value | positive normalized weight | Tier 4 specialist group | offline | ROI/match | map features/family | normalize score terms |
| exp_threat_weight | ExpansionPlannerBotModule | safety penalty | positive | Tier 4 | offline | expansion survival/ROI | map features | fog-honest threat |
| exp_distance_weight | ExpansionPlannerBotModule | travel/payback cost | positive | Tier 4 | offline | ROI/time-to-pay | map features | path distance, not hidden shortcut |
| exp_ally_claim_weight | ExpansionPlanner/TC | avoid allied conflict | positive | Tier 4/team later | offline | team expansion efficiency | team size | deterministic claims remain |
| mcv_expansion_trigger | McvManager/ExpansionPlanner | when to send/found expansion | bounded resource/time ratio | Tier 4 | offline | expansion ROI + base loss | faction family | failure feedback required |
| defence_perimeter_share | DefenseCoveragePlanner | perimeter vs interior defence share | 0..1 | Tier 4 | offline | coverage/base survival | map features/faction | current geometry stays authored |
| defence_specialty_mix | DefenseCoveragePlanner | AA/AG/specialty allocation | simplex | Tier 1 cause-of-loss + Tier 4 residual | offline | cause of loss | matchup | derive demand first |
| production_front_setback | BaseFrontBackPlanner | how far behind defence line producers sit | bounded cells | Tier 4 later | offline | producer survival + reinforcement time | map features | geometry safety bounds |
| valuable_rear_bias | BaseFrontBackPlanner | how aggressively valuables go back | bounded weight | Tier 4 later | offline | valuable survival / path cost | global | class assignment itself authored |
| radar_coverage_value_weight | BaseFrontBackPlanner | value of new approach coverage | bounded | Tier 4 later | offline | intel/coverage | map features | provider count/power constraints authored |

### B.I Formation/micro
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
| choke_avoid_weight | formation/zone map | avoid bad chokepoint congestion | bounded | Tier 4 | offline | engagement/path delay | map features | don't override mission destination |

### B.J Missions/operations
| Parameter / family | Owner | Context | Type / safe bounds | Learning route | Update | Reward/evidence | Pooling | Safety / dependency |
|---|---|---|---|---|---|---|---|---|
| mission_bid_value_weight | mission bidding future | value of objective | bounded | Tier 4 V2 | offline | mission success + match | mission type | needs mission cards/outcome sink |
| mission_bid_distance_weight | mission bidding | travel cost | bounded | Tier 4 | offline | mission efficiency | mission type / map features | path distance |
| mission_bid_risk_weight | mission bidding | threat cost | bounded | Tier 4 | offline | survival/success | mission type | fog-honest influence |
| raid_value_threshold | Master/Squad | when raid is worthwhile | bounded value/utility | Tier 4 | offline | raid mission score | personality/family | mission outcome attribution |
| defend_request_threshold | Master/TC | when to allocate defence mission | bounded threat ratio | Tier 4 | offline | base saved/opportunity cost | personality/team | avoid duplicate owner |
| secure_threshold | future Secure missions | when territory objective earns squad | bounded utility | Tier 4 V2 | offline | map control/match | map features | after mission bidding exists |
| staging_reserve_share | ArmyStagingBotModule | home/internal-threat reserve | 0..1 bounded | Tier 4 | offline | base defence vs attack opportunity | personality | scale targets interaction |
| staging_rim_inset | ArmyStagingBotModule | distance inside defence rim | small bounded cells | Tier 4 later | offline | response time / artillery exposure | map features | geometry constraint |

### B.K Air/support
| Parameter / family | Owner | Context | Type / safe bounds | Learning route | Update | Reward/evidence | Pooling | Safety / dependency |
|---|---|---|---|---|---|---|---|---|
| air_engage_ratio | air doctrine | air commit threshold | bounded ratio | Tier 4 | offline | air trade score | air role/family | reuse CP semantics |
| air_aa_priority_weight | air doctrine | prefer killing AA | bounded | Tier 4 | offline | sortie survival/objective | matchup | seen AA only |
| air_ground_safe_threshold | air doctrine | when ground targets are safe enough | bounded | Tier 4 | offline | sortie score | air role | influence threat-air |
| bomber_strike_min_value | air doctrine | minimum target value for bomber package | bounded cost | Tier 4 later | offline | strike ROI | faction family | ammo/reload context |
| support_attractiveness[family] | SupportPowerBotASModule | minimum utility for nuke/strike/disable/heal/etc. | bounded normalized utility | Tier 4 | offline | objective/value swing | power family | not one free scalar per power initially |
| support_climax_hold_bias | SupportPowerBotAS + Director | hold strategic power for synchronized climax | bounded | Tier 4 | offline | climax outcome | power family/personality | expiry/overhold penalty |
| support_consideration_residual[family,feature] | Support power scorer | calibrate authored utility terms | sparse bounded residual | Tier 1 measured | offline | post-use swing | power family | minimum evidence |

### B.L Team coordination
| Parameter / family | Owner | Context | Type / safe bounds | Learning route | Update | Reward/evidence | Pooling | Safety / dependency |
|---|---|---|---|---|---|---|---|---|
| team_sync_window_ticks | Team blackboard/commander | attack arrival synchronization | bounded ticks | Tier 4 later | offline 2v2 | team fight score | team size | human ally not coerced |
| team_sync_force_scale | TC/Director | how much ally timing lowers/raises launch bar | bounded multiplier | Tier 4 | offline | team engagement | team size/personality | combat veto downstream |
| team_defend_answer_threshold | TC | when ally request is worth answering | bounded utility | Tier 4 | offline | ally saved/opportunity cost | team size | distance/CP feasibility |
| team_target_focus_weight | TC/Master | shared-target convergence | bounded | Tier 4 | offline | team objective | team size | avoid overconcentration |
| team_personality_coverage_weight | TC/PlanBandit | prefer complementary allied doctrines | bounded | bandit/tune later | match-start | team result | team comp | public/allied data only |
| team_expansion_claim_penalty | TC/Expansion | avoid same field | large authored penalty or bounded weight | authored/tune | offline | resource conflict | team size | claim correctness more important than fine tuning |

### B.M Opponent model
| Parameter / family | Owner | Context | Type / safe bounds | Learning route | Update | Reward/evidence | Pooling | Safety / dependency |
|---|---|---|---|---|---|---|---|---|
| plan_prior[class] | Opponent model feature | rush/turtle/air/out-earn/etc. prior | probability simplex | empirical Bayes | offline + within-match Bayesian update | classification calibration | enemy faction→family | no human identity |
| plan_feature_likelihood[class,feature] | Opponent model | P(observation\|plan) | probabilities | MLE/Bayesian | offline | held-out likelihood | enemy faction→family | fogged features only |
| plan_transition[class_i,class_j] | optional HMM | strategy transition probabilities | row-simplex | MLE later | offline | sequence likelihood | enemy faction→global | feature only, not direct policy |
| belief_decay/change_rate | Opponent model | adapt to plan transition/nonstationarity | bounded | fit | offline | held-out prediction | global→faction | avoid overreacting one sighting |
| counter_plan_arm | PlanBandit | opening/plan conditioned on plan belief | categorical | contextual bandit V2 | match/episode | reward with propensities | faction/matchup | safety floor |

### B.N Learning-system hyperparameters
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

### B.O Design constants / never self-optimize
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

### C.1 Tier-1 empirical-Bayes residual

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

### C.2 Continuous-reward Thompson sampling

For an arm with sufficient statistics `(n, mean, m2)`:

```text
s² = m2 / (n - 1)
SE = sqrt(s² / n)

sample ~ StudentT(df=n-1, loc=mean, scale=SE)
```

For large `n`, normal approximation is adequate.

Parent scopes contribute capped pseudo-observations, as current PlanBandit already does.

Use **Beta-Bernoulli only for genuinely binary rewards**. Do not convert a rich engagement score into a fake win/loss purely to use a Beta posterior.

### C.3 Conservative safety floor

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

### C.4 SPSA

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

#### Recommended experiment discipline

- same map;
- same factions;
- same spawn pairing;
- same seed family;
- same opponent;
- plus and minus differ only by tested vector;
- minimum current floor = 20 scored matches per arm;
- final validation uses **fresh** seeds/maps and does not reuse adaptive-training games.

The current “weak evidence still moves 0.25×” rule is acceptable as an optimizer heuristic only if the final untouched A/B/league gate is strict. It is **not** itself proof that the new vector is better.

### C.5 Safe bounded in-match control (not online training)

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

### C.6 Contextual bandit — future extension

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

### C.7 Bayesian optimization

Use when:

- ≤ ~10–15 meaningful continuous dimensions;
- match evaluations are especially expensive;
- SPSA gradient estimates remain unstable;
- objective is smooth enough for a surrogate.

Do not make GP-BO the default for all bot parameters.

### C.8 CMA-ES

Use primarily for a tightly coupled micro/formation vector tested in short repeatable combat scenarios.

Never accept a skirmish-optimized CMA-ES vector without full-game validation.

---

## Appendix D — Reward and credit assignment

Pure win rate is too sparse. Pure local efficiency is gameable.

Use a hierarchy.

### D.1 Match reward

Recommended release-level objective:

```text
R_match =
    W_win * outcome
  + W_margin * normalized_value_margin
  + W_speed * bounded_speed_term
  + W_objective * objective_control
```

No raw harvesting bonus.

### D.2 Engagement reward

Current EL decomposition is a strong basis:

```text
R_engagement =
    trade_quality
  + performance_vs_prediction
  + objective_effect
```

Normalize by value at risk where possible.

### D.3 Episode rewards

Use local horizons for decisions whose effect ends early:

- opening: economy/army/objective state at opening end + small match-result regularizer;
- expansion: payback/survival before next expansion;
- support power: target effect + objective swing in a bounded window;
- raid: value damage and disruption minus committed losses;
- defend request: prevented loss minus opportunity cost;
- scouting: information gain and later decision utility minus scout cost.

### D.4 Reward-hacking protections

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


### E.1 `MapFeatureVector/v1`

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

### E.2 `OpponentFeatureVector`

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

### E.3 Paired experiments without persistent map identity

A paired A/B runner may know which scenario it launches, but learning records should not retain a reusable
map name/hash. Use a **batch-local opaque pair/cell token** that means only “these two runs were paired”.
The token must not be stable across batches and must not be resolvable by the runtime learner into a map identity.

This preserves common-random-number/paired-statistics benefits without teaching the bot a named-map table.

### E.4 Manifest

Every emitted learned artifact carries the manifest fields of §17.2:

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

### E.5 Visibility provenance

Every enemy-derived feature should be classifiable as:

- visible now;
- remembered;
- public lobby/map information;
- inferred from fog-honest history;
- offline truth.

`offline truth` must never appear in the live feature vector.

### E.6 Fingerprints

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

### Appendix F.1 Training and final validation are different datasets

Do not tune and declare victory on the same matches.

Recommended split:

- **training/adaptive:** used by fitter/SPSA/bandit;
- **validation:** used during model/group selection;
- **final holdout:** untouched until candidate freeze.

Split by seed and preferably map/faction cells, not random log lines from the same match.

### Appendix F.2 Paired design

For plus/minus or candidate/control:

- mirror factions first;
- swap spawn;
- use paired seeds;
- pair map/opponent cells;
- analyze paired score differences.

This can reduce variance substantially relative to unrelated matches.

### Appendix F.3 Confidence intervals

Report at minimum:

- win/loss and Wilson or beta-binomial interval;
- paired continuous-score mean difference with bootstrap/normal interval;
- engagement score distribution;
- catastrophic-loss/suicide rate;
- per-map/per-faction breakdown.

Do not hide a subgroup collapse behind pooled mean.

### Appendix F.4 Sample sizes

There is no single universal “8 games is enough” number.

Recommended operational levels:

- smoke/inertness: small batches;
- SPSA gradient: current minimum 20 scored matches per arm;
- ordinary behavior A/B: 24–48 per arm depending variance;
- high-impact release decision: 48+ per arm and league coverage;
- small expected win-rate deltas: power analysis may require hundreds or thousands.

Treat 16-match historical gates as engineering screens, not precise balance estimates.

### Appendix F.5 Multiple comparisons

Current Holm handling is appropriate for families of simultaneous hypotheses.

If experiments are repeatedly peeked/sequentially stopped, use:

- predeclared stopping rules;
- alpha spending/sequential Holm, or
- confidence sequences.

Do not run until a lucky p-value appears.

### Appendix F.6 League gate

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

### G.1 Synced simulation

Must remain deterministic. Learned bot logic affects it **only through orders** (`IBot.QueueOrder`, DESIGN §19.8); synced state such as the personality changes only through an order round trip (`SetBotPersonality`), never by granting a condition from bot code.

### G.2 Host-local bot reasoning

May use unsynced local state according to the current architecture, but:

- no hidden enemy information;
- no live external network;
- no silent rules mutation;
- no per-machine persistent release learning.

### G.3 Learned files

Release files are:

- committed;
- versioned;
- reviewed;
- reproducible;
- read at/before match setup as designed;
- immutable during the match.

### G.4 Match-local adaptation

Allowed only if:

- it uses fog-honest current-match evidence;
- it is bounded;
- it resets;
- its law is versioned;
- its gains are trained offline;
- save/load behavior is tested if relevant.

### G.5 Opponent privacy/fairness

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

### Strong / primary academic evidence

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

### Strong current Cameo sources

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
- `tools/ai/learnables.yaml` (the registry) and `tools/tests/test_learnables_registry.py`

### Useful community/engineering evidence

- Steamhammer/McRave opening/opponent-model descriptions;
- BWEM terrain analysis;
- M28AI development notes;
- Stockfish/Fishtest SPSA engineering practice;
- OpenRA and related open-source bot implementations.

These are useful engineering analogies, but they should not override primary literature or current Cameo binding design.

### Low-confidence material deliberately not used as a core design basis

The parallel report contained several claims that were either difficult to trace to a strong primary source or unnecessary for the design:

- generic “Company of Heroes logistic-regression AI mods” claims;
- generic “Age of Empires villager-allocation parametric research” without a concrete primary citation;
- GPU/iGPU-offload recommendations for the current small-model problem;
- broad NEAT/meta-learning proposals as if they were immediate Cameo requirements;
- exact claims that a clean retrain beats continual training by a universal percentage.

They may remain research leads, but they do not belong in the critical-path implementation plan without stronger evidence.
