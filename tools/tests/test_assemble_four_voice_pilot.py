"""Focused tests for the hand-authored four-voice pilot driver."""
import copy
import pathlib
import sys
import hashlib
import tempfile
import unittest


ROOT = pathlib.Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "tools" / "balance"))
import assemble_four_voice_pilot as pilot  # noqa: E402


def terms(seed):
    return {
        "None": {"A": seed, "B": 0.0},
        "Wood": {"A": seed + 1, "B": 0.0},
        "Concrete": {"A": seed + 2, "B": 0.0},
        "Light": {"A": seed + 3, "B": 0.01},
        "Heavy": {"A": seed + 4, "B": 0.02},
    }


def record(source, actor, weapon, *, status="RESOLVED", scenario="vehicle", seed=1):
    result = {
        "actor": actor,
        "weapon": weapon,
        "scenario": scenario,
        "status": status,
        "terms": terms(seed),
    }
    if source is not None:
        result["source"] = source
    return result


def datasets():
    return {
        "cameo": [record(None, "tank", "cameo-gun", seed=1)],
        "reference": [
            record("Combined Arms", "tank", "ca-gun", seed=2),
            record("OpenRA Red Alert", "tank", "ora-gun", seed=3),
        ],
        "dta": [record("DTA Enhanced", "tank", "dta-gun",
                       status="BASE_CHANNELS_RESOLVED",
                       scenario="eligible_unobstructed_impact", seed=4)],
    }


def manifest():
    return {
        "schema": 1,
        "policy": {"voice_weight": 0.25},
        "groups": [{
            "comparison_id": "tank/vehicle/base",
            "scenario": "vehicle",
            "state_key": "base",
            "voices": [
                {"voice": "Current Cameo", "dataset": "cameo", "record_index": 0,
                 "expected": {"actor": "tank", "weapon": "cameo-gun", "scenario": "vehicle"}},
                {"voice": "Combined Arms", "dataset": "reference", "record_index": 0,
                 "expected": {"actor": "tank", "weapon": "ca-gun", "scenario": "vehicle"}},
                {"voice": "DTA Enhanced", "dataset": "dta", "record_index": 0,
                 "record_scenario": "eligible_unobstructed_impact",
                 "status_alias": "DTA_BASE_CHANNELS_RESOLVED",
                 "expected": {"actor": "tank", "weapon": "dta-gun"}},
                {"voice": "OpenRA Red Alert", "dataset": "reference", "record_index": 1,
                 "expected": {"actor": "tank", "weapon": "ora-gun", "scenario": "vehicle"}},
            ],
        }],
    }


def bound_self_vote():
    return {"status": "RESOLVED", "basis": "test fixture"}


