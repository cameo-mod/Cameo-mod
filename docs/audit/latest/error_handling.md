# audit_error_handling — Python tooling error handling

Files scanned: **201**

| code | meaning | count | baseline |
|---|---|---|---|
| E1 | bare except / except BaseException | 2 | 2 |
| E2 | handler discards the error | 31 | 30 |
| E3 | open() without encoding= | 90 | 90 |
| E4 | subprocess call without check= | 9 | 9 |


## E1 — 2 finding(s)

| file | line | detail |
|---|---|---|
| tools/audit_ce_image_usage.py | 29 | bare `except:` |
| tools/audit_createeffect_image.py | 19 | bare `except:` |


## E2 — 31 finding(s)

| file | line | detail |
|---|---|---|
| tools/audit/audit_ai.py | 45 | handler body discards the error |
| tools/audit/audit_balance_sheet.py | 136 | handler body discards the error |
| tools/audit/audit_dune_rank_decoration.py | 15 | handler body discards the error |
| tools/audit/audit_elite_gating.py | 16 | handler body discards the error |
| tools/audit/audit_garrison_weapons.py | 61 | handler body discards the error |
| tools/audit/audit_missing_elite.py | 21 | handler body discards the error |
| tools/audit/audit_orphans.py | 93 | handler body discards the error |
| tools/audit/audit_power_budget.py | 100 | handler body discards the error |
| tools/audit/audit_rank_decoration.py | 49 | handler body discards the error |
| tools/audit/audit_rank_decoration.py | 68 | handler body discards the error |
| tools/audit/audit_upgrades.py | 145 | handler body discards the error |
| tools/audit/audit_weapon_suffixes.py | 119 | handler body discards the error |
| tools/audit/audit_weapon_suffixes.py | 185 | handler body discards the error |
| tools/audit/audit_weapon_suffixes.py | 297 | handler body discards the error |
| tools/audit/audit_weapon_uniqueness.py | 99 | handler body discards the error |
| tools/audit/gen_damage_matrix.py | 52 | handler body discards the error |
| tools/audit/miniyaml.py | 187 | handler body discards the error |
| tools/audit/review_resolve_diff.py | 58 | handler body discards the error |
| tools/audit_ce_image_usage.py | 29 | handler body discards the error |
| tools/audit_createeffect_image.py | 19 | handler body discards the error |
| tools/balance/effective_damage.py | 137 | handler body discards the error |
| tools/balance/extract_stats.py | 172 | handler body discards the error |
| tools/balance/extract_stats.py | 544 | handler body discards the error |
| tools/balance/formula.py | 100 | handler body discards the error |
| tools/balance/propose_class_rebalance.py | 268 | handler body discards the error |
| tools/rename/apply.py | 178 | handler body discards the error |
| tools/rename/safe_rename.py | 128 | handler body discards the error |
| tools/rename/safe_rename.py | 137 | handler body discards the error |
| tools/rename/safe_rename.py | 287 | handler body discards the error |
| tools/subset_judou_font.py | 56 | handler body discards the error |
| tools/tilesets/generate_volcanic_tileset.py | 814 | handler body discards the error |


## E3 — 90 finding(s)

