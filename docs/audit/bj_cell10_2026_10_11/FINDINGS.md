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
