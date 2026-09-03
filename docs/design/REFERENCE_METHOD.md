# The reference method — 10 relative values, geometric mean, normalized to Cameo

**Maintainer session 2026-09-03.** This is the METHOD document: what the reference corpus is, how a
Cameo target is derived from it, and which parts are ruled versus still open.

> ⛔ **This supersedes `BALANCE_SYNTHESIS.md` §5 steps 1 and 3** (the ÷ basic-rifleman transfer key
> and the "rifle anchor = 20000 HP" mapping). Those describe a method retired on 2026-08-30. Any
> document still teaching them is stale — see §7.

**Companions:** [`REFERENCE_DEDUP.md`](REFERENCE_DEDUP.md) (step 1, one roster one vote) ·
[`BALANCE_SYNTHESIS.md`](BALANCE_SYNTHESIS.md) (the source library and the faction map) ·
`docs/balance/REFERENCE_SYNTHESIS_REPORT.md` (the generated output).

---

## §0 — Why the rifle had to go, in the maintainer's words

> *"What if that game doesn't have any infantry and only uses vehicles?"* — 2026-08-30

Anchoring every comparison on one nominated actor has four failure modes and the corpus hits all
four: a source with no infantry has no anchor; "basic rifleman" is a different design object in
each game (a 40 HP Marine, a 12,500 HP Light Infantry, a 125 HP Conscript); one odd anchor silently
rescales everything measured against it; and it answers *"how many riflemen is this worth"*, which
is not a question anyone balances by.

**The replacement is POSITION IN DISTRIBUTION.** A unit is described by where it sits inside its own
source's spread. That description is dimensionless, so it transfers to Cameo without the two games
ever needing to share a scale.

---

## §1 — The ten relative values

For each **source**, each **stat**, and each of **two populations** — the unit's own TYPE
(infantry / vehicle / aircraft / ship / defense) and the OVERALL combat roster — compute five
aggregates and place the unit against them:

| coordinate | meaning |
|---|---|
| `r_med` | x ÷ the population's **median** |
| `r_am` | x ÷ the population's **arithmetic mean** |
| `r_gm` | x ÷ the population's **geometric mean** |
| `r_p05` | x ÷ the population's **low end** |
| `r_p95` | x ÷ the population's **high end** |

**5 coordinates × 2 populations = the 10 relative values.** They say how the unit performs relative
to its own type *and* relative to everything in that game — which is exactly the pair of readings
the class system needs, because a unit can be a heavy infantryman and a light actor overall.

⛔ **THE LOW AND HIGH ENDS ARE THE 5th AND 95th PERCENTILE, NOT THE RAW MIN AND MAX — and that is
measured, not preferred.** Both raw extremes are single actors, so both are hostage to one oddity:
Romanov's Vengeance lists a 100 HP vehicle; a roster's minimum damage is usually a dummy weapon and
its maximum a superweapon. Measured across all 302 matched actors, three variants:

| stat | variant | calibration (target/now, HIGH conf.) | within 2× |
|---|---|--:|--:|
| `hp` | raw min/max | 1.25 | 70% |
| `hp` | **p05/p95** | **1.22** | **70%** |
| `w_damage` | raw min/max | **0.30** ⛔ | **19%** ⛔ |
| `w_damage` | **p05/p95** | **1.08** | **65%** |
| `w_range` | raw min/max | **2.12** ⛔ | **39%** ⛔ |
| `w_range` | **p05/p95** | **1.05** | **92%** |

⭐ **On HP and Speed the choice barely matters** (raw min/max moves HP by a median 1.02×, and not one
unit by more than 2×) — the epic/`BuildLimit` exclusion already removed that distortion. **On the
weapon stats raw extremes destroy the model**: damage targets land 3.3× too low, range 2× too high,
and 85% of damage targets move by more than 2×. The percentile ends preserve the *"where in the
spread does this sit"* reading while denying any single prop or superweapon the power to define it.

⚠ `d_min` and `d_max` — ratios to the raw extremes — are still COMPUTED and kept as **diagnostics**.
When they disagree wildly with the middle three, that source's floor or ceiling is junk. They do
not vote.

