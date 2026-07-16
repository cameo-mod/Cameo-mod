faction_outpost2_plymouth =
   .description = Plymouth Colony from Outpost 2
      Plymouth is a splinter colony on the harsh planet of NewTerra, founded
      by colonists who broke away from Eden over ideological differences.
      Plymouth favors adaptability and survival through ingenuity, fielding
      unique weapons like StickyFoam, ESG (Electro-Static Grapple), and
      Microwave emitters. Their military doctrine emphasizes area denial,
      crowd control, and attrition warfare. Plymouth also fields Spider
      vehicles — small, fast, autonomous units that can overwhelm enemies
      through sheer numbers.

      In Cameo, Plymouth is a colony-building faction that must construct
      a functioning colony (Residence, Agridome, Lab, Factory) to operate
      effectively. Their vehicle roster uses modular chassis (Lynx, Panther,
      Tiger) with swappable weapon turrets. The Supernova Missile is their
      signature superweapon — a tactical nuclear device that deals heavy
      damage over a large area. Plymouth's unique weapons include the
      StickyFoam projectile, ESG, and Microwave, offering distinct tactical
      options compared to Eden's Laser and Rail Gun arsenal.

      Difficulty: ©©©©
      Early Game: ©©©
      Mid Game: ©©©©
      Late Game: ©©©©
      Playstyle: Colony Builder / Area Denial / Attrition
      Strength: Spider Swarms, Area Denial Weapons, Supernova Missile
      Weakness: Colony Dependency, Resource Intensive, Slow Start
      Countered by: Early Rushes, Economic Harassment, Air Power
      Special Units: Spider, Plymouth Lynx (Supernova), Plymouth Tiger (Supernova),
        StickyFoam Lynx, ESG Tiger, Microwave Lynx
      Special Buildings: Tokamak, Residence, Agridome, Vehicle Factory,
        Garage, Laboratory
      Team Upgrades: Weapon tech research tree (Microwave, StickyFoam, ESG, EMP)
      Support powers: Supernova Missile
      Superweapons: Supernova Missile

      Features:
      - Splinter Colony from NewTerra
      - High tech faction with colony-building mechanics
      - Must build a colony (Residence, Agridome, Lab, Factory) to operate
      - Has fast Lynx vehicles and strong Tiger vehicles
      - Spider: unique small autonomous unit that swarms enemies
      - StickyFoam: slows and traps enemies in expanding foam
      - ESG (Electro-Static Grapple): disables vehicles with electric discharge
      - Microwave: directed energy weapon that induces electrical damage
      - EMP Missile: disables all vehicles in target area
      - Tokamak: power plant that releases EMP shockwave when destroyed
      - Supernova Missile: tactical nuclear superweapon
      - Modular vehicle chassis: Lynx (fast), Panther (balanced), Tiger (heavy)
      - Colony morale system affects productivity
      - ConVecs deploy structure kits, Earthworkers build tubes and walls

faction_outpost2_eden =
   .description = Eden Colony from Outpost 2
      Eden is the primary colony on NewTerra, founded by the original colonists
      who value scientific progress and technological supremacy. Eden's military
      doctrine emphasizes precision and firepower, with unique weapons including
      the Laser (pulsed chemical laser), Rail Gun (kinetic projectile), Acid Cloud
      (corrosive area denial), and Starflare (shaped-charge suicide turret). Eden
      also fields the GeoCon, Repair Vehicle, and Robo-Surveyor — unique support
      units that Plymouth lacks. Their Tokamak power plants are more efficient,
      and their research tree is geared toward offensive technology.

      In Cameo, Eden is a colony-building faction like Plymouth, but with a
      more aggressive tech tree. Their Laser and Rail Gun weapons offer superior
      direct-fire firepower, while Acid Cloud provides area denial. The Supernova
      Missile is their signature superweapon. Eden's unique units include the
      GeoCon (transforms into Geothermal Plant), Repair Vehicle (field repairs),
      and the Supernova Lynx and Tiger variants. Their playstyle rewards tech
      rushing and aggressive expansion.

      Difficulty: ©©©©
      Early Game: ©©©
      Mid Game: ©©©©©
      Late Game: ©©©©
      Playstyle: Colony Builder / Tech Rush / Precision Strike
      Strength: Laser Weapons, Rail Gun, Acid Cloud, Repair Vehicles
      Weakness: Colony Dependency, Resource Intensive, Slow Start
      Countered by: Early Rushes, Economic Harassment, Spider Swarms
      Special Units: Eden Lynx (Supernova), Eden Tiger (Supernova),
        Laser Lynx, Rail Gun Tiger, Acid Cloud Lynx, Starflare Lynx,
        GeoCon, Repair Vehicle, Robo-Surveyor
      Special Buildings: Tokamak, Residence, Agridome, Vehicle Factory,
        Garage, Laboratory, Geothermal Plant
      Team Upgrades: Weapon tech research tree (Laser, Rail Gun, Acid Cloud, Starflare, EMP)
      Support powers: Supernova Missile
      Superweapons: Supernova Missile

      Features:
      - Primary Colony from NewTerra
      - High tech faction with colony-building mechanics
      - Must build a colony (Residence, Agridome, Lab, Factory) to operate
      - Has fast Lynx vehicles and strong Tiger vehicles
      - Laser: pulsed chemical laser with rapid cycle time, defeats ablative armor
      - Rail Gun: kinetic projectile at several km/s, no explosive warhead needed
      - Acid Cloud: corrosive area-denial weapon
      - Starflare: shaped-charge suicide turret for close-range destruction
      - GeoCon: transforms into Geothermal Plant over fumaroles (Eden only)
      - Repair Vehicle: field-repairs vehicles to 50% HP (Eden only)
      - Robo-Surveyor: surveys mining beacons for ore type and yield
      - Supernova Missile: tactical nuclear superweapon
      - Modular vehicle chassis: Lynx (fast), Panther (balanced), Tiger (heavy)
      - Colony morale system affects productivity
      - More aggressive research tree than Plymouth
      - EMP Missile: disables all vehicles in target area

