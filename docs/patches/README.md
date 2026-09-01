# `docs/patches/` — yaml changes prepared where they could not be boot-gated

⛔ **Why patches instead of commits.** CLAUDE.md rule 1 and the commit gate are absolute: engine
content (anything the game parses at boot — all of `mods/**`) may not be committed without a boot
proof, and `tools/hooks/bash_guard.py` enforces it on `git commit`. A cloud container has no
`engine/` build and no `%APPDATA%/OpenRA/Logs`, so the gate is **unsatisfiable there, by design**.

The wrong answers are to disable the hook, or to leave the work in an ephemeral container's
working tree where it dies with the container. The right answer is this directory: the change is
authored, verified as far as a boot-less environment can verify it, and committed as a patch that
applies in one command on a machine that *can* boot.

**A patch here is not a decision.** It is a prepared change waiting on the boot gate and, where
noted, on a maintainer ruling. Apply, verify, boot, then commit the yaml — and delete the patch in
that same commit, so this directory never accumulates changes that already landed.

---

## Applying one

```bash
git apply --check docs/patches/<name>.patch     # dry run first, always
git apply         docs/patches/<name>.patch
# ... run the verification named in the patch's section below ...
# ... BOOT GATE: launch-game.cmd -> main menu, no new exception-*.log ...
git add <the yaml files>  docs/patches/<name>.patch  <any doc the section says to update>
git rm  docs/patches/<name>.patch
git commit
```

⚠ Never `git add -A` (CLAUDE.md rule 2) — other contributors have live WIP in this tree.

---

## The bot-insurance work, in three parts

⭐ **Read this first.** `01` and `03` are alternatives, not a sequence you must complete. `01` is
the one-token stopgap that needs only a boot. `03` is the real replacement and needs a C# build.
`03` is written to apply on top of `01`, so landing both in order is fine and is the expected path.

**One command:**

```bash
bash docs/patches/apply_all.sh --check     # dry run: applies the series, checks it, undoes it
bash docs/patches/apply_all.sh             # apply, build, run every check that needs no game
```

It stops before the boot gate on purpose and prints exactly what is left for you. ⚠ The patches are
a **series, not independent** — 03b rewrites the block 01 edits — so applying them out of order or
individually with `--check` fails on the second. Use the script.

⚠ **Why the C# ships as a patch too.** `OpenRA.Mods.Cameo/` counts as engine content to
`bash_guard.py` exactly like `mods/`, so a boot-less environment cannot commit the `.cs` either.
`tools/tests/test_bot_insurance_model.py` reads the trait **out of the patch** when the file is not
in the tree, so the model-vs-C# drift guard stays live in the window before it lands — which is
precisely when the two are being edited together.

(`02`, which relocated the ladder to the `Player` actor, has been **deleted** — `03` subsumes it
entirely, and this directory must never hold a patch that is already superseded.)

---

## `bot_insurance_01_fix_medium_difficulty.patch`

**Status:** ready. Fixes a live bug. Needs the boot gate; needs no design ruling. **58 lines,
eight identical token changes.**

`^AIConyardCash` gates its four lowest rungs on **`normalbot`**. `^AIDifficulties` grants
`mediumbot` and never `normalbot`; the mod's only `normalbot` grant is on the Dark Reign building
`drpplant1.freedomguard` (`darkreign.yaml:3348`), and conditions are per-ACTOR, so the ladder's
host never sees it. `mediumbot` appears in **none** of the ten rung expressions, so a `medium`
bot — the DEFAULT difficulty — receives **zero** insurance income while `easy` gets 3 rungs and
`hard` gets 5.

The patch replaces those eight `normalbot` references with `mediumbot`. Nothing else.

```bash
python tools/audit/audit_bot_insurance.py     # FAILS before, PASSES after
python tools/audit/audit_doc_claims.py        # bot_insurance_unreachable_difficulties: 1 -> 0
```

| player kind | before | after |
|---|--:|--:|
| human | 0 | 0 |
| campaign | 0 | 0 |
| easiest / veryeasy / easy | 1 / 2 / 3 | 1 / 2 / 3 |
| **medium** | **0** ⛔ | **4** |
| hard … cameogod | 5 … 10 | 5 … 10 |

