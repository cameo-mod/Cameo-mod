#!/usr/bin/env python3
"""The THIRD weapon slot: `Elite=`, which the INI corpus drops entirely.

⛔ THE MAINTAINER HAD TO SAY THIS TWICE, so it is written down here permanently:

    "I already explained this before, it's the elite weapon which replaces the dummy weapon so
     on elite it has one cannon and one missile launcher"

They were right and the extraction was reading two of the unit's four weapon declarations. A TS
unit has `Primary=`, `Secondary=` and `Elite=`; `ini_corpus.json` carries only the first two, as
`w_*` and `w2_*`. **171 DTA sections declare `Elite=` and not one of those weapons reaches the
reference map.** The case that exposed it:

    [MTNK]                                      [70mmMsl1]
    Name=GDI Medium Tank                        Damage=30
    Primary=90mmDummy   ;ROF ... for both       Range=6.14
    Secondary=90mm                              Projectile=AGHeatSeeker
    Elite=70mmMsl1                              Warhead=BazAP

The primary is a zero-damage DUMMY that exists only to set the rate of fire. So the GDI Medium
Tank's real loadout at elite is the 90 mm cannon PLUS a 70 mm missile launcher — a cannon and a
missile, exactly as described, and the reason DTA can vote on a Cameo unit's missile.

⭐ `Trainable=` IS THE GATE, AND THE ENHANCE OVERLAY FLIPS IT. Measured 2026-09-13:

    DTA Classic    171 sections with Elite=,   0 trainable  -> NO elite weapon is reachable
    DTA Enhanced   171 sections with Elite=, 139 trainable

`[MTNK]` is `Trainable=no` in `Rules.ini` and `Trainable=yes` in `Enhance.ini`. So the elite
missile exists in both rulesets and can only ever be FIRED in Enhanced. An extractor that ignored
`Trainable` would hand DTA Classic 139 weapons no unit in that ruleset can reach.

⚠ AND MOST ELITE WEAPONS ARE NOT A SECOND WEAPON AT ALL. 130 of the 139 are the `E`-suffix upgrade
of a weapon the unit already fires (`RaiderCannonE`, `MinigunE`, `HellfireE`, `120mmE`); `Elite=`
REPLACES the primary, so for those it is the same gun, improved. Only where the primary is a
zero-damage dummy does the elite weapon occupy a slot that was otherwise empty — **9 units** —
and only those are a genuinely additional armament. Both facts are recorded per row
(`replaces_dummy_primary`), because the consumer must be able to tell them apart and this file is
the only place the distinction is visible.

    python tools/reference/extract_ini_elite_weapons.py \
        --ini-dir "<reference>/DTA Developer Edition/INI" --write
"""
from __future__ import annotations

import argparse
import hashlib
import json
import pathlib
import sys

sys.path.insert(0, str(pathlib.Path(__file__).resolve().parent))

from extract_ini_units import (read_ini, resolve_inherits, merge_overlay,  # noqa: E402
                               bool_value, num)
# ⛔ THE ROLE IS STAMPED HERE, not looked up later. `extract_ini_projectile_roles` only resolves
# the projectiles the corpus CITES, and it cites `Primary=`/`Secondary=` only — an elite weapon
# flying a projectile no baseline weapon uses would have no verdict and would silently abstain.
# Importing the one `role_of` keeps a single vocabulary with no second copy and no load order
# between the two extractors.
from extract_ini_projectile_roles import (role_of, DEFAULT_AA, DEFAULT_AG,  # noqa: E402
                                          DEFAULT_BASIS)

ROOT = pathlib.Path(__file__).resolve().parents[2]
OUT = ROOT / "docs" / "reference" / "ini_elite_weapon_evidence.json"
CORPUS = ROOT / "docs" / "reference" / "ini_corpus.json"

# Same two byte-pinned sources as `extract_ini_projectile_roles`, and for the same reason: a claim
# about a unit's third weapon is only evidence if it comes from the bytes the other two came from.
PINNED = {
    "DTA Classic": {"rules": "Rules.ini", "overlay": None, "engine": "ts"},
    "DTA Enhanced": {"rules": "Rules.ini", "overlay": "Enhance.ini", "engine": "ts"},
}

# The weapon fields the consumer needs, in the corpus's own spelling.
WEAPON_FIELDS = (("damage", "Damage"), ("range", "Range"), ("reload", "ROF"),
                 ("burst", "Burst"), ("projectile", "Projectile"), ("warhead", "Warhead"))


