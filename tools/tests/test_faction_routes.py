"""Route by faction, not by name — the ruling, and the three ways it silently does nothing.

PRIOR ART: `test_lineage_dedup.py` covers the de-duplication rulings and the exact failure this
guards against one layer up — a ruled label that does not match the pool's label, so the ruling
never fires. `test_assign_references.py` covers the matching law itself. Neither knows about
faction routing, which was added on 2026-09-04 after the maintainer rejected the scout sheet:
*"instead of trying to match something completely unrelated we now try to map reference faction to
our cameo factions."*

The three silent failures, one test each:
  1. a route whose SOURCE label was collapsed by a lineage ruling — reaches zero rows;
  2. a route whose FACTION TOKEN the source does not use (`yuri` where RV writes `psicorps`);
  3. a Cameo faction id that is not the tree's (`redalert2mod_asianalliance`), so nothing routes.
"""

from __future__ import annotations

import pathlib
import sys
import unittest

ROOT = pathlib.Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "tools" / "balance"))
sys.path.insert(0, str(ROOT / "tools" / "audit"))
import faction_routes as fr          # noqa: E402
import reference_distribution as rd  # noqa: E402


class TestFactionOf(unittest.TestCase):
    def test_longest_prefix_wins(self):
        # `ra2` is not a faction; `ra2_allies` is. A shortest-first scan would resolve the GI to
        # a faction that does not exist and route it nowhere.
        self.assertEqual(fr.faction_of("ra2_allies_gi"), "ra2_allies")
        self.assertEqual(fr.faction_of("ts_gdi_discthrower"), "ts_gdi")
        self.assertEqual(fr.faction_of("ra1_soviets_heavytank"), "ra1_soviets")

    def test_shared_pack_actors_have_no_faction(self):
        # `ra2_*` actors that are neither allies nor soviets are the RA2 SHARED pack.
        self.assertIsNone(fr.faction_of("ra2_sharedbarrel"))
        self.assertIsNone(fr.faction_of(""))
        self.assertIsNone(fr.faction_of("wc2_orcs_grunt"))

    def test_exact_faction_id_resolves(self):
        self.assertEqual(fr.faction_of("cabal"), "cabal")


class TestPeerFactions(unittest.TestCase):
    def test_slash_separated(self):
        self.assertEqual(fr.peer_factions({"faction": "cab/gdi/mut"}), {"cab", "gdi", "mut"})

    def test_untagged_forms_are_empty(self):
        for raw in ("", "—", "-", "?", "   "):
            self.assertEqual(fr.peer_factions({"faction": raw}), frozenset(), raw)
        self.assertEqual(fr.peer_factions({}), frozenset())

    def test_case_is_normalised(self):
        self.assertEqual(fr.peer_factions({"faction": "GDI"}), {"gdi"})


class TestAllows(unittest.TestCase):
    def test_admits_only_the_routed_source_and_token(self):
        row = {"source": "Shattered Paradise", "faction": "mut"}
        self.assertTrue(fr.allows("forgotten", row))
        self.assertFalse(fr.allows("ts_gdi", row))
        self.assertFalse(fr.allows("cabal", row))

    def test_untagged_rows_are_never_admitted(self):
        # Half the corpus carries no faction. Admitting those "just in case" would make one
        # untagged Combined Arms row visible to every Cameo faction at once — exactly the
        # cross-faction matching the ruling removes.
        self.assertFalse(fr.allows("td_gdi", {"source": "Combined Arms", "faction": "—"}))

    def test_a_multi_faction_row_is_admitted_by_each_of_its_factions(self):
        row = {"source": "OpenRA Tiberian Dawn", "faction": "gdi/nod"}
        self.assertTrue(fr.allows("td_gdi", row))
        self.assertTrue(fr.allows("td_nod", row))

    def test_the_mirror_merge_is_one_route_with_two_tokens(self):
        # OpenHV's `sc` and `yi` are ONE voice of twice the units (maintainer 2026-09-04), which
        # is two tokens on one source — not two routes, which would vote twice.
        self.assertEqual(len(fr.routes_for("steelconsortium")), 1)
        src, toks = fr.routes_for("steelconsortium")[0]
        self.assertEqual(src, "OpenHV")
        self.assertEqual(toks, {"sc", "yi"})
        for tok in ("sc", "yi"):
            self.assertTrue(fr.allows("steelconsortium", {"source": "OpenHV", "faction": tok}))


class TestRulingConsistency(unittest.TestCase):
    def test_routed_and_formula_only_are_disjoint(self):
        self.assertEqual(set(fr.ROUTES) & set(fr.UNROUTED), set())

    def test_every_routed_faction_is_declared(self):
        self.assertEqual(set(fr.ROUTES) - set(fr.CAMEO_FACTIONS), set())

    def test_open_second_game_only_names_routed_factions(self):
        self.assertEqual(set(fr.OPEN_SECOND_GAME) - set(fr.ROUTES), set())

    def test_a_faction_with_one_route_is_declared_open_or_unrouted(self):
        # A single-source faction is a ruling gap, not a finished mapping. The matrix says every
        # Cameo faction wants two reference factions from DIFFERENT games; anything on one must
        # say so out loud rather than looking complete.
        for fac, routes in fr.ROUTES.items():
            if len(routes) == 1:
                self.assertIn(fac, fr.OPEN_SECOND_GAME,
                              f"{fac} has one route and is not listed as OPEN")


class TestAgainstTheCorpus(unittest.TestCase):
    """The tests that would have caught the `RA2/YR` class of bug — a ruling that does nothing."""

    @classmethod
    def setUpClass(cls):
        cls.rows = rd.peer_rows()

    def test_every_ruled_route_resolves(self):
        self.assertEqual(fr.validate(self.rows), [])

    def test_no_route_reaches_zero_rows(self):
        for fac, routes in fr.ROUTES.items():
            for src, toks in routes:
                n = sum(1 for r in self.rows if r.get("source") == src
                        and (fr.peer_factions(r) & frozenset(toks)))
                self.assertGreater(n, 0, f"{fac}: {src} {toks} reaches no rows")

    def test_validate_catches_a_collapsed_source_label(self):
        # `Yuri's Revenge on OpenRA` is collapsed into Romanov's Vengeance by the lineage ruling,
        # so routing to it is a route to nothing.
        saved = dict(fr.ROUTES)
        try:
            fr.ROUTES["yuri"] = (("Yuri's Revenge on OpenRA", ("yuri",)),)
            problems = fr.validate(self.rows)
            self.assertTrue(any("not in the de-duplicated corpus" in p for p in problems),
                            problems)
        finally:
            fr.ROUTES.clear()
            fr.ROUTES.update(saved)

    def test_validate_catches_a_wrong_faction_token(self):
        # RV spells its Yuri faction `psicorps`. Routing to `yuri` silently matches nothing.
        saved = dict(fr.ROUTES)
        try:
            fr.ROUTES["yuri"] = (("Romanov's Vengeance", ("yuri",)),)
            problems = fr.validate(self.rows)
            self.assertTrue(any("has no faction token" in p for p in problems), problems)
        finally:
            fr.ROUTES.clear()
            fr.ROUTES.update(saved)

    def test_the_faction_column_survives_the_reader(self):
        # It was in the document and dropped on read, which is indistinguishable from never
        # having been extracted — the same bug `cost` had.
        tagged = sum(1 for r in self.rows if fr.peer_factions(r))
        self.assertGreater(tagged, 1000, "peer_rows() is not carrying the faction column")


if __name__ == "__main__":
    unittest.main()
