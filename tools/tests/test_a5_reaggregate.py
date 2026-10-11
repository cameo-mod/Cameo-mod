"""Offline A5 receipt reconstruction and Git provenance regressions."""
import hashlib
import json
import pathlib
import subprocess
import sys
import tempfile
import unittest
from unittest.mock import patch

ROOT = pathlib.Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "tools/ai"))
import a5_memory_parity_probe as probe
import campaign_runner as runner


class ReaggregateTests(unittest.TestCase):
    def test_live_binary_hashes_include_game_dll_and_fail_on_missing(self):
        with tempfile.TemporaryDirectory() as temp:
            root=pathlib.Path(temp);folder=root/"engine/bin";folder.mkdir(parents=True)
            names=("OpenRA.exe","OpenRA.Game.dll","OpenRA.Mods.Cameo.dll")
            for name in names:(folder/name).write_bytes(name.encode())
            with patch.object(probe,"ROOT",root):
                hashes=probe.binary_hashes()
                self.assertEqual(3,len(hashes))
                for name in names:self.assertEqual(hashlib.sha256(name.encode()).hexdigest(),hashes[str((folder/name).relative_to(root))])
                (folder/"OpenRA.Game.dll").unlink()
                with self.assertRaises(FileNotFoundError):probe.binary_hashes()

    def fixture(self, root):
        source = subprocess.check_output(["git", "rev-parse", "HEAD"], cwd=ROOT, text=True).strip()
        run = dict(seed=1337, source_head=source, baseline_parent=source, engine_version="engine", binary_sha256={"engine/bin/OpenRA.exe":"a"*64},
            exit_code=0, stop_reason="PROCESS_EXIT", peak_private_bytes=1, stop_threshold_bytes=7*1024**3,
            ceiling_bytes=runner.PRIVATE_LIMIT, host_hard_floor_bytes=runner.FREE_RAM_HARD_FLOOR,
            allocation_trace_policy={"enabled":False}, allocation_trace_files=[], fingerprint_id="same",
            seed_pin_verified=True, records_verified=True, valid_game=True, censored_cap_verified=True,
            natural_below_cap=True, cap_tick=16000, observed_cap_tick=16001, replay_count=1)
        runs=[dict(run,cell=f"run-{i:02d}") for i in (1,2)]
        for r in runs:
            (root/(r["cell"]+".receipt.json")).write_text(json.dumps(r),encoding="utf-8")
            log=root/r["cell"]/"Logs"/"graphics.log";log.parent.mkdir(parents=True)
            log.write_text("Null platform: headless window, rendering and textures disabled.\n",encoding="utf-8")
        raw=dict(pair=runs,source_head=source,baseline_commit=source,engine_version="engine",seed=1337,
            binary_sha256=run["binary_sha256"],a5_diagnostic_passed=True,order_stream_verdict="IDENTICAL_TAIL_FLUSH",
            system_openra_process_count="0",campaign_gate_passed=False,cap_tick=16000)
        path=root/"raw.json";path.write_text(json.dumps(raw),encoding="utf-8")
        return path,probe.file_sha256(path),raw

    def test_gate_accepts_reaggregate_and_rejects_each_missing_field(self):
        with tempfile.TemporaryDirectory() as temp:
            root=pathlib.Path(temp);path,sha,raw=self.fixture(root)
            with patch.object(probe.subprocess,"Popen",side_effect=AssertionError("no launch")):
                out=probe.reaggregate(path,sha,root/"derived.json")
            self.assertEqual(sha,out["reaggregation"]["source_raw_receipt_sha256"])
            self.assertEqual(7*1024**3,out["process_stop_threshold_bytes"])
            self.assertEqual(probe.file_sha256(root/"run-01/Logs/graphics.log"),out["pair"][0]["render_mode_derived_from"]["graphics_log_sha256"])
            manifest=dict(execution_approved=True,pins=dict(source_commit=raw["source_head"],engine_version="engine"),
                a5_parity_receipt_sha256=probe.file_sha256(root/"derived.json"),seeds=[1337],binary_sha256={"bin/OpenRA.exe":"a"*64})
            def gate(receipt):
                runner.execution_gate(manifest,a5_receipt=receipt,a5_receipt_sha256=manifest["a5_parity_receipt_sha256"],source_commit=raw["source_head"],engine_version="engine")
            gate(out)
            manifest["binary_sha256"]["bin/OpenRA.Game.dll"] = "b"*64
            with self.assertRaises(ValueError):gate(out)
            del manifest["binary_sha256"]["bin/OpenRA.Game.dll"]
            for key in ("render_mode","process_private_cap_bytes","host_emergency_floor_bytes"):
                bad=dict(out);bad.pop(key)
                with self.subTest(key=key),self.assertRaises(ValueError):gate(bad)
            for value in (0,runner.PRIVATE_LIMIT+1,True):
                bad=json.loads(json.dumps(out));bad["pair"][0]["stop_threshold_bytes"]=value
                with self.subTest(stop=value),self.assertRaises(ValueError):gate(bad)

    def test_missing_record_config_or_graphics_fails_closed(self):
        for field in ("ceiling_bytes","host_hard_floor_bytes","stop_threshold_bytes","allocation_trace_policy","graphics"):
            with self.subTest(field=field),tempfile.TemporaryDirectory() as temp:
                root=pathlib.Path(temp);path,sha,raw=self.fixture(root)
                if field=="graphics":(root/"run-01/Logs/graphics.log").unlink()
                else:
                    raw["pair"][0].pop(field)
                    (root/"run-01.receipt.json").write_text(json.dumps(raw["pair"][0]),encoding="utf-8")
                    path.write_text(json.dumps(raw),encoding="utf-8");sha=probe.file_sha256(path)
                with self.assertRaises(ValueError):probe.reaggregate(path,sha,root/"derived.json")
                self.assertFalse((root/"derived.json").exists())

    def test_hash_mismatch_and_output_overwrite_fail_closed(self):
        with tempfile.TemporaryDirectory() as temp:
            root=pathlib.Path(temp);path,sha,_=self.fixture(root)
            with self.assertRaises(ValueError):probe.reaggregate(path,"0"*64,root/"derived.json")
            with self.assertRaises(ValueError):probe.reaggregate(path,sha,path)

    def test_mismatched_external_run_cannot_replace_embedded_record(self):
        with tempfile.TemporaryDirectory() as temp:
            root=pathlib.Path(temp);path,sha,raw=self.fixture(root)
            raw["pair"][0]["ceiling_bytes"]=1
            (root/"run-01.receipt.json").write_text(json.dumps(raw["pair"][0]),encoding="utf-8")
            with self.assertRaises(ValueError):probe.reaggregate(path,sha,root/"derived.json")

    def test_cli_reaggregate_returns_before_launch_preflight(self):
        with tempfile.TemporaryDirectory() as temp:
            root=pathlib.Path(temp);path,sha,_=self.fixture(root)
            argv=["probe", "--reaggregate-source", str(path), "--source-raw-sha256", sha,
                "--reaggregate-output", str(root/"derived.json")]
            with patch.object(sys,"argv",argv), patch.object(probe,"host_memory_status",side_effect=AssertionError("no admission query")), patch.object(probe,"run_cell",side_effect=AssertionError("no launch")):
                self.assertEqual(0,probe.main())


