# Procedural fire generator — checkpoint notes

`tools/gen_fire.py` generates license-clean RGBA looping flame sprite sheets (numpy + Pillow)
as an alternative to the palette `fire1..4.shp`. This is **work-in-progress and NOT used
in-game**: the groundfire actor is currently back on the original palette `fire1.shp`, because
the hand-pixeled palette fire still reads better than the generated art so far. The goal is to
get the generated fire — **especially the base** — close to Factorio's `fire-flame-01`.

## Status
- **Generated art not wired in-game.** `mods/cameo/sequences/misc.yaml` `groundfire_flame:1`
  = the original palette `fire1.shp`.
- **The fade IS in use:** the groundfire actor (`mods/cameo/rules/misc.yaml`) uses
  `WithLifetimeFade` (replacing `KillsSelf`) so the palette ground fire fades in/out instead of
  popping. This is independent of the generated art. Burning buildings/husks use idle overlays
  (`WithIdleOverlay@CriticalFire` / `@Burns`) and would need a separate "fading overlay" trait.
- Generator has 5 styles (below). Closest to Factorio so far: **`puff`** (billowing lobes) and
  **`campfire`** (forged slim tongues). Neither is good enough yet.
- A fade trait (`OpenRA.Mods.Cameo/Traits/Render/WithLifetimeFade.cs`) was written for the
  in-game fade-in/out; it is compiled into the build but **currently unreferenced** (the YAML
  was reverted to `KillsSelf`). Keep it for when the art is ready.

## Styles (`--style`)
- `cluster` / `tongue` — original. Smooth value-noise + a Gaussian cone → smooth "candle"
  shapes. No internal turbulence. Weakest.
- `billow` — billow/turbulence noise (`|signed noise|` summed → rounded lobes), animated by a
  seamless vertical scroll (`vtile_billow`). Lobed but soft. Knobs: `--billow-gamma`,
  `--billow-mottle`.
- `puff` — **metaball compositor**: many discrete rounded puffs rise/grow/cool/fade in a
  seamless loop; each has a 3D "lit highlight" on its upper side; coverage-weighted temperature
  driven by screen height (white-hot foot → red crown). **Closest to Factorio's billowing body.**
  Knobs: `--puff-count/-radius/-gain/-alpha/-sharp/-rise-ease/-hl-size/-hl-strength/-tip-temp`.
- `campfire` — forge `--campfire-count` slim `billow` columns side by side (tent height profile,
  centre tallest), max-merged, plus a base "bed" ellipse and a downward-licking "fringe". Reads
  as licking tongues. Knobs: `--campfire-count/-spread/-min-h/-jitter/-dome/-bed-w/-bed-h/`
  `-bed-heat/--fringe-fingers/--fringe-len`.

All styles loop seamlessly (every contributor is periodic over `--frames`). Determinism-safe
(art only). Look parameters are all CLI flags.

## Preview workflow (no game needed)
```
python tools/gen_fire.py --style puff --contact --frames 8 --out docs/fire_scratch/x.png <knobs>
```
`--contact` writes a 1-row strip of every frame. Open the PNG on a dark background (it has
alpha). `docs/` is gitignored (scratch). To compare against the reference, crop frames from the
Factorio sheet onto a dark bg and zoom (NEAREST).

## The open problem: the BASE
Factorio's `fire-flame-01` base is (1) a dense, bright, white-hot mass of overlapping rounded
lobes, and (2) a **ragged fringe of thin ORANGE flame fingers licking downward/outward** at the
very bottom, with clear dark gaps between them. Approaches tried for the base that did NOT work:
- smooth elliptical "base bed" → too clean/artificial;
- curving the foot line into an arc/dome → looked like a concave "half-pipe" with cut corners;
- a downward "fringe" of fingers → still too pale/soft, not crisp orange licks.
Promising next direction: build the ragged downward base onto the **`puff`** body (its lobes match
Factorio better than `campfire`'s clean tongues), rather than polishing `campfire`.

## Reference art
`docs/factorio_ref/fire-flame-01.png` (840x1170, **84x130 per frame, 90 frames, 10 columns**) and
`fire-util.lua`. **Copyright — reference only, never bundle/commit/ship.** `docs/` is gitignored.
This is license-clean work: generate ORIGINAL art that *looks* like Factorio; do not copy pixels.

## How to re-wire into groundfire when the art is ready
1. Generate a production sheet (no `--contact`), e.g.:
   `python tools/gen_fire.py --style puff --frame-size 80 --frames 60 --cols 10 --out mods/cameo/bits/effects/groundfire_rgba.png <tuned knobs>`
2. Point `groundfire_flame:1` (`mods/cameo/sequences/misc.yaml`) at `groundfire_rgba.png`
   (`Length: *`, `Tick:` ~33, `Offset:` tuned to sit the foot on the ground).
3. For fade-in/out, replace `KillsSelf` on the groundfire actor (`mods/cameo/rules/misc.yaml`)
   with `WithLifetimeFade` (`Duration: 125, 250`, `FadeInTicks`/`FadeOutTicks` ~4). The trait
   rolls the lifetime on the synced RNG (MP-safe) and ramps alpha render-only; it fades decoration
   renderables too (the groundfire overlay is a decoration). C# change → rebuild with `.\make.ps1 all`.
4. Sequence/art changes need no rebuild. Test via `tools\debug-launch.cmd` (boots `debug.oramap`;
   fire a flame weapon at open ground to spawn groundfire).

## Lessons
- A baked sprite sheet must beat the hand-pixeled palette fire to be worth shipping; it hasn't yet.
- Previews are shown zoomed 2–3x; the in-game sprite is its true pixel size, so it always looks
  smaller in-game. Keep the flame filling most of the frame (high `base_y` + `tip-height`) for
  on-screen height, and place with `Offset`, not by shrinking the flame.
- This is a longer-term effort, not a quick task.
