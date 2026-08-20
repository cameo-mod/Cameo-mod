#!/usr/bin/env python3
"""rename_aa_weapons.py — Standardize AA-only weapon names to the `_AA`
suffix convention (DESIGN.md §1), safely.

SAFETY DESIGN (this is a rewrite after a prior version corrupted unrelated
Tooltip/Name/RequiresCondition/Prerequisite text by doing a blind,
file-wide word-boundary substitution of the bare weapon name — e.g.
renaming "Dragon" -> "Dragon_AA" also mangled "Way of the Dragon" tooltip
text and an unrelated Warcraft2 "Dragon Roost" building name):

Phase 1 (identify): scan every top-level YAML block; only blocks that
pass `is_weapon_definition_body()` (i.e. actually look like weapon defs —
have ReloadDelay/Projectile/Warhead/etc. and NOT Tooltip/Buildable/
Armament/RenderSprites/etc.) are considered weapons. From this set, find
AA-only weapons (ValidTargets includes Air, not dual-purpose, no legacy
AA keyword) whose name doesn't already end `_AA`/`_aa` or contain `_AA_`.

Phase 2 (apply): renames are applied ONLY to structurally-identified
weapon-name positions:
  - the top-level definition key itself (active or commented-out)
  - the value of any `*Weapon:` field (Weapon, EmptyWeapon, DetonateWeapon, ...)
  - each CSV token in any `*Weapons:` field (ThrowsShrapnel, FireProjectilesOnDeath, ...)
  - indexed superweapon lists (`Weapons:` followed by `N: <weapon>` children)
  - the value of an `Inherits:` field (weapon-to-weapon inheritance)
All matches require an EXACT full-token match against a known old weapon
name — never a substring/word-boundary match against arbitrary text. This
guarantees Tooltip Name:, RequiresCondition:, Prerequisite:,
ProvidesPrerequisite:, Armament Name:, and comments are never touched
unless they are themselves one of the exact structural fields above.
"""

import os
import re
import sys

ROOT = sys.argv[1] if len(sys.argv) > 1 else "mods/cameo"
DRY_RUN = "--apply" not in sys.argv

# ── Shared detection logic (mirrors tools/audit/audit_weapon_suffixes.py) ──

# NOTE: bare "aa" is intentionally excluded from this list — it would
# incorrectly exclude every X3b/X3c weapon (those that already contain
# "AA" as a substring but lack the underscore, which is exactly what we
# need to fix). Only real legacy naming keywords are listed here.
AA_LEGACY_KEYWORDS_STRICT = ("flak", "sam", "interceptor", "patriot",
                             "aagun", "aamissile", "aafortress")

DUAL_PURPOSE_TARGETS = ("Ground", "Water", "Infantry", "Vehicle", "Building",
                        "Defense", "Structure", "Ship")

WEAPON_MARKER_KEYS = (
    "ReloadDelay:", "Projectile:", "Warhead", "MinRange:", "ValidTargets:",
    "Report:", "Range:", "Burst:", "TargetActorCenter:", "Inaccuracy:",
)

NON_WEAPON_MARKER_KEYS = (
    "Tooltip:", "Buildable:", "RenderSprites", "Health:",
    "Mobile:", "Armament", "Filename:", "Facings:",
)


def is_weapon_definition_body(body_lines: list) -> bool:
    has_weapon_marker = False
    for bl in body_lines:
        stripped = bl.strip()
        if any(stripped.startswith(k) for k in NON_WEAPON_MARKER_KEYS):
            return False
        if any(stripped.startswith(k) for k in WEAPON_MARKER_KEYS):
            has_weapon_marker = True
    return has_weapon_marker


