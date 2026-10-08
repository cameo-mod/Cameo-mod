"""Seed merged_bot_modules.json's `trait_families` section from the frozen trait capture.

TRAIT-U0 (SPEC_2026-10-05_trait_unification.md §6): the migration manifest has one entry per
family with variants, source SHA, direct field schema + old/default-donor values, field aliases,
semantic-mode notes, affected files and the alias-removal phase. The frozen capture produced for
the spec is the only input; this script makes the seed reproducible instead of hand-copied:

  python tools/audit/gen_trait_families.py [CAPTURE_JSON]

CAPTURE_JSON defaults to the spec's frozen archive export (see --help). The script REWRITES only
the `trait_families` key of merged_bot_modules.json — `_doc`/`merged` are preserved byte-for-byte
in structure (audit_merged_bot_modules.py's parent-hash gate is untouched).

A family is emitted when it is a real unification candidate: at least two distinct LOADED
(non-donor) declarations share its (kind, stem), or one of its members is registered in
trait_aliases.json / the explicit KNOWN_PAIRS / cross-name groups. Upstream-shadow groups
(loaded type vs same-named upstream donor only) are not unification families and are skipped.
"""

from __future__ import annotations

import argparse
import collections
import json
import pathlib
import re
import sys

REPO = pathlib.Path(__file__).resolve().parents[2]
DEFAULT_CAPTURE = pathlib.Path("C:/cameo-wt/boss_trait_full_capture.json")
MANIFEST = REPO / "tools" / "audit" / "merged_bot_modules.json"
ALIASES = REPO / "tools" / "audit" / "trait_aliases.json"

# FindType order at the pinned revision (ObjectCreator prepends OpenRA.Game; spec §3).
ASM_ORDER = ["Game", "Unified", "AS", "CA", "Cameo", "Cnc", "D2k", "Common", "Fransbot"]

# Different-name members that belong to one family (spec §8.1 D3/D4/D5 + type_merge_inventory.py
# KNOWN_PAIRS). yaml names -> family key. Anything not listed groups by its capture stem.
EXPLICIT_MEMBERS = {
    "mcvexpansion (BEV)": ["BevManagerBotModule", "McvExpansionManagerBotModule", "McvManagerBotModuleCA",
                           "McvManagerBotModule", "McvManagerASBotModule", "FransMcvExpansionManagerBotModule"],
    "engineer capture": ["CaptureManagerBotModuleCA", "CncEngineerBotModule", "CncEngineerManagerBotModule",
                         "CaptureManagerBotModule", "CaptureManagerBotASModule"],
    "attackgarrisoned (trait)": ["AttackGarrisonedSP", "AttackOpenTopped", "AttackGarrisoned"],
    "projectilehusk (projectile)": ["ProjectileHusk", "ProjetcileHusk"],
}

# Per-family notes distilled from spec §4.x (design review surface, not exhaustively complete).
SEMANTIC_NOTES = {
    "missile (projectile)": [
        "TA adds slowdown/lookahead/lock-on/jam/altitude-cutoff options; CA adds LockOnBurstCounts, "
        "TrailSpacing, DirectFireMaxRange, singular PointDefenseType -> plural PointDefenseTypes. "
        "Keep Common defaults for its 477 resolved weapons; TA guidance stays an explicit mode. "
        "MissileCA is dormant-only (152 literals, 22 unmounted files) pending D1."],
    "bullet (projectile)": [
        "AS adds shadow palette/altitude detonation/point-defense; ValidBounceBlockerStances maps to "
        "Common ValidBounceBlockerRelationships preserving the bit mask."],
    "laserzap (projectile)": [
        "Vendored CA adds target.Y-source.Y beam Z offsets and Owner.Color; Common adds GlowIntensity/"
        "flare controls. TargetDepthOffset + UseOwnerColorOverride become explicit semantic modes."],
    "warheadtrailprojectile (projectile)": [
        "Equal field schemas hide different logic: CA passes the firing weapon and routes Impact via "
        "configured WeaponInfo, suppressing AS's lifespan explosion loop."],
    "attackgarrisoned (trait)": [
        "D4: vendored CA copy adds PerPassengerTargeting; SP donor uses per-port FirePortSP geometry. "
        "Union schema must carry both mechanisms (GARRISON-IMPL lands it in Common)."],
    "grantconditiononbotowner (trait)": [
        "Empty Bots means ALL bots in CA; base requires membership. Add MatchAllBotsWhenEmpty and "
        "preserve ownership-change IsBot checks."],
    "grantconditiononprerequisite (trait)": [
        "D6: CA registers on add/remove-world and defers condition updates to frame end vs base "
        "creation/disposal. Port manager+trait pair together."],
    "faction (trait)": [
        "D2: FactionCA adds Game=null to core FactionInfo; a mod-side shadow cannot beat OpenRA.Game "
        "precedence. Needs the canonical engine patch first."],
    "mcvexpansion (BEV)": [
        "D3: different-name expansion-owner family. Preserve yard count/cadence options (including old "
        "misspellings), anchor claims, expansion leases, refinery law. One expansion owner per bot."],
    "engineer capture": [
        "D3: engineer/capture variants share the job under different names; canonical union name is open."],
}


