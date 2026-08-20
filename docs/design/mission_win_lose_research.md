# Mission Win/Lose Condition Research — Cross-Codebase Analysis

_Research 2026-07-29_
_Cross-comparing Combined Arms (CA), Shattered Paradise (SP), base OpenRA (Cnc/RA/D2k), and the Cameo survival map._

## Purpose

Identify how each codebase ensures missions **reach a definitive end** (win or lose) rather than continuing indefinitely. Extract best practices applicable to the Cameo survival map.

---

## 1. Base OpenRA (Cnc, RA, D2k) — The Foundation

### 1.1 Shared Pattern: `InitObjectives(player)`

Every OpenRA mod (Cnc, RA, D2k) defines `InitObjectives` in their `campaign.lua`. This is the **mandatory setup function** called once per human player in `WorldLoaded`:

**Cnc** (`OpenRA-bleed/mods/cnc/scripts/campaign.lua:14-32`):
```lua
InitObjectives = function(player)
    Trigger.OnObjectiveCompleted(player, function(p, id)
        Media.DisplayMessage(p.GetObjectiveDescription(id), UserInterface.GetFluentMessage("objective-completed"))
    end)
    Trigger.OnObjectiveFailed(player, function(p, id)
        Media.DisplayMessage(p.GetObjectiveDescription(id), UserInterface.GetFluentMessage("objective-failed"))
    end)
    Trigger.OnPlayerLost(player, function()
        Trigger.AfterDelay(DateTime.Seconds(1), function()
            Media.PlaySpeechNotification(player, "Lose")
        end)
    end)
    Trigger.OnPlayerWon(player, function()
        Trigger.AfterDelay(DateTime.Seconds(1), function()
            Media.PlaySpeechNotification(player, "Win")
        end)
    end)
end
```

**RA** (`OpenRA-bleed/mods/ra/scripts/campaign.lua:14-33`): Identical pattern, uses `"MissionFailed"` / `"MissionAccomplished"` speech notifications.

**D2k** (`OpenRA-bleed/mods/d2k/scripts/campaign.lua:42-60`): Identical pattern, uses `"Lose"` / `"Win"`.

### 1.2 How Win/Lose Actually Triggers

The engine **automatically** fires `Trigger.OnPlayerWon` when all primary objectives are completed, and `Trigger.OnPlayerLost` when any primary objective is failed. The mission script's job is to:

1. Call `InitObjectives(player)` to register the callbacks
2. Create objectives with `AddPrimaryObjective` / `AddSecondaryObjective`
3. In `Tick()` or event triggers, call `MarkCompletedObjective` / `MarkFailedObjective`
4. The engine handles the rest — speech, game-over screen, etc.

### 1.3 Example: Simple Mission (gdi01.lua)

`OpenRA-bleed/mods/cnc/maps/gdi01/gdi01.lua:29-60`:
```lua
WorldLoaded = function()
    GDI = Player.GetPlayer("GDI")
    Nod = Player.GetPlayer("Nod")
    InitObjectives(GDI)  -- Register win/lose callbacks
    SecureAreaObjective = AddPrimaryObjective(GDI, "eliminate-nod")
    BeachheadObjective = AddSecondaryObjective(GDI, "establish-beachhead")
end

Tick = function()
    if Nod.HasNoRequiredUnits() then
        GDI.MarkCompletedObjective(SecureAreaObjective)  -- Win
    end
    if DateTime.GameTime > DateTime.Seconds(5) and GDI.HasNoRequiredUnits() then
        GDI.MarkFailedObjective(BeachheadObjective)  -- Lose
        GDI.MarkFailedObjective(SecureAreaObjective)
    end
    -- Secondary objective check
    if DateTime.GameTime % DateTime.Seconds(1) == 0 and not GDI.IsObjectiveCompleted(BeachheadObjective)
       and CheckForBase(GDI, GDIBaseBuildings) then
        GDI.MarkCompletedObjective(BeachheadObjective)
    end
end
```

