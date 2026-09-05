# The missing classes — air, naval, economy

**Measured 2026-09-02** by `tools/balance/class_membership.py` over the committed ledgers.
Every number here is re-measurable with the commands in §5; nothing is estimated.

> ⛔ **PROPOSAL, NOT LAW.** `docs/DESIGN.md` and `docs/design/FORMULA_V2.md` are binding; this file
> is a decision list for the maintainer. Nothing here is applied.

---

## §0 — The hole

`docs/balance/class_anchors.json` defines **27 classes**. Not one of them is an air class, a naval
class, or an economy class. **219 units — a third of the roster — have a unit template but no class
to belong to, so the balance pipeline cannot price them at all.**

This is the largest single gap left in PRIORITY 0 item 1.

⭐ **The design already treats these templates as distinct cohorts.** `DESIGN.md` gives
fighters/bombers their own turn-rate law (**F17**, `Speed/15`, frontal-weapon craft 2×) and
helicopters/spaceships another (**F19**, `Speed/5`), keyed on the template. `FORMULA_V2.md` §7
already queues *"Next classes: bomber (replace reload-250 convention), defense…"*. The templates
are already cohorts with their own laws — they are simply not classes yet. **So the classes should
mirror the templates, exactly as the thirteen ground-vehicle classes already do.**

---

## §1 — ⛔ FIRST, A PREREQUISITE: 22 of the 219 are not units

**22 members of these templates are not buildable.** They are spawned carrier drones, suicide
drones and interceptors parked in the air templates because no drone template existed:

| template | members | **not buildable** | examples |
|---|--:|--:|---|
| `^BomberTemplate` | 34 | **15** | `kami_asdf.asian`, `cabal_hunterdrone`, `kami.asian`, `tkmsuicidedrone`, `cruiser_f.steel`, `landcarr_drone.futu` |
| `^FighterTemplate` | 22 | **6** | `schwarzermond_drone`, `naxis_interceptor`, `scalpelMG.steel`, `gdirigdrone`, `scalpelAA.steel` |
| `^HelicopterTemplate` | 58 | 1 | `tkmdrone` |
| `^UnarmedTransportHelicopterTemplate` | 9 | 1 | `TRAN` |

⛔ **This is already a maintainer ruling, unapplied.** 2026-09-02: *"carrier drones… currently they
have the bomber template right? But it should be their own carrier drone template that belongs to
their own balancing"* and *"husks and other things must be also separated so they don't appear as
regular units in the balance formula."*

⚠ **And it MUST be done before any anchor is chosen.** The baseline law takes the cheapest member
of a class as its anchor. Measured against all members, that gives:

| class | anchor on ALL members | anchor on BUILDABLE members | error |
|---|---|---|--:|
| bomber | `kami_asdf.asian` @ 50 | `ordos_airmine` @ 500 | **10×** |
| fighter | `schwarzermond_drone` @ 120 | `zerg_scourge` @ 700 | **5.8×** |
| helicopter | `tkmdrone` @ 25 | `wc2_humans_flyingmachine` @ 350 | **14×** |

Three of the nine air anchors would have been planted on a drone, and every price in those classes
measured against it. `^BomberTemplate` is currently **44% carrier-drone parking lot**.

