# The baseline-actor review — the binding spec

_Maintainer rulings, 2026-09-02, over three question rounds. This document is the contract for
re-selecting every class anchor and repricing every member. It supersedes nothing in
[`DESIGN.md`](../DESIGN.md); where it touches the band it RESTATES the ruled law rather than
replacing it._

⭐ **Read §1 first.** Most of what this review asks for was **already ruled and already built**.
The genuinely new decisions are in §2, and the one arithmetic contradiction they created — and how
it is resolved — is §3.

---

## 1. What was already law before this review

Checked before designing anything (CLAUDE.md rule 8f). The maintainer's description of the target
matched the shipped law **exactly**, term for term:

| stated in the review | already in `check_band.py` / `test_band_law.py` |
|---|---|
| baseline actor = the lower sweet point | `SWEET_LO = 1.000` — *"the anchor itself"*, *"anchor ON the floor"* |
| base band 1.0x .. 2.5x | `SWEET_LO 1.000 .. SWEET_HI 2.500` |
| expanded band 0.5x .. 3.5x | `FLOOR 0.500 .. CEIL 3.500` |
| 10–20% outliers | `target >= 80% occupancy` |
| a bell curve over the band | **THE BELL LAW** in `band_granularity.py` |

And the band is **derived, not preferred** (`BALANCE_PIPELINE` §8.1a). Holding speed and range at
the anchor's, `price(x, x) = (2x + 1)(x + 1) / 6`, so the rings are exact in both spaces:
`x = 0.50 -> 0.500`, `x = 1.00 -> 1.000`, `x = 2.00 -> 2.500`, `x = 2.50 -> 3.500`.

**Three measurements already existed and are the starting state of this work:**

* **`sigma_log = 0.869`** against the **0.3575** an 80% band implies — the roster is **2.4x too
  dispersed**. `band_granularity.py` calls this *"the one number that sizes the whole repricing job"*.
* **22% of members sit below their LIVE anchor** — essentially exactly the 20% the expanded band
  allots. Against the **spec** values it is 55%, which is the restat debt, not a roster problem.
* **The reference layer is built**: `REFERENCE_SYNTHESIS_REPORT.md` places 302 Cameo actors against
  10+ mods (Combined Arms, Crystallized Nexus, Generals Alpha, OpenHV, D2K, Dune II, RA, RA2/RV,
  OpenE2140) with lineage de-duplication, and the rifleman is already retired as the transfer key.

⚠ **One prior ruling this review OVERRIDES.** `band_granularity.py` says *"A class with more members
than rungs is NOT overcrowded — peers deliberately price several units alike; what matters is that
the units sharing a rung come from DIFFERENT factions."* §2.4 replaces that with per-unit
uniqueness. That is a deliberate divergence from peer practice, ruled with the arithmetic in §3 in
front of it.

---

## 2. The eleven rulings

### 2.1 Anchor = the cheapest member, maintainer-confirmed
The candidate anchor for a class is its **cheapest member**; I compute it and present it with stats
and reference comparison, and the maintainer approves or overrides **per class**. This keeps
`fit_class.py`'s existing "maintainer-picked anchor" contract intact.

⛔ **Not by formula price** — price is a ratio *to the anchor*, so picking the anchor by price is
circular.

### 2.2 Anchor stats = triangulate three sources
The anchor's own stats come from a **common middle ground** between:
1. the **live yaml** as it stands (and as it stands in the current release),
2. the class anchor **spec** in `anchor_decisions_log.md`,
3. the **reference consensus** from the other mods.

Reference data **proposes; the maintainer approves.** It is not binding and it is not merely
advisory.

### 2.3 Distribution = a bell with a mildly widened core
Lognormal in price, deliberately **less peaked** than a pure Gaussian, tails still falling off fast
at 1.0x and 2.5x — generalized-normal shape **beta = 3**, against beta = 2 for a pure bell. The
0.3575 sigma stays the dispersion target; the shape exponent is new and must be pinned.

