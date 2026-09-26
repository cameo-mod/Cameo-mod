#!/usr/bin/env python3
"""w22_triage_unreachable.py — one-row triage for the 76 `unreachable` actors.

EMBER lane item 4 (fleet month-lane orders 2026-09-26). The W22 census
(`w22_roster_census.py`) labels an armored actor `unreachable` when it has a
Buildable.Queue but its positive prerequisites are never simultaneously
satisfiable in any real faction's roster fixpoint. This tool assigns each of
them ONE row with a class:

  map_script_only   placed by a .oramap / spawned by lua — unbuildable on
                    purpose, keep (maps cannot be grep-verified cheaply at
                    review time, so the corpus scan is the evidence).
  broken_prereq     a prereq token is undefined (nobody provides it) or only
                    provided by actors that are themselves unreachable —
                    the row names the blocking token.
  dead_content      the `~disabled` convention, or prereqs that no faction
                    can jointly satisfy even though every token is
                    individually reachable somewhere (cross-faction gap).

Read-only. Writes docs/balance/w22_unreachable_triage.{md,json}.
"""

from __future__ import annotations

import collections
import json
import pathlib
import sys
import zipfile

ROOT = pathlib.Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "tools/audit"))
sys.path.insert(0, str(ROOT / "tools/balance"))

from cameo_model import Model          # noqa: E402
import target_model as tm              # noqa: E402  (ARMORS)

OUT_MD = ROOT / "docs/balance/w22_unreachable_triage.md"
OUT_JSON = ROOT / "docs/balance/w22_unreachable_triage.json"

DISABLE_TOKENS = {"disabled", "disable", "disabledactor"}

# Same producer-trait set as w22_roster_census.py — spawn-only beats
# unreachable in the census ordering, so triage must reproduce it.
import re as _re
PRODUCER_TRAIT = _re.compile(
    r"(spawn|produce|power|airstrike|paratroop|paradrop|drop|deploy|reinforce"
    r"|grant|crew|crate|squad|camera|prisoner|garrison|exit|harvest|capture"
    r"|chronoshift|demo|carry|transport|eject|evac|transform|undeploy|sell)",
    _re.IGNORECASE)


def _leaf_values(node) -> list[str]:
    out = [str(node.value)]
    for c in getattr(node, "children", []) or []:
        out.extend(_leaf_values(c))
    return out


def _spawned_names(node, all_names: set[str]) -> set[str]:
    out: set[str] = set()
    for child in node.children:
        root = child.key.split("@", 1)[0]
        if not PRODUCER_TRAIT.search(root):
            continue
        for leaf in _leaf_values(child):
            for tok in leaf.replace(",", " ").split():
                t = tok.strip().lower()
                if t in all_names:
                    out.add(t)
    return out


ACTOR_LINE = _re.compile(r"^\t?Actor\d+:\s*(\S+)\s*$")


def _map_placed() -> set[str]:
    """Actor ids placed by at least one bundled .oramap (same extraction as
    audit_map_actors.map_actors)."""
    out: set[str] = set()
    for z in sorted((ROOT / "mods/cameo/maps").rglob("*.oramap")):
        try:
            with zipfile.ZipFile(z) as f:
                if "map.yaml" not in f.namelist():
                    continue
                text = f.read("map.yaml").decode("utf-8", "replace")
        except (OSError, zipfile.BadZipFile):
            continue
        inside = False
        for line in text.splitlines():
            if line.startswith("Actors:"):
                inside = True
                continue
            if inside:
                if line and not line[0].isspace():
                    inside = False
                    continue
                mm = ACTOR_LINE.match(line)
                if mm:
                    out.add(mm.group(1).lower())
    return out


def _lua_referenced() -> set[str]:
    out: set[str] = set()
    for z in sorted((ROOT / "mods/cameo/maps").rglob("*.oramap")):
        try:
            with zipfile.ZipFile(z) as f:
                for n in f.namelist():
                    if not n.endswith(".lua"):
                        continue
                    text = f.read(n).decode("utf-8", "replace")
                    for tok in text.replace('"', " ").replace("'", " ").split():
                        out.add(tok.strip().lower())
        except (OSError, zipfile.BadZipFile):
            continue
    return out


def _armored(node) -> str | None:
    for child in node.children:
        if child.key == "Armor" or child.key.startswith("Armor@"):
            v = child.get("Type")
            if v and str(v).strip() in tm.ARMORS:
                return str(v).strip()
    return None


def _providers_of_token(model: Model, token: str) -> set[str]:
    """Concrete actors whose _provider_tokens include `token`."""
    out: set[str] = set()
    for name in model.rs.actors:
        if name.startswith(("^", "$", "-")):
            continue
        res = model.rs.resolve(name)
        if res is None:
            continue
        if token in model._provider_tokens(name.lower(), res):
            out.add(name.lower())
    return out


