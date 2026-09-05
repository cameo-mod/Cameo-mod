# BOOT-GATE RUNBOOK — for an agent on a Windows machine that can run `launch-game.cmd`

**Written by:** Claude Opus 5, 2026-08-31, from the container session that produced commits
`0b61e48fb` → `e5a390da6`. That session had **no boot gate**, which is why this document
exists: everything it could prove without booting is proven, and everything it could not is
queued here with the exact evidence needed to close it.

> ⛔ **THIS IS A RUNBOOK, NOT AN ENTRY POINT.** `docs/HANDOFF.md` is the entry point and it
> outranks this file. `CLAUDE.md` outranks both. This document owns exactly one thing: the
> boot-gated work queue and how to execute it. If it disagrees with `HANDOFF.md`,
> `BALANCE_PIPELINE.md` or `DESIGN.md`, **they win and this file gets fixed.**
>
> It is also **not** a dated handoff. Do not move it to `docs/history/handoffs/` — those are
> provenance only. This is live until its queue is empty.

---

## §0 — The one rule, restated because it is the whole reason you are reading this

**Don't trust — verify.** Every number in this document was measured by a tool that is in
this repository, and every one names the command that reproduces it. If a number here
disagrees with the tool, **the tool wins and you fix this file in the same commit.**

That is not a slogan. This session shipped a wrong number twice and caught both only by
re-running the measurement:

* the first band census reported **100% of every class above 3.50×** — absurd on its face,
  caused by pricing through a helper that normalises `cost0` to 100;
* the first `--propose-anchors` ranked candidates by **centrality**, a proxy nobody ruled,
  and its top-3 shared *no members* with the correct ranking.

Neither was caught by review. Both were caught by looking at the output and asking whether
it could possibly be true.

---

## §1 — State at a glance

| | |
|---|---|
| repo | `github.com/cameo-mod/Cameo-mod` — ⛔ `Zeruel87/Cameo-mod` is the **abandoned fork**, forbidden by `CLAUDE.md`, and two external reviewers cited it this week. Commits quoted from that URL do not exist. |
| branch | `claude/docs-audit-reorganize-xgzwhr` |
| HEAD | `e5a390da6` + §6.1 extractor fix + §6.2 quarantines (merged with master twice) |
| behind master | 0 |
| tests | **754+ pass**, 1 pre-existing failure (`test_ledger_split` — `reference_distributions.json` has no raw counterpart; predates this work) |
| `audit_doc_health` | **PASS** |
| ledger drift | **0** |
| boot gate owed by these commits | **NO** — docs + tools only, no yaml, no engine content, no balance number |
| boot gate owed by the queue in §5 | **YES, all of it** |

Re-verify before trusting any of it:

```bash
git fetch origin master && git rev-list --count HEAD..origin/master
python tools/audit/audit_doc_health.py
python tools/audit/audit_balance_drift.py
```

---

## §2 — THE BAND LAW (sealed — four points, all exact in both spaces)

Maintainer ruling, 2026-08-31, in three parts across the session:

1. *"The reason cost from 100% to 250% makes sense is because in the balance formula that is
   exactly true when a unit has 2x HP and 2x DPS."*
2. *"The 75% referred to the unit price not the stats."*
3. *"we make the 1.0x to 2.5x the regular Band for 80% of the unit population, the baseline
   actor being exactly at 1.0x ... and the extended band for the remaining 20% outlier units
   is between 0.5x and 3.5x price."*

| ring | cost | stat window (HP **and** DPS) | exact in **both**? |
|---|--:|--:|:-:|
| `FLOOR` | **0.50** | **×0.50** | ✅ |
| `SWEET_LO` **= the anchor** | **1.00** | **×1.00** | ✅ |
| `SWEET_HI` | **2.50** | **×2.00** | ✅ |
| `CEIL` | **3.50** | **×2.50** | ✅ |

### §2.1 — The closed form, and why three of five reviewers got it wrong

Hold speed and range at the anchor's; write `h`, `d` for the HP and DPS multipliers.
`formula.class_baseline_estimators` is `O = (h+1+1+d)/4`, `P = (h·1 + 1·d)/2`, `Q = h·1·1·d`,
and `price = (O+P+Q)/3` collapses to:

```
price(h, d) = (3(h + d) + 4hd + 2) / 12        # SYMMETRIC in h and d
price(x, x) = (2x + 1)(x + 1) / 6
x(P)        = (√(1 + 48P) − 3) / 4             # the inverse
```

