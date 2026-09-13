#!/usr/bin/env python3
"""reference_targets.py — the R4 synthesis for real actors: 3 references + Cameo, one vote each.

PRIOR ART, and why this is not a fourth copy of it:
  * `assign_references.py` chooses WHICH reference units a Cameo actor may use. It stops at the
    pairing and stores only the reference's name/hp/cost.
  * `reference_distribution.py` owns the coordinate machinery (`aggregates`, `coordinates`,
    `project`, `gm`) and builds every source's own distributions. All of it is imported here.
  * `explain_unit.py` shows ONE actor's working, and re-matches with the superseded prefix test
    rather than reading the assignment.
This module is the missing join: the ASSIGNMENT's pairs, run through the DISTRIBUTION's
coordinates, for a whole faction at a time.

⛔ R4 — EQUAL THIRDS, AND CAMEO ALWAYS VOTES (maintainer, `REFERENCE_EXTRACTION_PLAN.md`):
   `Cameo TD GDI = DTA GDI x Combined Arms GDI x current Cameo TD GDI`, each 1/3, geometric mean.
   Generalised: every voice is equal, so with 3 references Cameo is 1 of 4 = 25%.

⛔ RAW STATS ARE NEVER AVERAGED ACROSS SOURCES. DTA runs ~2,500 HP vehicles and Combined Arms
~30,000; their mean belongs to no game. Only the DIMENSIONLESS coordinates are pooled, then
projected onto Cameo's own distribution. See `REFERENCE_METHOD.md` §1-§2.

    python tools/balance/reference_targets.py --faction td_gdi td_nod ra1_allies ra1_soviets
    python tools/balance/reference_targets.py --faction td_gdi --md docs/balance/targets_td.md
"""
from __future__ import annotations

import argparse
import collections
import json
import pathlib
import statistics
import sys

sys.path.insert(0, str(pathlib.Path(__file__).resolve().parent))
sys.path.insert(0, str(pathlib.Path(__file__).resolve().parents[1] / "audit"))
import faction_routes as fr  # noqa: E402
import reference_distribution as rd  # noqa: E402

ROOT = rd.ROOT
ASSIGN = ROOT / "docs" / "balance" / "derived" / "reference_assignment.json"
STATS = ("hp", "speed", "w_range", "w_dps", "cost")

# ⛔ DISCRETE COUNTS ARE TAKEN DIRECTLY, NEVER PROJECTED (maintainer caught this 2026-09-12:
# "the mammoth tank always has 2 bursts for all weapons from all sources right? and you
# averaged it to 1.67x?" -- correct, and the projection was the culprit, not an average).
#
# `target_for`'s normal path converts a raw value into its POSITION within that source's own
# distribution and maps the position onto Cameo's. That is right for HP, speed, cost, range
# and DPS: continuous magnitudes spanning orders of magnitude, where "this unit is a heavy in
# its own game" is the transferable fact and the raw number is not.
#
# It is WRONG for `w_burst`. Burst is a small integer the engine can only take whole, the
# distributions have wildly different supports, and they are dominated by outliers that have
# nothing to do with the unit in hand:
#
#     DTA Enhanced        burst min 2  max   4
#     OpenRA Tiberian Dawn          1        5
#     Combined Arms                 1       30
#     Cameo (projected onto)        1      100
#
# So a Mammoth's 2 -- low inside a 1..30 support -- maps low into a 1..100 support and lands
# at 1.67, even though EVERY eligible source says exactly 2. (The three `HTNK.*` Combined Arms
# variants that say 1 or 2 are `reference_base_eligible: False` upgraded variants and never
# voted at all.)
#
# MEASURED over the 209 actors that have both a burst and a burst target: 163 have UNANIMOUS
# agreement among their eligible sources, and projection contradicts that unanimous value on a
# large share of them -- `cabal_plasmaturret` every source 5, projected 2.21; `forgotten_mlrs`
# every source 8, projected 5.08; `cabal_cyborginfantry` every source 3, projected 1.92.
#
# Direct pooling reports a CHANGE on 98 of 209 against projection's 77, and that is the point:
# the extra ones are real disagreements between Cameo and its sources, which is exactly what
# the maintainer asked to see flagged in the map. `Burst` may not be touched without explicit
# permission, so the number has to be the sources' actual burst, not a position-derived one.
#
# One vote per source (median across that source's eligible rows), then the median of the
# source votes -- median, not mean, because the answer must be a count and an even split
# between 2 and 4 should read as one of them, not as 3.
DIRECT_STATS = ("w_burst",)

