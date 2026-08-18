# Physical-State System — damage-scaled status meters (design spec, rev. 2026-08-09)

Status: **The framework + the entire Temperature axis ALREADY EXIST and are wired.** This rev
corrects the first draft, which wrongly implied a from-scratch C# build. The real remaining work is
small (one C# field + yaml config). Verify against the code before building — "don't trust, verify".

Companion: `AREADAMAGE_WARHEAD_REBALANCE.md`, `SPREAD_FALLOFF_PLAN.md`; memory `cameo-weapon-differentiation`.

---

## ⛔ THE METER NEVER FILLS IN TIME — measured 2026-08-18, and it blocks E2's pricing

**Maintainer:** *"Cryo seems as strong as Fire IF it is able to completely freeze a unit BEFORE it
dies, and so goes also the saying about Flame and Corrosion — they can reach their full effect
before a unit dies at around 25% HP left, right? You need to make sure this works!"*

**It did not work.** Of the fired weapons carrying a meter, **1** reached full effect while the
target still had 25% HP. One — `SheridanMissilesCryo`, which carries `Amount: 48000`.

## ✅ FIXED — full strength is now 300 (maintainer, 2026-08-18)

*"just make it 300 for a round nice number"* — 300 gives `200/300 = 0.667`, so full effect lands
with **~33% HP** left, inside the 25% bar with margin. Applied in `gen_weapon_template.py`
(`FAMILY_PHYSICAL_STATE` and every blend, ×3 so the per-parent-average rule stays exact) and
spliced into the 94 templates.

```
                       before          after
weapons clearing the bar     1 / 367      124 / 372   (33%)
Temperature scaled   median ratio 2.00     0.67   ✅
Corrosion   scaled   median ratio 2.00     0.67   ✅
Temperature apply    median ratio 59.6     59.6   ⛔ untouched
```

⛔ **The 166 discrete `ApplyPhysicalState` weapons are NOT fixed by this**, because they carry an
absolute `Amount`, not a scale. They need `Amount ≥ 2.67 × damage per shot` each, or conversion to
the damage-scaled mechanism — the second is one rule instead of 166 numbers, and it auto-tracks
post-armor damage the way `PhysicalStateScale` already does. That is the next decision.

⚠ **Blends deliberately do NOT all clear the bar.** A Plasma is half thermal and delivers
`Temperature 150` → ratio 1.33; Waveforce 105 → 1.90; Quantum 75 → 2.67. That is the honest
reading of a blend, and it means **E2's 1.25× must follow DELIVERY, not the family name** — price
the weapons that actually reach full effect, and price a partial meter partially.

### Why — and ⭐ the target's HP cancels out entirely

Live config (`defaults.yaml`, on 1592 actors): `MaxValue: 20000`, `RelativeToHealth: true` — so an
incoming change lands as `Amount × 10000 / HP` — with relaxation 5–10 linear + 50–100 health-scaled
per tick after a 25-tick delay.

```
discrete `ApplyPhysicalState`      hits_to_fill = MaxValue × HP / (Amount × 10000)
                                   hits_to_kill = HP / damage
                                   ratio        = 2 × damage / Amount

damage-scaled `PhysicalStateScale` ratio        = MaxValue / (scale × 100) = 200 / scale
```

Both ratios are **independent of the target's HP**, which is why this is a structural property of
the constants rather than a per-unit balance issue. The bar is `ratio ≤ 0.75`. Measured:

| mechanism | n | median ratio | best | reading |
|---|--:|--:|--:|---|
| Temperature, damage-scaled | 168 | **2.00** | 2.00 | every flamethrower fills its meter exactly **twice as slowly as it kills** |
| Corrosion, damage-scaled | 33 | **2.00** | 2.00 | same, by construction |
| Temperature, discrete apply | 166 | **59.6** | 0.67 | `Amount: 1200` against damage in the tens of thousands |

⚠ **`scale: 100` pins the ratio at exactly 2.0 no matter what the weapon does** — damage cancels
too. So a flame unit reliably kills its target with the meter around half full, and the "full
effect" the pricing would pay for is never delivered. The relaxation makes the real figure *worse*
than these numbers, which ignore decay between shots.

### What makes it work — one constant, three ways to spend it

| fix | value | note |
|---|---|---|
| raise `PhysicalStateScale` | 100 → **267** | the meter then fills at 75% of the way to death |
| lower `MaxValue` | 20000 → **7500** | same effect, one number, applies to every axis at once |
| raise discrete `Amount` | ≥ **2.67 × damage per shot** | per weapon, so only for the `ApplyPhysicalState` set |

⛔ **E2's 1.25× cost multiplier cannot be applied before one of these lands** — it would charge for
an effect the unit does not actually deliver. Fix the constant, re-measure this table, then price.
Claim: `meters_filling_before_death`.

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
| **Sonic** | 🔵 **looped, transparently-shifting blue** overlay (the sonic-mark visual) | — (on-hit, short duration) | **NEW art needed** — a looped shifting-blue overlay. PLACEHOLDER live now: `^SonicDebuff` uses a flat `WithColoredOverlay@SONICDEBUFF` (`0088FF40`, Multiply) — swap it for the looped overlay when the art lands. The commented-out `WithDecoration@SONICDEBUFF` in `^SonicDebuff` still points at the existing `2100commandodebuff` icon. |
| **Armor Breach** | very light **grey** scaling overlay | **breach icon** at 100% — a bullet punching through armor plating (when they take 200%) | **NEW art needed** — the breach icon; overlay is just grey colour |

**New sprite art to create** (RGBA PngSheet per memory `cameo-custom-effects-pngsheet`; pair every new
effect with a sound): the **looped shifting-blue Sonic** overlay, and the **armor-breach breach-icon**
(bullet-through-plating) for the 100% state. Corrosion's pulse already exists (play it at max) and its
green tint is just a colour trait. The traits (`WithPhysicalStateColoredOverlay` / `WithIdleOverlay`)
reference an image+sequence, so the yaml wires with placeholders and the art drops in later.

## 5. Build STATUS + RESUME TRACKER (updated 2026-08-11 — ⭐ RESUME HERE after /compact)

> ⭐ **The physical-state work is now items W6–W10 in
> [`BALANCE_PROGRAM_PLAN.md`](BALANCE_PROGRAM_PLAN.md)**, which holds their status,
> ownership and acceptance criteria. Read §0 of that file first. Summary of the newly
> approved conversions (maintainer 2026-08-11):
>
> - **W6** — new C# `ModifiesCombatProportionalToPhysicalState` (signed from→to for
>   reload/range/speed/firepower, **with audio-pitch and glow hooks folded in**). This is
>   the framework's missing half: every existing proportional trait only makes things
>   worse, so a spin-**up** is impossible without it. Blocks W8 and W10.
> - **W7** — **Sonic → `Resonance` meter**, no new C# needed. The rule that keeps it
>   distinct from Corrosion: Resonance **deals no damage at all, ever** (pure force
>   multiplier, fast decay) while Corrosion is attrition (DoT, slow decay). Sonic becomes
>   the only debuff that kills nothing and doubles the army's output.
> - **W8** — gatling ladder → `SpinUp` meter. **47 actors × 20–30 multiplier traits ≈ 1340
>   objects, ~40% of all 3197 multiplier instances in the mod**, in ten 5% steps.
>   End-points to reproduce: `0.95¹⁰ = 0.599` reload, `1.02¹⁰ = 1.219` range/speed.
> - **W9** — **Poison meter**: a Corrosion clone for infantry (corrosion eats vehicles,
>   poison hurts infantry, flame does both). Gas clouds fill the meter by dwell time and
>   the DoT scales off it — dose-response, no new C#.
> - **W10** — **Blind meter**: range scales 100%→20% proportionally; at FULL blind only,
>   the weapon is disabled, the icon shows, and the `blinded` Targetable applies so
>   blinders retarget.
>
> **Keep binary:** `^Berserkable` (chaos gas) — it flips a *mode* (who you obey), not a
> magnitude. Rule of thumb: meter a *magnitude*, keep a *mode* binary.

