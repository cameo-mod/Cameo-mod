#!/usr/bin/env python3
"""ONE armament = ONE reference. The pairing key that stops a cannon being averaged with a missile.

⛔ THE MAINTAINER'S ORDER (2026-09-13), in full, because every line below implements one clause:

    "we need to be careful because those fire bombs and the sidewinder anti air missiles have
     separate ranges ... Those two weapons are so different they should not be mixed. actually
     this should be true for any dual or multi weapon units so the correct weapon is mapped ...
     you need to separate those two weapons, use DTA for the missile as reference only while
     using the cannon reference from all 3! Adjust all the existing units with that logic!"

THE CASE, MEASURED, so nobody has to take the defect on trust (`td_gdi_firehawk`):

    Armament            td_gdi_firehawk_firehawkmissiles_AA   role=air      range 12500
    Armament@BOMBS      td_gdi_firehawk_firehawkbomb          role=ground   range  1250

A TEN-FOLD range gap inside one actor. `armament_profile` reports `max(ranges)`, so whichever
survives the fold hands the other one its number. The same shape sits on every mammoth:

    td_gdi_mammothtank  120mmdual  role=ground  |  mammothmissiles  role=both

⭐ AND THE REFERENCES ALREADY SEPARATE THE SAME WAY, which is why this is a pairing problem and
not an extraction problem — no corpus needs regenerating (measured 2026-09-13):

    CA   4TNK  130mm      ground 4864  |  MammothTusk      Air,AirSmall,Infantry   both 6656
    CnC  HTNK  120mmDual  ground 4864  |  MammothMissiles  Ground,Water,Air        both 4864
    RA   4TNK  120mm      ground 4864  |  MammothTusk      AirborneActor,Infantry  both 6656

Three sources, three cannons, three missiles, and the roles line up without a single name match.

────────────────────────────────────────────────────────────────────────────────────────────────
THE ROLE VOCABULARY IS NOT NEW — IT IS THE MAINTAINER'S MISSILE RULING (2026-09-07), REUSED:

    ground only  ->  MissileHE        air only  ->  MissileAA        both  ->  MissileAP

`audit_missile_role_family.weapon_role` already implements it for the audit. This module does NOT
replace it and must never be merged into it: that audit carries four LOWER-ONLY ratchets, so it
fails CLOSED to "custom" the moment a weapon's ValidTargets holds any token outside
{Ground, Water, Air} — correct for a ratchet, useless for pairing, because "Ground, Water, Trees"
is 31 CA armaments and "AirborneActor, Infantry" is the entire Red Alert mammoth missile.

So this classifier reads a WIDER, explicitly enumerated vocabulary and still fails closed on
anything it has not seen. `test_armament_roles` pins that the two AGREE on every weapon the audit
is willing to judge; the wider set may only add answers, never change one.

⚠ IT ALSO SUPERSEDES `reference_distribution.is_anti_air_armament` FOR PAIRING ONLY. That helper
tests the NAME (`@AA`, `_AA`) and is left exactly as it is — it is load-bearing for the existing
baseline selection. But the name test cannot see 50 air-role armaments spelled otherwise
(`RA2Patriot`, `RA2TURRETFLAKAA`, `AsianPhotonCannon`, `PortableFlak`, `BallistaSingleShotAir`),
measured 2026-09-13, and a name blocklist is the mistake this codebase has paid for repeatedly.
The targeting envelope is the SOURCE; the slot spelling is a convention.
"""
from __future__ import annotations

import pathlib
import sys

sys.path.insert(0, str(pathlib.Path(__file__).resolve().parent))
sys.path.insert(0, str(pathlib.Path(__file__).resolve().parents[1] / "audit"))

ROLE_GROUND = "ground"
ROLE_AIR = "air"
ROLE_BOTH = "both"
ROLE_SPECIAL = "special"          # neither domain survives — an interceptor, a healer, a snare
ROLES = (ROLE_GROUND, ROLE_AIR, ROLE_BOTH, ROLE_SPECIAL)

