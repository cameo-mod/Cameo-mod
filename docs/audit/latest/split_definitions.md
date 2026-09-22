# Split definitions — one weapon, two live files, one silent merge

Live weapon files in the manifest: **40** · names defined more than once: **27**

The engine MERGES same-named top-level nodes across files. Editing one copy leaves the other supplying its own fields, so a removal can silently do nothing — see the `HMG` incident in this file's docstring.

| bucket | count | baseline |
|---|--:|--:|
| S1 legacy global + ContentPack | 22 | 56 |
| S2 same tier twice | 5 | 2 |


## S1 — defined in a legacy global AND a ContentPack (22)

ContentPack-migration residue. **Fix by deleting the LEGACY copy** once the pack copy is complete — never by editing both, which is how the two drift apart. ⚠ Check `mod.yaml` load order before deleting: if the global loads LATER it is the one whose fields win today, so a naive delete changes behaviour. Diff the resolved weapon before and after with `tools/audit/review_resolve_diff.py`.

| weapon | defined at |
|---|---|
| `ChemTibAtomic` | `ContentPacks/RedAlert/Shared/yaml/weapons.yaml:1306` · `weapons/tiberiandawn.yaml:231` |
| `RocketsG` | `ContentPacks/RedAlert/Shared/yaml/weapons.yaml:1439` · `ContentPacks/RedAlert/Shared/yaml/weapons.yaml:1443` · `weapons/weapons.yaml:12032` |
| `SardDeath` | `ContentPacks/D2k/Ordos/yaml/weapons.yaml:2624` · `weapons/d2k.yaml:1036` |
| `Sound` | `ContentPacks/D2k/Atreides/yaml/weapons.yaml:15` · `weapons/d2k.yaml:649` |
| `Sound2` | `ContentPacks/D2k/Ordos/yaml/weapons.yaml:2598` · `ContentPacks/D2k/Atreides/yaml/weapons.yaml:63` · `weapons/d2k.yaml:696` |
| `WormSwallow` | `ContentPacks/D2k/Ordos/yaml/weapons.yaml:2613` · `weapons/d2k.yaml:786` |
| `^D2K155mmLegacy` | `ContentPacks/D2k/Ordos/yaml/weapons.yaml:2440` · `weapons/d2k.yaml:154` |
| `^OCannon` | `ContentPacks/D2k/Ordos/yaml/weapons.yaml:2974` · `weapons/d2k.yaml:1816` |
| `d2k25mm` | `ContentPacks/D2k/Ordos/yaml/weapons.yaml:2669` · `weapons/d2k.yaml:1228` |
| `d2kFlameTurret` | `ContentPacks/D2k/Ordos/yaml/weapons.yaml:2808` · `weapons/d2k.yaml:1440` |
| `d2k_APCo_AA` | `ContentPacks/D2k/Ordos/yaml/weapons.yaml:2553` · `weapons/d2k.yaml:546` |
| `d2k_APCo_AG` | `ContentPacks/D2k/Ordos/yaml/weapons.yaml:2544` · `weapons/d2k.yaml:538` |
| `d2k_aircraft_eater` | `ContentPacks/D2k/Ordos/yaml/weapons.yaml:2906` · `weapons/d2k.yaml:1748` |
| `d2k_airdefenseplatform` | `ContentPacks/D2k/Ordos/yaml/weapons.yaml:2853` · `weapons/d2k.yaml:1725` |
| `d2k_laser_qafza` | `ContentPacks/D2k/Ordos/yaml/weapons.yaml:2817` · `weapons/d2k.yaml:1652` |
| `d2k_laser_qafza_aa` | `ContentPacks/D2k/Ordos/yaml/weapons.yaml:2834` · `weapons/d2k.yaml:1669` |
| `d2k_sard_crossbow` | `ContentPacks/D2k/Ordos/yaml/weapons.yaml:2946` · `weapons/d2k.yaml:1788` |
| `d2k_sard_heatblade` | `ContentPacks/D2k/Ordos/yaml/weapons.yaml:2925` · `weapons/d2k.yaml:1767` |
| `d2k_sardaukar_elite` | `ContentPacks/D2k/Ordos/yaml/weapons.yaml:2687` · `weapons/d2k.yaml:1372` |
| `d2k_tyrant` | `ContentPacks/D2k/Ordos/yaml/weapons.yaml:2482` · `weapons/d2k.yaml:499` |
| `emperor_sardaukar_chief_c4` | `ContentPacks/D2k/Ordos/yaml/weapons.yaml:2803` · `weapons/d2k.yaml:1420` |
| `mtank_pri` | `ContentPacks/D2k/Shared/yaml/weapons.yaml:548` · `weapons/d2k.yaml:481` |


## S2 — defined twice within the same tier (5)

| weapon | defined at |
|---|---|
| `Flamethrower` | `weapons/tiberiandawn.yaml:75` · `weapons/starcraft.yaml:1` |
| `ZClaw3` | `weapons/tiberiansun.yaml:1300` · `weapons/tiberiansun.yaml:1945` |
| `ra1_allies_alliedrocketsoldier_rocketsracryo` | `ContentPacks/RedAlert/Shared/yaml/weapons.yaml:210` · `ContentPacks/RedAlert/Allies/yaml/weapons.yaml:946` |
| `ra1_allies_rifleinfantry_carbine` | `ContentPacks/RedAlert/Shared/yaml/weapons.yaml:2428` · `ContentPacks/RedAlert/Allies/yaml/weapons.yaml:951` |
| `ra1_allies_rifleinfantry_carbine_cryo` | `ContentPacks/RedAlert/Shared/yaml/weapons.yaml:2445` · `ContentPacks/RedAlert/Allies/yaml/weapons.yaml:956` |


**FAIL** — S1 22/56, S2 5/2. A new split definition landed. Delete the duplicate rather than editing both copies.
