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

2026-10-12 logger-gates closure (Devin-Integrator, on master ebea34c82 which
already contains the reviewed restack 7ca7e9159 byte-identical):

- Per-cancellation own-queue lookup is now amortized: the recorder caches a
  canonical (producer, group) -> queue_id index built once per world tick on the
  first proven cancellation (BuildCancellationQueueIndex over own configured
  ProductionQueue traits, ResolveCancellationQueueId for O(1) binds). The index
  is capped at MaximumQueues; duplicate producer/group ownership resolves null;
  a queue created mid-tick after the index resolves null — all fail-closed,
  canonical evidence semantics unchanged. Replaces the per-cancel world scan.
- Coverage/order fixtures extended through the pinned consumer: an off-grid
  terminal (pulse/cancel/pulse/end all landing at tick 137, not a 50 multiple)
  returns OBSERVED_HEALTHY; a terminal end 87 ticks after the last pulse returns
  UNKNOWN (terminal watermark uncovered); a first pulse at tick 37 returns
  UNKNOWN (capture must start at tick zero). A same-tick double-observe state
  test pins callback interleaving as complete; PulseDue(-1) is false.
- Dual-gate campaign wiring landed in ab_campaign_analyzer.py: the new
  --econ-health-dir loads economy_invariants reports as an independent family
  (econ_games) alongside --health-dir (replay_health). Each armed family gets
  its own per-seat verdict through family_view + health_verdict; verdicts
  combine under worst_verdict (BLOCK > SCHEMA_MISMATCH > MISSING > UNKNOWN >
  HEALTHY). A game pools only when every armed checker is HEALTHY for every
  expected seat; foreign-policy reports in a family's view stay SCHEMA_MISMATCH.
  The report gains econ_armed/econ_policy; invalidation reasons name the
  failing checker family. Missing dirs keep the loud-warning opt-in posture.
- Bounds unchanged: activation still deliberately incomplete; no YAML mount,
  no runtime capture, no launch, no measured runtime cost. The dual gate is
  analyzer-side tooling only — it does not mount the recorder or claim live
  coverage.

2026-10-13 QUEUE-REMOVAL-DISCRIMINATOR (01a13686, Devin-Developer; campaign
blocker B; source+tests only, no build/run/push — awaiting exact-SHA review and
DevOps admission):

- Removed terminals carry a cause class resolved event-locally at the emit
  tick (BaseBuilderQueueEvalCA.RemovalCauseClass, world-free): Elimination (seat
  ended), Destruction (producer dead/disposed — kill, sell, demolish and
  transform all ClearQueue and are not separable mod-side), Captured (producer
  live but owned by another player — OnOwnerChanged -> ClearQueue is
  unconditional), Unbuildable (item absent from its own live, owned,
  still-observed queue's BuildableItems — the exact set CancelUnbuildableItems
  diffs to evict), else None.
- The evidence classifier admits Destruction/Elimination/Captured on a Removed
  as proven cause -> Observation (benign, never MarkIncomplete); a Removed
  with no resolved episode and any other class — None, or the diagnostic-only
  Unbuildable — stays UNKNOWN, fail-closed. Canonical cancel records
  (EconomyCancellationEvidence) are untouched: the classes only ever appear on
  Removed-kind queue-observation rows, and Unbuildable maps to no canonical
  string at all, so the pinned economy_invariants.py whitelist needs no change.
- BotQueuePendingTerminal.MaxInFlightTicks 8 -> 24: the RCA measured
  PlaceBuilding intent->removal wire latency of 12-19 ticks across the
  ra1/m1/m2 sentinel episodes, so every placement pending expired before its
  order applied and the proof window could never arm. The bound covers the
  worst observed latency plus margin and still bounds pending retention; cancel
  pendings still never confirm on disappearance.
- queueSeen gates the Unbuildable rung: a stale watch (queue left observation)
  on a live, owned producer carries no buildability evidence and stays UNKNOWN.
- The "coincident-unbuildability residual" named at the 2026-10-13 review is
  CLOSED by demotion, not waived. DISCRIMINATOR-REPAIR (01a13908) implements
  Sol's adjudication (fleet receipt
  ADJUDICATION_2026-10-14_removed_discriminator_6ccc0fad0_sol.md): absence
  from the observed buildable set is not a receipt that CancelUnbuildableItems
  evicted or migrated this item — the routine is cached-set gated
  (unchanged set returns early) and prefers upgrade replacement, so an
  uninstrumented removal plus prerequisite loss before the observer samples
  produces the identical observation. Unbuildable remains serialized on the
  raw queue-observation row as a diagnostic predicate; the evidence latch
  classifies it UNKNOWN like None. A future event-local receipt — an
  engine-side eviction signal, or a same-queue same-tick substitution tying
  queue/item/episode to the exact upgraded successor — is the only path back
  to admission.
- Still unprovable without an engine-side seam (kept UNKNOWN, documented for
  the admission gate): sold vs killed vs demolished vs transformed producers
  (identical observable state — all Destruction); a transform's successor actor
  (inferring it is banned correlation); buildability flicker that restores
  before the next probe tick (item reads buildable -> UNKNOWN); name-only
  pending intent bound to a same-name removal. ep53-class episodes (item
  removed from a live, owned, observed queue while still buildable) remain
  UNKNOWN by construction.