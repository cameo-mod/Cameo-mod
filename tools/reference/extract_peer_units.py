#!/usr/bin/env python3
"""Extract per-unit stats from a peer OpenRA mod's checkout, for the balance reference corpus.

PRIOR ART: `tools/reference/extract_versus.py` pulls WARHEAD/Versus profiles out of peer mods
(W13, feeding `versus_raw.json` and `aggregate_archetype.py`). This pulls UNIT stats — HP, Cost,
Speed — which that corpus deliberately does not carry. Different axis, no overlap.

    python tools/reference/extract_peer_units.py            # both mods, write the document
    python tools/reference/extract_peer_units.py --mod ca --dry-run

WHY IT EXISTS
-------------
`BALANCE_SYNTHESIS.md` §15 pools every reference source into a per-unit target, and
`ORIGINAL_UNIT_STATS.md` carries Combined Arms and Shattered Paradise as ROLE BANDS only —
"basic rifle 5000", "heavy trooper 7500-9000" — never as per-unit rows. So the two OpenRA peer
crossovers, the mods closest to Cameo in both engine and intent, could not vote on any named
unit. The single CA per-unit figure anywhere in the tree was the Apocalypse at 130,000 HP,
quoted as prose in §16. This closes that gap with real data.

⚠ THE RESOLVER IS NOT OPTIONAL. CLAUDE.md rule 8e forbids hand-parsing yaml, and an OpenRA actor
is a chain of `Inherits:` — `APOC` alone carries no HP; it comes from `^Tank` several levels up.
So this reads through `miniyaml.Ruleset`, which was parameterized by `mod_id` for exactly this
(it defaults to "cameo", so every existing caller is untouched). A regex over `vehicles.yaml`
would have returned nothing for most units and, worse, plausible numbers for a few.

⚠ EACH MOD SETS ITS OWN POWER LEVEL, so raw HP is meaningless across them. Every row is
normalized to that mod's own basic rifleman before it leaves here. Anchors are VERIFIED against
the checkout rather than trusted from a document — and one of them was wrong:
`ORIGINAL_UNIT_STATS.md` states "SP GDI rifle = 15000", but SP's `E1` (Light Infantry) resolves
to **12,500**. The artifact wins.
"""
import argparse
import pathlib
import re
import sys

ROOT = pathlib.Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "tools" / "audit"))
import miniyaml  # noqa: E402

OUT = ROOT / "docs" / "design" / "ORIGINAL_UNITS_PEER_OPENRA.md"

# mod_id -> (label, checkout candidates, rifle actor, expected rifle HP or None)
PEERS = {
    "ca": ("Combined Arms",
           ["/home/user/inq8/camod", "~/Documents/GitHub/CAmod", "../CAmod"],
           "E1", 5000),
    "sp": ("Shattered Paradise",
           ["/home/user/abrandau/shattered-paradise-sdk",
            "~/Documents/GitHub/Shattered-Paradise-SDK", "../Shattered-Paradise-SDK"],
           "E1", 12500),
}


def find_checkout(cands):
    for c in cands:
        p = pathlib.Path(c).expanduser()
        if (p / "mods").is_dir():
            return p
    return None


def trait(node, name, field):
    """First value of `field` on any `name` / `name@suffix` trait of a resolved actor."""
    for child in node.children:
        if child.key.split("@")[0] == name:
            v = child.get(field)
            if v:
                return v
    return None


def load_fluent(root, mod_id):
    """{key: text} from the mod's .ftl files — SP names its units by fluent key, not literally."""
    out = {}
    for f in (root / "mods" / mod_id / "fluent").glob("*.ftl"):
        for line in f.read_text(encoding="utf-8", errors="replace").splitlines():
            m = re.match(r"^([a-z0-9-]+)\s*=\s*(.+)$", line)
            if m:
                out[m.group(1)] = m.group(2).strip()
    return out


def unit_name(node, fluent):
    raw = trait(node, "Tooltip", "Name") or ""
    return fluent.get(raw, raw)


