# BJ cell 10: economy failure before the cap

Existing Death Valley control capture, frozen source a9349d015 / engine 0a3f77dbe1a. This is characterization, not a BJ treatment result. Recovered from the existing 2026-10-08 records; no new game launched.

The hard seat is Multi1, identified independently in situation records and match bot_type; Multi0 is classic. Both match spawn fields are zero, so they cannot identify physical sides. evidence.json retains distinct home coordinates and input hashes.

All 100 hard-seat situation samples (ticks 1?14851) report zero refineries and zero harvesters. At tick 1501, expansion reports 15 known fields, one in reach, none served, and one conyard. The terminal arsenal independently has no refinery entry. This precedes the turtle switch at tick 1807: turtle convergence cannot explain the initial missing refinery.

The hard bot did attack: first launch tick 151, cumulative two launches by tick 2551, still two at tick 14851. Its last positive sampled production_window is tick 8551; the situation army falls to zero at tick 12451, with one brief 220-value sample at 13351. Buildings fall from seven to one by tick 14551. The last sample has no army/harvesters, one building and 8676 banked cash. This supports stalled economic recovery followed by attrition, not a healthy army indefinitely awaiting its first attack. Order totals (455 AttackMove) count intent and repetition, not accepted progress or squad launches.

Match statistics at ticks 13500?14250 record hard earned +136/spent +750 and classic earned +2087/spent +2737, with no combat-cost increase for either. Later situation samples still record losses, so the one quiet interval is insufficient evidence of a sustained stalemate. Match army_value (560 at tick 14250) and situation combat army_value (0 at tick 14251) have different censuses and must remain distinct.

The Lua cap fails a newly created objective for both seats at tick 14901; both Lost outcomes are imposed and excluded from natural win/loss counts. Preserve TIME_CAP_LOW_PROGRESS as the original final-interval classifier, but qualify its interpretation with this diagnostic. Some engagement match-end timestamps exceed duration_ticks; do not use those aggregates as pre-cap survival proof.

Next source check: inspect frozen RA1 Allies refinery eligibility, initial build priorities and recovery prerequisites at a9349d015, including whether generator/overlay correctly retained those rules. Existing evidence cannot distinguish an unavailable refinery from a rejected or deprioritized request; it cannot establish the commit causing the fault. Reuse this capture for regression diagnosis before proposing another reserved run. Do not pool it into the current A5/pilot acceptance or infer BJ effectiveness from the paired, side-swapped natural loss.

Batch remains 16 valid captures: nine natural, seven synthetic-cap endings, with the original Frozen Rift startup failure retained separately. Strict baseline identity/adoption gates remain open. No switch activation, engine change or simulation edit.
