"""Tests for tools/ai/build_coverage.py (BUILD-COVERAGE-GUARD-01).

Synthetic replays are written in the .orarep layout that order_trace.py documents, with a GameUid
header packet, and the bot seats in Logs/cameo-ai-matches.jsonl beside the replay (the replay
trailer of a DEV recording lists only the human). The HOLD, PASS and calibration rules therefore run
without game files. Three real-replay checks run only when their environment variables are set:
  BUILD_COVERAGE_P4ON  known-bad 0d062ae83 game: must HOLD (0 defences on every seat).
  BUILD_COVERAGE_C29   known-good C29 game (PB01-C29-s1-M): must PASS with the calibrated thresholds.
"""
import contextlib
import importlib.util
import io
import json
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
GAME_UID = "11111111-2222-3333-4444-555555555555"
OTHER_UID = "66666666-7777-8888-9999-000000000000"
SEATS = [("seat_1", "hard", "td_gdi", 1), ("seat_2", "hard", "ra1_allies", 2)]
COMPLETE = [(100, 1, "td_gdi_barracks"), (500, 1, "td_gdi_guardtower"), (2000, 1, "td_gdi_radardome"),
            (100, 2, "ra1_allies_barracks"), (500, 2, "ra1_allies_gunturret"), (2000, 2, "ra1_allies_radardome")]


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


def _record(pkt):
    return struct.pack("<i", 0) + struct.pack("<i", len(pkt)) + pkt


def _replay(path, placements, final_tick, interval=3, interval_where="header", game_uid=GAME_UID):
    """Write a replay. placements is [(frame, subject, name)]. interval_where is "header", "trailer"
    or None (no NetFrameInterval anywhere, so the reader must assume one)."""
    head = "GameUid: {}\n".format(game_uid)
    if interval_where == "header":
        head += "NetFrameInterval: {}\n".format(interval)
    records = [_record(struct.pack("<i", 0) + head.encode("utf-8"))]  # frame-0 header packet
    for frame, subject, name in sorted(placements):
        records.append(_record(struct.pack("<i", frame) + _place(subject, name)))
    trailer = "FinalGameTick: {}\n".format(final_tick)
    if interval_where == "trailer":
        trailer += "NetFrameInterval: {}\n".format(interval)
    trailer += "Player@1:\n\tClientIndex: 0\n\tName: Commander\n\tIsBot: False\n\tFactionId: td_gdi\n\tTeam: 0\n"
    body = b"".join(records) + struct.pack("<i", -1) + struct.pack("<i", 1) + trailer.encode("utf-8")
    with open(path, "wb") as f:
        f.write(body)


def _matches(game_dir, seats, game_uid=GAME_UID):
    """Write Logs/cameo-ai-matches.jsonl (schema 3). seats is [(seat, bot_type, faction, team)]."""
    logs = os.path.join(game_dir, "Logs")
    os.makedirs(logs, exist_ok=True)
    lines = []
    for seat, bot, faction, team in seats:
        rec = {"schema": 3, "game_uid": game_uid,
               "player": {"seat": seat, "bot_type": bot, "faction": faction, "team": team}}
        lines.append(json.dumps(rec))
    with open(os.path.join(logs, "cameo-ai-matches.jsonl"), "w", encoding="utf-8") as f:
        f.write("\n".join(lines) + "\n")


RULES_YAML = """\
^BaseBuilding:
	Buildable:
		Queue: Building
^DefenceBase:
	Buildable:
		Queue: Defence
^SuperTemplate:
	Buildable:
		Queue: Defence
td_gdi_barracks:
	Inherits: ^BaseBuilding
td_gdi_radardome:
	Inherits: ^BaseBuilding
td_gdi_advancedcommunicationscenter:
	Inherits: ^BaseBuilding
	Inherits@SW: ^SuperTemplate
td_gdi_guardtower:
	Inherits: ^DefenceBase
td_gdi_concretewall:
	Inherits: ^DefenceBase
td_gdi_bunker.v2:
	Inherits: ^DefenceBase
ra1_allies_barracks:
	Inherits: ^BaseBuilding
ra1_allies_radardome:
	Inherits: ^BaseBuilding
ra1_allies_gunturret:
	Inherits: ^DefenceBase
ra1_allies_aagun:
	Inherits: ^DefenceBase
ra1_soviets_barracks:
	Inherits: ^BaseBuilding
ra1_soviets_missilesilo:
	Inherits: ^BaseBuilding
	Inherits@SW: ^SuperTemplate
ra1_oresilo:
	Buildable:
		Queue: Defence, RADefence
"""


