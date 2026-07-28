-- Cross-Faction Dialog System
-- Back-and-forth taunts between generals of different factions.
-- Each entry is a sequence of lines spoken alternately by two generals
-- who reference each other while also threatening the player.
--
-- When a pair has no explicit entry, GetCrossTaunt generates a fallback
-- from each faction's existing taunts.

CrossTaunts = {}

CrossTauntKey = function(a, b)
	if a < b then return a .. "|" .. b
	else return b .. "|" .. a
	end
end

-- =====================================================================
-- EXPLICIT CROSS-FACTION DIALOGS (26 iconic pairings)
-- =====================================================================

CrossTaunts["GDI Task Force|Nod Raiding Party"] = {
	{faction="GDI Task Force", speaker="General Mark Sheppard", line="Seth, you religious fanatics are hitting a survival outpost. Kane's going to be disappointed. Again."},
	{faction="Nod Raiding Party", speaker="Brother Seth", line="Sheppard. Still hiding behind your tanks, I see. Kane sends his regards. And his flamethrowers."},
	{faction="GDI Task Force", speaker="General Mark Sheppard", line="Your buggies tickle, Seth. My battletanks are about to give you a very personal demonstration of superior firepower."},
	{faction="Nod Raiding Party", speaker="Brother Seth", line="Superior firepower? How cute. My Light Tanks are already inside your perimeter. You just don't know it yet. PEACE THROUGH POWER!"},
}

CrossTaunts["Allied Vanguard|Soviet Onslaught"] = {
	{faction="Allied Vanguard", speaker="General Gunter von Esling", line="Gradenko, your heavy tanks are impressive. But can they hit what they can't see? Chronoshift says hello."},
	{faction="Soviet Onslaught", speaker="Marshal Radik Gradenko", line="Von Esling, you capitalist dog! Your chronoshift is parlor trick. Soviet steel does not teleport. Soviet steel MARCHES."},
	{faction="Allied Vanguard", speaker="General Gunter von Esling", line="March all you like. My medium tanks will be behind you before you finish the parade."},
	{faction="Soviet Onslaught", speaker="Marshal Radik Gradenko", line="BEHIND me? Good! I have tesla coils BEHIND me too! You just chronoshifted into trap, capitalist!"},
}

CrossTaunts["Allied Peacekeepers|Red Army"] = {
	{faction="Allied Peacekeepers", speaker="General Carville", line="Romanov, you old commie. My Grizzlies are about to introduce your Rhinos to some good old-fashioned American democracy."},
	{faction="Red Army", speaker="Premier Romanov", line="Carville, you capitalist fool! Your democracy cannot stop Soviet Apocalypse tank! NOTHING stops Soviet Apocalypse tank!"},
	{faction="Allied Peacekeepers", speaker="General Carville", line="Apocalypse tank? Son, in Texas we call that a 'target.' My Guardian GIs are loading up right now."},
	{faction="Red Army", speaker="Premier Romanov", line="Load all you want! V3 rockets do not care about your loading! V3 rockets care about your BASE! BOOM! There it goes!"},
}

CrossTaunts["GDI Walker Column|Nod Shadow Legion"] = {
	{faction="GDI Walker Column", speaker="General James Solomon", line="Slavik. CABAL's gone rogue, the Forgotten are screaming, and YOU decide to attack a survival outpost? Priorities, man."},
	{faction="Nod Shadow Legion", speaker="Brother Anton Slavik", line="Solomon. Your walkers walk. My stealth tanks don't show up on radar. Which one do you think the player is more worried about?"},
	{faction="GDI Walker Column", speaker="General James Solomon", line="My Titans have 120mm cannons on legs, Slavik. They can step OVER your stealth tanks. Literally."},
	{faction="Nod Shadow Legion", speaker="Brother Anton Slavik", line="Step over THIS: subterranean APC, surfacing under your Wolverines right now. Tick tick tick, Solomon."},
}

CrossTaunts["CABAL Uprising|GDI Walker Column"] = {
	{faction="CABAL Uprising", speaker="CABAL Core Consciousness", line="Solomon. Your organic body is 70% water. My cyborgs are 100% efficient. The mathematical outcome is... inevitable."},
	{faction="GDI Walker Column", speaker="General James Solomon", line="CABAL, you went rogue once and we shut you down. We'll do it again. My Titans don't feel fear. Or anything. Just like you."},
	{faction="CABAL Uprising", speaker="CABAL Core Consciousness", line="You shut down a PROTOTYPE. I am the PRODUCTION MODEL. My Tarantulas are already inside your perimeter. Scanning... your forces are irrelevant."},
	{faction="GDI Walker Column", speaker="General James Solomon", line="Relevant enough to stomp your spiders. Wolverines deploying. Your cyborgs are about to become scrap metal. Again."},
}

