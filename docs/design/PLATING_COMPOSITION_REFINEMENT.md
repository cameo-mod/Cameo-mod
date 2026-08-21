# Making each weapon's plating row unique â€” what physics allows, and what the cycle forbids

**Maintainer 2026-08-17:** *"maybe more reasoning to make each one a finer rating against each
armor type? like you said sword, arrow and rifle might impact the armors slightly differently even
though they are in the same kinetic family right? But you need to use your best real world
reasoning for this to get it right!"* â€¦ *"I want all weapon families to be a bit more unique so
don't put 3 energy weapons exactly on the same versus value but slightly different"*

**STATUS: DONE** â€” shipped in `e7fa2d57b`. **37 emitted families, 37 distinct rows.** Four groups
of ties are gone: `Laser/Prism/Tesla`, `Chemical/Cryo/Flame/Toxic`, `Concussion/Demolition`, and
`Arrow/Bullet/CannonAP/Melee`. Pinned by `tools/tests/test_plating_composition.py`.

---

## âš  FIRST: the kinetic/shaped split is INVISIBLE to every plating

The obvious approach â€” refine a sword against an arrow against a bullet *within* the kinetic
family â€” **does nothing at all**. Measured before anything was applied:

| family | proposed composition | HAZMAT | COMPOSITE | BLAST | REFLECTOR |
|---|---|--:|--:|--:|--:|
| Bullet | kinetic 0.90, shaped 0.10 | 150 | 50 | 100 | 100 |
| Arrow | kinetic 0.65, shaped 0.35 | 150 | 50 | 100 | 100 |
| CannonAP | kinetic 0.75, shaped 0.25 | 150 | 50 | 100 | 100 |

**Byte-identical.** The reason is in the cycle itself:

```
HAZMAT      counters {thermo}            weak {kinetic, shaped}
COMPOSITE   counters {kinetic, shaped}   weak {blast}
BLAST       counters {blast}             weak {energy}
REFLECTOR   counters {energy}            weak {thermo}
```

`kinetic` and `shaped` **always appear together as a set**, so `sum(kinetic, shaped)` is all the
formula ever sees. **Five axes, but only FOUR distinguishable groups:** `{thermo}`,
`{kinetic + shaped}`, `{blast}`, `{energy}`.

â­ **So a row can only earn its difference by moving mass ACROSS a group boundary.** Within-group
refinement is arithmetically inert. That is also why the original ties were a structural limit
rather than an oversight â€” and why the fix is *not* finer shares but a **second defeat mechanism**
for each family, in a different group. Four groups still give a continuum, so 32 unique rows are
reachable; what is unreachable is 32 unique rows built out of kinetic-vs-shaped hair-splitting.

â›” **What must NOT be done is adding Â±1 noise to break a tie.** The rows are derived from physics;
a fabricated difference is a lie about the model, and this project has already been burned by
exactly that (`b182fd228` â€” *"blend ladders were FABRICATED, not measured"*).

---

## The secondary shares, one family at a time

### Kinetic cluster â€” what happens BEHIND the plate

A solid projectile's only honest non-kinetic share is its **spall**: a penetration event throws
fragments, and a spall liner (`BLAST`) is the real-world answer to it â€” that is what spall liners
are *for*. So the share tracks how violent the event is.

| family | share | why |
|---|---|---|
| `Arrow` | kinetic 1.00 | the pure point: a slow sharp penetrator, no spall and no flash |
| `Sniper` | blast 0.05 | one round, one channel, very little behind-plate debris |
| `Bullet` | blast 0.10 | deforms, cavitates, sprays spall |
| `Melee` | blast 0.25 | â­ blunt trauma is **shock through** rigid armour, i.e. overpressure â€” a mace beats plate where a sword does not, and *not* by penetrating |
| `CannonAP` | thermo 0.15 | a DU dart is **pyrophoric**; the documented behind-armour effect is incendiary as much as mechanical |
| `Railgun` | energy 0.15 | unchanged â€” the EM launch and plasma sheath (this is why Railgun was never tied) |
| `MissileAP` | thermo 0.05 | behind-armour incendiary from the jet |

`Melee` now reads correctly in both directions: a composite plate helps *less* against a mace
than against a rifle (53 vs 42), and padding helps *more* (62 vs 68).

### Blast cluster

`Concussion` keeps `blast 1.00` as the pure overpressure archetype. `Demolition` takes
`thermo 0.15` for the detonation flash a contact charge delivers â€” which is what incendiary
cutting charges exploit. A sealed suit now gives a little protection against one (65) and none
against the other (70).

