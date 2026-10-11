#!/usr/bin/env python3
"""A/B campaign receipt analyzer — SPEC_2026-10-11_ab_campaign.md plus amendments 1-2.

Consumes only immutable per-game receipts (schema_version 1, contract in
AB_RECEIPT_SCHEMA_2026-10-11.md / ab_campaign_receipt.schema.json) as produced by
campaign_runner.run_one. Never reads raw match rows or driver-internal state, and never
launches a game or touches a manifest's execution_approved flag.

Reuses, rather than re-deriving, two things the team already owns and tested:
  - campaign_runner.validate_receipt     (producer-side structural/semantic contract)
  - ab_campaign_pilot.acceptance_decision (Amendment 2 sequential ACCEPT/REJECT/PARK gate)
  - ab_campaign_pilot.paired_bootstrap_interval (deterministic paired-effect CI)

Ownership note: this module is additive. It does not modify ab_campaign.py's stale
pre-schema `analyze()`/`classify_game()` (internal match-row format, 3-switch 1v1-only
scope) or ab_campaign_pilot.py's pilot-specific `analyze()`/`adjudicate()` (internal
pre-receipt cell format, single hardcoded BU pilot). Both predate the published receipt
contract. This analyzer is switch-agnostic and setup-agnostic: it reads whatever the
final receipts say, cross-checked against the manifest only for planned-but-missing
game detection.
"""
from __future__ import annotations

import argparse
import collections
import json
import pathlib
import sys
from typing import Any

import ab_campaign_pilot as pilot
import campaign_runner as runner

SCHEMA = 1
NATURAL, CAP, INCOMPLETE = "NATURAL", "CAP", "INCOMPLETE"
# Amendment 2 only names "crash/sentinel" for the immediate-REJECT trigger; ordinary
# censoring (CAP) or a single stall/timeout is not a crash. Keep this list narrow and
# explicit rather than treating every INCOMPLETE as catastrophic.
CRASH_END_REASONS = frozenset({"CRASH", "SENTINEL", "MEMORY_KILL", "PROCESS_DIED", "INVALID_MOD_REGISTRATION"})
# Devin-Reviewer finding (2026-10-11, exact-SHA review of 497d737f6): a percentile
# bootstrap over a single value is degenerate — every resample draws that same value,
# so paired_bootstrap_interval([-1], seed) == [-1.0, -1.0] looks like a tight, confident
# interval when it is actually one noisy observation. Require this many pairs in a
# setup before its CI is trusted to veto a pooled ACCEPT; below the floor the setup is
# reported but cannot veto. 4 is a pragmatic floor (not a formal power calculation),
# lead-confirmed 2026-10-11. Lead ruling: this must live in the frozen manifest/policy,
# not be hard-coded, so a change shows up in the policy hash. ANALYZER_POLICY_DEFAULTS
# is the fallback only when a manifest's acceptance_thresholds omits the key; when a
# manifest is supplied with acceptance_thresholds.min_setup_veto_sample set, that frozen
# value governs (and is covered by the manifest's own SHA-256/policy hash).
ANALYZER_POLICY_DEFAULTS = {"min_setup_veto_sample": 4}


def require(ok: bool, msg: str) -> None:
    if not ok:
        raise ValueError(msg)


# ---------------------------------------------------------------------------
# Loading and structural validation
# ---------------------------------------------------------------------------

def load_receipts(paths: list[pathlib.Path]) -> list[dict]:
    """Load every campaign receipt under the given files/directories.

    A file that fails to parse, or parses but is not schema_version==1 with the
    expected shape, is recorded as a load error row rather than silently skipped —
    unless it simply lacks the campaign-receipt marker fields, in which case it is
    some other artifact (A5 receipt, balance receipt) and is ignored.
    """
    out: list[dict] = []
    for raw in paths:
        p = pathlib.Path(raw)
        files = [p] if p.is_file() else sorted(p.rglob("*.json"))
        for f in files:
            try:
                data = json.loads(f.read_text(encoding="utf-8"))
            except (OSError, UnicodeDecodeError, json.JSONDecodeError) as e:
                out.append({"_path": str(f), "_load_error": f"{type(e).__name__}: {e}"})
                continue
            if not isinstance(data, dict) or "resolved_seats" not in data or "pair_id" not in data:
                continue  # not a campaign receipt; a directory walk will see other artifacts too
            data["_path"] = str(f)
            out.append(data)
    return out


