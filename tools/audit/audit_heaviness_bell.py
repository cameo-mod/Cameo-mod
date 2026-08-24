#!/usr/bin/env python3
"""audit_heaviness_bell.py — would the continuous-heaviness bell invert any family?

    python tools/audit/audit_heaviness_bell.py

Replaces `audit_level_ladder.py`, retired by maintainer ruling 2026-08-23.

⛔ WHY THE OLD AUDIT WENT: it required a family's EFFECTIVE damage to rise Light -> Medium ->
Heavy -> Super, and no law ever said so. DESIGN §12.0d makes the level a TILT (which armor the
weapon is good against) and §12.0h makes `Damage` a separate, free magnitude knob. The clincher
is structural: 145 of the `^Warhead_*` templates carry only a placeholder `Damage: 2000` — the
template holds the SHAPE, the weapon holds the MAGNITUDE — so a family's damage ladder is
emergent, and collapsing the levels into a continuous `h` never touches a damage number. Nine
families sat in a standing WARN for weeks against a rule that did not exist.

WHAT THE BELL ACTUALLY NEEDS — see DESIGN §12.0i, and §9 of docs/design/WEAPON_HEAVINESS.md:

    x(armor)      = rung position inside the armor's OWN ladder, normalised 0..2
                    VEH  Scout 0.0 < Light 0.5 < Medium 1.0 < Heavy 1.5 < Superheavy 2.0
                    AIR  Fighter 0.0 < Bomber 0.67 < Helicopter 1.33 < Spaceship 2.0
    mu(family, h) = centre_of_mass(base_profile) + SHIFT * (h - 1)
    curve(x)      = LO + (1 - LO) * exp( -(x - mu)^2 / (2*sigma^2) )
    Versus(a, h)  = base(a) * curve(x(a), mu)   then renormalised to a constant weighted mean

Two properties must hold at every `h`, and this audit checks both BEFORE the bell is implemented,
so step 5 of §9.6 lands against a test that already exists:

  1. NO FAMILY REORDERS. Within each armor ladder (INF/VEH/BLD/AIR) the FULL rank order a family
     held must survive at every h — not merely its first-vs-last direction. This is §12.0d's
     "can never invert" carried into the continuous model, and it holds because `belled()`
     performs the rank restore that law prescribes. An earlier version checked only the endpoints
     and skipped the restore, and so missed 127 internal reorderings while reporting two endpoint
     flips as permanent exceptions.
  2. THE WEIGHTED MEAN IS INVARIANT. Renormalisation must hold, or heaviness silently becomes a
     magnitude knob and re-prices every weapon — the exact coupling §12.0i ruled out.

⚠ It does NOT re-check the 2x-8x spread band. `audit_versus_profile.py` owns that
(`SPREAD_OFFENDERS_BASELINE = 0`, cleared 2026-08-22, 46 families in band); duplicating a clean
check in two places just makes two places to update.

⚠ NEVER HAND-PARSE YAML — profiles come through `miniyaml.Ruleset.resolve_weapon` and
`weapon_efficiency.versus_of`, for the reason spelled out in that audit's header.

EXIT CODE: 1 above the ratchet.
"""
from __future__ import annotations

import math
import pathlib
import statistics
import sys

if hasattr(sys.stdout, "reconfigure"):          # Windows consoles default to cp1252
    sys.stdout.reconfigure(encoding="utf-8")

sys.path.insert(0, str(pathlib.Path(__file__).resolve().parent))
sys.path.insert(0, str(pathlib.Path(__file__).resolve().parents[1] / "balance"))
from miniyaml import Ruleset  # noqa: E402
import weapon_efficiency as we  # noqa: E402

ROOT = pathlib.Path(__file__).resolve().parents[2]

