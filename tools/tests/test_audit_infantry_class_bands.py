"""THE §6b RANGE BANDS — and the two ways an audit of them quietly lies.

⛔ `docs/design/FORMULA_V2.md` §6b: *"CONTIGUOUS half-open range bands: no unit can ever fall
between classes again — the band DEFINES membership."* Four infantry classes have a band; nine
are listed TBD. An audit that judges the TBD nine invents law, and an audit that silently picks a
side at the 5500 boundary hides the one place §6b contradicts itself.

PRIOR ART: `tools/audit/audit_infantry_class_bands.py` is the audit this file TESTS, so the
overlap is by construction. The neighbouring test, `test_audit_class_templates.py`, covers
`audit_class_templates.py`, which asks whether a unit has EXACTLY ONE class; this covers whether
that class is the RIGHT one. No other test reads §6b's bands — `test_band_law.py` is the PRICE
band (`check_band.py`: 0.5x–3.5x of the class formula), an unrelated meaning of the word.
"""

from __future__ import annotations

import pathlib
import sys
import unittest

ROOT = pathlib.Path(__file__).resolve().parents[2]
AUDIT = ROOT / "tools" / "audit" / "audit_infantry_class_bands.py"
sys.path.insert(0, str(ROOT / "tools" / "audit"))
import audit_infantry_class_bands as acb  # noqa: E402


class BandDefinitionTest(unittest.TestCase):
    def test_the_four_bands_are_exactly_the_ones_6b_calls_live(self):
        """A fifth band added here would be law this audit made up. §6b names four."""
        self.assertEqual(
            [(n, lo, hi) for _, n, lo, hi in acb.BANDED],
            [("melee", 1250, 2500), ("closecombat", 2500, 4500),
             ("scout", 4500, 5500), ("special_forces", 5500, 6501)])

    def test_the_bands_are_contiguous(self):
        """"No unit can ever fall between classes again" is only true if no gap exists."""
        for (_, _, _, hi), (_, _, lo, _) in zip(acb.BANDED, acb.BANDED[1:]):
            self.assertEqual(hi, lo, "a gap between bands is a unit with no class")

    def test_bands_are_half_open_so_a_boundary_value_has_one_owner(self):
        """§6b: "a weapon at exactly 2500 is closecombat; exactly 4500 is scout"."""
        self.assertEqual(acb.which_band(2500), "closecombat")
        self.assertEqual(acb.which_band(2499), "melee")
        self.assertEqual(acb.which_band(4500), "scout")
        self.assertEqual(acb.which_band(4499), "closecombat")
        self.assertEqual(acb.which_band(6500), "special_forces")

    def test_the_5500_contradiction_is_reported_not_resolved_in_silence(self):
        """⛔ §6b's TABLE writes scout as the CLOSED interval [4500, 5500] and special forces as
        "5500-6500", so 5500 belongs to both. Its PROSE says half-open. The audit follows the
        prose — and must also say out loud which units sit on that exact value, because a
        boundary an audit picks for itself is a boundary nobody ruled. One unit does:
        `ra1_allies_rifleinfantry`."""
        src = AUDIT.read_text(encoding="utf-8")
        self.assertIn("on_boundary", src)
        self.assertIn("5500", src)
        self.assertIn('r["rng"] == 5500', src)

    def test_the_tbd_classes_are_measured_but_never_judged(self):
        """A grenadier at 6000 is not a defect — grenadier has no band. Only the banded four
        feed the exit code."""
        self.assertIn("^GrenadierInfantryTemplate", acb.UNBANDED_INFANTRY)
        self.assertIn("^HeavyInfantryTemplate", acb.UNBANDED_INFANTRY)
        for tname, _, _, _ in acb.BANDED:
            self.assertNotIn(tname, acb.UNBANDED_INFANTRY)
        src = AUDIT.read_text(encoding="utf-8")
        self.assertIn("findings = len(out_of_band) + len(sf_no_air)", src)

    def test_dogs_are_melee_because_their_template_inherits_melee(self):
        """`^DogTemplate` declares `Inherits: ^MeleeInfantryTemplate`. Counting its members
        under their own template would leave four units outside every band."""
        self.assertEqual(acb.BAND_PARENT.get("^DogTemplate"), "^MeleeInfantryTemplate")


class MeasurementTrapTest(unittest.TestCase):
    def test_garrison_armaments_are_not_the_units_reach(self):
        """A garrisoned armament is the BUILDING's range. Reading it as the unit's would put
        every garrisonable rifleman in the wrong band."""
        src = AUDIT.read_text(encoding="utf-8")
        self.assertIn('"GARRISON" in suffix.upper()', src)
        self.assertIn('str(nm.value).strip() == "garrisoned"', src)

    def test_air_capability_looks_at_every_armament_not_only_the_primary(self):
        """A unit whose anti-air is its SECOND armament still hits air. Checking only the
        primary would report it as a special-forces defect."""
        src = AUDIT.read_text(encoding="utf-8")
        self.assertIn("air = any(weapon_stats(w)[1] for _, w in arms)", src)

    def test_yaml_is_read_through_miniyaml_never_hand_parsed(self):
        """CLAUDE.md rule 8e. A bespoke line scanner is how a whole measurement round came back
        internally consistent and wrong."""
        src = AUDIT.read_text(encoding="utf-8")
        self.assertIn("import miniyaml", src)
        self.assertIn("rs.resolve_weapon", src)

    def test_failures_are_counted_not_swallowed(self):
        """A bare `except: continue` in a census prints zero and calls it a measurement."""
        src = AUDIT.read_text(encoding="utf-8")
        self.assertIn("unreadable", src)
        self.assertNotIn("except Exception:\n            continue", src)

    def test_a_unit_with_two_classes_is_reported_not_dropped(self):
        """⛔ THE SKIP THAT HID THE BASELINE. Two class templates means no single band to check,
        so those units fall out of the measurement. The first draft dropped them in silence —
        and one of the six is `japan_imperialscoutsman`, §6b's own special-forces BASELINE,
        which reaches `^ScoutInfantryTemplate` through `^RA1AlliesRifleInfantry`. The anchor of
        a class was invisible to the audit of that class."""
        src = AUDIT.read_text(encoding="utf-8")
        self.assertIn("ambiguous.append((name, full))", src)
        self.assertIn("japan_imperialscoutsman", src)
        i_skip = src.index("if len(full) != 1:")
        i_report = src.index("ambiguous.append((name, full))")
        self.assertLess(i_skip, i_report)
        self.assertLess(i_report, src.index("continue", i_report))


class WiringTest(unittest.TestCase):
    def test_it_is_advisory_in_run_all(self):
        """Every finding is a maintainer class ruling or a yaml edit needing the boot gate —
        the same reason `class_redundancy` and `ifv_conditions` are advisory. Gating the suite
        on it would make a clean tree exit 1 forever."""
        src = (ROOT / "tools" / "audit" / "run_all.sh").read_text(encoding="utf-8")
        self.assertIn("infantry_class_bands", src)
        advisory = src.split("# ADVISORY audits", 1)[1]
        self.assertIn("infantry_class_bands", advisory)


if __name__ == "__main__":
    unittest.main()
