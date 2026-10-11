import ast, copy, json, pathlib, sys, tempfile
from unittest.mock import patch
sys.path.insert(0, str(pathlib.Path(__file__).parent / 'tools' / 'ai'))
import campaign_runner as runner

# Use the exact author's valid receipt fixture; change only census cardinality.
tree = ast.parse(pathlib.Path('tools/tests/test_campaign_runner.py').read_text())
fixture = next(n.value for n in ast.walk(tree) if isinstance(n, ast.Assign)
    and any(isinstance(t, ast.Name) and t.id == 'receipt' for t in n.targets)
    and isinstance(n.value, ast.Dict))
receipt = eval(compile(ast.Expression(fixture), '<exact fixture>', 'eval'), {'runner': runner})
receipt['map']['required_seats'] = 2
second = copy.deepcopy(receipt['resolved_seats'][0]); second['seat_id'] = 'M1'; second['home'] = 'M1'
receipt['resolved_seats'].append(second)
receipt['deployment']['seats'].append(copy.deepcopy(receipt['deployment']['seats'][0]))
runner.validate_receipt(receipt)
print('REPRO: validator accepts verified two-seat receipt with duplicate M0 and no M1 deployment')

class Process:
    pid = 4242
    rc = None
    killed = False
    def poll(self): return self.rc
    def kill(self): self.killed = True; self.rc = -9
    def wait(self): return self.rc

with tempfile.TemporaryDirectory() as temp:
    base = pathlib.Path(temp); actor = base/'actor.log'; debug = base/'debug.log'
    debug.write_text('[WT 0] startup\n')
    process = Process(); loops = [0]
    def step(_):
        loops[0] += 1
        actor.write_text('AB_CAMPAIGN_ACTOR_SAMPLE tick=5000 actor_count=2\n')
        if loops[0] == 2:
            assert not process.killed
            process.rc = 0
    with patch.object(runner, 'private_bytes', return_value=1):
        result = runner.monitor_process(process, driver_log=base/'driver.log', actor_log=actor,
            stall_log=debug, clock=lambda: 0, sleep=step, required_seats=['Multi0', 'Multi1'])
    assert not process.killed and result['end_reason'] == 'INVALID_NO_DEPLOY'
    print('REPRO: observed actor tick5000 without deployment markers survives until simulated exit; no early kill')
