# Projectile templates — what belongs in them, and what should be DERIVED

**Maintainer 2026-08-17**, on finding that the three cannon projectile templates are the same
sprite: *"for cannons there is really only one cannon projectile so does it really make sense to
have light, medium and heavy cannons for the projectiles?"* … *"The sound should be always part
of the inlined weapon and not of the template anymore!"* … *"I want to see if we can bake the
inaccuracy and the speed already in the C# classes so we can just use that as a default."*

---

## 1. The measurement that started it

```
^Projectile_Shell_Light    Image: 120MM  Speed: 500  Inaccuracy: 150  Report: cannon1.aud
^Projectile_Shell_Medium   Image: 120MM  Speed: 500  Inaccuracy: 300  Report: tnkfire6.aud
^Projectile_Shell_Heavy    Image: 120MM  Speed: 500  Inaccuracy: 450  Report: tnkfire6.aud
```

Identical sprite, identical speed, identical shadow. The three-level split carries **one real
axis (`Inaccuracy`)** and a half-axis (two sounds across three levels). Missiles genuinely differ
— `^Projectile_Missile_*` and the TS set use different sprites — so the split earns its keep
there and does not here.

## 2. THE LAW — a projectile template describes the PROJECTILE

> A `^Projectile_*` template encodes what the projectile **is**: sprite (`Image`, `Sequences`),
> flight (`Speed`, `Shadow`, contrails, `HorizontalRateOfTurn`), and nothing else.
> **Sound belongs to the weapon. Balance numbers are DERIVED from the weapon's `Range`.**

Consequences:
* `^Projectile_Shell_{Light,Medium,Heavy}` collapse to **one** `^Projectile_Shell`.
* `Report:` moves out of every projectile template and onto each weapon inline — the maintainer's
  order, and it also unblocks the effect/sound pairing work, since a sound can then be chosen per
  weapon instead of inherited from a shared flight template.

## 3. Inaccuracy — the engine already scales it; the VALUE should be derived

⚠ **A distinction that is easy to miss.** `BulletInfo.InaccuracyType` defaults to
**`Maximum`**, documented as *"scale from 0 to max with range"*. So the current templates
**already scale inaccuracy with the shot's distance** — a Shell fired at half range is already
half as inaccurate. The three options:

| `InaccuracyType` | behaviour | fits |
|---|---|---|
| `Maximum` *(default)* | scales 0 → the stated value as the shot approaches max range | cannons |
| `PerCellIncrement` | grows from 0 by a fixed amount per cell, unbounded | artillery, artillery rockets |
| `Absolute` | the stated value regardless of distance | special cases only |

What is missing is not the scaling but **the stated value**, which is hand-written per template.
The maintainer's rule makes it derived:

```
Inaccuracy = InaccuracyPercentage% of the weapon's Range      (cannons: 1%)
Speed      = ProjectileSpeedPercentage% of the weapon's Range  (cannons: 10%, artillery: 2%)
```

⚠ **CORRECTION (measured 2026-08-17).** This document said *"the existing Shell templates are
`Speed: 500`, so 10% is already the de-facto convention and baking it in changes nothing."* That
was arithmetic on one hypothetical 5000-range cannon, not a measurement. Across the **145 weapons**
that actually inherit a `^Projectile_Shell_*` template:

| | today | derived at 10% of Range |
|---|---|---|
| Speed | flat **500** for all 145 | **300 … 1400** — `0.60×` to `2.80×`, and exactly ONE weapon lands on 500 |
| Inaccuracy | 150 / 300 / 450 by template | **30 … 140** — `3.2×` to `11.8×` tighter |

So deriving Speed is a real change: most cannons get a *faster* shell (the bulk land 540–700) and
the longest-ranged get 2.8×. Deriving Inaccuracy is a large accuracy buff, and E6 prices
inaccuracy at zero, so it would be free until E6 lands. Both go through the pipeline.

