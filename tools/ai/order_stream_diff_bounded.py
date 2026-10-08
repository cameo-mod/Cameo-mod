#!/usr/bin/env python3
r"""Outcome-bounded order-stream comparator — Phase-A of
SPEC_2026-10-08_deterministic_replay_cutoff.md (Architect ruling v2).

Companion to the frozen strict comparator (order_stream_diff.py, unchanged —
this tool imports it as a library so the wire-format parse is the approved
implementation byte-for-byte). Adds the deterministic terminal boundary:

  F_term  = first net frame whose SYNCHASH record carries the final
            defeatState mask (u64 at payload bytes 5-12 of the 13-byte sync
            body; Lost-bit per World.Players index — OrderManager.cs:265-275).
            Monotonic: bits only set, never clear, so M_term is the mask at
            the last observed sync and F_term its first reach.
  M_term  = that final mask value.
  B       = F_term + OrderLatency (L). Server projects incoming order frames
            by +OrderLatency (Server.cs:920-922) while sync packets skip the
            projection, so every order issued pre-decision lands <= B.
            Harness default: Local server => L=1 (Server.cs:136-137; the
            gameSpeed override at :1414-1415 is IsMultiplayer-gated).

Roster projection (Luna Req.1): mask bit i indexes World.Players, whose
order is [!Playable map PlayerReferences in map-definition order] ++
[lobby-seated clients in slot order] ++ [Everyone] (CreateMapPlayers.cs:96-
153; the server's worldPlayers mirrors it null-padded, Server.cs:1374-1397).
Trailer rows exist only for lobby clients, in world order. Combatants =
map refs with Bot set (map-side duelists) plus trailer rows whose map slot
is not NonCombatant. A set mask bit on a playable slot must correspond to a
trailer row with Outcome Lost; every trailer row must resolve Lost or Won.

Terminality (1v1 elimination scope): game-over fires when <=1 distinct
combatant side remains unresolved — for the 2-combatant harness roster that
is >=1 combatant Lost-bit set in M_term, corroborated by the trailer's
nonzero FinalGameTick (EndGame is only scheduled once CheckIfGameIsOver
passes — MissionObjectives.cs:164-174). Rosters with != 2 combatants are
out of scope and fail closed as NO_BOUNDARY.

Canonical ordered compare (Luna Req.4/5, Integrator's empirical check):
records are compared as a total order keyed (frame, clientId, intra-client
stream position). File order is server receipt order, not apply order —
the sim applies each client's frame packet sequentially in join order
(OrderManager.cs:234-260), so cross-client same-frame file interleave is
semantically commutative BY CONSTRUCTION and canonicalized away. Frame <= 0
(pregame lobby/handshake) is order-permuted by arrival races on real data —
compared as a multiset per Integrator's measurement.

Verdicts / exit codes:
  IDENTICAL                         0  canonical streams equal incl. <=B
                                        (boundary informational)
  IDENTICAL_OUTCOME_BOUNDED_TAIL    0  equal through B, extras only >B,
                                        boundary verified — PASS class for
                                        BASE attribution, outcome-scoped
  DIVERGENT                         1  canonical mismatch <= B (or pregame
                                        multiset mismatch) — wave-stop
  UNPARSED_TAILS                    3  opaque bytes on either side
  INCOMPLETE_CAPTURE                4  truncated stream / missing marker /
                                        missing sync frame <= B / no trailer
  NO_BOUNDARY                       5  no terminal mask transition,
                                        M_term==0, unresolved trailer
                                        outcome, or unresolvable roster
  BOUNDARY_MISMATCH                 5  F_term / M_term disagree across files
  TERMINAL_MASK_MISMATCH            5  mask bits inconsistent with the
                                        roster/trailer projection
  usage error                       2

Exit 0 verdicts are PASS classes; every other exit is wave-stop-invalid.
The strict comparator stays the mechanical gate upstream — this tool emits
additive boundary-scoped labels only.
"""
import argparse
import collections
import importlib.machinery
import importlib.util
import json
import pathlib
import struct
import sys
import zipfile

try:
    import order_stream_diff as _osd
except ImportError:
    # Fallback for hosts that import this module without tools/ai on
    # sys.path — load the frozen sibling file directly.
    _HERE = pathlib.Path(__file__).resolve().parent
    _sys_loader = importlib.machinery.SourceFileLoader(
        "order_stream_diff", str(_HERE / "order_stream_diff.py"))
    _spec = importlib.util.spec_from_loader(_sys_loader.name, _sys_loader)
    _osd = importlib.util.module_from_spec(_spec)
    _sys_loader.exec_module(_osd)


