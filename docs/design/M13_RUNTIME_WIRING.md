# M13 runtime wiring checkpoints

Current gameplay checkpoint: see **Absolute dispatch and response admission** below.
Earlier sections describe frozen historical checkpoints; their unwired limitations
are superseded only where the current section explicitly states implementation.

Base: accepted claim-provider tip `59e80a95a6142c001360fb587682cfdab515dc4a`.
The default-off increment is `BV_m13_liveness`, classified as one restraint; the
applier skips the classic squad-manager instance. No YAML enables it by default.

The first wiring checkpoint adds explicit BotLimits values for all ten tiers:

| Tier | No-launch ticks | Response expiry | Cooldown | Capacity |
|---|---:|---:|---:|---:|
| easiest | 9500 | 1900 | 950 | 1 |
| veryeasy | 9000 | 1800 | 900 | 1 |
| easy | 8500 | 1700 | 850 | 1 |
| medium | 8000 | 1600 | 800 | 1 |
| hard | 7500 | 1500 | 750 | 1 |
| veryhard | 7000 | 1400 | 700 | 1 |
| brutal | 6500 | 1300 | 650 | 1 |
| challenger | 6000 | 1200 | 600 | 1 |
| unbeatable | 5500 | 1100 | 550 | 1 |
| cameogod (`BotLimits@god`) | 5000 | 1000 | 500 | 1 |

The existing CreateAttackForce scale-target branch uses the configured absolute
MaxIdleUnits only when M13 is armed; switch-off retains the old integer formula.
Tests pin the N-1/N boundary under several scale targets and the legacy formula.
This closes only the scale-dependent count-valve defect. It does not yet bypass
composition/mission/combat holds or prove a dispatch at that count.

Remaining: bot-wide response membership and release, continuous liveness service,
authoritative observed-wave evidence, save/restore, separate frozen combat stream
and diagnostic consumer. Expansion assist without an episode stays unsupported.
No full M13, parity, cost, adoption or paired 3v3 claim follows from this checkpoint.

Maintainer extension (2026-10-10): planned noob is one named step below easiest,
with no-launch10000 / expiry2000 / cooldown1000 / capacity1. No noob tier exists
or is added in this checkpoint. The test keys each existing tier by its explicit
step from hard; adding noob later cannot re-index or shift the existing values.

## Observed-activity reset (maintainer ruling, 2026-10-10)

NO engine change. The manager assigns monotonic local wave identities at offensive
squad admission while the default-off switch is armed. Admission and order intent
are not activity evidence. Before the ordinary squad update, it samples own,
orderable wave members whose current target is a currently visible legal enemy.
Foreign-held members and explicit flee states are excluded. Public CurrentActivity,
active child state and GetTargets/attack target lines supply the evidence.

Active attack must target that same legal enemy. AttackMove or attack-parent move
requires the same activity instance across two distinct samples, actual own-position
change and decreasing distance to the currently visible target's same position.
Generic Move, queued/canceling/done activity, ReturnToBase, idle, unknown targets,
retreat and unrelated goals cannot reset. Child depth and target enumeration are
bounded at 16/256; absent public evidence stays unavailable. No hidden enemy census
or pathfinder is added. Each actor's sample memory is pruned with the live wave
membership; dead/dismissed wave records are pruned. A wave can reset once only;
older waves observed after a newer wave are conservatively ignored.

The pure state names this ObservedOffensiveActivity with LastObservedOffensiveTick
and ObservedOffensiveCount. It needs no dispatch intent and never certifies an order
resolution. The future combat stream must retain dispatch_causality UNKNOWN and
observed-only labels; it is not implemented or mounted by this checkpoint.

Eligibility sampling currently uses the fixed configured base value and existing
squad targets only. Unsupported floor/delay stays Unsupported. Wave IDs, activity
memory and interval state are not persisted yet: restored squads have no observation
episode and cannot claim fresh-wave proof. Full first-wave target coverage, complete
save/restore, soft-restraint release/deadline dispatch, team response membership,
combat stream/consumer and paired 3v3 remain separate pending implementation. This
checkpoint supplies the conservative reset producer, not the complete guarantee.

## Accepted-progress diagnostic receipt

The default-off observation path now emits `M13_PROGRESS` only when the existing
activity evaluator accepts a new, monotonically increasing offensive wave. The
marker uses `Player.InternalName` (not the shared host client index), and records
the tick, manager-local wave id, qualifying member count, accepted observation
count, and the original eligibility start/deadline before clearing the interval.
`dispatch_causality=UNKNOWN` is literal: this marker is neither order-resolution
proof nor a launch/health approval. Rejected evidence, duplicate/older waves and
registration alone produce no marker. `BotDebug` must be enabled to retain the
marker in diagnostic logs. Wave ids remain manager-local and must be paired with
manager identity before aggregation across personality instances; the diagnostic
must not be used as a player-wide deduplication key.

