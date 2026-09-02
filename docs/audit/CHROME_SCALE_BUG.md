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

## The cause

`mods/cameo/chrome.yaml` declares each collection's regions **once, in 1x coordinates**, and
`ChromeProvider` (`OpenRA.Game/Graphics/ChromeProvider.cs`) multiplies them by 2 or 3 to index into
the `Image2x` / `Image3x` sheet. That arithmetic is only correct if the sheet really is 2x / 3x.

**Two sheets are 4x, declared as 3x:**

| collection | base | `Image2x` | `Image3x` | |
|---|--:|--:|--:|---|
| `^Flags` (`chrome.yaml:246`) | `flags.png` 512x512 | `flags_2x.png` 1024 ✅ 2x | `flags_3x.png` **2048** | ⛔ 4x |
| `^Glyphs` (`chrome.yaml:13`) | `glyphs.png` 256x256 | `glyphs_2x.png` 512 ✅ 2x | `glyphs_3x.png` **1024** | ⛔ 4x |

3x of 512 is **1536**, not 2048. Every other collection in the file is unaffected — 71 of them
declare only `Image` and have no variants at all.

⭐ **`^Flags` is the faction and game icons. `^Glyphs` is editor/UI glyphs.** That is precisely
*"faction icons and some UI"*, and nothing else — the report's own scope is the strongest
confirmation of the diagnosis.

## Why the corner looks fine and the rest does not

Reading at 3x from a 4x sheet, a region at 1x `(x, y)` is fetched from `(3x, 3y)` while its art
actually sits at `(4x, 4y)`. **The error is proportional to distance from the top-left corner:**

| icon | 1x region | engine reads | art actually at | correct pixels |
|---|---|---|---|--:|
| `gdi` | 0, 0, 32, 16 | 0,0 96x48 | 0,0 128x64 | **100%** |
| `nod` | 0, 16, 32, 16 | 0,48 96x48 | 0,64 128x64 | 67% |
| `XCOM` | 352, 224, 32, 16 | 1056,672 | 1408,896 | **0%** |
| `Warcraft` | 160, 496, 32, 16 | 480,1488 | 640,1984 | **0%** |

So the first icons in the sheet render perfectly and everything further right or down degrades into
neighbouring artwork. That is why it reads as *"some* UI" and why it survived unnoticed.

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

**Glyphs — `docs/patches/chrome_05_drop_glyphs_3x_declaration.patch`.** No correct 3x glyph sheet
has ever existed, so there is nothing to restore. Dropping the declaration makes the engine fall
back to a smaller correct sheet: slightly softer, right pixels. Absence is the normal case here
(71 collections do it), so there is no missing-asset risk. Replace it whenever someone exports
`glyphs_3x.png` at exactly **768x768**; the audit will confirm the size.

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
