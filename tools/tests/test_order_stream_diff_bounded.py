"""order_stream_diff_bounded.py regressions — Phase-A outcome-bounded
comparator (SPEC_2026-10-08_deterministic_replay_cutoff.md, ruling v2).

Covers: ordered canonical compare keyed (frame,clientId,intra-idx);
pregame multiset with volatile-heartbeat exclusion; terminal boundary
derivation F_term/M_term; B = F_term + L persistence; roster-projected
mask verification (map-side bots absent from trailer); contiguity-through-B;
fail-closed NO_BOUNDARY / BOUNDARY_MISMATCH / TERMINAL_MASK_MISMATCH /
INCOMPLETE_CAPTURE; and the frozen-comparator byte pin.
"""
import hashlib
import pathlib
import struct
import sys
import zipfile

import pytest

ROOT = pathlib.Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "tools/ai"))

import order_stream_diff as osd  # noqa: E402
import order_stream_diff_bounded as osdb  # noqa: E402

# Frozen comparator byte pin (same digest wave1_scheduler asserts).
FROZEN_SHA256 = '88d9be75d73c36233ad9e7d341c505b1ff76150' \
                '7ebe23f58438f6d59e1586014'


def write_str(s):
    b = s.encode() if isinstance(s, str) else s
    return bytes([len(b)]) + b


def order_body(name, flags=0, subject=None):
    b = b'\xff' + write_str(name) + struct.pack('<h', flags)
    if flags & 0x80 and subject is not None:
        b += struct.pack('<I', subject)
    return b


def order_pkt(frame, *orders):
    return struct.pack('<i', frame) + b''.join(
        order_body(*o) for o in orders)


def heartbeat_pkt(frame):
    return order_pkt(frame, ('SyncConnectionQuality', 0))


def sync_pkt(frame, mask, tag=1):
    """13-byte sync body: 0x65 + i32 hash + u64 defeatState mask."""
    return struct.pack('<i', frame) + b'\x65' + struct.pack('<i', tag) + \
        struct.pack('<Q', mask)


TRAILER_YAML = (
    'Root:\n'
    '\tMod: cameo\n'
    '\tVersion: {DEV_VERSION}\n'
    '\tMapUid: testmap\n'
    '\tMapTitle: TestMap\n'
    '\tFinalGameTick: 1000\n'
    '\tStartTimeUtc: 2026-10-08 14-00-00Z\n'
    '\tEndTimeUtc: 2026-10-08 14-05-00Z\n'
    'Player@0:\n'
    '\tClientIndex: 0\n'
    '\tName: Commander\n'
    '\tIsHuman: True\n'
    '\tIsBot: False\n'
    '\tFactionId: td_gdi\n'
    '\tOutcome: Lost\n'
    '\tOutcomeTimestampUtc: 2026-10-08 14-04-00Z\n'
    '\tDisconnectFrame: 0\n'
)

TRAILER_YAML_WON_HOST = TRAILER_YAML.replace('Outcome: Lost',
                                           'Outcome: Won')

MAP_YAML = (
    'MapFormat: 12\n'
    'Players:\n'
    '\tPlayerReference@Neutral:\n'
    '\t\tName: Neutral\n'
    '\t\tNonCombatant: True\n'
    '\tPlayerReference@Creeps:\n'
    '\t\tName: Creeps\n'
    '\t\tNonCombatant: True\n'
    '\tPlayerReference@BotA:\n'
    '\t\tName: BotA\n'
    '\t\tPlayable: False\n'
    '\t\tBot: hard\n'
    '\tPlayerReference@BotB:\n'
    '\t\tName: BotB\n'
    '\t\tPlayable: False\n'
    '\t\tBot: hard\n'
    '\tPlayerReference@Referee:\n'
    '\t\tName: Referee\n'
    '\t\tPlayable: True\n'
    '\t\tNonCombatant: True\n'
    'Actors:\n'
)


def write_replay(path, records, trailer_yaml=TRAILER_YAML):
    """records: [(client, pkt_body), ...]. Trailer layout per
    ReplayMetadata.Write: [-1][i32 ver][i32 slen][yaml][i32 dlen=slen+4][-2]."""
    data = b''
    for client, pkt in records:
        data += struct.pack('<ii', client, len(pkt)) + pkt
    if trailer_yaml is not None:
        yaml = trailer_yaml.encode()
        data += struct.pack('<ii', -1, 1)
        data += struct.pack('<i', len(yaml)) + yaml
        data += struct.pack('<ii', len(yaml) + 4, -2)
    else:
        data += struct.pack('<i', -1)  # stream terminator only, no metadata
    path.write_bytes(data)


@pytest.fixture
def mapdir(tmp_path):
    d = tmp_path / 'mapdir'
    d.mkdir()
    (d / 'map.yaml').write_text(MAP_YAML)
    return str(d)