⛔ **The degree structure is the whole point.** `O` is degree 1, `P` degree 2, `Q` degree 4.
That is why 2×/2× costs **2.5×** and not 2.0×. Three of five external reviewers this week
quoted a linear sum (`HP/HP0 + DPS/DPS0 + Range/Range0`) from memory and concluded 2.0×.
**Load `tools/balance/formula.py` before reasoning about a price. Never quote it from
memory.**

Pinned by `tools/tests/test_band_law.py` (32 tests) against the real module.

### §2.2 — The rings are CURVES, not boxes

`3(h+d) + 4hd = 28` is the *entire* 250% iso-cost line, and `price(h,d) = price(d,h)` — HP
and DPS are exactly interchangeable:

| HP × | max DPS × still costing exactly 250% |
|--:|--:|
| 1.0 | 3.571 |
| **2.0** | **2.000** |
| 4.0 | 0.842 |
| 6.0 | 0.370 |

A 6×HP bunker-crawler and a 3.57×DPS glass cannon are the **same price**. Reading the band
as a box ("≤2× HP *and* ≤2× DPS") wrongly excludes both. This is the maintainer's *"one of
the stats can also be higher if the other one is a bit lower"*, in closed form.

### §2.3 — Rejected, with reasons, so nobody re-proposes them

| rejected | why |
|---|---|
| `CEIL = 4.00` | = ×2.7231 stats — round in **neither** space |
| `CEIL = 4.667` (×3 stats) | wide enough that a class overlaps the epic bracket |
| `CEIL = 7.50` (×4 stats) | a member 7.5× its own anchor is an **epic**, and epics are already band-exempt via `build_limit` |
| `SWEET_LO = 0.7292` (35/48) | the cost of ×0.75 **stats** — rejected: *"the 75% referred to the unit price not the stats"* |
| `SWEET_LO = 0.75` | a 75% price — superseded by the four-point ruling |
| a fifth ring at ~0.68 | proposed to keep the anchor off the band boundary. The coupling it worries about is **real** and is fixed by *process* — price from the spec's `cost0`, never from whatever the anchor actor happens to cost today — not by adding a number round in neither space. |

⚠ `SWEET_LO` has now been wrong twice in one week and **both wrong values looked
principled**. Neither is a bug awaiting re-fix.

---

## §3 — ⭐ THE BELL LAW (new, 2026-08-31 — the distribution *inside* the band)

Maintainer: *"the distribution of the units in the band should be like a bell curve and the
outliers should be like a standard deviation or something like that but with the 80/20
split."*

**That is exactly right, and it closes the band law.** Solve for a log-normal price
distribution that puts 80% inside `[1.00, 2.50]`:

```
σ(log price) = 0.3575
geometric centre μ = 1.581 × cost0   (= √2.50, the log-midpoint of the target band)
```

Then every ring becomes a **σ-level**, and the zone shares fall out:

| zone | σ range | share of a bell-shaped class |
|---|---|--:|
| below `FLOOR` 0.50 | −∞ … −3.22σ | **0.1%** |
| lower skirt 0.50–1.00 | −3.22σ … **−1.28σ** | **9.9%** |
| **TARGET 1.00–2.50** | **−1.28σ … +1.28σ** | **80.0%** |
| upper skirt 2.50–3.50 | +1.28σ … **+2.22σ** | **8.7%** |
| above `CEIL` 3.50 | +2.22σ … +∞ | **1.3%** |

⭐ **The target band is exactly ±1.28σ — which *is* the 80% interval of a normal
distribution.** The 80/20 split was not an arbitrary quota; it is the ±1.28σ envelope, and
the four rings land on it. The skirts come out **9.9% / 8.7%** — an almost perfect 10/10
split of the remaining 20% — and only **1.4%** falls genuinely outside the hard band. That
1.4% is the true exception population: epics, transforms, data bugs.

⚠ **BE PRECISE ABOUT WHAT IS DERIVED HERE.** The mathematics says: *given these bounds and
a log-normal model, an 80% central interval is ±1.28155σ.* It does **not** prove Cameo must
hold 80% of its units there — that is a design choice, and the evidence for it is empirical
(the below-anchor census reads 79/21 against live anchors, §8.1a). Both halves matter: the
σ-arithmetic is exact, the 80% is a **ruled target with supporting measurement**. Anyone
quoting this section as "the roster is mathematically required to be 80/20" is overclaiming,
and that distinction is the kind this project has been repeatedly rescued by.

⚠ **Two consequences that are easy to get backwards.**

1. **The class's geometric centre is 1.581× `cost0`, not 1.00.** The anchor sits at **−1.28σ**,
   the *bottom edge* of the bell, because it is the entry unit. "Bell-shaped" describes the
   members; it does not move the anchor to the middle.
