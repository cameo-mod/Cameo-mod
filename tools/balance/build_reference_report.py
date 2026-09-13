#!/usr/bin/env python3
"""build_reference_report.py — the reference map as a reviewable HTML page.

⭐ ORIGINALS AND EXPANSIONS ARE TWO DIFFERENT REPORTS (maintainer, 2026-09-07). Mixing them is
what made the first page unreadable. An ORIGINAL exists in OpenRA/OpenTD, so a counterpart
provably exists in every source: all three references must be present and must be the same unit,
and every row is checkable against the original game. An EXPANSION exists only in Cameo, Combined
Arms or DTA; it cannot always have three references, and holding it to the same standard buries
the rows that are genuinely wrong among rows that never could be right.

The two bands are therefore separated, counted and captioned apart, so a review pass over the
originals is a finite, decidable job.

    python tools/balance/build_reference_report.py --faction td_gdi td_nod ra1_allies ra1_soviets
"""
from __future__ import annotations

import argparse
import html
import json
import re
import pathlib
import sys

HERE = pathlib.Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
import reference_distribution as rd          # noqa: E402
import reference_targets as rt               # noqa: E402

ROOT = rd.ROOT
ASSIGN = ROOT / "docs/balance/derived/reference_assignment.json"
ORIGINAL_SOURCES = ("OpenRA Red Alert", "OpenRA Tiberian Dawn",
                    "OpenRA Tiberian Sun", "Romanov's Vengeance")
SECTIONS = (("infantry", "Infantry"), ("vehicle", "Vehicles"), ("aircraft", "Aircraft"),
            ("ship", "Naval"), ("defense", "Defenses"))
CONF_ORDER = {"STRONG": 0, "FAIR": 1, "SHAPE": 2, "WEAK": 3}
UNARMED_COUNTERPARTS = {('Combined Arms', 'SPY'), ('DTA Enhanced', 'SPY'), ('DTA Classic', 'SPY')}

STYLE = """
.cls{font-size:11px;color:var(--mut);white-space:nowrap}
.evidence{display:block;font-size:10px;font-weight:400;color:var(--mut)}
:root{--bg:#f7f6f3;--fg:#1b1a17;--mut:#6f6a60;--line:#ddd8cd;--card:#fffefb;--accent:#8a5a2b;
--strong:#1f6b4a;--fair:#7a6320;--shape:#4a5a78;--weak:#8a4a3c;--bad:#a3312a;--tgt:#2e5c8a;}
@media (prefers-color-scheme:dark){:root:not([data-theme=light]){--bg:#161513;--fg:#eae6dd;
--mut:#9b948a;--line:#33302a;--card:#1e1c19;--accent:#d8a56a;--strong:#5fbf8f;--fair:#c9a94a;
--shape:#8fa8cc;--weak:#d08a78;--bad:#e0736a;--tgt:#7fb0e0;}}
:root[data-theme=dark]{--bg:#161513;--fg:#eae6dd;--mut:#9b948a;--line:#33302a;--card:#1e1c19;
--accent:#d8a56a;--strong:#5fbf8f;--fair:#c9a94a;--shape:#8fa8cc;--weak:#d08a78;--bad:#e0736a;
--tgt:#7fb0e0;}
body{background:var(--bg);color:var(--fg);font:14px/1.5 ui-sans-serif,system-ui,sans-serif;
margin:0;padding:28px clamp(12px,4vw,56px);}
h1{font-size:1.6rem;margin:0 0 4px;letter-spacing:-.01em}
h2{margin:34px 0 6px;font-size:1.15rem;border-bottom:2px solid var(--line);padding-bottom:5px}
h2.band{border-bottom:none;color:var(--accent);font-size:.95rem;text-transform:uppercase;
letter-spacing:.1em;margin:26px 0 2px}
h3{margin:18px 0 6px;font-size:.8rem;text-transform:uppercase;letter-spacing:.09em;color:var(--mut)}
.muted{color:var(--mut);font-weight:400;text-transform:none;letter-spacing:0}
.lede{color:var(--mut);max-width:78ch;margin:0 0 10px}
.wrap{overflow-x:auto}
table{border-collapse:collapse;width:100%;background:var(--card);border:1px solid var(--line);
border-radius:7px;overflow:hidden;margin-bottom:6px}
th,td{padding:6px 9px;text-align:left;border-bottom:1px solid var(--line);vertical-align:top}
th{font-size:.72rem;text-transform:uppercase;letter-spacing:.06em;color:var(--mut);font-weight:600}
tr:last-child td{border-bottom:none}
.n{text-align:right;font-variant-numeric:tabular-nums;white-space:nowrap}
.t{color:var(--tgt);font-weight:600}
code{font:12px/1.4 ui-monospace,SFMono-Regular,Menlo,monospace}
.chip{display:inline-block;border:1px solid var(--line);border-radius:5px;padding:1px 6px;
margin:1px 3px 1px 0;font-size:12px;white-space:nowrap}
.chip i{font-style:normal;color:var(--mut)}
.chip.strong{border-left:3px solid var(--strong)}
.chip.fair{border-left:3px solid var(--fair)}
.chip.shape{border-left:3px solid var(--shape)}
.chip.weak{border-left:3px solid var(--weak)}
.chip.fam{border-style:dashed;color:var(--mut)}
.bad{color:var(--bad)}
.warn{color:var(--fair)}
.tag{font-size:11px;color:var(--mut);border:1px dashed var(--line);border-radius:4px;padding:0 4px}
"""


def num(v, dash="—"):
    return f"{v:,.0f}" if isinstance(v, (int, float)) and v else dash


