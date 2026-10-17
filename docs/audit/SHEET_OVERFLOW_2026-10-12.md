# Resource sheet overflow qualification

The supplied crash is historical and its fix is already in the current engine pin. No new engine pin, sampler-limit increase, resource packing change, or gameplay change is needed for this evidence.

## Evidence and cause

`%APPDATA%/OpenRA/Logs/exception-2026-10-08T183707Z.log` (SHA-256 `95c9e129b0f1a3ae68ddf5a64bdcd601b39438f9b6f2011511b50b66663fcb83`) records engine `0a3f77dbe1a90bc59ff3ef5ad81534e8fbcf7695`, map UID `4d1a1408d070078828d89a7e7c5a5ad11ee7c603`, Imminent Destruction. The failing caller is `ResourceRenderer.UpdateSpriteLayers` line 203: **resource spriteLayer.Update**, not the preceding resource-shadow update or the map's separate terrain layer.

The repository archive `mods/cameo/maps/_ra_imminent-destruction.oramap` reproduces that exact map UID using the engine's package-order SHA-1 over map.png/map.yaml/map.bin. It uses RA_TEMPERAT, 130×130 cells, binary format 2. Resource index counts are: empty 15,159; 1:330; 2:189; 3:712; 4:332; 5:146; 6:32. These six authored resource types map to Tiberium, BlueTiberium, Ore, Gems, RedTiberium, and GoldTiberium in the current mounted rules.

The old layer retained one fixed array of eight sheet references. The ninth distinct primary or secondary sheet registered across its lifetime threw `Sheet overflow`. This is a texture-atlas identity limit, **not a resource-type count limit**. Multiple resource types may share sheets; variants/frames can occupy multiple sheets. Current mounted ResourceRenderer has 32 types (its editor list has 14); neither declaration count proves how many sheets any map renders. The crash does not record the ninth sheet's identity, so exact offending atlas/frame reconstruction is unavailable from this log alone.

The confirmed affected map/tileset is Imminent Destruction / RA_TEMPERAT. The old failure mechanism also applies to any tileset/map whose one layer accumulates more than eight distinct primary/secondary sheet references, including resources introduced during play. It is not confined to RA_TEMPERAT. Terrain and resource layers use the same class but maintain separate sheet sets: their counts must not be added together. There is no terrain-layer failure in this supplied trace.

## Existing fix and regression

Engine commit `3c92c0ea0f054c8c3abed409e8573a68f366dd46` (2026-10-08 22:17+02:00, after this crash) partitions cells into multiple sheet-compatible batches. `ed6a34e4ef` adds four batch regressions. Both are ancestors of current pin `4f69df005fe0d893526890fcb5bbc42e442d6fd6`. Each draw remains within the eight-sampler GPU/vertex-format limit; simply raising the constant would not respect that format.

`TerrainSheetOverflowRegressionTest` invokes the **actual pinned FindBatch selector**, with an asset-free map/world-renderer shell and a recording-free stub vertex-buffer factory. Fourteen distinct primary sheets safely select two batches; fourteen distinct primary/secondary pairs select four. Each batch stays at or below eight sheets, and repeated frame references reuse their original batch without growing the batch list. The test restores Game.Renderer in finally and is non-parallelizable. It checks allocation/selection/admission, not GPU drawing, lighting, clipping or screenshot parity.

Validation: existing engine batch tests **4/4 PASS**; new pinned-layer regression **2/2 PASS**. No game launch. The installed DevBuild OpenRA.Game.dll contains FindBatch/NewSheetCount/batchForCell markers, which supports presence of the fix but is not exact-source provenance proof. No live crash on that newer build has been provided.

## Separate notification cache observation

Current engine TextNotificationsManager appends enabled notifications to its static NotificationsCache without a per-match bound, and Clear empties it. This remains a separate retention issue; limiting it requires deciding which notification history the UI must preserve. No cache change or new engine request is bundled here because sheet batching is already pinned. This document does not claim that cache retention has been fixed.
