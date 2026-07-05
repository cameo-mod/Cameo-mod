# Shellmap 2FFA — Reproducible Build Guide

How to (re)build the **Shellmap 2FFA** battle map from a terrain map: a two-player allied
base (GDI + Ixian) that endlessly repels three attacking factions (Zerg, Nod, Ordos), with
an indestructible base, an auto-repair "AI", and corner air patrols. The companion
[`attack.lua`](attack.lua) in this folder is the authoritative script — this guide describes
the map wiring around it.

- **Source terrain:** JUNGLE, 130×130, with the two faction bases pre-placed. (Originally
  authored in the OpenRA editor as `shellmap_2FFA.oramap`.)
- **Result:** `shellmap_v2.oramap` (Title `Shellmap v2`, `Visibility: Shellmap`) — same
  terrain, rewired players + ownership + rules, plus `attack.lua`.
- Set `Visibility: Shellmap` + `Categories: Shellmap` for the menu-background version; use
  `Lobby`/`Conquest` instead if you want to load it interactively in a skirmish lobby.
- A `.oramap` is a zip with `map.yaml`, `map.bin`, `map.png`, `attack.lua` at the archive root.

## 1. Players

Replace the editor's default `Multi0/Multi1` with these. GDI + Ixian are the allied
defenders; Zerg + Nod + Ordos are the allied attackers.

```yaml
Players:
	PlayerReference@Neutral:
		Name: Neutral
		OwnsWorld: True
		NonCombatant: True
		Faction: Random
	PlayerReference@Creeps:
		Name: Creeps
		NonCombatant: True
		Faction: Random
		Enemies: GDI, Ixian, Zerg, Nod, Ordos
	PlayerReference@GDI:
		Name: GDI
		Faction: gdi
		Allies: Ixian
		Enemies: Creeps, Zerg, Nod, Ordos
	PlayerReference@Ixian:
		Name: Ixian
		Faction: ixian
		Allies: GDI
		Enemies: Creeps, Zerg, Nod, Ordos
	PlayerReference@Zerg:
		Name: Zerg
		Faction: zerg
		Allies: Nod, Ordos
		Enemies: GDI, Ixian
	PlayerReference@Nod:
		Name: Nod
		Faction: nod
		Allies: Zerg, Ordos
		Enemies: GDI, Ixian
	PlayerReference@Ordos:
		Name: Ordos
		Faction: ordos
		Allies: Zerg, Nod
		Enemies: GDI, Ixian
```

> **Never mark GDI/Ixian `Playable: True`.** In a shellmap there is no lobby, so `Playable`
> slots are not instantiated — `Player.GetPlayer("GDI")` then returns nil and `WorldLoaded`
> crashes on first use, which reads in-game as the shellmap "freezing" (static, no camera).
> The scripted defenders must be plain `Name`/`Faction`/`Allies`/`Enemies` players (compare
> `desert-shellmap-2`). The editor **re-adds** `Playable: True` (and `Multi0/Multi1`) on every
> save — strip it from GDI/Ixian after each editor pass.
>
> `Multi0/Multi1` (regenerated from the 2 spawn points) are harmless — no actors own them.
> To remove them permanently, delete the `mpspawn` actors (loses lobby-spawn support).

## 2. Ownership reassignment

Every actor is owned by `Neutral` in the source. Reassign the **non-prop** actors by faction;
leave all terrain/props (trees `tc**`/`t**`, `v**`, `wall`, `brik`, `mine`, `split*`, …) as
Neutral. Attacker types never overlap Team 1 types, so type-based classification is safe.

| Actor type test | New owner |
|-----------------|-----------|
| ends with `.gdi`, or generic C&C `{nuk2, atwr, gtwr, mlrs, weap, pyle, gdimammoth3, htnk, mtnk}` | GDI |
| `eye` (Advanced Comms Center — no `.gdi` suffix, easy to miss) | GDI |
| ends with `.ixian` | Ixian |
| ends with `.ordos` | **retype to `.ixian` counterpart**, owner Ixian |
| `mpspawn` | Neutral |
| anything else (props) | unchanged |

Ordos→Ixian retypes actually applied: `repair_pad.ordos → repair_pad.ixian`,
`research_centre.ordos → research_centre.ixian`.

## 3. Map rules

Append to `map.yaml`. The **shellmap scaffold** (`Player` + `World` disables) is *mandatory* —
without it the map freezes ~40s in; see the freeze post-mortem below. It mirrors
`desert-shellmap-2/rules.yaml`, the reference working shellmap.

