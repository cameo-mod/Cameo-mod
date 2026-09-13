"""The per-armament pairing contract: a cannon's reference is never a missile's.

⛔ THE LOAD-BEARING TEST IS `test_agrees_with_the_missile_role_audit`. This module deliberately
reads a WIDER ValidTargets vocabulary than `audit_missile_role_family.weapon_role`, which fails
closed to "custom" on any token outside {Ground, Water, Air} because it drives four LOWER-ONLY
ratchets. A wider set may only ADD answers — the moment it changes one the audit already gives,
the two files are ruling differently on the same maintainer decision and the ratchets are
measuring something else than they were calibrated on.
"""

from __future__ import annotations

import json
import pathlib
import sys
import unittest


ROOT = pathlib.Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "tools" / "audit"))
sys.path.insert(0, str(ROOT / "tools" / "balance"))
sys.path.insert(0, str(ROOT / "tools" / "reference"))

import armament_roles as ar                      # noqa: E402
import miniyaml                                  # noqa: E402
import reference_distribution as rd              # noqa: E402
from audit_missile_role_family import weapon_role  # noqa: E402


PROJECTILE_EVIDENCE = ROOT / "docs" / "reference" / "ini_projectile_role_evidence.json"


class TargetVocabularyTests(unittest.TestCase):
    def test_absent_valid_targets_is_the_engine_default_and_therefore_ground(self):
        # WeaponInfo.cs:116. 451 of the ~900 peer armaments leave it unwritten, so reading
        # "absent" as "unknown" would abstain on half the corpus.
        self.assertEqual(ar.ROLE_GROUND, ar.role_of_targets(None)[0])
        self.assertEqual(ar.ROLE_GROUND, ar.role_of_targets("")[0])

    def test_the_three_domains(self):
        self.assertEqual(ar.ROLE_AIR, ar.role_of_targets("Air, AirSmall")[0])
        self.assertEqual(ar.ROLE_GROUND, ar.role_of_targets("Ground, Water, Trees")[0])
        self.assertEqual(ar.ROLE_BOTH, ar.role_of_targets("Ground, Water, Air")[0])
        # Red Alert spells its mammoth missile without the word "Air" at all.
        self.assertEqual(ar.ROLE_BOTH, ar.role_of_targets("AirborneActor, Infantry")[0])

    def test_invalid_targets_subtract(self):
        self.assertEqual(ar.ROLE_GROUND, ar.role_of_targets("Ground, Water, Air", "Air")[0])

    def test_a_selector_only_weapon_is_special_not_ground(self):
        # A weapon that can only shoot down rockets belongs to no domain. Calling it ground would
        # hand a real cannon an interceptor as its reference.
        self.assertEqual(ar.ROLE_SPECIAL, ar.role_of_targets("Missile")[0])
        # ...but a point-defence laser that ALSO hits ground is a ground weapon.
        self.assertEqual(ar.ROLE_GROUND, ar.role_of_targets("Ground, Water, Missile")[0])

    def test_an_unclassified_token_is_reported_and_stays_neutral(self):
        role, unknown = ar.role_of_targets("Ground, Water, SomethingNobodyHasSeen")
        self.assertEqual(ar.ROLE_GROUND, role)
        self.assertEqual({"SomethingNobodyHasSeen"}, unknown)
        # Alone, it cannot invent a domain either.
        self.assertEqual(ar.ROLE_SPECIAL, ar.role_of_targets("SomethingNobodyHasSeen")[0])

    def test_the_three_sets_are_disjoint(self):
        self.assertEqual(set(), ar.AIR_TARGETS & ar.GROUND_TARGETS)
        self.assertEqual(set(), ar.AIR_TARGETS & ar.NEUTRAL_TARGETS)
        self.assertEqual(set(), ar.GROUND_TARGETS & ar.NEUTRAL_TARGETS)


