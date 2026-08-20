# Per-game projectile & effect sourcing — RA2 (Romanov's Vengeance) + TS (Shattered Paradise)

Deep-research findings for the per-game projectile/effect layer of the weapon
3-way split (companion to `WEAPON_3WAY_SPLIT.md` + `GAME_SPECIFIC_WEAPON_BASES.md`).
Goal: build the `^Projectile_<fam>_<game>` and `^Effect_<fam>_<game>` template
libraries that dissolved weapons (`^RA2SmallArms` → atomic inherits) point at, and
identify missing RA2/TS art to replicate.

Sources studied (read-only reference mods, NOT dependencies):
- **RV** = `Romanovs-Vengeance-master/mods/rv/weapons/` — `defaults.yaml`, `missiles.yaml`,
  `bullets.yaml`, `explosions.yaml`, `flaks.yaml`, `mgs.yaml`, `gatling.yaml`.
- **SP** = `Shattered-Paradise-SDK-bleed/mods/sp/weapons/` — `weapondefaults.yaml`,
  `effectdefaults.yaml`, `explosionweapons.yaml`.

> **Headline finding:** SP already ships the exact 3-layer split cameo is building —
> separate `^RifleDamage` (warhead) / `^RifleWeapon` (projectile) / `^Small_Bang`
> (effect) template families. It is direct proof the architecture is sound; we can
> lift SP's effect library almost verbatim for TS.

---

## PART 1 — RA2 projectiles (from RV `defaults.yaml` + `missiles.yaml` + `bullets.yaml`)

RV routes every weapon through a tiny set of projectile bases. These map 1:1 to the
per-game templates cameo needs:

| Proposed cameo template | RV base | Projectile | Image | Signature traits |
|---|---|---|---|---|
| `^Projectile_Bullet_RA2` | `^MG` | `InstantHit` | — | hitscan, `Blockable: true` (no flight art — matches earlier finding) |
| `^Projectile_Shell_RA2` | `^LargeBullet` | `BulletAS` | `120mm` | `Speed 40c0`, `LaunchAngle 62`, `Shadow`, `Palette: ra` — the flat tank shell |
| `^Projectile_Missile_RA2` | `^Missile` | `Missile` | `DRAGON` | homing, `ContrailLength 8` / `ContrailStartWidth 38`, `ContrailEndColorUsePlayerColor: true`, `CruiseAltitude 4352`, turn-rate 220 |
| `^Projectile_Flak_RA2` | `^Flak` | `BulletAS` | `120mm` | `LaunchAngle 128`, arc; AA variant `^AAFlak` uses `InstantHit` |
| **`^Projectile_BallisticMissile_RA2`** (NEW, you requested) | `APTusk` / `V3StormStrike` | `BulletAS` | `radiationmissile` / `DRAGON` | `LaunchAngle 62`, `TrailImage: smokeyv3`, `Sequences: down`, arcing — V3 rocket / Dreadnought / Boomer sub |

### The contrail-color convention (variant encoding) — this is the key generalization
RV keeps ONE projectile per family and expresses the *variant* purely through
`ContrailStartColor`. Adopt this verbatim:

| Colour | Meaning | Seen on |
|---|---|---|
| `D8D8FF` | standard (light blue-white) | default missiles (HoverMissile, MissileLauncher) |
| `FFFFFF` | pure white | artillery shells (Grand Cannon, Howitzer, mortars) |
| `00b6ff` | **Boosted** (cyan) | `*Boosted` variants |
| `EA0000` | **Elite** (red) | `*E` variants |
| `FF8888` | Boosted+Elite (pink) | `*BoostedE` |
| `A8FFA8` | virus/toxin (green) | Mosquito |
| `FFA8FF` | chaos (magenta) | Chaos rocket |
| player-colour END | team identity | `ContrailEndColorUsePlayerColor` on all missiles |

So the "RA2 missile with classic white contrail + launch angle" you described =
`^Projectile_Missile_RA2` with `ContrailStartColor: D8D8FF` (or `FFFFFF`) + `MinimumLaunchAngle
/ MaximumLaunchAngle: 255` (vertical VLS). Light and Medium share this projectile and
differ only by contrail colour / speed; **Heavy/ballistic** (V3, Dreadnought, Boomer)
gets the separate `^Projectile_BallisticMissile_RA2` with the `smokeyv3` trail, exactly
as you called out.

