# Continuous CannonAP follow-up: implementation and remaining decisions

Draft implementation, not a whole-roster balance verdict. No actor price, HP,
movement, firing cadence or engine pin changes. Final combined validation pending.

## Implemented scope

Twenty existing CannonAP consumers move to the shared base. Total authored flat
damage is unchanged. The original five pilot consumers are unaffected.

| Cohort | Consumers | Policy and preservation |
|---|---:|---|
| Forgotten Tank Killer/Warrior/Turret | 3 | Existing Light AP role at h0; chemical alternatives untouched |
| Naxis Hetzer/Anti-Tank Cannon and elite/corrosion descendants | 6 | Existing tank-destroyer AP role at h0; roots reduced to three layers; exact effects and warhead order retained |
| Apocalypse normal/elite | 2 | Existing Light AP main at h0; radiation/fire/Tesla alternatives untouched |
| Allied Tank Destroyer | 1 | Existing reviewed AP role at h0;24000 retained, including shipped total |
| Sky Hawk cannon | 1 | h0; plasma alternate isolated on the exact legacy parent, not reprofiled |
| Tick Tank laser/deployed laser | 2 | h1 matches the already migrated normal/deployed Tick Tank; separate3% laser route retained |
| Gunboat2Inch | 1 | h0, authored Spread450 preserves runtime300; no blast shrink |
| Cobra/Python normal/deployed | 4 | h0 flat profile; previous folded component was zero; all four independent percentage companions per mode remain exact |

This intentionally changes armor effectiveness. Under shared Scale2000 the h0
main contributes zero percentage damage; h1 follows the approved h/2 formula.
It is not equivalent to merely renaming a template, and does not establish that
every resulting unit is optimally priced or balanced. The family role was retained,
not inferred from a desired price. Re-fitting stats requires fresh reference inputs
and is not silently performed by these YAML edits.

## Freedom Rocket elite: separate bounded W24 closure

One MissileAP_Medium main now carries360000 instead of240000 compatibility plus
120000 canonical. Its previously folded6000/10000 percentage component becomes
an explicit companion; the existing Flak and Shrapnel percentage routes remain
unchanged. COMPOSITE flat damage changes159600 to162000 (+2400, about1.50%).
Other target/profile fields are retained. Two flat events become one, so rounding,
lethal-hit ordering and zero-damage callbacks are not claimed equivalent.

The companion has Range0,32,33 and Falloff100,50,0. A naive clipped Range0,32
would lose the old inclusive cutoff hit: the engine's final falloff endpoint is
exclusive. The one-unit zero tail preserves damage at32 and gives zero at33;
the copied50% friendly-fire radius still ends at16.

Actual positional-impact probe, enemy distances0/16/32/33:
old and new percentage damage108000/81000/54000/0. Allied distances16/17:
40500/0. All six pairs matched, as did all twelve original control/endpoint lanes.
Peak RAM64.83%; no new exception log; owned process closed. The first attempt
used direct Actor impacts, which bypass spatial falloff, and correctly failed the
spatial expectations; it is not cited as spatial evidence.

## Regression evidence

Ordered historical fixtures preserve the prior converter evidence. A historical
view is available only after asserting the exact modern payload; original stored
hashes and historical reports are not rewritten. Focused checks cover non-profile
payloads, execution order, target/upgrade closures, positive percentage magnitudes,
every integer percentage distance0–65, and enemy/allied cutoffs. Whole-ruleset
comparison rejects changes outside the enumerated consumers and the exactly
preserved Sky Hawk legacy parent. Final audit/suite counts belong to the final
combined candidate, not these intermediate snapshots.

The tier diagnostic now distinguishes continuous profiles from unknown legacy
names without treating either as tier-certified. Its old multi-main budget does
not override the current one-main law; the existing ratchet is unchanged.

## Current source validation (10 September 2026)

This evidence belongs to the PR341 source worktree at published HEAD
`5a931844a3c6184fbfdd9f50fc8724b37543c5e0` plus the current unpublished candidate.
It is not combined-candidate evidence. The whole-weapon comparator passes for
2,898 resolved weapons: exactly twenty profile migrations, the exact Freedom
elite contract and one exact SkyHawk legacy clone, with no unexplained remainder.
All 33 source ledgers pass with zero drift; nine raw/derived faction pairs differ
from the published source. The earlier Forgotten report is a historical three-only
checkpoint and is explicitly labelled accordingly.