class GitProvenanceTests(unittest.TestCase):
    def test_real_git_ancestry_and_only_permitted_paths(self):
        with tempfile.TemporaryDirectory() as temp:
            root=pathlib.Path(temp)
            def git(*args):return subprocess.check_output(["git",*args],cwd=root,text=True,stderr=subprocess.DEVNULL).strip()
            git("init");git("config","user.name","Offline Test");git("config","user.email","offline@example.invalid")
            def commit(path):
                p=root/path;p.parent.mkdir(parents=True,exist_ok=True);p.write_text("fixture",encoding="utf-8")
                git("add",".");git("commit","-m","fixture");return git("rev-parse","HEAD")
            base=commit("docs/base.md");tools=commit("tools/ai/fix.py")
            with patch.object(runner,"REPO_ROOT",root):
                runner.verify_tools_only_ancestor(base,tools)
                with self.assertRaises(ValueError):runner.verify_tools_only_ancestor(tools,base)
                for path in ("OpenRA.Mods.Cameo/changed.cs","engine/VERSION","mods/cameo/ai/profile.yaml","tools/fixture.lua"):
                    git("checkout","--detach",tools);tip=commit(path)
                    with self.subTest(path=path),self.assertRaises(ValueError):runner.verify_tools_only_ancestor(base,tip)
                with self.assertRaises(ValueError):runner.verify_tools_only_ancestor("0"*40,tools)


if __name__=="__main__":unittest.main()