class RulesTests(unittest.TestCase):
    def setUp(self):
        self.tmp = tempfile.TemporaryDirectory()
        Path(self.tmp.name, "rules.yaml").write_text(RULES_YAML, encoding="utf-8")

    def tearDown(self):
        self.tmp.cleanup()

    def test_defence_and_building_queues_follow_inheritance(self):
        defences, buildings = bc.load_rules(self.tmp.name)
        self.assertEqual(defences, {"td_gdi_guardtower", "td_gdi_concretewall", "td_gdi_bunker.v2",
                                    "ra1_allies_gunturret", "ra1_allies_aagun"})
        self.assertEqual(buildings, {"td_gdi_barracks", "td_gdi_radardome",
                                     "td_gdi_advancedcommunicationscenter", "ra1_allies_barracks",
                                     "ra1_allies_radardome", "ra1_soviets_barracks",
                                     "ra1_soviets_missilesilo", "ra1_oresilo"})

    def test_superweapon_silo_and_comm_center_are_buildings_not_defences(self):
        defences, buildings = bc.load_rules(self.tmp.name)
        self.assertNotIn("ra1_soviets_missilesilo", defences)
        self.assertNotIn("ra1_oresilo", defences)
        self.assertNotIn("td_gdi_advancedcommunicationscenter", defences)
        self.assertIn("ra1_oresilo", buildings)

    def test_td_power_plant_named_nuke_counts_as_power(self):
        row = bc._seat_row(4, "td_nod", [(100, "nuke"), (200, "td_nod_tiberiumrefinery")], set(), set())
        self.assertEqual(row["power"], 1)

    def test_tier2_markers_exclude_production(self):
        self.assertFalse(bc._has("td_nod_handofnod", bc.TIER2_MARKERS))
        self.assertFalse(bc._has("td_nod_airstrip", bc.TIER2_MARKERS))
        self.assertTrue(bc._has("td_gdi_radardome", bc.TIER2_MARKERS))
        self.assertTrue(bc._has("ra1_allies_barracks", bc.PRODUCTION_MARKERS))

    def test_defence_category_by_name(self):
        self.assertEqual(bc._category("td_gdi_concretewall"), "wall_gate")
        self.assertEqual(bc._category("ra1_allies_aagun"), "aa")
        self.assertEqual(bc._category("td_gdi_guardtower"), "defence")

    def test_faction_prefix(self):
        self.assertEqual(bc.faction_of("td_gdi_barracks"), "td_gdi")
        self.assertEqual(bc.faction_of("ra1_soviets_orerefinery"), "ra1_soviets")
        self.assertEqual(bc.faction_of("ixian_windtrap"), "ixian")
        self.assertEqual(bc.faction_of("ra1_powerplant"), "ra1")

    def test_scoped_loader_reads_only_what_mod_yaml_loads(self):
        with tempfile.TemporaryDirectory() as mod:
            Path(mod, "rules").mkdir()
            Path(mod, "ContentPacks", "A").mkdir(parents=True)
            Path(mod, "mod.yaml").write_text(
                "Rules:\n\tcameo|rules/in.yaml\n\t# cameo|rules/commented.yaml\n"
                "Include: ContentPacks/A/content.yaml\n", encoding="utf-8")
            Path(mod, "rules", "in.yaml").write_text(
                "^BaseBuilding:\n\tBuildable:\n\t\tQueue: Building\n"
                "^DefenceBase:\n\tBuildable:\n\t\tQueue: Defence\n"
                "td_gdi_guardtower:\n\tInherits: ^DefenceBase\n", encoding="utf-8")
            Path(mod, "rules", "commented.yaml").write_text(
                "td_gdi_hidden_turret:\n\tInherits: ^DefenceBase\n", encoding="utf-8")
            Path(mod, "ContentPacks", "A", "content.yaml").write_text(
                "Rules:\n\tContentPacks|A/loaded.yaml\n", encoding="utf-8")
            Path(mod, "ContentPacks", "A", "loaded.yaml").write_text(
                "td_gdi_barracks:\n\tInherits: ^BaseBuilding\n", encoding="utf-8")
            info = {}
            defences, buildings = bc.load_rules(mod, info)
        self.assertEqual(info["source"], "mod.yaml")
        self.assertEqual(info["files"], 2)
        self.assertEqual(info["missing"], [])
        self.assertEqual(defences, {"td_gdi_guardtower"})
        self.assertEqual(buildings, {"td_gdi_barracks"})

    def test_round_up_to_1000(self):
        self.assertEqual(bc._round_up(1.25 * 23571), 30000)   # 29463.75 -> 30000
        self.assertEqual(bc._round_up(30000), 30000)
        self.assertEqual(bc._round_up(30000.0000001), 30000)  # float noise cannot add a step
        self.assertEqual(bc._round_up(30001), 31000)


