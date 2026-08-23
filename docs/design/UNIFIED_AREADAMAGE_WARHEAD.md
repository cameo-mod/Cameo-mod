# One warhead, one number — folding the percentage twin and the concrete warhead into `AreaDamage`

_Feasibility analysis, 2026-08-19. Maintainer proposal:_

> *"can we do the same for the percentage twin? … one trait that combines both. You have Versus and
> you have percentage versus, then you have the normal damage and the percentage damage. And you can
> set a scaler of how much percentage damage the weapon does compared to the flat damage … Only ONE
> number inline, everything else in the templates. The templates will be like the big brain and the
> inline weapon like an individual brain cell."*

**Verdict: yes, and the tree is already shaped for it.** The percentage twin is not an independent
warhead — it is the flat warhead with three constants applied, and the tree agrees on all three.

---

## 1. Why it works: the twin is already a subclass with ONE differing expression

`AreaDamagePercentageWarhead` is **65 lines** and inherits everything from `AreaDamageWarhead`.
It overrides exactly one method, and the only real difference is what the damage is a percentage OF:

```csharp
// AreaDamagePercentageWarhead.InflictDamage
var damage = Util.ApplyPercentageModifiers(healthInfo.HP,        // <-- flat half passes `Damage`
                args.DamageModifiers.Append(Damage, DamageVersus(victim, shape, args)));
```

Spread, Falloff, Ticks, MinRadius/MaxRadius, friendly fire, `PhysicalState`, `IntegrityScale` —
all inherited, all already shared. There is no second mechanism to merge, only a second *base
quantity*. `DamageVersus` is `protected virtual` and already overridden in `AreaDamageWarhead`, so a
second lookup table is a normal extension, not surgery.

## 2. The three constants are already the convention — and 2544 hand-typed numbers drift from them

Measured across every resolved weapon carrying a flat/percentage pair:

| relationship | measured | conformance |
|---|---|--:|
| `percentage = flat / N` | **N = 2000, median, in ALL 22 families** | 2544 pairs |
| `percentage Spread = flat Spread / 2` | ratio **0.50** | **2381 / 2487 (96%)** |
| `percentage Falloff = flat Falloff` | identical | **2499 / 2544 (98%)** |

The rule already exists — `WEAPON_3WAY_SPLIT.md` calls it *"% = 1-per-2000"*. What does **not** exist
is enforcement, and the per-family min/max shows what that costs:

    Bullet_Medium    250 … 12000   (48x spread around the intended 2000)
    Railgun_Heavy   2000 … 80000   (40x)
    Demolition_Light 167 …  4000   (24x)

Every one of those is a weapon whose percentage chip is silently 5x or 40x off intent, and **nothing
in the audit suite can see it**, because each value is individually plausible. Baking the ratio into
the warhead does not just remove typing — it removes the entire error class.

## 3. The design

Everything below lives in the **template**. The weapon inline sets `Damage:` and nothing else.

```yaml
^Warhead_CannonHE_Heavy:
    Warhead@CannonHE_Heavy: AreaDamage
        Damage: 2000                      # ← the ONLY number an inline weapon overrides
        Spread: 800
        Falloff: 100, 50, 25, 10, 5, 0
        Versus:                           # flat table
            Heavy: 108
            ...
        # ---- percentage half, folded in ----
        PercentageScale: 100              # 0.01%-units of max HP per 2000 flat Damage.
                                          #   100 == today's "1% per 2000". THIS is the per-family
                                          #   dial: a chemical family scales harder, a kinetic one
                                          #   softer, without touching a single weapon.
        PercentageSpread: 50              # % of the main Spread — mirrors FriendlyFireSpread: 50
        PercentageVersus:                 # its own table; falls back to Versus when omitted
            Heavy: 90
            ...
        # ---- concrete half, folded in ----
        DamagesConcrete: true             # slab damage = Damage x Versus[Concrete] / 100, 1:1.
                                          #   NO scale knob — ruling 2026-08-19: a wall, a building
                                          #   and a slab all take the same Concrete damage.
        # ---- already folded in (the friendly-fire precedent) ----
        FriendlyFireDamage: 50
        FriendlyFireSpread: 50
```

and the weapon becomes:

```yaml
SomeCannon:
    Inherits@wh: ^Warhead_CannonHE_Heavy
    Inherits@proj: ^Projectile_Shell_Heavy
    Inherits@fx: ^Effect_CannonHE_Heavy
    ReloadDelay: 40
    Range: 6144
    Warhead@CannonHE_Heavy:
        Damage: 40000                     # one number. The percentage and the slab damage follow.
```

