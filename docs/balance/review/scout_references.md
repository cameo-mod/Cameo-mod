# `scout` — reference assignment for review

**Generated** by `python tools/balance/assign_references.py --review scout`. Regenerates — record decisions and re-run rather than hand-editing.

> ⛔ **A PROPOSAL LIST, NOT EVIDENCE.** Until this review is done the class has no grounded
> members and therefore no anchor (`REFERENCE_METHOD.md` §9.9).

## §0 — State of the class

| | |
|---|--:|
| members | **30** |
| assigned at least one reference | **23** |
| **with ≥2 NAME-backed references** | **15** |
| with ≥2 name-or-shape references | 23 |
| members with NO reference at all | **7** |

Confidence: FAIR 21 · SHAPE 116 · **STRONG 34** · **WEAK 62**

* **STRONG** exact/alias name, or name overlap backed by matching shape
* **FAIR** a real name overlap, shape unconfirmed
* **SHAPE** same position in its own roster; the name says nothing — evidence for a distribution method, NOT a claim the two are the same unit
* **WEAK** neither; the greedy assigned the best of a bad field

⚠ **No reference at all** — formula-only unless the review rescues them:

* `E1` — cost 100.0
* `conehead2.nax` — cost 500.0
* `forgotten_mutant_sp` — cost 160.0
* `forgotten_mutant_wild` — cost 160.0
* `forgotten_mutantsoldier_sp` — cost 250.0
* `ra2e2.black` — cost 150.0
* `undead.nax` — cost 100.0

---

## §1 — NAME-backed proposals — confirm or strike

