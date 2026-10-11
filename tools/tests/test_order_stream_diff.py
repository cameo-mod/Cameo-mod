"""order_stream_diff.py regressions: queued-flag fidelity (R4) and fail-closed
unparsed tails (R5), plus the IDENTICAL / IDENTICAL_TAIL_FLUSH / DIVERGENT
verdicts on synthetic .orarep streams."""
import pathlib
import struct
import sys

ROOT = pathlib.Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "tools/ai"))

import order_stream_diff as osd  # noqa: E402


def write_str(s):
    b = s.encode() if isinstance(s, str) else s
    return bytes([len(b)]) + b


def handshake_pkt(frame, name=b'HandshakeResponse', target=b't: abc'):
    body = struct.pack('<i', frame)
    body += b'\xfe' + write_str(name) + write_str(target)
    return body


def order_pkt(frame, *orders, tail=b''):
    body = struct.pack('<i', frame)
    for name, flags, subject in orders:
        body += b'\xff' + write_str(name) + struct.pack('<h', flags)
        if flags & 0x80:
            body += struct.pack('<I', subject)
    body += tail
    return body


def sync_pkt(frame, tag=1):
    return struct.pack('<i', frame) + b'\x65' + bytes([tag]) * 8


def write_replay(path, packets):
    data = b''
    for pkt in packets:
        data += struct.pack('<ii', 0, len(pkt)) + pkt
    data += struct.pack('<i', -1)
    path.write_bytes(data)


def verdict(a, b, tmp_path):
    pa, pb = tmp_path / 'a.orarep', tmp_path / 'b.orarep'
    write_replay(pa, a)
    write_replay(pb, b)
    return osd.main([str(pa), str(pb)])


def test_identical_streams(tmp_path):
    pkts = [sync_pkt(1), order_pkt(3, ('Move', 0x88, 42))]
    assert verdict(pkts, pkts, tmp_path) == 0


def test_queued_flag_difference_diverges(tmp_path):
    # R4: 0x08 (Queued) has no field payload — must still be compared.
    a = [order_pkt(5, ('AttackMove', 0x80, 42))]
    b = [order_pkt(5, ('AttackMove', 0x88, 42))]
    assert verdict(a, b, tmp_path) == 1


def test_unparsed_tail_identical_fails_closed(tmp_path):
    # R5: bytes after the last order in a packet are opaque — byte-identical
    # tails still cannot yield IDENTICAL.
    pkts = [order_pkt(7, ('Move', 0x88, 42), tail=b'\xaa\xbb')]
    assert verdict(pkts, pkts, tmp_path) == 3


def test_unparsed_tail_difference_diverges(tmp_path):
    a = [order_pkt(7, ('Move', 0x88, 42), tail=b'\xaa\xbb')]
    b = [order_pkt(7, ('Move', 0x88, 42), tail=b'\xaa\xcc')]
    assert verdict(a, b, tmp_path) == 1


def test_trailing_sync_flush_is_identical(tmp_path):
    a = [sync_pkt(1), sync_pkt(2)]
    b = [sync_pkt(1), sync_pkt(2), sync_pkt(3)]
    assert verdict(a, b, tmp_path) == 0


def test_order_difference_diverges(tmp_path):
    a = [sync_pkt(1), order_pkt(3, ('Move', 0x88, 42))]
    b = [sync_pkt(1), order_pkt(3, ('Move', 0x88, 43))]
    assert verdict(a, b, tmp_path) == 1


def test_trailing_orders_not_flush(tmp_path):
    a = [sync_pkt(1)]
    b = [sync_pkt(1), order_pkt(3, ('Stop', 0x80, 9))]
    assert verdict(a, b, tmp_path) == 1


def test_empty_capture_is_not_proof(tmp_path):
    pa, pb = tmp_path / 'a.orarep', tmp_path / 'b.orarep'
    write_replay(pa, [])
    write_replay(pb, [])
    assert osd.main([str(pa), str(pb)]) == 4


def test_truncated_capture_is_not_proof(tmp_path):
    # Record header claims a longer packet than the file holds: cut short.
    pa, pb = tmp_path / 'a.orarep', tmp_path / 'b.orarep'
    good = struct.pack('<ii', 0, len(sync_pkt(1))) + sync_pkt(1) \
        + struct.pack('<i', -1)
    # A's capture is cut mid-record before the terminator ever arrives.
    pa.write_bytes(struct.pack('<ii', 0, len(sync_pkt(1))) + sync_pkt(1)
                   + struct.pack('<ii', 0, 999) + sync_pkt(2)[:6])
    pb.write_bytes(good)
    assert osd.main([str(pa), str(pb)]) == 4


