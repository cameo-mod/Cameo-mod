# Elite weapon gating audit (E2)

Armament@*ELITE* blocks without RequiresCondition: rank-elite: **21**

| File | Line | Actor | Trait | Issue |
|---|---|---|---|---|
| ContentPacks/RedAlert2/Allies/yaml/defenses.yaml | 313 | ra2_allies_patriotmissilesystem | Armament@missileeliteThunderbolt | RequiresCondition but NOT rank-elite |
| ContentPacks/TiberianDawn/Nod/yaml/aircraft.yaml | 202 | td_nod_venom | Armament@Elite | RequiresCondition but NOT rank-elite |
| ContentPacks/TiberianDawn/Nod/yaml/buildings.yaml | 485 | td_nod_laserturret | Armament@Elite | RequiresCondition but NOT rank-elite |
| ContentPacks/TiberianDawn/Nod/yaml/buildings.yaml | 591 | td_nod_obeliskoflight | Armament@Elite | RequiresCondition but NOT rank-elite |
| ContentPacks/TiberianDawn/Nod/yaml/vehicles.yaml | 787 | td_nod_buggymkii | Armament@LaserElite | RequiresCondition but NOT rank-elite |
| ContentPacks/TiberianDawn/Nod/yaml/vehicles.yaml | 808 | td_nod_buggymkii | Armament@LaserAAElite | RequiresCondition but NOT rank-elite |
| rules/generals.yaml | 3956 | glmaura | Armament@NormalElite | RequiresCondition but NOT rank-elite |
| rules/generals.yaml | 3963 | glmaura | Armament@ToxinElite | RequiresCondition but NOT rank-elite |
| rules/generals.yaml | 3970 | glmaura | Armament@ToxinBetaElite | RequiresCondition but NOT rank-elite |
| rules/shockwave.yaml | 9363 | glsmaura | Armament@NormalElite | RequiresCondition but NOT rank-elite |
| rules/shockwave.yaml | 9370 | glsmaura | Armament@ToxinElite | RequiresCondition but NOT rank-elite |
| rules/shockwave.yaml | 9377 | glsmaura | Armament@ToxinBetaElite | RequiresCondition but NOT rank-elite |
| rules/shockwave.yaml | 9384 | glsmaura | Armament@NormalElite2 | RequiresCondition but NOT rank-elite |
| rules/shockwave.yaml | 9391 | glsmaura | Armament@ToxinElite2 | RequiresCondition but NOT rank-elite |
| rules/shockwave.yaml | 9398 | glsmaura | Armament@ToxinBetaElite2 | RequiresCondition but NOT rank-elite |
| rules/shockwave.yaml | 10530 | sgldemomaura | Armament@NormalElite | no RequiresCondition |
| rules/shockwave.yaml | 10532 | sgldemomaura | Armament@ToxinElite | no RequiresCondition |
| rules/shockwave.yaml | 10534 | sgldemomaura | Armament@ToxinBetaElite | no RequiresCondition |
| rules/shockwave.yaml | 12049 | eglpickuptank | Armament@NormalElite | RequiresCondition but NOT rank-elite |
| rules/shockwave.yaml | 12056 | eglpickuptank | Armament@ToxinElite | RequiresCondition but NOT rank-elite |
| rules/shockwave.yaml | 12063 | eglpickuptank | Armament@ToxinBetaElite | RequiresCondition but NOT rank-elite |
