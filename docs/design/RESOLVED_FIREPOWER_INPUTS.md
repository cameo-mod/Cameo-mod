# Class-fitting firepower input correction

Scope: offline class fitting only. No actor costs, weapon damage, runtime code,
class anchors or live includes are changed. The inactive Hydra pilot stays inactive.

The old raw ledger field `firepower_multiplier` represents one local fine-tuning
knob. That field is still needed by legacy diagnostics/retirement tooling; it is
not a complete actor damage multiplier. Class fitting previously treated it as one.

Raw ledgers now also record `resolved_firepower_modifiers`: the inherited,
unconditional `FirepowerMultiplier` traits, with their integer percentages,
armament-name restrictions and local/inherited provenance. An empty list explicitly
means none; consumers must not fall back to the local knob in that case.

`fit_class.unit_inputs` multiplies the applicable percentages for each armament,
both for raw weapon DPS and the optional derived effective-DPS path. It does not
double-count the legacy field. Armament names are case-sensitive, default to
`primary` only when omitted, and retain explicitly empty names. Zero means zero,
not a missing value. Old ledger fixtures without the new field retain compatibility.

## Impact

The [generated comparison](../audit/latest/firepower_inputs.json) finds 730 changed
actor entries with usable class-fit inputs across the roster. These are input
corrections, not 730 balance defects or recommended price changes.

Examples of unconditional products:

| Actor | Old local multiplier | Resolved product |
|---|---:|---:|
| Hydralisk | 0.99 | 0.50 × 1.10 × 1.10 × 0.99 = 0.59895 |
| Marine | 0.31 | 0.50 × 1.10 × 1.10 × 0.31 = 0.18755 |

Both examples change by the same factor, 0.605. A common multiplier can cancel
when class members and their real anchor are normalized together. Do not interpret
the DPS-input ratio as a price ratio. No authoritative new prices are generated:
that requires selecting/refitting the intended class anchor and pricing mode.

## Deliberate limits

- This is the product of unconditional `FirepowerMultiplier` traits, not a combat
  simulator. Per-hit integer rounding can differ from multiplying floating DPS.
- Traits with `RequiresCondition` are excluded, including conditions that may hold
  at spawn. Veterancy, upgrades, player modifiers, health-dependent modifiers and
  other modifier trait types are not newly modeled here.
- The existing effective-damage/physical-state models and their limitations remain.
  Duplicate state delivery is not fixed or repriced by this change.
- Legacy workbook, proposal, range-update and band-check consumers are not silently
  migrated. This change fixes `fit_class` and records reusable facts for a separate
  reviewed migration; it does not claim all pricing tools now agree.
- The local fine-tuning field remains unchanged. Neither the resolved list nor its
  product is introduced as a gameplay write-back knob.

## Reproduce

Run with `tools/run-bounded-python.ps1` and an appropriate memory/deadline guard:

```
tools/balance/extract_stats.py --check
tools/balance/firepower_input_report.py
-m unittest discover -s tools/tests -p test_resolved_firepower_inputs.py -q
```

The report command checks freshness by default; `--write` refreshes only its JSON.
Regression coverage includes inherited Hydra/Marine values, conditional exclusions,
armament scopes, zero values, legacy compatibility, and both raw/derived DPS paths.

## Validation (2026-09-05)

- Full isolated suite: 80 modules, 767 tests run, 756 passed, 11 skipped; no failures.
  Includes nine new tests. Sampled process-tree peak 1,381.4 MB; PC peak 47.6%.
- All 32 ledgers pass fresh extraction checks. Removing only the new field from
  each refreshed ledger reproduces its prior contents; derived ledgers are unchanged.
- Generator sync (139 templates), percentage-runtime, structure/decision freshness,
  report freshness and diff whitespace checks pass.
- The decision audit initially hit a 1 GB process guard; rerunning under 2 GB passed.
  The existing physical-state audit still fails with 216 findings, unchanged.
- Independent review approved the scoped change after stronger exact-value and
  armament-name tests were added. No game launch or gameplay edits were needed.