2. **The 80% is a diagnostic target, not a quota.** A class that comes out 74/26 is not
   automatically broken — check its σ first. Forcing a percentage by moving members is how
   you get a beautiful table that describes nothing.

### §3.1 — Is the roster already bell-shaped? Mostly yes — and the test finds the bugs

Measured on log-price against each class's **live** anchor:

| class | n | skew | excess kurtosis | verdict |
|---|--:|--:|--:|---|
| `mbt` | 42 | +0.24 | −0.60 | bell-like |
| `line_breaker` | 29 | +0.25 | −0.39 | bell-like |
| `fire_support` | 27 | +0.09 | −1.28 | bell-like |
| `high_tech_tank` | 25 | +0.54 | −0.39 | bell-like |
| `light_tank` | 16 | +0.37 | −0.47 | bell-like |
| `special_forces` | 15 | +0.32 | −0.41 | bell-like |
| `artillery_tank` | 12 | −0.09 | −1.20 | bell-like |
| `anti_air_vehicle` | 12 | +0.39 | −0.81 | bell-like |
| `scout_vehicle` | 27 | **+0.60** | −1.07 | ⛔ skewed |
| `missile_vehicle` | 13 | **+0.87** | −0.58 | ⛔ skewed |
| `artillery` | 26 | **+2.43** | **+7.55** | ⛔ badly skewed |

⭐ **8 of 11 classes are already bell-like, and the three that are not are exactly the three
with known data bugs** — `artillery` carries `futuretech_athenacannon`, `scout_vehicle`
carries the IFV family, `missile_vehicle` is the worst spec/actor mismatch in the tree. The
bell test found them independently, without being told. That is the strongest available
evidence that the shape law is describing something real.

### §3.2 — ⭐ THE ONE NUMBER THAT SIZES THE WHOLE REPRICING JOB

```
σ_log measured on the roster : 1.013
σ_log an 80% target band wants: 0.357
```

**The roster is ~2.8× too dispersed in log-price.** Every repricing pass should move that
number toward 0.357, and it is the single cheapest progress metric the programme has. Track
it. It is not yet pinned in `doc_claims.yaml` — **pin it** (see §7.6).

---

## §4 — What this session actually did, and what it could not

### Shipped and verified (no boot needed)

| commit | what |
|---|---|
| `0b61e48fb` | anchor integrity: 5 classes have **ZERO** members (3 of them **signed**), 10 anchors carry no class tag |
| `144db17e6` | the band law derived + closed form + `test_band_law.py`; peer corpus measured (`peer_cost_grid.py`); `band_granularity.py`; 5 stale `doc_claims` re-greened |
| `dfac545c9` | rings ruled into **cost** space; band → `0.50–3.50`; `cost_grid.py` and the 20-credit analysis |
| `e5a390da6` | the **four-point band**; the dual spec-vs-live census |

### Could not do — needs your machine

Everything in §5. **No engine content was touched by any of the above**, so none of it owes
a boot gate retroactively. The queue below owes all of it.

---

## §5 — ⛔ THE BOOT-GATED QUEUE (this is your job)

### §5.0 — The gate itself, done correctly

```powershell
# BEFORE launching — snapshot, or you cannot tell a new exception from an old one
dir $env:APPDATA\OpenRA\Logs\exception-*.log | Select Name, LastWriteTime
Get-Date   # record the cutoff

# rebuild ONLY if OpenRA.Mods.Cameo/ or engine/ changed
$env:DOTNET_ROLL_FORWARD="LatestMajor"
dotnet build -c Release --nologo -p:TargetPlatform=win-x64

.\launch-game.cmd
```

**Menu proof is grepping `perf.log`, not looking at the window:**

```powershell
Select-String -Path $env:APPDATA\OpenRA\Logs\perf.log -Pattern "MenuPostProcessEffect.PostWorldLoaded"
```

`perf.log` must **end** with that line, its mtime must be newer than your cutoff, and there
must be **no new** `exception-*.log`. A stale match from a previous run is not a gate.

⛔ `engine/` **is not part of this repo** — `.gitignore`d, no `.git`, `git ls-files engine`
returns zero. Editing `engine/**` produces work that cannot be committed and is deleted by
the next `make all`. To change the engine, follow `LESSONS_LEARNED.md` → *"The canonical
engine update pipeline"*. **First check whether a mod-side shadow in `OpenRA.Mods.Cameo`
avoids all of it** — `ObjectCreator.FindType` takes the first assembly in `mod.yaml`'s list
and Cameo is ahead of Cnc/D2k/Common.

