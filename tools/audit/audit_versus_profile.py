#!/usr/bin/env python3
"""audit_versus_profile.py — guard DESIGN §12.0h / §12.0c / §12.0d on the LIVE profiles.

    python tools/audit/audit_versus_profile.py

Three binding maintainer rulings had NO guard at all — only `gen_weapon_template.py` implemented
them, so nothing checked that what the generator intends is what the tree actually carries:

  §12.0h  THE MEAN-100 LAW (2026-08-16) — every family's MAIN warhead has its 16 armor rows
          normalised to arithmetic MEAN 100. This is what makes `K` SHAPE-ONLY and `Damage` the
          sole magnitude knob, so a drifted mean is a HIDDEN price multiplier.
  §12.0d  THE CLASS TILT — each LEVEL tilts toward one end of every armor ladder, and
          *"the tilt MUST NEVER reorder a ladder ... it can never invert"*.
          ⚠ The guarantee is WITHIN a ladder. `None` is INF and `Superheavy` is VEH, so
          comparing them is a CROSS-ladder relation the tilt is DESIGNED to change — a Light
          tilt deliberately raises infantry relative to superheavy vehicles. A first version
          of this audit compared None vs Superheavy and reported 4 families "inverting"; that
          was a false positive. The real invariant is direction WITHIN each ladder.
  spread  Target band 2x-8x (aim 4x) between a profile's highest and lowest armor row.

⛔ WHY THIS EXISTS — AND WHY IT READS THROUGH THE RESOLVER, NEVER A HAND PARSER.

On 2026-08-22 I measured these laws with a bespoke yaml parser and got every number wrong. The
parser never CLOSED the `Versus:` block, so the `PercentageVersus:` rows that the AreaDamage fold
added inside the SAME warhead node silently overwrote the profile:

    Warhead@Bullet_Light: AreaDamage
        Versus:            None: 200 ... Superheavy 48   <- the real profile
        PercentageVersus:  None: 16  ... Superheavy  1   <- what the parser read

I reported "0 of 125 conform" and "every family violates the spread band". The truth was 123 of
125 and 37 of 42. Every downstream figure — means, spreads, ratios, inversion counts — was
internally consistent and wrong. This audit therefore uses `miniyaml.Ruleset.resolve_weapon` and
`weapon_efficiency.versus_of`, the project's own readers, which cannot make that mistake.

EXIT CODE: 1 above any ratchet.
"""
from __future__ import annotations

import pathlib
import statistics
import sys

if hasattr(sys.stdout, "reconfigure"):          # Windows consoles default to cp1252
    sys.stdout.reconfigure(encoding="utf-8")

ROOT = pathlib.Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "tools" / "audit"))
sys.path.insert(0, str(ROOT / "tools" / "balance"))
from miniyaml import Ruleset            # noqa: E402
import weapon_efficiency as we          # noqa: E402

# Measured 2026-08-22 through the resolver. LOWER ONLY.
MEAN_OFFENDERS_BASELINE = 2      # Nuclear_Super + Sniper_Light, both HAND_TUNED
SPREAD_OFFENDERS_BASELINE = 0    # CLEARED 2026-08-22 by fit_band_floor in gen_weapon_template
                                 # (Nuclear and Sniper excluded: their only level is HAND_TUNED)
FLIP_BASELINE = 0                # CLEARED 2026-08-22 — the blend tiebreak is now family-wide

# The generator skips these entirely, so they are not expected to obey the generated laws.
HAND_TUNED = {("Nuclear", "Super"), ("Sniper", "Light")}
# Flat BY DESIGN — `mean_normalise` special-cases them ("ignores armor").
FLAT_BY_DESIGN = {"Sonic", "Magic"}

NON_ARMOR = {"Shield", "HAZMAT", "COMPOSITE", "BLAST", "REFLECTOR", "ARMOR"}