# ── THE TOKEN SETS, MEASURED 2026-09-13 OVER BOTH CORPORA, NEVER GUESSED ─────────────────────
# Cameo resolves 59 distinct ValidTargets tokens and 37 InvalidTargets tokens; the OpenRA peer
# corpus adds 38 more. Every token either places the weapon in a DOMAIN or it does not; a token
# that does not is NEUTRAL and must not tilt the answer either way, which is why the three sets
# are written out instead of "anything not air is ground".
AIR_TARGETS = frozenset({
    "Air", "AirSmall", "AirborneActor", "Aircraft",
})

GROUND_TARGETS = frozenset({
    "Ground", "Water", "Underwater", "Trees", "GroundActor", "WaterActor",
    "Infantry", "Vehicle", "Structure", "Building", "Ship", "Submarine", "Bridge",
    "Wall", "wall", "Defense", "Barrel", "Husk", "Cyborg", "Monster", "Tank",
    "NavalYard", "TeslaCoil", "Epic", "Garrisoned", "Ant", "Paratrooper", "Hero",
    "MADTank", "Mine", "Broodling",
})

# Selectors and status markers. `Missile` / `ICBM` / `BallisticMissile` are here deliberately: a
# point-defence laser reads `Ground, Water, Missile` and is a GROUND weapon that also swats
# rockets, while a weapon that targets ONLY missiles belongs to neither domain and must land in
# ROLE_SPECIAL rather than be mislabelled as air. Membership here asserts "carries no domain".
NEUTRAL_TARGETS = frozenset({
    "Missile", "ICBM", "BallisticMissile", "Bullet", "BulletAS",
    "Repairable", "Repair", "Repairing", "SpecialRepair", "Heal", "Healable",
    "Healing", "HealableCyborg", "NoCrew", "Resupplying",
    "C4Attached", "C4Plantable", "TNTAttached", "TNTPlantable",
    "MindControllable", "MindControl", "MindControlStructure", "Psionic",
    "Hackable", "HackableRobot", "Chronobeamable", "Temporal", "Shielded", "shielded",
    "TerrorDronable", "terrordroned", "SquidGrab", "SquidGrabbed", "DiskSteal",
    "PrismTowerChargeable", "TeslaBoost", "RobotEnergizable", "PowerDrainable",
    "ResourceDrainable", "DetonateAttack", "WatcherParasiteAttachable",
    "LeechEggLaid", "LeechEggKill", "SlaveFaster", "BlackHole",
    "ivanattached", "ivanattachable", "lockdowned", "lockable", "lockon", "plagued",
    "blindable", "snareable", "demolishable", "bloodlust_target", "haste_target",
    "undeadtarget", "wc2_runes", "plymouth_minv", "StickyFoamed",
    "ToxinImmune", "MCImmune", "DogImmune", "DevourImmune",
})

# ⛔ `WeaponInfo.cs:116` — a weapon that never writes ValidTargets is `Ground, Water`, i.e. GROUND.
# Absence is a VALUE here, not a gap, and both corpora lean on it: 451 of the ~900 peer armaments
# and the majority of Cameo weapons leave it unwritten.
ENGINE_DEFAULT_TARGETS = "Ground, Water"


def _tokens(raw):
    return {t.strip() for t in str(raw or "").split(",") if t.strip()}


def role_of_targets(valid, invalid=None):
    """(role, unknown_tokens) from a weapon's targeting envelope.

    `valid` None/empty means the engine default, NOT "unknown" — see ENGINE_DEFAULT_TARGETS.
    Unknown tokens are RETURNED rather than swallowed so `--audit` can prove the vocabulary still
    covers the corpus; they are treated as neutral, so a new marker can never silently flip a
    weapon's domain.
    """
    targets = _tokens(valid) or _tokens(ENGINE_DEFAULT_TARGETS)
    targets -= _tokens(invalid)
    unknown = targets - AIR_TARGETS - GROUND_TARGETS - NEUTRAL_TARGETS
    air = bool(targets & AIR_TARGETS)
    ground = bool(targets & GROUND_TARGETS)
    if air and ground:
        return ROLE_BOTH, unknown
    if air:
        return ROLE_AIR, unknown
    if ground:
        return ROLE_GROUND, unknown
    return ROLE_SPECIAL, unknown


