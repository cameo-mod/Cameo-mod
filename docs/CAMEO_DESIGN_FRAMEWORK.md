# Cameo Design Framework

**Purpose:** keep Cameo's crossover breadth readable, fair, and strategically useful. This document is advisory. Binding rules and current implementation status live in the linked design and technical documents; this framework does not authorize a change to them.

## 1. Design promise

A Cameo match should give players reasons to scout, commit, adapt, and understand the result. Its crossover roster is material for interesting decisions, not a goal to maximize by itself. A choice is useful when the player can see plausible alternatives, understand meaningful costs and risks, and recognize later why the choice mattered.

Use three decision horizons:

- **Tactical:** target, retreat, ability, screen, flank, or focus fire.
- **Operational:** front, route, expansion, or objective.
- **Strategic:** economy, production, technology, composition, or alliance posture.

The intended match rhythm is **read → prepare → contest → adapt → resolve**. Every phase should provide a legible goal, a visible risk, and some way to press or recover. This is a design aim, not a fixed match-duration or comeback formula.

## 2. The crossover contract

Cameo brings together **ten source-universe content packs** plus Cameo-original factions. The ten packs are Tiberian Dawn, Tiberian Sun, Tiberium Wars, Red Alert, Red Alert 2, Red Alert 2 Mod, Dune 2000, StarCraft, Warcraft 2, and Outpost 2. `Core` and `Shared` are support packs, not additional universes. This count is from `mods/cameo/ContentPacks` directory names; the Red Alert 2 Mod pack also contains Cameo originals.

Give players a shared vocabulary for economy, production, scouting, roles, threat, and map control. Let factions express those concepts through distinct resource choices, timing, mobility, information, and composition. Units with the same role need not be interchangeable; their cost, exposure, delivery, and counterplay should explain the difference.

Combined Arms' subfaction variants are a useful example of making faction differences explicit through access and composition choices. Cameo should express this through a small, legible doctrine choice rather than multiplying parallel rosters. This is a mechanical lesson, not a comparison of press, popularity, or release history. ([Combined Arms](https://www.moddb.com/mods/command-conquer-combined-arms))

Crossover should create combinations, not universal answers. A borrowed mechanic should complement a role while keeping its constraints visible. Add roster content only when it creates a decision the current roster cannot express clearly; consider a role, prerequisite, ability, upgrade, squad policy, map object, or mission rule before adding another unit.

For each faction or major subfaction, keep a concise identity brief that answers:

1. What does it want the player to do differently?
2. How does it gain and spend resources, scout, and choose engagements?
3. Which roles are strong, absent, delayed, or unusually risky?
4. What visible signal tells an opponent the plan is underway?
5. What affordable response remains, and when is its response window?
6. Which map or mode makes the identity especially clear?

## 3. Combat, maps, and fairness

Design counters through measured efficiency, range, role, mobility, information, positioning, and cost. Keep them inside Cameo's current weapon and armour rules; do not invent immunity classes, damage axes, kill-ratio laws, or price targets here. Use the balance pipeline and controlled comparisons for balance claims. Do not tune YAML to make a design argument appear true.

Maps are economic and informational systems. Vary the exposure, travel time, vision, and payoff of routes and objectives so players have reasons to leave the base and choose where to spend force. Avoid both a single unavoidable choke and a field of equal-value points. Map review should test faction mobility, side, scouting, expansion, and recovery routes.

Fairness requires that players and bots act on information they are allowed to know. Information may be partial, stale, deceptive, or costly to acquire, but its limits should be understandable. Bots issue orders through the project’s ownership and synchronization path; they do not gain hidden knowledge to simulate skill. Keep the player's control legible: show order feedback, ability state, build blockers, and useful explanations after a match.

## 4. Modes and progression

Modes should share the simulation and earn their differences through objectives, pacing, information, or cooperation. Treat these as design directions, not claims that each mode is implemented:

- **Skirmish:** practice a faction and read the opponent's plan.
- **Competitive play:** support a deliberate faction/map subset only when matchup, replay, spectator, and disconnect evidence is ready.
- **Co-op and campaign:** use visible mission phases, complementary roles, and optional objectives; pressure should respond to the team without hidden rubber-banding.
- **Survival and objective scenarios:** ask players to change plans, venture beyond a perimeter, or win through a goal other than destroying every structure.
- **Creator play:** preserve pack boundaries and provide examples and validation tools.

Keep local and replayable experiences useful without relying on an external service. Explain faction identity through short authored play, then let the same identity work in other modes. Do not promise interface, replay, service, or engine features before checking current support.

## 5. AI and learning

A capable opponent communicates intent through observable actions: scouting, staging, defending an exposed site, raiding a weak edge, or changing posture after new evidence. It can make mistakes when information is incomplete. Difficulty should follow the existing documented schedule; avoid hidden omniscience, arbitrary order caps, or abilities withheld from lower tiers.

Keep responsibilities separated across high-level decisions, information providers, personality, squad management, and order validation, following the current AI architecture and binding rules. Learn from logged failures and measured outcomes. Store observations separately from truth, keep inputs reproducible and bounded, and freeze match-time learning where the current design requires it. Improve state, fair observation, and objective policy before adopting a black-box learning label.

## 6. How to test a design idea

Write a short hypothesis before implementation:

- Which player decision becomes newly useful?
- What information makes it possible?
- What does it cost, and what response remains available?
- Which faction, map, mode, or difficulty should demonstrate it?
- What evidence would make us revise or reject the idea?

Use evidence suited to the claim. A static audit establishes a structural property; a test establishes specified behavior; a build and boot establish loadability; match traces show system behavior; controlled playtests show whether players understand and enjoy the choice. A green build or one match does not establish gameplay quality. Keep experiments reversible, compare complete increments, and preserve reference behavior when a switch is off.

## 7. Cameo references and implementation boundaries

These project documents are authoritative for their subjects:

- [DESIGN.md](DESIGN.md) — binding gameplay, combat, bot, fog, ownership, and multiplayer rules.
- [ROADMAP.md](design/ROADMAP.md) — current project priorities.
- [AI_ARCHITECTURE.md](design/AI_ARCHITECTURE.md) and [AI_MASTER_PLAN.md](design/AI_MASTER_PLAN.md) — AI architecture and planned work.
- [AI_MATCH_LOG.md](design/AI_MATCH_LOG.md), [BALANCE_PROGRAM_PLAN.md](design/BALANCE_PROGRAM_PLAN.md), and [TASK_INDEX.md](TASK_INDEX.md) — evidence and work routing.
- [MIGRATION.md](MIGRATION.md) and [WORKFLOW.md](WORKFLOW.md) — content-pack boundaries and change validation.

External references inform specific design decisions; the translation to Cameo is an inference, not a claim that Cameo should reproduce another game's features:

- Combined Arms, [mod overview](https://www.moddb.com/mods/command-conquer-combined-arms) — its subfaction variants are an example of making faction differences visible through access and composition; Cameo should test a more compact doctrine choice.

- Sid Meier, [Interesting Decisions (GDC)](https://www.gdcvault.com/play/1015756/interesting) — use alternatives, costs, and consequences to evaluate whether a mechanic creates a meaningful choice.
- Blizzard, [StarCraft scouting guide](https://news.blizzard.com/en-us/article/4488316/game-guide-scouting) — connect scouting to a response such as changing composition, route, or expansion plan.
- Age of Empires, [civilizations and modes](https://www.ageofempires.com/learn-to-play/civilizations-game-modes-aoe2/) — use a common strategic vocabulary while faction availability and bonuses create preparation choices.
- Valve, [The AI Systems of Left 4 Dead](https://steamcdn-a.akamaihd.net/apps/valve/2009/ai_systems_of_l4d_mike_booth.pdf) — use visible, authored phases for cooperative pacing rather than hidden competitive rubber-banding.