**Stats carried:** `hp`, `speed`, `turn_speed`, `turn_ratio`, `w_range`, `w_damage`, `w_burst`,
`w_reload`, `w_dps`, and armor-aware `dps_vs_{INF,VEH,AIR,BLD}`. The four the maintainer named —
HP, Speed, Damage, Range — are all present.

**Population rule** (maintainer 2026-08-30): a row enters a distribution only if it is **buildable
and unlimited**. Not-buildable actors never reach a player; `BuildLimit` rows are one-offs; epics
and heroes are balanced separately. Buildings are excluded from `overall` — they are not mobile
combat units and they outnumber everything else in most rosters.

---

## §2 — The pipeline, in order

1. **De-duplicate the sources.** One roster, one vote — [`REFERENCE_DEDUP.md`](REFERENCE_DEDUP.md).
   Before the mean, always: the geometric mean has no defence against a roster that votes five times.
2. **Build each source's distributions** (§1), per stat, per population.
3. **Match** each Cameo actor to reference units — by name, then by role and design analogy.
4. **Place** each match → its 10 relative values.
5. **Pool across sources with the GEOMETRIC mean**, per coordinate. These are ratios: a source 2×
   high and one 2× low must cancel to exactly 1.0, and only the geometric mean does that (the
   arithmetic mean returns 1.25 and biases every target upward). It is also the only mean under
   which *normalize-then-average* and *average-then-normalize* agree.
   ⛔ **Raw stats are NEVER averaged across sources.** 125 HP and 12,500 HP are one design intent at
   two scales; their mean belongs to no game. Only dimensionless coordinates are pooled.
6. **Normalize to Cameo.** Multiply each pooled coordinate by **Cameo's own** matching aggregate →
   one candidate absolute per coordinate → the target is the geometric mean of those candidates. A
   unit sitting at 2.2× its source's vehicle median lands at 2.2× *Cameo's* vehicle median. **This
   is what makes Cameo's larger numbers automatic rather than hand-scaled.**
7. **Set the class anchor at the 100% mark** of the 100–250% target band, from a member that has a
   grounded target.
8. **Fill the rest of the class from the anchor via the formula** — see §5.

---

## §3 — Cameo's own value votes, capped at one third

**Maintainer ruling 2026-09-03:** *"Yes it votes but make sure there are always at least 2 reference
actors so the cameo stats are always 33% weighting or less!"*

So Cameo's current stat is **one vote among at least three**: a target may only be computed when
**≥2 independent reference sources** matched. A unit with one reference voice gets **no target** —
it is not synthesized at all, rather than synthesized from a single opinion plus itself.

⚠ This is a **change from both existing layers**. The distribution layer pools peers only and never
lets Cameo vote; the retired rifle layer lets Cameo vote with no minimum-source floor, which is how
LOW-confidence single-source rows were produced. Neither implements the ruling as stated.

⚠ **It also shrinks the corpus.** Today 302 actors carry a signature and only **161** reach ≥3
sources on HP; the ≥2-reference floor is what the ruling requires, and the count of units that
clear it must be reported every run rather than assumed.

---

## §4 — ⛔ Source routing and the duplication checks (measured 2026-09-03)

**Maintainer ruling:** *"make sure you don't duplicate stats from similar sources... better check
everything!"* Every pair below is measured by `tools/balance/lineage_dedup.py`; `w10` is the share
of shared units agreeing within 10% after the pair's median scale offset is divided out.

### The four the maintainer asked about

| check | result | verdict |
|---|--:|---|
| **CnC Reloaded ~ Romanov's Vengeance** | 49 shared, offset 1.00×, **47%** | ⭐ **NOT duplicates — both vote** |
| **Mental Omega ~ anything** | best match is 33% (OpenRA RA2); vs CnCR only **17%** | ⭐ **unique, as suspected — its own vote** |
| **Valiant Shades** | 79 / 76 / 73 / 72 / 67% against five RA2 sources, all at offset **1.92×** | ⚠ an RA2-lineage mod at ~1.92× scale; every pair below the 85% cut, so it votes — but it is the corpus's closest near-miss |
| **Crystallized Nexus** | **67%** vs OpenRA TS at offset 1.00; only 20% vs Shattered Paradise | ⭐ **TS-themed, confirmed** — and an independent rebalance, so it votes |

