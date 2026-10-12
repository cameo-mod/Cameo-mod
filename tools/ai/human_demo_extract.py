# Human-demonstration offline extractor — contract stages P0 + P1.
#
# Emits the canonical Phase-0 request-level record family defined by
# DESIGN_2026-10-12_human_demo_contract.md rev2 (Sol scoped-accepted at raw
# SHA 6b636db2…) section 14.2 and SPEC_2026-10-12_human_demo_extraction.md:
#
#   human_demo_inventory.jsonl — P0 sealed replay records
#     {schema, sha256, trailer, final_game_tick, human_seats, outcome_class,
#      disconnect_frames, last_sync_frame, game_uid, map_uid, demos, status,
#      notes}
#   human_demo_demos.jsonl — sealed demonstration/grouping table
#     (one per replay file sha256 x human seat; identity-adjacent provenance
#      lives HERE only, never in order records)
#   human_orders.jsonl — P1 canonical human-demo-orders/1 request records
#     (§14.2 field set verbatim; request-level only, status ladder all
#      "unknown" — orders alone never establish accepted/completed/spend)
#
# Hard rules honored:
#   - parse via order_stream_diff's canonical order parse ONLY
#     (parse_orders_detail); never order_trace (drops malformed orders).
#   - target_ref.kind none|actor|frozen_actor|terrain_cell|terrain_pos|
#     unsupported; parse ok|unsupported_target|unparsed_tail (§2.3).
#   - requested_item comes from TargetString ONLY for declared item-bearing
#     order strings — anything else stays null. A TargetString can carry a
#     session blob (SyncInfo) or free text (chat): never propagate those.
#   - censored records keep their reason and are never negative examples;
#     orders from the first unparsed byte onward are censored and the
#     demonstration is 'partial' (§8).
#   - identity: order records carry demo + renumbered seat only — no names,
#     no client indices, no game_uid/map_uid/fingerprint/paths/timestamps
#     (§3.4). The sealed demos file is the only place those live.
#   - request-only: no unit types, no ownership/liveness, no value/spend,
#     no wave sizes, no bank/fog/outcome inference (§14.1).
#
# Tool pins (CRLF-checkout sha256 of the exact reviewed bytes — same
# convention as wave1_scheduler.TOOL_SHA256): the repaired TargetType
# decoder lineage PLUS the unsupported_target surfacing patch that rides
# with this extractor's review. The d500-frozen comparator copies are STALE
# and barred; verify_tools() fails closed on anything but these bytes.
#
# Usage:
#   human_demo_extract.py <in.orarep|dir> [...] --out DIR
#   --summary-json PATH    also write the run summary to PATH
# Exit: 0 ok | 2 usage | 5 tool-pin mismatch | 6 unreadable input.
import hashlib, json, pathlib, struct, sys

_HERE = pathlib.Path(__file__).resolve().parent
_AUDIT = _HERE.parent / 'audit'
for _d in (_HERE, _AUDIT):
    if str(_d) not in sys.path:
        sys.path.insert(0, str(_d))
import order_stream_diff as osd           # canonical order parse (pinned)
import miniyaml                           # shared MiniYAML reader (pinned)

TOOL_SHA256 = {
    'tools/ai/order_stream_diff.py':
        '52883a81d8d2a4699c1551111e5fa0dbb72b1b718a8b1f348e82a954792ca05c',
    'tools/audit/miniyaml.py':
        'e9e46958683c8b1130caa0cdc0da0920fd0d371a863f99e03c2d65f40bfe50dc',
}

ORDER_SCHEMA = 'human-demo-orders/1'
INVENTORY_SCHEMA = 'human-demo-inventory/1'
DEMOS_SCHEMA = 'human-demo-demos/1'

# Order strings whose TargetString is an item name (production/placement
# class). Anything else with a TargetString is NOT an item request —
# SyncInfo carries a whole session blob incl. names, chat carries text.
ITEM_NAME_ORDERS = frozenset({
    'StartProduction', 'PauseProduction', 'CancelProduction',
    'PlaceBuilding', 'PlaceBuildingVariation', 'PlaceInfantry',
    'PlaceSupportPower', 'Deploy', 'Sell',
})

CENSOR_NONE = 'none'
CENSOR_DISCONNECT = 'after_disconnect_frame'
CENSOR_UNPARSED = 'from_unparsed_byte'
CENSOR_NO_SYNC = 'after_last_sync_frame'

STATUS_SEALED = 'sealed'
STATUS_PARTIAL = 'partial'
STATUS_EXCLUDED = 'excluded'