Regression coverage exercises the same production acceptance overload used by
the manager: receipt provenance before reset, all hard evidence refusals, and
repeated/older waves after a new interval begins. Release build and combined
progress/activity/liveness tests: 40/40, zero skipped. No boot/game or full suite
was run for this observational delta. All gameplay and stream gates listed above
remain open; the absolute idle valve still needs downstream soft-hold bypass.

## Absolute dispatch and response admission (2026-10-11)

This source checkpoint remains behind `AttackLivenessEnabled`, enabled only by
`BV_m13_liveness`; no default, tier value, engine pin, campaign arm or YAML changed.
The first armed squad-manager trait on each player retains a shared liveness clock,
response budget and monotonic wave allocator even while its personality is disabled.
No static, team-wide or random state is introduced. Accepted progress now uses the
shared per-player wave sequence; the earlier manager-local identity description is
historical. Seat and manager diagnostic fields remain explicit.

The absolute threshold is checked against a stable ActorID assault draft before the
ordinary attack-delay timer. Its independent admission route bypasses the normal
value/count scaling, composition, director, mission/Defend and pre-launch combat
holds. It excludes artillery, fire support, aircraft/naval units, harvesters,
buildings, cargo carriers, unorderable actors and foreign leases. Current visibility,
weapon capability and path checks remain hard. It forms a nonempty Rush squad and
queues AttackMove directly, entering the ordinary marching FSM; live combat/flee
safety is retained. It removes only drafted actors from the idle pool. The off path
retains the old registration point and normal launch logic.

Liveness eligibility includes unobserved offensive registrations and held response
members, so intent or a soft role diversion does not reset the interval. Newly
unmanaged owned assault actors cover a personality handoff. A visible legal target
is selected independently of an existing squad, with deterministic distance/ActorID
ordering. Frozen-only target evidence stays Unsupported and retains an established
deadline. Deadline dispatch uses a lead of max(150, 2*AttackForceInterval+1); this is a
scheduler allowance, not a proved engine resolution bound. Failed/timed-out intents
retain the original deadline and supply no dispatch causality. Actual enemy damage
inside the base grants a finite ProtectInterval review window, refreshed only by
new damage; prediction, personality and request pulses cannot refresh that exception.
Only the reviewed actual offensive observation resets the interval.

Own protection requests and allied defend responses require a known owner-produced
claim episode, active/fresh requester, locally observed contest, and capacity exactly
one. Unsupported expansion-assist provenance is refused. Admission takes a stable
assault prefix only while the residual pool retains both normal launch bars and
required composition roles. Missing role evidence refuses the optional response.
The shared budget fixes expiry at first admission; refreshes can change the destination
and evidence, never membership or expiry. Every tick evaluates expiry, freshness,
requester activity, contest and reserve loss. Stand-down cancels owned guard orders,
removes actual protection membership, releases its unit leases, returns orderable
nonforeign actors to the idle pool, notifies pool observers, and installs the
claim-episode cooldown. Personality disable releases membership with that same
cooldown. Own emergency defense remains separate; forward non-emergency drafts also
preserve the normal assault reserve. Diagnostics emit admission, release, intent and
accepted progress separately, with dispatch causality UNKNOWN.

Save restoration deliberately invalidates shared evidence: no saved interval,
wave/member mapping or response-slot adapter is implemented yet. It refuses urgent
admission and optional response admission rather than recycling history. This is
fail-closed compatibility, not completed M13 persistence. Other remaining gates:
actual loaded-world manager/order-sink and reclaim regressions, save/restore adapter,
finite failure recovery for busy/stuck offensive members, semantic combat emitter
and consumer, first-wave/runtime cost proof, switch-off replay parity, and paired3v3.
No deadline/launch guarantee, adoption or campaign activation is approved here.
The turtle fallback experiment remains a separate subsequent increment.

### Clock restore transport checkpoint (2026-10-11)

`AttackLivenessEvalCA.Capture/TryRestore` preserves the original interval, floor,
dispatch lead, pending/issued wave identity, accepted-wave history and soft-restraint
receipt. Restoration is one-shot into a fresh clock. Invalid versions, future ticks,
inconsistent interval/history, unknown enums/flags and missing snapshots refuse
restoration permanently; fresh observations cannot erase that refusal. A saved
Unsupported phase retains the original deadline and still requires complete evidence
to resume. An overdue pending intent stays pending until explicitly failed; restore
does not certify dispatch or renew a deadline. Rejected activity observations may
advance the clock tick without recomputing phase, so validation accepts that genuine
state and the next eligibility observation recomputes urgency.

