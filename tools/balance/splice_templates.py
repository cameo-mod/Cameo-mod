#!/usr/bin/env python3
"""splice_templates.py — regenerate the named weapon families and replace their
`^Warhead_<Family>_<Level>` blocks in mods/cameo/weapons/weapons.yaml in place.

Line-based, structure-preserving: each old block (header + its indented body) is
swapped for the regenerated one; blank separators and all other content are kept.
After splicing, `verify_generator_sync.py` should report drift = 0.

Usage: python tools/balance/splice_templates.py laser railgun tesla teslacharged prism
       python tools/balance/splice_templates.py --all      # every family the generator emits
"""
import re
import subprocess
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
F = ROOT / "mods" / "cameo" / "weapons" / "weapons.yaml"
GEN = Path(__file__).with_name("gen_weapon_template.py")


def parse_blocks(text, prefixes=("^Warhead_",)):
    """{header_without_colon: [header line, indented body lines...]}

    `prefixes` widens what counts as a block. It defaults to the templates the
    generator emits; `refresh_compatibility_copies` also needs `^Compatibility_`,
    and a default that silently excluded them is what made the first version of
    that pass report "0 refreshed" while 51 copies were out of sync.
    """
    blocks, lines, i = {}, text.split("\n"), 0
    while i < len(lines):
        ln = lines[i]
        if ln.startswith(prefixes) and ln.rstrip().endswith(":"):
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



# --------------------------------------------------------------------------- #
# The FROZEN COPIES — `^Compatibility_<Family>_<Level>Flat`
# --------------------------------------------------------------------------- #
# ⛔ A `^Compatibility_*Flat` template is a VERBATIM COPY of a `^Warhead_*` main
# warhead body, pinned into weapons.yaml by the 3-way-split consolidators so a
# retrofitted weapon keeps the exact profile it had. It is a COPY, not an
# inherit — so regenerating the canonical template silently DESYNCHRONISES it,
# and the consolidators cannot repair that: they skip anything already
# consolidated (`already = COMPATIBILITY_KEY in mains ...`), by design, because
# they are one-shot migrations.
#
# Measured 2026-08-30, when the emitter switched from `class_tilt` to §12.0i's
# bell: 54 of 54 copies matched their canonical before the splice, **3 of 54
# after**. The damage was not theoretical — the desync broke two PAID-UPGRADE
# contracts outright (`OfficerMachineGunAP` and `TS30mmRail` came out WEAKER
# than the weapons they are bought to replace), because the base weapon reads
# the frozen copy while its upgrade reads the live template.
#
# So the copies are refreshed HERE, as part of the same splice that moved the
# canonical. Two fields belong to the copy and are carried across untouched:
# `Damage: 0` and `PercentageScale: 0` are what make it a compatibility SHELL
# whose magnitude the weapon supplies.
COMPAT_RE = re.compile(r"^\^Compatibility_(\w+?)_(Light|Medium|Heavy|Super|Trace)Flat$")
COMPAT_OWN_FIELDS = ("Damage", "PercentageScale")


def _indent(line):
    return len(line) - len(line.lstrip("\t "))


def main_warhead_span(body):
    """(start, end) of the FIRST `Warhead@...:` sub-node inside a template block."""
    start = ind = None
    for i, ln in enumerate(body):
        if start is None:
            if ln.strip().startswith("Warhead@"):
                start, ind = i, _indent(ln)
        elif ln.strip() and _indent(ln) <= ind:
            return start, i
    return (start, len(body)) if start is not None else None


def refresh_compat_copy(compat_body, canon_body, name):
    """The compat block, with its main warhead body re-copied from the canonical.

    Fails CLOSED: a copy whose own fields are not all present in the canonical is
    not the same shape any more, and guessing would be worse than stopping.
    """
    cspan, kspan = main_warhead_span(compat_body), main_warhead_span(canon_body)
    if cspan is None or kspan is None:
        raise SystemExit(f"{name}: no main warhead sub-node to refresh")
    cs, ce = cspan
    ks, ke = kspan
    own = {ln.strip().split(":", 1)[0]: ln for ln in compat_body[cs + 1:ce]
           if ln.strip().split(":", 1)[0] in COMPAT_OWN_FIELDS}
    rebuilt, seen = [compat_body[cs]], set()          # keep the copy's OWN warhead key
    for ln in canon_body[ks + 1:ke]:
        key = ln.strip().split(":", 1)[0]
        if key in own:
            seen.add(key)
            rebuilt.append(own[key])
        else:
            rebuilt.append(ln)
    absent = sorted(set(own) - seen)
    if absent:
        raise SystemExit(f"{name}: own field(s) {absent} absent from the canonical — "
                         f"the copy no longer has the canonical's shape, refusing to guess")
    return compat_body[:cs] + rebuilt + compat_body[ce:]


