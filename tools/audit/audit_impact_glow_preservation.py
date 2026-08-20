#!/usr/bin/env python3
"""Enforce universal glow coverage for sprite-backed weapon effects.

Policy:

* Every resolved ``^Effect*`` template with a sprite-bearing ``CreateEffect``
  warhead (``Explosions`` or ``Image``) resolves exactly one
  ``Warhead@Glow: GlowImpact``.
* Effect templates without an impact sprite do not resolve impact glow.
* Every sprite-backed effect reaches exactly one light/medium/heavy glow tier.
* Converted three-way weapons select their glow through the effect layer, not
  through a fourth inline ``^ImpactGlow`` inherit.
"""
from __future__ import annotations

import argparse
import sys

from miniyaml import Ruleset, find_repo_root


GLOW_TIERS = {
	"^ImpactGlow_Light": ("0.3", "6"),
	"^ImpactGlow_Medium": ("0.55", "10"),
	"^ImpactGlow": ("0.8", "15"),
}


def direct_parents(ruleset: Ruleset, name: str) -> list[str]:
	node = ruleset.weapon(name)
	if node is None:
		return []
	return [
		child.value for child in node.children
		if child.key == "Inherits" or child.key.startswith("Inherits@")
	]


def has_impact_sprite(node) -> bool:
	return node is not None and any(
		child.key.startswith("Warhead@") and child.value == "CreateEffect" and
		(child.get("Explosions") is not None or child.get("Image") is not None)
		for child in node.children)


def has_impact_glow(node) -> bool:
	return node is not None and any(
		child.key == "Warhead@Glow" and child.value == "GlowImpact"
		for child in node.children)


def tier_path_count(
		ruleset: Ruleset, name: str, memo: dict[str, int], stack: tuple[str, ...] = ()) -> int:
	key = name.lower()
	if key in memo:
		return memo[key]
	if key in {item.lower() for item in stack}:
		return 0
	total = 0
	for parent in direct_parents(ruleset, name):
		if parent in GLOW_TIERS:
			total += 1
		total += tier_path_count(ruleset, parent, memo, stack + (name,))
	memo[key] = total
	return total


def show(label: str, names: set[str], details: bool) -> None:
	print(f"{label}: {len(names)}")
	if details:
		for name in sorted(names):
			print(f"  {name}")


def main() -> int:
	parser = argparse.ArgumentParser()
	parser.add_argument("--details", action="store_true")
	args = parser.parse_args()

	ruleset = Ruleset(find_repo_root())
	effects = sorted(name for name in ruleset.weapons if name.startswith("^Effect"))
	resolved = {name: ruleset.resolve_weapon(name) for name in effects}
	sprite_effects = {name for name in effects if has_impact_sprite(resolved[name])}
	non_sprite_effects = set(effects) - sprite_effects
	glowing_effects = {name for name in effects if has_impact_glow(resolved[name])}

	missing_glow = sprite_effects - glowing_effects
	unexpected_glow = non_sprite_effects & glowing_effects
	tier_memo: dict[str, int] = {}
	bad_tier_paths = {
		name for name in effects
		if tier_path_count(ruleset, name, tier_memo) != (1 if name in sprite_effects else 0)
	}
	obsolete_variants = {
		name for name in effects if name.endswith("_Glow") or name.endswith("_NoGlow")
	}
	tier_roots = {
		tier: {name for name in effects if tier in direct_parents(ruleset, name)}
		for tier in GLOW_TIERS
	}

	inline_threeway: set[str] = set()
	legacy_inline: set[str] = set()
	for name, node in ruleset.weapons.items():
		if name.startswith("^"):
			continue
		parents = direct_parents(ruleset, name)
		if "^ImpactGlow" not in parents:
			continue
		if any(parent.startswith("^Effect") for parent in parents):
			inline_threeway.add(name)
		else:
			legacy_inline.add(name)

	bad_tier_config: set[str] = set()
	for tier, (scale, fade) in GLOW_TIERS.items():
		node = ruleset.resolve_weapon(tier)
		glow = None if node is None else next(
			(child for child in node.children if child.key == "Warhead@Glow"), None)
		if (glow is None or glow.value != "GlowImpact" or glow.get("Scale") != scale or
				glow.get("FadeFrames") != fade):
			bad_tier_config.add(tier)

	print("# Universal impact-glow coverage audit")
	print(f"active ^Effect* templates: {len(effects)}")
	print(f"sprite-backed effects: {len(sprite_effects)}")
	print(f"non-sprite effects: {len(non_sprite_effects)}")
	print(
		"root tier assignments: " + ", ".join(
			f"{tier}={len(names)}" for tier, names in tier_roots.items()))
	show("sprite effects missing glow", missing_glow, args.details)
	show("non-sprite effects with glow", unexpected_glow, args.details)
	show("effects with invalid tier-path count", bad_tier_paths, args.details)
	show("obsolete glow/no-glow effect variants", obsolete_variants, args.details)
	show("three-way weapons with inline ^ImpactGlow", inline_threeway, args.details)
	show("bad glow-tier configuration", bad_tier_config, args.details)
	show("informational non-sprite effects", non_sprite_effects, args.details)
	show("informational legacy inline glows", legacy_inline, args.details)

	failed = bool(
		missing_glow or unexpected_glow or bad_tier_paths or obsolete_variants or
		inline_threeway or bad_tier_config)
	print("result: " + ("FAIL" if failed else "PASS"))
	return 1 if failed else 0


if __name__ == "__main__":
	sys.exit(main())
