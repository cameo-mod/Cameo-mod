"""human_demo_extract.py regressions: P0 trailer/SyncInfo inventory, sealed
demo table, and P1 human-demo-orders/1 records — golden synthetic .orarep
blobs covering the three target cases (none / decoded / unsupported),
unparsed tails, truncation, disconnect censoring, seat renumbering,
requested_item allowlisting, and the identity-field audit."""
import hashlib
import json
import pathlib
import struct
import sys

ROOT = pathlib.Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "tools/ai"))
sys.path.insert(0, str(ROOT / "tools/audit"))

import order_stream_diff as osd            # noqa: E402
import human_demo_extract as hde           # noqa: E402


# --------------------------------------------------------------------- #
# Synthetic wire builders (mirror Order.Serialize / ReplayMetadata.Write)
# --------------------------------------------------------------------- #

def s7(b):
    if isinstance(b, str):
        b = b.encode()
    n = len(b)
    out = bytearray()
    while True:
        byte = n & 0x7F
        n >>= 7
        if n:
            out.append(byte | 0x80)
        else:
            out.append(byte)
            break
    return bytes(out) + b


def order(name, flags=0, subject=None, target=None, tstr=None,
          extra_actors=(), extra_loc=None, extra_data=None, grouped=(),
          raw_tail=None):
    b = b'\xff' + s7(name) + struct.pack('<h', flags)
    if flags & 0x80:
        b += struct.pack('<I', subject)
    if flags & 0x01 and target is not None:
        tt, payload = target
        b += bytes([tt]) + payload
    elif flags & 0x01:
        b += b''                      # caller must supply target bytes
    if flags & 0x04:
        b += s7(tstr if tstr is not None else b'')
    if flags & 0x02:
        b += struct.pack('<i', len(extra_actors))
        for a in extra_actors:
            b += struct.pack('<I', a)
    if flags & 0x10:
        b += struct.pack('<i', extra_loc if extra_loc is not None else 0)
    if flags & 0x20:
        b += struct.pack('<i', extra_data if extra_data is not None else 0)
    if flags & 0x100:
        b += struct.pack('<i', len(grouped))
        for a in grouped:
            b += struct.pack('<I', a)
    if raw_tail is not None:
        b += raw_tail
    return b


def packet(client, frame, body):
    pkt = struct.pack('<i', frame) + body
    return struct.pack('<ii', client, len(pkt)) + pkt


def sync_packet(client, frame, tag=0):
    pkt = struct.pack('<i', frame) + b'\x65' + struct.pack('<iq', tag, 1)
    return struct.pack('<ii', client, len(pkt)) + pkt


def disconnect_packet(frame, client_id):
    pkt = struct.pack('<i', frame) + b'\xbf' + struct.pack('<i', client_id)
    return struct.pack('<ii', 0, len(pkt)) + pkt


def trailer_block(yaml_text):
    inner = s7(yaml_text.encode())
    return (struct.pack('<ii', -1, 1) + inner
            + struct.pack('<ii', len(inner), -2))


def replay(pkts, trailer_yaml=None, terminate=True):
    blob = b''.join(pkts)
    if trailer_yaml is not None:
        blob += trailer_block(trailer_yaml)
    elif terminate:
        blob += struct.pack('<i', -1)
    return blob


SESSION_YAML = """GlobalSettings:
	ServerName: test
	Map: map_uid_test
	GameUid: 11111111-2222-3333-4444-555555555555
	NetFrameInterval: 3
Client@1:
	Index: 1
	Slot: slot_a
	Name: Alice
Client@2:
	Index: 2
	Slot: slot_b
	Bot: rambo
	Name: BotTwo
"""

TRAILER_YAML = """Root:
	Mod: cameo
	Version: {DEV_VERSION}
	MapUid: map_uid_test
	MapTitle: Test Map
	FinalGameTick: 9000
Player@0:
	ClientIndex: 1
	Name: Alice
	IsHuman: True
	IsBot: False
	FactionId: allies
	Outcome: Won
	DisconnectFrame: 0
Player@1:
	ClientIndex: 2
	Name: BotTwo
	IsHuman: False
	IsBot: True
	FactionId: nod
	Outcome: Lost
	DisconnectFrame: 0
"""

