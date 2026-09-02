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

**The unshipped engine alternative** would modify the **`cameo-mod/OpenRA`** soft-fork to replace
the if/else ladder with:

> pick the **smallest declared variant whose density covers `dpiScale`**, and fall back to the
> largest declared one when none does.

⭐ **Why that shape rather than just bolting on a fourth `else if`:** an extra branch would need
`dpiScale > 3`, so the 4x sheet would only load above **300% display scaling** — no help to anyone
in the bug report. The loop instead selects `Image4x` for anything above 2x scaling when no 3x
sheet is declared, which is exactly the band where the bug bites, and the extra pixels simply
supersample.

⚠ **It touches every chrome sheet in the mod, so the safety claim is the whole argument**:
behaviour should be
**identical to upstream at every dpiScale** for every collection shape that exists — `Image` alone,
and the full `Image`/`Image2x`/`Image3x` triple — including the exact boundaries 1.0, 2.0, 3.0.
Measured across Cameo, upstream ra/cnc and Combined Arms, those are the only two shapes any of them
use.

⚠ **One honest divergence, found by the test sweep rather than by reading:** a collection declaring
`Image` + `Image3x` with **no** `Image2x` gets the 3x sheet at dpiScale 1.5 where upstream falls
back to 1x. Arguably better, but a difference. **Nothing anywhere declares that shape**, and
`test_the_divergent_shape_is_unused` fails the day something does.

**This branch deliberately does not take that engine route.** Stock `ChromeProvider` has no
`Image4x` field, so `flags_4x.png` is an art source only; the committed `Image3x` sheet is generated
at the correct density and the engine continues to use the stock ladder.

**The cost**, stated plainly: it is a permanent divergence from upstream that every engine update
must carry, it needs the full rule-7 pipeline (edit the `cameo-engine` clone → push → set
`ENGINE_VERSION` in `mod.config` → `make.cmd all` → boot gate), and it cannot be compiled or booted
from a cloud container. Against that: the 4x artwork is used at full resolution and no art is ever
downscaled.

### The three options### ⭐ THE RECOMMENDED ANSWER — one master, the rest generated

Maintainer, 2026-09-02: *"keep the 4x as a base which is used for all the edits and then write a
tool that automatically creates the 1x, 2x and 3x versions from the 4x so we only need to edit one
file and the rest is created automatically."*

**`tools/art/generate_chrome_scales.py`** does exactly that:

```bash
python tools/art/generate_chrome_scales.py flags --master flags_4x.png --emit 1,2,3 --write
python tools/audit/audit_chrome_scale_variants.py     # PASS
# --- BOOT GATE --- generated sheets are engine content
```

⭐ **It removes the bug class rather than the bug.** A sheet's layout is proportional, so uniformly
resizing the master scales every icon's position and size together — the derived sheets are correct
**by construction**. There is no offset to get wrong and no icon that can drift. Verified on the
real master:

| generated | canvas | artwork | vs 1x |
|---|--:|--:|--:|
| `flags.png` | 512x512 | 387x512 | 1.00x |
| `flags_2x.png` | 1024x1024 | 771x1024 | 1.99x |
| `flags_3x.png` | 1536x1536 | 1153x1536 | 2.98x |
| `flags_4x.png` (master) | 2048x2048 | 1536x2048 | 3.97x |

⭐ The generated 3x sheet is **1153x1536 — the same dimensions as Blackrobe's reverted file**,
derived independently. His file was right.

**And it answers the rename question: yes, rename the master to `flags_4x.png`.** The engine never
sees it — the ladder stops at 3x, so a 4x master is an *art source*, not a chrome declaration, and
`--master` takes it by path precisely so it need not appear in `chrome.yaml` at all. The name then
states the density instead of lying about it.

⚠ **Two hazards it guards, both hit while building it**, and both are the same underlying error —
trusting the canvas or the filename instead of measuring the artwork, which is the error that
caused this bug in the first place:

* **It refuses to overwrite the master.** Cameo's 4x master is *named* `flags_3x.png`, so `--emit 3`
  would replace the highest-resolution source with its own downscale. That actually happened during
  testing and was only recoverable from a manual backup.
* **It refuses a padded master.** Upstream and CA pad 3x artwork into a power-of-two canvas;
  `glyphs_3x.png` is exactly that shape. Resizing such a master gives correct artwork in a nonsense
  canvas, so the tool detects it and stops — `glyphs` needs nothing and must not be "fixed".

Resampling uses Pillow (LANCZOS) when importable and a pure-Python area-average box filter
otherwise, and prints which ran. 4x -> 2x and 4x -> 1x are exact integer ratios; 4x -> 3x is a 0.75
resample, so a native 3x export from the art source is still slightly better if one exists — the
audit accepts either.

