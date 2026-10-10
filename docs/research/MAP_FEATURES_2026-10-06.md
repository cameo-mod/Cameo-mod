# MAP FEATURES — continuous, bounded learned policy inputs — 2026-10-06

## Contract

`MapFeatureVector/v1` is calculated once per bot at match load from public rules, terrain, and the bot's own legal start. It is a vector of signed integers: raw cells/counts/value where that preserves useful resolution and permille or fixed-point ratios otherwise. It is never a map class and never contains a filename, title, UID, hash, author, map path, map cell identifier, or player identity.

Map mechanics remain live: TacticalMap/ZoneTopology find actual corridors and chokepoints, ExpansionPlanner finds actual fields, and placement finds current legal cells. Learned policy is a smooth bounded function of `(faction, matchup, MapFeatureVector)` that weights those live candidates. It may not store or select a map or pre-recorded cell.

The game writes the vector and version to anonymous logs for audit. It does not expose it to an opponent and does not use match state after load. The offline fitter accepts only records with the same feature schema and balance fingerprint, or applies its normal evidence decay.

## Integer feature schema

Every scan orders cells by `CellIndex`, actors by `ActorID`, starts by `PlayerIndex`, and candidate paths by `(length, destination CellIndex)`. A path is a deterministic shortest path using the locomotor relevant to the decision. Ratios use `x * 1000 / max(1, denominator)`, rounding only at the named final division.

| Group | Fields | Measurement |
|---|---|---|
| Map extent | `width_cells`, `height_cells`, `playable_cells`, `aspect_permille`, `perimeter_cells`, `blocked_share_permille` | Exact usable board dimensions/cell count, `max(width,height)*1000/min(width,height)`, and passability. |
| Seats and starts | `player_count`, `team_count`, `own_start_degree`, `closest_enemy_path_cells`, `furthest_enemy_path_cells`, `enemy_path_spread_permille`, `nearest_ally_path_cells` | All distances are from the bot's own start. Enemy starts exclude allies and are sorted before min/max. |
| Resource access | `nearest_resource_path_cells`, `nearest_expansion_path_cells`, `resource_field_count`, `reachable_field_count`, `spreader_count`, `total_resource_value`, `resource_value_per_player`, `resource_value_per_playable_cell_milli` | Field components and expansion candidates are public geometry. A start-field is distinct from the nearest non-start expansion. |
| Resource shape | `resource_near_spawn_permille`, `resource_contested_permille`, `resource_centre_permille`, `resource_edge_permille`, `resource_corner_permille`, `resource_cluster_permille`, `resource_mean_field_path_cells`, `resource_field_path_spread_permille` | “Near” is path distance ≤ one quarter of closest enemy path; contested means within 10% path distance of two enemy/own starts; centre/edge/corner use ruleset-normalised board bands. Cluster is within-field value divided by total value. |
| Objectives | `nearest_poi_path_cells`, `nearest_tech_path_cells`, `nearest_capturable_path_cells`, `nearest_oil_path_cells`, `poi_value_per_player`, `poi_contested_permille` | All capturable public actors are classified by trait/tag and then measured from own start; absent categories use an explicit sentinel `-1`. |
| Naval access | `nearest_water_path_cells`, `water_share_permille`, `own_start_water_access_permille`, `naval_route_share_permille` | Water is classified by a dedicated naval locomotor; `-1` means no reachable water. |
| Chokes and openness | `nearest_chokepoint_path_cells`, `chokepoint_count`, `chokepoint_width_mean_cells`, `chokepoint_width_min_cells`, `chokepoint_width_p90_cells`, `minimum_cut_cells`, `openness_permille`, `route_detour_permille` | ZoneTopology provides candidate corridors. Width is passable cross-section; minimum cut is start-to-centre/frontier. Openness is reachable area divided by bounding area. Detour is median path distance / straight-cell lower bound. |
| Travel topology | `start_to_field_ratio_permille`, `start_to_poi_ratio_permille`, `expansion_to_enemy_ratio_permille`, `frontier_path_spread_permille`, `component_count` | Ratios normalize each path by closest-enemy path so a new map interpolates by shape rather than raw scale alone. |
| Placement/defence | `buildable_near_start_permille`, `buildable_frontier_permille`, `defence_arc_permille`, `backline_depth_cells`, `elevation_spread` | Public buildability and terrain geometry used by placement/defence only. |

`-1` is a valid “absent/not reachable” sentinel for distance/value categories. The feature encoder supplies an accompanying `has_<field>` bit to a model; it never silently converts absence to a zero-distance observation. Future additions append fields and increment schema version—old models ignore unknown fields and new models reject missing required fields.

