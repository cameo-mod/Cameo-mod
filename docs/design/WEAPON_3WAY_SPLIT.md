# WEAPON 3-WAY SPLIT — warhead / projectile / effect layers (2026-08-02)

_The repoint architecture. Maintainer decision 2026-08-02: "do the full split
now." Supersedes the naive "reparent onto warhead-only templates" plan (which
would orphan 392/437 override keys + strip FX). Companion to
`WEAPON_TYPE_SYSTEM.md` (warhead layer) + `ARMOR_SYSTEM.md` (Versus law)._

## The model — up to 4 inherits per weapon

The **2-inherit cap is on WARHEADS**, not total inherits. A weapon composes ONE
of each layer (warhead may be 1–2):

```
SomeWeapon:
    Inherits@wh:   ^Bullet_Light      # 1–2 WARHEAD templates (Versus + damage + % + FF twin)
    Inherits@proj: ^ProjectileBullet_Light   # 1 PROJECTILE template
    Inherits@fx:   ^EffectBullet_Light        # 1 EFFECT template (impact FX + shield + smudge/specials)
    ReloadDelay: 20                     # per-weapon: cadence
    Range: 4608                          # per-weapon: range (beautiful ranges preserved)
    Report: gun8.aud                     # per-weapon: sound
    Warhead@Bullet_Light:                # main-damage override — ON THE 2000-GRID, all mains identical
        Damage: 4000                     # multiple of 2000; twins auto (FF 50%, % = 1-per-2000)
```

**DAMAGE LAW (DESIGN.md §nice-number, do NOT violate):** main `Damage` is ALWAYS a
**multiple of 2000** (`total ÷ N`, all N mains identical), **never off-grid, never
hand-nudged**. Effective damage is fine-tuned ONLY by a single **unconditional
`FirepowerMultiplier` named after the actor, on the unit itself** — never by editing
`Damage`. The retrofit is therefore **purely structural**: it renames the damage key
and PRESERVES the weapon's existing on-grid value verbatim; it invents NO numbers. The
2000-grid re-quantise + per-actor FirepowerMultiplier are the **restat's** job (the
balance pipeline: `extract_stats` → workbook → `apply_balance --confirm`).

- **Layer 1 WARHEAD** = the 55-template library (`gen_weapon_template.py`, committed
  `fa0947ae5`/`956cf1ecb`) — pure Versus/damage/%/FF, FX/projectile-agnostic by design.
  FF twin (AoE) done; ExtraDamage twin (energy) handled per-weapon in the energy retrofit.
- **Layer 2 PROJECTILE** = the `Projectile:` block only.
- **Layer 3 EFFECT** = all the non-damage warheads: CreateEffect impact visuals
  (@Effect/@EffectAir/@EffectWater), @ShieldHit + @ShieldHitEffect, LeaveSmudge craters,
  and specials (@EMPUnit, @GroundFire, @ApplyPhysicalState, @DamagesConcrete).

Why: the OLD central templates (^SmallArms/^Grenade/…) are FULL-STACK and weapons hook
their warheads BY THE OLD KEY NAME + rely on their bundled FX, so a bare reparent breaks
them. The split lets a weapon pick its damage identity independently of look/delivery.

## Layer 2 — PROJECTILE templates (proposed `^Proj*`)

Derived from the 30 old templates' `Projectile:` blocks (catalog 2026-08-02).

| Proposed | Projectile | Signature | From (old templates) |
|---|---|---|---|
| **^ProjBullet** | Bullet | 50CAL, contrail, Speed 2000–4000 | SmallArms, Chaingun |
| **^ProjFlak** | Bullet | 50CAL, contrail, Speed 7500–10000 (fast AA) | HeavyAAWeapon, FlakWeapon |
| **^ProjShell** | Bullet | 120MM, Speed 500, Shadow, arc; Inaccuracy L/M/H = 150/300/450 | TD/Medium/HeavyCannon |
| **^ProjGrenade** | Bullet | Speed 150, LaunchAngle 66, BOMB, Inaccuracy 400 | Grenade |
| **^ProjMissile** | Missile | DRAGON/MISSILE, homing; turn-rate L/M/H = 40/30/20 | Light/Medium/HeavyMissile |
| **^ProjArrow** | Missile | Inaccuracy 0, turn 40, PointDefenseTypes=Missile | ArrowWeapon |
| **^ProjSpray** | Bullet | Speed 3000–3500, short range (flame/chem) | Flame×3, Chemical×3 |
| **^ProjLaser** | LaserZap | Width 100, HitAnim laserfire | LaserWeapon |
| **^ProjLightning** | LightningZap | electric arc | Tesla, TeslaCharged |
| **^ProjRailgun** | Railgun | helix beam | RailgunWeapon |
| **^ProjMelee** | InstantHit | — | SwordWeapon |
| **^ProjToxic** | InstantExplode | AffectsParent | ToxicWeapon |
| **^ProjMagic** | Missile | tailsman image, TrailImage | MagicWeapon |
| **^ProjBomb** | (gravity / none) | dropped, no own projectile | Shrapnel, HeavyBomb, Nuclear |
| **^ProjHealBeam** | LaserZap | SecondaryBeam, heal color | HealingWeapon, RepairWeapon |

**Open granularity choice:** L/M/H variants (Shell Inaccuracy, Missile turn-rate, Bullet
speed) as SEPARATE templates (`^ProjShellL/M/H`) vs ONE base + per-weapon override. Leaning
base + per-weapon override (fewer templates; the varying field is a single line).

## Layer 3 — EFFECT templates (proposed `^Fx*`)

Each bundles impact FX + the near-universal shield/concrete core + family smudge/specials, so
ONE `Inherits@fx:` gives a weapon its whole look. Distinct FX key-sets from the catalog:
`(Effect,ShieldHitEffect)`×9, `(Effect,EffectAir,EffectWater,ShieldHitEffect)`×5, etc.

