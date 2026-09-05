# AI research reconciliation — rounds 2–3 (team play, learning, cheats)

**Compiled by:** Claude Opus 5, 2026-09-01. Sources: Perplexity, Grok, ChatGPT/Luna, Copilot,
Gemini, Devin, and the maintainer.

> ⛔ **SCOPE, so this does not duplicate a document that already owns the subject.**
> `AI_ARCHITECTURE.md` **§11 already reconciles round 1** (the five differentiated briefs:
> literature, OpenRA archaeology, decision maths, engine feasibility, yaml/migration) and it does
> so rigorously — it rejects invented constants and a false "all research synthesised" claim by
> name. **Do not re-litigate §11 here.** This file covers only what came *after* PR #324 was
> written and is therefore in no repository document at all:
>
> * **round 2** — cooperation between bots in team games, and target policy by game mode;
> * **round 3** — machine learning / RL integration, and the difficulty-cheat removal roadmap.
>
> `AI_ARCHITECTURE.md` is the design and outranks this file. `docs/HANDOFF.md` is the entry point.
> `CLAUDE.md` outranks both.

**Status vocabulary, used on every claim below. Nothing is admitted without one.**

| tag | means |
|---|---|
| ✅ **VERIFIED** | checked against this tree or a primary source in this session, with the evidence shown |
| ◑ **PLAUSIBLE** | consistent and probably right, but nothing here proves it — safe to design toward, not to cite |
| ⛔ **REJECTED** | contradicted by evidence, or asserted as settled fact with none |
| ⚠ **OPEN** | a real disagreement; recorded, not averaged |

---

## §1 — ✅ The difficulty cheats: measured, and the maintainer was right

The maintainer's correction — *"you were wrong earlier about the AI not having other cheats. There
exist production cost and build time modifiers for each AI difficulty ... and also there is a
passive income"* — was correct in **every** particular, including the passive income, and my first two checks were
wrong. Check one greped `ai.yaml` and `player.yaml` only; **the multipliers live in
`mods/cameo/rules/defaults.yaml`**. Check two then declared the income "not found" after searching
the whole mod for the *concept*; it is there under a name that never says what it does, and the
maintainer's third correction supplied that name. **The cheat surface is four axes, not two.**

| # | axis | where |
|--:|---|---|
| 1 | production cost / time multipliers | `defaults.yaml:3977`, `:4007` |
| 2 | `BotLimits` decision cadence | `ai.yaml:37-142` |
| 3 | **passive income — the `BotInsurance` ladder** | `defaults.yaml:6712` |
| 4 | omniscient vision | `AI_ARCHITECTURE.md` §0.2 |

### ✅ Axis 1 — production cost and time

Enumerated by scanning every `mods/cameo/**/*.yaml` for a trait gated on a `*botplayer`
prerequisite. **Two types, ten tiers each:**

| difficulty | `ProductionCostMultiplier` | `ProductionTimeMultiplier` |
|---|--:|--:|
| easiest | 115 | 130 |
| veryeasy | 110 | 120 |
| easy | 105 | 110 |
| **medium** | **100** | **100** |
| hard | 95 | 90 |
| veryhard | 90 | 80 |
| brutal | 85 | 70 |
| challenger | 80 | 60 |
| unbeatable | 75 | 50 |
| **cameogod** | **70** | **40** |

`defaults.yaml:3977` (time) and `:4007` (cost). At `cameogod` the bot pays **70% of a player's price**
and builds in **40% of the time** — a 1.43× economy and a 2.5× production-speed advantage. Below
`medium` the multipliers run the other way: the easy tiers are *handicapped*, not merely un-buffed.

### ✅ Axis 2 — `BotLimits` decision cadence, which nobody named

Separate from the above, in `ai.yaml`, `BotLimits@<difficulty>` sets
`Building/UnitDelayModifier` and `Building/UnitIntervalModifier` — **how often the bot issues
orders**, as a percentage:

| easiest | veryeasy | easy | medium | hard | veryhard | brutal | challenger | unbeatable | god |
|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|
| 300 | 250 | 200 | 175 | 150 | 125 | *(100)* | **75** | **50** | **25** |

⚠ **This is a distinct cheat from the production multipliers and is easy to double-count.** At
`cameogod` the bot *decides* 4× as often **and** builds at 40% time **and** pays 70% cost. Three
independent multipliers compound. `BotLimits` itself has **no** cost or income field — its ten
fields are limits and timings only (`OpenRA.Mods.CA/Traits/BotModules/BotLimits.cs:18-33`).

### ✅ Axis 3 — the passive-income claim: FOUND, and it is the largest of the four