# ⛔ LAW-GOVERNED STATS TAKE NO REFERENCE TARGET AT ALL.
# `turn_speed` and `turn_ratio` are not free parameters in Cameo — DESIGN.md fixes them:
# `TurnSpeed = Speed / 5` for turreted units, `2 x Speed / 5` for turretless/frontal ones,
# DERIVED IN C# and not written in yaml (maintainer 2026-09-07), with a turret always matching
# its hull and `audit_turn_speed.py` T3 guarding it. Cameo's `turn_ratio` is therefore 5.0 or
# 2.5 BY LAW, and asking the peers what it should be is asking a question Cameo has already
# answered — a reference target here can only contradict a shipped ruling.
#
# And the projection made that contradiction loud, because `turn_ratio` is DIMENSIONLESS
# (speed / turn_speed), so the peers' raw values are directly comparable and projecting a
# position in a distribution is meaningless. Measured 2026-09-12 on actors whose sources all
# agree:
#     asianalliance_howitzer      all sources 1.00   projected 3.83
#     asianalliance_pulverizer    all sources 0.60   projected 3.51
#     asianalliance_harbinger     all sources 4.67   projected 11.55
# An 11.55 turn ratio is not a number any mod authored; it is an artifact.
#
# ⚠ Two different repairs, and the distinction matters. `w_burst` is scale-free AND a real
# design input, so it moved to DIRECT_STATS and still produces a target. `turn_*` is scale-free
# and NOT an input, so it produces none. Do not "fix" these by adding them to DIRECT_STATS.
#
# Verified safe: nothing consumes a turn target. `build_reference_report` shows hp/speed/range/
# dps/burst/cost, `build_japan_pilot.AXES` is hp/speed/w_range/cost, and `apply_balance` writes
# `turn_speed` from the LEDGER (where the derived law put it), never from a reference.
LAW_GOVERNED_STATS = ("turn_speed", "turn_ratio")

# ---------------------------------------------------------------------------------------- #
# R1 — THE WEAPON STATS ARE THE INPUTS; TOTAL DPS IS A GUARD RAIL, NOT A FIFTH INPUT.
#
# Maintainer ruling, 2026-09-12, asked directly: "each individual stat like damage per shot,
# burst, burst delay, reload delay should be referenced separately but also at the same time
# the total DPS should kept as a verifier so nothing suddenly becomes too extreme right?"
#
# ⛔ WHY THIS HAD TO BE RULED. `target_for` projects EVERY stat against its own distribution
# independently, so `w_dps`, `w_damage`, `w_burst`, `w_reload` and `w_range` were five separate
# votes with nothing tying them together — and they are mutually inconsistent BY CONSTRUCTION,
# not by accident. On `td_gdi_mammothtank` (3 sources, STRONG on all three):
#
#     w_dps projected alone            400 -> 695   (+73.7%)
#     damage +9.1% with reload -11.1%  400 -> 485   (+21.2%)  through the identity below
#     the two answers are 1.43x apart
#
# Applying more than one of them at once silently picks a rebalance nobody chose. R1 resolves
# it: the COMPONENTS are applied, and `w_dps` is only ever consulted to ask "did composing the
# components produce something extreme?".
COMPONENT_STATS = ("w_damage", "w_burst", "w_reload")   # separately referenced, applied
VERIFIER_STATS = ("w_dps",)                             # consulted, never applied

# The composed target may not be wilder than this against the CURRENT value. DISAGREE_RATIO
# sits BELOW the mammoth's own 1.43x component-vs-aggregate gap, so the case that motivated
# the ruling is flagged rather than waved through.
EXTREME_RATIO = 2.00
DISAGREE_RATIO = 1.25