**DONE (committed work was boot-gated unless noted):**
- ✅ C# damage-scaled `PhysicalStateName`/`PhysicalStateScale` on `AreaDamage` + `_Percentage` — `406261128`
- ✅ C# MULTI-state `PhysicalStates` dict (one warhead → many meters) — `2e6d6968a`
- ✅ Corrosion meter axis on `^Corrodible` (green tint 200→20000, DoT+slow+vuln 50→100%, hazmat-half,
  `nax_corr_frag` pulse at max) — additive, INERT until fed — `ecf616978`
- ✅ Family wiring LIVE: Flame +100 / Laser +75 / Chemical +100 (generator `FAMILY_PHYSICAL_STATE`) — `51148be5b`
- ✅ Cryo family = `^Warhead_Cryo_*` inherits `^Warhead_Prism_*` + Temperature −100 (generator
  `INHERIT_FAMILIES`) — `f97a3b77c`
- ✅ Plasma family = avg(Flame,Chemical) Versus + Temperature 50 + Corrosion 50 (generator `BLEND_FAMILIES`
  + `versus_override`/`physical_states`) — `2e6d6968a`. Inert until a weapon adopts `^Warhead_Plasma_*`.
- ✅ Flame/Chemical `_Percentage` twins use `AreaDamagePercentage` and feed the matching meter; active
  fixed `ApplyPhysicalState` duplicates were removed, with `audit_physical_state_warheads` preventing
  regressions (static-audited; runtime test pending).
