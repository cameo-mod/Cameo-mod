## Resume (2026-10-11 evening, canonical ff + rebuilt pinned set — pre-A5 review pending)

Current branch: `codex/ab-campaign-port-fixes@e0c17eaef` (pushed). Now on the Luna-approved
`eaedd3e3b` (early-end fix `3d186c823` NATURAL-requires-authoritative-end_tick + analyzer fix
`6c1105ab9` + canonical fixtures) plus `d86841f20` (A5 probe emits the execution-gate receipt
schema: render_mode/process_private_cap_bytes/host_emergency_floor_bytes, per-run
stop_threshold_bytes=PRIVATE_LIMIT with the armed bound kept as diagnostic_stop_threshold_bytes,
3-binary sha set incl. OpenRA.Game.dll, orchestration_sha256 provenance, strict match_game_uid).

Pinned binary set REBUILT + boot-gated (builds are nondeterministic — Luna's hash mismatch was a
different local build in the raw `engine-ad061` engine checkout; the pinned set is what runs):
`C:\cameo-wt\ab-campaign-port\engine\bin` — OpenRA.exe `1a3a1fcbb581181fe7b572fab0f95ed1c7325a47f46bfa52226b1242ff1c2c27`,
OpenRA.Game.dll `ec2d2d79b00be5d74351da0e629bc40ea02e3acfb3607e5928324e2ec4d0d835`,
OpenRA.Mods.Cameo.dll `d07d4761ca6ec5b776ba0843f444fb5e163a4e0d936bc4cd2a1c2fae066e8619`.
engine/VERSION `ad061ebf79029da4dcd3a841a9b2540e39cbf968`. Boot-gate PASS on these bytes.

Manifests regenerated via the c38e864 generator only: candidate sha
`e1320e468e6c0cbb8b06f76e6aa09a55f79db495d72370fec36f57cbe9239cd2`, execution sha
`6d7986b4a6abc96c0db98662c6bf4e4b92a6dc2c07a10b6679c1563475e6a220` — source e49acf41, engine
ad061ebf79, min_setup_veto_sample=4, slot policy per ruling, a5 fields = explicit null
placeholder, execution_approved=false. No-launch dry-run PASS: 64 games/32 maps/1 treatment
change/0 launches (receipt `results/dryrun-eaedd3e3b.json`). Focused suite 109 tests green (1
skip = pinned-A5 test, expected while the receipt is pending).

A5 is launch-ready pending the HEAVY_RUN_WINDOW grant: detached worktree
`C:\cameo-wt\a5-e49-ad061` at `e49acf41a` (HEAD==BASELINE, scoped paths clean) + the pinned
engine copied in + probe/libs overlaid at `tools/ai_overlay/` (committed-at-pin tools/ai
untouched — batch runner and order differ execute from the pinned tree; the receipt records
every orchestration sha). All probe gate preconditions verified incl. admission at
7.5/3.25 GiB stops (needs 11.75 GiB; ~13.4 free). Runs `--process-stop-gib 7.5
--host-soft-stop-gib 3.25 --tick-cap 16000`, two serial same-seed 1v1 baseline cells.

## Resume (2026-10-11, four-branch consolidation + landed engine pin)

Current branch: `codex/ab-campaign`; tested source/tree SHA `1bd7751fbcf7ec947140dd6d4cfe0078bf0d7831`. Consolidated runner-final, engine-maproot-1011, tooling-manager, and ccr-c7f51935, initially based on master `2209a6e7`. Engine-bump commit `fb7faffaf` has since landed on master; this branch merges it and pins `mod.config` to engine `4f69df005fe0d893526890fcb5bbc42e442d6fd6`. ccr's `tools/ai/map_engine_acceptance.py` remains an acceptance-only preflight; its capped records are never campaign outcomes.

Policy follows the latest lead ruling: two concurrent 1v1/2v2 cells only when free RAM is at least 14 GiB; per-process stop is measured headless peak + 3 GiB. Current provisional value uses ccr's reported Linux peak RSS of 5,623,365,632 bytes, giving an 8,844,591,104-byte process ceiling. This is not a Windows private-byte measurement: run headless memory and map acceptance on the final engine before execution approval. Candidate engine pin is updated, but final Windows binary, A5 receipt, and host measurement hashes remain stale; manifest stays `execution_approved=false`.