def sha256(path: pathlib.Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def corpus_provenance():
    out = {}
    for line in CORPUS.read_text(encoding="utf-8").splitlines():
        if not line.strip():
            continue
        row = json.loads(line)
        if row.get("source_sha256"):
            out.setdefault(row["source"], (row["source_sha256"], row.get("overlay_sha256")))
    return out


def corpus_ids(source):
    """The unit ids this source actually contributes, so nothing unreferenced is emitted."""
    ids = set()
    for line in CORPUS.read_text(encoding="utf-8").splitlines():
        if not line.strip():
            continue
        row = json.loads(line)
        if row.get("source") == source and row.get("id"):
            ids.add(str(row["id"]))
    return ids


def projectile_role(ini, projectile):
    """(role, declared) for one projectile section, engine defaults applied — see `role_of`."""
    section = ini.get(projectile or "")
    if section is None:
        return None, []
    declared_aa, declared_ag = section.get("AA"), section.get("AG")
    aa = DEFAULT_AA if declared_aa is None else bool(bool_value(declared_aa))
    ag = DEFAULT_AG if declared_ag is None else bool(bool_value(declared_ag))
    return role_of(aa, ag), sorted(k for k, v in (("AA", declared_aa), ("AG", declared_ag))
                                   if v is not None)


def weapon_record(ini, name):
    section = ini.get(name or "")
    if section is None:
        return None
    rec = {"weapon": name}
    for key, field in WEAPON_FIELDS:
        value = section.get(field)
        if value is None:
            continue
        rec[key] = value if key in ("projectile", "warhead") else num(value)
    role, declared = projectile_role(ini, rec.get("projectile"))
    rec["role"] = role
    rec["role_declared"] = declared
    return rec


def elite_rows(ini, wanted):
    rows, unreachable, missing = {}, [], []
    for unit in sorted(wanted):
        section = ini.get(unit)
        if section is None:
            continue
        elite = (section.get("Elite") or "").strip()
        if not elite or elite.lower() == "none":
            continue
        # ⛔ UNREACHABLE IS NOT THE SAME AS ABSENT, and both are recorded. A unit that declares an
        # elite weapon it can never earn must not vote with it, but the declaration is still a
        # fact about the ruleset and hiding it would make the Classic/Enhanced difference
        # invisible — which is the difference that decides whether DTA votes at all.
        trainable = bool_value(section.get("Trainable"))
        record = weapon_record(ini, elite)
        if record is None:
            missing.append(f"{unit}:{elite}")
            continue
        if not trainable:
            unreachable.append(unit)
            continue
        primary = (section.get("Primary") or "").strip()
        primary_damage = num((ini.get(primary) or {}).get("Damage"), 0) or 0
        rows[unit] = {
            **record,
            "slot": "elite",
            "replaces": primary or None,
            # The one distinction the consumer cannot recover on its own: `Elite=` REPLACES the
            # primary, so it is an additional armament only when the primary was a dummy.
            "replaces_dummy_primary": primary_damage == 0,
            "trainable": True,
        }
    return rows, unreachable, missing


def build(ini_dir: pathlib.Path) -> dict:
    pinned = corpus_provenance()
    sources = []
    for label, spec in PINNED.items():
        if label not in pinned:
            raise SystemExit(f"{label}: the corpus pins no source_sha256 — refusing to guess")
        want_rules, want_overlay = pinned[label]
        rules_path = ini_dir / spec["rules"]
        if not rules_path.exists():
            raise SystemExit(f"{label}: {rules_path} not found")
        got = sha256(rules_path)
        if got != want_rules:
            raise SystemExit(f"{label}: Rules.ini is {got}, corpus pins {want_rules}")
        ini = resolve_inherits(read_ini(rules_path))
        overlay_digest = None
        if spec["overlay"]:
            overlay_path = ini_dir / spec["overlay"]
            if not overlay_path.exists():
                raise SystemExit(f"{label}: overlay {spec['overlay']} missing")
            overlay_digest = sha256(overlay_path)
            if overlay_digest != want_overlay:
                raise SystemExit(f"{label}: {spec['overlay']} is {overlay_digest}, "
                                 f"corpus pins {want_overlay}")
            ini = merge_overlay(ini, resolve_inherits(read_ini(overlay_path)))
        elif want_overlay:
            raise SystemExit(f"{label}: corpus pins an overlay this spec does not apply")
        rows, unreachable, missing = elite_rows(ini, corpus_ids(label))
        sources.append({
            "source": label,
            "engine": spec["engine"],
            "rules_sha256": got,
            "overlay_sha256": overlay_digest,
            "overlay_precedence": "overlay_over_rules" if overlay_digest else "none",
            "reachable": len(rows),
            "declared_but_untrainable": len(unreachable),
            "undeclared_weapon": missing,
            "additional_armament": sum(1 for r in rows.values()
                                       if r["replaces_dummy_primary"]),
            "units": rows,
        })
    return {
        "schema": 1,
        "scope": ("The TS `Elite=` weapon slot, which ini_corpus.json does not carry. Gated on "
                  "`Trainable=`. No damage, range or cadence of the Primary/Secondary slots is "
                  "read, changed or implied."),
        "engine_default": {"AA": DEFAULT_AA, "AG": DEFAULT_AG, "basis": DEFAULT_BASIS},
        "sources": sources,
    }


def load(root=ROOT):
    """{(source, unit_id): elite weapon record} for `armament_roles.ini_views`; {} when absent."""
    path = pathlib.Path(root) / "docs" / "reference" / "ini_elite_weapon_evidence.json"
    if not path.exists():
        return {}
    doc = json.loads(path.read_text(encoding="utf-8"))
    if doc.get("schema") != 1:
        raise ValueError("unsupported INI elite weapon evidence")
    return {(entry["source"], unit): record
            for entry in doc["sources"] for unit, record in entry["units"].items()}


def main(argv=None) -> int:
    ap = argparse.ArgumentParser(description=__doc__,
                                 formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--ini-dir", required=True, type=pathlib.Path)
    ap.add_argument("--write", action="store_true")
    args = ap.parse_args(argv)
    doc = build(args.ini_dir)
    for entry in doc["sources"]:
        print(f"{entry['source']}: {entry['reachable']} reachable elite weapon(s), "
              f"{entry['additional_armament']} of them an ADDITIONAL armament "
              f"(the primary is a dummy); {entry['declared_but_untrainable']} declared but "
              f"untrainable"
              + (f"; UNDECLARED {entry['undeclared_weapon']}"
                 if entry["undeclared_weapon"] else ""))
    if args.write:
        OUT.write_text(json.dumps(doc, indent=1, sort_keys=True) + "\n", encoding="utf-8")
        print(f"wrote {OUT.relative_to(ROOT)}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
