# Physical-State System — damage-scaled status meters (design spec, rev. 2026-08-09)

Status: **The framework + the entire Temperature axis ALREADY EXIST and are wired.** This rev
corrects the first draft, which wrongly implied a from-scratch C# build. The real remaining work is
small (one C# field + yaml config). Verify against the code before building — "don't trust, verify".

Companion: `AREADAMAGE_WARHEAD_REBALANCE.md`, `SPREAD_FALLOFF_PLAN.md`; memory `cameo-weapon-differentiation`.

## 0. WHAT ALREADY EXISTS (verified 2026-08-09)

**Engine traits** (`engine/OpenRA.Mods.Common/Traits/`): `PhysicalState`, `PhysicalStateBar`,
`PhysicalStateAura`, `DamageMultiplierProportionalToPhysicalState`, `SlowsProportionalToPhysicalState`,
`ChangesHealthProportionalToPhysicalState`, `ChangesHealthProportionalToPhysicalState`,
`GrantConditionOnPhysicalState`, `ChangeOwnerOnPhysicalState`, `ProvidesShieldFromPhysicalState`,
`PhysicalStateShieldManager`; Warhead `ApplyPhysicalStateWarhead`. Cameo adds `ChangesPhysicalState`,
`WithPhysicalStateColoredOverlay`. **This is a full, generic meter framework — no new trait C# needed
to add more axes.**

**The `Temperature` axis is FULLY BUILT** (`mods/cameo/rules/defaults.yaml`, on the infantry/vehicle
defaults). Verbatim behaviour — do NOT redesign it, only extend/tune:
- `PhysicalState@Temperature`: `MaxValue 20000 / MinValue -20000 / RelaxedValue 0`,
  **`RelativeToHealth: true`** (HP-scaled — big units need more), **`RelaxationLinear 5 + RelaxationScaled 50`
  + `RelaxationDelay 25`** (cools/warms back to 0 after 25 ticks), `ApplyDamageModifiers: true`.
- **Bar** `PhysicalStateBar@TemperatureBar`: 🔴 `FF0000` hot / 🔵 `0080FF` cold / grey neutral.
- **Hot side:** `GrantConditionOnPhysicalState@Overheating` (Temp **200→20000**) → `ChangesHealthProportionalTo…@Overheating`
  flat **150/tick FireDeath** (the damage burst); `@superhot` (Temp **==20000**, max) → **% damage** (the
  "keep heating → it pops" tier). Both already scale off the meter; the killing damage is credited to the
  attacker via the warhead's `firedBy` (verify XP attribution when wiring).
- **Cold side:** `@FrostSlowdown` (SlowsProportional) ramps Speed/Turn/Turret→0 and Reload→1 (freezes solid);
  `@CryoFreeze` (Temp **-200→-20000**) → **DamageMultiplier 100→200%** (takes up to 2× while frozen = the
  "shatter"); `@superfreeze` (Temp **==-20000**, max) → `frostspark` overlay (+ the aircraft-instakill hook —
  verify the 10× aircraft / 200% ground split when wiring); blue `WithPhysicalStateColoredOverlay@CryoFreeze`.
- **`EnemyProximity`** is a SECOND, non-HP axis (`PhysicalStateAura`) driving Tesla-discharge armor — proof
  the framework already carries multiple axes and that "scales with HP or not" is per-axis (`RelativeToHealth`).

**So for Temperature there is essentially nothing to build** — the meter, HP-scaling, decay, two hot
thresholds (burst→pop), two cold thresholds (freeze→shatter/superfreeze), bar and overlays all exist.

## 1. The ONE genuine C# gap — damage-scaled application

`ApplyPhysicalStateWarhead` applies a **fixed `Amount`** (× firepower modifiers), NOT the damage value —
so today you hand-tune `Amount` per weapon (e.g. Cryocopter `Amount: -16000`). The maintainer wants the
state to **scale with the weapon's actual damage**. The clean fix (small, additive):

Add to `OpenRA.Mods.Cameo/Warheads/AreaDamageWarhead.cs` (+ its `AreaDamagePercentage` subclass):
```
PhysicalStateName: Temperature    # default "" = off
PhysicalStateScale: -100          # state added = effective damage × Scale/100 (signed)
```
On impact, after computing per-target damage (post-falloff, post-Versus/armor) and calling
`InflictDamage`, also call the target's `PhysicalState.ApplyChange(damage × Scale/100, firedBy, applyMods)`.
- **On main + `_Percentage` warheads only, never `_ExtraDamage`** → chip auto-excluded; %-twin also feeds
  the meter (both maintainer rules satisfied by placement).
- Armor + falloff scale for free (it's a fraction of the computed damage). For attacker-firepower /
  target-damage mods, reuse `ApplyDamageModifiers`/`args.DamageModifiers` exactly as `ApplyPhysicalStateWarhead`
  does. Verify the hook against that warhead's `DoImpact` so behaviour matches the existing fixed-Amount path.

That single field removes the need for separate per-weapon `ApplyPhysicalState` warheads and makes heat/
cold/corrosion auto-scale with damage. **Everything else below is YAML config of existing traits.**

## 2. Corrosion axis — ADD in yaml (mirror Temperature, no new C#)

Add a `PhysicalState@Corrosion` block to the same defaults (unipolar): `MinValue 0 / MaxValue 20000 /
RelaxedValue 0`, `RelativeToHealth: true`, relaxation like Temperature. Bar 🟢 green (`PositiveColor 00C000`).
Reactions via the SAME proportional traits, gated on threshold conditions from `GrantConditionOnPhysicalState`:
- from 50%→100% ramp (maintainer 2026-08-09 — DoT + slow + vuln, the rounded "acid melts you"):
  `ChangesHealthProportionalToPhysicalState` DoT up to **~180/tick, short/sharp**;
  `SlowsProportionalToPhysicalState` down to **~60% speed**;
  `DamageMultiplierProportionalToPhysicalState` up to **+150% damage taken**.
- This IS the Schwarzer Mond "corruption" effect (Korruptes Biest / Rocket Soldier) turned into a meter.
- **Hazmat/reactive-armor reduction (maintainer 2026-08-09):** mirror the existing Temperature pattern —
  `@Overheating` (DoT 100–150) has a reduced `@OverheatingHazmat` variant (DoT 25) gated on `hazmatsuits`.
  Add the same for Corrosion: a `@CorrosionHazmat` variant at **half** the DoT gated on `hazmatsuits`,
  and/or a corrosion-resistant armor type that halves it — exactly as hazmat/reactive armor halves heat.
Then wire Chemical/Plasma to it via §1's field.

## 3. Family wiring (via §1 field, on main + _Percentage)

| Family | State | Scale% | Notes |
|---|---|--:|---|
| Flame (L/M/H) | Temperature | **+100** | + GroundFire linger |
| Laser (Heavy) | Temperature | **+75** | overheat→pop; main-damage only (chip excluded by placement) |
| **Prism (L/M/H)** | – | – | anti-LIGHT scatter beam, Versus LOCKED (`Scout 100 › None 94 › … › Superheavy 34`), ground-only, thin spread, no chip. **NO cryo by default** — Prism Tank / Athena Cannon are pure prism. |
| **Cryo (L/M/H)** — NEW | Temperature | **−100** | **inherits `^Warhead_Prism_<Level>`** and ONLY adds the cold scaling = "a prism beam that also freezes". Reuses Prism's anti-light Versus + scatter + thin spread; freeze/shatter uses the existing cold side. (So there IS a Cryo family, but it's a thin Prism child, not a from-scratch family.) |
| Chemical (L/M/H) | Corrosion | **+100** | pure corrosion |
| **Plasma (L/M/H)** — NEW | Temperature **+50** & Corrosion **+50** | | flagship Flame×Chem blend Versus |
| Tesla | (EMP, separate) | – | keeps existing EMP |
| Bullet/Cannon/Missile/Flak/Arrow/Demolition/Sonic/Prism/Magic/Melee | – | – | clean unless a §4 effect added |

## 4. Other effects — what exists vs what's new
- **Suppression:** partial already (infantry go prone for less damage). A Concussion suppression METER
  (−firepower/pin) would be a new Corrosion-style axis (yaml) — decide if worth it over existing prone.
- **Radiation:** already exists as flavor on some weapons (RA2 Soviets). Keep as-is / extend per faction.
- **Armor Breach (Sunder)** — AP/Railgun: a NEW physical-state axis (yaml, mirror Temperature) with
  `DamageMultiplierProportionalToPhysicalState` so stacked kinetic hits make the target take more. **No new
  C#.** High value — makes tank-destroyers "soften" targets → combined-arms incentive (maintainer likes this).
- **Hex** — Magic: −firepower / −accuracy (inaccuracy) / disable specials, as a granted condition (yaml).
  Maintainer likes it; tune the magnitude (~ −50% firepower or heavy inaccuracy).
- **Knockback** — Demolition: **NO engine support found** → needs a NEW C# warhead (push actors from
  impact center). Feasible but a real addition; maintainer said "do it if possible" → build a
  `PushWarhead` in `OpenRA.Mods.Cameo/Warheads/`.

