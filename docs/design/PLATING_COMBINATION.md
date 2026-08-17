# Should platings MULTIPLY with the class armor, or REPLACE it?

**Maintainer 2026-08-17:** *"now that you have made it 70% on average I think we can go back to
multiplying the damage values with the underlying armor from the HP? Can you find good reasons for
and against it first before you do anything?"*

Nothing has been changed. This is the analysis, measured.

Under multiplication, `effective = class_row × plating_row / 100`. So **a plating helps iff its
row is below 100 and hurts iff it is above** — the class row cancels out of that test, which makes
the question exactly answerable rather than a matter of taste.

---

## The measurements

**1. Harm, per cell (100 cells: 20 families × 5 platings)**

| | cells that INCREASE damage | worst case |
|---|--:|--:|
| the old AVERAGING world | 98 of 1152 | **×1.84 (+84%)** |
| MULTIPLYING mean-70 rows | **13 of 100** | **×1.07 (+7%)** |

The 13: `Demolition`/`Concussion` × COMPOSITE 107; `Bullet`/`CannonAP`/`Melee`/`Arrow` × HAZMAT
105; `Flame`/`Chemical`/`Toxic` × REFLECTOR 105; `Laser`/`Prism`/`Tesla` × BLAST 104;
`MissileAP` × HAZMAT 101.

⚠ **So "mean 70" does not mean "never hurts".** The mean is a COLUMN property (across families);
the harm test is per CELL. The maintainer's premise is right in aggregate and wrong in detail —
but the detail turns out to be small.

**2. Spread compounding — the objection that killed multiplication in W20**

```
class armor rows span    65.3 .. 114.7  =  1.76:1
plating rows span        35.0 .. 107.0  =  3.06:1
MULTIPLIED               22.8 .. 122.7  =  5.37:1
SELECTED (plating only)                 =  3.06:1
```

⭐ **5.37:1 sits INSIDE the documented 2–8× target band** (DESIGN §12.0 rule 5). W20's disaster
was `40% × 30% = 12%`, turning a 17:1 weapon into ~289:1 — but that was **two full ladders**
multiplied. A plating row is a SHALLOW modifier (35–107, mean 70), not a second ladder. **The
original objection does not apply to this case**, and that is the substantive change since W21.

---

## FOR multiplying

1. ⭐ **Selection ERASES the class armor, and that is a bigger loss than it sounds.** A plated
   Heroic unit stops being Heroic: only the plating row is read, so the unit-class ladder — the
   entire reason the armor system exists — is switched OFF by installing an upgrade. A Superheavy
   tank and a Scout car with the same plating take identical damage. Multiplication keeps both
   dimensions live.
2. **Harm is now ≤7% in 13% of matchups**, against +84% in the averaging world.
3. **The compounded spread lands in the target band** (5.37:1 vs the 2–8× design range).
4. **Both design axes stay simultaneously live** — the class ladder AND the plating cycle. Under
   selection they are mutually exclusive, so the cycle's rock-paper-scissors replaces the class
   ladder instead of layering on it.

## AGAINST multiplying

1. **13 cells still increase damage.** Real, but ≤7% — see the ruling below.
2. ⚠ **The two factors are CORRELATED, not independent, and multiplication assumes independence.**
   Both the class ladder and the plating row are projections of the **same** `COMPOSITION`: a
   thermal weapon is anti-infantry-sharp in its class ladder *and* countered by HAZMAT. Multiplying
   applies the weapon's identity **twice** to a plated unit. This is the strongest objection and it
   is conceptual rather than numeric — the numbers above stay in band, but they are in band for a
   reason that is partly luck.
3. **It reintroduces a multiply path.** Safe only while a unit can wear exactly ONE plating —
   `audit_plating_exclusivity` (X1) is what holds that, and it must stay green.
4. **The pricing model prices ROWS, not products.** `armor_exposure.py` and `K` read a weapon's
   Versus row; with multiplication a plated unit's effective armor is a product, so exposure needs
   to know about the plating distribution.

---

## Recommendation — multiply, and do NOT clamp

Multiplication, because argument 1 is decisive: an upgrade that deletes a unit's class identity is
a worse outcome than an upgrade that is 7% unhelpful against one damage axis.

**And deliberately no clamp at 100.** Clamping would remove the 13 harm cells — but those cells
*are the cycle's weaknesses*. The closed cycle (thermo → kinetic → blast → energy → thermo) is
built so each plating is strong against one axis and **weak against the next**; a clamp deletes
half of that design and turns every plating into a strict upgrade, which is exactly the "free
upgrade" the cycle exists to avoid. A +7% penalty against your counter-weapon is a trade a player
can read and plan around.