```yaml
Rules:
	Player:
		# Without this the match "ends" the instant a scripted faction has no qualifying
		# units/buildings — which STOPS the world tick and freezes the menu backdrop.
		-ConquestVictoryConditions:
		# Fog for 5 scripted players is a heavy per-tick shroud cost; shellmaps don't need it.
		Shroud:
			FogCheckboxEnabled: False
	World:
		-SpawnStartingUnits:
		-CrateSpawner:
		-AutoSave:
		-MapStartingLocations:
		GrantCondition@RAIN:                          # optional rain — see "No lobby" note below
			Condition: weather-rain
		WeatherOverlay@RAIN:
			InitialParticlePercentage: 100
		LuaScript:
			Scripts: attack.lua
		MusicPlaylist:                                # silence battle SFX behind the menu
			DisableWorldSounds: True
			AllowMuteBackgroundMusic: True
	wind_trap.ixian:            # example map-local stat override (see below)
		PowerMultiplier@shellmap:
			Modifier: 1000
	# Force turret searchlights on (they normally need the "weather" lobby option, absent here):
	^BasicDefenseTemplate:
		WithTurretSearchlight:
			RequireWeatherOption: False
	^AntiAirDefenseTemplate:
		WithTurretSearchlight:
			RequireWeatherOption: False
	^AdvancedDefenseTemplate:
		WithTurretSearchlight:
			RequireWeatherOption: False
	^SuperDefenseTemplate:
		WithTurretSearchlight:
			RequireWeatherOption: False
```

- `-SpawnStartingUnits` → defenders get **no MCV/starting units**; they own their pre-placed
  bases. (Skirmish AI is inherently off — GDI/Ixian have no `Bot:`.)

#### "No lobby" gotcha — weather & searchlights

A shellmap has **no lobby**, so anything gated on a lobby *option* silently stays off. Two mod
effects are affected, each needs a map-rule workaround:

- **Rain.** The mod's `WeatherOverlay@RAIN` requires the `weather-rain` condition, normally
  granted by the `@WEATHER` lobby dropdown. Instead grant it directly on the world actor with
  `GrantCondition@RAIN: Condition: weather-rain` (a base `Mods.Common` trait — no engine change),
  and set `InitialParticlePercentage: 100` because rain otherwise ramps up from 0% invisibly.
- **Turret searchlights.** `WithTurretSearchlight` reads the `weather` lobby *option* directly
  (`weatherEnabled`), which a shellmap can't set. The trait has a `RequireWeatherOption` flag
  (default `True`); overriding it to `False` on the four searchlight-bearing defense templates
  forces the base towers to sweep regardless. AA defenses (`^AntiAirDefenseTemplate`) angle their
  beam skyward via `IdleElevation`. The glow is a pale-yellow additive effect, most visible
  against the darker jungle ground.

### Map-local rule overrides (no Lua needed)

Any pure **stat** change belongs in a `Rules:` actor override in `map.yaml`, not in Lua — it
applies to this map only and leaves the shared rule files untouched. Example (used here to give
the Ixian Wind Trap 10× power output; `PowerMultiplier.Modifier` is a percentage, 100 = ×1):

```yaml
Rules:
	wind_trap.ixian:
		PowerMultiplier@shellmap:
			Modifier: 1000
```

Reserve Lua for behaviour that reacts to game state over time (the shield below); flat
number changes (power, HP, cost, damage, range, speed) should be trait overrides.

### Freeze post-mortem (what actually caused the "freeze" — and what didn't)

The shellmap froze ~40s in, every time, with an **empty `exception.log`**. Two real causes,
both map/rules-level. The wave-targeting logic was a **red herring**: the freeze reproduced
under a *completely empty* script, which is the key that unlocked the diagnosis.

1. **`ConquestVictoryConditions` was still active.** A shellmap has no lobby, so scripted
   factions have no production buildings. As soon as a faction had no qualifying units, the
   victory system declared the match **over** → the world **sim stopped ticking** (menu stays
   responsive, no crash, nothing logged). This is the *main* freeze. Fix: `Player:
   -ConquestVictoryConditions:` in the scaffold above.
