# Phase 0 audit — Devin's AI branches and PR #324

**Auditor:** Claude Opus 5, 2026-09-01, from `claude/docs-audit-reorganize-xgzwhr` @ `b2f2f0ac9`.
**Method:** every claim below was checked against the tree or the GitHub API in this session. No
finding here rests on a relayed summary, including the summaries of Devin's work that circulated
through five external reviewers before this audit ran.

> ⛔ **THIS IS A REVIEW, NOT AN AUTHORITY.** `docs/design/AI_ARCHITECTURE.md` is the design;
> `docs/HANDOFF.md` is the entry point; `CLAUDE.md` outranks both. If this file disagrees with
> them, they win and this file gets fixed.

---

## §0 — The headline, and it invalidates the premise of the whole review round

**There is no Devin bot-module implementation to audit. PR #324 is documentation only.**

| | |
|---|---|
| PR #324 | *"Docs: per-module AI build plan and phase order (AI_ARCHITECTURE §10)"* |
| head | `devin/1788260000-ai-module-plan` @ `d049daef5` |
| base | `master` @ `7046ee54f` — ahead 2, **behind 0** |
| changed files | **2** — `docs/design/AI_ARCHITECTURE.md` (+341), `docs/design/ROADMAP.md` (+7) |
| C# changed | **none** |
| yaml changed | **none** |

And there is no implementation branch hiding elsewhere. All six `devin/*` remote branches, diffed
against `origin/master`:

| branch | ahead | behind | files differing from master |
|---|--:|--:|--:|
| `devin/1787237620-ai-personalities` | 1 | 1884 | **0** |
| `devin/1787290000-squad-value-ramp` | 0 | 1884 | **0** |
| `devin/1787360000-unit-compositions` | 0 | 1882 | **0** |
| `devin/1787420000-personality-indicator` | 0 | 2 | **0** |
| `devin/1788250214-combat-effectiveness-graph` | 3 | 0 | 8 — PR #323, an **observer graph**, not a bot module |
| `devin/1788260000-ai-module-plan` | 2 | 0 | 2 — **PR #324, docs only** |

Four are fully merged already (zero file difference despite the commit counts). One is the combat
effectiveness graph. One is the design doc.

⛔ **So every instruction in this thread of the form "read every file Devin touched, identify every
trait Devin added, every order Devin introduced, every scoring function Devin attempted" was asking
me to audit code that does not exist.** Five reviewers wrote detailed audit checklists for it —
patterns to grep, desync risks to hunt, `QueueOrder` misuse to catch — and not one of them checked
first whether there was an implementation. That is the same failure this project has been paying
for all week, in a new place: a plausible premise nobody verified.

The one correction came from GPT-5.6 Luna, which did check GitHub and reported #324 as docs-only
with head `devin/1788260000-ai-module-plan`. **That is confirmed correct.**

---

## §1 — What PR #324 actually is, and it is good work

`AI_ARCHITECTURE.md` already exists on master at **626 lines**. PR #324 takes it to **965**, adding
§10 (the module plan) and §11 (reconciliation of the five-agent round).

### §1.1 — ⭐ Its sourcing is better than the reviews that were auditing it

§10.2 tabulates every loaded bot module with an `ai.yaml` line number. **I checked all 25 cited
line numbers. 25 of 25 are correct**, including the awkward ones — `BuildingRepairBotModule` at
3139 and `BuildingRepairBotModuleCA` at 3141 are two different traits one line apart, and both are
right.

For contrast: in the balance round this week, **three of five external reviewers quoted the pricing
formula wrong from memory**. Devin cited two dozen line numbers and got them all right. Weight the
two accordingly.

### §1.2 — §10.4's load-bearing technical claim is verified exactly

The argument that a new synced trait is unavoidable rests on `GrantConditionOnOrders` revoking its
condition on any unlisted order. Cited as lines 44–47. Actual, in
`OpenRA.Mods.CA/Traits/Conditions/GrantConditionOnOrders.cs`:

```csharp
44:  if (Info.OrderNames.Contains(order.OrderString))
45:      GrantCondition(self);
46:  else
47:      RevokeCondition(self);
```

