# Three vehicle production queues — Light / Vehicle / Super

_Research note, 2026-08-22. Maintainer + TheCommando315, Discord: "light factory only for light,
normal factory for light and big, super factory for light, big and super" — and
"only don't make it confusing"._

**Verdict: this is a pure yaml change. No C# and no engine change.** Every mechanism it needs
already exists and is already used elsewhere in the tree. The cost is not difficulty, it is
BREADTH — 1193 `Queue: Vehicle` tags (plus 1160 `RAVehicle`) have to be re-sorted into three
buckets, and the classification for most of them does not exist yet.

---

## 1. Why a super tank currently walks out of a normal war factory

TheCommando315: *"the exits aren't well programmed in cameo — if you have multiple queues you can
exit a giant tank from any factory."* That is not a Cameo bug, it is what the engine is told to do:

```csharp
ProductionQueue.cs:920   if (!local.IsTraitDisabled && local.Info.Produces.Contains(type))
ProductionQueue.cs:926   pair.Trait.Info.Produces.Contains(type))
```

When an item finishes, the queue asks every building the player owns *"does your `Production`
trait's `Produces:` list contain my `Type`?"* and the first match becomes the exit. **Today every
vehicle-producing building declares `Produces: Vehicle`**, so every one of them is a legal exit for
every vehicle. The MonsterTank is not escaping through the wrong door — it is using a door we told
it was correct.

The fix follows directly: **make the `Produces:` sets disjoint** and the exits become disjoint too,
with no code involved.

---

## 2. The four moving parts

| part | where | what it decides |
|---|---|---|
| `ProductionQueue.Type` | player/factory rules | the queue's identity |
| `Production.Produces` | each factory | **which BUILDINGS can serve as an exit** |
| `Buildable.Queue` | each unit | which queue(s) the unit appears in |
| `Exit.ProductionTypes` | each door | **which DOOR on that building the unit uses** |

The last row is the answer to the garage-door question. `Exit` is per-door and already filters by
production type (`defaults.yaml:8057` — the Soviet barracks routes `Infantry, RAInfantry` and
`ra1_soviets_attackdog` through specific cells). So a super factory can carry one wide exit for the
epic units and normal exits for everything else:

```yaml
	Exit@BIGDOOR:
		SpawnOffset: 0,1400,0
		ExitCell: 0,3
		ProductionTypes: SuperVehicle
		Priority: 3
	Exit@NORMAL:
		ExitCell: 0,1
		ProductionTypes: LightVehicle, Vehicle
		Priority: 2
```

---

## 3. Recommended shape — three queues, ONE sidebar button

`ProductionQueueInfo` has both `Type` and `Group`, and they do different jobs:

```
Type   = the queue identity (what Produces: matches)
Group  = "Group queues from separate buildings together into the same tab"
```

`ProductionTabsCAWidget.cs:120` builds one top-level sidebar BUTTON per distinct `Group`, and
`:191` gives each QUEUE inside that group its own tab — shown only when it has buildable items
(`Queue.BuildableItems().Any() || Queue.AlwaysVisible`).

So setting all three queues to **`Group: Vehicle`** gives:

- **one** Vehicle button in the sidebar — no new group icon art needed, since the chrome image is
  looked up from the group name (`ProductionTabsLogicCA.cs:43`, `group.ToLowerInvariant()`);
- **three** tabs inside it: Light / Vehicle / Super;
- the Super tab **disappears entirely** when the player owns no super factory, and the Light tab
  likewise. Nothing empty is ever shown.

That is the "don't make it confusing" answer: the player still clicks one Vehicle button, and the
extra tabs only appear when they are earned.

⚠ Three distinct `Group`s instead would mean three sidebar buttons **and three new chrome icons**,
one per group name. Not recommended.

### The containment ladder

The maintainer's rule maps onto `Produces:` with no special-casing:

| factory | `Production.Produces` |
|---|---|
| light factory | `LightVehicle` |
| war factory | `LightVehicle, Vehicle` |
| super factory | `LightVehicle, Vehicle, SuperVehicle` |