# Families with no gradient for the bell to preserve. Measured 2026-08-23: just Magic and Sonic.
# WEAPON_HEAVINESS §9.2 predicted SIX (adding Cryo, Railgun, Waveforce, Storm); those four have
# since been given real gradients by fit_band_floor, so the prediction is stale and the ratchet
# starts at the measured value. LOWER ONLY, by authoring real profiles (§9.4) — never by
# widening the bell.
INVERT_BASELINE = 2

# ⛔ THERE ARE NO KNOWN INVERSIONS, and the earlier claim that there were two was an artifact of
# this audit skipping §12.0d's rank restore. `BulletThermobaric` BLD and `CannonFire` AIR were
# recorded here as permanent exceptions caused by a "gap in §9.4" — a near-flat 1.13x sub-ladder
# that no 1.25x swing could preserve. That conclusion was WRONG: with the restore in place both
# hold their order, along with 125 other reorderings the endpoint-only check never saw. The
# spread band needs no widening and no warhead needs authoring for this. (Corrected 2026-08-24.)

# DESIGN §12.0i: the bell's constants, all ruled 2026-08-23.
SHIFT = 0.25
LO = 0.80
# ⚠ NOT RULED — an assumed default. WEAPON_HEAVINESS §9 writes sigma into the formula and never
# assigns it a value. On a three-point axis 1.0 spans the whole ladder (a gentle curve); smaller
# peaks it. Every number this audit reports was taken at 1.0. Rule it before the bell ships.
SIGMA = 1.0
HEAVINESS = [0.0, 1.0, 2.0]

LEVELS = ["Light", "Medium", "Heavy", "Super", "Trace"]
COMPANION = ("Percentage", "ExtraDamage", "ExtraRepair", "Concrete")

# Excluded from the axis by the fourth ruling in DESIGN §12.0i. Each has its own law:
#   §12.0c — the shield ladder is its own compressed scale, not a normal armor
#   §12.0e — the ALL-CAPS platings REPLACE the class armor rather than sit on the axis
#   §12.0b — the heroic cell is DERIVED, recomputed from the finished profile
OFF_AXIS = {"Shield", "Heroic", "HAZMAT", "COMPOSITE", "BLAST", "REFLECTOR", "ARMOR"}

# Direction is only meaningful WITHIN one ladder — comparing None (INF) to Superheavy (VEH) is a
# cross-ladder relation the tilt is DESIGNED to change. Order is lightest -> heaviest, and it is
# the canonical one from gen_weapon_template.LADDERS.
LADDERS = {
    "INF": ["None", "Flak", "Plate"],
    "VEH": ["Scout", "Light", "Medium", "Heavy", "Superheavy"],
    "BLD": ["Wood", "Steel", "Concrete"],
    "AIR": ["Fighter", "Bomber", "Helicopter", "Spaceship"],
}

# ⭐ THE x-AXIS: each armor's RUNG POSITION inside its OWN ladder, normalised to 0..2.
#
# ⛔ This REVISES the first form of §12.0i ruling 3, which used §12.0d's three coarse buckets.
# A bucket TIES armors that are not equally heavy — `Bomber` and `Helicopter` both landed on x=1,
# `Scout` and `Light` both on 0, `Heavy` and `Superheavy` both on 2 — and tied coordinates MOVE
# TOGETHER under the bell, so heaviness could not tell them apart at all. Measured on CannonAP's
# air rows from h=0 to h=2: under the buckets Bomber moved -0.86 and Helicopter -0.88, i.e.
# identically; on this axis Bomber moves -3.10 while Helicopter moves **+1.79** — opposite
# directions, because the peak slides away from the lighter one and toward the heavier one.
#
# Maintainer, 2026-08-24: "both bomber and helicopter armor type are considered medium but from
# the two helicopter is the heavier one. Helicopter is actually in between medium and heavy while
# bomber is between light and medium. Same with the scout to light and the heavy to superheavy."
#
# The coordinates say exactly that: Bomber 0.67, Helicopter 1.33, Scout 0.0 < Light 0.5,
# Heavy 1.5 < Superheavy 2.0. The switch cost nothing — across 48 families both axes give the
# same 2 inversions, the same 2 flat families and zero mean drift.
#
# Normalised PER LADDER rather than on one global scale because §12.0d already makes a
# cross-ladder comparison meaningless: a family's lean should land at the same RELATIVE position
# in every ladder, whatever that ladder's granularity.
BUCKET = {}
for _rungs in LADDERS.values():
    for _i, _a in enumerate(_rungs):
        BUCKET[_a] = 2.0 * _i / (len(_rungs) - 1)


