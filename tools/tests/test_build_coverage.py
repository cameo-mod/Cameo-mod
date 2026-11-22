"""Tests for tools/ai/build_coverage.py (BUILD-COVERAGE-GUARD-01).

Synthetic replays are written in the .orarep layout that order_trace.py documents, so the
HOLD and PASS rules run without game files. Two real-replay checks run only when their
environment variables point at a replay:
  BUILD_COVERAGE_P4ON  known-bad 0d062ae83 game: must HOLD (0 defences on every seat).
  BUILD_COVERAGE_C29   known-good 837a13915 C29 game: must PASS.
"""
import importlib.util
import os
import struct
import tempfile
import unittest
from pathlib import Path

HERE = Path(__file__).resolve().parent
spec = importlib.util.spec_from_file_location("build_coverage", HERE.parents[0] / "ai" / "build_coverage.py")
bc = importlib.util.module_from_spec(spec)
spec.loader.exec_module(bc)

RULES_ROOT = str(HERE.parents[1] / "mods" / "cameo")


def _s(text):
    """.NET BinaryWriter string: 7-bit length prefix, then UTF-8."""
    raw = text.encode("utf-8")
    out = bytearray()
    n = len(raw)
    while True:
        byte = n & 0x7F
        n >>= 7
        if n:
            out.append(byte | 0x80)
        else:
            out.append(byte)
            break
    return bytes(out) + raw


def _place(subject, name, cell=0):
    """A PlaceBuilding Fields order: subject, terrain target (cell), TargetString."""
    flags = 0x80 | 0x01 | 0x40 | 0x04
    return (bytes([0xFF]) + _s("PlaceBuilding") + struct.pack("<h", flags)
            + struct.pack("<I", subject) + bytes([2]) + struct.pack("<i", cell) + bytes([0])
            + _s(name))


def _replay(path, placements, final_tick, interval=3, players=None):
    """Write a replay: placements is [(frame, subject, name)]; players is [dict]."""
    records = []
    for frame, subject, name in sorted(placements):
        pkt = struct.pack("<i", frame) + _place(subject, name)
        records.append(struct.pack("<i", 0) + struct.pack("<i", len(pkt)) + pkt)
    trailer = "NetFrameInterval: {}\nFinalGameTick: {}\n".format(interval, final_tick)
    for i, p in enumerate(players or [], start=1):
        trailer += "Player@{}:\n".format(i)
        for key in ("ClientIndex", "Name", "IsBot", "BotType", "FactionId", "Team"):
            if key in p:
                trailer += "\t{}: {}\n".format(key, p[key])
    body = b"".join(records) + struct.pack("<i", -1) + struct.pack("<i", 1) + trailer.encode("utf-8")
    with open(path, "wb") as f:
        f.write(body)


RULES_YAML = """\
^BaseBuilding:
	Buildable:
		Queue: Building
^DefenceBase:
	Buildable:
		Queue: Defence
td_gdi_barracks:
	Inherits: ^BaseBuilding
td_gdi_radardome:
	Inherits: ^BaseBuilding
td_gdi_guardtower:
	Inherits: ^DefenceBase
ra1_allies_barracks:
	Inherits: ^BaseBuilding
ra1_allies_gunturret:
	Inherits: ^DefenceBase
ra1_allies_radardome:
	Inherits: ^BaseBuilding
"""

HARD = [{"ClientIndex": 1, "Name": "bot_a", "IsBot": True, "BotType": "hard", "FactionId": "td_gdi", "Team": 1},
        {"ClientIndex": 2, "Name": "bot_b", "IsBot": True, "BotType": "hard", "FactionId": "ra1_allies", "Team": 2}]


class RulesTests(unittest.TestCase):
    def test_defence_and_building_queues_follow_inheritance(self):
        with tempfile.TemporaryDirectory() as tmp:
            Path(tmp, "rules.yaml").write_text(RULES_YAML, encoding="utf-8")
            defences, buildings = bc.load_rules(tmp)
        self.assertEqual(defences, {"td_gdi_guardtower", "ra1_allies_gunturret"})
        self.assertEqual(buildings, {"td_gdi_barracks", "td_gdi_radardome", "ra1_allies_barracks",
                                     "ra1_allies_radardome"})

    def test_faction_prefix(self):
        self.assertEqual(bc.faction_of("td_gdi_barracks"), "td_gdi")
        self.assertEqual(bc.faction_of("ra1_soviets_orerefinery"), "ra1_soviets")
        self.assertEqual(bc.faction_of("ixian_windtrap"), "ixian")
        self.assertEqual(bc.faction_of("ra1_powerplant"), "ra1")


