"""spsa_calibration — reproducible gain/EMA fixture for TIER4_SPSA_SPEC section 4.7.

Simulates the implemented --spsa update loop on a synthetic quadratic landscape and reports the final
log-space distance to the target for a grid of (gain preset) x (gradient EMA on/off) x (paired-diff noise).
The implementation's own functions are used for the perturbation, gains, arm values, gating and update math -
this is a fixture of the tool's behaviour, not a parallel reimplementation. Run:

    python tools/ai/spsa_calibration.py

Deterministic: seeded stdlib Random, no wall clock, no repo state beyond tune_build_order.py.
"""
from __future__ import annotations

import math
import random
import statistics
import sys
import pathlib

sys.path.insert(0, str(pathlib.Path(__file__).resolve().parent))
import tune_build_order as tbo  # noqa: E402

P, F = "rush", "ra1_allies"
THETA_STAR = {"tempo": 1100, "greed": 1150, "production": 1050, "tech": 1180,
              "defence": 920, "power_margin": 880, "support": 1080}
PRESETS = {"a0.05_A25": (0.05, 25), "a0.10_A10": (0.10, 10)}


def j_log(vec: dict[str, int]) -> float:
    """Objective on the LOG multiplier vector (what the optimizer sees); higher is better."""
    return -sum((math.log(vec[k] / 1000.0) - math.log(THETA_STAR[k] / 1000.0)) ** 2 for k in tbo.TUNABLE)


def dist(learned: dict) -> float:
    f = learned["knobs"].get((P, F), {})
    return math.sqrt(sum((math.log(f.get(k, 1000) / 1000.0) - math.log(THETA_STAR[k] / 1000.0)) ** 2
                         for k in tbo.TUNABLE))


def simulate(a: float, stab: int, g_ema: float, sigma: float, steps: int = 60,
             n_per_arm: int = tbo.MIN_MATCHES, seed: int = 12345) -> float:
    """One damped SPSA walk. Returns final log-space distance to the target.

    Mirrors write_spsa_update/apply_spsa_step: sha256 delta, clamped arm values, paired diff with per-match
    noise sigma/sqrt(2) per arm (=> sigma on the diff), two-sided |z| >= 1.96 -> full a_k step else 0.25,
    signed eff_i, log-space update clamped to the bounds. g_ema=0 disables smoothing; g_ema>0 keeps an EMA
    over successive gradient estimates (G_EMA weight on the fresh estimate) like the reviewed GBar variant.
    """
    rng = random.Random(seed)
    learned = tbo.empty_learned()
    g_bar = dict.fromkeys(tbo.TUNABLE, 0.0)
    c_gain = tbo.SPSA_GAIN_C
    for k in range(steps):
        values = tbo.spsa_arm_values(learned, P, F, k)
        diffs = [j_log({kk: v["plus"] for kk, v in values.items()}) + rng.gauss(0, sigma / math.sqrt(2))
                 - j_log({kk: v["minus"] for kk, v in values.items()}) - rng.gauss(0, sigma / math.sqrt(2))
                 for _ in range(n_per_arm)]
        d_bar = sum(diffs) / len(diffs)
        sd = statistics.stdev(diffs) if len(diffs) > 1 else 0.0
        z = math.inf if sd == 0 and d_bar > 0 else (-math.inf if sd == 0 and d_bar < 0
                                                    else d_bar / (sd / math.sqrt(len(diffs))) if sd else 0.0)
        scale = tbo.SPSA_SCALE_FULL if abs(z) >= tbo.Z_CRIT else tbo.SPSA_SCALE_WEAK
        a_k = a / (k + 1 + stab) ** 0.602
        assert abs(c_gain / (k + 1) ** 0.101 - tbo.spsa_perturb(k)) < 1e-12
        state = learned["knobs"].setdefault((P, F), {})
        for knob in tbo.TUNABLE:
            eff = (math.log(values[knob]["plus"] / 1000.0) - math.log(values[knob]["minus"] / 1000.0)) / 2.0
            if abs(eff) < tbo.SPSA_MASK_LOG:
                continue
            g = d_bar / (2.0 * eff)
            g_bar[knob] = g_ema * g + (1 - g_ema) * g_bar[knob] if g_ema else g
            old = tbo.current_multiplier(learned, P, F, knob)
            state[knob] = int(tbo.clamp(round(1000 * math.exp(math.log(old / 1000.0) + scale * a_k * g_bar[knob])),
                                        tbo.LEARN_MIN, tbo.LEARN_MAX))
    return dist(learned)


def main() -> int:
    print(f"target dist at start (all knobs 1000): {dist(tbo.empty_learned()):.3f}")
    print(f"{'preset':<10} {'ema':>4} | " + "  ".join(f"sigma={s:<5}" for s in (0.0, 0.16, 0.30, 0.50)))
    for preset, (a, stab) in PRESETS.items():
        for g_ema in (0.0, 0.4):
            row = [f"{simulate(a, stab, g_ema, s):.4f}" for s in (0.0, 0.16, 0.30, 0.50)]
            print(f"{preset:<10} {g_ema:<4} | " + "  ".join(row))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
