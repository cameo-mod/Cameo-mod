## Resume

Next: ask the acting lead for the serial A5 slot and current approval status; meanwhile resolve the stale SWITCH_ORDER catalog artifact with its owner. Current local HEAD before the status-only update: `635c480e6199be03cc834a21f9bdf52321a9fe2f`, based on exact current `origin/master@964cdb630b1514e1c1a0baed55cbdbc427d5fc11` (merge base now equals master). No campaign or parity launch, boot, push, or integration has occurred from this worktree.

Implemented in the runner branch: strict receipt shape/cross-field validation and atomic immutable JSON write; per-process private-byte and system-free-RAM monitor with a 6.5 GiB stop, 8 GiB hard ceiling, tick/wall/stall observation, exact in-process seed pin, actor sampling, and PID-scoped child cleanup. The module does not yet integrate a queue, generate final receipts from match records, or start campaign jobs. Required gates remain approval, final manifest/hash lock, engine map acceptance/spawn symmetry, A5 exact-pin parity, reserved serial slot, and integrated end-to-end receipt/stop/resume proof.

## Progress

### Step 1 — receipt schema contract

`AB_RECEIPT_SCHEMA_2026-10-11.md` defines immutable per-game receipt fields for campaign/source/engine/manifest pins; experiment/setup/map/pair/seed; seed proof; team-to-arm assignment; resolved seats/factions/spawns; natural/cap/incomplete classification and winner; per-seat economy/army/combat/timeline; peak private bytes and actor-count samples; runtime/PID cleanup; and artifact hashes. Caps and incomplete games have no winner. Missing receipts are preserved as incomplete. Exact path sent to Luna DevOps.

### Step 2 — map inventory

`MAP_PREFLIGHT_2026-10-11.md` records all eight amended-scope maps resolved at the exact baseline pin. All package SHAs match the previously pinned assets; parsed playable seat references and spawn actor counts match the required 2/4/6/8 seats. Counts do not prove spawn symmetry or engine acceptance; those remain gates.

### Baseline

Pinned for all remaining work: `origin/master@964cdb630b1514e1c1a0baed55cbdbc427d5fc11` (includes the authorized INTEG-LEARN squash, B3 park fix, and replay identity support). Current runner branch still has stale merge-base `700bb164`; rebase it before any test/manifest pin claim. Do not launch campaign games until approval, final manifest, map gate, A5 parity and serial reservation are verified on the exact baseline.

## Runner receipt validation update

The strict JSON schema from the manager worktree was copied into `tools/ai/ab_campaign_receipt.schema.json`. `campaign_runner.validate_receipt` now checks exact keys, hashes, path safety, team/seat identity consistency, outcome/winner consistency, seed pin equality, timeline ordering, memory-stop classification, artifact hash/missing-reason consistency, and CAP proof. Three focused receipt tests pass; no game or boot process was started. The schema file was not present in the latest fetched campaign branch tip, so it is now included in this runner branch and should be reviewed with the runner changes.

Known focused-suite exception: `test_switch_order_covers_the_frozen_sixty_group_catalog` fails because `SWITCH_ORDER_2026-10-11.md` pins `700bb164` and lacks current `BP_squad_desire`; notified the SWITCH_ORDER owner. Do not delete or weaken that check.

Current verification on exact code tree: `py_compile` passed; receipt-focused tests 4/4 passed; campaign planner/analyzer plus pilot tests 26/27 passed, with only the stale SWITCH_ORDER catalog failure above; `git diff --check` passed. The exact-pinned no-launch CLI dry run completed: 64 games, 32 generated maps, one treatment change, 11.07 worker hours, 7.29 parallel-slot wall hours, zero launches. Input manifest SHA-256 `e7c09e25e5b5f7fd367a3ce1ab8d45d1348b005ec82216bcc1fd7ec36f613e04`; fixtures `tools/tests/fixtures/ab_campaign_pilot_dry_run.json` and `tools/tests/fixtures/ab_campaign_pilot_dry_run_receipt.json`. No full `tools/tests` run was started.
