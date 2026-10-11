# Exact cap-materialization review

Reviewed remote `codex/campaign-cap-resolve-crash@6914c2067b9575b4a4b01e3346977bef34dd8d21`. Direct parent is `b2e66577c437774db828a4868503fa4467af342d`; cumulative base is `e49acf41a1b3699f01d44e4def06ed4bf83af038`, four commits. Remote equality and cumulative diff-check verified in an isolated detached checkout; author's tree untouched.

**Bounded PASS for selective overlay/cap-resolution materialization; FIX REQUIRED for accepting the cumulative branch's deploy-guard integration. No wholesale merge or launch approval.**

The complete manifest copies declared Include files before parsing, retains pinned baseline resources, maps only the three AI rule files to the generated dual-arm package, and selects the explicit overlay directory after search-path candidates. This addresses both the missing Outpost2 include and the baseline-shadowing failure that left alias bots without IBotInfo/active modules. The temporary notification-suppression workaround is absent from the cumulative result. No C#, engine pin, strategy policy, orders, RNG or sync implementation changes occur in this delta. Loading the intended aliases necessarily changes execution relative to the broken inactive-bot overlay; static review does not prove order/sync parity, runtime cap completion, or observation cost. The new Lua polling checks owned actors and latches recognized yards; it adds observation callbacks, so zero simulation overhead is not established.

Independence disclosure: `campaign_overlay.py` is byte-identical to my own previously pushed `0e3e7f546` helper. This portion is a reviewed author contribution, not an independent second opinion by me. Luna's exact-6914 source review supplies the independent helper review; my review independently covers the receiving call sites, cumulative workaround removal, markers and consumers. Existing offline pinned-engine parser proof for that unchanged helper is retained, not rerun.

## Findings requiring selective integration fixes

1. **Missing markers can evade the early abort.** `monitor_process` observes actor-sample tick5000 but then overwrites its local world_tick with `latest_world_tick(stall_log)`, which accepts only `[WT N]`. With debug at WT0 and no deployment/check markers, it keeps running despite known tick5000. New focused fake-process reproduction confirms no kill before simulated exit; eventual receipt is INVALID_NO_DEPLOY. Preserve the highest trustworthy observed tick, and add a bounded missing-clock/marker policy rather than wait for generic 180s stall/3000s timeout. Actual tick100 timing remains unproven with two-second polling.
2. **Verified receipt census accepts duplicated seats.** `validate_receipt` checks count and membership separately, without uniqueness/set equality. The author's valid fixture expanded to two resolved seats plus duplicate M0 deployment passes with M1 unproved. Require unique resolved/deployed IDs and exact set equality. The current producer dictionary is unique; this finding concerns the published receipt validator's fail-closed contract.
3. **Cleanup is handle-only, not a game-tree guarantee.** `monitor_process` calls only `process.kill()/wait()` and reports pid_scoped_cleanup from that handle's exit. `run_one` monitors the launcher Popen PID; neither this path nor kill_owned_pid traverses descendants. If the launcher creates a child, child memory/exit/cleanup are unproved. Preserve the consolidated runner's identity-checked real-game process/tree handling and add a child-survival regression; a mock with only one PID does not prove tree cleanup.
4. `map_engine_acceptance.py` is absent on this branch. Its required deployment guard is still a separate integration gate.

## Tests and stale integration boundary

Reuse Luna's exact-6914 receipt for the four focused modules: **31/31 PASS**, compile/schema parse/diff-check PASS. Per TOKEN_RULES these unchanged tests were not repeated. I ran only `python review_cap_6914_probe.py`: **two new defect reproductions confirmed**, no real process/game created. The reproduction file accompanies this note. My cumulative diff-check also passes.

Retain current approved admission/parallelism and source/engine manifests during selective port: this branch still asserts 15.5 GiB admission/three small workers, while current ruling uses measured headless peak plus 3 GiB, at most two small games at >=14 GiB and exclusive large games. A5 still names 964cdb/6da7 rather than the current e49acf/4f69df coordinates documented by Luna. Do not overwrite consolidated policies, PID safeguards, acceptance guards or current pins with this branch. No runtime cap/deployment acceptance, full campaign readiness, or master merge is certified.

## Resume

Review complete. Owner should port only the materialization/explicit-selection fix and reconcile the four findings above on the clean campaign lineage. Re-review the exact integrated SHA and run focused guard regressions before any reserved acceptance run. Preserve prior receipts and invalid pilot evidence.
