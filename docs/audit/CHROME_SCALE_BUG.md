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

### ⭐ Can the 4x file be used DIRECTLY? Yes — with a ~10-line engine change

Not from yaml alone (there is no `Image4x` field to point at, and `density` is a hardcoded
literal). But with C# on the table it is small, and two facts make it smaller than it looks:

* `Collection` is built by **`FieldLoader.Load<Collection>(yaml)`**, which walks the type's public
  fields — so adding `public readonly string Image4x = null;` is the *entire* yaml side.
* The sprite maths is **already density-agnostic**: `density * mi` and `1f / density` work for 4
  exactly as for 3. Nothing else needs touching.

⛔ **A mod-side shadow cannot do it** — the check CLAUDE.md rule 7 requires first.
`ChromeProvider` is a `public static class` in `OpenRA.Game`, called directly at compile time and
never constructed through `ObjectCreator`, so the assembly-order trick does not apply. (`ImageWidget`
*is* ObjectCreator-resolved and could be shadowed, but it would need collection-specific scaling
inside a generic widget plus every flag region rewritten x4 — worse than the engine change.)

**The change**, `docs/patches/ENGINE_image4x_chromeprovider.patch` — for the **`cameo-mod/OpenRA`
soft-fork**, not this repository. It replaces the if/else ladder with:

> pick the **smallest declared variant whose density covers `dpiScale`**, and fall back to the
> largest declared one when none does.

⭐ **Why that shape rather than just bolting on a fourth `else if`:** an extra branch would need
`dpiScale > 3`, so the 4x sheet would only load above **300% display scaling** — no help to anyone
in the bug report. The loop instead selects `Image4x` for anything above 2x scaling when no 3x
sheet is declared, which is exactly the band where the bug bites, and the extra pixels simply
supersample.

⚠ **It touches every chrome sheet in the mod, so the safety claim is the whole argument** and it is
tested rather than asserted (`tools/tests/test_chrome_density_ladder.py`, 11 tests): behaviour is
**identical to upstream at every dpiScale** for every collection shape that exists — `Image` alone,
and the full `Image`/`Image2x`/`Image3x` triple — including the exact boundaries 1.0, 2.0, 3.0.
Measured across Cameo, upstream ra/cnc and Combined Arms, those are the only two shapes any of them
use.

⚠ **One honest divergence, found by the test sweep rather than by reading:** a collection declaring
`Image` + `Image3x` with **no** `Image2x` gets the 3x sheet at dpiScale 1.5 where upstream falls
back to 1x. Arguably better, but a difference. **Nothing anywhere declares that shape**, and
`test_the_divergent_shape_is_unused` fails the day something does.

**The mod side** is `docs/patches/chrome_08_flags_as_image4x.patch`: `^Flags` declares
`Image4x: flags_3x.png` instead of `Image3x`. ⛔ **Never apply it without the engine patch** —
stock `ChromeProvider` has no `Image4x` field, `FieldLoader` would silently drop the line
(CLAUDE.md rule 8b) and flags would quietly fall back to the 2x sheet.

**The cost**, stated plainly: it is a permanent divergence from upstream that every engine update
must carry, it needs the full rule-7 pipeline (edit the `cameo-engine` clone → push → set
`ENGINE_VERSION` in `mod.config` → `make.cmd all` → boot gate), and it cannot be compiled or booted
from a cloud container. Against that: the 4x artwork is used at full resolution and no art is ever
downscaled.

### The three options### The four options

| | what it does | needs | risk | result |
|---|---|---|---|---|
| **A. Restore the 3x sheet** (`chrome_06_*.sh`) | puts back Blackrobe's verified 3.00x file | boot gate | none — the asset is in history and measured | correct and sharp everywhere |
| **B. Use the 4x directly** (`ENGINE_image4x_*` + `chrome_08_*`) ⭐ **what "use the 4x file" means** | adds `Image4x` and a generalised ladder | **engine rebuild** + boot gate | small but engine-wide; backward compatibility is tested | full 4x resolution, no art ever downscaled |
| **C. Drop `Image3x`** (`chrome_07_*_FALLBACK.patch`) | falls back to the correct 2x sheet above 200% | boot gate | none — removes the failure mode by construction | slightly soft above 200% |
| **D. Copy CA literally** (drop `Image2x` too) | CA's `flags:` declares no variants at all | boot gate | none | ⛔ **worse for most users** |

**A is the smallest change that fixes it. B is the one that honours "use the highest-resolution
file".** They are mutually exclusive — A points `Image3x` at a 3x sheet, B points `Image4x` at the
4x sheet. C is the belt-and-braces fallback if either looks wrong on a real machine.

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