def base_stream(f_term=50, mask=0x14):
    """Syncs 1..f_term (mask reaches final at f_term) plus the boundary
    sync at B = f_term+1 — captures record one sync per net frame, and B is
    inside the recorded stream. Also a pregame StartGame and one order at B."""
    recs = [(0, order_pkt(0, ('StartGame', 0)))]
    for f in range(1, f_term):
        recs.append((0, sync_pkt(f, 0)))
    recs.append((0, sync_pkt(f_term, mask)))
    recs.append((0, sync_pkt(f_term + 1, mask)))
    recs.append((1, order_pkt(f_term + 1, ('Move', 0x88, 7))))
    return recs


def run_pair(a, b, mapdir, tmp_path, latency=1):
    pa, pb = tmp_path / 'a.orarep', tmp_path / 'b.orarep'
    write_replay(pa, a)
    write_replay(pb, b)
    res, code = osdb.run(str(pa), str(pb), mapdir, latency)
    return res, code


# ---------- happy path ----------

def test_identical_bounded_tail(mapdir, tmp_path):
    a = base_stream(50, 0x14) + [(0, sync_pkt(52, 0x14)),
                                 (0, sync_pkt(53, 0x14))]
    res, code = run_pair(a, base_stream(50, 0x14), mapdir, tmp_path)
    assert code == 0
    assert res['verdict'] == 'IDENTICAL_OUTCOME_BOUNDED_TAIL'
    assert res['B'] == 51          # F_term + L persisted exactly
    assert res['tail_extras']['a'] == {'SYNCHASH': 2}
    assert res['tail_extras']['b'] == {}


def test_identical_no_tail(mapdir, tmp_path):
    res, code = run_pair(base_stream(50, 0x14), base_stream(50, 0x14),
                         mapdir, tmp_path)
    assert code == 0 and res['verdict'] == 'IDENTICAL'


def test_pregame_permutation_accepted(mapdir, tmp_path):
    tail = base_stream(50, 0x14)[1:]
    a = ([(0, order_pkt(0, ('HandshakeResponse', 0))),
          (0, order_pkt(-1, ('LobbyInfo', 0))),
          (0, order_pkt(0, ('StartGame', 0)))] + tail)
    b = ([(0, order_pkt(0, ('StartGame', 0))),
          (0, order_pkt(0, ('HandshakeResponse', 0))),
          (0, order_pkt(-1, ('LobbyInfo', 0)))] + tail)
    res, code = run_pair(a, b, mapdir, tmp_path)
    assert code == 0


def test_pregame_heartbeat_count_diff_accepted(mapdir, tmp_path):
    a = base_stream(50, 0x14)
    b = base_stream(50, 0x14) + [(0, heartbeat_pkt(0)),
                               (0, heartbeat_pkt(0))]
    res, code = run_pair(a, b, mapdir, tmp_path)
    assert code == 0
    assert res['pregame_excluded']['b'] == {'SyncConnectionQuality': 2}


def test_pregame_real_order_extra_diverges(mapdir, tmp_path):
    b = base_stream(50, 0x14) + [(0, order_pkt(0, ('SetRallyPoint', 0)))]
    res, code = run_pair(base_stream(50, 0x14), b, mapdir, tmp_path)
    assert code == 1 and res['verdict'] == 'DIVERGENT'


def test_cross_client_same_frame_interleave_accepted(mapdir, tmp_path):
    # File order is receipt order; apply order is (client, intra-packet).
    # A: c1-o1 then c2-o2; B: c2-o2 then c1-o1 — semantically identical.
    a = base_stream(50, 0x14)[:-1] + [
        (1, order_pkt(51, ('Move', 0x88, 7))),
        (2, order_pkt(51, ('AttackMove', 0x88, 9)))]
    b = base_stream(50, 0x14)[:-1] + [
        (2, order_pkt(51, ('AttackMove', 0x88, 9))),
        (1, order_pkt(51, ('Move', 0x88, 7)))]
    res, code = run_pair(a, b, mapdir, tmp_path)
    assert code == 0


# ---------- ordered gameplay mismatch rejection ----------

def test_intra_packet_order_swap_diverges(mapdir, tmp_path):
    a = base_stream(50, 0x14)[:-1] + [
        (1, order_pkt(51, ('Move', 0x88, 7), ('AttackMove', 0x88, 9)))]
    b = base_stream(50, 0x14)[:-1] + [
        (1, order_pkt(51, ('AttackMove', 0x88, 9), ('Move', 0x88, 7)))]
    res, code = run_pair(a, b, mapdir, tmp_path)
    assert code == 1 and res['verdict'] == 'DIVERGENT'


