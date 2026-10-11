"""wave1_scheduler adjudication regressions (Sol wave5 re-review spec).

PASS requires exit 0 AND a proof verdict AND a well-formed payload;
adjudication must inspect the COMPLETE unmatched record sets (never the
first-diff-frame subset), must require extras to be one-sided passive
SYNCHASH strictly past the shared window with nonempty identical match
records as a gate (never proof), and must never reclassify extra orders
as artifacts or proof.

Loads the scheduler module from WAVE_SCHEDULER env (default: the staged
parity-wave1 path) so the same file Sol reviewed is exercised.
"""
import importlib.util
import importlib.machinery
import json
import os
import pathlib
import struct
import sys

SCHED = os.environ.get(
    "WAVE_SCHEDULER",
    str(pathlib.Path(__file__).resolve().parents[1]
        / "ai" / "wave1_scheduler.py"))
_loader = importlib.machinery.SourceFileLoader("wave1_scheduler", SCHED)
_spec = importlib.util.spec_from_loader(_loader.name, _loader)
ws = importlib.util.module_from_spec(_spec)
_loader.exec_module(ws)


# --- replay fixtures (same wire format as test_order_stream_diff.py) ---

def write_str(s):
    b = s.encode() if isinstance(s, str) else s
    return bytes([len(b)]) + b


def order_pkt(frame, *orders):
    body = struct.pack('<i', frame)
    for name, flags, subject in orders:
        body += b'\xff' + write_str(name) + struct.pack('<h', flags)
        if flags & 0x80:
            body += struct.pack('<I', subject)
    return body


def sync_pkt(frame, tag=1):
    return struct.pack('<i', frame) + b'\x65' + bytes([tag]) * 8


def write_replay(path, packets):
    data = b''
    for pkt in packets:
        data += struct.pack('<ii', 0, len(pkt)) + pkt
    data += struct.pack('<i', -1)
    path.write_bytes(data)


def write_match(cell_dir, record):
    """One cameo-ai-matches.jsonl record in a cell dir."""
    d = pathlib.Path(cell_dir) / 'Logs'
    d.mkdir(parents=True, exist_ok=True)
    (d / 'cameo-ai-matches.jsonl').write_text(json.dumps(record) + '\n')


MATCH = {"map": "m", "duration_ticks": 100, "outcome": "won",
         "record_id": "v1", "recorded_utc": "t1", "game_uid": "g1"}


def cmp_rec(rep_a, rep_b, na=10, nb=10):
    return {"exit": 1, "verdict": "DIVERGENT",
            "incomplete": {rep_a: False, rep_b: False},
            "records": {rep_a: na, rep_b: nb}}


def run_adj(a, b, tmp_path, recs=None, match=True):
    pa, pb = tmp_path / 'a.orarep', tmp_path / 'b.orarep'
    write_replay(pa, a)
    write_replay(pb, b)
    ca, cb = tmp_path / 'ca', tmp_path / 'cb'
    if match is True:
        write_match(ca, MATCH)
        write_match(cb, dict(MATCH, record_id="v2", recorded_utc="t2",
                             game_uid="g2"))
    elif isinstance(match, tuple):
        write_match(ca, match[0])
        write_match(cb, match[1])
    rec = recs or cmp_rec(str(pa), str(pb))
    return ws.adjudicate([ca, cb], rec, str(pa), str(pb))


# --- Sol finding 1: first-frame-only evidence hides later divergence ---

def test_first_frame_gap_cannot_hide_later_two_sided_mismatch(tmp_path):
    # Old code saw only first-diff-frame only_a=[sync@5] and called it a
    # one-sided sync drop; f9 is a real two-sided sync mismatch.
    a = [sync_pkt(1), sync_pkt(5), sync_pkt(9, tag=1)]
    b = [sync_pkt(1), sync_pkt(9, tag=2)]
    verdict, detail = run_adj(a, b, tmp_path)
    assert verdict == "DIVERGENT"
    assert "both sides" in detail


# --- Sol finding 2: extra orders in tail are never artifacts/proof ---

def test_tail_order_is_never_adjudicated(tmp_path):
    # run4 shape: B's stream ends, A's tail holds syncs + a StartProduction.
    # Match records identical -> gate passes -> extras analysis must still
    # refuse: aggregates can never discharge a missing gameplay order.
    a = [sync_pkt(1), sync_pkt(2), sync_pkt(8),
         order_pkt(9, ('StartProduction', 0x88, 42)), sync_pkt(9)]
    b = [sync_pkt(1), sync_pkt(2), sync_pkt(8)]
    verdict, detail = run_adj(a, b, tmp_path)
    assert verdict == "DIVERGENT"
    assert "non-sync" in detail


def test_interior_one_sided_extra_is_divergence_not_tail(tmp_path):
    # A's extra sync at f5 is INSIDE the shared window (B's last frame is 9)
    # — both captures were recording there; not a teardown tail.
    a = [sync_pkt(1), sync_pkt(5), sync_pkt(9)]
    b = [sync_pkt(1), sync_pkt(9)]
    verdict, detail = run_adj(a, b, tmp_path)
    assert verdict == "DIVERGENT"
    assert "not strictly past" in detail


