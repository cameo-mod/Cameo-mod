# R3/R1 diagnostics — 2026-09-13

This is the first read-only pass over the settled 34-ledger state after #356, R4, and R8. It writes no actor, ledger, registry, or price values.

## R3 virtual baselines and band

`derive_virtual_anchor.py --all` produced 28 class candidates from 129 selected source members. The candidates remain `UNAPPROVED`; 26 combat classes have no calibrated damage/reload model, one class has no source pool, and the support class is ability-priced without a combat verifier. The tool therefore does not invent model damage or reload.

`check_band.py` reports 205 members outside the 75%–350% practical band across 26 classes, plus 27 unresolved cargo passenger valuations and 33 authored cargo price mismatches. These are review evidence, not permission to move actors to force occupancy. The full structured report is [r3_r1_band_20260913.json](r3_r1_band_20260913.json), with the readable report in [r3_r1_band_20260913.md](r3_r1_band_20260913.md). Virtual class outputs are under [r3_virtual_anchor_20260913](r3_virtual_anchor_20260913).

## R1 separate weapon stats and DPS verifier

`weapon_stat_targets.py` now reports independent reference targets for damage per shot, reload, and burst. It retains the independently projected DPS target as a verifier and never decomposes it into those inputs. Across 348 actors with reference families, 217 have all three separate inputs but lack a shared burst-delay target; 131 are missing at least one separate input; 545 active actors have no reference family. All composed DPS checks are therefore explicitly withheld rather than mixing a reference target with Cameo's live burst-delay value.

The machine-readable output is [r3_r1_weapon_stat_targets_20260913.json](r3_r1_weapon_stat_targets_20260913.json), with the readable sample and status counts in [r3_r1_weapon_stat_targets_20260913.md](r3_r1_weapon_stat_targets_20260913.md). The reference-anchor proposal remains diagnostic and reports its existing DPS-basis warnings; no automatic writeback is enabled.

The one-time ledger refresh is separate in draft PR #357; `extract_stats.py --check` and `audit_balance_drift.py` are both clean. `docs/reference/ini_corpus.json` remains untouched.