> ### ⛔ IT WAS NEVER RUNNING — fixed 2026-08-22
>
> `ScaledBullet` has carried `InaccuracyPercentage: 1` and `ProjectileSpeedPercentage: 10` on
> `^Projectile_Shell` for weeks, and **neither ever reached a single weapon.** The reason is in
> `ScaledBullet.cs`'s own comment: it derives a value **only when the field is still at
> `BulletInfo`'s default**, because FieldLoader cannot report which keys the yaml actually
> contained — so *an explicit yaml value always wins*. `^Projectile_Shell` also wrote
> `Speed: 500`, and `^Projectile_Shell_{Light,Medium,Heavy}` each wrote
> `Inaccuracy: 150/300/450`. Every one of those literals switched the derivation back off, for
> all **145** shell weapons.
>
> The literals are deleted. `Inaccuracy = 1% of Range` and `Speed = 10% of Range` now actually
> reach the engine, which is what the maintainer asked for again on 2026-08-22: *"for tank shells
> those should scale their inaccuracy with their maximum range."*
>
> **The general lesson:** a "derive it unless overridden" default is invisible when something
> upstream always overrides it. Assert the derived value on a real weapon, never just that the
> percentage field is present.

⭐ **The measurement also settles the collapse question.** The Light/Medium/Heavy assignment does
not correlate with range at all — `120mm_cobra` sits on `Shell_Light` at Range 7640 while `105mm`
sits on `Shell_Medium` at 5469. The three templates differ ONLY in a hand-written `Inaccuracy`, and
once that number is derived from each weapon's own Range there is nothing left to distinguish them
(same `Image`, same speed rule, same shadow) except two sounds that are moving onto the weapons
anyway. **The three-way split is not carrying an axis; the data says collapse it.**

⚠ **Inaccuracy is NOT already at 1%.** Current Shell values on a 5000-range cannon are
150 / 300 / 450 = **3% / 6% / 9%**. Adopting 1% makes cannons **3–9× more accurate** — a real
balance change, not a refactor, and it should go through the pipeline rather than ride along with
a cleanup. It also interacts with **E6** (inaccuracy is priced at zero today), so making cannons
accurate for free would be a silent buff until E6 lands.

### The per-family table (proposed)

| family | `InaccuracyType` | inaccuracy | rationale |
|---|---|---|---|
| Cannon (direct fire) | `Maximum` | 1% of Range | aimed weapon; error grows with distance |
| Artillery shell | `PerCellIncrement` | per-cell | indirect fire; error compounds, unbounded |
| Artillery rocket (e.g. `latinsyndicate_burrito`, MissileHE) | `PerCellIncrement` | per-cell, **larger than a shell** | unguided rocket, worse dispersion than a rifled barrel |
| MissileAP / MissileAA | `Absolute` | **0** | guided; accuracy is the point. Spread lives in the WARHEAD, not here |

**Shell vs rocket** — the maintainer asked whether there should be a difference. There should, and
the physical reason gives the direction: a shell leaves a rifled barrel with a predictable
ballistic arc, while an unguided rocket accumulates error the whole time its motor burns. So both
are `PerCellIncrement`, and the **rocket's per-cell increment is larger**. Speed differs the same
way: a rocket accelerates, so its `ProjectileSpeedPercentage` is lower than a shell's off the
line.

## 4. ✅ IMPLEMENTED — `ScaledBullet`, and it is NOT a shadow

Shipped as `OpenRA.Mods.Cameo/Projectiles/ScaledBullet.cs`. Three findings changed the plan this
document originally set out:

* ⭐ **No shadow, and no 367-line copy.** `Ruleset.cs:70` calls `RulesetLoaded` on the projectile
  info with the firing `WeaponInfo` — so `ScaledBulletInfo : BulletInfo, IRulesetLoaded<WeaponInfo>`
  reads `Range` at LOAD time and derives both values once, inheriting every existing field
  (`Image`, `Sequences`, `Shadow`, `TrailImage`, `InaccuracyType`, …) for free.