### Launch-angle convention
- `255` (min=max) → vertical VLS launch, then homing → all standard missiles.
- `62` → flat direct-fire → tank shells (`^LargeBullet`).
- `120`–`196` → arcing → mortars, artillery, grenades.

---

## PART 2 — RA2 effects (from RV `defaults.yaml` + `explosions.yaml`)

RV's impacts are a **named size-ladder** of explosion sprites on a few palettes:

| Family ladder | sprites (small → large) |
|---|---|
| bang | `small_bang` · `medium_bang` · `large_bang` |
| collision | `small_clsn` · `medium_clsn` · `verylarge_clsn` |
| burn | `small_brnl` · `medium_brnl` · `large_brnl` |
| twilight/tumult | `large_twlt` · `large_tumu` |
| grey | `small_grey_explosion` · `medium_grey_explosion` |
| flak | `flak_puff` (ground) · `flak_puff_AA` (air) |
| specials | `terrorist_explosion`, `demotruck_explosion`, `nuke_explosion`/`nuke_ball`, `tesla_explosion`, `ivan_explosion`, `chaosgas50p`, `apoc_explosion`, `elite_explosion` |

Palettes: `ra` (unit), `tseffect`, `effect`, `player` (team-tinted), `effect50alpha`.
Water impacts always pair a `*_watersplash` (`small`/`large`/`huge`) CreateEffect.

Proposed per-family RA2 effect templates (impact + water + smudge, from the RV bases):

| cameo template | content (RV origin) |
|---|---|
| `^Effect_Bullet_RA2` | `ra2_piff` / `piffpiff` ground puff (already inline in `^RA2SmallArms` today) |
| `^Effect_Shell_RA2` | `medium_explosion` + `MediumCrater` smudge (`^LargeBullet`/`GrandCannonWeapon`) |
| `^Effect_Missile_RA2` | `small_grey_explosion` + `small_watersplash` + `SmallCrater` (`^Missile`/`HoverMissile`) |
| `^Effect_Flak_RA2` | `flak_puff` ground / `flak_puff_AA` air (`^Flak`/`^AAFlak`) |
| `^Effect_BallisticMissile_RA2` | `terrorist_explosion` + `MediumCrater, MediumScorch` (`V3StormStrike`) |
| `^Effect_Nuke_RA2` / `^Effect_Tesla_RA2` / `^Effect_Toxin_RA2` | the named specials above |

---

## PART 3 — TS projectiles & effects (from SP — near drop-in)

SP is TS-era and already 3-way-split. Lift it almost directly.

### TS projectiles (SP `weapondefaults.yaml`)
| cameo template | SP base | Projectile | Signature |
|---|---|---|---|
| `^Projectile_Bullet_TS` | `^RifleWeapon` / `^VulcanWeapon` | `InstantHitWithFakeBullets` | fake tracer bullets, contrail `FFFF00→FFAA00` (yellow) |
| `^Projectile_Shell_TS` | `^ArmorPierceAmmoWeapon` | `BulletAS` | `Image: cannonball`, `TrailImage: cannonsmokecircle`, contrail `BBC366→FF3311` |
| `^Projectile_Missile_TS` | `^RocketWeapon` | `MissileTA` | `Image: DRAGON`, `Sequences: idle2`, `TrailImage: small_smoke_trail`, `JetImage: explosion` (rocket flame), player palette, `CruiseAltitude 6000`, VLS angle 255 |

### TS effects (SP `effectdefaults.yaml`) — the whole named library is reusable
`^Piffs`/`^PiffsCyan` (bullet), `^Tiny_Explo`, `^Small_Clsn`/`^Mediuml_Clsn`/`^Large_Clsn`,
`^Small_Brnl`/`^Medium_Brnl`, `^Small_Bang`/`^Medium_Bang`, `^GreyExplo`,
`^Large_Explosion`, `^Small_Twlt`/`^Mediumtwlt`/`^Large_twlt`, `^FlameScorch`/`^FlameScorchBlue`,
`^Scrin_Pulse`/`^Scrin_Explo`, `^GreenPlasmaExplosion`, `^MeleeClaw`, `^Shrapnel`.
Palette: `gensmkexploj` (the TS smoke/explosion palette); water pairs `*_watersplash`
on `terrain`; every effect leaves a `LeaveSmudgeSP` crater/scorch.

