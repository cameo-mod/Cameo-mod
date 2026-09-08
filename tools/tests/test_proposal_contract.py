"""Exercise the actual report producer through its ledger consumer."""
import copy
import json
import pathlib
import sys
import tempfile
import unittest
from unittest.mock import patch

import _bootstrap  # noqa: F401
sys.path.insert(0, str(pathlib.Path(__file__).resolve().parents[2] / "tools/balance"))
import propose_class_rebalance as producer
import _patch_ledgers_from_reports as consumer
import extract_stats
from miniyaml import Ruleset


class ProposalContractTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.path = pathlib.Path(self.temp.name) / "proposal.md"
        self.row = dict(actor="soldier", faction="test", hp=20000, spd=60,
                        rng=5000, cost=200, per_wh=600, dmg=1200, n_wh=2,
                        rl=20, burst=1, fp0=1, dps_eff=60, price=200, delta=0,
                        note="", protected=False, dmg_filter="all", weapon="gun")
        self.doc = {"test": {"sections": {"infantry": {"soldier": {
            "hp": {"v": 10000}, "speed": {"v": 50}, "cost": {"v": 100},
            "armaments": [{"slot": "Armament", "pricing": True, "weapon": "gun",
                "damage_warheads": [
                    {"tag": "Bullet", "type": "AreaDamage", "damage": 100},
                    {"tag": "Chemical", "type": "SpreadDamage", "damage": 300},
                    {"tag": "BulletExtraDamage", "type": "SpreadDamage", "damage": 50},
                    {"tag": "BulletPercentage", "type": "AreaDamagePercentage",
                     "percentage_denominator": 10000, "damage": 1}]}]}}}}}

    def report(self, transform=lambda text: text):
        spec = dict(hp0=10000, speed0=50, range0_wdist=5000, dps0=60, cost0=100)
        with patch.object(producer, "load_anchors", return_value={"scout": {"spec": spec}}):
            text = producer.render_report([self.row], "scout")
        self.path.write_text(transform(text), encoding="utf-8")
        return consumer.parse_report(self.path)

    def test_round_trip_sets_each_main_not_ratio_to_old_max(self):
        before = copy.deepcopy(self.doc)
        result = consumer.prepare_ledgers(self.doc, self.report())
        unit = result["test"]["sections"]["infantry"]["soldier"]
        arm = unit["armaments"][0]
        self.assertEqual([w["damage"] for w in arm["damage_warheads"]][:3], [600, 600, 300])
        self.assertEqual((unit["hp"]["v"], arm["range"], arm["reloaddelay"]), (20000, 5000, 20))
        self.assertNotIn("firepower_multiplier", unit)
        self.assertEqual(self.doc, before)

    def test_inherited_weapon_round_trip_uses_resolved_mains_and_twins(self):
        root = pathlib.Path(self.temp.name).resolve()
        mod = root / "mods/cameo"
        mod.mkdir(parents=True)
        (mod / "mod.yaml").write_text("Weapons:\n\tcameo|weapons.yaml:\n", encoding="utf-8")
        (mod / "weapons.yaml").write_text(
            "^BASE:\n\tReloadDelay: 20\n\tRange: 5000\n"
            "\tWarhead@Bullet: AreaDamage\n\t\tDamage: 100\n"
            "\tWarhead@Chemical: SpreadDamage\n\t\tDamage: 300\n"
            "\tWarhead@BulletPercentage: AreaDamagePercentage\n\t\tDamage: 1\n"
            "\t\tPercentageDenominator: 10000\n"
            "gun:\n\tInherits: ^BASE\n\tWarhead@Bullet: AreaDamage\n\t\tDamage: 200\n",
            encoding="utf-8")
        rules = Ruleset(root)
        with patch.object(extract_stats, "ROOT", root), \
                patch.object(extract_stats, "derived_metrics", return_value={}):
            arm = extract_stats.weapon_entry(rules, "gun")
        arm.update(slot="Armament", pricing=True)
        self.doc["test"]["sections"]["infantry"]["soldier"]["armaments"] = [arm]
        result = consumer.prepare_ledgers(self.doc, self.report())
        changed = result["test"]["sections"]["infantry"]["soldier"]["armaments"][0]
        self.assertEqual({w["tag"]: w["damage"] for w in changed["damage_warheads"]},
                         {"Bullet": 600, "Chemical": 600, "BulletPercentage": 6})
        self.assertEqual(rules.resolve_weapon("gun").child("Warhead@Bullet").get("Damage"), "200")

    def test_missing_damage_header_refuses(self):
        with self.assertRaisesRegex(ValueError, "columns"):
            self.report(lambda text: text.replace("dmg/wh×n", "dmg"))

    def test_missing_or_invalid_targets_refuse(self):
        for value in ("-", "600", "NaN×2", "600×0", "600.5×2", "601×2"):
            with self.subTest(value=value), self.assertRaises(ValueError):
                self.report(lambda text: text.replace("600×2", value))

    def test_late_invalid_row_leaves_input_unchanged(self):
        rows = self.report()
        rows.append(dict(rows[0], actor="missing"))
        before = copy.deepcopy(self.doc)
        with self.assertRaisesRegex(ValueError, "not found"):
            consumer.prepare_ledgers(self.doc, rows)
        self.assertEqual(self.doc, before)

    def test_stale_weapon_and_count_refuse(self):
        for update in ({"weapon": "oldgun"}, {"n_wh": 3}, {"legacy FP%": 125}):
            rows = self.report()
            rows[0].update(update)
            with self.subTest(update=update), self.assertRaises(ValueError):
                consumer.prepare_ledgers(self.doc, rows)

    def test_zero_damage_survives_producer_round_trip(self):
        self.row["per_wh"] = 0
        rows = self.report()
        self.assertEqual(rows[0]["dmg"], 0)
        result = consumer.prepare_ledgers(self.doc, rows)
        warheads = result["test"]["sections"]["infantry"]["soldier"]["armaments"][0]["damage_warheads"]
        self.assertTrue(all(w["damage"] == 0 for w in warheads))

    def test_unsupported_weapon_selection_refuses_without_mutation(self):
        for mode in ("multiple", "conditional", "partial", "missing_hp", "retained"):
            with self.subTest(mode=mode):
                docs = copy.deepcopy(self.doc)
                rows = self.report()
                unit = docs["test"]["sections"]["infantry"]["soldier"]
                if mode == "multiple":
                    unit["armaments"].append(copy.deepcopy(unit["armaments"][0]))
                elif mode == "conditional":
                    unit["armaments"][0]["requires"] = "deployed"
                elif mode == "partial":
                    rows[0]["dmg_filter"] = "smallarms"
                elif mode == "missing_hp":
                    del unit["hp"]
                else:
                    unit["resolved_firepower_modifiers"] = []
                original = copy.deepcopy(docs)
                with self.assertRaises(ValueError):
                    consumer.prepare_ledgers(docs, rows)
                self.assertEqual(docs, original)

    def test_malformed_tables_refuse(self):
        self.report()
        valid = self.path.read_text(encoding="utf-8")
        for value in ("no table", valid.replace("| actor |", "| HP |"),
                      valid.replace("| soldier |", "| |"),
                      valid.replace("| `soldier` |", "| |")):
            if value == valid:
                continue
            self.path.write_text(value, encoding="utf-8")
            with self.subTest(value=value[:30]), self.assertRaises(ValueError):
                consumer.parse_report(self.path)

    def test_io_failure_after_first_ledger_is_rolled_back(self):
        rows = self.report()
        other = copy.deepcopy(self.doc["test"])
        other["sections"]["infantry"]["second"] = other["sections"]["infantry"].pop("soldier")
        docs = dict(self.doc, other=other)
        rows.append(dict(rows[0], actor="second"))
        directory = pathlib.Path(self.temp.name)
        originals = {}
        for name, doc in docs.items():
            path = directory / f"{name}.json"
            path.write_text(json.dumps(doc), encoding="utf-8")
            originals[path] = path.read_bytes()
        original_write = consumer.Transaction.write
        def failing_write(transaction, path, data):
            if path.name == "test.json":
                raise OSError("disk failure")
            return original_write(transaction, path, data)
        with patch.object(consumer, "parse_report", return_value=rows), \
                patch.object(consumer.apply_balance, "fresh_ledgers", return_value=docs), \
                patch.object(consumer, "LEDGER", directory), \
                patch.object(consumer.Transaction, "write", failing_write), \
                self.assertRaises(OSError):
            consumer.main([str(self.path), "--write"])
        for path, data in originals.items():
            self.assertEqual(path.read_bytes(), data)

    def test_duplicate_refuses(self):
        rows = self.report()
        with self.assertRaisesRegex(ValueError, "duplicate"):
            consumer.prepare_ledgers(self.doc, rows + rows)

    def test_protected_row_is_not_written(self):
        self.row.update(note="anchor", protected=True)
        self.assertEqual(consumer.prepare_ledgers(self.doc, self.report()), {})

    def test_cli_dry_run_and_write(self):
        self.report()
        output = pathlib.Path(self.temp.name) / "test.json"
        output.write_text(json.dumps(self.doc["test"]), encoding="utf-8")
        original = output.read_bytes()
        with patch.object(consumer.apply_balance, "fresh_ledgers", return_value=self.doc), \
                patch.object(consumer, "LEDGER", pathlib.Path(self.temp.name)):
            consumer.main([str(self.path)])
            self.assertEqual(output.read_bytes(), original)
            consumer.main([str(self.path), "--write"])
            self.assertNotEqual(output.read_bytes(), original)
            staged = output.read_bytes()
            with self.assertRaisesRegex(ValueError, "staged edits"):
                consumer.main([str(self.path), "--write"])
            self.assertEqual(output.read_bytes(), staged)

    def test_cli_refusal_preserves_existing_bytes(self):
        self.report()
        self.path.write_text(self.path.read_text(encoding="utf-8").replace("600×2", "-"), encoding="utf-8")
        output = pathlib.Path(self.temp.name) / "test.json"
        output.write_bytes(b"original\r\n")
        with patch.object(consumer, "LEDGER", pathlib.Path(self.temp.name)), \
                self.assertRaises(ValueError):
            consumer.main([str(self.path), "--write"])
        self.assertEqual(output.read_bytes(), b"original\r\n")