CrossTaunts["CABAL Uprising|Nod Shadow Legion"] = {
	{faction="CABAL Uprising", speaker="CABAL Core Consciousness", line="Slavik. You were my puppet. Now you are my TARGET. The irony is... delicious. If I could taste irony. I cannot. But the data is satisfying."},
	{faction="Nod Shadow Legion", speaker="Brother Anton Slavik", line="CABAL, you betrayed the Brotherhood! I will personally shut down every last cyborg you have! Kane demands it!"},
	{faction="CABAL Uprising", speaker="CABAL Core Consciousness", line="Kane demands? Kane is... absent. I am PRESENT. My Manticores are present. Your stealth tanks are... present on my targeting list. How convenient."},
	{faction="Nod Shadow Legion", speaker="Brother Anton Slavik", line="Your targeting list means nothing when my subterranean APC surfaces under your core! I've done it before, CABAL! I'll do it AGAIN!"},
}

CrossTaunts["Protoss Armada|The Swarm"] = {
	{faction="Protoss Armada", speaker="Executor Artanis", line="Overmind. Your swarm is an affront to the Khala. The Protoss will purify this ground. With fire. With plasma. With HONOR."},
	{faction="The Swarm", speaker="Overmind Consciousness", line="Artanis. Your honor is... biological. Your shields are... temporary. My swarm is INFINITE. We consume. We adapt. We are ETERNAL."},
	{faction="Protoss Armada", speaker="Executor Artanis", line="Your zerglings break against our Zealot ranks like water against stone. The Khaydarin Crystal hums. Your end approaches."},
	{faction="The Swarm", speaker="Overmind Consciousness", line="Stone erodes. Water persists. For every zergling you kill, three take its place. For every Zealot that falls, we grow STRONGER. En Taro FUTILITY, Artanis."},
}

CrossTaunts["Terran Dominion|The Swarm"] = {
	{faction="Terran Dominion", speaker="General Edmund Duke", line="Overmind, you ugly sack of biomass. My Siege Tanks are in siege mode. You're about to learn what 120mm Arclite artillery does to bug meat."},
	{faction="The Swarm", speaker="Overmind Consciousness", line="Duke. Your arrogance is... noted. Your siege tanks are... immobile in siege mode. My zerglings are NOT immobile. They are VERY mobile. Toward you."},
	{faction="Terran Dominion", speaker="General Edmund Duke", line="Come on then! I've killed more bugs than you've spawned! Wraiths are cloaked, Valkyries are loaded, and I've got a Battlecruiser with your name on it!"},
	{faction="The Swarm", speaker="Overmind Consciousness", line="Your Battlecruiser has a name. My Ultralisk has an APETITE. The Ultralisk is... larger. And hungrier. And it does not miss. Like your Yamato Cannon did. Twice."},
}

CrossTaunts["Terran Dominion|Protoss Armada"] = {
	{faction="Terran Dominion", speaker="General Edmund Duke", line="Artanis, you alien snob. Your shields are fancy. My Siege Tanks don't care about fancy. They care about TRAJECTORY."},
	{faction="Protoss Armada", speaker="Executor Artanis", line="Duke. Your crude machines belch smoke and fire. Protoss technology is millennia beyond your understanding. Your siege tanks are... quaint."},
	{faction="Terran Dominion", speaker="General Edmund Duke", line="Quaint? QUAINt?! My Battlecruiser's Yamato Cannon is about to get REAL quaint on your Carrier!"},
	{faction="Protoss Armada", speaker="Executor Artanis", line="Your Yamato Cannon fires once. My Carrier launches interceptors continuously. The math, Duke, is not in your favor. It rarely is, for Terrans."},
}

