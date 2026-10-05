# CAMEO — The Ultimate Crossover RTS Design Framework

**Version 8 | Strategic design guide | 2026-10-05**

> Cameo should make a player think, scout, commit, adapt, and tell a story about the match afterwards. Its crossover roster is the material for those decisions, not the design by itself.

This is a product vision and translation guide. It proposes priorities; it does not amend binding rules. When a recommendation touches combat, balance, AI, content, or workflow, the current law in [DESIGN.md](../DESIGN.md), the current state in [ROADMAP.md](ROADMAP.md), and the technical plans linked below take precedence. Dates, counts, release promises, and implementation claims are intentionally omitted unless kept current by those sources.

## 1. The design promise

Cameo's opportunity is to put armies with different histories and doctrines into one readable battlefield. The player should feel both recognition ("that faction does what its source universe taught me to expect") and discovery ("this combination makes me reconsider my plan"). The game earns its breadth by making the interaction among its factions interesting, not by maximizing the number of units on a selection screen.

A strong match repeatedly asks questions at three timescales:

- **Immediate:** Which units should retreat, focus fire, screen, flank, or spend an ability?
- **Operational:** Which front, route, expansion, or objective deserves the next force?
- **Strategic:** Which economy, technology, production mix, or alliance posture will still work after the opponent adapts?