def is_original(srcs):
    """An actor is an ORIGINAL when an original-shipping mod matched it BY NAME.

    That is the maintainer's own rule made mechanical: OpenRA Red Alert and Tiberian Dawn ship
    the original rosters and nothing else, so a name-backed match against one of them is proof
    the unit existed in the original game — and therefore proof that DTA and Combined Arms, both
    supersets, must have it too.
    """
    return any(d.get("confidence") in ("STRONG", "FAIR") and s in ORIGINAL_SOURCES
               for s, d in (srcs or {}).items())


def arm_note(actor, led_arms):
    """`x3` beside a damage/tick value with more than one priced armament.

    The value shown is the HARDEST-HITTING armament, never the sum — 495 of 822 armed actors carry
    several, and `ra2_allies_ifv` carries 39 mutually-exclusive ones. Without this marker the
    reader cannot tell a single-gun tank from one whose other weapons are conditional, which is
    exactly the question the maintainer asked about `td_nod_lighttankmkii`.
    """
    n = led_arms.get(actor, 0)
    if n <= 1:
        return ""
    # ⭐ SAY HOW MANY FIRE TOGETHER, not just how many exist. `x4` on the mammoth was answering a
    # question nobody asked: it has four priced armaments and can only ever fire two, because the
    # cannons and the missiles are each an upgrade pair gated on `C` / `!C`. Reading "x4" as "this
    # tank has four guns" is the mistake the maintainer was worried about when they asked whether
    # a single collapsed damage number would be better.
    sim, total = simultaneous_armaments(actor)
    if sim and total and sim < total:
        return (f'<span class="muted" title="{total} priced armaments, but only {sim} can fire at '
                f'once — the rest are mutually exclusive upgrade variants gated on a condition. '
                f'The figure shown is ONE armament, never a sum.">{sim} of {total} guns</span>')
    return (f'<span class="muted" title="{n} priced armaments, all able to fire; the figure shown '
            f'is ONE armament, never a sum">&#215;{n}</span>')


_SIM_CACHE = {}
_RULESET = []


def _ruleset():
    """The resolved ruleset, loaded once — the only place armament CONDITIONS exist."""
    if not _RULESET:
        sys.path.insert(0, str(ROOT / "tools" / "audit"))
        import miniyaml
        _RULESET.append(miniyaml.Ruleset(str(ROOT)))
    return _RULESET[0]


def simultaneous_armaments(actor):
    """(fire together, priced total) — because `x4` on the mammoth means neither.

    ⛔ THE PRICED COUNT IS NOT THE SIMULTANEOUS COUNT, and the maintainer spotted the problem
    from the other end: *"it gets more complicated for dual weapons then like the mammoth tank
    dual cannons and dual missiles"*. `td_gdi_mammothtank` carries four priced armaments and
    fires exactly TWO of them, ever:

        Armament@PRIMARY                    !td_gdi_upgrade_highvelocitycannons
        Armament@HV                          td_gdi_upgrade_highvelocitycannons
        Armament@SECONDARY                  !td_gdi_upgrade_advancedmissiletargeting
        Armament@AdvancedMissileTargeting    td_gdi_upgrade_advancedmissiletargeting

    Two mutually exclusive PAIRS — one cannon and one missile launcher, whichever upgrades are
    held. Summing four would describe a tank that cannot exist, which is exactly why the damage
    column shows one armament rather than a total.

    The rule is the condition's polarity: `C` and `!C` are the same slot in two states, so a
    group keyed on the condition with any leading `!` stripped contributes ONE. Unconditioned
    armaments each contribute one. ⚠ The LEDGER cannot answer this — its armament records carry
    `requires_condition: None` for all four — so it is read from the resolved yaml, through
    `miniyaml` rather than by hand.
    """
    if actor in _SIM_CACHE:
        return _SIM_CACHE[actor]
    try:
        node = _ruleset().resolve(actor)
    except Exception:                                   # a missing actor is not a report failure
        node = None
    if node is None:
        return _SIM_CACHE.setdefault(actor, (None, None))
    groups, loose = set(), 0
    total = 0
    for child in node.children:
        if not child.key.startswith("Armament"):
            continue
        fields = {g.key: g.value for g in child.children}
        if not fields.get("Weapon"):
            continue
        total += 1
        cond = (fields.get("RequiresCondition") or "").strip()
        if cond:
            groups.add(cond.lstrip("!").strip())
        else:
            loose += 1
    return _SIM_CACHE.setdefault(actor, (len(groups) + loose, total))


def burst_note(cameo_row):
    """`(x2)` after a per-shot damage figure — the maintainer's own format.

    Requested 2026-09-12: *"show the damage per shot and then in brackets behind it the bursts
    for example 10k damage (2x) which means the total damage is 2x of 10k so 20k"*. The column
    now holds damage PER SHOT on both sides (see `reference_distribution.cameo_rows`), so the
    bracket is the multiplier that reconstructs the per-cycle total, and nothing is hidden.

    ⭐ BURST STAYS VISIBLE RATHER THAN FOLDED IN, and that is deliberate. `Burst` is a separately
    referenced component under R1 and may not be changed without explicit permission, so a column
    that silently multiplied it away would hide the one number that needs sign-off. It also makes
    the rate legible at a glance: damage x burst over the cycle IS the formula.
    """
    burst = float(cameo_row.get("w_burst") or 1)
    dmg = cameo_row.get("w_damage")
    if burst <= 1 or not dmg:
        return ""
    return (f'<span class="muted" title="per cycle: {dmg:,.0f} x {burst:.0f} '
            f'= {dmg * burst:,.0f}">(&#215;{burst:.0f})</span>')