# `w_damage` has two live meanings in the source corpus.  Keep the convention explicit at
# the guard boundary instead of trying to infer it from damage / DPS: the same quotient is a
# cycle for a burst-inclusive row and a per-shot cycle for a per-shot row.
DAMAGE_PER_SHOT = "per_shot"
DAMAGE_BURST_INCLUSIVE = "burst_inclusive"


def recover_burst_time(damage, dps, reload_ticks, *, burst=1, damage_convention=None):
    """Recover total burst-delay ticks only when the damage convention is explicit.

    `extract_peer_units` stores per-shot damage while the frozen Cameo snapshot stores the
    whole burst.  Without an explicit convention, `damage / dps` is ambiguous and this
    function refuses to manufacture a delay.  The returned value is the sum of all delays in
    the burst; individual delays must still be supplied separately when composing a target.
    """
    if not damage or not dps or damage_convention not in {DAMAGE_PER_SHOT,
                                                          DAMAGE_BURST_INCLUSIVE}:
        return None
    shots = max(int(burst or 1), 1)
    cycle_damage = float(damage) * (shots if damage_convention == DAMAGE_PER_SHOT else 1.0)
    remaining = cycle_damage / float(dps) - float(reload_ticks or 0)
    # A negative remainder means the row's own DPS identity is shorter than ReloadDelay;
    # clamping that contradiction to zero would certify an impossible timing model.
    if remaining < -1e-6:
        return None
    return max(0.0, remaining)


def compose_dps(damage, reload_ticks, burst=1, burst_delay_per_shot=0.0,
                burst_delays=None, damage_convention=DAMAGE_BURST_INCLUSIVE):
    """Compose a DPS value from an explicit damage convention and delay sequence.

    A sequence is preferred because burst delays can vary.  The scalar argument remains a
    convenience for a known constant-delay row.  Per-shot damage is multiplied by `burst`;
    burst-inclusive damage is not.
    """
    shots = max(int(burst or 1), 1)
    if burst_delays is not None:
        delays = [float(x) for x in burst_delays]
        if len(delays) != max(shots - 1, 0):
            return None
        delay_total = sum(delays)
    else:
        delay_total = max(shots - 1, 0) * float(burst_delay_per_shot or 0)
    cycle = float(reload_ticks or 0) + delay_total
    if cycle <= 0 or not damage:
        return None
    numerator = float(damage) * (shots if damage_convention == DAMAGE_PER_SHOT else 1.0)
    return numerator / cycle