| ok? | conf | unit | source | reference unit | name | role | cost |
|:--:|---|---|---|---|--:|--:|--:|
| ☐ | STRONG | `asianalliance_asianmilitia` | Combined Arms **(home)** | Assimilator | 0.61 | 0.79 | 0.71 |
| ☐ | STRONG | `asianalliance_asianmilitia` | Shattered Paradise | Militant | 0.60 | 0.76 | 0.99 |
| ☐ | STRONG | `forgotten_mutant` | CnC Reloaded | Mutant | 1.00 | 0.00 | 0.85 |
| ☐ | STRONG | `forgotten_mutant` | Shattered Paradise **(home)** | Mutant Engineer | 0.90 | 0.43 | 0.52 |
| ☐ | FAIR | `forgotten_mutant` | OpenRA Red Alert | Scout Ant | 0.71 | 0.66 | 0.73 |
| ☐ | STRONG | `forgotten_mutantsoldier` | CnC Reloaded | Mutant Soldier | 1.00 | 0.00 | 0.88 |
| ☐ | FAIR | `forgotten_mutantsoldier` | Combined Arms | Rocket Soldier | 0.61 | 0.50 | 0.99 |
| ☐ | STRONG | `futuretech_scoutdroid` | OpenRA Tiberian Dawn | Visceroid | 0.63 | 0.80 | 0.59 |
| ☐ | FAIR | `ixian_lightinfantry` | CnC Reloaded | GDI Light Infantry | 0.90 | 0.00 | 0.88 |
| ☐ | STRONG | `latinsyndicate_latinmilitia` | Romanov's Vengeance **(home)** | Initiate | 0.60 | 0.86 | 0.63 |
| ☐ | STRONG | `latinsyndicate_latinmilitia` | Valiant Shades | Initiate | 0.60 | 0.75 | 1.00 |
| ☐ | STRONG | `light_inf` | Combined Arms | Infiltrator | 0.90 | 0.66 | 0.56 |
| ☐ | STRONG | `light_inf` | Mental Omega | Infiltrator | 0.90 | 0.00 | 0.49 |
| ☐ | STRONG | `naxis_naxiriflesoldier` | OpenRA Red Alert | Rocket Soldier | 0.62 | 0.77 | 0.65 |
| ☐ | STRONG | `naxis_naxiriflesoldier` | Romanov's Vengeance **(home)** | Rocket Soldier | 0.62 | 0.76 | 0.41 |
| ☐ | FAIR | `naxis_naxiriflesoldier` | OpenRA Tiberian Dawn | Rocket Soldier | 0.62 | 0.74 | 0.76 |
| ☐ | FAIR | `naxis_naxiriflesoldier` | Combined Arms **(home)** | Rocket Soldier | 0.62 | 0.58 | 0.82 |
| ☐ | FAIR | `ordos_lightinfantry` | CnC Reloaded | Lunar Infantry | 0.69 | 0.00 | 0.49 |
| ☐ | STRONG | `ra1_allies_rifleinfantry` | OpenRA Red Alert **(home)** | Rifle Infantry | 1.00 | 0.88 | 0.99 |
| ☐ | FAIR | `ra1_allies_rifleinfantry` | OpenRA Tiberian Sun | Rocket Infantry | 0.74 | 0.69 | 0.72 |
| ☐ | FAIR | `ra1_allies_rifleinfantry` | CnC Reloaded | Jumpjet Infantry | 0.64 | 0.00 | 0.46 |
| ☐ | FAIR | `ra1_soviets_ak47conscript` | Mental Omega | Conscript Disguise | 0.60 | 0.00 | 0.87 |
| ☐ | STRONG | `ra1_soviets_rifleinfantry` | Combined Arms **(home)** | Rifle Infantry | 1.00 | 0.71 | 0.99 |
| ☐ | FAIR | `ra1_soviets_rifleinfantry` | OpenRA Red Alert **(home)** | Flame Infantry | 0.85 | 0.64 | 0.65 |
| ☐ | FAIR | `ra1_soviets_rifleinfantry` | OpenRA Tiberian Sun | Cyborg Infantry | 0.67 | 0.61 | 0.15 |
| ☐ | FAIR | `ra1_soviets_rifleinfantry` | CnC Reloaded | Mutant Rocket Infantry | 0.61 | 0.00 | 0.77 |
| ☐ | STRONG | `ra2_allies_gi` | Romanov's Vengeance **(home)** | G.I. | 1.00 | 0.86 | 0.66 |
| ☐ | STRONG | `ra2_allies_gi` | Valiant Shades | G.I. | 1.00 | 0.79 | 0.98 |
| ☐ | STRONG | `ra2_allies_gi` | CnC Reloaded **(home)** | GI | 1.00 | 0.00 | 0.85 |
| ☐ | STRONG | `ra2_allies_gi` | OpenRA Red Alert | Giant Ant | 0.90 | 0.57 | 0.74 |
| ☐ | STRONG | `ra2_allies_gi` | Mental Omega **(home)** | Giantsbane | 0.90 | 0.00 | 0.43 |
| ☐ | STRONG | `ra2_soviets_conscript` | Romanov's Vengeance **(home)** | Conscript | 1.00 | 0.94 | 0.60 |
| ☐ | STRONG | `ra2_soviets_conscript` | Generals Alpha | Conscript | 1.00 | 0.93 | 0.99 |
| ☐ | STRONG | `ra2_soviets_conscript` | Valiant Shades | Conscript | 1.00 | 0.80 | 0.99 |
| ☐ | STRONG | `ra2_soviets_conscript` | CnC Reloaded **(home)** | Conscript | 1.00 | 0.00 | 0.83 |
| ☐ | STRONG | `ra2_soviets_conscript` | Mental Omega **(home)** | Conscript | 1.00 | 0.00 | 0.86 |
| ☐ | STRONG | `td_gdi_minigunner` | Generals Alpha | Minigunner | 1.00 | 0.89 | 0.39 |
| ☐ | STRONG | `td_gdi_minigunner` | OpenRA Tiberian Dawn **(home)** | Minigunner | 1.00 | 0.76 | 0.99 |
| ☐ | STRONG | `td_nod_minigunner` | Combined Arms **(home)** | Mini-Gunner | 1.00 | 0.64 | 0.99 |
| ☐ | FAIR | `td_nod_minigunner` | Mental Omega | Railguneer | 0.60 | 0.00 | 0.64 |
| ☐ | STRONG | `tkm_marine` | Shattered Paradise | Marine | 1.00 | 0.75 | 0.91 |
| ☐ | STRONG | `tkm_marine` | Crystallized Nexus | Marine | 1.00 | 0.70 | 0.79 |
| ☐ | FAIR | `tkm_marine` | Combined Arms **(home)** | Bombardier | 0.62 | 0.68 | 0.59 |
| ☐ | STRONG | `ts_gdi_lightinfantry` | OpenRA Dune II | Light Infantry Squad | 0.90 | 0.78 | 0.88 |
| ☐ | STRONG | `ts_gdi_lightinfantry` | OpenRA Dune 2000 | Thumper Infantry | 0.64 | 0.91 | 0.64 |
| ☐ | FAIR | `ts_gdi_lightinfantry` | OpenRA Tiberian Sun **(home)** | Jump Jet Infantry | 0.64 | 0.70 | 0.47 |
| ☐ | FAIR | `ts_gdi_lightinfantry` | Crystallized Nexus **(home)** | Jump Jet Infantry | 0.64 | 0.69 | 0.47 |
| ☐ | FAIR | `ts_gdi_lightinfantry` | Shattered Paradise **(home)** | Cyborg Infantry | 0.67 | 0.61 | 0.79 |
| ☐ | FAIR | `ts_gdi_lightinfantry` | CnC Reloaded | Rocket Infantry | 0.67 | 0.00 | 0.65 |
| ☐ | STRONG | `ts_nod_lightinfantry` | OpenRA Dune II | Light Infantry | 1.00 | 0.84 | 0.96 |
| ☐ | STRONG | `ts_nod_lightinfantry` | OpenRA Tiberian Sun **(home)** | Light Infantry | 1.00 | 0.83 | 0.96 |
| ☐ | STRONG | `ts_nod_lightinfantry` | OpenRA Dune 2000 | Light Infantry | 1.00 | 0.70 | 0.96 |
| ☐ | FAIR | `ts_nod_lightinfantry` | CnC Reloaded | Nod Light Infantry | 0.90 | 0.00 | 0.84 |
| ☐ | FAIR | `ts_nod_lightinfantry` | Crystallized Nexus **(home)** | Rocket Infantry | 0.67 | 0.73 | 0.83 |
| ☐ | FAIR | `ts_nod_lightinfantry` | Shattered Paradise **(home)** | Jumpjet Infantry | 0.64 | 0.73 | 0.57 |

