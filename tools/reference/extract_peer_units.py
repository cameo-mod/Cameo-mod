#!/usr/bin/env python3
"""Extract per-unit stats from a peer OpenRA mod's checkout, for the balance reference corpus.

PRIOR ART: `tools/reference/extract_versus.py` pulls WARHEAD/Versus profiles out of peer mods
(W13, feeding `versus_raw.json` and `aggregate_archetype.py`). This pulls UNIT stats — HP, Cost,
Speed — which that corpus deliberately does not carry. Different axis, no overlap.

    python tools/reference/extract_peer_units.py            # both mods, write the document
    python tools/reference/extract_peer_units.py --mod ca --dry-run

WHY IT EXISTS
-------------
`BALANCE_SYNTHESIS.md` §15 pools every reference source into a per-unit target, and
`ORIGINAL_UNIT_STATS.md` carries Combined Arms and Shattered Paradise as ROLE BANDS only —
"basic rifle 5000", "heavy trooper 7500-9000" — never as per-unit rows. So the two OpenRA peer
crossovers, the mods closest to Cameo in both engine and intent, could not vote on any named
unit. The single CA per-unit figure anywhere in the tree was the Apocalypse at 130,000 HP,
quoted as prose in §16. This closes that gap with real data.

⚠ THE RESOLVER IS NOT OPTIONAL. CLAUDE.md rule 8e forbids hand-parsing yaml, and an OpenRA actor
is a chain of `Inherits:` — `APOC` alone carries no HP; it comes from `^Tank` several levels up.
So this reads through `miniyaml.Ruleset`, which was parameterized by `mod_id` for exactly this
(it defaults to "cameo", so every existing caller is untouched). A regex over `vehicles.yaml`
would have returned nothing for most units and, worse, plausible numbers for a few.

⚠ EACH MOD SETS ITS OWN POWER LEVEL, so raw HP is meaningless across them. Every row is
normalized to that mod's own basic rifleman before it leaves here. Anchors are VERIFIED against
the checkout rather than trusted from a document — and one of them was wrong:
`ORIGINAL_UNIT_STATS.md` states "SP GDI rifle = 15000", but SP's `E1` (Light Infantry) resolves
to **12,500**. The artifact wins.
"""
import argparse
import math
import pathlib
import re
import sys

ROOT = pathlib.Path(__file__).resolve().parents[2]
sys.path.insert(0, str(ROOT / "tools" / "audit"))
import miniyaml  # noqa: E402

OUT = ROOT / "docs" / "design" / "ORIGINAL_UNITS_PEER_OPENRA.md"

# ⚠ THE HEALTH TRAIT IS NOT ALWAYS CALLED `Health`. Crystallized Nexus ships its own
# `CNHealth` (in `.modsdk/OpenRA.Mods.CN`), so a reader hardcoding `Health` finds 610 actors and
# ZERO units with hit points — an empty result that looks like "this mod has no data" rather than
# like a bug. Every peer therefore declares which traits carry its stats.
DEFAULT_TRAITS = {"health": ("Health",), "cost": ("Valued",),
                  "speed": ("Mobile", "Aircraft")}

# ── Unit type ────────────────────────────────────────────────────────────────────────────────
# Per-type distributions are the whole point of the distribution model: a Cameo vehicle must be
# compared against a peer's VEHICLES, not against its whole roster. Every OpenRA mod names its
# production queues differently — RA is plain (`Vehicle`), CA suffixes them (`VehicleSQ,
# VehicleMQ`), RV qualifies them by faction (`Vehicle.Civilian`) — so the queue string is matched
# by TOKEN rather than by equality, and traits are the fallback when a unit has no queue at all.
TYPE_TOKENS = [("aircraft", "aircraft"), ("infantry", "infantry"), ("cyborg", "infantry"),
               ("vehicle", "vehicle"), ("tank", "vehicle"), ("ship", "ship"), ("naval", "ship"),
               ("defense", "defense"), ("building", "building"), ("structure", "building")]


# ── Weapon extraction: the SCALE-FREE half ───────────────────────────────────────────────────
# Range, reload, burst, burst delays and raw damage need no armor taxonomy, so they are extracted
# now. Armor-aware effective DPS is deliberately NOT here: measured across the 13 peers there are
# 76 distinct Versus tags and only FIVE (None, Light, Heavy, Wood, Concrete) are shared by six or
# more mods. Generals Alpha declares 37, several of them per-unit
# (`vehicle.battle_bus.crate-1`); OpenRA Dune II declares none at all; Dune 2000 ships both
# `none` and `None`. A universal mapping is therefore not derivable from the data and must be
# hand-authored with per-source confidence — that is the next layer, and inventing it here would
# be fabricating a taxonomy.
#
# ⚠ `Versus` is a NODE WITH AN EMPTY VALUE whose CHILDREN are the armor rows. A probe using
# `node.get("Versus")` reads the empty value and concludes the mod has no Versus at all — which
# is exactly the wrong answer, and is how this pass nearly recorded "peers expose no Versus".
ARMOR_MAP_FILE = ROOT / "docs" / "reference" / "peer_armor_map.yaml"
_ARMOR_MAP = None


def armor_map(source_label):
    """{peer tag: Cameo ladder} plus the source's confidence, from the hand-authored table.

    Loaded from data rather than hardcoded because every entry is a judgement that has to stay
    inspectable. Only `high` and `medium` sources vote on armor-aware damage; `low` is recorded
    (E2140's four role tags carry no within-ladder information, so admitting them would flatten
    every ladder they touch) and `exclude` cannot participate at all.
    """
    global _ARMOR_MAP
    if _ARMOR_MAP is None:
        try:
            import yaml
            _ARMOR_MAP = yaml.safe_load(ARMOR_MAP_FILE.read_text(encoding="utf-8"))
        except Exception:
            _ARMOR_MAP = {"sources": {}}
    entry = (_ARMOR_MAP.get("sources") or {}).get(source_label) or {}
    return (entry.get("map") or {}), entry.get("confidence", "exclude")


# ── THE WEAPON EVIDENCE POLICY (P0/P1, 2026-09-10) ───────────────────────────
# `weapon_stats` used to fold the FIRST `Armament`'s weapon and present it as
# the unit's damage summary; every further slot, its conditions and its ammo
# cadence were dropped in silence. The recorded casualty is CA `HMMV.TOW`,
# whose TOW slot never surfaced (docs/balance/review/
# OPENRA_WEAPON_EVIDENCE_PLAN_20260910.md; BUGGY_HUMVEE_REFERENCE_REVIEW_20260909.md).
# The vocabulary is the INI extractor's contract, consumed by
# `reference_distribution.apply_weapon_evidence`:
#
#   * `nominal_direct` — the DECLARED contract that `w_dps` is the plain
#     direct Damage/ROF fold of ONE weapon. NEVER a complete unit DPS. It is
#     emitted only when the whole actor is provably simple: exactly ONE
#     resolved Armament slot, no PauseOnCondition / RequiresCondition /
#     UpgradeTypes on it, no AmmoPool (ammo is a cadence channel this fold
#     cannot read), no FirepowerMultiplier, a weapon that resolves through
#     the chain, every warhead a plain positive-Damage warhead seen through
#     fields we know, and a resolvable cadence — explicit BurstDelays
#     whenever Burst > 1 and an explicit ReloadDelay. The engine's
#     default-5 BurstDelays guess is NOT trusted here.
#   * `incomplete` (+ reason) — everything else: the raw first-weapon fold is
#     preserved on `w_dps_raw` (the old diagnostic; it votes nowhere),
#     `w_dps` is None, and EVERY resolved slot travels in `weapon_evidence`
#     with its raw warheads, conditions, targeting fields and ammo/rearm
#     bindings — for the future JSON export and for consumers to gate on.
#     Conditions are RETAINED, never assumed false or true: a gated slot is
#     recorded with its own weapon's numbers.
#   * An unknown warhead field is a fail-closed CHANNEL, not a cosmetic
#     extra — a peer engine may give it semantics this fold cannot see.
KNOWN_WARHEAD_FIELDS = frozenset(
    ("Damage", "Spread", "Falloff", "Delay", "Versus", "InvalidTargets"))