def classify_block(body_lines: list) -> str:
    """Three-way classification: 'weapon' (has a direct weapon marker),
    'nonweapon' (has a direct non-weapon marker), or 'unknown' (neither —
    a pure-inheritance block with zero own fields, e.g. a weapon variant
    that only combines templates via multiple `Inherits:` lines, like
    `ArmoredCarMGAAWaveforce: Inherits: ^HeavyAAWeapon / Inherits:
    ArmoredCarMGAA / ...`)."""
    has_weapon_marker = False
    for bl in body_lines:
        stripped = bl.strip()
        if any(stripped.startswith(k) for k in NON_WEAPON_MARKER_KEYS):
            return "nonweapon"
        if any(stripped.startswith(k) for k in WEAPON_MARKER_KEYS):
            has_weapon_marker = True
    return "weapon" if has_weapon_marker else "unknown"


def compute_new_name(name: str) -> str:
    """Compute the _AA-suffixed name for a weapon, enforcing the canonical
    `<base>_<doctrine/variant>_EMP_AA_elite` ordering (DESIGN.md §1) rather
    than blindly preserving wherever "AA" already sits in the name."""
    elite_suffix = ""
    base = name
    if base.endswith("_elite"):
        elite_suffix = "_elite"
        base = base[:-len("_elite")]

    # Pull out an existing _EMP suffix (if present) so it can be
    # re-emitted in the correct position (before _AA) regardless of
    # where it originally sat relative to the AA marker.
    emp_suffix = ""
    if base.endswith("_EMP"):
        emp_suffix = "_EMP"
        base = base[:-len("_EMP")]
    elif base.endswith("_emp"):
        emp_suffix = "_EMP"
        base = base[:-len("_emp")]

    # Already has a proper _AA marker somewhere — no change needed
    # (just re-append any _EMP/_elite suffix we split off above).
    if re.search(r"_AA($|_)", base) or re.search(r"_aa($|_)", base):
        return base + emp_suffix + elite_suffix

    # Case: "AA_" in the middle without a preceding underscore
    m = re.search(r"(?<!_)AA_", base)
    if m:
        new_base = base[:m.start()] + "_AA_" + base[m.end():]
        return new_base + emp_suffix + elite_suffix

    # Case: ends with AA/aa without a preceding underscore
    if base.endswith("AA") and not base.endswith("_AA"):
        return base[:-2] + "_AA" + emp_suffix + elite_suffix
    if base.endswith("aa") and not base.endswith("_aa"):
        return base[:-2] + "_AA" + emp_suffix + elite_suffix

    # Case: no AA marker at all — append
    return base + "_AA" + emp_suffix + elite_suffix


def parse_weapon_targets(root: str) -> dict:
    """Scan every weapon definition and return {name: [effective ValidTargets]}.
    Many weapons don't declare ValidTargets directly and instead inherit it
    from a ^Template (e.g. TSMechRailgun: Inherits: ^RailgunWeapon), so this
    resolves ValidTargets through the full Inherits chain."""
    direct_targets = {}   # name -> [ValidTargets] declared directly on this block
    inherits_of = {}      # name -> [parent names] (weapons AND templates)
    weapon_names = set()  # names confirmed to be genuine weapon definitions

    for dirpath, _, filenames in os.walk(root):
        for fn in filenames:
            if not fn.endswith(".yaml"):
                continue
            fpath = os.path.join(dirpath, fn)
            try:
                lines = open(fpath, encoding="utf-8").readlines()
            except Exception:
                continue

            i = 0
            while i < len(lines):
                line = lines[i]
                wm = re.match(r'^([A-Za-z^][A-Za-z0-9_.\-]*):\s*$', line)
                if wm and not line.startswith('\t') and not line.strip().startswith('#'):
                    block_name = wm.group(1)
                    j = i + 1
                    valid_targets = []
                    parents = []
                    body_lines = []
                    while j < len(lines):
                        bl = lines[j]
                        if re.match(r'^[^\t\s]', bl) and not bl.strip().startswith('#'):
                            break
                        body_lines.append(bl)
                        vtm = re.match(r'^\tValidTargets:\s*(.+)', bl)
                        if vtm:
                            valid_targets = [t.strip() for t in vtm.group(1).split(",")]
                        im = re.match(r'^\tInherits(?:@\w+)?:\s*(\S+)\s*$', bl)
                        if im:
                            parents.append(im.group(1))
                        j += 1
                    if valid_targets:
                        direct_targets[block_name] = valid_targets
                    if parents:
                        inherits_of[block_name] = parents
                    if not block_name.startswith('^') and is_weapon_definition_body(body_lines):
                        weapon_names.add(block_name)
                i += 1

    def resolve(name: str, visited: set) -> list:
        if name in visited:
            return []
        visited.add(name)
        if name in direct_targets:
            return direct_targets[name]
        for parent in inherits_of.get(name, []):
            resolved = resolve(parent, visited)
            if resolved:
                return resolved
        return []

    return {name: resolve(name, set()) for name in weapon_names}


