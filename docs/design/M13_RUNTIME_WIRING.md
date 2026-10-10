# M13 runtime wiring checkpoints

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