# ── THE ONE RATE FORMULA, ONE IMPLEMENTATION ─────────────────────────────────────────────────
def cycle_ticks(reload_delay, burst=1, burst_delays=()):
    """Ticks from the START of one burst to the next: ReloadDelay + (Burst-1) x mean(BurstDelays).

    Identical convention to `reference_distribution.burst_cycle`, OpenRA's own default delay of 5
    included, so a Cameo armament and a peer armament are measured on ONE ruler. It lives here as
    well because the peer corpus hands the delays as a LIST while the ledger hands them as a
    STRING, and a second parser is the only alternative to a second formula.
    """
    try:
        rel = float(reload_delay)
    except (TypeError, ValueError):
        return None
    try:
        n = int(float(burst))
    except (TypeError, ValueError):
        n = 1
    if n <= 1:
        return rel or None
    delays = []
    for d in (burst_delays or ()):
        try:
            delays.append(float(d))
        except (TypeError, ValueError):
            pass
    mean = (sum(delays) / len(delays)) if delays else 5.0
    return rel + (n - 1) * mean


def _num(x):
    try:
        v = float(str(x))
    except (TypeError, ValueError):
        return None
    return v


# ⛔ RANGE IS NOT ONE UNIT ACROSS THE CORPUS AND MUST NEVER BE PRINTED AS IF IT WERE. Cameo and
# the OpenRA peers state range in WDist; the TS/RA2 INI sources state it in CELLS, so a DTA mammoth
# reads `5.7` beside a Cameo `6141`. The distributions never noticed because every coordinate is
# normalised WITHIN its own source — but a side-by-side pairing report is read by a human, and
# `6141 <-> 5.7` is the kind of line that gets believed. Each view therefore carries its unit, and
# the conversion is offered as a SEPARATE derived field that says where it came from.
WDIST_PER_CELL = 1024          # OpenRA's cell size; the TS lepton grid is the same 1/256 cell
RANGE_UNITS = {"wdist": 1.0, "cells": float(WDIST_PER_CELL)}


def view(side, slot, weapon, role, unknown=(), damage_per_shot=None, burst=1,
         cycle=None, rng=None, requires=None, baseline=True, note=None, range_unit="wdist",
         gate=None):
    """One armament, on EITHER side of the map, in one shape.

    ⛔ `damage_per_cycle` AND `rate` ARE DERIVED HERE AND NOWHERE ELSE. The per-cycle coordinate is
    the comparable magnitude (burst is a delivery choice, not a magnitude), and the rate is the one
    formula: damage_per_shot x burst / cycle. Two call sites deriving them independently is how the
    per-shot / per-burst convention split in the first place.
    """
    burst = int(burst or 1)
    per_cycle = (damage_per_shot * burst) if damage_per_shot else None
    scale = RANGE_UNITS.get(range_unit)
    if scale is None:
        raise ValueError(f"unknown range unit: {range_unit}")
    return {
        "side": side, "slot": slot, "weapon": weapon, "role": role,
        "unknown_targets": sorted(unknown), "range": rng,
        "range_unit": range_unit,
        "range_wdist": (rng * scale) if rng else None,
        "damage_per_shot": damage_per_shot, "burst": burst,
        "damage_per_cycle": per_cycle, "cycle": cycle,
        "rate": (per_cycle / cycle) if (per_cycle and cycle) else None,
        "requires": requires, "baseline": baseline, "gate": gate, "note": note,
    }


# ── CAMEO ────────────────────────────────────────────────────────────────────────────────────
def cameo_weapon_role(rs, weapon):
    """(role, unknown) for one Cameo weapon, resolved — never hand-parsed (rule 8e)."""
    try:
        node = rs.resolve_weapon(weapon)
    except Exception:
        return None, ()
    if node is None:
        return None, ()
    get = lambda key: next((c.value for c in node.children if c.key == key), None)  # noqa: E731
    return role_of_targets(get("ValidTargets"), get("InvalidTargets"))


