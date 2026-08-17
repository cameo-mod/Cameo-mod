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

**1. Harm, per cell (155 cells: 31 families × 5 platings)**

| | cells that INCREASE damage | worst case |
|---|--:|--:|
| the old AVERAGING world | 98 of 1152 | **×1.84 (+84%)** |
| MULTIPLYING mean-70 rows | **5 of 155** | **×1.06 (+6%)** |

The 5: `Arrow` × HAZMAT 106, `Concussion` × COMPOSITE 106, `Prism` × BLAST 103, `Bullet` ×
HAZMAT 102, `Toxic` × REFLECTOR 102.

⚠ These figures are POST-`e7fa2d57b` (the per-family uniqueness pass) and they replace an
earlier "13 of 100 at ×1.07" measured while four groups of families still shared a row. Both the
count and the worst case improved, for a structural reason worth keeping: giving each family a
second mechanism in another group pulls its extremes toward the middle of every column.

⚠ **So "mean 70" does not mean "never hurts".** The mean is a COLUMN property (across families);
the harm test is per CELL. The maintainer's premise is right in aggregate and wrong in detail —
but the detail turns out to be small.

**2. Spread compounding — the objection that killed multiplication in W20**

```
class armor rows span    65.3 .. 114.7  =  1.76:1
plating rows span        35.0 .. 106.0  =  3.03:1
MULTIPLIED               22.9 .. 121.6  =  5.32:1
SELECTED (plating only)                 =  3.03:1
```

⭐ **5.32:1 sits INSIDE the documented 2–8× target band** (DESIGN §12.0 rule 5). W20's disaster
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
2. **Harm is now ≤6% in 3% of matchups**, against +84% in the averaging world.
3. **The compounded spread lands in the target band** (5.32:1 vs the 2–8× design range).
4. **Both design axes stay simultaneously live** — the class ladder AND the plating cycle. Under
   selection they are mutually exclusive, so the cycle's rock-paper-scissors replaces the class
   ladder instead of layering on it.

## AGAINST multiplying

1. **5 cells still increase damage.** Real, but ≤6% — see the ruling below.
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
a worse outcome than an upgrade that is 6% unhelpful against one damage axis.

**And deliberately no clamp at 100.** Clamping would remove the 5 harm cells — but those cells
*are the cycle's weaknesses*. The closed cycle (thermo → kinetic → blast → energy → thermo) is
built so each plating is strong against one axis and **weak against the next**; a clamp deletes
half of that design and turns every plating into a strict upgrade, which is exactly the "free
upgrade" the cycle exists to avoid. A +6% penalty against your counter-weapon is a trade a player
can read and plan around.

## ✅ SHIPPED 2026-08-17 — and it was NOT a one-field flip

Measured before the change: **`MultiArmorCombination` is set NOWHERE in yaml** (0 occurrences), so
every warhead ran the default `Average`, and under `Average` the Cameo override did
`if (plating.Count > 0) return plating.Min();` — **the class armor was discarded outright**. That
is layer SELECTION, confirmed in code rather than assumed.

⚠ **Setting the field to `Multiply` would NOT have implemented this ruling.** That value
short-circuits to the engine's `DamageWarhead.DamageVersus`, which takes the product of **every**
matched armor — including two CLASS armors, i.e. W20's squaring bug (40% × 30% = 12%) — and it
also bypasses the `plating.Min()` protection. So the ruling needed a code change, in
`AreaDamageWarhead.DamageVersus`:

```csharp
var classRow = armor.Count == 0 ? 100 : /* MultiArmorCombination over CLASS armors only */;
return plating.Count > 0 ? classRow * plating.Min() / 100 : classRow;
```

`MultiArmorCombination` now governs the **class** armors only (still `Average`, so the dual-armor
cyborgs are untouched) and the plating layer always multiplies on top — the two rules in one field
that this document called for. No clamp, deliberately.

⭐ **Cheap moment to land it:** only **7 plating grants** exist across the whole roster today, all
conditional, so the law is set before the platings roll out rather than after. Boot-gated with the
rebuilt assembly (menu 21:47:41, no new exception log).

⚠ **But do not read "7 grants" as "no gameplay change" — for those seven it is large, and it is
the whole point.** A HAZMAT-suited infantryman (class armor `None`) under `^Warhead_Flame_Light`,
which reads `None: 200 / HAZMAT: 40`:

| | vs light flamer | what it means |
|---|--:|---|
| SELECTION (before) | **40** | the suit ERASED the unit's flammability class — 5× tougher than an unsuited rifleman, and identically tough to a plated tank |
| MULTIPLY (after) | **80** | still 2.5× better than unprotected, but infantry stays infantry |

That is the argument in one cell: a hazmat suit should make you resist fire, not stop being a
soft target. The same unit is now also correctly *worse* off wearing HAZMAT against kinetic fire,
which is what the closed cycle is for.

**What must still be true:**
* `audit_plating_exclusivity` X1 stays green (one active plating, always) — it is what keeps
  `plating.Min()` from being load-bearing.
* `armor_exposure.py` learns the plating distribution before prices are set (E1).
* Re-measure the harm cells after any composition change — they moved from 13 to 5 the first time
  the compositions were touched, so this is a live coupling, not a one-off check.
* ⚠ The ~878 legacy warhead nodes still declaring inline `Versus` on `SpreadDamage` do **not**
  route through `AreaDamage` and therefore keep the engine's blanket multiply. The layer rule
  reaches a weapon only once it is on a `^Warhead_*` template (A5 / W24).

---

## Appendix — where the matrix lives, and why rows no longer tie

*"why do they both have the exact same values for composite and armor? shouldn't they be unique?"*

⚠ **This appendix used to hold a copy of the matrix and an argument that the ties were
unavoidable. Both are superseded.** The full 31-family × 5-plating matrix is maintained in
**`PLATING_COMPOSITION_REFINEMENT.md`** — one owner, so the two documents cannot drift — and as of
`e7fa2d57b` **every emitted family has a distinct row** (`tools/tests/test_plating_composition.py`
pins it; `doc_claims.yaml` re-measures the count every audit run).

What survives from the old argument, because it is still true and still governs any future edit:

* **Five axes, but only FOUR distinguishable groups.** `HAZMAT` counters `{thermo}`, `COMPOSITE`
  counters `{kinetic, shaped}`, `BLAST` counters `{blast}`, `REFLECTOR` counters `{energy}` — so
  `kinetic` and `shaped` are read as one SET and refining *within* a group is arithmetically
  INERT. `Bullet` 0.90/0.10 against `Arrow` 0.65/0.35 was measured as byte-identical.
* ⭐ **A row earns its difference only by moving mass ACROSS a group boundary**, and the honest way
  to do that is to name a SECOND real defeat mechanism (spall behind the plate, a pyrophoric
  penetrator, a deflagrating fuel, an arc's thunderclap). That is how the four tie groups were
  broken without inventing a number.
* ⛔ **Never break a tie with ±1 noise.** The rows are derived from physics; a fabricated
  difference is a lie about the model (`b182fd228` — *"blend ladders were FABRICATED, not
  measured"*).
* **`ARMOR` is 70 for every family BY DESIGN** and must stay flat — it is the generic hedge that
  *"receives 100% damage from everything"*. It is the one column where a tie is the specification.
* **The no-ties ladder law (DESIGN §12.0 rule 2) governs values WITHIN one weapon's profile**, not
  a column across weapons. Those are different claims and only the first is a law.
