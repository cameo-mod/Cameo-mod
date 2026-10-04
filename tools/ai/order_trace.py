# AR-S order-rate trace: parse an .orarep order stream and report per-unit
# alternating-order rates (stutter metric: >=2 alternations/sec on one unit).
# Replay layout (ReplayConnection): [i32 client][i32 packetLen][packet]*
# until client == -1. Packet = [i32 frame][serialized Order*].
# Order (Order.Serialize): u8 type (0xFF Fields / 0xFE Handshake) +
#   7bit-len string OrderString + i16 flags + flag-gated fields.
import struct, sys, collections, json

MOVE_ORDERS = {"Move", "AttackMove", "AssaultMove", "Stop", "MoveWithinRange",
               "DeployForPrimaryAttack", "ForceMove"}

def read_str(b, p):
    # .NET BinaryWriter string: 7-bit-encoded int length + UTF-8
    ln = 0; shift = 0
    while True:
        byte = b[p]; p += 1
        ln |= (byte & 0x7F) << shift
        if not (byte & 0x80):
            break
        shift += 7
    s = b[p:p+ln].decode('utf-8', 'replace')
    return s, p + ln

def parse_orders(pkt):
    """Yield (orderString, subject_id, grouped_ids) from a packet body."""
    p = 4
    out = []
    while p < len(pkt):
        t = pkt[p]; p += 1
        if t == 0xFE:  # Handshake: two strings
            _, p = read_str(pkt, p)
            _, p = read_str(pkt, p)
            continue
        if t != 0xFF:  # not a Fields order — bail on this packet
            break
        order, p = read_str(pkt, p)
        flags, = struct.unpack_from('<h', pkt, p); p += 2
        subject = None
        if flags & 0x80:
            subject, = struct.unpack_from('<I', pkt, p); p += 4
        if flags & 0x01:  # Target
            tt = pkt[p]; p += 1
            if tt == 0:      # Actor: u32 + i32
                p += 8
            elif tt == 1:    # FrozenActor: u32 + u32
                p += 8
            elif tt == 2:    # Terrain
                if flags & 0x40:  # TargetIsCell: i32 + u8
                    p += 5
                else:
                    p += 12
                    n, = struct.unpack_from('<h', pkt, p); p += 2
                    if n != -1:
                        p += 12 * n
        if flags & 0x04:  # TargetString
            _, p = read_str(pkt, p)
        if flags & 0x02:  # ExtraActors
            n, = struct.unpack_from('<i', pkt, p); p += 4 + 4 * n
        if flags & 0x10:  # ExtraLocation
            p += 4
        if flags & 0x20:  # ExtraData
            p += 4
        grouped = None
        if flags & 0x100:  # Grouped
            n, = struct.unpack_from('<i', pkt, p); p += 4
            grouped = struct.unpack_from(f'<{n}I', pkt, p); p += 4 * n
        out.append((order, subject, grouped))
    return out

def trace(path):
    units = collections.defaultdict(list)   # unit_id -> [(frame, order)]
    raw_counts = collections.Counter()
    with open(path, 'rb') as f:
        data = f.read()
    p = 0
    meta_start = -1
    while p < len(data):
        client, = struct.unpack_from('<i', data, p); p += 4
        if client == meta_start:
            break
        plen, = struct.unpack_from('<i', data, p); p += 4
        pkt = data[p:p+plen]; p += plen
        if len(pkt) < 4:
            continue
        frame, = struct.unpack_from('<i', pkt, 0)
        if frame <= 0:
            continue
        if len(pkt) > 4 and pkt[4] in (0xBF, 0x65):  # Disconnect / SyncHash
            continue
        for order, subject, grouped in parse_orders(pkt):
            raw_counts[order] += 1
            if order not in MOVE_ORDERS:
                continue
            if subject is not None:
                units[subject].append((frame, order))
            if grouped:
                for uid in grouped:
                    units[uid].append((frame, order))
    return units, raw_counts

def report(path, label):
    units, raw = trace(path)
    flagged = []
    total_alts = 0
    for uid, seq in units.items():
        seq.sort()
        # alternation = consecutive order with a different order string
        alt_frames = [seq[i][0] for i in range(1, len(seq)) if seq[i][1] != seq[i-1][1]]
        total_alts += len(alt_frames)
        # worst 25-tick window of alternations (1 sim-second; gamespeed
        # changes wall pacing only, the sim rate is fixed at 25 tps)
        best = 0
        j = 0
        for i, fr in enumerate(alt_frames):
            while alt_frames[j] < fr - 25:
                j += 1
            best = max(best, i - j + 1)
        if best >= 2:
            flagged.append((uid, best, len(seq), len(alt_frames)))
    flagged.sort(key=lambda x: -x[1])
    span = max((f for seq in units.values() for f, _ in seq), default=0)
    print(f"[{label}] units_ordered={len(units)} alternations={total_alts} "
          f"stutter_units(>=2alt/s)={len(flagged)} max_frame={span}")
    for uid, best, n, alts in flagged[:12]:
        print(f"    unit {uid}: peak {best} alt/s, {alts} alternations over {n} orders")
    print(f"    order mix: {dict(raw.most_common(8))}")
    return {"units_ordered": len(units), "alternations": total_alts,
            "stutter_units": len(flagged), "top": flagged[:20]}

if __name__ == '__main__':
    out = {}
    for path, label in zip(sys.argv[1::2], sys.argv[2::2]):
        out[label] = report(path, label)
    if len(sys.argv) > 4:
        print(json.dumps({k: {kk: vv for kk, vv in v.items() if kk != 'top'}
                          for k, v in out.items()}, indent=1))