# ---------- replay trailer (post -1 marker) ----------

def read_trailer(path):
    """Parse the ReplayMetadata trailer: [-1][i32 version][len-prefixed
    MiniYaml GameInformation][i32 dataLength][-2]. Returns dict with
    root scalars + players list, or None when absent/malformed."""
    data = pathlib.Path(path).read_bytes()
    if len(data) < 20:
        return None
    dlen, endm = struct.unpack_from('<ii', data, len(data) - 8)
    if endm != -2 or dlen <= 0 or dlen > len(data):
        return None
    start = len(data) - (8 + dlen + 4 + 4)
    if start < 12:
        return None
    startm, ver = struct.unpack_from('<ii', data, start)
    if startm != -1 or ver != 1:
        return None
    slen, = struct.unpack_from('<i', data, start + 8)
    if slen <= 0 or slen > 100 * 1024 or start + 12 + slen > len(data):
        return None
    text = data[start + 12:start + 12 + slen].decode('utf-8', 'replace')
    return parse_miniyaml_players(text)


def parse_miniyaml_players(text):
    """Strict subset parse of GameInformation MiniYaml: top-level `Root:` /
    `Player@i:` nodes with one tab level of `Key: Value` children."""
    root, players = {}, []
    cur = None
    for raw in text.splitlines():
        if not raw.strip():
            continue
        if not raw.startswith(('\t', ' ')):
            name = raw.split(':', 1)[0]
            cur = 'player' if name.startswith('Player@') else (
                'root' if name == 'Root' else None)
            if cur == 'player':
                players.append({})
        elif cur == 'root' or cur == 'player':
            k, _, v = raw.strip().partition(':')
            (root if cur == 'root' else players[-1])[k.strip()] = v.strip()
    try:
        return {
            'final_game_tick': int(root.get('FinalGameTick', '0') or 0),
            'map_uid': root.get('MapUid'),
            'map_title': root.get('MapTitle'),
            'players': [{
                'client_index': int(p.get('ClientIndex', '-1') or -1),
                'name': p.get('Name'),
                'is_bot': p.get('IsBot') == 'True',
                'is_human': p.get('IsHuman') == 'True',
                'faction_id': p.get('FactionId'),
                'team': int(p.get('Team', '0') or 0),
                'outcome': p.get('Outcome'),
                'disconnect_frame': int(p.get('DisconnectFrame', '0') or 0),
            } for p in players],
        }
    except (ValueError, TypeError):
        return None


# ---------- map roster projection ----------

def read_map_players(map_path):
    """Ordered PlayerReference list from map.yaml (dir or .oramap zip).
    Returns [{'name','playable','noncombatant','bot'}, ...] or None."""
    p = pathlib.Path(map_path)
    if p.is_dir():
        f = p / 'map.yaml'
        if not f.is_file():
            return None
        text = f.read_text(encoding='utf-8', errors='replace')
    elif p.suffix == '.oramap' and p.is_file():
        try:
            with zipfile.ZipFile(p) as z:
                text = z.read('map.yaml').decode('utf-8', 'replace')
        except (KeyError, zipfile.BadZipFile):
            return None
    else:
        return None
    refs, cur, in_players = [], None, False
    for raw in text.splitlines():
        s = raw.rstrip()
        if not s.strip() or s.strip().startswith('#'):
            continue
        if not raw.startswith(('\t', ' ')):
            key = s.split(':', 1)[0].strip()
            if in_players and key.startswith('PlayerReference@'):
                refs.append({'name': key.split('@', 1)[1], 'playable': False,
                             'noncombatant': False, 'bot': None})
                cur = refs[-1]
                continue
            in_players = (key == 'Players')
            cur = None
            continue
        if not in_players:
            continue
        k, _, v = s.strip().partition(':')
        k, v = k.strip(), v.strip()
        if k.startswith('PlayerReference@'):
            refs.append({'name': k.split('@', 1)[1], 'playable': False,
                         'noncombatant': False, 'bot': None})
            cur = refs[-1]
        elif cur is not None:
            if k == 'Playable':
                cur['playable'] = v == 'True'
            elif k == 'NonCombatant':
                cur['noncombatant'] = v == 'True'
            elif k == 'Bot':
                cur['bot'] = v or None
    return refs or None