support_supernova_missile =
  .description = Launch a tactical missile.
    Applies heavy damage over a large area.

actor_tokamak =
  .description = Generates power
    Releases Emp Shockwave on death

actor_eden_residence =
  .description = Colony Building
  Needed along with Agridome for Vehicle Factory
  Provides Radar

  Limited to 1.

actor_eden_lab_basic =
  .description = Basic Laboratory
    Needed along with Nursery and University for Standard Lab

actor_eden_factory_vehicle =
  .description = Manufactures vehicles.
    Requires Residence and Agridome

actor_eden_garage =
  .description = Repairs vehicles.
    Allows construction of MCVs

eden_smelter_common =
  .description = Processes resources

eden_agridome =
  .description = Colony Building
  Needed with Residence for Vehicle Factory

  Limited to 1.

eden_nursery =
  .description = Colony Building
  Needed with University and Basic Lab for Standard Lab

  Limited to 1.

eden_university =
  .description = Colony Building
  Needed with Nursery and Basic Lab for Standard Lab

  Limited to 1.

eden_lab_standard =
  .description = Colony Building
  Needed for DIRT, Consumer Factory, GORF, and Advanced Lab

  Limited to 1.

eden_dirt =
  .description = Provides Force Shield

eden_rcc =
  .description = Colony Building
  Needed for Consumer Factory

  Limited to 1.

eden_factory_consumer =
  .description = Manufactures Consumer Goods

eden_gorf =
  .description = Increases income from Common Ore Smelter

eden_lab_advanced =
  .description = Colony Building
  Needed for Space Port, Rare Ore Smelter

  Limited to 1.

eden_spaceport =
  .description = Needed for Solar Power Array
   Launches Supernova Missiles.

eden_solar_array =
  .description = Generates power
   Releases Emp Shockwave on
   Death

eden_smelter_rare =
  .description = Needed for Tigers

eden_light_tower =
  .description = Provides sight and detection.

eden_mine_common =
  .description = Provides passive income.
   spawns common ore

eden_storage_common =
  .description = Stores processed Tiberium

eden_gp_laser =
  .description = Laser defense.
   Strong vs Tanks, vehicles
   Weak vs Infantry

eden_gp_emp =
  .description = EMP defense.
   Strong vs Tanks, vehicles
   Weak vs Infantry

eden_gp_railgun =
  .description = Railgun defense.
   Strong vs Tanks, vehicles
   Weak vs Infantry

eden_impulseitems =
  .description = Cheap Items - Returns $600

eden_impulseitems_3 =
  .description = Basic Items - Returns $1100

eden_impulseitems_2 =
  .description = Luxury Items - Returns $2300

# PLYMOUTH localizations (mirror of Eden)
plymouth_smelter_common =
  .description = Processes resources

plymouth_agridome =
  .description = Colony Building
  Needed with Residence for Vehicle Factory

  Limited to 1.

plymouth_nursery =
  .description = Colony Building
  Needed with University and Basic Lab for Standard Lab

  Limited to 1.

