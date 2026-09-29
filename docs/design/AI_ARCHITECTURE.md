# Cameo AI architecture — bot modules, personalities, master module, learning

_Written 2026-08-31. Owner document for everything about how Cameo's bots decide what to do.
[`DESIGN.md`](../DESIGN.md) §19 and §20 keep the binding rules for the personality and composition
systems that already ship; this file is the forward design and the research behind it.
Task queue: [`ROADMAP.md`](ROADMAP.md) section "AI ARCHITECTURE"._

**Most of this document remains a design, not a shipped adaptive system.** Section 1 is verified
fact with source evidence. Sections 2–7 are proposals except the record-only implementation in
§6.2a, delivered on the follow-up branch for coordinator review. Section 8 is outside research. Section 9 records what is
still undecided. Section 10 is the module-by-module build plan and section 11 reconciles the
five-agent research round against sections 1–10. **Section 12 is the maintainer's combined-arms
order of 2026-09-28** (arsenal tracker with self-learning, role ratios, formation, siege, air
doctrine, scouting) mapped onto what ships, with phases CA-1…CA-6 and an owner for each.
Research round 2 — how the strongest RTS bots fight, reason about space, micro, learn and stay
fun, with the phases CP/ZG/IM/UT/MI/OM/LG it adds — is [`AI_DEEP_RESEARCH.md`](AI_DEEP_RESEARCH.md). The §6.2a match logger has runtime and replay
evidence in `docs/audit/ASTRA_REVIEW.md`; this does not validate the proposed decision system.

---

## 0. The goal, stated so it can be checked

A bot that beats a good human by deciding better, not by being given more. Two consequences that
shape every choice in this document:

1. **No economic or production cheats.** Cameo is already close to this. Difficulty is expressed
   purely as `BotLimits` — caps on production structures, refineries and harvesters, plus build
   delay/interval modifiers (`mods/cameo/ai/ai.yaml:37-142`). There is no cash bonus, no resource
   multiplier and no build-speed advantage for bots anywhere in the AI rules; `DefaultCash` is a
   global player setting (`mods/cameo/rules/player.yaml:75`). Difficulty currently *throttles the
   bot's own competence*, which is the honest form of it.
2. **The real cheat is information, and it is still there.** The squad manager scans
   `World.Actors` and filters visibility only through `IVisibilityModifier` — cloak and submersion
   — never through the player's shroud (`OpenRA.Mods.CA/Traits/BotModules/SquadManagerBotModuleCA.cs:226-253,331-345`).
   A bot therefore knows where every enemy unit and building is from tick zero, including inside
   unexplored map. Only the capture and crate modules expose a visibility option at all
   (`CaptureManagerBotModuleCA.cs:49`, `CratePickupBotModule.cs:41`). Any claim that a future
   Cameo bot "wins without cheating" is false until this is addressed, and the strategy-detection
   design in §3 is where it has to be addressed, because a detector that reads the true world
   state cannot be wrong and therefore cannot be beaten by deception.

The second point is the single most important finding in this document. It also makes the AI
*better* rather than worse to fix: an opponent model that can be wrong is what makes scouting,
feints and hidden tech meaningful, and it is what the whole opponent-modelling literature in §8
is about.

### 0a. The acceptance test (maintainer rulings, 2026-09-27)

> *"The new Frankenstein monster AI with no map wide vision [must be] able to beat our existing AI of
> the same difficulty level with map wide vision … every time with any faction. … At least as a
> first step it should win with the same faction on both sides. Later any match up."*

The program is done when this test passes. It is binding and still **unmeasured** (status below).

| | ruling |
|---|---|
| **Candidate** | the new bot (the CA × RV × CN × Fransbot merge), with **no map-wide vision**: every module reads the world only through what its player can see or remembers (§3.1). |
| **Opponent** | the existing bot **at the same difficulty**, **with** map-wide vision, i.e. today's omniscient squad targeting (§0 point 2). |
| **Pass** | the candidate wins **at least 16 of 20** matches, one match per map. |
| **Maps** | the **20 two-player maps in the `Tournament` category**, ordered by player count, then title. There are 21; **Twin Lakes CAMEO** is left out. |
| **Stage 1** | the same faction on both sides. |
| **Stage 2** | any match-up: every faction against every faction. |

The 20 maps (measured 2026-09-28 from each map's `Categories:`, `Title:` and `Playable: True`
count; file in brackets):

1. 16:9 (`16-9.oramap`) · 2. 420 blaze it (`420bzt`) · 3. A Nuclear Winter (`_ra_a-nuclear-winter`) ·
4. Alpine Assault (`AlpineAssault`) · 5. Bombshell Beach (`BombshellBeach`) · 6. Death Valley
(`DeathValley`) · 7. Frozen Rift CAMEO (`frozen_rift_cameo`) · 8. Model 200 (`model-200`) ·
9. Mountain Pass (`MountainPass`) · 10. Northern Lights CAMEO (`NL04`) · 11. Only Blood Is Accepted
(`Only_Blood_Is_Accepted_1v1_BI-4.4`) · 12. Pitfight (`_ra_pitfight`) · 13. Plan B (`plan-b`) ·
14. Proto Blood (`proto_map_blood`) · 15. Proto Desert Night (`proto_map_desert_night`) ·
16. Proto Mediterranean Conflict (`proto_map_mediterranean`) · 17. Proto Snow (`proto_map_snow`) ·
18. Satan's Clutch (`SatansClutch`) · 19. Snowy Woodlands (`SnowyWoodlands`) · 20. Ysmir (`_ra_ysmir`)

**Status 2026-09-28: the test cannot be run yet.** Two pieces are missing from master:
1. **A fog-honesty check.** Nothing verifies "no map-wide vision" across *all* modules;
   `MasterAiBotModule.UseFoggedObservation: true` (`mods/cameo/ai/ai.yaml`) covers only the modules
   that consume the fogged provider, and the squad manager still scans `World.Actors` (§0 point 2).
2. **A match runner** that plays one match per map and records the winner. DAWN's #578 branch has a
   Fransbot versus map; nothing on master plays the 20-map set.