def dps_guard(current, component_targets, dps_target):
    """Check a set of component targets against the DPS verifier.

    `current` carries `w_damage` / `w_burst` / `w_reload` / `w_dps`; `component_targets` the
    same keys, and a MISSING one falls back to the current value — a stat with no reference is
    not a stat being changed to zero, and zeroing it would make every partial row look extreme.

    Returns a dict, or None when there is not enough to judge. `verdict` is one of:
      ok         composing the components moves DPS by less than EXTREME_RATIO and lands
                 within DISAGREE_RATIO of what the DPS projection independently says.
      extreme    the composed move is itself wilder than EXTREME_RATIO.
      disagrees  the composed move and the DPS projection tell materially different stories —
                 the mammoth class. NOT an error: the components and the aggregate are
                 separate votes, and a human has to choose which to believe.
    """
    def pick(key):
        v = component_targets.get(key)
        return v if v not in (None, 0) else current.get(key)

    cur_dps = current.get("w_dps")
    convention = current.get("w_damage_convention")
    target_damage = component_targets.get("w_damage")
    target_convention = component_targets.get("w_damage_convention")
    delays = current.get("w_burst_delays")
    # A target without an explicit convention and delay sequence is not comparable.  Do not
    # silently mix a peer per-shot target with Cameo's burst-inclusive coordinate.
    if convention not in {DAMAGE_PER_SHOT, DAMAGE_BURST_INCLUSIVE}:
        return None
    if target_damage not in (None, 0) and target_convention != convention:
        return None
    if not isinstance(delays, (list, tuple)):
        return None
    burst = current.get("w_burst") or 1
    bt = recover_burst_time(current.get("w_damage"), cur_dps, current.get("w_reload"),
                            burst=burst, damage_convention=convention)
    if cur_dps is None or bt is None:
        return None
    if len(delays) != max(int(burst) - 1, 0) or abs(sum(float(x) for x in delays) - bt) > 1e-6:
        return None

    target_burst = pick("w_burst")
    # A changed burst needs its own delay evidence.  Reusing the current delay sequence would
    # turn a real cadence change into a fabricated verifier result.
    target_delays = component_targets.get("w_burst_delays")
    if target_burst != burst and not isinstance(target_delays, (list, tuple)):
        return None
    if target_delays is None and target_burst == burst:
        target_delays = delays
    if not isinstance(target_delays, (list, tuple)):
        return None
    if len(target_delays) != max(int(target_burst or 1) - 1, 0):
        return None

    new = compose_dps(pick("w_damage"), pick("w_reload"), target_burst,
                      burst_delays=target_delays, damage_convention=convention)
    if not new or not cur_dps:
        return None
    out = dict(current_dps=float(cur_dps), composed_dps=new, composed_ratio=new / cur_dps,
               burst_delays=list(target_delays), projected_dps=dps_target,
               projected_ratio=None, disagreement=None, verdict="ok")
    if out["composed_ratio"] > EXTREME_RATIO or out["composed_ratio"] < 1 / EXTREME_RATIO:
        out["verdict"] = "extreme"
    if dps_target:
        out["projected_ratio"] = float(dps_target) / cur_dps
        d = new / float(dps_target)
        out["disagreement"] = d
        if (d > DISAGREE_RATIO or d < 1 / DISAGREE_RATIO) and out["verdict"] == "ok":
            out["verdict"] = "disagrees"
    return out


def _guard_self_test():
    """The shipped mammoth and MLRS rows are the regression cases — both were got wrong once."""
    assert recover_burst_time(32000, 400, 72, burst=2,
                              damage_convention=DAMAGE_BURST_INCLUSIVE) == 8.0
    assert recover_burst_time(16000, 400, 72, burst=2,
                              damage_convention=DAMAGE_PER_SHOT) == 8.0
    assert recover_burst_time(32000, 400, 72) is None, "unknown convention must withhold"
    assert recover_burst_time(100, 20, 10, burst=1,
                              damage_convention=DAMAGE_BURST_INCLUSIVE) is None
    assert compose_dps(32000, 72, 2, burst_delays=[8]) == 400
    assert abs(compose_dps(48000, 111, 6, burst_delay_per_shot=5) - 352.941) < 0.01

    mam = dict(w_damage=32000, w_burst=2, w_reload=72, w_dps=400,
               w_damage_convention=DAMAGE_BURST_INCLUSIVE, w_burst_delays=[8])
    # components: damage +9.1%, reload -11.1%, burst unanimous at 2 (a DIRECT stat)
    g = dps_guard(mam, dict(w_damage=34915, w_burst=2, w_reload=64,
                            w_damage_convention=DAMAGE_BURST_INCLUSIVE), dps_target=695)
    assert g["burst_delays"] == [8], g
    assert abs(g["composed_dps"] - 485) < 1, g["composed_dps"]
    assert abs(g["projected_ratio"] - 1.7375) < 0.01, g["projected_ratio"]
    assert g["verdict"] == "disagrees", g          # ~1.43x apart, the documented gap
    assert abs(1 / g["disagreement"] - 1.43) < 0.02, g["disagreement"]

    # Same burst count, different delay evidence must change the composed verifier.
    delayed = dps_guard(mam, dict(w_damage=32000, w_burst=2, w_reload=72,
                                  w_burst_delays=[12],
                                  w_damage_convention=DAMAGE_BURST_INCLUSIVE), None)
    assert delayed["burst_delays"] == [12], delayed
    assert delayed["composed_ratio"] < 1.0, delayed

    # ⚠ A BURST CHANGE ALONE MUST NOT LOOK EXTREME. The MLRS target drops burst 6 -> 2, which
    # SHORTENS the cycle and nudges DPS up; the first version multiplied damage by burst and
    # called this "EXTREME 34%", a pure artifact of the wrong convention.
    mlrs = dict(w_damage=48000, w_burst=6, w_reload=111,
                w_dps=352.94117647058823,
                w_damage_convention=DAMAGE_BURST_INCLUSIVE,
                w_burst_delays=[5, 5, 5, 5, 5])
    m = dps_guard(mlrs, dict(w_damage=46534, w_burst=6, w_reload=106,
                             w_damage_convention=DAMAGE_BURST_INCLUSIVE), dps_target=None)
    assert 1.0 < m["composed_ratio"] < 1.3, m["composed_ratio"]
    assert m["verdict"] == "ok", m

    # a component set that agrees with the aggregate passes
    ok = dps_guard(mam, dict(w_damage=32000 * 1.7, w_reload=72,
                             w_damage_convention=DAMAGE_BURST_INCLUSIVE), 680)
    assert ok["verdict"] == "ok", ok

    # a genuine 2.5x move through the components alone is flagged
    ex = dps_guard(mam, dict(w_damage=32000 * 2.5, w_reload=72,
                             w_damage_convention=DAMAGE_BURST_INCLUSIVE), None)
    assert ex["verdict"] == "extreme", ex

    # a missing component is the CURRENT value, never zero
    same = dps_guard(mam, dict(w_damage=None, w_burst=None, w_reload=None,
                               w_damage_convention=DAMAGE_BURST_INCLUSIVE), None)
    assert same["composed_ratio"] == 1.0, same
    assert dps_guard(dict(w_damage=32000, w_burst=2, w_reload=72, w_dps=400),
                     dict(w_damage=32000), None) is None
    return "reference_targets R1 guard self-test: PASS (mammoth 1.43x gap; MLRS burst not extreme)"


