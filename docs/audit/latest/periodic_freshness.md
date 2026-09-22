# audit_periodic_freshness — mandatory recurring audits

Registry: `docs/audit/periodic.json` — grace **7** days. BROKEN: **0**, OVERDUE: **4**, DUE: **2**

| id | title | cadence (d) | age (d) | due in (d) | state | owner |
|---|---|---|---|---|---|---|
| code_duplication | Refactor duplicated code (audit tooling + yaml templates) | 30 | 43 | -13 | OVERDUE | unassigned |
| test_coverage | Test coverage floor (OpenRA.Mods.Cameo + tools/) | 30 | 43 | -13 | OVERDUE | unassigned |
| recent_changes_review | Review recent changes (regression review of the last N days of commits) | 14 | 16 | -2 | DUE | unassigned |
| error_handling | Error handling in tools/ (bare except, silent pass, unguarded IO) | 30 | 43 | -13 | OVERDUE | unassigned |
| security_scan | Security scan (dependencies, secrets, unsafe shell/deserialisation) | 14 | 16 | -2 | DUE | unassigned |
| armor_exposure | Re-measure armor exposure (coverage x intensity) — it drifts with every weapon change | 30 | 38 | -8 | OVERDUE | unassigned |


## DUE — run these next

- recent_changes_review (16d old, cadence 14d)
- security_scan (16d old, cadence 14d)


## OVERDUE — a scheduled scan is late

- code_duplication (43d old, cadence 30d)
- test_coverage (43d old, cadence 30d)
- error_handling (43d old, cadence 30d)
- armor_exposure (38d old, cadence 30d)

Run the command from the registry, then stamp it with `--record <id>`.
The tree itself is fine — this is a calendar fact, so it does NOT block the per-commit suite.

