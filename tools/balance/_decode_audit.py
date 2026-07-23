#!/usr/bin/env python3
import pathlib
names = ['min_range', 'weapon_uniqueness', 'buildable_order', 'stat_formulas']
for n in names:
    src = pathlib.Path(f'docs/audit/latest/{n}.md')
    dst = pathlib.Path(f'docs/audit/latest/{n}.safe.md')
    text = src.read_text(encoding='utf-16', errors='replace')
    dst.write_text(text, encoding='utf-8')
    print(dst)
