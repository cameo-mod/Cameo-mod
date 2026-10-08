"""Merged bot modules stay current with their parents (DESIGN §19.3).

A merged module (e.g. `BaseRepairBotModule` = OpenRA `BuildingRepairBotModule` + CA
`BuildingRepairBotModuleCA`) is a SEPARATE file: an upstream fix to a parent lands in the parent's
copy (CA via `ca_vendor_sync.py`, OpenRA/AS via the engine pin) and never reaches the merge by itself.
The parents stay in the tree verbatim (`classic` runs the CA copies), so their content is the signal.

`merged_bot_modules.json` records, per merged module, each parent file and the hash of its content at
the time of the merge. When a parent's hash changes, this audit FAILS until someone ports the change
into the merged module (or rules it irrelevant) and re-baselines:

  python tools/audit/audit_merged_bot_modules.py            # check
  python tools/audit/audit_merged_bot_modules.py --write    # after porting: record today's parents

Hashes ignore line endings, so a checkout's CRLF/LF setting cannot trip it. A parent under `engine/`
is missing in a tree without a built engine: it is reported UNVERIFIED (the run says how many were
checked), never counted as a pass.
"""

from __future__ import annotations

import argparse
import hashlib
import json
import pathlib
import sys

REPO = pathlib.Path(__file__).resolve().parents[2]
MANIFEST = REPO / "tools" / "audit" / "merged_bot_modules.json"

# TRAIT-U0: schema the gen_trait_families.py seeder must satisfy. The check is structural only —
# family variants may live outside this tree (donor refs) so no file existence/hash gate applies.
FAMILY_KEYS = {"canonical", "variants", "fields", "default_donor", "field_aliases",
               "semantic_notes", "affected_files", "alias_removal_phase", "decisions"}
VARIANT_KEYS = {"yaml", "class", "asm", "file", "sha", "use", "ref"}


def content_hash(path: pathlib.Path) -> str:
    return hashlib.sha256(path.read_bytes().replace(b"\r\n", b"\n")).hexdigest()[:16]


def validate_trait_families(manifest: dict) -> list[str]:
    failures = []
    families = manifest.get("trait_families")
    if families is None:
        return []
    if not isinstance(families, dict):
        return ["trait_families must be an object keyed by family name"]
    for name, entry in families.items():
        if not isinstance(entry, dict):
            failures.append(f"{name}: entry is not an object")
            continue
        missing = FAMILY_KEYS - set(entry)
        if missing:
            failures.append(f"{name}: missing keys {sorted(missing)}")
            continue
        variants = entry["variants"]
        if not isinstance(variants, list) or not variants:
            failures.append(f"{name}: variants must be a non-empty list")
            continue
        for v in variants:
            if not isinstance(v, dict) or VARIANT_KEYS - set(v):
                failures.append(f"{name}: variant {v!r} missing keys {sorted(VARIANT_KEYS - set(v))}")
                continue
            if v["ref"] is False and v["sha"] is None:
                failures.append(f"{name}: loaded variant {v['class']} has no source sha")
            if not isinstance(v["use"], int) or v["use"] < 0:
                failures.append(f"{name}: variant {v['class']} has invalid use count")
        classes = {v["class"] for v in variants if isinstance(v, dict)}
        for cls in entry["fields"]:
            if cls not in classes:
                failures.append(f"{name}: fields recorded for non-variant {cls}")
        donor = entry["default_donor"]
        if donor is not None and donor not in {v["yaml"] for v in variants if not v.get("ref")}:
            failures.append(f"{name}: default_donor {donor!r} is not a loaded variant")
        if not isinstance(entry["semantic_notes"], list) or not isinstance(entry["affected_files"], list):
            failures.append(f"{name}: semantic_notes/affected_files must be lists")
        if not isinstance(entry["field_aliases"], dict) or not isinstance(entry["decisions"], list):
            failures.append(f"{name}: field_aliases must be an object, decisions a list")
        if not isinstance(entry["alias_removal_phase"], str):
            failures.append(f"{name}: alias_removal_phase must be a string")
    return failures


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__)
    ap.add_argument("--write", action="store_true", help="record the current parent hashes (after porting)")
    args = ap.parse_args()

    manifest = json.loads(MANIFEST.read_text(encoding="utf-8"))
    failures = validate_trait_families(manifest)
    unverified, checked = [], 0
    for name, entry in manifest["merged"].items():
        merged = REPO / entry["file"]
        if not merged.is_file():
            failures.append(f"{name}: merged module file {entry['file']} is gone - update {MANIFEST.name}")
            continue
        for parent in entry["parents"]:
            path = REPO / parent["file"]
            if not path.is_file():
                unverified.append(f"{name}: parent {parent['file']} not in this tree (engine not built?)")
                continue
            now = content_hash(path)
            if args.write:
                parent["sha"] = now
            elif now != parent["sha"]:
                failures.append(f"{name}: parent {parent['file']} changed since the merge ({parent['sha']} -> {now}) - "
                                f"port the change into {entry['file']}, then run with --write")
            checked += 1

    if args.write:
        MANIFEST.write_text(json.dumps(manifest, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")
        print(f"wrote {MANIFEST.name}: {checked} parent hash(es) recorded")
        for f in failures:
            print(f"FAIL: {f}")
        return 0 if not unverified and not failures else 1

    for u in unverified:
        print(f"UNVERIFIED: {u}")
    for f in failures:
        print(f"FAIL: {f}")
    total = sum(len(e["parents"]) for e in manifest["merged"].values())
    if failures:
        print(f"{len(failures)} manifest/parent failure(s)")
        return 1
    fams = manifest.get("trait_families") or {}
    print(f"PASS: {len(manifest['merged'])} merged module(s), {checked} of {total} parent file(s) verified unchanged, "
          f"{len(fams)} trait families schema-checked" + (" - the rest UNVERIFIED" if unverified else ""))
    return 0


if __name__ == "__main__":
    sys.exit(main())