**Key pattern**: Win/lose checks run in `Tick()`. The engine fires the win/lose speech automatically once objectives are marked.

### 1.4 Example: Complex Mission (gdi04a.lua)

`OpenRA-bleed/mods/cnc/maps/gdi04a/gdi04a.lua:89-95`:
```lua
Tick = function()
    Nod.Cash = 1000  -- Perpetual AI cash
    if (GDIReinforcementsLeft == 0 or not GDI.IsObjectiveCompleted(ReinforcementsObjective))
       and GDI.HasNoRequiredUnits() then
        GDI.MarkFailedObjective(GDIObjective)  -- Lose
    end
end
```

Win is triggered by `Trigger.OnRemovedFromWorld(crate, function() GDI.MarkCompletedObjective(GDIObjective) end)` — an event-based win condition, not a poll.

### 1.5 Key OpenRA Best Practices

- **Always** call `InitObjectives(player)` in `WorldLoaded` for each human player
- Win/lose checks in `Tick()` use `HasNoRequiredUnits()` — simple and reliable
- Event-based triggers (`OnKilled`, `OnCapture`, `OnRemovedFromWorld`, `OnEnteredFootprint`) can mark objectives without polling
- `AddPrimaryObjective` / `AddSecondaryObjective` are global helpers defined in `common/scripts/utils.lua` that auto-announce to the player
- AI cash injection in `Tick()` is fine but should be guarded by game state

---

## 2. Shattered Paradise (SP) — TS Total Conversion

### 2.1 SP's `mission_utils.lua`

`Shattered-Paradise-SDK-bleed/mods/sp/scripts/mission_utils.lua:14-48`:
```lua
AddPrimaryObjective = function(player, description)
    local translation = UserInterface.GetFluentMessage(description)
    return player.AddObjective(translation, UserInterface.GetFluentMessage("primary-objective"), true)
end
AddSecondaryObjective = function(player, description)
    local translation = UserInterface.GetFluentMessage(description)
    return player.AddObjective(translation, UserInterface.GetFluentMessage("secondary-objective"), false)
end
TranslatedNotification = function(who, text, color)
    Media.DisplayMessage(UserInterface.GetFluentMessage(text), UserInterface.GetFluentMessage(who), HSLColor.FromHex(color))
end
```

**Notable**: SP does NOT define its own `InitObjectives` — it relies on the base OpenRA pattern. Individual missions handle win/lose directly.

### 2.2 SP Mission Pattern: `CheckObjectivesOnMissionEnd(success)`

SP's cabal-01 mission (`mods/sp/maps/cabal-01/mission.lua:60-104`) uses a **centralized end-game function**:

```lua
CheckObjectivesOnMissionEnd = function(success)
    -- Check all secondary objectives first
    if not LocalPlayer.IsObjectiveCompleted(SecondaryObjectiveHackAllArray) then
        LocalPlayer.MarkFailedObjective(SecondaryObjectiveHackAllArray)
    end
    if not LocalPlayer.IsObjectiveCompleted(SecondaryObjectiveCaptureMCV) then
        -- Try to check if player has MCV/conyard
        for key,unit in ipairs(LocalPlayer.GetActorsByType("cabmcv")) do
            LocalPlayer.MarkCompletedObjective(SecondaryObjectiveCaptureMCV)
            break
        end
        -- ... similar for cabyard
        if not LocalPlayer.IsObjectiveCompleted(SecondaryObjectiveCaptureMCV) then
            LocalPlayer.MarkFailedObjective(SecondaryObjectiveCaptureMCV)
        end
    end
    -- Mark primary objectives
    if success then
        LocalPlayer.MarkCompletedObjective(ObjectiveCaptureAlien)
    else
        LocalPlayer.MarkFailedObjective(ObjectiveCaptureAlien)
    end
    -- ... fail any incomplete primaries
end
```

