#!/usr/bin/env python3
"""audit_bot_insurance.py — the bot passive-income ladder must not skip a difficulty.

    python tools/audit/audit_bot_insurance.py

⛔ WHY THIS EXISTS. On 2026-09-01 the ladder was read end to end for the first time and
`medium` — the DEFAULT difficulty — turned out to receive ZERO insurance income, while `easy`
got three rungs and `hard` got five. The four lowest rungs gated on `normalbot`, a condition
`^AIDifficulties` never grants (it grants `mediumbot`); the mod's only `normalbot` grant sits on
the Dark Reign building `drpplant1.freedomguard`, and conditions are PER-ACTOR, so the actor
hosting the ladder never saw it.

⚠ `audit_orphans` O3 could not catch it and still cannot: it counts conditions MOD-GLOBALLY, so
a condition granted on actor A and consumed on actor B is neither "granted never consumed" nor
"consumed never granted". Its own docstring says the check is approximate. This audit closes that
one gap for the ladder specifically, by EVALUATING each rung's `RequiresCondition` for each
player kind instead of counting names.

WHAT IT CHECKS, on the RESOLVED actors so it holds wherever the ladder is hosted (it moved from
`^AIConyardCash` to `Player:` — see docs/patches/):

  1. MONOTONICITY — rung count must never DECREASE as difficulty rises. A dip is the bug class
     above: a harder bot getting less help than an easier one is always a wiring mistake.
  2. NO DEAD RUNG — every difficulty must reach at least one rung, since the ladder's whole
     stated purpose is stopping a bot getting permanently stuck at zero income.

It deliberately does NOT assert the exact counts: the ladder's shape is a maintainer decision,
its monotonicity is not.

EXIT CODE: 1 on any violation.
"""
from __future__ import annotations

import pathlib
import re
import sys

sys.path.insert(0, str(pathlib.Path(__file__).resolve().parent))
import miniyaml  # noqa: E402

if hasattr(sys.stdout, "reconfigure"):          # Windows consoles default to cp1252
    sys.stdout.reconfigure(encoding="utf-8")

# Easiest first. This IS the ladder's order and the monotonicity check reads it directly.
DIFFICULTIES = ["easiest", "veryeasy", "easy", "medium", "hard", "veryhard",
                "brutal", "challenger", "unbeatable", "cameogod"]

# Actors that may host the ladder. `^Conyard` is where it used to live; `Player` is where it
# belongs. Both are resolved so this audit keeps working across the move.
HOSTS = ["Player", "^Conyard"]

INSURANCE_RE = re.compile(r"\b\w+botinsurance\b")
TOKEN_RE = re.compile(r"[A-Za-z_][A-Za-z0-9_.-]*|\(|\)|&&|\|\||!")


def evaluate(expr: str, conditions: dict[str, bool]) -> bool:
    """Evaluate an OpenRA condition expression against a condition set.

    ⚠ Tokenised, not regex-substituted. A `.replace()` pass over an expression rewrites text
    inside identifiers too, and this mod has near-miss names (`mediumbot` inside
    `mediumbotinsurance`) where that silently changes the meaning — the same shape of bug as
    CLAUDE.md rule 8e. Unknown identifiers evaluate FALSE, which is what the engine does.
    """
    out = []
    for tok in TOKEN_RE.findall(expr):
        if tok == "&&":
            out.append(" and ")
        elif tok == "||":
            out.append(" or ")
        elif tok == "!":
            out.append(" not ")
        elif tok in ("(", ")"):
            out.append(tok)
        else:
            out.append(str(bool(conditions.get(tok, False))))
    return bool(eval("".join(out) or "False"))  # noqa: S307 - operands are booleans only


def ladder_rungs(rules: miniyaml.Ruleset) -> list[str]:
    """Every DISTINCT `RequiresCondition` gating a cash payout on a `*botinsurance` condition.

    Deduplicated: each rung wires two payouts (a `CashTrickler` and a `ResourcePurifier`) behind
    one identical expression, and counting both would report twice the rungs that exist.
    """
    found: list[str] = []
    for host in HOSTS:
        node = rules.resolve(host)
        if node is None:
            continue
        for kind in ("CashTrickler", "ResourcePurifier"):
            for child in node.children_named(kind):
                req = child.get("RequiresCondition")
                if req and INSURANCE_RE.search(req):
                    found.append(req)
    return sorted(set(found))


def conditions_for(kind: str) -> dict[str, bool]:
    """Conditions live on the host actor for one player kind, with the ladder fully triggered.

    Every `*botinsurance` is forced TRUE — this asks "which rungs can this player kind EVER
    reach", not "which are lit right now".
    """
    cond = {f"{d}botinsurance": True for d in DIFFICULTIES}
    if kind == "human":
        return cond
    if kind == "campaign":
        cond["campaignbot"] = True
        return cond
    cond["genericbot"] = True
    cond[f"{kind}bot"] = True
    return cond


def main() -> int:
    rules = miniyaml.Ruleset(".")
    rungs = ladder_rungs(rules)

    print("# audit_bot_insurance — does every difficulty reach the income ladder?\n")
    if not rungs:
        print("⛔ **FAIL** — no insurance rungs resolved on " + " or ".join(f"`{h}`" for h in HOSTS)
              + ". Either the ladder moved again or it was deleted; this audit needs updating.\n")
        return 1

    print(f"Ladder: **{len(rungs)}** payout rungs, resolved from "
          + " + ".join(f"`{h}`" for h in HOSTS) + ".\n")
    print("| player kind | rungs reachable |")
    print("|---|--:|")
    counts = {}
    for kind in ["human", "campaign"] + DIFFICULTIES:
        counts[kind] = sum(1 for r in rungs if evaluate(r, conditions_for(kind)))
        print(f"| {kind} | {counts[kind]} |")
    print()

    problems = []
    for lower, higher in zip(DIFFICULTIES, DIFFICULTIES[1:]):
        if counts[higher] < counts[lower]:
            problems.append(
                f"**{higher}** reaches {counts[higher]} rungs but the EASIER **{lower}** reaches "
                f"{counts[lower]} — rung count must never decrease as difficulty rises.")
    for d in DIFFICULTIES:
        if counts[d] == 0:
            problems.append(
                f"**{d}** reaches NO rung at all. The ladder exists to stop a bot getting stuck "
                f"at zero income; a difficulty that cannot reach it is dead wiring, almost "
                f"certainly a condition name that nothing grants on the host actor.")

    if problems:
        print("## ⛔ FAIL\n")
        for p in problems:
            print(f"- {p}")
        print("\nSee `docs/design/AI_RESEARCH_RECONCILIATION.md` §1 and `docs/patches/`.")
        return 1

    print("**PASS** — the ladder is monotonic and every difficulty reaches it.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
