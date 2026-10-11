# AB Campaign Consolidation Status — 2026-10-11

- Consolidation branch: `codex/ab-campaign-consolidation-luna`.
- Code integration commit: `1e00403c81056a3a55f66c3ebfe74dfdac045ec2`, based on source `e49acf41a1b3699f01d44e4def06ed4bf83af038` and engine `4f69df005fe0d893526890fcb5bbc42e442d6fd6`.
- Frozen candidate manifest: `docs/audit/ab_pilot_manifest_candidate.json`, SHA-256 `2b926834ec4bba743909755e248fe2c72fe1eddf875a10e807e72041c75a8923`; `execution_approved` is false.
- A5 receipt: `docs/audit/receipts/A5_fixed_tick_e49_4f69_attempt2.json`, SHA-256 `2bb6dffafced59ffc810e8f1c938c4465621834b134552e27b676da72c098f0f`. It is exact e49/4f69, two verified capped runs, `IDENTICAL_TAIL_FLUSH`.
- RAM policy: 10.5 GiB process cap; first-game admission at measured 6,538,502,144-byte peak plus 3 GiB; second small game needs 14 GiB; at most two small games; larger games exclusive.
- Verification: six focused suites, 67/67; py_compile pass; manifest validation and no-launch dry-run pass (64 planned, zero launches); diff check clean.
- Still gated: independent exact-SHA review, reserved runtime map/overlay acceptance, and maintainer approval. No campaign outcome cells launched.
