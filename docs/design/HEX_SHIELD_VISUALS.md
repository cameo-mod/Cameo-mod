# Recolorable hex-shield visuals

## Runtime contract

Shield mechanics still decide whether an actor is shielded. The visual traits keep their
existing `RequiresCondition`, palette, and upgrade/aura behavior. The fitting layer changes
only the overlay image and sequence.

`WithIdleOverlay` supports `{actor}` in `Sequence` and `StartSequence`. The token is replaced
once when the overlay is created, so shared receiver templates can use `fit-{actor}` without
per-frame fitting or per-actor trait patches.

The shape policy is:

- buildings: camera-correct dome;
- infantry: upright oval;
- vehicles, ships, and aircraft: sphere;
- explicitly elongated mobile actors: 32-facing directional oval (currently Cloudbreaker).

Palettes remain independent of shape: default/Protoss blue, Ixian silver, Yuri indigo, and
Consortium cyan. Idle and hit palettes retain 25% and 50% alpha.

## Fit formula

For actor visible bounds `W_A`, `H_A`, actor center `C_A`, master visible bounds `W_M`,
`H_M`, master center `C_M`, and per-shape padding `P`:

```text
scale = max((W_A + 2 P_x) / W_M, (H_A + 2 P_y) / H_M)
sequence offset = C_A / scale - C_M
```

Scale is rounded upward to three decimals so serialization cannot reintroduce leakage.
The sequence offset, rather than a trait offset, owns shield centering. This matters because
OpenRA scales sequence offsets together with the sprite.

## Regeneration workflow

Run the engine utility against the active Cameo manifest, then run the fitter:

```text
python tools/audit/fit_hex_shields.py --actors-out <actors.txt>
OpenRA.Utility cameo --measure-actor-sprite-bounds --actors-file <actors.txt> --out <bounds.json>
python tools/audit/fit_hex_shields.py <bounds.json> --json-out <report.json> --sequences-out mods/cameo/sequences/generated_hexshield_fits.yaml
```

The engine utility uses OpenRA's sprite cache, enumerates animation frames and facings, and
measures visible PNG/SHP pixels. Body-relative turret mounts and independently rotating turret
sprites are combined across their full facing sets, with both rest and maximum-recoil positions.
The fitter classifies receiver geometry, applies the formula, and regenerates one sequence per
actor under four shared image roots. Do not hand-edit the generated YAML.

Directional actors are fitted facing by facing. Each actor body facing is paired with the shield
facing selected by OpenRA's nearest-facing rule; the shared scale and offset must satisfy every
pair. This also covers actors such as Cloudbreaker that have 64 body facings but use 32 shield
facings.

After regeneration, rerun the fitter without `--sequences-out`. `current_covers_model` must
be true for every result, and every resolved `fit-{actor}` reference must exist.

## Voxel and maintenance notes

Headless sprite measurement cannot rasterize VXL models exactly. Hybrid actors union their
measured sprite components with conservative `Selectable.DecorationBounds`/`Bounds`; actors
without visible sprite pixels use the selectable bounds alone. Both cases are identified in the
JSON report. Review them in game when their voxel or selection bounds change.

The generated sequences all reference the same four indexed PNGs. They add sequence metadata,
not duplicate sprite atlases, and each shielded actor still renders one idle-or-hit overlay.
There is no per-frame bounds scan, scale calculation, or additional draw pass.

When actor art, body/turret offsets, animation facings, or selection bounds change, regenerate
the table. When adding an elongated exception, select the directional oval in the fitter's
shape policy and keep its actor template on the directional fitted image.
