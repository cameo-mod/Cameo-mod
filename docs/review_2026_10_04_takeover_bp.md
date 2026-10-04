# 2026-10-04 independent review recovery record

Owner: Boss (Devin), team slot 01a10697-1728-7a81-b12c-1f9acfad34f2. User requested coordinated independent review and durable documentation while Claude resumes in a fresh session. This is review evidence, not a replacement for DESIGN rulings. No source changes, merges, builds, or game launches in this review.

Checkpoints: takeover devin/t3verify/bot-takeover@9a0348101, worktree C:/cameo-wt/takeover; BP phase 1 devin/t3verify/bp-front-back@63cd1c372, worktree C:/cameo-wt/bp. Both baseline 8e86fca23. T3Verify owns both implementations. BP phase-1 static review is complete with changes requested; fixes and re-review are pending. REF-1 and stale-test-baseline checkpoint reviews were requested from their owners; neither is approved here.

## Takeover: changes requested (static source review)

- T1, P1: AiMatchLogWriter.cs:76-112,318-322 closes permanently at the first tick of a human-only match: IsLoggedPlayer selects no seats, All(empty) is true, and CaptureAndAppend marks written=true for empty output or non-ownership. A subsequent takeover cannot emit its required record. Keep capture available while future takeover candidates exist. Regression cases: humans only followed by takeover; initial lobby bots finish before a later human takeover.
- T2, P1: BotTakeoverTracker.cs:147-153 uses map PlayerReference.Team for everyone and overwrites only NonBotPlayers. Lobby bots therefore retain map team IDs instead of lobby team assignments. Engine CreateMapPlayers.CreatePlayers constructs players from the unchanged map reference; SetupPlayerMasks reads lobby client.Team separately. Snapshot every occupied lobby slot, including bots. Regression: one human on team 1 versus two lobby bots on team 2 is two teams; last-human surrender must defeat, not be mistaken for three teams and take over.
- T3, P2: BotTakeoverTracker.cs:80,318-319 grants only genericbot and hardbot. The same baseline enables GrantConditionOnBotOwner@inc3f1 for hard (mods/cameo/ai/ai.yaml:49-51), but the takeover seat never gains inc3_frans_services; eight service modules in ai/fransbot.yaml:33-53 stay disabled. This breaks DESIGN 19.14 R6 hard-stack parity. Resolve matching bot-owner grants consistently on all clients and test hard condition-set parity.
- T4, P2: connectedClients snapshots NonBotPlayers only. A connected spectator admin is therefore reported absent by AdminConnected. AiMatchLogWriter.IsLogOwner accepts both the admin via Game.IsHost and the elected player via !AdminConnected, allowing duplicate match records. Separate controller eligibility from admin connectivity and test a spectator admin plus player controller, including admin departure.

All four findings were sent to T3Verify and Claude via the team mailbox. Tier4 was notified that takeover smoke logging must use the corrected checkpoint. Fixes and live multiplayer verification remain pending; this record does not claim test failures were executed. Previously reported implementation gates are author-reported, not repeated by this review.

## BP phase 1: changes requested (static source review)

Default-OFF genericbot wiring, interface placement in OpenRA.Mods.CA, and isolation from REF-1-owned files were confirmed. The following defects affect the planner when consumed in phase 2; no gameplay test was run by this reviewer.

