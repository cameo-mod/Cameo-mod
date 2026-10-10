## Resume

Next: obtain lead/maintainer approval and A5 serial reservation; replace the stale dry-run manifest fixture with a current reviewed manifest before final manifest freeze. The runner branch merge-base is `origin/master@964cdb630b1514e1c1a0baed55cbdbc427d5fc11`; latest code-bearing runner commit remains `8cfec8f9e9cb5be44738de027fb6c733d4fd8eb8`. No campaign or parity launch, boot, master push, or integration has occurred.

Implemented in the runner branch: strict receipt shape/cross-field validation and atomic immutable JSON write; per-process private-byte and system-free-RAM monitor with a 6.5 GiB stop, 8 GiB hard ceiling, tick/wall/stall observation, exact in-process seed pin, actor sampling, and PID-scoped child cleanup. The module attaches supervised process fields to a validated receipt without inferring natural outcomes from exit code; cap classification requires the exact 45k marker, seed proof, and support bundle, otherwise it records INCOMPLETE. It still does not integrate a campaign queue, generate resolved seats/metrics from match records, or start campaign jobs. Required gates remain approval, final manifest/hash lock, engine map acceptance/spawn symmetry, A5 exact-pin parity, reserved serial slot, and integrated end-to-end receipt/stop/resume proof.

## Progress

### Step 1 — receipt schema contract

`AB_RECEIPT_SCHEMA_2026-10-11.md` defines immutable per-game receipt fields for campaign/source/engine/manifest pins; experiment/setup/map/pair/seed; seed proof; team-to-arm assignment; resolved seats/factions/spawns; natural/cap/incomplete classification and winner; per-seat economy/army/combat/timeline; peak private bytes and actor-count samples; runtime/PID cleanup; and artifact hashes. Caps and incomplete games have no winner. Missing receipts are preserved as incomplete. Exact path sent to Luna DevOps.

### Step 2 — map inventory

`MAP_PREFLIGHT_2026-10-11.md` records all eight amended-scope maps resolved at the exact baseline pin. All package SHAs match the previously pinned assets; parsed playable seat references and spawn actor counts match the required 2/4/6/8 seats. Counts do not prove spawn symmetry or engine acceptance; those remain gates.

### Baseline

Pinned for all remaining work: `origin/master@964cdb630b1514e1c1a0baed55cbdbc427d5fc11` (includes the authorized INTEG-LEARN squash, B3 park fix, and replay identity support). Verified `git merge-base HEAD origin/master` equals that SHA. Do not launch campaign games until approval, final manifest, map gate, A5 parity and serial reservation are verified on the exact baseline.

## Runner receipt validation update

The strict JSON schema from the manager worktree was copied into `tools/ai/ab_campaign_receipt.schema.json`. `campaign_runner.validate_receipt` now checks exact keys, hashes, path safety, team/seat identity consistency, outcome/winner consistency, seed pin equality, timeline ordering, memory-stop classification, artifact hash/missing-reason consistency, and CAP proof. Three focused receipt tests pass; no game or boot process was started. The schema file was not present in the latest fetched campaign branch tip, so it is now included in this runner branch and should be reviewed with the runner changes.

Known focused-suite exception: `test_switch_order_covers_the_frozen_sixty_group_catalog` fails because `SWITCH_ORDER_2026-10-11.md` pins `700bb164` and lacks current `BP_squad_desire`; notified the SWITCH_ORDER owner. Do not delete or weaken that check.

Current verification after receipt-result mapping: `py_compile` passed; receipt-focused and process-guard tests 6/6 passed, including a dummy Python child (no OpenRA) that verifies in-process seed propagation, cap marker parsing, and incomplete-vs-cap classification with/without a support bundle. Campaign planner/analyzer plus pilot tests 28/29 passed; the only failure remains stale SWITCH_ORDER catalog coverage. `git diff --check` passed. Exact-pinned no-launch CLI dry run: 64 games, 32 generated maps, one treatment change, 11.07 worker hours, 7.29 parallel-slot wall hours, zero launches. Input manifest SHA-256 `ed81da06a3ddd542ab25c5b3fab7c5cd17ffa6ea2806abf649a0d50baa990a3d`; fixtures `tools/tests/fixtures/ab_campaign_pilot_dry_run.json` and `tools/tests/fixtures/ab_campaign_pilot_dry_run_receipt.json`. No full `tools/tests` run was started.

## 2026-10-10 17:20Z source-only refresh

- Rechecked isolated branch: HEAD and `origin/codex/ab-campaign-runner-final` both `8cfec8f9e9cb5be44738de027fb6c733d4fd8eb8`; merge-base with `origin/master` is `964cdb630b1514e1c1a0baed55cbdbc427d5fc11`.
- Independently recomputed SHA-256 for all eight `.oramap` package paths pinned in `ab_campaign_pilot.py`; all eight matched. This establishes package identity only, not engine acceptance or spawn symmetry.
- Ran no-launch generated-map preflight with the current test helper's dynamically built, explicitly unapproved manifest: 32 variant `map.yaml` files / 144 seat bindings proved bot, faction, home, and spawn mapping; planned 64 games, launches 0. This is generator evidence only, not a substitute for the reviewed final manifest, engine acceptance, or spawn-symmetry gate.
- Static MapSize/mpspawn review using the runner's current `split_spawn_sides` order found exact D4-reflection/rotation partitions for Satan's Clutch, Red Spice, Terra Cotta, Back to Basics, and Winter's End. No exact grid symmetry exists under any 4+4 partition for Great Sahara 2 or Ice Cold, nor for A Nuclear Winter's 1v1 pair; Ice Cold is same-shape only after a 2-cell vertical translation, and Nuclear Winter is off by 2 y-cells under half-turn. This is spawn-coordinate geometry only and not a terrain/economy fairness test; keep these maps behind explicit map review or replace them before campaign use.
- The tracked `tools/tests/fixtures/ab_campaign_manifest_dry_run.json` is an older contract (`schema/switch mismatch`) and cannot serve as the current pilot manifest. No generated-map preflight was claimed from it.
- `SPEC_2026-10-11_ab_campaign.md` still says proposal / no launches authorized; `OPEN_DECISIONS.md` retains unresolved Amendment 2 checkpoint allocation. A5 parity, final reviewed manifest, generated map seat proof, engine acceptance/symmetry, and serial slot reservation remain open. No game launched.
