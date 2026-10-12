-- B1/B7 weapon validation gate (f5c98867d scratch: B1 exorcist-orb +
-- B7 Japan superweapon air-target fixes).
--
-- OWNERSHIP MODEL (v3): the exorcist is PLAYER-owned and rooted
-- (gate_rooted -> SpeedMultiplier 0), and every B1 victim belongs to
-- GateFoe, a non-playable player with no bot. Nothing in the scenario can
-- be ordered to move by AI or script drift; the orb ring's
-- AimTargetStances: Enemy resolves against the exorcist's owner (Player),
-- so GateFoe units are the aimable enemies in the B1 zone. All combat
-- traits are stripped map-locally on the probe types (rules.yaml), so the
-- fixtures cannot chase, return fire, or contaminate the geometry.
--
-- B1 orb chain, verified against engine sources:
--   PeriodicExplosion (RequiresCondition: spellcard && !disabled) detonates
--   ReimuDreamSeal AT THE EXORCIST every burst (Burst: 2, BurstDelays: 35,
--   ReloadDelay: 9999). Its FireRadius warhead launches 4 ReimuYinYangDrone
--   lasers to the four compass points at Range 5c0; each SpriteAthenaLaser
--   spirals in, then keeps detonating every 5 ticks (PierceTicks 64),
--   FireShrapnel aiming one Hakureiring3 at an enemy within Range 6666
--   (~6.5c) of each detonation point. Hakureiring3 (fixed head:
--   InvalidTargets = Epic,Psionic,Monster) is the B1 fix under test:
--   ordinary Ground targets in the envelope must be damaged; Epic /
--   Monster / Psionic controls inside the envelope must be untouched.
--   The gate grants `spellcard` via the map-local ExternalCondition shim
--   at t=300, after an Epic bait actor (b1_bosstarget) inside
--   Armament@SPECIAL range proves Hakureiring2 still fires (expected to
--   DIE by design). Tanks sit at 8-9c: inside the orb envelope, OUTSIDE
--   both armament ranges (6666 wu), and during spellcard the armaments are
--   additionally PauseOnCondition: spellcard - any tank damage is orb-only.
--
-- B7 chain: SupportPowerBotASModule fires MagicOrbHailstorm at the player
-- bait structures (MinimumAttractiveness 3000 met by the cluster value).
--   MagicOrbHailstormSpawner(s) rain AthenaProjectile orbs over a
--   MinRange 5000 .. Range 10000 (~4.9-9.8c) annulus; each orb's
--   FireFragment fires 8 MagicOrb bullets straight down to ground level.
--   Bullet detonations are AreaDamage (Spread 1200, Falloff 100/55/0 ->
--   max 2400wu) plus SpreadDamage ExtraDamage (1600, 100/75/50/25 -> max
--   4800wu). HitShape.DistanceFromEdge is 3D: an airborne victim at
--   altitude z is only inside the blast when a bullet lands within
--   sqrt(range^2 - z^2) horizontally. Cruise-altitude (~z=4000) fixed-wing
--   is therefore at or beyond reach; low-altitude (~z=2000) helicopters
--   are reachable only by near-direct hits. The gate asserts the Air
--   target class is accepted (>=1 airborne actor damaged) rather than
--   requiring every altitude state.
--
-- Fixtures self-heal (ChangesHealth@SelfHealing), so each receipt reports
-- hpmin - the lowest health observed since the baseline - in addition to
-- the two instantaneous samples.
--
-- Receipt format:
--   B17_RECEIPT <name> hp0=<baseline> hpmin=<lowest> hp1=<final> z=<alt>

subjects = {}

local HPMIN_UNSET = 1e15

local function watch(name, actor)
	subjects[name] = { actor = actor, hpmin = HPMIN_UNSET, polling = false }
end

local function hpz(actor)
	if actor ~= nil and not actor.IsDead then
		return actor.Health, actor.CenterPosition.Z
	end
	return 0, -1
end

local function snapshot(phase)
	for name, entry in pairs(subjects) do
		local hp, z = hpz(entry.actor)
		if phase == "hp0" and hp > 0 then
			entry.hpmin = hp
			entry.polling = true
		end
		entry[phase] = { hp = hp, z = z }
	end
end

local function poll_min()
	for _, entry in pairs(subjects) do
		if entry.polling then
			local hp = hpz(entry.actor)
			if hp < entry.hpmin then
				entry.hpmin = hp
			end
		end
	end
	Trigger.AfterDelay(10, poll_min)
end

local function print_pos(tag, actor)
	local pos = actor.CenterPosition
	print("AI_B1B7_POS " .. tag .. " tick=" .. DateTime.GameTime
		.. " pos=" .. pos.X .. "," .. pos.Y .. "," .. pos.Z)
end

