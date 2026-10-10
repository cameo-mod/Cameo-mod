# M13 runtime wiring checkpoints

Base: accepted claim-provider tip `59e80a95a6142c001360fb587682cfdab515dc4a`.
The default-off increment is `BV_m13_liveness`, classified as one restraint; the
applier skips the classic squad-manager instance. No YAML enables it by default.

The first wiring checkpoint adds explicit BotLimits values for all ten tiers:

| Tier | No-launch ticks | Response expiry | Cooldown | Capacity |
|---|---:|---:|---:|---:|
| easiest | 9500 | 1900 | 950 | 1 |
| veryeasy | 9000 | 1800 | 900 | 1 |
| easy | 8500 | 1700 | 850 | 1 |
| medium | 8000 | 1600 | 800 | 1 |
| hard | 7500 | 1500 | 750 | 1 |
| veryhard | 7000 | 1400 | 700 | 1 |
| brutal | 6500 | 1300 | 650 | 1 |
| challenger | 6000 | 1200 | 600 | 1 |
| unbeatable | 5500 | 1100 | 550 | 1 |
| cameogod (`BotLimits@god`) | 5000 | 1000 | 500 | 1 |

The existing CreateAttackForce scale-target branch uses the configured absolute
MaxIdleUnits only when M13 is armed; switch-off retains the old integer formula.
Tests pin the N-1/N boundary under several scale targets and the legacy formula.
This closes only the scale-dependent count-valve defect. It does not yet bypass
composition/mission/combat holds or prove a dispatch at that count.

Remaining: bot-wide response membership and release, continuous liveness service,
authoritative observed-wave evidence, save/restore, separate frozen combat stream
and diagnostic consumer. Expansion assist without an episode stays unsupported.
No full M13, parity, cost, adoption or paired 3v3 claim follows from this checkpoint.