def rel_use_path(p: str) -> str:
    """Turn a capture-machine path into a repo-relative one for stable review diffs."""
    p = p.replace("\\", "/")
    i = p.find("mods/cameo")
    return p[i:] if i >= 0 else p


def build_families(capture: dict, aliases: dict) -> dict:
    alias_old = {a["old"]: a for a in aliases["aliases"]}
    alias_new = {a["new"] for a in aliases["aliases"] if a["new"]}
    member_to_family = {}
    for fam, members in EXPLICIT_MEMBERS.items():
        for m in members:
            member_to_family[m] = fam

    by_family = collections.defaultdict(list)
    for t in capture["types"]:
        fam = member_to_family.get(t["yaml"], f"{t['stem']} ({t['kind']})")
        by_family[fam].append(t)

    uses = capture["uses"]
    shas = capture["source_hashes"]
    out = {}
    for fam, members in sorted(by_family.items()):
        distinct = {(m["asm"], m["name"]) for m in members}
        loaded = [m for m in members if not str(m["asm"]).startswith("ref:")]
        loaded_distinct = {(m["asm"], m["name"]) for m in loaded}
        tagged = [m for m in members if m["yaml"] in alias_old or m["yaml"] in alias_new]
        if len(loaded_distinct) < 2 and not tagged and fam not in EXPLICIT_MEMBERS:
            continue

        variants = []
        fields = {}
        files = set()
        decisions = set()
        for m in sorted(members, key=lambda x: (x["asm"] not in ASM_ORDER and 99 or ASM_ORDER.index(x["asm"]), x["asm"], x["yaml"])):
            key = f"{m['name']}"
            variants.append({
                "yaml": m["yaml"], "class": key, "asm": m["asm"], "file": m["file"],
                "sha": shas.get(m["file"]), "use": len(uses.get(m["kind"], {}).get(m["yaml"], [])),
                "ref": bool(m["ref"]),
            })
            fields[key] = m["fields"]
            for use in uses.get(m["kind"], {}).get(m["yaml"], []):
                files.add(rel_use_path(use[2]))
            if m["yaml"] in alias_old and alias_old[m["yaml"]].get("decision"):
                decisions.add(alias_old[m["yaml"]]["decision"])

        live = [v for v in variants if not v["ref"]]
        donor = None
        if live:
            with_use = [v for v in live if v["use"] > 0]
            pool = with_use or live
            donor = min(pool, key=lambda v: ASM_ORDER.index(v["asm"]) if v["asm"] in ASM_ORDER else 99)["yaml"]

        entry = {
            "canonical": next((a["new"] for a in aliases["aliases"]
                               if a["family"] == fam and a["new"]), None),
            "variants": variants,
            "fields": fields,
            "default_donor": donor,
            "field_aliases": {},
            "semantic_notes": SEMANTIC_NOTES.get(fam, []),
            "affected_files": sorted(files),
            "alias_removal_phase": "U7",
            "decisions": sorted(decisions),
        }
        out[fam] = entry
    return out


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__)
    ap.add_argument("capture", nargs="?", default=str(DEFAULT_CAPTURE),
                    help="frozen trait capture JSON (default: the spec's archive export)")
    args = ap.parse_args()

    capture_path = pathlib.Path(args.capture)
    if not capture_path.is_file():
        print(f"FAIL: capture not found: {capture_path}", file=sys.stderr)
        return 1
    capture = json.loads(capture_path.read_text(encoding="utf-8-sig", errors="replace"))
    aliases = json.loads(ALIASES.read_text(encoding="utf-8"))

    families = build_families(capture, aliases)
    manifest = json.loads(MANIFEST.read_text(encoding="utf-8"))
    manifest["trait_families"] = families
    manifest["_doc_trait_families"] = (
        "TRAIT-U0 (SPEC_2026-10-05_trait_unification §6): one entry per unification family — variants "
        "with capture source SHA, direct field schema, default donor, field aliases, semantic-mode "
        "notes, affected files and the alias-removal phase. Regenerate: python tools/audit/"
        "gen_trait_families.py <capture.json>. Schema enforced by audit_merged_bot_modules.py.")
    MANIFEST.write_text(json.dumps(manifest, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")
    n_variants = sum(len(e["variants"]) for e in families.values())
    print(f"wrote {MANIFEST.name}: {len(families)} trait families, {n_variants} variants")
    return 0


if __name__ == "__main__":
    sys.exit(main())
