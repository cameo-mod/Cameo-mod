#!/usr/bin/env python3
import pathlib
names = ['min_range', 'weapon_uniqueness', 'buildable_order', 'stat_formulas']
for n in names:
    src = pathlib.Path(f'docs/audit/latest/{n}.md')
    dst = pathlib.Path(f'docs/audit/latest/{n}.safe.md')
    raw = src.read_bytes().replace(b'\x00', b'')
    dst.write_bytes(raw)
    print(dst)
