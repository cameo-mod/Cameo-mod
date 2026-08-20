"""Unit tests for the shared audit file-walking helpers."""

from __future__ import annotations

import pathlib
import tempfile
import unittest

import _bootstrap  # noqa: F401 — sys.path side effect

from scanning import iter_dirs, iter_files


class ScanningTest(unittest.TestCase):
    def test_iter_files_is_sorted_and_skips_excluded_directories(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = pathlib.Path(tmp)
            (root / "z.py").write_text("", encoding="utf-8")
            (root / "a.py").write_text("", encoding="utf-8")
            (root / "archive").mkdir()
            (root / "archive" / "hidden.py").write_text("", encoding="utf-8")

            result = [path.relative_to(root).as_posix()
                      for path in iter_files(root, ".py")]

        self.assertEqual(result, ["a.py", "z.py"])

    def test_iter_files_missing_base_is_empty(self):
        with tempfile.TemporaryDirectory() as tmp:
            result = list(iter_files(pathlib.Path(tmp) / "missing", ".py"))

        self.assertEqual(result, [])

    def test_iter_dirs_scans_each_directory_in_order(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = pathlib.Path(tmp)
            for directory, name in (("first", "one.py"), ("second", "two.py")):
                path = root / directory
                path.mkdir()
                (path / name).write_text("", encoding="utf-8")

            result = [path.relative_to(root).as_posix()
                      for path in iter_dirs(root, ("first", "second"), ".py")]

        self.assertEqual(result, ["first/one.py", "second/two.py"])


if __name__ == "__main__":
    unittest.main()
