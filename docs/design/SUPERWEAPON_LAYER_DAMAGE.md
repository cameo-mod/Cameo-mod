# Superweapons vs the layered health stack — why "75%" is not 75%

**Maintainer 2026-08-17:** *"we need to make sure that any super weapon deals the same total
damage to shielded and unshielded buildings. That is the AtomicCore. It is supposed to destroy
75% of the target HP but with shields that is more complex ... Instead the Versus Values of the
Atomic Core which uses the Nuclear Warhead must be adjusted so the total damage it deals to
shields and then down to HP is the same, right? Do you know how to do that?"*

Short answer: **yes — but not with Versus values.** Versus can fix one weapon against one
building at one HP, and it breaks the moment any of those three change. There is a mechanism
that is invariant to all of them, and it is a small mod-side change. The algebra below is why.

---

## 1. What AtomicCore does today

`^Warhead_Nuclear_Super` (`mods/cameo/weapons/weapons.yaml:9243`) carries TWO damage warheads:

| component | value | scales with |
|---|---|---|
| `Warhead@Nuclear_Super: AreaDamage` | weapon sets `Damage: 500000` | nothing — a FLAT number |
| `AreaDamagePercentage` twin | `Damage: 25` = 25% of max health | the target's max HP |

Relevant Versus rows: `Concrete: 100`, `Shield: 155`.

The comment in the file states the intent: *"AtomicCore = 500000 flat + 25% → 500k + 250k =
750k (75%) on a 1,000,000-HP Concrete construction yard."*

### ⚠ Bug 1 — "75%" is true at exactly ONE building size, and shields have nothing to do with it

A flat term plus a percentage term is only a fixed percentage at one HP value:

| target max HP | flat | 25% | total | fraction destroyed |
|--:|--:|--:|--:|--:|
| 200 000 | 500 000 | 50 000 | 550 000 | **275% — guaranteed kill** |
| 1 000 000 | 500 000 | 250 000 | 750 000 | 75% ✅ the design point |
| 2 000 000 | 500 000 | 500 000 | 1 000 000 | **50%** |

So the "destroys 75%" contract already fails on every building that is not exactly 1M HP. **A
percentage contract can only be met by a pure percentage.**

### ⚠ Bug 2 — against a converted shielded building it does not even reach the health bar

Take the §W26 conversion (half HP, 200% shield of the halved HP), so health = 500 000 and the
shield pool = 1 000 000:

```
flat : 500 000 x (Shield 155/100)              = 775 000
pct  : 500 000 x 25/100 x (Shield 155/100)     = 193 750      <- % is of max HEALTH (500k), not the pool
total                                          = 968 750   applied to a 1 000 000 pool
```

The shield **survives with 31 250 left. Health is untouched — the structure is at 100%.**

In raw-damage currency (what the attacker must spend):

| | raw durability | nuke spends | destroyed |
|---|--:|--:|--:|
| unshielded 1M Concrete | 1 000 000 | 750 000 | **75%** |
| converted shielded | 1 145 161 | 625 000 | **54.6%** |

---

## 2. Why adjusting Versus CANNOT solve this in general

For the 50%-HP/200%-shield conversion to be **durability-neutral** against a given weapon, that
weapon needs

```
Versus[Shield] = 2 x Versus[the building's armor row]
```

*Derivation.* Raw damage to kill an unshielded building is `H x 100/V_c`. After conversion it is
`(H/2) x 100/V_c` for the health plus `H x 100/V_s` for the pool. Setting them equal gives
`0.5/V_c + 1/V_s = 1/V_c`, i.e. `V_s = 2 V_c`.

⭐ This is the same fact as the **185.2% break-even pool** (`100 / shield_hp_factor`): both say
that converting HP into an equal-value shield means undoing exactly the Shield row's average
penalty. AtomicCore has `Shield 155` against `Concrete 100`, i.e. 1.55x where neutrality needs
2.0x — which is precisely why the converted building came out *tougher*.

**And this is why Versus is the wrong tool:** neutrality would have to hold for **every**
weapon, which forces the entire `Shield` column to be exactly 2x the building columns. That
column is the mod's anti-shield rock-paper-scissors axis (Tesla 369, Melee ~76, DESIGN §12.0c).
Pinning it to 2x the building rows **destroys the axis**. Fixing AtomicCore alone with a Versus
tweak is possible (`V_s = 2 x V_c = 200`) but it is a hard-coded coincidence: it silently breaks
if the 50/200 split changes, if the building's armor row is Steel or Wood rather than Concrete,
or if the 75% target moves.

---

## 3. The mechanism that IS invariant — apply the percentage PER LAYER

**The contract "remove 75% of the target" should be read as "remove 75% of every layer".**

```
unshielded : 75% of health                       = 75% of durability ✅
shielded   : 75% of shield  +  75% of health     = 75% of durability ✅   (any split ratio)
```

This is invariant to **the HP scale, the shield/health split, the Versus rows, and any future
layer** (plating, Integrity). It needs no Versus coordination at all, which is exactly what
makes it maintainable.

### What changes

`OpenRA.Mods.Cameo/Warheads/AreaDamagePercentageWarhead.cs` currently computes

```csharp
var damage = Util.ApplyPercentageModifiers(healthInfo.HP,
    args.DamageModifiers.Append(Damage, DamageVersus(victim, shape, args)));
```

`healthInfo.HP` is the **health layer's** maximum, so on a shielded target the percentage is
taken against half the durability and then spent on the wrong bar. Two additions:

1. **`PerLayer: true`** — resolve the percentage against each layer's OWN maximum (shield pool
   max, then health max) and apply to each, instead of one number cascading.
2. **`IgnoreVersus: true`** — for a superweapon the percentage is a contract, not a damage
   type. A nuke removing "75% of what you are" should not care that the outer layer happens to
   be a shield. (Keep Versus on the FLAT component, where armor type belongs.)

Both are mod-side; `engine/` is untouched.

### AtomicCore afterwards

```
Warhead@Nuclear_Super_Percentage: AreaDamagePercentage
    Damage: 75            # 75% of EVERY layer
    PerLayer: true
    IgnoreVersus: true
```

and the flat 500 000 either goes away or shrinks to a splash value for units — its only current
job is to make the percentage come out right on one specific building, which the percentage now
does by itself.

⚠ **Ordering:** this must land before superweapons are priced. It changes what AtomicCore does
to 1016 buildings.

---

## 4. Open question for the maintainer

Per-layer 75% on a shielded building destroys 75% of the shield **and** 75% of the health, so
the building survives at 25% with a 25% shield. The old behaviour (*"kills the shield first
before applying damage to the HP"*) left the structure at 25% health with **no** shield. Both
destroy 75% of durability; they differ in what is left standing:

* **per-layer** (recommended) — proportional, keeps the shield's identity, trivially invariant;
* **strip-then-cascade** — more dramatic, matches the old feel, but the amount that reaches
  health depends on the Versus rows again, which is the coupling this document exists to remove.