TRAILER_YAML_DC = TRAILER_YAML.replace('DisconnectFrame: 0',
                                       'DisconnectFrame: 4500', 1)


def syncinfo_order():
    return order('SyncInfo', flags=0x04, tstr=SESSION_YAML)


def base_replay(trailer_yaml=TRAILER_YAML, extra_pkts=()):
    pkts = [packet(0, 0, syncinfo_order()),
            sync_packet(0, 10), sync_packet(0, 20)]
    pkts += list(extra_pkts)
    return replay(pkts, trailer_yaml=trailer_yaml)


def extract(tmp_path, blob, name='t.orarep'):
    p = tmp_path / name
    p.write_bytes(blob)
    return hde.extract_file(str(p))


# --------------------------------------------------------------------- #
# Surfacing patch: legacy tuple projection is byte-identical
# --------------------------------------------------------------------- #

def test_detail_projection_matches_legacy():
    pkt = (struct.pack('<i', 5)
           + order('Move', flags=0x80 | 0x01, subject=42,
                   target=(1, struct.pack('<Ii', 100, 3)))
           + order('Stop', flags=0x00)
           + b'\xfe' + s7(b'HandshakeResponse') + s7(b't: abc'))
    detail, tails = osd.parse_orders_detail(pkt)
    legacy, legacy_tails = osd.parse_orders(pkt)
    assert tails == 0 and legacy_tails == 0
    assert len(detail) == len(legacy) == 3
    assert legacy[0][0] == 'O' and legacy[0][1] == b'Move'
    assert legacy[0][3] == 42
    assert legacy[1] == ('O', b'Stop', 0, None, '', None, '', '')
    assert legacy[2][0] == 'HS'
    assert detail[0]['target_kind'] == 'actor'
    assert detail[0]['target_ref'] == {'actor_id': 100, 'generation': 3}


def test_detail_unsupported_vs_tail_reasons():
    # tt=0 unsupported; trailing garbage after a good order = unparsed tail.
    pkt_bad_tt = struct.pack('<i', 5) + order(
        'Attack', flags=0x81, subject=7, target=(0, b'\x00' * 8))
    recs, tails = osd.parse_orders_detail(pkt_bad_tt)
    assert tails == 1
    assert recs[0]['parse'] == 'unsupported_target'
    assert recs[0]['target_kind'] == 'unsupported'
    assert recs[0]['order'] == b'Attack'
    assert recs[0]['subject'] == 7

    pkt_tail = struct.pack('<i', 5) + order(
        'Attack', flags=0x81, subject=7, target=(9, b'\x09' * 4))
    recs2, _ = osd.parse_orders_detail(pkt_tail)
    assert recs2[0]['parse'] == 'unsupported_target'

    pkt_over = struct.pack('<i', 5) + order(
        'Move', flags=0x81, subject=7, target=(1, b'\x01\x02'))
    recs3, _ = osd.parse_orders_detail(pkt_over)
    assert recs3[0]['parse'] == 'unparsed_tail'


# --------------------------------------------------------------------- #
# P0 inventory / sealed demos
# --------------------------------------------------------------------- #

def test_p0_trailer_inventory(tmp_path):
    inv, demos, orders = extract(tmp_path, base_replay())
    assert inv['schema'] == 'human-demo-inventory/1'
    assert inv['trailer'] == 'present'
    assert inv['sha256'] == hashlib.sha256(
        base_replay()).hexdigest()
    assert inv['map_uid'] == 'map_uid_test'
    assert inv['final_game_tick'] == 9000
    assert inv['game_uid'] == '11111111-2222-3333-4444-555555555555'
    assert inv['human_seats'] == [1]
    assert inv['human_seats_source'] == 'trailer'
    assert inv['last_sync_frame'] == 20
    assert inv['status'] == 'sealed'
    assert len(demos) == 1
    d = demos[0]
    assert d['demo'].startswith('d_') and d['demo'].endswith('_s0')
    assert d['client_index'] == 1 and d['seat'] == 0
    assert d['outcome'] == 'won' and d['human_outcome'] == 'won'
    assert d['match_group'] == inv['game_uid']
    assert d['fingerprint_group'] == 'UNKNOWN'
    assert d['replicate'] == 'not_applicable'
    assert d['mirror_group'] is None


