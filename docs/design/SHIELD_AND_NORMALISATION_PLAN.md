# Versus normalisation + the Shield ladder — analysis and plan (rev. 2026-08-16)

**Status: PLAN. No code written yet.** Maintainer ordered a full analysis before
implementation. This file is the analysis, the options, and the execution order.

---

## 0. What actually broke, measured

`ARMOR_SYSTEM.md:43` sets the law **`Shield = top + floor`**. It was written when every
profile peaked at exactly **100**, so Shield landed at `100 + {10,25,40}` = **110 / 125 /
140** — always just above the ceiling every other armor obeyed. Clean, and it encoded a
real idea: *shields are the softest layer, so the one value allowed past the cap.*

W13 renormalised profiles to **median = 100**. "Top" stopped being a constant and became a
function of each family's **sharpness**. The rule silently changed meaning:

| template | Shield | top (non-Shield) |
|---|--:|--:|
| `^Warhead_Tesla_Heavy` | **151** | 106 |
| `^Warhead_Tesla_Super` | **160** | 114 |
| `^Warhead_Melee_Medium` | **200** | 174 |
| `^Warhead_Flame_Heavy` | **200** | 161 |
| `^Warhead_Bullet_Light` | 199 | 162 |

**The anti-shield identity is inverted.** A sword now out-damages a Tesla coil against an
energy shield, because Melee's profile is sharp and Tesla's is deliberately flat.

Two further causes compounded it:

1. **The carrier was deleted.** Tesla's 300% / 400% anti-shield lived in a *separate*
   `ExtraDamage` warhead. The universal AreaDamage conversion merged those chips into the
   main warhead — so the identity did not merely dilute, its vehicle was removed.
2. **Two rules contest one cell.** `gen_weapon_template.py` holds the design intent
   (`Tesla: Shield 300`, `Tesla_Super: Shield 400`), but that path only runs for
   hand-designed families. Tesla is *measured*, so `reference_main()` recomputes Shield
   from the corpus profile and the 300/400 is never used.

---

## 1. The maintainer's correction (2026-08-16) — and why it is right

> *"the maximum allowed value is 200 so do 200 + bottom value right? shield is allowed to
> be beyond the 200 limit right? ... basically the shield values just roughly double from
> before."*

Correct, and it is the *same* law, not a new one. The original rule was never "top + floor"
in the sense of "this family's peak" — it was **"the ceiling + floor"**, and under peak-100
normalisation those happened to be the same number. Restoring the intent under the new
window means:

```
Shield = CEILING (200) + floor          # 210 / 225 / 240 …
```

Shield is explicitly exempt from the `[10, 200]` window, exactly as it was exempt from the
old 100 cap. Everything roughly doubles, which is the correct consequence of the window
doubling.

**But `CEILING + floor` alone cannot express anti-shield identity** — it depends only on
the family's floor, so Tesla and a rifle land within ~30 points of each other. Hence the
second half of the maintainer's proposal.

---

## 2. The three inputs — with a measured verdict on each

Maintainer's proposal: derive Shield from three independent sources and average them.
The idea is sound. The weights, however, must follow the data:

### Input 1 — the reference corpus ⚠ **EMPTY FOR THIS CELL**

`docs/reference/versus_raw.json`: **3150 profiles / 16 mods**, 34 distinct armor names.

| armor | rows | mods |
|---|--:|--:|
| `heavy`, `wood`, `none`, `concrete`, `light` | ~2650–2850 each | 16 |
| **`shield`** | **13** | **1** |

**Verdict: the corpus cannot produce a per-family Shield ladder.** 13 rows from one mod
across 32 families × 4 levels is noise. Averaging it in as a third of the answer would
dress up an invention as a measurement — the exact failure mode
[[cameo-versus-reference-corpus]] was built to prevent.

**What the corpus IS for here:** the other 33 armors have thousands of rows, so it fully
supports the *normalisation* work in §3 — which is the prerequisite for input 3 anyway.

