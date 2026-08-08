"""Generate Phase B survey: all weapons still inheriting old full-stack
templates, grouped by inherited families and dominant damage.

Output: a markdown report at docs/audit/latest/phase_b_survey.md with:
- per old-family group: weapons, concrete only, families inherited,
  dominant warhead (highest Damage), total DPS-ish, recommended collapse
  target (just a heuristic suggestion, needs maintainer sign-off).
"""
import re
import sys
from pathlib import Path
from collections import defaultdict

MOD = Path(__file__).resolve().parents[2] / "mods" / "cameo"
CENTRAL = ["weapons/weapons.yaml", "weapons/redalert2.yaml",
           "weapons/redalert2mod.yaml", "weapons/tiberiansun.yaml",
           "weapons/tiberiandawn.yaml", "weapons/warcraft2.yaml",
           "weapons/missiles.yaml"]
FILES = [MOD / p for p in CENTRAL] + sorted((MOD / "ContentPacks").glob("*/*/yaml/weapons.yaml"))

# old full-stack family templates still in weapons.yaml
OLD_FAMILIES = {"^SmallArms", "^Chaingun", "^TankDestroyerCannon", "^MediumCannon",
                "^HeavyCannon", "^LightMissile", "^MediumMissile", "^HeavyMissile",
                "^FlakWeapon", "^HeavyAAWeapon", "^Grenade", "^ShrapnelWeapon",
                "^HeavyBomb", "^LaserWeapon", "^RailgunWeapon", "^TeslaWeapon",
                "^TeslaChargedWeapon", "^SwordWeapon", "^ArrowWeapon", "^MagicWeapon",
                "^LightFlameWeapon", "^MediumFlameWeapon", "^HeavyFlameWeapon",
                "^LightChemicalWeapon", "^MediumChemicalWeapon", "^HeavyChemicalWeapon",
                "^NuclearWarhead", "^SniperWeapon", "^LightArms"}
RE_INHERITS_OLD = re.compile(r"^\s*Inherits(?:@\w+)?\s*:\s*\^\w+")
RE_TOPNAME = re.compile(r"^(\w+):\s*$")
RE_WARHEAD = re.compile(r"^\s*Warhead@(\w+)\s*:\s*(\S*)")
RE_DAMAGE = re.compile(r"^\s*Damage\s*:\s*(\d+)")

def indent_of(s):
    return len(s) - len(s.lstrip("\t "))

def parse_file(path):
    text = path.read_text(encoding="utf-8")
    lines = text.splitlines(keepends=True)
    nodes = []
    headers = []
    for idx, ln in enumerate(lines):
        raw = ln.rstrip("\r\n")
        if not raw.strip() or raw.lstrip().startswith("#"):
            continue
        if indent_of(raw) == 0 and RE_TOPNAME.match(raw):
            headers.append(idx)
    for h_i, start in enumerate(headers):
        end = headers[h_i + 1] if h_i + 1 < len(headers) else len(lines)
        name = RE_TOPNAME.match(lines[start].rstrip("\r\n")).group(1).strip()
        block = lines[start:end]
        old_inherits = set()
        warheads = []  # (key, type, damage, has_children)
        cur_wh = None
        cur_type = ""
        cur_dmg = None
        cur_has_children = False
        for j, ln in enumerate(block):
            raw = ln.rstrip("\r\n")
            m_inh = re.match(r"^\s*Inherits(?:@\w+)?\s*:\s*(\^\w+)", raw)
            if m_inh and m_inh.group(1) in OLD_FAMILIES:
                old_inherits.add(m_inh.group(1))
            m_w = RE_WARHEAD.match(raw)
            if m_w:
                if cur_wh is not None:
                    warheads.append((cur_wh, cur_type, cur_dmg, cur_has_children))
                cur_wh = m_w.group(1)
                cur_type = m_w.group(2)
                cur_dmg = None
                cur_has_children = False
            if cur_wh is not None:
                if RE_DAMAGE.match(raw):
                    cur_dmg = int(RE_DAMAGE.match(raw).group(1))
                    cur_has_children = True
                else:
                    cur_has_children = True
        if cur_wh is not None:
            warheads.append((cur_wh, cur_type, cur_dmg, cur_has_children))
        nodes.append({"name": name, "old_inherits": old_inherits, "warheads": warheads,
                      "file": str(path.relative_to(MOD))})
    return nodes

weapons = []
for path in FILES:
    if not path.exists():
        continue
    weapons.extend(parse_file(path))

# group by old inherit families (frozengset of old families)
groups = defaultdict(list)
for w in weapons:
    if w["old_inherits"]:
        key = tuple(sorted(w["old_inherits"]))
        groups[key].append(w)

out = ["# Phase B Mixed-Weapon Survey", "", f"Generated: 2026-08-08"]

# split templates (^) vs concrete
concrete_groups = {k: [w for w in v if not w["name"].startswith("^")] for k, v in groups.items()}
concrete_groups = {k: v for k, v in concrete_groups.items() if v}
single = {k: v for k, v in concrete_groups.items() if len(k) == 1}
mixed = {k: v for k, v in concrete_groups.items() if len(k) > 1}

total = sum(len(v) for v in concrete_groups.values())
out += [
    f"Total concrete weapons on old families: {total}",
    f"Single-inherit (Phase A candidates): {sum(len(v) for v in single.values())} in {len(single)} groups",
    f"Mixed-inherit (Phase B maintainer sign-off): {sum(len(v) for v in mixed.values())} in {len(mixed)} groups",
    "",
]

out.append("## Single-inherit (Phase A) — mechanical conversion candidates")
for key in sorted(single, key=lambda k: (-len(single[k]), k)):
    fam = key[0].lstrip("^")
    out.append(f"### {fam} ({len(single[key])} weapons)")
    for w in sorted(single[key], key=lambda x: x["name"]):
        dom = max(w["warheads"], key=lambda x: x[2] if x[2] is not None else 0) if w["warheads"] else (None, "", 0, False)
        wh_list = ", ".join(f"{k}={d or 0}" for k, t, d, _ in w["warheads"])
        out.append(f"- `{w['name']}` ({w['file']}) | {wh_list}")
    out.append("")

out.append("## Mixed-inherit (Phase B) — dominant-damage analysis for maintainer sign-off")
for key in sorted(mixed, key=lambda k: (-len(k), -len(mixed[k]), k)):
    fams = ", ".join(k.lstrip("^") for k in key)
    out.append(f"### {fams} ({len(mixed[key])} weapons)")
    for w in sorted(mixed[key], key=lambda x: x["name"]):
        dom = max(w["warheads"], key=lambda x: x[2] if x[2] is not None else 0) if w["warheads"] else (None, "", 0, False)
        wh_list = ", ".join(f"{k}={d or 0}" for k, t, d, _ in w["warheads"])
        rec = f"collapse to {dom[0]}" if dom[0] else "needs review"
        out.append(f"- `{w['name']}` ({w['file']}) | dominant: {dom[0]}({dom[2] or 0}) | {wh_list} | → {rec}")
    out.append("")

OUTDIR = MOD.parents[1] / "docs" / "audit" / "latest"
OUTDIR.mkdir(parents=True, exist_ok=True)
OUTFILE = OUTDIR / "phase_b_survey.md"
OUTFILE.write_text("\n".join(out), encoding="utf-8")
print(f"Wrote {OUTFILE}")
print(f"Total weapons on old families: {sum(len(v) for v in groups.values())}")
print(f"Mix groups: {len(groups)}")