A5 qualification accepts `IDENTICAL` and maintainer-approved `IDENTICAL_TAIL_FLUSH`. No outcome cells are authorized until a fresh exact-baseline A5 pair passes on the bumped engine. Focused tests: 43/43 PASS across campaign runner, pilot, A5 probe, map acceptance, and analyzer; `py_compile`, manifest validation, and `git diff --check` PASS. Windows build and host-specific map acceptance/headless-memory runs remain open because this mod worktree has no engine checkout/binaries.

Candidate manifest text SHA256 (non-executable): `6daf4223c9c3f3337ec5bb386b6063ceda95032ef263edd656329116457824e9`. Next: push `codex/ab-campaign` for exact-SHA review. Then use the final engine checkout to build, run focused tests, map acceptance and headless memory here; regenerate all pins, run fresh A5, and only then re-evaluate the launch gate.

## Resume (2026-10-10 late, Claude: launch blockers fixed + runtime acceptance; branch `ccr-c7f51935-i53fkj`)

Maintainer directive: fix what stopped the local agents from running the A/B. A capped REAL-engine run of the pilot path
found that **no campaign cell could have produced a valid game**. It also fixed the memory blocker. Committed on the
branch above (boot gate: default platform and `Game.Platform=Null` both reached the menu, 0 exceptions). Not merged into master, and `execution_approved` is still false.

**Blockers fixed (details: `docs/LESSONS_LEARNED.md` "A runner that never ran a cell", master doc Part IIIc RR2):**
0. **The arms never ran.** `campaign_runner` passed the dual-arm `ai.yaml` as a search-path overlay, but the engine
   keeps the LAST `mods/cameo` it finds. `debug.log`: `Invalid bot type: hard_control / hard_treatment`, so every
   seat idled. The cap Lua then crashed (`ResolvePlayerName`) and left one seat `undecided` (adjudicated INVALID). Now
   the arms ride in the generated map: `ab_campaign_pilot.install_dual_arm_rules` writes `campaign_ai.yaml` first
   in `Rules:`.
1. `campaign_runner.py` imports: `shutil`/`zipfile`/`batch` were missing (crash before launch / before the receipt).
   Guard: `tools/tests/test_campaign_tools_names.py`.
2. Variant map dir: `Support/UserMaps` is not a MapFolder, so `Could not find map`. Now `batch.USER_MAP_DIR`.
3. Cap and actor markers are in `lua.log`, not `server.log` (the seed pin stays in `server.log`).
4. **The pair never swapped the arms' spawns** (`make_jobs` flipped twice). Control held `Multi0..n-1` in both games.
   Fixed; the test checks per-arm spawn sets. `SPAWN_SYMMETRY_2026-10-11.md` carries a correction note.
5. Text pins are line-ending independent (`text_sha`). The old pins were CRLF bytes from the Windows checkout and could
   never validate on an LF host.
6. `ab_campaign.py` map path case (`_ra_Siberian-Pass.oramap`).
7. `BP_squad_desire` effect class (`restraint`). The applier had refused every `--groups` run since `a55954c1`.
8. `run_ai_match_batch.py` refuses a stale build (a `.cs` newer than its `engine/bin` dll; `--allow-stale-build`).

**Memory:** the 8.4–8.8 GiB peaks are sprite-sheet textures (200+ 2048² sheets, ≥ 3.2 GiB), not the managed heap.
New mod project `OpenRA.Platforms.Null` (in `CameoMod.sln`, so `make.cmd all` builds it); select it with
`--render headless` on `run_ai_match_batch.py` and `campaign_runner.py execute-one`. Linux measurements: peak 8.62 → 5.19 GB,
in game 5.1 → 2.6 GB, ~100 ticks/s. A same-seed rendered/headless pair matched on all 2,673 shared frames (orders
and sync hashes). **The memory policy constants are unchanged:** first measure headless private bytes on THIS host
(one `run_ai_match_batch.py --render headless` 1v1, sample `Get-Process OpenRA`), then the lead decides whether
`PRIVATE_LIMIT` / `FREE_RAM_FLOOR` can drop to admit two cells.

