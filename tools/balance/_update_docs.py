#!/usr/bin/env python3
"""Update LESSONS_LEARNED.md and BALANCE_PIPELINE.md for multiplier and auto-audit rules."""
import pathlib
import re

ROOT = pathlib.Path(__file__).resolve().parents[2]

# --- LESSONS_LEARNED.md ---
ll = ROOT / "docs" / "LESSONS_LEARNED.md"
text = ll.read_text(encoding="utf-8")

multiplier_section = """\n### Multiplier formatting\n\n- All OpenRA `*Multiplier` traits (`FirepowerMultiplier`, `DamageMultiplier`, `RangeMultiplier`, `ReloadDelayMultiplier`, `SpeedMultiplier`, `InaccuracyMultiplier`, etc.) use `Modifier` as an **integer percentage in 1 % steps**.\n- `89` means 89 %, `100` means 100 %, `125` means 125 %.\n- Decimal `Modifier` values such as `0.89` are wrong and must be converted to `89`.\n- `tools/balance/apply_balance.py` and `tools/balance/extract_stats.py` now convert between the ledger fraction (`0.89`) and the YAML integer (`89`) automatically.\n- `tools/audit/audit_multiplier_modifiers.py` flags any non-integer `*Multiplier Modifier` value.\n"""

# Insert before "### Balance tooling discipline"
text = text.replace("\n### Balance tooling discipline", multiplier_section + "\n### Balance tooling discipline")

# Add auto-audit bullet before "### Data hygiene"
auto_audit_bullet = "\n- **After every `apply_balance.py --confirm` run, `extract_stats.py` and `audit_multiplier_modifiers.py` execute automatically**. A full audit (`tools/balance/_run_full_audit.py` or `tools/audit/run_all.sh`) is still mandatory before commit.\n"
text = text.replace("\n- Do not rely on the generic `propose_class_rebalance.py` for curated classes while ledger `class_anchor`, `subtype`, and weapon stats are stale.\n\n### Data hygiene",
                    "\n- Do not rely on the generic `propose_class_rebalance.py` for curated classes while ledger `class_anchor`, `subtype`, and weapon stats are stale." + auto_audit_bullet + "\n### Data hygiene")

ll.write_text(text, encoding="utf-8")
print("updated docs/LESSONS_LEARNED.md")

# --- BALANCE_PIPELINE.md ---
bp = ROOT / "docs" / "design" / "BALANCE_PIPELINE.md"
text = bp.read_text(encoding="utf-8")

text = text.replace(
    "7. verify  drift audit: yaml ≡ ledger    (runs in run_all.sh forever)\n```",
    "7. verify  drift audit: yaml ≡ ledger    (runs in run_all.sh forever)\n8. verify  multiplier audit: all `*Multiplier Modifier` values are integer percentages    (`tools/audit/audit_multiplier_modifiers.py`)\n```",
)

text = text.replace(
    "| `balance push [--faction X]` | ledger → yaml via provenance anchors | **maintainer order only**; prints diff; then boot gate + audits |",
    "| `balance push [--faction X]` | ledger → yaml via provenance anchors | **maintainer order only**; prints diff; auto-runs `extract_stats.py` + `audit_multiplier_modifiers.py`; full `run_all.sh` + boot gate before commit |",
)

bp.write_text(text, encoding="utf-8")
print("updated docs/design/BALANCE_PIPELINE.md")
