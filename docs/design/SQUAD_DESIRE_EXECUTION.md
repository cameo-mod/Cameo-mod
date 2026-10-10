# Squad desire execution (LEARN-P6 fix)

`BP_squad_desire` remains default off. With `UseSquadDesire: false`, or no enabled `IBotSquadDesire`, `SquadCA.Update` calls the existing FSM. No desire controller, enemy scan, ratio, RNG draw, or order is evaluated on that path. Coordinator replay identity against pre-P6 remains an acceptance gate.

When armed, Rush, Guerrilla, Harass and Protection squads run the desire controller on every normal manager squad update (`AttackForceInterval`, subject to the existing attention budget). Active attacks therefore continue sampling. Air/naval/artillery/support specializations keep their role FSMs. The provider owns urgency, leaky history, personality bias, hysteresis and dwell. Repeated same-tick consultation advances history once; it has no actor mutation or order issuance.

| Stance | Execution |
|---|---|
| Attack | AttackMove toward the nearest currently visible enemy |
| Defend | AttackMove back to the initial own-base center, fighting en route; losing predictions use Move |
| Retreat | Move home without deliberately initiating combat |
| Regroup | Move to the mean member cell latched on entry |
| Harass | AttackMove toward the highest-value visible non-combat economic target, with distance and ActorID tie breaks |
| Reinforce | Move to an existing nearest Rush wave; if none exists, wait at home |

No visible attack/raid target means assembly rather than an omniscient target. Reinforce is a rally action; the separate BR reinforcement-board increment owns production demand, expiry and builder fulfilment. The armed controller replaces the listed ground role FSMs; legacy formations/staging/lure microstate policies are retained on the switch-off path, rather than being run concurrently as a second order owner. Armed gameplay/order-rate validation is required before enabling.

`IntegerCombatPredictor` reads raw integer weapon damage, burst, reload cycle, Versus and target masks from immutable rules. It weights damage by opposing HP share and computes the square-law ratio in thousandths, using integer millionths of damage/tick and BigInteger aggregates. It observes current HP only on own or visible enemy actors. It does not initialize or convert the legacy double profile/predictor. Original predictor APIs remain unchanged for switch-off and other systems.

Commit permission requires ratio >= 1000 AND the configured retreat ratio times engage margin. This safety constraint applies immediately even if hysteresis/dwell retains Attack. It checks both the destination's visible combat force and nearby visible danger; no personality can override it. Defensive travel falls back to Move when the same gate rejects combat. Arrival and in-flight identical destination orders are suppressed; regroup's mean is latched so movement cannot continually relocate its rally point.

Fog ratchet: one added `World.Actors` site in the manager, cached per world tick, filters `CanBeViewedByPlayer` before enemy classification/profile access. ActorID sorts break ties; these are runtime actor references, never player keys. Two new canary sites pin visible targeting and integer ratio consumption. No per-player fields, CA-suffixed new types, actor activities or SharedRandom calls are introduced.

Regression suite: `SquadDesireRegressionTest` covers retained unsafe Attack, all six routes including the Harass consumer, integer threshold/order invariance with overflow-sized forces, and continuing cadence with same-tick idempotence. Existing desire tests retain the stance-flap/hysteresis/dwell/bias coverage.

## Accepted execution scope and consumer regressions (2026-10-10)

The lead accepted Rush/Guerrilla/Harass/Protection-only execution. Every other enum type, including Air, Naval, Artillery, Support, FireSupport, Fighter, Gunship, Bomber and Stealth, keeps its existing FSM whether BP is armed or off. The production Update selection and off-path canary/FSM calls are unchanged.

The controller now uses an internal execution port. LiveSquadDesireExecution retains the same manager observations, target selection, provider call, destination and nearby-danger guards, and IBot.QueueOrder sink. The production generic controller owns the existing regroup latch, stance-to-order routing, member order memory and emission loop; it is not a replacement test policy. Its regressions run that same controller with a controlled observation/order port and real SquadDesireMemory provider, pinning unsafe retained Attack, in-flight order replacement/dedup, continuing consultation, departed-member pruning and empty rosters. A separate regression calls actual SquadCA.Update with an installed recording FSM for all specialized types armed/off and every type off, without a world: an accidental desire/provider lookup fails instead of silently using a test selector.

These are offline consumer-contract tests. They do not create a live world, rerun the observation adapter against actors, establish match-level switch-off order identity, or measure armed churn. Those Coordinator-owned runtime gates remain open. The internal friend-assembly declaration exposes these production internals only to the test assembly.
