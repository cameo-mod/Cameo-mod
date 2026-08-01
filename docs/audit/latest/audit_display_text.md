# audit_display_text — internal ids leaked into UI prose

Active findings: **1**; dormant findings: **1**; technical references: **1**; comments for inspection: **0**


## D1 — active display text containing actor ids (1) — BLOCKING

| location | field | internal id | value |
|---|---|---|---|
| mods/cameo/fluent/rules/missing_keys_en.ftl:54 | FluentValue | ra1_soviets_attackdog | ra1_soviets_attackdog |


## D2 — dormant display text containing actor ids (1)

| location | field | internal id | value |
|---|---|---|---|
| mods/cameo/rules/classicdoom.yaml:867 | Name | ra1_soviets_submarine | ra1_soviets_submarine |


## D0 — technical references in display-named fields (1) — INFORMATIONAL

| location | field | internal id | value |
|---|---|---|---|
| mods/cameo/ContentPacks/D2k/Ixian/yaml/upgrades.yaml:124 | Description | ixian_ixresearchcenter | upgrade_d2k_advanced_ixian_technology.description, ~ixian_ixresearchcenter |

