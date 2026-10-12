# Demo timestamps: frames where large grouped orders fire (army marches/engagements).
# Times are sim ticks; replay playback at normal speed = tick/25 seconds.
import sys, struct, collections
sys.path.insert(0, r"C:\cameo-wt\ar2-desync")
from order_trace import parse_orders

def events(path, min_group=8):
    with open(path, 'rb') as f:
        data = f.read()
    p = 0
    out = []
    while p < len(data):
        client, = struct.unpack_from('<i', data, p); p += 4
        if client == -1:
            break
        plen, = struct.unpack_from('<i', data, p); p += 4
        pkt = data[p:p+plen]; p += plen
        if len(pkt) < 4:
            continue
        frame, = struct.unpack_from('<i', pkt, 0)
        if frame <= 0:
            continue
        if len(pkt) > 4 and pkt[4] in (0xBF, 0x65):
            continue
        try:
            orders = parse_orders(pkt)
        except Exception:
            continue
        for order, subject, grouped, tkey, tstr in orders:
            if grouped and len(grouped) >= min_group and order in ("Move", "AttackMove"):
                out.append((frame, order, len(grouped)))
    return out

for path in sys.argv[1:]:
    label = path.split("\\")[-1]
    ev = events(path)
    eps = []
    for f, o, n in sorted(ev):
        if eps and f - eps[-1][-1][0] <= 50:
            eps[-1].append((f, o, n))
        else:
            eps.append([(f, o, n)])
    print(f"[{label}] big-order episodes ({len(eps)}):")
    for ep in eps:
        mx = max(ep, key=lambda e: e[2])
        print(f"    {mx[0]} (~{mx[0]//25:>3}s): {mx[1]} x{mx[2]} actors" +
              (f"  episode {ep[0][0]}-{ep[-1][0]}" if len(ep) > 1 else ""))