⛔ **Correction, 2026-09-01.** This section previously read *"NOT FOUND in this mod"*. That was
wrong. The maintainer named it: *"the cash trickler for bot income you didn't find is called
something like bot insurance and it's basically the same thing as a cash trickler but with a delay
and a threshold under a certain cash amount so the bot can not get stuck on no income and
rebuild."* Correct down to the mechanism.

⭐ **Why the first grep missed it, because the lesson generalises.** It *is* a `CashTrickler` — but
it is gated on a condition whose name contains no economic word at all (`easiestbotinsurance` …
`cameogodbotinsurance`), granted by a Cameo-original trait called `BotInsurance`, and it lives on
the **construction yard**, not on the Player actor or in `ai.yaml`. Searching for the *concept*
("cash", "income", "trickler gated on a bot condition") returned nothing usable. Searching for the
maintainer's *word* found it in one grep. **Grep the name as well as the concept.**

#### The trait

`OpenRA.Mods.Cameo/Traits/BotInsurance.cs` — 92 lines, `ITick`, three fields:

| field | default | meaning |
|---|--:|---|
| `Condition` | *(required)* | granted while the owner is broke |
| `Threshold` | 1000 | grant below this much cash |
| `ThresholdDuration` | 250 | ticks the owner must stay below it first — the maintainer's *"delay"* |

`Tick` (lines 71–90): while `Cash >= Threshold` the counter resets to `ThresholdDuration`, else it
decrements; the condition is granted when `GetCashAndResources() < Threshold && ticks < 0`. The real
delay is therefore `ThresholdDuration + 1` ticks — **250 ticks ≈ 10.0 s** at this mod's default
40 ms timestep (`mods/cameo/mod.yaml:551`).

⚠ **Two different quantities, one comparison.** The countdown reads `Cash`; the grant reads
`Cash + Resources`. A bot sitting on a full silo counts down forever and never triggers. Harmless
today, but the delay is measured against a different number than the trigger and anyone editing
this must know it.

#### The wiring: ten rungs on the construction yard

`^AIConyardCash` (`mods/cameo/rules/defaults.yaml:6712`), inherited by **47** construction-yard /
faction-HQ actors including the `^Conyard` template. Ten rungs, each one
`BotInsurance` + `CashTrickler` + `ResourcePurifier`:

| rung | `Threshold` | `defaults.yaml` | reachable by |
|---|--:|--:|---|
| cameogod | 10000 | 6715 | cameogod |
| unbeatable | 9000 | 6728 | unbeatable, cameogod |
| challenger | 8000 | 6741 | challenger and above |
| brutal | 7000 | 6754 | brutal and above |
| veryhard | 6000 | 6767 | veryhard and above |
| hard | 5000 | 6780 | hard and above |
| medium | 4000 | 6793 | ⛔ **nobody** — see the bug below |
| easy | 3000 | 6806 | easy, and hard and above |
| veryeasy | 2000 | 6819 | veryeasy, easy, and hard and above |
| easiest | 1000 | 6832 | easiest and above |

Every rung carries `ThresholdDuration: 250` and the same two payouts: `CashTrickler`
`Interval: 1, Amount: 1, ShowTicks: False` — **1 credit per tick = 25 credits/s** — and
`ResourcePurifier` `Modifier: 5`.

⭐ **It is a graduated floor, not a flat stipend, and that part is good design.** Rungs switch on
independently as cash falls and switch off again as it recovers, so a `cameogod` bot draws 1
credit/tick below 10 000 and 10 credits/tick below 1 000. It cannot be starved out — exactly the
stated intent.