class SyntheticGameTests(unittest.TestCase):
    def setUp(self):
        self.tmp = tempfile.TemporaryDirectory()
        Path(self.tmp.name, "rules.yaml").write_text(RULES_YAML, encoding="utf-8")
        self.rules = self.tmp.name

    def tearDown(self):
        self.tmp.cleanup()

    def _game(self, placements, final_tick, seats=SEATS, interval=3, interval_where="header",
              name="game", game_uid=GAME_UID):
        game = os.path.join(self.tmp.name, name)
        os.makedirs(game, exist_ok=True)
        path = os.path.join(game, "game.orarep")
        _replay(path, placements, final_tick, interval, interval_where, game_uid)
        if seats is not None:
            _matches(game, seats, game_uid)
        return bc.analyse(path, self.rules)

    def _run(self, placements, final_tick, seats=SEATS, tier2=None, cov=None):
        return bc.evaluate(self._game(placements, final_tick, seats), tier2, cov)

    def test_seat_with_no_defence_holds(self):
        status, reasons, advisories = self._run(
            [(100, 1, "td_gdi_barracks"), (500, 1, "td_gdi_guardtower"), (900, 1, "td_gdi_radardome"),
             (100, 2, "ra1_allies_barracks"), (900, 2, "ra1_allies_radardome")], final_tick=13000)
        self.assertEqual(status, "HOLD")
        self.assertEqual(reasons, ["seat 2 (ra1_allies) placed 0 defences"])
        self.assertEqual(advisories, [])

    def test_complete_seats_pass(self):
        self.assertEqual(self._run(COMPLETE, final_tick=13000), ("PASS", [], []))

    def test_walls_do_not_count_as_defences(self):
        report = self._game([(500, 1, "td_gdi_concretewall"), (100, 1, "td_gdi_barracks"),
                             (900, 1, "td_gdi_radardome")] + [p for p in COMPLETE if p[1] == 2],
                            final_tick=13000)
        status, reasons, _adv = bc.evaluate(report)
        self.assertEqual(status, "HOLD")
        self.assertEqual(reasons, ["seat 1 (td_gdi) placed 0 defences"])
        self.assertEqual(report["seats"][0]["defence_split"], {"defence": 0, "aa": 0, "wall_gate": 1})

    def test_anti_air_counts_as_defence(self):
        status, reasons, _adv = self._run(
            [(100, 1, "td_gdi_barracks"), (500, 1, "td_gdi_guardtower"), (900, 1, "td_gdi_radardome"),
             (100, 2, "ra1_allies_barracks"), (500, 2, "ra1_allies_aagun"), (900, 2, "ra1_allies_radardome")],
            final_tick=13000)
        self.assertEqual((status, reasons), ("PASS", []))
        report = self._game([(100, 2, "ra1_allies_barracks"), (500, 2, "ra1_allies_aagun")], 13000,
                            name="aa")
        self.assertEqual(report["seats"][0]["defence_split"]["aa"], 1)
        self.assertEqual(report["seats"][0]["defences"], 1)

    def test_superweapon_silo_is_not_a_defence(self):
        seats = [("seat_1", "hard", "td_gdi", 1), ("seat_2", "hard", "ra1_soviets", 2)]
        status, reasons, _adv = self._run(
            [(100, 1, "td_gdi_barracks"), (500, 1, "td_gdi_guardtower"), (900, 1, "td_gdi_radardome"),
             (100, 2, "ra1_soviets_barracks"), (500, 2, "ra1_soviets_missilesilo"), (900, 2, "ra1_oresilo")],
            final_tick=13000, seats=seats)
        self.assertEqual(status, "HOLD")
        self.assertEqual(reasons, ["seat 2 (ra1_soviets) placed 0 defences"])

    def test_tier2_deadline_holds_in_games_of_30000_ticks(self):
        status, reasons, advisories = self._run(
            [(500, 1, "td_gdi_guardtower"), (11000, 1, "td_gdi_radardome"),
             (500, 2, "ra1_allies_gunturret"), (11000, 2, "ra1_allies_radardome")],
            final_tick=40000, tier2=30000)
        self.assertEqual(status, "HOLD")
        self.assertEqual(len(reasons), 2)
        self.assertTrue(all("no tier-2 building by tick 30000" in r for r in reasons), reasons)
        self.assertEqual(advisories, [])

    def test_tier2_miss_in_a_short_game_is_an_advisory(self):
        status, reasons, advisories = self._run(
            [(500, 1, "td_gdi_guardtower"), (4000, 1, "td_gdi_radardome"),
             (500, 2, "ra1_allies_gunturret"), (4000, 2, "ra1_allies_radardome")],
            final_tick=20000, tier2=9000)
        self.assertEqual((status, reasons), ("PASS", []))
        self.assertEqual(len(advisories), 2)

    def test_coverage_minimum_holds(self):
        status, reasons, _adv = self._run(
            [(100, 1, "td_gdi_barracks"), (500, 1, "td_gdi_guardtower"),
             (100, 2, "ra1_allies_barracks"), (500, 2, "ra1_allies_gunturret"), (900, 2, "ra1_allies_radardome")],
            final_tick=13000, cov=0.5)
        self.assertEqual(status, "HOLD")
        self.assertEqual(reasons, ["seat 1 (td_gdi) coverage 0.3333 below 0.5"])

    def test_short_game_is_not_eligible(self):
        self.assertEqual(self._run([(100, 1, "td_gdi_barracks")], final_tick=5000), ("NOT_ELIGIBLE", [], []))

    def test_brutal_is_hard_plus(self):
        seats = [("seat_1", "brutal", "td_gdi", 1), ("seat_2", "brutal", "ra1_allies", 2)]
        report = self._game(COMPLETE, 13000, seats=seats)
        self.assertEqual(report["eligibility"], "ELIGIBLE")
        self.assertEqual(bc.evaluate(report), ("PASS", [], []))

    def test_easy_bots_are_not_eligible(self):
        seats = [("seat_1", "hard", "td_gdi", 1), ("seat_2", "easy", "ra1_allies", 2)]
        report = self._game(COMPLETE, 13000, seats=seats)
        self.assertEqual(report["eligibility"], "NOT_ELIGIBLE")
        self.assertEqual(report["eligibility_reason"], "bot types outside Hard+: easy")

    def test_missing_match_metadata_is_not_eligible(self):
        report = self._game(COMPLETE, 13000, seats=None)
        self.assertEqual(report["eligibility"], "NOT_ELIGIBLE")
        self.assertTrue(report["eligibility_reason"].startswith("no match metadata"), report["eligibility_reason"])
        self.assertEqual(bc.evaluate(report), ("NOT_ELIGIBLE", [], []))

    def test_zero_placement_metadata_seat_counts_as_zero_defence(self):
        seats = SEATS + [("seat_3", "hard", "td_nod", 3)]
        report = self._game(COMPLETE, 13000, seats=seats)
        status, reasons, _adv = bc.evaluate(report)
        self.assertEqual(status, "HOLD")
        self.assertIn("no-placement (td_nod) placed 0 defences", reasons[0])
        self.assertEqual((report["metadata_seats"], report["replay_seats"]), (3, 2))
        self.assertTrue(report["reconciliation"])

    def test_frames_convert_to_world_ticks(self):
        report = self._game([(1000, 1, "td_gdi_guardtower")], 13000)
        self.assertEqual(report["seats"][0]["first_defence_tick"], 3000)
        self.assertEqual(report["last_tick"], 13000)
        self.assertEqual(report["interval_source"], "header")

    def test_interval_is_read_from_the_trailer_when_the_header_lacks_it(self):
        report = self._game([(1000, 1, "td_gdi_guardtower")], 13000, interval=2, interval_where="trailer")
        self.assertEqual(report["interval_source"], "trailer")
        self.assertEqual(report["seats"][0]["first_defence_tick"], 2000)

    def test_interval_is_assumed_when_absent(self):
        report = self._game([(1000, 1, "td_gdi_guardtower")], 13000, interval_where=None)
        self.assertEqual(report["interval_source"], "assumed")
        self.assertEqual(report["net_frame_interval"], bc.DEFAULT_NET_INTERVAL)
        self.assertEqual(report["seats"][0]["first_defence_tick"], 3000)

    def test_cli_exit_codes(self):
        game = os.path.join(self.tmp.name, "cli")
        os.makedirs(game, exist_ok=True)
        path = os.path.join(game, "game.orarep")
        _replay(path, COMPLETE, 13000)
        _matches(game, SEATS)
        with contextlib.redirect_stdout(io.StringIO()):
            self.assertEqual(bc.main([path, "--rules-root", self.rules]), 0)
        _replay(path, [(100, 1, "td_gdi_barracks")], 13000)
        with contextlib.redirect_stdout(io.StringIO()):
            self.assertEqual(bc.main([path, "--rules-root", self.rules]), 1)


