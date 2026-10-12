from pathlib import Path
p=Path(r'C:/cameo-wt/codex-rereview-econ-a/review-results/write_report.py')
s=p.read_text(encoding='utf-8-sig')
s=s.rsplit("''')",1)[0]+"''', encoding='utf-8')\n"
p.write_text(s,encoding='utf-8')