def world_player_layout(refs, n_trailer_players):
    """Reconstruct World.Players index -> descriptor list:
    [!Playable map refs in order] + [lobby clients (trailer rows)] +
    [Everyone] (CreateMapPlayers.cs:96-153; server worldPlayers mirrors it
    null-padded, Server.cs:1374-1397)."""
    layout = []
    for r in refs:
        if not r['playable']:
            layout.append({'kind': 'map', 'name': r['name'],
                           'combatant': bool(r['bot']) and not r['noncombatant'],
                           'trailer': None})
    for i in range(n_trailer_players):
        layout.append({'kind': 'lobby', 'name': None,
                       'combatant': None, 'trailer': i})
    layout.append({'kind': 'everyone', 'name': 'Everyone',
                   'combatant': False, 'trailer': None})
    return layout


def verify_mask(m_term, layout, trailer_players):
    """Project the terminal mask onto the reconstructed roster.
    Returns (ok, detail)."""
    bits = [i for i in range(64) if m_term & (1 << i)]
    combatants = [i for i, e in enumerate(layout) if e['combatant']]
    if len(combatants) != 2:
        return False, (f"unsupported roster topology: {len(combatants)} "
                       f"combatant slots (spec scope is 1v1 elimination)")
    # Trailer rows: resolve combatant status via the map slot the client
    # occupies — under this harness the single lobby slot is a noncombatant
    # referee/host; trailer rows are validated for resolved outcome only.
    for i, e in enumerate(layout):
        if e['kind'] != 'lobby':
            continue
        row = trailer_players[e['trailer']]
        if row['outcome'] not in ('Lost', 'Won'):
            return False, (f"trailer player {i} ({row['name']}) outcome "
                           f"{row['outcome']!r} — game not resolved")
        if i in bits and row['outcome'] != 'Lost':
            return False, (f"mask bit {i} set but trailer row "
                           f"{row['name']} outcome={row['outcome']}")
        if i not in bits and row['outcome'] == 'Lost':
            return False, (f"trailer row {row['name']} Lost but mask bit "
                           f"{i} clear — mask/trailer disagree")
    lost_combatants = [i for i in combatants if i in bits]
    if not lost_combatants:
        return False, f"no combatant Lost bit in terminal mask 0x{m_term:x}"
    if len(lost_combatants) > len(combatants):
        return False, "mask covers more combatants than exist"
    return True, (f"terminal mask 0x{m_term:x}: combatant loss "
                  f"{[layout[i]['name'] or f'slot{i}' for i in lost_combatants]},"
                  f" {len(combatants) - len(lost_combatants)} survivor(s) Won")


# ---------- boundary + compare ----------

def mask_of(rec):
    """defeatState u64 from a SYNCHASH record payload (hex of pkt[4:]:
    byte0=0x65, bytes1-4 hash, bytes5-12 defeat LE). None when the sync
    body is short — a pre-mask format cannot carry a boundary."""
    body = bytes.fromhex(rec[-1])
    return int.from_bytes(body[5:13], 'little') if len(body) >= 13 else None


def boundary(recs):
    """(F_term, M_term, monotone) from the sync stream: final mask + first
    frame reaching it. defeatState bits only ever set — a regressed mask is
    corrupt evidence and returns monotone=False; a mask-less sync returns
    F_term=None (no in-band boundary derivable)."""
    syncs = sorted((r[-3], mask_of(r)) for r in recs
                   if r[-2] == 'SYNCHASH' and r[-3] > 0)
    if not syncs or any(m is None for _, m in syncs):
        return None, None, True
    prev, monotone = 0, True
    for _, m in syncs:
        if prev & ~m:
            monotone = False
            break
        prev = m
    final = syncs[-1][1]
    first = min(f for f, m in syncs if m == final)
    return first, final, monotone


def canon_keyed(recs):
    """Ordered canonical sequence: (frame, client, intra-client position).
    Records are (client, frame, kind, payload) tuples in file order."""
    out = []
    counters = collections.Counter()
    for r in recs:
        f, c = r[-3], r[0]
        idx = counters[(f, c)]
        counters[(f, c)] += 1
        out.append(((f, c, idx), r))
    return sorted(out)


# Pregame orders whose emit COUNT is timing-dependent (connection-quality
# heartbeat) and carry no sim content. Verified on wave-5 real captures: the
# only frame<=0 multiset diffs were one-sided SyncConnectionQuality extras.
PREGAME_VOLATILE_ORDERS = frozenset({b'SyncConnectionQuality'})


