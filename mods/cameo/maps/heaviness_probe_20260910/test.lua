-- One shot per lane; no upgrades, AI, movement or damage modifiers.
-- Shield lanes test the armor coefficient, not a regenerating shield pool.
local lanes = { "legacy_light", "legacy_medium", "legacy_heavy", "h0", "h1000", "h2000", "shield_h0", "shield_h1000", "shield_h2000" }
local weapons = { "legacy_light", "legacy_medium", "legacy_heavy", "h0", "h1000", "h2000", "h0", "h1000", "h2000" }
local expected = { 3860, 4560, 4660, 2720, 3960, 5520, 2880, 6480, 11520 }
local targets = {}
local shooters = {}
local hits = {}
WorldLoaded = function()
	Camera.Position = Map.CenterOfCell(CPos.New(24, 42))
	local a = Player.GetPlayer("SideA")
	local b = Player.GetPlayer("SideB")
	for i, label in ipairs(lanes) do
		local shield = i > 6
		local y = 12 + (shield and i - 6 or i) * 11
		local x = shield and 52 or 24
		local target = Actor.Create(shield and "probe_target_shield" or "probe_target", true, { Owner = b, Location = CPos.New(x, y) })
		local shooter = Actor.Create("probe_" .. weapons[i], true, { Owner = a, Location = CPos.New(x - 3, y) })
		targets[i] = target
		shooters[i] = shooter
		hits[i] = 0
		Trigger.OnDamaged(target, function(self, attacker, damage)
			hits[i] = hits[i] + 1
			print("HEAVINESS_HIT lane=" .. label .. " damage=" .. damage)
		end)
	end
	print("HEAVINESS_PROBE_STARTED lanes=9 armor=Medium_and_Shield hp=1000000 mode=SharedVersus")
	Trigger.AfterDelay(25, function()
		for i, shooter in ipairs(shooters) do
			shooter.Attack(targets[i], false, true)
		end
	end)
	Trigger.AfterDelay(100, function()
		local passed = true
		local exact = true
		for i, target in ipairs(targets) do
			local lost = target.MaxHealth - target.Health
			print("HEAVINESS_RESULT lane=" .. lanes[i] .. " lost=" .. lost .. " expected=" .. expected[i] .. " events=" .. hits[i])
			if lost <= 0 or hits[i] == 0 then passed = false end
			if lost ~= expected[i] then exact = false end
			shooters[i].Destroy()
		end
		print("HEAVINESS_PROBE_COMPLETED all_lanes_hit=" .. tostring(passed) .. " all_expected=" .. tostring(exact))
		Media.DisplayMessage("Heaviness probe complete; measurements are in lua.log.", "Probe")
	end)
end
