#!/usr/bin/env python3
"""audit_armament_naming.py — armament slot + FirepowerMultiplier audit.

Reports (never auto-renames):
- Armament slot naming convention violations (underscore separators,
  fixed order: base -> upgrade tags -> AA/AG -> ELITE last).
- Elite weapon names that do not end with _elite, gated by approved
  weapon lineages.
- Actor-local FirepowerMultiplier rule violations:
  * at most one unconditional actor-local multiplier per actor
  * its trait name must be FirepowerMultiplier@<full_actor_id>
  * it must appear below the last Armament block in the actor
- Conditional actor-local FirepowerMultiplier traits for manual review.
- Candidate multi-step weapon lineages for the approved registry.

Per DESIGN.md §1 and §16.3; conservative, review-driven output.
"""

from __future__ import annotations

import json
import pathlib
import re
import sys

ROOT = pathlib.Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "tools/audit"))
from cameo_model import Model  # noqa: E402

REGISTRY = pathlib.Path(__file__).resolve().parent / "approved_weapon_lineages.json"

# --------------------------------------------------------------------------- #
# Slot grammar helpers
# --------------------------------------------------------------------------- #

VALID_BASE_SLOTS = {"PRIMARY", "SECONDARY", "GARRISONED"}
TARGETING_TAGS = {"AA", "AG"}


def parse_slot(tag: str) -> list[str]:
    """Split an Armament@ tag into underscore-separated segments."""
    return [s for s in tag.split("_") if s]


def check_slot(tag: str, actor: str) -> list[str]:
    """Return human-readable findings for a single Armament@<tag> name."""
    issues: list[str] = []
    if not tag:
        # Bare Armament is shorthand for Armament@PRIMARY.
        return issues
    if "-" in tag:
        issues.append("contains hyphen; use underscores only")
    parts = parse_slot(tag)
    if not parts:
        return issues

    # Base slot check: first segment should be a canonical base, unless it is
    # itself a targeting tag (e.g. legacy AA/AG standalone slots).
    base = parts[0]
    if base not in VALID_BASE_SLOTS and base not in TARGETING_TAGS:
        issues.append(
            f"non-canonical base slot '{base}'; expected PRIMARY/SECONDARY/GARRISONED"
        )

    # Elite must be the final segment if present.
    if "ELITE" in parts and parts[-1] != "ELITE":
        issues.append("ELITE is not the last segment")

    # Targeting tags must appear after any upgrade/condition tags and before ELITE.
    for tgt in TARGETING_TAGS:
        if tgt in parts:
            idx = parts.index(tgt)
            after = parts[idx + 1:]
            if after and after != ["ELITE"]:
                issues.append(
                    f"targeting tag {tgt} is followed by non-elite segment(s): {after}"
                )

    return issues


# --------------------------------------------------------------------------- #
# Approved lineage helpers
# --------------------------------------------------------------------------- #


def load_registry() -> dict:
    if not REGISTRY.exists():
        return {"lineages": []}
    return json.loads(REGISTRY.read_text(encoding="utf-8"))


def approved_elite_weapons(registry: dict) -> set[tuple[str, str]]:
    """(actor_lowercase, weapon_lowercase) pairs approved as elite/final.

    If a lineage entry lists ``actors``, every listed actor is paired with
    every weapon in the chain. Otherwise the ``actor_family`` is used as a
    prefix stem.
    """
    out = set()
    for entry in registry.get("lineages", []):
        fam = (entry.get("actor_family") or "").lower()
        actors = entry.get("actors")
        if actors:
            actor_names = [a.lower() for a in actors]
        elif fam:
            actor_names = [fam]
        else:
            continue
        for key in ("base_weapon", "upgrade_weapon", "elite_weapon"):
            w = entry.get(key)
            if w:
                for an in actor_names:
                    out.add((an, w.lower()))
    return out


def approved_for_actor(actor: str, weapon: str, registry: dict) -> bool:
    """Whether weapon is part of an approved lineage for actor's family."""
    actor_lc = actor.lower()
    weapon_lc = weapon.lower()
    approved = approved_elite_weapons(registry)
    # Exact actor match
    if (actor_lc, weapon_lc) in approved:
        return True
    # Prefix stem match for entries without explicit actors list
    for fam, w in approved:
        if fam and actor_lc.startswith(fam) and w == weapon_lc:
            return True
    return False


