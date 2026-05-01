actor-e1 =
   .description = General-purpose infantry.
        Strong vs Infantry
        Weak vs Vehicles, Aircraft
   .description-jp = JGSDF infantryman from Gate armed
      with a battle rifle.
        Strong vs Infantry
        Weak vs Vehicles, Aircraft
   .raname = Rifle Infantry
   .tdname = Minigunner
   .jpname = Imperial Scoutsman

actor-e2 =
   .description = Fast infantry armed with grenades.
        Strong vs Buildings, slow-moving targets
   .name = Grenadier

actor-e3 =
   .description = Anti-tank/Anti-aircraft infantry.
        Strong vs Tanks, Aircraft
        Weak vs Infantry
   .raname = RA Rocket Soldier
   .tdname = TD Rocket Soldier

actor-e4 =
   .description = Advanced anti-structure unit.
        Strong vs Infantry, Buildings
        Weak vs Vehicles, Aircraft
   .raname = Soviet Flamethrower
   .tdname = Nod Flamethrower
   .jpname = Japanese Flamethrower

actor-e5 =
   .description = Advanced general-purpose infantry.
        Strong vs all Ground units
   .name = Chemical Warrior

actor-e6 =
   .description = Infiltrates and captures enemy structures.
      Removes attached explosives from units.
        Unarmed
   .name = Engineer

actor-apc =
   .description = Armed infantry transport.
        Can attack Aircraft.
   .tdname = GDI APC
   .raname = Allied APC

actor-arty =
   .description = Long-range artillery.
        Strong vs Buildings
        Weak vs Air
   .tdname = Nod Artillery
   .raname = Allied Artillery

actor-ftnk =
   .description = Heavily armored flame-throwing vehicle.
        Strong vs Infantry, Buildings
   .name = Flame Tank

actor-bggy =
   .description = Scout vehicle armed with a machine gun.
        Strong vs Infantry
   .name = Nod Buggy

actor-jeep =
   .description = Scout vehicle armed with a machine gun.
        Strong vs Infantry
   .description2 = Scout vehicle armed with a recoilless gun.
        Strong vs Infantry, Light Vehicles, Aircraft
   .description3 = Scout vehicle armed with a rocket launcher.
        Strong vs Vehicles, Aircraft
   .name = Humvee
   .raname = Ranger
   .recon = Recon Ranger

actor-bike =
   .description = Scout vehicle armed with rockets.
      Can attack aircraft.
        Strong vs Vehicles
   .name = Recon Bike

actor-ltnk =
   .description = Fast, light tank.
        Strong vs Vehicles
        Weak vs Infantry
   .tdname = Nod Light Tank
   .raname = Allied Light Tank

actor-mtnk =
   .description = Main battle tank.
      Can attack aircraft with a missile launcher.
        Strong vs Vehicles
   .name = GDI Battle Tank

actor-htnk =
   .description = Heavily armored tank.
      Can attack aircraft.
        Strong vs Everything
   .name = GDI Mammoth Tank
   .raname = Soviet Mammoth Tank

actor-mlrs =
   .description = Long range rocket artillery.
        Can attack aircraft.
   .name = Rocket Launcher

actor-mssm =
   .description = Long range rocket artillery.
        Strong vs Buildings
   .name = SSM Launcher

actor-stnk =
   .description = Cloaked tank armed with missiles.
        Can attack aircraft.
   .name = Stealth Tank

actor-tran =
   .description = Infantry transport helicopter.
        Unarmed
   .name = Chinook Transport

actor-heli =
   .description = Helicopter armed with chainguns.
        Strong vs Infantry, Light Vehicles, Aircraft
   .name = Apache

actor-orca =
   .description = Aircraft armed with missiles.
      Strong vs Buildings, Vehicles
   .name = Orca

actor-nuke =
   .description = Generates power.
   .name = Power Plant

actor-nuk2 =
   .description = Generates more power than
      a standard power plant.
   .name = Advanced Power Plant

actor-afld =
   .description = Provides a drop zone for vehicle reinforcements.
      Produces Nod vehicles.
   .name = Airstrip

actor-hq-gdi =
   .description = Provides radar, advanced technologies,
      and Air Strike support power.
      Requires power to operate.
   .name = GDI Communications Center

power-td-airstrike =
   .description = A-10 planes strafe and bomb the target.
   .name = Air Strike

actor-eye =
   .description = Provides advanced GDI technologies.
      Can be upgraded with an ion cannon uplink.
   .name = Advanced Communications Center
   .ionc-description = Upgrades the Advanced Communications Center
      with an Ion Cannon Uplink.
      Provides access to Ion Cannon strikes.
      Increases durability of the building.
   .ionc-name = Ion Cannon Uplink

power-ioncannon =
   .name = Ion Cannon
   .description = Initiate an Ion Cannon strike.
      Applies instant damage to a small area.

power-clustermissile =
   .name = Cluster Missile
   .description = Tactical cluster missile.
   Deals heavy damage in an area.

power-chemicalmissile =
   .name = Chemical Missile
   .description = Tactical cluster missile with additional Tiberium-based warheads.
   Deals heavy damage in an area and leaves harmful corrosive clouds.

actor-tmpl =
   .description = Provides advanced Nod technologies.
      Can be upgraded with a missile silo.
   .name = Temple of Nod
   .nuke-description = Upgrades the Temple of Nod with a
      nuclear missile silo.
      Provides access to Nuclear Strikes.
      Increases durability of the building.
   .nuke-name = Nuclear Missile Silo

power-nuke =
   .tdname = Nuclear Strike
   .raname = Atomic Bomb
   .d2kname = Nuclear Weapon
   .description = Launches a nuclear missile.
      Applies heavy damage over a large area.
   .d2kdescription = Launches a missile which payload releases a shockwave that devastates a large area,
   while electro-magnetic pulse disables buildings around the target for some time.

actor-gun =
   .tdname = Nod Gun Turret
   .raname = Allied Gun Turret
   .description = Anti-tank base defense.
        Strong vs Vehicles

actor-nalasr =
   .name = Nod Laser Turret
   .description = Anti-infantry defense.
      Requires power to operate.
        Strong vs Infantry

actor-sam =
   .tdname = Nod SAM Site
   .raname = Soviet SAM Site
   .ra2name = Patriot Missile System
   .description = Anti-aircraft defense.
      Requires power to operate.

actor-obli =
   .name = Obelisk of Light
   .description = Advanced anti-ground defense.
      Requires power to operate.
        Strong vs Ground targets

actor-gtwr =
   .name = Guard Tower

actor-atwr =
   .name = Advanced Guard Tower
   .description = All-purpose defensive structure.
      Can attack aircraft.
      Requires power to operate.
        Strong vs Aircraft, Tanks
        Weak vs Infantry

actor-sbag =
   .name = Sandbag Barrier
   .description = Stops infantry and light vehicles.
      Can be crushed by tanks.

actor-cycl =
   .name = Chain Link Barrier
   .description = Stops infantry and light vehicles.
      Can be crushed by tanks.

actor-brik =
   .name = Concrete Barrier
   .description = Stops infantry and most tanks.
      Blocks some projectiles.

upgrade-upnodstealth =
   .name = Unlock Stealth Technology
   .description = Allows training of Stealth Soldiers
      and construction of Stealth Harvesters.

upgrade-upnodvenom =
   .name = Unlock Venom and Laser Commando
   .description = Allows construction of the Venom Gunship
      and training of Laser Commando.

actor-cheme3 =
   .name = Chemical Rocket Soldier
   .description = Soldier armed with volatile Tiberium rockets.
      Can walk over Tiberium unharmed.
        Strong vs Vehicles, Aircraft

actor-ltnk2 =
   .name = Light Tank Mk. II
   .description = Light tank with point defense lasers.
        Strong vs Infantry, Vehicles

actor-blackhandlaser =
   .name = Laser Trooper
   .description = Elite infantry armed with lasers.
        Strong vs ground targets

actor-blackhandflamer =
   .name = Black Hand Flamer
   .description = Elite stealth flamethrower.
        Strong vs Infantry, Buildings

actor-stealthsoldier =
   .name = Stealth Soldier
   .description = Cloaked infantry armed with EMP missiles.
        Strong vs Vehicles

actor-specter =
   .name = Specter
   .description = Long-range stealth artillery.
        Strong vs Buildings
        Weak vs Air

actor-venom =
   .name = Venom
   .description = Gunship armed with lasers.
        Strong vs Infantry, Air

actor-lasercommando =
   .name = Laser Commando
   .description = Stealth commando armed with lasers.
      Can shoot down missiles.
      Can demolish buildings.
        Strong vs Everything

actor-empgrenadier =
   .name = EMP Grenadier
   .description = Can throw grenades that disable vehicles.
        Strong vs Ground

actor-gdisniper =
   .name = GDI Sniper
   .description = Sniper armed with an anti-materiel rifle.
      Can attack air.
        Strong vs Infantry, Air

actor-gdipredator =
   .name = Predator Tank
   .description = Tank that can paint enemies with a targeting laser,
      making them take increased damage.
      Can attack air.
        Strong vs Everything

actor-exosuit =
   .name = Experimental Exosuit
   .description = Walker armed with railguns.
        Strong vs Ground

actor-gdiofficer =
   .name = GDI Officer
   .description = Officer with a heavy machine gun.
      Gives morale boost to nearby units to increase
      movement speed and fire-rate by 20%.
        Can attack air

actor-havoc =
   .name = Havoc
   .description = Commando armed with numerous weapons.
      Strong vs Everything

actor-firehawk =
   .name = Firehawk
   .description = Advanced fighter-bomber aircraft.
      Strong vs Everything

actor-nodlasercorvette =
   .description = Multi-role heavy warship.
      Can attack air.
        Strong vs Everything
   .name = Nod Laser Corvette

actor-dog =
   .name = Attack Dog
   .description = Can detect cloaked and disguised units.
      Strong vs Infantry

actor-rasniper =
   .name = Allied Sniper
   .description = Camouflaged soldier that can detect cloaked units.
        Strong vs Infantry

actor-medi =
   .description = Heals nearby infantry.
        Unarmed
   .name = Medic

actor-mech =
   .description = Repairs nearby vehicles and restores
      husks to working condition by capturing them.
      Unarmed
   .name = Mechanic

actor-spy =
   .description = Infiltrates enemy structures for intel or
    sabotage. Exact effect depends on the
    building infiltrated.
    Loses disguise when attacking.
    Can detect spies.
   .disguisetooltip-name = Spy
   .disguisetooltip-generic-name = Soldier

