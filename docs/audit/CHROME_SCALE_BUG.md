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

## The fix

**Mitigation — `docs/patches/chrome_05_drop_mis_scaled_3x_sheets.patch`.** Drops the two `Image3x:`
declarations. The engine falls back to a smaller correct sheet: slightly softer at high DPI, but
the *right pixels*. Absence is the normal case here (71 collections do it), so there is no risk of
a missing-asset failure. Two lines, reversible, and it unbreaks every affected player immediately.

**Proper fix — re-export `flags_3x.png` at 1536x1536 and `glyphs_3x.png` at 768x768**, from the
art source rather than by downscaling the 2048/1024 files, then restore the two declarations. The
audit will confirm the sizes.

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
