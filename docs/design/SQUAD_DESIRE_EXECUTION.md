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
