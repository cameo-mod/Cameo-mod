"""run_ai_match_batch.py: matchup matrix, variant patching, and record checks.

The harness is the Stage D data tap — its bugs would silently poison the
match-log corpus the aggregator learns from, so the pure pieces (matrix,
map patching, record slicing) get unit tests; the engine launch itself is
smoke-tested separately with a real match.
"""
import pathlib
import shutil
import sys
import tempfile
import unittest

ROOT = pathlib.Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "tools/ai"))
sys.path.insert(0, str(ROOT / "tools/audit"))

import run_ai_match_batch as batch
from miniyaml import Ruleset

TEMPLATE = ROOT / "mods" / "cameo" / "maps" / "ai_duel_gate_20260928"


class MatchupMatrixTests(unittest.TestCase):
    def test_unordered_pairs_with_mirrors(self):
        matchups = batch.build_matchups(["td_gdi", "td_nod"], "hard", "hard", repeats=1)
        pairs = {(m["side_a"]["faction"], m["side_b"]["faction"]) for m in matchups}
        self.assertEqual(pairs, {("td_gdi", "td_gdi"), ("td_gdi", "td_nod"), ("td_nod", "td_nod")})

    def test_repeats_alternate_spawn_sides(self):
        matchups = batch.build_matchups(["td_gdi", "td_nod"], "hard", "hard", repeats=2)
        sides = [
            (m["side_a"]["faction"], m["side_b"]["faction"])
            for m in matchups
            if {m["side_a"]["faction"], m["side_b"]["faction"]} == {"td_gdi", "td_nod"}
        ]
        self.assertEqual(sides, [("td_gdi", "td_nod"), ("td_nod", "td_gdi")])

    def test_difficulty_axes_stay_separate(self):
        matchups = batch.build_matchups(["td_gdi"], "easy", "brutal", repeats=1)
        m = matchups[0]
        self.assertEqual(m["side_a"]["bot"], "easy")
        self.assertEqual(m["side_b"]["bot"], "brutal")


class VariantPatchTests(unittest.TestCase):
    def test_patched_variant_resolves_both_bots(self):
        tmp = pathlib.Path(tempfile.mkdtemp(prefix="ai_duel_test_"))
        try:
            dest = tmp / "variant"
            matchup = {
                "side_a": {"faction": "terran", "bot": "medium"},
                "side_b": {"faction": "td_nod", "bot": "brutal"},
            }
            batch.write_variant(TEMPLATE, dest, matchup, time_limit=20)

            text = (dest / "map.yaml").read_text(encoding="utf-8")
            self.assertIn("Faction: terran", text)
            self.assertIn("Faction: td_nod", text)
            self.assertIn("Bot: medium", text)
            self.assertIn("Bot: brutal", text)
            # Enemies stay cross-wired regardless of the patched values.
            self.assertIn("Enemies: BotB", text)
            self.assertIn("Enemies: BotA", text)
            # Starting forces are pre-placed for the map-side bots.
            self.assertIn("BotA_base:", text)
            self.assertIn("BotB_base:", text)
            self.assertIn("Owner: BotA", text)
            self.assertIn("Owner: BotB", text)
            self.assertNotIn("AI_DUEL_BOT_UNITS", text)
            # Identical content shares one Map.ComputeUID — MapCache then keeps
            # a single preview and Launch.Map's name lookup loses twin dirs.
            # The per-dir salt line keeps every variant's uid unique.
            self.assertIn(f"# ai-match-batch variant: {dest.name}", text)

            rules = (dest / "rules.yaml").read_text(encoding="utf-8")
            self.assertIn("TimeLimitDefault: 20", rules)
            self.assertIn("TimeLimitLocked: True", rules)
        finally:
            shutil.rmtree(tmp, ignore_errors=True)

    def test_patch_rejects_unknown_block(self):
        text = (TEMPLATE / "map.yaml").read_text(encoding="utf-8")
        with self.assertRaises(SystemExit):
            batch.patch_player_block(text, "NotABot", "hard", "td_gdi")

    def test_starting_units_resolve_real_groups(self):
        # The mod ships light-class groups for the TD factions; the resolver
        # must find real actor names, not sentinel placeholders.
        base, supports = batch.starting_unit_group("td_gdi", "light")
        self.assertTrue(base)
        self.assertTrue(supports)

    def test_starting_units_unknown_faction_fails_closed(self):
        self.assertIsNone(batch.starting_unit_group("not_a_faction", "light"))
        with self.assertRaises(SystemExit):
            batch.render_side_actors("BotA", "not_a_faction", (16, 16))