⭐ **Every one of the maintainer's instincts checked out**: MO is different enough to justify a
unique vote, CnCR and RV are not the same data, and Crystallized Nexus is the other TS-themed mod.

### The rest of the routing checks

| pair | n | offset | `w10` | verdict |
|---|--:|--:|--:|---|
| Combined Arms ~ OpenRA Red Alert | 76 | 1.00× | 63% | independent |
| Combined Arms ~ OpenRA Tiberian Dawn | 33 | 1.00× | 36% | independent |
| Shattered Paradise ~ OpenRA Tiberian Sun | 46 | 1.00× | 35% | independent |
| Romanov's Vengeance ~ Combined Arms | 64 | 0.40× | 20% | independent |
| Valiant Shades ~ Romanov's Vengeance | 89 | 1.92× | 42% | independent |

**Already collapsed** (`REFERENCE_DEDUP.md`): the five RA2-family copies into Romanov's Vengeance,
and `Tiberian Sun` into `OpenRA Tiberian Sun` (96%). So the maintainer's *"all the Yuri's Revenge
and RA2 implementations might also be the same"* is **confirmed and already applied** — RA2 vanilla,
Yuri's Revenge, RA2/YR (raw INI), OpenRA RA2 official and YR-on-OpenRA no longer vote separately.

### ⚠ OPEN — routing vs the cross-reference principle

The 2026-09-03 ruling names per-family source sets (RA2 → CnCR + RV + MO + CA; TD/RA1 → DTA +
OpenRA TD/RA1 + CA; TS → mostly Shattered Paradise + Crystallized Nexus). The 2026-07-25 ruling in
`BALANCE_SYNTHESIS.md` §2 says the opposite: *"synthesize from ALL source material... never restrict
a mod to only its 'primary' factions."*

⛔ **These cannot both be law and the maintainer has to pick.** Reading the newer one as a
*de-duplication* instruction rather than a routing rule reconciles them — but the measurements above
show the named sources are NOT duplicates of each other, so de-duplication does not by itself
produce those per-family sets. **Unresolved; nothing is routed until it is ruled.**

---

## §5 — ⛔ The hard part: the reference has TYPES, Cameo has CLASSES

**Maintainer, 2026-09-03:** *"we can only relatively easily and accurately check for unit types but
not classes since the reference data doesn't have any classes like we have so there could be a lot
of inferred and invented data that might be wrong."*

**Correct, and measured.** Reference coverage of the 660 classed Cameo units:

| | |
|---|--:|
| units with a class | 660 |
| **carrying a reference signature** | **205 (31%)** |
| classes with **zero** coverage | **6** — `commando`, `epic_vehicle`, `dreadnought`, `closecombat`, `archer` (+`heavy_sniper` at 1) |
| `scout` | 11 of 30 (37%) |

Best covered: `mortar` 80%, `heavy_sniper` 50%, `support` 47%, `high_tech_tank` 46%.

⚠ And a prior measurement forbids the obvious shortcut: median distance from a unit to its **own**
class anchor is **2.94**, while median distance **between** anchors is **1.21**. Units sit further
from their own anchor than the anchors sit from each other, so **class boundaries are not
recoverable from stats.** Any attempt to infer a class from reference numbers will be wrong.

### THE RULING (maintainer 2026-09-03): a grounded anchor, then the formula

**Never synthesize a class target.**

1. Synthesize **per-unit** targets only for units with ≥2 reference sources.
2. Choose each class's **anchor from among those grounded members**, placed at the 100% mark.
3. Derive **every unmatched member from the anchor via the formula** — not from invented reference
   data.

