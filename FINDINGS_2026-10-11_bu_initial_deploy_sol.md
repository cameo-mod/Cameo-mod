# BU pilot initial deployment failure ? 2026-10-11

## Root cause and evidence

Campaign tooling `35b1dfc25f4f79076560dd05718a4e1268ec1add` builds an incomplete overlay mod: only three AI files plus a copied `mod.yaml`. The pinned engine's `Manifest` resolves top-level Include files directly from that mod package BEFORE its FileSystem mounts. Attempt3 `Support/Logs/debug.log` explicitly records rejection of the overlay because `ContentPacks/Outpost2/content.yaml` is missing. The launch still finds a valid baseline Cameo mod and proceeds.

There is a second independent selection bug. The runner supplies `Engine.ModSearchPaths=overlay/mods,root/mods,engine/mods`. `InstalledMods.GetInstalledMods` iterates those candidates and assigns `ret[id]=mod` for every valid manifest: LAST valid wins, not first. Even supplying the missing include files alone would allow the baseline to overwrite the overlay. Its ordinary AI catalogue has no `hard_control`/`hard_treatment` bot aliases or owner-condition bindings. The generated map still names those aliases. `Player` resolves bot logic by exact `IBotInfo.Type`; a missing type cannot activate its ModularBot. Owner conditions also omit both aliases, leaving the base builder disabled. The match rows show both alias seats with empty personality/composition timelines, 45001 ticks and the reported zero economy, consistent with this configuration failure.

The cap Lua crash is additional confirming evidence: `ResolvePlayerName` cannot find a matching bot info for the alias and throws `Sequence contains no matching element`. The first seat is marked lost during the cap handler before that exception; it is not a valid natural defeat. Preserve this cell as invalid/incomplete evidence, never as an A/B win/loss. Existing attempted cell artifacts are untouched.

Evidence directory: `C:/cameo-wt/ab-campaign-tooling/results/campaign-bu-pilot-20261011/1v1_nuclear_gdi_1337_0.attempt3_tooling_transientpid/Support/Logs`. Its debug log refers to the original cell Overlay path; the attempt's archived overlay has the same AI-only shape. No replay playback or game was launched in this investigation.

## Fix

`tools/ai/campaign_overlay.py` copies all original top-level manifest Include files byte-for-byte. The baseline `cameo` filesystem package mounts the pinned source mod directly, providing every unchanged content/rule/resource without copying the full asset tree. A separate `campaign_ai` package mounts only the generated overlay; all three declared AI rule entries explicitly use it. Both package keys use caret engine-relative paths because raw Windows drive colons are MiniYaml delimiters. The launch pins `Engine.EngineDir` to the supplied engine root, so those relative package mounts have a defined anchor.

The runner launches `Game.Mod=<absolute overlay/mods/cameo>` rather than the ambiguous ID. The engine appends explicit mod paths after search candidates, making this valid complete manifest win. Payload/Include validation fails before launch if required files are missing. The baseline mod and other agents' dirty tooling tree remain unchanged. Only the complete-manifest and launch-argument call sites changed in the runner; DevOps owns their port into the single consolidated campaign branch.

## Master impact

`origin/master@e49acf41a1b3699f01d44e4def06ed4bf83af038` has the same broken runner overlay/selection lines. Thus master CAMPAIGN TOOLING is affected. No base-builder, MCV lease, deploy-cell, seat/home-location or general hard-bot gameplay regression is established. `e49acf41` changes only the previously reviewed observational personality logging/test files and findings; it did not introduce these runner lines or alter deployment policy. Ordinary `hard` runs do not use these newly generated aliases and do not reproduce this alias fallback simply by loading the baseline.

## Validation and open gates

Focused Python `python -m unittest tools.tests.test_campaign_overlay tools.tests.test_ab_campaign_pilot tools.tests.test_campaign_runner -q`: 23/23 PASS. Four new regressions cover real pinned manifest Includes (including the reported Outpost2 failure), only-AI routing, explicit last-wins selection, immutable destination, missing rule payload and missing source Include fail-closed behavior. Existing pilot/runner tests pass. No whole tools/tests run.

An offline .NET console probe invokes the ACTUAL pinned engine `new Manifest("cameo", new Folder(generatedOverlay))`, verifies `campaign_ai|ai/ai.yaml` is present and baseline `cameo|ai/ai.yaml` absent, and resolves the copied Outpost2 Include. PASS, 342 Rules entries. This uses `OpenRA.Game.dll` SHA256 `64f83b7972d6612e6755163ae138861282ae58529ce2b161b806de6a7986108f` from the own engine6da build. Probe artifacts are in `C:/cameo-wt/sol-bu-pilot-overlay/review-results/overlay-parser` (not source files). An initial raw-drive-path manifest failed that parser; final caret paths corrected it and passed. This is manifest parsing, not a boot or gameplay acceptance claim.

The maintainer's NEW early-deploy guard is mandatory BEFORE any further launch, including diagnostic, map acceptance and A5. DevOps owns that shared in-game per-seat marker/consumer/queue-stop consolidation. Require first observed own construction yard/verified first building by tick100, not generic actor count; retain first proof if destroyed later. Missing/unsupported marker or missing seat => `INVALID_NO_DEPLOY`, kill owned process tree immediately, no automatic retry and stop queue for lead review. This overlay fix does not waive or itself implement that guard. Fresh guarded pilot capture must verify aliases activate and every starting MCV deploys; no runtime result is claimed here.