# --------------------------------------------------------------------------- #
# FirepowerMultiplier helpers
# --------------------------------------------------------------------------- #


def local_firepower_traits(model: Model, actor: str):
    """Yield (trait_key, trait_node, file_path, line) for actor-local traits."""
    node = model.rs.actor(actor)
    if node is None:
        return
    for c in node.children:
        if c.key == "FirepowerMultiplier" or c.key.startswith("FirepowerMultiplier@"):
            yield c.key, c, pathlib.Path(c.file), c.line


def file_lines_after_last_armament(file_path: pathlib.Path, actor: str) -> int | None:
    """Return the line number after the last Armament@ block for actor in file."""
    if not file_path.exists():
        return None
    text = file_path.read_text(encoding="utf-8-sig", errors="replace")
    in_actor = False
    last_armament = 0
    for lineno, raw in enumerate(text.splitlines(), 1):
        stripped = raw.strip()
        if not stripped or stripped.startswith("#"):
            continue
        indent = len(raw) - len(raw.lstrip("\t "))
        if indent == 0 and stripped.endswith(":"):
            name = stripped[:-1].strip()
            in_actor = (name.lower() == actor.lower())
            if in_actor:
                last_armament = 0
            continue
        if not in_actor:
            continue
        if indent == 1 and (stripped.startswith("Armament") or stripped.startswith("Armament@")):
            last_armament = lineno
    return last_armament if last_armament else None


# --------------------------------------------------------------------------- #
# Candidate lineage discovery
# --------------------------------------------------------------------------- #

LINEAGE_SUFFIXES = ("_elite", "_EMP", "_emp", "EMP", "emp", "E")
NUMERIC_SUFFIX_RE = re.compile(r"(\d+)$")


def lineage_base_name(weapon: str) -> str | None:
    """Strip known progression suffixes to guess a base weapon name."""
    for suffix in LINEAGE_SUFFIXES:
        if weapon.endswith(suffix):
            return weapon[: -len(suffix)]
    m = NUMERIC_SUFFIX_RE.search(weapon)
    if m and m.start() > 0:
        return weapon[: m.start()]
    return None


def discover_candidate_lineages(model: Model) -> list[dict]:
    """Find actors with weapons that look like base/upgrade/elite progressions.

    Reports:
    - groups of 3+ weapons sharing a base prefix with numeric/upgrade/elite
      suffixes inside one actor;
    - groups of 2+ weapons where at least one is an EMP upgrade and at least
      one is elite (the user's core 3-step pattern);
    - shared cross-unit numeric progressions (e.g. Foo1, Foo2, Foo3 used by
      different actors).
    """
    candidates: list[dict] = []
    seen = set()

    def add(actor: str, base: str, items: list[tuple[str, str, str]]):
        # items: (slot, weapon, condition)
        if len(items) < 2:
            return
        key = (actor.lower(), base.lower(), tuple(sorted({w for _, w, _ in items})))
        if key in seen:
            return
        seen.add(key)
        # Order: shortest name first, numeric suffixes in numeric order
        def sort_key(item):
            _, w, _ = item
            m = NUMERIC_SUFFIX_RE.search(w)
            n = int(m.group(1)) if m else 9999
            return (len(w), n, w)
        ordered = sorted(items, key=sort_key)
        candidates.append({
            "actor": actor,
            "guessed_base": base,
            "weapons": [w for _, w, _ in ordered],
            "slots": [slot for slot, _, _ in ordered],
            "conditions": [cond for _, _, cond in ordered],
        })

    # --- within-actor progressions ---
    for actor in sorted(model.rs.actors):
        if actor.startswith("^"):
            continue
        res = model.rs.resolve(actor)
        if res is None:
            continue
        arm_weapons = []
        for arm in res.children_named("Armament"):
            w = arm.get("Weapon")
            if w:
                arm_weapons.append((arm.key, w, (arm.get("RequiresCondition") or "").lower()))
        # group by guessed base
        groups: dict[str, list[tuple[str, str, str]]] = {}
        for slot, w, cond in arm_weapons:
            base = lineage_base_name(w) or w
            groups.setdefault(base, []).append((slot, w, cond))
        for base, items in groups.items():
            distinct_weapons = {w for _, w, _ in items}
            has_numeric = any(NUMERIC_SUFFIX_RE.search(w) for _, w, _ in items)
            has_emp = any("emp" in w.lower() for _, w, _ in items)
            has_elite = any("elite" in w.lower() or w.endswith("E") for _, w, _ in items)
            # require 3+ weapons, or 2+ with EMP+elite, or 2+ numeric progression
            if len(distinct_weapons) >= 3 or (len(distinct_weapons) >= 2 and has_emp and has_elite) or (len(distinct_weapons) >= 2 and has_numeric):
                add(actor, base, items)

    # --- cross-unit numeric progressions ---
    weapon_users: dict[str, list[str]] = {}
    for actor in sorted(model.rs.actors):
        if actor.startswith("^"):
            continue
        res = model.rs.resolve(actor)
        if res is None:
            continue
        for arm in res.children_named("Armament"):
            w = arm.get("Weapon")
            if w:
                weapon_users.setdefault(w, []).append(actor)
    numeric_weapons = {w for w in weapon_users if NUMERIC_SUFFIX_RE.search(w)}
    base_to_numeric: dict[str, list[str]] = {}
    for w in numeric_weapons:
        base = NUMERIC_SUFFIX_RE.sub("", w)
        base_to_numeric.setdefault(base, []).append(w)
    for base, variants in base_to_numeric.items():
        if len(variants) < 2:
            continue
        users = {u for w in variants for u in weapon_users[w]}
        if len(users) > 1:
            candidates.append({
                "actor": ", ".join(sorted(users)),
                "guessed_base": base,
                "weapons": sorted(variants, key=lambda w: int(NUMERIC_SUFFIX_RE.search(w).group(1))),
                "slots": ["(cross-unit)" for _ in variants],
                "conditions": ["(cross-unit)" for _ in variants],
            })

    return sorted(candidates, key=lambda c: c["actor"])


