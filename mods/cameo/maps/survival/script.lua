-- Crazy Survival — rebalanced wave system (design 2026-07-16)
--
-- 16 waves, ONE faction theme per wave, tech tier ramping T1 -> T4.
-- Each wave has a credit BUDGET that grows every wave and scales with
-- the number of players; the wave is composed from the theme's unit
-- list until the budget (or the unit cap) is reached, so waves stay
-- fair for 1-4 players. Tier-4 waves carry exactly ONE epic unit each.
-- Waves arrive faster than before (90s) but are much smaller than the
-- old 16-batch floods.

GoundAttackArrayTop = {Top1,Top2,Top3,Top4}
GoundAttackArrayRight = {Right1,Right2,Right3,Right4}
GoundAttackArraySouth = {South1,South2,South3,South4}
GoundAttackArrayLeft = {Left1,Left2,Left3,Left4}

Foes = {}
ActivePlayer = {}
Spielerzahl = 0
LiveFoes = {}
FinalWaveSent = false
GameWon = false
SurviveObjectives = {}
RemainingTime = 0
timerStarted = false
Text = ""

PrepSeconds = 60      -- build-up time before wave 1
WaveGapSeconds = 30    -- time between waves
BaseBudget = 1500      -- wave-1 budget at 1 player
BudgetGrowth = 650     -- extra budget per wave index
CenterPos = CPos.New(50, 50)

-- { name, tier, units = { {type, cost}, ... }, epic = {type, cost} or nil }
Waves = {
	{ name = "GDI Task Force",        tier = 1, units = { {"td_gdi_minigunner",100}, {"td_gdi_grenadier",200}, {"td_gdi_humvee",400}, {"td_gdi_battletank",900} } },
	{ name = "Nod Raiding Party",     tier = 1, units = { {"td_nod_minigunner",100}, {"td_nod_rocketsoldier",200}, {"td_nod_buggy",300}, {"td_nod_flamethrower",200}, {"td_nod_lighttank",600} } },
	{ name = "Allied Expedition",     tier = 1, units = { {"ra1_allies_rifleinfantry",100}, {"ra1_allies_ranger",300}, {"ra1_allies_machinegunner",400}, {"ra1_allies_alliedlighttank",500}, {"ra1_allies_alliedmediumtank",700} } },
	{ name = "Soviet Onslaught",      tier = 1, units = { {"ra1_soviets_rifleinfantry",100}, {"ra1_soviets_ak47conscript",200}, {"ra1_soviets_rocketsoldier",300}, {"ra1_soviets_flaktruck",800}, {"ra1_soviets_heavytank",1000} } },
	{ name = "Allied Peacekeepers",   tier = 2, units = { {"ra2_allies_gi",200}, {"ra2_allies_guardiangi",400}, {"ra2_allies_ifv",500}, {"ra2_allies_grizzlytank",750} } },
	{ name = "Red Army",              tier = 2, units = { {"ra2_soviets_conscript",100}, {"ra2_soviets_flaktrooper",300}, {"ra2_soviets_rhinoheavytank",850}, {"ra2_soviets_flaktrack",900}, {"ra2_soviets_v3rocketlauncher",900} } },
	{ name = "Psychic Corps",         tier = 2, units = { {"yuri_initiate",200}, {"yuri_gatlingtrooper",300}, {"yuri_brute",400}, {"yuri_lashertank",600}, {"yuri_magnetron",1300} } },
	{ name = "Asian Alliance Strike", tier = 2, units = { {"asianalliance_japanesesamurai",350}, {"asianalliance_veteranarcher",450}, {"asianalliance_lynxtank",850}, {"asianalliance_phoenix",1800} } },
	{ name = "GDI Walker Column",     tier = 3, units = { {"ts_gdi_lightinfantry",120}, {"ts_gdi_discthrower",300}, {"ts_gdi_wolverine",550}, {"ts_gdi_hovermlrs",900}, {"ts_gdi_titan",950} } },
	{ name = "Nod Shadow Legion",     tier = 3, units = { {"ts_nod_lightinfantry",120}, {"ts_nod_rocketinfantry",300}, {"ts_nod_attackcycle",550}, {"ts_nod_ticktank",800} } },
	{ name = "Imperial Assault",      tier = 3, units = { {"japan_samurai",300}, {"japan_archermaiden",500}, {"japan_chihaheavytank",1200}, {"japan_hovercraftflametank",1700} } },
	{ name = "Consortium Contract",   tier = 3, units = { {"steelconsortium_quantumtank",1600}, {"steelconsortium_defenderbot",3200}, {"steelconsortium_katytank",3800} } },
	{ name = "FutureTech Prototypes", tier = 4, units = { {"futuretech_scoutdroid",200}, {"futuretech_shotgundroid",400}, {"futuretech_cannondroid",525}, {"futuretech_missiledroid",700} }, epic = {"futuretech_futuretank",10000} },
	{ name = "CABAL Uprising",        tier = 4, units = { {"cabal_cyborginfantry",500}, {"cabal_rocketcyborg",650}, {"cabal_tarantula",1000}, {"cabal_manticore",1400} }, epic = {"cabal_berserker",10000} },
	{ name = "The Forgotten",         tier = 4, units = { {"forgotten_mutant",120}, {"forgotten_mutantsoldier",250}, {"forgotten_scoopertank",2250}, {"forgotten_ghoststalker",4000} }, epic = {"forgotten_experimentalmammothtank",6000} },
	{ name = "The Swarm",             tier = 4, units = { {"zerg_zergling",200}, {"zerg_hydralisk",500} }, epic = {"zerg_ultralisk",4400} },
}