class AssembleFourVoicePilotTests(unittest.TestCase):
    def test_separate_reconstruction_contract_does_not_require_mutating_baseline(self):
        baseline = {"rows": [], "inputs": {}}
        before = copy.deepcopy(baseline)
        contract = {"baseline_sha256": "original", "dataset_sha256": "recovered",
                    "reconstruction_evidence": "reviewed-source-closure.json"}
        bound = pilot.verify_current_cameo_binding(
            baseline, {"schema": 1}, "recovered",
            binding_contract=contract, baseline_sha256="original")
        self.assertEqual(bound["status"], "RESOLVED")
        self.assertEqual(baseline, before)
        rejected = pilot.verify_current_cameo_binding(
            baseline, {"schema": 1}, "live",
            binding_contract=contract, baseline_sha256="original")
        self.assertEqual(rejected["status"], "UNRESOLVED")

    def test_frozen_recovery_checks_bytes_without_certifying_channel_votes(self):
        with tempfile.TemporaryDirectory() as directory:
            path = pathlib.Path(directory) / "ledger.json"
            path.write_bytes(b'{"sections": {}}')
            snapshot = {"inputs": {"ledger.json": hashlib.sha256(path.read_bytes()).hexdigest()}}
            result = pilot.recover_frozen_input_evidence(snapshot, directory)
            self.assertEqual(result["matched_inputs"], 1)
            self.assertEqual(result["status"], "VERIFIED")
            path.write_bytes(b'changed')
            self.assertEqual(pilot.recover_frozen_input_evidence(snapshot, directory)["status"],
                             "UNRESOLVED")

    def test_manifest_builds_one_resolved_equal_vote_group(self):
        source = datasets()
        before = copy.deepcopy(source)
        result = pilot.build_document(
            manifest(), source, self_vote_binding=bound_self_vote())

        self.assertEqual(result["status"], "RESOLVED")
        self.assertEqual(result["summary"], {
            "groups": 1, "assembly_resolved": 1,
            "aggregation_resolved": 1, "aggregation_unresolved": 0,
            "gate_resolved": 1, "gate_unresolved": 0,
        })
        gate_result = result["groups"][0]["gate"]
        self.assertEqual(gate_result["policy"]["voice_weights"], {
            "Current Cameo": 0.25,
            "Combined Arms": 0.25,
            "DTA Enhanced": 0.25,
            "OpenRA Red Alert": 0.25,
        })
        dta_row = gate_result["source_rows"]["DTA Enhanced"]
        self.assertEqual(dta_row["raw_status"], "BASE_CHANNELS_RESOLVED")
        self.assertEqual(source, before)

    def test_explicit_channel_terms_are_reduced_and_checked_against_record_terms(self):
        source = datasets()
        source["cameo"][0]["channel_terms"] = [{
            "identity": "cameo-channel",
            "status": "RESOLVED",
            "flat_terms": {axis: values["A"] for axis, values in terms(1).items()},
            "percentage_terms": {axis: values["B"] for axis, values in terms(1).items()},
        }]
        result = pilot.build_document(
            manifest(), source, self_vote_binding=bound_self_vote())
        item = result["groups"][0]
        self.assertEqual(item["aggregation_status"], "RESOLVED")
        self.assertEqual(item["source_aggregation"]["Current Cameo"]["provenance"]["channel_projection"]["mode"],
                         "explicit_channel_terms")
        self.assertEqual(item["source_aggregation"]["Current Cameo"]["provenance"]["input_row_count"], 1)
        self.assertEqual(item["source_aggregation"]["Current Cameo"]["component_summary"]["channel_count"], 1)
        self.assertEqual(item["source_aggregation"]["Current Cameo"]["component_summary"]["max_hp_fraction_terms_B"]["Light"],
                         0.01)

    def test_channel_sum_mismatch_fails_closed_before_gate(self):
        source = datasets()
        source["cameo"][0]["channel_terms"] = [{
            "identity": "wrong-channel",
            "status": "RESOLVED",
            "flat_terms": {axis: values["A"] + 1 for axis, values in terms(1).items()},
            "percentage_terms": {axis: values["B"] for axis, values in terms(1).items()},
        }]
        result = pilot.build_document(
            manifest(), source, self_vote_binding=bound_self_vote())
        item = result["groups"][0]
        self.assertEqual(item["aggregation_status"], "UNRESOLVED")
        self.assertIn("Current Cameo:channel_sum_mismatch:selected_record_terms",
                      item["aggregation_reasons"])
        self.assertEqual(item["gate"]["status"], "UNRESOLVED")

    def test_missing_self_vote_binding_fails_closed(self):
        result = pilot.build_document(manifest(), datasets())
        self.assertEqual(result["status"], "UNRESOLVED")
        self.assertEqual(result["summary"]["gate_resolved"], 0)
        self.assertIn(
            "current_cameo_self_vote_unbound:current_cameo_self_vote_binding_not_supplied",
            result["groups"][0]["gate"]["reasons"],
        )

    def test_baseline_must_explicitly_bind_channel_dataset_hash(self):
        missing = pilot.verify_current_cameo_binding({}, {"schema": 1}, "abc")
        self.assertEqual(missing["status"], "UNRESOLVED")
        bound = pilot.verify_current_cameo_binding(
            {"current_cameo_channel_vote": {
                "dataset_sha256": "abc", "dataset_schema": 1,
            }},
            {"schema": 1},
            "abc",
        )
        self.assertEqual(bound["status"], "RESOLVED")

    def test_expected_identity_guard_fails_closed(self):
        spec = manifest()
        spec["groups"][0]["voices"][0]["expected"]["weapon"] = "wrong"
        result = pilot.build_document(spec, datasets())
        item = result["groups"][0]

        self.assertEqual(result["status"], "UNRESOLVED")
        self.assertIn("expected_identity_mismatch:0:weapon:'cameo-gun'!='wrong'",
                      item["assembly_reasons"])
        self.assertEqual(item["gate"]["status"], "UNRESOLVED")
        self.assertNotIn("means", item["gate"])

    def test_dta_alias_is_required_by_the_driver(self):
        spec = manifest()
        spec["groups"][0]["voices"][2].pop("status_alias")
        result = pilot.build_document(spec, datasets())
        item = result["groups"][0]

        self.assertEqual(result["status"], "UNRESOLVED")
        self.assertIn("status_not_resolved:2:'BASE_CHANNELS_RESOLVED'",
                      item["assembly_reasons"])
        self.assertEqual(item["gate"]["status"], "UNRESOLVED")


if __name__ == "__main__":
    unittest.main()
