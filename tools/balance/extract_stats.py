#!/usr/bin/env python3
"""extract_stats.py — Balance Pipeline Phase 1 (BALANCE_PIPELINE.md §2).

yaml -> per-faction RAW-STAT JSON ledger in docs/balance/.

Laws implemented here:
- RAW stats only: every value exactly as the resolved rules state it
  (wdist stays wdist, no DPS, no combined damage — warheads are listed
  raw, one entry per damage warhead).
- Provenance on every value: "<repo-relative-file>#Trait.Field" when the
  value is written in the actor's own block, "inherited" when it comes
  from a template (write-back then knows to add-or-edit).
- Deterministic serialization (sorted keys, fixed indent) so ledger
  diffs are minimal and mergeable.
- `--check`: re-extract in memory and diff against the committed ledger;
  exit 1 on drift (run_all wiring comes in Phase 6).

Usage:
    python tools/balance/extract_stats.py            # write the ledger
    python tools/balance/extract_stats.py --check    # drift detection
    python tools/balance/extract_stats.py --faction tkm   # subset
"""
from __future__ import annotations

import argparse
import json
import pathlib
import sys

ROOT = pathlib.Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "tools/audit"))
from cameo_model import Model  # noqa: E402

OUT = ROOT / "docs/balance"
PACKS = ROOT / "mods/cameo/ContentPacks"

# pack rules files that define balance-relevant actors (closed set,
# DESIGN §2); weapons/sequences/ai/templates/husks are not rosters.
SECTION_FILES = ("faction", "buildings", "defenses", "infantry", "vehicles",
                 "aircraft", "naval", "upgrades", "promotions", "misc")
SHARED_LEAVES = {"Shared", "Core"}


def rel(p) -> str:
    return str(pathlib.Path(p).resolve().relative_to(ROOT)).replace("\\", "/")


def top_keys(path: pathlib.Path) -> list[str]:
    out = []
    for line in path.read_text(encoding="utf-8-sig", errors="replace").split("\n"):
        if line and (line[0].isalnum() or line[0] in "^_") and ":" in line:
            out.append(line.split(":")[0].strip())
    return out


def child(node, key):
    for c in node.children:
        if c.key == key:
            return c
    return None


def stat(resolved, local, trait: str, field: str):
    """Raw value + provenance for one Trait.Field."""
    t = child(resolved, trait)
    if t is None:
        return None
    v = t.get(field)
    if v is None:
        return None
    lt = child(local, trait) if local is not None else None
    if lt is not None and lt.get(field) is not None:
        src = f"{rel(lt.file)}#{trait}.{field}"
    else:
        src = "inherited"
    return {"v": v, "src": src}


def weapon_entry(rs, wname: str) -> dict | None:
    resolved = rs.resolve_weapon(wname)
    if resolved is None:
        return None
    local = rs.weapon(wname)
    out = {"weapon": wname, "defined_in": rel(local.file) if local is not None else None}
    for field in ("ReloadDelay", "Burst", "BurstDelays", "Range", "MinRange"):
        v = resolved.get(field)
        if v is not None:
            out[field.lower()] = v
    warheads = []
    for c in resolved.children:
        if c.key.startswith("Warhead@") and c.value in ("SpreadDamage", "HealthPercentageDamage", "TargetDamage"):
            d = c.get("Damage")
            if d is not None:
                warheads.append({"tag": c.key.split("@", 1)[1], "type": c.value, "damage": d})
    out["warheads"] = warheads
    if local is not None:
        out["versus_templates"] = [c.value for c in local.children
                                   if c.key == "Inherits" or c.key.startswith("Inherits@")]
    return out


def extract_actor(rs, key: str) -> dict | None:
    resolved = rs.resolve(key)
    if resolved is None:
        return None
    buildable = child(resolved, "Buildable")
    valued = child(resolved, "Valued")
    if buildable is None and valued is None:
        return None  # not balance-relevant (husk fragments, decorations...)
    local = rs.actor(key)
    u: dict = {}
    tooltip = child(resolved, "Tooltip")
    if tooltip is not None and tooltip.get("Name"):
        u["name"] = tooltip.get("Name")
    for out_key, trait, field in (
            ("cost", "Valued", "Cost"),
            ("hp", "Health", "HP"),
            ("armor", "Armor", "Type"),
            ("speed", "Mobile", "Speed"),
            ("speed_air", "Aircraft", "Speed"),
            ("turn_speed", "Mobile", "TurnSpeed"),
            ("sight", "RevealsShroud", "Range"),
            ("build_limit", "Buildable", "BuildLimit"),
            ("build_duration", "Buildable", "BuildDuration"),
            ("self_heal_step", "ChangesHealth", "Step"),
    ):
        s = stat(resolved, local, trait, field)
        if s is not None:
            u[out_key] = s
    if buildable is not None:
        prereq = buildable.get("Prerequisites")
        if prereq:
            u["prerequisites"] = [p.strip() for p in prereq.split(",") if p.strip()]
        queue = buildable.get("Queue")
        if queue:
            u["queue"] = [q.strip() for q in queue.split(",") if q.strip()]
    arms = []
    for c in resolved.children:
        if c.key == "Armament" or c.key.startswith("Armament@"):
            wname = c.get("Weapon")
            if not wname:
                continue
            entry = {"slot": c.key}
            w = weapon_entry(rs, wname)
            if w is None:
                entry["weapon"] = wname
                entry["unresolved"] = True
            else:
                entry.update(w)
            req = c.get("RequiresCondition")
            if req:
                entry["requires"] = req
            if c.get("Name"):
                entry["armament_name"] = c.get("Name")
            arms.append(entry)
    if arms:
        u["armaments"] = arms
    # design judgment inputs — seeded by Phase 3 from the legacy sheet;
    # null until then (they never exist in yaml).
    u["design"] = {"unit_class": None, "special": None, "tech_tier": None,
                   "class_anchor": None}
    return u