## 4b. Visuals — every axis needs its OWN artwork (maintainer 2026-08-09)

Each state needs (a) a **level-scaling coloured overlay** (`WithPhysicalStateColoredOverlay`, already
used for Temperature's blue cold side) AND (b) **threshold artwork** at the extreme
(`WithIdleOverlay`, like `@frostspark` on `superfreeze`). Not just a tint.

| Axis | Scaling overlay | Threshold artwork | Asset status |
|---|---|---|---|
| Temperature hot | 🔴 red (bar exists) | overheat glow at max | red overlay exists; max-heat art TBD |
| Temperature cold | 🔵 blue (`@CryoFreeze` overlay) | ❄ `frostspark` at `superfreeze` | **exists** |
| **Corrosion** | 🟢 **green tint**, ever-increasing 200→20000 (`WithPhysicalStateColoredOverlay`, colour only) | the **existing pulsating corrosion effect**, played ONLY at 100% (20000) | **mostly EXISTS** — pulse effect exists; green tint is just the colour trait |
| **Sonic** | 🔵 **looped, transparently-shifting blue** overlay (the sonic-mark visual) | — (on-hit, short duration) | **NEW art needed** — a looped shifting-blue overlay |
| **Armor Breach** | very light **grey** scaling overlay | **breach icon** at 100% — a bullet punching through armor plating (when they take 200%) | **NEW art needed** — the breach icon; overlay is just grey colour |

**New sprite art to create** (RGBA PngSheet per memory `cameo-custom-effects-pngsheet`; pair every new
effect with a sound): the **looped shifting-blue Sonic** overlay, and the **armor-breach breach-icon**
(bullet-through-plating) for the 100% state. Corrosion's pulse already exists (play it at max) and its
green tint is just a colour trait. The traits (`WithPhysicalStateColoredOverlay` / `WithIdleOverlay`)
reference an image+sequence, so the yaml wires with placeholders and the art drops in later.

## 5. Build order (after sign-off)
1. C#: `PhysicalStateName`/`PhysicalStateScale` on `AreaDamageWarhead` (+subclass). Build → `engine/bin`
   (+ copy tracked dll), boot-gate.
2. yaml: add `Corrosion` axis + (optional) `ArmorBreach` axis to defaults (mirror Temperature). boot-gate.
3. yaml: generate `^Warhead_Cryo_*` + `^Warhead_Plasma_*`; add the §3 fields to family templates;
   `CommandoDebuff → SonicDebuff` rename + bake into `^Warhead_Sonic_*`. boot-gate per batch.
4. C# (optional): `PushWarhead` for knockback.

## 6. Decisions (maintainer 2026-08-09) + what's still open
DECIDED:
1. **Corrosion peak** = DoT + slow + vuln (values in §2). Hazmat halves the DoT.
2. **Cryo = a thin child of Prism** — `^Warhead_Cryo_*` inherits `^Warhead_Prism_*` and only adds Temperature −100; base Prism (Prism Tank / Athena Cannon) stays freeze-free. Prism anti-LIGHT Versus already locked.
3. **New axes to build:** Armor Breach + Hex + Knockback (new C# `PushWarhead`) + the base wiring (Corrosion/Prism-cryo/Plasma/Sonic).
4. **Sonic** = global `CommandoDebuff → SonicDebuff`, baked into `^Warhead_Sonic_*` (predator laser + waveforce keep applying it).
5. **Every axis needs its own art** (§4b) — green pulsating corrosion overlay + armor-breach breach-icon are NEW assets.

6. **Plasma Versus** = the **per-armor blend (average) of the Flame and Chemical ladders** (maintainer:
   "as close as possible to the flame + chemical combo"). The generator computes it from the two
   families — no hand-authoring. Plasma then applies +50% heat & +50% corrosion (§3). RESOLVED.

STILL OPEN:
- **XP/kill attribution** of overheat/corrosion DoT — verify `ChangesHealthProportionalToPhysicalState`
  credits the original attacker (`firedBy`), not the trait/self, so flame/acid kills grant XP. (Build.)
- **Sonic + armor-breach art** (§4b) — new PngSheet assets (artist); yaml wires with placeholders.
