# BALANCE PIPELINE — the mega plan (v1, 2026-07-18)

_The long-term goal: balance changes become MECHANICAL. No agent (or
human) can silently drift the game's stats, because the pipeline —
not discipline, not document-reading — enforces consistency._

## 0. The principle

Today, balance law lives in `docs/design/cameo_armor_system.xlsx` +
DESIGN.md and is enforced only by agents *choosing* to read them.
The pipeline inverts this: **yaml ⇄ JSON ledger ⇄ generated workbook**,
with the JSON ledger committed to git and an audit that fails whenever
yaml and ledger disagree. Hand-edited balance numbers then show up as
red audit findings mechanically — no matter who edits what.

```
            extract (1)                build (2)
  yaml  ────────────────►  JSON  ────────────────►  cameo_balance_v2.xlsx
 (packs)  ◄────────────── (ledger,   ◄──────────── (generated, formulas
           write-back (4)  committed)   read-back    live in the sheet)
                              │
                              ▼ compare (3)
                    legacy cameo_armor_system.xlsx
                       → discrepancy report
```

## 1. What the recon established (2026-07-18, read-only)

- Workbook tabs: `Armor Types`, `Weapon Types` (constants), then
  TYPE-organized legacy tabs (`Infantry`, `Tanks`, `Vehicles`,
  `Aircraft`, `Defenses`) and ONE modern per-faction tab (`CABAL`).
- **The CABAL tab is the target format** (the maintainer's own
  evolution): columns `Mod | Name | Actor(id) | HP | Speed | Range |
  Damage | WeaponClass | ReloadDelay | DPS | Special | UnitClass |
  TechTier | O | P | Q | Cost | Target Cost`.
- **The recovered formula set** (the balance law, verbatim from cells):
  - `DPS  = Damage / ReloadDelay * WeaponClass`
  - `O = (HP/100000 + Speed/100 + Range*Special/5 + DPS/200) * 200 * UnitClass * TechTier`
  - `P = ((HP*Speed/25000) + (Range*Special*DPS/2.5)) * UnitClass * TechTier`
  - `Q = (HP*Speed*Range*Special*DPS*UnitClass*TechTier) / 12500000`
  - `Price = (O+P+Q)/3`
  - Legacy tabs also use the INVERSE: solve Range (or another stat)
    backward from a target Cost — i.e. the sheet supports both
    "price the unit" and "fit the stat to the price" workflows.
- Legacy tabs have NO actor ids (display names only, pre-rename) —
  the comparator needs a name→id mapping table.
- Existing code to reuse: `tools/audit/cameo_model.py` (full resolved
  ruleset), `gen_damage_matrix`, `audit_stat_formulas`,
  `audit_balance_sheet` (absorbed by the pipeline when done).

## 2. Layout decision: ONE JSON PER FACTION (internal type sections)

`docs/balance/<theme>_<faction>.json` — e.g. `redalert2mod_tkm.json`,
plus `shared_<theme>.json` for theme-shared actors and `core.json`
for cross-theme/neutral. NOT one file per type per faction.

Why per-faction (recommended):
- Mirrors the ContentPack = one faction = one loading unit = one
  balance-review unit. A faction rebalance is one file diff.
- ~30 files instead of ~450; git history stays readable.
- The type split lives INSIDE the file as sections mirroring the
  pack's closed yaml set (`infantry`, `vehicles`, `aircraft`,
  `defenses`, `buildings`, `naval`, `upgrades`…), so nothing is lost
  versus the per-type alternative.

Why central `docs/balance/` and not inside each ContentPack: the JSON
is DERIVED tooling data (a ledger), not mod content the engine loads;
keeping it out of the packs keeps pack folders shippable and makes
cross-faction tooling trivial. (Revisit only if the ledger ever
becomes engine-loaded.)

## 3. JSON schema (sketch — Phase 1 freezes it as a JSON Schema file)

```json
{
  "schema": 1,
  "faction": "tkm",
  "pack": "ContentPacks/RedAlert2Mod/TKM",
  "generated_from": "<git rev>",
  "sections": {
    "infantry": {
      "tkm_rifleman": {
        "name": "Rifleman",
        "cost":       {"v": 100,   "src": "yaml/infantry.yaml#Valued.Cost"},
        "hp":         {"v": 14000, "src": "yaml/infantry.yaml#Health.HP"},
        "speed":      {"v": 58,    "src": "..."},
        "armor":      {"v": "None","src": "..."},
        "tier":       {"v": 1,     "derived": "prerequisites"},
        "unit_class": {"v": 0.5,   "sheet_input": true},
        "special":    {"v": 1,     "sheet_input": true},
        "weapons": [{
          "id": "tkmrifle", "damage": 2000, "reload": 20,
          "weapon_class": 0.75, "range": 5.0,
          "src": "yaml/weapons.yaml#tkmrifle"
        }],
        "build_limit": null, "prerequisites": ["~tkm_barracks"],
        "gated_by": {"promotion": null, "upgrade": null}
      }
    }
  }
}
```

