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
    b = s.encode()
    return bytes([len(b)]) + b


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
