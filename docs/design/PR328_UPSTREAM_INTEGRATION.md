# PR #328: upstream integration and armament-mode boundary

## Local follow-up: spawn-only roster classification (2026-09-06)

Fetched upstream `77beaef41`, 171 commits beyond this PR's integrated base.
Those commits are **not integrated** here. The snapshot and validation sections
below describe the PR worktree, not current upstream gameplay. Upstream's handoff
assigns the weapon codemod to Claude and composite-registry review to Nova/Ember;
this repair does not overlap those decisions or authorize merging PR 328.

The extractor lacked the actor-name parameter required by the existing
`test_assign_references.py` regressions and the self-prerequisite rule in
`REFERENCE_PIPELINE_HANDOFF.md` §10. It now excludes a positive exact self
prerequisite from offline roster eligibility while preserving negated self gates.
The actor key is passed by `extract_actor`, including for inherited Buildable
traits. This is the documented Cameo roster heuristic, not general prerequisite
reachability simulation. Costs, HP, weapons and runtime YAML are unchanged.

Both direct execution and unittest discovery now execute all 14 assignment tests;
the misplaced `unittest.main()` previously hid the last class during direct runs.
An independent reviewer ran the direct test file and found no blocking defect.

Comparison over 2,198 ledger rows changes only `forgotten_mutant_wild` and
`forgotten_tiberianfiend_wild` from buildable to non-buildable; all 100 exact
negated-self cases previously eligible remain eligible. Generator-produced diffs
contain exactly these two flags and removal of their four derived tier fields.
The global model fingerprint is unchanged. Full raw/derived ledger verification
passes for all 33 ledgers after the focused regeneration. Percentage-runtime and
145-template generator synchronization checks pass on this PR worktree.

This changes later pricing/reference populations intentionally; it does not apply
prices. The follow-up full suite completed **88/88 modules, 862 tests run, 15
skipped, 21 failed modules** in 481 seconds, recorded in
[`bounded_test_run.json`](../audit/latest/bounded_test_run.json). Compared with
the previous recorded run, there are no newly failing modules and the assignment
module is repaired. Remaining failures are not waived or automatically classified
as harmless. Structure and decision audits still fail on the invalid registry.

The default Python lacks openpyxl: four consumer tests and eleven workbook tests
were skipped in that run. Both modules were separately rerun with the bundled
Python: **16 consumer + 11 workbook tests pass, zero skips**. This supplemental
result does not turn the red full-suite report green. Sampled full-suite peak was
879.8 MB for its process tree and 50.5% PC memory, with an 84% PC guard. Diff checks
pass; no game was launched. Publication and latest-upstream integration remain
pending.

## Previous upstream integration snapshot

Integrated upstream `e06ed9907` (115 commits beyond the former base). This is a
tooling integration, not approval to merge PR #328 or change gameplay.

## What changed here

- The workbook and range-tool import conflicts combine upstream's centralized
  class membership with this PR's per-armament firepower handling. Both broad
  write guards remain closed. Workbook fingerprints include both helper modules.
- The JSON-only nominal proposal now requires an actual attack selecting the
  primary armament. Orders such as AttackMove do not qualify. Disabled, paused
  or unknown activation at the assumed zero-condition snapshot blocks a proposal.
  This is not a simulation of actual spawn-time conditions or combat readiness.
- The new armament-mode report resolves inherited slots and reports each mode's
  factor, attack selectors, activation uncertainty and other base-YAML references.
  It never sums primary/garrison slots, removes an armament, or proposes a price.
- All 33 raw and 33 derived ledgers were regenerated. A comparison with the exact
  upstream extractor, run against the same current YAML, produced identical raw
  data after removing only `resolved_firepower_modifiers`, and identical derived
  sidecars. Derived changes relative to committed upstream are regeneration drift,
  not a new pricing law or applied actor costs.

## Current roster result

The ledger-listed armed population is **1,000**, up from 950 on the previous base.
It is not an exhaustive inventory of every concrete actor or every map override.

| Actual armament topology | Actors |
|---|---:|
| Fewer than two slots | 338 |
| Same-weapon primary/garrison pair | 86 |
| Other repeated same-weapon slots | 23 |
| Other multiple weapons | 553 |

None of the 86 simple pairs pass the weapon-only model: 49 first stop at folded
percentage damage, 17 at standalone percentage damage, 8 at GlowImpact, 1 at state
feedback and 11 at projectile delivery. These are first blockers, not exhaustive
independent findings. **41 pairs also have other base-YAML references.**

There is therefore no evidence to relax the multi-armament proposal guard. Five
single-armament actors pass the structural screen; four have reference concerns.
The Spy remains the one unshared nominal candidate, not a rebalance recommendation.

## Hydra: current versus historical

Upstream `8748c68e4` replaced HydraSpit's four profiles with `BulletChem_Light`;
subsequent upstream generator work retains 18,000 Damage, PercentageScale 10,000,
ReloadDelay 15, Range 5979 and the Corrosion map binding at 20. This integration
preserves that upstream definition. It does not assert that the redesign preserves
the old weapon's damage, splash, state response or gameplay balance.

The old laboratory and two-stage pilot concern a different, historical weapon.
Their original JSON and report artifacts remain intact. Tests now explicitly use:

- A fully resolved weapon fixture captured from
  `819abe10d5858b810c6102a33eeebce42165f6cb`, canonical SHA-256
  `50c133e219282e45ffe130f8a657d61aba40e732aecd9953a19d9098680e4122`.
- The archived target/shooter scenario, canonical SHA-256
  `5591cd280cb6e097795a2bb6e1fccd9850e47b3245754b03fbd83b35b395d398`.

Historical evaluations require explicit complete inputs. Default execution against
today's weapon rejects the unsupported scenario before writing artifacts. Tests
retain the old arithmetic evidence separately from the current BulletChem contract.
No game launch or in-game validation is claimed.

## Validation boundary

Generator synchronization passes for 145 templates. Percentage-runtime checks pass.
The physical-state audit remains failing with 208 findings. The structure and
decision audits fail because upstream's reviewed-composite registry no longer
matches the resolved weapons. No stale digest or changed composite is automatically
re-approved here; the old structure/decision artifacts must not be read as current.

The [registry drift queue](../audit/latest/composite_registry_drift.json) retains
all **355 validator findings**. Its overlapping categories include 11 curated
main-name disagreements, 14 manifest main-name disagreements, 151 changed main
fingerprints despite unchanged names, and 5 reference/reachability disagreements.
These are review categories, not approval decisions. Its raw topology counts show
335 stacked concrete weapons: 242 reachable and 93 currently unreached. No
reviewed/unreviewed totals are claimed while the registry is invalid.

`tools/audit/report_composite_registry_drift.py --write` writes only the diagnostic
JSON. A fresh report still returns exit 1 when the registry is blocked; report
freshness must not turn a failed registry into a green validation.

The previous integrated suite completed **88/88 modules, 859 tests run, zero skips,
22 failed modules** at PR head `9a47d4703`. The current report is superseded by the
local follow-up run described above. Setup errors
prevent some classes from running their test methods; this is not 859 passes.
Sampled full-suite process-tree peak was 831.6 MB and PC memory peak 44.9%, with an
84% guard. The new/migrated focused tests pass, but the overall suite remains red.
A prior green 802-test result applies to the old base, not this integration.

Baseline reproduction was selective, not a second full clean-checkout suite:
loading the exact `e06ed9907` registry implementation against byte-matching upstream
YAML/manifest/resolver inputs reproduced all 355 registry findings. Loading that
revision's extractor and assignment tests reproduced the three two-argument
`_is_balance_buildable` API errors. Other failed historical contracts remain
unresolved findings; they are not all declared harmless or upstream-proven here.

Next review should reconcile changed composite decisions against their upstream
commits, then refresh the registry and dependent reports. That is separate from
approving weapon changes or removing the nominal solver's safety boundaries.

The 11 curated main-name disagreements group into three concrete review batches:

- Seven Tesla-containing superweapon chains lose a separate `Tesla_Heavy` main:
  Atomic, NaxiV1Rocket, PulseMissile, RA2Atomic, RAAtomic, SteelIonCannonDamage and
  TDIonCannonDamage.
- Three Ixian cannon profiles lose a separate `CannonHE_Medium` main:
  DuelistTankCannon, HeavyIxianCombatTankCannon and IxianCombatTankCannon.
- JapanesePlasmaBomb replaces its Chemical/Demolition/Flame trio with Plasma.

These observations describe profile membership only, not damage preservation or
approval. The other fingerprint/reference findings still require review too.

## Priority finding: AtomicCore loses a delayed extra hit

Upstream [a92ae850f](https://github.com/cameo-mod/Cameo-mod/commit/a92ae850ff65b65cab015f99e2a5a0fc9e115910)
does more than collapse two same-family mains. Comparing its parent AtomicCore
definition with current upstream gives these explicit Tesla stages, all targeting
`Shielded`:

| Authored Delay | Before raw Damage | Current raw Damage |
|---|---:|---:|
| 0 | 100,000 | 200,000 |
| 2 (`Tesla_Super_ExtraDamage`) | 100,000 | 100,000 |
| 3 (`Tesla_Heavy`) | 100,000 | removed |
| 5 (`Tesla_Heavy_ExtraDamage`) | 100,000 | removed |
| Sum of these explicit Tesla stages | 400,000 | 300,000 |

Thus the explicit raw Tesla-stage budget falls 25%, and a delayed 100,000 main
contribution moves to the initial stage. **This is not a claim of a 25% reduction
in total nuclear damage or actual shield damage**: armor, percentage applications,
defenses, target eligibility and the other nuclear payload remain separate.

The engine's `WeaponInfo.Impact` dispatches all warheads and schedules positive
Delay values through `DelayedImpact`; `ExtraDamage` is not an engine exemption.
The structural main-count predicate deliberately excludes that companion name.
Preserving only the main-warhead total therefore does not establish preservation
of the complete delivered payload or timing.

Current resolved descendants Atomic, RAAtomic, RA2Atomic, NaxiV1Rocket, DTAtomic
and ChemTibAtomic all contain the remaining two Tesla stages. This is a concrete
reason to reconcile the review decisions, not merely refresh their fingerprints.
Whether the removed hit and altered timing were intended remains a maintainer
decision. No gameplay restoration is applied by this PR.