def add_cost_distribution(dist, rows):
    """Fold a `cost` distribution into `dist` in place.

    ⛔ `reference_distribution.ALL_STATS` is CHASSIS + WEAPON + ARMOR and deliberately has no
    `cost` — the module is the CHASSIS layer and never priced anything. Asking it for a cost
    target therefore returns nothing, silently, for every actor: an empty column that reads as
    "no reference data" when the data is right there in every row. Price is a first-class
    reference stat here, so the aggregate is built the same way for the same populations.
    """
    by_source = collections.defaultdict(list)
    for r in rows:
        by_source[r["source"]].append(r)
    for source, items in by_source.items():
        pops = {"overall": [r for r in items if r["type"] in rd.COMBAT_TYPES]}
        for t in rd.POPULATIONS:
            pops[t] = [r for r in items if r["type"] == t]
        for pop, members in pops.items():
            agg = rd.aggregates([m.get("cost") for m in members
                                 if m.get("cost") and rd.eligible(m, "cost")])
            if agg:
                dist.setdefault(source, {}).setdefault(pop, {})["cost"] = agg


def peer_index(peers):
    idx = collections.defaultdict(list)
    for p in peers:
        idx[(p["source"], (p.get("name") or "").strip())].append(p)
    return idx


def attach(assignment, idx):
    """{actor: [peer row, ...]} — the assignment stores a NAME; recover the row it meant.

    ⚠ Two rows in one source can share a name (a variant), so the hp/cost the assignment
    recorded disambiguates; a name-only hit is the fallback rather than a dropped pair.
    """
    out = {}
    for actor, srcs in assignment.items():
        rows = []
        for src, rec in srcs.items():
            hits = idx.get((src, (rec.get("name") or "").strip()))
            if not hits:
                continue
            # The ID is the only reliable key — see the note in assign_references. hp/cost is the
            # fallback for assignments written before the id was recorded.
            best = None
            if rec.get("id"):
                best = next((h for h in hits if h.get("id") == rec["id"]), None)
            if best is None:
                best = next((h for h in hits if h.get("hp") == rec.get("hp")
                             and h.get("cost") == rec.get("cost")), hits[0])
            rows.append(best)
        if rows:
            out[actor] = rows
    return out


