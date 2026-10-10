## Resume

Next: lead review this exact SHA, resolve the map symmetric-spawn/engine acceptance gate, then freeze final source, engine, tools, and arm payloads. No launch backend is enabled, and this task performed zero launches. Current branch: `codex/ab-campaign-tooling`.

## Result

Implemented amendment-specific planner and evidence adjudicator for the 64-game BU pilot: 16 setup/faction-mirror configurations, two matched seed pairs per setup, side swaps, exact eight map package hashes, seat-count validation from archived map.yaml, and strict natural/cap/incomplete/invalid classification. Aggregates per-seat metrics to arm totals; explicitly reports synergy as not identifiable from this pilot because the team-size and individual setups lack matched context. Dry-run fixture receipt: `tools/tests/fixtures/ab_campaign_pilot_dry_run_receipt.json` (NO_LAUNCH, 64 games, estimated 11.07 serial worker hours).

## Open gates

- No actual frozen campaign manifest was supplied in the repository; the exercised manifest is explicitly fixture-only/non-executable.
- Map archives contain enough playable `MultiN` references, but engine acceptance and symmetric spawn proof remain unverified; dry run marks this pending.
- This changes planner/analyzer only. The runtime backend for distinct control/treatment bot profiles, map.yaml generation/proof, 45k cap, 3,000s wall/180s stall, 8 GiB process watchdog, PID-scoped cleanup, serial lock, and immutable per-cell receipts is not implemented. Run command remains disabled.
- A5 parity and broader fixture/negative-control gates remain before any launch; maintainer requires pilot-only lead go-ahead before switch 1.

## Amendment 2 map inventory (read-only)

All eight named packages exist at the pinned `700bb16483f6d92664153e98d6f2560c312acaab` tree and match the hashes in `ab_campaign_pilot.py`: A Nuclear Winter (`_ra_a-nuclear-winter.oramap`, 2 playable seats/2 mpspawn); Satan's Clutch (`SatansClutch.oramap`, 2/2); Red Spice (`Red_Spice_2v2_BI-4.4.oramap`, 4/4); Terra Cotta (`Terracotta-ratls2.oramap`, 4/4); Back to Basics (`back-to-basics.oramap`, 6/6); Winter's End (Rich) (`winters-end-rich.oramap`, 6/6); Great Sahara 2 (`Great_Sahara_3.oramap`, map.yaml title is Great Sahara 2, 8/8); Ice Cold (`ice_cold.oramap`, 8/8). Missing maps: none. Counts came from each archive's map.yaml PlayerReference@MultiN and actor mpspawn entries. Symmetric spawn pairing and engine acceptance are still unverified and remain hard preflight gates; counts alone do not prove symmetry.

## Validation

`py -3 -m unittest tools.tests.test_ab_campaign_pilot -v`: 5 passed. `py_compile` passed. No-launch dry run: 64 jobs, 0 launches. `git diff --check` passed.
