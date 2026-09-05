#!/usr/bin/env python3
"""SessionStart hook — inject the Cameo must-read + hard-rules checklist into
context every session, so 'read the docs first' and the hard rules don't depend
on the model choosing to read CLAUDE.md. Short and punchy on purpose."""
import json

CHECKLIST = """\
CAMEO — orient before acting this session (verify against the artifacts, don't trust summaries):

⛔⛔ PRIORITY 0 — FINISH THE BALANCE PIPELINE BEFORE ANY SINGLE-UNIT WORK.
   Maintainer order, 2026-09-02: *"We need to finish the balancing pipeline. Finish all the class
   anchors. Apply all the correct unit templates for each actor. Working on a single unit is not
   getting us any closer... we need to work on the TOP LEVEL first, like a system design."*
   The two open top-level items, in order:
     1. CLASS ANCHORS -- 8 of 27 signed, and only 336 of 1870 buildable units carry a class tag
        (18%). Every anchor is fitted against 18% of its own population, and 17 of 27 anchors are
        not even members of the class they anchor. `python tools/balance/anchor_readiness.py`
     2. UNIT TEMPLATES -- every buildable actor needs EXACTLY ONE `Inherits@Template:`.
        `python tools/audit/audit_class_templates.py`
   ⚠ THE DRIFT TEST, apply it to your own next action: *"does this move a NUMBER for one unit,
   or does it move the SYSTEM?"* Investigating one weapon, one warhead, one actor is the trap --
   it feels productive and it does not advance the pipeline. If a single-unit fix is genuinely
   needed, WRITE IT DOWN in docs/design/ROADMAP.md and keep going on the top level.
   ⭐ This block exists because it happened: 2026-09-02 went into one weapon (HydraSpit) and its
   warhead family while both items above sat untouched.

MUST-READ, in order: CLAUDE.md · docs/LESSONS_LEARNED.md · docs/AGENT_WORKSPACE.md ·
docs/HANDOFF.md · **docs/DESIGN.md** · docs/design/ROADMAP.md · docs/audit/SUMMARY.md.
docs/README.md defines that order and wins over any copy of it.

docs/HANDOFF.md is THE entry point: verified state + the priority-ordered queue. It supersedes
every dated handoff — those are in docs/history/handoffs/ and must NOT be resumed from.
For weapon work also: docs/design/WEAPON_3WAY_SPLIT.md · docs/design/WEAPON_TYPE_SYSTEM.md ·
docs/design/BALANCE_PROGRAM_PLAN.md (the board + §0a's binding order of operations).

⛔ BEFORE DESIGNING ANYTHING, GREP docs/DESIGN.md FOR THE CONCEPT. It is the BINDING contract
and it is long, so nobody reads it end to end — grep it. On 2026-08-22 a whole session was spent
re-deriving a weapon-tier model that §12.0h/§12.0c/§12.0d had already ruled AND shipped. A design
question that feels novel usually is not. The rulings most often re-invented:

  §12.0h MEAN-100      every ^Warhead_* MAIN warhead has its 16 armor rows normalised to
                       arithmetic MEAN 100. Therefore K is SHAPE-ONLY, `Damage` is the SOLE
                       magnitude knob, and a tilt is FREE. Weapon tier does NOT price via Versus.
  §12.0c SHIELD LADDER Shield is its own compressed [100,400] ladder, Tesla top. NOT a normal armor.
  §12.0d CLASS TILT    each LEVEL tilts toward one end of every armor ladder (Light->lightest rung,
                       Medium->middle, Heavy->heaviest, Super->FLAT generalist). The tilt is applied
                       to the VALUES and each armor is then given back the RANK it held, so it
                       "can never invert" — WITHIN a ladder. LADDERS are INF/VEH/BLD/AIR, so
                       `None` (INF) vs `Superheavy` (VEH) is a CROSS-ladder relation the tilt is
                       DESIGNED to change. Comparing them proves nothing.
  §12.0b HEROIC        a DERIVED cell: Heroic = Plate x Scout / PEAK. Never tilt it; recompute it.

⛔ WORKING ON CLASS ANCHORS, A CLASS FORMULA, OR A BASELINE? READ
`docs/balance/anchor_decisions_log.md` FIRST. It is the SOURCE OF TRUTH for anchors —
docs/README.md says class_anchors.json is "maintained via" it — and it holds LOCKED per-class
baselines with real numbers, verifier conventions, and the 3-input DEFENSE formula (HP, Range,
DPS — no speed term) plus the rearmable-aircraft SORTIE-cycle ruling. On 2026-08-30 a whole
session ran on class anchors without opening it: scout_vehicle's infantry HP granularity was
reported as a new ruling when it had been LOCKED since 2026-07-26, complete with a companion
"HARD RULE" (switch ^ScoutVehicleTemplate to the infantry self-heal timing) that was missed.

⛔ NEVER SET `signed_off: true` YOURSELF. fit_class.py step 4 reserves it for the maintainer,
because signing unblocks `apply_balance --confirm` for that class. On 2026-08-29 an agent signed
three anchors on its own validation tables; they were reverted on 2026-08-30. And note the trap
in the aftermath: the next session "corrected" a document to match the artifact — but the
artifact was the agent's own edit. A document agreeing with an artifact you wrote is an ECHO,
not corroboration.

⛔ GREP `tools/` BEFORE WRITING A TOOL, NOT JUST `docs/`. On 2026-08-30 a new audit was written
for a law `audit_stat_formulas.py` (F8/F9/F10/F17/F19) had enforced for months — already in
run_all.sh, already at 0 findings, already auto-fixed by gen_derived_stats.py. The duplicate
mis-scoped its cohort and published 340 findings against a CLEAN roster into DESIGN.md. Grep the
MECHANISM, not the phrase: "TurnSpeed (aircraft)" found one sentence of a two-part law;
`grep -ril fighter tools/` would have found the whole thing implemented. And a fresh measurement
that contradicts a PASSING audit is WRONG until proven otherwise — read that audit's SCOPE first.
[hook-enforced: prior_art_guard denies a new tools/*.py that duplicates an existing tool]

⛔ NEVER HAND-PARSE YAML. Read through `miniyaml.Ruleset.resolve_weapon` / `.resolve`, and pull
Versus with `weapon_efficiency.versus_of(node)`. A bespoke line-scanner opened a dict on `Versus:`
and never CLOSED it, so the `PercentageVersus:` rows sitting in the SAME warhead node overwrote the
profile: every mean, spread, ratio and inversion count came out internally consistent and WRONG
("0 of 125 obey MEAN-100"; the truth was 123 of 125). The near-miss sibling name is the trap — the
OPEN guard was right, the CLOSE was missing. Guarded by tools/audit/audit_versus_profile.py.
[hook-enforced: bash_guard blocks inline Versus scanning]

⚠ A RESULT THAT CONTRADICTS A BINDING LAW IS A CONTRADICTION, NOT A FINDING. If the generator
implements a law and verify_generator_sync reports 0 drift, "nothing conforms" means YOUR MEASURE
is broken. Check the measurement before writing it up.

⛔ ONE REPOSITORY: `cameo-mod/Cameo-mod`. `Zeruel87/Cameo-mod` is the ABANDONED original
fork -- it still answers `git fetch`, which is what makes it dangerous. Never add it as a
remote, fetch it, compare against it or push to it; on 2026-08-11 a session went into
reconciling two stray commits against that dead tree. Pre-repo history lives in docs/history/.
[hook-enforced: bash_guard 1b]  BUT `Zeruel87 Urban` (a TILESET CATEGORY id in
mods/cameo/tilesets/*.yaml) and the credits.txt entry are ART CREDIT -- never sweep the NAME,
only the URL. The engine soft-fork `cameo-mod/OpenRA` is a different, LIVE repository.

HARD RULES (several are enforced by hooks — see .claude/settings.json):
 1. Never commit without booting to the main menu (perf.log ends
    MenuPostProcessEffect.PostWorldLoaded; no new %APPDATA%/OpenRA/Logs/exception-*.log). [hook-enforced]
 2. Scoped `git add <files>` only — never -A/./--all (several contributors have live WIP). [hook-enforced]
 3. Don't trust, verify — grep the data / ls the file (incl. ~/Downloads) / run the tool /
    boot-gate before asserting done/pending/blocked/missing. When a summary and the artifact
    disagree, the artifact wins — then fix the stale summary.
 4. Never hand-edit balance numbers — use the pipeline (extract_stats -> ledger ->
    apply_balance --confirm; --confirm needs maintainer order).
 5. Versus lives ONLY in ^Warhead_* templates; never change a warhead / Burst / BurstDelays
    without explicit permission.
 6. Weapon 3-way split: preserve resolved behaviour (Damage verbatim, projectile fields),
    find_empty_warhead.py = 0, boot-gate per batch. Verify with tools/audit/review_resolve_diff.py.
 7. Multi-agent tree: one owner per file-set (boundaries in BALANCE_PROGRAM_PLAN.md §2);
    re-verify others' commits before building on them (check mtimes + git log -3 <file> first).
 8. Audit reports regen via `bash tools/audit/run_all.sh` ONLY (PowerShell > writes UTF-16),
    and ONLY from a COMPLETE tree (engine/ built, clone not shallow) - otherwise a dozen
    audits scan a smaller corpus, report FEWER findings and still say PASS. run_all diverts
    to the untracked docs/audit/degraded/ in that case; --force-latest overrides.
 9. Underscore-only naming — no hyphens in ids/files/fluent keys.
10. Commit trailer = the ACTUAL author, with your REAL model name:
    Co-Authored-By: Claude <your-model> <noreply@anthropic.com>  (a template, not a
    literal - do not copy a version from a previous commit). Other agents sign as
    themselves, e.g. Co-Authored-By: Devin AI <devin@cognition.ai>.

ENGINE CHANGES — `engine/` IS NOT PART OF THIS REPO. It is .gitignored, has no .git and no
.gitmodules, and `git ls-files engine` returns ZERO files; `git` run from inside it silently
operates on the PARENT repo. Editing engine/**  produces work that CANNOT be committed here
and is DELETED by the next `make all`. It is a build output, not source.
Full procedure: docs/LESSONS_LEARNED.md "The canonical engine update pipeline". In short:
 1. Edit C# in the SEPARATE clone of https://github.com/cameo-mod/OpenRA, branch cameo-engine.
 2. Commit + push to origin/cameo-engine (check `git status` for stray nested-clone entries).
 3. `git rev-parse cameo-engine` for the FULL 40-char hash — never hand-type or truncate it.
 4. Set ENGINE_VERSION="<hash>" in **mod.config** (NOT mod.yaml) in this repo.
 5. `make.cmd all` — VERSION mismatch makes the SDK delete engine/, refetch and rebuild.
 6. Verify engine/VERSION == the hash, build has 0 errors, and recreate any engine/glsl/
    shaders (the fetch WIPES them, e.g. postprocess_nuclearflash.frag).
 7. Boot-gate, then commit mod.config with the docs updates.
Before choosing that route, check whether the mod assembly can just SHADOW the engine trait:
ObjectCreator.FindType takes the FIRST assembly in mod.yaml's Assemblies list holding the
name, and the order is AS, CA, Cameo, Cnc, D2k, Common — so an OpenRA.Mods.Cameo type of the
same name wins with ZERO yaml changes (precedent: ColorPickerColorShift, PlayerColorShift,
SelectionDecorations). PROVE a shadow works by giving the Cameo Info a field the engine one
lacks and booting with that field set — `--docs` lists BOTH types and proves nothing.

CURRENT FRONT (2026-08-23): W24 (one damage warhead per weapon) -> W23 (retrofit the 47 legacy
templates) -> A5 -> class anchors. Pricing is deliberately NOT running yet; apply_balance --confirm
is a NO-OP until W11 sign-off writes targets into the ledger (signed-off anchors today: 0).
Work queue: docs/design/ROADMAP.md · effort estimate: docs/design/BALANCE_PIPELINE_ESTIMATE.md.

TWO THINGS YOU CANNOT RESOLVE FROM THE REPO:
 * commit hashes older than 2026-08-10 fail in a shallow checkout — `git fetch --unshallow`,
   or verify the claim against the artifact instead (better).
 * `memory <name>` citations point at a private per-agent store. Provenance only, never
   authority; promote anything binding into DESIGN.md.
"""