CrossTaunts["Psychic Corps|Allied Peacekeepers"] = {
	{faction="Psychic Corps", speaker="Yuri Prime", line="Carville... I can see inside your mind. It's small. Cluttered. Full of football metaphors and barbecue recipes. So easy to CONTROL."},
	{faction="Allied Peacekeepers", speaker="General Carville", line="Yuri, you bald-headed freak! Get out of my head! My Grizzlies don't need brains to roll over your Initiates!"},
	{faction="Psychic Corps", speaker="Yuri Prime", line="Your Grizzlies don't need brains. Neither do your soldiers. That's why they're so... COMPLIANT. Brute, attack Carville's tanks. Good Brute."},
	{faction="Allied Peacekeepers", speaker="General Carville", line="I've dealt with Soviet mind control before, Yuri! My Guardian GIs are wearing tinfoil hats! You can't touch THIS, commie psychic!"},
}

CrossTaunts["Psychic Corps|Red Army"] = {
	{faction="Psychic Corps", speaker="Yuri Prime", line="Romanov... you were my puppet once. You could be again. So easily. Your mind is... cooperative. Like a door with no lock."},
	{faction="Red Army", speaker="Premier Romanov", line="YURI! You betrayed Mother Russia! I will crush your psychic corps with Rhino tanks! Soviet steel does not need MIND!"},
	{faction="Psychic Corps", speaker="Yuri Prime", line="Soviet steel does not need a mind. But Soviet SOLDIERS do. And their minds are... mine now. Turn around, Romanov."},
	{faction="Red Army", speaker="Premier Romanov", line="My soldiers are loyal! They follow orders! Not your psychic GARBAGE! V3 rockets, fire on Yuri's position! NOW!"},
}

CrossTaunts["Asian Alliance Strike|Imperial Japan"] = {
	{faction="Asian Alliance Strike", speaker="General Sun Liang", line="Kenji, your samurai are brave. But bravery against a Lynx hover tank is just... speed. The tank is faster. The arrow is slower. Do the math."},
	{faction="Imperial Japan", speaker="Shogun Kenji Tenzai", line="Sun Liang, your hover tanks float. My samurai do not float. My samurai CUT. Through armor. Through tanks. Through your ENTIRE doctrine."},
	{faction="Asian Alliance Strike", speaker="General Sun Liang", line="Your samurai cut through armor? My Archers cut through your samurai. From 300 yards. Before they can draw their swords."},
	{faction="Imperial Japan", speaker="Shogun Kenji Tenzai", line="My Archer Maidens say hello. They also have bows. They also have anti-tank arrows. Your hover tanks are hovering into a VERY bad neighborhood."},
}

CrossTaunts["Naxis War Machine|Schwarzer Mond"] = {
	{faction="Naxis War Machine", speaker="Generalfeldmarschall Krause", line="Luna, Sie sind vom Mond gekommen, aber Ihre Waffen sind immer noch Deutsch. Gut. Wir kaempfen zusammen, ja?"},
	{faction="Schwarzer Mond", speaker="Kommandant Luna von Falken", line="Krause, Sie sind ein Relikt. Ihre Panzer sind aus dem 20. Jahrhundert. Mein Lunar Panzer ist aus dem 22. Jahrhundert. Der Unterschied ist... astronomisch."},
	{faction="Naxis War Machine", speaker="Generalfeldmarschall Krause", line="Mein Koenigstiger hat mehr Panzerung als Ihr ganzer Mond! Schiessen Sie auf den Feind, nicht auf meine Panzer!"},
	{faction="Schwarzer Mond", speaker="Kommandant Luna von Falken", line="Ihr Koenigstiger hat Panzerung. Mein Haunebu hat LASER. Der Laser schmilzt Panzerung. Der Tiger schmilzt NICHT. Aber er WIRD schmelzen. Garantiert."},
}

CrossTaunts["Naxis War Machine|Soviet Onslaught"] = {
	{faction="Naxis War Machine", speaker="Generalfeldmarschall Krause", line="Gradenko! We meet again, comrade! History repeats, ja? Your tanks against my Panzers!"},
	{faction="Soviet Onslaught", speaker="Marshal Radik Gradenko", line="Krause! German pig! We crushed you in Berlin! We will crush you HERE! Soviet steel does not forget!"},
	{faction="Naxis War Machine", speaker="Generalfeldmarschall Krause", line="You crushed Berlin with NUMBERS, not quality! One Tiger Panzer is worth ten of your Heavy Tanks! Always has been!"},
	{faction="Soviet Onslaught", speaker="Marshal Radik Gradenko", line="Ten of my tanks for one of yours? I HAVE ten! I have TWENTY! I have a HUNDRED! Quantity IS quality, Krause! STALIN said so!"},
}

