# BU_harvester_logistics A/B pilot — switch outcome log

Campaign: `ab-bu-pilot-2026-10-11` (manifest `docs/audit/ab_pilot_manifest_candidate.json`).
Source of truth: `devin/ab-launch-unblock@2dfe2c2a7` (base `aa67ab919`).
Status: **NOT READY** — `execution_approved=false`; no outcome cells launched. This file must be
appended after every completed/failed/censored cell. Never record a win/draw for a censored
(CENSORED_CAP) or incomplete cell — those carry verdict `CENSORED_CAP`/`INCOMPLETE_UNKNOWN` only.

## Qualification evidence (not campaign outcomes)

- A5 fixed-tick parity (attempt2): `results/campaign-a5-fixed-tick-parity-20261010-attempt2/A5_memory_probe_receipt.json`
  — IDENTICAL_TAIL_FLUSH, cap 16001 < natural end, seed/fingerprint/records verified, peaks 8.55/8.53 GiB. Qualification only.
- Dry-run: `NO_LAUNCH games=64 variants=32 seats=144 worker_hours=11.07 slot_wall_hours=7.29 launches=0`
  (all 144 generated-map seats proven). Focused tests 31/31 + 4 subtests.

## Switch outcomes

(None yet — awaiting maintainer `execution_approved` + merge + RAM/serial gates.)

| cell_id | game_uid | status | winner | map | seed | receipt SHA | notes |
|---|---|---|---|---|---|---|---|