⛔ **Humans stay at zero, deliberately.** An earlier draft of this patch also opened the four
lowest rungs to human players. That was **wrong and has been reverted**: one rung is
`Interval: 1, Amount: 1` = 1 credit/tick, and a buildable oil derrick is
`Interval: 250, Amount: 250` = **the same 1 credit/tick**. Four rungs is therefore four free oil
derricks, against a human derrick cap of 3 (`player.yaml:279`,
`CashTrickler < 3 && derricklimit_is_3`). The human safety net is, and stays, the two mechanics
already in `player.yaml:243-262`: insurance while you have no construction yard, and the sub-1000
trickle that stops ally cash transfers from bankrupting the giver.

---

## `bot_limits_04_brutal_explicit_cadence.patch`

**Status:** ready, zero behaviour change. 17 lines.

`BotLimits@brutal` was the only tier declaring none of the four cadence modifiers. It falls back to
the trait default of 100 (`OpenRA.Mods.CA/Traits/BotModules/BotLimits.cs:22-28`), which happens to
sit correctly between `veryhard`'s 125 and `challenger`'s 75 — **monotonic by luck, not by intent.**
The patch states them explicitly. Nothing changes today; it stops the ladder breaking silently if
the trait default ever moves. Skippable if you want the smallest possible Saturday diff.

---

## `bot_insurance_03a_dynamic_trait_csharp.patch` + `bot_insurance_03b_dynamic_trait_yaml.patch`

**Status:** ⚠ written and algorithmically verified, **but the C# has never been compiled** — a
cloud container has no `engine/` and no dotnet. Needs a build, then the boot gate.

### What it does

Deletes all thirty ladder nodes from `^AIConyardCash` and puts **one trait on `Player:`**:

```
	DynamicBotInsurance:
```

No conditions, no per-difficulty duplication. The trait reads `self.Owner.BotType`, finds its
index in a `Difficulties` list, and interpolates everything from that index — so a new difficulty
is one more name in one list.

| | easiest | → | cameogod |
|---|--:|:-:|--:|
| threshold tracking rate / tick | 1 | … | 10 |
| delay divisor (`delay = average / divisor`) | 10 | … | 100 |
| **PEAK** credits / tick (paid at zero cash) | 1 | … | 10 |
| purifier bonus | 5% | … | 50% |

### The net-worth layer (maintainer rulings, 2026-09-01)

Four rulings, all implemented and all pinned by tests:

| ruling | what it means in the trait |
|---|---|
| **Two-factor distress** | Liquidity (cash) decides *whether* the insurance arms and fires; net worth decides *how much*. |
| **Fog-safe self-comparison** | The peer ratio is against the bot's **own peak net worth** — never another player's, which no human can see and which would rubber-band against how well you are playing. |
| **Geometric mean** | `√(r_self · r_target)`, never the product. The two ratios are correlated, so multiplying squares one piece of evidence. |
| **Conservative par curve + logging** | The curve's ratio is **clamped to [0.5, 2.0]** so its invented magnitudes cannot dominate, and the trait logs measured-vs-expected worth every 1500 ticks so they can be replaced with real numbers. |

⭐ **The false positive this removes.** A bot at zero cash in the middle of a push, holding a
30 000-credit army, is not bankrupt — it is spending correctly and its harvesters will refill it.
Measured, at zero cash after a crash:

| bot | assets | worth factor | paid over 3000 ticks |
|---|--:|--:|--:|
| mid-push, big army + base | 60,000 | 25% | 2,800 |
| crippled, scraps left | 6,000 | 63% | 7,033 |
| wiped out | 500 | 83% | 9,318 |
| no `PlayerStatistics` | — | 100% | 11,200 |

⚠ **The last row is the degradation path**: without `PlayerStatistics` the worth factor stays
neutral and the trait behaves exactly as it did before net worth existed. Absence degrades, never
breaks.

⛔ **`ArmyValueWeight` ships at 0, and that is a decision not an oversight.** `PlayerStatistics`
exposes both `ArmyValue` and `AssetsValue`, and whether `AssetsValue` already counts combat units
could not be settled without the Common assembly. At 0 the army is counted exactly **once**,
through `AssetsValue`. If a real game shows `AssetsValue` excludes the army, raise it to 100 —
but do not guess: a wrong value double-counts the largest term in the calculation.

