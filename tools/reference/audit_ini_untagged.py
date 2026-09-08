#!/usr/bin/env python3
"""audit_ini_untagged.py — classify untagged rows in the INI corpus.

DAWN's buildability fix means most untagged rows are now honest
neutral/campaign/decorative/no-production-claim actors. This script
produces a per-source breakdown so the fleet can see whether any
still-buildable untagged rows remain and where the next extraction fix
should go.

Run:
    python tools/reference/audit_ini_untagged.py
"""

import json
import pathlib
import sys
from collections import defaultdict

CORPUS = pathlib.Path(__file__).parent.parent.parent / "docs" / "reference" / "ini_corpus.json"


def classify(row: dict) -> str:
    if row.get("buildable"):
        return "buildable_untagged"
    if row.get("cost") is not None:
        return "costed_not_buildable"
    if row.get("prerequisite"):
        return "prerequisite_no_owner"
    return "no_production_claim"


def main() -> int:
    if not CORPUS.exists():
        print(f"missing {CORPUS}", file=sys.stderr)
        return 1

    rows = []
    with CORPUS.open(encoding="utf-8") as f:
        for line in f:
            line = line.strip()
            if not line:
                continue
            rows.append(json.loads(line))

    by_source = defaultdict(list)
    for r in rows:
        if not r.get("owners"):
            by_source[r["source"]].append(r)

    print("# INI corpus untagged breakdown\n")
    print("| source | total | buildable | costed_not_buildable | prereq_no_owner | no_production_claim |")
    print("|---|---|---|---|---|---|")
    summary = {}
    for s, rs in sorted(by_source.items()):
        buckets = defaultdict(list)
        for r in rs:
            buckets[classify(r)].append(r)
        summary[s] = buckets
        print(f"| {s} | {len(rs)} | {len(buckets['buildable_untagged'])} | "
              f"{len(buckets['costed_not_buildable'])} | {len(buckets['prerequisite_no_owner'])} | "
              f"{len(buckets['no_production_claim'])} |")

    print("\n## Buildable but untagged (actionable)\n")
    any_actionable = False
    for s, buckets in sorted(summary.items()):
        actionable = buckets["buildable_untagged"]
        if actionable:
            any_actionable = True
            print(f"### {s} ({len(actionable)})\n")
            for r in actionable:
                print(f"- `{r['id']}` | {r.get('name', '')} | type={r.get('type')} | "
                      f"cost={r.get('cost')} | tech={r.get('tech_level')} | "
                      f"prereq={r.get('prerequisite')}")
            print()
    if not any_actionable:
        print("None. All buildable rows now have resolved owners.\n")

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
