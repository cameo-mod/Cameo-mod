#!/usr/bin/env python3
"""audit_engine_constraints.py — the engine-limit constraint reporter.

    python tools/audit/audit_engine_constraints.py

Maintainer rulings 2026-08-29. Three limits, each with the reason it exists and
an explicit exemption policy, because a bare threshold produces mostly false
positives on a roster this heterogeneous:

  E1  GROUND SPEED >= 30.  Pathfinding safety. NOT 50 — 50 is the class-anchor
      minimum (`docs/balance/class_anchors.json` speed0), and treating it as a
      hard floor illegally flags the super-heavy class: Sturmtiger 30, Ratte 35,
      Devastator 31, Yamato 35 are heavy BY DESIGN, and the 44-49 infantry band
      is acceptable. Aircraft are exempt (no ground pathfinder), as are actors
      deliberately stationary at speed 0.

  E2  RELOAD >= 10 for ordinary direct-fire weapons.  The reason is CPU: a
      hundred standard bullets firing every 3 frames is tick exhaustion, not a
      balance problem. Structural families are exempt BY MECHANISM — a
      continuous beam's ReloadDelay IS its damage tick, a Gatling ladder's
      6/4/2 is the spin-up, a charge weapon's cycle lives on AttackTesla. The
      exempt list is the registry's, not this file's.

  E3  SNIPERS USE InstantHitWithFakeBullets.  The Shattered Paradise port
      (`OpenRA.Mods.Cameo/Projectiles/InstantHitWithFakeBullets.cs`) is the
      target for every sniper; a `Bullet` sniper at Speed 2500-10000 is a
      deprecated pattern awaiting migration.

⚠ EXEMPTIONS LIVE IN `docs/design/balance_exceptions.yaml`, not here. A limit
whose exceptions are hardcoded in its own checker cannot be reviewed without
reading the checker. Adding a member to an exempt family is a registry edit.

⚠ Measure on LIVE weapons only — those referenced by a buildable actor's
`Armament`. Counting dead templates turned 121 real findings into 189 and made
the reload limit look like a bulk cleanup instead of a short list.

EXIT CODE: 1 on any violation outside the registry's exemptions.
"""
from __future__ import annotations

import collections
import pathlib
import sys

sys.path.insert(0, str(pathlib.Path(__file__).resolve().parent))

from cameo_model import Model  # noqa: E402

# The suffix grammar is DESIGN.md section 1's, and audit_weapon_suffixes.py already
# owns it. Import rather than restate: a second copy of the variant list is a
# second thing to forget when the grammar changes.
from audit_weapon_suffixes import EMP_SUBVARIANT_SUFFIXES  # noqa: E402

# Variant markers that are NOT underscore-prefixed, so the shared tuple misses
# them. They mark a re-skin of the same mechanism (a Waveforce or Resonance
# retune of one gun), which is what matters for a per-mechanism exemption.
EXTRA_VARIANT_SUFFIXES = ("Waveforce", "Resonance", "Cryo", "AA", "_EMP")

if hasattr(sys.stdout, "reconfigure"):          # Windows consoles default to cp1252
    sys.stdout.reconfigure(encoding="utf-8")

REGISTRY = pathlib.Path("docs/design/balance_exceptions.yaml")
INSTANT_HIT = "InstantHitWithFakeBullets"


def registry():
    """The maintainer's limits and exemptions. The registry is the authority."""
    try:
        import yaml
    except ImportError:                          # stdlib-only fallback
        return None
    return yaml.safe_load(REGISTRY.read_text(encoding="utf-8"))["limits"]


def live_weapons(rs):
    """Weapon names referenced by a buildable actor's Armament, lowercased."""
    out = set()
    for name in rs.actors:
        if name.startswith(("^", "_")):
            continue
        node = rs.resolve(name)
        if node is None or node.child("Valued") is None or node.child("Buildable") is None:
            continue
        for c in node.children:
            if c.key.split("@")[0] == "Armament":
                w = c.get("Weapon")
                if w:
                    out.add(w.strip().lower())
    return out


