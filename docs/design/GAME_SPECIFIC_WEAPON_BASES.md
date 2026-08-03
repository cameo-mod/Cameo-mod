# Game-specific weapon base templates ("special game inherits")

Research findings — companion to `WEAPON_3WAY_SPLIT.md` and the storage-architecture
question (per-game Shared projectiles/effects). Triggered by the `^TSDefaultMissile`
smell: *"It should only be the projectile, and a special explosion should be its own
effect inherit."* Correct — this doc catalogs every such base and how it decomposes.

Data source: accurate per-`^template` parse of the per-game weapon files
(`mods/cameo/weapons/{tiberiansun,redalert2,redalert2mod,d2k,tiberiandawn}.yaml`).
Central library + the 26 old full-stack templates live in `weapons.yaml` and are
covered by the main runbook; this doc is only about the **per-game intermediates**
that sit between the central templates and the concrete unit weapons.

---

## The 5 categories

| Category | Meaning | Roster |
|---|---|---|
| **ALREADY-3WAY** | Inherits central `@wh + @proj + @fx`, then adds a game-specific impact effect. The intended shape — except the game effect is *inlined* (see §3). | RA2: `^RA2SmallArms` `^RA2Chaingun` `^RA2FlakWeapon` `^RA2LightMissile` `^RA2MediumMissile` `^RA2HeavyMissile` `^RA2TankDestroyerCannon` `^RA2MediumCannon` `^RA2HeavyCannon`; TS: `^TSMG`; D2k: `^D2K_Cannon` `^D2KRocket` `^OCannon` |
| **PROJECTILE-base** | Has its own `Projectile:` art (game/faction sprite + trail) and often its own impact; carries **no** central warhead. This *is* the per-game projectile/effect layer — just not yet named/structured as `^Projectile_*` / `^Effect_*`. | TS: `^TSCannon` `^TSCannonEffect` `^TSLaserEffect` `^TSSonicGrenade` `^TSArtilleryWeapon` `^TSHealWeapon`; RA2: `^RA2MG`; RA2mod: `^SteelSmallArms` `^SteelChaingun` `^SteelLightMissile` `^SteelMediumMissile` `^SteelHeavyMissile` `^SteelMediumCannon` `^SteelHeavyCannon` `^LunarNaxGreenLasers` `^LunarNaxisUpgradedCannons`; D2k: `^HeavyMachineGunProjectile` `^D2KMissile` `^ORocket` `^OMissile` `^OMG`; TD: `^AMTProjectile` `^HVProjectile` |
| **single-old-WH** | Thin wrapper: inherits ONE central old template + a game effect. **Auto-converts** when that family runs in Phase 2 — no special handling. | RA2: `^RA2Grenade` `^RA2TeslaWeapon` `^RA2LaserWeapon` `^RA2RailgunWeapon`; TS: `^TSIonBeam`; RA2mod: `^AsianIonBeam` |
| **DUAL/STACK-WH** | Legacy hack: inherits **two** warhead bases to blend two armor-vs curves. Needs a maintainer-directed collapse to a single warhead (Phase 3). | TS: **`^TSDefaultMissile`** (the only true one left) |
| **other** | Effect/damage fragments (radiation, elite muzzle FX, energy blast) with no clean single warhead. | TS: `^TSEnergyBlast`; RA2: `^RA2EliteEffects` `^RA2RadShell`; RA2mod: `^NaxOxidationShells` |

Takeaway: after accurate parsing, the per-game base landscape is **healthy**. There is
only **one** real dual-warhead-stack base (`^TSDefaultMissile`); the rest are either
already-split, thin wrappers that convert automatically, or genuine per-game
projectile/effect art that the storage architecture already wants to relocate.

---

## §1 — Case study: `^TSDefaultMissile` (the trigger)

```
^TSDefaultMissile:
	Inherits: ^MediumMissile          # warhead base #1
	Inherits: ^LightMissile           # warhead base #2   <-- the smell
	ReloadDelay: 50
	Range: 6643
	Report: missile6.aud              # firing sound -> PROJECTILE layer
	Warhead@MediumMissile: SpreadDamage   { Damage: 10000 }
	Warhead@MediumMissilePercentage: HealthPercentageDamage { Damage: 5 }
	Warhead@LightMissile: SpreadDamage    { Damage: 10000 }
	Warhead@LightMissilePercentage: HealthPercentageDamage { Damage: 5 }
```

What it actually is: **not** a projectile and **not** an effect — it is a *dual-warhead
tuning wrapper*. It carries no `Projectile:` block and no `CreateEffect` of its own; it
inherits missile delivery + explosion from the central `^MediumMissile`/`^LightMissile`,
then stacks two SpreadDamage warheads (10000 each) so every hit applies **both** the
Medium and the Light armor-vs table at once — a legacy approximation of a specific curve.

Used by 5 weapons; each **overrides the projectile art per unit** (`Image: tsnodmmsil`,
`TrailImage: black_smokey`, …), so TS missiles are unit-specific art, not one shared sprite.