**Runtime acceptance (`tools/ai/map_engine_acceptance.py`, headless, cap 1,500 ticks, seed 1337):** **32 of 32 cells and 16 of 16 pairs ACCEPTED on all eight maps**: arms active, 144 of 144 seats `lost` at the cap, every seat on its planned spawn, and each pair swaps the arms between spawn sets. Headless peak 4.76–5.24 GB, flat from 2 to 8 seats. Receipt `docs/audit/map_engine_acceptance_2026-10-10.json`, summary `docs/audit/MAP_ENGINE_ACCEPTANCE_2026-10-10.md` (Linux host; re-run on this host before sign-off).

**Candidate manifest regenerated** (text pins only; design fields and Windows binary pins untouched):
`docs/audit/ab_pilot_manifest_candidate.json` sha256 `126d57ec8219fbdc9a3aef7c20d2562953858ff5cd01706167867c50a351c0f7`. The pilot and runner dry-runs are
green on Linux (64 games / 32 variants / 144 seats / 0 launches). `tools/tests/fixtures/ab_campaign_pilot_dry_run_receipt.json`
was refreshed. The binary pins are this host's build, and a rebuild (now including `OpenRA.Platforms.Null.dll`, which is
not pinned) should not change them: no engine or `OpenRA.Mods.Cameo` source changed.

**Next on the Windows host, in order:** (1) `git pull`, `make.cmd all` (builds `OpenRA.Platforms.Null`); (2) campaign
suites green (`python -m unittest tools.tests.test_ab_campaign_pilot tools.tests.test_campaign_runner
tools.tests.test_map_engine_acceptance tools.tests.test_campaign_tools_names tools.tests.test_ai_batch_harness`);
(3) `python tools/ai/map_engine_acceptance.py --manifest docs/audit/ab_pilot_manifest_candidate.json --out
results/map-acceptance-<date>` on this host (all 16 setups, both games; ~1 h headless) and keep the receipt;
(4) one headless 1v1 to measure private bytes, then the lead's memory-policy call; (5) DevOps sign-off of the
regenerated manifest; (6) the maintainer's `execution_approved` flip. Still pending as before: the receipt/analyzer
sign-off and the maintainer flip. A5 parity stands (it never used the overlay: `hard` bots on the gate map).

## Resume (2026-10-11, seat-join defect FIXED + memory policy implemented — uncommitted, DevOps review pending)

Two launch blockers cleared in this tree (uncommitted; user directive "fix all blockers", DevOps to review+commit):

1. **Seat-join defect FIXED** (`DEFECT_2026-10-11_seat_join_key.md` in main repo): real `cameo-ai-matches.jsonl` records carry `player.home` as the spawn coordinate (`"22,16"`), not the PlayerReference name (`"Multi0"`). `ab_campaign_pilot.py` gained a `byloc` map keyed on `seat["home_location"]`; the record join is `byloc.get(home) or byhome.get(home)` (byhome stays name-keyed for the `expected_seats` check). `campaign_runner.py` looks records up via `seat.get("home_location") or seat["home"]`. Before the fix every real record adjudicated `INVALID_UNKNOWN`; after: `TREATMENT_WIN` on real-shape records and name-form fixtures alike. Permanent negative-control `test_adjudicator_joins_real_coordinate_home_records` added. Fail-closed preserved.

2. **Memory policy implemented** per DevOps relay (10.5 GiB cap, ≥15.5 GiB admission, 4 GiB soft / 3 GiB hard floors): `PROCESS_CAP_BYTES`/`PRIVATE_LIMIT`=10.5 GiB, `RAM_FLOOR_BYTES`/`FREE_RAM_FLOOR`=15.5 GiB (admission — second cell cannot admit below it, enforcing one-worker on this host), new `FREE_RAM_HARD_FLOOR`=3 GiB (runtime `LOW_FREE_RAM` stop), `FREE_RAM_SOFT_FLOOR`=4 GiB (surfaced in `plan_summary`). Receipt schema `limit_private_bytes` const → 11274289152. Test constants updated.

3. **Stale harness test fixed**: `test_oramap_variant_converts_multi_slots` expected `Rules: rules.yaml`; the harness intentionally ships rules as `duel_rules.yaml` (reserved name, `8bd5bb1e1`). Test now reads `batch.HARNESS_RULES_NAME`.

