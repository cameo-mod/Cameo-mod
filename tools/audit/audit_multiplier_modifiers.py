#!/usr/bin/env python3
"""audit_multiplier_modifiers.py — check all *Multiplier Modifier values are integer percent.

OpenRA *Multiplier traits interpret Modifier as an integer percentage
(e.g. 89 = 89%).  Decimal values like 0.89 are a formatting/semantics bug.
Reports any Modifier that is not an integer literal or is not a 1 % step.
"""
import pathlib
import re
import sys

ROOT = pathlib.Path(__file__).resolve().parents[2]
YAML_ROOT = ROOT / "mods" / "cameo"

TRAIT_RE = re.compile(r"^(\t+)([A-Za-z0-9_]+Multiplier(?:@[A-Za-z0-9_-]+)?)\s*:\s*$")
MOD_RE = re.compile(r"^(\t+)Modifier\s*:\s*(\S.*)$")
INTEGER_RE = re.compile(r"^[+-]?\d+$")


def clean_value(s: str) -> str:
    """Strip whitespace and inline comments, e.g. '91 # comment' -> '91'."""
    s = s.split("#", 1)[0].strip()
    return s


def is_bad_modifier(s: str) -> tuple[bool, str]:
    s = clean_value(s)
    if not s:
        return True, "empty value"
    if not INTEGER_RE.match(s):
        return True, "non-integer"
    return False, ""


def audit_file(path: pathlib.Path):
    text = path.read_text(encoding="utf-8-sig")
    lines = text.split("\n")
    current_actor = None
    findings = []
    trait_names = set()
    for i, line in enumerate(lines):
        if line and not line.startswith("\t") and ":" in line and not line.startswith("#"):
            current_actor = line.split(":")[0].strip()
        m = TRAIT_RE.match(line)
        if m:
            trait_indent = len(m.group(1))
            trait_names.add(m.group(2))
            j = i + 1
            while j < len(lines):
                inner = lines[j]
                if not inner.startswith("\t"):
                    break
                inner_indent = len(inner) - len(inner.lstrip("\t"))
                if inner_indent <= trait_indent:
                    break
                mm = MOD_RE.match(inner)
                if mm and len(mm.group(1)) > trait_indent:
                    val_str = mm.group(2).strip()
                    bad, reason = is_bad_modifier(val_str)
                    if bad:
                        try:
                            suggested = int(round(float(val_str) * 100)) if not INTEGER_RE.match(val_str) else val_str
                        except ValueError:
                            suggested = "?"
                        findings.append({
                            "actor": current_actor or "?",
                            "trait": m.group(2),
                            "file": path.relative_to(ROOT).as_posix(),
                            "line": j + 1,
                            "value": val_str,
                            "reason": reason,
                            "suggested": suggested,
                        })
                j += 1
    return findings, trait_names


def main():
    all_findings = []
    trait_names = set()
    for path in sorted(YAML_ROOT.rglob("*.yaml")):
        f, t = audit_file(path)
        all_findings.extend(f)
        trait_names.update(t)

    print("# audit_multiplier_modifiers — *Multiplier Modifier integer percent check\n")
    print(f"*Multiplier trait families seen: {len(trait_names)}\n")
    if trait_names:
        print("| trait family |")
        print("|---|")
        for n in sorted(trait_names):
            print(f"| `{n}` |")
        print()

    print(f"Non-integer Modifier values: **{len(all_findings)}**\n")
    if all_findings:
        print("| file | actor | trait | line | value | reason | suggested fix |")
        print("|---|---|---|---|---|---|---|")
        for f in all_findings:
            print(f"| {f['file']} | `{f['actor']}` | `{f['trait']}` | {f['line']} | {f['value']} | {f['reason']} | {f['suggested']} |")
        return 1
    print("All *Multiplier Modifier values are integer percentages.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