actor-v1truck =
   .description = Dumb fire rocket artillery.
      Strong vs. Infantry, Buildings.
      Weak vs. Aircraft, Tanks
   .name = V1 Truck

actor-v2rl =
   .description = Long-range rocket artillery.
      Strong vs Infantry, Buildings
      Weak vs Vehicles, Aircraft
   .name = V2 Rocket Launcher

actor-2tnk =
   .description = Allied main battle tank.
      Strong vs Vehicles
      Weak vs Infantry, Aircraft
   .name = Allied Medium Tank

actor-heavyaatank =
   .description = Allied heavy anti air tank.
      Strong vs Aircraft
      Weak vs Tanks
   .name = Allied Heavy AA Tank

actor-3tnk =
   .description = Soviet main battle tank armed with dual cannons.
      Strong vs Vehicles
      Weak vs Infantry, Aircraft
   .name = Soviet Heavy Tank

actor-mnly =
   .description = Lays mines to destroy unwary enemy units.
      Can detect and clear mines.
      Unarmed.
   .name = Minelayer

actor-ttnk =
   .description = Tank with mounted Tesla coil.
      Strong vs Ground
      Weak vs Aircraft
   .name = Tesla Tank

actor-ftrk =
   .description = Mobile unit with mounted Flak cannon.
      Strong vs Infantry, Light armor, Aircraft
      Weak vs Tanks
   .name = Mobile Flak

actor-dtrk =
   .description = Truck with actively armed nuclear
    explosives. Has very weak armor.
   .name = Demolition Truck

actor-ctnk =
   .description = Armed with missiles.
    Can teleport anywhere.
      Strong vs Everything
   .name = Chrono Tank

actor-qtnk =
   .description = Deals seismic damage to nearby vehicles
    and structures.
      Strong vs Vehicles, Buildings
      Weak vs Infantry, Aircraft
   .name = MAD Tank
   .generic-name = Tank

actor-mgg =
   .description = Regenerates the shroud nearby,
    obscuring the area.
        Unarmed
   .name = Mobile Gap Generator

actor-mrj =
   .name = Mobile Radar Jammer
   .description = Jams nearby enemy radar domes
    and deflects incoming missiles.
        Unarmed

actor-tiger =
   .name = Tiger Heavy Tank
   .description = Advanced main battle tank.
      Immune to EMP.

actor-tnkd =
   .name = Tank Destroyer
   .description = Strong vs Vehicles
        Weak vs Infantry, Aircraft

actor-sapc =
   .description = Lightly armored infantry transport which
    can cloak. Armed with anti-ground missiles.
        Strong vs Light armor
        Weak vs Infantry, Tanks, Aircraft
   .name = Phase Transport

actor-sheridan =
   .description = General-purpose light tank.
      Can attack air with missiles.
        Strong vs Infantry, vehicles
   .name = Sheridan

actor-rapierjumpjet =
   .description = Fast multirole fighter-bomber.
     Strong vs Aircraft, Buildings
   .name = Rapier Jumpjet

upgrade-gapgen =
   .description = Allows construction of the Gap Generator
      and the Mobile Gap Generator and the Phase Transport.
   .name = Unlock Gap Generator Technology

actor-mortarsoldier =
   .name = Mortar Soldier
   .description = Long range siege infantry.
        Strong vs Infantry, Buildings
        Weak vs Aircraft

actor-shok =
   .description = Elite infantry with portable Tesla coils.
        Strong vs Infantry, Vehicles
        Weak vs Aircraft
   .name = Shock Trooper

actor-volkov =
   .description = Soviet Experimental Cyborg Super Soldier
   armed with a magnetic pistol and exploding bullets.
        Strong vs Infantry, Vehicles
        Weak vs Aircraft
   .name = Volkov

actor-gtnk =
   .description = Mobile unit with mounted gatling cannon.
        Strong vs Infantry, Light armor, Aircraft
        Weak vs Tanks
   .name = Gatling Tank

actor-ttnk2 =
   .description = Tank with mounted tesla coil.
      Can shoot down missiles.
      Strong vs Ground
   .name = Heavy Tesla Tank

actor-monstertank =
   .description = Remote-controlled supertank, armed
      with nuclear cannons and heavy missiles.
      Almost indestructible.
        Strong vs Everything
   .name = Monster Tank

actor-btr =
   .description = NBC-protected infantry transport,
      armed with an autocannon.
      Can attack air.
   .name = BTR-80

actor-kotin =
   .description = Tank armed with nuclear shells.
      Attacks leave radiation.
        Strong vs Vehicles
   .name = Kotin Nuclear Tank

actor-mignuke =
   .description = Fast nuclear bomber.
        Strong vs Buildings
   .name = Supersonic Nuclear Bomber

actor-mig =
   .description = Multirole fighter.
      Strong vs Vehicles, Aircraft
      Weak vs air defenses
   .name = MiG

actor-yak =
   .description = Attack plane armed with
    dual machine guns.
      Strong vs infantry, Light armor
      Weak vs air defenses
   .name = Yak

actor-su57 =
   .description = Multirole fighter-bomber.
        Strong vs Everything
   .name = Su-57 Attack Bomber

actor-raheli =
   .description = Helicopter gunship armed
    with multi-purpose missiles.
      Strong vs Buildings, Vehicles, Aircraft
      Weak vs infantry, air defenses
   .name = Longbow

actor-hind =
   .description = Helicopter gunship armed
    with dual chainguns.
      Strong vs infantry, Light armor
      Weak vs air defenses
   .name = Hind

actor-mh60 =
   .description = Helicopter gunship armed
    with dual chainguns.
      Strong vs infantry, Light armor
      Weak vs air defenses
   .name = Black Hawk

actor-raafld =
   .description = Produces aircraft.
      Provides Spy Plane, Paratroopers, and Parabombs.
   .name = Soviet Airfield

actor-atek =
   .description = Provides advanced technologies
      and a GPS satellite.

power-gps =
   .name = GPS Satellite
   .description = Reveals locations of enemy units.
      Requires power to operate.

power-spyplane =
   .description = Reveals an area of the map and cloaked enemy units.
   .name = Spy Plane

power-paratroopers =
   .description = Drops infantry at the targeted location.
   .name = Paratroopers

power-reinforcements =
   .description = Drops infantry and vehicles at the targeted location.
   .name = Rapid Reinforcements

power-parabombs =
   .description = Badgers drop bombs at the targeted location.
   .name = Parabombs

power-super-bombers =
   .description = Super Bombers drop bombs at the targeted location.
   .name = Super Bomber Airstrike

actor-mslo =
   .description = Provides an atomic bomb.
      Requires power to operate.
   .name = Missile Silo

actor-iron =
   .description = Makes vehicles temporarily invincible.
      Requires power to operate.
   .name = Iron Curtain

power-ironcurtain =
   .description = Makes a group of units temporarily invulnerable.
   .name = Invulnerability

actor-pdox =
   .description = Teleports vehicles across the map.
      Requires power to operate.
   .name = Chronosphere

power-chronoshift =
   .description = Teleports a group of units across the map.
   .name = Chronoshift

actor-pbox =
   .name = Pillbox
   .description = Static defense with a fireport for
    a garrisoned soldier.

actor-hbox =
   .name = Camo Pillbox
   .description = Camouflaged static defense with a fireport
    for 2 garrisoned soldiers.
    Can detect cloaked units.

actor-agun =
   .description = Anti-air base defense.
    Requires power to operate.
   .name = AA Gun

actor-gap =
   .name = Gap Generator
   .description = Obscures the enemy's view with shroud.
    Requires power to operate.

actor-ftur =
   .description = Anti-Infantry base defense.
      Strong vs Infantry, Light armor
      Weak vs Tanks, Aircraft
   .name = Flame Tower

actor-tsla =
   .description = Advanced base defense.
    Requires power to operate.
    Can detect cloaked units.
      Strong vs Vehicles, Infantry
      Weak vs Aircraft
   .name = Tesla Coil

actor-bastion =
   .description = Bunker with a cannon and fireports
      for 3 garrisoned soldiers.
   .name = Bastion Artillery Bunker

actor-mgnest =
   .description = Basic base defense.
      Can attack aircraft.
        Strong vs Infantry, Aircraft
        Weak vs Tanks
   .name = MG Nest

actor-waveforceturret =
   .description = Advanced base defense that
      damages enemies in a line.
        Strong vs Ground
   .name = Waveforce Turret

actor-rasamurai =
   .description = Melee warrior armed with a katana.
        Strong vs Infantry
        Weak vs Aircraft
   .name = Samurai

actor-ramaid =
   .description = Artillery and anti-air infantry.
        Strong vs Infantry, Aircraft
        Weak vs Tanks
   .name = Archer Maiden

actor-rocketangel =
   .description = Airborne soldier.
        Strong vs Infantry, Aircraft
        Weak vs air defenses
   .name = Rocket Angel

actor-hakurei =
   .description = Shrine maiden specializing in
   exterminating youkai.
   Can deploy to cast a spell card
   and gain temporary
   invulnerability.
     Strong vs Heroes
   .name = Exorcist

actor-typeigo =
   .name = I-Go Medium Tank

actor-typechiha =
   .name = Chi-Ha Heavy Tank

actor-nanobggy =
   .description = Remote control vehicle for a nanite swarm.
   Attacks bounce and heal nearby units.
        Strong vs Infantry
        Weak vs Tanks, Aircraft
   .name = Nanodrone Buggy

actor-jphover =
   .description = Amphibious infantry transport.
      Can attack air.
   .name = Hovercraft

actor-jpcore =
   .name-factory = Core (War Factory)
   .description-factory = Deploys into a War Factory.
      Amphibious
   .name-refinery = Core (Refinery)
   .description-refinery = Deploys into a Refinery.
      Amphibious
   .name-power = Core (Power Plant)
   .description-power = Deploys into a Waveforce Reactor.
      Amphibious
   .name-barracks = Core (Barracks)
   .description-barracks = Deploys into a Barracks.
      Amphibious
   .name-radar = Core (Radar)
   .description-radar = Deploys into a Radar Array.
      Amphibious
   .name-airfield = Core (Airfield)
   .description-airfield = Deploys into an Airfield.
      Amphibious
   .name-service-depot = Core (Service Depot)
   .description-service-depot = Deploys into a Service Depot.
      Amphibious
   .name-tech-center = Core (Tech Center)
   .description-tech-center = Deploys into a Tech Center.
      Amphibious

