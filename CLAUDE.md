# Cameo-mod

## Required reading, in order

1. **`docs/DESIGN.md`** — the binding design contract (naming grammar, stat
   formulas, tech tiers, content-pack layout, description scheme, agent
   operating rules). Read it before touching any yaml.
2. **`docs/audit/SUMMARY.md`** — current known-issue state by bug class.
3. `docs/MASTER_REPORT.md` — long-form analysis, bug taxonomy (B1–B12),
   roadmap; consult §9/§10/§13 when DESIGN.md is not enough.

## Tooling

- `tools/audit/run_all.sh` — full audit suite (run before/after changes;
  single checks: `python tools/audit/audit_<name>.py`).
- `tools/rename/apply.py` + `rename_map_<faction>.yaml` — naming migration.
- `tools/packs/split_faction.py` — ContentPack extraction.
- `tools/audit/dump_resolved.py` — resolved-ruleset snapshots; refactors
  must diff empty.

## Memory

Before running any shell command that has a corresponding memory file (build commands, engine sync, git operations), **read that memory file in full before executing**.