WorldLoaded = function()
	local player = Player.GetPlayer("Player")
	local bot = Player.GetPlayer("HardBot")
	local foe = Player.GetPlayer("GateFoe")
	local objective = player.AddPrimaryObjective("Hold until the gate completes.")

	-- ==== B7 zone: bait cluster around (19,70), strike annulus ~4.9-9.8c ====
	Actor.Create("japan_japaneseshrine", true, { Owner = bot, Location = CPos.New(85, 12) })
	Actor.Create("japan_waveforcereactor", true, { Owner = bot, Location = CPos.New(88, 15) })
	Actor.Create("japan_waveforcereactor", true, { Owner = bot, Location = CPos.New(88, 18) })
	Actor.Create("japan_waveforcereactor", true, { Owner = bot, Location = CPos.New(88, 21) })

	-- Player bait: the SW must fire at this cluster (receipts prove it).
	-- MinimumAttractiveness is 3000 and the decision scores enemy
	-- Structure-typed actors by Valued.Cost within 8000wu, so the bait
	-- must be genuinely expensive: conyard 2500 + warfactory 2000 +
	-- refinery 500 = 5000. (The earlier techcenter/refinery mix summed
	-- to ~1000 at best - under the threshold, which is why the bot
	-- never fired.)
	watch("b7_bait_a", Actor.Create("ts_gdi_constructionyard", true, { Owner = player, Location = CPos.New(19, 69) }))
	watch("b7_bait_b", Actor.Create("ts_gdi_warfactory", true, { Owner = player, Location = CPos.New(22, 72) }))
	Actor.Create("ts_gdi_tiberiumrefinery", true, { Owner = player, Location = CPos.New(16, 72) })

	-- Aircraft sit inside the orb annulus, player-owned, unarmed by type
	-- (transport helis / gate_high_transport cruise-altitude variant).
	watch("b7_hover", Actor.Create("ra1_allies_chinooktransport", true, { Owner = player, Location = CPos.New(14, 66) }))
	watch("b7_landed", Actor.Create("ra1_allies_chinooktransport", true, { Owner = player, Location = CPos.New(24, 74) }))
	watch("b7_moving", Actor.Create("ra1_soviets_hiptransport", true, { Owner = player, Location = CPos.New(13, 64) }))
	watch("b7_high", Actor.Create("gate_high_transport", true, { Owner = player, Location = CPos.New(15, 61) }))
	watch("b7_ground", Actor.Create("td_gdi_mobileconstructionvehicle", true, { Owner = player, Location = CPos.New(12, 74) }))

	-- Friendly-fire probe: a zero-cost bot-owned building inside the
	-- annulus (the -10x ally-value consideration would suppress the strike
	-- against a normally-valued ally), far from every player unit. Any
	-- damage is the SW hitting its own side.
	watch("b7_friendly", Actor.Create("gate_ff_probe", true, { Owner = bot, Location = CPos.New(27, 65) }))

	-- Hover case: take off to a hover point and stay airborne.
	Trigger.AfterDelay(10, function()
		subjects["b7_hover"].actor.Move(CPos.New(15, 67))
	end)

	-- Landed case: take off, fly to the landing cell, then a real landing
	-- activity at t=140 (z returns to ~0 while the actor stays an
	-- Aircraft-typed victim inside the blast envelope).
	Trigger.AfterDelay(10, function()
		subjects["b7_landed"].actor.Move(CPos.New(25, 75))
	end)
	Trigger.AfterDelay(140, function()
		local landed = subjects["b7_landed"].actor
		if not landed.IsDead then
			landed.LandOnGround(CPos.New(25, 75))
		end
	end)

	-- Moving cases: continuous orders inside the annulus.
	local hindA, hindB = CPos.New(13, 64), CPos.New(24, 74)
	local hindFlip = false
	Trigger.OnIdle(subjects["b7_moving"].actor, function()
		hindFlip = not hindFlip
		subjects["b7_moving"].actor.Move(hindFlip and hindA or hindB)
	end)
	local yakA, yakB = CPos.New(15, 61), CPos.New(26, 75)
	local yakFlip = false
	Trigger.OnIdle(subjects["b7_high"].actor, function()
		yakFlip = not yakFlip
		subjects["b7_high"].actor.Move(yakFlip and yakA or yakB)
	end)

	-- ==== B1 zone: player-owned rooted exorcist around (55,35) ====
	local exorcist = Actor.Create("japan_exorcist", true, { Owner = player, Location = CPos.New(55, 35) })
	local rootedToken = exorcist.GrantCondition("gate_rooted", 100000)
	print("AI_B1B7_DIAG rooted token=" .. rootedToken
		.. " accepts_rooted=" .. tostring(exorcist.AcceptsCondition("gate_rooted"))
		.. " accepts_spellcard=" .. tostring(exorcist.AcceptsCondition("spellcard"))
		.. " stance=" .. tostring(exorcist.Stance))

	-- The exorcist itself: sanity watch + receipt context.
	watch("b1_exorcist", exorcist)

	-- Movement/idle probe: any position or idle-state change is printed.
	-- gate_rooted (SpeedMultiplier 0) + spellcard (SpeedMultiplier 0)
	-- mean locomotion must be impossible for the whole run.
	local lastPos, lastIdle = exorcist.CenterPosition, exorcist.IsIdle
	local function watch_move()
		local pos, idle = exorcist.CenterPosition, exorcist.IsIdle
		if pos ~= lastPos or idle ~= lastIdle then
			print("AI_B1B7_MOVE tick=" .. DateTime.GameTime
				.. " pos=" .. pos.X .. "," .. pos.Y .. "," .. pos.Z
				.. " idle=" .. tostring(idle))
			lastPos, lastIdle = pos, idle
		end
		Trigger.AfterDelay(5, watch_move)
	end
	Trigger.AfterDelay(5, watch_move)

	-- Anti-boss proof: GateFoe Epic bait inside Hakureiring2 range (6666 wu).
	-- It is EXPECTED to die to the 240k special snipe before the orb window.
	watch("b1_bosstarget", Actor.Create("ra1_allies_mobilegapgenerator", true, { Owner = foe, Location = CPos.New(59, 35) }))

	-- Orb-envelope probe: 7c east - OUTSIDE both armament ranges (6666 wu),
	-- ~2c past the east drone's orbit endpoint. If orb lasers spawn at all,
	-- their Hakureiring3 shrapnel (aim range 6666) cannot miss it.
	watch("b1_orbprobe", Actor.Create("td_gdi_mobileconstructionvehicle", true, { Owner = foe, Location = CPos.New(62, 35) }))

	-- Ordinary ground victims: 8-9c east, inside the orb envelope, outside
	-- both armament ranges (6666 wu). Unarmed MCVs: passive by type.
	watch("b1_tank_a", Actor.Create("td_gdi_mobileconstructionvehicle", true, { Owner = foe, Location = CPos.New(63, 34) }))
	watch("b1_tank_b", Actor.Create("td_gdi_mobileconstructionvehicle", true, { Owner = foe, Location = CPos.New(63, 36) }))
	watch("b1_tank_c", Actor.Create("td_gdi_mobileconstructionvehicle", true, { Owner = foe, Location = CPos.New(64, 35) }))

	-- Exclusion controls inside the same orb envelope but >4c from any
	-- tank (missile splash is ~128wu, so proximity splash cannot leak):
	-- passive typed dummies from rules.yaml; must stay at full health.
	watch("b1_epic", Actor.Create("ra1_allies_mobilegapgenerator", true, { Owner = foe, Location = CPos.New(62, 40) }))
	watch("b1_monster", Actor.Create("gate_monster_dummy", true, { Owner = foe, Location = CPos.New(62, 30) }))
	watch("b1_psionic", Actor.Create("gate_psionic_dummy", true, { Owner = foe, Location = CPos.New(61, 40) }))

	print("AI_B1B7_GATE_STARTED bot=HardBot type=hard")

	Trigger.AfterDelay(8, function()
		snapshot("hp0")
		poll_min()
		print_pos("exorcist_baseline", exorcist)
		print("AI_B1B7_GATE_BASELINE tick=" .. DateTime.GameTime)
	end)

	-- Arm the orb ring: spellcard for ~800 ticks so both dreamseal bursts
	-- (t=0 and t=35 after enable) and every orb detonation land inside the
	-- receipt window. Armaments pause under spellcard, so orb missiles are
	-- the only damage source during the window.
	Trigger.AfterDelay(300, function()
		local token = exorcist.GrantCondition("spellcard", 800)
		print_pos("exorcist_spellcard", exorcist)
		print("AI_B1B7_GATE_SPELLCARD tick=" .. DateTime.GameTime
			.. " token=" .. token
			.. " accepts=" .. tostring(exorcist.AcceptsCondition("spellcard")))
	end)

	-- Armament-pause diagnostic: spawn a second Epic bait INSIDE the
	-- spellcard window (created at t=305, inside Hakureiring2 range). If
	-- `spellcard` really took hold the armaments are paused and it must
	-- survive to t=950; if it dies, the grant did not enable
	-- PeriodicExplosion and the missing orb ring is explained upstream.
	Trigger.AfterDelay(305, function()
		watch("b1_bait2", Actor.Create("ra1_allies_mobilegapgenerator", true, { Owner = foe, Location = CPos.New(59, 33) }))
	end)
	Trigger.AfterDelay(950, function()
		local entry = subjects["b1_bait2"]
		local hp = (entry ~= nil and entry.actor ~= nil and not entry.actor.IsDead) and entry.actor.Health or 0
		print("AI_B1B7_DIAG bait2_alive_at_950 hp=" .. hp)
	end)

	Trigger.AfterDelay(1400, function()
		snapshot("hp1")
		print_pos("exorcist_final", exorcist)
		for name, entry in pairs(subjects) do
			local before = entry.hp0 or { hp = -1, z = -1 }
			local after = entry.hp1 or { hp = -1, z = -1 }
			local hpmin = entry.hpmin
			if hpmin == HPMIN_UNSET then
				hpmin = -1
			end
			print("B17_RECEIPT " .. name
				.. " hp0=" .. before.hp
				.. " hpmin=" .. hpmin
				.. " hp1=" .. after.hp
				.. " z=" .. after.z)
		end
		print("AI_B1B7_GATE_COMPLETED tick=" .. DateTime.GameTime)
		player.MarkFailedObjective(objective)
	end)
end
