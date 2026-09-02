# "UI scaling above 150% messes up faction icons" — diagnosis

**Reported:** Discord `#bug-report`, 2026-09 (Legato, Dobry Kacer; AedisToru triaging).
**Status:** cause found and proven from the assets. Mitigation patched, proper fix needs an art
re-export. Guarded by `tools/audit/audit_chrome_scale_variants.py`.

## The reports, and why they look contradictory

| reporter | display | UI scale | result |
|---|---|---|---|
| Legato | 2800x1600 windowed | above 150% | broken |
| Dobry Kacer | 1080p (OS at 125%) | 150% exact | broken |
| AedisToru | 1440p | 150% | **fine** |

Same UI scale, opposite outcomes — which is why it read as a scaling bug. It is not. The three
reports partition exactly on **which DPI sheet the engine picks**, and only one of the three sheets
is broken.

## ⛔ CORRECTION — the first version of this diagnosis was half wrong

**Blackrobe asked the right question:** *"I don't know why it thinks the resolution should be at 3x
and not 4x like it is also for CA and OpenRA."* He was right about the convention, and the first
pass of this document (and its audit) was wrong to compare **canvas sizes**.

Read at the pinned engine (`mod.config` ENGINE_VERSION `462fc1fc4`,
`OpenRA.Game/Graphics/ChromeProvider.cs`):

```csharp
if (dpiScale > 2 && Image3x != null) { image = c.Image3x; density = 3; }
else if (dpiScale > 1 && Image2x != null) { image = c.Image2x; density = 2; }
...
new Sprite(sheet, density * mi, TextureChannel.RGBA, 1f / density);
```

`density` is a **hardcoded 3**, and the `Collection` class declares only `Image`, `Image2x` and
`Image3x` — **there is no `Image4x` field.** So the engine cannot use a 4x sheet at all.

⭐ **But upstream's `-3x` files really are 4x by CANVAS — and they are correct.** 3 × 256 = 768 is
not a power of two, so upstream pads: `OpenRA/mods/ra/uibits/glyphs-3x.png` is a **1024x1024 canvas
holding 768x768 of artwork**. All twelve upstream variant declarations look "4x" by canvas and every
one is fine. **The canvas is padding. What matters is where the ARTWORK is laid out.**

Measured that way — artwork bounding box against the base sheet's artwork:

| sheet | base artwork | expected at 3x | actual artwork | implied |
|---|--:|--:|--:|--:|
| `flags_3x.png` | 385x512 | 1155x1536 | **1536x2048** | **4.00x** ⛔ |
| `glyphs_3x.png` | 254x256 | 762x768 | 768x768 | 3.02x ✅ |
| Blackrobe's 1536 | 385x512 | 1155x1536 | 1153x1536 | **3.00x** ✅ |

⭐ **So exactly ONE sheet is wrong — `flags_3x.png` — and `glyphs_3x.png` is fine.** An earlier
revision of this document claimed both were broken and shipped a patch dropping the glyphs
declaration. That patch has been **withdrawn**; it was a false positive from measuring canvases.

⚠ **And that single-sheet scope is exactly what the reporters describe:** lobby faction and game
flags, and nothing else. Blackrobe's own note that Dobry's cut-off *build cards* "could be
engine-level" is consistent — those are a separate issue, not this one.

## The cause

## ⭐ It was found and fixed once, in 2026-06, and reverted the same day

Full history (the clone is shallow by default — `git fetch --unshallow` first, or `git log` on
these files tells you nothing):

| commit | date | author | |
|---|---|---|---|
| `1326cc44e` | 2026-06-09 | Blackrobe | *"Try to rescale faction flags in lobby for big-scaled screens"* — `flags-3x.png` 2048 → **1536** |
| `ce2170c9b` | 2026-06-09 | Blackrobe | **Revert**, back to 2048. No reason recorded. |

