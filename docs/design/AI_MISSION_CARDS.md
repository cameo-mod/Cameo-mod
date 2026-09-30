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
   fransotto describes Fransbot's log) and as records in `cameo-ai-missions.jsonl`, through ONE writer. **MC2** turns them into a per-mission story.
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

## 2. MC1 — the contract (ruled 2026-09-30, after three agents built three dialects)

Within hours of this document, NOVA (#681, squad missions), DAWN (#679, the Fransbot broker) and EMBER (a schema
note) each built or proposed a mission-card format — with four different id schemes and three state sets. DESIGN
§22 (one implementation per mechanic) applies to formats too, so the coordinator ruled ONE contract; everything
below is what `OpenRA.Mods.CA/Traits/BotModules/BotMissionLog.cs` implements, and every emitter goes through it.

### 2.1 Ids
* **MatchId** = the existing `game_uid` (every match record and the replay metadata already carry it).
* **MissionId** = a **string key, deterministic from the strategic reason**, so a mission re-published every
  situation rebuild keeps its identity without any registry (NOVA's insight — `BotMission` objects are recreated
  each pass): `raid:<target player>:r<region>`, `capture:<actor type>:<actor id>` (no owner: a building that changes hands is still the same mission), `frans:<MissionAuctionId>`
  for the Fransbot broker. ⛔ Never a hash, and never a `ClientIndex`: every non-human player carries the HOST's
  client index (`Player.cs:189`), and the first proposal's XOR hash collided 4,185 times over realistic ranges.
* **Attempt** = 1, 2, 3 … per MissionId, counted by whoever commits units; `attempt_id` = `<mission_id>|A<n>`.

