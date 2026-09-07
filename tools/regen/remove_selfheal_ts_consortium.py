"""Remove `ChangesHealth@SelfHealing` nodes that carry NOTHING but `Step:` — EMBER's share
of the regen conversion (TASK_2026-09-07_regen_conversion.md).

The new `ScaledSelfHeal` trait (shipped inert on master `4afa00095`) derives the heal
amount from MaxHP, so the per-actor `Step` overrides are dead weight once `defaults.yaml`
flips. Nodes carrying ANY other field (`StartIfBelow`, `Delay`, `DamageCooldown`, …) are
deliberate per-actor behaviour — reported, not touched.

Block removal is indentation-based: the node line plus every following line that is
indented deeper. The safety rule is `children == ["Step"]` exactly — a bare node or one
with extra fields is left in place and listed.

Usage: python tools/regen/remove_selfheal_ts_consortium.py [--apply]
"""
import pathlib
import re
import sys

ROOT = pathlib.Path(__file__).resolve().parents[2]
PACKS = [
    ROOT / "mods" / "cameo" / "ContentPacks" / "TiberianSun",
    ROOT / "mods" / "cameo" / "ContentPacks" / "RedAlert2Mod" / "Consortium",
]
NODE = re.compile(r"^(\s*)ChangesHealth@SelfHealing:\s*$")


def scan(lines):
    """(step_only_ranges, kept) — ranges are (start, end) line indexes to delete."""
    drop, kept = [], []
    i = 0
    while i < len(lines):
        m = NODE.match(lines[i])
        if not m:
            i += 1
            continue
        ind = len(m.group(1))
        j = i + 1
        kids = []
        while j < len(lines):
            line = lines[j]
            if line.strip() and (len(line) - len(line.lstrip())) <= ind:
                break
            if line.strip():
                kids.append(line.strip().split(":", 1)[0].strip())
            j += 1
        if kids == ["Step"]:
            drop.append((i, j))
        else:
            kept.append((i + 1, kids))
        i = j
    return drop, kept


def main():
    apply_ = "--apply" in sys.argv
    total_dropped = 0
    for pack in PACKS:
        for f in sorted(pack.rglob("*.yaml")):
            lines = f.read_text(encoding="utf-8").splitlines()
            drop, kept = scan(lines)
            if not drop and not kept:
                continue
            rel = f.relative_to(ROOT)
            for ln, kids in kept:
                print(f"  KEPT (extra fields) {rel}:{ln} children={kids}")
            if drop:
                total_dropped += len(drop)
                print(f"  {rel}: removing {len(drop)} node(s)")
                if apply_:
                    cut = {i for a, b in drop for i in range(a, b)}
                    f.write_text("\n".join(l for n, l in enumerate(lines)
                                         if n not in cut) + "\n",
                                 encoding="utf-8")
    print(f"{'REMOVED' if apply_ else 'would remove'} {total_dropped} Step-only nodes")


if __name__ == "__main__":
    main()