# ⭐ MAINTAINER RULING 2026-08-30 — `Heroic` IS CALCULATED BUT NOT MEASURED.
#
#   *"Since Heroic armor is only for hero units with build limits it should not be included in
#    the 4x measurements. Only unlimited units should be counted. Heroic should only be
#    calculated but not be part of the spread analysis."*
#
# It stays in `armor_rows`, so §12.0h's MEAN-100 still averages it and pricing still accounts for
# the damage a weapon deals to heroes. It is removed from the two SPREAD metrics only.
#
# THE PREMISE WAS VERIFIED, NOT ASSUMED. Resolved over the live tree: **32 actors wear `Heroic`
# — 30 buildable with `BuildLimit: 1`, 2 non-buildable campaign variants of a hero that is
# itself limited, and ZERO buildable-unlimited units.** So the row describes an armor no unit
# in the balance population wears; `reference_distribution.py` already drops every one of its
# wearers (`if rec.get("build_limit") is not None: continue`).
#
# THREE INDEPENDENT REASONS CONVERGE, which is what makes this a ruling rather than a tweak:
#   1. POPULATION — its wearers are build-limited heroes, balanced separately by explicit order.
#   2. DERIVED — §12.0b makes it a PRODUCT (`Plate x Scout / PEAK`) recomputed from the finished
#      profile, and §12.0d ALREADY excludes it from the class tilt for exactly that reason.
#      Including a formula's output in a law about authored design was the inconsistency.
#   3. MEASUREMENT — it was the MINIMUM row of **21%** of profiles, so it set the spread
#      denominator for a fifth of the corpus. Worse, under `macro_spread` it draws one input
#      from INF and one from VEH, so on an anti-air family BOTH are disfavoured and it falls as
#      roughly the SQUARE of the ratio — `MissileAA` hit 8.70x on `Heroic 23` alone. A derived
#      cell was setting the ceiling for a knob it does not describe.
DERIVED_ROWS = {"Heroic"}
# The armor LADDERS, from gen_weapon_template.LADDERS. Direction is only meaningful WITHIN one.
LADDERS = {
    "INF": ["None", "Flak", "Plate", "Heroic"],
    "VEH": ["Scout", "Light", "Medium", "Heavy", "Superheavy"],
    "BLD": ["Wood", "Steel", "Concrete"],
    "AIR": ["Fighter", "Bomber", "Helicopter", "Spaceship"],
}
DERIVED_ARMORS = ("Heroic", "Airborne")
LEVELS = ("Light", "Medium", "Heavy", "Super")
COMPANION = ("Percentage", "ExtraDamage", "ExtraRepair", "Concrete",
             "Effect", "ShieldHit", "Glow", "Smudge")
MEAN_LO, MEAN_HI = 95.0, 105.0
SPREAD_LO, SPREAD_HI = 2.0, 8.0

