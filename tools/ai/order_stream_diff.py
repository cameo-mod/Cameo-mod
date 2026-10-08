# BASE==BASE order-stream comparator for the BOT-DETERMINISM proof.
# Compares the canonical order streams of two .orarep replays and reports
# IDENTICAL or the first divergent record (with frame + context).
#
# Replay layout (ReplayConnection): [i32 client][i32 packetLen][packet]*
# until client == -1. Packet = [i32 frame][serialized Order*].
# Order (Order.Serialize): u8 type (0xFF Fields / 0xFE Handshake) +
#   7bit-len string OrderString + i16 flags + flag-gated fields.
#
# Per-launch fields are normalized before comparison — they differ between
# runs even when every decision is identical:
#   GlobalSettings.GameUid  (Guid.NewGuid per launch, Server.cs ctor)
#   Handshake AuthToken / AuthSignature (per-launch auth challenge)
# Everything else — every order byte, subject, target, extra field, sync-hash
# payload — must match byte-for-byte in the same order for the runs to pass.
#
# Usage:
#   order_stream_diff.py A.orarep B.orarep [--pregame] [--ignore-client]
#                        [--ignore-synchash] [--json]
# Default scope is gameplay frames only (frame >= 1); --pregame also compares
# the lobby/handshake orders (frame <= 0) with the normalizations above.
import hashlib, json, re, struct, sys

PER_LAUNCH_FIELDS = re.compile(
    r'((?:GameUid|AuthToken|AuthSignature)\s*:\s*)[^\r\n]+')

ORDER_FIELDS = 0xFF
ORDER_HANDSHAKE = 0xFE
PKT_DISCONNECT = 0xBF
PKT_SYNCHASH = 0x65


def read_str(b, p):
    ln = 0
    shift = 0
    while True:
        byte = b[p]
        p += 1
        ln |= (byte & 0x7F) << shift
        if not (byte & 0x80):
            break
        shift += 7
    s = b[p:p + ln].decode('utf-8', 'replace')
    return s, p + ln


def parse_orders(pkt):
    """Yield canonical tuples for the Field/Handshake orders in one packet."""
    p = 4
    out = []
    while p < len(pkt):
        t = pkt[p]
        p += 1
        if t == ORDER_HANDSHAKE:
            name, p = read_str(pkt, p)
            target, p = read_str(pkt, p)
            out.append(('HS', name, PER_LAUNCH_FIELDS.sub(r'\1<NORM>', target)))
            continue
        if t != ORDER_FIELDS:
            break
        order, p = read_str(pkt, p)
        flags, = struct.unpack_from('<h', pkt, p)
        p += 2
        subject = None
        if flags & 0x80:
            subject, = struct.unpack_from('<I', pkt, p)
            p += 4
        target_hex = ''
        if flags & 0x01:
            start = p
            tt = pkt[p]
            p += 1
            if tt in (0, 1):      # Actor u32+i32 / FrozenActor u32+u32
                p += 8
            elif tt == 2:         # Terrain
                if flags & 0x40:  # TargetIsCell: i32 + u8
                    p += 5
                else:
                    p += 12
                    n, = struct.unpack_from('<h', pkt, p)
                    p += 2
                    if n != -1:
                        p += 12 * n
            target_hex = pkt[start:p].hex()
        target_string = None
        if flags & 0x04:
            target_string, p = read_str(pkt, p)
            target_string = PER_LAUNCH_FIELDS.sub(r'\1<NORM>', target_string)
        extra_hex = ''
        start = p
        if flags & 0x02:          # ExtraActors: i32 n + n u32
            n, = struct.unpack_from('<i', pkt, p)
            p += 4 + 4 * n
        if flags & 0x10:          # ExtraLocation: i32
            p += 4
        if flags & 0x20:          # ExtraData: i32
            p += 4
        extra_hex = pkt[start:p].hex()
        grouped_hex = ''
        if flags & 0x100:
            n, = struct.unpack_from('<i', pkt, p)
            p += 4
            grouped_hex = pkt[p:p + 4 * n].hex()
            p += 4 * n
        out.append(('O', order, subject, target_hex, target_string,
                    extra_hex, grouped_hex))
    return out