This function is called from **event triggers**:
```lua
Trigger.OnCapture(ScrinRep, function()
    MissionCompleteMessage()
    CheckObjectivesOnMissionEnd(true)  -- Win
end)
Trigger.OnKilled(ScrinRep, function(self, killer)
    CheckObjectivesOnMissionEnd(false)  -- Lose
end)
Trigger.OnKilled(CabHacker, function(self, killer)
    CheckObjectivesOnMissionEnd(false)  -- Lose
end)
```

**Key SP pattern**: Win/lose is **event-driven** (OnCapture, OnKilled), not polled in Tick. The centralized function ensures ALL objectives are resolved (completed or failed) at mission end.

### 2.3 SP Minigame — Survival with Timer

`Shattered-Paradise-SDK-bleed/mods/sp/maps/minigame-01/mission.lua` is the closest analog to our survival map:

- **Survival objective**: `AddPrimaryObjective(LocalPlayer, "objective-survive")`
- **Secondary objective**: `AddSecondaryObjective(LocalPlayer, "objective-mcv")` (protect MCVs)
- **Timer-based win**: `RemainingTime` counts down in `Tick()`. When it hits 0, `CheckObjectivesOnMissionEnd(true)` fires — the player survived.
- **Event-based lose**: `Trigger.OnKilled(IonTur, function() CheckObjectivesOnMissionEnd(false) end)` — if the key structure dies, instant loss.
- **Wave system**: `SendWaveLoop()` sends reinforcements on a timer, decrements `Waves`, stops when `Waves <= 0`.
- **Difficulty setup**: Random hard modes selected at start, each with unique mechanics.
- **No `InitObjectives` call**: SP minigame does NOT call `InitObjectives` — it relies on the engine's default behavior. This means no speech notifications on win/lose (a gap).

### 2.4 SP Domination/KotH — Multiplayer Win/Lose

`Shattered-Paradise-SDK-bleed/mods/sp/scripts/domination.lua:86-104`:
```lua
-- If only 1 player left alive, they win
if players_still_in <= 1 then
    for i,player in pairs(players) do
        if player.alive then
            player.object.MarkCompletedObjective(player.objective)
        end
    end
end
-- Or if someone reaches target_points
if winner ~= nil then
    for i,player in pairs(players) do
        if i == winner then
            player.object.MarkCompletedObjective(player.objective)
        else
            player.object.MarkFailedObjective(player.objective)
        end
    end
    in_play = false  -- STOP the game loop
end
```

**Key pattern**: `in_play = false` flag stops the game loop. This is the equivalent of our `GameWon`/`GameLost` flags.

---

## 3. Combined Arms (CA) — The Gold Standard

### 3.1 CA's `InitObjectives(player)`

`CAmod-master/mods/ca/scripts/campaign.lua:226-267`:
```lua
InitObjectives = function(player)
    Trigger.OnObjectiveAdded(player, function(p, id)
        if p.IsLocalPlayer then
            Trigger.AfterDelay(1, function()
                local colour = HSLColor.Yellow
                if p.GetObjectiveType(id) ~= "Primary" then colour = HSLColor.Gray end
                Media.DisplayMessage(p.GetObjectiveDescription(id), "New " .. string.lower(p.GetObjectiveType(id)) .. " objective", colour)
            end)
        end
    end)
    Trigger.OnObjectiveCompleted(player, function(p, id)
        if p.IsLocalPlayer then
            Media.PlaySoundNotification(player, "AlertBleep")
            Media.DisplayMessage(p.GetObjectiveDescription(id), "Objective completed", HSLColor.LimeGreen)
        end
    end)
    Trigger.OnObjectiveFailed(player, function(p, id)
        if p.IsLocalPlayer then
            Media.DisplayMessage(p.GetObjectiveDescription(id), "Objective failed", HSLColor.Red)
        end
    end)
    Trigger.OnPlayerLost(player, function()
        if player.IsLocalPlayer then
            Trigger.AfterDelay(DateTime.Seconds(1), function()
                Media.PlaySpeechNotification(player, "MissionFailed")
            end)
        end
    end)
    Trigger.OnPlayerWon(player, function()
        if player.IsLocalPlayer then
            Trigger.AfterDelay(DateTime.Seconds(1), function()
                Media.PlaySpeechNotification(player, "MissionAccomplished")
            end)
        end
    end)
end
```

