# Turtle regression audit ? 2026-10-11

## Verdict and evidence boundary

The available Ice Cold evidence confirms excessive power/crawl construction, but does **not** establish that both teams built no armies, never attacked, or reached a combat-free stalemate. The only local complete eight-seat match capture records offensive squad registrations and combat for every seat; the treatment team's live combat armies and buildings reach zero before the forced cap outcome. The other two Ice Cold captures are incomplete. A campaign-wide causal regression commit is **not identified**: there is no matched earlier 4v4 Ice Cold baseline in the inventoried files. This is an evidence checkpoint, not a campaign approval or a completed empirical bisect.

Read-only: no games, replays executed in-engine, source edits, strategy activation, or new test processes. All figures below come from streaming existing records/replay orders. Artifacts and scripts are on `codex/turtle-regression-audit`; this branch inherits an old review/tooling base and must not be merged wholesale.

## Coverage and reproducibility

Inventoried local `C:/cameo-wt/*campaign*/results` plus `sol-ab-1008/results-*`: 62 capture roots with driver/server evidence, 43 match logs, 92 distinct seat records, 2,170 statistics samples. Acceptance, smoke, A5 diagnostics and earlier 1v1 characterization are separately identifiable by paths; these are **not 62 valid campaign games**. No local Satan's Clutch/Terra Cotta cloud match records were found in this census. Coordinator was asked for missing cloud roots and frozen receipts; those remain required to complete the requested all-games scope.

`turtle-audit-results/capture_orders.json` classifies every inventoried capture; `census.json` contains every recorded game/seat with arsenal and terminal metrics. `timeline.csv` supplies earned/net-spent/army/assets/bank over time; `truth_timeline.csv` supplies 16,639 recorded opponent truth samples (zero conflicting duplicate truth samples). `situations_timeline.csv`, `placements_timeline.csv`, `engagements_timeline.csv` and `behavior_summary.json` preserve per-seat time-series observations (8,364 situation rows, 3,324 placement rows, 5,089 closed engagement records across captures). `input_hashes.json` freezes input paths, byte sizes, SHA256s and snapshot UTC. No all-capture pooling is used for a win effect.

Metric limits: statistics ArmyValue follows actor `UpdatesPlayerStatistics` flags, whereas situation/truth army values count recorder-classified combat actors; they are different measures. Assets minus statistics army is a **non-army asset proxy**, not exclusively building value. Arsenal created counts include starting/captured/transformed actors; catalog defence counts differ from runtime placement categories. Placements are cumulative placements, not surviving counts. Power alive counts over time and paid unit-production share are **UNKNOWN**: these files lack the required complete per-item debit/refund/ownership ledger. Net resources spent is not gross paid unit spend. Never estimate a spend share by multiplying creation counts by nominal cost.

Offensive `attacks_launched` increments in `SquadManagerBotModuleCA.RegisterNewSquad`; it proves registration of one of the included offensive types, not accepted order causality or forward progress. First registration at tick151 can be a small initial specialized squad. Engagement records are local recorder episodes, may overlap across seats and are not unique global battles. Replay AttackMove totals include repeated/FSM/defensive orders; host client IDs do not identify bot seats. M13 accepted offensive activity remains a stronger, separate evidence contract.

## Capture validity

- Original BU Nuclear Winter attempt2/attempt3: aliases rejected / overlay includes rejected, zero economy, starting bank12345. **INVALID_OVERLAY_OR_ALIAS / no-deploy**, not gameplay turtling. The existing complete-manifest fix and typed deployment guard address that tooling failure; do not pool it with valid deployment games.
- `campaign-validation-20261011b/4v4_ice_2026_0`: UID a986e791-ced2-4c44-91d3-3e5a8e89889c, receipt INCOMPLETE at world tick25001; Polygon distance negative-sqrt exception in exception log. Replay footer exists but does not turn a crashed game into a valid outcome. Global AttackMove8362 / Attack86, not zero offensive orders.
- Same batch `4v4_sahara_1337_0`: receipt INCOMPLETE at20001 with Polygon crash. Exclude outcome.
- `campaign-validation-merged-20261011/4v4_ice_2026_0`: UID86f27b47-72e3-4839-8790-15f841144704, receipt INCOMPLETE at40001, missing replay terminator and match records. Global AttackMove13695 / Attack206. No causal end reason inferred beyond incomplete receipt.
- `campaign-validation-polyfix/4v4_ice_2026_0`: UID354f6de7-dddd-4910-b84b-e7432af2f195, cap45001, complete replay and eight terminal records. **CAP-censored diagnostic**, not a natural result. Receipt's `baseline_commit=964cdb...` is stale metadata and cannot authenticate loaded source. Archived FullMods YAML does register both aliases, hard/generic conditions, and a treatment condition; ST/AF/BP condition granters remain off for these aliases. No both-arms-baseline assertion from rejected earlier overlays transfers to this capture. Full treatment payload and binary provenance still require canonical manifest/source reconciliation.

