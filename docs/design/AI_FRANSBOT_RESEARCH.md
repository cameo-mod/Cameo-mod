# Fransbot research: what Cameo can take, and the phase-6+ handoff

_Written 2026-09-26 by Claude-Local (coordinator) for **Devin-Cloud (AI lane)** and the maintainer.
Source: the maintainer's conversation with fransotto, author of FransBots, on 2026-09-24, plus
Cameo's own code on master `91f865585`. This document **amends** [`AI_ARCHITECTURE.md`](AI_ARCHITECTURE.md)
§10.6 phase 6 and adds phases 6c–10. It does not replace that document. Where the two disagree on
something already built, `AI_ARCHITECTURE.md` and the code win._

---

## 0. TL;DR (read this if you read nothing else)

1. **The Fransbot source is not available yet.** `github.com/f850484/OpenRA-Fransbot` (tag
   `V1.29.19-RC`) returns **404**: the account `f850484` was created 2026-09-19 and has **0 public
   repositories**, so the repo is private. The zip fransotto sent through `send.mni.li` has
   **expired** (`/api/exists/c5e39f5134e55193` returns `Not Found`). No Wayback snapshot exists.
   Only the older **yaml-only** FransBots releases are public (ModDB `mods/fransbots`, OpenRA forum
   topic 21988), and both sites serve JS bot-challenges, so they cannot be scraped. **§6 (phase F0)
   waits on the maintainer getting access.** Nothing else in this document does.
2. **What fransotto described is the design Cameo's phase 6 already calls for, but further along.**
   Phase 6 is *"fogged observation + ScoutBotModule"* (`AI_ARCHITECTURE.md` §10.6). It was ordered on
   PR #455 on 2026-09-23 and **has not started**: there is no branch, and no `ScoutBotModule` or
   `UseFoggedObservation` anywhere in the tree. Fransbot says it already runs a bot with no global
   vision, on any map, using a **per-location memory of the cash value it has seen**. That memory
   only refreshes when the area is re-scouted, and every order (Secure / Raid / Recon / Retreat) is
   a **risk-versus-reward test over it**. That is a concrete, proven shape for phase 6. Build it now
   from the description; diff against the source when it arrives.
3. **Cameo already has the currency.** `MasterAiBotModule` already builds per-enemy profiles in
   **cash value** (`ValuedInfo.Cost`, `BotSituation.cs:847`), split by infantry, vehicle, air, naval
   and defence. It reads them from omniscient `World.Actors`, though (`BotSituation.cs:219`), and it
   keeps no spatial memory.
4. **The squads are omniscient too, not just the master.** Making only the master fogged leaves
   `SquadManagerBotModuleCA.FindClosestEnemy` scanning `World.Actors`
   (`OpenRA.Mods.CA/Traits/BotModules/SquadManagerBotModuleCA.cs:354-372`), so artillery squads keep
   their max-range knowledge. Phase 6 is not finished until the squad target scans read the memory.
5. **How much of the bot can we take?** The **algorithms**: value memory, risk gate, route risk,
   recon targeting, beacon response, harvester estimation. Those are the most valuable thing in it.
   The **files** are probably not drop-in. Fransbot is a fork of stock OpenRA (2-side RA, the Common
   `SquadManagerBotModule`), while Cameo runs the CA forks plus its own master module. The yaml
   build orders and compositions are RA-specific: **do not take them.** Cameo has 25 factions, and
   the only part of Fransbot that scales to 25 factions is the part that *"reads the stats of the
   unit files"*.

---

## 1. Provenance: what we know, and how sure we are of it

### 1.1 From the conversation (claims by the author, **not yet verified against source**)

