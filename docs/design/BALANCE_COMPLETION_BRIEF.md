# Balance pipeline — the completion brief

**What this is.** The ordered gate list that takes the balance programme from where it verifiably
is to a first production pricing run, plus the session contract an agent works under while doing
it. It is a ROUTING document: it restates no law, and every rule below points at its owner.

⚠ **Non-authoritative over any owning document.** `DESIGN.md` wins on law, `BALANCE_PIPELINE.md`
on machinery, `BALANCE_PROGRAM_PLAN.md` §0a on order, `anchor_decisions_log.md` on anchors. If
this file disagrees with one of them, that one wins and this file gets fixed.

---

## 0. Verified state — measured 2026-08-30, not remembered

Every number below has its command. Re-run before trusting any of it.

| | measured | command |
|---|--:|---|
| branch vs master | **58 ahead, 0 behind** | `git rev-list --count origin/master..HEAD` |
| ledger drift | **0** of 32 | `audit_balance_drift.py` |
| generator sync | **drift 0** | `verify_generator_sync.py` |
| §9.4 row spread | **3.63×** median, 46 in band, floor +2.0%, ceiling 29% headroom | `audit_versus_profile.py` |
| macro contrast | **1.67×**, 24% in band | same |
| `tools/tests/` | **714 tests, 1 failure** (`test_ledger_split`, pre-existing) | `python -m unittest discover -s tools/tests -t tools/tests` |
| doc claims | **5 drifted**, deliberately | `audit_doc_claims.py` |
| 3-way-split debt | **667 violating weapons** (ratchet 667) | `audit_three_way_split.py` |
| classes defined | **27** · signed **8** | `anchor_readiness.py` |
| **buildable units tagged with a class** | **336 of 1870 — 18.0%** | same |
| **classes ready to sign** | **3** · 23 need review · 1 unfitted | same |

---

## 1. ⛔ The two P0 findings that reorder everything

### 1a. The sign-off queue disagrees with what was signed

`anchor_readiness.py` ranks by **PRICING error** — how far the class formula's price sits from the
unit's actual cost. Against that metric, the eight signed classes read:

| signed class | scored members | median \|Δ\| | worst | the tool's verdict |
|---|--:|--:|--:|---|
| `flying_infantry` | **1** | 0% | 0% | ⚠ too few to judge |
| `grenadier` | **1** | 0% | 0% | ⚠ too few to judge |
| `mortar` | **1** | 6% | 6% | ⚠ too few to judge |
| `closecombat` | 4 | 12% | 74% | ⚠ review the outliers first |
| `archer` | 4 | 14% | 82% | ⚠ review the outliers first |
| `heavy_sniper` | 3 | 22% | 334% | ⚠ review the outliers first |
| `missile_vehicle` | 13 | **30%** | 373% | ⛔ **the anchor does not describe its members** |
| `special_forces` | 15 | **57%** | 523% | ⛔ **the anchor does not describe its members** |

And the three the tool says are **ready to sign today** — `dreadnought` (2%), `scout` (4%),
`heavy_infantry` (7%) — are exactly the three an agent self-signed on 2026-08-29 and which were
correctly reverted on 2026-08-30. **None of them is signed.**

⚠ **This is the two-metric confusion for the third time this session.** The eight were signed
against a ≤1 bar quoted in ROADMAP (`closecombat` 0.1, `mortar` 0.1, `archer` 0.2 …). Those are
NOT the percentages above — a different measure entirely. Neither is wrong; quoting one while
meaning the other is. ⛔ **The lesson generalises: an anchor prices ITSELF at 0% by construction,
so a one-member class showing 0% error is not evidence of anything.**

**What this does NOT mean.** The sign-off was maintainer-ordered and is not being second-guessed
here. What it means is that `apply_balance --confirm` on the current signed set would price
`special_forces` and `missile_vehicle` through anchors their own readiness tool flags — so the
queue needs re-reading before the apply, not before the flip.

### 1b. 82% of the roster has no class at all

**336 of 1870 buildable actors are class-tagged.** The formula cannot price an actor with no
class, and it must never guess one from numeric similarity — `anchor_readiness` itself lists
eight class pairs that are *statistically indistinguishable* (`anti_air_vehicle` ↔
`missile_vehicle` at 0.024, `archer` ↔ `flying_infantry` at 0.048 …) and are separated only by
**what they shoot at**. Coverage is a classification job, not a fitting job.

