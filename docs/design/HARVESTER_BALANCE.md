# Harvester balance — the six-parameter income model

**Status:** design reference. The model and every number below are derived from the
tree by `tools/balance/harvester_income.py`; re-run it rather than trusting this page.
Target bands in §5 are a PROPOSAL awaiting maintainer sign-off.

Harvesters are one of the balance-formula exceptions (`docs/design/balance_exceptions.yaml`):
income per second, not Cost/DPS/HP, is what a harvester is for, so the class formula
in `DESIGN.md` §12 does not price them. This page is the substitute.

---

## 0. Why the in-game test showed "great variance"

The maintainer's field test — on Nuclear Winter, build a power plant then three
refineries as close to the starting ore as possible, and time how long until the
*silos needed* message — has three confounds that produce spread on their own,
before any harvester difference is measured.

**(a) The storage cap is not the same number for every faction, and it grows as you
build.** 84 actors carry `StoresPlayerResources`:

| grantor | capacity | count |
|---|---|---|
| refineries | 4000 | 38 |
| silos, and `tkm_powerplant`, `protoss_pylon`, `zerg_overlord`, `zerg_broodweaver`, `terran_supplydepot`, `wc2_humans_farm`, `wc2_orcs_pigfarm` | 2000 | 13 |
| **every harvester** | **50** | **33** |

So "time to full" is time to fill a cap that differs per faction (TKM's power plant
alone adds 2000, +17% on three refineries) and that rises by 50 with every harvester
the player or the refinery adds. Swarm factions raise their own target the fastest.

**(b) Game speed rescales "seconds".** `mod.yaml` `GameSpeeds` default is
`Timestep: 40` = **25 ticks/second**; at *Fast* it is 33/s. The run must be pinned
to Normal or the denominator is not comparable.

**(c) The free fleet is not one harvester.** A refinery hands out between **1 and 6**
(§3). Whether that belongs in the measurement is a design choice — it is real in a
match — but it must be a deliberate one.

Refinery **cost and build time are identical across every faction** (3000 credits,
750 build duration), so that axis is clean and needs no correction.

### The official protocol (maintainer 2026-08-29 — RULED)

> *"The official testing protocol for economy profiling is now credits harvested
> over a fixed 5-minute window."*

Same map and build order as the field test, but measuring a **rate over a fixed
window** instead of a time to a moving threshold:

1. Game speed **Normal** (`Timestep: 40` = 25 ticks/sec). Single player, no bots,
   `DefaultCash: 12345`.
2. Power plant, then three refineries, placed as close to the starting ore as the
   footprint allows. Build nothing else.
3. At a fixed mark **T0 = 3:00** (all three refineries complete and saturated for
   every faction), record credits. At **T1 = 8:00**, record credits again.
4. **income = (credits(T1) − credits(T0)) / 300 seconds.**
   Spending nothing between the marks keeps the storage cap out of the
   measurement; if the cap is still reached, extend with silos and say so.
5. Repeat with the refineries placed at the **far** ore field for the long-range
   figure. Both numbers are needed — one alone is not a balance measurement (§4).

### Canonized model inputs (maintainer 2026-08-29 — RULED)

Three mechanics are officially recognised as the primary drivers of faction
economic divergence, and any economy model that omits one is incomplete:

* **`HarvesterBalancer`** — the +38% speed within 5 cells of a refinery (§1).
* **Multiple `DockHost` traits** — unload concurrency (§1 parameter 5).
* **Free refinery fleets** — the harvesters a refinery gives away (§1 parameter 6).

And: **`Capacity` must strictly follow the yaml.** No tool may carry its own
copy. `harvester_income.py` reads it from `StoresResources` on the resolved
actor; `harvester_table.py`'s hardcoded 100 for the Noid harvester against a
yaml value of 50 is exactly the failure this rule prevents.

This is deterministic: OpenRA's simulation is lockstep, so the same build order on
the same map yields the same numbers every run.

---

## 1. The six parameters

Four belong to the harvester, two to the refinery. The maintainer's count of five
was one short: the sixth is the free fleet size, and it is the single largest term.