def pack_rosters() -> dict[str, dict]:
    """{ledger-name: {"pack": relpath, "sections": {section: [actor,...]}}}"""
    rosters: dict[str, dict] = {}
    for pack_dir in sorted(PACKS.glob("*/*/")):
        theme, leaf = pack_dir.parts[-2], pack_dir.parts[-1]
        ydir = pack_dir / "yaml"
        if not ydir.is_dir():
            continue
        if leaf in SHARED_LEAVES:
            ledger = f"shared_{theme.lower()}" if leaf == "Shared" else None
            if ledger is None:
                continue  # Core: meta factions only, no balance rosters
        else:
            ledger = f"{theme.lower()}_{leaf.lower()}"
        entry = rosters.setdefault(
            ledger, {"pack": rel(pack_dir).rstrip("/"), "sections": {}})
        for section in SECTION_FILES:
            f = ydir / f"{section}.yaml"
            if not f.is_file():
                continue
            keys = [k for k in top_keys(f) if not k.startswith("^")]
            if keys:
                entry["sections"].setdefault(section, []).extend(keys)
    return rosters


def load_existing_design(name: str) -> dict[str, dict]:
    """{actor: design-dict} from the committed ledger — design.* fields
    are judgment data (seeded from the legacy sheet / maintainer), NOT
    yaml facts, so re-extraction must never wipe them."""
    p = OUT / f"{name}.json"
    if not p.exists():
        return {}
    out = {}
    try:
        doc = json.loads(p.read_text(encoding="utf-8"))
        for sec in doc.get("sections", {}).values():
            for actor, u in sec.items():
                d = u.get("design")
                if d and any(v is not None for v in d.values()):
                    out[actor] = d
    except (json.JSONDecodeError, OSError):
        pass
    return out


def build_ledgers(model: Model, only: str | None = None) -> dict[str, dict]:
    rs = model.rs
    ledgers: dict[str, dict] = {}
    for ledger, info in sorted(pack_rosters().items()):
        if only and only not in ledger:
            continue
        keep_design = load_existing_design(ledger)
        sections: dict = {}
        for section, actors in sorted(info["sections"].items()):
            sec: dict = {}
            for a in sorted(set(actors)):
                u = extract_actor(rs, a)
                if u is not None:
                    if a in keep_design:
                        u["design"] = keep_design[a]
                    sec[a] = u
            if sec:
                sections[section] = sec
        if sections:
            ledgers[ledger] = {"schema": 2, "ledger": ledger,
                               "pack": info["pack"], "sections": sections}
    return ledgers


def serialize(doc: dict) -> str:
    return json.dumps(doc, sort_keys=True, indent=1, ensure_ascii=False) + "\n"


def main() -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument("--check", action="store_true",
                    help="diff against the committed ledger; exit 1 on drift")
    ap.add_argument("--faction", help="ledger-name substring filter")
    args = ap.parse_args()

    ledgers = build_ledgers(Model(), args.faction)
    if args.check:
        drift = 0
        for name, doc in ledgers.items():
            p = OUT / f"{name}.json"
            want = serialize(doc)
            have = p.read_text(encoding="utf-8") if p.exists() else ""
            if want != have:
                print(f"DRIFT: {name} ({'missing' if not have else 'stale'})")
                drift += 1
        print(f"balance check: {len(ledgers)} ledgers, {drift} drifted")
        return 1 if drift else 0

    OUT.mkdir(parents=True, exist_ok=True)
    total = 0
    for name, doc in ledgers.items():
        (OUT / f"{name}.json").write_text(serialize(doc), encoding="utf-8", newline="\n")
        n = sum(len(s) for s in doc["sections"].values())
        total += n
        print(f"  {name}.json: {n} actors")
    print(f"wrote {len(ledgers)} ledgers, {total} actors -> {rel(OUT)}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