Proposed mapping:
| cameo template | SP origin |
|---|---|
| `^Effect_Bullet_TS` | `^Piffs` (+`^PiffsCyan` for energy) |
| `^Effect_Shell_TS` | `^Small_Clsn` / `^Mediuml_Clsn` |
| `^Effect_Explosion_TS` | `^Small_Bang` / `^Medium_Bang` / `^Large_Explosion` |
| `^Effect_Flame_TS` | `^FlameScorch` (+`^FlameScorchBlue`) |
| `^Effect_Laser_TS` / `^Effect_Tesla_TS` | `^LightningDefault` / `^GreenPlasmaExplosion` |

> ⚠ **Armour-taxonomy caveat.** SP uses the TS armour set (`Infantry/Building/Light/
> Heavy/Defense/Aircraft/Concrete`); RV uses the RA2 set (`None/Flak/Plate/Light/Medium/
> Heavy/Wood/Steel/Concrete/Drone/Rocket`). Cameo has its OWN armour ladder — so we lift
> **projectile + effect (art only)** from these mods and keep cameo's central warheads /
> Versus tables. Never copy their `Warhead@…Dam` Versus blocks.

---

## PART 4 — Missing RA2/TS art to replicate & open items

**What cameo already has:** central `^Projectile_Missile_*` on classic `DRAGON` + `smokey`
trail; generic classic impacts (`piff`, `water_piff`). Bullets are hitscan (fine).

**What RV/SP add that cameo's RA2/TS weapons still lack (candidate imports):**
1. **Contrail-colour variants** (`D8D8FF`/`EA0000`/`00b6ff`…) — behaviour-only, no new
   sprites; apply on the new `^Projectile_*_RA2` templates.
2. **Ballistic-missile trail** `smokeyv3` + `radiationmissile` image (V3/Dreadnought/Boomer).
3. **RA2 shell** `120mm`/`160mm`/`cannonball` images + `cannonsmokecircle` trail.
4. **TS rocket** `JetImage: explosion` flame + `small_smoke_trail`.
5. **Named effect ladders** (`*_bang`/`*_clsn`/`*_brnl`/`flak_puff`) + palettes
   `ra`/`tseffect`/`gensmkexploj`.

Each candidate needs an **asset check** (does the `.shp`/palette already ship in cameo,
or must it be imported from RV/SP `artsrc`?) before implementation — that asset audit is
the next concrete step and is NOT yet done here.

**Open naming decision (from `GAME_SPECIFIC_WEAPON_BASES.md` §5.2):** game suffix casing
& order — `^Projectile_Missile_RA2` (recommended: central `…_<Level>`-style grammar, game
as most-specific suffix, `RA2` all-caps to match `^RA2…` bases and the `RedAlert2` folder).

**Sequencing:** RA2 first (largest, most inlined), then TS. Build order per game:
projectile templates → effect templates → dissolve the game bundles (`^RA2SmallArms` …)
onto the atomic `@wh`(central) + `@proj`(game) + `@fx`(game) inherits.

---

## PART 5 — cameo's CURRENT state (PRESERVE — do not regress)

Cameo already does a lot of this well; the task is to STRUCTURE it, not replace it.

### 5.1 The CABAL faction template — the per-faction exemplar
`ContentPacks/TiberianSun/CABAL/yaml/weapons.yaml` already ships `^CabalMissile`:
```
^CabalMissile:                       # inherited @5 (last) by every CABAL missile
	Projectile: Missile
		MinimumLaunchAngle: 255 / MaximumLaunchAngle: 255   # TS-vertical (SP's 90°)
		HorizontalRateOfTurn/VerticalRateOfTurn: 80         # tuned not to overshoot
		CruiseAltitude: 6000
		TrailImage: cabal_rockettrail                       # CABAL blue = faction identity
	Warhead@Effect: CreateEffect
		Explosions: cabal_missileexplosion                  # CABAL impact
```
This is a **per-faction projectile+effect template** (no warhead) — the exact thing to
generalize. Note it **combines** the projectile and effect layers in ONE template
(inherited last so its projectile wins), rather than splitting `@proj` + `@fx`.

