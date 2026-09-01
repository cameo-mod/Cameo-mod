#!/usr/bin/env python3
"""bot_insurance_model.py — executable reference model of `DynamicBotInsurance`.

    python tools/balance/bot_insurance_model.py            # behaviour table, all difficulties
    python tools/balance/bot_insurance_model.py --trace medium

⛔ WHY THIS EXISTS. `DynamicBotInsurance.cs` cannot be compiled, run or boot-gated from a cloud
container (no `engine/`, no dotnet — CLAUDE.md rule 7). The C# is therefore unverified *as code*.
But the part that is actually easy to get wrong is not the C# — it is the STATE MACHINE: a
threshold that falls, freezes, and re-arms, with a delay derived from a rolling average, has
several ways to oscillate, stick, or never fire. That much can be verified here, exactly, and it
is what `tools/tests/test_bot_insurance_model.py` asserts.

⚠ THIS FILE AND THE C# MUST BE CHANGED TOGETHER. It is a reference model, not documentation:
if they drift, the tests are testing a fiction. Keep `tick()` a line-for-line mirror of
`DynamicBotInsurance.ITick.Tick`.

THE MACHINE, in three phases:

  ARMING    the bar TRACKS the rolling average, moving toward clamp(average, MinThreshold,
            MaxThreshold) by at most `rate_per_tick` a tick -- up when the average is above it,
            down when below, at the same rate either way. When the bar reaches the owner's cash
            (`cash < threshold`) the owner qualifies -> freeze, compute the delay, go to DELAYING.

            ⛔ IT IS A SLEW-LIMITED TRACKER, NOT A ONE-WAY RAMP, AND NOT A FALLING BAR. Two
            earlier designs were tried and rejected against this model:
              * FALLING from MaxThreshold (the original spec) is DEAD MECHANICS. The trigger
                `cash < threshold` is easiest to satisfy when the bar is HIGHEST, so a falling bar
                fires on tick one for anyone under MaxThreshold and only ever makes triggering
                harder afterwards. Measured, every difficulty behaved identically and the whole
                ordering came from the delay divisor -- the rate did nothing at all.
              * RISING ONCE from zero works, but the bar never comes back down, so it stops
                describing the economy the moment the economy changes.
            Tracking in both directions keeps the rate meaningful (`cameogod` converges ten times
            faster than `easiest`) AND keeps the bar an honest smoothed picture of how the owner
            has actually been doing.
  DELAYING  wait `average / delay_divisor` ticks, clamped. Recovering above the frozen bar cancels
            outright and re-arms from the top.
  PAYING    grant a payout SCALED BY DEPTH until cash reaches MaxThreshold, then re-arm.

⭐ THE PAYOUT IS PROPORTIONAL TO DEPTH, WHICH IS WHERE THE OLD LADDER'S GRANULARITY CAME FROM.
The ten-rung ladder was not merely ten difficulties — the rungs STACKED, so a `cameogod` bot drew
1 credit/tick just under 10000 and 10 credits/tick near zero. A flat "on/off" payout throws that
away and makes the hardest bot draw its maximum the whole time it is insured. So:

    depth_permille = clamp(1000 * (MaxThreshold - cash) / MaxThreshold, 0, 1000)
    accumulator   += cash_per_tick * depth_permille
    grant          = accumulator // 1000        # and keep the remainder

Measured against the old ladder for `cameogod`: cash 9000 -> 1/tick (old: 1 rung), 5000 -> 5/tick
(old: 5 rungs), 0 -> 10/tick (old: 10 rungs). It REPRODUCES the ladder and then keeps going
between the rungs, which is the "more granular than before" the maintainer asked for.

⚠ Integer milli-credit accumulation, not floating point: the payout must be deterministic across
machines or it desyncs. The remainder carries, so a 0.5 credit/tick rate really pays 1 credit every
other tick rather than rounding to nothing — which is what a naive integer divide would do to every
low difficulty.

⚠ THE EXIT IS `MaxThreshold`, NOT THE FROZEN BAR. Leaving at the bar it entered on re-triggers
within a few ticks, forever — a payout oscillator. `test_paying_does_not_oscillate` pins that.

Everything scales by the owner's INDEX in the difficulty list, so a new difficulty is one more
name and no new traits or conditions — which is the whole point of replacing the ten-rung ladder.
"""
from __future__ import annotations

