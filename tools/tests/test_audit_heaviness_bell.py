"""Unit tests for the three audits added on 2026-08-23.

    tools/audit/audit_heaviness_bell.py
    tools/audit/audit_engine_freshness.py
    tools/audit/audit_upstream_adoption.py

All three were written on 2026-08-23, and two of them had a real defect on the first run. These
tests pin what broke, and the invariants the rulings rest on:

  * `audit_engine_freshness.read_version_file` read `engine/VERSION` as UTF-8. It is UTF-16 LE
    with a BOM — the SDK writes it from PowerShell, the same hazard that forces `bash
    run_all.sh` — so the audit reported a permanent, FALSE "the built engine is not the pinned
    one" on every machine.
  * `audit_heaviness_bell.belled` renormalises to a constant mean. That invariant IS the ruling
    in DESIGN §12.0i: if the mean drifts, `K` moves with heaviness and every weapon that sets an
    `h` is silently re-priced. A test is cheaper than discovering that in the ledger.
  * `audit_upstream_adoption.scan` binds each `[Desc(...)]` to the class BELOW it, and that
    association is what pairs a duplicate mechanic arriving under a different name. Get it wrong
    and a description leaks onto the next class, inventing duplicates that do not exist.
"""

from __future__ import annotations

import pathlib
import statistics
import tempfile
import unittest

import _bootstrap  # noqa: F401 — sys.path side effect

import audit_heaviness_bell as bell
import audit_engine_freshness as fresh
import audit_upstream_adoption as adopt


class CentreOfMass(unittest.TestCase):
    def test_anti_heavy_profile_sits_high_on_the_axis(self):
        # The TOP rung of each ladder is x=2, so all the weight there centres at 2.
        com = bell.centre_of_mass({"Plate": 100.0, "Concrete": 100.0, "Superheavy": 100.0})
        self.assertAlmostEqual(com, 2.0)

    def test_heavy_is_below_superheavy_so_it_pulls_the_centre_down(self):
        # `Heavy` is the FOURTH of five vehicle rungs (x=1.5), not the top — the distinction the
        # coarse three-bucket axis could not make.
        com = bell.centre_of_mass({"Plate": 100.0, "Concrete": 100.0, "Heavy": 100.0})
        self.assertAlmostEqual(com, (2.0 + 2.0 + 1.5) / 3)

    def test_anti_light_profile_sits_low(self):
        com = bell.centre_of_mass({"None": 100.0, "Wood": 100.0, "Scout": 100.0})
        self.assertAlmostEqual(com, 0.0)

    def test_balanced_profile_sits_in_the_middle(self):
        com = bell.centre_of_mass({"None": 100.0, "Plate": 100.0})
        self.assertAlmostEqual(com, 1.0)

    def test_weighting_is_by_versus_not_by_count(self):
        # Two light rows at 10 vs one heavy row at 180: the heavy row dominates.
        com = bell.centre_of_mass({"None": 10.0, "Wood": 10.0, "Plate": 180.0})
        self.assertGreater(com, 1.5)

    def test_no_placeable_armor_returns_none(self):
        self.assertIsNone(bell.centre_of_mass({"Shield": 100.0}))

    def test_zero_rows_are_ignored_rather_than_dividing_by_zero(self):
        self.assertIsNone(bell.centre_of_mass({"None": 0.0, "Plate": 0.0}))


class BellPreservesTheMean(unittest.TestCase):
    """DESIGN §12.0i: heaviness redistributes, it never inflates. K must stay invariant in h."""

    PROFILE = {"None": 40.0, "Wood": 55.0, "Flak": 90.0, "Steel": 95.0,
               "Plate": 160.0, "Concrete": 150.0}

    def test_mean_is_unchanged_at_every_heaviness(self):
        before = statistics.mean(self.PROFILE.values())
        com = bell.centre_of_mass(self.PROFILE)
        for h in (0.0, 0.5, 1.0, 1.5, 2.0):
            after = statistics.mean(bell.belled(self.PROFILE, com + bell.SHIFT * (h - 1)).values())
            self.assertAlmostEqual(after, before, places=9, msg=f"mean drifted at h={h}")

    def test_off_axis_rows_pass_through_untouched_by_the_curve(self):
        profile = dict(self.PROFILE, Shield=200.0)
        out = bell.belled(profile, 1.0)
        # Shield has no x, so the curve does not apply — only the renormalisation scalar does.
        scale = out["None"] / profile["None"]
        self.assertNotAlmostEqual(scale, out["Shield"] / profile["Shield"], places=6)

    def test_shifting_heavier_raises_the_heavy_rows_relative_to_the_light_ones(self):
        com = bell.centre_of_mass(self.PROFILE)
        light = bell.belled(self.PROFILE, com - 0.25)
        heavy = bell.belled(self.PROFILE, com + 0.25)
        self.assertGreater(heavy["Plate"] / heavy["None"], light["Plate"] / light["None"])


