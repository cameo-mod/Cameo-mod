# Class status board — all 27, one by one

**Measured 2026-09-02** by `tools/balance/anchor_readiness.py`, *after* the class-membership fix
(`tools/balance/class_membership.py`) took coverage from 18% to 32% of buildable units. Every
number regenerates; nothing here is hand-typed.

> ⛔ **A STATUS BOARD, NOT A RULING.** Sign-off is a maintainer act (`signed_off: true` in
> `class_anchors.json`), never an agent's. This file says which classes are *ready* to be signed.

**Companion:** [`MISSING_CLASSES.md`](MISSING_CLASSES.md) — the nine classes that do not exist yet
(air, naval, economy; 219 units).

---

## §0 — The headline

⭐ **The sign-off queue was empty before this. It is now populated, and three classes are ready.**
That is the direct consequence of fixing membership: `fit_class` cannot score a class whose members
it cannot see.

| | |
|---|--:|
| classes defined | **27** |
| signed off | **8** |
| ✅ ready to sign TODAY | **3** |
| ⚠ close — review outliers first | 7 |
| ⛔ anchor does not describe its members | 12 |
| ⚠ too few scored members to judge | 7 |
| ⛔ could not be fitted at all | 1 (`support`) |

⭐ **And the structural gate is nearly clear.** `BALANCE_PROGRAM_PLAN.md` §0a is binding — weapon
structure before pricing, because `K` moves when mains collapse. **Only 7 tagged units still fire
2+ main warheads, and 22 of 27 classes owe nothing at all.** Pricing is no longer blocked
class-wide; it is blocked on five named units.

---

## §1 — ✅ Ready to sign today (3)

`median |Δ|` is how far the class formula's price sits from the unit's actual cost.

| class | scored | median \|Δ\| | within 10% | worst | why it is ready |
|---|--:|--:|--:|--:|---|
| `dreadnought` | 5 | **2%** | 80% | 27% | the anchor prices its own class almost exactly |
| `scout` | 7 | **4%** | 71% | 57% | one outlier at 57% drags the worst column; the median is solid |
| `heavy_infantry` | 3 | **7%** | 67% | 47% | 3 scored is the minimum that means anything, and it clears |

⛔ **CORRECTED 2026-09-03 — `dreadnought` is NOT a mech class.** That was inferred from its
current members and it was wrong. The maintainer's definition: **heavy, slow, frontal-facing (no
turret), with more range and damage than a regular tank** — *"like tank destroyers but with more
range and armor and slower"*. Mechs (`terran_warhound`, `naxis_sturmtiger`,
`asianalliance_pulverizermecha`, `ixian_neocymek`) are an EXAMPLE of the shape, not the class.
⚠ It is also not naval — the naval classes do not exist yet (`MISSING_CLASSES.md`).

⚠ Its anchor `terran_warhound` sits at the **80th percentile** of its 5 members, so the zero point
is near the top of the population rather than the middle. It still prices the class to 2%, so this
is a note, not a blocker — but if the anchor is ever moved, the fit has to be re-read.

---

## §2 — ⚠ Close: review the outliers, then sign (7)

| class | scored | median \|Δ\| | within 10% | worst | the outlier to look at |
|---|--:|--:|--:|--:|---|
| `tank_destroyer` | 5 | 11% | 40% | 47% | — |
| `closecombat` | 4 | 12% | 50% | 74% | — |
| `mbt` | 40 | 14% | 45% | **348%** | the 348% is one unit against 40 members |
| `archer` | 4 | 14% | 50% | 82% | — |
| `light_tank` | 16 | 16% | 38% | **284%** | — |
| `heavy_sniper` | 3 | 22% | 33% | **334%** | ⚠ and its anchor is not even in the class (§5) |
| `rocket_trooper` | 2 | 23% | 0% | 25% | only 2 scored — under the 3-member floor |

⭐ **`mbt` at 14% across 40 scored members is the strongest large-class result on the board**, and
it is the class every other ground price is compared against. The 348% worst case is a single unit,
not a systemic miss.

---

## §3 — ⛔ The anchor does not describe its members (12)

A median error above 30% means the class zero point is in the wrong place. `anchor_readiness`'s own
guidance applies: **fix this by moving the ANCHOR, not the formula.**