⚠ **The magnitude is not small, and it is per construction yard.** The rungs sit on the conyard, so
a bot with N conyards runs N independent ladders, and `BotLimits` allows `cameogod` **7**
(`ai.yaml:134`). Fully broke that is 7 × 10 = **70 credits/tick = 1 750/s ≈ 105 000/min**, plus a
stacked resource-value bonus if `ResourcePurifier.Modifier` is the percentage its other uses in this
mod imply (RA2's Ore Purifier and TS Nod both use `Modifier: 25`).
⚠ **That multiplication is arithmetic from the yaml, not an observation in play.** `engine/` is not
in this repository, so `CashTrickler` and `ResourcePurifier` semantics could not be read from
source here. Confirm the credited rate in a running game before this number drives a decision.

#### ⭐ Humans get a comeback mechanic too — and that sets the fairness target

`mods/cameo/rules/player.yaml`, on the `Player:` actor, for **every** player, bot or human:

| lines | what |
|---|---|
| 243–254 | `BotInsurance@secondaryinsurance` — `Threshold: 10000`, `ThresholdDuration: 1500` (**60 s**), gated `nobase` (no conyard) → `CashTrickler` 1/tick |
| 255–262 | `GrantConditionOnPlayerTotalCash` `Threshold: 999` → `CashTrickler@comeback` 1/tick |

So *"bots should only have what a player has"* has a concrete definition here: **one rung, 1
credit/tick, below ~1 000 cash** — plus the base-loss floor. The ten-rung conyard ladder is the part
that exceeds it, and the target state is one rung, not zero.

### ⛔ A real bug found while verifying the above: `medium` bots get no insurance at all

The four lowest rungs gate on **`normalbot`** where every other file in the mod uses **`mediumbot`**:

```
defaults.yaml:6801  RequiresCondition: (normalbot || hardbot || ...) && mediumbotinsurance
```

`^AIDifficulties` (`ai.yaml:16-18`) grants `mediumbot`, never `normalbot`. The only
`GrantConditionOnBotOwner` in the mod that grants `normalbot` is on a Dark Reign building,
`drpplant1.freedomguard` (`darkreign.yaml:3348`) — and conditions are per-actor, so it is invisible
to the conyard that needs it.

**Consequence, by exhaustion of all ten `RequiresCondition` lists: `mediumbot` appears in none of
them.** A `medium` bot receives **zero** insurance income, while `easy` gets 3 rungs and `hard` gets
5. The difficulty ladder has a hole in its middle, and it is the default difficulty.

⭐ **RULED 2026-09-01, and the ladder is being replaced outright.**

⛔ **First, a reversal.** An earlier draft opened the four lowest rungs to human players, on a
reading of "parity". **That was wrong and the maintainer caught it:** one rung is
`Interval: 1, Amount: 1` = 1 credit/tick, and a buildable oil derrick is
`Interval: 250, Amount: 250` = **the same 1 credit/tick**. Four rungs is four free oil derricks
against a human derrick cap of **3** (`player.yaml:279`, `CashTrickler < 3 && derricklimit_is_3`).
Humans keep exactly what they already had and nothing more: insurance while they hold no
construction yard, and the sub-1000 trickle that stops ally cash transfers from bankrupting the
giver (`player.yaml:243-262`).

**The committed replacement**, `OpenRA.Mods.Cameo/Traits/DynamicBotInsurance.cs` plus its Player
and conyard YAML changes — thirty yaml nodes and ten condition ladders
become **one trait on `Player:` with no conditions at all**. It reads the owner's bot type, finds
its index in a `Difficulties` list, and interpolates a tracking rate (1→10), a delay divisor
(10→100), credits/tick (1→10) and the purifier bonus (5%→50%) from that index. The ore-purifier
half is folded in, which also settles the `ResourcePurifier` ambiguity: the bare name resolves past
the vendored `ResourcePurifierCA` into an assembly this repo does not contain, so **neither CA nor
Common — Cameo owns it now.**

⭐ **What it buys.** No conyard scaling (the ladder multiplied by conyards owned, up to 7 for
`cameogod`, and switched off entirely when a bot lost its last conyard — the exact stuck case it
exists for). A payout stops at 10 000, so **difficulty buys speed, not a bigger total**. Measured
first payout after a crash: easiest 800 ticks, cameogod 80 — exactly 10×, monotonic at every step.

⭐ **Verified as code and algorithm.** `./make all` compiles the trait; Cameo boots to the main menu.
`tools/balance/bot_insurance_model.py` mirrors `Tick` line for line and
`tools/tests/test_bot_insurance_model.py` pins the behaviour, including a drift guard
that parses the C# field defaults. Four design points were wrong in earlier drafts and are now
tests: the payout **scales with depth** rather than being flat on/off, which is where the old
stacked ladder's granularity lived (it reproduces that curve and fills in between the rungs); the bar **tracks both ways** rather than falling (a falling bar is dead mechanics — the
trigger is easiest to satisfy at the highest bar); the trigger is **strictly `<`** (with `<=` every
bot under the cap eventually insures itself); and `MinThreshold` **must exceed 0** (at 0 a bankrupt
bot is stranded permanently). Generation and verification details: `docs/patches/README.md`.

⚠ **A second finding, C#-side:** `BotInsurance` marks `ticks` `[VerifySync]` but the class does not
implement `ISync`, so the sync check never runs on it — already recorded in the audit baseline
(`docs/audit/baseline/check_yaml_dedup.txt:11367`). `ticks` is driven by synced `PlayerResources`
state, so it should be `ISync`. A C# change means rebuild + boot gate; not done here.

⚠ **And a staleness note:** upstream CA shipped *"Don't enable bot insurance until 2 minutes into
the game"* (`docs/research/ca-staleness-audit.md:348`). Cameo's copy has **no game-time gate** —
the ladder is live from tick 251.

### ✅ Axis 4 — omniscient vision, already documented

