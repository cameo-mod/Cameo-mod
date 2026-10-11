## Resume (2026-10-11, sampled descendant cleanup)

Next: exact review `d47503dce8f...` then refresh acceptance tool pin and request a fresh reservation before any runtime map acceptance. No runtime acceptance or outcome cells were launched for this change; PILOT HOLD remains.

Exact pushed code commit: `d47503dce8f...` on `codex/ab-campaign-pidguard-integration`, parent `208079d90d28bc67cb9b441e799918bcaa19c131`. During a live, previously validated root, the monitor stores descendant identity (PID, parent PID, normalized engine executable, CreationDate). If the root exits, final discovery/cleanup may trust that exact previously observed identity even when the child argv omits SupportDir; a newly discovered process still needs the exact cell SupportDir. PID-scoped kill remains.

Focused map acceptance tests: 21/21 PASS; py_compile and `git diff --check` PASS. Regression covers an observed child without SupportDir surviving launcher exit, PID-scoped cleanup, and rejection of a newly discovered unattributed child. No games launched.
# Map acceptance direct-process monitor status

## Resume

Next: have the reviewer inspect `codex/ab-campaign-pidguard-integration@68193e2d65eea822718272047da06ffee0ec4640`, then run the focused Windows map acceptance under a fresh reservation after process/RAM admission. Do not launch campaign outcome cells; PILOT HOLD remains active.

## Result

- The acceptance tool invokes `OpenRA.exe` directly. A prior reserved attempt on `2c64b453408444101462d6a6d652e67d873cdd21` rejected its first two Nuclear Winter cells as `CHILD_PROCESS_NOT_FOUND`; artifacts are preserved in `results/map-engine-acceptance-20261011-pidguard-v1/` and labeled aborted.
- `map_engine_acceptance.py` now accepts a root-only process as the game only when the root executable and exact cell `Engine.SupportDir`, `Launch.Map`, and `Launch.Benchmark` argv tokens match. If descendants exist, they remain part of the sampled/cleaned tree; missing memory samples still fail closed. PID-scoped cleanup is unchanged.
- Regression coverage includes direct root-only success, wrong map/benchmark rejection, mismatched support rejection, launcher-plus-child, exited launcher plus surviving child, unrelated process exclusion, and missing memory samples.
- Focused `tools.tests.test_map_engine_acceptance`: 20/20 passed. `py_compile` and `git diff --check` passed. No launch occurred for this change.
- Candidate manifest remains SHA-256 `1f7cac209531e9ae1b94468ecc233562d4d04b9a844542186324b27510767074` and `execution_approved=false`; its A5 evidence is unchanged. Runtime acceptance must produce new receipts against the updated acceptance-tool SHA before any outcome campaign is considered.