ARMAMENT_HEADER_RE = re.compile(r'^\tArmament(@\w+)?:\s*$')
ARMAMENT_WEAPON_RE = re.compile(r'^\t\tWeapon:\s*(\S+)\s*$')


def find_actor_paired_air_only_weapons(root: str, weapon_targets: dict) -> set:
    """Find weapon names that are used via an Armament trait on some
    actor/template block that ALSO equips (via a different Armament trait)
    a ground-capable weapon. This is the actual `_AA` convention: it marks
    the air-only sibling of a dual-weapon actor (e.g. an Anti-Air Tank
    with both a ground cannon and an AA missile), NOT any weapon that
    merely happens to target only Air (e.g. a SAM Site with a single,
    standalone AA weapon)."""
    paired = set()
    for dirpath, _, filenames in os.walk(root):
        for fn in filenames:
            if not fn.endswith(".yaml"):
                continue
            fpath = os.path.join(dirpath, fn)
            try:
                lines = open(fpath, encoding="utf-8").readlines()
            except Exception:
                continue

            i = 0
            while i < len(lines):
                line = lines[i]
                bm = re.match(r'^([A-Za-z^][A-Za-z0-9_.\-]*):\s*$', line)
                if bm and not line.startswith('\t') and not line.strip().startswith('#'):
                    j = i + 1
                    block_weapons = []
                    while j < len(lines):
                        bl = lines[j]
                        if re.match(r'^[^\t\s]', bl) and not bl.strip().startswith('#'):
                            break
                        if ARMAMENT_HEADER_RE.match(bl):
                            k = j + 1
                            while k < len(lines) and re.match(r'^\t\t\S', lines[k]):
                                wmatch = ARMAMENT_WEAPON_RE.match(lines[k])
                                if wmatch:
                                    block_weapons.append(wmatch.group(1))
                                k += 1
                        j += 1

                    air_only = []
                    ground_capable = []
                    for w in block_weapons:
                        targets = weapon_targets.get(w)
                        if targets is None:
                            continue
                        is_dual = any(t in targets for t in DUAL_PURPOSE_TARGETS)
                        if "Air" in targets and not is_dual:
                            air_only.append(w)
                        elif "Ground" in targets:
                            ground_capable.append(w)
                    if air_only and ground_capable:
                        paired.update(air_only)
                    i = j
                else:
                    i += 1
    return paired


def find_aa_weapons_needing_rename(root: str) -> dict:
    """Phase 1: identify AA-only weapons violating the _AA convention.
    Only weapons that are the air-only sibling of a dual-weapon actor
    (see find_actor_paired_air_only_weapons) qualify — standalone AA-only
    weapons (SAM Sites, dedicated AA turrets with a single weapon) are
    intentionally excluded per the documented convention."""
    weapon_targets = parse_weapon_targets(root)
    paired_air_only = find_actor_paired_air_only_weapons(root, weapon_targets)

    rename_map = {}
    for weapon_name in paired_air_only:
        has_legacy = any(kw in weapon_name.lower() for kw in AA_LEGACY_KEYWORDS_STRICT)
        if has_legacy:
            continue
        if weapon_name.endswith('_AA') or weapon_name.endswith('_aa') \
                or '_AA_' in weapon_name or '_aa_' in weapon_name:
            continue
        new_name = compute_new_name(weapon_name)
        if new_name != weapon_name:
            rename_map[weapon_name] = new_name
    return rename_map


