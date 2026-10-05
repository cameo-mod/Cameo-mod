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
   direct super types (interfaces and base classes), plus the TraitInfo ->
   live-trait-type map (``XInfo : TraitInfo<Y>``, falling back to the engine's
   ``X``/``XInfo`` naming convention for the ConditionalTraitInfo family).
2. Resolve every actor via miniyaml.Ruleset and collect its *multiset* of
   concrete TraitInfo type names (yaml trait key + "Info", matching engine
   ``LoadTraitInfo``). The multiset matters: two ``Power@variant:`` keys are the
   crash this audit exists for — a set would dedupe them away.
3. For each single-instance lookup type used in mod code, count assignable
   concrete TraitInfo instances per actor; an actor with >= 2 makes the lookup
   type "multi-capable" and every such call site a FAIL. Live-trait lookups
   (``Trait<T>``/``TraitOrDefault<T>``) are judged on the produced trait types.

Call sites are scanned in the mod-owned runtime assemblies (OpenRA.Mods.CA,
OpenRA.Mods.Cameo, OpenRA.Mods.Fransbot); engine call sites are upstream's
problem since the fetched ``engine/`` tree is not editable here. Same-line
``.Single()``/``.SingleOrDefault()`` chains on ``TraitsImplementing<T>()`` /
``TraitInfos<T>()`` count as single-instance lookups too.

This is a zero-tolerance gate, not a ratchet: exit 1 on ANY dangerous call
site. ``--report`` prints the full multi-capable-type table; ``--json`` dumps
machine-readable detail.
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

# Declarations may put the ':' and/or the base list on following lines, and a
# `where` clause may sit between the bases and '{'. Bases are captured lazily
# up to '{' (where-clauses are consumed separately, not into the base list).
# The type model only needs declared-type names + direct bases + the
# TraitInfo<Y> production, so a single permissive match suffices.
# The generic-parameter group must close at the FIRST '>': a greedy inner
# class would swallow ``<T> : Base<T>`` whole (base list included), leaving the
# declaration with no bases — e.g. PausableConditionalTrait<InfoType>.
CLASS_RE = re.compile(
    r"\b(?:class|interface)\s+([A-Z_]\w*)\s*(?:<[^<>{};]*>)?"
    r"(?:\s*:\s*([^{};]*?))?\s*(?:where\b[^{};]*?)?\{")
TRAITINFO_ARG_RE = re.compile(r"\bTraitInfo\s*<\s*([A-Z_]\w*)\s*>")
IFACE_RE = re.compile(r"^I[A-Z]")

# Every assembly that declares trait types: engine (fetched, read-only) plus the
# mod's own three runtime assemblies at the repo root.
SCAN_ROOTS = (
    "engine/OpenRA.Game",
    "engine/OpenRA.Mods.Common",
    "engine/OpenRA.Mods.AS",
    "engine/OpenRA.Mods.Cnc",
    "engine/OpenRA.Mods.D2k",
    "OpenRA.Mods.CA",
    "OpenRA.Mods.Cameo",
    "OpenRA.Mods.Fransbot",
)

# Call-site forms that throw on multi-instance. `Trait`/`TraitOrDefault` act on
# live trait instances; `TraitInfo`/`TraitInfoOrDefault` on ActorInfo.
INFO_LOOKUP = re.compile(r"\.(TraitInfoOrDefault|TraitInfo)\s*<\s*([A-Z_]\w*)\s*>")
TRAIT_LOOKUP = re.compile(r"\.(TraitOrDefault|Trait)\s*<\s*([A-Z_]\w*)\s*>")

# `.Single()`/`.SingleOrDefault()` on a TraitsImplementing<T>()/TraitInfos<T>()
# enumeration is the same crash class as the single-lookup APIs. Chained on the
# same line (cross-line chains are a documented limitation).
SINGLE_LOOKUP = re.compile(
    r"\.(TraitsImplementing|TraitInfos)\s*<\s*([A-Z_]\w*)\s*>[^;\n]*?\.Single(?:OrDefault)?\s*\(")

# Gate scope: every runtime assembly the mod repo owns (engine call sites are
# upstream's problem — the fetched engine/ tree is not editable).
CALL_SITE_ROOTS = (
    "OpenRA.Mods.CA",
    "OpenRA.Mods.Cameo",
    "OpenRA.Mods.Fransbot",
)


