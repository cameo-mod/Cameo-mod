# Split definitions — one weapon, two live files, one silent merge

Live weapon files in the manifest: **42** · names defined more than once: **8**

The engine MERGES same-named top-level nodes across files. Editing one copy leaves the other supplying its own fields, so a removal can silently do nothing — see the `HMG` incident in this file's docstring.

| bucket | count | baseline |
|---|--:|--:|
| S1 legacy global + ContentPack | 2 | 56 |
| S2 same tier twice | 6 | 2 |


## S1 — defined in a legacy global AND a ContentPack (2)

ContentPack-migration residue. **Fix by deleting the LEGACY copy** once the pack copy is complete — never by editing both, which is how the two drift apart. ⚠ Check `mod.yaml` load order before deleting: if the global loads LATER it is the one whose fields win today, so a naive delete changes behaviour. Diff the resolved weapon before and after with `tools/audit/review_resolve_diff.py`.

| weapon | defined at |
|---|---|
| `ChemTibAtomic` | `ContentPacks/RedAlert/Shared/yaml/weapons.yaml:1311` · `weapons/tiberiandawn.yaml:231` |
| `RocketsG` | `ContentPacks/RedAlert/Shared/yaml/weapons.yaml:1444` · `ContentPacks/RedAlert/Shared/yaml/weapons.yaml:1448` · `weapons/weapons.yaml:12020` |


## S2 — defined twice within the same tier (6)

| weapon | defined at |
|---|---|
| `Flamethrower` | `weapons/tiberiandawn.yaml:75` · `weapons/starcraft.yaml:1` |
| `Sound2` | `ContentPacks/D2k/Ordos/yaml/weapons.yaml:3141` · `ContentPacks/D2k/Atreides/yaml/weapons.yaml:15` |
| `ZClaw3` | `weapons/tiberiansun.yaml:1294` · `weapons/tiberiansun.yaml:1939` |
| `ra1_allies_alliedrocketsoldier_rocketsracryo` | `ContentPacks/RedAlert/Shared/yaml/weapons.yaml:210` · `ContentPacks/RedAlert/Allies/yaml/weapons.yaml:936` |
| `ra1_allies_rifleinfantry_carbine` | `ContentPacks/RedAlert/Shared/yaml/weapons.yaml:2427` · `ContentPacks/RedAlert/Allies/yaml/weapons.yaml:941` |
| `ra1_allies_rifleinfantry_carbine_cryo` | `ContentPacks/RedAlert/Shared/yaml/weapons.yaml:2444` · `ContentPacks/RedAlert/Allies/yaml/weapons.yaml:946` |


**FAIL** — S1 2/56, S2 6/2. A new split definition landed. Delete the duplicate rather than editing both copies.