def test_p0_trailer_absent_seats_from_session(tmp_path):
    blob = base_replay(trailer_yaml=None)
    inv, demos, _ = extract(tmp_path, blob)
    assert inv['trailer'] == 'ABSENT'
    assert inv['human_seats'] == [1]
    assert inv['human_seats_source'] == 'session'
    assert inv['final_game_tick'] is None
    assert inv['status'] == 'partial'
    assert demos[0]['outcome'] == 'UNKNOWN'
    assert demos[0]['human_outcome'] == 'UNKNOWN'


def test_p0_truncated_stream(tmp_path):
    blob = base_replay(trailer_yaml=None, terminate=False) \
        if False else replay(
            [packet(0, 0, syncinfo_order()), sync_packet(0, 10)],
            terminate=False)
    inv, _, _ = extract(tmp_path, blob)
    assert inv['terminated'] is False
    assert inv['status'] == 'partial'
    assert any('unterminated' in n for n in inv['notes'])


def test_p0_excluded_tick0(tmp_path):
    blob = base_replay(
        trailer_yaml=TRAILER_YAML.replace('FinalGameTick: 9000',
                                          'FinalGameTick: 0'))
    inv, demos, orders = extract(tmp_path, blob)
    assert inv['status'] == 'excluded'
    assert demos == [] and orders == []


def test_p0_no_humans_excluded(tmp_path):
    yaml = TRAILER_YAML.replace('IsHuman: True', 'IsHuman: False')
    inv, demos, orders = extract(tmp_path, base_replay(trailer_yaml=yaml))
    assert inv['status'] == 'excluded'
    assert inv['human_seats'] == []
    assert orders == []


# --------------------------------------------------------------------- #
# P1 order records — target cases (§2.3)
# --------------------------------------------------------------------- #

def _one_order_blob(**kw):
    return base_replay(extra_pkts=[packet(1, 30, order('Move', **kw))])


def test_p1_no_target_order_parse_ok(tmp_path):
    _, _, orders = extract(tmp_path, _one_order_blob(flags=0x80, subject=9))
    assert len(orders) == 1
    o = orders[0]
    assert o['schema'] == 'human-demo-orders/1'
    assert o['order_type'] == 'Move'
    assert o['target_ref'] == {'kind': 'none', 'ref': None}
    assert o['parse'] == 'ok'
    assert o['censor'] == 'none'
    assert o['commanded_actor_refs'] == [9]
    assert o['commanded_ref_count'] == 1
    assert o['queued'] is False
    assert o['frame'] == 30


def test_p1_actor_target(tmp_path):
    _, _, orders = extract(tmp_path, _one_order_blob(
        flags=0x81, subject=9, target=(1, struct.pack('<Ii', 555, 2))))
    assert orders[0]['target_ref'] == {
        'kind': 'actor', 'ref': {'actor_id': 555, 'generation': 2}}
    assert orders[0]['parse'] == 'ok'


def test_p1_frozen_actor_target(tmp_path):
    _, _, orders = extract(tmp_path, _one_order_blob(
        flags=0x81, subject=9, target=(3, struct.pack('<II', 77, 88))))
    assert orders[0]['target_ref'] == {
        'kind': 'frozen_actor',
        'ref': {'viewer_player_actor_id': 77, 'frozen_actor_id': 88}}


