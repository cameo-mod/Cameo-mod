#!/usr/bin/env python
"""
Offline simulator for the Cameo AdaptiveGameSpeed controller (Stage C, Prong B).

Why this exists: the controller's whole dynamic is the game loop's feedback
    period = max(pacedTimestep, compute)
i.e. when compute fits the paced budget the loop sleeps to the paced rate; when it
doesn't, the loop runs at compute. The controller reads that period and adjusts the
pace. That relationship is exact and trivial to reproduce, so we can tune the
controller (floor / margin / smoothing / recovery / hold) here in seconds instead of
launching a 60s real-time skirmish each time. Final params still get ONE in-game check.

The controller block below is a line-for-line port of
OpenRA.Mods.Cameo/Traits/AdaptiveGameSpeed.cs — keep them in sync.
"""

import math
import random


# --- Controller params (mirror AdaptiveGameSpeedInfo defaults) ---
class Params:
    budget = 40                 # World.Timestep (ms) at Normal speed
    min_speed_percent = 25      # -> max_scale = 100/25 = 4.0
    overload_margin = 0.15
    smoothing = 0.30            # faster EMA: tracks the paced timestep down during recovery
    recovery_step = 0.01
    recovery_rate = 0.04
    overload_hold_ticks = 25


def run_controller(compute, p):
    """Feed a per-tick compute[ms] profile through the controller + loop feedback.
    Returns per-tick (scale, period, speed_pct, saturated, at_cap)."""
    max_scale = 100.0 / max(17, min(100, p.min_speed_percent))
    scale = 1.0
    ema = 0.0
    hold = 0
    out = []
    for c in compute:
        paced = p.budget * scale
        period = max(paced, c)                      # the loop's behavior
        ema = period if ema <= 0 else ema + p.smoothing * (period - ema)
        saturated = ema > paced * (1.0 + p.overload_margin)
        if saturated:
            target = ema / p.budget * (1.0 + p.overload_margin)
            if target > scale:
                scale = target
            hold = p.overload_hold_ticks
        elif hold > 0:
            hold -= 1
        else:
            scale -= max(p.recovery_step, (scale - 1.0) * p.recovery_rate)
        scale = max(1.0, min(max_scale, scale))
        if scale < 1.01:
            scale = 1.0
        at_cap = scale >= max_scale - 0.001
        out.append((scale, period, 100.0 / scale, saturated, at_cap))
    return out, max_scale


# --- Compute profiles (per-tick ms) ---
def triangle(peak, ramp, cycles):
    seq = []
    for _ in range(cycles):
        for i in range(ramp):
            seq.append(peak * i / ramp)
        for i in range(ramp):
            seq.append(peak * (ramp - i) / ramp)
    return seq


def battle(base, peak, rise, hold, fall, tail, noise=0.0, seed=1):
    rng = random.Random(seed)
    seq = []
    for i in range(rise):
        seq.append(base + (peak - base) * i / rise)
    for _ in range(hold):
        seq.append(peak + (rng.random() * 2 - 1) * noise)
    for i in range(fall):
        seq.append(peak - (peak - base) * i / fall)
    for _ in range(tail):
        seq.append(base)
    return seq


def constant(level, ticks, noise=0.0, seed=1):
    rng = random.Random(seed)
    return [level + (rng.random() * 2 - 1) * noise for _ in range(ticks)]


def spiky(base, spike, ticks, prob=0.15, seed=1):
    rng = random.Random(seed)
    return [spike if rng.random() < prob else base for _ in range(ticks)]


# --- Reporting ---
BARS = " .:-=+*#%@"


def spark(vals, lo, hi):
    s = []
    for v in vals:
        t = 0 if hi == lo else (v - lo) / (hi - lo)
        s.append(BARS[max(0, min(len(BARS) - 1, int(t * (len(BARS) - 1))))])
    return "".join(s)


def downsample(vals, width=80):
    if len(vals) <= width:
        return vals
    step = len(vals) / width
    return [vals[int(i * step)] for i in range(width)]


