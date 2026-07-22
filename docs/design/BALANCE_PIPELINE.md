# BALANCE PIPELINE — the mega plan (v2, 2026-07-18)

_Balance changes become MECHANICAL. No agent (or human) can silently
drift the game's stats, because the pipeline — not discipline, not
document-reading — enforces consistency._

v2 incorporates the maintainer's refinements (2026-07-18): the ledger
holds RAW yaml stats only (no derived values), the workbook consumes
raw stats directly, Range is solved from Cost in-sheet, and the
Formula-v2 per-unit-type program (DESIGN §12 second iteration) rides
the same pipeline.

## 0. The core loop (maintainer's target workflow)

```
1. pull    yaml ──► JSON ledger          python tools/balance/extract_stats.py
2. edit    change values in the ledger (or in the generated sheet)
3. sheet   JSON ──► cameo_balance_v2.xlsx python tools/balance/build_workbook.py
4. tune    set Cost, the sheet solves Range (or check O/P/Q deltas)
5. import  xlsx ──► JSON                 python tools/balance/import_workbook.py
6. push    JSON ──► yaml                 python tools/balance/apply_balance.py --confirm
7. verify  drift audit: yaml ≡ ledger    python tools/balance/extract_stats.py --check
8. verify  multiplier audit: all `*Multiplier Modifier` values are integer percentages    python tools/audit/audit_multiplier_modifiers.py
9. decode  audit reports (if UTF-16)     python tools/balance/_decode_audit.py
```

Class rebalances add two extra proposal steps before the normal push:

```
A. propose  class report (markdown)    python tools/balance/propose_class_rebalance.py --class <scout|closecombat|special_forces>
B. patch    ledger from reports        python tools/balance/_patch_ledgers_from_reports.py
   then run steps 3–8 above.
```

## 1. ARCHITECTURE CORRECTION (the honest-opinion part, agreed with maintainer intent)

"All 4 documents always mirrored and all writable" is the one part
that cannot work as stated — four independently-writable copies of the
same numbers is how drift is CREATED, not prevented. The goal (never
manually checking mirrors) is kept, but through two rules:

- **Single writer at any moment.** A tiny state file
  (`docs/balance/.session`) records which representation is "open"
  (yaml | ledger | sheet). Pipeline commands move values in ONE
  direction and flip the state; running a command against a stale
  state aborts with instructions. Mirrors are verified at rest, not
  hoped for during writes.
- **The workbook is a WORKBENCH, not a source.** xlsx is binary —
  concurrent agents + git = unmergeable conflicts. The committed
  truths are exactly two: yaml (runtime) and the JSON ledger (balance).
  The sheet is regenerated on demand from the ledger and read back
  through a validating import; a lost sheet costs one command.
- The **drift audit** re-extracts yaml and diffs against the ledger on
  every run_all: any hand-edited stat = red finding with file + line.
  THIS is the "always mirrored without me checking" guarantee.

## 2. Ledger: RAW STATS ONLY (maintainer order — no derived values)

No DPS, no combined Damage, no effective-anything in the JSON. Every
number appears exactly as the yaml states it, with provenance:

```json
{
  "schema": 2,
  "faction": "tkm",
  "pack": "ContentPacks/RedAlert2Mod/TKM",
  "sections": {
    "vehicles": {
      "tkm_abrams": {
        "name": "Abrams",
        "cost":  {"v": 1500,  "src": "yaml/vehicles.yaml#Valued.Cost"},
        "hp":    {"v": 125000,"src": "yaml/vehicles.yaml#Health.HP"},
        "speed": {"v": 80,    "src": "yaml/vehicles.yaml#Mobile.Speed"},
        "armor": {"v": "Heavy","src": "yaml/vehicles.yaml#Armor.Type"},
        "armaments": [
          {"slot": "Armament@PRIMARY", "weapon": "tkm120mm",
           "requires": null,
           "stats": {
             "damage":      {"v": 16000, "src": "yaml/weapons.yaml#tkm120mm.Warhead@...Damage"},
             "reload_delay":{"v": 65,    "src": "yaml/weapons.yaml#tkm120mm.ReloadDelay"},
             "burst":       {"v": 1,     "src": "..."},
             "burst_delays":{"v": null,  "src": "..."},
             "range":       {"v": 6500,  "src": "yaml/weapons.yaml#tkm120mm.Range"},
             "spread":      {"v": 426,   "src": "..."},
             "versus_template": "^MediumCannon"
           }}
        ],
        "design": {
          "unit_class": 1.0, "special": 1.0, "weapon_class": 1.0,
          "tech_tier": 1.0, "class_anchor": "mbt"
        },
        "prerequisites": ["~tkm_warfactory"], "build_limit": null
      }
    }
  }
}
```

- `design.*` fields are judgment inputs (they never exist in yaml);
  they seed from the legacy sheet once (Phase 3) and live here after.
