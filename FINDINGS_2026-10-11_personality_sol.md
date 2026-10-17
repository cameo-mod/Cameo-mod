# Personality logging and team turtle findings ? 2026-10-11

## Scope and evidence

Source-only investigation on `2209a6e7b4ade0dae691916870d1c497c3df1570` (lead's updated base, descending from requested `c07bef43`). Cloud RR4 is in `origin/ccr-c7f51935-i53fkj:docs/AI_ARCHITECTURE_RESEARCH_SYNTHESIS_MASTER_2026-10-06.md`. The lead confirmed that its raw matches, situations and replays stayed on the cloud machine and are unavailable here. The reported Clutch and Terra Cotta outcomes are context, not independently replay-verified evidence. No game, replay playback or boot was launched.

## Confirmed logging defects and correction

`AiMatchLogRecorderInfo.PersonalityConditions` omitted `personality-guerrilla`, although `BotPersonalityControllerInfo.Conditions` offers it. The mounted player recorder uses these defaults. Switching to guerrilla therefore removed the preceding live condition without recording the new posture: final personality could be empty and its timeline incomplete. Added guerrilla and a regression comparing the entire recorder/controller default catalogues.

`AiMatchLogWriter.BuildLog` also read the current condition at final serialization. The controller revokes that condition on disable; the recorder retains its transition history. A deterministic fixture reproduces an empty final summary after condition revocation for all six personalities. Final summaries now use the last observed transition. Live `CurrentPersonality` remains condition-based for gameplay consumers; a recorder that never observed a personality stays empty. This changes only observational output, with no orders, RNG, sync, engine, pin or gameplay policy change. The existing schema-3 field means the last observed posture, including after elimination; it does not assert a currently enabled condition. Timeline bounds remain 64 and preserve the last transition.

## Seat attribution: no swap established

`AiMatchLogWriter.BuildLog` obtains each recorder from that loop's `player.PlayerActor` and appends its timeline inside the same player object. `SeatKey` uses the player's index in `world.Players` plus one; neutral/map actors can precede playable seats. `AiSituationLogWriter` uses the same helper. A seat is not a filtered bot ordinal, lobby client index or spawn number. Join records by `game_uid` + exact `player.seat` / situation seat; use `home`, `team`, `bot_type` and replay player metadata to validate spawn/treatment attribution. Do not pair timelines by row position, sorted outcome or presumed `seat_1 == Multi0`.

Recorder timeline, active-condition list and counters are instance fields. Tests interleave two distinct histories, reverse serialization order, switch only the loser to guerrilla, revoke that condition and verify each serialized seat retains its own history and switch count; absent evidence is never copied from another seat. These are production callback/serialization seam tests, not loaded-world seat mapping proof. No seat renumbering or speculative swap correction is included. Fresh pilot raw logs must confirm whether RR4's apparent swap was an analysis join error, incomplete guerrilla history or a further bug.

## Turtle convergence: independent policy can produce it

`MasterAiBotModule` stores incumbent, sustained candidate, candidate-since, last-switch and emergency handling per instance. Its constructor binds the owning player; `Rebuild` groups actors by owner and computes own army from that owner's group. Personality orders target that player's actor. Initial choices consume separate sequential shared-RNG draws; switching is a deterministic policy, not a shared random personality assignment. No shared mutable personality state or teammate-copy branch was found.

`PersonalityCandidates` has a terminal `urgency >= Pressured ? turtle : expansion` fallback after offensive/tech candidates. With `EmergencyKeepsPersonality=true`, the immediate emergency turtle override is suppressed, but this ordinary fallback remains. Two pressured allies facing similarly strong known enemies can independently choose turtle, then switch after their ordinary sustained delay. The rule follows DESIGN ?19.11's explicit Pressured-equivalent candidate handling; the knob does not freeze the current posture for the whole emergency. Existing and new tests characterize this distinction. The new fixture converges two independent enemy profiles to turtle, then changes only one profile to weak and obtains rush for that seat while the other's turtle decision is unchanged; the turtle transition still waits its full delay.

This explains a plausible correlated policy response, not the causes of those two unavailable losses. Common pressure, losses and visibility may correlate inputs; turtle may follow a collapse rather than cause it. Snapshot staggering by client index does not remove such correlation.

## Proposed gameplay follow-up (not implemented)

First use the fresh pilot's per-seat situation rows to align urgency, candidate, own army, target strength, deaths/kills window, actual personality transition and offensive activity before each loss. Verify corrected match history agrees with situation/replay owner identity. Distinguish forced emergency switches from delayed terminal fallback and distinguish preceding army loss from loss after posture change.

If terminal fallback systematically turns the entire viable attacking team defensive, prefer a separately reviewed, default-off bounded fallback rule: retain the incumbent offensive posture when turtle is selected only by the terminal pressure fallback, allow verified base-danger defense immediately, and retain M13's offensive-activity deadline as the progress valve. Give the hold a tier-scaled absolute expiry that repeated pressure cannot renew. Emit a bounded reason distinguishing fallback/base-danger/legacy emergency. Preserve owner-local eligibility and ordinary offensive candidates; avoid random anti-correlation or a shared team personality latch. Exact danger signal, incumbent eligibility, expiry and switch group require maintainer approval and per-type tests before implementation, plus matched Terra Cotta/Clutch validation. This branch changes no strategy and claims no win-rate improvement.

## Validation

Release build PASS, 0 warnings / 0 errors. Focused C# PASS 101/101, zero skipped, including nine personality logging cases and the new independent-pressure policy characterization. Commands:

```powershell
dotnet build OpenRA.Mods.Cameo.Test/OpenRA.Mods.Cameo.Test.csproj -c Release --nologo -v quiet -m:1
dotnet test OpenRA.Mods.Cameo.Test/OpenRA.Mods.Cameo.Test.csproj -c Release --no-build --nologo --filter 'FullyQualifiedName~PersonalityLoggingTest|FullyQualifiedName~MasterAiBotModuleTest' --logger 'trx;LogFileName=personality-final.trx' --results-directory review-results/personality
```

TRX: `review-results/personality/personality-final.trx` in the isolated worktree. Engine pin remains `6da7fce14da541180c6baddd6925118fbef65b94`; copied own pinned engine build input and rebuilt the test project. Initial characterization build used a readonly initializer and was corrected; first test attempt accidentally used legacy Info defaults and failed the new policy case. The final test explicitly uses the existing `EmergencyInfo(25, true)` seam matching mounted generic-bot YAML. Both earlier failures are preserved locally and are not pass evidence. `git diff --check` PASS. No full tools/tests run, runtime parity, live-world adapter, pilot result, release adoption or publication approval is claimed.
