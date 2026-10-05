# Locked Missile terminal accuracy experiment

Base: `codex/accuracy-fix@4c04909e608798da497c22c09efca67c0d1b42c1`.

`BM_HOMING_MISSILE_TERMINAL_ACCURACY` is a separate default-off switch. It does not alter the target-footprint switch or the serialized sidecars when disabled.

## Runtime model

The model follows the pinned `Missile.cs` behavior: a successful lock refreshes the target position each tick; the missile checks `CloseEnough` after its movement step; and explosion damage uses the missile's position. Fuel limit zero means weapon range, a negative limit is unlimited, and range modifiers are applied by the engine at runtime.

The conservative terminal bound is `CloseEnough + sqrt(2) × lock-on offset bound`. The extra term accounts for the engine's independent x/y aim offsets. A terminal bound is used only when the missile is always locked, `CloseEnough` exceeds maximum per-tick speed, fuel covers nominal weapon range, homing activates before the shot range, both turn rates are positive, and the bound fits inside the target HitShape plus the applicable warhead radius. For radial damage, falloff is evaluated at the worst-case distance from the target edge; no Gaussian travel-drift term is substituted.

The evaluator does not simulate range/inaccuracy modifiers, blockers, point defence, terrain interactions, or the complete turning trajectory. Positive turn rates are a necessary gate, not proof of convergence in every map geometry. These limitations are why the switch remains opt-in pending broader comparison and, ideally, headless engine measurements.

## Current examples

- `td_nod_attacksubmarine_nodtorptube`: terminal bound 298 WDist (default CloseEnough, no authored inaccuracy); reliability changes from 0.162 to 1.0 in the model.
- `RA2MultiHoverMissile_AA`: terminal bound about 671 WDist after the authored lock-on offset; reliability changes from 0.546 to 0.620.
- `mtank_pri2`: rejected because `CloseEnough` (default 298) is no larger than its speed. The terminal switch leaves its target-footprint-only result unchanged and it remains below 0.2.

The four runtime gates and aim-offset allowance have unit coverage. Full ruleset tool tests, extractor check, and comparison against a representative in-engine sample are still required before considering this behavior for default-on use.