| Proposed | Impact FX | Smudge | Specials (folded in) | From |
|---|---|---|---|---|
| **^FxBullet** | Effect + EffectWater | — | ShieldHit, Concrete | SmallArms, Chaingun, Flak, AA |
| **^FxCannon** | Effect + EffectAir + EffectWater | crater ×4 | ShieldHit, Concrete | Cannons |
| **^FxMissile** | Effect + EffectAir | crater | ShieldHit, Concrete | Missiles, Arrow |
| **^FxExplosion** | Effect + EffectWater | crater | ShieldHit, Concrete | Grenade, Shrapnel, HeavyBomb |
| **^FxFlame** | Effect | scorch | ShieldHit, **GroundFire, PhysicalState** | Flame×3 |
| **^FxChem** | Effect | — | ShieldHit, Concrete | Chemical×3 |
| **^FxLaser** | Effect + EffectAir | scorch | ShieldHit, Concrete | Laser |
| **^FxTesla** | ShieldHitEffect only | — | ShieldHit, **EMP** | Tesla×2 |
| **^FxRailgun** | Effect | — | ShieldHit, Concrete | Railgun |
| **^FxNuclear** | Effect + ShieldHitEffectNuclear | crater ×3 | ShieldHit, Concrete | Nuclear |
| **^FxMelee** | Effect | — | ShieldHit | Sword |
| **^FxMagic** | Effect | smudge | ShieldHit | Magic |

The **ExtraDamage** twin (Laser/Railgun/Tesla/Magic/Sniper — an OpenToppedDamage/SpreadDamage
+vs-shield chip) is a WARHEAD-layer concern (goes in the warhead template), NOT effect.

## Build plan (incremental, each boot-gated)

1. **[additive, 0-usage]** Generate the `^Proj*` + `^Fx*` libraries into weapons.yaml above the
   divider (same low-risk pattern as the warhead splice). Boot-gate + commit. Nothing inherits
   them yet → no behavior change.
2. **[warhead layer]** Extend `gen_weapon_template.py`: FF twin for AoE families, ExtraDamage
   twin for energy families. Re-splice the warhead library. Boot-gate.
3. **[retrofit — the big batch]** Family-by-family, convert weapons from the old full-stack
   inherit to the 4-inherit model (warhead+proj+fx), rewriting `Warhead@<Old>` override keys to
   the new warhead key. Start with the 437 single-inherit; then the 609 mixed (each collapsed to
   ≤2 warheads — the "kill warhead-mixing" pass). Resolver-diff + boot-gate per family.
4. **[cleanup]** Delete the 30 orphaned old templates; drop their `weapon_classes.yaml` rows.

## The 2-warhead cap + its EXCEPTION allow-list (maintainer 2026-08-02)

The kill-mixing pass reduces every weapon to **≤2 warhead inherits** — EXCEPT a
maintainer-curated allow-list of units kept multi-warhead for uniqueness. Retrofit
must NOT strip these. Known so far (more to be defined; confirm each before keeping):
- **Dune combat tanks** — Ixian combat tank / Koda tank / Ordos combat tank / **any D2k
  combat tank** = **3 cannon warheads** (Cannon Light + Medium + Heavy), their signature
  (vs the single Medium cannon of other medium tanks).
- **Terran Siege Tank** (`SiegeTankSiegeCannon`) + **Warcraft Siege Engine**
  (`SiegeEngineCannon`) = keep ALL AoE warheads + others combined = a unique shared explosion.

Everything else: the 2-cap is strict. Build a concrete allow-list (unit/weapon ids) before
the kill-mixing pass. See memory `cameo-weapon-structure-rules`.

## Retrofit specifics + weapon assignments (maintainer 2026-08-02)

- **Intermediate templates.** Some weapons inherit a per-faction INTERMEDIATE
  (`^RA2Chaingun` → `^Chaingun`, `^RA2SmallArms` → `^SmallArms`), not the base directly.
  The retrofit must repoint these intermediates too (or the concrete weapons under them),
  so the whole chain lands on the new families.
- **Bullet_Heavy → the Pulverizer mecha** (Asian Alliance). Today `AsianPulverizerGatling`
  / `AsianPulverizerMechaGatling` stitch `^HeavyCannon + ^SmallArms + ^RA2Chaingun`
  (+ Chem + Missile) — a MIXED weapon → **Phase B**. Collapse it to the clean heavy-bullet
  identity `^Bullet_Heavy` (a heavy gatling). Bullet_Heavy has no old-template source, so
  the Pulverizer is its intended first home.
- **Energy families** (Laser/Railgun/Tesla/Magic) = **small spread → single target**, and
  their upside is the **ExtraDamage chip vs shields** (50% of main, excluded from total).
  Large-AoE weapons have big spread and NO such chip — that is the intended trade.

## SPREAD REBALANCE — a future balance task (maintainer 2026-08-02, reason later)

Spreads are currently placeholders (generator: 400/600/800/1000 by level). New law to work
out later: **every weapon's spread must be UNIQUE, but balanced so `Damage × Spread ≈ constant`**
(an inverse trade — high-damage = tight spread, low-damage = wide spread). A weapon with a
**small spread MUST carry a unique extra effect** to justify it (energy's +vs-shield chip is
the model; a plain small-spread weapon is under-powered). Do NOT hand-tune spreads yet — this
is a dedicated pass folded into the restat. Tracked in ROADMAP.

## Naming — AWAITING MAINTAINER (proposals above)

Per the project rule "maintainer names the templates, I propose." Prefixes `^Proj*` / `^Fx*`
and the family names above are my proposals — easily renamed before anything inherits them.
