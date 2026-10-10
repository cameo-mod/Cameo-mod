# A6 — per-module performance baseline (2026-10-10)

Coordinator runtime evidence for task `01a12440` (A5+A6). **Baseline, not a
release claim**: six bounded mirror duels, `ModulePerfReportIntervalTicks: 300`
on `ModularBot@HardAI` (already shipped in `mods/cameo/ai/ai.yaml`), reports
harvested from `<support>/Logs/debug.log`.

## Provenance

| Axis | Value |
|---|---|
| Tree A (partial) | `devin/a1-bot-rng` @ `163dad36b`, engine `0a3f77dbe`, worktree `C:\cameo-wt\ra-refinery-dock` |
| Tree B (baseline) | `devin/c1-annulus-range-cap` @ `9e00eabf3`, engine `0a3f77dbe`, worktree `C:\cameo-wt\c1-annulus-cap` |
| Cells | `td_gdi`/`td_nod` hard-vs-hard mirrors × gate fixture + `td_gdi` × Imminent/Nuclear-Winter, seed pinned via `CAMEO_DEV_SEED` in-process |
| Bounds | Lua cap 15000t (all players fail simultaneously), 900s wall backstop, `--render fast` |
| Driver | `coord_gate_batch.py` cells via `a6_league.py`; harvest `a6_reharvest.py` |

## Why the baseline tree is `c1-annulus-cap`, not the A1 tip

Tree A crashed in **4 of 6 cells** — every crash the SAME signature already
fixed on the C1 branch: `System.ArgumentOutOfRangeException:
FindTilesInAnnulus maxRange (57|62|67|68) > 50` from
`BaseFrontBackPlannerBotModule.Refresh→RadarPowerMargin`. The C1 annulus trap
is **not** Imminent-specific and **not** oramap-specific — it fired on the
gate fixture too (3/4 gate cells on A1). This is additional C1 evidence on
the record; the surviving-tree league gives the only complete windows.

## Per-module table (c1-annulus-cap tree — 6/6 clean cells, 624 reports)

Rows = module; value = mean ms per 300-tick report window, pooled per cell.
`total` = summed ms across the whole cell; `n` = report windows containing
the module.

| Module | gate gdi s1337 | gate gdi s20261006 | gate nod s1337 | gate nod s20261006 | imm gdi | nuke gdi |
|---|---|---|---|---|---|---|
| MasterAiBotModule | 31.2 (3309/106) | 32.9 (3384/103) | 32.0 (3324/104) | 30.8 (3263/106) | 32.6 (3387/104) | 32.8 (3409/104) |
| BaseBuilderBotModuleCA | 16.7 (1774/106) | 13.3 (1378/104) | 15.1 (1565/104) | 12.4 (1315/106) | 9.6 (995/104) | 12.6 (1314/104) |
| SquadManagerBotModuleCA | 4.4 (461/105) | 5.3 (524/98) | 3.7 (369/101) | 3.9 (416/106) | — | 4.6 (452/98) |
| ExpansionPlannerBotModule | 3.8 (361/96) | 3.7 (362/98) | 3.1 (310/100) | 3.6 (354/99) | 5.0 (483/96) | 4.0 (356/88) |
| UnitBuilderBotModuleCA | 9.3 (353/38) | 11.8 (353/30) | 8.6 (345/40) | 7.2 (345/48) | — | — |
| FransGeneralBotModule | — | — | — | — | — | 8.4 (395/47) |
| FransCommandBidBotModule | — | — | — | — | 4.0 (365/92) | — |
| TacticalMapBotModule | — | — | — | — | 39.1 (430/11) | — |
| EngagementLogBotModule | 57.4 (459/8) | 76.2 (457/6) | 117.0 (468/4) | 112.1 (448/4) | 488.2 (488/1) | 263.6 (527/2) |

(Each cell also shows a long tail of <5 ms modules — PlugSpawner,
Harvester, Scout, MineCluster, BuildOrderKnobs, Frans* support modules —
recorded in `a6-league-c1fix/a6_raw.json`.)

## Read-out

- **`MasterAiBotModule` is the dominant consumer in every cell** (~31–33 ms
  per 300-tick window ≈ ~10% of a 1 ms/tick frame budget), then
  `BaseBuilderBotModuleCA` (~10–17 ms). Everything else sits <12 ms except
  rare spikes.
- **`EngagementLogBotModule` is bursty, not heavy on average**: appears in
  only 1–8 windows per cell but at 57–488 ms/window — consistent with
  buffered log flush. Worth its own line in any future perf work.
