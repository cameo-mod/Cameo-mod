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

# Where may this run write?  docs/audit/latest/ is TRACKED evidence, and several
# audits read engine/ C# or full git history — neither of which exists in a fresh
# clone or a cloud container.  Missing them makes those audits report LESS and still
# say PASS, so a regenerate from an incomplete tree silently deletes real findings.
# tools/audit/environment.py owns the check and the exact list; pass --force-latest
# to override.  See docs/LESSONS_LEARNED.md "audit/latest is environment-bound".
FORCE_LATEST=""
ARGS=""
for arg in "$@"; do
  case "$arg" in
    --force-latest) FORCE_LATEST="--force-latest" ;;
    *) ARGS="$ARGS $arg" ;;
  esac
done
# shellcheck disable=SC2086
set -- $ARGS

OUT="$("$PYTHON" tools/audit/environment.py --print-dir $FORCE_LATEST)"
if [ -z "$OUT" ]; then
  # Fail loudly rather than defaulting: defaulting to latest/ is exactly the write
  # this guard exists to prevent, and defaulting to anywhere else scatters 60 reports.
  echo "run_all.sh: tools/audit/environment.py reported no output directory" >&2
  exit 2
fi
"$PYTHON" tools/audit/environment.py $FORCE_LATEST >&2 || true

mkdir -p "$OUT" docs/factions
failed=0

# Force child processes to emit UTF-8 regardless of OS console codepage
# (Windows git-bash defaults to cp1252, which corrupts §, —, etc.)
export PYTHONIOENCODING=utf-8

# NOTE: "elite_naming" is intentionally excluded — audit_elite_naming.py is
# deprecated, fully superseded by audit_weapon_suffixes.py X1 section
# (same check: rank-elite gated armaments not ending _elite).
# NOTE: "damage_grid" is intentionally excluded — audit_damage_grid.py still
# encodes the RETIRED 2000-step grid and the `main // 2000` percentage twin.
# The live law is formula.DAMAGE_STEP (= 100) + formula.percentage_twin().
# Re-derive it from `formula` before wiring it in; see docs/HANDOFF.md.
for a in inherits duplicate_inherits faction_leaks upgrades upgrade_coverage ai ai_personalities sequences \
         metadata outliers orphans assets fluent power_budget stat_formulas \
         weapon_uniqueness garrison_weapons asset_files promotion_gating min_range \
         basebuilder_crates buildable_order display_text rename_safety \
         missing_elite elite_gating rank_decoration \
         dune_rank_decoration effect_warhead_names weapon_suffixes \
         balance_sheet consistency_report packs balance_drift \
         duplicate_keys \
         template_conformance multiplier_modifiers nuclear_flash_bindings \
         ts_death_palette warhead_split physical_state_warheads \
         unique_traits armor_upgrade_harm plating_exclusivity k_linearity percentage_runtime \
         survivability_pricing doc_claims doc_health hex_shield_routing \
         impact_glow_preservation dead_warhead_fields family_uniqueness \
         three_way_split tier_weapon_class heaviness_bell versus_profile \
         meter_dilution ca_drift upstream_adoption engine_freshness \
         bot_insurance chrome_scale_variants chrome_master_freshness class_templates \
         docs_maxing; do
  echo "== audit_$a"
  "$PYTHON" "tools/audit/audit_$a.py" "$@" > "$OUT/$a.md" 2> "$OUT/$a.err" \
    || failed=1
  [ -s "$OUT/$a.err" ] || rm -f "$OUT/$a.err"
done