# Fields we can NAME but that are not plain-damage channels. AS's
# `PercentageVersus` is the near-miss sibling of `Versus` (LESSONS 8e);
# `DamageTypes` carries status/evaluator modifiers, not the plain hit.
NONDAMAGE_WARHEAD_FIELDS = ("PercentageVersus", "DamageTypes")
# The warhead node's VALUE is its TYPE, and only an exact allowlist backs a
# nominal claim. Measured across CA's weapons: 593 SpreadDamage warheads are
# the plain direct-damage channel; HealthPercentageDamage /
# HealthPercentageSpreadDamage / WarpPercentDamage are PERCENTAGE channels,
# TargetDamage / WarpDamage and every GrantCondition* / CreateEffect /
# FireShrapnel / SpawnActor node are channels this fold cannot read. An
# unfamiliar or missing type NEVER yields a numeric summary — the raw type
# and all its fields are retained instead.
CONVENTIONAL_WARHEAD_TYPES = frozenset(("SpreadDamage",))


def _cell_num(v):
    """Scale-free cell parser: OpenRA ranges are cell distances ("6c0" = 6 cells)."""
    if v is None:
        return None
    v = str(v).strip()
    if "c" in v:
        a, _, b = v.partition("c")
        try:
            return int(a) * 1024 + int(b or 0)
        except ValueError:
            return None
    try:
        return float(v)
    except ValueError:
        return None


def _delay_list(raw):
    """Parsed BurstDelays — NEVER silently drops a token. Zeros are preserved
    (a declared zero gap is data); a token that is not a finite non-negative
    number is returned in `bad` so the cadence is refused outright."""
    delays, bad = [], []
    for tok in str(raw or "").replace(",", " ").split():
        v = _cell_num(tok)
        if v is None or not math.isfinite(v) or v < 0:
            bad.append(tok)
        else:
            delays.append(v)
    return delays, bad


def _burst_time(burst, delays):
    """Burst cadence under the engine's repeat-last semantics: an array shorter
    than Burst-1 repeats its last entry for the remaining gaps. None = the
    cadence is UNRESOLVED (Burst > 1 with no explicit BurstDelays) and may not
    back a DPS claim."""
    gaps = max(0, int(burst) - 1)
    if not gaps:
        return 0
    if not delays:
        return None
    return sum(delays[min(i, len(delays) - 1)] for i in range(gaps))


def _warhead_evidence(wh):
    """Raw + audited view of one Warhead@* node. The node's VALUE is the
    warhead TYPE: only an exactly allowed conventional type whose damage
    comes through plain positive `Damage` fields we know qualifies; an
    unfamiliar, percentage, custom or missing type NEVER backs a nominal
    summary. The raw type and every field ride along, so no channel vanishes."""
    rows = {"key": wh.key, "type": (wh.value or "").strip(), "damage": wh.get("Damage"),
            "fields": {c.key: c.value for c in wh.children},
            "versus": {}, "channels": []}
    d = _cell_num(rows["damage"])
    rows["damage_num"] = d if (d is not None and math.isfinite(d)) else None
    if rows["type"] not in CONVENTIONAL_WARHEAD_TYPES:
        rows["channels"].append("missing_warhead_type" if not rows["type"]
                                else f"unknown_warhead_type: {rows['type']}")
    if rows["damage"] is None:
        rows["channels"].append("non_damage_warhead: no Damage")
    elif d is None or not math.isfinite(d):
        rows["channels"].append(f"unresolved_damage: {rows['damage']}")
    elif d <= 0:
        rows["channels"].append(f"non_damage_warhead: Damage {rows['damage']}")
    for c in wh.children:
        if c.key == "Versus":
            rows["versus"] = {r.key: r.value for r in c.children}
            for r in c.children:            # a malformed declared rung can never
                v = _cell_num(r.value)      # become the 100% engine default
                if v is None or not math.isfinite(v):
                    rows["channels"].append(f"unresolved_versus: {r.key}={r.value}")
        elif c.key in NONDAMAGE_WARHEAD_FIELDS:
            rows["channels"].append(f"nonconventional_channel: {c.key}={c.value}")
        elif c.key not in KNOWN_WARHEAD_FIELDS:
            rows["channels"].append(f"unknown_warhead_field: {c.key}={c.value}")
    return rows


def _audit_weapon(rules, weapon_id):
    """Resolve ONE weapon through the chain and audit it.

    Returns (audit, reason); reason is None only when the weapon resolved.
    The cadence arithmetic is fail-closed end to end: Burst must be a finite
    positive INTEGER when declared (a fraction 2.5, zero, negative, NaN, inf
    or malformed value is REFUSED, never rounded nor defaulted); an explicit
    finite positive-integer ReloadDelay is required; BurstDelay tokens are
    never silently dropped, and a declared zero gap stays zero.
    `burst_time_legacy` reproduces the OLD fold verbatim (sum of the parsed
    list, else the default-5 fallback) so the raw diagnostic keeps its old
    value wherever the cadence is still readable; the NOMINAL claim
    additionally requires `conventional` and `cadence_resolved`.
    """
    try:
        w = rules.resolve_weapon(weapon_id)
    except Exception:
        w = None
    if w is None:
        return None, f"weapon_unresolved: {weapon_id}"
    warheads = [_warhead_evidence(c) for c in w.children if c.key.startswith("Warhead")]
    channels = [ch for wh in warheads for ch in wh["channels"]]
    if not warheads:
        channels.append("no_warheads")

    problems = []
    burst_raw = w.get("Burst")
    if burst_raw is None or str(burst_raw).strip() == "":
        burst = 1                      # the engine's WeaponInfo default
    else:
        bnum = _cell_num(burst_raw)
        if bnum is None or not math.isfinite(bnum) or bnum < 1 or bnum != int(bnum):
            burst = None
            problems.append(f"invalid_burst: Burst {burst_raw} is not a positive integer")
        else:
            burst = int(bnum)
    delays, bad_delays = _delay_list(w.get("BurstDelays"))
    if bad_delays:
        problems.append(f"invalid_burst_delay: {', '.join(bad_delays)}")
    burst_time_strict = burst_time_legacy = None
    if burst is not None and not bad_delays:
        burst_time_strict = _burst_time(burst, delays)
        if burst_time_strict is None:
            problems.append(f"unresolved_burst_cadence: Burst {burst} without BurstDelays")
        burst_time_legacy = sum(delays) if delays else (burst - 1) * 5
    reload_raw = w.get("ReloadDelay")
    reload = None
    if reload_raw is None or str(reload_raw).strip() == "":
        problems.append("unresolved_reload_delay")
    else:
        rnum = _cell_num(reload_raw)
        if rnum is None or not math.isfinite(rnum) or rnum < 1 or rnum != int(rnum):
            problems.append(f"invalid_reload: ReloadDelay {reload_raw} is not a positive integer")
        else:
            reload = rnum
    channels.extend(problems)
    damage_pos = sum(wh["damage_num"] for wh in warheads if (wh["damage_num"] or 0) > 0)
    audit = {
        "weapon": weapon_id, "warheads": warheads, "channels": channels,
        "conventional": not channels,
        "burst": burst, "reload": reload, "delays": delays, "bad_delays": bad_delays,
        "burst_time_strict": burst_time_strict, "burst_time_legacy": burst_time_legacy,
        "cadence_resolved": not problems, "cadence_reason": "; ".join(problems) or None,
        "damage_pos": damage_pos or None,
        "mains": sum(1 for wh in warheads if (wh["damage_num"] or 0) > 0) or None,
        "range": _cell_num(w.get("Range")), "min_range": _cell_num(w.get("MinRange")),
        "valid_targets": w.get("ValidTargets"), "valid_stances": w.get("ValidStances"),
    }
    return audit, None


