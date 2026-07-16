#!/usr/bin/env python3

from __future__ import annotations

import unittest

import pathlib
import tempfile

from audit_display_text import actor_id_pattern, leaked_ids, scan_text_file


class DisplayTextAuditTests(unittest.TestCase):
    def setUp(self):
        self.pattern = actor_id_pattern({
            "td_nod_handofnod", "td_nod_gunturret", "td_gdi_apc",
        })

    def test_internal_id_in_prose_is_detected(self):
        self.assertEqual(
            leaked_ids("Black td_nod_handofnod Flamer", self.pattern),
            ("td_nod_handofnod",),
        )

    def test_ordinary_words_are_not_actor_ids(self):
        for value in ("Hand of Nod", "Black Hand", "left hand", "machine gun", "APC Truck"):
            self.assertEqual(leaked_ids(value, self.pattern), ())

    def test_identifier_boundaries_avoid_partial_matches(self):
        self.assertEqual(leaked_ids("x_td_nod_handofnod", self.pattern), ())

    def test_nested_display_lists_and_comments_are_detected(self):
        with tempfile.TemporaryDirectory() as directory:
            root = pathlib.Path(directory)
            path = root / "rules.yaml"
            path.write_text(
                "Power:\n"
                "\tNames:\n"
                "\t\t1: Unlock td_gdi_apc\n"
                "\tActors:\n"
                "\t\t1: td_gdi_apc\n"
                "# Black td_nod_handofnod infantry\n",
                encoding="utf-8",
            )
            findings = scan_text_file(path, True, root, self.pattern, include_comments=True)

        self.assertEqual(
            [(f.field, f.value) for f in findings],
            [
                ("Names[]", "Unlock td_gdi_apc"),
                ("Comment", "Black td_nod_handofnod infantry"),
            ],
        )

    def test_structural_lua_strings_are_not_display_text(self):
        with tempfile.TemporaryDirectory() as directory:
            root = pathlib.Path(directory)
            path = root / "script.lua"
            path.write_text(
                'Actor.Create("td_gdi_apc")\n'
                'Media.DisplayMessage("Destroy the td_gdi_apc")\n',
                encoding="utf-8",
            )
            findings = scan_text_file(path, False, root, self.pattern)

        self.assertEqual(
            [(f.field, f.value) for f in findings],
            [("LuaString", "Destroy the td_gdi_apc")],
        )


if __name__ == "__main__":
    unittest.main()
