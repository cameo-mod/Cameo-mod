"""Every module-level name the campaign tools read must be bound (stdlib only, no launch).

campaign_runner.run_one read `batch.read_appended_records` without importing `batch`
(2026-10-10): the first real cell would have crashed AFTER its game, before the receipt
was written; the earlier kill path crashed the same way on an unimported `subprocess`.
Neither path runs in the offline tests, so this checks the names statically.
"""
import ast
import builtins
import pathlib
import unittest

ROOT = pathlib.Path(__file__).resolve().parents[2]
TOOLS = ["campaign_runner.py", "ab_campaign_pilot.py", "map_engine_acceptance.py",
         "a5_memory_parity_probe.py", "run_ai_match_batch.py"]


def unbound_globals(path: pathlib.Path) -> set[str]:
    tree = ast.parse(path.read_text(encoding="utf-8"))
    bound = set(dir(builtins)) | {"__file__", "__name__", "__doc__"}
    for node in ast.walk(tree):
        if isinstance(node, (ast.Import, ast.ImportFrom)):
            bound |= {(a.asname or a.name).split(".")[0] for a in node.names}
        elif isinstance(node, (ast.FunctionDef, ast.AsyncFunctionDef, ast.ClassDef)):
            bound.add(node.name)
            args = node.args if hasattr(node, "args") else None
            if args:
                bound |= {a.arg for a in args.args + args.kwonlyargs + args.posonlyargs}
                bound |= {a.arg for a in (args.vararg, args.kwarg) if a}
        elif isinstance(node, ast.Name) and isinstance(node.ctx, (ast.Store, ast.Del)):
            bound.add(node.id)
        elif isinstance(node, ast.arg):
            bound.add(node.arg)
        elif isinstance(node, ast.ExceptHandler) and node.name:
            bound.add(node.name)
        elif isinstance(node, (ast.Global, ast.Nonlocal)):
            bound |= set(node.names)
        elif isinstance(node, ast.MatchAs) and node.name:
            bound.add(node.name)
    used = {n.id for n in ast.walk(tree) if isinstance(n, ast.Name) and isinstance(n.ctx, ast.Load)}
    return used - bound


class CampaignToolNameTests(unittest.TestCase):
    def test_no_unbound_names(self):
        for name in TOOLS:
            with self.subTest(tool=name):
                self.assertEqual(set(), unbound_globals(ROOT / "tools" / "ai" / name))


if __name__ == "__main__":
    unittest.main()
