## Resume

Next: keep the A/B pilot blocked until the A5 exact-baseline parity/order gate is rerun with a per-process cap consistent with the 6 GiB free-RAM reserve and accepted; then finish focused receipt-adapter fixtures/review, refresh the reviewed manifest and tool hashes, close map/engine acceptance and spawn-symmetry gates, and obtain an exclusive serial reservation plus explicit campaign authorization. Current runner code includes generated-map seat and natural-outcome cross-checks; latest code SHA is recorded in git. Merge-base remains `origin/master@964cdb630b1514e1c1a0baed55cbdbc427d5fc11`. A5 remains FAILED/OPEN: earlier runs peaked at 6.55 and 8.07 GiB; a later single hard-vs-hard 1v1 reportedly reached ~8.29 GiB at WT~3050/~65s with no match/replay and unknown exact run/config/logger SHAs. With 15.42 GiB free, a 12 GiB cap would leave 3.42 GiB (<6 GiB reserve); 9.42 GiB is the theoretical ceiling before headroom. Use <=8 GiB for any authorized bounded diagnostic and continuously enforce the free-RAM floor. No campaign cells are authorized. No campaign or parity launch, master push, or integration has occurred.

Implemented in the runner branch: strict receipt shape/cross-field validation and atomic immutable JSON write; per-process private-byte and system-free-RAM monitor with a 6.5 GiB stop, 8 GiB hard ceiling, tick/wall/stall observation, exact in-process seed pin, actor sampling, and PID-scoped child cleanup. The module attaches supervised process fields to a validated receipt without inferring natural outcomes from exit code; cap classification requires the exact 45k marker, seed proof, and support bundle, otherwise it records INCOMPLETE. A strict per-game receipt adapter is also present in `ab_campaign_pilot.py`: it derives job identity from the canonical receipt, verifies artifact-root confinement and hashes, and obtains `game_uid` only from the receipt's unique per-game matches artifact. Source syntax and diff checks passed, but this adapter remains untested/unreviewed. It still does not integrate a campaign queue, generate resolved seats/metrics from match records, or start campaign jobs. Required gates remain A5 exact-pin parity below memory limits, focused adapter fixtures/review, final manifest/hash lock, engine map acceptance/spawn symmetry, reserved serial slot, and integrated end-to-end receipt/stop/resume proof.

## Progress

### Step 1 — receipt schema contract

`AB_RECEIPT_SCHEMA_2026-10-11.md` defines immutable per-game receipt fields for campaign/source/engine/manifest pins; experiment/setup/map/pair/seed; seed proof; team-to-arm assignment; resolved seats/factions/spawns; natural/cap/incomplete classification and winner; per-seat economy/army/combat/timeline; peak private bytes and actor-count samples; runtime/PID cleanup; and artifact hashes. Caps and incomplete games have no winner. Missing receipts are preserved as incomplete. Exact path sent to Luna DevOps.

### Step 2 — map inventory

`MAP_PREFLIGHT_2026-10-11.md` records all eight amended-scope maps resolved at the exact baseline pin. All package SHAs match the previously pinned assets; parsed playable seat references and spawn actor counts match the required 2/4/6/8 seats. Counts do not prove spawn symmetry or engine acceptance; those remain gates.

### Baseline

Pinned for all remaining work: `origin/master@964cdb630b1514e1c1a0baed55cbdbc427d5fc11` (includes the authorized INTEG-LEARN squash, B3 park fix, and replay identity support). Verified `git merge-base HEAD origin/master` equals that SHA. Do not launch campaign games until approval, final manifest, map gate, A5 parity and serial reservation are verified on the exact baseline.

## Runner receipt validation update

The strict JSON schema from the manager worktree was copied into `tools/ai/ab_campaign_receipt.schema.json`. `campaign_runner.validate_receipt` now checks exact keys, hashes, path safety, team/seat identity consistency, outcome/winner consistency, seed pin equality, timeline ordering, memory-stop classification, artifact hash/missing-reason consistency, and CAP proof. Three focused receipt tests pass; no game or boot process was started. The schema file was not present in the latest fetched campaign branch tip, so it is now included in this runner branch and should be reviewed with the runner changes.

Known focused-suite exception: `test_switch_order_covers_the_frozen_sixty_group_catalog` fails because `SWITCH_ORDER_2026-10-11.md` pins `700bb164` and lacks current `BP_squad_desire`; notified the SWITCH_ORDER owner. Do not delete or weaken that check.