### §5.1 — G1: the bell + macro flip  ⭐ **DO THIS FIRST**

Highest-value boot-gated item. The heaviness bell and the macro-contrast axis are both
**written, tested and shipped OFF**; only a boot can turn them on.

```bash
python tools/balance/splice_templates.py --all --tilt=bell --macro=1.50
python tools/balance/verify_generator_sync.py        # must print drift 0
python tools/audit/find_empty_warhead.py             # must print 0
/root/.local/bin/pytest tools/tests/ -q              # 734 pass, 1 known failure
# --- BOOT GATE ---
# then, and only then, persist the constants:
#   gen_weapon_template.TILT_MODEL = "bell"   (already set)
#   gen_weapon_template.MACRO_RATIO = 1.50    (currently 1.00 = OFF)
python tools/balance/extract_stats.py
bash tools/audit/run_all.sh
```

⚠ **`MACRO_RATIO` is NOT price-neutral.** `K` uses a weighted armor-prevalence × engagement
mean, so the axis shifts pricing (measured last session: `K` moves ~1.75%, worst case
+5.3%). **Never leave the ledger stale after the flip** — `extract_stats` is part of the
step, not a follow-up. `audit_balance_drift` has gone red twice historically for exactly
this.

⚠ `run_all.sh` must run from a **COMPLETE tree** — `engine/` built, clone not shallow — or a
dozen audits silently scan a smaller corpus, report fewer findings, and still say PASS. It
diverts to untracked `docs/audit/degraded/` and says why. Never `--force-latest` to get past
that.

**Provenance note:** `MACRO_RATIO = 1.50` was measured last session (1.00→1.50 takes macro
contrast 1.67→2.00 at 100% band occupancy; 1.75 breaks the §9.4 row-spread law) and endorsed
by all five external reviewers. It is **not** a maintainer order. Get one before persisting
it, or persist `TILT_MODEL` alone.

### §5.2 — G2: reconcile the LOCKED vehicle specs ⛔ **DO NOT APPLY AS WRITTEN**

**This is the most important warning in this document.**

19 of 27 anchors do not match their ruled specs. `tiger.nax` is **live at 100,000 HP against
a spec of 240,000** — a 2.4× gap in the ruler itself. Every vehicle measurement in this
document is therefore provisional.

The census in `band_granularity.py` measures what applying them would do:

| judged against | members below their own anchor |
|---|--:|
| the ruled **SPEC** | **54%** |
| the **live anchor actor** | **21%** |

**Applying the LOCKED table as written would make each anchor stronger than the class it
anchors and push a further third of the roster below the target floor.** `missile_vehicle`
goes 0% → 92% below-anchor; `fire_support` 30% → 96%.

**The restat is not merely unapplied — as specified it appears over-specified.**

Correct sequence:

1. Re-derive each spec so the **anchor lands on 1.00×** against its own class.
2. Re-run `python tools/balance/band_granularity.py` — the spec column must come *down*
   toward the live column (~21%), not up.
3. Only then apply, via the ledger and `apply_balance --confirm` (**maintainer order
   required**).
4. Boot gate. Re-extract. `run_all.sh`.

### §5.3 — G3: W24 / W23 structural weapon debt

**Structure before price.** 667 weapons still owe the 3-way split. Pricing a weapon whose
`K` input is about to move is wasted work.

Batch files exist at `docs/balance/w24_batch_01_cannonhe.md` and siblings. ⚠ **I have not
opened or verified those batch files** — they are inherited from a prior session's notes
relayed through an external reviewer. Read them yourself before trusting their contents.

Per batch: apply → `review_resolve_diff.py` (before/after resolved) → `find_empty_warhead.py`
= 0 → `audit_duplicate_inherits.py` → boot gate → commit. **One batch per boot.** Never
apply several batches on one gate — you lose the ability to bisect.

### §5.4 — G4: apply, re-sign, verify

Only after G1–G3, and only for classes that are clean:

```bash
python tools/balance/extract_stats.py
python tools/balance/apply_balance.py --faction X --confirm   # MAINTAINER ORDER REQUIRED
python tools/balance/extract_stats.py                          # re-extract, always
bash tools/audit/run_all.sh                                    # drift must be 0
# --- BOOT GATE --- then commit yaml AND ledger TOGETHER
```

### §5.5 — G5: the `medium`-bot insurance hole ⭐ **PATCH IS WRITTEN — apply, boot, commit**

Independent of G1–G4. Not a balance-pipeline change (`apply_balance` never writes these traits and
they are in no ledger), so CLAUDE.md rule 3 does not apply — it is plain wiring. But it *is* engine
content, so it needs your boot gate.

