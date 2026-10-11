# A/B campaign per-game receipt contract

Schema version: `1`. One immutable UTF-8 JSON object is written for each planned game, including games that never start. Receipts are evidence records, not a source of requested configuration truth: resolved seats must come from generated `map.yaml` and match records. Do not infer a winner from a cap or incomplete game.

## Required top-level fields

| Field | Type | Meaning |
|---|---|---|
| `schema_version` | integer | Exactly `1`. |
| `campaign_id` | string | Frozen campaign identifier. |
| `manifest_sha256` | 64-char hex | Exact approved manifest bytes. |
| `baseline_commit` | 40-char hex | Frozen source baseline; current pin is `964cdb630b1514e1c1a0baed55cbdbc427d5fc11`. |
| `engine_sha256` | 64-char hex | Pinned engine identity. |
| `switch` | string | One tested increment; baseline games use `BASELINE`. |
| `setup_id` | string | Stable setup key including team size, map stratum, and faction round. |
| `map` | object | `name`, repo-relative `path`, package `sha256`, `generated_map_yaml_sha256`, and `required_seats`. |
| `pair_id` | string | Stable matched-pair key `setup_id:seed:pair_index`; same seed/setup/settings, opposite side/spawn assignment. |
| `pair_member` | integer | `0` or `1`, mapped directly to the planned job's `game_in_pair`. |
| `seed` | integer | Seed passed to the game. |
| `seed_proof` | object | `env_value`, exact server-log pin line, and server-log SHA-256. |
| `teams` | object | `A` and `B`, each with `arm` (`control` or `treatment`), `bot_type`, and resolved `seat_ids`. |
| `resolved_seats` | array | One element for every actual seat; details below. |
| `end_class` | enum | `NATURAL`, `CAP`, or `INCOMPLETE`. |
| `end_reason` | string/null | Authoritative natural end reason, cap marker reason, or incomplete cause. |
| `winner_team` | `A`, `B`, or null | Required null for `CAP` and `INCOMPLETE`; natural winner only when all seat outcomes agree. |
| `world_tick` | integer/null | Final observed world tick. |
| `cap` | object | `world_tick_cap`, marker observed, marker tick; cap class requires valid marker and tick. |
| `memory` | object | Process ID, `peak_private_bytes`, `limit_private_bytes` (6.5 GiB), and `samples`. Each sample has `utc`, `world_tick`, `private_bytes`, and `actor_count`. |
| `runtime` | object | `started_utc`, `finished_utc`, `exit_code`, `wall_seconds`, `stall_seconds`, and `pid_scoped_cleanup` boolean. |
| `artifacts` | object | Paths and SHA-256 values for `server_log`, `driver_log`, `matches_jsonl`, `replay`, `map_yaml`, and support bundle; unavailable artifacts are null with an explanatory `missing_reason`. Present paths resolve under the explicit campaign artifact root. `matches_jsonl` is a unique per-game file, not the shared aggregate. |

## Resolved-seat fields

Each `resolved_seats[]` entry contains `seat_id`, `team` (`A`/`B`), `arm`, `bot_type`, `faction`, `home`, numeric `spawn`, and `proof_source` (must name the generated `map.yaml` plus match-record evidence). `outcome` is `won`, `lost`, `draw`, or `unknown`. The `metrics` object contains `earned`, `spent`, `banked`, `army_value`, `kills`, and `deaths`; unavailable values are null, never fabricated as zero. Include per-seat capture-tick telemetry in `metrics.timeline`, with rows `{tick, earned, spent, banked, army_value, kills, deaths}`. Team members remain clustered within one game/pair observation.

## Classification and validation rules

- `NATURAL`: authoritative natural-end evidence and complete, consistent outcome records for all seats; `winner_team` may be set only when the team result is unambiguous.
- `CAP`: cap marker and configured cap tick are both proven, and the support bundle is complete; `winner_team` must be null. This is censored, never a win/loss/draw.
- `INCOMPLETE`: startup failure, crash/sentinel, memory kill, stall, wall timeout, unexpected process exit, or missing/invalid evidence; `winner_team` must be null and `end_reason` must preserve the concrete cause.
- A missing receipt for a planned game is materialized by the analyzer as `INCOMPLETE` with `end_reason=NO_RECEIPT`; it must not be dropped or replaced silently.
- Reject duplicate pair members, manifest/map/engine mismatches, absent or duplicate seats, seat/faction/spawn mismatch, seed-proof mismatch, memory over-limit without an `INCOMPLETE` memory-kill classification, and contradictory end evidence.
- The analyzer derives the planned cell by exact `setup_id`, `map`, `seed`, `pair_id`, and `pair_member` matching. For started games it verifies the unique per-game `matches_jsonl` artifact hash and requires all its rows to contain exactly one `game_uid`. It can aggregate these verified per-game files directly; an optional aggregate JSONL is only a cross-check and must exactly equal the per-game rows. It does not infer a UID from seed/map/seats.
- Driver writes to a temporary file, flushes it, then atomically renames it to the final receipt path. The final receipt is immutable; corrections are separate linked records.

The corresponding strict JSON Schema and fixture validators are added by the driver implementation. This Markdown contract is the producer/consumer interface for the analyzer work.
