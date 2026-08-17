# Recolorable hex-shield visuals

## Runtime contract

Shield mechanics still decide whether an actor is shielded. The visual traits retain their
existing conditions, upgrade and aura behavior. This system changes only the overlay image,
sequence, and fixed faction palette.

The shared geometry policy is:

- infantry: `hexshield_infantry` / `infantry-standard`;
- ordinary vehicles, ships, and aircraft: `hexshield_sphere` / `sphere-medium`;
- dreadnought-scale mobile actors: `hexshield_sphere` / `sphere-large`;
- defenses: `hexshield_dome` / `dome-small`;
- buildings: `hexshield_dome` / `dome-medium`;
- elongated mobile actors: explicit opt-in to `hexshield_directional_oval` /
  `directional-oval-large`.

Additional generic `sphere-small` and `dome-large` tiers are available for explicit actor or
template overrides. Shared assets and sequence names must remain actor- and faction-neutral.

Palettes are independent from geometry: default and Protoss shields are blue, Ixian shields
are silver, Yuri shields are indigo, and Consortium shields are cyan. Idle and hit palettes
retain 25% and 50% alpha.

## Choosing a tier

Class templates supply the normal tier. Override both shield overlays only when the inherited
tier is visibly too small or excessively loose:

```yaml
WithIdleOverlay@shield1:
	Image: hexshield_sphere
	Sequence: sphere-large
	StartSequence: sphere-large
WithIdleOverlay@shield_damage:
	Image: hexshield_sphere
	Sequence: sphere-large
```

Do not copy a sequence under an actor-specific name. Add a new generic tier only when multiple
actors can plausibly reuse it. Directional ovals are deliberate actor-level choices because
their facings and silhouette must be reviewed in game.

## Performance and fit policy

The eight shared sequences reuse four Indexed8 PNG atlases. A shielded actor still renders one
idle-or-hit overlay, and there is no runtime bounds scan, dynamic scaling, or per-actor sequence
lookup. This keeps sequence metadata small and does not require an engine feature.

The tradeoff is intentional: shared tiers are less exact than per-actor fitting. Some shields
will be loose, and unusual silhouettes may leak until given a generic tier override. Static
bounds are useful for finding candidates, but final scale and offset remain an in-game visual
decision because voxel models, turrets, animation, and the OpenRA camera affect the result.

When actor art changes, first test its inherited tier. Promote or demote it to an existing tier
when possible; create a reusable generic tier only when necessary.
