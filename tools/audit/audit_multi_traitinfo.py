#!/usr/bin/env python3
"""audit_multi_traitinfo.py — single-instance trait lookups that can throw on
multi-instance actors.

Background (crash class observed live 2026-10-04, EMBER flag):
``TypeDictionary.Get<T>`` / ``GetOrDefault<T>`` throw "multiple instances" when
the dictionary holds more than one object under the queried key. ``Add``
registers each object under *all* its interfaces and base types, so a lookup
``TraitInfoOrDefault<T>`` / ``TraitInfo<T>`` (ActorInfo level) or
``TraitOrDefault<T>`` / ``Trait<T>`` (live-actor level, TraitDictionary) throws
whenever an actor carries:

- the same trait type twice via ``Trait@variant:`` keys (e.g. ``Power@upgraded``),
- or two *different* concrete types assignable to ``T`` (e.g. two
  ``CameoRangedGpsProviderInfo`` instances satisfy a ``RangedGpsProviderInfo``
  lookup through base-type registration).

``HasTraitInfo<T>`` (``Contains``) and ``TraitInfos<T>``/``TraitsImplementing<T>``
(``WithInterface``) are always safe.

Checks
------
1. Parse every C# ``class``/``interface`` declaration in engine + mod sources for
   direct super types (interfaces and base classes), plus ``XInfo : TraitInfo<Y>``
   for the TraitInfo -> live-trait-type map.
2. Resolve every actor via miniyaml.Ruleset and collect its concrete TraitInfo
   type names (yaml trait key + "Info", matching engine ``LoadTraitInfo``).
3. For each single-instance lookup type used in mod code, count assignable
   concrete types per actor; an actor with >= 2 makes the lookup type
   "multi-capable" and every such call site a FAIL.

Exit 1 when dangerous call sites remain. ``--report`` prints the full
multi-capable-type table; ``--json`` dumps machine-readable detail.
"""

from __future__ import annotations

import argparse
import collections
import json
import pathlib
import re
import sys

sys.path.insert(0, str(pathlib.Path(__file__).parent))
import miniyaml

# --------------------------------------------------------------------------- #
# C# type model
# --------------------------------------------------------------------------- #

CLASS_RE = re.compile(
    r"\b(?:class|interface)\s+([A-Z_]\w*)\s*(?:<[^>{]*>)?\s*:\s*([^\n{]+)")
TRAITINFO_ARG_RE = re.compile(r"\bTraitInfo\s*<\s*([A-Z_]\w*)\s*>")
IFACE_RE = re.compile(r"^I[A-Z]")

SCAN_ROOTS = (
    "engine/OpenRA.Game",
    "engine/OpenRA.Mods.Common",
    "engine/OpenRA.Mods.CA",
    "OpenRA.Mods.Cameo",
)

# Call-site forms that throw on multi-instance. `Trait`/`TraitOrDefault` act on
# live trait instances; `TraitInfo`/`TraitInfoOrDefault` on ActorInfo.
INFO_LOOKUP = re.compile(r"\.(TraitInfoOrDefault|TraitInfo)\s*<\s*([A-Z_]\w*)\s*>")
TRAIT_LOOKUP = re.compile(r"\.(TraitOrDefault|Trait)\s*<\s*([A-Z_]\w*)\s*>")

# Bot-code scope: every C# file under the mod's Traits tree (modules, observers,
# log writers, planners). UI widgets/utility commands are reported as info only.
BOT_SCOPE = "OpenRA.Mods.Cameo/Traits"


def parse_type_model(root: pathlib.Path):
    """Return (supers, traitinfo_produced) where supers maps each declared type
    to its direct super type names and traitinfo_produced maps a TraitInfo class
    to the live trait type it constructs."""
    supers: dict[str, set[str]] = {}
    produced: dict[str, str] = {}
    decl_kind: dict[str, str] = {}
    for rel in SCAN_ROOTS:
        for path in (root / rel).rglob("*.cs"):
            try:
                text = path.read_text(encoding="utf-8", errors="replace")
            except OSError:
                continue
            for m in CLASS_RE.finditer(text):
                name = m.group(1)
                bases = {b.strip().split("<")[0].strip() for b in m.group(2).split(",")}
                bases = {b for b in bases if b and b[0].isupper() and not b.startswith("where")}
                supers.setdefault(name, set()).update(bases)
                decl_kind[name] = "interface" if "interface" in m.group(0).split(name)[0].split() else "class"
                prod = TRAITINFO_ARG_RE.search(m.group(2))
                if prod:
                    produced[name] = prod.group(1)
    return supers, produced


def closure(supers: dict[str, set[str]], t: str) -> set[str]:
    """All types assignable *to* t's objects — i.e. the types registered under t's
    dictionary key are those whose closure contains t."""
    out = set()
    stack = [t]
    while stack:
        x = stack.pop()
        if x in out:
            continue
        out.add(x)
        stack.extend(supers.get(x, ()))
    return out


def implementers_of(supers: dict[str, set[str]], t: str) -> set[str]:
    """All declared types whose transitive supers include t."""
    return {c for c in supers if t in closure(supers, c)}


# --------------------------------------------------------------------------- #
# Rules pass
# --------------------------------------------------------------------------- #

def actor_info_types(rs) -> dict[str, set[str]]:
    """actor -> set of concrete TraitInfo type names (yaml key + 'Info')."""
    out: dict[str, set[str]] = {}
    for name in rs.actors:
        node = rs.resolve(name)
        if node is None:
            continue
        types = set()
        for c in node.children:
            base = c.key.split("@")[0]
            if base.startswith("-"):
                continue
            types.add(base + "Info")
        out[name] = types
    return out


