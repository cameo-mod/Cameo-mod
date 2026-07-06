# Porting guide: weapon & explosion glow

A screen-space **glow** effect for OpenRA: lasers, tesla bolts, muzzle flashes and
explosions cast a soft additive halo onto the scene. Off by default, YAML-configurable
per weapon/warhead, and gated by a single graphics setting.

This is a **porting guide, not a git patch** — it describes exactly what to add and where,
with drop-in source for the big pieces and small inline snippets for the rest. Follow it by
hand, or hand it to a coding agent to apply against whatever engine version your mod runs.

## What you get

- **Beam/laser glow** — a tapered halo along any `BeamRenderable` (railguns, lasers).
- **Tesla glow** — a halo along tesla zaps, independently tunable colour/scale/intensity.
- **Explosion / impact glow** — a burst of light at a warhead's impact position.
- All additive (screen blend), fading over time, and free when nothing is glowing.

## How it works

One world trait, `GlowRenderer`, is a full-screen **post-process pass** (`IRenderPostProcessPass`).
Effects call `GlowRenderer.RegisterGlow(...)` each frame with a source→target segment, colour,
radius and fade; the pass batches up to 20 glows and renders them in a single shader pass
(`postprocess_glow.frag`) that reads the world framebuffer and screen-blends the halos on top.
Nothing about the simulation changes — it's render-only cosmetic state.

## Compatibility

- Written against OpenRA **`bleed` @ `b0b0544d4a`**; the `IRenderPostProcessPass` framework it
  builds on is stock upstream, so this applies to any recent bleed-tracking engine.
- **Build-verified on pristine bleed** (`OpenRA.Mods.Common` + `OpenRA.Platforms.Default`, 0 errors).
- No threading, no new framework — one trait, one shader, one shader-loader tweak, one setting.

## Package contents

| File | Role |
|---|---|
| `GlowRenderer.cs` (companion) | The render service — drop in verbatim. |
| `postprocess_glow.frag` (companion) | The GLSL shader — drop in verbatim. |
| this guide | The 3 small hand-edits + how to trigger and configure glow. |

---

## Step 1 — Add the render service

Copy the companion **`GlowRenderer.cs`** into your engine at:

```
OpenRA.Mods.Common/Traits/World/GlowRenderer.cs
```

It's self-contained: namespace `OpenRA.Mods.Common.Traits`, implements `IRenderPostProcessPass`
+ `INotifyActorDisposing`, and exposes `RegisterGlow(...)`. If your mod keeps such traits in its
own assembly, put it there instead and adjust the namespace — nothing in the engine references
it by type, so it can live wherever your effects can reach it.

## Step 2 — Add the shader

Copy the companion **`postprocess_glow.frag`** into your engine's shader folder:

```
glsl/postprocess_glow.frag
```

It reuses the stock `glsl/postprocess.vert` vertex shader (already present in bleed). The
shader name `"glow"` in `GlowRenderer` maps to `postprocess_glow.{vert,frag}` automatically —
no manifest entry needed. `MAX_BEAMS` (20) in the shader must match `MaxBeamsPerBatch` in
`GlowRenderer.cs`; keep them in sync if you change it.

## Step 3 — Teach the shader loader about uniform arrays (required)

The glow shader uses `uniform vec3 GlowColors[20]` style arrays and sets them **per element**
(`GlowColors[0]`, `GlowColors[1]`, …). OpenGL only auto-registers the `[0]` name, and ANGLE/ES
rejects bulk `glUniformXfv` with `count > 1`, so the loader must pre-register every element
location. In `OpenRA.Platforms.Default/Shader.cs`, in the uniform-scanning loop of the
constructor, right after `uniformCache[sampler] = loc;`, add:

```csharp
// For uniform arrays, OpenGL reports the name as "Name[0]"; register the bare "Name"
// and pre-register all element locations ("Name[1]", "Name[2]", etc.).
// ANGLE/ES rejects glUniformXfv with count > 1, so callers set individual elements
// using these pre-registered per-element locations.
if (sampler.EndsWith("[0]", StringComparison.Ordinal))
{
    var bareName = sampler.Substring(0, sampler.Length - 3);
    uniformCache[bareName] = loc;
    for (var j = 1; j < uniformSize; j++)
    {
        var elemName = $"{bareName}[{j}]";
        var elemLoc = OpenGL.glGetUniformLocation(program, elemName);
        OpenGL.CheckGLError();
        uniformCache[elemName] = elemLoc;
    }
}
```

This needs the array length, so change the `glGetActiveUniform` call a few lines up from
`out _, out _, out var type` to capture the size:

```csharp
OpenGL.glGetActiveUniform(program, i, 128, out _, out var uniformSize, out var type, sb);
```

That's the only required `Shader.cs` change; `GlowRenderer` sets elements through the stock
`SetVec(string, float, …)` overloads. (A bulk `SetVec(string, float[], int components, int count)`
overload also exists in the source engine but glow does not use it — skip it unless other
effects need it.)

## Step 4 — Add the on/off setting

In `OpenRA.Game/Settings.cs`, inside the **`GraphicSettings`** class, add:

