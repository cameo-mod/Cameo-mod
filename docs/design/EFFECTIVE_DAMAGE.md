# Effective damage — the area-integrated weapon metric (spec, rev. 2026-08-11)

Tool: `tools/balance/effective_damage.py` (read-only) · written into every ledger by
`tools/balance/extract_stats.py` · tests in `tools/tests/test_effective_damage.py`.

> ⚠ **Two different things in this repo are called "effective damage".** See §1
> before using either. This document is about the **area-integrated metric**.

---

## 0. Why it exists

Raw `Damage` is not comparable across weapon types. A railgun that puts 20 000 into
one hitbox and a nuke that puts 20 000 into a 4-cell blast have the same number and
wildly different battlefield value. Before this metric the pipeline priced on
`Σ Damage / reload`, which meant:

- **AoE was free.** Spread and Falloff cost nothing, so wide warheads were underpriced
  and single-target energy weapons were overpriced.
- **Accuracy was free.** A slow, inaccurate mortar and a hitscan laser of equal `Damage`
  priced identically, though the mortar frequently misses a moving target.
- **The 3-axis Spread/Falloff work had no price signal.** `SPREAD_FALLOFF_PLAN.md`
  makes the falloff SHAPE a design axis; nothing turned that shape into a number.

The metric folds all three — **how much**, **over what area**, **how often it lands** —
into one comparable scalar, so the class anchors can be fitted across weapon families
instead of within them.

## 1. ⚠ The name collision — read this first

| | **A. "effective damage per shot"** | **B. `effective_damage` (this doc)** |
|---|---|---|
| Defined in | `LESSONS_LEARNED.md` §Uniqueness | `tools/balance/effective_damage.py` |
| Formula | `Σ(main warhead Damage) × FirepowerMultiplier` | `Σ base × (reliability + SWARM_W × footprint)` |
| ExtraDamage chips | **excluded** (paid for by K / charge delay) | **included** |
| FirepowerMultiplier | **applied** | **not applied** (actor-level, not weapon-level) |
| Used for | uniqueness rule #3 (5 stats per class) | ranking / pricing input (**not yet wired**) |

They are **not interchangeable**. A ledger row's `effective_damage` must never be fed to
the uniqueness audit, and the uniqueness stat must never be called `effective_damage` in
code. When in doubt, say "uniqueness damage" for A and "area-integrated damage" for B.

## 2. The formula

```
effective = Σ over (main + every *_ExtraDamage)   base × ( reliability + SWARM_W × footprint )

  footprint   = 2π ∫ (F(r)/100) · r dr / 1024²                     [cell²]
  reliability = E[ F(miss distance) ] over the engine's scatter     [0 … 1]
  σ           = Inaccuracy + LEAD × TARGET_SPEED × Range / min(Speed, SPEED_CAP)
  clamps      : Spread ≥ 100 ; Falloff at least "100, 0"
```

Constants (all at the top of the tool, all tunable):

| constant | value | meaning |
|---|---|---|
| `SWARM_W` | 0.25 | **target density, in units per cell²** — see §2.3 |
| `LEAD` | 0.20 | the engine leads/tracks, so real miss ≈ 20 % of raw displacement |
| `TARGET_SPEED` | 100 | a typical dodging vehicle, WDist/tick |
| `SPEED_CAP` | 10000 | ≥ this a projectile is "basically instant" (~10 cells/tick) |
| `MIN_SPREAD` | 100 | nothing integrates below Spread 100 |

### 2.1 footprint — "how much ground does this cover"

`F(r)` is the piecewise-linear falloff curve the engine actually evaluates. Integrating
`F(r)·r dr` in polar coordinates gives the **damage-weighted area**: a disc that takes
full damage everywhere contributes its whole area, a cone that decays to zero
contributes less than its outer circle. Dividing by `1024²` puts it in **cells²**, the
unit a designer can picture.

Sanity check (pinned by a test): `Falloff: 100, 100` over `0…1024` gives exactly π cell²
— the area of a 1-cell-radius circle.

### 2.2 reliability — "does it land on the guy you aimed at"

