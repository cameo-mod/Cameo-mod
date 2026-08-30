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

# ⚠ THE HEALTH TRAIT IS NOT ALWAYS CALLED `Health`. Crystallized Nexus ships its own
# `CNHealth` (in `.modsdk/OpenRA.Mods.CN`), so a reader hardcoding `Health` finds 610 actors and
# ZERO units with hit points — an empty result that looks like "this mod has no data" rather than
# like a bug. Every peer therefore declares which traits carry its stats.
DEFAULT_TRAITS = {"health": ("Health",), "cost": ("Valued",),
                  "speed": ("Mobile", "Aircraft")}

# mod_id -> dict describing the peer:
#   label     human name used as the source label in the synthesis
#   root      checkout candidates; the FIRST whose mods/<mod_id>/mod.yaml exists wins
#   mod_id    the directory under mods/ (defaults to the key)
#   rifle     actor id of the basic rifleman — the mod's own 1.00x anchor
#   expect    documented anchor HP, checked against the checkout and REPORTED when it differs
#   traits    overrides for DEFAULT_TRAITS
PEERS = {
    "ca": {"label": "Combined Arms", "rifle": "E1", "expect": 5000,
           "root": ["/home/user/inq8/camod", "~/Documents/GitHub/CAmod", "../CAmod"]},
    "sp": {"label": "Shattered Paradise", "rifle": "E1", "expect": 12500,
           "root": ["/home/user/abrandau/shattered-paradise-sdk",
                    "~/Documents/GitHub/Shattered-Paradise-SDK",
                    "../Shattered-Paradise-SDK"]},
    # CN's basic rifleman is GASOL (the GDI "Marine", 125 HP / 120 cr) — it ships no E1, and its
    # HP scale is classic-Westwood-sized rather than OpenRA-sized, which is exactly why the
    # per-mod rifle normalization exists.
    "cn": {"label": "Crystallized Nexus", "rifle": "GASOL", "expect": 125,
           "traits": {"health": ("CNHealth", "Health")},
           # CN keeps its mod under .modsdk/, not at the checkout root.
           "root": ["/home/user/dogyaut/crystallized-nexus/.modsdk",
                    "~/Documents/GitHub/crystallized-nexus/.modsdk",
                    "~/Downloads/crystallized-nexus-main/.modsdk"]},
    # ⚠ FRACTURED REALMS IS DECLARED BUT CANNOT VOTE, and that is a finding, not a gap in this
    # tool. It resolves cleanly — 488 actors, 191 weapons — but only 23 actors carry BOTH Health
    # and Valued, and 18 of those are buildings (walls, gates, power plants, a forge). The mobile
    # remainder is a dozer, a transport ship, an MCV, one bomber and one scout. There is no basic
    # rifleman, so there is nothing to normalize against, and inventing an anchor would fabricate
    # every ratio derived from it. Last pushed 2023-10; it reads as an early prototype rather
    # than a balanced mod. Kept here so the check is recorded and re-runs automatically if the
    # mod ever grows a roster.
    "fnw": {"label": "Fractured Realms", "rifle": "e1", "expect": None,
            "root": ["/home/user/logue-yne/fractured-realms",
                     "~/Documents/GitHub/Fractured-Realms", "../Fractured-Realms"]},
    "rv": {"label": "Romanov's Vengeance", "rifle": "e1", "expect": 12500,
           "root": ["/home/user/mustaphatr/romanovs-vengeance",
                    "~/Documents/GitHub/Romanovs-Vengeance", "../Romanovs-Vengeance"]},
    # C&C Generals in OpenRA. §15.5 rules that SAGE economies do NOT map to credits, so its COST
    # column is identity-only and must not be read as a price; HP normalized to its own basic
    # infantry is still comparable. Its anchor is `infantry.conscript` (12,000 HP / 100) — there
    # is no `E1` in this universe.
    "gen": {"label": "Generals Alpha", "rifle": "infantry.conscript", "expect": 12000,
            "root": ["/home/user/mustaphatr/generals-alpha",
                     "~/Documents/GitHub/Generals-Alpha", "../Generals-Alpha"]},
    # The four OpenRA BASE mods — the original games as the engine ships them. They are the
    # closest thing to a neutral reading of Westwood's own balance, and `versus_raw.json` already
    # samples all four for warheads; these are their unit stats.
    "ra": {"label": "OpenRA Red Alert", "rifle": "E1", "expect": 5000,
           "root": ["/home/user/openra/openra", "~/Documents/GitHub/OpenRA", "../OpenRA"]},
    "cnc": {"label": "OpenRA Tiberian Dawn", "rifle": "E1", "expect": 5000,
            "root": ["/home/user/openra/openra", "~/Documents/GitHub/OpenRA", "../OpenRA"]},
    "ts": {"label": "OpenRA Tiberian Sun", "rifle": "E1", "expect": 12500,
           "root": ["/home/user/openra/openra", "~/Documents/GitHub/OpenRA", "../OpenRA"]},
    "d2k": {"label": "OpenRA Dune 2000", "rifle": "light_inf", "expect": 6000,
            "root": ["/home/user/openra/openra", "~/Documents/GitHub/OpenRA", "../OpenRA"]},
}


