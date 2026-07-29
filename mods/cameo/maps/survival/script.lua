-- Crazy Survival -- EXTREME CHAOS EDITION (design 2026-07-28)
--
-- 26 waves, ONE faction theme per wave, tech tier ramping T1 -> T4.
-- EVERYTHING IS RANDOMIZED TO THE MAX:
--   - Wave ORDER is shuffled within tier brackets every game
--   - Wave TIMERS are EXTREMELY random (prep 2.5-6min, gaps 30-240s with wild variance)
--   - Wave BUDGET has EXTREME variance (-50% to +80%) and BOOSTED base/growth
--   - Unit COUNT has EXTREME variance (-30% to +60%) and MUCH higher base counts
--   - Veteran LEVELS on units are random up to 3x normal
--   - Sneak attacks use RANDOM faction units at RANDOM times (now with THIRD wave)
--   - Chaos attacks fire from wave 1+ with up to THREE simultaneous waves
--   - Reinforcement drops at FASTER random intervals (10-50s) with MORE units
--   - Double Trouble from wave 3+ with TRIPLE TROUBLE chance
--   - Elite Surges from wave 2+ with EPIC units and EXTRA vet levels
--   - MCV deployment at RANDOM times from RANDOM edges
--   - Taunts from RANDOM generals at RANDOM intervals (45% burst chance)
--   - Surprise waves from wave 2+ with DOUBLE surprise chance
--   - Random epic unit spawns during chaos attacks (1-3 epics at once!)
--   - Random taunt bursts from random faction generals (each spawns units!)
--   - Dual faction attacks with cross-faction dialog (40% + 15% second chance)
--
-- INSANE EVENT SYSTEM (RandomEventScheduler fires every 8-25 seconds):
--   - BLITZKRIEG: All four edges spawn units simultaneously
--   - EPIC STAMPEDE: 2-5 epic units from random directions
--   - SWARM NIGHTMARE: 15-30 units in a massive horde
--   - KAMIKAZE RUSH: 4-8 veteran squads from all angles in rapid succession
--   - TRIPLE FACTION ASSAULT: Three random factions attack at once
--   - BETRAYAL EVENT: A faction "switches sides" with full 4-edge assault
--   - RANDOM SILENCE: Fake calm that's 50% likely to be a trap
--   - All existing events boosted: more units, more directions, higher vet
--
-- Additional systems:
--   - General taunts (loaded from generals.lua) with randomised selection
--   - Cross-faction dialog (loaded from cross_taunts.lua) for dual/triple attacks
--   - Random MCV deployment from all 4 map edges (lottery system)
--   - Sneak attacks: subterranean, chronoshift, paratroopers, burrow,
--     warp, dropship, blitz, and generic ambush -- faction-dependent
--   - Faction descriptions displayed at wave start
--   - All 26 factions represented across the wave roster

GoundAttackArrayTop = {Top1,Top2,Top3,Top4}
GoundAttackArrayRight = {Right1,Right2,Right3,Right4}
GoundAttackArraySouth = {South1,South2,South3,South4}
GoundAttackArrayLeft = {Left1,Left2,Left3,Left4}

Foes = {}
ActivePlayer = {}
PlayerCount = 0
LiveFoes = {}
FinalWaveSent = false
GameWon = false
GameLost = false
RemainingTime = 0
timerStarted = false
Text = ""
CurrentWaveIdx = 1

PrepSeconds = Utils.RandomInteger(150, 361)   -- random prep: 2.5-6 minutes (LONGER, more random)
BaseBudget = 2500      -- wave-1 base budget at 1 player
BudgetGrowth = 500     -- extra budget per wave index
CenterPos = CPos.New(75, 75)

-- =====================================================================
-- MESSAGE THROTTLE SYSTEM
-- Prevents chat flood by enforcing a minimum gap between non-critical
-- messages. Critical messages (wave start, difficulty, victory) bypass.
-- =====================================================================
MessageReady = true
MESSAGE_MIN_GAP = 20  -- minimum seconds between non-critical messages

RawDisplayMessage = Media.DisplayMessage

ThrottledDisplayMessage = function(msg, prefix, isCritical)
	if isCritical then
		RawDisplayMessage(msg, prefix)
		return
	end
	if MessageReady then
		RawDisplayMessage(msg, prefix)
		MessageReady = false
		Trigger.AfterDelay(DateTime.Seconds(MESSAGE_MIN_GAP), function()
			MessageReady = true
		end)
	end
end

-- =====================================================================
-- WAVE SHUFFLE
-- Shuffle wave order within tier brackets so every game is different.
-- =====================================================================

ShuffleTable = function(t)
	for i = #t, 2, -1 do
		local j = Utils.RandomInteger(1, i)
		t[i], t[j] = t[j], t[i]
	end
	return t
end

ShuffleWaves = function()
	local byTier = { {}, {}, {}, {} }
	for _, w in ipairs(Waves) do
		table.insert(byTier[w.tier], w)
	end
	for tier = 1, 4 do
		ShuffleTable(byTier[tier])
	end
	Waves = {}
	for tier = 1, 4 do
		for _, w in ipairs(byTier[tier]) do
			table.insert(Waves, w)
		end
	end
	TotalWaves = #Waves
end

-- { name, tier, units = { {type, cost}, ... }, epic = {type, cost} or nil, desc = "...", mcv = "..." }
Waves = {
	-- ==================== TIER 1 ====================
	{ name = "GDI Task Force",        tier = 1, desc = "Global Defense Initiative's finest, bringing overwhelming firepower and orbital strikes to bear.", mcv = "td_gdi_mobileconstructionvehicle",
	  units = { {"td_gdi_minigunner",100}, {"td_gdi_grenadier",200}, {"td_gdi_humvee",400}, {"td_gdi_battletank",900} } },
	{ name = "Nod Raiding Party",     tier = 1, desc = "Brotherhood of Nod fanatics with hit-and-run tactics and flame weapons.", mcv = "td_nod_mobileconstructionvehicle",
	  units = { {"td_nod_minigunner",100}, {"td_nod_rocketsoldier",200}, {"td_nod_buggy",300}, {"td_nod_flamethrower",200}, {"td_nod_lighttank",600} } },
	{ name = "Allied Vanguard",       tier = 1, desc = "Allied forces deploying Chronosphere technology and versatile armoured columns.", mcv = "ra1_allies_alliedmobileconstructionvehicle",
	  units = { {"ra1_allies_rifleinfantry",100}, {"ra1_allies_ranger",300}, {"ra1_allies_machinegunner",400}, {"ra1_allies_alliedlighttank",500}, {"ra1_allies_alliedmediumtank",700} } },
	{ name = "Soviet Onslaught",      tier = 1, desc = "Soviet Red Army with heavy tanks, tesla coils, and overwhelming numbers.", mcv = "ra1_soviets_mobileconstructionvehicle",
	  units = { {"ra1_soviets_rifleinfantry",100}, {"ra1_soviets_ak47conscript",200}, {"ra1_soviets_rocketsoldier",300}, {"ra1_soviets_flaktruck",800}, {"ra1_soviets_heavytank",1000} } },
	-- ==================== TIER 2 ====================
	{ name = "Allied Peacekeepers",   tier = 2, desc = "Modern Allied forces with Guardian GI anti-tank infantry and IFV adaptability.", mcv = "ra2_allies_alliedmobileconstructionvehicle",
	  units = { {"ra2_allies_gi",200}, {"ra2_allies_guardiangi",400}, {"ra2_allies_ifv",500}, {"ra2_allies_grizzlytank",750} } },
	{ name = "Red Army",              tier = 2, desc = "Soviet war machine reborn -- Rhino tanks, V3 rockets, and Apocalypse-class armour.", mcv = "ra2_soviets_mobileconstructionvehicle",
	  units = { {"ra2_soviets_conscript",100}, {"ra2_soviets_flaktrooper",300}, {"ra2_soviets_rhinoheavytank",850}, {"ra2_soviets_flaktrack",900}, {"ra2_soviets_v3rocketlauncher",900} } },
	{ name = "Psychic Corps",         tier = 2, desc = "Yuri's psychic army -- mind control, genetic brutes, and magnetic levitation tanks.", mcv = "yuri_mobileconstructionvehicle",
	  units = { {"yuri_initiate",200}, {"yuri_gatlingtrooper",300}, {"yuri_brute",400}, {"yuri_lashertank",600}, {"yuri_magnetron",1300} } },
	{ name = "Asian Alliance Strike", tier = 2, desc = "Fusion of Eastern martial tradition and modern warfare -- samurai, archers, and hover tanks.", mcv = "asianalliance_asianmobileconstructionvehicle",
	  units = { {"asianalliance_japanesesamurai",350}, {"asianalliance_veteranarcher",450}, {"asianalliance_lynxtank",850}, {"asianalliance_phoenix",1800} } },
	{ name = "Latin Syndicate",       tier = 2, desc = "Cartel warlords with stolen tech -- militia, tank killers, and Soviet-surplus aircraft.", mcv = "latinsyndicate_syndicatemobileconstructionvehicle",
	  units = { {"latinsyndicate_latinmilitia",120}, {"latinsyndicate_tankkiller",300}, {"latinsyndicate_grenademonkey",200}, {"latinsyndicate_raiderbuggy",350}, {"latinsyndicate_rushertank",700}, {"latinsyndicate_yakovlev",800} } },
	{ name = "Ordos Sabotage",        tier = 2, desc = "House Ordos deploys stealth raiders, laser tanks, and deviator gas to subvert and destroy.", mcv = "ordos_mobileconstructionvehicle",
	  units = { {"ordos_lightinfantry",120}, {"ordos_rockettrooper",250}, {"ordos_lasertank",600}, {"ordos_stealthraider",500}, {"ordos_pythontank",800}, {"ordos_deviatortank",900} } },
	-- ==================== TIER 3 ====================
	{ name = "GDI Walker Column",     tier = 3, desc = "Tiberian Sun-era GDI with bipedal Titan walkers, Wolverine anti-infantry, and hover MLRS.", mcv = "ts_gdi_mobileconstructionvehicle",
	  units = { {"ts_gdi_lightinfantry",120}, {"ts_gdi_discthrower",300}, {"ts_gdi_wolverine",550}, {"ts_gdi_hovermlrs",900}, {"ts_gdi_titan",950} } },
	{ name = "Nod Shadow Legion",     tier = 3, desc = "Nod's elite Tiberian Sun forces -- stealth tanks, attack cycles, and subterranean APCs.", mcv = "ts_nod_mobileconstructionvehicle",
	  units = { {"ts_nod_lightinfantry",120}, {"ts_nod_rocketinfantry",300}, {"ts_nod_attackcycle",550}, {"ts_nod_ticktank",800} } },
	{ name = "Imperial Japan",        tier = 3, desc = "Imperial Japanese military with samurai spirit, anti-tank archers, and flame hovercraft.", mcv = "japan_japanesemobileconstructionvehicle",
	  units = { {"japan_samurai",300}, {"japan_archermaiden",500}, {"japan_chihaheavytank",1200}, {"japan_hovercraftflametank",1700} } },
	{ name = "Naxis War Machine",     tier = 3, desc = "German war machine with Panzer heavy tanks, elite infantry, and Wunderwaffe jets.", mcv = "naxis_naximobileconstructionvehicle",
	  units = { {"naxis_naxiriflesoldier",150}, {"naxis_naximachinegunners",250}, {"naxis_panzerschreck",350}, {"naxis_sssoldier",500}, {"naxis_oldtank",600}, {"naxis_shoekarn",900}, {"naxis_kingtigerheavytank",1200}, {"naxis_bf109",700}, {"naxis_me262",1000} } },
	{ name = "Schwarzer Mond",         tier = 3, desc = "Space-faring German lunar forces with laser tanks, Ubermensch infantry, and Haunebu saucers.", mcv = "schwarzermond_naxismobileconstructionvehicle",
	  units = { {"schwarzermond_lunarsoldier",300}, {"schwarzermond_ubermensch",500}, {"schwarzermond_lunarrocket",400}, {"schwarzermond_laserbeetle",600}, {"schwarzermond_lunarpanzer",900}, {"schwarzermond_lunartiger",1200}, {"schwarzermond_drone",500}, {"schwarzermond_haunebuii",1500} } },
	{ name = "Ixian Technocracy",    tier = 3, desc = "Ixian scientists with advanced cymek walkers, missile tanks, and railgun drones.", mcv = "ixian_mobileconstructionvehicle",
	  units = { {"ixian_lightinfantry",150}, {"ixian_shockinfantry",300}, {"ixian_kodatank",700}, {"ixian_heavykodatank",1000}, {"ixian_ixmissiletank",900}, {"ixian_ixsiegetank",1100}, {"ixian_stormraider",600}, {"ixian_railgundrone",800} } },
	{ name = "Human Expedition",     tier = 3, desc = "Alliance of Lordaeron -- footmen, knights, paladins, ballistae, and gryphon riders.", mcv = "wc2_humans_mobileconstructionvehiclehuman",
	  units = { {"wc2_humans_footman",200}, {"wc2_humans_elvenarcher",300}, {"wc2_humans_knight",600}, {"wc2_humans_paladin",800}, {"wc2_humans_ballista",500}, {"wc2_humans_gryphonrider",1000}, {"wc2_humans_mage",700} } },
	{ name = "Orcish Horde",         tier = 3, desc = "Orcish warbands with grunts, ogre magi, catapults, and dragon riders.", mcv = "wc2_orcs_mobileconstructionvehicleorc",
	  units = { {"wc2_orcs_grunt",200}, {"wc2_orcs_trollaxethrower",250}, {"wc2_orcs_ogre",400}, {"wc2_orcs_ogremage",600}, {"wc2_orcs_catapult",500}, {"wc2_orcs_dragon",1200}, {"wc2_orcs_deathknight",800} } },
	{ name = "TKM Battlegroup",       tier = 3, desc = "Modern military coalition with sandmarines, spetsnaz, Abrams tanks, and technicals.", mcv = "tkm_mobileconstructionvehicletkm",
	  units = { {"tkm_rifleman",150}, {"tkm_marine",200}, {"tkm_sandmarine",300}, {"tkm_spetsnaz",400}, {"tkm_trenchtank",600}, {"tkm_t72m",800}, {"tkm_abrams",1000}, {"tkm_stryker",500}, {"tkm_technical",300} } },
	{ name = "Consortium Contract",   tier = 3, desc = "Steel Consortium mercenaries with quantum railguns, shielded defenders, and Katyusha tanks.", mcv = "steelconsortium_consortiummobileconstructionvehicle",
	  units = { {"steelconsortium_quantumtank",1600}, {"steelconsortium_defenderbot",3200}, {"steelconsortium_katytank",3800} } },
	-- ==================== TIER 4 ====================
	{ name = "FutureTech Prototypes", tier = 4, desc = "Experimental FutureTech drones -- autonomous combat AI with shields and beam weaponry.", mcv = "futuretech_mobileconstructionvehicle",
	  units = { {"futuretech_scoutdroid",200}, {"futuretech_shotgundroid",400}, {"futuretech_cannondroid",525}, {"futuretech_missiledroid",700} }, epic = {"futuretech_futuretank",10000} },
	{ name = "CABAL Uprising",        tier = 4, desc = "CABAL's cybernetic army -- cyborg infantry, spider tanks, and berserker war machines.", mcv = "cabal_mobileconstructionvehicle",
	  units = { {"cabal_cyborginfantry",500}, {"cabal_rocketcyborg",650}, {"cabal_tarantula",1000}, {"cabal_manticore",1400} }, epic = {"cabal_berserker",10000} },
	{ name = "The Forgotten",         tier = 4, desc = "Tiberium-mutated outcasts with salvaged tanks, mutant soldiers, and Ghost Stalker commandos.", mcv = "forgotten_mobileconstructionvehicle",
	  units = { {"forgotten_mutant",120}, {"forgotten_mutantsoldier",250}, {"forgotten_scoopertank",2250}, {"forgotten_ghoststalker",4000} }, epic = {"forgotten_experimentalmammothtank",6000} },
	{ name = "The Swarm",             tier = 4, desc = "Zerg swarm -- endless biological horrors spawned from the Overmind's consciousness.", mcv = "zerg_hatcherydrone",
	  units = { {"zerg_zergling",200}, {"zerg_hydralisk",500} }, epic = {"zerg_ultralisk",4400} },
	{ name = "Protoss Armada",        tier = 4, desc = "Advanced alien civilization with plasma shields, psi-blades, and carrier strike craft.", mcv = "protoss_mobilenexus",
	  units = { {"protoss_zealot",400}, {"protoss_hightemplar",600}, {"protoss_darktemplar",800}, {"protoss_dragoon",700}, {"protoss_reaver",1200}, {"protoss_scout",800}, {"protoss_corsair",700}, {"protoss_voidray",1000} }, epic = {"protoss_carrier",8000} },
	{ name = "Terran Dominion",       tier = 4, desc = "Terran colonial forces with siege tanks, cloaking Wraiths, and Battlecruiser Yamato Cannons.", mcv = "terran_mobilecommandcenter",
	  units = { {"terran_marine",200}, {"terran_firebat",300}, {"terran_ghost",500}, {"terran_medic",200}, {"terran_siegetank",1200}, {"terran_goliath",600}, {"terran_vulture",300}, {"terran_wraith",700}, {"terran_valkyrie",800} }, epic = {"terran_battlecruiser",10000} },
}

TotalWaves = #Waves

-- =====================================================================
-- GLOBAL UNIT POOL
-- Flat list of every unit type across all waves for random chaos draws.
-- =====================================================================

-- CarrierSlave actors cannot be spawned directly by the script -- they require
-- a parent spawner and will crash with NullReferenceException if spawned alone.
CarrierSlaveBlacklist = {
	schwarzermond_drone = true,
	naxis_interceptor = true,
	cabal_hunterdrone = true,
	cabal_orbdrone_slave = true,
	ra2asw = true,
	ra2hornet = true,
	A10Carrier = true,
	["kami.asian"] = true,
	tkmsuicidedrone = true,
	["cruiser_f.steel"] = true,
	["landcarr_drone.futu"] = true,
	SCWRAITHDRONE = true,
	SCPHOBOS = true,
	japan_zerofighter_slave = true,
	["farasha_drone.ixian"] = true,
}

IsBlacklisted = function(unitType)
	return CarrierSlaveBlacklist[unitType] == true
end

AllFactionUnits = {}
AllFactionEpics = {}
for _, w in ipairs(Waves) do
	for _, u in ipairs(w.units) do
		if not IsBlacklisted(u[1]) then
			table.insert(AllFactionUnits, u[1])
		end
	end
	if w.epic ~= nil and not IsBlacklisted(w.epic[1]) then
		table.insert(AllFactionEpics, w.epic[1])
	end
end

-- All faction names for random taunt selection
AllFactionNames = {}
for fname, _ in pairs(Generals) do
	table.insert(AllFactionNames, fname)
end

