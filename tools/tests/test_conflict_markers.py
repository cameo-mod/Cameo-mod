import re
import subprocess
import unittest
from pathlib import Path

ROOT = Path(__file__).parents[2]

MARKER = re.compile(r"^(<<<<<<<|=======|>>>>>>>)( |$)")

# An unresolved merge conflict in a committed file almost always leaves exactly
# these line-anchored markers. The increment_switches.yaml incident
# (2026-10-06): a lone `<<<<<<< HEAD` made the switch parser silently drop every
# group after BL — the yaml was still parseable, so nothing else caught it.
SKIP_PREFIXES = (
    "engine/",          # fetched build input, not this repo's source
    "tools/tests/test_conflict_markers.py",  # this file legitimately names the markers
)


class TestConflictMarkers(unittest.TestCase):
    def test_no_merge_markers_in_tracked_files(self):
        files = subprocess.run(
            ["git", "ls-files", "-z"], cwd=ROOT, check=True, capture_output=True
        ).stdout.split(b"\0")

        hits = []
        for raw in files:
            rel = raw.decode("utf-8")
            if not rel or rel.startswith(SKIP_PREFIXES):
                continue
            try:
                data = (ROOT / rel).read_bytes()
            except OSError:
                continue
            if b"\0" in data[:8192]:  # git's binary sniff: NUL in the head
                continue
            text = data.decode("utf-8", errors="replace")
            for i, line in enumerate(text.splitlines(), 1):
                if MARKER.search(line):
                    hits.append(f"{rel}:{i}: {line[:60]}")

        self.assertEqual(hits, [], "unresolved merge markers committed:\n" + "\n".join(hits[:50]))


if __name__ == "__main__":
    unittest.main()