| file | line | detail |
|---|---|---|
| tools/bake_d2k_overlay.py | 28 | `Image.open()` without encoding= |
| tools/bake_d2k_overlay_zap.py | 10 | `Image.open()` without encoding= |
| tools/bake_d2k_zap.py | 41 | `Image.open()` without encoding= |
| tools/d2k_to_openra.py | 153 | `Image.open()` without encoding= |
| tools/d2k_to_openra.py | 164 | `Image.open()` without encoding= |
| tools/extract_insignias.py | 113 | `Image.open()` without encoding= |
| tools/hooks/bash_guard.py | 71 | `perf.read_text()` without encoding= |
| tools/hooks/exec_guard.py | 69 | `perf.read_text()` without encoding= |
| tools/make_syndicate_insignia.py | 41 | `Image.open()` without encoding= |
| tools/make_syndicate_insignia.py | 63 | `Image.open()` without encoding= |
| tools/tilesets/apply_ai_edge_correction.py | 34 | `Image.open()` without encoding= |
| tools/tilesets/apply_ai_edge_correction.py | 78 | `Image.open()` without encoding= |
| tools/tilesets/apply_dark_noise_cleanup.py | 79 | `Image.open()` without encoding= |
| tools/tilesets/build_basalt_forest_bulk_review.py | 55 | `Image.open()` without encoding= |
| tools/tilesets/build_basalt_tree_imagegen_review.py | 39 | `Image.open()` without encoding= |
| tools/tilesets/build_cliff_connectivity_review.py | 84 | `Image.open()` without encoding= |
| tools/tilesets/build_cliff_connectivity_review.py | 85 | `Image.open()` without encoding= |
| tools/tilesets/build_cliff_connectivity_review.py | 86 | `Image.open()` without encoding= |
| tools/tilesets/build_cliff_offset_review.py | 87 | `Image.open()` without encoding= |
| tools/tilesets/build_cliff_offset_review.py | 88 | `Image.open()` without encoding= |
| tools/tilesets/build_cliff_offset_review.py | 89 | `Image.open()` without encoding= |
| tools/tilesets/build_cliff_offset_review.py | 94 | `Image.open()` without encoding= |
| tools/tilesets/build_ra_temperate_basalt_trees.py | 273 | `Image.open()` without encoding= |
| tools/tilesets/build_shoreline_mass_review.py | 181 | `Image.open()` without encoding= |
| tools/tilesets/build_t08_basalt_study.py | 250 | `Image.open()` without encoding= |
| tools/tilesets/build_t10_t11_basalt.py | 152 | `Image.open()` without encoding= |
| tools/tilesets/build_t10_t11_basalt.py | 153 | `Image.open()` without encoding= |
| tools/tilesets/build_t10_t11_basalt.py | 154 | `Image.open()` without encoding= |
| tools/tilesets/build_t10_t11_basalt.py | 171 | `Image.open()` without encoding= |
| tools/tilesets/build_tc_basalt_from_gimp.py | 75 | `Image.open()` without encoding= |
| tools/tilesets/compare_cliff_topology.py | 74 | `Image.open()` without encoding= |
| tools/tilesets/convert_semantic_mask_sample.py | 45 | `Image.open()` without encoding= |
| tools/tilesets/convert_semantic_mask_sample.py | 46 | `Image.open()` without encoding= |
| tools/tilesets/detect_cliff_dark_noise.py | 74 | `Image.open()` without encoding= |
| tools/tilesets/export_temperate_mask_sources.py | 140 | `path.read_text()` without encoding= |
| tools/tilesets/export_temperate_mask_sources.py | 168 | `path.read_text()` without encoding= |
| tools/tilesets/export_temperate_mask_sources.py | 296 | `write_text()` without encoding= |
| tools/tilesets/export_temperate_mask_sources.py | 298 | `open()` without encoding= |
| tools/tilesets/export_temperate_mask_sources.py | 349 | `write_text()` without encoding= |
| tools/tilesets/generate_inland_lava_rivers.py | 160 | `Image.open()` without encoding= |
| tools/tilesets/generate_lava_river_donor_layer_v2.py | 104 | `Image.open()` without encoding= |
| tools/tilesets/generate_lava_river_donor_layer_v2.py | 150 | `Image.open()` without encoding= |
| tools/tilesets/generate_molten_pool_study.py | 125 | `Image.open()` without encoding= |
| tools/tilesets/generate_sh04_alpha_beach_prototype.py | 568 | `Image.open()` without encoding= |
| tools/tilesets/generate_volcanic_rock_craters.py | 181 | `Image.open()` without encoding= |
| tools/tilesets/generate_volcanic_rock_craters.py | 182 | `Image.open()` without encoding= |
| tools/tilesets/generate_volcanic_tileset.py | 102 | `BARREN_TILESET.read_text()` without encoding= |
| tools/tilesets/generate_volcanic_tileset.py | 112 | `VOLCANIC_TILESET.write_text()` without encoding= |
| tools/tilesets/generate_volcanic_tileset.py | 143 | `path.read_text()` without encoding= |
| tools/tilesets/import_basalt_glow_envelopes.py | 134 | `Image.open()` without encoding= |
| tools/tilesets/manual_river_delta/install_production_vol.py | 38 | `Image.open()` without encoding= |
| tools/tilesets/manual_river_delta/prepare_production.py | 24 | `Image.open()` without encoding= |
| tools/tilesets/manual_river_delta/prepare_production.py | 25 | `Image.open()` without encoding= |
| tools/tilesets/manual_river_delta/prepare_production.py | 26 | `Image.open()` without encoding= |
| tools/tilesets/mark_semantic_cliff_sample.py | 63 | `Image.open()` without encoding= |
| tools/tilesets/mark_semantic_cliff_sample.py | 64 | `Image.open()` without encoding= |
| tools/tilesets/migrate_clear_lava_dependents.py | 207 | `Image.open()` without encoding= |
| tools/tilesets/migrate_clear_lava_dependents.py | 208 | `Image.open()` without encoding= |
| tools/tilesets/migrate_clear_lava_dependents.py | 233 | `Image.open()` without encoding= |
| tools/tilesets/migrate_clear_lava_dependents.py | 234 | `Image.open()` without encoding= |
| tools/tilesets/package_ai_cliff_batch.py | 81 | `Image.open()` without encoding= |
| tools/tilesets/package_ai_cliff_batch.py | 99 | `Image.open()` without encoding= |
| tools/tilesets/package_ai_cliff_batch.py | 100 | `Image.open()` without encoding= |
| tools/tilesets/package_ai_cliff_batch.py | 220 | `Image.open()` without encoding= |
| tools/tilesets/package_ai_cliff_batch.py | 252 | `Image.open()` without encoding= |
| tools/tilesets/place_authored_basalt_columns_on_shores.py | 425 | `Image.open()` without encoding= |
| tools/tilesets/place_authored_basalt_columns_on_shores.py | 437 | `Image.open()` without encoding= |
| tools/tilesets/place_basalt_columns_on_shores.py | 91 | `Image.open()` without encoding= |
| tools/tilesets/preview_thin_basalt_glow_envelope.py | 100 | `Image.open()` without encoding= |
| tools/tilesets/preview_thin_basalt_glow_envelope.py | 102 | `Image.open()` without encoding= |
| tools/tilesets/preview_thin_basalt_glow_envelope.py | 104 | `Image.open()` without encoding= |
| tools/tilesets/preview_yaml_rock_basalt_columns.py | 141 | `Image.open()` without encoding= |
| tools/tilesets/preview_yaml_rock_basalt_columns.py | 211 | `Image.open()` without encoding= |
| tools/tilesets/process_ai_edge_mask.py | 30 | `Image.open()` without encoding= |
| tools/tilesets/process_ai_edge_mask.py | 89 | `Image.open()` without encoding= |
| tools/tilesets/propose_semantic_mask.py | 43 | `Image.open()` without encoding= |
| tools/tilesets/propose_semantic_mask.py | 44 | `Image.open()` without encoding= |
| tools/tilesets/propose_semantic_mask.py | 91 | `open()` without encoding= |
| tools/tilesets/prototype_donor_recolored_bridge_ground.py | 72 | `Image.open()` without encoding= |
| tools/tilesets/prototype_liquid_lava_shore_transition.py | 237 | `Image.open()` without encoding= |
| tools/tilesets/prototype_liquid_lava_shore_transition.py | 240 | `Image.open()` without encoding= |
| tools/tilesets/recolor_basalt_tree_to_reference.py | 64 | `Image.open()` without encoding= |
| tools/tilesets/recolor_basalt_tree_to_reference.py | 65 | `Image.open()` without encoding= |
| tools/tilesets/recolor_basalt_tree_to_reference.py | 66 | `Image.open()` without encoding= |
| tools/tilesets/recolor_cliff_luminance.py | 105 | `Image.open()` without encoding= |
| tools/tilesets/recolor_volcanic_basalt_family.py | 44 | `Image.open()` without encoding= |
| tools/tilesets/recolor_volcanic_basalt_family.py | 68 | `Image.open()` without encoding= |
| tools/tilesets/replace_volcanic_debris_with_basalt.py | 60 | `Image.open()` without encoding= |
| tools/tilesets/shptd.py | 101 | `open()` without encoding= |
| tools/tilesets/transfer_ai_cliff_style.py | 101 | `Image.open()` without encoding= |


## E4 — 9 finding(s)

| file | line | detail |
|---|---|---|
| tools/audit/run_all.py | 57 | `subprocess.run()` without check= |
| tools/audit/run_all.py | 78 | `subprocess.run()` without check= |
| tools/balance/apply_balance.py | 253 | `subprocess.run()` without check= |
| tools/balance/apply_balance.py | 255 | `subprocess.run()` without check= |
| tools/balance/run_with_guard.py | 39 | `subprocess.Popen()` without check= |
| tools/balance/splice_templates.py | 42 | `subprocess.run()` without check= |
| tools/balance/verify_generator_sync.py | 55 | `subprocess.run()` without check= |
| tools/hooks/bash_guard.py | 49 | `subprocess.run()` without check= |
| tools/hooks/exec_guard.py | 45 | `subprocess.run()` without check= |


## FAIL

- E2: 31 > baseline 30

