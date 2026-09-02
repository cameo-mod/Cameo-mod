#!/usr/bin/env bash
# apply_all.sh — apply the whole bot-insurance change set, verify it, and stop before the boot gate.
#
# ⛔ THIS SCRIPT DELIBERATELY DOES NOT COMMIT. The boot gate (CLAUDE.md rule 1) is the one thing a
# script must never pretend to have done. It applies, builds, and runs every check that does not
# need a running game, then tells you exactly what to do next.
#
#   bash docs/patches/apply_all.sh            # apply + verify
#   bash docs/patches/apply_all.sh --check    # dry run, changes nothing
#
# Why patches at all: this branch was authored in a cloud container with no engine/ and no dotnet,
# where `tools/hooks/bash_guard.py` correctly refuses to commit anything under mods/ or
# OpenRA.Mods.Cameo/ without boot proof. See README.md in this directory.
set -euo pipefail

cd "$(git rev-parse --show-toplevel)"
CHECK=""
[ "${1:-}" = "--check" ] && CHECK="--check"

PATCHES=(
  docs/patches/bot_insurance_01_fix_medium_difficulty.patch
  docs/patches/bot_insurance_03a_dynamic_trait_csharp.patch
  docs/patches/bot_insurance_03b_dynamic_trait_yaml.patch
  docs/patches/bot_limits_04_brutal_explicit_cadence.patch
)

# ⚠ The patches are a SERIES, not independent: 03b rewrites the block 01 edits, so
# `git apply --check` on each one against the pristine tree fails on the second. A dry run has to
# apply them cumulatively and then undo. Files are backed up first and restored by a trap, so an
# interrupted check cannot leave the tree half-patched.
TOUCHED=(mods/cameo/ai/ai.yaml mods/cameo/rules/defaults.yaml mods/cameo/rules/player.yaml)
if [ -n "$CHECK" ]; then
  BACKUP="$(mktemp -d)"
  restore() {
    for f in "${TOUCHED[@]}"; do
      [ -f "$BACKUP/$(basename "$f")" ] && cp "$BACKUP/$(basename "$f")" "$f"
    done
    rm -f OpenRA.Mods.Cameo/Traits/DynamicBotInsurance.cs
    git checkout -- mods/cameo/uibits/flags_3x.png 2>/dev/null || true
    rm -rf "$BACKUP"
  }
  trap restore EXIT
  for f in "${TOUCHED[@]}"; do cp "$f" "$BACKUP/$(basename "$f")"; done
fi

echo "== applying ${#PATCHES[@]} patches ${CHECK:+(dry run — will be undone)}"
for p in "${PATCHES[@]}"; do
  printf '   %-56s' "$(basename "$p")"
  git apply "$p"
  echo "ok"
done

if [ -n "$CHECK" ]; then
  # The flags sheet comes from git history, not from a patch, so the dry run has to do it too or
  # the chrome audit below fails on a file the real run would have fixed. The trap restores it.
  bash docs/patches/chrome_06_restore_flags_3x.sh > /dev/null

  echo
  echo "== resolved-ruleset check (the part a dry run CAN prove)"
  python - <<'PYEOF'
import sys
sys.path.insert(0, "tools/audit")
import miniyaml
rules = miniyaml.Ruleset(".")
player = rules.resolve("Player")
conyard = rules.resolve("^Conyard")
assert player.child("DynamicBotInsurance") is not None, "the trait did not land on Player:"
rungs = sum(len(conyard.children_named(k))
            for k in ("BotInsurance", "CashTrickler", "ResourcePurifier"))
assert rungs == 0, f"^Conyard still carries {rungs} ladder nodes"
print("   Player: has DynamicBotInsurance, ^Conyard has 0 ladder nodes — ok")
PYEOF
  python tools/audit/audit_bot_insurance.py | tail -2
  python tools/audit/audit_chrome_scale_variants.py | tail -1
  echo
  echo "Dry run complete — the tree has been restored to exactly how it was."
  exit 0
fi

echo
echo "== restoring the correct 1536px flags sheet from history (no new art needed)"
bash docs/patches/chrome_06_restore_flags_3x.sh

echo
echo "== building OpenRA.Mods.Cameo (rule 7: stale DLLs crash the boot)"
DOTNET_ROLL_FORWARD=LatestMajor dotnet build -c Release --nologo -p:TargetPlatform=win-x64

echo
echo "== audits and tests that do not need a running game"
python tools/audit/audit_bot_insurance.py
python tools/audit/audit_chrome_scale_variants.py
python tools/audit/audit_doc_claims.py
python -m pytest tools/tests/test_bot_insurance_model.py \
                 tools/tests/test_bot_difficulty_curve.py \
                 tools/tests/test_audit_bot_insurance.py -q

cat <<'NEXT'

============================================================================
 APPLIED AND VERIFIED — everything that can be checked without the game is green.
 ⛔ NOT DONE YET. Three things remain, and only you can do them:

 1. BOOT GATE. Snapshot %APPDATA%/OpenRA/Logs first, then launch-game.cmd to the
    main menu. perf.log must end with MenuPostProcessEffect.PostWorldLoaded and
    there must be no new exception-*.log.

 2. THE ONE UNVERIFIABLE CLAIM. Start a skirmish with a bot and confirm the ore
    purifier half actually pays: DynamicBotInsurance implements
    INotifyResourceAccepted on the PLAYER actor, and whether the engine delivers
    that notification there could not be checked without the Common assembly.
    If it does not fire, the purifier needs a refinery-side forwarder.
    While you are there: grep the debug log for "BotInsurance" — it prints
    measured vs expected net worth every 1500 ticks, which is how the par
    curve's three invented magnitudes get replaced with measured ones.

 3. CHECK THE UI AT HIGH DPI. The chrome fix is in this set: set UI scale to 150%+
    and confirm faction icons and editor glyphs render correctly. ⚠ Fix BOTH or it
    looks like it failed — that is very likely why the 2026-06 attempt was reverted.

 4. COMMIT, and in the SAME commit `git rm` the patches and this script —
    docs/patches/ must never hold a change that already landed. Set
    bot_insurance_unreachable_difficulties to 0 in docs/audit/doc_claims.yaml.
    Scoped `git add <files>` only, never -A.
============================================================================
NEXT
