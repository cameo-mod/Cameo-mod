#!/usr/bin/env python3
"""apply_balance.py — Balance Pipeline Phase 4b (BALANCE_PIPELINE.md §4).

Ledger (docs/balance/*.json) -> yaml, through the provenance anchors.

GATED: without --confirm this is a dry run that prints the diff and
touches nothing. The balance law stands: this command runs on explicit
maintainer order only, and a boot gate + audits follow every real run.

What it writes (prototype scope):
- unit stats whose src is "file#Trait.Field" (written in the actor's
  own block) — Cost, HP, Speed, BuildLimit, sight...;
- weapon ReloadDelay/Burst/BurstDelays/Range/MinRange in the weapon's
  defining block when the field is written there;
- warhead Damage values by tag in the weapon's defining block.
Stats whose src is "inherited" are SKIPPED with a warning (editing a
template affects the whole class — that is Phase 5 knob territory).

Usage:
    python tools/balance/apply_balance.py [--faction tkm]           # dry run
    python tools/balance/apply_balance.py --faction tkm --confirm   # write
"""
from __future__ import annotations

import argparse
import json
import pathlib
import re
import subprocess
import sys

ROOT = pathlib.Path(__file__).resolve().parents[2]
LEDGER = ROOT / "docs/balance"

UNIT_FIELDS = ("cost", "hp", "speed", "speed_air", "turn_speed", "sight",
               "build_limit", "build_duration", "self_heal_step",
               "firepower_multiplier")
WEAPON_FIELDS = {"reloaddelay": "ReloadDelay", "burst": "Burst",
                 "burstdelays": "BurstDelays", "range": "Range",
                 "minrange": "MinRange"}


class YamlEditor:
    """Line-surgical editor for miniyaml blocks; one instance per file."""

    def __init__(self, path: pathlib.Path):
        self.path = path
        self.lines = path.read_text(encoding="utf-8-sig").split("\n")
        self.dirty = False

    def _block(self, key: str) -> tuple[int, int] | None:
        start = None
        for i, line in enumerate(self.lines):
            if start is None:
                if line.split(":")[0].strip() == key and line and \
                        (line[0].isalnum() or line[0] in "^_"):
                    start = i
            else:
                if line and (line[0].isalnum() or line[0] in "^_") and ":" in line:
                    return start, i
        return (start, len(self.lines)) if start is not None else None

    def set_field(self, block_key: str, trait: str, field: str, value) -> str:
        span = self._block(block_key)
        if span is None:
            return f"block `{block_key}` not found"
        s, e = span
        ti = None
        for i in range(s + 1, e):
            l = self.lines[i]
            if l.startswith("\t") and not l.startswith("\t\t") and \
                    l.strip().split(":")[0] == trait:
                ti = i
            elif ti is not None and l.startswith("\t") and not l.startswith("\t\t"):
                break
            elif ti is not None and l.startswith("\t\t"):
                m = re.match(rf"^(\t\t{re.escape(field)}:\s*)(.*)$", l)
                if m:
                    old = m.group(2).strip()
                    if old == str(value):
                        return "unchanged"
                    self.lines[i] = m.group(1) + str(value)
                    self.dirty = True
                    return f"{old} -> {value}"
        # Insert a new unqualified FirepowerMultiplier block if requested and missing
        if trait == "FirepowerMultiplier":
            self.lines.insert(e, f"\tFirepowerMultiplier:")
            self.lines.insert(e + 1, f"\t\t{field}: {value}")
            self.dirty = True
            return f"inserted {field} {value}"
        return f"`{trait}.{field}` not written locally"

    def set_weapon_field(self, weapon: str, field: str, value) -> str:
        """Top-level weapon field (single-tab indent). Inserts if missing."""
        span = self._block(weapon)
        if span is None:
            return f"weapon `{weapon}` not found"
        s, e = span
        for i in range(s + 1, e):
            m = re.match(rf"^(\t{re.escape(field)}:\s*)(.*)$", self.lines[i])
            if m:
                old = m.group(2).strip()
                if old == str(value):
                    return "unchanged"
                self.lines[i] = m.group(1) + str(value)
                self.dirty = True
                return f"{old} -> {value}"
        # Field is not defined locally: insert right after the weapon header
        self.lines.insert(s + 1, f"\t{field}: {value}")
        self.dirty = True
        return f"inserted {field} {value}"

    def set_warhead_damage(self, weapon: str, tag: str, value) -> str:
        span = self._block(weapon)
        if span is None:
            return f"weapon `{weapon}` not found"
        s, e = span
        wi = None
        for i in range(s + 1, e):
            l = self.lines[i]
            if re.match(rf"^\tWarhead@{re.escape(tag)}:", l):
                wi = i
            elif wi is not None and l.startswith("\t") and not l.startswith("\t\t"):
                break
            elif wi is not None:
                m = re.match(r"^(\t\tDamage:\s*)(.*)$", l)
                if m:
                    old = m.group(2).strip()
                    if old == str(value):
                        return "unchanged"
                    self.lines[i] = m.group(1) + str(value)
                    self.dirty = True
                    return f"{old} -> {value}"
        return f"warhead `{tag}` Damage not found"

    def save(self):
        if self.dirty:
            self.path.write_text("\n".join(self.lines), encoding="utf-8", newline="\n")


