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

def sub_yaml(text: str) -> str:
    # line-scoped: never touch package/installer path lines (bits/ss lesson)
    out = []
    for line in text.split("\n"):
        if not any(m in line for m in PATH_MARKERS):
            for old, new in PAIRS:
                line = re.sub(rf"(?<!{ID}){re.escape(old)}(?!{ID})", new, line, flags=re.I)
        out.append(line)
    return "\n".join(out)

def sub_ftl(text: str) -> str:
    for old, new in PAIRS:
        stem = re.escape(old.lower().replace(".", "_"))
        text = re.sub(rf"(?<!{ID})actor_{stem}(?!{ID})", f"actor_{new}", text)
    return text

def sub_lua(text: str) -> str:
    for old, new in PAIRS:
        text = re.sub(rf'"{re.escape(old)}"', f'"{new}"', text, flags=re.I)
    return text

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