actor-waveforcetank =
   .name = Waveforce Tank
   .description = Tank that damages enemies in a line.
      Strong vs Vehicles, Infantry
      Weak vs Aircraft

actor-waveforcearty =
   .name = Waveforce Artillery
   .description = Artillery that damages enemies in a line.
      Strong vs Ground
      Weak vs Aircraft

actor-jpbggy =
   .name = Grenade Buggy
   .description = Fast scout and anti-infantry vehicle.
      Strong vs Infantry
      Weak vs Tanks

actor-jphovert =
   .name = Hovercraft Flametank
   .description = Amphibious flamethrower.
      Strong vs Infantry, Buildings
      Weak vs Tanks, Aircraft

actor-jpbomber =
   .name = Japanese Bomber
   .description = Aircraft equipped with machine guns and bombs.
      Strong vs Infantry, Buildings
      Weak vs Air Defense, Aircraft

actor-oitank =
   .name = O-I Tank
   .description = Superheavy tank with multiple turrets.
        Strong vs Ground Targets
        Weak vs Aircraft
   .name-exorcist = Exorcist O-I Tank
   .description-exorcist = Superheavy mobile shrine powered by
   an onboard exorcist.
        Strong vs Everything

actor-ss =
   .description = Submerged anti-ship unit
    armed with torpedoes.
    Can detect other submarines.
      Strong vs Naval units
      Weak vs Ground units, Aircraft
   .name = Soviet Submarine

actor-msub =
   .description = Submerged anti-ground siege unit
    with anti-air capabilities.
    Can detect other submarines.
      Strong vs Buildings, Ground units, Aircraft
      Weak vs Naval units
   .name = Missile Submarine

actor-dd =
   .description = Fast multi-role ship.
    Can detect submarines.
      Strong vs Naval units, Vehicles, Aircraft
      Weak vs Infantry
   .name = Destroyer

actor-ca =
   .description = Very slow long-range ship.
      Strong vs Buildings, Ground units
      Weak vs Naval units, Aircraft
   .name = Cruiser

actor-lst =
   .description = General-purpose naval transport.
    Can carry infantry and tanks.
      Unarmed
   .name = Transport

actor-pt =
   .description = Light scout & support ship.
    Can detect submarines.
      Strong vs Naval units
      Weak vs Ground units, Aircraft
   .name = Gunboat

actor-jpspeedboat =
   .description = Fast attack craft armed with
      gatling cannons.
      Can attack air.
        Strong vs Aircraft
        Weak vs Ships
   .name = Japanese Speedboat

actor-japancarrier =
   .description = Very slow long-range ship.
      Can attack air.
        Strong vs Buildings, Ground units
        Weak vs Naval units, Aircraft
   .name = Japanese Aircraft Carrier

actor-yamatobattleship =
   .description = Very slow long-range ship.
      Can attack air.
        Strong vs Buildings, Ground units
        Weak vs Naval units, Aircraft
   .name = Yamato Battleship

actor-ra2e1 =
   .name = G. I.
   .description = General-purpose infantry.
   Can deploy for additional firepower.
        Strong vs Infantry
        Weak vs Air

actor-ggi =
   .name = Guardian GI
   .description = Anti-vehicle infantry.
   Can deploy to use anti-tank
   and anti-air missiles.
        Strong vs Vehicles
        Weak vs Infantry

actor-cleg =
   .name = Chrono Legionnaire
   .description = Freezes targets in time
   and erases them.
        Strong vs single targets
        Weak vs Aircraft

actor-ra2snipe =
   .name = Sniper
   .description = Strong vs Infantry

actor-seal =
   .name = SEAL
   .description = Elite amphibious infantry.
      Can demolish buildings and ships.
        Strong vs Infantry, Buildings, Ships
        Weak vs Vehicles

actor-ra2spy =
   .description = Infiltrates enemy structures for intel or
    sabotage. Exact effect depends on the
    building infiltrated.
    Can detect spies.
   .name = Spy

actor-ra2rock =
   .description = Flying infantry.
        Strong vs Infantry, Aircraft
        Weak vs Air Defense
   .name = Rocketeer

actor-cmin =
   .name = Chrono Miner
   .description = Collects resources and teleports to refinery when full.

actor-robo =
   .name = Robot Tank
   .description = Light unmanned tank.
      Can hover over water.
      Immune to mind control.
      Requires Robot Control to operate.
        Strong vs Vehicles

actor-fv =
   .name = Infantry Fighting Vehicle
   .description = Changes turret with passenger.

actor-sref =
   .name = Prism Tank
   .description = Armed with a prism cannon.
        Strong vs Infantry, Buildings
        Weak vs Tanks, Aircraft

actor-mgtk =
   .name = Mirage Tank
   .description = Disguises when stationary.
        Strong vs Infantry, Vehicles
        Weak vs Aircraft

actor-ra2tnkd =
   .name = Tank Destroyer
   .description = Armed with a sabot cannon.
        Strong vs Vehicles

actor-shad =
   .name = Night Hawk
   .description = Transport helicopter armed with a machine gun.

actor-falc =
   .name = Harrier
   .description = VTOL fighter armed with missiles.
        Strong vs Vehicles, Aircraft
        Weak vs Air Defense

actor-beag =
   .name = Black Eagle
   .description = VTOL heavy bomber.
      Strong vs Ground
        Weak vs Aircraft, Air Defense

actor-dest =
   .name = Destroyer
   .description = Armed with a cannon.
      Can deploy an Osprey against submarines.
        Strong vs Ships
        Weak vs Air

actor-aegis =
   .name = Aegis Cruiser
   .description = Anti-aircraft missile warship.
        Strong vs Air
        Weak vs Ships

actor-ra2carrier =
   .name = Aircraft Carrier
   .description = Deploys fighters against targets.
        Strong vs Ground
        Weak vs Aircraft, Ships

actor-dlph =
   .name = Dolphin
   .description = Armed with a sonic beam.
      Can remove squids from ships.
        Strong vs Ships

actor-flakt =
   .name = Flak Trooper
   .description = Anti-air infantry.
        Strong vs Infantry, Aircraft
        Weak vs Vehicles

actor-shk =
   .description = Heavy infantry with portable Tesla coils.
      Can charge tesla coils.
        Strong vs Infantry, Vehicles
        Weak vs Aircraft
   .name = Tesla Trooper

actor-ra2harv =
   .name = War Miner
   .description = Collects resources.
      Armed with a machinegun.

actor-deso =
   .name = Desolator
   .description = Armed with a radiation weapon.
      Can deploy to irradiate area.
        Strong vs Infantry

actor-terror =
   .name = Terrorist
   .description = Suicide bomber.
      Explodes when killed

actor-ivan =
   .name = Crazy Ivan
   .description = Plants time bombs on targets.
      Explodes when killed

actor-boris =
   .name = Boris
   .description = Commando armed with a rifle.
      Can send airstrikes against buildings.
        Strong vs Ground

actor-htk =
   .name = Flak Track
   .description = Troop transport armed with a flak cannon.
        Strong vs Infantry, Aircraft
        Weak vs Vehicles

actor-v3 =
   .name = V3 Rocket Launcher
   .description = Long-range rocket artillery.
      Rockets can be shot down
        Strong vs Buildings
        Weak vs Air

actor-dron =
   .name = Terror Drone
   .description = Infects and damages vehicles.
      Instantly kills infantry.

actor-schp =
   .name = Siege Chopper
   .description = Helicopter that can deploy into a cannon.
        Strong vs Infantry when airborne
        Strong vs Buildings when deployed
        Weak vs Aircraft, Air Defense

actor-ra2hind =
   .name = Hind Transport
   .description = Carryall helicopter armed with a machine gun.

actor-zep =
   .name = Kirov Airship
   .description = Heavy bomber airship.
        Strong vs Ground
        Weak vs Air

actor-bpln =
   .name = MiG Bomber
   .description = Fast multirole fighter-bomber.
        Strong vs Ground, Aircraft
        Weak vs Air Defense

actor-ra2sub =
   .name = Typhoon Attack Sub
   .description = Armed with torpedoes.
        Strong vs Ships

actor-hyd =
   .name = Sea Scorpion
   .description = Armed with a flak cannon.
        Strong vs Air
        Weak vs Ships

actor-dred =
   .name = Dreadnought
   .description = Siege warship armed with missiles.
      Missiles can be shot down.
        Strong vs Ground
        Weak vs Ships, Air

actor-sqd =
   .name = Giant Squid
   .description = Can entangle ships.
        Strong vs Ships

actor-brute =
   .name = Brute
   .description = Melee infantry.
        Strong vs Infantry, Vehicles

actor-virus =
   .name = Virus
   .description = Armed with a toxin rifle.
      Victims explode and release toxins
        Strong vs Infantry

actor-yuri =
   .name = Yuri Clone
   .description = Can mind-control enemy units.
      Can deploy to unleash a psychic blast.
        Weak vs Aircraft, Robots

actor-yurix =
   .name = Yuri Prime
   .description = Can mind-control enemy units and buildings.
      Can deploy to unleash a psychic blast.
        Weak vs Aircraft, Robots

actor-gtrp =
   .name = Gatling Trooper
   .description = Armed with a gatling gun.
        Strong vs Infantry, Aircraft

actor-biot =
   .name = Bio Trooper
   .description = Armed with a toxin spray.
        Strong vs Infantry, Buildings

actor-ytnk =
   .name = Gatling Tank
   .description = Tank armed with dual gatling cannons.
      Firepower increases during sustained firing
        Strong vs Infantry, Aircraft
        Weak vs heavy vehicles

actor-tele =
   .name = Magnetron
   .description = Immobilizes and disables vehicles.
        Strong vs Vehicles, Buildings
        Weak vs Infantry, Aircraft

actor-caos =
   .name = Chaos Drone
   .description = Releases a gas that causes units
      to go berserk and attack each other.

actor-mind =
   .name = Master Mind
   .description = Can mind control up to 3 units
      without damaging self.

actor-disk =
   .name = Floating Disc
   .description = Flying saucer armed with lasers.
      Can shut down power plants and defenses when overhead
      Can steal resources from refineries and silos
        Strong vs Everything

actor-bsub =
   .name = Boomer
   .description = Submarine armed with torpedoes
      and cruise missiles.
      Missiles can be shot down.
        Strong vs Everything