def test_p1_terrain_cell_and_pos(tmp_path):
    blob = base_replay(extra_pkts=[
        packet(1, 30, order('AttackMove', flags=0xC1, subject=9,
                            target=(2, struct.pack('<iB', 12345, 2)))),
        packet(1, 31, order('AttackMove', flags=0x81, subject=9,
                            target=(2, struct.pack('<iii', 1, 2, 3)
                                    + struct.pack('<h', -1))))])
    _, _, orders = extract(tmp_path, blob)
    assert orders[0]['target_ref']['kind'] == 'terrain_cell'
    assert orders[0]['target_ref']['ref'] == {
        'bits': struct.pack('<iB', 12345, 2).hex()}
    assert orders[1]['target_ref']['kind'] == 'terrain_pos'
    assert orders[1]['target_ref']['ref'] == {
        'bits': (struct.pack('<iii', 1, 2, 3)
                 + struct.pack('<h', -1)).hex()}


def test_p1_unsupported_target_tt0_and_tt9(tmp_path):
    for tt in (0, 9):
        blob = base_replay(extra_pkts=[
            packet(1, 30, order('Attack', flags=0x81, subject=7,
                                target=(tt, b'\xaa' * 6))),
            packet(1, 31, order('Move', flags=0x80, subject=8))])
        inv, _, orders = extract(tmp_path, blob)
        assert orders[0]['parse'] == 'unsupported_target'
        assert orders[0]['target_ref'] == {'kind': 'unsupported',
                                          'ref': None}
        assert orders[0]['order_type'] == 'Attack'
        assert orders[0]['censor'] == 'from_unparsed_byte'
        # Everything after the first unparsed byte is censored too.
        assert orders[1]['censor'] == 'from_unparsed_byte'
        assert orders[1]['parse'] == 'ok'
        assert inv['status'] == 'partial'
        assert inv['unparsed_stream'] is True


def test_p1_unparsed_tail_then_censored(tmp_path):
    # Order claims a target then the packet ends mid-payload.
    blob = base_replay(extra_pkts=[
        packet(1, 30, order('Move', flags=0x81, subject=7,
                            target=(1, b'\x01\x02'))),
        packet(1, 31, order('Stop', flags=0x80, subject=8))])
    inv, _, orders = extract(tmp_path, blob)
    assert orders[0]['parse'] == 'unparsed_tail'
    assert orders[0]['censor'] == 'from_unparsed_byte'
    assert orders[1]['censor'] == 'from_unparsed_byte'
    assert inv['status'] == 'partial'


def test_p1_nonhuman_failure_still_censors_tail(tmp_path):
    # A bot client's unparsed packet: the demo's own stream is intact,
    # but §8 censors every order after the first unparsed byte.
    blob = base_replay(extra_pkts=[
        packet(2, 30, order('Attack', flags=0x81, subject=7,
                            target=(0, b'\x00' * 8))),
        packet(1, 31, order('Move', flags=0x80, subject=8))])
    _, _, orders = extract(tmp_path, blob)
    assert len(orders) == 1
    assert orders[0]['censor'] == 'from_unparsed_byte'


# --------------------------------------------------------------------- #
# P1 — disconnect / sync-frame censoring, requested_item, refs, queued
# --------------------------------------------------------------------- #

def test_disconnect_censor_and_outcome(tmp_path):
    blob = base_replay(
        trailer_yaml=TRAILER_YAML_DC,
        extra_pkts=[packet(1, 4499, order('Move', flags=0x80, subject=9)),
                    packet(1, 4501, order('Move', flags=0x80, subject=9))])
    inv, demos, orders = extract(tmp_path, blob)
    assert orders[0]['censor'] == 'none'
    assert orders[1]['censor'] == 'after_disconnect_frame'
    assert demos[0]['human_outcome'] == 'UNKNOWN'
    assert demos[0]['outcome'] == 'won'
    assert inv['disconnect_frames'] == [{'client_index': 1, 'frame': 4500}]


