# SCG / Dispatch Proposal — Neutral RTS-AI Coalition Communication

**Status:** design proposal for discussion with Cameo and other OpenRA AI developers  
**SCG:** **Supreme Coalition General**  
**Cameo reference master:** `9a2f6667c6a7440804580a6f9e8da02c5646a106`  
**Core principle:** **SCG is not another AI brain. It is a neutral Dispatch relay.**

## 0. Why this exists

Different RTS AIs can cooperate in the same OpenRA team without sharing an architecture.

Fransbot may internally use:

```text
General -> MissionCards -> Commanders -> units
```

Cameo may internally use:

```text
MasterAi -> Situation / Utility / Director / TC-3 -> SquadManager -> units
```

A stock AI may have none of those abstractions.

Trying to standardize the internal intelligence would either make the protocol enormous or force every AI to adopt someone else's architecture.

SCG takes the opposite approach:

> Standardize only the smallest amount of information that must cross the boundary between allied AIs.

What happens inside each AI after it receives that information is private and entirely controlled by that AI's developers.

---

## 1. Locked principles

### 1.1 Shared vision communicates reality. Dispatch communicates intent.

OpenRA allies normally share vision. They can already observe the common battlefield state.

Therefore SCG should **not** create another world model or duplicate facts that allies can already see.

Do not send:

```text
I have 14 tanks at this visible position.
Enemy building X is visible here.
```

unless the information is needed to express an internal intention or request.

Do send:

```text
I intend to expand here.
I am committing to attack this player.
I need air support in this area.
I am abandoning this commitment.
```

The useful information is what observation cannot reveal: **intent, commitment and need**.

### 1.2 AI -> SCG -> AI

Different AI implementations do not need a direct compatibility contract.

```text
Fransbot -> SCG -> Cameo
Cameo    -> SCG -> Fransbot
Stock AI -> SCG -> any compatible participant
```

SCG is the one common boundary.

### 1.3 SCG relays. It does not decide.

SCG does **not**:

- choose the coalition's main target;
- select which AI must answer a support request;
- assign sectors;
- rank allied plans;
- cancel an AI's strategy;
- issue unit orders;
- create a shared tactical brain.

SCG may know enough metadata to route a Dispatch, but the strategic decision remains with the participating AIs.

If Fransbot asks for air support, SCG forwards that request to compatible participants. Cameo decides whether and how to react. If several AIs answer, Fransbot decides what to do with those answers.

### 1.4 The receiver owns interpretation

The same incoming Dispatch may produce very different behavior.

Example:

```text
REQUEST: AIR_SUPPORT
AREA: <map position/radius>
```

Fransbot may turn that into MissionCard candidates and Commander bids.

Cameo may feed it into its own advisor/squad system.

A stock-AI adapter may map it to a simple available behavior, or return `UNSUPPORTED`.

All are valid.

### 1.5 Internal AI architecture is private

SCG must never require Fransbot MissionCards, Cameo Director phases, utility axes, squad types, build queues or doctrine internals.

Those concepts may be used by an adapter, but they are not part of the common protocol.

### 1.6 Same-family communication may be richer

Two Fransbots can use a richer Fransbot-specific channel.

Two Cameo instances can continue using Cameo's TeamBlackboard / TC-3 coalition logic.

That is natural and desirable.

The rule is only:

> If a private decision creates a coalition-relevant intention, commitment or request, expose the relevant result through the simple SCG Dispatch boundary so other AI families are not blind to it.

---

## 2. Relationship to Cameo TC-3 after the 2026-10-02 merge

Cameo now has a real internal coalition implementation:

```text
TeamBroadcast
    -> CoalitionFold
    -> CoalitionDirective
       (MainTarget / Phase / RescueAssignments / SectorAnchors)
    -> Cameo consumers
```

This is **not the same thing as SCG**.

TC-3 is allowed to be Cameo-specific and intelligent. It can understand Director tension, army centroid, Cameo target votes and Cameo expansion sectors because all participants are running compatible Cameo logic.

SCG should remain deliberately less intelligent.

Recommended relationship:

```text
        Cameo TC-3 / internal logic
                  |
            Cameo SCG Adapter
                  |
             Dispatch v0.x
                  |
                 SCG
                  |
             Dispatch v0.x
                  |
        Fransbot SCG Adapter
                  |
        Fransbot internal logic
```

Do **not** route SCG into `CoalitionFold` as a second coalition brain.

Inbound SCG information should enter Cameo through a small advisory/provider seam. Existing Cameo decision owners remain free to use or ignore it.

---

## 3. Minimum participant contract

A participant first registers a small compatibility profile.

Example:

```json
{
  "schema": "scg-dispatch/0.1",
  "kind": "HELLO",
  "participant_id": "Multi2",
  "ai_family": "cameo",
  "can_send": ["INTENT", "COMMITMENT", "REQUEST", "RESPONSE", "RELEASE"],
  "can_receive": ["INTENT", "COMMITMENT", "REQUEST", "RESPONSE", "RELEASE"],
  "capabilities": ["GROUND", "AIR", "EXPANSION", "CAPTURE"]
}
```

`participant_id` must be a stable match-local player identity. Do **not** define the protocol identity as OpenRA `ClientIndex`; map-side bots can share the host client index.

The declared lists are compatibility metadata, not promises that the AI will accept every request.

---

## 4. Core Dispatch kinds

Keep v0.x intentionally small.

### INTENT

"I am considering / intending to do this."

Examples:

```text
INTENT EXPAND area X
INTENT ATTACK player Y
INTENT SECURE area Z
```

An INTENT is not ownership. The AI may still change its mind.

### COMMITMENT

"I have now committed internal resources/responsibility to this."

Example:

```text
COMMITMENT SECURE area X
```

The details of those resources stay private.

### REQUEST

"I need another participant to consider helping with this."

Example:

```text
REQUEST AIR_SUPPORT area X
```

SCG forwards the request. SCG does not select the helper.

### RESPONSE

A response to a specific Dispatch.

Suggested minimal values:

```text
ACCEPT
DECLINE
PARTIAL
UNSUPPORTED
```

`ACCEPT` means only that the receiver has chosen to handle the request internally. It does not standardize how.

### RELEASE

"This intent/commitment/request is no longer active."

Reasons may include:

```text
COMPLETED
ABANDONED
FAILED
SUPERSEDED
NO_LONGER_NEEDED
```

The reason is informative. The receiver still decides what it means.

---

## 5. Small common action vocabulary

The common vocabulary should describe strategic effects, not units.

A useful first set is:

```text
ATTACK
DEFEND
EXPAND
SECURE
RECON
SUPPORT
```

A useful first capability set is:

```text
GROUND
AIR
SEA
TRANSPORT
CAPTURE
EXPANSION
```

Do not encode faction-specific units in the common protocol.

Bad:

```text
SEND 8 MIGS
BUILD MCV
SEND TANYA
```

Better:

```text
REQUEST AIR_SUPPORT
REQUEST EXPANSION_SUPPORT
REQUEST SPECIAL/CAPTURE capability   (only if/when standardized)
```

Each AI maps the abstract need to its own faction and architecture.

The vocabulary can grow only when two or more implementations have a real use for the same semantic concept.

---

## 6. Dispatch envelope

Proposed neutral shape:

```json
{
  "schema": "scg-dispatch/0.1",
  "game_uid": "...",
  "dispatch_id": "d-000184",
  "sender": "Multi0",
  "audience": "capability:AIR",
  "kind": "REQUEST",
  "action": "SUPPORT",
  "capability": "AIR",
  "target_player": null,
  "target_actor_id": null,
  "area": {
    "x": 91,
    "y": 44,
    "radius": 12
  },
  "urgency": "HIGH",
  "reply_to": null,
  "created_tick": 32150,
  "expires_tick": 33650
}
```

Not every field is required for every Dispatch.

Important properties:

- **location uses common OpenRA map coordinates**, not AI-private region ids;
- `target_player` uses stable player identity;
- `target_actor_id` is optional and only useful when the subject is a specific shared-world actor;
- every live Dispatch has a bounded lifetime;
- responses correlate through `reply_to`.

---

## 7. Liveness is part of the protocol

A Dispatch without a lifecycle becomes stale strategy.

This is particularly important after reviewing Cameo's current TeamBroadcast/TC-3 implementation, where `SnapshotTick` exists but freshness is not yet centrally enforced.

SCG should make liveness explicit from day one:

```text
created_tick
expires_tick
RELEASE
```

SCG may maintain an **active Dispatch ledger**, but this is not a world model or strategic brain. It is only message lifecycle state.

When `expires_tick` is reached, the Dispatch is no longer forwarded as active intent.

A sender may refresh an intent with a replacement/revision, or close it with `RELEASE`.

---

## 8. Routing — what SCG actually does

SCG has a deliberately small job.

### SCG MAY

1. validate protocol/schema;
2. maintain participant registration/capability metadata;
3. assign/validate dispatch IDs;
4. keep active messages until release/expiry;
5. forward to an explicit participant;
6. broadcast to the coalition;
7. forward to all participants that declared a requested capability/message kind;
8. relay responses back to the sender;
9. log the traffic for debugging/replay analysis.

### SCG MUST NOT