# ADVISORY audits — they RUN and write full reports, but they must NOT set the suite's exit
# code. Maintainer ruling 2026-08-24.
#
# Every one is a SCHEDULED scan registered in docs/audit/periodic.json on a 14- or 30-day
# cadence, and this suite is the PER-COMMIT gate — the same argument made a few lines below for
# passing --warn-only to audit_periodic_freshness. All five had been red since 2026-08-16
# (test_coverage alone drifted 223 -> 235 -> 249 -> 257 -> 270 untested modules), so run_all.sh
# exited 1 on every clean tree for a week and the gate's "suite is green" signal was dead: a
# genuinely NEW failure looked identical to the stale ones.
#
# ⚠ The calendar is still enforced, just not here: `python tools/audit/audit_periodic_freshness.py`
#   with NO flag exits 1 when a scan is overdue. That is where lateness belongs.
# ⚠ Each script still exits 1 on its own findings, so CI may gate on one deliberately.
# ⚠ tools/audit/run_all.py parses BOTH loops out of this file — keep the `for a in ...; do`
#   shape so the two runners cannot drift apart.
#
# `support_powers` is advisory for a DIFFERENT reason: its S1 check is red on a
# real bug (9 support powers whose `Prerequisites:` header line is missing, so
# the engine silently drops the level map underneath — CLAUDE.md 8b). The fix is
# a yaml edit and yaml edits need a boot gate, so the finding is reported while
# the suite stays green. MOVE IT INTO THE BLOCKING LOOP once S1 reads clean;
# it guards a class grep cannot find. See docs/design/balance_exceptions.yaml.
# `engine_constraints` is advisory for the same reason: its findings are real
# (maintainer-ruled limits, 2026-08-29) but every fix is a yaml or pipeline
# change needing the boot gate — E2 in particular must be a PAIRED reload/damage
# change through apply_balance, not a sweep. MOVE IT INTO THE BLOCKING LOOP once
# the roster is inside the limits.
#
# `class_redundancy` is advisory because its findings are DESIGN decisions, not
# defects a script can fix: 70 pairs are the same class, buildable at once, and
# aimed at the same targets. Each needs a maintainer call (re-class one, gate one
# behind an upgrade, or differentiate its targeting). It also only sees the 336
# TAGGED units, so the count will RISE as classification proceeds — that is
# expected, not a regression.
#
# `ifv_conditions` reports REAL yaml defects, not design questions: every IFV
# default-weapon guard misses the same three conditions (ifv-archer, ifv-grenade,
# ifv-lightsniper), so those passengers make the vehicle fire its specialist AND
# its default weapon. Advisory only because the fix is yaml and needs the boot
# gate. MOVE IT INTO THE BLOCKING LOOP once F1/F3/F4 read clean.
#
# `check_band` enforces BALANCE_PIPELINE.md 8.1's baseband + tier-gate law: every unit's
# class-formula price ratio must sit in the 75%-400% caps, >200% ungated must earn a gate, and
# the 100-200% ungated band should hold >=80% of units. 8.1 says "wire into run_all.sh" and it
# never was. Advisory because it is red on real CONTENT, not on a defect in itself: 129
# violations across 20 classes (mbt 15/42 in the sweet spot, missile_vehicle 1/13), and every
# fix is a priced yaml change that must go through apply_balance and the boot gate. It also
# cannot be a per-commit gate while 0 anchors are signed — it is measuring prices nobody has
# approved yet. MOVE IT INTO THE BLOCKING LOOP once anchors are signed and a first production
# pass has brought the roster inside the band.
#
# `infantry_class_bands` measures FORMULA_V2 §6b's contiguous range bands against the tree:
# the band DEFINES class membership, so a scout whose weapon reaches 6000 is in the wrong class.
# Advisory because every finding is one of two maintainer calls — re-class the unit, or move its
# range, which is a priced change that must go through apply_balance and the boot gate. It also
# judges ONLY the four classes §6b gives a band; the nine TBD classes are measured and reported
# without a verdict, so the count will move as those bands get ruled. MOVE IT INTO THE BLOCKING
# LOOP once the four banded classes read clean.
#
# `counter_matrix` compares docs/balance/counter_matrix.yaml (design intent) with
# what the tree does. Advisory permanently: every finding is a design question —
# reassign a family, retag a class, or change the intent — and never a build break.
for a in code_duplication test_coverage recent_changes error_handling security \
         support_powers engine_constraints class_redundancy ifv_conditions \
         infantry_class_bands counter_matrix; do
  echo "== audit_$a (advisory)"
  "$PYTHON" "tools/audit/audit_$a.py" "$@" > "$OUT/$a.md" 2> "$OUT/$a.err" || true
  [ -s "$OUT/$a.err" ] || rm -f "$OUT/$a.err"
done

# `check_band` is ADVISORY and lives in tools/balance/, so it gets its own line rather than a
# slot in either loop: the cross-tree loop below sets `failed=1` on a non-zero exit, and
# check_band exits non-zero on real CONTENT (129 violations across 20 classes). Putting it there
# would turn the whole suite red for prices nobody has approved yet — 0 anchors are signed.
echo "== check_band (advisory)"
"$PYTHON" "tools/balance/check_band.py" "$@" > "$OUT/band.md" 2> "$OUT/band.err" || true
[ -s "$OUT/band.err" ] || rm -f "$OUT/band.err"

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

# audit_unconverted_templates writes its OWN report with --write; its stdout is only a
# short summary, so redirecting stdout into the report file would clobber the real one.
echo "== unconverted_templates"
"$PYTHON" tools/audit/audit_unconverted_templates.py --write $FORCE_LATEST > /dev/null 2> "$OUT/unconverted_templates.err" \
  || failed=1
[ -s "$OUT/unconverted_templates.err" ] || rm -f "$OUT/unconverted_templates.err"

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
MATRIX="docs/factions/MATRIX.md"
[ "$OUT" = "docs/audit/latest" ] || MATRIX="$OUT/MATRIX.md"
"$PYTHON" tools/audit/gen_faction_matrix.py > "$MATRIX" || failed=1

echo "reports in $OUT/ ; matrix in $MATRIX"
exit $failed
