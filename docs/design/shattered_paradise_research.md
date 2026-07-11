# Shattered Paradise (SP) research — TS authenticity + CABAL reference

_Research 2026-07-11 against the local SDK checkout
(`C:\Users\AedisToru\Downloads\Shattered-Paradise-SDK-bleed`, bleed).
SP is the reference-quality OpenRA TS total conversion. License: code
GPLv3 (traits are portable into our GPL assemblies with attribution).
**ASSET LAW (design 2026-07-11): take NO art or sound from SP** — no
icons, no sprites, no audio that doesn't exist in base Tiberian Sun.
Use only TS assets Cameo already has, or create new ones. Effects are
to be REBUILT to look almost the same (contrail colors, palettes,
beam settings are yaml parameters, not assets — freely reusable as
recipes; the sprite images they reference are not). Goal: heavy SP
inspiration for ALL TS factions — CABAL first, then GDI, Nod,
Forgotten, and eventually the upcoming Scrin. tjk-ws has already
started mining the code side: `TakeOffOnMake.cs` in OpenRA.Mods.Cameo
is SP's trait verbatim._

## 1. Layout differences vs Cameo

SP is a single-mod build: one `mods/sp` with per-faction WEAPON files
(`cabweapons.yaml`, `gdiweapons.yaml`, `nodweapons.yaml`,
`mutweapons.yaml`, `scrweapons.yaml`) but SHARED rules files
(infantry/vehicles/structures for all factions together), faction
ownership expressed through prerequisites (`~cabclaw`, `~cabweap`) and
`GrantConditionOnFaction@CAB`. Five factions: GDI, Nod, CABAL (`cab`),
Mutants (`mut`), Scrin (`scr`). Tech depth: `techlevel.1–6` lobby
options + per-faction tech tokens (`hasTech.cabT2/cabT3`) — a
double-gate similar to our tier system. Our ContentPack split is
strictly better for the dynamic-loading goal; SP's per-faction weapon
files confirm that direction.

## 2. SP CABAL — full roster (for our CABAL rebuild)

Buildings: Power → **Claw** (barracks) → War Factory (TL2) → Radar
(TL4) → Tech (TL5); defenses: **Drone Pit** (launches mini bomber
drones at targets, TL1), **Blaster Turret** (arcing green-plasma
artillery defense, Burst 5, MinRange), **Railgun Turret** (AA, cyan
railgun), **Eye of C.A.B.A.L.** (detection), **Nanomachine Core**
(support superweapon), **Iron Savior** (Scrin-tech energy cannon SW),
**C.A.B.A.L. Defender** (ultimate multi-weapon fortress: 2 lasers +
plasma cannon burst 8 + range-18 plasma artillery with FireRadius
shrapnel ring).

Units: Swarmling (T1 support inf), Gladiator (resilient cyborg,
4-burst 120mm), Abductor (ambusher drone), Cyborg Commando (plasma
cannon), Centurion (chaingun walker), **Cyborg Reaper** (missiles +
web launcher), Mobile Repair Vehicle, Hover Transport, **Drone Host**
(range-18 artillery whose shells spawn linked sentry drones at the
impact — `FireFragment` + `FireShrapnel` + `SpawnActor` warhead chain),
Minotaur (twin-laser walker), Wasp (railgun drone — same name as ours),
Basilisk (firestorm-rocket frigate), Devourer (siege frigate with a
melee **grinder** that applies `Slowdown50pp` on hit).

Faction economy mechanic — **nanomachine reanimation**: every organic
carries `SpawnCorpseOnDeath` (a reusable "corpse" token); the
Nanomachine Core power drops a field whose `SpawnActorsOnCorpseInRadius`
consumes corpses in 7c0 and reanimates them as `nanos` swarm actors for
CABAL. Strong candidate for our CABAL pillbox "assimilation" option or
a T4 support power.

Upgrade suite (all radar/tech-gated, cost 500–2500): Cybernetic Leg
Enhancements, Improved Reaper Nets, Limpet AA Targeting, Reclaim &
Recycle, Regenerative Materials, Gatling Cannons. Upgraded weapons are
`<Weapon>Upg` twins that differ ONLY in `DamageTypes` (`CabalDeath` →
`CabalDeathUpg`) — the upgrade effects hang off death types, keeping
weapon stats identical. Elegant pattern for our upgrade twins.

## 3. Exact effect recipes our design orders asked for

**Green plasma ball** (ordered for Commando/Mk2/new CABAL turret) —
SP's Cyborg Commando `CyCannon`:
```
Projectile: MissileTA            # we HAVE MissileTA (Mods.AS)
	Palette: jascgreen
	Image: greenplasma2          # SP asset — do NOT copy; recreate ours
	ContrailLength: 32
	ContrailStartColor: 0CD95740 (alpha 64) → ContrailEndColor: 0CD95710 (alpha 16)
	ContrailStartWidth: 0c172
Report: scrin5b.aud
Inherits@2: ^GreenPlasmaExplosion:
	CreateEffect: Explosions: plasmaballexplosion, ImpactSounds: expnew12.aud,
	ExplosionPalette: gensmkexploFgreen + LeaveSmudge Scorch
```
Turret-scale variant `BlasterProton`/`BlackCDefCannon`: same look on
`BulletAS` with `LaunchAngle: 42–120` (arcing volley) — exactly the
new green plasma turret. Artillery scale: `hugegreenplasma` image.