**Action: create `^CarrierDroneTemplate`, move the 22, and exclude it from the pricing formula**
(its members are priced as a fraction of their carrier — the recorded rule is 20% of carrier cost
÷ drone count, and drone damage folds into the carrier's).

---

## §2 — The nine proposed classes

One class per template, mirroring the ground-vehicle pattern. Costs are **buildable members only**,
so the anchors below are already drone-corrected.

### Air — 5 classes, 121 buildable units

| proposed class | template | buildable | cost min · median · max | anchor candidate (cheapest) |
|---|---|--:|---|---|
| `helicopter` | `^HelicopterTemplate` | 57 | 350 · 2,000 · 6,000 | `wc2_humans_flyingmachine` @ 350 |
| `bomber` | `^BomberTemplate` | 19 | 500 · 1,900 · 7,000 | `ordos_airmine` @ 500 |
| `fighter` | `^FighterTemplate` | 16 | 700 · 1,350 · 3,500 | `zerg_scourge` @ 700 |
| `spaceship` | `^SpaceshipTemplate` | 21 | 1,500 · 5,000 · 15,000 | `wc2_humans_gryphonrider` @ 1,500 |
| `air_transport` | `^UnarmedTransportHelicopterTemplate` | 8 | 2,000 · 3,350 · 6,000 | `zerg_broodweaver` @ 2,000 |

⚠ `^UnarmedTransportHelicopterTemplate` inherits `^HelicopterTemplate`, so its members are already
helicopters structurally. Two readings, and it is a ruling: **its own class** (they carry no weapon
and are priced on capacity, like the naval transports you already ruled get their own template), or
**a sub-cohort of `helicopter`**. The table assumes its own class.

### Naval — 3 classes, 47 buildable units

| proposed class | template | buildable | cost min · median · max | anchor candidate |
|---|---|--:|---|---|
| `scout_ship` | `^ScoutShipTemplate` | 21 | 500 · 1,300 · 3,600 | `ksub.asian` @ 500 |
| `artillery_ship` | `^ArtilleryShipTemplate` | 16 | 1,750 · 3,175 · 4,500 | `karrier.asian` @ 1,750 |
| `battleship` | `^BattleShipTemplate` | 10 | 600 · 1,600 · 2,600 | `ra2_soviets_seascorpion` @ 600 |

⚠ **`battleship` is cheaper than `artillery_ship` at every point of its range** (600–2,600 vs
1,750–4,500). Either the naming is inverted against the intent, or the battleships are underpriced.
Worth a look before the anchors are signed — it is the kind of inversion the class system exists to
surface.

⚠ `dreadnought` already exists as one of the 27 classes and is **not naval**. ⛔ It is also not a
mech class — that reading was inferred from its members and corrected 2026-09-03. It is defined by
SHAPE: heavy, slow, frontal-facing (no turret), more range and damage than a regular tank. Its
current members happen to be mechs; the definition is not.

### Economy — 1 class, 27 units

| proposed class | template | buildable | cost min · median · max | anchor candidate |
|---|---|--:|---|---|
| `harvester` | `^HarvesterTemplate` | 27 | 250 · 1,000 · 1,200 | `asianalliance_droneminer` @ 250 |

⚠ Only **7 of 27** harvesters are armed. `FORMULA_V2.md` §6c does not cover economy units at all,
and a harvester's value is throughput, not DPS — so this class probably needs a **different
formula**, not just an anchor. Same shape as `support`, which §6b already exempts as
ability-priced. ⚠ Also recorded: the naval-harvester mechanic does not work — *"they should travel
between the two naval buildings, load up in one and move to the other. Currently we can't do
that"* — and there are no water oil patches.

---

## §3 — What this buys

| | now | after |
|---|--:|--:|
| units with a class | 660 of 993 (66%) | **879 of 993 (89%)** |
| classes | 27 | **36** (+9), or 35 if air transport folds into helicopter |
| units the pipeline cannot price | 219 | **0** |

The remaining 114 are PRIORITY 0 item 2 — units with no template at all, which is a separate sweep
(`audit_class_templates.py`, 8 cohorts + 32 singles).

---

## §4 — The rulings needed, in order

1. **`^CarrierDroneTemplate`** — create it, move the 22 spawned units, exclude from the formula.
   ⛔ Blocks everything else: three air anchors are wrong until it lands.
2. **Approve the nine class ids** (or rename them — `air_transport`, `scout_ship`,
   `artillery_ship`, `battleship` are my names, not yours).
3. **Air transport**: own class, or a sub-cohort of `helicopter`?
4. **Harvester**: its own formula, or exempt like `support`?
5. **Battleship vs artillery ship** — is the price inversion intended?

Once 1–2 are ruled, adding the classes to `class_anchors.json` and the entries to
`class_membership.SUBTYPE_TO_CLASS` is mechanical, and coverage goes to 89% in one commit.

---

## §5 — Reproduce

```sh
python tools/balance/class_membership.py --gaps    # the 219, by template
python tools/balance/anchor_readiness.py           # anchor integrity, after the classes exist
python tools/audit/audit_class_templates.py        # PRIORITY 0 item 2, the other 114
```