### ⛔ The par curve is a TABLE, because a formula here is a desync

The curve feeds a `[Sync]` value in a simulation OpenRA replays in lockstep across machines.
`Math.Exp` is **not** guaranteed bit-identical across platforms or runtimes, so evaluating a
logistic live is a desync waiting for a multiplayer game. It is therefore sampled at authoring
time into `ParShape` — 25 permille samples every 0.125× the midpoint — and interpolated with
integer arithmetic end to end (including an integer `IntSqrt`, not `Math.Sqrt`).

⭐ **Which is also better for tuning:** the economy model is a yaml array, so retuning it needs no
rebuild — exactly what "ship conservative, log for tuning" asks for.

⚠ Sampling costs accuracy, and it is asserted rather than hoped: the table tracks the logistic it
came from to within **1.9%**. At the first attempt, a 0.25× step diverged 22.5% and a mismatched
steepness convention added another 24.7% — both caught by the cross-check test, not by inspection.

⭐ **And two magnitudes still need no inventing:** the asymptote is `10000 + 15000 × (rank+1)`,
which is 5000 per harvester slot because `HarvesterLimit` is exactly `3 × (rank+1)`; and the
midpoint interpolates 23400 → 7200 ticks, which is 12 minutes × `ProductionTimeMultiplier`.

### The state machine

**ARMING** → the bar **tracks** the rolling average: it moves toward
`clamp(average, 1000, 10000)` by at most `rate` per tick — **up when the average is above it, down
when below, same rate either way**. When the owner's cash falls **strictly below** the bar, freeze
it and compute `delay = average / divisor`.
**DELAYING** → wait that long; recovering above the frozen bar cancels outright and unfreezes.
**PAYING** → grant a payout **scaled by how deep below 10000 the owner is**, plus the purifier
percentage on the same scale, until cash reaches 10000 — then re-arm.

Three design points, each of which was wrong in an earlier draft and is now pinned by a test:

⭐ **The payout is PROPORTIONAL TO DEPTH, which is where the old ladder's granularity lived.** The
ten rungs were not just ten difficulties — they **stacked**, so a `cameogod` bot drew 1 credit/tick
just under 10000 and 10 credits/tick near zero. A flat on/off payout throws that away and hands the
hardest bot its maximum for the entire time it is insured. So:

```
depth_permille = clamp(1000 * (10000 - cash) / 10000, 0, 1000)
accumulator   += peak_rate * depth_permille
grant          = accumulator / 1000          # remainder carries to the next tick
```

Measured against the old ladder — it reproduces the curve and then fills in **between** the rungs:

| cash | old rungs (cameogod) | new credits/tick (cameogod) |
|--:|--:|--:|
| 9000 | 1 | 1.00 |
| 7500 | 3 | 2.50 |
| 5000 | 5 | 5.00 |
| 2500 | 8 | 7.50 |
| 1000 | 9 | 9.00 |
| 0 | 10 | 10.00 |

⚠ **Integer milli-credits, never floating point** — the payout must be deterministic across
machines or it desyncs. The carried remainder is what makes a 0.5 credit/tick rate actually pay
1 credit every other tick instead of truncating to nothing, which is what a naive integer divide
does to every low difficulty.

⛔ **The bar tracks both ways. It is not a one-way ramp and not a falling bar.** A bar *falling*
from 10000 (the first spec) is **dead mechanics**: the trigger is easiest to satisfy when the bar
is *highest*, so a falling bar fires on tick one for anyone under 10000 and only ever makes
triggering harder afterwards. Simulated, every difficulty behaved identically and the entire
ordering came from the delay divisor — the rate did nothing at all. A one-way *rising* bar works
but never comes back down, so it stops describing the economy the moment the economy changes.

⛔ **The trigger is strictly `<`, not `<=`.** With `<=` the bar converges to the average, the
average converges to a stable cash pile, and **every bot under the cap eventually insures itself** —
an emergency measure quietly becoming baseline income. Measured: at `<=` a bot cruising at 9000
insured itself; at `<` it does not. `test_a_stable_bot_is_not_subsidised` pins it.

