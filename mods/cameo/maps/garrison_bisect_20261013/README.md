# Garrison passenger-fire bisect fixture (task 01a13167)

Bounded runtime-bisect fixture for the garrison passenger-fire patch
(`devin-garrison` engine branch, `60b9f05a04`). SOURCE-ONLY: the bisect runs
only under a fresh admission the coordinator issues after logger closure +
pin promotion. Terrain (`map.bin`, `map.png`) is reused verbatim from
`heaviness_probe_20260910`; all fixture cells sit on verified clear tiles.

`LogFireDiagnostics: true` is set on every fixture host, so each host writes
per-station gate diagnostics to `debug.log` (via `TextNotificationsManager.Debug`).
Lua prints `GARRISON_BISECT_*` stamps to `lua.log`.

Coverage per reviewer erratum 3/3a/3b + Sol's Red Spice replay parse: autonomous
scanning on both occupant providers, ordinary `Attack` AND `ForceAttack` on a
single-AttackBase civilian host, and an explicit order on the dual-AttackBase
naxibunker to observe the order race.

## Cell map (all SideA hosts, SideB targets, all cells verified clear-sand)

| Cell | Host (type) | Occupants | Target (type) | Orders | Expected |
|---|---|---|---|---|---|
| c1 | `fixture_civ_defend` (V20/`^CivBuilding`) @ (20,22) | 2x `fixture_rifle_defend` | `fixture_target` (Ground,Vehicle) @ (25,23) | none | autonomous fire |
| c2 | `fixture_civ_defend` @ (20,36) | 2x `fixture_rifle_defend` | `fixture_target_structure` (Ground,Structure) @ (25,37) | ordinary `Attack` t60 | ordered fire only |
| c2f | `fixture_civ_defend` @ (34,36) | 2x `fixture_rifle_defend` | `fixture_target_structure` @ (39,37) | `Attack` force=true t60 | ordered fire only |
| c3 | `fixture_bunker_defend` (naxis_naxibunker) @ (20,50) | 2x `fixture_rifle_defend` | `fixture_target` @ (25,51) | ordinary `Attack` t120 | autonomous fire; order outcome = race evidence |
| ctl | `fixture_civ_holdfire` @ (20,64) | 2x `fixture_rifle_holdfire` | `fixture_target` @ (25,65) | none | silence, 0 hits |

Occupants spawn via `InitialUnits` (inherit host owner), bind to the first free
ports in index order: ports 0 and 1. `naxis_naxiriflesoldier` has two armaments
named `garrisoned` (normal + `rank-elite`-gated): both bind (`arms=2` at `bind`),
but the elite arm is trait-disabled and never survives `Eligible()`, so
prep/fire lines report `arms=1`.

## Expected LogFireDiagnostics signatures

Line formats (from `AttackGarrisoned` on the patch branch):

- `bind`: `<host>#<id>: bind port<N> occupant=<occ>#<id> arms=<n>(<names>) occAT=<none|disabled|<Stance>> effAT=<occupant:<Stance>|host:<St>> roster:cargo=<n>,provider=<n> activity=<name|none>`
- `unbind`: `<host>#<id>: unbind port<N> occupant=<occ>#<id>`
- `scan`: `<host>#<id> port<N> scan: <host-gate | no-effective-at occAT=<s> | empty occAT=<s> eff=<k> | hit <TargetType> occAT=<s> eff=<k>>`
- `request`: `<host>#<id>: request via=<order-resolve|activity-tick> target=<Type>[:<actor>] force=<bool> stations=<n> currentActivity=<name|none>`
- `release`: `<host>#<id>: release persistent=<bool> activity-ended`
- `activity`: `<host>#<id>: passenger-activity first-tick target=<Type> force=<bool>` / `passenger-activity ended canceled=<b> ran=<b>`
- `prep`: `<host>#<id> port<N>: <ordered|opp>:<Type> arms=<n>` or `<ordered|opp>-silent:<first-reject|none> occAT=<s> effAT=<k> hostAT=<h>`
- `fire`: `<host>#<id> port<N> fire: fired arm=<name>` / `checkfire-false arm=<name> delay=<d> burst=<b> paused=<p> disabled=<d>`
- `group` (turreted occupant path, unused here) and `retaliate port<N> attacker=<name>`.

Reject vocabulary (first-reject in `*-silent`): `host-or-occupant-unavailable`,
`invalid-target`, `outside-port-cone`, `requires-force-fire`,
`<arm>:disabled`, `<arm>:paused`, `<arm>:weapon-invalid`, `<arm>:relationship`,
`<arm>:range`.

### c1 (autonomous, Garrisonable route)