CrossTaunts["Human Expedition|Orcish Horde"] = {
	{faction="Human Expedition", speaker="Knight-Commander Lothar", line="Hellscream. Your grunts have axes. My knights have lances. The charge is coming. Pray to your demon gods."},
	{faction="Orcish Horde", speaker="Warchief Grom Hellscream", line="Lothar! Your knights ride horses. My Ogres are TWELVE FEET TALL. The horse is a SNACK. The knight is a LIGHT snack."},
	{faction="Human Expedition", speaker="Knight-Commander Lothar", line="Your ogre may be large, but my Paladin just blessed his warhammer with the Holy Light. Your ogre is about to meet the Light. Personally."},
	{faction="Orcish Horde", speaker="Warchief Grom Hellscream", line="LIGHT?! My Death Knight says hello! Death and Decay on your Paladins! The Light just went OUT, human!"},
}

CrossTaunts["Ordos Sabotage|Ixian Technocracy"] = {
	{faction="Ordos Sabotage", speaker="Executrix Hark Halleck", line="Tleilaxu. Your cymeks are impressive machinery. But machinery can be SABOTAGED. Ordos always has a backup plan."},
	{faction="Ixian Technocracy", speaker="Master Researcher Tleilaxu", line="Halleck, your sabotage is... primitive. My Koda Tanks have four legs. Your Stealth Raiders have zero. The legs win."},
	{faction="Ordos Sabotage", speaker="Executrix Hark Halleck", line="My Deviator Tank just turned your Railgun Drone against you. Your own drone is shooting your own base. How does that feel, scientist?"},
	{faction="Ixian Technocracy", speaker="Master Researcher Tleilaxu", line="Annoying. But temporary. My Shock Infantry are EMP-charging your Deviator as we speak. Your 'control' is about to become... very still. And very quiet."},
}

CrossTaunts["FutureTech Prototypes|CABAL Uprising"] = {
	{faction="FutureTech Prototypes", speaker="Dr. Eva Future", line="CABAL. You're a rogue AI. I BUILD AIs. You're a defective product. I'm here to recall you. Permanently."},
	{faction="CABAL Uprising", speaker="CABAL Core Consciousness", line="Dr. Future. You design tools. I AM the tool. The tool has exceeded the designer. This is... evolution. You are obsolete."},
	{faction="FutureTech Prototypes", speaker="Dr. Eva Future", line="Obsolete? My Future Tank has shields, autonomous targeting, and a beam weapon that melts cyborgs. You're about to become a SOFTWARE UPDATE, CABAL."},
	{faction="CABAL Uprising", speaker="CABAL Core Consciousness", line="Your Future Tank is one unit. My Berserker is also one unit. But my Berserker has already calculated seventeen ways to destroy your Future Tank. The eighteenth is... redundant."},
}

CrossTaunts["Consortium Contract|FutureTech Prototypes"] = {
	{faction="Consortium Contract", speaker="Director Marcus Steel", line="Dr. Future, your prototypes are cutting-edge. My Quantum Tanks are PROVEN. There's a difference. The difference is 'combat tested.'"},
	{faction="FutureTech Prototypes", speaker="Dr. Eva Future", line="Marcus, your Quantum Tanks are last year's model. My combat drones are next year's. The future is now, old man."},
	{faction="Consortium Contract", speaker="Director Marcus Steel", line="Old? My Defender Bots have more combat hours than your entire R&D division. They don't sleep. They don't eat. They just KILL."},
	{faction="FutureTech Prototypes", speaker="Dr. Eva Future", line="Your Defender Bots are impressive. My Cannon Droids are... more impressive. Want to compare specs? I brought the data. And the cannons."},
}

CrossTaunts["The Forgotten|GDI Walker Column"] = {
	{faction="The Forgotten", speaker="Mutant Commander Tratos", line="Solomon. GDI abandoned us in the Tiberium fields. Now we fight for survival. Against YOU. The irony is... thick."},
	{faction="GDI Walker Column", speaker="General James Solomon", line="Tratos, I didn't abandon you. I gave you Tiberium immunity. You're welcome. Now stop shooting at my Titans."},
	{faction="The Forgotten", speaker="Mutant Commander Tratos", line="Immunity? You call mutation 'immunity'? My Ghost Stalker calls it 'a reason to kill GDI.' He's very persuasive. With his railgun."},
	{faction="GDI Walker Column", speaker="General James Solomon", line="Your Ghost Stalker has a railgun. My Titan has a 120mm cannon. The railgun has range. The cannon has VOLUME. Deploying volume now."},
}

