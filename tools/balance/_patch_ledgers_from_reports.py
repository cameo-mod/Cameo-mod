#!/usr/bin/env python3
"""Patch ledger JSONs from `propose_class_rebalance` reports so apply_balance can write YAML.

This is step 2 of the sanctioned loop (`extract_stats` -> LEDGER -> `apply_balance --confirm`):
the proposals are markdown, and nothing reaches yaml until their targets are in the ledger.
`apply_balance` reads ONLY the ledger — it never opens `class_anchors.json` — which is why a
signed anchor on its own changes nothing and a dry run reports "0 values would change".

⚠ DEFAULTS TO THE **SIGNED** CLASSES ONLY. Sign-off is what authorises a class's numbers to move,
so an unsigned class is not patched unless it is named explicitly on the command line.

Three defects fixed 2026-08-30, each of which would have quietly corrupted the ledger:

  1. The report list was three HARDCODED `proposal_*_infantry.md` paths — a naming bug fixed the
     same day, so every path was dead and the tool patched NOTHING while exiting 0.
  2. It wrote `firepower_multiplier` on every actor. **FirepowerMultiplier is RETIRED (W17)** and
     `apply_balance` refuses to write it, reporting "RETIRED KNOB" per actor. The tool would have
     put a value in the ledger that the next stage rejects and `audit_balance_drift` flags red.
     The proposals already solve at fp = 1.0 and fold magnitude into Damage, so there is nothing
     to write.
  3. Damage selection matched `type == "SpreadDamage"` only. The W24/3-way-split conversion moves
     warheads to `AreaDamage`, so on every converted weapon the filter selected NOTHING and the
     damage half of the proposal was silently dropped.
"""
import argparse
import json
import pathlib
import apply_balance

ROOT = pathlib.Path(__file__).resolve().parents[2]
LEDGER = ROOT / "docs" / "balance"
ANCHORS = LEDGER / "class_anchors.json"

# The damage-carrying warhead types. `SpreadDamage` is the legacy one; `AreaDamage` is what the
# 3-way split converts to. Matching only the first is how a conversion silently disarms this tool.
DAMAGE_TYPES = ("SpreadDamage", "AreaDamage")


def signed_classes():
    """Classes the maintainer has signed off — the only ones authorised to move by default."""
    doc = json.loads(ANCHORS.read_text(encoding="utf-8"))
    doc = doc.get("classes", doc)
    return sorted(k for k, v in doc.items()
                  if isinstance(v, dict) and v.get("signed_off"))


def reports_for(classes):
    out = []
    for cls in classes:
        p = LEDGER / f"proposal_{cls}.md"
        if p.exists():
            out.append(p)
        else:
            print(f"  WARN: no proposal for `{cls}` — run propose_class_rebalance first")
    return out


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
    ap = argparse.ArgumentParser(description=__doc__.split("\n")[0])
    ap.add_argument("--class", dest="classes", action="append",
                    help="class to patch (repeatable). Default: every SIGNED class.")
    ap.add_argument("--dry-run", action="store_true",
                    help="report what would change without writing any ledger")
    args = ap.parse_args()

    classes = args.classes or signed_classes()
    if not classes:
        print("no signed classes and none named — nothing to do")
        return
    print(f"classes: {', '.join(classes)}")
    reports = reports_for(classes)
    if not reports:
        print("no proposals found — nothing to do")
        return

    fresh_docs = apply_balance.fresh_ledgers(None)

    actor_map = {}
    for name, doc in fresh_docs.items():
        for sec, actors in doc.get("sections", {}).items():
            for actor, u in actors.items():
                actor_map[actor] = (name, sec, u)

    updates = 0
    warnings = []
    for rp in reports:
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
            # ⚠ NO FirepowerMultiplier WRITE. W17 retired it as a pricing knob:
            # `apply_balance.RETIRED_UNIT_FIELDS` refuses it and reports "RETIRED KNOB" for
            # every actor carrying one, and `propose_class_rebalance` already solves at
            # fp = 1.0 and folds magnitude into Damage on the 100 grid. Writing it here put a
            # value in the ledger that the next stage rejects.

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
                    if w.get("type") not in DAMAGE_TYPES:
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

    if args.dry_run:
        print(f"DRY RUN: {updates} ledger value(s) would change. Re-run without --dry-run to write.")
    else:
        for name, doc in fresh_docs.items():
            (LEDGER / f"{name}.json").write_text(
                json.dumps(doc, ensure_ascii=False, indent=1), encoding="utf-8")
        print(f"patched {updates} ledger value(s)")
    for w in warnings:
        print(f"  WARN: {w}")


if __name__ == "__main__":
    main()