def refresh_compatibility_copies(newline, spliced):
    """Re-copy each `^Compatibility_*Flat` body from the canonical that just moved.

    ⛔ Scoped to `spliced` — the templates THIS run rewrote — and not to every
    canonical that happens to exist. `^Warhead_Nuclear_Super` and
    `^Warhead_Sniper_Light` are HAND_TUNED, the generator never emits them, and
    their copies have diverged in SHAPE (no `PercentageScale`). Refreshing a
    template that did not move is pointless; refusing to is what keeps the
    fail-closed guard below meaningful instead of routinely tripped.
    """
    text = F.read_text(encoding="utf-8")
    blocks = parse_blocks(text, ("^Warhead_", "^Compatibility_"))
    updates = {}
    for name, body in blocks.items():
        m = COMPAT_RE.match(name)
        if not m:
            continue
        canon = f"^Warhead_{m.group(1)}_{m.group(2)}"
        if canon not in blocks or canon not in spliced:
            continue
        fresh = refresh_compat_copy(body, blocks[canon], name)
        if fresh != body:
            updates[name] = fresh
    if not updates:
        print("compatibility copies: 0 refreshed (all already in sync)")
        return
    lines, result, i = text.split(newline), [], 0
    while i < len(lines):
        ln = lines[i]
        nm = ln.rstrip()[:-1] if ln.rstrip().endswith(":") else None
        if nm in updates and ln.startswith("^Compatibility_"):
            i += 1
            while i < len(lines) and lines[i] and lines[i][0] in " \t":
                i += 1
            result.extend(updates[nm])
        else:
            result.append(ln)
            i += 1
    F.write_text(newline.join(result), encoding="utf-8")
    print(f"compatibility copies: {len(updates)} refreshed from their canonical "
          f"({', '.join(sorted(updates))})")


def main():
    argv = sys.argv[1:]
    # Generator flags are forwarded, so the heaviness-bell switch is one command
    # (`--all --tilt=bell`) rather than an edit to `gen_weapon_template.TILT_MODEL`
    # that someone has to remember to revert. Everything else is a family filter.
    gen_flags = [a for a in argv if a.startswith("--") and a != "--all"]
    fams = [a for a in argv if a not in gen_flags]
    if not fams:
        sys.exit("usage: splice_templates.py <family> ... | --all  [--tilt=bell]")
    # The generator emits EVERY family when given no family filter, so `--all` is
    # simply the empty filter. Kept explicit rather than implicit: a bare
    # `splice_templates.py` rewriting all 88 templates by accident is not a mistake
    # anyone should be able to make by hitting return.
    if fams == ["--all"]:
        fams = []
    wanted = {f.lower() for f in fams}
    # Always run the full generator so shield_uniqueness sees the whole set and
    # produces correct final Shield values; then keep only the requested blocks.
    out = subprocess.run([sys.executable, str(GEN)] + gen_flags,
                         capture_output=True, text=True)
    if out.returncode:
        sys.exit("generator failed:\n" + out.stderr)
    all_gen = parse_blocks(out.stdout)
    if wanted:
        gen = {n: b for n, b in all_gen.items()
               if family_from(n).lower() in wanted}
    else:
        gen = all_gen

    text = F.read_text(encoding="utf-8")
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
    F.write_text(newline.join(result), encoding="utf-8")
    print(f"spliced {len(replaced)} blocks: {', '.join(replaced)}")
    # ⛔ NOT optional, and not a separate command. See COMPAT_RE above: a splice that
    # leaves the frozen copies behind ships a corpus where a weapon and its paid
    # upgrade disagree about what the same family's profile is.
    refresh_compatibility_copies(newline, set(replaced))


if __name__ == "__main__":
    main()