def validate_and_tag(receipt: dict) -> dict:
    """Attach a `_valid`/`_invalid_reason` verdict without mutating receipt semantics."""
    if "_load_error" in receipt:
        receipt["_valid"] = False
        receipt["_invalid_reason"] = receipt["_load_error"]
        return receipt
    try:
        runner.validate_receipt(receipt)
    except ValueError as e:
        receipt["_valid"] = False
        receipt["_invalid_reason"] = str(e)
        return receipt
    receipt["_valid"] = True
    receipt["_invalid_reason"] = None
    return receipt


# ---------------------------------------------------------------------------
# Planned-game cross-check (optional: only when the manifest matches a shape we
# know how to enumerate). Missing receipts must never be dropped silently.
# ---------------------------------------------------------------------------

def planned_pair_members(manifest: dict | None) -> dict[tuple[str, str, int], set[int]] | None:
    """Return {(switch, setup_id, pair_index): {0, 1}} for every planned matched pair,
    or None if the manifest shape is not one this analyzer knows how to enumerate
    (in which case NO_RECEIPT materialization is skipped and reported as such, never
    silently assumed-complete)."""
    if not isinstance(manifest, dict):
        return None
    if manifest.get("schema") == 1 and manifest.get("switch") == pilot.SWITCH and "pairs_per_setup" in manifest:
        jobs = pilot.make_jobs(manifest)
        planned: dict[tuple[str, str, int], set[int]] = collections.defaultdict(set)
        for job in jobs:
            planned[(job["switch"], job["setup"], job["pair"])].add(job["game_in_pair"])
        return dict(planned)
    return None


def materialize_missing(receipts: list[dict], planned: dict[tuple[str, str, int], set[int]] | None) -> list[dict]:
    """Add a synthetic NO_RECEIPT row for every planned (switch, setup, pair, member)
    with no matching receipt. Per the receipt contract: 'A missing receipt for a
    planned game is materialized by the analyzer as INCOMPLETE with end_reason=NO_RECEIPT;
    it must not be dropped or replaced silently.'"""
    if planned is None:
        return receipts
    present = set()
    for r in receipts:
        if r.get("_valid") and isinstance(r.get("seed"), int):
            present.add((r["switch"], r["setup_id"], r.get("_pair_index"), r["pair_member"]))
    out = list(receipts)
    for (switch, setup_id, pair_index), members in planned.items():
        for member in members:
            key = (switch, setup_id, pair_index, member)
            if key not in present:
                out.append({
                    "_valid": True, "_invalid_reason": None, "_synthetic": True,
                    "schema_version": SCHEMA, "switch": switch, "setup_id": setup_id,
                    "pair_id": f"{setup_id}:planned:{pair_index}", "_pair_index": pair_index,
                    "pair_member": member, "end_class": INCOMPLETE, "end_reason": "NO_RECEIPT",
                    "winner_team": None, "teams": {}, "resolved_seats": [],
                })
    return out


# ---------------------------------------------------------------------------
# Pairing and classification
# ---------------------------------------------------------------------------

def _team_arm(receipt: dict, side: str) -> str | None:
    return (receipt.get("teams") or {}).get(side, {}).get("arm")


