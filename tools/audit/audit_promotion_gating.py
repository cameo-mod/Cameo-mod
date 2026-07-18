#!/usr/bin/env python3
"""audit_promotion_gating.py — promotion wiring detector.

Checks every *_promotion_* actor that inherits ^promotion_upgrade.template:
  P1 the promotion provides a prerequisite token.
  P2 at least one buildable unit requires that token (i.e. the promotion
     actually gates a unit).
  P3 promotions are gated by the faction construction yard plus rank1 and
     previous promotions; the !self exclusion is allowed. No other
     production buildings or tech structures may gate a promotion.
"""

from __future__ import annotations

import re
import sys

from cameo_model import Model
from report import h1, h2, table

PROMO_RE = re.compile(r"^.+_promotion_.+$|^promotion_.+$|^up[a-z].*$")
PROMO_TEMPLATE = "^promotion_upgrade.template"


def tokens(prereq_value: str) -> list[str]:
    if not prereq_value:
        return []
    return [t.strip() for t in prereq_value.split(",") if t.strip()]


def is_self_exclude(tok: str, promo_name: str) -> bool:
    return tok == f"!{promo_name}"


def main() -> int:
    m = Model()
    rs = m.rs

    # Collect known promotion actor ids and the tokens they provide.
    promotions: dict[str, str] = {}  # actor name -> prerequisite token it grants
    for name, node in rs.actors.items():
        if PROMO_RE.match(name) is None:
            continue
        inherits = set(t for _, t in rs.inherits_of(node))
        if PROMO_TEMPLATE not in inherits:
            continue
        provided = name
        for c in node.children_named("ProvidesPrerequisite"):
            explicit = c.get("Prerequisite")
            if explicit:
                provided = explicit
                break
        promotions[name] = provided.lower()

    promotion_tokens = set(promotions.values())

    missing_gate: list[list[str]] = []
    building_gated: list[list[str]] = []

    for promo_name, token in promotions.items():
        promo_resolved = rs.resolve(promo_name)
        buildable = promo_resolved.child("Buildable") if promo_resolved else None
        prereq_value = buildable.get("Prerequisites") if buildable else None

        # P3: promotion must be gated by the faction construction yard, rank1,
        # previous promotions, and !self. No other buildings/tech allowed.
        CONSTRUCTION_YARD_PATTERNS = (
            "constructionyard", "fact.", "_nexus", "_hatchery", "_commandcenter"
        )
        has_cy = False
        if prereq_value:
            for tok in tokens(prereq_value):
                tlow = tok.lstrip("~").lower()
                if tok.startswith("!"):
                    if not is_self_exclude(tok, promo_name):
                        # !previous_promotion is allowed for mutually-exclusive
                        # promotion trees; any other ! token is suspicious.
                        if tlow not in promotion_tokens:
                            building_gated.append([promo_name, tok, str(buildable.line)])
                    continue
                if tlow in ("rank1", "rank2", "rank3", "rank4", "rank5"):
                    continue
                if tlow in promotion_tokens:
                    continue
                if tok.startswith("~") and any(p in tlow for p in CONSTRUCTION_YARD_PATTERNS):
                    has_cy = True
                    continue
                building_gated.append([promo_name, tok, str(buildable.line)])
        if not has_cy:
            building_gated.append([promo_name, "missing ~constructionyard", str(buildable.line)])

        # P2: find units that require this promotion token.
        consumers: list[str] = []
        for unit_name, unit_node in rs.actors.items():
            resolved = rs.resolve(unit_name)
            if resolved is None:
                continue
            ub = resolved.child("Buildable")
            if ub is None:
                continue
            up = ub.get("Prerequisites")
            if not up:
                continue
            req_tokens = tokens(up)
            if token in {t.lstrip("~").lower() for t in req_tokens if not t.startswith("!")}:
                consumers.append(unit_name)

        if not consumers:
            missing_gate.append([promo_name, token, str(promo_resolved.line if promo_resolved else 0)])

    print(h1("Promotion gating audit"))

    if building_gated:
        print(h2("P3 promotions gated by buildings/tech (should only use rank1 + previous promotions)"))
        print(table(["Promotion", "Forbidden prerequisite", "Line"], building_gated))

    if missing_gate:
        print(h2("P2 promotions that unlock no buildable unit (missing unit prerequisite)"))
        print(table(["Promotion", "Provided token", "Line"], missing_gate))

    if not building_gated and not missing_gate:
        print("No promotion wiring issues found.")
        return 0

    return 1


if __name__ == "__main__":
    sys.exit(main())
