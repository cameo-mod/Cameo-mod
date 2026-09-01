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
passive income"* — was substantially correct, and my first check was wrong because I greped
`ai.yaml` and `player.yaml` only. **The multipliers live in `mods/cameo/rules/defaults.yaml`.**

### ✅ The complete cheat surface is exactly two trait types

Enumerated by scanning every `mods/cameo/**/*.yaml` for a trait gated on a `*botplayer`
prerequisite. **Two types, ten tiers each, and nothing else:**

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

### ✅ A third axis nobody named: `BotLimits` decision cadence

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

### ⛔ The passive-income claim: NOT FOUND in this mod

No `CashTrickler`, `GrantCash` or income trait anywhere in `mods/cameo/**/*.yaml` is gated on a bot
difficulty. The only `CashTrickler` hits are a Warcraft 2 Orcs building available to any player
(`PauseOnCondition: disabled`, not bot-gated).

⚠ **Recorded as not-found, not as false.** It may exist in the engine's lobby-handicap system —
which `engine/` is not in this repository, so no reader here can check — or it may be a memory of a
different mod. **Whoever has the running game should confirm before the removal roadmap treats it
as a target.**

### ✅ And a fourth, already documented: omniscient vision

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
| 5 | `ProductionCostMultiplier` → 100 | learning shows parity | last, because it is the largest single advantage |

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
2. **Add the difficulty multipliers to the log schema (§6.2).** A match record that does not name
   the bot's cost and time multipliers cannot be compared across difficulties, and the cheat-removal
   roadmap in §1 is a sequence of exactly such comparisons.
3. **Team layer enters at the same phase as main-target selection**, as a `TeamSituation` published
   by one deterministic coordinator — never as a module that issues orders to allies.

⚠ **What this round does *not* change:** the phase order, the snapshot shape, the synced bridge, or
the one-owner rule. Every agent that proposed replacing those proposed it without evidence, and
`AI_ARCHITECTURE` §11.4 already rejected the five-module pipeline variant on the same grounds.

### Open decisions this file adds

| id | decision |
|---|---|
| **OD-C** | Strongest-first (maintainer's rule) vs the finishing exception — which wins when they disagree? Decide from phase-1 logs. |
| **OD-D** | Does a team cash-transfer API exist? ⛔ Verify before any sharing code. |
| **OD-E** | Is there a per-difficulty passive income anywhere (engine lobby handicap)? Not found in this mod; needs someone with the running game. |
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