def _ladder_means(warheads, amap):
    """({ladder: DAMAGE-WEIGHTED mean}, {withheld ladder: reason}).

    A ladder is numeric only when EVERY clean damaging warhead declares an
    explicit value for EVERY armor key this amap maps to that ladder.
    INTER-warhead and INTRA-ladder coverage are distinct requirements, and a
    single declared rung (Heavy 50 beside undeclared Light/Medium) proves
    neither: the peer's exact full ladder roster is NOT verifiable here (the
    map may even be a superset), so anything less WITHHOLDS that ladder — no
    engine-default fill, no invented roster, even when a default would be
    plausible. Fully covered ladders stay exact: each contributor is the
    mean of its own declared rungs, weighted by its own damage; every
    declared rung stays raw in the warhead evidence."""
    clean = [wh for wh in warheads if (wh.get("damage_num") or 0) > 0 and not wh["channels"]]
    if not clean:
        return {}, {}
    roster = {}                       # ladder -> the armor keys THIS amap maps to it
    for armor, lad in amap.items():
        roster.setdefault(lad, set()).add(armor)
    parsed, ladders = [], set()
    for wh in clean:
        per = {}
        for armor, raw in wh["versus"].items():
            lad = amap.get(armor)
            v = _cell_num(raw)
            if lad and v is not None:
                per.setdefault(lad, {})[armor] = v
        parsed.append((wh["damage_num"], per))
        ladders |= set(per)
    means, withheld = {}, {}
    for lad in ladders:
        required = roster.get(lad, set())
        if any(set(per.get(lad, {})) != required for _, per in parsed):
            withheld[lad] = "partial_versus_coverage"
            continue
        s = wgt = 0.0
        for d, per in parsed:
            vals = list(per[lad].values())
            s += d * (sum(vals) / len(vals))
            wgt += d
        means[lad] = s / wgt
    return means, withheld


def _pool_binding(pool, armament_name):
    """(state, kind) with state in bound | unknown | other.

    OpenRA matches a pool to an armament by the armament's `Name:` field
    (default "primary"), NOT by the `@suffix`. A MISSING `Armaments:` is an
    engine default that DIFFERS PER SOURCE (CA's engine — verified against
    ca-engine/1.09 — binds primary AND secondary); an explicitly EMPTY one
    is a distinct state that is also engine-defined. Without an explicit
    per-source profile neither is resolvable HERE, so both stay conservative
    `unknown` and are never read as "binds every armament"."""
    entry = pool.child("Armaments")
    if entry is None:
        return "unknown", "missing"
    if not (entry.value or "").strip():
        return "unknown", "empty"
    names = [t.strip() for t in str(entry.value).split(",") if t.strip()]
    return ("bound" if armament_name in names else "other"), "explicit"


def _ammo_pool_evidence(pool):
    return {"trait": pool.key, "armaments": pool.get("Armaments"),
            "ammo": pool.get("Ammo"), "ammo_condition": pool.get("AmmoCondition"),
            "fields": {c.key: c.value for c in pool.children}}


def _rearm_evidence(trait):
    return {"trait": trait.key, "delay": trait.get("Delay"), "count": trait.get("Count")}


MODIFIER_BASES = ("FirepowerMultiplier",)
# `DamageMultiplier` implements IDamageModifier: it modifies INCOMING damage
# on the defender (engine Traits/Multipliers/DamageMultiplier.cs), so it is a
# DEFENSIVE fact, never a weapon's outgoing multiplier — retained raw and
# never counted against the weapon's nominal certification.
DEFENSIVE_BASES = ("DamageMultiplier",)


def _weapon_modifier_evidence(trait):
    """Actor-level weapon modifiers (`FirepowerMultiplier*` and friends).
    The `Modifier` field is a PERCENT: 100 is neutral, 1 is a 99% nerf — it
    is retained RAW and never folded into a DPS claim. Any conditioned
    multiplier — an elite tier, an upgrade path — is a mutually exclusive
    state, not a summand."""
    raw = trait.get("Modifier")
    num = _cell_num(raw)
    conditioned = bool(trait.get("UpgradeTypes") or trait.get("RequiresCondition"))
    return {"trait": trait.key, "modifier": raw,
            "upgrade_types": trait.get("UpgradeTypes"),
            "requires_condition": trait.get("RequiresCondition"),
            "conditional": conditioned,
            "nontrivial": conditioned or (num is None or num != 100.0)}


# Attack* traits that are the NORMAL firing path (no gate fields = the
# weapon fires plainly). Any OTHER Attack* base — CA's AttackTesla, a fork's
# special activation — is a suspect this layer cannot prove neutral.
PLAIN_ATTACK_BASES = frozenset(
    ("AttackFrontal", "AttackTurreted", "AttackFollow", "AttackBomber",
     "AttackAircraft", "AttackWander"))
GATE_FIELDS = ("PauseOnCondition", "RequiresCondition", "UpgradeTypes")
# Verified against the engine's Traits/AttackMove: these two are the
# condition-grant fields of AttackMoveInfo (null = no grant); every other
# field is a voice/cursor/shroud cosmetic. A bare AttackMove is the ordinary
# movement-order command, NOT firing or cadence evidence.
ATTACKMOVE_GRANT_FIELDS = ("AttackMoveCondition", "AssaultMoveCondition")


def _activation_evidence(node, has_pool):
    """Actor traits that can gate or retime the weapon outside the weapon
    itself: a conditioned Attack* (CA pauses AttackTurreted on EMP), an
    unusual activation (`AttackTesla`), a configured AttackMove (its own
    condition grants), and `Reload*` cadence modifiers. Recorded RAW, and
    each suspect withholds the nominal summary — a clean weapon alone never
    proves the ACTOR simple. Two proven-inert exemptions: an ordinary
    UNCONDITIONED AttackMove with no condition-grant fields is the movement
    order command (any AttackMoveCondition/AssaultMoveCondition or gate the
    actor configures makes it a suspect again), and `ReloadAmmoPool*` with
    no AmmoPool on the actor (nothing to reload)."""
    out = []
    gate_keys = {"PauseOnCondition": "pause_on_condition",
                 "RequiresCondition": "requires_condition", "UpgradeTypes": "upgrade_types"}
    for c in node.children:
        base = c.key.split("@")[0]
        gate = {gate_keys[f]: c.get(f) for f in GATE_FIELDS}
        if base.startswith("Attack"):
            if base == "AttackMove":
                grants = any(c.get(f) for f in ATTACKMOVE_GRANT_FIELDS)
                if any(gate.values()) or grants:
                    out.append({"trait": c.key, "kind": "activation", **gate,
                                "attack_move_condition": c.get("AttackMoveCondition"),
                                "assault_move_condition": c.get("AssaultMoveCondition")})
                continue               # ordinary movement-order command: exempt
            if any(gate.values()) or base not in PLAIN_ATTACK_BASES:
                out.append({"trait": c.key, "kind": "activation", **gate})
        elif base.startswith("Reload"):
            if base.startswith("ReloadAmmoPool") and not has_pool:
                continue               # proven inert: no AmmoPool exists to reload
            out.append({"trait": c.key, "kind": "cadence", **gate})
    return out


