#!/usr/bin/env python3
"""count_mixed.py — Phase-W triage: group concrete weapons by which OLD full-stack
templates still sit in their inheritance ancestry, so the next 3-way-split cluster
can be chosen by frequency.

Resolves inheritance transitively (so a weapon on an intermediate like ^RA2Chaingun
is counted against its ^Chaingun base). Prints, ranked by size:
  1. per single OLD template — how many concrete weapons still transitively inherit it
  2. per exact OLD-template SIGNATURE (the set of old bases a weapon pulls in) + examples

A weapon is "converted" once it no longer inherits any OLD template (it inherits the
new ^Warhead_* / ^Projectile_* / ^Effect_* families instead). Usage: python tools/balance/count_mixed.py
"""
from __future__ import annotations
import re
from pathlib import Path
from collections import defaultdict, Counter

MOD = Path(__file__).resolve().parents[2] / "mods" / "cameo"
CENTRAL = ["weapons/weapons.yaml", "weapons/tiberiandawn.yaml", "weapons/redalert2.yaml",
           "weapons/redalert2mod.yaml", "weapons/d2k.yaml", "weapons/starcraft.yaml",
           "weapons/warcraft2.yaml", "weapons/tiberiansun.yaml", "weapons/outpost2.yaml"]
FILES = [MOD / p for p in CENTRAL] + sorted((MOD / "ContentPacks").glob("*/*/yaml/weapons.yaml"))

OLD_BASE = {"^" + n for n in (
    "SmallArms", "Chaingun", "Grenade", "ShrapnelWeapon", "HeavyBomb", "TankDestroyerCannon",
    "MediumCannon", "HeavyCannon", "LightMissile", "MediumMissile", "HeavyMissile", "FlakWeapon",
    "HeavyAAWeapon", "LightFlameWeapon", "MediumFlameWeapon", "HeavyFlameWeapon", "LightChemicalWeapon",
    "MediumChemicalWeapon", "HeavyChemicalWeapon", "SwordWeapon", "ArrowWeapon", "MagicWeapon",
    "LaserWeapon", "RailgunWeapon", "TeslaWeapon", "TeslaChargedWeapon", "NuclearWarhead",
    "SniperWeapon", "ToxicWeapon", "HealingWeapon", "RepairWeapon")}

RE_INHERITS = re.compile(r"^Inherits(?:@\S+)?:\s*(\S+)")
RE_TOP = re.compile(r"^([^\s#][^:]*):")


def indent_of(s):
    n = 0
    for ch in s:
        if ch == "\t":
            n += 8 - (n % 8)
        elif ch == " ":
            n += 1
        else:
            break
    return n


nodes = {}  # name (with ^ for templates) -> {"parents":[...], "file":str}
for path in FILES:
    if not path.exists():
        continue
    lines = path.read_text(encoding="utf-8").splitlines()
    headers = [i for i, ln in enumerate(lines)
               if ln.strip() and not ln.lstrip().startswith("#") and indent_of(ln) == 0 and RE_TOP.match(ln)]
    for h, start in enumerate(headers):
        end = headers[h + 1] if h + 1 < len(headers) else len(lines)
        name = RE_TOP.match(lines[start]).group(1).strip()
        rec = nodes.setdefault(name, {"parents": [], "file": path.name})
        child = [indent_of(lines[j]) for j in range(start + 1, end)
                 if lines[j].strip() and not lines[j].lstrip().startswith("#")]
        base = min(child) if child else 8
        for j in range(start + 1, end):
            raw = lines[j]
            if not raw.strip() or raw.lstrip().startswith("#") or indent_of(raw) != base:
                continue
            m = RE_INHERITS.match(raw.strip())
            if m:
                rec["parents"].append(m.group(1).strip())


def old_ancestors(name, stack=()):
    if name in stack:
        return set()
    out = set()
    if name in OLD_BASE:
        out.add(name)
    rec = nodes.get(name)
    if rec:
        for p in rec["parents"]:
            out |= old_ancestors(p, stack + (name,))
    return out


single = Counter()
sig = Counter()
examples = defaultdict(list)
sig_files = defaultdict(set)
for name, rec in nodes.items():
    if name.startswith("^"):
        continue  # concrete weapons only
    olds = old_ancestors(name)
    if not olds:
        continue
    for o in olds:
        single[o] += 1
    key = tuple(sorted(olds))
    sig[key] += 1
    sig_files[key].add(rec["file"])
    if len(examples[key]) < 3:
        examples[key].append(name)

total = sum(sig.values())
print(f"concrete weapons still on >=1 OLD template: {total}\n")
print("=== per OLD template (transitive) ===")
for o, c in single.most_common():
    print(f"  {c:4d}  {o}")
print(f"\n=== exact OLD-signature clusters (top 45 of {len(sig)}) ===")
for key, c in sig.most_common(45):
    tag = "SINGLE" if len(key) == 1 else f"{len(key)}-mix"
    print(f"  {c:4d}  [{tag}] {' + '.join(key)}")
    print(f"        files={len(sig_files[key])}  e.g. {', '.join(examples[key])}")
