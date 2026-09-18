#!/usr/bin/env python3
"""apply_harvester_durability.py — classic-four harvester durability batch (2026-09-18).

Applies the 2026-09-16 reference-map targets (docs/audit/latest/
Cameo-reference-map-original-four-20260916.html, "chassis-only" rows,
weapon column WITHHELD 0/n sources) for the five classic-four economy
units, per the 2026-09-15 accepted-batch conventions:

    HP -> 1,000 grid; speed -> 1; cost -> 10 grid; heal/repair steps
    scaled by the same ratio so F1/F2 (audit_stat_formulas.py) stay
    exact: Repairable.HpPerStep == HP/20, ChangesHealth@SelfHealing
    .Step == HP/2500, and F8 pins Mobile.TurnSpeed == round(Speed/5).

The shared templates (^TDHARV, ^RAHARV) are NOT edited: ^TDHARV also
feeds the four Tiberian Sun harvesters and ^RAHARV also feeds japan's
japaneseoretruck, neither of which is in the classic-four scope.
Instead each of the five actors receives per-actor child blocks in its
own yaml, the same "materialized override" shape the engine already
uses on td_nod_stealthharvester and ra1_soviets_heavyindustrialminer.

Batch table (unsnapped target -> applied):
    td_gdi_tiberiumharvester   239,813 -> 240,000  speed 69  cost 1,670
    td_nod_tiberiumharvester   239,813 -> 240,000  speed 69  cost 1,670
    td_nod_stealthharvester    175,085 -> 175,000  speed 77  cost 1,520
    ra1_allies_alliedoretruck  209,944 -> 210,000  speed 81  cost 1,560
    ra1_soviets_oretruck       209,944 -> 210,000  speed 81  cost 1,560

Heal/repair scaling keeps time-to-full and repair time-to-full constant:
    td harvesters      Step 60 -> 96   HpPerStep 7,500 -> 12,000
    stealth harvester  Step 50 -> 70   HpPerStep 6,250 ->  8,750
    ra1 ore trucks     Step 40 -> 84   HpPerStep 5,000 -> 10,500
TurnSpeed follows the F8 law: 60->69 => 12->14, 75->77 => 15, 90->81
=> 18->16.

Idempotent: every patched field must land on its listed pre-edit value
(EXPECTED_OLD) or the script exits nonzero WITHOUT writing that file, so
an unexpected yaml state is refused loudly instead of silently clobbered.
This is NOT an apply_balance pass on purpose: inherited-src edits still
need per-actor materialization. The extract/apply pipeline now carries
the named self-heal and repair fields, but it cannot materialize an
inherited field. This standalone writer uses the same transaction layer
as apply_balance and validates every file before any replacement.
 Dry/confirm distinction is not needed: this script only
materializes the batch the reference map already computed; the commit
carries the playtest report and Aedis's review is the gate.
"""
from __future__ import annotations

import pathlib
import re
import sys

from apply_transaction import ApplyError, Transaction

ROOT = pathlib.Path(__file__).resolve().parents[2]

GDI_VEH = ROOT / "mods/cameo/ContentPacks/TiberianDawn/GDI/yaml/vehicles.yaml"
NOD_VEH = ROOT / "mods/cameo/ContentPacks/TiberianDawn/Nod/yaml/vehicles.yaml"
US_VEH = ROOT / "mods/cameo/ContentPacks/RedAlert/Allies/yaml/vehicles.yaml"
SOV_VEH = ROOT / "mods/cameo/ContentPacks/RedAlert/Soviets/yaml/vehicles.yaml"

