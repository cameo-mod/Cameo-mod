"""Contract for the classic-four transport chassis-only batch."""

from __future__ import annotations

import pathlib
import json
import sys
import unittest


ROOT = pathlib.Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "tools" / "audit"))
sys.path.insert(0, str(ROOT / "tools" / "balance"))

from cameo_model import Model  # noqa: E402


TARGETS = {
    "td_gdi_chinooktransport": (4120, 82000, 124, 25, 33, 4100),
    "td_nod_chinooktransport": (3853, 82000, 124, 25, 33, 4100),
    "ra1_allies_alliedchinooktransport": (4300, 88000, 120, 24, 35, 4400),
    "ra1_soviets_hiptransport": (2760, 104000, 118, 24, 42, 5200),
}


class AcceptedClassicFourTransportChassisTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.rules = Model(ROOT).rs

    def test_chassis_moves_and_cost_stays_on_passenger_sum(self):
        for name, values in TARGETS.items():
            cost, hp, speed, turn, step, repair = values
            with self.subTest(actor=name):
                actor = self.rules.resolve(name)
                self.assertEqual(str(cost), actor.child("Valued").get("Cost"))
                self.assertEqual(str(hp), actor.child("Health").get("HP"))
                self.assertEqual(str(speed), actor.child("Aircraft").get("Speed"))
                self.assertEqual(str(turn), actor.child("Aircraft").get("TurnSpeed"))
                self.assertEqual(
                    str(step), actor.child("ChangesHealth@SelfHealing").get("Step")
                )
                self.assertEqual(str(repair), actor.child("Repairable").get("HpPerStep"))

    def test_aircraft_turn_speed_is_in_the_raw_ledger(self):
        for name, (_, _, _, turn, _, _) in TARGETS.items():
            with self.subTest(actor=name):
                hits = []
                for path in (ROOT / "docs" / "balance").glob("*.json"):
                    doc = json.loads(path.read_text(encoding="utf-8-sig"))
                    for section in (doc.get("sections") or {}).values():
                        if name in section:
                            hits.append(section[name])
                self.assertEqual(1, len(hits))
                slot = hits[0].get("turn_speed_air")
                self.assertEqual(str(turn), slot["v"])
                self.assertTrue(slot["src"].endswith("#Aircraft.TurnSpeed"))


if __name__ == "__main__":
    unittest.main()
