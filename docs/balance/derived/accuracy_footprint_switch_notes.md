# Accuracy model switch experiment

Base: `devin/tier4/pricing-default` at `d24668c39ff6d2e43f39456fe0abcc4e650e8331`.

## Switches

- `BM_TARGET_FOOTPRINT_ACCURACY` is default off. When enabled, positional falloff reliability evaluates `max(0, miss_distance - 426 WDist)`, using the vehicle `CircleShape` reference footprint. Direct-actor reliability uses the larger of the legacy 100 WDist point radius and this representative footprint.
- This branch changes no missile terminal model. A follow-up must use the engine's `Missile.cs` detonation and `ImpactPosition` behavior; a Gaussian sigma override is not included here.

## Evidence

The switch-on sidecar extraction priced 492 actors: median price delta +7.2%, range -61.9% to +106.2%, 324/492 new prices closer to current cost. The generated comparison is `pricing_default_delta_target_footprint_on.md`; it uses the staged sidecars at `C:\cameo-wt\accuracy-switch-on\derived` and does not replace canonical ledgers.

The 4-weapon tests include SpecterSniper, GhostSniper, RA2AWP, and `mtank_pri2`. The first three rise from their near-zero reliability values to 1.0 with the target-footprint switch. The disputed `mtank_pri2` estimate is pinned as an expected failure: with T=426 and sigma=1472 this implementation reports 0.239, while Architect measured about 0.16. Do not relax the `<0.2` assertion. The terminal-miss correction is deferred for a separate follow-up based on engine runtime behavior.

Default-off verification: `python tools/balance/extract_stats.py --check` reports 0 drift across 35 ledgers. The pricing delta comparison was generated read-only; no rules YAML or canonical price/ledger files changed.