### 2.4 Every unit in a class has a unique price
No two members of the same class may share a price. See §3 — this is what forced the grid design.

### 2.5 The price grid: atoms, coarse-first
Prices are multiples of an **atom**, filled **coarse-first**: place on the **50** grid; where that
collides, fall to **20**; where that still collides, fall to **10**. **10 credits is the floor —
nothing is ever finer.** Prefer the round value (2000 over 2050) whenever a slot is free.

* **Warcraft and StarCraft actors are always on the 20 grid**, and are **placed first**, before any
  other member of their class competes for a slot.
* **Cheap actors use the 5 grid** — scoped by **ContentPack and by price threshold** (mechanical,
  no hand-maintained class list, and it self-adjusts as prices move).

### 2.6 Spacing is a preference, atoms are the rule
Adjacent prices *should* sit at least **14.3%** apart — the perceptible step measured across 14
shipped mods (`cost_grid.py`, 266 adjacent-cost gaps). Where uniqueness and 14.3% conflict,
**uniqueness wins and the units pack tighter.**

### 2.7 Uniqueness escalation, and the last resort
Target band -> spill into the expanded band -> finer atom (50 -> 20 -> 10). **If every step fails,
shared prices are permitted** and must be reported. §3 shows this last resort is never reached.

### 2.8 Stat uniqueness: hard for ratios, soft for raw stats
**HP/cost and DPS/cost must be unique** within a class — they are continuous, so a collision is
always avoidable. **Raw speed and range may repeat**: two tanks moving at 85 is normal in every RTS,
and forcing 84 vs 85 invents a difference no player can feel.

### 2.9 Dispersion is decided per class, from the data
Not one global policy. Each class gets **split / compress / relax** on its own evidence — 3 classes
already fit the target band, 13 are repricing jobs, and `artillery_tank` does not fit even the hard
7.0x band, which `band_granularity.py` calls *"a SCOPE question: those members may not belong in one
class at all."*

### 2.10 Scope and rhythm: vehicles, one class at a time
The 11 vehicle classes first. **One class per round**, maintainer approves before the next starts,
so the format is corrected once rather than eleven times.

### 2.11 Re-anchoring unsigns the class
Any class whose anchor actor changes returns to `signed_off: false` and comes back for re-signing.
A signature covered a specific zero point; move the zero point and the signature no longer describes
what was approved.

⭐ **And that costs far less than it sounds** — measured, not assumed:

| the 8 signed classes | members |
|---|--:|
| `special_forces` | 15 |
| `missile_vehicle` | 13 |
| `archer`, `closecombat` | 4 each |
| `heavy_sniper` | 2 |
| **`flying_infantry`, `grenadier`, `mortar`** | **0 each** ⛔ |

**They cover 38 of 346 tagged members — 11% of the roster — and three of the eight govern no
members at all.** Unsigning is cheap; what it buys back is a zero point that means something.

---

## 3. ⛔ The contradiction, proved and resolved

Three of the rulings above cannot all hold at face value. Stated plainly so nobody re-derives it:

> **Unique price per unit + a flat step + the 1.0–2.5x band is arithmetically impossible.**

Slots in a band of width `W` at flat step `s` is `floor((W-1) * cost0 / s) + 1`. Measured:

| class | members | slots @50, target band | slots @50, FULL band |
|---|--:|--:|--:|
| `mbt` | 42 | **25** ⛔ | 49 |
| `line_breaker` | 30 | **25** ⛔ | 49 |
| `artillery` | 28 | **19** ⛔ | 37 |
| `scout_vehicle` | 28 | **10** ⛔ | **19** ⛔ |
| `special_forces` | 15 | **7** ⛔ | **13** ⛔ |
| `scout` | 6 | **4** ⛔ | 7 |

**6 of 17 classes fail in the target band; two fail even using the entire expanded band.**

⛔ **And it is sharper at the perceptible resolution.** At the measured 14.3% step the target band
holds **6.9 distinct prices in total**, so **11 of 17 classes** are impossible — which is exactly why
`band_granularity.py` had ruled that peers share prices deliberately.

