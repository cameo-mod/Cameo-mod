# AI review from the Fransbot author (2026-09-30): verified findings, response and plan

**Source:** an ordered review of the Cameo master AI stack and the `cameo-engine` bot changes by **Codex Astra from
Fransotto, the creator of Fransbot**, relayed by the maintainer on 2026-09-30 with the order to write it into the
documentation, act on it, reply, and fold it into the plan. Scope: ownership conflicts, lifecycle asymmetry, stale
state, conditional-trait transitions, modules issuing competing orders — the failure classes Fransbot development
hit most.

**Where it went:** every finding is in `AI_MASTER_PLAN.md` §3 under **"Lifecycle hardening (LC)"** with an id, an
owner and an estimate, and the plan's waves now put LC before further harvesting (the review's suggested order).
The review text itself is kept verbatim in §3 below.

---

## 1. Verification against the code (master `1ee562ec4` + #664, 2026-09-30)

"Don't trust, verify": each claim was checked against the artifact before it was accepted.

| # | Finding | Verified | Evidence | Status / plan id |
|---|---|---|---|---|
| P0a | Double repair owner (`BuildingRepairBotModule` + `…CA`) | **yes** | both loaded for `genericbot \|\| classicbot`; `RepairBuilding` is a toggle (`RepairableBuilding.RepairBuilding` removes an existing repairer); `RepairActive` only updates in `Tick` | **fixed in #664 (RV1)**: `BaseRepairBotModule` (merged, `Repairers.Contains` + in-flight window) for `genericbot`; classic runs only the CA copy; DESIGN §19.3 |
| P0b | `CaptureManagerBotModuleCA` and `CncEngineerBotModule` both select idle engineers | **yes** | both loaded for `genericbot && !easiestbot`; overlapping engineer lists; `ai.yaml` comment says *"whichever orders first wins — no double-assignment"*; `ModularBot.Tick` runs every module's `BotTick` **before** issuing queued orders and issues only `ceil(n / MinOrderQuotientPerTick)` per tick, so both can see the same engineer idle and both queue orders | **LC1** (lease contract) + **ENG** (merge the two, DESIGN §19.3) |
| P0 | `Actor.IsIdle` is not an ownership primitive | **yes** (follows from P0b) | same | **LC1** |
| P1a | EX-3: provider site vs the manager's own `checkspot` | **yes** | `McvExpansionManagerBotModule.ChooseMcvDeployLocation`: the provider replaces `expandCenter`, but `FindBadDeploySpot(bc.HasValue ? null : checkspot)` records the manager's internal spot; `activeMCVs[mcv] = checkloc` likewise; `ExpansionPlannerBotModule.McvSite` ranks by value × safety / straight-line distance with no locomotor check, and gets no rejection signal; `DriveMcvSite: true` is live on master | **LC3** (Cameo side now: path check + park after a timeout; engine feedback later) |
| P1b | `CratePickupBotModule.alreadyPursuitCrates` is never cleared | **yes** | `Add` at line 125, no `Remove` anywhere; disposed crates stay referenced | **LC2** |
| P1c | Conditionally enabled traits cached while disabled | **yes (class)** | `SiegeEvaluatorBotModule` fix present (re-resolves when empty or a cached manager is disabled) | **LC4** (sweep for the same pattern everywhere) |
| P1d | Fog honesty not end-to-end | **partly — two are ruled exceptions** | `CaptureManagerBotModuleCA.CheckCaptureTargetsForVisibility: false`, its `SafePath` scans enemies without a visibility check, `CratePickupBotModule.CheckTargetsForVisibility: false` — **all three are inside the maintainer's DESIGN §19.5 ruling** (the capture manager routes engineers around the army; a bot that lost its MCV finds a crate anywhere). `audit_fog_honesty` is a count ratchet, as stated | exceptions ruled; **LC6** adds semantic fog canaries for everything else; #664 widened the audit (`ActorsWithTrait`, visibility-switch check) |
| P1e | Ownership/lifecycle invariants | adopted | the local fixes cited (stuck-kick return, save/load reconcile, scout relinquish, beacon return) exist | **LC5** (watchdog) |
| P2 | A/B runs need an immutable fingerprint | **yes** | this session: a yaml edit landed while a batch ran from the same worktree; the harness re-reads `ai.yaml` per match; one smoke match stalled with no cause recorded | **LC7** |
| + | "SiegeEvaluator advises, the squad state machine orders" as the pattern | agreed | `SiegeEvaluatorBotModule` → `GroundUnitsAttackMoveStateCA` | used as the LC1 / ENG design pattern |

