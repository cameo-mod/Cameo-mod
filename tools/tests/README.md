# tools/tests — unit tests for the Python tooling

Stdlib `unittest` for most of it (no pip dependency, so CI and a fresh clone can
run it). Run from the repo root:

```sh
python -m unittest discover -s tools/tests -t tools/tests -v
```

⛔ **That command does NOT run the whole suite, and it says OK anyway.** Seven files
here are written pytest-style — bare `def test_*` functions, six of them also using
`@pytest.mark.parametrize` / `pytest.approx` / `pytest.raises`:

```
test_audit_bot_insurance   test_balance_exceptions      test_band_law
test_bot_difficulty_curve  test_bot_insurance_model     test_support_channel
test_generate_chrome_scales
```

`unittest discover` collects `unittest.TestCase` subclasses and nothing else, so it
executes **zero** of their 108 assertions — the six that `import pytest` are counted as
one import error each, and the seventh is skipped in complete silence. Neither shows up
as a missing test. **Run those with pytest:**

```sh
python -m pytest tools/tests -q          # runs all 88 files, including the 81 TestCase ones
```

⚠ `audit_test_coverage.py` counts `def test_*` either way, so its floor cannot detect
this: a file can satisfy the coverage audit while never executing. If you add a file
here, either subclass `TestCase` or accept that the stdlib runner will skip it.

The top-level directory (`-t`) must be `tools/tests`, not the repo root: neither
`tools/` nor `tools/tests/` is a package (no `__init__.py`), so `-t .` fails with
`ImportError: Start directory is not importable`. `audit_test_coverage.py` and
`docs/audit/PERIODIC.md` already document this form — keep all three in sync.

`audit_test_coverage.py` counts the `def test_*` functions here and fails the
audit suite if the count drops below its floor, so tests may be added but not
quietly deleted.

Conventions:

- One `test_<module>.py` per module under test; name the module in the file
  name so `audit_test_coverage.py` can match it.
- Tests build fixtures in a `tempfile.TemporaryDirectory()`; nothing may read
  the live `mods/cameo` tree, so a test never breaks because content changed.
- `_bootstrap.py` puts `tools/audit` on `sys.path` (the audit scripts import
  `miniyaml` / `report` as top-level modules).