SPECS = {
    "td_gdi_tiberiumharvester": (
        GDI_VEH,
        None,
        {"Valued": {"Cost": 1670},
         "Mobile": {"Speed": 69, "TurnSpeed": 14},
         "Health": {"HP": 240000},
         "Repairable": {"HpPerStep": 12000},
         "ChangesHealth@SelfHealing": {"Step": 96}},
    ),
    "td_nod_tiberiumharvester": (
        NOD_VEH,
        None,
        {"Valued": {"Cost": 1670},
         "Mobile": {"Speed": 69, "TurnSpeed": 14},
         "Health": {"HP": 240000},
         "Repairable": {"HpPerStep": 12000},
         "ChangesHealth@SelfHealing": {"Step": 96}},
    ),
    "td_nod_stealthharvester": (
        NOD_VEH,
        None,
        {"Valued": {"Cost": 1520},
         "Mobile": {"Speed": 77},
         "Health": {"HP": 175000},
         "Repairable": {"HpPerStep": 8750},
         "ChangesHealth@SelfHealing": {"Step": 70}},
    ),
    "ra1_allies_alliedoretruck": (
        US_VEH,
        None,
        {"Valued": {"Cost": 1560},
         "Mobile": {"Speed": 81, "TurnSpeed": 16},
         "Health": {"HP": 210000},
         "Repairable": {"HpPerStep": 10500},
         "ChangesHealth@SelfHealing": {"Step": 84}},
    ),
    "ra1_soviets_oretruck": (
        SOV_VEH,
        None,
        {"Valued": {"Cost": 1560},
         "Mobile": {"Speed": 81, "TurnSpeed": 16},
         "Health": {"HP": 210000},
         "Repairable": {"HpPerStep": 10500},
         "ChangesHealth@SelfHealing": {"Step": 84}},
    ),
}


# Pre-edit yaml values that each PATCHED field must carry. A patch target
# that is not on one of these values refuses the run loudly (review the
# playtest doc before force-extending this table).
EXPECTED_OLD = {
    "td_nod_stealthharvester": {
        "Mobile.Speed": "75",
        "Health.HP": "125000",
        "Repairable.HpPerStep": "6250",
        "ChangesHealth@SelfHealing.Step": "50",
    },
}


