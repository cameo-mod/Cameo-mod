# Insurance payout telemetry

Task TRACK-INSURANCE; base 5ea8c84f5. Source ownership: new Cameo insurance ledger/capture,
DynamicBotInsurance's two post-credit observations, a CashTrickler subclass and only its
secondaryinsurance YAML mount, world capture mount, ab_summary reader and focused tests.
No engine, queue, policy, order, RNG or synchronized state changes. No launches.

Use a separate `cameo-insurance` schema 1 stream named
`Logs/cameo-ai-insurance-<SHA256 of game UID>.jsonl`. CreateNew preserves earlier evidence;
the existing frozen economy streams and their activation remain unchanged. Host regular
non-replay/non-save worlds only. Identity is match UID plus world-player ordinal slot,
bot difficulty code and faction, never player/client names. Start at tick 0, one row for each
positive requested payout (including zero accepted at a cap), and one terminal summary per bot.
Normal insurance, purifier bonus and legacy secondaryinsurance use distinct fixed reasons.

Measure synchronous Cash+Resources before/after each unchanged grant. Record requested,
credited and cumulative credited values separately. The denominator is the engine Earned
ledger delta since tick 0, excluding starting cash/refunds; it is explicitly an accounting
statistic, not harvesting or gross-spending proof. Saturation, decreasing Earned, missing
start/end, output failure or untracked bot makes the share UNKNOWN. Never manufacture zero
from absent telemetry. Bounds: 64 bots, 128 MiB/file, 64 KiB/line, 200k rows; ledger continues
after I/O failure but incomplete evidence cannot produce an accepted total.

Verification: payout/cap/refund/identity and overflow ledger tests, inherited legacy settings,
default mounts, malformed/incomplete/duplicate stream guards, exact summary round trip.
Build and focused/full suites; independent exact-SHA review. Runtime output/CPU/disk cost
and gameplay parity remain unmeasured until a separately authorized capture.

Implemented command: `python tools/ai/ab_summary.py <support-directory>` discovers these
streams and prints per-match slot/difficulty/faction totals by reason, income and share.
`--timestep` applies to insurance as well as matches. Duplicate match/slot copies are UNKNOWN,
not pooled twice; truncated, malformed, missing, saturated or incomplete evidence is UNKNOWN.
There is no stop-gate or decision consumer. Output limits can be reached in a very long/high
payout match: preserve the partial file, stop writing, and do not claim a covered total.
