mod-title = Cameo
mod-windowtitle = OpenRA - Cameo

button-tab-container-asengine = Attacque Supérior

## ingame-observer.yaml
button-observer-widgets-pause-tooltip = Pause
button-observer-widgets-play-tooltip = Play

button-observer-widgets-slow =
   .tooltip = Slow speed
   .label = 50%

button-observer-widgets-regular =
   .tooltip = Regular speed
   .label = 100%

button-observer-widgets-fast =
   .tooltip = Fast speed
   .label = 200%

button-observer-widgets-maximum =
   .tooltip = Maximum speed
   .label = MAX

options-observer-stats =
    .minimal = Minimal
    .none = Information: None
    .basic = Basic
    .economy = Economy
    .production = Production
    .support-powers = Support Powers
    .combat = Combat
    .army = Army
    .upgrades = Upgrades
    .promotions = Promotions
    .earnings-graph = Earnings (graph)
    .army-graph = Army (graph)

label-minimal-stats-player-header = Player
label-minimal-stats-cash-header = Cash
label-minimal-stats-power-header = Power
label-minimal-stats-points-header = Points
label-minimal-stats-harvesters-header = Harv.

promotion-counter =
    .rank = Current Rank:
    .points = Promotion Points:
    .progress = Progress to next rank:

actor-stats-label-prefix =
    .armor = Armor:
    .sight = Sight:
    .speed = Speed:
    .power = Power:
    .mindcontrol = Mind Control:
    .damage = Damage:
    .spread = Spread:
    .rof = Reload Delay:
    .range = Range:
    .resource = Load:
    .cashtrickler = Income:
    .periodicproducer = Production:
    .cargo = Passengers:
    .garrison = Garrisoners:
    .sharedcargo = Passengers:
    .carrier = Carrier:
    .mob = Mob:
    .drone = Drones:
    .kills = Kills:
    .experience = Experience:

label-armor-class =
    .no-armor = None
    .None = None
    .Flak = Flak
    .Plate = Plate
    .Heroic = Hero
    .Fighter = Fighter
    .Bomber = Bomber
    .Helicopter = Helicopter
    .Spaceship = Spaceship
    .Wood = Wood
    .Concrete = Concrete
    .Steel = Steel
    .Scout = Scout
    .Light = Light
    .Medium = Medium
    .Heavy = Heavy
    .Superheavy = Superheavy
    .Shield = Shield

loadscreen-loading = Drag to set a formation for units when attack-moving$
   Access your Promotions tab through the star button on the sidebar$
   The promotions counter at the top edge of the screen displays your points and progress toward earning them$
   Multiple production buildings can be cycled through using their categories' respective hotkeys$
   Radiation will damage affected units regardless of player allegiance, and resulting deaths do not count as kills$
   Using Ctrl when issuing a build order will insert it at the front of the build queue$
   Using Alt when issuing a build order will enable an infinite build queue for that unit$
   (Ctrl+)Middle-clicking when issuing a build order will cancel the order without On-Hold$
   Having multiple Construction Yards (or equivalent) will greatly speed up the construction process$
   Dune 2000 factions are able to construct concrete foundations, increasing armor and enabling self-repair for structures above them$
   Starcraft factions treat the game's "power" resource the same as the "supply" resource in the original game$
   Starcraft factions do not have a limit on how many units they can have, as long as their supply can support them$
   Some upgrades are team upgrades, meaning your allies also receive the effect of the upgrade$
   Try selecting a Construction Yard (or equivalent) and then issuing a move order while holding Alt$
   Some Red Alert 1/2 factions employ cryo weaponry, which can freeze enemy units over time$
   Some weapons apply a burning effect, indicated by the red stripe below the health bar. Burning units will take damage over time$
   All Protoss units and structures are shielded; shields can regenerate over time when not in combat$
   The Ixian faction has a team upgrade that gives a personal shield to their allies' units$
   The Consortium faction is able to construct their buildings underwater, hiding them from view$
   Many air units are equipped with stealth-detecting sensors, whose range is indicated by the green dashed circle$
   Garrisonable buildings usually heal over time, but they can be permanently destroyed with a large enough burst of instant damage$
   Resource tiers: Ore (25) < Tiberium (30) < Blue Tiberium (35) < Red Tiberium (40) < Gold Tiberium (45) < Gems (50)$
   Small blossom trees replenish resources at a noticeably slower rate compared to normal ones$
   Refineries have a limit on how many harvesters can be queued to dock at a time$
   Air units are generally immune to mind control effects$
   Issuing a deploy order (defaults to F key) on rearmable aircraft will tell them to return to base$
   If the game's graphics are too bright (especially on snow theater) or too dim, you can adjust this in the settings$
   The information panel at the bottom-left of the screen contains useful stats, including what upgrades a unit currently has or could have$
   When a team upgrade has been researched, a small icon of the upgrade will be displayed at the top-left corner of the screen

# Double-tapping Q to select all of your combat units on the map$
# Q followed with F is the quickest way to deploy the MCV at the start of the game