# ⭐ FAMILY MEMBERS THAT DO NOT SHARE THE ID STEM (maintainer, 2026-09-07). The family rule finds
# `4TNK.ATOMIC` and `4TNK.ERAD` from `4TNK` because Combined Arms suffixes the variant onto the
# base id. It cannot find the Apocalypse or the Overlord, which are the SAME tier of Soviet super-
# heavy under their own names, and the maintainer wants them counted with the rest. Named rows
# only — this is a list of units, not a pattern anyone can widen by accident.
FAMILY_EXTRA = {
    ("ra1_soviets_siegemammothtank", "Combined Arms"): ("APOC", "OVLD"),
    # GDI's super-heavy tier is split across two chassis names: the Mammoth line and the Titan
    # walkers. Both are 110,000 HP / 2,000cr in Combined Arms and both belong with the Mk III.
    # ⚠ `allows("td_gdi", TITN)` currently returns FALSE — CA's broad faction tagging denies GDI
    # its own walker — so these rows are unreachable through routing and can only arrive here.
    # That is a symptom, not a fix: EMBER owns the CA over-tagging, and when it is corrected these
    # two entries should be re-checked to see whether the family rule finds them unaided.
    ("td_gdi_mammothtankmkiii", "Combined Arms"): ("TITN", "TITN.RAIL"),
}


def expand_families(attached, peers):
    """Replace each assigned row with its whole variant family from that source."""
    by_source = collections.defaultdict(list)
    for p in peers:
        by_source[p["source"]].append(p)
    out = {}
    for actor, rows in attached.items():
        faction = fr.faction_of(actor)
        grown, seen = [], set()
        for r in rows:
            for f in family_rows(r, by_source, faction):
                key = (f["source"], f.get("id"), f.get("name"))
                if key in seen:
                    continue
                seen.add(key)
                grown.append(f)
        for (a_id, src), ids in FAMILY_EXTRA.items():
            if a_id != actor:
                continue
            for extra in by_source.get(src, ()):
                if (extra.get("id") or "").upper() not in ids:
                    continue
                key = (extra["source"], extra.get("id"), extra.get("name"))
                if key not in seen:
                    seen.add(key)
                    grown.append(extra)
        out[actor] = grown
    return out


def family_rows(assigned, peers_by_source, faction):
    """Every VARIANT of the assigned reference, from that same source — one voice between them.

    ⛔ MAINTAINER 2026-09-07: *"if more than one variant exists just use all of them as reference
    since they would otherwise not be mapped... but weight it still only the mean from CA as one
    voice compared to our existing Cameo one."*

    Combined Arms routes FOUR mammoths to GDI — `HTNK`, `HTNK.Hover`, `HTNK.Ion`, `HTNK.Drone` —
    and clause 2 lets an actor take only one per source, so three of them describe nothing and a
    Cameo add-on above the Mammoth has almost no evidence to sit on. Taking all four and averaging
    them inside the source keeps R4 intact: Combined Arms still casts ONE vote, it is just a better
    informed one.

    The grouping signal is the mod's own naming, not a similarity guess: these mods suffix a
    variant onto the base actor id after a dot, so `HTNK.Ion` belongs to `HTNK`. A row is admitted
    only if routing already allows this faction to see it.
    """
    base = (assigned.get("id") or "").split(".")[0]
    if not base:
        return [assigned]
    out = [r for r in peers_by_source.get(assigned["source"], ())
           if (r.get("id") or "").split(".")[0] == base and fr.allows(faction, r)]
    # ⚠ DTA ships AI duplicates of its own units (`AIHTNK`, `AIHTNK2`) with identical stats.
    # They are not variants and must not weight the source's mean toward one design twice.
    seen, uniq = set(), []
    for r in out:
        key = (r.get("name"), r.get("hp"), r.get("cost"), r.get("w_damage"))
        if key in seen:
            continue
        seen.add(key)
        uniq.append(r)
    return uniq or [assigned]


FROZEN_CAMEO_SHA256 = '726ada6afec708f8c6e9798ecbfb2758c842195f66755c2d5e683432c4a86f95'