## Complete Ice Cold capture

| Seat / arm / faction | Power created | Defence created | Peak statistics army | Offensive registrations | Engagement records | Final live combat / defence / buildings | Bank before cap |
|---|---:|---:|---:|---:|---:|---|---:|
| seat_3 / control / td_gdi | 95 | 22 | 78950 | 22 | 117 | 33430 / 10500 / 130 | 378 |
| seat_4 / control / td_nod | 24 | 23 | 106739 | 31 | 270 | 52679 / 30500 / 87 | 156 |
| seat_5 / control / ra1_allies | 18 | 25 | 104110 | 29 | 214 | 61820 / 38800 / 84 | 1 |
| seat_6 / control / ra1_soviets | 12 | 14 | 139520 | 27 | 211 | 72880 / 17600 / 72 | 1 |
| seat_7 / treatment / td_gdi | 15 | 8 | 34660 | 17 | 215 | 0 / 0 / 0 | 2207 |
| seat_8 / treatment / td_nod | 17 | 14 | 36730 | 21 | 259 | 0 / 0 / 0 | 2178 |
| seat_9 / treatment / ra1_allies | 56 | 12 | 48070 | 14 | 264 | 0 / 0 / 0 | 2131 |
| seat_10 / treatment / ra1_soviets | 26 | 6 | 81860 | 38 | 332 | 0 / 0 / 0 | 2304 |

The control seat3 builds 95 catalogued power plants; 96 runtime-category power placements are logged (different classifiers/capture semantics), all96 with placement reason `crawl`. Treatment seat9 records56 power placements, all56 `crawl`. This is direct evidence for the visible construction pattern. Control late banks are 1?378; spending pressure is evident, but which queue caused each unit delay is unrecorded. Treatment brownout counters are nonzero for seats7/8/10; surviving control seats have zero final accumulated brownout in situation records. Large idle-production counters sum queues/ticks and must not be compared directly with single world duration.

Across the complete capture: 199 offensive registrations and 1,882 engagement records; 12,641 AttackMove,198 Attack,1 AssaultMove replay orders. Situation sampling records only seat9 in turtle (109 of301 samples); seats3?6 have **no turtle samples**, and seats7/8/10 also have none. Empty terminal personalities do not imply turtle or swapped seat identity: explicit situation `seat` and `game_uid` provide the join. This capture therefore does not support a shared team-wide turtle latch. It does support persistent crawl expenditure under other personalities.

Selected time slices (latest situation <= requested tick; remaining seats and all samples in artifacts):

| Seat | Tick requested / sampled | Combat value | Defence value | Buildings | Registrations | Bank | Power placements to tick |
|---|---|---:|---:|---:|---:|---:|---:|
| seat_3 | 7500 / 7351 | 2510 | 1500 | 13 | 1 | 8701 | 4 |
| seat_3 | 15000 / 14851 | 13920 | 3000 | 29 | 4 | 1471 | 7 |
| seat_3 | 30000 / 29851 | 20720 | 4500 | 66 | 12 | 242 | 45 |
| seat_3 | 45000 / 44851 | 32530 | 10000 | 129 | 22 | 343 | 96 |
| seat_9 | 7500 / 7351 | 1790 | 4600 | 17 | 1 | 7983 | 4 |
| seat_9 | 15000 / 14851 | 3650 | 5900 | 26 | 3 | 688 | 7 |
| seat_9 | 30000 / 29851 | 5560 | 11100 | 73 | 11 | 937 | 49 |
| seat_9 | 45000 / 44851 | 0 | 0 | 0 | 14 | 2131 | 56 |

## Earlier baseline and regression localization

Earlier local 2026-10-08 captures are 1v1, not matched 4v4 Ice Cold. Durable STATUS_2026-10-08_ab_ready_feature.md identifies mod a9349d015 / engine0a3f77dbe1a; initial seed1337 fixtures create a primary objective at load and cap at14900, and later BJ fixtures corrected that harness. These cohorts are characterization, not interchangeable campaign counterfactuals.

At world14250 on Nuclear Winter TD GDI seed1337, old hard-vs-hard seatMulti0 has statistics army69550/assets110150 (63.14%), bank38596; Multi1 already has zero army/assets. The newer side1 capture has treatment19220/52820 (36.39%), bank8597, control35310/80210 (44.02%), bank8896. This is a lower surviving army share for those sampled seats, **not a causal commit bisect**: different opponents/arm rules/engines/harnesses, one old seat eliminated, and no matched 4v4 cohort. Do not compare 14900 old peaks to45000 new peaks as a regression test. Full 7500/14250 seat comparison is `nuclear_time_comparison.json`.

Source-history localization:

