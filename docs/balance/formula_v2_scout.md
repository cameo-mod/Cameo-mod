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
- Set to the exact baseline (speed corrected 50→60 by maintainer,
  2026-07-18): **20000 HP / 60 Speed / 5.0 Range /
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
4. **LAW (maintainer): renaming a base weapon ALWAYS renames its
   upgraded variant with it** — the scoutsman now pairs
   japan_imperialscoutsman_rifle + japan_imperialscoutsman_rifle_waveforce
   (the orphaned CHGuardRifleWaveforce was retired). The upgrade
   variant's STATS still ride the upgrade pricing pass; only the
   naming/pairing moves with the base.
5. Sight 8000 is inherited and NOT part of the anchor spec — left
   alone; decide later whether sight joins the formula.

Next unit: maintainer picks (suggest ra2_soviets_conscript — closest
remaining to baseline ratios), each conversion appends a log entry here.
## CLASS REBALANCE PROPOSAL v1 (2026-07-18 — REVIEW PENDING, nothing applied)

Anchor: 20000 HP / 60 Spd / 5000 rng / 4000 dmg / 50 rl, SmallArms-only
(WC 0.75, eff DPS 60) = Cost 100 with O=P=Q=100. Laws applied: bands
(<=150 SA/0.75; <=200 SA+CG/0.875; ceiling 200 = RA2 GI), cost 10s,
HP 1000s (self-heal HP/1000), damage 2000s, % warhead 1% per 2000,
burst kept as flavor with a unit-named FirepowerMultiplier so the
effective DPS hits the formula target (the maintainer's 33% example).

| unit | HP→2x | spd | rng→prop | cost | band | dmg step | burst | rl | FP-mult | eff DPS target | note |
|---|---|---|---|---|---|---|---|---|---|---|---|
| `alien.nax` | 15000 | 40 | | 150 | | | | | | | **special civilian variant — manual** |
| `asianalliance_asianmilitia` | 12000→24000 | 52 | 4567→4500 | 100 | SA/0.75 | 6000 | 1 | 50 | 71% | 64 |  |
| `conehead2.nax` | 40000 | 90 | | 500 | | | | | | | **civilian/special (500) — not a buildable scout roster unit** |
| `forgotten_mutant` | 18000→36000 | 65 | 3132→3250 | 120 | SA/0.75 | 2000 | 2 | 18 | 38% | 57 | burst kept as flavor |
| `forgotten_mutantsoldier` | 30000 | 75 | | 250 | | | | | | | **250 > ceiling — cap 200 (band 2) or reclassify** |
| `futuretech_spyfutu` | 5000 | 60 | | 1000 | | | | | | | **SPY — utility pricing** |
| `ixian_lightinfantry` | 18000→36000 | 52 | 4448→4500 | 150 | SA/0.75 | 4000 | 1 | 20 | 54% | 81 |  |
| `latinsyndicate_latinmilitia` | 13000→26000 | 52 | 4375→4500 | 130 | SA/0.75 | 2000 | 3 | 22 | 60% | 90 | burst kept as flavor |
| `light_inf` | 18000→36000 | 52 | 4448→4500 | 150 | SA/0.75 | 4000 | 1 | 20 | 54% | 81 |  |
| `naxis_coneheadsknights` | 20000 | 90 | | 1000 | | | | | | | **melee elite (1000) — melee/heavy class** |
| `naxis_naxiriflerecruit` | 10000→20000 | 45 | 5501→5500 | 75 | SA/0.75 | 8000 | 1 | 100 | 81% | 48 |  |
| `naxis_naxiriflesoldier` | 15000→30000 | 50 | 5621→5500 | 100 | SA/0.75 | 6000 | 1 | 75 | 70% | 42 |  |
| `naxis_slaveoverseer` | 20000 | 90 | | 500 | | | | | | | **economy/support — not a rifle scout** |
| `ordos_lightinfantry` | 18000→36000 | 52 | 4448→4500 | 150 | SA/0.75 | 4000 | 1 | 20 | 54% | 81 |  |
| `ra1_allies_raspy` | 5000 | 60 | | 500 | | | | | | | **SPY — utility pricing** |
| `ra1_soviets_ak47conscript` | 22000→44000 | 71 | 4420→4500 | 200 | SA+CG/0.875 | 2000 | 3 | 11 | 20% | 72 | burst kept as flavor |
| `ra2_allies_gi` | 25000→50000 | 50 | 3854→3750 | 200 | SA+CG/0.875 | 2000 | 3 | 15 | 39% | 108 | burst kept as flavor |
| `ra2_allies_ra2spy` | 5000 | 60 | | 500 | | | | | | | **SPY — utility pricing** |
| `ra2_soviets_conscript` | 13000→26000 | 57 | 4434→4500 | 100 | SA/0.75 | 2000 | 1 | 18 | 63% | 53 |  |
| `schwarzermond_lunarsoldier` | 12000→24000 | 60 | 4097→4000 | 120 | SA/0.75 | 6000 | 1 | 50 | 93% | 84 |  |
| `tkm_marine` | 20000→40000 | 60 | 5385→5500 | 300 | SA+CG/0.875 | 2000 | 5 | 16 | 32% | 139 | burst kept as flavor |
| `tkm_rifleman` | 16000→32000 | 60 | 5753→5750 | 120 | SA/0.75 | 6000 | 1 | 75 | 70% | 42 |  |
| `tkm_trooper` | 16000→32000 | 60 | 5594→5500 | 200 | SA+CG/0.875 | 2000 | 5 | 31 | 40% | 100 | burst kept as flavor |
| `undead.nax` | 15000 | 50 | | 100 | | | | | | | **special civilian variant — manual** |
| `yuri_clone` | 5000 | 50 | | 500 | | | | | | | **clone/utility (500) — manual** |
| `zerg_defiler` | 80000 | 50 | | 1400 | | | | | | | **caster-tank (1400) — not a scout** |
| `zerg_spithid` | 40000 | 110 | | 300 | | | | | | | **300 > ceiling — heavy class or price cut** |

_15 units fully solved; reclassify/manual rows need a maintainer call first._

### 2–5. The classic rifles (2026-07-19) — four unique characters at the original cost 100

LAW recorded: original C&C factions keep their original prices for
memorability (custom factions may deviate). LAW recorded: every unit
stays within ±10% of its class baseline range (scouts 4500–5500);
lower edge = cheapest, upper edge = most expensive in the class.

| unit | HP | Spd | Rng | weapon family (paired per the rename law) | burst | FP-mult | price |
|---|---|---|---|---|---|---|---|
| td_gdi_minigunner | 32000 | 60 | 4750 | td_gdi_minigunner_minigun (+_ap) | 4 | 32% | 100.6 |
| td_nod_minigunner | 30000 | 65 | 4500 | td_nod_minigunner_minigun (+_laser) | 4 | 28% | 100.0 |
| ra1_allies_rifleinfantry | 28000 | 60 | 5250 | ra1_allies_rifleinfantry_carbine (+_cryo) | 3 | 50% | 100.5 |
| ra1_soviets_rifleinfantry | 34000 | 55 | 4600 | ra1_soviets_rifleinfantry_carbine (+_incendiary) | 3 | 54% | 100.0 |

Characters: GDI disciplined standard; Nod fast and light with the
shortest reach (low band edge); Allies accurate with the longest scout
rifle (high band edge, most expensive feel); Soviets tankiest and
slowest. All four had the hidden ScoutInfantryBuff knobs (50% damage
taken / 110% firepower) — neutralized per-unit, 2x-health bake applied.

Lessons added:
6. Detect the SELF-HEAL trait by exact tag (`ChangesHealth@SelfHealing`)
   — resolved actors carry a dozen conditional ChangesHealth traits
   (propaganda auras, hospital, poison); overriding the first match
   corrupts an aura (caught in-session before commit).
7. Old shared weapons (M16, M1Carbine) STAY for their other users;
   only this unit's armaments repoint — the upgrade weapons of OTHER
   users (M16AP/M16Laser/Cryo/Incendiary) also stay untouched.

### Corrections + the VERIFICATION UNIT (2026-07-19, second pass)

Maintainer rulings applied:
- **Rifles out-range minigunners; minigunners out-run rifles**:
  GDI 4600/spd 63, Nod 4500/spd 66 (miniguns) vs Allies 5400/spd 57,
  Soviets 5100/spd 54 (rifles). FP-mults recomputed: 30/27/51/50.
- **Infantry speeds are FREE values** (the 5-step law is vehicles-only,
  from turn rate = speed/5); infantry turn instantly.
- **Stat variance bands (provisional)**: Range ±10% (hard), Speed ±20%,
  HP/damage/reload free (formula-constrained). Maintainer will tune.
- **Price envelope: 50%–250% of the class baseline** for every
  template (supersedes the earlier scout ceiling 200; RA2 GI stays the
  priciest STANDARD scout, the verifier sits at the envelope top).
- **THE VERIFICATION-UNIT LAW**: every class carries one unit at
  exactly 2x HP + 2x damage, same range/speed, 250% cost — proving
  O=1.5x, P=2.0x, Q=4.0x, price=2.5x. Scout verifier:
  `forgotten_mutantsoldier` (40000 HP / 60 spd / 5000 rng /
  8000 dmg @ 50 rl SmallArms / cost 250) — verified EXACT: price
  250.0000. (Un-flagged from the reclassify list; its speed 75->60 and
  weapon rework are the verification duty.)

Lesson 8: inserting stat overrides at the top of a block is not enough
— if the block already defines the trait later, the later value wins.
Edit the existing lines (the verifier's Speed-75 trio initially
shadowed the verification values; caught by the identity check).