def recovered_burst_delay(row):
    """Ticks between shots INSIDE a burst, recovered from the row's own identity.

    ⛔ IT IS RECOVERED, NOT READ, and that is the only safe way to get it. No source publishes a
    usable per-shot delay on the unit row, and `w_damage` does not even mean the same thing on
    both sides of the map — per SHOT in `extract_peer_units`, BURST-INCLUSIVE in the frozen Cameo
    snapshot. `reference_targets.recover_burst_time` sidesteps that by never assuming either: the
    cycle falls out of `damage / dps` whatever those two mean, and the burst time is whatever is
    left after reload. Reading `w_damage` as damage-per-shot instead once gave the mammoth 800
    DPS against a true 400.

    ⚠ A SINGLE-SHOT WEAPON HAS NO BURST DELAY, so this returns None rather than a number. With
    `Burst: 1` there is no gap to measure, and the leftover cycle time is charge-up or rounding
    rather than a delay between shots. Measured across the tree, all 477 single-shot rows recover
    exactly 0.00, so printing "0" would read as a real measurement of something that does not
    exist; 245 rows carry a genuine burst.
    """
    # ⛔ ASK `reference_targets.burst_delay_of`, never `recover_burst_time` directly. The latter
    # computes `damage / dps - reload`, which was the cycle only while Cameo's `w_damage` was a
    # burst TOTAL. Now that every row is per shot the cycle is `damage * burst / dps`, and the old
    # call quietly returned 0 for every burst weapon in the map — the mammoth's 8 and the MLRS's 5
    # both went to nothing, which reads as "no burst delay" rather than as a broken calculation.
    return rt.burst_delay_of(row)


def burst_delay_cell(cameo_row, rows):
    """`now -> reference` for burst delay, marked as RECOVERED rather than projected.

    ⭐ ADDED 2026-09-12 at the maintainer's request: "with damage per shot, burst, burst delay,
    reload delay instead of just the DPS". That REVERSES an earlier instruction recorded in this
    file — "the burst delay should be referenced but not in the reference map" — and the newer
    one wins. The old claim is struck rather than left sitting next to the new behaviour.

    ⚠ THIS CELL DELIBERATELY DOES NOT GO THROUGH `estimate_cell`. Every other target is normalised
    against a per-source distribution before projection; burst delay has no such population,
    because it is not a published statistic anywhere — it is derived per row. Forcing it through
    that pipeline would mean inventing a distribution for it and would dress a recovered quantity
    up as a projected one. The reference figure is the plain MEDIAN of whatever the assigned
    reference rows recover, and the cell says exactly that in its tooltip.
    """
    cur = recovered_burst_delay(cameo_row)
    vals = sorted(v for v in (recovered_burst_delay(r) for r in rows) if v is not None)
    if cur is None and not vals:
        return '<span class="muted">—</span>'
    left = f'{cur:.0f}' if cur is not None else '<span class="muted">—</span>'
    if not vals:
        return (f'{left} <span class="muted">→</span> '
                f'<span class="muted" title="no assigned reference carries a burst">—</span>')
    mid = len(vals) // 2
    med = vals[mid] if len(vals) % 2 else (vals[mid - 1] + vals[mid]) / 2
    title = (f'median of {len(vals)} reference row(s) that carry a burst; RECOVERED from each '
             f'row&#39;s own damage/dps/reload identity, not a normalised projection')
    return (f'{left} <span class="muted">→</span> <span title="{title}">{med:.0f}'
            f'<small class="evidence">{len(vals)} recovered</small></span>')


def dps_verifier_cell(cameo_row, rows, tgt):
    """Total DPS as R1's GUARD RAIL: never a target, only a verdict on the components.

    Three things can come back, and the middle one is why the ruling exists:

      no change / a ratio  composing the component targets moves DPS by this much.
      DISAGREES            the components and the independent DPS projection tell materially
                           different stories. `td_gdi_mammothtank` produced the ruling: the
                           DPS projection alone said +73.7%, damage-plus-reload said +21.2%,
                           1.43x apart. Neither is wrong — they are separate votes, and a
                           human picks.
      EXTREME              the composed move alone exceeds EXTREME_RATIO.

    ⭐ THE CONVENTION IS KNOWN, SO THE VERIFIER IS NOT WITHHELD. A parallel fix (`41d0dad57`)
    made this cell fail closed on the premise that "the source corpus currently lacks that
    compatible evidence", which would print WITHHELD on every row. The premise is falsifiable and
    the AUTHORED YAML falsifies it: `td_gdi_mammothtank_120mmdualhv` declares `Damage: 16000`,
    `Burst: 2`, `BurstDelays: 8`, `ReloadDelay: 72` and the snapshot carries `w_damage` 32,000 —
    so Cameo stores the burst TOTAL, provably, and `td_gdi_mlrs_227mm` (8,000 x 6 = 48,000) says
    the same. The cure for an ambiguous unit is to resolve it, not to stop reporting.

    So rows are normalised to per shot at construction (`reference_distribution.to_per_shot`) and
    there is only ONE convention downstream. What IS kept from that fix is its genuinely better
    refusal: a recovered burst time below zero means the row's own DPS identity is shorter than
    its ReloadDelay, which is impossible, and clamping it to zero would certify a broken timing
    model. That still withholds.

    Burst delays ARE in the cycle and, as of 2026-09-12, are also their own column — see
    `burst_delay_cell`, which supersedes the earlier "referenced but not in the reference map"
    instruction (maintainer, 2026-09-12: "with damage per shot, burst, burst delay, reload delay
    instead of just the DPS").
    """
    keys = ("w_damage", "w_burst", "w_reload")
    cur = {k: cameo_row.get(k) for k in keys}
    cur["w_dps"] = cameo_row.get("w_dps")
    comp = {k: _cell_value(tgt.get(k)) for k in keys}
    g = rt.dps_guard(cur, comp, _cell_value(tgt.get("w_dps")))
    if g is None:
        return ('<span class="muted" title="withheld: explicit damage convention and complete '
                'burst-delay evidence required">WITHHELD</span>')
    pct = f'{g["composed_ratio"] * 100:.0f}%'
    bd = f'burst delays {g["burst_delays"]!r} (evidence, not a column)'
    if g["verdict"] == "ok":
        tag = (f'<span class="tag" title="{bd}">no change</span>'
               if abs(g["composed_ratio"] - 1) < 0.02
               else f'<span class="tag" title="{bd}">{pct} of now</span>')
    elif g["verdict"] == "extreme":
        tag = (f'<b class="warn" title="the composed move alone exceeds '
               f'{rt.EXTREME_RATIO:g}x; {bd}">EXTREME {pct}</b>')
    else:
        tag = (f'<b class="warn" title="components say {pct} of now, the DPS projection says '
               f'{g["projected_ratio"] * 100:.0f}% — {1 / g["disagreement"]:.2f}x apart; '
               f'separate votes, pick one. {bd}">DISAGREES {pct}</b>')
    return f'{g["composed_dps"]:.0f} {tag}'


