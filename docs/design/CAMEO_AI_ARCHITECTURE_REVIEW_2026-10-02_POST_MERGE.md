# Cameo AI Architecture Review — Post-Merge, 2026-10-02

**Reviewed repository:** `cameo-mod/Cameo-mod`  
**Reviewed master:** `9a2f6667c6a7440804580a6f9e8da02c5646a106`  
**Coalition implementation commit:** `9a8a39c46e029640213776a538db642585122d5f`  
**Purpose:** architecture review after the large 2026-10-02 merge, with emphasis on ownership, lifecycle, coalition/team coordination, and interoperability boundaries.

## Executive summary

The large merge materially improves Cameo's AI architecture. The final review was refreshed after PR #773 landed; #773 is documentation-only (field-economy/build-order specifications), so it does not change the runtime findings below. The strongest direction is still the same one that emerged from the previous Fransbot review: **one authority per decision, one owner per actor, advisors/providers instead of competing order emitters**.

The merge consolidates several previously overlapping mechanics behind provider seams or single owners: spacing, army-first building pressure, harvester field limits, formation/concave deployment, scale targets, production width, stealth doctrine, and the new TC-3 coalition layer. This is a healthier direction than adding more independent brains.

The most important new architectural issue is that TC-3 now exists in runtime code, not just design. `IBotCoalition`, `CoalitionFold`, rescue elections, coalition target bias and sector bias are real. That makes several assumptions in the TC-3 design load-bearing:

1. all team members are assumed to fold the same broadcast set;
2. broadcasts are assumed to remain live and current;
3. `ClientIndex` is assumed to be a unique participant identity;
4. a rescue responder is treated as "free" without a coalition-level capacity reservation.

The current code does not fully establish those assumptions. They should be hardened before TC-3 becomes a general team-coordination foundation.

A second immediate process issue is that the generated architecture evidence is now stale. `AI_ARCH_COVERAGE.md` and `AI_MODULE_MAP.md` predate the large merge and do not describe the current master. The human-authored `AI_ARCHITECTURE.md` has moved ahead of the generated audit that it says is authoritative.

Finally, the proposed **SCG / Dispatch** interoperability layer should **not** be merged into Cameo's TC-3 coalition brain. They solve different problems. TC-3 is an internal same-AI coordination mechanism. SCG should be a tiny neutral relay protocol between independent AI implementations, with no strategic authority of its own.

---

## 1. Current architecture after the merge

The current stack is best understood as:

```text
OBSERVE
  BotSituation / FogMemory / TacticalMap / Threat / Influence
        |
        v
SYNTHESIZE
  MasterAi / UtilityAxes / Director / TeamBroadcast / CoalitionFold
        |
        v
PLAN / ADVISE
  ExpansionPlanner / DefenseCoverage / Siege / Formation / ScaleTargets /
  StealthDoctrine / ProductionWidth / placement advisors
        |
        v
ARBITRATE
  BaseBuilder / UnitBuilder / SquadManager / ownership leases / action budget
        |
        v
EXECUTE
  squad states / engineer / repair / harvest / deploy / transport etc.
        |
        v
VERIFY / FEEDBACK
  Mission outcomes / telemetry / watchdogs / A-B tooling
```

This remains a sound overall shape. The important architectural property is that most new intelligence is entering through **read-only provider seams** while existing execution owners retain their orders.

Examples from the post-merge code:

- `SpacingAdvisorBotModule` implements `IBotPlacementAdvisor`; the queue manager still owns building placement.
- `AssaultFormationBotModule` supplies `IBotAssaultFormation`; the squad state still owns movement orders.
- `ScaleTargetsBotModule` publishes `IBotScaleTargets`; builders and harvesters consume the target values.
- `ParallelProductionBotModule` publishes `IBotProductionWidth`; it does not become another production owner.
- `StealthDoctrineBotModule` publishes `IBotStealthDoctrine`; stealth squad states remain the executors.
- `CoalitionFold` publishes `CoalitionDirective`; existing Master/Squad/Expansion owners consume it as bias.

That is the correct general pattern.

---

## 2. What the merge improved

### 2.1 One authority per decision is becoming real rather than aspirational

The merge explicitly consolidates several duplicate or overlapping mechanisms. This is important because the historical failure mode in modular RTS AI is rarely "not enough modules"; it is usually **multiple modules believing they own the same unit or decision**.

The current design increasingly uses this rule:

> Providers may influence a decision. Exactly one existing owner emits the resulting orders.

The formation merge is a good example. Rather than keeping a separate fan-out brain and a separate concave brain that both move the same squad, the merged design puts the geometry behind one provider/deploy path.

The same direction is visible in spacing, harvester spread and army-first building policy.

### 2.2 TC-3 preserves local execution ownership

`CoalitionDirective` does not issue orders itself. Its outputs are read by:

- `MasterAiBotModule` for coalition target bias;
- `SquadManagerBotModuleCA` for coalition target preference and rescue election;
- `ExpansionPlannerBotModule` for sector scoring.

That is much safer than adding a second team-level unit commander.

