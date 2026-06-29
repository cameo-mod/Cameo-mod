# SHP over-resolution downscale bake

Pre-bakes over-resolution unit sprites (sequences with `Scale < 1`) down to their
display scale and sets `Scale: 1`, recovering texture-sheet (VRAM) memory with no
visual change.

## Why it's visually lossless

OpenRA samples world sprite sheets with `GL_NEAREST` (the sheet texture's default
`TextureScaleFilter`; see `SpriteRenderer.DrawSprite` → `location + scale * s.Offset`,
size `scale * s.Size`). So a sprite drawn at `Scale: 0.6` is already a nearest
minification on screen. A CPU **nearest** downscale of the source SHP at the same
factor reproduces that output — and, because it only subsamples palette indices
(never blends them), player-colour (remap) and shadow indices survive untouched.
It also removes the sub-pixel shimmer non-integer runtime scaling produces.

## Geometry contract

`scale` multiplies BOTH the sprite offset and size at render time. After a uniform
`s×` downscale, the baked frame Size and Offset are already `s×` native, so dropping
`Scale → 1` preserves position/size **iff** each sequence's `Offset:` field is also
multiplied by `s`. The rewriter does this.

## Pipeline (run from this directory)

1. `shp_scale_audit.py` — scan active sequence files, rank candidate SHPs by
   recoverable sheet bytes, flag player-colour (remap) pixels.
2. `bake_shp_downscale.py` — palette-safe nearest downscaler + SHP(TS) reader/writer.
   Composite each frame on the canvas, uniform-nearest-downscale, crop to the
   scaled native box (deterministic `s×` geometry). Empty frames carry format
   byte 1 (IsShpTS rejects zero-size frames with type 0).
3. `td_shp.py` — SHP(TD) codec (LCW + XOR-delta decode, LCW encode). NB: the
   engine trims TD frames in VRAM, so the real TD prize is small (~2 MB) — not
   deployed here, kept for reference.
4. `batch_bake.py` — bake all clean TS candidates, run the geometry +
   loadability (`IsShpTS` port) gate, emit a report + contact sheet.
5. `rewrite_sequences.py --apply` — bake (via the gate), then surgically edit the
   active `sequences/*.yaml`: add/replace `Scale: 1` and scale `Offset` only on
   sub-sequences that resolve to a baked SHP; copy baked SHPs over originals.
   NOTE: not idempotent — run once from a clean (unedited) sequence tree.

`verify_render.py` renders frames via a palette for eyeballing.

## This archive

176 TS units baked (~61.5 MB sheet VRAM), 8 sequence files edited. In-game
verified loading normally. 5 units excluded (thin frames blank under downscale):
nax_quadflak, rasamurai, aa_shinobi, scybgm, aa_samurai.
