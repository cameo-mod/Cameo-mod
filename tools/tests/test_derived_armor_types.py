"""DESIGN §12.0l derived armour types: one list, three implementations, one rule.

The generator (`gen_weapon_template.GEO_DERIVED`), the runtime bell (`HeavinessBell.cs`) and its
Python mirror (`effective_heaviness.GEO_DERIVED`) must derive the SAME columns from the SAME
parents in the SAME order, or a weapon with `Heaviness` set would carry different derived rows in
the game than in every balance tool. The bell's other §12.0l/R16 duties are pinned here too:
Heroic = Plate x Scout / 200 in the MAIN table only, geometric-mean renormalisation.
"""
from __future__ import annotations

import math
import pathlib
import re
import statistics
import unittest

import sys

import _bootstrap  # noqa: F401 - sys.path side effect

ROOT = pathlib.Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "tools" / "balance"))

import effective_heaviness as eh  # noqa: E402
import gen_weapon_template as gen  # noqa: E402
BELL_CS = ROOT / "OpenRA.Mods.Cameo" / "Warheads" / "HeavinessBell.cs"

# A real, non-flat main profile (^Warhead_Bullet_Medium's 16 class rows, 2026-09-26).
BULLET_MEDIUM = {
    "Shield": 145, "None": 200, "Flak": 163, "Plate": 155, "Heroic": 95, "Scout": 123,
    "Light": 114, "Medium": 90, "Heavy": 74, "Superheavy": 60, "Wood": 110, "Steel": 95,
    "Concrete": 80, "Fighter": 120, "Bomber": 100, "Helicopter": 82, "Spaceship": 70,
}


def _cs_geo_derived():
    text = BELL_CS.read_text(encoding="utf-8")
    body = text[text.index("GeoDerived ="):text.index("};", text.index("GeoDerived ="))]
    out = []
    for name, parents in re.findall(r'\("(\w+)", new\[\] \{([^}]*)\}\)', body):
        out.append((name, tuple(re.findall(r'"(\w+)"', parents))))
    return tuple(out)


def _with_derived(table):
    """The table as a generated template carries it: every derived row present."""
    return dict(gen.derive_rows(list(table.items()), heroic=True))


class DerivedListsAgree(unittest.TestCase):
    def test_generator_python_mirror_and_csharp_list_the_same_columns(self):
        self.assertEqual(tuple(gen.GEO_DERIVED), tuple(eh.GEO_DERIVED))
        self.assertEqual(tuple(gen.GEO_DERIVED), _cs_geo_derived())

    def test_heroic_divisor_is_200_everywhere(self):
        self.assertEqual(gen.HEROIC_DIVISOR, 200)
        self.assertEqual(eh.HEROIC_DIVISOR, 200)
        self.assertIn("const double HeroicDivisor = 200.0;", BELL_CS.read_text(encoding="utf-8"))


class BellHonoursTheDerivedRule(unittest.TestCase):
    def setUp(self):
        self.table = _with_derived(BULLET_MEDIUM)

    def test_every_derived_column_is_the_geomean_of_its_belled_parents(self):
        for h in (0.0, 0.5, 1.0, 1.5, 2.0):
            out = eh.bell_transform(self.table, h, main_table=True)
            for name, parents in eh.GEO_DERIVED:
                want = round(math.prod(max(out[p], 0) for p in parents) ** (1 / len(parents)))
                self.assertEqual(out[name], want, (h, name))

    def test_main_table_heroic_is_plate_times_scout_over_200(self):
        for h in (0.0, 1.0, 2.0):
            out = eh.bell_transform(self.table, h, main_table=True)
            self.assertEqual(out["Heroic"], round(out["Plate"] * out["Scout"] / 200), h)

    def test_percentage_tables_keep_their_own_heroic(self):
        pct = {"None": 20, "Flak": 19, "Plate": 18, "Heroic": 17, "Scout": 16, "Light": 13,
               "Medium": 10, "Heavy": 7, "Superheavy": 5}
        out = eh.bell_transform(pct, 1.3)
        self.assertEqual(out["Heroic"], 17)

    def test_bell_preserves_the_geometric_mean_of_the_tilted_rows(self):
        tilt = [a for a in BULLET_MEDIUM if a in eh.BELL_AXIS and a not in eh.DERIVED_ARMORS]
        before = statistics.geometric_mean(BULLET_MEDIUM[a] for a in tilt)
        for h in (0.0, 1.0, 2.0):
            out = eh.bell_transform(self.table, h, main_table=True)
            after = statistics.geometric_mean(out[a] for a in tilt)
            self.assertAlmostEqual(after, before, delta=1.0)   # integer rounding only


if __name__ == "__main__":
    unittest.main()