class Direction(unittest.TestCase):
    RUNGS = ["Wood", "Steel", "Concrete"]

    def test_rising_profile_reads_up(self):
        self.assertEqual(
            bell.direction({"Wood": 50.0, "Steel": 80.0, "Concrete": 120.0}, self.RUNGS), "up")

    def test_falling_profile_reads_down(self):
        self.assertEqual(
            bell.direction({"Wood": 120.0, "Steel": 80.0, "Concrete": 50.0}, self.RUNGS), "down")

    def test_equal_endpoints_read_flat(self):
        self.assertEqual(
            bell.direction({"Wood": 90.0, "Steel": 10.0, "Concrete": 90.0}, self.RUNGS), "flat")

    def test_a_single_rung_is_unjudgeable(self):
        self.assertIsNone(bell.direction({"Wood": 90.0}, self.RUNGS))


class ArmorAxis(unittest.TestCase):
    """Each armor sits at its RUNG POSITION inside its own ladder, normalised 0..2.

    The coarse three-bucket version this replaced TIED armors that are not equally heavy, and
    tied coordinates move together under the bell — so heaviness could not distinguish
    Bomber from Helicopter, Scout from Light, or Heavy from Superheavy at all.
    """

    def test_every_ladder_spans_the_full_axis(self):
        for ladder, rungs in bell.LADDERS.items():
            self.assertEqual(bell.BUCKET[rungs[0]], 0.0, ladder)
            self.assertEqual(bell.BUCKET[rungs[-1]], 2.0, ladder)

    def test_each_ladder_is_strictly_increasing(self):
        for ladder, rungs in bell.LADDERS.items():
            xs = [bell.BUCKET[r] for r in rungs]
            self.assertEqual(xs, sorted(xs), ladder)
            self.assertEqual(len(set(xs)), len(xs), f"{ladder} has tied coordinates")

    def test_helicopter_is_heavier_than_bomber(self):
        # Both read as "medium", but the helicopter is the heavier of the two.
        self.assertGreater(bell.BUCKET["Helicopter"], bell.BUCKET["Bomber"])
        # Bomber between light and medium; helicopter between medium and heavy.
        self.assertLess(bell.BUCKET["Bomber"], 1.0)
        self.assertGreater(bell.BUCKET["Helicopter"], 1.0)

    def test_scout_is_lighter_than_light_and_superheavy_heavier_than_heavy(self):
        self.assertLess(bell.BUCKET["Scout"], bell.BUCKET["Light"])
        self.assertGreater(bell.BUCKET["Superheavy"], bell.BUCKET["Heavy"])

    def test_the_rank_restore_preserves_every_ladder_order(self):
        """§12.0d: the tilt is applied to VALUES, then each armor is given back its RANK.

        Without this step the bell reorders ladders — 127 cases across 60 family/ladder pairs on
        the real profiles. With it, zero.
        """
        profile = {"Scout": 40.0, "Light": 55.0, "Medium": 90.0, "Heavy": 95.0,
                   "Superheavy": 160.0, "None": 30.0, "Flak": 70.0, "Plate": 150.0}
        want = {lad: sorted([a for a in rungs if a in profile], key=lambda a: profile[a])
                for lad, rungs in bell.LADDERS.items()}
        for mu in (0.0, 0.5, 1.0, 1.5, 2.0):
            out = bell.belled(profile, mu)
            for lad, rungs in bell.LADDERS.items():
                got = sorted([a for a in rungs if a in out], key=lambda a: out[a])
                if len(got) >= 2:
                    self.assertEqual(got, want[lad], f"{lad} reordered at mu={mu}")

    def test_the_rank_restore_does_not_disturb_the_mean(self):
        # It permutes values within a ladder, so the multiset — and the mean — are unchanged.
        profile = {"Scout": 40.0, "Light": 55.0, "Medium": 90.0, "Heavy": 95.0, "Superheavy": 160.0}
        before = statistics.mean(profile.values())
        for mu in (0.0, 1.0, 2.0):
            self.assertAlmostEqual(statistics.mean(bell.belled(profile, mu).values()),
                                   before, places=9)

    def test_a_heavier_shift_separates_bomber_from_helicopter(self):
        """The distinction the coarse axis could not express at all.

        Bomber (x=0.67) and Helicopter (x=1.33) both read as "medium", but the helicopter is the
        heavier of the two. As the peak moves heavier the gap between them must WIDEN. Under the
        three-bucket axis they shared x=1 and moved identically, so the gap never changed.

        ⚠ The profile needs a real gradient: after §12.0d's rank restore a FLAT profile has no
        order to preserve, so every ratio stays 1.0 no matter where the peak sits.
        """
        profile = {"Fighter": 50.0, "Bomber": 70.0, "Helicopter": 90.0, "Spaceship": 120.0}
        light = bell.belled(profile, 0.75)
        heavy = bell.belled(profile, 1.25)
        self.assertGreater(heavy["Helicopter"] / heavy["Bomber"],
                           light["Helicopter"] / light["Bomber"])

    def test_a_flat_profile_gets_its_ties_broken_toward_ladder_order(self):
        """What actually happens to a family with NO gradient, e.g. Sonic or Magic.

        The rank restore sorts by the value each armor held. With every value equal, Python's
        sort is stable, so the "rank held" falls back to the ladder's own lightest -> heaviest
        order — and the bell's tilted magnitudes are then handed out along it. So a flat family
        is not inert under heaviness: it picks up a mild gradient pointing the same way as the
        ladder. That is a reasonable tie-break rather than a defect, but it is worth pinning,
        because it means "flat family" does NOT mean "heaviness does nothing".
        """
        profile = {a: 100.0 for a in bell.LADDERS["AIR"]}
        out = bell.belled(profile, 1.5)
        ordered = [out[a] for a in bell.LADDERS["AIR"]]
        self.assertEqual(ordered, sorted(ordered), "ties should break toward ladder order")
        self.assertGreater(out["Helicopter"], out["Bomber"])
        # And the mean is still untouched, so it costs nothing in price terms.
        self.assertAlmostEqual(statistics.mean(out.values()), 100.0, places=9)

    def test_the_off_axis_armors_are_not_on_the_axis(self):
        # §12.0c Shield, §12.0b Heroic and the §12.0e platings are excluded by ruling.
        for armor in bell.OFF_AXIS:
            self.assertNotIn(armor, bell.BUCKET, armor)