def find_checkout(cands, mod_id):
    """First candidate that actually holds this mod's manifest.

    Checking `mods/<mod_id>/mod.yaml` rather than just `mods/` matters: Crystallized Nexus keeps
    its mod under `.modsdk/`, so the repository root has no `mods/` at all.
    """
    for c in cands:
        p = pathlib.Path(c).expanduser()
        if (p / "mods" / mod_id / "mod.yaml").is_file():
            return p
    return None


def trait(node, names, field):
    """First value of `field` on any of `names` (matching `Name` or `Name@suffix`)."""
    if isinstance(names, str):
        names = (names,)
    for child in node.children:
        if child.key.split("@")[0] in names:
            v = child.get(field)
            if v:
                return v
    return None


def traits_for(spec):
    t = dict(DEFAULT_TRAITS)
    t.update(spec.get("traits") or {})
    return t


def load_fluent(root, mod_id):
    """{key: text} from the mod's .ftl files.

    SP and CN name their units by fluent key rather than literally, and the two use different
    Fluent shapes — SP writes `e1-name = Light Infantry` on one line, CN writes an ATTRIBUTE
    block:

        actor-gasol =
            .name = Marine
            .description =
            General-purpose infantry.

    so both forms are read. Getting this wrong is not cosmetic: the display name is what matches
    a peer unit to a Cameo actor, and an unresolved `actor-gasol.name` matches nothing.
    """
    out = {}
    for f in (root / "mods" / mod_id).rglob("*.ftl"):
        text = f.read_text(encoding="utf-8", errors="replace")
        key = None
        for line in text.splitlines():
            m = re.match(r"^([A-Za-z0-9_.-]+)\s*=\s*(.*)$", line)
            if m:
                key = m.group(1)
                if m.group(2).strip():
                    out[key] = m.group(2).strip()
                continue
            a = re.match(r"^\s+\.([A-Za-z0-9_-]+)\s*=\s*(.+)$", line)
            if a and key:
                out[f"{key}.{a.group(1)}"] = a.group(2).strip()
    return out


def unit_name(actor_id, node, fluent):
    """Display name, tried in the order that actually resolves across these mods."""
    raw = trait(node, "Tooltip", "Name") or ""
    if raw in fluent:
        return fluent[raw]
    # OpenRA's convention when a Tooltip carries no explicit Name.
    for guess in (f"actor-{actor_id.lower()}.name", f"{actor_id.lower()}-name"):
        if guess in fluent:
            return fluent[guess]
    return raw or actor_id


def extract(mod_id):
    spec = PEERS[mod_id]
    label, cands, rifle_id, expect = spec["label"], spec["root"], spec["rifle"], spec["expect"]
    T = traits_for(spec)
    root = find_checkout(cands, mod_id)
    if root is None:
        return label, None, f"no checkout found (looked in {', '.join(cands)})"
    rules = miniyaml.Ruleset(root, mod_id)
    fluent = load_fluent(root, mod_id)

    key = rules._actor_ci.get(rifle_id.lower())
    if not key:
        return label, None, f"rifle actor {rifle_id} not present"
    rifle = rules.resolve(key)
    rhp = trait(rifle, T["health"], "HP")
    rcost = trait(rifle, T["cost"], "Cost")
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
        hp = trait(node, T["health"], "HP")
        if not hp:
            continue
        cost = trait(node, T["cost"], "Cost")
        speed = trait(node, T["speed"], "Speed")
        rows.append({
            "id": actor, "name": unit_name(actor, node, fluent),
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
