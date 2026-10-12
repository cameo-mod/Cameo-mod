from pathlib import Path
p=Path(r'C:/cameo-wt/codex-context-providers/tools/tests/test_context_features.py')
s=p.read_text().replace('assert fields == audit.SCHEMAS[kind]', 'assert len(fields) == (57 if kind == "map_features" else 61)\n    assert fields == audit.SCHEMAS[kind]')
p.write_text(s)
