# CAMEO — The Ultimate Crossover RTS Design Framework

**Version:** 8.0 (definitive, fact-checked against live repository)
**Date:** October 5, 2026
**Project:** [cameo-mod/Cameo-mod](https://github.com/cameo-mod/Cameo-mod)
**Status:** Non-binding design proposal / Pre-Production

> **Important:** This framework is a non-binding design proposal. It does not override binding project documents (`DESIGN.md`, `FORMULA_V2.md`, `ARMOR_SYSTEM.md`, `FACTION_IDENTITY.md`, `ROADMAP.md`). All factual claims about the project are sourced from the current `cameo-mod/Cameo-mod` repository. Design research merged from multiple AI advisors (Grok 4.5/xAI, Microsoft Copilot, ChatGPT, Perplexity AI, deep research report) with the actual project architecture and a competitive analysis against Combined Arms.

### Scope Correction

This document is rebuilt from the **current** `cameo-mod/Cameo-mod` repository. The following old Zeruel-era concepts are **explicitly excluded** — they do not exist in the current project: MCV market, casino, heroes portal, sandworms, Halloween/zombie faction, hybrid factions, Atreides/Harkonnen, unverified claims about "six resource types" or "200+ maps."

---

## Table of Contents

1. [Design Doctrine](#1-design-doctrine)
2. [Competitive Landscape: Combined Arms vs Cameo](#2-competitive-landscape)
3. [Catch-Up Strategy](#3-catch-up-strategy)
4. [Three-Layer Coherence Model](#4-three-layer-coherence-model)
5. [Universal RTS Core](#5-universal-rts-core)
6. [Armor and Weapon System](#6-armor-and-weapon-system)
7. [Faction Doctrine and Complete Roster](#7-faction-doctrine-and-complete-roster)
8. [Roster Tiering](#8-roster-tiering)
9. [Crossover System](#9-crossover-system)
10. [Combat Framework](#10-combat-framework)
11. [Balance Pipeline](#11-balance-pipeline)
12. [Tech Progression and Commander Tree](#12-tech-progression-and-commander-tree)
13. [Game Modes](#13-game-modes)
14. [Map and Intel Systems](#14-map-and-intel-systems)
15. [AI Framework](#15-ai-framework)
16. [Multiplayer and Competitive Infrastructure](#16-multiplayer-and-competitive-infrastructure)
17. [UX and Codex](#17-ux-and-codex)
18. [Lore Framework](#18-lore-framework)
19. [Universe Adapter Sheets](#19-universe-adapter-sheets)
20. [Anti-Patterns](#20-anti-patterns)
21. [Community, Modding, and Legal](#21-community-modding-and-legal)
22. [Implementation Plan and Roadmap](#22-implementation-plan-and-roadmap)
23. [Appendix](#23-appendix)
24. [Sources](#24-sources)

---

## 1. Design Doctrine

### 1.1 The Central Thesis

> Cameo should not attempt to make every RTS universe obey its original rules. It should create one coherent RTS language in which every universe expresses its own doctrine.

Cameo is a third-party OpenRA mod incorporating content from C&C, StarCraft, Dune, Warcraft, Outpost, and Cameo originals — 28+ factions across 8 source universes on a single ruleset ([README](https://github.com/cameo-mod/Cameo-mod)).

### 1.2 The Five Gameplay Pillars

| Pillar | Principle | Test Question |
|--------|-----------|---------------|
| **Identity** | Every faction must feel unmistakably different. | "Can I identify this faction blindfolded?" |
| **Counterplay** | Everything powerful must have meaningful answers. | "Does the opponent have a response?" |
| **Agency** | Players win because of decisions, not faction choice. | "Was this a strategic decision or inevitable?" |
| **Emergence** | Faction interactions generate unscripted situations. | "Could this have happened in the designer's office?" |
| **Readability** | Enormous underneath, understandable on the surface. | "Do I understand what just happened and why?" |

### 1.3 The Binding Design Rule

> **CAMEO RULE: COMPLEXITY MUST CREATE CHOICES, NOT HOMEWORK.**

### 1.4 North-Star Pitch

> **Cameo** is the RTS where every classic army keeps its soul, but they all speak one language of armor, roles, and tempo — so the match is always a *story* (scout, deny, counter, commit), never a random unit encyclopedia.

### 1.5 The Honest Starting Point

Cameo's own README states: *"Cameo is extremely buggy, lacking in properly functioning features, and crash-prone."* This is the starting point. The goal is to close the gap with Combined Arms — which Polygon called *"the best new Command & Conquer game in over a decade"* ([Polygon](https://www.polygon.com/pc/503559/command-and-conquer-combined-arms-mod/)) — while leveraging Cameo's unique advantage: more universes, more factions, and a more sophisticated balance pipeline.

---

## 2. Competitive Landscape: Combined Arms vs Cameo

### 2.1 Combined Arms Profile

Combined Arms (CA) is the leading OpenRA mod. Its profile:

| Attribute | Combined Arms | Source |
|-----------|--------------|--------|
| Factions | 5 main (Allies, Soviets, GDI, Nod, Scrin), each with 4 sub-factions (~20 playable) | [Polygon](https://www.polygon.com/pc/503559/command-and-conquer-combined-arms-mod/), [Reddit](https://www.reddit.com/r/commandandconquer/comments/1i5ny3e/combined_arms/) |
| Source material | TD, RA, TS, RA2, Generals | [PCGamesN](https://www.pcgamesn.com/command-and-conquer/combined-arms) |
| Single-player | Full campaign with missions | [PCGamer](https://www.pcgamer.com/this-mod-mashes-up-five-factions-from-command-and-conquers-past-into-one-giant-brawl/) |
| Co-op | Cooperative multiplayer missions | [PCGamer](https://www.pcgamer.com/this-mod-mashes-up-five-factions-from-command-and-conquers-past-into-one-giant-brawl/) |
| Competitive | 1v1 ladder servers with rankings and statistics | [ModDB](https://www.moddb.com/mods/command-conquer-combined-arms) |
| Press coverage | Polygon, PCGamer, PCGamesN, GamePressure | Multiple |
| Distribution | Standalone on ModDB, Snap Store (Linux) | [Snapcraft](https://snapcraft.io/openra-combined-arms), [ModDB](https://www.moddb.com/mods/command-conquer-combined-arms) |
| Latest version | 1.08 (December 2025), latest OpenRA engine | [GamePressure](https://www.gamepressure.com/newsroom/free-rts-that-combines-command-conquer-universes-with-new-mission/z68b66) |
| Concurrent players | ~91–105 online | [RoWatcher](https://rowatcher.com/games/10452420697/ea-combined-arms), [SpawnScope](https://spawnscope.com/games/combined-arms/) |
| Community | Active Discord, regular tournaments | [OpenRA Forum](https://forum.openra.net/viewtopic.php?t=21795) |
| ModDB ranking | Second most popular OpenRA mod (June 2023) | [OpenRA News](https://www.openra.net/news/release-20231010/) |

### 2.2 Cameo Profile

| Attribute | Cameo | Assessment |
|-----------|-------|------------|
| Factions | 28+ across 8 source universes (C&C TD, C&C TS, C&C RA1, C&C RA2, Dune, StarCraft, Warcraft 2, Outpost 2, plus Cameo originals) | **Advantage** — more variety |
| Source material | TD, TS, RA1, RA2, Dune, StarCraft, Warcraft 2, Outpost 2, originals | **Advantage** — broader crossover |
| Balance pipeline | Distribution-based pricing, formula.py, band calibration, 17-type armor system | **Advantage** — more sophisticated |
| AI architecture | Frankenstein bot (6 parents), fog-honest target, personality system | **Advantage** — deeper design |
| Stability | "Extremely buggy, crash-prone" (own README) | **Critical gap** |
| Single-player | Campaign as vision, not yet implemented | **Critical gap** |
| Co-op | Not yet available | **Gap** |
| Competitive ladder | Not yet available | **Gap** |
| Press coverage | Minimal | **Gap** |
| Distribution | GitHub, ModDB | **Gap** — no Snap Store, no standalone installer |
| Community | Discord exists | **Gap** — smaller active player base |
| Versioned releases | Ongoing development | **Gap** — no regular release cadence |

### 2.3 Gap Analysis

```
                    Cameo Advantage          Combined Arms Advantage
                    ┌─────────────────┐      ┌─────────────────────┐
                    │ Faction count   │      │ Stability           │
                    │ Universe variety│      │ Polish              │
                    │ Balance pipeline│      │ Single-player       │
                    │ AI architecture │      │ Co-op missions      │
                    │ Armor system    │      │ Competitive ladder  │
                    │ Cameo originals │      │ Press coverage      │
                    │ Crossover vision│      │ Distribution        │
                    │                 │      │ Active player base  │
                    │                 │      │ Regular releases    │
                    └─────────────────┘      └─────────────────────┘
```

### 2.4 What Combined Arms Has That Cameo Doesn't

| CA Feature | Description | Priority for Cameo |
|------------|-------------|---------------------|
| **Stability** | CA is polished and crash-free; Cameo's README admits "extremely buggy" | P0 — #1 blocker |
| **Single-player campaign** | CA has a full campaign with missions across factions | P1 — critical for player retention |
| **Co-op missions** | CA has cooperative multiplayer missions | P2 — high engagement value |
| **1v1 ladder** | CA has ranked servers with statistics and rankings | P1 — competitive scene anchor |
| **Sub-faction system** | CA has 4 sub-factions per main faction (20 total) | P2 — Cameo uses in-game doctrine branching instead (see §3.4) |
| **Press coverage** | CA covered by major gaming outlets | P2 — visibility drives player acquisition |
| **Standalone distribution** | CA on Snap Store, ModDB as standalone | P2 — reduces friction |
| **Regular releases** | CA has versioned releases (0.90 → 1.05 → 1.07 → 1.08) | P1 — shows momentum |
| **Onboarding** | CA emulates original games — familiar to C&C players | P1 — Cameo needs tutorials |
| **Tournament infrastructure** | CA has regular tournaments | P2 — community building |

---

## 3. Catch-Up Strategy

### 3.1 The Core Thesis

> Cameo's advantage is breadth and depth. Combined Arms' advantage is polish and focus. The catch-up strategy is: **fix stability first, then ship the features that make players stay (campaign, ladder, co-op), then leverage the crossover advantage CA cannot match.**

### 3.2 Four-Phase Catch-Up Plan

#### Phase 1: Stop the Bleeding (Weeks 1–6)

| Task | Description | Target |
|------|-------------|--------|
| **Crash audit** | Systematic crash reproduction, logging, and fixing | Reduce crash rate by 80% |
| **Boot gate hardening** | Every PR must pass boot test; no merge without green | Zero boot failures on master |
| **README rewrite** | Remove "extremely buggy" language; set honest but positive tone | Public-facing confidence |
| **Bug triage** | Public bug tracker, prioritized by frequency and severity | Top 50 crashes fixed |
| **Performance pass** | Pathfinding optimization, large-battle frame rate | 60 FPS in 4-player matches |

> Without stability, nothing else matters. The community cannot grow if the game crashes.

#### Phase 2: Ship What Players Expect (Weeks 7–16)

| Task | Description | Competitive Equivalent |
|------|-------------|----------------------|
| **1v1 ladder** | Ranked servers with ELO/Glicko, statistics, rankings | CA's ladder servers |
| **In-game doctrine system** | Level 1/Level 2 mutually exclusive doctrine upgrades that branch the tech tree mid-match | CA's 4 fixed lobby sub-factions |
| **Tutorial system** | Interactive tutorials for each universe's mechanics | CA's familiarity advantage |
| **First campaign mission** | Beachhead mission as proof of concept | CA's campaign missions |
| **Versioned release** | First tagged release (e.g. Cameo 0.9) | CA's versioned releases |

> Players expect campaign, ladder, and stability. Ship these before adding more factions.

#### Phase 3: Leverage the Crossover Advantage (Weeks 17–32)

| Task | Description | Why CA Can't Match It |
|------|-------------|---------------------|
| **Full campaign (3-mission arcs)** | Singularity Campaign with multiverse map | CA is C&C-only; no crossover narrative |
| **Crossover loadout system** | Core + License + Relic + Detachment pre-match | CA has no crossover mechanic |
| **Universe cluster queues** | "C&C Night," "SC Night," "Dune Night" | CA has no multi-universe roster |
| **Cross-faction abilities** | Buffs/debuffs that work across universe lines | CA is single-universe |
| **Co-op campaign** | 2-player co-op with combined doctrines | CA has co-op but no crossover co-op |

> This is where Cameo pulls ahead. CA cannot replicate the crossover fantasy.

#### Phase 4: Community and Visibility (Weeks 33–48)

| Task | Description |
|------|-------------|
| **Press outreach** | Send build to Polygon, PCGamer, PCGamesN, Eurogamer, IGN |
| **Tournament series** | Cameo Showcase Tournament (Swiss + double elim) |
| **Content creator program** | Provide builds to YouTubers and streamers |
| **ModDB presence** | Active ModDB page with regular updates and screenshots |
| **Snap Store / Flatpak** | Linux distribution beyond GitHub |
| **Discord growth** | Community events, weekly play nights, faction guides |

### 3.3 The "Why Cameo Wins" Argument

Combined Arms is the best C&C crossover. Cameo is the only **multi-universe** crossover. The pitch to players and press:

> Combined Arms proved that a polished OpenRA crossover can be "the best new C&C game in a decade." Cameo asks the next question: what happens when you add StarCraft, Dune, Warcraft, and original factions to that formula? The answer is dream matches that no other game can offer — Zerg vs Soviets, Nod vs Protoss, Ordos vs CABAL — on one ruleset with a serious balance pipeline.

### 3.4 In-Game Doctrine Branching (Cameo's Answer to Sub-Factions)

Combined Arms uses lobby-level sub-faction selection (4 per main faction). Cameo takes a different approach: **expanded, polished, deep factions with doctrines and promotions that branch the tech tree in-game through mutually exclusive upgrades.** The player doesn't pick a sub-faction from the lobby — they make strategic commitments during the match itself.

#### Doctrine System Structure

```
Lobby: Select FACTION (e.g. Soviets RA1)
  │
  In-Game: Research TIER I DOCTRINE (mutually exclusive)
  │  ├── Conscription Doctrine
  │  ├── Industrial Efficiency Doctrine
  │  └── Inferno Doctrine
  │
  Later: Research TIER II DOCTRINE (mutually exclusive, requires any Tier I)
  │  ├── Tesla & Experimental Tech Doctrine
  │  ├── Heavy Armor Doctrine
  │  └── Nuclear War Doctrine
  │
  Result: 3×3 = 9 distinct strategic paths per faction
```

The doctrine system is already implemented across multiple factions, not just Soviets:

| Faction | Doctrine System | Tiers | Source |
|---------|----------------|-------|--------|
| Soviets RA1 | 3 Tier I × 3 Tier II = 9 paths | 2 tiers | [upgrades.yaml](https://github.com/cameo-mod/Cameo-mod/blob/master/mods/cameo/ContentPacks/RedAlert/Soviets/yaml/upgrades.yaml) |
| Allies RA2 | 3 Tier I × 3 Tier II = 9 paths | 2 tiers | [upgrades.yaml](https://github.com/cameo-mod/Cameo-mod/blob/master/mods/cameo/ContentPacks/RedAlert2/Allies/yaml/upgrades.yaml) |
| Soviets RA2 | 3 Tier I × 3 Tier II × 3 Tier III = 27 paths | 3 tiers | [upgrades.yaml](https://github.com/cameo-mod/Cameo-mod/blob/master/mods/cameo/ContentPacks/RedAlert2/Soviets/yaml/upgrades.yaml) |
| Yuri | 3 Tier I × 3 Tier II × 3 Tier III = 27 paths | 3 tiers | [upgrades.yaml](https://github.com/cameo-mod/Cameo-mod/blob/master/mods/cameo/ContentPacks/RedAlert2/Yuri/yaml/upgrades.yaml) |
| Asian Alliance | 3 doctrines | 1 tier | [upgrades.yaml](https://github.com/cameo-mod/Cameo-mod/blob/master/mods/cameo/ContentPacks/RedAlert2Mod/AsianAlliance/yaml/upgrades.yaml) |
| Latin Syndicate | 3 doctrines | 1 tier | [upgrades.yaml](https://github.com/cameo-mod/Cameo-mod/blob/master/mods/cameo/ContentPacks/RedAlert2Mod/Syndicate/yaml/upgrades.yaml) |
| FutureTech | 3 sequential doctrines (Equalizer X1→X2→X3) | Progressive | [upgrades.yaml](https://github.com/cameo-mod/Cameo-mod/blob/master/mods/cameo/ContentPacks/RedAlert2Mod/FutureTech/yaml/upgrades.yaml) |

#### How It Works

| Element | Description |
|---------|-------------|
| **Tier I Doctrine** | Early-game mutually exclusive upgrade that sets strategic direction. Researched at a tech building (e.g. Radar Dome). Choosing one locks out the others via `ProductionIconMutualExclusion` group. |
| **Tier II Doctrine** | Mid-game mutually exclusive upgrade that deepens the Tier I commitment. Requires any Tier I doctrine as prerequisite. Further specializes the faction. |
| **Tier III Doctrine** (some factions) | RA2 Soviets and Yuri have a third tier of mutually exclusive doctrines, creating 27 strategic paths. |
| **Promotion system** | Separate from doctrines: units gain experience through combat. CABAL uses a two-branch four-tier promotion system (Cyborg: Devout→Ascended→Beholder→Cyborg Commando V2; Spider: Spider CNC4→Heavy Reaper→Widow→Core Defender). Steel Consortium uses a two-branch promotion tree (Support: LVL1-4; Military: Barracuda→Dagger→Katy Tank→White Rabbit). |

#### Why This Beats Lobby Sub-Factions

| CA Sub-Factions | Cameo Doctrine Branching |
|----------------|------------------------|
| Decision made before match starts | Decision made during match — responds to opponent |
| Fixed playstyle per sub-faction | Dynamic — doctrine choice adapts to game state |
| 4 sub-factions = 4 fixed experiences | 3+ L1 × 3+ L2 = 9+ branching paths per faction |
| No strategic tension in selection | Timing of doctrine research is a strategic decision |
| Lobby complexity grows with faction count | Lobby stays clean — one faction per player |

#### Reference Implementation: Soviets RA1

The RA1 Soviets faction is the reference for this system. With **53 units, 21 buildings, and 44 upgrades** (the most of any faction in Cameo), the Soviets demonstrate how a single deep faction can offer more strategic variety than multiple shallow sub-factions:

| Tier | Doctrine | Unlocks | Playstyle |
|------|----------|---------|----------|
| **I** | Conscription Doctrine | Vengeance, Men of Steel, Commissar unit | Infantry mass, defensive retribution |
| **I** | Industrial Efficiency Doctrine | Mass Production, War Economy | Cheaper, durable, overwhelming numbers |
| **I** | Inferno Doctrine | Incendiary Bullets, Scorched Earth, Heatray Tank | Flame weapon supremacy, area denial |
| **II** | Tesla & Experimental Tech Doctrine | Tesla Arcing, Tesla Rockets, Reactor Overload, Tesla Yak, Heavy Tesla Tank upgrade | Electric weapon dominance, area denial |
| **II** | Heavy Armor Doctrine | Autoloaders, Thermobaric Rockets, Stalinium, Armored Yak, Shtora Defense System, Thermobaric Rockets | Fortified armor, counterattack doctrine |
| **II** | Nuclear War Doctrine | Unstable Isotopes, Thermonuclear Rockets, Nuclear Tank Shells, Nuclear Yak, Kotin Nuclear Tank upgrade | Radiation zones, superweapon synergy |

Tier I doctrines are mutually exclusive (group `ra1_soviets_doctrine_tier_1`), and Tier II doctrines are mutually exclusive (group `ra1_soviets_doctrine_tier_2`). Any Tier I doctrine unlocks Tier II. This creates **9 distinct strategic paths** from a single faction — more variety than CA's 4 fixed sub-factions, and all driven by in-match decisions.

Non-doctrine upgrades available regardless of path: Hazmat Suits (Tech Center), Afterburners (Radar Dome), Hammer Tank Upgrade (Radar Dome — feeds into doctrine-specific tank upgrades).

#### Current Doctrine Implementation Across Factions

The doctrine system is already implemented in 7 factions. The table below shows the actual doctrine paths from the live repository:

| Faction | Tier I Doctrines | Tier II Doctrines | Tier III Doctrines | Total Paths |
|---------|-----------------|------------------|-------------------|-------------|
| **Soviets RA1** | Conscription / Industrial Efficiency / Inferno | Tesla & Experimental Tech / Heavy Armor / Nuclear War | — | 9 |
| **Allies RA2** | Assault Squad Training / Vanguard Training / Elite Infiltrators Training | Composite Armor Plating / Reflective Armor Plating / Chromium Ion Pulse Plating | — | 9 |
| **Soviets RA2** | Conscription / Harsh Environment Infantry Conditioning / Shock Trooper Training | Heavy Armor Platings / Reactive Armor / Tesla Discharge Armor | Fire Munitions / Nuclear Munitions / Tesla Munitions | 27 |
| **Yuri** | Psionic Legion / Psionic Fanatics / Psionic Elite | Lasher HE Cannon / Lasher Toxic Gas Shells / Lasher Laser Cannon | Scrap Vehicle Armor / Chaos Gas Emitters / Psionic Vehicle Shields | 27 |
| **Asian Alliance** | Modernized Infantry Equipment / Heavy Pulverizer Weapons / Asian Phalanx | — | — | 3 |
| **Latin Syndicate** | Elite Guerillas / Cartel Rockets / Light Posts | — | — | 3 |
| **FutureTech** | Equalizer X1 (progressive, not mutually exclusive) → X2 → X3 | — | — | 1 (sequential) |

Factions without doctrines yet (expansion candidates):

| Faction | Proposed Doctrine Paths | Strategic Depth |
|---------|------------------------|----------------|
| GDI TD | Air Superiority / Heavy Armor / Defensive Fortification | 3+ × 3+ paths |
| Nod TD | Stealth Infiltration / Fast Strike / Tiberium Weapons | 3+ × 3+ paths |
| Protoss | Warp Superiority / Shield Tech / Psionic Storm | 3+ × 3+ paths |
| Zerg | Swarm Mass / Mutation Depth / Creep Dominance | 3+ × 3+ paths |
| Steel Consortium | Already uses promotion tree (2 branches, 4 tiers each) — could add doctrines on top | Promotion + doctrines |

Each faction gets **deeper, not wider** — more upgrades, more branching, more in-match decisions — rather than splitting into lobby-level sub-factions.

#### The Design Rule

> **CAMEO DOCTRINE RULE: Depth over width. One faction with branching in-game doctrines creates more strategic variety than three lobby sub-factions, because the doctrine choice is a strategic decision made in response to the game state, not a pre-match commitment.**

### 3.5 Stability-First Milestone Gates

No feature work proceeds until these gates pass:

| Gate | Criteria | Measured By |
|------|----------|------------|
| **G1: Boot** | Every PR passes boot test on Windows + Linux | CI/CD |
| **G2: Crash** | Zero crashes in 10 consecutive 1v1 AI matches | Automated test |
| **G3: 4-player** | 60 FPS in 4-player match for 20 minutes | Performance test |
| **G4: MP** | 10 consecutive multiplayer matches without desync | Ladder test |
| **G5: Campaign** | Campaign mission loads, plays, and completes | QA checklist |

---

## 4. Three-Layer Coherence Model

### 4.1 The Three Layers

```text
LAYER A — Shared Physics (must be universal)
  17-type armor system, LEVEL step law, PROFILE role order
  HP, range/DPS language, pathing, fog, algorithmic pricing
  → FORMULA_V2.md, ARMOR_SYSTEM.md, class anchors

LAYER B — Faction Doctrine (must be legible in 30 seconds)
  Eco model, production verb, army density, signature power
  HP ↔ Speed bias, no mirror factions, uniqueness law
  → FACTION_IDENTITY.md / ContentPacks

LAYER C — Spectacle & Nostalgia (never breaks A or B)
  VO, art, superweapons, map props
  → Safe to be chaotic; must have counters, tells, and costs
```

**Critical rule:** If a unit only lives in Layer C but breaks Layer A, it is a **bug**, not "flavor."

---

## 5. Universal RTS Core

### 5.1 Universal Verbs

| Verb | Description | Examples |
|------|-------------|----------|
| Move | Change position | Stealth Tank, Zergling |
| Attack | Deal damage | Mammoth Tank, Marine |
| Defend | Hold position | Bunker, Garrison |
| Scout | Gather intelligence | Observer, Spy |
| Produce | Create units/structures | Hatchery, War Factory |
| Capture | Control sectors/points | Engineer, Pioneer |
| Repair | Heal units/buildings | SCV, Medic |
| Upgrade | Research technology | Tech Center, Evolution Chamber |
| Expand | Establish new base | MCV, Nexus |
| Deploy | Assume special formation | Siege Tank, Warp-In |

### 5.2 Universal Role System

**Combat Roles:** Infantry, Heavy Infantry, Scout, Raider, Frontline, Tank, Tank Destroyer, Anti-Infantry, Anti-Air, Artillery, Missile, Assault, Siege, Fire Support

**Strategic Roles:** Harvester, Engineer, Builder, Transport, Detection, Support, Caster, Superweapon, Objective, Hero/Epic

The "gigantic roster" becomes an advantage: 15 different tanks should mean 15 different *roles*, not 15 statistically different versions of the same unit.

### 5.3 Tactical States

| State | Trigger | Effect |
|-------|---------|--------|
| Suppressed | Sustained fire | Movement −60%, Damage −30% |
| Pinned | Extreme suppression | Immobile until recovery |
| Garrisoned | Enter building | +75% Cover, immune to suppression |
| Burrowed | Stealth mechanic | Cloaked, immobile |
| Cloaked | Stealth field | Invisible without Detection |
| Entrenched | Preparation time | +50% Cover, +20% Range |
| Retreating | Morale break | Movement +30%, Attack −50% |
| Berserk | Special ability | Attack +50%, Defense −30% |

---

## 6. Armor and Weapon System

Reflects the **actual** system from [ARMOR_SYSTEM.md](https://github.com/cameo-mod/Cameo-mod/blob/master/docs/design/ARMOR_SYSTEM.md).

### 6.1 Two Orthogonal Axes

1. **LEVEL (power)** = step size by which effectiveness falls from 100 down the armor ladder
2. **PROFILE (role)** = the order of the 17 armor types — which armor sits at 100

### 6.2 LEVEL — The Step Law

| Level | Step | Runs 100 → | WC (K) |
|-------|------|------------|--------|
| Light | 6 | 10 | 0.75 |
| Medium | 5 | 25 | 1.00 |
| Heavy | 4 | 40 | 1.25 |
| Super | 3 | 55 | 1.50 |

- 16 non-Shield armor types, 100 down in 15 steps to the floor
- Flatter = generalist; steeper = specialist
- Super (step 3) confirmed for Nuclear and charged Tesla

### 6.3 Shield Law

> `Shield = top + floor` is **RETIRED** (W25, 2026-08-16). Replaced by `DESIGN.md §12.0c`: `Shield = PHYSICS_RANK × SHIELD_LEVEL × damped structural scale`, compressed onto [100, 400].

### 6.4 Faction Identity Bias System

From [FACTION_IDENTITY.md](https://github.com/cameo-mod/Cameo-mod/blob/master/docs/design/FACTION_IDENTITY.md):

**Primary axis: HP ↔ Speed.**
- Brute/Heavy/Turtle → +HP, +Damage, −Speed
- Rush/Mobile/Stealth/Swarm → +Speed, −HP

**Secondary axes:**
- Tech/Power → +Damage, +Range
- Special mechanic (priced via special-K): stealth, mind-control, self-heal, transforming

**Per-TYPE, not global:** A faction's lean can invert between unit types (Soviet infantry frail, Soviet tanks tanky).

**NO mirror factions:** Every faction and every individual unit gets distinct stats (uniqueness law). WC2 Humans lean defensive/versatile; Orcs lean aggressive/brute.

### 6.5 Faction Bias Table (Infantry)

| Faction | Playstyle | HP | SPD | DMG | RNG | Special |
|---------|----------|----|----|-----|-----|---------|
| GDI TD | Brute Force | + | − | + | · | Air power, defenses |
| Nod TD | Rush / Stealth | − | + | · | · | Stealth, asymmetric |
| GDI TS | Heavy Tech | + | − | + | + | EMP, air, armor |
| Nod TS | Stealth / Hit-and-Run | − | + | · | · | Stealth, subterranean |
| CABAL | Machine Redundancy | + | − | + | + | Backup, reanimation |
| Forgotten | Salvage | · | · | · | · | Salvage, mutation |
| Allies RA1 | Naval / Air | − | + | · | + | Naval, air, chrono |
| Soviets RA1 | Brute Force | + | − | + | · | Tesla, heavy tanks |
| Japan RA1 | Precision / Mobility | − | + | + | · | Naval, transform |
| Allies RA2 | Tech / Versatile | + | − | · | + | Chrono, air, naval |
| Soviets RA2 | Brute Force | + | − | + | · | Tesla, desolator |
| Yuri | Mind Control | · | · | · | · | Mind control, psychic |
| Asian Alliance | Coordinated | · | + | · | · | Multi-domain |
| Steel Consortium | Doctrine Switch | · | · | · | · | Promotion tree (2 branches, 4 tiers), flux |
| Latin Syndicate | Raid / Black Market | − | + | · | · | Black market, sabotage |
| Naxis | Alien / Dimensional | + | · | + | + | Alien tech, dimensional |
| Schwarzer Mond | Stealth / Occult | − | + | + | · | Stealth, psychological |
| FutureTech | Energy / Experimental | + | · | + | + | Shields, exotic |
| TKM | WIP | — | — | — | — | — |
| House Ordos | Manipulation | − | + | · | · | Stealth, mercenaries |
| Ixians | Tech / Innovation | · | · | + | + | Advanced tech |
| Terran | Mobility / Reactive | − | + | · | · | Stim, siege, bunkers |
| Protoss | Elite / Psionic | + | − | + | + | Shields, warp-in |
| Zerg | Swarm / Bio Growth | − | + | · | − | Creep, mutation, burrow |
| Humans WC2 | Defensive / Versatile | + | − | · | + | Support magic, paladins |
| Orcs WC2 | Aggressive / Brute | + | − | + | − | Bloodlust, heavy melee |
| Eden | Colony / Tech | · | · | + | + | Colony management |
| Plymouth | Adaptation / Survival | · | + | · | · | Resourcefulness |

---

## 7. Faction Doctrine and Complete Roster

### 7.1 Complete Faction Roster

From [FACTIONS.md](https://github.com/cameo-mod/Cameo-mod/blob/master/docs/FACTIONS.md):

| # | Faction | Universe | Source Game | Signature Mechanic | Primary Weakness |
|---|---------|----------|------------|--------------------|--------------------|
| 1 | GDI TD | C&C Tiberian | Tiberian Dawn | Ion Cannon + Combined Arms | Slow expansion |
| 2 | Nod TD | C&C Tiberian | Tiberian Dawn | Stealth Battlefield | Detection + heavy forces |
| 3 | GDI TS | C&C Tiberian | Tiberian Sun | Heavy Tech, EMP | Economy fragility |
| 4 | Nod TS | C&C Tiberian | Tiberian Sun | Subterranean Warfare | Frontal weakness |
| 5 | Forgotten | C&C Tiberian | Tiberian Sun | Salvage / Adaptation | Tech ceiling |
| 6 | CABAL | C&C Custom | Tiberian Sun | Backup Systems (reanimation) | EMP, energy |
| 7 | Allies RA1 | Red Alert | Red Alert 1 | Naval / Air / Chrono | Ground armor |
| 8 | Soviets RA1 | Red Alert | Red Alert 1 | Tesla / Heavy Tanks | Speed, air |
| 9 | Japan RA1 | Red Alert | Red Alert 1 | Naval / Transform | Economy |
| 10 | Allies RA2 | Red Alert | Red Alert 2 | Chrono / Tech | Brute force |
| 11 | Soviets RA2 | Red Alert | Red Alert 2 | Tesla / Desolator | Speed, tech |
| 12 | Yuri | Red Alert | Red Alert 2 | Mind Control / Psychic | Detection |
| 13 | Asian Alliance | Cameo Original | RA2 Mod | Multi-domain ops | Fragmented if disrupted |
| 14 | Steel Consortium | Cameo Original | RA2 Mod | Promotion tree (Support + Military branches), shields, quantum weapons | Early game, unit cost |
| 15 | Latin Syndicate | Cameo Original | RA2 Mod | Black Market / Sabotage | Frontal weakness |
| 16 | Naxis | Cameo Original | RA2 Mod | Alien / Dimensional | Economy, fragility |
| 17 | Schwarzer Mond | Cameo Original | RA2 Mod | Cryptofascism (stealth + fear) | Direct confrontation |
| 18 | FutureTech | Cameo Original | RA2 Mod | Experimental + shields | Cost, fragility |
| 19 | TKM | Cameo Original | RA2 Mod | WIP | WIP |
| 20 | House Ordos | Dune | Dune 2000 | Manipulation / Mercenaries | Direct force |
| 21 | Ixians | Dune | Dune 2000 | Tech / Innovation | Durability |
| 22 | Terran | StarCraft | StarCraft | Reactive Production / Mobility | Swarm matchups |
| 23 | Protoss | StarCraft | StarCraft | Warp / Psionic | Cost, numbers |
| 24 | Zerg | StarCraft | StarCraft | Biological Growth | Splash, attrition |
| 25 | Humans WC2 | Warcraft | Warcraft 2 | Defensive / Support Magic | Aggression |
| 26 | Orcs WC2 | Warcraft | Warcraft 2 | Aggressive / Brute | Tech, ranged |
| 27 | Eden | Outpost | Outpost 2 | Colony / Tech | Military fragility |
| 28 | Plymouth | Outpost | Outpost 2 | Adaptation / Survival | Tech ceiling |

### 7.2 The "One Crazy Thing" Rule

> **Every faction gets one primary extraordinary mechanic. Supporting mechanics must reinforce it.**

### 7.3 CABAL Promotion System

CABAL uses a **two-branch four-tier promotion system** (not doctrines — promotions are rank-gated, not mutually exclusive research picks):

| Branch | Tier 1 | Tier 2 | Tier 3 | Tier 4 |
|--------|--------|--------|--------|--------|
| **Cyborg** | Devout | Ascended | Beholder | Cyborg Commando V2 |
| **Spider** | Spider CNC4 | Heavy Reaper | Widow | Core Defender |

Each promotion unlocks a new unit. Destroyed vehicles spawn high-HP backup wrecks that can be repaired and reanimated. Working backup actors: Manticore, Artillery Spider, Tarantula, Avatar, Widow.

### 7.4 Schwarzer Mond Design Constraints

Every unit must have at least two upgrade hooks, with Cryptofascism counting as one. All combat units use `^PromotionUnitBuff`. Design documented in `DESIGN.md §18`.

---

## 8. Roster Tiering

### 8.1 Three-Tier Roster

| Tier | Description | Balance Bar | AI Support |
|------|-------------|------------|------------|
| **Core Ladder** | Fully class-tagged, anchors signed, AI compositions, role coverage | Strict | Full AI |
| **Showcase** | Fun, slightly swingy, labeled | Soft | Basic AI |
| **Museum / WIP** | Incomplete (e.g. TKM, Scrin structures) | None | Minimal |

### 8.2 No Mirror Factions

Every faction and every individual unit gets distinct stats (uniqueness law — no two units may share a stat value). WC2 Humans and Orcs are differentiated: Humans lean defensive/versatile/support-magic; Orcs lean aggressive/brute.

---

## 9. Crossover System

### 9.1 Controlled Crossover Loadout

```
1. CORE FACTION → Economy, base, production, tech, identity
2. CAMEO LICENSE → One limited foreign technology
3. CAMEO RELIC → One powerful artifact
4. ALLIED DETACHMENT → 3 foreign units as supplement
```

### 9.2 Complementary Crossover Design

| Combination | Adds | Does NOT Add |
|-------------|------|-------------|
| GDI + Protoss | Teleport, shields, mobility | No better heavy tank |
| Nod + Zerg | Bio infantry, regeneration | No better stealth |
| CABAL + Ixians | Alien structures, tech | No better backup |

### 9.3 Three Levels of Crossover

| Level | Freedom | Purpose |
|-------|---------|---------|
| Competitive | Core + 1 License | Balance, esports |
| Convergence | Full loadout | Experimentation |
| Campaign | Story-driven | Narrative |

---

## 10. Combat Framework

### 10.1 Soft Counter System

| Counter Level | Kill Ratio |
|-------------|-----------|
| Strong Counter | 3:1 |
| Soft Counter | 1.5:1 |
| Neutral | 1:1 |
| Bad Matchup | 1:1.5 |
| Hard Counter | 1:3+ |

### 10.2 Damage Formula

\[
D_{\text{effective}} = D_{\text{base}} \cdot (1 + m_{\text{upgrade}}) \cdot m_{\text{type}} \cdot m_{\text{cover}} \cdot m_{\text{height}} \cdot m_{\text{tier}}
\]

### 10.3 Combat Power (CP)

\[
\boxed{CP = c_m \cdot M + c_e \cdot E + c_t \cdot T + c_s \cdot S + c_p \cdot P}
\]

### 10.4 Combined Arms

```
Frontline → Heavy Armour → Anti-Armour → Anti-Air → Artillery → Support → Scout/Detection
```

### 10.5 Fairness Contract

Any extreme mechanic must have: **Cost + Telegraph + Counter + Risk + Opportunity Cost.**

### 10.6 Cover and Suppression

\[
\text{Suppression} = \min\left(1.0,\; \frac{D_{\text{incoming}} \cdot t}{\text{HP}_{\text{unit}} \cdot \tau}\right)
\]

---

## 11. Balance Pipeline

### 11.1 Philosophy

The balance system answers: *"Are these units equivalent in strategic value?"* — not *"Do all factions have the same statistics?"*

### 11.2 The Verified Price Law

\[
P(h,d) = \frac{3(h+d) + 4hd + 2}{12}
\]

### 11.3 Band Calibration

```yaml
band_semantics: price_ratio
floor: 0.50
sweet_lo: 0.75
anchor: 1.00
sweet_hi: 2.50
ceiling: 4.00
extreme_exception: 7.50
```

### 11.4 Stat Granularity (from ROADMAP.md)

- HP: 1000 steps for every type
- Speed: steps of 1
- `ScaledSelfHeal`: linear ramp over 125 ticks
- Uniqueness law: no two units may share a stat value

### 11.5 Multi-Level Balance Measurement

| Level | Metrics |
|-------|---------|
| Match | Win rate, game length, comeback rate |
| Army | Composition diversity, role diversity, trade ratio |
| Combat | TTK, counter effectiveness, ability usage |
| Strategy | Tech timings, expansion count, scouting quality |
| Player | Perceived fairness, decision diversity, rematch intention |

### 11.6 A/B Test Protocol

- Minimum 16 matches per arm, mirror matches only, swapped spawns
- Separate shards per faction pair — never cross-pairing
- Parallel execution, early stopping when verdict cannot flip

### 11.7 Known Pipeline Issues

| Issue | Priority |
|-------|----------|
| Negative DPS (healing summed as damage) | P0 |
| Transforming units (IFV shared DPS) | P1 |
| Coverage gaps (zero tagged members) | P1 |

---

## 12. Tech Progression and Commander Tree

### 12.1 Universal Tier Structure

```
Tier 1 (Foundation) → Tier 2 (Development) → Tier 3 (Advanced) → Tier 4 (Apex)
```

### 12.2 A/B Doctrine Upgrades

Upgrades create decisions: **A or B**, not **+10% +10% +10%**.

### 12.3 Escalation Phases

| Phase | Focus |
|-------|-------|
| Early | Skirmishes, map control |
| Mid | Heavy vehicular brawls, combined arms |
| Late | Titans, superweapons, comeback tools |

### 12.4 Commander Tree

- Promotion Points unlock abilities, upgrades, units
- Cross-universe abilities gated behind commander tree
- Team-wide upgrades for coordinated play

### 12.5 Campaign Progression: Crossover Consequences

```
Defeat Zerg → Unlock: Biological Regeneration
Defeat Protoss → Unlock: Warp Deployment
Defeat Nod → Unlock: Stealth Doctrine
Defeat Yuri → Unlock: Mind-Control Counter-technology
```

---

## 13. Game Modes

| Mode | Priority | Description |
|------|---------|-------------|
| 1v1 / 2v2 Ranked | **P1** | Core factions, strict balance, ladder |
| Chaos FFA | Entertainment | Full roster, swingy, 4–8 players |
| Universe Clusters | Practice | C&C Night, SC Night, Dune Night |
| Convergence Mode | Experimentation | Maximum crossover loadout |
| Singularity Campaign | **P1** | Multiverse map, three-mission arcs |
| Co-op Campaign | **P2** | 2-player co-op with combined doctrines |
| Co-op vs AI | Teamwork | Smart bots without cheats |
| Challenge | Extreme | Boss gauntlet |
| Sandbox | Creativity | No win condition |
| Special Events | Variety | Wild imbalance modes |

### 13.1 Singularity Campaign

From [VISION.md](https://github.com/cameo-mod/Cameo-mod/blob/master/docs/design/VISION.md):

1. **Strategic layer — Multiverse Map** (Empire at War style)
2. **Pre-Battle — Army Building** (Gates of Hell style)
3. **Tactical layer — Three-mission arc:**

| Mission | Description |
|---------|-------------|
| 1. Beachhead | Establish foothold against T1/T2 |
| 2. Asymmetric Objective | Faction-specific objective |
| 3. Artifact/Commander Assault | Fully unlocked, fortified enemy |

Campaign V1 proposed dimensions: GDI, Yuri, Consortium, Schwarzer Mond, Ordos, Zerg.

---

## 14. Map and Intel Systems

### 14.1 Map Structure

```
MAIN BASE
    │
    ├── Expansion A
    │
Central Conflict Zone
   / \
  Flank A    Flank B
  │              │
Tech Site    Resource Site
```

### 14.2 Terrain

| Terrain | Movement | Cover | Visibility |
|---------|----------|-------|------------|
| Open | 100% | 0% | Normal |
| Forest | 70% | +50% | −2 tiles |
| Building | 60% | +75% | Blocks |
| Hill | 80% | +30% | +2 tiles, +15% range |
| Water | 30% (amphibious) | 0% | Blocks |
| Tiberium | 50% | −20% | Damages non-C&C |
| Road | 120% | 0% | Normal |

### 14.3 Fog of War

```
Observed → Remembered → Inferred → Predicted → Decision
```

### 14.4 Anomalies

| Anomaly | Effect | Mode |
|---------|--------|------|
| Tiberium Bloom | New Tiberium field | All |
| Spice Storm | Vision reduced, spice grows | Dune maps |
| Psychic Storm | Random unit control confusion | Convergence |
| Warp Anomaly | Temporary teleport point | Convergence |
| Temporal Distortion | Unit slow-motion | Challenge |
| Rogue AI Event | Neutral hostile CABAL units | FFA |
| Dimensional Breach | Foreign units appear | Campaign |

---

## 15. AI Framework

### 15.1 Architecture Principles

From [AI_ARCHITECTURE.md](https://github.com/cameo-mod/Cameo-mod/blob/master/docs/design/AI_ARCHITECTURE.md) and [AI_MASTER_PLAN.md](https://github.com/cameo-mod/Cameo-mod/blob/master/docs/design/AI_MASTER_PLAN.md):

| Principle | Status | Description |
|-----------|--------|-------------|
| No economic cheats | Realized | Difficulty through competence |
| Fog-honest observation | Target | AI sees only visible; decaying memory |
| One owner per decision | Realized | Master provides hints; specialists decide |
| One owner per actor | Realized | `IBotUnitLeases` |
| Orders are boundary | Realized | Unsynced → synced |
| Learning separated | Realized | Write-only logs; frozen per match |
| A/B testing | Realized | Measured matches validate changes |

### 15.2 Acceptance Test (2026-09-27)

> "The new Frankenstein monster AI with no map-wide vision [must be] able to beat our existing AI of the same difficulty level with map-wide vision … every time with any faction."

### 15.3 Six Parent Projects

| Parent | Status |
|--------|--------|
| OpenRA | Stack foundation (harvester, support powers, repair, MCV) |
| Romanov's Vengeance (RV) | Fully merged (140 protected symbols) |
| Cameo | Phases 1–7a, combined arms foundations |
| Combined Arms (CA) | Fully merged (140 protected symbols) |
| Crystallized Nexus (CN) | 1 of ~16 modules running |
| Fransbot | 8 of 24 modules running as record-only on `hard` |

### 15.4 Dynamic Personality Switching

| Personality | Behavior | Instead of |
|-------------|----------|------------|
| Rush | Earlier pressure, aggressive scouting | +50% income |
| Turtle | Defensive investment, artillery | +30% HP |
| Guerrilla | Multiple fronts, raids | +20% damage |
| Tech | Delayed pressure, tech investment | Cheat production |
| Steamroller | Sustained frontal pressure | Stat bonuses |
| Expansion | Aggressive economic expansion | Resource cheats |

### 15.5 Difficulty Scaling

| Tier | Reaction (ms) | Horizon | Action Budget |
|------|---------------|---------|---------------|
| Beginner | 500 | Low | 30% |
| Easy | 300 | Low | 50% |
| Medium | 150 | Medium | 70% |
| Hard | 80 | High | 85% |
| Brutal | 30 | High | 95% |
| Insane | 10 | Maximum | 100% |

### 15.6 Critical Path

CA-1 → CA-3 → CA-4 → UT (utility strategist) → DI/TC/§13, with ZG → IM (CN's chokepoint map) feeding UT. UT is the largest single item and the point where the six parents truly become one bot.

### 15.7 AI Roadmap

1. Phase 1: MatchLog (record-only)
2. Phase 2: Snapshot + master module
3. Phase 3: Personality switching + target selection
4. Phase 4: Fog gating + scouting
5. Phase 5: Multiplayer team coordination
6. Phase 6: Offline learning

---

## 16. Multiplayer and Competitive Infrastructure

### 16.1 Matchmaking

| System | Description |
|--------|-------------|
| Skill-based | ELO or Glicko |
| Faction drafting | Pick/ban for tournaments |
| Ranked ladders | Separate for solo, team, special events |

### 16.2 Team Play

| Feature | Description |
|---------|-------------|
| Team bonuses | Team-wide upgrades via commander tree |
| Cross-faction coordination | Economy + military specialization |
| Shared objectives | Map-based objectives promote teamwork |
| Team commander | Each player: Ground / Air / Raid / Tech |

### 16.3 Tournament Infrastructure

| Feature | Description |
|---------|-------------|
| Spectator tools | Observer modes, replays, casting overlays |
| Custom lobbies | Detailed game setup |
| Showcase Tournament | Swiss stage + double-elimination bracket |
| AI tournaments | AI-only competitions |

### 16.4 Competitive Roadmap

| Phase | Task | Priority |
|-------|------|----------|
| 1 | Set up 1v1 ladder servers | P1 |
| 2 | Implement ELO/Glicko tracking | P1 |
| 3 | Build spectator/replay system | P2 |
| 4 | Host first Cameo Showcase Tournament | P2 |
| 5 | Establish regular tournament cadence | P3 |

---

## 17. UX and Codex

### 17.1 Semantic Unit Cards

```
┌──────────────────────────────────────┐
│  MAMMOTH TANK                         │
│  Heavy Tank                           │
│  ██████████ Armour (Heavy)            │
│  Strong vs: Infantry, Light, Structures│
│  Weak vs: Tank Destroyers, Artillery   │
│  Special: Siege-grade firepower        │
└──────────────────────────────────────┘
```

### 17.2 The Cameo War Codex

Built-in reference: every faction, unit role, counter relationships, weapon types, technologies, foreign technologies, campaign discoveries, lore, universe relationships.

### 17.3 V43 Gridlock Icon System

8×6 icon grid. C&C brutalism + SC2 precision + Dune desert-grit. Red-orange Steel Consortium / blue-white Allied Chrono split.

### 17.4 Universal Readability

| Element | Requirement |
|---------|-------------|
| Silhouette | Infantry/tank/artillery/AA/scout/air distinguishable |
| Sound | Weapon families have recognizable signatures |
| Effects | Shield ≠ armor ≠ bio damage |
| UI Accent | Each faction has own color; role icons universal |
| Tooltips | Detailed mechanics and cross-faction interactions |
| Tutorials | Interactive tutorials per universe (P1) |

---

## 18. Lore Framework

### 18.1 The Convergence

Collapsing timelines draw Tiberian, Red Alert, Dune, StarCraft, Warcraft, Outpost, and Cameo-original timelines into one unstable singularity ([VISION.md](https://github.com/cameo-mod/Cameo-mod/blob/master/docs/design/VISION.md)).

### 18.2 Cameo Originals as Connective Tissue

| Faction | Bridge Function |
|---------|----------------|
| Steel Consortium | Promotion versatility + industrial power |
| FutureTech | Energy + experimental AI |
| Schwarzer Mond | Stealth + occult/sci-fi |
| CABAL | Machine redundancy + cyber |
| Asian Alliance | Coordinated multi-domain operations |
| Latin Syndicate | Black market + sabotage |
| Naxis | Alien + dimensional tech |

### 18.3 The "Cameo Moment"

> Nod Stealth Tanks bypass the perimeter. GDI Mammoths hold the center. Zerg auxiliaries emerge behind the artillery. Protoss teleport into the rear. A Yuri mind controller steals a Mammoth. Chronosphere triggers. Counterattack destroys the base.

Balanced matches that generate stories.

---

## 19. Universe Adapter Sheets

### StarCraft

| Dimension | Cameo Adaptation |
|-----------|------------------|
| Economy | Credits + Vespene/Biomass + Supply |
| Base | Drop-in; production type as signature |
| Tech | Tier system, A/B upgrades |
| Signature | Bio Growth / Reactive Production / Warp |

### Command & Conquer

| Dimension | Cameo Adaptation |
|-----------|------------------|
| Economy | Credits + Tiberium |
| Base | MCV expansion as C&C signature |
| Signature | Combined Arms / Stealth Battlefield |

### Dune

| Dimension | Cameo Adaptation |
|-----------|------------------|
| Economy | Credits + Spice |
| Signature | Manipulation (Ordos) / Innovation (Ixians) |

### Warcraft 2

| Dimension | Cameo Adaptation |
|-----------|------------------|
| Economy | Credits + Materials + Supply |
| Base | Drop-in; NO mirror factions |
| Signature | Defensive/support (Humans) / Aggressive/brute (Orcs) |

### Outpost 2

| Dimension | Cameo Adaptation |
|-----------|------------------|
| Economy | Credits + colony management |
| Signature | Eden: colony/tech / Plymouth: adaptation/survival |

---

## 20. Anti-Patterns

| Anti-Pattern | Why It Kills the Dream |
|--------------|----------------------|
| Infinite faction merge without tiers | Nobody learns the game |
| Balancing only HP/DPS | Loses experiential design |
| Copy-paste "tank line" across universes | Asymmetry dies |
| Superweapons without tells | Feels random |
| AI strength = cheaper tanks + map hack | Players learn wrong lessons |
| Lore excuses for broken interactions | Crossover becomes unreadable |
| Treating all modes as one balance target | Competitive and Chaos both suffer |
| Mirror factions | Dull — Cameo differentiates |
| Shipping features before stability | Crashes kill the community |
| Ignoring press and distribution | Players can't find the game |

---

## 21. Community, Modding, and Legal

### 21.1 Modding Tools

| Tool | Description |
|------|-------------|
| OpenRA API | Custom units, abilities, maps, AI |
| [IFV Launcher](https://github.com/cameo-mod/cameo-ifv) | Incremental patching (zsync) |
| Balance editors | Adjust unit stats, abilities, tech trees |

### 21.2 Community Pipeline

| Feature | Description |
|---------|-------------|
| GitHub workflow | PRs, code reviews, issue tracking |
| CI/CD | Automated builds, testing, packaging |
| Discord | Central hub — weekly play nights, faction guides |
| ModDB | Active page with regular updates |
| Content creator program | Builds to YouTubers/streamers |

### 21.3 Legal

| Consideration | Approach |
|--------------|----------|
| Fan mod legality | Non-commercial ([CyberPost](https://cyberpost.co/are-fan-mods-legal/)) |
| Copyright | Clear disclaimers, respond to takedown |
| Licensing | Engine GPLv3; artwork CC BY-NC |
| Original content | Incentivize original assets |

---

## 22. Implementation Plan and Roadmap

### 22.1 Six Development Tracks

| Track | Description | Status |
|-------|-------------|--------|
| Track 1: Mechanical Truth | Weapon structure, anchors, boot gates | In progress |
| Track 2: Identity Lock | Core faction one-pagers | Next |
| Track 3: Role Matrix | Soft counters at class level | After structure debt |
| Track 4: Modes & Maps | Ranked-lite vs Chaos; map tags | Parallel |
| Track 5: Spectacle with Rules | Heroes, SW, promotions: counter, tell, cost | After Track 3 |
| Track 6: AI & Social | Team coordination, match logs | Phased |

### 22.2 Catch-Up Timeline (Aligned with §3)

| Phase | Weeks | Focus | Gate |
|-------|-------|-------|------|
| Phase 1: Stop the Bleeding | 1–6 | Crash fixes, boot gate, performance, README | G1–G3 pass |
| Phase 1: Ship Expectations | 7–16 | Ladder, in-game doctrine system, tutorials, first campaign mission, v0.9 release | First tournament |
| Phase 2: Crossover Advantage | 17–32 | Full campaign, crossover loadout, universe clusters, co-op | Press coverage |
| Phase 3: Community Growth | 33–48 | Tournaments, content creators, Snap Store, Discord growth | 100+ concurrent |

### 22.3 Release Milestones

| Milestone | Description |
|-----------|-------------|
| v0.9 | First tagged release — stability + basic ladder |
| v1.0 | Full release — campaign, co-op, crossover loadout |
| v1.x | Regular updates — new factions, maps, balance patches |

### 22.4 Immediate Priorities

1. **Fix crashes** — the #1 community killer
2. **Set up 1v1 ladder** — competitive scene anchor
3. **Ship first campaign mission** — proof of concept
4. **Rewrite README** — remove "extremely buggy" language
5. **First tagged release** — shows momentum
6. **Press outreach** — send builds to gaming media

### 22.5 Data Architecture

```
cameo-mod/
├── mods/
│   ├── cameo_core/           # Universal rules
│   │   ├── rules/
│   │   ├── maps/
│   │   └── ai/
│   ├── ContentPacks/         # Faction definitions
│   │   ├── TiberianDawn/     # GDI, Nod
│   │   ├── TiberianSun/      # GDI, Nod, Forgotten, CABAL
│   │   ├── RedAlert1/        # Allies, Soviets, Japan
│   │   ├── RedAlert2/        # Allies, Soviets, Yuri + Cameo originals
│   │   ├── Dune/             # Ordos, Ixians
│   │   ├── StarCraft/        # Terran, Protoss, Zerg
│   │   ├── Warcraft2/        # Humans, Orcs
│   │   └── Outpost2/         # Eden, Plymouth
│   ├── crossover/            # Loadout definitions
│   └── campaign/             # Singularity campaign
├── tools/
│   ├── reference_distribution.py
│   ├── formula.py
│   └── audit/
├── tests/
│   └── nuclear_winter/       # A/B test harness
└── docs/
    └── design/
```

---

## 23. Appendix

### 23.1 Example Loadouts

**Steel Consortium (Support Branch) + Protoss Warp**

```
CORE: Steel Consortium — Support LVL4 (Cobra, Quantum Tanks)
LICENSE: Protoss Warp Technology
RELIC: Khaydarin Crystal
DETACHMENT: Zealot, Stalker, Observer
```

**Nod + Zerg Bio**

```
CORE: Nod — Stealth + Raids
LICENSE: Zerg Biological Technology
RELIC: Tiberium Mutation
DETACHMENT: Zergling, Baneling, Overlord
```

### 23.2 Balance Simulation (Python)

```python
def simulate_battle(unit_a, unit_b, iterations=10000):
    a_wins, b_wins = 0, 0
    for _ in range(iterations):
        hp_a, hp_b = unit_a.hp, unit_b.hp
        while hp_a > 0 and hp_b > 0:
            hp_b -= calculate_damage(unit_a, unit_b)
            if hp_b <= 0: a_wins += 1; break
            hp_a -= calculate_damage(unit_b, unit_a)
            if hp_a <= 0: b_wins += 1; break
    return a_wins / max(a_wins + b_wins, 1)  # Target: 0.35–0.65
```

### 23.3 What Cameo Should Ultimately Feel Like

> A unified multiversal RTS where every universe contributes a different philosophy of warfare, and the player learns to manipulate the interactions between those philosophies.

Combined Arms proved that a polished OpenRA crossover can be "the best new C&C game in a decade." Cameo asks the next question: what happens when you add StarCraft, Dune, Warcraft, and original factions to that formula? The answer is dream matches that no other game can offer — Zerg vs Soviets, Nod vs Protoss, Ordos vs CABAL — on one ruleset with a serious balance pipeline.

The ultimate Cameo is not the RTS with the most units. It is the RTS where 28+ factions across 8 source universes remain understandable because they share one strategic language; the AI learns the same world the player sees; the campaign makes the multiverse mechanically meaningful; and every match has the potential to produce a "only in Cameo" moment.

---

## 24. Sources

### Cameo Project

1. Cameo README — [GitHub](https://github.com/cameo-mod/Cameo-mod)
2. Cameo DESIGN.md — [GitHub](https://github.com/cameo-mod/Cameo-mod/blob/master/docs/DESIGN.md)
3. Cameo VISION.md — [GitHub](https://github.com/cameo-mod/Cameo-mod/blob/master/docs/design/VISION.md)
4. Cameo AI_ARCHITECTURE.md — [GitHub](https://github.com/cameo-mod/Cameo-mod/blob/master/docs/design/AI_ARCHITECTURE.md)
5. Cameo AI_MASTER_PLAN.md — [GitHub](https://github.com/cameo-mod/Cameo-mod/blob/master/docs/design/AI_MASTER_PLAN.md)
6. Cameo ROADMAP.md — [GitHub](https://github.com/cameo-mod/Cameo-mod/blob/master/docs/design/ROADMAP.md)
7. Cameo FACTIONS.md — [GitHub](https://github.com/cameo-mod/Cameo-mod/blob/master/docs/FACTIONS.md)
8. Cameo ARMOR_SYSTEM.md — [GitHub](https://github.com/cameo-mod/Cameo-mod/blob/master/docs/design/ARMOR_SYSTEM.md)
9. Cameo FACTION_IDENTITY.md — [GitHub](https://github.com/cameo-mod/Cameo-mod/blob/master/docs/design/FACTION_IDENTITY.md)
10. Cameo Soviet upgrades.yaml — [GitHub](https://github.com/cameo-mod/Cameo-mod/blob/master/mods/cameo/ContentPacks/RedAlert/Soviets/yaml/upgrades.yaml)
11. Cameo CABAL promotions.yaml — [GitHub](https://github.com/cameo-mod/Cameo-mod/blob/master/mods/cameo/ContentPacks/TiberianSun/CABAL/yaml/promotions.yaml)
12. Cameo Steel Consortium promotions.yaml — [GitHub](https://github.com/cameo-mod/Cameo-mod/blob/master/mods/cameo/ContentPacks/RedAlert2Mod/Consortium/yaml/promotions.yaml)
13. Cameo-IFV Launcher — [GitHub](https://github.com/cameo-mod/cameo-ifv)

### Combined Arms

14. Polygon — "Best new C&C game in over a decade": https://www.polygon.com/pc/503559/command-and-conquer-combined-arms-mod/
15. PCGamer — "Five factions mashup": https://www.pcgamer.com/this-mod-mashes-up-five-factions-from-command-and-conquers-past-into-one-giant-brawl/
16. PCGamesN — "New C&C game": https://www.pcgamesn.com/command-and-conquer/combined-arms
17. GamePressure — "Free standalone RTS": https://www.gamepressure.com/newsroom/free-rts-that-combines-command-conquer-universes-with-new-mission/z68b66
18. ModDB — Combined Arms: https://www.moddb.com/mods/command-conquer-combined-arms
19. RoWatcher — CA player count: https://rowatcher.com/games/10452420697/ea-combined-arms
20. SpawnScope — CA player count: https://spawnscope.com/games/combined-arms/
21. Snapcraft — CA on Linux: https://snapcraft.io/openra-combined-arms
22. OpenRA Forum — CA Discord: https://forum.openra.net/viewtopic.php?t=21795
23. Reddit — CA sub-factions: https://www.reddit.com/r/commandandconquer/comments/1i5ny3e/combined_arms/
24. GamingOnLinux — CA v1.08: https://www.gamingonlinux.com/2026/01/the-excellent-free-command-conquer-combined-arms-gets-more-missions-and-co-op/
25. OpenRA News — CA ModDB ranking: https://www.openra.net/news/release-20231010/

### External RTS Design

26. Liquipedia — SC2 Resources: https://liquipedia.net/starcraft2/Resources
27. Simon Halliday — SC2 Asymmetrical Design: https://simonhalliday.com/2019/09/04/starcraft-ii-a-study-in-asymmetrical-design/
28. Hayao Design Codex — Faction Asymmetry: https://hayao.dev/docs/codex/system-faction-asymmetry
29. AoE2 DB — Counter Matrix: https://aoe2db.com/counters
30. arXiv — Generating RTS Units: https://arxiv.org/pdf/2212.03387.pdf
31. arXiv — RL for RTS AI: https://arxiv.org/pdf/2105.13807v3
32. OpenRA Docs: https://docs.openra.net/
33. OpenTelemetry: https://opentelemetry.io/docs/concepts/signals/metrics/
34. Arctic7 — Transmedia: https://www.arctic7.com/post/transmedia-storytelling-for-franchises
35. CyberPost — Fan Mod Legality: https://cyberpost.co/are-fan-mods-legal/

### Design Research (Merged)

36. Grok 4.5 (xAI) — Design research advisor
37. Microsoft Copilot — Research analyst
38. ChatGPT — One rules language, many dialects
39. Perplexity AI — Balance pipeline reviewer
40. Deep Research Report — Ultimate Crossover RTS