**Classic TS rocket trail** (ordered for all TS rockets): on the
missile projectile —
```
Projectile: MissileTA
	TrailImage: small_smoke_trail
	TrailSequences: idle2
```
That single pair IS the classic white TS smoke trail (CyborgRocket,
ReaperScythe, every SP rocket). Our TS rockets should adopt the
recipe; if we lack an equivalent trail sprite, create our own (SP's
asset itself is off-limits per the asset law).

**CABAL laser identity** (we ruled dark blue/purple): SP agrees —
Minotaur `PalaLazor`: LaserZap `Color: 1122FF88` + `SecondaryBeam`
white-cyan core `55fffff0`, Width 250/30, `HitAnimSequence: lazerflare`
palette `apblue`; Core Defender variant `0011FF88`. Railguns cyan
`22BBFF`/helix `44FFFF`. Recipe: main beam saturated deep blue at ~50%
alpha + thin near-white secondary core.

**Tracer performance trick**: `InstantHitWithFakeBullets` projectile
(Centurion vulcan) renders fake tracer bullets on an instant hit —
cheap high-ROF guns. NOT in our engine (port candidate).

**Web/stun on hit**: `SpreadDamageWithCondition` warhead (damage +
grants a condition like `WebDisable`/`Slowdown50pp` with duration on
victims). NOT in our engine — port candidate; useful for Reaper webs,
Devourer-style slows, and our corrosion mechanics.

## 4. Port candidates for the Cameo engine (OpenRA.Mods.Cameo)

Already present here: MissileTA, Railgun, FireFragment/FireShrapnel,
SpawnActorWarhead (AS), WarheadTrailProjectile (CA), LeaveSmudge
(Common), TakeOffOnMake (ported by tjk-ws).

High value, small ports:
- **ArmamentsChargeBar** — a charge-up UI bar for weapons; exactly what
  the Hacker / Ixian Projector charge mechanic needs.
- **SpreadDamageWithCondition** — see above.
- **InstantHitWithFakeBullets** — perf-friendly tracers for chainguns.
- **GuardsSelection** — support units ordered with a combat group
  auto-guard it (QoL for repair/medic units, incl. our new CABAL
  engineer).
- **SpawnCorpseOnDeath + SpawnActorsOnCorpseInRadius** — the
  nanomachine reanimation pair (CABAL flavor).
- **FirestromSP** — ring-shaped firestorm damage field.
- **WeaponWeather / CloudSpawner** — map-wide weapon weather (ion
  storms) and drifting cloud shadows; TS atmosphere.
- **SpawnSparks** — cheap ambient spark/effect emitter (CABAL bases).
- **WithMakeExplodeWeapon / WithSupportPowerActivationExplodeWeapon** —
  fire an effect weapon on build/power activation (visual polish).
- **ExplodesAlsoTransported** — passengers explode properly in
  transports (bug-class fix we may share).
- **AttackGarrisonedSP** — one fire port per passenger.
- AI: **UnpackBaseBotModule** (bot expands with spare MCVs),
  McvManagerSP; HarvesterBotModuleSP.

## 5. Differences worth noting vs our TS factions

- SP prices sit ~2–3× ours (Centurion 900, Minotaur 2000, Obelisk-class
  2500) — do NOT copy stats, only looks/mechanics; our balance comes
  from the Armor System workbook.
- SP's CABAL has no walls-of-cyborgs identity like 333ggg's concept —
  ours goes wider (Crab/Widow/Avatar, obelisk variants). Their roster
  overlaps ours on: Cyborg Reaper (web!), Cyborg Commando (plasma),
  Wasp, repair vehicle (we replaced with the repair-beam engineer),
  artillery spider (theirs deploys drones — a candidate twist for our
  Artillery Walker neutron upgrade instead of plain AoE?).
- Death feedback: per-death-type corpses/blood (`CabalDeath` etc.) and
  `SpawnHuskEffectOnDeath` (husk flies off as a projectile) — TS-feel
  polish we lack.
- Their garrison rule: `AttackGarrisonedSP` fire ports per passenger —
  compare with our §11 garrison law.

## 6. Follow-ups queued in ROADMAP

- TS authenticity pass now has its local reference (this doc, §3).
- CABAL weapon quality pass: use §3 recipes; Drone Host chain for
  ideas; keep our workbook stats.
- Engine port shortlist (§4) — needs design's priority pick.
- Reaper web upgrade (design 2026-07-11): SP's Improved Reaper Nets
  equivalent, implemented Cameo-style — a warhead that applies the
  existing `snared` condition (`^Snareable`, the Zerg Corruptor
  pattern) as a CABAL upgrade research improving TSReaperTrap.
- NO SP assets ever (asset law in the header) — recreate effects and
  sounds; only base-TS material or newly created work.
