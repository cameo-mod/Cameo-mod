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
        ConcreteScale: 25                 # slab damage per 2000 flat Damage, scaled by Versus[Concrete]
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

It also halves the `multi_main_fired_weapons` accounting problem: a percentage twin is a second
`Warhead@` node on 1235 weapons.

---

## 6. Risks and open questions — the honest list

**R1 — concrete damage is NOT proportional today, so folding it in CHANGES numbers.** Sampled
flat:concrete ratios: `oHMG` 1:1, `GoliathRockets_AA` 40:1, `Debris` 53:1, `SCUDNUKE` 300:1. There is
no constant to preserve, so any single `ConcreteScale` re-prices all 131 sites. That is arguably the
fix — the current values are noise — but it is a **balance change and needs a maintainer order and a
pipeline pass**, not a silent fold. ⚠ This is the one part that cannot be done "verbatim".

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

## 7. Suggested order

1. **C# first, behind compatibility.** Add `PercentageScale` / `PercentageSpread` / `PercentageVersus`
   / `PercentageTicks` / `ConcreteScale` to `AreaDamageWarhead`, defaulting to **off** so nothing
   changes. Keep `AreaDamagePercentageWarhead` working.
2. **Calibrate per family** from the measured medians; write them into the generator
   (`gen_weapon_template.py`), never by hand.
3. **Migrate in batches**, `review_batch_diff` clean per batch, boot gate per batch.
4. **Then** delete `AreaDamagePercentageWarhead` and the orphaned nodes.
5. **R1 (concrete) is a separate, later item** gated on a maintainer ruling, because it is the only
   half that cannot preserve current behaviour.
