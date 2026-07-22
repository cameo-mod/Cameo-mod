# MEGAPLAN — rebalance program index

_This is the non-binding index for balance-program sequencing. Canonical laws remain in the linked documents; active work remains in `ROADMAP.md`._

## 0. The goal

Rebalance EVERY unit in the game onto the per-class Formula v2 system,
mechanically enforced by the balance pipeline so no agent can drift it.
Two intertwined programs run together: the **class rebalance** (units)
and the **weapon-template refactor** (damage profiles). Neither is done
by hand — both flow through the ledger → workbook → gated apply loop.

## 1. The three pillars (existing docs — do not duplicate, extend)

| doc | what it is |
|---|---|
| **BALANCE_PIPELINE.md** | the yaml⇄JSON-ledger⇄workbook machinery + gated write-back + drift audit. The HOW. |
| **FORMULA_V2.md** | the per-class formula law book: O=P=Q=cost construction, King-Tiger 2.5× identity, stat bands, the infantry class ladder, all standing laws. The RULES. |
| **docs/balance/formula_v2_<class>.md** | per-class conversion logs (binding lessons; scout + closecombat live). The RECORD. |

The generated workbook now carries a **WeaponTypes** column (the
resolved ^-class templates per weapon — armor profile + effects) so a
unit's weapon behaviour is visible at a glance.

Supporting: ROADMAP.md (work queue), DESIGN.md (§12 formula origin),
docs/balance/class_anchors.json (the anchor registry).

## 2. Class rebalance program

The authoritative class taxonomy, range bands, anchors, status labels, and conversion law are in [FORMULA_V2.md](FORMULA_V2.md). Each active class has one conversion record at `docs/balance/formula_v2_<class>.md`; active batches and maintainer decisions belong in [ROADMAP.md](ROADMAP.md).

Program order:

1. Complete active infantry conversions through their dedicated class logs.
2. Define the next infantry class only after its template, baseline, verifier, ledger fields, and targeted audit are ready.
3. Begin vehicles only after the infantry program completes; then aircraft and defenses follow the same template → baseline → verifier → one-at-a-time conversion loop.

## 3. Weapon-template refactor

The weapon-template program follows the class work and is its own migration batch. Its canonical armor profile, step law, two explosion families, and migration mapping are in [ARMOR_SYSTEM.md](ARMOR_SYSTEM.md). Formula pricing and pair-rename requirements are in [FORMULA_V2.md](FORMULA_V2.md); active implementation work is tracked in [ROADMAP.md](ROADMAP.md).

Templates are generated from the canonical profile and level inputs, then resolver-diffed and boot-gated. Do not hand-author a replacement Versus table or start the bulk rename before the program has explicit roadmap approval.

## 4. Current program rules

- New and changed balance laws belong only in [FORMULA_V2.md](FORMULA_V2.md) or [ARMOR_SYSTEM.md](ARMOR_SYSTEM.md).
- Pipeline behavior and the exact scripts belong only in [BALANCE_PIPELINE.md](BALANCE_PIPELINE.md).
- Scoped ownership, current blockers, and implementation evidence belong only in [ROADMAP.md](ROADMAP.md).
- Audit exceptions, quick fixes, and one-off maintainer clarifications (e.g. `MinRange` exceptions, which duplicate weapons to keep shared, `stat_formulas` approved/ deferred categories) belong only in [docs/LESSONS_LEARNED.md](../LESSONS_LEARNED.md).
- Use dedicated class logs for conversion verdicts and per-class lessons; do not grow this index into a second law book or task log.

## 5. Long-term product direction

The non-actionable Dynamic Campaign Mode vision, including its narrative, campaign structure, co-op concept, and future balance-test harness, lives in [VISION.md](VISION.md). It is intentionally separate from this rebalance program and from the active work queue.
