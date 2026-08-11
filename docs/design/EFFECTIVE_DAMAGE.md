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
| `WeaponClass` (0.75/1.0/1.25/1.5) | a tier weight, not a physical property | `formula.dps()` applies it separately |
| `Versus` / armor profile | lives in the `^Warhead_*` template layer | two weapons with identical `Damage` and opposite armor profiles score the same |
| `ValidTargets` | — | a ground-only and an all-target weapon score the same |
| `MinRange`, blockability, `TargetActorCenter` | second-order | — |
| `*_Percentage` twins | a different currency (% of max HP) | priced separately |
| `*FriendlyFire` twins | baked own-side splash, never a benefit | correctly ignored |

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

## 5. Status — the column is not wired into pricing

`extract_stats.py` writes five fields per weapon (`c9a09dc91`):

`effective_damage`, `effective_base_total`, `effective_footprint_cells2`,
`effective_avg_reliability`, `effective_sigma`

**Nothing reads them.** `fit_class.py`, `formula.py`, `apply_balance.py` and
`propose_class_rebalance.py` all still price on `Σ main Damage × WeaponClass × burst /
eff_reload × FirepowerMultiplier`. The column is currently a *diagnostic*, not an input.
That is the correct state until §3.1 is decided and §6 is worked.

### 5.1 ⚠ It also violates the "RAW STATS ONLY" ledger law

`BALANCE_PIPELINE.md` §2 is explicit: *"No DPS, no combined Damage, no
effective-anything in the JSON. Every number appears exactly as the yaml states it."*
These five fields are derived, so the ledger no longer obeys its own law.

This is not pedantry. A derived field moves when the **model** changes, not when the
game does: fixing the scatter model on 2026-08-11 rewrote **4 136 ledger lines with zero
`mods/` changes**. The ledger's purpose is to prove yaml ↔ ledger equality
(`audit_balance_drift`), and mixing model noise into it makes "did a real stat move?"
unanswerable by diff.

**Recommendation:** emit derived metrics to `docs/balance/derived/*.json` from the same
`extract_stats.py` run, and keep the ledger raw. Maintainer's call — until then, do not
build anything that assumes these fields live in the ledger.

## 6. Roadmap for the metric

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