actor-smin =
   .name = Mobile Slave Miner
   .description = Deploys into a Slave Miner.
      Armed with a machine-gun.

actor-yarefn =
   .name = Slave Miner
   .description = Processes resources.
      Armed with a machine-gun.

actor-gaairc =
   .name = Airforce Command HQ
   .description = Produces aircraft and provides radar.

actor-atesla =
   .name = Prism Tower
   .description = Advanced base defense.
   Requires power to operate.
        Strong vs Ground

actor-gtgcan =
   .name = Grand Cannon
   .description = Long-range base defense.
   Requires power to operate.
        Strong vs Ground

actor-gaorep =
   .name = Ore Purifier
   .description = Increases income from minerals by 25%.

actor-garobo =
   .name = Robot Control Center
   .description = Unlocks and controls Robot Tanks.

actor-gaweat =
   .name = Weather Control Center
   .description = Provides Lightning Storm power.
      Requires power to operate.

actor-gaspysat =
   .name = Spy Satellite Uplink
   .description = Provides Spy Satellite scans.

actor-nanrct =
   .name = Nuclear Reactor
   .description = Generates power.
      Explodes violently when destroyed.

actor-nabnkr =
   .name = Battle Bunker
   .description = Defense with fireports for
      6 garrisoned soldiers.

actor-ra2tesla =
   .name = Tesla Coil
   .description = Advanced base defense.
      Can be charged by up to 3 tesla troopers.
      Strong vs Ground

actor-namisl =
   .name = Nuclear Missile Silo
   .description = Provides a nuclear missile.
      Requires power to operate.

actor-yapowr =
   .name = Bio Reactor
   .description = Generates power.
      Can be loaded to increase output.
      Explodes violently if destroyed when loaded.

actor-napsis =
   .name = Psychic Sensor
   .description = Provides radar and Psychic Reveal power.
      Requires power to operate.

actor-natbnk =
   .name = Tank Bunker
   .description = Defense with a fireport for a
      garrisoned tank.

actor-yaggun =
   .name = Gatling Cannon
   .description = Anti-infantry and anti-aircraft defense.
      Requires power to operate

actor-yapsyt =
   .name = Psychic Tower
   .description = Can mind-control units.
      Can detect cloaked units.
      Requires power to operate.

actor-yappet =
   .name = Psychic Dominator
   .description = Provides Psychic Dominator power.
      Requires power to operate.

actor-yagntc =
   .name = Genetic Mutator
   .description = Provides Genetic Mutator power.
      Requires power to operate.

actor-tsntwast =
   .name = Tiberium Waste Facility
   .description = Increases income from minerals by 25%.
   Unlocks Chemical Missile for the Missile Silo.

actor-tsntpowr =
   .name = Power Plant
   .description = Provides power for other structures.

actor-tsntradr =
   .name = Radar
   .description = Provides radar.
   Unlocks higher-tech units and buildings.
   Requires power to operate.

actor-tsgtradr =
   .name = Radar
   .description = Provides radar.
   Unlocks higher-tech units and buildings.
   Requires power to operate.

actor-tscabaltech =
   .name = Radar
   .description = Provides CABAL advanced technologies.
   Only one may be built at a time.

actor-tscore =
   .name = Radar
   .description = Provides CABAL advanced technologies.
   Provides Hunter-Seeker support power.

actor-tsntmisl =
   .name = Missile Silo
   .description = Constructs and launches long-range missiles as support power.

actor-tsgtplug =
   .name = Upgrade Center
   .description = Provides Hunter-Seeker and Ion Cannon support powers.

actor-tsntlasr =
   .name = Laser Turret
   .description = Basic base defense.
   Strong vs Ground units.
   Cannot attack Aircraft.

actor-tsntsam =
   .name = SAM Site
   .description = Anti-aircraft missile battery.
   Requires power to operate.
   Strong vs Aircraft.
   Cannot attack Ground units.

actor-tsntstlh =
   .name = Stealth Generator
   .description = Generates a cloaking field to hide your forces from the enemy.
   Only one can be built at a time.

actor-tsntpulsgdi =
   .name = E.M. Pulse Cannon
   .description = Launches electro-magnetic pulse that disables mechanical units in an area.
   Requires power to operate.

actor-tsntobel =
   .description = Advanced base defense.
   Requires power to operate.
   Strong vs Ground units.
   Cannot attack Aircraft.

power-ra2spysat =
   .name = Spy Satellite
   .description = Periodically reveals the entire map.
   Activated automatically.

power-ra2spyplane =
   .name = Spy Plane
   .description = Reveals area along a line.
      Does not detect cloaked units.

power-ra2psireveal =
   .name = Psychic Reveal
   .description = Reveals an area of the map and cloaked enemy units.

power-lightningstorm =
   .name = Lightning Storm
   .description = Creates a lightning storm.
      Applies heavy damage over a large area.

power-ra2nuke =
   .name = Nuclear Missile
   .description = Launches a devastating nuclear missile.
      Applies heavy damage and radiation over a large area.

power-psychicdominator =
   .name = Psychic Dominator
   .description = Unleashes a wave of psionic energy.
      Applies heavy damage to buildings.
      Mind-controls units.

power-mutate =
   .name = Genetic Mutator
   .description = Mutates infantry in an area
      into Brutes.

power-forceshield =
   .name = Force Shield
   .description = Makes selected buildings temporarily invulnerable.

actor-aashinobi =
   .description = Stealth soldier.
      Can infiltrate buildings.

actor-alligator =
   .description = Large reptile.
       Strong vs Infantry, Vehicles
       Weak vs Defenses, Aircraft

actor-aaquas =
   .description = Light scout vehicle.
      Can attack aircraft.
       Strong vs Infantry, Air

actor-aapulv =
   .description = Armed with a gatling cannon.
       Strong vs Infantry, Air

actor-aahowi =
   .description = Mobile heavy artillery.

actor-aaviper =
   .description = Hovering mobile artillery.
      Attacks leave toxin clouds.

actor-aamecha =
   .description = Robot armed with machine guns.
      Can attack air.

actor-aapanth =
   .description = Amphibious transport armed with a cannon.

actor-aaoilt =
   .description = Truck carrying fuel.
      Explodes violently when destroyed or deployed.

actor-aapelican =
   .description = Helicopter armed with missiles.
      Can attack air.

actor-aaphoenix =
   .description = Multirole fighter-bomber.
      Can attack air.

actor-aagunb =
   .description = Fast gunboat armed with a cannon and a flak gun.
      Can attack air.

actor-aaquasfrig =
   .description = Warship armed with plasma cannons.
      Can attack air.

actor-aacarrier =
   .description = Aircraft carrier that deploys 4 fighters
      armed with machine guns.

actor-aatsun =
   .description = Aircraft carrier that deploys 3 fighters
      armed with chemical bombs.

actor-aaksub =
   .description = Suicide bomber submarine.

actor-cgpnch =
   .description = Defense buildable on water.
      Can detect submarines.
       Strong vs Ships, Submarines

actor-cghype =
   .description = Anti-aircraft tractor beam.
      Requires power to operate.
       Strong vs Air

actor-cgtnkr =
   .description = Fill with Oil Trucks to generate more power.

actor-cgchtw =
   .description = Defense that dispenses chaos gas.
      Requires power to operate

actor-cgplas =
   .description = Long range anti-ground defense.
      Requires power to operate

actor-cgchao =
   .description = Provides Chaos Storm support power.
      Requires power to operate.

actor-cgionc =
   .description = Provides Ion Cannon support power.
      Requires power to operate.

power-chaos =
   .name = Chaos Storm
   .description = Releases chaos gas on a target area.

actor-steel_board_inf =
   .description = Armed with anti-vehicle grenades.

actor-steel_mako =
   .description = Main battle hoverbike.

actor-steel_manta =
   .description = Anti-infantry and anti-air vehicle.

actor-steel_qtank =
   .description = Main battle tank.
    Deploy to switch to an artillery gun.

actor-steel_beholder =
   .description = Armed with powerful anti-ground lasers.

actor-steel_katy =
   .description = Heavy anti-ground tank.

actor-steel_mega =
   .description = Melee fighting robot.

actor-steel_savi =
   .description = Protects nearby units with shields.

actor-steel_grun =
   .description = Anti-air turret.

actor-steel_inspect =
   .description = Armed with an ion cannon.

actor-latin_mili =
   .description = Basic rifle infantry.
       Can be upgraded with Molotovs.

actor-latin_fftr =
   .description = Attacks vehicles and aircraft with explosive arrows.

actor-latin_fthrow =
   .description = Flamethrower infantry.

actor-latin_monkey =
   .description = Primate armed with grenades.

actor-latin_narco =
   .description = Elite infantry armed with pistols and grenades.

actor-latin_yakovlev =
   .description = Attack aircraft armed with machine guns.

actor-latin_buggy =
   .description = Fast attack vehicle armed with machine guns.
       Can be upgraded with gatling guns

actor-latin_mortarbike =
   .description = Fast light artillery vehicle.

actor-latin_humvee =
   .description = Drive-by vehicle loaded with Narcos.

actor-latin_apc =
   .description = Amphibious infantry transport.
      Can attack air.

actor-latin_diablo =
   .description = Anti-aircraft tank.

actor-latin_burrito =
   .description = Incendiary heavy rocket artillery.

actor-latin_lars =
   .description = Long range anti air support.

actor-latin_topol =
   .description = ICBM launcher.

actor-latin_sml =
   .description = Long-range missile launcher.
      Rockets can be shot down

actor-latin_ca12hit =
   .description = Provides Topol Storm support power.
      Requires power to operate.

actor-nax_merc =
   .description = Sniper Infantry with long range.

actor-nax_mp40 =
   .description = Elite assault infantry.

actor-nax_litt =
   .description = Commando armed with a gun that creates black holes.
      Black holes damage nearby enemies and can be targeted

actor-nax_slavemaster =
   .description = Whips slaves to make them work faster.

actor-nax_atankcann =
   .description = Anti-tank artillery piece.
      Very slow to move and turn

actor-nax_alien =
   .description = Alien armed with an energy cannon.

actor-nax_hmg =
   .description = Machine gun crew.
      Needs to deploy to attack.

actor-nax_quadflak =
   .description = Flak cannon crew.
      Needs to deploy to attack.
      Can only attack air.

actor-nax_kubel =
   .description = Fast scout vehicle with 2 passengers.
      Can attack air.

actor-nax_bmwbike =
   .description = Very fast scout vehicle.
      Can attack air.