- BaseCrawlChance100 originated at6b58bba39 (2024-07-01); global budget600 at002e9441d (2026-06-09), per-bot max150 atae88a4347 (2026-06-16). These are long-standing, not newly introduced Oct10 changes.
- REF-1 explicit link supply at40bdade21 (2026-10-04), power-preferred legal GBA links at6f1b09702 (same day), are present in the older a9349d015 lineage too. They are plausible amplifiers in a long large-team game, not evidence of a new Oct10 regression introduction.
- ST scale-target idle threshold at3cf337713 (2026-10-02) scales MaxIdleUnits upward with army target, violating the M13 design's absolute-idle escape requirement. It is a real latent liveness concern, but archived Ice Cold granter is fransbot-only; **not the active cause in this BU capture**.
- c33de2ef1 (2026-10-11 08:38:47Z) subsequently pins initial campaign personalities to rush and raises budget600/150 to900/200. Archived Ice Cold YAML retains600/150 and no campaign personality pins, so this mitigation was not loaded in the studied capture. Pinning initial personality does not freeze MasterAi's later adaptation. No outcome/effect approval for this mitigation is supplied here.

## Code diagnosis and fix proposal

**Strongest construction mechanism:** ExpansionPlanner `LinkBuildingWanted` remains true while targetHops>0 and no claimable anchor is in reach. `PickCrawlLink` prefers a power plant; the CA queue consumes `WantedLinkBuilding` before ordinary building fractions and limits, provided the same name is not already in production. Placement then forces BaseCrawl and bypasses organic chance/cost checks. There is no per-episode spent-link allowance, army-spend reservation, or a proved decrease in remaining route distance in this predicate. This supplies power plants even with ample power. Recorded crawl placements are consistent with this mechanism; placement reason alone does not distinguish planner supply from organic crawl, so exact branch attribution remains UNKNOWN without a reason/progress receipt.

**Additional pressure:** low-power rescue is first priority; reserve grows100+10 per building, clamped1000. Ordinary fractions scale with building count. Both can perpetuate construction demand. Global unit budget counts mobile nonharvesters, excludes buildings, and pauses unit production only. With eight unresolved bots,600/8 gives75 per bot (later900/8 gives112). A bot can stop adding units while construction continues. No BotBudget debug census exists in these captures, so cap-hit causality is unproved. AF army-first is off; even enabling it alone leaves power essential, so it does not bound planner power links.

**Attack mechanism:** registration counts are nonzero, so a blanket 'never launches' fix misses this capture. Need last accepted-progress gaps, eligible idle values, target legality, response occupancy and spend arbitration to distinguish ineffective small waves, retained armies, and lack of close-out. M13 is a default-off pending implementation, not an active master cause. Its observed-activity reset must stay fail-closed and exclude idle/stuck/return/flee orders; small registration alone must not keep renewing the deadline.

Proposed increments, separately reviewed/default-off, preserving economic rescue and one-new-restraint-per-increment:

1. A bounded crawl episode allowance: preserve mandatory power needed for operation/refinery deployment; limit *optional* link supply by actual paid link cost/count, require deterministic frontier progress toward the same owner episode, and retire/park a nonprogressing route. Do not replace this with an arbitrary global power cap that can starve defenses/economy. Emit admission/refusal and route-distance provenance. Tests: true power deficit still admitted; affordable advancing link admitted; nonadvancing repeated link exhausts allowance; changed target starts new episode; unsupported progress evidence refuses optional link.
2. A separate queue arbitration increment that reserves a reviewed mobile-production share only while production is feasible, mobile capacity is available and an eligible offensive pool needs replenishment; optional defenses/links cannot consume that reservation. Refunds/cancellation/elimination must release it; essential first-yard/refinery/power exceptions remain explicit. Test competing queues and exactly-once release, then measure paid unit share.
3. Finish approved M13 absolute idle threshold and bounded offensive liveness/response leases; log accepted offensive progress, not registration-only telemetry. Keep gameplay, combat consumer, and paired3v3 gate separate. This addresses actual long inactivity; no guaranteed suicide attack into known fortifications.
4. Carry forward PERSONALITY-LOGGING+TURTLE findings: independent pressured bots can converge through MasterAi's turtle fallback without shared state. A reason/expiry-bounded fallback experiment can retain a legal offensive incumbent outside real base danger; no team-wide random/counter hack. This Ice Cold capture does not justify forcing all personalities to rush.

## Open evidence / completion gates

Missing cloud Clutch/TerraCotta games and canonical source/engine/arm receipts; matched pre-campaign 4v4 Ice Cold replay/capture; paid queue ledger/cap census; actor-order ownership and accepted launch/progress provenance. An empirical bisect requires a fixed map/factions/seed/spawns/engine/arm configuration and old/new candidate games under a future explicit HEAVY_RUN_WINDOW reservation. Do not choose a culprit commit from the unmatched 1v1 comparison. No launch reservation requested or held by this audit.