def test_missing_terminator_is_not_proof(tmp_path):
    pa, pb = tmp_path / 'a.orarep', tmp_path / 'b.orarep'
    body = struct.pack('<ii', 0, len(sync_pkt(1))) + sync_pkt(1)
    pa.write_bytes(body)            # no -1 terminator
    pb.write_bytes(body + struct.pack('<i', -1))
    assert osd.main([str(pa), str(pb)]) == 4


# --- R6/R7: shared-window diffs and byte-faithful parsing (Sol re-review) ---

def test_same_frame_different_sync_hash_diverges(tmp_path):
    # R6: a single frame where both captures disagree on the sync hash is a
    # world-state divergence — never a tail flush.
    a = [sync_pkt(49, tag=1)]
    b = [sync_pkt(49, tag=2)]
    assert verdict(a, b, tmp_path) == 1


def test_same_frame_different_sync_inside_window_diverges(tmp_path):
    a = [sync_pkt(1), sync_pkt(49, tag=1), sync_pkt(99)]
    b = [sync_pkt(1), sync_pkt(49, tag=2), sync_pkt(99)]
    assert verdict(a, b, tmp_path) == 1


def test_handshake_declared_len_overrun_fails_closed(tmp_path):
    # R7: handshake string declares 8 bytes, only 3 present.
    bad = struct.pack('<i', 5) + b'\xfe\x05name\x08abc'
    assert verdict([bad], [bad], tmp_path) == 3


def test_order_name_declared_len_overrun_fails_closed(tmp_path):
    bad = struct.pack('<i', 5) + b'\xff\x08abc' + struct.pack('<h', 0)
    assert verdict([bad], [bad], tmp_path) == 3


def test_invalid_utf8_bytes_do_not_collapse(tmp_path):
    # R7: lossy decode would collapse these to identical replacement chars;
    # byte-exact comparison must still see the difference.
    a = [order_pkt(5, (b'\xff\xfe', 0x00, None))]
    b = [order_pkt(5, (b'\xff\xfd', 0x00, None))]
    assert verdict(a, b, tmp_path) == 1


def test_valid_handshake_packets_compare(tmp_path):
    # In-game frame so the handshake is inside the default comparison scope.
    pkts = [handshake_pkt(1), sync_pkt(1), order_pkt(3, ('Move', 0x88, 42))]
    assert verdict(pkts, pkts, tmp_path) == 0


def test_per_launch_fields_normalized(tmp_path):
    a = [handshake_pkt(1, target=b'GameUid: 111\r\nAuthToken: aaa'),
         sync_pkt(1)]
    b = [handshake_pkt(1, target=b'GameUid: 222\r\nAuthToken: bbb'),
         sync_pkt(1)]
    assert verdict(a, b, tmp_path) == 0


def test_interior_gap_missing_record_diverges(tmp_path):
    # B is missing a record at an interior frame — inside the shared window,
    # both captures were recording -> real divergence, never a flush.
    a = [sync_pkt(1), sync_pkt(2), sync_pkt(3)]
    b = [sync_pkt(1), sync_pkt(3)]
    assert verdict(a, b, tmp_path) == 1


def test_interior_order_gap_diverges(tmp_path):
    a = [sync_pkt(1), order_pkt(5, ('Move', 0x88, 42)), sync_pkt(9)]
    b = [sync_pkt(1), sync_pkt(9)]
    assert verdict(a, b, tmp_path) == 1


def test_packet_grouping_is_irrelevant(tmp_path):
    # One packet holding two orders equals two packets with one order each —
    # ReplayConnection batching is a recording artifact, not gameplay.
    a = [order_pkt(5, ('Move', 0x88, 42), ('Stop', 0x80, 43))]
    b = [order_pkt(5, ('Move', 0x88, 42)), order_pkt(5, ('Stop', 0x80, 43))]
    assert verdict(a, b, tmp_path) == 0


def test_same_orders_different_packet_order_identical(tmp_path):
    # Interleaving across channels within/across frames is not gameplay.
    a = [order_pkt(5, ('Move', 0x88, 42)), sync_pkt(5)]
    b = [sync_pkt(5), order_pkt(5, ('Move', 0x88, 42))]
    assert verdict(a, b, tmp_path) == 0


def test_trailing_disconnect_is_not_flush(tmp_path):
    a = [sync_pkt(1), sync_pkt(2)]
    b = [sync_pkt(1), sync_pkt(2),
         struct.pack('<i', 9) + b'\xbf' + b'client left']
    assert verdict(a, b, tmp_path) == 1