def _cell_value(cell):
    """Pull the unrounded projection back out of a rendered estimate cell.

    `estimate_cell` returns HTML, and the guard needs the NUMBER. Parsing display output would
    be fragile, so `estimate_cell` stashes the value it used on the string via a data
    attribute; absent that, there is nothing to verify and the guard abstains rather than
    guessing.
    """
    if isinstance(cell, (int, float)):
        return cell
    if not isinstance(cell, str):
        return None
    m = re.search(r'data-v="([-0-9.eE+]+)"', cell)
    return float(m.group(1)) if m else None


def estimate_cell(rows, cameo_row, stat, dist, cdist, assigned_sources, flag_change=None):
    """Show metric evidence separately from identity matches; never relax eligibility.

    `flag_change` is the CURRENT value of an integer-valued stat. When given, the cell marks
    whether the reference target would actually MOVE it once snapped to its grid, and shows
    the unrounded projection in the tooltip. That distinction is the whole point for `Burst`:
    its raw target on `td_gdi_mammothtank` is **1.67**, which `num()` renders as "2" -- the
    same as the current value -- so the column reads as a change when it is not one. Burst may
    not be touched without the maintainer's explicit permission, so "would this move?" is a
    FLAG, not a routine number, and it has to be legible at a glance in the big table.
    """
    try:
        _, value, used = rt.target_for(rows, cameo_row, stat, dist, cdist) if rows else (None, None, 0)
        failure = None
    except (KeyError, TypeError, ValueError, ZeroDivisionError) as exc:
        value, used, failure = None, 0, type(exc).__name__
    reasons = []
    required = rd.ELIGIBILITY.get(stat, {}).get('requires')
    for row in rows:
        if rd.eligible(row, stat):
            if row.get(stat) and not any(dist.get(row['source'], {}).get(pop, {}).get(stat)
                    for pop in ('overall', row.get('type'))):
                reasons.append(f"{row['source']} / {row['id']}: raw statistic available, but insufficient normalization population (minimum three usable rows)")
            continue
        if stat in ('w_range', 'w_dps') and (row['source'], row['id']) in UNARMED_COUNTERPARTS:
            reasons.append(f"{row['source']} / {row['id']}: N/A, unarmed counterpart; identity match retained")
            continue
        if row.get('reference_base_eligible') is False:
            reasons.append(f"{row['source']} / {row['id']}: upgraded variant, excluded from base estimates")
            continue
        if required and not (row.get(required) is not None and row[required] > 0):
            reason = ('weapon estimate withheld' if required == 'w_dps'
                      else required + ' unavailable')
            raw_reason = row.get('w_evidence_reason') or ''
            categories = []
            if any(k in raw_reason for k in ('conditional', 'activation_trait', 'weapon_modifier', 'cadence_trait')):
                categories.append('conditional firing or modifiers not fully resolved')
            if any(k in raw_reason for k in ('unknown_warhead', 'nonconventional', 'non_damage', 'direct_undeclared')):
                categories.append('weapon damage/effect evidence incomplete')
            if 'multi_armament' in raw_reason:
                categories.append('multiple weapon slots not fully resolved')
            detail = ', '.join(categories) if required == 'w_dps' else None
        else:
            reason, detail = stat + ' unavailable', None
        reasons.append(f"{row['source']} / {row['id']}: {reason}" + (f" ({detail})" if detail else ''))
    status = f'{used}/{assigned_sources} sources used'
    if value is None:
        explanation = ('Calculation failed: ' + failure if failure else
                       'No usable source projection under the current evidence rules.')
    else:
        explanation = 'Sources counted once after pooling eligible variants; Cameo self-vote, if eligible, is additional.'
    badge = ""
    if flag_change is not None and value is not None:
        explanation += f' Unrounded projection {value:.4g}.'
        moved = round(value) != round(flag_change)
        badge = (f' <b class="warn" title="needs explicit maintainer permission">'
                 f'would change {round(flag_change):g}&rarr;{round(value):g}</b>' if moved
                 else ' <span class="tag" title="raw target rounds to the current value">'
                      'no change</span>')
    tooltip = html.escape(explanation + (' ' + '; '.join(reasons) if reasons else ''), quote=True)
    # `data-v` carries the UNROUNDED projection so the DPS verifier can compose the component
    # targets arithmetically. Re-deriving it by parsing the displayed number would silently
    # feed the guard a rounded value, and rounding is exactly what hid the burst 1.67 -> "2".
    dv = "" if value is None else f' data-v="{value:.10g}"'
    return (f'<span{dv} title="{tooltip}">{num(value)}'
            f'<small class="evidence">{status}</small></span>{badge}')


