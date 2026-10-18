# BJ cell 10: economy failure before the cap

Existing Death Valley control capture, frozen source a9349d015 / engine 0a3f77dbe1a. This is characterization, not a BJ treatment result. Recovered from the existing 2026-10-08 records; no new game launched.

The hard seat is Multi1, identified independently in situation records and match bot_type; Multi0 is classic. Both match spawn fields are zero, so they cannot identify physical sides. evidence.json retains distinct home coordinates and input hashes.

All 100 hard-seat situation samples (ticks 1?14851) report zero refineries and zero harvesters. At tick 1501, expansion reports 15 known fields, one in reach, none served, and one conyard. The terminal arsenal independently has no refinery entry. This precedes the turtle switch at tick 1807: turtle convergence cannot explain the initial missing refinery.

The hard bot did attack: first launch tick 151, cumulative two launches by tick 2551, still two at tick 14851. Its last positive sampled production_window is tick 8551; the situation army falls to zero at tick 12451, with one brief 220-value sample at 13351. Buildings fall from seven to one by tick 14551. The last sample has no army/harvesters, one building and 8676 banked cash. This supports stalled economic recovery followed by attrition, not a healthy army indefinitely awaiting its first attack. Order totals (455 AttackMove) count intent and repetition, not accepted progress or squad launches.

Match statistics at ticks 13500?14250 record hard earned +136/spent +750 and classic earned +2087/spent +2737, with no combat-cost increase for either. Later situation samples still record losses, so the one quiet interval is insufficient evidence of a sustained stalemate. Match army_value (560 at tick 14250) and situation combat army_value (0 at tick 14251) have different censuses and must remain distinct.

The Lua cap fails a newly created objective for both seats at tick 14901; both Lost outcomes are imposed and excluded from natural win/loss counts. Preserve TIME_CAP_LOW_PROGRESS as the original final-interval classifier, but qualify its interpretation with this diagnostic. Some engagement match-end timestamps exceed duration_ticks; do not use those aggregates as pre-cap survival proof.

Next source check: inspect frozen RA1 Allies refinery eligibility, initial build priorities and recovery prerequisites at a9349d015, including whether generator/overlay correctly retained those rules. Existing evidence cannot distinguish an unavailable refinery from a rejected or deprioritized request; it cannot establish the commit causing the fault. Reuse this capture for regression diagnosis before proposing another reserved run. Do not pool it into the current A5/pilot acceptance or infer BJ effectiveness from the paired, side-swapped natural loss.

Batch remains 16 valid captures: nine natural, seven synthetic-cap endings, with the original Frozen Rift startup failure retained separately. Strict baseline identity/adoption gates remain open. No switch activation, engine change or simulation edit.

## TF-1 / TF-2 correlation hypothesis ? 2026-10-11

The observed sequence is economic failure first (no sampled refinery/harvester and an unserved reachable field), then the turtle switch, then prolonged weak replenishment and attrition. This is consistent with economy pressure causing the fallback; it does not establish that the fallback caused the initial economy failure.

TF-1 addresses optional crawl-link expenditure and stalled routes. Hypothesis: bounding optional links could leave time/resources for the first refinery or recovery, if those links actually displaced the eligible refinery in this capture. This capture does not prove that displacement: seven buildings and substantial banked cash alone do not identify competing queue admission, paid link spend, rejected refinery requests, or placement eligibility. The absence of a refinery may instead be a prerequisite, reachability, placement or planner-selection failure. TF-1 must preserve mandatory refinery/power access and cannot create a missing legal refinery choice by itself.

TF-2 addresses competition between optional construction and feasible mobile replenishment. Hypothesis: protecting a mobile-production share could mitigate later weak army replenishment once income and legal queues exist. The hard bot's missing refinery/harvesters makes the feasibility condition central: a reserve must not block essential economy recovery or promise units whose prerequisites are absent. Banked cash with near-zero earned income and idle queues supports investigating eligibility/arbitration, but does not prove spending contention.

Both are proposed remediation correlations, not historical regression attribution. The cell used frozen a9349d015, before these later default-off increments; neither increment is the cause of the recorded failure. TF-1's old 5bedabace review requested changes; the later exact e8459d316 repair review reports source approval. No claim of runtime effectiveness follows from either review. TF-2 implementation/approval status must be checked independently before integration. No flags are armed, no gameplay edited and no new run requested here.

