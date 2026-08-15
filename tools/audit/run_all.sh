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

# Force child processes to emit UTF-8 regardless of OS console codepage
# (Windows git-bash defaults to cp1252, which corrupts §, —, etc.)
export PYTHONIOENCODING=utf-8

# NOTE: "elite_naming" is intentionally excluded — audit_elite_naming.py is
# deprecated, fully superseded by audit_weapon_suffixes.py X1 section
# (same check: rank-elite gated armaments not ending _elite).
for a in inherits faction_leaks upgrades upgrade_coverage ai sequences \
         metadata outliers orphans assets fluent power_budget stat_formulas \
         weapon_uniqueness garrison_weapons asset_files promotion_gating min_range \
         basebuilder_crates buildable_order display_text rename_safety \
         missing_elite elite_gating rank_decoration \
         dune_rank_decoration effect_warhead_names weapon_suffixes \
         balance_sheet consistency_report packs balance_drift \
         duplicate_keys code_duplication test_coverage recent_changes \
         error_handling security \
         template_conformance multiplier_modifiers nuclear_flash_bindings \
         ts_death_palette warhead_split physical_state_warheads \
         unique_traits; do
  echo "== audit_$a"
  "$PYTHON" "tools/audit/audit_$a.py" "$@" > "$OUT/$a.md" 2> "$OUT/$a.err" \
    || failed=1
  [ -s "$OUT/$a.err" ] || rm -f "$OUT/$a.err"
done

# Audits that live in tools/ rather than tools/audit/
for a in createeffect_image:tools/audit_createeffect_image.py \
         ce_image_usage:tools/audit_ce_image_usage.py \
         empty_warhead:tools/audit/find_empty_warhead.py \
         gen_sync:tools/balance/verify_generator_sync.py; do
  name="${a%%:*}"
  script="${a##*:}"
  echo "== $name"
  "$PYTHON" "$script" "$@" > "$OUT/$name.md" 2> "$OUT/$name.err" \
    || failed=1
  [ -s "$OUT/$name.err" ] || rm -f "$OUT/$name.err"
done

# Staleness gate for the mandatory recurring audits (docs/audit/periodic.json):
# runs last so its report reflects this run's evidence files.
# --warn-only: this suite is the PER-COMMIT gate, so a late scheduled scan must not
# turn it red for a reason unrelated to the commit being made. BROKEN entries (a
# registered script or evidence file is missing) still fail, because that is a real
# defect in the tree. Enforce the calendar in the scheduled run instead:
#   python tools/audit/audit_periodic_freshness.py     (no flag -> exit 1 when late)
echo "== periodic_freshness"
"$PYTHON" tools/audit/audit_periodic_freshness.py --warn-only \
  > "$OUT/periodic_freshness.md" 2> "$OUT/periodic_freshness.err" || failed=1
[ -s "$OUT/periodic_freshness.err" ] || rm -f "$OUT/periodic_freshness.err"

echo "== gen_damage_matrix"
"$PYTHON" tools/audit/gen_damage_matrix.py > "$OUT/damage_matrix.md" || failed=1
echo "== gen_rename_maps"
"$PYTHON" tools/audit/gen_rename_maps.py > "$OUT/naming.md" || failed=1
echo "== gen_faction_matrix"
"$PYTHON" tools/audit/gen_faction_matrix.py > docs/factions/MATRIX.md || failed=1

echo "reports in $OUT/ ; matrix in docs/factions/MATRIX.md"
exit $failed
