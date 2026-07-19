# Formula v2 — CLOSE-COMBAT INFANTRY class (proposal, 2026-07-19)

_The maintainer's question: units with less range than the scout band
allows need a new shotgunner/close-range class between melee and heavy
infantry. Problem analysis + solution proposal for review. Nothing
applied._

## 1. The problem, precisely

1. The scout range band (4500–5500) is correct for scouts — but it
   FORCED true close-range fighters upward (the forgotten_mutant went
   3132 → 4500 in proposal v2, a +44% reach buff that changes its
   combat identity from brawler to rifleman).
2. A ±10% band around any single anchor covers only ~20% of range
   space. The infantry ladder currently anchors melee (~1500–2000
   contact weapons) and scout (5000) — leaving everything between
   ~1700 and ~4400 classless. A survey of base-weapon ranges found
   **60 infantry units in that hole**.
3. Under the WRONG class anchor the formula misprices structurally:
   a low range ratio collapses Q, so the solver hands the unit huge
   DPS/HP for its cost — the class formula only works when the class
   is right.

## 2. What actually lives in the hole (survey 2026-07-19)

The 60 units cluster into archetypes — most belong to OTHER classes
and leave a clean shotgunner core behind:

| archetype | examples | verdict |
|---|---|---|
| Commando/hero (C4, attach kills, rng 2000) | Tanya(s), TD commandos, Havoc, SEALs, spetsnaz, shadow team, black widow, ghoststalker | hero/commando class (own anchor later) — NOT close-combat |
| Attack dogs | ra1/ra2 dogs, cyberdog | own mini-archetype (leap mechanic), later |
| Flame/chem | td_nod flamethrower 2085, chem warrior 3414, chemspray 3183, japan flamer 3603, firebat 3400, thermonaut 3204 | mostly ALREADY ^HeavyInfantryTemplate — stay heavy; maintainer may split a flame class later |
| Utility/economy | engineers (4303), crazyivan, saboteur, contaminator, leech, slaveoverseer, named civilians | support/special class (below) or their own thing |
| **THE SHOTGUN/SMG CORE** | **td_gdi_shotgunner 3125, ts_gdi_riottrooper 4002 (TSShotgun), futuretech_enforcer 3000 + shotgundroid 4110, forgotten_runnershotgal 3112 + mutant 3132 (dual pistols), naxis_sssoldier 4000 (MP40), fremen_creep 3072, heavy_inf.ixian 3800, ts_gdi/nod_lightinfantry 4062** | **the new class** |
| Casters | zerg_defiler, kerrigan, teslatrooper? | defiler → SNIPER transform (maintainer verdict); others case-by-case |

## 3. The proposed class: `closecombat` (shotgunners & SMGs)

**Ladder position** (maintainer: between melee and heavy):
melee (contact) < **closecombat 3150–3850** < scout 4500–5500;
tougher per cost than scouts (they must survive the approach), less
specialist than heavies.

**Anchor proposal (round numbers, O=P=Q=C₀ by construction):**

| | HP | Speed | Range | Damage | Reload | WC | eff DPS | Cost |
|---|---|---|---|---|---|---|---|---|
| baseline | 40000 | 55 | **3500** | 8000 | 50 | SA 0.75 | 120 | **200** |
| verifier (2×/2×) | 80000 | 55 | 3500 | 16000 | 50 | SA 0.75 | 240 | 500 |

- REV (maintainer 2026-07-19): band widened to the CONTIGUOUS
  [2500, 4500) — anchor stays 3500 (the exact center). No unit can
  fall between melee and scout anymore; within the class the price
  gradient binds (cheap → 2500 end, pricey → 4500 end). Original
  cluster needs almost no range moves at all now (shotgunner 3125,
  mutant 3132, shotgundroid 4110 all already inside).
- Vs the scout baseline: 2× HP and 2× DPS for 2× cost at −30% range
  and −5 speed — the archetype IS the formula trade.
- **Baseline unit pick: `td_gdi_shotgunner`** — THE archetypal
  shotgunner, already exactly cost 200 (classic-price law holds).
- Verifier pick: maintainer's call — `ts_nod_shotguncommando` is v
  wrong tier (3000); suggest converting `ts_gdi_riottrooper` (700)
  down to 500 as the verifier, or a Forgotten shotgun variant.
- Class rules inherit the law book (10-step ranges, damage 2000-steps,
  bands, envelope 100–500 = 50–250% of 200, no-air, ground autotarget,
  burst/pellets as flavor with unit-named FP-mult). Shotgun pellet
  spread stays a weapon-flavor property, not a formula input (like
  burst).

**Template mechanics:** new `^CloseCombatInfantryTemplate` in
defaults.yaml mirroring ^ScoutInfantryTemplate (armor class, its own
Buff knob pair for §5b live tuning, ^AutoTargetGroundAssaultMove
default); members migrate off ^ScoutInfantryTemplate /
^HeavyInfantryTemplate one at a time with the standard conversion
checklist (FORMULA_V2 §6).

## 4. The second new class from the maintainer's verdicts: `support`

Spies (raspy, ra2spy, spyfutu), Yuri mind-control units, CABAL
hackers — units whose value is an ABILITY, not DPS. New
`^SupportInfantryTemplate`; pricing needs an ability-value table
(infiltration, mind control, hack) because the combat formula has no
input for them — proposal: maintainer prices the ability tiers once
(like the old Special column, but per-ability), formula prices the
chassis (HP/speed), price = chassis + ability. To be designed with
the maintainer before any conversion.

## 5. Sequencing & open maintainer decisions

1. Finish the scout conversions (13 queued) — unchanged.
2. `closecombat`: approve anchor spec + baseline (td_gdi_shotgunner)
   + verifier pick + member list (§2 core).
3. `sniper`: transform zerg_defiler per verdict (stat proposal to
   follow once the sniper anchor exists — ^SniperInfantryTemplate
   already exists as a template).
4. `support`: ability-value table workshop with the maintainer.
5. Melee + heavy anchors: after closecombat lands, same survey method
   (melee r₀ ~1500 band 1350–1650; heavy needs its own survey — many
   flame units already live there).
6. Civilians (alien/undead/conehead2.nax): parked undecided per the
   maintainer — they can slot into scout/closecombat/melee/heavy once
   the ladder is complete.