# ── MACRO CONTRAST: the SECOND spread metric, and it is not the same number ──────────────────
# §9.4's `spread` is max/min over a profile's 16 armor ROWS, and Cameo passes it: measured
# 2026-08-30 over 6,093 live profiles the median is **4.00x**, exactly the documented target, with
# 80% inside [2,8]. That is the law being obeyed.
#
# MACRO CONTRAST is max/min over the three ladder MEANS (INF, VEH, BLD) — how far a weapon's
# PREFERRED unit type separates from the types it is not for. Averaging four or five rows into a
# ladder mean necessarily compresses, so this number is always the smaller of the two; the
# question is by how much, and against whom.
#
# ⛔ CORRECTED 2026-08-30. An earlier version of this comment carried an UNATTRIBUTED peer table
# (RV 3.00x, OpenRA RA 2.67x, CA 2.35x) and concluded from it that "Cameo is not short of gradient,
# it spends it WITHIN ladders instead of BETWEEN them". Both halves were unsound:
#
#   1. THE NUMBERS WERE NOT REPRODUCIBLE and two were wrong. Re-measured from the committed
#      `docs/reference/versus_raw.json` (`--peers`): RV is **2.00x**, not 3.00x, and CA is
#      **2.93x**, not 2.35x. Only OpenRA RA (2.56x vs 2.67x) was close.
#   2. THEY WERE NOT MEASURED ON THE SAME FRAME. A ladder MEAN over 4-5 rows compresses toward the
#      profile mean; a mean over ONE row does not. OpenRA RA has five armor classes total, so its
#      "INF mean" IS its `none` row, while Cameo averages None+Flak+Plate+Heroic. Measured on the
#      IDENTICAL 139 Cameo templates, the frame alone moves the answer **1.63x -> 1.91x (+17%)**.
#      Roughly a third of the published gap was the estimator, not the design.
#
# So `--peers` now measures every peer on ITS OWN frame and Cameo on that SAME frame. Like-for-like
# (median over each corpus, `python tools/audit/audit_versus_profile.py --peers`):
#
#     peer (own frame)        n     peer    Cameo on that frame   ratio
#     Mental Omega          367    4.15x           1.67x            2.48x  <- the ONLY corpus at "4x"
#     Combined Arms         196    2.93x           1.90x            1.54x
#     OpenRA Red Alert       45    2.56x           1.90x            1.35x
#     Romanov's Vengeance    75    2.00x           1.63x            1.23x
#     RA2 vanilla            60    1.73x           1.67x            1.03x  <- the original game is
#                                                                             AT Cameo's level
#
# WHAT SURVIVES: a real but smaller and far less uniform gap. Cameo sits below three of the five
# and level with RA2 vanilla. "Target 4x" describes Mental Omega alone, not the field.
# WHAT DOES NOT: "Cameo spends its gradient in the wrong place" — the macro SHARE of Cameo's total
# spread (~60-73%) is inside the peer range (64-90%), so the allocation was never the anomaly.
#
# The knob that was genuinely MISSING is `gen_weapon_template.macro_spread` — a third profile axis
# alongside the within-ladder shaper and the macro PRIORITY order. It ships inert (MACRO_RATIO 1.0);
# `gen_weapon_template.py --macro=<r>` sweeps it.
#
# ⚠ REPORTING ONE OF THESE AS "THE ARMOR TILT" IS HOW THIS GOT MISREAD. Quoting 1.8x invites
# "the armor system is flat" when §9.4 is being met exactly; quoting 4.0x hides that macro
# specialisation is materially below every peer. Both are printed, always, side by side.
# ⚠ `Heroic` is NOT here, by the same 2026-08-30 ruling: it was inside the INF mean, where the
# macro axis cannot move it as a rung, damping the very metric the axis is judged on (measured
# 1.84x with it against 1.99x without, at ratio 1.50).
MACRO_LADDERS = {"INF": ("None", "Flak", "Plate"),
                 "VEH": ("Scout", "Light", "Medium", "Heavy", "Superheavy"),
                 "BLD": ("Wood", "Concrete", "Steel")}
MACRO_TARGET_LO, MACRO_TARGET_HI = 2.0, 8.0


# Peer armor tag -> ladder, paired with the Cameo rows that make the SAME frame. Terrain props
# (`tree`, `truk`, `brick`) and `rocket` (a projectile-interception class) are not unit armors and
# are excluded. `drone` is a light mechanical walker, which Cameo files under `Scout`.
PEER_FRAMES = {
    "Combined Arms": ("combined_arms",
                      {"INF": ["none"], "VEH": ["light", "heavy"], "BLD": ["wood", "concrete"]},
                      {"INF": ["None"], "VEH": ["Light", "Heavy"], "BLD": ["Wood", "Concrete"]}),
    "OpenRA Red Alert": ("openra_ra",
                         {"INF": ["none"], "VEH": ["light", "heavy"], "BLD": ["wood", "concrete"]},
                         {"INF": ["None"], "VEH": ["Light", "Heavy"], "BLD": ["Wood", "Concrete"]}),
    "Romanov's Vengeance": ("romanovs_vengeance",
                            {"INF": ["none", "flak", "plate"],
                             "VEH": ["light", "medium", "heavy", "drone"],
                             "BLD": ["wood", "steel", "concrete"]},
                            {"INF": ["None", "Flak", "Plate"],
                             "VEH": ["Light", "Medium", "Heavy", "Scout"],
                             "BLD": ["Wood", "Steel", "Concrete"]}),
    "RA2 vanilla": ("ra2_vanilla",
                    {"INF": ["none", "flak", "plate"], "VEH": ["light", "medium", "heavy"],
                     "BLD": ["wood", "steel", "concrete"]},
                    {"INF": ["None", "Flak", "Plate"], "VEH": ["Light", "Medium", "Heavy"],
                     "BLD": ["Wood", "Steel", "Concrete"]}),
    "Mental Omega": ("mental_omega",
                     {"INF": ["none", "flak", "plate"], "VEH": ["light", "medium", "heavy"],
                      "BLD": ["wood", "steel", "concrete"]},
                     {"INF": ["None", "Flak", "Plate"], "VEH": ["Light", "Medium", "Heavy"],
                      "BLD": ["Wood", "Steel", "Concrete"]}),
}
VERSUS_RAW = ROOT / "docs" / "reference" / "versus_raw.json"