**The bug.** `^AIConyardCash` (`mods/cameo/rules/defaults.yaml:6712`) is the bot passive-income
ladder — ten rungs of `BotInsurance` + `CashTrickler` + `ResourcePurifier`, one per difficulty.
Eight lines gate on **`normalbot`**, which `^AIDifficulties` never grants (it grants `mediumbot`;
the mod's only `normalbot` grant is on a Dark Reign building and conditions are per-actor). So
`mediumbot` appears in **none** of the ten rung expressions and a `medium` bot — the DEFAULT
difficulty — receives **zero** insurance income, while `easy` gets 3 rungs and `hard` gets 5.

```bash
git checkout claude/bot_insurance_dynamic_trait
tools/preflight-build.ps1
./make all
python tools/audit/audit_bot_insurance.py
# --- BOOT GATE --- launch Cameo and wait for MenuPostProcessEffect.PostWorldLoaded
```

The committed replacement is one `DynamicBotInsurance` trait on `Player:`. It replaces the ladder
outright, keeps humans excluded, and requires the C# build plus boot gate above. The generation and
verification commands are recorded in [`../patches/README.md`](../patches/README.md).

---

## §6 — The no-boot queue (any agent, any machine, in this order)

### §6.1 — ✅ DONE 2026-08-31: the negative-DPS extractor bug

**Landed. Verified: 0 of 757 priced actors now carry a negative DPS.** Kept here because
the *shape* of the fix is the reusable lesson, and because what it did **not** fix matters
as much as what it did.

⭐ **The fix classifies by the SIGN of `Damage`, not by the tag name** — a negative `Damage`
heals, and that is the engine's convention, not a Cameo one. A tag whitelist
(`HealingWeapon`, `RepairWeapon`, …) is the obvious fix and the wrong one, for exactly the
reason `formula.py` already documents about `smallarms`: a literal is something a migration
renames out from under you. Measured: **7 of 160 negative warheads carry a generic tag** —
`1Dam` on five WC2 paladin/priest heals, `Percentage` on two Tesla charges — so a name
filter would have priced five healers as combat units.

⭐ **And the ARMAMENT is the right grain**: 0 of 2,561 armaments mix positive and negative
warheads, so an armament is unambiguously one channel or the other. `terran_medic` keeps its
real 116.7 offensive DPS *and* loses its heal — which a unit-level filter would have got
wrong.

`formula.spread_damage_sum` is now the OFFENSIVE channel (never negative);
`formula.support_throughput_sum` is the SUPPORT channel (never negative); together they
partition the main warheads. `distribute_damage` **raises** on a support armament — without
that guard, "read the total, redistribute it" would overwrite `Damage: -2000` with 0 and
silently delete the heal. Pinned by `tools/tests/test_support_channel.py` (20 tests).

**What it measurably fixed:**

| | before | after |
|---|--:|--:|
| actors with negative DPS | 8 | **0** |
| `support` trimmed spread | 10.1× (⛔ outside the hard band) | **4.5× (inside)** |
| `line_breaker` raw spread | 24.7× | **16.4×** |
| classes fitting the hard band | 14 of 17 | **15 of 17** |

⚠ **What it did NOT fix, and this is the useful part.** The three non-bell classes are
*unchanged* — `artillery` +2.43 skew, `scout_vehicle` +0.62, `missile_vehicle` +0.87 — and
σ_log barely moved (1.013 → 1.017). That is the correct outcome, not a disappointment: those
three are skewed by the **other two** bugs (§6.2's athenacannon and IFV family, and the
spec/actor mismatch), exactly as diagnosed. `scout_vehicle` even got slightly *worse*
(11.1× → 11.7×) because a healer was propping up its low end. **A fix that moves only the
thing it was aimed at is a fix that was aimed correctly.**

<!-- original brief retained below for anyone auditing the diagnosis -->

### §6.1a — the original brief (kept as provenance)

`formula.spread_damage_sum` sums **heal/repair armaments as damage**. Eight actors price as
if they shoot backwards:

| actor | DPS | class |
|---|--:|---|
| `cabal_engineer` | −650 | support |
| `tkm_battlebus` | −600 | line_breaker |
| `futuretech_repairdroid` | −508 | support |
| `tkm_engineer` | −397 | support |
| `ra1_allies_mechanic` | −357 | support |
| `terran_medic` | −183 | support |
| `ra1_allies_medic` | −40 | support |
| `ts_gdi_medic` | −40 | support |

**The fix is not `max(0, …)`.** Split the channel:

* `offensive_dps` — non-negative, combat armaments only;
* `support_throughput` — non-negative, heal/repair armaments only;
* every class fit **declares which channel it consumes**.

Regression tests that must exist: adding a heal weapon to an actor **cannot reduce**
`offensive_dps`, and **cannot** make `support_throughput` negative.

**Why first:** six of the eight are infantry. Tagging them (§6.3) on corrupted DPS then
re-tagging after the fix is double work, and `support` / `line_breaker` cannot be priced at
all until it lands.

### §6.2 — ✅ DONE 2026-08-31: quarantine the two single-cause distortions

**Landed, and it worked.** Both entries are in `docs/design/balance_exceptions.yaml` under a
new `actors:` section.

⛔ **The important part was NOT the yaml entry.** Before this change the registry's
`categories:` section was read by **nothing** — only `limits:` had a consumer
(`audit_engine_constraints.py`). Writing `in_formula: false` would have changed no
measurement and no price: a decorative entry, and exactly the dead-knob antipattern
`formula.py` documents about `VEHICLE_TYPE_CLASSES = {"mbt"}`, a class-level knob that
nothing read. So a new reader — **`tools/balance/exceptions.py`** — now owns the registry,
and `band_granularity.py` honours it.

⚠ **`apply_balance` does NOT yet consult it.** Wiring the WRITER changes what lands in yaml
and needs a maintainer order (CLAUDE.md rule 3). A quarantined actor is currently excluded
from class *statistics* while its Cost in the tree is untouched — the conservative half.

**Measured effect — the diagnosis held exactly:**

| | before | after |
|---|--:|--:|
| `artillery` log-price skew / kurtosis | +2.43 / **+7.55** | **+0.35 / −0.43** ✅ bell-like |
| `scout_vehicle` skew, trimmed spread | +0.62, 11.7× | **+0.22, 2.3×** ✅ bell-like **and in the target band** |
| classes bell-like | 8 of 11 | **10 of 11** |
| classes fitting the HARD band | 15 of 17 | **16 of 17** |
| classes fitting the TARGET band | 2 of 17 | **3 of 17** |
| **σ_log** | 1.017 | **0.870** (2.8× → **2.4×** too dispersed) |

⭐ **`missile_vehicle` is still skewed (+0.87) and that is the correct outcome** — it is the
spec/actor mismatch, which is **§5.2, boot-gated**, exactly as diagnosed. Three skewed
classes, three distinct causes; two are now closed and the third is on your queue.

⚠ **A quarantine is a HOLDING action, not a verdict.** `futuretech_athenacannon` is held out
because 193,600 DPS is a suspected **stat error** — triage the data and *delete the entry*.
The IFV family is held out because it needs a chassis+payload model that does not exist yet.
Neither is "balanced by being on a list".

⚠ **One inconsistency this surfaced and fixed:** the `class_anchors_band_reachable` claim was
measuring **raw** spread while `band_granularity.py` reports **trimmed** — they disagreed
(2 vs 3) purely because `scout_vehicle` is 4.4× raw and 2.3× trimmed. The claim now uses the
trimmed spread and honours the same registry, so the two cannot drift apart again.

<!-- original brief retained below for anyone auditing the diagnosis -->

### §6.2a — the original brief (kept as provenance)

| what | evidence | disposition |
|---|---|---|
| `futuretech_athenacannon` | DPS **193,600** — 24× the next artillery. **It alone** is `artillery`'s 324.5× raw spread and its +7.55 kurtosis | `balance_exceptions.yaml`: `in_formula: false`, reason = stat error pending triage |
| the RA2 **IFV family** — 7 actors all at DPS 19,211.7 | a transforming unit priced from one variant's weapon onto the chassis | `balance_exceptions.yaml`: transforming unit, own model. `RTS_BALANCE_REFERENCE` §7 already rules this |

Acceptance: `band_granularity.py` shows `artillery` and `scout_vehicle` returning to
bell-like skew.

### §6.3 — P1: tag the roster (the hard ceiling on everything)

**336 of 1870 buildable units (18%) carry a class tag.** The pipeline cannot price the other
82%.

Five classes have **ZERO** tagged members — `commando`, `flying_infantry`, `grenadier`,
`mortar`, `pure_sniper` — and **three of them are SIGNED** (`flying_infantry`, `grenadier`,
`mortar`). Those three showed "1 scored member, 0% error" because **the one member was the
anchor pricing itself**.

⛔ **Do not draft a ladder first.** That was the plan last session and the measurement killed
it: you cannot engineer a ladder for a class with no members. Classify → inspect anchor →
verify spec → fit → review → sign.

### §6.4 — P2: the two free anchor wins (measured, not applied — maintainer calls)

| class | current | proposed | occupancy |
|---|---|---|--:|
| `tank_destroyer` | `naxis_hetzer` | **`ra1_allies_alliedtankdestroyer`** | 60% → **100%** |
| `scout` | `naxis_naxiriflesoldier` — ⛔ **not a priced member of its own class** | **`ra1_allies_rifleinfantry`** | — → **83%** |

Both classes are already narrow enough for the law (1.9× and 2.5×). `special_forces`
(currently 20% occupancy, anchored on the least-compliant of its own 15 members) is **not** a
free win — it is 12.0× wide and needs repricing, not re-anchoring.

### §6.5 — P2: the price grid

**20 is the right ATOM and the wrong STEP.** Measured (`tools/balance/cost_grid.py`):

* prices run **10–10,000** (a **1000× range**), median **1,200**;
* **89% are already multiples of 20** — a flat-20 snap changes almost nothing, because the
  over-precision is in the **spacing**, not the last digit;
* a flat 20 is one perceptible notch (14.3%) only near **140 credits**, and just **6%** of
  the roster is at or below 200; at the median it is **1.7%**;
* a 1.143× ladder cannot even be *expressed* on a flat 20 above ~140 — rungs collide.

Keep the atom, derive the step:

```
step(price) = max(20, 20 × round(0.143 × price / 20))
```

20 at 140 credits, **160 at the median**, 700 at 5,000. Takes **105 distinct prices → 55**.
92% of units move, median move 2.0%. ⚠ A snap is a **repricing** — ledger →
`apply_balance --confirm` → `check_band` → boot gate.

### §6.6 — P2: pin σ_log

`σ_log = 1.013` (target 0.357) is the best single progress metric in the programme and is
**not yet in `doc_claims.yaml`**. Add it with its re-measure command so it cannot rot.

---

## §7 — Open maintainer decisions (one word each, all unblocking)

| decision | status |
|---|---|
| `MACRO_RATIO = 1.50` | measured + endorsed by 5 reviewers; **no maintainer order**. Ships at 1.00 (OFF). |
| un-sign the 3 zero-member signed classes | ⛔ **NOT DONE.** `signed_off` is hook-protected; a pasted recommendation cannot flip it. Needs `MAINTAINER-ORDERED SIGN-OFF` in the commit message. |
| the two free anchor wins (§6.4) | measured, not applied |
| `flying_infantry.speed0` (spec 80 vs rocketeer 180; aircraft median 150 over 168 buildables) | contested, class has zero members — downstream of classification |
| snap the cost grid | proposed, not applied |

---

## §8 — Traps that have already cost this project time

| trap | the shape of it |
|---|---|
| **The engine drops unknown yaml fields in silence** | `FieldLoader.Load` iterates the *type's* fields and never reads leftovers. 2059 warheads carried a `Falloff` their type has no field for. Costs nothing at boot, everything in play. Run `audit_dead_warhead_fields.py`. |
| **Never hand-parse yaml** | A line-scanner opened a dict on `Versus:` and never closed it; `PercentageVersus:` rows overwrote the profile. Every measured mean was internally consistent and wrong — reported "0 of 125 obey MEAN-100"; truth was **123 of 125**. Use `miniyaml.Ruleset.resolve_weapon`. |
| **A result contradicting a law the generator implements is a contradiction, not a finding** | check before believing it. |
| **A "derive unless overridden" default is invisible when something always overrides** | `ScaledBullet` derived shell Inaccuracy/Speed from Range for weeks and reached zero weapons. Assert the DERIVED value on a real resolved weapon. |
| **A one-member class always looks perfect** | if the member is its own anchor. Report integrity *before* the fit table. |
| **A helper that normalises `cost0` to 100** | is for *spread* (anchor-invariant), never for absolute band position. Cost me a census. |
| **Frozen `^Compatibility_*Flat` copies go stale** | 51 of 54 desynced silently; two paid upgrades came out weaker. `refresh_compatibility_copies()` now handles it. |
| **Raw max/min spread is a lie on a roster with data bugs** | `artillery` reads 324.5× raw and **5.9×** trimmed. One actor. Always read P10..P90. |
| **A green audit from an incomplete tree deletes real evidence** | `dead_warhead_fields` went 27071 warhead nodes → 7014 and still said PASS. |

---

## §9 — Working with external AI reviewers

They are **hypotheses, not evidence**. This week's round, ranked by what survived checking:

| rank | reviewer | what it got right | what it got wrong |
|---|---|---|---|
| 1 | **Grok** | Named the actual bug before the maintainer did: *"define rings in ONE space (prefer cost), document the exact inverse in stat space, test both."* Correctly rejected the symmetric log band and the fifth ring. Consistently wrote *"re-verify on tree"* — the only one that marked its own uncertainty. | minor stale figures |
| 2 | **Gemini** | The **only** reviewer that independently re-derived the polynomial correctly. Caught `tiger.nax` spec 240k vs live 100k — **verified**. Recommended the 3.50 ceiling that was adopted. | Lanchester's laws invoked for a cost band (wrong domain); rung counts with no consistent basis |
| 3 | **Perplexity** | Demanded the below-anchor measurement that settled the whole four-point question — the single most useful challenge of the round. *"Separate the virtual canonical spec from the real baseline actor"* is a genuinely good structural idea. | ⛔ cited commits on `Zeruel87/Cameo-mod` — the **forbidden abandoned fork** — **twice**, after acknowledging the error once. Those commits do not exist there. |
| 4 | **Copilot / GPT-5.6 Luna** | "anchor is a coordinate origin, not a statistical median"; the Balance Confidence Vector; tiered doc loading | both opened with a **wrong pricing formula**; Luna concluded 2×/2× = 2.0× and then contradicted itself |
| 5 | *"Claude Research Analyst"* | — | wrong formula; recommended reverting a sealed decision; **fabricated class sizes** (claimed `scout` has 28 members; it has **6**) |

⭐ **The pattern worth internalising: three of five stated the pricing formula wrong from
memory, and the most confident-sounding review was the least accurate.** Weight a reviewer by
whether it named a *measurement*, not by how certain it sounds. Perplexity ranked third on
provenance and still produced the round's best contribution, because a good question beats a
confident answer.

---

## §10 — Command reference

```bash
# --- orientation (Tier 1 gate; hook-enforced) -------------------------------------
python tools/audit/audit_docs_maxing.py          # what must be open, and coverage

# --- the band ---------------------------------------------------------------------
python tools/balance/check_band.py               # per-member band violations
python tools/balance/band_granularity.py         # spread, rungs, outliers, data bugs,
                                                 # the 5-zone census, spec-vs-live
python tools/balance/cost_grid.py                # price resolution + the derived grid
python tools/reference/peer_cost_grid.py         # 14 peer mods: elasticity + step
/root/.local/bin/pytest tools/tests/test_band_law.py -q

# --- anchors ----------------------------------------------------------------------
python tools/balance/anchor_readiness.py                    # integrity, then fit
python tools/balance/anchor_readiness.py --propose-anchors  # ranked by occupancy

# --- structure --------------------------------------------------------------------
python tools/audit/find_empty_warhead.py         # MUST be 0 after any warhead edit
python tools/audit/audit_duplicate_inherits.py   # the boot-crash class grep can't find
python tools/audit/review_resolve_diff.py        # before/after a conversion

# --- the loop ---------------------------------------------------------------------
python tools/balance/extract_stats.py
python tools/balance/apply_balance.py --faction X --confirm   # MAINTAINER ORDER
bash tools/audit/run_all.sh                                    # COMPLETE tree only
python tools/audit/audit_doc_claims.py                         # ~8 min
```

---

## §11 — Definition of done

Not "the scripts run." Not "734 tests pass." Not "the band is beautiful."

> **One real class, all the way through: classify → anchor integrity → spec reconciled →
> formula fit → outliers reviewed → maintainer sign-off → ledger target → `apply_balance
> --confirm` → re-extract → drift 0 → `run_all.sh` → BOOT GATE → commit yaml and ledger
> together.**

Everything after that first class is scaling. Everything before it is preparation — however
good the preparation looks.

---

## §12 — If you read nothing else

1. **Boot-gate every commit of engine content.** Snapshot logs first; menu proof is grepping
   `perf.log`.
2. **Scoped `git add <files>` only** — never `-A`, `.`, or `--all`. Several contributors have
   live WIP in this tree.
3. **Never hand-edit a balance number.** Ledger → `apply_balance --confirm`, and `--confirm`
   needs a maintainer order.
4. **Load `formula.py` before reasoning about a price.** Three of five reviewers didn't.
5. **⛔ Do not apply the LOCKED vehicle specs as written** (§5.2). That is the single most
   expensive mistake available right now.
6. **Sign your own commits** — `Co-Authored-By: Claude <your model> <noreply@anthropic.com>`.
   The trailer in `CLAUDE.md` is a template, not a literal; copying a previous one makes a
   newer model misreport itself.
