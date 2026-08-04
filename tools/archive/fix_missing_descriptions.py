#!/usr/bin/env python3
"""
Fix missing Buildable descriptions by adding inline descriptions based on actor name and Tooltip Name.
Only adds to actors that have Buildable but no Description field in Buildable.
"""

import os
import re
import sys
from pathlib import Path

MOD_ROOT = os.path.join(os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__)))), "mods", "cameo")

LOADED_PATH_PREFIXES = [
    'ContentPacks/TiberianDawn',
    'ContentPacks/RedAlert',
    'ContentPacks/TiberianSun',
    'ContentPacks/RedAlert2',
    'ContentPacks/RedAlert2Mod',
    'ContentPacks/TKM',
    'ContentPacks/D2k',
    'ContentPacks/StarCraft',
    'ContentPacks/Warcraft2',
    'ContentPacks/Outpost2',
    'ContentPacks/Core',
]

LOADED_RULES_FILES = [
    'rules/misc.yaml', 'rules/player.yaml', 'rules/world.yaml',
    'rules/map_generators.yaml', 'rules/music.yaml', 'rules/palettes.yaml',
    'rules/defaults.yaml', 'rules/shared.yaml', 'rules/trees.yaml',
    'rules/civilian.yaml', 'rules/civilian_desert.yaml', 'rules/tech.yaml',
    'rules/husks.yaml', 'rules/promotions.yaml', 'ai/ai.yaml',
    'rules/starcraft.yaml', 'rules/warcraft2.yaml', 'rules/tkm.yaml',
    'rules/redalert.yaml', 'rules/tiberiansun.yaml', 'rules/redalert2.yaml',
    'rules/redalert2mod.yaml', 'rules/d2k.yaml', 'rules/outpost2.yaml',
]

def is_loaded(filepath):
    rel = os.path.relpath(filepath, MOD_ROOT).replace('\\', '/')
    for prefix in LOADED_PATH_PREFIXES:
        if rel.startswith(prefix):
            return True
    for rf in LOADED_RULES_FILES:
        if rel == rf:
            return True
    return False