- `bind port0|1 occupant=fixture_rifle_defend arms=2(garrisoned+garrisoned) occAT=Defend effAT=occupant:Defend roster:cargo=0,provider=2 activity=<host activity name, or none>`
- `scan: hit Actor occAT=Defend eff=occupant:Defend`
- `port<N>: opp:Actor arms=1` then `fire: fired arm=garrisoned`
- Result: `GARRISON_BISECT_RESULT cell=c1 ... hits>0`
- BUG signature (pre-fix): `opp-silent:*` variants, no `fired`, `hits=0`.

### c2 (ordinary Attack arm, civilian single-AttackBase host)

This is the decisive venue per reviewer erratum 3: a single-attack-trait host
removes the order race, and a `Structure`-typed target cannot be
autonomously selected (absent from `^AutoTargetAll` Defend `ValidTargets`).

- Same `bind` line as c1.
- Before order: `scan: empty occAT=Defend eff=occupant:Defend`, `opp-silent:invalid-target`.
- Ordinary order at t60 (`actor.Attack(target,false,false)`, forceAttack=false,
  Lua path is always queued): `passenger-activity first-tick target=Actor force=False`,
  `request via=activity-tick target=Actor:fixture_target_structure force=False stations=2`,
  `port<N>: ordered:Actor arms=1`, `fire: fired arm=garrisoned`.
- Result: `hits>0`. BUG signature: `ordered-silent:<reject>` / `hits=0` —
  an ordered request that cannot fire isolates binding/eligibility, not AT gates.

### c2f (ForceAttack arm, civilian single-AttackBase host)

Mirrors the replay's force-fire variant. Identical to c2 except
`forceAttack=true` → `force=True` in `request`/`first-tick` lines.

### c3 (autonomous, Cargo route, dual-AttackBase host + order probe)

- `bind ... occupant=fixture_rifle_defend arms=2(garrisoned+garrisoned) occAT=Defend effAT=occupant:Defend roster:cargo=2,provider=0 activity=<...>`
- Pre-t120 autonomous: `scan: hit Actor`, `opp:Actor arms=1`, `fire: fired`, hits accumulating.
- Ordinary `Attack` at t120 on the same target — this probes the pre-existing
  dual-trait order race (`AttackGarrisoned` + `AttackTurreted` both resolve the
  order; trait enumeration order decides the current activity slot). Lua orders
  are queued=true, so whichever activity lands in the current slot first wins;
  the loser queues behind (the garrison activity never completes while the
  target lives, so a loser never runs).
  - Garrison activity wins: `passenger-activity first-tick`, `request via=activity-tick`,
    `ordered:Actor arms=1` — order reached stations.
  - Turret activity wins: no `first-tick`/`request` — the queued garrison
    activity never ticks; autonomous `opp:` fire continues unchanged.
    = the dual-trait race reproduced under the queued variant.
  (`request currentActivity=<name>` names the winner when emitted.)
- The bunker's `AttackTurreted` host armaments are all condition-gated off
  (no plugs installed) — it must never emit `fired` for host weapons.

### ctl (HoldFire occupant)

- `bind ... occAT=HoldFire effAT=occupant:HoldFire roster:cargo=0,provider=2`
- `scan: no-effective-at occAT=HoldFire` (usable occupant AT below Defend must
  NOT fall back to host AT),
- `opp-silent:invalid-target occAT=HoldFire effAT=occupant:HoldFire hostAT=Defend` (once; deduped),
- `fire:` lines absent; `hits=0`, `hp_lost=0`.

## Known coverage gap

`ForceAttack` against *terrain* has no Lua binding (`Actor.Attack` takes only
`Actor`; there is no terrain-targeted scripted order). That arm needs
order-stream injection or UI automation and is out of scope for this map;
the actor-target force variant (c2f) carries the force-path coverage.

## Run procedure (for the eventual admission)

1. Rebuild engine C# from the merged tree containing the garrison patch.
2. Launch the mod on this map (`garrison_bisect_20261013`), side slot = the
   `Player` seat (any faction; fixture actors ignore faction). ~28s game time.
3. Collect `debug.log` (diag lines above), `lua.log` (`GARRISON_BISECT_*`),
   `exception-*.log` (must be absent/unchanged).
4. Verdict per cell per the "Expected" column. Any `exception` or absent
   `GARRISON_BISECT_COMPLETED` = fixture failure, not a product verdict.

Determinism: fixed cells verified on clear tiles, stance-pinned occupants
(both `InitialStance`/`InitialStanceAI`), `ScanInterval=5` synchronized scans,
no wall clock, no unsynced random, 700-tick bound.
