#!/usr/bin/env python3
"""audit_three_way_split.py — ONE warhead, ONE projectile, ONE effect. No bundles.

    python tools/audit/audit_three_way_split.py

Maintainer 2026-08-22, looking at `IxianCombatTankCannon`: *"has 2 projectiles and 2 effects and
2 warheads and then the d2k cannon on top? can we please finish the 3 way split so there are no
more multiple of those things there?"*

    IxianCombatTankCannon:
        Inherits:   ^Warhead_CannonHE_Heavy      <- warhead 1
        Inherits@2: ^Projectile_Shell_Heavy      <- projectile 1
        Inherits@3: ^Effect_CannonHE_Heavy       <- effect 1
        Inherits@4: ^Warhead_CannonAP_Light      <- warhead 2
        Inherits@5: ^Projectile_Shell_Light      <- projectile 2
        Inherits@6: ^Effect_CannonAP_Light       <- effect 2
        Inherits@7: ^D2K_Cannon                  <- a BUNDLE carrying a third of each

WEAPON_3WAY_SPLIT.md's whole point is that a weapon is exactly three independent layers. Stacking
two of a layer means the last one silently wins for the single-valued parts (`Projectile:` is ONE
node — a second template does not add a projectile, it REPLACES fields of the first), while the
multi-valued parts accumulate: that is how a weapon ends up firing three warheads it was never
meant to have, and it is the source of the `multi_main_fired_weapons` backlog.

A LEGACY BUNDLE is a `^Template` that is not itself a layer but supplies one — `^D2K_Cannon`
inherits `^Warhead_CannonHE_Medium` AND carries its own `Warhead@` override, so it is a whole
weapon wearing a template's clothes. Mixing one with explicit layers is the worst case, because
the bundle's own balance numbers land on top of the layers the weapon chose.

⚠ RATCHET, LOWER-ONLY. The tree starts with a known backlog; this locks it in and lets W24 burn
it down. Never raise it to make the suite green.

EXIT CODE: 1 above the ratchet.
"""
from __future__ import annotations

import collections
import pathlib
import re
import sys

if hasattr(sys.stdout, "reconfigure"):          # Windows consoles default to cp1252
    sys.stdout.reconfigure(encoding="utf-8")

sys.path.insert(0, str(pathlib.Path(__file__).resolve().parent))
from miniyaml import load_manifest  # noqa: E402

# Weapons violating the split when this audit was written (2026-08-22). LOWER ONLY.
SPLIT_BASELINE = 393

ACTOR_RE = re.compile(r"^(?P<name>[^\s#][^:]*):\s*$")
LAYERS = ("^Warhead_", "^Projectile_", "^Effect_")


def blocks():
    """(file, name, [inherit targets]) for every top-level node in the live weapon files."""
    for path in sorted({p.resolve() for p in load_manifest(pathlib.Path(".")).weapons}):
        lines = path.read_text(encoding="utf-8", errors="surrogateescape").split("\n")
        i = 0
        while i < len(lines):
            m = ACTOR_RE.match(lines[i])
            if not (m and not lines[i][0].isspace()):
                i += 1
                continue
            name, j = m.group("name").strip(), i + 1
            while j < len(lines) and (not lines[j].strip() or lines[j][:1] in ("\t", " ")):
                j += 1
            body = lines[i + 1:j]
            inherits = [b.split(":", 1)[1].strip() for b in body
                        if b.lstrip().startswith("Inherits") and ":" in b]
            yield path, name, body, inherits
            i = j


def main() -> int:
    # A template counts as a BUNDLE when it supplies a layer without being one.
    bundles = set()
    for _path, name, body, inherits in blocks():
        if not name.startswith("^") or name.startswith(LAYERS):
            continue
        supplies = (any(t.startswith(LAYERS) for t in inherits)
                    or any(b.lstrip().startswith(("Warhead@", "Projectile:")) for b in body))
        if supplies:
            bundles.add(name)

    rows, counts = [], collections.Counter()
    for _path, name, _body, inherits in blocks():
        if name.startswith("^"):
            continue
        n = {p: sum(1 for t in inherits if t.startswith(p)) for p in LAYERS}
        used = [t for t in inherits if t in bundles]
        why = []
        for p in LAYERS:
            if n[p] > 1:
                why.append(f"{n[p]}x {p}*")
                counts[f">1 {p}*"] += 1
        if used and any(n.values()):
            why.append("bundle: " + ", ".join(sorted(set(used))[:3]))
            counts["bundle mixed with layers"] += 1
        if why:
            rows.append((name, "; ".join(why)))

    print(f"# audit_three_way_split — {len(rows)} weapons break ONE-warhead/ONE-projectile/ONE-effect\n")
    for k, v in counts.most_common():
        print(f"  {v:5d}  {k}")
    print(f"\n  {len(bundles):5d}  legacy bundle templates in play")

    if rows:
        print("\n| weapon | problem |\n|---|---|")
        for name, why in sorted(rows)[:40]:
            print(f"| {name} | {why} |")
        if len(rows) > 40:
            print(f"\n_({len(rows) - 40} more)_")

    over = len(rows) > SPLIT_BASELINE
    print(f"\n{'FAIL' if over else 'WARN'} {len(rows)} violating weapons (ratchet {SPLIT_BASELINE})")
    if over:
        print("**A weapon just gained a second layer or a legacy bundle.** Split it instead of "
              "raising SPLIT_BASELINE.")
    else:
        print("Lower `SPLIT_BASELINE` as W24 converts weapons; never raise it.")
    return 1 if over else 0


if __name__ == "__main__":
    sys.exit(main())