def weapon_calculation_details(rows, cameo_actor=None):
    """Expose source scalars and reviewed cycles without inventing missing delays."""
    import peer_nominal_evidence as nominal
    import projectile_travel_evidence as travel
    profile = nominal.load(ROOT)
    travel_profiles = travel.load(ROOT)
    parts = []
    if cameo_actor:
        parts.append('<p><b>Current Cameo projectile comparison</b><br>' + html.escape(
            travel.describe(travel_profiles.get(('Cameo current', cameo_actor), []))) + '</p>')
    def value(v):
        if isinstance(v, (int, float)):
            return f"{v:g}"
        return str(v) if v is not None else "unavailable"
    for row in rows:
        proof = profile.get((row.get('source'), row.get('id')))
        cycle = row.get('w_cycle_evidence')
        fields = [('weapon', row.get('weapon')),
            # ⚠ THESE ARE REFERENCE ROWS, AND THEY REALLY ARE PER SHOT. A parallel fix
            # relabelled this "source damage coordinate ... not per-shot", which is true of the
            # CAMEO snapshot and false of every peer: `extract_peer_units` sets
            # `w_damage = audit["damage_pos"]` and rates it `damage_pos * burst / cycle`. OpenRA
            # TD's `HTNK` proves it arithmetically — read as a burst total it gives a 24-tick
            # cycle against a declared ReloadDelay of 40, which cannot happen.
                  ('damage per shot (raw source units)', row.get('w_damage')),
                  ('reload delay / ROF (source ticks)', row.get('w_reload')),
                  ('burst', row.get('w_burst'))]
        if proof:
            fields = [('weapon', proof.get('weapon') or row.get('weapon')),
                      ('uncapped damage against named target' if proof.get('comparison_basis') == 'named_target_uncapped' else
                       'common authored damage basis per shot' if proof.get('comparison_basis') == 'common_authored_damage' else
                       'damage per shot after spin-up' if proof.get('warmup_shots') else
                       'mean damage per shot' if proof.get('shot_damage') else 'damage per shot (raw source units)', proof['damage']),
                      ('reload delay (source ticks)', proof['reload']),
                      ('burst', proof.get('burst', 1)),
                      ('burst delays (source ticks)', proof.get('burst_delays', []))]
        elif cycle:
            fields.extend([('cycle emission delay model (source ticks)', cycle['burst_delays']),
                           ('post-reload jitter/scheduling adjustment (source ticks)', cycle['post_burst_jitter'])])
            if 'cycle_shots' in cycle:
                fields.extend([('shots in modeled cycle (distinct from weapon Burst)', cycle['cycle_shots']),
                               ('additional charge and scheduling ticks', cycle.get('charge_ticks', 0)),
                               ('ammunition and charge proof', cycle.get('ammo_charge_proof'))])
        else:
            fields.append(('burst delays (source ticks)', row.get('w_burst_delays')))
        fields.append(('separate projectile travel comparison',
                       'N/A, reviewed unarmed counterpart' if (row['source'], row['id']) in UNARMED_COUNTERPARTS
                       else travel.describe(travel_profiles.get((row.get('source'), row.get('id')), []))))
        if proof and 'center_falloff_percent' in proof:
            fields.extend([('raw weapon Damage', proof['raw_weapon_damage']),
                           ('center falloff percent', proof['center_falloff_percent'])])
        if proof and proof.get('damage_parts'):
            fields.append(('included damage warheads', proof['damage_parts']))
        if proof and proof.get('shot_damage'):
            fields.extend([('authored weapon damage', proof.get('authored_damage')),
                           ('damage sequence across the burst', proof['shot_damage']),
                           ('per-shot firepower modifiers', proof.get('firepower_modifiers_by_shot'))])
        if proof and proof.get('warmup_shots'):
            fields.extend([('authored weapon damage', proof.get('authored_damage')),
                           ('damage sequence before full spin-up', proof['warmup_shots']),
                           ('first full-stage shot (ticks after first shot)', proof['first_full_stage_shot_tick']),
                           ('cold nominal rate (damage/tick)', proof['cold_nominal_rate'])])
        if proof and proof.get('delivery_path'):
            fields.append(('launcher to impact delivery path', proof['delivery_path']))
        if proof and proof.get('target_scenario'):
            fields.append(('target scenario and alternative warheads (not summed)', proof['target_scenario']))
        if proof and proof.get('basis_note'):
            fields.append(('comparison convention', proof['basis_note']))
        if proof and proof.get('linear_pulse_falloffs'):
            fields.append(('pulse falloffs; rate uses full-damage region', proof['linear_pulse_falloffs']))
        if proof and proof.get('charge_ticks_bounds'):
            fields.append(('additional charge ticks, minimum/maximum; formula uses mean', proof['charge_ticks_bounds']))
        if proof and proof.get('impact_count', 1) > 1:
            fields.extend([('raw damage per impact', proof['damage_per_impact']),
                           ('scheduled impacts per shot', proof['impact_count']),
                           ('impact ticks after emission', proof['impact_ticks'])])
        if row.get('range_selection'):
            fields.append(('selected base range weapon', row['range_selection'].get('weapon')))
        text = '; '.join(k + ': ' + value(v) for k, v in fields)
        if proof and proof.get('dps_usable', True):
            charge = f" + {proof['charge_ticks']:g} charge" if proof.get('charge_ticks') else ''
            text += (f"; reviewed nominal calculation: {proof['damage']:g} × "
                     f"{proof.get('burst', 1)} / ({proof['reload']:g} + "
                     f"{sum(proof.get('burst_delays', [])):g}{charge}) = {proof['dps']:.6g} damage/tick")
        elif cycle:
            text += (f"; nominal cycle model: {cycle['damage']:g} × {cycle.get('cycle_shots', cycle['burst'])} / "
                     f"{cycle['cycle_mean']:g} mean ticks = {cycle['dps']:.6g} damage/tick; "
                     f"cycle bounds {cycle['cycle_min']:g}–{cycle['cycle_max']:g} ticks; DTA runtime applicability unverified")
        else:
            text += '; retained source rate (damage/source tick): ' + value(row.get('w_dps_raw', row.get('w_dps')))
            text += '; cycle not independently reconstructed here'
        text += '; model damage/tick eligible: ' + ('yes' if rd.eligible(row, 'w_dps') else 'no')
        if row.get('reference_base_eligible') is False:
            text += '; base-state exclusion: ' + row['reference_base_reason']
        text += '; evidence: ' + str(row.get('w_evidence', 'unassessed'))
        if row.get('w_evidence_reason'):
            text += '; ' + row['w_evidence_reason']
        if (row['source'], row['id']) in UNARMED_COUNTERPARTS:
            text += '; weapon range and damage/tick: N/A, reviewed unarmed counterpart'
        parts.append('<p><b>' + html.escape(str(row.get('source')) + ' / ' + str(row.get('id'))) +
                     '</b><br>' + html.escape(text) + '</p>')
    if not parts:
        return ''
    return ('<details><summary>Weapon calculation details</summary>'
            '<p>Raw source values are not directly comparable across games. The model normalizes each source '
            'before projection; the rate below is a source-local damage/tick estimate, not a sustained gameplay DPS claim. Unknown delays remain unavailable. '
            'Reviewed nominal cycles exclude armor, splash totals, travel and upgrades. '
            'The separate travel sample uses fixed endpoints without scatter, blockers or speed modifiers. '
            'Tick calls start at the first projectile update, not the firing order; seconds depend on game speed. '
            'Unmodeled guided/custom projectiles remain unavailable, and conditional slots are not summed.</p>' + ''.join(parts) + '</details>')


