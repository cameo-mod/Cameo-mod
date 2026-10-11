# A/B campaign tooling status — 2026-10-11

Candidate tooling is on `codex/ab-campaign-pidguard-integration@aef2fe6edc592036c2e9d4845ddb99a418267bb8`, based on reviewed PID-guard commit `3abda62420b6fd9476709175341d93d7740f21ad`.

- Source/engine pins: `e49acf41a1b3699f01d44e4def06ed4bf83af038` / `4f69df005fe0d893526890fcb5bbc42e442d6fd6`.
- Accepted A5 receipt SHA-256: `2bb6dffafced59ffc810e8f1c938c4465621834b134552e27b676da72c098f0f`; both capped headless runs are valid, use the same fingerprint and binary pins, and the strict order verdict is `IDENTICAL_TAIL_FLUSH`.
- RAM policy uses measured 3,042,615,296-byte headless peak, 10.5 GiB process cap, 3 GiB hard reserve, 4 GiB soft threshold, single-game admission at peak + reserve, and 14 GiB for two small games. Large games remain exclusive.
- Candidate manifest SHA-256: `1f7cac209531e9ae1b94468ecc233562d4d04b9a844542186324b27510767074`; execution manifest SHA-256: `2e5fd1b56fcb76fbc2447154b8dd353ecd2e5186843850b67c0220a3cb9cacea`. Both remain `execution_approved=false` pending maintainer re-signature.
- Focused campaign suites: 54/54 pass, including the exact pinned A5 receipt test and refreshed source/slot/admission expectations. `py_compile`, manifest validation, 64-cell no-launch dry-run, and `git diff --check` pass.
- No game or map acceptance launch occurred. The no-launch validator accepts 8 map pins and 64 planned cells with zero launches; the actual A5 gate accepts the pinned receipt. Runtime map acceptance, canonical branch consolidation/review, and maintainer re-signature remain open.

This is a candidate branch only; it is not a merge into canonical `codex/ab-campaign` or `master`.