# --------------------------------------------------------------------------- #
# Main audit
# --------------------------------------------------------------------------- #


def main() -> int:
    model = Model()
    registry = load_registry()

    slot_findings = []
    elite_findings = []
    fp_unconditional_findings = []
    fp_conditional_rows = []
    lineage_findings = []

    for actor in sorted(model.rs.actors):
        if actor.startswith("^"):
            continue
        res = model.rs.resolve(actor)
        if res is None:
            continue

        # ---- armament slot naming + elite weapon naming ------------------- #
        arms = res.children_named("Armament")
        for arm in arms:
            tag = arm.key.split("@", 1)[1] if "@" in arm.key else ""
            issues = check_slot(tag, actor)
            if issues:
                slot_findings.append({
                    "actor": actor,
                    "slot": arm.key,
                    "weapon": arm.get("Weapon") or "",
                    "file": arm.file,
                    "line": arm.line,
                    "issues": "; ".join(issues),
                })

            # Elite-gated armament: weapon must end with _elite unless approved.
            cond = (arm.get("RequiresCondition") or "").lower()
            weapon = arm.get("Weapon") or ""
            if "rank-elite" in cond and weapon and not weapon.endswith("_elite"):
                if not approved_for_actor(actor, weapon, registry):
                    elite_findings.append({
                        "actor": actor,
                        "slot": arm.key,
                        "weapon": weapon,
                        "file": arm.file,
                        "line": arm.line,
                        "issue": "elite-gated weapon does not end with _elite",
                    })

        # ---- FirepowerMultiplier rules ------------------------------------ #
        local_traits = list(local_firepower_traits(model, actor))
        local_uncond = [
            (k, n, f, l) for k, n, f, l in local_traits
            if not n.get("RequiresCondition")
        ]
        local_cond = [
            (k, n, f, l) for k, n, f, l in local_traits
            if n.get("RequiresCondition")
        ]

        if len(local_uncond) > 1:
            fp_unconditional_findings.append({
                "actor": actor,
                "issue": f"{len(local_uncond)} unconditional actor-local FirepowerMultiplier traits (max 1)",
                "traits": [k for k, _, _, _ in local_uncond],
            })

        for k, n, f, l in local_uncond:
            expected = f"FirepowerMultiplier@{actor}"
            if k != expected:
                fp_unconditional_findings.append({
                    "actor": actor,
                    "trait": k,
                    "file": str(f),
                    "line": l,
                    "issue": f"unconditional multiplier must be named {expected}",
                })
            after = file_lines_after_last_armament(f, actor)
            if after is not None and l <= after:
                fp_unconditional_findings.append({
                    "actor": actor,
                    "trait": k,
                    "file": str(f),
                    "line": l,
                    "issue": f"unconditional multiplier is not below the last Armament block (last armament at line {after})",
                })

        for k, n, f, l in local_cond:
            fp_conditional_rows.append({
                "actor": actor,
                "multiplier": k,
                "modifier": n.get("Modifier") or "",
                "condition": n.get("RequiresCondition") or "",
                "proposed_role": "",
            })

    # ---- candidate lineages ----------------------------------------------- #
    approved_pairs = approved_elite_weapons(registry)
    for cand in discover_candidate_lineages(model):
        # Skip trivial AA twin pairs (e.g. Foo and Foo_AA)
        if len(cand["weapons"]) == 2 and any(w.endswith("_AA") for w in cand["weapons"]):
            continue
        # Skip if every weapon in this candidate is already approved for the actor
        actors = [a.strip() for a in cand["actor"].split(",")]
        if all(
            (a.lower(), w.lower()) in approved_pairs
            for a in actors
            for w in cand["weapons"]
        ):
            continue
        lineage_findings.append(cand)

    # ---- print report ----------------------------------------------------- #
    print("# Armament naming + FirepowerMultiplier audit\n")

    print(f"Armament slot naming findings: **{len(slot_findings)}**")
    print(f"Elite weapon naming findings: **{len(elite_findings)}**")
    print(f"Unconditional FirepowerMultiplier rule findings: **{len(fp_unconditional_findings)}**")
    print(f"Conditional FirepowerMultiplier traits to classify: **{len(fp_conditional_rows)}**")
    print(f"Candidate weapon lineages to review: **{len(lineage_findings)}**\n")

    if slot_findings:
        print("## Armament slot naming convention (underscore grammar)")
        print("| Actor | Slot | Weapon | File | Line | Issues |")
        print("|---|---|---|---|---|---|")
        for r in sorted(slot_findings, key=lambda x: (x["actor"], x["line"])):
            short = str(r["file"]).replace("\\", "/").replace("mods/cameo/", "")
            print(f"| {r['actor']} | {r['slot']} | {r['weapon']} | {short} | {r['line']} | {r['issues']} |")
        print()

    if elite_findings:
        print("## Elite weapons not ending with _elite (not in approved lineage)")
        print("| Actor | Slot | Weapon | File | Line |")
        print("|---|---|---|---|---|")
        for r in sorted(elite_findings, key=lambda x: (x["actor"], x["line"])):
            short = str(r["file"]).replace("\\", "/").replace("mods/cameo/", "")
            print(f"| {r['actor']} | {r['slot']} | {r['weapon']} | {short} | {r['line']} |")
        print()

    if fp_unconditional_findings:
        print("## Unconditional FirepowerMultiplier rule violations")
        print("| Actor | Trait | File | Line | Issue |")
        print("|---|---|---|---|---|")
        for r in sorted(fp_unconditional_findings, key=lambda x: x["actor"]):
            trait = r.get("trait", "")
            f = r.get("file", "")
            line = r.get("line", "")
            if f:
                f = f.replace("\\", "/").replace("mods/cameo/", "")
            print(f"| {r['actor']} | {trait} | {f} | {line} | {r['issue']} |")
        print()

    if fp_conditional_rows:
        print("## Conditional FirepowerMultiplier review report")
        print("Classify each row as: `ignore` / `special` / `upgrade` / `transient` / etc.")
        print("| Actor | Multiplier | Modifier | Condition | Proposed role |")
        print("|---|---|---|---|---|")
        for r in sorted(fp_conditional_rows, key=lambda x: (x["actor"], x["multiplier"])):
            print(f"| {r['actor']} | {r['multiplier']} | {r['modifier']} | {r['condition']} | {r['proposed_role']} |")
        print()

    if lineage_findings:
        print("## Candidate multi-step weapon lineages for approved registry")
        print("| Actor | Guessed base | Weapons (ordered) | Slots | Conditions |")
        print("|---|---|---|---|---|")
        for r in sorted(lineage_findings, key=lambda x: x["actor"]):
            print(f"| {r['actor']} | {r['guessed_base']} | {', '.join(r['weapons'])} | {', '.join(r['slots'])} | {', '.join(r['conditions'])} |")
        print()

    return 1 if (slot_findings or elite_findings or fp_unconditional_findings) else 0


if __name__ == "__main__":
    sys.exit(main())
