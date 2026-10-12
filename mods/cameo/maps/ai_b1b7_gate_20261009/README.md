# AI B1/B7 weapon-validation gate

Driven benchmark map that produces machine-readable health receipts for the
playtest repairs on `devin/playtest-b1-exorcist-orb` (`a65c04afd`) and
`devin/playtest-b7-japan-sw-air` (`890fba61e`).

## B1 — Hakureiring3 exclusion repair

The exorcist's orb ring is **not** an armament: `PeriodicExplosion` gated on
`spellcard` detonates `ReimuDreamSeal` on the exorcist, whose `FireRadius`
warhead launches four `ReimuYinYangDrone` lasers to the compass points at
`5c0`. Each laser detonates every 5 ticks (`PierceTicks: 64`), `FireShrapnel`
aiming one `Hakureiring3` at an enemy within `Range: 6666` (~6.5 cells).

The fix changed `Hakureiring3` `InvalidTargets` from `Ground` to
`Epic, Psionic, Monster`. The gate grants `spellcard` via the map-local
`ExternalCondition` shim in `rules.yaml` at t=300, so the orb ring runs for
~450 ticks while both armaments are paused (`PauseOnCondition: spellcard`).

Fixtures:

- `b1_bosstarget` — `ra1_allies_mobilegapgenerator` (Epic) at 4c, inside
  `Hakureiring2` (`ValidTargets: Epic, Psionic, Monster`, 240k) range.
  **Expected to die** — proves the anti-boss armament still fires.
- `b1_tank_a/b/c` — `td_gdi_battletank` at 8-9c east: inside the orb-missile
  envelope, outside both armament ranges. **Must lose health.**
- `b1_epic` (mobilegapgenerator), `b1_monster` (`gate_monster_dummy`),
  `b1_psionic` (`gate_psionic_dummy`) — exclusion controls inside the same
  envelope. **Must stay at full health.** The dummies are map-local
  gapgenerator variants whose `Targetable` is overridden to a single
  Monster/Psionic tag on the standard {Ground, Vehicle} chassis;
  `rules/monsters.yaml` is commented out of `mod.yaml` and `wc2_orcs_ogre`
  would chase the exorcist.

## B7 — MagicOrbHailstorm vs aircraft

`Warhead@Tesla_Heavy` (`AreaDamage`, Spread 1200, Falloff 100/55/0 -> 2400wu
max) and `Warhead@Tesla_Heavy_ExtraDamage` (`SpreadDamage`, 1600,
100/75/50/25 -> 4800wu max) on `MagicOrb`/`MagicOrb2` now list
`ValidTargets: Ground, Water, Air`.

`japan_japaneseshrine` gets a map-only `ChargeInterval: 250` (live default
10500 untouched), so `SupportPowerBotASModule` fires `MagicOrbHailstorm` at
the bait cluster (`ts_gdi_techcenter` + two `ts_gdi_tiberiumrefinery`,
value >= `MinimumAttractiveness: 3000`). Orbs rain over a ~5-10c annulus and
each fires 8 `MagicOrb` bullets straight down to ground level.

`HitShape.DistanceFromEdge` is **3D**: a victim at altitude z is in the
blast only within `sqrt(range^2 - z^2)` horizontally. The receipts document
altitude (`z=`) so the review chain can see the real coverage:

- `b7_ground` (tank) and `b7_landed` (longbow driven down by
  `LandOnGround`) — **must be damaged**.
- `b7_hover`/`b7_moving` (helicopters at ~z=2000) and `b7_high` (Yak at
  ~z=4000) — at least one airborne subject must be damaged; aimed spawner
  orbs can reach ~z=2000. The z=4000 case sits at/beyond the 3D reach of
  the main warhead (2400wu) and is a documented coverage boundary, not a
  gate failure.
- `b7_friendly` — japan samurai parked in the annulus; friendly fire is
  reported (`FriendlyFireDamage: 50`, `FriendlyFireSpread: 50`).

## Run

```powershell
python tools/tests/ai_b1b7_gate.py
```

Prints `B17_RECEIPT <name> hp0=<baseline> hp1=<final> z=<altitude>` lines
and a PASS/FAIL summary. The gate fails on nonzero exit, any new
`exception-*.log`, missing receipts, tanks/exclusion violations, an
undamaged ground or landed control, or zero airborne damage.

Do not run concurrently with the other `tools/tests/ai_*_gate.py` gates —
they share `%APPDATA%/OpenRA/Logs` for exception detection.
