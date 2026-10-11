## Resume

Next: report this exact-SHA tooling/no-launch result to lead and wait for a separate launch go-ahead. Current branch: `codex/ab-campaign-tooling`. No games were launched. Real campaign manifest, runtime launch orchestration, A5 parity, engine acceptance/symmetric spawn proof, live RSS/free-RAM watchdog and PID cleanup remain open gates.

## Result

Implemented amendment-specific planner and evidence adjudicator for the 64-game BU pilot: 16 setup/faction-mirror configurations, two matched seed pairs per setup, explicit seat/spawn rotation, exact eight map package hashes, seat-count validation from archived map.yaml, and strict natural/cap/incomplete/invalid classification. The no-launch CLI renders existing batch map variants, verifies every planned PlayerReference/HomeLocation for both side orientations, and now materializes control/treatment AI YAML payloads in isolated non-runnable directories; the treatment is constrained to `BU_harvester_logistics` and must match the independently calculated expected payload SHA. Added deterministic paired bootstrap intervals for natural outcomes, per-seat 5,000-tick economy snapshots, and campaign-event counts/first-tick summaries; censored/incomplete cells remain outside win intervals. Added ratchet baseline model, predeclared acceptance policy (16/32/48/64 rounds), five-accept regression cadence and bounded small/large-game slot policy, including a SQLite-backed process-shared lease counter with expiry/heartbeat APIs. `SWITCH_ORDER_2026-10-11.md` classifies all 60 groups (9 proposed fix/safety; 51 behavior impact order). Fixture dry run estimates 11.07 worker-hours serial, 7.29 wall-hours at 3 concurrent small-game slots, with 5–12 min/game producing 5.11–9.47 wall-hours; 0 launches.

## Open gates

- No actual frozen campaign manifest was supplied in the repository; the exercised manifest is explicitly fixture-only/non-executable.
- Map archives contain enough playable `MultiN` references, but engine acceptance and symmetric spawn proof remain unverified; dry run marks this pending.
- This changes the planner/analyzer and adds SQLite lease primitives, but no game launch orchestration exists. Distinct bot trees, generated map.yaml, per-process RSS sampling/8 GiB kill, continuous 6 GiB free-RAM admission, 45k cap, 3,000s wall/180s stall, PID-scoped cleanup, immutable receipts and resume-safe orchestration remain unimplemented. Run remains disabled.
- Amendment 2 ambiguities are recorded in fleet `OPEN_DECISIONS.md`: how 16/32/48/64 checkpoints interact with mandatory 32-pair/64-game pilot, what constitutes synergy, and whether the 6 GiB free-memory floor is continuous.
- A5 parity and broader fixture/negative-control gates remain before any launch; maintainer requires pilot-only lead go-ahead before switch 1.

## Amendment 2 map inventory (read-only)

All eight named packages exist at the pinned `700bb16483f6d92664153e98d6f2560c312acaab` tree and match the hashes in `ab_campaign_pilot.py`: A Nuclear Winter (`_ra_a-nuclear-winter.oramap`, 2 playable seats/2 mpspawn); Satan's Clutch (`SatansClutch.oramap`, 2/2); Red Spice (`Red_Spice_2v2_BI-4.4.oramap`, 4/4); Terra Cotta (`Terracotta-ratls2.oramap`, 4/4); Back to Basics (`back-to-basics.oramap`, 6/6); Winter's End (Rich) (`winters-end-rich.oramap`, 6/6); Great Sahara 2 (`Great_Sahara_3.oramap`, map.yaml title is Great Sahara 2, 8/8); Ice Cold (`ice_cold.oramap`, 8/8). Missing maps: none. Counts came from each archive's map.yaml PlayerReference@MultiN and actor mpspawn entries. Symmetric spawn pairing and engine acceptance are still unverified and remain hard preflight gates; counts alone do not prove symmetry.

## Validation

`py -3 -m unittest tools.tests.test_ab_campaign_pilot -v`: 10 passed, including isolated arm-payload hash/delta checks and refusal to reuse an existing destination, slot-policy tamper rejection, generated mixed-faction 4v4 seat proof, and paired-effect/economy/event analyzer fixtures. `py_compile` and `git diff --check` passed. Fixture-only dry run: 64 games, 32 generated map variants/144 proved seats, treatment delta 1 field, 0 launches; manifest SHA-256 `d47a1a9c64bd0dda2daa68c444d0051fc638c98242d6c394a516369c98fabe19`; source pin and `pt7-control/engine` hashes verified. Estimate: 11.07 worker-hours; 7.29 slot-wall hours at 3 small-game workers; 5.11-9.47 h range. Full tests directory intentionally not run per watchdog rule.