`AI_ARCHITECTURE.md` §0.2 already records that the squad managers' actor scan respects only
`IVisibilityModifier`, not shroud — so bots see the whole map from tick zero except cloaked units.
The maintainer's point stands exactly: *"their artillery can shoot at maximum range; players cannot
shoot what they cannot see."*

### The removal order, and why this order

| # | remove | precondition | reason |
|---|---|---|---|
| 1 | nothing — **measure first** | phase-1 logs exist | you cannot attribute a regression to a cheat you removed alongside two others |
| 2 | vision (fog gate) | phases 1–5 stable | it is the cheat that most distorts *balance* data: every engagement metric collected under omniscience is biased |
| 3 | `BotLimits` cadence toward 100 | fog absorbed | a cadence cheat compensates for a bad decision loop; fix the loop first |
| 4 | `ProductionTimeMultiplier` → 100 | learning shows parity | |
| 5 | `BotInsurance` ladder → **one rung**, matching what a human player already gets | the bot can hold an economy without it | ⛔ do NOT delete it outright — its stated job is stopping a bot getting permanently stuck at zero income, and `player.yaml:243-262` gives humans the same floor. Parity is one rung, not zero. |
| 6 | `ProductionCostMultiplier` → 100 | learning shows parity | last, because it is the largest single *per-unit* advantage |

⛔ **One at a time, with a measurement between each.** Removing three at once and then asking which
one mattered is the mistake this project has already paid for twice this week.

⭐ **After removal, difficulty must be expressed behaviourally** — reaction cadence, composition
quality, coordination depth, learning on/off — not by cheaper tanks. All five agents agreed on this
and it is the right target.

⚠ **And it interacts with the balance pipeline.** `ProductionCostMultiplier` means a bot's effective
unit cost is **not** the price the class formula computed. Any balance measurement taken from a bot
match is off by up to 30% at the top tier. That is a real coupling between the two programmes and is
not recorded anywhere else.

---

## §2 — Team cooperation (round 2)

### ⛔ First, the thing that must not be built

Five agents produced `TeamCoordinatorBotModule` designs, one with ~200 lines of C#. **None of it is
in the repository, and PR #324 contains no team coordination at all.** It is external proposal.

⛔ **The C# that circulated should not be pasted in.** It was written against an API nobody in this
round verified — `player.IsBot`, `PlayerRelationship.Ally`, `BaseProvider`, `p.WinState.HasValue`,
`IGameSaveTraitData` field shapes — and this session has already shown twice what an unverified
mechanism costs. **Every one of those must be checked against the engine before a line is written.**
The *design* below is worth keeping; the code is not.

### ✅ The one architectural rule, and it is already in the tree

`AI_ARCHITECTURE.md` §10.1: **one decision, one owner; the master changes inputs, never decides.**
Every agent independently reached the same conclusion for the team layer, which is the strongest
signal in the round:

> The coordinator **publishes**, it never **commands**. Followers read a team snapshot and choose
> whether to act. A hive-mind that issues orders to every ally's units re-creates the duplicate-
> authority failure that §1.3 says the engine punishes with a hard crash.

Adopted as: `TeamSituation`, immutable, pull-based, rebuilt on a slower cadence (~300 ticks) than
`BotSituation` (~150), read via `TraitOrDefault`, **null ⇒ solo behaviour unchanged**.

### Target policy by game mode

| mode | policy | status |
|---|---|---|
| **2 teams** | Focus the **highest-threat** enemy — army × economy × tech × aggression — not the nearest or the weakest. Leaving the strongest player alone usually loses. | ◑ **PLAUSIBLE** — unanimous across agents and standard competitive practice, but no citation survived checking |
| | **Finishing exception:** a player one base-wipe from elimination and reachable outranks harassing the leader | ◑ PLAUSIBLE |
| | **Ally-coverage penalty:** 1 ally on the same target = small penalty, 2+ = large, so someone watches the second threat | ◑ PLAUSIBLE — and ✅ the *mechanism* is real: CN already uses a coverage weight for profiles (`AI_ARCHITECTURE` §1.6) |
| **3+ teams** | Commit to **one** enemy team until it is broken; do not split. **Bleed heuristic:** if two enemy teams are fighting each other, do not intervene. Reassign before the last weak team dies, so a snowballing team is not gifted the map. | ◑ PLAUSIBLE |
| **FFA** | Expand quietly early; **punish the leader**; do not eliminate the weakest early (they are a buffer); avoid kingmaking | ◑ PLAUSIBLE |

⚠ **OPEN (OD-C): the maintainer's instruction was "always attack the strongest team first, and the
strongest player in that team".** That is a stronger rule than the agents' scoring functions, which
allow the finishing exception to override. **Which wins when a weak player is one hit from death and
the strongest is untouched?** Not resolvable by assertion — phase-1 logs will show how often the two
disagree. Recorded, not averaged.