### Input 2 — design intent + real-world physics ✅ **carries the weight**

The only input with genuine per-family signal. Framework in §4.

### Input 3 — `CEILING + floor`, after mean-normalisation ✅ **structural**

Always computable, ties Shield to the family's own ladder, and reduces exactly to the
historical rule. Its weakness is that it is identity-blind, which input 2 supplies.

### Recommended weighting

**Shield = mean(input 2, input 3)**, with input 1 used only as a sanity bound where its 13
rows apply. Presented as a 3-way average this would be dishonest arithmetic; presented as
"design × structure, checked against what little evidence exists" it is defensible. If the
maintainer prefers a literal 3-way average, input 1 must first be widened — see §6 Option C.

---

## 3. The prerequisite — normalise every family's Versus MEAN to 100

> *"all warheads average all versus values at 100 to make them comparable"*

This is the most consequential idea in the whole discussion and it should land **first**,
because Shield depends on it and so does everything else.

**Today:** profiles are normalised to **median = 100**. Their *means* differ (Bullet_Light
87, Melee ~74, Concussion ~85), so a family's mean is a hidden magnitude multiplier.

**Consequence of switching to mean = 100:** since `K` is a share-weighted average of the
profile, a family's mean IS its contribution to priced DPS. Pin every mean to 100 and:

- **`K` becomes shape-only.** Choosing a family no longer changes a weapon's total output,
  only *how that output is distributed across armors*.
- **`Damage` becomes the sole magnitude knob** — which is exactly what the balance pipeline
  wants, and it removes the "profile change must be paid for" coupling that made the W23
  retrofit need a `Damage` rescale at all.
- **Families become directly comparable**, which is the maintainer's stated goal.

⚠ **This supersedes the W13 median-normalisation** and re-derives all 88 templates. It is a
one-line change in `aggregate_archetype.py` (`NORMALISE_REFERENCE`) plus a re-run — but it
moves every profile, so it must be paired with a `report_versus_change.py` pass and a
pipeline re-price.

### The class-tilt law (also new)

> *"light weapons have a bigger damage to light armor types while heavy weapons have a
> bigger damage to heavy armor types with medium weapons having bigger damage to medium
> armor types ... the super type should be the generalized type that deals good damage
> against everything."*

With the mean pinned at 100, tilt is free — it costs nothing in total output, it only moves
where the output lands. Formalised:

| level | tilt | shape |
|---|---|---|
| Light | toward light armors (None, Flak, Plate, Scout, Wood) | sharp, front-loaded |
| Medium | toward mid armors (Light, Medium, Steel, Heroic) | sharp, centred |
| Heavy | toward heavy armors (Heavy, Superheavy, Concrete) | sharp, back-loaded |
| **Super** | **flat** — good vs everything | **the generalist; lowest spread** |

This gives the 2×/4×/8× band a *meaning* per level rather than a free parameter, and it
makes Super's identity structural rather than just "bigger numbers".

⚠ **Tension to resolve:** the existing `^Warhead_Nuclear_Super` is deliberately ordered
`BLD > VEH > AIR > INF`, which is a tilt, not a generalist. Either Nuclear is an explicit
exception or the Super law needs softening. **Maintainer decision.**

---

## 4. Input 2 — the physics framework for Shield, per family

An energy shield stops **energy and momentum at a boundary**. What matters is whether the
weapon's mechanism couples to a field or bypasses it. Proposed reasoning, to be reviewed
family by family rather than accepted wholesale:

| mechanism | vs an energy shield | families | rationale |
|---|---|---|---|
| **Direct electrical / EM** | **strongest** | Tesla, Storm | current couples straight into the field; the shield IS the conductor. This is the maintainer's stated law and the anchor of the scale. |
| **Coherent energy** | strong | Quantum, Railgun, Prism, Laser | delivers energy the emitter must absorb; scales with coherence |
| **Blended energy** | above average | Plasma, **Waveforce**, Inferno | part field-coupling, part thermal |
| **Thermal / chemical** | average | Flame, Chemical, Toxic | a shield stops heat and reagents well; little field coupling |
| **Kinetic / explosive** | below average | Bullet, Cannon*, Missile*, Flak, Concussion, Demolition | momentum is what shields are designed for |
| **Physical contact** | **weakest** | Melee, Arrow | a blade is the canonical thing a shield stops |