def fresh_ledgers(only):
    """Fresh in-memory extraction — write-back only writes values whose
    ledger entry differs from the RESOLVED game value, so shadowed or
    order-sensitive definitions (resolved != defining line) are never
    auto-edited; they are reported instead."""
    sys.path.insert(0, str(ROOT / "tools/balance"))
    sys.path.insert(0, str(ROOT / "tools/audit"))
    import extract_stats
    from cameo_model import Model
    return extract_stats.build_ledgers(Model(), only)


def main() -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument("--faction", help="ledger-name substring filter")
    ap.add_argument("--confirm", action="store_true", help="actually write yaml")
    args = ap.parse_args()

    fresh = fresh_ledgers(args.faction)

    def resolved_unit(ledger_name, section, actor):
        return ((fresh.get(ledger_name) or {}).get("sections", {})
                .get(section, {}).get(actor))

    editors: dict[pathlib.Path, YamlEditor] = {}

    def editor(relfile: str) -> YamlEditor:
        p = ROOT / relfile
        if p not in editors:
            editors[p] = YamlEditor(p)
        return editors[p]

    changed, skipped_inherited, problems = 0, 0, []
    for jf in sorted(LEDGER.glob("*.json")):
        if jf.name == "class_anchors.json":
            continue
        doc = json.loads(jf.read_text(encoding="utf-8"))
        if args.faction and args.faction not in doc["ledger"]:
            continue
        for section, sec in doc["sections"].items():
            for actor, u in sec.items():
                ru = resolved_unit(doc["ledger"], section, actor) or {}
                for field in UNIT_FIELDS:
                    slot = u.get(field)
                    if not isinstance(slot, dict):
                        continue
                    rslot = ru.get(field) or {}
                    if str(slot.get("v")) == str(rslot.get("v")):
                        continue  # ledger == resolved game value: nothing to do
                    src = slot.get("src", "")
                    if src == "inherited":
                        skipped_inherited += 1
                        continue
                    if "#" not in src:
                        continue
                    relfile, anchor = src.split("#", 1)
                    trait, _, tfield = anchor.partition(".")
                    if field == "firepower_multiplier":
                        # OpenRA stores the Modifier as an integer percentage, e.g. 89 = 89%.
                        val = int(round(slot["v"] * 100))
                    else:
                        val = slot["v"]
                    res = editor(relfile).set_field(actor, trait, tfield, val)
                    if res == "unchanged":
                        problems.append(f"{actor}.{field}: SHADOWED — resolved "
                                        f"{rslot.get('v')} != defining line {slot['v']} "
                                        f"(duplicate definition? fix by hand)")
                        continue
                    if "->" in res:
                        print(f"  {actor}.{field} [{relfile}]: {res}")
                        changed += 1
                    else:
                        problems.append(f"{actor}.{field}: {res}")
                rarms = {a.get("slot"): a for a in ru.get("armaments", [])}
                for arm in u.get("armaments", []):
                    wfile, wname = arm.get("defined_in"), arm.get("weapon")
                    if not wfile or not wname:
                        continue
                    rarm = rarms.get(arm.get("slot")) or {}
                    for lkey, ykey in WEAPON_FIELDS.items():
                        if lkey not in arm:
                            continue
                        if str(arm.get(lkey)) == str(rarm.get(lkey)):
                            continue
                        res = editor(wfile).set_weapon_field(wname, ykey, arm[lkey])
                        if res == "unchanged":
                            problems.append(f"{actor}/{wname}.{ykey}: SHADOWED "
                                            f"(resolved {rarm.get(lkey)} != defining line)")
                            continue
                        if "->" in res:
                            print(f"  {actor}/{wname}.{ykey} [{wfile}]: {res}")
                            changed += 1
                    rwh = {w.get("tag"): w for w in rarm.get("damage_warheads", [])}
                    for w in arm.get("damage_warheads", []):
                        if str(w.get("damage")) == str((rwh.get(w["tag"]) or {}).get("damage")):
                            continue
                        res = editor(wfile).set_warhead_damage(wname, w["tag"], w["damage"])
                        if res == "unchanged":
                            problems.append(f"{actor}/{wname} Warhead@{w['tag']}: SHADOWED")
                            continue
                        if "->" in res:
                            print(f"  {actor}/{wname} Warhead@{w['tag']}.Damage [{wfile}]: {res}")
                            changed += 1

    for p in problems[:10]:
        print(f"  WARN {p}")
    if args.confirm:
        for ed in editors.values():
            ed.save()
        print(f"APPLIED: {changed} values written "
              f"({skipped_inherited} inherited stats skipped).")
        print("Auto-running extract_stats.py to refresh ledgers...")
        subprocess.run([sys.executable, str(ROOT / "tools" / "balance" / "extract_stats.py"), str(ROOT)], cwd=ROOT)
        print("Auto-running audit_multiplier_modifiers.py...")
        subprocess.run([sys.executable, str(ROOT / "tools" / "audit" / "audit_multiplier_modifiers.py")], cwd=ROOT)
        print("Re-run the full audit suite (tools/audit/run_all.sh or tools/audit/run_all.py) "
              "and the boot gate before committing.")
    else:
        print(f"DRY RUN: {changed} values would change "
              f"({skipped_inherited} inherited stats skipped). "
              f"Re-run with --confirm on maintainer order.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