### The cooperation mechanisms, ranked by cost

| mechanism | shape | cost |
|---|---|---|
| **Shared focus target** | coordinator names it; followers treat it as preference, not constraint | cheapest; reuses the §4.3 target scorer |
| **Coordinated attack** | a **wave window**, not a continuous trickle: followers hold until a trigger, then commit together. Defenders do not join. | medium |
| **Ally defence** | contribute only *surplus* — never below your own defence minimum — and to the ally's construction yard, not their front line. Return when the attacker leaves. | medium; prevents the all-bots-rush-to-one-ally failure |
| **Expansion claim** | a mutex list of claimed cells so two allies do not race the same ore | trivial and high value |
| **Cash sharing** | surplus → starving ally, rate-limited, ⛔ **never while under attack** | ⚠ **VERIFY THE API FIRST** — do not write this until a team-cash transfer path is confirmed to exist. Two agents specified it in detail; neither checked. |

⭐ **Cash sharing is the maintainer's explicit concern** (*"so that one bot doesn't drain the team
mates dry"*) and is therefore worth doing — but a designed transfer against a nonexistent API is the
`balance_exceptions.yaml` dead-knob bug in a new place.

---

## §3 — Machine learning (round 3)

### ✅ The sequencing every agent converged on, and it is right

```
rule-based system stable  →  match logs  →  offline comparison (fixed vs dynamic)
    →  bandits on discrete choices  →  imitation seeds  →  selective RL on small action spaces
```

⭐ **Do not introduce RL before the rule base is stable.** RL on a broken rule base learns to exploit
the bugs, not to play. This is the same discipline as the balance pipeline's *measure before you
control*, and `AI_ARCHITECTURE` §11.3 already amended the design in the same direction: **the cheap
first learning step is an offline comparison of fixed vs dynamic personality on logged matches, not
a bandit.** A bandit that has not been shown to beat the fixed policy on Cameo's own data is not
cheap, it is unfalsifiable.

### Where RL is worth it, and where it is not

| target | verdict |
|---|---|
| Unit micro (movement, target selection in a fight) | ◑ highest ROI — small action space, fast feedback |
| Build-order / composition bias | ◑ tractable, clear reward (army value per credit) |
| Personality transition policy | ◑ replaces the least-evidenced part of the current design |
| **Full end-to-end deep RL** | ⛔ **not the next step, and probably never in-tree** — tens of thousands of matches, GPU training, an un-debuggable policy, and a desync surface. AlphaStar-class work is a research programme, not a PR. |

### ✅ Invariants that must hold whatever is learned

* Learning is **offline and unsynced**. Priors are reviewed data files; **no weights ever enter
  synced simulation state**.
* A **deterministic fallback** always remains. If the priors file is missing or unreadable, the bot
  plays the rule policy — ⛔ and it must say so, not silently behave as if the file were empty. That
  is the exact bug this session shipped and fixed in `tools/balance/exceptions.py`.
* **Mixed-opponent training.** Self-play alone produces a policy tuned to other bots; the standard
  mitigation is to keep frozen past versions and the rule-based bot in the pool.
* **Human match data must be in the mix from phase 1**, or the bandits only ever see bot-vs-bot.

⛔ **REJECTED — the specific numbers.** "70% self-play + 20% frozen + 10% rule-based",
"bandit priors need N samples", "fog costs 15–30% win rate": all stated as findings, none with a
citation that survived checking. ◑ Directionally plausible; ⛔ not admissible as design constants.
This is the same call `AI_ARCHITECTURE` §11.2 made about the invented `S_def > 0.35` thresholds, and
it is made the same way here.

---

## §4 — What this round changes in the plan

Three amendments to `AI_ARCHITECTURE.md` §10, all small:

1. ⛔ **Add `BotGlobalUnitBudget` to §10.2 and `ProductionPaused` to `BotSituation`.** From
   `DEVIN_BRANCH_REVIEW.md` §2 — it can pause all bot unit production and is a second authority over
   the decision counter-demand hints feed. Without it, a phase-5 log showing "AA demand 91, zero AA
   built" reads as a broken demand model when it is a budget cap working correctly.
2. **Add all four cheat axes to the log schema (§6.2).** A match record that does not name the
   bot's cost and time multipliers, its `BotLimits` cadence, **and its live insurance rungs** cannot
   be compared across difficulties, and the cheat-removal roadmap in §1 is a sequence of exactly
   such comparisons. ⭐ Insurance especially: it is *conditional* income, so it does not show up as a
   constant and a log that records only "credits earned" will attribute it to harvesting. Log the
   count of granted `*botinsurance` conditions per tick-bucket, per conyard.
