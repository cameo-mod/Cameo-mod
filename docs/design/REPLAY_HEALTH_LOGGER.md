# Runtime economy health logging plan

2026-10-09. Codex Sol; branch codex/replay-health-logger, base f7e1d0fff.
Consumer contract: economy-invariants-v2/schema2 at approved 2a417dc3e.
Lead authorized isolated implementation/build/unit tests before seam acceptance;
adoption still requires VP acceptance, integration receipt and measured cost.

Write scope: new AiEconomyHealthState/Recorder/LogWriter files in
OpenRA.Mods.Cameo/Traits, their new tests, and this task's documentation.
No queue-manager/eval/base-builder source, engine, production YAML mounting,
orders, sync state, policy, RNG, launches or shared refs.

The world writer initializes at WorldLoaded and captures world tick0 explicitly
(World.Tick increments before ITick). Player recorders observe only own queue
heads/resources each tick, maintaining ready/idle observation timestamps; emit
complete queue census every50 ticks plus terminal covered pulse/end. Building
groups derive from the resolved BaseBuilderInfo.BuildingQueues, not faction
names; defense-only groups remain diagnostics. Unsupported/ambiguous ownership,
profile, queue identity, missing tick0, truncation or drop => UNKNOWN.

Schema2 health JSONL contains pulse/cancel/end only. Preserve signed net_spent;
gross_spent=null/gross_spend_complete=false. Cash/resources/capacity are public
own-player values. Separate bounded raw transition/delivery stream preserves
intent and observation; no seam Placed proves actor placement. Actual accepted
resource callbacks record post-cap value; positive value proves accepted credit,
zero does not, and harvester identity is unknown. Passive income/refunds never
substitute for delivery proof.

Consumer-fit blocker sent to VP/Architect: f7e1 still correlates pending requests
with removals within8 ticks rather than proving terminal cause. Do not emit these
as authoritative production cancellations or certify event completeness. Keep
intent/removal evidence raw and mark incomplete until the accepted correction.
Restack on corrected seam head before adoption; no duplicate full seam review.

Bound state to live queues/items, reusable census buffers and explicit numeric
output/record budgets. Drop/error state is monotonic and visible in terminal
records; absence of terminal coverage is UNKNOWN. File failures never change
gameplay. No pathfinding/map-cell/dock-access scan in the per-tick or50tick loop.
Expensive access diagnostics remain a separate750tick/cache phase.

Tests: tick0/50 cadence, per-instance ready reset, idle age, paused/dead queue,
queue disappearance/pruning, event-local destruction/elimination/unknown,
signed refundable spend, accepted value0/positive, budget/truncation/error,
schema2 serialization and approved analyzer round trip. Runtime all32-faction
profiles, actual placement/client compatibility and CPU/allocation/output cost
are separate serial Coordinator gates, not inferred from unit tests.

Implementation checkpoint: own-player census/head identity, positive accepted
credit callbacks, schema2 pulses and separate raw evidence compile on f7e1.
Tick0 activation and <=50tick pulses are wired in an unmounted world controller.
State is deliberately incomplete from activation until the terminal-outcome seam
is accepted/restacked: this checkpoint cannot produce a healthy campaign verdict.
The bounded writer uses new files only, 64KiB lines, 32MiB/file and 200000 records;
write/flush/encoding/budget failure is sticky. 16 focused core/writer tests pass.
Remaining: accepted seam adaptation, recorder/schema round-trip integration tests,
file-disposal coverage, all-faction profile validation, measured runtime cost and
independent review. No production YAML mounting or runtime launch occurred.

Full mod suite at this checkpoint: 1301/1301 PASS.
