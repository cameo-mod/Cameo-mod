"""Unit tests for tools/audit/audit_error_handling.py."""

from __future__ import annotations

import ast
import unittest

import _bootstrap  # noqa: F401 — sys.path side effect

import audit_error_handling as eh


def codes(source: str) -> list[str]:
    visitor = eh.Visitor("fixture.py")
    visitor.visit(ast.parse(source))
    return [code for code, _rel, _line, _detail in visitor.findings]


class BareExceptTest(unittest.TestCase):
    def test_bare_except_is_e1(self):
        self.assertIn("E1", codes("try:\n    f()\nexcept:\n    log()\n"))

    def test_base_exception_is_e1(self):
        self.assertIn("E1", codes("try:\n    f()\nexcept BaseException:\n    log()\n"))

    def test_named_exception_is_not_e1(self):
        self.assertNotIn("E1", codes("try:\n    f()\nexcept ValueError:\n    log()\n"))


class SwallowedErrorTest(unittest.TestCase):
    def test_pass_only_handler_is_e2(self):
        self.assertIn("E2", codes("try:\n    f()\nexcept OSError:\n    pass\n"))

    def test_ellipsis_only_handler_is_e2(self):
        self.assertIn("E2", codes("try:\n    f()\nexcept OSError:\n    ...\n"))

    def test_handler_that_records_the_error_is_clean(self):
        source = "try:\n    f()\nexcept OSError as exc:\n    rows.append(exc)\n"
        self.assertNotIn("E2", codes(source))


class OpenEncodingTest(unittest.TestCase):
    def test_text_open_without_encoding_is_e3(self):
        self.assertIn("E3", codes("open('a.txt')\n"))

    def test_open_with_encoding_is_clean(self):
        self.assertNotIn("E3", codes("open('a.txt', encoding='utf-8')\n"))

    def test_binary_open_is_exempt(self):
        self.assertNotIn("E3", codes("open('a.bin', 'rb')\n"))

    def test_read_text_without_encoding_is_e3(self):
        self.assertIn("E3", codes("path.read_text()\n"))


class SubprocessTest(unittest.TestCase):
    def test_run_without_check_is_e4(self):
        self.assertIn("E4", codes("import subprocess\nsubprocess.run(['ls'])\n"))

    def test_run_with_check_is_clean(self):
        self.assertNotIn("E4", codes("import subprocess\n"
                                     "subprocess.run(['ls'], check=True)\n"))


class BaselineTest(unittest.TestCase):
    def test_every_code_has_a_baseline(self):
        self.assertEqual(set(eh.BASELINES), {"E1", "E2", "E3", "E4"})
        for code, value in eh.BASELINES.items():
            self.assertGreaterEqual(value, 0, code)


if __name__ == "__main__":
    unittest.main()
