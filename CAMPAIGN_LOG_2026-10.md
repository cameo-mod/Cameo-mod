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