**Decomposition (target shape):**
- `@wh: ^Warhead_MissileAP_<lvl>` — collapse the Medium+Light blend to **one** warhead.
  *Which profile/level replaces the blend is a balance decision* → Phase 3, and per
  `cameo-warhead-change-permission` needs explicit maintainer sign-off.
- `@proj: ^Projectile_Missile_<lvl>` carrying `Report: missile6.aud`; each unit keeps its
  own `Image`/`TrailImage` override (or gets a faction projectile). Lives in **TS Shared**.
- `@fx:` central `^Effect_Missile_<lvl>` unless the unit has a special explosion, in which
  case its own `^Effect_Missile_TS…` (see §3).

Until the maintainer picks the collapse profile, `^TSDefaultMissile` correctly stays a
Phase-B **mixed** template: the retrofit tool leaves it and its children untouched (this
is what the resolution-based repair now guarantees — its children keep their
`-Warhead@MediumMissile` removals instead of orphaning).

---

## §2 — The `single-old-WH` wrappers convert for free

`^RA2Grenade`, `^RA2TeslaWeapon`, `^RA2LaserWeapon`, `^RA2RailgunWeapon`, `^TSIonBeam`,
`^AsianIonBeam` each inherit exactly one central old template. They are **single-inherit**,
so `retrofit_weapon_family.py --old <family>` converts them automatically when that family
runs (Grenade → Phase-2 explosions; Tesla/Laser/Railgun → the Energy pass). No manual work;
they only look untouched today because those families haven't run yet.

---

## §3 — The inlined-effect pattern (ALREADY-3WAY bases)

The RA2 bases do the right thing structurally but inline the game explosion instead of
naming it:

```
^RA2SmallArms:
	Inherits@fx: ^Effect_Bullet_Light     # central: Warhead@Effect = piff
	Warhead@Effect: CreateEffect          # OVERRIDE same key -> ra2_piff (clean, not doubled)
		Explosions: ra2_piff
	Warhead@EffectAir: CreateEffect        # NEW key -> adds an air variant
		Explosions: ra2_piff
```

The override is **clean** (same `Warhead@Effect` key → replaces `piff` with `ra2_piff`;
verified — no double impact). But it means the RA2 look is buried inside a warhead override
rather than being a reusable, relocatable template.

**Recommendation (storage architecture):** extract each game explosion into its own
`^Effect_<family>_<game>` template in that game's `Shared/` folder, and have the base
inherit it:

```
^RA2SmallArms:
	Inherits@wh: ^Warhead_Bullet_Light
	Inherits@proj: ^Projectile_Bullet_Light
	Inherits@fx: ^Effect_Bullet_RA2       # <- RA2 Shared, replaces the inline override
```

Minor latent bug noted for later: `^RA2SmallArms`'s `Warhead@Effect` keeps the parent's
`ValidTargets: Ground, Ship, Air` while also adding `Warhead@EffectAir` → air targets get
`ra2_piff` twice. Fold the fix into the extraction (give `Warhead@Effect` ground-only
targeting).

---

## §4 — Storage placement

| Layer | Central (global fallback) | Per-game Shared | Per-faction |
|---|---|---|---|
| **Warhead** | ✅ all `^Warhead_*` (artwork-independent) | — | — |
| **Projectile** | generic classic art (TD/RA1 `piff`, `120mm`, `DRAGON`) as fallback | TS/RA2/D2k/SC shared missile & shell sprites + trails (`^Projectile_Missile_TS`, …) | only if a single unit's projectile is unique |
| **Effect** | generic classic impacts as fallback | game explosions (`^Effect_Bullet_RA2`, `^Effect_Missile_TS`, …) — extracted from the inline overrides in §3 | only if unique to one unit |

The **PROJECTILE-base** roster in the table above is the raw material for the per-game
Shared layer: those templates already hold the game art; the work is to rename/relocate
them to the `^Projectile_*` / `^Effect_*` grammar and move the assets into the game's
`Shared/` pack so the dynamic loader pulls them only when a faction of that game is active.

---

## §5 — Maintainer decisions required

1. **`^TSDefaultMissile` collapse** — which single `^Warhead_MissileAP_<lvl>` (and level)
   replaces the Medium+Light blend for each of its 5 users? (Phase 3, warhead permission.)
2. **Per-game effect extraction** — approve extracting inline game explosions (§3) into
   `^Effect_<family>_<game>` templates in each game's `Shared/` folder. Naming: confirm
   `^Effect_Bullet_RA2` vs `^Effect_Bullet_Ra2` (game suffix casing).
3. **PROJECTILE-base relocation order** — which game's Shared projectile/effect pack to
   build first (recommend RA2, the largest and most inlined).

None of these block the remaining Phase-2 family retrofits (Flame/Chem/Explosions/Melee/
Arrow/Magic/Nuclear): those convert single-inherit blocks and correctly leave every base
in this doc as Phase B until the decisions above are made.