The canonical source audit completed with exit 1, not green. Existing gate reports
retain inherits, upgrades, basebuilder_crates, buildable_order, packs,
nuclear_flash_bindings, doc_claims and doc_health findings. The Contents-link
repair resides in PR339 and is absent from this source. The Freedom companion's
raw authored denominator occurrence increases 183 to 184; its registry and both
listed design documents are corrected together. This resolves that single claim
mismatch; five other baseline claim mismatches keep doc_claims failing. No markdown report is
zero bytes. The sole stderr sidecar is recent_changes.err, containing four
historical git-grep no-match comments, not a traceback.

The current structure survey counts 2,367 concrete weapons, 229 raw stacked
weapons (167 reachable), and 424 excess main instances (332 reachable). These are
raw debt counts, not hidden exemptions. Percentage-runtime reports 23 authored
shared applications, including 19 zero-unit profiles, and zero dispatch findings.
K-linearity covers 2,061 weapons, 1,634 folded and 2,442 standalone applications;
invariance and decomposition checks are clean.

The final source affected rerun passes 72 tests: six endpoint, 57 heaviness, three
Freedom and six Forgotten tests. Exact ordered warhead assertions now reject
reordered effect/damage events before returning a historical view, including
negative reorder tests across all twenty migrations and Freedom. Original
fixtures and hashes remain untouched. Narrow Gunboat and Firehawk name adapters
support the separate ownership PR without requiring its runtime data here.
A subsequent combined-suite review identified three historical test modules still
expecting pre-migration main tags. Their adapters now validate the exact ordered
modern payload before presenting the frozen historical view; the original
Scoop converter refusal, fixtures and hashes remain intact. The source rerun
passes all 19 tests across Apocalypse merge, exact-profile duplicate and final-
tranche modules. Independent adapter review found no objection; merged ownership
lookup compatibility and the combined rerun remain the integration owner's proof.

No further game or build was needed. The earlier bounded positional runtime
proof remains applicable; full combined tests and independent final review are
reported separately by the integration owner.

External continuation logs are under the handoff validation directory with prefix
`gameplay-continuation-`: comparator.log, ledgers.log, structure.log,
canonical.log, affected.log and final-adapters.log. Source audit regeneration also refreshes the
tracked latest reports; it does not certify baseline failures as accepted policy.

## Not applied: exact remaining balance decisions

Sixteen nonstandard compatibility consumers were assessed, not changed:
110mm_Gun; NaxiJadgDestroyer normal/elite/corrosion; LunarNaxiJadgDestroyer
normal/elite; RA2Gren60mm normal/elite; RA2MirageGun normal/elite;
RA2HeavyMirageGun normal/elite; TS70mmTur; tkmjuggap; tkmtechnicalmgap;
tkmturretcannon. Their main's preserved folded magnitude ranges300–4500 basis
points **before armor/falloff**, which h0 would remove. Independently authored
percentage/state routes would remain. Passing a profile-only comparator is not
sufficient justification for those material, role-dependent high-HP losses.
They need a supported per-cohort damage/role target, not an invented exception.

AsianTankMine's custom mine percentage contract and DragunovSniper's flat-only,
shared-owner and5x-shipped-damage history also remain decisions; no automatic
heavy-profile percentage term or mine redesign is introduced.

Other same-family W24 holds are concrete target distinctions: AAGunBoatFlak
normal/elite currently deliver8000 Ground/Water but2000 Air before armor. An
8000 all-target main would quadruple anti-air flat damage. FutureTankCannons
normal/elite similarly have100000 Air-capable collateral versus650888 ground
flat total. Direct weapon targeting does not prove airborne collateral impossible.
Selecting one target contract requires a gameplay decision; these are not hidden
from raw multi-main counts. Broader mixed-family survivor choices and unimplemented
family-specific state/extra-damage semantics are not declared completed by this pilot.

Independent static review confirms the target split: FutureTech
`yaml/weapons.yaml:1341` onward preserves 100000 Air-capable flat plus 550888
ground/water flat. An all-target main would multiply that airborne flat component
by 6.50888; a ground-only main removes it. The analogous Gunboat factor is four.
These factors apply to raw flat damage before armor/falloff, not whole-shot damage
including independent percentage and state routes. In
`OpenRA.Mods.Cameo/Warheads/AreaDamageWarhead.cs:583`, nearby-victim enumeration
checks each warhead victim independently of the weapon's direct-target filter.
`engine/OpenRA.Mods.Common/Warheads/Warhead.cs:71` validates victim target types;
`engine/OpenRA.Mods.Common/HitShapes/Circle.cs:43` computes shape distance including
height. The heavy cannon's Spread400/Falloff100,50,20,0 therefore permits low
nearby airborne collateral. This proves reachable geometry, not match frequency.
No policy-free single-main conversion follows; this backlog remains blocked on
intended target behavior, with no new engine mechanism or runtime change added.