actor-nax_wirbelwind =
   .description = Anti-aircraft and anti-infantry tank.

actor-nax_grille =
   .description = Self-propelled medium-range howitzer.

actor-nax_brummbar =
   .description = Self-propelled long-range howitzer.

actor-nax_sturmtiger =
   .description = Heavily armored assault artillery.

actor-nax_hetzer =
   .description = Light tank destroyer.

actor-nax_jagdpanzer =
   .description = Heavy tank destroyer.

actor-nax_eng =
   .description = Captures buildings and repairs vehicles.
      Unarmed

actor-nax_brad =
   .description = Multiple rocket launcher.
      Rockets can be shot down

actor-nax_sarubia =
   .description = Main battle tank.
      Projectiles can be shot down

actor-nax_shoe =
   .description = Superheavy tank armed with cannons, mortars, and rockets.
      Has fireports for 2 passengers.

actor-nax_nokana =
   .description = Superheavy tank with a fireport for a vehicle.
      Armed with cannons and rockets

actor-nax_haunebu =
   .description = Multirole floating disc.

actor-nax_haunebu2 =
   .description = Heavy multirole floating disc.
      Drops a cow on targets.

actor-nax_me262 =
   .description = Multirole fighter armed with machineguns.

actor-nax_piercer =
   .description = Advanced multirole fighter.
      Projectiles splash and corrode targets,
      inflicting debuffs and damage over time.

actor-nax_dieglocke =
   .description = Superheavy spaceship.
      Creates massive toxin clouds.
      Only attacks targets below.

actor-nax_flak88 =
   .description = All-purpose defense.
      Can attack air.

actor-nax_airfield =
   .description = Produces aircraft.
      Automatically deploys fighters to attack enemies.

actor-nax_rocket =
   .description = Provides a V1 missile.
      Requires power to operate

actor-shock_infantry =
   .description = Armed with a shock gun.
       Damages enemies in a line.
       Strong vs Infantry, Vehicles

actor-rocket_raider =
   .description = Fast raider armed with rockets.
      Can carry 4 passengers.
       Strong vs Infantry, Light Vehicles, Air

actor-shock_raider =
   .description = Fast raider armed with a shock gun.
      Strong vs Tanks

actor-missile_tank =
   .description = Long-range rocket artillery.
       Strong vs Vehicles, Buildings

actor-siege_tank =
   .description = Long-range artillery gun.
       Strong vs Infantry, Buildings

actor-duelist_tank =
   .description = Heavy tank.
      Strong vs Vehicles

actor-quake_tank =
   .description = Deals seismic damage to vehicles and structures.

actor-carryall =
   .description = Automatically transports harvesters.

actor-air_drone =
   .description = Multirole light fighter armed with napalm rockets.
       Strong vs Infantry, Aircraft

actor-farasha =
   .description = Carrier spaceship.
      Armed with a laser.
      Carries 5 Alfayrus drones.
       Strong vs Everything

actor-ixprojector =
   .description =  Ultimate Ixian unit for causing chaos and distraction among enemy forces
      Jams enemy radars and disables enemy units and buildings with an EMP beam.
      Can make fake projections giving impression there is a larger army present.
      Projections disappears when too far away from Projector
      Special Abilities: Invisibility, Projections, Detector

actor-d2k_munitions =
   .description = Increases firing speed of nearby units.
      Effect can stack.
      Explodes when destroyed

actor-storm_lasher =
   .description = Long-range defensive structure.
      Deals massive splash damage.
       Strong vs Ground

actor-large_gun_turret =
   .description = Requires power to operate.
       Strong vs Infantry, Air
       Weak vs Tanks

actor-high_tech_factory =
   .description = Unlocks advanced technologies.
      Produces aircraft.

actor-oilb =
   .name = Oil Derrick
   .description = Provides passive income.
      Levels up over time.
      Build limit: 3

upgrade-a10airstrike =
   .description = Enables the A10 Airstrike Support Power
      Increases damage of all GDI Aircraft by 15%.

upgrade-longrangesensors =
   .description = Increases vision, stealth detection range
      and accuracy of all units and defenses by 25%.
      Increases range and damage by 5%.

upgrade-armorpiercingbullets =
   .description = Increases damage of all bullet based weapons by 33%.
      while also making them more effective against tank armor.
      Increases Minigunner and Shotgunner damage by 100%.
      Increases GDI Sniper and A10 damage by 10%.
      Also adds a machine gun to the Battle and Predator Tank and increases damage by 5%.

upgrade-heavyaircraftarmorplating =
   .description = Increases armor of Orcas, Firehawks, Chinooks and A10s by 50%.

upgrade-advancedmissiletargeting =
   .description = Increases missile damage, speed and Acceleration.
      Increases damange and range of all missile weapons by 15%.
      Tanks increase range by 5%
      Firehawks shoot twice the amount of missiles.

upgrade-cuttingedgeequipment =
   .description = Increases armor, damage and fire rate of all units and defenses by 10%.
      Doubles the effect for EMP Grenadiers, Experimental Exosuits, Firehawks and Havoc.

upgrade-highvelocitycannons =
   .description = Increases damage and range of cannons and railguns by 15%.
      Replaces tank cannons with high velocity cannons that deal 100% more damage
      and travel 100% faster.

upgrade-lightweightarmorplating =
   .description = TEAM UPGRADE
      Increases armor by 20% and speed by 10% of all tanks, scout and support vehicles in your team.

upgrade-guerillatactics =
   .description = Increases Firepower of all infantry, scout vehicles, support vehicles,
      tanks, helicopters, fire support and stealth units by 15%, Speed by 10% and Range by 5%.
      The effect is doubled for the Stealth Tank.

upgrade-tiberiuminfusion =
   .description = Gives infantry rapid regeneration and 10% more speed, fire rate and armor.

upgrade-improvedartilleries =
   .description = Increases Firepower and Range of all Artilleries by 25%.
      Increases Range of Gun Turrets and the SSM by 10%.

upgrade-elementalwarfare =
   .description = Increases damage of all flame and chemical weapons by 25%.
      Increases armor of elemental warfare units by 20%

upgrade-elitecapacitors =
   .description = Increases fire power and fire rate of all laser weapons by 30% and range and vision by 15%.
      Obelisks of Light increase fire power and fire rate by 60% and range and vision by 30%.

upgrade-cyberneticmodifications =
   .description = Gives all infantry heavy armor platings that reduce incoming damage.
      While the armor is active, the infantry armor type is heavy which makes them take less damage from
      most anti infantry weapons but increased damage from anti tank weapons instead.
      Increases Movement Speed by 20%.

upgrade-blackmarketupgrades =
   .description = Gives certain units new or additional weapons:
      Minigunner: Gets a Laser Rifle.
      Buggy Mk1 and Mk2: Adds a Flamethrower.
      Recon Bike: Adds a Point Defense Laser.
      Light Tank Mk1 and Mk2: Adds a Missile Launcher.
      Apache: Adds Missile Launchers.
      Artillery and Specter: Increases damage and spread of the warhead makes it more effective against vehicles.
      Gun Turret and Attack Submarine: Increases damage and spread of the warhead.

upgrade-advancedguerillatactics =
   .description = TEAM UPGRADE
      Increases Firepower of all infantry, scouts, support vehicles and fire supports in your team
      by 15%, Speed by 10% and Range by 5%
      For Nod it also doubles the effect for all units affected by the guerilla tactics upgrade.
      The effect is doubled once more for the Stealth Tank.

upgrade-conscription =
   .description = Decreases cost and training time for all infantry Flak Tracks, BTRs and Hip Transports by 25%.
   Unlocks the "Paratroopers" Support power from the Soviet Airfield.
   All Infantry and Troop Transports deal 10% more damage and have 10% more armor.

upgrade-nuclearengines =
   .description = Increases speed of tanks by 20%.
   Tank damage is also increased by 10%

upgrade-rocketenhancements =
   .description = Increases weapon and vision range and damage of all ballistic rockets:
   V1 Truck: 25%
   V2 Launcher: 20%
   Nuclear V2 Launcher, Mig and Su-57: 15%
   Missile Submarine, SAM Site and Hind: 10%
   Mammoth Tank, Monster Tank and Volkov: 5%

upgrade-advancedthermobarics =
   .description = Increases damage of all fire and nuclear weapons by 25%.
   Adds Napalm Warheads to the V1 Truck, Mig, Su-57 and all tanks.
   V2 Launcher and Nuclear V2 Launcher increase damage by 100%.
   Adds Incendiary Bullets to the Rifle Infantry, Yak, Hind and Gatling Tank.
   Increases Armor of Grenadiers, Flamethrowers, Fire Rocket Soldiers and Mortar Soldiers by 50%

upgrade-experimentalteslaweaponry =
   .description = Increases damage of tesla weapons by 50%
   and adds an EMP effect that will stun vehicles and buildings after several hits.
   Unlocks the "Parabombs" Support power from the Soviet Airfield.

upgrade-sovietautoloaders =
   .description = Reduces the Reload Delay of Tanks, Hinds and Volkov by 40%.

upgrade-sovietsteel =
   .description = TEAM UPGRADE
   Increases armor of all tanks in your team by 40% but makes them 10% slower.
   For Soviets it will also increase the armor of most other vehicles, submarines, helicopters and defenses.
   Dogs are turned into cybernetic monstrosities that are much more durable.

upgrade-advancedradarsystems =
   .description = Increases Radar Dome detection and vision range by 50%.
   Increases damage, fire rate, accuracy, vision, detection and weapon range of all units and defenses by 10%.

upgrade-infantryarmorplating =
   .description = Increases armor of all infantry units by 30%.
   Also reduces Reload Delay by 10%.

upgrade-reinforcedstructures =
   .description = Increases armor of all buildings and defenses by 25%.
   Also reduces Cost and Build Time by 25%.

upgrade-cryomissiles =
   .description = Adds cryo warheads to all missiles and bombs that freezes enemy units and buildings.
   Reduces Firepower by 25%
   Each hit slows down the enemy target by 10%, increases damage taken by 10% and stacks 10 times.
   Each stack lasts 5 seconds and on 10 stacks the unit is completely disabled.
   Artillery, Bastion and Rapier Jumpjet get cryo bombs with double the cryo effect radius of effect.

upgrade-lasertargetingsystems =
   .description = Increases damage of all units and defenses by 25%

upgrade-airsuperioritydoctrine =
   .description = Increases damage, rate of fire, armor, speed, range and accuracy of aircraft and the Heavy AA Tank by 15%.

