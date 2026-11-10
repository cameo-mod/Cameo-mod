#!/usr/bin/env python3
"""Sentinel-diagnostic policy layer (SENTINEL-RUNNER-POLICY rev6.1).

Pure functions — no I/O, no engine state. Two axes:

1. Economy-gate disposition (maintainer F1 ruling, subset-of-pair
   CONFIRMED): a sentinel capture is `baseline_exempted` only for a
   planned-roster-proven classic seat carrying raw BLOCK with a completed,
   error-free report whose non-empty findings are all severity "DEFECT"
   with code in {NEAR_EMPTY_FUNDS, FUNDS_BELOW_BAND}; every non-classic
   seat must be OBSERVED_HEALTHY with a completed, error-free report and
   zero structural anomalies are tolerated. All-healthy is a distinct
   `clean_diagnostic` disposition — still scoreable:false.

2. MISSING_SITUATIONS verdict-layer exception (maintainer ruling, granted
   verbatim): classic-seat replay_health UNKNOWN whose sole finding is
   MISSING_SITUATIONS is a pre-registered coverage diagnostic — raw
   UNKNOWN/exit-21 preserved verbatim, non-blocking at the verdict layer.
   It certifies none of the skipped placement/match semantics; independent
   roster/identity/terminal/integrity/economy checks remain mandatory.
   Any co-finding, any hard-seat occurrence, any other status blocks.
   Checker and raw records are unchanged — this predicate is the ONLY place
   the exception lives.

3. Economy findings recorded (Opus ruling E1HFRUL02 7C127BC2, D-E): economy_invariants marks every
   DEFECT finding as status BLOCK, and they fire for the Cameo bot and the stock classic bot alike
   during ordinary play, so as a per-game admission gate they are a calibration defect. A seat whose
   report is BLOCK solely because of findings with severity DEFECT, with no error, is RECORDED,
   not blocking: disposition ECONOMY_FINDINGS_RECORDED (every other seat OBSERVED_HEALTHY and
   error-free or likewise recorded; no structural failure; exact roster coverage). Opus ruling on
   HEALTHFIX_02 item 1: EVERY DEFECT finding is recorded except DROPPED_TELEMETRY (dropped rows).
   Its findings stay in the per-seat raw reports and in recorded_findings. Still blocking: any error
   (even with status BLOCK), a DROPPED_TELEMETRY finding, a finding whose severity is not DEFECT
   (unknown severity fails closed) or whose code is missing, a non-dict finding, empty findings, a
   structural / roster / identity failure, and any seat status outside {OBSERVED_HEALTHY, BLOCK}.
   economy_invariants itself and the raw reports are never changed.
"""
from __future__ import annotations

HEALTHY = "OBSERVED_HEALTHY"
EXEMPTIBLE_CODES = {"NEAR_EMPTY_FUNDS", "FUNDS_BELOW_BAND"}
CLASSIC_BOT = "classic"

DISPOSITION_BASELINE_EXEMPTED = "baseline_exempted"
DISPOSITION_CLEAN = "clean_diagnostic"
DISPOSITION_BLOCKING = "blocking"
DISPOSITION_FINDINGS_RECORDED = "ECONOMY_FINDINGS_RECORDED"
# D-E (E1HFRUL02, widened by the Opus ruling on HEALTHFIX_02 item 1): every economy_invariants DEFECT finding is recorded instead of
# blocking EXCEPT the codes below. DROPPED_TELEMETRY (dropped rows) still blocks. A non-DEFECT or unknown severity also blocks.
NON_RECORDABLE_DEFECT_CODES = {
    "DROPPED_TELEMETRY",
}


def seat_planned_bot(player: str, planned_roster) -> str | None:
    """Bot type for a seat name from the PLANNED roster (name, faction, bot, home)."""
    if not isinstance(planned_roster, (tuple, list)):
        return None
    for row in planned_roster:
        if isinstance(row, (tuple, list)) and len(row) == 4:
            name, _faction, bot, _home = row
            if name == player:
                return bot if isinstance(bot, str) else None
    return None


def _eligible_classic_block(report: dict, planned_bot: str | None) -> bool:
    """One seat is exemptible iff planned-roster classic, raw status exactly
    BLOCK, the report is completed and error-free (R2: an errored report's
    findings cannot certify completeness), non-empty findings, every
    finding severity DEFECT with code in the allowed pair (one code or
    both; anything else blocks)."""
    if (not isinstance(report, dict) or planned_bot != CLASSIC_BOT
            or report.get("status") != "BLOCK"):
        return False
    # Missing key or None = no error; ANY non-null value (even empty or
    # malformed) disqualifies — strict per exact-SHA review.
    if report.get("error") is not None:
        return False
    findings = report.get("findings")
    if not isinstance(findings, list) or not findings:
        return False
    for finding in findings:
        if not isinstance(finding, dict):
            return False
        if finding.get("severity") != "DEFECT":
            return False
        if finding.get("code") not in EXEMPTIBLE_CODES:
            return False
    return True