- `TacticalMapBotModule` on Imminent showed 39 ms mean across 11 windows —
  the only map-correlated module in the top tier.
- A1-partial data (328 reports) shows the SAME ordering (MasterAi →
  BaseBuilder → tail); its higher means (54–180 ms) are survivorship-skew —
  crashed cells only recorded the first/init-heavy windows.

## Methodology / caveats

- Each `AI (n): module timing` line = ms spent inside that module's
  `BotTick` over the preceding 300 world ticks, top-8 by cost
  (`ModulePerfReportTop: 8`); modules below the cut are not counted —
  `total` underestimates full AI cost.
- Mirror hard-vs-hard duels only; no classic-bot or human-load numbers;
  first windows carry init cost (one 3033 ms MasterAi window observed).
- Log lines use comma decimals; parser `a6_reharvest.py` normalizes.
- No performance verdict is claimed; this is the reference table future
  changes should be measured against.

## Post-merge master baseline (added same day, reviewer suggestion)

Re-run on the fac-stale-labels tree = master `10d3f44f7` (+ runtime-neutral
faction fix, engine `6da7fce14` — the master pin, C1 capped path in force).
**6/6 clean, 614 reports**, incl. the two cells that died on the A1 tip.
Distribution CHANGED vs the pre-merge base — worth a line of its own:

| Module | gate gdi | gate nod | imm gdi | nuke gdi |
|---|---|---|---|---|
| ExpansionPlannerBotModule | 27.5 / 22.3 | 30.0 / 23.5 | **115.4** | 41.3 |
| BaseBuilderBotModuleCA | 9.6 / 9.3 | 9.4 / 8.0 | 7.7 | 9.8 |
| MasterAiBotModule | 8.0 / 8.6 | 8.1 / 7.4 | 7.6 | 11.1 |
| TacticalMapBotModule | 10.1 / 11.2 | 12.8 / 12.0 | 23.3 | 12.6 |
| EngagementLogBotModule | 45.4 / 33.2 (12-15w) | 18.3 / 39.0 (27/13w) | 126.6 (4w) | 69.5 (7w) |
| EngineerBotModule | 11.7 / 14.1 (30-36w) | — | 21.8 (19w) | — |

- `MasterAiBotModule` fell from ~32 to ~8 ms/300t — no longer dominant.
- `ExpansionPlannerBotModule` is now the top consumer everywhere and is
  sharply map-correlated (**115 ms/300t on Imminent** vs ~23–30 on the
  fixture) — likely interacts with merged MCV/expansion work since the old
  base (lease/prebuild changes) or engine `6da7fce14`. Observation, not a
  verdict — flagged for the next perf pass.
- **Attribution (peer NOTE_2026-10-10_devin_b3_expansionplanner_perf.md +
  counter check)**: ExpansionPlanner hosts the B3 coverage model — entirely
  new per-tick work (`EvaluateCoverageTick`, `CoverageProbesPerTick=8`,
  `CoverageProbeLimit=64/refresh`, route legs ≤10, radius site enumeration
  + per-generation PatchCells memo). Cost scales anchors×sites×field-size,
  fitting the Imminent correlation. Counter evidence from these logs:
  `REPAIR-B3 coverage ...` lines show the **64-probe budget exhausted on
  EVERY ceiling event in all six cells** (spent=64/64, n=78) alongside
  `N anchors pending` pacing lines — probe-dominated, so
  `CoverageProbesPerTick` is the natural throttle if tuning is ever asked.
- **MasterAi 32→8 resolved (same-day window decomposition)**: the gap is
  concentrated in the early windows — per-cell first-third means ~87–92 ms
  (max ~2900 ms, init) on the pre-merge base vs ~20–30 ms (max ~600 ms) on
  master; mid/late windows are ~3–4 ms vs ~1–2 ms on BOTH trees. So
  MasterAi was never the dominant *sustained* cost — the pooled mean was
  init-dominated; master's merges and/or engine halved the startup spike.
- Engine differs too (`6da7fce14` vs `0a3f77dbe`); the shift cannot be
  attributed to mod merges alone from this data.
- The C1 capped path held through the whole league — zero `FindTilesInAnnulus`
  exceptions on master.

## Artifacts

- Raw: `C:\cameo-wt\parity-wave1\a6-league{,-c1fix,-master}\a6_raw.json`, per-cell
  `Logs/debug.log`, `batch_results.jsonl`, exception logs (A1 cells).
- Exception logs (4×): `a6-league/*/Logs/exception-2026-10-10T*.log` — all
  `FindTilesInAnnulus maxRange>50` via `BaseFrontBackPlannerBotModule`.
