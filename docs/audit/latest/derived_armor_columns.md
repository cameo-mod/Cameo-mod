# audit_derived_armor_columns — DESIGN §12.0l derived rows in every Versus table

missing or wrong derived rows: **135** (ratchet 0)

| file | rows |
|---|--:|
| `mods/cameo/ContentPacks/RedAlert2/Shared/yaml/weapons.yaml` | 135 |

Fix: `python tools/balance/derive_versus_columns.py --files <file> --write`

**FAIL** — 135 > ratchet 0: a new table was written without the derived rows. Run the tool on it; never raise the ratchet.
