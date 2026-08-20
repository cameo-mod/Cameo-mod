#!/usr/bin/env python3
"""splice_templates.py — regenerate the named weapon families and replace their
`^Warhead_<Family>_<Level>` blocks in mods/cameo/weapons/weapons.yaml in place.

Line-based, structure-preserving: each old block (header + its indented body) is
swapped for the regenerated one; blank separators and all other content are kept.
After splicing, `verify_generator_sync.py` should report drift = 0.

Usage: python tools/balance/splice_templates.py laser railgun tesla teslacharged prism
       python tools/balance/splice_templates.py --all      # every family the generator emits
"""
import subprocess
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
F = ROOT / "mods" / "cameo" / "weapons" / "weapons.yaml"
GEN = Path(__file__).with_name("gen_weapon_template.py")


def parse_blocks(text):
    """{header_without_colon: [header line, indented body lines...]}"""
    blocks, lines, i = {}, text.split("\n"), 0
    while i < len(lines):
        ln = lines[i]
        if ln.startswith("^Warhead_") and ln.rstrip().endswith(":"):
            name = ln.rstrip()[:-1]
            body = [ln]
            i += 1
            while i < len(lines) and lines[i] and lines[i][0] in " \t":
                body.append(lines[i])
                i += 1
            blocks[name] = body
        else:
            i += 1
    return blocks


def main():
    fams = sys.argv[1:]
    if not fams:
        sys.exit("usage: splice_templates.py <family> ... | --all")
    # The generator emits EVERY family when given no family filter, so `--all` is
    # simply the empty filter. Kept explicit rather than implicit: a bare
    # `splice_templates.py` rewriting all 88 templates by accident is not a mistake
    # anyone should be able to make by hitting return.
    if fams == ["--all"]:
        fams = []
    out = subprocess.run([sys.executable, str(GEN), *fams], capture_output=True, text=True)
    if out.returncode:
        sys.exit("generator failed:\n" + out.stderr)
    gen = parse_blocks(out.stdout)

    flines = F.read_text(encoding="utf-8").split("\n")
    result, replaced, i = [], [], 0
    while i < len(flines):
        ln = flines[i]
        if ln.startswith("^Warhead_") and ln.rstrip().endswith(":") and ln.rstrip()[:-1] in gen:
            name = ln.rstrip()[:-1]
            i += 1
            while i < len(flines) and flines[i] and flines[i][0] in " \t":
                i += 1  # skip old block body
            result.extend(gen[name])
            replaced.append(name)
        else:
            result.append(ln)
            i += 1
    F.write_text("\n".join(result), encoding="utf-8")
    print(f"spliced {len(replaced)} blocks: {', '.join(replaced)}")
    missing = sorted(set(gen) - set(replaced))
    if missing:
        print("WARNING: generated block not present in file (NOT spliced):", missing)


if __name__ == "__main__":
    main()