def _slot_evidence(slot, audit, pool):
    """One resolved Armament slot's full evidence record."""
    ev = {
        "slot": slot.key,
        "name": slot.get("Name") or "primary",
        "weapon": slot.get("Weapon"),
        "weapon_resolved": audit is not None,
        "pause_on_condition": slot.get("PauseOnCondition"),
        "requires_condition": slot.get("RequiresCondition"),
        "upgrade_types": slot.get("UpgradeTypes"),
        "target_stances": slot.get("TargetStances"),
        "ammo_pool": pool,
        "warheads": audit["warheads"] if audit else [],
        "channels": [] if audit is not None else ["weapon_unresolved"],
    }
    if audit is not None:
        ev.update({
            "valid_targets": audit["valid_targets"], "valid_stances": audit["valid_stances"],
            "range": audit["range"], "min_range": audit["min_range"],
            "reload_delay": audit["reload"], "burst": audit["burst"],
            "burst_delays": audit["delays"], "bad_burst_delays": audit["bad_delays"],
            "cadence_resolved": audit["cadence_resolved"],
            "cadence_reason": audit["cadence_reason"],
        })
    else:
        ev.update({"valid_targets": None, "valid_stances": None, "range": None,
                   "min_range": None, "reload_delay": None, "burst": None,
                   "burst_delays": [], "bad_burst_delays": [],
                   "cadence_resolved": False, "cadence_reason": None})
    return ev


def weapon_stats(rules, node, source_label=""):
    """Per-armament weapon evidence + the unit summary, fail-closed.

    Legacy scalar fields keep their shapes; `w_dps` carries a number only
    under the `nominal_direct` contract above, the old first-weapon fold
    survives as `w_dps_raw` otherwise, and `weapon_evidence` (plus
    `ammo_rearm` / `activation_traits` / `weapon_modifiers`) carries the
    full structured record for the future JSON export. An actor with no
    Armament carrying a Weapon is still {} — the same unarmed verdict as
    before.
    """
    slots = [c for c in node.children
             if c.key.split("@")[0] == "Armament" and c.get("Weapon")]
    if not slots:
        return {}
    pools = [c for c in node.children if c.key.split("@")[0] == "AmmoPool"]
    rearms = [_rearm_evidence(c) for c in node.children
              if c.key.split("@")[0].startswith("ReloadAmmoPool")]
    mods = [_weapon_modifier_evidence(c) for c in node.children
            if c.key.split("@")[0] in MODIFIER_BASES]
    defensive = [_weapon_modifier_evidence(c) for c in node.children
                 if c.key.split("@")[0] in DEFENSIVE_BASES]
    activation = _activation_evidence(node, has_pool=bool(pools))

    audits, evidence = {}, []
    for slot in slots:
        wid = slot.get("Weapon")
        if wid not in audits:
            audits[wid] = _audit_weapon(rules, wid)[0]
        audit = audits[wid]
        name = slot.get("Name") or "primary"
        # Priority bound > unknown > other: the pool record this slot votes
        # on, with its binding state carried verbatim. A pool explicitly
        # bound to a DIFFERENT armament name is proven inert here.
        pick = None
        order = {"bound": 0, "unknown": 1, "other": 2}
        for p in pools:
            state, kind = _pool_binding(p, name)
            if pick is None or order[state] < order[pick[1]]:
                pick = (p, state, kind)
        pool_ev = None
        if pick is not None:
            pool_ev = dict(_ammo_pool_evidence(pick[0]),
                           binding=pick[1], binding_kind=pick[2])
        evidence.append(_slot_evidence(slot, audit, pool_ev))

    reasons = []
    if len(slots) > 1:
        reasons.append(f"multi_armament: {len(slots)} slots")
    for ev in evidence:
        gated = [f for f in ("pause_on_condition", "requires_condition", "upgrade_types")
                 if ev.get(f)]
        if gated:
            reasons.append(f"conditional_armament: {ev['slot']} ({', '.join(gated)})")
        p = ev["ammo_pool"]
        if p is not None and p["binding"] == "bound":
            reasons.append(f"ammo_pool: {p['trait']} -> {p['armaments']}")
        elif p is not None and p["binding"] == "unknown":
            reasons.append(
                f"ammo_pool_binding_unknown: {p['trait']} "
                f"({'empty Armaments' if p['binding_kind'] == 'empty' else
                    'Armaments missing; engine default differs per source'})")
        if not ev["weapon_resolved"]:
            reasons.append(f"weapon_unresolved: {ev['weapon']}")
        else:
            reasons.extend(audits[ev["weapon"]]["channels"])
    for act in activation:
        reasons.append(f"{act['kind']}_trait: {act['trait']}")
    reasons.extend(f"weapon_modifier: {m['trait']}" for m in mods if m["nontrivial"])
    reasons = list(dict.fromkeys(reasons))          # de-duped, order preserved

    amap, conf = armor_map(source_label)
    audit = audits[slots[0].get("Weapon")]          # the OLD first-weapon pick
    row = {}
    raw_dps = None
    if audit is not None:
        row = {"weapon": slots[0].get("Weapon"),
               "w_range": audit["range"], "w_min_range": audit["min_range"],
               "w_damage": audit["damage_pos"], "w_mains": audit["mains"],
               "w_burst": audit["burst"], "w_reload": audit["reload"]}
        if conf in ("high", "medium"):
            means, withheld = _ladder_means(audit["warheads"], amap)
            row.update({f"eff_vs_{lad}": v / 100.0 for lad, v in means.items()})
            if withheld:
                row["ladders_withheld"] = withheld
        if audit["burst_time_legacy"] is not None:
            legacy_cycle = (audit["reload"] or 0) + audit["burst_time_legacy"]
            if audit["damage_pos"] and legacy_cycle > 0:
                raw_dps = (audit["damage_pos"] * audit["burst"]) / legacy_cycle
    if reasons:
        row.update({"w_evidence": "incomplete",
                    "w_evidence_reason": "; ".join(reasons),
                    "w_dps_usable": False, "w_dps": None})
        if raw_dps is not None:
            row["w_dps_raw"] = raw_dps
    else:
        cycle = (audit["reload"] or 0) + audit["burst_time_strict"]
        row.update({"w_evidence": "nominal_direct", "w_evidence_reason": None,
                    "w_dps_usable": True,
                    # Sustained output over the full cycle, in damage per tick — the plain
                    # direct fold, and even under `nominal_direct` NOT a complete unit DPS.
                    "w_dps": (audit["damage_pos"] * audit["burst"]) / cycle})
    row["weapon_evidence"] = evidence
    # every AmmoPool trait's full raw record at ROW level, not only each
    # slot's prioritized pick (whose binding certification stays unchanged)
    row["ammo_pools"] = [_ammo_pool_evidence(p) for p in pools]
    row["ammo_rearm"] = rearms
    row["activation_traits"] = activation
    row["weapon_modifiers"] = mods
    row["defensive_modifiers"] = defensive
    return row


