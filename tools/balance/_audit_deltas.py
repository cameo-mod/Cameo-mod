#!/usr/bin/env python3
"""Audit curated rebalance proposals for delta > 1."""
import pathlib, re

ROOT = pathlib.Path(__file__).resolve().parents[2]
REPORTS = [
    ROOT / "docs" / "balance" / "proposal_scout_infantry.md",
    ROOT / "docs" / "balance" / "proposal_closecombat_infantry.md",
    ROOT / "docs" / "balance" / "proposal_special_forces_infantry.md",
]

def parse(path):
    text = path.read_text(encoding="utf-8-sig")
    h = None
    for i, line in enumerate(text.splitlines()):
        if line.startswith("|") and "actor" in line:
            h = i
            break
    if h is None:
        return []
    headers = [c.strip() for c in text.splitlines()[h].split("|")[1:-1]]
    idx = {n: i for i, n in enumerate(headers)}
    rows = []
    for line in text.splitlines()[h+2:]:
        if not line.startswith("|"):
            continue
        cells = [c.strip() for c in line.split("|")[1:-1]]
        if len(cells) != len(headers):
            continue
        actor = cells[idx["actor"]].strip("`")
        if actor in ("actor", ""):
            continue
        delta = cells[idx.get("Δ", idx.get("delta", -1))].strip()
        m = re.search(r"([+-]?\d+)", delta)
        d = int(m.group(1)) if m else 0
        rows.append({"actor": actor, "class": path.stem, "delta": d, "line": line})
    return rows

for r in REPORTS:
    rows = parse(r)
    bad = [x for x in rows if abs(x["delta"]) > 1]
    print(f"\n{r.name}: {len(bad)} of {len(rows)} rows have |delta| > 1")
    for x in bad:
        print(f"  {x['actor']:40s} delta = {x['delta']:+d}")