**CA enhancements over base OpenRA**:
- `OnObjectiveAdded` handler — announces new objectives with color-coded messages
- `PlaySoundNotification("AlertBleep")` on objective completion
- `IsLocalPlayer` check — only show messages to the local player (multiplayer-safe)
- Both speech and text notifications

### 3.2 CA Coop Win/Lose Sync

`CAmod-master/mods/ca/scripts/coop.lua:316-350`:
```lua
ForEachPlayer(function(player)
    Trigger.OnPlayerWon(player, function()
        Trigger.AfterDelay(DateTime.Seconds(1), function()
            Media.PlaySpeechNotification(player, "Win")
        end)
    end)
    Trigger.OnPlayerLost(player, function(p)
        PlayerDefeatedOrDisconnected(p)  -- Redistribute units to survivors
    end)
    TriggerCA.OnPlayerDisconnected(player, function(p)
        PlayerDefeatedOrDisconnected(p)
    end)
end)

Trigger.OnObjectiveCompleted(MainPlayer, function(_, obid)
    ForEachPlayer(function(player)
        player.MarkCompletedObjective(obid)  -- Mirror to all coop players
    end)
end)
Trigger.OnObjectiveFailed(MainPlayer, function(_, obid)
    ForEachPlayer(function(player)
        player.MarkFailedObjective(obid)  -- Mirror to all coop players
    end)
end)
```

**Key CA coop patterns**:
- Objectives are **mirrored** across all coop players — when MainPlayer completes/fails, all players get the same
- `PlayerDefeatedOrDisconnected` redistributes units and cash to survivors
- `OnPlayerDisconnected` handled separately from `OnPlayerLost`
- Win speech played per-player via `ForEachPlayer`

### 3.3 CA Mission End Conditions

CA missions (e.g., `crossrip.lua`) use:
- `MissionPlayersHaveNoRequiredUnits()` — checks if ALL human players have no units (defeat)
- Event triggers for specific objectives (OnCapture, OnKilled, OnEnteredFootprint)
- `Trigger.OnAllKilled` for squad-based objectives
- Difficulty-scaled AI attacks via `InitAttackSquad` system
- `AfterWorldLoaded` / `AfterTick` hooks for mission-specific logic

---

## 4. Cameo Survival Map — Current State & Gaps

### 4.1 What Works

- **Wave system**: `SendWave()` with `FinalWaveSent` flag, `TotalWaves` count
- **Defeat check**: `CheckDefeat()` polls every 100 ticks, checks `HasNoRequiredUnits()` on all active players
- **Victory check**: `CheckVictory()` polls every 100 ticks, checks if all `LiveFoes` are dead after `FinalWaveSent`, or if all Foe players have no units
- **Objective creation**: Creates "Survive all enemy waves" and "Destroy all enemy bases" objectives on all human players
- **Game state flags**: `GameWon`, `GameLost`, `FinalWaveSent` control flow

### 4.2 What's Broken / Missing