### ⭐ The resolution: the coarse-first mixed grid (§2.5) fits everything

Because the grid is **mixed** rather than flat — most units on 50s, collisions falling to 20s and
only then to 10s — the capacity is set by the **finest** atom, not the coarsest. Verified over every
class:

| class | members | slots @50 | @20 | @10 | finest atom needed | spill needed? |
|---|--:|--:|--:|--:|---|---|
| `mbt` | 42 | 25 | 61 | 121 | **20** | no |
| `line_breaker` | 30 | 25 | 61 | 121 | **20** | no |
| `artillery` | 28 | 19 | 46 | 91 | **20** | no |
| `scout_vehicle` | 28 | 10 | 23 | 46 | **20** | no |
| `special_forces` | 15 | 7 | 16 | 31 | **20** | no |
| `scout` | 6 | 4 | 8 | 16 | **20** | no |
| every other class | — | — | — | — | **50** | no |

⭐ **Every class fits inside the TARGET band. No class spills into the expanded band to satisfy
uniqueness, no class reaches the 10 atom, and the shared-price last resort of §2.7 is never used.**
The finest atom the whole roster needs is **20** — which is also the atom Warcraft and StarCraft
already require, so the two rules agree instead of competing.

⚠ **What is genuinely given up, stated honestly:** in the tight classes adjacent prices sit **2.5%
to 6.7%** apart, against the 14.3% a player can perceive. Those distinctions are real in the ledger
and invisible in play. That is the accepted cost of per-unit unique pricing, ruled with this table
in view (§2.6).

---

## 4. ⛔ CORRECTED — there are no orphan classes. The taxonomy lives in yaml.

⛔ **An earlier version of this section listed five classes as having "zero members". That was
wrong, and it was wrong three times over** — see `LESSONS_LEARNED.md` → *"Three ways I measured
zero"*. The decisive correction came from the maintainer: *"Check the unit templates defined in the
defaults yaml and you can find each one of them."*

⭐ **CLASS MEMBERSHIP IS `Inherits@Template:` — a KEYED inherit, in the actor's own yaml.**

```
td_gdi_grenadier:
    Inherits: ^Soldier
    Inherits@Template: ^GrenadierInfantryTemplate      <-- the class
```

A traversal that follows only the bare `Inherits:` sees none of it. The five "orphans" are fully
populated, including every unit the maintainer named:

| class | template | members | cheapest | current anchor | cheapest already? |
|---|---|--:|---|---|---|
| `grenadier` | `^GrenadierInfantryTemplate` | **7** | 200 `td_gdi_grenadier` | `td_gdi_grenadier` | ✅ (3-way tie at 200) |
| `mortar` | `^MortarInfantryTemplate` | **5** | 500 `forgotten_mutantmortarman` | `forgotten_mutantmortarman` | ✅ (3-way tie at 500) |
| `flying_infantry` | `^FlyingInfantryTemplate` | **11** | 270 `cabal_orbdrone_slave` | `ra2_allies_rocketeer` (600) | ⛔ no |
| `pure_sniper` | `^SniperInfantryTemplate` | **26** | 250 `naxis_naximercenarysniper` | `naxis_naximercenarysniper` | ✅ |
| `commando` | `^HeroInfantryTemplate` | **33** | 750 `forgotten_mutanthijacker` | `td_gdi_commando` (3000) | ⛔ no |

### 4.1 ⛔ The bigger finding: the ledger tags are a drifted copy of the taxonomy

Only **8 of 27** classes agree between `Inherits@Template:` and `design.class_anchor`:

| agree ✅ | drift ⛔ (structural − tagged) |
|---|---|
| `anti_air_vehicle`, `artillery_tank`, `closecombat`, `dreadnought`, `epic_vehicle`, `light_tank`, `missile_vehicle`, `tank_destroyer` | `heavy_infantry` **+48** · `support` **+47** · `rocket_trooper` **+43** · `melee` **+43** · `scout` **+34** · `commando` **+33** · `scout_vehicle` **+27** · `pure_sniper` **+26** · `mbt` **+21** · `flying_infantry` **+11** · `line_breaker` **+10** · `grenadier` **+7** · `fire_support` **+6** · `mortar` **+5** · `high_tech_tank` **+2** · `artillery` **+1** · `special_forces` **−11** · `archer` **−4** · `heavy_sniper` **−2** |

