#!/usr/bin/env python3
"""Reject physical-state warhead combinations that double-apply a meter.

The Formula V2 Flame and Chemical families apply Temperature/Corrosion in
proportion to actual damage. Their percentage-damage twins must use
AreaDamagePercentage so that damage also reaches the meter. A resolved weapon
must not combine either damage-scaled implementation with a legacy fixed
ApplyPhysicalState warhead for the same meter.
"""

from __future__ import annotations

import sys
import re

from miniyaml import Ruleset, find_repo_root


EXPECTED_PERCENTAGE_STATES = {
	"Flame": ("Temperature", "100"),
	"Chemical": ("Corrosion", "100"),
}
LEVELS = ("Light", "Medium", "Heavy")
PERCENTAGE_KEY = re.compile(r"Warhead@(Flame|Chemical)_(Light|Medium|Heavy)_Percentage$")


def scaled_states(warhead):
	states = set()
	state = warhead.get("PhysicalStateName")
	if state:
		states.add(state)

	multiple = warhead.child("PhysicalStates")
	if multiple:
		states.update(child.key for child in multiple.children)

	return states


def state_scale(warhead, state):
	if warhead.get("PhysicalStateName") == state:
		return warhead.get("PhysicalStateScale")

	multiple = warhead.child("PhysicalStates")
	if multiple:
		child = multiple.child(state)
		if child:
			return child.value

	return None


def main() -> int:
	rs = Ruleset(find_repo_root())
	problems = []

	for family, (expected_state, expected_scale) in EXPECTED_PERCENTAGE_STATES.items():
		for level in LEVELS:
			tag = f"{family}_{level}"
			template_name = f"^Warhead_{tag}"
			template = rs.weapon(template_name)
			percentage = template.child(f"Warhead@{tag}_Percentage") if template else None
			if percentage is None:
				problems.append(f"{template_name}: missing percentage warhead")
				continue

			if percentage.value != "AreaDamagePercentage":
				problems.append(
					f"{template_name}: percentage warhead is {percentage.value}, expected AreaDamagePercentage")
			if expected_state not in scaled_states(percentage):
				problems.append(
					f"{template_name}: percentage warhead does not apply {expected_state}")
			if state_scale(percentage, expected_state) != expected_scale:
				problems.append(
					f"{template_name}: percentage warhead scale is not {expected_scale}")

	for weapon_name in sorted(rs.weapons, key=str.lower):
		if weapon_name.startswith("^"):
			continue

		weapon = rs.resolve_weapon(weapon_name)
		if weapon is None:
			continue

		damage_scaled = set()
		fixed = set()
		for warhead in weapon.children:
			match = PERCENTAGE_KEY.fullmatch(warhead.key)
			if match:
				family = match.group(1)
				expected_state, expected_scale = EXPECTED_PERCENTAGE_STATES[family]
				if (warhead.value != "AreaDamagePercentage"
						or expected_state not in scaled_states(warhead)
						or state_scale(warhead, expected_state) != expected_scale):
					problems.append(
						f"{weapon_name}: {warhead.key} does not feed {expected_state} through AreaDamagePercentage")

			if warhead.value in {"AreaDamage", "AreaDamagePercentage"}:
				damage_scaled.update(scaled_states(warhead))
			elif warhead.value == "ApplyPhysicalState":
				state = warhead.get("PhysicalStateName")
				if state:
					fixed.add(state)

		for state in sorted(damage_scaled & fixed):
			problems.append(
				f"{weapon_name}: combines damage-scaled and fixed ApplyPhysicalState for {state}")

	print("# Physical-state warhead audit\n")
	print(f"Active concrete weapons checked: {sum(not name.startswith('^') for name in rs.weapons)}")
	print(f"Formula percentage templates checked: {len(EXPECTED_PERCENTAGE_STATES) * len(LEVELS)}\n")
	if problems:
		print(f"## FAIL ({len(problems)} problem(s))\n")
		for problem in problems:
			print(f"- {problem}")
		return 1

	print("## PASS\n")
	print("- Flame and Chemical percentage damage feeds the matching physical-state meter.")
	print("- No active weapon double-applies a meter through scaled and fixed warheads.")
	return 0


if __name__ == "__main__":
	sys.exit(main())