### The four options

| | what it does | needs | risk | result |
|---|---|---|---|---|
| **A. Restore a 3x sheet** | puts back Blackrobe's verified 3.00x file | boot gate | none — the asset is in history and measured | correct and sharp everywhere |
| **B. Use the 4x directly** | adds `Image4x` and a generalised ladder | **engine rebuild** + boot gate | small but engine-wide | full 4x resolution, no art ever downscaled |
| **C. Drop `Image3x`** | falls back to the correct 2x sheet above 200% | boot gate | none — removes the failure mode by construction | slightly soft above 200% |
| **D. Copy CA literally** (drop `Image2x` too) | CA's `flags:` declares no variants at all | boot gate | none | ⛔ **worse for most users** |

⭐ **E. Generate everything from the 4x master** (`tools/art/generate_chrome_scales.py`, above) is
the recommended route: it needs no engine change, no new art, and no one to remember anything.
A is the smallest one-off fix; B is the only way the 4x pixels themselves reach the screen.
A, B and E are mutually exclusive at the `Image3x` slot — A points `Image3x` at a 3x sheet, B points `Image4x` at the
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

## ✅ ANSWERED — which scale lands in which bucket, and what a 4x rung would need

Measured from `cameo-mod/OpenRA` @ `2b3da9e`, not inferred. Four files decide everything:

| file | fact |
|---|---|
| `OpenRA.Game/Graphics/ChromeProvider.cs:117` | `if (dpiScale > 2 && Image3x != null) density = 3; else if (dpiScale > 1 && Image2x != null) density = 2;` |
| `OpenRA.Game/Renderer.cs:386` | `WindowScale => Window.EffectiveWindowScale` |
| `OpenRA.Platforms.Default/Sdl2PlatformWindow.cs:77-82` | `EffectiveWindowScale = windowScale * scaleModifier` — **the OS display scale times the game's UI Scale setting** |
| `OpenRA.Mods.Common/Widgets/Logic/Settings/DisplaySettingsLogic.cs:628` | `validScales = { 1f, 1.25f, 1.5f, 1.75f, 2f }` filtered by `maxScale = NativeResolution / MinEffectiveResolution` |

`windowScale` is the OS scaling factor (`SDL_GetDisplayDPI / 96` on Windows, `Xft.dpi / 96` or
`GDK_SCALE` on Linux, GL-pixels-per-point on macOS), overridable with **`OPENRA_DISPLAY_SCALE`**.
`MinEffectiveResolution` is the engine default **1024x720** — `mods/cameo/mod.yaml:523` sets only
`DefaultScale` / `MaxZoomScale` / `MaxZoomWindowHeight`, so Cameo does not move it.

⭐ **The report said "150%" and the threshold is `> 2`; both are right.** The setting is only half
the product. Windows at 150% display scaling *with* the game's UI Scale at 150% gives
`1.5 x 1.5 = 2.25`, which trips the 3x branch. On a plain 100%-DPI monitor the UI Scale dropdown
stops at 200%, `1.0 x 2.0 = 2.0` is **not** `> 2`, and **the 3x sheet is never loaded at all**.

⚠ **The two ceilings multiply out to one number.** `NativeWindowSize` is the *logical* size
(`surfaceSize / windowScale`, Sdl2PlatformWindow.cs:280), so the OS scale cancels:

```
dpiScale  =  nativeScale x UIScale
UIScale   <=  min(logicalW/1024, logicalH/720)  =  min(physW, physH-based) / nativeScale
=>  dpiScale <= min(physW/1024, physH/720)      AND      dpiScale <= 2 x nativeScale
```

**Worked out over every offered UI Scale — by `tools/art/chrome_density_reach.py`, not by hand:**

| panel | max `dpiScale` | at OS scale x UI Scale | sheet today | with `Image4x` |
|---|--:|---|---|---|
| 1920x1080 | 1.50 | 100% x 150% | 2x | 2x |
| 2560x1440 | 2.00 | 100% x 200% | 2x | **2x** — `> 2` is strict, so 3x never loads |
| 3840x2160 (4K) | **3.00** | 150% x 200% | 3x | **3x — a 4x rung would be DEAD here** |
| 3456x2234 (16in Retina) | 3.06 | 175% x 175% | 3x | **4x** ⭐ |
| 5120x2880 (5K) | **4.00** | 200% x 200% | 3x | **4x** ⭐ |