GetherData = function()
	for i = 1, 4 do
		local Spieler = Player.GetPlayer("Multi" .. tostring(i - 1))
		if Spieler ~= nil then
			table.insert(ActivePlayer, Spieler)
			Media.DisplayMessage("Multi" .. tostring(i - 1) .. " is playing!", "")
		end
	end
	Spielerzahl = math.max(#ActivePlayer, 1)
	for _, Spieler in ipairs(ActivePlayer) do
		SurviveObjectives[Spieler.InternalName] = Spieler.AddPrimaryObjective("Survive all " .. #Waves .. " waves.")
	end
	table.insert(Foes, Player.GetPlayer("True Nemesis"))
	table.insert(Foes, Player.GetPlayer("True Enemy"))
	table.insert(Foes, Player.GetPlayer("True Opponent"))
	table.insert(Foes, Player.GetPlayer("True Villian"))
	Media.DisplayMessage(tostring(Spielerzahl) .. " player(s) — wave strength scales with the team.", "")
end

PlayerScale = function()
	return 1 + 0.75 * (Spielerzahl - 1)
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
	if wave.epic ~= nil then
		table.insert(list, wave.epic[1])
		budget = math.max(budget - wave.epic[2], 1200)
	end
	local cheapest = CheapestOf(wave.units)
	local guard = 0
	while #list < maxUnits and budget >= cheapest[2] and guard < 400 do
		guard = guard + 1
		local pick = Utils.Random(wave.units)
		if pick[2] <= budget then
			table.insert(list, pick[1])
			budget = budget - pick[2]
		end
	end
	return list
end

SendWave = function(idx)
	local wave = Waves[idx]
	local budget = math.floor((BaseBudget + BudgetGrowth * (idx - 1)) * PlayerScale())
	local maxUnits = 10 + idx + 6 * (Spielerzahl - 1)
	local list = ComposeWave(wave, budget, maxUnits)

	local groups = { {}, {}, {}, {} }
	for i, t in ipairs(list) do
		table.insert(groups[(i % 4) + 1], t)
	end

	local edges = { GoundAttackArrayTop, GoundAttackArrayRight, GoundAttackArraySouth, GoundAttackArrayLeft }
	local offsets = { CVec.New(0, 5), CVec.New(-5, 0), CVec.New(0, -5), CVec.New(5, 0) }
	local slot = ((idx - 1) % 4) + 1
	for p = 1, 4 do
		if #groups[p] > 0 and Foes[p] ~= nil then
			local entry = edges[p][slot]
			Reinforcements.Reinforce(Foes[p], groups[p], { entry.Location, entry.Location + offsets[p] }, 8,
				function(unit)
					table.insert(LiveFoes, unit)
					unit.AttackMove(CenterPos)
					IdleHunt(unit)
				end)
		end
	end

	Media.DisplayMessage("Wave " .. idx .. "/" .. #Waves .. " — " .. wave.name .. " (Tier " .. wave.tier .. ") inbound!", "")
	if idx < #Waves then
		Text = "Next: Wave " .. (idx + 1) .. "/" .. #Waves .. " — " .. Waves[idx + 1].name .. "."
		RemainingTime = DateTime.Seconds(WaveGapSeconds)
		timerStarted = true
		Trigger.AfterDelay(DateTime.Seconds(WaveGapSeconds), function() SendWave(idx + 1) end)
	else
		FinalWaveSent = true
		timerStarted = false
		UserInterface.SetMissionText("FINAL WAVE — destroy every attacker to win!", Player.GetPlayer("Neutral").Color)
	end
end

CheckVictory = function()
	if GameWon or not FinalWaveSent then
		return
	end
	for _, unit in ipairs(LiveFoes) do
		if not unit.IsDead then
			return
		end
	end
	GameWon = true
	UserInterface.SetMissionText("YOU SURVIVED ALL " .. #Waves .. " WAVES!", Player.GetPlayer("Neutral").Color)
	Media.DisplayMessage("The last attacker has fallen. You survived!", "")
	for _, Spieler in ipairs(ActivePlayer) do
		if SurviveObjectives[Spieler.InternalName] ~= nil then
			Spieler.MarkCompletedObjective(SurviveObjectives[Spieler.InternalName])
		end
	end
end

WorldLoaded = function()
	Trigger.AfterDelay(DateTime.Seconds(3), GetherData)
	Trigger.AfterDelay(DateTime.Seconds(10), function()
		Text = "First wave: " .. Waves[1].name .. ". Build your defenses!"
		RemainingTime = DateTime.Seconds(PrepSeconds - 10)
		timerStarted = true
	end)
	Trigger.AfterDelay(DateTime.Seconds(PrepSeconds), function() SendWave(1) end)
end

Tick = function()
	if timerStarted and RemainingTime > 0 then
		UserInterface.SetMissionText(Text .. " Time until attack: " .. Utils.FormatTime(RemainingTime), Player.GetPlayer("Neutral").Color)
		RemainingTime = RemainingTime - 1
	end
	if DateTime.GameTime % 100 == 0 then
		CheckVictory()
	end
end
