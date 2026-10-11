# MCV deployment search: investigation and gated repair plan

2026-10-09. Owner: Codex Sol. Task MCV-EXPAND-IDLE.
Base: c76283c0b15dd845b66fe54070ca10f59146d7ea.
Engine inspected: 0a3f77dbe. Status: awaiting engine-seam ruling; no implementation.

## Static findings

`OpenRA.Mods.Common/Traits/BotModules/McvExpansionManagerBotModule.cs`
in the pinned engine selects the first placeable candidate at lines 965-971,
then checks that candidate's reachability at 978-979. It does not continue the
weighted search when the first candidate is unreachable. The closest-first
fallback at 989-990 also returns null on its first unreachable placeable cell.
Thus a later placeable, reachable cell can be missed. This proves an H2 code
defect, not its occurrence or frequency in the recorded match.

H1 (every footprint rejected) remains unproven. The replay/debug evidence has
no per-search rejection census. `BuildingUtils.cs:88-93` requires every footprint
tile to be buildable; `:18-85` checks actors/replacements, building influence,
ramps and terrain, not just resources. Resource terrain can change through
`ResourceLayer.cs:195-201`; resource exclusion is plausible but does not establish
that every annulus cell failed. The trace's statement that units cannot block
placement is incorrect: `BuildingUtils.cs:34-43` rejects non-ignored occupants
when replacement is unavailable. The trace also overstates the initial loop:
reachability is checked after selecting one placeable cell, not during selection.

## Why the existing mod provider cannot repair it

`ChooseMcvDeployLocation` is private (`McvExpansionManagerBotModule.cs:913`) and
`FindDeployCell` is its local function (`:940`). Neither can be overridden.
`IBotMcvExpansionSiteProvider` (`TraitsInterfaces.cs:687-690`) returns a center;
the engine independently chooses a cell in the annulus. Returning a validated
center does not guarantee the engine selects a validated cell. Issuing movement
orders independently would compete with the manager and change its scheduling.
Vendoring the roughly 950-line manager is possible but is not a minimal repair.

## Accepted implementation (supersedes the initial seam proposal)

The lead authorized an opt-in engine repair, then rejected a split YAML mount
because disabled clones still receive expansion/attack interface callbacks.
Final ruling: keep one instance and use standard condition observers.

Engine branch `devin/mcv-deploy-cell-engine` at
`331657f07aeb30dfe3dc9d8cdb9c56d289dcc41b` adds
`SkipUnreachableDeployCellsCondition` (BooleanExpression, null by default).
GetVariableObservers retains the base observers and updates a derived bool.
Null/false takes the original search body, verified byte-identical after LF
normalization. No callback guards, virtual extraction or second module instance.

When enabled, McvDeployCellSearch accepts only candidates satisfying placement
AND the existing locomotor reachability predicate. Both weighted and closest
passes continue past rejections. A valid weighted candidate survives an
unreachable fallback; a closer reachable candidate replaces it. Verdicts are
cached across passes, bounded by the annulus's unique cells. The transform offset
remains inside the caller's CanPlaceBuilding delegate. Source-equals-target keeps
the supplied shuffle ordering. This repairs search completeness within the
existing candidate set, not movement/path guarantees or placement restrictions.

One debug line per failed enabled sweep reports own MCV id, tick, source/target,
unique scanned/placeable counts and Placement/Unreachable/None tallies. It does
not infer terrain/actor causes from CanPlaceBuilding=false or log each cell.
Diagnostics add no orders, reassignment, RNG draws or gameplay mutation.

`mods/cameo/ai/ai.yaml` retains the SINGLE original module instance and adds only
`SkipUnreachableDeployCellsCondition: genericbot`. Classic's original shared
RequiresCondition and every other setting remain byte-identical. Fransbot source
is unchanged. The engine pin changes only on this task branch; master/inc freeze
and runtime adoption gates remain.

Validation: nine focused engine tests (condition activation/revocation, default
off, first-pass continuation, fallback continuation/preservation, unique tallies,
equal-source ordering, empty annulus); two mod tests parse the real YAML with
OpenRA MiniYaml/FieldLoader and prove single-instance/generic-only selection plus
byte preservation against the base block fixture. No game launches or boot gates
were performed, as explicitly instructed. Match-level H1/H2 discrimination and
classic order-stream parity remain runtime work for the Coordinator after review.