Current verification after receipt-result mapping: `py_compile` passed; receipt-focused and process-guard tests 6/6 passed, including a dummy Python child (no OpenRA) that verifies in-process seed propagation, cap marker parsing, and incomplete-vs-cap classification with/without a support bundle. Campaign planner/analyzer plus pilot tests 28/29 passed; the only failure remains stale SWITCH_ORDER catalog coverage. `git diff --check` passed. Exact-pinned no-launch CLI dry run: 64 games, 32 generated maps, one treatment change, 11.07 worker hours, 7.29 parallel-slot wall hours, zero launches. Input manifest SHA-256 `ed81da06a3ddd542ab25c5b3fab7c5cd17ffa6ea2806abf649a0d50baa990a3d`; fixtures `tools/tests/fixtures/ab_campaign_pilot_dry_run.json` and `tools/tests/fixtures/ab_campaign_pilot_dry_run_receipt.json`. No full `tools/tests` run was started.

## 2026-10-10 17:20Z source-only refresh

- Rechecked isolated branch: HEAD and `origin/codex/ab-campaign-runner-final` both `8cfec8f9e9cb5be44738de027fb6c733d4fd8eb8`; merge-base with `origin/master` is `964cdb630b1514e1c1a0baed55cbdbc427d5fc11`.
- Independently recomputed SHA-256 for all eight `.oramap` package paths pinned in `ab_campaign_pilot.py`; all eight matched. This establishes package identity only, not engine acceptance or spawn symmetry.
- Ran no-launch generated-map preflight with the current test helper's dynamically built, explicitly unapproved manifest: 32 variant `map.yaml` files / 144 seat bindings proved bot, faction, home, and spawn mapping; planned 64 games, launches 0. This is generator evidence only, not a substitute for the reviewed final manifest, engine acceptance, or spawn-symmetry gate.
- Static MapSize/mpspawn review using the runner's current `split_spawn_sides` order found exact D4-reflection/rotation partitions for Satan's Clutch, Red Spice, Terra Cotta, Back to Basics, and Winter's End. No exact grid symmetry exists under any 4+4 partition for Great Sahara 2 or Ice Cold, nor for A Nuclear Winter's 1v1 pair; Ice Cold is same-shape only after a 2-cell vertical translation, and Nuclear Winter is off by 2 y-cells under half-turn. This is spawn-coordinate geometry only and not a terrain/economy fairness test; keep these maps behind explicit map review or replace them before campaign use.
- The tracked `tools/tests/fixtures/ab_campaign_manifest_dry_run.json` is an older contract (`schema/switch mismatch`) and cannot serve as the current pilot manifest. No generated-map preflight was claimed from it.
- `SPEC_2026-10-11_ab_campaign.md` still says proposal / no launches authorized; `OPEN_DECISIONS.md` retains unresolved Amendment 2 checkpoint allocation. A5 parity, final reviewed manifest, generated map seat proof, engine acceptance/symmetry, and serial slot reservation remain open. No game launched.

## Analyzer integration blocker

The immutable receipt writer (`campaign_runner.validate_receipt`) and the pilot analyzer (`ab_campaign_pilot.analyze` / `adjudicate`) currently have incompatible JSON contracts. The canonical receipt has `setup_id`, `pair_member`, `end_class`, `resolved_seats`, and nested `map`/`cap`/`memory`/`artifacts`; the analyzer requires `cell_id`, `game_uid`, `setup`, `pair`, `game_in_pair`, `status`, `seats`, `cap_marker`, and `support_complete`. There is no adapter in the CLI path, and the canonical receipt has no `game_uid` field to associate match rows. A valid runner receipt therefore cannot currently flow directly into analysis; keep the pilot blocked until a strict adapter or a single reconciled schema is implemented and reviewed.

## 2026-10-11 manager schema compatibility check

- Reviewed published `codex/ab-campaign-tooling-manager@0fe28abad487323350b9dffe0951f2eb57722611` schema and `AB_RECEIPT_SCHEMA_2026-10-11.md`. The published schema confirms the mismatch above: it requires `setup_id`, `pair_id`, `pair_member`, `end_class`, and `resolved_seats`, disallows unlisted keys, and has no `game_uid` or `cell_id`.
- The analyzer still requires each receipt's `cell_id` and uses receipt `game_uid` to join `matches_jsonl` rows. The schema has enough planned identity to derive `cell_id` only if a stable `setup_id`/`pair_id`/`pair_member` mapping is documented and checked against a unique planned job. A UID could be recovered without adding a field only if `artifacts.matches_jsonl` is contractually a per-game file, its SHA is verified, and it contains exactly one UID; current schema prose does not guarantee per-game granularity or define artifact path resolution.
- No schema/adapter edits, tests, launches, or integration were performed in this check. Pilot remains blocked. Contract/adapter resolution: either add `game_uid` for started games (nullable only for never-started `INCOMPLETE`) or guarantee and specify per-game match artifact semantics; in both cases define stable `cell_id` mapping and validate artifact SHA, UID uniqueness, and all job pins before analysis.

### Producer implementation cross-check (latest fetched manager branch)

