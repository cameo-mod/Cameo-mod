# Baseline Audit — Summary

_One page. Details: [FINDINGS.md](FINDINGS.md) · raw tables: [baseline/](baseline/) ·
faction map: [../factions/MATRIX.md](../factions/MATRIX.md)._

## Counts by bug class

| class | what | count (live tree) | severity profile |
|---|---|---|---|
| B8 | crash-class content | **0** distinct (was 3+ — fixed 2026-07-14: ts_nod_ticktank voxel, magicnuke sequence, ra2_cgtbnkbb/ctoutpbb missing assets; 2026-07-15: CABAL CreateEffect Image: fields removed, impact animations consolidated in misc.yaml, map actors renamed; 2026-07-24: RA2 weapons migrated to ContentPack, Yuri weapons headers restored, Naxis Kübelwagen encoding fixed, nuclearflash shader created) | crash |
| B1 | cross-faction leaks | 10 L1 + 13 L3 (+1,106 shared needing owners) | balance |
| B2 | illegal inherits | **328** concrete→concrete, 24 cross-faction, 0 dangling | balance-risk |
| B5 | AI wiring | **200** ids defined nowhere, 620 unloaded refs, 26 factions with unwired units | balance |
| B3 | upgrade direction | 12 anti-buff combos (2 suspicious, 1 verify, rest intended drawbacks), 4 dead upgrades, 5 dead-wiring families on 300–1,042 actors each | balance |
| B4 | upgrade coverage | 15 tracked upgrades, ~40 real uncovered combat slots (CABAL backup systems: legion+avatar fixed but backup actors still needed) | balance |
| B6 | art/sequence refs | 11 missing images, 11 missing sequences, 542 orphan images | cosmetic→crash-risk |
| B7 | metadata rot | 24 duplicate-tooltip groups, 0 missing tooltips | cosmetic |
| B9 | numeric drift | bounds screen **clean** (TB23 fix held); 163 outlier leads | balance-minor |
| B10 | dead content | 345 orphan weapons, 542 orphan images, 16 dead conditions | hygiene |
| B11 | asset norms | 3,632 / 8,776 WAVs off-norm (mono/16-bit/22050 Hz); 131 PNGs over budget | hygiene |
| B12 | localization | 0 unresolved Fluent refs, 233 orphaned messages, ≤10% Fluent coverage | cosmetic |
| R2 | stacked multipliers | **757** units over the 2.0× budget; worst 36× (RA2 Allies) | balance |
| W | weapon uniqueness (DESIGN §10) | 36 same-faction + 42 cross-faction shared weapons; 95 carrier-only (IFV borrow, informational) | design/identity |
| G | garrison weapons (DESIGN §11) | **clean** (G1/G2/G3 = 0 after 2026-07-10 fixes; 30 design exceptions) | crash-free/balance |

## Top 20 findings

1. **`tatacitus` NukePower fires nonexistent `TSChemTacticalMissile`** — FIXED: changed to existing `TSTacticalChemMissile` with valid `tsnodmmsil` image (tiberiaalliances.yaml).
2. **RA2 Allies hero-infantry stack measures 36×** fresh-self power (Assault Squad + Vanguard + Infiltrators + Chromium/Prismatic lines) — worst in game; Yuri 30× behind it.
3. **ai.yaml: 200 references defined nowhere** — incl. `ra2naclon`, `nax2_chrono` (CABAL refs `tsgtcnstcabalb`/`tsntpulscabal` already removed).
4. **Stale "BuildingFractions Dune Universe" block** uses pre-ContentPacks names — entire section steers nothing.
5. **`raider.ordos` not in any AI build list** — FIXED: added to Dune Universe `UnitsToBuild` with weight 7 (also `runner.steel`, `orion.futu`, `yrrobo.futu`, 5 Naxis units still pending).
6. **`ra_doctrine_teslatech` doubles reload (Modifier 200) on 2 actors** — suspected Dark-Armament-class inversion; verify.
7. **`up_energizedarrows` has ReloadDelayMultiplier 125** on one actor — suspected inversion; verify.
8. **328 concrete→concrete inherits** — the Slave-Miner bug factory; Phase-1 queue, full grouped list in FINDINGS.
9. **13 L3 leaks**: CABAL/Forgotten/TS-Nod buildings inherit GDI/Nod concrete actors (tscabaltech→tsgttech etc.).
10. **Modern Fire Control Systems covers 15/33 of TS GDI** — all aircraft + half the infantry lack the roster-wide hook.
11. **WC2 tower upgrade names** (guard↔cannon swap) — FIXED; remaining 24 duplicate-tooltip groups still under review.
12. **Dead-wiring families on 1,042 actors each** (`usabombardament`, `usaholdtheline`, `usasearchndestroy`, `upsubliminal(2)`) + `upra2deso` on 302 — Generals-era hooks granted by nothing.
13. **3 player-visible raw Fluent keys** — STALE/RESOLVED: current `audit_fluent.py` F1 shows 0 unresolved refs.
14. **`wc2_orc_eye_of_kilrogg` TurnSpeed 2048** — FIXED: reduced to 28 (bounds screen still clean; high vision range remains by design as scout).
15. **345 orphan weapons + 542 orphan sequence images** — RAM/load-time dead weight.
16. **CABAL absent from Random AND Tournament pools, still titled "(WIP)"** — FIXED: CABAL added to both pools, WIP label removed.
17. **CABAL post-TB23 full stack = 3.9×** — over the 2.0 budget but sane; trim one Research multiplier or cap rank scaling.
18. **_old.yaml deprecated files removed** — tiberiansunold + warcraft2old rules/sequences/weapons deleted (20,919 lines).

## Recommended fix order (per MASTER_REPORT §4)

1. **B5 AI wiring** (items 3–5) — restore bot competence for pool factions; delete/fence the 620 unloaded refs.
2. **B3/B4 verify+fix** (items 6, 7, 10) and transcribe the remaining 526 `upgrades_intent.yaml` entries.
3. **B2+B1 structurally** via §12 Phase-1 per-faction migration (items 8–9), `dump_resolved.py`-verified; turn `audit_inherits` blocking in CI as factions land.
4. **B7/B9/B12 quick wins** (items 11, 13, 14) — ideal AI-agent batch work.
5. **B10/B11 hygiene** (items 15, 18) — orphan purge + per-directory WAV normalization; deprecated *_old.yaml files already removed.
6. **R2 rebalance** (items 2, 17) with tournament telemetry before touching Consortium-family numbers.
