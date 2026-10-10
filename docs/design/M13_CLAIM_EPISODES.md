# M13 claim admission identity checkpoint

This record-only checkpoint adds explicit claim identity to the protection and coalition
contracts. It does not activate M13 or change claim decisions, orders, RNG, or sync fields.

`BotClaimEpisode` identifies a participant, provider type, provider instance and positive
admission sequence. Counters are per owner; sequence zero/default is UNKNOWN. The encoded
owner key uses length-prefixed strings and invariant numeric formatting. Consumers must
compare the full owner and sequence, never the sequence alone.

EngineerBotModule mints an episode only when it admits a new EscortPlan. The stored episode
survives request reads, location/value changes and expiry pulses; a later admission gets a
new sequence. The provider instance comes from the actor's stable trait-info ordinal.
Unsupported owners and exhausted counters return UNKNOWN. Legacy saves contain no admission
history: resolving save data invalidates the counter and clears any existing plan identity,
preserving existing save fields and gameplay behavior rather than recycling an episode.

TeamBroadcast and coalition assignments carry an optional explicit episode and reject
foreign-owner/inactive-claim metadata as UNKNOWN. Existing callers remain compatible and
retain their previous elections. Unsupported publishers still publish UNKNOWN; the new,
default-off M13 consumer must refuse these claims. Legacy consumers are unchanged here.

The BotSituation TeamBroadcast publisher is deliberately outside this first checkpoint.
Its minimal admission plumbing is authorized as a separate follow-up commit. No snapshot
tick, location, disappearance or handout can manufacture admission/deployment proof.

Validation: ClaimEpisodeTest covers admission refresh/re-admission, distinct owners and
provider instances, missing history, legacy UNKNOWN, owner validation and exact coalition
transport across pulses. These are offline counter/record/fold regressions, not a game
capture or match-level proof. Runtime integration and the paired 3v3 gate remain pending.
