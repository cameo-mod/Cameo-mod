# Day–Night and Weather Cycle Feasibility

## Conclusion

Both systems are implementable. Cameo already contains most of the visual weather infrastructure; the main missing component is a deterministic runtime cycle controller. A convincing day–night cycle is also feasible, but smooth terrain-lighting transitions require careful engine-side performance work.

## Existing Foundation

Cameo already has:

- Rain, sandstorm, and blizzard particle overlays, including fade-in and fade-out support.
- Weather-specific ambient sounds and thunder.
- Weather-specific terrain and unit tinting.
- Localized terrain light sources.
- Searchlights currently gated by the weather lobby option.
- An engine API for changing the global ambient tint during a match.

The active weather configuration is in `mods/cameo/rules/world.yaml` and `mods/cameo/rules/palettes.yaml`. The ambient-tint bridge is implemented by `OpenRA.Mods.Cameo/Traits/World/ConditionalWorldTint.cs`.

Currently, selecting **Weather** grants one tileset-specific condition at match start. It never changes afterward.

## Recommended Architecture

Add a Cameo world trait—conceptually named `EnvironmentCycle`—which owns a synchronized timeline:

```text
Dawn → Day → Dusk → Night → Dawn
                  ↓
       Clear → Weather → Clear
```

The controller would:

- Derive its state from the simulation tick so multiplayer clients, observers, saves, and replays agree.
- Grant and revoke `weather-rain`, `weather-sandstorm`, or `weather-blizzard`.
- Interpolate day–night ambient colour and brightness.
- Use tileset climate rules: rain on temperate and jungle, sandstorms on desert and Arrakis, and blizzards on snow.
- Offer lobby modes such as `Off`, `Visual Cycle`, and potentially `Visual + Gameplay`.

The existing `WeatherOverlay` system already supports gradual particle activation and removal. Cosmetic particle randomness can remain client-local; only the timing and selected weather state must be synchronized.

## Main Technical Limitation

`TerrainLighting.SetAmbientTint()` currently calls `RefreshGlobalLighting()`, which walks every map cell and rebuilds cached terrain tint data.

Consequences:

- Abrupt changes are already practical.
- Calling it every tick for a perfectly smooth sunrise would be wasteful, especially on large maps.
- A prototype could update lighting in visible steps every 10–25 ticks.
- A production-quality smooth cycle should optimize the engine path, either by separating sprite and terrain updates or moving the global multiplier into a cheaper render-time mechanism while preserving `IgnoreWorldTint`.

This is the principal engineering problem. The cycle state machine itself is straightforward.

## Practical Visual Scope

A strong first version could support:

- Warm dawn and dusk.
- Neutral daylight.
- Cool, darker nighttime.
- Rain, blizzard, and sandstorm intervals.
- Gradual particle appearance and disappearance.
- Weather audio.
- Searchlights enabled at night instead of whenever the weather option is active.
- Existing `TerrainLightSource` illumination becoming more prominent at night.

It would not automatically provide:

- Moving sun shadows.
- Directional lighting.
- Building-window illumination.
- Wet terrain, puddles, snow accumulation, or texture replacement.
- True fog volumes or cloud shadows.

Those would require separate rendering or asset systems. The current lighting model is essentially a global tint plus circular local lights.

## Gameplay Effects

Weather could affect gameplay, but this should remain outside the first implementation. Possible later effects include:

- Reduced vision during storms.
- Aircraft or projectile accuracy modifiers.
- Movement penalties.
- Solar-power variation.
- Night-specific stealth or detection.

These greatly increase balance, AI, multiplayer, and map-compatibility risk. A visual-only cycle is render-focused and can remain determinism-safe. Gameplay weather requires conditions to propagate consistently to every relevant actor and bot decision.

## Feasibility Assessment

| Feature | Feasibility | Main concern |
|---|---:|---|
| Weather cycle | High | Runtime state controller and clean audio transitions |
| Visual day–night cycle | High | Cost of refreshing terrain tint during smooth transitions |
| Directional sun and moving shadows | Low without major renderer work | No existing directional-light or dynamic-shadow system |
| Gameplay-affecting environment cycle | Feasible, higher risk | Balance, AI, synchronization, and map compatibility |

## Recommended First Prototype

Build a visual-only prototype on one temperate map with:

1. Fixed-duration dawn, day, dusk, and night phases.
2. One scheduled rain interval.
3. Quantized tint updates rather than per-tick terrain refreshes.
4. Existing weather particle fades and ambient audio.
5. Searchlights gated by nighttime.

This prototype would establish whether the artistic result is worthwhile before optimizing the lighting API or introducing gameplay effects.

## Validation Status

This report is based on a static audit of the active Cameo configuration and current source code. It is not runtime or framebuffer validation. The existing weather-tint implementation was historically built but recorded as not yet tested in-game.