def cameo_views(rec, rs):
    """Every PRICED armament of one ledger record, one view each.

    Baseline membership uses the SHARED built-state evaluator (`formula.condition_holds_by_default`
    via `reference_distribution.is_upgrade_gated`) so an upgraded barrel, an elite rank and an IFV
    passenger mode are all recognised as alternatives rather than parallel weapons — the same rule
    `baseline_armaments` applies, not a second opinion about it.
    """
    import reference_distribution as rd
    out = []
    for arm in (rec.get("armaments") or []):
        if not arm.get("pricing"):
            continue
        role, unknown = cameo_weapon_role(rs, arm.get("weapon"))
        mains = [w for w in (arm.get("damage_warheads") or []) if (_num(w.get("damage")) or 0) > 0]
        dmg = sum(_num(w.get("damage")) or 0 for w in mains) or None
        raw = str(arm.get("burstdelays") or "").replace(",", " ").split()
        out.append(view(
            "cameo", arm.get("slot"), arm.get("weapon"), role, unknown,
            damage_per_shot=dmg, burst=int(_num(arm.get("burst")) or 1),
            cycle=cycle_ticks(_num(arm.get("reloaddelay")), _num(arm.get("burst")) or 1, raw),
            rng=_num(arm.get("range")), requires=arm.get("requires"),
            baseline=not rd.is_upgrade_gated(arm),
            note=",".join(sorted({w.get("tag") for w in mains if w.get("tag")})) or None))
    return out


# ── OPENRA PEERS (`weapon_evidence`) ─────────────────────────────────────────────────────────
def peer_views(row):
    """Every armament of one OpenRA peer row, from the evidence the extractor already captured.

    ⭐ NOTHING IS RE-EXTRACTED. `weapon_evidence` has carried slot, weapon, range, reload_delay,
    burst, burst_delays, valid_targets, requires_condition and each warhead's damage + versus
    since the Doc 5 emitter shipped; the consumer simply folded them. 410 peer rows carry it, 217
    of those hold more than one armament (measured 2026-09-13).
    """
    out = []
    for arm in (row.get("weapon_evidence") or []):
        role, unknown = role_of_targets(arm.get("valid_targets"))
        dmg = sum(_num(w.get("damage_num")) or 0
                  for w in (arm.get("warheads") or [])
                  if (_num(w.get("damage_num")) or 0) > 0) or None
        out.append(view(
            "peer", arm.get("slot"), arm.get("weapon"), role, unknown,
            damage_per_shot=dmg, burst=int(_num(arm.get("burst")) or 1),
            cycle=cycle_ticks(arm.get("reload_delay"), arm.get("burst") or 1,
                              arm.get("burst_delays") or ()),
            rng=_num(arm.get("range")), requires=arm.get("requires_condition"),
            baseline=_peer_baseline(arm), note=arm.get("cadence_reason")))
    return out


def _peer_baseline(arm):
    """An armament with no requirement, or one gated on the ABSENCE of an upgrade, is baseline.

    The peer corpus spells conditions as OpenRA expressions (`!hypersonic-upgrade && ...`), the
    same grammar `formula.condition_holds_by_default` evaluates, so the shared evaluator decides.
    A condition it cannot parse FAILS CLOSED to non-baseline — an upgraded barrel counted as
    baseline would double a unit's armament set, which is the error this whole lane exists to fix.
    """
    cond = arm.get("requires_condition")
    if not cond:
        return True
    import formula
    try:
        return bool(formula.condition_holds_by_default(cond))
    except Exception:
        return False


