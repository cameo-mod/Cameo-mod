# Soviet, dog and TD naval naming repair

## Scope and player impact

This is the bounded naming migration requested by Aedis on 10 September at
03:03–03:22 Jakarta, based on upstream `50b7d001`. It is not a whole-roster
rename, a balance pass, or skirmish-AI development.

- Restore 31 Soviet roots plus the Mammoth color-picker variant, and rename
  the two RA2 dogs: 34 identities. The three dogs end in `_dog`.
- Rename eight TD naval actors to the `td_gdi_` / `td_nod_` scheme.
- Split concrete `DogJaw` into abstract `^DogJaw` and three actor-owned `_bite`
  weapons. All resolved payloads remain identical; no arbitrary stat differences.
- Update actor references, conditions, prerequisites, translations, map placements
  and scripts, AI lists, current tooling and generated ledgers. Dormant files with
  references are updated too; this is not claimed to be active-files-only.

No cost, HP, speed, damage, reload or weapon-range rebalance is intended. Actor IDs
are a compatibility boundary: external maps and old replays naming the old IDs
may require migration. This PR does not promise backward-compatible aliases.

## Root cause and asset preservation

The earlier tooltip-derived generator combined the faction `soviets` with the
adjective `Soviet`, duplicating it in actor IDs. The generator already strips that
adjective; this repairs the remaining definitions and references. The old
`actor_dog.name` Fluent key is valid in the active localization file, not missing.
The three dog `GenericName` literals are corrected to `Attack Dog`.

Actor IDs and sprite namespaces are distinct. Existing sequence and asset names
are preserved, including explicit old-ID `RenderSprites.Image` bindings and the
miner's `ImageByFullness`. No sprite files or sequence keys are renamed.

## Evidence

- Required captured resolved fixtures verify all 42 renamed identities. Asset
  fields are not normalized as actor IDs; unknown scalar fields stay unchanged.
  Collision checks and negative image-substitution tests guard false equivalence.
- 33 focused naming, dog-ownership and shellmap-reference tests passed.
- Complete weapon comparison: 2896 before / 2899 after; only `DogJaw` removed,
  abstract base plus three bites added, every other resolved weapon unchanged.
  All four new definitions resolve exactly to the original `DogJaw` payload.
- Seven edited map archives retain member order, metadata and unrelated bytes;
  coordinated Soviet/naval substitutions are combined in the shared lake map.
- All 33 ledgers and derived sidecars regenerated; recheck reports zero drift.
  The first full run exposed three stale-ledger failures; regeneration cleared
  all three without weakening their assertions. Remaining baseline failures are
  reported separately in the final validation checkpoint.
- 90-second menu boot passed at 04:45 Jakarta with no new exception logs and
  fresh `MenuPostProcessEffect.PostWorldLoaded` evidence. Peak sampled system RAM
  81.64%; owned game process closed. Existing engine build used read-only through
  the worktree junction; no build or engine mutation performed here.

Boot proof does not replace map-reference validation or prove every map playable.
Independent review challenged typed references, asset preservation, collision
handling and regression coverage; identified issues were corrected.

## Remaining work

Final full Python run: 1337 tests, 14 failures, 8 errors, 45 skips. All 22
failure/error signatures match the earlier upstream/core baseline; peak sampled
system RAM 80.94%, safety guard not triggered. This is not an all-green suite.

The release-drift audit intentionally remains red at D4: 336 unmatched old names
versus the lower-only ratchet of 335. The additional name is `DogJaw`, whose three
replacement payloads were compared exactly above. The raw count and ratchet are
not concealed or raised. A future rename-aware release comparison should retain
both provenance and raw unmatched counts; this draft does not claim that gate
passes merely because the rename is intentional.

Canonical `run_all.sh` completed with eight failing categories: inherits,
basebuilder_crates, buildable_order, packs, split_definitions, release_drift,
doc_claims and doc_health. The release-drift addition is explained above;
upgrade and nuclear-flash reference gates now pass. Map audit: 363 maps,
184162 placements, zero dangling actors and zero unreadable archives. Percentage
runtime, K-linearity, generator sync, weapon_shape, three_way_split and naming
ratchets pass. `split_definitions` remains a separate failing gate.

Whole-roster actor/weapon ownership migration and broader W24 changes remain
separate. Naval IDs now carry canonical faction prefixes; downstream reference
routing and CA/DTA naval assignments remain to be validated. Generated
diagnostics are evidence, not approval to apply balance targets.

Implementation: OpenCode GLM 5.3 Flash. Planning, review corrections and
independent review: Codex. Draft publication authorized by Blackrobe; no merge.

## Actor-owned weapon follow-up (10 September)

13 weapons now carry their complete owner prefix: BTR80 (six), Flak Truck (two)
and Monster Tank (five). Upgrade descriptors and the dual-weapon `_AA` suffix
follow DESIGN. The reviewed map was applied through `safe_rename.py`; only the
active Soviet vehicles/weapons files changed, with 33 identity replacements.
No archive, loose map, asset, audio namespace or game-stat value was changed.

Before/after census: 2,899 resolved weapons both times; 13 old IDs replaced by
13 new IDs; every unrenamed weapon payload is identical. The 13 renamed payloads
and all traits on their three owners are fixture-checked, allowing only the exact
Armament weapon-reference changes. A complete pre-migration namespace fixture
checks destination collisions. Independent review found no gameplay/reference blocker.

Current converter inventories and routing/source-key tests now use the new names.
Historical damage comparison JSON and hashes are unchanged; their set checks translate
only the exact reviewed identity mapping. Fresh ledgers change weapon IDs only.
The initial full run used stale ledgers and exposed those dependencies; corrected
focused checks pass. The frozen-snapshot rerun completed: 1,341 tests, 14 failures,
8 errors and 45 skips. Failure/error signatures exactly match the prior published
naming baseline; sampled system memory peaked at 79.84%, without a guard stop.

Raw release-drift D4 rises from 336 to 343 because seven renamed identifiers existed
under their old names in the release baseline. The threshold remains 335 and the gate
remains failing. This is not hidden or counted as new damage drift: D1/D2/D3 are unchanged.
This batch does not complete whole-roster naming or change the shared-heaviness pilot.
Implementation and follow-up review: Codex; OpenCode is suspended at Blackrobe's request.

The combined runtime, including these names, passed a fresh 90-second menu observation
starting 06:29:54 Jakarta: no new exception logs, fresh menu-load proof, peak memory
79.62%, test process closed. No build or engine pin change was made; the isolated
integration tree reused PR341's verified binary. No further matchup claim follows.
Canonical audits completed with the same eight failing categories as this PR's prior
run; no empty final Markdown report. D4's additional name-only debt remains explicit.