⚠ **This does NOT mean the template count is simply right.** Spot-checked on `mbt`, the 21 the
ledger omits are `EDEN_*`/`PLYMOUTH_*` imports, `*_backup` variants and `ra2_c_*` — plausibly
excluded on purpose. **Two sources, two scopes:** the template says what a unit structurally *is*;
the ledger says what the balance programme *prices*. Which one defines a class member is a
maintainer ruling, and it changes every count in §3.

⚠ **Two classes have no template at all** — `archer` (no `^ArcherTemplate` exists) and
`heavy_sniper` (`^HeavySniperInfantryTemplate` is declared at `defaults.yaml:1425` and **nothing
inherits it**) — yet both carry ledger tags and both are **signed**.

### 4.2 ⛔ Fourteen structural classes have no anchor at all

Whole branches of the roster sit outside the band programme:

| template | members | | template | members |
|---|--:|---|---|--:|
| `^HelicopterTemplate` | 67 | | `^ScoutShipTemplate` | 26 |
| `^BasicDefenseTemplate` | 46 | | `^FighterTemplate` | 23 |
| `^BomberTemplate` | 36 | | `^SpaceshipTemplate` | 21 |
| `^AdvancedDefenseTemplate` | 34 | | `^AntiAirDefenseTemplate` | 18 |
| `^HarvesterTemplate` | 30 | | `^ArtilleryShipTemplate` | 16 |

Air, naval and defences are entirely unanchored. Out of scope for the vehicle pass, but they are
why "27 classes" is not the same as "the roster".

### 4.3 Two anchor candidates that need judgment, not arithmetic

Exactly the case §2.1 keeps the maintainer in the loop for:

* **`flying_infantry` → `cabal_orbdrone_slave` (270).** A **slave** unit — spawned, not built.
  Anchoring a class on something a player cannot buy is almost certainly wrong.
* **`commando` → `forgotten_mutanthijacker` (750)** against a class priced 3,000–10,000. Anchoring
  there puts the rest of the class at 4x–13x and blows the band open by itself.
* ⚠ **And `pure_sniper` is not one class.** `^SniperInfantryTemplate` currently carries four
  **engineers** (`TSENGINEER`, `forgotten_engineer`, `ts_gdi_engineer`, `ts_nod_engineer`) plus
  mages, priests and sorceresses. That is a §2.9 scope question before it is a pricing question.

## 4.4 ⭐ RULED: one class template per buildable unit — and it is now audited

**Maintainer, 2026-09-02:** *"Every buildable actor must have an inherited template so the unit can
be classified. And this is also what you must use to balance the units around for each class in the
unit class balancing pipeline. If a buildable unit doesn't have an inherited template it's wrong and
a real defect, and if it inherits multiple templates that's also a real defect — with the only
exception being the epic vehicle and epic aircraft, which is like an add-on template but not a full
template by itself."*