class SyntheticGameTests(unittest.TestCase):
    def setUp(self):
        self.tmp = tempfile.TemporaryDirectory()
        Path(self.tmp.name, "rules.yaml").write_text(RULES_YAML, encoding="utf-8")
        self.rules = self.tmp.name

    def tearDown(self):
        self.tmp.cleanup()

    def _run(self, placements, final_tick, players=None):
        path = os.path.join(self.tmp.name, "game.orarep")
        _replay(path, placements, final_tick, players=players if players is not None else HARD)
        report = bc.analyse(path, self.rules)
        return bc.evaluate(report)

    def test_seat_with_no_defence_holds(self):
        status, reasons = self._run([(100, 1, "td_gdi_barracks"), (500, 1, "td_gdi_guardtower"),
                                     (900, 1, "td_gdi_radardome"),
                                     (100, 2, "ra1_allies_barracks"), (900, 2, "ra1_allies_radardome")],
                                    final_tick=13000)
        self.assertEqual(status, "HOLD")
        self.assertTrue(any("seat 2 (ra1_allies) placed 0 defences" in r for r in reasons), reasons)

    def test_complete_seats_pass(self):
        status, reasons = self._run([(100, 1, "td_gdi_barracks"), (500, 1, "td_gdi_guardtower"),
                                     (2000, 1, "td_gdi_radardome"),
                                     (100, 2, "ra1_allies_barracks"), (500, 2, "ra1_allies_gunturret"),
                                     (2000, 2, "ra1_allies_radardome")],
                                    final_tick=13000)
        self.assertEqual((status, reasons), ("PASS", []))

    def test_tier2_after_deadline_holds(self):
        # frame 4000 * 3 = tick 12000 > 9000: defences are present, tier-2 is late.
        status, reasons = self._run([(500, 1, "td_gdi_guardtower"), (4000, 1, "td_gdi_radardome"),
                                     (500, 2, "ra1_allies_gunturret"), (4000, 2, "ra1_allies_radardome")],
                                    final_tick=13000)
        self.assertEqual(status, "HOLD")
        self.assertTrue(all("no tier-2 building by tick 9000" in r for r in reasons), reasons)
        self.assertEqual(len(reasons), 2)

    def test_short_game_is_not_eligible(self):
        status, reasons = self._run([(100, 1, "td_gdi_barracks")], final_tick=5000)
        self.assertEqual((status, reasons), ("NOT_ELIGIBLE", []))

    def test_non_hard_bots_are_not_eligible(self):
        players = [dict(p, BotType="brutal") for p in HARD]
        status, reasons = self._run([(100, 1, "td_gdi_barracks")], final_tick=13000, players=players)
        self.assertEqual((status, reasons), ("NOT_ELIGIBLE", []))

    def test_frames_convert_to_world_ticks(self):
        path = os.path.join(self.tmp.name, "tick.orarep")
        _replay(path, [(1000, 1, "td_gdi_guardtower")], final_tick=13000, interval=3, players=HARD)
        report = bc.analyse(path, self.rules)
        self.assertEqual(report["seats"][0]["first_defence_tick"], 3000)
        self.assertEqual(report["last_tick"], 13000)


@unittest.skipUnless(os.environ.get("BUILD_COVERAGE_P4ON"), "set BUILD_COVERAGE_P4ON to the known-bad replay")
class KnownBadP4onTests(unittest.TestCase):
    def test_p4on_holds_with_zero_defences(self):
        report = bc.analyse(os.environ["BUILD_COVERAGE_P4ON"], RULES_ROOT)
        status, reasons = bc.evaluate(report)
        self.assertEqual(status, "HOLD", reasons)
        self.assertTrue(all(s["defences"] == 0 for s in report["seats"]))


@unittest.skipUnless(os.environ.get("BUILD_COVERAGE_C29"), "set BUILD_COVERAGE_C29 to the known-good C29 replay")
class KnownGoodC29Tests(unittest.TestCase):
    def test_c29_passes(self):
        report = bc.analyse(os.environ["BUILD_COVERAGE_C29"], RULES_ROOT)
        status, reasons = bc.evaluate(report)
        self.assertEqual(status, "PASS", reasons)


if __name__ == "__main__":
    unittest.main()