def side_swap_consistent(a: dict, b: dict) -> bool:
    """The two matched-pair members must assign control/treatment to opposite sides."""
    aa, ab = _team_arm(a, "A"), _team_arm(a, "B")
    ba, bb = _team_arm(b, "A"), _team_arm(b, "B")
    if None in (aa, ab, ba, bb) or {aa, ab} != {"control", "treatment"} or {ba, bb} != {"control", "treatment"}:
        return False
    return aa != ba


def treatment_won(receipt: dict) -> bool | None:
    """True/False only for a NATURAL receipt with an unambiguous winner; else None."""
    if receipt.get("end_class") != NATURAL or receipt.get("winner_team") not in ("A", "B"):
        return None
    return _team_arm(receipt, receipt["winner_team"]) == "treatment"


def seat_metrics_by_arm(receipt: dict) -> dict[str, list[dict]]:
    out: dict[str, list[dict]] = {"control": [], "treatment": []}
    for seat in receipt.get("resolved_seats") or []:
        arm = seat.get("arm")
        if arm in out and isinstance(seat.get("metrics"), dict):
            out[arm].append(seat["metrics"])
    return out


def group_pairs(receipts: list[dict]) -> dict[tuple[str, str, str], dict[int, dict]]:
    """Group by (switch, setup_id, pair_id) -> {pair_member: receipt}."""
    pairs: dict[tuple[str, str, str], dict[int, dict]] = collections.defaultdict(dict)
    for r in receipts:
        if not r.get("_valid"):
            continue
        key = (r.get("switch"), r.get("setup_id"), r.get("pair_id"))
        member = r.get("pair_member")
        if None in key or member not in (0, 1):
            continue
        if member in pairs[key]:
            # Contract violation: duplicate pair member. Keep both as evidence,
            # never silently pick one — downgrade both and flag in the report.
            pairs[key][member] = {"_duplicate_with": pairs[key][member], **r}
        else:
            pairs[key][member] = r
    return pairs


def classify_pair(members: dict[int, dict]) -> dict[str, Any]:
    if set(members) != {0, 1}:
        return {"status": INCOMPLETE, "reason": "pair incomplete (missing member)", "natural": False}
    a, b = members[0], members[1]
    if "_duplicate_with" in a or "_duplicate_with" in b:
        return {"status": "INVALID", "reason": "duplicate pair member", "natural": False}
    if not side_swap_consistent(a, b):
        return {"status": "INVALID", "reason": "matched-pair side-swap proof missing/inconsistent", "natural": False}
    end_classes = {a.get("end_class"), b.get("end_class")}
    if end_classes == {NATURAL}:
        wa, wb = treatment_won(a), treatment_won(b)
        if wa is None or wb is None:
            return {"status": "INVALID", "reason": "natural end lacks unambiguous winner", "natural": False}
        diff = int(wa) + int(wb) - 1  # +1 treatment swept, 0 split, -1 control swept
        return {"status": NATURAL, "reason": "natural end, both members", "natural": True, "win_diff": diff}
    if INCOMPLETE in end_classes:
        return {"status": INCOMPLETE, "reason": "at least one member incomplete", "natural": False}
    if CAP in end_classes:
        return {"status": CAP, "reason": "at least one member capped, none incomplete", "natural": False}
    return {"status": "INVALID", "reason": "unrecognized end_class combination", "natural": False}


# ---------------------------------------------------------------------------
# Metrics
# ---------------------------------------------------------------------------

def _sum_metric(seat_metrics: list[dict], key: str) -> float | None:
    vals = [m.get(key) for m in seat_metrics if isinstance(m.get(key), (int, float))]
    return sum(vals) if vals else None


