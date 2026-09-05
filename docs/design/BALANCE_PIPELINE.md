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

**One command runs the verifying half of this loop in order:**

```sh
python tools/balance/run_pipeline.py              # verify — writes nothing
python tools/balance/run_pipeline.py --extract    # + step 1, refresh the ledgers
python tools/balance/run_pipeline.py --workbook   # + step 3, rebuild the workbooks
python tools/balance/run_pipeline.py --dry-run    # print the plan, run nothing
```

It executes steps 1, 3, 7 and 8 plus the structural gates, reports each stage's real
exit code, and **stops at step 6**. It cannot apply: there is no flag that reaches
`--confirm`, because an approval gate a tool can open by itself is not a gate. When the
verify stage is clean it prints the command for the maintainer to type.

Steps 2, 4 and 6 are human by definition — a balance decision is not a transformation —
and the runner lists them as such instead of skipping them quietly.

**The compiler property is measured, not assumed:**

```sh
python tools/balance/check_determinism.py                  # all ledgers
python tools/balance/check_determinism.py --faction d2k_ordos
python tools/balance/run_pipeline.py --determinism         # as a pipeline stage
```

It extracts twice in **separate processes** under different `PYTHONHASHSEED` and `TZ`,
builds the ledgers in memory, and compares every artifact byte for byte. Separate
processes are the point: inside one interpreter the hash seed is fixed, so set and dict
iteration order is stable by accident and an ordering leak stays invisible.

Nothing is written under `docs/balance/` — a tool that verifies the ledgers must never
be able to be the thing that moved them. `serialize()` already writes `sort_keys=True`,
so mapping order is safe; what this catches is a **list** built by iterating a set,
plus timestamps, timezone-dependent values and absolute paths reaching an artifact.

```
1. pull    yaml ──► JSON ledger          python tools/balance/extract_stats.py
2. edit    change values in the ledger (or in the generated sheet)
3. sheet   JSON ──► cameo_balance_by_faction.xlsx + cameo_balance_by_type.xlsx
                                         python tools/balance/build_workbook.py
4. tune    set Cost, the sheet solves Range (or check O/P/Q deltas)
5. import  xlsx ──► JSON                 python tools/balance/import_workbook.py --workbook faction|type
6. push    JSON ──► yaml                 python tools/balance/apply_balance.py --confirm
7. verify  drift audit: yaml ≡ ledger    python tools/balance/extract_stats.py --check
8. verify  multiplier audit: all `*Multiplier Modifier` values are integer percentages    python tools/audit/audit_multiplier_modifiers.py
9. decode  audit reports (if UTF-16)     — no longer needed; `run_all.sh` forces UTF-8
```

`cameo_balance_v2.xlsx` is the frozen pre-split prototype. It remains tracked
for historical comparison, but the builder and importer no longer read or write
it; only the faction/type workbooks above are active.

Class rebalances add two extra proposal steps before the normal push:

```
A. propose  class report (markdown)    python tools/balance/propose_class_rebalance.py --class <scout|closecombat|special_forces>
B. patch    ledger from reports        python tools/balance/_patch_ledgers_from_reports.py
   then run steps 3–8 above.
```

## 0b. THE TWO ENGINEERING PRINCIPLES (maintainer 2026-08-29) — binding

Adopted after the same failure shape appeared four separate times. They are not
philosophy; each has a named incident behind it.

### P1 — Metrics must be context-aware

> *"No metric should blindly sweep the tree without understanding the mechanical
> context of the nodes it flags."*

Classify the object BEFORE applying a threshold. A number means nothing until you
know what produced it.

| the sweep | what it would have condemned |
|---|---|
| `Speed < 50` | 100 of 807 buildable ground movers — the entire super-heavy class. The engine floor is **30**; 50 is the CLASS ANCHOR minimum, a different concept. At 30: 7 flagged, 6 not units. |
| `ReloadDelay < 10` | 121 live weapons, most of them exempt BY MECHANISM — a continuous beam's reload IS its damage tick, a Gatling ladder's 6/4/2 is the spin-up. |
| `credits per second` | two different economies. Swarm and single-harvester factions are not comparable on one number. |
| `Damage == 0` | the GDI Ion Cannon, which delegates through `FireFragment` (see P2). |

In practice: exemptions are declared by MECHANISM in
`docs/design/balance_exceptions.yaml`, never hardcoded in the checker, and they
match on **family stem** using the DESIGN §1 suffix grammar so one entry covers
`RA2GattlingMG3` and `RA2GattlingMG3_AA` both.

### P2 — Relationship traversal is infrastructure

> *"We must trace the full delegation chain before declaring a value zero or a
> node dead."*

A raw yaml node is not the runtime mechanic. Resolve the chain:

```
actor -> Armament -> weapon -> Warhead -> {damage | FireFragment -> weapon -> ...}
actor -> Buildable.Prerequisites -> upgrade/promotion -> condition -> consumer
harvester -> refinery -> DockHost -> unload throughput -> free fleet
weapon  -> Inherits@wh -> ^Warhead_<Family>_<Level>   (the §1b name source)
```