def emit(body, members, crows, assignment, attached, chassis_only, dist, cdist, counts, klass, led_arms, hero_context=None):
    for kind, title in SECTIONS:
        group = [a for a in members if crows[a]["type"] == kind]
        if not group:
            continue
        body.append(f'<h3>{title} <span class="muted">· {len(group)}</span></h3>')
        reference_details = []
        generic_details = []
        # ⭐ CLASS, RANGE and damage/tick added 2026-09-08 at the maintainer's request. The class is what
        # the virtual anchor will be derived from (EXTRAPOLATION_PROGRAM.md), so a row whose class
        # looks wrong is a finding BEFORE any anchor is signed — and range/damage were the two stats
        # a reference actually moves that the table never showed.
        # ⭐ BURST added 2026-09-12 (sixth column). Burst delays deliberately stay OUT of the map at
        # the maintainer's request — they are collected and reported separately, not shown here.
        # Keep the map itself to the direct actor stats requested by Aedis.
        # Mapping confidence, class labels and generic weapon/delivery evidence
        # are emitted below as a separate review section.
        body.append('<table><thead><tr><th>Cameo actor</th>'
                    '<th class="n">HP (now → reference)</th>'
                    '<th class="n">Speed (now → reference)</th>'
                    '<th class="n">Range (now → reference)</th>'
                    '<th class="n">Damage/shot <span class="muted">(&#215;burst)</span></th>'
                    '<th class="n">Reload (now → reference)</th>'
                    '<th class="n">Burst (now → reference)</th>'
                    '<th class="n">Burst delay <span class="muted">t/shot, recovered</span></th>'
                    '<th class="n">DPS verifier</th>'
                    '<th class="n">Cost (now → reference)</th>'
                    '</tr></thead><tbody>')
        for a in group:
            c = crows[a]
            rows = attached.get(a) or []
            chosen = assignment.get(a) or {}
            counts["actors"] += 1
            counts["refs"] += len(chosen)
            srcs = sorted(chosen.items(), key=lambda kv: (
                CONF_ORDER.get((kv[1] or {}).get("confidence", "WEAK"), 9), kv[0]))
            flag = ""
            if not srcs:
                counts["none"] += 1
                flag = ' <b class="bad">no reference — formula</b>'
            elif is_original(chosen) and len(srcs) < 3:
                counts["thin"] += 1
                flag = ' <b class="warn">original, &lt;3 sources</b>'
            chips = "".join(
                '<span class="chip {cls}"><i>{src}</i> <code>{rid}</code> {rname}</span>'.format(
                    cls=(d or {}).get("confidence", "WEAK").lower(),
                    src=html.escape(s),
                    rid=html.escape(str((d or {}).get("id") or "?")),
                    rname=html.escape(str((d or {}).get("name") or "")))
                for s, d in srcs)
            # ⭐ SHOW THE VARIANT FAMILY, because the chip list was hiding the actual evidence.
            # A target is computed from every variant of the assigned unit in that source — the
            # Mammoth Mk III already averages CA's Mammoth, Hover Mammoth, Ion Mammoth and Mammoth
            # Drone — but the report displayed only the one row the greedy picked, so it read as a
            # single arbitrary choice. The maintainer reasonably objected to a mapping that was in
            # fact four rows deep.
            extra = len(rows) - len(srcs)
            if extra > 0:
                fam = ", ".join(dict.fromkeys(
                    str(r.get("id")) for r in rows
                    if str(r.get("id")) not in {str((d or {}).get("id")) for d in chosen.values()}))
                chips += (f'<span class="chip fam">+{extra} variant'
                          f'{"s" if extra != 1 else ""}: {html.escape(fam[:90])}</span>')
            selected_dist, selected_cdist = (hero_context if c.get('hero') and hero_context else (dist, cdist))
            tgt = {stat: estimate_cell(rows, c, stat, selected_dist, selected_cdist, len(srcs))
                   for stat in ('hp', 'speed', 'cost', 'w_range', 'w_dps')}
            # ⭐ R1, 2026-09-12: the WEAPON COMPONENTS are the referenced inputs and total DPS
            # is only a guard rail. Damage and reload therefore get their own columns, and the
            # old `damage/tick` column becomes the verifier — it reports whether composing the
            # components produces something extreme, or something that disagrees with what the
            # DPS projection independently claims (the mammoth was 1.43x apart).
            for stat in rt.COMPONENT_STATS:
                if stat == 'w_burst':
                    continue
                tgt[stat] = estimate_cell(rows, c, stat, selected_dist, selected_cdist,
                                          len(srcs), flag_change=c.get(stat))
            # ⭐ BURST added 2026-09-12 at the maintainer's request: "update the reference map to
            # also display burst since that's important and if it should change I should know it
            # in the big reference map table". It carries `flag_change` because `Burst` may not be
            # touched without explicit permission, so the actionable fact is whether the target
            # MOVES it, not the number — and the raw target rounds, which hid that distinction.
            tgt['w_burst'] = estimate_cell(rows, c, 'w_burst', selected_dist, selected_cdist,
                                           len(srcs), flag_change=c.get('w_burst'))
            note = ' <span class="tag">chassis-only</span>' if a in chassis_only else ""
            if c.get('hero'):
                note += ' <span class="tag">hero-only model</span>'
            empty = '<span class="muted">—</span>'
            reference_details.append((a, klass.get(a) or "—", len(srcs), chips or empty))
            generic = weapon_calculation_details(rows, a)
            if generic:
                generic_details.append((a, generic))
            body.append(
                f'<tr><td><code>{html.escape(a)}</code>{note}{flag}</td>'
                f'<td class="n">{num(c.get("hp"))} <span class="muted">→</span> {tgt["hp"]}</td>'
                f'<td class="n">{num(c.get("speed"))} <span class="muted">→</span> {tgt["speed"]}</td>'
                f'<td class="n">{num(c.get("w_range"))} <span class="muted">→</span> {tgt["w_range"]}</td>'
                f'<td class="n">{num(c.get("w_damage"))} {burst_note(c)}{arm_note(a, led_arms)} <span class="muted">→</span> {tgt["w_damage"]}</td>'
                f'<td class="n">{num(c.get("w_reload"))} <span class="muted">→</span> {tgt["w_reload"]}</td>'
                f'<td class="n">{num(c.get("w_burst"))} <span class="muted">→</span> {tgt["w_burst"]}</td>'
                f'<td class="n">{burst_delay_cell(c, rows)}</td>'
                f'<td class="n">{dps_verifier_cell(c, rows, tgt)}</td>'
                f'<td class="n">{num(c.get("cost"))} <span class="muted">→</span> {tgt["cost"]}</td></tr>')
        body.append("</tbody></table>")
        if reference_details:
            body.append('<h4>Reference mapping and generic group evidence</h4>')
            body.append('<p class="lede">The table above stays focused on HP, speed, range, DPS and cost. '
                        'This section keeps class/mapping provenance and the generic weapon, projectile, '
                        'spread, falloff and delivery evidence separate from the actor stats.</p>')
            body.append('<table><thead><tr><th>Cameo actor</th><th>class</th>'
                        '<th class="n">assigned refs</th><th>reference units chosen</th></tr></thead><tbody>')
            for actor, actor_class, ref_count, chips in reference_details:
                body.append(f'<tr><td><code>{html.escape(actor)}</code></td>'
                            f'<td class="cls">{html.escape(actor_class)}</td>'
                            f'<td class="n">{ref_count}</td><td>{chips}</td></tr>')
            body.append('</tbody></table>')
            for actor, details in generic_details:
                body.append(f'<div><code>{html.escape(actor)}</code>{details}</div>')


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    ap.add_argument("--faction", nargs="+", required=True)
    ap.add_argument("--out", default="report_td_ra1.html")
    ap.add_argument("--include-heroes", action="store_true", help="Include a separate frozen hero comparison population.")
    ap.add_argument("--pending", help="JSON map actor -> PENDING class (C46). Renders the class "
                                      "cell as 'today -> after'. A trailing '?' marks a "
                                      "reclassification that OVERRIDES an existing combat class "
                                      "and is not yet ruled.")
    args = ap.parse_args()

    peers, cameo = rd.peer_rows(), rd.cameo_rows()
    dist = rd.build_distributions(peers)
    rt.add_cost_distribution(dist, peers)
    cdist = rt.cameo_context()
    doc = json.loads(ASSIGN.read_text(encoding="utf-8"))
    assignment, chassis_only = doc["assignment"], doc.get("chassis_only", {})
    # ⚠ THE INDEX MUST HOLD EVERY POOL THE ASSIGNMENT COULD DRAW FROM. It is built from
    # `peer_rows()`, which by construction EXCLUDES the hero and variant lanes — so a row either
    # lane assigned cannot be recovered here and silently renders as no reference at all. The
    # variant lane made that visible: `td_gdi_humveemkii` holds DTA's `JEEPPTNK` and the page
    # showed the cell empty, which is the worst of both worlds — a mapping that exists in the
    # data and reads as missing work in the report.
    index_rows = peers + rd.peer_variant_rows()
    attached = rt.expand_families(rt.attach(assignment, rt.peer_index(index_rows)), peers)
    hero_context = None
    if args.include_heroes:
        hero_peers = [r for r in rd.peer_hero_rows() if r.get('hero') is True]
        hero_dist = rd.build_distributions(hero_peers)
        rt.add_cost_distribution(hero_dist, hero_peers)
        hero_context = (hero_dist, rt.hero_cameo_context())
        heroes = rd.cameo_hero_rows()
        hero_index = {(r['source'], r['id']): r for r in hero_peers}
        for hero in heroes:
            attached[hero['id']] = [hero_index[(source, ref['id'])]
                for source, ref in assignment.get(hero['id'], {}).items()
                if ref.get('confidence') in ('STRONG', 'FAIR')
                and (source, ref.get('id')) in hero_index]
        cameo += heroes
    crows = {c["id"]: c for c in cameo}
    # The class comes from `class_membership.classify`, NEVER from the raw `design.class_anchor`
    # field: membership is DERIVED from `subtype` when no explicit tag exists, so reading the tag
    # alone reports `commando` as empty when it has 30 members.
    import class_membership as cm
    led_arms = {}
    for _p in sorted((ROOT / "docs" / "balance").glob("*.json")):
        if "class_anchors" in _p.name:
            continue
        try:
            _d = json.loads(_p.read_text(encoding="utf-8"))
        except ValueError:
            continue
        for _sec in (_d.get("sections") or {}).values():
            if not isinstance(_sec, dict):
                continue
            for _n, _r in _sec.items():
                if isinstance(_r, dict):
                    led_arms[_n] = sum(1 for x in (_r.get("armaments") or [])
                                       if isinstance(x, dict) and x.get("pricing"))
    klass = {}
    for actor, design in cm.ledger_rows():
        c, _why = cm.classify(design)
        if c:
            klass[actor] = c
        else:
            reason = {"no-class-exists": "anchor class pending",
                      "no-template": "role template missing",
                      "not-a-unit": "separate defense lane"}.get(_why, "class unresolved")
            klass[actor] = f"{design.get('subtype') or 'Unknown role'} ({reason})"

    # ⚠ PENDING classes are NOT in the ledger and cannot be: `^ArmedTroopTransportTemplate` and
    # `^MobileBunkerTemplate` do not exist in yaml yet (C46), and `extract_stats` rewrites
    # `design.class_anchor` to None on every run, so subtype — i.e. the inherited template — is the
    # only durable membership signal. This overlay exists so the maintainer can review the
    # reclassification BEFORE any yaml lands, not to assert it has happened.
    if args.pending:
        pend = json.loads(pathlib.Path(args.pending).read_text(encoding="utf-8"))
        for actor, new_class in pend.items():
            klass[actor] = f"{klass.get(actor) or '—'} → {new_class}"

    body = []
    counts = {"actors": 0, "refs": 0, "thin": 0, "none": 0, "orig": 0, "exp": 0}
    for fac in args.faction:
        members = sorted(a for a in crows if a.startswith(fac + "_"))
        originals = [a for a in members if is_original(assignment.get(a))]
        expansions = [a for a in members if not is_original(assignment.get(a))]
        counts["orig"] += len(originals)
        counts["exp"] += len(expansions)
        body.append(f'<h2>{html.escape(fac)} <span class="muted">· {len(originals)} original, '
                    f'{len(expansions)} expanded</span></h2>')
        for band, label, note in (
            (originals, "Originals",
             "These exist in OpenRA/OpenTD, so a counterpart exists in every source and all "
             "three must be present and must be the same unit. Anything short of three, or any "
             "reference that is not plainly the same unit, is a defect worth reporting."),
            (expansions, "Expanded units",
             "Cameo, Combined Arms or DTA additions. No counterpart is guaranteed, so references "
             "here are accepted only on a name or id match — never on a similar stat shape, which "
             "is what used to hand these actors critters and hero units. An actor with no "
             "reference is priced by the formula from its class anchor, which is the intended "
             "outcome, not a gap.")):
            if not band:
                continue
            body.append(f'<h2 class="band">{label} '
                        f'<span class="muted">· {len(band)}</span></h2>'
                        f'<p class="lede">{note}</p>')
            emit(body, band, crows, assignment, attached, chassis_only, dist, cdist, counts, klass, led_arms, hero_context)

    summary = (f'{counts["orig"]} originals · {counts["exp"]} expanded · '
               f'{counts["refs"]} references · {counts["none"]} priced by formula · '
               f'{counts["thin"]} originals under three sources')
    page = (
        "<title>TD &amp; RA1 Reference Map</title>\n"
        f"<style>{STYLE}</style>\n"
        "<h1>TD &amp; RA1 Reference Map</h1>\n"
        '<p class="lede">Every Cameo actor with the reference unit chosen from each source, by '
        'full id and name, split into units that exist in the original games and units that do '
        'not. The bar on each chip is match confidence. <b>HP →</b>, <b>speed →</b> and '
        '<b>cost →</b> are the R4 synthesis targets — references plus Cameo, one vote each. '
        'Nothing here has been written to yaml.</p>\n'
        f'<p class="lede">{summary}</p>\n'
        '<div class="wrap">\n' + "\n".join(body) + "\n</div>\n")
    pathlib.Path(args.out).write_text(page, encoding="utf-8")
    print(f"wrote {args.out}  ({summary})")
    return 0


if __name__ == "__main__":
    sys.exit(main())