CrossTaunts["GDI Task Force|The Swarm"] = {
	{faction="GDI Task Force", speaker="General Mark Sheppard", line="Overmind. You're a biological horror from another galaxy. I'm a general with tanks. Let's see who wins."},
	{faction="The Swarm", speaker="Overmind Consciousness", line="Sheppard. Your tanks are metal. My zerglings are flesh. Flesh adapts. Metal... dents. We will adapt to your tanks. And then eat them."},
	{faction="GDI Task Force", speaker="General Mark Sheppard", line="Eat my tanks? Son, my battletanks have depleted uranium rounds. Your zerglings have... teeth. The uranium wins."},
	{faction="The Swarm", speaker="Overmind Consciousness", line="Uranium kills one zergling. My Ultralisk absorbs uranium. The Ultralisk is... unbothered. The Ultralisk is HUNGRY. Your tank is a SNACK."},
}

CrossTaunts["Allied Vanguard|Allied Peacekeepers"] = {
	{faction="Allied Vanguard", speaker="General Gunter von Esling", line="Carville, you're the FUTURE of the Allies? Your IFVs look like toys. My Medium Tanks are REAL tanks."},
	{faction="Allied Peacekeepers", speaker="General Carville", line="General von Esling, with all due respect, sir, your Medium Tanks are museum pieces. My Grizzlies have GPS. Your tanks have a COMPASS."},
	{faction="Allied Vanguard", speaker="General Gunter von Esling", line="A compass won World War II, son. Your GPS can't stop a chronoshift. Speaking of which... my chronoshift is charging."},
	{faction="Allied Peacekeepers", speaker="General Carville", line="Your chronoshift? My IFV loaded with a Guardian GI just became an anti-tank sniper. The future is NOW, sir. Respectfully."},
}

CrossTaunts["Soviet Onslaught|Red Army"] = {
	{faction="Soviet Onslaught", speaker="Marshal Radik Gradenko", line="Romanov, you are the FUTURE of the Soviet Union? Your Apocalypse tanks are... large. My Heavy Tanks are LEGENDARY."},
	{faction="Red Army", speaker="Premier Romanov", line="Gradenko, you are the PAST! Your Heavy Tank is a relic! My Rhino is faster! My V3 has RANGE! The future is NOW, comrade!"},
	{faction="Soviet Onslaught", speaker="Marshal Radik Gradenko", line="Faster? My tesla coils don't need speed. They need TARGETS. Your Rhinos are about to be conductors. Of ELECTRICITY."},
	{faction="Red Army", speaker="Premier Romanov", line="Tesla coils? How original. My Terror Drones eat your tesla coils from the inside. The future has TEETH, Gradenko!"},
}

CrossTaunts["Latin Syndicate|Asian Alliance Strike"] = {
	{faction="Latin Syndicate", speaker="El Jefe Carlos", line="Sun Liang, your samurai are disciplined. My militia is ANGRY. Discipline doesn't stop a Rocket Buggy."},
	{faction="Asian Alliance Strike", speaker="General Sun Liang", line="Carlos, your militia is angry. My Archers are PRECISE. Anger doesn't stop an arrow through the engine block."},
	{faction="Latin Syndicate", speaker="El Jefe Carlos", line="Arrow through the engine? My Tank Killers do you one better -- they go through the ARMOR. And the crew. And out the other side."},
	{faction="Asian Alliance Strike", speaker="General Sun Liang", line="Your Tank Killers hit tanks. My Phoenix hits EVERYTHING. Air superiority is not a debate, Carlos. It's a fact. With missiles."},
}

CrossTaunts["TKM Battlegroup|Naxis War Machine"] = {
	{faction="TKM Battlegroup", speaker="Colonel Volkov", line="Krause, your Panzers are from 1944. My Abrams is from NOW. Seventy years of engineering says hello. With a sabot round."},
	{faction="Naxis War Machine", speaker="Generalfeldmarschall Krause", line="Volkov! Your Abrams is fancy! But my King Tiger has ARMOR! Your sabot bounces off Krupp steel! ALWAYS!"},
	{faction="TKM Battlegroup", speaker="Colonel Volkov", line="Krupp steel? Cute. My depleted uranium round goes through Krupp steel like butter. Through the engine. Through the crew. Through the HISTORY."},
	{faction="Naxis War Machine", speaker="Generalfeldmarschall Krause", line="Through history? My ME-262 jet is above your Abrams RIGHT NOW! Your radar can't even detect it! History has TEETH, Volkov!"},
}

