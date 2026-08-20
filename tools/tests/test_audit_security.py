"""Unit tests for tools/audit/audit_security.py."""

from __future__ import annotations

import pathlib
import tempfile
import unittest

import _bootstrap  # noqa: F401 — sys.path side effect

import audit_security as sec


def details(source: str) -> list[str]:
    return [detail for _rel, _line, detail in sec.scan_python_exec("f.py", source)]


def secret_labels(text: str) -> list[str]:
    return [label for label, pattern in sec.SECRET_PATTERNS if pattern.search(text)]


class CodeExecutionTest(unittest.TestCase):
    def test_eval_is_reported(self):
        self.assertTrue(any("eval" in d for d in details("eval(user_input)\n")))

    def test_pickle_load_is_reported(self):
        self.assertTrue(any("pickle" in d
                            for d in details("import pickle\npickle.load(fh)\n")))

    def test_yaml_load_without_loader_is_reported(self):
        self.assertTrue(any("yaml.load" in d
                            for d in details("import yaml\nyaml.load(text)\n")))

    def test_yaml_safe_load_is_clean(self):
        self.assertEqual(details("import yaml\nyaml.safe_load(text)\n"), [])

    def test_shell_true_with_built_up_command_is_reported(self):
        source = ("import subprocess\n"
                  "subprocess.run('ls ' + target, shell=True)\n")
        self.assertTrue(any("shell=True" in d for d in details(source)))

    def test_shell_true_with_a_literal_command_is_clean(self):
        source = "import subprocess\nsubprocess.run('ls -l', shell=True)\n"
        self.assertEqual(details(source), [])

    def test_unparsable_source_does_not_raise(self):
        self.assertEqual(sec.scan_python_exec("f.py", "def broken(:\n"), [])


class SecretPatternTest(unittest.TestCase):
    def test_aws_key_shape_matches(self):
        key = "AKIA" + "IOSFODNN7EXAMPLE"
        self.assertIn("AWS access key id", secret_labels(f"key = {key}"))

    def test_private_key_block_matches(self):
        block = "-----BEGIN " + "PRIVATE KEY-----"
        self.assertIn("private key block", secret_labels(block))

    def test_env_placeholder_is_not_a_secret(self):
        self.assertNotIn("hardcoded password", secret_labels('password = "${DB_PASS}"'))

    def test_short_value_is_not_a_secret(self):
        self.assertNotIn("hardcoded password", secret_labels('password = "abc"'))


class DownloadsIntegrityTest(unittest.TestCase):
    def run_scan(self, text: str) -> list[list[str]]:
        with tempfile.TemporaryDirectory() as tmp:
            root = pathlib.Path(tmp)
            target = root / sec.DOWNLOADS_YAML
            target.parent.mkdir(parents=True, exist_ok=True)
            target.write_text(text, encoding="utf-8")
            return sec.scan_downloads(root)

    def test_url_without_sha_is_reported(self):
        rows = self.run_scan("music: Music\n\tURL: https://example.invalid/m.zip\n")
        self.assertEqual(len(rows), 1)
        self.assertIn("music", rows[0][2])

    def test_url_with_sha1_is_clean(self):
        self.assertEqual(self.run_scan(
            "music: Music\n\tSHA1: abc\n\tURL: https://example.invalid/m.zip\n"), [])

    def test_mirrorlist_without_sha_is_reported(self):
        rows = self.run_scan("base: Base\n\tMirrorList: https://example.invalid/l.txt\n")
        self.assertEqual(len(rows), 1)

    def test_entry_without_download_is_ignored(self):
        self.assertEqual(self.run_scan("meta: Meta\n\tExtract:\n\t\ta: b\n"), [])

    def test_missing_file_returns_no_rows(self):
        with tempfile.TemporaryDirectory() as tmp:
            self.assertEqual(sec.scan_downloads(pathlib.Path(tmp)), [])


class BaselineTest(unittest.TestCase):
    def test_baselines_cover_every_code(self):
        self.assertEqual(set(sec.BASELINES), {"S1", "S2", "S3", "S4", "S5", "S6"})

    def test_credential_and_execution_baselines_are_zero(self):
        self.assertEqual(sec.BASELINES["S1"], 0)
        self.assertEqual(sec.BASELINES["S2"], 0)


if __name__ == "__main__":
    unittest.main()
