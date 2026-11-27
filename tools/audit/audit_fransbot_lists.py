#!/usr/bin/env python3
"""audit_fransbot_lists — drift guard for mods/cameo/ai/fransbot_lists.yaml.

The generated file fills the Fransbot modules' [ActorReference] list fields with
actor ids derived from resolved ruleset traits (tools/ai/gen_fransbot_lists.py).
If actor rules change without a regenerate, the lists go stale silently — this
audit reruns the generator in --check mode and FAILS when the committed file
does not match a fresh generate.

Expected-stale (FRANS-PRUNE-01 split): a pending behaviour refresh may be listed in
tools/audit/fransbot_expected_stale.txt as a PAIR per line:
    <sha256 of the committed file> <sha256 of the fresh generate>
Both digests are over LF bytes. The audit passes as EXPECTED-STALE only when the
committed file AND the fresh generate both match a listed pair. Any other drift
fails. Removing the marker (the FRANS-LISTS-REFRESH commit) makes gen --check
strict again.

Fix: python tools/ai/gen_fransbot_lists.py   (then boot-gate, then commit)
"""

from __future__ import annotations

import hashlib
import pathlib
import subprocess
import sys

REPO = pathlib.Path(__file__).resolve().parents[2]
GEN = REPO / "tools" / "ai" / "gen_fransbot_lists.py"
OUT = REPO / "mods" / "cameo" / "ai" / "fransbot_lists.yaml"
MARKER = REPO / "tools" / "audit" / "fransbot_expected_stale.txt"


def lf_digest(data: bytes) -> str:
    return hashlib.sha256(data.replace(b"\r\n", b"\n")).hexdigest()


def expected_pairs() -> set[tuple[str, str]]:
    if not MARKER.exists():
        return set()
    pairs = set()
    for line in MARKER.read_text(encoding="utf8").splitlines():
        parts = line.split()
        if len(parts) >= 2 and not line.lstrip().startswith("#"):
            pairs.add((parts[0].lower(), parts[1].lower()))
    return pairs


def main() -> int:
    print("# Fransbot generated lists — drift check\n")
    if not GEN.exists():
        print(f"FAIL: generator missing: {GEN.relative_to(REPO)}")
        return 1
    if not OUT.exists():
        print(f"FAIL: generated file missing: {OUT.relative_to(REPO)}")
        print("Fix: run `python tools/ai/gen_fransbot_lists.py`")
        return 1
    r = subprocess.run([sys.executable, str(GEN), "--check"],
                       capture_output=True, text=True, cwd=REPO)
    print(r.stdout.strip() or "(no output)")
    if r.returncode == 0:
        print("\nPASS — generated file matches the resolved ruleset")
        return 0

    fresh = subprocess.run([sys.executable, str(GEN), "--stdout"],
                           capture_output=True, cwd=REPO)
    if fresh.returncode != 0:
        print(fresh.stderr.decode("utf8", "replace").strip())
        print("\nFAIL — the generator could not produce a fresh file")
        return 1
    pair = (lf_digest(OUT.read_bytes()), lf_digest(fresh.stdout))
    if pair in expected_pairs():
        print(f"\nEXPECTED-STALE — committed sha256 {pair[0][:8]} and fresh sha256 {pair[1][:8]} "
              f"are a listed pair in {MARKER.relative_to(REPO)} (pending FRANS-LISTS-REFRESH)")
        print("PASS (expected-stale)")
        return 0

    print(r.stderr.strip())
    print(f"\nFAIL — committed {pair[0][:8]} / fresh {pair[1][:8]} is not a listed expected-stale pair; "
          "regenerate with `python tools/ai/gen_fransbot_lists.py`")
    return 1


if __name__ == "__main__":
    sys.exit(main())