def _recordable_defect_block(report: dict) -> bool:
    """D-E: one seat is recordable iff raw status exactly BLOCK, the report is completed and
    error-free (ANY non-null error disqualifies, even a BLOCK produced by dropped rows), the
    findings are a non-empty list and every finding is a dict with severity exactly DEFECT and a
    non-empty string code that is not in NON_RECORDABLE_DEFECT_CODES (DROPPED_TELEMETRY). Any seat
    planned bot qualifies (Cameo and classic alike)."""
    if not isinstance(report, dict) or report.get("status") != "BLOCK":
        return False
    if report.get("error") is not None:
        return False
    findings = report.get("findings")
    if not isinstance(findings, list) or not findings:
        return False
    return all(isinstance(finding, dict) and finding.get("severity") == "DEFECT"
               and isinstance(finding.get("code"), str) and finding["code"]
               and finding["code"] not in NON_RECORDABLE_DEFECT_CODES for finding in findings)


def economy_disposition(raw_status: str, seat_reports: list[dict],
                        structural_failures: list, planned_roster) -> dict:
    """Sentinel policy over the collected raw evidence.

    Returns {"disposition", "exempt_findings", "seats"} — the raw_status and
    per-seat reports are recorded verbatim alongside; this function never
    rewrites them.
    """
    inputs_well_formed = (
        isinstance(seat_reports, list)
        and isinstance(structural_failures, list)
        and isinstance(planned_roster, (tuple, list)))
    seat_reports = seat_reports if isinstance(seat_reports, list) else []
    structural_failures = structural_failures if isinstance(structural_failures, list) else ["malformed"]
    planned_roster = planned_roster if isinstance(planned_roster, (tuple, list)) else []
    seats = []
    eligible: list[bool] = []
    recordable: list[bool] = []
    planned_names = [row[0] for row in planned_roster
                     if isinstance(row, (tuple, list)) and len(row) == 4
                     and isinstance(row[0], str) and row[0]]
    roster_valid = (inputs_well_formed
                    and len(planned_names) == len(planned_roster)
                    and len(set(planned_names)) == len(planned_names))
    observed_names: list[str] = []
    for report in seat_reports:
        if not isinstance(report, dict):
            roster_valid = False
            continue
        player = report.get("player")
        if not isinstance(player, str) or not player:
            roster_valid = False
        else:
            observed_names.append(player)
        planned_bot = seat_planned_bot(player, planned_roster)
        raw_findings = report.get("findings")
        findings = raw_findings if isinstance(raw_findings, list) else []
        seats.append({
            "player": player,
            "bot_type": planned_bot,
            "status": report.get("status"),
            "error": report.get("error"),
            "finding_codes": [f.get("code") for f in findings if isinstance(f, dict)],
        })
        eligible.append(_eligible_classic_block(report, planned_bot))
        recordable.append(_recordable_defect_block(report))

    exempt_findings = sorted({
        code for seat in seats for code in seat["finding_codes"]
        if code in EXEMPTIBLE_CODES
    })

    exact_coverage = (roster_valid and len(observed_names) == len(planned_names)
                      and len(set(observed_names)) == len(observed_names)
                      and set(observed_names) == set(planned_names))

    if structural_failures or not exact_coverage:
        disposition = DISPOSITION_BLOCKING
    elif (raw_status == HEALTHY and seat_reports
          and all(seat["status"] == HEALTHY and seat["error"] is None for seat in seats)):
        disposition = DISPOSITION_CLEAN
    elif raw_status == "BLOCK" and any(eligible) and all(
            (seat["status"] == HEALTHY and seat["error"] is None) or ok
            for seat, ok in zip(seats, eligible)):
        # Every non-healthy seat must independently satisfy the strict
        # predicate — extracted finding_codes alone would exempt a classic
        # BLOCK seat carrying a WARN severity or a non-dict finding row —
        # and a healthy seat counts only on a completed, error-free report.
        disposition = DISPOSITION_BASELINE_EXEMPTED
    elif raw_status == "BLOCK" and any(recordable) and all(
            (seat["status"] == HEALTHY and seat["error"] is None) or ok
            for seat, ok in zip(seats, recordable)):
        # D-E: every non-healthy seat independently satisfies the strict recordable predicate
        # (a baseline-exemptible classic seat is a subset of it); a healthy seat counts only on a
        # completed, error-free report.
        disposition = DISPOSITION_FINDINGS_RECORDED
    else:
        disposition = DISPOSITION_BLOCKING

    return {
        "disposition": disposition,
        "exempt_findings": exempt_findings if disposition == DISPOSITION_BASELINE_EXEMPTED else [],
        "recorded_findings": ({seat["player"]: list(seat["finding_codes"]) for seat, ok in zip(seats, recordable) if ok}
                              if disposition == DISPOSITION_FINDINGS_RECORDED else {}),
        "seats": seats,
    }


def missing_situations_coverage(report: dict, planned_bot: str | None) -> bool:
    """Verdict-layer ruling: True iff this replay_health report is exactly the
    granted coverage diagnostic — classic seat, UNKNOWN, sole finding
    MISSING_SITUATIONS. Anything else returns False (still blocking)."""
    if not isinstance(report, dict) or planned_bot != CLASSIC_BOT:
        return False
    if report.get("status") != "UNKNOWN":
        return False
    if report.get("error") is not None:
        return False
    findings = report.get("findings")
    if not isinstance(findings, list) or len(findings) != 1:
        return False
    finding = findings[0]
    return isinstance(finding, dict) and finding.get("code") == "MISSING_SITUATIONS"