- Reviewed `origin/codex/ab-campaign-tooling-manager@07c036d5b7c682c728959993f9a58dbb57317e8f`. Its `run_one` writes `artifacts.matches_jsonl` to the unique game directory `Support/Logs/cameo-ai-matches.jsonl`, so it is in fact a per-game artifact; the receipt builder sets `pair_id` to `setup:seed:pair` and `pair_member` to `game_in_pair`. Those producer conventions make a strict adapter possible without adding `game_uid`, provided the consumer resolves artifact paths inside an explicit support root, verifies SHA, and requires exactly one UID per started game's artifact.
- The manager branch's `ab_campaign_pilot.analyze` still expects legacy `cell_id`/`game_uid` receipt fields and its CLI does not perform that artifact-backed normalization. So the gap is now narrower: the producer data is sufficient, but the adapter and human contract/path-root rules are still missing. No files from the manager branch were merged or copied.

## 2026-10-11 receipt-to-analyzer adapter draft

- Added `normalize_campaign_receipts` to the owned runner branch. It validates each strict receipt, maps `setup_id`/map/seed/`pair_id`/`pair_member` to exactly one planned job, confines present artifact paths to an explicit `--artifact-root`, verifies map/match/server/support artifact hashes, checks the pinned seed line in `server.log`, extracts one unique `game_uid` only from that receipt's per-game match artifact, and optionally requires exact equality with corresponding aggregate `--matches` JSONL rows. Without an aggregate file, it analyzes the union of the hash-verified per-game rows. The analyzer then receives its prior normalized internal shape. The Markdown contract documents the producer identity conventions.
- Source check: `py -3 -m py_compile tools/ai/ab_campaign_pilot.py` and `git diff --check` passed. Per instruction, tests were not run. The adapter has not yet been exercised against valid, malformed, incomplete, and cross-game negative-control receipts; do not treat it as accepted or ready for pilot until that focused fixture review passes. No games were launched.
- The adapter/contract delta changes pinned tool/content hashes. Refresh the final manifest and add focused canonical-receipt fixtures before any analysis or launch authorization.

## 2026-10-11 acting-lead readiness handoff

- Read `STATUS_2026-10-10_acting_lead.md`: A5 run-a peaked at 6.55 GiB private bytes (above the 6.5 GiB stop), run-b reached 8.07 GiB and was PID-scoped stopped. There is no valid same-seed parity/order-stream receipt.
- A/B is NOT READY. Keep the per-game 6.5 GiB stop active; do not launch outcome/campaign cells until exact-baseline A5 parity/order proof and strict receipt-to-analyzer join integration pass. Serial reservation and other approval/map gates also remain prerequisites.
- This is a status/evidence update only; no tests, launches, branch/ref changes, or integration were performed.

## 2026-10-11 adapter source audit

- Source-only review found that the receipt adapter checked `map.yaml`'s hash but did not independently bind `resolved_seats` to the generated map contents; the existing `proof_source` string was not sufficient evidence by itself.
- Added `validate_receipt_map_seats`: it reads the hash-verified generated `map.yaml`, rejects duplicate or unexpected Multi player references, checks each planned home/bot/faction/Playable/HomeLocation tuple against its actual `PlayerReference` block, bounds spawn indexes, and derives expected coordinates from the pinned map's spawn actors using the batch writer's side-split routine. Natural match rows are also required to have exactly the receipt's homes and seat outcomes, preventing contradictory winner evidence from being analyzed.
- `py -3 -m py_compile tools/ai/ab_campaign_pilot.py` and `git diff --check` pass. No tests or launches were run. The adapter still needs focused positive/negative fixtures and an independent exact-SHA review; A5, final manifest, engine map acceptance/spawn symmetry, and authorization gates remain open.

## 2026-10-10 MiniYaml parsing correction

- Replaced both generated-map `PlayerReference` regex block readers with the shared `tools/audit/miniyaml.py` parser, retaining duplicate-reference rejection and the same bot/faction/home/playable checks. This follows the repository rule against hand-parsing YAML.
- `py -3 -m py_compile tools/ai/ab_campaign_pilot.py` and `git diff --check` pass. Focused tests were not rerun; behavioral fixture coverage and independent exact-SHA review remain open. No launch, manifest refresh, or campaign gate changed.

## 2026-10-11 A5 memory-budget update

- Acting-lead report adds a single 1v1 diagnostic at ~8.29 GiB private / WT~3050 / ~65 seconds, on baseline 964 plus an unspecified logger optimization, with zero match/replay. No exact run, logger, or kill-path-fix SHA was supplied. This is censored resource evidence only; root cause and parity remain UNKNOWN.
- Host was reported at 31.93 GiB total / 15.42 GiB free with no OpenRA. A 12 GiB process cap would violate the 6 GiB reserve; retain `min(8 GiB, available_RAM - 6 GiB)` for any separately authorized diagnostic, with continuously monitored system floor. This does not authorize a launch or relax the campaign gate.
