# How much work is the bot-module rework? — an estimate with its evidence attached

_Asked 2026-09-01: "how hard and how much effort will it be to implement the rest of the bot module
rework, and how many sessions will it take?" This is the answer, written 2026-09-02 once
[`AI_ARCHITECTURE.md`](AI_ARCHITECTURE.md) §10/§11 could be read (PR #324, still open — see §5)._

⛔ **The headline, before the numbers: only phases 1–2 can honestly be estimated.** Phases 3–7
depend on thresholds that the plan says, deliberately, will be *fitted from phase-2 logs* rather
than chosen up front (§10.6, §11.4). A number for them is a guess wearing a number's clothes. What
follows separates the two so the difference is visible instead of averaged away.

---

## 1. The calibration datum: what one trait actually cost

Not a guess — this workstream shipped exactly one new trait, and every part of it is measurable.

| artifact | lines |
|---|--:|
| `OpenRA.Mods.Cameo/Traits/DynamicBotInsurance.cs` | 631 |
| `tools/balance/bot_insurance_model.py` (line-for-line mirror, so the logic is testable) | 382 |
| `tools/tests/test_bot_insurance_model.py` | 504 |
| `tools/audit/audit_bot_insurance.py` | 256 |
| **total** | **1,773** |

**What it cost beyond the typing**, which is the part an estimate usually misses:

* **8 design questions** that only the maintainer could answer, asked and answered in one round.
* **Two independent reviews** after the code was written, which found *release blockers*: distress
  decisions reading net worth where they had to read spendable cash, purifier value banked outside
  the paying phase, and hidden simulation state missing from the sync hash.
* **One boot gate**, which the authoring environment could not perform at all.
* **One unrelated boot blocker** surfaced only by that gate — a duplicate inherited compatibility
  root on `GladiusCannon`, in a file this change never touched.

⭐ **So the unit of cost is not "a trait". It is "a trait, plus a review round that finds real
blockers, plus a gate on someone else's machine, plus whatever the gate wakes up."** Every
behaviour-changing phase below carries that whole package. Phases that change no behaviour do not.

---

## 2. What already exists — the plan does not credit it, and it moves the numbers

Checked in the tree, not assumed:

| already built | evidence | what it means for the plan |
|---|---|---|
| A working **match-record system with a repository and schema versioning** | `OpenRA.Mods.Cameo/CameoCareer.cs` (451 lines), `CameoCareerRecorder.cs` (172) — `CareerMatchRecord` already carries outcome, faction, map, duration, units/buildings killed and lost, resources earned/spent, assets | ⭐ **§10.6 phase 1 is an EXTENSION, not a build.** |
| ⛔ …but it is **local-player-only** | `CameoCareerRecorder.cs:39, :62, :105, :169` all gate on `world.LocalPlayer` | The extension is real work: per-player capture, plus the personality field. Not free, but not from scratch |
| Two **Cameo-original bot modules** as precedent | `CratePickupBotModule.cs` (145), `PlugSpawnerBotModuleCA.cs` (158) | A bot module in this codebase is ~150 lines, not ~600 |
| **`BotGlobalUnitBudget`**, a Cameo-original trait | `mods/cameo/ai/ai.yaml:4773`, `OpenRA.Mods.Cameo/Traits/BotGlobalUnitBudget.cs` | ⛔ **Missing from §10.2's table.** The required amendment from [`DEVIN_BRANCH_REVIEW.md`](DEVIN_BRANCH_REVIEW.md) §2, still open |

**The loaded module inventory checks out otherwise.** A direct scan of `ai.yaml` finds **18 distinct
bot trait types in 25 instances**; §10.2's table accounts for all 18 except `BotGlobalUnitBudget`.
Five `SquadManagerBotModuleCA` instances, three `LoadCargoBotModule`, two `SendUnitToAttackBotModule`.

---

## 3. The estimable part

A **session** here means one context window of focused agent work. **Maintainer touchpoints** are
counted separately because they are the actual critical path — the authoring environment cannot boot
the game, so every gate is someone else's evening.

| phase | agent sessions | maintainer | confidence |
|---|--:|---|---|
| **0. Merge #324 with its amendments** — add `BotGlobalUnitBudget` to §10.2, re-cite the `engine/` path, fold in the closed OD-A…OD-N rulings | **1** | none (docs only) | **High** |
| **1. Match logging, per-player** — extend the existing recorder off `LocalPlayer`, add personality + the §6.2 fields | **1–2** | 1 boot gate | **High** — the persistence layer exists |
| **2. `MasterAiBotModule`, observe-only** — the `BotSituation` snapshot, the per-enemy signal scan, the decision trace (§11.3.2). Decides nothing; nothing reads it | **2–3** | 1 boot gate, no playtest | **Medium-high** — largest new C#, but no behaviour to regress |

**Phases 0–2 total: 4–6 agent sessions and 2 boot gates.** They are worth committing to, because
they are pure additions, they are individually shippable, and **they are what makes everything after
them estimable** — phase 2's logs are where the thresholds come from.

---

## 4. The part that cannot honestly be estimated yet, and why

| phase | rough shape | why the number would be fiction |
|---|---|---|
| **3. `BotPersonalityController` + switching** | ~40 lines of synced C# + the decision layer | The trait is small; the **sync risk is not**. §1.1 forbids the shortcut CN uses (§1.6), so this is Cameo's own bridge with no in-family reference. Cost is dominated by a review round and a playtest, exactly like §1's datum |
| **4. Main target selection** | consumed by 5 squad managers + support powers | Needs §4.3's scoring, which §11.3.1 just amended to bounded features — **with `k` per feature unchosen**, and unchoosable before phase-2 logs |
| **5. Counter demand + hints** | 4 reader modules + `ProvidesPrerequisite` tokens | Mostly yaml, so cheap — *if* the composition table takes the tags cleanly. That is one experiment nobody has run |
| **6a. Contact memory** | CN has a working shape (§1.6) | The most estimable of the six; costs the bots nothing |
| **6b. Shroud gate on the squad scan** | the actual §0.2 fix | ⛔ **Makes the bots weaker and invalidates every tuning pass before it.** The rebuild cost lands on *balance*, not on code, and that is the single largest unknown in the whole plan |
| **6c. `ScoutBotModule`** | genuinely new behaviour | **No reference implementation in any OpenRA mod** (§1.5). Unbounded |
| **7. Offline learning** | log aggregation + priors | §11.3.5 moved the cheap first step to an offline fixed-vs-dynamic comparison. Gated on having enough logged matches — that is calendar time, not session time |

⛔ **§11.1 is the reason this section exists.** Of five research briefs, **one** came back usable.
The literature, engine-feasibility and yaml-migration questions are still open, and the document
says plainly that **§3's signal thresholds, §6.3's bandit sample sizes and §2.7's migration steps
have no external evidence behind them**. Estimating work whose inputs are explicitly missing
produces a number that will be wrong and will still be quoted.

**If a planning number is needed anyway:** phases 3–7 look like **11–16 further agent sessions,
~8 boot gates and at least 3 playtest/tuning cycles**, with 6b's balance rebuild as an open-ended
addition on top. Treat that as an order of magnitude — *tens of sessions, not hundreds and not
five* — and re-derive it after phase 2, when it can be grounded.

---

## 5. What to do next, in order

1. **Merge #324** with the `BotGlobalUnitBudget` amendment. It is docs-only, `mergeable_state:
   clean`, and every later estimate depends on §10/§11 being in the tree rather than in a branch.
2. **Phase 1**, extending the recorder that already exists. One boot gate.
3. **Phase 2**, observe-only. One boot gate, no playtest, and the logs it produces are what turn
   §4 of this document into arithmetic.
4. **Re-derive §4 from those logs before committing to phase 3.**

⚠ **One dependency worth stating plainly:** the maintainer's boot gate is the bottleneck, not agent
capacity. Four to six sessions of phase 0–2 work need only **two** gates. Phases 3–7 need roughly
eight, plus playtests. Batching behaviour changes to reduce gate count is the single biggest
schedule lever available.
