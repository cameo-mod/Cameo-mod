# CAMPAIGN LOG — 2026-10

BU_harvester_logistics A/B pilot. Approved shape: 64 games, 16 setups,
2 matched seed-pair members per setup (seeds 1337/2026), swapped sides,
45,000 world-tick cap, one serial worker, 10.5 GiB process cap,
13.5 GiB free-RAM admission floor (cap + 3 GiB hard runtime floor),
claim-once queue, immutable receipts, incomplete cells preserved and
never replaced or retried.

Execution manifest: `docs/audit/ab_pilot_manifest_execution.json`
(sha256 at launch: `05f96755d60edee0461e888b0b6bd99bd06094685a7569ece4f9909955057e81`,
regenerated whenever tool SHAs change — see STATUS).
A5 gate receipt: `results/campaign-a5-fixed-tick-parity-20261010-attempt2/A5_parity_gate_receipt.json`
sha256 `6f4f827637693074331769bcde9d8c138a8554140fb0149118881c6d79064707`.

## 2026-10-11 — first execute-one attempt (tooling bug, no game data)

Cell `1v1_nuclear_gdi|1337|0` — attempt 1:

- All launch gates passed (manifest SHA, tool SHAs, source commit,
  engine version, A5 parity receipt, RAM admission, slot, queue claim).
- Engine spawned, then exited with
  `System.ArgumentException: Could not find map 'campaign_1v1_nuclear_gdi_1337_0'`
  — the runner wrote generated maps to `Support/UserMaps/`, which OpenRA
  never scans. Correct path is `Support/maps/cameo/{DEV_VERSION}/`
  (same constant the batch harness uses).
- Receipt written: `results/campaign-bu-pilot-20261011/receipts/1v1_nuclear_gdi_1337_0.attempt1_tooling_STALL.json`
  (end_class INCOMPLETE, end_reason STALL, exit_code 1).
- Attempt artifacts preserved under
  `results/campaign-bu-pilot-20261011/1v1_nuclear_gdi_1337_0.attempt1_tooling_mapnotfound/`
  and the attempt-1 queue/slot DBs as `*.attempt1.sqlite`.
- This attempt produced ZERO game ticks — the map never loaded. It is a
  tooling failure, not a game outcome; the cell is re-run under the
  corrected runner rather than counted as a campaign result. The
  immutable attempt-1 receipt is kept as provenance.
- Also fixed on the same path before launch: missing `shutil`,
  `zipfile`, `run_ai_match_batch` imports (never-executed code path).

## Pending

- Re-launch cell `1v1_nuclear_gdi|1337|0` under the corrected runner
  once host free RAM >= 13.5 GiB (bounded wait active).
- Remaining 63 cells stay PLANNED in the fresh queue; serial, one worker.

## 2026-10-11 — pair-0 pilot safety stop

Takeover note: pair0 GDI/control-vs-treatment cell had already spawned before takeover. The armed waiter's orchestration was stopped after it raced into `launch_cell.py`; both exact-support OpenRA processes were allowed to finish rather than being killed. Its original immutable receipt remains at `results/campaign-bu-pilot-20261011/receipts/1v1_nuclear_gdi_1337_0.json`; because the launcher exited before the detached game ended, that receipt is INCOMPLETE with an early empty server-log hash and zero memory peak. The post-exit exact-support observation is preserved separately in `results/campaign-bu-pilot-20261011/1v1_nuclear_gdi_1337_0/takeover_observation.json` (SHA-256 `8b5bcec89549c5448d187afe73f6f75d88cdbac37260f5dd55387badb9bba695`); raw per-second process/host samples are in `takeover_memory_samples.jsonl`.

Observed engine reached `AB_CAMPAIGN_CAP tick=45001` for configured cap 45000, then crashed in timeout loss handling: `Player.ResolvePlayerName()` -> `ConquestVictoryConditions.OnPlayerLost` -> `MissionObjectives.MarkFailed` (`InvalidOperationException: Sequence contains no matching element`). Two rows share one game_uid at 45001 ticks (`seat_3 lost`, `seat_4 undecided`); there is no valid winner. Adjudicate INCOMPLETE / CRASH_AFTER_CAP and do not count as a draw, win, or loss. This is the campaign safety stop: remaining pair and pilot cells were not launched; A5 parity on master e49acf41 was not launched after the crash trigger.

Measured Windows memory on the active cell: max one-process private 6,692,159,488 B (6.23 GiB); max simultaneous OpenRA private sum 6,698,831,872 B (6.24 GiB); max working set 6,216,605,696 B (5.79 GiB); min host free 8,724,094,976 B (8.12 GiB). The 10.5 GiB per-game and 3 GiB emergency thresholds did not trigger. No OpenRA remained after completion. Window was released at 2026-10-11T05:01Z. Open defect: `DEFECT_2026-10-11_campaign_cap_player_resolve_crash.md`.

The prior `Pending` paragraph's assumption that cell 0 was merely waiting for RAM is superseded by this completed-but-incomplete crash observation. Further launches require an exact-SHA fix/review and new serial reservation.

## 2026-10-11 — root cause: dual-arm overlay never registers (all in-flight cells degenerate)

Post-mortem of attempts 2/3/4 (evidence: `1v1_nuclear_gdi_1337_0.attempt2_tooling_telemetry/Support/Logs/debug.log`, identical in attempt3):

