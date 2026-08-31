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

### 1c. The anchor ACTORS do not match their ruled specs

The anchor is the zero point of its class formula, so a wrong baseline actor freezes a wrong zero:

    mbt              tiger.nax                 hp 100000 != 240000, range 6000 != 5500, speed 100 != 95
    tank_destroyer   naxis_hetzer              hp  75000 != 150000, cost ratio 2.17x
    missile_vehicle  ts_gdi_hovermlrs          hp  30000 != 160000, speed 80 != 100     ← SIGNED
    line_breaker     td_nod_flametank          hp 100000 != 750000
    mortar           forgotten_mutantmortarman range 10830 != 10000                     ← SIGNED

Reconcile spec vs actor **before** fitting, per `anchor_decisions_log.md`. Do not fit around it.

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

1. **Re-read the sign-off queue against pricing error** (§1a) and decide: re-affirm the eight, or
   narrow to the three the tool calls ready. No boot needed. This gates G6.
2. **Class coverage** (§1b) — 18% is the ceiling on how much of the roster can ever be priced.
   Classification is judgement plus evidence, never numeric proximity. No boot needed.
3. **The single regeneration** (G1+G2) on the boot machine. Everything downstream is stale until
   it lands, including three of the five red pins.