| # | Claim (fransotto, 2026-09-24) | Confidence |
|---|---|---|
| C1 | Worked on bots for 3–4 years, **yaml only** at first; now C# written by ChatGPT/Codex from replay + screenshot + a **custom debug log** loop | author's account |
| C2 | Runs **without global vision**, on **any map including the map generator**, with fog of war | author's claim, the key one |
| C3 | Core: the bot keeps, per place, **the cash value of units and buildings it has seen**. It stays in memory until that area is scouted again | author's claim, repeated three times |
| C4 | Decisions are risk/reward on that memory: *"it does not send a squad with value 1000 against an area with value 5000"* | author's claim |
| C5 | Uses **native OpenRA orders**: Secure, Raid, Recon, Retreat. Routing is also a "risk-cash" analysis | author's claim |
| C6 | Reads **unit stats from the rules files**, with no per-unit hand-tuning (his hard rule) | author's claim, the most portable part |
| C7 | Understands **air vs AA**, but **not yet** "rocket soldiers are dangerous to tanks" (no general counter model) | author's own limitation |
| C8 | 10 bots on a Europe map, transports, raids and supplies, **without lag**. Close to or better than stock Normal AI | author's claim |
| C9 | **Beacons**: an allied beacon draws troops, a beacon on your own building draws a supply truck (**in progress**) | author's claim, WIP |
| C10 | Estimates every player's **harvester count**. Simple team chat ("I need cash", "enemy weak here", "spot has AA") is **paused**, next release | author's claim, not shipped |
| C11 | Several personalities with 6–7 `SquadManagerBotModule` instances each, which *"became quite hard to keep track on"* | author's account |
| C12 | **Island expansion and airdrops.** The maintainer asked how island expansion works. fransotto never answered the mechanism in the conversation, but lists "transports" in C8 | **unconfirmed**: must come from the source (§6) |

Public trail: FransBots on ModDB (`moddb.com/mods/fransbots`, e.g. `fransbots20251116`) and the
OpenRA forum (topics 21565 "FransBots 20231220" and 21988 "FransBots 20260122!"). Search-engine
summaries of those pages describe **10 AIs, yaml-only, no cheats, only build orders, squad
compositions and target priorities changed**, with specialist units (engineers, thieves, medics,
Tanya, nukes). That is the **old** line. The C# `V1.29.x` line described above is the new, private one.

### 1.2 Licence and consent

Fransbot is a fork of OpenRA, which is GPLv3, so its C# is GPLv3 and compatible with this repo.
fransotto also said he wants to *"give back to the OpenRA community"* and *"cannot claim any
ownership"*. **Still: credit him by name** in any ported file header and PR body
(`Ported from FransBots by fransotto, <commit/tag>`), and do not import anything until the
maintainer has the source with his consent. A private repo is not consent to copy.

### 1.3 Cameo facts (verified on master `91f865585`, 2026-09-26)

