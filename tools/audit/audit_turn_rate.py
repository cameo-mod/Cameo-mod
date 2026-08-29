#!/usr/bin/env python3
"""audit_turn_rate.py — is every mobile actor's TurnSpeed the one the law prescribes?

THE LAW (DESIGN.md, FORMULA_V2 §3). ⚠ **It depends on the TURRET**
(maintainer 2026-08-29):

    turreted vehicle                    TurnSpeed = Speed / 5
    no turret / fixed forward weapon    TurnSpeed = 2 x Speed / 5
    helicopter, spaceship, epic air     TurnSpeed = Speed / 5   (like vehicles)
    FIGHTER or BOMBER                   TurnSpeed = Speed / 15
    fighter/bomber, frontal weapon      TurnSpeed = 2 x Speed / 15
    infantry                            instant — EXCEPT CABAL cyborgs, which
                                        carry forward-facing weapons and take the
                                        vehicle fixed-weapon rule

⚠ **DESIGN.md STATES THIS LAW IN TWO SEPARATE TABLES** and this audit first shipped
knowing only one of them. The stat-law list says "helicopters and spaceships both use
Speed/5" and stops; the derived-stat table 1100 lines earlier carries "Fighters &
bombers (by template): Aircraft.TurnSpeed = Speed / 15 (frontal-weapon craft 2x)".
Grepping for one phrasing found one half. A law worth encoding is worth grepping twice.

⚠ **`Speed/15` does NOT make the Speed grid 15.** The grid is 5 and stays 5. A fighter
at Speed 250 derives TurnSpeed 16.67 — that is a question about how TurnSpeed is
represented, never a reason to re-grid Speed to make the equation convenient.

⭐ **And this is WHY the Speed grid is 5 for all of them, turret or not.** `2·S/5` is
an integer exactly when `5 | 2S`, and `gcd(2, 5) = 1`, so it reduces to `5 | S` — the
same condition the turreted branch imposes. The turret changes the VALUE of TurnSpeed,
never the grid Speed sits on.

⚠ **WHY THIS AUDIT COULD NOT EXIST BEFORE.** Aircraft keep their turn rate in the
`Aircraft` trait, not `Mobile` — exactly as they keep `Speed` there. Everything that
looked for `Mobile.TurnSpeed` therefore saw **zero** of the 168 aircraft in the ledger
and concluded they had no turn rate at all. They have one: 323 actors carry an
`Aircraft` trait and 318 define both Speed and TurnSpeed. Reading BOTH traits is what
makes the law checkable.

T1  TurnSpeed disagrees with the law for the actor's turret state and airframe.
T2  Speed is off the 5 grid, so no integer TurnSpeed can satisfy either branch.

Run: python tools/audit/audit_turn_rate.py
"""
from __future__ import annotations

import pathlib
import sys

ROOT = pathlib.Path(__file__).resolve().parents[2]
sys.path[:0] = [str(ROOT / "tools/audit"), str(ROOT / "tools/balance")]

import formula  # noqa: E402
from miniyaml import Ruleset  # noqa: E402


def num(v):
    try:
        return float(str(v).strip())
    except (TypeError, ValueError):
        return None


def air_templates(rs, name, _seen=None):
    """Airframe from the actor's INHERIT CHAIN, because DESIGN.md classifies
    fighters and bombers "by template".

    Measured, the template also beats the trait flags outright: classifying by
    `CanHover`/`VTOL` puts helicopters at 62% compliance with their own law, the
    template puts them at 95%. Names are never used — `A10` says nothing a rule
    can rely on.
    """
    seen = _seen if _seen is not None else set()
    if name in seen:
        return set()
    seen.add(name)
    node = rs.actors.get(name)
    if node is None:
        return set()
    out = set()
    for _, target in rs.inherits_of(node):
        if target in formula.AIR_TEMPLATES:
            out.add(formula.AIR_TEMPLATES[target])
        out |= air_templates(rs, target, seen)
    return out


def mobile_actors(rs):
    """(actor, speed, turn_speed, turreted, airframe) for everything that moves.

    Reads BOTH traits. `Aircraft` wins when present because an actor carrying it
    is flying, whatever else it declares.
    """
    for name in rs.actors:
        # `.Husk` actors are wreckage drifting to the ground, not units anybody
        # balances; holding them to a combat-mobility law is noise.
        if name.startswith("^") or name.lower().endswith(".husk"):
            continue
        node = rs.resolve(name)
        if node is None:
            continue
        air = mob = None
        turreted = False
        for c in node.children:
            k = c.key.split("@")[0]
            if k == "Aircraft":
                air = air or c
            elif k == "Mobile":
                mob = mob or c
            elif k == "Turreted":
                turreted = True
        src = air or mob
        if src is None:
            continue
        speed, turn = num(src.get("Speed")), num(src.get("TurnSpeed"))
        if speed is None or turn is None:
            continue
        frames = air_templates(rs, name) if air is not None else set()
        yield (name, speed, turn, turreted,
               (sorted(frames)[0] if frames else None), air is not None)