- Range stays in **raw wdist** (5000, not 5.0) — DESIGN §12 note; the
  sheet divides by 1000 in a helper column, the ledger never does.
- Multi-armament units keep EVERY armament with its condition
  (`requires`) — the old single-Damage flattening is retired; which
  armaments count toward pricing is a design flag per armament
  (default: unconditional + primary-upgrade ones).
- All derived quantities (DPS, effective reload, price) exist ONLY as
  formula cells in the sheet and as `formula.py` functions — computed,
  never stored.

## 3. Workbook v2 format (raw stats in, formulas visible)

Per-faction tabs (CABAL-tab lineage), one UNIT row followed by one
indented WEAPON row per armament (mirroring yaml structure):

| col | content | kind |
|---|---|---|
| A–C | Mod, Name, Actor id | identity (locked) |
| D–H | HP, Speed, Armor, TechTier, UnitClass, Special | raw + design inputs |
| I–N (weapon rows) | Damage, ReloadDelay, Burst, BurstDelays, Range(wdist), WeaponClass | raw |
| O | EffReload `= ReloadDelay + BurstDelays*(Burst-1)` | helper formula |
| P | DPS `= Damage*Burst/EffReload*WeaponClass` (summed to the unit row) | helper formula |
| Q–S | O, P, Q estimators (burst-aware, from raw cells) | formula |
| T | Price `=(O+P+Q)/3` — Formula v2 swaps in the class-anchor form | formula |
| U | Cost (actual, from ledger) | value |
| V | Δ = Price − Cost, traffic-light conditional formatting | formula |
| W | **Range-solver**: Range required for Price = Cost (closed form — the estimator mean is linear in Range, so the legacy inverse survives the raw-stat refactor) | formula |

- Helper columns instead of monster formulas: every intermediate is a
  visible, debuggable cell (maintainer's "all stats included" rule).
- Constants tab: armor ladder, weapon-class tables, class-anchor
  baselines (Formula v2), rounding conventions. All formulas reference
  it by named range — tune the law in ONE place.
- Locked cells everywhere except raw-stat and design-input columns.
- `formula.py` implements the identical math; equivalence-tested
  against the sheet on every build (legacy workbook's own computed
  values are the ground truth for the overlap set).

## 4. Sync commands (tools/balance/)

| command | direction | gate / notes |
|---|---|---|
| `python tools/balance/extract_stats.py [--faction X]` | yaml → ledger | overwrites `docs/balance/*.json`; run `--check` to detect drift |
| `python tools/balance/build_workbook.py` | ledger → `docs/design/cameo_balance_*.xlsx` | workbench regen; gitignored; safe to regenerate |
| `python tools/balance/import_workbook.py` | xlsx → ledger | validates and prints every input-cell diff |
| `python tools/balance/apply_balance.py [--faction X]` | ledger → yaml (dry-run) | prints diff; **does not write** |
| `python tools/balance/apply_balance.py --confirm [--faction X]` | ledger → yaml | **maintainer order only**; auto-runs `extract_stats.py` + `tools/audit/audit_multiplier_modifiers.py`; full `run_all.sh` + boot gate before commit |
| `python tools/balance/propose_class_rebalance.py --class <cls>` | ledger → `docs/balance/proposal_<cls>_infantry.md` | generates a markdown report; does not touch yaml/ledger |
| `python tools/balance/_patch_ledgers_from_reports.py` | `proposal_*.md` → ledger | patches `docs/balance/*.json` from the three class reports |

Round-trip invariants tested in CI-style: `extract_stats.py` ∘ `apply_balance.py --confirm` = identity, `build_workbook.py` ∘ `import_workbook.py` = identity.

## 5. Formula v2 — per-unit-type baselines (DESIGN §12 second iteration)

Already designed in DESIGN §12 (looked up 2026-07-18): everything is
anchored on the **Naxis Tiger Tank** (100000 HP / 100 speed / 10000
damage / range 5000 / reload 50 → O=P=Q=Cost=800 exactly), which
breaks at the low end (no intercept) and forces conventions like
defenses-speed-100 and bombers-reload-250. The documented fix — now
implemented through the pipeline:

- **One baseline unit per unit class**, each with Tiger-style round
  numbers; price by normalized deviation from the class anchor:
  `Cost = Cost₀ × (O/O₀ + P/P₀ + Q/Q₀) / 3` (exact at each anchor).
- Class list & anchor candidates (maintainer confirms each):
  - `mbt` (exists): Naxis Tiger Tank, Cost₀ 800 — unchanged.
  - `fighter` (exists): port the current separate fighter formula
    into the registry unchanged, then re-express on raw stats.
  - `bomber` (NEW): replace the reload=250 convention with real
    ReloadDelay from raw stats; anchor = a maintainer-picked bomber.
  - `defense` (NEW): replace speed=100 convention with a defense term
    (footprint + power draw are the natural "mobility" substitutes);
    keep DESIGN's charge-delay −0.25 rule as a K modifier.
  - `infantry` classes (NEW): scout / basic / heavy / hero anchors —
    absorbs the UnitClass column per DESIGN §12.
  - later: naval, harvester/economy, epic/BuildLimit-1 units.
- Fitting workflow per class: maintainer names 3–5 units they consider
  correctly priced → `fit_anchor.py` does least-squares on the class
  coefficients → validation table across the whole class → maintainer
  signs → the class formula becomes law in the Constants tab and
  `formula.py`. One class at a time, sheet stays usable throughout
  (unfitted classes keep the Tiger formula until replaced).

## 5b. Class tuning knobs & the modifier normalization program (2026-07-18)

Survey result: 83 templates in defaults.yaml carry multiplier traits.
They split into three kinds with three different fates:

1. **Cross-cutting systems — KEEP, out of pipeline scope.** Veterancy
   ranks (^GainsExperience*), crate buffs, debuff mechanics
   (^TerrorDronable, ^SquidGrabbable), melee cooldowns, fire-actor
   rules. These are gameplay mechanics, not balance knobs.
2. **The sanctioned knob hierarchy — KEEP, becomes PIPELINE-OWNED.**
   `^GlobalBuffs` → per-class (`^InfantryBuffs`, `^VehicleBuffs`,
   `^TankBuffs`, `^AircraftBuffs`, `^DefenseBuffs`) → per-subclass
   (Scout/Grenadier/AntiTankAntiAir/Heavy/Melee/Sniper…Buff). This
   already-existing structure IS the maintainer's one-value correction
   system and it SURVIVES normalization:
   - knob values live in the ledger + Constants tab (`class_tuning`);
     `balance push` writes them into the defaults.yaml traits via
     provenance anchors like any other stat;
   - **pricing formulas become knob-aware**: the sheet computes
     EffDamage = Damage × ∏Firepower-knobs and EffHP = HP ÷
     ∏Damage-knobs along the Global→class→subclass chain, and O/P/Q
     consume the EFFECTIVE values — so while a knob ≠ 100 the prices
     stay honest and the Δ column shows the price consequence of the
     knob turn immediately (better than today, where a knob turn
     silently invalidates the sheet);
   - the one-value workflow is unchanged in feel: edit ONE Constants
     cell → push → the whole class shifts in game.
3. **Ad-hoc formula-gap patches — BAKE & DELETE via Formula v2.**
   Knobs that exist only to paper over the Tiger formula's low-end
   failure (the ScoutInfantry damage-reduction stopgap is DESIGN §12's
   own example) become redundant once per-class anchors price small
   units correctly — they are folded into raw stats and removed.

**The bake operation** (maintainer-ordered, per knob): fold an
accepted long-term knob into raw stats and reset it to 100 —
Firepower×0.9 becomes every class member's Damage ×0.9 (rounded per
conventions; clean), Damage-taken knobs become HP adjustments (CAVEAT:
subtly shifts self-heal/repair proportions — when equivalence is
imperfect the knob simply stays live; nothing forces a bake). Baking
keeps effective power constant, so costs do not move.