| Issue | Impact | Reference | Status |
|-------|--------|-----------|--------|
| **No `InitObjectives` call** | No `Trigger.OnPlayerWon`/`OnPlayerLost` handlers → no speech notifications ("MissionAccomplished"/"MissionFailed"), no clean game-over feedback | All three reference codebases register these | **FIXED** — Added `InitObjectives` with all handlers |
| **No `Trigger.OnObjectiveCompleted/Failed` handlers** | Players get no notification when objectives are marked complete/failed | CA and OpenRA both register these | **FIXED** — Added in `InitObjectives` |
| **`GameLost` not checked in perpetual systems** | `RandomReinforcementDrop`, `RandomEventScheduler`, `CheckDifficulty`, AI cash injection all continue after player loses | SP minigame's `in_play = false` stops all loops; CA checks game state in all schedulers | **FIXED** — All systems now check `GameLost` |
| **No `GameLost` guard in `SendWave`** | If player loses between waves, next wave still fires | Should check `GameLost or GameWon` at top of `SendWave` | **FIXED** — Guard added |
| **No `GameLost` guard in AI cash injection** | AI keeps getting cash after game is over | `Tick()` line 2717 should check game state | **FIXED** — Guard added |
| **No centralized end-game function** | Win/lose logic scattered across `CheckDefeat`/`CheckVictory`, no single function to resolve all objectives | SP's `CheckObjectivesOnMissionEnd(success)` is the model | **FIXED** — Added `ResolveMission` |
| **No objective announcement** | Players don't see "New primary objective" when objectives are created | CA's `OnObjectiveAdded` handler announces | **FIXED** — Added `OnObjectiveAdded` handler |
| **`LiveFoes` tracking edge cases** | Delayed spawns via `Trigger.AfterDelay` may not be in `LiveFoes` when victory check runs | Could cause premature victory trigger | **FIXED** — Added `PendingSpawns` counter |

---

## 5. Comparative Summary

| Feature | OpenRA | SP | CA | Cameo Survival |
|---------|--------|-----|-----|----------------|
| `InitObjectives` with win/lose speech | Yes | No (relies on engine) | Yes (enhanced) | **YES** (implemented) |
| `OnObjectiveCompleted/Failed` messages | Yes | No | Yes (with sound) | **YES** (implemented) |
| `OnObjectiveAdded` announcement | No | No | Yes | **YES** (implemented) |
| Centralized end-game function | No | Yes (`CheckObjectivesOnMissionEnd`) | No | **YES** (`ResolveMission`) |
| Event-driven win/lose | Partial | Yes (OnCapture/OnKilled) | Partial | No (poll-based) |
| `HasNoRequiredUnits()` defeat check | Yes | Yes | Yes (`MissionPlayersHaveNoRequiredUnits`) | Yes |
| Game state flags to stop loops | N/A | `in_play` | Various | `GameWon`/`GameLost` (complete) |
| Coop objective sync | N/A | N/A | Yes (mirror to all) | **YES** (elimination handling) |
| Perpetual system guards | N/A | `in_play` check | Game state checks | **All guarded** (verified) |
| Timer-based win | N/A | Yes (minigame) | No | No (wave-based) |
| Wave system | N/A | Yes (minigame) | Yes (attack squads) | Yes |

---

## 6. Fixes Applied to Cameo Survival Map (2026-07-29)

All 5 recommended fixes have been implemented in `mods/cameo/maps/survival/script.lua`.

### Fix 1: `InitObjectives` (Implemented)

Added CA-style `InitObjectives(player)` function (~line 2564) and called it for each human player in `WorldLoaded`:
- `Trigger.OnObjectiveAdded` → displays "New primary/secondary objective" message with color coding (with `IsLocalPlayer` check)
- `Trigger.OnObjectiveCompleted` → plays "AlertBleep" + displays green message (with `IsLocalPlayer` check)
- `Trigger.OnObjectiveFailed` → displays red message (with `IsLocalPlayer` check)
- `Trigger.OnPlayerLost` → plays "MissionFailed" speech (1s delay, `IsLocalPlayer` check)
- `Trigger.OnPlayerWon` → plays "MissionAccomplished" speech (1s delay, `IsLocalPlayer` check)

### Fix 2: All Perpetual Systems Guarded with `GameLost` (Implemented)