def unit_type(node):
    """infantry | vehicle | aircraft | ship | defense | building | other."""
    queue = ""
    for c in node.children:
        if c.key.split("@")[0] == "Buildable":
            queue = (c.get("Queue") or "") + " " + (c.get("BuildAtProductionType") or "")
    q = queue.lower()
    for token, kind in TYPE_TOKENS:
        if token in q:
            return kind
    traits = {c.key.split("@")[0] for c in node.children}
    if "Aircraft" in traits:
        return "aircraft"
    if "Mobile" in traits:
        return "vehicle"          # an unqueued mobile actor; infantry is nearly always queued
    if "Building" in traits:
        return "building"
    return "other"


def turn_speed(node):
    """Turn rate and whether it comes from a TURRET.

    Cameo's turn law is relative to speed and keyed on the turret: a turreted ground unit turns at
    Speed/5 while a turretless one turns at 2xSpeed/5, and aircraft split again (helicopters and
    spaceships Speed/5, planes Speed/15). A peer's raw TurnSpeed is therefore only meaningful
    next to its speed and its turret state, so both travel together.
    """
    turreted = any(c.key.split("@")[0] == "Turreted" for c in node.children)
    for names in (("Turreted",), ("Mobile",), ("Aircraft",)):
        v = trait(node, names, "TurnSpeed")
        if v:
            try:
                return int(str(v)), turreted
            except ValueError:
                return None, turreted
    return None, turreted

# mod_id -> dict describing the peer:
#   label     human name used as the source label in the synthesis
#   root      checkout candidates; the FIRST whose mods/<mod_id>/mod.yaml exists wins
#   mod_id    the directory under mods/ (defaults to the key)
#   rifle     actor id of the basic rifleman — the mod's own 1.00x anchor
#   expect    documented anchor HP, checked against the checkout and REPORTED when it differs
#   traits    overrides for DEFAULT_TRAITS
PEERS = {
    "ca": {"label": "Combined Arms", "rifle": "E1", "expect": 5000,
           "root": ["/home/user/inq8/camod", "~/Documents/GitHub/CAmod", "../CAmod"]},
    "sp": {"label": "Shattered Paradise", "rifle": "E1", "expect": 12500,
           "root": ["/home/user/abrandau/shattered-paradise-sdk",
                    "~/Documents/GitHub/Shattered-Paradise-SDK",
                    "../Shattered-Paradise-SDK"]},
    # CN's basic rifleman is GASOL (the GDI "Marine", 125 HP / 120 cr) — it ships no E1, and its
    # HP scale is classic-Westwood-sized rather than OpenRA-sized, which is exactly why the
    # per-mod rifle normalization exists.
    "cn": {"label": "Crystallized Nexus", "rifle": "GASOL", "expect": 125,
           "traits": {"health": ("CNHealth", "Health")},
           # CN keeps its mod under .modsdk/, not at the checkout root.
           "root": ["/home/user/dogyaut/crystallized-nexus/.modsdk",
                    "~/Documents/GitHub/crystallized-nexus/.modsdk",
                    "~/Downloads/crystallized-nexus-main/.modsdk"]},
    # ⚠ FRACTURED REALMS IS DECLARED BUT CANNOT VOTE, and that is a finding, not a gap in this
    # tool. It resolves cleanly — 488 actors, 191 weapons — but only 23 actors carry BOTH Health
    # and Valued, and 18 of those are buildings (walls, gates, power plants, a forge). The mobile
    # remainder is a dozer, a transport ship, an MCV, one bomber and one scout. There is no basic
    # rifleman, so there is nothing to normalize against, and inventing an anchor would fabricate
    # every ratio derived from it. Last pushed 2023-10; it reads as an early prototype rather
    # than a balanced mod. Kept here so the check is recorded and re-runs automatically if the
    # mod ever grows a roster.
    "fnw": {"label": "Fractured Realms", "rifle": "e1", "expect": None,
            "root": ["/home/user/logue-yne/fractured-realms",
                     "~/Documents/GitHub/Fractured-Realms", "../Fractured-Realms"]},
    "rv": {"label": "Romanov's Vengeance", "rifle": "e1", "expect": 12500,
           "root": ["/home/user/mustaphatr/romanovs-vengeance",
                    "~/Documents/GitHub/Romanovs-Vengeance", "../Romanovs-Vengeance"]},
    # C&C Generals in OpenRA. §15.5 rules that SAGE economies do NOT map to credits, so its COST
    # column is identity-only and must not be read as a price; HP normalized to its own basic
    # infantry is still comparable. Its anchor is `infantry.conscript` (12,000 HP / 100) — there
    # is no `E1` in this universe.
    "gen": {"label": "Generals Alpha", "rifle": "infantry.conscript", "expect": 12000,
            "root": ["/home/user/mustaphatr/generals-alpha",
                     "~/Documents/GitHub/Generals-Alpha", "../Generals-Alpha"]},
    # The four OpenRA BASE mods — the original games as the engine ships them. They are the
    # closest thing to a neutral reading of Westwood's own balance, and `versus_raw.json` already
    # samples all four for warheads; these are their unit stats.
    "ra": {"label": "OpenRA Red Alert", "rifle": "E1", "expect": 5000,
           "root": ["/home/user/openra/openra", "~/Documents/GitHub/OpenRA", "../OpenRA", "~/Documents/GitHub/cameo-engine"]},
    "cnc": {"label": "OpenRA Tiberian Dawn", "rifle": "E1", "expect": 5000,
            "root": ["/home/user/openra/openra", "~/Documents/GitHub/OpenRA", "../OpenRA", "~/Documents/GitHub/cameo-engine"]},
    "ts": {"label": "OpenRA Tiberian Sun", "rifle": "E1", "expect": 12500,
           "root": ["/home/user/openra/openra", "~/Documents/GitHub/OpenRA", "../OpenRA", "~/Documents/GitHub/cameo-engine"]},
    "d2k": {"label": "OpenRA Dune 2000", "rifle": "light_inf", "expect": 6000,
            "root": ["/home/user/openra/openra", "~/Documents/GitHub/OpenRA", "../OpenRA", "~/Documents/GitHub/cameo-engine"]},

    # ── Found 2026-08-30 by searching GitHub's `topic:openra`, all cloned from source ────────
    # `e1` (G.I.) and `e2` (Conscript) are both 125 HP in the RA2 family; `e1` is used as the
    # anchor throughout for consistency with the rest of the corpus.
    "ra2": {"label": "OpenRA RA2 official", "rifle": "e1", "expect": 125,
            "root": ["/home/user/openra/ra2", "~/Documents/GitHub/ra2", "../ra2"]},
    "yr": {"label": "Yuri's Revenge on OpenRA", "rifle": "e1", "expect": 125,
           "root": ["/home/user/cookgreen/yuris-revenge",
                    "~/Documents/GitHub/Yuris-Revenge", "../Yuris-Revenge"]},
    # Valiant Shades runs on the Attacque Supérior fork — the same OpenRA.Mods.AS that Cameo's
    # own engine carries — so its power level is the closest of any peer to Cameo's own.
    "ra2vsh": {"label": "Valiant Shades", "rifle": "e1", "expect": 65000,
               "root": ["/home/user/as/valiantshades",
                        "~/Documents/GitHub/ValiantShades", "../ValiantShades"]},
    # OpenHV is original sci-fi IP rather than a C&C crossover, so it shares no unit NAMES with
    # Cameo and will rarely match by name. It is kept for the role/spread reading: a from-scratch
    # OpenRA roster balanced without Westwood's legacy numbers is a genuinely independent voice.
    "hv": {"label": "OpenHV", "rifle": "RIFLEMAN", "expect": 15000,
           "root": ["/home/user/openhv/openhv", "~/Documents/GitHub/OpenHV", "../OpenHV"]},
    "d2": {"label": "OpenRA Dune II", "rifle": "light_inf", "expect": 20,
           "root": ["/home/user/openra/d2", "~/Documents/GitHub/d2", "../d2"]},
    # Earth 2140. §15.5: Earth economies do NOT map to credits, so its cost column is
    # identity-only; HP still normalizes against its own basic infantry.
    "e2140": {"label": "OpenE2140", "rifle": "ed_infantry_a01", "expect": 28,
              "root": ["/home/user/opene2140/opene2140",
                       "~/Documents/GitHub/OpenE2140", "../OpenE2140"]},
}