| # | parameter | trait | role |
|---|---|---|---|
| 1 | `Capacity` | `StoresResources` (**not** `Harvester`) | bales per trip |
| 2 | `BaleLoadDelay` | `Harvester` | ticks per bale at the field |
| 3 | `BaleUnloadDelay` (with `BaleUnloadAmount`) | `Harvester` | ticks per batch at the dock |
| 4 | `FullyLoadedSpeed` | `Harvester` | % of `Mobile.Speed` on the loaded leg |
| 5 | **`DockHost` count** | refinery | how many harvesters unload **at once** |
| 6 | **free fleet size** | refinery `FreeActor*` + `SlaveMinerSpawnerMaster` | harvesters the refinery gives away |

⚠ `Capacity` lives on `StoresResources`, not on the `Harvester` trait. Grepping
`Harvester:` for it finds nothing.

⚠ **Cell sharing is not what makes swarm workers unload together.** Infantry
harvesters do share a cell (`world.yaml` locomotors with `SharesCell: true`), but
simultaneous *unloading* requires multiple `DockHost` traits on the refinery.
`MaxQueueLength` is how many may **queue**, not how many may unload — a refinery
with one dock and `MaxQueueLength: 15` still serialises all fifteen.

### The seventh term, already in the tree and easy to miss

Every one of the 33 harvesters inherits from `^Harvester` (`defaults.yaml`):

```
SpeedMultiplier@HARVBALANCE:
    RequiresCondition: harv-balance
    Modifier: 138
HarvesterBalancer:
    Condition: harv-balance
```

`HarvesterBalancer` (`OpenRA.Mods.CA/Traits/HarvesterBalancer.cs`) grants its
condition while the harvester is within `MaxDistanceFromRefinery` — **CA default 5
cells**, and no actor in the tree overrides it. So the first and last five cells of
every trip are travelled **38% faster**, uniformly, for every harvester in the mod.

Note the sign: it rewards being **close** to the refinery, so it *widens* the gap
between short-range and long-range mining rather than closing it. If the goal is to
flatten the two distances, this is the lever, and it currently points the wrong way.

---

## 2. The model

For one harvester at mining distance `D` cells:

```
T_load   = Capacity * (BaleLoadDelay + 1)
T_unload = ceil(Capacity / BaleUnloadAmount) * BaleUnloadDelay + 1
T_travel = leg(D, v_empty) + leg(D, v_loaded)
T_cycle  = T_load + T_unload + T_travel

v_empty  = Speed * terrain% / 100
v_loaded = Speed * FullyLoadedSpeed% / 100 * terrain% / 100
leg(D,v) = min(5,D) * 1024 / (v * 1.38)  +  (D - min(5,D)) * 1024 / v
```

A refinery's fleet is capped by whichever binds first — the cycle or the dock:

```
income = min( N / T_cycle , docks / T_unload ) * Capacity * ResourceValue
```

and the crossover is the **dock saturation point**:

```
N_saturate = docks * T_cycle / T_unload
```

Past `N_saturate`, extra harvesters queue and earn nothing. `ResourceValue` comes
from `player.yaml` `PlayerResources.ResourceValues` (Ore 25, Tiberium 30, Spice 75,
D2SpiceDense 125). Every faction's `Resources:` list is identical, so on a given map
the value is a shared constant and cancels out of cross-faction comparisons.

Engine provenance for the tick arithmetic is cited in the tool's docstring. `engine/`
is not part of this repo, so those line references cannot be re-checked from a
mod-only checkout — re-verify them from a complete tree before relying on them
(`LESSONS_LEARNED.md`, "environment-bound evidence").

### Validation

The maintainer observed in-game that Asian Alliance drone miners, Yuri slave miners
and Schwarzer Mond Noid harvesters reach **the same income** in the three-refinery
test. At `D = 6` cells the model gives:

| refinery | free fleet | model, credits/sec |
|---|---|---|
| `asianalliance_asianorerefinery` | 4 × drone miner | **82.53** |
| `naxis_orerefinery` | 5 × slave (same stats as `YRSLAV`) | **82.71** |
| `schwarzermond_orerefinery` | 2 × Noid harvester | 111.33 |

