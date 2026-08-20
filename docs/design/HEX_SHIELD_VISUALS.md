# Recolorable hex-shield visuals

## Decisions

Shield mechanics remain authoritative. Visual changes must not alter shield conditions,
upgrade prerequisites, aura behavior, or hit-state switching.

The retained visual decisions are:

- upright oval for infantry;
- sphere for ordinary vehicles, aircraft, naval units, and large mobile classes;
- camera-correct dome for buildings and defenses;
- directional oval as an explicit geometry opt-in for elongated aircraft;
- fixed faction colors: default/Protoss blue, Ixian silver, Yuri indigo, Consortium cyan;
- Indexed8 art with transparent index 0 and 25% idle / 50% hit palettes.

Actor-specific shield sizing is forbidden. Concrete actors must not define `Sequence` or
`StartSequence` on the shield overlays. A concrete actor may override only `Image` when it
needs a different geometry, such as the directional oval.

## Class and footprint sizing

Mobile sequences are sized from class medians and normal cell occupancy:

| Class | Sequence | Scale |
| --- | --- | ---: |
| Infantry | `infantry-standard` | 1.10 |
| Vehicle and unclassified mobile | `vehicle-standard` | 1.00 |
| Aircraft | `aircraft-standard` | 1.15 |
| Naval | `naval-standard` | 1.30 |
| Dreadnought-scale mobile | `large-mobile-standard` | 1.50 |
| Directional aircraft geometry | `aircraft-standard` | 0.60 |

These are class standards, not promises to cover every sprite pixel. Large or unusually offset
art remains visually exceptional, but it must be addressed through an existing semantic class
template rather than by naming or sizing the individual actor.

## Why the previous sizing was rejected

The previous values optimized for containment: infantry 1.70, ordinary spheres 1.55,
naval/large spheres 2.30, defenses 0.80, and buildings 1.15. Across the prior 1,592-actor
measurement set, the median shield was 1.65 times the padded fitted target; 1,022 actors were
over 1.5 times their fitted target and 458 were over twice their fitted target. In-game review
confirmed this as widespread visual oversizing.

Class and footprint values reduce the overall median ratio to 1.00. The static report still
flags 146 actors above 1.5 times and 278 below 0.75 times the old padded target. Those tails are
an accepted limitation of general sizing, not a queue for actor-by-actor fixes.

## Buildings and selection boxes

Each existing `^NxMShape` selects a matching `dome-NxM` sequence. For the rectangular Cameo
grid, the selection footprint projects to approximately `48N x 48M` screen pixels. Sequence
scale is calculated with one shared 8-pixel padding on every edge:

```text
scale = max((48N + 16) / 261, (48M + 16) / 222)
```

The master dome's visible center is `(-0.5, 1)`, so every footprint sequence uses the derived
centering offset `(0.5, -1)`. Orientation remains significant: 2x3 and 3x2 are separate.

`^NxMShape` inherits one complete, condition-gated overlay pair without palette fields.
Shield-capable buildings merge their fixed faction palette from `^ShieldedShieldable`; 261
non-shield building actors carry the same pair dormant because they never satisfy `shielded`.
This is two disabled animations per live non-shield building, instead of the much larger cost
of defining every footprint alternative on every shielded building.

High-count walls and bridges opt out at their shared semantic templates. Bridges also lack
`RenderSprites`, which makes the opt-out required for trait validity rather than only performance.

The routing audit currently records 22 actors whose final `Selectable.Bounds` was overridden
to a different standard rectangle after their inherited shape. Their `^NxMShape` remains the
authoritative general class; these are warnings, not actor-specific sizing work.

## Performance and maintenance

All actors reuse four Indexed8 PNG atlases and render at most one idle-or-hit overlay. There is no
runtime bounds scan, dynamic scaling, generated actor table, or actor-name lookup. Directional
facings remain authored rather than interpolated.

When adding an actor, inherit its normal unit/building class and do not add shield sizing YAML.
If a whole semantic class is consistently wrong, adjust that shared class or introduce a
reusable class template only after representative in-game review.
