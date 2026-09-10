# audit_release_drift - measured against the build players played


baseline: **playtest-20260709** (`8c238ffc3`), 1912 weapons · 1506 shared with the tree · **1332 unchanged**

| code | check | count | ratchet |  |
|---|---|---|---|---|
| D1 | INFLATED - deals more than it shipped | 115 | 133 | PASS |
| D2 | WEAKENED - deals less than it shipped | 59 | 62 | PASS |
| D3 | EXTREME - 3x or worse, either way | 17 | 27 | PASS |
| D4 | UNMATCHED - in the release, gone under that name | 406 | 335 | FAIL |
| D5 | ACCEPTED value edit (informational) | 34 | 43 | PASS |


## D3 EXTREME - 17 weapon(s) a player will feel

| weapon | shipped | now | x | mains |
|---|---|---|---|---|
| AsianTSIonCannon | 30000 | 230000 | 7.67 | 3 -> 4 |
| TSIonCannon | 38000 | 238000 | 6.26 | 3 -> 4 |
| MadcapGun | 6000 | 36000 | 6.00 | 3 -> 1 |
| MarineMG | 6000 | 36000 | 6.00 | 3 -> 1 |
| NaxiAlienPistol | 4000 | 24000 | 6.00 | 2 -> 1 |
| DragunovSniper | 40000 | 200000 | 5.00 | 5 -> 1 |
| KamovTeslaArcFragment2 | 8000 | 40000 | 5.00 | 2 -> 1 |
| YakTeslaArcFragment2 | 4000 | 20000 | 5.00 | 2 -> 1 |
| RA2GattlingInf | 4000 | 16000 | 4.00 | 2 -> 1 |
| elitecadregun | 4000 | 16000 | 4.00 | 2 -> 1 |
| KamovTeslaArcFragment1 | 16000 | 48000 | 3.00 | 2 -> 1 |
| MissileAttackRobotGun | 8000 | 24000 | 3.00 | 2 -> 1 |
| YakTeslaArcFragment1 | 8000 | 24000 | 3.00 | 2 -> 1 |
| RA2ThunderboltMissile | 12000 | 4000 | 0.33 | 6 -> 1 |
| RA2ThunderboltMissile_elite | 12000 | 4000 | 0.33 | 6 -> 1 |
| RA2MultiThunderboltMissile | 16000 | 4000 | 0.25 | 8 -> 1 |
| RA2MultiThunderboltMissile_elite | 16000 | 4000 | 0.25 | 8 -> 1 |



**FAIL: D4 above ratchet.** A rise means a weapon moved FURTHER from the shipped build, or that the gate went BLIND to more of them. Lower a baseline as the repair lands; never raise one.