class CycleTests(unittest.TestCase):
    def test_matches_the_existing_burst_cycle_on_the_ledger_shape(self):
        # ONE formula, two parsers (the ledger stores delays as a string, the peer corpus as a
        # list). If these ever disagree the map is drawing an arrow between two rulers.
        for arm in ({"reloaddelay": "72", "burst": "2", "burstdelays": "8"},
                    {"reloaddelay": "68", "burst": "2", "burstdelays": "12"},
                    {"reloaddelay": "50", "burst": "7", "burstdelays": "6"},
                    {"reloaddelay": "80", "burst": "2"},
                    {"reloaddelay": "55", "burst": "1"},
                    {"reloaddelay": "40", "burst": "4", "burstdelays": "3, 5, 7"}):
            raw = str(arm.get("burstdelays") or "").replace(",", " ").split()
            self.assertEqual(
                rd.burst_cycle(arm, rd_num := (lambda v: None if v is None else float(v))),
                ar.cycle_ticks(arm["reloaddelay"], arm["burst"], raw),
                arm)
        del rd_num

    def test_a_burst_without_declared_delays_uses_openras_own_default(self):
        self.assertEqual(80.0 + 5.0, ar.cycle_ticks(80, 2, []))

    def test_the_rate_is_per_cycle_damage_over_the_cycle(self):
        v = ar.view("cameo", "Armament", "w", ar.ROLE_GROUND,
                    damage_per_shot=8000, burst=2, cycle=80.0)
        self.assertEqual(16000, v["damage_per_cycle"])
        self.assertAlmostEqual(200.0, v["rate"])


class RangeUnitTests(unittest.TestCase):
    def test_ini_ranges_are_cells_and_carry_their_conversion(self):
        # A DTA mammoth reads `5.7` beside a Cameo `6141`. Both are correct and they are not the
        # same unit; the view must say so rather than let a reader compare them.
        views = ar.ini_views({"source": "DTA Enhanced", "weapon": "120mm",
                              "w_range": 5.7, "w_damage": 30, "w_reload": 80})
        self.assertEqual("cells", views[0]["range_unit"])
        self.assertAlmostEqual(5.7 * ar.WDIST_PER_CELL, views[0]["range_wdist"])

    def test_openra_ranges_are_wdist_and_convert_to_themselves(self):
        views = ar.peer_views({"weapon_evidence": [
            {"slot": "Armament", "weapon": "120mm", "range": 4864, "reload_delay": 40,
             "burst": 1, "warheads": [{"damage_num": 4000}]}]})
        self.assertEqual("wdist", views[0]["range_unit"])
        self.assertEqual(4864, views[0]["range_wdist"])


