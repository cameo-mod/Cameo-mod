# Scrin content pack

This is a map/editor-ready visual foundation for the future Scrin faction, now
covering its approved structures, defenses, Eradicator Hexapod, and initial
infantry and vehicle rosters. It is not a playable roster yet, and the faction
remains hidden from the lobby. In particular, the Drone Platform must eventually
be deployed from a Drone Ship; the platform is not registered as a starting actor.

The pack contains 40 approved map/editor-ready actors. All sheets pair their
body art with separate, model-derived ground shadows. The structures use
Cameo's dynamic `player_rgba` palette. Reactor has a restrained 12-frame
central-spire cycle; Extractor, Growth Accelerator, and Storm Column have
16-frame idle cycles. Photon Cannon and Plasma Missile Battery use 32 embedded
turret facings. Signal Transmitter remains static.

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
Each indexed sheet reserves indices 249-255 for player-color remapping.

The actors currently expose only the shared building behavior, turret rigs,
and provisional unit locomotion needed by maps and the editor. Production roles,
weapons, costs, power, prerequisites, construction animations, destruction art,
AI, and balance are intentionally deferred. Apart from the reviewed Drone
Platform 3x2 footprint, the new gameplay footprints remain provisional
scaffolding.

The sprites were independently extracted and rendered from the official
*Command & Conquer 3: Tiberium Wars* and *Kane's Wrath* assets. The source
models and textures are copyright Electronic Arts and remain the property of
their respective owner.