| Fact | Evidence |
|---|---|
| Bot omniscience is **code-level, not a rule**: no map reveal or bot shroud override in `player.yaml` or `ai.yaml` | grep of `RevealsMap`/`ExploredMap`/`Shroud` in `mods/cameo/rules/player.yaml`, `mods/cameo/ai/ai.yaml` |
| Master snapshot scans all actors | `OpenRA.Mods.Cameo/Traits/BotModules/BotSituation.cs:219` `player.World.Actors.Where(...)` |
| Value = production cost | `BotSituation.cs:847` `ValuedInfo.Cost` |
| Per-enemy profile already has Army / Inf / Veh / Air / Naval / Defence / Pressure values | `BotSituation.cs:24-38`, built at `:388-430` |
| Squad target search is omniscient | `SquadManagerBotModuleCA.cs:354-372` (`World.Actors.Where(IsPreferredEnemyUnit)`) |
| Only cloak and submersion are respected (`IVisibilityModifier`), never shroud | `AI_ARCHITECTURE.md` §0, `SquadManagerBotModuleCA.cs` `IsNotHiddenUnit` |
| Squads already have **local** fight-or-flee, but only once enemies are near | `Squads/AttackOrFleeFuzzyCA.cs:166` `CanAttack`, used at `States/GroundStatesCA.cs:23,90`; air flees on AA count `States/AirStatesCA.cs:210-212` |
| **No pre-commit risk test** (before a squad is sent) and **no risk-aware routing** | absent from `SquadManagerBotModuleCA.cs` / `Squads/**` |
| Island patches are **silently dropped**: expansion skips unreachable resources | `engine/OpenRA.Mods.Common/Traits/BotModules/McvExpansionManagerBotModule.cs:470-472` (`path == PathFinder.NoPath`), also `:830-833`, `:958` |
| MCVs are carryable cargo: ~37 MCV-like actors, cargo type `Vehicle`, most `Carryable` | resolver scan (`miniyaml.Ruleset.resolve`), 2026-09-26 |
| `Passenger.Weight` defaults to **0** in this engine | `engine/OpenRA.Mods.Common/Traits/Passenger.cs:31` |
| Vehicle-carrying transports exist in many factions. Naval: `ra1_navaltransport`, `td_gdi_landingcraft`, `td_nod_transportsubmarine`, `ra2lcrf`, `ra2sapc`, `cabal_lcraft`, `forgotten_lcraft`, `asianalliance_panth`. Air: `ra2_soviets_transportkirov`, `naxis_transportzeppelin`, `schwarzermond_spacezeppelin`, `protoss_shuttle`, `terran_dropship`, `steelconsortium_cargoship`. Plus 13 `Carryall` actors (D2k houses, TS GDI, Forgotten, Syndicate Hind) | same scan. **Faction coverage is uneven: list the factions with NO MCV ferry before promising island expansion for all** |
| Airdrop / unload mechanic already exists (dummy weapon or on-damage unload) through `ExternalBotOrdersManager` + `IssueOrderToBot` | `engine/OpenRA.Mods.AS/Traits/BotModules/ExternalBotOrdersManager.cs:120-149`; 15 `IssueOrderToBot` uses in `mods/cameo/ContentPacks/**` |
| Cargo loading bots exist, but only for infantry / bunkers / battery | `ai.yaml` `LoadCargoBotModule@Infantry/@TankBunker/@Battery` (~line 3725) |
| Beacons are **invisible to bots**: `Beacon` keeps `owner` and position **private**, and `PlaceBeacon.ResolveOrder` only runs on the placing player's actor | `engine/OpenRA.Mods.Common/Effects/Beacon.cs:20-24`, `engine/OpenRA.Mods.Common/Traits/Player/PlaceBeacon.cs:47-71` |
| The engine **already remembers fogged buildings** per player (frozen actors). Units are not remembered | `engine/OpenRA.Game/Traits/Player/FrozenActorLayer.cs:363,371` (`FrozenActorsInRegion/InCircle`), `Actor.CanBeViewedByPlayer` (`engine/OpenRA.Game/Actor.cs:510`), `Shroud.IsVisible/IsExplored` (`Shroud.cs:413-463`) |
| Match and situation logs already exist (phases 1–2) | `OpenRA.Mods.Cameo/Traits/AiMatchLogWriter.cs`, `AiSituationLogWriter.cs`, `docs/design/AI_MATCH_LOG.md` |

---

## 2. Fransbot's core model, restated precisely enough to build

Everything here comes from C2–C5. Treat it as a **spec to implement and then diff against the
source**, not as a description of the source.

**Value memory ("threat map").** Divide the map into coarse regions (suggestion: 8×8-cell buckets;
tune it). For each region, keep per enemy player:

```
RegionMemory { int ArmyValue; int DefenceValue; int AAValue; int EconomyValue;
               int LastSeenTick; bool EverSeen; }
```

* **Write** only from what the bot's player can see: `actor.CanBeViewedByPlayer(player)` for units,
  and `player.FrozenActorLayer` for fogged buildings (the engine already keeps those).