def report(name, compute, p):
    out, max_scale = run_controller(compute, p)
    speeds = [o[2] for o in out]
    scales = [o[0] for o in out]
    at_cap = [o[4] for o in out]

    avg_speed = sum(speeds) / len(speeds)
    min_speed = min(speeds)
    pct_at_cap = 100.0 * sum(1 for a in at_cap if a) / len(at_cap)

    # recovery: after the last tick whose compute needs slowdown, how many ticks to
    # get back to >=99% speed?
    need = [i for i, c in enumerate(compute) if c > p.budget]
    recover = None
    if need:
        last = need[-1]
        for i in range(last, len(speeds)):
            if speeds[i] >= 99.0:
                recover = i - last
                break

    print(f"\n=== {name}  (cap={max_scale:.2f}x = {100/max_scale:.0f}% min speed) ===")
    print(f"compute ms : {spark(downsample(compute), 0, max(compute))}  (0..{max(compute):.0f})")
    print(f"speed %    : {spark(downsample(speeds), 25, 100)}  (25..100)")
    print(f"  avg speed {avg_speed:5.1f}%   min speed {min_speed:5.1f}%   time at cap {pct_at_cap:4.1f}%", end="")
    if recover is not None:
        print(f"   recover-to-full {recover} ticks (~{recover*0.16:.1f}s @ ~6tps)")
    elif need:
        print("   recover-to-full: NEVER (within window)")
    else:
        print()


def metrics(compute, p):
    out, max_scale = run_controller(compute, p)
    speeds = [o[2] for o in out]
    avg = sum(speeds) / len(speeds)
    # settle = avg speed over the last 30% (steady-state behaviour)
    tail = speeds[int(len(speeds) * 0.7):]
    settle = sum(tail) / len(tail)
    need = [i for i, c in enumerate(compute) if c > p.budget]
    recover = None
    if need:
        last = need[-1]
        for i in range(last, len(speeds)):
            if speeds[i] >= 99.0:
                recover = i - last
                break
    return avg, settle, recover


def sweep():
    print("\n###### PARAMETER SWEEP ######")
    profiles = {
        "battle->clear (want: settle~100% after clear)":
            battle(8, 100, 120, 400, 120, 400, noise=8),
        "sustained 100ms (want: settle~35%, steady)":
            constant(100, 600, noise=6),
        "sustained 60ms (want: settle~58%)":
            constant(60, 600, noise=6),
    }
    candidates = [
        dict(recovery_rate=0.0, recovery_step=0.02, smoothing=0.15),
        dict(recovery_rate=0.0, recovery_step=0.05, smoothing=0.15),
        dict(recovery_rate=0.0, recovery_step=0.05, smoothing=0.30),
        dict(recovery_rate=0.0, recovery_step=0.10, smoothing=0.30),
        dict(recovery_rate=0.04, recovery_step=0.01, smoothing=0.30),
    ]
    for name, prof in profiles.items():
        print(f"\n--- {name} ---")
        print(f"{'rate':>5} {'step':>5} {'smooth':>6} | {'avg%':>6} {'settle%':>8} {'recover(ticks)':>15}")
        for c in candidates:
            p = Params()
            p.recovery_rate = c["recovery_rate"]
            p.recovery_step = c["recovery_step"]
            p.smoothing = c["smoothing"]
            avg, settle, rec = metrics(prof, p)
            recs = "n/a" if rec is None and not any(x > p.budget for x in prof) else ("NEVER" if rec is None else str(rec))
            print(f"{c['recovery_rate']:>5} {c['recovery_step']:>5} {c['smoothing']:>6} | "
                  f"{avg:>6.1f} {settle:>8.1f} {recs:>15}")


def main():
    p = Params()
    print("AdaptiveGameSpeed controller simulation")
    print(f"params: floor={p.min_speed_percent}% margin={p.overload_margin} "
          f"smooth={p.smoothing} recStep={p.recovery_step} recRate={p.recovery_rate} "
          f"hold={p.overload_hold_ticks}")

    report("triangle 0->120->0 (matches current in-game test)", triangle(120, 400, 2), p)
    report("battle: ramp to 100ms, sustain, then CLEAR to ~0 (recovery test)",
           battle(8, 100, 120, 400, 120, 400, noise=8), p)
    report("sustained 100ms (sub-cap; want steady ~35%)", constant(100, 600, noise=6), p)
    report("sustained 60ms (want steady ~58%)", constant(60, 600, noise=6), p)
    report("spiky: 55ms base, 135ms spikes (spike-reaction)", spiky(55, 135, 600), p)
    report("sustained 200ms (over-cap; want pinned 25%)", constant(200, 400, noise=6), p)


if __name__ == "__main__":
    main()