Each unit stays in **exactly one** queue (`Buildable.Queue: LightVehicle`), and the factory decides
what it can serve. A light vehicle therefore rolls out of any of the three; a super unit only ever
out of the super factory.

---

## 4. Everything doubles — the classic/RA parallel system

Cameo runs **two** queue systems side by side, switched by the `classicproductionqueues` condition
(`actiblizz.yaml:287-292`):

```yaml
	Production@NORMAL:
		Produces: Vehicle
		RequiresCondition: !classicproductionqueues
	Production@CLASSICPRODUCTIONQUEUES:
		Produces: RAVehicle
		RequiresCondition: classicproductionqueues
```

So this is not 3 new queues, it is **6**: `LightVehicle`/`RALightVehicle`,
`Vehicle`/`RAVehicle`, `SuperVehicle`/`RASuperVehicle`. Every unit tag becomes
`Queue: LightVehicle, RALightVehicle`, matching the existing `Queue: Vehicle, RAVehicle` pattern.

---

## 5. The three steps that are easy to forget

1. **The AI stops building anything in a queue it has never heard of.** `ai.yaml:4488`:
   ```
   UnitQueues: Infantry, RAInfantry, SCZergInfantry, Vehicle, RAVehicle, Starport, Aircraft, …
   ```
   The four new names must be added or every bot silently stops producing light and super vehicles.
   This produces no error and no crash — just an AI that never fields a tank again.
2. **`ProductionBar`** is per production type (`ProductionBar@VEHICLEGDI: ProductionType: Vehicle`),
   so each factory needs bars for the types it now produces, in both condition variants.
3. **`ProductionQueue` needs its audio block repeated** — `ReadyAudio`, `BlockedAudio`,
   `QueuedAudio` etc. are per-queue, not inherited from a sibling. Copy the block or the new
   queues go silent.

---

## 6. Scope

| item | count |
|---|---|
| `Queue: Vehicle` tags to re-sort | **1193** |
| `Queue: RAVehicle` tags to re-sort | **1160** |
| vehicle-producing buildings needing new `Produces:` | ~85 `Produces: Vehicle` + 79 `RAVehicle` |
| new `ProductionQueue` trait blocks | 6 (3 × 2 systems) |

**The classification is the real work, not the plumbing.** The ledger already carries a
`class_anchor` per actor under `["design"]["class_anchor"]`, and it already has the right buckets:

    epic_vehicle    24     <- the Super queue
    scout_vehicle   28     <- the Light queue
    light_tank      16     <- the Light queue
    mbt             42
    high_tech_tank  26
    line_breaker    30
    ...             total 346 tagged

But only **346 actors are tagged at all**, and **122 `Vehicles` + 4 `Tanks` carry a `design` block
with no `class_anchor`** — with well over a thousand `Queue: Vehicle` tags in yaml, most vehicles
have no class assignment to sort on. So the honest order is:

1. finish `class_anchor` tagging for vehicles (a balance-ledger job, and useful on its own);
2. generate the `Buildable.Queue` rewrite mechanically from the tag — never hand-sort 1193 entries;
3. hand-author only the ~164 `Produces:` lines and the 6 queue blocks.

---

## 7. Open decisions for the maintainer

1. **Where does the boundary sit?** `epic_vehicle` -> Super is obvious. Light is ambiguous:
   `scout_vehicle` + `light_tank` only (44 tagged), or does `missile_vehicle` /
   `anti_air_vehicle` / `support` count as light too? This decides whether the Ordos light factory
   is a niche building or a real early-game alternative.
2. **Harvesters, MCVs and support vehicles** — one of `Light`, `Vehicle`, or left where they are.
   An MCV in the Light queue would let a light factory rebuild a base.
3. **Does the super factory replace the war factory or stack with it?** The containment ladder
   above means a super factory alone can build everything, which makes the war factory redundant
   once you have one. If they should stack, the super factory produces `SuperVehicle` **only**.
4. **Naming.** `LightVehicle` / `Vehicle` / `SuperVehicle` keeps the existing `Vehicle` string
   untouched, so nothing already tagged has to move except the units that change bucket. Underscore
   law is satisfied (no hyphens); the queue strings are engine tokens either way.
