import importlib.util,sys,copy,json
from pathlib import Path
p=Path('tools/ai/fit_fight_learning.py');s=importlib.util.spec_from_file_location('fit',p);f=importlib.util.module_from_spec(s);sys.modules['fit']=f;s.loader.exec_module(f)
def row(own='td_gdi',enemy='td_nod',killed=100,lost=0):
 return {'schema':'engagement/2','record':'engagement','record_id':'same','faction':own,'enemy_faction':enemy,'seen':{'start':{'own_committed_value':100,'enemy_unit_value':100,'predicted_enemy_surviving_permille':500}},'outcome':{'enemy_killed_value':killed,'own_lost_value':lost}}
print('149 duplicates:',f.fit([row()]*149))
print('150 duplicates accepted:',f.fit([row()]*150))
for wins in [0,1,100,200]:
 r=f.fit([row()]*wins+[row(killed=0,lost=100)]*(200-wins)); print('effective wins',wins,'of200:',r['any'][2])
print('Identity scope:',list(f.fit([row(own='PrivatePlayerName')]*150)))
z=row();z['outcome']['enemy_killed_value']=0;z['truth']={'enemy_loss_value':100};print('zero seen kill chooses truth:',f.facts(z))
b=row();b['schema']='wrong';print('wrong schema accepted:',f.facts(b))
for v in [0,1000]:
 b=row();b['seen']['start']['predicted_enemy_surviving_permille']=v;print('valid extreme prediction',v,'accepted:',f.facts(b))
try: b=row();b['seen']['start']['own_committed_value']='invalid';f.facts(b)
except Exception as e: print('malformed scalar aborts fitter:',type(e).__name__)