`PercentageDenominator` moves to **10000** (0.01% steps) so the derived value has room: today's
integer percentages run 1–42, which is a 42-step ladder for the entire game. At 0.01% steps the same
range is 100–4200 and the derived number is never forced to round.

## 4. The concrete half is reachable mod-side — no engine change

`DamagesConcreteWarhead` (in `OpenRA.Mods.D2k`) is **twelve lines**:

```csharp
var layer = world.WorldActor.Trait<BuildableTerrainLayer>();
var cell = world.Map.CellContaining(target.CenterPosition);
layer.HitTile(cell, Damage);
```

and `OpenRA.Mods.Cameo.csproj` **already references `OpenRA.Mods.D2k`** (line 16), so `AreaDamage`
can call `HitTile` directly. This does not touch `engine/` and needs none of the engine-update
pipeline. Routing it through `Versus[Concrete]` is the maintainer's proposal and is self-consistent:
a weapon that is good against concrete *buildings* is good against concrete *slabs*.

## 5. What it removes

| | nodes |
|---|--:|
| `*_Percentage` warheads on concrete weapons (1235 weapons) | **2946** |
| `*_Percentage` warheads in templates | 166 |
| `DamagesConcrete` warheads on concrete weapons (55 weapons) | 56 |
| `DamagesConcrete` warheads in templates | 75 |
| **total yaml nodes deleted** | **3243** |

