# gen_rename_maps — §9.1 naming compliance (RA1-Soviet baseline)


## Actor-id compliance per faction (faction-exclusive buildables)

| faction | compliant | % | proposal collisions | asset files to rename |
|---|---|---|---|---|
| allies | 0/62 | 0% | 0 | 124 |
| asianalliance | 0/72 | 0% | 0 | 242 |
| cabal | 0/57 | 0% | 0 | 136 |
| consortium | 0/61 | 0% | 0 | 145 |
| edenl | 40/43 | 93% | 0 | 59 |
| forgotten | 0/76 | 0% | 0 | 131 |
| futuretech | 0/57 | 0% | 0 | 134 |
| gdi | 2/60 | 3% | 0 | 101 |
| human2 | 0/69 | 0% | 0 | 57 |
| ixian | 0/57 | 0% | 0 | 59 |
| lnaxis | 0/41 | 0% | 0 | 81 |
| modjapan | 1/69 | 1% | 0 | 106 |
| naxis | 0/73 | 0% | 0 | 118 |
| nod | 1/64 | 1% | 0 | 113 |
| orc2 | 0/60 | 0% | 0 | 40 |
| ordos | 0/65 | 0% | 0 | 57 |
| plymouthl | 44/44 | 100% | 0 | 58 |
| protoss | 0/72 | 0% | 0 | 116 |
| ra2america | 18/66 | 27% | 0 | 217 |
| ra2russia | 0/56 | 0% | 0 | 141 |
| soviet | 0/104 | 0% | 0 | 177 |
| syndicate | 0/65 | 0% | 0 | 153 |
| terran | 0/77 | 0% | 0 | 136 |
| tkm | 0/72 | 0% | 0 | 119 |
| tsgdi | 0/62 | 0% | 0 | 140 |
| tsnod | 0/46 | 0% | 0 | 128 |
| yuri | 0/64 | 0% | 0 | 137 |
| zerg | 0/74 | 0% | 0 | 142 |


## Icon filename compliance (_icon suffix rule)

| faction | icons compliant | % |
|---|---|---|
| allies | 0/61 | 0% |
| asianalliance | 32/71 | 45% |
| cabal | 10/57 | 17% |
| consortium | 59/61 | 96% |
| edenl | 0/43 | 0% |
| forgotten | 53/76 | 69% |
| futuretech | 9/57 | 15% |
| gdi | 7/60 | 11% |
| human2 | 14/16 | 87% |
| ixian | 19/41 | 46% |
| lnaxis | 34/41 | 82% |
| modjapan | 3/68 | 4% |
| naxis | 63/72 | 87% |
| nod | 4/64 | 6% |
| orc2 | 1/6 | 16% |
| ordos | 31/45 | 68% |
| plymouthl | 0/44 | 0% |
| protoss | 1/72 | 1% |
| ra2america | 15/64 | 23% |
| ra2russia | 15/54 | 27% |
| soviet | 1/103 | 0% |
| syndicate | 34/65 | 52% |
| terran | 1/77 | 1% |
| tkm | 0/72 | 0% |
| tsgdi | 0/61 | 0% |
| tsnod | 0/46 | 0% |
| yuri | 30/64 | 46% |
| zerg | 0/74 | 0% |


_Ownership is data-driven: an actor counts for a faction only if no other faction's prerequisite closure can build it. Sequence filenames referenced by more than 3 images are treated as shared archives and exempted. Rename proposals written to tools/rename/rename_map_<faction>.yaml (actors: + files: sections); collisions need manual `_variant` suffixes before applying._