# ── INI PEERS (`w_*` / `w2_*`) ───────────────────────────────────────────────────────────────
# ⛔ DTA AND ITS SIBLINGS ALREADY SHIP TWO SLOTS PER ROW — `w_*` is Primary=, `w2_*` is Secondary=.
# Their role does NOT come from ValidTargets (the TS/RA2 engine has no such field) but from the
# PROJECTILE's `AA=` / `AG=` flags, which live in each source's Rules.ini and are captured by
# `extract_ini_projectile_roles.py` into `docs/reference/ini_projectile_role_evidence.json`.
#
# ⚠ AND THE NAME IS A TRAP, WHICH IS WHY THE FLAGS ARE READ AND NOT THE SPELLING. DTA's
# `[AGHeatSeeker2]` — the mammoth Tusk's projectile, "AG" right there in the name — declares
# `$Inherits=AGHeatSeeker` and then `AA=yes`. It is a DUAL-role missile. A name classifier calls
# it anti-ground and pairs the mammoth's air missile against a peer cannon.
def ini_views(rec, projectile_roles=None, elite_weapons=None):
    """Every INI weapon slot as a view; role None when the projectile evidence has no verdict.

    ⛔ THERE ARE THREE SLOTS, NOT TWO — `Primary=`, `Secondary=` AND `Elite=`. The corpus carries
    only the first two and the maintainer had to point that out twice: *"it's the elite weapon
    which replaces the dummy weapon so on elite it has one cannon and one missile launcher."* DTA's
    `[MTNK]` is `Primary=90mmDummy` (zero damage, it only sets the rate of fire), `Secondary=90mm`,
    `Elite=70mmMsl1` — so its real loadout is a cannon AND a missile launcher, and reading two
    slots made it look like a single-cannon tank. `extract_ini_elite_weapons` supplies the third.
    """
    out = []
    roles = projectile_roles or {}
    source = rec.get("source")
    for prefix, slot in (("w_", "primary"), ("w2_", "secondary")):
        weapon = rec.get("weapon" if prefix == "w_" else "w2_weapon")
        dmg = _num(rec.get(prefix + "damage"))
        if not weapon and dmg is None:
            continue
        proj = rec.get(prefix + "projectile")
        verdict = roles.get((source, proj)) or roles.get((source, str(proj).lower()))
        role = verdict.get("role") if verdict else None
        burst = int(_num(rec.get(prefix + "burst")) or 1)
        out.append(view(
            "ini", slot, weapon, role, (),
            damage_per_shot=dmg, burst=burst,
            # ⛔ THE 694 UNFOLDED RATES STAY BLOCKED. Only 77 INI rows declare a burst delay, in
            # Phobos/Ares notation that is not OpenRA's, so the cycle is left None rather than
            # invented — the view still carries range, damage and burst, which is what pairs.
            cycle=cycle_ticks(rec.get(prefix + "reload"), 1, ()) if burst <= 1 else None,
            rng=_num(rec.get(prefix + "range")), requires=None, baseline=True,
            range_unit="cells",
            note=("projectile=%s" % proj) if proj else None))
    elite = (elite_weapons or {}).get((source, str(rec.get("id") or "")))
    if elite:
        burst = int(_num(elite.get("burst")) or 1)
        out.append(view(
            "ini", "elite", elite.get("weapon"), elite.get("role"), (),
            damage_per_shot=_num(elite.get("damage")), burst=burst,
            cycle=cycle_ticks(elite.get("reload"), 1, ()) if burst <= 1 else None,
            rng=_num(elite.get("range")), requires="rank-elite",
            # ⚠ NOT BASELINE — it is rank-gated, exactly like Cameo's own `_elite` armaments and
            # the peer mods' upgrade barrels, and `strongest_by_role` therefore skips it for any
            # role a baseline weapon already fills. `pair_by_role` lets it cover a role that would
            # otherwise be EMPTY, which is the whole point: the reference does carry that weapon.
            baseline=False, range_unit="cells", gate="elite",
            note=("elite, replaces %s%s" % (elite.get("replaces"),
                                            "" if elite.get("replaces_dummy_primary")
                                            else " (an upgrade of it, not an extra weapon)"))))
    return out


# ── PAIRING ──────────────────────────────────────────────────────────────────────────────────
# ⭐ `both` IS ADJACENT TO EVERY DOMAIN, and that is the whole reason a strict equality key fails.
# The Cameo mammoth's missile is `both` (Ground, Water, Air); CnC's MammothMissiles is `both` too,
# but RA's Tusk is `AirborneActor, Infantry` — also `both` — while a Cameo MissileHE rocket is
# `ground`. Pairing must therefore prefer an exact role and ACCEPT a `both` on either side as the
# nearest legal partner, never pair `air` with `ground`. That single forbidden pair is the
# maintainer's rule: "those two weapons are so different they should not be mixed."

# Which Cameo weapon an UNPROVEN peer armament may stand against, in order. Ground first, because
# DESIGN's `anti_air_vehicle` anchor prices on the ground weapon and a TS/RA2 `Primary=` is
# overwhelmingly a main gun. `special` last: an interceptor is never a stand-in for a real gun.
UNPROVEN_PREFERENCE = (ROLE_GROUND, ROLE_BOTH, ROLE_AIR, ROLE_SPECIAL)

