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

## 2. Corrosion axis — CONVERT the existing binary `corroded` into a meter (verified 2026-08-09)

⚠ Corrosion is NOT greenfield: it already exists as a **binary `corroded` condition** (`defaults.yaml`
~5495–5516) applied on/off by the Schwarzer Mond rockets — `DamageMultiplier@corroded 125`,
`SpeedMultiplier@corroded 75`, `WithColoredOverlay@corroded 44FF4444` (green), `WithIdleOverlay@corroded`
(`Image: explosion, Sequence: nax_corr_frag` = the pulse), `Targetable@corroded`. So BUILD 1 is a
CONVERSION, reusing the existing art:
1. Add `PhysicalState@Corrosion` meter (0–20000, `RelativeToHealth`, decay) on `^CryoFreezable` (the
   shared Temperature home) + wherever else Temperature lives.
2. Replace the fixed `@corroded` traits with **scaled** ones from 50%→100% (`ChangesHealthProportionalToPhysicalState`
   DoT ~180 + a `@…Hazmat` half via `hazmatsuits`; `SlowsProportionalToPhysicalState` `OnlyPositiveValues`
   to ~60%; `DamageMultiplierProportionalToPhysicalState` to +150%).
3. Keep the art: `WithPhysicalStateColoredOverlay@Corrosion` green tint 200→20000; `WithIdleOverlay`
   `nax_corr_frag` pulse gated on a `CorrosionMax` condition (meter == 20000).
4. Migrate the Schwarzer Mond weapons from granting the binary `corroded` to feeding the Corrosion meter
   (via the new `PhysicalStateScale`, or an `ApplyPhysicalState` warhead). Keep the old binary path until
   migrated so nothing breaks mid-way.
Boot-gate after each step. (The section below is the original greenfield sketch, superseded by this.)

### (superseded greenfield sketch)

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

## 3b. Effect UPGRADES (cryo etc.) = DUAL warhead (maintainer 2026-08-09)

