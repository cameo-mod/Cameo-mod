# Schwarzer Mond Artwork Status

## Copy-pasted / borrowed icons (need replacement)

These Schwarzer Mond actors previously used icons from the Naxis faction or reused
another Schwarzer Mond actor's icon. They now have temporary unique placeholder
icons pending final artwork.

| Priority | Actor | Current icon | Notes |
|---|---|---|---|
| High | `schwarzer_mond_mars` | `schwarzer_mond_mars_icon.png` | MARS hover artillery; placeholder generated. |
| High | `schwarzer_mond_m200bjagerline` | `schwarzer_mond_m200bjagerline_icon.png` | Heavy tank destroyer/artillery; placeholder generated. |
| Medium | `schwarzer_mond_gravitycoretank` | `schwarzer_mond_gravitycoretank_icon.png` | Superheavy advanced tank; placeholder generated. |
| Low | `schwarzer_mond_blackbomb` | `schwarzer_mond_blackbomb_icon.png` | Kamikaze bomb; placeholder generated. |

## Missing icon files

All icon files referenced by `sequences.yaml` exist in `mods/cameo/bits/ra2/` or
`mods/cameo/bits/ra2/mod/`. No missing PNGs were found.

## Other potential artwork gaps

This audit only checked icon filenames. A full asset audit (sprites, voxel
sequences, SHP files, cameo artwork) has not been run. The user should decide if
we should expand the search to:

- Placeholder/default SHP sprite sheets for buildings and vehicles.
- Voxel files (`.vxl`/`.hva`) for voxel-based units.
- Construction / death / weapon-fire animation sequences.
- Faction-specific UI artwork (loading screens, faction icon, etc.).

## Suggested replacement order

1. **MARS + M200 B. Jägerline** — both use the same borrowed Naxis icon and are
   player-facing combat units. Fixing them removes the most obvious copy-paste.
2. **Gravity Core Tank** — uses a Naxis Jagdpanzer icon; a superheavy tank needs
   a distinctive cameo.
3. **Black Bomb** — optional, internal reuse is less urgent.
4. **Full asset sweep** — if time permits, run a sequence-by-sequence audit for
   missing or placeholder sprites/voxels.
