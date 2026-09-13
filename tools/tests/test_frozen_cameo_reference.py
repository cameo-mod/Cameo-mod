import pathlib,sys,unittest
from unittest.mock import patch
sys.path.insert(0,str(pathlib.Path(__file__).resolve().parents[1]/'balance'))
import reference_distribution as rd,reference_targets as rt

class FrozenCameoTest(unittest.TestCase):
 def test_live_mutation_cannot_move_vote_or_ruler(self):
  frozen=[{'id':'a','type':'vehicle','hp':100},{'id':'b','type':'vehicle','hp':200},{'id':'c','type':'vehicle','hp':300}]
  peers=[dict(row,source='Peer') for row in frozen]
  peer_dist=rd.build_distributions(peers)
  ruler=rd.build_distributions([dict(row,source='Cameo') for row in frozen])['Cameo']
  context=rt.FrozenCameoDistribution(ruler,frozen)
  before=rt.target_for([peers[0]],frozen[0],'hp',peer_dist,context)
  after=rt.target_for([peers[0]],dict(frozen[0],hp=999999,type='aircraft'),'hp',peer_dist,context)
  self.assertEqual(before,after)
  newcomer=rt.target_for([peers[0]],{'id':'new','hp':999999,'type':'vehicle'},'hp',peer_dist,context)
  self.assertEqual(newcomer[0],newcomer[1])

 def test_snapshot_corruption_is_not_silently_live(self):
  with patch.object(pathlib.Path,'read_bytes',return_value=b'changed'):
   with self.assertRaises(ValueError):rt.cameo_context()

 def test_frozen_population_loaded(self):
  self.assertEqual(len(rt.cameo_context().cameo_votes),893)