A decision is useful when the player has more than one plausible option, can understand the costs and risks, and can later tell why the choice mattered. That is the practical Cameo reading of Sid Meier's "interesting decisions" model, not a mandate to make every click consequential or every match unpredictable. [GDC: Sid Meier's Interesting Decisions](https://www.gdcvault.com/play/1015756/interesting)

The intended emotional arc is **read -> prepare -> contest -> adapt -> resolve**. A match may be fast, sprawling, cooperative, or scripted, but each phase should offer clear goals, visible risks, and meaningful ways to recover or press an advantage.

## 2. Coherence rules for a crossover game

### 2.1 Shared language, distinct doctrine

Every faction needs to share enough vocabulary for players to transfer skill: economy, production, scouting, army roles, threat, retreat, and map control. Faction identity then changes how that vocabulary is expressed: what resources it values, how it reaches a fight, which roles it combines, how it denies information, and what weakness an opponent can exploit.

Use a common role grammar where possible, but never assume two units with the same role are interchangeable. A scout can be cheap and expendable, a stealth infiltrator, a flying observer, or a fast combat raider. Their identity comes from the choices and exposure they create, not just a label.

### 2.2 Preserve the weapon and armour contract

Cameo's combat identity must fit its existing damage model. The binding weapon rule is one main damage warhead, resolved through the three-way weapon structure; special effects and conditions use the documented exceptions. The armour system preserves meaningful matchup differences while every damage profile remains applicable across the armour set. Design counters through measured efficiency, range, role, mobility, information, positioning, and cost, rather than inventing immunity classes or adding bespoke damage axes. See [DESIGN.md, section 11b](../DESIGN.md) and [WEAPON_3WAY_SPLIT.md](WEAPON_3WAY_SPLIT.md).

There is no universal kill-ratio target. A 3:1 or 1.5:1 ratio is not a design law unless Cameo's balance pipeline and matchup evidence establish it for the actual unit, target, cost, delivery method, and scenario. A counter should create a reason to adapt and a counter-window in which the opponent can respond.

### 2.3 Balance is a reproducible pipeline

Balance means that a strategic choice has a defensible cost and effect across its intended contexts; it does not mean identical statistics or mirror rosters. Use the project balance pipeline, derived ledgers, and controlled comparisons. Never hand-edit prices or damage values to make a design story come true. Keep assumptions, inputs, formulas, and measured outcomes together. A proposed mechanic is not balanced because its description sounds fair.

### 2.4 Information and agency are part of fairness

A player may act only on public, allied, or personally observed information. Bots have the same visibility contract, with only the narrow documented exceptions. Information can be stale, partial, deceptive, or expensive to acquire, but the game should make those properties legible. Never let a generic bot read hidden positions, types, or health to create the feeling of smart play. See [DESIGN.md, section 19.5](../DESIGN.md) and `tools/ai/audit_fog_honesty.py`.

The unit's owner should also be legible. One module owns a unit at a time; other modules coordinate through providers and orders. This keeps both player-facing behaviour and synchronized multiplayer behaviour understandable. Bots act through orders, not direct actor mutation. See [DESIGN.md, sections 19.6 and 19.8](../DESIGN.md).

### 2.5 Change behaviour behind reversible experiments

New behaviour starts opt-in and scoped to the intended generic bot or game mode. Preserve the existing reference behaviour when the switch is off, and compare whole increments using the established workflow. A switch is not a substitute for a default, a field declaration, a test, a multiplayer check, or a readable outcome. See [WORKFLOW.md](../WORKFLOW.md) and [AI_MASTER_PLAN.md](AI_MASTER_PLAN.md).

## 3. Lessons from RTS families and current projects

These are design patterns to test against Cameo's needs, not claims that one game has solved the genre.

| Reference | Useful pattern | Cameo translation and boundary |
|---|---|---|
| **StarCraft / Brood War** | Strongly asymmetric toolkits make scouting and response central. Information matters only when it changes a build, route, target, or army composition. Blizzard's scouting guide links enemy observation to counter-force and expansion denial. | Keep asymmetry legible through unit roles and tech paths; make scouting reveal a decision, not a checklist. Bots consume only their visible memory. [Blizzard scouting guide](https://news.blizzard.com/en-us/article/4488316/game-guide-scouting) |
| **Age of Empires II** | A shared unit and technology language can support many civilizations through bonuses, technology access, and distinctive units. This gives players transfer learning while preserving matchup preparation. | Prefer recognizable shared roles with faction-specific availability and economics. Document missing options as a real weakness, not a hidden stat penalty. [Age of Empires II: civilizations and modes](https://www.ageofempires.com/learn-to-play/civilizations-game-modes-aoe2/) |
| **Warcraft III** | Heroes and progression focus attention and create memorable tactical spikes. They can also crowd out army and map decisions if every faction becomes a hero-management game. | Reserve high-attention progression for a clearly defined Cameo role or mode. Keep faction identity distributed across the force; do not gate unit abilities by bot tier. [Blizzard: Warcraft III hero basics](https://classic.battle.net/war3/basics/heroes.shtml) |
| **Company of Heroes** | Terrain, cover, suppression, retreats, and destruction can turn position into a combat decision. Replays can help players learn from command sequences. | Use map affordances and mission rules where current engine rules support them; do not promise a new universal cover system without an engine design and separate implementation path. Make losses and suppression readable. [Relic patch archive](https://help.relic.com/hc/en-us/articles/39307744455571-Company-of-Heroes-3-Patch-Notes-Archive), [Relic replays](https://help.relic.com/hc/en-us/articles/39572125196947-Introducing-Replays) |
| **Supreme Commander / Beyond All Reason** | Strategic zoom and long-range command tools let the player reason about huge fronts; reclaimable wrecks turn a battle's aftermath into an economic choice. | Improve overview, order feedback, and post-battle resource choices before adding more unit micro. Large-scale automation must remain predictable and inspectable. [Supreme Commander manual](https://manuals.thqnordic.com/SupremeCommander/SupremeCommander_PC_Manual_EN.pdf), [BAR commands](https://www.beyondallreason.info/commands-20), [BAR reclaim guide](https://www.beyondallreason.info/guide/reclaim-resurrect-repair) |
| **Red Alert 2 / Generals** | Strong silhouettes, factions with clear strategic signatures, and discrete special powers create fast recognition and dramatic commitments. Powers work best when warnings and response windows are clear. | Make powers create contestable timing and map choices. Avoid stacking so many emergency abilities that normal production and positioning stop mattering. [Generals manual listing](https://www.cncnz.com/downloads/generals-downloads/), [Red Alert 2 instruction manual](https://static.wikia.nocookie.net/cnc_gamepedia_en/images/9/94/Command_%26_Conquer_-_Red_Alert_2.pdf/revision/latest) |
| **Mental Omega, Combined Arms, Romanov's Vengeance** | Long-lived C&C mods show the appeal of campaign arcs, subfaction identity, and a strong link between faction play and fiction. They also show that content breadth needs sustained balance and mission craft. | Treat C&C communities as collaborators and references, not a feature checklist. Establish each Cameo faction's playable identity before multiplying variants. [Mental Omega](https://mentalomega.com/index.php?page=mod), [Combined Arms](https://www.moddb.com/mods/command-conquer-combined-arms), [Romanov's Vengeance](https://github.com/MustaphaTR/Romanovs-Vengeance) |
| **Recent RTS releases** | Stormgate's service changes underline the risk of putting the entire experience behind external online services. ZeroSpace foregrounds co-op and a shared campaign map; Tempest Rising connects its Veti faction campaign to skirmish and multiplayer. | Prioritize durable local play, replayable scenarios, and portable logs. Teach a faction through authored missions, then let the same identity work in skirmish and multiplayer. Treat each mode as a complete experience, not a checkbox. [Stormgate service notice](https://steamcommunity.com/app/2012510/allnews/), [ZeroSpace](https://playzerospace.com/), [Tempest Rising](https://tempestrising.com/), [Veti expansion notice](https://steamcommunity.com/app/1486920/allnews/?l=english), [Tempest Rising support updates](https://support.saber.games/hc/en/tempest-rising/articles/march-10-2026-patch-notes-21) |

The table's recommendations are design inferences from the cited material. They do not imply that Cameo should reproduce another game's mechanics, business model, faction count, or server architecture.

## 4. The player loop

### 4.1 Start with purpose

Before a match, the player should know the map's strategic shape, the victory condition, the opponent or team structure, and what the selected faction does unusually well. A faction card should communicate its opening strengths, main constraint, likely counterplay, and one example composition. Do not expose hidden lobby information during play.

### 4.2 Make the opening teach the economy

The opening should present at least two viable investments: secure income or map position, protect production or scout, tech now or field a response. Tutorials should teach transferable actions through a short match, not a long control lecture. The first opponent should demonstrate recognizable signals and allow recovery after a mistaken read.

### 4.3 Give the midgame competing fronts

The map should make a player choose where to spend movement and production. Expansion sites, chokepoints, side objectives, resource denial, and scouting posts should differ in exposure and payoff. One uncontested optimal route is not strategic variety; a map full of equal-value points is not meaningful choice either.

### 4.4 Resolve through adaptation, not a hidden rubber band

A leading player should gain initiative, not an automatic win. A losing player needs an actionable way to trade space for time, deny a resource, exploit a counter-window, use a reserve, or complete a side objective. The game may make a late comeback difficult; it should not conceal an arbitrary bonus or let an opponent recover with invisible resources. Campaign and co-op directors can vary authored pressure, but players should see the phase, signal, and likely consequence.

### 4.5 End with an explanation

The result screen should answer: which fronts mattered, what each side built and lost, which engagements changed the balance, and what objective or economy swing ended the match. Give the player a replay or match log where the current engine supports it. A loss should suggest a useful next experiment, not just show a winner.

## 5. Faction and crossover design

### 5.1 A faction is a strategic promise

Write a one-page identity brief for every playable faction and important subfaction:

1. What does it want the player to do differently in the first five minutes?
2. How does it acquire and spend resources?
3. How does it scout, move, and choose a fight?
4. Which roles are efficient, missing, delayed, or unusually risky?
5. What visible signal tells an opponent which plan is underway?
6. What can the opponent do about it, and when is that response still affordable?
7. What map, mode, or matchup makes the identity especially interesting?

A signature mechanic should answer at least one of those questions and create a tradeoff. A bonus that only raises a unit's damage or health is a weak identity unless it changes a real timing, composition, route, or risk decision.

### 5.2 Crossover should create combinations, not universal answers

Crossover is strongest when a borrowed mechanic complements a Cameo role while preserving its original constraints. Put the mechanic where it can be explained and tested: content pack, provider, or common rule, depending on its scope. Avoid combinations that erase another faction's core weakness, bypass counterplay, or require lore knowledge to read.

Use a matchup map to record each faction's best pressure, response options, and blind spots. Its purpose is to find dead ends and dominant answers, not to promise equal win rates before measurement. Keep every weapon inside the existing common damage/armour law and every balance claim inside the pipeline.

### 5.3 Control roster growth

New units earn a place by adding a decision the roster cannot already express. Reuse visual or mechanical variants only when they teach a meaningful faction difference. Before adding a unit, ask whether the same choice could be made more clearly by a role, prerequisite, ability, upgrade, squad policy, map object, or mission rule. This is how a crossover roster stays learnable.

## 6. Combat, map, and economy

### 6.1 Combat offers counterplay at several layers

A tactical answer can be a counter-unit, a retreat, a target switch, terrain, range, vision, formation, production timing, or objective denial. The opponent should have an observable clue and enough time or resources to respond. Hard counters may create strong efficiency, but no unit should become categorically immune to all ordinary damage under Cameo's current weapon contract.

Keep tactical abilities few enough to read under pressure. Distinguish a faction's signature ability from routine unit commands. Feedback should show range, cooldown, target, result, and any status or damage-over-time effect in consistent vocabulary.

### 6.2 Maps are economic and informational systems

A good map distributes income, travel time, vision, and safe production across competing lanes. The player should be able to infer what a resource site buys and what it costs to hold. Terrain may channel or expose forces, but should not create a single unavoidable death choke without an alternate route, objective, or defensive response.

For each map, authors should list likely first contact, first expansion choices, safe and exposed routes, information-denial locations, comeback routes, and the reason to leave the base. Map review should test more than symmetry: faction mobility, spawn knowledge, and team side all change the value of space.

### 6.3 The economy supports a readable tempo curve

Economy choices should compete with army readiness. A greedy player trades current safety for future throughput; a defensive investment buys time but gives up initiative; tech exposes a timing before it pays. Avoid automatic exponential advantages that make the first successful expansion impossible to contest. Also avoid catch-up income that invalidates earned map control.

Cameo's resource, build, and price questions should use the existing extraction and balance pipeline. This framework sets no new prices, multipliers, or universal match-duration target.

## 7. AI that feels capable and fair

A fun opponent communicates a plan through actions the player can notice and answer. It can raid a weak edge, stage before a push, defend an exposed expansion, react to a seen counter, and change posture when evidence changes. It should sometimes misread incomplete information; that is part of fair play, not a defect to patch with hidden knowledge.

Cameo already has a path for varied bot posture, target choice, composition, staging, combat observation, and between-match learning. Keep roles separate: the master controller chooses high-level target and personality; providers supply bounded evidence; the squad manager owns and orders units. The current personality and difficulty laws remain authoritative. Difficulty strength may scale along the existing monotonic schedule, but do not add arbitrary APM limits, hidden omniscience, or abilities that disappear for lower-tier bots. See [AI_ARCHITECTURE.md](AI_ARCHITECTURE.md), [AI_MASTER_PLAN.md](AI_MASTER_PLAN.md), and [DESIGN.md, section 19](../DESIGN.md).

Use engagement logs and measured priors to find repeatable decision failures. Keep learned values frozen during the match when the current design requires it; publish only bounded, versioned, reproducible values. Deep reinforcement learning is not the near-term target. Better state, fair observation, good objective policies, and useful tests are more valuable than a black-box label.

For co-op, the director should coordinate pressure, not merely increase unit counts. Alternate attack lanes with recovery windows; respond to team choices; telegraph an escalation; and let players divide roles or combine forces. A loss should identify which signal or objective was missed. Use current mission, team coordination, and AI logging systems before proposing a new director layer. Valve's Left 4 Dead AI Systems talk is a useful pacing reference because it describes pacing from team intensity; Cameo should adapt this as authored, visible mission phases rather than hidden competitive rubber-banding. [Valve: The AI Systems of Left 4 Dead](https://steamcdn-a.akamaihd.net/apps/valve/2009/ai_systems_of_l4d_mike_booth.pdf)

## 8. Modes should share a core and earn their differences

A shared core lowers learning and maintenance cost. Modes then change objectives, pacing, information, or cooperation in a way that gives players a reason to choose them.

- **Skirmish:** quick practice against a selectable personality and difficulty, with faction/map preview and post-match learning cues.
- **Competitive multiplayer:** a small, deliberately supported subset of factions and maps first. Publish rules, balance evidence, replays, and spectator needs before promising a ladder. The full crossover roster can stay a separate sandbox queue.
- **Co-op campaign:** players share an operational goal, but roles, scouting, and reinforcement choices should let each person contribute. Use authored mission phases and optional objectives rather than a second invisible economy.
- **Survival/defence:** waves should test a changing roster and defence plan; periodic objectives force a choice between holding and venturing out. Preserve a clear route to recovery after losing a perimeter.
- **Objective scenarios:** victory can come from controlling, evacuating, escorting, or denying something other than destroying every structure. These modes teach different faction strengths and create varied match stories.
- **Spectator and replay:** make intent, team decisions, objectives, and critical fights observable. Preserve enough match data to understand decisions without exposing private information during play.
- **Custom maps and modding:** protect engine and content-pack boundaries; provide examples and validation tools that help creators make coherent, legal, testable additions.

Offline/local play and replayable content are resilience features. Be careful about online-only progression or modes that cannot survive the loss of an external service.

## 9. Readability and player control

Every strategic promise needs an interface signal. Use consistent role icons and names, plain-language faction summaries, visible ability readiness, clear selection feedback, and concise combat logs. Let the player inspect why a unit can or cannot be built, what it counters, and what currently blocks its order.

Keep routine actions ergonomic: production queues, rally points, group selection, hotkeys, and camera movement should support decisions rather than become the main test of skill. APM is a style outcome, not a quality goal. Reward anticipation, prioritization, and timely attention. Never fix workload by silently removing player control or issuing hidden counter-orders.

Include a learning view for replays and match results: show first contact, significant scouting observations, major production changes, lost/held objectives, and decisive engagements. Do not expose fogged enemy state to active players through spectator UI or logs.

## 10. Failure modes and safeguards

| Failure mode | Why it makes the game less interesting | Safeguard |
|---|---|---|
| Turtling with no reason to leave | The defender gets safety without sacrificing tempo; the map has no contest. | Give the map multiple valuable but risky sites, useful scouting, and a finite reason to move. Do not make attacks automatically win against a prepared defence. |
| Deathballing | Force concentration removes route and role decisions. | Use objectives, travel time, terrain, scouting, and credible raids to make concentration costly; do not split armies through arbitrary scripts. |
| Snowballing | Early success becomes a mechanical confirmation of the result. | Measure resource and force growth after expansion/engagement wins; retain contestable objective paths and retreats without granting free resources. |
| APM arms race | Repetition and interface speed dominate strategic judgement. | Reduce duplicate actions and improve group-level tools; keep high-skill micro optional and meaningful. The AI receives no arbitrary order cap. |
| Superweapon frustration | A single hidden timer or unavoidable strike erases many decisions. | Telegraph research and launch, provide defensive or offensive responses, and treat the weapon as a map/timing problem with a cost. Apply the same balance pipeline. |
| Invisible AI cheating | Players cannot learn the opponent's plan or trust a loss. | Enforce fog honesty, order ownership, and documented difficulty scaling. Expose signals and outcomes to spectators, not hidden state to players. |
| Roster bloat | More units increase lookup and balance cost without changing decisions. | Require a distinct strategic job and a counter story before adding a unit. Prefer role and content-pack reuse. |
| Unreadable crossover | Familiar units behave in surprising, undocumented ways. | Maintain faction guides, consistent icons, in-game tooltips, and short scenario tutorials for unusual mechanics. |
| Mode sprawl | Campaign, co-op, ranked, survival, and custom play each become thin. | Build on shared simulation and content tools; release a mode only when its goals, UI, AI, maps, and validation are complete. |
| Service dependence | A server shutdown removes play or community history. | Keep local skirmish, campaign, replay export, and documented content formats usable without an external account. |

## 11. Design-to-implementation map

| Design recommendation | Cameo system to use | Boundary and evidence needed |
|---|---|---|
| Keep shared combat readable while factions specialize | `DESIGN.md` weapon/armour laws; role and tier profiles; balance pipeline | No new universal damage type, immunity, or hand-edited balance value. Resolve and measure each matchup. |
| Make faction identity about meaningful composition and timing | ContentPacks, prerequisites, bot compositions, personality-specific doctrine | Do not add all faction IDs to central AI rules; follow [MIGRATION.md](../MIGRATION.md) and pack ownership. |
| Turn scouting into counterplay | shroud/visibility, fog memories, radar and contact providers | Use only information the player/bot can see; run the fog audit for new genericbot reads. |
| Give the AI a readable plan and a single order owner | master controller, personality modules, squad manager, providers, order gate | Preserve leases, synchronized order flow, classic reference, and genericbot-only switch boundaries. |
| Improve fight learning | engagement logging, normalized priors, bounded between-match fitting | Store observations separately from truth; freeze in-match inputs as the binding design requires; test parity and deterministic loading. |
| Add co-op and authored pacing | existing mission/map, team coordination, bot director and logging components | Start with one scenario design. Do not implement hidden rubber-banding or assume a service/lobby feature already exists. |
| Improve onboarding and replay learning | mission text, UI docs, match/replay logs, observer tools | Keep player information separate from observer information. Check engine support before promising UI. |
| Test a new behaviour | default-off switch, pure eval, unit tests, audit, build, boot, measured match | Follow `WORKFLOW.md`; classic stays unchanged and the off-arm order stream stays identical. |

This table is a translation map, not an approval to implement every idea. Route technical tasks through [TASK_INDEX.md](../TASK_INDEX.md) and the live team task record.

## 12. Prioritized roadmap

### P0 - Preserve trust and finish the playable loop

- Keep build, boot, map load, save/load, multiplayer synchronization, and content-pack loading dependable.
- Refresh player-facing faction summaries, controls, warnings, and first-match onboarding from current content.
- Fix measured crashes, stranded units, fog leaks, ownership violations, and game-breaking balance outliers before adding surface area.
- Keep compatibility, content installation, and local skirmish usable without external services.

### P1 - Make each faction understandable and distinctive

- Complete an identity brief and role/counter map for every faction and major subfaction.
- Remove dead roster overlap; ensure key strengths, costs, counters, and timing are visible in rules, tooltips, and at least one authored scenario.
- Use the balance pipeline and controlled match evidence to tune the intended roster; do not set an arbitrary launch count.

### P2 - Build the learning and AI loop

- Close gaps in fair observation, role/target decisions, formation, objective priorities, and personality readability using logged failure cases.
- Keep learning inputs reproducible, bounded, versioned, and based on balanced features rather than actor IDs.
- Publish representative AI skirmish scenarios and difficulty guidance; test against multiple faction/map pairs.

### P3 - Deliver authored play with replay value

- Ship a short, polished campaign/mission arc that introduces factions through their actual mechanics.
- Prototype one two-player co-op scenario and one objective or survival variant from shared content, then assess upkeep and player demand.
- Make mission pressure, optional goals, and recovery routes visible. Use replay and log feedback to revise pacing.

### P4 - Expand competitive and creator ecosystems

- Choose a stable competitive subset only after matchup, map, replay, spectator, and disconnect behavior have evidence.
- Add matchmaking/ranking only when service ownership, moderation, maintenance, and offline fallbacks are explicit.
- Improve custom-map and mod authoring support through documented examples, validation tools, and pack-level modularity.

These priorities describe order of attention, not calendar commitments. The current task queue and release decisions live in `docs/design/ROADMAP.md` and the team task board.

## 13. Evaluation: test decisions, not slogans

For every new mechanic or mode, write a short hypothesis before implementation:

- Which player decision becomes newly useful?
- What information makes that decision possible?
- What does it cost, and what response remains available to the opponent?
- Which faction, map, difficulty, or team context should demonstrate it?
- What failure signal would make us reject or revise the idea?

Use evidence suited to the claim. Static checks establish structural properties; tests establish specified behaviour; build and boot establish loadability; match traces show what the AI did; controlled playtests show whether players understood and enjoyed the choice. Do not claim gameplay quality from a green build or a single match.

Track outcomes without inventing universal numeric gates: faction/map win and surrender patterns; resource and force timing; objective control and expansion denial; order-gate refusals; fog-audit findings; repeated, ineffective army decisions; mission completion and replay patterns; and player comprehension of the faction and loss. Separate observation from interpretation and revise the hypothesis when the data disagrees.

## 14. Source shelf

### Cameo authority

- [DESIGN.md](../DESIGN.md) - binding gameplay, weapon, armour, bot, fog, and multiplayer laws.
- [ROADMAP.md](ROADMAP.md) - current project priorities and decisions.
- [AI_ARCHITECTURE.md](AI_ARCHITECTURE.md) and [AI_MASTER_PLAN.md](AI_MASTER_PLAN.md) - AI composition, future AI work, and architecture.
- [AI_MATCH_LOG.md](AI_MATCH_LOG.md) and [AI learning research](AI_LEARNING_RESEARCH_2026-10-03.md) - telemetry and learning constraints.
- [BALANCE_PROGRAM_PLAN.md](BALANCE_PROGRAM_PLAN.md) and [TASK_INDEX.md](../TASK_INDEX.md) - balance method and task routing.
- [MIGRATION.md](../MIGRATION.md) and [WORKFLOW.md](../WORKFLOW.md) - content-pack boundaries and implementation evidence.

### External references

The external links in section 3 point to source material for the design patterns summarized here. The summaries are Cameo recommendations inferred from those examples, not quotations or endorsements by the referenced developers.