META_START = -1
META_END = -2
META_VERSION = 1

OUTCOME_MAP = {'Won': 'won', 'Lost': 'lost', 'Undefined': 'undecided'}


def _verify_tools():
    root = _HERE.parent.parent
    bad = []
    for rel, want in TOOL_SHA256.items():
        path = root / rel
        if not path.is_file():
            bad.append(f'{rel}: missing')
            continue
        got = hashlib.sha256(path.read_bytes()).hexdigest()
        if got != want:
            bad.append(f'{rel}: sha {got} != pinned {want}')
    if bad:
        sys.stderr.write('tool-pin mismatch (fail closed):\n  '
                         + '\n  '.join(bad) + '\n')
        sys.exit(5)


def _read_lenpref(b, p):
    return osd.read_str(b, p)  # canonical 7bit-length-prefixed string read


def parse_trailer(data):
    """Decode the ReplayMetadata trailer block; returns dict or None.

    Layout (ReplayMetadata.cs Write/Read): at file end
      [i32 dataLength][i32 -2]                         last 8 bytes
      [i32 -1][i32 version=1][len-prefixed utf8 yaml]  block of 8+dataLength
    The block's -1 start marker doubles as the order-stream terminator.
    Returns None when absent (killed/crashed replay) or structurally invalid.
    """
    if len(data) < 20:
        return None
    data_len, end = struct.unpack_from('<ii', data, len(data) - 8)
    if end != META_END or data_len <= 0 or data_len > len(data):
        return None
    block_start = len(data) - 8 - data_len - 8
    if block_start < 0:
        return None
    start, ver = struct.unpack_from('<ii', data, block_start)
    if start != META_START or ver != META_VERSION:
        return None
    try:
        raw, np = _read_lenpref(data, block_start + 8)
        if np != block_start + 8 + data_len:
            return None
        text = raw.decode('utf-8')
    except (IndexError, ValueError, UnicodeDecodeError):
        return None
    try:
        nodes = miniyaml.load_text(text, '<trailer>')
    except Exception:
        return None
    info = {'raw_present': True}
    players = []
    for node in nodes:
        base = node.key.split('@')[0]
        if base == 'Root':
            info['mod'] = node.get('Mod')
            info['version'] = node.get('Version')
            info['map_uid'] = node.get('MapUid')
            info['final_game_tick'] = _int_or(node.get('FinalGameTick'))
        elif base == 'Player':
            players.append({
                'client_index': _int_or(node.get('ClientIndex')),
                'is_human': _bool_or(node.get('IsHuman')),
                'is_bot': _bool_or(node.get('IsBot')),
                'faction_id': node.get('FactionId'),
                'team': _int_or(node.get('Team')),
                'outcome': OUTCOME_MAP.get(node.get('Outcome'), 'undecided'),
                'disconnect_frame': _int_or(node.get('DisconnectFrame')),
            })
            # Name, Fingerprint, SpawnPoint deliberately NOT extracted —
            # identity-class fields barred even from sealed provenance here.
    info['players'] = players
    return info


def _int_or(v):
    try:
        return int(v)
    except (TypeError, ValueError):
        return None


def _bool_or(v):
    if isinstance(v, str):
        return v.lower() == 'true'
    return None


def parse_session(target_string):
    """Decode a SyncInfo TargetString session blob (MiniYAML).

    Returns {game_uid, map_uid, humans:[client_index]} where humans are
    Client@N nodes with a Slot and no Bot field. Names/Fingerprints are
    never read out of the blob.
    """
    try:
        text = target_string.decode('utf-8')
        nodes = miniyaml.load_text(text, '<session>')
    except Exception:
        return {'game_uid': None, 'map_uid': None, 'humans': []}
    out = {'game_uid': None, 'map_uid': None, 'humans': []}
    for node in nodes:
        base = node.key.split('@')[0]
        if base == 'GlobalSettings':
            out['game_uid'] = node.get('GameUid')
            out['map_uid'] = node.get('Map')
        elif base == 'Client':
            slot = node.get('Slot')
            bot = node.get('Bot')
            idx = _int_or(node.get('Index'))
            if idx is None:
                idx = _int_or(node.key.split('@')[1])
            if idx is not None and slot and not bot:
                out['humans'].append(idx)
    if out['game_uid'] is None:
        # Fallback: scan any top-level node for a GameUid child — the field
        # name is stable even if the wrapping node name drifts.
        for node in nodes:
            v = node.get('GameUid')
            if v:
                out['game_uid'] = v
                break
    return out


