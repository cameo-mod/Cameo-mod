#!/usr/bin/env python3
"""Generate the candidate executable manifest for the 64-game BU pilot.

Derives every pinned field from ab_campaign_pilot's own constants and the
live tree, so the emitted manifest is exactly what validate() will accept.
Output: docs-ready JSON + its sha256. execution_approved stays false —
the maintainer flips that byte at sign-off.

Usage: python make_pilot_manifest.py <repo_root> [engine_root] [--write]
"""
import hashlib, json, pathlib, sys

ROOT = pathlib.Path(sys.argv[1]).resolve()
ENGINE = pathlib.Path(sys.argv[2]).resolve() if len(sys.argv) > 2 and not sys.argv[2].startswith("--") else None

sys.path.insert(0, str(ROOT / "tools" / "ai"))
import ab_campaign_pilot as pilot
import apply_increment_switches as increment

sha = pilot.sha
SPEC = pathlib.Path(pilot.__file__).with_name("increment_switches.yaml")
groups = increment.load_spec(SPEC)[1]

maps = [{"name": k, "path": v[0], "sha256": v[1], "required_seats": v[2]} for k, v in pilot.MAPS.items()]
setups = [{"id": n, "team_size": s, "map": m, "team_factions": list(f)} for n, s, m, f in pilot.SETUPS]

# Baseline manifest = the ratchet's zero-accepted-switches starting point.
baseline_path = ROOT / "docs" / "audit" / "ab_baseline_manifest_pilot.json"
baseline_doc = {
    "schema": 1,
    "purpose": "ratchet baseline for the 64-game BU pilot — zero accepted switches",
    "accepted_switches": [],
    "control_groups": [],
    "source_commit": "964cdb630b1514e1c1a0baed55cbdbc427d5fc11",
    "engine_version": "4f69df005fe0d893526890fcb5bbc42e442d6fd6",
    "note": "control arm materializes stock hard AI payload; each ratchet stage adds accepted groups to both arms",
}
if not baseline_path.is_file():
    baseline_path.parent.mkdir(parents=True, exist_ok=True)
    baseline_path.write_bytes((json.dumps(baseline_doc, indent=2) + "\n").encode("utf-8"))
    print(f"wrote {baseline_path}")

tool_paths = {
    "driver_analyzer": pathlib.Path(pilot.__file__),
    "campaign_runner": ROOT / "tools" / "ai" / "campaign_runner.py",
    "batch_runner": ROOT / "tools" / "ai" / "run_ai_match_batch.py",
    "switch_applier": ROOT / "tools" / "ai" / "apply_increment_switches.py",
}

binary_names = ["OpenRA.exe", "OpenRA.Game.dll", "OpenRA.Mods.Cameo.dll"]
engine_root = ENGINE or (ROOT / "engine")
binary_sha = {}
for name in binary_names:
    p = engine_root / "bin" / name
    binary_sha[f"bin/{name}"] = sha(p)

manifest = {
    "schema": 1,
    "campaign_id": "ab-bu-pilot-2026-10-11",
    "switch": pilot.SWITCH,
    "pairs_per_setup": 2,
    "team_size_scope": [1, 2, 3, 4],
    "arm_bot_types": {"control": "hard_control", "treatment": "hard_treatment"},
    "seeds": [1337, 2026],
    "round_stages": pilot.ROUND_STAGES,
    "acceptance_thresholds": pilot.DEFAULT_THRESHOLDS,
    "slot_policy": pilot.SLOT_POLICY,
    "baseline_manifest_path": baseline_path.relative_to(ROOT).as_posix(),
    "baseline_manifest_sha256": pilot.text_sha(baseline_path),
    "pins": {
        "source_commit": "964cdb630b1514e1c1a0baed55cbdbc427d5fc11",
        "engine_version": "4f69df005fe0d893526890fcb5bbc42e442d6fd6",
    },
    "switch_patch_sha256": increment.group_patch_sha256(groups[pilot.SWITCH]),
    "baseline_ai_payload_sha256": pilot.ai_payload_sha(ROOT),
    "treatment_ai_payload_sha256": pilot.ai_payload_sha(ROOT, pilot.SWITCH),
    "tool_hash_algorithm": "sha256-crlf-folded",
    "tool_sha256": {k: pilot.text_sha(v) for k, v in tool_paths.items()},
    "receipt_schema_sha256": pilot.text_sha(ROOT / "tools" / "ai" / "ab_campaign_receipt.schema.json"),
    "binary_sha256": binary_sha,
    "maps": maps,
    "setups": setups,
    "execution_approved": False,
    "notes": "candidate dry-run manifest; maintainer flips execution_approved at sign-off after the open-item proposal is ruled on",
}

out = ROOT / "docs" / "audit" / "ab_pilot_manifest_candidate.json"
prior = json.loads(out.read_text(encoding="utf-8-sig")) if out.is_file() else {}
a5_path = prior.get("a5_parity_receipt_path")
if not a5_path or not pathlib.Path(a5_path).is_file():
    raise SystemExit("existing candidate must identify the pinned A5 receipt file")
manifest["a5_parity_receipt_path"] = a5_path
manifest["a5_parity_receipt_sha256"] = sha(pathlib.Path(a5_path))
manifest["notes"] = "non-executable candidate; execution approval remains false until maintainer sign-off"
text = json.dumps(manifest, indent=2) + "\n"
if "--write" in sys.argv:
    out.write_bytes(text.encode("utf-8"))
print("manifest_sha256", hashlib.sha256(text.encode("utf-8")).hexdigest())
print("bytes", len(text.encode("utf-8")), "->", out)
jobs = pilot.make_jobs(manifest)
print("jobs", len(jobs), "expected 64")
print("sample cell", jobs[0]["cell_id"], "| seats:", len(jobs[0]["seat_assignments"]))