### 1c. The anchors, reviewed — half are engineered, half are placeholders

⭐ **"The anchor actor does not match its ruled spec" does NOT mean the anchor is wrong.** It means
the restat was **designed, locked, and never applied**. `class_anchors.json`'s `mbt` entry says so
in its own comment: *"NEW 2026-07-31 restat (**was legacy Tiger 100k/100/200/5000**)"* — and the
live `tiger.nax` still reads hp 100000, speed 100. The `spec` block is a TARGET the baseline actor
is meant to be restatted TO; the LOCKED table itself says *"HP/Speed/Cost/armor restat can proceed
now"*. Measured: **19 of 27 anchor actors differ from their spec, 8 match, 0 are missing.**

#### The vehicle ladder is genuinely engineered — verified, not quoted

The 2026-08-01 LOCKED table claims four properties. Recomputed from `class_anchors.json`:

| claim | measured |
|---|---|
| HP in clean 10,000 steps | ✅ all 13 |
| Cost / HP / Speed / DPS / Range all unique | ✅ all five |
| A+B spread ≤ 2.0× | ✅ **1.922×** |
| DPS/Cost within 0.5–1.5, epic the sole 2.0 | ✅ 0.50–2.00 |

That is a real ladder: 13 classes ordered by A+B, every stat deliberate. **The vehicle anchors make
sense.**

#### The other 14 do not have a ladder at all

| provenance recorded in `provisional` | classes |
|---|---|
| `2026-08-01 LOCKED table` | **13** — the vehicle ladder |
| `dps0/cost0 placeholders — test in-game` | `archer` `grenadier` `melee` `rocket_trooper` |
| **`— none —`** | `closecombat` `scout` `special_forces` `support` |
| one-off notes (weights frozen, verifier blocked, "derived from the RA2 sniper") | 6 |

The decisions log carries **no infantry ladder**. Its only infantry-flavoured entry is the
scout-vehicle HP-granularity rule. So every infantry class's anchor is a placeholder or has no
recorded provenance.

#### ⛔ And that is where the sign-offs went

**Seven of the eight signed classes sit in the un-engineered half** — `archer`, `closecombat`,
`flying_infantry`, `grenadier`, `heavy_sniper`, `mortar`, `special_forces`. Three of those
(`closecombat`, `special_forces`, plus `scout` which is unsigned) carry **no provenance at all**.
Only `missile_vehicle` comes from the LOCKED table — and it is the one `anchor_readiness` flags
⛔ at 30% median pricing error.

This is not an argument that the sign-off was wrong. It is the observation that **the engineered
half is unsigned and the placeholder half is signed**, which is the reverse of what the evidence
supports, and it explains `special_forces` reading 57%.

#### ⛔⛔ AND THE INFANTRY CLASSES DO NOT HAVE POPULATIONS TO FIT

Before drafting any ladder I measured whether each anchor is REPRESENTATIVE of the class it
anchors. It is worse than "placeholders", and it changes what the next job is.

| finding | measured |
|---|--:|
| anchors **tagged into the class they anchor** | **17 of 27** |
| anchors carrying **no class tag at all** | **10** — `commando` `flying_infantry` `grenadier` `heavy_infantry` `heavy_sniper` `melee` `mortar` `pure_sniper` `rocket_trooper` `scout` |
| classes with **ZERO tagged members** | **5** — `commando` `flying_infantry` `grenadier` `mortar` `pure_sniper` |
| of those five, **signed** | **3** — `flying_infantry` `grenadier` `mortar` |

And where a class does have members, the anchor is often not near its centre:

| class | members | anchor's percentile within its own class |
|---|--:|---|
| `special_forces` | 15 | **13th** ← SIGNED |
| `closecombat` | 4 | 25th ← SIGNED |
| `support` | 34 | 44th |
| `archer` | 4 | 50th ← SIGNED |

⚠ **One caveat before anyone re-anchors a vehicle class.** `anchor_readiness --` now reports 12
off-centre anchors, but for the 13 LOCKED-table classes the actor is still PRE-RESTAT, so its
percentile is measured on stats the design already intends to replace (`scout_vehicle`'s buggy
reads 7th at hp 20000 against a spec of 30000). Those are a SYMPTOM of §1c's unapplied restat —
apply it, then re-read the list. The infantry entries are not explained that way, because no
restat is queued for them.