## ingame-observer.yaml, ingame-player.yaml
label-mute-indicator = Audio Muted
button-top-buttons-options-tooltip = Options

## ingame-player.yaml
supportpowers-support-powers-palette =
   .ready = READY
   .hold = ON HOLD

button-command-bar-attack-move =
   .tooltip = Attack Move
   .tooltipdesc = Selected units will move to the desired location
    and attack any enemies they encounter en route.

    Hold <(Ctrl)> while targeting to order an Assault Move
    that attacks any units or structures encountered en route.

    Left-click icon then right-click on target location.

button-command-bar-force-move =
   .tooltip = Force Move
   .tooltipdesc = Selected units will move to the desired location
     - Default activity for the target is suppressed
     - Vehicles will attempt to crush enemies at the target location
     - Helicopters will land at the target location

    Left-click icon then right-click on target.
    Hold <(Alt)> to activate temporarily while commanding units.

button-command-bar-force-attack =
   .tooltip = Force Attack
   .tooltipdesc = Selected units will attack the targeted unit or location
     - Default activity for the target is suppressed
     - Allows targeting of own or ally forces
     - Long-range artillery units will always target the
       location, ignoring units and buildings

    Left-click icon then right-click on target.
    Hold <(Ctrl)> to activate temporarily while commanding units.

button-command-bar-guard =
   .tooltip = Guard
   .tooltipdesc = Selected units will follow the targeted unit.

    Left-click icon then right-click on target unit.

button-command-bar-deploy =
   .tooltip = Deploy
   .tooltipdesc = Selected units will perform their default deploy activity
     - MCVs will unpack into a Construction Yard
     - Construction Yards will re-pack into a MCV
     - Transports will unload their passengers
     - Suicide bombers will detonate
     - Minelayers will deploy a mine
     - Rearmable aircraft will return to base
     - Other units may swap or detonate weapons

    Acts immediately on selected units.

button-command-bar-scatter =
   .tooltip = Scatter
   .tooltipdesc = Selected units will stop their current activity
    and move to a nearby location.

    Acts immediately on selected units.

button-command-bar-stop =
   .tooltip = Stop
   .tooltipdesc = Selected units will stop their current activity.
    Selected buildings will reset their rally point.

    Acts immediately on selected targets.

button-command-bar-queue-orders =
   .tooltip = Waypoint Mode
   .tooltipdesc = Use Waypoint Mode to give multiple linking commands
    to the selected units. Units will execute the commands
    immediately upon receiving them.

    Left-click icon then give commands in the game world.
    Hold <(Shift)> to activate temporarily while commanding units.

button-stance-bar-attackanything =
   .tooltip = Attack Anything Stance
   .tooltipdesc = Set the selected units to Attack Anything stance:
     - Units will attack enemy units and structures on sight
     - Units will pursue attackers across the battlefield

button-stance-bar-defend =
   .tooltip = Defend Stance
   .tooltipdesc = Set the selected units to Defend stance:
     - Units will attack enemy units on sight
     - Units will not move or pursue enemies

button-stance-bar-returnfire =
   .tooltip = Return Fire Stance
   .tooltipdesc = Set the selected units to Return Fire stance:
     - Units will retaliate against enemies that attack them
     - Units will not move or pursue enemies

button-stance-bar-holdfire =
   .tooltip = Hold Fire Stance
   .tooltipdesc = Set the selected units to Hold Fire stance:
     - Units will not fire upon enemies
     - Units will not move or pursue enemies

button-top-buttons-beacon-tooltip = Place Beacon
button-top-buttons-sell-tooltip = Sell
button-top-buttons-power-tooltip = Power Down
button-top-buttons-repair-tooltip = Repair

productionpalette-sidebar-production-palette =
   .ready = READY
   .hold = ON HOLD

button-production-types-building-tooltip = Buildings
button-production-types-defense-tooltip = Defense
button-production-types-infantry-tooltip = Infantry
button-production-types-vehicle-tooltip = Vehicles
button-production-types-aircraft-tooltip = Aircraft
button-production-types-naval-tooltip = Naval
button-production-types-addon-tooltip = Building Addons
button-production-types-upgrade-tooltip = Upgrades
button-production-types-promotion-tooltip = Promotions
button-production-types-scroll-up-tooltip = Scroll up
button-production-types-scroll-down-tooltip = Scroll down

bot-ai =
   .easiest = Easiest AI
   .veryeasy = Very Easy AI
   .easy = Easy AI
   .medium = Medium AI
   .hard = Hard AI
   .veryhard = Very Hard AI
   .brutal = Brutal AI
   .challenger = Challenger AI
   .unbeatable = Unbeatable AI
   .cameogod = Cameo God AI

support-power-timer = { $player }'s { $support-power }: { $time }

## settings-gameplay.yaml
label-experimental-section-header = Experimental Features
checkbox-quota-mode =
    .label = Quota Mode (Experimental)
    .tooltip = Production buildings automatically re-queue units to maintain target alive counts per type.
        Left-click a unit in the production panel to set its target; right-click to lower it.
        Due to its instability, this feature is currently single-player only, and disabled in multiplayer.