class PairingTests(unittest.TestCase):
    def test_air_and_ground_are_never_paired(self):
        cam = [ar.view("cameo", "Armament", "bomb", ar.ROLE_GROUND, damage_per_shot=1, cycle=1)]
        peer = [ar.view("peer", "Armament", "sam", ar.ROLE_AIR, damage_per_shot=1, cycle=1)]
        pairs, cam_only, peer_only = ar.pair_by_role(cam, peer)
        self.assertEqual([], pairs)
        self.assertEqual(["bomb"], [v["weapon"] for v in cam_only])
        self.assertEqual(["sam"], [v["weapon"] for v in peer_only])

    def test_an_exact_role_claims_its_partner_before_a_both_may_stand_in(self):
        # Without the two-pass order the cannon claims the peer's dual-role missile first and the
        # Cameo missile is left unmatched next to a peer cannon it can never use.
        cam = [ar.view("cameo", "A", "cannon", ar.ROLE_GROUND, damage_per_shot=1, cycle=1),
               ar.view("cameo", "B", "missile", ar.ROLE_BOTH, damage_per_shot=1, cycle=1)]
        peer = [ar.view("peer", "A", "peer_cannon", ar.ROLE_GROUND, damage_per_shot=1, cycle=1),
                ar.view("peer", "B", "peer_missile", ar.ROLE_BOTH, damage_per_shot=1, cycle=1)]
        pairs, cam_only, peer_only = ar.pair_by_role(cam, peer)
        self.assertEqual({("ground", "cannon", "peer_cannon"), ("both", "missile", "peer_missile")},
                         {(r, c["weapon"], p["weapon"]) for r, c, p, _ in pairs})
        self.assertTrue(all(exact for *_x, exact in pairs))
        self.assertEqual(([], []), (cam_only, peer_only))

    def test_a_second_weapon_in_a_compatible_role_is_available_to_the_second_armament(self):
        # ⛔ THE MAINTAINER'S BATTLE TANK. DTA's GDI Medium Tank at elite carries TWO ground-role
        # weapons (the 90 mm cannon and the 70 mm missile launcher — the TS engine puts both on the
        # ground domain), while Cameo's battle tank carries a `ground` cannon and a `both` missile.
        # Keeping only the strongest peer weapon per role let the cannon claim DTA's only candidate
        # and then told the missile no source covered its role.
        cam = [ar.view("cameo", "A", "cannon", ar.ROLE_GROUND, damage_per_shot=8000, cycle=72),
               ar.view("cameo", "B", "missile", ar.ROLE_BOTH, damage_per_shot=8000, cycle=72)]
        peer = [ar.view("peer", "secondary", "90mm", ar.ROLE_GROUND,
                        damage_per_shot=30, cycle=50),
                ar.view("peer", "elite", "70mmMsl1", ar.ROLE_GROUND,
                        damage_per_shot=30, cycle=50, baseline=False, gate="elite")]
        pairs, cam_only, _peer_only = ar.pair_by_role(cam, peer)
        got = {c["weapon"]: p["weapon"] for _r, c, p, _e in pairs}
        self.assertEqual({"cannon": "90mm", "missile": "70mmMsl1"}, got)
        self.assertEqual([], cam_only)

    def test_a_zero_damage_dummy_is_never_anybodys_reference(self):
        # ⛔ CLAUSE 5. DTA's `90mmDummy` is a real armament with a real range whose only job is to
        # set the rate of fire for the next slot; the first bench handed it to the Battle Tank's
        # missile as its reference.
        cam = [ar.view("cameo", "A", "cannon", ar.ROLE_GROUND, damage_per_shot=8000, cycle=72),
               ar.view("cameo", "B", "missile", ar.ROLE_BOTH, damage_per_shot=8000, cycle=72)]
        peer = [ar.view("peer", "primary", "90mmDummy", ar.ROLE_GROUND,
                        damage_per_shot=0, cycle=50),
                ar.view("peer", "secondary", "90mm", ar.ROLE_GROUND,
                        damage_per_shot=30, cycle=50)]
        pairs, cam_only, _peer_only = ar.pair_by_role(cam, peer)
        self.assertEqual({"90mm"}, {p["weapon"] for _r, _c, p, _e in pairs})
        self.assertEqual(["missile"], [v["weapon"] for v in cam_only])

    def test_a_rank_gated_weapon_never_displaces_an_ordinary_one(self):
        cam = [ar.view("cameo", "A", "cannon", ar.ROLE_GROUND, damage_per_shot=1, cycle=1)]
        peer = [ar.view("peer", "primary", "gun", ar.ROLE_GROUND, damage_per_shot=10, cycle=1),
                ar.view("peer", "elite", "elitegun", ar.ROLE_GROUND, damage_per_shot=99,
                        cycle=1, baseline=False, gate="elite")]
        pairs, *_ = ar.pair_by_role(cam, peer)
        self.assertEqual([("gun", True)], [(p["weapon"], e) for _r, _c, p, e in pairs])

    def test_a_source_that_cannot_state_a_role_still_votes_on_the_main_weapon(self):
        # ⛔ SKIPPING THESE ENTIRELY WAS A REGRESSION. Seven of the nine INI sources pin no
        # source_sha256, so their projectiles carry no domain verdict; the first draft dropped
        # every such view and `ra2_soviets_apocalypsetank`, `ra2_allies_ifv`, `yuri_gatlingtank`
        # and `ra2_soviets_flaktrack` came back with ZERO votes on any weapon.
        cam = [ar.view("cameo", "A", "cannon", ar.ROLE_GROUND, damage_per_shot=24000, cycle=68),
               ar.view("cameo", "B", "aa_missile", ar.ROLE_AIR, damage_per_shot=32000, cycle=73)]
        peer = [ar.view("peer", "primary", "120mmx", None, damage_per_shot=200, cycle=60)]
        pairs, _cam_only, _peer_only = ar.pair_by_role(cam, peer)
        self.assertEqual(1, len(pairs))
        role, c, p, exact = pairs[0]
        # ⛔ AND IT MUST BE THE GROUND WEAPON, NOT THE STRONGEST. The Apocalypse's AA missile
        # out-damages its cannon, so "strongest" matched five peer CANNONS against an anti-air
        # missile — the one pairing the maintainer's ruling forbids.
        self.assertEqual(ar.ROLE_GROUND, role)
        self.assertEqual("cannon", c["weapon"])
        self.assertFalse(exact, "an unproven pairing must never be reported as exact")
        self.assertIsNone(p["role"], "the peer view must keep its unproven role")

    def test_an_unproven_source_never_displaces_a_proven_pairing(self):
        cam = [ar.view("cameo", "A", "cannon", ar.ROLE_GROUND, damage_per_shot=1, cycle=1)]
        peer = [ar.view("peer", "A", "peer_cannon", ar.ROLE_GROUND, damage_per_shot=1, cycle=1),
                ar.view("peer", "B", "mystery", None, damage_per_shot=99, cycle=1)]
        pairs, *_ = ar.pair_by_role(cam, peer)
        self.assertEqual([("ground", "cannon", "peer_cannon", True)],
                         [(r, c["weapon"], p["weapon"], e) for r, c, p, e in pairs])

    def test_an_unproven_source_with_no_damage_does_not_vote(self):
        cam = [ar.view("cameo", "A", "cannon", ar.ROLE_GROUND, damage_per_shot=1, cycle=1)]
        peer = [ar.view("peer", "primary", "dummy", None, damage_per_shot=0, cycle=1)]
        self.assertEqual([], ar.pair_by_role(cam, peer)[0])

    def test_the_strongest_baseline_armament_represents_its_role(self):
        # `td_gdi_lighttankmkii` reports a 1-damage point-defence laser as its gun when the FIRST
        # armament wins; within one role the alternatives are still alternatives.
        views = [ar.view("cameo", "A", "pointdefense", ar.ROLE_GROUND,
                         damage_per_shot=1, cycle=1),
                 ar.view("cameo", "B", "cannon", ar.ROLE_GROUND, damage_per_shot=8000, cycle=1)]
        self.assertEqual("cannon", ar.strongest_by_role(views)[ar.ROLE_GROUND]["weapon"])

    def test_the_peer_baseline_gate_reads_openras_condition_grammar(self):
        # The peer corpus spells conditions as OpenRA expressions; the SHARED evaluator decides,
        # so the CA MLRS's four upgrade tiers cannot be mistaken for four parallel armaments.
        for cond, expected in ((None, True), ("", True),
                               ("!hypersonic-upgrade && !hammerhead-upgrade", True),
                               ("hypersonic-upgrade", False), ("!player-iraq", True),
                               ("player-iraq", False),
                               # ⛔ FAIL CLOSED. An unparsable condition counted as baseline would
                               # double a unit's armament set, which is the defect this lane fixes.
                               ("garbage(((", False)):
            self.assertEqual(expected, ar._peer_baseline({"requires_condition": cond}), cond)

    def test_an_upgrade_gated_armament_is_not_baseline(self):
        views = [ar.view("cameo", "A", "base", ar.ROLE_GROUND, damage_per_shot=1, cycle=1),
                 ar.view("cameo", "B", "upgraded", ar.ROLE_GROUND, damage_per_shot=9,
                         cycle=1, baseline=False)]
        self.assertEqual("base", ar.strongest_by_role(views)[ar.ROLE_GROUND]["weapon"])


