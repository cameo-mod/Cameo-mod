#!/usr/bin/env python3
"""audit_template_conformance.py — template-value conformance (design 2026-07-19).

T1  Conyard power: every actor inheriting ^Conyard must use the
    template's Power (100) — local overrides are violations.
T2  Icon offsets: when an image's Defaults defines a nonzero Offset,
    its `icon:` sequence must carry an explicit `Offset: 0,0`
    (otherwise the world-sprite offset displaces the UI icon — the
    Terran command center MCV bug class). Explicit non-zero icon
    offsets are reported separately (T2b) — the D2k legacy `-30,-24`
    pattern needs a maintainer visual pass before normalizing.
"""
from __future__ import annotations

import pathlib
import re
import sys

ROOT = pathlib.Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "tools/audit"))
from cameo_model import Model  # noqa: E402


def zero(val: str) -> bool:
    return all(float(x) == 0 for x in re.split(r"[,\s]+", val.strip()) if x)


def main() -> int:
    findings = 0
    print("# audit_template_conformance — template values are law (2026-07-19)\n")

    # ---- T1: conyard power ------------------------------------------------ #
    rs = Model().rs

    def inherits_conyard(name, seen=None):
        seen = seen or set()
        if name.lower() in seen:
            return False
        seen.add(name.lower())
        n = rs.actor(name)
        if n is None:
            return False
        for c in n.children:
            if c.key == "Inherits" or c.key.startswith("Inherits@"):
                if c.value == "^Conyard" or inherits_conyard(c.value, seen):
                    return True
        return False

    print("## T1 — conyards must use the template Power (100)\n")
    t1 = 0
    for a in sorted(rs.actors):
        if a.startswith("^") or not inherits_conyard(a):
            continue
        r = rs.resolve(a)
        power = next((c.get("Amount") for c in r.children if c.key == "Power"), None)
        if str(power) != "100":
            print(f"- `{a}`: resolved Power {power} (template says 100)")
            t1 += 1
    if not t1:
        print("_clean_")
    findings += t1

    # ---- T2: icon offsets -------------------------------------------------- #
    print("\n## T2 — icons under a nonzero Defaults Offset must set Offset: 0,0\n")
    t2, t2b = [], []
    seqfiles = sorted(set(
        list((ROOT / "mods/cameo").rglob("sequences*.yaml")) +
        list((ROOT / "mods/cameo/ContentPacks").rglob("sequences.yaml")) +
        list((ROOT / "mods/cameo/sequences").glob("*.yaml"))))
    for p in seqfiles:
        lines = p.read_text(encoding="utf-8-sig", errors="replace").split("\n")
        rel = str(p.relative_to(ROOT)).replace("\\", "/")
        img = None
        in_defaults = in_icon = False
        def_nonzero = False
        icon_off = None
        icon_line = None

        def flush():
            nonlocal icon_line, icon_off
            if icon_line is not None and def_nonzero:
                if icon_off is None:
                    t2.append(f"{rel}:{icon_line}: `{img}` icon inherits the Defaults offset")
                elif not zero(icon_off):
                    t2b.append(f"{rel}:{icon_line}: `{img}` icon has explicit Offset {icon_off}")
            icon_line = None
            icon_off = None

        for i, l in enumerate(lines):
            if l and (l[0].isalnum() or l[0] in "^_") and ":" in l:
                flush()
                img = l.split(":")[0]
                def_nonzero = False
                in_defaults = in_icon = False
            elif l.startswith("\t") and not l.startswith("\t\t"):
                if in_icon:
                    flush()
                name = l.strip().split(":")[0]
                in_defaults = name == "Defaults"
                in_icon = name == "icon"
                if in_icon:
                    icon_line = i + 1
            elif l.startswith("\t\t"):
                m = re.match(r"\t\tOffset:\s*(.+)", l)
                if m:
                    if in_defaults and not zero(m.group(1)):
                        def_nonzero = True
                    if in_icon:
                        icon_off = m.group(1).strip()
        flush()
    for x in t2:
        print(f"- {x}")
    if not t2:
        print("_clean_")
    findings += len(t2)
    print("\n### T2b — explicit non-zero icon offsets (maintainer visual pass "
          "pending; D2k legacy pattern)\n")
    for x in t2b[:40]:
        print(f"- {x}")
    if len(t2b) > 40:
        print(f"- … and {len(t2b) - 40} more")
    if not t2b:
        print("_clean_")

    print(f"\nTotal blocking findings: {findings} (T2b informational: {len(t2b)})")
    return 1 if findings else 0


if __name__ == "__main__":
    sys.exit(main())