This helper is transport only and has no manager caller. The live save hook still
invalidates evidence. Player identity/global wave sequence, actual squad/member mapping,
response ownership and membership, and the combined manager save adapter remain open.
The existing response-budget snapshot is unchanged. This checkpoint does not complete
M13 persistence or authorize runtime/adoption or a wholesale lineage merge.

Validation of the clock transport delta: 18 new restore cases; combined focused
restore/liveness/progress 45 passed; full Release C# suite 1640 passed. Two existing
explicit maximum-census fixtures were not selected. No boot/game or Python directory
suite ran. The frozen gameplay parent remains unchanged for its separate review.

Validation: Release build passed. Focused M13 launch/player-state, budget, claim and
activity cases: 118 passed. Final full Release C# suite: 1622 passed; two existing
explicit maximum-census fixtures were not selected by the default run. No whole
Python test directory, boot or game was run. New tests exercise the production urgent
commit boundary at N-1/N under every soft-hold flag, hard refusals, fixed-floor
lead/deadline behavior, intent failure without renewal, independent player state,
and unsupported restoration/danger expiry. These are controller tests, not a
loaded-world adapter run. Source needs its own exact-SHA delta review and a reviewed
port from this older isolated lineage before any landing.

## Reviewed-delta port onto master e49acf41 (2026-10-11)

This branch ports only the provider/tier/runtime/budget/progress/gameplay source,
tests and design documents through reviewed checkpoint3693a4a93 onto master
e49acf41a1b3699f01d44e4def06ed4bf83af038. The three-way application was clean;
master's unrelated source remains. Historical DEVELOPMENT_LOG/HANDOFF replacements
were excluded, as was the later clock-restore transport0c77867f6 awaiting review.
The inherited master engine pin4f69df005f is unchanged and the isolated build input
matches it. No branch changes default switch activation or imports c33de2ef1.

The base registry omitted the effect class for its existing BP_squad_desire group;
classifying that stance hold as restraint repairs the fail-closed catalogue without
arming it. Focused M13 C#139 passed, full Release1790 passed, focused Python tier/
registry19 passed. Two existing explicit max-census fixtures were not selected.
No boot/game or full tools/tests run occurred. Exact port review and integrator
landing remain required; runtime/parity/3v3/adoption remain open. The separate
turtle-fallback branch also carries the BP classification; when combining switches,
retain it once and update the catalogue count for the combined group set.

## Manager save/restore adapter (2026-10-12)

The unconditional evidence invalidation at load is replaced by a validated adapter.
All M13 bookkeeping that was runtime-only (squad-wave dictionary, pending-intent
dictionary, observation flag, response membership) now lives on the squad record:
`M13WaveId`, `M13ObservedProgress`, `M13IntentTick`/`M13IntentWave` (the intent's
wave snapshot survives squad removal and dead-wave cleanup), `M13Response` and a
`M13Corrupt` parse marker. The intent subject keeps a manager reference
(`m13IntentSquad`) so a detached squad still resolves its pending wave.

`IssueTraitData` adds, per armed manager, the response binding (channel, rally,
value, expiry, and the full claim-episode identity) when it holds a live flagged
protection squad; the first armed manager (`M13StateOwner`) also emits `M13Player`
with seat identity, wave counter, base-danger window, the complete liveness clock
transport and the complete response-budget transport including sorted cooldowns.
Nothing emits while `EvidenceComplete` is false, so a dead state cannot resurrect.

`ResolveTraitData` rebuilds per-manager bindings from squad records, re-reads the
response binding on the claimant, and lets the owner replay `M13Player` through
`M13PlayerStateCA.TryRestore` — a one-shot composition of both component
transports plus counter/identity/window checks. Missing, malformed or rejected
records invalidate exactly like the old code (evidence UNKNOWN). `RestoreSeen`
distinguishes a load with no record (legacy save or disabled owner) from a fresh
game; the first armed tick checks it before any liveness use.

Cross-manager membership validates once at the first armed tick via the pure
`M13RestoreMembershipCA.Evaluate`: restored waves must not exceed the counter,
at most one intent flag may exist and must equal the pending wave, observed flags
require a live wave, at most one protection flag/binding may exist, and an active
budget claim requires exactly one valid flagged squad whose owning manager
reproduces `ActiveKey` via `M13ResponseClaimCA.TryKey` with `Owner` identity.
A pending wave whose intent squad did not survive resolves once as a dispatch
failure (no renewal, no invented causality). Any other inconsistency invalidates
the shared evidence and clears restored M13 state on every armed manager.

Regression coverage (`M13RestoreAdapterTest`): player record round-trip of
identity/counter/budget/clock, missing/corrupt/half-restored records failing
closed, dead-pending-intent resolution, consistent membership acceptance, and ten
inconsistency classes. No loaded-world adapter run, save/load cycle, boot gate,
parity or paired 3v3 is claimed by this checkpoint; the manager-level save/load
path and the switch-off replay parity gate remain open.