class CalibrationTests(unittest.TestCase):
    def setUp(self):
        self.tmp = tempfile.TemporaryDirectory()
        Path(self.tmp.name, "rules.yaml").write_text(RULES_YAML, encoding="utf-8")
        self.rules = self.tmp.name

    def tearDown(self):
        self.tmp.cleanup()

    def _good(self, name, uid, td_radar_frame, ra_radar_frame=2000):
        game = os.path.join(self.tmp.name, name)
        os.makedirs(game, exist_ok=True)
        path = os.path.join(game, "game.orarep")
        placements = [(100, 1, "td_gdi_barracks"), (500, 1, "td_gdi_guardtower"),
                      (td_radar_frame, 1, "td_gdi_radardome"),
                      (100, 2, "ra1_allies_barracks"), (500, 2, "ra1_allies_gunturret"),
                      (ra_radar_frame, 2, "ra1_allies_radardome")]
        _replay(path, placements, 40000, game_uid=uid)
        _matches(game, SEATS, uid)
        return path

    def test_thresholds_from_factors(self):
        g1 = self._good("g1", GAME_UID, 7000)     # tier-2 at tick 21000 (td), 6000 (ra)
        g2 = self._good("g2", OTHER_UID, 7857)    # tier-2 at tick 23571 (td)
        result = bc.calibrate([g1, g2], self.rules, None, 1.25, 0.8)
        self.assertFalse(result["refused"])
        self.assertEqual(result["distinct_game_uids"], 2)
        self.assertEqual(result["tier2_ticks"]["max"], 23571)
        self.assertEqual(result["thresholds"]["tier2_deadline_tick"], 30000)
        # td seat 1 places 2 of 3 faction buildings: coverage 0.6667 (rounded per seat), the minimum.
        self.assertEqual(result["thresholds"]["coverage_min"], round(0.8 * 0.6667, 4))

    def test_factors_are_never_defaulted(self):
        g1 = self._good("g1", GAME_UID, 7000)
        result = bc.calibrate([g1], self.rules)
        self.assertEqual(result["thresholds"]["tier2_deadline_tick"], None)
        self.assertEqual(result["thresholds"]["coverage_min"], None)

    def test_calibration_refuses_a_non_eligible_input(self):
        game = os.path.join(self.tmp.name, "short")
        os.makedirs(game, exist_ok=True)
        path = os.path.join(game, "game.orarep")
        _replay(path, [(100, 1, "td_gdi_barracks")], 5000)
        _matches(game, SEATS)
        result = bc.calibrate([path], self.rules, None, 1.25, 0.8)
        self.assertTrue(result["refused"])
        self.assertIsNone(result["thresholds"]["tier2_deadline_tick"])