---

## §2 — SHAPE-only proposals

Same position in its own roster, unrelated name. Real evidence for the distribution method; your call whether it counts.

| ok? | conf | unit | source | reference unit | name | role | cost |
|:--:|---|---|---|---|--:|--:|--:|
| ☐ | SHAPE | `asianalliance_asianmilitia` | Romanov's Vengeance **(home)** | sspy | 0.12 | 0.93 | 0.41 |
| ☐ | SHAPE | `asianalliance_asianmilitia` | Generals Alpha | Rebel | 0.12 | 0.87 | 0.94 |
| ☐ | SHAPE | `asianalliance_asianmilitia` | OpenRA Dune II | Fremen | 0.11 | 0.84 | 0.70 |
| ☐ | SHAPE | `asianalliance_asianmilitia` | OpenRA Red Alert | SPY.England | 0.27 | 0.80 | 0.74 |
| ☐ | SHAPE | `asianalliance_asianmilitia` | Valiant Shades | Engineer | 0.20 | 0.80 | 0.81 |
| ☐ | SHAPE | `forgotten_mutant` | Combined Arms | Cyberscrin | 0.12 | 0.90 | 0.57 |
| ☐ | SHAPE | `forgotten_mutant` | Romanov's Vengeance | Rocketeer | 0.13 | 0.84 | 0.47 |
| ☐ | SHAPE | `forgotten_mutant` | Crystallized Nexus **(home)** | Zone Trooper | 0.12 | 0.82 | 0.15 |
| ☐ | SHAPE | `forgotten_mutantsoldier` | Romanov's Vengeance | Medic | 0.33 | 0.96 | 0.74 |
| ☐ | SHAPE | `forgotten_mutantsoldier` | Shattered Paradise **(home)** | Plague Trooper | 0.39 | 0.90 | 0.72 |
| ☐ | SHAPE | `forgotten_mutantsoldier` | Crystallized Nexus **(home)** | Acolyte | 0.40 | 0.86 | 0.34 |
| ☐ | SHAPE | `forgotten_mutantsoldier` | Valiant Shades | Tesla Trooper | 0.48 | 0.81 | 0.97 |
| ☐ | SHAPE | `futuretech_scoutdroid` | Generals Alpha | Saboteur | 0.44 | 0.93 | 0.18 |
| ☐ | SHAPE | `futuretech_scoutdroid` | Shattered Paradise | Crusader | 0.33 | 0.89 | 0.74 |
| ☐ | SHAPE | `futuretech_scoutdroid` | Valiant Shades | Grenadier | 0.21 | 0.88 | 0.70 |
| ☐ | SHAPE | `futuretech_scoutdroid` | Combined Arms **(home)** | Cryo Trooper | 0.48 | 0.88 | 0.24 |
| ☐ | SHAPE | `futuretech_scoutdroid` | Romanov's Vengeance **(home)** | Lazarus Spectre | 0.33 | 0.86 | 0.20 |
| ☐ | SHAPE | `ixian_lightinfantry` | Romanov's Vengeance | Gatling Trooper | 0.37 | 0.96 | 0.56 |
| ☐ | SHAPE | `ixian_lightinfantry` | Valiant Shades | Chrono Legionnaire | 0.27 | 0.88 | 0.10 |
| ☐ | SHAPE | `ixian_lightinfantry` | OpenRA Dune 2000 | Fremen | 0.10 | 0.87 | 0.67 |
| ☐ | SHAPE | `ixian_lightinfantry` | Combined Arms | Intruder | 0.38 | 0.87 | 0.56 |
| ☐ | SHAPE | `ixian_lightinfantry` | OpenRA Red Alert | Mechanic | 0.29 | 0.84 | 0.25 |
| ☐ | SHAPE | `ixian_lightinfantry` | OpenRA Tiberian Dawn | Commando | 0.19 | 0.76 | 0.16 |
| ☐ | SHAPE | `ixian_lightinfantry` | Shattered Paradise | Eagle Guard | 0.35 | 0.75 | 0.32 |
| ☐ | SHAPE | `latinsyndicate_latinmilitia` | Combined Arms **(home)** | Rad Trooper | 0.18 | 0.89 | 0.62 |
| ☐ | SHAPE | `latinsyndicate_latinmilitia` | Shattered Paradise | Phalanx | 0.32 | 0.89 | 0.72 |
| ☐ | SHAPE | `latinsyndicate_latinmilitia` | OpenRA Red Alert | Shock Trooper | 0.08 | 0.85 | 0.36 |
| ☐ | SHAPE | `latinsyndicate_latinmilitia` | OpenE2140 | Android A02 | 0.27 | 0.79 | 0.69 |
| ☐ | SHAPE | `latinsyndicate_latinmilitia` | Generals Alpha | Hacker | 0.11 | 0.78 | 0.25 |
| ☐ | SHAPE | `latinsyndicate_latinmilitia` | Crystallized Nexus | Sniper Squad | 0.09 | 0.77 | 0.70 |
| ☐ | SHAPE | `light_inf` | OpenRA Red Alert | Medic | 0.25 | 0.89 | 0.90 |
| ☐ | SHAPE | `light_inf` | Crystallized Nexus | Missile Trooper | 0.12 | 0.88 | 0.86 |
| ☐ | SHAPE | `light_inf` | Romanov's Vengeance | Tesla Trooper | 0.00 | 0.87 | 0.56 |
| ☐ | SHAPE | `light_inf` | Valiant Shades | Hoplite | 0.20 | 0.84 | 0.17 |
| ☐ | SHAPE | `light_inf` | Shattered Paradise | Zone Defender | 0.27 | 0.81 | 0.23 |
| ☐ | SHAPE | `light_inf` | OpenRA Dune 2000 | Sardaukar | 0.00 | 0.81 | 0.67 |
| ☐ | SHAPE | `naxis_coneheadsknights` | Valiant Shades | Headless Kamikaze | 0.44 | 0.95 | 0.19 |
| ☐ | SHAPE | `naxis_coneheadsknights` | Romanov's Vengeance **(home)** | Motorised Engineer | 0.24 | 0.94 | 0.90 |
| ☐ | SHAPE | `naxis_coneheadsknights` | Shattered Paradise | Reclaimer | 0.16 | 0.93 | 0.22 |
| ☐ | SHAPE | `naxis_coneheadsknights` | Combined Arms **(home)** | Terror Dog | 0.24 | 0.88 | 0.37 |
| ☐ | SHAPE | `naxis_coneheadsknights` | Generals Alpha | Hijacker | 0.17 | 0.85 | 0.85 |
| ☐ | SHAPE | `naxis_coneheadsknights` | OpenRA Red Alert | Attack Dog | 0.24 | 0.81 | 0.36 |
| ☐ | SHAPE | `naxis_coneheadsknights` | OpenRA Tiberian Dawn | Triceratops | 0.22 | 0.79 | 0.77 |
| ☐ | SHAPE | `naxis_coneheadsknights` | Crystallized Nexus | Elite Cadre | 0.23 | 0.78 | 0.76 |
| ☐ | SHAPE | `naxis_naxiriflerecruit` | Combined Arms **(home)** | Medic | 0.19 | 0.91 | 0.92 |
| ☐ | SHAPE | `naxis_naxiriflerecruit` | Valiant Shades | Flak Trooper | 0.30 | 0.90 | 0.94 |
| ☐ | SHAPE | `naxis_naxiriflerecruit` | OpenRA Red Alert | Engineer | 0.33 | 0.88 | 0.23 |
| ☐ | SHAPE | `naxis_naxiriflerecruit` | Shattered Paradise | Legionnaire | 0.37 | 0.88 | 0.66 |
| ☐ | SHAPE | `naxis_naxiriflerecruit` | Romanov's Vengeance **(home)** | Angry Mob | 0.17 | 0.86 | 0.19 |
| ☐ | SHAPE | `naxis_naxiriflerecruit` | OpenE2140 | SILVER R | 0.43 | 0.85 | 0.62 |
| ☐ | SHAPE | `naxis_naxiriflerecruit` | OpenRA Dune II | Saboteur | 0.17 | 0.85 | 0.17 |
| ☐ | SHAPE | `naxis_naxiriflerecruit` | OpenRA Dune 2000 | Engineer | 0.33 | 0.84 | 0.10 |
| ☐ | SHAPE | `naxis_naxiriflerecruit` | Generals Alpha | Super Hacker | 0.30 | 0.83 | 0.19 |
| ☐ | SHAPE | `naxis_naxiriflerecruit` | OpenRA Tiberian Dawn | Engineer | 0.33 | 0.83 | 0.58 |
| ☐ | SHAPE | `naxis_naxiriflerecruit` | Crystallized Nexus | Engineer | 0.33 | 0.83 | 0.64 |
| ☐ | SHAPE | `naxis_naxiriflerecruit` | OpenRA Tiberian Sun | Engineer | 0.33 | 0.79 | 0.57 |
| ☐ | SHAPE | `naxis_naxiriflesoldier` | Crystallized Nexus | Shadow Trooper | 0.34 | 0.81 | 0.22 |
| ☐ | SHAPE | `naxis_naxiriflesoldier` | Shattered Paradise | Marauder | 0.42 | 0.76 | 0.97 |
| ☐ | SHAPE | `ordos_lightinfantry` | Combined Arms | Grenadier | 0.18 | 0.90 | 0.99 |
| ☐ | SHAPE | `ordos_lightinfantry` | Romanov's Vengeance | Terrorist | 0.18 | 0.82 | 0.58 |
| ☐ | SHAPE | `ra1_allies_rifleinfantry` | Combined Arms **(home)** | Shock Trooper | 0.16 | 0.90 | 0.56 |
| ☐ | SHAPE | `ra1_allies_rifleinfantry` | OpenRA Tiberian Dawn **(home)** | Flamethrower | 0.40 | 0.88 | 0.84 |
| ☐ | SHAPE | `ra1_allies_rifleinfantry` | Romanov's Vengeance | Yuri Clone | 0.36 | 0.84 | 0.16 |
| ☐ | SHAPE | `ra1_allies_rifleinfantry` | Valiant Shades | Yuri | 0.23 | 0.77 | 0.20 |
| ☐ | SHAPE | `ra1_soviets_ak47conscript` | OpenRA Red Alert **(home)** | Thief | 0.11 | 0.97 | 0.27 |
| ☐ | SHAPE | `ra1_soviets_ak47conscript` | Combined Arms **(home)** | Crazy Ivan | 0.27 | 0.95 | 0.40 |
| ☐ | SHAPE | `ra1_soviets_ak47conscript` | Shattered Paradise | Disc Thrower | 0.25 | 0.94 | 0.93 |
| ☐ | SHAPE | `ra1_soviets_ak47conscript` | OpenRA Tiberian Dawn **(home)** | Tyrannosaurus rex | 0.28 | 0.89 | 0.51 |
| ☐ | SHAPE | `ra1_soviets_ak47conscript` | Romanov's Vengeance | Tesla Commando | 0.31 | 0.88 | 0.16 |
| ☐ | SHAPE | `ra1_soviets_ak47conscript` | Valiant Shades | Navy SEAL | 0.19 | 0.83 | 0.67 |
| ☐ | SHAPE | `ra1_soviets_rifleinfantry` | Romanov's Vengeance | Flak Trooper | 0.42 | 0.93 | 0.49 |
| ☐ | SHAPE | `ra1_soviets_rifleinfantry` | OpenRA Dune II | Fremen | 0.32 | 0.88 | 0.67 |
| ☐ | SHAPE | `ra1_soviets_rifleinfantry` | Valiant Shades | Crazy Ivan | 0.36 | 0.86 | 0.59 |
| ☐ | SHAPE | `ra1_soviets_rifleinfantry` | OpenRA Tiberian Dawn **(home)** | Chemical Warrior | 0.21 | 0.81 | 0.76 |
| ☐ | SHAPE | `ra2_allies_gi` | Combined Arms **(home)** | Tesla Trooper | 0.00 | 0.88 | 0.40 |
| ☐ | SHAPE | `ra2_allies_gi` | Shattered Paradise | Missile Cyborg | 0.13 | 0.86 | 0.62 |
| ☐ | SHAPE | `ra2_allies_gi` | Crystallized Nexus | Disc Thrower | 0.15 | 0.86 | 0.95 |
| ☐ | SHAPE | `ra2_allies_gi` | Generals Alpha | Ranger | 0.25 | 0.86 | 0.76 |
| ☐ | SHAPE | `ra2_allies_gi` | OpenRA Dune 2000 | Sardaukar | 0.00 | 0.86 | 0.79 |
| ☐ | SHAPE | `ra2_allies_gi` | OpenRA Dune II | Sardaukar | 0.00 | 0.81 | 0.76 |
| ☐ | SHAPE | `ra2_allies_gi` | OpenE2140 | SILVER T | 0.22 | 0.77 | 0.84 |
| ☐ | SHAPE | `ra2_soviets_conscript` | OpenRA Dune 2000 | Fremen | 0.13 | 0.87 | 0.60 |
| ☐ | SHAPE | `ra2_soviets_conscript` | Combined Arms **(home)** | Thief | 0.14 | 0.87 | 0.50 |
| ☐ | SHAPE | `td_gdi_minigunner` | Combined Arms **(home)** | Acolyte | 0.12 | 0.94 | 0.51 |
| ☐ | SHAPE | `td_gdi_minigunner` | OpenRA Red Alert **(home)** | Grenadier | 0.32 | 0.91 | 0.89 |
| ☐ | SHAPE | `td_gdi_minigunner` | Romanov's Vengeance | Cosmonaut | 0.32 | 0.86 | 0.29 |
| ☐ | SHAPE | `td_gdi_minigunner` | Valiant Shades | Cosmonaut | 0.32 | 0.79 | 0.26 |
| ☐ | SHAPE | `td_gdi_minigunner` | Shattered Paradise | Black Hand Trooper | 0.23 | 0.77 | 0.26 |
| ☐ | SHAPE | `td_nod_minigunner` | Romanov's Vengeance | Mechanic | 0.33 | 0.94 | 0.38 |
| ☐ | SHAPE | `td_nod_minigunner` | Shattered Paradise | Tiberian Fiend | 0.26 | 0.91 | 0.76 |
| ☐ | SHAPE | `td_nod_minigunner` | Valiant Shades | Terrorist | 0.21 | 0.85 | 0.42 |
| ☐ | SHAPE | `td_nod_minigunner` | Generals Alpha | Toxin Terrorist | 0.33 | 0.84 | 0.81 |
| ☐ | SHAPE | `td_nod_minigunner` | OpenRA Red Alert **(home)** | Fire Ant | 0.23 | 0.82 | 0.65 |
| ☐ | SHAPE | `tkm_marine` | Romanov's Vengeance **(home)** | Spy | 0.00 | 0.93 | 0.58 |
| ☐ | SHAPE | `tkm_marine` | OpenRA Dune 2000 | Saboteur | 0.29 | 0.93 | 0.41 |
| ☐ | SHAPE | `tkm_marine` | Generals Alpha | Grendier | 0.43 | 0.87 | 0.83 |
| ☐ | SHAPE | `tkm_marine` | Valiant Shades | Guardian G.I. | 0.50 | 0.87 | 0.98 |
| ☐ | SHAPE | `tkm_rifleman` | Generals Alpha | Red Guard | 0.38 | 0.92 | 0.95 |
| ☐ | SHAPE | `tkm_rifleman` | Romanov's Vengeance **(home)** | Guardian G.I. | 0.44 | 0.92 | 0.53 |
| ☐ | SHAPE | `tkm_rifleman` | Combined Arms **(home)** | Grenadier | 0.35 | 0.88 | 0.99 |
| ☐ | SHAPE | `ts_gdi_lightinfantry` | Combined Arms | Mechanic | 0.29 | 0.84 | 0.71 |
| ☐ | SHAPE | `ts_gdi_lightinfantry` | OpenE2140 | SILVER ONE | 0.18 | 0.82 | 0.96 |
| ☐ | SHAPE | `ts_gdi_lightinfantry` | Valiant Shades | Engineer | 0.38 | 0.82 | 0.82 |
| ☐ | SHAPE | `ts_gdi_lightinfantry` | Generals Alpha | Angry Mob | 0.38 | 0.77 | 0.14 |
| ☐ | SHAPE | `ts_gdi_lightinfantry` | Romanov's Vengeance | Slave | 0.22 | 0.76 | 0.64 |
| ☐ | SHAPE | `ts_nod_lightinfantry` | Combined Arms | SPY | 0.12 | 0.90 | 0.53 |
| ☐ | SHAPE | `ts_nod_lightinfantry` | Romanov's Vengeance | Engineer | 0.38 | 0.88 | 0.41 |
| ☐ | SHAPE | `ts_nod_lightinfantry` | OpenE2140 | Android A01 | 0.26 | 0.82 | 0.96 |
| ☐ | SHAPE | `ts_nod_lightinfantry` | Valiant Shades | Engineer | 0.38 | 0.82 | 0.82 |
| ☐ | SHAPE | `ts_nod_lightinfantry` | Generals Alpha | Terrorist | 0.18 | 0.81 | 0.85 |
| ☐ | SHAPE | `ts_nod_lightinfantry` | OpenRA Red Alert | SPY | 0.12 | 0.80 | 0.22 |
| ☐ | SHAPE | `zerg_spithid` | Romanov's Vengeance | Hijacker | 0.27 | 0.94 | 0.58 |
| ☐ | SHAPE | `zerg_spithid` | Valiant Shades | Rocketeer | 0.12 | 0.90 | 0.79 |
| ☐ | SHAPE | `zerg_spithid` | Combined Arms | Black Hand Trooper | 0.09 | 0.87 | 0.70 |
| ☐ | SHAPE | `zerg_spithid` | OpenRA Tiberian Dawn | Stegosaurus | 0.22 | 0.82 | 0.63 |
| ☐ | SHAPE | `zerg_spithid` | Shattered Paradise | Cardinal | 0.13 | 0.80 | 0.65 |