```csharp
[Desc("Enable screen-space glow effect along laser beams.")]
public bool LaserGlow = true;
```

Every trigger below checks `Game.Settings.Graphics.LaserGlow`, so this is the player-facing
master switch. (Optionally bind a checkbox to it in your Display settings — not required.)

## Step 5 — Enable the pass in YAML

Add the trait to your world actor (e.g. `world` in `rules/world.yaml`):

```yaml
World:
    GlowRenderer:
```

## Step 6 — Trigger glow from your effects

Glow is opt-in: something has to call `RegisterGlow`. The three patterns below cover weapons
and explosions — copy whichever you need into your own renderables/warheads.

**Explosion / impact glow** — in an impact warhead (pattern from `CreateEffectWarhead.DoImpact`):

```csharp
// YAML-exposed fields on the warhead:
[Desc("Color of the glow at the impact position. Requires GlowScale > 0.")]
public readonly Color GlowColor = Color.FromArgb(255, 255, 102, 0);
[Desc("Scale of the glow effect at the impact position. Set above 0 to enable.")]
public readonly float GlowScale = 0f;
[Desc("Number of render frames for the glow to fade out over.")]
public readonly int GlowFadeFrames = 60;
[Desc("Number of render frames for the glow to fade in over. 0 = instant.")]
public readonly int GlowFadeInFrames = 0;

// at impact:
if (GlowScale > 0 && Game.Settings.Graphics.LaserGlow)
    world.WorldActor.TraitOrDefault<GlowRenderer>()
        ?.RegisterGlow(pos, pos, GlowColor, GlowScale, GlowFadeFrames, GlowFadeInFrames);
```

**Beam / laser glow** — at the end of a beam renderable's render (pattern from `BeamRenderable`):

```csharp
if (Game.Settings.Graphics.LaserGlow)
    wr.World.WorldActor.TraitOrDefault<GlowRenderer>()
        ?.RegisterGlow(Pos, Pos + length, color, width.Length / 86f);
```

**Tesla glow** — thread glow fields from the projectile into its renderable, then (pattern from
`TeslaZapRenderable`):

```csharp
// projectile (e.g. TeslaZap) YAML fields:
public readonly Color GlowColor = Color.FromArgb(160, 200, 255);
public readonly float GlowScale = 1f;      // 0 disables
public readonly float GlowIntensity = 1.65f;

// in the renderable's render:
if (Game.Settings.Graphics.LaserGlow && glowScale > 0f && length.Length != 0)
    wr.World.WorldActor.TraitOrDefault<GlowRenderer>()
        ?.RegisterGlow(Pos, Pos + length, glowColor, glowScale, intensity: glowIntensity);
```

### `RegisterGlow` signature reference

```csharp
void RegisterGlow(
    WPos source, WPos target, Color color,
    float scale = 1f,          // radius at source
    int fadeFrames = 0,        // 0 = one-frame flash; >0 fades out over N render frames
    int fadeInFrames = 0,
    float intensity = 1f,      // brightness only (does not grow radius)
    float scaleEnd = -1f,      // taper radius to this at target; -1 = uniform
    float endpointBoost = 0f,  // separate brighter pool at the target (0 = off)
    float selfBrighten = 0f,   // radial gamma-lift of the scene under the glow
    float fadeEnd = 1f,        // body brightness at the target end
    float edgeExponentStart = 2f, float edgeExponentEnd = 2f, // 2 = Gaussian edge
    float endpointSquash = 1f, // flatten the endpoint pool into a ground ellipse
    float poolScale = 0f)      // endpoint pool radius
```

For a plain weapon/explosion glow you only need the first few args; the rest shape searchlight-
style cones and ground pools.

## Step 7 — Author it in YAML

Once wired, modders tune glow entirely in weapon/warhead YAML — no code per weapon:

```yaml
# explosion glow on a warhead
Warhead@glow: CreateEffect
    GlowColor: FF6600
    GlowScale: 3
    GlowFadeFrames: 45

# tesla bolt glow
Projectile: TeslaZap
    GlowColor: A0C8FF
    GlowScale: 1.2
    GlowIntensity: 1.65
```

The **"Weapon Glow Effects"** master switch is the `LaserGlow` setting from Step 4.

## Step 8 — Build & verify

```sh
dotnet build OpenRA.Mods.Common/OpenRA.Mods.Common.csproj -c Release
dotnet build OpenRA.Platforms.Default/OpenRA.Platforms.Default.csproj -c Release
```

Both should report 0 errors (verified on bleed). Then in-game: fire a glow-enabled weapon or
detonate a glow warhead and confirm the halo renders; toggle the `LaserGlow` setting off and
confirm it disappears. On a fullscreen/ANGLE build, verify Step 3 took (arrays of >1 element
render correctly rather than only the first beam).

## Notes

- Everything is gated by `GlowScale > 0` and the `LaserGlow` setting, so applying this changes
  nothing until a weapon opts in.
- `MAX_BEAMS`/`MaxBeamsPerBatch` (20) bounds glows per batch; extras spill to the next batch.
- Render-only and single-threaded — no sim/determinism impact.