def test_pure_sync_tail_adjudicates(tmp_path):
    a = [sync_pkt(1), sync_pkt(2), sync_pkt(3), sync_pkt(4), sync_pkt(5)]
    b = [sync_pkt(1), sync_pkt(2), sync_pkt(3)]
    verdict, _ = run_adj(a, b, tmp_path)
    assert verdict == "ADJUDICATED_SYNC_DROP"


def test_sync_tail_with_differing_match_records_stays_divergent(tmp_path):
    # The match-record gate may only reject, never prove: differing
    # aggregates + a pure sync tail still cannot adjudicate.
    a = [sync_pkt(1), sync_pkt(2), sync_pkt(3), sync_pkt(4), sync_pkt(5)]
    b = [sync_pkt(1), sync_pkt(2), sync_pkt(3)]
    verdict, detail = run_adj(a, b, tmp_path,
                              match=(MATCH, dict(MATCH, outcome="lost")))
    assert verdict == "DIVERGENT"
    assert "match-record" in detail


def test_sync_tail_with_missing_match_records_stays_divergent(tmp_path):
    a = [sync_pkt(1), sync_pkt(2), sync_pkt(3), sync_pkt(4), sync_pkt(5)]
    b = [sync_pkt(1), sync_pkt(2), sync_pkt(3)]
    verdict, detail = run_adj(a, b, tmp_path, match=False)
    assert verdict == "DIVERGENT"
    assert "match-record" in detail


# --- Sol finding 3: empty/missing metadata is never evidence ---

def test_empty_match_records_both_sides_is_not_identical(tmp_path):
    ca, cb = tmp_path / 'ca', tmp_path / 'cb'
    for d in (ca, cb):
        (d / 'Logs').mkdir(parents=True)
        (d / 'Logs' / 'cameo-ai-matches.jsonl').write_text('')
    same, detail = ws.match_records_identical(ca, cb)
    assert same is False
    assert 'no match records' in detail


def test_adjudicate_rejects_incomplete_capture(tmp_path):
    pa, pb = tmp_path / 'a.orarep', tmp_path / 'b.orarep'
    write_replay(pa, [sync_pkt(1)])
    write_replay(pb, [sync_pkt(1)])
    rec = {"exit": 1, "verdict": "DIVERGENT",
           "incomplete": {str(pa): True, str(pb): False},
           "records": {str(pa): 5, str(pb): 5}}
    verdict, detail = ws.adjudicate([tmp_path], rec, str(pa), str(pb))
    assert verdict == "DIVERGENT"
    assert "incomplete" in detail


def test_adjudicate_requires_divergent_exit(tmp_path):
    pa, pb = tmp_path / 'a.orarep', tmp_path / 'b.orarep'
    write_replay(pa, [sync_pkt(1)])
    write_replay(pb, [sync_pkt(1)])
    rec = {"exit": 0, "verdict": "IDENTICAL",
           "incomplete": {str(pa): False, str(pb): False},
           "records": {str(pa): 5, str(pb): 5}}
    verdict, _ = ws.adjudicate([tmp_path], rec, str(pa), str(pb))
    assert verdict == "DIVERGENT"


def test_adjudicate_rejects_zero_records(tmp_path):
    pa, pb = tmp_path / 'a.orarep', tmp_path / 'b.orarep'
    write_replay(pa, [sync_pkt(1)])
    write_replay(pb, [sync_pkt(1)])
    rec = cmp_rec(str(pa), str(pb), nb=0)
    verdict, detail = ws.adjudicate([tmp_path], rec, str(pa), str(pb))
    assert verdict == "DIVERGENT"
    assert "nonpositive" in detail


# --- PASS shape: payload_is_proof (exit 0 alone is never enough) ---

A = r"C:\x\a.orarep"
B = r"C:\x\b.orarep"


def good_payload(**kw):
    p = {"verdict": "IDENTICAL",
         "records": {A: 100, B: 97},
         "incomplete": {A: False, B: False},
         "unparsed_tails": {A: 0, B: 0}}
    p.update(kw)
    return p


def test_wellformed_proof_accepted():
    assert ws.payload_is_proof(good_payload(), A, B) is True


def test_tail_flush_verdict_accepted():
    assert ws.payload_is_proof(
        good_payload(verdict="IDENTICAL_TAIL_FLUSH"), A, B) is True


def test_missing_incomplete_key_fails():
    p = good_payload()
    del p["incomplete"]
    assert ws.payload_is_proof(p, A, B) is False


def test_zero_record_count_fails():
    p = good_payload()
    p["records"][B] = 0
    assert ws.payload_is_proof(p, A, B) is False


def test_bool_record_count_fails():
    p = good_payload()
    p["records"][A] = True
    assert ws.payload_is_proof(p, A, B) is False


def test_true_incomplete_fails():
    p = good_payload()
    p["incomplete"][A] = True
    assert ws.payload_is_proof(p, A, B) is False


def test_nonzero_unparsed_fails():
    p = good_payload()
    p["unparsed_tails"][B] = 1
    assert ws.payload_is_proof(p, A, B) is False


def test_wrong_keyset_fails():
    p = good_payload()
    p["records"][r"C:\x\c.orarep"] = p["records"].pop(B)
    assert ws.payload_is_proof(p, A, B) is False


def test_nonproof_verdict_fails():
    assert ws.payload_is_proof(good_payload(verdict="DIVERGENT"), A, B) is False


def test_non_dict_payload_fails():
    assert ws.payload_is_proof("IDENTICAL", A, B) is False
    assert ws.payload_is_proof(None, A, B) is False