class ReadVersionFile(unittest.TestCase):
    """engine/VERSION is written by the SDK from PowerShell, i.e. UTF-16 LE with a BOM."""

    HASH = "462fc1fc4bfc490c42b88b429670c7f0c64c7aca"

    def written(self, data: bytes) -> str | None:
        with tempfile.TemporaryDirectory() as tmp:
            path = pathlib.Path(tmp) / "VERSION"
            path.write_bytes(data)
            return fresh.read_version_file(path)

    def test_utf16_le_with_bom_is_the_real_case(self):
        self.assertEqual(self.written(("﻿" + self.HASH).encode("utf-16-le")), self.HASH)

    def test_utf16_be_with_bom(self):
        self.assertEqual(self.written(("﻿" + self.HASH).encode("utf-16-be")), self.HASH)

    def test_plain_utf8_still_works(self):
        self.assertEqual(self.written(self.HASH.encode("utf-8")), self.HASH)

    def test_utf8_with_bom_still_works(self):
        self.assertEqual(self.written(("﻿" + self.HASH).encode("utf-8")), self.HASH)

    def test_trailing_whitespace_and_newlines_are_stripped(self):
        self.assertEqual(self.written((self.HASH + "\r\n\r\n").encode("utf-8")), self.HASH)

    def test_a_missing_file_is_none_rather_than_an_exception(self):
        with tempfile.TemporaryDirectory() as tmp:
            self.assertIsNone(fresh.read_version_file(pathlib.Path(tmp) / "absent"))


class GitHelper(unittest.TestCase):
    def test_a_failed_git_call_returns_none_rather_than_raising(self):
        with tempfile.TemporaryDirectory() as tmp:
            self.assertIsNone(fresh.git(pathlib.Path(tmp), "rev-parse", "no-such-ref"))

    def test_indented_format_output_keeps_its_indent(self):
        # The helper strips newlines only: a --format that indents must survive it.
        root = pathlib.Path(__file__).resolve().parents[2]
        out = fresh.git(root, "log", "-1", "--format=  indented")
        if out is not None:                      # skip where git or history is unavailable
            self.assertTrue(out.startswith("  "), repr(out))


