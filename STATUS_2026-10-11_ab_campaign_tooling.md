## Resume

Step 1 receipt schema and Step 2 eight-map inventory are complete. Step 3 tooling is implemented and focused tests/no-launch dry run pass on branch `codex/ab-campaign-tooling` (last base SHA `ca590543108a6f90349a59acca711dc9678a10a0`; check current HEAD before continuing). Next: refresh fixture tool/schema hashes after final edits, inspect diff, commit/push; send exact schema path+SHA to Luna DevOps and send the final receipt to lead. Open launch gates: execution-approved manifest; exact-baseline A5 parity receipt; engine acceptance and symmetric-spawn proof for all eight maps; downstream analyzer fixture/negative controls. Never launch campaign cells until all gates pass.

## Progress

### Step 1 — receipt schema contract

`AB_RECEIPT_SCHEMA_2026-10-11.md` defines immutable per-game receipt fields for campaign/source/engine/manifest pins; experiment/setup/map/pair/seed; seed proof; team-to-arm assignment; resolved seats/factions/spawns; natural/cap/incomplete classification and winner; per-seat economy/army/combat/timeline; peak private bytes and actor-count samples; runtime/PID cleanup; and artifact hashes. Caps and incomplete games have no winner. Missing receipts are preserved as incomplete. Exact path sent to Luna DevOps.

### Step 2 — map inventory

`MAP_PREFLIGHT_2026-10-11.md` records all eight amended-scope maps resolved at the exact baseline pin. All package SHAs match the previously pinned assets; parsed playable seat references and spawn actor counts match the required 2/4/6/8 seats. Counts do not prove spawn symmetry or engine acceptance; those remain gates.

### Baseline

Pinned for all remaining work: `origin/master@964cdb630b1514e1c1a0baed55cbdbc427d5fc11`. Correction: B3 squash `b070e788318ea76203ce443007b85d3b32c8318a` is an ancestor of this pin; replay-health seat-identity fallback is the tip commit. Existing A5 evidence is from an earlier master-lineage SHA, not exact `964cdb63`, so it does not satisfy the runner's exact-baseline gate.

### Driver and no-launch verification

- `tools/ai/ab_campaign_receipt.schema.json` is the strict machine-readable receipt contract; `AB_RECEIPT_SCHEMA_2026-10-11.md` is its human-readable companion. Luna DevOps was sent the paths.
- `tools/ai/campaign_runner.py` supports fail-closed dry-run and gated `execute-one`, atomically claims immutable jobs in SQLite, uses the shared size/RAM slot counter, resolves the exact map and seat roster, runs with an in-process pinned seed, samples private bytes and actor count, enforces 45,000 tick / 3,000 second / 180 second stall limits, kills only its Popen handle or verifies PID+image+support path before orphan cleanup, and emits immutable per-game receipts.
- `tools/ai/ab_campaign_pilot.py` verifies baseline/map/tool/schema/binary hashes; control/treatment aliases differ only by one candidate over the ratchet baseline. Private-memory and slot ceiling are 6.5 GiB per process and free-RAM floor is 6 GiB.
- Focused tests: `py -3 -m unittest tools.tests.test_ab_campaign_pilot tools.tests.test_campaign_runner -q` — 17/17 PASS; `py_compile` and JSON schema parse pass. Never run `tools/tests` as one suite.
- No-launch dry run on the pinned 964 source and 6da7fce engine: 64 games, 32 map variants, 144 seat proofs; 11.07 estimated worker-hours, 7.29 slot-wall hours (5.11–9.47 range); 0 launches. Receipt: `tools/tests/fixtures/ab_campaign_pilot_dry_run_receipt.json`.
- No campaign/A5 games were launched. Engine map acceptance and symmetric-spawn proof remain untested. The execution gate requires an explicit approved manifest plus a two-run exact-pin A5 JSON receipt with seed pins and `IDENTICAL`/`IDENTICAL_TAIL_FLUSH` order stream verdict.
