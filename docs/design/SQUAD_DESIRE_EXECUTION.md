# Squad desire execution (LEARN-P6 fix)

`BP_squad_desire` remains default off. With `UseSquadDesire: false`, or no enabled `IBotSquadDesire`, `SquadCA.Update` calls the existing FSM. No desire controller, enemy scan, ratio, RNG draw, or order is evaluated on that path. Coordinator replay identity against pre-P6 remains an acceptance gate.

When armed, every declared squad type runs the same desire controller on every normal manager squad update (`AttackForceInterval`, subject to the existing attention budget). Active attacks therefore continue sampling. This includes Air, Naval, Artillery, Support, FireSupport, Fighter, Gunship, Bomber and Stealth as well as Rush, Guerrilla, Harass and Protection. The provider owns urgency, leaky history, personality bias, hysteresis and dwell. Repeated same-tick consultation advances history once; it has no actor mutation or order issuance.

| Stance | Execution |
|---|---|
| Attack | AttackMove toward the nearest currently visible enemy |
| Defend | AttackMove back to the initial own-base center, fighting en route; losing predictions use Move |
| Retreat | Move home without deliberately initiating combat |
| Regroup | Move to the mean member cell latched on entry |
| Harass | AttackMove toward the highest-value visible non-combat economic target, with distance and ActorID tie breaks |
| Reinforce | Move to an existing nearest Rush wave; if none exists, wait at home |

No visible attack/raid target means assembly rather than an omniscient target. Reinforce is a rally action; the separate BR reinforcement-board increment owns production demand, expiry and builder fulfilment. The armed controller replaces each role FSM as its order owner. Legacy role-specific formations, staging, aircraft rearm/repair and support attachment policies remain on the switch-off path; they are not separate parallel order owners while armed. Armed gameplay/order-rate validation is required before enabling.

`IntegerCombatPredictor` reads raw integer weapon damage, burst, reload cycle, Versus and target masks from immutable rules. It weights damage by opposing HP share and computes the square-law ratio in thousandths, using integer millionths of damage/tick and BigInteger aggregates. It observes current HP only on own or visible enemy actors. It does not initialize or convert the legacy double profile/predictor. Original predictor APIs remain unchanged for switch-off and other systems.

Commit permission requires ratio >= 1000 AND the configured retreat ratio times engage margin. This safety constraint applies immediately even if hysteresis/dwell retains Attack. It checks both the destination's visible combat force and nearby visible danger; no personality can override it. Defensive travel falls back to Move when the same gate rejects combat. Arrival and in-flight identical destination orders are suppressed; regroup's mean is latched so movement cannot continually relocate its rally point.

Fog ratchet: one added `World.Actors` site in the manager, cached per world tick, filters `CanBeViewedByPlayer` before enemy classification/profile access. ActorID sorts break ties; these are runtime actor references, never player keys. Two new canary sites pin visible targeting and integer ratio consumption. No per-player fields, CA-suffixed new types, actor activities or SharedRandom calls are introduced.

Regression suite: `SquadDesireRegressionTest` covers retained unsafe Attack, all six routes including the Harass consumer, integer threshold/order invariance with overflow-sized forces, and continuing cadence with same-tick idempotence. Existing desire tests retain the stance-flap/hysteresis/dwell/bias coverage.

## Maintainer override and consumer regressions (2026-10-10)

The all-type maintainer override supersedes the four-type acceptance at 0249f6fc. All 13 declared SquadCAType values use the same learned-desire controller while armed with an enabled provider. Invalid enum values, switch-off and missing-provider paths retain the existing FSM selection. The default-off switch and off-path canary/FSM calls are unchanged.

The internal execution port supplies observations and the order sink. Its production factory still returns LiveSquadDesireExecution, preserving visible-enemy selection, provider signals, integer prediction, destination and nearby-danger guards, and IBot.QueueOrder. Offline regressions replace only that factory's observation/sink adapter: actual SquadCA.Update, the actual manager provider getter/cache and the shared production controller run for every type. Each type pins retained unsafe Attack becoming Move home, all six stance routes with in-flight deduplication, no parallel FSM while armed, and unchanged installed-FSM delegation when off or missing a provider. Existing pure controller and memory regressions remain.

These tests establish consumer wiring and switch-off delegation, not loaded-world adapter behavior or replay identity. Specialized armed logistics, target reachability and armed order churn still require runtime review before enabling. Coordinator-owned switch-off parity remains open. The internal friend-assembly declaration exposes the port only to the test assembly. No engine, pin, default activation, RNG or sync-field changes accompany this extension.
