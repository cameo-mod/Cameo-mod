# STATUS: A/B campaign execution tooling — 2026-10-11

## Resume
- Current branch: `codex/devops-ab-fixed-tick`; base before this tooling commit: `532197a3b1e3e7a1a18b6d384ecadc99d5f96ef3`.
- Current code applies the approved memory policy: 10.5 GiB process cap, 15.5 GiB launch admission, 4 GiB host soft stop, 3 GiB hard reserve; one small-game worker and exclusive large games.
- Last verification: 23 focused tests passed (`test_campaign_runner`, `test_ab_campaign_pilot`, `test_a5_memory_parity_probe`); JSON schema parse and `git diff --check` passed.
- No campaign cells launched. Last reported free RAM was approximately 8.4 GiB, below 15.5 GiB admission.
- Next: independent exact-SHA review, refresh and validate frozen manifest/tool/schema hashes, obtain all-map engine acceptance plus Great Sahara asymmetry ruling, verify current engine/A5 pin, and require maintainer execution approval. Recheck host admission immediately before any authorized launch.

## Changes captured in this checkpoint
- Added fixed-tick A5 cap plumbing and strict `IDENTICAL` parity qualification.
- Corrected receipt schema and campaign seat resolution to join engine `player.home` coordinates against verified generated-map `home_location`; PlayerReference `home` remains a separate identity.
- Fixed dry-run policy lookup and aligned serial scheduling estimates/tests.
- Enforced the authorized RAM limits in probe, runner, pilot policy, schema, and documentation.

## Open gates
- Campaign launch remains disallowed: current host memory is below admission threshold.
- Executable manifest remains `execution_approved=false` until all hard gates pass.
- Runtime acceptance and seat-fidelity proof for every pinned map; explicit Great Sahara asymmetry ruling.
- Analyzer fixtures and negative controls must pass on this exact source SHA; this commit includes focused coordinate-shape coverage but requires independent review.
- If engine binary/version changes from A5's pin, obtain A5 parity proof on the final exact engine pin before outcome cells.