* **Deliberately a NEW type rather than a `Bullet` shadow.** This document warned that shadowing
  `Bullet` silently changes every weapon that says `Projectile: Bullet` — the most-used projectile
  in the mod. A separate name means a template opts in and nothing else moves.
* The derived values are written through the same reflection FieldLoader itself uses on
  `public readonly` fields, which is why no engine change is needed.

```
InaccuracyPercentage: 1        # of the weapon's Range; 0 disables
ProjectileSpeedPercentage: 10  # of the weapon's Range, in WDist/tick; 0 disables
```

⚠ **An explicit `Inaccuracy` / `Speed` in yaml still WINS**, so this is a default, never a
constraint. "Explicit" is detected as *differs from `BulletInfo`'s own default*, because
FieldLoader does not report which keys the yaml contained — so a weapon that deliberately writes
`Inaccuracy: 0` or `Speed: 17` **and** sets a percentage gets the derived value instead. Set the
percentage to 0 there.

The three Shell templates now say `Projectile: ScaledBullet` and carry both percentages **with
their explicit `Speed` / `Inaccuracy` kept**, so behaviour today is byte-identical and the boot
proves the type and its Cameo-only fields load (an unknown field throws at load). Deleting the
explicit lines is what activates the rules — and that is the balance change measured above.

## 5. The original plan — a mod-side shadow (superseded by §4)

`ProjectileArgs` carries the firing `WeaponInfo`, so the projectile can read `Range` at creation
and compute both defaults. That means a Cameo-side `Bullet` (and `Missile`) with two new fields:

```
InaccuracyPercentage    : int  (0 = disabled, use the explicit Inaccuracy)
ProjectileSpeedPercentage: int  (0 = disabled, use the explicit Speed)
```

An explicit `Inaccuracy` / `Speed` in yaml always wins, so this is a DEFAULT and never a
constraint — which is what makes it safe to roll out family by family.

⚠ **`Bullet` is the most-used projectile in the mod, so shadowing it is wide-reaching.**
`ObjectCreator.FindType` takes the first assembly in `mod.yaml`'s list (AS, CA, **Cameo**, Cnc,
D2k, Common), so an `OpenRA.Mods.Cameo.Bullet` silently replaces the Common one for **every**
weapon that says `Projectile: Bullet`. Prove the shadow is live before trusting it: give the
Cameo Info a field the engine's lacks and boot with that field set — `--docs` lists both types
and proves nothing (LESSONS_LEARNED).

## 5. Order of work

1. **Sound out of templates** (mechanical, no balance effect) — move every `Report:` from
   `^Projectile_*` onto the weapons that inherit it. Needs a sweep tool: a weapon that inherits
   two templates must end up with exactly the sound it resolves to today.
2. **Collapse `^Projectile_Shell_*` → `^Projectile_Shell`**, moving `Inaccuracy` to the weapons so
   resolved behaviour is preserved verbatim.
3. **Then** the C# defaults, family by family, starting with cannons.
4. **Separately, through the pipeline:** the 3% → 1% inaccuracy decision, which is balance.

## 6. ✅ IMPLEMENTED — dedicated artillery projectile families

Global `^Projectile_ArtilleryShell_Medium` and `^Projectile_ArtilleryRocket_Medium` now live in
`mods/cameo/weapons/weapons.yaml`. The rocket family was previously duplicated in
`mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml`; that copy was removed so the global
family is the single source of truth. `RA2RTruckRocket`, `TSChemVanMissile`, `TSChemMLRSMissile`,
and `Future_MultiMissile_Frag` all inherit the rocket family and keep their weapon-local
`Speed` / `Inaccuracy` / `LaunchAngle` / `Arm` overrides. The shell family is used by
`TSChemJuggerboat90mm`. Both families keep `Blockable: false` and `Shadow: true` by default.