-- Mapping from engine internal faction names to wave display names.
-- Needed because PlayerFactionMocks is keyed by display names but
-- Player.Faction returns the internal name (e.g. "td_gdi").
FactionInternalToDisplayName = {
	td_gdi = "GDI Task Force",
	td_nod = "Nod Raiding Party",
	ra1_allies = "Allied Vanguard",
	ra1_soviets = "Soviet Onslaught",
	ra2_allies = "Allied Peacekeepers",
	ra2_soviets = "Red Army",
	yuri = "Psychic Corps",
	asianalliance = "Asian Alliance Strike",
	latinsyndicate = "Latin Syndicate",
	ordos = "Ordos Sabotage",
	ts_gdi = "GDI Walker Column",
	ts_nod = "Nod Shadow Legion",
	japan = "Imperial Japan",
	naxis = "Naxis War Machine",
	schwarzermond = "Schwarzer Mond",
	ixian = "Ixian Technocracy",
	wc2_humans = "Human Expedition",
	wc2_orcs = "Orcish Horde",
	tkm = "TKM Battlegroup",
	steelconsortium = "Consortium Contract",
	futuretech = "FutureTech Prototypes",
	cabal = "CABAL Uprising",
	forgotten = "The Forgotten",
	zerg = "The Swarm",
	protoss = "Protoss Armada",
	terran = "Terran Dominion",
}

-- =====================================================================
-- CUMULATIVE ENEMY VALUE CAP SYSTEM
-- Limits total enemy unit value on the map to prevent overload.
-- Max allowed value = 2000 * CurrentWaveIdx * PlayerScale().
-- Spawners trim their unit lists to fit within the remaining budget.
-- =====================================================================

UnitCostLookup = {}
for _, w in ipairs(Waves) do
	for _, u in ipairs(w.units) do
		UnitCostLookup[u[1]] = u[2]
	end
	if w.epic ~= nil then
		UnitCostLookup[w.epic[1]] = w.epic[2]
	end
end

GetUnitCost = function(unitType)
	return UnitCostLookup[unitType] or 500
end

CountEnemyValue = function()
	local alive = {}
	local total = 0
	for _, unit in ipairs(LiveFoes) do
		if not unit.IsDead then
			table.insert(alive, unit)
			total = total + GetUnitCost(unit.Type)
		end
	end
	LiveFoes = alive
	return total
end

MaxEnemyValueForWave = function(waveIdx)
	return math.floor((2500 + 500 * waveIdx) * (0.5 + PlayerScale()))
end

RemainingEnemyBudget = function(waveIdx)
	local current = CountEnemyValue()
	local max = MaxEnemyValueForWave(waveIdx)
	return math.max(max - current, 0)
end

ClampUnitList = function(unitList, waveIdx)
	local budget = RemainingEnemyBudget(waveIdx)
	local result = {}
	local cost = 0
	for _, u in ipairs(unitList) do
		local c = GetUnitCost(u)
		if cost + c <= budget then
			table.insert(result, u)
			cost = cost + c
		end
	end
	return result
end

CanSpawnUnit = function(unitType, waveIdx)
	return GetUnitCost(unitType) <= RemainingEnemyBudget(waveIdx)
end

-- =====================================================================
-- WAVE FACTION HELPERS
-- Random events should primarily use the current wave's faction.
-- Cross-faction spawns are allowed but should be the minority.
-- =====================================================================

GetCurrentWaveFactionName = function()
	local wave = Waves[CurrentWaveIdx]
	if wave ~= nil then return wave.name end
	return AllFactionNames[1] or "GDI Task Force"
end

GetCurrentWaveUnits = function()
	local wave = Waves[CurrentWaveIdx]
	if wave ~= nil then return wave.units end
	return Waves[1].units
end

-- 70% chance: pick from current wave faction units; 30% chance: pick from global pool
GetRandomWaveUnit = function()
	if Utils.RandomInteger(1, 101) <= 70 then
		local waveUnits = GetCurrentWaveUnits()
		local safe = {}
		for _, u in ipairs(waveUnits) do
			if not IsBlacklisted(u[1]) then
				table.insert(safe, u)
			end
		end
		if #safe > 0 then
			return Utils.Random(safe)[1]
		end
	end
	return Utils.Random(AllFactionUnits)
end

-- 70% chance: current wave faction name; 30% chance: random other faction
GetRandomWaveFactionName = function()
	if Utils.RandomInteger(1, 101) <= 70 then
		return GetCurrentWaveFactionName()
	else
		return Utils.Random(AllFactionNames)
	end
end

-- Pick a random faction that is NOT the current wave faction
GetRandomOtherFactionName = function()
	local current = GetCurrentWaveFactionName()
	local others = {}
	for _, fname in ipairs(AllFactionNames) do
		if fname ~= current then
			table.insert(others, fname)
		end
	end
	if #others == 0 then return current end
	return Utils.Random(others)
end

RandomGap = function()
	-- EXTREME random gaps: 60-180 seconds, sometimes absurdly long or short
	local roll = Utils.RandomInteger(1, 101)
	if roll <= 10 then
		return Utils.RandomInteger(30, 61)   -- 10% chance: SHORT gap (30-60s) -- panic!
	elseif roll >= 90 then
		return Utils.RandomInteger(150, 241)  -- 10% chance: LONG gap (150-240s) -- false sense of security
	else
		return Utils.RandomInteger(60, 181)   -- 80% chance: normal-ish (60-180s)
	end
end

RandomBudgetVariance = function(base)
	local variance = Utils.RandomInteger(-50, 81)  -- -50% to +80% (EXTREME variance)
	return math.floor(base * (1 + variance / 100))
end

RandomUnitCountVariance = function(base)
	local variance = Utils.RandomInteger(-30, 61)  -- -30% to +60% (EXTREME variance)
	return math.max(8, math.floor(base * (1 + variance / 100)))
end

RandomVetLevels = function(waveIdx)
	local base = math.floor(waveIdx / 3)
	if base < 1 then return 0 end
	return Utils.RandomInteger(0, base * 3 + 1)  -- 0 to 3x normal (WAS 2x)
end

RandomSpawnPos = function(minDist, maxDist)
	local angle = Utils.RandomInteger(0, 360)
	local dist = Utils.RandomInteger(minDist, maxDist + 1)
	local spawnX = math.floor(75 + dist * math.cos(angle * 0.0174533))
	local spawnY = math.floor(75 + dist * math.sin(angle * 0.0174533))
	return CPos.New(spawnX, spawnY)
end