Also, **the candidate must first be able to play every faction.** On 2026-09-28 an Atreides bot
starting from a bare construction yard still built nothing (the power, barracks and production lists
lack the D2k and Outpost2 ids; #588 and the next list-rollout roles fix this).

Speed for the runs: `GameSpeed: maximum` — the top ladder tier — with the dropdown locked
and adaptive speed off (see `LESSONS_LEARNED.md`); promoted from `insane` for unattended
tuning runs (maintainer order 2026-09-29).

---

## 1. Verified facts

Everything in this section was read out of the pinned tree or produced by running the engine's
own resolver. Raw artifacts: `C:\Users\Administrator\research\ai-yaml-experiment\`.

### 1.1 Bot logic is unsynced and may only act by issuing orders

`ModularBot` ticks its modules inside `Sync.RunUnsynced(...)` and states the contract directly:
"Bot logic is not allowed to affect world state, and can only act by issuing orders"
(`engine/OpenRA.Mods.Common/Traits/Player/ModularBot.cs:68-70,86-104`). Modules are activated only
for the client that owns the bot, and not at all in replays (`:66-79`).

Three consequences, all load-bearing:

* A bot module **may** hold arbitrary local state, read files, use `World.LocalRandom`, and write
  logs. None of it can desync, because none of it reaches the simulation except as orders.
* A bot module **may not** grant a condition, set a variable other traits read, or otherwise
  mutate synced state. This is why the personality switch in §4 must travel as an *order*.
* Bot modules exist as traits on every client but only tick on one, so any per-module counter will
  legitimately differ between clients. That is harmless, and it is also why such a counter must
  never feed a synced decision.

**Precedent for the order round-trip already exists in Cameo**, which removes the main risk from
§4: `PlugSpawnerBotModuleCA` queues `new Order("PlacePlugAI", player.PlayerActor, ...)` with a
`TargetString` payload from `IBotTick`, and resolves it in synced code via `IResolveOrder` on the
same trait (`OpenRA.Mods.Cameo/Traits/BotModules/PlugSpawnerBotModuleCA.cs:84-108`).
`ExternalBotOrdersManager` does the same for `IssueOrderToBot`
(`engine/OpenRA.Mods.AS/Traits/BotModules/ExternalBotOrdersManager.cs:120-149`).

### 1.2 How ContentPack ai.yaml actually merges — measured, not assumed

The user's premise was that per-pack bot modules "would be overwritten in the order they are
loaded". The mechanism is more specific than that, and the specifics decide the architecture.
Measured with `.\utility.cmd cameo --resolved-rules Player`, one case at a time, against a
1375-row baseline:

| # | Case | Result |
|---|---|---|
| 0 | Load order | **ContentPack rules resolve BEFORE the global `Rules:` block**, so every `ContentPacks/**/yaml/ai.yaml` is merged before `cameo|ai/ai.yaml` |
| 1 | Pack adds a *new* row to `UnitsToBuild` | **Unions.** 1375 → 1376 rows, new row present alongside all existing ones |
| 2 | Pack sets an *existing* `UnitsToBuild` row | Row keeps the global file's value (void as a measurement — the appended fragment landed at the wrong depth; the conclusion rests on case 3) |
| 3 | Pack sets a scalar the global file also sets (`SquadSize`) | **Global wins.** Resolved `SquadSize` is 3, not the pack's 77; no trace of 77 survives; every other field of the block is intact |
| 4 | Pack declares a *new* instance `SquadManagerBotModuleCA@gdi_test` | **Works.** Both `@rush` and `@gdi_test` resolve, no error or warning |
| 5 | Pack removes a trait the global file defines (`-SquadManagerBotModuleCA@rush`) | **Hard error:** `OpenRA.YamlException: ContentPacks|TiberianDawn/GDI/yaml/ai.yaml:56: There are no elements with key SquadManagerBotModuleCA@rush to remove` |

This matches `MiniYaml.MergePartial`, which recurses into duplicate keys and takes the override
(later) node's value at the leaves (`engine/OpenRA.Game/MiniYaml.cs`), and `FieldLoader`'s
dictionary parsing, which builds a field from whatever nested nodes survived the merge
(`engine/OpenRA.Game/FieldLoader.cs`).

**The rule, stated for implementers:**

> A ContentPack can **add** keys, rows and whole trait instances to a bot module. It can **never
> override** a key the global `ai/ai.yaml` already sets — the global file loads later and wins —
> and it can **never remove** one, which is a load-time crash rather than a silent no-op. Packs
> can, however, override and remove things declared by *earlier* packs, ordered by the `Include:`
> lines in `mod.yaml`.

So the user's instinct is right about the symptom and the fix is not "stop the overwriting": it is
that **the global file must stop declaring anything a pack is supposed to own.** Whatever key the
global file sets is permanently unownable by any pack. This is a subtractive job on
`mods/cameo/ai/ai.yaml`, not primarily an additive one on the 28 pack files.

Case 5 also kills the most obvious layering idiom: a pack cannot opt out of a global default.
Opt-out has to be expressed as a value a pack *adds* (a condition, a prerequisite token, a row
with weight 0 where the consumer treats 0 as "never"), never as a removal.

**Ordering caveat (measured 2026-09-05):** `MiniYaml.MergePartial`
(`engine/OpenRA.Game/MiniYaml.cs:590-643`) appends new dictionary keys in merge order — pack
rows first, then global rows. Moving an existing `UnitsToBuild` row from the global file to a
pack therefore relocates it to the top of the resolved dictionary: the `--resolved-rules` dump
cannot stay byte-identical, though the resolved content (keys + values, and the
`FrozenDictionary` the bot consumes) is identical. Any migration gate must compare *content*,
not dump bytes, or the rows must stay in the global file.

### 1.3 Multi-instance safety is per-consumer, and one consumer is a crash

Multiple same-type modules are fine *iff* every consumer enumerates them. The engine's own bot
modules already do this: `UnitBuilderBotModule`, `McvManagerBotModule`,
`McvExpansionManagerBotModule` and `HarvesterBotModule` all cache arrays of
`IBotRequestUnitProduction` / `IBotPositionsUpdated` / `IBotRequestPauseUnitProduction` via
`TraitsImplementing<T>()`.

The exception, verified: `UnitBuilderBotModuleCA` resolves compositions with
`self.World.WorldActor.TraitOrDefault<UnitCompositionsBotModule>()`
(`OpenRA.Mods.CA/Traits/BotModules/UnitBuilderBotModuleCA.cs:151`), and
`TraitDictionary.GetOrDefault` throws `Actor World has multiple traits of type ...` on the second
instance (`engine/OpenRA.Game/TraitDictionary.cs:178`). Because a disabled `ConditionalTrait`
still lives in the trait dictionary, condition-gating does not save it: five personality-gated
composition modules crash on the first bot tick regardless of which conditions are active.
Credit for this correction goes to the Claude review; my earlier "compositions are personality-
blind because the trait has no condition field" was wrong on the mechanism.

### 1.4 Personality-tagged compositions need no C# at all

Composition eligibility runs through the tech tree:
`techTree.HasPrerequisites(composition.Prerequisites)` (`UnitBuilderBotModuleCA.cs:581`),
`TechTree` gathers `ITechTreePrerequisite` from every actor the player owns including the Player
actor itself, and `ProvidesPrerequisiteInfo : ConditionalTraitInfo, ITechTreePrerequisiteInfo`
yields nothing while disabled (`engine/OpenRA.Mods.Common/Traits/Player/ProvidesPrerequisite.cs:20,61`).
Cameo already relies on exactly this pattern — `ProvidesPrerequisite@botplayer` with
`RequiresCondition: genericbot` gates every bot module in the file (`mods/cameo/ai/ai.yaml:176-178`).

So a condition-gated `ProvidesPrerequisite` per personality plus a token in a composition's
`Prerequisites` gives personality-specific compositions with one composition module, no
`TraitOrDefault` collision, and zero C#. `Prerequisites` is an AND-list, but OR is expressible the
way tech trees always express it: several condition-gated instances providing the same group token
(`personality_aggressive` from both the Rush and Steamroller conditions).

### 1.5 What the reference mods actually have — and the one correction that matters

I previously told the user that CN's personalities are "budget allocators, not switchers". That
was incomplete, and the missing half is the closest existing thing to what the user is asking for.
`CNBotProfileBotModule` (924 lines) has an **`Adaptive` profile** that re-scores and switches
profiles at runtime, with machinery Cameo should copy the shape of:

* candidate scoring per profile, with named weights (`AdaptiveFortificationRushPenalty`,
  `AdaptiveFortificationSteamrollerBonus`, `AdaptiveTechCashRichBonus`, …);
* **hysteresis** as an explicit score bonus for the incumbent (`AdaptiveProfileMomentumBonus`,
  default 0.75) plus a minimum hold time (`AdaptiveMinimumIntentHoldTicks`, 3000) and a
  re-evaluation cooldown (`AdaptiveSwitchCooldownTicks`, 1500);
* an **emergency override** that bypasses hysteresis on a danger spike
  (`AdaptiveEmergencyTurtleDangerThreshold`, checked every 25 ticks);
* earliest-tick and army-ratio gates so a profile cannot be picked before it makes sense
  (`AdaptiveSteamrollerEarliestTick`, `AdaptiveSteamrollerArmyRatio`);
* **team coordination** — a penalty per allied bot already running a profile
  (`TeamAdaptiveCoverageWeight`) so allies diversify;
* observer-only announcements of each switch (`AnnounceProfileToObservers`), the same information
  discipline Cameo adopted in §19.

Its own field documentation contains the lesson worth quoting: momentum must be small enough that
"the signal the profile exists for could never trigger the switch it was written for".

Equally important is what CN does **not** have, which is precisely the user's ask:

* **No per-enemy-player model.** Its fortification signal is a single aggregate,
  `squadManager.KnownEnemyDefenseCount` (`CNBotProfileBotModule.cs:571-572`), not a count per
  opponent. There is no notion of a main target: the string `TargetPlayer` does not occur.
* **No learning and no persistence.** Zero occurrences of `File.`, `Path.Combine`, `Json`, `Save`
  or `WinRate` in the file. Nothing survives the match.

No other reference mod is closer. So dynamic switching has a reference implementation to learn
from; **per-enemy targeting and cross-match learning have none, in any OpenRA mod** — that part is
new work, and §8 is where the outside prior art for it comes from.

"Copy the shape" above means the *numbers and the safeguards*. It does not mean the mechanism:
§1.6 is a second read of the same file, and it shows the mechanism is one Cameo may not use.

### 1.6 CN, read a second time: the numbers hold, the mechanism does not

Verified against the local clone at `crystallized-nexus@30cf70a66`, file
`.modsdk/OpenRA.Mods.CN/Traits/BotModules/CNBotProfileBotModule.cs` unless stated otherwise.

* **The adaptive constants are as quoted in §1.5**: `AdaptiveStaticExposureWeight` 0.25 (`:90`),
  `AdaptiveSwitchCooldownTicks` 1500 (`:96`), `AdaptiveEmergencyCheckInterval` 25 (`:110`),
  `AdaptiveMinimumIntentHoldTicks` 3000 (`:113`), `AdaptiveEmergencyTurtleDangerThreshold` 600
  (`:116`), `AdaptiveEnemyFortifiedDefenses` 6 (`:126`), `AdaptiveProfileMomentumBonus` 0.75
  (`:190`), `TeamAdaptiveCoverageWeight` 1.5 (`:194`).
* **CN switches profiles by granting the condition directly from the bot module.** `SwitchTo`
  revokes the old token and grants the new one on the player actor (`:882-887`); it is reached from
  `IBotTick.BotTick` (`:321`), i.e. from inside `Sync.RunUnsynced`. The initial profile is drawn
  with `world.LocalRandom` in the trait constructor and granted in `Created` (`:307`, `:317-318`,
  `:901-910`). Under §1.1 that is synced state being mutated from unsynced code, on one client, from
  a host-local random draw. I have not tested whether CN desyncs in practice — it may be effectively
  single-client in the modes people play — but the pattern is the one `ModularBot` explicitly
  forbids, so **it is not available to Cameo**. The order bridge in §4.2 and §10.4 is therefore not
  "validated by CN"; it is Cameo's own, and it exists precisely because CN's shortcut is closed to
  us. Two of the research replies asserted the opposite; see §11.2.
* **CN's fortification input is fog-honest contact memory, not omniscience.**
  `UpdateKnownEnemyDefenses` (`Squads/CNSquadManagerBotModule.cs:4101-4145`) records a defence only
  if `building.CanBeViewedByPlayer(Player)`, forgets a remembered cell only when
  `Player.Shroud.IsVisible(cell)` proves it empty, and otherwise keeps what was last seen under fog;
  damage taken also writes the attacker's cell into the same memory (`:975`). So the blanket claim
  that the family's bots are omniscient except for cloak — mine for Cameo in §0.2, and Grok's for the
  family — is true of *targeting* but false of CN's *strategy input*. This matters for sequencing:
  contact memory is the cheapest first step towards §0.2 and it has a working in-family reference,
  while a full shroud gate on squad targeting does not.
* **CN documents its own worst bug in-source, and Cameo would inherit it verbatim** (`:325-332`):
  because there is one squad manager, base builder and MCV manager *per profile*, each gated by a
  condition, a reference cached on the first tick points at an instance the module's own next
  decision disables — "the fortification input read zero for the rest of the match while the active
  squad manager knew of twenty-two enemy emplacements". Cameo's personality squad managers are
  per-personality instances too (§10.2), so any master-module reference to a personality-gated module
  must be re-resolved whenever the cached one is not enabled (`:333-351` is the fix), never cached
  once.

---

## 2. Splitting bot modules across ContentPacks

### 2.1 What "splittable" has to mean here

Cameo ships 28 `ContentPacks/**/yaml/ai.yaml` files, all effectively placeholders, while
`mods/cameo/ai/ai.yaml` holds ~6,440 lines of bot configuration for every faction at once,
including a single 1,375-row `UnitsToBuild` table. The goal is that a pack contributes its own AI
behaviour and can be enabled or disabled independently, with no cross-pack breakage and no
dependence on load order for correctness.

Given §1.2, five mechanisms are available. They are not exclusive; the recommendation uses three.

### 2.2 Option A — pack-owned rows in globally-declared dictionaries

The pack adds rows to a dictionary field the global file declares (`UnitsToBuild`, `UnitLimits`,
`BuildingFractions`, …). Measured to work (case 1).

* **Good:** zero C#, zero new concepts, works today, and it is the natural home for the faction
  data that makes up most of the bulk. Actor names are faction-prefixed, so rows are already
  effectively pack-scoped: a GDI bot never sees a Nod row.
* **Bad:** the global file must not already contain the row (case 3), so this only works after the
  rows are *moved out* of `ai/ai.yaml`. It cannot express per-pack *scalars* at all, because the
  global file's value always wins.
* **Verdict: adopt, for dictionary data only.** This is the mechanical bulk of the migration and
  the one part that is safely delegable.

### 2.3 Option B — pack-owned uniquely-named module instances

The pack declares `SquadManagerBotModuleCA@td_gdi` etc. Measured to work (case 4).

* **Good:** full per-pack control of every field, including scalars.
* **Bad:** it multiplies decision authorities. Two enabled squad managers both form squads from the
  same idle pool; two unit builders both queue production against the same cash. It also duplicates
  the enormous shared type lists per pack, and for the composition module it is a *crash* (§1.3).
* **Verdict: adopt only for modules that are genuinely per-scope specialists, and only when gated
  by a condition that guarantees at most one is enabled.** Never for the composition module.

### 2.4 Option C — prerequisite/condition-gated configuration

The pack contributes tokens and condition-gated traits rather than module configuration, and the
global module reacts (§1.4).

* **Good:** zero C#, no new authorities, and it composes — this is the mechanism that makes
  personality-specific compositions possible at all.
* **Bad:** expressiveness is limited to what a token can gate; AND-only semantics need group
  tokens for OR.
* **Verdict: adopt.** It is the right mechanism for eligibility and membership, not for numbers.

### 2.5 Option D — a fragment registry with an aggregator (needs C#)

Introduce a small multi-instance provider trait — say `BotDataFragment@<pack>` — that packs
declare freely, plus one aggregator that collects every instance with `TraitsImplementing<T>()`
and hands the merged result to the real modules. The engine's bot modules already use exactly this
discovery pattern (§1.3), so the idiom is native rather than invented.

* **Good:** removes load-order sensitivity entirely, makes "which pack contributed this" a
  first-class question (and therefore loggable and auditable), and gives packs scalar control
  through a merge policy the aggregator defines explicitly (max, sum, last, per-scope) instead of
  through whichever file happens to load last.
* **Bad:** new C#, and a merge policy is a new thing to get wrong.
* **Verdict: defer.** It is the right answer *if* option A's constraint (global file must not
  declare the key) proves too restrictive in practice. Do not build it speculatively.

### 2.6 Option E — a generated merged ai.yaml

Keep authoring per-pack and generate the global file with a tool at build time.

* **Good:** arbitrary merge semantics, no engine change.
* **Bad:** a generated 6,000-line file in the tree, a generator to maintain, and the thing the
  game loads stops being the thing a human edited. Cameo's audit tooling already treats the yaml
  as source of truth.
* **Verdict: reject** for runtime configuration.

### 2.7 Recommended target shape

```
mods/cameo/ai/ai.yaml            difficulties, ModularBot types, singleton authorities,
                                 module DECLARATIONS with shared non-faction defaults,
                                 and no faction rows and no pack-ownable scalars
ContentPacks/<pack>/yaml/ai.yaml UnitsToBuild / UnitLimits / BuildingFractions rows for that
                                 pack's actors, its compositions, its personality tokens,
                                 and any pack-scoped specialist module instances
```

Migration order, each step independently verifiable by diffing `--resolved-rules Player` against
the pre-migration dump — the resolved output must be **byte-identical** until behaviour is
deliberately changed:

1. Pick one pack (TD/GDI) and one dictionary (`UnitsToBuild`). Move only that pack's rows out of
   the global file into the pack file. Resolved dump must be unchanged.
2. Repeat per pack for that dictionary, then per dictionary. This is mechanical and delegable.
3. Move compositions and personality tokens to their packs (§1.4).
4. Only then consider option D, and only for a scalar that genuinely needs per-pack values.

**Trap to document loudly** (also going into `LESSONS_LEARNED.md`): moving a row out of the global
file is only safe if no *other* file still sets it, and a pack cannot remove a global default
without a load-time crash. The failure mode of a partial migration is silent — the global value
just keeps winning — which is why every step is gated on the resolved-rules diff rather than on
reading the yaml.

### 2.7a Status (2026-09-27, AI architect: Claude)

Measured on master `28cf7b404` with `tools/audit/miniyaml` (rule 8e): the central `ai.yaml` held
**7,151 actor-id references**. **2,680** are dictionary rows (12 fields, e.g. `UnitsToBuild` 1,436,
`BuildingFractions` 402, `BuildingLimits` 250). **4,471** are list entries (41 fields, e.g.
`GuerrillaTypes` 1,265, `ExcludeFromSquadsTypes` 510, `HighValueTargetTypes` 500). **0 of 28**
pack `ai.yaml` files held live config. They held a stale "cannot be split" note plus commented
copies of the rows, which is a second copy that drifts.

* **Dictionaries: built.** `tools/packs/split_ai_rows.py` moves every row whose key is an actor
  defined in exactly one pack into that pack's `yaml/ai.yaml`, under the same trait instance and
  field. It adds the `content.yaml` include when the pack had no ai file; 13 packs had none, and
  without the include their rows would silently vanish. **2,646 of the 2,680 rows are movable.**
  The rest are keyed by actors defined in no pack or in several. `tools/packs/compare_resolved.py`
  is the gate: it compares the engine's `--resolved-rules Player` before and after, as content.
  **Pilot measured: TD/GDI, 100 rows, 7,054 resolved keys before and after, 0 differences.** The
  all-pack apply is prepared but not committed: its engine verification is still pending.
* **Lists: not movable in yaml** (§2.8).

### 2.8 Lists: derive the membership, don't enumerate it

A pack cannot append to a list that another file sets. MiniYaml merges a scalar value by
override, and the central file loads last (§1.2). A list therefore cannot be split by moving
lines. As long as the central file names every faction's ids, two things break. A game that
doesn't load faction X still carries ids for X's actors: that is a lint error today and a
KeyNotFound the day loading becomes dynamic. And a pack author has to edit the central file to
add a unit, which is exactly the coupling ContentPacks exist to remove.

Three mechanisms were considered:

| | Mechanism | Pack edits | C# | Verdict |
|---|---|---|---|---|
| **A** | **Roles on the actor.** Each pack's unit yaml says what the unit *is for* (`BotRoles: Guerrilla, AntiAir`). Mechanical roles are **derived** from traits and need no yaml (Harvester → harvester, Aircraft → air unit, a naval locomotor → naval, Refinery, conyard, MCV, power, barracks). At rules load, one Cameo trait (`BotRoleSets`, `IRulesetLoaded`) adds each role's actors into the module lists that role feeds, through a role → (module, field) table. | on the pack's own actors only | one new trait; **zero edits to CA files** (Frankenstein and CA sync stay safe) | **RULED 2026-09-27 (maintainer): adopt** |
| B | Engine list-append syntax (`GuerrillaTypes+: …`) in cameo-engine's MiniYaml | 5× per personality instance, per pack | engine patch | rejected: duplicates every list ×5 and forks the yaml language |
| C | Every consumer reads `Info.X ∪ roles(X)` | on the pack's own actors | edits at every read site in ~15 CA files | rejected: conflicts with every CA sync |

Why A fits the rulings already made. The raid-unit ruling (2026-09-27: guerrilla = fast/light,
*generated from traits*) is a derived role. EMBER's 6g `BotTargetTags` already derives targeting
tags from rules. An unloaded pack contributes no actors, so it contributes no ids, and the lists
become plug and play automatically. Mechanism A **replaces** each target field's value once, at
rules load, with a new set of the field's own type (old ∪ role members), identically on every
client, so it is sync-safe. It follows `ScaledBullet`'s derive-at-load idiom. Replacing rather than
mutating is required: `BaseBuilderBotModuleCA`'s lists are already `FrozenSet` on master.

**Built: `BotRoleSets` (Player) + `BotRoles` (actor)**, in `OpenRA.Mods.Cameo/Traits/BotModules/BotRoleSets.cs`.
The yaml has `DeriveHas` / `DeriveNot` / `Exclude` / `Targets` per role, and only roles listed
under `Apply` change anything. `DeriveHasField` / `DeriveNotField` add **field predicates**:
`Trait.Field any v1|v2` (the field holds at least one listed value) or `Trait.Field only v1|v2`
(it holds values, and all of them are listed). They're read by reflection at rules load, and a
predicate that no trait can satisfy fails the load, so a typo can't silently match nothing. This
is the primitive DAWN's Fransbot field spec (Class B: `Mobile.Locomotor`, `Production.Produces`)
builds on. Every other role **reports** to `bot-roles.log`: its member count,
what it would add, and what is written but not in the role. `tools/ai/derive_roles_preview.py`
predicts the same numbers from the yaml. **First report (2026-09-27, report-only):**

| Role → target | Written | Would add | Written, not derived |
|---|---|---|---|
| harvester → `HarvesterBotModuleCA` / `ResourceMapBotModule.HarvesterTypes` | 25 / 26 | 9: the D2k spice harvesters (atreides, harkonnen, corrino), Outpost 2 cargo trucks (Eden, Plymouth), `futuretech_prospectormk2`, `ra1_soviets_heavyindustrialminer`, `tkm_templateharvesterraname`, `wc2_humans_militiapeasant` | 0 / 1 (`naxis_slaveoverseer`) |
| refinery → 3 `RefineryTypes` fields | 28–32 | 10–14: the D2k refineries, the Outpost 2 smelters, the StarCraft resource depots (`protoss_nexus`, `terran_commandcenter`, `zerg_hatchery`: also yards), the Warcraft II oil refineries | 4 (`chsupply`, `glsupply`, `usasupply`, `refinery`) |
| mcv → `McvTypes` | 35 | 10: the D2k MCVs, Outpost 2 convecs, **and false hits**: `ra1_soviets_stalinfist`, `tkm_flakbus`, `tkm_trenchtank`, `tkm_trenchtruck`, `ts_nod_shadowteam` (they transform, but not into a yard) | 0 |
| conyard → `ConstructionYardTypes` | 28 | 5: the D2k yards, the Outpost 2 structure factories | 2 (`zerg_hive`, `zerg_lair`) |

Derivation only counts **producible** actors: `Buildable` **with a Queue**. Spawned slaves such as
`YRSLAV` and `tkmworker` carry a queueless `Buildable` for their tooltip. Their master miner drives
them, so they're no one's harvester. Without that rule the harvester role would have added both.

What an unlisted actor costs depends on the module. The lists are looked up **by name**
(`ActorIndex.OwnerAndNames…`), but not every use is by name:

* **Harvester:** `HarvesterBotModuleCA` sends *every* `Harvester`-trait actor out to harvest
  (`ActorsHavingTrait<Harvester>`), listed or not. The name list controls the **count**, **which
  harvester to build** (`GetBuildableInfoByCommonName`, a random pick among the buildable members)
  and the retreat-when-attacked response. A faction whose harvester isn't listed counts zero, and the
  module has nothing buildable to request. The same happens after a switch makes the listed type
  unbuildable: RA1's Industrial Efficiency doctrine replaces `ra1_soviets_oretruck` with the unlisted
  heavy industrial miner, and FutureTech's promotion does the same with `futuretech_prospectormk2`.
* **Refinery and conyard: the bot is inert.** `BaseBuilderBotModuleCA.PauseUnitProduction` is
  `!HasMinimalRefineryCount()` (BaseBuilderBotModuleCA.cs:395), a by-name count. A faction whose
  refinery isn't listed keeps **unit production paused for the whole game**, and a yard that isn't
  listed gives the base builder nothing to build from. That applies to **Atreides, Harkonnen,
  Corrino, Eden and Plymouth**. Measured on `ai_harvester_gate_20260927`: a hard Atreides bot with a
  yard, a refinery, a heavy factory and 10,000 credits spent **nothing in 6,000 ticks**. The
  refinery and conyard roles fix it (applied together; see the table below). The same by-name
  count had also left `HarvesterBotModuleCA` without `ordos_refineryordos` and
  `schwarzermond_orerefinery`, so those two bots aimed for a single harvester.
* **Refinery: which refinery gets built.** `BaseBuilderQueueManagerCA.GetProducibleBuilding`
  picks **at random** among the buildable `RefineryTypes`. A water-only refinery (the WC2 oil
  refineries) would be chosen about half the time on any map, so the role excludes it with
  `DeriveNotField: refinery: Building.TerrainTypes only Water`. `only`, not `any`:
  `steelconsortium_consortiumrefinery` can be placed on land *and* water, and it stays. Yards that
  are also resource depots (StarCraft Nexus, Command Center, Hatchery) are excluded with
  `DeriveNot: BaseBuilding`. They were never written as refineries, so nothing changes for them.

Each role is applied only after its additions are reviewed and a match with an affected faction
confirms it. False hits are best removed by a tighter **derivation rule** (the producible rule
above). `Exclude` names ids centrally, so it's a last resort. The "written, not derived" actors get a
`BotRoles` line in their own pack. Only then does the central list shrink.

Order: the derived roles first (they remove the most ids with no yaml at all), then `BotRoles` on
the judgment lists (`HighValueTargetTypes`, `BigAirThreats`, `ExcludeFromSquadsTypes`). A list is
then emptied in the central file. Progress metric: actor ids in the central `ai.yaml`, lower-only,
→ 0. It was **7,151** before the pack split. Measure it with `python tools/ai/count_central_ids.py`,
which counts only ids of loaded actors (a node key or a list entry): **4,530** on `e9d500212` (after
#574), **4,480** after the harvester role, and **4,092** after refinery + conyard and the squad exclusion.

**Applied roles** (each needs a match with an affected faction; refinery and conyard went in together,
because a bot with neither listed stays paused):

| Role | Applied | Central ids removed | Evidence |
|---|---|---|---|
| harvester | 2026-09-27 | 50: `HarvesterBotModuleCA.HarvesterTypes` emptied, and `ResourceMapBotModule.HarvesterTypes` reduced to `naxis_slaveoverseer` (a slave whip with no `Harvester` trait; kept so its behaviour doesn't change) | `ai_harvester_gate_20260927`, 4 paired runs to tick 6000: TKM (refinery listed, harvester not) **built +1, +1 without the role and +5, +6 with it**. Atreides built 0 either way: it needs refinery + conyard. `bot-roles.log`: `34 members, 0 written; ADDED 34` |
| refinery + conyard, and harvester → `SquadManagerBotModuleCA.ExcludeFromSquadsTypes` | 2026-09-27 | 388: `HarvesterBotModuleCA.RefineryTypes` emptied; the other two `RefineryTypes` keep only `wc2_humans_townhall`/`wc2_orcs_greathall` (refinery-yards); the 7 `ConstructionYardTypes` lists keep only `zerg_hive`/`zerg_lair` (and `td_gdi_defenserig` in `McvExpansionManagerBotModule`); 4 dead ids deleted (`chsupply`, `glsupply`, `usasupply`, `refinery`); 26 harvesters × 5 personalities out of `ExcludeFromSquadsTypes` | `ai_harvester_gate_20260927` to tick 6000: the Atreides bot owned **7 → 7** actors (inert) with the harvester role only and **11 → 22/24** with these roles, +2 harvesters built in both runs; its harvesters (`far`: more than 25 cells from base) were **4 of 4 far by tick 6000 without** the `ExcludeFromSquadsTypes` target, drafted into attack squads as the maintainer saw, and **0 in 4 of 4 runs with it**; the same gap already existed on master for `ra1_soviets_heavyindustrialminer`, `futuretech_prospectormk2` and `wc2_humans_militiapeasant`. `bot-roles.log`: 31 refinery and 31 conyard members |

**Interim repair (2026-09-28, EMBER, `devin/ember/ai-faction-wiring`):** the "would add" set was
a live bug — the five newest factions were inert because their ids were in no central list. The
ids were hand-appended (119 ids across 66 rows, all verified defined) until `Apply:` drains them;
`Apply` unions into set types so the enumeration dedupe-composes with the role mechanism. The
table's "written" column therefore now includes these ids — do not re-review them as pending
additions.

**Where the central ids are (measured 2026-09-29, master `f37342668`: 5,825).** The metric went
**up** from 4,092, and nothing caught it, because `count_central_ids.py` is in no gate: +670 from
#588's interim repair (4,601 after `06c74eff4` lowercased it), +580 from `1d754587f`
(`SquadManagerBotModuleCA@classic`) and +644 from `3fc9e7e5f` (`SquadManagerBotModuleCA@guerrilla`).
Every personality block carries its **own full copy** of the squad lists, so each new personality
adds ~600 ids. The seven `SquadManagerBotModuleCA` copies hold ~4,270 of the 5,746 list entries:
`GuerrillaTypes` 1,776 (254 × 7), `HighValueTargetTypes` 794, `ExcludeFromSquadsTypes` 745,
`BigAirThreats` 284, `AirUnitsTypes` 222, `NavalUnitsTypes` 215, `StaticAntiAirTypes` 177. A role
that targets a SquadManager field drains all seven copies at once, so the squad lists come first;
the base-builder lists (`PowerTypes` 33, `BarracksTypes` 30, `ProductionTypes` 88) are worth far
less. A `--max` ratchet in `run_all.sh` follows the next role that lowers the count.

### 2.8a The guerrilla role: generated onto the actors, because the band is per faction

**Ruled 2026-09-27:** guerrilla (raid) units are fast/light only, generated from traits, never
hand-typed (AI_SYNTHESIS.md §3.1). **Bands ruled 2026-09-29 (maintainer, from four measured
options):** a unit is a guerrilla when it is in the **fastest third** of its own faction's infantry
(or vehicles) **and** costs **at most that group's median**.

The band is relative to the *faction*, and the bot cannot see ContentPacks at rules load, so
`BotRoleSets` cannot compute it. `tools/ai/derive_guerrilla_roles.py` computes it offline (the
faction is the pack, `ContentPacks/<Theme>/<Faction>/`; an actor still in a central rules file is
`<file stem>/<id prefix>`, e.g. `outpost2/eden`) and writes `BotRoles: Roles: guerrilla` on the
actor **in its own file**. That keeps the id out of `ai.yaml` (plug and play) and the unit list
out of human hands. `tools/audit/audit_guerrilla_roles.py` (in `run_all.sh`) reruns it with
`--check` and fails on any drift.

The pool, per faction and per infantry/vehicle group: producible (a queue some trait `Produces` or
some `*ProductionQueue` declares: no factory makes the `Disabled` queue, and the Zerg queue
is a player queue), armed with a weapon that can hurt an enemy (`scrin_repair_drone`'s beam is
ally-only), ground-mobile (not `naval` or `subterranean`), not an engineer, saboteur or spy
(`Captures` with a `building` type, or `Infiltrates`: every infantryman also `Captures`
`ra2garrison`), and not from the artillery, artillery-tank or fire-support template (§12.4a).
**Result: 154 actors in 37 factions, 57 infantry and 97 vehicles, 1–8 per faction; 12 of them
live in central rules files (Outpost 2, TS, WC2).**

Two traps the generator handles, both caught while building it:
* **A tag leaks to children.** An actor that inherits another actor inherits its `BotRoles`:
  the Plymouth Lynx chassis is in the band and the heavier Tiger (`PLYMOUTH_TIGER_*`) that
  inherits it is not. The generator compares the **resolved** roles and writes `-BotRoles:` on
  such a child (10 today), so `--check` also catches a new child of a guerrilla.
* **A child `Roles:` replaces the parent's.** A new block repeats every role the actor keeps.

**Not applied yet.** Tagging is inert: `guerrilla` has no `Targets` entry. Applying it
(`guerrilla: SquadManagerBotModuleCA.GuerrillaTypes` + `Apply`, and the seven 254-id lists
emptied) removes **1,776 central ids** and changes behaviour (254 raiders → 154, the 51 aircraft
leave the ground guerrilla squad), so it lands only after a Nuclear Winter A/B (§12.10 gate).

---

## 3. Reading the enemy: the observation model

The personality manager is only as good as its inputs, and the inputs are where the no-cheat goal
is won or lost (§0).

### 3.1 Fogged observation, deliberately

Every signal below must be computed from **what the bot is entitled to know**: actors currently
visible, plus a decaying memory of actors seen earlier. Concretely, gate scanning on
`player.Shroud.IsVisible`/`IsExplored` for the cell, keep a per-enemy `LastSeen` record with a
tick stamp, and let confidence decay with age rather than snapping to zero. This is a change of
*policy*, not of plumbing: the scan loops already exist, they simply don't filter on shroud today.

Two honest consequences to accept up front: a fogged bot will sometimes attack into a defence it
should have scouted, and it needs scouting to play well — which is why OpenHV's `ScoutBotModule`
stops being a nice-to-have and becomes a dependency of this design. Cameo has no scouting
behaviour at all today.

### 3.2 Per-enemy signals

For **each** enemy player, tracked independently (this is the part CN does not have):

| Signal | Derivation | Feeds |
|---|---|---|
| Static defence count / value | visible defensive buildings owned by that player | Steamroller, artillery demand |
| Army value and composition mix | visible combat units, by class (inf/veh/air/naval) | counter-composition, AA demand |
| Tech level | highest-tier visible production and tech buildings | Tech matching |
| Expansion count | distinct visible base clusters / refineries | Guerrilla |
| Economy proxy | visible harvester count × refinery count | boom detection |
| Aggression | our losses attributable to that player over a window | Turtle, target switching |
| Proximity / reachability | path distance from our base to their nearest cluster | target feasibility |
| Superweapon presence | visible superweapon structures | urgency override |
| Cloak/stealth reliance | share of seen units with stealth traits | detector demand |
| Confidence | age of the newest observation for that player | damping on everything above |

`PlayerStatistics` (`engine/OpenRA.Mods.Common/Traits/Player/PlayerStatistics.cs`) gives
`KillsCost`, `DeathsCost`, `ArmyValue`, `AssetsValue`, `Income` per player, which is useful for
end-of-match logging but is **aggregate, not pairwise** — it cannot say "player 3 is the one
killing my units". Pairwise attribution needs the master module to keep its own ledger, keyed by
attacker owner, which it may do freely under §1.1.

### 3.3 Derived global signals

Own army value vs summed visible enemy army value; own income trend; whether we are ahead or
behind on tech; whether any of our production is dead; map control proxy (owned/visible resource
patches). CN's danger score is the precedent for the shape.

---

## 4. The personality manager

### 4.1 What a personality is, extended

A personality is a condition that selects one of six `SquadManagerBotModuleCA` instances
(§19). The sixth — **Guerrilla**, the missing personality the user named — is now wired end
to end (2026-09-28, EMBER): `personality-guerrilla` joined the controller's default
`Conditions`, `SquadManagerBotModuleCA@guerrilla` fields the small-squad harassment tuning
(`JoinGuerrilla: 100`, `IndirectRouteChance: 60`, `StageBeforeAssault: false`, `PreferMainTarget:
false`, 700-tick attack cadence, `HarasserTypes` — the dormant CA HVT-strike list — finally
populated with the cross-faction elite/commando roster), and the observer notification exists. Its trigger was already in the master module:
`ExpansionClusters >= GuerrillaMinClusters` on the main target yields `guerrilla` — an enemy
spread across many expansion clusters gets its economy raided from several directions instead
of meeting one blob.

### 4.2 The switch mechanism, given §1.1

The manager cannot grant a condition. The round-trip, following the `PlacePlugAI` precedent
(§1.1):

```
master module (IBotTick, unsynced)      decides personality P for this player
        │  bot.QueueOrder(new Order("SetBotPersonality", player.PlayerActor, false)
        │                 { TargetString = P, SuppressVisualFeedback = true })
        ▼
BotPersonalityController (synced, on Player, IResolveOrder)
        grants the ExternalCondition token for P, revokes the previous one
        ▼
existing condition consumers: SquadManagerBotModuleCA@<P>, ProvidesPrerequisite@personality_<P>
        (→ personality-specific compositions, §1.4), ObserverConditionNotification@<P>
```

This keeps every existing consumer unchanged, keeps the synced state machine tiny and
deterministic, and makes each switch a replay-visible event. `BotPersonalityController` owns
the initial draw as well as later switches; when switching is disabled, the lower tiers preserve
the fixed random personality.

The observer notification in §19 fires per trait instance once, so it needs a small change to
announce repeat switches; live players must still see nothing.

### 4.3 Main target selection — the user's question

The master module owns it. Per enemy player, a target score from §3.2, roughly:

```
score(e) = w_reach · reachability(e)
         + w_weak  · (our army value / their visible army value)
         + w_hurt  · damage we have dealt to e / damage e has dealt to us
         + w_econ  · their economy share of the enemy team
         + w_kill  · closeness to elimination
         - w_def   · their fortification
         - w_ally  · number of our allies already committed to e
```

Re-evaluated on the same slow cadence as personality (not every tick), with the incumbent target
carrying a momentum bonus. The user's "check how well it is doing against that player" is the
`w_hurt` term, and it is what makes the target switch when a fight is going badly: a sustained
adverse trade ratio against the current target lowers its score until a softer teammate outranks
it. Explicit guards, all learned from CN's momentum documentation: a minimum hold time, a
mandatory re-target when the current target is eliminated or unreachable, and an override when a
different player is actively killing our base (you do not get to ignore who is hitting you).

Target and personality are **coupled but distinct**: the target answers "who", the personality
answers "how". The personality is chosen against the *selected target's* profile, damped by the
worst threat among the others — otherwise the bot turtles against a rusher it isn't fighting, or
steamrolls into a fortified target while a second player razes its base.

The `w_hurt` **producer** landed 2026-09-28: `CombatAnalysisBotModule` (Cameo, ported from CN
`30cf70a`) implements `IBotThreatAnalysis` — per-role threat weights fed by `IBotRespondToAttack`
with decay, plus a nemesis score per enemy player (the "damage e has dealt to us" side). The
dealt side landed the same day (EMBER): `INotifyAppliedDamage` fires on the *attacker's* player
actor (`Health.cs`), so `dealtScores` mirrors `nemesisScores` with the same per-player throttle,
weight, cap and decay. Consumed 2026-09-28 (EMBER): `WeightHurt` scores the **taken share**
`taken/(taken+dealt)` — the bounded form of the dealt/taken ratio — and a nemesis above
`NemesisOverrideWeight` force-retargets regardless of hold time — the 'do not ignore who is
hitting you' clause.

### 4.4 Transition table

The user's five cases, plus the ones the design needs to cover. "Signal" is per §3.2, evaluated
for the main target unless stated.

| Detected situation | Personality | Also |
|---|---|---|
| High static defence count / value | Steamroller | artillery/siege composition tokens; slow massed push |
| Aggressive expansion, many clusters | Guerrilla | many small squads, simultaneous raids on outlying clusters |
| Enemy rushing us (early aggression, our losses spiking) | Turtle | static defence fractions up, defensive squads near base |
| Few defences and small army | Rush | small squads from multiple directions |
| Enemy teching (tier climbing fast, low army) | Tech | match tech pace; keep enough army to punish |
| Air-heavy enemy | (keep) | AA demand up, `AirToAirUnits`/`StaticAntiAirTypes` priority |
| Naval-heavy on a water map | (keep) | naval squad share up |
| Mass infantry | (keep) | anti-infantry weighting |
| Mass armour | (keep) | anti-armour weighting |
| Stealth reliance | (keep) | detector demand |
| Economic boom, no army | Rush | punish now; the window closes |
| Superweapon under construction | Rush/Guerrilla | urgency override on hold time |
| We lost production structures | Turtle | rebuild before committing |
| Two enemies focusing one ally | (keep) | target the aggressor, not the score leader |
| No contact / nothing known | Expansion | scout; take map while blind |
| No signal crosses a threshold (terminal fallback) | Turtle if Pressured, else Expansion | the candidate set always yields a posture; the incumbent is no longer a silent default |

**Measured defect, fixed 2026-09-28 (EMBER):** in nw-ab-7 both `hard` matches sat in `turtle`
for ~38k ticks while the candidate stayed `rush`. The latch was not a missing candidate — the
emergency checker had a single 600-loss threshold, so the loss window bounced across it every
25-tick check, each flicker flipped the candidate to `turtle` and back and reset the
sustained-candidate timer, so `rush` never accumulated its reaction delay. Two changes: an
off-threshold (`EmergencyLossClearThreshold = 300`, half the on-threshold) gives the emergency
state hysteresis, and `PersonalityCandidates` now always ends with a posture yield (Turtle under
pressure, Expansion when calm) so no profile can starve the switcher of a decision.

Note the "(keep)" rows: **most enemy facts should change composition and priorities, not
personality.** Personality is the coarse posture; a six-state machine cannot express "he went
air" and should not try. This split is deliberate and is the main structural opinion in this
document.

### 4.5 Switching policy

Copy CN's shape, with numbers as tunable fields, not constants: slow re-evaluation cadence
(~1500 ticks); a fast emergency check (~25 ticks) that can force Turtle on a danger spike; a
minimum hold time (~3000 ticks); an incumbent momentum bonus small enough not to mask the signal
each personality exists for; earliest-tick and army-ratio gates so Steamroller cannot be chosen
before a mass exists; and a per-personality coverage penalty across allied bots so a team of bots
diversifies. Every switch is logged (§6) with the signal vector that caused it — without that,
the learning loop has nothing to learn from and the behaviour is unexplainable in a replay.

---

## 5. The master module

`MasterAiBotModule` — one instance per player, singleton by construction, `IBotTick`.

**It owns exactly three decisions:** main target, personality, and the published signal snapshot
(the user's "input matrix"). Everything else stays where it is. The failure mode to avoid is a
second production or squad authority (§2.3), and the existing modules are competent; what they
lack is a shared view of the enemy.

```
MasterAiBotModule
├── observes   fogged per-enemy signals (§3), own state, our pairwise damage ledger
├── decides    main target · personality · urgency
├── publishes  an immutable snapshot other modules may read
└── acts       only by queueing SetBotPersonality / target-hint orders (§4.2)

readers (unchanged authorities, now better informed)
├── SquadManagerBotModuleCA@<personality>   who to attack, how big a squad
├── BaseBuilderBotModuleCA                  defence fraction, expansion appetite
├── UnitBuilderBotModuleCA                  composition eligibility, AA/anti-armour demand
├── UnitCompositionsBotModule (world, singleton) via personality tokens (§1.4)
└── specialists (harvester, MCV, power, support powers, capture, repair, scout)
```

As of 2026-09-28 the published snapshot is still **telemetry-only**: `DefenceFractionHint` and
`ExpansionAppetiteHint` reach `cameo-ai-situations.jsonl` but no in-game reader exists — there is
no `IBotSituationProvider` interface and `BaseBuilderBotModuleCA` does not consult it. Wiring it
means a new CA-side interface plus defaults for the `classic` stack (which has no master module);
that is the next integration seam, deliberately left for coordination since it touches the
CA-sync-tracked builder.

Publication should be pull-based — readers ask the master for the current snapshot — so the master
never has to know who its readers are, and a missing master degrades to today's behaviour instead
of crashing. That is the difference between "the master coordinates" and "the master is a single
point of failure".

What the master must **not** do: pick individual unit targets, choose build items, move squads, or
duplicate any decision a specialist already owns.

---

## 6. Logging and learning

### 6.1 The boundary that keeps this safe

Four strictly separated tiers. Crossing them is the only way this feature can break the game:

| Tier | Determinism | Where |
|---|---|---|
| Synced simulation | must be identical on every client | personality condition state only |
| Live bot reasoning | host-local, unsynced, free (§1.1) | master module |
| Match log | write-only, no gameplay effect | disk, end of match + on events |
| Learned parameters | read at map load, then frozen for the match | a data file, treated as configuration |

The hard rule: **learned data may only be read when the match starts, and must be identical for
every client, or it must not touch synced state at all.** A weight table that only steers the
master's own unsynced scoring is safe on the host. Anything that changes what a *condition* does
must be part of the map/mod configuration, not a file one client happens to have. No inference at
runtime, no network calls, no adapting mid-match from a file that another client cannot see.

`Log.AddChannel(name, file, isTimestamped)` writes to `Platform.SupportDir + "Logs"`
(`engine/OpenRA.Game/Support/Log.cs:111,128`) and mods already add channels from traits
(`ScriptContext.cs:146`, `TraitDictionary.cs:62`). The shipped phase-one writer uses
an append-only file with host authority, mutex coordination and bounded retries;
its exact contract is in `AI_MATCH_LOG.md`.

### 6.2 Shipped record-only log schema (one JSON object per line)

Phase 1 ships one versioned JSONL object per bot player per finished match. The
authoritative field order and writer rules live in [`AI_MATCH_LOG.md`](AI_MATCH_LOG.md).
`AiMatchLogRecorder` observes personality transitions without changing them, and
`AiMatchLogWriter` appends the records only on the host in regular non-replay worlds.
The game never reads the file back. The offline
[`aggregate_ai_matches.py`](../../tools/ai/aggregate_ai_matches.py) tool consumes
schema version 1 records; later decision and episode records remain proposals.

Three proposed learning record types, not the shipped schema above:

* `match` — map, ruleset hash, player slots (faction, difficulty, bot type, human/bot), duration,
  outcome per player.
* `decision` — tick, player, chosen personality, chosen main target, urgency, the full signal
  vector per enemy, and *why* (winning score and margin). One per re-evaluation, not per tick.
* `outcome` — per player per personality-episode: ticks held, army value delta, resources spent,
  units/buildings killed and lost, attributed pairwise against the main target of that episode.
  Plus per composition: ticks active, cost committed, value destroyed vs value lost.

The unit of learning is the **episode** (a personality held against one target), not the match.
Match-level win/loss alone is far too sparse to attribute — a 40-minute game with six switches
gives one bit of signal against six decisions, which is the credit-assignment problem in §8.

### 6.2a Integration boundary and learning stages

PR #329 adopts the merged #331 writer rather than shipping its competing
`CameoMatchRecorder`. Older `Logs/cameo_matches/*.jsonl` experiments have a different
schema and must not be mixed with `Logs/cameo-ai-matches.jsonl`. The retained writer
captures bot rows when all bots resolve or GameOver occurs, not necessarily at
completion of the whole world. Replays, loaded saves and non-host clients are excluded.
Prior PR #329 runtime recording evidence exercised the retired writer, not this one.

The shipped schema does not carry source hashes, module IDs or lobby options;
matching mod-version strings do not prove identical balance inputs. Missing stats
are zero, and overlapping personalities are not separately marked ambiguous.
Timeline length and retries are bounded, but file size and record size are not.
These are explicit limitations, not grounds for introducing a second recorder.

**Phase 1 — record only (implemented).** Emit schema version 1 logs, change no behaviour, and
leave aggregation offline. The shipped recorder, writer, schema, and aggregator are the proof of
concept deliverable. Verify the schema survives real matches and that the numbers are attributable.

**Stage B — offline aggregation extensions.** The shipped aggregate tool handles
schema-v1 bot rows; future episode-aware extensions would produce, per
(faction × enemy faction × personality) and (composition × enemy faction), the episode counts,
mean value-trade ratio and win contribution. This is where "which composition does badly" gets
answered once episode attribution exists, with uncertainty and coverage reported before
any number informs a decision. First compare fixed versus dynamic policies offline (§11.3.5).

**Stage C — offline weight fitting, still no online learning.** Fit the §4 scores' weights, or
simply a prior over personality choice per matchup, and ship the result as a committed data file
reviewed like any balance change. Bandit-style selection (UCB1/Thompson over personalities per
matchup) is a candidate from §8.2, not a selected Cameo policy. It must first show a benefit
over the fixed-policy comparator on compatible Cameo data (§11.3.5).

**Stage D — AI-vs-AI batch harness.** Headless repeated matches across matchups, feeding stages
B–C. This is what makes the data volume possible; it should be a script and a map rotation, not
engine work. *Shipped:* `tools/ai/run_ai_match_batch.py`. **Since the 2026-09-28 maintainer
ruling the duel map is the shipped tournament map "A Nuclear Winter"
(`mods/cameo/maps/_ra_a-nuclear-winter.oramap`)** — the harness extracts the .oramap into a
variant dir, seats a `Referee`, and converts `Multi0`/`Multi1` into map-side bots bound to the
map's real `mpspawn` cells. The acceptance A/B is the Frankenstein bot at a tier (`hard`,
fog-honest, every merged module) vs the `classic` `ModularBot` type (omniscient, pre-merge
modules, same difficulty scaling) — **not** the `fransbot` type, which is a hidden donor
(maintainer ruling 2026-09-28) — both spawns (`--repeats 4 --swap-bots`), `gamespeed: maximum`
locked — see `docs/design/AI_MATCH_LOG.md` § "The A/B acceptance protocol". Legacy fixture:
`mods/cameo/maps/ai_duel_gate_20260928/` (Desert Rats donor terrain, two real mirrored mpspawns)
remains usable via `--map` —
the harness copies the template into an isolated `Engine.SupportDir` user-map cache per matchup
(faction × bot × time-limit patching + faction starting-unit actors written into `Actors:`),
launches `Launch.Map`+`Launch.Benchmark` (exits on `GameOver`), and slices the appended
`cameo-ai-matches.jsonl` per run. Constraints the map design had to satisfy, verified against
the engine: a `Local` server refuses to start with every slot empty, so the map keeps an inert
host-occupied `Referee` slot whose `PlayerReference.NonCombatant` keeps it out of every record's
`opponents`/`allies` (lobby clients ignore `Player.NonCombatant`); empty playable slots produce
no `Player` at all, so the duelists are `Playable: False` + `Bot:` map-side players (the writer
admits them via `IsBot`) with `SpawnStartingUnits` bypassed by preplaced actors; Cameo strips
`MustBeDestroyed` from most actors so the map re-adds it to the base templates for real
elimination, and `TimeLimitManager` (locked) is the guaranteed terminator — its timeout ranking
reads `Playable` only, so a drawn duel records both bots `lost`. **Run bot tests at high game
speed:** the fixture locks `gamespeed: maximum` (1 ms timestep — CPU-bound, ~25x default at
parity hardware) so decisive matches resolve as fast as the box can tick them — the minutes
cap then spans 40x the ticks (`TimeLimit *= 60 * ticksPerSecond`, `TimeLimitManager`), and
`AdaptiveGameSpeed` pacing slows the target rate under CPU contention rather than janking, so a
generous wall bound plus a debug.log stall detector (`run_ai_match_batch.py`) replaces a tight
match timeout. Match records are only comparable within one speed — a `10`-minute maximum match
contains 40x the simulated play of a default-speed one and 10x an insane-era one, so pooled
baselines do not carry across the 2026-09-29 speed change.

**Stage E — anything neural.** Explicitly deferred until factions and balance are finished, per
the user's own sequencing. Training against a moving balance target fits noise.

Recording and coverage diagnostics help debug the current bots while balance moves. Learned
weights remain §10.6 phase 7, after the earlier delivery phases; neither this section nor the
batch-harness proposal authorizes skipping the observe-only and behavior-review gates.

---

## 7. Dependencies and risks

**Dependencies.** Fogged observation (§3.1) needs a scouting module or the bot plays blind.
Personality-specific compositions (§1.4) need composition authoring per faction — that is balance
work, not a port. The pack split (§2) should land before per-pack AI behaviour is authored, or the
migration has to be redone.

**Risks.**

* *Silent migration failure* — the global file keeps winning after a partial move. Mitigated by
  gating every step on a byte-identical `--resolved-rules` diff.
* *Load-time crash from removal syntax* in a pack (case 5). Mitigated by the "add, never remove"
  rule.
* *Composition module crash* if anyone reaches for multi-instance gating (§1.3).
* *Thrashing* — a manager that switches too often is worse than a random one. Mitigated by CN's
  hysteresis shape, and observable because every switch is logged.
* *Overfitting to bot opponents* — self-play data teaches beating bots, which is not the goal.
  Human replays are the only corrective, and there is no pipeline for them today.
* *Fog makes bots weaker before it makes them better.* Expect a temporary strength regression when
  §3.1 lands, and hold it against the honest-play goal rather than against win rate.

---

## 8. Outside research: how RTS AI actually does this

Provenance is separated deliberately: the items below are published work, not Cameo facts, and
each is cited so the claim can be checked.

### 8.1 Opponent modelling and strategy prediction under fog

Synnaeve and Bessière's Bayesian models predict an opponent's opening and build/tech tree from
partial, noisy observations, with parameters learned from replays
([CIG 2011](https://doi.org/10.1109/cig.2011.6032018),
[AIIDE 2011](https://doi.org/10.1609/aiide.v7i1.12429)). The structural lesson for §3 is that a
build tree is hierarchical, so a single sighting raises the probability of everything it implies —
which is exactly how a fogged bot should reason from one scouted building, instead of the
all-or-nothing knowledge it has today. Their framing of it as *keyhole plan recognition* is also
the right framing for Cameo: the bot infers intent from what it happens to see, without
interrogating the opponent.

### 8.2 Strategy selection as a bandit — the cheap win

The strongest *scripted* Brood War bots learn between games rather than within them. ZZZKBot uses
"a multi armed bandit online learning algorithm for opening selection" and, from AIIDE 2017,
"uses the results from past games for an opponent to decide which strategy to try the next game
against that opponent" ([Liquipedia](https://liquipedia.net/starcraft/ZZZKBot)). That is precisely
the user's "learn which personality works against which faction and build order", implemented with
a handful of counters and no neural network — which motivates the bandit candidate in §6.3 stage C.

Tavares et al. treat strategy selection itself as a game, filling a payoff matrix from recorded
matches and showing it pays to *deviate* from Nash equilibrium to exploit a suboptimal opponent,
with safe-exploitation bounds to limit the downside
([AIIDE 2016](https://doi.org/10.1609/aiide.v12i1.12857)). Two consequences for §4: a
personality-vs-enemy-strategy payoff matrix is the natural learned artifact, and a bot that always
plays the "safe" counter is exploitable by a human who notices — some deliberate randomisation is
correct, not sloppy.

### 8.3 Learned high-level switching, and its cost

Gehring et al. cast high-level strategy selection in Brood War as reinforcement learning where an
action *is* a switch to a strategy, under partial observability, and report substantial win-rate
gains over a fixed-strategy baseline ([arXiv:1811.08568](https://www.alphaxiv.org/abs/1811.08568)).
This is close to the user's target and is the reason §6.3 stage E is not dismissed. But it is also
the reason it is last: it needed a research team, a mature bot to sit inside, and training volume
Cameo cannot produce until the balance stops moving. AlphaStar-class approaches are further still
outside reach and, more importantly, outside the point — a deterministic, auditable manager is
worth more to a mod that people have to debug and tune by hand.

### 8.4 What the literature says the failure modes are

Recurring across the above: **credit assignment** (which of many decisions caused the loss — §6.2's
episode records exist for this), **non-stationarity** (an opponent that adapts invalidates learned
weights, hence bandits with exploration rather than fixed tables), and **distribution shift**
(training against bots does not transfer to humans — §7's overfitting risk).

---

## 9. Open decisions

1. **Does fogged observation ship, and when?** It is the difference between "no cheats" being true
   and being a slogan, and it will make bots temporarily weaker. Maintainer's call.
2. **Is Guerrilla a sixth personality or a mode of Rush?** Sixth costs another squad-manager block
   and a token; a mode is cheaper but less legible in logs. Leaning sixth.
3. **How far does the pack split go?** Dictionary rows only (option A, no C#), or eventually the
   fragment registry (option D)?
4. **Where do learned weights live** — committed yaml reviewed as balance, or a support-dir data
   file? The synced/unsynced boundary in §6.1 permits either; reviewability argues for committed.
5. **Do we want a human-replay pipeline** at all, given §8.4's distribution-shift warning?
6. **Are the ten difficulty tiers all supposed to get the manager**, or is dynamic switching itself
   a high-difficulty feature? Making it difficulty-gated is a cheap, honest difficulty axis.
7. **How deep does the opponent model go in phase 2** — per-enemy strategy labels as §3 designs, or
   own-state plus a fortification scalar as CN actually ships (§1.6)? Kept open on purpose: the
   phase-2 logs are the evidence that settles it.
8. **Who owns contact memory** — one memory built by the master, or an extension of the squad
   manager's own scan, which is where CN keeps it? One authority per decision (§10.1) says pick one,
   and the master needs it per enemy player while the squad manager needs it per cell.
9. **How verbose is the decision trace** (§11.3.2) — always-on JSONL, or behind a debug flag? Eight
   bots re-deciding every 1500 ticks with a full candidate vector is small; the same trace on the
   emergency cadence is not.
10. **What may an emergency override change** — target and urgency only, or the personality too? CN
    switches the personality straight to Turtle on a danger spike (§1.6); the review reply argues an
    emergency should never rewrite the strategic posture. Unresolved conflict, and the answer decides
    whether §4.5's fast path needs its own hold time.
11. **Do CN's hysteresis constants ship as Cameo's defaults**, or get re-fitted from phase-2 logs
    before phase 3 turns switching on? Leaning: ship CN's as the starting point, since they were
    tuned against a switching bot in this engine family, and re-fit after the first logged matches.
12. **How does a fog-honest offense get fresh target intel?** Measured 2026-09-28 (nw-ab-5/6,
    `FransGeneralBotModule` recon loop): ordinary RECON selects only the nearest *stale
    MineCluster* on a geographic fan from home — there is no candidate class for "the enemy's
    probable base". `FransMissionType` has no assault verb; `Raid` is the only offense, and
    `TryBuildGroundRaidBid` rejects remembered-intel targets (a deliberate fog-honesty rule —
    `FransRaidIntel` docs reserve remembered-building strikes for Sea). Result on A Nuclear
    Winter: recon fans stall on mineral waypoints short of the enemy base, every RAID publishes
    `bids 0`, zero enemy buildings die across six matches. The choices, in increasing size:
    a. **Spawn-directed recon** — `Map.ActorDefinitions` `mpspawn` cells are public map data
       (lobby-visible to every human). Add unscouted-spawn cells as a RECON candidate class
       alongside mine clusters (same cooldown/staleness machinery, higher priority for cells
       whose fan arm is unexplored). Fog-honest, minimal, and it is what every human does.
    b. **Bounded remembered-building raids for ground** — permit `IsRememberedIntel` targets
       when `IsBuilding` is true (buildings cannot move; last-seen cell stays valid), inside a
       freshness window. Extends the Sea-only rule by one axis.
    c. **A distinct assault/base-attack verb** — a heavier mission type with escort/consolidation
       semantics, versus teaching Raid to fill the gap.
    Leaning (a)+(b): they are orthogonal, both fog-honest, and together they close the
    "no fresh targets -> no bids -> no pressure" funnel without new verbs. (c) only if the
    combined change still cannot produce raid bids in measured matches.

---

## 10. The module plan: every module, what changes, and how they connect

§5 states the master module's role in the abstract. This section is the concrete build plan: what
each module that Cameo actually loads does today, what changes for it, and what it is allowed to
read. It exists so that implementation can start module by module without re-deriving the design,
and so that a reviewer can check any single module against it in isolation.

### 10.1 One authority per decision

The load-bearing rule, and the one most likely to be violated by accident:

> A decision has exactly one owner. The master module changes the *inputs* to a decision. It never
> makes a decision another module already owns.

Concretely: the master may raise the AA demand that `UnitBuilderBotModuleCA` reads, but it never
queues a production order itself; it may name the main target, but `SquadManagerBotModuleCA` still
picks which squad attacks what. Every past AI regression in this tree came from two writers of the
same state, and §1.3 shows the engine punishes duplicate authorities with a hard crash rather than
a subtle bug.

The second rule follows from it: **absence degrades, it never breaks.** Every reader treats a
missing master, a missing snapshot, or a stale snapshot as "carry on as today". That is what makes
this incrementally shippable — each phase in 10.6 is a complete, playable state.

### 10.2 What Cameo loads today

> **The authoritative list is generated:** [`AI_MODULE_MAP.md`](AI_MODULE_MAP.md), built by
> `python tools/ai/ai_module_map.py --write` from the resolved `Player`/`World` and the C#. On
> 2026-09-27 it lists **32 loaded bot types, 67 instances**, the interface provider → consumer
> graph, and four checks (consumer without provider, producer without consumer, unloaded code,
> shadowing). The table below is the annotated narrative; where they disagree, the map wins.
> Run `--check` before citing a count.

Verified on 2026-09-07 from the active `mods/cameo/mod.yaml` manifest and resolved
`Player` / `World`, against upstream base `291052380`. Scope here is the decision modules,
their explicit coordination adapter, and the three data/limit providers named below:
**51 distinct trait types, 69 Player instances plus one World instance** (2026-09-28: #621 adds
`SquadManagerBotModuleCA@guerrilla`, the 69th instance; #607 adds `ResourceMapBotModule@fransbot` and `SquadManagerBotModuleCA@classic`, the 67th–68th instances; #578's Route-A Fransbot port adds 24 vendored `Frans*BotModule` types / 24 instances, the 28th–51st / 43rd–66th, which run only under the `fransbot` bot type; `BeaconResponderBotModule` (#580) is the 27th type / 42nd instance; `CncEngineerBotModule` (#562), `CombatAnalysisBotModule` (#564) and `HumanPaceBotModule` added the 24th–26th types / 39th–41st instances; `ScoutBotModule` was the 23rd/38th). Conditional instances
are loaded, not necessarily enabled simultaneously. This replaces the old unqualified
"20 loaded modules" claim. The scope does not count `ModularBot` dispatchers,
`GrantConditionOnBotOwner`, `BotInsurance`, generic condition/prerequisite traits, or observers;
it is not a claim that nothing else affects bots.

Sources: `mods/cameo/ai/ai.yaml`, active pack additions, and the `SupportPowerBotModule`
inherited through Player rules. C# implementations are under
`OpenRA.Mods.CA/Traits/BotModules/`, `engine/OpenRA.Mods.AS/Traits/BotModules/`,
`engine/OpenRA.Mods.Common/Traits/BotModules/`, with Cameo's
`Traits/BotModules/CratePickupBotModule.cs` and `Traits/BotGlobalUnitBudget.cs`.
Names below are actual resolved types; `PowerDownBotModule` resolves to AS
(`PowerDownBotManager.cs`), not the separately named CA implementation.

**Boundary legend:** **U** = local bot reasoning / callbacks; only queued orders may change
simulation. `ModularBot` wraps both `IBotTick` and `IBotRespondToAttack` in
`Sync.RunUnsynced`; it does not activate bots in replays. **R** = rule/state provider read by
local reasoning, not an autonomous order issuer. Provider trait existence on all peers does
not make a local cache safe for synced consumers. None of these rows reads the proposed
master snapshot today. "Hint" below is a future read-only integration, not shipped behavior.

| Actual type (loaded instances) | Decision / data ownership today | Current reads; future snapshot hint | Publishes / cadence / boundary |
|---|---|---|---|
| `SquadManagerBotModuleCA` (5 personalities) | squad assignment, combat posture and retreat | actors, attack events, base positions, limits; main target + urgency hint | orders, idle-unit and position callbacks; roles 25 ticks, attack-force 50/100/75/75/60 by current personality; U |
| `BaseBuilderBotModuleCA` (@generic) | structure choice, placement and construction priorities | queues, economy, terrain, limits, base/defence state; defence/expansion hints | build/placement/rally orders, production-pause query; queue-state delays and configured building intervals, not one universal period; U |
| `UnitBuilderBotModuleCA` (@generic) | unit production and composition selection | buildability, cash, idle units, requests, pause providers, composition table; counter-demand hint | production orders, requested-production count; feedback loop plus `UnitBuilderInterval`, per-unit delays and composition-selection gates; U |
| `UnitCompositionsBotModule` (World singleton) | composition definitions and lookup data | resolved rules and configured rows; no direct snapshot read planned | `Info.UnitCompositions`, prerequisite/queue/cost dictionaries at construction; R |
| `SupportPowerBotModule` (1) | Common decision-table power targeting | available powers, funds, configured decisions, target actors; target preference if integrated | power orders; each bot tick with per-power retry delays; U |
| `SupportPowerBotASModule` (1) | AS decision-table power targeting | same manager, AS decision rows and target evaluation; target preference if integrated | power orders; each bot tick with its own per-power retry delays; U |
| `SendUnitToAttackBotModule` (default, @chrono) | configured opportunistic attackers | eligible units/targets and attack desire; target preference | attack orders; `ScanTick` default 463; U |
| `HarvesterBotModuleCA` (1) | harvester task/threat handling | collectors, resources, threats and unit requests; economy-pressure hint later | harvesting/movement and production requests; idle scan configured 1000 ticks plus other source callbacks; U |
| `McvExpansionManagerBotModule` (1) | MCV deployment and expansion | mobile construction actors, resource map, base positions, unit builder; expansion hint | movement/deploy orders, production requests, position callbacks; new-MCV scan default 20, build check 101; U |
| `CaptureManagerBotModuleCA` (1) | capture assignment | eligible capturers/targets and visibility configuration; target preference | capture orders; minimum capture delay configured 125 ticks; U |
| `BuildingRepairBotModule` (1) | Common building repair response | attacked actor, damage state, repair capability | repair orders on `IBotRespondToAttack`, including cooldown-gated all-building scan inside that callback; U |
| `BuildingRepairBotModuleCA` (1) | CA repair response path | cached repair trait, damage event and attacker relationship | conditional repair orders on `IBotRespondToAttack`; U |
| `PowerDownBotModule` (1) | power toggling | power totals and toggleable buildings; no new hint | PowerDown orders; interval default 150 ticks; U |
| `CratePickupBotModule` (1) | crate collector assignment | crate candidates, collectors and visibility setting; no new hint | movement orders; scan configured 300 ticks; U |
| `LoadGarrisonerBotModuleCA` (@Infantry) | passenger-to-garrison assignment | configured passengers, garrisons, capacity/proximity; posture hint only in a later phase | Stop/AttackMove/EnterGarrison orders; scan default 457 ticks; U |
| `LoadCargoBotModule` (@Infantry/@TankBunker/@Battery) | configured cargo loading | passengers, transport capacity and proximity; no phase-1 hint | cargo-related orders; scan default 317, Battery configured 799 ticks; U |
| `MinelayerBotModule` (1) | minefield assignment | minelayers, positions and attack events; posture hint later | mine-related orders; scan default 320 ticks and attack callbacks; U |
| `ResourceMapBotModule` (1) | resource-index information | resource layer and nearby actors; no snapshot hint | index/threat query methods; `UpdateResourceMapInverval` default 67 ticks, randomized initialization; U provider |
| `ExternalBotOrdersManager` (1) | forwarding registered external requests | direct entries / `IssueOrderToBot` registrations and current issuer validity | queued orders each bot tick; local bridge, not a new strategy owner; U |
| `BotLimits` (10 difficulty instances) | configured cap/delay inputs | enabled difficulty condition; no master replacement | enabled `Info` queried by consumers; no independent tick; R |
| `BotGlobalUnitBudget` (1) | global-budget production pause input | living bots, owned mobile actors, exclusions and configured clamps | `IBotRequestPauseUnitProduction.PauseUnitProduction`, lazy recalculation at default 25-tick interval; R/local cache |

Intervals are simulation ticks, not milliseconds, and are gates rather than promises to emit
an order every interval. Startup randomization, disabled conditions, affordability, target
availability and existing queue backpressure still apply. These contracts do not alter any of
them or certify the separate economic-fairness claims elsewhere in this design.

### 10.2a Interaction and failure contracts

The following is the integration contract; absence of the future snapshot preserves current
decisions. A delayed rebuild must not trigger a compensating burst of production or orders.
Invalid/dead targets are revalidated by the decision owner; an unavailable hint is not permission
to mutate simulation directly. Existing overlap is recorded honestly below: the target design's
"one authority" rule is not proof that today's modules never compete.

**SquadManagerBotModuleCA.** Retains tactical ownership; the master may suggest a strategic
target but cannot commandeer squad members. Existing idle-unit notifications feed the unit
builder, and position updates coordinate bases. On future personality changes, re-resolve
enabled manager references rather than caching a disabled instance. No hint means current
targeting, including its existing visibility limitations, remains in force.

**BaseBuilderBotModuleCA.** Owns construction orders and placement attempts; production-pause
responses are advisory to the unit builder, whose existing opening-defence exception remains.
An unaffordable or unplaceable candidate stays subject to current retry/backoff limits.
Expansion appetite must not become an independent second building queue in the master.

**UnitBuilderBotModuleCA.** Owns fulfilling unit requests, not callers such as the harvester
or MCV manager. It checks non-base pause providers before its base-builder opening-defence
exception; a budget pause must not be bypassed by a future counter-demand hint. Missing
composition eligibility leaves existing selection behavior intact, not an invented fallback unit.

**UnitCompositionsBotModule.** Is data, not a second production agent. Consumers own selection
and prerequisite evaluation. Preserve the singleton: adding condition-gated duplicates breaks
single-trait lookup even when most copies are disabled (§1.3). Future personality influence
travels through properly supplied prerequisite tokens, not mutation of shared dictionaries.

**SupportPowerBotModule.** Owns its Common decision rows and queues, never directly executes,
power orders. Missing decision entries do not fire powers; no affordable target triggers the
existing decision-specific retry. Its co-presence with AS is real: future integration must
compare decision `OrderName` coverage before claiming exclusive power ownership.

**SupportPowerBotASModule.** Retains the AS targeting algorithm and separate retry dictionary.
A suggested main target cannot override power readiness, cost or target-validity checks.
There is no shared reservation protocol with Common today; any overlapping decision rows
require an explicit later ownership decision rather than silently disabling one module here.

**SendUnitToAttackBotModule.** The two instances retain their configured unit selections and
attack-desire accumulation. Missing suitable attackers/targets leaves them waiting. The master
does not issue substitute attack orders; avoiding overlap with squad ownership is an integration
acceptance test, not an already implemented reservation system.

**HarvesterBotModuleCA.** Keeps collector control and requests replacements through the unit
production interface. A future danger hint is additional input, not a command to abandon the
economy. Unavailable resources, actors or production capacity retain current retry behavior;
the master cannot create a collector or bypass the builder.

**McvExpansionManagerBotModule.** Uses ResourceMap's indices and requests production through
the builder; it owns deployment decisions. Missing viable expansion locations must not cause
another module to deploy the same MCV. Current `MoveConyardTick: 0` is preserved; a future
appetite hint does not implicitly enable that separate relocation behavior.

**CaptureManagerBotModuleCA.** Keeps capturer eligibility and capture-target checks. A stale
main target is discarded as a preference, not forced through capture restrictions. Easiest-bot
condition gating remains; waiting for the minimum delay cannot transfer ownership to the master.

**BuildingRepairBotModule.** Receives attack callbacks and inspects repair-capable actors
through the existing Common path. Its all-building scan is cooldown-gated inside the attack
callback, not independently scheduled; without callbacks there is no scan. Unsupported actors
produce no direct repair order; future hints must not create an unconditional expenditure loop.

**BuildingRepairBotModuleCA.** Is separately loaded, not an alias of Common. Its source caches
`RepairableBuilding` from the trait's own actor at creation, so loading it on Player does not
prove it repairs attacked buildings: a missing cached trait makes its response inert. This is
a diagnosis boundary, not authorization to change attachment or repair behavior in this task.

**PowerDownBotModule.** Reads the power manager and eligible buildings, then queues toggles.
It does not own construction of replacement generators. Insufficient toggleable capacity
remains a power-management limitation; future base-builder hints cannot justify direct power
or condition changes inside this local module.

**CratePickupBotModule.** Chooses a collector under existing distance/eligibility and visibility
settings and queues movement. No crate or collector yields no useful assignment; future squad
coordination must explicitly resolve competing actor orders rather than inventing an actor-lock
API absent from today's code.

**LoadGarrisonerBotModuleCA.** Chooses passengers and garrisons, with the existing capacity and
scan limits. Full/dead destinations require current eligibility checks; a future Turtle hint
must not bypass capacity or force the same infantry simultaneously into a squad and garrison.
Such arbitration is future work, not a shipped shared allocator.

**LoadCargoBotModule.** Keeps each instance's configured passenger/transport policy. A full or
missing transport does not authorize an unrelated module to spawn one. Preserve the three
instances and Battery cadence; cross-instance passenger contention must be tested if their
selection sets change.

**MinelayerBotModule.** Owns mine-placement assignments, using its current periodic and attack
inputs. No usable minelayer or site leaves no assignment. A posture hint may bias sites later,
but cannot grant mines or issue orders independently from this owner.

**ResourceMapBotModule.** Publishes local derived indices through query methods; it is not
the planned shared strategic snapshot. Consumers must tolerate stale information and recheck
actors/locations before orders. Its scan data must not feed a synced trait directly merely
because the source world is synced.

**ExternalBotOrdersManager.** Forwards valid registered requests rather than deciding global
strategy. Dead, out-of-world or no-longer-owned direct issuers are skipped, and direct entries
are cleared after processing. Preserve the source's distinction between direct entries and
`IssueOrderToBot` registrations; it is not a durable, acknowledged inter-module message bus.

**BotLimits.** Supplies enabled-instance policy to builders and squads; it neither publishes
a snapshot nor emits orders. Consumers must handle condition selection using their existing
refresh rules. The master must not silently replace difficulty limits while changing personality;
these are different axes.

**BotGlobalUnitBudget.** Is a query-driven pause provider, not an actor producer or remover.
Disabled/nonpositive budget returns no pause; otherwise existing clamps and exclusions apply.
Cached results may wait for the next configured recalculation; no other local module may
interpret that cache as synchronized world state or bypass it through a new production path.

**Loaded, observe-only:** `MasterAiBotModule` publishes the immutable local snapshot
at the §10.5 cadence (emergency ~25, rebuild ~150, decisions ~1500 ticks).
It has **no consumers and no hint reads**, and logs candidate personality and target values only.
Every difficulty issues `SetBotPersonality` after its sustained reaction delay; the synced
`BotPersonalityController` validates the order, ignores repeats, and owns the sole condition token.
`ScoutBotModule` remains a later owner of explicitly allocated scouting tasks after contact memory
and the visibility gate (§11.3), not a current capability. Reading an unsynced personality field
from simulation code is forbidden. `PersonalityReactionDelay` controls how long
a candidate must persist before switching; the ten difficulty tiers range from
7500 ticks on easiest to 750 on cameogod in 750-tick steps. The personality
hold is clamped to that reaction delay, so no tier reacts slower than its own
delay; a negative value preserves the fixed random personality fallback.

```text
Current synced world / rule data
    -> local modules + data providers (existing callbacks and queries)
    -> bot.QueueOrder -> synchronized order resolution -> world

Planned local Master snapshot --read-only hints--> existing decision owners
    |                                              -> same order boundary
    +-- SetBotPersonality order --> planned synced Controller --> condition
    +-- observation / decision records --> local diagnostics only
```

This specification changes documentation only. It adds no callbacks, shared snapshot consumers,
personality switching, scouting, fog restriction, priority arbitration or AI orders. In particular,
it does not re-adopt §11's rejected direct-condition mutation or multi-agent pipeline designs.

### 10.3 The snapshot: the one shared data structure

The user's "input matrix". One immutable object per player, rebuilt on the slow cadence, read by
everyone, written by nobody but the master.

```csharp
// unsynced, host-local, rebuilt on the slow cadence (10.5)
sealed class BotSituation
{
    int Tick;                                 // when this was built; readers check staleness
    Player MainTarget;                        // may be null: no contact yet
    string Personality;                       // what the master ASKED for, not what is granted
    Urgency Urgency;                          // Normal | Pressured | Emergency
    IReadOnlyDictionary<Player, EnemyProfile> Enemies;   // §3.2 signals, per enemy
    CounterDemand Demand;                     // AA, anti-armour, anti-infantry, detector, artillery: 0-100
    int DefenceFractionHint;                  // for the base builder
    int ExpansionAppetiteHint;                // for the MCV/expansion modules
}
```

Three properties are deliberate:

* **Pull-based.** Readers ask the master (`TraitOrDefault<MasterAiBotModule>()?.Situation`). The
  master does not know its readers, so a reader can be added without touching it, and a missing
  master is a `null` that every reader already has to handle.
* **Immutable and stamped.** Readers may cache it and compare `Tick`; nobody can mutate another
  module's view. This also makes the snapshot the natural log record (§6.2) — the thing we log is
  exactly the thing the bot decided on, so a replay explanation is never a reconstruction.
* **Hints, not commands.** `DefenceFractionHint` is an input the base builder may clamp or ignore
  by its own rules. That is what keeps 10.1 true.

`Personality` in the snapshot is the *request*. The authoritative state is the granted condition,
because that is what the five squad-manager instances key off. Readers that care about posture must
read the condition (as they do today), not this field; the field exists for logging and for the
one-tick window before the order resolves.

### 10.4 The one synced piece, and why an existing trait cannot do it

The switch has to cross from unsynced reasoning into synced state, and §1.1 allows exactly one
bridge: queue an order. `ExternalBotOrdersManager` (10.2) is the precedent for order plumbing, but
it runs the other way — synced traits register, the module queues. What we need is the
`PlacePlugAI` direction: module queues, synced trait resolves.

`GrantConditionOnOrders` (`OpenRA.Mods.CA/Traits/Conditions/GrantConditionOnOrders.cs`) looks like a
zero-C# answer and is not one. Its `ResolveOrder` revokes the condition on **any** order whose name
is not in its set (lines 44-47), and the Player actor resolves plenty of unrelated orders —
`PlaceBuilding` is a player-level trait (`engine/OpenRA.Mods.Common/Traits/Player/PlaceBuilding.cs:22`),
as are the production queues. The first building the bot places would clear its own personality.
Five instances would give correct mutual exclusion and still lose the state on the next placement.

So one small synced trait is unavoidable — the only new synced state in the whole design:

```
MasterAiBotModule (unsynced, IBotTick)
    bot.QueueOrder(new Order("SetBotPersonality", player.PlayerActor, false)
                   { TargetString = "guerrilla", SuppressVisualFeedback = true })
        |
BotPersonalityController (synced, on Player, IResolveOrder)   ~40 lines
    validates TargetString against its configured token map
    revokes the previous token, grants the new one, ignores a repeat of the current one
        |
existing consumers, unchanged:
    SquadManagerBotModuleCA@<personality>       (condition-gated instance selection)
    ProvidesPrerequisite@personality_<p>        -> personality-tagged compositions (§1.4)
    ObserverConditionNotification@<p>           (observer-only switch announcement)
```

The controller owns the initial draw as well as every later switch: deleting
`GrantRandomCondition@personality` prevents an unrecoverable first token from surviving a switch.
If switching is difficulty-gated off, the bot behaves exactly as it does today — a random fixed
personality — which is the degradation rule of 10.1 applied to the riskiest change here.

Two consequences to accept: every switch is a replay-visible order (good — it is auditable and it
is how the observer indicator learns about it), and the switch costs one order per change, so the
cadence limits in §4.5 are not just anti-thrash tuning, they are the cost control.

### 10.5 Cadence, and why the master is cheap

| Loop | Cadence | Work |
|---|---|---|
| Emergency check | ~25 ticks | danger delta only; can force Urgency=Emergency and Turtle |
| Snapshot rebuild | ~150 ticks | per-enemy signal scan, counter demand, hints |
| Target + personality decision | ~1500 ticks | scoring, hysteresis, the order |
| Log flush | match end + on switch | append JSONL (§6) |

The expensive part is the actor scan in the snapshot rebuild, and it is the same scan the squad
manager already does every tick — so doing it once per 150 ticks in the master and letting readers
share the result is a net *saving* if the squad managers are later pointed at the shared result
instead of scanning independently. That consolidation is not phase 1, but the snapshot is shaped to
allow it.

### 10.5a The assign layer: missions

Phase 7a adds the first assign-layer contract without creating another unit owner.
`MasterAiBotModule` publishes fog-honest `Raid` and `Defend` intent through
`IBotMissionProvider`; `SquadManagerBotModuleCA` consumes the ordered missions only when it is
about to form a new attack force. The producer derives missions from `RegionMemory`, while the
consumer retains ownership of force formation, target validation, and orders.

The strategist publishes intent rather than commanding units because squad membership and
execution already belong to the squad manager. A mission may defer a force for a bounded defend
hold or focus a newly formed raid, but it never moves units between existing squads and never
assigns a unit itself. `Recon` remains with `ScoutBotModule` and `Secure` is deferred to a later
phase. The assign layer must never become a second owner of a unit.

The mission consumer revalidates each cycle: an exhausted Defend posture is skipped so a later affordable Raid remains eligible, and a cleared threat releases the hold immediately. Raid target lookup first uses visible actors; in fogged mode the consumer may use the existing remembered frozen-actor path. With `FoggedScans` disabled, the fallback can select unseen actors because it inherits the existing omniscient behavior of that mode rather than introducing a mission-layer cheat. The permanent `ai_raid_gate_20260928` fixture proves Raid publication and target-bearing assignment with reachable enemy economy under fog; it does not assert frozen assignment because that path is not reliably reproducible in the fixture.

### 10.6 Build order, each phase shippable on its own

1. **Match logging, record-only.** No behaviour change. Writes the match record (§6.2) including
   the fixed personality and the outcome. Value: the learning loop has data before any decision
   code exists, and the log schema gets exercised while it is still cheap to change.
2. **`MasterAiBotModule`, observe-only (implemented).** Builds and publishes the snapshot; decides
   nothing, and no module reads it yet. Logged per rebuild. This is intentionally pre-fog, and the
   score omits `w_hurt` until phase 4 adds pairwise attribution. The signal derivations can be
   validated against replays cheaply without touching gameplay.
3. **`BotPersonalityController` + dynamic switching (implemented).** The first behaviour change.
   Difficulty-gated so the lower tiers keep today's fixed personality.
   The guerrilla rule is currently a computed-but-unavailable candidate: it remains in the
   situation log's candidate field, while the active five-personality controller falls through
   to the next grantable rule.
4. **Main target selection (landed).** `SquadManagerBotModuleCA` consumes the
   master's target for proactive, unbounded picks only when `PreferMainTarget` is
   enabled; in-radius targeting is untouched, and empty preferred results fall
   back to the existing unrestricted selection.
5. **Counter demand and hints (implemented).** `MasterAiBotModule` issues the
   `SetBotCounterDemand` order; `BotCounterDemandController` owns the synced demand conditions;
   `ProvidesPrerequisite` maps those conditions to `demand.*` tokens, and composition
   `Prerequisites` consume them without new consumer-side C#. Signals use hysteresis, with
   per-tier sustained activation delays and immediate removal below the off threshold. Only a
   small set of pilot compositions ships initially.
6. **Fogged observation + `ScoutBotModule`.** Deliberately last among the behaviour changes,
   because it makes the bots temporarily weaker and it invalidates any tuning done against
   omniscient signals. This is the §9.1 decision; phases 1-5 are honest about being pre-fog.
   *Phase 6a (fogged observation + `RegionMemory` + `UseFoggedObservation`) landed 2026-09-27 —
   the master builds profiles from `BotFogMemory` instead of `World.Actors`; Phase 6b
   (`ScoutBotModule`: staleness x interest region targets, yaml-listed scout types, scout-loss
   danger marks now feed it) and 6c (the pre-commit risk
   gate: `IBotRegionThreatProvider` + `AttackRiskMargin` on `SquadManagerBotModuleCA`, gating the
   idle-squad commit only) and 6d (squad-side fogged scans: `IBotFoggedEnemyProvider` gates every
   `World.Actors` target scan to observed enemies, with `FrozenActorLayer` remembered buildings as
   fallback targets — remembered mobile units are re-observed by scouts instead of being chased at
   stale positions) all landed 2026-09-27, as did 6e (risk routing: a coarse A*
   over `RegionMemory` costed by remembered hostile value —
   `IBotRouteThreatRouter` on the master answers `RouteAroundThreat` with 1-4
   locomotor-checked waypoints; guerrillas keep their harass routes), all merged
   2026-09-27 — and 6f's first piece (artillery attach: `SquadCAType.Artillery`,
   rules-derived via max weapon range, hangs `ArtilleryHangBackCells` behind a
   parent assault squad and bombards only what the parent sees — no separate
   staging or support-follow yet) — phase 6 is done except 6f staging/support —
   `docs/design/AI_FRANSBOT_RESEARCH.md` §4.*
7. **Assign-layer missions (phase 7a, implemented).** The master publishes ordered, reserved
   `Raid` and `Defend` intent from fogged region memory; squad formation remains the execution
   owner's decision. `Recon` and `Secure` are deferred.
8. **Offline learning.** Aggregate logs, fit bandit priors per (faction, personality, enemy
   strategy), commit them as reviewed data (§6.1 tier 4). Nothing neural until balance is frozen.

Phases 1 and 2 are pure additions with no gameplay effect and can proceed while the balance
pipeline is still moving. Phase 3 is the first one that needs playtesting attention, and phase 6 is
the one that needs a tuning pass on everything before it.

### 10.7 Failure modes this shape is chosen to avoid

| Failure | Why it is avoided here |
|---|---|
| Duplicate authority (two writers of production or squads) | 10.1; enforced by the master owning no queues and no squads |
| Second instance of a `TraitOrDefault` consumer | §1.3; the master and the compositions module are singletons by declaration |
| Personality thrash | §4.5 sustained reaction delay + clamped hold + momentum; and every switch costs an order (10.4) |
| Desync from learning | §6.1 tiers; learned data is read at load or never touches synced state |
| Learned weights overfitted to bot-vs-bot play | §8.4 distribution shift; priors stay small and are reviewed as balance data |
| Losing today's behaviour on a bad phase | degradation rule in 10.1; set `PersonalityReactionDelay` negative to disable switching |
| Tuning invalidated by the fog switch | fog is phase 6, and phases 1-5 are labelled pre-fog rather than pretending otherwise |
| Stale reference to a personality-gated module after a switch | §1.6; CN shipped this bug and documented it — every such reference is re-resolved whenever the cached instance is not enabled |
| A borrowed detection threshold that never fires | §11.4; detection thresholds are fitted from phase-2 logs, only the hysteresis constants are imported |

---

## 11. Reconciliation of the five-agent research round

Five differentiated briefs went out (`AI-RESEARCH-BRIEFS.md`: literature, OpenRA archaeology,
decision maths, engine feasibility, yaml/migration). This section records what came back, what
survived checking, and what the answers changed in the design. The discipline is the one that made
the balance pipeline usable: **repository artifact > measured experiment > primary external source >
independent review > AI synthesis**. Where two replies disagree, the disagreement is kept as an open
decision in §9 rather than averaged into a compromise nobody verified.

### 11.1 What actually came back

| Brief | Delivered | Usable content |
|---|---|---|
| OpenRA archaeology (Grok) | **Yes**, with file paths and constants | CN's adaptive constants, the per-profile stale-cache trap, the "no in-family main-target focuser" gap, an anti-pattern list. Independently re-verified in §1.6 — the constants and the trap check out; two of its architectural conclusions did not (§11.2) |
| Decision maths | **Partly** — a design review, not the requested derivation | bounded/saturating features, personality/target decoupling, decision traces, confidence propagation (§11.3). No thresholds, no team-coverage shape |
| Engine feasibility | **No** — a restatement of the existing spec as a "handoff document" | nothing new; it also reported #324 as merged, which it is not |
| Literature | **No** — the reply in this round answered the observer-graph question instead | none for the AI briefs |
| Yaml / migration | **No** — a "synthesis matrix" asserting findings the other agents never produced (§11.2) | none admissible |

So one of five briefs is answered on its own terms. The literature, engine-feasibility and
yaml-migration questions are still open, and **§3's signal thresholds, §6.3's bandit sample sizes and
§2.7's migration steps still have no external evidence behind them**. That does not block phases 1-2
of §10.6, which deliberately need no thresholds: they log raw signals so the thresholds can be fitted
from Cameo's own matches instead of borrowed.

### 11.2 Claims rejected, with what falsifies them

Recorded because they were stated as settled fact, and two of them would have changed the design.

| Claim | Status |
|---|---|
| "The order-based `BotPersonalityController` is validated by CN's implementation" | **False.** CN grants the condition directly from the bot module and issues no order at all (§1.6). The bridge is Cameo's own invention, forced by §1.1 |
| "CN-style token lifecycle owned by one module is an acceptable alternative to the order bridge" | **Rejected**, same evidence. It is a §1.1 violation; that it apparently works in CN is not evidence that it is sound |
| "Misclassifying a Rush as a Turtle was proved fatal" | **Unsourced.** Plausible and probably directionally right, but no paper, no measurement. Not admitted to §1; if it holds, phase-1 logs will show it |
| "`S_def > 0.35` classifies heavy defences; `S_eco > 1.25` classifies aggressive expansion" | **Invented constants.** No derivation, no data. Thresholds come from phase-2 logs (§10.6) or not at all |
| "All five agents' research has been synthesised into the architecture" | **False**, per §11.1 |
| "PR #323 merged" / "PR #324 merged" | **False when asserted in round one.** Current check (2026-09-07): #324 merged on September 6 as `15a08466e`; #323 remains open/conflicting and its graph is adapted on the follow-up branch. Historical rejection is not current PR status |
| "Cameo bots are omniscient, and so is every bot in the family" | **Half true.** Correct for Cameo's squad targeting (§0.2), false for CN's strategy input (§1.6) |

### 11.3 Recommendations adopted, and what they amend

1. **Bounded, saturating features in the target score.** §4.3's weighted sum takes raw quantities,
   which lets one large economy or army number dominate every other term. Amended: every feature is
   normalised into `[0,1]` before weighting, using `x / (x + k)` for unbounded quantities (economy
   share, army value, damage) so that twice the value is not twice the attractiveness, with `k` per
   feature in the tuning block. Weights stay linear on top of bounded features.
2. **A decision trace on every strategic decision.** Not just the chosen personality and target, but
   the full candidate vector, the incumbent's momentum bonus, the winning margin and the gates that
   fired. This is what makes phase 2 of §10.6 worth shipping on its own: an observe-only master whose
   log says only "would pick Rush" cannot be debugged, while one that says "Rush 0.73, Tech 0.67,
   incumbent +0.15, margin +0.06, held by minimum-hold" can. Folded into §6.2 as the
   `decision` object; §9.9 covers how verbose it is allowed to be.
3. **Confidence travels with every signal.** "Saw two tanks" must not enter the transition table as
   "the enemy army is armour-heavy". Each per-enemy signal in §3.2 carries a confidence derived from
   observation age and coverage, and a switch below a confidence floor is not taken — it is logged as
   suppressed. This is also the mechanism that makes fog (§10.6 phase 6) a change of input rather
   than a rewrite of the decision layer.
4. **Contact memory is split out ahead of fog and scouting.** §10.6 phase 6 bundled the shroud gate
   with `ScoutBotModule`. Amended to three increments in order: (a) contact memory — remember what
   was seen and what shot us, forget it only when a visible cell disproves it, the CN shape in §1.6;
   (b) the shroud gate on the squad managers' actor scan, which is the actual §0.2 fix; (c)
   `ScoutBotModule`, which only has a job once (b) leaves gaps worth filling. Each is independently
   playable and (a) costs the bots nothing.
5. **Bandits come after an offline comparison, not as the cheap first win.** §6.3 frames bandit
   priors as the cheap first learning step. Amended: the cheap first step is an offline comparison of
   fixed vs dynamic personality on logged matches. A bandit that has not been shown to beat the fixed
   policy on Cameo's own data is not cheap, it is unfalsifiable.
6. **The master stays out of the economy.** Restating §10.1 because two replies drifted here: the
   harvester, MCV-expansion and base-builder modules keep their decisions; the master publishes
   economy and expansion pressure as hints and nothing else.
7. **No caching of personality-gated module references** (§1.6, last bullet). Any master-module
   reference to a per-personality module is re-resolved whenever the cached instance is not enabled.

### 11.4 Rejected as design changes

* **A five-module unsynced pipeline** (`FogPerceptionBotModule` → `OpponentModelBotModule` →
  `EnemyTargetingBotModule` → `PersonalityEvaluator` → controller). Four extra trait lifecycles,
  four more load-order and enable/disable interactions, for what are pure functions over one
  snapshot. §10.1's last rule stands: the master's stages are methods, not traits. The only new
  traits remain `MasterAiBotModule`, `BotPersonalityController` and eventually `ScoutBotModule`.
* **A richer snapshot up front** (snapshot age, per-enemy tech/air/armour shares, aggression score,
  economy pressure as separate fields). Every field needs a consumer in the same phase that adds it;
  otherwise it is unverified surface that the log makes look authoritative. The fields arrive with
  their readers.
* **A full enemy-strategy classifier in phase 2.** Phase 2 logs raw per-enemy signals only. Strategy
  labels are added once the logs show the signals actually separate the strategies — this is the
  disagreement between the archaeology reply (own-state and fortification only, as CN does) and the
  §3 design (a per-enemy classifier), and §9.7 keeps it open rather than resolving it by assertion.
* **Any fixed numeric threshold before phase-2 logs exist**, including the CN constants as Cameo
  defaults. CN's numbers are the starting point for *hysteresis* (they were tuned against a switching
  bot in the same engine family), not for *detection*.

---

## 12. Combined arms: the arsenal tracker, composition, formation, siege and air doctrine

**Maintainer order, 2026-09-28** (paraphrased; every clause below is binding):

> Add a unit/defence tracker that keeps count of every unit and defence built and where they
> usually are, and that is **self-learning**. The AI must use **everything in its arsenal, in the
> right ratio**; build squads of the right composition; move them **in formation** (tanky units in
> front, long-range artillery behind, infantry supporting the tanks, aircraft giving air support)
> while **fast scouts** roam to spot enemies and decide where to attack next. Build **quick air
> strike teams** that hit high-value targets along the route of **least remembered resistance**.
> **Most importantly, always move so as to minimise losses: no suicide runs into defences.** Stop
> just outside their range, take them down with artillery, and move in only when every defence in
> the area is destroyed or the army is strong enough to destroy them. Helicopters and spaceships
> give air support to the ground army; fighters pick off enemies that are out of position and join
> big battles; bombers take out high-value targets, sometimes defences and power plants.

Two standing laws shape every item. **§10.1: one owner per decision.** Each item below changes the
*inputs* of a decision an existing module owns; none adds a second producer, a second squad
manager, or a second target picker. **§6.1: learning tiers.** In-match learning is live bot
reasoning (host-local, unsynced, acts only through orders; §1.1) and is free. Cross-match learning
is a **committed data file read at match start and frozen**, fitted offline from harness runs
(§6.2a Stage C) and reviewed like a balance change. "Self-learning" means both: the bot adapts
inside a match, and the batch harness plus the fitter improve its priors between matches.

### 12.1 What already exists (do not rebuild it)

| Order clause | Shipped today | Where |
|---|---|---|
| Enemy memory, "where they are" | `BotFogMemory` + `RegionMemory`: remembered hostile value per region, fog-honest | `OpenRA.Mods.Cameo/Traits/BotModules/BotFogMemory.cs` (§6a) |
| Enemy composition | `IBotEnemyCompositionProvider` from fog memory; Versus-scored counters | `AdaptiveCounterProduction.cs` (#605) |
| Who hurts us | `CombatAnalysisBotModule`: per-role threat weights from damage events, nemesis | `OpenRA.Mods.Cameo/Traits/BotModules/CombatAnalysisBotModule.cs` |
| Compositions | personality-tagged compositions, zero C# (§1.4) | `UnitCompositionsBotModule.cs` |
| Artillery behind the assault | artillery squads attach to an assault and hang back (`ArtilleryMinRangeCells`, `ArtilleryHangBackCells`) | `SquadManagerBotModuleCA.cs` (6f, #554) |
| Support follows | `SquadCAType.Support` trails its parent (`SupportFollowRangeCells`) | same (6f) |
| Rally before assault | `StageBeforeAssault` / `StageAssemblePercent` / `StageTimeoutTicks` | `GroundUnitsStageStateCA` (6f) |
| No suicide (partial) | pre-commit gate `AttackRiskMargin`: squad value must beat remembered regional threat | `SquadManagerBotModuleCA.cs` (6c) |
| Avoid defences en route (ground) | `UseRiskRouting` → `IBotRouteThreatRouter` waypoints around remembered threat | `GroundStatesCA.cs:508`, `RegionRouter.cs` (6e) |
| Flee when losing | fuzzy attack-or-flee | `AttackOrFleeFuzzyCA.cs` |
| Scouts | `ScoutBotModule`: cheap fast units cycle the stalest regions | `ScoutBotModule.cs` (6b) |
| Target priorities | rules-derived tags `artillery`, `harvester`, `production`, `superweapon`; per-squad `*PriorityTags`; air hunts artillery | `BotTargetTags.cs` (6g) |
| Donor stack | `FransRiskModel` (role-aware cell/route risk), `FransGroundDefendForcePreservationGuard`, `FransCombatIntel` (last-seen force estimate), `FransAirCommander` | `OpenRA.Mods.Fransbot/Traits/` — merge one module at a time, A/B each (§0a) |

### 12.2 The gaps, precisely

1. **No arsenal ledger.** Nothing counts *per unit type* what was built, lost, or destroyed, on
   either side; `PlayerStatistics` only holds per-player totals. So the bot cannot learn "our
   type X trades well against this enemy", and cannot know "the enemy usually has N defences of
   range R at choke C".
2. **No ratio law.** Production follows demand counters and compositions, but nothing guarantees
   every role the faction owns is used, in a target mix.
3. **No formation.** Artillery and support trail; tanks, infantry, AA and air do not hold roles
   relative to one another, and a squad moves at each unit's own speed.
4. **Siege is missing.** The 6c gate says *whether* to attack, not *how*: there is no stand-off
   outside defence range, no "artillery first", no "commit when the area's defences are gone".
5. **No air doctrine.** Air squads are one type with one tag set; risk routing is ground-only
   (`GroundStatesCA` is its only consumer), so strike aircraft fly straight through remembered AA.

### 12.3 The arsenal tracker (phase CA-1) — the foundation for everything else

* **Own ledger, per unit type:** built, alive, lost (count and cost), value destroyed (cost of
  victims killed), per enemy faction. **Mechanism:** a Cameo shadow of
  `UpdatesPlayerStatistics` (engine `PlayerStatistics.cs:215`; carried by 1,243 actor nodes, so no
  yaml changes). Its `INotifyKilled.Killed(self, e)` already receives the attacker actor; the
  shadow keeps the engine behaviour verbatim and additionally books `victim cost` to
  `(attacker owner, attacker type)` and `victim type` to the victim owner's ledger. The ledger
  lives on a player-level `BotArsenalTracker` trait and is read only by bot code (never `[Sync]`).
  Prove the shadow with a Cameo-only field (CLAUDE.md rule 7).
* **Enemy ledger, fog-honest:** distinct enemy actors *seen* (dedupe by actor id), per rules-derived
  role (§12.4), with the region they were seen in; for defences also the **max weapon range**
  (read from rules once the type is known). Extends `RegionMemory`; never enumerates unseen
  actors (`audit_fog_honesty.py` ratchet).
* **Where they usually are:** per region, a decaying average of remembered enemy defence value and
  army value — a heat map the siege planner (§12.6) and air router (§12.8) read.
* **In-match learning:** for each own role, the trade ratio `value destroyed / value lost` against
  this enemy, smoothed (prior-weighted), becomes a production *weight input* to the existing owner
  (`UnitBuilderBotModuleCA` via the master's demand inputs — §10.1).
* **Cross-match learning:** the match record gains the per-type ledger; `tools/ai/` aggregates it
  per (own faction, enemy faction, role and type) across harness runs and writes
  `mods/cameo/ai/learned/arsenal_priors.yaml` — **committed, reviewed, read at match start**.
  Every harness batch is therefore a training run; nothing is learned from a file only one client has.
* **Record-only first:** CA-1 ships as telemetry (match and situation logs) with no behaviour
  change; the in-match weight is a separate, A/B-tested step.

### 12.4 Roles are derived from rules, never listed

Extend `BotTargetTags` into `BotUnitRoles` (same load-time derivation, no actor ids, 25 factions):
`frontline` (high HP × armour, short range), `skirmisher`, `anti_infantry` / `anti_armour` (from the
weapon's Versus profile, as `AdaptiveCounterProduction` scores it), `artillery` (existing tag),
`anti_air`, `scout` (fast, cheap, long vision), `support` (heal/repair), `transport`, and for air:
`gunship` (VTOL/hovering, ground weapons — helicopters and hovering spaceships), `fighter`
(air-to-air weapons), `bomber` (bomb/drop attacks or fixed-wing ground-only), `air_transport`.
New **target** tags: `power` (positive `Power`), `defence` (building with an armament), `tech`.

### 12.4a Squad membership rulings (maintainer, 2026-09-29)

1. **Artillery squads are the `^ArtilleryTemplate` and `^ArtilleryTankTemplate` units ONLY.**
   Both templates now declare `BotRoles: Roles: artillery` (`rules/defaults.yaml`; 50 actors
   resolve to it). This replaces the range rule the code uses today: `SquadManagerBotModuleCA
   .ArtilleryMinRangeCells` (10) splits *any* ground unit with a 10-cell weapon into an artillery
   squad, and `BotTargetTags` tags *any* 12-cell unit as artillery.
2. **Fire-support units form their own squads, with tanks, and protect the artillery.**
   `^FireSupportTemplate` declares `BotRoles: Roles: firesupport` (37 actors: e.g. the Tesla tank,
   the GDI exosuit). They are support, never assault or raid units.
3. **Ships are their own squads, never mixed into ground or air squads.** A ship is a `naval`
   locomotor (the `navalunit` role's rule; `hover` and `amphibius` units such as hovercraft move on
   land and stay ground units). Today `FindNewUnits` checks `GuerrillaTypes` before
   `NavalUnitsTypes` and sends anything unlisted to a ground squad, and #627 measured **19 naval
   units missing from `NavalUnitsTypes`**: those ships are drafted into ground squads now.
   Applying `navalunit` is the list fix; a guard that a naval unit can never enter a ground or air
   squad is the code fix.

None of the three is a guerrilla (§2.8a excludes them). **Open, C# in `SquadManagerBotModuleCA`:**
read the `artillery` role instead of the range rule, add the fire-support squad and its escort
of tanks, and the naval guard. Owners per §12.10: CA-3/CA-4 (NOVA) for the squad composition,
Claude for the roles and the `navalunit` application; Claude-Local (who integrated the DF code
below) reviews the C#.

**Interlocks with the defence code on master `297c626f4`** (all in `SquadManagerBotModuleCA`):
* **Naval check first.** `PrepositionDefenceTick` and `ProtectOwn` draft the idle pool (`AttackBase`,
  not Building/Harvester/Aircraft), so a ship missing from `NavalUnitsTypes` can be drafted into a
  *land* protection squad today. Put the naval branch **before** the guerrilla branch in
  `FindNewUnits` (guerrilla assignment goes through `OpenGuerrillaSquad`, which respects
  `MaxGuerrillaSquads` / `MaxGuerrillaSquadsLate`), so a ship never reaches the idle pool.
* **`ReleaseDefenders`** routes released defenders by `GuerrillaTypes` / `HarasserTypes`, else to the
  idle pool. The guerrilla role follows automatically once it has `Targets`; a fire-support squad
  type needs its own branch there, or released fire-support units fall back into the idle pool.
* **`FastSquadsReactToThreats`** moves only Guerrilla/Harass squads. Fire-support squads guard the
  artillery and are not fast squads, so they stay out of it.
* **Artillery by role** does not touch the combat predictor (`BotUnitProfiles` is independent of
  `ArtilleryMinRangeCells`).

### 12.5 Composition and ratios (phase CA-3)

* A **target mix by role** per personality (starting values in yaml; e.g. frontline 35 %,
  anti-infantry 20 %, artillery 15 %, anti-air 10 %, air 15 %, scout 5 %), shifted by the enemy
  ledger (counters) and by the learned trade ratios, with a **floor** so every role the faction
  can build is used. Production fills the largest *deficit* against the mix — through the existing
  builder, as a demand input.
* Squads are formed to the same mix (a main assault without frontline or AA waits for them,
  bounded by `StageTimeoutTicks`), and compositions (§1.4) remain the personality flavour.

### 12.6 Siege and force preservation (phase CA-2) — the maintainer's "most importantly"

The rule, in order of precedence:

1. **Never path through remembered defence coverage** (6e, now fed by §12.3 ranges).
2. **Stop at stand-off**: the assault halts at `max remembered defence range + margin` from the
   nearest remembered defence of the target area (margin per difficulty on the DESIGN.md §19.1 equal-step line).
3. **Artillery first**: the attached artillery squad (6f) engages the defences from outside their
   range; frontline screens the artillery; air (§12.8) may strike defences.
4. **Commit** only when (a) no remembered defence in the area survives (re-verified by sight), or
   (b) `effective squad value ≥ R × (remembered defence value + remembered army in the region)`,
   where "effective" is Versus-weighted (the 6c gate, generalised) and `R` is a per-personality
   input the Aggression axis moves.
5. **Retreat** before the trade turns (fuzzy flee exists); a failed siege writes the loss into the
   region memory so the next plan avoids it.

Donor to evaluate first: `FransRiskModel` + `FransGroundDefendForcePreservationGuard`.

### 12.7 Formation movement (phase CA-4)

Engine orders have no formation, so the bot moves a squad **in steps along its route** (orders
only, §1.1): per step the frontline goes first; infantry holds just behind/alongside the tanks;
anti-air sits inside the group; artillery hangs back (6f exists); support trails (6f exists);
gunships hover over the frontline centroid. The group advances at the **pace of its slowest
frontline unit** (units that run ahead wait at the step point). Fast scouts are never in the
formation — they run ahead on their own (6b).

### 12.8 Air doctrine (phase CA-5)

* **Gunships (helicopters, hovering spaceships): close air support** — attach to the main
  assault like support squads and engage what the frontline engages.
* **Fighters: air superiority and pick-off** — hunt enemy aircraft and **isolated** enemy units
  (far from their army and from remembered defences), and join big battles near the own army.
* **Bombers: strike teams** — 2–4 aircraft, targets by tag priority (`superweapon`, `tech`,
  `production`, `power`, `harvester`, `artillery`; `defence` when it opens a siege), routed over
  the **air-threat layer** (remembered anti-air coverage) with minimum exposure, regroup and
  return. This is risk routing for air — today's router is ground-only.
  *Landed first slice (2026-09-28):* the 6e router now picks its remembered-threat
  read by the leader's domain — airborne leaders pay `AntiAirValue`, ground
  leaders pay `ArmyValue+DefenceValue` — and `AirAttackStateCA` transits a fresh
  target through those waypoints (`Fly` chain + queued `Attack`, skipped by the
  per-tick re-issue so transit is not cancelled). The doctrine split (gunship CAS,
  fighter pick-off, bomber strike-team targets) still waits on CA-1 roles.

### 12.9 Scouting decides where to attack next

`ScoutBotModule` (6b) keeps running; add **spawn-directed recon** (§9 item 12 (a): `mpspawn` cells are
public map data) and feed each scout report into the main-target / region choice: attack the
most valuable region whose remembered defence the available force beats (§12.6 rule 4), not the
nearest one.

### 12.10 Order of work, owners, and the gate for each phase

Every phase: record-only telemetry first where it applies, then the behaviour behind a yaml
switch, then **an A/B on A Nuclear Winter against the current master** (≥ 8 matches, both spawns,
`--bot-a hard --bot-b classic --swap-bots`, compared with a master run in the same session). A
phase lands only if it does not lose to master; the standing target stays "beat `classic`".

| Phase | What | Owner | Depends on |
|---|---|---|---|
| **CA-1** | arsenal tracker: `BotUnitRoles`/new target tags, own ledger (stats shadow), enemy ledger with defence ranges and region heat map, match/situation log fields; then the in-match production weight | **Claude** | — |
| **CA-1b** | offline fitter: aggregate harness ledgers → `learned/arsenal_priors.yaml`; harness runs more matchups | **Claude** (was Devin Cloud, out of tokens 2026-09-28) | CA-1 log fields |
| **CA-2** | siege and force preservation (§12.6) incl. evaluating the Fransbot donor guard | **DAWN** | CA-1 defence ranges |
| **CA-3** | role mix production + squad composition (§12.5), personality starting mixes | **NOVA** | CA-1 roles |
| **CA-4** | formation movement (§12.7) | **NOVA** | CA-3 |
| **CA-5** | air doctrine: gunship CAS, fighter pick-off, bomber strike teams, air-threat routing (§12.8) | **EMBER** | CA-1 roles |
| **CA-6** | scouting → target choice (§12.9), with §9 item 12 (a)+(b) | **DAWN** (owns recon) | CA-1 heat map |
| **SG** | scout-rebuild rationing and garrisons as defences (§12.12) | **Claude** | — |
| **EX** | expansion planner: field score, placement, refinery per field, MCV hand-off, enemy creep (§12.13) | **Claude** | CA-1 (fog memory) |
| **PL** | personality leads (§12.14): telemetry, then each lead's budget lean | Claude / NOVA / DAWN per row | CA-1 |

CA-2 and CA-5 may start on the parts that do not need CA-1 (reading the existing
`RegionMemory`), and switch to the tracker's ranges when it lands.

Research round 2 ([`AI_DEEP_RESEARCH.md`](AI_DEEP_RESEARCH.md) §9) adds phases that interleave
with these: **CP** combat predictor (the single engage/commit/retreat authority; CA-2's commit
rule and the 6c gate become its inputs), **ZG/IM** zone graph + influence layers (the region set
and "where they usually are"), **UT** utility strategist (absorbs CA-3's blended squad manager),
**MI** budgeted micro (with CA-5), **OM** opponent model and **LG** league harness (with CA-1b).

### 12.11 What a spectator saw, measured (maintainer review of the #633 A/B, 2026-09-29)

The maintainer watched `hard` vs `classic` (td_gdi, A Nuclear Winter) and reported suicidal
infantry at garrisoned buildings, infantry blobs without tanks, Humvees instead of tanks, no
artillery, idle armies and a timid base. The arsenal ledger (§12.3) of the 8 master-half matches
plus 3 branch-half matches confirms each, per match:

| `hard` built | per match | the composition asks for (`UnitsToBuild`, td_gdi vehicles) |
|---|--:|---|
| Humvee Mk2 + Humvee | 59.4 + 13.0 | Humvee Mk2 weight 5 of 100 |
| battle tank, Predator, Mammoth (all marks) | 1.2 + 1.5 + 0.6 | 45 + 20 + 15 of 100 |
| MLRS + Archer artillery | 0.9 + 1.1 | 10 of 100 |
| minigunner, grenadier, rocket soldier | 184.5, 81.1, 81.5 | — |

Causes, each traced to code:
1. **Humvees are scout replacements that jump the queue.** `td_gdi_humvee`/`humveemkii` are
   `ScoutBotModule.ScoutUnitTypes`. Below `MaxScouts` (2) the scout module calls
   `RequestUnitProduction` on every scan (`ScanInterval` 50), and `UnitBuilderBotModuleCA` builds a
   requested unit at the start of every production cycle, **before the cash check and outside the
   queue rotation**. Scouts die at the enemy base (`EnemySpawnBonus` 20000), and a new Humvee can be
   drafted by `FindNewUnits` into a guerrilla squad before the scout module claims it (both types are
   in `GuerrillaTypes`). The count stays below 2, and the vehicle queue builds Humvees instead of the
   composition's tanks. Fix: §12.12 item 1.
2. **Garrisoned buildings count as free.** The civilian houses (`^CivBuilding` →
   `^GarrisonableBuilding`) carry `AttackOpenTopped` and `ChangeOwnerOnGarrisoner`. Once enemy
   infantry enter, `BotFogMemory.Classify` marks them `Defence`, but their `Value` is the house's
   `Valued.Cost`, and the houses have **none** (`v02`, `v03`: no `Valued`; a GDI guard tower: 500).
   The occupants are cargo, invisible to the memory. The 6c risk gate therefore sees a garrison as
   worth 0 and lets infantry walk in one by one. Fix: §12.12 item 2.
3. **No artillery first, no tanks in front:** not built yet (CA-2 siege, CA-4 formation, §12.6–12.7).
4. **Idle blobs:** `IdleBaseUnitsMaximum: 50` holds the army at base until squads form. The CA-2/CA-3
   owners take it with the commit rule.
5. **A timid base:** `BaseCrawl` is a 50 % roll per building under 1,000 cost, aimed at a **random**
   resource cell within 50 cells, else at the enemy building nearest the defence centre, found by
   scanning `world.ActorsHavingTrait<Building>()`: omniscient. `ExpansionAppetiteHint` has no reader
   (§5). The engine `ResourceMapBotModule` also counts enemy units per field without fog. Fix: §12.13.

**Army-mix tracking (maintainer 2026-09-29):** *"add a unit tracking to see the army composition so
that you can easily notice … just Humvee and infantry spam instead of building a mix of all
available units."* `tools/ai/army_mix_report.py <support dirs>` reads the arsenal ledger of any
batch and prints, per bot type, the units built per match: shares by class (infantry, vehicle,
aircraft, ship; harvesters and MCVs apart as economy), the heavy and artillery share of the
vehicles, and the top types. Classes come from the resolved rules (`Aircraft`, the locomotor,
`Infantry` target type, `Armor.Type` Heavy/Superheavy, the `artillery` role). It **flags** one type
above 25 % of all units, heavy vehicles below 5 % of vehicles, and artillery below 5 % (each a
flag), and exits 1 when anything is flagged. On the master half above it flags all three for
`hard` (minigunner 34 %, heavy 4 %, artillery 1 %). Run it after every batch.

### 12.12 Scout rebuilds and garrisons (maintainer rulings 2026-09-29; owner Claude)

1. **A scout request is rationed.** `ScoutBotModule` requests a replacement at most once per
   `ScoutRebuildCooldownTicks` (`genericbot`: 3000; 0 = the old request-every-scan). So the
   requests can no longer take over the vehicle queue, whatever happens to the scouts.
2. **A garrisoned building is a defence, priced by its garrison.** An enemy-owned `Garrisonable`
   building (owned means occupied: `ChangeOwnerOnGarrisoner`) is valued at
   `Garrisonable.MaxWeight × GarrisonOccupantValue` when it has no `Valued` cost, and is a `Defence`
   like a tower. `genericbot`: 250, half the median cost (500) of the 323 buildable garrisoning
   infantry, because an observer cannot see how full the house is; 235 garrisonable buildings have
   no cost (capacity 1–40). So the 6c risk gate, the risk router (6e) and the siege planner (CA-2) all avoid
   it, stand off from it and shell it. Fog-honest: ownership is visible, and the value is a rules
   constant, never the real passenger list.

Both are behaviour changes, so each runs the §12.10 gate: a Nuclear Winter A/B (≥ 8 matches, both spawns).

### 12.13 The expansion planner (phase EX; maintainer rulings 2026-09-29; owner Claude)

> *"Always expand, always build more harvesters, always build more units, never be idle, always try
> to pressure and attack, always build towards the enemy and towards the resources. Always occupy all
> the resource fields … make a formula estimating where to place the next building so you can get
> the most money in the shortest amount of time … distance and value … and the less enemies the
> safer."*

**Rulings.** (a) Every resource field within building reach gets **one refinery**, protected by
towers. (b) The base builder never idles: **every building it places moves the base closer to the
next resource field**, and power plants make the line when nothing else is due. (c) **Fog:** where
the fields are and how rich they are at match start is public map data (like the spawn points).
Depletion, enemy refineries and enemy defences count only once seen, through `BotFogMemory` /
`RegionMemory`, never through `ResourceMapBotModule`'s omniscient enemy counts. (d) **Creeping toward
the enemy base** scales with difficulty (DESIGN §19.1) **and** aggressiveness (the personality's
Aggression axis).

**The score of a resource field `f`** (every constant is a yaml knob, learnable by route 2, §6.4):

    V_f    = value of f: its initial resource cells × value per cell, minus the depletion seen
    hops_f = ceil( max(0, d_f − R_reach) / R_link )   d_f: path distance from our nearest building
    C_f    = refinery cost + hops_f × link cost + towers_f × tower cost
    T_f    = C_f / income + hops_f × link build time + refinery build time   (seconds until it pays)
    S_f    = 1 / (1 + threat_f / max(own force near f, 1))   threat_f: remembered enemy value near f
    score_f = V_f × S_f / (T_f + τ₀)

Higher value ranks higher, a shorter distance ranks higher (fewer hops, so a smaller `T_f`), and
fewer remembered enemies rank safer. `τ₀` keeps a field next to the yard from dividing by nearly
zero. A field with a seen enemy refinery is not a candidate. It becomes a raid target for Rush and
Guerrilla (§12.14).

**Placement.** The planner keeps a target field `f*` (the best score, re-chosen every
`ExpansionReplanTicks`). The next non-refinery building goes on the valid cell that **minimises
the path distance to `f*`**, ties broken toward home, so the base walks toward the field one
building at a time. Once `f*` is in reach, its refinery goes on the free cell nearest the field's
resource centre on the home side, ahead of every other building; then `towers_f` towers, where
`towers_f` grows with `threat_f`, placed on the side of the highest remembered threat. When
`hops_f` exceeds `MaxLinkHops`, the MCV module is asked to found a yard at `f*` instead
(`McvExpansionManagerBotModule` owns the deployment, §10.1).

**The MCV goes to the best field, and never alone (maintainer 2026-09-29).** An MCV's site is
the field with the best `score_f`, where `d_f` is the MCV's own path distance and the link terms
become its travel time. Each factor comes from its own analytics: value from the map's resource
layer (public, ruling (c)), distance from the pathfinder, and safety from `RegionMemory`'s
remembered threat along the route **and** at the site (the 6e router's cost). Before the MCV
moves, the planner asks the squad manager for an **escort**, sized so its Versus-weighted strength
beats the route's remembered threat (the CP predictor's ratio). The escort travels with the MCV
and stays to guard the new yard until its first tower stands (`OutpostGuardTicks`). The request
goes through the squad manager's existing protection path (`ProtectOwn` / `PrepositionDefenceTick`,
§12.4a), never a second squad manager (§10.1). The hook in `SquadManagerBotModuleCA` is agreed
with its owner (NOVA, CA-3/CA-4).

**Creeping toward the enemy.** The enemy's probable base (a spawn, or seen buildings) is one more
candidate, with `score = CreepWeight × difficulty step × Aggression`. It wins only when no field
scores higher. Its towers go on the enemy-facing edge.

**Order of work** (each step: telemetry first, then behaviour behind a yaml switch, then the A/B):
EX-0 compute and log `score_f`, `f*` and the placement choice (no behaviour change); EX-1 replace
`BaseCrawl`'s random/omniscient target with `f*` and the distance-minimising placement; EX-2
refinery-per-field priority plus towers; EX-3 the MCV hand-off; EX-4 the enemy creep, scaled per
(d). The CA base builder is CA-sync-tracked: the planner lives in a Cameo module that the builder
consults (a pull interface, as §5 prescribes for the master's hints), so the CA file changes by a
small hook only.

### 12.14 Personality leads: one top priority each, measured against the enemy (maintainer ruling 2026-09-29)

DESIGN §19.1c gives every personality **one top priority, measured as a lead over the enemy**:
`lead = own metric / remembered enemy metric`. It is fog-honest: the enemy side comes only from
what was seen (§3). While the lead is below its target, the personality's budget leans toward the
lead's driver. The other priorities keep their floors, so no personality abandons the rest.

| personality | top priority | own metric | enemy metric (seen only) | driver it leans on | owner |
|---|---|---|---|---|---|
| Expansion | out-earn | income per minute | seen refineries × seen harvesters (the economy proxy, §3.2) | EX planner (§12.13), harvesters | Claude |
| Steamroller | out-produce | army value produced per minute | seen production buildings, seen army growth | unit builder budget, factories | NOVA (CA-3) |
| Guerrilla | out-scout, map control | regions seen within `StaleAfterTicks`, share held | regions with remembered enemy presence | scouts, guerrilla squads | DAWN (CA-6 recon) |
| Rush | pressure: attack early and often, kill the economy | attacks launched, enemy economy value destroyed | none: it is absolute (first-attack tick, attack frequency) | squad manager attack thresholds | NOVA / DAWN |
| Turtle | more defences | own defence value | remembered enemy defence value | base builder defence share | Claude (base builder) |
| Tech | tech up faster | research and upgrades done, tier reached | seen tech buildings and upgrades | research queue priority | Claude (base builder) |

Telemetry first, as for EX-0: every lead goes into the situation log, so the targets are tuned
from data rather than invented (§10.6).
