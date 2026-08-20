#!/usr/bin/env python3
"""Protect the active directional nuclear-flash wiring from YAML cleanup regressions."""

from __future__ import annotations

import pathlib
import sys

from miniyaml import Ruleset


ROOT = pathlib.Path(__file__).resolve().parents[2]

EXPECTED = {
	"RAAtomic": {
		"Duration": "40",
		"Radius": "0.35",
		"Brightness": "1.3",
		"Darkness": "0.85",
		"MinimumExposure": "0.45",
	},
	"PulseMissile": {
		"Color": "32C8FF",
		"Duration": "40",
		"Radius": "0.35",
		"Brightness": "1.3",
		"Darkness": "0.85",
		"MinimumExposure": "0.45",
	},
	"CabalMagicNuke": {
		"Color": "32C8FF",
		"Duration": "40",
		"Radius": "0.35",
		"Brightness": "1.3",
		"Darkness": "0.85",
		"MinimumExposure": "0.45",
	},
}

LAUNCHERS = {
	"RAAtomic": "ra1_soviets_missilesilo",
	"PulseMissile": "ixian_supercomputer",
	"CabalMagicNuke": "cabal_core",
}

SOURCE_CONTRACTS = {
	"OpenRA.Mods.Cameo/Traits/World/NuclearFlashRenderer.cs": {
		"edge-projected renderer description":
			"Renders a nuclear exposure flash with edge-projected off-screen glow and readable global underexposure.",
		"off-screen detection":
			"var offscreen = x < 0f || x > fbWidth || y < 0f || y > fbHeight;",
		"nearest-edge horizontal projection":
			"x = Math.Clamp(x, 0f, fbWidth);",
		"nearest-edge vertical projection":
			"y = Math.Clamp(y, 0f, fbHeight);",
		"full-radius edge or corner glow":
			'shader.SetVec("LightRadius", Math.Min(fbWidth, fbHeight) * radius);',
		"full-strength edge or corner glow":
			'shader.SetVec("Brightness", brightness * strength);',
		"minimum exposure shader binding":
			'shader.SetVec("MinimumExposure", minimumExposure);',
	},
	"engine/glsl/postprocess_nuclearflash.frag": {
		"global readable underexposure":
			"float retainedExposure = max(1.0 - Darkness, MinimumExposure);",
		"non-saturating directional glow":
			"float flashStrength = 1.0 - exp(-Brightness * lightFalloff);",
	},
}


def location(node) -> str:
	return f"{pathlib.Path(node.file).relative_to(ROOT)}:{node.line}"


def main() -> int:
	ruleset = Ruleset(ROOT)
	failures: list[str] = []

	for relative_path, contracts in SOURCE_CONTRACTS.items():
		source = (ROOT / relative_path).read_text(encoding="utf-8")
		for description, required_text in contracts.items():
			if required_text not in source:
				failures.append(f"{relative_path}: missing {description}")

	world = ruleset.resolve("World")
	if world is None or not any(c.key == "NuclearFlashRenderer" for c in world.children):
		failures.append("World is missing the active NuclearFlashRenderer trait")

	ra_atomic_source = ruleset.weapon("RAAtomic")
	if ra_atomic_source is None:
		failures.append("RAAtomic is missing from the active mod.yaml include graph")
	else:
		inherits = [target for _, target in ruleset.inherits_of(ra_atomic_source)]
		if "^AtomicCore" not in inherits:
			failures.append("RAAtomic must inherit ^AtomicCore directly")
		if any(c.key.startswith("-Warhead@") for c in ra_atomic_source.children):
			failures.append("RAAtomic must not use regex-fragile negative warhead removals")

	for weapon_name, expected_fields in EXPECTED.items():
		launcher_name = LAUNCHERS[weapon_name]
		launcher = ruleset.resolve(launcher_name)
		power = launcher.child("NukePowerCA") if launcher is not None else None
		missile_weapons = power.child("MissileWeapons") if power is not None else None
		if (power is None or missile_weapons is None
				or not any(c.value.lower() == weapon_name.lower() for c in missile_weapons.children)):
			failures.append(
				f"{launcher_name}: active NukePowerCA must launch {weapon_name} "
				"through MissileWeapons")

		weapon = ruleset.resolve_weapon(weapon_name)
		if weapon is None:
			failures.append(f"{weapon_name}: weapon is missing from the active mod.yaml include graph")
			continue

		flashes = [
			child for child in weapon.children
			if child.key.startswith("Warhead@") and child.value == "NuclearFlashEffect"
		]
		if len(flashes) != 1:
			failures.append(
				f"{weapon_name} ({location(weapon)}): expected exactly one "
				f"NuclearFlashEffect warhead, found {len(flashes)}")
			continue

		flash = flashes[0]
		for field, expected in expected_fields.items():
			actual_node = flash.child(field)
			actual = actual_node.value if actual_node is not None else None
			if actual != expected:
				failures.append(
					f"{weapon_name} ({location(flash)}): {field} must be {expected}, "
					f"found {actual!r}")

	if failures:
		print("FAIL: directional nuclear-flash contract")
		for failure in failures:
			print(f"  - {failure}")
		return 1

	print("PASS: RA1, Ixian, and CABAL launchers retain their active nuclear flashes")
	return 0


if __name__ == "__main__":
	sys.exit(main())
