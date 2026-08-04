#!/usr/bin/env python3
"""Resolution-aware AreaDamage retrofit sweep (dry-run by default).

Prepares every weapon that (transitively) inherits a ^Warhead_{Fam}_{Lvl} family
template for the SpreadDamage -> AreaDamage template conversion:

  class 1  delete `-Warhead@X_FriendlyFire:` REMOVAL lines  (twin no longer exists)
  class 2  strip ` SpreadDamage` from `Warhead@X:` MAIN overrides (inherit AreaDamage type)
  class 2d strip inner `ValidRelationships: Neutral, Enemy` from swept MAIN blocks (unblock FF)
  class 3  delete `Warhead@X_FriendlyFire: ...` twin DEFINITION blocks (FF baked into main)

Family keys resolved via ACTUAL transitive inheritance of the specific
^Warhead_{Fam}_{Lvl} template -- never key-name. Local/standalone keys
(e.g. Napalm_Crate's standalone twin) and _Percentage/_ExtraDamage are untouched.

Usage: sweep_areadamage.py [--apply]
"""
from __future__ import annotations
import re
import sys
from pathlib import Path
from collections import defaultdict

MOD = Path(__file__).resolve().parents[2] / "mods" / "cameo"
CENTRAL = [
    "weapons/weapons.yaml", "weapons/tiberiandawn.yaml", "weapons/redalert2mod.yaml",
    "weapons/d2k.yaml", "weapons/starcraft.yaml", "weapons/warcraft2.yaml",
    "weapons/tiberiansun.yaml", "weapons/outpost2.yaml",
]
FILES = [MOD / p for p in CENTRAL] + sorted((MOD / "ContentPacks").glob("*/*/yaml/weapons.yaml"))

RE_INHERITS = re.compile(r"^Inherits(?:@\S+)?:\s*(\S+)")
RE_WARHEAD = re.compile(r"^(-?)Warhead@(\S+?):\s*(\S*)\s*$")
RE_TOPNAME = re.compile(r"^([^\s#][^:]*):")
RE_VALIDREL = re.compile(r"^ValidRelationships:\s*(.+?)\s*$")


def indent_of(s: str) -> int:
    n = 0
    for ch in s:
        if ch == "\t":
            n += 8 - (n % 8)
        elif ch == " ":
            n += 1
        else:
            break
    return n


def content(s: str) -> str:
    return s.rstrip("\r\n").lstrip()  # strip indentation + EOL


class Node:
    __slots__ = ("name", "file", "start", "end", "base_indent", "parents",
                 "warheads")  # warheads: list of (idx, removal, key, wtype, block_end)

    def __init__(self, name, file, start):
        self.name = name
        self.file = file
        self.start = start
        self.end = None
        self.base_indent = None
        self.parents = []
        self.warheads = []


def parse_file(path: Path):
    text = path.read_text(encoding="utf-8")
    lines = text.splitlines(keepends=True)
    nodes = []
    i, n = 0, len(lines)
    # find top-level node header line indices (indent 0, not blank, not comment)
    headers = []
    for idx, ln in enumerate(lines):
        raw = ln.rstrip("\r\n")
        if not raw.strip() or raw.lstrip().startswith("#"):
            continue
        if indent_of(raw) == 0 and RE_TOPNAME.match(raw):
            headers.append(idx)
    for h_i, start in enumerate(headers):
        end = headers[h_i + 1] if h_i + 1 < len(headers) else n
        name = RE_TOPNAME.match(lines[start].rstrip("\r\n")).group(1).strip()
        node = Node(name, path, start)
        node.end = end
        # base indent = min indent of non-blank child lines
        child_indents = [indent_of(lines[j]) for j in range(start + 1, end)
                         if lines[j].strip() and not lines[j].lstrip().startswith("#")]
        base = min(child_indents) if child_indents else 8
        node.base_indent = base
        # scan node-direct lines (indent == base)
        j = start + 1
        while j < end:
            raw = lines[j].rstrip("\r\n")
            if not raw.strip() or raw.lstrip().startswith("#"):
                j += 1
                continue
            ind = indent_of(raw)
            if ind == base:
                c = content(raw)
                m = RE_INHERITS.match(c)
                if m:
                    node.parents.append(m.group(1).strip())
                mw = RE_WARHEAD.match(c)
                if mw:
                    removal, key, wtype = mw.group(1), mw.group(2), mw.group(3)
                    # block end = next line with indent <= base
                    k = j + 1
                    while k < end:
                        r2 = lines[k].rstrip("\r\n")
                        if r2.strip() and not r2.lstrip().startswith("#") and indent_of(r2) <= base:
                            break
                        k += 1
                    node.warheads.append((j, removal, key, wtype, k))
            j += 1
        nodes.append(node)
    return lines, nodes