def pregame_volatile(rec):
    """Frame<=0 record excluded from multiset equality: heartbeat orders
    whose count tracks lobby dwell time, not the simulation."""
    return rec[-2] == 'ORDER' and rec[-1][0] == 'O' and \
        rec[-1][1] in PREGAME_VOLATILE_ORDERS


def compare_stream(a_recs, b_recs, bound):
    """Canonical ordered compare for 1 <= frame <= bound plus multiset
    compare for frame <= 0 (volatile heartbeat orders excluded).
    Returns (equal, first_diff_frame, only_a, only_b) — fdiff 0 = pregame."""
    pre_a = [r for r in a_recs if r[-3] <= 0 and not pregame_volatile(r)]
    pre_b = [r for r in b_recs if r[-3] <= 0 and not pregame_volatile(r)]
    da = collections.Counter(pre_a) - collections.Counter(pre_b)
    db = collections.Counter(pre_b) - collections.Counter(pre_a)
    if da or db:
        return False, 0, list(da.elements()), list(db.elements())
    ca = [kv for kv in canon_keyed(a_recs) if 0 < kv[0][0] <= bound]
    cb = [kv for kv in canon_keyed(b_recs) if 0 < kv[0][0] <= bound]
    for (ka, ra), (kb, rb) in zip(ca, cb):
        if ka != kb or ra != rb:
            return False, min(ka[0], kb[0]), [ra], [rb]
    if len(ca) != len(cb):
        extra = ca[len(cb):] if len(ca) > len(cb) else cb[len(ca):]
        f = extra[0][0][0]
        only = [r for k, r in extra if k[0] == f]
        return (False, f, only if len(ca) > len(cb) else [],
                only if len(cb) > len(ca) else [])
    return True, None, [], []


def sync_coverage_ok(recs, bound):
    """Every frame 1..bound carries >=1 SYNCHASH (one per net frame is the
    engine contract; a gap inside the boundary is an incomplete capture)."""
    have = {r[-3] for r in recs if r[-2] == 'SYNCHASH'}
    missing = [f for f in range(1, bound + 1) if f not in have]
    return missing