def test_same_client_frame_order_swap_diverges(mapdir, tmp_path):
    a = base_stream(50, 0x14)[:-1] + [
        (1, order_pkt(51, ('Move', 0x88, 7))),
        (1, order_pkt(51, ('AttackMove', 0x88, 9)))]
    b = base_stream(50, 0x14)[:-1] + [
        (1, order_pkt(51, ('AttackMove', 0x88, 9))),
        (1, order_pkt(51, ('Move', 0x88, 7)))]
    res, code = run_pair(a, b, mapdir, tmp_path)
    assert code == 1 and res['verdict'] == 'DIVERGENT'


def test_interior_order_extra_diverges(mapdir, tmp_path):
    b = base_stream(50, 0x14)
    b = b[:20] + [(1, order_pkt(20, ('RepairBuilding', 0)))] + b[20:]
    res, code = run_pair(base_stream(50, 0x14), b, mapdir, tmp_path)
    assert code == 1 and res['first_diff'] == 20


def test_post_boundary_order_tail_is_bounded(mapdir, tmp_path):
    # Orders strictly > B on one side: post-decision stragglers, not sim diffs.
    b = base_stream(50, 0x14) + [(1, order_pkt(52, ('Move', 0x88, 3)))]
    res, code = run_pair(base_stream(50, 0x14), b, mapdir, tmp_path)
    assert code == 0
    assert res['verdict'] == 'IDENTICAL_OUTCOME_BOUNDED_TAIL'
    assert res['tail_extras']['b'] == {'ORDER': 1}


# ---------- boundary semantics ----------

def test_boundary_mismatch_f_term(mapdir, tmp_path):
    res, code = run_pair(base_stream(50, 0x14), base_stream(48, 0x14),
                         mapdir, tmp_path)
    assert code == 5 and res['verdict'] == 'BOUNDARY_MISMATCH'


def test_terminal_mask_mismatch(mapdir, tmp_path):
    res, code = run_pair(base_stream(50, 0x14), base_stream(50, 0x18),
                         mapdir, tmp_path)
    assert code == 5 and res['verdict'] == 'TERMINAL_MASK_MISMATCH'


def test_no_boundary_no_syncs(mapdir, tmp_path):
    a = base_stream(50, 0x14)
    b = [(0, order_pkt(0, ('StartGame', 0))), (1, order_pkt(3, ('Move', 0)))]
    res, code = run_pair(a, b, mapdir, tmp_path)
    assert code == 5 and res['verdict'] == 'NO_BOUNDARY'


def test_no_boundary_zero_mask(mapdir, tmp_path):
    b = base_stream(50, 0x14)[:-3] + [(0, sync_pkt(50, 0)),
                                    (0, sync_pkt(51, 0))]
    res, code = run_pair(base_stream(50, 0x14), b, mapdir, tmp_path)
    assert code == 5 and res['verdict'] == 'NO_BOUNDARY'


def test_mask_without_combatant_bit_fails(mapdir, tmp_path):
    # mask 0x10 = host only — no combatant Lost bit, not a decision.
    res, code = run_pair(base_stream(50, 0x10), base_stream(50, 0x10),
                         mapdir, tmp_path)
    assert code == 5 and res['verdict'] == 'TERMINAL_MASK_MISMATCH'


def test_trailer_lost_without_mask_bit_fails(mapdir, tmp_path):
    # Bidirectional: trailer Lost but mask bit clear on the lobby slot.
    pa, pb = tmp_path / 'a.orarep', tmp_path / 'b.orarep'
    write_replay(pa, base_stream(50, 0x04))   # BotA lost only, host bit clear
    write_replay(pb, base_stream(50, 0x04))
    res, code = osdb.run(str(pa), str(pb), mapdir, 1)
    assert code == 5 and res['verdict'] == 'TERMINAL_MASK_MISMATCH'


def test_mask_won_host_variant(mapdir, tmp_path):
    # Host Won + BotA lost: mask 0x04 is consistent when trailer says Won.
    pa, pb = tmp_path / 'a.orarep', tmp_path / 'b.orarep'
    write_replay(pa, base_stream(50, 0x04), TRAILER_YAML_WON_HOST)
    write_replay(pb, base_stream(50, 0x04), TRAILER_YAML_WON_HOST)
    res, code = osdb.run(str(pa), str(pb), mapdir, 1)
    assert code == 0


def test_mask_regression_is_corrupt_evidence(mapdir, tmp_path):
    b = base_stream(50, 0x14)
    b[10] = (0, sync_pkt(10, 0x10))          # transient bit set then cleared
    res, code = run_pair(base_stream(50, 0x14), b, mapdir, tmp_path)
    assert code == 5 and res['verdict'] == 'NO_BOUNDARY'
    assert 'regressed' in res['detail']


