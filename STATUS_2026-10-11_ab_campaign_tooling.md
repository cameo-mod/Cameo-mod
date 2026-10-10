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

## Resume (2026-10-10 17:22Z, correction)

Current branch `codex/ab-campaign-tooling`, commit `69e0f5acb4e586c239ff6dca318b6ec7fb37c04`, pushed. The earlier statement that no A5 games had been launched is superseded: one A5 run-a was attempted under the reserved parity slot. Gameplay source `mods/cameo` has no diff against baseline 964, but the game failed before a match during ruleset initialization with `Cannot locate type: SquadDesireBotModuleInfo`; failure log: `results/campaign-a5-20261010/run-a/Logs/exception-2026-10-10T171521Z.log`. Classify it `INCOMPLETE_PREFLIGHT_FAILURE`; preserve all artifacts; no second A5 run or campaign outcome cells launched. No OpenRA process from this attempt remains.

Next: diagnose why the binary identified by `engine/VERSION=6da7fce14da541180c6baddd6925118fbef65b94` cannot load a trait required by exact-baseline mod sources. VERSION is descriptive and does not prove binary/source compatibility. Do not relax pin checks. After compatibility is proven, rerun both A5 seeds and verify order streams. Remaining gates: execution-approved manifest and engine acceptance plus symmetric-spawn proof for all eight maps. Current no-launch dry run remains 64 games / 32 variants / 144 seat proofs, estimated 7.29 slot-wall hours (5.11�9.47 range).

### A5 parity gate attempt (2026-10-10)

Three distinct A5 observations; the gate is **NOT PASSED**:

1. `run-a` failed before a match because the copied `engine/bin/OpenRA.Mods.Cameo.dll` was stale and lacked `SquadDesireBotModuleInfo`; exception is preserved at `results/campaign-a5-20261010/run-a/Logs/exception-2026-10-10T171521Z.log`.
2. After a serial `dotnet build OpenRA.Mods.Cameo/OpenRA.Mods.Cameo.csproj` (0 errors), `run-a-rebuilt` completed one 1v1 gate match with two mutual-lost records, no exception. Fingerprint: source tooling HEAD `69e0f5acb4e586c239ff6dca318b6ec7fb37c04`, engine VERSION `6da7fce14da541180c6baddd6925118fbef65b94`; baseline `mods/cameo` diff from 964 is empty. Manual sampling observed one OpenRA process at 6.55 GiB private bytes; exact peak is not recorded, and this exceeds the campaign's 6.5 GiB limit. Treat this run as safety-invalid for A5 parity evidence.
3. `run-b-rebuilt` was interrupted at a sample of 8.07 GiB private bytes, exceeding the 6.5 GiB campaign limit. PID 19040 was killed after verifying its command line referenced this exact support directory; the companion OpenRA PID 31080 and the same-run retry tree (PIDs 30136, 31996, 30788, then PID 10444) were also PID-scoped stopped by exact support-directory identity to prevent automatic retries. Logs/output remain under `results/campaign-a5-20261010/run-b-rebuilt`.

No replay parity comparison is valid; no exact-baseline A5 receipt was produced; no campaign outcome cell launched. Stop trigger: observed private-byte ceiling breach. Tooling must add/verify early enough sampling for A5, then exact binary/source build provenance and memory-safe A5 must be re-established before any campaign run. All OpenRA/A5 worker processes are now gone; the reserved slot was released in `HEAVY_RUN_WINDOW.txt`.

## Resume (handoff, 2026-10-10)
Branch `codex/ab-campaign-tooling-manager` includes map inventory, schema, guarded runner, fixtures/tests and A5 failure status; HEAD pinned below.
No-launch dry run: 64 games / 32 variants / 144 seat proofs; 0 launches; estimated 7.29 slot-wall hours (5.11�9.47 range).
A5 parity gate failed: initial stale-DLL load failure; rebuilt run-a exceeded 6.5 GiB; run-b hit 8.07 GiB and was PID-scoped stopped; campaign cells never started.
Preserved logs: `C:\cameo-wt\ab-campaign-tooling\results\campaign-a5-20261010`; schema: `C:\cameo-wt\ab-campaign-tooling\tools\ai\ab_campaign_receipt.schema.json`.
Next owner is Luna DevOps; do not run any campaign cells until memory-safe exact-baseline A5 parity and map/manifest gates pass.