## 6. Phases (revised, in execution order)

| phase | deliverable | effort |
|---|---|---|
| 1 | Extractor: raw-stat ledger, multi-armament, provenance, deterministic; `balance check` mode | M |
| 2 | Workbook builder: raw columns + helpers + solver + locks + Constants; equivalence test vs formula.py | M |
| 3 | Legacy comparator: name→id map, seed `design.*` inputs from old sheet, discrepancy report, maintainer triage | M–L |
| 4 | Sync commands + session state + round-trip invariants + gated push | M |
| 5 | Formula v2 program: class registry, anchor fitting, per-class sign-off (mbt → fighter → bomber → defense → infantry → rest) | L (per-class S) |
| 6 | Enforcement: `balance check` in run_all, DESIGN/CLAUDE law update, retire audit_stat_formulas + audit_balance_sheet into the pipeline | S |

First customer: the SM full rebalance (ROADMAP P1b) runs on Phases
1–4 the moment they land.

## 7. Risks & mitigations (v2 delta)

- Multi-armament pricing is the hardest format problem (the old
  one-Damage flattening is why sheet and yaml drifted); the
  weapon-sub-row layout + per-armament pricing flags solve it
  explicitly rather than implicitly.
- Solving Range from Cost stays closed-form only while price is
  linear in Range; Formula-v2 class variants must preserve that (they
  do — Range appears linearly in O, P and Q alike). The solver cell
  is regenerated per class formula.
- Versus tables: mirrored into the ledger read-only (from the ~30
  weapon-class templates per DESIGN's weapon-construction law); the
  sheet's WeaponClass stays the design scalar. Formalizing versus
  into pricing is a Formula-v3 question, deliberately out of scope.
- xlsx never committed → no binary merge conflicts, ever.
