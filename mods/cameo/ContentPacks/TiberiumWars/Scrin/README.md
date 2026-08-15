# Scrin content pack

This is a structures-and-defenses foundation for the future Scrin faction, now
with an initial map/editor-ready Eradicator Hexapod visual. It is not a playable
roster yet, and the faction remains hidden from the lobby. In particular, the
Drone Platform must eventually be deployed from a Drone Ship; the platform is
not registered as a starting actor.

The pack contains 23 approved map/editor-ready actors. All sheets pair their
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

The actors currently expose only the shared building behavior, turret rigs,
and provisional Hexapod locomotion needed by maps and the editor. Production
roles, weapons, costs, power, prerequisites, construction animations,
destruction art, AI, and balance are intentionally deferred. Apart from the
reviewed Drone Platform 3x2 footprint, the new gameplay footprints remain
provisional scaffolding.

The sprites were independently extracted and rendered from the official
*Command & Conquer 3: Tiberium Wars* and *Kane's Wrath* assets. The source
models and textures are copyright Electronic Arts and remain the property of
their respective owner.
