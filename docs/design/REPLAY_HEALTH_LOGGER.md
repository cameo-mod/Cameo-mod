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

2026-10-09 consumer-fit delta: canonical shared Logs/cameo-ai-economy-health.jsonl and separate cameo-ai-economy-raw.jsonl now match economy_invariants.py input discovery; FileMode.CreateNew refuses existing evidence. World owns both streams, closing after all player terminals or on actor disposal without fabricated completion. Runtime and tests share schema serializer. Focused tests 20/20; actual C#-generated JSONL consumed by approved analyzer 2a417: normal/refundable net spend OBSERVED_HEALTHY, ready-at-250 BLOCK, incomplete terminal UNKNOWN. Synthetic consumer fixtures do not prove runtime coverage. Terminal causality remains deliberately incomplete pending accepted seam.

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


2026-10-09 output-budget correction: shared file cap is now 128MiB, matching
approved replay_health.read_jsonl MAX_FILE_BYTES (128MiB); line cap64KiB and
record cap200000 unchanged. This supersedes the earlier32MiB implementation.
Explicit one/two-player fixtures each keep128 queues through45001 ticks; both
PASS. Two-player receipt:1802 pulses,38046762 output bytes,1144.5332ms,
300133944 allocated bytes (process/JIT warm-up differs; timing is NOT a comparative
runtime benchmark). Raw stream budgets are independent. Arbitrary64-player
maximum queue/event workloads are not certified; forecasting and overflowUNKNOWN
remain required. No mount/seam/campaign clearance. Receipt:
engine/bin/TestResults/economy-health-core-cost-2p.json.


2026-10-09 per-stream sizing clarification: two-player fixture writes health only:
38046762 bytes/1802 rows; maximum actual UTF8 record including newline21114 bytes
(<64KiB writer cap and <1MiB approved reader line cap). Raw events and summary
are NOT measured by this fixture; combined fixture bytes38046762, combined
campaign bytes UNKNOWN/null. Configured health128MiB + raw128MiB + optional
external summary reader1MiB gives257MiB capacity upper bound for those three
artifacts only; does not estimate their usage or include pre-existing logs/replay.
Actual raw workload and summary size remain adoption sizing gates. New frozen
sizing receipt:engine/bin/TestResults/economy-health-core-cost-2p-line-sizing.json.
Explicit2/2 PASS; no runtime/seam/adoption clearance.


2026-10-09 consumer clarification and actual-path CLI proof: replay_health.py
@83f14ab19 is the startup checker; it reads situations/placements/matches only
and DOES NOT consume this logger file. The separate approved economy consumer
is tools/ai/economy_invariants.py@2a417dc3e20cc408bec6e1818cf5734b0431a516;
its CLI explicitly reads support/Logs/cameo-ai-economy-health.jsonl. Earlier
wording about generic pinned-analyzer discovery was ambiguous and must not be
read as startup-checker integration approval.

C# AiEconomyHealthSchemaTest now uses the same bounded writer as runtime to
create unique support roots and actual canonical health files. Python CLI tests
extract immutable economy_invariants.py and its replay_health.py dependency
from exact approved2a417 Git blobs into an isolated temporary directory; execute
CLI on those generated support paths, verify exit/status and consumed byte SHA,
and verify input bytes remain unchanged. C#4/4 and CLI4/4 PASS: healthy0,
ready250 BLOCK20, incomplete UNKNOWN21, raw-path-only UNKNOWN21. No adapter,
checker mutation, live game or driver execution. Run:
dotnet test OpenRA.Mods.Cameo.Test/OpenRA.Mods.Cameo.Test.csproj -c Release --filter FullyQualifiedName~AiEconomyHealthSchemaTest
python -m unittest tools.tests.test_economy_logger_cli -v

These tests establish canonical path/schema consumer-fit for economy_invariants,
not runtime event completeness. Coordinator still must explicitly invoke BOTH
startup and economy checkers under separately accepted pins/capture fingerprints;
existing startup-only driver cannot detect the new queue/cash invariants.
Seam/outcome integration, runtime cost and campaign gates remain unresolved.

2026-10-10 inert restack checkpoint on integrated master
46a89fdcbf1f30a184ba821557d21749c325a0ab, containing independently accepted
terminal seam1e89fd91d4667d55c024d2cfe92550a3dfd7d0ee. Engine pin remains
6da7fce14da541180c6baddd6925118fbef65b94; frozen logger3741 is unchanged.

The recorder retains every queue transition in raw evidence. PlacementOrdered,
CancelOrdered and ordinary Queued/Ready/Held/Resumed observations cannot establish
an outcome. Every Removed event marks coverage permanently incomplete. Only
accepted Cancelled terminals with nonzero producer/episode, known event-local
producer liveness and supported cancellation class can emit schema2 cancel.
Proven Placed remains raw evidence: the pinned health consumer has no placement
record kind, and the census separately observes the resulting queue state.

Canonical cancellation queue IDs resolve the unique own configured building
queue trait as producer:queue-type:group. Missing or ambiguous resolution is
UNKNOWN. This performs an additional bounded own-queue lookup per cancellation
(max128 own configured queues); no pathfinding/cell/enemy/policy/order work.
Actual lookup and callback runtime cost is unmeasured. Item IDs use
seam:category:episode, paired with resolved queue ID. Distinct same-name requeue
episodes differ; these IDs are independent of pulse census IDs. The pinned
consumer deduplicates cancel queue/item pairs without joining them to census IDs.

Changed-boundary tests generate complete SYNTHETIC fixtures with the runtime
schema serializer and bounded canonical writer. Pinned economy consumer2a417
returns BLOCK20 for three active production cancels; inactive/elimination and
destruction cleanup do not produce that defect; duplicate episode and incomplete
Removed evidence return UNKNOWN21. These fixtures demonstrate consumer fit only.
Run the entire AiEconomy filter before the CLI suite to generate all fixtures:

dotnet test OpenRA.Mods.Cameo.Test/OpenRA.Mods.Cameo.Test.csproj -c Release --filter FullyQualifiedName~AiEconomy
python -m unittest tools.tests.test_economy_logger_cli -v

Activation's sticky incomplete guard is deliberately retained. No YAML mount,
activation change, checker change or engine/source pin change is included. Actual
tick0/callback/pulse/end ordering, capability/census/terminal coverage, mounted
parity and CPU/allocation/disk cost, Coordinator dual-gate wiring and fresh
reservation remain adoption gates. No runtime capture or launch was performed.