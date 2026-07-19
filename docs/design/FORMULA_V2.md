# FORMULA V2 — the complete law book (as learned through 2026-07-19)

_Master index: **MEGAPLAN.md** ties this + BALANCE_PIPELINE + the class
logs + the weapon-template program together._

_The consolidated, binding reference for the per-class balance system.
Grew out of DESIGN §12 + the balance pipeline (BALANCE_PIPELINE.md) +
the scout-class conversions (docs/balance/formula_v2_scout.md holds the
class log). Update THIS file whenever a law is added or tuned._

## 1. The construction (why O = P = Q = cost always holds)

Each class formula normalizes every stat against the class BASELINE
unit (h₀ = HP, s₀ = Speed, r₀ = Range in wdist, d₀ = effective DPS,
C₀ = cost). With ratios h,s,r,d (and r carrying the Special factor K):

    O = (h + s + r + d) · C₀/4 · Tier
    P = (h·s + r·d)      · C₀/2 · Tier
    Q = (h·s·r·d)        · C₀   · Tier
    price = (O + P + Q) / 3

- At the baseline every ratio is 1 → **O = P = Q = price = C₀ exactly**
  (the maintainer's rule; must hold for EVERY baseline unit).
- The legacy Tiger global formula IS this construction with
  (100000, 100, 5000, 200, 800) plugged in — its "magic constants"
  (25000, 12.5e6, ×200) all derive from the anchor.
- **The King-Tiger identity**: at 2×HP + 2×damage, same range/speed →
  O = 1.5×, P = 2.0×, Q = 4.0×, price = 2.5×. ALWAYS, for any class.
- Price is LINEAR in each single stat → closed-form solvers (the
  workbook's Range-from-Cost column) survive every class variant.
- Code: `tools/balance/formula.py::class_baseline_estimators/price`.
  Registry: `docs/balance/class_anchors.json`.

## 2. Baselines & verifiers (fixed points at both envelope ends)

- Every class gets a **living baseline unit in game** (round stats,
  price = C₀ exactly) — the testable reference.
- Every class gets a **verification unit**: exactly 2×HP + 2×damage,
  same range/speed, 250% cost — price must compute 2.5×C₀ EXACTLY.
  It doubles as a formula tripwire (it caught a shadowed-override bug
  on day one).
- Established anchors:
  | class | baseline | stats | verifier |
  |---|---|---|---|
  | mbt | Naxis Tiger Tank | 100000/100/5000/10000@50 → 800 | King Tiger (2×/2× @ 2000) |
  | scout | japan_imperialscoutsman | 20000/60/5000/4000@50 SA → 100 | forgotten_mutantsoldier (40000/60/5000/8000@50 → 250.0000 exact) |

## 3. Stat laws (all classes unless stated)

- **Price**: 10-credit steps; envelope **50%–250% of the class C₀**;
  classic C&C factions keep their ORIGINAL prices (memorability);
  custom factions (AsianAlliance, LatinSyndicate, …) may deviate.
- **Range**: ±10% of the class baseline (HARD band; scouts 4500–5500);
  low edge = cheapest units, high edge = priciest; **steps of 10**
  (tank shells use bullet speed = range/10; tank destroyers = 2× that).
- **Speed**: ±20% of baseline (provisional, maintainer will tune).
  Vehicles: steps of 5 (turn rate = speed/5). **Infantry: FREE values**
  (instant turn) — use the freedom for faction character.
- **HP**: infantry in 1000 steps; self-heal Step = HP/1000. (The
  2×-health bake replaced the ScoutInfantryBuff 50% damage reduction.)
- **Damage**: steps of 2000; every weapon carries a
  HealthPercentageDamage warhead at 1% per 2000 damage.
- **Burst is flavor, not power**: keep burst feel, trim effective DPS
  to the formula target with a **unit-named FirepowerMultiplier**
  (e.g. burst 3 → ~33%). Gatling/spinup units (soviet gatling tank,
  RA1 allied heavy AA tank) are special cases — handle individually.
- **Weapon-class bands by cost** (scout values; per-class analogues):
  ≤150% of C₀ → SmallArms only (WC 0.75); above → SmallArms+Chaingun
  (WC 0.875). The class's own DPS₀ already includes the baseline WC.
- **SoundVolume = 1/burst** (LAW): a BurstDelays-0 weapon firing N
  shots on one tick must set SoundVolume 1/N or it deafens.
- **Tech tier factor** multiplies O/P/Q: T1 = 1.0, T3 = 0.75
  (higher tech = cheaper per stat). From the deepest tech-building
  prerequisite. Closecombat verifier doubles DPS via 2x BURSTS.
- **Scout infantry never hit aircraft**: ValidTargets Ground, Water on
  every scout weapon INCLUDING upgrade variants; units use
  ^AutoTargetGroundAssaultMove (faction consistency program).

## 4. Weapon & template laws

- **Weapon Versus tables**: built by the step law in ARMOR_SYSTEM.md —
  LEVEL = step 6/5/4 (light/medium/heavy, floor 10/25/40, Shield
  110/125/140), PROFILE = the armor order. Generate, never hand-type.

- **Dedicated weapons**: a converted unit never shares its weapon —
  check sharing repo-wide FIRST (`Weapon: <name>`); shared originals
  stay for their other users (brik.shp lesson, weapon edition).
- **Pair-rename law** ("remembered at all times"): renaming/dedicating
  a base weapon ALWAYS renames its upgrade variants with it — the
  family moves together, orphans are retired.
- **Templates are law**: conyards always use the ^Conyard template
  Power (100); icons set Offset: 0,0 whenever their image's Defaults
  defines a nonzero offset (`audit_template_conformance` enforces both).
- **Every unit is UNIQUE within its class** (maintainer 2026-07-19):
  no two units in a class share identical HP / speed / damage /
  reload — give each faction's member its own small deviations
  (per the faction-personality guide §5), pricing each via the
  formula. EXCEPTION: original C&C units keep their ORIGINAL price
  (stats still vary; the price is pinned). This kills clone rows
  like the three identical D2k light infantry (light_inf /
  ixian_lightinfantry / ordos_lightinfantry): Ordos = cheaper/
  faster/weaker, Ixian = pricier/slower/harder-hitting.
- **Descriptions carry NO `
`**: unit/weapon descriptions live in the
  fluent files (`fluent/**/en.ftl`) with REAL line breaks, never the
  `
` escape. New descriptions go straight to fluent.
- **Knob hierarchy** (^GlobalBuffs → class → subclass) stays as the
  live one-value tuning layer, pipeline-owned, with knob-aware pricing;
  formula-gap patches get baked away per class (BALANCE_PIPELINE §5b).

## 5. Faction personality (differentiation guide)

Same-class units vary SLIGHTLY per faction, staying near their old
feel: e.g. minigunners faster + shorter-ranged than rifle soldiers
(TD GDI 4600/63 disciplined, TD Nod 4500/66 light & fast) vs rifles
longer-ranged + slower (Allies 5400/57 accurate, Soviets 5100/54
tanky). Larger personalities for custom factions: Ordos = weak, cheap,
fast; Ixians = expensive, slow, high firepower/range/attack speed.

## 6. The conversion process (one unit at a time, learn each time)

1. Read the class conversion log (`docs/balance/formula_v2_<class>.md`)
   — accumulated lessons are BINDING.
2. Check weapon sharing → dedicate the weapon family (pair law).
3. Apply stat laws (bands, steps, bake, no-air for scouts).
4. Neutralize class knobs per-unit (Modifier: 100) until the whole
   class is converted, then delete knobs + overrides in one sweep.
5. Scale ChangesHealth — EXACT tag `ChangesHealth@SelfHealing` (a
   resolved infantry carries a dozen conditional heal traits).
6. Edit EXISTING stat lines; a block's later same-trait definition
   shadows anything inserted above it.
7. Verify price via resolver + formula BEFORE boot (target ±2%).
8. Ledger sync (`extract_stats.py`, design fields, `--check` green),
   boot gate, scoped commit, push, append the conversion-log entry.

## 6b. The infantry class ladder (target state, rev. 2026-07-19 late)

**CONTIGUOUS half-open range bands** (maintainer design): no unit can
ever fall between classes again — the band DEFINES membership.

| class | range band | anchor r₀ | baseline | status |
|---|---|---|---|---|
| melee | [1250, 2500) | 1750 | TBD | future anchor; range is SIZE-DERIVED (see below) |
| **closecombat** (shotgun/SMG) | **[2500, 4500)** | **3500** | td_gdi_shotgunner @ 200 | **LIVE** (baseline 200.00, verifier fanatic 500.00 exact, +naxis_sssoldier T3) |
| scout (rifles) | [4500, 5500] | 5000 | japan_imperialscoutsman @ 100 | LIVE (6 converted) |

- **Band width is a PER-CLASS property.** Scouts keep the tight ±10%
  (one weapon archetype: rifles). Melee and closecombat get WIDE
  contiguous bands because their ranges express different things:
- **Melee range is PHYSICS, not power**: contact reach follows unit
  size (small: shriek 1150 / zergling 1350 / zealot 1335; medium:
  footman 1333 / knight 1420 / worker 1500; large: dogs 2000; huge
  melee VEHICLES like the Consortium Megalodon belong to a melee-
  vehicle class, not infantry). Size→reach convention: small
  1250–1400, medium 1400–1700, large 1700–2500. Sub-1250 outliers
  (zombiemutant 1127) round UP to 1250. In the MELEE formula the
  range ratio is FIXED at 1 (reach is coupled to hitbox size — bigger
  reach = bigger target — so it is not priced); melee pricing runs on
  HP/speed/DPS.
- **Closecombat range IS a balance lever** (2500 SMG spray → 4500
  long shotgun): it prices normally, and the wide band follows the
  price-gradient law — cheapest members at the low edge, priciest at
  the high edge (a cost axis, not free choice).
- Boundary rule: a weapon at exactly 2500 is closecombat; exactly
  4500 is scout (half-open bands).
| sniper | TBD (long) | TBD | zerg_defiler transforms in (maintainer verdict) |
| heavy | TBD (own survey) | TBD | future anchor (flame/chem units live here) |
| hero/commando | ~2000 attach/C4 | TBD | future anchor |
| support/special | n/a (ability-priced) | n/a | NEW class for spies + Yuri mind control + CABAL hackers (maintainer verdict); ability-value table to design |

Maintainer verdicts 2026-07-19: case-by-case for misfits — defiler →
sniper; spies/mind-control/hackers → support template; civilians
(alien/undead/conehead2.nax) parked undecided.

## 7. Open items

- Scout proposal (formula_v2_scout.md) awaits maintainer row verdicts;
  ranges clamp to the band at application.
- Speed ±20% band and other stat bands await maintainer tuning.
- Unconverted scout weapons (shared M16/M1Carbine users) get the
  no-air rule at their own conversion.
- Next classes: bomber (replace reload-250 convention), defense
  (replace speed-100), infantry sub-anchors, fighter port.
- MARS-type shrapnel-chain weapons need extractor coverage first.