def stream_scan(data):
    """Walk the canonical packet stream once.

    Returns dict(terminated, packets, first_unparsed_offset,
    last_sync_frame, disconnects, session) where packets is a list of
    (client, frame, detail_records, pkt_len<4 flag) in file order and
    disconnects is [{client_id, frame}] decoded per OrderIO.TryParseDisconnect
    (server packet: client tag 0, payload i32 clientId at offset 5).
    """
    packets = []
    disconnects = []
    session = {'game_uid': None, 'map_uid': None, 'humans': []}
    last_sync_frame = None
    first_unparsed = None
    terminated = False
    p = 0
    while p < len(data):
        if p + 4 > len(data):
            break
        client, = struct.unpack_from('<i', data, p)
        p += 4
        if client == -1:
            terminated = True
            break
        if p + 4 > len(data):
            break
        plen, = struct.unpack_from('<i', data, p)
        p += 4
        if plen < 0 or p + plen > len(data):
            break
        pkt = data[p:p + plen]
        p += plen
        if len(pkt) < 4:
            if first_unparsed is None:
                first_unparsed = p - plen
            packets.append((client, None, [], True))
            continue
        frame, = struct.unpack_from('<i', pkt, 0)
        if len(pkt) > 4 and pkt[4] == osd.PKT_SYNCHASH:
            last_sync_frame = frame
            continue
        if len(pkt) > 4 and pkt[4] == osd.PKT_DISCONNECT:
            # [i32 frame][0xBF][i32 clientId] (OrderIO.TryParseDisconnect)
            if len(pkt) >= 9:
                dc, = struct.unpack_from('<i', pkt, 5)
                disconnects.append({'client_id': dc, 'frame': frame})
            continue
        recs, tails = osd.parse_orders_detail(pkt)
        if tails and first_unparsed is None:
            first_unparsed = p - plen
        for r in recs:
            if (r.get('kind') == 'order' and r.get('parse') == 'ok'
                    and r.get('order') == b'SyncInfo'
                    and r.get('target_string')):
                session = parse_session(r['target_string'])
        packets.append((client, frame, recs, False))
    return {'terminated': terminated, 'packets': packets,
            'first_unparsed_offset': first_unparsed,
            'last_sync_frame': last_sync_frame,
            'disconnects': disconnects, 'session': session}


def _target_ref(rec):
    kind = rec.get('target_kind')
    if kind == 'unsupported':
        return {'kind': 'unsupported', 'ref': None}
    if kind in (None, 'none'):
        return {'kind': 'none', 'ref': None}
    return {'kind': kind, 'ref': rec.get('target_ref')}


def _requested_item(order_type, target_string):
    if order_type not in ITEM_NAME_ORDERS or target_string is None:
        return None
    try:
        item = target_string.decode('utf-8')
    except UnicodeDecodeError:
        return None
    # Item ids are single tokens; refuse anything blob/text-shaped.
    if '\n' in item or '\r' in item or len(item) > 128:
        return None
    return item


def _commanded_refs(rec):
    ids = set()
    if rec.get('subject') is not None:
        ids.add(rec['subject'])
    ids.update(rec.get('extra_actor_ids') or [])
    ids.update(rec.get('grouped_ids') or [])
    return sorted(ids)


def order_record(demo, seat, frame, rec, censor):
    """Canonical §14.2 human-demo-orders/1 record — request-level only."""
    order_b = rec.get('order')
    order_type = None
    if order_b is not None:
        order_type = order_b.decode('utf-8', 'replace')
    flags = rec.get('flags')
    item = _requested_item(order_type, rec.get('target_string'))
    refs = _commanded_refs(rec)
    return {
        'schema': ORDER_SCHEMA,
        'demo': demo,
        'seat': seat,
        'frame': frame,
        'order_type': order_type,
        'requested_item': item,
        'commanded_actor_refs': refs,
        'commanded_ref_count': len(refs),
        'target_ref': _target_ref(rec),
        'queued': bool(flags & 0x08) if flags is not None else None,
        'payload_raw': rec.get('undecoded_hex', rec.get('raw_hex', '')),
        'status': {'requested': True, 'accepted': 'unknown',
                   'completed': 'unknown', 'cancelled': 'unknown',
                   'rejected': 'unknown'},
        'censor': censor,
        'parse': rec.get('parse', 'ok'),
    }