COMPATIBLE = {
    ROLE_GROUND: (ROLE_GROUND, ROLE_BOTH),
    ROLE_AIR: (ROLE_AIR, ROLE_BOTH),
    ROLE_BOTH: (ROLE_BOTH, ROLE_GROUND, ROLE_AIR),
    ROLE_SPECIAL: (ROLE_SPECIAL,),
}


def strongest_overall(views, baseline_only=True):
    """The single hardest-hitting baseline armament, role or no role."""
    pool = [v for v in views if not baseline_only or v["baseline"]]
    if not pool:
        return None
    return max(pool, key=lambda v: v["damage_per_cycle"] or 0)


def candidates_by_role(views, baseline_only=True):
    """{role: [views, hardest-hitting first]} — the full bench, not just the starter.

    ⛔ ONLY THE STRONGEST WAS KEPT AND THAT LOST THE MAINTAINER'S OWN CASE. DTA's GDI Medium Tank
    at elite carries TWO ground-role weapons — `90mm` (a cannon, `Warhead=AP`) and `70mmMsl1` (a
    missile launcher, `Warhead=BazAP`) — because the TS engine puts BOTH on the ground domain.
    Cameo's `td_gdi_battletank` carries a `ground` cannon and a `both` missile. The cannon claimed
    DTA's only ground candidate and the missile was then told no source covers its role, when the
    source plainly has a second weapon it could stand against.

    ⚠ AND A DELIVERY AXIS WOULD NOT HAVE FIXED IT HONESTLY. The obvious idea — classify guided vs
    unguided and match missile to missile — has no reliable source signal on the INI side: `ROT`
    (rate of turn) reads as the homing field but `TracerM`, an ordinary gun tracer, declares
    `ROT=1`, and 426 of 433 sections that declare it are above zero. `Image=DRAGON` and
    `Warhead=BazAP` are names, which is the trap this whole module exists to avoid. A bench needs
    no semantics at all: the reference's SECOND weapon in a compatible role stands for the Cameo
    unit's second weapon, ranked by the per-cycle coordinate.
    """
    bench = {}
    for v in views:
        if baseline_only and not v["baseline"]:
            continue
        if v["role"] is None:
            continue
        # ⛔ CLAUSE 5 OF THE MATCHING LAW — "a zero-damage row never matches a combat unit." The
        # first bench let DTA's `90mmDummy` stand as the Battle Tank's missile reference: it is a
        # real armament with a real range, and its only job is to set the rate of fire for the
        # weapon in the next slot. A dummy is never anybody's reference.
        if not v["damage_per_cycle"]:
            continue
        bench.setdefault(v["role"], []).append(v)
    for role in bench:
        bench[role].sort(key=lambda v: v["damage_per_cycle"] or 0, reverse=True)
    return bench


def strongest_by_role(views, baseline_only=True):
    """{role: view} — the hardest-hitting BASELINE armament in each role.

    ⚠ MAX WITHIN A ROLE, NEVER SUM ACROSS ONE. `td_gdi_lighttankmkii` reports a 1-damage point
    defence laser as its gun when the first armament wins, and `ra2_allies_ifv` sums 39 mutually
    exclusive passenger weapons when the set does; within ONE role the alternatives are still
    alternatives, so the representative is the strongest, ranked on the per-cycle coordinate.
    """
    best = {}
    for v in views:
        if baseline_only and not v["baseline"]:
            continue
        if v["role"] is None or not v["damage_per_cycle"]:
            continue                       # clause 5 — see `candidates_by_role`
        cur = best.get(v["role"])
        if cur is None or (v["damage_per_cycle"] or 0) > (cur["damage_per_cycle"] or 0):
            best[v["role"]] = v
    return best