def contrast_on(profile, frame):
    """max/min over ladder means for an ARBITRARY frame. None if a ladder has no live row."""
    means = []
    for rungs in frame.values():
        vals = [profile[a] for a in rungs if profile.get(a, 0) > 0]
        if not vals:
            return None
        means.append(statistics.fmean(vals))
    return max(means) / min(means) if min(means) > 0 else None


def peer_table():
    """[(label, n, peer median, Cameo median ON THE SAME FRAME)] — the like-for-like."""
    import json
    raw = json.loads(VERSUS_RAW.read_text(encoding="utf-8"))["sources"]
    cameo = list(profiles().values())
    out = []
    for label, (key, ptags, ctags) in PEER_FRAMES.items():
        if key not in raw:
            continue
        pv = [x for x in (contrast_on({k.lower(): float(v) for k, v in r["versus"].items()}, ptags)
                          for r in raw[key]["rows"]) if x]
        cv = [x for x in (contrast_on(p, ctags) for p in cameo) if x]
        if len(pv) < 5 or not cv:
            continue
        out.append((label, len(pv), statistics.median(pv), statistics.median(cv)))
    return sorted(out, key=lambda r: -r[2])


def macro_contrast(profile):
    """max/min over the INF, VEH and BLD ladder means. None when a ladder is missing."""
    means = []
    for rows in MACRO_LADDERS.values():
        vals = [profile[r] for r in rows if profile.get(r, 0) > 0]
        if vals:
            means.append(sum(vals) / len(vals))
    if len(means) < 3 or min(means) <= 0:
        return None
    return max(means) / min(means)


def profiles() -> dict[tuple[str, str], dict[str, float]]:
    """{(family, level): {armor: versus}} for every `^Warhead_<Family>_<Level>` MAIN warhead."""
    rs = Ruleset(ROOT)
    out: dict[tuple[str, str], dict[str, float]] = {}
    for name in rs.weapons:
        if not name.startswith("^Warhead_"):
            continue
        family, _, level = name[len("^Warhead_"):].rpartition("_")
        if level not in LEVELS or not family:
            continue
        resolved = rs.resolve_weapon(name)
        if resolved is None:
            continue
        for wh in resolved.children:
            if not wh.key.startswith("Warhead@") or any(c in wh.key for c in COMPANION):
                continue
            versus = we.versus_of(wh)      # ⭐ the project's reader, not a hand parser
            if versus:
                out[(family, level)] = versus
            break
    return out


def armor_rows(profile):
    return {k: v for k, v in profile.items() if k not in NON_ARMOR}


def ladder_direction(profile, rungs):
    """'up' if the profile rises along this ladder, 'down' if it falls, None if unjudgeable.

    Derived armors are excluded — `Heroic` is a PRODUCT of two other cells (§12.0b) and is
    recomputed from the finished profile, so it is not an independent rung.
    """
    present = [a for a in rungs if a in profile and a not in DERIVED_ARMORS]
    if len(present) < 2:
        return None
    return "up" if profile[present[-1]] > profile[present[0]] else "down"