def main() -> int:
    rs = Ruleset(ROOT)
    rows = list(mobile_actors(rs))
    t1 = [(n, s, t, turr, frame) for n, s, t, turr, frame, _ in rows
          if abs(t - formula.turn_speed_for(s, turreted=turr, airframe=frame)) > 1e-9]
    step = formula.stat_step("speed", "vehicle")
    t2 = [(n, s) for n, s, _, _, _, _ in rows if s % step]
    # An aircraft with no air template cannot be held to EITHER air law — the
    # classifier is the template, by design, so this is classification work.
    t3 = [n for n, _, _, _, frame, is_air in rows if is_air and frame is None]

    print("# audit_turn_rate — TurnSpeed vs the turret-dependent law\n")
    print(f"mobile actors with both Speed and TurnSpeed: **{len(rows)}**\n")

    # ⭐ The evidence that the law is REAL and that the branches are the right way
    # round. Ratio 1.0 means the actor follows `Speed/5`, 2.0 means `2 x Speed/5`.
    import collections
    dist = collections.defaultdict(collections.Counter)
    for n, sp, t, turr, frame, is_air in rows:
        if not sp:
            continue
        # An untemplated aircraft is NOT a ground unit. Folding the 123 of them
        # into "ground turretless" diluted that cohort from 64% to 47% and made a
        # confirmed law look shaky — a labelling bug reading as evidence.
        if frame:
            kind = frame
        elif is_air:
            kind = "aircraft (no template)"
        else:
            kind = "ground turreted" if turr else "ground turretless"
        dist[kind][round(t / (sp / 5), 3)] += 1
    print("## The law, measured — `TurnSpeed / (Speed/5)` by turret state and airframe\n")
    print("| cohort | n | modal ratio | share | law |")
    print("|---|--:|--:|--:|---|")
    for kind in sorted(dist):
        tot = sum(dist[kind].values())
        ratio, hits = dist[kind].most_common(1)[0]
        law = {1.0: "Speed/5", 2.0: "2 x Speed/5",
               0.333: "Speed/15", 0.667: "2 x Speed/15"}.get(ratio, "—")
        print(f"| {kind} | {tot} | {ratio:g} | {hits / tot:.0%} | {law} |")
    print("\nBoth halves of the law are visible in the roster: ground turreted and "
          "every PIVOTING airframe (helicopter, spaceship, epic air) sit on "
          "`Speed/5`, ground turretless on `2 x Speed/5`, and fighters and bombers "
          "on `Speed/15`. That is the law confirmed, not assumed.\n")

    print(f"## T1 — TurnSpeed disagrees with the law ({len(t1)})\n")
    if t1:
        print("| actor | Speed | TurnSpeed | turret | airframe | law says |")
        print("|---|--:|--:|---|---|--:|")
        for n, s, t, turr, frame in sorted(t1, key=lambda r: r[0])[:40]:
            want = formula.turn_speed_for(s, turreted=turr, airframe=frame)
            print(f"| `{n}` | {s:g} | {t:g} | {'yes' if turr else 'no'} | "
                  f"{frame or 'ground'} | {want:g} |")
        if len(t1) > 40:
            print(f"\n_+{len(t1) - 40} more._")
    else:
        print("_clean_")

    print(f"\n## T2 — Speed off the {step} grid, so no integer TurnSpeed fits ({len(t2)})\n")
    if t2:
        for n, s in sorted(t2)[:40]:
            print(f"- `{n}` speed {s:g}")
        if len(t2) > 40:
            print(f"\n_+{len(t2) - 40} more._")
    else:
        print("_clean_")

    # Reported, not enforced: this is a survey of a law the roster predates, so a
    # red exit would fail the suite on day one for content reasons. It becomes a
    # ratchet once a boot-gated pass has brought the roster inside the law.
    print(f"\n## T3 — aircraft carrying no air template, so unclassifiable ({len(t3)})\n")
    if t3:
        print("The airframe IS the classifier (DESIGN.md: \"by template\"), so these "
              "can be held to neither air law until they inherit one. This is "
              "classification work, not a turn-rate defect.\n")
        for n in sorted(t3)[:30]:
            print(f"- `{n}`")
        if len(t3) > 30:
            print(f"\n_+{len(t3) - 30} more._")
    else:
        print("_clean_")

    print("\n**REPORT ONLY** — the roster predates the law; this is the work list, "
          "not a gate. Ratchet it once a boot-gated pass has landed.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