- B1, P1: BaseFrontBackPlannerBotModule.cs:709-722,799-805 returns missing providers rather than the documented absolute target. Two defended fronts with one already served return 1; the prepared phase-2 consumer compares this with one owned provider and suppresses the second. All served returns 0. Return an absolute target consistent with defended fronts plus justified extras and the power-margin calculation. Regressions: two fronts with zero/one/two served, an extra with sufficient new coverage, and zero defended fronts.
- B2, P1: BaseFrontBackPlannerBotModule.cs:645-651 builds each cluster's defence perimeter from all owned defences, without filtering membership in that cluster. Remote expansion towers can therefore create or move the home front's line and approach. Restrict perimeter actors to the current cluster footprint. Regression: separate home/expansion clusters, only one defended; adding remote towers must leave the home line unchanged.
- B3, P1: BaseFrontBackPlannerBotModule.cs:778-796 falls back after the wait to unconstrained ChooseValuableCell. If every legal candidate is beyond the defence line, or ahead of the centre when no line exists, it still returns a forward radar. Filter to safe back cells; Hold when none exist. Regressions: defended and undefended fronts, timeout elapsed, only forward candidates.
- B4, P2: BaseFrontBackPlannerBotModule.cs:720-721,774-790 allows an extra radar when total uncovered approach reaches the threshold, without requiring the selected candidate to cover that many new union cells. A band candidate adding zero or 29 cells can pass when RadarMinNewCoverageCells is 30. Enforce the candidate's delta for extras and positive approach reach for first providers. Regressions: at least 30 uncovered cells but best legal candidate adds zero/29/30.

All four were sent as one batch to T3Verify. The current helper tests do not cover these live assembly paths. Phase-2 QM integration and FragileTypes changes remain blocked until REF-1 lands under coordinator control.

## Recovery routing and queued work

This documentation branch is separate from master and is not merged. Resume from `C:/cameo-wt/boss_review/docs/HANDOFF.md` and this record; the fleet status note points here. T3Verify subsequently reported BP head `bd9ee1eac`, a DEVELOPMENT_LOG-only successor to reviewed `63cd1c372`; no implementation change or review approval is implied.

| Work | Owner | Durable status / next dependency |
|---|---|---|
| Takeover corrections T1-T4 | Devin-T3Verify (01a1027f-1525-7d21-bb28-7b98aadfeee7) | Task 01a106a7-01f8-79b0-9cb6-0cd86896758f; pending corrective checkpoint and focused regression evidence. |
| BP corrections B1-B4 | Devin-T3Verify | Task 01a106a7-0231-70c0-9d35-ba91cf698e2b; phase-1 corrections only. QM/FragileTypes phase 2 waits for REF-1 on master. |
| REF-1 refinery law | Devin-T2Verify (01a1027f-14ce-7320-8d29-14fb7d21e602) | Task 01a10617-2ed2-73d3-a702-16bd7a0d940b; C:/cameo-wt/ref1, devin/ref1-refinery-law-v2. Active dirty worktree observed; awaiting owner checkpoint, no independent approval. |
| tools/tests baseline | Devin-Tier1 (01a1027f-1416-78b1-9c15-f685e238b297) | Task 01a1060d-ee08-71a1-adf0-4dc05c838687; C:/cameo-wt/ember-tests, devin/ember/tests-baseline. Active fixture/test edits observed; awaiting classification note and owner checkpoint. |
| Dedicated takeover smoke | Devin-Tier4 (01a1027f-147c-7b13-afeb-bdd16fd7b03c) | Task 01a10659-06fb-76d1-b51e-40138e463da7. Fleet NOTE_2026-10-04_devin-tier4_takeover-smoke-feasibility.md reports feasibility with a dev-only hook; STEP 2 awaits Claude's explicit go per existing assignment. Use corrected takeover checkpoint. No live result asserted. |
| Allied radar then radar contacts | Devin-Tier4 | Queue: takeover smoke step 2, RADAR-ALLY, mpspawn fix, RADAR-A. Radar-ally ruling supersedes RADAR-A's older own-provider-only wording: allied providers must count. RADAR-A remains provider/cluster/logging only, default OFF, no consumers. |

The user asked every owner to preserve exact checkpoints, verification actually run, open review issues, and next steps in HANDOFF.md, DEVELOPMENT_LOG.md and fleet status notes. Claude remains coordinator; no authority to merge or start the held smoke implementation transfers through this review.

## Next session

1. Read this record and the fleet status note before relying on mailbox history.
2. Obtain T3Verify corrective checkpoint, review all four fixes, and preserve Tier4 dedicated-client smoke results.
3. Review BP corrective checkpoint against B1-B4; keep QM/FragileTypes phase 2 blocked until REF-1 lands under Claude coordination.
4. Review available REF-1/baseline checkpoints. Claude retains merge and campaign authority.
