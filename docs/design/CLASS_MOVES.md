# Class moves — completing the 2026-07-21 template split, one class at a time

_Working document. One section per class, in the order they are done. Each section is a PROPOSAL
until the maintainer approves it; approved sections still need `apply_balance --confirm` where they
move a number, and a **boot gate** in every case (they edit `mods/**`)._

Governed by [`BASELINE_ACTOR_REVIEW.md`](BASELINE_ACTOR_REVIEW.md). Enforced by
`tools/audit/audit_class_templates.py`.

---

## 0. ⭐ Why five templates are dead: a split that was designed and never applied

`^RocketTrooperInfantryTemplate` documents it in its own comment:

> *"Formula v2 ROCKET TROOPER class (design 2026-07-21): dedicated missile-based anti-vehicle /
> anti-air infantry. **Splits from the old AntiTankAntiAir template, which mixed bullet-based AA
> infantry into special forces.**"*

`^ArcherInfantryTemplate` carries the same 2026-07-21 stamp. So the five dead templates —
`^ArcherInfantryTemplate`, `^HeavySniperInfantryTemplate`, `^RocketTrooperInfantryTemplate`,
`^SupportInfantryTemplate`, `^SuperDefenseTemplate` — are not abandoned ideas. **They are the
unapplied half of a designed split.** The units they were meant to hold are still sitting in the
templates the split was supposed to empty.

⛔ **That is why `archer` and `heavy_sniper` are the only two classes whose member count went DOWN
against the ledger, and why both are SIGNED against a template with zero inheritors.**

---

## 1. `archer` — ⭐ PROPOSAL, no price changes needed

### The roster

Four units are archers. Two look like archers and are not: `ra1_allies_longbow` is an Apache
gunship (`^HelicopterTemplate` ✅) and `td_gdi_archerartillery` is the British Archer SPG
(`^ArtilleryTankTemplate` ✅). Both keep their current class.

| unit | today | cost | HP | move to |
|---|---|--:|--:|---|
| `asianalliance_veteranarcher` | `^AntiTankAntiAirInfantryTemplate` | 450 | 14,000 | `^ArcherInfantryTemplate` |
| `japan_archermaiden` | ⛔ `^HeavyInfantryTemplate` **+** `^SniperInfantryTemplate` | 500 | 20,000 | `^ArcherInfantryTemplate` |
| `wc2_humans_elvenarcher` | `^AntiTankAntiAirInfantryTemplate` | 600 | 25,000 | `^ArcherInfantryTemplate` |
| `wc2_humans_highelvenarcher` | `^AntiTankAntiAirInfantryTemplate` | 1,100 | 35,000 | `^ArcherInfantryTemplate` |

⭐ `japan_archermaiden` is **also one of the 18 multi-template defects** and **the current `archer`
anchor** — it is signed while inheriting neither archer template nor a single class.

### The new anchor, and how the class lands

**Cheapest member = `asianalliance_veteranarcher` @ 450** (today's anchor is `japan_archermaiden`
@ 500, so the anchor moves and the class unsigns per §2.11).

| | measured | target |
|---|--:|--:|
| occupancy in the target band | **4 of 4 = 100%** | >= 80% |
| spread | **2.44x** | fits 2.50x |
| **sigma_log** | **0.346** | 0.3575 — and 0.869 roster-wide |
| prices unique | ✅ | required |
| prices multiples of 50 | ✅ 450 / 500 / 600 / 1,100 | required |

⭐ **No price changes are needed.** The class already satisfies the band, the bell and the grid on
the costs it ships with. This class is a pure re-tag.

### ⚠ But the re-tag is NOT behaviour-neutral

Moving between templates moves real traits. Per unit:

| | from `^AntiTankAntiAir` | from `^Sniper` | to `^Archer` |
|---|---|---|---|
| `Armor.Type` | Flak | Flak | **None** ⛔ |
| class `FirepowerMultiplier` | 110 | 120 | **100** |
| `RevealsShroud` | 6,500 | 7,500 | 7,000 |
| `DetectCloaked` | — | 3,750 | **removed** |
| `KeepsDistance` | — | 10 | **removed** |
| `RenderRangeCircle` | — | yes | **removed** |
| IFV condition | `ifv-miss` | `ifv-lightsniper` | `ifv-miss` |
| `BuildPaletteOrder` | 30 | 70 | 50 |

⛔ **`Armor: Flak -> None` is the largest consequence and it is a real balance change**, not a
label: armor type is the row every incoming weapon looks up. `japan_archermaiden` additionally
loses a **120 -> 100 firepower multiplier (-17%)** and its cloak detection; the three AT/AA archers
lose **110 -> 100 (-9%)** and gain 500 shroud range.

⚠ **So this cannot ship as a bare yaml edit.** The moves go through the pipeline: re-extract ->
`check_band` -> proposal -> `apply_balance --confirm` **on a maintainer order** for any stat that
must be compensated -> re-extract -> `audit_balance_drift` -> audit suite -> **boot gate**.

### Open questions for this class

1. **Is `Armor: None` intended for archers?** It is what `^ArcherInfantryTemplate` declares, but
   the four units all ship as `Flak` today, so the template has never been tested against a real
   member. If the intent was "unarmoured light infantry", fine; if it is a placeholder, it should be
   settled before four units inherit it.
2. **Should archers keep hitting air?** The template comment says *"Hits air"*, which is why these
   units sat in AT/AA. Confirm that survives the move, or the three WC2/Asian archers lose their
   role.
3. **`^HeavySniperInfantryTemplate` is the mirror case** and is next: `td_gdi_heavysniper` (700) is
   the signed `heavy_sniper` anchor and does not inherit it either.