3. **Team layer enters at the same phase as main-target selection**, as a `TeamSituation` published
   by one deterministic coordinator — never as a module that issues orders to allies.

⚠ **What this round does *not* change:** the phase order, the snapshot shape, the synced bridge, or
the one-owner rule. Every agent that proposed replacing those proposed it without evidence, and
`AI_ARCHITECTURE` §11.4 already rejected the five-module pipeline variant on the same grounds.

### ⭐ Findings for the "adaptive difficulty" question (2026-09-01, measured)

Two facts that were expensive to establish and that decide most of the design space. Recorded
here so nobody re-derives them.

⛔ **The production multipliers CANNOT move into a Player-actor trait.**
`ProductionTimeMultiplier` / `ProductionCostMultiplier` live in `^BotProductionBehavior`
(`defaults.yaml:3976`), which is inherited by **`^BasicUnit` and `^BaseBuilding`**
(`defaults.yaml:2406`) — i.e. by every produced actor, not by `Player:`. The engine asks the
**item being built** for its cost and time modifiers, so no trait on the Player actor can supply
them. (They are also gated on `Prerequisites: *botplayer`, not on the `*bot` conditions, so they
are a fourth gating mechanism on top of the three already catalogued in §1.)
**What IS possible:** keep one thin per-actor trait that reads its multiplier from a single
player-level authority. That takes 20 hand-maintained instances down to 2 and makes the values
continuous — but it needs two new Cameo traits implementing the engine's modifier interfaces,
not a field on `DynamicBotInsurance`.
⚠ **And the real risk is not the plumbing.** OpenRA recomputes an item's remaining cost against
its current total while it is building. A multiplier that moves mid-build can over- or
under-charge, or strand a queue. Any dynamic cost multiplier must be **sampled when the item is
queued and held for that item**, or only allowed to change while the queue is empty.

✅ **Army and building value are already available on the Player actor, for free.**
`PlayerStatistics` is at `player.yaml:217` and exposes `ArmyValue` and `AssetsValue` (read at
`CameoObserverStatsLogic.cs:478` and `:705`), plus `ArmySamples` / `IncomeSamples` histories.
`DynamicBotInsurance` can read them via `playerActor.TraitOrDefault<PlayerStatistics>()` with no
new scanning cost.
⭐ **This fixes a real false positive in the trait as written:** a bot at zero cash in the middle
of a push, holding a 30 000-credit army, is not bankrupt — it is spending correctly and its
harvesters will refill it. Today it would be insured. Distress is **liquidity AND low net worth**,
not liquidity alone.

⛔ **Binding fog rule for any of this: insurance inputs must be SELF-REFERENTIAL.** My cash, my
army, my assets, my own history. The moment the trait reads another player's values it is
omniscience — the same cheat class the roadmap in §1 exists to remove, reintroduced through the
economy instead of through vision. An adaptive *difficulty setting* comparing against opponents
may be a deliberate exception, but it must be argued for, not leaked in.

### ⭐ The net-worth "par curve" proposal (2026-09-01) — three corrections and two free numbers

Maintainer proposal: measure distress as net worth (cash + army + assets) against **two** ratios —
`r_target` (versus what this difficulty should be worth at this game time) and `r_peers` (versus
the average opponent) — and combine them into one "effective cash" for the insurance.
Model: **`tools/balance/bot_difficulty_curve.py`**; 14 tests in `test_bot_difficulty_curve.py`.

⛔ **1. The curve is a LOGISTIC, not an exponential approach.** "Rises slowly, then grows
exponentially, then flattens" is a **sigmoid**. The function usually reached for —
`A - (A-S)·e^(-t/τ)`, the **monomolecular / Mitscherlich / Newton-cooling** curve, a.k.a.
"exponential rise to a maximum" — has *no slow start*: it grows fastest at t=0 and only ever
decelerates. Measured on a `medium` bot, the two disagree most in exactly the window that matters:

| minutes | 0 | 5 | 10 | 15 | 20 |
|---|--:|--:|--:|--:|--:|
| logistic | 10,000 | 12,205 | 27,150 | 57,591 | 68,396 |
| Mitscherlich | 10,000 | **30,445** | 43,924 | 52,809 | 58,667 |

At 5 minutes the wrong curve expects **2.5×** what the right one does. Judging bots against it
would put nearly every early game "behind par" and fire the insurance for everybody.

⛔ **2. The two ratios must NOT be multiplied — they are correlated.** A bot behind the curve is
usually also behind the field, because both measure the same failure. Multiplying squares one piece
of evidence: the worked example 0.5 × 0.5 = **0.25** claims "four times worse than par" from two
observations that each said "twice". Worse, 0.25 × 0.25 = **0.0625** would pin the insurance at
maximum permanently. Use the **geometric mean** `√(r_target · r_peers)`: 0.5 and 0.5 → **0.5**, and
"twice the field but half the curve" → exactly **1.0**, which is the honest reading of par.

