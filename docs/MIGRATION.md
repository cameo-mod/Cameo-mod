# Cameo Migration Board — status + runbook

_The living status of the great restructuring. Any agent or contributor can
resume the work from this file alone. Rules live in [DESIGN.md](DESIGN.md);
this file tracks WHERE WE ARE and HOW TO CONTINUE._

## The mission (why all this exists)

Cameo is the ultimate crossover RTS between the classic RTS games, and it
keeps growing. Loading everything at boot peaked at **12 GB RAM**, locking
out 8 GB/4 GB players before the main menu. The end state is **dynamic
faction loading**: the game loads only the factions picked in the lobby
(and only what the active shellmap needs). That requires every faction to
be a **completely self-contained ContentPack** — rules, weapons, sequences,
its own ai.yaml, and all of its assets (sprites, voxels, icons, sounds) in
per-type subfolders — with zero cross-pack dependencies, shared content
only in theme `Shared/` packs or core, and an audit that deletes files
nothing references.

## Target content-pack folder structure (design 2026-07-12)

Every content pack gets the SAME shape — one folder per faction:

```
ContentPacks/<Theme>/<Faction>/
  content.yaml          # the central include dictionary (stays at root)
  yaml/                 # ALL MiniYaml: rules + weapons + sequences merged
                        #   (folder name decided 2026-07-12: `yaml`)
  files/               # ALL assets: sprites, voxels, sounds, icons
                        #   (created empty now; asset migration is later)
```

This replaces the current `rules/ + weapons/ + sequences/` split (they
collapse into the single yaml folder). Shared assets live in a **per-game**
`ContentPacks/<Theme>/Shared/files/` folder if they are shared only between
factions of the same game. Assets shared **across different games** are a
critical architecture error and must move to the top-level
`ContentPacks/Shared/files/` folder, then be duplicated/replaced so each
game owns its own copy. **Do the yaml-folder move first** (create the
folder, move the yaml, keep `content.yaml` at root, update the
`content.yaml` include paths, create an empty `files/`); the asset
migration into `files/` comes later. Research one already-split pack and
apply the identical structure everywhere.

**YAML-folder restructure COMPLETE (2026-07-14):** All ContentPacks now
use the `yaml/` + `files/` structure. The old `rules/`, `weapons/`, and
`sequences/` subdirectories have been removed. `translations/` stays
separate (not MiniYaml).

**Asset migration IN PROGRESS (2026-07-15):**
- `mods/cameo/mod.yaml` mounts per-faction, per-game shared, and
cross-game shared packages before `bits/` so new `files/` content shadows
legacy assets.
- CABAL unique assets (128 files) migrated into
`ContentPacks/TiberianSun/CABAL/files/{icons,sprites,voxels}` and
referenced with `cabal_*|<name>` prefixes.
- 38 single-file cross-game shared assets migrated into
`ContentPacks/Shared/files/sprites/` and referenced with
`shared_sprites|<name>` prefixes across D2k, RedAlert2Mod, TiberianDawn,
and TiberianSun.
- TiberianSun intra-game shared assets (21 files) migrated into
`ContentPacks/TiberianSun/Shared/files/{icons,sprites,voxels}` and
referenced with `ts_shared_*|<name>` prefixes.
- Remaining cross-game shared assets with multiple variants or name
  collisions are still in `bits/` and tracked as critical: `gunfire2`
  (generic/RA/TD variants), `electro` (7 tileset variants), `dragon`
  (RA sprite vs WC2 sound collision), and `d2k/DATA.R16` (resource
  package). These must be resolved before `bits/` can be deprecated.

**Cross-faction shared effects — long-term de-sharing.** Factions now
cross-reference each other's effect sprites/weapons heavily. The
end-state needs each faction's effects to be its own, or at minimum
shared only PER GAME (never across games), so a pack loads without
pulling another faction's assets. The top-level `ContentPacks/Shared/files/`
folder is a temporary holding area for cross-game assets that must be
duplicated per-game and then removed.

## AI module split — why it is blocked and how to unblock it

The global `mods/cameo/ai/ai.yaml` defines a single `Player:` actor with
one `BaseBuilderBotModuleCA@generic`, one `UnitBuilderBotModuleCA@generic`,
and one `SquadManagerBotModuleCA@generic`. Their sub-sections
(`BuildingLimits`, `BuildingFractions`, `UnitsToBuild`, `UnitLimits`) are
single dictionaries containing ALL faction data. OpenRA's YAML loader
replaces trait instances with the same `@name`; it does **not** deep-merge
their sub-sections. This means per-faction bot data cannot be split across
multiple files by simply adding more `BaseBuilderBotModuleCA@<faction>`
traits — the last one loaded wins.

### Candidate solutions (ranked by preference)

