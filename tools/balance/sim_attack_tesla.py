#!/usr/bin/env python3
"""sim_attack_tesla.py — a source-derived `AttackTesla` timing model.

⛔ WHY THIS EXISTS. Four numbers have been published for one building's attack period
(`asianalliance_railtower`): 160, 172, 180 and 220. Three are wrong, and two of the wrong
ones were mine — both produced by reading the trait and reasoning about what it "should"
do. So this file does not reason. It transcribes the engine's state machine and runs it.

THE MODEL, file by file:

  AttackTesla.cs      `ChargeAttack` queues `Wait(InitialChargeDelay)` then `ChargeFire`.
                      `ITick` refills `charges` when `timeToRecharge` expires, and
                      `INotifyAttack.Attacking` resets that to the trait's `ReloadDelay` on
                      every real shot — so the trait reload runs from the LAST shot.
  AttackBase.cs       `CanAttack` calls `HasAnyValidWeapons(target, reloadingIsInvalid: true)`,
                      whose loop sets `reloadingStateIsValid = !reloadingIsInvalid ||
                      !armament.IsReloading`. **A reloading armament therefore makes
                      `CanAttack` FALSE.**
  Armament.cs         `IsReloading` is `FireDelay > 0`; `Tick` decrements `FireDelay`.
  Activity.cs:126-133 "avoid a single tick delay if the childactivity was just queued" — a
                      freshly queued child ticks in the same tick, which is what makes
                      `Wait(N)` block its parent for exactly N ticks rather than N+1.

⭐ THE LOAD-BEARING LINE, and the one I got wrong twice:

    ChargeFire.Tick:  if (IsCanceling || !attack.CanAttack(self, target)) return true;

`ChargeFire` does **not** spin while the weapon reloads — it EXITS. `ChargeAttack` carries
the identical guard, so it exits too and the whole attack activity ends. The actor then
reacquires and comes back in through `ChargeAttack`, which pays `InitialChargeDelay` AGAIN.

That gives the mode detection the maintainer asked for, with no per-actor knowledge and no
trait-name special case — just `ChargeDelay` against the weapon's reload:

    weapon reload <= ChargeDelay   the volley is never interrupted.  CHARGE-ONCE:
                                   gap = ChargeDelay
    weapon reload >  ChargeDelay   ChargeFire exits after every shot. CHARGE-PER-SHOT:
                                   gap = weapon reload + InitialChargeDelay

    cycle = (MaxCharges - 1) * gap + trait ReloadDelay + InitialChargeDelay

With immediate reacquisition this reproduces all three maintainer rulings — 131, 95 and **220** — including
*"for the asian alliance railgun tower it needs to charge for every shot unlike the tesla
coil! So at 5 shots the initial charge delay is used 5 times!"*, which is precisely what
the guard produces.

⚠ THE UNRESOLVED INPUT: how fast the actor reacquires after the activity ends. This model
defaults to immediate reacquisition (`--reacquire 0`), which reproduces the ruled 220. The
real actor inherits AutoTarget with a randomized 3–7 tick scan interval, so this script does
not prove a fixed runtime period. Any reacquisition delay lengthens the Rail Tower while
leaving the two coils untouched, since they never exit in the first place.

⚠ A SECOND CAVEAT THE TRACE DOES NOT SETTLE: `DoAttack` calls `CheckFire` on EVERY armament
and `Attacking` decrements `charges` once per firing armament. The RA1 coil declares 3
armaments and the RA2 coil 6, all condition-gated. This assumes exactly one is enabled —
which is what reproduces 131 and 95.

⛔ NOTHING HERE IS APPLIED. `armament_roles` still withholds the charge term; the
implemented Tesla Coil period is still 106. This is evidence for a decision, not a change.

Usage:
    python tools/balance/sim_attack_tesla.py            # the three charged actors
    python tools/balance/sim_attack_tesla.py --check    # exit 1 on any disagreement
"""
from __future__ import annotations

import argparse
import sys

# The three `AttackTesla` actors in the tree, every field resolved from the ruleset.
# `ChargeDelay` is written by NONE of them — 3 is AttackTeslaInfo's own default, so the
# comparison that decides the mode is invisible in the yaml.
#   name: (MaxCharges, trait ReloadDelay, InitialChargeDelay, ChargeDelay, weapon ReloadDelay)
ACTORS = {
    "ra1_soviets_teslacoil":   (3, 100, 25, 3, 3),
    "ra2_soviets_teslacoil":   (1, 75, 20, 3, 3),
    "asianalliance_railtower": (5, 120, 12, 3, 10),
}

# Every period ever claimed for these actors, kept beside the simulation that adjudicates
# them so the verdict cannot drift away from the claim in some other document.
RULINGS = {
    "ra1_soviets_teslacoil": 131,
    "ra2_soviets_teslacoil": 95,
    "asianalliance_railtower": 220,
}
CLAIMED = {
    "ra1_soviets_teslacoil":   {"maintainer 2026-09-14": 131},
    "ra2_soviets_teslacoil":   {"maintainer 2026-09-14": 95},
    "asianalliance_railtower": {"maintainer ruling / immediate model": 220, "#385": 160,
                                "my spin trace (withdrawn)": 172,
                                "my quantised trace (withdrawn)": 180},
}


