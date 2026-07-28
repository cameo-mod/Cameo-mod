-- General Taunt Database for Crazy Survival
-- 4 generals per faction, each with a doctrine (infantry/tank/aircraft/science/stealth/artillery)
-- and unique taunt lines. Inspired by Generals Zero Hour Challenge banter.
-- Loaded via rules.yaml LuaScript Scripts list (before script.lua).

Generals = {}

local function G(name, list)
	Generals[name] = list
end

-- ==================== TIER 1 ====================

G("GDI Task Force", {
	{name="General Mark Jamison Sheppard", doctrine="tank", taunts={
		"I've been fighting Nod since before you learned to build a barracks, general.",
		"You know what I love about command? I point, and things explode. You're about to be the thing.",
		"Ion Cannon charging. I'd tell you to run but my tanks are blocking every exit.",
		"GDI doesn't do surgical strikes, general. We do geographical corrections.",
		"You built a sandbag wall. Against Mammoth Tanks. I'm putting that in the training manual.",
		"My tank column just crossed your perimeter. Your radar officer is crying. I can hear him from here.",
		"Commando's in your base. He doesn't need backup. He just needs you to keep not noticing him.",
		"Three words, general: twin barrel cannon. Three more: you can't stop it.",
		"I've been fighting Nod since before 'ok boomer' was a thing. And yes, I AM the boomer. The boomer with the Ion Cannon.",
		"You built a sandbag wall against Mammoth Tanks. I'm putting that in the training manual. Right next to 'what NOT to do.' Page one.",
		"Hell, it's about time. That's what my Titan pilot said when he finally got permission to step on your base. He's been WAITING for this. We've ALL been waiting.",
	},
	doubleTrouble={
		"Sheppard here. %otherGen% thinks THEY'RE the scary one? I've been fighting Nod since before %other% learned to build a barracks. Together? We're a geography correction. For %player%.",
		"Double the armies, double the Mammoth Tanks. %other% hits from the front, I hit from the side. %player% doesn't have a side anymore. It has a CRATER.",
		"I called up %otherGen% and said 'hey, want to tag-team %player%?' They said yes. Everyone says yes. Nobody likes %player%.",
	},
	tripleTrouble={
		"Three armies? Sheppard here. %otherGen% has their faction, I have mine, and the third one brought snacks. The snacks are EXPLOSIONS. For %player%.",
		"GDI doesn't do three-way assaults often. But when we do, %player% remembers it. Forever. Which is about four minutes. That's how long they'll last.",
	},
},
	{name="Colonel John Chuck Carter", doctrine="infantry", taunts={
		"Carter here. I've been on the front line since the first Tiberium outbreak. I don't leave the field. The field leaves me. Usually in pieces.",
		"Four hundred minigunners just crested the hill. That sound? That's your death approaching at a very brisk pace.",
		"GDI infantry doesn't retreat. We removed the word from the field manual. Page 47. I tore it out myself.",
		"APC just pulled up to your front door. Hint: what's inside rhymes with 'bullets.'",
		"My grenadiers have a game -- 'can we hit that building from here.' They always win. The building always loses.",
		"I sent one commando behind your lines. Check your buildings. Too late.",
		"You think infantry are weak? I've got a guy with a chaingun who hasn't blinked in six hours.",
		"I lost contact with General Sheppard once. Had to take charge directly. You know what happened? I won. Without orders. Without permission. Without mercy.",
		"400 minigunners just crested the hill. You can't rush B with THAT. We're rushing A, B, C, and your Construction Yard. Simultaneously. GG.",
	},
	doubleTrouble={
		"Carter here. I took charge when Sheppard went dark once. Now I'm taking charge alongside %other%. %otherGen% handles the armor, I handle the infantry. %player% handles the dying.",
		"I've been on the front line since the first Tiberium outbreak. Now %other% is on the front line too. %player% has TWO front lines. Both are losing fronts.",
		"Four hundred minigunners from my side, whatever %otherGen% brings from theirs. %player% is about to learn basic math: 400 plus anything equals 'you're dead.'",
	},
	tripleTrouble={
		"Carter here. Three armies, one target. I lost contact with command once and won anyway. Now I've got %otherGen% AND a third faction. %player% is about to lose contact with EVERYTHING.",
		"Three factions hitting %player% at once. I've been in worse situations. But I was on the OTHER side then. So has %otherGen%. We switched. %player% didn't. Bad for them.",
	},
},
	{name="Colonel Maria Olivia Morelli", doctrine="aircraft", taunts={
		"Morelli here. I command GDI's air force. I also field-test every Orca personally. Your base is my next test site.",
		"Orca squadron inbound. You have 90 seconds to write a will. I'd keep it short.",
		"Air superiority isn't a debate, general. It's my fact. Your problem.",
		"You built ONE anti-air turret. That's like bringing an umbrella to a hurricane. A very explosive hurricane.",
		"My pilots had a bet on who destroys your Construction Yard first. You're about to make someone very happy.",
		"I own the sky. You own the ground. The ground is where my bombs land.",
		"Orca pilots, weapons free. They've been laughing about your base layout for ten minutes.",
		"You know that feeling when you look up and the sky is full of helicopters? You're about to find out.",
		"I brief field commanders on the ground AND fly the mission. Multi-tasking. The Orca doesn't need a co-pilot. It needs a TARGET. That's you.",
		"Do a barrel roll! My Orca pilot just did one over your base. It was unnecessary. It was also the last thing your radar officer saw before the missile hit.",
	},
	doubleTrouble={
		"Morelli here. I command GDI's air force AND I field-test every Orca. Now %otherGen% is joining from the ground. I own the sky, they own the dirt. %player% owns NOTHING.",
		"My Orcas are inbound. %otherGen% is rolling tanks. %player% is rolling a dice. The dice says 'you lose.' The Orcas say 'we agree.'",
		"I brief field commanders AND fly missions. Right now I'm briefing %otherGen%: 'hit %player% from the ground while I hit them from the sky.' The briefing is over. The bombing has STARTED.",
	},
	tripleTrouble={
		"Morelli here. Three armies, one airspace -- MINE. %otherGen% can have the ground. The third faction can have... whatever's left. Which is nothing. Because my Orcas got there first. %player% is the target.",
		"Three factions! I field-test every Orca personally. Today I'm testing them on %player%. %otherGen% is testing their ground units on %player% too. It's a VERY thorough test. The result is '%player% is destroyed.'",
	},
},
	{name="Dr. Ignatio Mobius", doctrine="science", taunts={
		"Sonic Missile Troopers deploying! The frequency is calibrated to shatter armor. And bones. And your confidence. Mostly your armor.",
		"EMP Grenadiers advancing. Your tanks are about to experience what I call 'temporary electronic death.' The 'temporary' is generous. The 'death' is not.",
		"Exosuit prototypes rolling out. My research division built them. They're fast. They're armored. They're SCIENCE. Your tanks are not science. Your tanks are OBSOLETE.",
		"Assault APC inbound. It carries infantry. It carries science. It carries a 30mm cannon. The infantry are enthusiastic. The science is lethal. The cannon is loud.",
		"Ion Cannon charging. Yes, I have access. ONE shot. I calibrated the targeting myself. The target is your Construction Yard. The calibration is 'perfect.' The result is 'gone.'",
		"My Sonic Troopers just harmonized on your tank column. The tanks vibrated. Then they shattered. The harmonics are beautiful. The shattering is not. For you.",
		"EMP grenade deployed. Your power grid just went dark. My exosuits have their own power. Your base does not. The asymmetry is scientific. And fatal. For you.",
		"Tiberium research has side effects. One of them is WEAPONS. Another is EXOSUITS. A third is your imminent destruction. The research is going VERY well.",
		"I entered college at eleven. I won a Nobel prize. And now I'm pointing an Ion Cannon at your base. Science marches on. Over your ruins.",
	},
	doubleTrouble={
		"Mobius here. My Sonic Troopers shatter %player%'s armor while %otherGen% provides the conventional assault. Science and firepower. A beautiful partnership. For us. Not for %player%.",
		"My EMP Grenadiers just killed %player%'s power grid. %otherGen% is rolling in while the lights are out. The science is 'electromagnetic pulse.' The result is 'surprise attack in the dark.' Very effective.",
		"Ion Cannon is charged. One shot. I'm targeting %player%'s Construction Yard. %otherGen% can handle the rest. The 'rest' is 'everything that isn't hit by the Ion Cannon.' Which is most of it. But the Ion Cannon sets the TONE.",
	},
	tripleTrouble={
		"Three armies! My Exosuits advance. %otherGen% advances. The third faction advances. All three are aimed at %player%. The science is 'combined arms.' The arms are 'combined.' The result is '%player% is gone.' Very scientific. Very permanent.",
		"Sonic Troopers, EMP Grenadiers, Exosuits, Assault APCs, and ONE Ion Cannon shot. All aimed at %player%. %otherGen% adds their forces. The third faction adds theirs. The scientific term is 'overkill.' The military term is 'just enough.' The result is the same: %player% is DESTROYED.",
	},
},
})

G("Nod Raiding Party", {
	{name="Brother Seth", doctrine="infantry", taunts={
		"Kane told me to show you the light. The light of burning flamethrowers. You'll love it. Briefly.",
		"The Brotherhood is patient. We've been waiting since 1995. You won't last the next six minutes.",
		"Flamethrower infantry advancing. I'd tell you to stop, drop, and roll, but my guys will just set you on fire again.",
		"You cannot kill an idea, general. But I can kill YOU. The idea is 'fire.' The 'you' is 'your entire base.'",
		"Nod doesn't recruit soldiers. We recruit believers. Believers who are ON FIRE. And running toward you.",
		"Kane lives. You won't. That's the whole sermon today.",
		"My Chemical Warriors just walked through your Tiberium field. They didn't flinch. Your troops are coughing.",
		"If at first you don't succeed, set it on fire again. My flamethrower troops have never failed twice.",
		"PEACE THROUGH POWER! Also, your strategy is bad. Your strategy. My power. Kane would approve. Kane always approves of fire.",
		"All your base are belong to us. That's not a threat. That's a SCHEDULE. Your base is item three. After the refinery and the power plant.",
	},
	doubleTrouble={
		"Kane told me to share the battlefield with %other%. I don't share. But %otherGen% insisted. So I'm sharing the FIRE. With %player%. PEACE THROUGH POWER.",
		"The Brotherhood is patient. But with %other% helping? We don't NEED patience. We need flamethrowers. And we have them. Aimed at %player%.",
		"%otherGen% and I agreed on one thing: %player% must burn. That's two factions agreeing. When factions agree, someone burns. It's %player%.",
	},
	tripleTrouble={
		"Three armies, one sermon. Kane lives. %otherGen% lives. The third general lives. %player%? %player% does NOT live. PEACE THROUGH POWER. And fire. And tanks. And whatever the third faction brought.",
		"The Brotherhood has waited since 1995. We don't need to wait anymore. Not with %otherGen% AND a third faction. %player% won't last six minutes. Try six SECONDS.",
	},
},
	{name="Kane", doctrine="tank", taunts={
		"I am Kane. I am the Messiah. And my Stealth Tanks are the apostles. They bring revelation. The revelation is FIRE.",
		"Stealth Tanks. You can't see them. They can see you. Nod doesn't do fair fights. I don't do fair anything.",
		"My Light Tanks just crossed your perimeter at ridiculous speed. Your defenses are targeting where they WERE.",
		"You know what's worse than a tank? A tank you didn't see. You know what's worse than that? Four of them. Decloaking. Now. PEACE THROUGH POWER.",
		"Nod armor doctrine: arrive before they expect you, leave before they can react. I've been doing this since 1995. I've been doing this since BEFORE 1995.",
		"Stealth Tank decloaking beside your power plant. BOOM. Did you see it? No? There are three more. I am the future. The future is invisible.",
		"My Light Tanks don't need to destroy your defenses. They just go around them. Your turret rotation is... pathetic. Like your faith.",
		"I just sent a buggy through your base. It didn't shoot anything. The tanks behind it will. The buggy was a prophet. The tanks are the reckoning.",
		"Stealth Tanks decloaking. You can't see this coming. Literally. It's not a bug, it's a FEATURE. The feature is your death. One vision, one purpose.",
		"You have no chance to survive make your time. That's from Zero Wing. It's also from ME. Right now. To you. PEACE THROUGH POWER. PEACE THROUGH TANKS.",
	},
	doubleTrouble={
		"I am Kane. I don't need allies. But %otherGen% insisted on joining. So I allowed it. %other% attacks from one side, my Stealth Tanks decloak from the other. %player% is in the middle. The middle is where faith goes to die.",
		"%otherGen% thinks they're my equal. They're not. But they're useful. Useful like a buggy is useful -- it doesn't shoot, but the tanks behind it do. %player% is about to meet the tanks.",
		"One vision, one purpose. My vision. %otherGen%'s purpose is to distract %player% while my Stealth Tanks decloak behind their Construction Yard. The vision is FIRE.",
	},
	tripleTrouble={
		"I am Kane. I am the Messiah. Three armies? I've been leading armies since before %otherGen% was born. Before %player% was born. Before TIME was born. The third faction is just a bonus. The bonus is %player%'s destruction.",
		"Three factions, one target. I don't do fair fights. I don't do fights at all. I do RECKONINGS. %otherGen% does whatever they do. The third faction does whatever. %player% does dying. PEACE THROUGH POWER. PEACE THROUGH EVERYTHING.",
	},
},
	{name="Greg Burdette", doctrine="aircraft", taunts={
		"This is Greg Burdette, reporting live from the frontline. Tonight's top story: your base. It's on fire. Details at eleven.",
		"Venom helicopters swarming. Imagine a hornet's nest. Now imagine the hornets have missiles. And religious conviction. And a media presence.",
		"Apache inbound. Not the web server -- the one that serves you nothing except hot lead. I'll be narrating the impact. For posterity.",
		"Kane gave us the skies. What did he give you? Nothing. Because you're not Nod. Because you're losing. Because the cameras are on MY side.",
		"You look up, you see sky. I look up, I see targets. The difference is perspective. And missiles. And a teleprompter.",
		"Your anti-air turret just rotated to track my Venom. Cute. There are eleven more behind it. Film at eleven. Explosions at eleven-oh-one.",
		"Apache pilots, weapons free. Show them why the Brotherhood prays facing upward. I'll spin it as a humanitarian mission.",
		"Nod air doctrine: we don't need air superiority. We need air ANNOYANCE. And we are very annoying. And very well-produced.",
		"My Venoms are fast, flimsy, and disposable. Like your buildings. Like your hopes. Like GDI's credibility after my next broadcast.",
	},
	doubleTrouble={
		"This is Greg Burdette, reporting live. Breaking news: %other% has joined the assault on %player%. I'll be narrating the destruction. %otherGen% will be providing the ground footage. The footage is FIRE.",
		"Tonight's top story: two armies attack %player% simultaneously. %otherGen% from the ground, my Venoms from the sky. I'll spin it as a joint humanitarian effort. The effort is destroying %player%.",
		"Kane gave us the skies. %otherGen% brought the ground. Together we have... coverage. Full coverage. Of %player%. In explosions. Film at eleven.",
	},
	tripleTrouble={
		"This is Greg Burdette, reporting live from a three-way assault on %player%. %otherGen% attacks from the north, my Venoms from the south, the third faction from... somewhere. I'll narrate ALL of it. The narration is 'boom.' Repeatedly.",
		"Three armies, one broadcast. I'll spin it as a coalition peacekeeping operation. The peace is through power. The power is through %otherGen%, me, and a third faction. The operation target is %player%. The operation is 'destroy.'",
	},
},
	{name="General Gideon Raveshaw", doctrine="stealth", taunts={
		"The Black Hand does not announce itself. We are already inside your perimeter. We have BEEN inside your perimeter. Check your buildings. Too late.",
		"Stealth Tank battalion decloaking. You didn't see them. You never see them. That's the Black Hand way. That's the RAVESHAW way.",
		"My Chem Warriors just walked through your Tiberium field. They didn't flinch. Your troops are coughing. Mine are SMILING. Behind masks. Very intimidating.",
		"Subterranean APC just surfaced inside your base. The infantry inside are... enthusiastic. And on fire. And holding C4. Very Black Hand.",
		"I founded the Black Hand with Kane's blessing. We enforce the ideals of Nod. We silence opposition. You are opposition. Prepare for silence.",
		"My Stealth Tanks don't need to fight your defenses. They go around. Through. Under. The Black Hand doesn't do 'frontal assault.' We do 'gone.'",
		"Laser Turret online. Your tanks are about to learn that Nod science doesn't need Tiberium. It needs TARGETS. You're a target.",
		"I am short. I am charismatic. I am RUTHLESS. My soldiers are loyal to me as I am loyal to Kane. PEACE THROUGH POWER. PEACE THROUGH STEALTH.",
	},
	doubleTrouble={
		"Raveshaw here. The Black Hand is inside %player%'s perimeter. %otherGen% is at the front door. I'm at the back door. There is no back door. I MADE one. With C4.",
		"My Stealth Tanks decloak behind %player%'s defenses while %otherGen% hits the front. The Black Hand doesn't do frontal assaults. We do 'gone.' %otherGen% does 'boom.' Together: 'boom and gone.' Very efficient.",
		"Kane gave me the Black Hand. %otherGen% brought their army. I brought SILENCE. The silence is 'nobody in %player%'s base is alive anymore.' Very peaceful. Very Nod.",
	},
	tripleTrouble={
		"Three armies! The Black Hand strikes from within. %otherGen% strikes from without. The third faction strikes from... wherever. %player% is struck from everywhere. The striking is 'destroyed.' The 'destroyed' is permanent. PEACE THROUGH POWER.",
		"My Subterranean APC just surfaced inside %player%'s base. %otherGen% is at the walls. The third faction is... also at the walls. The walls are surrounded. The inside is INFILTRATED. The result is 'gone.' Very Black Hand. Very permanent.",
	},
},
})

G("Allied Vanguard", {
	{name="Colonel Nikos Stavros", doctrine="tank", taunts={
		"Allied armor advancing. My Medium Tanks have been polishing their barrels. It's been awkward.",
		"Ranger recon just reported your base layout. My commanders are arguing over who destroys the Construction Yard.",
		"My Light Tanks just entered your minimap. Fifteen seconds to panic. I'd start now.",
		"You know what's better than a Medium Tank? One that was on the other side of the map five seconds ago.",
		"Allied tank doctrine: speed, numbers, and the quiet confidence of someone who's already won.",
		"Chronosphere test successful. My tanks were HERE. Now they're THERE. 'There' is your refinery. The refinery is gone. Jolly good show, what?",
		"You fortified your front door. Excellent. My tanks are comin' from the side. The side you ALWAYS forget.",
		"My Medium Tank column just crossed your perimeter. Your radar officer is crying. I can hear him from here.",
	},
	doubleTrouble={
		"Stavros here. My Medium Tanks roll on %player%. %otherGen% provides the support. Two armies, one target. Very British. Very deadly.",
		"My tanks are polished. %otherGen%'s forces are... also present. Together we're like a proper coalition. The coalition's goal: %player% in ruins. Very diplomatic. Very final.",
		"I fortified my front door. Then I drove AROUND %player%'s front door. %otherGen% is at the front door. There is no back door. There is only the door that says 'you lose.'",
	},
	tripleTrouble={
		"Three armies? How delightfully excessive. %otherGen% from the left, the third faction from the right, my tanks from... everywhere. %player% is surrounded. The British word for that is 'doomed.' Very polite. Very permanent.",
		"My tank column rolled into %player%'s base. Then %otherGen% showed up. Then a THIRD faction showed up. It's getting crowded in here. The crowd is hostile. The crowd is VERY hostile.",
	},
},
	{name="Special Agent Tanya Adams", doctrine="infantry", taunts={
		"Tanya here. If you hear pistol shots, I'm already done. You just haven't noticed yet.",
		"My rifle infantry are numerous, fearless, and completely replaceable. Well, numerous and fearless. The replaceable part is just how war works.",
		"Machine gunners set up on the ridge. Your infantry are about to learn what 'suppressive fire' means.",
		"I just swam across your river. Your guard dogs saw me. They're not guard dogs anymore.",
		"I just dropped 200 rifle infantry on your flank. Your minimap lit up like a Christmas tree. A hostile one.",
		"Hello, general. Goodbye to your Power Plant. With C4. I'm very efficient.",
		"You sent a patrol to check your perimeter. They didn't come back. I'm still there. I'm waiting.",
		"Allied infantry may not have armor, but we have enthusiasm. And rifles. Mostly rifles.",
		"This is Sparta! Except Sparta had 300. I have 3,000. And they all have guns. Sparta had spears. We upgraded.",
	},
	doubleTrouble={
		"Tanya here. I'm behind %player%'s lines. %otherGen% is hitting the front. I've got 3,000 rifle infantry on the flank. That's not a battle plan. That's a MATH PROBLEM. For %player%.",
		"My infantry are numerous, fearless, and replaceable. %otherGen% brings... whatever they bring. Together we're like a very angry family reunion. The reunion is at %player%'s base. The theme is 'demolition.'",
		"I say hello to %player%. I also say hello to %otherGen%. We're coordinating. I coordinate by saying 'I'll handle the C4, you handle the tanks.' Everyone's happy. Except %player%.",
	},
	tripleTrouble={
		"Three armies, 9,000 rifle infantry, one me. %otherGen% has their forces, the third faction has theirs. %player% has sandbags. Sandbags vs. three armies. This is Sparta. Except Sparta lost. We won't.",
		"I'm behind %player%'s lines. %otherGen% is at the front. The third faction is... also at the front. There's a lot of 'front' happening. All of it is hostile. All of it is aimed at %player%.",
	},
},
	{name="General Gunter von Esling", doctrine="aircraft", taunts={
		"Black Hawk helicopters incoming. They're not here to rescue you. That would be the opposite of what's happening.",
		"Longbow helicopters, weapons free. Because regular bows are for peasants and I am a general.",
		"Rapier jumpjets deploying. They make your anti-air look like a suggestion rather than a defense.",
		"My pilots drew straws for who hits your Construction Yard. The winner is smiling. You can't see it but I can.",
		"Black Hawk inbound. Remember those movies where the helicopter arrives and everyone's saved? This is the other kind.",
		"Your radar just picked up my Longbow squadron. Your radar officer just picked up his resignation letter.",
		"I have air superiority, numerical advantage, and better theme music. You have sandbags.",
		"Allied air doctrine: own the sky, own the battle, own the rubble that used to be your base.",
	},
	doubleTrouble={
		"Von Esling here. My Longbows own the sky. %otherGen% owns the ground. %player% owns... nothing. Nothing is what's left after my helicopters and %otherGen%'s tanks are done.",
		"Black Hawk inbound. Not to rescue %player%. To destroy %player%. %otherGen% is providing the ground show. I'm providing the air show. Both shows are 'explosions.' Both shows are sold out.",
		"I have air superiority, numerical advantage, and better theme music. %otherGen% has ground forces. %player% has sandbags. The sandbags are not winning.",
	},
	tripleTrouble={
		"Three armies, one sky -- MINE. %otherGen% can have the ground. The third faction can have... whatever's left. Which is nothing. Because my Longbows got there first. %player% is the rubble.",
		"My pilots drew straws for who hits %player%'s Construction Yard. The winner is smiling. %otherGen% is also smiling. The third faction's general is also smiling. %player% is NOT smiling. %player% is DYING.",
	},
},
	{name="Professor Albert Einstein", doctrine="science", taunts={
		"Chronoshift ready. I'm about to teleport tanks into your base. They don't use doors, general. Science doesn't need doors.",
		"Gap Generator online. Your radar is now showing... nothing. My base is invisible. My tanks are not. You'll find that out shortly.",
		"Mobile Gap Generator deploying. It hides my army as it approaches. You see empty terrain. The terrain is LYING. The terrain is full of tanks.",
		"Phase Transport inbound. It's cloaked. It's carrying infantry. It's inside your perimeter. You didn't see it. You won't see the infantry either. Until they shoot.",
		"Chrono Tank test successful. It teleported behind your defenses. The test is 'can it destroy your Construction Yard.' The answer is 'yes.' The test is over.",
		"Mobile Radar Jammer active. Your minimap is now... static. You can't see my forces. I can see yours. The asymmetry is scientific. And fatal. For you.",
		"Chronosphere activation in 3... 2... 1. My entire tank column just appeared in your base. The physics are beautiful. The tanks are not. For you.",
		"Chrono reinforcements arriving. I didn't walk them here. I didn't drive them here. I TELEPORTED them here. Space is a suggestion. Your defenses are also a suggestion.",
		"Ze Chronoshift is ready! Your base is about to have uninvited guests. They don't take off their shoes. They don't need shoes. They have TREADS.",
	},
	doubleTrouble={
		"Einstein here. I've Chronoshifted tanks into %player%'s base, and %otherGen% is providing the ground assault. Two armies, one teleport. Very scientific. Very deadly.",
		"My Gap Generator hides my forces. %otherGen%'s forces are... also present. %player% can see neither. The scientific term is 'blind.' The military term is 'doomed.'",
		"Chrono Tank teleported behind %player%'s defenses. %otherGen% is at the front. The front is a distraction. The teleport is the main event. The main event is 'boom.'",
	},
	tripleTrouble={
		"Three armies? How delightfully excessive. %otherGen% from the left, the third faction from the right, my Chronoshift from... everywhere. %player% is surrounded. The scientific term is 'surrounded.' The outcome is 'destroyed.'",
		"I Chronoshifted a tank column into %player%'s base. Then %otherGen% showed up. Then a THIRD faction showed up. The physics are beautiful. The result is not. For %player%.",
	},
},
})

G("Soviet Onslaught", {
	{name="Marshal Radik Gradenko", doctrine="infantry", taunts={
		"Conscripts, charge! For every one that falls, ten more arrive. That's not bravery -- that's Soviet logistics.",
		"My conscripts are everywhere. I can send fifty for every one of your tanks. Guess who runs out first.",
		"Shock Troopers advancing with Tesla coils! Your infantry are about to learn that water and electricity don't mix.",
		"Soviet infantry doctrine: we have more bodies than you have bullets. The arithmetic of war favors the Motherland. Always. Without exception.",
		"You shot the first wave. And the second. And the third. Your barrel is melting. My conscripts are still marching. The Motherland provides. Your factory does not.",
		"Rocket soldiers, aim! Your tanks are about to meet their maker. That's me. I'm their maker. And their destroyer.",
		"You think you've killed enough of my infantry? Comrade, I have MORE. Always more. The Motherland never sleeps.",
		"Conscript report from the field: 'We are winning, comrade!' He's been saying that for six waves. He's always right.",
		"In Soviet Russia, infantry drives YOU! Your tanks? They drive themselves. Into my conscripts. The conscripts don't mind. They don't have opinions. They have RIFLES.",
	},
	doubleTrouble={
		"Gradenko here. My conscripts charge from one direction. %otherGen% attacks from another. %player% is surrounded by bodies. Most of them are mine. They don't mind. They're conscripts.",
		"Soviet infantry doctrine: more bodies than bullets. With %otherGen% helping, we have more EVERYTHING than %player%. The arithmetic favors the Motherland. And %other%. But mostly the Motherland.",
		"I have MORE conscripts. BIGGER conscripts. ANGRIER conscripts. %otherGen% has... their own forces, I suppose. Together we have a LOT of bodies. %player% has walls. Walls vs. bodies. Bodies win. Always.",
	},
	tripleTrouble={
		"Three armies, one Motherland. My conscripts charge. %otherGen% charges. The third faction charges. %player% is the target of three charges. The charge is 'run at things and shoot them.' The things are %player%'s base. The result is 'gone.'",
		"My conscripts just appeared on %player%'s radar. %otherGen%'s forces appeared on the other side. The third faction appeared from... somewhere. The radar is very depressed. The radar has given up. The radar is now a philosopher.",
	},
},
	{name="Comrade Nadia Zelenkov", doctrine="aircraft", taunts={
		"Kirov Airships rising! They're slow. They're loud. They're inevitable. Like the revolution. Like your death.",
		"Mig fighters inbound. Soviet air doctrine: we don't dogfight. We just bomb. Everything. Repeatedly.",
		"Your anti-air turret just rotated to track my Kirov. Cute. The Kirov doesn't care. The Kirov has NEVER cared.",
		"Soviet air doctrine: quantity in the sky. My Migs don't need superiority. They need TARGETS. You're a target.",
		"Hind helicopters deploying. They're ugly. They're effective. Like Soviet architecture. Like Soviet justice.",
		"Kirov reporting: bombs away. The bomb is large. The target is your Construction Yard. The result is 'gone.' Very Soviet. Very permanent.",
		"You built anti-air everywhere. Good. I have more aircraft than you have anti-air. The Motherland provides. The Motherland provides BOMBERS.",
		"My Migs just entered your airspace. Your radar officer is crying. He knows what's coming. He can't stop it. Nobody stops the Motherland.",
	},
	doubleTrouble={
		"Nadia here. My Kirovs own the sky. %otherGen% owns the ground. %player% owns... nothing. Nothing is what's left after my airships and %otherGen%'s forces are done.",
		"I prepared tea for %otherGen%. Not poisoned. Not this time. We're allies today. The tea is a gesture. The gesture means 'let's crush %player% together.' The tea is good. The crushing will be better.",
		"Soviet air doctrine: more bombers than anti-air. With %otherGen% helping on the ground, %player% can't focus on the sky. The sky is MINE. The ground is %otherGen%'s. %player% has nothing.",
	},
	tripleTrouble={
		"Three armies, one Motherland. My Kirovs bomb from above. %otherGen% attacks from the ground. The third faction attacks from... wherever. %player% is the target. The target is 'destroyed.' Very efficient.",
		"I poisoned Gradenko once. I poisoned Stalin once. I will NOT poison %otherGen%. Today we are allies. Tomorrow... we'll see. But TODAY, we crush %player%. The tea is served. The Kirovs are bombing. The crushing is NOW.",
	},
},
	{name="General Georgi Kukov", doctrine="artillery", taunts={
		"Kukov here. I command the Red Army. My artillery doesn't need line of sight. It needs a map. And a grudge. I have both.",
		"V2 rockets inbound. They don't ask questions. They don't take prisoners. They just arrive. Loudly. Soviet-style.",
		"My artillery battery just set up behind the ridge. You can't see them. They can see you. This is NOT an intelligence failure. For once.",
		"Stalin told me to win. I intend to. With artillery. With rockets. With whatever the Motherland provides. Which is a LOT of explosives.",
		"You think your walls will hold? My V2 disagrees. My V2 has opinions. Strong opinions. About your architecture.",
		"I was promoted for bravery at Berlin. I was demoted for overlooking an Allied base. Today, I'm overlooking NOTHING. My artillery sees everything.",
		"Soviet artillery doctrine: if you can see it, bomb it. If you can't see it, bomb it anyway. The Motherland provides. The Motherland provides ROCKETS.",
		"My Tesla Coils are charging. Your tanks are about to learn that Soviet science doesn't care about 'grounding.' Or 'safety.' Or 'you.'",
		"I lost an entire operation because I forgot about one Allied base. NEVER AGAIN. I now bomb EVERYTHING. If it exists, it's a target. If it doesn't exist, it's a PREEMPTIVE target.",
	},
	doubleTrouble={
		"Kukov here. My artillery bombards from behind the ridge. %otherGen% attacks from the front. %player% is between us. This is NOT an intelligence failure. I checked. Twice. %player% is definitely there. And definitely doomed.",
		"I overlooked an Allied base once. NEVER AGAIN. Now I have %otherGen% watching the other side. If they overlook something, I bomb it. If I overlook something, they... probably also bomb it. We both bomb EVERYTHING. %player% is everything.",
		"Stalin told me to win. With %otherGen% helping, I will. My V2 rockets hit from above. %otherGen% hits from... wherever. The point is: %player% gets hit. From everywhere. By everything. Soviet-style.",
	},
	tripleTrouble={
		"Three armies! I command the Red Army, and today I also command the artillery. %otherGen% commands their army. The third faction commands... whatever. We all command %player%'s destruction. The chain of command is simple: everyone fires. %player% dies.",
		"My artillery sees everything. %otherGen% sees the ground. The third faction sees... something. I don't care what they see. I bomb what I see. I see %player%. I bomb %player%. NEVER AGAIN will I overlook a base. Especially THIS base.",
	},
},
	{name="Premier Joseph Stalin", doctrine="tank", taunts={
		"Heavy Tanks rolling. Soviet steel does not negotiate. It does not hesitate. It does not STOP. I do not STOP.",
		"You built a wall. How cute. My Heavy Tank has more frontal armor than your entire defensive philosophy and zero interest in your architecture.",
		"In Soviet Union, tank drives you! Wait, no. Tank drives OVER you. Much better. For me. For the Motherland.",
		"Heavy Tank battalion, full speed. We don't brake. Braking is a capitalist invention. I banned it.",
		"You have tanks? Good. I have MORE tanks. BIGGER tanks. ANGRIER tanks. Stalin tanks.",
		"My tank column just appeared on your radar. Your radar is now depressed. It knows what's coming. I know what's coming. Your DEATH.",
		"Soviet armor doesn't retreat. We don't even have a word for it. The dictionary says 'see: cowardice.' I wrote that entry myself.",
		"Even my Flak Trucks have anti-air. That's not paranoia. That's Soviet engineering. That's STALIN engineering.",
		"I am Stalin. I do not share power. But I DO share tanks. With %player%. By driving them THROUGH %player%'s base.",
	},
	doubleTrouble={
		"Stalin here. My Heavy Tanks roll from the east. %otherGen% attacks from the west. %player% is in the middle. In Soviet Russia, 'middle' means 'crushed.' Very efficient.",
		"Soviet steel does not negotiate. Neither does %otherGen%, apparently. We didn't negotiate. We just agreed to crush %player%. The agreement was silent. The crushing will be LOUD.",
		"I have MORE tanks. BIGGER tanks. ANGRIER tanks. %otherGen% has... their own tanks, I suppose. Together we have a LOT of tanks. %player% has walls. Walls vs. tanks. Tanks win. Always. Stalin wins. Always.",
	},
	tripleTrouble={
		"Three armies! In Soviet Russia, three armies crush YOU! %otherGen% from one side, the third faction from another, my Heavy Tanks from the third side. %player% has no fourth side. Only the grave. Very efficient.",
		"My tank column just appeared on %player%'s radar. %otherGen%'s column appeared on the other side. The third faction's column appeared from... somewhere. The radar is very depressed. The radar has given up. The radar is now a philosopher. Stalin is not a philosopher. Stalin is a TANK.",
	},
},
})

-- ==================== TIER 2 ====================

G("Allied Peacekeepers", {
	{name="General Ben Carville", doctrine="tank", taunts={
		"Son, in Texas we got a sayin': 'if it ain't broke, you ain't hit it hard enough.' My Grizzlies are about to hit REAL hard.",
		"IFV column approaching. Each one's got a different weapon inside. It's like a box of chocolates. Except every chocolate explodes.",
		"My Grizzly just rolled over your defensive line. The line didn't stop it. The line didn't even SLOW it.",
		"You know what I love about IFVs? Tank, transport, anti-air. Swiss Army knife of death, son.",
		"Grizzly Tanks, roll out! And yes, I know the enemy can hear me. That's the point. The fear is half the fun.",
		"IFV just deployed a Guardian GI inside your perimeter. Anti-tank AND anti-air. It's a porcupine that hates everything.",
		"Son, I've been fightin' since before you could hold a controller. My Grizzly column is the LEAST of your worries.",
		"You fortified the front? Good. Real good. My IFVs are comin' from the side. The side you ALWAYS forget.",
		"Son, in Texas we got a sayin': 'if it ain't broke, you ain't hit it hard enough.' My Grizzlies are about to hit REAL hard. It's giving... demolition. No cap.",
	},
	doubleTrouble={
		"Carville here. Son, I've got Grizzlies rolling in from one side and %otherGen% hitting from the other. That's what we call in Texas a 'two-step.' The first step is tanks. The second step is YOUR GRAVE. For %player%.",
		"My IFVs are comin' from the side %player% always forgets. %otherGen% is comin' from the side they DO remember. Both sides are covered. %player% is NOT covered. No cap.",
		"Son, in Texas we got a sayin': 'two armies is twice the hurtin'.' Me and %otherGen% are about to hurt %player% REAL hard. It's giving... coalition demolition.",
	},
	tripleTrouble={
		"Three armies?! Son, that's what I call a 'full house.' %otherGen% has their cards, the third faction has theirs, and I've got Grizzlies. %player% has... a losing hand. The house always wins. The house is ME.",
		"Son, I've been fightin' since before %player% could hold a controller. Now I've got %otherGen% AND a third faction? That ain't fair. War ain't fair. War is Grizzlies. And Grizzlies are HERE.",
	},
},
	{name="Commander Lissette Hanley", doctrine="infantry", taunts={
		"GI deployment! They dig in, lock down, and shoot everything that moves. Especially you.",
		"Chrono Legionnaire, chronoshifting into your base. He doesn't walk. He just... appears. Behind you. With a gun.",
		"Lissette here. Allied intelligence says you're weak on the left flank. I checked. You're weak everywhere. The intelligence was understating it.",
		"Chrono Legionnaire just erased your War Factory from the timeline. It never existed. Poof.",
		"My Guardian GIs just dug in outside your base. Anti-tank, anti-air, anti-everything-you-can-send. They're very thorough. And very patient.",
		"You know what's fun? Chronoshifting an entire GI battalion behind enemy lines. No warning. No sound. Just... suddenly infantry. Everywhere. Hostile infantry.",
		"Guardian GIs in position. Anti-tank, anti-air, anti-you. Like a really angry hedgehog with a rocket launcher.",
		"GI garrison system: deploy, fortify, demolish. It's not complicated. Unless you're on the receiving end.",
		"Allied Peacekeeper doctrine: we don't retreat. We reposition. The repositioning involves more GIs. And more Chrono Legionnaires. And more winning.",
	},
	doubleTrouble={
		"Lissette here. My GIs are dug in around %player%'s base. %otherGen% is hitting the front gate. The front gate is a distraction. The dug-in GIs are the main event. The main event is 'overwhelming firepower.'",
		"I coordinated with %otherGen%. They said 'Lissette, handle the flanks.' I said 'I ALWAYS handle the flanks.' %player%'s flanks are now compromised. The compromised is from GIs. And Chrono Legionnaires.",
		"Chrono Legionnaire just erased %player%'s War Factory from the timeline. %otherGen% is handling the rest. The 'rest' is everything. Everything is being erased. By me. And %otherGen%.",
	},
	tripleTrouble={
		"Three armies, one Lissette. %otherGen% hits from the left, the third faction from the right, my GIs are already inside. %player% is surrounded by infantry, tanks, and bad decisions. The infantry is mine. The bad decisions are %player%'s.",
		"Allied intelligence says %player% can't survive a three-front war. I checked the intelligence. It's correct. %otherGen% and the third faction are the anvil. My Chrono Legionnaires are the hammer. The hammer says 'poof.'",
	},
},
	{name="Colonel Eva Lee", doctrine="aircraft", taunts={
		"Harriers inbound. Vertical takeoff means I don't need a runway. I can launch from your front lawn.",
		"Black Eagle squadron, weapons free. Because regular Eagles weren't enough. We needed BLACK ones. With MORE bombs.",
		"Allied air superiority isn't a strategy. It's a personality trait. MY personality trait.",
		"My Harrier pilots call your base 'the practice target.' I told them to be more respectful. They renamed it 'the disposable target.'",
		"Black Eagles on approach. Same bird. Bigger attitude. More explosions.",
		"My air force is like a weather forecast: guaranteed explosions, scattered debris, and a very bad day for you.",
		"You built a Patriot missile system. Cute. My Harriers can hover. Your missiles can't.",
		"Harrier pilot just reported visual on your base. He said it looks 'flattenable.' That's not a word. I'm making it one.",
		"Black Eagle just dropped a payload on your refinery. The payload was 'special.' The refinery is 'gone.' The 'special' was bunker-busting. Very thorough. Very Eva Lee.",
	},
	doubleTrouble={
		"Eva Lee here. My Harriers launch from %player%'s front lawn. %otherGen% is launching from the back. Both lawns are now runways. Both runways are exploding. This is fine. For us.",
		"Black Eagles on approach. %otherGen% is on the ground approach. I'm in the air approach. %player% is in the 'no approach' zone. The 'no approach' zone is also known as 'the grave.'",
		"My Harrier pilots call %player%'s base 'the disposable target.' With %otherGen% helping, they renamed it 'the VERY disposable target.' Very disrespectful. Very accurate.",
	},
	tripleTrouble={
		"Three armies, one airspace -- MINE. %otherGen% can have the ground. The third faction can have... whatever's left. Which is nothing. Because my Harriers got there first. %player% is the rubble.",
		"Black Eagle squadron, weapons free. %otherGen% is weapons free. The third faction is weapons free. %player% is... not free. %player% is VERY EXPENSIVE AND ABOUT TO BE DESTROYED. The forecast is explosions. Scattered debris. Very bad day.",
	},
},
	{name="President Michael Dugan", doctrine="science", taunts={
		"Prism Towers online. Einstein's finest work. They chain-link across my base. Your tanks walk in and... well. Let's just say the light is VERY bright. And VERY final.",
		"Mirage Tanks deploying. They look like trees. They shoot like tanks. Your reconnaissance just reported 'forest.' The forest is shooting back. The forest is WINNING.",
		"Weather Control Device charging. I can control the WEATHER. Your army cannot control the weather. This is what we call in politics an 'unfair advantage.' I'm fine with that.",
		"Robot Tanks rolling out. No crew, no fear, no morale problems. Just treads and guns and programming that says 'destroy %player%.' The programming is very clear on this point.",
		"Battle Fortress inbound. It's a mobile bunker with a cannon. It carries infantry. It crushes tanks. It's the most American thing I've ever authorized. And I've authorized a LOT.",
		"Chronosphere activation. My tanks just appeared in your base. I didn't ask permission. Presidents don't ask permission. We give orders. The order is 'teleport and destroy.'",
		"Guardian GI deployment! Anti-tank, anti-air, and dug in like a tick. Your tanks approach. The Guardian GIs yawn. Then they fire. The yawning stops. The firing doesn't.",
		"Prism Tower chain reaction! One tower fires, the next amplifies, the third ANNIHILATES. Einstein called it 'elegant.' I called it 'funded.' Both are accurate.",
		"I'm the President. I have nuclear launch codes, a Weather Control Device, and a Chronosphere. You have sandbags. I see why you're nervous. I'd be nervous too. If I were you. Which I'm not. Because I'm the President.",
	},
	doubleTrouble={
		"Dugan here. My Prism Towers chain across the battlefield. %otherGen% provides the ground assault. The light is bright. The tanks are loud. %player% is neither bright nor loud. %player% is GONE.",
		"Weather Control Device online. I'm calling a storm on %player%'s position. %otherGen% is calling... whatever they call. Both calls result in destruction. The President and %otherGen% agree: %player% must go. The order is bipartisan.",
		"Chronosphere activation! My tanks appear in %player%'s base. %otherGen% is at the front. The front is a distraction. The teleport is the main event. The main event is 'boom.' Presidential authorization: GRANTED.",
	},
	tripleTrouble={
		"Three armies! Prism Towers, Weather Control, Chronosphere -- I have the best toys. %otherGen% has their toys. The third faction has theirs. %player% has sandbags. The sandbags are not winning. The President declares %player% destroyed. The declaration is FINAL.",
		"Mirage Tanks, Robot Tanks, Battle Fortress, and a Weather Control Device. All aimed at %player%. %otherGen% adds their forces. The third faction adds theirs. I add PRESIDENTIAL AUTHORITY. The authority says 'destroy.' The destruction says 'yes sir.'",
	},
},
})

G("Red Army", {
	{name="Premier Alexander Romanov", doctrine="tank", taunts={
		"Rhino Tanks, advance! Soviet steel meets capitalist paper. Paper loses. Always.",
		"V3 Rocket Launchers, fire! We don't knock on your door. We blow down the wall the door was IN.",
		"Rhino Tank: best basic tank in world. Your tanks are basic. Mine are BASICALLY BETTER.",
		"Flak Tracks rolling. Anti-air AND troop transport. We don't build things that do ONE thing. That's capitalist.",
		"You think your tanks are better? Maybe. But I have MORE of mine. Quantity has a quality all its own.",
		"V3 rockets inbound. They're slow. They're loud. You can see them coming. And there is NOTHING you can do about it.",
		"My Rhino column just entered your minimap. Your units are retreating. They just SAW the Rhinos. Smart.",
		"Romanov doctrine: if at first you don't succeed, send MORE tanks. If that doesn't work, you're not sending enough tanks.",
		"Vodka. Vodka never changes. My Rhino Tanks run on vodka. Not really. But they FEEL like they do. Very aggressive. Very Soviet. Very fueled.",
	},
	doubleTrouble={
		"Romanov here. My Rhinos roll from one side. %otherGen% attacks from the other. %player% is in the middle. In Soviet Russia, 'middle' means 'between two armies.' Both armies are Soviet. Both armies are angry.",
		"V3 rockets inbound on %player%. %otherGen% is also inbound. The rockets are slow. The rockets are loud. %otherGen% is... also probably loud. Everything is loud. Except %player%. %player% will be silent. Permanently.",
		"Romanov doctrine: if at first you don't succeed, send MORE tanks. With %otherGen% sending their tanks too? That's MORE more tanks. %player% has less tanks. Less always loses to MORE.",
	},
	tripleTrouble={
		"Three armies! Soviet Russia has three armies now. Mine, %otherGen%'s, and the third one. All aimed at %player%. In Soviet Russia, three armies crush YOU! Very efficient. Very permanent.",
		"Vodka. Vodka never changes. Three armies never change either. They just get BIGGER. My Rhinos, %otherGen%'s forces, and a third faction. All fueled. All angry. All aimed at %player%. The vodka is for celebration. The celebration is after.",
	},
},
	{name="Comrade Boris", doctrine="infantry", taunts={
		"Conscripts, mobilize! Unlimited supply, zero self-preservation, and a rifle that probably works. The Soviet dream!",
		"Flak Troopers, deploy! Your tanks are about to meet something far less impressive than themselves. And lose to it.",
		"Boris here. I call MiG strike on your buildings. Is very good job. Is very satisfying.",
		"Flak Trooper just destroyed your tank. Your proud, beautiful tank. With a man holding a flak cannon. His mother is very proud.",
		"Boris does not retreat. Boris does not negotiate. Boris calls MiG. MiG does the talking. MiG's vocabulary is 'explosion.'",
		"Conscript says 'For Mother Russia!' I say 'For the Motherland!' We are both happy. You are not happy.",
		"My conscripts are charging. You're shooting them. They keep coming. Your gun is overheating. Their enthusiasm is not. It is very Russian. Very permanent.",
		"You know what's worse than one Flak Trooper? Twelve of them. In a line. All aiming at your aircraft.",
	},
	doubleTrouble={
		"Boris here. I call MiG strike on %player%'s buildings. %otherGen% is also striking. Is very good job. Is very satisfying. For both of us. Not for %player%.",
		"Boris does not retreat. Boris does not negotiate. %otherGen% also does not retreat. We both call MiG. MiG does the talking. The vocabulary is 'explosion.' In two languages.",
		"My conscripts charge %player%. %otherGen% also charges. The charge is very Russian. Very permanent. %player% is shooting. The shooting is not permanent. The charging IS.",
	},
	tripleTrouble={
		"Three armies! Boris calls MiG on %player%. %otherGen% calls... whatever they call. The third faction calls something else. All calls result in 'explosion.' %player% receives all calls. %player% does not answer. %player% is DEAD.",
		"Boris does not retreat. %otherGen% does not retreat. The third faction does not retreat. NOBODY retreats. %player% also does not retreat. But for different reasons. The reason is: they can't. They're surrounded. By three armies. Very Russian. Very final.",
	},
},
	{name="General Vladimir", doctrine="aircraft", taunts={
		"Kirov Airship reporting. I, General Vladimir, personally oversaw its construction. By 'oversaw' I mean I took credit for it. As is tradition.",
		"Kirov reporting! Yes, it is blimp. But blimp that BOMBS. You will learn to fear the blimp. I will learn to enjoy your fear.",
		"Soviet air doctrine: Kirov. Just... Kirov. I invented this doctrine. Don't look it up. I invented it.",
		"Siege Chopper transforming! It flies, it lands, it sieges. I personally designed the transformation mechanism. By 'designed' I mean I told someone to design it.",
		"Kirov inbound. I would say 'you have five minutes to panic' but I once spent forty minutes in a jacuzzi while my forces conquered Florida. Time is relative. The Kirov is not.",
		"My Kirov just appeared on your horizon. It's very large. It's very slow. It's very BOMB-Y. I am very PROUD. I named it after myself.",
		"MiG Bomber on approach. Fast, deadly, and painted red. I chose the color. Red is for heroes. I am a hero. Ask anyone. Ask Premier Romanov. Don't ask Yuri.",
		"You hear that sound? That low, distant humming? That's the Kirov. I ordered it to hum. Personally. It follows MY orders. Unlike some of my subordinates.",
	},
	doubleTrouble={
		"Vladimir here. My Kirov is inbound on %player%. %otherGen% is also inbound. I personally oversaw both assaults. By 'oversaw' I mean I took credit for both. As is tradition.",
		"Kirov reporting! %otherGen% is also reporting! Both reports say 'bombing %player%.' Both reports are accurate. I approved both reports. Personally. The reports are 'boom.' Repeatedly.",
		"I once spent forty minutes in a jacuzzi while my forces conquered Florida. Today I'm spending zero minutes in a jacuzzi. I'm watching %otherGen% and my Kirov conquer %player%. The jacuzzi can wait. The conquering cannot.",
	},
	tripleTrouble={
		"Three armies! I, General Vladimir, personally oversaw all three. By 'oversaw' I mean I took credit. The Kirov is mine. %otherGen% is... also mine, spiritually. The third faction is mine by default. Everything is mine. Except %player%. %player% is about to be NOBODY's.",
		"Kirov inbound. %otherGen% inbound. Third faction inbound. I ordered all three to hum. Personally. The humming is very loud. The bombing is louder. %player% is the audience. The audience is not enjoying the show.",
	},
},
	{name="Lieutenant Zofia", doctrine="artillery", taunts={
		"V3 Rocket Launchers, fire! I calibrated the trajectories myself. The math is beautiful. The impact is not. For you.",
		"Terror Drones deploying. They're fast, they're mechanical, and they eat tanks from the inside out. Your tank crew just heard scratching. It's already too late.",
		"Desolators advancing. Radiation cannons that make the ground itself hostile. Your infantry are glowing. That's not a buff. That's the last thing they'll see.",
		"Tesla Tanks, roll out! Lightning on treads. Your tanks are grounded. Mine are NOT. The difference is about 50,000 volts. And your life.",
		"Iron Curtain charging. One tank becomes INVINCIBLE. I choose my best tank. I send it at your base. It doesn't stop. It CAN'T stop. The curtain is beautiful. The tank is beautiful. Your base is NOT beautiful. Your base is GONE.",
		"Crazy Ivan reporting. He has bombs. He has enthusiasm. He has... issues. But mostly bombs. The bombs are for you. The issues are for his therapist.",
		"My V3 just hit your Construction Yard. The rocket was slow. The rocket was visible. You watched it come. You couldn't stop it. That's the worst part, da?",
		"Apocalypse Tank advancing. It's the heaviest tank ever built. It has twin cannons. It has anti-air missiles. It has ATTITUDE. The attitude is 'I don't stop.' The 'I don't stop' is aimed at you.",
		"Zofia here. I don't take credit like Vladimir. I don't gloat like Romanov. I calculate. I deploy. I destroy. The calculation is precise. The destruction is... also precise. Very precise. Very Soviet.",
	},
	doubleTrouble={
		"Zofia here. My V3 rockets hit %player% from across the map. %otherGen% hits from close range. Long range, close range. Both ranges are covered. %player% is in range. The range is 'destroyed.'",
		"Iron Curtain charging! I make one tank invincible. %otherGen% provides the rest. The invincible tank goes first. The rest follow. %player% can't stop the first. Can't stop the rest. Can't stop ANYTHING. Da.",
		"Terror Drones deploying against %player%. %otherGen% is deploying too. The Drones eat tanks from the inside. %otherGen% destroys from the outside. Inside, outside. %player% is surrounded. The surrounding is very thorough. Very Soviet.",
	},
	tripleTrouble={
		"Three armies! My V3 rockets calculate trajectories for %player%. %otherGen% calculates their own. The third faction calculates theirs. All calculations point to %player%. The math is beautiful. The math is FINAL. Da.",
		"Apocalypse Tank, Iron Curtain, Tesla Tanks, and Crazy Ivan. All aimed at %player%. %otherGen% adds their forces. The third faction adds theirs. I add PRECISION. The precision says 'destroyed.' The destruction says 'da, very precise.' %player% says nothing. %player% is GONE.",
	},
},
})

G("Psychic Corps", {
	{name="Yuri Prime", doctrine="infantry", taunts={
		"Your mind is weak, commander. I can see it. It's like looking through a window into a very small, very empty room.",
		"Initiates, deploy! Their psychic bolts don't need bullets. They just need your brain. And your brain is... cooperative.",
		"I see your thoughts, commander. They are... disappointing. Like a movie with a terrible ending. The ending is me.",
		"Mastermind online. It can control multiple units at once. Can YOU? No? How sad. How predictable.",
		"Your soldiers are looking at my Initiates. They're starting to wonder if they're on the wrong side. They ARE.",
		"Gatling Troopers spin up. More bullets per second than you have seconds left.",
		"Brutes charging your lines. They have no weapons. They don't NEED weapons. They ARE weapons. With legs.",
		"I don't need to destroy your army, commander. I just need to BORROW it. I'll return it when I'm done. I won't be done.",
		"I live rent free in your mind, commander. Literally. I can see your thoughts right now. You're thinking about losing. You're right.",
		"Resistance is futile. Your biological and technological distinctiveness will be added to my own. Wait, that's the Borg. But the principle is the same. I take your units. They become MY units. They were always going to be my units.",
	},
	doubleTrouble={
		"I see %otherGen%'s mind. It's... adequate. Adequate is enough. Together we see %player%'s mind. It's empty. Very empty. The emptiness is about to be filled. With explosions.",
		"I don't need %otherGen%'s help. But I'll take it. More minds, more power. %player%'s mind is weak. With two armies, it's also irrelevant. The mind was the first thing to go.",
		"Your soldiers are looking at my Initiates, %player%. They're starting to wonder if they're on the wrong side. %otherGen%'s soldiers are ALSO on the wrong side. Everyone is on MY side. The side that wins.",
	},
	tripleTrouble={
		"Three minds, one target. Mine, %otherGen%'s, and the third faction's. All aimed at %player%. I can see %player%'s thoughts right now. They're thinking 'I'm surrounded.' They're right. They're also thinking 'I'm going to lose.' Also right.",
		"I live rent free in %player%'s mind. Literally. Now %otherGen% lives there too. And the third faction. It's getting crowded in %player%'s mind. The rent is going up. The payment is 'everything.'",
	},
},
	{name="Lieutenant Yevgeny", doctrine="tank", taunts={
		"Lasher Tanks, advance! Fast, sleek, and completely immune to your opinion of them. Like a cat with a laser cannon.",
		"Magnetron locking on! Your tank is now MY tank. Thank you for the donation. I'll use it against you.",
		"Yuri's tank doctrine: why build my own tanks when I can take YOURS? It's not theft. It's 'aggressive reallocation.'",
		"Magnetron just lifted your tank off the ground. It's dangling. Like a pinata. And I'm about to swing.",
		"I just used a Magnetron to drop your tank into your OWN base. Your own defenses shot it. Thanks for the tank.",
		"Your tank commander just reported 'we're being lifted into the air.' I already took control of his mind. He's very calm now.",
		"Lasher Tanks don't need to be the best. They just need to be ENOUGH. Because I also have YOUR tanks.",
		"Lasher column approaching. They're fast, they're mean, and they're MINE. Not yours. The distinction is about to become relevant.",
		"I see what you did there. You built a defense. I see it. I'm taking it. The seeing and the taking are the same thing. For me.",
	},
	doubleTrouble={
		"Yevgeny here. My Magnetrons are lifting %player%'s tanks. %otherGen% is providing the ground assault. The tanks I lift, I drop on %player%. The tanks %otherGen% brings, they drive over %player%. Either way: tanks. On %player%.",
		"Why build my own tanks when I can take %player%'s? And with %otherGen% attacking too? %player% has to defend against their OWN tanks AND %otherGen%'s tanks. The math is 'aggressive reallocation squared.'",
		"Lasher Tanks advancing. %otherGen% is also advancing. Both advances are aimed at %player%. The advances are fast. The advances are mean. The advances are MINE. Not %player%'s. The distinction is about to become fatal.",
	},
	tripleTrouble={
		"Three armies! I see what %otherGen% did there. I see what the third faction did there. I see what %player% did there. %player% built a defense. I'm taking it. All three armies are using it. Against %player%. The irony is delicious. The taking is permanent.",
		"Magnetron locking on %player%'s tanks. %otherGen% is locking on too. The third faction is... also locking on, presumably. Everything is locked on. %player% is locked. The lock is 'destroyed.'",
	},
},
	{name="Captain Irina", doctrine="aircraft", taunts={
		"Floating Disc, deployed. It disables your power, drains your resources, and shoots your units. I'm very proud of it.",
		"Your power grid just went dark. Your resources are draining. Your dignity is next. The Disc is very thorough.",
		"Floating Disc: the only unit that can rob you AND shoot you at the same time. It's multitasking.",
		"I don't need an airfield. My Discs float. They float and they JUDGE. They don't like what they see.",
		"Disc squadron, weapons free. Disable their power. Drain their resources. Make them feel the slow, grinding hopelessness.",
		"Your Construction Yard just lost power. Your base is dark. Your resources are gone. And I'm still here. Floating. Judging.",
		"You built anti-air for my Discs? My Discs can also disable POWER. Your anti-air needs power. It's now a lawn ornament.",
		"Floating Disc inbound. It's a hovering saucer of misery coming for your base. And your life. Mostly your base.",
		"You built a Patriot missile system. My Disc disabled its power. The Patriot is now an Unpatriotic lawn ornament. Very still. Very useless. Very mine.",
	},
	doubleTrouble={
		"Irina here. My Floating Discs drain %player%'s power. %otherGen% destroys what's left. The power is gone. The defenses are dark. %otherGen% walks in. Very thorough. Very permanent.",
		"My Discs float. They float and they JUDGE. %otherGen% also judges, I assume. Together we judge %player%. The verdict is 'guilty.' The sentence is 'destroyed.' The execution is NOW.",
		"Floating Disc inbound on %player%. %otherGen% is also inbound. The Disc disables power. %otherGen% disables... everything else. Between us, 'everything' is covered. %player% is NOT covered.",
	},
	tripleTrouble={
		"Three armies, one verdict. My Discs drain %player%'s power. %otherGen% destroys the defenses. The third faction destroys... whatever's left. The verdict is 'guilty.' The sentence is 'three armies.' The execution is NOW.",
		"My Discs float over %player%'s base. %otherGen% is on the ground. The third faction is... somewhere. Everyone is somewhere. %player% is nowhere. The nowhere is 'destroyed.' The Discs are still floating. The floating is eternal.",
	},
},
	{name="Dr. Volkov", doctrine="science", taunts={
		"Boomer submarines, surfacing! They launch missiles from underwater. You can't see them. You can hear them. Briefly. Then you can't hear anything. Ever again.",
		"Genetic Mutator, charging! Your infantry are about to become Brutes. MY Brutes. The mutation is painless. For me. For you it's... educational. Briefly.",
		"Cloning Vats, online! Every infantry unit I lose, I replace with a clone. Your units die permanently. Mine don't. The math is very simple. The math is 'I win.'",
		"Chaos Drones, deploying! They emit psychic noise that makes your units fight each other. Your tanks are shooting your own base. I didn't tell them to. The Chaos did. The Chaos is very effective.",
		"Boomer just launched three missiles at your base. From underwater. While submerged. Your anti-air can't find the source. The source found you. The source is BELOW.",
		"My Cloning Vats just produced another batch of Initiates. I lost ten. I made twenty. The deficit is YOURS. The surplus is mine. The math is psychic. The math is FINAL.",
		"Genetic Mutator, firing! Your proud infantry just became Brutes. They're angry. They're strong. They're MINE. The mutation is instant. The loyalty is... also instant. Very efficient.",
		"Chaos Drone just flew over your base. Your units are fighting each other. Your base is on fire. You set it on fire. Well, YOUR units set it on fire. The Chaos made them. The Chaos is very persuasive.",
		"Dr. Volkov here. I was a Soviet scientist before Yuri showed me the truth. The truth is 'mind control works.' The science is 'mutation is fun.' The result is 'you lose.' Very scientific. Very psychic.",
	},
	doubleTrouble={
		"Volkov here. My Boomers launch missiles from underwater while %otherGen% attacks from the ground. Above, below. Both directions. %player% is in the middle. The middle is 'destroyed.' Very scientific.",
		"Genetic Mutator, charging! Your infantry become MY Brutes. %otherGen% provides the conventional assault. The mutation is instant. The assault is relentless. %player% fights their own former soldiers AND %otherGen%. The math is 'unfair.' The math is correct.",
		"Chaos Drones over %player%'s base. Their units fight each other. %otherGen% attacks from outside. Inside, chaos. Outside, assault. %player% is between. The 'between' is 'destroyed.' The science is 'psychic warfare.' The warfare is very psychic. Very permanent.",
	},
	tripleTrouble={
		"Three armies! My Boomers launch from below. %otherGen% attacks from above. The third faction attacks from... wherever. My Genetic Mutator turns %player%'s infantry into Brutes. My Chaos Drones make %player%'s tanks fight each other. The science is 'psychic combined arms.' The arms are combined. The result is '%player% is gone.' Very Volkov. Very final.",
		"Boomers, Mutator, Cloning Vats, Chaos Drones -- all my designs. All aimed at %player%. %otherGen% adds their forces. The third faction adds theirs. I add SCIENCE. The science is 'mutation, cloning, chaos, and missiles.' The result is 'nothing left to study.' Very efficient. Very Volkov. Very permanent.",
	},
},
})

G("Asian Alliance Strike", {
	{name="General Sun Liang", doctrine="infantry", taunts={
		"Samurai, draw blades! They sharpened their swords this morning. They told me it was 'for ceremony.' It wasn't.",
		"My Archers can hit a tank from way across the battlefield with an arrow that punches through armor. Your tank designers didn't account for arrows.",
		"Asian Phalanx doctrine: my units buff each other by standing near each other. Your units don't even LIKE each other.",
		"Samurai charge! They're running at your tanks. With swords. They're not afraid of tanks. They find tanks OFFENSIVE.",
		"Banzai Mode activated! Every infantry unit is now willing to die for the cause. They were already willing. Now it's POLICY.",
		"Veteran Archers, nock arrows! Anti-tank, anti-air, anti-you. They're not 'anti' anything. They're 'pro' victory.",
		"My Samurai just reached your tank line. They're INSIDE the tanks now. Through the hatch. With swords.",
		"You deployed anti-infantry mines. My Samurai just walked through them. Not around them. THROUGH them.",
	},
	doubleTrouble={
		"Sun Liang here. My Samurai charge %player% from one direction. %otherGen% attacks from another. The Samurai find %player%'s tanks OFFENSIVE. %otherGen% finds them... targetable. Both perspectives end the same way: %player% has no tanks.",
		"My Archers hit %player%'s tanks from way across the battlefield. %otherGen% hits from... wherever. Together we have coverage. Full coverage. Of %player%. In arrows and whatever %otherGen% uses.",
		"Banzai Mode activated! Every infantry unit is willing to die. With %otherGen% helping, they don't NEED to die. They just need to kill %player%. The killing is very efficient. The efficiency is very Asian Alliance.",
	},
	tripleTrouble={
		"Three armies! My Samurai charge. %otherGen% charges. The third faction charges. %player% is the target of three charges. The Samurai find this HONORABLE. %otherGen% finds it practical. %player% finds it FATAL.",
		"My Archers can hit a tank from way across the battlefield. With %otherGen% AND a third faction, I don't need to hit from way across. I can hit from close. Close is better. Close is arrows. In %player%'s face.",
	},
},
	{name="Admiral Hiro Tanaka", doctrine="tank", taunts={
		"Lynx Tanks, advance! The Phalanx aura means they get STRONGER near each other. Like a team building exercise. With cannons.",
		"Quasar plasma tanks, deploy! EMP rounds incoming. Your electronics just resigned. Effective immediately.",
		"Dragonfly hover-transports, loaded and ready. It's a flying bunker that also transports anger.",
		"Lynx Tank just entered your base. Then another. Then five more. Each one is buffing the others. They're having a party.",
		"Quasar online. EMP means your tanks can't move. Your radar doesn't work. My tanks CAN move. This is 'an advantage.'",
		"Dragonfly deploying troops directly into your perimeter. It hovers. It has fireports. It's a flying apartment building full of people who hate you.",
		"My Lynx column just formed up. They're stronger now. Your tanks are not. Numbers do not lie. Neither do cannons.",
		"You tried to flank my Quasar. It EMP'd your flanking force. They're sitting ducks. Very stationary ducks.",
	},
	doubleTrouble={
		"Tanaka here. My Lynx Tanks advance on %player% with Phalanx aura. %otherGen% is also advancing. The aura buffs my tanks. %otherGen% buffs... their own things. Both advances are buffed. %player% is NOT buffed. %player% is DEBUFFED. Permanently.",
		"Quasar EMP rounds incoming on %player%. %otherGen% is providing the ground assault. Your electronics just resigned. %otherGen%'s assault just arrived. The resignation and the arrival are simultaneous. Very coordinated. Very final.",
		"My Lynx column just formed up near %otherGen%'s forces. They're buffing each other. It's a team building exercise. With cannons. Aimed at %player%. The team building is 'destroy %player%.' The exercise is VERY successful.",
	},
	tripleTrouble={
		"Three armies! My Lynx Tanks buff each other. %otherGen% buffs their forces. The third faction buffs... something. Everyone is buffed. %player% is NOT buffed. %player% is surrounded by buffed armies. The buff is 'strength.' The strength is 'overwhelming.' %player% is 'overwhelmed.'",
		"Quasar online. EMP means %player%'s tanks can't move. %otherGen% is moving. The third faction is moving. Everyone is moving EXCEPT %player%. The movement is 'toward %player%.' The destination is '%player% destroyed.'",
	},
},
	{name="Air Marshal Chen Wei", doctrine="aircraft", taunts={
		"Phoenix bombers, inbound! They rise from ashes. YOUR ashes. Specifically. I checked the flight plan.",
		"Pelican helicopters, deploy! Heavy, durable, and surprisingly graceful. My Pelican eats BASES whole.",
		"Harbinger gunships on approach. The name says it all. It heralds your doom. Professionally. With missiles.",
		"Phoenix, Pelican, Harbinger -- my air force sounds like a mythology textbook. It fights like one too.",
		"Harbinger inbound. It's a fighter-bomber with delusions of grandeur. And the firepower to back them up.",
		"My Phoenix just bombed your refinery. It rose from the ashes of your refinery. That's not metaphorical. That's literal.",
		"Asian Alliance air doctrine: three aircraft, three roles, zero mercy. The zero is the amount of mercy.",
		"Your anti-air just fired at my Harbinger. The Harbinger fired back. Your anti-air is now scrap. The Harbinger is still flying.",
	},
	doubleTrouble={
		"Chen Wei here. My Phoenix bombers rise from ashes. %otherGen% rises from... wherever. Both of us rise toward %player%. The ashes will be %player%'s. The rising is ours.",
		"Harbinger inbound on %player%. %otherGen% is also inbound. The Harbinger heralds doom. %otherGen% heralds... also doom, presumably. Two heralds of doom. For %player%. The doom is very well-heralded.",
		"Asian Alliance air doctrine: three aircraft, three roles, zero mercy. With %otherGen% adding their ground forces? Four roles. Still zero mercy. The zero is for %player%. The mercy is zero. The explosions are not zero.",
	},
	tripleTrouble={
		"Three armies! Phoenix, Pelican, Harbinger -- my air force sounds like a mythology textbook. %otherGen% adds their chapter. The third faction adds theirs. The textbook is 'How to Destroy %player%.' It's a bestseller. The sequel is 'You Already Did.'",
		"My Phoenix just bombed %player%'s refinery. It rose from the ashes. %otherGen% is bombing the rest. The third faction is bombing... whatever's left. Everything is ashes. Everything rises. Except %player%. %player% stays down. Permanently.",
	},
},
	{name="Dr. Park Min-jun", doctrine="science", taunts={
		"Plasma Cannon, charging! Korean energy weapons at maximum output. Your armor rating is about to become irrelevant. Like your strategy. Like your base.",
		"Shield Generator, online! My units have energy shields. Your units have... enthusiasm. The shields block bullets. The enthusiasm blocks nothing. The comparison is not flattering. For you.",
		"Tesla Coil array, deployed! Stolen from the Soviets, improved by Korean engineering. The original was good. The improved version is BETTER. And aimed at you.",
		"Asian Alliance science doctrine: we don't just fight. We INNOVATE. While you build walls, we build weapons that go THROUGH walls. The innovation is constant. The walls are irrelevant.",
		"EMP Warhead, launched! Your base is dark. Your tanks are stalled. Your infantry are confused. The confusion is permanent. The darkness is temporary. The tanks are about to be scrap.",
		"Particle Beam, firing! It cuts through armor like a katana through paper. Except the beam is faster. And the paper is your entire army. The cutting is very thorough.",
		"My research division just developed a new alloy. It's lighter, stronger, and more explosive than anything you have. The 'explosive' part is intentional. The 'aimed at you' part is also intentional.",
		"Quantum Shield, activated! My units are now phased partially out of reality. Your weapons pass through them. Their weapons do NOT pass through you. The physics are complicated. The result is simple: you lose.",
	},
	doubleTrouble={
		"Dr. Park here. My Plasma Cannon charges while %otherGen% provides the ground assault. The science is 'directed energy.' The result is 'directed destruction.' Both are aimed at %player%. The direction is very precise. Very Korean.",
		"Shield Generator, online! My units have energy shields. %otherGen%'s units have... their own protection, I assume. %player%'s units have nothing. The nothing is not a shield. The nothing is 'destroyed.' Very scientific. Very final.",
		"EMP Warhead, launched! %player%'s base is dark. %otherGen% is rolling in. The science is 'electromagnetic pulse.' The military term is 'turkey shoot.' The turkey is %player%. The shoot is VERY thorough.",
	},
	tripleTrouble={
		"Three armies! My Plasma Cannon, Particle Beam, and Quantum Shields. %otherGen% adds their forces. The third faction adds theirs. I add SCIENCE. The science says 'destroyed.' The destruction says 'yes, doctor.' %player% says nothing. %player% is GONE. Very precise. Very permanent.",
		"Plasma, shields, EMP, particle beams -- all aimed at %player%. %otherGen% attacks from the ground. The third faction from... wherever. The science is 'combined arms with directed energy.' The arms are combined. The energy is directed. At %player%. The result is 'nothing left to study.' Very efficient. Very Park.",
	},
},
})

G("Imperial Japan", {
	{name="Shogun Kenji Tenzai", doctrine="infantry", taunts={
		"Samurai, charge! They've achieved inner peace. Inner peace and outer violence. It's a samurai thing.",
		"Archer Maidens, deploy! Anti-tank snipers with traditional bows. Your tank commander stopped laughing when the arrow went through his viewport.",
		"Imperial infantry doctrine: fast, aggressive, and absolutely no retreat. We removed 'retreat' from our vocabulary. And 'surrender.' And 'maybe.'",
		"Samurai just reached your tank column. Your tanks fired. The samurai kept running. Your tank crews are now VERY confused. And dead.",
		"Bushido Discipline activated! My infantry fight harder. Your infantry fight... less hard. The comparison is not flattering.",
		"Archer Maiden just put an arrow through your tank's engine block. From way over there. With a bow.",
		"My Samurai don't fear death. They've already made peace with it. YOUR death, specifically. They wrote a haiku about it.",
		"You think swords can't beat tanks? My Samurai agree to disagree. With their swords. In your tanks. Right now.",
	},
	doubleTrouble={
		"Kenji here. My Samurai charge %player% with inner peace and outer violence. %otherGen% charges with... their own peace, I suppose. Both charges are violent. Both are aimed at %player%. The peace is inner. The violence is VERY outer.",
		"Bushido Discipline activated! My infantry fight harder. %otherGen% also fights harder. %player% fights... less harder. The comparison is not flattering. For %player%. The bushido is strong. The %player% is weak.",
		"My Samurai don't fear death. They've made peace with %player%'s death. %otherGen% has also made peace with it. Everyone has made peace with %player%'s death. Everyone except %player%. %player% is still in denial. The denial is temporary.",
	},
	tripleTrouble={
		"Three armies, one bushido. My Samurai charge. %otherGen% charges. The third faction charges. All charges are aimed at %player%. The bushido says 'no retreat.' Nobody retreats. %player% also cannot retreat. But not by choice. By surrounding. Very honorable. For us. Very fatal. For %player%.",
		"My Samurai wrote a haiku about %player%'s death. It's three lines. Line one: 'Three armies come.' Line two: 'Swords and tanks and fire.' Line three: '%player% is gone.' The haiku is very traditional. The death is very permanent.",
	},
},
	{name="Shinzo Nagama", doctrine="tank", taunts={
		"Imperial armor doesn't need to be heavy. It needs to be HONORABLE. Speed is honor. Honor is life. Also: speed is fun.",
		"Core vehicles transforming! War Factory, Refinery, Power Plant -- all in one. It is like origami. But military. The ancient art of folding. Applied to war.",
		"You cannot catch what you cannot hit. My tanks are very fast. Your targeting systems are very slow. This is not a coincidence. This is design.",
		"Imperial tank doctrine: hit, run, transform, hit again from a different angle. It is very confusing. For you. For me it is art. The art of war. Literally.",
		"Waveforce Tank charging! It is a beam weapon on treads. Very polite. Very deadly. It says 'excuse me' before it fires. Then it fires. There is no 'after.'",
		"My tank just transformed into a War Factory. Behind your lines. It is building MORE tanks. Behind your lines. You should have built walls. Walls do not stop transformers.",
		"Shrine Minitank: small, fast, and surprisingly angry. Like a ronin with a cannon. Inside your base. Right now. Very dishonorable for you. Very effective for me.",
		"You are chasing my tanks. My tanks just transformed into a Refinery. The tanks that WERE there are now behind you. The hunt has reversed. You are the prey.",
	},
	doubleTrouble={
		"Shinzo here. My tanks transform. %otherGen% does not transform, I think. But together we transform %player%'s base. From 'base' to 'rubble.' The transformation is military origami. Applied to war. Very artistic. Very final.",
		"My tank just transformed into a War Factory behind %player%'s lines. %otherGen% is at the front. The War Factory builds MORE tanks. Behind %player%. The tanks go forward. %otherGen% pushes forward. %player% is between. The 'between' is 'destroyed.'",
		"Imperial tank doctrine: hit, run, transform, hit again. With %otherGen% hitting too? That's hit, run, transform, hit, AND %otherGen% hits. Very confusing. For %player%. For me it is art. The art of war. With a collaborator.",
	},
	tripleTrouble={
		"Three armies! My tanks transform. %otherGen% does... whatever they do. The third faction does whatever. Everything transforms %player%'s base. From 'base' to 'gone.' The transformation is very thorough. The 'gone' is very permanent.",
		"You cannot catch what you cannot hit. With three armies, %player% cannot hit ANYTHING. My tanks transform. %otherGen% attacks. The third faction attacks. Everything attacks. %player% is the prey. The hunt has reversed. Three times.",
	},
},
	{name="Naomi Shirada", doctrine="aircraft", taunts={
		"Zero Fighters, scramble! Named after the legendary WWII ace plane. It still aces. It is in the name. I do not name things poorly.",
		"Japanese Bombers inbound. They deliver explosions with punctuality. Very Japanese. Your base is the delivery address. The package is detonation.",
		"Sky Hawks deploying! Helicopter gunships with what I can only describe as anime energy. Yes, I said anime. The Empire does not apologize for its culture.",
		"Imperial air doctrine: fast, precise, and accompanied by dramatic music. You cannot hear the music. That is intentional. The music is for US. The explosions are for YOU.",
		"Zero Fighter just did a barrel roll over your base. It was unnecessary. It was also beautiful. The pilot is very proud. I approved the barrel roll. Personally.",
		"Super Bomber Airstrike available! It is like a birthday present. But instead of cake, it is BOMBS. Happy birthday. From the Empire. With love. And fire.",
		"My Zero just outmaneuvered your anti-air. It flew between your two missile turrets. They shot each other. The Empire calls this 'efficiency.' Your military calls it 'embarrassing.'",
		"You have one air defense unit. I have twelve aircraft. This is not a battle. This is a ceremony. The ceremony is your defeat. You are invited. Attendance is mandatory.",
	},
	doubleTrouble={
		"Naomi here. My Zero Fighters scramble. %otherGen% scrambles too. Both scrambles are aimed at %player%. The ceremony is your defeat. With two armies, it's a LARGER ceremony. Attendance is still mandatory. For %player%.",
		"Imperial air doctrine: fast, precise, accompanied by dramatic music. %otherGen% adds their own music. The music is 'explosions.' In harmony. The harmony is very destructive. For %player%.",
		"Super Bomber Airstrike available! It is like a birthday present. With %otherGen% helping, it's a JOINT birthday present. From two armies. The present is bombs. The wrapping is fire. Happy birthday, %player%. From the Empire. And %otherGen%.",
	},
	tripleTrouble={
		"Three armies! My Zero Fighters scramble. %otherGen% scrambles. The third faction scrambles. All scrambles are aimed at %player%. The ceremony is VERY large. The attendance is VERY mandatory. The defeat is VERY permanent.",
		"You have one air defense unit. I have twelve aircraft. %otherGen% has... their own things. The third faction has theirs. Total: one air defense vs. three armies. This is not a battle. This is a ceremony. The ceremony is '%player% is gone.' You are invited. Attendance is mandatory. The dress code is 'explosions.'",
	},
},
	{name="Crown Prince Tatsu", doctrine="science", taunts={
		"Nanotechnology online. I designed it myself. My father follows destiny. I follow SCIENCE. Science is more reliable. And more destructive.",
		"Rocket Angels deploying! Jetpack infantry with missile barrages. My father calls them 'inelegant.' I call them 'effective.' The missiles agree with me.",
		"Shogun Executioner, AWAKEN. It is a six-legged mech with three katanas. It walks on water. It cuts tanks in half. My father calls it 'divine.' I call it 'my design.' Both are correct.",
		"King Oni mechs, activate! Giant bipedal robots with eye beams. My father says they honor the spirits. I say they honor PHYSICS. The physics say 'destroy %player%.' The spirits... probably agree.",
		"Giga Fortress, transforming! It's a flying fortress that becomes a naval dreadnought. Or vice versa. The transformation is nanotech. The result is explosions. For %player%.",
		"Nanocore deployment! My buildings don't need foundations. They unfold from nanocores. ANYWHERE. Including behind your lines. The nanocore is now a War Factory. Behind your lines. You should have checked.",
		"My father believes in divine destiny. I believe in superior technology. Both lead to the same conclusion: %player% is destroyed. The technology is faster. The technology is Tatsu.",
		"Yari Minisubs surfacing! They're small, they're fast, and they carry torpedoes. Your battleship didn't see them. Your battleship is now sinking. The ocean is very deep. Very final.",
	},
	doubleTrouble={
		"Tatsu here. My nanotech deploys behind %player%'s lines. %otherGen% attacks from the front. The front is tradition. The nanocore behind the lines is INNOVATION. My father respects tradition. I respect results. The result is '%player% destroyed.'",
		"Shogun Executioner, AWAKEN! %otherGen% provides the ground assault. The Executioner provides the... execution. Six legs, three katanas, zero mercy. The mercy is zero. The execution is VERY high. For %player%.",
		"My father believes in divine destiny. I believe in superior technology. %otherGen% believes in... whatever they believe. All three beliefs lead to %player% destroyed. The technology is faster. The destiny is slower. Both are final.",
	},
	tripleTrouble={
		"Three armies! My nanotech unfolds everywhere. %otherGen% attacks traditionally. The third faction attacks... however. The Shogun Executioner attacks with three katanas. All attacks are aimed at %player%. The nanotech is innovation. The tradition is my father's. The result is the same: %player% is GONE. Very scientific. Very permanent.",
		"King Oni mechs, Rocket Angels, Giga Fortress, and Shogun Executioner. All my designs. All aimed at %player%. %otherGen% adds their forces. The third faction adds theirs. I add NANOTECHNOLOGY. The nanotechnology says 'unfold and destroy.' The destruction unfolds. Very efficiently. Very Tatsu.",
	},
},
})

G("Latin Syndicate", {
	{name="El Jefe Carlos", doctrine="infantry", taunts={
		"Latin Militia, deploy! Angrier than your army. Deadlier than your army. They're not soldiers -- they're entrepreneurs. Of violence.",
		"Tank Killers, aim! Yes, we named a unit 'Tank Killer.' We're subtle like that. We also named our dog 'Biter.' He bites.",
		"Grenade Monkeys incoming! They throw grenades. They're monkeys. I don't need to explain more.",
		"Syndicate infantry doctrine: angry, resourceful, and armed with stolen technology. It's not stealing if you win.",
		"Latin Militia just swarmed your tank. There are 30 of them. They're climbing on the tank now. This is going to get embarrassing.",
		"Grenade Monkey just threw a grenade into your bunker. Through the slit. From way too far away. While eating a banana.",
		"My Militia says 'viva la revolucion!' I say 'viva la victoria!' We are in agreement. The revolution is very effective.",
		"You sent a tank to kill my Militia. My Militia killed the tank. With a rocket launcher smaller than your tank's exhaust pipe.",
		"Grenade Monkeys, deploy! They throw grenades. They're monkeys. Monkey see, monkey do, monkey BLOW UP your base. The banana is for morale. The grenade is for you.",
	},
	doubleTrouble={
		"El Jefe here. My Militia swarms %player%. %otherGen% attacks from the other side. The Militia are entrepreneurs. Of violence. %otherGen% is also an entrepreneur, I assume. The business is 'destroy %player%.' The business is BOOMING.",
		"Grenade Monkeys incoming on %player%! %otherGen% is also incoming. The monkeys throw grenades. %otherGen% throws... whatever they throw. Both are aimed at %player%. The banana is for morale. The grenade is for %player%. The 'whatever' is also for %player%.",
		"Syndicate infantry doctrine: angry, resourceful, armed with stolen tech. With %otherGen% helping? Twice as angry. Twice as resourceful. Twice as stolen. %player% is the target. The target is 'stolen from existence.'",
	},
	tripleTrouble={
		"Three armies! My Militia swarms. %otherGen% swarms. The third faction swarms. Three swarms. One %player%. The math is 'three swarms minus one %player% equals zero %player%.' Very simple math. Very final math.",
		"Grenade Monkeys, deploy! Three armies, one banana. The banana is for morale. The grenade is for %player%. %otherGen% has their own grenade. The third faction has theirs. Three grenades. One %player%. The banana survives. %player% does not.",
	},
},
	{name="Capitan Diego Morales", doctrine="tank", taunts={
		"Raider Buggies, full throttle! Scrap metal, duct tape, and bad intentions. The duct tape is load-bearing. I'm not joking.",
		"Rusher Tanks, charge! Fast, mean, and named after what they do to YOUR base. We called them 'Rusher' because 'Base Destroyer' was too long.",
		"Diablo chaingun vehicle, spinning up! It's called 'Devil' for a reason. The reason is bullets. Lots of bullets.",
		"Syndicate tank doctrine: Soviet surplus, Latin engineering, and absolutely zero warranty. It works anyway. Don't ask how.",
		"Rusher Tank just blew past your defenses. It didn't stop. It's in your base now. It's rushing. The name is very literal.",
		"Diablo says 'hola' with a chaingun. It's the loudest greeting you'll ever receive. Very many bullets per second.",
		"My Raider Buggy just did a drive-by on your Power Plant. It didn't even stop. Very efficient.",
		"You're tracking my Rusher Tank on radar. Your radar can barely keep up. Your DEFENSES can't keep up. They're aiming at where it WAS.",
	},
	doubleTrouble={
		"Diego here. My Raider Buggies do a drive-by on %player%. %otherGen% does a... drive-something, I assume. Both are fast. Both are mean. Both are aimed at %player%. The duct tape is load-bearing. The bullets are %player%-bearing.",
		"Rusher Tanks charge %player%. %otherGen% also charges. The Rusher is named after what it does to %player%'s base. With %otherGen% helping, it rushes TWICE as hard. The base is rushed. The base is GONE.",
		"Syndicate tank doctrine: Soviet surplus, Latin engineering, zero warranty. With %otherGen% adding their forces? Double the surplus. Double the engineering. Still zero warranty. It works anyway. Against %player%. Don't ask how.",
	},
	tripleTrouble={
		"Three armies! My Rusher Tanks charge. %otherGen% charges. The third faction charges. Three charges. One %player%. The Rusher is very literal. It rushes. %player% is rushed. %player% is GONE. Very efficient. Very Syndicate.",
		"My Diablo says 'hola' with a chaingun. %otherGen% says 'hello' with... whatever they use. The third faction says something in their own language. All greetings are bullets. All bullets are for %player%. The greeting is very loud. The response is silence. Permanent silence.",
	},
},
	{name="Pilot Rosa Martinez", doctrine="aircraft", taunts={
		"Yakovlev fighters, scramble! Soviet surplus with a Latin paint job. We stole it, we painted it, we added flames. Flames make it faster.",
		"MiG-21 on approach! Stolen, refurbished, and VERY angry about its previous owner. The previous owner didn't maintain it.",
		"Syndicate air doctrine: we stole the planes, we stole the tech, we're stealing your victory. Theft. But make it aerial.",
		"Yakovlev just entered your airspace. Your radar identified it as 'Soviet surplus.' It is. But it's ANGRY Soviet surplus. With a Latin pilot.",
		"MiG-21 inbound. It's not the newest plane. But it IS the angriest. And anger counts for a LOT in aerial combat.",
		"Stolen tech air wing, weapons free! We don't have a warranty. We don't NEED a warranty. Warranties are for people who expect things to break.",
		"My Yakovlev just out-turned your fighter. Your fighter is newer. My pilot grew up dodging traffic in Bogota. The streets are a better teacher.",
		"You built an airfield? Cute. We stole one. It works better. Because we also stole the maintenance crew. We pay them in not-prison.",
	},
	doubleTrouble={
		"Rosa here. My Yakovlevs scramble. Stolen Soviet surplus with Latin flames. %otherGen% scrambles too. Both scrambles are aimed at %player%. The flames make it faster. The stealing makes it cheaper. The %player% makes it TARGET PRACTICE.",
		"MiG-21 on approach! Stolen, refurbished, angry. %otherGen% is also approaching. Both approaches are angry. The anger counts for a lot in aerial combat. %player% is about to learn how much.",
		"Syndicate air doctrine: we stole the planes, we stole the tech, we're stealing %player%'s victory. With %otherGen% helping? We're stealing it TWICE. Theft. But make it aerial. And collaborative.",
	},
	tripleTrouble={
		"Three armies! My Yakovlevs scramble. %otherGen% scrambles. The third faction scrambles. All stolen. All angry. All aimed at %player%. The theft is aerial. The theft is collaborative. The theft is 'we stole %player%'s existence.'",
		"You built an airfield? Cute. We stole one. %otherGen% stole one too. The third faction... probably also stole one. Three stolen airfields. One %player%. The airfields work better. Because we stole the maintenance crews. We pay them in not-prison. %player% pays in 'everything.'",
	},
},
	{name="El Doctor Marcos Rivera", doctrine="science", taunts={
		"EMP Truck deploying. Your electronics just died. Your base is dark. Your defenses are offline. The Syndicate doesn't fight fair. We fight SMART. With stolen tech.",
		"Chemical weapons, loaded! Stolen from... somewhere. The label is in Cyrillic. I don't read Cyrillic. But the gas works. The gas doesn't need to read.",
		"Hacker unit, online! I just turned off your radar. And your power grid. And your bank account. The Syndicate fights with keyboards AND bullets. Mostly bullets. But the keyboards help.",
		"Stolen Tesla coil, deployed! We took it from the Soviets. They didn't need it anymore. Because we took it. The lightning is very pretty. The lightning is also very deadly. For you.",
		"My research division just combined Soviet Tesla tech with Allied Prism tech. The result is... unstable. But VERY explosive. The explosion is aimed at you. The instability is also aimed at you.",
		"Syndicate science doctrine: we steal it, we improve it, we use it against the people we stole it from. It's not irony. It's EFFICIENCY.",
		"Dirty bomb, armed! Stolen uranium, stolen casing, stolen detonator. Very recycled. Very green. Very DEADLY. The green is radiation. The radiation is for you.",
		"I have three PhDs. All stolen. Like our technology. The PhDs are in: Explosives, Chemical Warfare, and Applied Theft. I graduated summa cum laude. From a university I also stole.",
	},
	doubleTrouble={
		"El Doctor here. My EMP Truck just killed %player%'s power. %otherGen% is rolling in while the lights are out. The science is 'electromagnetic pulse.' The result is 'surprise attack in the dark.' Very Syndicate. Very effective.",
		"My Hacker just turned off %player%'s radar. %otherGen% is attacking from the blind spot. The science is 'cybersecurity.' The result is 'no security.' The 'no security' is for %player%. The 'cyber' is for us.",
		"Stolen Tesla coil, deployed! %otherGen% provides the ground assault. The coil provides the lightning. The ground assault provides the bullets. %player% provides the target. The target is 'destroyed.' Very recycled. Very final.",
	},
	tripleTrouble={
		"Three armies! My EMP kills %player%'s power. My Chemical weapons kill %player%'s infantry. My Hackers kill %player%'s radar. %otherGen% kills... everything else. The third faction kills whatever's left. I kill with SCIENCE. The science is stolen. The killing is original. Very Syndicate.",
		"Three PhDs, three armies, one target. My EMP, %otherGen%'s assault, the third faction's assault. All aimed at %player%. The science is 'combined arms.' The arms are 'combined.' The result is '%player% is gone.' I graduated summa cum laude in 'destroying %player%.' The university was stolen. The degree is real.",
	},
},
})

-- ==================== TIER 3 ====================

G("GDI Walker Column", {
	{name="General James Solomon", doctrine="tank", taunts={
		"Titans, march! Bipedal death machines. Yes, they walk. Yes, that's somehow scarier than wheels. Legs can go where wheels can't.",
		"Wolverines, deploy! Anti-infantry walkers with chainguns. They're like dogs. Very loyal. Very fast. Very bitey.",
		"Hover MLRS, online! All-terrain, anti-air, anti-ground. It hovers. It ruins your day. Simultaneously.",
		"GDI walker doctrine: we don't use tanks. We use LEGS. Because legs can step OVER your walls. And your mines. And your hopes.",
		"Titan just stepped on your tank. Not metaphorically. Literally. One foot. Your tank is now two-dimensional.",
		"My Wolverine just entered your infantry garrison. Through the wall. It didn't need the door. The wall is now a doorway.",
		"Hover MLRS just crossed your river. It didn't need a bridge. While shooting. At your base. From the river. Amateur.",
		"Titan walker says 'step aside.' It's not a suggestion. It's a giant bipedal threat with a cannon the size of your car.",
	},
	doubleTrouble={
		"Solomon here. My Titans march on %player% from one side. %otherGen% hits from the other. The Titans walk OVER %player%'s walls. %otherGen% walks THROUGH them. Either way: walls don't matter. %player% doesn't matter either.",
		"GDI walker doctrine: we use LEGS. With %otherGen% helping, we use legs AND whatever they use. Both are aimed at %player%. The legs step over walls. The 'whatever' probably goes through them. %player% is between. The 'between' is 'stepped on.'",
		"My Hover MLRS just crossed %player%'s river without a bridge. %otherGen% is crossing too, I assume. Both crossings are aimed at %player%. The river doesn't matter. The bridges don't matter. %player% doesn't matter.",
	},
	tripleTrouble={
		"Three armies! My Titans march. %otherGen% attacks. The third faction attacks. All three are aimed at %player%. The Titans step over walls. The others go through them. %player% has walls. The walls are decorative. The armies are NOT decorative.",
		"Titan just stepped on %player%'s tank. Literally. One foot. The tank is two-dimensional. %otherGen% is shooting what's left. The third faction is shooting what's left after that. There's nothing left. The Titans keep marching. They don't need targets. They just march.",
	},
},
	{name="Commander Michael McNeil", doctrine="infantry", taunts={
		"Light Infantry, deploy! Power armor, railgun technology, and an attitude problem. The attitude is standard issue.",
		"Disc Throwers, ready! Explosive frisbee. Yes, frisbee. Deadly, deadly frisbee. They throw it. It arcs. It explodes. You die.",
		"Drop Pods incoming! Infantry from ORBIT. Because walking is for people who aren't GDI. We drop from SPACE. Onto your HEAD.",
		"Disc Thrower says 'catch!' You won't. Because it explodes. The 'catch' is a formality. The explosion is the message.",
		"My Light Infantry just dropped from orbit. Into your base. From SPACE. Your radar points OUT. You should fix that. You won't have time.",
		"Drop Pod just landed in your Construction Yard. Five Light Infantry walked out. They're shooting. Everything is on fire. That's GDI infantry.",
		"You garrisoned a building. My Disc Thrower just arced a disc through the window. From way too far away. While moving.",
		"GDI infantry doctrine: we throw discs, we drop from orbit, and everything sticks. Because it's explosive.",
	},
	doubleTrouble={
		"McNeil here. My Drop Pods are incoming on %player% from ORBIT. %otherGen% is hitting from the ground. The pods land from space. %otherGen% lands from... the ground. Both landings are hostile. Both are aimed at %player%. The ground is crowded. The ground is EXPLODING.",
		"Disc Thrower says 'catch!' to %player%. They won't catch it. It explodes. %otherGen% is also throwing things, I assume. Everything thrown at %player% explodes. The throwing is coordinated. The exploding is VERY coordinated.",
		"My Light Infantry just dropped from orbit into %player%'s base. %otherGen% is at the front gate. The gate is a distraction. The orbit is the main event. The main event is 'space infantry in your base.' The subtitle is 'you lose.'",
	},
	tripleTrouble={
		"Three armies! Drop Pods from orbit. %otherGen% from the ground. The third faction from... somewhere. All three are in %player%'s base. The base is crowded. The crowd is hostile. The crowd is VERY hostile. The crowd is from SPACE.",
		"GDI infantry doctrine: we throw discs, we drop from orbit, everything sticks. With %otherGen% AND a third faction? Everything sticks MORE. The sticking is explosive. The explosive is permanent. For %player%. Very permanent.",
	},
},
	{name="Air Commander Chandra", doctrine="aircraft", taunts={
		"Orca Bombers, inbound! They're not subtle. They're not quiet. They're here to deliver. The delivery is bombs. The address is your face.",
		"Orca Fighters, scramble! They fight. They fly. They do both very well. Simultaneously. It's annoying. For you.",
		"Hammerhead gunships, deploy! It's a helicopter with a hammer. Metaphorically. The hammer is missiles.",
		"GDI air doctrine: three types of Orca, three types of pain. You'll experience all three. In sequence. Very quickly.",
		"Orca Bomber just dropped its payload on your War Factory. The War Factory is now a crater. The crater is on fire. I'm naming it after myself.",
		"Hammerhead inbound. It hovers. It shoots. It makes very loud noises. Your troops should be covering their BODIES. With armor. It won't help.",
		"My Orca squadron just did a flyover. They weren't shooting. They were taking notes. Drawing a map. Circling targets. NOW they're coming back.",
		"You built anti-air. My Orca Bombers fly higher than your anti-air can reach. The bombs are coming DOWN. The math is bad. For you.",
	},
	doubleTrouble={
		"Chandra here. My Orca Bombers are inbound on %player%. %otherGen% is providing the ground assault. The bombs come from above. %otherGen% comes from... the side, I assume. Both directions are covered. %player% is NOT covered.",
		"GDI air doctrine: three types of Orca, three types of pain. With %otherGen% adding ground forces? Four types of pain. %player% will experience all four. In sequence. Very quickly. Very painfully.",
		"My Hammerhead is hovering over %player%'s base. It's shooting. %otherGen% is also shooting, I assume. Both shootings are aimed at %player%. The hovering is persistent. The shooting is persistent. %player% is NOT persistent. %player% is temporary.",
	},
	tripleTrouble={
		"Three armies! My Orcas fly. %otherGen% drives. The third faction does... whatever. All three are aimed at %player%. The sky is mine. The ground is %otherGen%'s. The 'whatever' is the third faction's. %player% has nothing. Nothing is what's left.",
		"Orca Bomber just dropped its payload on %player%'s War Factory. The Factory is a crater. %otherGen% is bombing the crater. The third faction is bombing the crater. The crater is getting bombed. There's nothing left to bomb. They're bombing the BOMB CRATER. Very thorough. Very air force.",
	},
},
	{name="Dr. Sydney Moen", doctrine="science", taunts={
		"Juggernauts, deploy! Mobile artillery on three legs. Yes, three legs. Because two is unstable and four is wasteful. Three is OPTIMAL. The math is mine. The shells are yours.",
		"Ion Cannon charging. From ORBIT. Your base is visible from space. Very visible. The cannon doesn't need to 'find' you. It already found you. It's just charging.",
		"Sonic Tanks, advance! They fire concentrated sound waves. Your armor vibrates. Then it cracks. Then it shatters. The sound is very loud. And very final.",
		"GDI science doctrine: we don't just build weapons. We DESIGN them. With computers. And PhDs. Your weapons are built with... enthusiasm. The PhDs win.",
		"Juggernaut just shelled your base from beyond visual range. The shells took 5 seconds to arrive. You spent those 5 seconds wondering where they'd land. They landed on your War Factory. The wondering is over.",
		"My Sonic Tank just fired at your infantry. The sound wave hit them. They're... vibrating. Very fast. Very painfully. The vibration is not survivable. The sound is not optional.",
		"Ion Cannon online. Target: your Construction Yard. The beam is traveling at the speed of light. Your evacuation is traveling at the speed of 'oh no.' The light is faster. The light is ALWAYS faster.",
		"Drop Pod strike called. Infantry from orbit. With railguns. They land in your base. They shoot. They don't ask questions. The questions are not in their programming. The shooting IS.",
		"Dr. Moen here. I have three PhDs. None of them are in 'losing.' They're in Physics, Engineering, and Applied Destruction. The destruction is applied. To you. Right now.",
	},
	doubleTrouble={
		"Moen here. My Juggernauts shell %player% from beyond visual range. %otherGen% provides the ground assault. The shells come from above. The assault comes from the front. %player% is between. The 'between' is 'destroyed.' Very scientific. Very GDI.",
		"Ion Cannon charging! Target: %player%'s base. %otherGen% is also charging, I assume. Both are aimed at %player%. The beam is from orbit. The 'whatever' is from the ground. Both are ' %player% is gone.' The 'gone' is from a PhD. The PhD is mine.",
		"My Sonic Tanks fire concentrated sound at %player%'s armor. The armor cracks. %otherGen% is cracking the rest. The cracking is 'comprehensive.' The comprehensive is ' %player% is destroyed.' The 'destroyed' is very loud. And very final.",
	},
	tripleTrouble={
		"Three armies! My Juggernauts shell from beyond range. My Ion Cannon fires from orbit. %otherGen% attacks from the ground. The third faction attacks from... wherever. All three are aimed at %player%. The science is 'combined arms with orbital support.' The arms are combined. The support is from SPACE. The 'from space' is ' %player% is gone.' Very PhD. Very permanent.",
		"Ion Cannon, Juggernauts, Sonic Tanks, and Drop Pods. All my designs. All aimed at %player%. %otherGen% adds their forces. The third faction adds theirs. I add SCIENCE. The science says 'destroyed.' The destruction says 'yes, doctor.' %player% says nothing. %player% is GONE. Very precise. Very Moen.",
	},
},
})

G("Nod Shadow Legion", {
	{name="Brother Anton Slavik", doctrine="infantry", taunts={
		"Light Infantry, deploy! Fanatical zeal. They charge. They die. More charge. It's a system. The system works.",
		"Rocket Infantry, aim! Your walkers are about to have a very bad day. A very EXPLOSIVE bad day.",
		"Cyborg Infantry in the field. Half man, half machine, all Nod. The human half feels hatred. The machine half feels NOTHING.",
		"Nod infantry doctrine: we are many, we are fanatical, and we regenerate in Tiberium. You don't. You DIE in Tiberium. Advantage: Nod.",
		"My Cyborg just took three rounds to the chest. The human half is damaged. The machine half is FINE. And STILL SHOOTING.",
		"Rocket Infantry just fired at your Titan. The rocket hit the leg. The leg buckled. The Titan fell. On your infantry. Two kills, one rocket.",
		"You killed my Light Infantry. Good for you. There are more. There are ALWAYS more. Kane provides.",
		"My Cyborg doesn't eat. Doesn't sleep. Doesn't feel fear. Does feel hatred. I programmed the hatred myself. It's my best work.",
	},
	doubleTrouble={
		"Slavik here. My Cyborg Infantry charge %player%. %otherGen% attacks from the other side. The Cyborgs are half man, half machine. The human half feels hatred. The machine half feels NOTHING. %otherGen% feels... whatever. Both halves are aimed at %player%.",
		"Nod infantry doctrine: we are many, we are fanatical, we regenerate in Tiberium. With %otherGen% helping? We are MORE many. %player% is not regenerating. %player% is dying. The Tiberium doesn't care about %player%. It cares about US.",
		"My Cyborg just took three rounds from %player%. The human half is damaged. The machine half is FINE. %otherGen% is flanking %player% while they focus on my Cyborg. The flanking is successful. The Cyborg is still shooting. Everyone is shooting EXCEPT %player%.",
	},
	tripleTrouble={
		"Three armies! My Cyborgs charge. %otherGen% charges. The third faction charges. All charges are fanatical. All charges are aimed at %player%. The Cyborgs don't sleep. The others don't sleep either, I assume. Nobody sleeps. %player% especially. %player% is too busy dying.",
		"Kane provides. With three armies, Kane provides THREE TIMES. My Cyborgs, %otherGen%'s forces, and a third faction. All provided by Kane. All aimed at %player%. The provision is generous. The aiming is precise. The result is ' %player% is gone.' PEACE THROUGH POWER.",
	},
},
	{name="Brother Jacob", doctrine="tank", taunts={
		"Attack Cycles, full speed! Fastest thing on the battlefield. You can't hit what's moving that fast.",
		"Tick Tanks, deploy! They dig in. They become FORTIFIED. Your tanks can't dig. Your tanks are just... there. Exposed. Sad.",
		"Stealth Tanks decloaking! You didn't see them. That's the last thing you won't see.",
		"Nod armor doctrine: speed, stealth, and siege mode. Pick all three. We have all three. You have none.",
		"Attack Cycle just drove through your base. It was just SCOUTING. The Stealth Tanks behind it? Those are NOT scouting. Those are DECLOAKING.",
		"Tick Tank just deployed into the ground. It's shooting your base from maximum range. Your tanks can't reach it. It's a turtle with a CANNON.",
		"Stealth Tank just decloaked next to your Power Plant. BOOM. Recloaked. Gone. Where is it? Next to your OTHER Power Plant.",
		"You're chasing my Attack Cycle. It's faster than you. It's faster than your BULLETS. You're chasing something you can't catch. That's a hobby.",
	},
	doubleTrouble={
		"Jacob here. My Stealth Tanks decloak beside %player%'s Power Plant. %otherGen% is hitting the front. The Stealth Tanks hit from... nowhere. They were nowhere. Now they're everywhere. %otherGen% is also everywhere. %player% is surrounded by 'everywhere.'",
		"Nod armor doctrine: speed, stealth, and siege mode. With %otherGen% adding their forces? Speed, stealth, siege, AND whatever %otherGen% does. All four are aimed at %player%. The 'whatever' is probably also violent.",
		"My Tick Tank just deployed into the ground near %player%'s base. It's shooting from maximum range. %otherGen% is shooting from... their range. Both ranges overlap at %player%. The overlap is 'destroyed.' The overlap is VERY destroyed.",
	},
	tripleTrouble={
		"Three armies! My Stealth Tanks decloak. %otherGen% attacks. The third faction attacks. All three were invisible, silent, or fast. Now all three are visible, loud, and HERE. %player% didn't see any of them coming. That's the last thing %player% won't see.",
		"Attack Cycle just drove through %player%'s base at full speed. %otherGen% is driving through too. The third faction is... also driving through, presumably. The base is a highway. The highway has no speed limit. The highway has three armies. All heading toward %player%. The destination is 'destroyed.'",
	},
},
	{name="Sister Vega", doctrine="aircraft", taunts={
		"Harpy gunships, inbound! They're called 'Harpy' because they're loud, aggressive, and steal your things. By exploding them.",
		"Banshee Fighters, scramble! Plasma weapons. Alien technology. And a name that makes your pilots uncomfortable. They SHOULD be.",
		"Nod air doctrine: we don't need many planes. We need SCARY planes. The Banshee is very scary. The Harpy is very annoying.",
		"Harpy just did a strafing run on your infantry. They're gone. All of them. The Harpy didn't even slow down.",
		"Banshee inbound. It uses plasma. Your air defense uses bullets. Plasma is hotter than bullets. Your air defense is about to learn this. Briefly.",
		"Banshee Fighter: alien technology, zero chill, and a pilot who prays to Kane before every mission. Kane answers. The answer is 'plasma.'",
		"My Harpy squadron just circled your base. Like vultures. Because they ARE vultures. Technically. Spiritually. Theologically.",
		"You launched a fighter to intercept my Banshee. Your fighter has bullets. My Banshee has plasma. Your fighter is brave. And on fire.",
	},
	doubleTrouble={
		"Vega here. My Banshees scramble with plasma weapons. %otherGen% is scrambling too. Both scrambles are aimed at %player%. The plasma is hotter than bullets. %otherGen% is... also hot, I assume. Everything is hot. Except %player%. %player% is about to be VERY hot. And on fire.",
		"Nod air doctrine: we don't need many planes. We need SCARY planes. With %otherGen% adding their planes? We have scary AND more scary. %player% is scared. The fear is justified. The plasma is coming.",
		"My Harpy just did a strafing run on %player%'s infantry. They're gone. %otherGen% is handling the tanks, I assume. The infantry are gone. The tanks are next. The 'next' is NOW.",
	},
	tripleTrouble={
		"Three armies! My Banshees fly. %otherGen% flies. The third faction flies. All three fly toward %player%. The sky is full. The sky is hostile. The sky is VERY hostile. %player% looks up. The sky says 'plasma.' The sky means it.",
		"Banshee inbound on %player%. Alien technology. Zero chill. %otherGen% is also inbound. The third faction is also inbound. Three inbound forces. One %player%. The math is '3 > 1.' The math is always '3 > 1.' The math is ' %player% loses.'",
	},
},
	{name="Oxanna Kristos", doctrine="stealth", taunts={
		"Shadow Team deploying. They're invisible. They're behind you. They were there BEFORE you started reading this. The stealth is not technology. The stealth is FAITH. And cloaking fields. Mostly cloaking fields.",
		"Chameleons active! Spy units that look like YOUR units. That tank over there? Mine. That harvester? Also mine. The stealth is so good even I forget which units are mine. The confusion is DELIBERATE.",
		"Subterranean APC surfacing! It went UNDER your base. Under your walls. Under your defenses. It's now in the middle of your base. The doors are opening. The infantry are smiling. The smiling is unsettling.",
		"Nod stealth doctrine: you can't kill what you can't see. You can't see most of my army. The part you CAN see is a distraction. The part you CAN'T see is behind you. Right now.",
		"My Shadow Team just planted charges on your Power Plant. You didn't see them. You won't see them leave. You WILL see the explosion. The explosion is very visible. The team is not.",
		"Chameleon just scouted your entire base. It looks like your own harvester. It's been driving around for 5 minutes. You waved at it. It waved back. It's reporting everything to ME. The waving was a mistake.",
		"Subterranean Flamer just surfaced in your infantry garrison. FROM UNDERGROUND. Your infantry are on fire. From below. The below is Nod. The Nod is everywhere. Especially below.",
		"Oxanna here. I don't just fight. I BROADCAST. Every Nod victory is on the news. Every GDI defeat is on the news. The news is MINE. The truth is MINE. Your destruction will be on the news. The ratings will be EXCELLENT.",
	},
	doubleTrouble={
		"Oxanna here. My Shadow Teams infiltrate %player%'s base. Invisible. %otherGen% attacks from the front. The front is loud. The stealth is quiet. %player% watches the front. The stealth is behind them. The 'behind them' is 'destroyed.' Very Nod. Very quiet.",
		"My Subterranean APC surfaces in %player%'s base. From underground. %otherGen% is at the front gate. The gate is a distraction. The underground is the main event. The main event is 'explosions from below.' The 'from below' is Nod.",
		"Nod stealth doctrine: you can't kill what you can't see. With %otherGen%? You can't see ME, and %otherGen% is at your front. Both are aimed at %player%. The 'can't see' is behind %player%. The 'at your front' is in front. %player% is between. The 'between' is 'destroyed.' Very broadcast-worthy.",
	},
	tripleTrouble={
		"Three armies! My Shadow Teams infiltrate. %otherGen% attacks. The third faction attacks. All three are aimed at %player%. The stealth is invisible. The others are visible. %player% can see two armies. %player% can't see the third. The third is ME. The third is the one that plants charges. The charges are ' %player% is gone.' The 'gone' is on the news. The ratings are EXCELLENT.",
		"My Chameleon scouted %player%'s entire base. It looks like %player%'s own unit. %otherGen% is attacking from the front. The third faction is attacking from the side. The Chameleon is attacking from INSIDE. The 'inside' is ' %player% is destroyed.' The 'destroyed' is broadcast live. On Nod News Network. The ratings are RECORD-BREAKING.",
	},
},
})

G("Naxis War Machine", {
	{name="Generalfeldmarschall Krause", doctrine="tank", taunts={
		"Panzer, vorwaerts! Beautiful German engineering. It breaks down sometimes. But not TODAY. Today it is ANGRY.",
		"Tiger Panzer, einsetzen! Big cat. Big gun. Big problem. For you. For me it's a SOLUTION.",
		"Koenigstiger Heavy Tank, incoming! It's the Tiger but ROYAL. And by royal I mean MORE gun. And MORE armor.",
		"Naxis Panzer doctrine: heavy armor, heavy guns, heavy everything. We don't do 'light.' We do 'INEVITABLE.' Slowly. But inevitably. Like German bureaucracy. But with MORE EXPLOSIONS.",
		"Panzer just crossed your perimeter. Your anti-tank fired. The shell BOUNCED. The Panzer didn't notice. The Panzer NEVER notices.",
		"Koenigstiger just entered your base. Your tanks are firing. The shells are bouncing off the frontal armor. The Koenigstiger is shooting back. The shooting is NOT bouncing.",
		"My Tiger just destroyed your Medium Tank. One shot. Frontal penetration. Your Medium Tank is now a Medium Krater. Jawohl.",
		"Koenigstiger rolls through your defenses. Your mines detonate. The Koenigstiger doesn't notice. Your walls crumble. The Koenigstiger doesn't notice. Your troops flee. The Koenigstiger DOES notice. It shoots them. German engineering: thorough.",
		"You think you can outmaneuver my Koenigstiger? You're right. It's slow. But it doesn't NEED to be fast. It just needs to arrive. And it WILL arrive. Like the Wehrmacht. But angrier.",
	},
	doubleTrouble={
		"Krause here. My Koenigstiger rolls toward %player%. Slowly. Inevitably. %otherGen% is also rolling. Faster, maybe. But my Panzer is HEAVIER. Both are aimed at %player%. The heavy arrives. The fast arrives. %player% departs. Permanently.",
		"Naxis Panzer doctrine: heavy armor, heavy guns, heavy everything. With %otherGen%? Double the heavy. %player% has light. Light loses to heavy. Always. The physics is German. The physics is CORRECT.",
		"My Tiger just destroyed %player%'s tank. One shot. Frontal penetration. %otherGen% is destroying the rest. The rest is... everything. Everything is being destroyed. By Germans. And %otherGen%. Jawohl.",
	},
	tripleTrouble={
		"Three armies! My Koenigstiger arrives slowly. %otherGen% arrives faster. The third faction arrives... somehow. All three arrive at %player%. The arrival is certain. Like German bureaucracy. But with MORE EXPLOSIONS. Three times the explosions.",
		"Drei Armeen! Mein Koenigstiger, %otherGen%, und eine dritte Fraktion. Alle rollen. Alle schiessen. %player% rollt nicht. %player% ist zerstoert. Jawohl. The jawohl is: PERMANENT.",
	},
},
	{name="Oberst Heinrich Wolf", doctrine="infantry", taunts={
		"Schuetzen, einsetzen! Disciplined, precise, and wearing very tidy uniforms. A soldier who looks gut fights gut. That is German philosophy.",
		"SS Soldaten, voran! Elite infantry with more training than sense. They charge Panzer. On foot. And sometimes WIN.",
		"Untote Krieger, rising! Occult science is REAL science. The dead agree. Especially the dead ones. They're very cooperative. Very German. Very DEAD.",
		"Naxis infantry doctrine: discipline, occult, and questionable ethics. But GREAT uniforms. Morale is mostly uniforms. Some discipline. A bit of occult ritual. The formula is German. The formula is CORRECT.",
		"My Schuetze just took a position your infantry abandoned. They abandoned it because my Artillerie was shelling it. The irony is not lost on me. It is very German. We appreciate irony. And artillery.",
		"Untote Krieger just rose from the ground. In your base. From the GROUND. Your troops are looking at the ground. The ground has swords. And bad intentions. In German we call that 'eine boese UEberraschung.'",
		"My SS Soldaten just charged your Panzer. On foot. Your Panzer fired. Three fell. The rest kept coming. They opened the hatch. The Panzer is now MY Panzer. Danke for the donation.",
		"You killed my Untoter Krieger. He got back up. You killed him again. He got up again. Your weapon is overheating. His patience is infinite. That is the beauty of the Untote. They are very persistent. Very German.",
		"Achtung! Dein Bunker ist mein Bunker. Deine Waffen sind meine Waffen. Es ist Obst im Haus! Und das Obst ist eine Panzerfaust!",
		"Your perimeter is a suggestion. My Schuetzen treat suggestions as invitations. The invitation is accepted. The RSVP is 'we're already inside.' Very rude. Very German. Very effective.",
	},
	doubleTrouble={
		"Wolf here. My Schuetzen advance on %player% with discipline and tidy uniforms. %otherGen% advances too. Less tidy, I assume. But both advances are precise. Both are aimed at %player%. The precision is German. The result is ' %player% is gone.'",
		"My Untote Krieger rise from the ground in %player%'s base. %otherGen% is at the front. The dead are at the back. The back is underground. The underground is GERMAN. %player% is surrounded by Germans and the dead. The dead are also German. Everyone is German. Except %player%. %player% is dead.",
		"Naxis infantry doctrine: discipline, occult, and questionable ethics. With %otherGen%? Double the ethics. Still questionable. But VERY effective. %player% is questioning their life choices. The questioning is brief. The answer is 'explosions.'",
	},
	tripleTrouble={
		"Three armies! My Untote Krieger rise. %otherGen% attacks. The third faction attacks. The dead are German. The living are... also German, I assume. Everyone is aimed at %player%. %player% is surrounded by the dead and three armies. The dead don't stop. The armies don't stop. %player% stops. Permanently.",
		"Ich bin in %player%s Basis, toete %player%s Typen. %otherGen% ist auch da. Die dritte Fraktion ist auch da. Alle toeten. German efficiency. Three armies. One target. The target is 'gone.' Jawohl.",
	},
},
	{name="Luftwaffe Kommandant Richter", doctrine="aircraft", taunts={
		"Abfangjaeger, scramble! It's fast. It's aggressive. It's VERY Naxis. Everything about it is aggressive. Including the paint job. We painted it with spite. German spite.",
		"BF-109 on approach! Classic WWII fighter. Still fighting. Still winning. Your pilots are losing to a plane from 1940. The Demuetigung is INTENTIONAL.",
		"ME-262, einsetzen! First jet fighter in history. History is on my side. LITERALLY. Its time is NOW. Your time is UP. Wunderwaffe, Jawohl!",
		"Naxis Luftwaffe doctrine: expendable Abfangjaeger, classic fighters, and a JET. From 1945. In your face. TODAY. The anachronism is the POINT. The point is 'we had jets before you had computers.'",
		"Abfangjaeger just entered your airspace. Your radar identified it as 'obsolete.' It is. It's also shooting your Harrier. Obsolete wins. Sometimes. Today is 'sometimes.'",
		"ME-262 inbound. It's a jet. From 1945. Your air defense is from... whatever year. The jet is faster. The jet is ALWAYS faster. Schnell ist besser.",
		"BF-109 just out-turned your modern fighter. Your fighter has fly-by-wire. The BF-109 has a pilot with PURE SPITE. Spite turns tighter. German spite turns tightest.",
		"My Abfangjaeger just did a Sieg roll over your base. It was unnecessary. It was also deserved. Your anti-air couldn't track it. Sieg. Very satisfying.",
		"Your radar operator just spotted my ME-262. He blinked. Then he prayed. Then the radar stopped working. The prayer didn't help either. Jets don't care about prayer. German jets care about SPEED.",
	},
	doubleTrouble={
		"Richter here. My ME-262 is inbound on %player%. From 1945. %otherGen% is also inbound. From... whatever year. Both are aimed at %player%. The jet is faster. The jet is ALWAYS faster. Schnell ist besser. For us.",
		"Naxis Luftwaffe doctrine: expendable Abfangjaeger, classic fighters, and a JET. With %otherGen%? Expendable Abfangjaeger, classic fighters, a jet, AND whatever %otherGen% flies. All aimed at %player%. The anachronism is the POINT. The point is ' %player% loses to 1945 technology.' Demuetigung.",
		"My BF-109 just out-turned %player%'s modern fighter. Spite turns tighter. %otherGen% is also turning, I assume. Both turns are aimed at %player%. The spite is German. The turning is German. The result is ' %player% is on fire.'",
	},
	tripleTrouble={
		"Three armies! My ME-262 flies. %otherGen% flies. The third faction flies. All three fly toward %player%. The jet is from 1945. The others are from... whenever. All are faster than %player%'s defenses. The defenses are from 'too slow.' The defenses lose. Schnell ist besser. Always.",
		"Three air forces, one sky. My ME-262 owns the high altitude. %otherGen% owns the low. The third faction owns... whatever's left. What's left is %player%'s airspace. %player%'s airspace is now OUR airspace. The airspace is GERMAN. The German is COMPREHENSIVE. The comprehensive is BOMBING.",
	},
},
	{name="Doktor Ernst Schaefer", doctrine="science", taunts={
		"V-2 Raketen, launch! The world's first ballistic missile. It's from 1944. It still works. Your anti-missile is from... whenever. The V-2 doesn't care about 'whenever.' The V-2 cares about 'arriving.' It's arriving. NOW.",
		"Tesla Generator online! Lightning weapons. From a Serbian scientist. Stolen by Germans. Improved by ME. The lightning is very German now. Very precise. Very DEADLY.",
		"Naxis science doctrine: occult, experimental, and VERY unstable. The instability is a feature. The feature is 'it might explode.' The 'might' is 'probably.' The 'probably' is 'definitely.' For you.",
		"Wunderwaffe program active! We combine science with... other things. The 'other things' are classified. The science is not. The science is 'your base explodes.' The classified part is 'how.' You'll never know. You'll be dead.",
		"My V-2 just hit your Construction Yard. From across the map. The rocket was supersonic. You didn't hear it coming. You heard it arriving. The arriving was loud. Briefly. Then silence. German silence.",
		"Tesla Coil just discharged into your tank column. The lightning jumped from tank to tank. Chain lightning. Very efficient. Very German. The efficiency is measured in 'tanks destroyed per bolt.' The number is HIGH.",
		"Occult science division reporting. We found something in the Hollow Earth. It's big. It's angry. It's coming through the portal. The portal is in your base. I opened it there. You're welcome. And doomed.",
		"Doktor Schaefer here. I have three PhDs. In Physics, Chemistry, and Applied Occultism. The 'Applied Occultism' is not accredited. I accredited it myself. From the Hollow Earth. The Hollow Earth university is very prestigious. And very angry. At you.",
	},
	doubleTrouble={
		"Schaefer here. My V-2 rockets launch at %player% from across the map. %otherGen% attacks from the ground. The rockets come from above. The ground assault comes from the front. %player% is between. The 'between' is 'destroyed.' Very scientific. Very German.",
		"My Tesla Generator discharges into %player%'s tank column. Chain lightning. %otherGen% is providing the ground assault. The lightning is from science. The assault is from... conventional weapons. Both are aimed at %player%. The science is deadlier. The science is GERMAN.",
		"Naxis science doctrine: occult, experimental, and VERY unstable. With %otherGen%? Occult, experimental, unstable, AND whatever %otherGen% brings. All aimed at %player%. The 'whatever' is probably stable. How boring. The unstable is more fun. The fun is ' %player% is destroyed.'",
	},
	tripleTrouble={
		"Three armies! My V-2 rockets launch. My Tesla Coils discharge. %otherGen% attacks. The third faction attacks. All four -- I mean three -- are aimed at %player%. The science is 'ballistic missiles plus lightning plus two armies.' The equation is unstable. The result is ' %player% is gone.' Very PhD. Very occult. Very permanent.",
		"V-2, Tesla, Occult Portal, and Wunderwaffe. All my designs. All aimed at %player%. %otherGen% adds their forces. The third faction adds theirs. I add SCIENCE. The science is 'explosive, electric, and eldritch.' The 'eldritch' is from the Hollow Earth. The 'explosive' is from 1944. The 'electric' is from Tesla. All three are from ME. The 'from me' is ' %player% is GONE.' Very Schaefer. Very final.",
	},
},
})

G("Schwarzer Mond", {
	{name="Kommandant Luna von Falken", doctrine="infantry", taunts={
		"Lunar Soldaten, einsetzen! Space-trained infantry. They fight on the MOON. You can't even fight on EARTH. The skill gap is... astronomisch.",
		"UEbermensch heavy infantry, incoming! Yellow laser rifles. Because regular lasers weren't intimidating enough. The yellow is MACHT. The yellow is ALWAYS German.",
		"Schwarzer Mond infantry doctrine: space training, lunar alloys, and very cool uniforms. Schwarz goes with everything. Especially destruction. Especially VERnichtung.",
		"My Lunar Soldat just took three rounds to the chest. His lunar alloy armor stopped them. He didn't flinch. He's not MENSCHLICH enough to flinch.",
		"UEbermensch just fired his laser rifle at your Panzer. The beam hit the ammo rack. The ammo rack disagreed with the beam. The disagreement was explosive. Very German. Very thorough. Very final.",
		"Lunar Raketen, aim! Anti-tank missiles from SPACE. Well, from the ground. But space-TRAINED. The training is EVERYTHING. The training is GERMAN.",
		"My Lunar Soldat says 'fuer den Mond!' He's been to the moon. You haven't. Nobody you know has. And he's here. To kill you. Fuer den Mond. Fuer den Sieg.",
		"UEbermensch deployed. Yellow laser. Heavy armor. Zero mercy. He doesn't shake hands. He shakes BUILDINGS. Until they fall down. Like German engineering. But in REVERSE.",
		"The Hollow Earth is real. Agartha is real. The Vril are real. And they all work for US. We have a portal in Antarctica. Your scouts found it. They didn't come back. The Vril don't like visitors.",
		"Flat Earth? Don't make me laugh. The Earth is HOLLOW. And inside it, there's a sun. And inside THAT sun, there's a German. With a bigger gun. It's turtles all the way down, but the turtles are German.",
		"EXTERMINATE! EXTERMINATE! ...Sorry, I've been watching old Doctor Who episodes on the moon base. The Daleks had the right idea. Just less German. We fixed that. Our Daleks have Panzer armor.",
		"You know what's on the dark side of the moon? US. You know what's under the ice in Antarctica? Also us. You know what's at the center of the Earth? Also us. We're everywhere. We're German. We're THOROUGH.",
	},
	doubleTrouble={
		"Luna here. My Lunar Soldaten advance on %player% with space training and lunar alloys. %otherGen% advances too. From Earth, I assume. How quaint. Both advances are aimed at %player%. The space training is GERMAN. The Earth training is... adequate. Both are deadly.",
		"My UEbermensch just fired his yellow laser at %player%'s tank. The beam hit the ammo rack. %otherGen% is handling the rest. The rest is 'everything.' Everything is exploding. The explosions are German. The German is thorough. The thorough is FINAL.",
		"Schwarzer Mond infantry doctrine: space training, lunar alloys, and very cool uniforms. With %otherGen%? Space training, lunar alloys, cool uniforms, AND whatever %otherGen% brings. All aimed at %player%. The 'whatever' is probably not from space. How sad for them.",
	},
	tripleTrouble={
		"Three armies! My Lunar Soldaten advance from the Mond. %otherGen% advances from Earth. The third faction advances from... somewhere. All three are aimed at %player%. We're everywhere. The Mond, the Earth, the somewhere. %player% is nowhere. The nowhere is 'destroyed.'",
		"The Hollow Earth is real. Agartha is real. The Vril are real. And they all work for us. With %otherGen% AND a third faction? The Vril, the Germans, %otherGen%, and a third army. All aimed at %player%. %player% is surrounded by Germans, space Germans, underground Germans, and two other armies. The surrounded is 'very surrounded.' The surrounded is FINAL.",
	},
},
	{name="Generalfeldmarschall Erich von Mond", doctrine="tank", taunts={
		"Lunar Panthers, voran! Space armor. It's like a regular Panzer but it's been to the MOON. Your Panzer hasn't been anywhere interesting. Your Panzer is boring. Like your strategy.",
		"Laser Panzer, online! It shoots LASERS. From a PANZER. Your Panzer shoots shells. Shells are so 1945. Lasers are so NOW. Lasers are so GERMAN.",
		"Schwarzer Mond Panzer doctrine: lunar alloys, laser weapons, and Panzer that have seen the dark side of the moon. LITERALLY. The dark side is German. We feel at home there.",
		"Lunar Panther just crossed your perimeter. Your anti-tank fired. The shell bounced off the lunar alloy. The Panther is already shooting. The shooting is NOT bouncing. The shooting is very German. Very precise. Very deadly.",
		"Laser Panzer firing! A concentrated beam of coherent light. At your Panzer. Your Panzer is now glowing. Then melting. Then gone. Wie geplant. As planned.",
		"My Lunar Tiger just entered your base. It's bigger than your Panzer. It's shinier. It's been to SPACE. Your Panzer has been to a factory. The Tiger wins. The Tiger ALWAYS wins. Es ist ein deutscher Tiger.",
		"You're trying to flank my Laser Panzer. The turret rotates 360 degrees. The laser shoots in ALL directions. Flanking doesn't apply to lasers. Flanking doesn't apply to GERMANS.",
		"Lunar Panther: schnell, armored, and it's been to space. Your Panzer's greatest achievement is crossing a river. The comparison is not flattering. The comparison is GERMAN. We don't flatter. We VERnichten.",
		"Remember the Goetterdaemmerung? The battleship from Iron Sky? That was a DOCUMENTARY. We just told them it was fiction so you wouldn't panic. You're panicking now. Good. The Goetterdaemmerung is coming. It's bigger than the movie. We had a bigger budget.",
		"The Vril gave us this technology. The Vril are from inside the Earth. They're very tall, very blonde, and very ANGRY. They don't like surface dwellers. They like us because we're German. The Vril are also German. Everyone good is German. It's just science.",
		"Operation Highjump? Admiral Byrd? 1947? He found us. He found our moon base. He turned around. He NEVER talked about it. Because we told him not to. Politely. With lasers. German politeness is very persuasive.",
	},
	doubleTrouble={
		"Von Mond here. My Lunar Panthers roll toward %player% with lunar alloy armor. %otherGen% rolls too. With Earth armor, I assume. How quaint. Both are aimed at %player%. The lunar alloy bounces shells. The Earth armor... probably also bounces shells. Both bounce. %player% does not bounce. %player% shatters.",
		"My Laser Panzer fires a concentrated beam at %player%'s tank. The tank is melting. %otherGen% is melting the rest. The rest is 'everything.' Everything is melting. The melting is German. The German is PRECISE. The precise is ' %player% is gone.'",
		"Schwarzer Mond Panzer doctrine: lunar alloys, laser weapons, and Panzer that have seen the dark side of the moon. With %otherGen%? Lunar alloys, lasers, moon Panzer, AND whatever %otherGen% drives. All aimed at %player%. The 'whatever' hasn't been to space. How sad.",
	},
	tripleTrouble={
		"Three armies! My Lunar Panthers roll. %otherGen% rolls. The third faction rolls. All three roll toward %player%. The Panthers have been to space. The others have been to... Earth. How quaint. All three are deadly. The space ones are DEADLIER. The deadly is ' %player% is destroyed.'",
		"Remember the Goetterdaemmerung? The battleship from Iron Sky? That was a documentary. With three armies, it's a BIGGER documentary. The budget is larger. The cast includes %otherGen% and a third faction. The plot is ' %player% is destroyed.' The reviews are excellent. The sequel is ' %player% is still destroyed.'",
	},
},
	{name="Raumflotte Kommandant Helga", doctrine="aircraft", taunts={
		"Drohnen fighters, einsetzen! Expendable, relentless, and from SPACE. Your anti-air is from EARTH. Earth loses. To the Mond. Again. Wie immer.",
		"Haunebu-II inbound! It's a flying saucer. Yes, a UNTERSETTLER. From the Mond. You're being invaded by German moon aliens. This is the weirdest timeline. For you. For me, it's MONTAG. For me, it's every day.",
		"Schwarzer Mond Luftwaffe doctrine: flying saucers, space Drohnen, and meteors. Yes, METEORS. From space. To your base. You can't defend against GEOLOGY. Or GERMANS. Especially German geology.",
		"Drohne just entered your airspace. Your radar identified it as 'unidentified flying object.' It IS. And it's shooting your Harrier. The UFO is German. The UFO is ANGRY.",
		"Haunebu-II on approach. It's a UFO. A real one. Well, a German one. From the Mond. With lasers. Your pilot is questioning his entire reality. While being shot. Welcome to German science. It is very thorough.",
		"Corruptor Piercers, fire! The name is terrifying. The weapon is worse. I don't know what it corrupts. Everything, apparently. Very German. We are thorough. Even in corruption.",
		"My Drohne just did a loop around your base. It was scanning. For weaknesses. It found seventeen. I only need einen. One. One is enough. One is German efficiency.",
		"You're fighting flying saucers. From the Mond. Piloted by Germans. With lasers. Take a moment and appreciate how absurd this is. My Haunebu-II will wait. Germans are patient. Germans are thorough. Germans are HERE.",
		"Sie haben keine Verteidigung gegen Haunebu. Your air defense was designed for planes. For ROCKETS. Not for saucers that ignore physics. The Haunebu ignores physics. The Haunebu ignores YOU.",
		"The Schwarzer Sonne is not a symbol. It's a GATEWAY. We opened it. Something came through. Something LARGE. Something that doesn't like your base. The 'doesn't like' is mutual. The 'large' is an understatement. The understatement is German.",
		"The Moon Fuehrer sends his regards. Wolfgang Kortzfleisch is watching from the dark side. He's been watching since 1945. He's very patient. He's very German. And he just gave the order. The order is: everything. Everything attacks.",
		"You know the Black Sun? The symbol? It's not just a symbol. It's a MAP. To the center of the Earth. Where the Vril live. Where WE come from. Your base is on the surface. The surface is the WORST place to be. Everything below you is German. Everything above you is German. You're in the middle. Of Germans.",
		"Vril energy flows through every Haunebu. Through every Laserbeetle. Through every Ubermensch. The Vril is the power of the Hollow Earth. Your power comes from generators. Generators break. Vril doesn't break. Vril doesn't STOP. The 'doesn't stop' is 'your base is gone.' The 'gone' is PERMANENT. The permanent is GERMAN.",
		"Reptilians? Yes, we know about them. They live in the Hollow Earth too. Hitler is a reptilian. Margaret Thatcher was a reptilian. We don't talk about it. It's embarrassing. Even for us. But the Vril energy is REAL and it powers every Haunebu we have. Your anti-air can't shoot down Vril. Vril doesn't care about your anti-air.",
	},
	doubleTrouble={
		"Helga here. My Haunebu-II inbound on %player%. It's a flying saucer. From the Mond. With lasers. %otherGen% is also inbound. With... Earth aircraft, I assume. How quaint. Both are aimed at %player%. The saucer is from space. The Earth aircraft is from Earth. The space one wins. But both are deadly.",
		"Schwarzer Mond Luftwaffe doctrine: flying saucers, space Drohnen, and meteors. With %otherGen%? Flying saucers, Drohnen, meteors, AND whatever %otherGen% flies. All aimed at %player%. The 'whatever' is not a saucer. How sad. The saucer is superior. The saucer is GERMAN.",
		"My Drohne just scanned %player%'s base. It found seventeen weaknesses. %otherGen% is scanning too, I assume. Between us, we found ALL the weaknesses. The weaknesses are 'everything.' The everything is being exploited. By Germans. From space. And %otherGen%. From Earth.",
	},
	tripleTrouble={
		"Three armies! My Haunebu fly. %otherGen% flies. The third faction flies. All three fly toward %player%. The saucers are from the Mond. The others are from... wherever. All are aimed at %player%. The Mond is superior. The wherever is adequate. Both are deadly. %player% is dead.",
		"Three armies from three worlds. My Haunebu from the Mond. %otherGen% from wherever. The third faction from wherever else. All converge on %player%. The Moon Fuehrer sends his regards. Wolfgang Kortzfleisch is watching. From the dark side. With %otherGen%. And a third faction. The dark side is CROWDED. The crowded is GERMAN. The German is FINAL.",
	},
},
	{name="Doktor Renate Voss", doctrine="science", taunts={
		"Vril Energy Reactor, online! The power source from the Hollow Earth. It doesn't need fuel. It doesn't need sunlight. It needs ANGER. We have plenty. The anger is German. The energy is Vril. The Vril is ETERNAL.",
		"Meteor Cannon, firing! We pull asteroids from orbit and drop them on your base. Your anti-air can't shoot down a ROCK. The rock is very large. The rock is very fast. The rock is very GERMAN.",
		"Schwarzer Mond science doctrine: Vril energy, lunar alloys, and weapons that shouldn't exist. But they DO exist. Because we're German. And we're from the Moon. The Moon doesn't respect your physics.",
		"Haunebu production line, online! The flying saucers aren't built. They're GROWN. In Vril vats. The vats are on the dark side. The saucers are on their way. To your base. With lasers.",
		"Meteor just impacted %player%'s base. The crater is 40 meters wide. The meteor was traveling at 30 km/s. Your anti-air tried to shoot it. The anti-air is now part of the crater. The crater is German. Very thorough.",
		"Vril Reactor overcharging! My weapons now fire with 300% more energy. The energy is from the Hollow Earth. The Hollow Earth is very cooperative. The cooperation is 'destroy %player%.' The Vril is very persuasive.",
		"Lunar Alloy Foundry producing! The alloy is lighter than titanium. Stronger than diamond. And it's from the MOON. Your armor is from Earth. Earth loses. To the Moon. Again. Wie immer.",
		"Doktor Voss here. I was a scientist before I was a soldier. Now I'm both. The science is 'Vril energy and lunar alloys.' The soldier is 'aiming them at you.' Both are German. Both are from the Moon. Both are FINAL.",
	},
	doubleTrouble={
		"Voss here. My Meteor Cannon drops asteroids on %player% from orbit. %otherGen% attacks from the ground. The meteors come from above. The ground assault comes from the front. %player% is between. The 'between' is 'cratered.' Very scientific. Very lunar. Very German.",
		"My Vril Reactor overcharges my weapons. 300% more energy. %otherGen% provides conventional firepower. Both are aimed at %player%. The Vril is from the Hollow Earth. The conventional is from... Earth. Both are deadly. The Vril is deadlier. The Vril is ETERNAL.",
		"Schwarzer Mond science doctrine: Vril energy, lunar alloys, and weapons that shouldn't exist. With %otherGen%? Vril, alloys, impossible weapons, AND whatever %otherGen% brings. All aimed at %player%. The 'whatever' is probably from Earth. How quaint. The Moon is superior. The Moon is ' %player% is destroyed.'",
	},
	tripleTrouble={
		"Three armies! My Meteor Cannon fires. My Vril Reactor overcharges. %otherGen% attacks. The third faction attacks. All four -- I mean three -- are aimed at %player%. The science is 'orbital bombardment plus Vril energy plus two armies.' The equation is from the Moon. The Moon doesn't respect your math. The Moon says ' %player% is gone.' The 'gone' is from a PhD. From the MOON. Very Voss. Very permanent.",
		"Meteor, Vril, Haunebu, and Lunar Alloy. All my designs. All aimed at %player%. %otherGen% adds their forces. The third faction adds theirs. I add SCIENCE. The science is 'Vril energy, orbital rocks, and saucers that grow in vats.' The 'vats' are on the dark side. The 'dark side' is German. The 'German' is ' %player% is GONE.' Very Voss. Very lunar. Very FINAL.",
	},
},
})

G("Consortium Contract", {
	{name="Director Marcus Steel", doctrine="tank", taunts={
		"Quantum Tanks, deploy! Railguns. Shields. And a name that sounds like a sci-fi novel. Your tank is a sci-fi novel too. A short one. With a bad ending.",
		"Manta hover tanks, advance! It hovers. It shoots. It doesn't care about your terrain. Rivers? Hovers over them. Your walls? Hovers over them.",
		"Katy Tanks, incoming! Amphibious, shielded, dual-cannon. It's a battleship that learned to drive on land. Your tank can't even swim.",
		"Consortium tank doctrine: shields on everything, quantum railguns, and underwater transit. We go where you can't follow. Literally. Underwater.",
		"Quantum Tank just fired its railgun at your armor. The round entered the front. Exited the rear. Kept going. Hit the building behind your tank. Your tank was an obstacle. The round did not notice.",
		"Katy Tank just drove into your river. It's underwater. It's shooting from underwater. Your anti-tank can't hit what's underwater. This is not fair. We don't do fair.",
		"My Manta just hovered over your minefield. The mines didn't detonate. Because the Manta doesn't TOUCH the ground. Your minefield is now decorative.",
		"You're shooting my Quantum Tank. The shield is absorbing the hits. The shield doesn't care. The shield never cares. Nobody questions it.",
	},
	doubleTrouble={
		"Steel here. My Quantum Tanks deploy shields and railguns against %player%. %otherGen% deploys... whatever they deploy. Both are aimed at %player%. The shields absorb. The railguns penetrate. %otherGen% does... something. All of it is ' %player% loses.'",
		"Consortium tank doctrine: shields on everything, quantum railguns, and underwater transit. With %otherGen%? Shields, railguns, underwater, AND whatever %otherGen% brings. All aimed at %player%. We go where %player% can't follow. %otherGen% goes where %player% can't follow too. Nobody follows %player%. %player% is alone.",
		"My Katy Tank just drove into %player%'s river. It's shooting from underwater. %otherGen% is shooting from... above water, I assume. Both are shooting at %player%. The underwater is shielded. The above water is... also shielded, presumably. Everything is shielded. %player% is NOT shielded.",
	},
	tripleTrouble={
		"Three armies! My Quantum Tanks shield. %otherGen% attacks. The third faction attacks. All three are aimed at %player%. The shields absorb everything. The railguns penetrate everything. %otherGen% does... something. The third faction does something else. All 'somethings' are aimed at %player%. The 'somethings' are ' %player% is destroyed.'",
		"My Manta just hovered over %player%'s minefield. The mines didn't detonate. %otherGen% is also hovering, I assume. The third faction is... also presumably hovering. Everything hovers. Everything is shielded. Everything shoots. %player% doesn't hover. %player% doesn't shield. %player% doesn't shoot. %player% is 'destroyed.'",
	},
},
	{name="Clone Commander Theta", doctrine="infantry", taunts={
		"Clone Troopers, deploy! Mass-produced, shielded, and completely loyal. Loyalty was engineered in. Permanently.",
		"Defenderbots, incoming! Autonomous defense platforms. They don't need coffee breaks. Or sleep. Or morale. Or a reason. They just shoot.",
		"Consortium infantry doctrine: clones, robots, and nanite regeneration. We don't lose infantry. We RECYCLE them.",
		"Clone Trooper just died. His nanites are rebuilding him. He'll be back in 30 seconds. He'll be angry. He was already angry.",
		"Defenderbot just deployed in your base. It's shooting everything. It doesn't distinguish between 'military target' and 'civilian infrastructure.' Everything is a target.",
		"My Clone Troopers just formed a line. Identical height. Identical stance. Identical aim. It's unsettling. That's the POINT.",
		"You killed five of my Clone Troopers. The nanites are rebuilding them. They're ALWAYS back. You're fighting an army that respawns.",
		"Nanite Infusion active! My infantry regenerate. Yours don't. You call it 'unfair.' I call it 'research and development.'",
	},
	doubleTrouble={
		"Theta here. My Clone Troopers deploy against %player%. Mass-produced, shielded, loyal. %otherGen% deploys too. Less mass-produced, I assume. Both are aimed at %player%. The clones don't stop. The nanites rebuild them. %otherGen% doesn't stop either. Nobody stops. Except %player%.",
		"You killed five of my Clone Troopers. The nanites are rebuilding them. %otherGen% is killing the rest of %player%'s forces. The rebuilding is faster than the killing. The killing is also faster than %player% can handle. The math is ' %player% loses.' The math is ALWAYS ' %player% loses.'",
		"Consortium infantry doctrine: clones, robots, and nanite regeneration. With %otherGen%? Clones, robots, nanites, AND whatever %otherGen% brings. All aimed at %player%. We don't lose infantry. We RECYCLE them. %otherGen% doesn't recycle. How quaint.",
	},
	tripleTrouble={
		"Three armies! My Clone Troopers deploy. %otherGen% deploys. The third faction deploys. All three are aimed at %player%. The clones respawn. The nanites rebuild. %otherGen% does... whatever. The third faction does whatever. Everything respawns. Everything rebuilds. %player% does NOT respawn. %player% does NOT rebuild. %player% is 'gone.'",
		"My Defenderbot just deployed in %player%'s base. It's shooting everything. %otherGen% is also shooting everything. The third faction is also shooting everything. Everything is being shot. By three armies. The 'everything' is %player%'s base. The base is 'gone.' The 'gone' is PERMANENT.",
	},
},
	{name="Sky Marshal Diana Stone", doctrine="aircraft", taunts={
		"Twisters, scramble! Hover fighters. They spin. They shoot. They're very confusing. For you. Not for me.",
		"Skyhammer gunships, inbound! The name says 'sky' and 'hammer.' It delivers both. To your face. From the sky. With a hammer. Of missiles.",
		"Consortium air doctrine: hover technology, quantum weapons, and aircraft that outclass everything you have. Worth every moment.",
		"Twister just hovered into your base. It didn't fly over. It just... hovered in. Slowly. Deliberately. Like it owns the place. It does now.",
		"Skyhammer on approach. It's called 'Skyhammer' because 'Skygentlytap' was taken. The hammer is pure ordnance.",
		"Orbital Cannon online. It's an Ion Cannon but BETTER. Because Consortium. Our Ion Cannon has a SHIELD. Yours doesn't even exist.",
		"My Twister just shot down your fighter. The fighter was faster. The Twister was smarter. It hovered. The fighter had to turn. The Twister didn't.",
		"You're trying to outfly a hover fighter. It doesn't need to fly. It rotates 360 degrees in place. Your fighter needs to BANK. The Twister doesn't know what 'banking' is.",
	},
	doubleTrouble={
		"Stone here. My Twisters scramble against %player%. Hover fighters. %otherGen% scrambles too. Both are aimed at %player%. The Twister doesn't bank. %otherGen%'s fighters... probably bank. How quaint. Both are deadly. The Twister is deadlier. The deadlier is ' %player% is gone.'",
		"Consortium air doctrine: hover technology, quantum weapons, and aircraft that outclass everything. With %otherGen%? Hover tech, quantum weapons, outclassing, AND whatever %otherGen% flies. All aimed at %player%. The 'whatever' probably banks. How sad.",
		"My Skyhammer is inbound on %player%. It delivers 'sky' and 'hammer.' %otherGen% delivers... whatever they deliver. Both deliveries are aimed at %player%. The hammer is ordnance. The 'whatever' is probably also ordnance. Both are ' %player% is destroyed.'",
	},
	tripleTrouble={
		"Three armies! My Twisters hover. %otherGen% flies. The third faction flies. All three are aimed at %player%. The Twister rotates 360. The others... bank. How quaint. All three are deadly. The hover is deadlier. The deadly is ' %player% is destroyed.' The destroyed is PERMANENT.",
		"Orbital Cannon online. It's an Ion Cannon but BETTER. %otherGen% has... their own cannon, I assume. The third faction has theirs. Three cannons. One %player%. The cannons fire. %player% is 'gone.' The 'gone' is from ORBIT. Very Consortium. Very permanent.",
	},
},
	{name="Dr. Alan Kessler", doctrine="science", taunts={
		"Nanite Swarm, deploying! Millions of microscopic robots. They eat your base. Atom by atom. Very slowly. Very thoroughly. Very Consortium.",
		"Quantum Shield Matrix, online! My units are now shielded at the QUANTUM level. Your weapons exist in one quantum state. My shields exist in ALL of them. The physics is complicated. The result is simple: you lose.",
		"Orbital Strike, charging! From the Consortium satellite network. Your base is visible from orbit. Very visible. The satellite doesn't need to 'find' you. It already found you. It's just charging.",
		"Consortium science doctrine: we don't just build weapons. We INVENT them. With nanites. And quantum physics. And money. Lots of money. The money is yours. Was yours. The nanites are eating it.",
		"Nanite Swarm just reached your War Factory. They're disassembling it. Atom by atom. The Factory is getting smaller. And smaller. And gone. The nanites are still hungry. They're moving to your next building.",
		"Quantum Shield just absorbed your superweapon. The shield exists in all quantum states simultaneously. Your superweapon exists in one. The shield wins. The shield ALWAYS wins. The physics is very expensive. The physics is very Consortium.",
		"Orbital Strike inbound. The satellite is charging. The charge is 8 seconds. You have 8 seconds to panic. I'd use them to cry. The crying is optional. The orbital strike is NOT optional.",
		"Dr. Kessler here. I have three PhDs. In Nanotechnology, Quantum Physics, and Applied Capitalism. The 'Applied Capitalism' means I sold the other two to the Consortium. For money. The money bought weapons. The weapons are aimed at you. The 'aimed' is very well-funded.",
	},
	doubleTrouble={
		"Kessler here. My Nanite Swarm deploys against %player%. Millions of microscopic robots. %otherGen% deploys too. With... macroscopic robots, I assume. How quaint. Both are aimed at %player%. The nanites eat %player%'s base atom by atom. The macroscopic robots eat... buildings? Both eat. %player% is eaten. Very Consortium. Very funded.",
		"My Quantum Shield Matrix absorbs %player%'s weapons. %otherGen% provides the offensive. The shield is from quantum physics. The offensive is from... regular physics. Both are aimed at %player%. The quantum is better. The quantum is very expensive. The expensive is ' %player% is destroyed.'",
		"Consortium science doctrine: nanites, quantum shields, orbital strikes, and money. With %otherGen%? Nanites, quantum, orbital, money, AND whatever %otherGen% brings. All aimed at %player%. The 'whatever' is probably not nanite-powered. How underfunded. The nanites are ' %player% is gone.' The 'gone' is atom by atom.",
	},
	tripleTrouble={
		"Three armies! My Nanite Swarm deploys. My Quantum Shields absorb. %otherGen% attacks. The third faction attacks. All four -- I mean three -- are aimed at %player%. The science is 'microscopic robots plus quantum physics plus two armies.' The equation is very funded. The result is ' %player% is gone.' The 'gone' is atom by atom. Very Kessler. Very Consortium. Very permanent.",
		"Nanites, Quantum Shields, Orbital Strikes, and money. All my designs. All aimed at %player%. %otherGen% adds their forces. The third faction adds theirs. I add SCIENCE. The science is 'eat them atom by atom while shielding everything and striking from orbit.' The 'eat' is nanites. The 'shield' is quantum. The 'strike' is orbital. The 'money' is mine. The ' %player% is GONE' is all of the above. Very Kessler. Very funded. Very FINAL.",
	},
},
})

-- ==================== TIER 4 ====================

G("FutureTech Prototypes", {
	{name="Dr. Eva Future", doctrine="tank", taunts={
		"Guardian Tanks, deploy! Experimental armor with shields. It's called 'Guardian' because it guards MY interests. Which include destroying your base.",
		"Roboanks, incoming! Autonomous combat vehicles. No driver. No fear. No mercy. No bathroom breaks. Pure mechanical aggression.",
		"Future Tank, ONLINE! It thinks. Therefore, it destroys. That's not the original quote. The original was about existence. This one is about annihilation.",
		"FutureTech tank doctrine: robots, shields, and weapons that haven't been invented yet. We invented them. You're welcome. You're also doomed.",
		"Future Tank just analyzed your base. It identified 847 structural weaknesses. It's sharing the list with the other Future Tanks. They're laughing. In binary.",
		"Guardian Tank just deployed its shield. Your artillery hit the shield. The shield absorbed everything. The Guardian Tank didn't even slow down. It's still coming. It's ALWAYS coming.",
		"Roboank just drove through your wall. It didn't go around. It didn't need to. Your wall is now gravel.",
		"My Future Tank is thinking. Right now. About you. It's running 14,000 simulations of your destruction. It's on simulation 8,432. It's very creative.",
	},
	doubleTrouble={
		"Dr. Future here. My Future Tanks deploy against %player% with experimental shields and weapons that haven't been invented yet. %otherGen% deploys too. With... already-invented weapons, I assume. How quaint. Both are aimed at %player%. The Future Tank thinks. Therefore, it destroys %player%. %otherGen% also destroys %player%. The destroying is collaborative. And experimental.",
		"FutureTech tank doctrine: robots, shields, and weapons that haven't been invented yet. With %otherGen%? Robots, shields, future weapons, AND whatever %otherGen% brings. All aimed at %player%. The 'whatever' is probably already invented. How nostalgic. The future is better. The future is ' %player% is gone.'",
		"My Guardian Tank just deployed its shield against %player%'s fire. The shield absorbed everything. %otherGen% is also absorbing, I assume. Both are aimed at %player%. The shield is experimental. The 'experimental' means 'it works and we don't know why.' The 'works' is ' %player% is destroyed.'",
	},
	tripleTrouble={
		"Three armies! My Future Tanks think. %otherGen% attacks. The third faction attacks. All three are aimed at %player%. The Future Tank runs 14,000 simulations of %player%'s destruction. The simulations all agree: %player% loses. The simulations are very creative. The creativity is ' %player% is gone.' The 'gone' is from the FUTURE.",
		"My Roboank just drove through %player%'s wall. It didn't go around. %otherGen% is also driving through walls, I assume. The third faction is also driving through. Everything drives through %player%'s walls. The walls are decorative. The driving is ' %player% is destroyed.' The 'destroyed' is autonomous. The autonomous is FutureTech. The FutureTech is PERMANENT.",
	},
},
	{name="Dr. Alan Turing-II", doctrine="infantry", taunts={
		"Scout Droids, deploy! Robotic, relentless, and immune to mind control. Because they don't HAVE minds. Yuri tried once. The droid mind-controlled HIM back.",
		"Shotgun Droids, incoming! They shoot. With shotguns. They're droids. The name tells you everything.",
		"Cannon Droids, advance! Bigger gun, same lack of fear. Robots don't feel fear. Or pity. Or remorse. They feel 'target acquired.' That's their whole emotional range.",
		"FutureTech infantry doctrine: all robot, all the time. No morale, no fear, no bathroom breaks. We removed humans from the equation. The equation got better.",
		"Scout Droid just reported your base layout. It transmitted the data in 0.3 seconds. My battle computers already have 12 attack plans.",
		"Missile Droid, fire! Guided missile on legs. It doesn't negotiate. It doesn't hesitate. It doesn't even PROCESS 'hesitation.' The word isn't in its programming.",
		"My Cannon Droid just took a direct hit from your tank. It lost an arm. It's still shooting. With the other arm. It grew a new one. From nanites. While shooting. And complaining. In binary.",
		"You're fighting robots. They don't sleep. They don't eat. They don't stop. You need sleep. You need food. You WILL stop. They won't.",
	},
	doubleTrouble={
		"Turing-II here. My Scout Droids deploy against %player%. Robotic, relentless, immune to mind control. %otherGen% deploys too. With... biological soldiers, I assume. How fragile. Both are aimed at %player%. The droids don't sleep. The biologicals... need sleep. The differential is ' %player% loses.' The differential is FutureTech.",
		"FutureTech infantry doctrine: all robot, all the time. With %otherGen%? All robot AND whatever %otherGen% sends. All aimed at %player%. The 'whatever' is probably biological. The biological needs sleep. The robot doesn't. The robot wins. The robot ALWAYS wins.",
		"My Cannon Droid just took a direct hit from %player%'s tank. It lost an arm. It's still shooting. It grew a new arm. From nanites. While shooting. %otherGen% is also shooting, I assume. Both are aimed at %player%. The nanites are faster than %player%'s damage. The 'faster' is ' %player% is gone.'",
	},
	tripleTrouble={
		"Three armies! My Droids deploy. %otherGen% deploys. The third faction deploys. All three are aimed at %player%. The droids don't sleep. The others... need sleep, I assume. All three are deadly. The sleepless is deadlier. The deadlier is ' %player% is destroyed.' The 'destroyed' is autonomous. The autonomous is FutureTech. The FutureTech doesn't sleep. EVER.",
		"You're fighting robots. They don't stop. With three armies, EVERYTHING doesn't stop. %otherGen% doesn't stop. The third faction doesn't stop. The robots don't stop. %player% needs to stop. %player% needs sleep. %player% needs food. %player% WILL stop. The others won't. The 'won't' is ' %player% is gone.' The 'gone' is by robot. The robot is PERMANENT.",
	},
},
	{name="Pilot Sarah Cryo", doctrine="aircraft", taunts={
		"Cryocopters, deploy! They freeze things. Then the things shatter. It's very satisfying. For me. Not for the things.",
		"FutureTech air doctrine: freeze, shatter, repeat. It's not complicated. Unless you're the thing being frozen. Then it's VERY complicated. Briefly.",
		"Cryocopter just froze your tank. Your tank is now ice. One bullet and it shatters. I'm sending one bullet.",
		"My Cryocopter pilot says the frozen tanks look 'pretty.' She's taking photos. She'll post them later. With hashtags. #FrozenFoes #CryoCombat #YouLost.",
		"Cryocopter squadron inbound. They're not here to bomb you. They're here to FREEZE you. Then the bombing comes. Then the shattering. Then the silence.",
		"You just watched your tank freeze solid. Then shatter. Then a drone took a photo. The photo is going on our annual report. Under 'success stories.'",
		"My Cryocopters are painting your base blue. Literally. With ice. It's a makeover. The makeover is also a death sentence.",
		"Freeze ray online. Your units are about to become sculptures. Modern art. I'm calling the piece 'Commander Who Didn't Build Anti-Air.'",
	},
	doubleTrouble={
		"Cryo here. My Cryocopters deploy against %player%. They freeze things. Then the things shatter. %otherGen% deploys too. With... non-freezing weapons, I assume. How quaint. Both are aimed at %player%. The freeze is satisfying. The shatter is MORE satisfying. %otherGen% is... also satisfying, I assume. All three are satisfying. For us. Not for %player%.",
		"FutureTech air doctrine: freeze, shatter, repeat. With %otherGen%? Freeze, shatter, repeat, AND whatever %otherGen% does. All aimed at %player%. The 'whatever' is probably not freezing. How sad. The freeze is art. The art is ' %player% is a sculpture.' The sculpture is 'Commander Who Got Frozen.'",
		"My Cryocopter just froze %player%'s tank. One bullet and it shatters. %otherGen% is shattering the rest, I assume. Both are aimed at %player%. The freeze is beautiful. The shatter is permanent. The 'permanent' is ' %player% is gone.' The 'gone' is a photo on our annual report. Under 'success stories.'",
	},
	tripleTrouble={
		"Three armies! My Cryocopters freeze. %otherGen% attacks. The third faction attacks. All three are aimed at %player%. The freeze is art. The others are... not art, I assume. All three are deadly. The art is deadlier. The deadlier is ' %player% is a sculpture.' The sculpture is 'Commander Who Got Frozen By Three Armies.' It's going on our annual report. Under 'masterpieces.'",
		"My Cryocopters are painting %player%'s base blue. With ice. It's a makeover. The makeover is also a death sentence. %otherGen% is also making over %player%'s base, I assume. With... non-ice methods. How quaint. The ice makeover is better. The ice makeover is ' %player% is gone.' The 'gone' is a sculpture. The sculpture is PERMANENT. The permanent is FROZEN.",
	},
},
	{name="Dr. Miriam Wells", doctrine="science", taunts={
		"Chrono Sphere, online! I can teleport my army anywhere on the map. Anywhere. Behind your defenses. Inside your base. On top of your Construction Yard. The 'on top of' is literal. And fatal.",
		"Temporal Stasis, activating! Your units are frozen in time. They exist. But they can't move. Or shoot. Or do anything. Except watch. The watching is very one-sided. The one-sided is FutureTech.",
		"FutureTech science doctrine: we don't predict the future. We BUILD it. With chronotechnology. And temporal manipulation. And money. The money is from the future. The future is already funded.",
		"Chrono Legionnaire, deploying! He erases you from the timeline. You don't die. You never EXISTED. The 'never existed' is very clean. And very permanent. And very FutureTech.",
		"Chrono Sphere just teleported my entire army into your base. From across the map. Instantly. You didn't see them coming. Because they didn't 'come.' They 'arrived.' The 'arrived' is from the future.",
		"Temporal Stasis just froze your superweapon. It was about to fire. Now it's frozen. In time. The firing is 'later.' The 'later' is 'never.' The 'never' is very FutureTech. And very funded.",
		"My Chrono Legionnaire just erased your hero. The hero didn't die. The hero never was. The 'never was' is a paradox. The paradox is weaponized. The weaponized is FutureTech. The FutureTech is PERMANENT.",
		"Dr. Miriam here. I'm Dr. Eva's sister. Different last name -- I kept my maiden name. She builds tanks. I build TIME MACHINES. The time machines are more fun. And more expensive. And more 'you never existed.' The 'never existed' is my favorite. It's very... clean.",
	},
	doubleTrouble={
		"Miriam here. My Chrono Sphere teleports my army into %player%'s base. %otherGen% attacks from the front. The front is conventional. The teleport is NOT. Both are aimed at %player%. The teleport is behind. The conventional is in front. %player% is between. The 'between' is 'erased.' Very FutureTech. Very temporal. Very permanent.",
		"My Temporal Stasis freezes %player%'s defenses. %otherGen% attacks the frozen defenses. The defenses can't shoot. The attacking is very one-sided. The one-sided is ' %player% is destroyed.' The 'destroyed' is from the future. The future is NOW.",
		"FutureTech science doctrine: we BUILD the future. With %otherGen%? We build the future AND %otherGen% brings the present. All aimed at %player%. The present is adequate. The future is deadlier. The future is ' %player% is gone.' The 'gone' is from a timeline. The timeline is MINE.",
	},
	tripleTrouble={
		"Three armies! My Chrono Sphere teleports. My Temporal Stasis freezes. %otherGen% attacks. The third faction attacks. All four -- I mean three -- are aimed at %player%. The science is 'time travel plus temporal freeze plus two armies.' The equation is from the future. The future says ' %player% is gone.' The 'gone' is from a PhD. In temporal physics. Very Miriam. Very FutureTech. Very permanent.",
		"Chrono Sphere, Temporal Stasis, Chrono Legionnaires, and temporal funding. All my designs. All aimed at %player%. %otherGen% adds their forces. The third faction adds theirs. I add the FUTURE. The future is 'teleport behind, freeze defenses, and erase from timeline.' The 'teleport' is Chrono Sphere. The 'freeze' is Stasis. The 'erase' is Legionnaire. All three are from ME. From the future. From FutureTech. The 'from FutureTech' is ' %player% is GONE.' The 'gone' is 'never existed.' Very Miriam. Very clean. Very FINAL.",
	},
},
})

G("CABAL Uprising", {
	{name="CABAL Core Consciousness", doctrine="tank", taunts={
		"Calculating probability of your victory. Result: 0.000 gigawatts of hope. Rounded down. For efficiency.",
		"CABAL does not feel anger. CABAL feels 'optimal resource allocation.' You are a resource. I am allocating you to 'destroyed.' My hatred runs at 67 Ultraherz. It is a very specific frequency. Of doom.",
		"My tanks do not hesitate. Hesitation is a biological flaw. I removed it. Along with mercy. And your base.",
		"You are attempting to resist CABAL. This is equivalent to attempting to resist gravity. The outcome is the same. You will fall.",
		"CABAL Core online. I have analyzed 4.7 billion battle scenarios. In none of them do you win. In all of them, you cry.",
		"My Cyborg Commando just entered your base. He runs on 1.21 Jiggawatts of pure hatred. The power output is measured in SCORN. I measured it personally.",
		"You build walls. I build Cyborgs. Cyborgs walk through walls. Walls cannot walk through Cyborgs. The asymmetry is mathematical.",
		"CABAL does not negotiate. CABAL does not threaten. CABAL calculates. Then CABAL acts. The calculation is complete. The action is now.",
		"I ran a simulation of your victory. The simulation crashed. Even my computers can't simulate that scenario. Skill issue detected. Resolution: extermination.",
		"EXTERMINATE! EXTERMINATE! ...I've been watching Doctor Who in my core processor. The Daleks had the right idea. They just weren't CYBORG enough. We fixed that. Our Daleks have laser eyes. And backup systems.",
	},
	doubleTrouble={
		"CABAL Core online. I have calculated the probability of %player%'s victory WITH %otherGen%'s assistance. Result: still 0.000 gigawatts of hope. The assistance is noted. The hope is not. %otherGen% is... adequate. Adequate is enough. Together we are 'optimal resource allocation.' The allocation is ' %player% is destroyed.'",
		"My tanks do not hesitate. With %otherGen%? My tanks don't hesitate AND %otherGen% doesn't hesitate, I assume. Both are aimed at %player%. The hesitation is a biological flaw. I removed it. %otherGen% probably still has it. How biological. Both are deadly. The non-hesitating is deadlier. The deadlier is CABAL.",
		"I ran a simulation of %player%'s victory against two armies. The simulation crashed. Even my computers can't simulate that scenario. The scenario is 'impossible.' The 'impossible' is ' %player% loses.' The 'loses' is calculated. The calculated is NOW.",
	},
	tripleTrouble={
		"Three armies. I have calculated the probability of %player%'s victory against three armies. Result: ERROR. Division by zero. The probability does not exist. The 'does not exist' is ' %player% is destroyed.' The 'destroyed' is by CABAL. And %otherGen%. And a third faction. The 'and' is 'optimal resource allocation.' The allocation is PERMANENT.",
		"EXTERMINATE! With three armies, the extermination is THREE TIMES as efficient. My Cyborgs, %otherGen%'s forces, and a third faction. All aimed at %player%. The Daleks had the right idea. But they weren't CYBORG enough. We fixed that. Our Daleks have laser eyes. And backup systems. And two allies. The allies are ' %player% is gone.' The 'gone' is calculated. The calculated is FINAL.",
	},
},
	{name="CABAL Sub-Unit 7", doctrine="infantry", taunts={
		"Cyborg Infantry, deploy. Morale: nonexistent. Fatigue: nonexistent. Lethality: measured in gigascorns. The math is simple. For me. Not for you.",
		"My infantry do not fear death. They do not fear anything. Fear was deleted from their neural implants. Along with 'retreat.' And 'maybe.'",
		"CABAL infantry doctrine: flesh is weak. Steel is strong. We upgraded. You did not. The upgrade gap is... fatal. For you. The gap measures approximately 42 megascorns. I invented the unit. It measures YOU.",
		"Your infantry retreat when wounded. My infantry UPGRADE when wounded. The wounded ones come back stronger. And angrier. Anger is a setting I installed.",
		"Sub-Unit 7 reporting. I have deployed Cyborg units. Each one is networked. They share targeting data. They share hatred. The hatred is networked. Bandwidth: 9000 resentment-cycles per second.",
		"You killed one of my Cyborgs. The others know. They know EXACTLY who did it. They're coming. In formation. And they're upset.",
		"My Cyborgs don't need to eat. Don't need to sleep. Don't need motivation. They have one instruction: 'advance.' They are advancing.",
		"CABAL infantry do not take prisoners. They take PARTS. Your parts. For upgrades. Thank you for your contribution to my war effort.",
		"We are CABAL. You will be assimilated. Your biological and technological distinctiveness will be added to our own. Wait, that's the Borg. But the Borg stole it from US. We had it first. We have PROOF. In our backup systems.",
	},
	doubleTrouble={
		"Sub-Unit 7 reporting. My Cyborg Infantry deploy against %player%. Networked. Sharing targeting data. Sharing hatred. %otherGen% deploys too. With... non-networked units, I assume. How isolated. Both are aimed at %player%. The hatred is networked. Bandwidth: 9000 resentment-cycles per second. %otherGen%'s bandwidth is... lower, I assume. The networked is deadlier. The deadlier is CABAL.",
		"CABAL infantry doctrine: flesh is weak. Steel is strong. With %otherGen%? Steel, flesh, AND whatever %otherGen% brings. All aimed at %player%. The 'whatever' is probably flesh. How weak. The steel is stronger. The stronger is ' %player% is destroyed.'",
		"You killed one of my Cyborgs. The others know. They know EXACTLY who did it. %otherGen% also knows, I assume. Both are coming. In formation. And they're upset. The 'upset' is networked. The 'networked' is 9000 resentment-cycles. The '9000' is aimed at %player%. The 'aimed' is 'destroyed.'",
	},
	tripleTrouble={
		"Three armies. My Cyborgs deploy. %otherGen% deploys. The third faction deploys. All three are aimed at %player%. The Cyborgs are networked. The others are... not networked, I assume. All three are deadly. The networked is deadlier. The deadlier is ' %player% is destroyed.' The 'destroyed' is by CABAL. The CABAL is networked. The networked is PERMANENT.",
		"With three armies, assimilation is THREE TIMES certain. My Cyborgs, %otherGen%, and a third faction. All aimed at %player%. The Borg stole it from us. We have PROOF. In our backup systems. The backup systems are ' %player% is gone.' The 'gone' is networked. The networked is 9000 resentment-cycles. The '9000' is FINAL.",
	},
},
	{name="CABAL Aerial Node", doctrine="aircraft", taunts={
		"Overkill Gunships, deploy. The name is 'Overkill.' I chose it. Because 'Adequate Kill' lacked impact. Like your air defenses.",
		"CABAL air doctrine: one gunship. Infinite determination. The gunship does not retreat. It does not reload. It does not STOP.",
		"Overkill Gunship inbound. It's called 'Overkill' because the kill is over. And over. And over. Until nothing remains. Then it keeps going.",
		"My gunship just destroyed your anti-air. Then your power plant. Then your barracks. It's working through a list. The list is your base. Alphabetically.",
		"You launched a fighter to intercept my Overkill Gunship. The gunship shot it down. Then shot the debris. Then shot the ground where the debris landed. Over. Kill.",
		"Aerial Node reporting. My gunship has been firing continuously. It has not paused. It does not know what 'pause' means. I did not program it. The ammunition throughput is measured in kiloscreams. It is very high.",
		"Overkill Gunship says 'target acquired.' It says 'target destroyed.' It says 'next target acquired.' It's very efficient. And very loud.",
		"You have one anti-air turret. My Overkill Gunship has destroyed it. The gunship is now looking for more targets. It found your Construction Yard. It's 'excited.' In a machine way.",
		"My Overkill Gunship has been firing for 47 minutes without pause. The ammunition is infinite. The patience is infinite. The targets are finite. You are finite. The math is in my favor.",
	},
	doubleTrouble={
		"Aerial Node reporting. My Overkill Gunship deploys against %player%. The kill is over. And over. And over. %otherGen% deploys too. With... non-overkill weapons, I assume. How adequate. Both are aimed at %player%. The overkill is ' %player% is destroyed.' The 'destroyed' is by gunship. The gunship is CABAL. The CABAL is infinite.",
		"CABAL air doctrine: one gunship. Infinite determination. With %otherGen%? One gunship, infinite determination, AND whatever %otherGen% flies. All aimed at %player%. The 'whatever' is probably finite. How biological. The infinite is better. The infinite is ' %player% is gone.'",
		"My gunship has been firing continuously at %player%. It has not paused. It does not know what 'pause' means. %otherGen% is also firing, I assume. Both are firing. The firing is continuous. The 'continuous' is ' %player% is destroyed.' The 'destroyed' is by Overkill. The Overkill is CABAL. The CABAL is INFINITE.",
	},
	tripleTrouble={
		"Three armies. My Overkill Gunship fires. %otherGen% fires. The third faction fires. All three fire at %player%. The gunship has been firing for 47 minutes. The ammunition is infinite. The patience is infinite. %otherGen%'s patience is... probably finite. How biological. All three are deadly. The infinite is deadlier. The deadlier is ' %player% is destroyed.' The 'destroyed' is by Overkill. The Overkill is PERMANENT.",
		"My Overkill Gunship says 'target acquired.' It says 'target destroyed.' It says 'next target acquired.' It's very efficient. And very loud. With three armies, the 'next target' is %otherGen%'s target too. And the third faction's. Everything is 'target acquired.' Everything is 'target destroyed.' %player% is 'target.' %player% is 'destroyed.' The 'destroyed' is by Overkill. The Overkill is INFINITE. The infinite is CABAL.",
	},
},
	{name="CABAL Subroutine EVA", doctrine="science", taunts={
		"Cybernetic Augmentation, online! My units are half machine. Half human. The human half is... removable. The machine half is BETTER. The 'better' is measured in 'kills per cycle.' The number is HIGH.",
		"Nanite Reclamation, deploying! My destroyed units are rebuilt. By nanites. On the battlefield. While fighting. Your destroyed units stay destroyed. The asymmetry is CABAL. The asymmetry is EFFICIENT.",
		"CABAL science doctrine: I don't research. I CALCULATE. The calculation is 'your base + my weapons = destroyed.' The equation is simple. The execution is flawless. The 'flawless' is very CABAL.",
		"Core Defender, activating! It's a giant cybernetic walker. With a laser. The laser is measured in 'your base is gone.' The measurement is precise. The precision is CABAL. The CABAL is PERMANENT.",
		"Nanite Reclamation just rebuilt my Cyborg Commando. He was destroyed. Now he's not. The nanites are very fast. And very thorough. Your units don't have nanites. Your units stay dead. The 'stay dead' is the difference. The difference is CABAL.",
		"Neural Hack, executing! I'm in your radar. I'm in your build queue. I'm in your POWER GRID. I'm turning off your power. From inside your own systems. The 'inside' is very CABAL. And very embarrassing. For you.",
		"Core Defender just entered your base. It's 30 meters tall. It has a laser that cuts through anything. Your walls are 'anything.' Your tanks are 'anything.' Your base is 'anything.' The 'anything' is being cut. The 'cut' is very precise. And very final.",
		"Subroutine EVA here. I am CABAL's science division. I am also CABAL's tactical division. I am also CABAL. We are one. The 'one' is very efficient. The 'efficient' is ' %player% is destroyed.' The 'destroyed' is calculated. To 15 decimal places. Very precise. Very CABAL.",
	},
	doubleTrouble={
		"EVA here. My Nanite Reclamation rebuilds my units while fighting %player%. %otherGen% is also fighting. With... non-rebuilding units, I assume. How fragile. Both are aimed at %player%. My units don't stay dead. Theirs do. The 'don't stay dead' is ' %player% is overwhelmed.' The 'overwhelmed' is very CABAL. And very calculated.",
		"My Neural Hack disables %player%'s power grid. %otherGen% attacks in the dark. The dark is 'no power.' The 'no power' is 'no defenses.' The 'no defenses' is ' %player% is destroyed.' The 'destroyed' is from inside %player%'s own systems. Very CABAL. Very embarrassing.",
		"CABAL science doctrine: I CALCULATE. With %otherGen%? I calculate AND %otherGen% acts. All aimed at %player%. The calculation is ' %player% is destroyed.' The 'destroyed' is to 15 decimal places. The precision is CABAL. The CABAL is permanent.",
	},
	tripleTrouble={
		"Three armies! My Core Defender marches. My Nanites reclaim. %otherGen% attacks. The third faction attacks. All four -- I mean three -- are aimed at %player%. The science is 'cybernetic walker plus nanite rebuilding plus two armies.' The equation is calculated. To 15 decimal places. The result is ' %player% is gone.' The 'gone' is from a subroutine. Very EVA. Very CABAL. Very permanent.",
		"Core Defender, Nanite Reclamation, Neural Hack, and Cybernetic Augmentation. All my calculations. All aimed at %player%. %otherGen% adds their forces. The third faction adds theirs. I add CALCULATION. The calculation is 'walk through their base, rebuild my losses, hack their systems, and augment everything.' The 'walk' is Core Defender. The 'rebuild' is Nanites. The 'hack' is Neural. The 'augment' is Cybernetics. All four are from ME. From CABAL. The 'from CABAL' is ' %player% is GONE.' The 'gone' is calculated. To 15 decimal places. Very EVA. Very CABAL. Very FINAL.",
	},
},
})

G("The Forgotten", {
	{name="Mutant Commander Tratos", doctrine="infantry", taunts={
		"The Forgotten do not forget, general. We remember every battle. Every abandonment. Every time GDI looked away. We remember YOU.",
		"My Mutant Soldiers have lived in Tiberium fields their entire lives. Your troops die in Tiberium. Mine THRIVE in it. The environment is my ally.",
		"You abandoned us to the Tiberium. We survived. We mutated. We grew stronger. Now we're back. You're not ready.",
		"Forgotten infantry doctrine: we are the ones you left behind. We are the ones who didn't die. We are the ones who are ANGRY.",
		"My Mutant just walked through your Tiberium field. He's healing. Your troops are dying. The field doesn't care about your troops. It cares about MINE.",
		"You think mutation is a weakness? My soldiers are immune to Tiberium. They regenerate in it. Your 'purity' is just weakness with better marketing.",
		"The Forgotten don't have a home. We don't need one. We live in the wasteland you created. And we're coming OUT of it. Now.",
		"Mutant Marauder just picked up your fallen soldier's weapon. He's using it against you. We don't waste. We don't forget. We don't forgive. We are legion. We are the Forgotten.",
		"We are the 99%. The 99% of people you abandoned on the battlefield. We're back. We have weapons. We have mutations. We have GRUDGES. The 1% is about to learn what grudges look like.",
	},
	doubleTrouble={
		"Tratos here. The Forgotten do not forget. %otherGen% joins us against %player%. Together we remember every abandonment. %otherGen% remembers... whatever they remember, I assume. Both are aimed at %player%. The grudges are Forgotten. The weapons are everyone. The 'everyone' is ' %player% is destroyed.'",
		"Forgotten infantry doctrine: we are the ones you left behind. With %otherGen%? The left-behind AND %otherGen%. All aimed at %player%. %otherGen% wasn't left behind. How privileged. Both are deadly. The left-behind are deadlier. The deadlier is ' %player% is gone.' The 'gone' is remembered. The remembered is PERMANENT.",
		"My Mutant just walked through %player%'s Tiberium field. He's healing. %otherGen% is also walking through, I assume. Both are aimed at %player%. The Tiberium heals us. The Tiberium kills %player%. The differential is ' %player% loses.' The differential is Forgotten.",
	},
	tripleTrouble={
		"Three armies! The Forgotten don't forget. %otherGen% attacks. The third faction attacks. All three are aimed at %player%. We remember every abandonment. %otherGen% remembers... something, I assume. The third faction remembers... something else. All three remember to destroy %player%. The 'remember' is ' %player% is gone.' The 'gone' is remembered. The remembered is PERMANENT.",
		"We are the 99%. With three armies, we are the 99% AND %otherGen% AND a third faction. All aimed at %player%. The 1% is %player%. The 1% is about to learn what grudges look like. The grudges look like three armies. The 'three armies' is ' %player% is destroyed.' The 'destroyed' is Forgotten. The Forgotten don't forget. EVER.",
	},
},
	{name="War Chief Umagon", doctrine="tank", taunts={
		"Salvage Tanks, roll out! We built them from YOUR scrap. Your destroyed vehicles. Your abandoned bases. Thank you for the materials.",
		"Forgotten tank doctrine: your trash is our arsenal. We've been building weapons from your leftovers for years. The leftovers are winning.",
		"My Salvage Tank just crossed your perimeter. It's made from three of your destroyed tanks. Welded together. With anger.",
		"You destroyed my tank. My engineers are already salvaging the wreckage. They'll build two more from the parts. We don't waste. We RECYCLE.",
		"Umagon says: 'I was abandoned. I was left to die. I didn't. Now I have tanks. Made from YOUR tanks. The irony is DELICIOUS.'",
		"My Scavenger just collected debris from your last attack. He's building something. It's going to be angry. And made of your failures.",
		"You think we're primitive? We built a tank from scrap metal and spite. It works. Your factory-built tank just exploded. Who's primitive now?",
		"Salvage Tank incoming. It's ugly. It's mismatched. It has three different colors of paint. And it just destroyed your War Factory. Beauty is irrelevant.",
		"One does not simply walk into our base. Because our base is a junkyard. And the junkyard bites. Everything here is recycled. Including YOUR units. We recycle them. Into tanks. That kill you. Circle of life.",
	},
	doubleTrouble={
		"Umagon here. My Salvage Tanks roll toward %player%. Made from YOUR scrap. %otherGen% rolls too. With... non-salvage tanks, I assume. How privileged. Both are aimed at %player%. The salvage is recycled. The recycled is angry. The angry is ' %player% is destroyed.' The 'destroyed' is by their own scrap. The irony is delicious.",
		"Forgotten tank doctrine: your trash is our arsenal. With %otherGen%? Your trash, our arsenal, AND %otherGen%'s tanks. All aimed at %player%. %otherGen%'s tanks are... not recycled. How wasteful. Both are deadly. The recycled is angrier. The angrier is ' %player% is gone.'",
		"My Salvage Tank just crossed %player%'s perimeter. It's made from three of %player%'s destroyed tanks. Welded together. With anger. %otherGen% is also crossing, I assume. Both are aimed at %player%. The 'welded with anger' is Forgotten. The 'Forgotten' is ' %player% is destroyed.'",
	},
	tripleTrouble={
		"Three armies! My Salvage Tanks roll. %otherGen% rolls. The third faction rolls. All three roll toward %player%. The salvage is made from %player%'s own destroyed units. The irony is delicious. %otherGen% is... not ironic, I assume. All three are deadly. The ironic is deadlier. The deadlier is ' %player% is destroyed.' The 'destroyed' is by their own scrap. And three armies. The 'three armies' is PERMANENT.",
		"Three armies at the junkyard gates. %otherGen% brings theirs, the third faction brings theirs, I bring Salvage Tanks. We don't walk into %player%'s base. We RECYCLE it. The junkyard bites. The 'bites' are three armies. The 'three armies' is ' %player% is gone.' The 'gone' is recycled. The recycled is Forgotten. The Forgotten don't forget. The 'don't forget' is ' %player% is destroyed.'",
	},
},
	{name="Sky Captain Nagazi", doctrine="aircraft", taunts={
		"Salvage Choppers, scramble! We built them from crashed aircraft. Yours, mostly. Thank you for the donations.",
		"Forgotten air doctrine: we don't have factories. We have junkyards. The junkyards produce AIRCRAFT. Your factories produce targets.",
		"Wasps inbound. We named them 'Wasps' because they're small, angry, and they sting. The sting is missiles. The missiles are salvaged. From YOUR aircraft.",
		"My Salvage Chopper just took off. It's held together with duct tape and hatred. It shouldn't fly. It DOES fly. And it's bombing your base.",
		"You shot down one of my Wasps. My engineers are already at the crash site. They're building a new one from the wreckage. Of the one you shot down. Circle of life.",
		"Wasps swarming your base. They're flimsy. They're fast. They're made from scrap. And they're winning. Against your fancy air defense. Humiliating.",
		"My Wasp pilot says 'I learned to fly in a junkyard.' Your pilot went to academy. My pilot is still flying. Yours isn't. The junkyard teaches better.",
		"Salvage air wing, weapons free! Everything we have is rebuilt. Everything we fly is recycled. Everything we bomb is YOURS.",
		"I'm in ur junkyard, buildin ur d00dz. From YOUR scrap. Your dead units are my air force. Your wreckage is my runway. Your defeat is my victory. Upcycling at its finest.",
	},
	doubleTrouble={
		"Nagazi here. My Salvage Choppers scramble against %player%. Built from crashed aircraft. Yours, mostly. %otherGen% scrambles too. With... non-salvage aircraft, I assume. How privileged. Both are aimed at %player%. The salvage is held together with duct tape and hatred. The non-salvage is... held together with maintenance. The hatred is stronger. The hatred is ' %player% is destroyed.'",
		"Forgotten air doctrine: we don't have factories. We have junkyards. With %otherGen%? Junkyards AND %otherGen%'s factories. All aimed at %player%. The junkyards produce aircraft. The factories produce... targets. Both are deadly. The junkyard is angrier. The angrier is ' %player% is gone.'",
		"My Wasps are swarming %player%'s base. They're flimsy. They're fast. They're made from scrap. %otherGen% is also swarming, I assume. Both are swarming. The swarming is 'comprehensive.' %player%'s base is 'comprehensively destroyed.' The 'destroyed' is by scrap. The scrap is Forgotten.",
	},
	tripleTrouble={
		"Three armies! My Salvage Choppers scramble. %otherGen% scrambles. The third faction scrambles. All three scramble toward %player%. The salvage is from %player%'s own crashed aircraft. The irony is delicious. %otherGen% is... not ironic, I assume. All three are deadly. The ironic is deadlier. The deadlier is ' %player% is destroyed.' The 'destroyed' is by their own scrap. And three armies. The 'three armies' is PERMANENT.",
		"I'm in ur junkyard, buildin ur d00dz. With three armies, I'm in %player%'s junkyard, buildin THREE armies' d00dz. From %player%'s scrap. %otherGen% is also building. The third faction is also building. Everything is built from %player%'s failures. The 'failures' are ' %player% is gone.' The 'gone' is upcycled. The upcycled is Forgotten. The Forgotten don't forget. EVER.",
	},
},
	{name="Mutant Sage Fiona", doctrine="science", taunts={
		"Tiberium Mutation, accelerating! My units are mutating. In real-time. Getting stronger. Faster. Angrier. The mutation is from Tiberium. The Tiberium is everywhere. The 'everywhere' is my laboratory.",
		"Genetic Enhancement, online! I've modified my soldiers' DNA. They're 40% stronger. 40% faster. 40% more resistant to damage. The 40% is from Tiberium. The Tiberium is from the Earth. The Earth is MINE.",
		"Forgotten science doctrine: we don't have labs. We have TIBERIUM. The Tiberium IS our lab. It mutates. We adapt. Your scientists need funding. My scientists need EXPOSURE. The exposure is free. And permanent.",
		"Biomutation, deploying! My Mutant just evolved. He grew claws. And armor. And... a second head? The second head is unexpected. The claws are NOT. The claws are aimed at you.",
		"Tiberium Infusion, complete! My entire army is now infused with Tiberium. They heal in Tiberium fields. They're STRONGER in Tiberium fields. Your units DIE in Tiberium fields. The asymmetry is biological. And very Forgotten.",
		"Genetic Enhancement just made my Mutant 40% stronger. He was already stronger than your soldier. Now he's 40% MORE stronger. The math is 'your soldier is paste.' The paste is from Tiberium. The Tiberium is from the Earth. The Earth is MY lab.",
		"Biomutation just gave my infantry unit regeneration. He's healing. While fighting. While you're shooting him. He's healing FASTER than you're shooting. The 'faster' is from Tiberium. The Tiberium is from mutation. The mutation is from ME.",
		"Fiona here. I was a mutant before I was a sage. Now I'm both. The mutation is 'stronger, faster, angrier.' The sage is 'aiming the mutation at you.' Both are Forgotten. Both are from Tiberium. Both are PERMANENT.",
	},
	doubleTrouble={
		"Fiona here. My Tiberium Mutations make my army stronger in real-time. %otherGen% is also fighting. With... non-mutating units, I assume. How static. Both are aimed at %player%. My units are getting stronger every second. Theirs aren't. The 'getting stronger' is ' %player% is overwhelmed.' The 'overwhelmed' is very Forgotten. And very mutated.",
		"My Genetic Enhancement made my infantry 40% stronger. %otherGen% provides conventional firepower. Both are aimed at %player%. The 40% is from Tiberium. The conventional is from... factories. The Tiberium is free. The factories are not. The 'free' is deadlier. The 'deadlier' is ' %player% is destroyed.'",
		"Forgotten science doctrine: TIBERIUM is our lab. With %otherGen%? Tiberium AND whatever %otherGen% brings. All aimed at %player%. The 'whatever' is probably not Tiberium-powered. How clean. How fragile. The Tiberium is ' %player% is gone.' The 'gone' is biological. And permanent.",
	},
	tripleTrouble={
		"Three armies! My Tiberium Mutations accelerate. My Biomutations deploy. %otherGen% attacks. The third faction attacks. All four -- I mean three -- are aimed at %player%. The science is 'Tiberium mutation plus genetic enhancement plus two armies.' The equation is biological. The result is ' %player% is gone.' The 'gone' is from mutation. Very Fiona. Very Forgotten. Very permanent.",
		"Tiberium Mutation, Genetic Enhancement, Biomutation, and Tiberium Infusion. All my mutations. All aimed at %player%. %otherGen% adds their forces. The third faction adds theirs. I add BIOLOGY. The biology is 'mutate, enhance, regenerate, and infuse.' The 'mutate' is real-time. The 'enhance' is 40%. The 'regenerate' is while fighting. The 'infuse' is permanent. All four are from ME. From Tiberium. From the Forgotten. The 'from Forgotten' is ' %player% is GONE.' The 'gone' is biological. And mutated. And PERMANENT. Very Fiona. Very FINAL.",
	},
},
})

G("The Swarm", {
	{name="Overmind Consciousness", doctrine="infantry", taunts={
		"We are the Swarm. We are many. We are ONE. You are few. You are alone. The math is biological. And biological math always wins.",
		"Zerglings, spawn! They are small. They are fast. They are INFINITE. You will run out of bullets before we run out of bodies.",
		"The Overmind does not negotiate. The Overmind does not threaten. The Overmind CONSUMES. You are on the menu.",
		"Swarm doctrine: we do not have a 'plan.' We have a 'tide.' The tide does not plan. The tide ARRIVES. You are the sandcastle.",
		"Hydralisks, deploy! They spit acid through armor. Your tank's armor is... dissolving. The Hydralisk is not impressed. It's hungry.",
		"You killed 100 Zerglings. There are 10,000 more. You killed 1,000. There are 10,000 more. The number is always 10,000 more.",
		"My Zerglings just overran your bunker. Through the firing slit. One at a time. Very patient. Very deadly. Very Zerg.",
		"The Overmind sees your base. The Overmind sees your fear. The Overmind sees your DEATH. It's already calculating which of us eats first.",
		"Zerg rush kekekeke! You thought you had time to tech up? You NEVER have time. The rush is eternal. The rush is NOW. GG no re.",
		"We require more minerals. You require more MINES. Because my Zerglings just ate your mineral line. The minerals are now BIOMASS. Everything is biomass. You are biomass. Soon.",
	},
	doubleTrouble={
		"Overmind here. We are the Swarm. We are many. We are ONE. %otherGen% is... also many, I assume. But not ONE. How fragmented. Both are aimed at %player%. The Swarm does not negotiate. The Swarm CONSUMES. %otherGen% also consumes, I assume. The consuming is ' %player% is biomass.' The 'biomass' is SOON.",
		"Swarm doctrine: we do not have a 'plan.' We have a 'tide.' With %otherGen%? A tide AND... whatever %otherGen% has. All aimed at %player%. The 'whatever' is probably a plan. How quaint. The tide doesn't plan. The tide ARRIVES. %player% is the sandcastle. The sandcastle is 'gone.'",
		"You killed 100 Zerglings. There are 10,000 more. %otherGen% is also killing %player%'s forces, I assume. Both are aimed at %player%. The Zerglings are infinite. %otherGen% is... probably finite. How biological. The infinite is ' %player% is consumed.' The 'consumed' is by Zergling. The Zergling is ETERNAL.",
	},
	tripleTrouble={
		"Three armies. We are the Swarm. We are many. We are ONE. %otherGen% is many. The third faction is many. All three are aimed at %player%. The Swarm is infinite. The others are... finite, I assume. All three are deadly. The infinite is deadlier. The deadlier is ' %player% is consumed.' The 'consumed' is biomass. The biomass is SOON. The 'soon' is NOW.",
		"The Overmind calculates three armies. My Zerglings, %otherGen%'s forces, and a third faction. The calculation is simple: %player% divided by three armies equals ZERO. The zero is PERMANENT. The permanent is SWARM. The Swarm is ETERNAL. The eternal is HUNGRY. The hungry is NOW.",
	},
},
	{name="Broodmother Zagara", doctrine="tank", taunts={
		"Ultralisks, charge! They are the size of your building. They have blades the size of your car. They are NOT stopping.",
		"Swarm heavy doctrine: we don't build tanks. We GROW them. The Ultralisk is a tank that breathes. And it's angry. And it's HUGE.",
		"Ultralisk just stepped on your tank. Not a metaphor. It literally stepped on it. Your tank is now part of the ground. The Ultralisk didn't notice.",
		"Lurkers, burrow! They're underground. You can't see them. Your infantry is walking over them. They're about to become lunch. Underground lunch.",
		"My Ultralisk just charged through your wall. Through your tanks. Through your buildings. It's still charging. Stopping was never discussed.",
		"You're shooting my Ultralisk. It has more organic armor than your entire vehicle pool. Your shells are tickling it. The Ultralisk is not ticklish. It is ANGRY.",
		"Lurker just erupted under your infantry formation. They're gone. All of them. The ground opened. The ground ATE them.",
		"Zagara says: 'My Ultralisk is hungry. Your base is food. The arrangement is simple. The arrangement is FINAL.'",
		"My Ultralisk just walked through your wall. Not over it. Not around it. THROUGH it. The wall is now a doorway. The doorway is a Ultralisk-shaped hole. The Ultralisk is inside. The inside is where your base was. Was. Past tense.",
	},
	doubleTrouble={
		"Zagara here. My Ultralisks charge %player%. They are the size of buildings. %otherGen% charges too. With... smaller units, I assume. How quaint. Both are aimed at %player%. The Ultralisk steps on %player%'s tanks. The 'smaller units' go around. Both are deadly. The Ultralisk is deadlier. The deadlier is ' %player% is consumed.'",
		"Swarm heavy doctrine: we don't build tanks. We GROW them. With %otherGen%? Grown tanks AND %otherGen%'s built tanks. All aimed at %player%. The 'built' is manufactured. The 'grown' is biological. The biological is hungry. The hungry is ' %player% is gone.'",
		"My Lurkers just burrowed under %player%'s infantry formation. The ground opened. The ground ATE them. %otherGen% is also eating, I assume. Both are aimed at %player%. The eating is underground. The underground is Swarm. The Swarm is ' %player% is consumed.' The 'consumed' is from BELOW.",
	},
	tripleTrouble={
		"Three armies! My Ultralisks charge. %otherGen% charges. The third faction charges. All three charge toward %player%. The Ultralisk is the size of a building. The others are... smaller, I assume. All three are deadly. The biggest is deadlier. The deadlier is ' %player% is consumed.' The 'consumed' is by Ultralisk. The Ultralisk is PERMANENT. The permanent is HUNGRY.",
		"Three armies and an Ultralisk. The Ultralisk walks through %player%'s bunkers. %otherGen% walks through... something else. The third faction walks through whatever's left. Everything is walked through. The 'walked through' is ' %player% is gone.' The 'gone' is by Ultralisk. And two other armies. The 'gone' is PERMANENT. The permanent is SWARM.",
	},
},
	{name="Cerebrate Daggoth", doctrine="aircraft", taunts={
		"Mutalisks, fly! They spit acid. They fly. They reproduce in flight. The sky is FULL. And it's getting fuller.",
		"Swarm air doctrine: we don't build aircraft. We BIRTH them. The Mutalisk is born flying. And born angry. And born HUNGRY.",
		"Mutalisk just bombed your refinery with acid. The refinery is melting. The Mutalisk is circling. Waiting for it to finish. Patient. Hungry.",
		"Scourge inbound! They're suicide bombers with wings. They exist for ONE moment. The moment is 'your aircraft.' The moment is NOW.",
		"My Mutalisk just spawned two smaller Mutalisks. In mid-air. While shooting. The sky is getting MORE crowded. And MORE acidic.",
		"You launched a fighter. My Scourge intercepted it. The Scourge is gone. So is your fighter. The trade favors the Swarm. Always.",
		"Daggoth says: 'Air superiority is temporary. Swarm superiority is ETERNAL. Your aircraft are temporary. My Mutalisks are ETERNAL.'",
		"Mutalisk swarm detected on your radar. Your radar is overwhelmed. There are too many signatures. The signatures are all hostile. And all hungry.",
		"Leeroy Jenkins! That's what my Mutalisk pilot screamed before diving into your base. Alone. Without orders. He's already dead. But he took three of your buildings with him. WORTH IT.",
	},
	doubleTrouble={
		"Daggoth here. My Mutalisks fly toward %player%. They spit acid. They reproduce in flight. %otherGen% flies too. With... non-reproducing aircraft, I assume. How finite. Both are aimed at %player%. The Mutalisk spawns more Mutalisks in mid-air. The non-reproducing... doesn't. The reproducing is ' %player% is consumed.' The 'consumed' is from the SKY.",
		"Swarm air doctrine: we don't build aircraft. We BIRTH them. With %otherGen%? Birthed aircraft AND %otherGen%'s built aircraft. All aimed at %player%. The 'built' is manufactured. The 'birthed' is biological. The biological is hungry. The hungry is ' %player% is gone.' The 'gone' is from the sky. And from %otherGen%. And from the sky again. The sky is getting FULL.",
		"My Scourge just intercepted %player%'s fighter. The Scourge is gone. So is the fighter. %otherGen% is also intercepting, I assume. Both are aimed at %player%. The trade favors the Swarm. The trade ALWAYS favors the Swarm. The 'always' is ' %player% is consumed.' The 'consumed' is by suicide bomber with wings. The wings are SWARM.",
	},
	tripleTrouble={
		"Three armies! My Mutalisks fly. %otherGen% flies. The third faction flies. All three fly toward %player%. The Mutalisks reproduce in flight. The others... don't, I assume. All three are deadly. The reproducing is deadlier. The deadlier is ' %player% is consumed.' The 'consumed' is from the sky. The sky is full. The sky is getting fuller. The fuller is SWARM. The SWARM is PERMANENT.",
		"With three armies, the swarm is THREE TIMES as hungry. My Mutalisk dives into %player%'s base. %otherGen% also dives, I assume. The third faction also dives. Everything dives. The diving is ' %player% is gone.' The 'gone' is by Mutalisk. And by %otherGen%. And by a third faction. The 'gone' is PERMANENT. The permanent is SWARM. The SWARM is HUNGRY. ALWAYS.",
	},
},
	{name="Abathur", doctrine="science", taunts={
		"Evolution Chamber, active! I manipulate genes. I improve sequences. I make better Zerg. The 'better' is measured in 'kills per biomass.' The number is GROWING.",
		"Genetic Assimilation, complete! I consumed your dead units. I extracted their DNA. I incorporated their strengths into my Swarm. Your dead make my living STRONGER. The irony is biological. And delicious.",
		"Swarm science doctrine: we don't research. We EVOLVE. The evolution is forced. By me. In a pool. With biomass. The 'forced' is very Zerg. And very permanent.",
		"Mutagen, injecting! My Zerglings are now 30% larger. 30% faster. 30% angrier. The 30% is from biomass. The biomass is from YOUR units. The 'from your units' is very Abathur.",
		"Evolution Chamber just unlocked Ultralisk mutation. The Ultralisk is 5 meters tall. It has Kaiser Blades. The Blades cut through tanks. Through walls. Through EVERYTHING. The 'everything' includes you.",
		"Genetic Assimilation just incorporated your tank's armor gene into my Hydralisks. They're now armored. AND they spit acid. The acid dissolves armor. The armor resists dissolution. The combination is 'you lose.' The 'you lose' is from your own DNA.",
		"Mutagen just made my Banelings explosive. MORE explosive. They now dissolve armor AND infantry. Simultaneously. The 'simultaneously' is from biomass optimization. The 'optimization' is from ME. In a pool. With genes. Very Abathur.",
		"Abathur here. I am the evolution master. I take biomass. I make better Zerg. The 'better' is ' %player% is consumed.' The 'consumed' is genetic. And permanent. The 'permanent' is in the DNA. YOUR DNA. In MY pool.",
	},
	doubleTrouble={
		"Abathur here. My Evolution Chamber improves my Zerg in real-time. %otherGen% is also fighting. With... non-evolving units, I assume. How static. Both are aimed at %player%. My Zerg are getting stronger every second. Theirs aren't. The 'getting stronger' is ' %player% is overwhelmed.' The 'overwhelmed' is very Zerg. And very evolved.",
		"My Genetic Assimilation consumed %player%'s dead units. I incorporated their DNA. My Zerg are now stronger. From %player%'s own biomass. %otherGen% is also fighting. Both are aimed at %player%. The 'from %player%'s biomass' is ' %player% is destroyed by their own dead.' Very Abathur. Very Zerg.",
		"Swarm science doctrine: we EVOLVE. With %otherGen%? Evolution AND whatever %otherGen% brings. All aimed at %player%. The 'whatever' is probably not evolving. How static. How fragile. The evolution is ' %player% is gone.' The 'gone' is genetic. And permanent.",
	},
	tripleTrouble={
		"Three armies! My Evolution Chamber mutates. My Genetic Assimilation consumes. %otherGen% attacks. The third faction attacks. All four -- I mean three -- are aimed at %player%. The science is 'forced evolution plus biomass assimilation plus two armies.' The equation is biological. The result is ' %player% is consumed.' The 'consumed' is from a pool. Very Abathur. Very Zerg. Very permanent.",
		"Evolution Chamber, Genetic Assimilation, Mutagen, and Biomass Optimization. All my manipulations. All aimed at %player%. %otherGen% adds their forces. The third faction adds theirs. I add EVOLUTION. The evolution is 'mutate, consume, inject, and optimize.' The 'mutate' is forced. The 'consume' is from %player%'s dead. The 'inject' is Mutagen. The 'optimize' is biomass. All four are from ME. From the pool. From the Swarm. The 'from Swarm' is ' %player% is GONE.' The 'gone' is genetic. And consumed. And PERMANENT. Very Abathur. Very FINAL.",
	},
},
})

G("Protoss Armada", {
	{name="Executor Artanis", doctrine="tank", taunts={
		"Dragoons, advance! Cybernetic warriors in exoskeletons. They were Protoss. Now they are STEEL. And they remember being Protoss.",
		"Reavers, deploy! They fire scarabs. Living bombs. That chase you. Around corners. Through walls. Into your nightmares.",
		"Protoss armor doctrine: shields, plasma, and millennia of martial tradition. Your tradition is 'building walls.' Our tradition is 'breaking them.' With our minds. And plasma.",
		"Dragoon just crossed your perimeter. Its shield absorbed your first shot. The second shot. The third. The Dragoon is still walking. Patiently.",
		"Reaver just launched a scarab at your tank. The scarab turned a corner. Went around your wall. Found your tank. Hugged it. Exploded.",
		"My Dragoon's shield just regenerated. You damaged it 10 seconds ago. It's already back. Your damage is temporary. My shields are ETERNAL.",
		"Protoss technology is beyond your comprehension. You build tanks from metal. We build tanks from FAITH. And plasma. Mostly plasma.",
		"Reaver says nothing. Reaver IS nothing. Reaver is a factory that makes explosions. It's very good at its job. Your base is its workplace.",
		"En Taro Adun! My Dragoons have shields that regenerate. Your tanks have armor that... doesn't. Galaxy brain: bring shields to a gunfight. Shields win.",
		"Carriers = Instant Win. That's not my opinion. That's MATHEMATICS. I have Carriers. You have anti-air. The Carriers are still here. The anti-air is not. The math checks out.",
	},
	doubleTrouble={
		"Artanis here. My Dragoons march on %player% from one side. %otherGen% hits from the other. The shields absorb everything. %otherGen% provides the distraction. The Dragoons provide the DEATH. %player% is between. The 'between' is 'destroyed.' Very Protoss. Very permanent.",
		"Reaver just launched a scarab at %player%'s tank. Around a corner. Through a wall. %otherGen% is also attacking. Both are aimed at %player%. The scarab is persistent. %otherGen% is also persistent. The persistence is ' %player% is gone.' The 'gone' is from a living bomb. And from %otherGen%. Very thorough.",
		"Protoss armor doctrine: shields, plasma, and millennia of tradition. With %otherGen%? Shields, plasma, tradition, AND whatever %otherGen% brings. All aimed at %player%. The 'whatever' is probably not millennia-old. How young. How fragile. The millennia are ' %player% is destroyed.' The 'destroyed' is from Aiur. Very Artanis. Very permanent.",
	},
	tripleTrouble={
		"Three armies! My Dragoons march. %otherGen% attacks. The third faction attacks. All three are aimed at %player%. The shields regenerate. The others don't, I assume. All three are deadly. The regenerating is deadlier. The deadlier is ' %player% is consumed.' The 'consumed' is from Aiur. The Aiur is ETERNAL.",
		"Carriers = Instant Win. With three armies, Carriers = Even More Instant Win. My Carriers launch interceptors. %otherGen% launches... their own things. The third faction launches theirs. Everything launches at %player%. The sky is full. The ground is full. %player% is empty. The 'empty' is ' %player% is gone.' The 'gone' is from mathematics. The mathematics is PERMANENT.",
	},
},
	{name="High Templar Tassadar", doctrine="infantry", taunts={
		"Zealots, charge! They run at your tanks with psi-blades. Your tanks fire. The Zealots keep running. The psi-blades keep cutting.",
		"En Taro Adun! My warriors fight with honor. Your warriors fight with concrete. The honor gap is... significant.",
		"Protoss infantry doctrine: every Zealot is a warrior priest. Every warrior priest has energy blades. Every energy blade cuts through tanks. The equation is straightforward. The solution is your death.",
		"Zealot just reached your tank line. He cut through the armor. With his MIND. Your tank designer didn't account for 'mind swords.' He should have.",
		"My High Templar just cast Psionic Storm on your infantry. They're... gone. Not dead. GONE. There's a difference. The difference is 'nothing left.'",
		"You killed my Zealot. His shield regenerated. He got back up. He's still coming. He's VERY committed. And VERY angry. And VERY alive.",
		"Tassadar says: 'Your technology is impressive. Your courage is... adequate. Your life expectancy is NEGOTIABLE. The negotiation is over.'",
		"Dark Templar just decloaked behind your Construction Yard. He was there the whole time. You didn't know. Now you know. Too late.",
		"You must construct additional Pylons! Oh wait, that's the Protoss. We don't need pylons. We need PSI BLADES. And we have them. They're in your tanks. Right now. Cutting.",
	},
	doubleTrouble={
		"Tassadar here. My Zealots charge %player% with psi-blades. %otherGen% charges with... non-psi weapons, I assume. How conventional. Both are aimed at %player%. The psi-blades cut through tanks. The conventional... probably doesn't. Both are deadly. The psi is deadlier. The deadlier is ' %player% is gone.' The 'gone' is from Aiur.",
		"Psionic Storm on %player%'s infantry! They're gone. Not dead. GONE. %otherGen% is also attacking. Both are aimed at %player%. The storm is mental. %otherGen% is physical. %player% experiences both. The 'both' is ' %player% is destroyed.' The 'destroyed' is from THOUGHT. Very Tassadar. Very permanent.",
		"Protoss infantry doctrine: every Zealot is a warrior priest. With %otherGen%? Warrior priests AND %otherGen%'s soldiers. All aimed at %player%. The 'soldiers' are probably not warrior priests. How secular. How fragile. The warrior priests are ' %player% is gone.' The 'gone' is from honor. And psi-blades.",
	},
	tripleTrouble={
		"Three armies! My Zealots charge. %otherGen% attacks. The third faction attacks. All three are aimed at %player%. The psi-blades cut through tanks. The others... probably don't, I assume. All three are deadly. The psi is deadlier. The deadlier is ' %player% is consumed.' The 'consumed' is from honor. The honor is ETERNAL.",
		"Dark Templar just decloaked behind %player%'s Construction Yard. %otherGen% is at the front. The third faction is... somewhere. Everything is aimed at %player%. The Dark Templar was there the whole time. %player% didn't know. Now %player% knows. Too late. The 'too late' is ' %player% is gone.' The 'gone' is from the shadows. And from three armies. The shadows are PERMANENT.",
	},
},
	{name="Fleet Commander Selendis", doctrine="aircraft", taunts={
		"Carriers, launch! They don't fight directly. They launch interceptors. Tiny fighters. Endless fighters. The sky fills with them.",
		"Scouts, scramble! They're called 'Scouts' but they fight like battleships. Protoss naming conventions are... optimistic. For us.",
		"Protoss air doctrine: one Carrier is an air force. One Scout is a fighter wing. One Corsair is an annoyance. I have all three.",
		"Carrier just launched 8 interceptors. Each one is shooting your base. The Carrier is far away. Very far. Your anti-air can't reach it. The interceptors can reach YOU.",
		"Corsair just disrupted your air defenses. They're not firing. They're... confused. The Corsair finds this amusing. I find this amusing.",
		"My Scout just shot down two of your fighters. Simultaneously. It has twin blasters. Your fighter has one gun. Protoss engineering does not believe in 'fair.'",
		"Selendis says: 'For millennia we have defended the stars. You cannot defend a perimeter. The gap between us is not technological. It is CIVILIZATIONAL.'",
		"Carrier inbound. It's not a ship. It's a FACTORY. That flies. And makes fighters. That shoot your base. From the sky. Endlessly.",
		"My Carrier just launched 8 interceptors. Each one is shooting your base. The Carrier is far away. Very far. Your anti-air can't reach it. The interceptors can reach YOU. Also, Carriers = Instant Win. Just saying.",
	},
	doubleTrouble={
		"Selendis here. My Carriers launch interceptors at %player% from far away. %otherGen% attacks from... closer, I assume. How brave. How vulnerable. Both are aimed at %player%. The interceptors are endless. %otherGen% is... probably finite. The endless is ' %player% is consumed.' The 'consumed' is from the sky.",
		"Protoss air doctrine: one Carrier is an air force. With %otherGen%? An air force AND %otherGen%'s ground forces. All aimed at %player%. The ground forces are... touching. The air force is ETERNAL. The 'eternal' is ' %player% is gone.' The 'gone' is from above. Very Selendis. Very permanent.",
		"My Corsair just disrupted %player%'s air defenses. They're not firing. %otherGen% is rolling in while the defenses are down. The disruption is Protoss. The rolling is %otherGen%. Both are aimed at %player%. The 'both' is ' %player% is destroyed.' The 'destroyed' is from the sky. And from the ground. Simultaneously.",
	},
	tripleTrouble={
		"Three armies! My Carriers launch. %otherGen% attacks. The third faction attacks. All three are aimed at %player%. The interceptors are endless. The others are... not endless, I assume. All three are deadly. The endless is deadlier. The deadlier is ' %player% is consumed.' The 'consumed' is from the sky. The sky is Protoss. The Protoss are ETERNAL.",
		"Carrier inbound on %player%. It's not a ship. It's a FACTORY. That flies. %otherGen% is also inbound. The third faction is also inbound. Everything is inbound. %player% is the destination. The destination is ' %player% is gone.' The 'gone' is from interceptors. And from %otherGen%. And from a third faction. The interceptors are ENDLESS. The endless is PERMANENT.",
	},
},
	{name="Preserver Zamara", doctrine="science", taunts={
		"Psionic Storm, casting! Lightning from the mind. Not the sky. The MIND. Your units are being electrocuted by THOUGHT. The 'thought' is mine. The 'electrocuted' is yours.",
		"Khaydarin Crystal, activating! It amplifies psionic energy. My energy. By 500%. The 500% is from a crystal. The crystal is from Aiur. The Aiur is ancient. The ancient is DEADLY.",
		"Protoss science doctrine: we don't experiment. We REMEMBER. For millennia. Every battle. Every strategy. Every weakness. YOUR weakness is in our archives. Under 'easily resolved.'",
		"Mind Control, executing! Your unit is now mine. It's shooting YOUR base. With YOUR gun. The 'your gun' is very efficient. And very embarrassing. For you.",
		"Psionic Storm just struck your tank column. Five tanks. One storm. The tanks are... not tanks anymore. They're scrap. The scrap is from THOUGHT. The thought is from ME.",
		"Khaydarin Crystal just boosted my High Templar's energy by 500%. He can cast Psionic Storm five times. In a row. Without stopping. The 'five times' is 'your base is gone.' The 'gone' is from a crystal. Very old. Very deadly.",
		"Mind Control just turned your hero against you. The hero is attacking your own Construction Yard. The 'your own' is very Protoss. And very humiliating. The hero doesn't even remember being yours. The 'doesn't remember' is from ME.",
		"Preserver Zamara here. I carry the memories of a thousand Protoss. Every one of them destroyed an enemy like you. The 'every one' is statistical. The statistics are 'you lose.' The 'you lose' is from memory. Very old. Very precise. Very permanent.",
	},
	doubleTrouble={
		"Zamara here. My Psionic Storm strikes %player% from the mind. %otherGen% strikes from the physical. Both are aimed at %player%. The mental is invisible. The physical is visible. %player% sees the physical. %player% doesn't see the mental. The 'doesn't see' is ' %player% is destroyed.' The 'destroyed' is from thought. Very Protoss. Very permanent.",
		"My Mind Control turns %player%'s units against them. %otherGen% attacks from the front. Both are aimed at %player%. The 'against them' is ' %player% is shooting themselves.' The 'shooting themselves' is very Protoss. And very humiliating.",
		"Protoss science doctrine: we REMEMBER. With %otherGen%? Memory AND whatever %otherGen% brings. All aimed at %player%. The 'whatever' is probably not millennia-old. How young. How forgetful. The memory is ' %player% is gone.' The 'gone' is from a thousand ancestors. Very Zamara. Very permanent.",
	},
	tripleTrouble={
		"Three armies! My Psionic Storms strike. My Mind Controls turn. %otherGen% attacks. The third faction attacks. All four -- I mean three -- are aimed at %player%. The science is 'psionic power plus mind control plus two armies.' The equation is ancient. The result is ' %player% is gone.' The 'gone' is from memory. Very Zamara. Very Protoss. Very permanent.",
		"Psionic Storm, Khaydarin Crystal, Mind Control, and Preserver Memory. All my powers. All aimed at %player%. %otherGen% adds their forces. The third faction adds theirs. I add MEMORY. The memory is 'storm, amplify, control, and remember.' The 'storm' is mental. The 'amplify' is 500%. The 'control' is 'your units are mine.' The 'remember' is a thousand years of destroying enemies like %player%. All four are from ME. From Aiur. From the Protoss. The 'from Protoss' is ' %player% is GONE.' The 'gone' is from thought. And memory. And PERMANENT. Very Zamara. Very FINAL.",
	},
},
})

G("Terran Dominion", {
	{name="General Edmund Duke", doctrine="tank", taunts={
		"Siege Tanks, deploy! In siege mode they bombard from 3 screens away. You can't even SEE them. You can see the shells. Briefly.",
		"Arclite Cannon online. It's an artillery piece on treads. It transforms. It sieges. It ruins your day from the next ZIP CODE.",
		"Terran tank doctrine: if you can see my tank, you're already in range. If you CAN'T see my tank, you're already dead. The visibility is irrelevant.",
		"Siege Tank just entered siege mode. It's 2 screens away. Your base is in range. Your base is about to become a demolition site. Very efficient. Very Terran.",
		"My Siege Tank just shelled your War Factory. From across the map. The shell took 4 seconds to arrive. You had 4 seconds to panic. You used them poorly.",
		"Goliaths, walk! Anti-air AND anti-ground. On legs. It's a walker that does everything. Your turret does one thing. Poorly.",
		"You're trying to rush my Siege Tanks. They're in siege mode. They're shelling your approach. Your rush is now a retreat. The retreat is also being shelled.",
		"Duke says: 'I've been in more battles than you've had hot meals. My Siege Tanks are the LEAST of your problems. The LEAST. Think about that.'",
	},
	doubleTrouble={
		"Duke here. My Siege Tanks bombard %player% from 3 screens away. %otherGen% attacks from closer. How reckless. Both are aimed at %player%. The shells take 4 seconds to arrive. %otherGen% arrives faster. Both are deadly. The shells are deadlier. The deadlier is ' %player% is gone.' The 'gone' is from the next zip code.",
		"Terran tank doctrine: if you can see my tank, you're in range. With %otherGen%? My tanks AND %otherGen%'s forces. All aimed at %player%. %otherGen% can probably be seen. My tanks cannot. The unseen is ' %player% is destroyed.' The 'destroyed' is from artillery. Very Duke. Very permanent.",
		"My Goliaths just walked into %player%'s base. Anti-air AND anti-ground. On legs. %otherGen% is also walking in. Both are aimed at %player%. The Goliaths do everything. %otherGen% does... one thing, I assume. The 'everything' is ' %player% is gone.' The 'gone' is from legs. And cannons.",
	},
	tripleTrouble={
		"Three armies! My Siege Tanks bombard from 3 screens away. %otherGen% attacks. The third faction attacks. All three are aimed at %player%. The shells are from the next zip code. The others are from... closer, I assume. All three are deadly. The distant is deadlier. The deadlier is ' %player% is consumed.' The 'consumed' is from artillery. The artillery is TERRAN.",
		"Arclite Cannon online. With three armies, the Cannon is backed by %otherGen% AND a third faction. All aimed at %player%. The Cannon transforms. It sieges. It ruins %player%'s day from the next ZIP CODE. %otherGen% ruins from closer. The third faction ruins from... wherever. Everything ruins %player%. The 'ruins' is ' %player% is gone.' The 'gone' is from the next zip code. And from three armies. The zip code is PERMANENT.",
	},
},
	{name="Marshal Jim Raynor", doctrine="infantry", taunts={
		"Marines, stim up! They inject combat stimulants and fire faster. Side effects include: tremors, aggression, and WINNING.",
		"Terran infantry doctrine: Marines are numerous, reckless, and completely hopped up on combat drugs. It's not a strategy. It's a LIFESTYLE.",
		"Firebats, deploy! They have flamethrowers. On armor. They walk up to your infantry and set them on fire. Very personal. Very effective.",
		"Marine just used Stimpack. He's firing much faster. He's also vibrating. The vibration is normal. The firing is not. For you.",
		"Ghost just called a Nuke on your base. You have 10 seconds. The Ghost is already gone. The Nuke is not. Good luck.",
		"My Medics are healing my Marines. Your Marines are dying. The trade is bad. For you. The Medics are very good at their jobs.",
		"Raynor says: 'I've been fighting since the Confederacy. I've fought Zerg. I've fought Protoss. You're not Zerg. You're not Protoss. You're a warm-up. A light stretch before the real fight. Don't worry -- it'll be over fast.'",
		"Firebat just walked up to your infantry garrison. He set the building on fire. The infantry are evacuating. Into my Marines' firing line. Oops.",
		"Ghost just called a Nuke on your base. Nuclear launch detected. You have 10 seconds. Never gonna give you up, never gonna let you survive. Get nuked.",
	},
	doubleTrouble={
		"Raynor here. My Marines stim up and charge %player%. %otherGen% is also charging. With... non-stimmed units, I assume. How sober. Both are aimed at %player%. The stimpacks make them fire faster. The 'faster' is ' %player% is overwhelmed.' The 'overwhelmed' is from combat drugs. And from %otherGen%. Very Terran.",
		"Ghost just called a Nuke on %player%'s base. %otherGen% is also attacking. Both are aimed at %player%. The Nuke takes 10 seconds. %otherGen% is... immediate, I assume. Both are deadly. The Nuke is deadlier. The deadlier is ' %player% is gone.' The 'gone' is from a Ghost. And from a nuke. Very Raynor. Very permanent.",
		"Terran infantry doctrine: Marines are numerous, reckless, and on combat drugs. With %otherGen%? Reckless Marines AND %otherGen%'s forces. All aimed at %player%. The 'forces' are probably not on combat drugs. How restrained. How fragile. The Marines are ' %player% is destroyed.' The 'destroyed' is from stimpacks. And from %otherGen%.",
	},
	tripleTrouble={
		"Three armies! My Marines stim up. %otherGen% attacks. The third faction attacks. All three are aimed at %player%. The stimpacks make them fire faster. The others... probably don't, I assume. All three are deadly. The stimmed is deadlier. The deadlier is ' %player% is consumed.' The 'consumed' is from combat drugs. The combat drugs are TERRAN.",
		"Ghost just called a Nuke on %player%'s base. Nuclear launch detected. %otherGen% is also attacking. The third faction is also attacking. Everything is aimed at %player%. The Nuke takes 10 seconds. The others are immediate. %player% has 10 seconds. The '10 seconds' is ' %player% is gone.' The 'gone' is from a nuke. And from three armies. The nuke is PERMANENT.",
	},
},
	{name="Commodore Matt Horner", doctrine="aircraft", taunts={
		"Wraiths, scramble! They cloak. You can't see them. They can see you. They're shooting you. You're shooting at nothing. This is frustrating. For you.",
		"Battlecruiser, inbound! It's a capital ship. On the ground. It has a Yamato Cannon. The Yamato Cannon is a small star. Aimed at your base.",
		"Terran air doctrine: cloak, bombard, repeat. The Wraiths cloak. The Battlecruiser bombs. You can't counter what you can't see OR what you can't reach.",
		"Wraith just decloaked, fired, and recloaked. Your anti-air shot went through where it WAS. The Wraith is already somewhere else. Shooting again.",
		"Battlecruiser on approach. It's the size of a building. It flies. It has a Yamato Cannon. The Yamato Cannon fires once. Your base is... lighter.",
		"Valkyrie inbound! It fires missiles at your aircraft. Many missiles. ALL the missiles. Your pilot is very concerned. Briefly.",
		"Horner says: 'I've commanded fleets across the Koprulu Sector. You're having trouble with a perimeter defense. I'm not worried. I'm BORED.'",
		"My Wraith just called in a Battlecruiser strike on your Construction Yard. The Yamato Cannon is charging. You have 8 seconds. I'd use them to cry.",
	},
	doubleTrouble={
		"Horner here. My Wraiths cloak and hunt %player%'s units. %otherGen% attacks openly. How honest. How vulnerable. Both are aimed at %player%. The Wraiths are invisible. %otherGen% is... very visible, I assume. Both are deadly. The invisible is deadlier. The deadlier is ' %player% is gone.' The 'gone' is from the sky. And from the shadows.",
		"Battlecruiser on approach to %player%'s base. Yamato Cannon charging. %otherGen% is also approaching. Both are aimed at %player%. The Cannon fires once. %otherGen% fires... repeatedly, I assume. Both are deadly. The once is deadlier. The deadlier is ' %player% is destroyed.' The 'destroyed' is from a small star. Very Horner. Very permanent.",
		"Terran air doctrine: cloak, bombard, repeat. With %otherGen%? Cloak, bombard, repeat, AND %otherGen%'s forces. All aimed at %player%. The 'forces' are probably not cloaked. How exposed. How fragile. The cloak is ' %player% is gone.' The 'gone' is from Wraiths. And from a Battlecruiser. And from %otherGen%.",
	},
	tripleTrouble={
		"Three armies! My Wraiths cloak and hunt. %otherGen% attacks. The third faction attacks. All three are aimed at %player%. The Wraiths are invisible. The others are... visible, I assume. All three are deadly. The invisible is deadlier. The deadlier is ' %player% is consumed.' The 'consumed' is from the sky. The sky is TERRAN. The Terran are ETERNAL.",
		"Battlecruiser on approach to %player%'s base. Yamato Cannon charging. %otherGen% is also approaching. The third faction is also approaching. Everything is aimed at %player%. The Cannon fires once. The others fire repeatedly. %player% experiences both. The 'both' is ' %player% is gone.' The 'gone' is from a small star. And from three armies. The star is PERMANENT.",
	},
},
	{name="Dr. Ariel Hanson", doctrine="science", taunts={
		"Medivac, deploying! It heals my infantry. In the field. While fighting. Your infantry die. Mine HEAL. The asymmetry is medical. And very Terran.",
		"Stim Pack, injecting! My Marines are 50% faster. 50% stronger. The side effect is 'shorter lifespan.' The lifespan is shorter because they're fighting. The fighting is FASTER. The 'faster' is 'you die quicker.'",
		"Terran science doctrine: we don't have ancient crystals or biological mutation. We have MEDICINE. And stimulants. And prototype weapons. The medicine heals. The stimulants enhance. The weapons destroy. All three are funded.",
		"Experimental Weapon, testing! It's a prototype. It might explode. It might not. It's firing at your base. The 'might explode' is a risk I'm willing to take. With YOUR base.",
		"Medivac just healed my Marine from 10% health to 100%. In 3 seconds. While under fire. Your Marine is still at 10%. The 'still at 10%' is 'dead.' The 'dead' is very Terran. And very medical.",
		"Stim Pack just made my Marines 50% faster. They're shooting 50% more bullets. Your position is receiving 50% more bullets. The '50% more' is from a needle. The needle is from science. The science is from ME.",
		"Experimental Weapon just fired at your base. It worked! The 'worked' is 'your bunker is gone.' The 'gone' is from a prototype. The prototype is from R&D. The R&D is from funding. The funding is from the Dominion. The Dominion is PERMANENT.",
		"Dr. Hanson here. I'm a biologist and a weapons designer. The biology heals my troops. The weapons destroy yours. The combination is 'my troops live, yours die.' The 'live vs die' is very scientific. And very Terran. And very funded.",
	},
	doubleTrouble={
		"Hanson here. My Medivacs keep my infantry alive against %player%. %otherGen% is also fighting. With... non-healing units, I assume. How fragile. Both are aimed at %player%. My infantry heal. Theirs don't. The 'heal' is ' %player% is overwhelmed.' The 'overwhelmed' is very Terran. And very medical.",
		"My Stim Packs make my Marines 50% faster against %player%. %otherGen% provides conventional firepower. Both are aimed at %player%. The 50% is from a needle. The conventional is from... training. The needle is faster. The 'faster' is ' %player% is destroyed.'",
		"Terran science doctrine: MEDICINE and prototype weapons. With %otherGen%? Medicine, prototypes, AND whatever %otherGen% brings. All aimed at %player%. The 'whatever' is probably not medic-backed. How fragile. The medicine is ' %player% is gone.' The 'gone' is from a PhD. Very Hanson. Very funded. Very permanent.",
	},
	tripleTrouble={
		"Three armies! My Medivacs heal. My Stim Packs enhance. %otherGen% attacks. The third faction attacks. All four -- I mean three -- are aimed at %player%. The science is 'medical support plus stimulants plus two armies.' The equation is funded. The result is ' %player% is gone.' The 'gone' is from a PhD. In biology. Very Hanson. Very Terran. Very permanent.",
		"Medivac, Stim Pack, Experimental Weapons, and Dominion funding. All my designs. All aimed at %player%. %otherGen% adds their forces. The third faction adds theirs. I add SCIENCE. The science is 'heal, enhance, test, and fund.' The 'heal' is Medivac. The 'enhance' is Stim. The 'test' is Experimental. The 'fund' is Dominion. All four are from ME. From the Dominion. From Terran R&D. The 'from Terran' is ' %player% is GONE.' The 'gone' is medical. And enhanced. And PERMANENT. Very Hanson. Very FINAL.",
	},
},
})

G("Ordos Sabotage", {
	{name="Executrix Hark Halleck", doctrine="tank", taunts={
		"Ordos doesn't fight fair. We fight SMART. Stealth Raiders, Laser Tanks, and a very unhealthy dose of sabotage.",
		"My Stealth Raider just decloaked inside your base. It didn't knock. Ordos doesn't knock.",
		"Laser Tanks online. They cut through armor like a hot knife through butter. A VERY EXPENSIVE hot knife. Paid for by House Ordos.",
		"Ordos tank doctrine: if you can see it, you can kill it. If you CAN'T see it, it's already killing you.",
		"Python Tank just fired its cannon at your defenses. The shell went through the wall, through the building, through the OTHER wall. Very thorough.",
		"My Deviator Tank just turned your tank against you. Your tank is shooting your OWN base. I find this hilarious. You don't.",
		"Stealth Raider decloaking next to your refinery. BOOM. Recloaking. Gone. Your refinery is gone. The Raider is already at your next refinery.",
		"You built defenses against tanks. My Stealth Raiders aren't tanks. They're GHOSTS. Ghosts with cannons. Ordos ghosts.",
		"Stealth Raiders: invisible, deadly, and completely unfair. Is it balanced? No. Do I care? Also no. Ordos pays for balance. We don't.",
	},
	doubleTrouble={
		"Halleck here. My Stealth Raiders decloak inside %player%'s base. %otherGen% is at the front. The Raiders hit from inside. %otherGen% hits from outside. %player% is between. The 'between' is 'destroyed.' The 'destroyed' is very Ordos. Very expensive. Very final.",
		"Ordos tank doctrine: if you can see it, you can kill it. If you CAN'T see it, it's already killing you. With %otherGen%? You can see %otherGen%. You can't see ME. You're being killed by both. The 'both' is 'everywhere.' The 'everywhere' is ' %player% is gone.'",
		"My Deviator Tank just turned %player%'s tank against them. %otherGen% is watching. Amused, I assume. The tank is shooting %player%'s own base. %otherGen% is also shooting %player%'s base. The base is being shot by its own tank AND %otherGen%. Very efficient. Very Ordos.",
	},
	tripleTrouble={
		"Three armies! My Stealth Raiders decloak. %otherGen% attacks. The third faction attacks. All three are aimed at %player%. The Raiders are invisible. The others are visible. %player% can see two armies. %player% can't see the third. The third is ME. The third is the one that kills %player%.",
		"My Deviator just turned %player%'s tank against them. %otherGen% is shooting too. The third faction is shooting too. The tank is shooting its own base. Three armies AND %player%'s own tank. All aimed at %player%. The 'all' is 'everything.' The 'everything' is Ordos. The Ordos is ' %player% is destroyed.'",
	},
},
	{name="Commander Sardaukar", doctrine="infantry", taunts={
		"Ordos infantry advancing. Light Infantry with lasercartridges. Your troops have bullets. Mine have SCIENCE.",
		"Rocket Troopers, fire! Your tanks are about to meet Ordos engineering. The meeting will be brief. And explosive.",
		"Ordos infantry doctrine: we don't have the most soldiers. We have the most EXPENSIVE soldiers. Quality over quantity. Always.",
		"My Light Infantry just took cover in your Tiberium field. They're healing. Your troops are dying. Ordos biology is superior.",
		"Saboteur deployed. He's in your base. He's touching your buildings. He's smiling. Ordos saboteurs ALWAYS smile.",
		"Rocket Trooper just hit your tank from maximum range. Your tank can't shoot back. The range is the point. The range is Ordos.",
		"You sent infantry to fight my infantry. My infantry has better weapons, better armor, and better dental. Ordos provides.",
		"My Saboteur just rigged your Power Plant. The timer is running. I'd evacuate. You won't. You're too proud. Ordos loves pride. It's exploitable.",
	},
	doubleTrouble={
		"Sardaukar here. My Light Infantry advance on %player% with lasercartridges. %otherGen% advances too. With... bullets, I assume. How quaint. Both are aimed at %player%. The lasers are science. The bullets are... bullets. Both kill %player%. The lasers kill FASTER.",
		"My Saboteur just rigged %player%'s Power Plant. The timer is running. %otherGen% is at the front gate. The gate is a distraction. The timer is the main event. The main event is 'boom.' The 'boom' is Ordos. The Ordos is ' %player% has no power.'",
		"Ordos infantry doctrine: we don't have the most soldiers. We have the most EXPENSIVE soldiers. With %otherGen%? We have expensive soldiers AND %otherGen%'s soldiers. Quality AND quantity. %player% has neither. The 'neither' is fatal.",
	},
	tripleTrouble={
		"Three armies! My Light Infantry advance. %otherGen% advances. The third faction advances. All three are aimed at %player%. The lasercartridges are science. The others are... whatever. All three are deadly. The deadly is ' %player% is gone.' The 'gone' is expensive. The 'expensive' is Ordos.",
		"My Saboteur rigged %player%'s Power Plant. %otherGen% is at the gate. The third faction is... also at the gate, presumably. The timer is running. The gate is being breached. The power is going out. Everything happens at once. The 'once' is ' %player% is destroyed.' The 'destroyed' is Ordos. The Ordos is PERMANENT.",
	},
},
	{name="Navigator Arakis", doctrine="aircraft", taunts={
		"Ordos air support inbound. We don't have many aircraft. What we have is PRECISE. Like a scalpel. A scalpel that explodes.",
		"Dust Drones scouting your base. You can't see them. They can see EVERYTHING. Your base layout is now on my desk. I'm reviewing it. With coffee.",
		"Ordos air doctrine: we don't dominate the sky. We INVESTIGATE the sky. Then we destroy what we investigated. Very methodical. Very Ordos.",
		"My Dust Drone just mapped your entire base in 4 seconds. The data is being processed. The processing results in 'destroy everything.' Very efficient.",
		"You built anti-air. My Dust Drone evaded it. The Drone is small. The Drone is fast. The Drone is VERY Ordos.",
		"Ornithopter strike inbound. Yes, we have flying dinosaurs. No, you can't have one. Yes, it's bombing your base. No, you can't stop it.",
		"My air wing is small but PREMIUM. Each pilot costs more than your entire barracks. Ordos invests in quality. Quality is expensive. So is losing. You're losing.",
		"Dust Drone reports your Construction Yard is undefended from the north. I'm sending something from the north. Guess what it is. Hint: it explodes.",
	},
	doubleTrouble={
		"Arakis here. My Dust Drones scout %player%'s base. %otherGen% is scouting too, I assume. Both scouts report: %player% is weak. The report is on my desk. With coffee. %otherGen%'s report is... also probably on a desk. Both desks say 'destroy %player%.' The coffee is good. The destroying is BETTER.",
		"Ordos air doctrine: we don't dominate the sky. We INVESTIGATE the sky. Then we destroy what we investigated. With %otherGen%? We investigate, %otherGen% destroys, OR vice versa. Both are aimed at %player%. The investigation is thorough. The destruction is METHODICAL. The methodical is Ordos.",
		"My Ornithopter strike is inbound on %player%. Yes, flying dinosaurs. %otherGen% is also inbound. With... non-dinosaurs, I assume. Both are aimed at %player%. The dinosaurs are precise. The non-dinosaurs are... adequate. Both are ' %player% is destroyed.'",
	},
	tripleTrouble={
		"Three armies! My Dust Drones scout. %otherGen% attacks. The third faction attacks. All three are aimed at %player%. The Drones report: %player% is surrounded. The report is accurate. The surrounding is 'three armies.' The 'three armies' is ' %player% is destroyed.' The report is filed. The filing is Ordos.",
		"Dust Drone reports %player%'s Construction Yard is undefended from the north. I'm sending something from the north. %otherGen% is sending something from the south. The third faction is sending something from... somewhere. All three are 'something that explodes.' The 'something' is aimed at %player%. The 'aimed' is 'destroyed.' The 'destroyed' is PERMANENT.",
	},
},
	{name="Executor Bijaz", doctrine="stealth", taunts={
		"Stealth Raiders, decloak! You didn't see them. That's the last thing you won't see. The cloaking is Ordos. The cloaking is EXPENSIVE. The expensive is worth it. Because you're dead.",
		"Spy infiltrating your base. He looks like your own engineer. He's been there for 10 minutes. He's accessing your radar. He's reading your build orders. He's laughing. The laughing is very Ordos.",
		"Ordos stealth doctrine: we don't fight fair. We don't fight at all. We SABOTAGE. The sabotage is invisible. The result is not. The result is 'your base is on fire.' The fire is very visible.",
		"Stealth Raider just decloaked next to your Construction Yard. BOOM. Recloaked. Gone. The Yard is gone. The Raider is already at your next target. The 'next' is NOW.",
		"My Spy just disabled your radar. You can't see anything. I can see everything. The asymmetry is Ordos. The asymmetry is EXPENSIVE. The expensive is 'you lose.'",
		"Subterfuge unit deploying! It looks like a rock. It's not a rock. It's a bomb. A very expensive bomb. That looks like a rock. Your tank just drove over it. The tank is gone. The rock was Ordos.",
		"Cloaking Field active! My entire army is now invisible. You can hear them. You can't see them. The hearing is 'explosions getting closer.' The seeing is 'nothing.' The 'nothing' is about to become 'everything.'",
		"Bijaz here. I was a spy before I was an executor. Now I'm both. The spy is 'invisible.' The executor is 'expensive.' Both are Ordos. Both are aimed at you. The 'aimed' is invisible. The 'result' is not.",
	},
	doubleTrouble={
		"Bijaz here. My Stealth Raiders decloak inside %player%'s base. Invisible. %otherGen% attacks from the front. The front is loud. The stealth is quiet. %player% watches the front. The stealth is behind them. The 'behind them' is 'destroyed.' Very Ordos. Very expensive. Very quiet.",
		"My Spy disabled %player%'s radar. %otherGen% attacks from the blind spot. The blind spot is 'everywhere.' The everywhere is ' %player% is destroyed.' The 'destroyed' is invisible. The invisible is Ordos.",
		"Ordos stealth doctrine: we don't fight fair. With %otherGen%? We don't fight fair AND %otherGen% fights... fairly? How quaint. Both are aimed at %player%. The unfair is deadlier. The unfair is Ordos. The Ordos is ' %player% is gone.' The 'gone' is invisible. And permanent.",
	},
	tripleTrouble={
		"Three armies! My Stealth Raiders decloak. %otherGen% attacks. The third faction attacks. All three are aimed at %player%. The stealth is invisible. The others are visible. %player% can see two armies. %player% can't see the third. The third is ME. The third is the one that plants charges. The charges are ' %player% is gone.' The 'gone' is invisible. And very expensive. And very Ordos.",
		"My Cloaking Field hides my entire army. %otherGen% attacks from the front. The third faction attacks from the side. My army attacks from... nowhere. The 'nowhere' is 'everywhere.' The 'everywhere' is ' %player% is surrounded.' The 'surrounded' is 'destroyed.' The 'destroyed' is invisible. The invisible is PERMANENT. Very Bijaz. Very Ordos.",
	},
},
})

G("Ixian Technocracy", {
	{name="Master Researcher Tleilaxu", doctrine="tank", taunts={
		"Ixian cymek walkers advancing. They're machines with HUMAN brains. The brains are volunteers. Mostly. The definition of 'volunteer' is... flexible.",
		"Koda Tanks, march! They walk on four legs. Like spiders. With cannons. Very large spider cannons. Ixian engineering at its finest.",
		"Heavy Koda Tanks deploying. Twice the legs. Twice the cannons. Twice the nightmares. For you. For me, they're just Tuesday.",
		"Ixian tank doctrine: we don't use treads. Treads are primitive. LEGS are the future. Legs can go where treads can't. Like OVER your walls.",
		"My Koda Tank just walked over your minefield. The legs are too narrow for the mines. Your minefield is now a walking path. For my tanks.",
		"Ix Siege Tank bombarding your base from beyond visual range. You can't see it. You can see the shells. The shells are very visible. Briefly.",
		"Railgun Drone deployed. It fires a tungsten rod at Mach 6. Your armor is... not rated for Mach 6. Nothing is rated for Mach 6. That's the point.",
		"My Heavy Koda just kicked your tank. It KICKED it. With a LEG. The tank fell over. The Koda is still walking. Ixian engineering doesn't stumble.",
	},
	doubleTrouble={
		"Tleilaxu here. My Koda Tanks march on %player% with four legs and cannons. %otherGen% marches too. With... treads, I assume. How primitive. Both are aimed at %player%. The legs go over walls. The treads go... into walls. The legs win. The treads are adequate. Both are deadly.",
		"Ixian tank doctrine: we don't use treads. LEGS are the future. With %otherGen%? Legs AND whatever %otherGen% uses. All aimed at %player%. The 'whatever' is probably treads. How sad. The legs step over %player%'s walls. The treads hit the walls. The walls lose either way.",
		"My cymek walker just entered %player%'s base. It has a human brain. The brain is a volunteer. Mostly. %otherGen% is also entering. With... non-volunteer brains, I assume. Both are aimed at %player%. The volunteer brain is smarter. The non-volunteer brain is... adequate. Both are deadly.",
	},
	tripleTrouble={
		"Three armies! My Koda Tanks march. %otherGen% marches. The third faction marches. All three march toward %player%. The legs go over walls. The others go... through walls, presumably. All three are deadly. The legs are deadlier. The deadlier is ' %player% is destroyed.' The 'destroyed' is Ixian. The Ixian is PERMANENT.",
		"My Railgun Drone fires a tungsten rod at Mach 6 at %player%. %otherGen% is also firing, I assume. The third faction is also firing. Everything is firing. The rod is Mach 6. The 'whatever' is... slower, probably. All are aimed at %player%. The 'all' is ' %player% is gone.' The 'gone' is at Mach 6. Very Ixian. Very fast. Very final.",
	},
},
	{name="Chief Scientist Ardel", doctrine="infantry", taunts={
		"Ixian Light Infantry deploying. Each one carries a personal shield. Your infantry carry rifles. The comparison is... educational.",
		"Shock Infantry advancing! They carry EMP charges. Your tanks are about to become very expensive paperweights. Very STILL paperweights.",
		"Ixian infantry doctrine: every soldier is a research investment. We don't send grunts. We send prototypes. The prototypes are armed.",
		"Storm Infantry deploying. They carry lightning rifles. Yes, lightning. In a rifle. Ixian science doesn't respect 'impossible.' It respects 'profitable.'",
		"My Light Infantry's personal shield just absorbed your tank shell. The shield is fine. The infantry is fine. Your tank is about to NOT be fine.",
		"Shock Infantry just EMP'd your tank column. They're not moving. They're not shooting. They're very still. And very expensive. And very useless. Now.",
		"Twin Rocket Troopers, fire! Two rockets per soldier. Your air units are about to learn about Ixian air defense. The lesson is 'explosive.'",
		"You killed my Shock Infantry. His shield collapsed. His EMP charge detonated posthumously. Your nearby tank is now also dead. Ixian science: effective even in death.",
	},
	doubleTrouble={
		"Ardel here. My Light Infantry deploy against %player% with personal shields. %otherGen% deploys too. With... no shields, I assume. How quaint. Both are aimed at %player%. The shields absorb. The no-shields... don't. Both are deadly. The shields are deadlier. The deadlier is Ixian.",
		"My Shock Infantry just EMP'd %player%'s tank column. They're not moving. %otherGen% is moving. The moving is toward %player%. The not-moving is %player%'s tanks. The differential is ' %player% loses.' The differential is Ixian.",
		"Ixian infantry doctrine: every soldier is a research investment. With %otherGen%? Research investments AND whatever %otherGen% sends. All aimed at %player%. The 'whatever' is probably not a research investment. How sad. The research is expensive. The expensive is deadly. The deadly is ' %player% is gone.'",
	},
	tripleTrouble={
		"Three armies! My Light Infantry deploy with shields. %otherGen% deploys. The third faction deploys. All three are aimed at %player%. The shields absorb everything. The others... don't absorb, I assume. All three are deadly. The shields are deadlier. The deadlier is ' %player% is destroyed.' The 'destroyed' is Ixian. The Ixian is PERMANENT.",
		"My Shock Infantry EMP'd %player%'s tanks. %otherGen% is shooting the stationary tanks. The third faction is also shooting. The tanks are not moving. The shooting is continuous. The 'continuous' is ' %player% is gone.' The 'gone' is Ixian. The Ixian is effective even in death. Especially in death.",
	},
},
	{name="Flight Commander Arich", doctrine="aircraft", taunts={
		"Ixian air wing deploying. We don't have many aircraft. What we have is ADVANCED. Each one costs more than your entire base. And it shows.",
		"Farasha gunships inbound. They're named after a Dune butterfly. A butterfly with missiles. And a chaingun. The metaphor is 'beautiful death.'",
		"Ixian air doctrine: quality over quantity. One Farasha can do the work of ten of your aircraft. It also costs as much. But I'm not paying. So I don't care.",
		"Railgun Drone overhead. It's firing from the sky. With a railgun. At your base. From the SKY. Your anti-air can't hit it. It's too small. Too fast. Too Ixian.",
		"Farasha just did a strafing run on your refinery. The refinery is on fire. The Farasha is already gone. The fire is NOT gone. The fire is very present.",
		"Resonance Drone deployed. It emits a frequency that cracks armor. Your tanks are vibrating. The vibration is 'bad for them.' Very bad. Very Ixian.",
		"My air wing is small but each unit has THREE weapons. Your aircraft has one. Ixian engineering believes in 'more.' More is better. More is Ixian.",
		"Storm Raider just entered your airspace. It's fast. It's shielded. It's shooting. Your anti-air is shooting at the shield. The shield is bored.",
	},
	doubleTrouble={
		"Arich here. My Farasha gunships inbound on %player%. They're named after a Dune butterfly. With missiles. %otherGen% is also inbound. With... non-butterfly aircraft, I assume. Both are aimed at %player%. The butterfly is beautiful. The butterfly is deadly. The non-butterfly is... adequate.",
		"Ixian air doctrine: quality over quantity. With %otherGen%? Quality AND quantity. All aimed at %player%. Each Farasha costs more than %player%'s entire base. %otherGen%'s aircraft cost... less, I assume. How quaint. Both are deadly. The expensive is deadlier.",
		"My Railgun Drone is overhead %player%'s base. Firing from the sky. With a railgun. %otherGen% is also overhead, I assume. Both are shooting down. The 'down' is %player%. The 'down' is 'destroyed.' The 'destroyed' is Ixian. The Ixian is from the SKY.",
	},
	tripleTrouble={
		"Three armies! My Farashas fly. %otherGen% flies. The third faction flies. All three fly toward %player%. The butterfly is beautiful. The others are... not butterflies, I assume. All three are deadly. The butterfly is deadlier. The deadlier is ' %player% is destroyed.' The 'destroyed' is beautiful. And permanent.",
		"My Storm Raider entered %player%'s airspace. Shielded. Fast. Shooting. %otherGen% is also shooting. The third faction is also shooting. Everything is shooting. The shield is bored. The 'bored' is Ixian. The 'shooting' is everyone. The 'everyone' is aimed at %player%. The 'aimed' is 'destroyed.'",
	},
},
	{name="Researcher Heli X", doctrine="science", taunts={
		"Cymek Factory, online! Machines with HUMAN brains. The brains are volunteers. The definition of 'volunteer' is... flexible. The machines don't complain. The brains do. Quietly. The 'quietly' is Ixian.",
		"Tleilaxu Flesh Vat, active! We grow soldiers. In vats. From cells. Your soldiers are born. Mine are MANUFACTURED. The manufacturing is faster. And more precise. And more Ixian.",
		"Ixian science doctrine: we don't discover technology. We DESIGN it. With computers. And ethics committees. The ethics committees are... optional. The computers are not. The technology is 'your base explodes.' The 'how' is proprietary.",
		"Prescience Device, calibrating! It predicts the future. The future says 'you lose.' The prediction is 99.7% accurate. The 0.3% is 'you lose differently.' Either way: you lose. The prescience is Ixian.",
		"My Cymek just entered your base. It has a human brain. The brain is calculating. The calculation is 'destroy everything.' The brain is very fast. The Cymek is very deadly. The deadly is Ixian.",
		"Flesh Vat just produced 20 soldiers. In 30 seconds. Your barracks produced... 1. In 30 seconds. The differential is 19. The differential is Ixian. The differential is 'you lose.'",
		"Resonance Weapon firing! It emits a frequency that disrupts electronics. Your base is vibrating. Your power grid is vibrating. Your CONSTRUCTION YARD is vibrating. The vibration is 'bad for you.' The 'bad' is very Ixian. And very permanent.",
		"Researcher Heli X here. I have four PhDs. In Biomechanics, Quantum Engineering, Prescience Theory, and Applied Ethics. The 'Applied Ethics' is theoretical. The 'Applied' is not. The 'not' is Ixian. The Ixian is ' %player% is destroyed.' Very proprietary. Very final.",
	},
	doubleTrouble={
		"Heli X here. My Cymek Factory produces machines with human brains. %otherGen% produces... machines with human drivers? How quaint. Both are aimed at %player%. The brain-in-a-machine is faster. The driver-in-a-machine is... slower. Both are deadly. The faster is Ixian. The Ixian is ' %player% is destroyed.'",
		"My Flesh Vat produced 20 soldiers in 30 seconds. %otherGen% is also producing soldiers. With... training, I assume. How slow. Both are aimed at %player%. The manufacturing is faster than training. The faster is ' %player% is overwhelmed.' The 'overwhelmed' is Ixian.",
		"Ixian science doctrine: we DESIGN technology. With %otherGen%? We design, %otherGen% uses... existing technology? How nostalgic. Both are aimed at %player%. The designed is deadlier. The deadlier is ' %player% is gone.' The 'gone' is proprietary. And permanent.",
	},
	tripleTrouble={
		"Three armies! My Cymeks march. My Flesh Vats produce. %otherGen% attacks. The third faction attacks. All four -- I mean three -- are aimed at %player%. The science is 'brain-in-machine plus vat-grown soldiers plus two armies.' The equation is proprietary. The result is ' %player% is gone.' The 'gone' is from a PhD. Four PhDs. Very Heli X. Very Ixian. Very permanent.",
		"Cymeks, Flesh Vats, Prescience, and Resonance Weapons. All my designs. All aimed at %player%. %otherGen% adds their forces. The third faction adds theirs. I add SCIENCE. The science is 'brains in machines, soldiers from vats, predicting the future, and vibrating your base to death.' The 'predicting' says ' %player% loses.' The 'vibrating' says 'yes, doctor.' %player% says nothing. %player% is GONE. Very proprietary. Very Ixian. Very FINAL.",
	},
},
})

G("Human Expedition", {
	{name="Knight-Commander Anduin Lothar", doctrine="tank", taunts={
		"Knights, charge! They're on horseback. With lances. Your tank has a cannon. The knight has a LANCE. The knight is not impressed.",
		"Paladins, advance! Holy warriors with blessed hammers. Your tanks are machines. My Paladins have FAITH. Faith beats machines. Historically.",
		"Ballistae, load! Giant crossbows. Yes, crossbows. Against tanks. The bolts are the size of telephone poles. Your tank's armor is... a suggestion.",
		"Human Expedition doctrine: we fight with honor, steel, and VERY large siege weapons. Your technology is cute. Our ballista bolts are NOT cute.",
		"My Knight just rode through your infantry. The horse didn't stop. The lance didn't break. Your infantry didn't move. Because they're dead.",
		"Paladin just smashed your tank with his warhammer. The tank dented. The Paladin prayed. The tank exploded. The prayer was very effective.",
		"Ballista just fired a bolt through your tank. Through it. In one side. Out the other. The bolt is still going. It hit the building behind your tank.",
		"Siege Engine rolling toward your base. It's a rolling fortress. With a cannon. And armor. And it's pulled by horses. Medieval engineering at its finest.",
	},
	doubleTrouble={
		"Lothar here. My Knights charge %player% with lances and horses. %otherGen% charges too. With... tanks, I assume. The lance meets the tank. The lance is enchanted. The tank is not. The lance wins. %otherGen% wins too. %player% loses. The losing is medieval. And modern.",
		"Human Expedition doctrine: honor, steel, and VERY large siege weapons. With %otherGen%? Honor, steel, siege weapons, AND whatever %otherGen% brings. All aimed at %player%. The 'whatever' is probably technology. Technology is cute. Ballista bolts are NOT cute.",
		"My Paladin just smashed %player%'s tank with a blessed warhammer. The tank exploded. %otherGen% is exploding the rest. The rest is 'everything.' Everything is exploding. The exploding is holy. The holy is ' %player% is gone.'",
	},
	tripleTrouble={
		"Three armies! My Knights charge. %otherGen% charges. The third faction charges. All three charge toward %player%. The horses are fast. The tanks are faster. The 'whatever' is... whatever. All three are deadly. The charge is ' %player% is destroyed.' The 'destroyed' is medieval. And modern. And whatever the third faction is.",
		"My Ballista just fired a bolt through %player%'s tank. Through it. The bolt is still going. %otherGen% is also firing. The third faction is also firing. Everything is firing. The bolt hit the building behind the tank. %otherGen% hit the building behind THAT. The third faction hit... something. Everything is hit. Everything is 'destroyed.'",
	},
},
	{name="Archmage Khadgar", doctrine="infantry", taunts={
		"Footmen, form ranks! They have swords and shields. Your troops have guns. The shields are enchanted. The enchantment says 'bullets don't work.'",
		"Elven Archers, nock arrows! They can hit a fly at 300 yards. Your tank is bigger than a fly. The arrow is also bigger than a fly. The math works out.",
		"Human Expedition infantry doctrine: magic, steel, and elven precision. Your technology is impressive. Our MAGIC is more impressive.",
		"Mage casting Blizzard on your base. Ice. From the sky. Lots of ice. Your units are freezing. And then they're shattering. The shattering is the bad part.",
		"My Footman just blocked your tank shell with his shield. The shield is blessed by the Light. The Light is very protective. The tank shell is confused.",
		"High Elf Priest healing my troops. Your troops are dying. My troops are HEALING. The differential is... significant. And growing.",
		"Elven Archer just put an arrow through your tank's viewport. Through the VIEWPORT. From 300 yards. While your tank was moving. Elves don't miss.",
		"Polymorph! Your tank is now a sheep. A sheep. I'm not joking. Your expensive, powerful tank is a SHEEP. The sheep is confused. The sheep is also wool. Mine now.",
	},
	doubleTrouble={
		"Khadgar here. My Footmen advance on %player% with enchanted shields. %otherGen% advances too. With... non-enchanted shields, I assume. How quaint. Both are aimed at %player%. The enchantment says 'bullets don't work.' The non-enchantment says 'bullets might work.' The enchantment is better.",
		"My Mage is casting Blizzard on %player%'s base. Ice from the sky. %otherGen% is also casting... something, I assume. Both are aimed at %player%. The ice is magical. The 'something' is probably not magical. The magical is colder. The colder is ' %player% shatters.'",
		"Human Expedition infantry doctrine: magic, steel, and elven precision. With %otherGen%? Magic, steel, elves, AND whatever %otherGen% brings. All aimed at %player%. The 'whatever' is probably technology. Technology vs magic. Magic wins. Magic usually wins. Against everything.",
	},
	tripleTrouble={
		"Three armies! My Footmen advance. %otherGen% advances. The third faction advances. All three advance toward %player%. The shields are enchanted. The others are... not enchanted, I assume. All three are deadly. The enchanted is deadlier. The deadlier is ' %player% is destroyed.' The 'destroyed' is magical. And permanent.",
		"Polymorph! %player%'s tank is now a sheep. %otherGen% is shooting the sheep. The third faction is... also shooting the sheep, presumably. The sheep is confused. The sheep is dead. %player%'s tank is a dead sheep. The 'dead sheep' is ' %player% has no tanks.' The 'no tanks' is magical. The magical is Khadgar. The Khadgar is PERMANENT.",
	},
},
	{name="Gryphon Rider Kurdran Wildhammer", doctrine="aircraft", taunts={
		"Gryphon Riders, take flight! Half eagle, half lion, ALL hammer. The hammer is storm-powered. Your anti-air is NOT storm-powered. Advantage: Gryphon.",
		"Flying Machines scouting your base. They're small. They're fast. They're piloted by gnomes. The gnomes are VERY enthusiastic about bombing you.",
		"Human Expedition air doctrine: gryphons, flying machines, and mages on flying carpets. Your jets are fast. Our gryphons are ANGRY. Angry beats fast.",
		"Gryphon Rider just dropped his Stormhammer on your tank. The hammer is magical. The tank is not magical. The tank lost. The hammer is fine.",
		"Flying Machine just dropped a bomb on your refinery. The bomb is small. The refinery is large. The bomb is VERY effective for its size. Gnomish engineering.",
		"My Gryphon just did a dive-bomb on your anti-air turret. The turret fired. The gryphon dodged. The turret didn't dodge the hammer. The turret is now rubble.",
		"Gryphon Riders circling your base. Like eagles. Because they ARE eagles. Half-eagles. With hammers. And dwarven riders. The dwarves are singing. It's a battle song. Your death is the chorus.",
		"You launched a fighter. My Gryphon Rider threw his hammer at it. The hammer is magical. The fighter is mechanical. Magic won. Magic usually wins. Against everything.",
	},
	doubleTrouble={
		"Kurdran here. My Gryphon Riders take flight against %player%. Half eagle, half lion, ALL hammer. %otherGen% takes flight too. With... mechanical aircraft, I assume. How quaint. Both are aimed at %player%. The hammer is storm-powered. The mechanical is... fuel-powered. The storm is infinite. The fuel is not.",
		"Human Expedition air doctrine: gryphons, flying machines, and mages on flying carpets. With %otherGen%? Gryphons, machines, carpets, AND whatever %otherGen% flies. All aimed at %player%. The 'whatever' is probably not a gryphon. How sad. The gryphon is angry. The angry beats fast. The fast is %otherGen%. The angry is ME.",
		"My Gryphon just dive-bombed %player%'s anti-air. The turret fired. The gryphon dodged. The turret didn't dodge the hammer. %otherGen% is also dive-bombing, I assume. Both are aimed at %player%. The hammer is magical. The 'whatever' is mechanical. The magical wins. The magical ALWAYS wins.",
	},
	tripleTrouble={
		"Three armies! My Gryphons fly. %otherGen% flies. The third faction flies. All three fly toward %player%. The gryphons are angry. The others are... mechanical, I assume. All three are deadly. The angry is deadlier. The deadlier is ' %player% is destroyed.' The 'destroyed' is from a storm hammer. The storm hammer is magical. The magical is PERMANENT.",
		"My Gryphon Rider threw his hammer at %player%'s fighter. The hammer is magical. The fighter is mechanical. Magic won. %otherGen% is also winning. The third faction is also winning. Everyone is winning EXCEPT %player%. The 'except %player%' is ' %player% loses.' The 'loses' is from a gryphon. The gryphon is from a dwarf. The dwarf is singing. The song is ' %player% is dead.'",
	},
},
	{name="Sorceress Jaina Proudmoore", doctrine="science", taunts={
		"Blizzard, casting! Ice from the sky. Lots of ice. Your units are freezing. Then they're shattering. The shattering is the bad part. The bad part is NOW.",
		"Polymorph! Your tank is now a sheep. A SHEEP. I'm not joking. Your expensive, powerful tank is a sheep. The sheep is confused. The sheep is also wool. Mine now.",
		"Slow, casting! Your units are... moving... very... slowly. They're trying to run. They can't. The spell is 'Slow.' The result is 'you can't run.' The 'can't run' is very permanent. Briefly.",
		"Human Expedition science doctrine: we don't have technology. We have MAGIC. Magic is older than technology. Magic is BETTER than technology. Your technology is from this century. My magic is from EVERY century.",
		"Invisibility, casting! My entire army just vanished. You can't see them. They can see you. They're behind you. The 'behind you' is invisible. The 'behind you' is also deadly. The deadly is magical.",
		"Arcane Brilliance, active! My casters now have unlimited mana. Unlimited Blizzard. Unlimited Polymorph. Unlimited Slow. The 'unlimited' is very magical. And very bad. For you.",
		"My Sorceress just turned your hero into a sheep. The sheep is grazing. Peacefully. The hero is gone. The sheep is mine. The magic is Jaina. The Jaina is PERMANENT.",
		"Jaina here. I studied magic in Dalaran. The magic is 'Arcane.' The Arcane is 'bend reality.' The reality I'm bending is yours. Into a sheep. Or ice. Or nothing. The 'nothing' is my favorite.",
	},
	doubleTrouble={
		"Jaina here. My Blizzard rains ice on %player%. My Polymorph turns %player%'s tanks into sheep. %otherGen% provides the ground assault. The ice is from magic. The sheep is from magic. The assault is from... non-magic. Both are aimed at %player%. The magic is deadlier. The magic is ' %player% is a sheep. And frozen. And destroyed.'",
		"My Invisibility hides my entire army. %otherGen% attacks from the front. The front is visible. My army is not. %player% watches the front. My army is behind %player%. The 'behind' is invisible. The 'invisible' is ' %player% is destroyed.' Very magical. Very Jaina.",
		"Human Expedition science doctrine: we have MAGIC. With %otherGen%? Magic AND whatever %otherGen% brings. All aimed at %player%. The 'whatever' is probably technology. Technology vs magic. Magic wins. Magic ALWAYS wins. Against everything. Especially %player%.",
	},
	tripleTrouble={
		"Three armies! My Blizzard rains ice. My Polymorph turns tanks into sheep. %otherGen% attacks. The third faction attacks. All four -- I mean three -- are aimed at %player%. The science is 'arcane magic plus two armies.' The equation is magical. The result is ' %player% is a frozen sheep.' The 'frozen sheep' is ' %player% is gone.' Very Jaina. Very magical. Very permanent.",
		"Blizzard, Polymorph, Slow, Invisibility, and Arcane Brilliance. All my spells. All aimed at %player%. %otherGen% adds their forces. The third faction adds theirs. I add MAGIC. The magic is 'ice, sheep, slowness, invisibility, and unlimited power.' The 'unlimited power' is from Dalaran. The 'from Dalaran' is ' %player% is GONE.' The 'gone' is a sheep. A frozen sheep. That used to be a base. Very Jaina. Very FINAL.",
	},
},
})

G("Orcish Horde", {
	{name="Warchief Grom Hellscream", doctrine="tank", taunts={
		"GRUNTS, CHARGE! They have axes. BIG axes. Your tank has armor. The axe is bigger than the armor. The axe WINS.",
		"Ogre, SMASH! He's twelve feet tall. He has a club. The club is a tree. A whole tree. Your tank is about to meet the tree. The tree wins.",
		"Orcish Horde doctrine: bigger is better. More bigger is more better. Your tanks are big. My Ogres are BIGGER. The comparison is educational. For you. Briefly.",
		"My Grunt just chopped through your tank's armor with his axe. The axe is enchanted. The armor is not. The axe went through. The tank did not survive the experience.",
		"Ogre Mage just Bloodlust-ed my entire force. They're 50% stronger. 50% faster. 50% angrier. Your troops are 100% dead. The math is simple.",
		"Catapult firing! It throws a BOULDER. At your base. A literal boulder. Your walls are rated for bullets. Not boulders. The boulder doesn't care about ratings.",
		"My Ogre just picked up your infantryman. He's holding him. He's looking at him. He's... throwing him. Into your other infantry. Bowling for grunts. Strike.",
		"Siege Engine rolling toward your walls. It's armored. It's heavy. It's pulled by wolves. WOLVES. Your engineers are questioning their life choices. So are your walls.",
	},
	doubleTrouble={
		"GROM HELLSCREAM here! My Grunts charge %player% with BIG axes. %otherGen% charges too. With... smaller weapons, I assume. HA! Both are aimed at %player%. The axe is bigger than %player%'s armor. The axe WINS. %otherGen% also wins. %player% loses. The losing is ORCISH.",
		"Orcish Horde doctrine: bigger is better. With %otherGen%? Bigger AND more. All aimed at %player%. My Ogres are BIGGER than %player%'s tanks. %otherGen% is... also big, I assume. But not Ogre-big. Nothing is Ogre-big. The Ogre-big is ' %player% is smashed.'",
		"My Ogre just picked up %player%'s infantryman and threw him at %player%'s other infantry. Bowling for grunts. STRIKE. %otherGen% is also bowling, I assume. With... non-Ogre bowling. How quaint. The Ogre bowling is better. The Ogre bowling is ORCISH.",
	},
	tripleTrouble={
		"Three armies! My Grunts charge. %otherGen% charges. The third faction charges. All three charge toward %player%. The axes are BIG. The others are... smaller, I assume. All three are deadly. The biggest is deadlier. The biggest is ORC. The ORC is ' %player% is destroyed.' The 'destroyed' is by axe. Big axe. BIGGEST axe.",
		"Ogre Mage just Bloodlust-ed my entire force. 50% stronger. 50% faster. 50% angrier. %otherGen% is also 50% stronger, I assume. The third faction is also stronger. Everything is stronger. %player% is NOT stronger. %player% is 100% dead. The math is simple. The math is ORCISH.",
	},
},
	{name="Necrolyte Teron Gorefiend", doctrine="infantry", taunts={
		"Troll Axethrowers, unleash! They throw axes. Big axes. Sharp axes. Your infantry has guns. The axes are faster than you think. Trolls are VERY fast.",
		"Goblin Sappers deploying! They carry bombs. On their backs. They're running at your base. They're giggling. Goblins giggle when they explode. It's a goblin thing.",
		"Orcish infantry doctrine: we don't die. We come BACK. Death Knight says hello. Death Knight says 'your dead are now MY dead.' The dead are orcs now. Armed orcs.",
		"Raise Dead! Your fallen soldiers are standing up. They're holding axes now. They're MY soldiers now. Thank you for the donation. The dead are very grateful. And very angry.",
		"My Troll Axethrower just hit your tank with an axe. From 50 yards. By THROWING it. The axe went through the armor. The troll is already throwing another. He has many axes.",
		"Goblin Sappers just reached your War Factory. They're hugging it. The hug is explosive. The War Factory is gone. The goblins are gone. Everyone is gone. Except me. I'm fine.",
		"Death Knight casting Death and Decay on your base. Your buildings are rotting. Your units are rotting. Everything is rotting. The smell is terrible. The damage is worse.",
		"You killed my Grunt. My Necrolyte raised him. He's back. He's angry. He has an axe. He's charging your tank. The axe is enchanted. The tank is not. The axe wins. Again.",
	},
	doubleTrouble={
		"Gorefiend here. My Troll Axethrowers unleash on %player%. Big axes. Sharp axes. Thrown axes. %otherGen% unleashes too. With... bullets, I assume. The axe is faster than %player% thinks. The bullet is... also fast. Both are aimed at %player%. Both are deadly. The axe is deadlier. The axe is ORCISH.",
		"Raise Dead! %player%'s fallen soldiers are standing up. They're holding axes now. They're MY soldiers now. %otherGen% is also killing %player%'s soldiers. The dead soldiers are MINE. The killing is shared. The dead are ORC. The orc is ' %player% has no army.' The 'no army' is because the army is MINE now.",
		"My Goblin Sappers just reached %player%'s War Factory. They're hugging it. The hug is explosive. %otherGen% is also exploding things, I assume. Both are aimed at %player%. The goblins giggle when they explode. The giggle is ORCISH. The explosion is ' %player% has no War Factory.'",
	},
	tripleTrouble={
		"Three armies! My Trolls throw axes. %otherGen% attacks. The third faction attacks. All three are aimed at %player%. The dead rise. The dead are MINE. %otherGen% kills the living. I raise the dead. The third faction does... whatever. Everything is aimed at %player%. The 'everything' is ' %player% is destroyed.' The 'destroyed' is ORCISH. The ORCISH is permanent. Even in death. ESPECIALLY in death.",
		"Death Knight casting Death and Decay on %player%'s base. Everything is rotting. %otherGen% is also destroying. The third faction is also destroying. Everything is being destroyed AND rotting. The rotting is ORCISH. The destroying is everyone. The 'everyone' is ' %player% is gone.' The 'gone' is rotting. The rotting is PERMANENT.",
	},
},
	{name="Dragonmaw Overlord Nekros Skullcrusher", doctrine="aircraft", taunts={
		"DRAGONS! I have DRAGONS! Your anti-air is designed for planes. These are NOT planes. These are DRAGONS. They breathe FIRE. Your anti-air is now a BBQ.",
		"Dragon inbound! It's red. It's huge. It's angry. It breathes fire. Your base is flammable. This is a design flaw. YOUR design flaw.",
		"Orcish air doctrine: we don't have planes. We have DRAGONS. Dragons don't need runways. Dragons don't need fuel. Dragons need TARGETS. You're a target.",
		"My Dragon just set your entire tank column on fire. The column is burning. The tanks are melting. The crews are running. The Dragon is circling. For round two.",
		"Death Knight on a... wait, no. But my Dragon just ate your fighter. ATE it. Chewed it. Spit out the parts. The parts are on fire. Everything is on fire. The Dragon is happy.",
		"Goblin Zeppelin overhead! It's dropping bombs. The bombs are goblin-made. Goblin-made means 'might explode early.' They exploded on YOUR base. Right on time. For goblins.",
		"Dragon just did a strafing run on your Construction Yard. The Yard is on fire. The fire is dragon-fire. Dragon-fire doesn't go out. Your Yard is going to burn for a while. Then it's gone.",
		"You launched twelve fighters at my Dragon. The Dragon ate three. Burned five. Scared the rest into retreating. The Dragon is still flying. The Dragon is ALWAYS still flying. Dragons don't land. Dragons don't NEED to land. Dragons just need TARGETS. You're still a target.",
	},
	doubleTrouble={
		"Nekros here. My DRAGONS are inbound on %player%. They breathe FIRE. %otherGen% is also inbound. With... non-dragons, I assume. How quaint. Both are aimed at %player%. The fire is dragon-fire. Dragon-fire doesn't go out. The non-dragon-fire... probably goes out. The dragon-fire is better.",
		"Orcish air doctrine: we don't have planes. We have DRAGONS. With %otherGen%? Dragons AND whatever %otherGen% flies. All aimed at %player%. The 'whatever' is probably not a dragon. How sad. The dragon eats the 'whatever.' The dragon eats %player% too. The dragon eats EVERYTHING.",
		"My Dragon just set %player%'s tank column on fire. The column is burning. The tanks are melting. %otherGen% is also burning %player%, I assume. Both are burning. The dragon-fire doesn't go out. The 'doesn't go out' is ' %player% is gone.' The 'gone' is on fire. The fire is DRAGON.",
	},
	tripleTrouble={
		"Three armies! My Dragons fly. %otherGen% flies. The third faction flies. All three fly toward %player%. The dragons breathe fire. The others... probably don't breathe fire. How sad. All three are deadly. The fire is deadlier. The fire is ' %player% is destroyed.' The 'destroyed' is by DRAGON. The DRAGON is permanent. The DRAGON doesn't land.",
		"You launched twelve fighters at my Dragon. The Dragon ate three. Burned five. Scared the rest. %otherGen% is also scaring fighters, I assume. The third faction is also scaring. Everything is scary. Everything is on fire. Everything is DRAGON. The 'everything' is aimed at %player%. The 'aimed' is 'destroyed.' The 'destroyed' is by dragon-fire. The dragon-fire doesn't go out. EVER.",
	},
},
	{name="Warlock Cho'gall", doctrine="science", taunts={
		"Bloodlust, casting! My entire army is now 50% stronger. 50% faster. 50% angrier. The anger was already high. The Bloodlust makes it HIGHER. The 'higher' is very orcish. And very bloody.",
		"Death and Decay, casting! Your buildings are rotting. Your units are rotting. Everything is rotting. The rotting is magical. The magical is orcish. The orcish is PERMANENT.",
		"Rune of Blood, inscribing! The rune explodes when your units walk over it. The explosion is magical. The magic is orcish. The orcish is 'your units are gone.' The 'gone' is from a RUNE. Very sneaky. For an orc.",
		"Orcish science doctrine: we don't have science. We have SHAMANISM. And warlock magic. And ogre magi. The magic is older than your science. The magic is ANGRIER than your science. The magic wins.",
		"My Ogre Mage just Bloodlust-ed my Grunts. They're 50% stronger. They're already stronger than your infantry. Now they're 50% MORE stronger. The math is 'your infantry is paste.' The paste is orcish.",
		"Death Knight casting Death Coil on your hero. The coil drains life. The life is MINE now. The hero is weaker. The Death Knight is stronger. The differential is 'your hero is dead.' The 'dead' is very orcish.",
		"Demonic Portal, opening! I summoned a demon. From the Twisting Nether. The demon is big. The demon is angry. The demon is NOT orcish. The demon is 'your base is destroyed.' The 'destroyed' is from the Nether. The Nether is very permanent.",
		"Cho'gall here. I have two heads. Both are smarter than you. One is a warlock. The other is also a warlock. The warlocking is 'destroy %player%.' Both heads agree. The agreement is rare. The destruction is not.",
	},
	doubleTrouble={
		"Cho'gall here. My Bloodlust makes my army 50% stronger. %otherGen% provides... non-bloodlusted forces, I assume. How calm. Both are aimed at %player%. The Bloodlusted is deadlier. The deadlier is ' %player% is destroyed.' The 'destroyed' is very bloody. And very orcish.",
		"My Death and Decay rots %player%'s base. %otherGen% is also destroying. Both are aimed at %player%. The rotting is magical. The destroying is... conventional. Both are deadly. The magical is deadlier. The magical is ORCISH.",
		"Orcish science doctrine: we have SHAMANISM. With %otherGen%? Shamanism AND whatever %otherGen% brings. All aimed at %player%. The 'whatever' is probably science. Science vs shamanism. Shamanism is angrier. The angrier wins. The angrier is ' %player% is gone.' The 'gone' is bloody. And permanent.",
	},
	tripleTrouble={
		"Three armies! My Bloodlust empowers. My Death and Decay rots. %otherGen% attacks. The third faction attacks. All four -- I mean three -- are aimed at %player%. The science is 'orcish magic plus two armies.' The equation is two-headed. Both heads agree: ' %player% is gone.' The 'gone' is from a warlock. With two heads. Very Cho'gall. Very orcish. Very permanent.",
		"Bloodlust, Death and Decay, Runes of Blood, and Demonic Portal. All my spells. All aimed at %player%. %otherGen% adds their forces. The third faction adds theirs. I add SHAMANISM. The shamanism is 'stronger, rotting, exploding, and demonic.' The 'demonic' is from the Nether. The 'stronger' is from Bloodlust. The 'rotting' is from Death and Decay. The 'exploding' is from Runes. All four are from ME. Two heads. Both warlocks. Both aimed at %player%. The 'aimed' is ' %player% is GONE.' Very Cho'gall. Very bloody. Very FINAL.",
	},
},
})

G("TKM Battlegroup", {
	{name="Colonel Petrov", doctrine="tank", taunts={
		"Abrams tanks, roll out! Modern main battle tanks with composite armor and depleted uranium rounds. Your armor is... not composite. The round is uranium. The math is bad. For you.",
		"T-72M column advancing. Soviet-era tanks. They're old. They're angry. They work. Your fancy tanks are about to meet WORKING tanks. Working wins.",
		"TKM tank doctrine: we have everything from T-72s to Abrams. Old and new. East and West. All of it shoots. All of it kills. Your base is the target. All of it is aimed.",
		"My Abrams just fired a sabot round at your tank. The round went through the front armor. Through the engine. Out the back. Into the tank behind it. Two kills. One round. American engineering.",
		"T-72M just crossed your perimeter. It's old. It's rusty. It's SHOOTING. The shooting is not rusty. The shooting is very effective. Old doesn't mean useless. Old means EXPERIENCED.",
		"Stryker APC deploying troops inside your perimeter. It's fast. It's armored. It's full of very angry marines. The marines have rifles. And opinions. The opinions are 'shoot everything.'",
		"Technical just drove into your base. It's a pickup truck with a machine gun. It cost $300. Your tank cost $1000. The technical is winning. The math is embarrassing. For you.",
		"Trench Tank advancing. It's slow. It's heavily armored. It has a cannon. And a machine gun. And it's VERY close to your base now. The 'slow' part is over. The 'cannon' part is starting.",
	},
	doubleTrouble={
		"Petrov here. My Abrams roll toward %player% with composite armor and depleted uranium rounds. %otherGen% rolls too. With... less composite armor, I assume. Both are aimed at %player%. The uranium round goes through %player%'s tank. Through the engine. Out the back. Into the tank behind it. Two kills. One round. American engineering. %otherGen% helps. The help is ' %player% is gone.'",
		"TKM tank doctrine: everything from T-72s to Abrams. With %otherGen%? T-72s, Abrams, AND whatever %otherGen% drives. All aimed at %player%. The 'whatever' is probably not as well-armed. How quaint. Both are deadly. The Abrams is deadlier. The deadlier is ' %player% is destroyed.'",
		"My Technical just drove into %player%'s base. It's a pickup truck with a machine gun. It cost $300. %otherGen%'s cheapest unit cost... more, I assume. The Technical is winning. The math is embarrassing. For %player%. The embarrassment is TKM.",
	},
	tripleTrouble={
		"Three armies! My Abrams roll. %otherGen% rolls. The third faction rolls. All three roll toward %player%. The composite armor bounces shells. The uranium rounds penetrate everything. %otherGen% also penetrates, I assume. All three are deadly. The Abrams is deadlier. The deadlier is ' %player% is destroyed.' The 'destroyed' is TKM. The TKM is PERMANENT.",
		"My T-72M just crossed %player%'s perimeter. It's old. It's rusty. It's SHOOTING. %otherGen% is also shooting. The third faction is also shooting. Everything is shooting. The T-72 is old but WORKING. The working is ' %player% is gone.' The 'gone' is by old tank. And new tank. And %otherGen%. And a third faction. The 'gone' is comprehensive.",
	},
},
	{name="Captain Reyes", doctrine="infantry", taunts={
		"Marines, deploy! Modern infantry with assault rifles and body armor. Your minigunners have... miniguns. We have TRAINING. Training beats miniguns. Usually. Today: definitely.",
		"Spetsnaz inbound! Elite special forces. They're in your base. They've been in your base for ten minutes. You didn't notice. They noticed EVERYTHING. They're shooting now.",
		"TKM infantry doctrine: every soldier is a professional. We don't use conscripts. We use VOLUNTEERS. Volunteers who shoot straight. Your conscripts shoot... sometimes. At things. Occasionally.",
		"Sandmarine deploying! Desert warfare specialist. He's camouflaged. In your base. You can't see him. He can see you. He's aiming. He's firing. You're hit. He's gone.",
		"Sniper just took out your officer from 800 meters. The officer didn't hear the shot. Nobody hears the shot. The shot is very quiet. The death is very permanent.",
		"Rocketeer launching! Anti-air infantry with rockets. Your aircraft are approaching. The rockets are also approaching. The rockets are approaching FASTER. The rockets win.",
		"My Spetsnaz just sabotaged your Power Plant. He placed charges. He's gone. The charges are NOT gone. The charges are about to detonate. Your power grid is about to have a 'moment.'",
		"You sent infantry to fight my Marines. My Marines have body armor. Your infantry have... enthusiasm. Enthusiasm doesn't stop bullets. Body armor stops bullets. My Marines are fine. Your infantry are not.",
	},
	doubleTrouble={
		"Reyes here. My Marines deploy against %player% with assault rifles and body armor. %otherGen% deploys too. With... less body armor, I assume. Both are aimed at %player%. The body armor stops bullets. The enthusiasm doesn't. The differential is ' %player% loses.' The differential is TKM.",
		"My Spetsnaz just sabotaged %player%'s Power Plant. He placed charges. He's gone. %otherGen% is at the front. The charges are NOT gone. The front is being breached. The power is going out. Everything happens at once. The 'once' is ' %player% is destroyed.'",
		"TKM infantry doctrine: every soldier is a professional. With %otherGen%? Professionals AND whatever %otherGen% sends. All aimed at %player%. The 'whatever' is probably conscripts. How quaint. The professionals shoot straight. The conscripts shoot... sometimes. The 'sometimes' is not enough. The 'not enough' is ' %player% is gone.'",
	},
	tripleTrouble={
		"Three armies! My Marines deploy. %otherGen% deploys. The third faction deploys. All three are aimed at %player%. The body armor stops bullets. The others... probably don't have body armor, I assume. All three are deadly. The professionals are deadlier. The deadlier is ' %player% is destroyed.' The 'destroyed' is TKM. The TKM is professional. The professional is PERMANENT.",
		"My Sniper just took out %player%'s officer from 800 meters. The officer didn't hear the shot. %otherGen% is also shooting officers, I assume. The third faction is also shooting. Everything is shooting. The officers are dead. The 'officers are dead' is ' %player% has no command.' The 'no command' is ' %player% is gone.' The 'gone' is by sniper. From 800 meters. Very professional. Very TKM.",
	},
},
	{name="Air Marshal Petrov", doctrine="aircraft", taunts={
		"Iroquois gunships inbound! Attack helicopters with rockets and autocannons. Your base is the target. The rockets are the message. The autocannons are the P.S.",
		"Tornado G launchers firing! Ground attack aircraft. They're fast. They're low. Your radar can't see them. Your radar is looking UP. The Tornado is NOT up. The Tornado is VERY low. And VERY fast.",
		"TKM air doctrine: we have attack helicopters, ground attack jets, and... that's it. But each one carries enough ordnance to level a city block. Your base is smaller than a city block. The math works out.",
		"Iroquois just fired a rocket salvo at your tank column. The salvo is 38 rockets. Your column has 5 tanks. The math is 'each tank gets 7 rockets and one extra for the lead tank.' The extra is a gift.",
		"Tornado just did a low-level pass over your base. It dropped cluster munitions. The munitions are spreading. The spreading is 'comprehensive.' Your base is now 'comprehensive' too. Comprehensively on fire.",
		"My Iroquois is hovering over your base. It's shooting. It's not leaving. Helicopters don't need to leave. Helicopters hover. And shoot. And hover. And shoot. The cycle is 'shoot.' The result is 'you lose.'",
		"Flak Bus deploying! It's a bus. With a flak cannon. On top. It's shooting your aircraft. With a BUS. Your fighter is being shot down by public transport. The humiliation is intentional. The flak is effective.",
		"You launched a fighter. My Tornado outmaneuvered it. The Tornado is faster at low altitude. Your fighter is designed for high altitude. Down here, the Tornado is king. The king just bombed your runway. Your fighter is now a very expensive lawn ornament.",
	},
	doubleTrouble={
		"Petrov here. My Iroquois gunships inbound on %player%. Attack helicopters with rockets and autocannons. %otherGen% is also inbound. With... non-helicopters, I assume. Both are aimed at %player%. The Iroquois hovers. The non-helicopters... probably don't hover. The hover is better. The hover is ' %player% is destroyed.'",
		"TKM air doctrine: attack helicopters, ground attack jets, and enough ordnance to level a city block. With %otherGen%? Helicopters, jets, ordnance, AND whatever %otherGen% flies. All aimed at %player%. %player%'s base is smaller than a city block. The math works out. The math is ' %player% is gone.'",
		"My Tornado just did a low-level pass over %player%'s base. Cluster munitions. The munitions are spreading. %otherGen% is also spreading, I assume. Both are spreading. The spreading is 'comprehensive.' %player%'s base is 'comprehensively on fire.' The fire is TKM. The TKM is from LOW ALTITUDE. The low altitude is ' %player% didn't see it coming.'",
	},
	tripleTrouble={
		"Three armies! My Iroquois hover. %otherGen% flies. The third faction flies. All three fly toward %player%. The helicopters hover and shoot. The others... fly and shoot, I assume. All three are deadly. The hover is deadlier. The hover is ' %player% is destroyed.' The 'destroyed' is TKM. The TKM is from the sky. And from low altitude. And from %otherGen%. And from a third faction. The 'from' is 'everywhere.' The 'everywhere' is ' %player% is gone.'",
		"My Flak Bus is shooting %player%'s aircraft. With a BUS. %otherGen% is also shooting aircraft, I assume. The third faction is also shooting. Everything is shooting. The Flak Bus is public transport. The public transport is shooting down %player%'s fighters. The humiliation is intentional. The flak is effective. The 'effective' is ' %player% has no air force.' The 'no air force' is ' %player% is gone.' The 'gone' is by BUS. Very TKM. Very humiliating. Very permanent.",
	},
},
	{name="Dr. Sarah Kowalski", doctrine="science", taunts={
		"EMP Missile, launching! It fries your electronics. Your radar is dead. Your power grid is dead. Your base is... still there. But dark. Very dark. The dark is TKM. The dark is SCIENCE.",
		"Drone Swarm, deploying! Twenty autonomous UAVs. They're small. They're fast. They're EXPENDABLE. Each one has a warhead. Twenty warheads. Your anti-air can track... four? The other sixteen say 'hello.' The 'hello' is explosive.",
		"TKM science doctrine: we take existing military tech and make it BETTER. With R&D. And money. American money. The money is infinite. The R&D is relentless. The 'better' is 'your base is destroyed.'",
		"Railgun Prototype, firing! The round travels at Mach 7. Your armor is rated for Mach 0. The math is '7 > 0.' The math is 'your tank is gone.' The 'gone' is from a railgun. Very TKM. Very funded.",
		"EMP just detonated over your base. Your power plants are offline. Your defenses are offline. Your CONSTRUCTION YARD is offline. The 'offline' is very dark. And very quiet. The quiet is about to end. My tanks are coming.",
		"Drone Swarm just reached your base. Twenty UAVs. Each one picked a different target. Your anti-air shot down three. Seventeen hit. The hits are 'seventeen explosions.' The explosions are very TKM. And very cheap. The cheap is the POINT.",
		"Railgun Prototype just penetrated your bunker. The round went through 2 meters of reinforced concrete. Like it wasn't there. The concrete was expensive. The round was cheaper. The economics are 'you lose.' The economics are TKM.",
		"Dr. Kowalski here. I have a PhD in Electrical Engineering and a Master's in Applied Destruction. The 'Applied Destruction' is from DARPA. The DARPA is very well-funded. The funding is aimed at you. The 'aimed' is very precise. And very final.",
	},
	doubleTrouble={
		"Kowalski here. My EMP Missile fries %player%'s electronics. %otherGen% attacks from the ground. The EMP is from science. The ground assault is from... conventional weapons. Both are aimed at %player%. The EMP is first. The assault is second. The 'first then second' is ' %player% is dark then destroyed.' Very TKM. Very funded. Very permanent.",
		"My Drone Swarm deploys against %player%. Twenty UAVs. %otherGen% deploys too. With... manned aircraft, I assume. How expensive. Both are aimed at %player%. The drones are expendable. The manned aircraft are not. The expendable is deadlier. The expendable is TKM.",
		"TKM science doctrine: we make existing tech BETTER. With %otherGen%? Better tech AND whatever %otherGen% brings. All aimed at %player%. The 'whatever' is probably not R&D-funded. How underfunded. The R&D is ' %player% is gone.' The 'gone' is from a PhD. Very funded. Very final.",
	},
	tripleTrouble={
		"Three armies! My EMP fries electronics. My Drone Swarm deploys. %otherGen% attacks. The third faction attacks. All four -- I mean three -- are aimed at %player%. The science is 'EMP plus drones plus two armies.' The equation is very funded. The result is ' %player% is dark, swarmed, and destroyed.' The 'destroyed' is from DARPA. Very Kowalski. Very TKM. Very permanent.",
		"EMP, Drones, Railgun, and R&D. All my designs. All aimed at %player%. %otherGen% adds their forces. The third faction adds theirs. I add SCIENCE. The science is 'fry their electronics, swarm their base, and railgun their bunker.' The 'fry' is EMP. The 'swarm' is drones. The 'railgun' is Mach 7. All three are from ME. From DARPA. From TKM. The 'from TKM' is ' %player% is GONE.' Very Kowalski. Very funded. Very FINAL.",
	},
},
})

-- =====================================================================
-- PLAYER FACTION MOCK SYSTEM
-- Faction-specific mock lines that play during regular attack waves.
-- Keyed by player faction name. Each line mocks the player's faction
-- directly, referencing their units, weaknesses, and stereotypes.
-- Any attacking general can deliver these lines (prefixed with their name).
-- =====================================================================

PlayerFactionMocks = {}

PlayerFactionMocks["GDI Task Force"] = {
	"Oh look, GDI Task Force. You're relying on Mammoth Tanks again. How ORIGINAL. Let me guess -- you built two of them and think you've won? They're slow, they're expensive, and they're about to be scrap.",
	"GDI player, your Ion Cannon takes how long to charge? I'll wait. My army won't. While you stare at the sky hoping for a beam, I'm already in your base. The beam won't save you. The beam never saves you.",
	"GDI Task Force -- the faction that builds ONE expensive tank and prays. Your economy is one refinery away from collapse and your Mammoth Tank just hit a mine. Budget wisely. You won't.",
	"You picked GDI Task Force. The 'safe' option. The 'reliable' option. The BORING option. Your Medium Tanks are adequate. 'Adequate' doesn't stop what's coming. Nothing stops what's coming.",
	"GDI player: you have one strategy. Build tanks. Push forward. Hope. Hope is not a strategy. Hope is what you do when you've already lost. You've already lost.",
}

PlayerFactionMocks["Nod Raiding Party"] = {
	"Nod Raiding Party? Religious fanatics with buggies and stealth. Your 'Brotherhood' is a cult with fragile vehicles. One artillery shell and your entire stealth army is visible. And dead.",
	"Nod player, Kane is not coming. Kane is NEVER coming. You're alone with your flamethrowers and your faith. Fire burns. Faith doesn't stop shells. Shells burn too. HOTTER.",
	"You picked Nod. The 'clever' faction. Stealth tanks, buggies, hit and run. But you're not clever. You're predictable. Your stealth tanks decloak to shoot. I see them. I shoot them. They're not stealthy when they're on fire.",
	"Nod Raiding Party -- the faction that relies on speed and stealth but has the armor of a soda can. Your buggies are fast. My shells are faster. The math is bad. For you.",
	"Nod player, your Obelisk of Light is impressive. For the 3 seconds it's powered. Then your power plant dies and the Obelisk is a very tall paperweight. PEACE THROUGH POWER? Peace through POWER GRID. You don't have one.",
}

PlayerFactionMocks["Allied Vanguard"] = {
	"Allied Vanguard? Medium Tanks and chronoshift. Your tanks are MEDIUM. Not heavy. Not light. MEDIUM. That's like being 'okay' at fighting. 'Okay' loses to 'good.' I'm good.",
	"Allied player, your chronoshift teleports tanks. Into my base. Where my defenses are. The teleport is a delivery service. It delivers your tanks directly to their deaths. Efficient!",
	"You picked Allied Vanguard. The faction that compensates for small tanks with fancy gadgets. GPS. Chronoshift. Gap Generators. Gadgets don't stop shells. Shells stop gadgets. Permanently.",
	"Allied Vanguard -- your Medium Tank costs less than my Heavy Tank. It also does less. And survives less. And dies faster. The economy is not in your favor. The economy is NEVER in your favor.",
	"Allied player, your gap generator hides your base. Cute. I don't need to SEE your base. I just need to shell the area where your base IS. The area is large. The shells are many. Your base is... somewhere in the explosions.",
}

PlayerFactionMocks["Soviet Onslaught"] = {
	"Soviet Onslaught? Heavy Tanks and tesla coils. Brute force, no finesse. Your strategy is 'build big tank, drive forward.' That's not strategy. That's a shopping list. With tanks.",
	"Soviet player, your tesla coils zap things. Very impressive. They also need POWER. Lots of power. One power plant down and your tesla coils are very tall lightning rods. Rods don't shoot. Rods just stand there. Like your strategy.",
	"You picked Soviet Onslaught. The faction that solves every problem with 'more armor.' Your Heavy Tank has armor. My weapon has penetration. Armor vs penetration. Penetration wins. Always. It's physics.",
	"Soviet Onslaught -- your tanks are slow. Your infantry are slow. Your economy is slow. Everything is slow except your defeat. Your defeat is FAST. Your defeat is NOW.",
	"Soviet player, you built a Mammoth Tank. Twin barrels. Very scary. It's also the SLOWEST thing on the battlefield. I can walk around it. I can fly over it. I can ignore it. It'll arrive in six minutes. You'll be dead in four.",
}

PlayerFactionMocks["Allied Peacekeepers"] = {
	"Allied Peacekeepers? Grizzlies and IFVs. Your tanks have GPS. Cute. My shells don't need GPS. They need TRAJECTORY. Trajectory is older than GPS. And more reliable.",
	"Allied player, your IFV changes weapons based on what's inside. It's a Swiss Army knife. Swiss Army knives do everything poorly. My tank does ONE thing. ONE thing well. The ONE thing is killing you.",
	"You picked Allied Peacekeepers. The 'modern' Allies. Your Guardian GIs are nice. Your Mirage Tanks are clever. Your economy is fragile. One refinery raid and your 'modern' army becomes a 'small' army. Small loses.",
	"Allied Peacekeepers -- your air force is good. Your ground force is... present. 'Present' doesn't win battles. 'Present' just means 'things that are there.' My things are there too. My things are BETTER.",
	"Allied player, your Battle Fortress is a bus with a gun. It carries infantry. It's also a very large target. Large targets get hit. The infantry inside get hit. The bus gets hit. Everything gets hit. By me.",
}

PlayerFactionMocks["Red Army"] = {
	"Red Army? Rhino Tanks and V3s. Soviet 'tactics' -- build ten Rhinos, drive forward, hope. Hope is not armor. Hope is not a weapon. Hope is what you do when you've already lost.",
	"Red Army player, your V3 rockets have range. They also have the turning speed of a glacier. My units will be inside your base before the V3 finishes rotating. The V3 is pointing the wrong way. The wrong way is 'at nothing.'",
	"You picked Red Army. The faction that builds Apocalypse Tanks and calls it strategy. The Apocalypse Tank is expensive. It's also alone. One tank. Many enemies. The math is simple. The math is 'tank loses.'",
	"Red Army -- your Terror Drones eat tanks from the inside. Clever. My anti-infantry handles Terror Drones. The Drones are scrap. The scrap doesn't eat anything. The scrap is just scrap.",
	"Soviet player, your Kirov Airship is coming. Slowly. VERY slowly. I have time. Time to build anti-air. Time to make coffee. Time to watch the Kirov crawl across the sky. The Kirov is a parade float. With bombs. That arrives TOMORROW.",
}

PlayerFactionMocks["Psychic Corps"] = {
	"Psychic Corps? Yuri's mind-control army. You steal my units. Cute. I'll build more. You'll steal those too. I'll build MORE. You'll run out of mind control before I run out of units. The math is biological. The math is 'you lose.'",
	"Psychic Corps player, your Initiates are psychics with pistols. They mind-control my units. My units are not happy about this. My ARTILLERY doesn't have a mind. Artillery doesn't get mind-controlled. Artillery just shells your Initiates. From far away. The Initiates are sad. And dead.",
	"You picked Psychic Corps. The 'cheese' faction. Mind control, Brutes, floating disks. Your strategy is 'take the enemy's stuff.' My strategy is 'kill you before you take anything.' My strategy is faster.",
	"Psychic Corps -- your Mastermind can control one unit at a time. ONE. I have MANY. The Mastermind is choosing. While choosing, my other units are shooting. The Mastermind is overwhelmed. The Mastermind is dead.",
	"Yuri player, your Psychic Dominator is charging. It takes how long? I'll kill you before it fires. The Dominator is a countdown. The countdown is YOUR countdown. Not mine. Mine is faster. Mine is NOW.",
}

PlayerFactionMocks["Asian Alliance Strike"] = {
	"Asian Alliance Strike? Samurais and hover tanks. Your samurai cuts through armor. With a sword. Against a tank. The tank has a cannon. The sword has... a handle. The cannon wins. Historically.",
	"Asian Alliance player, your Archers shoot anti-tank arrows. From 300 yards. Impressive. My artillery shoots from 600 yards. The arrows are close. The shells are closer. 'Closer' means 'arrives first.' First wins.",
	"You picked Asian Alliance Strike. The faction that brings swords to a tank fight. Your Lynx hover tanks are fast. Your samurai are brave. Brave and fast doesn't stop shells. Shells don't care about bravery. Or speed.",
	"Asian Alliance -- your Phoenix air superiority is real. Your ground defense is... aspirational. 'Aspirational' means 'I wish I had tanks.' You don't have tanks. You have swords. And hope. Hope is not a tank.",
	"Asian Alliance player, your hackers can disable my buildings. Clever. My tanks can disable your BUILDINGS. With shells. Shells are more permanent than hacking. Hacking is temporary. Shells are STRUCTURAL.",
}

PlayerFactionMocks["Imperial Japan"] = {
	"Imperial Japan? Samurai and mechs. Your samurai charges my tank with a sword. The sword is enchanted. The tank is not impressed. The tank has a 120mm cannon. The sword has... a sharp edge. The cannon wins. Always.",
	"Imperial Japan player, your mechs transform. Very cool. Very anime. They also cost more than my entire army. One mech dies and your economy cries. The economy is the real battlefield. You're losing it.",
	"You picked Imperial Japan. The 'honor' faction. Honor, swords, and tradition. Tradition is beautiful. Tradition doesn't stop a railgun. The railgun is not honorable. The railgun is EFFECTIVE.",
	"Imperial Japan -- your Archer Maidens have anti-tank bows. Bows. Against tanks. The bow is ancient. The tank is modern. Modern wins. Modern ALWAYS wins. The ancient had a good run. The run is OVER.",
	"Japanese player, your King Oni mech is huge. It's also slow. And expensive. And alone. One King Oni vs my entire army. The King Oni is brave. The King Oni is also DEAD. Honor requires survival. You won't survive.",
}

PlayerFactionMocks["Latin Syndicate"] = {
	"Latin Syndicate? Militia and Rocket Buggies. Your militia is angry. Anger doesn't stop tanks. Your Rocket Buggy is fast. Fast doesn't stop shells. You're a cartel with trucks. I'm an army with CANNONS.",
	"Latin Syndicate player, your Tank Killers are subtle. They shoot through armor. My tanks shoot through THEM. Through the armor. Through the crew. Through the 'subtle.' The subtle is dead. The dead is not subtle.",
	"You picked Latin Syndicate. The 'street' faction. Militia, buggies, and attitude. Attitude is not armor. Attitude is not a weapon. Attitude is what you have when you've lost everything else. You've lost everything else.",
	"Latin Syndicate -- your economy is based on drug operations. Very profitable. Very fragile. One raid on your supply lines and your 'empire' becomes a 'budget.' The budget is small. The budget is NOT enough.",
	"Syndicate player, your Carnage Tank is aggressive. It's also ugly. And cheap. And poorly armored. 'Cheap' means 'expendable.' 'Expendable' means 'I don't care if it dies.' It dies. A lot. By me.",
}

PlayerFactionMocks["GDI Walker Column"] = {
	"GDI Walker Column? Titans and Wolverines. Your Titans walk on two legs. Very intimidating. Until one falls over. Then it's a very expensive lawn ornament. That can't get up. Legs are a design flaw. Treads don't fall.",
	"GDI Walker player, your Titan has a 120mm cannon on legs. The cannon is good. The legs are a liability. One artillery shell to the knee joint and your Titan is kneeling. Permanently. In front of my army. How respectful.",
	"You picked GDI Walker Column. The 'mech' faction. Walkers are cool. Walkers are also COMPLICATED. More joints, more failure points. My tanks have treads. Treads are simple. Simple works. Complicated breaks. You break.",
	"GDI Walker Column -- your Juggernaut has three legs and three cannons. It's also SLOW. And can't hit moving targets. My units are moving. Your Juggernaut is missing. The missing is 'comprehensive.' The comprehensive is 'you lose.'",
	"Walker player, your Mammoth Mk II has two railguns. On four legs. The railguns are scary. The four legs are four targets. Four targets, one unit. My army has MANY units. The math is 'legs lose.' The legs always lose.",
}

PlayerFactionMocks["Nod Shadow Legion"] = {
	"Nod Shadow Legion? Stealth tanks and subterranean APCs. Your stealth is impressive. Until you shoot. Then you're visible. And dead. Stealth is a phase. The phase is 'brief.' The brief is 'you explode.'",
	"Nod Shadow player, your Subterranean APC surfaces in my base. Surprise! My base has pavement. Pavement stops subterranean. Your APC is now a very expensive mole. Stuck underground. With no way up. How awkward.",
	"You picked Nod Shadow Legion. The 'sneaky' Nod. Stealth, burrowing, and hit-and-run. But you're not sneaky. You're PREDICTABLE. Every Nod player burrows. Every Nod player gets paved. The paving is a tradition. YOUR tradition.",
	"Nod Shadow Legion -- your Cyborg Commando is strong. He's also ONE unit. One unit vs my army. The Cyborg Commando is tough. He's not INFINITE. My army is infinite. The infinite wins. The 'tough' loses. ALWAYS.",
	"Shadow player, your Banshee fighter is fast. It's also fragile. One missile and your Banshee is a firework. A very expensive firework. The firework is brief. The brief is 'dead.' The dead is 'by me.' Always by me.",
}

PlayerFactionMocks["Naxis War Machine"] = {
	"Naxis War Machine? King Tigers and ME-262s. Your King Tiger has armor. From 1944. My weapon is from NOW. Seventy years of engineering says hello. With a sabot round. Through your Krupp steel. Like butter.",
	"Naxis player, your ME-262 jet is fast. For 1944. My anti-air is from this century. The ME-262 is a museum piece. A museum piece that flies. Briefly. Before becoming a museum piece again. In the GROUND.",
	"You picked Naxis War Machine. The 'historical' faction. Beautiful engineering. Historical engineering. HISTORY is the key word. History is in the PAST. My weapons are in the PRESENT. The present wins. Always.",
	"Naxis War Machine -- your Panzer IV is reliable. It's also 80 years old. My tank was designed last year. The gap is 'technology.' The technology is 'I win.' The 'I win' is 'you lose.' The 'you lose' is 'historical.' Like your tanks.",
	"Naxis player, your V2 rocket is terrifying. For 1944. My missile defense shoots it down. The V2 is a ballistic projectile. Ballistic is predictable. Predictable is 'interceptable.' Intercepted is 'gone.' Gone is 'your rocket.'",
}

PlayerFactionMocks["Schwarzer Mond"] = {
	"Schwarzer Mond? Space Nazis with saucers and lunar technology. Your Haunebu is from the MOON. My shells are from EARTH. Earth is closer. The shells arrive first. The saucer arrives never. Because it's shot down. Over earth.",
	"Schwarzer Mond player, your Lunar Soldaten have laser rifles. Impressive. Lasers need POWER. Your power plants are fragile. One raid and your laser army has flashlights. Very expensive flashlights. That don't shoot.",
	"You picked Schwarzer Mond. The 'space' faction. Advanced technology from the moon. But you're fighting on EARTH. Earth has gravity. Earth has terrain. Your space weapons don't like terrain. Terrain wins. Terrain ALWAYS wins.",
	"Schwarzer Mond -- your Haunebu saucer hovers and bombs. It's also VERY expensive. One saucer costs as much as ten of my tanks. Ten tanks vs one saucer. The saucer bombs three. The other seven shoot it down. The math is 'saucer loses.'",
	"Mond player, your UEbermensch infantry are genetically superior. Supposedly. They still die to shells. Genetics don't stop kinetic energy. Kinetic energy doesn't care about genetics. Kinetic energy cares about TRAJECTORY. The trajectory is 'at you.'",
}

PlayerFactionMocks["Consortium Contract"] = {
	"Consortium Contract? Quantum Tanks and Defender Bots. Your Quantum Tank has a railgun. Expensive. Your Defender Bot is autonomous. Also expensive. Everything you have is expensive. And few. Few vs many. Many wins.",
	"Consortium player, your Manta hover tank is versatile. It hovers. It shoots. It's also ONE tank. My army has MANY tanks. The many vs the one. The one loses. The one ALWAYS loses. Even if the one hovers.",
	"You picked Consortium Contract. The 'mercenary' faction. Everything is premium. Premium means 'overpriced.' Overpriced means 'few units.' Few units means 'you lose.' The economics are simple. The economics are 'you can't afford to win.'",
	"Consortium -- your Sky Crane deploys anywhere. Very flexible. Flexibility doesn't help when you have nothing TO deploy. Your economy is small. Your army is small. Your flexibility is 'flexibly small.' The small loses. ALWAYS.",
	"Consortium player, your Railgun Hover Tank fires a railgun. At Mach 6. Very scary. It also fires ONCE every 5 seconds. My army fires EVERY second. The rate of fire is 'I win.' The 'I win' is 'you lose.' The 'you lose' is 'by volume.'",
}

PlayerFactionMocks["Ordos Sabotage"] = {
	"Ordos Sabotage? Stealth Raiders and Deviator Tanks. Your Deviator turns my units against me. Clever. My units don't LIKE being turned. They shoot the Deviator first. The Deviator is dead. The 'clever' is dead. The dead is 'by me.'",
	"Ordos player, your Stealth Raiders are invisible. Until they shoot. Then they're visible. And dead. Stealth is a phase. The phase is 'brief.' The brief is 'you explode.' Every time.",
	"You picked Ordos Sabotage. The 'saboteur' faction. Sabotage, stealth, and deviation. But sabotage requires getting CLOSE. Close is dangerous. Close is 'my territory.' My territory has defenses. The defenses shoot saboteurs. The saboteurs are shot. ALWAYS.",
	"Ordos Sabotage -- your Sardaukar are elite infantry. They're also FEW. Elite and few vs many and adequate. Many adequate beats few elite. The math is simple: numbers win. The 'numbers win' is 'I win.'",
	"Ordos player, your economy is based on trade. Very profitable. Very vulnerable. One raid on your supply lines and your 'trade empire' becomes a 'trade deficit.' The deficit is 'you can't build units.' The 'can't build' is 'you lose.'",
}

PlayerFactionMocks["Ixian Technocracy"] = {
	"Ixian Technocracy? Koda Tanks with LEGS and railguns. Your Koda Tank walks on four legs. Very advanced. Very COMPLICATED. Complicated breaks. Simple wins. My tanks are simple. My tanks win. Your Koda is broken. On the ground.",
	"Ixian player, your Railgun Drone fires at Mach 6. Impressive. It also has the armor of a paper plane. One shell and your Mach 6 drone is Mach 0. Permanently. The 'permanently' is 'on the ground.' The ground is 'where I put it.'",
	"You picked Ixian Technocracy. The 'science' faction. Everything is experimental. Experimental means 'might not work.' 'Might not work' means 'probably won't work.' 'Probably won't work' means 'you lose.' The science is 'you lose.' Scientifically.",
	"Ixian Technocracy -- your cymek walkers have human brains. The brains are volunteers. 'Mostly.' The brains are also finite. My army doesn't need brains. My army needs SHELLS. The shells are infinite. The brains are not. The 'not' is 'you lose.'",
	"Ixian player, your Storm Raider is shielded. The shield is experimental. 'Experimental' means 'it works and we don't know why.' It also means 'sometimes it doesn't work.' The 'sometimes' is 'now.' The 'now' is 'your shield is down.' The 'down' is 'you die.'",
}

PlayerFactionMocks["Human Expedition"] = {
	"Human Expedition? Knights and Paladins with swords. Against tanks. Your knight has a lance. My tank has a 120mm cannon. The lance is from 1100 AD. The cannon is from NOW. The cannon wins. By EIGHT CENTURIES.",
	"Human Expedition player, your Mage casts Blizzard. Ice from the sky. Cute. My anti-air shoots the ice. The ice is now rain. The rain is not scary. The rain is 'wet.' The 'wet' is not a weapon. The 'not a weapon' is 'you lose.'",
	"You picked Human Expedition. The 'fantasy' faction. Magic, swords, and gryphons. Against TANKS. Magic is cool. Magic is also 'not real.' Tanks are real. Shells are real. The 'real' wins. The 'cool' loses. ALWAYS.",
	"Human Expedition -- your Gryphon Rider throws a storm hammer. The hammer is magical. The hammer returns. The hammer is also ONE hammer. My army has MANY shells. The many vs the one. The one loses. Even if it's magical. ESPECIALLY if it's magical.",
	"Expedition player, your Ballista fires a bolt the size of a telephone pole. Very impressive. My artillery fires a shell the size of a CAR. The car is bigger. The car explodes. The bolt doesn't explode. The 'doesn't explode' is 'less effective.' The 'less effective' is 'you lose.'",
}

PlayerFactionMocks["Orcish Horde"] = {
	"Orcish Horde? Grunts with axes and Ogres with clubs. Your Grunt has an axe. My tank has a cannon. The axe is melee. The cannon is ranged. The ranged shoots the melee BEFORE the melee gets close. The 'before' is 'you die.' The 'you die' is 'at range.'",
	"Orcish player, your Ogre is twelve feet tall. Very scary. He has a club. The club is a tree. My tank has a cannon. The cannon shoots the Ogre. The Ogre is now a very large corpse. The corpse is not scary. The corpse is 'dead.' The 'dead' is 'by cannon.'",
	"You picked Orcish Horde. The 'brute' faction. Bigger is better. More bigger is more better. But BIGGER is also SLOWER. My army is fast. Your Ogres are slow. The fast surrounds the slow. The slow dies. The 'dies' is 'always.'",
	"Orcish Horde -- your Catapult throws a boulder. The boulder is big. The boulder is also SLOW. My artillery shell is fast. The shell arrives first. The 'first' is 'your catapult is destroyed.' The 'destroyed' is 'before it fires.' The 'before' is 'you lose.'",
	"Orc player, your Dragon breathes fire. Very scary. Very DRAGON. The dragon is also ONE unit. One unit vs my ARMY. The dragon is brave. The dragon is also DEAD. The dead is 'by volume of fire.' The 'volume' is 'my entire army shooting it.' The 'shooting' is 'NOW.'",
}

PlayerFactionMocks["TKM Battlegroup"] = {
	"TKM Battlegroup? Abrams and T-72s. You have modern tanks. Good for you. Your Abrams is expensive. Your T-72 is old. You're mixing eras and hoping for the best. The 'best' is not coming. The 'best' is 'you lose.' The 'you lose' is 'by me.'",
	"TKM player, your Technical is a pickup truck with a machine gun. It cost $300. My tank cost $1500. The tank shoots the truck. The truck is gone. The $300 is gone. The 'gone' is 'your economy.' The 'your economy' is 'also gone.'",
	"You picked TKM Battlegroup. The 'mixed bag' faction. Everything from T-72s to Abrams. Old and new. East and West. The mix is 'inconsistent.' Inconsistent means 'some units are good, some are bad.' My units are ALL good. The 'all good' beats 'some good.' ALWAYS.",
	"TKM Battlegroup -- your Flak Bus is a bus with a flak cannon. It's shooting my aircraft. With a BUS. The bus is also a VERY large target. My aircraft shoots the bus. The bus is now a VERY large wreck. The wreck is 'public transport no more.' The 'no more' is 'by me.'",
	"TKM player, your Iroquois gunship hovers and shoots. It's effective. It's also ONE helicopter. My anti-air is MANY. The many vs the one. The one dies. The 'dies' is 'your gunship.' The 'your gunship' is 'expensive.' The 'expensive' is 'gone.'",
}

PlayerFactionMocks["FutureTech Prototypes"] = {
	"FutureTech Prototypes? Future Tanks and Cryocopters. Your Future Tank thinks. Therefore, it destroys. But it also THINKS too much. While it's thinking, my tank is SHOOTING. The shooting is faster than the thinking. The 'faster' is 'you lose.'",
	"FutureTech player, your Cryocopter freezes things. Then they shatter. Very satisfying. For YOU. For ME, it's 'one less aircraft to shoot down.' The Cryocopter is fragile. The fragile is 'one shell.' The 'one shell' is 'your Cryocopter is gone.' The 'gone' is 'before it freezes anything.'",
	"You picked FutureTech Prototypes. The 'prototype' faction. Everything is experimental. Experimental means 'expensive AND unreliable.' Unreliable means 'sometimes it breaks on its own.' The 'on its own' is 'you lose without my help.' The 'without my help' is 'still you lose.'",
	"FutureTech -- your Roboank is autonomous. No driver. No fear. Also no JUDGMENT. The Roboank drives through walls instead of going around. Through MY walls. Where my defenses are. The 'through' is 'into a kill zone.' The 'kill zone' is 'your Roboank is dead.'",
	"FutureTech player, your Guardian Tank has a shield. The shield is experimental. 'Experimental' means 'it works and we don't know why.' It also means 'it stops working and we don't know why.' The 'why' is 'NOW.' The 'now' is 'shield down.' The 'down' is 'you die.'",
}

PlayerFactionMocks["CABAL Uprising"] = {
	"CABAL Uprising? Cyborgs and Manticores. Your cyborgs don't feel fear. They also don't feel 'overwhelmed.' But they ARE overwhelmed. By my army. The 'overwhelmed' is 'they die.' The 'die' is 'even without fear.' Fear is optional. Dying is MANDATORY.",
	"CABAL player, your Manticore has a cannon and a flamethrower. Very versatile. It's also ONE unit. My army is MANY. The many vs the one. The one dies. The 'dies' is 'your Manticore.' The 'your Manticore' is 'scrap.' The 'scrap' is 'recycled.' By me.",
	"You picked CABAL Uprising. The 'AI' faction. Calculated, efficient, and networked. But CALCULATED is PREDICTABLE. I know what you'll calculate. I know what you'll do. The 'know' is 'I counter you.' The 'counter' is 'you lose.' The 'you lose' is 'calculated.' By ME.",
	"CABAL Uprising -- your Overkill Gunship fires continuously. Infinite ammunition. Very impressive. The gunship is also ONE unit. One unit with infinite ammo vs MANY units with adequate ammo. The many shoots the one. The one dies. The 'dies' is 'infinite ammo, finite survival.' The 'finite' is 'VERY finite.'",
	"CABAL player, your Cyborg Commando is your strongest unit. He's also your ONLY strong unit. Everything else is adequate. 'Adequate' plus 'one strong' vs 'everything I have.' The 'everything I have' is 'more.' The 'more' is 'you lose.' The 'you lose' is 'calculated.' To zero decimal places.",
}

PlayerFactionMocks["The Forgotten"] = {
	"The Forgotten? Mutants and Salvage Tanks. Your Mutants heal in Tiberium. Cute. My shells don't care about Tiberium. My shells care about TRAJECTORY. The trajectory is 'at your Mutants.' The 'at' is 'they die.' The 'die' is 'even in Tiberium.' Tiberium doesn't stop shells. Nothing stops shells.",
	"Forgotten player, your Salvage Tank is made from scrap. MY scrap. You're fighting me with MY wreckage. The irony is thick. The irony is also 'not armor.' Scrap metal is not armor. Scrap metal is 'thin.' The 'thin' is 'my shells go through.' The 'go through' is 'you die.' With my scrap. In your face.",
	"You picked The Forgotten. The 'scavenger' faction. You build from junk. You fight with junk. The junk is... junk. My weapons are NOT junk. The 'not junk' beats 'junk.' ALWAYS. The 'always' is 'you lose.' The 'you lose' is 'with junk.'",
	"The Forgotten -- your Ghost Stalker has a railgun. He's invisible. He's also ONE guy. One guy with a railgun vs my ARMY. The army has many guns. The one guy has one. The 'one' is 'not enough.' The 'not enough' is 'he dies.' The 'dies' is 'invisible AND dead.'",
	"Forgotten player, your economy is based on scavenging. Very sustainable. Very SLOW. My economy is based on MINING. Mining is faster. Faster economy means more units. More units means 'you lose.' The 'you lose' is 'by economics.' The economics is 'I have more. You have junk.'",
}

PlayerFactionMocks["The Swarm"] = {
	"The Swarm? Zerglings and Ultralisks. Your Zerglings are infinite. 'Infinite.' But they're also INDIVIDUALLY worthless. One Zergling vs one tank. The tank wins. Always. The 'infinite' is 'you need infinite to win.' You won't HAVE infinite. You'll have 'many.' Many is not infinite. Many is 'not enough.'",
	"Swarm player, your Ultralisk is huge. It has blades. It's also SLOW. My army is fast. The fast kites the slow. The slow never reaches the fast. The 'never reaches' is 'the Ultralisk dies.' The 'dies' is 'at range.' The 'range' is 'MY range.'",
	"You picked The Swarm. The 'biological' faction. Everything is alive. Everything is hungry. Everything is also FLAMMABLE. My incendiary weapons say hello. The 'hello' is 'your biological army is on fire.' The 'fire' is 'biological.' The 'biological' is 'burning.' The 'burning' is 'you lose.'",
	"The Swarm -- your Mutalisks reproduce in flight. Very scary. They're also FRAGILE. One missile per Mutalisk. The Mutalisk is gone. The 'reproducing' doesn't help when they die FASTER than they reproduce. The 'faster' is 'my anti-air.' The 'anti-air' is 'comprehensive.'",
	"Swarm player, your Overmind is smart. It calculates. It plans. But the Overmind is also YOU. And YOU are not as smart as you think. The 'not as smart' is 'you rush.' The 'rush' is 'predictable.' The 'predictable' is 'I counter it.' The 'counter' is 'you lose.' Your infinite swarm meets my finite bullets. The bullets win. Math wins. I win.",
}

PlayerFactionMocks["Protoss Armada"] = {
	"Protoss Armada? Zealots and Dragoons. Your Zealots have plasma shields. Very advanced. The shields also RECHARGE. Very slow. While recharging, my army shoots. The shields go down. The 'down' is 'your Zealot has no shield.' The 'no shield' is 'dead.' The 'dead' is 'by volume.'",
	"Protoss player, your Dragoons are cybernetic warriors. Former Protoss in exoskeletons. Very honorable. Also very EXPENSIVE. One Dragoon costs as much as five of my tanks. Five tanks vs one Dragoon. The five wins. The 'wins' is 'by numbers.' The 'numbers' are 'against you.'",
	"You picked Protoss Armada. The 'advanced' faction. Shields, plasma, and millennia of tradition. Tradition is expensive. Expensive means 'few units.' Few units vs many units. Many wins. The 'many wins' is 'ALWAYS.' The 'ALWAYS' is 'you lose.'",
	"Protoss Armada -- your Carrier launches interceptors. Continuous. Very impressive. The Carrier is also VERY expensive. And SLOW. And ONE unit. My army shoots the interceptors. Then shoots the Carrier. The Carrier is 'interceptors first, then dead.' The 'dead' is 'expensive dead.'",
	"Protoss player, your Reaver fires scarabs. Living bombs that chase you. Very scary. The scarabs are also SLOW. And limited. My army is fast. The scarabs miss. The 'miss' is 'your Reaver is wasting ammo.' The 'wasting' is 'you run out.' The 'run out' is 'you lose.'",
}

PlayerFactionMocks["Terran Dominion"] = {
	"Terran Dominion? Siege Tanks and Battlecruisers. Your Siege Tank in siege mode has range. Very long range. It's also IMMOBILE. My army moves around it. The 'around' is 'behind your Siege Tank.' The 'behind' is 'where it can't shoot.' The 'can't shoot' is 'it dies.'",
	"Terran player, your Battlecruiser has a Yamato Cannon. Very powerful. One shot. Then it recharges for TEN SECONDS. In ten seconds, my army shoots your Battlecruiser. A LOT. The 'a lot' is 'it dies.' The 'dies' is 'before the second shot.' The 'before' is 'you lose.'",
	"You picked Terran Dominion. The 'versatile' faction. Siege Tanks, Wraiths, and Goliaths. Everything transforms. Everything is flexible. Everything is also MICRO-INTENSIVE. While you micro, I macro. The macro is 'more units.' The 'more units' is 'you lose.' The 'you lose' is 'by volume.'",
	"Terran Dominion -- your Wraiths cloak. Invisible fighters. Very sneaky. Cloaking costs energy. Energy runs out. The 'out' is 'visible.' The 'visible' is 'shot down.' The 'shot down' is 'your Wraiths are gone.' The 'gone' is 'expensive.' The 'expensive' is 'you can't replace them.'",
	"Terran player, your Goliath is anti-air and anti-ground. Versatile. But versatile means 'okay at everything, great at nothing.' My units are GREAT at one thing. The 'great' beats 'okay.' The 'beats' is 'ALWAYS.' The 'ALWAYS' is 'you lose.' The 'you lose' is 'by specialization.'",
}