⭐ **`special_forces` sitting at the 13th percentile of its own 15 members IS the 57% pricing
error.** The zero point is an outlier at the bottom of the population it defines, so every member
is measured against a ruler planted in the wrong place. That is a mechanical explanation, not a
coincidence — and it is fixable by re-selecting the anchor, without touching the formula.

#### ⛔ MUST THE ANCHOR BE AT THE CENTRE OF THE BAND? NO — AND THAT WAS ALREADY RULED

The question came up as *"should the baseline be at 100% Cost while the band goes from 50% to
400%?"*, and grepping first (RULE 8f) found it decided and shipped. `tools/balance/check_band.py`
enforces `BALANCE_PIPELINE` §8.1:

```python
FLOOR, SOFT_FLOOR, SWEET_LO, SWEET_HI, CEIL = 0.50, 0.75, 1.00, 2.50, 4.00
```

| ring | multiple of `cost0` | meaning |
|---|--:|---|
| hard band | **0.50× – 3.50×** | = ×0.50 – ×2.50 stats; outside this the unit is not in the class |
| **target band** | **0.75× – 2.50×** | where **≥80%** of members must land |
| the anchor | **1.00×** | ×1.00 stats — the class's zero point |

⛔ **The rings are COST numbers** — ruled directly: *"The 75% referred to the unit price not the
stats"*. Cost is what a player reads off the build palette; the stat window is the derived reading,
and it comes out exact at four of the five rings. The anchor at 1.00 sits at the **lower quartile**
of the target band, not its centre: the class extends **upward** from its zero point because the
anchor is the entry unit.

⭐ **And that is the right shape, not an accident.** The anchor is the class's recognisable ENTRY
unit — the plain rifleman, the plain light tank — and everything else in the class is that unit
with more of something, which the formula `Cost = cost0 · (O/O0 + P/P0 + Q/Q0)/3` prices as *more
than 1.0×*. Centre-anchoring would put half of every class BELOW its own baseline, which would
mean the zero point is not the entry unit. The band is asymmetric because the design is.

#### The candidate anchors, ranked by the ruled criterion — `--propose-anchors`

⚠ **The first version of this mode ranked by CENTRALITY and was wrong.** Centrality is a proxy I
invented; the law above is the actual criterion. The mode now scores each member by *if THIS actor
were the anchor, what share of the class lands in 1.00×–2.50×?*, importing `check_band`'s pricing
so there is one implementation of §8.1. The rankings changed completely — for `special_forces` the
centrality top-3 and the occupancy top-3 share **no** members.

⛔ Still evidence for a ruling, never an assignment: an anchor must also be ROLE-typical, and no
stat can see role.

#### ⛔⛔ AND THE ANSWER IT PRODUCED IS BIGGER THAN THE ANCHORS

Members are priced as RATIOS to the anchor, so **moving the anchor SLIDES a class along the band —
it can never NARROW it** (pinned by `test_a_class_spread_does_not_depend_on_which_member_anchors_it`).
The target band is `2.50 / 0.75` = **3.33× wide**. A class whose own priced spread exceeds that
cannot reach the ruled ≥80% occupancy from *any* member, and the shortfall is arithmetic, not tuning.

⛔ **BUT DO NOT READ THE RAW SPREAD.** `artillery` measures **324.5×** raw and **5.9×** on P10..P90,
because ONE member carries the entire number. Judging the class on the raw figure would condemn a
class that is within striking distance of the band. `tools/balance/band_granularity.py` reports both
and leads with the trimmed one.

| class | n | factions | raw | **P10..P90** | fits target 3.33×? |
|---|--:|--:|--:|--:|:-:|
| `mbt` | 42 | 22 | 22.9× | **6.1×** | no |
| `line_breaker` | 30 | 15 | 24.7× | **4.2×** | no |
| `fire_support` | 27 | 16 | 8.6× | **6.0×** | no |
| `scout_vehicle` | 27 | 13 | 18.8× | **11.1×** | no |
| `artillery` | 26 | 15 | **324.5×** | **5.9×** | no |
| `high_tech_tank` | 25 | 15 | 7.9× | **4.4×** | no |
| `support` | 11 | 8 | 15.8× | **10.1×** | no |
| `light_tank` | 16 | 14 | 9.5× | **6.0×** | no |
| `special_forces` | 15 | 10 | 12.0× | **5.8×** | no |
| `missile_vehicle` | 13 | 9 | 9.1× | **6.6×** | no |
| `artillery_tank` | 12 | 9 | 12.0× | **8.3×** | no |
| `anti_air_vehicle` | 12 | 10 | 7.0× | **3.9×** | no |
| `scout` | 6 | 4 | 2.5× | **2.5×** | **YES** |
| `dreadnought` | 5 | 5 | 4.0× | **4.0×** | no |
| `tank_destroyer` | 5 | 4 | 1.9× | **1.9×** | **YES** |
| `closecombat` | 4 | 4 | 2.9× | **2.9×** | **YES** |
| `archer` | 4 | 3 | 1.8× | **1.8×** | **YES** |