import argparse
import collections
import sys

DIFFICULTIES = ["easiest", "veryeasy", "easy", "medium", "hard",
                "veryhard", "brutal", "challenger", "unbeatable", "cameogod"]

# Defaults mirroring DynamicBotInsuranceInfo. ⚠ AVERAGE_WINDOW is 1500 = ONE MINUTE at the mod's
# default 40ms timestep (mod.yaml GameSpeeds/default), not 3000.
#
# MIN_THRESHOLD is 1000 rather than 0, and with a STRICT `<` trigger that is load-bearing, not
# cosmetic: a persistently broke owner drives its own average to 0, the bar tracks it to 0, and
# `cash < 0` is unsatisfiable -- the mechanic would switch itself off in exactly the situation it
# exists for. The floor is the absolute poverty line: below it you are insured whatever your
# history says. See test_a_zero_floor_strands_a_bankrupt_bot.
#
# MAX_THRESHOLD is both the bar's ceiling AND the cash level at which a payout stops. Keep them the
# same number: a ceiling above the payout exit lets an owner trigger and then immediately stop
# paying, which is churn with no benefit.
AVERAGE_WINDOW = 1500
MAX_THRESHOLD = 10000
MIN_THRESHOLD = 1000
MIN_RATE, MAX_RATE = 1, 10
MIN_DIVISOR, MAX_DIVISOR = 10, 100
MIN_DELAY, MAX_DELAY = 25, 1500
MIN_CASH, MAX_CASH = 1, 10
MIN_PURIFIER, MAX_PURIFIER = 5, 50


def by_rank(minimum: int, maximum: int, rank: int, count: int) -> int:
    """Integer interpolation across the difficulty list — the C# `ByRank`."""
    steps = count - 1
    if steps <= 0:
        return minimum
    return minimum + ((maximum - minimum) * rank) // steps


