#!/usr/bin/env python3
"""Guard pre-3-way ``GlowImpact`` behavior across the active weapon manifest.

The weapon 3-way migration moved impact behavior from legacy full-stack weapon
templates into ``^Effect_*`` templates.  A resolver comparison is required:
text searches cannot see glow inherited through intermediate templates, and
renamed weapons must be compared to their replacement IDs.

By default this compares the working tree against the last revision before the
first 3-way retrofit.  It fails when an existing/renamed weapon loses the exact
``Warhead@Glow: GlowImpact`` node, or when a common ID unexpectedly gains it.

Usage:
    python tools/audit/audit_impact_glow_preservation.py
    python tools/audit/audit_impact_glow_preservation.py --details
    python tools/audit/audit_impact_glow_preservation.py --report-only
"""
from __future__ import annotations

import argparse
import posixpath
import subprocess
import sys
from pathlib import Path

from miniyaml import Node, Ruleset, _merge_into, _strip_comment, find_repo_root, merge_children


DEFAULT_BASELINE = "2d5dc10e92834c9357a853c938cfe8c8edbe9eb9"

# The baseline IDs disappeared during unrelated spelling/name cleanups.  Their
# replacements are the same behavioral weapons and must preserve the old glow.
RENAMES = {
	"JHindCannon": "SkyHawkCannon",
	"JHindPlasmaCannon": "SkyHawkPlasmaCannon",
	"RashinanGun_upgrade": "RashidanGun_upgrade",
}

PACKAGE_PREFIXES = {
	"cameo": "mods/cameo",
	"ContentPacks": "mods/cameo/ContentPacks",
	"common": "engine/mods/common",
}


def parse_text(text: str, source: str) -> list[Node]:
	"""Parse a MiniYAML blob using the same subset as tools/audit/miniyaml.py."""
	root: list[Node] = []
	stack: list[tuple[int, Node | None]] = [(-1, None)]
	for lineno, raw in enumerate(text.splitlines(), 1):
		stripped = _strip_comment(raw)
		if not stripped.strip():
			continue
		indent = len(stripped) - len(stripped.lstrip("\t "))
		body = stripped.strip()
		key, _, value = body.partition(":")
		node = Node(key.strip(), value.strip(), [], source, lineno)
		while stack and stack[-1][0] >= indent:
			stack.pop()
		parent = stack[-1][1]
		(parent.children if parent else root).append(node)
		stack.append((indent, node))
	return root


class GitWeaponRuleset:
	"""Active weapon manifest loaded directly from one Git revision."""

	def __init__(self, repo_root: Path, revision: str):
		self.repo_root = repo_root
		self.revision = revision
		self.manifest_documents: list[str] = []
		self.weapon_files: list[str] = []
		self.weapons = self._load_weapons()
		self._weapon_ci = {key.lower(): key for key in self.weapons}
		self._cache: dict[str, Node] = {}

	def _git_text(self, path: str) -> str:
		proc = subprocess.run(
			["git", "-C", str(self.repo_root), "show", f"{self.revision}:{path}"],
			stdout=subprocess.PIPE,
			stderr=subprocess.PIPE,
			check=False)
		if proc.returncode != 0:
			raise FileNotFoundError(path)
		return proc.stdout.decode("utf-8-sig", errors="replace")

	@staticmethod
	def _resolve_ref(ref: str) -> str | None:
		if "|" in ref:
			package, relative = ref.split("|", 1)
			base = PACKAGE_PREFIXES.get(package)
			if base is None:
				return None
			return posixpath.normpath(posixpath.join(base, relative))
		return posixpath.normpath(posixpath.join("mods/cameo", ref))

	def _load_weapons(self) -> dict[str, Node]:
		seen_manifests: set[str] = set()

		def absorb(path: str) -> None:
			if path in seen_manifests:
				return
			seen_manifests.add(path)
			try:
				document = parse_text(self._git_text(path), path)
			except FileNotFoundError:
				return
			self.manifest_documents.append(path)
			base_dir = posixpath.dirname(path)
			for top in document:
				if top.key == "Include" and top.value:
					include = (self._resolve_ref(top.value) if "|" in top.value else
						posixpath.normpath(posixpath.join(base_dir, top.value)))
					if include is not None:
						absorb(include)
					continue
				if top.key != "Weapons":
					continue
				for entry in top.children:
					ref = entry.key if not entry.value else f"{entry.key}:{entry.value}"
					weapon_file = self._resolve_ref(ref)
					if weapon_file is not None:
						self.weapon_files.append(weapon_file)

		absorb("mods/cameo/mod.yaml")
		merged: dict[str, Node] = {}
		for path in self.weapon_files:
			try:
				document = parse_text(self._git_text(path), path)
			except FileNotFoundError:
				continue
			for top in document:
				if top.key.startswith("-"):
					merged.pop(top.key[1:], None)
					continue
				if top.key in merged:
					previous = merged[top.key]
					previous.children = merge_children(previous.children, top.children)
					if top.value:
						previous.value = top.value
				else:
					merged[top.key] = top.deep_copy()
		return merged

	def weapon(self, name: str) -> Node | None:
		return self.weapons.get(name) or self.weapons.get(self._weapon_ci.get(name.lower(), ""))

	def resolve_weapon(self, name: str, stack: tuple[str, ...] = ()) -> Node | None:
		cache_key = name.lower()
		if cache_key in self._cache:
			return self._cache[cache_key]
		node = self.weapon(name)
		if node is None or cache_key in {item.lower() for item in stack}:
			return None
		children: list[Node] = []
		index: dict[str, Node] = {}
		for child in node.children:
			if child.key == "Inherits" or child.key.startswith("Inherits@"):
				parent = self.resolve_weapon(child.value, stack + (name,))
				if parent is not None:
					_merge_into(children, index, parent.children)
				continue
			_merge_into(children, index, [child])
		resolved = Node(node.key, node.value, children, node.file, node.line)
		self._cache[cache_key] = resolved
		return resolved


