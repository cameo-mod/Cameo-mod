-- Garrison passenger-fire bisect fixture (task 01a13167).
-- c1 : civilian Garrisonable-route host, Defend occupants, autonomous fire expected.
-- c2 : same host, ordinary Attack on a Structure-typed target that Defend
--      priorities can never select; only the requested path may fire.
-- c2f: same host, ForceAttack on a Structure-typed target (force variant arm).
-- c3 : naxibunker Cargo-route host, Defend occupants; autonomous fire expected,
--      then an ordinary Attack at tick 120 probes the dual-AttackBase order race.
-- ctl: civilian host with HoldFire occupants; silence is the pass condition.
-- Deterministic: fixed cells, stance-pinned actors, fixed ticks, no wall clock
-- or unsynced randomness. Terrain force-attack has no Lua binding; see README.

local cells = {}

local function SpawnCell(hostType, targetType, owner, enemy, hostLoc, targetLoc)
	local host = Actor.Create(hostType, true, { Owner = owner, Location = hostLoc })
	local target = Actor.Create(targetType, true, { Owner = enemy, Location = targetLoc })
	local cell = { host = host, target = target, hits = 0, damage = 0 }
	Trigger.OnDamaged(target, function(self, attacker, dmg)
		cell.hits = cell.hits + 1
		cell.damage = cell.damage + dmg
	end)
	return cell
end

WorldLoaded = function()
	Camera.Position = Map.CenterOfCell(CPos.New(27, 44))
	local a = Player.GetPlayer("SideA")
	local b = Player.GetPlayer("SideB")
	cells.c1 = SpawnCell("fixture_civ_defend", "fixture_target", a, b, CPos.New(20, 22), CPos.New(25, 23))
	cells.c2 = SpawnCell("fixture_civ_defend", "fixture_target_structure", a, b, CPos.New(20, 36), CPos.New(25, 37))
	cells.c2f = SpawnCell("fixture_civ_defend", "fixture_target_structure", a, b, CPos.New(34, 36), CPos.New(39, 37))
	cells.c3 = SpawnCell("fixture_bunker_defend", "fixture_target", a, b, CPos.New(20, 50), CPos.New(25, 51))
	cells.ctl = SpawnCell("fixture_civ_holdfire", "fixture_target", a, b, CPos.New(20, 64), CPos.New(25, 65))
	print("GARRISON_BISECT_STARTED cells=c1,c2,c2f,c3,ctl order_ticks=60,120 end_tick=700")
	Trigger.AfterDelay(60, function()
		cells.c2.host.Attack(cells.c2.target, false, false)
		print("GARRISON_BISECT_ORDER cell=c2 via=lua_attack force=false")
		cells.c2f.host.Attack(cells.c2f.target, false, true)
		print("GARRISON_BISECT_ORDER cell=c2f via=lua_attack force=true")
	end)
	Trigger.AfterDelay(120, function()
		cells.c3.host.Attack(cells.c3.target, false, false)
		print("GARRISON_BISECT_ORDER cell=c3 via=lua_attack force=false")
	end)
	Trigger.AfterDelay(700, function()
		for _, name in ipairs({ "c1", "c2", "c2f", "c3", "ctl" }) do
			local cell = cells[name]
			local lost = cell.target.MaxHealth - cell.target.Health
			print("GARRISON_BISECT_RESULT cell=" .. name .. " host=" .. cell.host.Type ..
				" target=" .. cell.target.Type .. " hits=" .. cell.hits .. " hp_lost=" .. lost)
		end
		print("GARRISON_BISECT_COMPLETED")
		Media.DisplayMessage("Garrison bisect complete; diagnostics are in debug.log and lua.log.", "Bisect")
	end)
end