def test_trailerless_sync_bound_censors(tmp_path):
    blob = base_replay(trailer_yaml=None, extra_pkts=[
        packet(1, 15, order('Move', flags=0x80, subject=9)),
        packet(1, 25, order('Move', flags=0x80, subject=9))])
    _, _, orders = extract(tmp_path, blob)
    # last_sync_frame = 20: frame 15 ok, frame 25 censored.
    assert orders[0]['censor'] == 'none'
    assert orders[1]['censor'] == 'after_last_sync_frame'


def test_requested_item_allowlist(tmp_path):
    blob = base_replay(extra_pkts=[
        packet(1, 30, order('StartProduction', flags=0x84, subject=9,
                            tstr=b'barracks')),
        packet(1, 31, order('Move', flags=0x84, subject=9,
                            tstr=b'not-an-item\x00\x00'))])
    _, _, orders = extract(tmp_path, blob)
    assert orders[0]['requested_item'] == 'barracks'
    assert orders[1]['requested_item'] is None


def test_syncinfo_blob_never_leaks(tmp_path):
    blob = base_replay(extra_pkts=[
        packet(1, 30, order('SyncInfo', flags=0x04, tstr=SESSION_YAML))])
    _, _, orders = extract(tmp_path, blob)
    assert orders[0]['order_type'] == 'SyncInfo'
    assert orders[0]['requested_item'] is None
    assert 'Alice' not in json.dumps(orders)


def test_commanded_refs_union_sorted(tmp_path):
    blob = base_replay(extra_pkts=[packet(
        1, 30, order('Attack', flags=0x182, subject=9,
                     extra_actors=(30, 10), grouped=(20, 10)))])
    _, _, orders = extract(tmp_path, blob)
    assert orders[0]['commanded_actor_refs'] == [9, 10, 20, 30]
    assert orders[0]['commanded_ref_count'] == 4


def test_queued_flag(tmp_path):
    blob = base_replay(extra_pkts=[
        packet(1, 30, order('Move', flags=0x88, subject=9)),
        packet(1, 31, order('Move', flags=0x80, subject=9))])
    _, _, orders = extract(tmp_path, blob)
    assert orders[0]['queued'] is True
    assert orders[1]['queued'] is False


def test_status_ladder_exact(tmp_path):
    _, _, orders = extract(tmp_path, _one_order_blob(flags=0x80, subject=9))
    assert orders[0]['status'] == {'requested': True, 'accepted': 'unknown',
                                   'completed': 'unknown',
                                   'cancelled': 'unknown',
                                   'rejected': 'unknown'}


def test_handshake_never_emitted(tmp_path):
    blob = base_replay(extra_pkts=[packet(
        1, 30, b'\xfe' + s7(b'HandshakeResponse') + s7(b't: abc'))])
    _, _, orders = extract(tmp_path, blob)
    assert orders == []


def test_payload_raw_only_undecoded_extras(tmp_path):
    blob = base_replay(extra_pkts=[packet(
        1, 30, order('Move', flags=0xB2, subject=9,
                     extra_actors=(5,), extra_loc=0x11223344,
                     extra_data=0x55667788))])
    _, _, orders = extract(tmp_path, blob)
    assert orders[0]['payload_raw'] == (
        struct.pack('<i', 0x11223344) + struct.pack('<i', 0x55667788)).hex()
    assert orders[0]['commanded_actor_refs'] == [5, 9]


# --------------------------------------------------------------------- #
# Seat renumbering + multi-human, determinism, identity audit
# --------------------------------------------------------------------- #

def _multi_human():
    session = SESSION_YAML + ("Client@3:\n\tIndex: 3\n\tSlot: slot_c\n"
                              "\tName: Carol\n")
    trailer = (TRAILER_YAML.rstrip() + "\nPlayer@2:\n\tClientIndex: 3\n"
               "\tName: Carol\n\tIsHuman: True\n\tIsBot: False\n"
               "\tFactionId: nod\n\tOutcome: Lost\n\tDisconnectFrame: 0\n")
    si = order('SyncInfo', flags=0x04, tstr=session)
    pkts = [packet(0, 0, si),
            packet(3, 30, order('Move', flags=0x80, subject=3)),
            packet(1, 31, order('Move', flags=0x80, subject=1)),
            packet(2, 32, order('Move', flags=0x80, subject=2))]
    return replay(pkts, trailer_yaml=trailer)