def coverage_disposition(raw_status: str, seat_reports: list[dict],
                         structural_failures: list, planned_roster) -> dict:
    """Independent replay-coverage axis; raw checker records remain untouched."""
    valid = (isinstance(seat_reports, list) and isinstance(structural_failures, list)
             and isinstance(planned_roster, (tuple, list)))
    reports = seat_reports if isinstance(seat_reports, list) else []
    failures = structural_failures if isinstance(structural_failures, list) else ["malformed"]
    planned = planned_roster if isinstance(planned_roster, (tuple, list)) else []
    names = [row[0] for row in planned if isinstance(row, (tuple, list)) and len(row) == 4
             and isinstance(row[0], str) and row[0]]
    roster_ok = (valid and len(names) == len(planned) and len(set(names)) == len(names))
    raw_reports = [r.get("raw_report") if isinstance(r, dict)
                   and isinstance(r.get("raw_report"), dict) else r for r in reports]
    observed = [r.get("player") for r in raw_reports if isinstance(r, dict)]
    coverage_exact = (roster_ok and len(observed) == len(names)
                      and all(isinstance(n, str) and n for n in observed)
                      and len(set(observed)) == len(observed) and set(observed) == set(names))
    diagnostic_seats = []
    every_healthy = coverage_exact
    only_coverage_exception = coverage_exact
    hard_block = False
    structural = list(failures)
    if not coverage_exact:
        structural.append("coverage identity/report coverage mismatch")
    for report in raw_reports:
        if not isinstance(report, dict):
            every_healthy = False
            only_coverage_exception = False
            structural.append("coverage report is malformed")
            continue
        status = report.get("status")
        if status == "BLOCK":
            # Preserve any hard in-window block alongside structural failure.
            hard_block = True
        if report.get("error") is not None:
            every_healthy = False
            only_coverage_exception = False
            structural.append(f"coverage report error for {report.get('player')!r}")
            continue
        if status == HEALTHY:
            continue
        every_healthy = False
        bot = seat_planned_bot(report.get("player"), planned)
        if missing_situations_coverage(report, bot):
            diagnostic_seats.append(report["player"])
        elif status == "BLOCK":
            only_coverage_exception = False
        else:
            only_coverage_exception = False
            structural.append(f"coverage report status is structurally incomplete: {status!r}")
    if (raw_status == "UNKNOWN"
            and not (diagnostic_seats and only_coverage_exception)):
        structural.append("coverage collector returned structural UNKNOWN")
    elif raw_status not in {HEALTHY, "BLOCK", "UNKNOWN"}:
        structural.append(f"coverage collector status is malformed: {raw_status!r}")
    if failures or not coverage_exact:
        disposition = "blocking"
    elif raw_status == HEALTHY and every_healthy:
        disposition = "coverage_healthy"
    elif diagnostic_seats and only_coverage_exception:
        # The exception is only verdict-layer coverage; separate match/roster/
        # terminal/integrity evidence must already have passed in failures.
        disposition = "coverage_diagnostic"
    else:
        disposition = "blocking"
    return {"disposition": disposition,
            "diagnostic_seats": sorted(diagnostic_seats),
            "hard_block": hard_block,
            "raw_status": raw_status,
            "structural_failures": structural}


def canonical_decision(economy: dict, coverage: dict, *, final_row: bool = False) -> dict:
    """Reviewer-facing row disposition; never grants the separate dispatch gate."""
    economy_ok = (isinstance(economy, dict)
                  and economy.get("disposition") in {DISPOSITION_CLEAN, DISPOSITION_BASELINE_EXEMPTED,
                                                     DISPOSITION_FINDINGS_RECORDED})
    coverage_ok = (isinstance(coverage, dict)
                   and coverage.get("disposition") in {"coverage_healthy", "coverage_diagnostic"})
    if economy_ok and coverage_ok:
        decision, next_admission = "ACCEPTED_DIAGNOSTIC", (
            "END_OF_SENTINEL_REVIEW" if final_row else "REQUEST_ELIGIBLE")
    else:
        hard_block = (isinstance(coverage, dict) and coverage.get("hard_block") is True)
        hard_block = hard_block or (isinstance(economy, dict)
                                    and economy.get("raw_status") == "BLOCK"
                                    and economy.get("disposition") not in {DISPOSITION_BASELINE_EXEMPTED,
                                                                           DISPOSITION_FINDINGS_RECORDED})
        structural = (isinstance(coverage, dict)
                      and bool(coverage.get("structural_failures")))
        decision, next_admission = ("HALT" if hard_block or structural else "HOLD"), "NO_GO"
    return {"decision": decision, "next_admission": next_admission,
            "dispatch_gate": {"status": "NO_GO", "jit_granted": False,
                              "requires_fresh_preflight": True}}
