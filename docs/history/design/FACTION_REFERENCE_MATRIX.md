# Faction reference matrix

**Current state of the reference-routing layer.** Regenerate the numbers with:

```sh
python tools/balance/faction_routes.py
python tools/balance/faction_routes.py --check    # every ruled route resolves, or exit 1
python tools/balance/faction_extrapolate.py --by-class
```

> The accreted 2026-09-04 narrative (the blocker story, the wiring session, the
> measured "bullshit rate" of the rejected matcher) is preserved in
> [`docs/history/FACTION_REFERENCE_MATRIX_2026-09-04.md`](../FACTION_REFERENCE_MATRIX_2026-09-04.md).
> Everything here is the present.

## Maintainer rulings (binding — kept inline per fleet order)

1. **2026-09-04, the founding ruling:** *"most of the references are bullshit — what does the
   Asian Alliance Militia have to do with the Combined Arms Infiltrator? — instead of trying to
   match something completely unrelated we now try to map reference faction to our cameo
   factions."* Routing is faction-to-faction; name matching never crosses the route.
2. **≥2 reference factions from DIFFERENT games per Cameo faction** — a second faction from the
   same game may be a mirror and adds no information (OpenE2140 `ucs`/`ed` were the example:
   identically-named rosters).
3. **Mirror-merge rule (2026-09-04):** *"since they are nearly identical we just regard them as
   one big faction with twice the units as reference."* Applied: OpenHV `sc`+`yi` = one merged
   voice for Steel Consortium.
4. **Rosters need not line up (2026-09-04):** only a portion of units may map; the excess
   reference rows are extrapolated from (the `k` exchange-rate method below).
5. **A faction with no real counterpart is formula-only from a grounded class anchor** — never
   force a bad match. (Today: `corrino`, `ixian`.)
6. **Combined Arms `scrin` is RESERVED** for the upcoming Cameo Scrin faction.
7. **References decide placement inside the class distribution, not final stats.** The balance
   formula still prices the unit.
8. **`fit_class`/`fit_class --spec` run against the class `spec`, never `--anchor` on a
   pre-restat actor** (Claude, 2026-09-06 — `--anchor` writes `o0/p0/q0` back into
   `class_anchors.json` and silently drags the anchor to replaced stats; `mbt` was the proof).

## Binding structural lessons (do not regress these)

- **Faction ids are the tree's `InternalName`s** — `asianalliance`, `td_gdi`, `forgotten` —
  not the long ledger/pack names. Routing keyed on a name no actor id carries routes nothing,
  silently. `faction_routes.py` validates against the tree; `--check` is green.
- **The rejected matcher's route-legal rate was 7.8%** (144/1,852 proposals), and 599 of the
  illegal ones were labelled STRONG. That measurement — kept in the history document — is why
  routing exists. Do not reintroduce name-first matching.

## The route matrix (tool output, current)

```
corpus: 4520 peer rows from 21 sources



  cameo faction     reference source          tokens            rows

  asianalliance     Generals Alpha            prc                 38

  asianalliance     Mental Omega              chinese            170

  asianalliance     Rise of the East          china               82

  asianalliance     RA2 Reborn                china/northkore    159

  atreides          OpenRA Dune 2000          atreides             5

  atreides          OpenRA Dune II            atreides             4

  cabal             Shattered Paradise        cab                 44

  cabal             CnC Reloaded              robotcountry/ro    129

  forgotten         Shattered Paradise        mut                 56

  forgotten         Twisted Insurrection      forsaken            14

  futuretech        OpenE2140                 ucs                 48

  futuretech        Mental Omega              unitedstates       173

  harkonnen         OpenRA Dune 2000          harkonnen            4

  harkonnen         OpenRA Dune II            harkonnen            2

  japan             Mental Omega              pacific            172

  japan             RA2 Reborn                japan              173

  latinsyndicate    Generals Alpha            gla                 47

  latinsyndicate    Mental Omega              latin              172

  latinsyndicate    RA2 Reborn                china/northkore    159

  naxis             OpenE2140                 ed                  47

  naxis             RA2 0XX                   germans            106

  ordos             OpenRA Dune 2000          ordos                4

  ordos             OpenRA Dune II            ordos                3

  ra1_allies        Combined Arms             allies              37

  ra1_allies        OpenRA Red Alert          allies              24

  ra1_allies        DTA Enhanced              allies              74

  ra1_soviets       Combined Arms             soviet              57

  ra1_soviets       OpenRA Red Alert          soviet              23

  ra1_soviets       DTA Enhanced              soviet              73

  ra2_allies        Romanov's Vengeance       allies              62

  ra2_allies        Valiant Shades            allies              65

  ra2_allies        Mental Omega              europeans          174

  ra2_allies        CnC Reloaded              alliescountry      199

  ra2_allies        RA2 Reborn                alliance/americ    173

  ra2_allies        Red Resurrection          alliance/americ    165

  ra2_allies        RA2 0XX                   alliance/americ    107

  ra2_soviets       Romanov's Vengeance       soviets             68

  ra2_soviets       Valiant Shades            soviets             54

  ra2_soviets       Mental Omega              ussr               172

  ra2_soviets       CnC Reloaded              sovietcountry      208

  ra2_soviets       RA2 Reborn                africans/arabs/    161

  ra2_soviets       Red Resurrection          africans/arabs/    158

  ra2_soviets       RA2 0XX                   africans/arabs/    106

  schwarzermond     Mental Omega              guild1/guild2/g    119

  schwarzermond     OpenE2140                 ucs                 48

  steelconsortium   OpenHV                    sc/yi               96

  steelconsortium   Mental Omega              guild1/guild2/g    119

  td_gdi            Combined Arms             gdi                 37

  td_gdi            OpenRA Tiberian Dawn      gdi                 37

  td_gdi            DTA Enhanced              gdi                 63

  td_nod            Combined Arms             nod                 50

  td_nod            OpenRA Tiberian Dawn      nod                 40

  td_nod            DTA Enhanced              nod                 89

  tkm               Mental Omega              scorpioncell       158

  tkm               Rise of the East          iraq                79

  ts_gdi            Shattered Paradise        gdi                 55

  ts_gdi            Crystallized Nexus        gdi                 34

  ts_gdi            OpenRA Tiberian Sun       gdi                 27

  ts_gdi            CnC Reloaded              gdicountry         218

  ts_gdi            Twisted Insurrection      gdi/phoenix         86

  ts_nod            Shattered Paradise        nod                 49

  ts_nod            Crystallized Nexus        nod                 42

  ts_nod            OpenRA Tiberian Sun       nod                 30

  ts_nod            CnC Reloaded              nodcountry         236

  ts_nod            Twisted Insurrection      nod/sons            72

  yuri              Romanov's Vengeance       psicorps            12

  yuri              Combined Arms             yuri                 6

  yuri              Mental Omega              psicorps/headqu    161

  yuri              CnC Reloaded              yuricountry        201

  yuri              RA2 Reborn                yuricountry/yur    161

  yuri              Red Resurrection          yuricountry        109

  yuri              RA2 0XX                   yuricountry         75



routed factions   : 22 of 24 declared

formula-only      : corrino, ixian

second game OPEN  : asianalliance, atreides, cabal, forgotten, futuretech, harkonnen, latinsyndicate, naxis, ordos, steelconsortium

ruled but PENDING : 8 routes across 5 factions (missing source data)



 every ruled route resolves against the corpus


```

