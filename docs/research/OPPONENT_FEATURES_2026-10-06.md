# OPPONENT FEATURES — identity-free per-match attribute vector — 2026-10-06

## Contract

`OpponentFeatureVector/v1` describes the **currently observed behaviour of one opposing seat in this match**. It is recomputed from zero each match, held only in match memory, and discarded at game end. It is never keyed, cached, joined, or persisted by player name, account, client index, stable seat, IP, lobby slot, replay ID, map identifier, or hash. The only durable learned artifacts are smooth models keyed by own faction, current public enemy faction/doctrine, and anonymous continuous feature values.

This is an estimate of what this bot has seen, not a player skill rating. It may only read its own fog memory, actors currently visible to the bot, public faction data, its own actions, and public map features. Offline truth can label how well the observed estimate predicted an outcome, but may never become a runtime input. In FFA/team games it is held separately per current enemy seat; consumers either score the threatening visible enemy, or aggregate visible enemy vectors by observed threat value with sorted `(faction, current-seat-index)` tie breaking. There is no cross-match carry-over.

## Evidence already available

P0 already writes anonymous `opponent_signatures` from `AiMatchLogWriter`: per observer/enemy sample it records seen faction, army/role values, defence/building/harvester counts, known regions and last-seen tick. `BotFogMemory.Observe` supplies that seen-side census. Samples are emitted when the signature changes subject to a floor of `max(750, SampleIntervalTicks)`, and are relabelled with truth only at game end for offline evaluation. The initial `fit_opponent_signatures.py` clusters nine logged census fields; it is useful as a migration input but its logarithmic buckets are not a runtime identity or the new vector model.

Add an observer-only `OpponentFeatureBotModule` adjacent to `BotSituation` and reuse the P0 signature buffer rather than introduce a second hidden-state reader. It issues no orders. A runtime consumer receives only a read-only vector plus confidence.

## Fog-honest integer fields

All values are signed integers: values/ticks/counts are raw, shares and estimates are permille. `-1` with an explicit `has_*` bit means absent or not observed; zero means a measured zero. Every numerator includes only an observed event/census and every denominator includes its observation opportunity count.

| Attribute group | Fields | Seen-side construction |
|---|---|---|
| Coverage/confidence | `seen_actor_samples`, `seen_regions`, `observation_age_ticks`, `contact_ticks`, `confidence_permille`, `coverage_permille` | Evidence mass is capped, decayed by last-seen age, and scaled by sampled visible regions/encounters. It is the confidence of observation, never confidence in a person. |
| Macro-efficiency proxy | `visible_econ_growth_per_1000t`, `visible_production_growth_per_1000t`, `visible_bank_pressure_permille`, `construction_completion_rate`, `resource_exposure_permille` | Uses visible refineries/harvesters/production buildings, visible construction starts/completions, and observed military/economic value growth. It is an evidence-conditioned macro-efficiency estimate, never a read of enemy cash, income, queues, or resource assignments. |
| Army curve | `army_value_slope`, `army_value_acceleration`, `military_to_econ_permille`, `production_to_army_lag_ticks`, `role_entropy_permille`, `tech_depth_permille` | Differenced P0 seen census with integer least-squares over a fixed rolling window; observed buildings/tech actors and role value shares only. The current census never subtracts units hidden since an earlier sighting. |
| Expansion pace | `visible_expansion_starts_per_10000t`, `visible_expansion_completion_rate`, `expansion_distance_ratio_permille`, `field_claim_rate`, `outpost_commitment_permille` | Records only a visible refinery/depot/outpost start or completion and its public field/path geometry. Unseen expansions are unknown, not failures. |
| Response | `threat_response_latency_ticks`, `defence_response_rate_permille`, `counter_role_latency_ticks`, `retarget_after_contact_ticks` | Start a clock at an observed mutual encounter/visible own threat; stop only when a visible enemy movement, new defence, counter role or retreat manifests. Do not claim the enemy saw an invisible threat and do not inspect their order queue. |
| Micro proxy | `focus_convergence_permille`, `low_hp_retreat_rate_permille`, `overkill_waste_permille`, `formation_spread_permille`, `fire_uptime_permille` | At visible engagements, infer target convergence from attacks/damage landing on visible targets, retreat from a damaged visible actor moving away from the observed threat, and fire uptime from visible weapon events. These are observable effects, never enemy order/APM reads. |
| Observable action tempo | `visible_action_transitions_per_1000t`, `multi_front_response_permille`, `command_burst_permille` | Count visible state transitions—new construction, deployment, attack/fire onset, retreat, move-direction change—not internal orders. The label is an APM proxy only, conditioned on coverage. |
| Style: Aggression | `aggression_permille`, `first_pressure_tick`, `attack_value_share_permille`, `harass_frequency_per_10000t`, `forward_commitment_permille` | Visible army value entering own/frontier regions, damaging/raiding events, and aggression timing, divided by visible force/opportunity. |
| Style: Greed | `greed_permille`, `economy_share_permille`, `tech_investment_permille`, `expansion_commitment_permille`, `resource_security_permille` | Visible economic/tech/expansion value and construction flow, normalised by observed assets and map opportunity. |
| Style: Concentration | `concentration_permille`, `largest_front_share_permille`, `front_entropy_permille`, `route_repeat_permille`, `split_response_permille` | Visible army distribution across public regions/routes; routes are encoded as live topology attributes (entry width, path ratio, objective type), never a coordinate or map identity. |
| Signature | `role_share_*`, `unit_mix_novelty_permille`, `tech_sequence_distance_permille`, `air/naval/stealth_share_permille` | Reuses P0 role census and first observed build-category/tech sequence. It compares against public faction/doctrine templates with bounded edit distance; it never writes a unit-list identity. |

