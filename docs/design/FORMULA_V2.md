# FORMULA V2 — the complete law book (as learned through 2026-07-19)

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
- **Scout infantry never hit aircraft**: ValidTargets Ground, Water on
  every scout weapon INCLUDING upgrade variants; units use
  ^AutoTargetGroundAssaultMove (faction consistency program).

## 4. Weapon & template laws

- **Dedicated weapons**: a converted unit never shares its weapon —
  check sharing repo-wide FIRST (`Weapon: <name>`); shared originals
  stay for their other users (brik.shp lesson, weapon edition).
- **Pair-rename law** ("remembered at all times"): renaming/dedicating
  a base weapon ALWAYS renames its upgrade variants with it — the
  family moves together, orphans are retired.
- **Templates are law**: conyards always use the ^Conyard template
  Power (100); icons set Offset: 0,0 whenever their image's Defaults
  defines a nonzero offset (`audit_template_conformance` enforces both).
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

## 6b. The infantry class ladder (target state, 2026-07-19)

| class | range anchor (band) | baseline | status |
|---|---|---|---|
| melee | ~1500 (1350–1650) | TBD | future anchor |
| **closecombat** (shotgun/SMG) | **3500 (3150–3850)** | td_gdi_shotgunner @ 200 (proposed) | PROPOSAL: docs/balance/formula_v2_closecombat.md |
| scout | 5000 (4500–5500) | japan_imperialscoutsman @ 100 | LIVE (6 converted) |
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
