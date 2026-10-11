# Map engine acceptance and runtime seat proof — BU pilot maps (2026-10-10)

Claude (Opus), branch `ccr-c7f51935-i53fkj`, engine `6da7fce14`, Linux cloud container. This is the runtime half of
the pilot gate "engine acceptance plus symmetric spawn proof for all eight maps". `SPAWN_SYMMETRY_2026-10-11.md` is
the static half: geometry and ore distances.

**Tool:** `tools/ai/map_engine_acceptance.py`. For each planned setup it renders the variant with the pinned pilot
writer (`prove_generated_map`) and installs the dual-arm AI as map rules (`install_dual_arm_rules`). It then plays the
variant headless (`Game.Platform=Null`) to a 1,500-tick cap with seed 1337 and proves from the match records, joined
on the spawn coordinate, that the engine did what the plan says:
- no exception and no `Fatal Lua Error`;
- no `Invalid bot type`;
- the cap marker in `lua.log` and the seed pin in `server.log`;
- one record per planned seat, with the planned bot type and faction;
- allies and opponents exactly the planned sides.

Per setup, games 0 and 1 must swap the arms between the two spawn sets, and both arms must field the same factions.
These are preflight games, never outcome cells.

## What the first runs found (before the fixes)

| Run | Result | Meaning |
|---|---|---|
| campaign path as written | `Could not find map 'acceptance_…'` | variants were written to `Support/UserMaps`, not a `MapFolders` entry (fixed) |
| next | no cap marker found | markers are in `lua.log`, not `server.log` (fixed) |
| 5 cells, old planner | all `Invalid bot type: hard_control / hard_treatment`; one seat `undecided` after a Lua crash at the cap | the dual-arm overlay was ignored, so no arm's bot ran (fixed: map rules) |
| same 5 cells | Nuclear Winter: control at 11,45 in **both** games; pair proof `arms do not swap spawn sets` | the planner's two flips cancelled (fixed in `make_jobs`) |

## After the fixes

Probe (`1v1_nuclear_gdi`, game 0): ACCEPTED in 121 s. Both bots activated (0 `Invalid bot type`), no Lua error, both
seats `lost` at the cap (the mutual-loss form `CENSORED_CAP` expects).

`1v1_nuclear_gdi` pair (partial sweep): game 0 put control at 11,45 and treatment at 90,24; game 1 put treatment at
11,45 and control at 90,24. Pair proof OK; peak RSS 4.9–5.0 GB; about 115 s per cell.

**Full sweep: 32 of 32 cells and 16 of 16 pairs ACCEPTED, all eight maps.** That is all 16 setups, both games,
seed 1337. Receipt: `docs/audit/map_engine_acceptance_2026-10-10.json`.

| Map | Setups (team size) | Cells | Pairs (arm swap + faction mirror) |
|---|---|---|---|
| nuclear_winter | 1v1 gdi, 1v1 nod | 4/4 | 2/2 |
| satans_clutch | 1v1 allies, 1v1 soviets | 4/4 | 2/2 |
| red_spice | 2v2 ×3 | 6/6 | 3/3 |
| terra_cotta | 2v2 ×3 | 6/6 | 3/3 |
| back_to_basics | 3v3 ×2 | 4/4 | 2/2 |
| winters_end_rich | 3v3 ×2 | 4/4 | 2/2 |
| great_sahara_2 | 4v4 | 2/2 | 1/1 |
| ice_cold | 4v4 | 2/2 | 1/1 |

Every cell (144 seats in total) met all of the following:
- both arms' bots activated;
- no exception and no Lua error;
- every seat `lost` at the cap (mutual loss);
- seed pinned;
- each seat on its planned spawn with its planned bot type and faction;
- allies and opponents exactly the planned sides.

Headless peak RSS was 4.76–5.24 GB (median 4.87), almost flat from 2 to 8 seats, because the textures are gone and
what remains is load-phase. Each cell took 102–130 s.

Provenance note: the receipt's `manifest_sha256` (`315adb74…`) is an earlier regeneration of the candidate. It differs from
the committed `126d57ec…` only in `tool_sha256`. The tools and `OpenRA.Platforms.Null.dll` the sweep ran are
byte-identical to the committed ones (checked with `cmp`).

**On the Windows host:** re-run the tool with all 16 setups (the default) after `make.cmd all`; it needs
`engine/bin/OpenRA.Platforms.Null.dll`. Keep the receipt with the campaign evidence. The DevOps lead and the
maintainer decide whether this closes the gate.