class ActorEditor:
    def __init__(self, path: pathlib.Path):
        raw = path.read_bytes()
        self.bom = raw.startswith(b"\xef\xbb\xbf")
        text = raw.decode("utf-8-sig")
        self.crlf = "\r\n" in text
        self.lines = text.replace("\r\n", "\n").split("\n")
        self.problems: list[str] = []

    def content(self) -> bytes:
        out = "\n".join(self.lines)
        if self.crlf:
            out = out.replace("\n", "\r\n")
        return (b"\xef\xbb\xbf" if self.bom else b"") + out.encode()

    def span(self, actor: str) -> tuple[int, int, int]:
        """Return (header_index, end_index_exclusive, child_indent_tabs)."""
        start = None
        for i, line in enumerate(self.lines):
            if line.rstrip() == f"{actor}:" :
                start = i
                break
        if start is None:
            raise SystemExit(f"actor {actor} not found")
        end = len(self.lines)
        indent = None
        for j in range(start + 1, len(self.lines)):
            line = self.lines[j]
            if not line.strip():
                continue
            tabs = len(line) - len(line.lstrip("\t"))
            if tabs == 0:
                end = j
                break
            if indent is None:
                indent = tabs
        return start, end, (indent if indent is not None else 1)

    def child_span(self, block, end, child: str, indent: int):
        """Return (child_index, child_end) or None."""
        pat = re.compile(f"^\t{{{indent}}}{re.escape(child)}:")
        i = block
        while i < end:
            m = pat.match(self.lines[i])
            if m:
                j = i + 1
                while j < end:
                    nxt = self.lines[j]
                    if not nxt.strip():
                        j += 1
                        continue
                    t = len(nxt) - len(nxt.lstrip("\t"))
                    if t <= indent:
                        break
                    j += 1
                return i, j
            i += 1
        return None

    def patch_child(self, block, end, indent, name, fields, report, allowed_old):
        span = self.child_span(block, end, name, indent)
        if span is None:
            return False
        ci, ce = span
        sub_indent = indent + 1
        for key, want in fields.items():
            pat = re.compile(f"^(\t{{{sub_indent}}}{re.escape(key)}:)\\s*(.*?)\\s*(#.*)?$")
            hit = None
            for k in range(ci + 1, ce):
                m = pat.match(self.lines[k])
                if m:
                    hit = (k, m)
                    break
            if hit is None:
                self.problems.append(f"{name}.{key}: key absent inside existing child")
                continue
            k, m = hit
            old = m.group(2)
            if old == str(want):
                report.append(f"{name}.{key}: already {want}")
                continue
            # allowed_old is keyed by the SHORT field name (the caller builds
            # it per (actor, child) from the actor-level EXPECTED_OLD rows);
            # deriving the key back to `Actor.Field` here again would look up
            # a key the caller never stored and refuse every pre-vector.
            if old not in allowed_old.get(key, ()):
                self.problems.append(
                    f"{name}.{key}: refusing to overwrite unexpected value {old!r}"
                )
                continue
            self.lines[k] = f"{m.group(1)} {want}"
            report.append(f"{name}.{key}: {old} -> {want}")
        return True

    def insert_child(self, actor, block, end, indent, name, fields, report):
        pad = "\t" * indent
        sub = "\t" * (indent + 1)
        lines = [f"{pad}{name}:"] + [f"{sub}{k}: {v}" for k, v in fields.items()]
        # Insert at the END of the actor block: siblings later in the block
        # win same-key merges, and templates merge in at the `Inherits:`
        # position, so a child placed before `Inherits:` would be silently
        # overridden by the template (seen on this batch: before-Inherits
        # placement failed to resolve). Keep the phantom trailing newline
        # element ("" after split) last, so the file keeps its final
        # newline when the actor is the last one in the file.
        at = min(end, len(self.lines))
        if at == len(self.lines):
            if self.lines and self.lines[-1] == "":
                at -= 1
            else:
                self.lines.append("")
        self.lines[at:at] = lines
        for key, want in fields.items():
            report.append(f"{name}.{key}: added {want}")
        return len(lines)

    def apply(self, actor: str, fields: dict[str, dict[str, int]]):
        allowed = EXPECTED_OLD.get(actor, {})
        report: list[str] = []
        block, end, indent = self.span(actor)
        # Patch existing children first (order-independent).
        remaining = dict(fields)
        for name in list(remaining):
            allow: dict[str, tuple] = {}
            for key in remaining[name]:
                pre = allowed.get(f"{name}.{key}")
                if pre is not None:
                    allow[key] = (pre,)
            if self.child_span(block, end, name, indent) is not None:
                self.patch_child(block, end, indent, name, remaining.pop(name), report, allow)
        # Insert the children the actor does not own yet.
        for name, vals in remaining.items():
            end += self.insert_child(actor, block, end, indent, name, vals, report)
        return report


def main() -> int:
    by_file: dict[pathlib.Path, list[tuple[str, dict]]] = {}
    for actor, (path, _, fields) in SPECS.items():
        by_file.setdefault(path, []).append((actor, fields))
    rc = 0
    pending: list[tuple[pathlib.Path, bytes]] = []
    for path, actors in by_file.items():
        ed = ActorEditor(path)
        for actor, fields in by_file[path]:
            report = ed.apply(actor, fields)
            print(f"-- {actor}")
            for line in report:
                print(f"   {line}")
        if ed.problems:
            rc = 1
            for p in ed.problems:
                print(f"   PROBLEM {p}")
            print(f"   REFUSED WRITING {path}")
            continue
        pending.append((path, ed.content()))
    if rc:
        print("REFUSED WRITING: at least one file failed validation; no batch files were written")
        return rc
    transaction = Transaction({path: path.read_bytes() for path, _ in pending})
    try:
        for path, content in pending:
            transaction.write(path, content)
    except BaseException as error:
        conflicts = transaction.rollback()
        print(f"REFUSED WRITING: transaction failed: {error}")
        if conflicts:
            print("ROLLBACK CONFLICTS:", ", ".join(conflicts))
        return 1
    return rc


if __name__ == "__main__":
    sys.exit(main())