class DefaultGateTests(unittest.TestCase):
    """The command line runs the calibrated tier-2 and coverage checks when no flag is given."""

    def setUp(self):
        self.tmp = tempfile.TemporaryDirectory()
        Path(self.tmp.name, "rules.yaml").write_text(RULES_YAML, encoding="utf-8")

    def tearDown(self):
        self.tmp.cleanup()

    def test_built_in_defaults_are_the_calibrated_values(self):
        self.assertEqual(bc.TIER2_DEADLINE_DEFAULT, 30000)
        self.assertEqual(bc.COVERAGE_MIN_DEFAULT, 0.4)
        args = bc._parser().parse_args(["replay.orarep"])
        self.assertEqual(args.tier2_deadline, 30000)
        self.assertEqual(args.coverage_min, 0.4)

    def test_cli_holds_on_coverage_and_tier2_without_flags(self):
        game = os.path.join(self.tmp.name, "defaults")
        os.makedirs(game, exist_ok=True)
        path = os.path.join(game, "game.orarep")
        # td seat 1: 1 of 3 faction buildings (0.3333 < 0.4) and no tier-2 by tick 30000 (40000-tick game).
        placements = [(100, 1, "td_gdi_barracks"), (500, 1, "td_gdi_guardtower"),
                      (100, 2, "ra1_allies_barracks"), (500, 2, "ra1_allies_gunturret"),
                      (900, 2, "ra1_allies_radardome")]
        _replay(path, placements, 40000)
        _matches(game, SEATS)
        buf = io.StringIO()
        with contextlib.redirect_stdout(buf):
            code = bc.main([path, "--rules-root", self.tmp.name])
        out = json.loads(buf.getvalue())
        self.assertEqual(code, 1)
        self.assertEqual(out["status"], "HOLD")
        self.assertEqual(out["thresholds"]["tier2_deadline_tick"], 30000)
        self.assertEqual(out["thresholds"]["coverage_min"], 0.4)
        self.assertIn("seat 1 (td_gdi) coverage 0.3333 below 0.4", out["reasons"])
        self.assertIn("seat 1 (td_gdi) no tier-2 building by tick 30000", out["reasons"])


