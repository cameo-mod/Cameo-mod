# audit_armor_upgrade_harm — the armor-plating layer's invariants

Checked **94** generated `^Warhead_*` templates against **5** platings (skipping `^Warhead_Nuclear_Super`, `^Warhead_Sniper_Light`, which the generator does not emit).

## I1 — no gaps (a missing row makes `DamageVersus` return 100)

_clean_ — every template carries a row for every plating.

## I2 — the column law (every plating averages 70 across all templates)

| plating | mean | min | max |
|---|--:|--:|--:|
| `HAZMAT` | **70.17** | 35 | 105 |
| `COMPOSITE` | **69.97** | 36 | 107 |
| `BLAST` | **69.94** | 35 | 104 |
| `REFLECTOR` | **70.16** | 35 | 105 |
| `ARMOR` | **70.00** | 70 | 70 |

## I3 — closure (every family has a counter and an exposure)

_clean_ — every family is countered by at least one plating and beats at least one.