def run(rep_a, rep_b, map_path, order_latency):
    res = {'a': str(rep_a), 'b': str(rep_b), 'order_latency': order_latency,
           'server_type': 'local(Launch.Map)'}
    fa_info, fb_info = {}, {}
    sides = {}
    for tag, path in (('a', rep_a), ('b', rep_b)):
        recs, unparsed, incomplete = _osd.extract(path, True, False, False)
        trailer = read_trailer(path)
        sides[tag] = {'recs': recs, 'unparsed': unparsed,
                      'incomplete': incomplete, 'trailer': trailer,
                      'n_records': len(recs)}
        (sides[tag]['f_term'], sides[tag]['m_term'],
         sides[tag]['monotone']) = boundary(recs)
    paths = {'a': str(rep_a), 'b': str(rep_b)}
    res['records'] = {paths[t]: s['n_records'] for t, s in sides.items()}
    res['unparsed_tails'] = {paths[t]: s['unparsed'] for t, s in sides.items()}
    res['incomplete'] = {paths[t]: s['incomplete'] for t, s in sides.items()}

    def finish(verdict, detail, exitcode):
        res.update(verdict=verdict, detail=detail)
        for t in ('a', 'b'):
            s = sides[t]
            res[f'{t}_boundary'] = {'f_term': s['f_term'],
                                    'm_term': s['m_term'],
                                    'n_records': s['n_records']}
        return res, exitcode

    if any(s['unparsed'] for s in sides.values()):
        return finish('UNPARSED_TAILS', 'opaque bytes in capture', 3)
    if any(s['incomplete'] for s in sides.values()):
        return finish('INCOMPLETE_CAPTURE', 'truncated or unterminated stream', 4)
    if any(s['n_records'] == 0 for s in sides.values()):
        return finish('INCOMPLETE_CAPTURE', 'zero gameplay records', 4)
    if any(s['trailer'] is None for s in sides.values()):
        return finish('INCOMPLETE_CAPTURE', 'missing/malformed metadata trailer', 4)
    if any(s['trailer']['final_game_tick'] <= 0 for s in sides.values()):
        return finish('NO_BOUNDARY', 'game never ended (FinalGameTick==0)', 5)
    if any(p['outcome'] == 'Undefined' for s in sides.values()
           for p in s['trailer']['players']):
        return finish('NO_BOUNDARY', 'trailer player outcome Undefined', 5)
    if any(not s['monotone'] for s in sides.values()):
        return finish('NO_BOUNDARY', 'defeatState mask regressed — corrupt '
                                     'evidence', 5)
    if any(s['f_term'] is None for s in sides.values()):
        return finish('NO_BOUNDARY', 'no in-band boundary — missing sync '
                                     'stream or mask-less sync format', 5)
    if any(not s['m_term'] for s in sides.values()):
        return finish('NO_BOUNDARY', 'M_term==0 (all-win/draw has no in-band '
                                     'marker in this scope)', 5)

    if sides['a']['f_term'] != sides['b']['f_term']:
        return finish('BOUNDARY_MISMATCH',
                      f"F_term differs: {sides['a']['f_term']} vs "
                      f"{sides['b']['f_term']}", 5)
    if sides['a']['m_term'] != sides['b']['m_term']:
        return finish('TERMINAL_MASK_MISMATCH',
                      f"M_term differs: 0x{sides['a']['m_term']:x} vs "
                      f"0x{sides['b']['m_term']:x}", 5)

    refs = read_map_players(map_path) if map_path else None
    if refs is None:
        return finish('TERMINAL_MASK_MISMATCH',
                      'roster unreconstructable — no map roster source', 5)
    for t in ('a', 'b'):
        layout = world_player_layout(refs, len(sides[t]['trailer']['players']))
        ok, detail = verify_mask(sides[t]['m_term'], layout,
                                 sides[t]['trailer']['players'])
        if not ok:
            return finish('TERMINAL_MASK_MISMATCH', f"side {t}: {detail}", 5)
    res['mask_verification'] = detail

    b_frame = sides['a']['f_term'] + order_latency
    res['B'] = b_frame
    for t in ('a', 'b'):
        missing = sync_coverage_ok(sides[t]['recs'], b_frame)
        if missing:
            return finish('INCOMPLETE_CAPTURE',
                          f"side {t}: sync gap at frame(s) {missing[:5]}"
                          f"{'...' if len(missing) > 5 else ''} <= B", 4)

    res['pregame_excluded'] = {
        t: dict(collections.Counter(
            r[-1][1].decode('utf-8', 'replace')
            for r in sides[t]['recs'] if r[-3] <= 0 and pregame_volatile(r)))
        for t in ('a', 'b')}
    equal, fdiff, only_a, only_b = compare_stream(
        sides['a']['recs'], sides['b']['recs'], b_frame)
    res['first_diff'] = fdiff
    res['only_a'] = [_osd.describe(r) for r in only_a]
    res['only_b'] = [_osd.describe(r) for r in only_b]
    tail_a = [r for r in sides['a']['recs'] if r[-3] > b_frame]
    tail_b = [r for r in sides['b']['recs'] if r[-3] > b_frame]
    res['tail_extras'] = {
        'a': dict(collections.Counter(r[-2] for r in tail_a)),
        'b': dict(collections.Counter(r[-2] for r in tail_b))}
    if not equal:
        return finish('DIVERGENT',
                      'pregame (frame<=0) multiset mismatch' if fdiff == 0
                      else f'ordered content differs at frame {fdiff} <= B',
                      1)
    if tail_a or tail_b:
        return finish('IDENTICAL_OUTCOME_BOUNDED_TAIL',
                      'streams equal through B; extras confined to post-'
                      'boundary frames', 0)
    return finish('IDENTICAL', 'canonical streams equal through B '
                               '(no post-boundary extras)', 0)


def main(argv=None):
    ap = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    ap.add_argument('replay_a')
    ap.add_argument('replay_b')
    ap.add_argument('--map', required=True,
                    help='map roster source: variant map dir or .oramap')
    ap.add_argument('--order-latency', type=int, default=1,
                    help='session OrderLatency (Launch.Map Local server = 1)')
    ap.add_argument('--json', action='store_true')
    args = ap.parse_args(argv)
    res, code = run(args.replay_a, args.replay_b, args.map,
                    args.order_latency)
    if args.json:
        print(json.dumps(res, indent=2, default=str))
    else:
        print(f"{res['verdict']}: {res['detail']}")
        mt = res['a_boundary']['m_term']
        print(f"  B={res.get('B')} F_term={res['a_boundary']['f_term']} "
              f"M_term={'-' if mt is None else hex(mt)} "
              f"L={res['order_latency']}")
    return code


if __name__ == '__main__':
    sys.exit(main())