`audit_support_powers.py` S3 reported the GDI Ion Cannon at **zero damage**
because it stopped at the top weapon; following `FireFragment` gives **452075**.
"The GDI Ion Cannon is broken" was too surprising to be true, and it wasn't.

No audit re-walks yaml by hand. Read through `miniyaml.Ruleset.resolve` /
`.resolve_weapon` (CLAUDE.md 8e), and when a traversal is needed twice, promote it
to shared infrastructure rather than reimplementing it.

## 1. ARCHITECTURE CORRECTION (the honest-opinion part, agreed with maintainer intent)

"All 4 documents always mirrored and all writable" is the one part
that cannot work as stated — four independently-writable copies of the
same numbers is how drift is CREATED, not prevented. The goal (never
manually checking mirrors) is kept, but through two rules:

- **Single writer at any moment.** ⚠ **PROPOSED, NEVER BUILT.** The v2 plan called for
  a state file (`docs/balance/.session`) recording which representation is "open"
  (yaml | ledger | sheet), so a command run against a stale state would abort. No such
  file exists and no tool reads one — this paragraph described it in the present tense
  for long enough that it read as shipped. What actually enforces the invariant is the
  weaker but real pair below: one direction per command, and the drift audit catching
  disagreement after the fact rather than preventing it during the write.
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