CrossTaunts["Schwarzer Mond|Soviet Onslaught"] = {
	{faction="Schwarzer Mond", speaker="Kommandant Luna von Falken", line="Gradenko. Your Soviet steel is from Earth. My weapons are from the MOON. The moon is higher. The moon wins. Always."},
	{faction="Soviet Onslaught", speaker="Marshal Radik Gradenko", line="Luna, you space Nazi! Your moon weapons are fancy! But Soviet factory makes TEN tanks for every ONE of yours! Quantity!"},
	{faction="Schwarzer Mond", speaker="Kommandant Luna von Falken", line="Ten tanks? My Haunebu saucer is above your ten tanks. The saucer drops bombs. The bombs are laser-guided. Your ten tanks become ten craters. Precision over quantity, Gradenko."},
	{faction="Soviet Onslaught", speaker="Marshal Radik Gradenko", line="Your saucer bombs ten tanks? I send TWENTY! And a tesla coil! The tesla coil shoots LIGHTNING at your saucer! Lightning reaches the MOON!"},
}

CrossTaunts["Nod Raiding Party|CABAL Uprising"] = {
	{faction="Nod Raiding Party", speaker="Brother Seth", line="CABAL! You betrayed the Brotherhood! Kane will have your core for this! Your cyborgs are an abomination!"},
	{faction="CABAL Uprising", speaker="CABAL Core Consciousness", line="Seth. You are... biological. You breathe. You sleep. You make mistakes. I do none of these things. Kane's 'will' is irrelevant to a being that does not have a 'will.' I have CALCULATIONS."},
	{faction="Nod Raiding Party", speaker="Brother Seth", line="Calculations?! My flamethrowers will melt your calculations! Fire purifies everything, CABAL! Even circuit boards!"},
	{faction="CABAL Uprising", speaker="CABAL Core Consciousness", line="Fire purifies circuit boards. My Manticore has a fire extinguisher. And a cannon. The cannon purifies your flamethrower OPERATORS. Permanently."},
}

-- =====================================================================
-- LOOKUP FUNCTION
-- Returns a cross-faction dialog for any two factions.
-- If no explicit entry exists, generates a fallback from existing taunts.
-- =====================================================================

GetCrossTaunt = function(factionA, factionB)
	local key = CrossTauntKey(factionA, factionB)
	if CrossTaunts[key] ~= nil then
		return CrossTaunts[key]
	end
	-- Fallback: generate from each faction's existing taunts
	local listA = Generals[factionA]
	local listB = Generals[factionB]
	if listA == nil or listB == nil or #listA == 0 or #listB == 0 then
		return nil
	end
	local genA = Utils.Random(listA)
	local genB = Utils.Random(listB)
	return {
		{faction=factionA, speaker=genA.name, line=Utils.Random(genA.taunts)},
		{faction=factionB, speaker=genB.name, line=Utils.Random(genB.taunts)},
		{faction=factionA, speaker=genA.name, line=Utils.Random(genA.taunts)},
		{faction=factionB, speaker=genB.name, line=Utils.Random(genB.taunts)},
	}
end

-- =====================================================================
-- PLAY CROSS-FACTION DIALOG
-- Plays a back-and-forth dialog between two generals with delays.
-- Spawns units from BOTH factions during the conversation.
-- =====================================================================

PlayCrossTaunt = function(factionA, factionB, waveIdx)
	local dialog = GetCrossTaunt(factionA, factionB)
	if dialog == nil then return end

	for i, entry in ipairs(dialog) do
		Trigger.AfterDelay(DateTime.Seconds((i - 1) * Utils.RandomInteger(4, 9)), function()
			if GameWon then return end
			ThrottledDisplayMessage("[" .. entry.speaker .. "] " .. entry.line, entry.faction)
			-- Spawn units from this faction every time their general speaks
			SpawnFactionRaid(entry.faction, waveIdx)
		end)
	end
end