### 2.2 Attempt states — fransotto's six, plus two
`DENIED  COMMITTED  PROGRESSING  STALLED  RECOVER  SUCCESS` (his vocabulary, verbatim) **+ `FAILED`** (the units were
lost — his list had no terminal loss; NOVA) **+ `RELEASED`** (the executor handed the attempt back without a verdict —
DAWN's broker release, a dissolved squad, an engineer that went idle). Terminal: `DENIED`, `SUCCESS`, `FAILED`,
`RELEASED`. **Dormant is not a state**: a mission with no live attempt is dormant, and the next commit simply opens
attempt n+1 (EMBER's reading of fransotto's model).

**Reasons** (closed set, lowercase): `no_units unreachable undeployable reserved outmatched target_gone timeout stuck
superseded lost_units done dropped`. A project-private reason carries an `x_` prefix (`x_frans_board_closed`); any
other spelling is rewritten to `x_invalid_…` by the writer so a dialect is caught, never archived.

### 2.3 One writer, two outputs
`BotMissionLog.Write(BotMissionRecord)` (CA — both the genericbot stack and the vendored Fransbot can call it):
1. one plain-text `debug.log` line — `Log.Write`, never `AIUtils.BotDebug`, which only reaches chat and only with
   the bot-debug setting on;
2. the record to every `IBotMissionRecordSink` on the World actor — today Cameo's `AiMissionLogWriter`, which appends
   `cameo-ai-missions.jsonl` (§5) through the existing `AiLogFileAppender`, host-only, record-only (DESIGN §21.1).

The return path to the strategist stays `IBotMissionOutcomeSink` (NOVA's #681): executors report exactly one terminal
state per attempt, the **owner decides** what it means (retry, dormant, give up) — the SiegeEvaluator pattern one
level up. That decision half is **LC8** (the dormant shelf in the master AI). LC1 leases will carry the MissionId so
an actor answers "which mission is this unit on?", and the LC5 watchdog will flag attempts that never end.

### 2.4 The General's log (fransotto: *"Attack the harvester! — Air: I can't — Sea: I can — General: Sea, do it!"*)
The exact format, with illustrative ids and ticks (the engineer owner, `EngineerBotModule`, is the first consumer):
```
AI Multi0: MISSION capture:oilb:526 ATTEMPT 1 COMMITTED by=Engineers tick=2561
AI Multi0: MISSION capture:oilb:526 ATTEMPT 1 SUCCESS reason=done by=Engineers tick=2790
AI Multi0: MISSION capture:td_gdi_constructionyard:412 ATTEMPT 1 FAILED reason=lost_units by=Engineers tick=6120
AI Multi0: MISSION capture:td_gdi_constructionyard:412 ATTEMPT 2 RELEASED reason=stuck by=Engineers tick=7400
```
**First live match** (hard vs classic, td_gdi, Nuclear Winter, 2026-09-30): 18 capture attempts, **every one reached a
terminal state** (6 SUCCESS, 11 FAILED lost_units, 1 RELEASED target_gone). One enemy derrick took **five engineers
in a row**, two of them at once — so the engineer owner now sends one engineer per target (`MaxEngineersPerTarget: 1`)
and rests a mission after two consecutive losses (`CaptureFailuresBeforeDormant: 2`, `CaptureDormantTicks: 3000`,
logged `MISSION <id> DORMANT until tick N`) — fransotto's dormant shelf, owned by the module that both chooses and
executes captures. Both ship OFF (0) until their A/B (AI_MASTER_PLAN §1.2 step 6); the candidate sets 1 and 2. For
missions the master AI chooses and squads execute, the shelf is LC8.

`grep "MISSION <id>"` tells one mission's whole story; when "then nothing happens", the last line names the layer
that went quiet — fransotto's point about finding the bug in the right commander file. Emitters, in order:
engineer owner (built, MC1), squad raids/defends (NOVA #681, switching to the writer), Fransbot broker (DAWN #679),
MCV sites (LC3's engine half), refinery claims (EX-2).

---

## 3. MC2 — the story tool

`tools/ai/mission_story.py <support dir> [--match <game_uid>] [--mission <id>]` reads `Logs/cameo-ai-missions.jsonl`
(no new situation-log schema) and prints each mission as a short story
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

**Share the format, not the brain.** One JSON object per attempt transition, append-only, one file per support dir
(`Logs/cameo-ai-missions.jsonl`) — EMBER's per-transition shape: crash-safe to append, and a "card" is simply the
fold of one `mission_id`'s lines, which the story tool (MC2) does. The shape `AiMissionLogWriter` writes (values illustrative):

```json
{"schema":"mission-card/1","recorded_utc":"2026-09-30T12:00:00.0000000Z","game_uid":"…","map_uid":"…",
 "map_title":"A Nuclear Winter","player":"Multi0","faction":"td_gdi","bot":"hard",
 "mission_id":"capture:oilb:526","attempt_id":"capture:oilb:526|A1","attempt":1,
 "state":"SUCCESS","terminal":true,"reason":"done","by":"Engineers","tick":2790,
 "type":"capture","target_cell":"61,33","units":1}
```

* Required: `schema game_uid player mission_id attempt state terminal by tick`. Optional context (`type region
  target_cell units value reason faction bot map_*`) is omitted when unknown, never written as null.
* The **states and reasons** of §2.2 are the shared vocabulary; private reasons use the `x_` prefix, which the other
  project's tools pass through.
* **Versioning:** `schema` is `mission-card/<major>`; a reader rejects an unknown major and ignores unknown fields.
* **Replay correlation:** `match_id` + `tick` (+ the replay metadata) — no replay parser is needed for this.
* Cameo will put the schema and `mission_story.py` in this repository under GPLv3; Fransbot can take them with a
  "Ported from" line, as Cameo does for Fransbot's modules. If fransotto prefers a neutral home, a tiny shared repo
  holding only the schema and the tools is fine too.

---

## 6. Order of work

| id | work | needs | done when |
|---|---|---|---|
| **MC1** | ids + states + one writer (`BotMissionLog`, `AiMissionLogWriter`) + the General's log lines; first consumer the engineer owner (built 2026-09-30), then squad raids (#681), the Fransbot broker (#679), MCV sites, refinery claims; lease `MissionId` | LC1 | a smoke match's `debug.log` tells every mission's story end to end, and LC5 finds no attempt without a terminal state |
| **MC2** | `mission_story.py` over `cameo-ai-missions.jsonl` (owner: EMBER, the A/B-tooling lane) | MC1 | one Nuclear Winter batch summarised per mission type (success rate, median attempts, top failure reasons) |
| **MC3** | `mission_card.schema.json`, the JSONL archive, the offer to fransotto | MC1 | the schema validates Cameo's archive; fransotto has the link |
| (OM) | reads a distilled per-faction profile at match start (§6.1) — mission outcomes become its input | MC2, CA-1b | as in AI_DEEP_RESEARCH §6.1 |

**Not in scope:** replacing the decision systems (Cameo keeps its master + squad owner, Fransbot its General and
commanders), a replay program with rewind (fransotto's codex wanted to build one; he held it back for his release,
and so do we), and any learning that changes synced state.
