#!/usr/bin/env python3
"""Add faction-specific rank decorations to Starcraft actors."""
import re

filepath = "mods/cameo/rules/starcraft.yaml"
with open(filepath, "r", encoding="utf-8") as f:
    content = f.read()

# Insert 3 templates before the Terran Upgrade Inherits section
templates = """^ZergRankDecoration:
	WithDecoration@RANK-1:
		Image: alienrank
		Sequence: rank-veteran-1
		Palette: greyscale
	WithDecoration@RANK-2:
		Image: alienrank
		Sequence: rank-veteran-2
		Palette: greyscale
	WithDecoration@RANK-3:
		Image: alienrank
		Sequence: rank-veteran-3
		Palette: greyscale
	WithDecoration@RANK-ELITE:
		Image: alienrank
		Sequence: rank-elite
		Palette: greyscale

^ProtossRankDecoration:
	WithDecoration@RANK-1:
		Image: protossrank
		Sequence: rank-veteran-1
		Palette: greyscale
	WithDecoration@RANK-2:
		Image: protossrank
		Sequence: rank-veteran-2
		Palette: greyscale
	WithDecoration@RANK-3:
		Image: protossrank
		Sequence: rank-veteran-3
		Palette: greyscale
	WithDecoration@RANK-ELITE:
		Image: protossrank
		Sequence: rank-elite
		Palette: greyscale

^TerranRankDecoration:
	WithDecoration@RANK-1:
		Image: terranrank
		Sequence: rank-veteran-1
		Palette: greyscale
	WithDecoration@RANK-2:
		Image: terranrank
		Sequence: rank-veteran-2
		Palette: greyscale
	WithDecoration@RANK-3:
		Image: terranrank
		Sequence: rank-veteran-3
		Palette: greyscale
	WithDecoration@RANK-ELITE:
		Image: terranrank
		Sequence: rank-elite
		Palette: greyscale

"""

# Insert templates before "# Terran Upgrade Inherits" comment
marker = "####################################################################################################\n#\t\tTerran Upgrade Inherits"
content = content.replace(marker, templates + marker, 1)

# Actor list with faction
zerg_actors = ["zerg_creepcolony","zerg_zergling","zerg_talon","zerg_sporemaw","zerg_hydralisk",
    "zerg_spithid","zerg_dreadshroud","zerg_corruptor","zerg_swarmling","zerg_shriek",
    "zerg_mutalisk","zerg_devourer","zerg_scourge","zerg_queen","zerg_guardian",
    "zerg_ultralisk","zerg_gorekraken","zerg_hermit","zerg_goremaw","zerg_lurker",
    "zerg_infestedterranbomber","SCBROODLING","zerg_kerrigan","zerg_behemoth"]

protoss_actors = ["protoss_photoncannon","protoss_zealot","protoss_adept","protoss_dragoon",
    "protoss_manifold","protoss_analogue","protoss_idol","protoss_hightemplar",
    "protoss_darktemplar","protoss_reaver","protoss_archon","protoss_scout",
    "protoss_epigraph","protoss_voidray","protoss_gladius","protoss_corsair",
    "protoss_carrier","SCINTERCEPTOR","protoss_starshipsovereign","protoss_zeratul",
    "protoss_amaranth","protoss_atreus","protoss_positron","protoss_legionnaire","protoss_patriarch"]

terran_actors = ["terran_bunker","terran_missileturret","terran_sentinel","SCSENTINELM",
    "terran_marine","terran_reaper","terran_madcap","terran_firebat","terran_harakan",
    "terran_marauder","terran_ghost","terran_specter","terran_jimraynor","terran_vulture",
    "terran_siegetank","terran_matador","terran_cyclone","terran_goliath","terran_goliathmk2",
    "terran_warhound","terran_wraith","terran_sundog","terran_valkyrie","terran_wyvern",
    "terran_raven","terran_pythean","terran_battlecruiser","terran_phobos",
    "SCWRAITHDRONE","terran_sciencevessel"]

# For each actor, add Inherits@decoration after the actor definition line
# We need to find the actor definition and add the inherit after Inherits@exp or Inherits@EXPERIENCE line
# Strategy: find "actorname:\n" then find the next "Inherits@exp" or "Inherits@EXPERIENCE" line and add after it

def add_decoration(content, actor_name, decoration_template):
    # Find the actor definition
    pattern = r"^" + re.escape(actor_name) + r":\n"
    match = re.search(pattern, content, re.MULTILINE)
    if not match:
        print(f"WARNING: {actor_name} not found")
        return content
    
    # Find the Inherits@exp or Inherits@EXPERIENCE line after this actor
    start = match.end()
    # Search for next Inherits@exp or Inherits@EXPERIENCE line
    exp_pattern = r"(\tInherits@(exp|EXPERIENCE):[^\n]+\n)"
    exp_match = re.search(exp_pattern, content[start:start+2000])
    if not exp_match:
        # Try adding after any Inherits line
        exp_pattern = r"(\tInherits@[A-Za-z]+:[^\n]+\n)"
        exp_match = re.search(exp_pattern, content[start:start+2000])
        if not exp_match:
            print(f"WARNING: No Inherits line found for {actor_name}")
            return content
    
    insert_pos = start + exp_match.end()
    decoration_line = f"\tInherits@decoration: {decoration_template}\n"
    
    # Check if it's already there
    if decoration_template in content[insert_pos:insert_pos+100]:
        print(f"SKIP: {actor_name} already has {decoration_template}")
        return content
    
    content = content[:insert_pos] + decoration_line + content[insert_pos:]
    print(f"OK: {actor_name} -> {decoration_template}")
    return content

for actor in zerg_actors:
    content = add_decoration(content, actor, "^ZergRankDecoration")

for actor in protoss_actors:
    content = add_decoration(content, actor, "^ProtossRankDecoration")

for actor in terran_actors:
    content = add_decoration(content, actor, "^TerranRankDecoration")

with open(filepath, "w", encoding="utf-8") as f:
    f.write(content)

print("Done!")
