# Formula v2 — SCOUT INFANTRY class (maintainer proposal 2026-07-18)

**Proposed anchor (maintainer):** 20000 HP / 50 Speed / 5.0 Range /
4000 Damage / 50 Reload → DPS 80, Cost 100. Direction: replace the
^ScoutInfantryTemplate 50% damage-reduction knob with 2× RAW health
(the §5b "bake" — exact equivalence for damage taken; self-heal/repair
proportions shift slightly).

**Assessment (simulated against the real 27-member class with the
2×-health bake applied):**

| question | answer |
|---|---|
| Anchor structure sound? | YES — normalized class form works; O0=P0=Q0 symmetry like the Tiger is mathematically impossible at cost 100 with playable stats (each O-term would force speed ≈ 12), and it is NOT needed: the ratios do the work. |
| Speed 50 or 60? | **60 recommended** — every live scout runs 50–110 (median ~60); 50 sits below the whole population and skews all speed ratios upward. Median class deviation improves 215% → 170% just from this. |
| 2×-health bake? | YES — right direction, kills a category-3 knob, makes HP honest. Sequence it BEFORE anchoring. |
| Are the remaining deltas real? | Mostly NOT yet — two measurement gaps dominate (below). Median deviation after fixing the first: 67%. |

**Measurement gaps to close before the class can validate:**
1. Garrisoned armament variants double-counted DPS (fixed in the
   simulation by skipping `Name: garrisoned` arms; the extractor gets a
   pricing-armament flag).
2. WeaponClass judgments are unseeded — raw burst math overstates
   sustained DPS (e.g. MutAPRifle: 4000 dmg @ reload 8 → raw DPS 500;
   the legacy sheet's H column existed exactly to damp this). The
   per-weapon WeaponClass column must be seeded/judged before deltas
   are law.

**Validation snapshot (anchor @ speed 60, 2×HP bake, garrisoned excluded):**

| unit | 2×HP | spd | rng | dps | cost | v2 price | Δ |
|---|---|---|---|---|---|---|---|
| forgotten_mutant | 36000 | 65 | 3132 | 200 | 120 | 197 | +64% |
| forgotten_mutantsoldier | 60000 | 75 | 5146 | 500 | 250 | 1072 | +329% |
| naxis_coneheadsknights (melee) | 40000 | 90 | 1555 | 889 | 1000 | 549 | −45% |
| ra1_soviets_ak47conscript | 44000 | 71 | 4420 | 400 | 200 | 578 | +189% |
| ra2_allies_gi | 50000 | 50 | 4750 | 240 | 200 | 335 | +67% |
| tkm_trooper | 32000 | 60 | 5594 | 286 | 200 | 380 | +90% |
| zerg_defiler | 160000 | 50 | 9000 | 171 | 1400 | 1079 | −23% |
| zerg_spithid | 80000 | 110 | 3855 | 133 | 300 | 454 | +51% |

**Proposed execution order (on maintainer GO):**
1. Bake: ^ScoutInfantryTemplate loses DamageMultiplier@ScoutInfantryBuff;
   all 27 members get HP×2 through the pipeline (ledger → apply).
2. Seed WeaponClass for the class's ~20 weapons (maintainer judgments,
   legacy H column as the starting point).
3. Anchor: `fit_class.py --class scout --spec 20000,60,5000,4000,50,100`
   → validation table → sign-off → workbook prices scouts with it.
4. Then per-unit deltas are real and the maintainer approves price/stat
   corrections row by row.

Anchor registry: `scout` will be written by fit_class on step 3;
until sign-off the class keeps the global Tiger formula.

---

## CONVERSION LOG (the learning record — one entry per converted unit)

### 1. japan_imperialscoutsman — THE LIVING BASELINE (2026-07-18, boot-verified)

- Chosen by the maintainer as closest to spec: was 11000 HP / 55 spd /
  cost 100 / CHGuardRifle (2000 dmg, 55 rl, 5547 rng).
- Set to the exact baseline: **20000 HP / 50 Speed / 5.0 Range /
  4000 Damage / 50 Reload / Cost 100** → O = P = Q = 100.000000 by the
  per-stat-normalized construction (`formula.class_baseline_price`).
- **Rule confirmed general**: the maintainer's O=P=Q=cost law holds for
  ANY class baseline under this construction — the Tiger's constants
  are exactly this construction with (100000, 100, 5000, 200, 800).

Lessons captured for the next conversions:
1. **Shared-weapon hazard**: CHGuardRifle serves civilians/generals
   rules — the baseline got a DEDICATED weapon
   (`japan_imperialscoutsman_rifle`). Check weapon sharing FIRST, every
   time (grep `Weapon: <name>` repo-wide).
2. **Knob neutralization is per-unit during the one-by-one phase**: the
   template keeps ScoutInfantryBuff (50% dmg reduction + 110%
   firepower) for unconverted units; converted units override BOTH to
   100 in their own block. When all 27 are converted, delete the
   template knobs + all overrides in one sweep.
3. **Scale self-heal with HP**: ChangesHealth Step went 11 → 20
   (keeps heal-rate proportional under the 2x-health bake).
4. **Upgrade-weapon variants stay untouched** (waveforce armament keeps
   CHGuardRifleWaveforce) — their rebalance rides the upgrade pricing
   pass, not the baseline conversion.
5. Sight 8000 is inherited and NOT part of the anchor spec — left
   alone; decide later whether sight joins the formula.

Next unit: maintainer picks (suggest ra2_soviets_conscript — closest
remaining to baseline ratios), each conversion appends a log entry here.