2. **The shield touched the player proxy actor.** `player.GetActors()` includes a hidden actor
   of type `player` that has **no `Health`**; reading `self.Health` on it throws a **fatal Lua
   error** that kills the *entire* script — including the camera `Tick` — so the picture froze
   (this one surfaced only after #1 was fixed and the map ran long enough to take damage). Fix:
   register the shield only on `HasProperty("Health")` actors, plus a guard in `applyShield`.

**Diagnostic method that found it** (generic for shellmap "freezes"):
- Run with an **empty `WorldLoaded`**. If it still freezes, the script is exonerated → look at
  the map/rules. (Give the empty build a camera orbit or watch the OS window, so a *static idle*
  scene isn't mistaken for a freeze.)
- Watch the **OS window `Responding` flag + CPU**, not just `perf.log`: a stopped sim stays
  `Responding=True` at **low, steady CPU**; an infinite loop pegs a full core; a crash exits.
- **Diff the map's `Rules` against `desert-shellmap-2`** — the missing scaffold jumps out.

**Non-issues we chased and discarded for the *freeze*:** `Hunt` vs `AttackMove` vs a rally cell;
wave sizes; spawn grouping. (The one script bug that *was* real and separate: spawning a whole
26-unit wave in a single `Reinforce` tick crashes `SpawnActorEffect`; spawn in **groups of 4 on
staggered ticks**.) `AttackerCap` bounds the live pile-up.

### Stuck units — the goal cell must be *standable* (empty + reachable)

Separate from the freeze: attackers were getting **stuck**, inconsistently — some pathed into
the base, others wedged at trees/walls. Cause: `TargetCell` was **80,58, which is inside the
Supercomputer's footprint** — an occupied cell no unit can stand on. A blocked goal doesn't make
pathfinding fail cleanly; each unit degrades to *"route to the nearest cell I can reach,"* and
that nearest-reachable cell differs **per unit** by approach direction, live unit congestion at
entrances, and `AttackAnything` peeling off to reachable enemies — hence the *inconsistency*.
The 5s idle-kick made it worse by re-issuing the same unreachable order every 5s (grinding the
ring). **Fix: point `TargetCell` at an empty, reachable cell (75,55).** With a standable goal
every unit gets a complete path and converges consistently; trees just get routed around.

Rules of thumb for the target: (1) never a building/wall/occupied cell; (2) reachable from every
spawn (don't fully seal a spawn's lane with trees — trees are solid `Building`-footprint actors).

## 4. Script systems (`attack.lua`)

| System | Behaviour | Key tunables |
|--------|-----------|--------------|
| **Attack waves** | Zerg every 30s (t=0); Nod +5s; Ordos +10s. Zerg/Nod alternate two comps. `AttackAnything` + a single `AttackMove(TargetCell)` where `TargetCell` is an **empty, reachable** cell (75,55), *not* a building/wall. Each faction has **two spawn zones** (rectangles); a wave picks one and spawns its groups of 4 at random cells inside it (fans out; avoids single-cell congestion and the `SpawnActorEffect` single-tick crash). Per-player live cap 40. | `TargetCell`, `*ZoneA/B` rectangles + comps at top; `GroupSize`, `AttackerCap` |
| **Un-stick attackers** | Every 5s, **every** live attacker (with an `AttackMove` property) is re-issued `AttackMove(TargetCell)`. Not idle-only — a *blocked* unit isn't `IsIdle`, so idle-only misses exactly the stuck ones. `AttackMove` still lets them fight en route, so re-ordering movers is harmless. | interval in `KickIdlers` |
| **Indestructible base** | Per-hit tiered resistance on every Team 1 **Health-bearing** actor: >50% HP none, 30–50% 80%, <30% 100% (min 1 HP). Death-safe (`Damaged` fires before the kill check); guards non-positive damage to avoid the heal re-firing itself; skips the Health-less `player` proxy actor. | thresholds in `applyShield`; registration in `fortify` |
| **Defenders hold position** | GDI/Ixian `AutoTarget` units set to `Defend` stance so guardians fire on in-range enemies but never leave their posts to chase. Non-playable players otherwise default to `AttackAnything` — the *only* stance that allows chasing (engine: `AllowMove => Stance > Defend`). | stance in `fortify` (WorldLoaded) |
| **Repair AI** | Every 60s, fund GDI/Ixian (`Cash=100000`) and `StartBuildingRepairs` on damaged buildings (idempotent). | interval in `RepairBases` |
| **Air patrols** | GDI 6× `orca` clockwise; Ixian 6× `air_drone.ixian` reverse. `AttackAnything` (they *should* move); refilled to 6 every 15s. | `SquadronSize`, `PatrolRoute(Reverse)` |
| **Menu camera** | `Tick` slowly orbits the base centre so the fight stays framed; the orbit starts from `CameraStartAngle` (initial framing). | `CameraSpeed`, `CameraRadius`, `CameraStartAngle`, `BaseCentre` |

> **Zoom is not controllable from the map.** Lua `Camera` exposes only `Position`; the shellmap
> always renders at the menu default (fully zoomed out, engine `MinZoom`), and min-zoom is a
> mod-wide setting, not per-map. The base sits *east* of map-centre, so a wide orbit sweeps the
> east void into frame — keep `CameraRadius` small (~6 cells = 6144 wu). `CameraSpeed` is
> intuitive: higher = faster (0.1°/tick at `1` ≈ 144 s per revolution). `CameraStartAngle`
> sets the initial framing in degrees around the base: `0` = south, `90` = west, `180` = north,
> `270` = east (`sin`→x/east-west, `cos`→y; +y is south, so 180° frames the north side).

Base corners (patrol route): `63,42 → 57,76 → 103,77 → 104,41` (GDI order; Ixian reverse).

**Never use husk actors** (`orca.Husk`, `air_drone_husk.ixian`, `*_husk.*`) as spawn types.

### Attack force configuration

All values live at the top of `attack.lua`. **Cadence:** Zerg fires at t=0 then every 30s; Nod
5s after each Zerg; Ordos 10s after each Zerg. Zerg and Nod alternate between two compositions on
successive waves; Ordos uses one. All attackers `AttackMove` to `TargetCell = 75,55` (empty,
reachable) and are re-kicked there every 5s.

**Compositions** (`ZergComp1` etc.):

| Faction | Comp A | Comp B |
|---|---|---|
| Zerg | 20 `sczergling`, 2 `scultralisk`, 5 `schydralisk` | 20 `sczergling`, 10 `scmutalisk` |
| Nod | 5 `ltnk`, 5 `ftnk`, 2 `mssm` | 20 `e1.nod`, 10 `e4` |
| Ordos | 10 `chem_troop.ordos`, 5 `combat_tank.ordos`, 5 `raider.ordos` | *(same comp each wave)* |

**Spawn zones** — inclusive rectangles `{x1,y1,x2,y2}`; a wave randomly picks one of its faction's
two zones and spawns each group of 4 at a random cell inside it (fans out to avoid congestion):

| Faction | Zone A | Zone B |
|---|---|---|
| Zerg (north) | `40,3 → 44,5` | `75,2 → 79,4` |
| Nod (west/south) | `2,76 → 4,80` | `113,125 → 117,127` |
| Ordos (east) | `123,2 → 127,4` | `126,63 → 128,67` |

`AttackerCap = 40` bounds live attackers per faction; over the cap a wave is skipped. `GroupSize = 4`
units per staggered spawn group.

## 5. Package + install

```
zip: map.yaml + map.bin + map.png + attack.lua  (at archive root)  ->  shellmap_v2.oramap
```

Drop into a scanned map folder — e.g. `mods/cameo/maps/` (System) or
`%APPDATA%/OpenRA/maps/cameo/{DEV_VERSION}` (User).

## Editor caveat + reconcile workflow (important)

**The editor clobbers everything except the actor list.** Editing terrain/trees in the OpenRA
editor and saving reverts the map to editor defaults: `Visibility: Lobby`, `Categories: Conquest`,
re-adds `Playable: True` (the original crash cause!) + `Multi0/Multi1`, **drops the entire Rules
scaffold/overrides**, and writes back an **old embedded `attack.lua`**. It also saves under a
*different filename* (`shellmap_2FFA_v2.oramap` in the User maps dir), leaving `shellmap_v2`
untouched — so the edits appear "lost" until you find that file.

**Trees are actors, not tiles** — tree edits land in `map.yaml`'s `Actors:` section (and the
regenerated `map.png`), *not* `map.bin`. So the only salvageable part of an editor save is its
`Actors:` block.

**Reconcile after every editor save:**
1. Find the newest `shellmap_2FFA_v2.oramap` (User maps dir); unzip it.
2. Rebuild `map.yaml` = **your** meta+`Players`+`Rules` (before `Actors:` and from `Rules:` on)
   **+ the editor save's `Actors:` block** (its trees). Take `map.png` from the editor save.
3. Re-attach the authoritative `attack.lua` from this folder.
4. Repackage as `shellmap_v2.oramap`; verify `Visibility: Shellmap`, GDI/Ixian **not** `Playable`,
   `-ConquestVictoryConditions` present, and `attack.lua` byte-matches this folder's copy.

Don't hand-edit the script inside the editor.

## Actor name reference

- GDI air: `orca` · Ixian air: `air_drone.ixian`
- Zerg: `sczergling`, `schydralisk`, `scmutalisk`, `scultralisk`
- Nod: `ltnk`, `ftnk`, `mssm`, `e1.nod`, `e4`
- Ordos: `chem_troop.ordos`, `combat_tank.ordos`, `raider.ordos` (raider trike)
- Ixian buildings referenced: `repair_pad.ixian`, `research_centre.ixian`, `launchpad.ixian`