The probability-weighted falloff value at the impact point, for a **single point
target**. `σ` has two terms:

- **`Inaccuracy`** — the weapon's own scatter, straight from the projectile.
- **travel drift** `LEAD × TARGET_SPEED × Range / Speed` — a slow projectile at long
  range gives the target time to walk out of the blast. Dimensionally: (WDist/tick) ×
  WDist / (WDist/tick) = WDist. ✔

**Instant hits** (`InstantHit`, `LaserZap`, `Railgun`, …, or any projectile with no
`Speed`) always strike the aim point → `reliability = 1.0` at every range. This was a
maintainer decision (2026-08-11) after global-range superweapons drifted absurdly.

**The scatter distribution matches the engine, not a convenient approximation.**
`Bullet.cs` does `target += WVec.FromPDF(rng, 2) * maxInaccuracy / 1024`, and
`WVec.FromPDF(r, 2)` draws **each axis** as the sum of two uniforms — a *triangular*
density on `[-σ, σ]`. Hits therefore cluster near the aim point:

| model | mean miss radius | P(miss < σ/4) |
|---|---|---|
| engine (2-axis triangular) | **0.52 σ** | **15.7 %** |
| uniform disc (the old approximation) | 0.67 σ | 6.2 % |

The tool integrates against the real density. Using a uniform disc — as the first
revision did — throws ~28 % of hits too far out and **systematically under-values
inaccurate and slow weapons**. Fixing this moved 1 950 of 2 024 ledger rows up, median
**+4.9 %**, max **+167.8 %**.

### 2.3 `SWARM_W` — what the 0.25 actually means

`reliability` is a probability and `footprint` is an area, so adding them looks
dimensionally wrong. It is not, once `SWARM_W` is read as its true meaning:

```
effective / base  =  P(hit the primary target)  +  E[number of SECONDARY targets caught]
                  =  reliability                +  density × footprint
```

`SWARM_W` is a **target density in units per cell²**. `0.25` = one unit per 4 cell² = a
unit every 2×2 cells, i.e. a moderately packed formation. That is the knob to turn if
AoE feels over- or under-valued, and it has a physical meaning you can argue about:

- raise it toward `0.5` if you want blob-clearing weapons to price higher;
- lower it toward `0.1` to model spread-out armies where splash rarely multi-hits.

## 3. What it deliberately does NOT include

| not included | why | consequence |
|---|---|---|
| `ReloadDelay`, `Burst`, `BurstDelays` | the metric is **per shot**, not per second | **it is not DPS.** `DPS = effective × burst / eff_reload`. `fit_class.py` still builds DPS the old way |
| `FirepowerMultiplier` | actor-level, and one weapon serves many actors | multiply at the actor, as `fit_class.py` already does |
| `WeaponClass` (0.75/1.0/1.25/1.5) | **RETIRED from pricing entirely (W4)** | `formula.dps()` no longer takes it; K measures weapon quality directly |
| `Versus` / armor profile | lives in the `^Warhead_*` template layer | folded into K as `avg_versus` (W1), weighted by the measured armor census |
| `*_Percentage` twins | a different currency (% of max HP) | converted through `reference_hp` and folded into K |
| `*FriendlyFire` twins | baked own-side splash, never a benefit | correctly ignored |

### 3.0 The context factors (W5, 2026-08-11) — no longer excluded

`ValidTargets` and `MinRange` used to sit in the table above. They are now measured,
each as a **separate named factor** rather than one blended fudge, so a price that
moved can be traced to the single factor that moved it:

| factor | models | shape | example |
|---|---|---|---|
| `targets` | `ValidTargets` — a weapon that cannot hit air fights less of the game | `FLOOR + (1-FLOOR) x engagement share`, `TARGETS_FLOOR = 0.5` | ground-only **0.95**, AA-only **0.55** |
| `range` | outranging is worth more than DPS | `1 + 0.25 x (range/median - 1)`, bounded `[0.75, 1.50]` | median range 1.00, long artillery **1.33** |
| `deadzone` | a `MinRange` hole costs the annulus you cannot cover | `1 - (MinRange/Range)²` (area, not radius) | MinRange 2800 / range 11000 → **0.96** |
| `overkill` | DPS ignores waste — a 200k burst on a 50k target throws away 75% | `HP / (ceil(HP/dmg) x dmg)` — waste is only the LAST shot | 200k on 50k → **0.25** |

