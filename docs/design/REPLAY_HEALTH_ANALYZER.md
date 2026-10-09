# Replay health analyzer and campaign stop gate

## Scope and current implementation

Maintainer request, 2026-10-09: detect fundamental bot failures automatically,
stop wasteful A/B runs, and explain strengths and weaknesses from each capture.
Owner: Sol; branch `codex/replay-health-gate`, base `3d99405bd`;
exclusive worktree `C:/cameo-wt/sol-replay-health`. Files claimed for this task:
`tools/ai/replay_health.py`, `tools/tests/test_replay_health.py`, this document,
and bounded routing/handoff/log entries. Claim ends at delivery. Coordinator
owns serial campaigns; Architect owns the refinery repair. No game launch,
runtime telemetry edit, scheduler mutation, engine change or master push here.

Existing `ab_summary.py`, `mission_story.py`, `round_trip_check.py`, order
decoders and JSONL loggers were inspected. They provide aggregates, mission
analysis, order identity or observation, but no startup economy stop gate.
Use the existing repaired decoder; do not create another replay parser.

The first implementation is a **telemetry-assisted symptom checker**, not a
complete replay simulator or causal analyzer. It consumes existing per-capture
JSONL, selects an explicit GameUid/player, emits evidence and bounded warnings.

```powershell
python tools/ai/replay_health.py C:/capture --game-uid GAME_UID --player Multi0 --output C:/capture/health.json
python -m unittest discover -s tools/tests -p test_replay_health.py -q
```

| Exit/status | Meaning and scheduler action |
|---|---|
| 0 / OBSERVED_HEALTHY | Startup refinery observed; this alone does not approve capture integrity, routes, spending or combat. |
| 20 / BLOCK | Persistent startup economy symptom; latch a review hold before another cell. Cause is unknown. |
| 21 / UNKNOWN | Missing, invalid, unsupported or insufficient evidence; hold review rather than silently pass. |
| WARN findings | Retain game/outcome; investigate spending or combat without treating an ordinary loss as a structural bug. |

`--live` accepts only newline-committed rows and defers an incomplete last line.
**Existing situation logs buffer until match resolution**, so this option does
not make old logging stream. Today the gate can stop before the next match;
early termination needs the live pulse described below. The checker never
launches, kills, resumes, edits or repairs a campaign.

Policy `startup-economy-v1`: conventional profiles TD GDI, TD Nod, RA1 Allies,
RA1 Soviets and Japan only; 3000-tick grace, 1500-tick persistence, maximum
750-tick sample gap. Alarm requires an alive construction yard, no own refinery
ever observed in snapshots/placement/lifecycle, zero harvesters, and successful
same-faction classic refinery placement in the same game. Other economy types
are UNKNOWN. This conservative sentinel detects an asymmetry worth holding;
it does not establish resource accessibility, map fairness or a code cause.
An intended no-economy map, asymmetric damaged start or special profile needs
an explicit manifest/profile ruling before this gate is adopted there.

Warn separately on persistent high bank, poor killed/lost value exchange, and
high bank with little spending over a contiguous 3000-tick window. Spend is
aggregate spend, not specifically non-power production. Timeline idle queues
are instantaneous counts, not cumulative idle ticks. Refunds/other income mean
earned resources are not a proof of harvester delivery. No warning changes exit
status. Detailed economy/combat timelines retain the original units and ticks.

Inputs are bounded (128 MiB/file, 1 MiB/row, 200000 rows/file), JSON objects only,
supported schemas, stable identities and increasing ticks. Reports contain
consumed-prefix hashes. Fingerprints copied from summaries are provenance,
not validated launch receipts: scheduler must separately enforce manifests,
parser completeness, hashes, end reason and crash/cap status.

Review hardening: duplicate keys/nonfinite constants and wrong identity/schema/
timeline types are rejected. Reports must target a **new** file outside capture
Logs/Replays; existing files are never overwritten. Publication uses a flushed
sibling temporary file and exclusive atomic link, refusing concurrent target
creation. Event joins and spending-window analysis are linear; a 200000-point
timeline/200000-event regression exercises the configured row bound. Live
files do not share a commit watermark yet: restrict symptom holds to quiescent
captures; a future live logger must commit a shared watermark before automated
early termination.