upgrade-chronoarmor =
   .description = After taking damage, tanks will activate a chrono field
   which increases armor and fire rate by 35% and speed by 50%
   Unlocks Chrono Reinforcements from the Chronosphere.

upgrade-gpssatellitesupport =
   .description = TEAM UPGRADE
   Launches a GPS Satellite that instantly reveals all enemy units and buildings on the map.
   Increases accuracy, vision, detection and weapon range of all units and defenses by 10%.

upgrade-bushidodiscipline =
   .description = Increases damage, rate of fire, armor, speed, range and accuracy by 10% and gives self healing.
   When wounded the effect is tripled.

upgrade-waveforcebullets =
   .description = Increases range and damage of all bullets by 10% and makes them more effective against armor.

upgrade-divinewindprotocol =
   .description = Increases damage, rate of fire, armor and speed, of all air units by 10%

upgrade-stealthsuitintegration =
   .description = All Infantry come equipped with a stealth suit, that makes them invisible when not attacking or moving.
   Increases speed and armor by 10% and gives a personal shield generator.

upgrade-energizedarrows =
   .description = Increases damage of arrows by 25%.
   Arrows are upgraded with an electric charge that deals bonus damamge to infantry and disables vehicles and structures.

upgrade-superiorwarengines =
   .description = Increases speed, fire rate and armor of all vehicles by 20%
   The effect is halfed for Ballistas, Waveforce Tanks and Waveforce artilleries.

upgrade-advancedplasmaweapons =
   .description = Increases the damage, fire rate and range of all plasma weapons by 10%.

upgrade-nanotechrepairs =
   .description = TEAM UPGRADE
   Increases armor of all vehicles in your team by 10% and makes them regnerate faster.
   For Japan the effect is also applied to all aircrafts and buildings.
   Japanese Fire Support Vehicles and Tanks also get a nanodrone shield
   for 0.2 seconds after taking damage that increases armor by 20%.

upgrade-wind_trap =
   .description = Increases power output of Wind Traps by 100%.
   Additionally, Wind Traps have their cost and build time reduced by 50%.

upgrade-d2k_upgrade_needleguns =
   .description = Increases damage of machine guns by 25% and range by 10%.
   Tungsten Needles are more effective against vehicles and buildings and can hit air units.

upgrade-d2k_general_purpose_armor =
   .description = General Purpose Armor.
   Increases armor of all infantry, vehicles and aircraft by 20%.

upgrade-personal_shield =
   .description = Gives personal shield generators to all infantry units.

upgrade-d2k_siege_range_upgrade =
   .description = All Tanks, Gun Turrets and Artillery gain increased range and damage.
   Gun Turret, Ix Combat Tanks and Duelist Tanks: 10% higher range and 25% more damage.
   Ix Siege Tank: 25% higher range and 50% more damage.
   Ix Combat Siege: 50% higher range and 100% more damage.

upgrade-d2k_heavy_missile_upgrade =
   .description = All missile based weapons deal 20% more damage and have 20% more range.

upgrade-spice_sifter =
   .description = Decreases amount of Spice filtered
      out incorrectly from Spice Sifters and Refineries.
      Increases Spice Sifter and Spice Refinery income by 25%.

upgrade-d2k_advanced_ixian_technology =
   .description = TEAM UPGRADE
      Gives Stealth Generators to all Harvesters in your team and increases artillery damage by 25% and range by 10%.
      For Ixians:
      Gives shields to all vehicles and aircraft and doubles shock and storm weapon damage.
      Stealth Generators are also given to Ix Missile Tanks and Ix Siege Tanks.
      Infantry with Personal Shield Generators also have 50% more shield armor.

upgrade-uplatin_mili =
   .description =  Equips Militias with Molotovs.
      Increases Firepower and Damage Resistance of all Infantry by 25% and gives Self Healing.
      Doubles the effect for the Militia and gives Molotovs.

upgrade-uplatin_cartelrockets =
   .description =  Upgrades all rockets with Black Market Cartel Rockets.
      Increases Firepower and Range of all Rocket Artilleries by 25%.
      Gives Rusher Tanks, Smoker Tanks and APCs additional Rocket Launchers.

upgrade-uplatin_ngbunk2 =
   .description = Increases garrisoned buildings range, vision and armor by 30%.

upgrade-latin_up_chaingun =
   .description = Stolen Tech from Yuri:
      Equips different units and defenses with Chainguns.
      Increases Rate of Fire with each shot!
      Diablos gain 25% more Range, 25% faster Reload Delay and 50% more Firepower.

upgrade-latin_up_industrial =
   .description = Stolen Tech from Soviets:
      Reduces Cost of Vehicles by 20%.
      Increases Damage Resistance of all Vehicles by 20%.

upgrade-uplatin_alliedstolentech =
   .description = Stolen Tech from Allies:
      Increases Speed of all Vehicles by 25% and Range by 5%.
      Freedom Fighters and Diablos disguise as trees.

upgrade-latin_up_hotfire =
   .description =  Stolen Tech from Asian Alliance:
      Increases Firepower and Rate of Fire of all Fire Weapons by 25%.
      Terrorists, Demo Trucks, Flame Troopers and Mortar Bikes have their Firepower increased by 100% instead.
      Flame Troopers and Mortar Bikes also gain 10% more Range and 20% more Speed.

upgrade-uplatin_cashrecover =
   .description = Recycles lost vehicles for a cashback of 20%.

asian_upgrade_infantryspec =
   .description = Equips Infantry with new or additional weapons.
      All Infantry have 5% more Damage Resistance, Fire Power and Range.

asian_upgrade_pulverizer =
   .description = Doubles the amount of bullets fired at 75% damage per shot.
      Increases Range by 15%.

asian_upgrade_phalanx =
   .description = Increases Damage Resistance of Asian Alliance Units by 10%.
      Increases Damage Resistance of Asian Alliance Units next to Lynx Tanks by 5% but slows them also down by 5%.
      Stacks up to 5 times.
      Lynx Tanks and Pelicans get an additional Machine Gun.

asian_upgrade_dragonfire =
   .description = Increases Flame and Plasma Weapon Damage by 50%.

asian_upgrade_celestialpower =
   .description = Increases Firepower and Range of Quasar Weapons and Railguns by 25%.
      Increases EMP Effect of Quasar Weapons by 100%

asian_upgrade_banzai =
   .description = Infantry enter Banzai Mode on low health:
      Stacks 2 Times for Heavy and Critical Damage.
      Reduces Range by 25% but incrases Speed by 25%.
      Increases Damage Resistance and Firepower and Rate of Fire by 25%.
      Increases Health Regeneration.

asian_team_upgrade_dragonway =
   .description = Increases Speed, Firepower and Damage Resistance of all Units in your team by 5%.
      Doubles the effect for 1 second after taking damage.

asian_team_upgrade_diplomacy =
   .description = Through clever diplomacy all the units in your team are 10% cheaper.

upgrade-infweapon =
   .description = Increases firepower of all Terran infantry by 25%

upgrade-infarmor =
   .description = Increases armor of all Terran infantry by 20%

upgrade-mechweapon =
   .description = Increases firepower of all Terran vehicles by 25%

upgrade-mecharmor =
   .description = Increases armor of all Terran vehicles by 20%

upgrade-shipweapon =
   .description = Increases firepower of all Terran aircraft by 25%

upgrade-shiparmor =
   .description = Increases armor of all Terran aircraft by 20%

ra_doctrine_conscription =
   .description = DOCTRINE (Only One Doctrine For Each Tier Can Be Researched)
      Focuses on Mass Produced Infantry:
      All Infantry are 25% cheaper and faster to train
      All Infantry have 10% more Firepower, Speed and Damage Resistance
      Replaces Rifle Soldiers and Grenadiers with AK47 and Molotov Conscripts
      Replaces Rocket Soldiers with Dragunov Anti Material Snipers
      Replaces Flak Trucks with BTR-80s
      Unlocks Commissar
      Unlocks Vengeance and Men of Steel Upgrades

ra_doctrine_industrialefficiency =
   .description = DOCTRINE (Only One Doctrine For Each Tier Can Be Researched)
      Focuses on strong Economy and Mass Produced Vehicles and Aircraft
      Replaces Ore Trucks with Industrial Miners
      Replaces War Factories with Large Ore Factories (100% faster production)
      Replaces Airfields with Large Airfields (100% faster production)
      Unlocks Mass Production and War Economy Upgrades

ra_doctrine_inferno =
   .description = DOCTRINE (Only One Doctrine For Each Tier Can Be Researched)
      Focuses on Flame and Artillery Weapons
      All Grenadiers, Mortars, Flame Weapons and Artilleries have 25% more firepower
      and take 20% less damage
      Replaces Rocket Soldiers with Fire Rocket Soldiers
      Unlocks Heatray Tank
      Unlocks Incendiary Bullets and Scorched Earth Upgrades

ra_doctrine_teslatech =
   .description = DOCTRINE (Only One Doctrine For Each Tier Can Be Researched)
      Focuses on Experimental Tesla Technology
      All Tesla Weapons deal additional EMP Damage.
      Replaces Shock Troopers with Zappers
      Replaces Hind with Kamov
      Unlocks Heavy Tesla Tank
      Unlocks Tesla Arcing, Tesla Rockets and Reactor Overload Upgrades

ra_doctrine_heavyarmor =
   .description = DOCTRINE (Only One Doctrine For Each Tier Can Be Researched)
      Focuses on Heavy Armor and Powerful Tanks
      All Vehicles and Aircraft have 10% additional Damage Resistance
      Replaces Mammoth Tanks with Siege Mammoth Tanks
      Replaces V1 Rocket Trucks with Grads
      Replaces Replaces Migs with Su-57s
      Unlocks Shtora Defense System Upgrade
      Unlocks Auto Loaders, High Explosive Rockets and Stalinium Upgrades

ra_doctrine_nuclearwar =
   .description = DOCTRINE (Only One Doctrine For Each Tier Can Be Researched)
      Focuses on High Damage and Speed.
      All Vehicles have 10% higher Firepower and Speed
      Replaces V2 Rocket Launcher with Nuclear V2 Rocket Launcher
      Unlocks Kotin Nuclear Tank
      Unlocks Unstable Isotopes, Thermonuclear Rockets and Nuclear Tank Shells Upgrades

ra_upgrade_hazmatsuits =
   .description = Infantry Armor Upgrade
      Increases Infantry Damage Resistance by Tiberium, Radiation,
      High Explosives, Fire, Chemicals and Nuclear Weapons by 100%.

