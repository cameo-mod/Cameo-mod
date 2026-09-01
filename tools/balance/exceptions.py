#!/usr/bin/env python3
"""exceptions.py — the ONE reader for `docs/design/balance_exceptions.yaml`.

    from exceptions import quarantined_actors, is_priced
    if not is_priced(actor): continue

⛔ WHY THIS EXISTS. Before 2026-08-31 the registry's `categories:` section was
read by **nothing** — only `limits:` had a consumer (`audit_engine_constraints.py`).
Writing "in_formula: false" into it therefore changed no measurement and no
price: a decorative entry that answers "is this handled?" with a lie. That is
exactly the dead-knob antipattern `formula.py` documents about
`VEHICLE_TYPE_CLASSES = {"mbt"}`, a class-level knob that **nothing read** while
the per-row step always won.

So a quarantine only counts once a tool honours it, and this module is how a
tool honours it. One reader, so the registry cannot mean different things in
two places.

⚠ SCOPE, STATED HONESTLY. This module is consumed by the MEASUREMENT tools
(`band_granularity.py`). It is **not** yet consumed by `apply_balance.py`, the
writer — wiring the writer changes what lands in yaml and needs a maintainer
order (CLAUDE.md rule 3). Until then, a quarantined actor is excluded from
class statistics but its Cost in the tree is untouched, which is the correct
conservative half: it stops one actor corrupting a class's measured shape
without silently repricing anything.

⚠ AND A QUARANTINE IS A HOLDING ACTION, NOT A VERDICT. `futuretech_athenacannon`
is quarantined because 193,600 DPS is a suspected STAT ERROR; the fix is to
triage the data and delete the entry. The RA2 IFV family is quarantined because
it needs a chassis+payload model that does not exist yet. Neither is "balanced
by being on a list".
"""
from __future__ import annotations

import functools
import pathlib

ROOT = pathlib.Path(__file__).resolve().parents[2]
REGISTRY = ROOT / "docs/design/balance_exceptions.yaml"


# ⛔ WHY THERE IS A FALLBACK PARSER, AND WHY IT IS NOT OPTIONAL.
#
# The first cut of this module did `import yaml` and returned `{}` on ImportError.
# It worked from the CLI and SILENTLY QUARANTINED NOTHING under `pytest` — because
# pytest here runs on a uv-managed interpreter that has no PyYAML. Six tests failed
# and told the truth; had they not existed, the registry would have looked live
# while doing nothing on every machine without PyYAML, including possibly the
# Windows one. That is the dead-knob bug again, one level deeper: not "no consumer"
# but "a consumer that reads an empty file and says nothing".
#
# So: PyYAML when present, a STRICT minimal parser otherwise, and
# `test_balance_exceptions.py` asserts the two agree whenever PyYAML is available.
# That cross-check is what makes a hand parser acceptable here — `LESSONS_LEARNED`
# rule 8e's disaster was a hand parser with NOTHING checking it, which opened a
# block and never closed it.
#
# ⚠ The fallback deliberately understands only the `actors:` section's shape:
# two indent levels, scalar `key: value`, and one `members:` list. Anything else
# it ignores. It is not a YAML implementation and must never be used on game yaml
# (that is `miniyaml.Ruleset`'s job, and MiniYaml is a different language).


def _parse_actors_minimal(text: str) -> dict:
    """Just the `actors:` section, without PyYAML. Strict and small on purpose."""
    out: dict[str, dict] = {}
    lines = text.splitlines()
    i = 0
    while i < len(lines) and lines[i].rstrip() != "actors:":
        i += 1
    i += 1
    entry_name = None
    in_members = False
    while i < len(lines):
        raw = lines[i]
        i += 1
        if not raw.strip() or raw.lstrip().startswith("#"):
            continue
        indent = len(raw) - len(raw.lstrip())
        if indent == 0:                       # a new top-level key ends the section
            break
        stripped = raw.strip()
        if indent == 2 and stripped.endswith(":"):        # actor / family key
            entry_name = stripped[:-1].strip()
            out[entry_name] = {}
            in_members = False
            continue
        if entry_name is None:
            continue
        if indent >= 6 and in_members and stripped.startswith("- "):
            out[entry_name].setdefault("members", []).append(stripped[2:].strip())
            continue
        if indent == 4:
            # ⚠ CLOSE THE LIST THE MOMENT INDENTATION RETURNS TO ITS LEVEL. This is
            # the exact line whose absence caused the rule-8e incident.
            in_members = False
            if stripped == "members:":
                in_members = True
                out[entry_name]["members"] = []
            elif ":" in stripped:
                k, _, v = stripped.partition(":")
                v = v.strip()
                if v in ("true", "false"):
                    out[entry_name][k.strip()] = (v == "true")
                elif v and not v.startswith(">"):
                    out[entry_name][k.strip()] = v
    return {"actors": out}


@functools.lru_cache(maxsize=1)
def _registry() -> dict:
    try:
        text = REGISTRY.read_text(encoding="utf-8")
    except OSError as e:                       # ⛔ never silently empty
        raise RuntimeError(f"cannot read {REGISTRY}: {e}") from e
    try:
        import yaml
    except ImportError:
        return _parse_actors_minimal(text)
    try:
        return yaml.safe_load(text) or {}
    except ValueError as e:
        raise RuntimeError(f"{REGISTRY} is not valid YAML: {e}") from e


@functools.lru_cache(maxsize=1)
def quarantined_actors() -> frozenset[str]:
    """Actor names the registry's `actors:` section holds OUT of the formula.

    An entry may name a single actor (its key) or a family (`members:` list).
    Only entries with `in_formula: false` are collected — an entry can exist to
    record a ruling that an actor IS priced.
    """
    out: set[str] = set()
    for key, entry in (_registry().get("actors") or {}).items():
        if not isinstance(entry, dict) or entry.get("in_formula", True):
            continue
        members = entry.get("members")
        if isinstance(members, list) and members:
            out.update(str(m) for m in members)
        else:
            out.add(str(key))          # the key IS the actor when no members
    return frozenset(out)


def is_priced(actor: str) -> bool:
    """False when the registry holds this actor out of the class formula."""
    return actor not in quarantined_actors()


def quarantine_reason(actor: str) -> str | None:
    """The `note` for whichever entry quarantines `actor`, for reporting."""
    for key, entry in (_registry().get("actors") or {}).items():
        if not isinstance(entry, dict) or entry.get("in_formula", True):
            continue
        members = entry.get("members") or [key]
        if actor in {str(m) for m in members}:
            return (entry.get("note") or "").strip() or key
    return None


if __name__ == "__main__":
    q = sorted(quarantined_actors())
    print(f"# quarantined actors: {len(q)}\n")
    for a in q:
        print(f"  {a}")
