"""The MACRO-CONTRAST AXIS — `gen_weapon_template.macro_spread`.

⛔ WHAT THIS EXISTS TO PIN. A profile had two knobs, both WITHIN-ladder: where along a
ladder the family peaks (the heaviness shaper) and which macro type leads the row order
(`build_order`). Neither controls how far the preferred macro type pulls AWAY from the
rest, which is the metric the maintainer rejected at 1.7x.

The axis is only safe because of four structural properties, and each is a test here:

  * a ladder is scaled by ONE factor, so it CANNOT reorder internally -> §12.0d holds by
    construction and needs no rank restore (unlike the tilt, which does);
  * `mean_normalise` runs after it, so §12.0h MEAN-100 and price invariance survive;
  * ranks come from the profile's OWN ladder means, so the axis amplifies a family's
    measured identity and can never impose one;
  * TIES SHARE A FACTOR, which is what makes a generalist inert WITHOUT a special case.
    That last one matters most: `Sonic`, `Magic` and `Concussion` are exempt because of
    what they ARE, not because someone remembered to list them.
"""

from __future__ import annotations

import pathlib
import subprocess
import sys
import unittest

import _bootstrap  # noqa: F401 — sys.path side effect

ROOT = pathlib.Path(__file__).resolve().parents[2]
# `_bootstrap` puts tools/audit on the path; the generator lives in tools/balance, which
# every balance test adds for itself (see test_exact_profile_duplicate_consolidation).
sys.path.insert(0, str(ROOT / "tools" / "balance"))

import gen_weapon_template as g  # noqa: E402


def profile(**over):
    """A profile with a clear macro preference: INF high, VEH middle, BLD low."""
    rows = [("None", 150.0), ("Flak", 130.0), ("Plate", 110.0),
            ("Scout", 100.0), ("Light", 95.0), ("Medium", 90.0), ("Heavy", 85.0),
            ("Superheavy", 80.0),
            ("Wood", 70.0), ("Steel", 65.0), ("Concrete", 60.0),
            ("Fighter", 55.0), ("Bomber", 50.0), ("Helicopter", 45.0), ("Spaceship", 40.0),
            ("Heroic", 30.0)]
    return [(a, over.get(a, v)) for a, v in rows]


def means(rows):
    return g.macro_means(dict(rows))


class TheAxisIsInertUntilItIsTurnedOn(unittest.TestCase):
    def test_ratio_one_is_the_identity(self):
        rows = profile()
        self.assertEqual(rows, g.macro_spread(rows, 1.0))

    def test_the_shipped_default_is_off(self):
        """It regenerates every template, so it is BOOT-GATED. The default must stay 1.0
        until a sweep is ruled on — a agent-chosen ratio would ship a silent rebalance."""
        self.assertEqual(1.0, g.MACRO_RATIO)
        self.assertEqual(profile(), g.macro_spread(profile()))


class TheWideningIsExactlyTheRatio(unittest.TestCase):
    def test_most_over_least_favoured_ladder_mean_grows_by_the_ratio(self):
        before = means(profile())
        for ratio in (1.25, 1.5, 2.0, 3.0):
            after = means(g.macro_spread(profile(), ratio))
            gap_before = max(before.values()) / min(before.values())
            gap_after = max(after.values()) / min(after.values())
            self.assertAlmostEqual(gap_before * ratio, gap_after, places=6, msg=str(ratio))

    def test_it_amplifies_the_families_own_preference_never_imposes_one(self):
        """Ranks come from the profile itself, so a BLD-preferring family gets pushed
        further toward BLD — not toward whatever a hard-coded table would have said."""
        flipped = profile(**{"None": 60.0, "Flak": 65.0, "Plate": 70.0,
                             "Wood": 150.0, "Steel": 130.0, "Concrete": 110.0})
        before, after = means(flipped), means(g.macro_spread(flipped, 2.0))
        self.assertGreater(before["BLD"], before["INF"])
        self.assertGreater(after["BLD"] / after["INF"], before["BLD"] / before["INF"])