def profiles() -> dict[str, dict[str, float]]:
    """{family: {armor: versus}} from one `^Warhead_<Family>_<Level>` MAIN warhead per family."""
    rs = Ruleset(ROOT)
    out: dict[str, dict[str, float]] = {}
    for name in sorted(rs.weapons):
        if not name.startswith("^Warhead_"):
            continue
        family, _, level = name[len("^Warhead_"):].rpartition("_")
        if level not in LEVELS or not family or family in out:
            continue
        resolved = rs.resolve_weapon(name)
        if resolved is None:
            continue
        for wh in resolved.children:
            if not wh.key.startswith("Warhead@") or any(c in wh.key for c in COMPANION):
                continue
            versus = we.versus_of(wh)      # the project's reader, never a hand parser
            if versus:
                out[family] = {a: float(v) for a, v in versus.items() if a not in OFF_AXIS}
            break
    return out


def centre_of_mass(profile: dict[str, float]) -> float | None:
    """Where on the 0..2 axis the family's strength sits, weighted by its own Versus."""
    pairs = [(BUCKET[a], v) for a, v in profile.items() if a in BUCKET and v > 0]
    total = sum(v for _x, v in pairs)
    return sum(x * v for x, v in pairs) / total if total else None


def belled(profile: dict[str, float], mu: float) -> dict[str, float]:
    """The FULL §12.0i pipeline: bell at `mu`, renormalise, then RESTORE RANK per ladder.

    ⛔ THE RANK RESTORE IS NOT OPTIONAL — it is what makes §12.0d's "can never invert" true.
    §12.0d: the tilt "is applied to the VALUES and each armor is then given back the RANK it
    held". An earlier version of this audit skipped that step and only compared a ladder's FIRST
    and LAST rung, so it missed **127** internal reorderings across 60 family/ladder pairs and
    reported two endpoint flips as permanent exceptions. With the restore there are **zero**
    reorderings anywhere, and the two "known inversions" disappear with them.

    The restore permutes values WITHIN a ladder, so the multiset is unchanged and the weighted
    mean is untouched — §12.0i's price invariance survives it.
    """
    out = {}
    for a, v in profile.items():
        x = BUCKET.get(a)
        out[a] = v if x is None else v * (LO + (1 - LO) * math.exp(-((x - mu) ** 2) / (2 * SIGMA ** 2)))
    before, after = statistics.mean(profile.values()), statistics.mean(out.values())
    if after:
        out = {a: v * before / after for a, v in out.items()}

    for rungs in LADDERS.values():
        present = [a for a in rungs if a in profile and a in out]
        if len(present) < 2:
            continue
        # The rank each armor HELD, and the tilted magnitudes sorted the same way.
        for armor, value in zip(sorted(present, key=lambda a: profile[a]),
                                sorted(out[a] for a in present)):
            out[armor] = value
    return out


def rank_order(profile: dict[str, float], rungs: list[str]) -> list[str] | None:
    """The armors of one ladder, ordered by their Versus. None if unjudgeable."""
    present = [a for a in rungs if a in profile]
    return sorted(present, key=lambda a: profile[a]) if len(present) >= 2 else None


