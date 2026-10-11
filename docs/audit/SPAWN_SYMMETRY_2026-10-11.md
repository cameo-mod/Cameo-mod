# Spawn symmetry proof — 8 campaign maps (2026-10-11)

Devin Coordinator, complementing `MAP_PREFLIGHT_2026-10-11.md` (capacity-only).
Method: extract `mpspawn` actor cells + `MapSize` from each `.oramap`'s
`map.yaml`; team A = spawns[0..n/2), team B = spawns[n/2..n) as assigned by
`make_jobs`/`seat_assignments`; for each candidate isometry report the
symmetric Hausdorff distance between reflect(A) and B.

**Verdict: all 8 maps are near-perfect mirrors — under DIFFERENT axes.**
Worst offset: 3.16 cells (≈2–3 tiles on 96–144-cell maps).

| map | seats | spawn cells (A side / B side) | best isometry | offset (cells) |
|---|---:|---|---|---:|
| nuclear_winter | 2 | (11,45) / (90,24) | point 180° | 3.16 |
| satans_clutch | 2 | (49,20) / (50,79) | horizontal flip | 1.41 |
| red_spice | 4 | (104,44),(97,62) / (62,97),(44,104) | **diagonal x↔y** | **0.00** |
| terra_cotta | 4 | (32,84),(45,95) / (89,39),(102,50) | point 180° | 1.41 |
| back_to_basics | 6 | top row / bottom row | horizontal flip | 1.00 |
| winters_end_rich | 6 | (6,34),(26,26),(34,6) / (95,123),(103,103),(123,95) | anti-diagonal | 1.41 |
| great_sahara_2 | 8 | (32,11),(25,18),(18,25),(11,32) / (83,64),(76,71),(68,77),(61,84) | anti-diagonal | 3.16 |
| ice_cold | 8 | top-right / bottom-right | horizontal flip | 3.00 |

Findings on the flagged maps:

- **Red Spice**: exact **diagonal** mirror (x↔y), Hausdorff 0.00 — perfect.
  A point-180 check (35.17) made it look asymmetric; it is not.
- **Terra Cotta**: point-180, 1.41 — fine.
- **Great Sahara 2**: anti-diagonal, 3.16 — "near-symmetry" confirmed:
  each A spawn's ideal mirror is ~2–3 cells from the actual B spawn.
  Same magnitude for nuclear_winter (3.16).
- **Ice Cold**: horizontal flip (top↔bottom), 3.00.

Interpretation for the A/B gate:

1. The mirror pairing design already cancels first-order position effects —
   each pair plays both orientations with both arms, so a small systematic
   bias cancels in the *paired* win difference.
2. Residual offsets ≤3.16 cells are spawn-cell only; the remaining layer is
   resource symmetry (distance from each spawn to nearest ore fields), which
   matters specifically for the BU harvester-logistics switch. Recommend a
   per-spawn nearest-ore-distance mirror check before maintainer sign-off;
   it is cheap to compute from the same map.yaml actor list.
3. Engine acceptance (the maps actually loading under the campaign harness
   + seat proofs resolving at runtime) is still pending — this doc proves
   static geometry only.

## Resource-deposit layer (per-spawn distance to nearest mine/gmine deposit)

Deposit actors (`mine`, `gmine`, `ra2mine`, `ra2gmine`) are declared in
`map.yaml` — the A/B outcome measures harvester logistics, so spawn→ore
distance is THE fairness variable. Distances in cells:

| map | A-side spawn→deposit | B-side spawn→deposit | deposit-set mirror | verdict |
|---|---|---|---|---|
| nuclear_winter | 7.8 | 9.4 | 1.41 | mild offset (−1.6) |
| satans_clutch | 10.0 | 10.0 | 18.87* | matched per-spawn |
| red_spice | 36.4, 41.1 | 41.1, 36.4 | 3.16 | exact swap |
| terra_cotta | 16.1, 15.6 | 15.6, 16.1 | 2.83 | exact swap |
| back_to_basics | 6.1 ×3 | 6.1 ×3 | 1.00 | perfectly uniform |
| winters_end_rich | 9.1, 9.9, 9.1 | 9.1, 9.9, 9.1 | 6.08 | exact swap |
| great_sahara_2 | 11.0, 8.1, 11.0, 8.9 | 10.8, 11.4, 9.9, **14.0** | **12.73** | **asymmetric — B-side worst spawn +27% farther** |
| ice_cold | 9.0×3, 33.1 | 11.0×3, 35.1 | 1.00 | mild B-side handicap (~2 cells) |

*satans_clutch's deposit field isn't globally mirror-shaped, but each spawn's
nearest deposit is equidistant — what a harvester sees is matched.

**Great Sahara 2 is the asymmetric cell** (the flagged "near-symmetry" map):
B-side spawns see systematically longer ore runs (14.0 vs A-side max 11.0).
The pair side-swap still cancels team-level bias — both arms cover both
halves — but a spawn×treatment interaction is possible since the switch
measures ore logistics. Maintainer call: keep (note the handicap), drop, or
re-bucket spawns. ice_cold has a milder uniform ~2-cell B-side handicap.

Reproduce: `spawn_geometry2.py` (spawn table) and `resdist.py` (resource
distances) in `C:\cameo-wt\parity-wave1`.