class Insurance:
    """One player's insurance state machine. `tick()` mirrors the C# `Tick` exactly."""

    def __init__(self, difficulty: str, difficulties: list[str] | None = None,
                 average_window: int = AVERAGE_WINDOW, max_threshold: int = MAX_THRESHOLD,
                 min_threshold: int = MIN_THRESHOLD):
        self.difficulties = difficulties or DIFFICULTIES
        self.rank = self.difficulties.index(difficulty) if difficulty in self.difficulties else -1
        self.max_threshold = max_threshold
        self.min_threshold = min_threshold

        n = len(self.difficulties)
        self.rate_per_tick = max(1, by_rank(MIN_RATE, MAX_RATE, max(self.rank, 0), n))
        self.delay_divisor = max(1, by_rank(MIN_DIVISOR, MAX_DIVISOR, max(self.rank, 0), n))
        self.cash_per_tick = by_rank(MIN_CASH, MAX_CASH, max(self.rank, 0), n)
        self.purifier_modifier = by_rank(MIN_PURIFIER, MAX_PURIFIER, max(self.rank, 0), n)

        self.history: collections.deque[int] = collections.deque(maxlen=max(1, average_window))
        self.threshold = 0
        self.delay_remaining = 0
        self.banked = 0
        self.phase = "arming"
        self.paid = 0
        self.accumulator = 0            # milli-credits carried between ticks

    def depth_permille(self, cash: int) -> int:
        """How deep below the cap the owner is, 0 (at the cap) to 1000 (broke).

        Measured against MaxThreshold rather than against the dynamic bar on purpose: this is the
        ABSOLUTE poverty scale the old ladder used, so the two agree rung for rung.
        """
        return max(0, min(1000, 1000 * (self.max_threshold - cash) // self.max_threshold))

    @property
    def average(self) -> int:
        return sum(self.history) // len(self.history) if self.history else 0

    def tick(self, cash: int) -> int:
        """Advance one tick against the owner's current cash. Returns credits granted this tick."""
        if self.rank < 0:
            return 0

        self.history.append(cash)
        granted = 0

        if self.phase == "arming":
            target = min(max(self.average, self.min_threshold), self.max_threshold)
            step = max(-self.rate_per_tick, min(self.rate_per_tick, target - self.threshold))
            self.threshold += step
            # ⛔ STRICTLY BELOW. With `<=` the bar converges to the average, the average converges
            # to a stable cash pile, and every bot under the cap eventually insures itself -- the
            # mechanic stops being an emergency measure and becomes baseline income. Strict `<`
            # means "poorer than your own recent normal", which is the actual signal wanted.
            if cash < self.threshold:
                self.delay_remaining = min(max(self.average // self.delay_divisor, MIN_DELAY),
                                           MAX_DELAY)
                self.phase = "delaying"

        elif self.phase == "delaying":
            if cash > self.threshold:
                # Unfreeze only. Slamming the bar back to zero would throw away the tracker and
                # make the next arming cycle start from a lie about the economy.
                self.phase = "arming"
            else:
                self.delay_remaining -= 1
                if self.delay_remaining <= 0:
                    self.phase = "paying"

        elif self.phase == "paying":
            if cash >= self.max_threshold:
                self.banked = 0
                self.accumulator = 0
                self.phase = "arming"
            else:
                depth = self.depth_permille(cash)
                self.accumulator += self.cash_per_tick * depth
                granted += self.accumulator // 1000
                self.accumulator %= 1000

                if self.banked >= 250:
                    granted += self.banked * self.purifier_modifier * depth // 100000
                    self.banked = 0

        self.paid += granted
        return granted


def simulate(difficulty: str, cash: int, ticks: int, spend_per_tick: int = 0) -> dict:
    """Run a bot that holds `cash` and optionally bleeds `spend_per_tick`, and report."""
    ins = Insurance(difficulty)
    first_payout = None
    for t in range(ticks):
        granted = ins.tick(max(0, cash))
        cash += granted - spend_per_tick
        if granted and first_payout is None:
            first_payout = t
    return {
        "difficulty": difficulty,
        "rate": ins.rate_per_tick,
        "divisor": ins.delay_divisor,
        "cash_per_tick": ins.cash_per_tick,
        "purifier": ins.purifier_modifier,
        "first_payout_tick": first_payout,
        "total_paid": ins.paid,
        "end_cash": cash,
    }


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__.split("\n")[0])
    ap.add_argument("--cash", type=int, default=1500, help="starting (and held) cash")
    ap.add_argument("--ticks", type=int, default=3000, help="ticks to simulate")
    ap.add_argument("--trace", metavar="DIFFICULTY", help="print a phase trace for one difficulty")
    args = ap.parse_args()

    if args.trace:
        ins = Insurance(args.trace)
        print(f"# trace — {args.trace} (rate {ins.rate_per_tick}/tick, divisor {ins.delay_divisor}, "
              f"{ins.cash_per_tick} credits/tick, purifier {ins.purifier_modifier}%)\n")
        print("| tick | phase | threshold | avg | cash | granted |")
        print("|--:|---|--:|--:|--:|--:|")
        cash, last = args.cash, None
        for t in range(args.ticks):
            g = ins.tick(max(0, cash))
            cash += g
            if ins.phase != last or t % 500 == 0:
                print(f"| {t} | {ins.phase} | {ins.threshold} | {ins.average} | {cash} | {g} |")
                last = ins.phase
        return 0

    print(f"# DynamicBotInsurance — behaviour at cash {args.cash}, {args.ticks} ticks "
          f"({args.ticks / 25:.0f}s at the default 40ms timestep)\n")
    print("| difficulty | rate/tick | divisor | credits/tick | purifier | first payout | total paid |")
    print("|---|--:|--:|--:|--:|--:|--:|")
    for d in DIFFICULTIES:
        r = simulate(d, args.cash, args.ticks)
        first = r["first_payout_tick"]
        print(f"| {d} | {r['rate']} | {r['divisor']} | {r['cash_per_tick']} | {r['purifier']}% | "
              f"{'never' if first is None else first} | {r['total_paid']} |")
    return 0


if __name__ == "__main__":
    sys.exit(main())
