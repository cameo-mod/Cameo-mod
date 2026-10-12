from pathlib import Path
p=Path(r'C:/Users/AedisToru/Documents/GitHub/Cameo-mod-fleet/REREVIEW_2026-10-06_econ_a.md')
p.write_text('''# REREVIEW — ECON-A fix

**Verdict: FIX-REQUIRED.** Independent re-review of devin/econ-a@aa117a0da0b863ea12d18c42e8eafac79ae7e60c, against the six findings in REVIEW_2026-10-06_econ_a.md and Part A of SPEC_2026-10-05_econ_logistics.md. Reviewer gpt-6.1-sol. Review only; no source changes, switch enabling, push or merge.

## Remaining blocking findings

### R1 — HIGH, still open: live state leaks through the replacement ETA adapter

OpenRA.Mods.CA/Traits/BotModules/BaseBuilderBotModuleCA.cs:1571 obtains `fa.Actor`, then :1576 and :1580 query live temporary-blocker/crushability traits. The pinned engine OpenRA.Game/Traits/Player/FrozenActorLayer.cs:119 returns that backing Actor only while its **current** IsDead is false; it is not an observed snapshot. Gate.CanRemoveBlockage also reads live disabled/paused/friendliness state (engine OpenRA.Mods.Common/Traits/Buildings/Gate.cs:145), and CrushableBy reads live conditional state/ownership (Traits/Crushable.cs:97). None of these facts is frozen at observation time.

Actual compiled-method repro: invoke FrozenBlockedCells on an unchanged valid, visible frozen enemy footprint containing a real DoesNotBlock trait. With the backing actor alive, blocked_cells=0. Dispose that actor under fog without changing the frozen snapshot, owner or footprint: blocked_cells=1. This changes the custom ETA blocker set using hidden death alone; a single-cell travel corridor can therefore change finite/unreachable ETA and scheduling. This probe uses the actual production method, not a copied predicate.

Separately :1521/:1542 treat `player.Shroud.IsVisible(cell)` as knowledge of every ActorMap occupant. Cloaked or otherwise visibility-modified actors on visible cells need not be viewable by this player. The adapter does not call CanBeViewedByPlayer before accepting them. FrozenActorsInRegion also filters IsValid/Visible, not Hidden (engine layer :364–369); the new adapter does not exclude hidden frozen records.

The new HiddenOccupantsNeverBlockTheEta test at OpenRA.Mods.Cameo.Test/BaseBuilderQueueEvalTest.cs:503 supplies known=false directly. It cannot detect either the incorrect knowledge adapter or the live-backed frozen reads. Use only legal visible observations plus immutable remembered facts, and add adapter-level hidden-state invariance tests, including stealth and changes to a backing actor after losing sight.

### R4 — HIGH, partially fixed: a targetless non-idle redirect still renews forever

OpenRA.Mods.CA/Traits/BotModules/BotModuleLogic/BaseBuilderQueueEvalCA.cs:342 accepts any activity chain with no targets as committed. Module :1354 consumes that result; :1289 renews ExpiresTick for every non-idle actor. Together they still make an unrelated targetless activity an immortal expansion journey. The new lease heartbeat does not solve this: this same base-builder owner renews its own lease before the journey check.

Actual SweepExpansionDemands repro: an owned, in-world mobile MCV has a posted demand and is redirected to WaitFor(()=>false), with no Move/deploy target remaining. At ticks100/200/1000 it remains non-idle, demand_count=1 and expiry=200/300/1100. The activity remains blocked when ticked because its predicate is false. Thus a Ready bound item can keep holding its queue after the committed expansion journey has ended. The probe sets NextEtaTick=int.MaxValue solely to isolate lifecycle behavior from pathfinding.

Capture/foreign-lease and explicit far-target checks are improved. JourneyLapsesOnRedirectButNotOnSilence at tests :577 actually locks in the remaining acceptance of silence. Bound targetless/non-journey grace, or prove an explicit ongoing deploy/relocation state before heartbeat; do not renew indefinitely from IsIdle alone.

## Closure of other original findings

| Finding | Result and regression evidence |
|---|---|
| R2 reversed-path chord | CLOSED: module :1504 delegates to ExpansionPathLength (queue eval :319), which sums consecutive returned-path segments only. ReversedPathHasNoSourceToTargetChord :473, adjacent and degenerate tests pass. |
| R3 repeated deploy / invalid replacement | CLOSED: module :1209 consumes the edge only while !Deployed, validates live/own/in-world replacement, tracks DeployedYard and checks its loss at :1301. Actual sweep: tick100 expiry200/wait0; tick101 expiry200/wait50; tick200 demand_count0/wait50; tick1000 still0/wait50. Transform state/invalid replacement regressions pass. |
| R5 premature / ambiguous binding | CLOSED by inspection and focused regressions: queue :304 reserves after cash/army vetoes, :329 binds after QueueOrder, picks require producer+name exclusivity including queued and in-flight orders, and ordinary picks cannot duplicate a demand binding. DemandBindingNeedsAnExclusiveName :587 passes. This is helper/admission inspection, not a complete refund integration fixture. |
| R6 missing anchor reservation / future footprint | Reservation mechanics CLOSED by inspection plus ledger regressions: claim evaluated with future yard tiles (planner :429), reserved at admission, renewed while bound, released on unwind/expiry, re-adopted only with a valid untaken hold. Six ledger lifecycle tests cover competing owners, refresh, expiry/release, taken/prune and commit. Actual future-footprint queue→Ready→placement and multi-producer world fixtures remain unsupplied; no such test is claimed here. |

Non-blocking note: RefineryAnchorReservations.TryReserve at provider :45 refreshes an existing owner's live hold without consulting taken. Placement separately rejects a taken anchor (:461) and Prune later retires the hold, so this is not a demonstrated duplicate refinery placement. Tighten its documented return contract and test own-refresh-after-taken when addressing lifecycle coverage.

## Independent verification and limits

- Own isolated review tree C:/cameo-wt/codex-rereview-econ-a at exact aa117a0da; unchanged d5d8 engine junction points only to the reviewer's owned engine tree. No other agent's outputs were modified.
- Focused dependency compilation only, BuildProjectReferences=false, Release: CA/Cameo/Fransbot each 0 warnings/errors, about2–4s each. No engine rebuild or full build gate.
- `dotnet test OpenRA.Mods.Cameo.Test/OpenRA.Mods.Cameo.Test.csproj -c Release -p:BuildProjectReferences=false --no-restore --filter FullyQualifiedName~BaseBuilderQueueEvalTest --logger "trx;LogFileName=econ-focused.trx" --results-directory review-results`: **57/57 PASS**, 38ms test duration. These tests do not override the adapter/lifecycle counterexamples above.
- Actual-method reflection probe: `dotnet run --project review-results/probe/probe.csproj -c Release`; artifacts Program.cs/probe.log plus focused-tests.log/econ-focused.trx in the owned review-results folder. Small identity/trait/world shell, not a full bot match.
- Source clean except untracked review-results; git diff --check PASS. Fix commit author AedisToru <122120981+AedisToru@users.noreply.github.com>.
- Initial classic/OFF order/RNG paths remain unchanged by inspection; new producer-order bookkeeping runs even OFF but has no order/RNG write. No live bit-identical claim because the shared baseline harness is nondeterministic.
- Developer-reported full1215/1215 tests and BOOT PASS are noted, not independently re-run. The lead's HEAVY_RUN_WINDOW18:25Z–19:30Z permits focused tests only; no boot, game or full suite was launched. No new replay/desync/refund-world acceptance claim.

Keep ECON-A unmerged. R1 and R4 require another fix and independent re-review.
''', encoding='utf-8')