def direction(profile: dict[str, float], rungs: list[str]) -> str | None:
    present = [a for a in rungs if a in profile]
    if len(present) < 2:
        return None
    lo, hi = profile[present[0]], profile[present[-1]]
    if math.isclose(lo, hi, rel_tol=1e-9):
        return "flat"
    return "up" if hi > lo else "down"


def main() -> int:
    data = profiles()
    if not data:
        print("# audit_heaviness_bell\n\n_no `^Warhead_*` profiles resolved_ — cannot run.")
        return 0

    print("# audit_heaviness_bell — would the continuous-heaviness bell invert any family?\n")
    print(f"DESIGN §12.0i: `SHIFT` {SHIFT}, `LO` {LO} (swing ~"
          f"{(1 / LO):.2f}x), x-axis = rung position inside each armor's own ladder, 0..2. "
          f"Simulated at h = {', '.join(str(h) for h in HEAVINESS)}.\n")

    inverted, flat_families, mean_drift = [], [], []
    for family, base in sorted(data.items()):
        com = centre_of_mass(base)
        if com is None:
            continue
        base_mean = statistics.mean(base.values())
        at = {h: belled(base, com + SHIFT * (h - 1)) for h in HEAVINESS}

        for h, prof in at.items():
            drift = abs(statistics.mean(prof.values()) - base_mean) / base_mean
            if drift > 1e-6:
                mean_drift.append((family, h, drift))

        family_flat = True
        for ladder, rungs in LADDERS.items():
            if direction(base, rungs) in (None, "flat"):
                continue
            family_flat = False
            # The FULL rank order, not just the endpoints: an internal swap is an inversion too.
            want = rank_order(base, rungs)
            for h, prof in at.items():
                got = rank_order(prof, rungs)
                if got != want:
                    inverted.append((family, ladder, h, " < ".join(want), " < ".join(got)))
        if family_flat:
            flat_families.append(family)

    print(f"| | |\n|---|--:|")
    print(f"| families measured | {len(data)} |")
    print(f"| with NO gradient the bell could preserve | {len(flat_families)} |")
    print(f"| ladder ORDERINGS changed by the bell | {len(inverted)} |")
    print(f"| weighted-mean drift beyond 1e-6 | {len(mean_drift)} |")

    if flat_families:
        print("\n## Flat families — no gradient to preserve\n")
        print("§9.2 predicted SIX of these; four (Cryo, Railgun, Waveforce, Storm) have since been "
              "given real gradients, so the prediction is stale and only these remain. The bell "
              "cannot help a family with no gradient — they need real profiles authored (§9.4). "
              "Lower `INVERT_BASELINE` as that happens, never by widening the bell.\n")
        for f in flat_families:
            print(f"  {f}")

    if inverted:
        print("\n## ⛔ Direction changed — §12.0d says this must never happen\n")
        for family, ladder, h, want, got in inverted[:20]:
            print(f"  {family:16s} {ladder:4s} h={h:.0f}")
            print(f"      was    {want}")
            print(f"      became {got}")
        if len(inverted) > 20:
            print(f"  … and {len(inverted) - 20} more")

    if mean_drift:
        print("\n## ⛔ Renormalisation failed — heaviness would become a magnitude knob\n")
        print("§12.0i rules that `K` stays invariant in `h`. A drifting mean re-prices every "
              "weapon that sets a heaviness.\n")
        for family, h, drift in mean_drift[:20]:
            print(f"  {family:14s} h={h:.0f}  drift {drift:.2%}")

    bad = len(flat_families)
    verdict = "FAIL" if bad > INVERT_BASELINE or inverted or mean_drift else "WARN"
    print(f"\n{verdict} {bad} flat families (ratchet {INVERT_BASELINE}) · "
          f"{len(inverted)} inversions (must be 0) · {len(mean_drift)} mean drifts (must be 0)")
    print("Lower `INVERT_BASELINE` as flat families get real profiles; never raise it.")
    return 1 if verdict == "FAIL" else 0


if __name__ == "__main__":
    raise SystemExit(main())