def charge_mode(charge_delay: int, weapon_reload: int) -> str:
    """CHARGE-ONCE or CHARGE-PER-SHOT, decided by the engine's own reloading guard."""
    return "once" if weapon_reload <= charge_delay else "per-shot"


def formula(max_charges, trait_reload, initial_charge, charge_delay, weapon_reload) -> int:
    """The immediate-reacquisition closed form."""
    gap = (charge_delay if charge_mode(charge_delay, weapon_reload) == "once"
           else weapon_reload + initial_charge)
    return (max_charges - 1) * gap + trait_reload + initial_charge


def simulate(max_charges, trait_reload, initial_charge, charge_delay, weapon_reload,
             reacquire: int = 0, ticks: int = 6000) -> list[int]:
    """Tick indices of every shot, running the trait's state machine as transcribed above."""
    charges, time_to_recharge, fire_delay = max_charges, 0, 0
    state, wait, shots = "ChargeAttack", 0, []

    for t in range(ticks):
        can_attack = fire_delay == 0          # HasAnyValidWeapons(reloadingIsInvalid: true)

        # An activity that finishes hands control to its parent in the SAME tick
        # (Activity.cs:117, `lastRun = TickChild(self) && (finishing || Tick(self))`), so
        # unwinding out of ChargeFire into ChargeAttack costs nothing. Hence a loop, not a
        # state that burns a tick - modelling the unwind as a tick was worth 4 ticks of
        # error on the Rail Tower and is exactly the kind of slip this file exists to catch.
        while wait == 0:
            if state == "ChargeFire":
                if not can_attack or charges == 0:
                    # ⛔ THE LINE THIS FILE EXISTS FOR. ChargeFire RETURNS TRUE here; it
                    # does not keep ticking through the weapon's reload.
                    state = "ChargeAttack"
                    continue
                fire_delay = weapon_reload
                charges -= 1
                time_to_recharge = trait_reload            # INotifyAttack.Attacking
                shots.append(t)
                wait = charge_delay                        # QueueChild(Wait(ChargeDelay))
                break
            # ChargeAttack, carrying the identical guard.
            if not can_attack:
                wait = reacquire                           # the whole activity ends
                break
            if charges == 0:
                break                                      # returns false: spins in place
            wait, state = initial_charge, "ChargeFire"
            break

        if wait > 0:
            wait -= 1
        if fire_delay > 0:
            fire_delay -= 1
        time_to_recharge -= 1
        if time_to_recharge <= 0:
            charges = max_charges

    return shots


def analyse(name, spec, reacquire: int = 0):
    """(period, closed form, inter-shot gaps, shots per volley) for one actor."""
    shots = simulate(*spec, reacquire=reacquire)
    closed = formula(*spec) if reacquire == 0 else None
    if len(shots) < 2:
        return None, closed, [], len(shots)
    gaps = [b - a for a, b in zip(shots, shots[1:])]
    volley_gap = max(gaps)
    firsts = [shots[0]] + [b for a, b in zip(shots, shots[1:]) if b - a == volley_gap]
    periods = sorted({b - a for a, b in zip(firsts, firsts[1:])})
    inner = sorted({g for g in gaps if g != volley_gap})
    per_volley = spec[0] if len(periods) == 1 else None
    return (periods[0] if len(periods) == 1 else periods), closed, inner, per_volley


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__.split("\n", 1)[0])
    ap.add_argument("--check", action="store_true",
                    help="exit 1 if simulation, closed form and ruling disagree")
    ap.add_argument("--reacquire", type=int, default=0,
                    help="ticks to reacquire after the attack activity ends (default 0)")
    args = ap.parse_args()

    bad = 0
    print(f"{'actor':<26} {'mode':<9} {'gap':>5} {'SIMULATED':>10} {'formula':>8}   claims")
    print("-" * 104)
    for name, spec in ACTORS.items():
        period, closed, inner, _ = analyse(name, spec, args.reacquire)
        mode = charge_mode(spec[3], spec[4])
        agree = closed is None or period == closed
        bad += not agree
        gap = inner[0] if len(inner) == 1 else (inner or "-")
        claims = "  ".join(f"{who} {val}" + ("  <== MATCHES" if val == period else "")
                           for who, val in CLAIMED[name].items())
        print(f"{name:<26} {mode:<9} {str(gap):>5} {str(period):>10} {str(closed or '-'):>8}"
              f"{'' if agree else '  DISAGREE'}   {claims}")
    print("-" * 104)
    print("WITHHELD: the charge term is NOT applied to any price or reference. Evidence only.")

    if args.check:
        for name, spec in ACTORS.items():
            period, _, _, _ = analyse(name, spec, args.reacquire)
            ruled = RULINGS[name]
            if args.reacquire == 0 and period != ruled:
                print(f"FAIL: {name} simulates {period}, the ruling says {ruled}",
                      file=sys.stderr)
                bad += 1
        if bad:
            return 1
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
