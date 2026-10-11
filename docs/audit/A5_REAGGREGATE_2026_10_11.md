# A5 offline re-aggregation

Live qualification records individual hashes for OpenRA.exe, OpenRA.Game.dll and OpenRA.Mods.Cameo.dll in both run and aggregate receipts. All three must exist before launch; missing binaries are never silently omitted. Lead's disposition: preserve raw 99d9 as a superseded PASS and perform a fresh qualification after review/landing, under the runtime owner's reservation. This change launches nothing.

Focused validation: 50 tests run, 49 passed, one existing skip. No full tools/tests process or game launch.

The emitter derives renderer, hard process ceiling, host emergency floor, actual stop threshold and allocation-trace status from the two matching run records. Offline mode bypasses launch preflight and all game execution. It requires an exact raw SHA, matching embedded/external per-run receipts, matching configurations and a new output path. Missing legacy renderer fields may be derived only from preserved headless graphics logs; their raw hashes and the per-run receipt hashes are recorded. No original artifacts are edited.

Example (PowerShell; substitute absolute evidence paths):

```powershell
python tools/ai/a5_memory_parity_probe.py --reaggregate-source RAW.json --source-raw-sha256 99D9C1B8E8D91BA46FE63C2331D58CB08B251D0B92D855AC9006040311FC4D6A --reaggregate-output NEW.json
```

The preserved d686 A5 pair has source d686eb5ee76f4d021c258309544b023927d54a9f and baseline 68be89ca0650faaedde2cedd649aa0ac75aa17bf. The gate verifies both against the manifest source using Git ancestry and a restricted changed-path check: tools/, docs/ and DEVELOPMENT_LOG.md only, rejecting C#, YAML, Lua and map files. Missing Git evidence fails closed. Engine and binary pins remain exact. Actual 7 GiB stops are accepted below the unchanged 10.5 GiB hard ceiling; the host emergency floor remains exactly 3 GiB. Baseline fields are preserved, never rewritten to today's HEAD.

Validation: focused re-aggregation, A5, campaign-runner and pilot unit tests; no game launch. Gate regressions cover reconstructed receipt acceptance, missing aggregate fields, missing record configuration/graphics evidence, invalid stops, mismatched records, raw hash mismatch, overwrite refusal, real Git ancestry and forbidden source changes.

**Remaining evidence gap:** the raw pair records only OpenRA.exe and OpenRA.Mods.Cameo.dll individual hashes. The current candidate manifest also pins OpenRA.Game.dll. Preserved batch summaries contain a combined mod-DLL digest, not its individual hash. The reconstructed receipt passes with the exact recorded two-binary test pins and deliberately fails the current three-binary manifest. This change does not authorize dropping that pin, inventing a historical hash, arming a manifest or launching outcome games. A5/map acceptance and execution readiness must be assessed separately by the runtime owner.
