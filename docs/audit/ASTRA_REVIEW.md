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
- A second independent real integration test in a disposable copy applied one
  Heavy Sniper HP edit (25000 to 26000) with `--confirm`, then checked all 33
  ledgers with zero drift. Exactly one YAML line changed. Percentage-model
  sidecars changed consistently with the population HP input. This was not an
  approved or applied live balance change. Peak process tree 1594.6 MB, PC 51.4%.

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
tests pass; helper/data inspection finds 27/27 anchors in class
and zero signed anchors. Its buildable denominator is explicitly 1956 ledger
rows, including structures/upgrades: 632 classified rows is not claimed as unit
coverage. Every class still has a raw stacked-main finding. Extraction check:
33 ledgers, zero drift; 778.3 MB peak process memory and 51.0% PC memory.
Independent review challenged and corrected missing-stat handling and misleading
formula/sign-off wording. Candidate ranking remains diagnostic, not clearance.

No anchors are signed and no faction is repriced: unresolved weapon structure
and unapproved/missing anchor inputs remain prerequisites, not waived gates.
Revert the dedicated readiness/classification commit to undo this item.

## Completed: packaged shellmap rename fallout repair

The required graph boot test failed with `No rules definition for unit
ra1_soviets_barracks`. This was reproduced against the unchanged upstream map
payloads: desert-shellmap-2 had 15 missing placed actors and shellmap_v3 had ten.
The exact replacements are actor-key renames from upstream `ad7c5e232`, not new
naming decisions. The related Lua spawns and map-local rule overrides needed the
same correction; survival's script also retained four renamed upgrade actors.

Reference-only archive edits: desert-shellmap-2 (22 replacements), shellmap_v3
(45), survival (15), total 82. ZIP member order, timestamps, compression metadata,
archive comments and every unchanged member's bytes are preserved. No map terrain,
damage, cost, HP, durability or spawn timing changed. Original archives were saved
outside the worktree for recovery; the main checkout and its maps were not touched.

The shared MiniYAML reader now accepts archive text via `load_text`, using the
same parser as file loading. Eleven parser tests and two packaged-shellmap tests
pass. The tests check every packaged shellmap's placed actor references and Soviet
actor literals in those scripts plus survival; they do not claim full Lua execution.
Menu boot after repair: `MenuPostProcessEffect.PostWorldLoaded`, zero new exception
logs, 43.2 seconds, peak PC memory 69.2%. The owned test game was stopped immediately
after proof. Revert the dedicated map-reference commit to undo this repair.

## Completed: observer combat-value graph integration from PR 323

Adapted Devin AI's graph implementation from PR 323 head
`053e7eeac6f930ef3e178e964a3091d2527cc81a`. The original PR is open/conflicting;
its only merge conflict with this branch was the accumulated lessons document.
This follow-up carries the feature for coordinator review; it does not merge or
modify the original PR. Original authorship is preserved in the commit trailer.

Integration corrections: enum and dropdown ordering now agree for hotkeys; the
axis says value destroyed minus value lost, not enemy-only damage (the pinned
PlayerStatistics accounting can include non-neutral friendly victims). Signed
scaling retains positive-only behavior and continues sampling zero/flat results.
New tests cover ranges, cadence, initial/signed/flat samples and panel indices.

Fresh `make.ps1 all`: zero errors; eight existing engine warnings. The updated
Cameo DLL timestamp and its unique UTF-8 description marker were verified. All
63 Cameo C# tests passed before the separate telemetry tests were introduced.
The repaired tree passed the menu boot described above. Independent source review
approved the graph and map changes; the implementer's own review was not counted
as the independent approval. No live prices, weapons or AI decisions change.

Still pending: visual review of signed labels, scrolling and clipping in an actual
observer match. A menu boot is loading proof, not graph visual approval. Revert
the dedicated observer-graph commit to remove this feature.

## Completed on this branch: record-only match telemetry and AI contracts

Implemented Aedis H.4 phase one, not adaptive AI. The active Player observer reads
existing personality conditions; the active World recorder writes one bounded,
versioned local JSONL file at completed-match notification. Records contain slots,
factions, bot identifiers, team/alliance metadata, initial/final personality status,
outcomes and existing accounting totals. No orders, conditions, prices or decision
logic are changed. Disk failures are non-fatal and do not recurse into disk logging.

