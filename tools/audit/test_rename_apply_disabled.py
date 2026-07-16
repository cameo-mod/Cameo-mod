#!/usr/bin/env python3

from __future__ import annotations

import contextlib
import importlib.util
import io
import pathlib
import unittest


class RenameApplySafetyTests(unittest.TestCase):
    def test_legacy_applicator_fails_closed(self):
        path = pathlib.Path(__file__).resolve().parents[1] / "rename" / "apply.py"
        spec = importlib.util.spec_from_file_location("legacy_rename_apply", path)
        module = importlib.util.module_from_spec(spec)
        assert spec.loader is not None
        spec.loader.exec_module(module)

        output = io.StringIO()
        with contextlib.redirect_stdout(output):
            result = module.main()

        self.assertEqual(result, 2)
        self.assertIn("is disabled", output.getvalue())


if __name__ == "__main__":
    unittest.main()