Verification: campaign suite **136/136 pass** (pilot 11, runner, A5 probe, harness 36, ab_campaign/increment/summary, replay_health, aggregate, league). Candidate manifest regenerated: `docs/audit/ab_pilot_manifest_candidate.json` sha256 `c60e464127201e08bcdbacf0b6481e1d2934bff1292ec033f17133ccf8ae6f8d` (tool_sha256 + slot_policy current). Full dry-run green: 64 games, 32 maps, 0 launches. Fixtures refreshed.

Remaining launch gates (unchanged): exact-SHA manifest/analyzer integration sign-off by DevOps, 8-map engine acceptance + symmetric spawn proof, maintainer `execution_approved` flip. No outcome cells launched.

## Resume (2026-10-10 19:02Z, A5 fixed-tick parity PASS)
A5 fixed-tick qualification PASS at commit `bcdd89e5451f4be8a66add9d506392adb5e17e52`, exact baseline ancestry `964cdb630b1514e1c1a0baed55cbdbc427d5fc11`, engine `6da7fce14da541180c6baddd6925118fbef65b94`, seed 1337, same runtime fingerprint `3d90631faa76`. Both runs verified Lua cap marker tick16001, complete two-seat mutual-loss CENSORED_CAP rows, and exactly one complete replay each; strict comparator returned `IDENTICAL_TAIL_FLUSH` (same shared per-frame orders/hashes, terminal sync-only flush). Durations 16601/16640; do not score either censored result. Peak private 8.55/8.53 GiB; host free at finish14.98 GiB; zero OpenRA processes.
Receipt and all raw evidence: `results/campaign-a5-fixed-tick-parity-20261010-attempt2/A5_memory_probe_receipt.json`, `order_stream_diff.json`, run receipts/logs/replays. The prior failed preflight attempt remains preserved separately as INCOMPLETE_PREFLIGHT_REJECTION.
The A5 same-seed gameplay prefix gate is now closed for the exact capped diagnostic. Campaign outcomes are still blocked on map engine acceptance/spawn assignment proof, analyzer fixtures/joins, approved executable manifest, and RAM/slot preflight. Current campaign setting remains 45,000 tick cap; no outcome cells launched.## Resume (2026-10-10 18:57Z, fixed-tick attempt 1 incomplete)
Attempt 1 of fixed-tick A5 did not start OpenRA: the batch driver refused a copied temporary directory map before any server log/PID; output is preserved at `results/campaign-a5-fixed-tick-parity-20261010` and classified INCOMPLETE_PREFLIGHT_REJECTION, not a match. Its reservation is released; do not overwrite or count it.
Root cause was an intentional directory-map restriction. Added explicit `--allow-temporary-template` opt-in with fixed-layout/player/rules validation; only the A5 diagnostic uses it. Focused validation is now 21/21 PASS, py_compile and diff-check PASS.
Next: push the harness fix, reserve a new no-retry serial A5 pair in a new output directory, verify capped mutual-loss records plus the Lua cap marker and strict IDENTICAL replay streams. Campaign outcome cells remain blocked until that proof and remaining map/analyzer/manifest gates pass.## Resume (2026-10-10 18:52Z, fixed-tick A5 parity rerun staged)
Next: commit and push the diagnostic harness, reserve one serial slot, then rerun same-seed A5 with an explicit 16,000-tick all-seat cap; require complete censored-cap receipts and strict replay identity before treating A5 as passed. This is qualification only, no campaign outcome cells.
Reason: exact-baseline natural pair peaks 8.43/8.84 GiB during load then falls; strict replay differs only at terminal boundary but same outcome and records. Reviewer says strict comparator stays DIVERGENT, so use a pre-registered common fixed-tick comparison instead of relaxing the verdict.
Harness now injects the normal campaign cap monitor into a temporary copy of the A5 map; cap marker and mutual-loss records are verified separately. Focused tests: 20/20 PASS; Python compilation PASS; no-launch preflight reports cap 16000, 9.5GiB PID stop, 4GiB host soft, 3GiB hard, launches=0.
If the 16k pair is IDENTICAL, A5 parity only is closed; campaign still requires eight-map engine acceptance/spawn proof, receipt/analyzer fixture gates and manifest/slot checks. Current cap remains 45,000 for campaign games and the host lane stays serial.## Resume (2026-10-10, A5 memory and replay review)
Next: hold campaign outcome cells until lead resolves whether the reviewed end-boundary pair qualifies for A5 parity; if accepted, metrics need a common fixed-tick/outcome-tick endpoint. Do not relaunch based only on the memory result.
A5 pair: same exact source lineage (964cdb baseline), engine, binary hashes, map hash, seed 1337, fingerprint and seats; both natural completions. Peaks 8.43/8.84 GiB private, then memory declined during 17k+ ticks; host minima 6.74/5.83 GiB. GC dump found ~855 MB live managed at 4.885 GiB private; no in-game leak indicated. Sol/Devin analysis: native/runtime/load-phase envelope dominates, about 9 GiB at WT0.
Replay review: all orders and sync hashes identical through frame 5809; diffs are six sync hashes plus one RepairBuilding order in the end tail; end ticks differ by 20 and terminal metrics vary. Strict comparator remains DIVERGENT. Proposed ADJUDICATED_END_BOUNDARY is unapproved; no campaign A/B outcomes launched.
Evidence: `results/campaign-a5-parity-20261010-user12gb/` plus fleet `ANALYSIS_2026-10-10_a5_parity_divergent_tail_devin.md` and `ANALYSIS_2026-10-10_a5_gcdump_native_share_devin.md`. User-authorized host hard reserve is 3 GiB; keep execution serial, retain dual host/process stop guards and at least a 10.5 GiB process cap unless updated by the lead.## Resume (2026-10-10 18:03Z)
Next: keep A/B launch blocked; investigate live-game memory growth in the preserved 18:01Z A5 diagnostic after the runner kill-path fix. Do not rerun until instrumentation can sample before the 6.5 GiB ceiling and a reviewed memory hypothesis is ready.
Current branch HEAD: a3ce97dc4fb8ba45b40c262fbff094d230538550 (pushed). The missing `subprocess` import and identity-scoped kill regression are fixed; focused campaign-runner tests pass 8/8. The diagnostic reached ~8.29 GiB private bytes at WT ~3050, produced zero match records/replay, and the monitor crashed because `kill_owned_pid` referenced unimported `subprocess`; verified PIDs 23792/21796 were manually stopped by image+support identity. Incident receipt: `results/campaign-a5-memory-patch-20261010/A5_memory_probe_incident.json`.
Open gates: mitigation demonstrably under 6.5 GiB; exact-baseline A5 pair with seed/order/replay proof; map engine acceptance/spawn symmetry; approved execution manifest. Never bypass memory/parity gates or start campaign outcome cells.
## Resume (2026-10-10, bounded A5 qualification)
Next: reserve the exclusive campaign slot, then run `py -3 tools/ai/a5_memory_parity_probe.py --execute-a5-diagnostic`; it runs two serial same-seed baseline-vs-baseline A5 cells, logs per-process private-memory samples, stops at 6.25 GiB, checks server seed pins and order-stream identity, and never starts campaign outcome cells. Do not treat this diagnostic as campaign authorization or as an exact-964 binary proof: the compiled logger mitigation is commit 173038d.
Preflight: no OpenRA process; 15.4 GiB free RAM; engine VERSION is 6da7fce; map-template no-launch dry-run resolves the prior A5 map SHA c546ed9; probe `py_compile` and no-launch mode pass. Probe source must be committed first so its tooling fingerprint is clean.
Existing A5 evidence remains invalid parity: run-a 6.55 GiB, run-b 8.07 GiB; no campaign outcome cells have been launched. If the diagnostic exceeds its lower stop, preserve receipt and stop. If both stay below and order is identical, request reviewer/maintainer approval to update the runtime pin before campaign launch.

