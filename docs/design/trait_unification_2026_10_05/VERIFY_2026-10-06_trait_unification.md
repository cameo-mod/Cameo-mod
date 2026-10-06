# Verification log — trait unification spec (2026-10-06)

Verifier: Devin-Architect, task `01a10dcd-55b6-7d10-96a8-4a60b7d99d56` (reopened).
Subject: `SPEC_2026-10-05_trait_unification.md` (Boss, frozen capture).

Method: every factual claim in the spec is re-checked against source — the Cameo
repo at baseline `b6f522e78d41c68649e66e315363a42fc0bb683c` (immutable git
objects), the pinned engine `d5d8b2a6853bff3b5a00cc5db7d86b03d4f30684`, the six
donor clones at their recorded SHAs, and the frozen captures
`C:/cameo-wt/boss_trait_full_capture.json` and `boss_trait_catalog.json`.
Independent recomputation is preferred over trusting the capture; where a claim
can only be evaluated by re-measuring, the measurement is recorded.

Status legend: **VERIFIED** (claim matches source), **CORRECTED** (claim is
wrong or imprecise; correction recorded), **UNVERIFIABLE** (claim cannot be
established from available evidence).

---

## §1 Evidence, scope and provenance

| # | Claim | Status | Evidence |
|---|---|---|---|
| 1.1 | Cameo measured revision is `b6f522e78d41c68649e66e315363a42fc0bb683c` | VERIFIED | `git rev-parse b6f522e78` → same; equals `origin/inc/2026_10_05` tip in `Cameo-mod` repo. |
| 1.2 | Engine pin `d5d8b2a6853bff3b5a00cc5db7d86b03d4f30684` in `mod.config` | VERIFIED | `git show b6f522e78:mod.config` → `ENGINE_VERSION="d5d8b2a6853bff3b5a00cc5db7d86b03d4f30684"`. |
| 1.3 | Main checkout was `master@0fd6ec67e`, doc-only atop `3ba05ede7` (INC-e) | VERIFIED | `master` = `0fd6ec67e3bb7de479bba3d3fcb85aaa865d692b`; parent `3ba05ede7`; diff to parent = `docs/CAMEO_DESIGN_FRAMEWORK.md` +1215 only. |
| 1.4 | Master's tree lacks later trait changes present in `b6f522e78` | VERIFIED | `diff 0fd6ec67e b6f522e78` = 117 `.cs` + 42 non-cs files (incl. `mods/cameo/ai/ai.yaml`, `mod.yaml`, squad/lease code). |
| 1.5 | 66 source files changed/added when `fe4459c9c` was substituted | VERIFIED | `diff --diff-filter=ACMR 0fd6ec67e fe4459c9c -- *.cs` = 103 `.cs` files, of which **66 non-test production sources** (excl. `OpenRA.Mods.Cameo.Test/`). |
| 1.6 | `b6f522e78` ruling updated 11 changed/added source files + `mods/cameo/ai/ai.yaml` | VERIFIED | `diff fe4459c9c b6f522e78` = 20 files: exactly **11 non-test `.cs`** (squad family), 5 test `.cs`, `ai.yaml`, `increment_switches.yaml`, `DEVELOPMENT_LOG.md`, `AI_ARCH_COVERAGE.md`. |
| 1.7 | Donor captured SHAs (7 rows of the source table) | VERIFIED | All clones in `C:\Users\AedisToru\Documents\GitHub` sit at exactly the recorded SHAs, `git status --porcelain` empty for all six donors: CAmod `f31049d2d…`, RV `231059963…`, SP-SDK `124b7054a…`, CN `30cf70a66…`, Generals-Alpha `70154e6a9…`, upstream OpenRA `f3ec7f8e1…`. |
| 1.8 | `cameo-engine` local HEAD was `486374dcc7` at capture; not substituted for the pin | VERIFIED | Engine reflog: `486374dcc7` was HEAD before `d1448475fb` (committed 2026-10-06 for MISSILE-RANGE-PCT, after the capture). Capture `repo_heads` records `486374dcc7…` for `cameo-engine` while provenance used pinned `d5d8b2a685…` objects. Claim was accurate at capture time. |
| 1.9 | Donor checkouts were clean | VERIFIED | `git status --porcelain` = 0 lines for all six donors (2026-10-06 re-check). |
| 1.10 | 2,317 YAML-facing declaration candidates in 4,504 source files | VERIFIED | `boss_trait_full_capture.json`: `types` = 2,317 entries; `texts` = 4,504 source files. |
| 1.11 | Comparison appendix has 1,754 rows | VERIFIED | `boss_trait_catalog.json`: `comparison` = 1,754 rows; inventory md table rows match. |
| 1.12 | 73 loaded declarations have CA/AS/RV/SP/TA-style tags | CORRECTED | Reproducible count is **68** suffixed type declarations in loaded assemblies (CA 48, Cameo 10, AS 10); **70** when also counting two AS warheads whose YAML name is suffixed but Info class is not (`ChangeOwnerAS`, `FireClusterAS`). 38 of the 68 are mounted (USE>0); 30 are source-only. No interpretation of "loaded" reproduces 73 — closest consistent reading is 70. |
| 1.13 | Uncovered intake = 263 donor declarations: CA 123, CN 77, SP 32, RV 10, Generals 21 | VERIFIED | `catalog.uncovered` = 263 rows; per-donor `asm` counts exactly CA 123 / CN 77 / SP 32 / Generals 21 / RV 10. |
| 1.14 | `tools/audit/type_merge_inventory.py` exists; scanner skips donor classes whose names occur locally, omits CN and the core Game assembly | PENDING | existence certain; behavioural claims to re-check vs script source. |
| 1.15 | USE = resolved non-template trait instances / projectile instances / warhead instances; `@instances` count separately; assembly-shadowed = 0 | PENDING | verify usage map semantics vs capture fields. |

