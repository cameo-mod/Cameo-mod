# Physical-State System — damage-scaled status meters (design spec, 2026-08-08)

Status: **DESIGN — awaiting maintainer sign-off on the OPEN questions (§7) before any C# is written.**
Owner: this doc is the authority for the physical-state / status-effect layer. Companion:
`AREADAMAGE_WARHEAD_REBALANCE.md` (warhead design), `SPREAD_FALLOFF_PLAN.md` (spread), memory
`cameo-weapon-differentiation` + `cameo-weapon-structure-rules`.

## 1. Concept — separate DAMAGE from STATUS

A weapon = **one damage type** (its `Versus` armor profile) **+ optional status layers** (orthogonal
effects). Damage types stay FEW and distinct; status layers give combinatorial variety WITHOUT
diluting damage-type identity. The engine already works this way (Tesla = damage + EMP; Cryo = damage
+ freeze; Sonic = damage + slow/vuln). This spec formalises the status layer as **damage-scaled
meters** that accumulate per hit and trigger scaling/threshold reactions.

## 2. The two meters (physical-state axes)

Both are **accumulators**: each hit adds `amount = effective_damage × Scale%` to the target's meter;
the meter **decays** over time; reactions read the meter LEVEL.

| Meter | Bar colour | Range | Sources (Scale%) | Reaction |
|---|---|---|---|---|
| **Temperature** | 🔴 hot / 🔵 cold | **bipolar** (−cold … +hot) | Flame **+100**, Laser **+75**, Plasma **+50**, Cryo **−100** | **hot threshold → overheat**; **cold threshold → freeze/slow** |
| **Corrosion** | 🟢 green | **unipolar** (0 … max) | Chemical **+100**, Plasma **+50** | builds up; **50%→100% escalating** DoT (high/tick, short) + slow + +damage-taken |

- **Temperature is bipolar on one axis**: heat and cold push opposite directions, so a flamethrower
  can thaw a frozen unit and vice-versa. Engine already models `PhysicalStateName: Temperature`
  with `Amount` (negative = cold, proven on the FutureTech Cryocopter) + a `^CryoFreezable` receiver.
- **Corrosion is the Schwarzer Mond "corruption" effect** (Korruptes Biest / Rocket Soldier), converted
  from a flat on-hit condition into a **meter that fills with repeated hits** and whose punch ramps
  from 50%→100% — rewards sustained fire.

## 3. C# mechanism — fold it into AreaDamage (one warhead, no extra warheads)

Add two optional fields to `AreaDamageWarhead.cs` **and** its `AreaDamagePercentageWarhead` subclass:

```
PhysicalStateName: Temperature   # or Corrosion; default "" = off
PhysicalStateScale: -100         # amount = effective damage × Scale/100 (signed)
```

- **Placed on the main + `_Percentage` warheads only, NEVER on `_ExtraDamage`** → the energy chip is
  auto-excluded (maintainer rule: "scale with main damage, not extra damage"), and the %HP twin also
  feeds the meter (maintainer rule: "scale with the area percentage damage as well").
- One `AreaDamage` warhead applies damage **and** the state with the **same Spread/Falloff** — replaces
  the 3 stacked `ApplyPhysicalState` rings the Cryocopter currently uses.

### Scaling (what the amount tracks)
- **Armor (Versus) + Falloff: automatically** — the amount is a fraction of the warhead's *computed*
  damage, which already has armor + falloff baked in. Armored targets resist heat/corrosion. ✅ free.
- **Attacker FirepowerMultiplier + target DamageMultiplier: track the FINAL damage dealt** — compute
  the state amount from the value actually inflicted (post-`InflictDamage`), so a buffed attacker
  overheats faster and a resistant target heats slower. ⚠ exact hook to be confirmed against engine
  source when building (do not guess the pipeline).

## 4. Family wiring (Scale% per family, on main + _Percentage)

| Family | Temperature | Corrosion | Notes |
|---|--:|--:|---|
| Flame (L/M/H) | **+100** | – | pure heat; + GroundFire linger |
| Laser (Heavy) | **+75** | – | overheat→explode; main-damage only |
| Cryo (L/M/H) | **−100** | – | NEW family (generated like Flame); freeze |
| Chemical (L/M/H) | – | **+100** | pure corrosion |
| **Plasma (L/M/H)** | **+50** | **+50** | NEW flagship (Flame×Chem blend Versus); half heat, half acid |
| Tesla | – | – | keeps its own EMP status (separate) |
| all others (Bullet/Cannon/Missile/Flak/Arrow/Demolition/Concussion/Sonic/Railgun/Prism/Magic/Melee) | – | – | clean baseline unless a §5 effect is added |

## 5. Other physical/status effects (menu — pick which to build)

Existing: **EMP** (Tesla), **Heat/Cryo** (Temperature), **Corrosion** (Chemical), **SonicMark**
(Sonic = slow + +damage, on hit, short duration — rename `CommandoDebuff → SonicDebuff`).

Proposed additions:

| Effect | Weapon | Kind | Does | Unique because |
|---|---|---|---|---|
| **Armor Breach** | Railgun, CannonAP | meter (stacks) | each kinetic hit shreds armor → target takes progressively more | rewards focused fire on heavies |
| **Suppression** | Concussion | meter | −firepower, forced prone, brief pin at max | anti-infantry crowd control |
| **Radiation** | Nuclear | field/meter | lingering DoT that **blocks repair/heal** + can spread | distinct from corrosion (no-heal/spread) |
| **Knockback** | Demolition | instant impulse | pushes units from blast centre | tactical scatter |
| **Hex** | Magic | duration | −firepower / disables special abilities | anti-special niche |

Recommended first: **Armor Breach** + **Suppression** (highest gameplay value). Bullet/Arrow/Flak/
MissileAA stay clean.

## 6. Build plan (after sign-off)
1. C#: add `PhysicalStateName`/`PhysicalStateScale` to `AreaDamageWarhead` + subclass; apply post-damage.
2. C#/traits: **Corrosion** physical-state + receiver (mirror `^CryoFreezable`); level-scaled DoT/slow/
   vuln (50→100%); decay. **Temperature hot-threshold** reaction (overheat). HUD bars (red/blue/green).
3. yaml: generate `^Warhead_Cryo_*` + `^Warhead_Plasma_*`; add `PhysicalStateName/Scale` to family
   templates per §4; `CommandoDebuff → SonicDebuff` global rename + bake into `^Warhead_Sonic_*`.
4. Build C# → `engine/bin` (+ copy to tracked `mods/cameo` dll), boot-gate per batch.

## 7. OPEN QUESTIONS — maintainer, please decide (blocks the C#)
1. **Overheat reaction:** at the hot threshold, does the target (a) **explode/die**, (b) take a **big
   damage burst**, or (c) **burn (DoT)**? (Cryo cold side = freeze/immobilise vs just slow?)
2. **Decay:** do meters cool/fade over time? Rough rate (e.g. lose X per second when not hit)?
3. **Thresholds:** overheat/freeze/max-corrosion trigger at what level — a fixed absolute, or a %
   of the target's max-HP-equivalent (so big units take more to overheat)?
4. **Which §5 effects** to build now (Armor Breach / Suppression / Radiation / Knockback / Hex)?
5. **Corrosion ramp:** confirm 50%→100% linear ramp of DoT + slow + vuln; peak values?
