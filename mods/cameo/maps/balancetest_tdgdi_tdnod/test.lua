-- Balance test harness: TD GDI vs TD Nod (design 2026-07-16, expanded 2026-07-27)
-- Eight rounds covering infantry, light vehicles, battle tanks, heavy armor,
-- air-to-air, artillery, promotion units, and a mixed combined-arms battle.
-- Both sides spawn ~10,000 credits of units per round, fight to the death,
-- and the harness announces the winner with surviving unit count and credit
-- value. All units are destroyed between rounds. The human player is a
-- spectator. See docs/MASTER_REPORT.md §8.2 (GDI-100 benchmark).

Costs = {
	td_gdi_minigunner = 100, td_gdi_grenadier = 200, td_gdi_rocketsoldier = 200,
	td_gdi_humvee = 400, td_gdi_battletank = 900, td_gdi_mammothtank = 1600,
	td_gdi_mlrs = 1000, td_gdi_archerartillery = 750,
	td_gdi_orca = 1700, td_gdi_firehawk = 2000,
	td_gdi_humveemkii = 600, td_gdi_predatortank = 1250, td_gdi_mammothtankmkiii = 3000,
	td_nod_minigunner = 100, td_nod_rocketsoldier = 200, td_nod_flamethrower = 200,
	td_nod_buggy = 300, td_nod_reconbike = 500, td_nod_lighttank = 600,
	td_nod_lighttankmkii = 800, td_nod_artillery = 400, td_nod_stealthtank = 900,
	td_nod_flametankmkii = 1300, td_nod_ssmlauncher = 800,
	td_nod_specterartillery = 900, td_nod_flametank = 800,
	td_nod_apacheattackhelicopter = 1600, td_nod_venom = 900,
	td_nod_buggymkii = 500,
}

-- every army sums to ~10,000 credits (verified against live costs)
Rounds = {
	{ name = "Tier 1 — Infantry",
	  a = { {"td_gdi_minigunner",20}, {"td_gdi_grenadier",20}, {"td_gdi_rocketsoldier",20} },
	  b = { {"td_nod_minigunner",20}, {"td_nod_rocketsoldier",20}, {"td_nod_flamethrower",20} } },
	{ name = "Tier 1 — Light vehicles",
	  a = { {"td_gdi_humvee",25} },
	  b = { {"td_nod_buggy",20}, {"td_nod_reconbike",8} } },
	{ name = "Tier 2 — Battle tanks",
	  a = { {"td_gdi_battletank",10}, {"td_gdi_humvee",2}, {"td_gdi_rocketsoldier",1} },
	  b = { {"td_nod_lighttank",12}, {"td_nod_lighttankmkii",3}, {"td_nod_artillery",1} } },
	{ name = "Tier 3 — Heavy armor",
	  a = { {"td_gdi_mammothtank",5}, {"td_gdi_battletank",2}, {"td_gdi_rocketsoldier",1} },
	  b = { {"td_nod_stealthtank",6}, {"td_nod_flametankmkii",2}, {"td_nod_ssmlauncher",2}, {"td_nod_artillery",1} } },
	{ name = "Air-to-Air",
	  a = { {"td_gdi_orca",5}, {"td_gdi_firehawk",1} },
	  b = { {"td_nod_apacheattackhelicopter",5}, {"td_nod_venom",2} } },
	{ name = "Artillery Duel",
	  a = { {"td_gdi_archerartillery",7}, {"td_gdi_mlrs",5} },
	  b = { {"td_nod_artillery",10}, {"td_nod_ssmlauncher",5}, {"td_nod_specterartillery",2} } },
	{ name = "Promotion Units",
	  a = { {"td_gdi_humveemkii",3}, {"td_gdi_predatortank",4}, {"td_gdi_mammothtankmkiii",1} },
	  b = { {"td_nod_buggymkii",4}, {"td_nod_lighttankmkii",5}, {"td_nod_flametankmkii",3} } },
	{ name = "Mixed Army",
	  a = { {"td_gdi_minigunner",3}, {"td_gdi_grenadier",2}, {"td_gdi_rocketsoldier",2}, {"td_gdi_humvee",3}, {"td_gdi_battletank",2}, {"td_gdi_mammothtank",2}, {"td_gdi_mlrs",1}, {"td_gdi_orca",1} },
	  b = { {"td_nod_minigunner",3}, {"td_nod_rocketsoldier",2}, {"td_nod_flamethrower",2}, {"td_nod_buggy",3}, {"td_nod_lighttank",2}, {"td_nod_stealthtank",2}, {"td_nod_ssmlauncher",1}, {"td_nod_apacheattackhelicopter",1}, {"td_nod_flametankmkii",2} } },
}

