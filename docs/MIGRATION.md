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
3. LATER PHASES (per pack): own ai.yaml; move assets into the pack in
   per-type subfolders; unused-file audit + deletion; per-faction fluent
   descriptions (DESIGN.md §7) on top of final ids.
```

Every step is behavior-preserving by proof, never by hope. Balance changes
are never mixed in; typo-class bugs found on the way are fixed in separate
commits and reported.

## Status board

| faction | rename | pack split | own ai.yaml | assets in pack | fluent descriptions |
|---|---|---|---|---|---|
| Forgotten (TS) | DONE `d7b86798d` | DONE `03ce7e96c` | — | — | — |
| TS GDI | DONE `9d901fb45` | DONE `dfa00f20f` | — | — | — |
| TS Nod | DONE `8c82ad950` | DONE `a34c80678` | — | — | — |
| CABAL | **next?** on design hold (WIP faction — call when to rename) | | | | |
| TS Shared | after TS factions (shared actor ownership decisions) | | | | |
| TD GDI / TD Nod | rules already packed | weapons+sequences split pending | — | — | — |
| RA2Mod six, D2k four | rules already packed | weapons+sequences split pending | — | — | — |
| RA1 (allies/soviet/japan) | maps drafted | monolith | | | |
| RA2 (america/russia/yuri) | maps drafted | monolith | | | |
| StarCraft / WC2 / TKM / Outpost2 | maps drafted (op2 ~compliant) | monolith/wrappers | | | |

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
- Commit policy: clean commits, one concern each; commit when design says.