- ✅ **BUILD 3 — Sonic mark** — global rename `CommandoDebuff → SonicDebuff` (29 lines / 8 yaml files:
  `^SonicDebuff` in defaults.yaml + the condition, `Warhead@` keys and both `Inherits@`; the
  `2100commandodebuff` **asset**, its sequences and its palette keep their own names) + the mark BAKED
  into all three `^Warhead_Sonic_*` levels by the generator (`FAMILY_CONDITION` → a
  `Warhead@<tag>_Debuff: GrantExternalCondition`, both numbers DERIVED: `Duration = 2 × ReloadDelay` = 50
  ticks, `Range = 2 × Spread` = 800/1200/1600 = the half-damage radius). Zero damage → price-neutral,
  drift stays 1. The predator laser / waveforce / IonPulse keep their own hand-tuned grants, now of the
  renamed condition. `5a14355e6`
- (Temperature axis + framework were ALREADY built — see §0.)

**TODO — resume queue (in order):**
1. **ADOPT the Sonic family** (needs a maintainer warhead order — rule 4): nothing inherits
   `^Warhead_Sonic_*` yet, so the baked mark is inert. Candidates are the TS GDI sonic weapons
   (`TSSonicZapWeapon`/`…Sonic` = the Disruptor, currently Tesla+Magic; `TSVulcanGunSonic`,
   `TSAssaultCannonSonic`, `TSHellfireSonic`, `TSBombSonic`, `TSGrenadeSonic` = sonic UPGRADE variants
   still on the legacy `^SmallArms`/`^Chaingun`/`^TeslaWeapon`/`^MagicWeapon` inline templates) and RA2
   `SonicZap`. Same shape as the cryo retrofit (§3b): the upgrade ADDS `^Warhead_Sonic_*` as a second
   warhead rather than replacing the base one — never drop a damage TYPE.
2. **BUILD 4 — new axes:** Armor Breach (new PhysicalState axis on defaults, mirror Temperature +
   `DamageMultiplierProportional`, AP/Railgun feed it; grey overlay + breach-icon @100%); Hex (Magic →
   −firepower/inaccuracy condition); Knockback = new C# `PushWarhead` in `OpenRA.Mods.Cameo/Warheads/`.
3. **PIPELINE — cryo 75% tax:** wire the "cryo weapon = 0.75× effective damage" invariant (§3b) into
   `extract_stats`/`fit_class`/`apply_balance` (a cryo tag → auto FirepowerMultiplier).
4. **RETROFIT cryo-upgrade weapons** (RA1 Allies etc.): armament-swap to a variant that adds `^Warhead_Cryo_*`
   (dual/triple, §3b) + the FP tax; replace the old manual `ApplyPhysicalState` rings.
