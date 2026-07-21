# Basalt Columns - Codex Shoreline Decoration Handoff

Owner/source marker: **Codex basalt-columns session**  
Status: **approved authoritative RGBA art handoff; not yet converted to `.vol` or wired into YAML**

Approved review: `reference/all-twelve-refit-ground-and-lava-thin-grid-review.png`

This folder contains the 12 approved volcanic basalt formations: two variants for each of `1x1`, `2x1`, `2x2`, `1x2`, `2x3`, and `3x2` footprints.

## Selection rule

- Ground placement: use `combined-ground.png`, which contains the approved refit formation over its separate short east-facing shadow.
- Offshore/cracked-lava placement: use `combined-lava.png`.
- Editable ground composition: place `shadow.png` behind `formation.png`.
- Editable offshore composition: place `lava-glow.png` behind `formation-soft-light.png`. The approved Soft Light result is already baked into `formation-soft-light.png` and `combined-lava.png`.
- Do not add `shadow.png` to offshore/cracked-lava placements.
- Do not composite `lava-bounce.png` with Normal mode and do not mirror sprites automatically. Both actions change the approved lighting.

## Resolution levels

- `master-144/`: transparent 144x144 placement masters.
- `footprint/`: exact 48px-per-tile legal crops.
- `source-24px/`: authoring-density assets, upscaled 2x for Cameo production.

The formations were refitted at 24px authoring density to reserve east-side shadow clearance. The top-left of every footprint crop is phase `(0,0)` of production `w1.vol`. The detached glow was regenerated after the refit and is modulated against that phase, so it follows the lava cracks instead of creating a continuous orange pool.

## Production lava reference

- Branch commit: `1ae1302818024d3871d4f54c17ca727414a26165`
- `w1.vol` SHA-256: `AE3A984C8DCDF6F5BD1E815D107140C41B46EFDD9D1B3F726D4DA0213AF68424`
- The decoded reference has zero nonuniform 2x2 blocks, confirming exact 24-to-48 nearest-neighbor scaling.

## Guarantees

- Every visible master pixel, including the detached ground shadow, is inside the legal box recorded in `manifest.json`.
- Every shadow retains at least two transparent 24px-source pixels at both the east and south edges.
- All cast-shadow projection is due east; compact contact darkness remains directly beneath the lower contour.
- Generated detached glow preserves the same 2x pixel cadence as production lava.
- Lava bounce is clipped to the refitted formation alpha and uses Soft Light.
- Existing full-size/no-shadow and broad-pool versions remain archived in the artist workspace, not in this handoff.

## Integration boundary

This package deliberately does not modify repository YAML, shoreline assets, `.vol` files, or the untracked inland-river generator. Placement, palette export, and `.vol` production remain with the consuming session.