### 5.2 Current inline per-weapon art (looks good — keep it)
RA2 weapons already carry per-weapon `ContrailStartColor` (BB8866, 88BB66, E60000,
EEEE00, 00EE00, 88FF44 …), `LaunchAngle` / `MinimumLaunchAngle`, trail images, and
impact overrides — e.g. the deployed Guardian GI (`RA2GIRocketsG`, `MinimumLaunchAngle:
200`) and its elite (`red_dragon` + `red_smokey` + `ContrailStartColor: E60000`). The
maintainer confirms the white contrails + launch angles look good. **Any restructure
must reproduce these values, not flatten them.**

### 5.3 The intended THREE-tier hierarchy (per maintainer)
| Tier | Who uses it | Example |
|---|---|---|
| **Central** (global fallback) | factions with no own art yet | classic `piff`, `DRAGON`+`smokey` |
| **Per-game original** | ORIGINAL RA2 / TS factions (Allies, Soviets, GDI, Nod) | RA2-authentic look (RV-sourced), TS-authentic (SP-sourced) |
| **Per-faction unique** | custom / crossover factions | `^CabalMissile` blue trail; Yuri green; Naxis; etc. |

So each faction can have its own projectile + effect artwork (like CABAL), while the
original TS/RA2 factions deliberately fall back to the original-game look.

### 5.4 Structural comparison (what to adopt)
| Approach | Structure | Trade-off |
|---|---|---|
| **cameo current** | per-weapon inline contrail/launch/effect | explicit, works, looks good; but art scattered, not DRY, not relocatable |
| **RV** | one shared `^Missile`/`^LargeBullet` base + per-weapon `ContrailStartColor` variant | DRY; variant via colour only; one base per family |
| **SP** | fully split `^RifleDamage`/`^RifleWeapon`/`^Small_Bang` | the pure 3-way ideal; many named templates |
| **CABAL (cameo)** | per-faction combined proj+fx template, inherited last | faction identity in one place; but merges `@proj`+`@fx` |

---

## PART 6 — DECISIONS (maintainer, 2026-08-03) + launch-angle finding

**Locked invariant.** Every weapon inherits **exactly ONE `@proj` + exactly ONE
`@fx`**, plus its warhead(s) mixed under the TYPES×LEVELS budget (1/2/4, see
`cameo-tier-weaponclass-law`). No weapon carries two projectile or two effect inherits.

**Granularity — SPLIT everything.** Projectile and effect are always separate templates
(`^Projectile_<fam>_<scope>` + `^Effect_<fam>_<scope>`), never combined. `^CabalMissile`
(the proj+fx bundle) is therefore **split** into `^Projectile_Missile_Cabal` +
`^Effect_Missile_Cabal`.

**Three-tier scope.**
- **Central** = global classic fallback (factions with no own art).
- **Per-game shared** = ORIGINAL factions of a game share it. Confirmed: **TS GDI + Nod
  share the same original TS art** → one `_TS` set; likewise RA2 Allies + Soviets → `_RA2`.
- **Per-faction unique** = crossover / custom factions get their OWN art. Confirmed
  unique: **Forgotten, CABAL** (and similar). CABAL blue trail stays, just split.

**Minimize inlining.** The template is the default; a weapon overrides it only when
*absolutely necessary*. The current good inline art (Guardian GI launch, elite red
trails, contrail colours) is preserved by folding the common case into templates and
leaving only true exceptions inline. **Move cameo's projectiles/effects closer to RV/SP.**

**Launch-angle investigation (answered).** RA2 sets a launch angle at 108 sites with no
dominant value (0,5,25,30,45,50,60,64,66,70,75,83,96,100,111,120,125,128,180,200,222,255…).
It CANNOT be one global value — but it clusters into **launch STYLES = projectile
subtypes**:
| Style | angle band | subtype template | examples |
|---|---|---|---|
| Vertical VLS homing | ~200–255 (min=max) | `^Projectile_Missile_<scope>` | Guardian GI (200), most homing SAMs |
| Ballistic arc | ~111–128 | `^Projectile_BallisticMissile_<scope>` | V3, Dreadnought, Boomer, artillery |
| Direct / low | ~0–75 | direct variant or inline override | flat-fire rockets |
So launch angle **moves into the projectile template per launch-style**, with a canonical
angle each; only genuine oddballs override. Guardian GI's 200 = the Vertical-VLS default.

**Sequence unchanged:** RA2 first, then TS; projectile templates → effect templates →
dissolve game bundles onto `@wh`(central) + `@proj` + `@fx`. Mechanical Phase-2 family
retrofits continue in parallel (they don't touch this layer).