## Smooth model and fitted file

Every learned scalar is a smooth function of faction, matchup and this vector. The default artifact is **Integer Additive Ridge v1**, because it is compact, deterministic, explainable, and interpolates between maps:

```
y = clamp(base[faction, matchup]
          + Σ feature_coefficient[i] * normalized_feature[i] / 1000
          + Σ selected_hinge_coefficient[i,k] * max(0, normalized_feature[i] - knot[i,k]) / 1000,
          lower, upper)
```

`normalized_feature` is an integer robust z-like transform derived offline from global median and inter-decile range, clamped to `[-3000, 3000]`. Coefficients, knots, bounds, prior and evidence are signed 32-bit integers. The fitter uses weighted ridge shrinkage to faction/matchup → faction → global coefficients; an insufficient effective sample count returns the parent/global model. Interaction terms are allowed only from a reviewed, capped registry (for example water share × naval route share, or chokepoint width × closest-enemy path), no polynomial explosion.

For discontinuous choice arms (opening, route, unit mix), use **capped prototype smoothing** instead: at most 64 reviewed, anonymous feature prototypes per `(parameter, faction, matchup)`; choose the weighted mean/reward where `weight = max(0, bandwidth - weighted_L1_distance)`. Fixed normalisation, sorted prototype ID, integer division and lexical tie-breaking make it deterministic. Below `MinimumWeight` or `MinimumEffectiveSamples`, fall back to the parent/global prior. The fitter may emit either model type, never an unbounded training-point list.

At runtime model selection happens only when its increment switch grants the provider. A missing model, schema mismatch, invalid coefficient, sparse evidence, or disabled trait returns the existing hand-authored value exactly. All consumer calculations stay integer-only.

## Storage bound

The core vector has roughly 60 values; it is one per bot start in an event, not a learned table. An additive model with 60 coefficients, 24 approved hinges, metadata and three scope layers is about 4–8 KiB per parameter scope. A 64-prototype model with a 60-feature sparse mask is capped at roughly 24–80 KiB per scope. With 40 planned parameters, 12 factions, 144 faction matchups, strict sparse emission and parent fallback, reviewed shipped YAML is budgeted at **8 MiB** (CI hard cap 12 MiB and 64 prototypes/scope), independent of map count. Logs may retain measurements under their normal retention policy; no model grows when a map is added or renamed.

## Consumer mapping

| Policy family | Runtime use of the vector |
|---|---|
| Fight: predictor calibration, effective unit value, engage/retreat | EngagementPriors derives a bounded prediction residual from vector features only where the fitted model has evidence. CombatVeto and SquadManager ask the same `IBotMapFeatures` provider for the threshold delta; missing/disabled data leaves current priors and `BotLimits` unchanged. LEARN-FIGHT’s current `BV_learn_fight` threshold provider must therefore add this optional delta after its faction/matchup lookup, clamped to its existing limits. |
| Economy/production | Expansion, harvester, power and build-order consumers pass their current live decision context plus map vector to an additive model; distance-to-resource, field shape, contested share, travel and choke features adjust thresholds, never choose a stored field/cell. |
| Tactical/placement | TacticalMap, defence, artillery, staging and route consumers score their current legal candidates using map vector plus candidate-local features. They never read an historic coordinate. |

The `IBotMapFeatures` provider owns vector calculation and schema validation. It is instantiated for generic bots only behind the first map-feature increment; existing consumers receive `null` and retain their current code path. The fitting model is loaded once before play and cannot learn during a match.

## Tests and gates

1. Encoder unit tests: shuffled terrain/actor/start iteration produces byte-identical vectors; integer boundary arithmetic; unreachable/absent sentinels; symmetric start swaps; known tiny maps; and no map text/UID/hash in the serialized record.
2. Geometry tests: resource components, start/field/POI/water paths, choke widths/count, minimum cut and ratios agree with independently constructed maps; test all locomotors used by consumers.
3. Fitter tests: linear and hinge synthetic truths interpolate at unseen feature values; outliers clamp; ridge shrinkage and effective-sample fallbacks work; prototype cap and deterministic ties hold; a renamed or byte-identical copied map produces the same vector and result.
4. Runtime tests: YAML integer round-trip, schema mismatch/missing provider returns the exact existing value, switch-off fixed-seed order stream remains identical, and overflow bounds are safe.
5. League gates: slice outcomes by continuous feature quantiles for audit only (not lookup); reject a regression in safety, fog honesty, order rate, variety, or any low-support region. Run the no-map-identity grammar audit and the 8/12 MiB file-size caps in CI.