## What the historical runs tell us

Offline scan of 127 completed hard-player captures at
`C:/cameo-wt/sol-ab-1008/devin-ab-campaign` produced 64 startup holds (33 Allies,
31 Soviets) and 63 startup-observed records (35 GDI, 28 Nod). Control cells
04–07 alarm at evidence tick 4501. Machine-readable receipts are preserved in
fleet `replay_health_historical_2026-10-09.json`. These are repeated fixed
screens, not independent statistical samples; counts describe this frozen
population, not a win-rate estimate. The capture base is the older `a9349d015`
campaign, not the tool's `3d99405bd` base.

RA1 runs remain valuable regression evidence for the first-refinery
cancellation and broken economy. They cannot support combat-switch efficacy
claims while the common startup defect dominates. Nod is different: refinery
startup exists; Pitfight underdevelopment/hoarding deserves a separate queue
and tactical investigation. Alpine can develop normally. Terminal zero
refineries after destruction does not mean startup failure. Preserve all old
runs; do not repair their labels into feature wins/losses or merge them with
fresh controls.

## Feasibility and evidence boundaries

A useful analyzer is possible; a replay-only tool cannot recover every reason
behind an order. OpenRA records framed order packets and metadata. Playback
reconstructs simulation with the matching rules/assets/engine; the original
bot reasoning is absent. The local pinned `ModularBot.Activate` explicitly
bypasses activation on replay, and Cameo loggers exclude replay. Re-running
the AI during playback would produce new reasoning, not recover original
decisions. [OpenRA ReplayRecorder source](https://github.com/OpenRA/OpenRA/blob/bleed/OpenRA.Game/Network/ReplayRecorder.cs),
[ReplayConnection source](https://github.com/OpenRA/OpenRA/blob/bleed/OpenRA.Game/Network/ReplayConnection.cs).

Exact fork authority: engine `0a3f77dbe1a90bc59ff3ef5ad81534e8fbcf7695`,
mod `3d99405bd`; online bleed/release documentation is architecture context.
Local evidence: `OpenRA.Game/Network/ReplayRecorder.cs`,
`OpenRA.Mods.Common/Traits/Player/ModularBot.cs:70`,
`OpenRA.Mods.Cameo/Traits/AiSituationLogWriter.cs:55–73,110–129`,
`AiMatchLogWriter.cs`, `AiPlacementLogWriter.cs`. Situation snapshots buffer;
placements log success/lifecycle, not search rejection; match timeline is
750-tick aggregate. Logging is useful but insufficient for early automatic
causal diagnosis.

Existing Python replay readers can use pythonnet and engine assemblies rather
than guess the binary format. That is an alternative interoperability route,
not a reason to replace our repaired parser or silently load a mismatched
engine. [Author's replay-reader documentation](https://github.com/anvilvapre/openra-replay-reader/blob/main/README.md).
OpenRA exposes trait and Lua facilities suitable for controlled scenarios and
observations; APIs must be checked against our pin before implementation.
[Official trait documentation](https://docs.openra.net/en/release/traits/),
[official Lua documentation](https://docs.openra.net/en/release/lua/).

## Detailed capture design

Combine three layers: packet/order integrity, reconstructed or captured world
state, and original decision-reason telemetry. Every finding needs a trigger,
time interval, actor/queue/field identifiers, cited evidence, confidence,
counterevidence and a reproduction recipe. Store diagnostic text separately
from lockstep gameplay. Avoid fresh path searches for logging; record results
already computed by the owning module.

New opt-in `cameo-ai-economy-health.jsonl` should flush bounded committed pulses
every 750 ticks plus transition events. Common envelope: schema, GameUid,
player slot, faction/profile, map UID, seed, mod/engine/config hashes, world
tick, sequence, observed-vs-reconstructed origin, dropped/truncated counters.
Only actual host/client compatibility and resolved Ruleset profiles count.

| Domain | State/reasons needed | Automated diagnoses |
|---|---|---|
| Economy | Accepted harvest delivery, compatible harvesters/docks, both directed routes, harvest/wait/unload state, field depletion/known threat | No delivery, blocked dock, excessive travel/wait, deadlocked clients; distinguish destruction, depleted or unreachable resources |
| Construction | Per-category failCount, suppression deadline, selected item/producer/claim, candidate outcome enums, cancel/refund, retry/reset cause | Permanent queue latch, repeated rejected footprint, no claim, unbuildable producer, policy hold, starvation |
| Spending | Cash/reservations/power/tech eligibility, actionable pending demands, producer utilization and queue age | Cash hoarding with actionable demand; genuine blocked production vs deliberate reserve |
| Expansion | Lease/claim transitions, requester/owner IDs, acceptance/refusal reason, legal alternative considered | Repeated refused expansion, stale leases, overlapping reservations, stranded MCV |
| Combat | Mission/target changes, known enemy estimates, retreat/engage reasons, ownership, losses by encounter | Churn, idle armed groups, harmful engagement, missed defence, effective trades; tactical weaknesses stay outcomes |
| Production/plugs | Admission, requested batch, active/held/infinite items, compatible free slots, cap token, cancellation | Queue blockers, cap race, unusable upgrades; preserve normal intentional cancellation |
| Performance | Tick CPU summaries, allocation/GC stalls, module timing samples, path-query counts, wall-clock/RTT separately | Simulation cost vs network delay, excessive probes, resource exhaustion |
| Integrity | Exception/OOM, parser unparsed/incomplete, missing/drop sequences, desync, mismatched fingerprint/end receipt | Invalid capture; never infer game defeat from a failed runner |

Proposed bounds: 32 transition events/player/750-tick interval, 256 KiB/player
per match, coalesced repeat counts, buffered flush at pulse boundary. Overflow
is TRUNCATED/UNKNOWN, not evidence absence. Keep a small reason ring for the
failure interval; no per-cell/per-tick actor dumps by default. Profiled runtime
overhead is an acceptance gate. A detailed log must remain affordable.

All-faction capability profiles must resolve real Ruleset roles, actual docks,
client compatibility and exceptional economies (Yuri slaves, WC2 land/water,
OP2 inherited defaults). The static 32-faction inventory is a plan, not proof
that 32 profiles support conventional refinery assertions.

## Stop, triage, repair and resume

1. Persist validated capture/receipt, integrity label, outcome/end reason and
   health report separately. Check result before scheduling the next cell.
2. On explicit reason-backed FUNDAMENTAL_BLOCK, latch stop with artifact paths
   and fingerprint. Prototype symptom BLOCK also latches a review hold; it is
   not automatic source-bug certainty. UNKNOWN/incomplete holds diagnosis.
3. Emit an incident bundle: earliest divergence, relevant log window, order
   trace/replay/video if available, profile, suspected subsystem, reproducible
   fixed seed/map, and suggested source locations. Do not erase failed attempts.
4. Assigned agent diagnoses and fixes on an isolated branch; independent review
   plus regression and startup sentinel gates precede resume. Automated agents
   can assist triage, but a detected loss never authorizes arbitrary live patches.
5. Resume explicitly on a new frozen cohort/root and fresh controls. Stop marker
   survives restart; no auto-resume by deleting a warning or changing defaults.

Current scheduler seam audit found `devin_ab_campaign.py:238` ignores
`run_cell(False)` and line239 marks the group done anyway. Fix this alongside
gate integration: failed/missing cells cannot count as completed. Use
append-only event JSONL and atomic derived snapshots; persist stop before
another launch. Coordinator owns this implementation and serial runtime.

After the refinery repair: first serial startup/load/dock tests for all resolved
profiles, then fresh matched controls and candidate sentinels for GDI/Nod/RA1
Allies/Soviets plus Japan. Mirror spawns on the same maps/seeds across factions
to separate faction from map effects; include RA passable bibs, blocked exits,
water/trees/cliffs and both <=10-tile harvester legs. Then full feature A/B.
Keep ordinary weak outcomes; report uncertainty and paired effects rather than
using health warnings to filter losses.

Clock receipts must distinguish world ticks, normal/default-equivalent time,
accelerated fixture time and wall time. Current requested cap is 45000 ticks:
30 default-speed-equivalent minutes via 40ms Lua default clock, 45 seconds
under the fixture's 1ms timestep. Child wall timeout remains separate. Do not
reinterpret the cap as 1.8 million ticks or infer a cap from two lost labels.

## Delivery and remaining gates

V1 offline checker and regressions are implemented. Historical detection is
validated; original rejection causes and a live early-stop stream remain
future work. Independent review, Coordinator integration, actual post-fix
matches, runtime logging overhead, all-faction profile resolution and replay
determinism are separate gates. No fresh A/B success or master release claim.

Research contributions: fleet `RESEARCH_2026-10-09_replay_health_gate.md`
(Luna Manager), `REPORT_2026-10-09_luna_campaign_health_gate_seams.md`
(Luna DevOps), and `REPORT_2026-10-09_faction_ab_forensics.md` (Sol + Luna).

## Maintainer construction and cash invariants (2026-10-09)

New separate `tools/ai/economy_invariants.py` preserves the approved startup
checker bytes. Run against `Logs/cameo-ai-economy-health.jsonl` with explicit
GameUid/player; exit semantics match the startup checker. This is an offline
verifier against a new contract, not evidence that runtime logging is shipped.
Missing old-log fields are UNKNOWN. No live decision without shared watermark.

Mandatory defects: live building producer's ready item unplaced for >=250
ticks; idle construction queue for >=1500 ticks; >=3 cancellations of the same
building type owner-wide within an inclusive1500-tick rolling window. Reasons
(no site, policy hold, cap, affordability, relocation, enemy damage) are recorded
for diagnosis; they do not erase the requested threshold violation. Dead
producer queues are not treated as a postmortem obligation to produce.

Initial **proposed** cash band is 1000..10000 combined cash+stored resources,
inclusive. Outside band for1500ticks is a defect; near-empty total funds
(<=100) for250ticks is an additional proposed signal. Exact zero is unsuitable:
the maintainer confirms passive income of one credit per tick below1000 credits.
Passive income must remain distinct from accepted harvest delivery evidence.
Full positive-capacity player storage for250ticks is
a defect. Zero capacity is not100% storage. Above-band spend delta<=1000 is a
separate float/no-spending signal. These are starting thresholds to validate
by faction/economy; no universal calibrated optimal bank is claimed. Resource
storage capacity is player-wide engine capacity, not a separate tank at each
refinery. Do not confuse unbounded cash with stored resources.

Schema1 contract: all records carry GameUid/player/map_uid/faction/profile,
profile_supported=true only after Ruleset profile resolution, schema integer1,
seq contiguous from0, monotonic worldtick and dropped0. Kinds:

- `pulse` every<=50ticks: complete building-queue census (queues_complete=true),
  player_active bool, cash/resources/capacity/spent cumulative nonnegative ints;
  each queue has unique queue_id, producer_live bool, state ready/idle/producing/
  paused, exact state_since_tick, reason. Ready additionally has item and unique
  production item_id; state timestamps are captured from transitions, not
  reconstructed from occasional samples. Logger keeps queue IDs stable.
- `cancel`: queue_id/item/production item_id/reason, exactly once per cancelled
  item. Cancel records are building-queue events only. Duplicate item cancellation
  is invalid evidence, not another counted defect.
- `end`: complete=true, same envelope, after final pulse<=50ticks, commits the
  quiescent capture. Missing sequence/pulse/census/terminal or unsupported profile
  makes UNKNOWN even if earlier defect evidence exists.

Runtime logger/Architect queue observer must supply these fields before the
driver can require this new gate. Actual accepted delivery remains an additional
independent event, never inferred from cash/credited income. Tests cover exact
250/1500 boundaries, ready/idle, dead producer, cancellations, balance recovery,
near-empty/full storage, passive-income starvation, inactive players, low spending
and malformed/incomplete capture.