class ItCannotBreakTheLawsThatBindHere(unittest.TestCase):
    def test_no_ladder_can_reorder_internally_at_any_ratio(self):
        """§12.0d. One factor per ladder makes this true BY CONSTRUCTION — there is no
        rank restore here, and this test is why none is needed."""
        base = dict(profile())
        for ratio in (1.25, 1.5, 2.0, 3.0, 4.0):
            after = dict(g.macro_spread(profile(), ratio))
            for rungs in g.LADDERS.values():
                present = [a for a in rungs if a in base]
                self.assertEqual(sorted(present, key=lambda a: base[a]),
                                 sorted(present, key=lambda a: after[a]),
                                 f"ladder reordered at ratio {ratio}")

    def test_the_row_sequence_is_preserved(self):
        """Downstream stages index positionally; a reordered list would corrupt them."""
        rows = profile()
        self.assertEqual([a for a, _ in rows],
                         [a for a, _ in g.macro_spread(rows, 2.0)])

    def test_the_derived_cell_is_RE_DERIVED_not_scaled_and_not_frozen(self):
        """§12.0b: `Heroic = Plate x Scout / PEAK`, recomputed from the FINISHED profile.

        ⛔ THIS TEST USED TO ASSERT THE WRONG PROPERTY and it hid a real bug. It checked that
        `Heroic` was UNCHANGED, which is not the law — the law is that it is not scaled as a
        RUNG but IS recomputed once its inputs move. This stage moves both inputs (`Plate` in
        INF, `Scout` in VEH), so "unchanged" meant "stale". The test passed, and what actually
        caught it was measuring the audit's §9.4 metric, where `Heroic` sits inside the INF
        ladder mean and a frozen row visibly damped the axis.

        A test can encode a weaker property than the law and then defend the bug. Assert the
        MECHANISM — recomputed from the finished cells — not a symptom.
        """
        after = dict(g.macro_spread(profile(), 2.0))
        peak = max(v for a, v in after.items()
                   if a not in g.NON_ARMOR_ROWS and a not in g.DERIVED_ARMORS)
        self.assertAlmostEqual(after["Plate"] * after["Scout"] / peak, after["Heroic"],
                               places=6)
        # and it must NOT be the INF rung factor applied to the old value
        before = dict(profile())
        self.assertNotAlmostEqual(before["Heroic"] * after["Plate"] / before["Plate"],
                                  after["Heroic"], places=3)


class GeneralistsAreExemptWithoutBeingNamed(unittest.TestCase):
    def test_a_flat_profile_is_returned_untouched(self):
        """`Sonic` and `Magic` reach this stage on the FLAT/PCT branches with every row
        equal. No gradient means nothing to spread, and no list of exempt families."""
        flat = [(a, 100.0) for a, _ in profile()]
        self.assertEqual(flat, g.macro_spread(flat, 3.0))

    def test_ladders_that_tie_keep_tying(self):
        """A family that genuinely does not distinguish two macro types must not have a
        distinction invented for it by rank order."""
        tied = profile(**{"Wood": 110.0, "Steel": 130.0, "Concrete": 150.0})
        before, after = means(tied), means(g.macro_spread(tied, 2.0))
        self.assertAlmostEqual(before["INF"], before["BLD"], places=6)
        self.assertAlmostEqual(after["INF"], after["BLD"], places=6)


class TheSweepFlagIsUsableAndBounded(unittest.TestCase):
    def test_the_generator_accepts_a_ratio_and_rejects_nonsense(self):
        gen = ROOT / "tools/balance/gen_weapon_template.py"
        ok = subprocess.run([sys.executable, str(gen), "--macro=1.5", "--list"],
                            capture_output=True, text=True)
        self.assertEqual(0, ok.returncode, ok.stderr[-400:])
        bad = subprocess.run([sys.executable, str(gen), "--macro=9"],
                             capture_output=True, text=True)
        self.assertNotEqual(0, bad.returncode)


class HeroicIsCalculatedButNotMeasured(unittest.TestCase):
    """⭐ MAINTAINER RULING 2026-08-30 (WEAPON_HEAVINESS §9.4a).

    *"Since Heroic armor is only for hero units with build limits it should not be
    included in the 4x measurements ... Heroic should only be calculated but not be
    part of the spread analysis."*

    The distinction is the whole ruling: `Heroic` stays in MEAN-100 (a weapon really
    does damage heroes, and pricing must see it) and leaves the two SPREAD metrics.
    These pin BOTH halves, because dropping it from `armor_rows` outright would
    silently change pricing.
    """

    def test_the_derived_row_is_named_and_excluded_from_the_spread_metrics(self):
        import audit_versus_profile as A
        self.assertIn("Heroic", A.DERIVED_ROWS)
        for rungs in A.MACRO_LADDERS.values():
            self.assertNotIn("Heroic", rungs)

    def test_it_is_STILL_counted_by_MEAN_100(self):
        """⛔ The half that is easy to over-apply. §12.0h averages every armor row; a
        weapon that ignores heroes is not thereby cheaper. `armor_rows` must keep it."""
        import audit_versus_profile as A
        self.assertNotIn("Heroic", A.NON_ARMOR)
        self.assertIn("Heroic", A.armor_rows({"Heroic": 50.0, "None": 100.0}))

    def test_the_premise_still_holds_no_unlimited_unit_wears_heroic(self):
        """The ruling rests on a fact about the tree, so the fact is a test. If a
        buildable-unlimited actor ever gains `Heroic`, the exclusion stops being
        justified and this fails rather than quietly mis-measuring the corpus."""
        import miniyaml
        rs = miniyaml.Ruleset(ROOT)
        offenders = []
        for name in rs.actors:
            if name.startswith(("^", "-")):
                continue
            r = rs.resolve(name)
            if r is None or "Heroic" not in {a.get("Type") for a in r.children_named("Armor")}:
                continue
            b = r.child("Buildable")
            if b is not None and not b.get("BuildLimit"):
                offenders.append(name)
        self.assertEqual([], offenders,
                         "buildable-unlimited actors now wear Heroic; §9.4a's premise is broken")


if __name__ == "__main__":
    unittest.main()