class FrozenCameoDistribution(dict):
    """Projection ruler and self-votes captured together; never live feedback."""
    def __init__(self, distribution, rows):
        super().__init__(distribution)
        self.cameo_votes = {row['id']: row for row in rows}


def cameo_context():
    import hashlib
    path = ROOT / 'docs/reference/cameo_baselines/pre_reference_20260910.json'
    raw = path.read_bytes()
    if hashlib.sha256(raw).hexdigest() != FROZEN_CAMEO_SHA256:
        raise ValueError('permanent Cameo reference snapshot changed')
    document = json.loads(raw)
    if document.get('schema') != 1:
        raise ValueError('unsupported Cameo reference snapshot')
    rows = document['rows']
    distribution = rd.build_distributions(rows)
    add_cost_distribution(distribution, rows)
    return FrozenCameoDistribution(distribution['Cameo'], rows)


FROZEN_HERO_SHA256 = '101a934792713dd6ccf5fbeb99629db0379af7b1f9a51d52d02a010795e5365c'


def hero_cameo_context():
    """Separate hero population recovered from the same immutable ledger inputs."""
    import hashlib
    path = ROOT / 'docs/reference/cameo_baselines/pre_reference_heroes_20260910.json'
    raw = path.read_bytes()
    if hashlib.sha256(raw).hexdigest() != FROZEN_HERO_SHA256:
        raise ValueError('permanent hero reference snapshot changed')
    doc = json.loads(raw)
    if doc['parent_snapshot_sha256'] != FROZEN_CAMEO_SHA256:
        raise ValueError('hero reference does not share the frozen baseline')
    rows = doc['rows']
    dist = rd.build_distributions(rows)
    add_cost_distribution(dist, rows)
    return FrozenCameoDistribution(dist['Cameo'], rows)


def _direct_target(rows, vote_row, stat):
    """(peers_only, with_cameo, n_sources) for a DISCRETE count -- see DIRECT_STATS.

    No distribution, no projection: one vote per source (median of that source's eligible
    rows), then the median of the source votes. Eligibility is the SAME `rd.eligible` gate the
    projected path uses, so an upgraded variant or an evidence-withheld row abstains here too.

    `with_cameo` deliberately does NOT fold Cameo's own value in. On a continuous stat the
    self-vote damps a projection; on a count it would just drag a unanimous source answer
    toward the status quo and hide the very disagreement this stat is shown to reveal.
    """
    per = collections.defaultdict(list)
    for r in rows:
        x = r.get(stat)
        if not x or x <= 0 or not rd.eligible(r, stat):
            continue
        per[r["source"]].append(float(x))
    if not per:
        return None, None, 0
    votes = [statistics.median(v) for v in per.values()]
    value = statistics.median(votes)
    return value, value, len(per)