def extract(mod_id):
    label, cands, rifle_id, expect = PEERS[mod_id]
    root = find_checkout(cands)
    if root is None:
        return label, None, f"no checkout found (looked in {', '.join(cands)})"
    rules = miniyaml.Ruleset(root, mod_id)
    fluent = load_fluent(root, mod_id)

    key = rules._actor_ci.get(rifle_id.lower())
    if not key:
        return label, None, f"rifle actor {rifle_id} not present"
    rifle = rules.resolve(key)
    rhp = trait(rifle, "Health", "HP")
    rcost = trait(rifle, "Valued", "Cost")
    if not rhp:
        return label, None, f"{rifle_id} has no resolvable HP"
    rhp, rcost = int(rhp), int(rcost or 0)
    note = ""
    if expect and rhp != expect:
        note = (f"⚠ documented anchor was {expect:,}; the checkout resolves **{rhp:,}** — "
                "the artifact wins")

    rows = []
    for actor in sorted(rules.actors):
        if actor.startswith(("^", "-")):
            continue
        node = rules.resolve(actor)
        if node is None:
            continue
        if not any(c.key.split("@")[0] == "Buildable" for c in node.children):
            continue
        hp = trait(node, "Health", "HP")
        if not hp:
            continue
        cost = trait(node, "Valued", "Cost")
        speed = trait(node, "Mobile", "Speed") or trait(node, "Aircraft", "Speed")
        rows.append({
            "id": actor, "name": unit_name(node, fluent) or actor,
            "hp": int(hp), "cost": int(cost) if cost else None,
            "speed": int(speed) if speed else None,
            "x_hp": int(hp) / rhp,
            "x_cost": (int(cost) / rcost) if (cost and rcost) else None,
        })
    return label, {"root": root, "rifle": (rifle_id, rhp, rcost), "rows": rows,
                   "note": note}, None


def main():
    ap = argparse.ArgumentParser(description=__doc__.split("\n")[0])
    ap.add_argument("--mod", choices=sorted(PEERS), action="append")
    ap.add_argument("--dry-run", action="store_true")
    args = ap.parse_args()

    out = ["# Original units — OpenRA peer crossovers (Combined Arms, Shattered Paradise)", "",
           "_AUTO-GENERATED by `tools/reference/extract_peer_units.py` from each mod's own "
           "checkout, read through `miniyaml.Ruleset` so the `Inherits:` chains actually "
           "resolve. Do not hand-edit._", "",
           "Companion to [`ORIGINAL_UNITS_RAW.md`](ORIGINAL_UNITS_RAW.md) (the RA2 family) and "
           "[`ORIGINAL_UNIT_STATS.md`](ORIGINAL_UNIT_STATS.md) (the source games). Those two "
           "carry CA and SP as ROLE BANDS only, so before this document neither could vote on a "
           "named unit in the synthesis.", "",
           "**Each mod sets its own power level**, so `×rifle` is the only comparable column. "
           "Anchors are verified against the checkout, never trusted from a document.", ""]

    total = 0
    for mod_id in (args.mod or sorted(PEERS)):
        label, data, err = extract(mod_id)
        if err:
            print(f"{label}: {err}")
            out += [f"## {label}", "", f"⚠ not extracted — {err}", ""]
            continue
        rid, rhp, rcost = data["rifle"]
        total += len(data["rows"])
        print(f"{label}: {len(data['rows'])} buildable units "
              f"(rifle {rid} = {rhp:,} HP / {rcost} cr){'  ' + data['note'] if data['note'] else ''}")
        out += [f"## {label}  ({len(data['rows'])} buildable units)", "",
                f"Checkout: `{data['root']}` · rifle anchor **`{rid}` = {rhp:,} HP / "
                f"{rcost} credits = 1.00×**"]
        if data["note"]:
            out += ["", data["note"]]
        out += ["", "| id | unit | HP | ×rifle | Cost | ×rifle cost | Speed |",
                "|---|---|--:|--:|--:|--:|--:|"]
        for r in sorted(data["rows"], key=lambda x: -x["x_hp"]):
            xc = f"{r['x_cost']:.2f}" if r["x_cost"] else "—"
            cost = f"{r['cost']:,}" if r["cost"] else "—"
            spd = r["speed"] if r["speed"] else "—"
            out.append(f"| `{r['id']}` | {r['name']} | {r['hp']:,} | {r['x_hp']:.2f} | "
                       f"{cost} | {xc} | {spd} |")
        out.append("")

    if args.dry_run:
        print(f"DRY RUN: {total} rows, nothing written")
        return 0
    OUT.write_text("\n".join(out) + "\n", encoding="utf-8")
    print(f"wrote {OUT.relative_to(ROOT)} ({total} rows)")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