SpawnA = CPos.New(26, 38)   -- GDI block, faces east (moved left to avoid cliff)
SpawnB = CPos.New(66, 38)   -- Nod block, faces west (moved right to avoid cliff)
GridRows = 10
GridSpacing = 2              -- spread units to avoid terrain features

CurrentRound = 0
RoundActive = false
RoundTimeout = 0
LiveA = {}
LiveB = {}
Results = {}

SpawnArmy = function(owner, comp, origin, facingTarget)
	local live = {}
	local i = 0
	for _, entry in ipairs(comp) do
		for n = 1, entry[2] do
			local row = i % GridRows
			local col = math.floor(i / GridRows)
			local pos = CPos.New(origin.X + col * GridSpacing, origin.Y + row * GridSpacing)
			local unit = Actor.Create(entry[1], true, { Owner = owner, Location = pos })
			table.insert(live, { unit = unit, cost = Costs[entry[1]] or 0 })
			i = i + 1
		end
	end
	Trigger.AfterDelay(25, function()
		for _, e in ipairs(live) do
			if not e.unit.IsDead then
				e.unit.AttackMove(facingTarget)
				Trigger.OnIdle(e.unit, e.unit.Hunt)
			end
		end
	end)
	return live
end

CountAlive = function(live)
	local n, value = 0, 0
	for _, e in ipairs(live) do
		if not e.unit.IsDead then
			n = n + 1
			value = value + e.cost
		end
	end
	return n, value
end

DestroySurvivors = function(live)
	for _, e in ipairs(live) do
		if not e.unit.IsDead then
			e.unit.Destroy()
		end
	end
end

StartRound = function(idx)
	CurrentRound = idx
	local r = Rounds[idx]
	Media.DisplayMessage("Round " .. idx .. "/" .. #Rounds .. ": " .. r.name .. " — 10,000 vs 10,000 credits. FIGHT!", "Harness")
	LiveA = SpawnArmy(Player.GetPlayer("SideA"), r.a, SpawnA, SpawnB)
	LiveB = SpawnArmy(Player.GetPlayer("SideB"), r.b, SpawnB, SpawnA)
	RoundTimeout = DateTime.Minutes(4)
	RoundActive = true
end

FinishRound = function(verdict)
	RoundActive = false
	table.insert(Results, "Round " .. CurrentRound .. " (" .. Rounds[CurrentRound].name .. "): " .. verdict)
	Media.DisplayMessage(verdict, "Harness")
	-- destroy all surviving units immediately so nothing persists into next round
	DestroySurvivors(LiveA)
	DestroySurvivors(LiveB)
	LiveA = {}
	LiveB = {}
	if CurrentRound < #Rounds then
		Trigger.AfterDelay(DateTime.Seconds(10), function() StartRound(CurrentRound + 1) end)
	else
		Trigger.AfterDelay(DateTime.Seconds(5), function()
			Media.DisplayMessage("=== BALANCE TEST COMPLETE ===", "Harness")
			for _, line in ipairs(Results) do
				Media.DisplayMessage(line, "Result")
			end
			UserInterface.SetMissionText("Balance test complete — results in the chat log.", Player.GetPlayer("Neutral").Color)
		end)
	end
end

CheckRound = function()
	if not RoundActive then
		return
	end
	local na, va = CountAlive(LiveA)
	local nb, vb = CountAlive(LiveB)
	RoundTimeout = RoundTimeout - 25
	if na == 0 and nb == 0 then
		FinishRound("Mutual destruction — dead even.")
	elseif nb == 0 then
		FinishRound("GDI wins with " .. na .. " units / " .. va .. " credits remaining (" .. math.floor(va / 100) .. "% of army value).")
	elseif na == 0 then
		FinishRound("Nod wins with " .. nb .. " units / " .. vb .. " credits remaining (" .. math.floor(vb / 100) .. "% of army value).")
	elseif RoundTimeout <= 0 then
		FinishRound("Timeout — GDI " .. va .. " vs Nod " .. vb .. " credits remaining (higher value wins on points).")
	end
end

WorldLoaded = function()
	Camera.Position = Map.CenterOfCell(CPos.New(46, 47))
	Media.DisplayMessage("Balance test harness: TD GDI vs TD Nod. Eight rounds, ~10k-credit armies each.", "Harness")
	Media.DisplayMessage("Rounds: Infantry, Light vehicles, Battle tanks, Heavy armor, Air, Artillery, Promotion units, Mixed army.", "Harness")
	UserInterface.SetMissionText("Balance test running — watch the battles.", Player.GetPlayer("Neutral").Color)
	Trigger.AfterDelay(DateTime.Seconds(5), function() StartRound(1) end)
end

Tick = function()
	if DateTime.GameTime % 25 == 0 then
		CheckRound()
	end
end
