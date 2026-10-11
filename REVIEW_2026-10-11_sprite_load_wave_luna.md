# Exact-SHA review: sprite load waves

**Commit:** `devin/sprite-load-wave@d4d3c78e9e0a4c51188d3dcb7827bfb34ce8da57`
**Parent:** `6da7fce14da541180c6baddd6925118fbef65b94`
**Verdict:** PASS, bounded to render/load behavior; no simulation or sync change found.

The commit changes only `OpenRA.Game/Graphics/SpriteCache.cs`. `LoadReservations` replaces all-at-once parallel sprite decoding with deterministic batches of 256 filenames. For each batch it decodes, resolves reservations, sorts pending frames by height as before, packs them through the same `SheetBuilder.Add` and sprite-cache keys, then advances to the next batch. Missing-file fallback, frame-range validation, frame adjustment, premultiplication, cache-hit behavior, and final `ReleaseBuffer` remain present. `BeginNewSession` still runs once before the first wave with pending sprites. No world, order, RNG, tick, replay, or sync-hash code is modified or called by the new batching logic.

Packing is now height-sorted within each wave rather than globally across all reserved files. This can change atlas layout and the number of resident sheets, which is the intended rendering/resource tradeoff; it does not change the resolved sprite content or simulation state. The commit message reports equal packed sprite counts (391,710), 191 to 193 resident sheets, no new exceptions, and a measured private-memory peak reduction from 8.37 to 6.29 GiB. I did not independently reproduce these measurements or run a build/game.

The batch bound is by file count rather than decoded byte size; exceptionally large individual sprite files are not capped by this constant. This is a limitation of the memory bound, not a correctness defect for the reviewed asset set and measured corpus.

**Scope/evidence:** exact commit object inspected; parent is the pinned engine revision; single-file diff confirmed. No tests were added in this commit. No engine build, test suite, game, or boot was run as part of this review. Runtime A5 parity on the bumped binary remains a separate required gate.