| class | scored | median \|Δ\| | within 10% | worst | anchor percentile |
|---|--:|--:|--:|--:|--:|
| `missile_vehicle` | 13 | 30% | 23% | 373% | — |
| `high_tech_tank` | 25 | 31% | 16% | 178% | — |
| `fire_support` | 27 | 35% | 11% | 339% | 16th of 31 |
| `anti_air_vehicle` | 12 | 36% | 33% | 686% | — |
| `line_breaker` | 25 | 40% | 20% | 132% | 18th of 33 |
| `special_forces` | 15 | **57%** | 13% | 523% | 12th of 16 — **SIGNED** |
| `artillery` | 24 | 59% | 12% | 425% | 7th of 28 |
| `artillery_tank` | 7 | 63% | 29% | 101% | 93rd of 14 |
| `scout_vehicle` | 27 | **122%** | 11% | 690% | 4th of 51 |
| `epic_vehicle` | 20 | **125%** | 0% | 555% | 83rd of 24 |

⛔ **`special_forces` is SIGNED at a 57% median error**, anchored on an actor at the 12th percentile
of its own class. `anchor_readiness` names this as the real thing rather than a restat artifact:
*"the zero point is an outlier at the bottom of the population it defines, so every member is
measured against a ruler planted in the wrong place."* **It is signed and it is wrong** — worth an
unsign-and-re-anchor.

⚠ **`scout_vehicle` (122%) and `epic_vehicle` (125%) are the two worst, and both may be
restat artifacts.** The board's own caveat: for the 13 classes on the 2026-08-01 LOCKED table the
anchor actor is still PRE-RESTAT, so its percentile is measured on stats the design already intends
to replace — `scout_vehicle`'s buggy reads 4th at hp 20000 against a spec of 30000. **Apply the
restat, then re-read.** Do not re-anchor these two on today's numbers.

---

## §4 — ⚠ Too few scored members to judge (7)

These now have members — the membership fix gave five of them their first — but `fit_class` scores
only one or two, so the 0% median is the anchor pricing itself, not a result.

`commando` · `flying_infantry` · `grenadier` · `melee` · `pure_sniper` · `mortar` ·
`rocket_trooper`

⚠ **A one-member fit table reads 0% by construction.** Three of these (`flying_infantry`,
`grenadier`, `mortar`) are **SIGNED** — signed against a table with one row in it.

**What they need:** not a ruling, but `cost0`/spec data so `fit_class` can score the members that
now exist. That is mechanical follow-on work from the membership fix.

---

## §5 — ⛔ The two structural defects left in the anchor set

1. **`support` cannot be fitted at all** — 110 members, **0 scored**. ⭐ **This is correct, not a
   defect.** `FORMULA_V2.md` §6b prices support *"n/a (ability-priced)"*, and the maintainer
   confirmed 2026-09-02: *"Support units are exempt from the balance pipeline and are all hand
   tuned."* It should be marked EXEMPT in `class_anchors.json` so it stops appearing as a failure.
2. **`heavy_sniper`'s anchor is not in its own class.** `td_gdi_heavysniper` sits in
   `^SniperInfantryTemplate`, so it classifies `pure_sniper`. `^HeavySniperInfantryTemplate` is one
   of the five dead templates (`CLASS_MOVES.md` §0) — **the anchor is SIGNED for a class whose
   template has zero inheritors.** It is the last of the 27 still outside its own class (was 10).

---

## §6 — The structural gate: 22 of 27 classes are clear

Only **7 tagged units** still fire 2+ main warheads, across five classes:

| class | owing a split | of tagged | worst offender |
|---|--:|--:|---|
| `mbt` | 3 | 46 | `japan_chihaheavytank` via `Type97PlasmaCannon` (3 mains) |
| `tank_destroyer` | 1 | 5 | `ra1_allies_alliedtankdestroyer` (2 mains) |
| `light_tank` | 1 | 16 | `ra1_allies_sheridanassaulttank` (2 mains) |
| `commando` | 1 | 30 | `terran_jimraynor` (2 mains) |
| `line_breaker` | 1 | 33 | `ts_gdi_disruptor` via `TSSonicZapWeapon` (2 mains) |

**The other 22 classes owe nothing and are structurally ready to price.**

---

## §7 — ⚠ One finding that constrains every future membership check

> Median distance to OWN anchor: **2.94**. Median distance BETWEEN anchors: **1.21**.

**Units sit further from their own class anchor than the anchors sit from each other.** So class
boundaries are *not* recoverable from stats — they are role judgements, which is exactly why
membership comes from the TEMPLATE and not from a clustering rule. Any future check that tries to
police membership numerically will be wrong.

---

## §8 — What to do next, in order

1. **Sign `dreadnought`, `scout`, `heavy_infantry`** — ready today, maintainer order only.
2. **Unsign and re-anchor `special_forces`** — signed at 57% median error on a 12th-percentile
   anchor.
3. **Mark `support` EXEMPT** in `class_anchors.json` — it is hand-tuned by ruling and should not
   read as an unfitted failure.