⭐ **14 of 17 already fit the HARD band (7.0×); only 4 fit the target band (3.33×).** That gap is
the work-sorting rule: inside the hard band is a REPRICING job, outside it (`scout_vehicle` 11.1×,
`support` 10.1×, `artillery_tank` 8.3×) is a SCOPE question. The honest gap on the rest is
1.1×–3.2×, not one to two orders of magnitude.** That is a tractable repricing job — which is what the pipeline exists to do — rather
than the structural collapse the raw numbers implied. ⚠ An earlier revision of this section reported
the raw spreads as the verdict; that was wrong and this replaces it.

#### ⛔ EIGHT MEMBERS HAVE NEGATIVE DPS — no repricing can fix that

`band_granularity.py` flags them: `tkm_battlebus` (−600), `cabal_engineer` (−650),
`futuretech_repairdroid` (−508), `tkm_engineer` (−397), `ra1_allies_mechanic` (−357),
`terran_medic` (−183), `ra1_allies_medic` and `ts_gdi_medic` (−40). A **heal/repair armament is
being summed as damage** by `formula.spread_damage_sum`, so every one of them prices as if it
shot backwards. Fix the extractor, not the units.

#### The outlier triage queue — 33 members, and the shape of it is informative

| what | members | reading |
|---|---|---|
| `futuretech_athenacannon` | DPS **193,600** — 24× the next artillery | ⛔ a stat error, not a heavy unit. It alone is `artillery`'s 324× |
| the RA2 **IFV family** (7 actors) | all at DPS 19,211.7, 5.9–7.9× the `scout_vehicle` median | a TRANSFORMING unit. `RTS_BALANCE_REFERENCE` §7 already rules these need their own model; pricing one variant's weapon onto the chassis is the error |
| `steelconsortium_megalodon`, `latinsyndicate_smokertank`, `ra1_soviets_siegemammothtank`, … | 3.3×–5.8× their class median | genuinely heavy — these are repricing or reclassification, not bugs |

So the 33 outliers are **one stat error, one modelling gap, and ~25 real repricings**. Only the
third group is pipeline work.

#### Granularity — the band is not the constraint, and that is measured

At the shipped-mod cost resolution of **1.143×** (median of 266 adjacent-cost gaps across 14 peer
mods, `tools/reference/peer_cost_grid.py`) the 3.33× target band holds **9.0 distinct rungs** and
the 7.0× hard band **14.6**.
`mbt` has 42 members from **22 factions** — 4.6 per rung, filled from different factions, against
Combined Arms' observed **4.67 units per distinct cost** at 215 armed units. **The band comfortably
holds every class Cameo has.**

⛔ **The price grid: 20 is the right ATOM and the wrong STEP** (`tools/balance/cost_grid.py`).
Prices run 10–10,000 (a **1000× range**, median **1,200**) and **89% are already multiples of 20** —
so a flat-20 snap changes almost nothing, because the over-precision is in the SPACING, not the last
digit. A flat 20 is one perceptible notch only near **140 credits**, and 6% of the roster is at or
below 200; at the median it is **1.7%**. Keep the atom and derive the step:
`step(price) = max(20, 20 × round(0.143 × price / 20))` — 20 at 140, **160 at the median**, 700 at
5,000. **105 distinct prices → 55**, median step 1.041× → 1.078×, 92% of units move by a median 2.0%.

#### The two free wins, offered for a ruling

Two classes are already narrow enough for the law, and one is mis-anchored:

* **`tank_destroyer`** — `naxis_hetzer` (60%) → **`ra1_allies_alliedtankdestroyer` (100%)**. The
  class fits in 1.9×; the only reason it misses the target is the pick.
* **`scout`** — the signed-off anchor `naxis_naxiriflesoldier` **is not a priced member of its own
  class**, so it scores nothing. **`ra1_allies_rifleinfantry` reaches 83%**, over the ≥80% target,
  and is the archetypal scout-tier rifleman on the role axis too.