Key properties:
- **Every value carries a provenance anchor** (`src`: file + trait
  path) so write-back is surgical and needs no guessing.
- **Sheet-input fields** (`unit_class`, `special`, `weapon_class`,
  `tech_tier`) are design judgments, not yaml facts — they SEED from
  the legacy sheet during Phase 3 and afterwards live in the ledger
  as the single source.
- Deterministic serialization (sorted keys, one value per line) so
  git diffs are minimal and mergeable across concurrent agents.

## 4. The five phases

**Phase 1 — Extractor** (`tools/balance/extract_stats.py`) — effort M
- cameo_model resolves every faction roster (post-Inherits); emit the
  per-faction JSONs; JSON-Schema validation; `--check` mode re-extracts
  and diffs against committed ledger (exit 1 on drift).
- Verification: extract twice = byte-identical; spot-check 10 units
  against yaml by hand; commit the baseline ledger.

**Phase 2 — Workbook builder** (`tools/balance/build_workbook.py`) — effort M
- Generates `docs/design/cameo_balance_v2.xlsx` from the ledger:
  - `Constants` tab: armor/weapon class tables + formula coefficients
    in NAMED cells (the one place to tune the law);
  - one tab per faction in the CABAL-tab format, one row per unit,
    raw stats as values, O/P/Q/DPS/Price as REAL Excel formulas
    referencing Constants (so the maintainer's set-M-watch-OPQ
    workflow survives);
  - Delta columns: `Price(formula) − Cost(actual)` with conditional
    formatting (green within ±10%, amber ±25%, red beyond);
  - cell protection: everything locked except the designated input
    columns — fat-fingering a computed column is impossible.
- The same formulas implemented ONCE in `tools/balance/formula.py`;
  equivalence test: python evaluation == Excel evaluation for every
  row (openpyxl reads the computed values after a LibreOffice/Excel
  recalc pass, or we evaluate with `formulas` lib).

**Phase 3 — Legacy comparator** (`tools/balance/compare_legacy.py`) — effort M–L
- Reads the OLD workbook (respecting the `~$` lock law), builds the
  display-name → actor-id mapping (auto-match + a committed manual
  `name_map.yaml` for the rest — the legacy tabs predate the renames).
- Report `docs/balance/discrepancies.md`:
  (a) yaml units absent from the legacy sheet (never priced),
  (b) legacy rows with no living actor (dead rows),
  (c) value mismatches yaml-vs-sheet per stat with severity,
  (d) formula-price vs actual-cost outliers (replaces
      audit_stat_formulas' price section).
- Maintainer triages ONCE: for each mismatch, which side is law.
  The verdicts seed the ledger's sheet-input fields.

**Phase 4 — Write-back** (`tools/balance/apply_balance.py`) — effort M
- `apply_balance.py --faction tkm [--fields cost,hp] [--from-workbook]`
  writes ledger values into yaml via the provenance anchors, prints a
  human diff, refuses to run on a dirty faction file set.
- **Gated: runs only on explicit maintainer order** (the balance law
  stays: numbers move sheet-first, then yaml — now automated).
- Round-trip invariant test: extract → write-back → extract is a
  fixed point (byte-identical ledger).
- Boot gate + audit suite are part of the command's checklist output.

**Phase 5 — Enforcement** — effort S
- `audit_balance_drift` joins run_all.sh: re-extract and diff vs the
  committed ledger → any hand-edited yaml stat = red finding with the
  exact file/line and the ledger value.
- DESIGN.md gets the new law: agents NEVER hand-edit balance numbers;
  the only path is ledger/workbook → apply_balance.py. CLAUDE.md
  balance section updated to point here.
- audit_stat_formulas + audit_balance_sheet retire into the pipeline.

## 5. Risks & mitigations

- **Formula archaeology wrong** → Phase 2's equivalence test uses the
  LEGACY workbook's own computed values as ground truth for overlap
  rows before trusting the reimplementation.
- **Name mapping (renames)** → manual `name_map.yaml` is committed and
  reviewed; unmatched rows are listed, never guessed silently.
- **Rounding conventions** (Cost granularity 25/50, HP steps) →
  captured as Constants-tab values; write-back rounds identically.
- **Workbook open in Excel** (`~$` lock) → every tool checks the lock
  and queues, per the standing law.
- **Concurrent agents** → deterministic serialization + drift audit
  makes collisions visible immediately; the ledger merges line-wise.
- **Stat scale anomalies** (e.g. SM's ×10 HP outliers found by
  audit_stat_formulas) surface in Phase 3 as mismatch class (d) and
  get fixed through the pipeline, not ad hoc.

## 6. Sequencing & first customer

Phases land in order 1 → 2 → 3 (each is independently useful); 4–5
follow once the maintainer signs off the Phase 3 triage. **The SM full
rebalance (ROADMAP P1) becomes the pipeline's first customer**: its 38
known findings are re-derived by Phase 3 and fixed via Phase 4 —
proving the loop end-to-end on exactly the faction that needs it.