The rules fingerprint covers ordered raw Rules/Weapons inputs at world load,
including map overrides; it is explicitly not a whole-runtime hash. Module MVIDs
cover Game/Cameo/Common only. Records are capped at 256 KiB, published without
overwriting old files, and contain no player display names or account identifiers.
Each client may record a match: consumers must deduplicate and inspect metadata.
Completed-world coverage excludes shellmaps, replays, editors and loaded saves;
abandoned matches without GameOver, episode histories and pairwise attribution
remain unavailable. Unknown values stay unknown rather than becoming zero.

Validation:

- All 76 Cameo C# tests pass, including 13 new telemetry cases covering serialization,
  limits, failure handling, fingerprints, eligibility and personality ambiguity.
- Fresh build: zero errors, eight existing engine warnings; bounded tree 967.7 MB,
  PC 48.6%. Runtime DLL freshness checked before the game tests.
- A temporary deterministic playable map completed normally: one match record,
  human Won, medium bot Lost, observed personality, expected 100 value lost, 101
  ticks at 40 ms, populated source fingerprint; zero new exception logs.
- The normally completed match's replay exited normally, produced no new record
  and no exception/desync report. This is a short single-client regression, not
  multiplayer stress certification. An earlier force-stopped recording produced
  an incomplete replay and is not counted as a replay pass.
- Final menu/shellmap boot with telemetry enabled: required menu marker, zero new
  exceptions and no match records; 41.9 seconds. Maximum PC memory across these
  runtime checks was 71.6%, below the 84% guard and user's 90% ceiling.
- Test fixture, two synthetic records, two test replays and benchmark CSVs were
  moved out of game directories into the owned temporary evidence folder. They
  remain recoverable; no Jungle archive or real match record was removed.
- Independent source review challenged the hash scope and disk-error fallback;
  those limitations and the non-recursive failure path were corrected.

H.2 extends the existing AI_ARCHITECTURE document with ownership, current inputs,
future read-only hints, cadence, synchronization and failure contracts for the
measured scoped module inventory. H.3's five differentiated round-two briefs are
written; external services have not been contacted and answers are still pending.
Conflicting learning-stage numbering and stale current PR status were clarified.
This is not completion of all Task H acceptance gates or a merge approval.

Revert the dedicated telemetry/AI-contract commit to remove these additions.
Next runtime work is the observe-only master after review, not dynamic switching.

## Additional readiness and neighboring-PR review

Coverage now separates structures/upgrades from buildable unit-like ledger rows:
632 classified out of 886 non-structural buildable rows (71.3%); 55 have no usable
template, 199 lack a class in the current taxonomy. The all-buildable denominator
remains visible (1956). These are ledger rows, including variants, not deduplicated
units. This is a denominator correction, not hundreds of new classifications.
Unreadable ledgers fail closed. Eleven readiness and 13 class-membership tests pass.
All 27 anchors are assigned to their stated classes, but none are signed and every
class still has a raw stacked-main finding; no prices are authorized by coverage.

PR 321 remains open at `e42eb9914972346f77a931385da62d741f22ae35`.
Its old stat/converter/insurance proposals are not imported. Shared document files
need reconciliation if both PRs proceed, but no shared runtime implementation file
conflict was found in this checkpoint. The original PR was not changed or merged.

## Completed: exact active D2K engineer classification

Task B review identified three unequivocal missed roles: `atreides_engineer`,
`corrino_engineer` and `harkonnen_engineer` inherit `^EngineerInfantryTemplate` from
the active D2K shared pack. The extractor's defaults-only registry missed that
template. Formula V2 section 6b explicitly assigns engineers to support.

The existing extractor now recognizes this exact template when active; arbitrary
pack template names are not inferred as roles. The shared class map maps
EngineerInfantry to support. Three inheritance regressions pass (live actors,
inherited role with no cache mutation, missing/unrelated template rejection).
Independent review also ran nine resolved-firepower and 14 assignment regressions.
Full extraction comparison across 67 artifacts changes exactly three subtype
strings in the D2K raw ledgers, no numeric value or derived artifact.

Before/after: support members 112 to 115; other classes unchanged. Buildable
classified rows 632 to 635, out of 886 non-structural rows (71.3% to 71.7%);
no-template buildable rows 55 to 52. This is a small real classification repair,
not completion of Task B's wider coverage objective. Air/naval/economy taxonomy
gaps and subjective roles are deliberately not guessed. Revert the dedicated
engineer-classification commit and its three ledger strings to undo it.

