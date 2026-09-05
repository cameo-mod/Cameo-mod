"""ONE CLASS TEMPLATE PER BUILDABLE UNIT — and the traps in measuring that.

⛔ Maintainer law, 2026-09-02: a buildable unit with no class template is a defect; one with more
than one is a defect; `^EpicVehicleTemplate` / `^EpicAirUnitTemplate` are ADD-ONS that layer on top
of a full class. This is also the classification the balance pipeline must read, in place of the
ledger's `design.class_anchor` tag, which had drifted to agreeing on only 8 of 27 classes.
"""

from __future__ import annotations

import pathlib
import sys
import unittest

ROOT = pathlib.Path(__file__).resolve().parents[2]
AUDIT = ROOT / "tools" / "audit" / "audit_class_templates.py"
sys.path.insert(0, str(ROOT / "tools" / "audit"))
import audit_class_templates as act  # noqa: E402


class ClassTemplateAuditTest(unittest.TestCase):
    def test_it_walks_every_inherits_key_not_just_the_bare_one(self):
        """⛔ THE BUG THAT HID THE WHOLE TAXONOMY. Membership arrives as `Inherits@Template:`;
        following only `Inherits:` reports zero members for every class in the mod."""
        src = AUDIT.read_text(encoding="utf-8")
        self.assertIn('c.key.split("@", 1)[0] == "Inherits"', src)
        self.assertIn("KEYED", src)

    def test_upgrade_templates_are_not_classes(self):
        """632 buildable actors are upgrades and promotions. Counting them as unclassified units
        turned a 109-defect finding into a 1,153-defect one."""
        for t in ("^UpgradeTemplate", "^PromotionUpgradeTemplate", "^DoctrineTemplate"):
            self.assertIn(t, act.NOT_A_CLASS)

    def test_the_addon_exception_is_exactly_the_two_the_maintainer_named(self):
        """Widening this set silently reclassifies real defects as legal."""
        self.assertEqual(act.ADD_ON, {"^EpicVehicleTemplate", "^EpicAirUnitTemplate"})

    def test_an_addon_alone_is_still_a_defect(self):
        """An epic tank with no base class has no class. 24 actors are in this state."""
        src = AUDIT.read_text(encoding="utf-8")
        self.assertIn("addon_only", src)
        self.assertIn("Add-on template but no class", src)

    def test_template_usage_is_recorded_before_the_scope_skip(self):
        """⛔ MY OWN BUG, CAUGHT BY READING THE OUTPUT. Recording usage after the building skip
        made `^BasicDefenseTemplate` report as DEAD, listed next to the defence buildings that
        plainly inherit it. Dead-template detection has to see every actor, not every UNIT."""
        src = AUDIT.read_text(encoding="utf-8")
        self.assertIn("used |= anc_tmpl", src)
        i_used = src.index("used |= anc_tmpl")
        i_skip = src.index("if not is_mobile:")
        self.assertLess(i_used, i_skip, "usage must be recorded before the building skip")

    def test_failures_are_counted_not_swallowed(self):
        """⛔ A bare `except: continue` in a census turns every failure into a zero and prints the
        zero as a measurement. That is exactly how this taxonomy was reported as empty."""
        src = AUDIT.read_text(encoding="utf-8")
        self.assertIn("unreadable", src)
        self.assertNotIn("except Exception:\n            continue", src)

    def test_it_reports_dead_templates(self):
        """A class whose template nothing inherits has no structural members, however many the
        ledger tags. Five are dead, and two of those back SIGNED class anchors."""
        src = AUDIT.read_text(encoding="utf-8")
        self.assertIn("Dead class templates", src)

    def test_it_is_wired_into_the_blocking_audit_loop(self):
        runner = (ROOT / "tools" / "audit" / "run_all.sh").read_text(encoding="utf-8")
        self.assertIn("class_templates", runner)


if __name__ == "__main__":
    unittest.main()


class SubTemplateTest(unittest.TestCase):
    """⛔ TEMPLATES INHERIT TEMPLATES, AND A TRANSITIVE COUNT CALLS THAT A DEFECT.

    `^UnarmedTransportHelicopterTemplate` declares `Inherits@Template: ^HelicopterTemplate`, and
    `^DogTemplate` declares `Inherits: ^MeleeInfantryTemplate`. A chinook naming ONLY the transport
    template still has two templates in its ancestry. The first run of this audit reported 18
    multi-class defects; 12 of them were this bug and the real number is 6. The maintainer caught it
    by asking whether the transport template already inherits the helicopter one.
    """

    def test_only_the_most_specific_template_counts(self):
        src = AUDIT.read_text(encoding="utf-8")
        self.assertIn("superseded", src)
        self.assertIn("anc_tmpl -= superseded", src)
        self.assertIn("MOST SPECIFIC", src)

    def test_the_counting_path_uses_the_most_specific_set(self):
        """The first fix landed on the building branch only and the count stayed at 18."""
        src = AUDIT.read_text(encoding="utf-8")
        self.assertIn("tmpl = anc_tmpl", src)

    def test_a_base_template_is_not_dead_just_because_its_subtemplate_is_used(self):
        """⚠ `used` holds only the most specific template, so `^HelicopterTemplate` would report
        dead the day every helicopter became a transport. Closed upward before subtracting."""
        src = AUDIT.read_text(encoding="utf-8")
        self.assertIn("used_closed", src)
        self.assertIn("declared - used_closed - ADD_ON", src)