⛔ **`MinThreshold` must be greater than zero.** This is the answer to *"0 for the lowest
boundary?"* — **no, and 0 breaks the feature outright.** The bar tracks the average, so a bot stuck
at zero drives its own average to zero, the bar follows it down, and `cash < 0` is unsatisfiable.
Measured, floor 0 strands a bankrupt bot **permanently**; floor 500 or 1000 rescues it. The floor
is the absolute poverty line: below it you are insured whatever your history says.

| floor | bot stuck at 0 | bot stuck at 300 |
|--:|---|---|
| **0** | ⛔ **stuck forever** | ⛔ **stuck forever** |
| 500 | rescued | rescued |
| **1000** (chosen) | rescued | rescued |

⚠ **And `MaxThreshold` should stay at 10000, not go higher** — it is both the bar's ceiling and the
payout exit, and raising it insures bots that are not in trouble. A bot running a 25 000 economy
that dips is left alone at a cap of 10000 and subsidised at 20000:

| cap | crash 25000 → 12000 | → 15000 | → 18000 |
|--:|---|---|---|
| **10000** (chosen) | not insured | not insured | not insured |
| 20000 | ⛔ insured | ⛔ insured | ⛔ insured |

⚠ **Honest limit on the rate knob.** Even tracking both ways, `ThresholdRatePerTick` is the
**weakest** of the three difficulty levers: the trigger fires as soon as cash dips below the
tracked average, so convergence speed rarely gates anything. The divisor and the credits/tick are
what actually separate the difficulties (both 10×). If the rate needs more bite, the lever is
`AverageWindow`, not the rate.

### What the removal buys, and what it costs

⭐ **No more conyard scaling.** The ladder sat on the construction yard, so it multiplied by
conyards owned (`BotLimits` allows `cameogod` **7**) — a late-game snowball — and switched off
entirely when a bot lost its last conyard, the exact "stuck with no income" case it exists to
prevent. Both are gone.

⭐ **Self-limiting by construction, and it tapers.** A payout stops at 10000 and shrinks as the
owner approaches it, so difficulty buys **speed and depth of help, not a bigger cap**. Recovering a
bankrupt bot over 12000 ticks: easiest reaches 6977, medium 9912, cameogod 9991 — the same
destination, approached at very different rates. This is a real nerf to late-game AI economy
(previously up to 7 conyards × 10 rungs = 70 credits/tick; now 10 at the absolute peak) and it is
intended.

### ⚠ What is verified, and what is not

**Verified** (`tools/balance/bot_insurance_model.py`, 50 tests in
`tools/tests/test_bot_insurance_model.py`): the state machine — the track/freeze/delay/pay/re-arm
cycle, monotonicity across difficulties, no payout oscillation, a rich bot never insured, a stable
bot never subsidised, a bankrupt bot always rescued at every difficulty, both boundary choices
above, the payout ramping rather than switching on, fractional rates surviving integer truncation,
the purifier only paying while paying, and the model's constants matching the C# field
defaults parsed out of the `.cs`. The maintainer's worked example (average 1500 → 150-tick delay at
easiest) is pinned as a test.

**NOT verified — do these at build time:**

1. ⛔ **It does not compile here.** No `engine/`, no dotnet. Expect to fix small things.
2. ⛔ **`INotifyResourceAccepted` on the Player actor.** The purifier half depends on the engine
   delivering that notification to a Player-actor trait. `ResourcePurifierCA` carries the same
   `self.Info.Name == "player"` special case and guards its floating text with
   `HasTraitInfo<IOccupySpaceInfo>()`, so it plainly expects to run there — but that is inference,
   not proof, and the notifying code is in an assembly this repository does not contain.
   **Check in a running game that purifier income is credited**; if it is not, the fallback is a
   thin refinery-side trait that forwards the value.
3. **Balance.** The numbers above are the shape you asked for, not a tuned economy.

### On CA vs Common for the purifier — the answer is neither

Today's yaml says `ResourcePurifier:`, and with the assembly order
`AS, CA, Cameo, Cnc, D2k, Common` **nobody in this repository can say which type that resolves
to** — the vendored copy is named `ResourcePurifierCA`, so the bare name resolves past it into an
assembly that is not here. Folding the logic into `DynamicBotInsurance` removes the question
entirely: it becomes Cameo-owned, single-assembly, and reads the way it behaves. That is also why
the whole mechanic is one trait rather than three from three assemblies.