**It reached neither the release nor dev.** Every revision since carries the 2048 file, including
tag `playtest-20260709` (the newest of 67) and `origin/master` today. So the answer to *"was it
before or after the release, or dev only"* is **none of those** — it existed for a few hours and
was undone.

⭐ **And the reverted file was good.** 1536 is exactly 3 × 512, arrived at independently. Its
colour density per pixel matches the 2x sheet that ships and works:

| icon | 1x | 2x (works) | 2x/1x | Blackrobe 1536 | 1536/1x |
|---|--:|--:|--:|--:|--:|
| gdi | 235 | 697 | 3.0x | 1,423 | 6.1x |
| XCOM | 478 | 1,712 | 3.6x | 3,368 | 7.0x |
| Warcraft | 510 | 1,975 | 3.9x | 4,163 | 8.2x |

2x holds 4× the pixels for ~3.4× the colours (0.85 per pixel); the 1536 sheet holds 9× the pixels
for ~7.0× the colours (0.78 per pixel). Same treatment, no evidence of a bad resample.

⚠ **Why was it reverted, then? The most likely answer is that it only fixed HALF the bug.**
`glyphs_3x.png` has been 1024px (4x of a 256 base) since 2020 and has never been touched by anyone.
Fixing flags alone leaves every editor/UI glyph still broken — so a tester at high DPI would still
see mangled UI and reasonably conclude the flags change had not worked.
**Fix both, or it will look like it failed again.**

⭐ **Decoded, the misread is unmistakable.** Reading each icon's 3x region out of the shipped 4x
sheet, distinct colours collapse as you move away from the origin — `Warcraft` in the far corner
returns **one flat colour**, i.e. the icon is not there at all:

| icon | colours @1x | read from the 4x sheet at 3x |
|---|--:|--:|
| gdi | 235 | 54 |
| nod | 219 | 84 |
| XCOM | 478 | 37 |
| Warcraft | 510 | **1** |

## The fix

**Faction icons — `docs/patches/chrome_06_restore_flags_3x.sh`. No new art required.** The correct
1536px sheet is already in this repository's history; restoring it is one line:

```bash
git show 1326cc44e:mods/cameo/uibits/flags-3x.png > mods/cameo/uibits/flags_3x.png
```

(The hyphen→underscore rename landed later, in `938e988d2`, so the paths differ.) This keeps full
sharpness at high DPI — strictly better than dropping the declaration.

**Glyphs — nothing to do.** `glyphs_3x.png` measures 3.02x and is correct. The patch that dropped
its declaration was withdrawn.

⭐ **Nothing needs "dumbing down".** The 4x master art can stay in the project as the source — the
1x and 2x sheets generated from it by halving are already correct and already ship. The engine
simply caps at 3x, so the sheet `Image3x` points at must be 3x artwork. Adding `Image4x` to the
soft-forked engine would not help the reported players either: that branch would need
`dpiScale > 3`, i.e. above 300% display scaling.

⚠ **Do not "fix" this by inventing a file or by pointing `Image3x` at the 2x sheet** — the region
maths would then be wrong in the other direction.

## Not yet answered

* **Which scale lands in which bucket.** The exact `ChromeProvider` thresholds could not be read
  here: `engine/` is build output and is not part of this repository (CLAUDE.md rule 7). The
  diagnosis does not depend on them — a 4x sheet declared as 3x is wrong at any threshold — but
  whoever confirms the fix should note the scale at which the 3x path engages, so the report can be
  closed with a reproduction rather than an inference.
* **Three chrome files are declared but not present in the tree**: `loadscreen.png`,
  `ca-loading-artwork.png`, `ca-menu-logo.png` (`^LoadScreen`, `loading-artwork`, `menu-logo`).
  `^LoadScreen` is additionally inherited by nothing. The game boots, so these are either dead
  config or supplied outside `mods/cameo/`. Advisory in the audit, not a failure — worth a look,
  unrelated to this bug.
