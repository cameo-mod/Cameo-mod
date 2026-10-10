## Resume

Next: lead review the amendment-2 update, resolve the round/checkpoint and synergy questions in fleet `OPEN_DECISIONS.md`, then implement the process-shared runtime driver before any launch. No launch backend is enabled, and this task performed zero launches. Current branch: `codex/ab-campaign-tooling`.

## Result

Implemented amendment-specific planner and evidence adjudicator for the 64-game BU pilot: 16 setup/faction-mirror configurations, two matched seed pairs per setup, explicit seat/spawn rotation, exact eight map package hashes, seat-count validation from archived map.yaml, and strict natural/cap/incomplete/invalid classification. Added deterministic paired bootstrap intervals for natural outcomes, per-seat 5,000-tick economy snapshots, and campaign-event counts/first-tick summaries; censored/incomplete cells remain outside win intervals. Added ratchet baseline model, predeclared acceptance policy (16/32/48/64 rounds), five-accept regression cadence and bounded small/large-game slot policy, including a SQLite-backed process-shared lease counter with expiry/heartbeat APIs. `SWITCH_ORDER_2026-10-11.md` classifies all 60 groups (9 proposed fix/safety; 51 behavior impact order). Fixture dry run estimates 11.07 worker-hours serial, 7.29 wall-hours at 3 concurrent small-game slots, with 5–12 min/game producing 5.11–9.47 wall-hours; 0 launches.

## Open gates

- No actual frozen campaign manifest was supplied in the repository; the exercised manifest is explicitly fixture-only/non-executable.
- Map archives contain enough playable `MultiN` references, but engine acceptance and symmetric spawn proof remain unverified; dry run marks this pending.
- This changes the planner/analyzer and adds SQLite lease primitives, but no game launch orchestration exists. Distinct bot trees, generated map.yaml, per-process RSS sampling/8 GiB kill, continuous 6 GiB free-RAM admission, 45k cap, 3,000s wall/180s stall, PID-scoped cleanup, immutable receipts and resume-safe orchestration remain unimplemented. Run remains disabled.
- Amendment 2 ambiguities are recorded in fleet `OPEN_DECISIONS.md`: how 16/32/48/64 checkpoints interact with mandatory 32-pair/64-game pilot, what constitutes synergy, and whether the 6 GiB free-memory floor is continuous.
- A5 parity and broader fixture/negative-control gates remain before any launch; maintainer requires pilot-only lead go-ahead before switch 1.

## Amendment 2 map inventory (read-only)

All eight named packages exist at the pinned `700bb16483f6d92664153e98d6f2560c312acaab` tree and match the hashes in `ab_campaign_pilot.py`: A Nuclear Winter (`_ra_a-nuclear-winter.oramap`, 2 playable seats/2 mpspawn); Satan's Clutch (`SatansClutch.oramap`, 2/2); Red Spice (`Red_Spice_2v2_BI-4.4.oramap`, 4/4); Terra Cotta (`Terracotta-ratls2.oramap`, 4/4); Back to Basics (`back-to-basics.oramap`, 6/6); Winter's End (Rich) (`winters-end-rich.oramap`, 6/6); Great Sahara 2 (`Great_Sahara_3.oramap`, map.yaml title is Great Sahara 2, 8/8); Ice Cold (`ice_cold.oramap`, 8/8). Missing maps: none. Counts came from each archive's map.yaml PlayerReference@MultiN and actor mpspawn entries. Symmetric spawn pairing and engine acceptance are still unverified and remain hard preflight gates; counts alone do not prove symmetry.

## Validation

`py -3 -m unittest tools.tests.test_ab_campaign_pilot -v`: 8 passed, including slot-policy tamper rejection and paired-effect/economy/event analyzer fixtures. `py_compile` and `git diff --check` passed. Fixture-only dry run: 64 games, 0 launches; manifest SHA-256 `6f859250d5d46c7cf4d865c14f840007bc9135d0fa2db125573bee3fdcda3ec1`; source pin and `pt7-control/engine` hashes verified. Estimate: 11.07 worker-hours; 7.29 slot-wall hours at 3 small-game workers; 5.11-9.47 h range. Full tests directory intentionally not run per watchdog rule.