**Two further duplicates found while verifying** (DESIGN §19.3): `SupportPowerBotModule` + `SupportPowerBotASModule`
on every bot (**RV2**), and `BevManagerBotModule` vs `McvExpansionManagerBotModule`: every `McvTypes` vehicle — Japan's
nanocores included — goes through the expansion-site logic, which deploys away from existing yards, and since EX-3 to
a far field; BevManager's job is to deploy such base-building vehicles next to the base (**BEV**).

---

## 2. Reply to the reviewer

> Thank you — this is exactly the kind of review we needed, from the person who has already hit these bugs.
>
> **Everything you flagged checks out in the code**, and it is now in our plan (`AI_MASTER_PLAN.md` §3, the new
> "Lifecycle hardening" block), with your order of work adopted: hardening lands before we accelerate harvesting.
>
> * **Double repair owner:** fixed (#664). The two modules are merged into one owner that reads
>   `RepairableBuilding.Repairers` instead of `RepairActive` and remembers orders still in flight, so it can never
>   send the toggle that switches a repair off. The maintainer also made "one module per decision" binding
>   (DESIGN §19.3): duplicates from any parent are merged, never run side by side, and a new audit fails when a
>   merged module's parent file changes, so upstream fixes get ported.
> * **Engineers / `IsIdle`:** agreed, it is a live duplicate and the yaml comment is wrong. We are building the lease
>   you describe (`ActorID → owner, purpose, acquired tick, expiry`; `TryClaim` / `Release` / `IsClaimedByOther`) as a
>   small common contract, first for engineers, capture, scouts, beacons and crates, and merging the two engineer
>   modules on top of it.
> * **EX-3:** confirmed exactly as you describe — the manager records its own `checkspot`, the planner never hears the
>   rejection, and the same unreachable field comes back. Immediate fix: the planner checks the MCV's locomotor path
>   and parks a field it keeps handing out without a yard appearing. Then the candidate object with
>   accepted / unreachable / undeployable / reserved feedback, through the engine hook. "Not land-reachable" will be
>   kept as its own class, for the transport expansion we still want to harvest from Fransbot.
> * **Crate reservation:** confirmed (never cleared); it becomes `crate → collector + tick` with the release rules you list.
> * **Conditional-trait caches:** sweeping for the SiegeEvaluator pattern across the stack.
> * **Fog:** two of your three examples are deliberate — the maintainer ruled the capture manager (routing engineers
>   around the army) and MCV-recovery crate pickup the only modules allowed to see through fog (DESIGN §19.5); the
>   `SafePath` scan belongs to the first. Everything else must be honest, and we agree a count ratchet is not proof:
>   we will add your fog canaries. (The ratchet also now counts `ActorsWithTrait`, which it had missed.)
> * **Ownership watchdog and A/B fingerprinting:** adopted as written. Today's session illustrated the second one: a
>   yaml edit landed while a batch was running from the same checkout.
>
> If Fransbot already has a lease/claim implementation or an ownership watchdog you are happy to share, we would
> rather harvest it than reinvent it (with the GPLv3 header and a "Ported from" line, as for the other modules).

---

## 3. The review, verbatim

# Cameo AI review — Fransbot perspective

I reviewed the current Cameo master AI stack together with the `cameo-engine` bot changes, focusing specifically on the failure classes that have caused the most trouble during Fransbot development: ownership conflicts, lifecycle asymmetry, stale state, conditional-trait transitions, and modules issuing competing orders.

## Overall assessment

The direction of the Cameo AI is strong. The individual systems are generally not the main concern.

The largest technical risk I see is the **control plane between the systems**.

Cameo's own current master plan states the correct architectural rule: **one owner per decision**. I strongly agree with that rule. My main recommendation is to make it an enforced runtime/design contract rather than only an architectural convention.

This matters increasingly as Cameo combines OpenRA, CA, RV, CN, Cameo and Fransbot modules. The current plan reports 53 bot-module types / 76 instances, with substantially more harvesting planned.

I would therefore consider a short **AI lifecycle/ownership hardening phase before adding many more behavioural modules**.

## P0 — Multiple modules can own the same actor or decision

The clearest known example is already documented by Cameo: both `BuildingRepairBotModule` and `BuildingRepairBotModuleCA` are active for the same bots. The master plan itself records this as a bug because the repair toggle can cancel itself.

This should be treated as a representative architecture bug rather than an isolated repair bug.

There is a second concrete example around engineers.

`CaptureManagerBotModuleCA` and `CncEngineerBotModule` can both select idle engineers. The YAML comment currently says that because both select idle engineers, "whichever orders first wins".

That assumption is unsafe.

`ModularBot.QueueOrder()` only enqueues an order. All enabled bot modules execute their BotTick before the queued orders are issued. Therefore two modules may both observe the same actor as idle during the same simulation tick and both enqueue orders for it.

In addition, `MinOrderQuotientPerTick` means queued orders can survive into following ticks.

In other words:

**Actor.IsIdle is not an ownership primitive.**

I recommend a common lightweight ownership/lease mechanism for long-lived AI assignments:

`ActorID -> owner / purpose / acquired tick / expiry or heartbeat`

Examples of owners could be:

`Squad`, `Scout`, `Beacon`, `Capture`, `Engineer`, `Crate`, `MCVExpansion`, `FransMission`.

Modules should use something equivalent to `TryClaim`, `Release`, and `IsClaimedByOther` before issuing long-lived assignments.

I would not initially turn this into a huge central scheduler. A small common reservation contract would catch most of the dangerous cases without replacing the existing commanders.

## P1 — EX-3 MCV expansion has a failure-lifecycle mismatch

The new `IBotMcvExpansionSiteProvider` seam is a good idea, but I believe its current contract is too narrow.

`ExpansionPlannerBotModule.McvSite()` chooses the highest-scoring far resource field from `LastScores`. The ranking uses resource value, safety and geometric MCV distance, but does not prove that the MCV's locomotor can actually reach that field.

The engine `McvExpansionManagerBotModule` then replaces its own `expandCenter` with the provider's site.

However, the manager's `checkspot` remains the one belonging to its original internally selected site.

If the external provider site cannot be reached or cannot produce a deployable cell:

1. the MCV manager rejects the deployment;
2. its failure bookkeeping records the internal `checkspot`, not the provider field;
3. the ExpansionPlanner receives no rejection;
4. the planner can return exactly the same external field on the next request;
5. changing the MCV manager between CheckResource / CheckBase does not necessarily escape the problem because the external provider overrides `expandCenter` again.

There is a related multi-MCV issue: `activeMCVs` records the internal check location, so duplicate-site suppression may not describe the site actually selected by the provider.

This is especially important before importing Fransbot's island/transport expansion. "Not land-reachable by this MCV" should eventually be a useful classification — potentially a transport objective — rather than either globally bad or retried indefinitely.

I would expand the interface from a bare `CPos?` toward a candidate object containing at least:

- site/location;
- stable candidate/field identity;
- candidate kind;
- score;
- optionally reachability requirements.

Ideally the MCV manager should be able to report `accepted`, `unreachable`, `undeployable`, `reserved`, etc. back to the provider.

A smaller immediate fix would be to ensure the provider site is path/deploy validated and parked for a bounded time after rejection.

## P1 — CratePickup has a stale reservation

`CratePickupBotModule` keeps:

`readonly List<Actor> alreadyPursuitCrates`

A crate is added when a collector is ordered toward it, but I cannot find a corresponding removal path.

This means that if the collector dies, is reassigned, becomes stuck, or its queued Move loses to another module, that crate can remain permanently marked as already pursued.

Disposed crate actors also remain referenced by the list.

I would change this into a reservation:

`crate -> collector + assignedTick`

and clear it when:

- crate disappears;
- collector disappears;
- collector becomes owned by another role;
- collector becomes idle after a grace period;
- assignment times out.

This is exactly the class of stale-state bug that tends to become rare, long-match "AI just stopped doing X" behaviour.

## P1 — Audit all caches of conditionally enabled traits

Cameo has already found a very important example of this in `SiegeEvaluatorBotModule`.

The evaluator resolved enabled personality squad managers before the personality latch. The result was an empty cached array that stayed empty for the match. The recent fix correctly re-resolves the set when it is empty or a cached manager becomes disabled.

I would treat this as a bug class and search the AI stack for every case that combines:

- `TraitsImplementing<T>()`;
- filtering by `!IsTraitDisabled` / enabled state;
- storing the result;
- personality/difficulty/condition-dependent traits.

Either cache all instances and check enabled state when consuming them, or explicitly refresh on lifecycle transitions.

The same principle applies to provider selection: if only one provider is allowed to own a decision, assert that rather than relying on enumeration order.

## P1 — Fog honesty is not yet end-to-end

The core fog-memory work is good, but some side modules still leak world information.

Current `ai.yaml` has:

`CaptureManagerBotModuleCA.CheckCaptureTargetsForVisibility: false`

and:

`CratePickupBotModule.CheckTargetsForVisibility: false`

so those modules can react to targets/crates the player has not observed.

There is also a subtler CaptureManager case: even with capture-target visibility enabled, `SafePath` evaluates enemy actors around path cells without a visibility check. Invisible enemies can therefore influence the chosen path.

The existing `audit_fog_honesty.py` is useful, but its own documentation correctly says that it is a **count ratchet**, not a semantic proof. A PASS means that no new global-enumeration sites were added, not that all existing sites are fog-honest.

I suggest adding a few runtime "fog canary" tests where an unseen actor/crate exists and asserting that it cannot change bot behaviour until it has actually been observed.

## P1 — Add ownership/lifecycle invariants

Cameo already contains local fixes for stranded actors:

- squad members kicked for being stuck are returned to the idle pool;
- save/load reconciles actors present in `activeUnits` but in neither a squad nor the idle pool;
- scouts relinquish ownership when a squad claims them;
- scouts and beacon responders return actors to the shared idle pool.

These are good fixes.

I would generalise their underlying rule.

Periodically, in debug/test builds, calculate an ownership snapshot for every own orderable combat actor and assert:

- no actor has more than one exclusive long-lived owner;
- every actor marked active by a subsystem has a valid owner;
- every squad member belongs to exactly one squad manager;
- no dead/disposed actor remains reserved;
- a released actor eventually becomes visible to the normal allocation pool.

This would have detected several bugs that were otherwise discovered only through long runtime matches.

## P2 — Protect the experiment pipeline as strongly as the AI

Cameo's A/B infrastructure is impressive, but its history also shows why match results cannot be the only acceptance mechanism.

Recent examples include:

- MasterAI profiling no enemies because test-map bot players were `Playable: false`;
- stale binary/rules windows;
- YAML changes affecting the next match in a running batch;
- outcome interpretation problems;
- test/document counters drifting after merges.

I would make every batch record and verify an immutable fingerprint of:

- mod commit;
- engine commit;
- resolved AI YAML/rules hash;
- map hash;
- bot type/personality configuration.

If any of these changes while a batch is running, invalidate/abort that batch automatically.

A/B then measures behaviour and strength, while invariants establish correctness.

## Things I think Cameo is doing very well

The `SiegeEvaluator` architecture is a good pattern: it advises `Advance / StandOff / Retreat`, while the squad state machine remains the single order authority. I would reuse this pattern widely.

The recent FransCommanderCore change is also exactly the right solution. Fransbot's stance logic now yields when a CA squad manager is enabled instead of trying to make both systems coexist as order authorities.

The scout and beacon-response lifecycle is significantly stronger than some of the older modules because ownership and return-to-pool are explicit.

The current UnitBuilder configuration also avoids the old `IdleBaseUnitsMaximum < required squad size` production/formation deadlock.

Finally, the amount of telemetry, fight reporting, module auditing and A/B infrastructure is a major strength. I would keep that work, but add state-machine and ownership assertions underneath it.

## Suggested order of work

1. Fix the known double repair owner.
2. Introduce a small common unit-claim/lease contract and use it first for Engineer/Capture/Scout/Beacon/Crate.
3. Fix CratePickup reservation cleanup.
4. Add failure feedback/reachability ownership to the EX-3 MCV provider seam.
5. Sweep conditional-trait caches for stale enabled-instance sets.
6. Add unit-ownership and stale-reservation watchdogs.
7. Add semantic fog-canary tests.
8. Harden A/B run fingerprinting.
9. Only then accelerate the remaining module harvesting.

My main takeaway from Fransbot is that once an RTS bot becomes modular enough, **decision quality stops being the only hard problem**. The difficult bugs increasingly occur at the boundaries: who owns an actor, when ownership begins, how it ends, what happens when an objective disappears, and whether a cached view of another subsystem is still valid.

Cameo already has most of the design vocabulary needed to solve this. I would now make those lifecycle rules first-class contracts before adding substantially more brains to the Frankenstein stack.