1. **Custom `GrantConditionOnFaction` trait (C#)** — add a trait that sets
   a player condition based on the chosen faction (e.g., `cabalbot`). Each
   ContentPack can then define `BaseBuilderBotModuleCA@cabal` with
   `RequiresCondition: cabalbot`. This keeps the YAML split clean and does
   not require changing the engine or the lobby UI. The condition provider
   can be added to the `Player:` actor in the core rules or injected by each
   faction's content pack. **Recommended path.**

2. **Per-faction bot names** — create `ModularBot@CabalEasiestAI` with
   `Name: bot_ai.cabal.easiest` etc. The lobby would offer a bot per
   faction, but the player must manually pick the matching bot. This is
   fragile and breaks the current "pick a difficulty, play any faction"
   flow. **Not recommended.**

3. **Engine YAML merge change** — modify OpenRA to deep-merge
   `BaseBuilderBotModuleCA@*` sub-sections. This is the most invasive and
   makes the fork harder to maintain. **Last resort.**

4. **Single-file per-faction AI with a custom loader** — keep one AI file
   per faction but load it into a shared dictionary at runtime via a custom
   C# bot module. This gives per-faction files but still requires code.

### Next step

Design and implement the `GrantConditionOnFaction` trait (or equivalent),
add the corresponding `Player:` conditions, then split the faction-specific
`BuildingLimits`/`BuildingFractions`/`UnitsToBuild` entries out of the
global `ai.yaml` into each ContentPack's `ai.yaml`.

## The per-faction pipeline (proven, verified, repeatable)

```
1. RENAME   python tools/rename/curate_map.py <faction> [--slugtag <tag>]
            # stops and asks design if display names collide (DESIGN.md §1)
            python tools/audit/dump_resolved.py --faction <f> > before.json
            python tools/rename/apply.py tools/rename/rename_map_<f>.yaml
            # verify: map before.json through the map, diff resolved trees
            # -> MUST be empty; zero old ids; assets on disk.  Commit.
2. SPLIT    python tools/packs/split_faction.py --theme <T> --faction <F> \
                --prefix <id_prefix> --rules <mono>.yaml --weapons ... \
                --sequences ...
            # verify: actor/weapon/sequence registries identical, resolved
            # closure diff empty.  Commit.
3. DESCRIBE  Part of EVERY faction split: rework the faction's actor,
   upgrade, and promotion descriptions into Fluent (DESIGN.md §7 — the
   Tarantula model: derived from resolved traits/weapons/upgrades, RA1
   layout, Strong/Weak vs). One sample actor goes to design review first,
   then the faction is batched.
4. LATER PHASES (per pack): own ai.yaml; move assets into the pack in
   per-type subfolders; unused-file audit + deletion.
```

Every step is behavior-preserving by proof, never by hope. Balance changes
are never mixed in; typo-class bugs found on the way are fixed in separate
commits and reported.

## Status board

| faction | rename | pack split | own ai.yaml | assets in pack | fluent descriptions |
|---|---|---|---|---|---|
| Forgotten (TS) | DONE `d7b86798d` | DONE `03ce7e96c` | — | — | DONE `d723a6b78` |
| TS GDI | DONE `9d901fb45` | DONE `dfa00f20f` | — | — | — |
| TS Nod | DONE `8c82ad950` | DONE `a34c80678` | — | — | — |
| CABAL | DONE | DONE | — | DONE `68cdd5ebb`/`472209150` | DONE `68cdd5ebb` |
| TS Shared | — | — | — | DONE `6835a04` | — |
| Top-level Shared | — | — | — | DONE `e1b153d9c`/`472209150` | — |
| TD GDI / TD Nod | rules packed (ids unrenamed) | DONE incl. weapons+sequences | — | — | — |
| RA2Mod six, D2k four | rules packed (ids unrenamed) | DONE incl. weapons+sequences | — | — | — |
| RA1 (allies/soviets/japan) | DONE incl. 52 legacy ids 2026-07-17 (RAE1→ra1_allies_rifleinfantry etc.; only `japan` unprefixed; map: rename_map_ra1_legacy.yaml) | DONE 2026-07-16 (yaml/ layout, registry-identical, boot-verified) | — | — | — |
| RA2 (america/russia/yuri) | maps drafted | monolith | | | |
| StarCraft (terran/zerg/protoss) | DONE (ids) | DONE 2026-07-17 (registry-identical, boot-verified) | — | — | — |
| WC2 (humans/orcs) · TKM | DONE (ids) | DONE 2026-07-17 (registry-identical, boot-verified) | — | — | — |
| Outpost2 (eden/plymouth, WIP factions) | maps drafted (~compliant) | monolith/wrapper | | | |

Proposal maps for every faction: `tools/rename/rename_map_<faction>.yaml`
(regenerate: `python tools/audit/gen_rename_maps.py`).

## Standing decisions (design)

- Names: one lowercase group, RA1 baseline; tooltip <-> id in sync; unique
  tooltips per faction; **new display names are design's pick — propose
  options first** (blue fiend -> "Vinifera Fiend").
- Voice sets & shared namespaces are never renamed with a unit (apply.py
  protects them; `tsmedic` collision caught live).
- Pack layout: rules split per actor type; exactly ONE weapons.yaml and ONE
  sequences.yaml per faction.
- Every faction keeps a Tier-1 defense; DEFERRED audit findings list what
  waits on that.
- **Keep `bits/` until all factions are ContentPacked.** The legacy `bits/`
  folder is the archive for the old 0.31 ("classic cameo") factions and
  shared assets that have not yet been migrated. It stays mounted after all
  ContentPack packages so migrated assets shadow the legacy copies. Only
  remove `bits/` after (a) every referenced asset has been moved to a faction,
  per-game Shared, or top-level Shared pack; (b) the remaining cross-game
  shared assets have been duplicated per-game; and (c) an unused-file audit
  has been run and the old factions are either restored as ContentPacks or
  formally dropped.
- Commit policy: clean commits, one concern each; commit when design says.
