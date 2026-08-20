#!/usr/bin/env python3
"""Analyze check-yaml output and categorize errors/warnings with better pattern extraction."""
import re
import sys
from collections import Counter

def analyze(path):
    errors = []
    warnings = []
    with open(path, encoding="utf-8") as f:
        for line in f:
            line = line.rstrip("\n")
            if line.startswith("Error:"):
                errors.append(line)
            elif line.startswith("Warning:"):
                warnings.append(line)

    print(f"Total errors: {len(errors)}")
    print(f"Total warnings: {len(warnings)}")
    print()

    error_cats = Counter()
    error_details = {}

    for e in errors:
        cat = None
        detail = None

        if "TypeDictionary contains multiple instances of type" in e:
            cat = "TypeDictionary duplicate type"
            m = re.search(r"type `([^`]+)`", e)
            detail = m.group(1) if m else "unknown"
        elif "Undefined palette reference" in e:
            cat = "Undefined palette reference"
            m = re.search(r"reference `([^`]+)`", e)
            detail = m.group(1) if m else "unknown"
        elif "Undefined player palette reference" in e:
            cat = "Undefined player palette reference"
            m = re.search(r"reference `([^`]+)`", e)
            detail = m.group(1) if m else "unknown"
        elif "DuplicateUnitCrateActionInfo.ExcludedActorType" in e:
            cat = "Crate ExcludedActorType missing actor"
            m = re.search(r"Missing actor `([^`]+)`", e)
            detail = m.group(1) if m else "unknown"
        elif "RepairableNearInfo.RepairActors" in e:
            cat = "RepairableNear missing actor"
            m = re.search(r"Missing actor `([^`]+)`", e)
            detail = m.group(1) if m else "unknown"
        elif "TransformsIntoRepairableInfo.RepairActors" in e:
            cat = "TransformsIntoRepairable missing actor"
            m = re.search(r"Missing actor `([^`]+)`", e)
            detail = m.group(1) if m else "unknown"
        elif "SpawnActorOnDeathInfo.Actor" in e:
            cat = "SpawnActorOnDeath missing actor"
            m = re.search(r"Missing actor `([^`]+)`", e)
            detail = m.group(1) if m else "unknown"
        elif "has prereq" in e and "not provided" in e:
            cat = "Unresolved prerequisite"
            m = re.search(r"prereq `([^`]+)` not provided", e)
            detail = m.group(1) if m else "unknown"
        elif "Missing key" in e and "ftl files" in e:
            cat = "Missing FTL key (error)"
            detail = "ftl-key"
        elif "Field `" in e and "of type" in e:
            cat = "Field type mismatch"
            detail = e[:80]
        elif "Sequence" in e and "not found" in e:
            cat = "Missing sequence"
            detail = e[:80]
        elif "Image not found" in e or "File not found" in e:
            cat = "Missing asset file"
            detail = e[:80]
        else:
            cat = "Uncategorized"
            detail = e[:100]

        error_cats[cat] += 1
        if cat not in error_details:
            error_details[cat] = Counter()
        error_details[cat][detail] += 1

    print("=== ERROR CATEGORIES (sorted by count) ===")
    for cat, count in error_cats.most_common():
        print(f"  {count:>6}  {cat}")

    print()
    for cat, count in error_cats.most_common():
        if count < 10:
            break
        print(f"=== {cat} ({count} total) - top details ===")
        for detail, dcount in error_details[cat].most_common(15):
            print(f"  {dcount:>6}  {detail}")
        print()

    warn_cats = Counter()
    warn_details = {}

    for w in warnings:
        cat = None
        detail = None

        if "Missing key" in w and "ftl files" in w:
            cat = "Missing FTL key (warning)"
            m = re.search(r"Missing key `([^`]+)`", w)
            detail = m.group(1)[:60] if m else "unknown"
        elif "grants conditions that are not consumed" in w:
            cat = "Unused granted conditions"
            m = re.search(r"Actor type `([^`]+)`", w)
            detail = m.group(1) if m else "unknown"
        elif "Field `" in w and "of type" in w:
            cat = "Field type warning"
            detail = w[:80]
        elif "is not used" in w or "unused" in w.lower():
            cat = "Unused field/trait"
            detail = w[:80]
        elif "Sequence" in w and "not found" in w:
            cat = "Missing sequence (warning)"
            detail = w[:80]
        elif "Image not found" in w or "File not found" in w:
            cat = "Missing asset (warning)"
            detail = w[:80]
        else:
            cat = "Uncategorized warning"
            detail = w[:100]

        warn_cats[cat] += 1
        if cat not in warn_details:
            warn_details[cat] = Counter()
        warn_details[cat][detail] += 1

    print("=== WARNING CATEGORIES (sorted by count) ===")
    for cat, count in warn_cats.most_common():
        print(f"  {count:>6}  {cat}")

    print()
    for cat, count in warn_cats.most_common():
        if count < 10:
            break
        print(f"=== {cat} ({count} total) - top details ===")
        for detail, dcount in warn_details[cat].most_common(15):
            print(f"  {dcount:>6}  {detail}")
        print()

if __name__ == "__main__":
    analyze(sys.argv[1] if len(sys.argv) > 1 else "docs/audit/check-yaml-baseline.txt")
