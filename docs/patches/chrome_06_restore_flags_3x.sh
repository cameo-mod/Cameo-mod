#!/usr/bin/env bash
# Restore the CORRECT 1536x1536 flags sheet — it already exists in this repository's history.
#
# Blackrobe built it on 2026-06-09 in 1326cc44e ("Try to rescale faction flags in lobby for
# big-scaled screens") and reverted it the same day in ce2170c9b, with no reason recorded. The
# file was good: 1536 is exactly 3x the 512px base, and its colour density per pixel (0.78
# colours/px relative to 1x) matches the 2x sheet that ships and works (0.85). Measured, not
# assumed — see docs/audit/CHROME_SCALE_BUG.md.
#
# ⭐ So the faction-icon half of the bug needs NO new art. It needs this one line.
#
# ⚠ The file was renamed hyphen -> underscore in 938e988d2 (DESIGN.md §1, no-hyphen naming), so
# the old path and the new path differ.
set -euo pipefail
cd "$(git rev-parse --show-toplevel)"

git show 1326cc44e:mods/cameo/uibits/flags-3x.png > mods/cameo/uibits/flags_3x.png
python - <<'PY'
import struct, pathlib
d = pathlib.Path("mods/cameo/uibits/flags_3x.png").read_bytes()
w, h = struct.unpack(">II", d[16:24])
assert (w, h) == (1536, 1536), f"expected 1536x1536, got {w}x{h}"
print(f"  flags_3x.png restored: {w}x{h} — exactly 3x the 512px base")
PY
echo "  now run: python tools/audit/audit_chrome_scale_variants.py"