def test_noncontiguous_sync_through_b(mapdir, tmp_path):
    b = [r for r in base_stream(50, 0x14) if r[1] != sync_pkt(30, 0)]
    res, code = run_pair(base_stream(50, 0x14), b, mapdir, tmp_path)
    assert code == 4 and res['verdict'] == 'INCOMPLETE_CAPTURE'
    assert '30' in res['detail']


def test_missing_metadata_trailer_incomplete(mapdir, tmp_path):
    pa, pb = tmp_path / 'a.orarep', tmp_path / 'b.orarep'
    write_replay(pa, base_stream(50, 0x14))
    write_replay(pb, base_stream(50, 0x14), trailer_yaml=None)
    res, code = osdb.run(str(pa), str(pb), mapdir, 1)
    assert code == 4 and res['verdict'] == 'INCOMPLETE_CAPTURE'


def test_unterminated_stream_incomplete(mapdir, tmp_path):
    pa, pb = tmp_path / 'a.orarep', tmp_path / 'b.orarep'
    write_replay(pa, base_stream(50, 0x14))
    data = b''
    for c, pkt in base_stream(50, 0x14):
        data += struct.pack('<ii', c, len(pkt)) + pkt
    pb.write_bytes(data)  # no -1 terminator at all
    res, code = osdb.run(str(pa), str(pb), mapdir, 1)
    assert code == 4 and res['verdict'] == 'INCOMPLETE_CAPTURE'


def test_unresolved_trailer_outcome_no_boundary(mapdir, tmp_path):
    pa, pb = tmp_path / 'a.orarep', tmp_path / 'b.orarep'
    yaml_undef = TRAILER_YAML.replace('Outcome: Lost', 'Outcome: Undefined')
    write_replay(pa, base_stream(50, 0x14))
    write_replay(pb, base_stream(50, 0x14), yaml_undef)
    res, code = osdb.run(str(pa), str(pb), mapdir, 1)
    assert code == 5 and res['verdict'] == 'NO_BOUNDARY'


def test_b_persisted_exact(mapdir, tmp_path):
    res, _ = run_pair(base_stream(77, 0x14), base_stream(77, 0x14),
                      mapdir, tmp_path, latency=1)
    assert res['B'] == 78 and res['a_boundary']['f_term'] == 77
    res, _ = run_pair(base_stream(77, 0x14), base_stream(77, 0x14),
                      mapdir, tmp_path, latency=3)
    assert res['B'] == 80


# ---------- roster projection ----------

def test_map_side_bot_roster_projection(mapdir):
    refs = osdb.read_map_players(mapdir)
    assert refs is not None
    layout = osdb.world_player_layout(refs, 1)
    names = [e['name'] for e in layout]
    assert names[:4] == ['Neutral', 'Creeps', 'BotA', 'BotB']
    assert names[5] == 'Everyone'
    combat = [i for i, e in enumerate(layout) if e['combatant']]
    assert combat == [2, 3]
    row_lost = [{'outcome': 'Lost', 'name': 'Cmd'}]
    row_won = [{'outcome': 'Won', 'name': 'Cmd'}]
    assert osdb.verify_mask(0x14, layout, row_lost)[0]   # BotA + host Lost
    assert osdb.verify_mask(0x18, layout, row_lost)[0]   # BotB + host Lost
    assert not osdb.verify_mask(0x14, layout, row_won)[0]  # bit4 set, says Won
    assert not osdb.verify_mask(0x10, layout, row_lost)[0]  # no combatant bit
    assert not osdb.verify_mask(0x04, layout, row_lost)[0]  # Lost but bit clear
    assert osdb.verify_mask(0x04, layout, row_won)[0]    # consistent Won host


def test_missing_map_source_fails(mapdir, tmp_path):
    res, code = run_pair(base_stream(50, 0x14), base_stream(50, 0x14),
                         str(tmp_path / 'nonexistent'), tmp_path)
    assert code == 5 and res['verdict'] == 'TERMINAL_MASK_MISMATCH'


def test_oramap_zip_roster(tmp_path):
    z = tmp_path / 'm.oramap'
    with zipfile.ZipFile(z, 'w') as f:
        f.writestr('map.yaml', MAP_YAML)
    refs = osdb.read_map_players(str(z))
    assert refs and any(r['bot'] == 'hard' for r in refs)


# ---------- frozen comparator integrity ----------

def test_frozen_comparator_bytes_unchanged():
    p = ROOT / 'tools' / 'ai' / 'order_stream_diff.py'
    assert hashlib.sha256(p.read_bytes()).hexdigest() == FROZEN_SHA256


def test_bounded_tool_imports_frozen_extract():
    # The wire parse must be the approved implementation, not a copy.
    assert osdb._osd.extract is osd.extract
