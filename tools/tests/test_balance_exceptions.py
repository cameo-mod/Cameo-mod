"""THE EXCEPTION REGISTRY IS LIVE, NOT DECORATIVE.

⛔ THE ANTIPATTERN THIS FILE EXISTS TO PREVENT. Until 2026-08-31 the
`categories:` section of `docs/design/balance_exceptions.yaml` was read by
**nothing** — only `limits:` had a consumer (`audit_engine_constraints.py`).
Writing `in_formula: false` into it changed no measurement and no price. That is
the dead-knob antipattern `formula.py` already documents about
`VEHICLE_TYPE_CLASSES = {"mbt"}`: a knob that looks like it enforces a law and
answers "is this handled?" with a lie.

So these tests assert the two things a quarantine must actually do:

  1. the registry parses and the reader finds the entries;
  2. **a consumer honours it** — the quarantined actors really are absent from
     `band_granularity.py`'s populations.

Test 2 is the one that matters. Test 1 alone would pass on a decorative file.
"""

from __future__ import annotations

import pathlib
import sys

import pytest

ROOT = pathlib.Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "tools" / "balance"))

import exceptions as exc  # noqa: E402

ATHENA = "futuretech_athenacannon"
IFV_FAMILY = {
    "ra2_allies_ifv", "ra2_allies_ifv_chrono", "ra2_allies_ifv_hmg",
    "ra2_allies_ifv_mg", "ra2_allies_ifv_missile", "ra2_allies_ifv_repair",
    "futuretech_salamanderifv",
}


def test_the_registry_parses_and_the_two_quarantines_are_found():
    q = exc.quarantined_actors()
    assert ATHENA in q
    assert IFV_FAMILY <= q, f"missing IFV members: {IFV_FAMILY - q}"


def test_a_members_list_expands_to_every_member_not_just_the_key():
    """⚠ `ra2_ifv_family` is a FAMILY key, not an actor. If the reader ever
    returns the key instead of expanding `members:`, seven real actors silently
    stop being quarantined while the registry still looks correct."""
    q = exc.quarantined_actors()
    assert "ra2_ifv_family" not in q, "family key leaked in place of its members"
    assert len(q) == 1 + len(IFV_FAMILY)


def test_is_priced_is_the_inverse_and_an_ordinary_actor_is_untouched():
    assert not exc.is_priced(ATHENA)
    assert not exc.is_priced("ra2_allies_ifv_missile")
    assert exc.is_priced("tiger.nax")
    assert exc.is_priced("ra1_allies_rifleinfantry")


def test_every_quarantine_carries_a_reason():
    """A quarantine with no recorded reason is indistinguishable from a typo."""
    for actor in exc.quarantined_actors():
        assert (exc.quarantine_reason(actor) or "").strip(), actor


def test_an_entry_with_in_formula_true_is_NOT_quarantined():
    """The registry can also record that an actor IS priced. Only
    `in_formula: false` holds one out — reading the section as "everything
    listed here is excluded" would quarantine whatever gets documented next."""
    reg = exc._registry().get("actors") or {}
    for key, entry in reg.items():
        if isinstance(entry, dict) and entry.get("in_formula") is True:
            members = entry.get("members") or [key]
            for m in members:
                assert exc.is_priced(str(m)), f"{m} excluded despite in_formula: true"


# --------------------------------------------------------------------------------------
# ⭐ THE TEST THAT PROVES THE KNOB IS NOT DEAD.
# --------------------------------------------------------------------------------------

def test_a_CONSUMER_actually_honours_the_registry():
    """`band_granularity.py` must not carry a quarantined actor in any class
    population. This is the assertion a decorative registry fails."""
    sys.path.insert(0, str(ROOT / "tools" / "balance"))
    import json

    import band_granularity as bg

    anchors = {k: v for k, v in json.loads(
        (ROOT / "docs/balance/class_anchors.json").read_text(encoding="utf-8")).items()
        if isinstance(v, dict)}
    rows, _live = bg.collect_classes(anchors)

    leaked = [a for members in rows.values() for a, _i in members
              if not exc.is_priced(a)]
    assert leaked == [], f"quarantined actors still in class populations: {leaked}"


def test_the_quarantined_actors_would_otherwise_BE_in_those_populations():
    """⛔ Guards against a false pass. If the actors were absent for some other
    reason — renamed, untagged, unpriced — the test above would pass while the
    registry did nothing. So assert they are real, tagged, priceable actors that
    the quarantine is what removes."""
    import check_band as cb

    seen = {}
    for _fn, actor, u, du in cb.collect({}):
        if actor in exc.quarantined_actors():
            cls = (u.get("design") or {}).get("class_anchor")
            inp = cb.unit_inputs(u, du)
            seen[actor] = (cls, inp is not None and all(inp[:4]))

    assert seen, "no quarantined actor found in the tree at all — stale registry?"
    priceable = [a for a, (cls, ok) in seen.items() if cls and ok]
    assert priceable, (
        "every quarantined actor is unpriceable anyway, so the registry is not "
        f"what removes them: {seen}")


# --------------------------------------------------------------------------------------
# ⭐ THE CROSS-CHECK THAT MAKES A HAND PARSER ACCEPTABLE.
# --------------------------------------------------------------------------------------

def test_the_fallback_parser_agrees_with_PyYAML():
    """⛔ `LESSONS_LEARNED` rule 8e: a hand-written yaml parser once opened a block
    and never closed it, and every downstream number was internally consistent and
    wrong. The rule is not "never hand-parse" in the abstract — it is "never hand-parse
    with nothing checking you". This is the check.

    `exceptions.py` uses PyYAML when it is importable and a strict minimal parser
    otherwise, because pytest here runs on an interpreter WITHOUT PyYAML — which is how
    the silent-empty-registry bug was found in the first place. Wherever both are
    available they must return the identical quarantine set."""
    yaml = pytest.importorskip(
        "yaml", reason="this interpreter has no PyYAML — the fallback is all there is, "
                       "and the other tests already cover it")

    text = exc.REGISTRY.read_text(encoding="utf-8")
    real = (yaml.safe_load(text) or {}).get("actors") or {}
    mine = exc._parse_actors_minimal(text)["actors"]

    assert set(mine) == set(real), "entry keys differ"
    for key in real:
        assert mine[key].get("members") == real[key].get("members"), f"{key} members"
        assert mine[key].get("in_formula") == real[key].get("in_formula"), f"{key} in_formula"


def test_a_missing_registry_RAISES_rather_than_quarantining_nothing():
    """⛔ THE BUG THIS PINS, EXACTLY. The first version returned `{}` when it could not
    read the registry, so a machine without PyYAML quarantined nothing and said nothing.
    Silence is the failure. An unreadable registry must be loud."""
    import pathlib as _pl

    original = exc.REGISTRY
    exc._registry.cache_clear()
    try:
        exc.REGISTRY = _pl.Path("/nonexistent/balance_exceptions.yaml")
        with pytest.raises(RuntimeError, match="cannot read"):
            exc._registry()
    finally:
        exc.REGISTRY = original
        exc._registry.cache_clear()
