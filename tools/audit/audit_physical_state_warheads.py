#!/usr/bin/env python3
"""Physical-state meters: check the FOLDED warhead, and reject double-application.

The Formula V2 Flame and Chemical families raise Temperature/Corrosion in proportion to actual
damage. Two checks:

  1. Each `^Warhead_{Flame,Chemical}_{Light,Medium,Heavy}` main warhead carries its meter AND a
     non-zero `PercentageScale`, so the percentage component reaches the same meter.
  2. No resolved weapon combines a damage-scaled meter with a legacy fixed `ApplyPhysicalState`
     for that same meter — that is a double-application and it fills the bar twice as fast.

⛔ CHECK (1) USED TO LOOK FOR A SEPARATE `Warhead@<tag>_Percentage` TWIN, AND THAT WAS STALE.
The AreaDamage fold put flat damage, the percentage component and friendly fire into ONE warhead:
`PercentageScale` / `PercentageSpread` / `PercentageVersus` and `FriendlyFireDamage` /
`FriendlyFireSpread` are fields on `AreaDamageWarhead` itself. There are no twins any more, so the
audit reported all six templates as "missing percentage warhead" against a structure the design had
retired — six false failures that turned the whole suite red. Corrected 2026-08-24 after the
maintainer caught it. Verify the shape before trusting a count:

    Warhead@Flame_Light: AreaDamage
        Damage: 2000  Spread: 200  Falloff: ...    <- flat
        PercentageScale: 10000  PercentageSpread: 50 <- percentage, folded in
        FriendlyFireDamage: 50  FriendlyFireSpread: 50
        PhysicalStateName: Temperature  PhysicalStateScale: 100

⚠ The meter comes in TWO forms and both are legal: Flame uses the singular
`PhysicalStateName`/`PhysicalStateScale`, Chemical uses the `PhysicalStates:` MAP (blend families
emit the map). `scaled_states()` and `state_scale()` read both — never grep for one.
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


def folded_percentage_problems(warhead, expected_state, expected_scale):
	"""Return contract failures for a folded percentage-damage family node."""
	problems = []
	if warhead.value != "AreaDamage":
		problems.append(f"main warhead is {warhead.value}, expected AreaDamage")

	try:
		percentage_scale = int(warhead.get("PercentageScale") or "0")
	except ValueError:
		percentage_scale = 0

	if percentage_scale <= 0:
		problems.append("main warhead does not enable folded percentage damage")
	if expected_state not in scaled_states(warhead):
		problems.append(f"main warhead does not apply {expected_state}")
	if state_scale(warhead, expected_state) != expected_scale:
		problems.append(f"main warhead {expected_state} scale is not {expected_scale}")

	multiple = warhead.child("PhysicalStates")
	if (warhead.get("PhysicalStateName") == expected_state
			and multiple is not None and multiple.child(expected_state) is not None):
		problems.append(
			f"main warhead applies {expected_state} through both PhysicalStateName and PhysicalStates")

	return problems


def main() -> int:
	rs = Ruleset(find_repo_root())
	problems = []

	for family, (expected_state, expected_scale) in EXPECTED_PERCENTAGE_STATES.items():
		for level in LEVELS:
			tag = f"{family}_{level}"
			template_name = f"^Warhead_{tag}"
			# Resolve inheritance: the meter and percentage fields may come from a parent.
			template = rs.resolve_weapon(template_name)
			main_warhead = template.child(f"Warhead@{tag}") if template else None
			if main_warhead is None:
				problems.append(f"{template_name}: missing main warhead")
				continue

			for problem in folded_percentage_problems(main_warhead, expected_state, expected_scale):
				problems.append(f"{template_name}: {problem}")

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
				problems.append(
					f"{weapon_name}: obsolete {warhead.key} duplicates folded percentage damage")

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
	print(f"Formula folded templates checked: {len(EXPECTED_PERCENTAGE_STATES) * len(LEVELS)}\n")
	if problems:
		print(f"## FAIL ({len(problems)} problem(s))\n")
		for problem in problems:
			print(f"- {problem}")
		return 1

	print("## PASS\n")
	print("- Flame and Chemical folded percentage damage feeds the matching physical-state meter.")
	print("- No active weapon double-applies a meter through scaled and fixed warheads.")
	return 0


if __name__ == "__main__":
	sys.exit(main())
