# Accepted delivery raw aggregation

`AiEconomyHealthRecorder` emits `resource-accepted-tick` instead of one
`resource-accepted` row per delivery. This is record-only: resource credit,
orders, RNG, queues, and the health schema are unchanged. Existing production
health consumers read pulse/cancel/end, not raw delivery rows. Raw totals
must never substitute for missing or incomplete health evidence.

Each player/tick rollup contains exact 64-bit `event_count`, signed `count`
and `value` sums, positive `credited_value` sum, and `accepted_event_count`.
`accepted_credit` means at least one event had positive credited value,
matching the existing per-event proof predicate. Negative/zero values do not
cancel positive accepted credit. Harvester identity remains unknown/null.

One event sample is retained, with its resource type capped at 64 characters.
`sample_count`, `detail_truncated`, and `resource_type_truncated` explicitly
distinguish sampled detail from complete totals. Detail truncation does not
poison total completeness. Numeric overflow or an undrained tick mismatch
still marks capture incomplete; writer I/O/byte/record failures retain their
existing sticky fail-closed behavior.

An ordinary observe only flushes an earlier tick, because the world capture
trait may run before that tick's delivery callbacks. A later delivery or
queue transition also flushes an earlier pending tick. Terminal observe
flushes the final pending tick before computing the end completeness tuple.
Early world disposal does not fabricate a complete end record.

## Declared sizing envelope

For P players, T captured ticks and Q total queue observations, accepted
delivery callbacks contribute at most P*T raw rows, independent of delivery
rate. The existing sizing workload has P=2, T=45,001, Q=2*901, hence at most
91,804 rows, strictly below 200,000. The previous three-event rate produced
271,808 rows. This proof is scoped to that queue/player/duration envelope;
unbounded queue traffic, players, or duration can still exceed writer limits.

`tools/ai/raw-size-fixture` exercises the compiled production accumulator and
serializer, verifies counts and credited sums, measures UTF-8 bytes including
newlines, and refuses to produce a passing receipt if any workload exceeds
128 MiB, 64 KiB per line, or 199,999 records. It includes three deliveries per
tick with escaped 64-character identifiers. Synthetic measurements establish
these declared bounds, not live event coverage or runtime performance.

`EconomyAcceptedTickTest` floods 270,006 callbacks through the real writer,
checks every credited total and completeness, and verifies same-tick deferral,
final drain, bounded text/details, mixed values, and sums above Int32.MaxValue.
The unchanged economy-invariant tests must continue to reject truncated,
foreign, or otherwise incomplete health streams. A fresh bounded build/test
and sizing receipt are required before this source candidate is qualified.
