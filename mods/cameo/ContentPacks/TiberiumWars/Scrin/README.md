# Scrin content pack

This is a map/editor-ready visual foundation for the future Scrin faction, now
covering its approved structures, defenses, Eradicator Hexapod, and initial
infantry, vehicle, and aircraft rosters. It is not a playable roster yet, and the faction
remains hidden from the lobby. The map/editor-ready Drone Ship now lands and
transforms into the Drone Platform, which plays its genuine deployment cycle.

The pack contains 51 approved map/editor-ready actors. All sheets pair their
body art with separate, model-derived ground shadows. The structures use
Cameo's dynamic `player_rgba` palette. Reactor and Signal Transmitter remain
static; Extractor, Growth Accelerator, and Storm Column have 16-frame idle
cycles. Photon Cannon and Plasma Missile Battery use 32 embedded turret
facings. Drone Platform uses a
45-frame, 1.8-second make sequence whose final frame is also its exact idle
frame. Combined with the Drone Ship's tuned landing rate, deployment takes
approximately three seconds.

Portal, Warp Gate, and Warp Chasm cores use RGBA animation sheets so their
authored soft transparency survives the runtime sprite loader. Alternate linked
portal colors remain deferred until the gameplay mode is implemented.

The Reaper-17 Growth Stimulator uses its distinct Growth Accelerator-derived
body with four Tiberium extraction pods. The Global Conquest Lifeform Recycling
Plant and Terraforming Nexus are included as map/editor actors; their strategic
operations remain outside the current faction foundation.

## Top-priority todo

These are the most desirable next features for the Scrin faction, in priority
order:

1. **Engine-supported Scrin building materialization.** Add a reusable,
   configurable construction effect. On creation, the actor sprite first fades
   in as a white silhouette over a configurable interval. A jagged electric
   boundary then travels from bottom to top, progressively replacing the white
   silhouette with the actor's actual sprite. Timing, electric-band appearance,
   and affected sprite bodies must be configurable; the result must not require
   a separately baked make animation for every structure.

The earlier Tiberium-creep visual pilot was rejected and is deliberately not
part of this checkpoint. A replacement ground-field concept remains deferred
until it has a stronger visual direction.

The Eradicator Hexapod uses the approved AIDA frame 65 static pose and the
corrected WLKA cycle sampled to 12 frames across 32 facings. Its indexed source
frames are stored at half resolution and displayed at 2x scale, preserving the
approved physical size with a more native OpenRA pixel density. Its embedded
palette reserves indices 249-255 for `scrin_hexapod_player` remapping.

The initial infantry roster contains the Disintegrator, Assimilator, Shock
Trooper, Blink Pack Shock Trooper, Ravager, Mastermind, and Prodigy. Each uses
the approved held idle pose and 12-frame walk cycle across 8 facings. Their
indexed idle and movement sheets share a per-unit embedded palette, with indices
249-255 reserved for player-color remapping. Cultist is deliberately excluded;
Buzzer remains deferred to a later simplified FX-style representation.

The initial vehicle roster contains the Explorer, Harvester, Seeker, Devourer
Tank, Repair Drone, Gun Walker, Shard Walker, Corrupter, Annihilator Tripod, and
Reaper Tripod. Hover vehicles remain static except for the Repair Drone. The
walkers use approved native movement cycles across 32 facings; Gun Walker has
softened leg-joint skinning to preserve its silhouette at OpenRA resolution.
Each indexed sheet reserves indices 249-255 for player-color remapping. Every
mobile-unit facing is locked to an authored body/rig pivot, with the same
per-facing correction applied to its complete body and native shadow group.
This prevents asymmetric hulls, barrels, tentacles, and lifted legs from making
the model orbit its cell while turning without suppressing real walk motion.

The initial aircraft roster contains the Drone Ship, Stormrider, Devastator
Warship, Planetary Assault Carrier, Invader Fighter, and Mothership. Their
approved 32-facing spacecraft renders use fixed authored pivots, native ground
shadows, and bright player-color ramps. The PAC idle visual carries its full
complement of eight docked Invader Fighters. Carrier launch/recovery behavior
remains deferred.

The actors currently expose only the shared building behavior, turret rigs,
and provisional unit locomotion needed by maps and the editor. Production roles,
weapons, costs, power, prerequisites, construction animations, destruction art,
AI, and balance are intentionally deferred. Apart from the reviewed Drone
Platform 3x3 cross footprint (`_x_ / xxx / _x_`), the new gameplay footprints
remain provisional scaffolding.

The sprites were independently extracted and rendered from the official
*Command & Conquer 3: Tiberium Wars* and *Kane's Wrath* assets. The source
models and textures are copyright Electronic Arts and remain the property of
their respective owner.