Note this **exactly inverts today's table** — Melee is currently top and Tesla bottom.

Level scaling within a family: shields are an *energy budget*, so a bigger discharge
depletes more. Suggested `Light < Medium < Heavy < Super`, with Super highest.

⚠ **Hard constraint (maintainer): no two families may share a Shield value.** Enforceable
with the same `MIN_GAP` machinery the armor ladder already uses. With 32 families × 4
levels = 128 distinct values needed, spread across roughly 210–400, gaps land ~1.5 apart —
tight but feasible. **If it proves too tight, widen the Shield range rather than allow ties.**

---

## 5. Execution order

Each step has a VERIFY and is independently boot-gateable.

| # | step | verify |
|---|---|---|
| **S1** | Switch normalisation median → **mean = 100** in `aggregate_archetype.py`; re-derive `family_profiles.json` | every family's mean == 100 ± 1 |
| **S2** | Implement the class-tilt law (Light/Medium/Heavy tilt, Super flat) | tilt direction matches the level for all 88 templates |
| **S3** | Rebuild Shield: `mean(physics_table, CEILING + floor)`, uniqueness-enforced | Tesla top, Melee bottom, 0 duplicate Shield values |
| **S4** | Regenerate all 88 templates; report the movement | `report_versus_change.py` + `verify_generator_sync.py` drift = 1 |
| **S5** | Re-price through the pipeline | `extract_stats --check` 0 drifted; needs `apply_balance --confirm` (maintainer order) |

**S1–S3 are pure generator work** — no yaml hand-edits, consistent with CLAUDE.md rule 3.

---

## 5b. MEASURED: neither structural formula can carry the identity (2026-08-16)

Maintainer asked whether `top` could be kept via a geometric mean:
`sqrt((200 + floor) x (100 + top))`. Computed over all 96 live templates:

| formula | range | spread | distinct values |
|---|---|--:|---|
| `200 + floor` | 210 – 265 | **1.26x** | **41 / 96** |
| `sqrt((200+floor)(100+top))` | 165 – 253 | **1.54x** | **44 / 96** |

The geometric mean IS better — wider spread, fewer ties — but both fail, and the reason is
mathematically interesting: **`floor` and `top` are anti-correlated by construction.** Every
profile is normalised, so a sharp family necessarily has a low floor and a high top, and a
flat family the reverse. Multiplying the two therefore CANCELS most of the variation — the
product is close to an invariant of the normalisation rather than a property of the weapon.
`Sonic_Medium` (top 55, floor 55) and `Melee_Heavy` (top 165, floor 35) land 199 vs 250.

Both formulas also violate the **no-two-families-share-a-Shield-value** rule outright: 41
and 44 distinct values across 96 templates means more than half are ties.

**Conclusion, now with numbers:** a structural rule can set the SCALE (where the Shield band
sits, and that it tracks weapon strength) but it cannot set the RANK. Anti-shield identity
is not recoverable from a normalised profile's own shape, because normalisation is exactly
what removes it. So:

```
Shield = physics_rank (input 2)  x  structural_scale (input 3)   then uniqueness-spread
```

`input 3` = `sqrt((200+floor)(100+top))` is the better of the two structural terms and is
recommended as the scale factor — it keeps `top` in the formula as the maintainer wanted,
and its near-invariance is a virtue in that role: it anchors the band without fighting the
physics rank for control of the ordering.

Tesla must end up several times a sword, not 1.2x. Only input 2 can do that.

---

## 6. Options for the maintainer

**Option A — full programme (recommended).** S1–S5 as above. Fixes the root cause
(normalisation), gives every level a structural identity, and restores Tesla. Largest
change; every profile moves.