## Resume (2026-10-10, memory mitigation)
Next: reviewer/DevOps should inspect the commit, then rebuild the exact baseline and request approval for a bounded A5 parity rerun; no campaign launches until exact-baseline A5 is under 6.5 GiB with matching order streams.
Change: AiMatchLogWriter aggregates each sampled own-actor snapshot immediately into EnemyProfile and retains only aggregate truth per history sample, avoiding repeated per-actor snapshots in deferred history. Serialization uses the same ProfileOf aggregation.
Focused verification: with `-p:IsTestProject=true`, DeferredSignatureHistoryRetainsOnlyAggregatedTruthProfiles PASS 1/1; AiLogPrivacyRegressionTest PASS 9/9. The earlier `--no-restore` invocation silently skipped (unset IsTestProject); it is not counted.
A5 evidence remains run-a 6.55 GiB and run-b 8.07 GiB (invalid parity); artifacts preserved under `results/campaign-a5-20261010`. This mitigation has not been measured in a rerun and is not a passed memory gate.

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

## Resume (2026-10-10 18:04Z, memory cap review)
Sol's guidance: PT7 1v1 hard-vs-hard on the same map ran 45k ticks at bounded memory, so the rapid 8.29 GiB at WT~3050 is unlikely ordinary workload; the logger-build change is a key new variable. Next diagnostic should compare telemetry/logging behavior and sample private bytes, free RAM, actor count/types, managed GC/LOH, and log/replay/temp sizes every 1-2 seconds. No `dotnet-counters` CLI is installed on this host.
User authorized considering 12 GiB per instance, but current host has 31.93 GiB total / 15.42 GiB free. With the 6 GiB free-RAM reserve, a 12 GiB instance is not safe now (leaves 3.42 GiB); raw arithmetic ceiling is 9.42 GiB before headroom, so use at most an 8 GiB single-instance diagnostic unless free RAM rises. Do not launch campaign outcome cells; A5 has no valid match/replay/order proof.
Incident receipt remains `results/campaign-a5-memory-patch-20261010/A5_memory_probe_incident.json`; kill-path fix is in pushed commit `8f8a532b2898dc6928c4e04e159aed76b18c9e7e`.
## Resume (2026-10-10 18:13Z, instrumented A5 memory diagnostic)
Branch `codex/ab-campaign-tooling-manager` at `ea66c7b03531f3f3f4d12b6cc6e9476e3e6ead8e` (pushed). Updated `a5_memory_parity_probe.py` to sample at 1s: process private bytes + working set, host physical/commit headroom, System.Runtime CSV, world tick and log sizes; actor count remains null with an explicit note because current logs do not emit it. Process soft stop 6.25 GiB, host stop 7 GiB, 6 GiB hard floor; no 12 GiB escalation with current free memory.
Focused tests 12/12 pass; py_compile, no-launch invocation, diff check pass. Existing A5 evidence still invalid; no A/B outcome cells launched. Next: a single reserved diagnostic-only A5 probe pair, no retries, exact-support process kill, preserve output; stop on any failed sample/counter, process threshold or host threshold. Then report memory telemetry and gate state; campaign remains blocked pending exact-pin A5 parity/order, maps and manifest gates.