ra_upgrade_afterburners =
   .description = Increases damage, range and speed of Migs, Su-57s and Nuclear Bombers by 15%.
      Unlocks the "Paratroopers" Support Power from the Soviet Airfield.
      Upgrades the "Parabombs" Support Power from the Soviet Airfield.
      Jets also take 15% less damage.

ra_upgrade_afterburners =
   .description = Increases damage, range and speed of Migs, Su-57s and Nuclear Bombers by 15%.
      Unlocks the "Paratroopers" Support Power from the Soviet Airfield.
      Upgrades the "Parabombs" Support Power from the Soviet Airfield.
      Jets also take 15% less damage.

ra_upgrade_vengeance =
   .description = Tech Upgrade (Only affects units of own faction)
      Doubles moral boost effect to nearby units by fallen Conscripts and Commissars.
      Commissars gives this double effect now permanently to nearby units.

ra_upgrade_menofsteel =
   .description = Team Upgrade (Also affects units of your teammates)
      Increases Infantry Firepower and Damage Resistance by 10%

ra_upgrade_massproduction =
   .description = Tech Upgrade (Only affects units of own faction)
      Reduces Cost of all Vehicles and Aircraft by 20%

ra_upgrade_wareconomy =
   .description = Team Upgrade (Also affects units of your teammates)
      Reduces Production Cost and Time of Refineries and Harvesters by 10%.
      Increases Income from Refineries by 10%.
      Increases Speed of Harvesters by 10%.

ra_upgrade_incendiarybullets =
   .description = Tech Upgrade (Only affects units of own faction)
      Adds an additional fire warhead to all bullet based weapons.
      Increases Firepower by 100% and gives higher area of effect.

ra_upgrade_scorchedearth =
   .description = Team Upgrade (Also affects units of your teammates)
      Increases Firepower of all Grenadiers, Mortars, Flame Weapons and Artilleries by 25%.
      Equips the V1 Rocket Truck and the Grad with Fire Rockets.

ra_upgrade_teslaarcing =
   .description = Tech Upgrade (Only affects units of own faction)
      Adds arcing effects to all Tesla Weapons.
      Can arc up to 2 extra times for extra damage.
      Reduces recharge delay for the Heavy Tesla Tank by 25%
      Equips Volkov with arcing Tesla Bombs.

ra_upgrade_teslarockets =
   .description = Tech Upgrade (Only affects units of own faction)
      Heavy Rockets such as of the Mammoth Tank, Kamov, Mig and V2 Launcher
      are replaced with Tesla Rockets that can arc over to other targets on explosion.

ra_upgrade_reactoroverload =
   .description = Team Upgrade (Also affects units of your teammates)
      Doubles the Power Output of all Power Plants in your team.
      Also doubles Power Consumption and Firepower of Soviet Tesla Coils.

ra_upgrade_autoloaders =
   .description = Tech Upgrade (Only affects units of own faction)
      Reduces Reload Delay of all Tanks by 40%.

ra_upgrade_highexplosiverockets =
   .description = Tech Upgrade (Only affects units of own faction)
      Heavy Rockets such as of the Siege Mammoth Tank, Hind, Su-57, Grad and V2 Launcher
      are replaced with High Explosive Rockets that deal 20% more damage and have higher area of effect.

ra_upgrade_stalinium =
   .description = Team Upgrade (Also affects units of your teammates)
      Reduces Incoming Damage of all Tanks in your team by 20%.

ra_upgrade_unstableisotopes =
   .description = Tech Upgrade (Only affects units of own faction)
      Increases Speed of all Vehicles by 20%.
      Increases Speed of Kotin Nuclear Tanks by 40%.

ra_upgrade_nuclearrockets =
   .description = Tech Upgrade (Only affects units of own faction)
      Heavy Rockets such as of the Mammoth Tank, Hind, Mig and Nuclear V2 Launcher
      are replaced with Thermonuclear Rockets that deal increased damage to heavier armor types
      and have massively higher area of effect.

ra_upgrade_nuclearshells =
   .description = Team Upgrade (Also affects units of your teammates)
      Increases Firepower of all Tanks in your team by 20%.
      Equips Soviet Tanks with special shells that explode in a higher radius.
      Equips Volkov with a Nuclear Cannon.
      Increases Spread and Radiation Damage of the Kotin Nuclear Tank.

ra_promotion_superoptics =
   .description = Promotion Upgrade (Only affects units of own faction)
      Increases Infantry Vision and Stealth Detection by 20%.
      Increases Infantry Weapon Range by 10%.

ra_promotion_targetingcomputer =
   .description = Promotion Upgrade (Only affects units of own faction)
      Allows Mammoth Tanks to use both Cannons and Missiles for all Ground and Water Targets.

ra_promotion_hurricanerocketpod =
   .description = Promotion Upgrade (Only affects units of own faction)
      Doubles the amount of Missiles that Hind and Kamov can fire.

ordos_upgrade_shield =
   .description = Tech Upgrade (Only affects units of own faction)
      Equips all vehicles and aircraft with shields.

ordos_upgrade_toxin =
   .description = Tech Upgrade (Only affects units of own faction)
      Infuses Infantry with powerful chemicals that permanently boost combat effectiveness.
      Increases Rate Of Fire, Speed and Damage Resistance by 10%.
      Chemical Weapons deal an additional 50% more damage.

ordos_upgrade_contraband =
   .description = Tech Upgrade (Only affects units of own faction)
      Upgrades any non biological weapons with new contraband versions.
      Increases Firepower by 25%.

upgrade-seretraining =
   .name = SERE Training
   .description = Tech Upgrade (Only affects units of own faction)
      Increases Damage Resistance of infantry by 25%
      Increases Speed by 20%.

upgrade-sonicweaponry =
   .name = Sonic Weaponry
   .description = Tech Upgrade (Only affects units of own faction)
      Greately increases Disruptor damage
      Equips Orcas, Vulcan Towers, Disc Throwers and the Kodiak with Sonic weapons.

upgrade-tsprojectileimprovements =
   .name = Projectile Improvements
   .description = Tech Upgrade (Only affects units of own faction)
      Increases Damage and Range of Enforcers, Pitbulls, Hover MLRS and SAM Towers by 20%

upgrade-mechengineering =
   .name = Mech Engineering
   .description = Tech Upgrade (Only affects units of own faction)
      Increases Damage Resistance of walkers by 20%.
      Increases Speed by 25%.

upgrade-railgunweaponry =
   .name = Railgun Weaponry
   .description = Tech Upgrade (Only affects units of own faction)
      Increases damage for Railgun Commando and Mammoth Prototype.
      Equips Enforcer, Titan and RPG Tower with Railgun weapons.

upgrade-mechanicalreliability =
   .name = Mechanical Reliability
   .description = Tech Upgrade (Only affects units of own faction)
      Increases Damage Resistance and Speed of all vehicles, tanks and aircraft by 10%.
      Also gives them additional health regeneration of 1% per second.

upgrade-ceramicarmor =
   .name = Ceramic Armor
   .description = Tech Upgrade (Only affects units of own faction)
      Increases Damage Resistance of aircraft by 25%.
      Increases Speed by 10%.

upgrade-modernfirecontrolsystems =
   .description = Team Upgrade (Also affects units of your teammates)
      Increases Accuracy and Rate Of Fire of all vehicles in your team by 10%
      Increases Vision, Detection and Weapon Range by 5%.

template-mcv =
   .description = Deploys into another Construction Yard.
      Unarmed
   .name = Mobile Construction Vehicle

template-harvester =
   .description = Collects resources for processing.
      Unarmed
   .tdname = Harvester
   .raname = Ore Truck

template-refinery =
   .description = Processes resources into cash.
   .tdname = Tiberium Refinery
   .raname = Ore Refinery
   .ra-japan = Japanese Ore Refinery
   .ra2-allies = Allied Ore Refinery
   .ra2-soviet = Soviet Ore Refinery

template-silo =
   .description = Stores excess resources.
   .tdname = Tiberium Silo
   .raname = Silo

template-power =
   .description = Generates power.

template-advpower =
   .description = Generates more power compared to the standard power generator.

template-barracks =
   .description = Trains infantry.
   .td-gdi = GDI Barracks
   .td-nod = Hand of Nod
   .ra-soviet = Soviet Barracks
   .ra-allies = Allied Barracks
   .ra-japan = Japanese Barracks
   .ra2-soviet = Soviet Barracks
   .ra2-allies = Allied Barracks
   .ra2-yuri = Yuri Barracks

template-factory =
   .description = Produces vehicles.
   .td-gdi = Weapons Factory
   .ra-allies = Allied War Factory
   .ra-soviet = Soviet War Factory
   .ra-japan = Japanese War Factory
   .ra2-allies = Allied War Factory
   .ra2-soviet = Soviet War Factory
   .ra2-yuri = Yuri War Factory

template-radar =
   .description = Provides radar and advanced technologies.
      Requires power to operate.
   .td-nod = Nod Communications Center
   .ra-allies = Allied Radar Dome
   .ra-soviet = Soviet Radar Dome
   .ra-japan = Japanese Radar Array
   .ra2-soviet = Soviet Radar

template-service-depot =
   .description = Repairs vehicles.
   .td-gdi = GDI Repair Facility
   .td-nod = Nod Repair Facility
   .ra-allies = Allied Service Depot
   .ra-soviet = Soviet Service Depot
   .ra-japan = Japanese Service Depot
   .ra2-allies = Allied Service Depot
   .ra2-soviet = Soviet Service Depot

template-airfield =
   .description = Produces aircraft.
   .td-gdi = GDI Helipad
   .td-nod = Nod Helipad
   .ra-allies = Allied Helipad

template-shipyard =
   .description = Produces and repairs ships.
   .td-gdi = GDI Naval Yard
   .td-nod = Nod Sub Pen
   .ra-soviet = Soviet Sub Pen
   .ra-allies = Allied Naval Yard
   .ra-japan = Japanese Naval Yard
   .ra2-allies = Allied Naval Yard
   .ra2-soviet = Soviet Shipyard
   .ra2-yuri = Yuri Sub Pen

template-tech-center =
   .description = Provides advanced technologies.
   .ra-allies = Allied Tech Center
   .ra-soviet = Soviet Tech Center
   .ra-japan = Japanese Tech Center
   .ra2-allies = Allied Battle Lab
   .ra2-soviet = Soviet Battle Lab
   .ra2-yuri = Yuri Battle Lab