def generate_description(actor_name, tooltip_name):
    """Generate a sensible description from actor name and tooltip name."""
    name = tooltip_name if tooltip_name else actor_name
    name = name.strip().strip('"').strip("'")
    actor_lower = actor_name.lower()

    # Skip proxy/infiltrated/sub-actors
    if 'infiltrated' in actor_lower:
        return None

    # Handle specific dot-actors before the generic skip
    if 'cgyard.asian' in actor_lower or 'cgyard.latin' in actor_lower or 'naval.nax' in actor_lower:
        return None  # Skip - proxy actors
    if 'promotion_upgrade.template' in actor_lower:
        return "Promotion unlock upgrade"
    if 'upgrade.template' in actor_lower:
        return "Generic upgrade"
    if 'combat_tank.atreides' in actor_lower:
        return "Main battle tank.\\n  Strong vs Vehicles\\n  Weak vs Infantry"
    if 'd2k_silo.atreides' in actor_lower:
        return "Stores excess spice."
    if 'refinery.atreides' in actor_lower:
        return "Processes spice into credits."
    if 'wind_trap.atreides' in actor_lower:
        return "Provides power to the base."
    if 'camera.gpssat' in actor_lower:
        return "GPS satellite camera. Reveals map area."
    if 'ra2shk.bot' in actor_lower:
        return "Bot-only unit."

    if '.' in actor_name:
        return None

    # Buildings - power
    if 'windtrap' in actor_lower:
        return "Provides power to the base."
    if 'powerplant' in actor_lower or 'petrolplant' in actor_lower or 'advancedpower' in actor_lower:
        return "Provides power to the base."

    # Buildings - production
    if 'constructionyard' in actor_lower or 'mobileconstructionvehicle' in actor_lower or '_mcv' in actor_lower:
        return "Deploys into a Construction Yard.\\n  Unarmed"
    if 'barracks' in actor_lower or 'handofnod' in actor_lower:
        return "Trains infantry units."
    if 'refinery' in actor_lower or 'orerefinery' in actor_lower or 'tiberiumrefinery' in actor_lower:
        return "Processes ore into credits."
    if 'oretruck' in actor_lower or 'harvester' in actor_lower:
        return "Harvests ore for processing.\\n  Unarmed"
    if 'warfactory' in actor_lower or 'weaponsfactory' in actor_lower or 'syndicatefactory' in actor_lower:
        return "Produces vehicles."
    if 'hightechfactory' in actor_lower:
        return "Produces advanced vehicles."

    # Buildings - support
    if 'radar' in actor_lower or 'radardome' in actor_lower or 'radararray' in actor_lower or 'communicationscenter' in actor_lower:
        return "Provides radar coverage of the battlefield."
    if 'servicedepot' in actor_lower or 'repairfacility' in actor_lower or 'repairpad' in actor_lower:
        return "Repairs nearby vehicles."
    if 'helipad' in actor_lower or 'airpad' in actor_lower or 'airforce' in actor_lower or 'airfield' in actor_lower:
        return "Produces and repairs aircraft."
    if 'techcenter' in actor_lower or 'battlelab' in actor_lower or 'researchcenter' in actor_lower or 'ixresearchcenter' in actor_lower:
        return "Unlocks advanced technology."
    if 'academy' in actor_lower:
        return "Trains elite infantry and unlocks upgrades."
    if 'spycenter' in actor_lower:
        return "Provides intelligence capabilities."

    # Buildings - naval
    if 'naval' in actor_lower or 'shipyard' in actor_lower or 'navyard' in actor_lower or 'syrd' in actor_lower or 'subpen' in actor_lower or 'spen' in actor_lower:
        return "Naval production structure."

    # Buildings - D2k
    if 'starport' in actor_lower:
        return "Purchase units from off-world suppliers."
    if 'outpost' in actor_lower:
        return "Expands buildable area and provides radar."
    if 'launchpad' in actor_lower:
        return "Launches units to and from orbit."
    if 'storagesilo' in actor_lower or 'silo' in actor_lower:
        return "Stores excess resources."
    if 'concreteslab' in actor_lower or 'concretea' in actor_lower or 'concreteb' in actor_lower:
        return "Provides flat building foundation."

    # Buildings - WC2
    if 'farm' in actor_lower or 'pigfarm' in actor_lower:
        return "Provides food and supply for units."
    if 'goldmine' in actor_lower and '_2' in actor_lower:
        return "Send workers inside to provide passive income."

    # Buildings - defenses
    if 'bunkertemplate' in actor_lower:
        return "Garrisonable defensive bunker"
    if 'barrier' in actor_lower or 'wall' in actor_lower or 'brik' in actor_lower:
        return "Defensive wall that blocks movement."
    if 'bunker' in actor_lower or 'addon' in actor_lower:
        return "Defensive bunker addon."

    # Infantry
    if 'engineer' in actor_lower:
        return "Captures and repairs structures.\\n  Unarmed"
    if 'rocket' in actor_lower and 'soldier' in actor_lower:
        return "Anti-tank infantry.\\n  Strong vs Vehicles, Aircraft\\n  Weak vs Infantry"
    if 'minigunner' in actor_lower or 'rifleinfantry' in actor_lower or 'lightinfantry' in actor_lower:
        return "Basic infantry unit.\\n  Strong vs Infantry\\n  Weak vs Vehicles"
    if 'commando' in actor_lower:
        return "Elite commando unit.\\n  Strong vs Infantry, Buildings\\n  Can detect cloaked units"
    if 'marine' in actor_lower:
        return "Basic infantry unit.\\n  Strong vs Infantry\\n  Weak vs Vehicles"
    if 'trooper' in actor_lower:
        return "Anti-armor infantry.\\n  Strong vs Vehicles\\n  Weak vs Infantry"

    # Vehicles
    if 'chinook' in actor_lower or 'transport' in actor_lower:
        return "Transport vehicle.\\n  Unarmed"
    if 'landing' in actor_lower and 'craft' in actor_lower:
        return "Naval transport. Carries infantry to shore."
    if 'carryall' in actor_lower:
        return "Aircraft that picks up and carries vehicles.\\n  Unarmed"
    if 'mongoose' in actor_lower:
        return "Fast scout vehicle.\\n  Strong vs Infantry\\n  Weak vs Tanks"
    if 'gorynych' in actor_lower:
        return "Heavy assault tank with multiple weapons.\\n  Strong vs Vehicles, Infantry"
    if 'stalinfist' in actor_lower:
        return "Mobile command vehicle.\\n  Strong vs Vehicles, Infantry"
    if 'railgun' in actor_lower:
        return "Heavy tank with railgun weapon.\\n  Strong vs Vehicles, Tanks"
    if 'buggy' in actor_lower:
        return "Fast raider vehicle.\\n  Strong vs Infantry\\n  Weak vs Tanks"
    if 'stealthharvester' in actor_lower:
        return "Cloaked ore harvester.\\n  Unarmed"
    if 'scarabapc' in actor_lower:
        return "Armored personnel carrier.\\n  Can carry infantry"
    if 'demolitiontruck' in actor_lower or 'nuketruck' in actor_lower:
        return "Suicide vehicle that detonates on contact."

    # Creatures/wild
    if 'fiend' in actor_lower and 'wild' in actor_lower:
        return "Wild Tiberian creature.\\n  Strong vs Vehicles\\n  Weak vs Infantry"
    if 'mutant' in actor_lower and 'wild' in actor_lower:
        return "Wild mutant creature. Attacks nearby enemies."

    # StarCraft
    if 'creepcolony' in actor_lower and '_2' in actor_lower:
        return "Morphs into defensive structures."

    # Template actors (defaults.yaml)
    if 'harvestertemplate' in actor_lower:
        return "Harvests ore for processing.\\n  Unarmed"
    if 'mainbattletanktemplate' in actor_lower:
        return "Main battle tank.\\n  Strong vs Vehicles\\n  Weak vs Infantry"
    if 'scoutvehicletemplate' in actor_lower:
        return "Fast scout vehicle.\\n  Strong vs Infantry\\n  Weak vs Tanks"
    if 'supportvehicletemplate' in actor_lower:
        return "Support vehicle.\\n  Unarmed"
    if 'hightechtanktemplate' in actor_lower:
        return "Advanced vehicle with specialized weaponry."
    if 'linebreakertemplate' in actor_lower:
        return "Heavy siege unit.\\n  Strong vs Buildings, Vehicles\\n  Weak vs Infantry"
    if 'artillerytemplate' in actor_lower:
        return "Long-range artillery.\\n  Strong vs Buildings, Infantry\\n  Weak vs Vehicles"
    if 'firesupporttemplate' in actor_lower:
        return "Mobile fire support unit.\\n  Strong vs Vehicles\\n  Weak vs Infantry"
    if 'epicvehicletemplate' in actor_lower:
        return "Epic vehicle. Extremely powerful and expensive."
    if 'medictemplate' in actor_lower:
        return "Heals nearby infantry.\\n  Unarmed"
    if 'mechanictemplate' in actor_lower:
        return "Repairs nearby vehicles.\\n  Unarmed"
    if 'scoutinfantrytemplate' in actor_lower:
        return "Light scout infantry.\\n  Strong vs Infantry\\n  Weak vs Vehicles"
    if 'grenadierinfantrytemplate' in actor_lower:
        return "Grenadier infantry.\\n  Strong vs Infantry, Buildings\\n  Weak vs Vehicles"
    if 'antitankantiairinfantrytemplate' in actor_lower:
        return "Anti-vehicle and anti-air infantry.\\n  Strong vs Vehicles, Aircraft\\n  Weak vs Infantry"
    if 'heavyinfantrytemplate' in actor_lower:
        return "Heavy infantry unit.\\n  Strong vs Vehicles, Infantry"
    if 'meleeinfantrytemplate' in actor_lower:
        return "Melee infantry unit.\\n  Strong vs Infantry\\n  Weak vs Vehicles"
    if 'mortarinfantrytemplate' in actor_lower:
        return "Mortar infantry.\\n  Strong vs Infantry, Buildings\\n  Weak vs Vehicles"
    if 'sniperinfantrytemplate' in actor_lower:
        return "Sniper infantry.\\n  Strong vs Infantry\\n  Weak vs Vehicles"
    if 'heroinfantrytemplate' in actor_lower:
        return "Hero unit. Powerful and versatile."
    if 'fightertemplate' in actor_lower:
        return "Fighter aircraft.\\n  Strong vs Aircraft\\n  Weak vs Anti-Air"
    if 'bombertemplate' in actor_lower:
        return "Bomber aircraft.\\n  Strong vs Buildings, Vehicles\\n  Weak vs Fighters"
    if 'helicoptertemplate' in actor_lower:
        return "Helicopter gunship.\\n  Strong vs Vehicles, Infantry\\n  Weak vs Anti-Air"
    if 'spaceshiptemplate' in actor_lower:
        return "Spaceship\\n  Strong vs Vehicles, Aircraft"
    if 'epicairunittemplate' in actor_lower:
        return "Epic aircraft. Extremely powerful and expensive."
    if 'flyinginfantrytemplate' in actor_lower:
        return "Flying infantry unit.\\n  Strong vs Infantry\\n  Weak vs Anti-Air"
    if 'unarmedtransporthelicoptertemplate' in actor_lower:
        return "Transport helicopter.\\n  Unarmed"
    if 'scoutshiptemplate' in actor_lower:
        return "Fast scout ship.\\n  Strong vs Ships\\n  Weak vs Aircraft"
    if 'artilleryshiptemplate' in actor_lower:
        return "Artillery ship.\\n  Strong vs Buildings, Ships\\n  Weak vs Aircraft"
    if 'battleshiptemplate' in actor_lower:
        return "Heavy battleship.\\n  Strong vs Ships, Buildings\\n  Weak vs Aircraft"
    if 'basicdefensetemplate' in actor_lower:
        return "Basic defensive structure."
    if 'antiairdefensetemplate' in actor_lower:
        return "Anti-air defensive structure."
    if 'advanceddefensetemplate' in actor_lower:
        return "Advanced defensive structure."
    if 'bunkertemplate' in actor_lower:
        return "Garrisonable defensive bunker"
    if 'superdefensetemplate' in actor_lower:
        return "Super defensive structure."
    if 'powerplant' in actor_lower or actor_lower == '^power':
        return "Provides power to the base."
    if 'producesunits' in actor_lower:
        return "Production structure"
    if 'superweapon' in actor_lower:
        return "Superweapon structure."
    if 'upgrade.template' in actor_lower:
        return "Generic upgrade"
    if 'promotion_upgrade.template' in actor_lower:
        return "Promotion unlock upgrade"
    if actor_lower == '^dino':
        return "Dinosaur - wild creature that attacks nearby enemies"
    if actor_lower == '^monster':
        return "Monster - wild creature that attacks nearby enemies"

    # WC2 template actors
    if 'wc2_worker' in actor_lower or 'wc2worker' in actor_lower or 'wc2_peasant' in actor_lower:
        return "Worker unit. Gathers resources and constructs buildings.\\n  Unarmed"
    if 'wc2_blacksmith' in actor_lower:
        return "Enables weapon and armor upgrades."
    if 'wc2_lumber_mill' in actor_lower:
        return "Enables ranged unit upgrades and production."
    if 'wc2_inventor' in actor_lower:
        return "Provides radar and trains sapper units."
    if 'wc2_church' in actor_lower:
        return "Enables spell caster upgrades and training."
    if 'wc2_nest' in actor_lower:
        return "Produces flying units."
    if 'wc2_foundry' in actor_lower:
        return "Unlocks advanced naval vessels and upgrades."
    if 'wc2_destroyer' in actor_lower:
        return "Light warship.\\n  Strong vs Ships, Aircraft\\n  Weak vs Battleships"
    if 'wc2_battleship' in actor_lower:
        return "Heavy warship with long range cannons.\\n  Strong vs Ships, Buildings\\n  Weak vs Aircraft"
    if 'wc2_airscout' in actor_lower:
        return "Flying scout unit.\\n  Detector"
    if 'wc2_demolitioner' in actor_lower:
        return "Demolition expert. Destroys buildings and obstacles."
    if 'wc2_mage' in actor_lower:
        return "Spell caster with multiple magical abilities."
    if 'wc2_barracks' in actor_lower:
        return "Trains infantry units."
    if 'wc2_oil_refinery' in actor_lower:
        return "Processes oil into resources."
    if 'wc2_oil_platform' in actor_lower:
        return "Provides passive income from offshore oil."
    if 'wc2_oil_tanker' in actor_lower:
        return "Transports oil from platforms to refineries.\\n  Unarmed"
    if 'wc2_stables' in actor_lower:
        return "Trains mounted cavalry units."
    if 'wc2_supplier' in actor_lower:
        return "Provides additional supply for units."
    if 'wc2_temple' in actor_lower:
        return "Enables spell caster upgrades and training."
    if 'wc2_watch_tower' in actor_lower:
        return "Observation tower. Can be upgraded to guard or cannon tower."
    if 'wc2_mcv' in actor_lower:
        return "Deploys into a Construction Yard.\\n  Unarmed"
    if 'wc2_engineer' in actor_lower:
        return "Captures and repairs structures.\\n  Unarmed"

    # StarCraft template actors
    if 'sctvehicle' in actor_lower:
        return "Terran vehicle template."
    if 'scworker' in actor_lower:
        return "Worker unit. Gathers resources and constructs buildings.\\n  Unarmed"
    if 'zergdoublespawner' in actor_lower:
        return "Zerg spawner that produces two units at once."

    # RA2 IFV variants
    if 'ifv_mg' in actor_lower:
        return "IFV with machine gun.\\n  Strong vs Infantry\\n  Weak vs Vehicles"
    if 'ifv_hmg' in actor_lower:
        return "IFV with heavy machine gun.\\n  Strong vs Infantry, Aircraft\\n  Weak vs Vehicles"
    if 'ifv_missile' in actor_lower:
        return "IFV with missile launcher.\\n  Strong vs Vehicles, Aircraft\\n  Weak vs Infantry"
    if 'ifv_chrono' in actor_lower:
        return "IFV with chrono weapon.\\n  Strong vs Vehicles\\n  Weak vs Infantry"
    if 'ifv_repair' in actor_lower:
        return "IFV repair variant. Repairs nearby vehicles.\\n  Unarmed"
    if 'battlefortress_2' in actor_lower or 'battlefortress_3' in actor_lower:
        return "Heavy assault vehicle with garrisonable infantry compartment."

    # RA1 shared infantry
    if actor_lower == 'rae1':
        return "Basic infantry unit.\\n  Strong vs Infantry\\n  Weak vs Vehicles"
    if actor_lower == 'rae3':
        return "Anti-tank infantry.\\n  Strong vs Vehicles, Aircraft\\n  Weak vs Infantry"
    if actor_lower == 'rae6':
        return "Captures and repairs structures.\\n  Unarmed"
    if actor_lower == 'ralst':
        return "Naval transport. Carries infantry to shore."

    # D2k shared
    if 'carryall' in actor_lower and 'advanced' not in actor_lower:
        return "Aircraft that picks up and carries vehicles.\\n  Unarmed"
    if 'concreteadefense' in actor_lower or 'concretebdefense' in actor_lower:
        return "Provides flat building foundation for defenses."
    if 'd2k_mine' in actor_lower or 'd2kmine' in actor_lower:
        return "Explosive mine that damages nearby enemies."

    # Japan templates
    if 'rafix' in actor_lower:
        return "Provides power to the base."
    if 'raproc' in actor_lower:
        return "Processes ore into credits."

    # Defaults - remaining templates
    if 'producesunits' in actor_lower:
        return "Production structure"

    # Misc
    if 'op2_supplier' in actor_lower:
        return "Supplies resources to nearby units."

    # Default: use the tooltip name
    if name and name != actor_name:
        return f"{name}"

    return None  # Can't generate a good description

