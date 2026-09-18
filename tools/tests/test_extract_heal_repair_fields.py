"""The ledger carries SelfHealing Step and Repairable HpPerStep with honest provenance."""

from __future__ import annotations

import json
import pathlib
import unittest


ROOT = pathlib.Path(__file__).resolve().parents[2]
LEDGER = ROOT / "docs" / "balance"


class ExtractHealRepairFieldsTests(unittest.TestCase):
    """PR #404's precondition, closed: the batch shape before this could not
    ride the ledger for heal/repair at all - extract_stats.stat() looked up
    the literal key `ChangesHealth`, so `ChangesHealth@SelfHealing` (828
    blocks vs 88 bare ones) was invisible, and `Repairable.HpPerStep` had no
    schema slot. These tests pin the two new fields on the committed ledger.
    """

    @staticmethod
    def row(name: str) -> dict:
        for path in sorted(LEDGER.glob("*.json")):
            doc = json.loads(path.read_text(encoding="utf-8-sig"))
            sections = doc.get("sections")
            if not isinstance(sections, dict):
                continue
            for section in sections.values():
                if name in section:
                    return section[name]
        raise AssertionError(f"{name} absent from the ledger")

    def test_template_backed_heal_repair_extracts_as_inherited(self):
        # ^TDHARV carries the values; the TD GDI harvester only inherits.
        u = self.row("td_gdi_tiberiumharvester")
        self.assertEqual(
            {"src": "inherited", "v": "60"}, u.get("self_heal_step"))
        self.assertEqual(
            {"src": "inherited", "v": "7500"}, u.get("repairable_hp_per_step"))

    def test_own_block_instances_carry_file_provenance(self):
        u = self.row("td_nod_stealthharvester")
        slot = u.get("self_heal_step")
        self.assertEqual("50", slot["v"])
        self.assertIn("#ChangesHealth@SelfHealing.Step", slot["src"])
        repair = u.get("repairable_hp_per_step")
        self.assertEqual("6250", repair["v"])
        self.assertIn("#Repairable.HpPerStep", repair["src"])

    def test_a_faction_specific_override_beats_the_template(self):
        u = self.row("japan_japaneseoretruck")
        self.assertEqual("30", u.get("self_heal_step")["v"])
        self.assertIn("#ChangesHealth@SelfHealing.Step", u.get("self_heal_step")["src"])
        self.assertEqual("3750", u.get("repairable_hp_per_step")["v"])


if __name__ == "__main__":
    unittest.main()