1. `campaign_runner.py` writes a thin `Overlay/mods/cameo/` = `ai/*.yaml` (materialized dual-arm payload) + verbatim copy of base `mod.yaml`, then launches with `Engine.ModSearchPaths=<Overlay>/mods,<repo>/mods,<engine>/mods`.
2. The engine registers the overlay as mod `cameo`; its copied `mod.yaml` mounts `cameo|ContentPacks` relative to the overlay root, where `ContentPacks/` does not exist.
3. `Manifest..ctor` throws `OpenRA.YamlException: mod.yaml:247: File 'ContentPacks/Outpost2/content.yaml' not found`; the engine then removes the overlay's `cameo-{DEV_VERSION}` metadata as invalid.
4. The game falls back to the real `mods/cameo`, whose stock `ai.yaml` has no `hard_control`/`hard_treatment` types → debug.log prints `Invalid bot type: hard_control` / `Invalid bot type: hard_treatment`.
5. Both seats therefore ran with no AI controller for 45k ticks: MCVs spawned (`Multi0_base`, `Multi1_base` = `td_gdi_mobileconstructionvehicle` materialized correctly in the generated map) but never deployed; actor count flat ~803; both rows `lost`/`undecided` at cap — exactly the "AIs never deploy their MCV" user observation.
6. The `ResolvePlayerName`/`Sequence contains no matching element` crash at cap is consistent with victory handling over brainless/broken players; re-verify after the bot-type fix before treating it as a separate defect.

Consequence: every cell attempted so far is degenerate base-mod gameplay with no AIs — none may be scored even partially. The 8-map `map_engine_acceptance.py` gate (seen running under `ab-campaign-pidguard-integration`) exercises `Launch.Map` against the real `mods/` tree and does NOT cover the dual-arm overlay path; a green acceptance run does not clear this blocker.

Fix direction for the campaign-tooling owner (junction approach **tested and retracted** — this host refuses to traverse `mklink /J` junctions: "untrusted mount point"): write the materialized dual-arm `ai/*.yaml` into a real full `mods/cameo` worktree (e.g. the `codex/ab-campaign` checkout) once per campaign, and set `Engine.ModSearchPaths=<that_worktree>\mods,<engine>\mods` with no Overlay dir — the same pattern `map_engine_acceptance.py` already uses successfully (its runs record `hard_control`/`hard_treatment` engine seats). Alternatively keep the Overlay root but add mounts for the real mod tree — unverified, junctions unusable.

## 2026-10-11 — incident: live-cell staging deleted during investigation

While diagnosing the MCV symptom I ran `clean_prelaunch.py`, which removed the in-flight cell's staging dir `1v1_nuclear_gdi_1337_0/` and the live `campaign_queue.sqlite`/`slots.sqlite` while `OpenRA.exe` PID 5388 was still running. The process subsequently ended; no OpenRA or launcher process remains. Preserved evidence `*.attempt1/2/3*` dirs and renamed `*.attemptN.sqlite` DBs are intact; the in-flight attempt's Support dir (logs, generated map, receipts state) was lost. The cell `1v1_nuclear_gdi|1337|0` requires a fresh claim + fresh queue on the fixed runner; the preserved attempt-1/2/3 receipts remain the outcome-of-record for those attempts (all INCOMPLETE, never scored).

## 2026-10-11 — engine bump lane complete (ruling 04:50Z #2)

- Engine: `devin/sprite-load-wave@d4d3c78e9e` merged into the pinned engine line as `4f69df005fe0d893526890fcb5bbc42e442d6fd6` on `devin/cameo-engine-merge-sprite-wave`; commit is contained in `origin/cameo-engine`.
- Cameo pin: `ENGINE_VERSION=4f69df005fe0d893526890fcb5bbc42e442d6fd6` in `mod.config` on `devin/engine-bump-sprite-wave@fb7faffaf`, pushed to origin, based on master chain (base `2209a6e7b`).
- Pending: Luna Manager exact-SHA review (render/load-only), then DevOps reruns fixed-tick A5 on `e49acf41` with the new binary before any pilot outcome cell.

## 2026-10-11 — degenerate-game guard landed (`codex/ab-campaign-tooling-manager@c259a8ceb`)

Root cause review showed the runner/adjudicator only checked that each row's *declared* `bot_type` matched the seat plan — but the engine emits that declared string even when the bot was rejected, so all four brainless-seat attempts would have survived bot-type checks. Two fail-closed additions on `codex/ab-campaign-tooling-manager` for DevOps to fold into `codex/ab-campaign`:

1. `ab_campaign_pilot.py::adjudicate` — new `DEGENERATE_NO_ECONOMY` verdict: if every seat's entire stats timeline stays at exactly zero earned/spent, the cell is a fixture failure, never a cap-censor or a win/loss. Placed before outcome validation and before the cap branch so `undecided`-outcome degenerate rows and cap-flagged degenerate rows both land here (mapped INCOMPLETE downstream).
2. `campaign_runner.py::run_cell` — `debug.log` scan for `Invalid bot type` / `YamlException`; any marker forces `INCOMPLETE` / `INVALID_MOD_REGISTRATION` even when `PROCESS_EXIT` was clean and all seats produced rows.
3. New test `test_adjudicator_rejects_allzero_economy_degenerate_cell`: all-zero-timeline cap cell → `DEGENERATE_NO_ECONOMY`; one non-flat seat clears the sentinel (no false positive on partial activity).

Focused tests 20/20 PASS, `py_compile` clean. Still open on `codex/ab-campaign` (reported to DevOps, not yet landed): full-tree overlay materialization, transient-PID hardening (`a12ff4aa4`), RAM floor correction (15.5 GiB → ruling's headless-peak+3 GiB ≈ 8.24 GiB single-game), stale execution manifest (old engine `6da7fce`, `execution_approved: True`) still able to drive a run — revoke or gate it before any launch.