⛔ **The table is generated, and that is not fastidiousness.** The first hand-written version got
two of these five rows wrong: 1920x1080 was quoted as 1.87 — that is the WIDTH term, while the
HEIGHT term binds at 1.50 — and the 16in Retina row was quoted as 3.00 when it actually reaches
3.06 and therefore *would* use a 4x sheet. Two ceilings multiply and a `min()` picks between four
terms; that is one step past what is safe to do in your head.

⛔ **So a 4x rung would be dead on 4K**, the most common high-DPI setup there is: it lands on
exactly 3.00 across every combination, and the 4x test is `dpiScale > 3`. It needs
`min(physW/1024, physH/720) > 3` **and** an OS scale above 150% — a 5K/6K/8K panel, a 4K 16:10
panel at 250%, or a 3456x2234-class Retina at 175%.

⭐ **THE TRIAGE RULE FOR THE NEXT REPORT.** Because `NativeWindowSize` is logical, the reachable
`dpiScale` is bounded by the physical panel alone — which turns "is this report even this bug?"
into one check on the numbers a reporter can read off their own display:

> **If `min(width/1024, height/720) <= 2`, that machine cannot load the 3x sheet at ANY setting,
> so the report is a different bug.**

⛔ **This clears the common ultrawides.** 2560x1080 reaches 1.50 and 3440x1440 reaches exactly
2.00 — and the test is `> 2` — so neither ever touches the malformed sheet. An ultrawide report of
broken flags is a separate layout problem and needs its own screenshot. Same for 2560x1440.
`python tools/art/chrome_density_reach.py 3440x1440` answers it for any panel.

⭐ **Two escape hatches make it testable without the hardware**, which is how to decide this
empirically rather than by argument: `OPENRA_DISPLAY_SCALE` forces `nativeScale` on Windows and
Linux, and `Graphics.UIScale` written straight into `settings.yaml` is **not** clamped to the
dropdown's 2.0 — only `BlankLoadScreen.cs:117` touches it, and only to reset to 1.0 when the
resolution is below `MinEffectiveResolution`.

⚠ **And glyphs would need the same treatment**, but only if flags gets it: `glyphs_3x.png` is a
correct padded 3x sheet, so a 4x rung would silently keep using it — no breakage, just no gain.

## Not yet answered

* **Dead config inherited from CA, resolved as advisory.** `loading-artwork` and `menu-logo`
  (`chrome.yaml:1143`, `:1150`) declare `ca-loading-artwork*.png` and `ca-menu-logo*.png`, which
  exist in Combined Arms but were never copied into Cameo. `^LoadScreen` likewise names a missing
  `loadscreen.png` and is inherited by nothing. **Nothing under `mods/cameo/chrome/` references any
  of them**, so they never load and the game boots fine — `ChromeProvider` opens a sheet lazily,
  on first sprite request. Harmless today, a crash if anything ever asks for them. Left alone
  deliberately: removing them is a yaml change needing a boot gate for zero present benefit.

---

## The guard that catches the NEXT mistake: `audit_chrome_master_freshness.py`

⛔ **`audit_chrome_scale_variants.py` cannot catch the likeliest future failure.** It measures each
sheet's artwork EXTENT against its declared density, which is exactly right for the bug that
shipped — a sheet laid out at 4x sitting in the 3x slot. It is blind to this: someone edits one
faction icon inside `flags_4x.png`, commits, and the 1x/2x/3x sheets keep the OLD icon. Every
extent still matches, every dimension is still right, the audit passes, and three of the game's
four scales render stale art.

So the freshness audit compares **content**. `generate_chrome_scales.py --write` records the
master's SHA-256 and each derived sheet's SHA-256 in `tools/art/chrome_masters.json`; the audit
re-hashes and reports **which side moved**:

| master | derived | verdict |
|---|---|---|
| changed | unchanged | ⛔ edited and never regenerated — **`--fix` regenerates** |
| unchanged | changed | ⛔ a generated sheet was hand-edited — **`--fix` is refused** |
| changed | changed | ⚠ regenerated without re-stamping, or both edited |

⛔ **`--fix` is deliberately NOT offered for the second row.** Regenerating pulls from an unchanged
master, so it would overwrite the hand edit it just flagged — the guard would destroy the work it
found. The remedy is to port the change into the master first.

⚠ **Hashes, not mtimes.** A checkout, a stash pop or a rebase rewrites mtimes without changing a
pixel, and git does not preserve them at all.

⚠ **`--fix` does not clear the boot gate** and says so: it writes PNGs under `mods/`, exits
non-zero, and tells you to launch the game. The stamp itself lives under `tools/art/` precisely
because it is tooling metadata — it can be committed in a tree that cannot boot, while the PNGs it
describes cannot.

Wired into the blocking loop in `tools/audit/run_all.sh`; nine tests in
`tools/tests/test_audit_chrome_master_freshness.py`.