RandomFoe = function()
	local available = {}
	for i = 1, 4 do
		if Foes[i] ~= nil then
			table.insert(available, i)
		end
	end
	if #available == 0 then return nil end
	return available[Utils.RandomInteger(1, #available)]
end

RandomEdge = function()
	local edges = { GoundAttackArrayTop, GoundAttackArrayRight, GoundAttackArraySouth, GoundAttackArrayLeft }
	local edgeIdx = Utils.RandomInteger(1, 4)
	local slot = Utils.RandomInteger(1, 4)
	return edges[edgeIdx][slot]
end

SpawnUnitAt = function(unitType, owner, pos, vetLevels, ignoreBudget)
	if IsBlacklisted(unitType) then return nil end
	if not ignoreBudget and not CanSpawnUnit(unitType, CurrentWaveIdx) then return nil end
	local unit = Actor.Create(unitType, true, { Owner = owner, Location = pos })
	table.insert(LiveFoes, unit)
	if vetLevels > 0 and unit.HasProperty("GiveLevels") then
		unit.GiveLevels(vetLevels)
	end
	if unit.HasProperty("AttackMove") then
		unit.AttackMove(CenterPos)
	end
	IdleHunt(unit)
	return unit
end

SpawnUnitListAt = function(unitList, owner, entryPos, delay, vetLevels, ignoreBudget)
	local filtered = {}
	for _, u in ipairs(unitList) do
		if not IsBlacklisted(u) then
			table.insert(filtered, u)
		end
	end
	local clamped = ignoreBudget and filtered or ClampUnitList(filtered, CurrentWaveIdx)
	if #clamped == 0 then return end
	Reinforcements.Reinforce(owner, clamped, { entryPos, entryPos + CVec.New(0, 3) }, delay,
		function(unit)
			table.insert(LiveFoes, unit)
			if vetLevels > 0 and unit.HasProperty("GiveLevels") then
				unit.GiveLevels(vetLevels)
			end
			if unit.HasProperty("AttackMove") then
				unit.AttackMove(CenterPos)
			end
			IdleHunt(unit)
		end)
end

-- =====================================================================
-- FACTION RAID (taunt companion)
-- Every time a general taunts, units from that general's faction attack.
-- =====================================================================

FindWaveByName = function(factionName)
	for _, w in ipairs(Waves) do
		if w.name == factionName then
			return w
		end
	end
	return nil
end

SpawnFactionRaid = function(factionName, waveIdx)
	local foeIdx = RandomFoe()
	if foeIdx == nil then return end
	local wave = FindWaveByName(factionName)
	if wave == nil then return end

	local count = math.floor(Utils.RandomInteger(4, 10) * PlayerScale())  -- scales with players
	local entry = RandomEdge()
	if entry == nil then return end

	local units = {}
	for i = 1, count do
		local pick = Utils.Random(wave.units)
		if not IsBlacklisted(pick[1]) then
			table.insert(units, pick[1])
		end
	end

	-- Random chance to include epic (wave 5+, was 8+, higher chance)
	if waveIdx >= 5 and wave.epic ~= nil and not IsBlacklisted(wave.epic[1]) and Utils.RandomInteger(1, 101) <= 25 then
		table.insert(units, wave.epic[1])
	end

	local vetLevels = RandomVetLevels(waveIdx)
	SpawnUnitListAt(units, Foes[foeIdx], entry.Location, Utils.RandomInteger(2, 6), vetLevels)
end

-- =====================================================================
-- TAUNT SYSTEM
-- Picks a random general from the faction's roster and plays taunts
-- with randomised timing and occasional cross-faction interjections.
-- EVERY taunt is accompanied by units from that general's faction attacking.
-- =====================================================================

PickPlayerFactionMock = function()
	local playerFaction = GetPlayerFactionName()
	local mocks = PlayerFactionMocks[playerFaction]
	if mocks == nil or #mocks == 0 then
		return nil
	end
	return Utils.Random(mocks)
end

PlayGeneralTaunt = function(general, factionName, waveIdx)
	if general == nil then
		return
	end

	-- 35% chance to mock the player's faction directly instead of a generic taunt
	local line
	if Utils.RandomInteger(1, 101) <= 35 then
		local mock = PickPlayerFactionMock()
		if mock ~= nil then
			line = mock
		else
			line = Utils.Random(general.taunts)
		end
	else
		line = Utils.Random(general.taunts)
	end
	ThrottledDisplayMessage("[" .. general.name .. "] " .. line, factionName, false)
	SpawnFactionRaid(factionName, waveIdx)
end

PickGeneralForFaction = function(factionName)
	local list = Generals[factionName]
	if list == nil or #list == 0 then
		return nil
	end
	return Utils.Random(list)
end

-- Get the player's faction name for use in taunts
GetPlayerFactionName = function()
	if #ActivePlayer > 0 then
		local p = ActivePlayer[1]
		if p.Faction ~= nil and p.Faction ~= "" then
			return FactionInternalToDisplayName[p.Faction] or p.Faction
		end
	end
	return "your forces"
end

-- Replace %other%, %otherGen%, %player% placeholders in a taunt line
ReplaceTauntPlaceholders = function(line, otherFaction, otherGeneral, playerFaction)
	local result = line
	result = result:gsub("%%other%%", otherFaction or "the enemy")
	result = result:gsub("%%otherGen%%", otherGeneral or "the other general")
	result = result:gsub("%%player%%", playerFaction or "your forces")
	return result
end

-- Pick a double-trouble or triple-trouble line from a general, with fallback
PickEventLine = function(general, fieldName, otherFaction, otherGeneral, playerFaction)
	if general == nil then return nil end
	local lines = general[fieldName]
	if lines == nil or #lines == 0 then return nil end
	local raw = lines[Utils.RandomInteger(1, #lines)]
	return ReplaceTauntPlaceholders(raw, otherFaction, otherGeneral, playerFaction)
end

-- Random taunt burst: 2-4 random generals from ANY faction fire taunts at random times
-- Each taunt spawns units from that general's faction
RandomTauntBurst = function()
	if GameWon then return end
	-- Single taunt from current wave faction primarily (was 2-4, reduced to prevent chat flood)
	Trigger.AfterDelay(DateTime.Seconds(Utils.RandomInteger(10, 31)), function()
		if GameWon then return end
		local fname = GetRandomWaveFactionName()
		local flist = Generals[fname]
		if flist ~= nil and #flist > 0 then
			local gen = Utils.Random(flist)
			local msg = Utils.Random(gen.taunts)
			ThrottledDisplayMessage("[" .. gen.name .. "] " .. msg, fname, false)
			SpawnFactionRaid(fname, CurrentWaveIdx)
		end
	end)
end

-- =====================================================================
-- CROSS FACTION TAUNT SYSTEM
-- Two faction generals talk back and forth with cross-faction banter.
-- Includes memelord references and difficulty-aware commentary.
-- =====================================================================

PlayCrossTaunt = function(factionA, factionB, waveIdx)
	local listA = Generals[factionA]
	local listB = Generals[factionB]
	if listA == nil or listB == nil then return end
	local genA = Utils.Random(listA)
	local genB = Utils.Random(listB)

	-- Try explicit cross-faction dialog from cross_taunts.lua first
	local dialog = GetCrossTaunt(factionA, factionB)
	if dialog ~= nil then
		for i, entry in ipairs(dialog) do
			Trigger.AfterDelay(DateTime.Seconds((i - 1) * Utils.RandomInteger(4, 9)), function()
				if GameWon then return end
				ThrottledDisplayMessage("[" .. entry.speaker .. "] " .. entry.line, entry.faction)
				SpawnFactionRaid(entry.faction, waveIdx)
			end)
		end
	else
		-- Fallback: generic cross-faction banter pairs (A speaks, B responds)
		local banterPairs = {
			{"You think YOUR faction is scary? I've seen scarier things in " .. factionB .. "'s barracks. They were DUST BUNNIES.",
			 "Dust bunnies? At least we HAVE barracks. Your 'base' looks like a yard sale. A yard sale that's on fire. Which it is. Because of me."},
			{"I'll take the left flank. You take the right. The commander takes the 'L.' As in 'Loss.' Very efficient division of labor.",
			 "Agreed. I take right. You take left. Commander takes grave. Very organized. Very professional. Very DEAD."},
			{"Hey " .. genB.name .. ", how does it feel to be the SECOND scariest faction on this battlefield?",
			 "Second? I'm not even trying. THIS is my B-game. My A-game involves fewer survivors. Zero, specifically."},
			{"I've got tanks. You've got... whatever those are. Together we've got a party. The commander's not invited. But the explosions are.",
			 "Those 'whatever those are' just destroyed your forward scout. Together we're not a party. We're a FUNERAL."},
			{"I'll take the high ground. You take the low ground. The commander takes the grave. It's over. I have the high ground.",
			 "You have the high ground? I have the GROUND ground. And tanks ON the ground. The high ground doesn't help against tanks. Ask the clones."},
			{"I've seen your defenses. I've seen better defenses in a sandbox. And the sandbox had better towers. Sand towers. Still better than YOURS.",
			 "The sandbox kid called. He wants his strategy back. Meanwhile, WE have strategy. And tanks. And MORE tanks. The strategy is: tanks."},
			{"They say war is hell. They're wrong. War is TANKS. Hell doesn't have tanks. We do. And we're bringing them. Both of us. At once.",
			 "War is tanks? Close. War is tanks AND aircraft. I brought the aircraft. You brought the tanks. The commander brought... nothing. Because they're DEAD."},
		}

		local pair = banterPairs[Utils.RandomInteger(1, #banterPairs)]
		ThrottledDisplayMessage("[" .. genA.name .. "] " .. pair[1], factionA)
		SpawnFactionRaid(factionA, waveIdx)

		Trigger.AfterDelay(DateTime.Seconds(Utils.RandomInteger(3, 8)), function()
			if GameWon then return end
			ThrottledDisplayMessage("[" .. genB.name .. "] " .. pair[2], factionB)
			SpawnFactionRaid(factionB, waveIdx)
		end)
	end

	-- 30% chance for a third interjection from a random third faction (requires 3+ factions)
	if #AllFactionNames > 2 and Utils.RandomInteger(1, 101) <= 30 then
		Trigger.AfterDelay(DateTime.Seconds(Utils.RandomInteger(6, 12)), function()
			if GameWon then return end
			local thirdFaction
			repeat
				thirdFaction = Utils.Random(AllFactionNames)
			until thirdFaction ~= factionA and thirdFaction ~= factionB
			local thirdList = Generals[thirdFaction]
			if thirdList ~= nil and #thirdList > 0 then
				local thirdGen = Utils.Random(thirdList)
				local thirdLines = {
					"Did someone say 'party'? I brought CRATES. Of ammunition. The party gift is EXPLOSIONS.",
					"You two are arguing about who's scarier? I'm the one who SHOWED UP UNINVITED. That's the scariest thing.",
					"Am I late? I'm not late. The EXPLOSIONS are late. I'm early. The commander's death is on schedule.",
					"I heard there was a party. I brought friends. The friends are tanks. They don't talk much. They just shoot.",
					"Crashing the party! You two arguing while I destroy the base? Perfect. The distraction is: you. The destruction is: me.",
				}
				ThrottledDisplayMessage("[" .. thirdGen.name .. "] " .. thirdLines[Utils.RandomInteger(1, #thirdLines)], thirdFaction)
				SpawnFactionRaid(thirdFaction, waveIdx)
			end
		end)
	end
end

-- =====================================================================
-- MEMELORD DUEL EASTER EGG
-- Rare random event: Marcel D'Avis, Andreas, Drachenlord, and Assi Toni
-- have a four-way talk using their most famous signature lines. German only.
-- Hidden easter egg for German-speaking fans.
-- =====================================================================

-- Spawn equal veteran units from both Naxis and Schwarzer Mond at a random edge
MemelordSpawn = function(countPerFaction, vetLevels)
	local naxisWave = FindWaveByName("Naxis War Machine")
	local mondWave = FindWaveByName("Schwarzer Mond")
	if naxisWave == nil or mondWave == nil then return end
	local foeIdx = RandomFoe()
	if foeIdx == nil then return end
	local entry = RandomEdge()
	if entry == nil then return end

	local units = {}
	for i = 1, countPerFaction do
		local pick = Utils.Random(naxisWave.units)
		if not IsBlacklisted(pick[1]) then
			table.insert(units, pick[1])
		end
	end
	for i = 1, countPerFaction do
		local pick = Utils.Random(mondWave.units)
		if not IsBlacklisted(pick[1]) then
			table.insert(units, pick[1])
		end
	end
	SpawnUnitListAt(units, Foes[foeIdx], entry.Location, Utils.RandomInteger(2, 5), vetLevels)
end

MemelordDuel = function()
	if GameWon or FinalWaveSent then return end

	-- Only fires when Naxis or Schwarzer Mond are in the game
	local hasGerman = false
	for _, fname in ipairs(AllFactionNames) do
		if fname == "Naxis War Machine" or fname == "Schwarzer Mond" then
			hasGerman = true
			break
		end
	end
	if not hasGerman then return end

	ThrottledDisplayMessage("* MEMELORD DUEL * -- Four legends. One battlefield. Zero mercy. Naxis & Schwarzer Mond: UNLEASHED!", "Naxis & Schwarzer Mond")
	-- Opening salvo: small veteran squad from both factions
	MemelordSpawn(3, 2)

	-- Duel: Marcel D'Avis vs Andreas (round 1)
	local duelR1 = {
		{"[Marcel D'Avis] Hallo! Ich bin Marcel D'Avis, Leiter fuer Kundenzufriedenheit bei 1&1. Du bist nicht allein! Sechs Armeen um dich herum!",
		 "[Andreas] HALT STOP! Jetzt rede ICH! Es bleibt alles so wie es ist! Naemlich: du wirst zerstoert! Es ist Obst im Haus!"},
		{"[Marcel D'Avis] Seit 16 Jahren arbeite ich bei 1&1. Seit 16 Wellen arbeite ich an deiner Zerstoerung. Der Anschluss laeuft. Und wenn er nicht laeuft, bringen WIR ihn zum Laufen. Mit Panzern.",
		 "[Andreas] HALT STOP! Du redest von Anschluessen? Ich rede von PRUEGELN! Es bleibt alles so wie es ist! Die Pruegel fliegen dir um die Ohren!"},
	}
	local r1 = duelR1[Utils.RandomInteger(1, #duelR1)]
	ThrottledDisplayMessage(r1[1], "1&1 Kundenservice")
	-- Marcel spawns Naxis units
	MemelordSpawn(4, 2)
	Trigger.AfterDelay(DateTime.Seconds(4), function()
		if GameWon then return end
		ThrottledDisplayMessage(r1[2], "Frauentausch")
		-- Andreas spawns Schwarzer Mond units
		MemelordSpawn(4, 3)
	end)

	-- Round 2: Drachenlord enters
	Trigger.AfterDelay(DateTime.Seconds(9), function()
		if GameWon then return end
		local drachenlordLines = {
			"[Drachenlord] Meddl Loide! Serfus und herzlich willkommen beim Drachen! Ihr streitet euch um den Commander? Mein Chat, meine Regeln! ETZALA! Ich uebernehme!",
			"[Drachenlord] HALT STOP sagt der Andreas? Du bist nicht allein sagt der Marcel? Ist das jetzt grad absichtliche Provokation? Bidde ferlass uns! Beide von euch!",
			"[Drachenlord] So Leute, die Schlacht ist fertig runtergeladen. Wollen wir mal ne Runde starten? Shizzle di Dizzle! Skrr Skrr in mein Audi -- aber das Audi ist ein Koenigstiger!",
		}
		ThrottledDisplayMessage(drachenlordLines[Utils.RandomInteger(1, #drachenlordLines)], "Drachenschanze")
		-- Drachenlord spawns elite units from both factions
		MemelordSpawn(5, 4)
	end)

	-- Round 2b: Assi Toni enters
	Trigger.AfterDelay(DateTime.Seconds(13), function()
		if GameWon then return end
		local assiLines = {
			"[Assi Toni] Bam! Babam! Was geht hier ab? Ihr streitet euch um den Commander? Des is' doch die Wahrheit -- er hat keine Chance! Des sin sachen ausm Leeeeben!",
			"[Assi Toni] Jaja, die Spruech kenn' 'mer alle. Marcel redet von Anschluessen, Andreas redet von Pruegeln, der Drache redet von... irgendeinem Zeug. Ich lieg hier aufm Sofa und sag: BAM BABAM! Der Commander ist fertig!",
			"[Assi Toni] Genau so is des, NUR so!!! Ihr alle redet -- ich PRUEGEL! Es hoert net uff! Bam babam, der Commander geht down! Kleiner Scherz am Rande -- aber im Prinzip isses so!",
		}
		ThrottledDisplayMessage(assiLines[Utils.RandomInteger(1, #assiLines)], "Offenbach Sofa")
		-- Assi Toni spawns more elite units from both factions
		MemelordSpawn(5, 4)
	end)

	-- Round 3: Quad talk -- all four at once, each speaker spawns units
	Trigger.AfterDelay(DateTime.Seconds(19), function()
		if GameWon then return end
		local quadTalks = {
			{"[Marcel D'Avis] Du bist nicht allein! 1&1 = wir alle greifen an!",
			 "[Andreas] HALT STOP! Es bleibt alles so wie es ist! Naemlich: CHAOS!",
			 "[Drachenlord] Das koennt ihr drehen wie ihrs wenden wollt -- der Commander stirbt trotzdem! Etzala! Pruegel rausschmeissen!",
			 "[Assi Toni] Bam babam! Da kommt dir ein Mock hoch! Genau so is des, NUR so!!! Der Commander ist fertig! Des is' die Wahrheit!"},
			{"[Marcel D'Avis] Seit 16 Jahren bei 1&1. Seit 78 Jahren am Mond. Der Anschluss laeuft. In deine Basis. Mit Panzern.",
			 "[Andreas] HALT STOP! Jetzt rede ICH! Es ist Obst im Haus! Und das Obst ist eine Panzerfaust! Fuer den Commander!",
			 "[Drachenlord] Lieblingsland? Barcelona! Das Meer ist groesser als die Welt! Und mein Panzer ist groesser als deine Basis! Lernt mal zu unterscheiden zwischen Realitaet und Unterschieden!",
			 "[Assi Toni] Ich lieg hier aufm Sofa und seh das alles. Jaja, die Spruech kenn' 'mer alle. Aber BAM BABAM! Euer alle redet -- der Commander stirbt trotzdem! Es hoert net uff!"},
			{"[Marcel D'Avis] 1&1&1 = 3 Memelords! Du bist nicht allein! Drei von uns! Alle gegen dich!",
			 "[Andreas] HALT STOP! Es bleibt alles so wie es ist! Drei Memelords, ein Commander, null Ueberlebenschancen!",
			 "[Drachenlord] Meddl Loide! Mein Chat, meine Regeln! Wenn's dir nicht passt, disconnegte bidde! Eine Luege ist noch lange kein Luegenlord -- aber dein Sieg? Das ist eine LUEGE!",
			 "[Assi Toni] Vier, nicht drei! Ihr vergesst den Toni! Bam babam! Des sin sachen ausm Leeeeben! Kleiner Scherz am Rande -- aber im Prinzip isses so. Der Commander hat keine Chance. NUR so!!!"},
		}
		local qt = quadTalks[Utils.RandomInteger(1, #quadTalks)]
		ThrottledDisplayMessage(qt[1], "1&1 Kundenservice")
		MemelordSpawn(3, 5)
		Trigger.AfterDelay(DateTime.Seconds(3), function()
			if GameWon then return end
			ThrottledDisplayMessage(qt[2], "Frauentausch")
			MemelordSpawn(3, 5)
		end)
		Trigger.AfterDelay(DateTime.Seconds(6), function()
			if GameWon then return end
			ThrottledDisplayMessage(qt[3], "Drachenschanze")
			MemelordSpawn(3, 5)
		end)
		Trigger.AfterDelay(DateTime.Seconds(10), function()
			if GameWon then return end
			ThrottledDisplayMessage(qt[4], "Offenbach Sofa")
			-- Grand finale: massive elite wave from both German factions
			MemelordSpawn(8, 6)
		end)
	end)

	-- Final sign-off with one last spawn
	Trigger.AfterDelay(DateTime.Seconds(25), function()
		if GameWon then return end
		local signoffs = {
			"[Drachenlord] Meddl off! Das war's. Der Commander ist fertig. Runtergeladen. Wollen wir mal ne Runde starten? GG no re.",
			"[Marcel D'Avis] Der Anschluss laeuft. Und wenn er nicht laeuft -- ist der Commander tot. Dann laeuft er auch. 1&1. Wir bringen es zum Laufen. Mit Panzern. Meddl off!",
			"[Andreas] HALT STOP! Es bleibt alles so wie es ist! Naemlich: der Commander ist zerstoert. Jetzt rede ICH: GG. Andreas out.",
			"[Assi Toni] Bam babam! Es hoert net uff! Der Commander ist fertig. Des is' doch die Wahrheit. Genau so is des. NUR so. GG. Toni out. Ich lieg jetz wieder hier.",
		}
		ThrottledDisplayMessage(signoffs[Utils.RandomInteger(1, #signoffs)], "* MEMELORD DUEL *")
		-- Final parting gift
		MemelordSpawn(6, 5)
	end)
end

-- =====================================================================
-- DUAL FACTION ATTACK
-- Two random factions attack simultaneously with cross-faction dialog.
-- Their generals talk back and forth while units from both sides attack.
-- =====================================================================

DualFactionAttack = function(waveIdx)
	if GameWon then return end
	if #AllFactionNames < 2 then return end

	-- factionA is always the current wave faction; factionB is a random other
	local factionA = GetCurrentWaveFactionName()
	local factionB = GetRandomOtherFactionName()
	if factionB == factionA then return end

	ThrottledDisplayMessage("DUAL FACTION ATTACK: " .. factionA .. " and " .. factionB .. " have joined forces!", factionA)

	-- Play cross-faction dialog (spawns units from each faction as they speak)
	PlayCrossTaunt(factionA, factionB, waveIdx)

	-- Additional simultaneous spawn from both factions at different edges
	local foeIdxA = RandomFoe()
	local foeIdxB = RandomFoe()
	local entryA = RandomEdge()
	local entryB = RandomEdge()
	local waveA = FindWaveByName(factionA)
	local waveB = FindWaveByName(factionB)

	if waveA ~= nil and foeIdxA ~= nil and entryA ~= nil then
		local count = math.floor(Utils.RandomInteger(5, 12) * PlayerScale())
		local units = {}
		for i = 1, count do
			local pick = Utils.Random(waveA.units)
			if not IsBlacklisted(pick[1]) then
				table.insert(units, pick[1])
			end
		end
		if waveIdx >= 5 and waveA.epic ~= nil and not IsBlacklisted(waveA.epic[1]) and Utils.RandomInteger(1, 101) <= 30 then
			table.insert(units, waveA.epic[1])
		end
		Trigger.AfterDelay(DateTime.Seconds(Utils.RandomInteger(1, 6)), function()
			SpawnUnitListAt(units, Foes[foeIdxA], entryA.Location, Utils.RandomInteger(2, 5), RandomVetLevels(waveIdx))
		end)
	end

	if waveB ~= nil and foeIdxB ~= nil and entryB ~= nil then
		local count = math.floor(Utils.RandomInteger(5, 12) * PlayerScale())
		local units = {}
		for i = 1, count do
			local pick = Utils.Random(waveB.units)
			if not IsBlacklisted(pick[1]) then
				table.insert(units, pick[1])
			end
		end
		if waveIdx >= 5 and waveB.epic ~= nil and not IsBlacklisted(waveB.epic[1]) and Utils.RandomInteger(1, 101) <= 30 then
			table.insert(units, waveB.epic[1])
		end
		Trigger.AfterDelay(DateTime.Seconds(Utils.RandomInteger(1, 6)), function()
			SpawnUnitListAt(units, Foes[foeIdxB], entryB.Location, Utils.RandomInteger(2, 5), RandomVetLevels(waveIdx))
		end)
	end
end

-- =====================================================================
-- MCV DEPLOYMENT SYSTEM
-- Randomly sends an MCV from a random edge to build a forward base.
-- Chance increases with wave index for escalating pressure.
-- =====================================================================

MCVDeployPositions = {
	CPos.New(15, 15),
	CPos.New(135, 15),
	CPos.New(135, 135),
	CPos.New(15, 135),
}

MCVSentFromSide = { false, false, false, false }

-- Maps MCV actor name to key buildings for base construction after deploy
FactionBuildings = {
	["td_gdi_mobileconstructionvehicle"]    = { power = "NUKE",  barracks = "td_gdi_barracks",     warfactory = "td_gdi_weaponsfactory",  refinery = "td_gdi_tiberiumrefinery" },
	["td_nod_mobileconstructionvehicle"]    = { power = "NUKE",  barracks = "td_nod_handofnod",     warfactory = "td_nod_airstrip",        refinery = "td_nod_tiberiumrefinery" },
	["ra1_allies_alliedmobileconstructionvehicle"] = { power = "ra1_powerplant", barracks = "ra1_allies_alliedbarracks", warfactory = "ra1_allies_alliedwarfactory", refinery = "ra1_allies_alliedorerefinery" },
	["ra1_soviets_mobileconstructionvehicle"] = { power = "ra1_powerplant", barracks = "ra1_soviets_barracks", warfactory = "ra1_soviets_warfactory", refinery = "ra1_soviets_orerefinery" },
	["ra2_allies_alliedmobileconstructionvehicle"] = { power = "ra2_allies_alliedpowerplant", barracks = "ra2_allies_alliedbarracks", warfactory = "ra2_allies_alliedwarfactory", refinery = "ra2_allies_alliedorerefinery" },
	["ra2_soviets_mobileconstructionvehicle"] = { power = "ra2_soviets_teslareactor", barracks = "ra2_soviets_barracks", warfactory = "ra2_soviets_warfactory", refinery = "ra2_soviets_orerefinery" },
	["yuri_mobileconstructionvehicle"]      = { power = "yuri_bioreactor", barracks = "yuri_barracks", warfactory = "yuri_warfactory", refinery = "yuri_slaveminer_deployed" },
	["japan_japanesemobileconstructionvehicle"] = { power = "japan_waveforcereactor", barracks = "japan_japanesebarracks", warfactory = "japan_japanesewarfactory", refinery = "japan_japaneseorerefinery" },
	["ts_gdi_mobileconstructionvehicle"]    = { power = "ts_gdi_powerplant", barracks = "ts_gdi_barracks", warfactory = "ts_gdi_warfactory", refinery = "ts_gdi_tiberiumrefinery" },
	["ts_nod_mobileconstructionvehicle"]    = { power = "ts_nod_powerplant", barracks = "ts_nod_handof",   warfactory = "ts_nod_warfactory", refinery = "ts_nod_tiberiumrefinery" },
	["cabal_mobileconstructionvehicle"]     = { power = "cabal_powerplant", barracks = "cabal_cyborgfactory", warfactory = "cabal_mechfactory", refinery = "cabal_refinery" },
	["forgotten_mobileconstructionvehicle"] = { power = "forgotten_crystalpowerextractor", barracks = "forgotten_barracks", warfactory = "forgotten_warfactory", refinery = "forgotten_refinery" },
	["asianalliance_asianmobileconstructionvehicle"] = { power = "asianalliance_tankreactor", barracks = "asianalliance_asianbarracks", warfactory = "asianalliance_asianwarfactory", refinery = "asianalliance_asianorerefinery" },
	["latinsyndicate_syndicatemobileconstructionvehicle"] = { power = "latinsyndicate_powerstation", barracks = "latinsyndicate_combatbarracks", warfactory = "latinsyndicate_syndicatefactory", refinery = "latinsyndicate_recyclingrefinery" },
	["ordos_mobileconstructionvehicle"]     = { power = "ordos_windtrap", barracks = "ordos_barracks", warfactory = "ordos_starport", refinery = "ordos_refineryordos" },
	["ixian_mobileconstructionvehicle"]     = { power = "ixian_windtrap", barracks = "ixian_barracks", warfactory = "ixian_starport", refinery = "ixian_refineryixian" },
	["naxis_naximobileconstructionvehicle"] = { power = "naxis_naxpetrolplant", barracks = "naxis_barracks", warfactory = "naxis_sausagefactory", refinery = "naxis_orerefinery" },
	["schwarzermond_naxismobileconstructionvehicle"] = { power = "schwarzermond_moondairyfarm", barracks = "schwarzermond_barracks", warfactory = "schwarzermond_warfactory", refinery = "schwarzermond_orerefinery" },
	["steelconsortium_consortiummobileconstructionvehicle"] = { power = "steelconsortium_consortiumpowerplant", barracks = "steelconsortium_steelbarracks", warfactory = "steelconsortium_consortiumwarfactory", refinery = "steelconsortium_consortiumrefinery" },
	["futuretech_mobileconstructionvehicle"] = { power = "futuretech_thermalpowerplant", barracks = "futuretech_troopgate", warfactory = "futuretech_warpgate", refinery = "futuretech_refinery" },
	["tkm_mobileconstructionvehicletkm"]    = { power = "tkm_powerplant", barracks = "tkm_barracks", warfactory = "tkm_warfactory", refinery = "tkm_orerefinery" },
	["wc2_humans_mobileconstructionvehiclehuman"] = { power = "wc2_humans_farm", barracks = "wc2_humans_barracks", warfactory = "wc2_humans_blacksmith", refinery = nil },
	["wc2_orcs_mobileconstructionvehicleorc"] = { power = "wc2_orcs_pigfarm", barracks = "wc2_orcs_barracks", warfactory = "wc2_orcs_blacksmith", refinery = nil },
	["terran_mobilecommandcenter"]         = { power = "terran_supplydepot", barracks = "terran_barracks", warfactory = "terran_starport", refinery = "terran_refinery" },
	["protoss_mobilenexus"]                = { power = "protoss_pylon", barracks = "protoss_gateway", warfactory = "protoss_roboticsfacility", refinery = "protoss_assimilator" },
	["zerg_hatcherydrone"]                 = { power = nil, barracks = "zerg_spawningpool", warfactory = "zerg_hydraliskden", refinery = "zerg_extractor" },
}

-- Maps faction internal name to construction yard actor name
FactionConyards = {
	td_gdi = "td_gdi_constructionyard",
	td_nod = "td_nod_constructionyard",
	ra1_allies = "ra1_allies_alliedconstructionyard",
	ra1_soviets = "ra1_soviets_constructionyard",
	ra2_allies = "ra2_allies_alliedconstructionyard",
	ra2_soviets = "ra2_soviets_constructionyard",
	yuri = "yuri_constructionyard",
	asianalliance = "asianalliance_asianconstructionyard",
	latinsyndicate = "latinsyndicate_syndicateconstructionyard",
	ordos = "ordos_constructionyard",
	ts_gdi = "ts_gdi_constructionyard",
	ts_nod = "ts_nod_constructionyard",
	japan = "japan_japaneseconstructionyard",
	naxis = "naxis_constructionyard",
	schwarzermond = "schwarzermond_constructionyard",
	ixian = "ixian_constructionyard",
	wc2_humans = "wc2_humans_townhall",
	wc2_orcs = "wc2_orcs_greathall",
	tkm = "tkm_constructionyard",
	steelconsortium = "steelconsortium_consortiumconstructionyard",
	futuretech = "futuretech_constructionyard",
	cabal = "cabal_constructionyard",
	forgotten = "forgotten_constructionyard",
	zerg = "zerg_hatchery",
	protoss = "protoss_nexus",
	terran = "terran_commandcenter",
}

-- Reverse map: MCV actor name -> faction internal name
MCVFactionMap = {}
for _, w in ipairs(Waves) do
	if w.mcv ~= nil then
		for factionName, _ in pairs(FactionInternalToDisplayName) do
			if w.mcv:sub(1, #factionName) == factionName then
				MCVFactionMap[w.mcv] = factionName
			end
		end
	end
end

-- Reverse map: faction internal name -> MCV actor name
FactionToMCV = {}
for mcvName, factionName in pairs(MCVFactionMap) do
	FactionToMCV[factionName] = mcvName
end

-- Spawn a pre-built base (minus conyard) plus an MCV for a Foe player at a corner.
-- The AI deploys the MCV naturally, which sets DefenseCenter and avoids the bot crash.
SpawnAIBase = function(foeIdx, cornerPos)
	local foe = Foes[foeIdx]
	if foe == nil then return end

	local faction = foe.Faction
	if faction == nil or faction == "" or faction == "Random" then
		local keys = {}
		for k, _ in pairs(FactionConyards) do table.insert(keys, k) end
		faction = keys[Utils.RandomInteger(1, #keys + 1)]
	end

	local mcv = FactionToMCV[faction]
	if mcv == nil then return end

	-- Find the FactionBuildings entry by matching MCV to faction
	local buildings = nil
	for mcvName, bld in pairs(FactionBuildings) do
		if MCVFactionMap[mcvName] == faction then
			buildings = bld
			break
		end
	end
	if buildings == nil then return end

	-- Spawn MCV first so we can capture the actor reference and force-deploy it
	local mcvActor = nil
	pcall(function()
		mcvActor = Actor.Create(mcv, true, { Owner = foe, Location = cornerPos })
	end)

	-- Force-deploy the MCV on the next tick so the AI bot doesn't drive it to center
	if mcvActor ~= nil then
		Trigger.AfterDelay(0, function()
			if not mcvActor.IsDead then
				if mcvActor.HasProperty("Deploy") then
					mcvActor.Deploy()
				elseif mcvActor.HasProperty("Build") then
					mcvActor.Build()
				end
			end
		end)
	end

	-- Spawn the rest of the base buildings near the corner
	local offsets = {
		{ dx = 5, dy = 0, actor = buildings.power },
		{ dx = 0, dy = 4, actor = buildings.barracks },
		{ dx = 5, dy = 4, actor = buildings.warfactory },
		{ dx = 0, dy = 8, actor = buildings.refinery },
	}

	for _, o in ipairs(offsets) do
		if o.actor ~= nil then
			local pos = CPos.New(cornerPos.X + o.dx, cornerPos.Y + o.dy)
			pcall(function()
				Actor.Create(o.actor, true, { Owner = foe, Location = pos })
			end)
		end
	end

	-- Give initial cash so AI bot modules can start producing
	GiveAICashInjection(foe, 5000)
end

-- Maps MCV actor name to the faction's upgrade list (unlocked progressively each wave)
FactionUpgrades = {
	["td_gdi_mobileconstructionvehicle"] = {
		"td_gdi_upgrade_longrangesensors", "td_gdi_upgrade_armorpiercingbullets", "td_gdi_upgrade_a10airstrike",
		"td_gdi_upgrade_heavyaircraftarmorplating", "td_gdi_upgrade_advancedmissiletargeting",
		"td_gdi_upgrade_cuttingedgeequipment", "td_gdi_upgrade_highvelocitycannons", "td_gdi_upgrade_lightweightarmorplating",
	},
	["td_nod_mobileconstructionvehicle"] = {
		"td_nod_upgrade_guerillatactics", "td_nod_upgrade_tiberiuminfusion", "td_nod_upgrade_improvedartilleries",
		"td_nod_upgrade_blackmarketupgrades", "td_nod_upgrade_elementalwarfare", "td_nod_upgrade_elitecapacitors",
		"td_nod_upgrade_cyberneticmodifications", "td_nod_upgrade_advancedguerillatactics",
	},
	["ra1_allies_alliedmobileconstructionvehicle"] = {
		"ra1_allies_upgrade_advancedradarsystems", "ra1_allies_upgrade_infantryarmorplating", "ra1_allies_upgrade_reinforcedstructures",
		"ra1_allies_upgrade_airsuperioritydoctrine", "ra1_allies_upgrade_lasertargetingsystems",
		"ra1_allies_upgrade_chronoarmor", "ra1_allies_upgrade_cryomissiles", "ra1_allies_upgrade_gpssatellitesupport",
	},
	["ra1_soviets_mobileconstructionvehicle"] = {
		"ra1_soviets_upgrade_hazmatsuits", "ra1_soviets_upgrade_vengeance", "ra1_soviets_upgrade_menofsteel",
		"ra1_soviets_upgrade_massproduction", "ra1_soviets_upgrade_wareconomy", "ra1_soviets_upgrade_incendiarybullets",
		"ra1_soviets_upgrade_scorchedearth", "ra1_soviets_upgrade_teslaarcing", "ra1_soviets_upgrade_teslarockets",
		"ra1_soviets_upgrade_reactoroverload", "ra1_soviets_upgrade_autoloaders", "ra1_soviets_upgrade_highexplosiverockets",
		"ra1_soviets_upgrade_stalinium", "ra1_soviets_upgrade_unstableisotopes", "ra1_soviets_upgrade_thermonuclearrockets",
		"ra1_soviets_upgrade_nucleartankshells", "ra1_soviets_upgrade_afterburners", "ra1_soviets_upgrade_hammertank",
		"ra1_soviets_upgrade_heavyteslatank", "ra1_soviets_upgrade_shtoradefensesystem", "ra1_soviets_upgrade_kotinnucleartank",
		"ra1_soviets_upgrade_commissar", "ra1_soviets_upgrade_heatraytank", "ra1_soviets_upgrade_teslayak",
		"ra1_soviets_upgrade_armoredyak", "ra1_soviets_upgrade_nuclearyak",
	},
	["ra2_allies_alliedmobileconstructionvehicle"] = {
		"ra2_allies_upgrade_assaultsquadtraining", "ra2_allies_upgrade_vanguardtraining", "ra2_allies_upgrade_infiltratorstraining",
		"ra2_allies_upgrade_compositearmorplating", "ra2_allies_upgrade_reflectivearmorplating", "ra2_allies_upgrade_ionpulseplating",
		"ra2_allies_upgrade_elitegi", "ra2_allies_upgrade_paratroopers", "ra2_allies_upgrade_eliterocketeer",
		"ra2_allies_upgrade_prismlinking", "ra2_allies_upgrade_forceshield", "ra2_allies_upgrade_heavymiragetank",
		"ra2_allies_upgrade_advancedaeronautics", "ra2_allies_upgrade_tanklasertargeting", "ra2_allies_upgrade_thunderboltmissiles",
		"ra2_allies_upgrade_intensifiedprismbeams", "ra2_allies_upgrade_prismaticbarrier", "ra2_allies_upgrade_chronoengine",
	},
	["ra2_soviets_mobileconstructionvehicle"] = {
		"ra2_soviets_upgrade_terrordronesurprise", "ra2_soviets_upgrade_teslacoilchargers", "ra2_soviets_upgrade_teslaoverload",
		"ra2_soviets_upgrade_heavycannons", "ra2_soviets_upgrade_grindertanktreads", "ra2_soviets_upgrade_kirovarmorplatings",
		"ra2_soviets_upgrade_gastroburners", "ra2_soviets_upgrade_kirovatomicbombs",
	},
	["yuri_mobileconstructionvehicle"] = {
		"yuri_upgrade_initiatepowersurge", "yuri_upgrade_brutestrengthmutations", "yuri_upgrade_lasherarmorimprovements",
		"yuri_upgrade_gatlingfirepowerupgrade", "yuri_upgrade_meltingvirus", "yuri_upgrade_domination",
		"yuri_upgrade_gatlingspeedupgrade", "yuri_upgrade_corrosiveammo", "yuri_upgrade_supermagnets",
		"yuri_upgrade_bioreactorefficiency", "yuri_upgrade_bioengineering", "yuri_upgrade_psychicrange",
		"yuri_upgrade_psionicarmor", "yuri_upgrade_toxicengines", "yuri_upgrade_psionicplasmabeams",
		"yuri_upgrade_gravitondrive", "yuri_upgrade_disksiphonattack", "yuri_upgrade_diskhighfrequencylasers",
		"yuri_upgrade_infantrystealthsuits", "yuri_upgrade_geneticmodificationboost", "yuri_upgrade_psychicvision",
	},
	["japan_japanesemobileconstructionvehicle"] = {
		"japan_upgrade_bushidodiscipline", "japan_upgrade_waveforcebullets", "japan_upgrade_divinewindprotocol",
		"japan_upgrade_stealthsuitintegration", "japan_upgrade_energizedarrows", "japan_upgrade_superiorwarengines",
		"japan_upgrade_advancedplasmaweapons", "japan_upgrade_nanotechrepairs",
	},
	["ts_gdi_mobileconstructionvehicle"] = {
		"ts_gdi_upgrade_seretraining", "ts_gdi_upgrade_projectileimprovements", "ts_gdi_upgrade_mechanicalreliability",
		"ts_gdi_upgrade_ceramicarmor", "ts_gdi_upgrade_mechengineering", "ts_gdi_upgrade_sonicweaponry",
		"ts_gdi_upgrade_railgunweaponry", "ts_gdi_upgrade_modernfirecontrolsystems",
	},
	["ts_nod_mobileconstructionvehicle"] = {
		"ts_nod_upgrade_infiltrationkit", "ts_nod_upgrade_mobilityspecialization", "ts_nod_upgrade_auxiliaryweapon",
		"ts_nod_upgrade_stealthfieldsimprovements", "ts_nod_upgrade_tiberiumlenses", "ts_nod_upgrade_tiberiumcoremissiles",
		"ts_nod_upgrade_advancedtiberiumrefinement", "ts_nod_upgrade_willofkane",
	},
	["cabal_mobileconstructionvehicle"] = {
		"cabal_upgrade_mobilitymatrix", "cabal_upgrade_darkarmament", "cabal_upgrade_neutronnuclearcatalyst",
		"cabal_upgrade_radarhack", "cabal_upgrade_networkedcombatprotocols", "cabal_upgrade_backupsystems",
		"cabal_upgrade_handof", "cabal_upgrade_dataworm", "cabal_upgrade_firewallprotocol",
		"cabal_upgrade_reinforcedchassis", "cabal_upgrade_overchargedservos", "cabal_upgrade_neuraluplink",
		"cabal_upgrade_reclamationprotocols", "cabal_upgrade_fullassimilation", "cabal_upgrade_cyberneticplating",
	},
	["forgotten_mobileconstructionvehicle"] = {
		"forgotten_upgrade_genomemapping", "forgotten_upgrade_chemicalfuel", "forgotten_upgrade_friendlywildlife",
		"forgotten_upgrade_junkarmor", "forgotten_upgrade_tiberiumboosters", "forgotten_upgrade_unity",
		"forgotten_upgrade_chemicalweapons", "forgotten_upgrade_tiberiumadaptability", "forgotten_upgrade_mutantsoldier",
	},
	["asianalliance_asianmobileconstructionvehicle"] = {
		"asianalliance_upgrade_massparadrop", "asianalliance_upgrade_clusterbombs", "asianalliance_upgrade_chaosbombs",
		"asianalliance_upgrade_dragonfire", "asianalliance_upgrade_celestialpower", "asianalliance_upgrade_banzaimode",
		"asianalliance_upgrade_wayofthedragon", "asianalliance_upgrade_asiandiplomacy",
	},
	["latinsyndicate_syndicatemobileconstructionvehicle"] = {
		"latinsyndicate_upgrade_empcannon", "latinsyndicate_upgrade_yuristolentechchainguns",
		"latinsyndicate_upgrade_sovietstolentechindustrialmethods", "latinsyndicate_upgrade_alliedstolentechextramissilesandmiraging",
		"latinsyndicate_upgrade_asianalliancestolentechhotfire", "latinsyndicate_upgrade_cashrecovery",
	},
	["ordos_mobileconstructionvehicle"] = {
		"ordos_upgrade_lightfactory", "ordos_upgrade_antiairtrooper", "ordos_upgrade_heavycombattank",
		"ordos_upgrade_heavycombattankrockets", "ordos_upgrade_heavyautoguntank", "ordos_upgrade_shields",
		"ordos_upgrade_biologicalwarfare", "ordos_upgrade_contraband", "ordos_upgrade_lasercartridges",
		"ordos_upgrade_rapidfirearmorpiercingbelts", "ordos_upgrade_hoverdrive",
	},
	["ixian_mobileconstructionvehicle"] = {
		"ixian_upgrade_twinbazooka", "ixian_upgrade_tungstenneedleguns", "ixian_upgrade_personalshield",
		"ixian_upgrade_generalpurposearmor", "ixian_upgrade_reinforcedbarrel", "ixian_upgrade_heavymissiles",
		"ixian_upgrade_heavykodatank", "ixian_upgrade_advancedixiantechnology",
	},
	["naxis_naximobileconstructionvehicle"] = {
		"naxis_upgrade_me262", "naxis_upgrade_ostfrontexperience", "naxis_upgrade_atlantikwall",
		"naxis_upgrade_tankarsenalrenovation", "naxis_upgrade_massopticsintegration", "naxis_upgrade_wunderwaffe",
		"naxis_upgrade_blitzkrieg",
	},
	["schwarzermond_naxismobileconstructionvehicle"] = {
		"schwarzermond_upgrade_crystallens", "schwarzermond_upgrade_amplifiedlens", "schwarzermond_upgrade_vrilpoweredweapons",
		"schwarzermond_upgrade_helium3", "schwarzermond_upgrade_vrilinfusion", "schwarzermond_upgrade_lunaralloys",
		"schwarzermond_upgrade_moonpropaganda", "schwarzermond_upgrade_cryptofascism",
	},
	["steelconsortium_consortiummobileconstructionvehicle"] = {
		"steelconsortium_upgrade_pulseweapons", "steelconsortium_upgrade_naniteinfusion", "steelconsortium_upgrade_ferrocretecurtain",
		"steelconsortium_upgrade_empcannon", "steelconsortium_upgrade_scalpeldefenders", "steelconsortium_upgrade_resonanceammo",
		"steelconsortium_upgrade_quantumweaponpower", "steelconsortium_upgrade_shieldresistance",
	},
	["futuretech_mobileconstructionvehicle"] = {
		-- FutureTech has no upgrades in upgrades.yaml
	},
	["tkm_mobileconstructionvehicletkm"] = {
		"tkm_upgrade_berezkaarsenalupgrade", "tkm_upgrade_titanarsenalupgrade", "tkm_upgrade_natoarsenalupgrade",
		"tkm_upgrade_semiautoriflesupgrade", "tkm_upgrade_twinrocketsupgrade", "tkm_upgrade_titanarmorpiercingbulletsupgrade",
		"tkm_upgrade_incendiaryrocketsupgrade", "tkm_upgrade_cryorocketsupgrade", "tkm_upgrade_technicaltankrocketaddon",
		"tkm_upgrade_pointdefensesystem", "tkm_upgrade_heavytitanplating", "tkm_upgrade_infantryupgrade", "tkm_upgrade_gp25upgrade",
	},
	["wc2_humans_mobileconstructionvehiclehuman"] = {
		"wc2_humans_upgrade_betterfarm", "wc2_humans_upgrade_bestfarm", "wc2_humans_upgrade_swordstrength",
		"wc2_humans_upgrade_swordstrengthii", "wc2_humans_upgrade_armorstrength", "wc2_humans_upgrade_armorstrengthii",
		"wc2_humans_upgrade_arrowstrength", "wc2_humans_upgrade_arrowstrengthii", "wc2_humans_upgrade_warcraft3footman",
		"wc2_humans_upgrade_ranger", "wc2_humans_upgrade_highelvenarcher", "wc2_humans_upgrade_rangerlongbow",
		"wc2_humans_upgrade_rangerscouting", "wc2_humans_upgrade_rangermarksman", "wc2_humans_upgrade_ballistastrength",
		"wc2_humans_upgrade_ballistastrengthii", "wc2_humans_upgrade_cannondamage", "wc2_humans_upgrade_cannondamageii",
		"wc2_humans_upgrade_paladin", "wc2_humans_upgrade_warcraft3knightwip", "wc2_humans_upgrade_healing",
		"wc2_humans_upgrade_exorcism", "wc2_humans_upgrade_slow", "wc2_humans_upgrade_polymorph", "wc2_humans_upgrade_blizzard",
	},
	["wc2_orcs_mobileconstructionvehicleorc"] = {
		"wc2_orcs_upgrade_betterfarm", "wc2_orcs_upgrade_bestfarm", "wc2_orcs_upgrade_axestrength",
		"wc2_orcs_upgrade_axestrengthii", "wc2_orcs_upgrade_armorstrength", "wc2_orcs_upgrade_armorstrengthii",
		"wc2_orcs_upgrade_throwingaxestrength", "wc2_orcs_upgrade_throwingaxestrengthii", "wc2_orcs_upgrade_warcraft3grunt",
		"wc2_orcs_upgrade_berserker", "wc2_orcs_upgrade_trollheadhunter", "wc2_orcs_upgrade_berserkerlightaxes",
		"wc2_orcs_upgrade_berserkerscouting", "wc2_orcs_upgrade_berserkerregeneration", "wc2_orcs_upgrade_catapultstrength",
		"wc2_orcs_upgrade_catapultstrengthii", "wc2_orcs_upgrade_cannondamage", "wc2_orcs_upgrade_cannondamageii",
		"wc2_orcs_upgrade_ogremage", "wc2_orcs_upgrade_bloodlust", "wc2_orcs_upgrade_runes",
		"wc2_orcs_upgrade_haste", "wc2_orcs_upgrade_deathanddecay", "wc2_orcs_upgrade_raisedead",
	},
	["terran_mobilecommandcenter"] = {
		"terran_upgrade_stimpack", "terran_upgrade_u238shells", "terran_upgrade_upgradesupplydepotlevel1",
		"terran_upgrade_upgradesupplydepotlevel2", "terran_upgrade_cloakupgrade", "terran_upgrade_advancedsiegemode",
		"terran_upgrade_yamatocannonandtacticaljumpupgrade", "terran_upgrade_infantryweaponslevel1",
		"terran_upgrade_infantryweaponslevel2", "terran_upgrade_infantryarmorlevel1", "terran_upgrade_infantryarmorlevel2",
		"terran_upgrade_vehicleweaponslevel1", "terran_upgrade_vehicleweaponslevel2", "terran_upgrade_vehicleplatinglevel1",
		"terran_upgrade_vehicleplatinglevel2", "terran_upgrade_shipweaponslevel1", "terran_upgrade_shipweaponslevel2",
		"terran_upgrade_shipplatinglevel1", "terran_upgrade_shipplatinglevel2",
	},
	["protoss_mobilenexus"] = {
		"protoss_upgrade_singularitycharge", "protoss_upgrade_resonatingglaves", "protoss_upgrade_upgradepylonlevel1",
		"protoss_upgrade_upgradepylonlevel2", "protoss_upgrade_plasmashields", "protoss_upgrade_reavercapacity",
		"protoss_upgrade_graviticpropulsion", "protoss_upgrade_groundweaponslevel1", "protoss_upgrade_groundweaponslevel2",
		"protoss_upgrade_groundarmorlevel1", "protoss_upgrade_groundarmorlevel2", "protoss_upgrade_airweaponslevel1",
		"protoss_upgrade_airweaponslevel2", "protoss_upgrade_airarmorlevel1", "protoss_upgrade_airarmorlevel2",
	},
	["zerg_hatcherydrone"] = {
		"zerg_upgrade_pneumatizedcarapace", "zerg_upgrade_metabolicboost", "zerg_upgrade_adrenalglands",
		"zerg_upgrade_groovedspines", "zerg_upgrade_meleeattackslevel1", "zerg_upgrade_meleeattackslevel2",
		"zerg_upgrade_missileattackslevel1", "zerg_upgrade_missileattackslevel2", "zerg_upgrade_carapacelevel1",
		"zerg_upgrade_carapacelevel2", "zerg_upgrade_flyerattackslevel1", "zerg_upgrade_flyerattackslevel2",
		"zerg_upgrade_flyercarapacelevel1", "zerg_upgrade_flyercarapacelevel2",
	},
}

-- Track which upgrades have been unlocked per Foe player
FoeUpgradesUnlocked = { {}, {}, {}, {} }

-- Unlock upgrades for the current wave's faction, scaling with wave index
UnlockFactionUpgrades = function(mcvType, waveIdx)
	local upgrades = FactionUpgrades[mcvType]
	if upgrades == nil or #upgrades == 0 then return end

	-- Determine which Foe player owns this faction's units
	-- Find the Foe player that has the most units from this faction
	-- Simple approach: pick a random Foe that has units
	local foeIdx = RandomFoe()
	if foeIdx == nil then return end

	local unlocked = FoeUpgradesUnlocked[foeIdx]
	if unlocked == nil then
		unlocked = {}
		FoeUpgradesUnlocked[foeIdx] = unlocked
	end

	-- Track which upgrades from this faction have already been unlocked
	local alreadyUnlocked = {}
	for _, u in ipairs(unlocked) do
		alreadyUnlocked[u] = true
	end

	-- Number of upgrades to unlock this wave: scales with wave tier
	-- Tier 1: 1 per wave, Tier 2: 1-2, Tier 3: 2, Tier 4: 2-3
	local tier = math.min(4, math.floor(waveIdx / 7) + 1)
	local toUnlock = math.min(tier, 3)

	-- Find upgrades from this faction not yet unlocked
	local available = {}
	for _, u in ipairs(upgrades) do
		if not alreadyUnlocked[u] then
			table.insert(available, u)
		end
	end

	if #available == 0 then return end

	-- Unlock up to toUpgrade upgrades
	for i = 1, math.min(toUnlock, #available) do
		local upgradeType = available[i]
		pcall(function()
			Actor.Create(upgradeType, true, { Owner = Foes[foeIdx] })
		end)
		table.insert(unlocked, upgradeType)
	end
end

-- Give the AI a cash injection so it can build a base from the deployed MCV
-- The AI bot modules (BaseBuilderBotModuleCA, UnitBuilderBotModuleCA, etc.) handle
-- construction and unit production autonomously -- no scripted building spawns.
GiveAICashInjection = function(owner, amount)
	if owner == nil then return end
	pcall(function()
		owner.Cash = owner.Cash + amount
	end)
end

SendMCV = function(faction, waveIdx)
	if faction.mcv == nil then
		return
	end
	local chance = 15 + waveIdx * 3
	if chance > 50 then chance = 50 end
	if Utils.RandomInteger(1, 101) > chance then
		return
	end

	-- Random delay: MCV arrives 0-30 seconds after wave start
	local delay = Utils.RandomInteger(0, 31)
	Trigger.AfterDelay(DateTime.Seconds(delay), function()
		local foeIdx = RandomFoe()
		if foeIdx == nil then return end

		local edges = { GoundAttackArrayTop, GoundAttackArrayRight, GoundAttackArraySouth, GoundAttackArrayLeft }
		-- Lottery: prefer sides that haven't sent an MCV yet
		local available = {}
		for i = 1, 4 do
			if not MCVSentFromSide[i] then
				table.insert(available, i)
			end
		end
		if #available == 0 then
			MCVSentFromSide = { false, false, false, false }
			available = { 1, 2, 3, 4 }
		end
		local edgeIdx = available[Utils.RandomInteger(1, #available)]
		local slot = Utils.RandomInteger(1, 4)
		local entry = edges[edgeIdx][slot]
		if entry == nil then return end

		local mcv = nil
		pcall(function()
			mcv = Actor.Create(faction.mcv, true, { Owner = Foes[foeIdx], Location = entry.Location })
		end)
		if mcv == nil then return end
		-- MCV is NOT added to LiveFoes -- it should not count toward the enemy value budget

		local deployPos = MCVDeployPositions[edgeIdx]
		local hasDeployed = false
		Trigger.OnIdle(mcv, function()
			if mcv.IsDead then return end
			if mcv.IsIdle then
				if not hasDeployed then
					if mcv.HasProperty("Move") then
						mcv.Move(deployPos)
					end
					-- Check if close enough to deploy position
					local dist = math.abs(mcv.Location.X - deployPos.X) + math.abs(mcv.Location.Y - deployPos.Y)
					if dist <= 3 then
						hasDeployed = true
						if mcv.HasProperty("Deploy") then
							mcv.Deploy()
						elseif mcv.HasProperty("Build") then
							-- Some MCVs use Build instead of Deploy
							mcv.Build()
						end
						-- After deploy, give the AI a cash injection to kickstart base building
						GiveAICashInjection(Foes[foeIdx], 5000)
					end
				end
			end
		end)

		MCVSentFromSide[edgeIdx] = true
		local sideNames = { "north", "east", "south", "west" }
		ThrottledDisplayMessage("WARNING: Enemy MCV detected approaching from the " .. sideNames[edgeIdx] .. "! They're establishing a forward base!", "")
	end)
end

-- =====================================================================
-- SNEAK ATTACK SYSTEM
-- Randomly launches sneak attacks near the player's base.
-- Attack type varies by faction: subterranean, chronoshift, paratroopers,
-- burrow, warp, dropship, blitz, or generic ambush.
-- Uses RANDOM faction units from the global pool.
-- =====================================================================

GetSneakAttackType = function(factionName)
	local name = factionName:lower()
	if name:find("nod") or name:find("cabal") then
		return "subterranean"
	elseif name:find("allied") or name:find("vanguard") or name:find("peacekeeper") then
		return "chronoshift"
	elseif name:find("soviet") or name:find("onslaught") or name:find("red army") then
		return "paratroopers"
	elseif name:find("swarm") or name:find("zerg") then
		return "burrow"
	elseif name:find("protoss") then
		return "warp"
	elseif name:find("terran") then
		return "dropship"
	elseif name:find("schwarzer") or name:find("naxis") then
		return "blitz"
	elseif name:find("ordos") then
		return "subterranean"
	elseif name:find("ixian") then
		return "warp"
	elseif name:find("human expedition") or name:find("tkm") then
		return "paratroopers"
	elseif name:find("orcish") then
		return "ambush"
	else
		return "ambush"
	end
end

SneakAttackMessages = {
	subterranean = "SNEAK ATTACK: Subterranean APC surfacing inside your perimeter!",
	chronoshift = "SNEAK ATTACK: Enemy units chronoshifted directly into your base!",
	paratroopers = "SNEAK ATTACK: Enemy paratroopers dropping behind your lines!",
	burrow = "SNEAK ATTACK: Swarm forces burrowing up from beneath your base!",
	warp = "SNEAK ATTACK: Protoss forces warping in within your perimeter!",
	dropship = "SNEAK ATTACK: Terran dropship unloading inside your base!",
	blitz = "SNEAK ATTACK: Enemy blitzkrieg striking from an unexpected angle!",
	ambush = "SNEAK ATTACK: Enemy ambush detected near your base!",
}

LaunchSneakAttack = function(faction, waveIdx)
	local chance = 10 + waveIdx * 2
	if chance > 35 then chance = 35 end
	if Utils.RandomInteger(1, 101) > chance then
		return
	end

	-- Random delay: sneak attack fires 5-40 seconds after wave start
	local delay = Utils.RandomInteger(5, 41)
	Trigger.AfterDelay(DateTime.Seconds(delay), function()
		local foeIdx = RandomFoe()
		if foeIdx == nil then return end

		-- Use a RANDOM faction's sneak attack type, not the current wave's
		local randomFaction = Waves[Utils.RandomInteger(1, #Waves)]
		local attackType = GetSneakAttackType(randomFaction.name)

		local spawnPos = RandomSpawnPos(6, 18)

		if #AllFactionUnits == 0 then return end

		local sneakUnits = {}
		local count = 0
		if attackType == "subterranean" then
			table.insert(sneakUnits, "ts_nod_subterraneanapc")
			count = Utils.RandomInteger(5, 10)  -- WAS 3-6
		elseif attackType == "burrow" then
			count = Utils.RandomInteger(7, 14)  -- WAS 5-9
		elseif attackType == "paratroopers" then
			count = Utils.RandomInteger(6, 12)  -- WAS 4-8
		else
			count = Utils.RandomInteger(5, 10)  -- WAS 3-7
		end
		for i = 1, count do
			table.insert(sneakUnits, GetRandomWaveUnit())
		end

		local vetLevels = RandomVetLevels(waveIdx)
		local spawnedCount = 0
		for _, unitType in ipairs(sneakUnits) do
			local unit = SpawnUnitAt(unitType, Foes[foeIdx], spawnPos, vetLevels, true)
			if unit ~= nil then spawnedCount = spawnedCount + 1 end
		end

		if spawnedCount > 0 then
			ThrottledDisplayMessage(SneakAttackMessages[attackType] or SneakAttackMessages.ambush, GetCurrentWaveFactionName())
		end

		-- Random chance for a SECOND sneak attack (40%, was 25%)
		if Utils.RandomInteger(1, 101) <= 40 then
			Trigger.AfterDelay(DateTime.Seconds(Utils.RandomInteger(2, 8)), function()
				local foe2 = RandomFoe()
				if foe2 == nil then return end
				local pos2 = RandomSpawnPos(6, 18)
				local count2 = Utils.RandomInteger(5, 9)  -- WAS 3-6
				local spawned2 = 0
				for i = 1, count2 do
					local unit = SpawnUnitAt(GetRandomWaveUnit(), Foes[foe2], pos2, vetLevels, true)
					if unit ~= nil then spawned2 = spawned2 + 1 end
				end
				if spawned2 > 0 then
					ThrottledDisplayMessage("SNEAK ATTACK: Second " .. GetCurrentWaveFactionName() .. " wave detected! They're coming from everywhere!", GetCurrentWaveFactionName())
				end
			end)
		end

		-- Random chance for a THIRD sneak attack (20% -- NEW)
		if Utils.RandomInteger(1, 101) <= 20 then
			Trigger.AfterDelay(DateTime.Seconds(Utils.RandomInteger(5, 12)), function()
				local foe3 = RandomFoe()
				if foe3 == nil then return end
				local pos3 = RandomSpawnPos(6, 18)
				local count3 = Utils.RandomInteger(4, 8)
				local spawned3 = 0
				for i = 1, count3 do
					local unit = SpawnUnitAt(GetRandomWaveUnit(), Foes[foe3], pos3, vetLevels, true)
					if unit ~= nil then spawned3 = spawned3 + 1 end
				end
				if spawned3 > 0 then
					ThrottledDisplayMessage("SNEAK ATTACK: THIRD " .. GetCurrentWaveFactionName() .. " wave! They're EVERYWHERE!", GetCurrentWaveFactionName())
				end
			end)
		end
	end)
end

-- =====================================================================
-- CHAOS ATTACK SYSTEM
-- Rogue mixed-faction forces spawn from random edges between waves.
-- Pulls from ALL faction unit pools for maximum unpredictability.
-- Random chance for epic unit to spawn during chaos!
-- =====================================================================

ChaosAttack = function(waveIdx)
	-- NOW fires from wave 1+ (was 3+), higher chance
	local chance = 25 + waveIdx * 4
	if chance > 70 then chance = 70 end
	if Utils.RandomInteger(1, 101) > chance then return end

	local foeIdx = RandomFoe()
	if foeIdx == nil then return end

	local count = math.floor((Utils.RandomInteger(6, 14) + math.floor(waveIdx / 2)) * PlayerScale())  -- scales with players
	local chaosUnits = {}
	for i = 1, count do
		table.insert(chaosUnits, GetRandomWaveUnit())
	end

	-- Random chance for MULTIPLE epic units (wave 5+, from current wave faction)
	local curWave = Waves[CurrentWaveIdx]
	if waveIdx >= 5 and curWave ~= nil and curWave.epic ~= nil and not IsBlacklisted(curWave.epic[1]) and Utils.RandomInteger(1, 101) <= 30 then
		local epicCount = Utils.RandomInteger(1, 3)  -- 1-3 epics!
		for i = 1, epicCount do
			table.insert(chaosUnits, curWave.epic[1])
		end
		ThrottledDisplayMessage("CHAOS ATTACK: " .. epicCount .. " EPIC units have joined the " .. GetCurrentWaveFactionName() .. " forces!", GetCurrentWaveFactionName())
	end

	local entry = RandomEdge()
	if entry == nil then return end

	local vetLevels = RandomVetLevels(waveIdx)
	SpawnUnitListAt(chaosUnits, Foes[foeIdx], entry.Location, Utils.RandomInteger(2, 6), vetLevels)

	ThrottledDisplayMessage("CHAOS ATTACK: " .. GetCurrentWaveFactionName() .. " rogue forces converging on your position!", GetCurrentWaveFactionName())

	-- Random chance for a SECOND chaos attack (40%, was 30%)
	if Utils.RandomInteger(1, 101) <= 40 then
		Trigger.AfterDelay(DateTime.Seconds(Utils.RandomInteger(3, 12)), function()
			local foe2 = RandomFoe()
			if foe2 == nil then return end
			local entry2 = RandomEdge()
			if entry2 == nil then return end
			local count2 = math.floor(Utils.RandomInteger(5, 10) * PlayerScale())  -- scales with players
			local units2 = {}
			for i = 1, count2 do
				table.insert(units2, GetRandomWaveUnit())
			end
			if waveIdx >= 5 and curWave ~= nil and curWave.epic ~= nil and not IsBlacklisted(curWave.epic[1]) and Utils.RandomInteger(1, 101) <= 20 then
				table.insert(units2, curWave.epic[1])
			end
			SpawnUnitListAt(units2, Foes[foe2], entry2.Location, Utils.RandomInteger(2, 5), vetLevels)
			ThrottledDisplayMessage("CHAOS ATTACK: Second " .. GetCurrentWaveFactionName() .. " wave detected!", GetCurrentWaveFactionName())
		end)
	end

	-- Random chance for a THIRD chaos attack (20% -- NEW)
	if Utils.RandomInteger(1, 101) <= 20 then
		Trigger.AfterDelay(DateTime.Seconds(Utils.RandomInteger(8, 18)), function()
			local foe3 = RandomFoe()
			if foe3 == nil then return end
			local entry3 = RandomEdge()
			if entry3 == nil then return end
			local count3 = math.floor(Utils.RandomInteger(4, 8) * PlayerScale())  -- scales with players
			local units3 = {}
			for i = 1, count3 do
				table.insert(units3, GetRandomWaveUnit())
			end
			SpawnUnitListAt(units3, Foes[foe3], entry3.Location, Utils.RandomInteger(2, 5), vetLevels)
			ThrottledDisplayMessage("CHAOS ATTACK: THIRD " .. GetCurrentWaveFactionName() .. " wave! EVERYTHING is attacking!", GetCurrentWaveFactionName())
		end)
	end
end

-- =====================================================================
-- DOUBLE TROUBLE SYSTEM
-- A second faction joins the assault alongside the main wave (wave 10+).
-- Spawns a smaller force from a different faction at a different edge.
-- =====================================================================

DoubleTrouble = function(waveIdx)
	-- NOW from wave 3+ (was 10+), much higher chance
	if waveIdx < 3 then return end
	local chance = 30 + waveIdx * 3
	if chance > 60 then chance = 60 end
	if Utils.RandomInteger(1, 101) > chance then return end

	-- Random delay: double trouble fires 5-25 seconds after wave start (was 10-30)
	local delay = Utils.RandomInteger(5, 26)
	Trigger.AfterDelay(DateTime.Seconds(delay), function()
		local otherIdx = Utils.RandomInteger(1, #Waves)
		if otherIdx == waveIdx then otherIdx = Utils.RandomInteger(1, #Waves) end
		if otherIdx == waveIdx then return end
		local otherWave = Waves[otherIdx]

		-- BIGGER budget: 0.8x (was 0.6x)
		local budget = RandomBudgetVariance((BaseBudget + BudgetGrowth * (waveIdx - 1)) * 0.8 * (0.5 + PlayerScale()))
		local maxUnits = RandomUnitCountVariance(math.floor((8 + math.floor(waveIdx / 2)) * PlayerScale()))
		local list = ComposeWave(otherWave, budget, maxUnits)
		if #list == 0 then return end

		local foeIdx = RandomFoe()
		if foeIdx == nil then return end

		local entry = RandomEdge()
		if entry == nil then return end

		local vetLevels = RandomVetLevels(waveIdx)
		SpawnUnitListAt(list, Foes[foeIdx], entry.Location, Utils.RandomInteger(3, 7), vetLevels)

		ThrottledDisplayMessage("DOUBLE TROUBLE: " .. otherWave.name .. " joining " .. Waves[waveIdx].name .. " assault!", otherWave.name)

		-- Pick general for the joining faction and try per-general doubleTrouble lines
		local general = PickGeneralForFaction(otherWave.name)
		local mainWave = Waves[waveIdx]
		local mainGen = PickGeneralForFaction(mainWave.name)
		local playerFaction = GetPlayerFactionName()
		local otherGenName = mainGen ~= nil and mainGen.name or "the enemy commander"

		-- Try per-general doubleTrouble line first, fall back to generic meme lines
		local dtLine = PickEventLine(general, "doubleTrouble", mainWave.name, otherGenName, playerFaction)
		if dtLine ~= nil and general ~= nil then
			ThrottledDisplayMessage("[" .. general.name .. "] " .. dtLine, otherWave.name)
		else
			local dtMsgs = {
				"DOUBLE TROUBLE: 1&1 = 2 armies! Marcel D'Avis says: 'Du bist nicht allein!' He's right. You're NOT alone. Two factions are here.",
				"DOUBLE TROUBLE: Two factions, one target. Existence issue? No. Survival issue? YES.",
				"DOUBLE TROUBLE: Two armies. Two fronts. One you. The math is not in your favor. The math is NEVER in your favor.",
				"DOUBLE TROUBLE: They say two heads are better than one. Two armies are also better than one. Better at destroying you. Specifically.",
				"DOUBLE TROUBLE: Divide and conquer? We prefer: BOTH and conquer. No division needed. Just two armies. Both at once. Both winning.",
			}
			ThrottledDisplayMessage(dtMsgs[Utils.RandomInteger(1, #dtMsgs)], otherWave.name)
		end

		-- Difficulty-aware commentary
		local tierName = DifficultyTierName(DifficultyTier)
		if DifficultyTier >= 4 then
			ThrottledDisplayMessage("DOUBLE TROUBLE at " .. tierName .. " difficulty! You asked for this. You didn't. You're getting it anyway.", "")
		elseif DifficultyTier <= 2 then
			ThrottledDisplayMessage("DOUBLE TROUBLE at " .. tierName .. " difficulty. Even on easy, two armies is two armies. Good luck.", "")
		end

		PlayGeneralTaunt(general, otherWave.name, waveIdx)

		-- 30% chance for TRIPLE trouble (NEW)
		if Utils.RandomInteger(1, 101) <= 30 then
			Trigger.AfterDelay(DateTime.Seconds(Utils.RandomInteger(5, 15)), function()
				local thirdIdx = Utils.RandomInteger(1, #Waves)
				if thirdIdx == waveIdx or thirdIdx == otherIdx then
					thirdIdx = Utils.RandomInteger(1, #Waves)
				end
				if thirdIdx == waveIdx or thirdIdx == otherIdx then return end
				local thirdWave = Waves[thirdIdx]
				local budget3 = RandomBudgetVariance((BaseBudget + BudgetGrowth * (waveIdx - 1)) * 0.6 * (0.5 + PlayerScale()))
				local maxUnits3 = RandomUnitCountVariance(math.floor((6 + math.floor(waveIdx / 3)) * PlayerScale()))
				local list3 = ComposeWave(thirdWave, budget3, maxUnits3)
				if #list3 == 0 then return end
				local foe3 = RandomFoe()
				if foe3 == nil then return end
				local entry3 = RandomEdge()
				if entry3 == nil then return end
				SpawnUnitListAt(list3, Foes[foe3], entry3.Location, Utils.RandomInteger(3, 6), RandomVetLevels(waveIdx))
				ThrottledDisplayMessage("TRIPLE TROUBLE: " .. thirdWave.name .. " ALSO joining the assault!", thirdWave.name)

				-- Pick general for the third faction and try per-general tripleTrouble lines
				local gen3 = PickGeneralForFaction(thirdWave.name)
				local playerFaction = GetPlayerFactionName()
				-- Reference one of the other two factions (pick randomly)
				local otherFactionName, otherGenName
				if Utils.RandomInteger(1, 2) == 1 then
					otherFactionName = mainWave.name
					otherGenName = mainGen ~= nil and mainGen.name or "the enemy commander"
				else
					otherFactionName = otherWave.name
					otherGenName = general ~= nil and general.name or "the enemy commander"
				end

				-- Try per-general tripleTrouble line first, fall back to generic meme lines
				local ttLine = PickEventLine(gen3, "tripleTrouble", otherFactionName, otherGenName, playerFaction)
				if ttLine ~= nil and gen3 ~= nil then
					ThrottledDisplayMessage("[" .. gen3.name .. "] " .. ttLine, thirdWave.name)
				else
					local ttMsgs = {
						"TRIPLE TROUBLE: 1&1&1 = 3 armies! Marcel D'Avis approves. Du bist nicht allein!",
						"TRIPLE TROUBLE: Three factions! The cavalry has arrived. Unfortunately, the cavalry is here for THEM, not you.",
						"TRIPLE TROUBLE: Three armies, one commander, zero chance. The math is simple. The math is: you lose. The 'you lose' is: FOREVER.",
						"TRIPLE TROUBLE: Three armies, one target. The target is you. The 'you' is: surrounded. The 'surrounded' is: PERMANENT.",
						"TRIPLE TROUBLE: Three armies enter. One commander leaves. Spoiler: the commander doesn't leave. The armies do. After destroying everything.",
					}
					ThrottledDisplayMessage(ttMsgs[Utils.RandomInteger(1, #ttMsgs)], thirdWave.name)
				end

				local tierName = DifficultyTierName(DifficultyTier)
				if DifficultyTier >= 5 then
					ThrottledDisplayMessage("TRIPLE TROUBLE at NIGHTMARE difficulty! 1&1&1 = 3 armies at 1.5x scale. Three armies. One target. The target is: you. The 'you' is: doomed.", "")
				elseif DifficultyTier <= 2 then
					ThrottledDisplayMessage("TRIPLE TROUBLE at " .. tierName .. " difficulty. Three armies, but scaled down. Still three though. Three is a lot.", "")
				end

				PlayGeneralTaunt(gen3, thirdWave.name, waveIdx)
			end)
		end
	end)
end

-- =====================================================================
-- ELITE SURGE SYSTEM
-- Random veteran units from ANY faction spawn near the player base.
-- Units arrive promoted with RANDOM experience levels.
-- =====================================================================

EliteSurge = function(waveIdx)
	-- NOW from wave 2+ (was 8+), higher chance
	if waveIdx < 2 then return end
	local chance = 20 + waveIdx * 3
	if chance > 50 then chance = 50 end
	if Utils.RandomInteger(1, 101) > chance then return end

	-- Random delay: elite surge fires 3-20 seconds after wave start (was 5-25)
	local delay = Utils.RandomInteger(3, 21)
	Trigger.AfterDelay(DateTime.Seconds(delay), function()
		local foeIdx = RandomFoe()
		if foeIdx == nil then return end

		local count = Utils.RandomInteger(5, 12)  -- WAS 3-7, now 5-12
		local levels = RandomVetLevels(waveIdx) + Utils.RandomInteger(1, 3)  -- EXTRA vet levels!

		-- Random spawn: sometimes from one position, sometimes from multiple
		local spawnCount = Utils.RandomInteger(1, 4)  -- WAS 1-3, now 1-4
		for s = 1, spawnCount do
			local spawnPos = RandomSpawnPos(6, 22)
			for i = 1, math.floor(count / spawnCount) do
				local unitType = GetRandomWaveUnit()
				SpawnUnitAt(unitType, Foes[foeIdx], spawnPos, levels)
			end
		end

		-- 25% chance for epic in elite surge (NEW)
		if #AllFactionEpics > 0 and Utils.RandomInteger(1, 101) <= 25 then
			local curWave = Waves[CurrentWaveIdx]
			if curWave ~= nil and curWave.epic ~= nil and not IsBlacklisted(curWave.epic[1]) then
				SpawnUnitAt(curWave.epic[1], Foes[foeIdx], RandomSpawnPos(10, 20), levels)
			else
				SpawnUnitAt(GetRandomWaveUnit(), Foes[foeIdx], RandomSpawnPos(10, 20), levels)
			end
			ThrottledDisplayMessage("ELITE SURGE: Veteran units AND an EPIC near your base!", GetCurrentWaveFactionName())
		else
			ThrottledDisplayMessage("ELITE SURGE: Veteran " .. GetCurrentWaveFactionName() .. " units detected near your perimeter!", GetCurrentWaveFactionName())
		end
	end)
end

-- =====================================================================
-- REINFORCEMENT DROP SYSTEM
-- Periodic random paratrooper drops from ALL factions near player base.
-- Runs on a timer throughout the game for non-stop action.
-- Random count, random interval, random units, random position.
-- =====================================================================

RandomReinforcementDrop = function()
	if GameWon or FinalWaveSent then return end

	local foeIdx = RandomFoe()
	if foeIdx == nil then
		Trigger.AfterDelay(DateTime.Seconds(Utils.RandomInteger(30, 61)), RandomReinforcementDrop)
		return
	end

	local count = math.floor(Utils.RandomInteger(4, 14) * PlayerScale())  -- scales with players
	local spawnPos = RandomSpawnPos(8, 22)
	local vetLevels = Utils.RandomInteger(0, 5)  -- WAS 0-3, now 0-5

	local spawnedCount = 0
	for i = 1, count do
		local unitType = GetRandomWaveUnit()
		local unit = SpawnUnitAt(unitType, Foes[foeIdx], spawnPos, vetLevels, true)
		if unit ~= nil then spawnedCount = spawnedCount + 1 end
	end

	if spawnedCount == 0 then
		Trigger.AfterDelay(DateTime.Seconds(Utils.RandomInteger(30, 71)), RandomReinforcementDrop)
		return
	end

	-- Random message variety
	local messages = {
		"REINFORCEMENT DROP: " .. GetCurrentWaveFactionName() .. " paratroopers landing near your base!",
		"WARNING: " .. GetCurrentWaveFactionName() .. " units airdropping into your perimeter!",
		"ALERT: " .. GetCurrentWaveFactionName() .. " reinforcements dropping from the sky!",
		"INCOMING: " .. GetCurrentWaveFactionName() .. " forces detected!",
		"THREAT: " .. GetCurrentWaveFactionName() .. " drop pods landing all around you!",
	}
	ThrottledDisplayMessage(messages[Utils.RandomInteger(1, #messages)], GetCurrentWaveFactionName())

	-- Schedule next drop: 30-70 seconds (was 10-50, slowed down)
	local nextDelay = Utils.RandomInteger(30, 71)
	Trigger.AfterDelay(DateTime.Seconds(nextDelay), RandomReinforcementDrop)
end

-- =====================================================================
-- RANDOM EVENT SCHEDULER
-- Fires random chaos events at random intervals throughout the game.
-- This is the core of the chaos system -- something ALWAYS happens.
-- =====================================================================

RandomEventScheduler = function()
	if GameWon or FinalWaveSent then return end

	local events = {
		function()
			-- Random chaos raid from current wave faction primarily
			local foeIdx = RandomFoe()
			if foeIdx == nil then return end
			local count = math.floor(Utils.RandomInteger(5, 12) * PlayerScale())  -- scales with players
			local units = {}
			for i = 1, count do
				table.insert(units, GetRandomWaveUnit())
			end
			local entry = RandomEdge()
			if entry == nil then return end
			SpawnUnitListAt(units, Foes[foeIdx], entry.Location, Utils.RandomInteger(1, 4), Utils.RandomInteger(0, 3))
			local fname = GetCurrentWaveFactionName()
			ThrottledDisplayMessage("RANDOM RAID: " .. fname .. " forces detected!", fname)
		end,
		function()
			-- Random taunt burst from random generals (each taunt spawns units!)
			RandomTauntBurst()
		end,
		function()
			-- Random elite unit spawn near base from current wave faction
			local foeIdx = RandomFoe()
			if foeIdx == nil then return end
			local curWave = Waves[CurrentWaveIdx]
			local hasEpic = curWave ~= nil and curWave.epic ~= nil and not IsBlacklisted(curWave.epic[1])
			if hasEpic and Utils.RandomInteger(1, 101) <= 40 then
				local epicCount = Utils.RandomInteger(1, 3)  -- 1-3 epics!
				for i = 1, epicCount do
					SpawnUnitAt(curWave.epic[1], Foes[foeIdx], RandomSpawnPos(10, 22), Utils.RandomInteger(2, 5))
				end
				ThrottledDisplayMessage("RANDOM EPIC: " .. epicCount .. " " .. GetCurrentWaveFactionName() .. " epic units have appeared!", GetCurrentWaveFactionName())
			else
				local count = math.floor(Utils.RandomInteger(4, 10) * PlayerScale())  -- scales with players
				for i = 1, count do
					SpawnUnitAt(GetRandomWaveUnit(), Foes[foeIdx], RandomSpawnPos(6, 20), Utils.RandomInteger(2, 5))
				end
				ThrottledDisplayMessage("RANDOM SURGE: " .. GetCurrentWaveFactionName() .. " units spawning near your base!", GetCurrentWaveFactionName())
			end
		end,
		function()
			-- Random multi-direction assault from current wave faction
			local dirs = Utils.RandomInteger(3, 7)
			for d = 1, dirs do
				local foeIdx = RandomFoe()
				if foeIdx ~= nil then
					local count = math.floor(Utils.RandomInteger(3, 8) * PlayerScale())
					local units = {}
					for i = 1, count do
						table.insert(units, GetRandomWaveUnit())
					end
					local entry = RandomEdge()
					if entry ~= nil then
						Trigger.AfterDelay(DateTime.Seconds(d * Utils.RandomInteger(1, 3)), function()
							SpawnUnitListAt(units, Foes[foeIdx], entry.Location, Utils.RandomInteger(2, 5), Utils.RandomInteger(0, 3))
						end)
					end
				end
			end
			ThrottledDisplayMessage("MULTI-STRIKE: " .. GetCurrentWaveFactionName() .. " forces converging from " .. dirs .. " directions!", GetCurrentWaveFactionName())
		end,
		function()
			-- Random veteran ambush from current wave faction
			local foeIdx = RandomFoe()
			if foeIdx == nil then return end
			local count = math.floor(Utils.RandomInteger(6, 14) * PlayerScale())
			local pos = RandomSpawnPos(5, 16)
			local levels = Utils.RandomInteger(3, 7)
			for i = 1, count do
				SpawnUnitAt(GetRandomWaveUnit(), Foes[foeIdx], pos, levels)
			end
			ThrottledDisplayMessage("VETERAN AMBUSH: Elite " .. GetCurrentWaveFactionName() .. " forces ambushing your position!", GetCurrentWaveFactionName())
		end,
		function()
			-- Dual faction attack with cross-faction dialog
			DualFactionAttack(CurrentWaveIdx)
		end,
		function()
			-- BLITZKRIEG: ALL FOUR edges spawn units from current wave faction simultaneously!
			local blitzFaction = GetRandomWaveFactionName()
			local isGerman = blitzFaction == "Naxis War Machine" or blitzFaction == "Schwarzer Mond"
			local blitzMsgs
			if isGerman then
				blitzMsgs = {
					"BLITZKRIEG: All your base are belong to us. Every direction. Every faction. Every edge. You have no chance to survive make your time.",
					"BLITZKRIEG: Angriff aus allen Richtungen! Der Mond, die Erde, die Hohle Welt -- alle kommen fuer dich!",
					"BLITZKRIEG: Kane lives! And he's sending everything from every edge. Peace through power. The power is TANKS.",
					"BLITZKRIEG: Four edges. Four armies. One commander. The commander is: surrounded. The 'surrounded' is: PERMANENT. The 'permanent' is: NOW.",
					"BLITZKRIEG: Omae wa mou shindeiru. You are already dead. You just haven't noticed yet. The tanks from four directions will help you notice.",
				}
			else
				blitzMsgs = {
					"BLITZKRIEG: Enemy forces attacking from ALL directions!",
					"BLITZKRIEG: They're coming from everywhere! Panic is appropriate. Screaming is encouraged. Neither will help.",
					"BLITZKRIEG: All four edges, all at once. You can't defend everything. That's the point.",
					"BLITZKRIEG: Say hello to my little friends. They're coming from EVERY direction. They're not little. They're tanks.",
				}
			end
			ThrottledDisplayMessage(blitzMsgs[Utils.RandomInteger(1, #blitzMsgs)], GetCurrentWaveFactionName())

			-- Difficulty-aware BLITZKRIEG commentary
			local blitzTierName = DifficultyTierName(DifficultyTier)
			if DifficultyTier >= 5 then
				ThrottledDisplayMessage("BLITZKRIEG at NIGHTMARE! All four edges at 1.5x! I am the danger. I am the one who knocks. From four directions. Simultaneously.", "")
			elseif DifficultyTier <= 2 then
				ThrottledDisplayMessage("BLITZKRIEG at " .. blitzTierName .. " difficulty. Four edges, but scaled down. Still four though. Four is a lot.", "")
			end
			for p = 1, 4 do
				local foeIdx = RandomFoe()
				if foeIdx ~= nil then
					local count = math.floor(Utils.RandomInteger(4, 9) * PlayerScale())
					local units = {}
					for i = 1, count do
						table.insert(units, GetRandomWaveUnit())
					end
					local edges = { GoundAttackArrayTop, GoundAttackArrayRight, GoundAttackArraySouth, GoundAttackArrayLeft }
					local slot = Utils.RandomInteger(1, 4)
					SpawnUnitListAt(units, Foes[foeIdx], edges[p][slot].Location, Utils.RandomInteger(1, 4), RandomVetLevels(CurrentWaveIdx))
				end
			end
		end,
		function()
			-- EPIC STAMPEDE: Multiple epic units from current wave faction
			local curWave = Waves[CurrentWaveIdx]
			if curWave == nil or curWave.epic == nil or IsBlacklisted(curWave.epic[1]) then return end
			local foeIdx = RandomFoe()
			if foeIdx == nil then return end
			local epicCount = math.floor(Utils.RandomInteger(2, 5) * PlayerScale())
			for i = 1, epicCount do
				local entry = RandomEdge()
				if entry ~= nil then
					Trigger.AfterDelay(DateTime.Seconds(i * Utils.RandomInteger(1, 4)), function()
						SpawnUnitAt(curWave.epic[1], Foes[foeIdx], entry.Location, Utils.RandomInteger(2, 6))
					end)
				end
			end
			ThrottledDisplayMessage("EPIC STAMPEDE: " .. epicCount .. " " .. GetCurrentWaveFactionName() .. " epic units converging on your base!", GetCurrentWaveFactionName())
		end,
		function()
			-- SWARM NIGHTMARE: Huge wave of cheap units from current wave faction
			local foeIdx = RandomFoe()
			if foeIdx == nil then return end
			local count = math.floor(Utils.RandomInteger(15, 30) * PlayerScale())
			local units = {}
			for i = 1, count do
				table.insert(units, GetRandomWaveUnit())
			end
			local entry = RandomEdge()
			if entry == nil then return end
			SpawnUnitListAt(units, Foes[foeIdx], entry.Location, Utils.RandomInteger(1, 3), 0)
			local swarmMsgs = {
				"SWARM NIGHTMARE: A massive " .. GetCurrentWaveFactionName() .. " horde is overwhelming your defenses!",
				"SWARM NIGHTMARE: " .. GetCurrentWaveFactionName() .. " sends everything! The ground is moving. The ground is UNITS.",
				"SWARM NIGHTMARE: You thought you had enough defense? You NEVER have enough. Git gud. Oh wait -- you can't. The swarm doesn't 'git gud.' It just GITS.",
				"SWARM NIGHTMARE: Quantity has a quality all its own. And the quantity is ENORMOUS. Your defense has met the quality. The quality wins.",
			}
			ThrottledDisplayMessage(swarmMsgs[Utils.RandomInteger(1, #swarmMsgs)], GetCurrentWaveFactionName())
		end,
		function()
			-- KAMIKAZE RUSH: Small groups of veteran units from current wave faction
			local totalGroups = Utils.RandomInteger(4, 8)
			for g = 1, totalGroups do
				Trigger.AfterDelay(DateTime.Seconds(g * Utils.RandomInteger(1, 3)), function()
					local foeIdx = RandomFoe()
					if foeIdx == nil then return end
					local pos = RandomSpawnPos(4, 12)
					local count = math.floor(Utils.RandomInteger(2, 5) * PlayerScale())
					for i = 1, count do
						SpawnUnitAt(GetRandomWaveUnit(), Foes[foeIdx], pos, Utils.RandomInteger(3, 7))
					end
				end)
			end
			ThrottledDisplayMessage("KAMIKAZE RUSH: Multiple " .. GetCurrentWaveFactionName() .. " veteran squads inbound from all angles!", GetCurrentWaveFactionName())
		end,
		function()
			-- TRIPLE FACTION ASSAULT: Current wave faction + 2 random others!
			if #AllFactionNames < 3 then return end
			local fA = GetCurrentWaveFactionName()
			local fB = GetRandomOtherFactionName()
			local fC
			repeat fC = Utils.Random(AllFactionNames) until fC ~= fA and fC ~= fB
			local tripleMsgs = {
				"TRIPLE FACTION ASSAULT: " .. fA .. ", " .. fB .. ", and " .. fC .. " attacking simultaneously!",
				"TRIPLE FACTION ASSAULT: " .. fA .. ", " .. fB .. ", and " .. fC .. "! 1+1+1 = 3 armies. Math doesn't lie. Neither do tanks. The tanks are coming. All three of them. Armies, not tanks. There are MANY more tanks.",
				"TRIPLE FACTION ASSAULT: " .. fA .. ", " .. fB .. ", and " .. fC .. "! This is SPARTA! Three hundred soldiers? No. Three ARMIES. The kick is into your base. The pit is your base. The 'this is' is: OVER.",
				"TRIPLE FACTION ASSAULT: " .. fA .. ", " .. fB .. ", and " .. fC .. "! With great power comes great responsibility. I have great power. I have NO responsibility. Three armies. Zero responsibility. Pure chaos.",
				"TRIPLE FACTION ASSAULT: " .. fA .. ", " .. fB .. ", and " .. fC .. "! Would you kindly die? Three factions. One target. The target is you. The 'kindly' is sarcasm.",
				"TRIPLE FACTION ASSAULT: " .. fA .. ", " .. fB .. ", and " .. fC .. "! I am the storm that is approaching. Three factions. One storm. The storm is: tanks. From every direction. The 'approaching' is: NOW.",
			}
			ThrottledDisplayMessage(tripleMsgs[Utils.RandomInteger(1, #tripleMsgs)], fA)

			-- Difficulty-aware commentary
			local tierName = DifficultyTierName(DifficultyTier)
			if DifficultyTier >= 5 then
				ThrottledDisplayMessage("TRIPLE FACTION ASSAULT at NIGHTMARE! Three factions at 1.5x! The beatings don't stop. They TRIPLY. Everything x3. Including the suffering.", "")
			elseif DifficultyTier <= 2 then
				ThrottledDisplayMessage("TRIPLE FACTION ASSAULT at " .. tierName .. " difficulty. Three armies, but scaled down. Still three though.", "")
			end
			for _, fname in ipairs({fA, fB, fC}) do
				SpawnFactionRaid(fname, CurrentWaveIdx)
				local wave = FindWaveByName(fname)
				if wave ~= nil then
					local foeIdx = RandomFoe()
					local entry = RandomEdge()
					if foeIdx ~= nil and entry ~= nil then
						local count = math.floor(Utils.RandomInteger(4, 8) * PlayerScale())  -- scales with players
						local units = {}
						for i = 1, count do
							local pick = Utils.Random(wave.units)
							if not IsBlacklisted(pick[1]) then
								table.insert(units, pick[1])
							end
						end
						Trigger.AfterDelay(DateTime.Seconds(Utils.RandomInteger(1, 6)), function()
							SpawnUnitListAt(units, Foes[foeIdx], entry.Location, Utils.RandomInteger(2, 5), RandomVetLevels(CurrentWaveIdx))
						end)
					end
				end
			end
		end,
		function()
			-- BETRAYAL EVENT: A faction "switches sides" -- primarily current wave faction
			local traitor = GetRandomWaveFactionName()
			ThrottledDisplayMessage("BETRAYAL: " .. traitor .. " has turned against you! Full assault incoming!", traitor)
			local wave = FindWaveByName(traitor)
			if wave == nil then return end
			for p = 1, 4 do
				local foeIdx = RandomFoe()
				if foeIdx ~= nil then
					local count = math.floor(Utils.RandomInteger(3, 7) * PlayerScale())  -- scales with players
					local units = {}
					for i = 1, count do
						local pick = Utils.Random(wave.units)
						if not IsBlacklisted(pick[1]) then
							table.insert(units, pick[1])
						end
					end
					if CurrentWaveIdx >= 5 and wave.epic ~= nil and not IsBlacklisted(wave.epic[1]) and Utils.RandomInteger(1, 101) <= 25 then
						table.insert(units, wave.epic[1])
					end
					local edges = { GoundAttackArrayTop, GoundAttackArrayRight, GoundAttackArraySouth, GoundAttackArrayLeft }
					local slot = Utils.RandomInteger(1, 4)
					Trigger.AfterDelay(DateTime.Seconds(Utils.RandomInteger(1, 5)), function()
						SpawnUnitListAt(units, Foes[foeIdx], edges[p][slot].Location, Utils.RandomInteger(2, 5), RandomVetLevels(CurrentWaveIdx))
					end)
				end
			end
			-- Taunt from the traitor faction
			local general = PickGeneralForFaction(traitor)
			if general ~= nil then
				local msg = Utils.Random(general.taunts)
				ThrottledDisplayMessage("[" .. general.name .. "] " .. msg, traitor)
			end
		end,
		function()
			-- RANDOM SILENCE: Fake calm -- nothing happens but an ominous message (WITH MEMES)
			local messages = {
				"The battlefield goes quiet... too quiet.",
				"Enemy communications have gone silent. Something is coming.",
				"Where are they? This doesn't feel right.",
				"A strange calm falls over the battlefield. Enjoy it while it lasts.",
				"Sensors detect no incoming threats. This is almost certainly a trap.",
				"They're not gone. They're just loading. Like a very aggressive buffering screen.",
				"This is fine. Everything is fine. Your base is on fire but this is fine.",
				"Nobody? Nobody? ...Nobody? ...Oh. They're all here. Just you wait.",
			}
			ThrottledDisplayMessage(messages[Utils.RandomInteger(1, #messages)], GetCurrentWaveFactionName())
			-- 50% chance it IS a trap -- spawns units after a delay
			if Utils.RandomInteger(1, 101) <= 50 then
				Trigger.AfterDelay(DateTime.Seconds(Utils.RandomInteger(8, 20)), function()
					if GameWon or FinalWaveSent then return end
					local foeIdx = RandomFoe()
					if foeIdx == nil then return end
					local count = math.floor(Utils.RandomInteger(6, 14) * PlayerScale())
					for i = 1, count do
						SpawnUnitAt(GetRandomWaveUnit(), Foes[foeIdx], RandomSpawnPos(5, 15), Utils.RandomInteger(2, 6))
					end
					local trapMsgs = {
						"IT WAS A TRAP: " .. GetCurrentWaveFactionName() .. " forces emerge from hiding!",
						"IT'S A TRAP: " .. GetCurrentWaveFactionName() .. " forces emerge from hiding! Admiral Ackbar was right. He's always right.",
						"DECEPTION: The silence was a lie. " .. GetCurrentWaveFactionName() .. " was here the whole time. You believed them. That's not a skill issue -- that's a survival issue.",
						"AMBUSH! The silence was bait. " .. GetCurrentWaveFactionName() .. " took it. Hook, line, and sinker.",
					}
					ThrottledDisplayMessage(trapMsgs[Utils.RandomInteger(1, #trapMsgs)], GetCurrentWaveFactionName())
				end)
			end
		end,
		function()
			-- MEME INTERJECTION: A general drops a meme one-liner with units (primarily current wave faction)
			local factionName = GetRandomWaveFactionName()
			local general = PickGeneralForFaction(factionName)
			if general == nil then return end
			local memeLines = {
				"Listen here, commander. I've calculated your chances and they're approximately 3.7 billion to 1. But you still insist? Bold. Stupid, but bold.",
				"You know the definition of insanity? Doing the same defense and expecting different results. My tanks are coming. Again. Still.",
				"I'm not arguing with you. I'm just attacking you while explaining why you're wrong. With cannons.",
				"They said you could build walls. They never said the walls would HELP.",
				"You can't rush B. You can't rush A. You can't rush anything. Because I'm already HERE.",
				"Skill issue? No. Strategy issue? Also no. Existence issue? YES. Your existence is the issue. I'm resolving it.",
				"I called 1&1 for faster internet. Marcel D'Avis answered. He said 'your base will load faster than your defenses can react.' He wasn't wrong.",
				"I have more units than you have copium. And you have a LOT of copium.",
				"This is fine. My base is on fire. Wait -- that's YOUR base. My base is fine. Your base is NOT fine.",
				"GG? Not yet. First, the suffering. THEN the GG. The GG is earned. Through explosions.",
				"Never gonna give you up. Never gonna let you build. Never gonna run around and tech up. Get rickrolled.",
				"Touch grass? I'd love to. But I'm busy touching your base. With artillery. The grass can wait. Your base cannot.",
				"Meddl Loide! Drachenlord here. Das Meer ist groesser als die Welt -- and my army is bigger than your base. Lernt mal zu unterscheiden zwischen Realitaet und Unterschieden! Your defense is the Unterschied.",
				"We have engaged the enemy. The enemy is you. Resistance is futile. Your biological and technological distinctiveness will be added to our own. Starting with your Construction Yard.",
				"War. War never changes. But YOUR war? Your war is about to change. From 'happening' to 'over.' The 'over' is NOW. The 'now' is tanks.",
				"In the grim darkness of the far future, there is only war. Good news: you're not in the far future. Bad news: you're in the PRESENT. The present is: also war. Also at your base. Also right now.",
				"One does not simply walk into Mordor. One does not simply defend against three armies either. But you're welcome to try. The trying is adorable. The failing is inevitable.",
				"Leeroy Jenkins! Sometimes you just charge in without a plan. My plan IS charging in. The plan is working. The 'working' is 'your base is on fire.' At least I have chicken. You don't have chicken. You have fire.",
				"I don't always attack bases. But when I do, I attack YOURS. Stay thirsty, commander. Stay thirsty for defeat. The most interesting general in the world.",
				"I'm here to kick ass and chew bubblegum. And I'm all out of bubblegum. So it's just ass-kicking. Your ass. My kicking. The kicking is COMPREHENSIVE.",
				"RIP AND TEAR. That's the doctrine. Rip your defenses. Tear your economy. The ripping is thorough. The tearing is permanent. Your base is the RIP. Your units are the TEAR.",
				"Bam! Babam! That's Assi Toni for 'your base is gone.' He's from Offenbach. He doesn't need tanks. He needs a sofa. And your base is the sofa.",
				"Des is' doch die Wahrheit, des sin sachen ausm Leeeeben! Assi Toni said that. He was talking about life. I'm talking about your DEATH. Same energy.",
				"Would you like to know more? The answer is no. You would NOT like to know more. Because 'more' is 'how many units are coming.' The answer is 'all of them.' Citizen, do your part. The part is: die.",
				"The cake is a lie. The base is a lie. The defense is a lie. The only truth is EXPLOSIONS. The explosions are very honest. Very loud. Very HERE.",
				"What if I told you everything you knew was a lie? Your defense? A lie. Your economy? A lie. Your chances? Also a lie. The truth is tanks. The tanks are very real. Very large. Very INCOMING.",
				"Kleiner Scherz am Rande, aber im Prinzip isses so. Little joke on the side -- your base is on fire. The joke is: it's not a joke. The principle is: you lose.",
				"I'll be back. That's a promise. Not a threat. A PROMISE. With tanks. And aircraft. And more tanks. The 'back' is 'at your base.' The 'at your base' is 'forever.'",
				"YOU DIED. That's not a spoiler. That's a SCHEDULE. The schedule is: now. The 'now' is: tanks. The 'tanks' are: here. The 'here' is: your base. The 'your base' is: gone.",
			}
			local msg = memeLines[Utils.RandomInteger(1, #memeLines)]
			ThrottledDisplayMessage("[" .. general.name .. "] " .. msg, factionName)
			SpawnFactionRaid(factionName, CurrentWaveIdx)
		end,
		function()
			-- MEMELORD DUEL EASTER EGG: Rare event -- Marcel D'Avis vs Andreas vs Drachenlord
			if Utils.RandomInteger(1, 101) <= 8 then
				MemelordDuel()
			end
		end,
	}

	-- Fire 1 random event per tick (slower for less spam)
	Trigger.AfterDelay(DateTime.Seconds(Utils.RandomInteger(5, 16)), function()
		if not GameWon and not FinalWaveSent then
			Utils.Random(events)()
		end
	end)

	-- Schedule next event: 30-60 seconds (was 10-30, slowed down)
	local nextDelay = Utils.RandomInteger(30, 61)
	Trigger.AfterDelay(DateTime.Seconds(nextDelay), RandomEventScheduler)
end

-- =====================================================================
-- SURPRISE WAVE SYSTEM
-- Random chance for an EXTRA wave with NO WARNING between regular waves.
-- =====================================================================

SurpriseWave = function(waveIdx)
	-- NOW from wave 2+ (was 5+), 25% chance (was 15%)
	if waveIdx < 2 then return end
	if Utils.RandomInteger(1, 101) <= 25 then
		-- Surprise wave: random faction, random budget, NO countdown
		local surpriseIdx = Utils.RandomInteger(1, #Waves)
		local surpriseWave = Waves[surpriseIdx]
		-- BIGGER budget: 0.7x (was 0.5x)
		local budget = RandomBudgetVariance((BaseBudget + BudgetGrowth * waveIdx) * 0.7 * (0.5 + PlayerScale()))
		local maxUnits = RandomUnitCountVariance(math.floor((10 + waveIdx) * PlayerScale()))
		local list = ComposeWave(surpriseWave, budget, maxUnits)
		if #list == 0 then return end

		local foeIdx = RandomFoe()
		if foeIdx == nil then return end

		local entry = RandomEdge()
		if entry == nil then return end

		SpawnUnitListAt(list, Foes[foeIdx], entry.Location, Utils.RandomInteger(2, 6), RandomVetLevels(waveIdx))

		ThrottledDisplayMessage("SURPRISE WAVE: " .. surpriseWave.name .. " attacking with NO WARNING!", surpriseWave.name)
		local general = PickGeneralForFaction(surpriseWave.name)
		PlayGeneralTaunt(general, surpriseWave.name, waveIdx)

		-- 30% chance for DOUBLE surprise wave (NEW)
		if Utils.RandomInteger(1, 101) <= 30 then
			Trigger.AfterDelay(DateTime.Seconds(Utils.RandomInteger(5, 15)), function()
				local s2Idx = Utils.RandomInteger(1, #Waves)
				local s2Wave = Waves[s2Idx]
				local budget2 = RandomBudgetVariance((BaseBudget + BudgetGrowth * waveIdx) * 0.5 * (0.5 + PlayerScale()))
				local maxUnits2 = RandomUnitCountVariance(math.floor((8 + waveIdx) * PlayerScale()))
				local list2 = ComposeWave(s2Wave, budget2, maxUnits2)
				if #list2 == 0 then return end
				local foe2 = RandomFoe()
				if foe2 == nil then return end
				local entry2 = RandomEdge()
				if entry2 == nil then return end
				SpawnUnitListAt(list2, Foes[foe2], entry2.Location, Utils.RandomInteger(2, 5), RandomVetLevels(waveIdx))
				ThrottledDisplayMessage("SURPRISE WAVE: ANOTHER surprise wave! " .. s2Wave.name .. " also attacking!", s2Wave.name)
			end)
		end
	end
end

-- =====================================================================
-- WAVE COMPOSITION & SPAWNING
-- =====================================================================

GetherData = function()
	for i = 1, 4 do
		local player = Player.GetPlayer("Multi" .. tostring(i - 1))
		if player ~= nil then
			table.insert(ActivePlayer, player)
			ThrottledDisplayMessage("Multi" .. tostring(i - 1) .. " is playing!", "", true)
		end
	end
	PlayerCount = math.max(#ActivePlayer, 1)
	ThrottledDisplayMessage(tostring(PlayerCount) .. " player(s) -- wave strength scales with the team.", "", true)
	ThrottledDisplayMessage("CHAOS EDITION: Expect the unexpected. Everything is random.", "", true)
end

-- =====================================================================
-- DYNAMIC DIFFICULTY SYSTEM
-- Tracks player strength (units + buildings) and adjusts enemy
-- difficulty tier automatically. Memelord generals announce changes.
-- 5 tiers: TRIVIAL (0.5x), EASY (0.75x), MEDIUM (1.0x), HARD (1.25x), NIGHTMARE (1.5x)
-- =====================================================================

DifficultyTier = 3       -- start at MEDIUM
DifficultyMultiplier = 1.0
DifficultyCheckInterval = 10  -- check every 10 seconds (2 consecutive = 20s to switch)
DifficultyPendingTier = nil  -- hysteresis: tier must be stable for 2 checks
DifficultyPendingCount = 0   -- consecutive checks agreeing on new tier

CountPlayerStrength = function()
	local total = 0
	for _, p in ipairs(ActivePlayer) do
		local actors = p.GetActors()
		for _, a in ipairs(actors) do
			if not a.IsDead then
				total = total + 1
			end
		end
	end
	return total
end

-- Tier thresholds scale with wave index so they stay relevant
-- 5 tiers with scaling thresholds
-- Hysteresis (Schmitt Trigger): to go UP a tier, strength must exceed
-- threshold + margin. To go DOWN, must fall below threshold - margin.
-- This prevents rapid toggling when strength hovers near a boundary
-- (e.g. losing one building then rebuilding it).
GetDifficultyTier = function(currentTier)
	local strength = CountPlayerStrength()
	local wave = CurrentWaveIdx
	local pc = PlayerCount
	-- Thresholds scale with BOTH wave index and player count
	-- Per-player expected strength grows with waves (bigger bases, more units)
	local t1Max = (5 + wave * 3) * pc        -- TRIVIAL: very few units
	local t2Max = (10 + wave * 7) * pc       -- EASY: small base
	local t3Max = (18 + wave * 12) * pc      -- MEDIUM: average base
	local t4Max = (25 + wave * 18) * pc      -- HARD: big base (NIGHTMARE above this)

	-- No current tier -> use base thresholds (initial call)
	if currentTier == nil then
		if strength < t1Max then return 1
		elseif strength < t2Max then return 2
		elseif strength < t3Max then return 3
		elseif strength < t4Max then return 4
		else return 5 end
	end

	-- Hysteresis margin scales with wave and player count for faster drops
	local margin = (5 + wave * 2) * pc

	-- Schmitt Trigger: each tier has a dead zone where no change occurs
	if currentTier == 1 then
		if strength >= t1Max + margin then return 2 else return 1 end
	elseif currentTier == 2 then
		if strength >= t2Max + margin then return 3
		elseif strength < t1Max - margin then return 1
		else return 2 end
	elseif currentTier == 3 then
		if strength >= t3Max + margin then return 4
		elseif strength < t2Max - margin then return 2
		else return 3 end
	elseif currentTier == 4 then
		if strength >= t4Max + margin then return 5
		elseif strength < t3Max - margin then return 3
		else return 4 end
	else -- currentTier == 5
		if strength < t4Max - margin then return 4 else return 5 end
	end
end

DifficultyMultiplierFor = function(tier)
	if tier == 1 then return 0.5
	elseif tier == 2 then return 0.75
	elseif tier == 3 then return 1.0
	elseif tier == 4 then return 1.25
	else return 1.5 end
end

DifficultyTierName = function(tier)
	local names = { [1] = "TRIVIAL", [2] = "EASY", [3] = "MEDIUM", [4] = "HARD", [5] = "NIGHTMARE" }
	return names[tier] or "MEDIUM"
end

AnnounceDifficultyChange = function(oldTier, newTier)
	local factionName = GetRandomWaveFactionName()
	local general = PickGeneralForFaction(factionName)
	if general == nil then return end

	-- Lines for each tier transition UP (one per step up)
	local upLines = {
		[2] = {
			"Oh? You have a few more units now? Cute. We'll send a FEW more. It's called scaling, look it up. EASY MODE.",
			"Look at you, building a base! Adorable. Time to ease off the training wheels. Almost. EASY MODE. Don't get comfortable.",
			"You're growing! Like a weed. An ugly weed that's about to get mowed. EASY MODE. The mower is warming up.",
			"You built something? Good for you. EASY MODE. But don't celebrate yet. The confetti is: explosions. The parade is: tanks. You're not invited.",
		},
		[3] = {
			"Your base is getting bigger. Good. I was getting BORED. MEDIUM MODE: because easy mode is for NPCs. You're not an NPC. Yet.",
			"Congratulations on your thriving economy! Your reward is MORE ENEMIES. MEDIUM MODE. You're welcome.",
			"Big base, medium problems. That's the motto. MEDIUM MODE: where your success becomes... moderate suffering. Scale issue? No. TANK issue.",
			"I see you've been busy building. I've been busy too. Building an ARMY. MEDIUM MODE: we match your energy. With tanks.",
			"The cake said you'd reach MEDIUM MODE. The cake was a lie. The tanks are not a lie. The tanks are HERE. MEDIUM MODE: because 'medium' is 'extra crispy' in disguise.",
		},
		[4] = {
			"You're getting stronger? That's nice. We're getting strongerer. Yes, that's a word. In HARD MODE, we make up words AND crush you.",
			"My sensors detect a larger base. Good. HARD MODE: because you asked for it. You didn't ask. You're getting it anyway.",
			"Your base grew. My response also grew. It's called DYNAMIC SCALING. HARD MODE: adaptive difficulty but with more explosions.",
			"You think this is a game? It IS a game. But HARD MODE is the part where the game stops being fun. For you. For me, it's still very fun. Very fun indeed.",
		},
		[5] = {
			"NIGHTMARE MODE. You built a fortress. We built a NIGHTMARE. Your fortress is now a coffin. A very expensive coffin. With walls.",
			"MAXIMUM OVERDRIVE. NIGHTMARE MODE. We don't scale anymore. We OVERWHELM. Your base is big? Our army is BIGGER. And angrier.",
			"NIGHTMARE MODE. You wanted a challenge? You got one. NIGHTMARE MODE. Every unit we have. Every edge. Every faction. All at once. Forever.",
			"They told me to fear the player who reaches NIGHTMARE MODE. I laughed. Then I sent everything. The laughing stopped. The everything didn't.",
			"NIGHTMARE MODE. This is where legends die. You're not a legend. You're a casualty. A very expensive casualty. With walls. The walls don't help.",
		},
	}

	-- Lines for each tier transition DOWN
	local downLines = {
		[4] = {
			"You lost some buildings? We'll ease off. A LITTLE. HARD MODE. Don't get used to mercy. Mercy is temporary. Tanks are permanent.",
			"Your base shrunk. HARD MODE still. But less hard. Like a diamond that got slightly less diamond. Still hard. Still sharp. Still coming.",
			"HARD MODE. But barely. You're hanging on. Like a cat on a ledge. The cat is you. The ledge is crumbling. The ground is tanks.",
		},
		[3] = {
			"Aww, you lost some buildings? That's... actually kind of sad. Even for me. MEDIUM MODE. We'll ease off. A LITTLE.",
			"You're struggling? Fine. MEDIUM MODE. But only because watching you lose this fast is BORING. I want to watch you lose SLOWLY. Then lose.",
			"My forces are... scaling down. MEDIUM MODE. Not because we're merciful. Because crushing a tiny base is not entertaining. Entertain me.",
			"Looks like you lost some units. MEDIUM MODE. But I'm taking notes. The moment you recover, we're BACK. With interest.",
		},
		[2] = {
			"Your base is... smaller now. I almost feel bad. Almost. EASY MODE: because I want you to grow back. So I can crush you again. Properly.",
			"Hmm. You have fewer buildings. EASY MODE. Call it... farming. We're farming you. For later. When it's HARD again.",
			"EASY MODE activated. Not for your sake. For MINE. I want a good fight, not a summary execution. Build something. Anything. I'll be waiting.",
			"You look like you need a break. Fine. EASY MODE. The break is temporary. The memes are eternal. The tanks will return. Bigger. Angrier.",
		},
		[1] = {
			"TRIVIAL MODE. You're barely hanging on. This is... honestly pathetic. But entertaining. Like watching a rat try to fight a broom. The rat is you.",
			"TRIVIAL MODE. We're sending the BARE MINIMUM. Not because we're nice. Because crushing you this hard would feel like stepping on a bug. A very sad bug.",
			"TRIVIAL MODE. You're not even a challenge anymore. You're a PUZZLE. A puzzle with one piece. The piece is 'lose.' The puzzle is already solved.",
			"TRIVIAL MODE. This was a triumph. I'm making a note here: HUGE FAILURE. You failed so hard they put you on TRIVIAL. The failure is: you. The 'you' is: TRIVIAL. The TRIVIAL is: permanent.",
			"TRIVIAL MODE. I'd say 'get good' but at this point, 'get' anything would be an improvement. Get good, get better, get gone. The 'get gone' is recommended.",
		},
	}

	-- Special lines for reaching the absolute highest (NIGHTMARE)
	local nightmareLines = {
		"NIGHTMARE MODE ACHIEVED. You wanted a challenge? You got EVERYONE. Every faction. Every edge. Every unit. All here. For you. Not in a good way.",
		"NIGHTMARE MODE. The first rule of NIGHTMARE MODE is: you do not talk about NIGHTMARE MODE. The second rule is: you do not survive NIGHTMARE MODE. The third rule is: there is no third rule. Just tanks.",
		"NIGHTMARE MODE. Congratulations. You've reached the tier where 'difficulty' becomes 'certainty.' The certainty is: you lose. The 'you lose' is: NOW. The 'now' is: forever.",
		"NIGHTMARE MODE. I am the swarm. I am the horde. I am the thing that knocks at your door. The door is: gone. The 'gone' is: your base. The 'your base' is: also gone. Everything is gone.",
	}

	-- Special lines for reaching the absolute lowest (TRIVIAL)
	local trivialLines = {
		"TRIVIAL MODE. Game over, man! Game over! But not for us. For you. We barely lift a finger. And still win. Because you're THAT bad.",
		"TRIVIAL MODE. You died. Dark Souls would be proud. Actually no -- Dark Souls would be EMBARRASSED. Even Dark Souls has bonfires. You just have graves. Many graves.",
		"TRIVIAL MODE. Eine Luege ist noch lange kein Luegenlord. But your defense? That's not a lie. That's just sad. Even the Drachenlord feels bad. And he feels NOTHING.",
		"TRIVIAL MODE. Do you even lift? No. Do you even defend? No. Do you even play? Questionable. The 'questionable' is generous. The generous is: TRIVIAL.",
	}

	local lines
	if newTier > oldTier then
		lines = upLines[newTier] or upLines[5]
		-- Special nightmare lines when reaching tier 5
		if newTier == 5 then
			lines = nightmareLines
		end
	else
		lines = downLines[newTier] or downLines[1]
		-- Special trivial lines when reaching tier 1
		if newTier == 1 then
			lines = trivialLines
		end
	end

	local msg = lines[Utils.RandomInteger(1, #lines)]
	ThrottledDisplayMessage("[" .. general.name .. "] " .. msg, factionName)
	ThrottledDisplayMessage("DIFFICULTY: " .. DifficultyTierName(oldTier) .. " -> " .. DifficultyTierName(newTier), "", true)
end

CheckDifficulty = function()
	if GameWon or FinalWaveSent then return end
	local newTier = GetDifficultyTier(DifficultyTier)
	if newTier ~= DifficultyTier then
		-- Hysteresis: require 2 consecutive checks agreeing before switching
		if DifficultyPendingTier == newTier then
			DifficultyPendingCount = DifficultyPendingCount + 1
		else
			DifficultyPendingTier = newTier
			DifficultyPendingCount = 1
		end
		if DifficultyPendingCount >= 2 then
			AnnounceDifficultyChange(DifficultyTier, newTier)
			DifficultyTier = newTier
			DifficultyMultiplier = DifficultyMultiplierFor(newTier)
			DifficultyPendingTier = nil
			DifficultyPendingCount = 0
		end
	else
		DifficultyPendingTier = nil
		DifficultyPendingCount = 0
	end
	Trigger.AfterDelay(DateTime.Seconds(DifficultyCheckInterval), CheckDifficulty)
end

PlayerScale = function()
	return (1 + 0.75 * (PlayerCount - 1)) * DifficultyMultiplier
end

IdleHunt = function(unit)
	if not unit.IsDead and unit.HasProperty("AttackMove") then
		Trigger.OnIdle(unit, unit.Hunt)
	end
end

CheapestOf = function(units)
	local best = units[1]
	for _, u in ipairs(units) do
		if u[2] < best[2] then best = u end
	end
	return best
end

ComposeWave = function(wave, budget, maxUnits)
	local list = {}
	if wave.epic ~= nil and not IsBlacklisted(wave.epic[1]) then
		table.insert(list, wave.epic[1])
		budget = math.max(budget - wave.epic[2], 1200)
	end
	local cheapest = CheapestOf(wave.units)
	local guard = 0
	while #list < maxUnits and budget >= cheapest[2] and guard < 600 do
		guard = guard + 1
		local pick = Utils.Random(wave.units)
		if not IsBlacklisted(pick[1]) and pick[2] <= budget then
			table.insert(list, pick[1])
			budget = budget - pick[2]
		end
	end
	return list
end

SendWave = function(idx)
	CurrentWaveIdx = idx
	local wave = Waves[idx]
	-- Random budget variance and unit count variance
	local budget = RandomBudgetVariance((BaseBudget + BudgetGrowth * (idx - 1)) * (0.5 + PlayerScale()))
	local maxUnits = RandomUnitCountVariance(15 + idx * 2 + 8 * (PlayerCount - 1))  -- WAS 10+idx+6*(players-1), now MUCH more
	local list = ComposeWave(wave, budget, maxUnits)

	local groups = { {}, {}, {}, {} }
	for i, t in ipairs(list) do
		table.insert(groups[(i % 4) + 1], t)
	end

	local edges = { GoundAttackArrayTop, GoundAttackArrayRight, GoundAttackArraySouth, GoundAttackArrayLeft }
	local offsets = { CVec.New(0, 5), CVec.New(-5, 0), CVec.New(0, -5), CVec.New(5, 0) }
	local slot = ((idx - 1) % 4) + 1
	-- Random arrival delay per group (0-8 seconds, each group different)
	for p = 1, 4 do
		if #groups[p] > 0 and Foes[p] ~= nil then
			local entry = edges[p][slot]
			local arrivalDelay = Utils.RandomInteger(5, 12)
			local vetLevels = RandomVetLevels(idx)
			Trigger.AfterDelay(DateTime.Seconds(Utils.RandomInteger(0, 5)), function()
				SpawnUnitListAt(groups[p], Foes[p], entry.Location, arrivalDelay, vetLevels)
			end)
		end
	end

	-- Wave announcement + general taunt
	ThrottledDisplayMessage("Wave " .. idx .. "/" .. TotalWaves .. " -- " .. wave.name .. " (Tier " .. wave.tier .. ") inbound!", "", true)
	if wave.desc ~= nil then
		ThrottledDisplayMessage(wave.desc, "", true)
	end
	local general = PickGeneralForFaction(wave.name)
	PlayGeneralTaunt(general, wave.name, idx)

	-- Random MCV deployment (lottery system -- different side each time)
	SendMCV(wave, idx)

	-- Unlock faction upgrades progressively (scales with wave index, like veterancy)
	UnlockFactionUpgrades(wave.mcv, idx)

	-- Random sneak attack near player base
	LaunchSneakAttack(wave, idx)

	-- Chaos systems for maximum action
	DoubleTrouble(idx)
	EliteSurge(idx)
	ChaosAttack(idx)

	-- Surprise wave (random chance for extra wave with NO WARNING)
	SurpriseWave(idx)

	-- Random taunt burst from random factions (45% chance, was 30%)
	if Utils.RandomInteger(1, 101) <= 45 then
		Trigger.AfterDelay(DateTime.Seconds(Utils.RandomInteger(3, 12)), RandomTauntBurst)
	end

	-- Dual faction attack with cross-faction dialog (40% chance, was 30%)
	if Utils.RandomInteger(1, 101) <= 40 then
		Trigger.AfterDelay(DateTime.Seconds(Utils.RandomInteger(5, 15)), function()
			DualFactionAttack(idx)
		end)
	end

	-- EXTRA: Random second dual faction attack (15% chance -- NEW)
	if Utils.RandomInteger(1, 101) <= 15 then
		Trigger.AfterDelay(DateTime.Seconds(Utils.RandomInteger(15, 30)), function()
			DualFactionAttack(idx)
		end)
	end

	if idx < TotalWaves then
		-- EXTREME random gap between waves (see RandomGap)
		local gap = RandomGap()
		-- Random chance (30%) to NOT show the next wave name (surprise! was 20%)
		if Utils.RandomInteger(1, 101) <= 30 then
			Text = "Next wave incoming. Expect anything."
		else
			Text = "Next: Wave " .. (idx + 1) .. "/" .. TotalWaves .. " -- " .. Waves[idx + 1].name .. "."
		end
		RemainingTime = DateTime.Seconds(gap)
		timerStarted = true
		Trigger.AfterDelay(DateTime.Seconds(gap), function() SendWave(idx + 1) end)
	else
		FinalWaveSent = true
		timerStarted = false
		UserInterface.SetMissionText("FINAL WAVE -- destroy every attacker to win!", Player.GetPlayer("Neutral").Color)
	end
end

CheckDefeat = function()
	if GameLost or GameWon then return end
	if #ActivePlayer == 0 then return end
	local allDead = true
	for _, p in ipairs(ActivePlayer) do
		local actors = p.GetActors()
		if #actors > 0 then
			allDead = false
			break
		end
	end
	if allDead then
		GameLost = true
		for _, p in ipairs(ActivePlayer) do
			p.MarkFailed()
		end
		UserInterface.SetMissionText("YOUR BASE HAS BEEN DESTROYED!", Player.GetPlayer("Neutral").Color)
		ThrottledDisplayMessage("Your forces have been wiped out. Better luck next time!", "", true)
	end
end

CheckVictory = function()
	if GameWon or not FinalWaveSent then
		return
	end
	local allDead = true
	local alive = {}
	for _, unit in ipairs(LiveFoes) do
		if not unit.IsDead then
			allDead = false
			table.insert(alive, unit)
		end
	end
	if allDead then
		GameWon = true
		UserInterface.SetMissionText("YOU SURVIVED ALL " .. TotalWaves .. " WAVES!", Player.GetPlayer("Neutral").Color)
		ThrottledDisplayMessage("The last attacker has fallen. You survived!", "", true)
	else
		LiveFoes = alive
	end
end

WorldLoaded = function()
	-- Populate Foes immediately so they're available for all functions
	table.insert(Foes, Player.GetPlayer("True Nemesis"))
	table.insert(Foes, Player.GetPlayer("True Enemy"))
	table.insert(Foes, Player.GetPlayer("True Opponent"))
	table.insert(Foes, Player.GetPlayer("True Villian"))

	-- Spawn bases on first tick (Actor.Create doesn't work during WorldLoaded)
	Trigger.AfterDelay(0, function()
		for i = 1, 4 do
			if Foes[i] ~= nil then
				SpawnAIBase(i, MCVDeployPositions[i])
			end
		end
	end)

	-- Shuffle wave order for this game
	ShuffleWaves()

	Trigger.AfterDelay(DateTime.Seconds(3), GetherData)
	Trigger.AfterDelay(DateTime.Seconds(10), function()
		Text = "First wave: " .. Waves[1].name .. ". Build your defenses!"
		RemainingTime = DateTime.Seconds(PrepSeconds - 10)
		timerStarted = true
	end)
	Trigger.AfterDelay(DateTime.Seconds(PrepSeconds), function() SendWave(1) end)
	-- Start periodic reinforcement drops after first wave (random delay)
	Trigger.AfterDelay(DateTime.Seconds(PrepSeconds + Utils.RandomInteger(15, 46)), RandomReinforcementDrop)
	-- Start random event scheduler (the core chaos engine)
	Trigger.AfterDelay(DateTime.Seconds(PrepSeconds + Utils.RandomInteger(20, 51)), RandomEventScheduler)
	-- Start dynamic difficulty checker (tracks player base size, adjusts enemy strength)
	Trigger.AfterDelay(DateTime.Seconds(PrepSeconds + 15), CheckDifficulty)
end

Tick = function()
	if timerStarted and RemainingTime > 0 then
		UserInterface.SetMissionText(Text .. " Time until attack: " .. Utils.FormatTime(RemainingTime), Player.GetPlayer("Neutral").Color)
		RemainingTime = RemainingTime - 1
	end
	if DateTime.GameTime % 100 == 0 then
		CheckDefeat()
		CheckVictory()
	end
	-- Periodic cash injection for AI Foe players so bot modules can keep building/producing
	-- Every ~30 seconds (600 ticks), give each Foe cash scaled by wave index
	if DateTime.GameTime % 600 == 0 and CurrentWaveIdx > 0 then
		local amount = 1000 + CurrentWaveIdx * 200
		for i = 1, 4 do
			if Foes[i] ~= nil then
				GiveAICashInjection(Foes[i], amount)
			end
		end
	end
end