The final live readiness invocation also exposed two CLI-only defects missed by
the earlier helper tests: formatting missing cost baselines raised TypeError, and
the stacked-main table reused `rows`, replacing the JSON fit rows with the last
class's weapon list. Both are repaired with command-level JSON regression coverage.
The previous statement that the CLI completed was too broad: helper/data results
were valid, but the final missing-baseline table failed. The corrected full command
is rerun as part of final validation; unavailable baselines stay explicit.

## Brief reconciliation: physical-state pricing is already implemented

Task E's claim that status-effect delivery is priced at zero is stale in this base.
`physical_state_price.py`, `formula.physical_state_price_multiplier`, extraction's
derived physical-state fields, and `fit_class.price_unit` already implement the
documented bounded delivery multiplier. The existing 22 physical-state tests pass.
No duplicate weight path or second coefficient was introduced. Model limitations,
including relaxation and changing weapon structure, remain limitations rather than
grounds to apply unsupported prices. Current measured reports take precedence over
the brief's historical binding counts.

## Completed: fail-closed ledger claims and equivalent source cleanup

The `ledgers_drifted` prose measurement previously interpreted any missing drift
marker as zero, including a failed child audit. It now requires a successful,
unique, positive-population clean marker or a consistent explicit drift result.
Unknown output, contradictory markers, zero ledgers and child failures stay
unavailable. Three test methods exercise 15 scenarios using the actual registry
measurement. No documented target or audit ratchet was raised.

`D2K_APC_Rocket_AA` repeated the same compatibility warhead key in two source
blocks. Their disjoint fields are now together in the first block. Comparing all
2,894 active resolved weapon definitions, including values and child order,
finds zero changes. The focused inheritance regression passes and duplicate-key
findings decrease from 261 to the existing ceiling of 260. This is weapon YAML
source cleanup, not a damage, targeting or physical-state rebalance. Extraction
still reports 33 ledgers with zero drift. The post-edit menu gate passed in 36.1
seconds with no new exceptions (63.6% sampled PC memory).

Two existing regression references now use the exact upstream Soviet Barracks
rename already repaired in the packaged maps. Their assertions are unchanged;
all ten weapon-correctness follow-up tests pass. The missing Kotin weapon test is
not mechanically renamed: upstream also changed its profile and operation, so its
remaining failure needs a separate role review. Two readiness test reads now use
explicit UTF-8. Revert the dedicated cleanup/guard commits to undo these repairs.

## Final automated checkpoint (2026-09-07)

- Full isolated Python suite: 93/93 modules, 823 tests run, zero skipped, 42 failed
  modules; 479.6 seconds, 928.9 MB sampled process-tree peak, 66.3% PC peak.
  Merged PR 328's report has 766 tests, 88 modules and 43 failed modules.
  Independent method-identity comparison finds zero newly failing methods and
  four resolved failure identities. This is not an 823-pass claim: setup/import
  failures and upstream structural/role-contract debt remain visible in the JSON.
- All 76 Cameo C# tests pass. The latest C# sources match the freshly built and
  runtime-verified DLLs; no engine source or pin changes were made.
- Complete canonical audits: exit 1, 628.5 seconds, 1,120.0 MB process-tree peak,
  66.2% PC peak, no empty report files. Percentage-runtime passes, 33 ledgers have
  zero drift, and generated templates remain synchronized. Raw document and
  structural findings remain failures; no registry exemptions were restored.
- Fresh structure check fails honestly at 967 reachable stacks against 240 and
  3,577 excess mains against 452. The retired decision-report module is absent;
  its stale test remains an explicit import failure, not a current decision audit
  pass. Older generated decision artifacts must not be treated as current approval.
- Error-handling raw counts improve: discarded errors 88 to 86, unchecked child
  calls 20 to 18; missing-encoding findings remain 92, not hidden by a raised limit.
  All three new AI inventory claims measure exactly (21 types, 36 Player modules,
  one World module); other pre-existing document mismatches remain visible.
- Independent final source/scope review found no new blocker and confirmed the
  unchanged-assertion repairs. Coordinator review remains required before merge.

PR 321 remains unchanged. Its four overlapping paths at this checkpoint are
LESSONS_LEARNED, doc_claims, and the Atreides/Harkonnen ledgers. The latter overlap
only our engineer subtype metadata: preserve both sides' intended metadata and
regenerate, rather than choosing an entire ledger. No shared runtime implementation
file conflict was found. PR 323 also remains unchanged; its graph adaptation is
contained here with attribution.
