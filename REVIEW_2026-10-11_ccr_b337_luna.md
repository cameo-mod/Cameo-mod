# Review: A/B headless platform and map runtime acceptance

**Reviewed:** `b337f43e5a13b87deecf55fae5465fd46e7d0f48` (parent `c07bef435d8bb42d7e922255581223df6adf9a28`) and documentation/evidence-only child `6eb87bbd361e4ad4d7faa82beb5604f13a51aa8d`.

**Verdict: NOT READY for Windows map-acceptance execution until the acceptance tool's resource checks fail closed and enforce the approved limits.** The headless simulation approach itself is technically sound; campaign-runner process monitoring and authorization gates are materially fail-closed. This finding blocks the runtime-acceptance gate, not use of the regular campaign runner once that gate is closed.

## Findings

### F1 — Map acceptance skips RAM admission and process memory enforcement on Windows (high, safety gate)

`tools/ai/map_engine_acceptance.py:50-68` reads `/proc/meminfo` and `/proc/<pid>/status`, returning `None` when these Linux files are unavailable. The launch loop at lines 248-251 only rejects when the free-memory value is non-`None`; therefore an unknown reading is treated as permission to launch. The default admission threshold is 6 GiB at line 213, below the campaign's approved 15.5 GiB admission floor. Its only per-process stop is the 1,200-second timeout (`process.kill()` at line 159); peak RSS can remain null and there is no private-memory ceiling or host hard-floor monitor. The audit explicitly instructs running this tool on Windows, so this is not merely an unsupported-platform edge case.

**Required:** reuse the campaign runner's cross-platform RAM/private-byte samplers and constants (15.5 GiB admission, 10.5 GiB private ceiling, continuous 4/3 GiB soft/hard host floors), or refuse to launch when those measurements cannot be obtained. Add focused tests for unknown RAM, below-admission RAM, and process/host threshold stops. Keep cleanup PID-scoped. Until then, do not use this tool for Windows runtime acceptance; existing Linux receipt evidence does not prove Windows resource safety.

### F2 — Headless request can silently produce a windowed campaign receipt (medium, provenance)

`tools/ai/campaign_runner.py:428-431` checks for the Null-platform graphics marker but does not reject a missing marker. It records the actual renderer as `fast`, which is honest and avoids falsely labelling the run headless, but an explicitly requested headless run can still complete and be accepted under the higher-memory renderer. If headless is relied upon for the memory envelope, classify missing marker as incomplete or reject before accepting the result. This is not a false-renderer receipt, but it leaves the requested execution mode unenforced.

## Reviewed behavior that passes

- **Simulation separation:** on the exact pinned OpenRA source `6da7fce14da541180c6baddd6925118fbef65b94`, `OpenRA.Game/Game.cs` calls `LogicTick()` independently of rendering. `WindowIsSuspended` suppresses `RenderTick()` and keeps logic ticks advancing. The Null platform drops texture pixel buffers while retaining dimensions; fonts and dummy audio delegate to Default. Thus the source shows the change is presentation/resource handling, not a gameplay or synchronized-order branch.
- **Empirical parity:** `docs/LESSONS_LEARNED.md` records a same-seed headless/rendered capped replay comparison with 2,673 shared frames and identical orders and sync hashes through the cap. It notes two post-game headless tail orders; the receipt should be interpreted through the common capped frame, not by that tail.
- **Scope/defaults:** the Null platform is included in the Cameo solution and can be explicitly selected via `Game.Platform=Null`; it is not technically restricted to the campaign executable. No default is changed: batch defaults to `fast`, campaign runner defaults to `fast`, and only explicit `--render headless` selects Null. This is opt-in presentation behavior and does not alter normal default play.
- **Runner authorization and monitoring:** `campaign_runner.py` requires execution approval and exact pins, checks DLL presence when headless is requested, enforces the 15.5 GiB admission floor, samples private bytes, and stops for low host RAM/private-cap/stall/time limits using its owned process. Receipts record the renderer actually observed. Those protections do not repair F1 in the standalone map-acceptance tool.
- **Map acceptance semantics:** static generated-map checks plus runtime record joins verify seat count, spawn coordinate, bot, faction, alliances/opponents, arm swap, cap marker, seed, Lua/exception failures. Selected game launches are capped preflight cells, and output paths must be new. Runtime preflight is not outcome scoring.
- **Child commit spot-check:** `6eb87bb` changes documentation/status/evidence only. Its acceptance JSON parses as 32/32 `ACCEPTED` cells and 16/16 `ACCEPTED` pairs across all eight named maps, with zero cell problems. This evidence does not eliminate F1 because it does not demonstrate safe Windows admission/monitoring.

## Validation performed

- Exact-SHA detached checkout at `b337f43e5a13b87deecf55fae5465fd46e7d0f48`; no source edits made to the reviewed commit.
- Focused offline tests: `python -m unittest tools.tests.test_map_engine_acceptance tools.tests.test_campaign_runner tools.tests.test_ab_campaign_pilot -q` — **26 passed**.
- No engine build, game launch, or full `tools/tests` run was performed.