**Option B — Shield only.** Just S3 with `CEILING + floor` plus the physics table, leaving
median-normalisation in place. Much smaller and fixes the reported bug, but leaves the mean
as a hidden magnitude multiplier, so families stay non-comparable and W23-style "pay for
the profile change" rescales remain necessary forever.

**Option C — widen input 1 first.** Before deciding, mine the 16 mods for *shield-like*
armor types under other names (`plasma_shield`, `energy`, `barrier`, forcefield analogues)
and for the SC/SC2 lineage where shields are a first-class mechanic. Would turn input 1
from 13 rows into something real and make a genuine 3-way average possible. Costs a
data-mining pass; delays S3.

**Recommendation: A, with C run in parallel** as a check on the physics table rather than a
blocker — the physics ordering is confident enough to proceed, and the corpus can confirm
or correct it afterwards.

---

## 6b. ✅ ALL FOUR DECISIONS TAKEN (maintainer, 2026-08-16)

1. **Tesla reaches 400** — *"if the formula allows it then yes but you need to calculate the
   exact value"*. It does. Calibrating `Shield = physics_rank x level x geometric_scale` so
   that `Tesla_Super` lands exactly on 400 gives **K = 1.39186**.
2. **Nuclear becomes a generalist** — *"nuclear can be changed now to be a generalist"*. No
   exception; the Super-is-flat law applies universally. The `BLD > VEH > AIR > INF` tilt is
   retired and `HAND_TUNED` comes off Nuclear.
3. **Options A + B + C combined** — full normalisation + tilt + Shield rebuild, with the
   corpus mining run as a parallel check on the physics table rather than as a blocker.
4. **Shield STAYS a `Versus` row.** Maintainer: *"shields have their own armor type so they
   feel unique. Energy weapons deal more damage to shields than physical weapons but
   physical weapons deal more damage to vehicle armor than energy weapons"* — i.e. Shield is
   a genuine rock-paper-scissors axis, not a redundant expression of the W21 layer.

### The computed ladder

| template | rank x level | geometric scale | Shield NOW | **Shield NEW** |
|---|--:|--:|--:|--:|
| `Tesla_Super` | 1.25 | 230 | 160 | **400** |
| `Storm_Super` | 1.19 | 235 | 199 | 388 |
| `Tesla_Heavy` | 1.12 | 225 | 151 | **350** |
| `Railgun_Heavy` | 0.87 | 249 | 200 | 302 |
| `Quantum_Heavy` | 0.92 | 234 | 160 | 299 |
| … | | | | |
| `Melee_Medium` | 0.22 | 249 | 200 | **76** |
| `Melee_Light` | 0.20 | 249 | 200 | 69 |

**Range 69–400 (5.80x). `Tesla / Melee` moves from 0.76x (inverted) to 4.60x.**
Distinct values: **77 of 93** — the remaining 16 ties are resolved by the existing `MIN_GAP`
uniqueness pass, exactly as the armor ladder already does.

⚠ **This restates a historical invariant and that is deliberate.** Shield used to be "the one
value always allowed ABOVE the cap", because shields were assumed uniformly soft. Under the
maintainer's ruling they are not — they are soft to energy and *hard to kinetics*. So
physical families land BELOW 100 (a sword at 76 is the canonical thing a shield stops) and
Shield is now exempt from the window in **both** directions. The old invariant was a
consequence of the old assumption, not a law in its own right.

## 7. Open decisions

1. **Shield range** — `CEILING + floor` gives 210/225/240. Should Tesla reach ~400 as it
   did before the chips were merged? That sets the scale's top.
2. **Nuclear vs the Super-is-a-generalist law** (§3) — exception, or soften the law?
3. **Option A / B / C.**
4. **Does `Shield` stay a `Versus` row at all**, now that W21 made shields a real health
   LAYER? If damage-to-shields is better expressed on the layer, this whole ladder may be
   the wrong mechanism. Worth answering before S3 rather than after.