def clean_code(text: str) -> str:
    """Blank comments and string/char literal contents to spaces, keeping
    newlines so line numbers survive. Without this, a ``//`` inside a string
    literal truncates the line (hiding real code) and comment text can parse
    as declarations."""
    out = list(text)
    i, n = 0, len(text)
    while i < n:
        c = text[i]
        if c == "/" and i + 1 < n and text[i + 1] == "/":
            while i < n and text[i] != "\n":
                out[i] = " "
                i += 1
        elif c == "/" and i + 1 < n and text[i + 1] == "*":
            out[i] = out[i + 1] = " "
            i += 2
            while i < n and not (text[i] == "*" and i + 1 < n and text[i + 1] == "/"):
                if text[i] != "\n":
                    out[i] = " "
                i += 1
            if i + 1 < n:
                out[i] = out[i + 1] = " "
            i += 2
        elif c == "@" and i + 1 < n and text[i + 1] == '"':
            # Verbatim string: ends at a '"' not followed by another '"'.
            out[i] = out[i + 1] = " "
            i += 2
            while i < n:
                if text[i] == '"' and i + 1 < n and text[i + 1] == '"':
                    out[i] = out[i + 1] = " "
                    i += 2
                    continue
                if text[i] == '"':
                    out[i] = " "
                    i += 1
                    break
                if text[i] != "\n":
                    out[i] = " "
                i += 1
        elif c == '"' or c == "'":
            quote = c
            out[i] = " "
            i += 1
            while i < n:
                if text[i] == "\\":
                    out[i] = " "
                    if i + 1 < n and text[i + 1] != "\n":
                        out[i + 1] = " "
                    i += 2
                    continue
                if text[i] == quote:
                    out[i] = " "
                    i += 1
                    break
                if text[i] == "\n":
                    break  # unterminated literal — resync on the next line
                out[i] = " "
                i += 1
        else:
            i += 1
    return "".join(out)


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
                text = clean_code(path.read_text(encoding="utf-8", errors="replace"))
            except OSError:
                continue
            for m in CLASS_RE.finditer(text):
                name = m.group(1)
                raw = m.group(2) or ""
                bases = {b.strip().split("<")[0].strip() for b in raw.split(",")}
                bases = {b for b in bases if b and b[0].isupper() and not b.startswith("where")}
                supers.setdefault(name, set()).update(bases)
                decl_kind[name] = "interface" if m.group(0).startswith("interface") else "class"
                prod = TRAITINFO_ARG_RE.search(raw)
                if prod:
                    produced[name] = prod.group(1)

    # Fallback for the ``XInfo : ConditionalTraitInfo`` (non-generic TraitInfo)
    # family — by engine convention an info type produces the trait sharing its
    # name minus the Info suffix. Only applied when the stripped name is itself
    # a declared type, and never overriding an explicit TraitInfo<Y> base.
    for name in supers:
        if name.endswith("Info") and name not in produced:
            base = name[:-4]
            if base in supers:
                produced[name] = base
    return supers, produced


def make_closure(supers: dict[str, set[str]]):
    """Memoized closure: all types assignable *to* a type's objects — i.e. the
    dictionary keys a concrete type registers under."""
    cache: dict[str, frozenset[str]] = {}

    def closure(t: str) -> frozenset[str]:
        if t in cache:
            return cache[t]
        out = set()
        stack = [t]
        while stack:
            x = stack.pop()
            if x in out:
                continue
            out.add(x)
            stack.extend(supers.get(x, ()))
        frozen = frozenset(out)
        cache[t] = frozen
        return frozen

    return closure


# --------------------------------------------------------------------------- #
# Rules pass
# --------------------------------------------------------------------------- #

def actor_info_types(rs) -> dict[str, list[str]]:
    """actor -> multiset of concrete TraitInfo type names (yaml key + 'Info').

    A list, not a set: two ``Power@variant:`` keys are the crash this audit
    exists for — deduping them would make the same-type multi case invisible.
    """
    out: dict[str, list[str]] = {}
    for name in rs.actors:
        node = rs.resolve(name)
        if node is None:
            continue
        types = []
        for c in node.children:
            base = c.key.split("@")[0]
            if base.startswith("-") or base in ("Inherits", "InheritsAbstract"):
                continue
            types.append(base + "Info")
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
    closure = make_closure(supers)

    for actor, types in per_actor.items():
        # Candidate lookup keys = every super reachable from any concrete type.
        info_keys: dict[str, list[str]] = collections.defaultdict(list)
        trait_keys: dict[str, list[str]] = collections.defaultdict(list)
        for c in types:
            for k in closure(c):
                info_keys[k].append(c)
            inst = produced.get(c)
            if inst:
                for k in closure(inst):
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
    """Yield (file, line, lookup_level, type) for every throwing single lookup
    inside the mod-owned runtime assemblies."""
    for rel_root in CALL_SITE_ROOTS:
        for path in (root / rel_root).rglob("*.cs"):
            rel = path.relative_to(root).as_posix()
            try:
                lines = clean_code(path.read_text(encoding="utf-8", errors="replace")).splitlines()
            except OSError:
                continue
            for i, line in enumerate(lines, 1):
                for m in INFO_LOOKUP.finditer(line):
                    yield rel, i, "info", m.group(2)
                for m in TRAIT_LOOKUP.finditer(line):
                    yield rel, i, "trait", m.group(2)
                for m in SINGLE_LOOKUP.finditer(line):
                    yield rel, i, "trait" if m.group(1) == "TraitsImplementing" else "info", m.group(2)


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

    print(f"\nsingle-instance lookups scanned: {len(info_sites)}")
    print(f"dangerous sites (multi-capable type): {len(sites)}")
    for s in sites:
        print(f"  {s['file']}:{s['line']}  {s['level']}<{s['type']}>  e.g. {', '.join(s['example_actors'])}")

    if sites:
        print("\nFAIL — multi-instance trait lookups can throw; convert to TraitInfos<T>/"
              "TraitsImplementing<T> with a correct aggregate (Any/Sum/Max/First).")
        return 1
    print("\nPASS — no single-instance lookup can hit a multi-instance type.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
