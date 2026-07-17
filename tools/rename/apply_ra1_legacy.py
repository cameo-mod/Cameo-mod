#!/usr/bin/env python3
"""apply_ra1_legacy.py — context-scoped applicator for rename_map_ra1_legacy.yaml.

Successor to the retired apply.py for ACTOR-ID-ONLY renames (no asset file
renames — shared-asset law, DESIGN §1). Scoping rules learned from the
2026-07 rename regressions:

- .yaml:   whole-identifier replace, case-insensitive, longest-first;
           identifier chars are [A-Za-z0-9_.] so `CA` never matches inside
           `OpenRA.Mods.CA` or `ca.aud`, and `U2` never matches inside
           `..._u2.shp`.
- .ftl:    only `actor_<oldid>` message-key stems (never prose).
- .lua:    only whole quoted strings ("OLDID" -> "newid"), case-insensitive.
- maps:    unpacked map.yaml/*.lua AND every .oramap zip member, with the
           same rules per file type. Zips are rewritten only when changed.

Prints per-area change counts; run the audit suite + boot gate afterwards.
"""
from __future__ import annotations

import io
import pathlib
import re
import sys
import zipfile

ROOT = pathlib.Path(__file__).resolve().parents[2]
MOD = ROOT / "mods/cameo"

def load_map() -> list[tuple[str, str]]:
    pairs = []
    for line in (ROOT / "tools/rename/rename_map_ra1_legacy.yaml").read_text(encoding="utf-8").splitlines():
        s = line.strip()
        if not s or s.startswith("#") or s in ("actors:", "files:"):
            continue
        old, _, new = s.partition(":")
        pairs.append((old.strip(), new.strip()))
    pairs.sort(key=lambda p: -len(p[0]))          # longest-first
    return pairs

PAIRS = load_map()
ID = r"[A-Za-z0-9_.]"

PATH_MARKERS = ("|", "bits/", "SupportDir", "Content/", "EngineDir")

# yaml also carries EXPLICIT fluent references (Tooltip.Name: actor_x.name,
# Buildable.Description: actor_x.description) whose `actor_` prefix joins the
# id with an ID char, so the whole-identifier pass never matches them.
# Lesson from the 2026-07 RA1 rename: 13 refs broke when the ftl keys were
# renamed but these yaml refs were not. Rename the stems the same way sub_ftl
# renames the keys.
# All passes use ONE combined alternation regex (52 sequential re.subs per
# line was too slow over the whole mod). Alternatives stay longest-first so
# the longest id wins, matching the old per-pair behaviour.

ID_MAP = {old.lower(): new for old, new in PAIRS}
STEM_MAP = {old.lower().replace(".", "_"): new for old, new in PAIRS}

RX_IDS = re.compile(
    rf"(?<!{ID})(?:{'|'.join(re.escape(o) for o, _ in PAIRS)})(?!{ID})", re.I)
# stems contain no dots (dots -> underscores), so a FOLLOWING dot is the
# `.description`/`.name` attribute access and must be allowed to follow the
# match — `(?!ID)` would reject exactly the refs this pass exists to fix.
RX_STEMS = re.compile(
    rf"(?<!{ID})actor_(?:{'|'.join(sorted((re.escape(s) for s in STEM_MAP), key=len, reverse=True))})(?![A-Za-z0-9_])",
    re.I)
RX_LUA = re.compile(
    rf"\"(?:{'|'.join(re.escape(o) for o, _ in PAIRS)})\"", re.I)

def sub_yaml(text: str) -> str:
    # line-scoped: never touch package/installer path lines (bits/ss lesson)
    out = []
    for line in text.split("\n"):
        if not any(m in line for m in PATH_MARKERS):
            line = RX_IDS.sub(lambda m: ID_MAP[m.group(0).lower()], line)
            line = RX_STEMS.sub(lambda m: "actor_" + STEM_MAP[m.group(0)[6:].lower()], line)
        out.append(line)
    return "\n".join(out)

def sub_ftl(text: str) -> str:
    return RX_STEMS.sub(lambda m: "actor_" + STEM_MAP[m.group(0)[6:].lower()], text)

def sub_lua(text: str) -> str:
    return RX_LUA.sub(lambda m: f"\"{ID_MAP[m.group(0)[1:-1].lower()]}\"", text)

def main() -> int:
    counts = {"yaml": 0, "ftl": 0, "lua": 0, "map.yaml": 0, "oramap": 0}
    SKIP_DIRS = ("installer", "chrome", "tilesets")   # never contain actor ids
    for f in MOD.rglob("*.yaml"):
        if f.name == "chrome.yaml" or any(d in f.parts for d in SKIP_DIRS):
            continue
        t = f.read_text(encoding="utf-8-sig", errors="replace")
        t2 = sub_yaml(t)
        if t2 != t:
            f.write_text(t2, encoding="utf-8", newline="\n")
            counts["map.yaml" if "maps" in f.parts else "yaml"] += 1
    for f in MOD.rglob("*.ftl"):
        t = f.read_text(encoding="utf-8-sig", errors="replace")
        t2 = sub_ftl(t)
        if t2 != t:
            f.write_text(t2, encoding="utf-8", newline="\n")
            counts["ftl"] += 1
    for f in (MOD / "maps").rglob("*.lua"):
        t = f.read_text(encoding="utf-8-sig", errors="replace")
        t2 = sub_lua(t)
        if t2 != t:
            f.write_text(t2, encoding="utf-8", newline="\n")
            counts["lua"] += 1
    for zpath in (MOD / "maps").glob("*.oramap"):
        with zipfile.ZipFile(zpath) as zin:
            members = {i.filename: zin.read(i.filename) for i in zin.infolist()}
        changed = False
        for name, data in list(members.items()):
            if name.endswith(".yaml"):
                t = data.decode("utf-8-sig", errors="replace"); t2 = sub_yaml(t)
            elif name.endswith(".lua"):
                t = data.decode("utf-8-sig", errors="replace"); t2 = sub_lua(t)
            else:
                continue
            if t2 != t:
                members[name] = t2.encode("utf-8"); changed = True
        if changed:
            buf = io.BytesIO()
            with zipfile.ZipFile(buf, "w", zipfile.ZIP_DEFLATED) as zout:
                for name, data in members.items():
                    zout.writestr(name, data)
            zpath.write_bytes(buf.getvalue())
            counts["oramap"] += 1
    print("changed:", counts)
    return 0

if __name__ == "__main__":
    sys.exit(main())