def find_checkout(cands, mod_id):
    """First candidate that actually holds this mod's manifest.

    Checking `mods/<mod_id>/mod.yaml` rather than just `mods/` matters: Crystallized Nexus keeps
    its mod under `.modsdk/`, so the repository root has no `mods/` at all.
    """
    for c in cands:
        p = pathlib.Path(c).expanduser()
        if (p / "mods" / mod_id / "mod.yaml").is_file():
            return p
    return None


def trait(node, names, field):
    """First value of `field` on any of `names` (matching `Name` or `Name@suffix`)."""
    if isinstance(names, str):
        names = (names,)
    for child in node.children:
        if child.key.split("@")[0] in names:
            v = child.get(field)
            if v:
                return v
    return None


def traits_for(spec):
    t = dict(DEFAULT_TRAITS)
    t.update(spec.get("traits") or {})
    return t


def load_fluent(root, mod_id):
    """{key: text} from the mod's .ftl files.

    SP and CN name their units by fluent key rather than literally, and the two use different
    Fluent shapes — SP writes `e1-name = Light Infantry` on one line, CN writes an ATTRIBUTE
    block:

        actor-gasol =
            .name = Marine
            .description =
            General-purpose infantry.

    so both forms are read. Getting this wrong is not cosmetic: the display name is what matches
    a peer unit to a Cameo actor, and an unresolved `actor-gasol.name` matches nothing.
    """
    out = {}
    for f in (root / "mods" / mod_id).rglob("*.ftl"):
        text = f.read_text(encoding="utf-8", errors="replace")
        key = None
        for line in text.splitlines():
            m = re.match(r"^([A-Za-z0-9_.-]+)\s*=\s*(.*)$", line)
            if m:
                key = m.group(1)
                if m.group(2).strip():
                    out[key] = m.group(2).strip()
                continue
            a = re.match(r"^\s+\.([A-Za-z0-9_-]+)\s*=\s*(.+)$", line)
            if a and key:
                out[f"{key}.{a.group(1)}"] = a.group(2).strip()
    return out


def unit_name(actor_id, node, fluent):
    """Display name, tried in the order that actually resolves across these mods."""
    raw = trait(node, "Tooltip", "Name") or ""
    if raw in fluent:
        return fluent[raw]
    # OpenRA's convention when a Tooltip carries no explicit Name.
    for guess in (f"actor-{actor_id.lower()}.name", f"{actor_id.lower()}-name"):
        if guess in fluent:
            return fluent[guess]
    return raw or actor_id


def declared_factions(rules):
    """{internal name: is a PLAYABLE subfaction} from the mod's own World actor.

    ⛔ THE WHITELIST HAS TO COME FROM THE MOD, NOT FROM A GUESS. Faction tokens sit in the same
    `Prerequisites` string as unit tokens — Combined Arms writes `~vehicles.soviet` beside
    `~vehicles.mtnk`, and `mtnk` is a Medium Tank, not a faction. A regex over `word.word` tagged
    only 20% of CA's roster and would have invented factions out of unit names. Every OpenRA mod
    declares its factions on `World`, so that list is read and used as the filter.

    ⚠ `Selectable: False` marks a PARENT faction — `allies`, `soviet`, `gdi` — that groups the
    playable subfactions beneath it. Both are kept: the parent is what a Cameo faction maps to
    (RA2 Allies), the subfaction is what a unit is usually tagged with (england, france, usa).
    """
    world = rules.resolve("World") or rules.resolve("world")
    if world is None:
        return {}
    out = {}
    for c in world.children:
        if c.key == "Faction" or c.key.startswith("Faction@"):
            d = {k.key: k.value for k in c.children}
            name = (d.get("InternalName") or c.key.split("@")[-1] or "").strip().lower()
            if not name or name == "random":
                continue
            # ⭐ `random-allies` DECLARES that `allies` is a real faction GROUP. Romanov's Vengeance
            # names 31 subfactions (america, england, russia, cuba…) and gates its units on the
            # PARENT (`Infantry.Allies`, `Infantry.Soviets`) — parents that appear nowhere else in
            # the mod except as these random-* entries. Reading them recovers the grouping a Cameo
            # faction actually maps to, which is RA2 Allies rather than RA2 France.
            if name.startswith("random-"):
                out[name[len("random-"):]] = True
            else:
                out[name] = (d.get("Selectable", "") or "").strip().lower() != "false"
    return out


# How many prerequisite hops to follow when resolving a faction. 2 covers the real chains
# (unit -> barracks -> `structures.<faction>`); deeper mostly reaches shared infrastructure and
# risks attaching a faction through a building both sides can build.
PREREQ_DEPTH = 2


def _faction_tokens(text, known):
    """The faction tokens inside one comma-separated Queue/Prerequisites string."""
    found = set()
    for chunk in (text or "").split(","):
        tok = chunk.strip().lstrip("~!").strip().lower()
        # `Infantry.Allies` -> allies · `~infantry.england` -> england · bare `yuri` -> yuri
        for part in (tok.split(".")[-1], tok):
            if not part or part.isdigit():
                continue
            if part in known:
                found.add(part)
                continue
            # ⚠ Mods abbreviate their own faction names inconsistently: Shattered Paradise
            # DECLARES `mut`, `cab`, `scr` and then gates units on `mutant`, `cabal`, `scrin`.
            # A prefix match in either direction reconciles them; 3 characters is the floor so
            # short unit tokens cannot masquerade as a faction.
            for k in known:
                if len(part) >= 3 and len(k) >= 3 and (part.startswith(k) or k.startswith(part)):
                    found.add(k)
                    break
    return found


def _buildable(node):
    return next((c for c in node.children
                 if c.key == "Buildable" or c.key.startswith("Buildable@")), None)


