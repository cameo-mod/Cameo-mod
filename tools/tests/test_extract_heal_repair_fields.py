"""The ledger carries SelfHealing Step and Repairable HpPerStep with honest provenance."""

from __future__ import annotations

import json
import pathlib
import sys
import unittest


ROOT = pathlib.Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "tools" / "audit"))
sys.path.insert(0, str(ROOT / "tools" / "balance"))
LEDGER = ROOT / "docs" / "balance"


class ExtractHealRepairFieldsTests(unittest.TestCase):
    """PR #404's precondition, closed: the batch shape before this could not
    ride the ledger for heal/repair at all - extract_stats.stat() looked up
    the literal key `ChangesHealth`, so `ChangesHealth@SelfHealing` (the
    instance most actors use) was invisible, and `Repairable.HpPerStep` had
    no schema slot at all.

    The contract is asserted VALUE-AGNOSTIC on purpose: the ledger value
    must equal the resolved yaml value for the same instance, and `src`
    must be `inherited` or point at the same `Trait.Instance.Field` the
    ledger consumed. Hardcoded balance numbers here would break on every
    rebase (PR #404 moves these very units), and a value snapshot proves
    nothing the resolver does not already watch.
    """

    @classmethod
    def setUpClass(cls):
        from cameo_model import Model
        cls.rules = Model(ROOT).rs

    @staticmethod
    def ledger_row(name: str) -> tuple[str, dict]:
        hits = []
        for path in sorted(LEDGER.glob("*.json")):
            doc = json.loads(path.read_text(encoding="utf-8-sig"))
            sections = doc.get("sections")
            if not isinstance(sections, dict):
                continue
            for section in sections.values():
                if name in section:
                    hits.append((path.name, section[name]))
        if not hits:
            raise AssertionError(f"{name} absent from the ledger")
        if len(hits) > 1:
            raise AssertionError(f"{name} present in several ledgers: "
                                 f"{[p for p, _ in hits]}")
        return hits[0]

    def assert_slot_tracks_yaml(self, actor: str, ledger_key: str,
                                trait: str, field: str):
        row = self.ledger_row(actor)[1]
        slot = row.get(ledger_key)
        self.assertIsInstance(slot, dict,
                              f"{actor}.{ledger_key} missing from the ledger")
        src = slot["src"]
        self.assertTrue(src == "inherited" or src.endswith(f"#{trait}.{field}"),
                        f"{actor}.{ledger_key} src {src!r} does not name "
                        f"{trait}.{field}")
        resolved = self.rules.resolve(actor)
        t = resolved.child(trait) if resolved is not None else None
        self.assertIsNotNone(t, f"resolved {actor} lacks {trait}")
        self.assertEqual(str(t.get(field)), slot["v"],
                         f"{actor}.{ledger_key} disagrees with resolved yaml")

    def test_named_self_heal_instance_is_extracted(self):
        for actor in ("td_gdi_tiberiumharvester", "td_nod_stealthharvester",
                      "japan_japaneseoretruck", "ra1_allies_alliedoretruck",
                      "ra1_soviets_oretruck"):
            with self.subTest(actor=actor):
                self.assert_slot_tracks_yaml(
                    actor, "self_heal_step", "ChangesHealth@SelfHealing", "Step")

    def test_repair_step_is_extracted(self):
        for actor in ("td_gdi_tiberiumharvester", "td_nod_stealthharvester",
                      "japan_japaneseoretruck"):
            with self.subTest(actor=actor):
                self.assert_slot_tracks_yaml(
                    actor, "repairable_hp_per_step", "Repairable", "HpPerStep")

    def test_actors_carrying_both_instances_keep_both_layers(self):
        """The bare `ChangesHealth` trait is a separate engine instance and
        both tick independently, so the raw ledger must keep the second
        layer rather than overwrite it with the named one.
        """
        for actor in ("cabal_cyborgreaper", "cabal_heavyreaper",
                      "protoss_starshipsovereign"):
            with self.subTest(actor=actor):
                named = self.ledger_row(actor)[1]["self_heal_step"]
                self.assertIn("#ChangesHealth@SelfHealing.Step", named["src"])
                bare = self.ledger_row(actor)[1]["self_heal_step_other"]
                self.assertIn("#ChangesHealth.Step", bare["src"])


if __name__ == "__main__":
    unittest.main()
