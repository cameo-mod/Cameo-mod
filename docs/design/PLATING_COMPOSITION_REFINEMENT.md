# Making each weapon's plating row unique — what physics allows, and what the cycle forbids

**Maintainer 2026-08-17:** *"maybe more reasoning to make each one a finer rating against each
armor type? like you said sword, arrow and rifle might impact the armors slightly differently even
though they are in the same kinetic family right? But you need to use your best real world
reasoning for this to get it right!"*

Nothing applied. This is the reasoning, and it hits a structural wall worth knowing about.

---

## ⚠ FIRST: the kinetic/shaped split is INVISIBLE to every plating

Refining a sword vs an arrow vs a bullet within the kinetic family **does nothing**. Measured:

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
formula ever sees. **The model has five axes but only FOUR distinguishable groups:** `{thermo}`,
`{kinetic + shaped}`, `{blast}`, `{energy}`.

⭐ **So differentiation only works by moving mass ACROSS group boundaries.** Within-group refinement
is arithmetically inert — and this is exactly why the earlier tie was a pigeonhole limit rather than
an oversight: 20 families into 4 groups must collide.

---

## What CAN be differentiated, with the physics

### ✅ Melee — blunt trauma is overpressure, not penetration

A mace beats plate armour where a sword does not, and the reason is not penetration: a swung mass
transmits **shock through** rigid armour. That is overpressure behaviour, i.e. the `blast` axis.

```
Melee: kinetic 0.70, blast 0.30    ->  HAZMAT 135  COMPOSITE 80  BLAST 85  REFLECTOR 100
```

Distinct from Bullet at last, and it reads correctly: a composite plate helps less against a mace
than against a rifle, and a blast-rated plate helps more.

### ⭐ Tesla — a mirror does not stop lightning

**This is the strongest finding here, and it says the current grouping is wrong.** `Laser`, `Prism`
and `Tesla` are all `energy 1.00`, so a REFLECTOR counters all three identically (row 35). But
reflection defeats **radiant** energy; against a conducted electrical arc a mirror does nothing —
the real counters are a Faraday cage and grounding. What an arc *does* deliver is intense local
**resistive heating** and a plasma channel.

```
Tesla: energy 0.60, thermo 0.40    ->  HAZMAT 80  COMPOSITE 100  BLAST 130  REFLECTOR 90
```

So a reflective plating gives only a slight benefit (90, not 35) and an insulating/sealed one gives
a real one (80) — which is physically right and finally separates Tesla from Laser/Prism.

### ✅ Railgun — already correct

`kinetic 0.85, energy 0.15` gives 142.5 / 57.5 / 107.5 / 92.5. The energy share is the EM launch
and plasma sheath, and it already crosses a group boundary, which is why Railgun was never tied.

### ❌ Bullet vs Arrow vs CannonAP — cannot be honestly separated

An arrow is low-velocity with a concentrating bodkin point; a bullet is high-velocity and
deforming; a sabot round is a hypervelocity long rod. Those are real differences — **and all three
live inside `{kinetic + shaped}`**, so no plating in the current cycle can express them. Inventing
a blast or energy share for a bullet to break the tie would be fabricating physics, which is the
`b182fd228` mistake ("blend ladders were FABRICATED, not measured").

### ❌ Flame vs Chemical vs Toxic — the difference is real but not on these axes

* **Flame** — burning fuel: surface heat plus oxygen deprivation.
* **Chemical** — CORROSION (per `PHYSICAL_STATE_SYSTEM.md`, *not* gas): a reaction consuming the
  armour material itself.
* **Toxic** — a biological/chemical agent attacking the **crew**, not the armour.

A sealed, insulated suit is the correct counter to **all three**, so `HAZMAT 35` for each is right.
Their differentiation properly lives in the **class ladder**, where it already exists — Toxic is
`INF > BLD > VEH > AIR` because it attacks people, not plating. Forcing them apart on the plating
axes would misdescribe them.

---

## If full uniqueness is wanted: split the shaped counter out

The honest route is not finer shares — it is **cutting the cycle differently**, because the
real-world distinction the current five collapse is a famous one:

> **Explosive reactive armour defeats shaped charges specifically** — it disrupts the jet before it
> forms — **and does very little against a long-rod kinetic penetrator.** Spaced armour is the same
> story. Composite/ceramic armour is the reverse: excellent against kinetic rods, less so against a
> focused jet.

So `COMPOSITE` currently merges two platings that behave oppositely in reality. Splitting it gives
`kinetic` and `shaped` their own counters, which immediately separates Bullet (kinetic) from
MissileAP (shaped 0.90) from CannonAP (0.75/0.25) **with no invented numbers** — the shares already
exist and would simply become visible.

⚠ Cost: a **sixth plating**, and the closed cycle has to be re-cut so every plating still has
exactly one counter-axis and one weakness. That is a bigger change than a composition tweak and it
touches the "each plating equally exposed" property, so it needs its own ruling.

---

## Separately — the hybrid-armor confirmation needs one clarification

*"the hybrid armors like heroic = plate x scout and the jumpjet = fighter x scout and the cabal
infantry x vehicle armors should be averaged while the armor layer on top should be multiplied
right?"*

Agreed on the outcome, but these are **two different mechanisms** and only one of them is
`MultiArmorCombination`:

| | mechanism | where it happens | rule |
|---|---|---|---|
| `Heroic = Plate × Scout / peak`, `Airborne = Helicopter × Scout / peak` | a **DERIVED Versus COLUMN**, computed once per warhead by the generator | `gen_weapon_template`, DESIGN §12.0b | already a product; `MultiArmorCombination` never sees it |
| CABAL cyborgs / droids carrying **two Armor traits** | runtime multi-armor | `AreaDamageWarhead.MultiArmorCombination` | **Average** — keep |
| a **plating** over the class armor | runtime, one plating at a time | same field | **Multiply** — the change |

So: **Heroic and Airborne are not affected by this decision at all** — they are columns, not
runtime combinations. The dual-armor CABAL units stay on `Average` (multiplying two full ladders is
W20's squaring bug, 40% × 30% = 12%). Platings multiply. That does give each mechanic its own
behaviour, as intended — it just needs implementing as *two* rules in one field, which is why the
plating set is checked by name in `DamageVersus`.