def pair_by_role(cameo_views_, peer_views_, baseline_only=True):
    """([(role, cameo_view, peer_view, exact)], cameo_unmatched, peer_unmatched).

    Exact role matches are claimed FIRST across every role, and only then is a `both` allowed to
    stand in — otherwise a unit carrying a `both` missile and a `ground` cannon could see its
    cannon claim the peer's `both` missile before the missile ever got a turn.

    ⛔ A SOURCE THAT CANNOT STATE A ROLE STILL VOTES ON THE MAIN WEAPON — see `_unproven_pair`.
    Dropping it entirely is how the first draft of this function silently blanked every RA2 unit.
    """
    cam = strongest_by_role(cameo_views_, baseline_only)
    bench = candidates_by_role(peer_views_, baseline_only)
    # ⭐ A RANK-GATED PEER WEAPON JOINS THE BENCH, at the BACK — it never displaces an ordinary one.
    # DTA's GDI Medium Tank fires its missile only at elite rank, and the maintainer's rule is
    # about POSSESSION, not availability: *"if the reference unit does not have the weapon it
    # should not vote on it"* — it does have it. It is flagged (`gate`) so the map can show what
    # kind of vote it is.
    for role, gated in candidates_by_role(peer_views_, baseline_only=False).items():
        for candidate in gated:
            if candidate.get("gate") and candidate not in bench.get(role, ()):
                bench.setdefault(role, []).append(candidate)

    taken, pairs = set(), []

    def claim(role, want):
        for candidate in bench.get(want, ()):
            key = (want, candidate["slot"], candidate["weapon"])
            if key not in taken:
                taken.add(key)
                return candidate
        return None

    for role in ROLES:                       # exact matches first, across every role
        if role not in cam:
            continue
        partner = claim(role, role)
        if partner is not None:
            pairs.append((role, cam[role], partner, True))
    matched = {p[0] for p in pairs}
    for role in ROLES:                       # then the nearest legal stand-in
        if role in matched or role not in cam:
            continue
        for want in COMPATIBLE[role]:
            partner = claim(role, want)
            if partner is not None:
                pairs.append((role, cam[role], partner, False))
                break
    if not pairs:
        unproven = _unproven_pair(cameo_views_, peer_views_, baseline_only)
        if unproven is not None:
            pairs.append(unproven)
    matched_cam = {p[0] for p in pairs}
    matched_peer = {(p[2]["slot"], p[2]["weapon"]) for p in pairs}
    peer_best = strongest_by_role(peer_views_, baseline_only)
    return (pairs,
            [v for r, v in sorted(cam.items()) if r not in matched_cam],
            [v for _r, v in sorted(peer_best.items())
             if (v["slot"], v["weapon"]) not in matched_peer])


def _unproven_pair(cameo_views_, peer_views_, baseline_only):
    """The MAIN weapon against the MAIN weapon, when the peer's role cannot be established.

    ⛔ THIS EXISTS BECAUSE ITS ABSENCE WAS A REGRESSION, CAUGHT BY LOOKING AT THE OUTPUT. Seven of
    the nine INI sources pin no `source_sha256`, so `extract_ini_projectile_roles` refuses to state
    their projectiles' domains and every one of their armaments arrives with `role = None`. The
    first draft simply skipped those views — and `ra2_soviets_apocalypsetank` (5 sources),
    `ra2_allies_ifv` (5), `yuri_gatlingtank` (5) and `ra2_soviets_flaktrack` (4) came back with
    **zero** votes on any weapon. That is the exact shape of PR #369: a guard that looks green by
    having nothing left to guard.

    ⭐ THE MAINTAINER'S RULE IS SATISFIED EITHER WAY — *"if the reference unit does not have the
    weapon it should not vote on it"*. We cannot prove such a source has the ANTI-AIR weapon, so it
    does not vote on it. We can see it has a main gun, so it votes there and nowhere else. The pair
    is flagged `exact=False` and the peer view keeps `role: None`, so a reader can always tell a
    proven pairing from a carried-over one.

    ⛔ AND IT PAIRS AGAINST THE GROUND WEAPON, NOT THE STRONGEST ONE. The first version took the
    Cameo actor's hardest hitter and immediately produced the one thing the ruling forbids: on
    `ra2_soviets_apocalypsetank` the AA missile out-damages the cannon (32,000 vs 24,000), so five
    peer CANNONS were matched against an anti-air missile. `UNPROVEN_PREFERENCE` is the fix and it
    is not a new rule either — DESIGN's `anti_air_vehicle` anchor already says to price on the
    ground weapon, and `baseline_armaments` already implements that ordering.
    """
    peer = strongest_overall([v for v in peer_views_ if v["role"] is None], baseline_only)
    if peer is None or not peer["damage_per_cycle"]:
        return None
    by_role = strongest_by_role(cameo_views_, baseline_only)
    for role in UNPROVEN_PREFERENCE:
        if role in by_role:
            return (role, by_role[role], peer, False)
    return None