def extract_file(path):
    """Run P0+P1 on one .orarep; returns (inventory, demos, orders)."""
    data = pathlib.Path(path).read_bytes()
    sha = hashlib.sha256(data).hexdigest()
    trailer = parse_trailer(data)
    scan = stream_scan(data)

    notes = []
    if not scan['terminated']:
        notes.append('stream unterminated (truncated/killed capture)')
    if scan['first_unparsed_offset'] is not None:
        notes.append('unparsed order bytes present')

    # --- seats -------------------------------------------------------
    if trailer is not None and trailer['players']:
        humans = sorted(pp['client_index'] for pp in trailer['players']
                        if pp['is_human'] and pp['client_index'] is not None)
        seat_source = 'trailer'
    elif scan['session']['humans']:
        humans = sorted(scan['session']['humans'])
        seat_source = 'session'
    else:
        humans = []
        seat_source = 'none'

    if trailer is None:
        trailer_status = 'ABSENT'
        final_tick = None
        map_uid = scan['session']['map_uid']
        players_by_client = {}
    else:
        trailer_status = 'present'
        final_tick = trailer['final_game_tick']
        map_uid = trailer['map_uid'] or scan['session']['map_uid']
        players_by_client = {pp['client_index']: pp
                             for pp in trailer['players']}

    # --- censor bounds -------------------------------------------------
    # trailer DisconnectFrame per seat (authoritative when present); stream
    # disconnects are the fallback for trailerless files.
    dc_frames = {}
    for d in scan['disconnects']:
        c = d['client_id']
        dc_frames[c] = min(dc_frames.get(c, d['frame']), d['frame'])
    seat_disconnect = {}
    for ci in humans:
        pp = players_by_client.get(ci)
        if (pp is not None and pp['disconnect_frame'] is not None
                and pp['disconnect_frame'] > 0):
            # Trailer DisconnectFrame is an int field defaulting to 0 —
            # only a positive frame is a real disconnect.
            seat_disconnect[ci] = pp['disconnect_frame']
        elif ci in dc_frames:
            seat_disconnect[ci] = dc_frames[ci]

    # --- inventory + exclusion -----------------------------------------
    excluded = None
    if trailer is not None and final_tick == 0:
        excluded = 'tick-0 death (FinalGameTick=0)'
    if trailer is not None and trailer['players'] and not humans:
        excluded = 'no human seats (bot-only or undecided seats)'
    if not humans and seat_source == 'none':
        excluded = 'no human seats derivable'

    status = STATUS_SEALED
    if excluded:
        status = STATUS_EXCLUDED
    elif (trailer is None or not scan['terminated']
          or scan['first_unparsed_offset'] is not None):
        status = STATUS_PARTIAL

    demos = []
    if not excluded:
        for k, ci in enumerate(humans):
            pp = players_by_client.get(ci, {})
            outcome = pp.get('outcome', 'UNKNOWN')
            dcf = seat_disconnect.get(ci)
            demos.append({
                'schema': DEMOS_SCHEMA,
                'demo': f'd_{sha[:12]}_s{k}',
                'sha256': sha,
                'game_uid': scan['session']['game_uid'],
                'match_group': scan['session']['game_uid'],
                'map_uid': map_uid,
                'cell_group': {'map_uid': map_uid, 'setup': None},
                'fingerprint_group': 'UNKNOWN',
                'demonstrator_group': None,   # retention undecided (§3.4)
                'replicate': 'not_applicable',
                'mirror_group': None,
                'client_index': ci,
                'seat': k,
                'faction_id': pp.get('faction_id'),
                'outcome': outcome,
                'disconnect_frame': dcf,
                'human_outcome': 'UNKNOWN' if dcf is not None else outcome,
                'seat_source': seat_source,
                'status': status,
            })

    # --- P1 order records ----------------------------------------------
    orders = []
    if not excluded:
        seat_of = {ci: k for k, ci in enumerate(humans)}
        demo_of = {ci: f'd_{sha[:12]}_s{k}' for k, ci in enumerate(humans)}
        unparsed_hit = False
        for client, frame, recs, short_pkt in scan['packets']:
            if short_pkt:
                unparsed_hit = True
                continue
            for rec in recs:
                if rec.get('kind') == 'handshake':
                    continue            # auth material — never emit
                censor = CENSOR_NONE
                if client in seat_disconnect and frame is not None \
                        and frame > seat_disconnect[client]:
                    censor = CENSOR_DISCONNECT
                if unparsed_hit:
                    censor = CENSOR_UNPARSED
                if trailer is None and scan['last_sync_frame'] is not None \
                        and frame is not None \
                        and frame > scan['last_sync_frame']:
                    censor = CENSOR_NO_SYNC
                if rec.get('parse') in ('unsupported_target',
                                        'unparsed_tail'):
                    unparsed_hit = True
                    if client not in demo_of:
                        continue      # a non-seat client's failure still
                                      # censors everything after it
                    censor = CENSOR_UNPARSED
                if client not in demo_of:
                    continue          # only human-seat requests are records
                orders.append(order_record(demo_of[client],
                                           seat_of[client], frame, rec,
                                           censor))

    outcomes = [d['outcome'] for d in demos]
    if not outcomes or all(o == 'UNKNOWN' for o in outcomes):
        outcome_class = 'UNKNOWN'
    elif all(o == 'won' for o in outcomes):
        outcome_class = 'human_won'
    elif all(o == 'lost' for o in outcomes):
        outcome_class = 'human_lost'
    elif all(o == 'undecided' for o in outcomes):
        outcome_class = 'undecided'
    else:
        outcome_class = 'mixed'

    inventory = {
        'schema': INVENTORY_SCHEMA,
        'sha256': sha,
        'replay_name': pathlib.Path(path).name,
        'trailer': trailer_status,
        'mod': trailer['mod'] if trailer else None,
        'version': trailer['version'] if trailer else None,
        'map_uid': map_uid,
        'final_game_tick': final_tick,
        'game_uid': scan['session']['game_uid'],
        'human_seats': humans,
        'human_seats_source': seat_source,
        'outcome_class': outcome_class,
        'disconnect_frames': [{'client_index': ci, 'frame': f}
                              for ci, f in sorted(seat_disconnect.items())],
        'stream_disconnects': scan['disconnects'],
        'last_sync_frame': scan['last_sync_frame'],
        'unparsed_stream': scan['first_unparsed_offset'] is not None,
        'terminated': scan['terminated'],
        'demos': [d['demo'] for d in demos],
        'status': status,
        'notes': notes + ([excluded] if excluded else []),
    }
    return inventory, demos, orders