# ── Phase 2: structurally-scoped, exact-match application ──────────────

TOP_LEVEL_KEY_RE = re.compile(r'^(#*\s*)([A-Za-z][A-Za-z0-9_.\-]*):\s*(#.*)?$')

# Nested-field prefix: this codebase places the comment marker BEFORE the
# indentation tabs when disabling a field (e.g. "# \t\tWeapon: Foo"), not
# after. Support both orderings to be safe.
_FIELD_PREFIX = r'^(#\s*)?(\t+)(?:#\s*)?'
FIELD_SINGLE_RE = re.compile(_FIELD_PREFIX + r'([A-Za-z]*Weapon|Inherits(?:@\w+)?):\s*(\S+)(\s*#.*)?\s*$')
FIELD_PLURAL_RE = re.compile(r'^((?:#\s*)?\t+(?:#\s*)?[A-Za-z]*Weapons:\s*)(.+?)(\s*#.*)?\s*$')
INDEXED_CHILD_RE = re.compile(_FIELD_PREFIX + r'(\d+):\s*(\S+)(\s*#.*)?\s*$')
BARE_FIELD_RE = re.compile(_FIELD_PREFIX + r'([A-Za-z]*Weapons?):\s*$')


def compute_block_weapon_flags(lines: list, known_weapon_names: set) -> list:
    """For every line, determine whether it belongs to a top-level block
    that is a genuine weapon definition. Needed because some content packs
    (e.g. SOW) reuse the exact same identifier for an actor, a weapon, AND
    a sequence (e.g. `sow_mech_avenger`), so `Inherits:` references must
    only be renamed when they're genuinely inside a weapon body.

    Blocks with NO direct marker of either kind ('unknown' — a pure
    multi-`Inherits:` combination, e.g. `ArmoredCarMGAAWaveforce:
    Inherits: ^HeavyAAWeapon / Inherits: ArmoredCarMGAA / ...`) are
    resolved by checking whether any of their `Inherits:` targets is a
    name in `known_weapon_names` (built from every weapon body found
    anywhere in the tree) — if so, the block is a weapon too. This
    prevents dangling `Inherits:` references after a rename (see
    docs/LESSONS_LEARNED.md § Bulk YAML rename scripts)."""
    flags = [False] * len(lines)
    idx = 0
    while idx < len(lines):
        line = lines[idx]
        m = TOP_LEVEL_KEY_RE.match(line)
        if m:
            is_comment = bool(m.group(1).strip())
            body_lines = []
            parents = []
            k = idx + 1
            while k < len(lines):
                bl = lines[k]
                if is_comment:
                    if not bl.strip().startswith('#'):
                        break
                else:
                    if re.match(r'^[^\t\s]', bl) and not bl.strip().startswith('#'):
                        break
                stripped_bl = bl.lstrip('#') if is_comment else bl
                body_lines.append(stripped_bl)
                im = re.match(r'^\s*Inherits(?:@\w+)?:\s*(\S+)\s*$', stripped_bl)
                if im:
                    parents.append(im.group(1))
                k += 1
            classification = classify_block(body_lines)
            if classification == "unknown":
                block_is_weapon = any(p in known_weapon_names for p in parents)
            else:
                block_is_weapon = classification == "weapon"
            for li in range(idx, k):
                flags[li] = block_is_weapon
            idx = k
        else:
            idx += 1
    return flags


