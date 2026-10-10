## Resume

Step 1 schema published to Luna DevOps at `765a39b00f8c6213deae5ba5f92e575b7bf3dac7`; Step 2 map inventory complete. Step 3: update tooling to exact `origin/master@964cdb630b1514e1c1a0baed55cbdbc427d5fc11`, implement receipt-producing resume-safe runner with 6.5 GiB per-game monitor, actor samples, 45k cap, concurrency policy/slot lease, wall/stall bounds and PID-scoped cleanup; fixture/negative tests and no-launch dry run. No launches until dry run and exact-baseline A5 parity pass and required B3 landing. Engine acceptance/symmetric spawn proof and executable manifest remain gates.

## Progress

### Step 1 — receipt schema contract

`AB_RECEIPT_SCHEMA_2026-10-11.md` defines immutable per-game receipt fields for campaign/source/engine/manifest pins; experiment/setup/map/pair/seed; seed proof; team-to-arm assignment; resolved seats/factions/spawns; natural/cap/incomplete classification and winner; per-seat economy/army/combat/timeline; peak private bytes and actor-count samples; runtime/PID cleanup; and artifact hashes. Caps and incomplete games have no winner. Missing receipts are preserved as incomplete. Exact path sent to Luna DevOps.

### Step 2 — map inventory

`MAP_PREFLIGHT_2026-10-11.md` records all eight amended-scope maps resolved at the exact baseline pin. All package SHAs match the previously pinned assets; parsed playable seat references and spawn actor counts match the required 2/4/6/8 seats. Counts do not prove spawn symmetry or engine acceptance; those remain gates.

### Baseline

Pinned for all remaining work: `origin/master@964cdb630b1514e1c1a0baed55cbdbc427d5fc11` (includes `a55954c18` and replay identity support). B3 is not yet an ancestor of this master pin; no campaign game may launch until the required B3 landing and A5 parity gates are verified on the exact baseline.
