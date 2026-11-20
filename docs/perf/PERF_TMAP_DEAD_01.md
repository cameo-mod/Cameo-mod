# PERF-TMAP-DEAD-01

Base: `837a139150dc810bebbeebe9d1b33c3f16ee8d61`  
Branch: `fullstack/perf-tmap-dead-01`

## Static caller audit

Searched exact API and output names through `OpenRA.Mods.Cameo`, `OpenRA.Mods.CA`,
`OpenRA.Mods.Cameo.Dev`, `engine`, `mods/cameo` C# sources, Lua and YAML.
No external readers were found for useful-chokepoint outputs, sealable-corridor outputs,
high-ground edges, or territory doors/wall/front/door-derived outputs. `IBotZoneTopology.TerritoryDoors`
is declared outside TacticalMap but has no consumer. The overlay accessors likewise have no current
call sites in these source/config trees.

Internal readers were checked separately. Useful chokepoints feed only the optional topology-hotspot,
access-bearing and sealable-corridor APIs. Sealable corridors feed only their optional defense-anchor API.
High-ground edges have no internal reader. Territory wall/front/door details are not read by the territory
claim or region refresh. `BuildDoors` reads the already-built region graph and claim, but does not feed
the claim, region ownership, region roles, orders, placement or `GetRegionGateCorridor`. The claim's
`ResolveChokepointCorridors` remains eager because it gates the territory flood; region gates and region
ownership remain eager because they have decision readers.

## Change and determinism constraints

The optional useful, corridor and high-ground refreshes now return until their corresponding API or
overlay accessor requests that output. Once requested, the original incremental refresh runs on the bot
tick with the original interval, cursor and ordering. Territory membership remains on the original
unconditional refresh path. Its derived door/wall/front pass runs only when one of those output accessors
requests details; overlay accessors request the work for a later simulation tick rather than computing
from the render thread. The same snapshots and builder are used when the details are requested.

`ResetPerBaseCaches` retains the original five `BotRng.For(player, nameof(TacticalMapBotModule)).Next(interval)`
draws in their original order (high-ground, useful, corridor, territory, region ownership). The skipped
refresh methods contain no further `BotRng` calls. Shared topology construction, claim refresh, region
ownership refresh, `GetRegionGateCorridor`, and their execution order are unchanged. The optional debug
funnel message is emitted when the optional territory details are built.

## Verification

Added `LazyOptionalMapOutputsLeaveUsedTerritoryAndRegionOwnersBitIdentical`, comparing the used territory
membership and region-owner projections with the production lazy gate false/true. This pins the independent
core outputs while optional work is skipped or requested. `git diff --check` passes. Build, unit-test
execution and game/order-identity capture were not run under the active E3B batch freeze; the order-identity
game remains a separate Workhorse perf-window gate after static review.