⛔ **EVERY pure concrete warhead is DELETED, not kept alongside** (maintainer: *"you need to remove any
pure concrete damage warheads now because it's all included into the main area damage warhead right?
so no more duplicates right?"* — yes). A surviving `Warhead@Concrete: DamagesConcrete` next to a folded
`AreaDamage` would hit the slab twice. `audit_weapon_identity` and `review_batch_diff` both run per
batch, and a leftover is exactly the kind of double-application that neither the boot gate nor a
damage-total check would flag.

It also halves the `multi_main_fired_weapons` accounting problem: a percentage twin is a second
`Warhead@` node on 1235 weapons.

---

## 6. Risks and open questions — the honest list

**R1 — RESOLVED BY RULING (maintainer, 2026-08-19).** *"I want 1:1 any damage to concrete to go to
the concrete slabs. So doesn't matter if the target is a wall or a building or a concrete slab, the
damage it deals to concrete is definitely going to all of them equally and if we need we should
increase the concrete slab health. So I say do it!"*

So there is no `ConcreteScale` at all — **slab damage IS `Damage × Versus[Concrete] / 100`, 1:1**, the
same number a concrete wall or building would take. One rule, no third knob, and the `Concrete` Versus
row means exactly what it says everywhere it appears.

⚠ The compensation moves to the SLABS, not the weapons. Current `DamagesConcrete` values are tiny and
unrelated to main damage (sampled ratios `oHMG` 1:1, `GoliathRockets_AA` 40:1, `Debris` 53:1,
`SCUDNUKE` 300:1), so routing full damage at 1:1 makes slabs melt unless `BuildableTerrainLayer`'s
per-cell health is raised to match.

### ⭐ RULED: `BuildableTerrainLayer.MaxStrength` 9000 → **6 000 000**

`MaxStrength` is currently the engine default 9000 — `mods/cameo/rules/world.yaml:1056` declares
`BuildableTerrainLayer:` with no override at all. Maintainer 2026-08-19 asked first for 200x, then *"make it a nice 2 million"* — and the
percentage term below rules out both.

**Two measurements, and they disagree — which is why 200 is better than either.**

    effective main damage : current concrete damage      (1495 weapons carrying both)
        lower quartile     68 : 1
        MEDIAN            166 : 1
        upper quartile    352 : 1
        ratio of medians  144 : 1        <- 14 400 effective main vs 100 concrete

Median-of-ratios (166) and ratio-of-medians (144) are both defensible and neither is the whole story,
because **the fold also widens the attacker pool**:

| | weapons |
|---|--:|
| damage slabs TODAY (carry a concrete warhead) | 1504 |
| have a `Concrete` Versus row, so damage slabs AFTER | **1987** |
| **gain the ability** | **+483** (×1.32) |

⛔ **AND THEN THE PERCENTAGE HALF CHANGES THE ANSWER AGAIN.** Maintainer: *"don't forget the percentage
damage is still there so at that high value they should be kind of quickly destroyed from the
percentage values alone right?"* — correct, and it is decisive. Median effective percentage vs
`Concrete` is **0.9%**, and a percentage is a fraction of MAX strength, so it does not care how big
that maximum is:

    slab HP       flat-only weapon      weapon carrying percentage
     1 800 000        143 shots                  62
     2 000 000        159                        65
     3 000 000        238                        76
     6 000 000        476                        90      <-- today's 90
    10 000 000        794                        97
    20 000 000       1587                       104
    ceiling             --                      111      <-- UNREACHABLE at any HP

**Percentage damage CAPS slab durability no matter how high the HP goes.** At 0.9% per shot nothing
can ever take more than ~111 median shots, so past ~3M more HP buys almost nothing.

That kills 200× (1 800 000): with percentage applied it gives **62 shots — 1.4× SOFTER than today's
90** — and 483 more weapons can hit it on top. The exact opposite of "really tanky".

**6 000 000 is the number.** It is the parity point exactly
(`90 × 12600 / (1 − 90 × 0.009) = 5 968 421`) and it produces the design the ruling was reaching for:

- a weapon with **no** percentage half needs **476 shots** — concrete really is a fortification;
- a weapon **carrying** percentage clears it in **90** — exactly today's feel.

Percentage becomes *the* answer to concrete and flat damage alone effectively cannot break it, so the
rock-paper-scissors is structural rather than tuned. The quartile spread 68…352 is the noise the 131
hand-set values encode; after the fold it collapses into one honest number.

⚠ **The percentage half MUST therefore apply to slabs**, as a fraction of `MaxStrength`. If it stays
flat-only, 6 000 000 means 476 shots for every weapon and concrete becomes near-immortal.

⚠ Still a balance change: `world.yaml` + boot gate + a play check on D2k concrete-heavy maps, where
slab durability is a real strategic layer rather than a detail.

**R2 — ~106 pairs do not use the 0.50 spread rule** (56 at 1.00, 28 at 10.00, 9 at 5.00) and **45 use
a different Falloff.** They need either an explicit per-weapon escape hatch or individual conversion.
Do NOT let the migration quietly normalise them; that is exactly how the nuclear batch lost 93%.

**R3 — `Ticks` can differ between the halves.** `ThermobaricNuclearMaverick` ran the flat half at 10
ticks and the percentage half at **7**. A merged warhead shares one tick count unless a
`PercentageTicks` is added. Decide before migrating, not during.

**R4 — the meter may currently be fed TWICE.** Both `InflictDamage` paths call `ApplyPhysicalState`
and `ApplyIntegrityScale`. If a template sets `PhysicalStateScale` on both halves, the physical-state
meter and the integrity layer receive two contributions per hit. Merging makes that structurally
impossible — which is a **fix**, but it will move E2's delivery numbers, so re-measure after.

**R5 — scale of the mechanical change.** 3243 nodes across 1235 weapons. This must be a scripted
migration verified with `review_batch_diff.py` (main damage AND blast shape) plus a boot gate, in
batches. `PercentageScale` must be calibrated per family from the *measured* current ratio, not set
to a uniform 100, or families that legitimately drifted get re-priced by accident.

---

**R6 — ⛔ THE PREREQUISITE I MISSED, and it is most of the work.** §1's "65-line subclass, easy
merge" is only true for the twins that are already the Cameo type. Measured on the live tree:

    resolved *_Percentage warhead nodes
        HealthPercentageDamage (stock engine)   3378     <-- NOT foldable as-is
        AreaDamagePercentage   (Cameo)          1426
    `PercentageDenominator` anywhere in mods/      0     <-- the basis-point rollout never landed

`HealthPercentageDamage` is a different warhead with no `PercentageDenominator`, no `Ticks`, no
`Spread`/`Falloff` — there is nothing to fold it INTO until it is migrated. **So the fold is gated on
W18**, which already specs exactly this migration plus `PercentageDenominator: 10000` (the 0.01% steps
this design needs) and is marked READY on the board. Do W18 first and the fold becomes mechanical;
skip it and 3378 nodes have no path.

---

## 7. Suggested order

0. ⛔ **W18 FIRST** — migrate the 3378 `HealthPercentageDamage` twins to `AreaDamagePercentage` and
   roll out `PercentageDenominator: 10000`. Behaviour-preserving, already specced, already READY, and
   without it 3378 of 4804 nodes cannot be folded at all (R6).
1. **C# next, behind compatibility.** Add `PercentageScale` / `PercentageSpread` / `PercentageVersus`
   / `PercentageTicks` and the 1:1 concrete routing to `AreaDamageWarhead`, defaulting to **off** so
   nothing changes. Keep `AreaDamagePercentageWarhead` working.
2. **Calibrate per family** from the measured medians; write them into the generator
   (`gen_weapon_template.py`), never by hand.
3. **Migrate in batches**, `review_batch_diff` clean per batch, boot gate per batch.
4. **Then** delete `AreaDamagePercentageWarhead` and the orphaned nodes.
5. **R1 (concrete) is a separate, later item** gated on a maintainer ruling, because it is the only
   half that cannot preserve current behaviour.
