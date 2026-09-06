# Astra implementation review

## Window and scope

User-authorized three-hour run: 2026-09-07 03:18:43–06:18:43 WIB.
Branch `astra/balance-pipeline`, based on merged PR 328 (`291052380`).
Main checkout is not used for edits. This branch is published for coordinator
review, not directly to master. No pricing targets are inferred from authority.

## Foundation review

- Full audits and tests were completed against the identical source immediately
  before PR 328 merged; see `docs/design/PR328_UPSTREAM_INTEGRATION.md`. The suite
  remains red, with raw debt retained. Fresh upstream fetch still resolves to
  `291052380` at this run's start.
- Independent hand calculations agree with `formula.py` for MBT, archer and
  artillery: equal baseline ratios yield C0; doubling HP and DPS yields
  `(1.5 + 2 + 4) / 3 = 2.5 C0`, respectively 2000, 1250 and 1250.
- Independent unchanged Zerg ledger dry run reports zero changes and writes
  nothing. This does not exercise the unsafe confirmation path.
- Document-claims audit is not green; recorded upstream mismatches must not be
  waived or overwritten with guessed values.

## Completed: safe apply completion

Review found that the current apply command writes despite collected errors,
passes an obsolete positional repository argument to extraction, ignores child
exit codes, and silently discards some missing-warhead errors. Confirmation can
therefore report success while leaving stale ledgers. The first repair closes
these paths; no live balance target was applied to validate it.

Implemented: complete preflight, unambiguous fresh provenance, explicit
shared-consumer checks, staged extraction, exact raw-ledger agreement, checked
validation, and rollback that does not overwrite concurrent edits. Regression
tests exercise failure paths and preserve unrelated pending proposals.

Validation:

- `python tools/tests/test_apply_balance.py`: 35 tests pass, including interruptions,
  disk/audit/extraction failures, shared consumers, stale provenance, unsupported
  leaf deletions, comments/BOM/newlines, and staged-output input isolation.
- Sequential full test run: 89/89 modules, 800 tests, zero skipped, 43 failed
  modules. Compared with merged PR 328's 766-test baseline, both failing-module
  and failing-method identities are identical. The full run contains 34 apply
  tests; the final missing-ledger case and deletion subcase were rerun in the
  35-test focused validation afterwards. This is not an 800-pass claim.
- Real `extract_stats.py --output-dir` verification: 67 raw/derived artifacts
  semantically identical to the baseline; all live ledger bytes unchanged.
  The initial byte comparison detected checkout CRLF versus generated LF, so
  publication now avoids rewriting semantically unchanged JSON.
- Live `apply_balance.py --faction starcraft_zerg`: zero planned changes,
  zero inherited skips, exit 0; no writes. Peak process tree 783.1 MB.
- Canonical `bash tools/audit/run_all.sh`: completed exit 1, 713.9 seconds,
  no zero-byte reports. Existing upstream debt remains visible. Peak process
  tree 1119.2 MB; peak PC memory 52.5% under the 84% guard.
- After staging the new source files, direct security/error-handling scans
  confirmed no additional findings; unchecked-subprocess flags drop by two.
  Generated audit snapshots will be refreshed for the complete follow-up PR.
- Independent review approved the writer scope after challenging deletion,
  interruption, concurrent-edit and Windows temporary-file cases.

Revert: revert the dedicated safe-apply commit; no live YAML needs reverting.
Limits: exception-safe, not filesystem-wide atomic; no concurrent game reader;
hard kills retain printed recovery originals; map/script references still require
review. Existing unrelated upstream test/audit failures are not waived.

## Completed: readiness reporting and ruled Heavy Sniper membership

The readiness command crashed because it called the removed
`intentional_composite` exemption. It now uses the shared resolved main-warhead
predicate without subtracting reviewed exceptions, and reports unavailable
weapons or active-rule resolution failures rather than silently clearing them.
Low pricing residuals and absence of stacked mains no longer claim sign-off or
complete structural clearance. Missing baseline/stat data is explicit, and all
27 anchor actors are inspected even when their fitted baseline is absent.

The sole ledger edit sets `td_gdi_heavysniper.design.class_anchor` to
`heavy_sniper`, implementing the existing FORMULA_V2 section 6b heavy-sniper
override. It preserves the live SniperInfantry template, every numeric stat and
weapon. This is metadata classification, not a gameplay rebalance.

Validation: nine focused readiness tests pass; the existing 13 class-membership
tests pass; the real readiness command completes, with 27/27 anchors in class
and zero signed anchors. Its buildable denominator is explicitly 1956 ledger
rows, including structures/upgrades: 632 classified rows is not claimed as unit
coverage. Every class still has a raw stacked-main finding. Extraction check:
33 ledgers, zero drift; 778.3 MB peak process memory and 51.0% PC memory.
Independent review challenged and corrected missing-stat handling and misleading
formula/sign-off wording. Candidate ranking remains diagnostic, not clearance.

No anchors are signed and no faction is repriced: unresolved weapon structure
and unapproved/missing anchor inputs remain prerequisites, not waived gates.
Revert the dedicated readiness/classification commit to undo this item.