> **SETTLED 2026-08-11 (W3).** The law briefly broke: `c9a09dc91` put five DERIVED
> fields in every ledger row, and a derived field moves when the **formula** moves,
> with no yaml change at all — correcting the metric's scatter model rewrote **4 136
> ledger lines while `mods/` was untouched**, i.e. model noise inside the very artifact
> whose job is to prove yaml ↔ ledger equality.
>
> The fields now live in a second tree, written by the **same** `extract_stats.py` run
> off the same resolve, so the two can never desync:
>
> | tree | contents | a diff means |
> |---|---|---|
> | `docs/balance/<faction>.json` | raw stats + provenance | **the game changed** — yaml was edited |
> | `docs/balance/derived/<faction>.json` | scalable `k_flat`, measured `k`, standalone percentage floor, folded rounding residual, `effective_per_shot`, full-cycle `eff_reload` / `effective_dps`, and spatial diagnostics | **the model changed** — a tool was edited |
> | `docs/balance/derived/_model.json` | the constants every derived number depends on (`SWARM_W`, `BLOB_UPTIME`, `DENSITY`, `ENGAGEMENT`, `reference_hp`, the armor census …) | the model was **retuned** |
>
> Each diff now answers exactly one question. `audit_balance_drift` reads only the raw
> tree (`extract_stats.build_ledgers()` returns raw by construction, so it cannot start
> diffing model output by accident); `extract_stats.py --check` verifies both and labels
> the finding `DRIFT (raw)` or `DRIFT (model)`. The derived rows repeat only `slot` and
> `weapon` as join keys — never a raw stat, so there is still exactly one copy of every
> number. Spec: [`EFFECTIVE_DAMAGE.md`](EFFECTIVE_DAMAGE.md).
>
> ⚠ Nothing consumes the derived tree yet — it is a read-only sidecar. `build_workbook.py`
> never read the old in-ledger fields either. Wiring K into pricing is **W11**, behind a
> flag and with a maintainer sign-off; do not let `apply_balance` write `Damage` from a
> derived number before then.

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
- **Multi-warhead damage convention.** Cameo weapons stack several
  offensive `Warhead@X: SpreadDamage` nodes (one per inherited weapon-class
  template); the engine detonates ALL of them, so the effective per-shot
  damage is the SUM (`formula.spread_damage_sum`), never the max. The
  workbook's **Damage cell is that per-shot TOTAL** — the same quantity
  pricing/DPS use — so the sheet and the price never disagree. Editing it
  writes per-warhead Damage through the ONE canonical reducer
  `formula.distribute_damage`, which applies the fixed DESIGN.md law:
  **every main class warhead gets the IDENTICAL value `total ÷ N` snapped
  to the 100-damage grid** ("all class warheads carry the identical
  value" — never proportional, never off-grid), `*FriendlyFire` and
  `*ExtraDamage` twins = **50%** of the main. Standalone `*Percentage`
  companions track **0.01% per 100 flat Damage** in their own denominator;
  folded `PercentageScale` damage derives from the main Damage. The calculator mirrors
  the Cameo runtime: both positional and direct-Actor `AreaDamage` impacts apply the
  folded hit exactly once, with wide intermediate arithmetic and checked final results.
  `*ExtraDamage` (the energy-weapon shield/AoE-compensation chip) is
  always 50% of the main but is **excluded from the damage total**.
  Fine-tuning is done on the 100-Damage grid or with reload timing;
  unconditional actor `FirepowerMultiplier` is retired as a tuning knob. A single number can therefore never be
  broadcast identically onto every warhead (the 2026-07-22 over-damage
  regression, commit `04de392b3`). `audit_warhead_split` fails the suite if
  that fingerprint ever reappears.
- All derived quantities (DPS, effective reload, price) exist ONLY as
  formula cells in the sheet and as `formula.py` functions — computed,
  never stored.

## 3. Workbook v2 format (raw stats in, formulas visible)

Per-faction tabs (CABAL-tab lineage), one UNIT row followed by one
indented WEAPON row per armament (mirroring yaml structure):

| col | content | kind |
|---|---|---|
| A–C | Mod, Actor id, Name | identity (locked) |
| D | Class | design input |
| E–G | HP, Speed, Armor | raw values; Armor is locked |
| H–J | TechTier, UnitClass, Special | design inputs |
| K | FirepowerMultiplier | compatibility value (locked; retired as a tuning input) |
| L–Q (weapon rows) | Damage, Reload, Burst, BurstDel, Range(wd), WeapClass | raw + design inputs |
| R | EffReload `= Reload + sum(all Burst-1 gaps)`; one delay repeats, blank uses engine default 5 | helper formula |
| S | DPS `= Damage*Burst/EffReload*FirepowerMultiplier` (summed to the unit row; WeapClass is design-only) | helper formula |
| T–V | O, P, Q estimators (burst-aware, from raw cells) | formula |
| W | Price `=(O+P+Q)/3` — Formula v2 swaps in the class-anchor form | formula |
| X | Cost (actual, from ledger) | raw input |
| Y–Z | Δ = Price − Cost; absolute Δ% with traffic-light formatting | formula |
| AA | **Range-solver**: Range required for Price = Cost (closed form — the estimator mean is linear in Range, so the legacy inverse survives the raw-stat refactor) | formula |
| AB | WeaponTypes | resolved classification (locked) |

- Helper columns instead of monster formulas: every intermediate is a
  visible, debuggable cell (maintainer's "all stats included" rule).
- Constants tab: armor ladder, weapon-class tables, class-anchor
  baselines (Formula v2), rounding conventions. All formulas reference
  it by named range — tune the law in ONE place.
- Missing Reload, Burst, and Range fields display their engine defaults (1, 1,
  and 0). Editing one creates the top-level weapon field; an unchanged default
  remains absent. Blank BurstDel means the engine's five-tick default.
- Cells stay locked when there is no safe backing field to edit, such as a
  synthetic defense Speed or a weapon row with no main damage warhead.
- `formula.py` implements the identical math; equivalence-tested
  against the sheet on every build (legacy workbook's own computed
  values are the ground truth for the overlap set).

## 4. Sync commands (tools/balance/)

| command | direction | gate / notes |
|---|---|---|
| `python tools/balance/extract_stats.py [--faction X]` | yaml → ledger | overwrites `docs/balance/*.json`; run `--check` to detect drift |
| `python tools/balance/build_workbook.py` | ledger → `docs/design/cameo_balance_*.xlsx` | tracked generated workbenches; regenerate and review the binary diff |
| `python tools/balance/import_workbook.py` | xlsx → ledger | validates and prints every input-cell diff |
| `python tools/balance/apply_balance.py [--faction X]` | ledger → yaml (dry-run) | prints diff; **does not write** |
| `python tools/balance/apply_balance.py --confirm [--faction X]` | ledger → yaml | **maintainer order only**; auto-runs `extract_stats.py` + `tools/audit/audit_multiplier_modifiers.py`; full `run_all.sh` + boot gate before commit |
| `python tools/balance/propose_class_rebalance.py --class <cls>` | ledger → `docs/balance/proposal_<cls>_infantry.md` | generates a markdown report; does not touch yaml/ledger |
| `python tools/balance/_patch_ledgers_from_reports.py` | `proposal_*.md` → ledger | patches `docs/balance/*.json` from the three class reports |

Round-trip invariants tested in CI-style: `extract_stats.py` ∘ `apply_balance.py --confirm` = identity, `build_workbook.py` ∘ `import_workbook.py` = identity.
Each generated workbook also carries a SHA-256 fingerprint of the builder,
formula/tier helpers, active ordering files, and raw/derived ledgers. `--check`
rejects a workbook that predates any of those inputs; manual formula edits still
require the normal regeneration/review gate.

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
- The active xlsx workbenches are tracked generated artifacts; regenerate them
  from the ledger and review binary conflicts instead of treating them as sources.

## 8. HARDENING — deterministic, agent-independent, memory-free (2026-07-25)

Maintainer directive: the pipeline must **run identically no matter who starts it or which AI agent
is used**, be **absolutely stable**, and **never be disrupted by wrong memories.** The architecture
above already resists silent drift; this section closes the remaining gaps so the pipeline is truly
foolproof, and fixes the ORDER of operations. Reference data behind the anchor targets:
`BALANCE_SYNTHESIS.md` §12–§19.

### 8.1 The BASEBAND law — encode it, stop trusting it to discipline

⛔ **RETIRED 2026-08-29 — there is no verifier actor any more.** Maintainer: *"we no longer
have to have those verifiers — they should be regular units like anything else and not have those
stiff rules."* `verifier_actor` is gone from `class_anchors.json` and from every code path. It used
to be exempt from balancing alongside the anchor; three measurements retired it. Freezing it moved
the other members' worst |Δ| by **0.0 in 17 of 23 classes** (and improved 5). Only **8 of 23**
nominated verifiers sat at the 2.5× cost0 below, while three sat BELOW their own baseline. And
because exempt rows are excluded from the report's worst-|Δ| line, a verifier off by **−3779.9**
credits (`dreadnought`) was invisible in the report meant to catch it — freezing it did not merely
fail to help, it HID the failure. **Only the ANCHOR is frozen**, because it defines `cost0`, which
is what makes the class formula a formula. The baseband ratio below is unchanged and still enforced
by `check_band.py`, on price RATIOS — it never needed a nominated actor.

The class **baseline** (100% cost) and the 250% ceiling (**= 2× HP + 2× DPS**) bound a *band where
most units live*. Distribution is deliberately uneven:
- **⭐ TARGET BAND 72.9%–250% cost** (maintainer 2026-08-31: *"the target band should be at 75% to
  250% where most units are located"*) — ~**80% of all units**, skewed toward the **baseline**.
  ⚠ The floor is **72.9%, not 75%**: see §8.1a — the maintainer's "75%" is a STAT number, and
  ¾ of the anchor's HP and DPS costs 0.729, while 75% of the *cost* is ×0.771 stats.
- **Hard caps 50%–400%** — only a few units below the target floor or above 250%.
- **★ The 50% floor is not a guess either** — it is exactly HALF the anchor's HP and DPS, the
  same derivation as the 250% ceiling run downward (§8.1a).
- **★ Price ⇒ tech-tier GATE mapping (maintainer 2026-07-25).** Cost above the baseline must be
  *earned* by a gate (the 2.5× verifier already is — e.g. the Forgotten Soldier is an upgrade-unlock
  on Tier-2/Radar while its baseline is a plain Tier-1 unit). The rule:
  - **100%–200% cost** — allowed **ungated** (plain Tier-1, no promotion/upgrade).
  - **200%–300% cost** — MUST have **≥1 gate**: a promotion, an upgrade-unlock, **or** Tier 3+.
  - **> 300% cost** — **end-game**: MUST be gated by a promotion, upgrade-unlock, **or** the latest
    tech tier (3+). (Power is allowed to scale with tier; the gate scales with the price.)
- **→ NEW VALIDATOR (`tools/balance/check_band.py`):** for every unit, compute its class-formula
  price ratio price/cost0. **Findings:** priced **< 75%** (breakdown risk); priced **> 200% while
  ungated** (plain Tier-1, no promotion/upgrade → must gate); priced **> 300%** not end-game-gated.
  Report the **100–200% ungated occupancy** (target ≥ 80%). Gate detection = `design.tech_tier ≥ 3`,
  a promotion/upgrade token in `prerequisites`, or `build_limit`/hero template. Wire into
  `run_all.sh`. This turns the baseband + tier-gate from remembered rules into an **enforced gate**.

### 8.1a ⭐ WHY THOSE NUMBERS — the band law is DERIVED, not preferred

⛔ **Read this before proposing any band ring, and before "re-anchoring" a class to fix
occupancy.** Every constant in `check_band.py` is the price of a **stat window**, and the
derivation is a closed form that `tools/tests/test_band_law.py` checks against
`formula.py` itself at every point quoted here.

Hold speed and range at the anchor's, and write `h`, `d` for the HP and DPS multipliers.
`formula.class_baseline_estimators` is then `O = (h+1+1+d)/4`, `P = (h·1 + 1·d)/2`,
`Q = h·1·1·d`, and `price = (O+P+Q)/3` collapses to:

```
price(h, d) = (3(h + d) + 4hd + 2) / 12        # SYMMETRIC in h and d
price(x, x) = (2x + 1)(x + 1) / 6              # both stats moved together
x(P)        = (√(1 + 48P) − 3) / 4             # the inverse: what window a ring means
```

⛔ **THE RINGS ARE DECLARED IN COST.** Maintainer, 2026-08-31: *"The 75% referred to the
unit price not the stats"*, and *"let's use the full band from cost 50% and stats 50% to
cost 3.5x and stats 2.5x"*. Cost is the space a player reads off the build palette; the
stat window is the **derived** reading. ⭐ **Four of the five rings come out exact in both
spaces at once** — which is exactly why this band beats the 4.00 ceiling it replaced
(×2.7231 stats: round in neither space, and unexplainable to anyone who asked why).

⭐ **THE FOUR-POINT BAND** (maintainer, 2026-08-31): *"we make the 1.0x to 2.5x the regular
Band for 80% of the unit population, the baseline actor being exactly at 1.0x ... and the
extended band for the remaining 20% outlier units is between 0.5x and 3.5x price."*

| ring | cost | stat window | exact in BOTH? |
|---|--:|--:|:-:|
| `FLOOR` | **0.50** | **×0.50** HP and DPS | ✅ |
| `SWEET_LO` **= the anchor** | **1.00** | **×1.00** | ✅ |
| `SWEET_HI` | **2.50** | **×2.00** | ✅ — *"2× HP and 2× DPS"* |
| `CEIL` | **3.50** | **×2.50** | ✅ |

**All four are exact in both spaces at once — no earlier candidate managed it.** The fifth
ring is gone: the target floor IS the anchor.

⚠ **`SWEET_LO` has been wrong twice, and both wrong values looked principled.** 0.7292
(= 35/48) is the cost of ×0.75 *stats* — rejected, *"the 75% referred to the unit price not
the stats"*. 0.75 is a 75% *price* — superseded by the four-point ruling. Neither is a bug
awaiting re-fix; `test_SWEET_LO_IS_THE_ANCHOR_and_both_rejected_values_stay_rejected` pins it.

#### ⛔ The strong claim this encodes, and the measurement that licensed it

Putting the target floor **on** the anchor says a normal class member is never *cheaper*
than the class face — anything below 1.00 is an outlier **by construction**, not by
measurement. That is much stronger than "the anchor is the entry unit", and it was
challenged as possibly unreachable. The challenge was right to demand a number.
`band_granularity.py` answers it:

| judged against | members below their own anchor |
|---|--:|
| the ruled **SPEC** | **54%** |
| the **live anchor actor** | **21%** |

⭐ **21% — essentially exactly the 20% the extended band allots.** The strong claim survives
contact with the roster.

⛔ **And the 33-point gap is not the roster; it is the RESTAT DEBT, carrying a warning about
the LOCKED table itself.** The specs price as if the anchor were far stronger than the actor
carrying it — `tiger.nax` is live at **100k HP against a spec of 240k**. Applying those
specs as written would make each anchor stronger than the class it anchors and push a
further third of the roster below the target floor. **The restat is not merely unapplied; as
specified it appears over-specified.** Re-derive the specs so the anchor lands *on* 1.00,
then re-run the census as the check. `test_SWEET_LO_is_a_PRICE_of_75_percent_and_NOT_the_cost_of_three_quarter_stats`
fails if anyone tries again.

The rejected ceilings, for the record: ×2.723 → 4.00 (round in neither space), ×3 → 4.667
(wide enough that a class starts overlapping the epic bracket), ×4 → **7.50** (a member 7.5×
its own anchor is not a class member — it is an epic, and epics are already band-exempt via
`build_limit`, §8.4). And the rejected *fifth ring*: a distinct lower sweet edge at ~0.68
was proposed to keep the anchor off the band boundary. Rejected — the coupling it worries
about is real but is fixed by **process** (price from the spec's `cost0`, never from
whatever the anchor actor happens to cost today), not by adding a number that is round in
neither space.

**The maintainer derived `SWEET_HI` twice, independently, a month apart** — §8.1 above has
carried *"verifier (250% cost = 2× HP + 2× DPS)"* since it was written. It is correct, and
the same argument run downward is what makes `FLOOR = 0.50` exact rather than a round number
someone liked. The ×2.5 → 3.50 ceiling closes the set: the hard band is now exactly the
**±2.5× stat window**, and the target band the **75%–250% price window**, each round in the
space it is declared in.

⚠ **The rings are CURVES, not boxes.** `3(h+d) + 4hd = 28` is the *entire* 250% iso-cost line:

| HP × | max DPS × still costing exactly 250% |
|--:|--:|
| 1.0 | 3.571 |
| 1.5 | 2.611 |
| **2.0** | **2.000** |
| 3.0 | 1.267 |
| 4.0 | 0.842 |
| 6.0 | 0.370 |

That is the maintainer's *"one of the stats can also be higher if the other one is a bit
lower"*, in closed form — and because `price(h,d) = price(d,h)`, **HP and DPS are exactly
interchangeable in pricing**. A single narrow cost band therefore holds units that play
nothing alike: a 6× HP / 0.37× DPS bunker-crawler and a 1× HP / 3.57× DPS glass cannon are
the SAME price. Reading the band as a box ("≤2× HP AND ≤2× DPS") would wrongly exclude both.

### 8.1b ⭐ THE BELL LAW — the distribution INSIDE the band

Maintainer, 2026-08-31: *"the distribution of the units in the band should be like a bell
curve and the outliers should be like a standard deviation or something like that but with
the 80/20 split."* **That closes the band law.** Solve a log-normal price distribution that
puts 80% inside `[1.00, 2.50]`:

```
σ(log price) = 0.3575        geometric centre μ = 1.581 × cost0   ( = √2.50 )
```

Every ring then becomes a **σ-level**, and the zone shares fall out of the arithmetic:

| zone | σ range | share of a bell-shaped class |
|---|---|--:|
| below `FLOOR` 0.50 | −∞ … −3.22σ | **0.1%** |
| lower skirt 0.50–1.00 | −3.22σ … **−1.28σ** | **9.9%** |
| **TARGET 1.00–2.50** | **−1.28σ … +1.28σ** | **80.0%** |
| upper skirt 2.50–3.50 | +1.28σ … **+2.22σ** | **8.7%** |
| above `CEIL` 3.50 | +2.22σ … +∞ | **1.3%** |

⭐ **The target band is exactly ±1.28σ — which *is* the 80% interval of a normal
distribution.** The 80/20 split was never an arbitrary quota; it is the ±1.28σ envelope, and
the four ruled rings land on it. The skirts split **9.9% / 8.7%** — an almost perfect 10/10
of the remaining 20% — and only **1.4%** falls genuinely outside the hard band. That 1.4% is
the true exception population the registry is for: epics, transforms, data bugs.

⚠ **BE PRECISE ABOUT WHAT IS DERIVED HERE.** The mathematics says: *given these bounds and
a log-normal model, an 80% central interval is ±1.28155σ.* It does **not** prove Cameo must
hold 80% of its units there — that is a design choice, and the evidence for it is empirical
(the below-anchor census reads 79/21 against live anchors, §8.1a). Both halves matter: the
σ-arithmetic is exact, the 80% is a **ruled target with supporting measurement**. Anyone
quoting this section as "the roster is mathematically required to be 80/20" is overclaiming,
and that distinction is the kind this project has been repeatedly rescued by.

⚠ **Two things that are easy to get backwards.**

1. **The class's geometric centre is 1.581× `cost0`, not 1.00.** The anchor sits at the
   **bottom edge** of the bell (−1.28σ) because it is the entry unit. "Bell-shaped" describes
   the MEMBERS; it does not move the anchor to the middle. §8.1a's lower-quartile result and
   this one are the same fact in two coordinate systems.
2. **The 80% is a DIAGNOSTIC TARGET, not a quota.** A class at 74/26 is not automatically
   broken — check its σ first. Forcing a percentage by moving members produces a beautiful
   table that describes nothing.

#### And the test earns its keep — it found the data bugs unprompted

`band_granularity.py` measures skew and excess kurtosis of log price per class. **8 of 11
classes are already bell-like**, and the three that are not are **exactly** the three
carrying known data bugs: `artillery` (skew +2.43, kurtosis +7.55 — `futuretech_athenacannon`
at DPS 193,600), `scout_vehicle` (+0.60 — the 7-actor IFV family), `missile_vehicle` (+0.87 —
the worst spec/actor mismatch in the tree). The shape law identified them without being told
what to look for, which is the strongest available evidence that it describes something real.

#### ⭐ σ_log — the one number that sizes the whole repricing job

```
σ_log measured on the roster  : 1.013
σ_log an 80% target band wants: 0.357
```

**The roster is ~2.8× too dispersed in log price.** Every repricing pass should move that
number toward 0.357. It is the cheapest progress metric the programme has, it is pinned in
`doc_claims.yaml` as `roster_sigma_log`, and it collapses "how much work is left?" into one
scalar that cannot be argued with.

#### Where does the baseline actor sit in the band? At the lower quartile — by construction

Not the centre. In the target band `[0.729, 2.50]` the anchor at 1.000 sits at **26% of the
log-width**; in stat space the window is ×0.75…×2.00 and the anchor at ×1 sits at **37%**.
That is a *consequence*, not a preference: the anchor is the class's recognisable ENTRY
unit, so almost nothing in the class is meaningfully weaker than it, while plenty is
stronger. A centred anchor would put half of every class below its own zero point, which
would mean the zero point is not the entry unit. **The band is asymmetric because the
design is.**

#### ⛔ RE-ANCHORING CANNOT FIX A CLASS THAT IS WIDER THAN THE BAND

Members are priced as **ratios** to the anchor, so choosing a different anchor **slides** a
class along the band and never **narrows** it (pinned by
`test_a_class_spread_does_not_depend_on_which_member_anchors_it`). The target band is
`2.50 / 1.00` = **2.50× wide** and the hard band `3.50 / 0.50` = **7.0×**. A class whose own
priced spread exceeds those cannot reach the ruled ≥80% occupancy from *any* member.
Measure it with **`tools/balance/band_granularity.py`**, never by eye.

⭐ **And the two numbers it reports are a work-sorting rule, not one verdict.** On trimmed
(P10..P90) spreads: **14 of 17 classes already fit the HARD band, and only 2 fit the target
one.** A class inside the hard band is a **repricing** job — its members sit at plausible
relative values and need pulling toward the anchor. A class outside it (`scout_vehicle`
11.1×, `support` 10.1×, `artillery_tank` 8.3×) is a **scope** question: those members may
not belong in one class at all. ⚠ `support` is outside for a third reason entirely — it is
the class carrying six of the eight negative-DPS extractor bugs.

#### Granularity — how many units actually fit, measured against 14 shipped mods

The band is a **resolution budget**: at a cost step `s`, a band of width `W` holds
`ln(W)/ln(s)` rungs a player can tell apart. `s` is not a guess — it is measured from the
peer corpus in `docs/design/ORIGINAL_UNITS_PEER_OPENRA.md` (`tools/reference/`, 266 adjacent
cost gaps, 14 mods): **the median adjacent cost step in a shipped OpenRA mod is 1.143×**,
and per-mod it runs 1.056×–1.200×. So:

> **the 2.50×-wide target band holds ≈ 6.9 distinct price rungs**, and the 7.0× hard band
> holds ≈ 14.6.

⚠ That is the honest cost of putting the floor on the anchor: the target band lost ~2 rungs.
`mbt`'s 42 members over 6.9 rungs is 6.1 per rung, above Combined Arms' observed 4.67 — tight
but not broken, since same-rung members are cross-faction siblings drawn from 22 factions.

⭐ **A class with 42 members does not need 42 rungs.** Shipped mods deliberately price
several units alike — Combined Arms runs **4.67 units per distinct cost** across 215 armed
units. Cameo's `mbt` has 42 members drawn from **22 factions** (1.9 per faction), so 42
members over 9.2 rungs is **4.6 per rung**, filled by different factions — the exact density
a shipped 215-unit mod already uses. **The band is wide enough. The problem was never
capacity.**

#### ⛔ The price grid — why a FLAT credit step is not a grid

Maintainer proposal: *"having prices of units in steps of 20 is good enough"*. **20 is the
right ATOM and the wrong STEP**, and the roster is what says so
(`tools/balance/cost_grid.py`):

* Cameo's prices run **10 – 10,000 credits, a 1000× range**, median **1,200**.
* **89% of prices are ALREADY multiples of 20.** Snapping to a flat 20 changes almost
  nothing, because the over-precision is in the SPACING, not the last digit.
* A flat 20 is ~14% — one perceptible notch — only near **140 credits**. Just **6% of the
  roster** sits at or below 200. At the median it is **1.7%**, eight times finer than
  anything a player can read; at 5,000 it is 0.4%.
* A 1.143× ladder cannot even be *expressed* on a flat 20 above ~140 — consecutive rungs
  collide on the same multiple.

So keep the atom and derive the step:

```
step(price) = max(20, 20 × round(0.143 × price / 20))
```

Every price stays a legible multiple of 20; adjacent rungs stay one perceptible notch
apart. The step is 20 at 140 credits, **160 at the median**, 700 at 5,000. That takes the
roster from **105 distinct prices to 55** (0.078 → 0.041 per unit, against Combined Arms'
0.214) and the median adjacent step from **1.041× to 1.078×**. 92% of units move, median
move 2.0%.

⚠ A grid snap is a **repricing**: it goes through the ledger and `apply_balance --confirm`,
must re-pass `check_band`, and must boot-gate. `cost_grid.py` proposes and never writes.

#### How steep is Cameo's pricing, against the same 14 mods?

Regressing `log(cost)` on `log(HP)` and `log(DPS)` over **766 armed mobile units** with mod
fixed effects gives a shipped-mod exponent of **a+b = 0.84** (HP 0.62, DPS 0.21; R² 0.43;
per-mod median 0.84, range 0.48 – 1.25). Cameo's class formula has a combined elasticity of
**1.16** over ×0.5…×2 (1.17 locally at the anchor).

> **Cameo charges ~38% more per unit of stat than the median shipped mod**, and sits at the
> top of the observed range — above 12 of the 14, near Tiberian Sun (1.25), Dune 2000 (1.20)
> and Shattered Paradise (1.12).

⛔ That is a **finding, not a defect**, and it is NOT an argument to flatten the formula. A
steeper curve means a class's stat window maps to a wider cost band, which is precisely why
the ×2 window costs 2.5× here and would cost only 1.78× in the median peer. It is recorded so
that the next person who compares a Cameo price to an RA2 price knows the exchange rate
instead of rediscovering it. Re-measure with `tools/reference/peer_cost_grid.py`.

### 8.2 Determinism & agent-independence (the anti-"wrong memory" rules)

1. **Three authorities, and ONLY three:** `formula.py` (the math), `class_anchors.json` (the
   numbers), and this doc + `DESIGN.md` (the laws). **Memories are hints, never authority.** If a
   memory and these disagree, these win — always verify against them before acting.
2. **Every command is idempotent + guarded.** `extract_stats.py --check`, the single-writer
   `.session` state, and the drift audit mean a re-run or a wrong-state run **aborts with
   instructions** rather than half-applying. Same inputs → same outputs, every agent, every time.
3. **The guardrails catch mistakes regardless of belief:** drift audit (yaml≡ledger) + the new band
   validator (§8.1) + anchor-completeness (§8.5) + the boot gate. An agent acting on a wrong memory
   **cannot land** an inconsistent state silently — a guard goes red.
4. **No step requires judgment that isn't recorded.** Anchor picks, `design.*` inputs, and sign-offs
   live in the ledger/anchors JSON, not in an agent's head — so the next agent reproduces them.

### 8.3 ORDER OF OPERATIONS (maintainer law) — base units + defenses FIRST, upgrades LAST

**Upgrades are priced ON TOP of finished unit stats.** If a unit's stats change, every upgrade's
effect (armor mult, regen, weapon swap) changes too — so pricing upgrades before units are final
means re-doing them. Therefore the strict sequence:

1. **Rebalance all BASE units + DEFENSES** (this whole §8.4 anchor pass) → validate band → apply →
   boot-gate → commit.
2. **THEN** price upgrades on the finished baselines (incl. the defensive-upgrade-stacking fix,
   `BALANCE_SYNTHESIS.md` §18.2 — the actual "unkillable" cause). **Do not start upgrades early.**

### 8.4 Anchor-finalization sequence (grounded in §12–§19, one class at a time)

Most `class_anchors.json` entries are still `signed_off: false`. Finalize them using the synthesis
data, each via `fit_class.py` + a maintainer-confirmed anchor unit + sign-off:

- **Infantry anchors → keep** (§19: infantry are on-scale, ~1× rifle). Confirm + sign the existing
  provisional infantry anchors.
- **Vehicle / aircraft anchors → REVIEW, don't presume (maintainer 2026-07-25).** §19 shows Cameo
  tanks ~2× / aircraft ~2.5× the RA2 *reference* ratio, BUT the maintainer judges the **baselines
  fair** (scout 20k/100¢ vs Tiger 100k/800¢ = 5× is intended). So **do NOT cut the class baselines**;
  the reference is context, not a target. The real suspect is **specific later-game units scaling too
  much with HP** — handle per-case, **test in-game**, keep the Tiger MBT anchor (100k, cost0 800)
  unless testing says otherwise. (`BALANCE_SYNTHESIS.md` §19.3.)
- **Defenses → roughly keep** (§19: near reference) — but account for the §7 building damage-exemption
  so effective durability isn't double-counted.
- **Price default = ORIGINAL price** (`BALANCE_SYNTHESIS.md` §20): each unit defaults to its
  source-game cost; deviate only for real power enhancements or faction economy-identity, always
  in-band. Uniqueness rides the **stat-mix** (formula-stable), not the role/silhouette.
- **Epic / hero → not band-limited** (`BuildLimit: 1`, §18.1) — priced separately, deliberately extreme.
- Each class: `fit_class.py --class X --anchor <unit>` → review `formula_v2_X.md` + `check_band.py`
  → maintainer sets `signed_off: true`. **One class at a time**; unfitted classes keep the Tiger
  formula meanwhile. Suggested order: **mbt → high-tech → light/TD/AA/arty tanks → aircraft →
  defenses → infantry confirmations.**

### 8.4b Fixed pricing rules that BYPASS the class formula (maintainer laws)

Two unit kinds are NOT priced by the class-baseline formula:
- **Support** (medics / mechanics / casters / mind-control / spies) — **fully EXEMPT.** No consistent
  or no damage ⇒ nothing to compute. Never anchor/verify/band-check. The one class outside balancing.
- **Cargo vehicles + aircraft** (any actor with a `Cargo:` trait — APC, transport, Battle Fortress) —
  priced at the **SUM of the infantry carried at full capacity**, *even if unarmed.* A FIXED rule:
  `cost target = Σ(passenger costs at Cargo.MaxWeight)`. `check_band` must detect `Cargo:` and check
  the passenger-sum, not the class formula. (Verified: Battle Fortress MaxWeight 6 @ 4000¢.) So an
  unarmed transport is *balanced* (passenger-sum), unlike exempt support.

### 8.5 New guards to build (wire all into `run_all.sh`)

- **`check_band.py`** — the §8.1 baseband validator.
- **anchor-completeness** — fail if any ledger unit tagged `design.class_anchor = X` belongs to a
  class whose anchor is missing or `signed_off: false` once that class is declared "done" (prevents
  half-finalized classes shipping).
- **new-template registration** — the 4 new vehicle templates (`^LightTank`, `^TankDestroyer`,
  `^AntiAirTank`, `^ArtilleryTank`) + the `^SupportVehicle` redefinition must exist in
  `defaults.yaml` before their class anchors can sign off (boot-gated yaml task).

### 8.6 The one-command runbook (what "anyone can run it" means)

The end-state: a single guarded entry (e.g. `balance` wrapper) runs
`extract_stats --check → check_band → audit_balance_drift → (report)` read-only for a **status**, and
the gated `apply_balance --confirm` for a **write** — both fully deterministic, both refusing to
proceed on a stale `.session` or a red guard. No memory, no tribal knowledge, no per-agent variance.