## Resume (2026-10-10 18:13Z, A5 telemetry diagnostic stopped)
Diagnostic run-01 stopped at the 6.25 GiB soft threshold; measured peak was 6.36 GiB private / 6.35 GiB working set, with host free physical moving 14.64 to 8.46 GiB. It produced zero match rows and no replay; run-02 was not launched, so A5 parity/order remains NOT PASSED and no campaign outcome cells ran.
System.Runtime CSV at stop: GC committed 5.09 GiB, last-collected LOH 2.39 GiB, Gen2 2.66 GiB, LOH fragmentation 0.94 GiB, allocation-rate peak 2.44 GiB/s. This points to substantial managed-heap/LOH activity but does not prove retention/root cause; world-tick and actor count were unavailable from current logs. Preserve exact evidence at `results/campaign-a5-memory-diagnostic-20261010-sol-advice`; ask Sol to interpret before any further probe.
The soft stop is lowered to 3 GiB because the sampled working set jumped from 3.41 to 5.12 GiB in one second; no repeat without a new reservation. 12 GiB remains unsafe with <19 GiB free; per-process campaign cap stays 6.5 GiB. Branch pushed at `b830d83766e73f8feb52da686332fcc596a41023`; focused tests 12/12 pass; no OpenRA processes remain.

## Resume (2026-10-10 18:18Z, GC burst trace preparation)
Sol's interpretation: managed-heap activity dominates the observed growth, with large allocation flow, survivor/old-generation storage and LOH fragmentation; no leak or owning subsystem is proven. Run-01 remains safely stopped/incomplete with zero match/replay. Exact telemetry lives under `results/campaign-a5-memory-diagnostic-20261010-sol-advice`.
Prepared `dotnet-trace gc-verbose` collection (32 MiB buffer, 10-second duration, max 64 MiB artifact) triggered at target private memory >=1 GiB; the probe's process soft stop is 3 GiB due the 1.8 GiB/s observed jump, with host soft/hard stops 7/6 GiB. The user's 3 GiB reserve authorization is the hard floor; stricter soft/launch headroom remain.
Next: reserve one serial diagnostic-only attempt, no retries, inspect collected allocation types/stacks; do not launch campaign outcome cells or change production limits. Branch will be pushed before the run; current host/process samples are freshly checked.