def prerequisite_providers(rules, known):
    """{provided token: set(declared factions)} — the INVERTED direction of faction gating.

    ⚠ Some mods gate a unit's faction from the PROVIDER side, not the consumer side.
    Combined Arms writes `Prerequisites: ~vehicles.1tnk` on the unit and then answers
    "who may build it" on a structure:

        ProvidesPrerequisiteValidatedFaction@1tnk:
            Factions: allies, france, germany, usa
            Prerequisite: vehicles.1tnk

    `factions_of` walks the unit's prerequisites UP toward actors and finds nothing —
    `vehicles.1tnk` is a capability token, not an actor. This index reads the other
    direction: every `ProvidesPrerequisite*` trait's `Prerequisite:` token is mapped
    to its scope, which is

    * the trait's own `Factions:` (`ProvidesPrerequisiteValidatedFaction`), or
    * the PROVIDING actor's `ValidFactions.Factions` — a Soviet-only barracks
      provides `infantry.ra` unscoped, and the scope is the building's, not the
      token's (structures.yaml: `ValidFactions: soviet, russia, ukraine, iraq, yuri`).

    A provider with neither field contributes nothing — that preserves the deliberate
    shared-infrastructure refusal (`anypower` et al. stay unscoped).
    """
    prov = {}
    for aid in rules.actors:
        try:
            node = rules.resolve(aid)
        except Exception:
            continue
        if node is None:
            continue
        own_factions = set(factions_of(node, known, rules))
        actor_scope = set()
        for c in node.children:
            if c.key.split("@")[0] == "ValidFactions":
                d = {k.key.lower(): k.value for k in c.children}
                actor_scope |= {f.strip().lower()
                                for f in (d.get("factions") or "").split(",") if f.strip()}
        for c in node.children:
            if not c.key.startswith("ProvidesPrerequisite"):
                continue
            d = {k.key.lower(): k.value for k in c.children}
            pr = d.get("prerequisite") or d.get("prerequisites") or ""
            fac = {f.strip().lower()
                   for f in (d.get("factions") or "").split(",") if f.strip()}
            # ⛔ A BARE `ProvidesPrerequisite` INHERITS THE PROVIDER'S OWN FACTIONS. OpenRA's
            # Red Alert gates E1 on `~barracks`, and BOTH barracks grant it with no `Factions:`
            # line — `TENT` is Allied and `BARR` is Soviet purely through their own buildability.
            # Taking only the trait's declared factions left `barracks` unscoped, so E1 and E3 —
            # the basic rifle and rocket infantry that every faction builds — came out UNTAGGED
            # and `allows()` then refused them to everyone. The union across both providers is
            # what makes them universal, which is exactly R14's carve-out.
            scope = (fac or actor_scope or own_factions) & set(known)
            for t in pr.split(","):
                t = t.strip().lower()
                if t and scope:
                    prov.setdefault(t, set()).update(scope)
    return prov


def factions_of(node, known, rules=None, _depth=PREREQ_DEPTH, _seen=None, vfi=None):
    """The faction tokens an actor is gated on, filtered by what the mod actually declares.

    ⛔ THE FACTION IS OFTEN ONE HOP AWAY, IN THE PREREQUISITE BUILDING. OpenRA gates most infantry
    on a barracks rather than on a faction:

        E2 (Grenadier):  Prerequisites: ~barr, ~techlevel.infonly
        BARR:            Prerequisites: anypower, ~structures.soviet, ~techlevel.infonly

    Reading only the unit's own line finds no faction and returns nothing, which is why infantry
    was the WORST-tagged type in the corpus at 25% against defence's 80% — and why
    `ra1_soviets` routed to ZERO reference infantry while OpenRA Red Alert plainly ships Soviet
    infantry. `heavy_sniper`, a SIGNED class, had both its members ground to nothing for exactly
    this reason.

    ⚠ Depth is capped at `PREREQ_DEPTH`. Following the chain forever eventually reaches
    infrastructure both sides build (`anypower` -> any power plant), and a faction attached
    through shared infrastructure is worse than no faction at all.

    `rules` is optional so existing callers and tests keep working; without it the resolution is
    the original single-level read.
    """
    b = _buildable(node)
    if b is None:
        return []
    # ⛔ A FACTION-SUFFIXED `Queue:` IS THE OWNERSHIP STATEMENT, and nothing may dilute it.
    # `Queue` names the production queue the actor appears in, which is exactly "who can build
    # this". `Prerequisites` names what you must already own, which is routinely SHARED — and
    # unioning the two destroyed the direct gate on 15 of OpenRA Tiberian Dawn's 49 faction-gated
    # actors, the Light Tank, Medium Tank, Flame Tank, Orca and Apache among them:
    #
    #     LTNK:  Queue: Vehicle.Nod      Prerequisites: anyhq, ~techlevel.medium
    #
    # `Vehicle.Nod` resolves to {nod} correctly; then `anyhq` — a token BOTH headquarters provide —
    # came back through the prerequisite-provider index and widened it to {gdi, nod}. Nod's Light
    # Tank became claimable by a GDI actor. OBLI and SAM escaped only because `tmpl` and `hand`
    # happen to be Nod-only buildings, so the dilution was invisible wherever it did no harm.
    # The rule below the vfi block already said a direct gate must not be diluted; it simply ran
    # too late to protect this one.
    queue_gate = _faction_tokens(b.get("Queue"), known)
    if queue_gate:
        return sorted(queue_gate)
    found = set()
    for field in ("Queue", "Prerequisites"):
        found |= _faction_tokens(b.get(field), known)
    # A VALIDATED-FACTION GRANT IS A DIRECT CLAIM, not an inherited one: the mod names the
    # factions explicitly next to the token this actor is gated on. Consulted before the
    # prerequisite hop, and filtered by what the mod declares, like every other path here.
    if vfi:
        for chunk in ((b.get("Prerequisites") or "") + "," + (b.get("Queue") or "")).split(","):
            tok = chunk.strip().lstrip("~!").strip().lower()
            if tok and tok in vfi:
                found |= {f for f in vfi[tok] if not known or f in known}
    # Already decided at this level — do not dilute a direct gate with an inherited one.
    if found or rules is None or _depth <= 0:
        return sorted(found)
    _seen = set() if _seen is None else _seen
    for chunk in (b.get("Prerequisites") or "").split(","):
        tok = chunk.strip().lstrip("~!").strip()
        if not tok or tok.lower() in _seen:
            continue
        _seen.add(tok.lower())
        key = rules._actor_ci.get(tok.lower())
        if not key:
            continue
        try:
            parent = rules.resolve(key)
        except Exception:                       # a prerequisite that does not resolve is not fatal
            continue
        found |= set(factions_of(parent, known, rules, _depth - 1, _seen, vfi))
    return sorted(found)