⛔ **3. `r_peers` is the omniscience rule from the previous section, re-entering by another door.**
It reads opponents' net worth, which no player can see, and it rubber-bands against the human's
success — the same objection that ruled out scaling the production multipliers by distress
(OD-L). It is a legitimate *choice*, not an error, but it must be made deliberately: it adds a
cheat to a roadmap whose purpose is removing them, and it makes bot difficulty depend on how well
the human is playing. A fog-safe alternative that keeps most of the value: compare against the
bot's **own peak** net worth rather than the field.

⭐ **Two numbers do NOT need inventing — they are already in the tree and already balanced:**

* **The asymptote scale.** `BotLimits.HarvesterLimit` is exactly `3 × (rank+1)` — 3, 6, 9 … 30,
  a precise **1× to 10× ladder**, which is the scale the proposal guessed at. Income capacity *is*
  harvester count, so the per-difficulty asymptote derives straight from it.
* **The time constant.** `ProductionTimeMultiplier` runs 130 (easiest) → 40 (cameogod). Scaling the
  curve's midpoint by it makes harder bots ramp proportionally sooner and reuses a number that is
  already tuned, instead of adding a second one to keep in sync.

⚠ **One trap to check before summing.** `PlayerStatistics` exposes both `ArmyValue` and
`AssetsValue`. If `AssetsValue` already counts combat units, then `cash + ArmyValue + AssetsValue`
**double-counts the army**. Confirm against the engine source before wiring it — Common is not
vendored here, so this could not be settled from this container.

⚠ **And an unrelated fragility found while measuring:** `BotLimits@brutal` (`ai.yaml:103`) declares
no `BuildingDelayModifier` / `BuildingIntervalModifier` / `UnitDelayModifier` /
`UnitIntervalModifier`, unlike every other tier. It falls back to the trait default of 100
(`OpenRA.Mods.CA/Traits/BotModules/BotLimits.cs:22-28`), which happens to sit correctly between
`veryhard`'s 125 and `challenger`'s 75 — so the ladder is monotonic **by luck, not by intent**.
Not a live bug; worth stating explicitly in yaml.

### ✅ RULED 2026-09-01 — the eight decisions, and what shipped

The maintainer answered all eight open questions in one pass. Recorded here as rulings, not
options; `docs/patches/README.md` carries the delivery detail.

| # | question | ruling |
|---|---|---|
| 1 | Sequencing | **No boot gate until Saturday.** Build everything now on a separate branch, `claude/bot_insurance_dynamic_trait`, never touching master. |
| 2 | Peer signal (OD-N) | **The bot's own peak net worth.** Fog-safe: it never reads another player, so it adds no omniscience and does not rubber-band against the human. |
| 3 | Distress input (OD-K) | **Two-factor.** Liquidity decides *whether*, net worth decides *how much*. |
| 4 | Combining the ratios | **Geometric mean**, never the product. |
| 5 | Delivery | **Committed source plus boot evidence.** The C# and YAML land together only after a preflight build and a main-menu boot. |
| 6 | Production multipliers (OD-J) | **Continuous only for the future `adaptive` type.** The ten fixed difficulties keep their fixed multipliers — they are the A/B baseline the whole cheat-removal roadmap is measured against. |
| 7 | Adaptive difficulty (OD-L) | **After `DynamicBotInsurance` boots.** Design now, build on proven code. |
| 8 | Par-curve magnitudes (OD-M) | **Ship conservative, log for tuning.** The curve's ratio is clamped to [0.5, 2.0] so invented numbers cannot dominate, and the trait logs measured-vs-expected worth every 1500 ticks. |

⭐ **Two things the implementation turned up that no amount of design would have.**

**The par curve cannot be a formula.** It feeds a `[Sync]` value in a lockstep simulation, and
`Math.Exp` is not guaranteed bit-identical across platforms or runtimes — a live logistic is a
desync waiting for a multiplayer game. It ships as a sampled integer table with an integer square
root. ⚠ Sampling it correctly took two corrections, both caught by a cross-check test rather than
by reading: a 0.25× step diverged **22.5%** from its own logistic, and a steepness expressed per
minute rather than per midpoint added another **24.7%** by giving faster difficulties a relatively
shallower curve. Final divergence: **1.9%**.

**`ArmyValueWeight` ships at 0 deliberately.** `PlayerStatistics` exposes `ArmyValue` *and*
`AssetsValue` and it is still unproven whether the latter already counts combat units. At 0 the
army is counted exactly once. ⛔ This is the one number that must be checked in a running game
before it is trusted — a wrong value double-counts the largest term in the calculation.

