# Continuous heaviness — balance & gameplay research

> # ⛔⛔ CORRECTION 2026-08-22 — THE VERSUS NUMBERS BELOW ARE WRONG
>
> **Every Versus figure in this document was measured with a broken parser** and must not be
> acted on. The parser never CLOSED the `Versus:` block, so the `PercentageVersus:` rows that the
> AreaDamage fold added inside the same warhead node silently OVERWROTE the real profile. What
> was measured throughout was the percentage twin's 1..16 RANK ladder, not the armor profile.
>
> ```
> Warhead@Bullet_Light: AreaDamage
>     Versus:            None: 200 ... Spaceship 53, Superheavy 48   <- the REAL profile
>     PercentageVersus:  None: 16  ... Spaceship  2, Superheavy  1   <- what was read
> ```
>
> **Re-measured correctly (close the block when indentation returns to its level):**
>
> | claim in this document | truth |
> |---|---|
> | 0 of 125 profiles obey the MEAN-100 law | **123 of 125 obey it** (the 2 are `Nuclear_Super` and `Sniper_Light`, both `HAND_TUNED`) |
> | every family violates the 2x-8x spread band | **37 of 42 are IN the band**; median spread **4.17x** against a target of 4x |
> | spreads run 12x-100x | min 1.00x, p25 2.61x, median 4.17x, p75 4.97x, max 10.00x |
> | the level applies an additive `+4 / +5` offset | that was the twin's rank ladder stepping 1/5/10 |
> | 26 of 42 families invert under a tier-only bell | unverified — measured on the wrong data |
> | a Heavy weapon self-prices at ~2x a Light one | unverified — measured on the wrong data |
>
> **The real outliers are three families:** `CannonAP` 1.81x and `Cryo` 1.97x (too flat), `Sniper`
> 10.00x (too sharp). `Magic` and `Sonic` at 1.00x are FLAT BY DESIGN — `mean_normalise`
> special-cases them ("ignores armor").
>
> **And the substance of this document is already LAW and already LIVE.** See `DESIGN.md`
> **§12.0a THE MEAN-100 LAW** (2026-08-16, binding: *"all warheads average all versus values at
> 100"*, so `K` is SHAPE-ONLY and `Damage` is the sole magnitude knob) and **§12.0d THE CLASS
> TILT** (each LEVEL tilts toward one end of every armor ladder — Light toward the lightest rung,
> Heavy toward the heaviest). §12.0d is the bell curve, and it already solves the inversion
> problem properly: *"the tilt MUST NEVER reorder a ladder ... it can never invert"* — it is
> applied to the VALUES and each armor is then given back the RANK it held. Both are implemented
> in `gen_weapon_template.py` (`class_tilt` line 927, `mean_normalise` line 980) and live.
>
> **What is genuinely still open** is only this: make the tilt CONTINUOUS (driven by `h` from
> `tier_chain`) instead of four discrete levels, and collapse the level templates to one per
> family plus a per-weapon `h`.

**Status:** research findings. No yaml, no C#, no balance numbers changed.
**Date:** 2026-08-22
**Companion to:** [`CONTINUOUS_WEAPON_HEAVINESS.md`](CONTINUOUS_WEAPON_HEAVINESS.md)

Answers four maintainer questions: does heaviness feed the PRICE, does a late-game unit keep its
anti-light identity, what deterministic rule governs SECONDARY weapons, and what must not be
broken when families are collapsed.

---

## 1. Does `h` raise the unit's price? — **YES, automatically**

`tools/balance/weapon_efficiency.py` line 8:

```
K = SUM over warheads   share_w x versus_w x ( reliability_w + secondary_w )

  versus_w = Versus averaged over armors, WEIGHTED by how common each armor
             actually is (target_model.armor_weights)
```

and `extract_stats.py`:

```
effective_dps = k_context x damage_total x burst / eff_reload
```

Price is driven by effective DPS. So **raising Versus raises `versus_w` → raises K → raises
`effective_dps` → raises the price**, with no additional plumbing.

Measured, with the additive offset and real armor weights:

| family | h=0 | h=1 | h=2 | price pressure at h=2 |
|---|--:|--:|--:|--:|
| Laser | 0.075 | 0.115 | 0.165 | **+120%** |
| MissileAP | 0.090 | 0.130 | 0.180 | **+100%** |
| Flame | 0.099 | 0.139 | 0.189 | **+91%** |

A Heavy weapon costs roughly **twice** a Light one, purely through measured effectiveness.

### 1.1 It cannot double-charge — the old knob was already removed

`formula.py:82`:

> **`weapon_class` was REMOVED here on 2026-08-11 (W4).** It was a tier weight standing in for
> "how good is this weapon type", back when nothing measured that. The K coefficient now measures
> it directly from the weapon's own geometry, so keeping the tier weight as well would charge a
> weapon twice for the same property.

Continuous heaviness works **through** K, exactly where the discrete tier weight was deliberately
taken out. The two designs are already compatible; nothing needs adding to the price formula.

---

## 2. ⛔ Can a Tier-4 unit stay anti-light? — **NOT under the additive offset**

**This corrects an earlier claim that "family character survives".** It survives in ORDERING but
not in MAGNITUDE. A uniform additive offset raises every armor entry equally, so the RATIO between
a family's best and worst target collapses as `h` rises:

| family | h=0 | h=1 | h=2 |
|---|--:|--:|--:|
| MissileAP (best/worst) | **16.00x** | 4.00x | **2.50x** |
| Laser | 5.33x | 2.86x | 2.08x |
| Flame | 3.20x | 2.22x | 1.79x |
| CannonHE | 1.75x | 1.50x | 1.35x |

`MissileAP` goes from savagely anti-heavy to nearly generic.

⚠ **This is not caused by the continuous model — today's discrete ladder is already additive**, so
current Heavy weapons are ALREADY less differentiated than current Light ones. The continuous
model only makes an existing defect visible and smooth.

### 2.1 The fix: multiplicative, not additive

```
ADDITIVE        Versus(armor, h) = base(armor) + offset(h)     ratios COLLAPSE
MULTIPLICATIVE  Versus(armor, h) = base(armor) x (1 + 0.5h)    ratios PRESERVED EXACTLY
```

Multiplicative keeps a Tier-4 Venom exactly as anti-light as a Tier-1 one, just stronger overall.
It also produces the same ~2x K at h=2 (K is linear in Versus), so **the pricing behaviour of
§1 is unchanged**.

| | additive | multiplicative |
|---|---|---|
| reproduces today's templates | **yes, exactly** | no |
| preserves RPS at high tier | **no** | **yes** |
| effect on price | 2x at h=2 | 2x at h=2 |
| is it a balance change | none | **yes — every Medium/Heavy weapon moves** |

⚠ **This is the decision that matters.** Additive is a pure refactor; multiplicative is the
correct design but restates every non-Light weapon and must go through the balance pipeline.

---

## 3. Secondary weapons — a deterministic rule

### 3.1 The population

| | actors |
|---|--:|
| with 2+ classifiable weapons | 320 |
| ...all weapons at the SAME level (a unit-derived `h` just works) | **270 (84%)** |
| ...MIXED levels, needing a per-weapon rule | **50 (16%)** |

Mixed combinations: `Medium+Heavy` 22, `Light+Medium` 13, `Light+Medium+Heavy` 7, `Heavy+Super` 5,
`Light+Heavy` 3.

The mixed cases are **not random — they track the armament's ROLE**, which is already encoded in
the armament key:

```
td_gdi_minigunner     PRIMARY=Bullet_Light,  Upgrade=Bullet_Medium
td_gdi_humvee         Armament=Bullet_Light, Upgrade=Bullet_Medium
A10Carrier            GUNS=Bullet_Medium,    AA=MissileAP_Heavy
ra1_allies_destroyer  Armament=MissileAP_Heavy, DC1/DC2=Demolition_Light
```

### 3.2 The shared-weapon problem

522 of 1524 weapons are carried by 2+ units. Most are tier-consistent (p50 spread **0.00**,
p75 **0.12**) — but **96 weapons span >= 1.0 in `h`** across their carriers:

```
8Inch          h 0.00 (ra1_allies_cruiser)   ->  2.00 (ts_nod_cruiser)      15 carriers
Grenade        h 0.00 (td_gdi_grenadier)     ->  2.00 (ra2_allies_ifv_chrono) 9 carriers
BigFlamer      h 0.00 (ra2_allies_ifv_mg)    ->  2.00 (ra2_allies_ifv_chrono) 9 carriers
```

A warhead field is per-WEAPON, so one weapon can only have one `h`. Per-carrier heaviness is not
expressible without cloning the weapon per tier — which is the template explosion again.

### 3.3 Proposed rule — deterministic, no manual tagging for 96% of cases

```
h(weapon) = clamp( 5 x (1 - f(C_min)), 0, 2 )

  C_min = the SMALLEST prerequisite chain cost among all units that field this weapon
          as a PRIMARY armament; if it is never primary, over all carriers.

  An explicit `Heaviness:` on the weapon overrides, and always wins.
```

**Why minimum, and why it is principled rather than arbitrary:** a weapon's heaviness is *the
earliest tech at which it can be fielded*. If `8Inch` is available on a T1 cruiser, it is a T1 gun,
and the T3 cruiser is simply fielding a T1 gun — it should pay T1 price for it and be differentiated
by hull, armor and its OTHER weapons. This also solves the coaxial-MG case for free: a machine gun
shared with a T1 scout is never primary on the tank, so it takes the low `h` and stays a genuine
anti-light counter, exactly as intended.

**Determinism:** the rule is a pure function of (roster, prerequisite graph, explicit overrides).
It contains no averaging, no sampling and no dependence on evaluation order, so repeated pipeline
runs cannot drift.

**Cost:** the ~50 mixed actors and 96 wide-span weapons should be reviewed once; anything the rule
gets wrong gets an explicit `Heaviness:` and is then frozen and auditable.

---

## 4. Family identity — what must NOT be lost when collapsing

The maintainer flagged that Laser is anti-heavy and the anti-light beam should be Prism, with
Inferno = Prism x Flame. **The data confirms all three.** Profiles at the Heavy rung:

| family | None | Light | Medium | Heavy | Superheavy | character |
|---|--:|--:|--:|--:|--:|---|
| Laser | 12 | 13 | 17 | 21 | 25 | **ANTI-HEAVY** |
| Railgun | 17 | 22 | 23 | 24 | 25 | ANTI-HEAVY |
| Tesla | 18 | 19 | 21 | 23 | 25 | ANTI-HEAVY |
| **Prism** | **24** | 22 | 19 | 16 | **14** | **ANTI-INFANTRY** |
| **Inferno** | **24** | 19 | 17 | 15 | **14** | ANTI-INFANTRY |
| Flame | 25 | 17 | 16 | 15 | 14 | ANTI-INFANTRY |

`Inferno` sits **between** `Prism` (24, 22, 19, 16, 14) and `Flame` (25, 17, 16, 15, 14) at every
armor class — numerically consistent with `Inferno = Prism x Flame`. The taxonomy is already right
and must be preserved through any collapse.

### 4.1 ⚠ Two RPS-dead families found while checking

| family | None | Light | Medium | Heavy | Superheavy |
|---|--:|--:|--:|--:|--:|
| **Sonic** | 10 | 10 | 10 | 10 | 10 |
| **Magic** | 50 | 50 | 50 | 50 | 50 |

Both are **completely flat** — no rock-paper-scissors at all, at any level. Multiplicative scaling
cannot help them (`flat x k` is still flat). They need real profiles authored, and that is a
separate design task from heaviness.

---

## 5. What this changes in the build order

`CONTINUOUS_WEAPON_HEAVINESS.md` §7 stands, with two insertions:

1. Fix the 9 broken level ladders (unchanged blocker).
2. **NEW — rule additive vs multiplicative (§2).** This decides whether the rollout is a refactor
   or a balance restat, so it must be settled before any C# is written.
3. **NEW — author profiles for `Sonic` and `Magic` (§4.1).**
4. Add `Heaviness` to `AreaDamageWarhead`, inert at 0.
5. Verify the transform reproduces all 126 existing templates.
6. Collapse to one template per family; set `h` by the §3.3 rule.
7. Re-point the 102 mix weapons; lower the ratchets.

---

## 6. Provenance

Every figure measured on the resolved ruleset (`tools/audit/miniyaml.Ruleset`) or read from
`tools/balance/weapon_efficiency.py` / `formula.py` / `docs/balance/derived/*.json`, on 2026-08-22.

⚠ The "family character survives" claim in the first heaviness discussion was WRONG (§2) — it
checked ordering and not magnitude. Confirm differentiation with a best/worst RATIO, never by
eyeballing whether the biggest number is still biggest.