class DescriptionNormalisation(unittest.TestCase):
    """The pairing that catches a duplicate mechanic under a different name."""

    def test_case_punctuation_and_spacing_are_ignored(self):
        self.assertEqual(adopt.norm("This actor can be affected by temporal warheads."),
                         adopt.norm("this  actor CAN be affected by temporal warheads"))

    def test_genuinely_different_text_does_not_collide(self):
        self.assertNotEqual(adopt.norm("Creates a smudge in `SmudgeLayer`."),
                            adopt.norm("Spawn actors upon explosion."))

    def test_the_temporal_pair_that_started_this_is_recorded(self):
        # RV's Temporal/AffectedByTemporal ARE CA's WarpDamage/Warpable. The prose pairs the
        # traits; only reading both implementations pairs the warheads, so they are manual.
        self.assertIn("Temporal", adopt.KNOWN_EQUIVALENTS)
        self.assertIn("AffectedByTemporal", adopt.KNOWN_EQUIVALENTS)


class ScanAssociatesDescWithTheClassBelowIt(unittest.TestCase):
    def scan(self, source: str):
        with tempfile.TemporaryDirectory() as tmp:
            root = pathlib.Path(tmp)
            (root / "Thing.cs").write_text(source, encoding="utf-8")
            return adopt.scan(root)

    def test_info_class_yields_the_yaml_name_without_the_suffix(self):
        names, _descs = self.scan("public class GrantConditionOnDeployInfo : TraitInfo { }")
        self.assertIn("GrantConditionOnDeploy", names)

    def test_warhead_class_yields_the_yaml_name_without_the_suffix(self):
        names, _descs = self.scan("public class SpreadDamageWarhead : Warhead { }")
        self.assertIn("SpreadDamage", names)

    def test_desc_binds_to_the_class_that_follows_it(self):
        _names, descs = self.scan(
            '[Desc("First mechanic.")]\n'
            'public class AlphaInfo : TraitInfo { }\n'
            '[Desc("Second mechanic.")]\n'
            'public class BetaInfo : TraitInfo { }\n')
        self.assertEqual(descs["Alpha"], "First mechanic.")
        self.assertEqual(descs["Beta"], "Second mechanic.")

    def test_a_desc_is_not_reused_by_a_later_undescribed_class(self):
        _names, descs = self.scan(
            '[Desc("Only Alpha has this.")]\n'
            'public class AlphaInfo : TraitInfo { }\n'
            'public class BetaInfo : TraitInfo { }\n')
        self.assertEqual(descs["Alpha"], "Only Alpha has this.")
        self.assertNotIn("Beta", descs)

    def test_obj_and_bin_output_is_skipped(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = pathlib.Path(tmp)
            (root / "obj").mkdir()
            (root / "obj" / "Generated.cs").write_text(
                "public class GeneratedInfo : TraitInfo { }", encoding="utf-8")
            names, _descs = adopt.scan(root)
        self.assertNotIn("Generated", names)

    def test_a_missing_directory_is_empty_rather_than_an_exception(self):
        names, descs = adopt.scan(pathlib.Path("no", "such", "dir"))
        self.assertEqual((names, descs), ({}, {}))


class UsageCount(unittest.TestCase):
    """A type its own mod never references is dead code there too, not a porting candidate."""

    def counts(self, yaml_text: str, names):
        with tempfile.TemporaryDirectory() as tmp:
            root = pathlib.Path(tmp)
            (root / "mods").mkdir()
            (root / "mods" / "rules.yaml").write_text(yaml_text, encoding="utf-8")
            return adopt.usage_count(root, ["mods"], set(names))

    def test_a_bare_trait_declaration_counts(self):
        self.assertEqual(self.counts("actor:\n\tLaysMinefield:\n", {"LaysMinefield"}),
                         {"LaysMinefield": 1})

    def test_a_suffixed_trait_declaration_counts(self):
        self.assertEqual(self.counts("actor:\n\tLaysMinefield@x:\n", {"LaysMinefield"}),
                         {"LaysMinefield": 1})

    def test_a_value_reference_counts(self):
        self.assertEqual(self.counts("weapon:\n\tWarhead@1: CashHack\n", {"CashHack"}),
                         {"CashHack": 1})

    def test_an_unreferenced_type_counts_zero(self):
        self.assertEqual(self.counts("actor:\n\tSomethingElse:\n", {"LaysMinefield"}),
                         {"LaysMinefield": 0})


if __name__ == "__main__":
    unittest.main()
