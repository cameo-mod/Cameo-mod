# Mission cards: one id from the strategist to the actors and back

**Status:** design, 2026-09-30. Plan rows **MC1–MC3** (AI_MASTER_PLAN §3), built on **LC8** (failure write-back)
and **LC1** (unit leases). **Source:** fransotto (Fransbot's author), in conversation with the maintainer on
2026-09-30, after his review ([`../FRANSBOT_AI_REVIEW_2026-09-30.md`](../FRANSBOT_AI_REVIEW_2026-09-30.md)): Fransbot is
moving from a bid system to *mission cards* (`MatchId → MissionId → AttemptId`), so that the General gets feedback
on its own decisions. He proposed that Cameo and Fransbot share the **neutral infrastructure** (id format, states,
serialisation, archive, replay correlation) while keeping different decision systems. This document is Cameo's side
of that proposal, plus the offer back.

---

## 0. The answer in five lines

1. Cameo already publishes missions (`BotMission`, AI_ARCHITECTURE §10.5a), but they carry **no id** and the
   feedback runs **one way**: `IBotMissionProvider.MissionTaken` tells the strategist a mission was *taken*, never
   what became of it. That is the review's sixth boundary, LC8.
2. **MC1** gives every mission a `MissionId` and every try an `AttemptId`, a closed state machine, and an outcome
   report from the executor back to the owner. The unit leases (LC1) carry the `MissionId`, so an actor can be traced
   to the reason it is moving.
3. The same transitions are written as **plain sentences** in `debug.log` ("the General and his commanders", as
   fransotto describes Fransbot's log) and as records in the situation log. **MC2** turns them into a per-mission story.
4. Learning across matches is **OM**, already planned, under AI_ARCHITECTURE §6.1: read at match start, frozen for
   the match, steers only host-local reasoning. fransotto's own rule, *"current information > historical experience"*,
   is the same boundary.
5. **MC3** publishes the card format as a versioned JSON Schema both projects can write, so tools (story, replay
   correlation, archive) are shared while the brains stay separate.

---

## 1. What already exists (measured on master `7b89bb899`, 2026-09-30)

| piece | where | what it lacks for mission cards |
|---|---|---|
| `BotMission` (`Recon`, `Raid`, `Secure`, `Defend`; location, target player, region, required value, priority) | `OpenRA.Mods.CA/Traits/BotModules/BotMission.cs` | no id, no attempt, no state |
| `IBotMissionProvider.Missions` + `MissionTaken(mission)` | same file; `MasterAiBotModule` publishes, `SquadManagerBotModuleCA` consumes (§10.5a) | feedback stops at "taken" |
| `BotMissionAssignment` (type, region, frozen) | same file | not linked to the squad or the units |
| LC1 leases, purpose `Mission` reserved | `IBotUnitLeases.cs` (#668) | no mission id on the lease |
| failure memories that already work | CA-2c siege failure memory; EX-2 parks a refinery claim it missed; LC3 parks an MCV site after 3 hand-outs (#667) | each is private to one module, none reports to the strategist |
| record-only situation log, schema 2 with the published mission intent | DESIGN §21.1, `AiSituationLogWriter` | no transitions, no outcome |
| decision trace (candidate vector, margin, gates that fired) | AI_ARCHITECTURE §11.3.2 | per strategic decision, not per mission |
| match records keyed by `game_uid` | `cameo-ai-matches.jsonl` (schema 2) | — (already the MatchId) |

So the vocabulary is in place; the missing parts are the **ids**, the **states** and the **return path**.

---

## 2. MC1 — the contract

### 2.1 Ids
* **MatchId** = the existing `game_uid` (already in every match record and in the replay metadata).
* **MissionId** = a per-player integer, allocated by the mission's **owner** (the strategist for Raid/Defend/Secure,
  `ScoutBotModule` for Recon, the MCV owner for expansion sites, the engineer owner for captures). A mission is
  **one strategic reason** — "secure resource field 7", "raid the refinery at region 12" — so re-publishing the
  same reason keeps its id. Identity key: `(type, region or field id, target player)`.
* **AttemptId** = 1, 2, 3 … within a mission: one per commitment of units. An attempt can fail without deleting
  the mission.

### 2.2 States (closed set)
```
Proposed ──► Denied ──► Dormant ◄─────────────┐
    │                     │ (situation changed:        │
    ▼                     ▼  new recon, new value)     │
Committed ──► Progressing ──► Succeeded               │
                 │   ▲                                 │
                 ▼   │                                 │
              Stalled ─► Recovering                    │
                 │                                     │
                 ▼                                     │
              Failed ─────────────────────────────────┘ (or Abandoned: the reason no longer exists)
```
`Denied` = no executor would take it (no units, unreachable, outmatched). `Dormant` keeps the card and its history;
the owner reconsiders it only when an input it depends on changes (the review's "objective disappears" boundary).
Every transition carries a **reason** from a closed enum: `no_units`, `unreachable`, `undeployable`, `reserved`,
`outmatched`, `target_gone`, `timeout`, `stuck`, `superseded`, `lost_units`, `done`.

### 2.3 The return path
```csharp
// Owner side: publishes cards and receives outcomes. Executors never change a card's strategy, only report.
public interface IBotMissionOutcomeSink
{
	void Report(int missionId, int attemptId, BotMissionState state, BotMissionReason reason, int tick);
}
```
* The executor that took an attempt (squad manager, MCV owner, engineer owner, scout) **must** report exactly one
  terminal state per attempt (`Succeeded`, `Failed`, `Abandoned`) — the LC5 watchdog (not built yet) will assert
  it, the way it will assert one owner per actor.
* The **owner decides** what a failure means (retry, dormant, abandon). This is the SiegeEvaluator pattern the
  review praised, one level up: the executor advises by reporting, the owner decides.
* LC1 leases gain an optional `MissionId`; an actor's lease therefore answers "which mission is this unit on?".
* The first consumers, in order: the MCV site (LC3's engine half: `unreachable` / `undeployable` / `reserved`
  become reasons), the capture target (ENG), the refinery claim (EX-2), the squad raid target (CA-2c's memory).

### 2.4 The General's log (fransotto: *"Attack the harvester! — Air: I can't — Sea: I can — General: Sea, do it!"*)
One `debug.log` line per transition, readable without tooling, in the order the decision flows:
```
AI (1) General   M42 RAID harvester field 7 (region 12) PROPOSED  value 3200  [Rush 0.71, margin +0.08]
AI (1) Squads    M42/A1 DENIED     no_units (idle value 1400 < 3200)
AI (1) General   M42 DORMANT       until idle value >= 3200 or new recon of region 12
AI (1) Squads    M42/A2 COMMITTED  squad 5, 11 units, value 3650
AI (1) Squads    M42/A2 STALLED    stuck at chokepoint (region 9) 250 ticks
AI (1) Squads    M42/A2 FAILED     lost_units 8 of 11
AI (1) General   M42 DORMANT       outmatched at region 9; siege memory +1
```
When "then nothing happens", the last line names the layer that went quiet — which is fransotto's point about
finding the bug in the right commander file.

The same transitions go into the situation log as records (**schema 3**, record-only, DESIGN §21.1 unchanged).

---

## 3. MC2 — the story tool

`tools/ai/mission_story.py <support dir> [--match <game_uid>] [--mission 42]` prints each mission as a short story
(proposal → attempts → outcome, with ticks as game time), the per-type success rate, and the missions that ended
**without** a terminal state (the ownership bugs). It correlates with the replay by `game_uid` and tick; the
replay's own metadata comes from `OpenRA.Utility.exe cameo --replay-metadata <file.orarep>` (the route fransotto
uses). Output is plain text first: the maintainer and fransotto both read these logs by eye.

---

## 4. Across matches: this is OM, under the §6.1 boundary

fransotto's proposed flow — *download/select experience before the match → freeze a snapshot → play locally →
save results after* — is exactly AI_ARCHITECTURE §6.1's fourth tier ("learned parameters: read at map load, then
frozen for the match, a data file treated as configuration"), and AI_DEEP_RESEARCH §6.1's **OM** (per-enemy-faction
profiles, host-local, committed priors plus a local layer that grows with every game). So:

* **No new ruling is needed** as long as the archive is read once at match start, is frozen, and only steers the
  bot's unsynced reasoning on the host. Anything that would change a synced condition must be mod configuration.
* The match log stays **record-only** (DESIGN §21.1): OM reads its own profile file, which a tool distils from the
  logs between matches — never the log itself during a match.
* fransotto's rule is adopted verbatim as OM's weighting rule: **current information outranks history**; a prior
  shifts a score, it never overrides an observation.
* Local-first, per installation, works offline; any upload is opt-in and anonymised (no player names; keyed by
  faction, as ruled for OM).

Mission cards give OM a better input than raw snapshots: "Raid on the refinery field from the north spawn, this
faction pair, this army value → failed, outmatched at the chokepoint" is a reusable prior; a unit-mix snapshot alone
is not.

---

## 5. MC3 — what to share with Fransbot (the offer)

**Share the format, not the brain.** A versioned JSON Schema, `docs/design/mission_card.schema.json`, that both
projects write and both projects' tools read:

```json
{
  "schema": "mission-card/1",
  "match_id": "<game_uid>", "map_uid": "…", "player": "Multi0", "faction": "td_gdi", "bot": "hard",
  "mission_id": 42, "type": "raid", "owner": "General",
  "target": { "kind": "economy", "cell": "61,33", "region": 12, "actor_type": "harvester" },
  "context": { "tick": 18200, "own_value": 3650, "enemy_estimate": 2900, "confidence": 0.6, "eta_ticks": 900 },
  "attempts": [
    { "attempt_id": 1, "executor": "Squads", "states": [ ["proposed", 18200, null], ["denied", 18210, "no_units"] ] },
    { "attempt_id": 2, "executor": "Squads", "units": 11,
      "states": [ ["committed", 20400, null], ["stalled", 21100, "stuck"], ["failed", 22900, "lost_units"] ],
      "losses": 8, "kills": 3 }
  ],
  "outcome": { "state": "dormant", "reason": "outmatched" }
}
```

* The **states and reasons** above are the shared vocabulary; each project may add private reasons under an
  `x_` prefix (`x_fransbot_bid_lost`), which the other's tools pass through.
* **Archive:** one JSONL file per match (`<support dir>/Logs/mission-cards-<game_uid>.jsonl`), local, append-only.
* **Versioning:** `schema` is `mission-card/<major>`; a reader rejects an unknown major and ignores unknown fields.
* **Replay correlation:** `match_id` + `tick` (+ the replay metadata) — no replay parser is needed for this.
* Cameo will put the schema and `mission_story.py` in this repository under GPLv3; Fransbot can take them with a
  "Ported from" line, as Cameo does for Fransbot's modules. If fransotto prefers a neutral home, a tiny shared repo
  holding only the schema and the tools is fine too.

---

## 6. Order of work

| id | work | needs | done when |
|---|---|---|---|
| **MC1** | ids + states + `IBotMissionOutcomeSink` + lease `MissionId` + the General's log lines; first consumers MCV site, capture, refinery claim, squad raid | LC1, LC8 | a smoke match's `debug.log` tells every mission's story end to end, and LC5 finds no attempt without a terminal state |
| **MC2** | `mission_story.py` + situation-log schema 3 | MC1 | one Nuclear Winter batch summarised per mission type (success rate, median attempts, top failure reasons) |
| **MC3** | `mission_card.schema.json`, the JSONL archive, the offer to fransotto | MC1 | the schema validates Cameo's archive; fransotto has the link |
| (OM) | reads a distilled per-faction profile at match start (§6.1) — mission outcomes become its input | MC2, CA-1b | as in AI_DEEP_RESEARCH §6.1 |

**Not in scope:** replacing the decision systems (Cameo keeps its master + squad owner, Fransbot its General and
commanders), a replay program with rewind (fransotto's codex wanted to build one; he held it back for his release,
and so do we), and any learning that changes synced state.
