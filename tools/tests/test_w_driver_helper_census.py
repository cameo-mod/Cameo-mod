"""Regression: w_driver helper-census grant hashing (str-vs-Path defect).

VJIT-02 Phase-1 halted at W1 because w_driver.py:834 passed _h(name) — a
str — to sha256_file, which requires a pathlib.Path (AttributeError:
'str' object has no attribute 'read_bytes'). The census path is the
grant's tool_sha256 synthesis over spec["helpers"]; this fixture replays
that exact comprehension for the W1 case and asserts a real digest.
"""

from __future__ import annotations

import pathlib
import sys
import tempfile
import unittest

ROOT = pathlib.Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "tools" / "tests" / "qualmon_helpers"))
import w_driver


class WDriverHelperCensusHashTest(unittest.TestCase):

	def test_w1_census_helper_hashes_without_error(self):
		with tempfile.TemporaryDirectory() as tmp:
			spec = w_driver.case_spec("W1_normal_short", pathlib.Path(tmp))
		helper_hashes = {
			f"tools/tests/qualmon_helpers/{name}.py":
				w_driver.sha256_file(pathlib.Path(w_driver._h(name)))
			for name in spec["helpers"]}
		self.assertEqual(
			list(helper_hashes), ["tools/tests/qualmon_helpers/w_wait.py"])
		for key, digest in helper_hashes.items():
			self.assertRegex(digest, r"^[0-9a-f]{64}$", key)


if __name__ == "__main__":
	unittest.main()
