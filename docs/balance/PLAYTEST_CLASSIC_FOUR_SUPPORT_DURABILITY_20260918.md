# Classic-four MCV/support durability batch — 2026-09-18

This is the next chassis-only slice after the accepted classic-four batch and
the harvester batch. It applies the newest reference map's complete projections
to four MCVs and two unarmed RA1 support vehicles. The four transport rows are
held for a separate review because their cost changes intersect the passenger-
sum cargo pricing law.

## Evidence and scope

Source: `docs/audit/latest/Cameo-reference-map-original-four-20260916.html`.
Every applied row has complete HP, Speed and Cost evidence (2/2 or 3/3
sources) and no actor-level weapon projection. No weapon, warhead, `Burst` or
`BurstDelays` field is changed.

The shared `^MCV` template is not edited: it feeds MCVs across roughly twenty
other packs. The four classic MCVs receive materialized per-actor blocks at the
end of their actor nodes, after `Inherits:`, so the template's other users stay
unchanged. The two RA1 support vehicles already own their chassis blocks and
are patched in place.

## Applied values

| Actor | HP | Speed | TurnSpeed | Self-heal Step | HpPerStep | Cost |
|---|---:|---:|---:|---:|---:|---:|
| `td_gdi_mobileconstructionvehicle` | 300,000 -> **294,000** | 75 -> **65** | 15 -> **13** | 120 -> **118** | 15,000 -> **14,700** | 5,000 -> **4,920** |
| `td_nod_mobileconstructionvehicle` | 300,000 -> **294,000** | 75 -> **65** | 15 -> **13** | 120 -> **118** | 15,000 -> **14,700** | 5,000 -> **4,920** |
| `ra1_allies_alliedmobileconstructionvehicle` | 300,000 -> **253,000** | 75 -> **70** | 15 -> **14** | 120 -> **101** | 15,000 -> **12,650** | 5,000 -> **4,650** |
| `ra1_soviets_mobileconstructionvehicle` | 300,000 -> **253,000** | 75 -> **70** | 15 -> **14** | 120 -> **101** | 15,000 -> **12,650** | 5,000 -> **4,650** |
| `ra1_allies_mobilegapgenerator` | 25,000 -> **96,000** | 75 -> **76** | 30 -> **30** | 10 -> **38** | 1,250 -> **4,800** | 5,000 -> **1,640** |
| `ra1_allies_mobileradarjammer` | 25,000 -> **72,000** | 100 -> **74** | 40 -> **30** | 10 -> **29** | 1,250 -> **3,600** | 5,000 -> **1,710** |

HP uses the accepted 1,000 grid. `Repairable.HpPerStep` follows `HP / 20`
exactly; `ChangesHealth@SelfHealing.Step` is the nearest integer to
`HP / 2500`, matching the existing audit tolerance. Speed is the nearest
integer. Costs use the 10-credit grid with half-up handling at an exact
5-credit tie.
TurnSpeed follows the resolved actor's existing formula: the MCVs follow
`round(Speed / 5)`; the two support actors' existing doubled turn lane follows
`2 * round(Speed / 5)`.

The doubled support lane is not inferred from the actor name: both actors
currently carry `AttackFrontal` support/dummy traits and no turret, which is
why the audit selects F10 rather than F8. Removing those traits later would be
a separate design change and would require revisiting their TurnSpeed values.

## Deliberately held rows

`td_gdi_chinooktransport`, `td_nod_chinooktransport`,
`ra1_allies_alliedchinooktransport` and `ra1_soviets_hiptransport` have
complete chassis projections but remain out of this batch. Their cost changes
must be reconciled with the accepted passenger-sum pricing and cargo-load
contracts before application.

## Playtest focus

- Confirm MCV price reductions do not make early expansion strictly dominant.
- Confirm the TD and RA1 MCV speed reductions preserve deployment timing and
  do not make an MCV unable to escape harassment.
- Confirm the gap generator's large HP increase and cost reduction do not make
  mobile stealth support the cheapest default opening.
- Confirm the radar jammer's speed reduction is still usable while its larger
  health pool survives the approach to a frontline.

`mods/cameo/rules/camea.yaml` contains a commented-out MCV that inherits the TD
GDI MCV directly. It is inert in the current `mod.yaml` graph, but must be
reviewed before Camea is re-enabled because it would intentionally inherit the
TD-specific override.