def multi_capable_lookup_types(rs, supers, produced):
    """lookup-type -> sorted example actors where >=2 types land under its key.

    Covers both lookup levels: a TraitInfo lookup<T> groups by every concrete
    TraitInfo type assignable to T; a live-trait lookup<T> groups by the trait
    types each TraitInfo produces (``XInfo : TraitInfo<Y>`` -> Y).
    """
    per_actor = actor_info_types(rs)
    info_multi: dict[str, list[str]] = collections.defaultdict(list)
    trait_multi: dict[str, list[str]] = collections.defaultdict(list)
    impl_cache: dict[str, set[str]] = {}

    # Precompute implementers once for the types that appear on actors.
    actor_types = set().union(*per_actor.values()) if per_actor else set()
    for t in actor_types:
        impl_cache[t] = {c for c in actor_types if c in implementers_of(supers, c) and t in closure(supers, c)}

    for actor, types in per_actor.items():
        # Candidate lookup keys = every super reachable from any concrete type.
        info_keys: dict[str, list[str]] = collections.defaultdict(list)
        trait_keys: dict[str, list[str]] = collections.defaultdict(list)
        for c in types:
            for k in closure(supers, c):
                info_keys[k].append(c)
            inst = produced.get(c)
            if inst:
                for k in closure(supers, inst):
                    trait_keys[k].append(inst)
        for k, hits in info_keys.items():
            if len(hits) >= 2:
                info_multi[k].append(actor)
        for k, hits in trait_keys.items():
            if len(hits) >= 2:
                trait_multi[k].append(actor)
    return info_multi, trait_multi, per_actor


# --------------------------------------------------------------------------- #
# Call-site scan
# --------------------------------------------------------------------------- #

def scan_call_sites(root: pathlib.Path):
    """Yield (file, line, lookup_level, type) for every throwing single lookup."""
    for path in (root / "OpenRA.Mods.Cameo").rglob("*.cs"):
        rel = path.relative_to(root).as_posix()
        try:
            lines = path.read_text(encoding="utf-8", errors="replace").splitlines()
        except OSError:
            continue
        for i, line in enumerate(lines, 1):
            code = line.split("//")[0]
            for m in INFO_LOOKUP.finditer(code):
                yield rel, i, "info", m.group(2)
            for m in TRAIT_LOOKUP.finditer(code):
                yield rel, i, "trait", m.group(2)


# --------------------------------------------------------------------------- #
# Main
# --------------------------------------------------------------------------- #

def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--report", action="store_true", help="print the multi-capable type table")
    ap.add_argument("--json", action="store_true", help="machine-readable detail")
    args = ap.parse_args()

    root = miniyaml.find_repo_root()
    supers, produced = parse_type_model(root)
    rs = miniyaml.Ruleset(root, "cameo")
    info_multi, trait_multi, per_actor = multi_capable_lookup_types(rs, supers, produced)

    if args.report or args.json:
        print(f"# multi-capable TraitInfo lookup types: {len(info_multi)}")
        for t in sorted(info_multi, key=lambda x: -len(info_multi[x])):
            ex = ", ".join(info_multi[t][:4])
            print(f"  {t:<44} {len(info_multi[t]):>4} actors   {ex}")
        print(f"# multi-capable live-trait lookup types: {len(trait_multi)}")
        for t in sorted(trait_multi, key=lambda x: -len(trait_multi[x])):
            ex = ", ".join(trait_multi[t][:4])
            print(f"  {t:<44} {len(trait_multi[t]):>4} actors   {ex}")

    sites = []
    info_sites = []
    for rel, line, level, t in scan_call_sites(root):
        multi = info_multi if level == "info" else trait_multi
        if t in multi:
            sites.append({"file": rel, "line": line, "level": level, "type": t,
                          "example_actors": multi[t][:4]})
        info_sites.append((rel, line, level, t))

    if args.json:
        print(json.dumps({"dangerous_sites": sites,
                          "multi_info_types": {k: v for k, v in info_multi.items()},
                          "multi_trait_types": {k: v for k, v in trait_multi.items()}}, indent=1))

    t2_owned = {"OpenRA.Mods.Cameo/Traits/BotModules/ExpansionPlannerBotModule.cs",
                "OpenRA.Mods.Cameo/Traits/AiLogExpansion.cs",
                "OpenRA.Mods.Cameo/Traits/AiPlacementLogWriter.cs"}
    mine = [s for s in sites if s["file"] not in t2_owned]
    theirs = [s for s in sites if s["file"] in t2_owned]

    print(f"\nsingle-instance lookups scanned: {len(info_sites)}")
    print(f"dangerous sites (multi-capable type): {len(sites)}")
    for s in sites:
        tag = "T2-lane" if s["file"] in t2_owned else "FIX"
        print(f"  [{tag}] {s['file']}:{s['line']}  {s['level']}<{s['type']}>  e.g. {', '.join(s['example_actors'])}")
    if theirs:
        print(f"  ({len(theirs)} sites in T2Verify's files — covered by devin/t2verify/multi-info-crash)")

    if sites:
        print("\nFAIL — multi-instance trait lookups can throw; convert to TraitInfos<T>/"
              "TraitsImplementing<T> with a correct aggregate (Any/Sum/Max/First).")
        return 1
    print("\nPASS — no single-instance lookup can hit a multi-instance type.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