def extract(mod_id):
    spec = PEERS[mod_id]
    label, cands, rifle_id, expect = spec["label"], spec["root"], spec["rifle"], spec["expect"]
    T = traits_for(spec)
    root = find_checkout(cands, mod_id)
    if root is None:
        return label, None, f"no checkout found (looked in {', '.join(cands)})"
    rules = miniyaml.Ruleset(root, mod_id)
    fluent = load_fluent(root, mod_id)
    known_factions = declared_factions(rules)
    # EMBER's index (devin/ember/untagged-ca-sp), consulted at CLAIM level rather than as a
    # last resort — see factions_of. Their version is the broader read: it takes every
    # `ProvidesPrerequisite*` trait and falls back to the PROVIDING actor's `ValidFactions`,
    # which a `ValidatedFaction`-only reader misses.
    vfi = prerequisite_providers(rules, known_factions)

    key = rules._actor_ci.get(rifle_id.lower())
    if not key:
        return label, None, f"rifle actor {rifle_id} not present"
    rifle = rules.resolve(key)
    rhp = trait(rifle, T["health"], "HP")
    rcost = trait(rifle, T["cost"], "Cost")
    if not rhp:
        return label, None, f"{rifle_id} has no resolvable HP"
    rhp, rcost = int(rhp), int(rcost or 0)
    note = ""
    if expect and rhp != expect:
        note = (f"⚠ documented anchor was {expect:,}; the checkout resolves **{rhp:,}** — "
                "the artifact wins")

    rows = []
    for actor in sorted(rules.actors):
        if actor.startswith(("^", "-")):
            continue
        node = rules.resolve(actor)
        if node is None:
            continue
        if not any(c.key.split("@")[0] == "Buildable" for c in node.children):
            continue
        # ⛔ `~disabled` MEANS THE MOD SHIPS IT UNBUILDABLE, and such a row is not a reference.
        # OpenRA gates its critters on a prerequisite nothing ever grants — the dinosaurs, the
        # Visceroid, the giant ants — while leaving the `Buildable` block in place so the map
        # editor can still place them. They therefore arrived in the corpus as ordinary infantry
        # tagged `gdi/nod` with 100,000 HP, and the matcher, having spent the real units on the
        # originals, handed the leftovers to Cameo's expansion units: `td_gdi_officer` drew a
        # Triceratops, `td_gdi_shotgunner` a Stegosaurus, `td_gdi_heavysniper` a Velociraptor.
        #
        # ⭐ This is the DATA'S OWN test, not a name blocklist. A blocklist of dinosaur names
        # would rot the moment a mod adds a critter, and would never have caught OpenRA Red
        # Alert's `HIND` — which carries `~disabled` too, and so is honestly NOT an available
        # reference for our Hind even though the unit exists in the files.
        b = _buildable(node)
        prereq = (({k.key: k.value for k in b.children}).get("Prerequisites") or "") if b is not None else ""
        if any(c.strip().lstrip("~!").lower() == "disabled" for c in prereq.split(",")):
            continue
        hp = trait(node, T["health"], "HP")
        if not hp:
            continue
        cost = trait(node, T["cost"], "Cost")
        speed = trait(node, T["speed"], "Speed")
        # BuildLimit marks a mod's one-off epic/hero. It is emitted rather than dropped here so
        # the reference document stays a COMPLETE record of each mod; the distribution engine
        # filters it, where the choice is visible and auditable.
        limit = trait(node, ("Buildable",), "BuildLimit")
        ts, turreted = turn_speed(node)
        wep = weapon_stats(rules, node, label)
        rows.append({
            "id": actor, "name": unit_name(actor, node, fluent),
            "type": unit_type(node), "turn_speed": ts, "turreted": turreted,
            # ⭐ THE FACTION COLUMN (maintainer 2026-09-04). Reference routing needs it: an Asian
            # Alliance unit may only draw on Mental Omega China, which is what stops
            # "Animal Alligator" from ever being a candidate.
            "faction": "/".join(factions_of(node, known_factions, rules, vfi=vfi)) or "",
            "limit": int(limit) if (limit and str(limit).strip().isdigit()) else None,
            **wep,
            "hp": int(hp), "cost": int(cost) if cost else None,
            "speed": int(speed) if speed else None,
            "x_hp": int(hp) / rhp,
            "x_cost": (int(cost) / rcost) if (cost and rcost) else None,
        })
    return label, {"root": root, "rifle": (rifle_id, rhp, rcost), "rows": rows,
                   "note": note}, None


def main():
    # ⚠ A LABEL MUST NOT CONTAIN "(". Document 5's section headings are `## <label>  (N units)`,
    # and the synthesis reads them back with `line[3:].split("(")[0]` — so a parenthesised label
    # is TRUNCATED on the way in. That has bitten twice: "Romanov's Vengeance (live)" silently
    # failed to match a de-duplication rule, and "Yuri's Revenge (OpenRA)" would have collapsed
    # into Document 1's separate "Yuri's Revenge" source, merging two different mods without a
    # word. Fail loudly here instead.
    bad = {k: v["label"] for k, v in PEERS.items() if "(" in v["label"]}
    if bad:
        raise SystemExit(f"peer labels must not contain '(' — they are truncated by the "
                         f"Document 5 heading parser: {bad}")

    ap = argparse.ArgumentParser(description=__doc__.split("\n")[0])
    ap.add_argument("--mod", choices=sorted(PEERS), action="append")
    ap.add_argument("--dry-run", action="store_true")
    args = ap.parse_args()

    out = ["# Original units — OpenRA peer crossovers (Combined Arms, Shattered Paradise)", "",
           "_AUTO-GENERATED by `tools/reference/extract_peer_units.py` from each mod's own "
           "checkout, read through `miniyaml.Ruleset` so the `Inherits:` chains actually "
           "resolve. Do not hand-edit._", "",
           "Companion to [`ORIGINAL_UNITS_RAW.md`](ORIGINAL_UNITS_RAW.md) (the RA2 family) and "
           "[`ORIGINAL_UNIT_STATS.md`](ORIGINAL_UNIT_STATS.md) (the source games). Those two "
           "carry CA and SP as ROLE BANDS only, so before this document neither could vote on a "
           "named unit in the synthesis.", "",
           "**Each mod sets its own power level**, so `×rifle` is the only comparable column. "
           "Anchors are verified against the checkout, never trusted from a document.", ""]

    total = 0
    for mod_id in (args.mod or sorted(PEERS)):
        label, data, err = extract(mod_id)
        if err:
            print(f"{label}: {err}")
            out += [f"## {label}", "", f"⚠ not extracted — {err}", ""]
            continue
        rid, rhp, rcost = data["rifle"]
        total += len(data["rows"])
        print(f"{label}: {len(data['rows'])} buildable units "
              f"(rifle {rid} = {rhp:,} HP / {rcost} cr){'  ' + data['note'] if data['note'] else ''}")
        out += [f"## {label}  ({len(data['rows'])} buildable units)", "",
                f"Checkout: `{data['root']}` · rifle anchor **`{rid}` = {rhp:,} HP / "
                f"{rcost} credits = 1.00×**"]
        if data["note"]:
            out += ["", data["note"]]
        out += ["", "| id | unit | type | faction | HP | ×rifle | Cost | ×rifle cost | Speed | "
                "Turn | Turret | Limit | Range | Dmg | Burst | Reload | DPS | vsINF | vsVEH | "
                "vsAIR | vsBLD | Evidence | Reason |",
                "|---|---|---|---|--:|--:|--:|--:|--:|--:|:-:|--:|--:|--:|--:|--:|--:|--:|--:|--:|--:"
                "|---|---|"]
        for r in sorted(data["rows"], key=lambda x: -x["x_hp"]):
            xc = f"{r['x_cost']:.2f}" if r["x_cost"] else "—"
            cost = f"{r['cost']:,}" if r["cost"] else "—"
            spd = r["speed"] if r["speed"] else "—"
            out.append(f"| `{r['id']}` | {r['name']} | {r['type']} | "
                       f"{r.get('faction') or '—'} | {r['hp']:,} | "
                       f"{r['x_hp']:.2f} | {cost} | {xc} | {spd} | "
                       f"{r['turn_speed'] if r['turn_speed'] else '—'} | "
                       f"{'Y' if r['turreted'] else 'n'} | "
                       f"{r['limit'] if r['limit'] else '—'} | "
                       + " | ".join(
                           (f"{r.get(k):,.0f}" if isinstance(r.get(k), (int, float)) else "—")
                           for k in ("w_range", "w_damage", "w_burst", "w_reload", "w_dps"))
                       + " | " + " | ".join(
                           (f"{r.get(k):.2f}" if isinstance(r.get(k), (int, float)) else "—")
                           for k in ("eff_vs_INF", "eff_vs_VEH", "eff_vs_AIR", "eff_vs_BLD"))
                       + " | " + str(r.get("w_evidence") or "—")
                       + " | " + str(r.get("w_evidence_reason") or "—").replace("|", "/")
                       + " |")
        out.append("")

    if args.dry_run:
        print(f"DRY RUN: {total} rows, nothing written")
        return 0
    OUT.write_text("\n".join(out) + "\n", encoding="utf-8")
    print(f"wrote {OUT.relative_to(ROOT)} ({total} rows)")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