def has_impact_glow(node: Node | None) -> bool:
	return node is not None and any(
		child.key == "Warhead@Glow" and child.value == "GlowImpact"
		for child in node.children)


def glow_names(weapons: dict[str, Node], resolver) -> set[str]:
	return {name for name in weapons if has_impact_glow(resolver(name))}


def inheritance_path_counts(ruleset, target: str) -> dict[str, int]:
	"""Count raw inheritance paths to target for every active weapon/template."""
	memo: dict[str, int] = {}

	def count(name: str, stack: tuple[str, ...] = ()) -> int:
		key = name.lower()
		if key in memo:
			return memo[key]
		if key in {item.lower() for item in stack}:
			return 0
		node = ruleset.weapon(name)
		if node is None:
			return 0
		total = 0
		for child in node.children:
			if child.key != "Inherits" and not child.key.startswith("Inherits@"):
				continue
			if child.value == target:
				total += 1
			total += count(child.value, stack + (name,))
		memo[key] = total
		return total

	return {name: count(name) for name in ruleset.weapons}


def split_counts(names: set[str]) -> str:
	templates = sum(name.startswith("^") for name in names)
	return f"{len(names)} ({len(names) - templates} concrete, {templates} templates)"


def print_names(label: str, names: set[str], details: bool) -> None:
	print(f"{label}: {split_counts(names)}")
	if details and names:
		for name in sorted(names):
			print(f"  {name}")


def main() -> int:
	parser = argparse.ArgumentParser()
	parser.add_argument("--baseline", default=DEFAULT_BASELINE)
	parser.add_argument("--details", action="store_true")
	parser.add_argument(
		"--report-only", action="store_true",
		help="always return success while the repair is still in progress")
	args = parser.parse_args()

	root = find_repo_root()
	baseline = GitWeaponRuleset(root, args.baseline)
	current = Ruleset(root)
	baseline_glows = glow_names(baseline.weapons, baseline.resolve_weapon)
	current_glows = glow_names(current.weapons, current.resolve_weapon)

	expected: set[str] = set()
	unmapped_removals: set[str] = set()
	for name in baseline_glows:
		if name in current.weapons:
			expected.add(name)
			continue
		replacement = RENAMES.get(name)
		if replacement is not None and replacement in current.weapons:
			expected.add(replacement)
		else:
			unmapped_removals.add(name)

	common = set(baseline.weapons) & set(current.weapons)
	lost = expected - current_glows
	unexpected_common_gains = {
		name for name in common
		if name not in baseline_glows and name in current_glows
	}
	new_current_glows = current_glows - common - set(RENAMES.values())
	baseline_paths = inheritance_path_counts(baseline, "^ImpactGlow")
	current_paths = inheritance_path_counts(current, "^ImpactGlow")
	multiplicity_increases = {
		name for name in common
		if current_paths[name] > 1 and current_paths[name] > baseline_paths[name]
	}
	direct_inline_threeway: set[str] = set()
	legacy_inline: set[str] = set()
	for name, node in current.weapons.items():
		if name.startswith("^"):
			continue
		parents = [
			child.value for child in node.children
			if child.key == "Inherits" or child.key.startswith("Inherits@")
		]
		if "^ImpactGlow" not in parents:
			continue
		if any(parent.startswith("^Effect_") for parent in parents):
			direct_inline_threeway.add(name)
		else:
			legacy_inline.add(name)

	print("# Impact-glow preservation audit")
	print(f"baseline: {args.baseline}")
	print(f"baseline manifest documents: {len(baseline.manifest_documents)}")
	print(
		f"active weapon files: {len(set(baseline.weapon_files))} baseline / "
		f"{len(set(map(str, current.manifest.weapons)))} current")
	print(f"active IDs: {len(baseline.weapons)} baseline / {len(current.weapons)} current")
	print(f"resolved GlowImpact: {len(baseline_glows)} baseline / {len(current_glows)} current")
	print_names("lost expected glow", lost, args.details)
	print_names("unexpected common-ID gains", unexpected_common_gains, args.details)
	print_names("unmapped removed baseline glows", unmapped_removals, args.details)
	print_names("increased duplicate ^ImpactGlow paths", multiplicity_increases, args.details)
	print_names("three-way weapons with inline ^ImpactGlow", direct_inline_threeway, args.details)
	print_names("informational legacy inline glows", legacy_inline, args.details)
	print_names("informational new-ID glows", new_current_glows, args.details)

	failed = bool(
		lost or unexpected_common_gains or unmapped_removals or multiplicity_increases or
		direct_inline_threeway)
	print("result: " + ("FAIL" if failed else "PASS"))
	return 0 if args.report_only or not failed else 1


if __name__ == "__main__":
	sys.exit(main())