### 2.3 The provider vocabulary is becoming a useful architectural spine

The growing set of `IBot*` provider seams is increasingly useful because it gives Cameo stable places to harvest ideas from donor AIs without importing their entire control loops.

This is the right way to reuse useful Fransbot/CN/CA concepts: **import the capability or advice, not a second brain**.

---

## 3. High-priority findings

### 3.1 The generated architecture audit is stale after the merge

This is currently the first thing I would fix, because it affects confidence in every later architecture claim.

`docs/design/AI_ARCH_COVERAGE.md` was last updated at commit `2fbd9a811fef` on 2026-10-02 10:21 UTC.  
`docs/design/AI_MODULE_MAP.md` was last updated much earlier, at `85c95b257683` on 2026-10-02 06:39 UTC.  
The coalition implementation landed at `9a8a39c46e02` at 18:17 UTC, followed by the large merge ending at current master `65fbcf70fdd6`.

The stale generated documents demonstrate the mismatch directly: their provider/consumer maps do not include the new `IBotCoalition` seam even though current `MasterAiBotModule` implements it and current consumers read it.

This means the repository currently has a process contradiction:

- `AI_ARCHITECTURE.md` says the generated module map is authoritative;
- the generated map no longer represents current master.

**Recommendation:** regenerate both audits on current master and make staleness itself a CI failure whenever files under the AI architecture surface change.

Suggested gate:

```text
python tools/ai/ai_module_map.py --check
python tools/ai/ai_arch_audit.py --check
```

The check should fail if the generated docs do not match the current source tree.

---

### 3.2 TC-3's "identical directive" invariant is not currently guaranteed

`CoalitionFold` is deterministic **for a given input set**. The unit tests correctly prove that arithmetic property.

The runtime assumption is stronger: every allied bot is documented as computing the same `CoalitionDirective` because it sees the same broadcasts.

Current `MasterAiBotModule` deliberately staggers snapshot timing:

```csharp
nextSnapshotTick = Math.Abs(player.ClientIndex * 37) % Math.Max(1, info.SnapshotInterval);
```

Each bot then does:

```csharp
broadcast = new TeamBroadcast(...current own snapshot...);
coalition = CoalitionFold.Compute(broadcast, TeamBlackboard.CollectBroadcasts(player));
```

Therefore Bot A may fold:

```text
A generation 20
B generation 19
C generation 20
```

while Bot B later folds:

```text
A generation 20
B generation 20
C generation 20
```

The function is deterministic, but the input sets are not guaranteed to represent the same generation. The test suite currently swaps `own`/`allies` while keeping identical broadcast values; it does not test staggered publication.

This matters because `MainTarget`, `Phase`, rescue elections and sector data can differ transiently between team members.

There are two valid architectural choices:

**A. Require identical directives.**  
Then add a team epoch / generation barrier or double buffer. Bots publish generation N while consumers fold the last complete generation N-1.

**B. Accept eventual consistency.**  
Then remove the stronger "identical directive" invariant from docs/tests and make consumers robust to short disagreement.

Either is reasonable. The current code/documentation combination claims A while implementing something closer to B.

---

### 3.3 Team broadcasts have a timestamp but no real liveness contract

`TeamBroadcast` carries `SnapshotTick`, but most team consumers do not use it for freshness. `TeamBlackboard.CollectBroadcasts` currently selects:

```csharp
p != me && p.IsBot && me.IsAlliedWith(p)
```

and returns the enabled `IBotTeamMember.Broadcast` without checking:

- `WinState`;
- maximum broadcast age;
- `SnapshotTick == 0` except in isolated consumers;
- whether the publisher has stopped updating.

OpenRA keeps the `Player` / `PlayerActor` object after a player has lost. A last published intent can therefore remain readable after the publisher is no longer an active coalition member.

Potential effects when team switches are armed:

- a dead ally's old `Climax` can continue affecting synchronized launch behavior;
- an old defence request can remain eligible;
- an old expansion claim can still cause another bot to yield a field;
- an old target vote can remain in a coalition fold;
- old spawn/sector identity can survive longer than intended.

**Recommendation:** define one liveness rule centrally and apply it before any team fold.

For example:

```text
valid broadcast =
  SnapshotTick > 0
  AND publisher.WinState == Undefined
  AND now - SnapshotTick <= TeamBroadcastMaxAge
```

Do not make each consumer reinvent this filter.

---

### 3.4 Coalition rescue can elect the same responder for multiple simultaneous requests

`CoalitionFold.Compute` builds `freePool` once. For each requester it selects the nearest responder from that same list, but does not remove or reserve the chosen responder.

With two allies asking for help, one nearby army can therefore be elected for both requests in the same directive.

That contradicts the semantic phrase "nearest free ally" unless one responder is intentionally allowed to own several simultaneous rescue obligations.

**Recommendation:** make capacity explicit.

The simple first rule is:

```text
one responder -> at most one rescue assignment per fold
```

Remove the selected responder from `freePool` after assignment. A later version can expose capacity if multi-rescue is ever desirable.