The two slave/drone economies land **0.2% apart**, which is the observation
reproduced from yaml alone. Schwarzer Mond comes out 35% above them, not level —
the one place the model and the field reading disagree, and the likeliest reason is
that the Noid harvester is not a pure economy unit: it carries `NaxiMP40Laser` and
75000 HP against the drone miner's 25000 and the slave's 10000. Worth a re-run of
the field test on Schwarzer Mond specifically before treating either as settled.

---

## 3. Measured state

`python tools/balance/harvester_income.py` (short 6 cells, long 30, 50/50 weight,
Ore at 25). `N` is the free fleet, `sat` the dock saturation point:

| refinery | N | docks | short | long | aggregate | long/short | sat |
|---|---|---|---|---|---|---|---|
| `ra1_soviets_orerefinery` | 2 | 1 | 147.29 | 42.64 | **94.97** | 0.29 | 9.1 |
| `tkm_orerefinery` | **6** | 1 | 149.08 | 38.28 | **93.68** | 0.26 | 9.2 |
| `td_nod_tiberiumrefinery` | 2 | 1 | 127.31 | 35.92 | 81.62 | 0.28 | 10.2 |
| `schwarzermond_orerefinery` | 2 | 1 | 111.33 | 37.79 | 74.56 | 0.34 | 11.0 |
| `ordos` / `ixian` / `atreides` | 2 | 1 | 105.34 | 29.62 | 67.48 | 0.28 | 11.6 |
| `naxis_orerefinery` | 5 | 1 | 82.71 | 18.85 | 50.78 | 0.23 | 9.2 |
| `asianalliance_asianorerefinery` | 4 | 1 | 82.53 | 17.92 | 50.23 | 0.22 | 7.1 |
| `steelconsortium_consortiumrefinery` | 1 | 1 | 74.18 | 20.04 | 47.11 | 0.27 | 8.1 |
| `latinsyndicate_recyclingrefinery` | 1 | 1 | 67.59 | 24.09 | 45.84 | **0.36** | **4.6** |
| `japan_japaneseorerefinery` | 1 | 1 | 69.59 | 18.07 | 43.83 | 0.26 | 8.6 |
| `ra1_allies_alliedorerefinery` | 1 | 1 | 66.37 | 19.43 | 42.90 | 0.29 | 9.1 |
| `ra2_soviets_orerefinery` | 1 | 1 | 62.73 | 17.57 | 40.15 | 0.28 | **4.9** |
| `futuretech_refinery` | 1 | 1 | 58.25 | 20.88 | 39.57 | **0.36** | 5.3 |
| `td_gdi` / `cabal` / `forgotten` / `ts_gdi` / `ts_nod` | 1 | 1 | 60.06 | 18.85 | 39.46 | 0.31 | 10.2 |
| `ra2_allies_alliedorerefinery` | 1 | 1 | 43.22 | 11.82 | 27.52 | 0.27 | 7.0 |
| `protoss_assimilator` / `zerg_extractor` / `terran_refinery` | 2 | **3** | 45.55 | 9.15 | 27.35 | **0.20** | **205.8** |
| `wc2_humans_elvenlumbermill` / `wc2_orcs_trolllumbermill` | 2 | **3** | 36.46 | 7.70 | 22.08 | 0.21 | **164.6** |

**Spread 4.30×**, median 41.53 credits/sec, 13 of 26 outside ±25% of it.

Five things the table says that the per-unit view hides:

1. **The free fleet dominates.** RA1 Soviets is the top economy because its refinery
   hands out *two* harvesters (a normal ore truck and a heavy industrial miner);
   TKM is second because it hands out **six** — one full harvester plus five slave
   workers, the only hybrid fleet in the game.
2. **Swarm workers are not weak; their refineries are.** A Protoss probe is a fine
   harvester. Two of them on a three-dock Nexus still earn 27 credits/sec against a
   single GDI harvester's 39, because the Nexus gives away only two.
3. **Three docks are worth nothing at the free fleet.** SC and WC2 refineries
   saturate at 165–206 harvesters; no game reaches that. The three docks only pay off
   in a mass-worker strategy the current free fleet does not seed.
4. **Two factions saturate absurdly early.** Latin Syndicate (4.6) and RA2 Soviets
   (4.9) cannot productively run a fifth harvester per refinery — their high
   `BaleUnloadDelay` on one dock is the ceiling. Compare Ordos/Ixian at 11.6.
