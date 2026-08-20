# Cameo Vision

This document preserves long-term, non-binding product intent; it is not an implementation queue, balance law, or release commitment.

Active, actionable work belongs only in [ROADMAP.md](ROADMAP.md). Binding gameplay and balance rules remain in [DESIGN.md](../DESIGN.md), [FORMULA_V2.md](FORMULA_V2.md), and [ARMOR_SYSTEM.md](ARMOR_SYSTEM.md).

## Dynamic Campaign Mode — The Singularity Crisis

A replayable, turn-based campaign blending the strategic overworld of *Empire at War*, pre-battle army building inspired by *Call to Arms: Gates of Hell*, and the boss-gauntlet structure of *C&C Generals: Zero Hour Challenges*.

### Narrative hook: the Convergence

Collapsing timelines draw every universe—Tiberian, Red Alert, Dune, StarCraft, Warcraft, Outpost, and custom timelines—into a single unstable singularity. The player’s faction develops a faction-specific Chronal-Lock-equivalent and must acquire pinnacle assets from other dimensions to stabilize it.

The commander and a vanguard army step through dimensional tears, select a target dimension, and complete a three-mission invasion before moving on. For example, a GDI commander might invade Schwarzer Mond for gravity technology, then the Zerg dimension for biological regeneration research.

### Strategic layer: the multiverse map

- **Dimensions:** map nodes represent Cameo’s factions.
- **Target selection:** the player chooses a faction dimension to invade.
- **Three-mission arc:** a selected dimension becomes a locked mini-campaign.
- **Threat scaling:** each defeated faction raises the Convergence Threat; later invasions begin with stronger AI technology, units, or resources.

### Pre-battle setup and meta-progression

Between missions and dimensional jumps, players return to a command ship or temporal hub to manage their army.

- **Starting MCV loadout:** earned Meta-Credits purchase the initial drop force, allowing the vanguard to grow across the campaign.
- **Universal meta-tech:** lasting passive rewards from defeated dimensions, such as vehicle regeneration or infantry Tiberium resistance.
- **Tactical tech:** standard faction research unlocks the right to research that technology during an RTS match; credits and time remain in-match costs.

### Tactical layer: the three-mission arc

1. **Beachhead:** establish a foothold against T1/T2 opposition and secure a forward base or local resource.
2. **Asymmetric objective:** complete a faction-tailored medium/high-difficulty objective, such as rescuing Yuri’s mind-controlled scientists or surviving a Nod subterranean ambush until evacuation.
3. **Artifact or commander assault:** face a fully unlocked, heavily fortified enemy to destroy an HQ, steal an artifact, or assassinate the commander.

### Co-op

Two-player co-op increases AI difficulty and passive income while pooling Meta-Credits for a shared MCV drop force.

### Long-term technical direction

- Handcraft terrain and faction-specific missions; use reusable Lua templates for mission objectives, AI behavior, and attack waves.
- Build procedural mission templates around the three-mission structure without substituting generated content for lore-accurate unit composition.
- Start a Campaign V1 with six dimensions—GDI, Yuri, Consortium, Schwarzer Mond, Ordos, and Zerg—then expand as maps and scripts are completed.
- Investigate an automated in-game balance-test harness for TTK measurement where the formula cannot price caster DoT, AoE, or mind-control reliably.