* **Keep** the values when the region goes dark. That is the "sticks in memory until it scans that
  area again" behaviour, and it is what lets the bot be wrong. Do **not** decay the value itself.
  Keep `LastSeenTick`, and let **confidence** = f(age) damp decisions (`AI_ARCHITECTURE.md` §3.1
  already specifies this).
* **Overwrite** a region's values when it is visible again, including to zero. Seen empty is information.
* Units move, so an unseen unit's value would otherwise count twice: once where it was last seen,
  and again wherever it turns up. Per-actor last-seen positions (keyed by `ActorID`) fix that. Move a
  known actor's value when it is re-seen elsewhere, and drop it after a long timeout.

**Risk gate.** Before committing a squad to a target region, compare the squad's value with the
remembered hostile value in and around the target:

```
commit if  squadValue * RiskAppetite(personality)  >=  remembered(target) + remembered(route)
```

`RiskAppetite` is per personality. Rush is high, Turtle low, Steamroller commits only with a large
margin. The emergency path of §4.5 in `AI_ARCHITECTURE.md` (defend the base) **bypasses** the gate:
you do not get to refuse a fight in your own base.

**Order vocabulary** (C5), mapped onto what Cameo squads already do:

| Fransbot | Meaning | Cameo equivalent today |
|---|---|---|
| Recon | refresh an unknown or stale region | **none**: the new `ScoutBotModule` |
| Raid | hit a soft, valuable target (harvesters, expansions) | `SquadManagerBotModuleCA` guerrilla knobs (`JoinGuerrilla`, `MaxGuerrillaSize`), a candidate that is computed but not grantable (§10.6 item 3) |
| Secure | take and hold a region (expansion site, choke) | protection squads (`States/ProtectionStatesCA.cs`), partly |
| Retreat | leave when the risk turns | `AttackOrFleeFuzzyCA` local flee, but only once engaged |

**Risk routing** (C5). Pick a waypoint path that minimises the sum of remembered hostile value
along it, then issue ordinary move / attack-move orders through the waypoints. The pathfinder itself
must **not** be replaced (that is engine code and synced). This is a coarse A* over the region grid,
in unsynced bot code, and the result is only a list of waypoints.