def target_for(rows, cameo_row, stat, dist, cdist):
    """(peers_only, with_cameo, n_sources) on one stat, or (None, None, 0).

    ⭐ POOLED PER SOURCE FIRST. Every source casts exactly ONE vote however many of its rows are
    in play, so a mod that happens to ship four variants of a unit cannot outvote one that ships
    a single unit. Without this, expanding to variant families would quietly re-weight R4.

    ⛔ AND EVERY VOTE IS GATED BY `rd.eligible` (review, 2026-09-09). The old test was only
    `if not x or x <= 0` — true for a withheld w_dps row's raw `w_range`/`w_damage`, which then
    voted against the other rows' aggregates even though the weapon's DPS fold never became a
    usable estimate. `rd.eligible` is the SAME gate `build_distributions` applies, so a row
    that abstains from a distribution cannot re-enter through `target_for`: for `w_dps` AND
    every stat that REQUIRES w_dps (`w_range`/`w_damage`/`w_burst`/`w_reload`, and the
    `dps_vs_*`), an evidence-withheld row contributes no coordinate, no peer vote and no
    source count. The optional Cameo self-vote is gated the same way, so an ineligible Cameo
    stat cannot outvote anything either. hp/speed/cost have their own eligibility and are
    unaffected. `peers_only` keeps its meaning: the peers' coordinates alone.
    """
    frozen = getattr(cdist, 'cameo_votes', None)
    vote_row = frozen.get(cameo_row.get('id')) if frozen is not None else cameo_row
    projection_row = vote_row if vote_row is not None else cameo_row
    if stat in LAW_GOVERNED_STATS:
        return None, None, 0
    if stat in DIRECT_STATS:
        return _direct_target(rows, vote_row, stat)
    per_source = collections.defaultdict(lambda: collections.defaultdict(list))
    for r in rows:
        x = r.get(stat)
        if not x or x <= 0:
            continue
        if not rd.eligible(r, stat):
            continue
        for pop in ("overall", r["type"]):
            agg = dist.get(r["source"], {}).get(pop, {}).get(stat)
            for k, v in rd.coordinates(float(x), agg).items():
                per_source[r["source"]][(pop, k)].append(v)
    pooled, used = collections.defaultdict(list), set()
    for source, coords in per_source.items():
        used.add(source)
        for key, vals in coords.items():
            # p_rng is a bounded position and averages arithmetically; the rest are ratios.
            pooled[key].append(statistics.fmean(vals) if key[1] == "p_rng" else rd.gm(vals))
    if not pooled:
        return None, None, 0
    # p_rng is a bounded [0,1] position and can legitimately be 0, where a geometric mean is
    # undefined; every other coordinate is a ratio and pools geometrically.
    synth = {pk: (statistics.fmean(v) if pk[1] == "p_rng" else rd.gm(v)) for pk, v in pooled.items()}
    cands = []
    for pop in ("overall", projection_row["type"]):
        coord = {k: v for (p_, k), v in synth.items() if p_ == pop}
        cands += list(rd.project(coord, cdist.get(pop, {}).get(stat)).values())
    cands = [c for c in cands if c and c > 0]
    if not cands:
        return None, None, 0
    peers_only = rd.gm(cands)
    now = vote_row.get(stat) if vote_row is not None else None
    now = now if (now and now > 0 and rd.eligible(vote_row, stat)) else None
    with_cameo = rd.gm([peers_only] * len(used) + [now]) if now else peers_only
    return peers_only, with_cameo, len(used)


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    ap.add_argument("--faction", nargs="+", required=True)
    ap.add_argument("--md", help="write the table to this path")
    args = ap.parse_args()

    peers = rd.peer_rows()
    cameo = rd.cameo_rows()
    dist = rd.build_distributions(peers)
    add_cost_distribution(dist, peers)
    cdist = cameo_context()
    assignment = json.loads(ASSIGN.read_text(encoding="utf-8"))["assignment"]
    attached = expand_families(attach(assignment, peer_index(peers)), peers)
    crows = {c["id"]: c for c in cameo}

    out = ["# Reference targets — R4 synthesis (references + Cameo, one vote each)", ""]
    for fac in args.faction:
        members = sorted(a for a in crows if a.startswith(fac + "_"))
        out += [f"## {fac} — {len(members)} actors", "",
                "| actor | src | hp now | hp -> | speed now | speed -> | range now | range -> "
                "| dps now | dps -> | cost now | cost -> |",
                "|---|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:|"]
        for a in members:
            rows = attached.get(a)
            c = crows[a]
            if not rows:
                out.append(f"| `{a}` | **0** | " + " | ".join(
                    [f"{c.get(s) or 0:,.0f}" + " | —" for s in STATS]) + " |")
                continue
            cells, nsrc = [], 0
            for s in STATS:
                _, t, n = target_for(rows, c, s, dist, cdist)
                nsrc = max(nsrc, n)
                now = c.get(s)
                cells.append(f"{now:,.0f}" if now else "—")
                cells.append(f"**{t:,.0f}**" if t else "—")
            out.append(f"| `{a}` | {nsrc} | " + " | ".join(cells) + " |")
        out.append("")

    text = "\n".join(out)
    if args.md:
        p = ROOT / args.md
        p.parent.mkdir(parents=True, exist_ok=True)
        p.write_text(text.rstrip() + "\n", encoding="utf-8")
        print(f"wrote {args.md}")
    else:
        print(text)
    return 0


if __name__ == "__main__":
    sys.exit(main())