4. **Fix `heavy_sniper`'s anchor** — either move `td_gdi_heavysniper` into
   `^HeavySniperInfantryTemplate` (reviving a dead template) or re-anchor the class.
5. **Seed `cost0` for the 7 under-scored classes** so their new members can be fitted — mechanical.
6. **Apply the restat** before touching `scout_vehicle` / `epic_vehicle`.
7. **Split the 7 remaining multi-main weapons**, and the structural gate is fully clear.

```sh
python tools/balance/anchor_readiness.py     # this board
python tools/balance/class_membership.py     # membership + coverage
python tools/balance/check_band.py           # per-class band occupancy
```

---

## §9 — ⛔ The `dreadnought` shape sweep (2026-09-03) — REPORT ONLY

Run after the class was defined by SHAPE for the first time: *heavy, SLOW, frontal-facing (NO
TURRET), with more range and damage than a regular tank.* MBT median for comparison (n=39):
**hp 110,000 · speed 80 · range 5,672 · dmg 12,000**. **Zero of 39 MBTs are turretless.**

> ⚠ **Proposes, never decides.** Membership comes from the TEMPLATE. §7's finding stands: units sit
> further from their own class anchor (2.94) than the anchors sit from each other (1.21), so no
> stat rule can settle a class boundary.

### §9.1 — ⛔ Only 2 of the 5 current members fit the definition

| unit | turret | hp | speed | range | dmg | verdict |
|---|:--:|--:|--:|--:|--:|---|
| `naxis_sturmtiger` | no | 250,000 | 30 | 14,000 | 80,000 | ⭐ fits |
| `terran_warhound` | no | 300,000 | 45 | 7,156 | 16,006 | ⭐ fits |
| `asianalliance_pulverizermecha` | no | 285,000 | 55 | 7,020 | 10,002 | ⚠ damage **below** the MBT median |
| `ixian_neocymek` | **YES** | 300,000 | 45 | 6,787 | 32,000 | ⛔ **has a turret** |
| `schwarzermond_neojagdpanzer` | **YES** | 450,000 | 45 | 7,379 | 90,000 | ⛔ **has a turret** — and it is named *Jagdpanzer*, a turretless tank destroyer |

### §9.2 — 10 units elsewhere fit the shape

| current class | unit | hp | speed | range | dmg |
|---|---|--:|--:|--:|--:|
| `fire_support` | `protoss_reaver` | 275,000 | 40 | 7,777 | 200,075 |
| `fire_support` | `asianalliance_heavyrailguntank` | 250,000 | 50 | 10,000 | 37,000 |
| `fire_support` | `asianalliance_railguntank` | 160,000 | 65 | 8,888 | 25,000 |
| `high_tech_tank` | `cabal_avatar` | 1,000,000 | 25 | 6,332 | 81,000 |
| `line_breaker` | `futuretech_plasmastrider` | 240,000 | 40 | 7,000 | 30,010 |
| `line_breaker` | `steelconsortium_poseidontank` | 125,000 | 50 | 6,333 | 14,000 |
| `mbt` | `cabal_widow` | 120,000 | 60 | 6,813 | 121,000 |
| `tank_destroyer` | `ra2_allies_tankdestroyer` | 145,000 | 65 | 7,040 | 80,000 |
| `tank_destroyer` | `naxis_jagdpanzer` | 125,000 | 50 | 7,396 | 60,000 |
| `tank_destroyer` | `ra1_allies_alliedtankdestroyer` | 120,000 | 60 | 6,819 | 24,000 |

### §9.3 — ⛔ The dreadnought / tank-destroyer distinction does NOT hold in the roster

The definition separates them: a dreadnought is *"like tank destroyers but with more range and
armor and slower"*. Measured:

| class | n | hp | speed | range | dmg | turretless |
|---|--:|--:|--:|--:|--:|--:|
| `dreadnought` | 5 | **300,000** | **45** | 7,156 | 32,000 | **3/5** |
| `tank_destroyer` | 5 | 120,000 | 65 | 7,132 | 56,021 | **5/5** |

* ⭐ **Armour and speed separate them correctly** — 2.5× the HP, two-thirds the speed.
* ⛔ **Range does NOT.** 7,156 against 7,132 — statistically identical, where the definition
  requires dreadnoughts to out-range tank destroyers.
* ⛔ **Tank destroyers honour the frontal-facing rule better than the class defined by it** — 5/5
  turretless against 3/5.

**So the class needs a stat pass, not just an anchor**, and three of the ten candidates above are
tank destroyers, which a stat rule alone can never separate from dreadnoughts.

