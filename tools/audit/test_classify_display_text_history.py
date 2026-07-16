#!/usr/bin/env python3

from __future__ import annotations

import unittest

from audit_display_text import actor_id_pattern
from classify_display_text_history import changes_confined_to_ids, framed_value, value_frame


class DisplayTextHistoryTests(unittest.TestCase):
    def setUp(self):
        self.pattern = actor_id_pattern({"td_nod_handofnod", "td_nod_gunturret"})

    def test_exact_context_blind_rename_is_accepted(self):
        self.assertTrue(changes_confined_to_ids(
            "Hand of Nod", "td_nod_handofnod of Nod", self.pattern))
        self.assertTrue(changes_confined_to_ids(
            "Machine Gun Turret", "Machine td_nod_gunturret Turret", self.pattern))

    def test_unrelated_wording_change_is_rejected(self):
        self.assertFalse(changes_confined_to_ids(
            "Hand of Nod", "Advanced td_nod_handofnod of Nod", self.pattern))

    def test_value_frame_round_trip(self):
        line = "\t\tName: td_nod_handofnod of Nod # barracks"
        prefix, suffix = value_frame(line, "td_nod_handofnod of Nod")
        self.assertEqual(prefix, "\t\tName: ")
        self.assertEqual(suffix, " # barracks")
        self.assertEqual(framed_value(
            "\t\tName: Hand of Nod # barracks", prefix, suffix), "Hand of Nod")


if __name__ == "__main__":
    unittest.main()
