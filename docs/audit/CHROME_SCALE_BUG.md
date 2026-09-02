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

## How OpenRA and Combined Arms do it — measured, not assumed

Blackrobe's objection was *"why 3x and not 4x like it is also for CA and OpenRA"*. He is right
about what those files look like, and the resolution is that **canvas size and artwork scale are
two different things.** Measured across all three projects:

### Combined Arms (`Inq8/CAmod`, `mods/ca/uibits/`)

| file | canvas | **artwork** | implied |
|---|--:|--:|--:|
| `glyphs.png` | 256x512 | 254x272 | 1x |
| `glyphs-2x.png` | 512x1024 | 509x544 | **2.00x** |
| `glyphs-3x.png` | **1024x2048** | **763x816** | **3.00x** |

⭐ CA's `-3x` file has a canvas **4x** the base — exactly what Blackrobe saw — while its **artwork
is exactly 3x**. The extra canvas is padding, nothing more.

⭐ **And CA's `flags:` collection declares only `Image: flags.png` — no `Image2x`, no `Image3x`.**
CA cannot have this bug for flags because CA never loads a scaled flag sheet at all. "It doesn't
happen in CA" is true, and it is not because CA solved 4x.

### Upstream OpenRA (`mods/ra/uibits/`)

| file | canvas | **artwork** | implied |
|---|--:|--:|--:|
| `glyphs.png` | 256x256 | — | 1x |
| `glyphs-2x.png` | 512x512 | 512x512 | 2.00x |
| `glyphs-3x.png` | **1024x1024** | **768x768** | **3.00x** |

Same convention: 3 x 256 = 768 is not a power of two, so the sheet is padded to 1024. All twelve
of upstream's variant declarations look "4x" by canvas; every one is 3x artwork.

### Cameo

| file | canvas | **artwork** | implied | |
|---|--:|--:|--:|---|
| `glyphs_3x.png` | 1024x1024 | 768x768 | 3.02x | ✅ matches the convention |
| `flags_3x.png` | 2048x2048 | **1536x2048** | **4.00x** | ⛔ **the only outlier anywhere** |
| Blackrobe's reverted 1536 | 1536x1536 | 1153x1536 | **3.00x** | ✅ correct |

**So "make it work exactly like OpenRA and Combined Arms" means: artwork at 3x.** One file in one
project is out of line, and restoring the already-authored replacement puts it back in line.

### ⛔ Can ONLY the highest-resolution sheet be used, scaled correctly?

No — and the blocker is one line in the engine. `ImageWidget.Draw()`:

```csharp
WidgetUtils.DrawSprite(GetSprite(), RenderOrigin.ToVector2());
```

It draws the sprite at its **native size** at the widget's origin. The `Width:` / `Height:` on a
chrome `Image@FLAG:` widget (e.g. `ingame_observer.yaml:1207`, `Width: 35`) only lay the widget
out; they do **not** scale what is drawn. The only thing that makes a flag render at 32x16 instead
of its raw pixel size is the `1f / density` scale that `ChromeProvider` bakes into the `Sprite` —
and `density` comes from the hardcoded 1/2/3 ladder.

So pointing the base `Image` at the 2048 sheet (with regions rewritten to 4x) would fetch the right
pixels and then draw every flag at **128x64** in a 35px-wide slot. There is no yaml-side offset or
scale that fixes that. **All three variants must share one set of 1x regions, so each must be laid
out at exactly its declared density.**

### ⛔ Can it be 4x instead?

No, and not for a stylistic reason:

* `ChromeProvider.Collection` declares **only** `Image`, `Image2x`, `Image3x`. There is no
  `Image4x` field to point at.
* `density` is a hardcoded literal, never measured from the sheet.
* Adding `Image4x` to the soft-forked engine would follow CLAUDE.md rule 7 and is only a few
  lines — but it **would not help these players**. The existing ladder is `dpiScale > 2` for 3x, so
  a 4x branch would need `dpiScale > 3`: above **300% display scaling**. Nobody in the report is
  near that, and it would put Cameo's chrome pipeline permanently out of step with both upstreams
  for a case that essentially never fires.

⚠ **Nothing has to be "dumbed down" to do this.** The 4x master art stays in the project as the
source. The 1x and 2x sheets halved from it are already correct and already ship. Only the sheet
that `Image3x` points at has to be 3x artwork — and that file already exists in this repository's
history.

⚠ **One convention detail if the sheet is ever re-exported rather than restored:** CA and upstream
both pad the 3x artwork into a power-of-two canvas (1024, 2048). Blackrobe's file is 1536x1536,
which is not a power of two. OpenRA handles non-POT sheets, and this one measured correct — but
padding 1155x1536 of artwork into a 2048x2048 canvas would match the peers exactly.

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

### The three options, and why "exactly like CA" is not the best one

| | what it does | risk | sharpness |
|---|---|---|---|
| **Restore the 3x sheet** (`chrome_06_restore_flags_3x.sh`) ⭐ **recommended** | puts back Blackrobe's verified 3.00x file | none measurable; the asset is in history and was measured | full at every scale |
| **Drop `Image3x`** (`chrome_07_drop_flags_3x_FALLBACK.patch`) | engine falls back to the correct 2x sheet above 200% scaling | none — removes the failure mode by construction | slightly soft above 200% |
| **Copy CA exactly** (drop `Image2x` too) | CA's `flags:` declares no variants at all | none | ⛔ **worse for most users** |

⚠ **Do not copy CA literally.** CA's flags collection has no scale variants, so CA renders the 1x
sheet at every DPI. Doing that here would throw away Cameo's `flags_2x.png`, which is **verified
correct at 2.00x** and covers the common HiDPI case (any 200%-scaled laptop). Only the 3x path was
ever broken; the 2x path has always been right. "Like CA" is the right instinct about *outcome* —
no broken flags — but the literal configuration would be a quality regression.

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
* **Dead config inherited from CA, resolved as advisory.** `loading-artwork` and `menu-logo`
  (`chrome.yaml:1143`, `:1150`) declare `ca-loading-artwork*.png` and `ca-menu-logo*.png`, which
  exist in Combined Arms but were never copied into Cameo. `^LoadScreen` likewise names a missing
  `loadscreen.png` and is inherited by nothing. **Nothing under `mods/cameo/chrome/` references any
  of them**, so they never load and the game boots fine — `ChromeProvider` opens a sheet lazily,
  on first sprite request. Harmless today, a crash if anything ever asks for them. Left alone
  deliberately: removing them is a yaml change needing a boot gate for zero present benefit.