def income_army_metrics(receipts: list[dict]) -> dict[str, dict[str, float | None]]:
    """Aggregate per-arm earned/spent/banked/army_value/kills/deaths across every
    valid, non-synthetic receipt, regardless of end_class (CAP/INCOMPLETE games still
    carry real seat metrics up to whatever point they reached)."""
    totals = {arm: {"earned": 0.0, "spent": 0.0, "banked": 0.0, "army_value": 0.0, "kills": 0, "deaths": 0,
                     "seats_with_metrics": 0, "seats_missing_metrics": 0} for arm in ("control", "treatment")}
    for r in receipts:
        if not r.get("_valid") or r.get("_synthetic"):
            continue
        by_arm = seat_metrics_by_arm(r)
        for arm, seats in by_arm.items():
            for m in seats:
                have_any = any(isinstance(m.get(k), (int, float)) for k in ("earned", "spent", "banked"))
                if have_any:
                    totals[arm]["seats_with_metrics"] += 1
                else:
                    totals[arm]["seats_missing_metrics"] += 1
                for k in ("earned", "spent", "banked", "army_value"):
                    if isinstance(m.get(k), (int, float)):
                        totals[arm][k] += m[k]
                for k in ("kills", "deaths"):
                    if isinstance(m.get(k), int):
                        totals[arm][k] += m[k]
    out = {}
    for arm, t in totals.items():
        earned_spent_ratio = t["earned"] / t["spent"] if t["spent"] else None
        kill_exchange = t["kills"] / t["deaths"] if t["deaths"] else None
        out[arm] = {**t, "earned_spent_ratio": earned_spent_ratio, "kill_exchange": kill_exchange}
    return out


def team_metrics(receipts: list[dict]) -> dict[str, Any]:
    """Per-team (A/B, re-expressed as per-arm since arm swaps sides each pair member)
    win counts, per-seat metrics, and a team_total = sum-of-seats aggregate.

    Synergy ("team total vs the sum of individual seats") is not estimated: team-size
    strata use different maps than the 1v1 strata (per the pilot manifest's setup
    table), so there is no matched individual-seat counterfactual to compare a team
    total against. ab_campaign_pilot.analyze reached the same conclusion for the
    pilot's internal format; this mirrors that, explicitly, rather than fabricating a
    ratio. Only genuinely identical map+arm+switch 1-seat vs N-seat data could support
    it, which the current campaign design does not produce.
    """
    team_totals: dict[str, dict[str, Any]] = {}
    for r in receipts:
        if not r.get("_valid") or r.get("_synthetic"):
            continue
        setup = r.get("setup_id")
        by_arm_seats: dict[str, list[dict]] = {"control": [], "treatment": []}
        for seat in r.get("resolved_seats") or []:
            arm = seat.get("arm")
            if arm in by_arm_seats:
                by_arm_seats[arm].append(seat)
        bucket = team_totals.setdefault(setup, {"control": {"wins": 0, "games": 0, "seats": []},
                                                  "treatment": {"wins": 0, "games": 0, "seats": []}})
        for arm, seats in by_arm_seats.items():
            if not seats:
                continue
            bucket[arm]["games"] += 1
            bucket[arm]["seats"].append([
                {"seat_id": s.get("seat_id"), "faction": s.get("faction"), "metrics": s.get("metrics")}
                for s in seats
            ])
            if r.get("end_class") == NATURAL and treatment_won(r) is not None:
                won_this_arm = (arm == "treatment") == treatment_won(r)
                if won_this_arm:
                    bucket[arm]["wins"] += 1
    for setup, bucket in team_totals.items():
        for arm in ("control", "treatment"):
            seat_lists = bucket[arm]["seats"]
            team_total = {"earned": 0.0, "spent": 0.0, "army_value": 0.0, "kills": 0, "deaths": 0}
            for game_seats in seat_lists:
                for seat in game_seats:
                    m = seat.get("metrics") or {}
                    for k in ("earned", "spent", "army_value"):
                        if isinstance(m.get(k), (int, float)):
                            team_total[k] += m[k]
                    for k in ("kills", "deaths"):
                        if isinstance(m.get(k), int):
                            team_total[k] += m[k]
            bucket[arm]["team_total"] = team_total
            bucket[arm]["synergy"] = "NOT_IDENTIFIABLE_FROM_RECEIPTS (team-size strata use different maps than 1v1; no matched individual-seat counterfactual)"
    return team_totals