⭐ **This keeps invented data at exactly zero.** A unit either has a reference voice, or it is priced
by the formula from a grounded anchor. Nothing is interpolated from a class-level profile that the
data does not support.

⛔ **The cost, stated plainly:** the **6 zero-coverage classes have no grounded anchor candidate at
all**, and must be resolved by better matching or a hand ruling. They cannot be synthesized.

---

## §6 — ⛔ What is missing from the corpus, and what it blocks

Only **13 of 26** source labels are in the distribution layer. The other 13 are hand-typed markdown
tables, and the split is not arbitrary — the 13 are exactly the **OpenRA clones on disk**, resolvable
by the resolver. Mental Omega, CnC Reloaded and DTA are Westwood/Ares INI mods; no checkout exists.

| group | rows | has a type column? | can do the OVERALL 5 | can do the TYPE 5 |
|---|--:|---|---|---|
| the 13 OpenRA clones | 2,256 | ✅ `type` | ✅ | ✅ |
| **Mental Omega + CnC Reloaded** (+RA2v, YR) | 1,021 | ✅ `kind` | ✅ | ✅ — **ruled IN, not yet wired** |
| StarCraft, Warcraft 2, RA1, TD, TS, RA2/YR raw | 188 | ❌ | ✅ | ❌ needs a type column |
| **DTA** | **0** | ❌ | ❌ | ❌ |

**Ruled 2026-09-03:** wire Mental Omega and CnC Reloaded into the distribution layer now; the
Westwood originals wait for a type column.

### ⛔ DTA is registered, contributes zero rows, and blocks the TD/RA1 routing

`DOC4_SOURCES` registers `Dawn of the Tiberium Age`, and it votes on nothing. Two separate faults:

1. **Parser:** its table is `| Unit | Classic | Enhanced | DTA's intent |` — no `HP` column, so
   `parse_doc4` skips every row. It reads as present while voting on nothing.
2. **Data, and this one cannot be parsed around:** that section is **15 hand-picked highlight rows**,
   not a roster. A source's median cannot be computed from 15 cherry-picked units, so DTA could not
   enter the distribution layer even with the column fixed.

⛔ **The 2026-09-03 routing ruling requires DTA for the TD and RA1 factions, so that half of the
ruling is BLOCKED** until `INI/Rules.ini` + `INI/Enhance.ini` reach the repository (HP ÷10 for the
TS engine). Searched and confirmed absent from the working environment: no `~/Downloads`, no
`*_units.csv`, no DTA INI anywhere on disk.

---

## §7 — Documents still teaching the retired rifle method

⛔ **`DESIGN.md` — the binding contract — describes NEITHER method.** That is the root cause of the
drift: with no binding statement, nothing can be checked against it, and a retired method survived
in seven documents and one live tool. **Fixing that is step 0.**

| document | what is stale |
|---|---|
| `BALANCE_SYNTHESIS.md` §5 | steps 1 and 3 teach ÷rifleman and "rifle anchor = 20000 HP" |
| `SYNTHESIS_DELTA.md` | auto-generated by the rifle tool; its "How each target is reached" is the retired method |
| `REFERENCE_SYNTHESIS_REPORT.md` | says *"five ratios plus `p_rng`"* — six per population; the code votes four |
| `BALANCE_PIPELINE_ESTIMATE.md`, `ORIGINAL_UNITS_*.md`, `HANDOFF.md` | rifle-relative framing |
| `tools/balance/synthesize_reference.py` | **still produces the per-unit HP/cost targets**, by the retired method |

⚠ **`lineage_dedup.py` scores duplication on `×rifle` HP** — the only coordinate all three documents
share. Once this method is law, the de-duplication test should be re-scored on `r_med`/`r_gm`.

---

## §8 — Reproduce

```sh
python tools/balance/reference_distribution.py         # the 10 relative values + targets
python tools/balance/lineage_dedup.py                  # source duplication, every pair
python tools/balance/lineage_dedup.py --pair "CnC Reloaded" "Romanov's Vengeance"
python tools/balance/anchor_readiness.py               # per-class anchor integrity
```