def main(argv):
    args, out_dir, summary_path = [], None, None
    i = 0
    while i < len(argv):
        a = argv[i]
        if a == '--out':
            i += 1
            out_dir = argv[i] if i < len(argv) else None
        elif a == '--summary-json':
            i += 1
            summary_path = argv[i] if i < len(argv) else None
        elif a.startswith('--'):
            print(__doc__)
            return 2
        else:
            args.append(a)
        i += 1
    if not args or out_dir is None:
        print(__doc__)
        return 2
    _verify_tools()

    inputs = []
    for a in args:
        p = pathlib.Path(a)
        if p.is_dir():
            inputs += sorted(p.glob('*.orarep'))
        elif p.is_file():
            inputs.append(p)
        else:
            sys.stderr.write(f'unreadable input: {a}\n')
            return 6
    out = pathlib.Path(out_dir)
    out.mkdir(parents=True, exist_ok=True)

    inv_lines, demo_lines, order_lines = [], [], []
    for p in inputs:
        inv, demos, orders = extract_file(p)
        inv_lines.append(json.dumps(inv, sort_keys=False))
        demo_lines += [json.dumps(d, sort_keys=False) for d in demos]
        order_lines += [json.dumps(o, sort_keys=False) for o in orders]

    (out / 'human_demo_inventory.jsonl').write_text(
        '\n'.join(inv_lines) + ('\n' if inv_lines else ''), encoding='utf-8')
    (out / 'human_demo_demos.jsonl').write_text(
        '\n'.join(demo_lines) + ('\n' if demo_lines else ''), encoding='utf-8')
    (out / 'human_orders.jsonl').write_text(
        '\n'.join(order_lines) + ('\n' if order_lines else ''), encoding='utf-8')

    summary = {
        'inputs': len(inputs),
        'inventories': len(inv_lines),
        'demos': len(demo_lines),
        'orders': len(order_lines),
        'sealed': sum('"status": "sealed"' in l for l in inv_lines),
        'partial': sum('"status": "partial"' in l for l in inv_lines),
        'excluded': sum('"status": "excluded"' in l for l in inv_lines),
        'out': str(out),
    }
    if summary_path:
        pathlib.Path(summary_path).write_text(
            json.dumps(summary, indent=2), encoding='utf-8')
    print(json.dumps(summary))
    return 0


if __name__ == '__main__':
    sys.exit(main(sys.argv[1:]))