1. decide which participant is strategically best;
2. choose a winner between conflicting intents;
3. elect a rescue responder;
4. vote a main enemy target;
5. allocate territory;
6. infer unit orders;
7. maintain a second battlefield intelligence model;
8. require a receiving AI to act.

If two AIs announce conflicting `INTENT EXPAND` messages, SCG forwards the relevant intents. **The AI implementations decide how to react.**

If Fransbot sends a support request and Cameo plus another AI both accept, SCG relays both responses. **Fransbot decides what to do next.**

---

## 9. Stock AI participation

The protocol must allow partial participation.

### Level 0 — publish only

A stock-AI adapter can emit a few high-level events when the existing AI commits to an obvious action:

```text
INTENT / COMMITMENT ATTACK player X
INTENT / COMMITMENT EXPAND area Y
RELEASE ...
```

It does not need to understand incoming Dispatches.

Other AIs still benefit because they learn the stock AI's intention before or while its visible actions develop.

### Level 1 — basic receive

An adapter may understand only a tiny subset, e.g. support requests around a location.

Everything else returns `UNSUPPORTED` or is ignored according to the declared profile.

### Level 2 — full common protocol

A more advanced AI can send and receive the complete common v0.x vocabulary.

No level is allowed to pretend support for semantics it does not implement.

---

## 10. Same-family fast paths

SCG is not intended to make Fransbot-to-Fransbot or Cameo-to-Cameo communication worse.

Example team:

```text
Fransbot A <==== rich Fransbot channel ====> Fransbot B
     |                                         |
     +--------------- Dispatch ----------------+
                         |
                        SCG
                         |
     +--------------- Dispatch ----------------+
     |                                         |
 Cameo A   <===== Cameo TC-3 / blackboard ===> Cameo B
```

The rich same-family channels can coordinate at much higher resolution.

The SCG boundary remains the **coalition lingua franca**.

---

## 11. Suggested Cameo adapter boundary

Because current Cameo already has TC-1/TC-3, the clean implementation is a separate adapter, not a modification of the coalition fold.

Illustrative outbound mappings only:

```text
Cameo chooses/commits MainTarget
    -> Dispatch INTENT/COMMITMENT ATTACK target_player

ExpansionPlanner publishes a real expansion commitment
    -> Dispatch INTENT/COMMITMENT EXPAND area

Cameo decides it wants outside help
    -> Dispatch REQUEST <capability> area

Cameo abandons/completes that commitment
    -> Dispatch RELEASE reference
```

Do not automatically dump every TeamBroadcast field into SCG. Director tension, utility axes, army centroid and TC-3 sector internals are Cameo-private unless a future common use case explicitly justifies standardizing them.

For inbound traffic, expose an inbox/provider such as conceptually:

```text
IBotExternalCoalitionInbox
```

Existing Cameo owners may query that provider. The adapter itself should not issue gameplay orders.

The exact interface name is Cameo's choice; the architectural rule is the important part.

---

## 12. Suggested Fransbot adapter boundary

Fransbot can publish only coalition-relevant outcomes from its own strategic process:

```text
General intends SECURE area
    -> Dispatch INTENT SECURE

General commits resources through its own MissionCards
    -> Dispatch COMMITMENT SECURE

General needs a capability it cannot currently satisfy
    -> Dispatch REQUEST

General no longer needs / can no longer hold it
    -> Dispatch RELEASE
```

Inbound Dispatches become information for Fransbot's own General / Best Read / MissionCard logic **only if Fransbot's implementation chooses to use them**.

SCG does not create Fransbot MissionCards.

---

## 13. Versioning and extension rule

Start with a deliberately small versioned schema:

```text
scg-dispatch/0.1
```

Rules:

- unknown optional fields are ignored;
- unsupported action/capability is answered `UNSUPPORTED` when a response is expected;
- breaking semantic changes increment the major version;
- AI-private data must not become mandatory common fields.

If two same-family adapters want richer data through the same transport, SCG may relay an opaque optional extension block, but SCG must not understand or depend on it.

Example:

```json
"extensions": {
  "cameo": { "...": "opaque to SCG" }
}
```

This is optional. Direct same-family channels remain valid.

---

## 14. What success looks like

A mixed team can contain:

```text
2 x Fransbot
2 x Cameo
1 x stock AI
```

Fransbots may coordinate richly with each other. Cameos may coordinate richly through TC-3. Stock AI may understand only a small subset.

All five can still share the minimum common layer:

```text
what I intend to do
what I have committed to
what I need from allies
how I answer a request
when that intent/commitment is no longer active
```

That is enough for different AI developers to cooperate without merging their AI architectures.

The core design sentence is:

> **Shared vision communicates reality. Dispatch communicates intent. SCG relays the Dispatch. The receiving AI decides.**

