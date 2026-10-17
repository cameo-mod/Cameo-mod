# Garrison fire-port migration

Canonical `AttackGarrisoned` is supplied by the engine Common assembly at
`2db0db28b46a986112595218de709f3f816e3732` (codex/garrison-p3-fix), based on the
integrated engine commit `0e42ed433d237c7844005f2b145e877953053bf4`. The Common
occupant-provider bridge is implemented by AS Garrisonable. AS AttackOpenTopped
and mod CA AttackGarrisonedSP are temporary load aliases that identify the actor
and replacement in the debug log. Their Info types carry `ObsoleteAttribute`
guidance for code-level consumers as well. No assembly lookup order is changed.
These aliases are retired by U7 after zero-use validation.

Run `python tools/audit/garrison_fireports.py --capacity-report <output.json>`.
`--write --manifest <manifest.json>` performs conversion, records names/suffixes,
source lines and before/after text hashes, and preserves the existing manifest on an
idempotent rerun. Geometry and palette fields are retained; the old passenger-mode
flag is removed only inside converted traits. Both removal keys and @instances are
converted, including dormant YAML and packed map override YAML. Archive metadata,
comments and unrelated asset bytes are preserved. Duplicate converted instances
are rejected. Check `garrison_migration_manifest.json` for the applied conversion.

Mounted inheritance resolution audits maximum Cargo/Garrisonable capacity against
exclusive port count. 304 mounted port actors are checked. 146 actors deliberately
retain their original geometry and have `NoFireOverflow: true` at their actor node;
`garrison_overflow_manifest.json` records each decision, capacity and port count.
This is a gameplay change: excess occupants remain loaded but cannot fire until a
port becomes free. Existing occupants keep their stations; there is no modulo or
random sharing. No new art, offsets, yaws, cones or palette values are fabricated.

Stance gating uses the more restrictive of the host and passenger stances: opportunity
scans require both to be at least Defend, and retaliation requires both to be at least
ReturnFire. A passenger cannot fire opportunistically through a host set to HoldFire.

Bots and previews use the canonical selected-armament/range/cone queries. Owned
carrier profiles include its occupied station weapons. Target-specific damage/range
count only weapons eligible at that target's observed position. The kiting query
uses that target-specific range. A visible enemy carrier does not reveal its private
cargo. Unobserved/type-only targets conservatively provide no passenger damage
estimate; strategic type profiles do not guess cargo composition. The existing
floating point analytical predictor remains analytical; the new station assignment,
scan, geometry, cadence and firing implementation uses fixed point/integer values.
Non-port weapon formulas are unchanged. Read-only forecasts do not issue orders,
move passengers, change station targets/cadence or consume RNG.

Engine AttackGarrisonedTest supplies a reusable headless real-trait fixture; only
the World shell/spatial index are substituted. Mod predictor and alias tests reuse
it via a test-only engine test-project reference. Engine tests cover real Cargo and
AS Garrisonable, independent priorities, selected weapons, per-port range/cone and
shot origins, exits, overflow, capture, stop/cancellation/persistence, resupply,
hidden live targets, frozen snapshots, pause/disable, off-world hosts, recreation,
forecast purity and repeated scripted fire events/RNG. These fixtures do not replace
rendered gameplay/projectile-damage or multiplayer replay acceptance. A maintainer
mixed rifle/anti-vehicle passenger check remains required for gameplay approval.

## RA1 pillbox directional regression (2026-10-12)

Normal and camouflaged RA1 pillboxes historically let each passenger choose any matching fire port. The exclusive-station migration bound a lone passenger to port zero while retaining six 88-angle directional cones. This made targets on the other sides unreachable, even under explicit Attack. These two stationary profiles now retain their offsets and independent bindings but use full-turn (512) cones. The lead approved this narrow default-on regression fix; other host geometry is unchanged.

`MountedGarrisonFireTest` loads the mounted infantry armaments, conditional auto-target priorities, Passenger/Garrisoner, target types and host attack/container profiles. It exercises automatic fire and explicit Attack, including four cardinal directions with one occupant. The engine's existing fixture supplies an asset-free world/position shell; projectile/warhead impacts, sounds and muzzle rendering are omitted, and the infantry renderer's derived cargo weight of one is preserved explicitly. This proves firing admission and real armament notifications, not live-world/projectile parity. On the original cones: 4 pass / 4 fail. With this change: 8/8 pass. No engine source/pin change or game launch is required for these tests.

The broader civilian-garrison report and Cargo-turret bot recruitment remain separate investigations. A generic inherited civilian profile passes automatic/explicit firing; real civilian profiles still need eight-direction and full-load coverage. The mounted FutureTech faction roster already includes the multi turret, and baseline garrison mission counters cannot substitute for occupancy evidence.