# ---------------------------------------------------------------------------
# Per-setup / overall summary + sequential ACCEPT/REJECT/PARK
# ---------------------------------------------------------------------------

def summarize(receipts: list[dict], manifest: dict | None) -> dict[str, Any]:
    receipts = [validate_and_tag(dict(r)) for r in receipts]
    switches = sorted({r.get("switch") for r in receipts if r.get("_valid")})
    require(len(switches) <= 1, f"analyzer input mixes switches: {switches}; run one switch at a time")
    switch = switches[0] if switches else None

    planned = planned_pair_members(manifest) if manifest is not None else None
    # pair_id -> pair_index is not literally embedded for arbitrary manifests; for the
    # pilot shape we know it (setup:seed:pair), so recover it when possible for the
    # missing-receipt cross-check. Non-pilot manifests skip this cross-check (reported).
    for r in receipts:
        pid = r.get("pair_id")
        if isinstance(pid, str) and pid.count(":") == 2:
            try:
                r["_pair_index"] = int(pid.rsplit(":", 1)[1])
            except ValueError:
                r["_pair_index"] = None
    receipts = materialize_missing(receipts, planned)

    invalid_rows = [r for r in receipts if not r.get("_valid")]
    pairs = group_pairs(receipts)
    pair_results = {key: classify_pair(members) for key, members in pairs.items()}

    by_setup: dict[str, dict[str, Any]] = {}
    pooled_win_diffs: list[int] = []
    pooled_counts = collections.Counter()
    crashed = any(
        (not r.get("_valid")) or (r.get("end_class") == INCOMPLETE and r.get("end_reason") in CRASH_END_REASONS)
        for r in receipts if not r.get("_synthetic")
    ) or any(v["status"] == "INVALID" for v in pair_results.values())

    for (switch_key, setup_id, pair_id), verdict in pair_results.items():
        s = by_setup.setdefault(setup_id, {"planned_or_seen_pairs": 0, "natural": 0, "cap": 0,
                                            "incomplete": 0, "invalid": 0, "win_diffs": []})
        s["planned_or_seen_pairs"] += 1
        pooled_counts[verdict["status"]] += 1
        if verdict["status"] == NATURAL:
            s["natural"] += 1
            s["win_diffs"].append(verdict["win_diff"])
            pooled_win_diffs.append(verdict["win_diff"])
        elif verdict["status"] == CAP:
            s["cap"] += 1
        elif verdict["status"] == INCOMPLETE:
            s["incomplete"] += 1
        else:
            s["invalid"] += 1

    for setup_id, s in by_setup.items():
        n = len(s["win_diffs"])
        s["paired_win_effect"] = sum(s["win_diffs"]) / n if n else None
        s["paired_bootstrap_95ci"] = pilot.paired_bootstrap_interval(s["win_diffs"], f"{switch}:{setup_id}:{n}")
        s["underpowered"] = n < 16  # Amendment 2 stage-1 round count, not the original spec's 20

    pooled_n = len(pooled_win_diffs)
    treatment_wins = sum(1 for d in pooled_win_diffs if d > 0) + sum(0.5 for d in pooled_win_diffs if d == 0)
    control_wins = sum(1 for d in pooled_win_diffs if d < 0) + sum(0.5 for d in pooled_win_diffs if d == 0)
    income = income_army_metrics(receipts)
    teams = team_metrics(receipts)

    thresholds = (manifest or {}).get("acceptance_thresholds", pilot.DEFAULT_THRESHOLDS)
    # Devin-Reviewer finding (2026-10-11, re-review of 8272b271b): a silent fallback to
    # ANALYZER_POLICY_DEFAULTS when a *supplied* manifest omits min_setup_veto_sample
    # means the manifest's SHA-256 does not actually bind the veto threshold used —
    # the same signed manifest could behave differently under a different analyzer
    # version. Require and validate the field whenever a manifest is supplied; only
    # the no-manifest fixture path may use the analyzer's own default.
    if manifest is not None:
        veto_sample_raw = thresholds.get("min_setup_veto_sample")
        require(isinstance(veto_sample_raw, int) and not isinstance(veto_sample_raw, bool) and veto_sample_raw > 0,
                "manifest acceptance_thresholds.min_setup_veto_sample must be a frozen positive integer "
                "when a manifest is supplied; it cannot default silently, or the manifest SHA would not "
                "bind the veto threshold actually used")
    earned_spent_delta = (
        income["treatment"]["earned_spent_ratio"] - income["control"]["earned_spent_ratio"]
        if income["treatment"]["earned_spent_ratio"] is not None and income["control"]["earned_spent_ratio"] is not None
        else None
    )
    army_c, army_t = income["control"]["army_value"], income["treatment"]["army_value"]
    army_efficiency_delta = (army_t - army_c) / army_c if army_c else None
    kx_c, kx_t = income["control"]["kill_exchange"], income["treatment"]["kill_exchange"]
    kill_exchange_delta = (kx_t - kx_c) if (kx_c is not None and kx_t is not None) else None
    economy_not_worse = earned_spent_delta is not None and earned_spent_delta >= -abs(thresholds.get("min_earned_spent_improvement", 0.0))
    economy_better = earned_spent_delta is not None and earned_spent_delta > thresholds.get("min_earned_spent_improvement", 0.0)
    army_not_worse = army_efficiency_delta is not None and army_efficiency_delta >= -abs(thresholds.get("min_army_efficiency_improvement", 0.0))

    decision = pilot.acceptance_decision(
        rounds=pooled_n, treatment_wins=treatment_wins, control_wins=control_wins,
        economy_not_worse=economy_not_worse, economy_better=economy_better, army_not_worse=army_not_worse,
        earned_spent_delta=earned_spent_delta, army_efficiency_delta=army_efficiency_delta,
        kill_exchange_delta=kill_exchange_delta, crashed=crashed, thresholds=thresholds,
    )

    # Lead ruling 2026-10-11: pool per switch across setups (above), stratified by
    # setup (by_setup, above), PLUS a per-setup harm veto — a setup whose paired
    # win-effect 95% CI lies entirely below zero overrides a pooled ACCEPT. The
    # ruling named "REJECT/PARK" without picking one; PARK is used here (a would-be
    # ACCEPT becomes "needs review", not an outright kill) since the pooled signal
    # was otherwise positive. A pooled REJECT/CONTINUE/PARK is never upgraded.
    #
    # A setup below min_setup_veto_sample is reported (harm_veto_ineligible_setups)
    # but excluded from the veto itself: see ANALYZER_POLICY_DEFAULTS docstring above.
    # Sourced from the frozen manifest's acceptance_thresholds when supplied (required
    # and validated above, so it is actually bound by that manifest's policy hash),
    # else the analyzer's own default (no-manifest fixture mode only).
    min_setup_veto_sample = (
        thresholds["min_setup_veto_sample"] if manifest is not None
        else ANALYZER_POLICY_DEFAULTS["min_setup_veto_sample"]
    )
    harm_veto_setups = sorted(
        setup_id for setup_id, s in by_setup.items()
        if s["paired_bootstrap_95ci"] is not None and s["paired_bootstrap_95ci"][1] < 0
        and len(s["win_diffs"]) >= min_setup_veto_sample
    )
    harm_veto_ineligible_setups = sorted(
        setup_id for setup_id, s in by_setup.items()
        if s["paired_bootstrap_95ci"] is not None and s["paired_bootstrap_95ci"][1] < 0
        and len(s["win_diffs"]) < min_setup_veto_sample
    )
    pre_veto_decision = decision
    if decision == "ACCEPT" and harm_veto_setups:
        decision = "PARK"

    return {
        "schema": SCHEMA,
        "switch": switch,
        "manifest_cross_check": "APPLIED" if planned is not None else "SKIPPED_UNKNOWN_MANIFEST_SHAPE",
        "setups": by_setup,
        "pooled": {
            "complete_natural_pairs": pooled_n,
            "paired_win_effect": sum(pooled_win_diffs) / pooled_n if pooled_n else None,
            "paired_bootstrap_95ci": pilot.paired_bootstrap_interval(pooled_win_diffs, f"{switch}:pooled:{pooled_n}"),
            "status_counts": dict(pooled_counts),
            "underpowered": pooled_n < 16,
        },
        "income_army_metrics": income,
        "team_metrics": teams,
        "sequential_decision": {
            "verdict": decision,
            "pre_veto_verdict": pre_veto_decision,
            "harm_veto_setups": harm_veto_setups,
            "harm_veto_ineligible_setups": harm_veto_ineligible_setups,
            "harm_veto_min_sample": min_setup_veto_sample,
            "rounds": pooled_n, "treatment_wins": treatment_wins, "control_wins": control_wins,
            "crashed": crashed, "thresholds": thresholds,
            "earned_spent_delta": earned_spent_delta, "army_efficiency_delta": army_efficiency_delta,
            "kill_exchange_delta": kill_exchange_delta,
        },
        "invalid_receipt_count": len(invalid_rows),
        "invalid_receipts": [{"path": r.get("_path"), "reason": r.get("_invalid_reason")} for r in invalid_rows],
        "telemetry_gaps": [
            "schema_version 1 seat metrics.timeline carries only earned/spent/banked/army_value/kills/deaths at "
            "fixed ticks; there is no refinery/expansion/production event timestamp field, so "
            "'time-to-first/each refinery, expansion, production event' from the original spec is not computable "
            "from receipts alone and is intentionally omitted rather than inferred.",
        ],
        "note": "One game is one observation; seats are clustered within a game. Only pairs where both members "
                "reached NATURAL end with verified side-swap evidence enter the paired win effect. CAP is "
                "censored, never a win/loss. A missing planned receipt is INCOMPLETE/NO_RECEIPT, never dropped.",
    }


