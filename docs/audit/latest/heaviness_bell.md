# audit_heaviness_bell — would the continuous-heaviness bell invert any family?

DESIGN §12.0i: `SHIFT` 0.25, `LO` 0.8 (swing ~1.25x), x-axis = §12.0d's three buckets. Simulated at h = 0.0, 1.0, 2.0.

| | |
|---|--:|
| families measured | 48 |
| with NO gradient the bell could preserve | 2 |
| ladder directions changed by the bell | 0 |
| known flips, already recorded | 2 of 2 |
| weighted-mean drift beyond 1e-6 | 0 |

## Flat families — no gradient to preserve

§9.2 predicted SIX of these; four (Cryo, Railgun, Waveforce, Storm) have since been given real gradients, so the prediction is stale and only these remain. The bell cannot help a family with no gradient — they need real profiles authored (§9.4). Lower `INVERT_BASELINE` as that happens, never by widening the bell.

  Magic
  Sonic

## Known flips — a near-flat SUB-ladder, not a bell problem

§9.4's spread band is measured over the FULL armor table, so a family can sit in band while one sub-ladder is nearly flat. Both of these run at a 1.13x gradient, which no 1.25x swing can preserve. Fix by authoring a real gradient on that ladder (a warhead change — hard rule 4), never by shrinking the bell.

  BulletThermobaric  BLD
  CannonFire         AIR

WARN 2 flat families (ratchet 2) · 0 inversions (must be 0) · 0 mean drifts (must be 0)
Lower `INVERT_BASELINE` as flat families get real profiles; never raise it.
