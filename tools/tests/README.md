# tools/tests — unit tests for the Python tooling

The original suite uses stdlib `unittest`, but this branch also includes pytest-style
`test_generate_chrome_scales`, `test_bot_insurance_model`, and `test_band_law`; `unittest discover` does NOT run the whole suite. Install pytest
and run `python -m pytest -q tools/tests` for the complete test set.

The stdlib suite remains dependency-free for the older tests.
Run from the repo root:

```sh
python -m unittest discover -s tools/tests -t tools/tests -v
```

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
