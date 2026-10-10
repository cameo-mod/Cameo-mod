## Resume

Next: lead review the amendment-2 update, resolve the round/checkpoint and synergy questions in fleet `OPEN_DECISIONS.md`, then implement the process-shared runtime driver before any launch. No launch backend is enabled, and this task performed zero launches. Current branch: `codex/ab-campaign-tooling`.

## Result

Implemented amendment-specific planner and evidence adjudicator for the 64-game BU pilot: 16 setup/faction-mirror configurations, two matched seed pairs per setup, explicit seat/spawn rotation, exact eight map package hashes, seat-count validation from archived map.yaml, and strict natural/cap/incomplete/invalid classification. Added ratchet baseline model, predeclared acceptance policy (16/32/48/64 rounds), five-accept regression cadence and bounded small/large-game slot policy. `SWITCH_ORDER_2026-10-11.md` classifies all 60 groups (9 proposed fix/safety; 51 behavior impact order). Fixture dry run estimates 11.07 worker-hours serial, 7.29 wall-hours at 3 concurrent small-game slots, with 5–12 min/game producing 5.11–9.47 wall-hours; 0 launches.

## Open gates

- No actual frozen campaign manifest was supplied in the repository; the exercised manifest is explicitly fixture-only/non-executable.
- Map archives contain enough playable `MultiN` references, but engine acceptance and symmetric spawn proof remain unverified; dry run marks this pending.
- This changes planner/analyzer and an in-process admission policy only. Runtime backend for distinct bot profiles, generated map.yaml, all-seat outcome/replay receipts, 45k cap, 3,000s wall/180s stall, hard 8 GiB process kill, live 6 GiB RAM floor, process-shared slot lock/expiry, PID cleanup, and resume-safe receipts is not implemented. Run command remains disabled.
- Amendment 2 ambiguities are recorded in fleet `OPEN_DECISIONS.md`: how 16/32/48/64 checkpoints interact with mandatory 32-pair/64-game pilot, what constitutes synergy, and whether the 6 GiB free-memory floor is continuous.
- A5 parity and broader fixture/negative-control gates remain before any launch; maintainer requires pilot-only lead go-ahead before switch 1.

## Amendment 2 map inventory (read-only)

All eight named packages exist at the pinned `700bb16483f6d92664153e98d6f2560c312acaab` tree and match the hashes in `ab_campaign_pilot.py`: A Nuclear Winter (`_ra_a-nuclear-winter.oramap`, 2 playable seats/2 mpspawn); Satan's Clutch (`SatansClutch.oramap`, 2/2); Red Spice (`Red_Spice_2v2_BI-4.4.oramap`, 4/4); Terra Cotta (`Terracotta-ratls2.oramap`, 4/4); Back to Basics (`back-to-basics.oramap`, 6/6); Winter's End (Rich) (`winters-end-rich.oramap`, 6/6); Great Sahara 2 (`Great_Sahara_3.oramap`, map.yaml title is Great Sahara 2, 8/8); Ice Cold (`ice_cold.oramap`, 8/8). Missing maps: none. Counts came from each archive's map.yaml PlayerReference@MultiN and actor mpspawn entries. Symmetric spawn pairing and engine acceptance are still unverified and remain hard preflight gates; counts alone do not prove symmetry.

## Validation

`py -3 -m unittest tools.tests.test_ab_campaign_pilot -v`: 7 passed, including slot-policy tamper rejection. `py_compile` and `git diff --check` passed. Fixture-only dry run: 64 games, 0 launches; manifest SHA-256 `e40eb38b188da744c73af4c707c2e6aa076e9235541778247b93a95ed92ad753`; source pin and `pt7-control/engine` hashes verified. Estimate: 11.07 worker-hours; 7.29 slot-wall hours at 3 small-game workers; 5.11-9.47 h range. Full tests directory intentionally not run per watchdog rule.