## Resume (2026-10-10 18:21Z, bounded GC trace complete)
Branch `codex/ab-campaign-tooling-manager`; source changes committed and pushed through `52e2b97b89046987ec7efa27be3cd5bad7ac9048`. GC-verbose diagnostic run `campaign-a5-gc-trace-20261010-sol-advice/run-01` exited fail-closed at 3 GiB soft limit (peak 3,276,107,776 private bytes); no matches, replay or retry. Trace captured PID 26588 at 1 GiB for 10s, 32 MiB buffer; original nettrace SHA-256 `385258a53f8abfb6924e0e51e31492eb9a88804cabaf995da55a9c38063afa47`; retain it. Speedscope conversion warned potentially broken after PID stop and yielded no usable samples, so allocation stacks remain UNKNOWN. Reservation released at 18:20:51Z; verified 0 OpenRA. A5 parity and campaign readiness remain NOT PASSED; no campaign outcome cells launched.
Next: inspect original trace with a supported PerfView/nettrace workflow or run another specifically reserved short trace with graceful trace stop before process stop; preserve host/process guards and 6.5 GiB campaign cap. Then request an exact-baseline, two-run A5 qualification only after memory evidence and parity/order artifact gates are valid. Do not bypass A5 by raising process limit; the observed 12 GiB allowance has insufficient documented host headroom. Analyzer/map findings remain separate gates.

## Resume (2026-10-10 18:25Z, A5 qualification window reserved)
Current pushed branch `codex/ab-campaign-tooling-manager@263c74b50`. A5 probe now supports explicit memory thresholds, 250ms PID sampling, user-authorized 3GiB host floor, and profiler-off same-config parity runs while retaining 1s System.Runtime counters. Focused test 5/5 PASS, py_compile and diff-check PASS; no-launch preflight computes 9.5GiB process stop / 4GiB soft / 3GiB hard / 250ms / no profiler.
A two-cell exact-source A5 parity window is reserved until 19:04:43Z. Launch admission requires at least 14.5GiB available physical, 11.5GiB commit, no OpenRA, clean new output directory. User authorized up to 12GiB but chosen process kill is 9.5GiB to retain host margin; any missed sample, host soft floor, process threshold, timeout, mismatch or failed artifact stops the pair with no retry. No campaign outcomes until this pair passes and manifest/map/analyzer gates are satisfied.

## Resume (2026-10-10 18:32Z, guarded A5 parity failed determinism)
Exact-baseline A5 diagnostic pair ran serially on seed1337, source `f9342f469dd758975be4e8dfdf2337a7bb2758d5`, engine `6da7fce14da541180c6baddd6925118fbef65b94`, identical fingerprint `340bf82c3df0`, same map hash and seed-pin line. Both cells completed naturally (17,448 / 17,428 ticks), records verified, one replay each; private peaks 8.43 / 8.84 GiB under the selected 9.5GiB PID stop. Host minimum free 6.74 / 5.83 GiB, so the user-approved 3GiB hard reserve held. No process remains.
Strict `order_stream_diff` FAILED at frame5810 due divergent SYNCHASH; a diagnostic `--ignore-synchash` comparison still found a real `RepairBuilding` order divergence at frame5816. Thus same-seed determinism/A5 parity remains NOT PASSED despite valid individual match artifacts. Do not run campaign outcome cells or silently replace this pair. Preserved full evidence in `results/campaign-a5-parity-20261010-user12gb/A5_memory_probe_receipt.json`, each per-run receipt/log/replay, plus `order_stream_diff.json`; profiler-off config retained and 1s System.Runtime counters captured.
Window released at 18:31:28Z. Next: isolate why identical fingerprint, map, binary, seed pin and seat setup produced an order divergence; seek a source/reviewer diagnosis before any new qualification pair. The probe's 9.5GiB diagnostic process cap is still provisional; prior campaign policy was6.5GiB and this pair is not approval to raise that for outcome runs. Continue to require maps/seat/manifest/analyzer gates. No campaign outcome cells launched.