def extract(path, pregame, ignore_client, ignore_synchash):
    """Return the canonical record list for one .orarep file."""
    with open(path, 'rb') as f:
        data = f.read()
    records = []
    p = 0
    while p < len(data):
        client, = struct.unpack_from('<i', data, p)
        p += 4
        if client == -1:
            break
        plen, = struct.unpack_from('<i', data, p)
        p += 4
        pkt = data[p:p + plen]
        p += plen
        if len(pkt) < 4:
            continue
        frame, = struct.unpack_from('<i', pkt, 0)
        if not pregame and frame <= 0:
            continue
        head = () if ignore_client else (client,)
        if len(pkt) > 4 and pkt[4] == PKT_SYNCHASH:
            if not ignore_synchash:
                records.append(head + (frame, 'SYNCHASH', pkt[4:].hex()))
            continue
        if len(pkt) > 4 and pkt[4] == PKT_DISCONNECT:
            records.append(head + (frame, 'DISCONNECT', pkt[4:].hex()))
            continue
        orders = parse_orders(pkt)
        if orders:
            records.append(head + (frame, 'ORDERS', tuple(orders)))
        elif len(pkt) > 4:
            records.append(head + (frame, 'OTHER', pkt[4:].hex()))
    return records


def describe(rec):
    """Human-readable one-liner for a canonical record."""
    kind = rec[-2] if rec[-2] in ('SYNCHASH', 'DISCONNECT', 'ORDERS', 'OTHER') else None
    if kind == 'ORDERS':
        orders = rec[-1]
        names = [o[1] for o in orders]
        return f"frame={rec[len(rec)-3]} ORDERS {names[:6]}{'...' if len(names) > 6 else ''}"
    return f"frame={rec[len(rec)-3]} {kind} {str(rec[-1])[:80]}"


def main(argv):
    args = [a for a in argv if not a.startswith('--')]
    flags = set(a for a in argv if a.startswith('--'))
    if len(args) != 2:
        print(__doc__)
        return 2
    pregame = '--pregame' in flags
    ignore_client = '--ignore-client' in flags
    ignore_synchash = '--ignore-synchash' in flags
    want_json = '--json' in flags

    streams = {path: extract(path, pregame, ignore_client, ignore_synchash)
               for path in args}
    digests = {path: hashlib.sha256(repr(recs).encode()).hexdigest()[:16]
               for path, recs in streams.items()}

    a, b = args
    ra, rb = streams[a], streams[b]
    result = {'records': {a: len(ra), b: len(rb)},
              'sha256_16': digests}

    first_diff = None
    for i in range(min(len(ra), len(rb))):
        if ra[i] != rb[i]:
            first_diff = i
            break
    if first_diff is None and len(ra) != len(rb):
        first_diff = min(len(ra), len(rb))

    if first_diff is None:
        result['verdict'] = 'IDENTICAL'
        print(f"IDENTICAL — {len(ra)} canonical records, "
              f"sha256/16 {digests[a]}")
        if want_json:
            print(json.dumps(result))
        return 0

    result['verdict'] = 'DIVERGENT'
    result['first_diff_index'] = first_diff
    print(f"DIVERGENT at record {first_diff} "
          f"(of {len(ra)}/{len(rb)}), sha256/16 {digests[a]} vs {digests[b]}")
    lo = max(0, first_diff - 3)
    for i in range(lo, min(first_diff + 2, len(ra))):
        mark = '>>' if i == first_diff else '  '
        print(f"  {mark} A[{i}] {describe(ra[i])}")
    for i in range(lo, min(first_diff + 2, len(rb))):
        mark = '>>' if i == first_diff else '  '
        print(f"  {mark} B[{i}] {describe(rb[i])}")
    if want_json:
        result['a_record'] = repr(ra[first_diff]) if first_diff < len(ra) else None
        result['b_record'] = repr(rb[first_diff]) if first_diff < len(rb) else None
        print(json.dumps(result))
    return 1


if __name__ == '__main__':
    sys.exit(main(sys.argv[1:]))
