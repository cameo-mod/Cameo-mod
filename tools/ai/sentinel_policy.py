#!/usr/bin/env python3
"""Sentinel-diagnostic policy layer (SENTINEL-RUNNER-POLICY rev6.1).

Pure functions — no I/O, no engine state. Two axes:

1. Economy-gate disposition (maintainer F1 ruling, subset-of-pair
   CONFIRMED): a sentinel capture is `baseline_exempted` only for a
   planned-roster-proven classic seat carrying raw BLOCK whose non-empty
   findings are all severity "DEFECT" with code in {NEAR_EMPTY_FUNDS,
   FUNDS_BELOW_BAND}; every non-classic seat must be OBSERVED_HEALTHY and
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
"""
from __future__ import annotations

HEALTHY = "OBSERVED_HEALTHY"
EXEMPTIBLE_CODES = {"NEAR_EMPTY_FUNDS", "FUNDS_BELOW_BAND"}
CLASSIC_BOT = "classic"

DISPOSITION_BASELINE_EXEMPTED = "baseline_exempted"
DISPOSITION_CLEAN = "clean_diagnostic"
DISPOSITION_BLOCKING = "blocking"


def seat_planned_bot(player: str, planned_roster) -> str | None:
    """Bot type for a seat name from the PLANNED roster (name, faction, bot, home)."""
    for name, _faction, bot, _home in planned_roster:
        if name == player:
            return bot
    return None


def _eligible_classic_block(report: dict, planned_bot: str | None) -> bool:
    """One seat is exemptible iff planned-roster classic, raw status exactly
    BLOCK, non-empty findings, every finding severity DEFECT with code in the
    allowed pair (one code or both; anything else blocks)."""
    if planned_bot != CLASSIC_BOT or report.get("status") != "BLOCK":
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


def economy_disposition(raw_status: str, seat_reports: list[dict],
                        structural_failures: list, planned_roster) -> dict:
    """Sentinel policy over the collected raw evidence.

    Returns {"disposition", "exempt_findings", "seats"} — the raw_status and
    per-seat reports are recorded verbatim alongside; this function never
    rewrites them.
    """
    seats = []
    eligible: list[bool] = []
    for report in seat_reports:
        player = report.get("player")
        planned_bot = seat_planned_bot(player, planned_roster)
        raw_findings = report.get("findings")
        findings = raw_findings if isinstance(raw_findings, list) else []
        seats.append({
            "player": player,
            "bot_type": planned_bot,
            "status": report.get("status"),
            "finding_codes": [f.get("code") for f in findings if isinstance(f, dict)],
        })
        eligible.append(_eligible_classic_block(report, planned_bot))

    exempt_findings = sorted({
        code for seat in seats for code in seat["finding_codes"]
        if code in EXEMPTIBLE_CODES
    })

    if structural_failures:
        disposition = DISPOSITION_BLOCKING
    elif raw_status == HEALTHY:
        disposition = DISPOSITION_CLEAN
    elif raw_status == "BLOCK" and any(eligible) and all(
            seat["status"] == HEALTHY or ok
            for seat, ok in zip(seats, eligible)):
        # Every non-healthy seat must independently satisfy the strict
        # predicate — extracted finding_codes alone would exempt a classic
        # BLOCK seat carrying a WARN severity or a non-dict finding row.
        disposition = DISPOSITION_BASELINE_EXEMPTED
    else:
        disposition = DISPOSITION_BLOCKING

    return {
        "disposition": disposition,
        "exempt_findings": exempt_findings if disposition == DISPOSITION_BASELINE_EXEMPTED else [],
        "seats": seats,
    }


def missing_situations_coverage(report: dict, planned_bot: str | None) -> bool:
    """Verdict-layer ruling: True iff this replay_health report is exactly the
    granted coverage diagnostic — classic seat, UNKNOWN, sole finding
    MISSING_SITUATIONS. Anything else returns False (still blocking)."""
    if planned_bot != CLASSIC_BOT:
        return False
    if report.get("status") != "UNKNOWN":
        return False
    findings = report.get("findings")
    if not isinstance(findings, list) or len(findings) != 1:
        return False
    finding = findings[0]
    return isinstance(finding, dict) and finding.get("code") == "MISSING_SITUATIONS"