class ProjectileRoleEvidenceTests(unittest.TestCase):
    """The INI half: a TS weapon's domain lives on the PROJECTILE, as `AA=` / `AG=`."""

    @classmethod
    def setUpClass(cls):
        if not PROJECTILE_EVIDENCE.exists():
            raise unittest.SkipTest("ini_projectile_role_evidence.json not generated")
        cls.doc = json.loads(PROJECTILE_EVIDENCE.read_text(encoding="utf-8"))
        cls.dta = {e["source"]: e for e in cls.doc["sources"]}["DTA Enhanced"]["projectiles"]

    def test_the_name_trap_is_read_from_the_flags_not_the_spelling(self):
        # ⛔ `[AGHeatSeeker2]` — "AG" in the name — declares `$Inherits=AGHeatSeeker` and then
        # `AA=yes`. It is the DTA mammoth Tusk and it is DUAL-role, which is what makes it the
        # right partner for Cameo's MissileAP and CA's `MammothTusk` (Air, AirSmall, Infantry).
        # A name classifier calls it anti-ground and pairs an air missile against a cannon.
        self.assertEqual("both", self.dta["AGHeatSeeker2"]["role"])
        self.assertTrue(self.dta["AGHeatSeeker2"]["aa"])
        self.assertTrue(self.dta["AGHeatSeeker2"]["ag"])
        self.assertEqual("air", self.dta["AAHeatSeeker"]["role"])
        self.assertEqual("ground", self.dta["HeatSeeker"]["role"])

    def test_an_undeclared_projectile_takes_the_engine_default_and_says_so(self):
        self.assertEqual("ground", self.dta["TracerM"]["role"])
        self.assertEqual([], self.dta["TracerM"]["declared"])
        self.assertEqual({"AA": False, "AG": True}, {k: self.doc["engine_default"][k]
                                                     for k in ("AA", "AG")})

    def test_every_cited_projectile_resolves(self):
        for entry in self.doc["sources"]:
            self.assertEqual([], entry["undeclared"], entry["source"])
            self.assertEqual(entry["referenced"], entry["resolved"], entry["source"])

    def test_the_vocabulary_is_the_same_one_the_openra_side_uses(self):
        self.assertEqual(list(ar.ROLES), self.doc["role_vocabulary"])