---

## §3 — WEAK proposals — STRUCK, not reviewed

**62 rows across 22 members** were the greedy taking the best of a bad field. Struck per the maintainer's ruling so this sheet only asks about proposals worth judging.

⚠ Listed so nothing vanishes silently — a member whose only proposals were weak should look struck, not unmatched.

| unit | struck | what they were |
|---|--:|---|
| `naxis_naxiriflesoldier` | 5 | CnC Reloaded: Yuri Slave Worker; Generals Alpha: Pilot; Mental Omega: Virus Boss Brute … |
| `asianalliance_asianmilitia` | 4 | CnC Reloaded: Animal Alligator; Crystallized Nexus: Engineer; Mental Omega: Animal Hyena … |
| `forgotten_mutant` | 4 | Mental Omega: Technician; OpenRA Tiberian Dawn: Velociraptor; OpenRA Tiberian Sun: Medic … |
| `ordos_lightinfantry` | 4 | Generals Alpha: RPG Trooper; Mental Omega: Civilian Snow Thin Male; Shattered Paradise: Shadow Warrior … |
| `ra1_soviets_rifleinfantry` | 4 | Crystallized Nexus: Medic; Generals Alpha: Toxin Rebel; Mental Omega: Slave Worker … |
| `tkm_rifleman` | 4 | CnC Reloaded: General Pentagon; Mental Omega: Civilian Snow Female A; Shattered Paradise: Templar … |
| `zerg_spithid` | 4 | CnC Reloaded: Yuri Initiate; Generals Alpha: Flame Tower; Mental Omega: Archer … |
| `forgotten_mutantsoldier` | 3 | Mental Omega: Sciencist; OpenRA Red Alert: Zombie; OpenRA Tiberian Sun: Disc Thrower |
| `latinsyndicate_latinmilitia` | 3 | CnC Reloaded: Vladimir; Mental Omega: Civilian Texan A; OpenRA Dune 2000: Grenadier |
| `ra1_allies_rifleinfantry` | 3 | Generals Alpha: Flamethrower; Mental Omega: Animal Cow; Shattered Paradise: Skirmisher |
| `ra1_soviets_ak47conscript` | 3 | CnC Reloaded: Civilian Texan A; Generals Alpha: Pathfinder; OpenRA Dune 2000: Trooper |
| `tkm_marine` | 3 | CnC Reloaded: Terrorist; Mental Omega: Flak Trooper; OpenE2140: SILVER MAX |
| `futuretech_scoutdroid` | 2 | CnC Reloaded: Secret Service; Mental Omega: 18 RMB |
| `ixian_lightinfantry` | 2 | Generals Alpha: Tank Hunter; Mental Omega: Animal Camel |
| `light_inf` | 2 | CnC Reloaded: Tiberian Fiend; Generals Alpha: Missile Defender |
| `naxis_coneheadsknights` | 2 | CnC Reloaded: Yuri Clone; Mental Omega: Who's that? |
| `naxis_naxiriflerecruit` | 2 | CnC Reloaded: Fake Venom Soldier; Mental Omega: Gene Boss Brute |
| `ra2_soviets_conscript` | 2 | Crystallized Nexus: Rookie; Shattered Paradise: Medic |
| `td_gdi_minigunner` | 2 | CnC Reloaded: Animal Polar Bear; Mental Omega: Psychic Boss Brute |
| `td_nod_minigunner` | 2 | CnC Reloaded: Animal Monkey; OpenRA Tiberian Dawn: Grenadier |
| `ts_gdi_lightinfantry` | 1 | Mental Omega: Civilian Texan C |
| `ts_nod_lightinfantry` | 1 | Mental Omega: Civilian Snow Fat Male |

