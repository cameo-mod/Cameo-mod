--[[
	Shellmap 2FFA — attacking-team wave spawner.
	Team 2 (Zerg + Nod + Ordos) assaults Team 1's base at (80,58).
	Cadence: Zerg every 30s; Nod 5s after each Zerg; Ordos 10s after each Zerg.
	Each wave spawns at a randomly chosen entry, set to AttackAnything, and
	attack-moves on the base target (TargetCell).
]]

TargetCell = CPos.New(75, 55)

Zerg = nil
Nod = nil
Ordos = nil

-- Team 1 (defenders) — shielded + auto-repaired.
GDI = nil
Ixian = nil

-- Menu camera: slow orbit around the base so the battle stays in frame. Radius is kept small
-- (~6 cells) because the shellmap always renders fully zoomed out — a wide orbit sweeps the
-- map edge/void into view. 1024 world units = 1 cell. Increase for more motion, decrease if
-- the map border still shows at the orbit extremes.
-- CameraSpeed is intuitive: higher = faster orbit. 1 ~= 144s per full revolution.
-- CameraStartAngle frames the initial view around the base (degrees): 0 = south side,
-- 90 = west, 180 = north side, 270 = east. The orbit then advances from there.
Ticks = 0
CameraSpeed = 1
CameraRadius = 6144
CameraStartAngle = 180
BaseCentre = nil