Review routing: Luna DevOps reviews this evidence checkpoint; Luna Manager remains on A5. A future causal test needs verified first-refinery eligibility plus accepted admission/payment/placement provenance and a reserved runtime slot. Current A5/map/pilot gates remain separate.

## Frozen source and placement follow-up

Seat-bound cameo-ai-placements.jsonl records four hard-seat placements: powerplant tick278 at73,24 (crawl), barracks580 at69,25 (base), gunturret1495 at66,26 (defence), pillbox1947 at66,24 (defence). No refinery placement appears. This cell does not demonstrate a huge hard base or repetitive powerplant spending: only one crawl placement is logged. It therefore supplies weaker direct support for TF-1 budget contention than the separate 4v4 audit. Missing refinery eligibility/placement remains a stronger next diagnostic target than a presumed global construction overspend.

The EX-1 debug entry at tick278 matches the seat-bound powerplant type, coordinate and tick exactly. REF-1 debug entries later report parked anchors74,19 at4250/10250 and84,35 at13500, each after12 replans without a refinery placed. Debug AI(0) is a shared host prefix and is not an independent seat identifier; those latter lines are contextual planner evidence, not a fabricated seat join.

At frozen a9349d015, ExpansionPlannerBotModule.cs lines1923?1944 publishes wantsRefinery from claimable anchor count minus queued refineries, then logs parking after repeated lack of placement. The queue's planner-want priority (BaseBuilderQueueManagerCA.cs lines774?794) serves WantedMcvPrerequisite and WantedLinkBuilding before opening/fraction choices. Its refinery-law placement path (lines1286?1313) requires NextRefineryClaim; a missing claim or unsuccessful LawRefineryPlacement returns no location instead of falling back to an arbitrary home refinery. Thus a reachable field counter does not guarantee a buildable/claimable refinery site or accepted queue choice. These are plausible failure seams, not an established erroneous branch taken in this replay.

The generated cell rules contain cap/speed/conquest fixture edits; they do not remove or override refinery/base-builder traits. The frozen AI lists include RA1 Allies refinery types. This rules out a simple omission in those inspected surfaces, not every inherited rule, tech prerequisite or queue-eligibility defect. Keep TF-1/TF-2 as hypotheses; no gameplay fix or empirical bisect is claimed. The next trace should identify the first refinery production request, accepted queue item and specific claim/placement rejection if the existing replay/order tooling can expose them. New telemetry/runtime work needs separate approval/reservation.

## Preserved replay production trace

The replay parses completely with zero unparsed tails. Subject actor4 binds to hard/Multi1 through all four PlaceBuilding item/decoded-cell pairs, exactly matching the seat-tagged placement log: powerplant73,24; barracks69,25; gunturret66,26; pillbox66,24. The shared client0 is not used as seat identity. Terrain cells decode using engine CPos packing; replay frames are not labeled world ticks.

For this producer the complete replay records StartProduction(ra1_allies_orerefinery) at frame207, then CancelProduction for the same item at480, and no PlaceBuilding for a refinery or later refinery StartProduction. The classic producer3 requests and places refineries; those orders must not be assigned to the hard bot. replay_production.json retains the hard producer's full building-order trace and replay/parser hashes.

This rules out a complete absence of refinery selection intent in the hard bot. It narrows the remaining diagnosis to post-request queue/admission/placement/cancellation and subsequent recovery suppression. It does not prove that the request was accepted or paid, nor that the cancel succeeded. Frozen queue source emits CancelProduction on a building-limit check, exhausted placement failures, or relocation cleanup; the capture lacks a seat-bound reason marker proving which branch produced frame480. Do not upgrade the placement-failure hypothesis to an exact root cause or silently label queued intent as paid progress.

Next: inspect existing order/placement refusal and relocation evidence for frame480's reason, or add a separately reviewed observational reason marker before any newly reserved diagnostic. No gameplay change or launch. TF-1/TF-2 remain bounded remediation hypotheses, and current campaign acceptance is unaffected by this old capture.
