# tools/tests — unit tests for the Python tooling

Stdlib `unittest` only (no pip dependency, so CI and a fresh clone can run it):

```sh
python -m unittest discover -s tools/tests -t . -v
```

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
