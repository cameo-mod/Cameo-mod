# audit_periodic_freshness — mandatory recurring audits

Registry: `docs/audit/periodic.json` — grace **7** days. BROKEN: **0**, OVERDUE: **6**, DUE: **0**

| id | title | cadence (d) | age (d) | due in (d) | state | owner |
|---|---|---|---|---|---|---|
| code_duplication | Refactor duplicated code (audit tooling + yaml templates) | 30 | 55 | -25 | OVERDUE | unassigned |
| test_coverage | Test coverage floor (OpenRA.Mods.Cameo + tools/) | 30 | 55 | -25 | OVERDUE | unassigned |
| recent_changes_review | Review recent changes (regression review of the last N days of commits) | 14 | 28 | -14 | OVERDUE | unassigned |
| error_handling | Error handling in tools/ (bare except, silent pass, unguarded IO) | 30 | 55 | -25 | OVERDUE | unassigned |
| security_scan | Security scan (dependencies, secrets, unsafe shell/deserialisation) | 14 | 28 | -14 | OVERDUE | unassigned |
| armor_exposure | Re-measure armor exposure (coverage x intensity) — it drifts with every weapon change | 30 | 50 | -20 | OVERDUE | unassigned |


## OVERDUE — a scheduled scan is late

- code_duplication (55d old, cadence 30d)
- test_coverage (55d old, cadence 30d)
- recent_changes_review (28d old, cadence 14d)
- error_handling (55d old, cadence 30d)
- security_scan (28d old, cadence 14d)
- armor_exposure (50d old, cadence 30d)

Run the command from the registry, then stamp it with `--record <id>`.
The tree itself is fine — this is a calendar fact, so it does NOT block the per-commit suite.