Notes on the reading of it:

- **Every routed Cameo faction now clears the ≥2-games floor in token terms**; the tool's
  "second game OPEN" list means a second *game* whose extraction is still thin or a route
  whose partner source is pending, not an unruled faction.
- **`corrino` and `ixian` are the only unrouted (formula-only) factions** — both wait on
  Emperor: Battle for Dune source data.
- "ruled but PENDING: 8 routes across 5 factions" = sources ruled but not yet extracted.

## Grounding state (`faction_extrapolate.py --by-class`, current)

The extrapolation layer: matched pairs give an exchange rate `k` (geometric mean of
cameo/reference per stat per route); every routed reference row x `k` is Cameo-scale shape
data; a unit with no counterpart is placed by rank inside its own (faction, type) population.
Cameo's roster decides the order; the reference decides the spread.

```
  class              memb exmpt limit routed paired placed  >=2   grounded

  mbt                  44     0     0     33     31      1   26     97%

  heavy_infantry       39     0     2     34     28      3   20     91%

  fire_support         31     0     0     27     27      0   25    100%

  artillery            34     0     0     27     23      3   21     96%

  scout_vehicle        28     0     0     27     26      0   20     96%

  line_breaker         32     0     0     23     22      1   19    100%

  high_tech_tank       26     0     0     21     20      1   18    100%

  melee                44     1     4     22     21      0   16     95%

  rocket_trooper       43     0     4     24     21      0   16     88%

  scout                27     0     0     23     18      2   14     87%

  special_forces       16     0     0     14     13      1   10    100%

  anti_air_vehicle     14     0     0     13     13      0   10    100%

  light_tank           15     0     0     13     13      0   11    100%

  artillery_tank       14     0     0     12     10      2    9    100%

  missile_vehicle      14     0     0     11      9      2    9    100%

  pure_sniper          15     0     0      9      7      2    5    100%

  flying_infantry       9     0     0      7      6      1    4    100%

  grenadier             6     0     0      6      6      0    5    100%

  tank_destroyer        5     0     0      5      4      1    4    100%

  closecombat           4     0     0      4      3      1    1    100%

  dreadnought           5     0     0      3      3      0    2    100%

  mortar                5     0     0      4      3      0    1     75%

  archer                4     0     0      2      2      0    2    100%

  heavy_sniper          2     0     0      2      1      1    1    100%

  commando             27     0    27     23      0      0    0      0%   excluded by the population rule (all build-limited)

  epic_vehicle         24     0    24     21      0      0    0      0%   excluded by the population rule (all build-limited)

  support             107   107     2      0      0      0    0       —  ✅ wholly exempt (clause 10)

  TOTAL               634   108          410    330     22  269



  grounded: 330 paired + 22 rank-placed = 352 of 410 routed class members

   only 269 of 410 routed members reach the >=2 reference floor; 25 classes are still short of it.


```

## What is actually open

1. `corrino`, `ixian` — unrouted; need Emperor: Battle for Dune data.
2. 8 ruled-but-pending routes across 5 factions (missing source data; see the matrix footer).
3. 25 classes short of the ≥2-reference floor (`269` of `410` routed members reach it).
4. Class-anchor sign-off: `fit_class.py --spec ...` per class → `docs/balance/formula_v2_<class>.md`,
   then `anchor_readiness.py` ranks the queue (Claude's Phase-D replacement task, 2026-09-06).