This is the coalition-level equivalent of the actor-ownership problem already solved lower in the stack: assigning a resource twice is not fixed merely because the lower-level unit leases are correct.

---

### 3.5 `ClientIndex` is too weak as a universal coalition identity

TC-2/TC-3 currently use `ClientIndex` for precedence, rescue identity and sector anchors.

That is convenient for normal lobby bots, but OpenRA's `Player` implementation gives map-side players the host/admin client index (`TODO: fix this` in engine code). Multiple map-side bots can therefore share a `ClientIndex`.

Consequences for a generalized team layer can include:

- precedence collisions;
- rescue requester/responder ambiguity;
- sector-anchor grouping collapsing two participants into one;
- identity instability if the mechanism is reused outside the tested lobby harness.

**Recommendation:** use a stable player identity for coalition protocol identity. `Player.InternalName` / a match-local player-slot key is a better basis than `ClientIndex`. Keep `ClientIndex` only where lobby ordering itself is the desired input.

This becomes especially important for SCG interoperability, where participants may include Cameo, Fransbot, stock AI adapters and map-side bots.

---

## 4. Lifecycle finding still open

The current `AI_DATAFLOW.md` still records dangling mission outcomes at match end: open attempts can finish the match without a terminal state.

That remains a significant architecture issue because mission records are increasingly being treated as feedback and future learning evidence.

The contract should remain simple:

> every COMMIT / attempt must reach exactly one terminal outcome, including match teardown.

At world/game teardown, still-open attempts should emit a bounded terminal record such as:

```text
RELEASED reason=match_end
```

A match ending is not mission success or failure by itself, but it must close the lifecycle.

---

## 5. MasterAiBotModule: keep one strategic owner, reduce internal coupling

The merge adds still more provider roles to `MasterAiBotModule`, including `IBotCoalition` on top of target selection, mission publishing/outcomes, fog information, threat routing, prediction, defence memory, personality leads, utility axes, Director and team broadcast.

I do **not** recommend splitting this into several competing strategic brains. That would reverse the architecture's strongest improvement.

I do recommend continuing to extract pure internal components behind the one strategic owner:

```text
MasterAiBotModule
  -> SituationBuilder
  -> TargetEvaluator
  -> MissionLifecycle
  -> Utility/Director state
  -> TeamPublisher
  -> CoalitionFold adapter
```

The external ownership stays one. The internal code surface becomes easier to reason about and test.

---

## 6. TC-3 and SCG should be separate layers

The newly merged TC-3 should be treated as **Cameo's internal/same-family team-coordination system**.

It knows Cameo concepts:

- Director tension/phase;
- Cameo main-target scoring;
- expansion claims;
- army centroid;
- sector bias;
- Cameo squad rescue consumers.

That is useful when several Cameo bots play together.

It is not a suitable interoperability contract for unrelated AIs, because Fransbot, stock AI and future bots do not share those internal concepts.

The proposed **SCG (Supreme Coalition General) / Dispatch** layer therefore should not replace TC-3 and should not be implemented as another `CoalitionFold` consumer.

The clean boundary is:

```text
Cameo internal logic / TC-3
          |
      SCG adapter
          |
   neutral Dispatch
          |
         SCG
          |
   neutral Dispatch
          |
      SCG adapter
          |
Fransbot / stock AI / other AI internal logic
```

Cameo-to-Cameo may continue to communicate more efficiently through TC-3. Fransbot-to-Fransbot may use richer Fransbot-specific coordination. Only coalition-relevant intent/request/commitment needs to cross the simple SCG boundary.

---

## 7. Recommended order of work

1. **Regenerate architecture evidence on current master.** Do not rely on the pre-merge module map/audit.
2. **Add coalition broadcast liveness.** Alive + age + non-empty filter in one place.
3. **Decide the TC-3 consistency model.** Epoch/barrier for identical directives, or explicitly eventual consistency.
4. **Fix/define rescue capacity.** Prevent accidental multi-assignment of one responder.
5. **Replace universal use of ClientIndex as coalition identity** before expanding beyond the current lobby harness.
6. **Close mission attempts at match end.** Preserve the lifecycle contract before using mission archives as learning evidence.
7. **Keep SCG interoperability outside TC-3.** Add a small adapter rather than another strategic brain.

---

## 8. Overall assessment

The merge is a net architectural improvement. Cameo is moving away from overlapping modules toward a provider/advisor architecture with clearer ownership, and the consolidation work around formation, spacing, production and team behavior is exactly the right direction.

The main risk has shifted. Earlier the danger was primarily **too many modules owning the same action**. After this merge, the higher-level risk is **assuming distributed team state is more coherent than it really is**.

That is a much better problem to have, because it can be fixed with explicit identity, liveness and generation contracts without rewriting the AI.

The strongest recommendation is therefore:

> Keep Cameo's internal team intelligence rich, but make its boundaries explicit. Use liveness + generation contracts inside TC-3, and use the separate, deliberately simple SCG/Dispatch protocol for interoperability with other AI architectures.