5. **MIGRATE** the Schwarzer Mond binary `corroded` → the Corrosion meter (Chemical/Plasma weapons feed
   it via the wired templates). Verify overheat/corrosion DoT credits `firedBy` for XP
   (`ChangesHealthProportionalToPhysicalState`).
6. **ART** (artist): looped shifting-blue Sonic overlay + armor-breach breach-icon (PngSheet, §4b).
7. Then the broader roadmap: faction damage-type binding, temporal signatures, spread/falloff per-type
   curves, Railgun charge-delay, resume Phase B mixed-weapon collapse (~350, behavior-preserving).

**Guardrails to keep:** boot-gate every commit (kill lingering OpenRA before a C# rebuild — it locks
`engine/bin`); `verify_generator_sync` drift stays **1** (pre-existing `^Warhead_Sniper_Light`, not the
generator's); scoped `git add`; family PhysicalState goes on the main and percentage warheads (chip excluded).

## 6. Decisions (maintainer 2026-08-09) + what's still open
DECIDED:
1. **Corrosion peak** = DoT + slow + vuln (values in §2). Hazmat halves the DoT.
2. **Cryo = a thin child of Prism** — `^Warhead_Cryo_*` inherits `^Warhead_Prism_*` and only adds Temperature −100; base Prism (Prism Tank / Athena Cannon) stays freeze-free. Prism anti-LIGHT Versus already locked.
3. **New axes to build:** Armor Breach + Hex + Knockback (new C# `PushWarhead`) + the base wiring (Corrosion/Prism-cryo/Plasma/Sonic).
4. **Sonic** = global `CommandoDebuff → SonicDebuff`, baked into `^Warhead_Sonic_*` (predator laser + waveforce keep applying it). **BUILT `5a14355e6`** — the family templates now grant the mark themselves; the three hand-tuned grants (GDI predator laser 22/222, Japan waveforce 222/666 + 150/1500, RA2 `IonPulseDischarge`'s 4 expanding rings) were only renamed, not folded, because converting their warheads is a separate permission-gated change.
5. **Every axis needs its own art** (§4b) — green pulsating corrosion overlay + armor-breach breach-icon are NEW assets.

6. **Plasma Versus** = the **per-armor blend (average) of the Flame and Chemical ladders** (maintainer:
   "as close as possible to the flame + chemical combo"). The generator computes it from the two
   families — no hand-authoring. Plasma then applies +50% heat & +50% corrosion (§3). RESOLVED.

STILL OPEN:
- **XP/kill attribution** of overheat/corrosion DoT — verify `ChangesHealthProportionalToPhysicalState`
  credits the original attacker (`firedBy`), not the trait/self, so flame/acid kills grant XP. (Build.)
- **Sonic + armor-breach art** (§4b) — new PngSheet assets (artist); yaml wires with placeholders.
- **MagicDeath death type** (2026-08-10) — Magic weapons currently use `ElectricityDeath` as a placeholder.
  A dedicated `MagicDeath` death type should be added: wire it to the existing `gendeath.shp` mutation
  animation (Yuri Genetic Mutator's `mutate` explosion sequence) via a new `MagicDeathEffect` weapon
  (like `MutateEffect` but WITHOUT the `SpawnActor` warhead — pure visual only). Implementation:
  (1) add `MagicDeath: 8` to `^TDRAInfantry` `WithDeathAnimation.DeathTypes` in `defaults.yaml`,
  (2) add `FireWarheadsOnDeath@MagicDeath` on `^Infantry` pointing to `MagicDeathEffect`,
  (3) create `MagicDeathEffect` weapon (`Explosions: mutate`, `ExplosionPalette: playerra2`, no SpawnActor),
  (4) replace `ElectricityDeath` with `MagicDeath` on all `^Warhead_Magic_*` and `^Warhead_Storm_*` templates.
  The `gendeath.shp` animation already exists in `mods/cameo/bits/ra2/` and is already registered as the
  `mutate` sequence in `sequences/misc.yaml`. Note: `PsychicDeath` (sequence 5) is reserved for Yuri
  mind-control weapons and should NOT be reused for generic magic.
