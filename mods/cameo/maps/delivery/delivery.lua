--zerg invasion force
zergForce = {"zerg_zergling","zerg_hydralisk","zerg_zergling","zerg_ultralisk","zerg_zergling","zerg_hydralisk","zerg_zergling","zerg_zergling","zerg_zergling","zerg_ultralisk","zerg_zergling","zerg_hydralisk","zerg_zergling","zerg_zergling","zerg_hydralisk","zerg_zergling","zerg_zergling","zerg_ultralisk","zerg_hydralisk","zerg_zergling","zerg_zergling","zerg_zergling","zerg_zergling","zerg_hydralisk","zerg_zergling","zerg_ultralisk","zerg_zergling","zerg_hydralisk","zerg_zergling","zerg_zergling","zerg_zergling","zerg_ultralisk","zerg_zergling","zerg_hydralisk","zerg_zergling","zerg_zergling","zerg_hydralisk","zerg_zergling","zerg_zergling","zerg_ultralisk","zerg_hydralisk","zerg_zergling","zerg_zergling","zerg_zergling"}

	SendZerg = function()
		Reinforcements.Reinforce(Zerg1, zergForce, { ZergEnter.Location, ZergPoint.Location}, 15, function(zergy)
			IdleHunt(zergy)
		end)
	end
	
	SendZerg2 = function()
		Reinforcements.Reinforce(Zerg1, zergForce, { ZergEnter2.Location, ZergPoint2.Location}, 15, function(zergy)
			IdleHunt(zergy)
		end)
	end

WorldLoaded = function()
	GDI = Player.GetPlayer("Player")
	Truck = Player.GetPlayer("Truck")
	Nod1 = Player.GetPlayer("EnemyIdle")
	Nod2 = Player.GetPlayer("EnemyLasers")
	Nod3 = Player.GetPlayer("EnemyActive")
	Zerg1 = Player.GetPlayer("Enemy2Idle")
	Zerg2 = Player.GetPlayer("Enemy2Active")
	Creeps = Player.GetPlayer("Creeps")

ActivateNod = function ()
	local NodActors = Nod1.GetActors()
	for i=1, #NodActors do
		NodActors[i].Owner = Nod3
	end
end

ActivateZerg = function ()
	local ZergActors = Zerg1.GetActors()
	for i=1, #ZergActors do
		ZergActors[i].Owner = Zerg2
	end
end

--objectives: kill nod tech center
	InitObjectives(GDI)
	
	KillBuildingObjective = GDI.AddPrimaryObjective("Destroy Nod Tech Center")
	
	
	--Timer starts here, change owner of Nod when over
	DateTime.TimeLimit = DateTime.Minutes(13)
	
	Trigger.OnTimerExpired(function ()
		Media.DisplayMessage("Nod Comms are back on!")
		ActivateNod()
	end)
--proximity trigger for MCV owner
	Trigger.OnEnteredProximityTrigger(ConTruck.CenterPosition, WDist.New(3*1024), function(a, id)
		if a.Owner == GDI then
			ConTruck.Owner = GDI
			Trigger.RemoveProximityTrigger(id)
		end
	end)
--proximity trigger for commando team
		Trigger.OnEnteredProximityTrigger(TeamTrigger.CenterPosition, WDist.New(3*1024), function(a, id)
		if a.Owner == GDI then
			Reinforcements.ReinforceWithTransport(GDI, "tran.gdi",{"rmbo.gdi", "rmbo.gdi","rmbo.gdi","rmbo.gdi","rmbo.gdi","rmbo.gdi","rmbo.gdi"}, {TeamEnter.Location, TeamTrigger.Location}, {TeamExit.Location})
			Trigger.RemoveProximityTrigger(id)
		end
	end)
--OnDeath Trigger for Tech building
	Trigger.OnKilled(TechCenter, function(self, killer)
		GDI.MarkCompletedObjective(KillBuildingObjective)
	end)

--OnDeath Trigger for Zerg
	Trigger.OnKilled(ZergFinal, function(self, killer)
		GDI.MarkCompletedObjective(KillZergObjective)
	end)
	
--multiple prox triggers for zerg invasion
--make sure to kill all prox triggers when doing this and to change the zerg AI, also add objective for killing Zerg
--GOD DAMN FUCKING PIECE OF SHIT TRIGGERS STILL TRIGGER WHEN THE FUCKING TRIGGER GOT DELETED FUCK FUCK FUCK MOTHERFUCKER FUCK FUCK FUCK
	Trigger.OnEnteredProximityTrigger(TriggWake1.CenterPosition, WDist.New(7*1024), function(a, id)
		if a.Owner == GDI then
			Kain.Move(Able.Location)
			Trigger.RemoveProximityTrigger(id)
		end
	end)
	
	Trigger.OnEnteredProximityTrigger(TriggWake2.CenterPosition, WDist.New(7*1024), function(a, id)
		if a.Owner == GDI then
			Kain.Move(Able.Location)
			Trigger.RemoveProximityTrigger(id)
		end
	end)

--Some rogue-goldberg hacky bullshit I have to do because triggers won't piss off when the actor they're attached to is destroyed and I can't get the Trigger.Clear Function to work
	Trigger.OnEnteredProximityTrigger(Able.CenterPosition, WDist.New(3*1024), function(a, id)
		if a.Owner == Creeps then
			Media.DisplayMessage("It seems this Nod base was defending against the Zerg")
			SendZerg()
			SendZerg2()
			CamZerg.Owner = GDI
			Beacon.New(GDI,CamZerg.CenterPosition)
			Trigger.AfterDelay(75, function()
				Media.DisplayMessage("Destroy this Infestation before it grows out of control!")
			end)
			KillZergObjective = GDI.AddPrimaryObjective("Destroy Zerg Infestation")
			Trigger.AfterDelay(900, function()
				CamZerg.Owner = Creeps
				ActivateZerg()
			end)
			Trigger.RemoveProximityTrigger(id)
		end
	end)
	
	Camera.Position = CamStart.CenterPosition

end

Tick = function()
--if player runs out of units they lose
	if GDI.HasNoRequiredUnits() then
		GDI.MarkFailedObjective(KillBuildingObjective)
	end
end