Both are maintainer calls. Neither is applied.

⚠ For the 13 LOCKED-table classes the actor is still PRE-RESTAT, so these numbers are measured on
stats §1c already intends to replace. Apply the restat, then re-read. The infantry rows are not
explained that way — no restat is queued for them.

⛔ **THE LADDER IS NOT THE NEXT JOB.** You cannot engineer a ladder for five classes that have no
members and ten anchors that are not in their own class. The vehicle ladder worked because those
classes have real populations — `mbt` 40 members, `scout_vehicle` 27, `high_tech_tank` 25 — so the
table had something to describe. **Classification comes first; the ladder is derived from the
populations, not invented ahead of them.**

⚠ **And it reframes the "8 signed" number.** Three of the eight price ZERO units, so signing them
changed nothing in effect — but it also means readiness was never 8. Of the eight, five have any
members at all, and two of those five are the ⛔ ones.

#### Three DIFFERENT problems hide behind one phrase

Sorting the 19 mismatches by kind, because they need different fixes:

| kind | examples | what to do |
|---|---|---|
| **unapplied restat** — the spec is right, the actor is pre-restat | `line_breaker` hp 100k→750k (7.5×), `dreadnought` 300k→1.15M, `high_tech_tank` 225k→700k, `epic_vehicle` 1M→4M, `fire_support` 30k→120k | apply through the PIPELINE (ledger → `apply_balance --confirm`), boot-gated. Never a hand edit. |
| **near-miss** — rounding or a grid step | `archer` speed 72≠70, `heavy_sniper` 78≠80, `scout_vehicle` hp 20k≠30k | trivial; fold into the same apply |
| ⚠ **suspected SPEC bug** — the spec looks wrong, not the actor | `flying_infantry` speed **80** vs the rocketeer's **180**. Its note says *"speed0 from air-speed"*, but measured over 168 buildable aircraft the median is **150** and only 36 fly at ≤80. A rocketeer at 80 would be in the slow tail, contradicting the role. | **maintainer ruling** — do not restat the actor to a spec that may itself be wrong |

⛔ **Reconcile spec against actor BEFORE fitting**, per `anchor_decisions_log.md`. Fitting around a
mismatch freezes a wrong zero point into every price in that class.

---

## 2. The gates, in order

Each gate lists what must be TRUE, not what must be run. A gate is closed when its check passes
from a complete tree.

| gate | closed when | state |
|---|---|--:|
| **G0 — tree truth** | fetched, 0 behind, drift 0, generator sync 0 | ✅ |
| **G1 — one shape regeneration** | bell + `MACRO_RATIO` live in `weapons.yaml`, boot-verified once, constants persisted only after the boot | ⛔ needs boot |
| **G2 — re-extract** | `extract_stats` run immediately after G1; drift 0 | ⛔ blocked by G1 |
| **G3 — structure** | W24 → W23 → A5; no priced actor carries structure debt | ⛔ 667 violating weapons |
| **G4 — coverage** | every eligible buildable-unlimited actor has a class **or** a registry exception | ⛔ 18% tagged |
| **G5 — anchors** | spec reconciled with actor; ≥3 scored members; readiness verdict not ⛔; maintainer-signed | ⛔ 3 ready, 2 signed-and-flagged |
| **G6 — apply** | `apply_balance --confirm` for split-clean signed classes → boot → extract → drift 0 | ⛔ blocked by G3/G5 |
| **G7 — suite** | `run_all.sh` green from a COMPLETE tree; intentional reds documented | ⛔ blocked |

⭐ **G1 and G2 are one boot session.** The macro axis is arithmetic-mean-neutral but NOT
weighted-mean-neutral (`WEAPON_HEAVINESS.md` §9.7a): K moves ~1.75% mean and +5.3% worst, so every
price behind the ledger is stale the moment G1 lands.

---

## 3. Sequencing corrections worth keeping

* ⭐ **Measure the shield pins AFTER the flip, not now.** `shield_versus_mean` is *"mean Versus-vs-
  Shield across all main flat warheads"* — `Shield` IS a Versus row, recomputed by `shield_for()`
  from the finished profile. Three of the five red `doc_claims` are **downstream of G1**; ruling on
  them today buys a number G1 invalidates within the hour.
