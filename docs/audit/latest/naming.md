# gen_rename_maps — §9.1 naming compliance (RA1-Soviet baseline)


## Actor-id compliance per faction (faction-exclusive buildables)

| faction | compliant | % | proposal collisions | asset files to rename |
|---|---|---|---|---|
| asianalliance | 71/71 | 100% | 0 | 112 |
| cabal | 80/80 | 100% | 0 | 147 |
| eden | 43/43 | 100% | 0 | 2 |
| forgotten | 78/78 | 100% | 0 | 49 |
| futuretech | 56/56 | 100% | 0 | 36 |
| harkonnen | 0/0 | — | 0 | 0 |
| ixian | 0/0 | — | 0 | 0 |
| japan | 68/68 | 100% | 0 | 40 |
| latinsyndicate | 65/65 | 100% | 0 | 44 |
| naxis | 73/73 | 100% | 0 | 31 |
| ordos | 70/70 | 100% | 0 | 31 |
| plymouth | 44/44 | 100% | 0 | 2 |
| protoss | 72/72 | 100% | 0 | 46 |
| ra1_allies | 0/62 | 0% | 0 | 123 |
| ra1_soviets | 0/106 | 0% | 0 | 181 |
| ra2_allies | 0/66 | 0% | 0 | 233 |
| ra2_soviets | 0/56 | 0% | 0 | 141 |
| schwarzermond | 59/59 | 100% | 0 | 24 |
| steelconsortium | 60/60 | 100% | 0 | 33 |
| td_gdi | 0/60 | 0% | 0 | 106 |
| td_nod | 0/64 | 0% | 0 | 116 |
| terran | 77/77 | 100% | 0 | 58 |
| tkm | 72/72 | 100% | 0 | 39 |
| ts_gdi | 0/62 | 0% | 0 | 142 |
| ts_nod | 0/46 | 0% | 0 | 127 |
| wc2_humans | 69/69 | 100% | 0 | 15 |
| wc2_orcs | 60/60 | 100% | 0 | 12 |
| yuri | 64/64 | 100% | 0 | 7 |
| zerg | 75/75 | 100% | 0 | 60 |


## Icon filename compliance (_icon suffix rule)

| faction | icons compliant | % |
|---|---|---|
| asianalliance | 70/70 | 100% |
| cabal | 80/80 | 100% |
| eden | 43/43 | 100% |
| forgotten | 76/78 | 97% |
| futuretech | 56/56 | 100% |
| harkonnen | 0/0 | — |
| ixian | 0/0 | — |
| japan | 64/67 | 95% |
| latinsyndicate | 65/65 | 100% |
| naxis | 73/73 | 100% |
| ordos | 48/48 | 100% |
| plymouth | 44/44 | 100% |
| protoss | 72/72 | 100% |
| ra1_allies | 61/61 | 100% |
| ra1_soviets | 105/105 | 100% |
| ra2_allies | 64/64 | 100% |
| ra2_soviets | 54/55 | 98% |
| schwarzermond | 59/59 | 100% |
| steelconsortium | 59/60 | 98% |
| td_gdi | 60/60 | 100% |
| td_nod | 64/64 | 100% |
| terran | 77/77 | 100% |
| tkm | 72/72 | 100% |
| ts_gdi | 62/62 | 100% |
| ts_nod | 46/46 | 100% |
| wc2_humans | 15/16 | 93% |
| wc2_orcs | 6/6 | 100% |
| yuri | 63/64 | 98% |
| zerg | 75/75 | 100% |


_Ownership is data-driven: an actor counts for a faction only if no other faction's prerequisite closure can build it. Sequence filenames referenced by more than 3 images are treated as shared archives and exempted. Rename proposals written to tools/rename/rename_map_<faction>.yaml (actors: + files: sections); collisions need manual `_variant` suffixes before applying._