def fix_file(filepath, dry_run=False):
    """Fix missing descriptions in a single YAML file."""
    with open(filepath, 'r', encoding='utf-8', errors='replace') as f:
        lines = f.readlines()

    changes = []
    current_actor = None
    current_actor_line = None
    in_buildable = False
    buildable_indent = 0
    has_description = False
    buildable_start_line = None
    tooltip_name = None
    last_buildable_indent = 0

    i = 0
    while i < len(lines):
        line = lines[i]
        stripped = line.rstrip('\n\r')

        if not stripped.strip() or stripped.strip().startswith('#'):
            i += 1
            continue

        indent = len(stripped) - len(stripped.lstrip())

        # Top-level actor definition
        if indent == 0 and stripped.endswith(':') and not stripped.startswith('-') and not stripped.startswith(' '):
            # Before resetting, check if previous actor's Buildable needs a description
            if in_buildable and not has_description and current_actor:
                desc = generate_description(current_actor, tooltip_name)
                if desc is not None:
                    insert_line = buildable_start_line + 1
                    for k in range(buildable_start_line + 1, i):
                        kline = lines[k].rstrip('\n\r')
                        if re.match(r'^\s+(Queue|Prerequisites):', kline):
                            insert_line = k + 1
                    desc_indent = '\t' * 2
                    for k in range(buildable_start_line + 1, i):
                        kline = lines[k].rstrip('\n\r')
                        if kline.strip() and not kline.strip().startswith('#'):
                            desc_indent = kline[:len(kline) - len(kline.lstrip())]
                            break
                    new_line = f"{desc_indent}Description: {desc}\n"
                    changes.append((current_actor, current_actor_line, desc))
                    if not dry_run:
                        lines.insert(insert_line, new_line)
                        i += 1
            # Reset state for new actor
            current_actor = stripped.rstrip(':').strip()
            current_actor_line = i + 1
            in_buildable = False
            has_description = False
            tooltip_name = None
            i += 1
            continue

        if current_actor is None:
            i += 1
            continue

        # Check for Buildable section
        if re.match(r'^\s+Buildable:', stripped) or re.match(r'^\s+Buildable@[^:]+:', stripped):
            in_buildable = True
            buildable_indent = indent
            buildable_start_line = i
            has_description = False
            i += 1
            continue

        # Check for Tooltip Name
        if re.match(r'^\s+Tooltip:', stripped) or re.match(r'^\s+Tooltip@[^:]+:', stripped):
            # Look ahead for Name field
            j = i + 1
            while j < len(lines):
                next_line = lines[j].rstrip('\n\r')
                if not next_line.strip() or next_line.strip().startswith('#'):
                    j += 1
                    continue
                next_indent = len(next_line) - len(next_line.lstrip())
                if next_indent <= indent:
                    break
                if re.match(r'^\s+Name:', next_line):
                    tooltip_name = next_line.split('Name:', 1)[1].strip()
                    break
                j += 1
            i += 1
            continue

        # Check if leaving Buildable section
        if in_buildable and stripped.strip() and indent <= buildable_indent:
            in_buildable = False
            if not has_description:
                # Need to add a description
                desc = generate_description(current_actor, tooltip_name)
                if desc is not None:
                    # Insert after the Buildable: line (or after Queue/Prerequisites if they exist)
                    insert_line = buildable_start_line + 1
                    # Find the best insertion point - after Queue or Prerequisites
                    for k in range(buildable_start_line + 1, i):
                        kline = lines[k].rstrip('\n\r')
                        if re.match(r'^\s+(Queue|Prerequisites):', kline):
                            insert_line = k + 1

                    # Use the indent of existing fields in Buildable (preserve tabs)
                    desc_indent = '\t' * 2  # default
                    for k in range(buildable_start_line + 1, i):
                        kline = lines[k].rstrip('\n\r')
                        if kline.strip() and not kline.strip().startswith('#'):
                            desc_indent = kline[:len(kline) - len(kline.lstrip())]
                            break

                    new_line = f"{desc_indent}Description: {desc}\n"
                    changes.append((current_actor, current_actor_line, desc))
                    if not dry_run:
                        lines.insert(insert_line, new_line)
                        i += 1  # Adjust for inserted line
            i += 1
            continue

        # Check for Description in Buildable
        if in_buildable and re.match(r'^\s+Description:', stripped):
            has_description = True

        i += 1

    # Handle case where Buildable is the last section in the file
    if in_buildable and not has_description and current_actor:
        desc = generate_description(current_actor, tooltip_name)
        if desc is not None:
            insert_line = buildable_start_line + 1
            for k in range(buildable_start_line + 1, len(lines)):
                kline = lines[k].rstrip('\n\r')
                if re.match(r'^\s+(Queue|Prerequisites):', kline):
                    insert_line = k + 1
            desc_indent = '\t' * 2  # default
            for k in range(buildable_start_line + 1, len(lines)):
                kline = lines[k].rstrip('\n\r')
                if kline.strip() and not kline.strip().startswith('#'):
                    desc_indent = kline[:len(kline) - len(kline.lstrip())]
                    break
            new_line = f"{desc_indent}Description: {desc}\n"
            changes.append((current_actor, current_actor_line, desc))
            if not dry_run:
                lines.insert(insert_line, new_line)

    if changes and not dry_run:
        with open(filepath, 'w', encoding='utf-8', newline='\n') as f:
            f.writelines(lines)

    return changes

def main():
    dry_run = '--dry-run' in sys.argv

    # Find all YAML files in loaded factions
    yaml_files = []
    for dirpath, dirnames, filenames in os.walk(MOD_ROOT):
        if '.git' in dirpath:
            continue
        for f in filenames:
            if f.endswith('.yaml') or f.endswith('.yml'):
                fp = os.path.join(dirpath, f)
                if is_loaded(fp):
                    yaml_files.append(fp)

    total_changes = 0
    for yf in sorted(yaml_files):
        changes = fix_file(yf, dry_run)
        if changes:
            rel = os.path.relpath(yf, MOD_ROOT)
            print(f"\n{'DRY RUN: ' if dry_run else ''}{rel}:")
            for actor, line, desc in changes:
                print(f"  {actor} (line {line}): {desc[:80]}")
            total_changes += len(changes)

    print(f"\n{'DRY RUN: ' if dry_run else ''}Total fixes: {total_changes}")

if __name__ == '__main__':
    main()