### Open decisions this file adds

| id | decision |
|---|---|
| **OD-C** | Strongest-first (maintainer's rule) vs the finishing exception — which wins when they disagree? Decide from phase-1 logs. |
| **OD-D** | Does a team cash-transfer API exist? ⛔ Verify before any sharing code. |
| **OD-E** | ✅ **CLOSED 2026-09-01 — it exists.** `BotInsurance` + `CashTrickler` + `ResourcePurifier`, ten rungs on `^AIConyardCash` (`defaults.yaml:6712`). See §1. What remains open is the *removal shape*, now row 5 of the removal order: trim to one rung (human parity), not to zero. |
| **OD-G** | ✅ **CLOSED 2026-09-01.** `normalbot` → `mediumbot`, plus human parity at four rungs (the maintainer's ruling). Patch written and verified; needs only the boot gate. |
| **OD-H** | ✅ **CLOSED 2026-09-01** — the ladder moves to the `Player` actor as part of the `DynamicBotInsurance` rewrite, which also removes the conyard multiplication. The one open verification is carried on that patch: does `INotifyResourceAccepted` reach a Player-actor trait? Needs a running game. |
| **OD-M** | ✅ **CLOSED** — ship conservative with the par ratio clamped, and log measured-vs-expected for tuning. |
| **OD-N** | ✅ **CLOSED** — the bot's own peak net worth. Fog-safe; no opponent data is read. |
| **OD-J** | ✅ **CLOSED** — continuous only for the future `adaptive` type; the ten fixed difficulties keep fixed multipliers. |
| **OD-K** | ✅ **CLOSED** — two-factor: liquidity decides whether, net worth decides how much. Shipped. |
| **OD-L** | ✅ **CLOSED** — `adaptive` is an opt-in 11th type, built after the insurance trait boots. |
| **OD-I** | Cheat-removal end state for axis 3: with the payout now capped at 10 000 and no conyard scaling, is the bot ladder still a "cheat" to remove, or is it now close enough to the human floor to keep permanently? Decide from phase-1 logs. |
| **OD-F** | Cheat-removal order — is vision genuinely first, given it is the one that most corrupts *balance* measurement rather than making bots weakest? |

---

## §5 — Honest scoring of the agents, on evidence rather than fluency

Applied consistently: **does the claim name a measurement, a file:line, or a primary source?**

| agent | strongest contribution | where it failed |
|---|---|---|
| **Grok** | The OpenRA/CN archaeology — CN's hysteresis constants, the stale-cached-module trap, "absolute thresholds never fire in large games". Reusable, sourced, and it flagged another agent's fake synthesis table. | Two architectural conclusions did not survive re-verification (`AI_ARCHITECTURE` §11.2) |
| **ChatGPT / Luna** | Checked GitHub and correctly reported #324 as docs-only when four others assumed an implementation. Separated *"an 80% interval is ±1.28σ"* from *"Cameo must be 80/20"* — a correction I adopted. Insisted on verified/inferred/proposed tagging. | Long relative to content; one round opened with a wrong pricing formula |
| **Perplexity** | Demanded the below-anchor census that settled the four-point band. A good question beats a confident answer. Also the best structural idea of the whole programme: **separate the canonical class spec from the real baseline actor.** | ⛔ Cited the forbidden `Zeruel87` fork twice, after acknowledging the error once |
| **Copilot** | Clear module-interaction write-ups; correctly restated the one-owner rule | Almost entirely unsourced; one round was a restatement of the existing spec presented as new |
| **Gemini** | Independently re-derived the pricing polynomial correctly — the only agent that did. Caught `tiger.nax` spec 240k vs live 100k, **verified**. | A "synthesis matrix" asserting findings other agents never produced (`AI_ARCHITECTURE` §11.2 rejects it by name) |
| **Devin** | ⭐ **Best-sourced work in the programme.** §10.2's 25 `ai.yaml` line citations: **25 of 25 correct.** §10.4's `GrantConditionOnOrders.cs:44-47` claim: exact. §11 rejects invented constants and a false synthesis claim without being asked to. | One exhaustiveness claim wrong (`BotGlobalUnitBudget`); one citation into `engine/`, which is not in this repository |

⭐ **The pattern worth internalising.** In the balance round, three of five agents quoted the pricing
formula wrong from memory. In the AI round, four of five wrote detailed audit procedures for a Devin
implementation **that does not exist**, without checking first. In both rounds the most
confident-sounding output was among the least accurate, and the most useful contributions were a
*question* (Perplexity), a *correction* (Luna), and a *file path* (Grok, Devin).

**Weight a reviewer by whether it names something checkable — never by how certain it sounds.**