When an upgrade adds an effect to an EXISTING weapon (e.g. RA1 Allied cryo projectiles upgrading the
demo artillery), replace the old manual `ApplyPhysicalState` rings with the **dual model**: the upgrade
swaps the armament (the JHind/waveforce `RequiresCondition` pattern) to a variant weapon that keeps the
original warhead **and adds the real Cryo warhead** — both at ~50% damage:
```
DemoArtilleryCryo:
    Inherits: DemoArtillery              # keeps Warhead@Demolition_Heavy (its Versus)
    Inherits@cryo: ^Warhead_Cryo_Heavy   # adds the Cryo warhead (anti-LIGHT Versus + Temperature -100)
    Warhead@Demolition_Heavy: { Damage: <50%>, PhysicalStateName: Temperature, PhysicalStateScale: -100 }
    Warhead@Cryo_Heavy:       { Damage: <50%> }   # freeze via the family's baked -100
```
Rationale (maintainer): the point is NOT just to freeze — it's to **change the damage profile** so the
upgraded weapon feels unique (demo blast + cryo's anti-light Versus + freeze). The added Cryo warhead's
Versus is the real value (temperature alone would be redundant). Both warheads carry the −100 scaling so
the whole weapon freezes. → this makes the **Cryo family a prerequisite** (BUILD 2b).

⚠ **Combined weapons stack it further (maintainer 2026-08-09):** artillery is being reworked to
**CannonHE + Demolition** (the slow big-blast combo), so a cryo upgrade makes Cryo the **THIRD** warhead
(CannonHE + Demolition + Cryo). This exceeds the usual 2-warhead cap but is a justified exception (a
combined artillery weapon + an upgrade) — the allow-list case in [[cameo-weapon-structure-rules]].

**Damage model (maintainer 2026-08-09) — symmetric warheads + a FirepowerMultiplier penalty:**
keep all members at the SAME damage (e.g. each 2000) and pay for the freeze by REDUCING net output.
Worked example (artillery): base CannonHE 2000 + Demolition 2000 = **4000**. Add Cryo 2000 → symmetric
**6000 raw** (+50%). Put **`FirepowerMultiplier: 50`** on the cryo-upgraded actor → 6000 × 0.5 = **3000
effective = 75%** of the original 4000. So the cryo upgrade *lowers* damage to 75% — fair, because the
freeze is very strong (immobilise + the cold-side +incoming-damage). Freeze buys the −25% damage.
General rule: N symmetric members × D, then `FirepowerMultiplier` set so the effective total lands where
the freeze/effect is priced (here 75%).

**⚠ PIPELINE INVARIANT (maintainer 2026-08-09): a cryo weapon ALWAYS deals 75% of the damage the SAME
weapon would deal without cryo.** Take the pipeline-balanced value X, then set a `FirepowerMultiplier`
so the cryo weapon's EFFECTIVE damage = 0.75·X — the fixed "cryo tax" that pays for the freeze
(immobilise + cold-side +incoming-damage). The FP *value* is computed from the raw: with a symmetric
added cryo warhead the raw is 1.5·X, so FP = 0.75/1.5 = **50%**; a cryo weapon with no added warhead
would use FP **75%**. This is a balance-pipeline rule to ENFORCE automatically (a cryo tag → the pipeline
applies the 0.75× effective tax), so it can never be forgotten. TODO: wire the cryo tax into
`extract_stats`/`fit_class`/`apply_balance`.

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

## 5. Build STATUS + RESUME TRACKER (updated 2026-08-09 — ⭐ RESUME HERE after /compact)

**DONE (committed, each boot-gated):**
- ✅ C# damage-scaled `PhysicalStateName`/`PhysicalStateScale` on `AreaDamage` + `_Percentage` — `406261128`
- ✅ C# MULTI-state `PhysicalStates` dict (one warhead → many meters) — `2e6d6968a`
- ✅ Corrosion meter axis on `^Corrodible` (green tint 200→20000, DoT+slow+vuln 50→100%, hazmat-half,
  `nax_corr_frag` pulse at max) — additive, INERT until fed — `ecf616978`
- ✅ Family wiring LIVE: Flame +100 / Laser +75 / Chemical +100 (generator `FAMILY_PHYSICAL_STATE`) — `51148be5b`
- ✅ Cryo family = `^Warhead_Cryo_*` inherits `^Warhead_Prism_*` + Temperature −100 (generator
  `INHERIT_FAMILIES`) — `f97a3b77c`
- ✅ Plasma family = avg(Flame,Chemical) Versus + Temperature 50 + Corrosion 50 (generator `BLEND_FAMILIES`
  + `versus_override`/`physical_states`) — `2e6d6968a`. Inert until a weapon adopts `^Warhead_Plasma_*`.
- (Temperature axis + framework were ALREADY built — see §0.)

**TODO — resume queue (in order):**
1. **BUILD 3 — Sonic:** global rename `CommandoDebuff → SonicDebuff` (defaults.yaml `^CommandoDebuff` +
   every grant/require: GDI predator blue laser, Japan waveforce, commandos) + bake a short-duration
   `GrantExternalCondition` into `^Warhead_Sonic_*` (all levels) so every sonic hit applies it; fold
   Japan `^WaveforceBulletWarhead` weapons + the predator into Sonic. boot-gate.
2. **BUILD 4 — new axes:** Armor Breach (new PhysicalState axis on defaults, mirror Temperature +
   `DamageMultiplierProportional`, AP/Railgun feed it; grey overlay + breach-icon @100%); Hex (Magic →
   −firepower/inaccuracy condition); Knockback = new C# `PushWarhead` in `OpenRA.Mods.Cameo/Warheads/`.
3. **PIPELINE — cryo 75% tax:** wire the "cryo weapon = 0.75× effective damage" invariant (§3b) into
   `extract_stats`/`fit_class`/`apply_balance` (a cryo tag → auto FirepowerMultiplier).
4. **RETROFIT cryo-upgrade weapons** (RA1 Allies etc.): armament-swap to a variant that adds `^Warhead_Cryo_*`
   (dual/triple, §3b) + the FP tax; replace the old manual `ApplyPhysicalState` rings.
5. **MIGRATE** the Schwarzer Mond binary `corroded` → the Corrosion meter (Chemical/Plasma weapons feed
   it via the wired templates). Convert generated `_Percentage` twins → `AreaDamagePercentage` so the
   %-damage also feeds the meters (currently main-only). Verify overheat/corrosion DoT credits `firedBy`
   for XP (`ChangesHealthProportionalToPhysicalState`).
6. **ART** (artist): looped shifting-blue Sonic overlay + armor-breach breach-icon (PngSheet, §4b).
7. Then the broader roadmap: faction damage-type binding, temporal signatures, spread/falloff per-type
   curves, Railgun charge-delay, resume Phase B mixed-weapon collapse (~350, behavior-preserving).

**Guardrails to keep:** boot-gate every commit (kill lingering OpenRA before a C# rebuild — it locks
`engine/bin`); `verify_generator_sync` drift stays **1** (pre-existing `^Warhead_Sniper_Light`, not the
generator's); scoped `git add`; the family PhysicalState goes on the MAIN warhead only (chip excluded).

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
