@echo off
setlocal EnableDelayedExpansion
title OpenRA (debug map)

REM ============================================================================
REM Launch Cameo straight into a local skirmish on a test map (auto-readies).
REM
REM   debug-launch.cmd                -> loads volcanic_debug.oramap
REM   debug-launch.cmd mymap.oramap   -> loads the named map
REM
REM Why this is more than `Launch.Map=debug.oramap`:
REM OpenRA scans BOTH map folders from mod.yaml (cameo|maps = System, and the
REM SupportDir User folder). Game.LoadMap matches by filename with SingleOrDefault,
REM so if the same filename exists in both folders it throws. We instead resolve the
REM EXACT file in the User folder and pass its UID. The UID is recomputed every run,
REM so it stays correct even after you edit the map.
REM ============================================================================

REM --- read mod.config (same parsing as launch-game.cmd) ---
FOR /F "tokens=1,2 delims==" %%A IN (mod.config) DO (set %%A=%%B)
if exist user.config (FOR /F "tokens=1,2 delims==" %%A IN (user.config) DO (set %%A=%%B))
REM strip surrounding quotes from the values we use
set ENGINE_DIRECTORY=%ENGINE_DIRECTORY:"=%
set MOD_ID=%MOD_ID:"=%

REM --- resolve the test map in the User (SupportDir) map folder ---
set MAPNAME=%~1
if "%MAPNAME%"=="" set MAPNAME=volcanic_debug.oramap
set "MAPPATH=%APPDATA%\OpenRA\maps\cameo\{DEV_VERSION}\%MAPNAME%"
if not exist "%MAPPATH%" (
	echo [debug-launch] Map not found: %MAPPATH%
	pause
	exit /b 1
)

REM --- compute that file's UID so we target it unambiguously ---
set "MOD_SEARCH_PATHS=%~dp0..\mods,./mods"
set "ENGINE_DIR=.."
set MAPUID=
pushd "%ENGINE_DIRECTORY%"
for /f "delims=" %%U in ('dotnet bin\OpenRA.Utility.dll %MOD_ID% --map-hash "%MAPPATH%"') do set "MAPUID=%%U"
popd
if "%MAPUID%"=="" (
	echo [debug-launch] Failed to compute UID for %MAPPATH%
	pause
	exit /b 1
)

echo [debug-launch] Loading %MAPNAME%  (uid !MAPUID!)
call "%~dp0..\launch-game.cmd" Launch.Map=!MAPUID!