template-anti-infantry-defense =
   .description = Anti-infantry defense.
      Strong vs Infantry
      Weak vs Tanks

template-anti-vehicle-defense =
   .description = Anti-vehicle defense.

template-anti-aircraft-defense =
   .description = Anti-aircraft defense.

template-anti-aircraft-power-defense =
   .description = Anti-aircraft defense.
      Requires power to operate.

template-scout-infantry =
   .description = General-purpose infantry.
      Strong vs Infantry
      Weak vs Vehicles, Aircraft

template-antitank-antiair-infantry =
   .description = Anti-vehicle infantry.
      Strong vs Vehicles, Aircraft
      Weak vs Infantry

template-antitank-infantry =
   .description = Anti-vehicle infantry.
      Strong vs Vehicles
      Weak vs Infantry, Aircraft

template-mbt =
   .description = Main battle tank.
      Strong vs Vehicles
      Weak vs Infantry, Aircraft

template-commando =
   .description = Elite commando infantry.
      Can demolish buildings.
      Strong vs Infantry, Buildings
      Weak vs Vehicles, Aircraft

template-opentopped =
   .description = Transport with fireports for passengers.
      Comes already loaded with infantry inside.

template-unarmed-transport =
   .description = Unarmed troop transport.

promotions =
   .lobby-label = Promotion Points per Rank
   .lobby-description = Promotion points earned for each rank through destroying enemies can be used to purchase upgrades and new technologies
   .text-notification = You have been promoted!
   .rank1 = Level 1
   .rank2 = Level 2
   .rank3 = Level 3
   .rank4 = Level 4
   .rank5 = Level 5
   .rank6 = Level 6
   .rank7 = Level 7
   .rank8 = Level 8

promotions-flavor =
   .prefix = General Staff
   .notification-01 = You have been promoted. Review the Promotions tab for access to new technologies.
   .notification-02 = Congratulations on your field promotion! Purchase a Promotional upgrade to bolster your forces.
   .notification-03 = Your successes have granted you access to an exclusive field technology or ability of your choice.
   .notification-04 = You've been authorized to purchase a Promotional upgrade. Fight on to earn more points and expand your arsenal.
   .notification-05 = A Promotional upgrade is now available to you. Unlock new units and powers or augment your existing ones.

faction-random =
   .name = Any
   .description = Random faction
      A random faction from any game will be chosen when the game starts.

faction-td-random =
   .name = Any
   .description = Random faction
      A random faction from Tiberian Dawn will be chosen when the game starts.

faction-td-gdi =
   .name = GDI TD
   .description = GDI from Tiberian Dawn
      Support powers: Airstrike, Ion Cannon

faction-td-nod =
   .name = Nod TD
   .description = Nod from Tiberian Dawn
      Support powers: Nuclear Strike

faction-ra-random =
   .name = Any
   .description = Random faction
      A random faction from Red Alert will be chosen when the game starts.

faction-ra-allies =
   .name = Allies RA
   .description = Allies from Red Alert
      Support powers: GPS, Chronosphere, Chrono Vortex

faction-ra-soviets =
   .name = Soviets RA
   .description = Soviets from Red Alert
      Support powers: Parabombs, Iron Curtain, Atomic Bomb

faction-ra-japan =
   .name = Japan RA
   .description = Japan custom faction in Red Alert 1 style.
      Support powers: Super Bomber Airstrike, Magic Orb Hailstorm

faction-ts-random =
   .name = Any
   .description = Random faction
      A random faction from Tiberian Sun will be chosen when the game starts.

faction-ts-gdi =
   .name = GDI TS
   .description = GDI from Tiberian Sun
      Support powers: Drop Pods, Ion Cannon

faction-ts-nod =
   .name = Nod TS
   .description = Nod from Tiberian Sun
      Support powers: Chemical Missile

faction-ts-forgotten =
   .name = Forgotten
   .description = Forgotten from Tiberian Sun
      Support powers: Tiberian Wildlife Rampage

faction-ts-cabal =
   .name = CABAL
   .description = CABAL from Tiberian Sun
      Support powers:

faction-ra2-random =
   .name = Any
   .description = Random faction
      A random faction from Red Alert 2 will be chosen when the game starts.

faction-ra2-allies =
   .name = Allies RA2
   .description = Allies from Red Alert 2
      Support powers: Chronosphere, Lightning Storm

faction-ra2-soviets =
   .name = Soviets RA2
   .description = Soviets from Red Alert 2
      Support powers: Iron Curtain, Nuclear Missile

faction-ra2-yuri =
   .name = Yuri
   .description = Yuri from Yuri's Revenge
      Support powers: Genetic Mutator, Psychic Dominator

faction-ra2-modded-random =
   .name = Any
   .description = Random faction
      A random faction from Red Alert 2 Mods will be chosen when the game starts.

faction-ra2-asianalliance =
   .name = Asian Alliance
   .description = Asian Alliance from Eagle Red Mod
      Asian Alliance is a massive high-tech coalition of China, Japan, and Korea.
      Acting as a unified East Asian front that has decided to pursue its own
      technological path, independent of the Allied and Soviet blocs. They focus on
      pulverizer, fire and high-energy plasma weaponry and Mass Infantry Deployment

      Support powers: Chaos Storm, Ion Cannon

faction-ra2-syndicate =
   .name = Latin Syndicate
   .description = Latin American Crime Syndicate with old Soviet Technology
      The Latin Syndicate is a ruthless, criminal-industrial war machine that weaponizes attrition,
      area denial, and the enemy's own technology. Operating from fortified bunker networks,
      they combine "black market" Soviet surplus with ingenious scrap-based engineering to overwhelm
      opponents through constant pressure and tactical disruption.

      Support powers: Topol Strike

faction-ra2-consortium =
   .name = Steel Consortium
   .description = Steel Consortium from Reign of Steel Mod
      Support powers: Ion Cannon

faction-ra2-naxis =
   .name = Naxis
   .description = Naxis custom faction in Red Alert 2 style.
      A satirical fusion of 1940s aesthetics and over-the-top "secret weapon" tropes,
      Naxis serves as a parody of World War II-era Germany, incorporating anachronistic
      units from across the WWII gaming genre. They trade elegance for raw, clanking
      industrial power and occult experimentation.

      Difficulty: ©©©©
      Early Game: ©©©©©
      Mid Game: ©©
      Late Game: ©©©©
      Playstyle: Turtle
      Strength: Bunkers, Heavy Tanks and Artillery
      Weakness: Aircraft
      Countered by: Heavy Armor, Map Control
      Special Units: Ratte, Nokana
      Special Buildings: Sausage Factory, Beer Factory
      Team Upgrades: Blitzkrieg
      Support powers: Revive the Undead Warriors, V1 Rocket

faction-ra2-lnaxis =
   .name = Schwarzer Mond
   .description = Schwarzer Mond custom faction in Red Alert 2 style.
      The Schwarzer Mond (Black Moon) is the elite, space-faring branch of the Naxis,
      operating from secret bases on the lunar surface. They have abandoned the
      traditional warfare of Earth for "crazy science" and the manipulation of
      planetary forces, making them one of the most unpredictable factions in the field.

      Difficulty: ©©©
      Early Game: ©©
      Mid Game: ©©©©©
      Late Game: ©©©©
      Playstyle: Timing Attack
      Strength: Mid to Lategame Tanks and Artillery
      Weakness: Aircraft
      Countered by: Early Game Rush, Aircraft
      Special Units: Parzival, Dalek, Die Glocke
      Special Buildings: Moon Dairy Farm
      Team Upgrades: WIP
      Support powers: Gravity Core, Meteor Traction Beam

faction-ra2-futuretech =
   .name = FutureTech
   .description = FutureTech: High-End Robotics and Experimental Weaponry

      The secretive European defense conglomerate FutureTech pushes the boundaries of
      ethical science to secure Allied dominance. Operating from high-security labs in
      the Netherlands, they field an arsenal of cutting-edge robotics and experimental
      energy weapons that excel at overwhelming the enemy at a steep cost.

      Heavy investment in infrastructure is required before FutureTech's most advanced
      units can be deployed. Their technology is among the most powerful on the battlefield,
      but their base defenses are sparse, offering little protection without a standing army.

      Difficulty: ©©©©©
      Early Game: ©©
      Mid Game: ©©©
      Late Game: ©©©©©
      Playstyle: Tech Rush
      Strength: Late Game Offense, Robotic Units
      Weakness: Early Game, Base Defense
      Countered by: Early Aggression, Air Raids
      Special Units: Future Tank, Harbinger Gunship, Cryocopter
      Special Buildings: Hypercore, Robot Control Center
      Support powers: Paradrop (Planned: Sigma Harmonizer, Robot Energize)

faction-d2k-random =
   .name = Any
   .description = Random faction
      A random faction from the Dune Universe will be chosen when the game starts.

faction-d2k-ixian =
   .name = Ixians
   .description = Ixians from the Dune Universe
      The enigmatic Ixians of the industrial world Ix
      are known for their innovation, cold logic, and mastery
      of the machine. Bending the rules of the Great Convention,
      they often deploy experimental weaponry and mechanical
      monstrosities that others fear to touch.

      Difficulty: ©©©©
      Early Game: ©©©
      Mid Game: ©©©©
      Late Game: ©©©©©
      Playstyle: Turtle
      Strength: Late Game Units
      Weakness: Mobility
      Countered by: Early Game Pressure
      Special Units: Ixian Projector, Farasha
      Special Buildings: Starport, Spice Sifter
      Team Upgrades: Advanced Ixian Technology
      Support powers: Pulse Missile

faction-d2k-ordos =
   .name = House Ordos
   .description = House Ordos from the Dune Universe
      The insidious Ordos of the icy planet Sigma Draconis IV
      are known for their wealth, greed and treachery.
      Relying heavily on mercenaries they often resort
      to sabotage and forbidden Ixian technologies.
      Their Strength lies in Stealth and quick hit and run tactics

      Difficulty: ©©©
      Early Game: ©©
      Mid Game: ©©©
      Late Game: ©©©©©
      Playstyle: Mobility / Steamrolling
      Strength: Late Game Units
      Weakness: Early Game Tanks
      Countered by: Artillery
      Special Units: Wraith, Face Dancer
      Special Buildings: Starport, Spice Sifter
      Team Upgrades: WIP
      Support powers: Saboteur, Chaos Lightning