* **Structure before pricing is not advice, it is causality.** W24 changes a weapon's warhead set,
  which changes K, which changes the price. Pricing an actor with structure debt prices a moving
  input.
* **Never `extract_stats` while targets are staged** — extract rebuilds the raw ledger. Extract
  AFTER apply, never before.
* **Direction of good belongs in the metric**, not in the reader's head: `multi_main_fired_weapons`
  is a burn-DOWN, `warhead_family_reach` a burn-UP.

---

## 4. Explicitly OUT of scope for pipeline v1

`EconomyProfile` · tempo · snowball/comeback · telemetry · counterplay as a pricing input · new
armor types · AIR peer data that does not exist · reaching Mental Omega's 4× macro by scalar ·
exponent-reshaping `Versus` to force macro (breaks §9.4).

`RTS_BALANCE_REFERENCE.md` is **non-binding and authorises no work**. Its own measured split:
the fight layer is well modelled, the match layer is not, and that is the correct order given §0a.

---

## 5. The session contract

Paste this to open a completion session. It restates no law — it points at owners and forbids the
specific failures this programme has actually paid for.

```text
ROLE — Cameo balance-systems engineer, in the live cameo-mod/Cameo-mod repository.
You implement an existing balance compiler. You do not design a new one.

PRIME DIRECTIVE — DON'T TRUST, VERIFY.
The artifact outranks every summary, including mine and yours. When a document and the tree
disagree, the tree wins and you FIX the document in the same commit. A claim is not evidence.
A green test is evidence only for the invariant that test actually checks.

CONTEXT — route, do not dump. The authored corpus is ~117 files / ~92,700 lines / ~1.9M tokens
and does not fit. Tier 1 (the seven reading-order docs) gates every action and is hook-enforced.
Load Tier 2 by SUBJECT, and only the sections you need. Never load the corpus to look thorough.

HARD RULES — each one is a failure someone already paid for:
 1  BOOT-GATE engine content. No perf.log to the main menu, no commit. Produce runbooks instead.
 2  Never hand-edit a balance number. extract -> ledger -> propose -> maintainer sign -> apply.
 3  Never extract while targets are staged; extract AFTER apply.
 4  STRUCTURE BEFORE PRICING (§0a). An actor with structure debt has a moving K.
 5  Never set signed_off yourself. It needs MAINTAINER-ORDERED SIGN-OFF in the commit message.
 6  Scoped `git add <paths>`. Never -A. Several contributors have live WIP.
 7  Prior art: grep tools/ before writing a tool, and grep DESIGN.md before designing anything.
 8  Never hand-parse yaml. miniyaml.Ruleset.resolve_weapon + weapon_efficiency.versus_of.
 9  A cross-corpus number needs its FRAME in the same breath as its value.
10  A derived cell recomputes on EVERY exit path. Assert the mechanism, not a symptom.
11  Know a ratchet's DIRECTION OF GOOD before reading it. Never re-baseline a burn-down upward.
12  MEAN-100 pins the ARITHMETIC mean; K uses a WEIGHTED one. Shape changes can move prices.

METHOD — for each gate in §2 of this brief:
  verify the gate's check on the tree (command + number)
  do only what this machine can do without a boot
  when blocked, produce the exact runbook for the human and STOP
  report: what I verified / what I changed / boot status / law checks / next human command

DEFINITION OF DONE — G0..G7 in §2. Not "the scripts exist".
```

---

## 6. First three actions, ranked by leverage

⛔ **NOT the infantry ladder.** That was the obvious next move and the measurement killed it: five
classes have zero members and ten anchors are not tagged into their own class. A ladder needs
populations to describe.

1. **Tag the infantry roster** — classification, the same job as G4's 18%. It is the prerequisite
   for everything else in the anchor system: no population, no fit, no ladder, no sign-off.
   Judgement plus evidence, never numeric proximity. No boot.
2. **Re-select the anchors that sit outside or at the edge of their class** — `special_forces` at
   the 13th percentile is the 57% error, and it is fixable by moving the zero point rather than
   touching the formula. Maintainer ruling, evidence preparable now. No boot.
3. **THEN derive the infantry ladder** from the measured populations, the way the vehicle one was
   consolidated from per-class rulings rather than invented in one pass.
4. **Re-read the sign-off queue** (§1a) once 1–3 land; three of the eight currently price nothing.
5. **The single regeneration** (G1+G2) on the boot machine — independent of all the above, and it
   invalidates three of the five red pins, so it can happen whenever you have the machine.