def max_damage(weapon):
    """Largest absolute Damage on any warhead. Zero means a utility weapon."""
    best = 0
    for c in weapon.children:
        if c.key.split("@")[0] != "Warhead":
            continue
        d = c.get("Damage")
        if d:
            try:
                best = max(best, abs(int(str(d).strip())))
            except ValueError:
                pass
    return best


def e1_ground_speed(rs, floor):
    """Buildable GROUND movers below the pathfinding floor."""
    findings = []
    for name in sorted(rs.actors):
        if name.startswith(("^", "_")):
            continue
        node = rs.resolve(name)
        if node is None or node.child("Valued") is None or node.child("Buildable") is None:
            continue
        for c in node.children:
            if c.key.split("@")[0] != "Mobile":       # Aircraft: no ground pathfinder
                continue
            sp = c.get("Speed")
            if sp is None:
                continue
            try:
                sp = int(str(sp).strip())
            except ValueError:
                continue
            if sp == 0:
                findings.append((name, sp, "stationary — exempt if deliberate"))
            elif sp < floor:
                findings.append((name, sp, ""))
    return findings


def e2_reload(rs, floor, exempt, live):
    """Live damage-dealing weapons below the reload floor, minus exempt families."""
    findings = []
    for wname in sorted(rs.weapons):
        if wname.startswith("^") or wname.lower() not in live:
            continue
        w = rs.resolve_weapon(wname)
        if w is None:
            continue
        rd = w.get("ReloadDelay")
        try:
            rd = int(str(rd).strip())
        except (TypeError, ValueError):
            continue
        if rd >= floor:
            continue
        if exempt.get(wname.lower()) or exempt.get(variant_stem(wname)):
            continue
        if max_damage(w) == 0:                    # utility weapon: no DPS to floor
            continue
        proj = w.child("Projectile")
        findings.append((wname, rd, (proj.value or "?").strip() if proj else "-"))
    return findings


def e3_snipers(rs, live):
    """Sniper weapons not yet on InstantHitWithFakeBullets."""
    ok, deprecated = [], []
    for wname in sorted(rs.weapons):
        if wname.startswith("^") or wname.lower() not in live:
            continue
        if "snip" not in wname.lower():
            continue
        w = rs.resolve_weapon(wname)
        proj = w.child("Projectile") if w else None
        kind = (proj.value or "?").strip() if proj else "NONE"
        (ok if kind == INSTANT_HIT else deprecated).append(
            (wname, kind, proj.get("Speed") if proj else None))
    return ok, deprecated


def variant_stem(name):
    """Strip trailing variant markers so `RA2GattlingMG3_AA` reduces to its base.

    ⚠ A weapon's exemption is a statement about its MECHANISM, and a variant
    shares the mechanism of its base. `RA2GattlingMG3` is exempt as part of the
    gatling spin-up ladder; `RA2GattlingMG3_AA` is the same ladder pointed at
    aircraft. Listing every variant by hand in the registry means the exemption
    silently stops covering a family the moment someone adds a `_Waveforce`.
    """
    stem = name
    changed = True
    while changed:
        changed = False
        for suf in EMP_SUBVARIANT_SUFFIXES + EXTRA_VARIANT_SUFFIXES:
            if len(stem) > len(suf) and stem.lower().endswith(suf.lower()):
                stem = stem[: -len(suf)]
                changed = True
    return stem.rstrip("_").lower()


def split_families(reg):
    """{weapon_stem: family} from the registry's exempt_families.

    Keyed by variant stem so one registry entry covers its whole family.
    """
    out = {}
    for fam in reg["reload_delay"].get("exempt_families", []):
        for m in fam.get("members", []):
            out[m.lower()] = fam["name"]
            out[variant_stem(m)] = fam["name"]
    return out


