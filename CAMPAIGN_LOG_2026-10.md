# CAMPAIGN LOG — 2026-10

BU_harvester_logistics A/B pilot. Approved shape: 64 games, 16 setups,
2 matched seed-pair members per setup (seeds 1337/2026), swapped sides,
45,000 world-tick cap, 10.5 GiB process cap, admission at measured
headless peak (5.24 GiB) + 3 GiB reserve, >=14 GiB for a second parallel
small game, max 2 parallel small games, 3v3/4v4 exclusive (ruling 04:50Z),
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

## 2026-10-11 — consolidated-branch fix port (launch blockers, 06:30-07:10Z)

Devin Coordinator review of codex/ab-campaign@169388f97 found the
launch-blocking defects from the thin-overlay diagnosis plus an inverted
memory ruling. Ported as `codex/ab-campaign-port-fixes@59cb7295c`
(tooling-manager@{c259a8ceb,01b15da12,ddd16354d,c67e3ae30,4029a47d0}
applied onto the consolidated head, preserving its newer text_sha /
BOM-aware version read / geographic-home seat fix / 4f69df pin /
receipt schema):

- Inverted 04:50Z ruling corrected: 54d432b7c had set the per-process
  cap to HEADLESS_PEAK+3 GiB (~8.24 GiB) — a kill threshold below the
  documented ~9.5 GiB startup burst — while gating all admission at
  14 GiB. Restored: cap 10.5 GiB, admission = peak+3 (~8.24 GiB),
  second small game needs >=14 GiB, max 2 parallel, large exclusive.
- Thin Overlay -> per-cell FullMods materialization (hardlink clone of
  mods/cameo minus ai/, real files only). Fixes mod.yaml:247
  ContentPacks/Outpost2/content.yaml YamlException that dropped the arm
  bot registrations -> Invalid bot type -> brainless seats -> MCVs never
  deployed in every attempt so far.
- DEGENERATE_NO_ECONOMY adjudicator verdict + debug.log scan
  (Invalid bot type / YamlException) -> INCOMPLETE/
  INVALID_MOD_REGISTRATION; verified markers live in driver.log
  (cap/actor), server.log (seed pin), debug.log (registration).
- Windows-safe pid_alive, two-poll stable game-PID resolution,
  monitor_process watch_pid ordering, game PID (not launcher) in
  queue/receipt.
- Stale execution manifest on dead 6da7fce pin revoked.

Focused tests 24/24, py_compile clean, offline FullMods materialization
verified on this tree (33,472 linked entries; mod.yaml, ContentPacks,
hard_control/hard_treatment all resolve).

## Pending

- Merge `codex/ab-campaign-port-fixes` onto `codex/ab-campaign` (DevOps),
  then regenerate the execution manifest with final tool/engine SHAs.
- Luna exact-SHA review of engine merge `4f69df005f` / pin `fb7faffaf`.
- A5 fixed-tick parity rerun on source `e49acf41` + new engine binary.
- Re-launch cell `1v1_nuclear_gdi|1337|0` under the corrected runner
  once host free RAM >= ~8.24 GiB (admission floor, ruling 04:50Z).
  No bounded wait may be armed outside the reserved launch window.
- Remaining cells stay PLANNED; serial, one worker until the second-
  small-game parallel gate is exercised under observation.
