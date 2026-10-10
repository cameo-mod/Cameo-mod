## Resume

Next: lead review this exact SHA, resolve the map symmetric-spawn/engine acceptance gate, then freeze final source, engine, tools, and arm payloads. No launch backend is enabled, and this task performed zero launches. Current branch: `codex/ab-campaign-tooling`.

## Result

Implemented amendment-specific planner and evidence adjudicator for the 64-game BU pilot: 16 setup/faction-mirror configurations, two matched seed pairs per setup, side swaps, exact eight map package hashes, seat-count validation from archived map.yaml, and strict natural/cap/incomplete/invalid classification. Aggregates per-seat metrics to arm totals; explicitly reports synergy as not identifiable from this pilot because the team-size and individual setups lack matched context. Dry-run fixture receipt: `tools/tests/fixtures/ab_campaign_pilot_dry_run_receipt.json` (NO_LAUNCH, 64 games, estimated 11.07 serial worker hours).

## Open gates

- No actual frozen campaign manifest was supplied in the repository; the exercised manifest is explicitly fixture-only/non-executable.
- Map archives contain enough playable `MultiN` references, but engine acceptance and symmetric spawn proof remain unverified; dry run marks this pending.
- This changes planner/analyzer only. The runtime backend for distinct control/treatment bot profiles, map.yaml generation/proof, 45k cap, 3,000s wall/180s stall, 8 GiB process watchdog, PID-scoped cleanup, serial lock, and immutable per-cell receipts is not implemented. Run command remains disabled.
- A5 parity and broader fixture/negative-control gates remain before any launch; maintainer requires pilot-only lead go-ahead before switch 1.

## Validation

`py -3 -m unittest tools.tests.test_ab_campaign_pilot -v`: 5 passed. `py_compile` passed. No-launch dry run: 64 jobs, 0 launches. `git diff --check` passed.
