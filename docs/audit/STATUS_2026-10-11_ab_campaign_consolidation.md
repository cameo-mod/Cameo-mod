# A/B campaign consolidation status — 2026-10-11

## Current candidate

- Consolidation branch: `codex/ab-campaign-consolidation-final`.
- Game source pin: `e49acf41a1b3699f01d44e4def06ed4bf83af038`.
- Engine pin: `ad061ebf79029da4dcd3a841a9b2540e39cbf968` (lead-approved engine revision; parity is still a separate gate).
- Candidate manifest: `docs/audit/ab_pilot_manifest_candidate.json`; `execution_approved` is `false` and its A5 receipt pin is null until a receipt exists for this exact source and engine pair.
- Frozen memory policy: 10.5 GiB per-process cap, first-game admission at 6,810,783,744 observed peak + 3 GiB, 14 GiB before a second small game, two small games maximum, and large games exclusive. The first-game floor remains conservative pending a measurement on the ad061 binary.

## Gates still open

- Run the fixed-tick A5 pair against the exact e49/ad061 binary pins; the available e49/4f69 receipt does not match the engine pin.
- Complete runtime map and overlay/deployment acceptance with a fresh reservation and current RAM check.
- Finish analyzer fixture/negative-control acceptance and obtain explicit maintainer execution approval before any outcome cells.

No outcome cells were launched from this branch. The focused code tests and no-launch dry-run passed; see the handoff for the exact SHA and counts.