⭐ `turreted` is now recorded by `extract_stats.py`, so both halves of the definition become
machine-checkable on the next ledger refresh. **138 of 305 Cameo vehicles are turretless.**

---

## §10 — ⛔ The range inversion is in the ANCHORS, not just the yaml (2026-09-03)

§9's table read the LEDGER — what yaml ships today. The maintainer's response: *"These are still the
old values right? I was already planning to change them with the class anchors."* Correct, and the
anchors were checked. **The defect survives into the spec.**

| class | anchor | hp0 | speed0 | range0 | dps0 | cost0 | sight |
|---|---|--:|--:|--:|--:|--:|--:|
| `dreadnought` | `terran_warhound` | **1,150,000** | **50** | **7,000** | 3,750 | 3,000 | 8,000 |
| `tank_destroyer` | `naxis_hetzer` | 150,000 | 70 | **7,500** | 900 | 600 | 7,500 |
| `mbt` | `tiger.nax` | 240,000 | 95 | 5,500 | 600 | 800 | 6,000 |

Against the definition — *"like tank destroyers but with more range and armor and slower"*:

| axis | dreadnought ÷ tank destroyer | verdict |
|---|--:|---|
| HP | **7.7×** | ⭐ tougher — holds |
| speed | **0.71×** | ⭐ slower — holds |
| DPS | 4.2× | harder-hitting |
| **range** | **0.93×** | ⛔ **SHORTER — the definition is inverted in the spec** |

⭐ **And the anchor knows it should be longer.** Its own comment reads *"Heavy long-range assault
walker"*, and its `reveals_shroud` is **8,000** against the tank destroyer's 7,500 — the sight
range already carries the intent the weapon range contradicts.

**So this cannot be fixed by restating the units.** `dreadnought.spec.range0_wdist` has to move above
`tank_destroyer`'s 7,500, or the class definition and the class anchor stay in conflict whatever the
members do.

---

## §11 — Three ruled exceptions, and one that cannot be automated (2026-09-03)

### `asianalliance_pulverizermecha` — a RAMPING weapon, not a weak one

*"uses the gatling speedup behavior so it deals more damage the more it fires and together with the
heavy pulverizer upgrade it becomes one of the most powerful weapons in the game if not outright
broken."*

⛔ **This is a measurement gap in the whole stat layer, not a fact about one mecha.** Every damage
figure the reference pipeline reads — Cameo's and every peer's — is **base damage**. For a weapon
that ramps with sustained fire, base damage understates the unit, and §9.1 flagged the Pulverizer as
"below the MBT median" on exactly that basis. It is not weak; it is measured wrong.

⚠ **The size of the gap is currently unknowable and must not be guessed.** `GrantConditionOnAttack`,
`FirepowerMultiplier` and `ReloadDelayMultiplier` each resolve onto ~1,860 of ~2,000 actors —
barrels, ammo boxes and civilians included — because they are inherited from shared defaults. Their
presence proves nothing, so the ramping units cannot be counted by trait presence. A real detector
has to follow the granted CONDITION through to a firepower change or a weapon swap.
**Until that exists, no damage target should be trusted for a ramping weapon.**

### `schwarzermond_neojagdpanzer` — the hover chassis, a ruled faction exception

*"The lunar neo cymek is turreted indeed but it uses an invisible chassis so the whole unit is the
turret which makes it look like it can drift like the hovercraft / anti gravity unit it is, so the
rule here still holds true ... we can make it a ruled exception for the Schwarzer Mond faction since
all their units hover and it's their specialty."*

**Ruled:** a Schwarzer Mond actor whose `Turreted` is the whole chassis counts as **frontal-facing**
for class purposes. ⚠ Measured: only **15 of 61** Schwarzer Mond actors (25%) carry a `Turreted`
trait at all, so the exception applies to those 15 and is not a blanket faction rule.

### ⛔ `ixian_neocymek` — the distinction is NOT in the data

*"the main weapon is the dual railgun and it is frontal facing while the support weapon in the back
is turreted."*

Real in game, and **not machine-readable here.** The actor carries one unnamed `Turreted` and two
armaments — `Armament@Cannon` (D2K_StormGunCymek) and `Armament@Rockets` — and **neither declares a
`Turret:` link**, so both bind to the single default turret. Across 1,044 armaments scanned only
**73 (7%)** declare an explicit `Turret:`; the rest bind by OpenRA's default.

**So "which weapon is frontal" is only readable for 7% of armaments.** A mixed-mount unit needs an
explicit hand tag — the turret-presence flag alone will keep misclassifying it, and no amount of
yaml reading will fix that.