def main() -> int:
    m = Model()
    rs = m.rs
    factions = [f.internal for f in m.real_factions()]
    rosters = {f: m.roster(f) for f in factions}
    faction_tokens = {f: m.faction_tokens(f) for f in factions}

    placed = _map_placed()
    lua_ids = _lua_referenced()

    # Armored concrete set + producer map first (census ordering: spawn_only
    # outranks unreachable when something produces the actor).
    armored: dict[str, tuple[str, object]] = {}
    for name in rs.actors:
        if name.startswith(("^", "$", "-")) or "." in name:
            continue
        node = rs.resolve(name)
        if node is None:
            continue
        a = _armored(node)
        if a is not None:
            armored[name.lower()] = (a, node)
    producers: set[str] = set()
    for name in rs.actors:
        if name.startswith(("^", "$", "-")) or "." in name:
            continue
        node = rs.resolve(name)
        if node is None:
            continue
        producers |= _spawned_names(node, set(armored))

    rows = []
    for lname, (armor, node) in armored.items():
        b = node.child("Buildable")
        if b is None or not b.get("Queue"):
            continue
        prereqs = m.positive_prereqs(node)
        if any(lname in r for r in rosters.values()):
            continue  # reachable somewhere — not our target
        if lname in producers:
            continue  # census: spawn_only
        # bot-gated handled by census; here we care about unreachable only
        if any(any(bt in t for bt in ("botplayer",)) for t in prereqs):
            continue
        name = lname

        mapped = name in placed or name in lua_ids
        owner = m.owner_of(name) or "?"
        # ContentPacks owner "theme/sub" -> faction internal (atreides,
        # orcs -> wc2_orcs, nod -> ts_nod when theme-scoped).
        sub = owner.rsplit("/", 1)[-1]
        owner_f = next(
            (f for f in factions if f == sub or f.endswith("_" + sub)), None)

        # per-token analysis — for each token record which FACTIONS can hold it
        undefined, dead_chain, cross = [], [], []
        for t in prereqs:
            if t.startswith(m.OPTION_TOKEN_PREFIXES):
                continue
            providers = _providers_of_token(m, t)
            if not providers:
                undefined.append(t)
                continue
            holders = sorted(f for f in factions if t in faction_tokens[f])
            if not holders:
                dead_chain.append(t)
            elif owner_f is not None and t not in faction_tokens[owner_f]:
                # provider reachable for others but not the owner — find the
                # owner's own providers and why each one fails.
                same_owner = sorted(
                    p for p in providers
                    if (m.owner_of(p) or "").rsplit("/", 1)[-1]
                    in (sub, owner_f or ""))
                if same_owner:
                    det = []
                    for p in same_owner:
                        pres = m.rs.resolve(p)
                        pb = pres.child("Buildable") if pres else None
                        if pb is None or not pb.get("Queue"):
                            det.append(f"{p} (no Queue)")
                        else:
                            det.append(p)
                    cross.append((t, det))
                else:
                    cross.append((t, holders))

        if mapped:
            cls = "map_script_only"
            why = "placed/spawned by bundled map(s) or lua"
        elif any(t in DISABLE_TOKENS for t in prereqs) or any(
                t in DISABLE_TOKENS for t in undefined):
            cls = "dead_content"
            why = "`~disabled` convention (disabled token is never provided)"
        elif undefined:
            cls = "broken_prereq"
            why = "undefined prereq token(s): " + ",".join(undefined)
        elif dead_chain:
            cls = "broken_prereq"
            why = "prereq provider(s) unreachable: " + ",".join(dead_chain)
        elif cross:
            cls = "broken_prereq"
            why = "token(s) owned by other factions: " + ",".join(
                f"{t}<-{'/'.join(h)}" for t, h in cross)
        else:
            cls = "dead_content"
            why = "no faction jointly satisfies the token set"

        rows.append({
            "actor": name.lower(), "class": cls, "owner": owner,
            "armor": armor, "queue": str(b.get("Queue")),
            "prereqs": prereqs, "undefined": undefined,
            "dead_chain": dead_chain,
            "cross": [{"token": t, "holders": h} for t, h in cross],
            "map_or_lua": mapped, "why": why,
        })

    by_class = collections.Counter(r["class"] for r in rows)

    md = [
        "# W22 unreachable-actor triage — one row per `unreachable` actor",
        "",
        "Generated by `tools/balance/w22_triage_unreachable.py` — read-only.",
        "Classes per the fleet order: `broken_prereq` | `map_script_only` |",
        "`dead_content`. **Nothing is deleted by this pass** — deletion needs a",
        "maintainer ruling per row.",
        "",
        "## Findings",
        "",
        "- **One token gates the whole naval tree.** `globalnaval` is defined",
        "  by nobody — every navalyard (wc2_humans, wc2_orcs, ts_gdi, ts_nod,",
        "  cabal, forgotten) and every hull behind it (~30 rows) is one",
        "  decision away from live or permanently parked.",
        "- **d2k research providers are unbuildable.** `atreides_ixresearchcenter`",
        "  and `corrino_ixresearchcenter` carry `Buildable` but no `Queue:` —",
        "  `research_centre` is never granted, killing 6 rows downstream.",
        "- **`~disabled` parks 22 actors** (backup mechs, `ra2_c_*` campaign",
        "  units, steel consortium set, wc2 summons, misc singles).",
        "- **Map/script only:** `td_gdi_landingcraft`, `ra1_soviets_kennel`.",
        "",
        "| class | count |",
        "|---|--:|",
        *(f"| `{c}` | {n} |" for c, n in by_class.most_common()),
        "",
        "| actor | class | owner | queue | why |",
        "|---|---|---|---|---|",
        *("| `{}` | {} | {} | {} | {} |".format(
            r["actor"], r["class"], r["owner"], r["queue"], r["why"])
          for r in rows),
    ]
    OUT_MD.write_text("\n".join(md) + "\n", encoding="utf-8")
    OUT_JSON.write_text(json.dumps(
        {"generated_by": "tools/balance/w22_triage_unreachable.py",
         "classes": dict(by_class), "rows": rows}, indent=1),
        encoding="utf-8")
    print(f"unreachable triaged: {len(rows)} — {dict(by_class)}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
