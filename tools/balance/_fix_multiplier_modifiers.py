#!/usr/bin/env python3
"""One-time migration: convert decimal *Multiplier Modifier values to integer percent.

OpenRA *Multiplier traits use Modifier as an integer percentage (e.g. 89 = 89%).
Values written as decimals (e.g. 0.89) are incorrect and are converted here to
int(round(value * 100)).
"""
import pathlib
import re
import sys

ROOT = pathlib.Path(__file__).resolve().parents[2]
YAML_ROOT = ROOT / "mods" / "cameo"

TRAIT_RE = re.compile(r"^(\t+)([A-Za-z0-9_]+Multiplier(?:@[A-Za-z0-9_-]+)?)\s*:\s*$")
MOD_RE = re.compile(r"^(\t+)Modifier\s*:\s*(\S.*)$")


def clean_value(s: str) -> str:
    return s.split("#", 1)[0].strip()


def is_decimal(s: str) -> bool:
    """True if the value string contains a dot or exponent (i.e., is not an integer literal)."""
    s = clean_value(s)
    if not s:
        return False
    if "." in s or "e" in s.lower():
        return True
    return False


def process_file(path: pathlib.Path):
    text = path.read_text(encoding="utf-8-sig")
    lines = text.split("\n")
    changed = []
    current_actor = None
    i = 0
    while i < len(lines):
        line = lines[i]
        # top-level block key (actor or template)
        if line and not line.startswith("\t") and ":" in line and not line.startswith("#"):
            current_actor = line.split(":")[0].strip()
        m = TRAIT_RE.match(line)
        if m:
            trait_indent = len(m.group(1))
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
                    if is_decimal(val_str):
                        try:
                            old = float(val_str)
                            new = int(round(old * 100))
                            lines[j] = f"{mm.group(1)}Modifier: {new}"
                            changed.append((current_actor, m.group(2), val_str, new, j + 1))
                        except ValueError:
                            pass
                j += 1
        i += 1
    if changed:
        path.write_text("\n".join(lines), encoding="utf-8")
    return changed


def main():
    total = 0
    files = 0
    for path in sorted(YAML_ROOT.rglob("*.yaml")):
        changed = process_file(path)
        if changed:
            files += 1
            total += len(changed)
            rel = path.relative_to(ROOT)
            print(f"{rel}:")
            for actor, trait, old, new, line in changed:
                print(f"  {actor or '?'}/{trait} line {line}: {old} -> {new}")
    print(f"\nfixed {total} decimal Modifier(s) in {files} file(s)")
    return 1 if total else 0


if __name__ == "__main__":
    sys.exit(main())