def main() -> int:
    if "--peers" in sys.argv:
        print("# macro contrast, each peer on ITS OWN armor frame, Cameo on that SAME frame\n")
        print(f"{'corpus':24s} {'n':>5s} {'peer':>8s} {'Cameo':>8s}  ratio")
        for label, n, pm, cm in peer_table():
            print(f"{label:24s} {n:5d} {pm:7.2f}x {cm:7.2f}x  {pm / cm:.2f}x")
        print("\n⚠ A ladder mean over ONE row does not compress; over five it does. Never compare "
              "two corpora on different frames — the frame alone is worth ~17%.")
        return 0

    data = profiles()
    families = sorted({f for f, _l in data})

    import statistics as _st
    mean_bad, spread_bad, flips, all_spreads = [], [], [], []

    for key, prof in sorted(data.items()):
        rows = armor_rows(prof)
        if not rows:
            continue
        mean = statistics.mean(rows.values())
        if not (MEAN_LO <= mean <= MEAN_HI):
            mean_bad.append((key, mean, key in HAND_TUNED))

    for family in families:
        level = next((l for l in LEVELS if (family, l) in data), None)
        if level is None or family in FLAT_BY_DESIGN:
            continue
        # HAND_TUNED profiles are authored by hand and never generated, so the
        # generated laws do not apply to them.
        if (family, level) in HAND_TUNED:
            continue
        rows = [v for k, v in armor_rows(data[(family, level)]).items()
                if v > 0 and k not in DERIVED_ROWS]      # ruling above: calculated, not measured
        if not rows:
            continue
        spread = max(rows) / min(rows)
        all_spreads.append(spread)
        if not (SPREAD_LO <= spread <= SPREAD_HI):
            spread_bad.append((family, spread))

        levels = [l for l in LEVELS if (family, l) in data]
        for ladder_name, rungs in LADDERS.items():
            dirs = {d for d in (ladder_direction(data[(family, l)], rungs) for l in levels) if d}
            if len(dirs) > 1:
                flips.append((family, ladder_name, sorted(dirs)))

    print(f"# audit_versus_profile — {len(data)} MAIN profiles across {len(families)} families\n")

    print(f"## §12.0h MEAN-100 — {len(data) - len(mean_bad)} of {len(data)} conform\n")
    for key, mean, hand in mean_bad:
        tag = " _(HAND_TUNED — generator skips it, expected)_" if hand else " **UNEXPECTED**"
        print(f"  {key[0]}_{key[1]}  mean {mean:.1f}{tag}")

    _mc = [m for m in (macro_contrast(p) for p in data.values()) if m]
    if _mc:
        _in = sum(1 for m in _mc if MACRO_TARGET_LO <= m <= MACRO_TARGET_HI)
        print(f"\n## macro contrast (INF/VEH/BLD ladder means) — median "
              f"{_st.median(_mc):.2f}x over {len(_mc)} profiles, {_in / len(_mc) * 100:.0f}% "
              f"in [{MACRO_TARGET_LO:.0f}x, {MACRO_TARGET_HI:.0f}x]\n")
        print("⛔ This is NOT §9.4's row spread and must never be quoted as it — they are "
              "printed together, below, so neither can stand in for the other.\n"
              "Peer comparison: `--peers`, which measures each corpus on ITS OWN armor frame. "
              "Like-for-like, ONLY Mental Omega (4.15x) is at '4x'; the field median is 2.56x "
              "and RA2 vanilla (1.73x) is level with Cameo. An earlier UNATTRIBUTED table here "
              "had RV at 3.00x when it measures 2.00x, and compared across frames — worth ~17% "
              "on its own. Pinned as `macro_contrast_peer_median`.\n")

    print(f"\n## §9.4 spread band {SPREAD_LO:.0f}x-{SPREAD_HI:.0f}x (target 4x) — "
          f"{len(families) - len(spread_bad) - len(FLAT_BY_DESIGN)} in band, "
          f"median {_st.median(all_spreads):.2f}x over {len(all_spreads)} families\n"
          if all_spreads else
          f"\n## §9.4 spread band {SPREAD_LO:.0f}x-{SPREAD_HI:.0f}x (target 4x) — "
          f"{len(families) - len(spread_bad) - len(FLAT_BY_DESIGN)} in band\n")
    # ⛔ THE MEDIAN IS PRINTED BECAUSE ITS ABSENCE CAUSED A REAL ERROR. This section used to
    # report only the OFFENDERS, so the band's central value lived nowhere and "4.00x" was
    # quoted from memory — sometimes meaning this per-FAMILY figure, sometimes a per-WEAPON
    # one measured over a different population entirely. A law with a target and no published
    # central value invites exactly that. Note the population: ONE level per family, over
    # `armor_rows` (the 16 class armors, `Heroic` INCLUDED, platings and Shield excluded).
    for family, spread in sorted(spread_bad, key=lambda r: r[1]):
        why = "too FLAT" if spread < SPREAD_LO else "too SHARP"
        print(f"  {family:14s} {spread:6.2f}x   {why}")
    # ⭐ REPORT THE MARGINS, NOT ONLY THE PASS. "100% in band" hides how close the band is to
    # breaking, and this corpus has two live reasons to care:
    #   * the CEILING is structural — `macro_spread` pushes a family's disfavoured ladders down
    #     together, so a new anti-air family or a change to `build_order`'s interleave eats the
    #     headroom without anyone touching `MACRO_RATIO`;
    #   * the FLOOR has a float/int seam — `fit_band_floor` aims at BAND_LOW * BAND_MARGIN in
    #     floats and the emit rounds to integers, which is how `CannonAP_Light` once landed at
    #     137/69 = 1.9855x from a fix that had just put it in band.
    # A pass with 0.7% of headroom and a pass with 12% are not the same fact.
    if all_spreads:
        lo_f, hi_f = min(all_spreads), max(all_spreads)
        print(f"\n  margin to the 2x FLOOR    {lo_f:5.2f}x  ({(lo_f / SPREAD_LO - 1) * 100:+.1f}%)"
              f"\n  margin to the 8x CEILING  {hi_f:5.2f}x  "
              f"({(1 - hi_f / SPREAD_HI) * 100:.0f}% headroom)")
    print(f"  _(flat by design, excluded: {', '.join(sorted(FLAT_BY_DESIGN))})_")

    print("")
    print("## §12.0d DIRECTION WITHIN A LADDER - must not change between a family's levels")
    print("")
    if flips:
        print("⛔ The tilt may never reorder a ladder, yet these family/ladder pairs rise at")
        print("   one level and fall at another. A near-FLAT profile has no stable direction,")
        print("   so the fix is the family's spread, not the tilt.")
        print("")
        for family, ladder_name, dirs in flips:
            print("  {:14s} ladder {:4s} {}".format(family, ladder_name, " / ".join(dirs)))
    else:
        print("  OK - every family keeps one direction within every ladder, at every level.")

    unexpected_mean = [m for m in mean_bad if not m[2]]
    fail = (len(unexpected_mean) > 0
            or len(spread_bad) > SPREAD_OFFENDERS_BASELINE
            or len(flips) > FLIP_BASELINE)
    print(f"\n{'FAIL' if fail else 'WARN'} "
          f"mean {len(mean_bad)}/{MEAN_OFFENDERS_BASELINE} ({len(unexpected_mean)} unexpected) · "
          f"spread {len(spread_bad)}/{SPREAD_OFFENDERS_BASELINE} · "
          f"orientation flips {len(flips)}/{FLIP_BASELINE}")
    if fail:
        print("**A profile law regressed.** Fix the profile — never raise a ratchet, and never "
              "hand-edit a Versus value (they are generated; `Versus` lives only in ^Warhead_*).")
    else:
        print("Lower the ratchets as profiles are brought onto the laws; never raise them.")
    return 1 if fail else 0


if __name__ == "__main__":
    sys.exit(main())