def main():
    apply = "--apply" in sys.argv

    # ---- parse everything ----
    file_lines = {}
    all_nodes = {}          # name -> list of Node (merged across files)
    parents_map = defaultdict(set)
    for path in FILES:
        lines, nodes = parse_file(path)
        file_lines[path] = lines
        for nd in nodes:
            all_nodes.setdefault(nd.name, []).append(nd)
            parents_map[nd.name].update(nd.parents)

    family_templates = {nm for nm in all_nodes if nm.startswith("^Warhead_")}

    # ---- transitive ancestors ----
    anc_cache = {}

    def ancestors(name):
        if name in anc_cache:
            return anc_cache[name]
        seen = set()
        stack = list(parents_map.get(name, ()))
        while stack:
            p = stack.pop()
            if p in seen:
                continue
            seen.add(p)
            stack.extend(parents_map.get(p, ()))
        anc_cache[name] = seen
        return seen

    SUFFIXES = ("_FriendlyFire", "_Percentage", "_ExtraDamage")

    # edits: file -> {lineidx_to_delete:set, lineidx_to_replace:{idx:newtext}}
    del_lines = defaultdict(set)
    repl_lines = defaultdict(dict)
    stats = defaultdict(int)
    affected_weapons = set()
    reports = {"class1": [], "class2": [], "class2d": [], "class3": [], "surprise": []}

    for name, nds in all_nodes.items():
        if name.startswith("^Warhead_"):
            continue  # generator owns these
        anc = ancestors(name)
        for nd in nds:
            lines = file_lines[nd.file]
            for (idx, removal, key, wtype, blk_end) in nd.warheads:
                # split suffix
                base, suffix = key, ""
                for suf in SUFFIXES:
                    if key.endswith(suf):
                        base, suffix = key[:-len(suf)], suf
                        break
                cand = "^Warhead_" + base
                if cand not in family_templates:
                    continue
                if cand not in anc:
                    continue  # standalone / local -- do not touch
                rel = str(nd.file.relative_to(MOD)).replace("\\", "/")
                loc = f"{rel}:{idx+1}"
                if suffix == "_FriendlyFire":
                    # delete whole block (class 1 removal, or class 3 definition)
                    for d in range(idx, blk_end):
                        del_lines[nd.file].add(d)
                    if removal == "-":
                        stats["class1"] += 1
                        reports["class1"].append(f"{loc}  {name}  -Warhead@{key}")
                    else:
                        stats["class3"] += 1
                        reports["class3"].append(f"{loc}  {name}  Warhead@{key} ({wtype})")
                    affected_weapons.add(name)
                elif suffix == "":
                    # MAIN warhead
                    if removal == "-":
                        continue  # valid removal of AreaDamage main -- leave
                    if wtype == "SpreadDamage":
                        raw = lines[idx].rstrip("\r\n")
                        newtxt = re.sub(r"(Warhead@" + re.escape(key) + r":)\s*SpreadDamage\s*$",
                                        r"\1", raw)
                        eol = lines[idx][len(raw):]
                        repl_lines[nd.file][idx] = newtxt + eol
                        stats["class2"] += 1
                        reports["class2"].append(f"{loc}  {name}  Warhead@{key}: SpreadDamage -> bare")
                        affected_weapons.add(name)
                    elif wtype not in ("", "SpreadDamage"):
                        reports["surprise"].append(f"{loc}  {name}  Warhead@{key}: {wtype} (unexpected type)")
                        continue
                    # scan block for ValidRelationships: Neutral, Enemy (class 2d)
                    for b in range(idx + 1, blk_end):
                        c = content(lines[b].rstrip("\r\n"))
                        mvr = RE_VALIDREL.match(c)
                        if mvr:
                            val = mvr.group(1).strip()
                            norm = {x.strip() for x in val.split(",")}
                            if norm == {"Neutral", "Enemy"}:
                                del_lines[nd.file].add(b)
                                stats["class2d"] += 1
                                reports["class2d"].append(f"{rel}:{b+1}  {name}  strip ValidRelationships: {val}")
                            else:
                                reports["surprise"].append(
                                    f"{rel}:{b+1}  {name}  MAIN ValidRelationships: {val} (kept -- not Neutral,Enemy)")
                # _Percentage / _ExtraDamage: leave

    # ---- report ----
    print("=" * 78)
    print("AreaDamage retrofit sweep -- DRY RUN" if not apply else "AreaDamage retrofit sweep -- APPLYING")
    print("=" * 78)
    print(f"live files parsed : {len(FILES)}")
    print(f"family templates  : {len(family_templates)}")
    print(f"weapons/nodes touched: {len(affected_weapons)}")
    print()
    print(f"class 1  (delete -Warhead@X_FriendlyFire removal) : {stats['class1']}")
    print(f"class 2  (strip ' SpreadDamage' from main)        : {stats['class2']}")
    print(f"class 2d (strip ValidRelationships Neutral,Enemy) : {stats['class2d']}")
    print(f"class 3  (delete Warhead@X_FriendlyFire twin)     : {stats['class3']}")
    print(f"total lines deleted : {sum(len(s) for s in del_lines.values())}")
    print(f"total lines edited  : {sum(len(d) for d in repl_lines.values())}")
    print()
    # per-file summary
    print("per-file:")
    for path in FILES:
        d = len(del_lines.get(path, ()))
        e = len(repl_lines.get(path, ()))
        if d or e:
            print(f"  {str(path.relative_to(MOD)).replace(chr(92),'/'):55s} del={d:3d} edit={e:3d}")
    for cls in ("class1", "surprise"):
        if reports[cls]:
            print()
            print(f"--- {cls} ({len(reports[cls])}) ---")
            for r in reports[cls]:
                print("  " + r)
    # samples for class2/class3
    for cls in ("class2", "class3", "class2d"):
        if reports[cls]:
            print()
            print(f"--- {cls} sample (first 12 of {len(reports[cls])}) ---")
            for r in reports[cls][:12]:
                print("  " + r)

    if apply:
        for path in FILES:
            dels = del_lines.get(path, set())
            repls = repl_lines.get(path, {})
            if not dels and not repls:
                continue
            lines = file_lines[path]
            out = []
            for idx, ln in enumerate(lines):
                if idx in dels:
                    continue
                out.append(repls.get(idx, ln))
            path.write_text("".join(out), encoding="utf-8", newline="")
        print("\nAPPLIED.")


if __name__ == "__main__":
    main()
