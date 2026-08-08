# Spread + Damage-Falloff balancing plan (draft 2026-08-08)

Purpose: a principled, per-(damage-type × class) scheme for `Spread` and `Falloff`, accounting for
projectile speed and air-capability. Draft for review — numbers are a starting proposal derived from
the method; the METHOD is the point.

## 1. The engine mechanic (verified in `OpenRA.Mods.Cameo/Warheads/AreaDamageWarhead.cs`)

- `Spread` (WDist) = the distance **between** falloff steps.
- `Falloff = [f0, f1, … f(N-1)]` → range points `effectiveRange[i] = i × Spread` (line 90), i.e.
  `0, S, 2S, … (N-1)S`.
- Damage% at distance `d` = **linear interpolation** between the two bracketing falloff values
  (`GetDamageFalloff` → `int2.Lerp`). Beyond the last point `(N-1)·S`, damage = **0** (line 178).
- **Max damage radius = (N-1) × Spread.** Each extra Falloff value adds ONE Spread to the radius.
- Center (`d=0`) always gets `f0` (normally 100) × the warhead's `Versus`/`Damage`.

Example — `Spread 200, Falloff 100, 50, 25`: 100% at 0 → 75% at 100 → 50% at 200 → 37.5% at 300 →
25% at 400 → **0 past 400**. Radius = 2×200 = 400.

## 1a. "Just a max radius, linear center→edge?" — yes, and it needs NO C# change

