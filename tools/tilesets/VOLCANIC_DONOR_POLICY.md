# Volcanic donor policy

Current Volcanic artwork and semantic-mask workflows use **RA Temperate**:

- Template metadata: `mods/cameo/tilesets/ra_temperat.yaml`
- Donor artwork: `mods/cameo/bits/temp/*.tem`
- Donor palette: `mods/cameo/bits/ratemperat/ra_temperat.pal`

`mods/cameo/tilesets/volcanic.yaml` may supply legal production layout and
terrain metadata when shared `.tem` artwork is not declared by RA Temperate.
It must not cause donor pixels to be loaded from Barren.

The standalone Barren theater remains active and independent. It is not a
Volcanic donor.

`generate_volcanic_tileset.py` is the historical Barren-derived bootstrap. It
is disabled unless `--allow-legacy-barren` is explicitly supplied. Do not use
it to regenerate current production assets.

The `Barren Roads` and `Barren Grass` entries inherited by `volcanic.yaml` are
known legacy shared-asset placeholders. Replace them through the RA Temperate
material workflow when the Roads and Debris families enter production review.