plymouth_university =
  .description = Colony Building
  Needed with Nursery and Basic Lab for Standard Lab

  Limited to 1.

plymouth_lab_standard =
  .description = Colony Building
  Needed for DIRT, Consumer Factory, GORF, and Advanced Lab

  Limited to 1.

plymouth_dirt =
  .description = Provides Force Shield

plymouth_rcc =
  .description = Colony Building
  Needed for Consumer Factory

  Limited to 1.

plymouth_factory_consumer =
  .description = Manufactures Consumer Goods

plymouth_gorf =
  .description = Increases income from Common Ore Smelter

plymouth_lab_advanced =
  .description = Colony Building
  Needed for Space Port, Rare Ore Smelter

  Limited to 1.

plymouth_spaceport =
  .description = Needed for Solar Power Array
   Launches Supernova Missiles.

plymouth_solar_array =
  .description = Generates power
   Releases Emp Shockwave on
   Death

plymouth_smelter_rare =
  .description = Needed for Tigers

plymouth_light_tower =
  .description = Provides sight and detection.

plymouth_mine_common =
  .description = Provides passive income.
   spawns common ore

plymouth_storage_common =
  .description = Stores processed Tiberium

plymouth_gp_microwave =
  .description = Microwave defense.
   Strong vs Tanks, vehicles
   Weak vs Infantry

plymouth_gp_stickyfoam =
  .description = Stickyfoam defense.
   Strong vs Tanks, vehicles
   Weak vs Infantry

plymouth_gp_rpg =
  .description = RPG defense.
   Strong vs Tanks, vehicles
   Doubles as Anti-air

plymouth_convec_structure_factory =
  .description = Deploys into a Structure Factory.
   Unarmed

plymouth_scout =
  .description = Fast scout armed with a machine gun

plymouth_lynx_microwave =
  .description = Fast microwave vehicle

plymouth_tiger_microwave =
  .description = Slow and strong microwave vehicle

plymouth_lynx_rpg =
  .description = Fast rpg vehicle

plymouth_tiger_rpg =
  .description = Slow and strong RPG vehicle

plymouth_lynx_emp =
  .description = Fast EMP vehicle

plymouth_tiger_emp =
  .description = Slow and strong EMP vehicle

plymouth_lynx_stickyfoam =
  .description = Fast Stickyfoam vehicle

plymouth_tiger_stickyfoam =
  .description = Slow and strong Stickyfoam vehicle

plymouth_lynx_esg =
  .description = Fast ESG vehicle

plymouth_tiger_esg =
  .description = Slow and strong ESG vehicle

plymouth_lynx_starflare =
  .description = Fast starflare vehicle

plymouth_tiger_starflare =
  .description = Slow and strong Starflare vehicle

plymouth_lynx_supernova =
  .description = Fast supernova vehicle

plymouth_tiger_supernova =
  .description = Slow and strong Supernova vehicle

eden_convec_structure_factory =
  .description = Deploys into another Command Center.
   Unarmed

eden_scout =
  .description = Fast scout armed with a machine gun

eden_lynx_laser =
  .description = Fast laser vehicle

eden_tiger_microwave =
  .description = Slow and strong laser vehicle

eden_lynx_railgun =
  .description = Fast railgun vehicle

eden_tiger_railgun =
  .description = Slow and strong railgun vehicle

eden_lynx_emp =
  .description = Fast EMP vehicle

eden_tiger_emp =
  .description = Slow and strong EMP vehicle

eden_lynx_acidcloud =
  .description = Fast AcidCloud vehicle

eden_tiger_acidcloud =
  .description = Slow and strong AcidCloud vehicle

eden_lynx_starflare =
  .description = Fast starflare vehicle

eden_tiger_starflare =
  .description = Slow and strong Starflare vehicle

eden_lynx_thorshammer =
  .description = Fast ThorsHammer vehicle

eden_tiger_thorshammer =
  .description = Slow and strong ThorsHammer vehicle

plymouth_residence =
  .description = Colony Building
  Needed with Agridome for Vehicle Factory
  Provides Radar

  Limited to 1.

plymouth_basic_lab =
  .description = Colony Building
  Needed with Nursery and University for Standard Lab

  Limited to 1.

plymouth_factory_vehicle =
  .description = Constructs Vehicles

plymouth_garage =
  .description = Repairs vehicles.
   Allows construction of MCVs

plymouth_factory_arachnid =
  .description = Constructs Arachnids

plymouth_spider =
  .description = Arachnid capable of repair and capturing vehicles

plymouth_scorpion =
  .description = Arachnid armed with laser