def main():
    reg = registry()
    if reg is None:
        print("audit_engine_constraints: PyYAML not available; cannot read "
              f"{REGISTRY}. The exemption lists live there and are not "
              "duplicated here, so the audit cannot run without it.",
              file=sys.stderr)
        return 2

    rs = Model().rs
    live = live_weapons(rs)
    speed_floor = reg["movement_speed"]["floor"]
    reload_floor = reg["reload_delay"]["floor"]
    exempt = split_families(reg)

    print("# Engine constraint report\n")
    print(f"Limits from `{REGISTRY}` (maintainer 2026-08-29): ground speed >= "
          f"{speed_floor}, reload >= {reload_floor} for ordinary direct-fire "
          f"weapons, snipers on `{INSTANT_HIT}`.\n")

    failures = 0

    # ------------------------------------------------------------------ E1 ---
    e1 = e1_ground_speed(rs, speed_floor)
    real = [f for f in e1 if not f[2]]
    stationary = [f for f in e1 if f[2]]
    print(f"\n## E1 — ground movers below Speed {speed_floor} "
          f"({len(real)} + {len(stationary)} stationary)\n")
    print("Aircraft are excluded: they do not use the ground pathfinder. "
          "Speed 0 is listed separately — a deliberately stationary actor is "
          "exempt, but a unit that reached 0 by accident is not.\n")
    if real:
        failures += len(real)
        for name, sp, _ in sorted(real, key=lambda x: x[1]):
            print(f"  {name:46} Speed {sp}")
    else:
        print("_clean_")
    if stationary:
        print("\nStationary (Speed 0) — classify each as deliberate or not:\n")
        for name, sp, _ in stationary:
            print(f"  {name:46} Speed {sp}")

    # ------------------------------------------------------------------ E2 ---
    e2 = e2_reload(rs, reload_floor, exempt, live)
    print(f"\n\n## E2 — live weapons below ReloadDelay {reload_floor} ({len(e2)})\n")
    print(f"{len(exempt)} weapons in {len(reg['reload_delay']['exempt_families'])} "
          "structural families are exempt by mechanism, and zero-damage utility "
          "weapons carry no DPS to floor. What remains is the ordinary "
          "direct-fire population.\n")
    print("⚠ These are NOT free to raise. Reload is half of DPS, so the fix is a "
          "PAIRED change through the pipeline — reload x2 with damage x2 keeps "
          "DPS and halves the tick load. Raising reload alone is a straight nerf.\n")
    if e2:
        failures += len(e2)
        print(f"{'weapon':34}{'reload':>7}  projectile")
        for wname, rd, proj in sorted(e2, key=lambda x: (x[1], x[0])):
            print(f"  {wname:32}{rd:7}  {proj}")
    else:
        print("_clean_")

    # ------------------------------------------------------------------ E3 ---
    ok, deprecated = e3_snipers(rs, live)
    print(f"\n\n## E3 — snipers not on {INSTANT_HIT} "
          f"({len(deprecated)} of {len(ok) + len(deprecated)})\n")
    if deprecated:
        failures += len(deprecated)
        print(f"{'weapon':38}{'projectile':30}speed")
        for wname, kind, sp in sorted(deprecated):
            print(f"  {wname:36}{kind:30}{sp}")
        # A family split across two projectile models is the sharper finding.
        by_stem = collections.defaultdict(set)
        for wname, kind, _sp in ok + deprecated:
            by_stem[wname.lower().replace("_elite", "")].add(kind)
        split = {s: k for s, k in by_stem.items() if len(k) > 1}
        if split:
            print("\n⚠ Families split across two projectile models — the base and "
                  "elite variants of one weapon behave differently:\n")
            for stem, kinds in sorted(split.items()):
                print(f"  {stem:36} {sorted(kinds)}")
    else:
        print("_clean_")

    if failures:
        print(f"\n\n**FAIL** — {failures} constraint violations.")
        return 1
    print("\n\n**PASS**")
    return 0


if __name__ == "__main__":
    sys.exit(main())