Every `if GameWon` check in the file updated to `if GameWon or GameLost`. Verified with grep — zero remaining unguarded instances:
- `RandomReinforcementDrop` — entry guard
- `RandomEventScheduler` — entry guard + event fire guard + reschedule guard
- `CheckDifficulty` — entry guard
- `SendWave` — entry guard
- `RandomTauntBurst` — entry guard + inner delayed callback guard
- `MemelordDuel` — entry guard + all 8 delayed callback guards
- `DualFactionAttack` — entry guard
- `PlayCrossTaunt` — all delayed callback guards
- AI cash injection in `Tick()` — condition guard
- RANDOM SILENCE trap — delayed spawn guard

### Fix 3: Centralized `ResolveMission` (Implemented)

Added `ResolveMission(success, reasonText, messageText)` function (~line 2588):
- Sets `GameWon` or `GameLost` flag
- Marks ALL survive + destroy objectives on ALL players as completed or failed
- Displays final mission text and message
- `CheckDefeat` and `CheckVictory` both refactored to call it (no more duplicated logic)

### Fix 4: Coop Player Elimination (Implemented)

`CheckDefeat` now handles individual player elimination in multiplayer:
- Detects players with `HasNoRequiredUnits()`, announces their elimination
- Removes eliminated players from `ActivePlayer` list
- Mission loss only triggers when NO survivors remain
- `EliminatedPlayers` table prevents duplicate elimination announcements

### Fix 5: `LiveFoes` Tracking Edge Case (Implemented)

Added `PendingSpawns` counter to prevent premature victory:
- `SpawnUnitListAt` increments `PendingSpawns` by the number of units scheduled via `Reinforcements.Reinforce`
- The arrival callback decrements `PendingSpawns` when each unit arrives and is added to `LiveFoes`
- `CheckVictory` now requires `PendingSpawns == 0` before checking if all `LiveFoes` are dead
- This prevents a victory trigger when reinforcements are in transit but not yet counted in `LiveFoes`

---

## 7. Source Files Referenced

- `OpenRA-bleed/mods/cnc/scripts/campaign.lua` — Cnc campaign framework
- `OpenRA-bleed/mods/ra/scripts/campaign.lua` — RA campaign framework
- `OpenRA-bleed/mods/d2k/scripts/campaign.lua` — D2k campaign framework (most sophisticated AI)
- `OpenRA-bleed/mods/common/scripts/utils.lua` — Shared `AddPrimaryObjective`/`AddSecondaryObjective`/`IdleHunt`
- `OpenRA-bleed/mods/cnc/maps/gdi01/gdi01.lua` — Simple mission example
- `OpenRA-bleed/mods/cnc/maps/gdi04a/gdi04a.lua` — Complex mission with reinforcements
- `Shattered-Paradise-SDK-bleed/mods/sp/scripts/mission_utils.lua` — SP objective helpers
- `Shattered-Paradise-SDK-bleed/mods/sp/maps/cabal-01/mission.lua` — Complex SP mission with `CheckObjectivesOnMissionEnd`
- `Shattered-Paradise-SDK-bleed/mods/sp/maps/minigame-01/mission.lua` — SP survival minigame (closest analog to Cameo survival)
- `Shattered-Paradise-SDK-bleed/mods/sp/scripts/domination.lua` — SP multiplayer win/lose with `in_play` flag
- `CAmod-master/mods/ca/scripts/campaign.lua` — CA campaign framework with enhanced `InitObjectives`
- `CAmod-master/mods/ca/scripts/coop.lua` — CA coop system with objective mirroring and player defeat handling
- `CAmod-master/mods/ca/missions/main-campaign/ca01-crossrip/crossrip.lua` — CA mission example
- `CAmod-master/mods/ca/missions/coop-campaign/ca01-crossrip-coop/crossrip-coop.lua` — CA coop mission
- `Cameo-mod/mods/cameo/maps/survival/script.lua` — Current survival map script (2770 lines, fixes applied)
- `Cameo-mod/mods/cameo/maps/survival/rules.yaml` — Survival map rules