@unittest.skipUnless(os.environ.get("BUILD_COVERAGE_P4ON"), "set BUILD_COVERAGE_P4ON to the known-bad replay")
class KnownBadP4onTests(unittest.TestCase):
    def test_p4on_holds_with_zero_defences(self):
        report = bc.analyse(os.environ["BUILD_COVERAGE_P4ON"], RULES_ROOT)
        status, reasons, _adv = bc.evaluate(report)
        self.assertEqual(report["eligibility"], "ELIGIBLE", report["eligibility_reason"])
        self.assertEqual(status, "HOLD", reasons)
        self.assertTrue(all(s["defences"] == 0 for s in report["seats"]))


@unittest.skipUnless(os.environ.get("BUILD_COVERAGE_C29"), "set BUILD_COVERAGE_C29 to the known-good C29 replay")
class KnownGoodC29Tests(unittest.TestCase):
    def test_c29_passes_with_calibrated_thresholds(self):
        report = bc.analyse(os.environ["BUILD_COVERAGE_C29"], RULES_ROOT)
        self.assertEqual(report["eligibility"], "ELIGIBLE", report["eligibility_reason"])
        status, reasons, advisories = bc.evaluate(report, tier2_deadline=30000, coverage_min=0.4)
        self.assertEqual((status, reasons, advisories), ("PASS", [], []))
        self.assertTrue(all(s["defences"] > 0 for s in report["seats"]))


if __name__ == "__main__":
    unittest.main()
