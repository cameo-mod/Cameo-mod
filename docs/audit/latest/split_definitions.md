# Split definitions — one weapon, two live files, one silent merge

Live weapon files in the manifest: **40** · names defined more than once: **26**

The engine MERGES same-named top-level nodes across files. Editing one copy leaves the other supplying its own fields, so a removal can silently do nothing — see the `HMG` incident in this file's docstring.

| bucket | count | baseline |
|---|--:|--:|
| S1 legacy global + ContentPack | 22 | 56 |
| S2 same tier twice | 4 | 2 |


## S1 — defined in a legacy global AND a ContentPack (22)

ContentPack-migration residue. **Fix by deleting the LEGACY copy** once the pack copy is complete — never by editing both, which is how the two drift apart. ⚠ Check `mod.yaml` load order before deleting: if the global loads LATER it is the one whose fields win today, so a naive delete changes behaviour. Diff the resolved weapon before and after with `tools/audit/review_resolve_diff.py`.

| weapon | defined at |
|---|---|
| `ChemTibAtomic` | `ContentPacks/RedAlert/Shared/yaml/weapons.yaml:1257` · `weapons/tiberiandawn.yaml:230` |
| `RocketsG` | `ContentPacks/RedAlert/Shared/yaml/weapons.yaml:1381` · `ContentPacks/RedAlert/Shared/yaml/weapons.yaml:1385` · `weapons/weapons.yaml:12036` |
| `SardDeath` | `ContentPacks/D2k/Ordos/yaml/weapons.yaml:2527` · `weapons/d2k.yaml:1034` |
| `Sound` | `ContentPacks/D2k/Atreides/yaml/weapons.yaml:15` · `weapons/d2k.yaml:647` |
| `Sound2` | `ContentPacks/D2k/Ordos/yaml/weapons.yaml:2497` · `ContentPacks/D2k/Atreides/yaml/weapons.yaml:63` · `weapons/d2k.yaml:694` |
| `WormSwallow` | `ContentPacks/D2k/Ordos/yaml/weapons.yaml:2512` · `weapons/d2k.yaml:784` |
| `^D2K155mmLegacy` | `ContentPacks/D2k/Ordos/yaml/weapons.yaml:2343` · `weapons/d2k.yaml:154` |
| `^OCannon` | `ContentPacks/D2k/Ordos/yaml/weapons.yaml:2887` · `weapons/d2k.yaml:1816` |
| `d2k25mm` | `ContentPacks/D2k/Ordos/yaml/weapons.yaml:2572` · `weapons/d2k.yaml:1228` |
| `d2kFlameTurret` | `ContentPacks/D2k/Ordos/yaml/weapons.yaml:2721` · `weapons/d2k.yaml:1440` |
| `d2k_APCo_AA` | `ContentPacks/D2k/Ordos/yaml/weapons.yaml:2456` · `weapons/d2k.yaml:544` |
| `d2k_APCo_AG` | `ContentPacks/D2k/Ordos/yaml/weapons.yaml:2447` · `weapons/d2k.yaml:536` |
| `d2k_aircraft_eater` | `ContentPacks/D2k/Ordos/yaml/weapons.yaml:2819` · `weapons/d2k.yaml:1748` |
| `d2k_airdefenseplatform` | `ContentPacks/D2k/Ordos/yaml/weapons.yaml:2766` · `weapons/d2k.yaml:1725` |
| `d2k_laser_qafza` | `ContentPacks/D2k/Ordos/yaml/weapons.yaml:2730` · `weapons/d2k.yaml:1652` |
| `d2k_laser_qafza_aa` | `ContentPacks/D2k/Ordos/yaml/weapons.yaml:2747` · `weapons/d2k.yaml:1669` |
| `d2k_sard_crossbow` | `ContentPacks/D2k/Ordos/yaml/weapons.yaml:2859` · `weapons/d2k.yaml:1788` |
| `d2k_sard_heatblade` | `ContentPacks/D2k/Ordos/yaml/weapons.yaml:2838` · `weapons/d2k.yaml:1767` |
| `d2k_sardaukar_elite` | `ContentPacks/D2k/Ordos/yaml/weapons.yaml:2590` · `weapons/d2k.yaml:1372` |
| `d2k_tyrant` | `ContentPacks/D2k/Ordos/yaml/weapons.yaml:2385` · `weapons/d2k.yaml:497` |
| `emperor_sardaukar_chief_c4` | `ContentPacks/D2k/Ordos/yaml/weapons.yaml:2716` · `weapons/d2k.yaml:1420` |
| `mtank_pri` | `ContentPacks/D2k/Shared/yaml/weapons.yaml:548` · `weapons/d2k.yaml:479` |


## S2 — defined twice within the same tier (4)

| weapon | defined at |
|---|---|
| `Flamethrower` | `weapons/tiberiandawn.yaml:75` · `weapons/starcraft.yaml:1` |
| `OrniBombC` | `ContentPacks/D2k/Ordos/yaml/weapons.yaml:2523` · `ContentPacks/D2k/Atreides/yaml/weapons.yaml:89` |
| `OrniGunC` | `ContentPacks/D2k/Ordos/yaml/weapons.yaml:2610` · `ContentPacks/D2k/Atreides/yaml/weapons.yaml:153` |
| `ZClaw3` | `weapons/tiberiansun.yaml:1226` · `weapons/tiberiansun.yaml:1868` |


**FAIL** — S1 22/56, S2 4/2. A new split definition landed. Delete the duplicate rather than editing both copies.