5. **Long-range mining costs everyone 64–80% of their income**, and the loss is not
   even: Latin Syndicate and FutureTech keep 36% at 30 cells, StarCraft and Asian
   Alliance keep 20%. That 1.8× difference in range resilience is a real and
   currently unowned balance axis.

`schwarzermond_noidharvester` was previously reported at ~3× the roster on
credits-per-cost. That was a data error in `tools/balance/harvester_table.py`, whose
hardcoded table gives it `Capacity: 100` against a yaml value of **50**. That table
is otherwise accurate for 30 of 33 rows but is missing `EDEN_CARGOTRUCK_EMPTY` and
`PLYMOUTH_CARGOTRUCK_EMPTY`, and predates both the balancer and the dock terms.
Prefer `harvester_income.py`, which hardcodes nothing.

---

## 4. Aggregating short and long range

One distance is not a balance measurement, because the two orderings differ: Latin
Syndicate is 9th at short range and 5th at long. The aggregate is a weighted mean of
the two figures:

```
income = w * income(D_short) + (1 - w) * income(D_long)
```

with `D_short = 6`, `D_long = 30`, `w = 0.5` as the defaults, all three settable on
the command line. The defaults say a match spends about half its mining time on a
field near the refinery and half on a distant one.

The **long/short ratio is deliberately not flattened**. A faction that mines well at
range is a legitimate identity (it trades early tempo for map control), and the
model reports the ratio precisely so it can be given a band rather than a value.

### The test map

The field test needs both distances on one map, so the map wants two ore fields per
start: one adjacent to the base footprint (D ≈ 4–8 cells) and one at D ≈ 28–32, with
a clear path and no chokepoint between them, so the measurement is travel time and
not pathfinding. Mining the near field out and letting harvesters walk to the far one
gives a third, more realistic reading, but it is not deterministic across factions —
different capacities exhaust the near field at different moments. **Measure the two
distances as separate runs, and aggregate arithmetically.**

---

## 5. Proposed target bands (NOT yet signed off)

The maintainer's stated goal: *different parameters, around the same income rate.*
That splits into two targets.

* **T1 — aggregate income.** Every refinery's free fleet within **±15%** of the
  roster median (41.53 → band 35.3–47.8 credits/sec). Currently **13 of 26** are
  outside ±25%, so this is a real body of work.
* **T2 — range resilience.** long/short ratio within **0.24–0.34**. Currently three
  entries sit outside (StarCraft 0.20, Asian Alliance 0.22, and Latin Syndicate /
  FutureTech at 0.36).

Both are met by moving the six parameters, never by hand-editing income. The
cheapest levers, in order:

1. **Free fleet size (6).** One extra `FreeActor` moves a 1-harvester refinery by
   roughly +100%. This is the coarse knob and explains most of the current spread.
2. **`Capacity` (1).** Scales income and `T_unload` together, so it moves the
   aggregate without moving the dock saturation point much.
3. **`BaleUnloadDelay` (3).** The dock-saturation knob. Raising it starves the
   refinery's ceiling; Latin Syndicate at 4.6 is the warning case.
4. **`FullyLoadedSpeed` (4).** Touches only the loaded leg, so it moves the long-range
   figure far more than the short one — **this is the T2 knob**.
5. **`DockHost` count (5).** Only meaningful alongside a large free fleet; today the
   three-dock refineries get no value from it.
6. **`BaleLoadDelay` (2).** Distance-independent, so it shifts both figures together
   and changes the ratio least.

`HarvesterBalancer` is a seventh lever and the only global one: it currently pays
+38% within 5 cells to every harvester in the mod. Inverting it — or making
`MaxDistanceFromRefinery` a per-faction number — would move T2 for the whole roster
in one edit, and should be a deliberate decision rather than an inherited default.

---

## 6. Tools

| tool | what |
|---|---|
| `tools/balance/harvester_income.py` | this model; derives all six parameters from the tree, reports both distances, the aggregate, dock saturation and the outliers. `--short/--long/--weight-short/--value/--per-unit`. |
| `tools/balance/harvester_table.py` | maintainer's earlier HTML/PDF table. Hardcoded roster, one wrong Capacity, two missing actors, no balancer or dock term. Kept for its engine-source notes; needs `fpdf`. |
