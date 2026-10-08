"""The fight-learning artifact must admit only anonymous integer rows."""
from pathlib import Path
import sys

import pytest

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / 'tools/audit'))
from audit_no_player_names import validate_learned


def test_shipped_fight_artifact_passes():
    assert validate_learned([ROOT / 'mods/cameo/ai/learned/fight_learning.yaml']) == []


def test_fight_headers_and_scoped_rows_pass(tmp_path):
    headers = ('Schema', 'CalibrationMinimumSamples', 'ThresholdMinimumSamples', 'EffectiveValueMinimumSamples')
    metrics = ('Evidence', 'RetreatRatioPct', 'EngageMarginPct', 'CalibrationMilli', 'EffectiveValueMilli')
    scopes = ('any', 'family_td', 'td_gdi', 'td_gdi__vs__td_nod')
    rows = [f'\t{key}: 200' for key in headers]
    rows += [f'\t{key}@{scope}: 1000' for key in metrics for scope in scopes]
    path = tmp_path / 'fight_learning.yaml'
    path.write_text('BotFightLearning:\n' + '\n'.join(rows) + '\n', encoding='utf-8')
    assert validate_learned([path]) == []


@pytest.mark.parametrize('row', [
    '\tPlayerName: 1', '\tUnknown@any: 1', '\tEvidence: 150',
    '\tEvidence@PrivatePlayerName: 150', '\tEvidence@family_PrivatePlayerName: 150',
    '\tEvidence@td_gdi__vs__PrivatePlayerName: 150',
    '\tEvidence@td_gdi__vs__td_nod__vs__td_gdi: 150', '\tEvidence@: 150',
    '\tEvidence@any: 1.5', '\tSchema: true', '\tEvidence@any: PrivatePlayerName',
    '\tEvidence@any:\n\t\tPlayerName: 1', '\t\tEvidence@any: 150',
])
def test_fight_unknown_identity_nesting_and_nonintegers_fail(tmp_path, row):
    path = tmp_path / 'fight_learning.yaml'
    path.write_text('BotFightLearning:\n\tSchema: 1\n' + row + '\n', encoding='utf-8')
    assert validate_learned([path])
