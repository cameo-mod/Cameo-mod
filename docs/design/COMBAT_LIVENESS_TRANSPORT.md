# Combat liveness transport preflight (implementation checkpoint)

The M13 design names `Logs/cameo-ai-combat-liveness.jsonl`,
`cameo-combat-liveness` schema version 1 and an independent diagnostic consumer.
This checkpoint implements only the bounded transport reader, not the symptom
evaluator or a runtime emitter. The approved economy logger remains untouched.

Run `python tools/ai/combat_liveness.py --logs PATH_TO_LOGS --game-uid UID --player KEY`.
The CLI always exits **21/UNKNOWN**. `coverage: TRANSPORT_COMPLETE` means only
that the selected participant's envelope passed validation; it cannot certify
eligibility, offensive activity, a launch, response release or bot health.
`dispatch_causality` is always UNKNOWN. Findings stay empty until independently
reviewed semantic readers exist. No economy BLOCK20 or campaign stop is added.

Every row requires the design's nonempty common string identity fields
`game_uid`, `player`, `map_uid`, `faction`, `profile` (maximum 1024 characters),
integer `tick`, `seq`, `dropped`, and boolean `roster_complete`,
`eligibility_complete`, `dispatch_complete`. These are envelope requirements,
not permission to set a channel complete without its owner-produced evidence.
The supported row kinds are start, eligibility_transition, restraint_transition,
dispatch_intent, dispatch_observed, pulse, end. The latter two dispatch labels
do not establish resolver provenance by their presence.

Per participant: seq starts at zero and is contiguous; start is at tick zero;
ticks are monotonic; identity is stable; start/pulse/end coverage gaps are at
most 50 ticks; end is terminal and has an explicit boolean complete watermark.
Any dropped row or false channel watermark remains incomplete even if end
claims complete. Other participants can interleave independent sequences.
All rows must have a valid versioned envelope; malformed foreign rows also
invalidate the input rather than silently hiding capture corruption.

Bounds: 128 MiB input, 65536 bytes per line including newline, 200000 rows.
Strict JSON rejects duplicate keys and nonfinite numbers. Missing, malformed,
late, gapped, overflowing and unterminated capture returns UNKNOWN. Input is
read only; the report names the actual path and digest of consumed bytes.

Synthetic Python fixtures test the reader contract only. They are not emitted
C# evidence, runtime coverage, output sizing, cost, parity or the paired 3v3 gate.
The consumer is unpinned and unadopted until exact-SHA review. Subsequent work
must agree owner-produced eligibility/response fields and distinguish observed
activity from verified dispatch under the maintainer's no-engine ruling.
