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

Sanity check against what ships: a 5000-range cannon → Speed 500. **The existing Shell templates
are `Speed: 500`, so 10% is already the de-facto convention** and baking it in changes nothing.

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

## 4. Implementation — a mod-side shadow, no engine change

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
