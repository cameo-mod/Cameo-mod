import subprocess, sys, pathlib, time, json, os

ROOT = pathlib.Path(r'C:\cameo-wt\devin-engbump')
TESTS = ROOT/'tools'/'tests'
MODULE_TIMEOUT = 180
TOTAL_TIMEOUT = 2400
modules = sorted(p.name for p in TESTS.glob('test_*.py'))
start = time.time()
rows = []
fails = []
killed = []
for mod in modules:
    if time.time() - start > TOTAL_TIMEOUT:
        rows.append({'module': 'REMAINING', 'status': 'TOTAL_TIMEOUT', 'rc': -99})
        break
    t0 = time.time()
    try:
        r = subprocess.run([sys.executable, '-m', 'unittest', 'discover', '-s', 'tools/tests', '-p', mod, '-q'],
                           cwd=ROOT, capture_output=True, text=True, timeout=MODULE_TIMEOUT)
        tail = (r.stderr or '')[-400:] + (r.stdout or '')[-400:]
        ran = None
        import re
        m = re.findall(r'Ran (\d+) test', r.stderr + r.stdout)
        ran = int(m[-1]) if m else 0
        status = 'ok' if r.returncode == 0 else 'FAIL'
        if r.returncode != 0:
            fails.append(mod)
        rows.append({'module': mod, 'status': status, 'rc': r.returncode, 'tests': ran, 'sec': round(time.time()-t0,1), 'tail': tail if r.returncode else ''})
    except subprocess.TimeoutExpired:
        killed.append(mod)
        rows.append({'module': mod, 'status': 'TIMEOUT', 'rc': -1, 'tests': 0, 'sec': MODULE_TIMEOUT, 'tail': ''})
    print(f"{mod}: {rows[-1]['status']} rc={rows[-1]['rc']} tests={rows[-1].get('tests',0)}", flush=True)

summary = {'modules': len(modules), 'completed': len([r for r in rows if r['status'] != 'REMAINING']),
           'failed': fails, 'timed_out': killed, 'tests_total': sum(r.get('tests') or 0 for r in rows),
           'elapsed_sec': round(time.time()-start,1), 'rows': rows}
(ROOT/'mod_suite_result.json').write_text(json.dumps(summary, indent=1), encoding='utf-8')
print(f"DONE completed={summary['completed']}/{len(modules)} failed={len(fails)} timed_out={len(killed)} tests={summary['tests_total']}", flush=True)