**What must be true for this to ship:**
* `audit_plating_exclusivity` X1 stays green (one active plating, always).
* `MultiArmorCombination` returns to `Multiply` **for platings only** — the legacy dual-armor
  cyborgs/droids must stay on `Average`, or they resume squaring (that is W20's actual bug and it
  has nothing to do with platings).
* `armor_exposure.py` learns the plating distribution before prices are set.
* Re-measure the 13 cells after any composition change.

---

## Appendix — the full family × plating matrix, and why rows TIE

*"why do they both have the exact same values for composite and armor? shouldn't they be unique?"*

| family | HAZMAT | COMPOSITE | BLAST | REFLECTOR | ARMOR | composition |
|---|--:|--:|--:|--:|--:|---|
| Arrow | 105 | 36 | 69 | 70 | 70 | kinetic 1.00 |
| Bullet | 105 | 36 | 69 | 70 | 70 | kinetic 1.00 |
| Melee | 105 | 36 | 69 | 70 | 70 | kinetic 1.00 |
| CannonAP | 105 | 36 | 69 | 70 | 70 | kinetic 0.75, shaped 0.25 |
| MissileAP | 101 | 43 | 66 | 70 | 70 | shaped 0.90, blast 0.10 |
| Railgun | 99 | 41 | 74 | 64 | 70 | kinetic 0.85, energy 0.15 |
| Flak | 91 | 64 | 55 | 70 | 70 | kinetic 0.60, blast 0.40 |
| MissileAA | 89 | 68 | 54 | 70 | 70 | kinetic 0.55, blast 0.45 |
| MissileHE | 79 | 89 | 43 | 70 | 70 | shaped 0.25, blast 0.75 |
| CannonHE | 73 | 100 | 38 | 70 | 70 | kinetic 0.10, blast 0.90 |
| Concussion | 70 | 107 | 35 | 70 | 70 | blast 1.00 |
| Demolition | 70 | 107 | 35 | 70 | 70 | blast 1.00 |
| Sonic | 70 | 96 | 55 | 59 | 70 | blast 0.70, energy 0.30 |
| Laser | 70 | 71 | 104 | 35 | 70 | energy 1.00 |
| Prism | 70 | 71 | 104 | 35 | 70 | energy 1.00 |
| Tesla | 70 | 71 | 104 | 35 | 70 | energy 1.00 |
| Magic | 63 | 78 | 83 | 56 | 70 | thermo 0.20, blast 0.20, energy 0.60 |
| Chemical | 35 | 71 | 69 | 105 | 70 | thermo 1.00 |
| Flame | 35 | 71 | 69 | 105 | 70 | thermo 1.00 |
| Toxic | 35 | 71 | 69 | 105 | 70 | thermo 1.00 |
| **Inferno** | **54** | 71 | 88 | **67** | 70 | thermo 0.46, energy 0.54 |

**Why the ties, and why they are honest:**

* **`ARMOR` is 70 for every family BY DESIGN** — it is the generic hedge that *"receives 100%
  damage from everything"*, so it must be flat. Varying it would contradict its purpose.
* **The others tie because the COMPOSITIONS are identical.** `Laser`/`Prism`/`Tesla` are all
  `energy 1.00`; `Chemical`/`Flame`/`Toxic` are all `thermo 1.00`. The rows are *derived*, so an
  identical composition must give an identical row — the tie is the model reporting truthfully that
  a composite plating cannot tell a bullet from an arrow.
* ⚠ **This is a PIGEONHOLE limit, not an oversight: 5 axes cannot separate 20 families.** Making
  every row unique needs one of:
  1. **more axes** — `Chemical` is CORROSION and `Toxic` is a biological agent, but neither
     corrosion nor toxicity is one of the five, so both read as pure `thermo`;
  2. **finer shares within an axis** — a sword, an arrow and a rifle bullet are all "kinetic" but
     differ in velocity and contact area, so they could split kinetic/shaped differently;
  3. **accepting derived ties.**
* ⛔ **What must NOT be done is adding ±1 noise to break ties.** The rows are derived from physics;
  a fabricated difference is a lie about the model, and this project has already been burned by
  exactly that (`b182fd228` — *"blend ladders were FABRICATED, not measured"*). The no-ties ladder
  law (DESIGN §12.0 rule 2) governs values **within one weapon's profile**, where Inferno's
  54/71/88/67/70 are all distinct — it has never governed a column across weapons.

**If uniqueness is wanted, option 2 is the honest route** and it is real design work: give each
kinetic family a distinct kinetic/shaped split reflecting velocity and contact area, and each
thermochemical family a distinct thermo share. That changes compositions, which regenerates every
column (the mean-pinning couples them all), so it is its own pass with its own boot gate.
