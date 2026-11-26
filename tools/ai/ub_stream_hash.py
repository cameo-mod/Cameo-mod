#!/usr/bin/env python3
"""CONSOLIDATE-03 P5 equivalence check: the ordered hash of the unit pick trace (design 02 addendum 2, FINISH-01 (a)).

Input: unit_pick_trace.csv written by a DEV build with UnitPickTrace on (no header; columns tick,seat,event,type,queue,detail; the detail never contains a comma).
Per seat the rows keep their file order (the DEV writer appends per seat in call order); the hash is SHA-256 over the rows of one seat joined by newline, and the total is SHA-256 over the
per-seat hashes in seat order. Two builds on the same seed with the same switches must give the same per-seat hashes and the same total; on a difference the first divergent row per seat is printed.

Usage:
  py -I tools/ai/ub_stream_hash.py TRACE.csv             prints the per-seat hashes, row counts and the total
  py -I tools/ai/ub_stream_hash.py OLD.csv NEW.csv       also compares; exit 1 on any difference (first divergent row per seat)
  py -I tools/ai/ub_stream_hash.py --self-test           fixture self-test
"""
import hashlib
import sys


def read_rows(path):
    """Return {seat: [row, ...]} in file order; a blank line is skipped."""
    seats = {}
    with open(path, "r", encoding="utf-8", newline="") as handle:
        for line in handle:
            row = line.rstrip("\r\n")
            if not row:
                continue
            fields = row.split(",", 5)
            if len(fields) < 3:
                continue
            seats.setdefault(fields[1], []).append(row)
    return seats


def seat_hash(rows):
    return hashlib.sha256("\n".join(rows).encode("utf-8")).hexdigest()


def seat_key(seat):
    return (0, int(seat)) if seat.lstrip("-").isdigit() else (1, seat)


def summarize(seats):
    per_seat = {seat: seat_hash(rows) for seat, rows in seats.items()}
    total = hashlib.sha256("".join(per_seat[s] for s in sorted(per_seat, key=seat_key)).encode("utf-8")).hexdigest()
    return per_seat, total


def first_divergence(old_rows, new_rows):
    """Index of the first differing row (the shorter list's length when one is a prefix of the other), or -1 when equal."""
    for index in range(min(len(old_rows), len(new_rows))):
        if old_rows[index] != new_rows[index]:
            return index
    return -1 if len(old_rows) == len(new_rows) else min(len(old_rows), len(new_rows))


def compare(old, new):
    """Return (equal, lines) for two {seat: rows} dictionaries."""
    lines = []
    equal = True
    for seat in sorted(set(old) | set(new), key=seat_key):
        a, b = old.get(seat, []), new.get(seat, [])
        index = first_divergence(a, b)
        if index < 0:
            lines.append("seat %s: equal (%d rows)" % (seat, len(a)))
            continue
        equal = False
        lines.append("seat %s: DIFFERENT at row %d (old %d rows, new %d rows)" % (seat, index, len(a), len(b)))
        lines.append("  old: %s" % (a[index] if index < len(a) else "<end>"))
        lines.append("  new: %s" % (b[index] if index < len(b) else "<end>"))
    return equal, lines


def _self_test():
    old = {"0": ["10,0,ub_draw,e1,Infantry,ord=1;x", "20,0,ub_fill,e1,Infantry,res=accept"], "1": ["15,1,ub_empty,,Vehicle,fill=1"]}
    same = {"0": list(old["0"]), "1": list(old["1"])}
    assert summarize(old) == summarize(same)
    assert compare(old, same)[0]
    changed = {"0": [old["0"][0], "20,0,ub_fill,e2,Infantry,res=accept"], "1": list(old["1"])}
    assert summarize(old)[1] != summarize(changed)[1]
    equal, lines = compare(old, changed)
    assert not equal and "seat 0: DIFFERENT at row 1" in lines[0], lines
    shorter = {"0": old["0"][:1], "1": list(old["1"])}
    assert first_divergence(old["0"], shorter["0"]) == 1
    assert not compare(old, shorter)[0]
    print("ub_stream_hash self-test: PASS")


def main(argv):
    if len(argv) == 2 and argv[1] == "--self-test":
        _self_test()
        return 0

    if len(argv) not in (2, 3):
        print(__doc__)
        return 2

    old = read_rows(argv[1])
    per_seat, total = summarize(old)
    for seat in sorted(per_seat, key=seat_key):
        print("seat %s  rows %d  sha256 %s" % (seat, len(old[seat]), per_seat[seat]))

    print("total sha256 %s" % total)
    if len(argv) == 2:
        return 0

    new = read_rows(argv[2])
    _, new_total = summarize(new)
    equal, lines = compare(old, new)
    print("\n".join(lines))
    print("RESULT: %s (old total %s, new total %s)" % ("EQUAL" if equal else "DIFFERENT", total[:16], new_total[:16]))
    return 0 if equal else 1


if __name__ == "__main__":
    sys.exit(main(sys.argv))