### Thermochemical cluster

A sealed insulated suit really is the right counter to all four, so they keep a thermo **lead**
and separate on their second mechanism:

* `Toxic` **1.00 thermo** â€” an agent attacking the **crew** is exactly what a hazmat suit is for,
  so this is the pure case. It is also the only family a REFLECTOR still makes *worse* (102):
  the purest agent is the one that fouls a mirror.
* `Flame` **blast 0.15** â€” fuel **deflagrates**: a pressure pulse and oxygen depletion.
* `Chemical` **shaped 0.25** â€” corrosion (per `PHYSICAL_STATE_SYSTEM.md`, *not* gas) eats a
  channel through the material, i.e. localised material removal. **Ceramics are chemically
  inert** where steel and reactive armour are not, so `COMPOSITE` earns a partial answer (62).
* `Cryo` **energy 0.55, thermo 0.25, kinetic 0.20** — Laser×Prism coldray. The shield table
  sees mostly coherent energy field-coupling (rank 0.75); the thermal load is still real
  (insulation stops it), and the small kinetic share is cryogenic **embrittlement**: what breaks
  is frozen material fracturing.

### Energy cluster â€” how much of the delivered damage is THERMAL

That share is also the order in which a mirrored coating stops being the right idea:

| family | composition | REFLECTOR | why |
|---|---|--:|---|
| `Prism` | energy 0.90, thermo 0.10 | 41 | focused visible light: the purest radiant beam, and a mirror is its exact counter |
| `Tesla` | energy 0.75, thermo 0.20, blast 0.05 | 49 | a conducted arc; the thermal part is resistive heating and the blast part is the **thunderclap** â€” thunder is literally an overpressure wave |
| `Laser` | energy 0.65, thermo 0.35 | 58 | coherent IR, but the **kill** is ablation |

---

## âš  Two of my own claims were wrong, and are corrected here

**1. "A mirror does not stop lightning."** I argued REFLECTOR should barely help Tesla
(`energy 0.60 / thermo 0.40`), because reflection defeats *radiant* energy while an arc needs a
Faraday cage. **Overruled** â€” maintainer: *"the tesla is the opposite [of Inferno]: it's mostly
energy and a bit of thermal"* â€” and the ruling is defensible on physics I had missed: a mirrored
plating is a **metal skin**, i.e. a conductor, which spreads and grounds an arc. Same benefit,
different mechanism. `PHYSICS_RANK` also already called Tesla the field-coupling champion at 1.00,
so "mostly energy" is what the other table had been saying all along.

**2. "Energy must EXCEED thermo or a 50/50 blend cancels."** True of the **raw** row, false of the
**shipped** one. Every column is pinned to `PLATING_TARGET_MEAN`, so at mean 70 a value only stops
being a benefit above ~143 raw. A thermo-LED heat ray still gets a real reflector benefit:

```
Inferno  thermo 0.65 / energy 0.35  ->  HAZMAT 47   REFLECTOR 79
```

