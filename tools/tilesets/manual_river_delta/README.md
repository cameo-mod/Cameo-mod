# Manual river-delta handoff

This workspace is for hand-cutting liquid-lava overlays while preserving a repeatable production pipeline. Each tile has its own folder under `projects`.

## sh18 quick start

1. In GIMP, open `projects/sh18/inputs/approved_cracked_base_sh18.png`.
2. Use **File > Open as Layers** and add `projects/sh18/inputs/donor_lava_layer_sh18.png`.
3. Confirm the image remains exactly **144 x 144 px** and both layers start at **0,0**.
4. Rename the layers `Approved cracked base` and `Donor lava cutout`.
5. Save the working file as `projects/sh18/gimp/river_delta_edit_sh18.xcf`.

Work at native resolution and zoom to 800% or 1600%. The supplied 4x files are inspection aids; native files are safer for final pixel placement.

## Cutting with the Path tool

1. Select the `Donor lava cutout` layer.
2. Add a white layer mask: **Layer > Mask > Add Layer Mask > White (full opacity)**.
3. Use the Path tool to trace the liquid region you want to retain. Let the path follow existing plate edges and cracks where the liquid meets cracked ground.
4. Convert the path to a selection.
5. Leave feathering at **0 px** for a crisp pixel-art boundary. Use at most **1 px** only where a genuinely soft contact is wanted.
6. Invert the selection if necessary, select the layer mask, and fill the unwanted region with black.
7. Refine the mask with a 1 px hard brush. Keep the lava layer itself unchanged; edit only its mask.

The transition should come from the shape of the cut—plate-edge steps, feeder cracks, and retained dark islands—not from a broad translucent blur.

## Required exports

Save the XCF, then export both files below.

### 1. Lava-only cutout

Hide the base layer and export:

`projects/sh18/exports/manual_lava_cutout_sh18.png`

Requirements:

- RGBA PNG with transparency outside the retained lava
- exactly 144 x 144 px
- no scaling, cropping, layer offset, or indexed-color conversion
- preserve any intentional left/right edge contact

### 2. Visual composite

Show the base and lava layers and export:

`projects/sh18/exports/manual_composite_sh18.png`

Requirements:

- RGB or RGBA PNG
- exactly 144 x 144 px
- visually identical to the saved XCF composition

PNG compression and metadata settings do not matter. Do not flatten the XCF; the editable mask is useful if another continuity adjustment is needed.

## What Codex will do afterward

After the two exports are present, Codex will:

1. Validate dimensions, alpha, layer alignment, and required edge contacts.
2. Compare the composite against the approved cracked base and confirm that only the intended region changed.
3. Convert the flattened result to the Volcanic indexed palette with deterministic dithering where necessary.
4. Audit neighboring-tile continuity and protected sparse-frame edges.
5. Produce production previews and an audit report.
6. Write or update `.vol` art only after explicit approval.

## Adding another river-delta tile

1. Copy `_template` to `projects/shXX`.
2. Create `inputs`, `gimp`, `exports`, and `production` subfolders.
3. Put the native approved base and transparent donor-lava layer in `inputs`.
4. Update `project.json` with the tile name, canvas size, and expected edge contacts.
5. Use filenames `manual_lava_cutout_shXX.png` and `manual_composite_shXX.png` in `exports`.

The standardized names let the same validation and production-conversion process handle every river-delta tile.

## GIMP 3 batch helpers

`create_gimp_project.py` runs inside GIMP 3's `python-fu-eval` batch
interpreter. It creates a layered XCF containing a visible editable lava layer,
a hidden untouched lava backup, and the approved cracked base. Its
`inspect_xcf` helper reports canvas size, layer order, visibility, and offsets.

`export_gimp_project.py` exports the named editable lava layer and the visible
composite. It deliberately ignores extra hidden layers that GIMP or the artist
may add during manual editing.