# ---------------------------------------------------------------------------
# CLI
# ---------------------------------------------------------------------------

def analyze_cmd(args: argparse.Namespace) -> int:
    receipts = load_receipts(args.receipts)
    manifest = json.loads(args.manifest.read_text(encoding="utf-8")) if args.manifest else None
    result = summarize(receipts, manifest)
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(json.dumps(result, indent=2, default=str) + "\n", encoding="utf-8")
    print(json.dumps({
        "switch": result["switch"], "pooled_pairs": result["pooled"]["complete_natural_pairs"],
        "decision": result["sequential_decision"]["verdict"], "invalid_receipts": result["invalid_receipt_count"],
    }))
    return 0 if result["invalid_receipt_count"] == 0 else 2


def main(argv=None):
    p = argparse.ArgumentParser(description=__doc__)
    sub = p.add_subparsers(dest="command", required=True)
    a = sub.add_parser("analyze", help="read immutable campaign receipts and produce the SPEC_2026-10-11 readout")
    a.add_argument("--receipts", nargs="+", type=pathlib.Path, required=True, help="receipt files or directories")
    a.add_argument("--manifest", type=pathlib.Path, help="frozen campaign manifest, for missing-receipt detection only")
    a.add_argument("--output", type=pathlib.Path, required=True)
    a.set_defaults(fn=analyze_cmd)
    args = p.parse_args(argv)
    try:
        return args.fn(args)
    except (OSError, ValueError, KeyError, TypeError, json.JSONDecodeError) as e:
        print(f"error: {e}", file=sys.stderr)
        return 2


if __name__ == "__main__":
    raise SystemExit(main())