## §2 MissileCA

| # | Claim | Status | Evidence |
|---|---|---|---|
| 2.1 | 152 `Projectile: MissileCA` declarations in 22 files | VERIFIED | `catalog.missileca_literals` = 152 rows across exactly 22 distinct files. |
| 2.2 | Zero of the 22 files in the manifest weapon list; zero resolved weapons use MissileCA | VERIFIED | All 152 literals have `mounted: false`; `usage.projectile` has no `MissileCA` key (24 resolved projectile kinds). |
| 2.3 | No `MissileCAInfo` in loaded assemblies | PENDING | verify no such class under `OpenRA.Mods.*` at baseline + pinned engine. |
| 2.4 | Donor impl at `CAmod/OpenRA.Mods.CA/Projectiles/MissileCA.cs:26` | PENDING | verify file/line in CAmod clone. |
| 2.5 | Per-file declaration table (28/22/18/15/11/10/9/7/6/5/3/2×7/1×4) | VERIFIED | Capture per-file counts match the spec table exactly, including both `advacewars.yaml` (10) and `advancewars.yaml` (9) as separate files. |

## §3 Destination architecture

| # | Claim | Status | Evidence |
|---|---|---|---|
| 3.1 | `mods/cameo/mod.yaml:431` orders AS → CA → Cameo → Cnc → D2k → Common → Fransbot | PENDING | |
| 3.2 | `ObjectCreator` prepends OpenRA.Game before manifest assemblies (`ObjectCreator.cs:36–43`) | PENDING | |
| 3.3 | The listed ~40 `IBot*`/`IHasParallelQueueSlots` contracts live in `OpenRA.Mods.CA.Traits` | PENDING | |
| 3.4 | Intro commits: `ConcaveEvalCA` a75b65609, `SquadMicroEvalCA` aecb69059, `IBotFrontBackAdvisor` 63cd1c372, `IBotUnitLeases` f7239386c | PENDING | |
| 3.5 | `PlugSpawnerBotModuleCA` exists and donor AS has a real `PlugSpawnerBotModule` counterpart | PENDING | |

## §4 Family contracts (per-row)

PENDING — each table row will be checked against donor+base source; field-level
claims cross-checked against the capture's `added`/`changed`/`absent_direct` maps.

## §5 Uncovered overlaps

PENDING.

## §6–§7 Migration/tooling claims

PENDING — UpdateRule hook existence, tool scripts, boot gate script.

## Inventory spot-check coverage (≥10% per family)

PENDING — stratified sample of `comparison` rows per family/donor, each checked
against donor source text.

---

## Corrections ledger

1. **§1 "73 loaded declarations"** → reproducible counts are 68 (type-name
   suffix) / 70 (incl. YAML-suffixed warhead names); logged as CORRECTED above.

(work in progress — appended as verification proceeds)