The single-knob idea (author a `MaxRadius`; interpolate 100%→0% from center to it) is already a
special case of the current mechanic: **`Spread: <radius>`, `Falloff: 100, 0`**. Range points become
`[0, radius]`; `GetDamageFalloff` lerps `100→0`; damage is a clean linear cone, 0 at the edge. Both
`AreaDamageWarhead` and its subclass `AreaDamagePercentageWarhead` support it unchanged. (A cosmetic
`Radius:` field could be added as sugar for `Spread + Falloff 100,0`, but it's optional — same behavior.)

**What you GAIN:** one intuitive knob (radius); trivial to balance — the area-integrated damage of a
linear cone is exactly `πR²/3` (a lone target still takes 100% at center).

**What you LOSE: the falloff SHAPE.** Pure-linear can only vary *radius*, not *concentration*. Radius
alone still separates precision (small) from swarm (large) — that covers most cases. But two roles
collapse together:
- "**big radius, punchy center**" (Demolition — a hard blast that fades fast), and
- "**big radius, flat zone**" (Flame/Chemical — stays lethal across the whole area)

become the *same curve* at the same radius; you could only tell them apart by DoT + Damage. If that
distinction matters to you (it's the thing that makes a flame *zone* feel different from a demo
*blast*), keep a **tiny shape menu** instead of pure-linear — still no C# change, just which Falloff
array the generator emits:

| shape | Falloff | feel |
|---|---|---|
| Linear | `100, 0` | neutral cone (the default) |
| Punchy | `100, 40, 0` | concentrated center, fast fade (demolition, precision) |
| Flat | `100, 80, 55, 30, 0` | lethal zone across the radius (flame, chemical, sonic swarm) |

**Recommendation:** adopt **radius as the primary knob** for everything (supersedes the ad-hoc
5-step falloffs), with just those **three shapes** as the secondary role selector. Simplest possible
authoring that still lets flame/demo feel distinct. If you truly want ONE knob only, use `100, 0`
everywhere and differentiate flame/demo purely by DoT + radius + Damage.

## 2. Why spread must be traded steeply against damage

The area a shot covers grows with the **square** of the radius: `area ∝ ((N-1)·S)²`. The total
"AoE output" (area-integrated damage) therefore scales ≈ `S² × falloff-shape-weight`. So spread is a
*quadratically* powerful stat — doubling it quadruples the ground covered.

Two calibration laws follow:
- **Single-target value = center damage only** = `Damage × Versus/100`. Spread does nothing to a lone
  target. So anti-armor/precision weapons want **small spread + high Damage**.
- **Swarm value ∝ Damage × area**. To stop AoE weapons from being strictly better, hold
  **`Damage × Spread ≈ constant`** (the maintainer's inverse-trade law) — a *linear* trade that
  deliberately UNDER-credits spread vs its quadratic power, so effective enemy density (never a full
  field) closes the gap. **Corollary: keep spreads bounded** — a runaway spread is worth far more than
  the linear rule charges.

## 3. Design in terms of RADIUS, then back out Spread

Pick the **target radius** (intuitive) and the **falloff shape** (role); then `Spread = radius / (N-1)`.

**Falloff shapes (the curve = the role):**

| shape | Falloff | pts | radius | use |
|---|---|--:|--:|---|
| **Sharp** (concentrated) | `100, 40, 10` | 3 | 2·S | precision / anti-armor / giant-killer |
| **Standard** | `100, 55, 25, 8` | 4 | 3·S | small splash (bullets, light missiles) |
| **Broad** (anti-swarm) | `100, 72, 48, 28, 12` | 5 | 4·S | swarm-clear (CannonHE, Concussion, Sonic) |
| **Lingering** (area-denial) | `100, 85, 70, 55, 40` | 5 | 4·S | fire / chemical (flat + DoT/GroundFire) |
| **Concentrated-Large** | `100, 55, 15` | 3 | 2·S | demolition (one big blast, not a wide fade) |
| **Expanding** | 10-ring, hand-tuned | 10 | huge | Nuclear (already built) |

**Base target radius by AoE role (Medium tier), in map cells (1 cell = 1024):**
Pinpoint ≈ 0.4 cell (400) · Small ≈ 0.7 (700) · Medium ≈ 1.3 (1300) · Large ≈ 2.1 (2200).

## 4. Modifiers (multiply the target radius; Damage then moves inversely per §2)

| modifier | ×radius | rationale |
|---|--:|---|
| **Air-capable** (Bullet, Flak, Missile-*, Laser, Arrow) | **×0.6** | it also hits air — pay for that flexibility with less ground splash |
| **Ground-only** (Cannon, Flame, Chem, Demo, Concussion, Sonic, Magic, Prism, Railgun, Tesla, Melee) | **×1.25** | compensation for no air |
| projectile **instant** (energy beams) | ×0.7 | perfectly reliable, never dodged |
| projectile **fast** (bullets, flak) | ×1.0 | — |
| projectile **medium** (shells, missiles) | ×1.15 | — |
| projectile **slow arc/lob** (artillery, grenade, demo) | ×1.35 | telegraphed & dodgeable → paid back in radius + Damage |

## 5. Per-type proposal (Medium tier; L = ×0.8, H = ×1.25 on the radius)

| type | role | air | speed | shape | radius (M) | **Spread (M)** | notes |
|---|---|:--:|---|---|--:|--:|---|
| CannonAP | pinpoint | – | med | Sharp | 500 | **250** | tank-killer; high Damage |
| **CannonHE** | medium | – | med | Broad | 1600 | **400** | the swarm-clearer (ground bonus) |
| MissileAP | pinpoint | ✓ | med | Sharp | 240 | **120** | anti-armor, hits air |
| **MissileHE** | small | ✓ | med | Standard | 420 | **140** | << CannonHE (air penalty) |
| MissileAA | pinpoint | ✓ | med | Sharp | 300 | **100–200** | dedicated AA (already decided) |
| Bullet | small | ✓ | fast | Standard | 420 | **140** | anti-inf |
| Flak | small | ✓ | fast | Broad | 420 | **105** | flak burst, anti-air+light |
| Arrow | small | ✓ | fast | Standard | 420 | **140** | |
| Laser / Prism / Railgun / Tesla | pinpoint | mix | instant | Sharp | 200–400 | **100** | energy — thin; chip/utility pays (done) |
| TeslaCharged | pinpoint | – | instant | Sharp | 800 | **200** | super tier (done) |
| **Flame** | medium | – | med | Lingering | 1600 | **400** | + GroundFire; area-denial |
| **Chemical** | medium | – | med | Lingering | 1600 | **400** | + cloud/DoT; area-denial |
| **Concussion** | medium | – | med | Broad | 1600 | **400** | frag; anti-inf swarm |
| **Demolition** | large | – | slow | Concentrated-Large | 2200 | **1100** | one big blast (few steps); anti-bldg+inf |
| **Sonic** | medium | – | fast | Broad | 1400 | **350** | anti-swarm flat (wants width) |
| **Magic** | pinpoint | – | med | Sharp | 500 | **250** | giant-killer → single-target |
| Melee | point | – | – | 1-step | ~100 | **50** | |
| **Nuclear** | super | – | – | Expanding | — | (hand-tuned) | 10-ring, done |

## 6. The families you flagged (fire / chem / demolition / concussion / nuclear)

- **Flame & Chemical = area-DENIAL**, not burst: a **Lingering** falloff (stays ~70-85% across the
  radius) + their existing DoT (`GroundFire` / chem cloud). Medium radius, ground-only bonus. They
  should feel like a *zone* you can't stand in, not a single big hit.
- **Concussion (shrapnel) = anti-infantry swarm**: **Broad** falloff, medium-large radius — frag
  spreads wide and thin. Low per-target Damage, wide area (per §2 trade).
- **Demolition = one concentrated blast**: large radius but a **Concentrated-Large** (few-step) shape
  so the damage is punchy near the center and gone quickly, not a soft 4-ring fade. Slow lob → paid
  back in radius + Damage. Anti-building + anti-infantry.
- **Nuclear = expanding rings** (already hand-tuned in `^Warhead_Nuclear_Super`): leave as-is.

## 7. Open questions before implementing
1. **Level scaling of radius** — proposal is ×0.8 / ×1.0 / ×1.25 (L/M/H). Or keep radius by ROLE and
   let per-unit Damage carry the class? (Simpler, but a heavy artillery then has the same blast as a light.)
2. **Exact falloff arrays** — the shapes in §3 are starting curves; the anti-swarm "Broad" tail
   (28,12) and the "Lingering" flatness are tunable.
3. **Damage×Spread≈const enforcement** — should the pipeline auto-derive per-unit Damage from the
   assigned Spread, or stay a manual guardrail?
4. Implementation is generator-driven (`gen_weapon_template.py` per-family `spreads`/`falloffs`
   overrides + `splice_templates.py`), then boot-gate — same path as the energy chips.
