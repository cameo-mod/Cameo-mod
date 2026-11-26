#!/usr/bin/env python3
"""CONSOLIDATE-03 P1 (a): the building order-stream hash (CONSOLIDATE_03_P1_DESIGN_01.md). A DIAGNOSTIC, not the parity gate (Opus ruling 11-26):
the gate is OLD build vs NEW build, same seed, the full replay order stream identical (order_stream_diff.py) plus the sync hash. This hash omits
the producer/queue identity and the random draws of no-pick walks; use it to find WHERE a building stream first differs.

Reads the DEV player-event logs (cameo-ai-players-*.jsonl) of a game and hashes, per seat, the ordered bb_pick rows: every admitted building start of
the staged planner (tick, actor, stage, candidate, bank). bb_pick is written for every start and is not rate-limited, and builds before P1 already
write it, so an old build needs no hook. Per seat: SHA-256 over the canonical rows, ordered by (tick, seq) with ties kept in file order. Total:
SHA-256 over the sorted "seat=hash" lines.

  bb_stream_hash.py GAME              print the per-seat and total hashes of one game (a Logs dir, a game dir or a .jsonl file)
  bb_stream_hash.py OLD NEW           compare two games: per seat equal or the first divergent row; exit 0 only when every seat is equal

Read-only. It never launches anything.
"""
from __future__ import annotations

import hashlib
import json
import pathlib
import sys

FIELDS = ("tick", "actor", "stage", "candidate", "bank")


def log_files(arg: str) -> list[pathlib.Path]:
    p = pathlib.Path(arg)
    if p.is_file():
        return [p]
    if (p / "Logs").is_dir():
        p = p / "Logs"
    files = sorted(p.glob("cameo-ai-players-*.jsonl"))
    if not files:
        raise SystemExit(f"no cameo-ai-players-*.jsonl under {arg}")
    return files


def streams(arg: str) -> dict[str, list[str]]:
    rows: dict[str, list[tuple[int, int, int, str]]] = {}
    n = 0
    for f in log_files(arg):
        with f.open(encoding="utf-8") as fh:
            for line in fh:
                if '"bb_pick"' not in line:
                    continue
                try:
                    r = json.loads(line)
                except json.JSONDecodeError:
                    continue
                if r.get("kind") != "bb_pick":
                    continue
                canon = "|".join(str(r.get(k, "")) for k in FIELDS)
                rows.setdefault(str(r.get("seat")), []).append((int(r.get("tick", 0)), int(r.get("seq", 0)), n, canon))
                n += 1
    return {seat: [c for *_, c in sorted(v)] for seat, v in rows.items()}


def digest(lines: list[str]) -> str:
    h = hashlib.sha256()
    for line in lines:
        h.update(line.encode("utf-8"))
        h.update(b"\n")
    return h.hexdigest()


def total(per_seat: dict[str, str]) -> str:
    return digest([f"{s}={per_seat[s]}" for s in sorted(per_seat)])


def report(arg: str) -> dict[str, list[str]]:
    s = streams(arg)
    per = {seat: digest(v) for seat, v in s.items()}
    for seat in sorted(per):
        print(f"{seat} rows={len(s[seat])} sha256={per[seat]}")
    print(f"TOTAL seats={len(per)} sha256={total(per)}")
    return s


def main(argv: list[str]) -> int:
    if len(argv) == 2:
        report(argv[1])
        return 0
    if len(argv) != 3:
        print(__doc__)
        return 2

    print("== OLD", argv[1])
    old = report(argv[1])
    print("== NEW", argv[2])
    new = report(argv[2])
    same = True
    for seat in sorted(set(old) | set(new)):
        a, b = old.get(seat, []), new.get(seat, [])
        if a == b:
            print(f"{seat} EQUAL rows={len(a)}")
            continue
        same = False
        i = next((k for k in range(min(len(a), len(b))) if a[k] != b[k]), min(len(a), len(b)))
        print(f"{seat} DIVERGE at row {i}: old={a[i] if i < len(a) else '<end>'} new={b[i] if i < len(b) else '<end>'}")
    print("VERDICT", "EQUAL" if same else "DIVERGED")
    return 0 if same else 1


if __name__ == "__main__":
    sys.exit(main(sys.argv))
