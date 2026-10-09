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

2026-10-09 consumer-fit delta: canonical shared Logs/cameo-ai-economy-health.jsonl and separate cameo-ai-economy-raw.jsonl now match analyzer input discovery; FileMode.CreateNew refuses existing evidence. World owns both streams, closing after all player terminals or on actor disposal without fabricated completion. Runtime and tests share schema serializer. Focused tests 20/20; actual C#-generated JSONL consumed by approved analyzer 2a417: normal/refundable net spend OBSERVED_HEALTHY, ready-at-250 BLOCK, incomplete terminal UNKNOWN. Synthetic consumer fixtures do not prove runtime coverage. Terminal causality remains deliberately incomplete pending accepted seam.

2026-10-09 timing/cost preparation: producer_live now means actor alive/not disposed, independently of ProductionQueue.Enabled. Disabled unfinished/empty queues are paused; Done heads remain ready, so disabled production does not hide a completed building. Re-enable empty queues start a fresh idle interval. Focused 27/27 PASS; explicit isolated maximum-census test 1/1 PASS (45001 ticks, 128 queues, 901 pulses, MemoryStream). Measured 1907.0625ms elapsed, 150460520 allocated bytes and 19021579 output bytes. These include test assertions/core/serializer and exclude World enumeration, seam, callbacks and disk; NOT runtime-cost approval. Two such players exceed current 32MiB shared file budget: forecast bounds before adoption, adapt bounded budget or declare unsupported rather than silently drop.

Proposed separate Coordinator cost gate after accepted seam/restack: frozen engine/tree/config, serial matched logger-unmounted versus mounted fixtures, representative and supported maximum players/queues; at least 5 paired samples. Record tick CPU distribution, total/peak allocation, working set, per-stream bytes/records, write/flush cost, observer history bounds, outcome/order parity and terminal coverage. Compare to a predeclared acceptable overhead budget with Lead/VP; no universal threshold or runtime clearance is inferred from the isolated core receipt. Overflow/error/truncation must yield UNKNOWN. Every supported campaign duration/roster must fit the analyzer input limits (128MiB/200000 rows) and logger bounds including raw events.


2026-10-09 independent checkpoint acceptance: VP BOUNDED APPROVE at exact code
322a69d190503389d4ed421be04f715300767bfc (parent655218af3bfe85c8fb6fea8f34899ae1aafbb3f9).
Scope: actor producer_live independent of queue Enabled, paused/Done-ready
semantics and explicit isolated cost fixture ONLY. Receipt:
C:/Users/AedisToru/Documents/GitHub/Cameo-mod-fleet/REREVIEW_2026-10-09_logger_liveness_cost_322a_luna.md.
Reviewer inspected committed delta/remote/diff-check; tests were not independently
rerun. Sol-reported focused27/27 and explicit1/1. No adoption/runtime/campaign
clearance; 01a121b6 terminal-causality remains FIX REQUIRED. Accepted seam restack,
outcome consumer integration, supported-roster/output budgeting, paired runtime
cost and reservation gates remain. Canonical files must both be preserved.