local function rep(t, n)
	local r = {}
	for _ = 1, n do r[#r + 1] = t end
	return r
end

local function concat(...)
	local r = {}
	for _, list in ipairs({ ... }) do
		for _, v in ipairs(list) do r[#r + 1] = v end
	end
	return r
end

-- Pick one of two options at random each call (RandomInteger high bound is exclusive).
local function pick(a, b)
	return Utils.RandomInteger(0, 2) == 0 and a or b
end

-- Spawn zones: inclusive rectangles { x1, y1, x2, y2 }. Each wave picks one of its side's
-- zones and spawns groups at random cells inside it, so the force fans out instead of
-- stacking on one tile. Two zones per faction (picked at random each wave).
local ZergZoneA = { 40, 3, 44, 5 }        -- north-west
local ZergZoneB = { 75, 2, 79, 4 }        -- north-centre
local NodZoneA = { 2, 76, 4, 80 }         -- west
local NodZoneB = { 113, 125, 117, 127 }   -- south
local OrdosZoneA = { 123, 2, 127, 4 }     -- north-east
local OrdosZoneB = { 126, 63, 128, 67 }   -- east

-- Random cell within an inclusive rectangle zone (RandomInteger high bound is exclusive).
local function randomCell(zone)
	return CPos.New(Utils.RandomInteger(zone[1], zone[3] + 1),
		Utils.RandomInteger(zone[2], zone[4] + 1))
end

-- Per-unit setup: attack-anything + attack-move on the reachable base target (TargetCell).
-- AttackAnything makes them engage anything in weapon range on the way in.
local function waveOnEnter(a)
	if a.IsDead then return end
	a.Stance = "AttackAnything"
	a.AttackMove(TargetCell)
end

-- The base is indestructible, so attackers never "win" and would pile up forever. Cap the
-- live attacker count per player; over the cap we skip that wave (keeps the sim bounded).
local AttackerCap = 40
local function overCap(player)
	return player == nil or #Utils.Where(player.GetActors(), function (a)
		return a.HasProperty("Hunt")
	end) >= AttackerCap
end

-- Spawn a wave as small groups on *separate ticks*. The desert shellmap only ever
-- reinforces ~5 units at once; creating a whole 26-unit wave in a single tick is what
-- trips the engine's spawn crash. Groups of 4 also fit an infantry cell's sub-cells.
local GroupSize = 4
local function launchWave(player, comp, zone)
	local i, gi = 1, 0
	while i <= #comp do
		local g = {}
		for _ = 1, GroupSize do
			if comp[i] then g[#g + 1] = comp[i]; i = i + 1 end
		end
		Trigger.AfterDelay(gi * 12, function ()
			-- fresh random cell in the zone per group, so the wave fans out across the rectangle
			Reinforcements.Reinforce(player, g, { randomCell(zone) }, 3, waveOnEnter)
		end)
		gi = gi + 1
	end
end

local ZergComp1 = concat(rep("sczergling", 20), rep("scultralisk", 2), rep("schydralisk", 5))
local ZergComp2 = concat(rep("sczergling", 20), rep("scmutalisk", 10))
local NodComp1 = concat(rep("ltnk", 5), rep("ftnk", 5), rep("mssm", 2))
local NodComp2 = concat(rep("e1.nod", 20), rep("e4", 10))
local OrdosComp = concat(rep("chem_troop.ordos", 10), rep("combat_tank.ordos", 5), rep("raider.ordos", 5))

-- Alternate between the two compositions on successive waves.
local zergFirst = true
local nodFirst = true

ZergWave = function ()
	if not overCap(Zerg) then
		local comp = zergFirst and ZergComp1 or ZergComp2
		zergFirst = not zergFirst
		launchWave(Zerg, comp, pick(ZergZoneA, ZergZoneB))
	end
	Trigger.AfterDelay(DateTime.Seconds(30), ZergWave)
end

NodWave = function ()
	if not overCap(Nod) then
		local comp = nodFirst and NodComp1 or NodComp2
		nodFirst = not nodFirst
		launchWave(Nod, comp, pick(NodZoneA, NodZoneB))
	end
	Trigger.AfterDelay(DateTime.Seconds(30), NodWave)
end

OrdosWave = function ()
	if not overCap(Ordos) then
		launchWave(Ordos, OrdosComp, pick(OrdosZoneA, OrdosZoneB))
	end
	Trigger.AfterDelay(DateTime.Seconds(30), OrdosWave)
end

-- Some attackers get stuck after spawning (blocked path at the east/edge spawns, or piled up
-- at the walls). A blocked unit is NOT IsIdle, so an idle-only kick misses it — instead we
-- re-issue the assault order to EVERY live attacker every few seconds. AttackMove still lets
-- them engage anything in range en route, so re-ordering fighters is harmless; a fresh
-- AttackMove gives a blocked unit a new path attempt and un-sticks it.
local function kickIdlers(player)
	if player == nil then return end
	Utils.Do(player.GetActors(), function (a)
		if not a.IsDead and a.HasProperty("AttackMove") then
			a.Stance = "AttackAnything"
			a.AttackMove(TargetCell)
		end
	end)
end

KickIdlers = function ()
	kickIdlers(Zerg)
	kickIdlers(Nod)
	kickIdlers(Ordos)
	Trigger.AfterDelay(DateTime.Seconds(5), KickIdlers)
end

--[[
	Tiered damage resistance for Team 1's base + guardians (makes them effectively
	indestructible). Resistance is keyed to the HP % *before* the incoming hit:
	    > 50% HP : 0% resistance   (full damage)
	    30-50% HP: 80% resistance
	    < 30% HP : 100% resistance (never loses HP)
	The Health setter routes through InflictDamage, which re-fires OnDamaged with a
	negative value, so we ignore non-positive damage to avoid recursion.
]]
local function applyShield(self, _, damage)
	-- GetActors() includes the player proxy actor (type 'player'), which has no Health;
	-- touching self.Health on it is a *fatal* Lua error that kills the whole script (and
	-- with it the camera orbit — reading in-game as a freeze). Bail on Health-less actors.
	if not self.HasProperty("Health") then return end
	if damage == nil or damage <= 0 or self.IsDead then
		return
	end

	local pre = self.Health + damage
	local prePct = pre * 100 / self.MaxHealth
	local newHP
	if prePct <= 30 then
		newHP = pre                                    -- 100% resisted
	elseif prePct <= 50 then
		newHP = self.Health + math.floor(damage * 0.8) -- 80% resisted
	else
		return                                         -- > 50%: take it in full
	end

	if newHP < 1 then newHP = 1 end                    -- stay indestructible
	self.Health = newHP
end

-- Shellmap "AI": order repairs on any damaged Team 1 building once a minute
-- (skirmish AI is off — these players have no Bot). Keep them funded so repairs run.
RepairBases = function ()
	Utils.Do({ GDI, Ixian }, function (p)
		if p == nil then return end
		p.Cash = 100000
		Utils.Do(Utils.Where(p.GetActors(), function (a)
			return a.HasProperty("StartBuildingRepairs") and a.Health < a.MaxHealth
		end), function (a)
			a.StartBuildingRepairs(p)
		end)
	end)
	Trigger.AfterDelay(DateTime.Minutes(1), RepairBases)
end

--[[
	Air patrols around the four corners of the base:
	    GDI   Orcas       — clockwise:        63,42 -> 57,76 -> 103,77 -> 104,41
	    Ixian Air Drones   — reverse order:    104,41 -> 103,77 -> 57,76 -> 63,42
	Each side keeps up to 6 airborne; destroyed craft are replaced (checked every 15s).
]]
local PatrolRoute = { CPos.New(63, 42), CPos.New(57, 76), CPos.New(103, 77), CPos.New(104, 41) }
local PatrolRouteReverse = { CPos.New(104, 41), CPos.New(103, 77), CPos.New(57, 76), CPos.New(63, 42) }
local SquadronSize = 6

-- Patrol the route on every idle (also the first idle after spawn), so it loops
-- forever. Orders are deferred to OnIdle so we never command a not-yet-added actor.
local function setPatrol(a, route)
	Trigger.OnIdle(a, function (u)
		if u.IsDead then return end
		u.Stance = "AttackAnything"
		Utils.Do(route, function (c) u.AttackMove(c) end)
	end)
end

-- Find a player's airfield cell to launch from, falling back to a patrol corner.
local function launchCell(player, buildingType, fallback)
	if player == nil then return fallback end
	local b = Utils.Where(player.GetActors(), function (a) return a.Type == buildingType end)
	if #b > 0 then return b[1].Location end
	return fallback
end

-- Top the squadron back up to SquadronSize and (re)assign the patrol.
local function maintainSquadron(player, unitType, cell, route)
	if player ~= nil then
		local count = #Utils.Where(player.GetActors(), function (a) return a.Type == unitType end)
		for _ = count + 1, SquadronSize do
			Reinforcements.Reinforce(player, { unitType }, { cell }, 8, function (a) setPatrol(a, route) end)
		end
	end
	Trigger.AfterDelay(DateTime.Seconds(15), function () maintainSquadron(player, unitType, cell, route) end)
end

WorldLoaded = function ()
	Zerg = Player.GetPlayer("Zerg")
	Nod = Player.GetPlayer("Nod")
	Ordos = Player.GetPlayer("Ordos")
	GDI = Player.GetPlayer("GDI")
	Ixian = Player.GetPlayer("Ixian")

	-- Centre the orbit on the base (mid-point of the four corners).
	BaseCentre = Map.CenterOfCell(CPos.New(82, 60))
	Camera.Position = BaseCentre + WVec.New(0, -CameraRadius, 0) -- start framed on the north side

	-- Shield the base + guardians (all pre-placed Team 1 actors).
	-- For each Team 1 player: shield every Health-bearing actor, and make guardian units
	-- HOLD POSITION. Non-playable players default to InitialStanceAI = AttackAnything, the only
	-- stance that lets a unit leave its post to chase (engine: AllowMove => Stance > Defend);
	-- Defend still fires on anything in range but never moves off its post.
	local function fortify(player)
		if player == nil then return end
		Utils.Do(player.GetActors(), function (a)
			if a.HasProperty("Health") then Trigger.OnDamaged(a, applyShield) end
			if a.HasProperty("Stance") then a.Stance = "Defend" end
		end)
	end
	fortify(GDI)
	fortify(Ixian)

	ZergWave()
	Trigger.AfterDelay(DateTime.Seconds(5), NodWave)
	Trigger.AfterDelay(DateTime.Seconds(10), OrdosWave)
	Trigger.AfterDelay(DateTime.Minutes(1), RepairBases)
	Trigger.AfterDelay(DateTime.Seconds(5), KickIdlers)

	-- Air patrols (GDI clockwise, Ixian reverse).
	maintainSquadron(GDI, "orca", launchCell(GDI, "hpad.gdi", CPos.New(63, 42)), PatrolRoute)
	maintainSquadron(Ixian, "air_drone.ixian", launchCell(Ixian, "launchpad.ixian", CPos.New(104, 41)), PatrolRouteReverse)
end

Tick = function ()
	if BaseCentre == nil then return end
	Ticks = Ticks + 1
	-- 0.1 degrees per tick at CameraSpeed 1 (~144s/revolution at 25 ticks/s), starting from
	-- CameraStartAngle. sin drives east/west (x), cos drives north/south (y); +y is south, so
	-- 180° puts the camera north of the base centre.
	local t = (CameraStartAngle + Ticks * CameraSpeed * 0.1) % 360 * (math.pi / 180)
	Camera.Position = BaseCentre + WVec.New(CameraRadius * math.sin(t), CameraRadius * math.cos(t), 0)
end