**The split that matters.** `targets`, `range` and `deadzone` do **not** depend on
`Damage`, so they fold into **`k_flat_context`** and the pricing inversion stays closed-form:

```
Damage_required = (target_per_shot - pct_absolute_context) / k_flat_context
```

**`overkill` does** depend on Damage — it compares per-shot damage against target HP.
Folding it into K would turn that exact inversion into a fixed-point iteration, so it is
reported **beside** K and never inside it. `tools/tests/test_weapon_context.py` pins
this distinction; if you ever fold `overkill` in, the inversion must become iterative.

⚠ **The `%`-of-max-HP twin was the same defect, and it WAS folded in** (E4, measured
2026-08-17). Its damage is a share of the TARGET's max HP, so it does not scale with the
weapon's flat `Damage` — yet `k` carried it as `share = ref_hp × pct_damage / 100 /
flat_total`, putting `flat_total` in a denominator. **`k` and `k_context` therefore move
when `Damage` moves and must never be inverted**: doubling `AnthraxCloudLarge`'s flat
Damage drops its `k` by 37%, and inverting through it to reach 2× the DPS prescribes 40%
of the Damage actually needed. `k` is still a correct MEASUREMENT — `effective_per_shot =
Damage_total × k_context` is exact at the weapon's current Damage — it is not a shape
coefficient. Invert `k_flat_context` (scale-invariant) against `pct_absolute_context`
(additive). Guard: `tools/audit/audit_k_linearity.py`.

`TARGETS_FLOOR` exists because AA units are separately class-anchored: a raw
engagement share would price an AA-only weapon at 0.10 and penalise those units twice.
An exotic `ValidTargets` set with no `Ground`/`Air` token (`Infantry, Monster`) scores
1.0 — declining to judge beats guessing "hits nothing".

**`AttackDelay` — the fifth item — does not exist.** W5 listed it, but the field appears
**0 times** in the tree: charge-up is an ACTOR trait (`AttackCharged`, `AttackCharges`,
`AttackTesla`, …), and W4 implemented it there as a 0.75x price multiplier. Nothing to
add at the weapon level.

### 3.1 ⚠ The ExtraDamage contradiction (needs a maintainer ruling)

`formula.spread_damage_sum()` **excludes** `*_ExtraDamage` chips — DESIGN law:
"`*ExtraDamage` is ALWAYS excluded from the damage calculation", because the chip is
*paid for* by a structural handicap (Tesla's `K = 1.25`, Railgun's charge delay).

`effective_damage` **includes** them.

Both positions are defensible — a chip is real damage the enemy takes, so an
"effective damage" comparison that omits it is lying; but pricing it twice (once in the
chip, once in the handicap) double-charges the weapon. **They cannot both be right when
this column is wired into pricing.** Decide before that happens, and record it here.

## 4. Engine fidelity: the single-`Range` footgun

`AreaDamageWarhead` and upstream `SpreadDamageWarhead` both do:

```csharp
if (Range != null) effectiveRange = Range;                  // NO expansion
else effectiveRange = [i * Spread for i in Falloff];
...
int GetDamageFalloff(int distance) {
    var inner = effectiveRange[0].Length;
    for (var i = 1; i < effectiveRange.Length; i++) { ... }  // never runs when Length == 1
    return 0;
}
```

A `Range:` with a **single** value and a multi-step `Falloff` therefore makes the warhead
deal **zero damage at every distance**. The load-time validation accepts it (`Range.Length
== 1` is legal), so it fails silently.

The metric now mirrors this exactly rather than inventing a per-step grid. **See
`ROADMAP.md` for the live weapons currently hit by this.**

## 5. Status — where the numbers live, and what still reads them

`extract_stats.py` writes them to **`docs/balance/derived/<faction>.json`**, never to
the raw ledger (W3, 2026-08-11). One row per armament, joined back to the raw ledger by
`slot` + `weapon`:

| field | metric | meaning |
|---|---|---|
| `effective_damage` | area-integrated | per-shot damage integrated over the blast, after scatter |
| `damage_total` | — | Σ flat main Damage (the input the two metrics share) |
| `footprint` | area-integrated | damage-weighted area in cell² |
| `reliability` | area-integrated | P-weighted falloff at the impact point |
| `sigma` | area-integrated | scatter σ in WDist |
| `k` | **pricing (W1)** | the dimensionless coefficient — see below |
| `k_context` | **pricing (W5)** | `k × targets × range × deadzone` — still Damage-independent |
| `avg_versus` | pricing | prevalence-weighted mean Versus over the FLAT warheads |
| `factor_targets` / `factor_range` / `factor_deadzone` | pricing (W5) | the three context factors, individually inspectable (§3.0) |
| `overkill` | diagnostic (W5) | Damage-DEPENDENT, so reported beside K, never inside it |
| `effective_per_shot` | pricing | `k_context × damage_total` |
| `eff_reload` | pricing | `formula.eff_reload(reload, burst, burst_delays)` |
| `effective_dps` | pricing | `k_context × damage_total × burst / eff_reload` |

Two metrics sit side by side on purpose — §1 warns they are not interchangeable, so
they keep distinct names instead of being blended into one number.

`effective_dps` is the **weapon's** number. `FirepowerMultiplier` is an actor property
and is deliberately not baked in; the caller applies it.

`docs/balance/derived/_model.json` records the constants every one of these depends on
(`SWARM_W`, `LEAD`, `TARGET_SPEED`, `MIN_SPREAD`, `A_BLOB`, `A_SELF`, `BLOB_UPTIME`,
`DENSITY`, `ENGAGEMENT`, `reference_hp`, the armor census). A retune therefore shows up
as a short readable diff at the top of the tree, not only as thousands of shifted
decimals underneath it.

**Nothing reads any of it yet.** `fit_class.py`, `formula.py`, `apply_balance.py` and
`propose_class_rebalance.py` all still price on `Σ main Damage × WeaponClass × burst /
eff_reload × FirepowerMultiplier`; `build_workbook.py` never read the fields even when
they sat in the ledger. This is a *diagnostic* tree, not an input — wiring K into
pricing is **W11**, behind a flag and with a maintainer sign-off.

### 5.1 The "RAW STATS ONLY" ledger law — restored

`BALANCE_PIPELINE.md` §2 is explicit: *"No DPS, no combined Damage, no
effective-anything in the JSON. Every number appears exactly as the yaml states it."*
For a few days it was broken: `c9a09dc91` wrote five derived fields into every ledger
row.

That was not pedantry. A derived field moves when the **model** changes, not when the
game does: fixing the scatter model on 2026-08-11 rewrote **4 136 ledger lines with zero
`mods/` changes**. The ledger's purpose is to prove yaml ↔ ledger equality
(`audit_balance_drift`), and mixing model noise into it made "did a real stat move?"
unanswerable by diff.

The split restores it, and the restoration is pinned rather than trusted:

- `extract_stats.build_ledgers()` returns the **raw** docs by construction, so the drift
  audit cannot start diffing model output by accident;
- `extract_stats.py --check` verifies both trees and labels each finding `DRIFT (raw)`
  ("the game changed") or `DRIFT (model)` ("a tool changed");
- `tools/tests/test_ledger_split.py` fails if any committed raw ledger regrows a model
  field — under the pre-split ledgers that guard trips on 310 rows.

The split commit itself is the proof of purity: **12 130 deletions, 0 additions**, every
removed line one of the five field names.

## 6. Roadmap for the metric

> **Status and ownership for every item below live in
> [`BALANCE_PROGRAM_PLAN.md`](BALANCE_PROGRAM_PLAN.md) (W1–W12), not here.** W1 (the K
> coefficient, prevalence-weighted Versus and the capped density model) is **done** —
> `tools/balance/target_model.py` + `tools/balance/weapon_efficiency.py`. The list below
> is the original reasoning; the plan file is what to execute.

**The K coefficient (W1, done).** Because `effective` is linear in `base`, a weapon's
whole geometry collapses into one dimensionless number:

```
effective_dps = Damage_total × (burst / eff_reload) × FirepowerMultiplier × K
K = Σ_warheads  share_w × versus_w × ( reliability_w + secondary_w )
```

The FLAT part of K never depends on the Damage magnitude, so pricing inverts exactly and
the grid is never violated — `Damage_required = (target_per_shot − pct_absolute_context) /
k_flat_context`, snapped to the grid. A workbook value of 2351.85 therefore never goes into
yaml: the designer sets geometry for feel, K measures it, the pipeline solves for Damage.

⚠ The `%`-twin is **additive**, not multiplicative (see the E4 note above), so the model is
affine: `effective_per_shot = Damage_total × k_flat_context + pct_absolute_context`. That
also means **the twin is a DPS FLOOR** — a weapon still delivers `pct_absolute_context` at
`Damage: 0`, so no reduction of flat Damage can price it lower. 1537 concrete weapons carry
a twin and 52 have a floor at ≥25% of output (worst: `AnthraxCloudLarge`, 75%). A target
below the floor is UNREACHABLE; `weapon_efficiency.required_damage()` returns `None` rather
than a confidently wrong positive number, and `dps_floor` is published per weapon in the
derived ledger. To price such a weapon lower, the TWIN has to shrink.

**Secondary targets (W1, done).** `secondary = ρ_class × BLOB_UPTIME × (min(footprint,
A_BLOB) − A_SELF)`. `ρ` is per macro class (INF 2.0 / VEH 0.33 / BLD 0.25 / AIR 0.20 units
per cell²) so anti-infantry splash legitimately catches more bodies; `A_BLOB = 9 cell²`
caps a superweapon-sized blast from claiming 50 kills; `A_SELF = 1 cell²` stops the aimed
target being counted twice. `BLOB_UPTIME = 0.30` is the fraction of shots that actually
land in a crowd — without it every splash family scored ~5× every single-target family.



1. **Decide §3.1** (chips in or out). Blocking for any pricing use.
2. **Make it a rate.** Add `effective_dps = effective × burst / eff_reload(...)` to the
   ledger so it is comparable with the DPS the formula already uses. Per-shot and
   per-second answer different questions and both are wanted.
3. **Armor-weight it.** `effective_vs = Σ_armor w(armor) × Versus(armor) × effective`
   with `w` a target-mix weight per class. This is the single biggest missing axis: it
   would let the anchors compare an anti-infantry and an anti-tank weapon honestly.
4. **Calibrate `SWARM_W` from the game**, not from taste: measure the mean number of
   actors inside a 1-cell² disc in real engagements and set the density from that.
5. **Model `TARGET_SPEED` per victim class** rather than one global 100 — a Harvester and
   a scout bike do not dodge alike; the drift term is linear in it.
6. **Then** wire it into `fit_class.py` behind a flag, fit one class both ways, and
   compare the resulting prices before switching the pipeline over.

## 7. Reading the numbers

```sh
python tools/balance/effective_damage.py --top 40      # ranked table
python tools/balance/effective_damage.py NAME [NAME…]  # specific weapons
```

Diagnostics per row: `base` (raw sum), `reliab` (0…1), `footprint` (cell²), `sigma`
(WDist). Useful shapes:

- `reliab` far below 1 on a weapon that should be accurate → check `Inaccuracy` / `Speed`.
- `effective` well below `base` → the warhead is barely landing, or it is a **dead**
  warhead (§4).
- `footprint` ≈ 0 on something that should splash → `Spread` or `Falloff` is wrong.

Related: [`BALANCE_PIPELINE.md`](BALANCE_PIPELINE.md) · [`FORMULA_V2.md`](FORMULA_V2.md) ·
[`SPREAD_FALLOFF_PLAN.md`](SPREAD_FALLOFF_PLAN.md) · [`WEAPON_3WAY_SPLIT.md`](WEAPON_3WAY_SPLIT.md)
