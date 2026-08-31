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