def test_seat_renumber_multi_human(tmp_path):
    inv, demos, orders = extract(tmp_path, _multi_human())
    assert inv['human_seats'] == [1, 3]
    assert [d['seat'] for d in demos] == [0, 1]
    assert [d['client_index'] for d in demos] == [1, 3]
    # records use anonymous seat, never client index; bot client 2 skipped
    assert [o['seat'] for o in orders] == [1, 0]
    assert orders[0]['demo'] == demos[1]['demo']
    assert orders[1]['demo'] == demos[0]['demo']


def test_demo_ids_deterministic(tmp_path):
    blob = _multi_human()
    _, d1, _ = extract(tmp_path, blob, 'a.orarep')
    _, d2, _ = extract(tmp_path, blob, 'b.orarep')
    assert [d['demo'] for d in d1] == [d['demo'] for d in d2]


ORDER_KEYS = {'schema', 'demo', 'seat', 'frame', 'order_type',
              'requested_item', 'commanded_actor_refs',
              'commanded_ref_count', 'target_ref', 'queued',
              'payload_raw', 'status', 'censor', 'parse'}
BANNED_ORDER_KEYS = {'name', 'client_index', 'game_uid', 'map_uid',
                     'map_title', 'fingerprint', 'spawn_point', 'team',
                     'path', 'replay_name', 'timestamp', 'record_id',
                     'selected_actors', 'viewport'}


def test_identity_audit_order_records(tmp_path):
    blob = _multi_human()
    inv, demos, orders = extract(tmp_path, blob)
    for o in orders:
        assert set(o.keys()) == ORDER_KEYS
        assert not (set(o.keys()) & BANNED_ORDER_KEYS)
        blob_json = json.dumps(o)
        assert inv['game_uid'] not in blob_json
        assert 'Alice' not in blob_json and 'Carol' not in blob_json
        assert 'map_uid_test' not in blob_json
        assert isinstance(o['seat'], int) and o['seat'] in (0, 1)
        assert o['demo'].startswith('d_')


def test_no_banned_fields_in_order_records(tmp_path):
    # §14.2 bar list: no scout/expansion/army/wave/type/spend fields.
    _, _, orders = extract(tmp_path, _one_order_blob(flags=0x80, subject=9))
    banned = {'scout', 'expansion_site', 'army_value', 'wave_size',
              'unit_type', 'spend', 'tick', 'own', 'seen', 'truth',
              'resolved', 'episode', 'outcome'}
    for o in orders:
        assert not (set(o.keys()) & banned)


# --------------------------------------------------------------------- #
# CLI end-to-end (writes the three jsonl outputs)
# --------------------------------------------------------------------- #

def test_cli_writes_three_outputs(tmp_path):
    rep = tmp_path / 'r.orarep'
    rep.write_bytes(base_replay(extra_pkts=[
        packet(1, 30, order('Move', flags=0x80, subject=9))]))
    out = tmp_path / 'out'
    rc = hde.main([str(rep), '--out', str(out)])
    assert rc == 0
    inv = [json.loads(l) for l in
           (out / 'human_demo_inventory.jsonl').read_text().splitlines()]
    orders = [json.loads(l) for l in
              (out / 'human_orders.jsonl').read_text().splitlines()]
    demos = [json.loads(l) for l in
             (out / 'human_demo_demos.jsonl').read_text().splitlines()]
    assert len(inv) == len(demos) == len(orders) == 1
    assert inv[0]['status'] == 'sealed'


def test_tool_pins_match_checkout():
    root = ROOT
    for rel, want in hde.TOOL_SHA256.items():
        got = hashlib.sha256((root / rel).read_bytes()).hexdigest()
        assert got == want, f'{rel} pin {want} != {got}'


if __name__ == '__main__':
    sys.exit(__import__('pytest').main([__file__, '-q']))