Temperament axes are evidence-weighted estimates in `[0,1000]`, not labels. The renderer or policy may name an axis locally, but synchronises only integer values if it ever needs network representation.

## Update, confidence, and expiry

1. Sample the existing observed enemy census at its current 750-tick minimum and on a material visible change. Engagement-local micro and response facts are accumulated only while both relevant actors are visible.
2. Recompute a vector every 250 ticks from fixed-size integer windows: 3,000 ticks for reaction/micro, 7,500 for action tempo/army slope, and 15,000 for macro/style. Windows contain aggregate counters, not event identities.
3. For each field, `confidence = clamp(1000 * effective_observations / required_observations, 0, 1000)`, then multiply by coverage/recency permille. `effective_observations` is capped and decayed when no relevant actor remains observed. A model receives both feature and confidence; below field-specific floors it uses a missing bit and parent prior.
4. At game end, only anonymous `{enemy_faction, feature_schema, feature values, confidence, map-feature schema, outcome labels}` is appended to the P0 record. Seat references remain in-record relational labels only and are removed/rewritten by the existing anonymous-log grammar before fitting. The in-memory vector is disposed with the world.

## Smooth learned policy

Every learned adjustment is a deterministic integer function of:

`(own faction, public enemy faction/doctrine, OpponentFeatureVector, MapFeatureVector, current decision context)`.

Use the same bounded Integer Additive Ridge / approved hinge registry defined in `MAP_FEATURES_2026-10-06.md`. Opponent features are robust-normalised offline, clamped to `[-3000,3000]`, and multiplied by their confidence before evaluation. Models shrink through `(faction, faction-vs-faction) → faction → global`; sparse/missing/low-confidence vectors return the parent or neutral policy. Choice arms use at most 64 anonymous prototypes per scope with fixed weighted-L1 smoothing and minimum effective weight. Neither form contains opponent records, names, map identities, nor an unbounded sample table.

The normalisation and model-schema file has a 1 MiB cap per policy family, 64 prototypes per scope, 32 approved interactions, and a 12 MiB aggregate learned-data CI cap shared with map features. Runtime uses 64-bit intermediates for multiply/divide and clamps each published threshold to its existing safety bounds.

## Consumer plan

| Consumer | Safe response to vector | Fallback |
|---|---|---|
| LEARN-FIGHT | `EngagementPriors` may apply a confidence-weighted residual only to observed-force calibration. `CombatVeto` and `SquadManager` can tighten/loosen engage and retreat bounds from aggression, visible army slope, response/micro confidence, and map travel features. The provider never treats an unseen army as weak. | Existing faction/matchup prior and `BotLimits` thresholds. |
| ECON | Expansion, harvester, production and defence planning use observed aggression/greed, expansion pace and counter-role latency to adjust conservative risk/hold thresholds. | Existing seen-threat/economy rules. |
| BUILD | BuildOrderKnobs, composition and scout choices use role shares, observed tech/air/naval signals, and their confidence to weight existing public-category arms. | Existing faction/doctrine opening and fog-honest react channels. |

All consumers are default-off behind their own increments. `OpponentFeatureBotModule` may log unconditionally as P0 telemetry, but no policy reads it without an enabled provider. It does not issue orders, modify the opponent, or change an order stream when consumers are off.

## Test plan and gates

1. Build observed-only fixtures where an enemy hidden behind shroud differs from the visible census; vectors must be identical. Audit forbids `Player`, owner names, account/client fields, enemy cash/queues, truth, raw enemy orders, map UID/name/hash, and coordinates in provider/model keys.
2. Test window arithmetic, sorted iteration, missing sentinels, age decay, confidence floors, overflow clamps, world disposal, and no carry-over into a second match with the same faction.
3. Test micro/response attribution with visible and invisible control cases; invisible order changes may not change the vector. Test FFA aggregation ordering and allied exclusion.
4. Test smooth fitting on synthetic points: interpolation for unseen attribute/map combinations, shrinkage under sparse evidence, prototype cap, schema mismatch, and bit-identical integer round trip.
5. Require fixed-seed switch-off parity, fog-honesty/no-identity grammar audits, model-size caps, and league A/B results sliced by confidence and continuous-feature quantiles. Reject a safety, order-rate, variety, or low-observation regression.
