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


def family_from(name: str) -> str:
    """'^Warhead_Family_Level:' -> 'Family'."""
    parts = name.split("_")
    return "_".join(parts[1:-1]) if len(parts) >= 3 else name


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
    wanted = {f.lower() for f in fams}
    # Always run the full generator so shield_uniqueness sees the whole set and
    # produces correct final Shield values; then keep only the requested blocks.
    out = subprocess.run([sys.executable, str(GEN)], capture_output=True, text=True)
    if out.returncode:
        sys.exit("generator failed:\n" + out.stderr)
    all_gen = parse_blocks(out.stdout)
    if wanted:
        gen = {n: b for n, b in all_gen.items()
               if family_from(n).lower() in wanted}
    else:
        gen = all_gen

    # READ WITH newline="" OR THE LINE-ENDING DETECTION BELOW IS A NO-OP. read_text applies
    # universal-newline translation, so a CRLF file arrives already normalised and the CRLF
    # check never fires; write_text then translates back to os.linesep, so on Windows this
    # tool rewrote EVERY line of weapons.yaml whatever the file actually used.
    #
    # MEASURED, because the obvious conclusion is wrong: the COMMITTED diff was never at risk.
    # .gitattributes carries "*.yaml eol=lf" and "* text=lf", so git normalises on add and
    # shows the same 12 changed lines whichever ending is on disk -- verified by writing CRLF
    # and re-running git diff. What the rewrite actually costs is the WORKING TREE: a plain
    # (non-git) diff reports all 19,882 lines, byte-comparing tools see a fully changed file,
    # editors churn, and git prints a warning on every touch. That is a detour, not a
    # corruption -- but it is free to avoid, and the protection is one .gitattributes edit away
    # from disappearing for any path the patterns stop covering.
    with F.open(encoding="utf-8", newline="") as fh:
        text = fh.read()
    newline = "\r\n" if "\r\n" in text else "\n"
    flines = text.split(newline)
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
    missing = sorted(set(gen) - set(replaced))
    if missing:
        for m in missing:
            result.append("")
            result.extend(gen[m])
        replaced += missing
    with F.open("w", encoding="utf-8", newline="") as fh:
        fh.write(newline.join(result))
    print(f"spliced {len(replaced)} blocks: {', '.join(replaced)}")


if __name__ == "__main__":
    main()