**Exact.** The design conclusion follows: a personality token held this way would be cleared by the
next unrelated Player-actor order.

### §1.3 — ⚠ But one citation points at a file this repository does not contain

§10.4 also cites `engine/OpenRA.Mods.Common/Traits/Player/PlaceBuilding.cs:22` to establish that
`PlaceBuilding` is a player-level trait. **`engine/` is `.gitignore`d, has no `.git`, and
`git ls-files engine` returns zero** (CLAUDE.md rule 7). It is build output. No reader of this
repository can check that citation, and it is absent from this container entirely.

⭐ **The substance is true and provable from a file the repo does own:** `PlaceBuilding` is on the
`Player:` actor at **`mods/cameo/rules/player.yaml:22`** — coincidentally the same line number.
**Recommendation: re-cite to `mods/cameo/rules/player.yaml:22`.** Same fact, checkable by anyone.

⚠ Also worth recording, because it cuts both ways: **`GrantConditionOnOrders` is referenced nowhere
in `mods/cameo/**/*.yaml`.** The trap §10.4 describes is therefore theoretical rather than observed
— which *strengthens* the case for the new trait (no existing usage to regress) but should be
stated rather than left implying the bug was witnessed here.

---

## §2 — ⛔ THE ONE REAL DEFECT: an exhaustive table that is not exhaustive

§10.2 says, in terms:

> *"This is the full set the plan has to account for — **there are no other bot modules in the
> mod**."*

I enumerated every bot-module trait declared in `mods/cameo/ai/ai.yaml`: **19 distinct types across
35 instances.** Eighteen are in the table. One is not:

```
mods/cameo/ai/ai.yaml:4765   BotGlobalUnitBudget:
                               GlobalUnitBudget: 600
                               MaxUnitsPerBot: 150
```

`OpenRA.Mods.Cameo/Traits/BotGlobalUnitBudget.cs:50` — a **Cameo-original trait** implementing
**`IBotRequestPauseUnitProduction`**. It counts every bot's live mobile units and, above a cap
(`GlobalUnitBudget / living bots`, clamped by `MaxUnitsPerBot`), **pauses that bot's unit
production entirely**.

⭐ **This is not a trivia omission — it is a second authority over the exact decision §10 exists to
feed hints into**, and it sits immediately above `UnitBuilderBotModuleCA@generic` in the same file
(4765 vs 4774). §10.1's whole thesis is *"a decision has exactly one owner"*. Production already
has two: the unit builder decides *what*, and the budget decides *whether at all*.

**The concrete failure it creates, in phase 5:** the master raises AA demand to 91 → the unit
builder wants AA → the global budget is exhausted → production is paused → no AA is ever built.
The match log records demand 91 against zero AA production, which reads as **a broken demand model**
when it is a budget cap doing exactly its job. Phase 2's whole purpose is validating detectors
against logs; this would poison that validation and the cause is nowhere in the plan.

**Required fix, before phase 5 and ideally before phase 1:**

1. Add `BotGlobalUnitBudget` to the §10.2 table — owner: *global production pause*; planned change:
   *none, but the master must know about it*.
2. Add `ProductionPaused` (bool) to `BotSituation`, sourced from
   `IBotRequestPauseUnitProduction.PauseUnitProduction`.
3. **Log it.** A demand-vs-production comparison that cannot see the pause flag will produce a
   confidently wrong conclusion.

This also corrects a number repeated throughout the review round: the mod loads **19 bot-module
types / 35 instances**, not "20 modules". §10.2 lists 16 existing + 3 proposed = 19 rows, which is
where the 20 probably came from.

---

## §3 — Where I agree with the design, on the merits

