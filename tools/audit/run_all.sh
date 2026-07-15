#!/bin/sh
# run_all.sh — execute the full Cameo audit suite (MASTER_REPORT Appendix A).
# Writes one markdown report per audit to docs/audit/latest/, regenerates
# docs/factions/MATRIX.md, and exits non-zero if any blocking audit failed.
#
# Usage: tools/audit/run_all.sh            (from the repo root)
#        PYTHON=python3 tools/audit/run_all.sh
set -u
cd "$(dirname "$0")/../.."

PYTHON="${PYTHON:-python}"
if ! "$PYTHON" -c 'import sys' 2>/dev/null; then
  for cand in python3 py \
      "$LOCALAPPDATA/Programs/Python/Python312/python.exe"; do
    if "$cand" -c 'import sys' 2>/dev/null; then PYTHON="$cand"; break; fi
  done
fi

OUT="docs/audit/latest"
mkdir -p "$OUT" docs/factions
failed=0

for a in inherits faction_leaks upgrades upgrade_coverage ai sequences \
         metadata outliers orphans assets fluent power_budget stat_formulas \
         weapon_uniqueness garrison_weapons asset_files promotion_gating min_range \
         basebuilder_crates buildable_order; do
  echo "== audit_$a"
  "$PYTHON" "tools/audit/audit_$a.py" "$@" > "$OUT/$a.md" 2> "$OUT/$a.err" \
    || failed=1
  [ -s "$OUT/$a.err" ] || rm -f "$OUT/$a.err"
done

echo "== gen_damage_matrix"
"$PYTHON" tools/audit/gen_damage_matrix.py > "$OUT/damage_matrix.md" || failed=1
echo "== gen_rename_maps"
"$PYTHON" tools/audit/gen_rename_maps.py > "$OUT/naming.md" || failed=1
echo "== gen_faction_matrix"
"$PYTHON" tools/audit/gen_faction_matrix.py > docs/factions/MATRIX.md || failed=1

echo "reports in $OUT/ ; matrix in docs/factions/MATRIX.md"
exit $failed
