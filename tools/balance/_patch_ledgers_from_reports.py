#!/usr/bin/env python3
"""Patch ledger JSONs from curated markdown reports so apply_balance.py can write YAML."""
import json
import pathlib
import apply_balance

ROOT = pathlib.Path(__file__).resolve().parents[2]
LEDGER = ROOT / "docs" / "balance"
REPORTS = [
    LEDGER / "proposal_scout_infantry.md",
    LEDGER / "proposal_closecombat_infantry.md",
    LEDGER / "proposal_special_forces_infantry.md",
]


def parse_report(path: pathlib.Path):
    lines = path.read_text(encoding="utf-8-sig").splitlines()
    h = None
    for i, line in enumerate(lines):
        if line.startswith("|") and "actor" in line:
            h = i
            break
    if h is None:
        return []
    headers = [c.strip() for c in lines[h].split("|")[1:-1]]
    idx = {name: i for i, name in enumerate(headers)}

    def get_num(row, name):
        if name not in idx:
            return None
        s = row[idx[name]].strip().split()[0].strip("`")
        if s in ("", "-"):
            return None
        try:
            if "." in s:
                return float(s)
            return int(s)
        except ValueError:
            return None

    rows = []
    for line in lines[h + 2:]:
        if not line.startswith("|"):
            continue
        cells = [c.strip() for c in line.split("|")[1:-1]]
        if len(cells) != len(headers):
            continue
        actor = cells[idx["actor"]].strip("`")
        if not actor or actor == "actor":
            continue
        rows.append({
            "actor": actor,
            "HP": get_num(cells, "HP"),
            "spd": get_num(cells, "spd"),
            "rng": get_num(cells, "rng"),
            "cost": get_num(cells, "cost"),
            "dmg": get_num(cells, "dmg"),
            "dmg_filter": cells[idx["dmg_filter"]].strip() if "dmg_filter" in idx else "all",
            "burst": get_num(cells, "burst"),
            "rl": get_num(cells, "rl"),
            "fp": get_num(cells, "FP%"),
            "tech": get_num(cells, "tech"),
        })
    return rows


def main():
    fresh_docs = apply_balance.fresh_ledgers(None)

    actor_map = {}
    for name, doc in fresh_docs.items():
        for sec, actors in doc.get("sections", {}).items():
            for actor, u in actors.items():
                actor_map[actor] = (name, sec, u)

    updates = 0
    warnings = []
    for rp in REPORTS:
        for row in parse_report(rp):
            actor = row["actor"]
            if actor not in actor_map:
                warnings.append(f"{actor}: not found in any ledger")
                continue
            _jf, _sec, u = actor_map[actor]

            if row["HP"] is not None and isinstance(u.get("hp"), dict):
                u["hp"]["v"] = row["HP"]
                updates += 1
            if row["spd"] is not None and isinstance(u.get("speed"), dict):
                u["speed"]["v"] = row["spd"]
                updates += 1
            if row["cost"] is not None and isinstance(u.get("cost"), dict):
                u["cost"]["v"] = row["cost"]
                updates += 1
            if row["tech"] is not None:
                u.setdefault("design", {})["tech_tier"] = row["tech"]
                updates += 1
            fp = row["fp"] if row["fp"] is not None else 100
            if isinstance(u.get("firepower_multiplier"), dict):
                u["firepower_multiplier"]["v"] = fp / 100.0
                updates += 1
            else:
                # derive file from an existing unit src and create an unqualified FirepowerMultiplier entry
                src_base = None
                for ref in ("hp", "speed", "cost"):
                    if isinstance(u.get(ref), dict) and "src" in u[ref]:
                        cand = u[ref]["src"].split("#", 1)[0]
                        if cand and not cand.lower().startswith("inherited"):
                            src_base = cand
                            break
                if src_base:
                    u["firepower_multiplier"] = {
                        "v": fp / 100.0,
                        "src": f"{src_base}#FirepowerMultiplier.Modifier",
                    }
                    updates += 1
                else:
                    warnings.append(f"{actor}: no src to attach FirepowerMultiplier")

            arms = u.get("armaments", [])
            pricing = [a for a in arms if a.get("pricing") and a.get("slot") in ("Armament", "Armament@PRIMARY")]
            if not pricing:
                warnings.append(f"{actor}: no primary pricing armament found")
                continue
            arm = pricing[0]
            if row["rng"] is not None:
                arm["range"] = row["rng"]
                updates += 1
            if row["rl"] is not None:
                arm["reloaddelay"] = row["rl"]
                updates += 1
            if row["burst"] is not None:
                arm["burst"] = row["burst"]
                updates += 1
            if row["dmg"] is not None:
                smallarms_only = row["dmg_filter"] == "smallarms"
                selected = []
                old_total = 0
                for w in arm.get("damage_warheads", []):
                    if w.get("type") != "SpreadDamage":
                        continue
                    tag = (w.get("tag") or "").lower()
                    if tag.endswith("extradamage") or tag.endswith("percentage"):
                        continue
                    if "friendly" in tag:
                        continue
                    if smallarms_only and not tag.startswith("smallarms"):
                        continue
                    selected.append(w)
                    old_total += float(w.get("damage") or 0)
                if selected:
                    old_max = max(float(w["damage"]) for w in selected)
                    if old_max > 0:
                        ratio = row["dmg"] / old_max
                        for w in selected:
                            w["damage"] = int(round(float(w["damage"]) * ratio))
                            updates += 1

    for name, doc in fresh_docs.items():
        (LEDGER / f"{name}.json").write_text(json.dumps(doc, ensure_ascii=False, indent=1), encoding="utf-8")

    print(f"patched {updates} ledger value(s)")
    for w in warnings:
        print(f"  WARN: {w}")


if __name__ == "__main__":
    main()