**Stats-driven valuation** (C6, C7). Value alone cannot see that rocket infantry beat tanks, and
fransotto says Fransbot cannot see it either. Cameo can go further than Fransbot here, because the
warhead system already encodes it: every weapon's damage per armour ladder is derivable from
`^Warhead_*` Versus (`DESIGN.md` §12.0h, and the derived damage-% tooltip landed in #445). Phase 9
below turns that into an effective-value multiplier. Do **not** re-derive Versus by hand: the
weapon and armour data come from rules at load time.

---

## 3. Feature-by-feature: Fransbot vs Cameo today vs what to do

| Feature | Fransbot (claimed) | Cameo today | Gap / route | Phase |
|---|---|---|---|---|
| No global vision | yes (C2) | omniscient master + squads (§1.3) | fogged observation for the master **and** the squad scans | **6a** |
| Spatial value memory | yes, the core (C3) | per-enemy totals only, no spatial part, no memory | `RegionMemory` grid in `MasterAiBotModule`, exposed on the snapshot | **6a** |
| Scouting | Recon order (C5) | none (`AI_ARCHITECTURE.md` §3.1: "Cameo has no scouting behaviour at all") | `ScoutBotModule`: target the stalest high-interest regions; unit choice through compositions or prerequisites, no hard-coded ids | **6b** |
| Pre-commit risk test | yes (C4) | none; local fuzzy flee only | risk gate in `SquadManagerBotModuleCA` target choice, reading the snapshot | **6c** |
| Squad target scans honour fog | implied by C2 | `World.Actors` at `SquadManagerBotModuleCA.cs:354-372` | read visible actors plus memory; fall back to today's behaviour when the snapshot is missing (§10.1 degradation rule) | **6d** |
| Risk-aware routing | yes (C5) | direct paths | region-grid A* → waypoints → existing orders | **6e** |
| Island expansion | unconfirmed (C12) | unreachable patches dropped (`McvExpansionManagerBotModule.cs:470`) | new Cameo module: find unreachable resource clusters → pick a ferry the faction owns (naval `Cargo` with `Vehicle`, or `Carryall`) → load, move, unload, deploy. Do **not** edit the engine module; it is `engine/` and not in this repo | **7** |
| Airdrops | yes (C8) | yes: `ExternalBotOrdersManager` + `IssueOrderToBot` unload on dummy weapon or damage | nothing to take; reuse the plumbing for the island ferry | — |
| Beacon response | WIP (C9) | bots cannot see beacons (§1.3) | shadow `PlaceBeacon` in `OpenRA.Mods.Cameo` (assembly order puts Cameo before Common, CLAUDE.md rule 7). Record `(owner, pos, tick)` into a world-level `BeaconTracker`, and have a bot module react to **allied** beacons only | **8** |
| Counter awareness (AT vs armour) | air/AA only (C7) | phase 5 counter demand drives builds from class **totals** | effective value = cost × damage-vs-armour factor from resolved Versus; feeds the risk gate and counter demand | **9** |
| Harvester estimate / economy proxy | yes (C10) | `EnemyProfile` has no economy field; omniscient own counts only | visible-harvester count per enemy in `RegionMemory`, fogged | 6a (field only) |
| Team chat | paused (C10) | none for skirmish bots (the survival map has Lua general chat) | low priority; unsynced chat lines are possible, but they are UX, not strength | later |
| LLM replay-tuning log | custom debug log (C1) | JSONL match and situation logs (phases 1–2) | compare formats in F0; adopt any field Fransbot logs that we do not | F0 |
| Many squad modules per personality | 6–7 (C11) | 5 personality-gated `SquadManagerBotModuleCA` | **don't copy**: it is his own stated pain point, and Cameo's master and snapshot design exists to avoid it | — |
| Performance at 10 bots | yes (C8) | scan cadence in `AI_ARCHITECTURE.md` §10.5 | the memory update must be O(visible actors) per rebuild, never O(map cells) per tick | all |

---

## 4. The work plan for Devin-Cloud (amends `AI_ARCHITECTURE.md` §10.6)

One PR per sub-phase. Each is shippable alone, and each honours the **degradation rule**
(§10.1: a missing snapshot means today's behaviour). The window closes **~2026-10-22**, so the
order below is also the priority order: stop wherever time runs out, and leave §7's handoff.

### 6a. Fogged snapshot + region value memory (C#: `OpenRA.Mods.Cameo/Traits/BotModules/`)

* Replace the omniscient scan at `BotSituation.cs:219` with visible actors
  (`CanBeViewedByPlayer`), plus frozen actors for buildings, plus the per-actor last-seen table.
* Add the `RegionMemory` grid (§2) and publish it on `BotSituation` as a read-only view.
  `EnemyProfile` values become sums over remembered regions, so the phase 3–5 consumers keep working
  unchanged.
* Add `UseFoggedObservation` (bool, **default true**, per the maintainer ruling of 2026-09-23 on
  PR #455). `false` must reproduce today's numbers exactly. That is the A/B lever for tuning.
* Add `EnemyProfile.HarvesterCount` (visible only).
* **Gate**: `python tools/tests/ai_bot_player_gate.py` in both modes; log how many regions are known
  at 5 / 10 / 20 minutes. A fogged bot that knows zero regions at 10 minutes means scouting is
  broken, not that fog works.

### 6b. `ScoutBotModule`

* Pick targets by **staleness × interest**: expansion sites (resource clusters from
  `ResourceMapBotModule`), the last-known enemy base, and choke regions on the route to the main target.
* Choose the unit through the existing path: a composition or prerequisite token such as
  `demand.scout`, following phase 5's `BotCounterDemandController` pattern. **No actor ids in C#**,
  because there are 25 factions.
* Hold at most N scouts, cheap and fast. Losing a scout is information: write the killer's value
  into the region.

### 6c. Risk gate in the squad manager

* In `SquadManagerBotModuleCA` attack-target selection, refuse (or re-target to a softer region)
  when `squadValue × RiskAppetite < remembered(target)`. `RiskAppetite` is a field per personality
  instance, in yaml.
* Home defence and the §4.5 emergency override bypass the gate.
* ⚠ This file lives in `OpenRA.Mods.CA/`, **outside the lane as written on #435** (see §7). Phase 4
  already had to touch CA (`IBotMainTargetProvider.cs`); the lane is corrected below.

### 6d. Fogged squad scans

* `FindClosestEnemy` and the high-value scan (`SquadManagerBotModuleCA.cs:354-372`) read the
  snapshot's visible-plus-remembered set instead of `World.Actors`. A remembered target is a
  **position**, not an actor, so squads attack-move to it and re-acquire on arrival.
* This is the change that actually removes the maintainer's "bot artillery always fires at max
  range" complaint. 6a alone does not.

### 6e. Risk routing

* Coarse A* over `RegionMemory` from the squad's centroid to the target, costed by remembered hostile
  value. Emit 1–4 waypoints and reuse the existing move / attack-move orders. Unsynced only.

### 7. Island expansion (new Cameo module)

* Candidates: resource clusters for which `McvExpansionManagerBotModule` would get
  `PathFinder.NoPath` for the MCV locomotor, but which a ferry can reach.
* Ferry choice from rules at load time: any buildable actor with a `Cargo` whose `Types` include the
  MCV's `Passenger.CargoType` (and weight fits), or a `Carryall` where the MCV is `Carryable`.
  **First, list per faction which ferry exists**, since some factions will have none (§1.3), and
  skip those factions cleanly.
* Sequence: request the ferry (a production request through `IBotRequestUnitProduction`, like the
  engine modules), then load, move to the shore cell nearest the patch, unload, and let the existing
  MCV manager deploy.
* Map test: `mods/cameo/maps/ai_*` is lane-owned. Add a small island map if none exists.

### 8. Beacon response

* Shadow `PlaceBeacon` in `OpenRA.Mods.Cameo` with the **same type name**, and **prove the shadow**
  with a Cameo-only field (CLAUDE.md rule 7: `--docs` proves nothing). It must still create the
  identical beacon, and additionally append `(owner, pos, tick)` to a world trait `BeaconTracker`.
  Writing in synced code and reading in unsynced bot code is safe, but **never** let bot code write
  back.
* Bot module: an allied beacon near **enemy** memory sends the nearest free squad (risk gate
  applies). An allied beacon on the **ally's own building** sends support (a repair or supply unit
  if the faction has one; skip otherwise).
* ⚠ Shadowing `PlaceBeacon` is outside the lane (it is not a `Bot*` file). Ask in the PR, per the
  lane rule.

### 9. Stats-derived effective value (optional this month)

* Effective value of an enemy unit against *our* squad = cost × (our weakness to its weapons).
  Precompute a per-actor-type table at load from resolved weapons and armour. Read Versus through
  the rules the engine loaded, not by parsing yaml.
* Feeds 6c (gate) and phase 5 (counter demand). This is where Cameo can **beat** Fransbot (C7).

### F0. Source analysis, blocked until access (§6)

---

## 5. What NOT to take, and why

* **Build orders, compositions, `UnitsToBuild` rows.** RA-only actor ids, and Cameo's composition
  system is prerequisite-token based (`AI_ARCHITECTURE.md` §1.4).
* **Six or seven squad managers per personality.** His own pain point (C11), and a `TraitOrDefault`
  crash risk in this tree (`AI_ARCHITECTURE.md` §1.3).
* **Anything that reads `World.Actors` for enemy information.** It defeats the whole point.
* **Map-specific data.** He trained on uploaded maps, but claims generality (C2). If the source
  contains per-map tables, leave them out.
* **Engine edits.** `engine/` is not in this repo (CLAUDE.md rule 7). If Fransbot patches Common bot
  modules, port the change as a Cameo-side module or shadow.

---

## 6. Phase F0: when the source arrives

**Maintainer action (only the maintainer can do this):** ask fransotto either to add a
collaborator on `f850484/OpenRA-Fransbot` (the maintainer's GitHub account; Devin's GitHub app
cannot read a repo it was not granted), or to re-send the zip by a non-expiring route, or to make
`V1.29.19-RC` public when he is comfortable. Clone it **outside** this repo (for example
`~/Documents/GitHub/OpenRA-Fransbot`, next to the CA and CN clones).

**Then, in this order:**

1. Identify the upstream base: `git log --reverse | head` and the OpenRA tag it forked from. Diff
   `OpenRA.Mods.Common/Traits/BotModules/**` against that tag. **The diff is the bot**; everything
   else is noise.
2. Find the value memory: grep for `LastSeen`, `Memory`, `Threat`, `Risk`, `Value`, `Recon`,
   `Raid`, `Secure`. Record its region size, decay rule, and how moving units are de-duplicated.
3. Find the island and transport logic (C12): grep `Cargo`, `Passenger`, `Transport`, `Island`,
   `NoPath`, `Unload`. This is the maintainer's original question.
4. Find the debug log format and compare it with `AiSituationLogWriter` (§3, last rows).
5. For each of 6a–8: note where his implementation differs from §2, and whether the difference is
   better. **Update this document**; do not fork a second one.
6. Check determinism: `HashSet` or `Dictionary` iteration driving orders, `Random` vs
   `World.LocalRandom`, anything in `ITick` (synced) rather than `IBotTick`. A yaml-first author's
   AI-written C# is exactly where a rare desync hides.

Questions to ask fransotto, so nobody has to guess: region size; how the memory handles units seen
once and never again; how Recon picks targets; whether island expansion is naval, air, or both; and
what the debug log contains.

---

## 7. Constraints for whoever implements this

* **Lane files (corrected).** PR #435's orders list `mods/cameo/rules/ai.yaml`; the real file is
  **`mods/cameo/ai/ai.yaml`**. 6c/6d need **`OpenRA.Mods.CA/Traits/BotModules/**`**, which phase 4
  already touched. Both are granted to the AI lane by these orders. Still outside the lane: the
  `PlaceBeacon` shadow (phase 8), `engine/**` (never), and weapons and warheads (never).
* **Overlap.** PR #245 (`codex/hard-bot-projects`, Blackrobe, idle since 2026-09-17) edits
  `BaseBuilderBotModuleCA.cs`, `BaseBuilderQueueManagerCA.cs`, `UnitBuilderBotModuleCA.cs`,
  `BotGlobalUnitBudget.cs`, `ai.yaml`, `defaults.yaml`. Phases 6a–6e do not need those builder
  files; if you must touch them, say so in the PR.
* **Sync rules** (lessons from #458): bot logic is unsynced and acts only through orders
  (`AI_ARCHITECTURE.md` §1.1); never iterate a `HashSet<string>` to drive orders or synced state;
  declare granted conditions with `[GrantedConditionReference]`; a shadowing trait must re-declare
  its interfaces.
* **Gates per PR:** C# build (`dotnet build -c Release`), `python tools/tests/ai_bot_player_gate.py`
  (running it starts a match; it has no `--help`), and a boot to the main menu with `perf.log`
  **newer than your pre-launch snapshot**. Put the gate numbers in the PR body.
* **Sign** `Co-Authored-By: Devin AI <devin@cognition.ai>`.
* **Before 2026-10-20:** a short handoff in `docs/HANDOFF.md` (the AI lane section) saying which of
  6a–9 landed and what is open, and tick the phases in `AI_ARCHITECTURE.md` §10.6.