def apply_renames(root: str, rename_map: dict, known_weapon_names: set) -> int:
    total_changes = 0
    for dirpath, _, filenames in os.walk(root):
        for fn in filenames:
            if not fn.endswith(".yaml"):
                continue
            fpath = os.path.join(dirpath, fn)
            try:
                lines = open(fpath, encoding="utf-8").readlines()
            except Exception:
                continue

            block_is_weapon = compute_block_weapon_flags(lines, known_weapon_names)
            changed = False
            in_indexed_weapons_block = False
            indexed_block_indent = None

            for idx, line in enumerate(lines):
                # Top-level definition key (active or commented-out).
                # Only rename if this specific block is actually a weapon
                # definition — the same identifier can exist in a
                # different namespace (e.g. a sequence or actor sharing
                # the same name as a weapon, like `d2k_aircraft_eater`
                # or `sow_mech_avenger`).
                m = TOP_LEVEL_KEY_RE.match(line)
                if m and m.group(2) in rename_map:
                    if block_is_weapon[idx]:
                        new_line = line.replace(m.group(2) + ":", rename_map[m.group(2)] + ":", 1)
                        if new_line != line:
                            lines[idx] = new_line
                            changed = True
                            total_changes += 1
                    continue

                # Bare "Weapons:" (no inline value) — start of an indexed
                # superweapon list; track indent to scope child lines
                bm = BARE_FIELD_RE.match(line)
                if bm and bm.group(3).endswith("Weapons"):
                    in_indexed_weapons_block = True
                    indexed_block_indent = len(bm.group(2))
                    continue

                # Indexed child of a superweapon Weapons: list
                if in_indexed_weapons_block:
                    im = INDEXED_CHILD_RE.match(line)
                    if im and len(im.group(2)) > indexed_block_indent and im.group(4) in rename_map:
                        old_val = im.group(4)
                        new_line = line.replace(old_val, rename_map[old_val], 1)
                        lines[idx] = new_line
                        changed = True
                        total_changes += 1
                        continue
                    elif not (im and len(im.group(2)) > indexed_block_indent):
                        in_indexed_weapons_block = False

                # Single-value field: Weapon:/EmptyWeapon:/.../Inherits:
                # NOTE: Weapon*: field names are unambiguous per OpenRA's
                # schema (always reference a weapon), but Inherits: is
                # ambiguous (actors and weapons both use it) — gate that
                # one on block_is_weapon.
                sm = FIELD_SINGLE_RE.match(line)
                if sm and sm.group(4) in rename_map:
                    field_name = sm.group(3)
                    is_inherits = field_name.startswith("Inherits")
                    if not is_inherits or block_is_weapon[idx]:
                        old_val = sm.group(4)
                        new_line = line.replace(old_val, rename_map[old_val], 1)
                        if new_line != line:
                            lines[idx] = new_line
                            changed = True
                            total_changes += 1
                    continue

                # Plural CSV field: Weapons:/ThrowsShrapnel Weapons:/...
                pm = FIELD_PLURAL_RE.match(line)
                if pm:
                    tokens = [t.strip() for t in pm.group(2).split(",")]
                    new_tokens = [rename_map.get(t, t) for t in tokens]
                    if new_tokens != tokens:
                        prefix = pm.group(1)
                        suffix = pm.group(3) or ""
                        lines[idx] = prefix + ", ".join(new_tokens) + suffix + "\n"
                        changed = True
                        total_changes += 1
                    continue

            if changed and not DRY_RUN:
                with open(fpath, "w", encoding="utf-8", newline="") as f:
                    f.writelines(lines)

    return total_changes


def main() -> int:
    rename_map = find_aa_weapons_needing_rename(ROOT)
    if not rename_map:
        print("No AA weapons need renaming.")
        return 0

    print(f"# AA weapon rename plan ({len(rename_map)} weapons)\n")
    for old, new in sorted(rename_map.items()):
        print(f"  {old} -> {new}")

    known_weapon_names = set(parse_weapon_targets(ROOT).keys())
    total_changes = apply_renames(ROOT, rename_map, known_weapon_names)
    print(f"\n{'[DRY RUN] Would apply' if DRY_RUN else 'Applied'} {total_changes} line changes.")
    if DRY_RUN:
        print("Re-run with --apply to write changes.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