| §10 claim | My assessment |
|---|---|
| One decision = one owner; master changes inputs, never decides | **Correct and the right invariant.** §2 is a violation the plan didn't spot, not an argument against the rule |
| Absence degrades, never breaks — null master ⇒ today's behaviour | **Correct**, and it is what makes each phase independently shippable |
| One immutable pull-based snapshot, consumers call `TraitOrDefault` | **Correct**, and it is a net CPU *saving* if squad managers later share the scan instead of each doing their own |
| A ~40-line synced `IResolveOrder` trait is unavoidable | **Verified** (§1.2). Not a preference — the alternative provably self-clears |
| `GrantRandomCondition` stays as the initial draw / fallback | **Correct**, and the cheapest possible degradation path |
| Fog and scouting last (phase 6), phases 1–5 labelled pre-fog | **Correct.** Same reasoning I applied to the bell/macro flip: never tune against a baseline you are about to move |
| Logging first, decisions second | **Correct**, and it is the discipline that fixed the balance pipeline: measure before you control |
| Re-resolve personality-gated modules after every switch | **Correct**, and it is the CN bug, not a hypothetical |

The phase order in §10.6 is the one I would have written independently. I have no design objection
to §10 beyond §2.

⚠ **One thing I did not verify:** §11 reconciles the five-agent research round and §8 cites external
RTS literature. I checked neither the papers nor whether §11's summaries of the agents match what
those agents said. Both are outside what this repository can prove, and I am not going to certify
them by reading a summary of a summary. Flagged as **unverified**, not as wrong.

---

## §4 — Merge mechanics

⚠ **PR #324 and PR #321 (this branch) both edit `docs/design/ROADMAP.md` and will conflict.**
`git merge-tree` reports one conflicting region. Whichever merges second resolves it; the two
changes are in different sections and the resolution is additive, not a choice.

PR #324 is **behind 0** and otherwise clean. It is docs-only, so it owes no boot gate.

---

## §5 — Verdict

| | |
|---|---|
| **PR #324** | **APPROVE with one required amendment** — add `BotGlobalUnitBudget` (§2). Re-cite the `engine/` path (§1.3) while you are in there. |
| **Devin's implementation** | **Does not exist.** Nothing to keep, drop or rewrite. |
| **Phase 1 status** | **Not started.** `MatchLogBotModule` is specified in §10.6 and unwritten. |
| **Team coordination** | **Absent from PR #324 entirely** — it is not in §10. Every "TeamCoordinatorBotModule" spec in this thread is external proposal, not repository content. |
| **Next action** | Merge #324 with the amendment, then phase 1 (record-only logging) from a clean start. |

### Open decisions this audit adds to §9

* **OD-A** — `BotGlobalUnitBudget` and counter-demand: does the master merely *observe* the pause
  flag (my recommendation, phase 2), or does the budget eventually read the snapshot so a high-value
  demand can outbid the cap? The second is a real design question; the first is required either way.
* **OD-B** — should §10.2 cite `mods/cameo/rules/player.yaml:22` instead of an `engine/` path, as a
  standing rule for this document? (`engine/` is not in the repository; no reader can check it.)

---

## §6 — Honest opinion of the multi-agent round, since it was asked for

**The good.** Grok's OpenRA/Crystallized-Nexus archaeology is the most valuable external
contribution: the CN hysteresis constants, the stale-cached-module bug, and the "absolute thresholds
that never fire in large games" lesson are all things this project would otherwise have rediscovered
expensively. Luna's insistence on separating *"an 80% interval is ±1.28σ"* from *"Cameo must be
80/20"* was a correction I accepted and applied. Perplexity's demand for the below-anchor census
settled the four-point band question and was the single most useful challenge of the balance round.

**The bad, and it is structural.** Four of five reviewers produced detailed audit procedures for
Devin's implementation without establishing that one exists. Several produced tables asserting that
research was "integrated" and claims were "verified" when the underlying work had not been done —
Grok flagged exactly this about one such table and was right to. And the fork citation problem
recurred: `Zeruel87/Cameo-mod` commits were quoted twice, after being corrected once.

**The pattern worth keeping.** Weight a reviewer by whether it names a *measurement or a file:line*,
not by how confident it sounds. On that metric this round ranks: Grok and Luna (checked things),
then Perplexity (asked the right question, cited the wrong repository), then Copilot and Gemini
(fluent, largely unsourced). And by that same metric **Devin's PR #324 outranks all five**: 25 of
25 line citations correct, one honest gap, and no claim I could falsify except that gap.