class RulesetTests(unittest.TestCase):
    """The slow half — one Ruleset, shared."""

    @classmethod
    def setUpClass(cls):
        cls.rules = miniyaml.Ruleset(ROOT)

    def test_agrees_with_the_missile_role_audit(self):
        checked = 0
        for name in sorted(self.rules.weapons):
            if name.startswith("^"):
                continue
            try:
                node = self.rules.resolve_weapon(name)
            except Exception:
                continue
            if node is None:
                continue
            audit = weapon_role(node)
            if audit == "custom":
                continue        # the audit declines to rule; this module may still answer
            mine, _unknown = ar.cameo_weapon_role(self.rules, name)
            self.assertEqual(audit, mine, name)
            checked += 1
        # A guard that silently checked nothing is the failure mode this repo has hit four times.
        self.assertGreater(checked, 1500, "the agreement check scanned almost nothing")

    def test_the_fire_hawks_two_weapons_are_separated(self):
        # THE MAINTAINER'S OWN CASE. One actor, two weapons, a TEN-FOLD range gap; the folded
        # model reports `max(ranges)` and hands the bomb the Sidewinders' reach.
        views = {v["weapon"]: v for v in ar.cameo_views(_ledger_row("td_gdi_firehawk"), self.rules)}
        missiles = views["td_gdi_firehawk_firehawkmissiles_AA"]
        bomb = views["td_gdi_firehawk_firehawkbomb"]
        self.assertEqual(ar.ROLE_AIR, missiles["role"])
        self.assertEqual(12500, missiles["range"])
        self.assertEqual(ar.ROLE_GROUND, bomb["role"])
        self.assertEqual(1250, bomb["range"])

    def test_the_mammoths_cannon_and_missile_split(self):
        views = ar.cameo_views(_ledger_row("td_gdi_mammothtank"), self.rules)
        by_role = ar.strongest_by_role(views)
        self.assertEqual("td_gdi_mammothtank_120mmdual", by_role[ar.ROLE_GROUND]["weapon"])
        self.assertEqual("td_gdi_mammothtank_mammothmissiles", by_role[ar.ROLE_BOTH]["weapon"])
        self.assertNotIn(ar.ROLE_AIR, by_role)


def _ledger_row(actor):
    import assign_references as asg
    row = asg.ledger().get(actor)
    if row is None:
        raise AssertionError(f"{actor} is absent from the ledger")
    return row


if __name__ == "__main__":
    unittest.main()