⭐ **This settles §4.1's scope question: the TEMPLATE is the class, the ledger tag is not.**
`tools/audit/audit_class_templates.py` (new, in `run_all.sh`'s blocking loop) enforces it.

**Measured on the current tree:**

| | |
|---|--:|
| buildable actors | 2,166 |
| — upgrades and promotions (not units) | 632 |
| — buildings (scope question, §4.5) | 556 |
| **units the law reaches** | **978** |
| ✅ exactly one class template | **869 (89%)** |
| ⛔ no class template | **67** |
| ⛔ more than one | **18** |
| ⛔ add-on only, no base class | **24** |

### ⛔ CORRECTED: 6 multi-template defects, not 18

**Maintainer, 2026-09-02:** *"Unarmed transport helicopter already inherits the helicopter template
right? Same thing with the dog template and the melee template?"* — **yes, and that made 12 of my
18 "defects" my own bug.**

```
^UnarmedTransportHelicopterTemplate:
    Inherits@Template: ^HelicopterTemplate        <-- a SUB-template

td_gdi_chinooktransport:
    Inherits: ^Helicopter                          <-- a different template entirely
    Inherits@Template: ^UnarmedTransportHelicopterTemplate   <-- names ONE class, correctly
```

The chinook is right. My audit counted its ancestry **transitively**, so the sub-template plus its
own parent read as two classes. **Only the most specific template counts**: any template that
another kept template inherits is dropped before counting. Fixed, with three regression tests.

**The six that survive are real:**

| pair | actor | cost |
|---|---|--:|
| `^HeavyInfantryTemplate` + `^HeroInfantryTemplate` | `cabal_cyborgcommando` | 5,000 |
| | `cabal_cyborgcommandov2` | 10,000 |
| `^HeavyInfantryTemplate` + `^SniperInfantryTemplate` | ⛔ `japan_archermaiden` — the signed `archer` anchor | 500 |
| `^ScoutInfantryTemplate` + `^SpecialForcesInfantryTemplate` | ⛔ `japan_imperialscoutsman` — the signed `special_forces` anchor | 200 |
| `^ScoutInfantryTemplate` + `^SniperInfantryTemplate` | `forgotten_mutantsniper` | 650 |
| `^HarvesterTemplate` + `^MeleeInfantryTemplate` | `wc2_humans_militiapeasant` | 300 |

⭐ Two of the six are signed class anchors, and `japan_archermaiden` is resolved by class 1.

⚠ **The corrected headline: 881 of 978 units (90%) already carry exactly one class template.**

### ⛔ Five DEAD class templates

Declared in `defaults.yaml` and inherited by **nothing**:

| template | a class anchor points at it? |
|---|---|
| `^ArcherInfantryTemplate` | **`archer` — and it is SIGNED** |
| `^HeavySniperInfantryTemplate` | **`heavy_sniper` — and it is SIGNED** |
| `^RocketTrooperInfantryTemplate` | `rocket_trooper` |
| `^SuperDefenseTemplate` | no |
| `^SupportInfantryTemplate` | no |

⭐ **This is why `archer` and `heavy_sniper` looked strange in §4.1**: they are the two classes whose
member count went *down* against the ledger. Structurally they have **no members at all** — their
templates are dead, and `japan_archermaiden` (the `archer` anchor) inherits Heavy Infantry + Sniper
instead of `^ArcherInfantryTemplate`.

## 4.5 ⚠ Open: do buildings need a class?

556 buildable buildings are out of scope above. Defence buildings **do** carry templates
(`^BasicDefenseTemplate`, `^AdvancedDefenseTemplate`, `^BunkerTemplate`); production buildings carry
none. Whether a barracks or a refinery must be classifiable is a maintainer ruling — the audit lists
them rather than failing on them.

Per-class proposals live in [`CLASS_MOVES.md`](CLASS_MOVES.md), one section per
class, in the order they are done.

## 5. Order of work

1. ⛔ **Maintainer rules on the SCOPE question** (§4.1): does a class member mean *inherits the
   template* or *is tagged in the ledger*? Every count in §3 depends on it — `mbt` is 42 or 63,
   `scout_vehicle` 28 or 55 — and so does whether the grid still fits (§3 was computed on the
   ledger counts).
2. **`mbt` end to end** — candidate anchor, triangulated stats, reference comparison, full member
   repricing on the coarse-first grid, distribution fit. Maintainer approves the FORMAT here.
3. The remaining ten vehicle classes, one per round.
4. Infantry and the rest, once the vehicle pass is signed.

⚠ Every yaml write in this programme goes through the pipeline: propose -> ledger -> review ->
`apply_balance --confirm` **on a maintainer order** -> re-extract -> `audit_balance_drift` ->
audit suite -> **boot gate**. Nothing here authorises a hand edit (CLAUDE.md rule 3).