class TemplateMapContractTests(unittest.TestCase):
    def test_template_map_is_well_formed(self):
        text = (TEMPLATE / "map.yaml").read_text(encoding="utf-8")
        self.assertIn("PlayerReference@Referee:", text)
        self.assertIn("NonCombatant: True", text)
        for ref in ("BotA", "BotB"):
            self.assertIn(f"PlayerReference@{ref}:", text)

    def test_bot_slots_are_map_side_bots(self):
        # The whole design rests on this: headless Launch.Map cannot seat bot
        # clients, so duelists are Playable:False map players — client==null
        # keeps IsBot (records) and the writer's IsBot-admitting eligibility
        # lists them in opponents. Playable:True here would mean "unoccupied
        # lobby slot" and no Player would exist at all.
        text = (TEMPLATE / "map.yaml").read_text(encoding="utf-8")
        for ref in ("BotA", "BotB"):
            block = text.split(f"PlayerReference@{ref}:")[1].split("PlayerReference@")[0]
            self.assertIn("Playable: False", block)
            self.assertIn("Bot:", block)
            self.assertIn("HomeLocation:", block)
            self.assertNotIn("NonCombatant", block)

    def test_referee_declares_map_intent_noncombatant(self):
        text = (TEMPLATE / "map.yaml").read_text(encoding="utf-8")
        block = text.split("PlayerReference@Referee:")[1].split("PlayerReference@")[0]
        self.assertIn("NonCombatant: True", block)
        self.assertIn("StartingUnitsClass: empty", block)

    def test_writer_honors_declared_noncombatant(self):
        # Lobby-occupied slots ignore Player.NonCombatant (engine), so the
        # writer must check PlayerReference.NonCombatant or the referee leaks
        # into opponents and every record becomes non-1v1.
        writer = (ROOT / "OpenRA.Mods.Cameo/Traits/AiMatchLogWriter.cs").read_text(encoding="utf-8")
        self.assertIn("PlayerReference.NonCombatant", writer)

    def test_writer_reads_stance_masks_not_post_game_alliance(self):
        # The log is built after the match resolves, when every decided player
        # reports Spectating (WinState != Undefined) and Player.IsAlliedWith
        # short-circuits to ally on non-mission maps — losers then land in
        # "allies" instead of "opponents". Relationships must come from the
        # static stance masks assigned by SetupPlayerMasks.
        writer = (ROOT / "OpenRA.Mods.Cameo/Traits/AiMatchLogWriter.cs").read_text(encoding="utf-8")
        body = writer.split("AppendRelationships(StringBuilder")[1].split("internal static void")[0]
        self.assertIn("AlliedPlayersMask.Overlaps(subject.PlayerMask)", body)
        self.assertNotIn("p.IsAlliedWith(subject)", body)

    def test_template_has_no_lua_or_spawn_dependency(self):
        # Starting units come from SpawnStartingUnits (per-slot class), match
        # end from ConquestVictoryConditions + the locked TimeLimitManager.
        self.assertFalse((TEMPLATE / "test.lua").exists())
        rules = (TEMPLATE / "rules.yaml").read_text(encoding="utf-8")
        self.assertNotIn("LuaScript", rules)
        self.assertIn("MustBeDestroyed:", rules)

    def test_template_rules_lock_a_valid_time_limit(self):
        text = (TEMPLATE / "rules.yaml").read_text(encoding="utf-8")
        import re

        limit = int(re.search(r"TimeLimitDefault: (\d+)", text).group(1))
        self.assertIn(limit, batch.VALID_TIME_LIMITS)
        self.assertIn("TimeLimitLocked: True", text)


class RecordSliceTests(unittest.TestCase):
    def test_appended_records_skip_garbage(self):
        tmp = pathlib.Path(tempfile.mkdtemp(prefix="ai_log_test_"))
        try:
            log = tmp / "cameo-ai-matches.jsonl"
            prefix = '{"schema": 1}\n'
            log.write_text(prefix, encoding="utf-8")
            before = log.stat().st_size
            with log.open("a", encoding="utf-8") as f:
                f.write('{"schema": 2, "player": {"name": "BotA"}}\n')
                f.write("not json\n")
                f.write('{"schema": 2, "player": {"name": "BotB"}}\n')
            records = batch.read_appended_records(log, before)
            self.assertEqual(len(records), 2)
            self.assertEqual(records[0]["player"]["name"], "BotA")
        finally:
            shutil.rmtree(tmp, ignore_errors=True)


if __name__ == "__main__":
    unittest.main()
