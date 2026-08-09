---
name: run-audits
description: "Run the Cameo audit suite and report results"
triggers:
  - user
  - model
---

# Run-Audits — execute the Cameo audit suite

This skill runs the project's audit tooling and reports findings.

## Quick audit (targeted checks)

For a fast check after weapon conversions, run these individually:

```powershell
# Empty warhead audit (NRE crash detector) -- must be 0
python tools/audit/find_empty_warhead.py

# Orphaned old warhead keys (double-fire bug) -- must be 0 real
python tools/audit/find_orphan_old_keys.py

# AreaDamage type sweep (SpreadDamage regression) -- should be 0 candidates
python tools/balance/sweep_areadamage.py

# Multi-variant orphan check
python tools/audit/find_orphan_old_keys_multi.py

# Generator sync verification (drift should be 1 = ^Warhead_Sniper_Light only)
python tools/balance/verify_generator_sync.py
```

## Full audit suite

The canonical way to regenerate ALL audit reports:

```bash
bash tools/audit/run_all.sh
```

**IMPORTANT:** Use `bash`, NOT PowerShell for this. PowerShell's `>` redirect
writes UTF-16, which corrupts the audit report files. The bash script handles
encoding correctly.

Individual audit scripts can be run with `python tools/audit/audit_<name>.py`:
- `audit_empty_warheads.py` -- empty warhead type NRE detector
- `audit_balance_drift.py` -- yaml vs ledger disagreements
- `audit_multiplier_modifiers.py` -- non-integer Modifier values
- `audit_orphans.py` -- dangling weapon references
- `audit_inherits.py` -- dangling inherit targets
- `audit_weapon_uniqueness.py` -- duplicate weapons per faction
- `audit_nuclear_flash_bindings.py` -- directional flash warhead protection

## Interpreting results

- **0 empty warheads** = safe to commit (no NRE crash risk)
- **0 orphaned old keys** = no double-fire bugs from conversions
- **0 AreaDamage sweep candidates** = no SpreadDamage regressions
- **Generator sync drift = 1** = expected (^Warhead_Sniper_Light is the known exception)
- **Balance drift = 0** = yaml matches committed ledgers

## Ledger refresh

After any weapon conversion, refresh the balance ledgers:

```powershell
python tools/balance/extract_stats.py
```

This updates all `docs/balance/*.json` files. The ledger diff in git shows
what changed structurally (new warhead keys, renamed keys, etc.).

## Phase B survey

To see remaining unconverted weapons:

```powershell
python tools/audit/phase_b_survey.py
```

Output goes to `docs/audit/latest/phase_b_survey.md`.