# ⭐ THE DOCS MAXING AUDIT (maintainer order, 2026-08-30). The manifest is appended
# to every SessionStart so the whole authored documentation set is at least ENUMERATED
# before anything happens, and the TIER 1 gate in `read_first_guard.py` then refuses
# every non-read action until the seven reading-order documents are actually opened.
# Generated here rather than pasted: a hand-maintained file list goes stale the first
# time someone adds a document, and a stale manifest is how "I didn't know it existed"
# comes back.
def _docs_maxing():
    import pathlib as _pl
    import sys as _sys
    root = _pl.Path(__file__).resolve().parents[1]
    _sys.path.insert(0, str(root / "audit"))
    try:
        import audit_docs_maxing as dm
    except Exception:
        return ""
    lines = ["", "=" * 78,
             "DOCS MAXING AUDIT — enumerate everything, then open the gate documents.",
             "=" * 78,
             "⛔ TIER 1 — NO TOOL ACTION IS PERMITTED until every one of these has been",
             "   OPENED this session (hook-enforced: tools/hooks/read_first_guard.py).",
             "   Reads and `git status`/`log`/`diff` are exempt, so the gate is satisfiable."]
    for d in dm.TIER1:
        lines.append(f"     sed -n '1,400p' {d}")
    lines.append("⛔ TIER 2 — the document that OWNS your subject blocks an EDIT in it:")
    for d in dm.TIER2:
        lines.append(f"     {d}")
    rest = [d for d in dm.authored_docs() if d not in dm.TIER1 and d not in dm.TIER2]
    lines.append(f"TIER 3 — {len(rest)} further authored documents. Know THAT they exist and")
    lines.append("   what each owns; open the one that covers your area before working in it.")
    lines.append("   The authored set is ~92,000 lines / ~1.9M tokens — it does NOT fit a")
    lines.append("   context window, which is why the gate is Tier 1 and not all of it.")
    for d in rest:
        lines.append(f"     {d}")
    lines.append("   Full report + this session's coverage:")
    lines.append("     python tools/audit/audit_docs_maxing.py --transcript <transcript>")
    lines.append("=" * 78)
    return "\n".join(lines)


print(json.dumps({"hookSpecificOutput": {
    "hookEventName": "SessionStart",
    "additionalContext": CHECKLIST + _docs_maxing()}}))