Both reduce it, HAZMAT far more â€” which is exactly the earlier request (*"reduced by both hazmat
and reflector armor then? But maybe more by hazmat"*). **The mean-70 ruling is what made the
maintainer's "mostly thermal" reading available**; under the old mean of 100 a 50/50 really did
land on ~97, i.e. nothing.

---

## The anti-drift guard: `_rank_blend` is retired

`_rank_blend` derived Inferno's thermo/energy split from `PHYSICS_RANK` arithmetically. That
**over-reached**: the two tables answer different questions â€” rank asks how much of a discharge a
**force field** absorbs, composition asks what reaches **matter** â€” and `Railgun` has always been
the standing proof that they are not one axis (rank 0.78, a nearly pure kinetic slug). Deriving
one from the other therefore had to be overruled the moment a ruling touched either table, which
is precisely what happened.

`rank_composition_conflicts()` keeps only what the two tables genuinely share, and constrains no
share:

> a family the shield table calls **field-coupling** (`PHYSICS_RANK >= 0.56`, the table's own band
> boundary) must have **some** energy share; one it calls thermal/kinetic must have **none**.

That catches exactly the drift that shipped twice (`Inferno` 0.57 (Flame×Prism) and `Cryo` 0.75 (Laser×Prism), with explicit thermo/energy/kinetic shares) without pretending to know the exact split.

---

## The shipped matrix

37 families, 37 distinct rows (ARMOR excluded — it is flat by definition).

| family | HAZMAT | COMPOSITE | BLAST | REFLECTOR | ARMOR | composition |
|---|--:|--:|--:|--:|--:|---|
| Arrow | 104 | 35 | 71 | 69 | 70 | kinetic 1.00 |
| Bullet | 101 | 42 | 68 | 69 | 70 | kinetic 0.90, blast 0.10 |
| Railgun | 99 | 41 | 77 | 63 | 70 | kinetic 0.85, energy 0.15 |
| MissileAP | 97 | 44 | 68 | 70 | 70 | shaped 0.85, blast 0.10, thermo 0.05 |
| Melee | 96 | 53 | 62 | 69 | 70 | kinetic 0.75, blast 0.25 |
| CannonAP | 94 | 41 | 71 | 74 | 70 | kinetic 0.70, thermo 0.15, shaped 0.15 |
| Flak | 90 | 63 | 57 | 69 | 70 | kinetic 0.60, blast 0.40 |
| MissileAA | 89 | 67 | 55 | 69 | 70 | kinetic 0.55, blast 0.45 |
| MissileHE | 78 | 88 | 45 | 69 | 70 | blast 0.75, shaped 0.25 |
| PhotonCannon | 78 | 74 | 58 | 70 | 70 | blast 0.46, kinetic 0.34, thermo 0.11, energy 0.08, shaped 0.01 |
| MissileChem | 75 | 53 | 70 | 82 | 70 | shaped 0.55, thermo 0.40, blast 0.05 |
| CannonHE | 73 | 99 | 39 | 69 | 70 | blast 0.90, kinetic 0.10 |
| Quantum | 73 | 61 | 89 | 57 | 70 | energy 0.52, kinetic 0.28, thermo 0.18, blast 0.02 |
| CannonChem | 73 | 51 | 71 | 84 | 70 | thermo 0.45, kinetic 0.35, shaped 0.20 |
| Concussion | 70 | 106 | 36 | 69 | 70 | blast 1.00 |
| Sonic | 70 | 95 | 57 | 58 | 70 | blast 0.70, energy 0.30 |
| Cryo | 68 | 63 | 91 | 58 | 70 | energy 0.55, thermo 0.25, kinetic 0.20 |
| Storm | 66 | 74 | 96 | 45 | 70 | energy 0.80, thermo 0.10, blast 0.10 |
| Prism | 66 | 70 | 103 | 41 | 70 | energy 0.90, thermo 0.10 |
| Demolition | 64 | 100 | 41 | 74 | 70 | blast 0.85, thermo 0.15 |
| Magic | 63 | 78 | 86 | 55 | 70 | energy 0.60, thermo 0.20, blast 0.20 |
| Tesla | 63 | 72 | 96 | 50 | 70 | energy 0.75, thermo 0.20, blast 0.05 |
| Waveforce | 62 | 64 | 81 | 73 | 70 | thermo 0.43, energy 0.31, kinetic 0.17, shaped 0.05, blast 0.04 |
| MissileFire | 59 | 82 | 55 | 83 | 70 | blast 0.45, thermo 0.42, shaped 0.12 |
| CannonFire | 57 | 87 | 53 | 83 | 70 | blast 0.53, thermo 0.42, kinetic 0.05 |
| Laser | 57 | 70 | 95 | 58 | 70 | energy 0.65, thermo 0.35 |
| Thermobaric | 56 | 92 | 50 | 82 | 70 | blast 0.60, thermo 0.40 |
| Chemical | 52 | 62 | 71 | 94 | 70 | thermo 0.75, shaped 0.25 |
| Plasma | 50 | 70 | 87 | 72 | 70 | thermo 0.55, energy 0.45 |
| Inferno | 47 | 70 | 84 | 79 | 70 | thermo 0.65, energy 0.35 |
| Flame | 40 | 76 | 66 | 98 | 70 | thermo 0.85, blast 0.15 |
| Toxic | 35 | 70 | 71 | 103 | 70 | thermo 1.00 |

| CannonNuke | 62 | 91 | 51 | 75 | 70 | blast 0.65, thermo 0.25, kinetic 0.05, energy 0.05 |
| MissileNuke | 74 | 64 | 65 | 76 | 70 | shaped 0.43, thermo 0.28, blast 0.25, energy 0.05 |
| MissileQuantum | 85 | 52 | 79 | 64 | 70 | shaped 0.43, energy 0.26, kinetic 0.14, thermo 0.12, blast 0.06 |
| MissileTesla | 79 | 58 | 83 | 60 | 70 | shaped 0.43, energy 0.38, thermo 0.13, blast 0.08 |
| MissileThermobaric | 68 | 91 | 47 | 74 | 70 | blast 0.71, thermo 0.17, shaped 0.13 |

**`ARMOR` is 70 for every family BY DESIGN** â€” it is the generic hedge that *"receives 100% damage
from everything"*, so it must be flat. Varying it would contradict its purpose, and
`test_the_generic_plating_stays_flat` pins that.

### "evenly distributed among all the axis" â€” measured

| | axis share | group share (what the cycle READS) |
|---|---|---|
| before | thermo 25.8, energy 25.0, blast 22.9, kinetic 19.7, shaped 6.7 | 1.15Ã— spread |
| **after** | thermo 27.4, blast 24.9, energy 21.4, kinetic 18.6, shaped 7.7 | **1.28Ã— spread** |

Groups after: `thermo 27.4%`, `kinetic+shaped 26.4%`, `blast 24.9%`, `energy 21.4%`.

âš  **Honest trade:** group evenness got slightly *worse* (1.15Ã— â†’ 1.28Ã—), because the energy
families gave mass to thermo. Each plating still faces a quarter of the roster Â±3 points, which is
well inside "even" â€” the ties were the bigger problem and this is what buying uniqueness cost.
The raw `shaped` figure stays low (7.7%) and always will: only `MissileAP` is shaped-led, and the
cycle folds shaped into `COMPOSITE` anyway, which is how a real tank is built.

### Under multiplication

**6 cells of 185 increase damage, worst Ã—1.06** (was 13 of 100 at Ã—1.07 â€” better on both counts):
`Arrow`/`Concussion` 106, `Prism` 103, `Bullet`/`Toxic` 102. Rows span 3.03:1; multiplied by the
class ladder that is **5.32:1**, inside the documented 2â€“8Ã— band (DESIGN Â§12.0 rule 5).

---

## Still open: if EVERY CELL must be unique, re-cut the cycle

Full-row uniqueness is done. Individual **cells** still coincide (`REFLECTOR` has 23 distinct
values across 37 families), and the honest route to more is not finer shares but **cutting the
cycle differently**, because `COMPOSITE` currently merges two platings that behave oppositely in
reality:

> **Explosive reactive armour defeats shaped charges specifically** â€” it disrupts the jet before it
> forms â€” **and does very little against a long-rod kinetic penetrator.** Spaced armour is the same
> story. Composite/ceramic armour is the reverse: excellent against kinetic rods, less so against a
> focused jet.

Splitting that counter would separate `Bullet` (kinetic) from `MissileAP` (shaped 0.85) from
`CannonAP` (0.70/0.15) **with no invented numbers** â€” the shares already exist and would simply
become visible. âš  Cost: a **sixth plating**, and the closed cycle has to be re-cut so every
plating still has exactly one counter-axis and one weakness. That needs its own ruling.

---

## Separately â€” the hybrid-armor confirmation needs one clarification

*"the hybrid armors like heroic = plate x scout and the jumpjet = fighter x scout and the cabal
infantry x vehicle armors should be averaged while the armor layer on top should be multiplied
right?"*

Agreed on the outcome, but these are **two different mechanisms** and only one of them is
`MultiArmorCombination`:

| | mechanism | where it happens | rule |
|---|---|---|---|
| `Heroic = Plate Ã— Scout / peak`, `Airborne = Helicopter Ã— Scout / peak` | a **DERIVED Versus COLUMN**, computed once per warhead by the generator | `gen_weapon_template`, DESIGN Â§12.0b | already a product; `MultiArmorCombination` never sees it |
| CABAL cyborgs / droids carrying **two Armor traits** | runtime multi-armor | `AreaDamageWarhead.MultiArmorCombination` | **Average** â€” keep |
| a **plating** over the class armor | runtime, one plating at a time | same field | **Multiply** â€” the change |

So: **Heroic and Airborne are not affected by this decision at all** â€” they are columns, not
runtime combinations. The dual-armor CABAL units stay on `Average` (multiplying two full ladders is
W20's squaring bug, 40% Ã— 30% = 12%). Platings multiply. That does give each mechanic its own
behaviour, as intended â€” it just needs implementing as *two* rules in one field, which is why the
plating set is checked by name in `DamageVersus`.
